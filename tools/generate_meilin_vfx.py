from __future__ import annotations

import math
import json
import plistlib
import shutil
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Any

try:
    from PIL import Image
except Exception:  # pragma: no cover - optional in editor environments
    Image = None


PROJECT_ROOT = Path(__file__).resolve().parents[1]
SOURCE_EFFECT_DIR = Path(r"E:\DATA\GODOT\res\1027\effect")
COMMON_EFFECT_DIR = Path(r"E:\DATA\GODOT\res\effect")
MODEL_DATA_DIR = Path(r"E:\DATA\GODOT\res\1027\model_data")
PARTICLE_TEXTURE_DIR = Path(r"E:\DATA\GODOT\res\ripper\particle")
CONVERTER = PROJECT_ROOT / "MeiLinMod" / "spine" / "SpineSkeletonDataConverter.exe"

CONFIG_ROOT = PROJECT_ROOT / "MeiLinMod" / "vfx_configs" / "1027" / "generated"
SPINE_ROOT = PROJECT_ROOT / "MeiLinMod" / "spine" / "effect" / "generated"
SCENE_ROOT = PROJECT_ROOT / "MeiLinMod" / "scenes" / "vfx" / "generated"
PARTICLE_IMAGE_ROOT = PROJECT_ROOT / "MeiLinMod" / "images" / "vfx" / "particles"

SKIP_CFX_CONTAINS = ("skill_x",)


@dataclass
class TextureInfo:
    source: Path
    res_path: str
    width: int
    height: int


def main() -> int:
    ensure_dirs()
    reset_generated_scene_root()
    cfx_files = collect_cfx_files()
    texture_map = copy_particle_textures(collect_particle_files(cfx_files))

    generated = 0
    generated_effects: set[str] = set()
    skipped: list[str] = []
    used_spines: set[str] = set()

    for cfx_path in cfx_files:
        try:
            layers = read_cfx_layers(cfx_path)
            missing = missing_sources(layers)
            if missing:
                skipped.append(f"{cfx_path.name}: missing {', '.join(sorted(missing))}")
                continue

            generate_cfx_scene(cfx_path, layers, texture_map, used_spines)
            generated += 1
            generated_effects.add(cfx_path.stem)
        except Exception as exc:
            skipped.append(f"{cfx_path.name}: {type(exc).__name__}: {exc}")

    print(f"Generated CFX scenes: {generated}")
    print(f"Converted/copied Spine sources: {len(used_spines)}")
    generate_command_config(generated_effects)
    if skipped:
        print("Skipped:")
        for item in skipped:
            print(f"  - {item}")
    return 0 if generated else 1


def ensure_dirs() -> None:
    for path in (CONFIG_ROOT, SPINE_ROOT, SCENE_ROOT, PARTICLE_IMAGE_ROOT):
        path.mkdir(parents=True, exist_ok=True)


def reset_generated_scene_root() -> None:
    project_root = PROJECT_ROOT.resolve()
    scene_root = SCENE_ROOT.resolve()
    if scene_root == project_root or project_root not in scene_root.parents:
        raise RuntimeError(f"Refuse to reset scene root outside project: {scene_root}")

    if SCENE_ROOT.exists():
        shutil.rmtree(SCENE_ROOT)
    SCENE_ROOT.mkdir(parents=True, exist_ok=True)


def collect_cfx_files() -> list[Path]:
    cfx_by_name: dict[str, Path] = {}

    for path in sorted(SOURCE_EFFECT_DIR.glob("*.cfx")):
        if not should_skip_cfx(path.stem):
            cfx_by_name[path.stem] = path

    srmd_refs = read_srmd_effect_refs()
    for effect in srmd_refs:
        if should_skip_cfx(effect) or effect in cfx_by_name:
            continue

        common_path = COMMON_EFFECT_DIR / f"{effect}.cfx"
        if common_path.exists():
            cfx_by_name[effect] = common_path

    return [cfx_by_name[name] for name in sorted(cfx_by_name)]


def should_skip_cfx(name: str) -> bool:
    return any(token in name for token in SKIP_CFX_CONTAINS)


