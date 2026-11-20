"""Maintain exact BasicExport exclusions for authoring-only Spine JSON; verify Godot 4.5 PCKs."""

import argparse
import json
import os
import posixpath
import re
import struct
from pathlib import Path


def skeleton_sources(project, mod):
    sources = {}
    for binary in (project / mod).rglob("*.skel"):
        source = binary.with_suffix(".json")
        remap = binary.with_suffix(".skel.import")
        if not source.is_file() or not remap.is_file() or not binary.stat().st_size:
            continue
        if 'type="SpineSkeletonFileResource"' not in remap.read_text(encoding="utf-8"):
            continue
        data = json.loads(source.read_bytes())
        header = data.get("skeleton") if isinstance(data, dict) else None
        if isinstance(header, dict) and header.get("spine") and isinstance(data.get("bones"), list):
            sources[source.relative_to(project).as_posix()] = source

    # A direct runtime reference wins, including paths relative to a scene/resource.
    json_ref = re.compile(r'''["']([^"'\r\n]*?\.json)["']''', re.IGNORECASE)
    referenced = set()
    development = {"tmp", "tmpmods", "tests", "tools", "packages", "bin", "obj", "docs", "skills", "outputs", "logs"}
    for directory, dirs, filenames in os.walk(project):
        if Path(directory) == project:
            dirs[:] = [d for d in dirs if not d.startswith(".") and d not in development]
        for name in filenames:
            file = Path(directory) / name
            if file.suffix.lower() not in {".gd", ".cs", ".tscn", ".tres", ".json", ".gdshader"}:
                continue
            if file.relative_to(project).as_posix() in sources:
                continue
            for ref in json_ref.findall(file.read_text(encoding="utf-8-sig")):
                if ref.startswith("res://"):
                    referenced.add(ref[6:])
                else:
                    referenced.add(posixpath.normpath((file.parent.relative_to(project) / ref).as_posix()))
                    referenced.add(ref)
    return {path: file for path, file in sources.items() if path not in referenced}


def sync_preset(preset, project, mod, sources, update):
    raw = preset.read_bytes()
    pattern = rb'(?m)^exclude_filter="([^"\r\n]*)"'
    matches = list(re.finditer(pattern, raw))
    if len(matches) != 1:
        raise ValueError(f"{preset}: expected one BasicExport exclude_filter")
    match = matches[0]
    current = match[1].decode("utf-8").split(",")

    def manual(entry):
        # Preserve unrelated exclusions and globs; exact paired paths are tool-managed.
        return not (entry.startswith(mod + "/") and entry.endswith(".json")
                    and not any(char in entry for char in "*?[")
                    and (project / entry).with_suffix(".skel").is_file())

    desired = ",".join([entry for entry in current if entry and manual(entry)] + sorted(sources))
    if desired.encode() != match[1]:
        if not update:
            raise ValueError(f"{preset}: skeleton exclusion list is stale (run with --update)")
        preset.write_bytes(raw[:match.start(1)] + desired.encode() + raw[match.end(1):])


def pck_index(pack):
    with pack.open("rb") as stream:
        header = stream.read(40)
        if header[:4] != b"GDPC" or struct.unpack_from("<I", header, 4)[0] != 3:
            raise ValueError(f"Not a standalone Godot 4.5 v3 PCK: {pack}")
        stream.seek(struct.unpack_from("<Q", header, 32)[0])
        count, = struct.unpack("<I", stream.read(4))
        result = set()
        for _ in range(count):
            length, = struct.unpack("<I", stream.read(4))
            result.add(stream.read(length).rstrip(b"\0").decode("utf-8").removeprefix("res://"))
            stream.seek(36, 1)
        return result


def verify_pack(project, mod, sources, pack, baseline):
    entries = pck_index(pack)
    problems = set(sources) & entries
    for path in sources:
        remap = Path(path).with_suffix(".skel.import")
        imported = re.search(r'^path="res://([^"]+)"', (project / remap).read_text(), re.MULTILINE)
        for required in (remap.as_posix(), imported[1] if imported else str(remap) + ": missing import path"):
            if required not in entries:
                problems.add(required)
    if baseline:
        old = pck_index(baseline)
        # Both logical resources/remaps and their imported binary payloads must survive the export.
        problems.update(path for path in old - entries - set(sources)
                        if path.startswith((mod + "/", ".godot/imported/")))
    if problems:
        raise ValueError(f"{pack}: unexpected inclusions or missing runtime files:\n" + "\n".join(sorted(problems)))
    print(f"{pack}: {pack.stat().st_size:,} bytes; {len(entries)} files; all runtime-resource checks passed")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path.cwd())
    parser.add_argument("--mod", required=True)
    parser.add_argument("--preset", type=Path, help="Alternate copy of export_presets.cfg to update/check")
    parser.add_argument("--update", action="store_true")
    parser.add_argument("--pack", type=Path)
    parser.add_argument("--baseline", type=Path, help="Older PCK; reject unrelated runtime-resource loss")
    args = parser.parse_args()
    project = args.project.resolve()
    sources = skeleton_sources(project, args.mod)
    if not sources:
        raise ValueError("No paired authoring skeletons found; check --project and --mod")
    sync_preset(args.preset or project / "export_presets.cfg", project, args.mod, sources, args.update)
    print(f"{args.mod}: {len(sources)} authoring JSON files, {sum(p.stat().st_size for p in sources.values()):,} bytes")
    if args.pack:
        verify_pack(project, args.mod, sources, args.pack, args.baseline)


if __name__ == "__main__":
    main()