def read_srmd_effect_refs() -> set[str]:
    srmd_path = MODEL_DATA_DIR / "1027.srmd"
    if not srmd_path.exists():
        return set()

    data = json.loads(srmd_path.read_text(encoding="utf-8"))
    refs: set[str] = set()
    for command in data.get("command", {}).values():
        for effect in command.get("effect") or []:
            file_name = str(effect.get("file_name") or "").strip()
            if file_name:
                refs.add(file_name)
    return refs


def collect_particle_files(cfx_files: list[Path]) -> list[Path]:
    particles: dict[str, Path] = {}
    for cfx_path in cfx_files:
        for layer in read_cfx_layers(cfx_path):
            if str(layer.get("format", "")).strip() != "particle":
                continue

            source = str(layer.get("source", "")).strip()
            particle_path = find_particle_source(source)
            if particle_path != null_path():
                particles[source] = particle_path

    return [particles[name] for name in sorted(particles)]


def null_path() -> Path:
    return Path()


def copy_particle_textures(particle_files: list[Path]) -> dict[str, TextureInfo]:
    textures: dict[str, TextureInfo] = {}
    texture_names = set()

    for particle_path in particle_files:
        data = plistlib.loads(particle_path.read_bytes())
        for emitter in data.get("emitters", []):
            name = str(emitter.get("textureFileName", "")).strip()
            if name:
                texture_names.add(name)

    for texture_name in sorted(texture_names):
        png_name = Path(texture_name).with_suffix(".png").name
        source = PARTICLE_TEXTURE_DIR / png_name
        if not source.exists():
            continue

        dest = PARTICLE_IMAGE_ROOT / png_name
        shutil.copy2(source, dest)
        width, height = image_size(dest)
        textures[texture_name] = TextureInfo(
            source=source,
            res_path=f"res://MeiLinMod/images/vfx/particles/{png_name}",
            width=width,
            height=height,
        )

    return textures


def image_size(path: Path) -> tuple[int, int]:
    if Image is None:
        return (100, 100)
    with Image.open(path) as image:
        return image.size


def read_cfx_layers(cfx_path: Path) -> list[dict[str, Any]]:
    data = plistlib.loads(cfx_path.read_bytes())
    layers = data.get("primitive", [])
    return [layer for layer in layers if isinstance(layer, dict) and not is_disabled(layer)]


def is_disabled(layer: dict[str, Any]) -> bool:
    value = layer.get("disable")
    return value is True or str(value).lower() in {"1", "true", "yes"}


def missing_sources(layers: list[dict[str, Any]]) -> set[str]:
    missing: set[str] = set()
    for layer in layers:
        source = str(layer.get("source", "")).strip()
        fmt = str(layer.get("format", "")).strip()
        if not source:
            continue

        if fmt == "spine":
            if find_spine_source(source) is None:
                missing.add(source)
        elif fmt == "particle":
            if find_particle_source(source) == null_path():
                missing.add(source)
    return missing


def find_particle_source(source: str) -> Path:
    for root in (SOURCE_EFFECT_DIR, COMMON_EFFECT_DIR):
        path = root / f"{source}.particle"
        if path.exists():
            return path
    return null_path()


def find_spine_source(source: str) -> Path | None:
    for root in (SOURCE_EFFECT_DIR, COMMON_EFFECT_DIR):
        if (root / f"{source}.json").exists() and (root / f"{source}.atlas").exists() and (root / f"{source}.png").exists():
            return root
    return None


def generate_cfx_scene(
    cfx_path: Path,
    layers: list[dict[str, Any]],
    texture_map: dict[str, TextureInfo],
    used_spines: set[str],
) -> None:
    effect = cfx_path.stem
    shutil.copy2(cfx_path, CONFIG_ROOT / cfx_path.name)

    ext_resources: list[str] = []
    ext_ids: dict[str, str] = {}
    sub_resources: list[str] = []
    nodes: list[str] = [f'[node name="{pascal_name(effect)}" type="Node2D"]']
    sub_index = 1

    def ext_id(resource_type: str, res_path: str, hint: str) -> str:
        key = f"{resource_type}:{res_path}"
        if key in ext_ids:
            return ext_ids[key]
        rid = f"{len(ext_ids) + 1}_{safe_id(hint)}"
        ext_ids[key] = rid
        ext_resources.append(f'[ext_resource type="{resource_type}" path="{res_path}" id="{rid}"]')
        return rid

    for layer_index, layer in enumerate(layers):
        fmt = str(layer.get("format", "")).strip()
        source = str(layer.get("source", "")).strip()
        if not fmt or not source:
            continue

        layer_name = f"{source}_layer_{layer_index + 1}"
        parent = "."
        node_lines = [f'[node name="{layer_name}" type="Node2D" parent="{parent}"]']
        append_layer_transform(node_lines, layer)
        delay = float_value(layer.get("delay"), 0.0) / 1000.0
        if delay > 0:
            node_lines.append("visible = false")
            node_lines.append(f'metadata/meilin_vfx_delay_sec = {format_float(delay)}')
        nodes.extend(node_lines)

        if fmt == "spine":
            skel_res = prepare_spine(source, used_spines)
            rid = ext_id("SpineSkeletonDataResource", skel_res, source)
            pma_rid = ext_id("Material", "res://MeiLinMod/materials/spine_pma.tres", "spine_pma")
            nodes.extend(
                [
                    f'[node name="{source}" type="SpineSprite" parent="{layer_name}"]',
                    f'normal_material = ExtResource("{pma_rid}")',
                    f"skeleton_data_res = ExtResource(\"{rid}\")",
                    'preview_skin = "Default"',
                    f'preview_animation = "{str(layer.get("ani", "animation"))}"',
                    "preview_frame = false",
                    "preview_time = 0.0",
                ]
            )
            opacity = float_value(layer.get("opacity"), 1.0)
            if opacity < 1.0:
                nodes.append(f"modulate = Color(1, 1, 1, {format_float(opacity)})")
        elif fmt == "particle":
            particle_path = find_particle_source(source)
            if particle_path == null_path():
                continue
            shutil.copy2(particle_path, CONFIG_ROOT / particle_path.name)
            particle_nodes, particle_subs, sub_index = build_particle_nodes(
                particle_path,
                layer_name,
                texture_map,
                ext_id,
                sub_index,
            )
            sub_resources.extend(particle_subs)
            nodes.extend(particle_nodes)

    scene_path = scene_file_path_for_effect(effect)
    scene_path.parent.mkdir(parents=True, exist_ok=True)
    load_steps = max(1, len(ext_resources) + len(sub_resources) + 1)
    content = ["[gd_scene load_steps=%d format=3]" % load_steps, ""]
    content.extend(ext_resources)
    if ext_resources:
        content.append("")
    content.extend(sub_resources)
    if sub_resources:
        content.append("")
    content.extend(nodes)
    scene_path.write_text("\n".join(content) + "\n", encoding="utf-8")


def append_layer_transform(lines: list[str], layer: dict[str, Any]) -> None:
    x = float_value(layer.get("x"), 0.0)
    y = -float_value(layer.get("y"), 0.0)
    if x or y:
        lines.append(f"position = Vector2({format_float(x)}, {format_float(y)})")

    sx, sy = parse_scale(layer.get("scale"))
    if abs(sx - 1.0) > 0.0001 or abs(sy - 1.0) > 0.0001:
        lines.append(f"scale = Vector2({format_float(sx)}, {format_float(sy)})")

    rotation = parse_random_or_float(layer.get("rotate"), 0.0)
    if rotation:
        lines.append(f"rotation_degrees = {format_float(rotation)}")

    z = int(round(float_value(layer.get("z"), 0.0)))
    if z:
        lines.append(f"z_index = {z}")


def parse_scale(value: Any) -> tuple[float, float]:
    if value is None:
        return (1.0, 1.0)
    text = str(value).strip()
    if not text:
        return (1.0, 1.0)
    if "~" in text:
        parts = [float_value(part, 1.0) for part in text.split("~", 1)]
        val = sum(parts) / len(parts)
        return (val, val)
    if "," in text:
        parts = [float_value(part, 1.0) for part in text.split(",", 1)]
        return (parts[0], parts[1])
    val = float_value(text, 1.0)
    return (val, val)


def parse_random_or_float(value: Any, default: float) -> float:
    if value is None:
        return default
    text = str(value).strip()
    if "~" in text:
        a, b = text.split("~", 1)
        return (float_value(a, default) + float_value(b, default)) / 2.0
    return float_value(text, default)


def prepare_spine(source: str, used_spines: set[str]) -> str:
    source_root = find_spine_source(source)
    if source_root is None:
        raise FileNotFoundError(source)

    out_dir = SPINE_ROOT / source
    out_dir.mkdir(parents=True, exist_ok=True)
    json_src = source_root / f"{source}.json"
    atlas_src = source_root / f"{source}.atlas"
    png_src = source_root / f"{source}.png"
    json_dest = out_dir / json_src.name
    atlas_dest = out_dir / atlas_src.name
    png_dest = out_dir / png_src.name
    skel_dest = out_dir / f"{source}.skel"
    tres_dest = out_dir / f"{source}_skel_data.tres"

    shutil.copy2(json_src, json_dest)
    shutil.copy2(atlas_src, atlas_dest)
    shutil.copy2(png_src, png_dest)

    if not skel_dest.exists() or json_dest.stat().st_mtime > skel_dest.stat().st_mtime:
        subprocess.run(
            [str(CONVERTER), str(json_dest), str(skel_dest), "-v", "4.2.11"],
            check=True,
            cwd=str(PROJECT_ROOT),
        )

    tres_dest.write_text(
        "\n".join(
            [
                "[gd_resource type=\"SpineSkeletonDataResource\" load_steps=3 format=3]",
                "",
                f"[ext_resource type=\"SpineAtlasResource\" path=\"res://MeiLinMod/spine/effect/generated/{source}/{source}.atlas\" id=\"1_atlas\"]",
                f"[ext_resource type=\"SpineSkeletonFileResource\" path=\"res://MeiLinMod/spine/effect/generated/{source}/{source}.skel\" id=\"2_skel\"]",
                "",
                "[resource]",
                "atlas_res = ExtResource(\"1_atlas\")",
                "skeleton_file_res = ExtResource(\"2_skel\")",
            ]
        )
        + "\n",
        encoding="utf-8",
    )
    used_spines.add(source)
    return f"res://MeiLinMod/spine/effect/generated/{source}/{source}_skel_data.tres"


def generate_command_config(generated_scenes: set[str]) -> None:
    srmd_path = MODEL_DATA_DIR / "1027.srmd"
    srcs_path = MODEL_DATA_DIR / "1027.srcs"
    if not srmd_path.exists():
        return

    srmd = json.loads(srmd_path.read_text(encoding="utf-8"))
    srcs = json.loads(srcs_path.read_text(encoding="utf-8")) if srcs_path.exists() else {}
    commands: dict[str, Any] = {}
    for name, command in srmd.get("command", {}).items():
        commands[name] = {
            "animation": [compact_animation(event) for event in command.get("ani") or []],
            "effects": [compact_effect(event, generated_scenes) for event in command.get("effect") or []],
            "hits": [compact_event(event) for event in command.get("hit") or []],
            "shakes": [compact_event(event) for event in command.get("shake") or []],
            "stops": [compact_event(event) for event in command.get("stop") or []],
            "moves": [compact_event(event) for event in command.get("move") or []],
            "closeCombat": bool(command.get("close_combat", False)),
            "closeCombatOffset": command.get("close_combat_offset"),
            "sparkDelayMs": command.get("spark_delay"),
            "sparkOffset": command.get("spark_offset"),
        }

    output = {
        "source": str(srmd_path),
        "commandSets": srcs.get("command_sets", {}),
        "commands": commands,
        "missingEffectScenes": sorted(
            {
                effect["fileName"]
                for command in commands.values()
                for effect in command["effects"]
                if effect["fileName"] and not effect["scenePath"]
            }
        ),
    }
    (CONFIG_ROOT / "meilin_vfx_commands.json").write_text(
        json.dumps(output, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )


def compact_animation(event: dict[str, Any]) -> dict[str, Any]:
    return {
        "animationName": event.get("animation_name"),
        "delayMs": float_value(event.get("delay"), 0.0),
        "durationMs": float_value(event.get("duration"), 0.0),
        "loop": bool(event.get("loop", False)),
        "waitUntilEnd": bool(event.get("wait_until_end", False)),
    }


def compact_effect(event: dict[str, Any], generated_scenes: set[str]) -> dict[str, Any]:
    file_name = str(event.get("file_name") or "").strip()
    return {
        "fileName": file_name,
        "sceneGroup": effect_scene_group(file_name) if file_name else "",
        "scenePath": scene_path_for_effect(file_name) if file_name in generated_scenes else "",
        "delayMs": float_value(event.get("delay"), 0.0),
        "durationMs": float_value(event.get("duration"), -1.0),
        "type": event.get("type"),
        "boneType": event.get("bone_type"),
        "bone": event.get("bone"),
        "offset": event.get("offset"),
        "offsetXY": event.get("offset_xy"),
        "scale": float_value(event.get("scale"), 1.0),
        "inheritScale": bool(event.get("inherit_scale", False)),
        "formationScale": bool(event.get("formation_scale", False)),
        "ignoreSlotScale": bool(event.get("igenor_slot_scale", False)),
        "rotation": float_value(event.get("rotation"), 0.0),
        "zOrder": int_value(event.get("zorder"), 0),
        "globalZ": int_value(event.get("global_z"), 0),
        "loop": bool(event.get("loop", False)),
        "waitUntilEnd": bool(event.get("wait_until_end", False)),
    }


def compact_event(event: dict[str, Any]) -> dict[str, Any]:
    ignored = {"guid", "id", "from_guid", "bounds", "elapsed_start", "elapsed_end", "elapsed", "etty"}
    result = {key: value for key, value in event.items() if key not in ignored}
    result["delayMs"] = float_value(event.get("delay"), 0.0)
    result.pop("delay", None)
    return result


def scene_path_for_effect(effect: str) -> str:
    group = effect_scene_group(effect)
    return f"res://MeiLinMod/scenes/vfx/generated/{group}/{effect}.tscn"


def scene_file_path_for_effect(effect: str) -> Path:
    return SCENE_ROOT / effect_scene_group(effect) / f"{effect}.tscn"


def effect_scene_group(effect: str) -> str:
    if not effect:
        return "logic"

    if effect in {
        "common_hit_eff",
        "meirin_attack_play_target",
        "meirin_attack_play_target_botglow",
    }:
        return "common"

    if "attack_play1" in effect:
        return "attack_play1"
    if "attack_play2" in effect:
        return "attack_play2"
    if effect.startswith("meirin_strong_attack"):
        return "strong_attack"
    if effect.startswith("meirin_unique_strong_attack") or effect == "meirin_uinique_strong_attack_play_selfglow":
        return "u2_attack"

    if effect.startswith("meirin_unique_buff"):
        return "u1_buff"
    if effect.startswith("meirin_debuff"):
        return "debuff"

    if effect.startswith("meirin_1027_u3"):
        return "u3_buff"
    if effect.startswith("meirin_1027_u4"):
        return "u4_buff"

    if (
        "skill_2" in effect
        or effect.startswith("meirin_1027_ug")
        or effect.startswith("meirin_1027_unique_technical_attack")
    ):
        return "ug_attack"

    if "skill_x" in effect:
        return "skill_x"

    if effect in {
        "meirin_1027_self_in_eff",
        "meirin_1027_self_out_eff",
        "meirin_1027_self_out_sparkle_eff",
    }:
        return "entry_exit"

    return "misc"


def build_particle_nodes(
    particle_path: Path,
    parent: str,
    texture_map: dict[str, TextureInfo],
    ext_id_func,
    sub_index: int,
) -> tuple[list[str], list[str], int]:
    data = plistlib.loads(particle_path.read_bytes())
    nodes: list[str] = []
    subs: list[str] = []

    for emitter_index, emitter in enumerate(data.get("emitters", []), start=1):
        if emitter.get("disable") is True:
            continue

        texture_name = str(emitter.get("textureFileName", "")).strip()
        texture = texture_map.get(texture_name)
        if texture is None:
            continue

        name = safe_node_name(str(emitter.get("name", "emitter")) or "emitter")
        node_name = f"{particle_path.stem}_{name}_{emitter_index}"
        tex_id = ext_id_func("Texture2D", texture.res_path, Path(texture.res_path).stem)

        mat_id = f"CanvasItemMaterial_{sub_index}"; sub_index += 1
        gradient_id = f"Gradient_{sub_index}"; sub_index += 1
        gradient_texture_id = f"GradientTexture1D_{sub_index}"; sub_index += 1
        curve_id = f"Curve_{sub_index}"; sub_index += 1
        curve_texture_id = f"CurveTexture_{sub_index}"; sub_index += 1
        process_id = f"ParticleProcessMaterial_{sub_index}"; sub_index += 1

        blend_mode = 1 if int_value(emitter.get("blendFuncDestination"), 0) == 1 else 0
        subs.extend(
            [
                f'[sub_resource type="CanvasItemMaterial" id="{mat_id}"]',
                f"blend_mode = {blend_mode}",
                "",
            ]
        )

        colors = parse_colors(emitter)
        subs.extend(
            [
                f'[sub_resource type="Gradient" id="{gradient_id}"]',
                f"offsets = PackedFloat32Array({', '.join(format_float(c[0]) for c in colors)})",
                "colors = PackedColorArray("
                + ", ".join(
                    f"{format_float(c[1])}, {format_float(c[2])}, {format_float(c[3])}, {format_float(c[4])}"
                    for c in colors
                )
                + ")",
                "",
                f'[sub_resource type="GradientTexture1D" id="{gradient_texture_id}"]',
                f"gradient = SubResource(\"{gradient_id}\")",
                "",
            ]
        )

        start_size = max(1.0, float_value(emitter.get("startParticleSize"), 20.0))
        finish_size = max(0.1, float_value(emitter.get("finishParticleSize"), start_size))
        finish_ratio = max(0.01, finish_size / start_size)
        subs.extend(
            [
                f'[sub_resource type="Curve" id="{curve_id}"]',
                f"_data = [Vector2(0, 1), 0.0, 0.0, 0, 0, Vector2(1, {format_float(finish_ratio)}), 0.0, 0.0, 0, 0]",
                "point_count = 2",
                "",
                f'[sub_resource type="CurveTexture" id="{curve_texture_id}"]',
                f"curve = SubResource(\"{curve_id}\")",
                "",
            ]
        )

        angle = float_value(emitter.get("angle"), 0.0)
        direction = (math.cos(math.radians(angle)), -math.sin(math.radians(angle)), 0.0)
        speed = float_value(emitter.get("speed"), 0.0)
        speed_var = float_value(emitter.get("speedVariance"), 0.0)
        spread = abs(float_value(emitter.get("angleVariance"), 0.0)) * 2.0
        var_x = abs(float_value(emitter.get("sourcePositionVariancex"), 0.0))
        var_y = abs(float_value(emitter.get("sourcePositionVariancey"), 0.0))
        scale_min = max(0.01, (start_size - float_value(emitter.get("startParticleSizeVariance"), 0.0)) / max(texture.width, 1))
        scale_max = max(scale_min, (start_size + float_value(emitter.get("startParticleSizeVariance"), 0.0)) / max(texture.width, 1))
        rot = float_value(emitter.get("rotationStart"), 0.0)
        rot_var = float_value(emitter.get("rotationStartVariance"), 0.0)
        angular = float_value(emitter.get("rotationDir"), 0.0)
        angular_var = float_value(emitter.get("rotationDirVariance"), 0.0)

        subs.extend(
            [
                f'[sub_resource type="ParticleProcessMaterial" id="{process_id}"]',
                "particle_flag_disable_z = true",
                "emission_shape = 3",
                f"emission_shape_scale = Vector3({format_float(var_x * 2.0)}, {format_float(var_y * 2.0)}, 1)",
                "emission_box_extents = Vector3(1, 1, 1)",
                f"direction = Vector3({format_float(direction[0])}, {format_float(direction[1])}, 0)",
                f"spread = {format_float(spread)}",
                f"initial_velocity_min = {format_float(max(0.0, speed - speed_var))}",
                f"initial_velocity_max = {format_float(max(0.0, speed + speed_var))}",
                f"gravity = Vector3({format_float(float_value(emitter.get('gravityx'), 0.0))}, {format_float(-float_value(emitter.get('gravityy'), 0.0))}, 0)",
                f"scale_min = {format_float(scale_min)}",
                f"scale_max = {format_float(scale_max)}",
                f"scale_curve = SubResource(\"{curve_texture_id}\")",
                f"color_ramp = SubResource(\"{gradient_texture_id}\")",
                f"angle_min = {format_float(rot - rot_var)}",
                f"angle_max = {format_float(rot + rot_var)}",
                f"angular_velocity_min = {format_float(angular - angular_var)}",
                f"angular_velocity_max = {format_float(angular + angular_var)}",
                "",
            ]
        )

        pos_x = float_value(emitter.get("sourcePositionx"), 0.0)
        pos_y = -float_value(emitter.get("sourcePositiony"), 0.0)
        amount = max(1, int(round(float_value(emitter.get("emissionRate"), 20.0))))
        lifetime = max(0.05, float_value(emitter.get("particleLifespan"), 0.5))
        duration = float_value(emitter.get("duration"), 0.0)
        start_delay = float_value(emitter.get("startDelay"), 0.0)

        nodes.extend(
            [
                f'[node name="{node_name}" type="GPUParticles2D" parent="{parent}"]',
                f"material = SubResource(\"{mat_id}\")",
                f"position = Vector2({format_float(pos_x)}, {format_float(pos_y)})",
                "emitting = false",
                f"amount = {amount}",
                f"texture = ExtResource(\"{tex_id}\")",
                f"lifetime = {format_float(lifetime)}",
                f"preprocess = {format_float(max(0.0, start_delay))}",
                "randomness = 0.6",
                "one_shot = true",
                "explosiveness = 1.0",
                f"process_material = SubResource(\"{process_id}\")",
            ]
        )
        if duration > 0:
            nodes.append(f"visibility_rect = Rect2(-1000, -1000, 2000, 2000)")

    return nodes, subs, sub_index


def parse_colors(emitter: dict[str, Any]) -> list[tuple[float, float, float, float, float]]:
    blends = emitter.get("colorBlends")
    if blends:
        text = str(blends[0]) if isinstance(blends, list) else str(blends)
        colors: list[tuple[float, float, float, float, float]] = []
        for part in text.split(";"):
            vals = [float_value(v, 0.0) for v in part.split(",")]
            if len(vals) >= 5:
                # CFX order: offset, alpha, red, green, blue.
                colors.append((vals[0], vals[2], vals[3], vals[4], vals[1]))
        if colors:
            return colors

    return [
        (
            0.0,
            float_value(emitter.get("startColorRed"), 1.0),
            float_value(emitter.get("startColorGreen"), 1.0),
            float_value(emitter.get("startColorBlue"), 1.0),
            float_value(emitter.get("startColorAlpha"), 1.0),
        ),
        (
            1.0,
            float_value(emitter.get("finishColorRed"), 1.0),
            float_value(emitter.get("finishColorGreen"), 1.0),
            float_value(emitter.get("finishColorBlue"), 1.0),
            float_value(emitter.get("finishColorAlpha"), 0.0),
        ),
    ]


def float_value(value: Any, default: float = 0.0) -> float:
    if value is None:
        return default
    text = str(value).strip()
    if not text:
        return default
    if text.startswith("."):
        text = "0" + text
    try:
        return float(text)
    except ValueError:
        return default


def int_value(value: Any, default: int = 0) -> int:
    try:
        return int(float_value(value, default))
    except Exception:
        return default


def format_float(value: float) -> str:
    if abs(value) < 0.000001:
        value = 0.0
    text = f"{value:.6f}".rstrip("0").rstrip(".")
    return text if text else "0"


def safe_id(value: str) -> str:
    return "".join(ch if ch.isalnum() else "_" for ch in value)[:40] or "res"


def safe_node_name(value: str) -> str:
    return safe_id(value)


def pascal_name(value: str) -> str:
    return "".join(part[:1].upper() + part[1:] for part in safe_id(value).split("_") if part) or "Vfx"


if __name__ == "__main__":
    raise SystemExit(main())
