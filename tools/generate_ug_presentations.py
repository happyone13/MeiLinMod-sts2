"""Build the two card cinematics from portrait SRMD/CFX, without resetting existing VFX.

Run from either repository with --character meilin or --character yuki.
Timing follows from_guid + wait_until_end; the editor's elapsed fields are stale.
"""
from __future__ import annotations
import argparse
import json
import math
import plistlib
import re
import shutil
import subprocess
from pathlib import Path
from PIL import Image


def duration(value):
    if isinstance(value, dict):
        return max([float(value.get('time', 0)), float(value.get('duration', 0))] +
                   [duration(v) for v in value.values() if isinstance(v, (dict, list))])
    if isinstance(value, list):
        return max([0] + [duration(v) for v in value])
    return 0


def events_for(command):
    events = {e['guid']: e for group in command.values() if isinstance(group, list)
              for e in group if isinstance(e, dict) and 'guid' in e}
    starts = {}
    def start(e, visiting=frozenset()):
        key = e['guid']
        if key in visiting:
            raise ValueError('Cycle in SRMD event graph')
        if key not in starts:
            parent = events.get(e.get('from_guid'))
            starts[key] = float(e.get('delay') or 0) + (start(parent, visiting | {key}) +
                (max(0, float(parent.get('duration') or 0)) if parent.get('wait_until_end') else 0)
                if parent else 0)
        return starts[key]
    return sorted([dict(e, at=round(start(e) / 1000, 5)) for e in events.values()], key=lambda e: e['at'])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--character', choices=['meilin', 'yuki'], required=True)
    parser.add_argument('--presentation', choices=['ug', 'ux'], default='ug')
    parser.add_argument('--source', type=Path)
    args = parser.parse_args()
    char = args.character
    presentation = args.presentation
    if char == 'meilin':
        mod, ident, command = 'MeiLinMod', '1027', ('ug_attack' if presentation == 'ug' else 'ux_buff')
    else:
        mod, ident, command = 'YukiMod', '1057', ('ug_all_attack' if presentation == 'ug' else 'ux_all_attack')
    root = Path(__file__).resolve().parents[1]
    source = args.source or Path(f'E:/DATA/GODOT/res/{ident}/portrait_{char}')
    out = root / mod / f'scenes/vfx/{presentation}'
    out.mkdir(parents=True, exist_ok=True)
    converter = root / mod / 'spine/SpineSkeletonDataConverter.exe'
    if not converter.exists():
        converter = root.parent / 'MeiLinMod-sts2/MeiLinMod/spine/SpineSkeletonDataConverter.exe'
    events = events_for(json.loads((source / (ident + '.srmd')).read_text())['command'][command])
    effects = []
    resources = {}

    def spine(name):
        if name in resources:
            return resources[name]
        dest = root / mod / f'spine/{presentation}' / name
        dest.mkdir(parents=True, exist_ok=True)
        for ext in ['json', 'atlas', 'png']:
            shutil.copy2(source / (name + '.' + ext), dest / (name + '.' + ext))
        skel = dest / (name + '.skel')
        if not skel.exists() or skel.stat().st_mtime < (dest / (name + '.json')).stat().st_mtime:
            subprocess.run([str(converter), str(dest / (name + '.json')), str(skel), '-v', '4.2.11'], check=True, stdout=subprocess.DEVNULL)
        res = f'res://{mod}/spine/{presentation}/{name}/{name}'
        (dest / (name + '.tres')).write_text(f'''[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]
[ext_resource type="SpineAtlasResource" path="{res}.atlas" id="1"]
[ext_resource type="SpineSkeletonFileResource" path="{res}.skel" id="2"]
[resource]
atlas_res = ExtResource("1")
skeleton_file_res = ExtResource("2")
default_mix = 0.0
''', encoding='utf-8')
        resources[name] = res + '.tres'
        return resources[name]

    def composite(name):
        # MeiLin already has converted particles and common hit effects. Reuse that
        # project-local conversion; only the portrait cut-in gets a fresh conversion.
        if char == 'meilin' and presentation == 'ug' and name != 'meirin_1027_ug_attack_cutin_eff':
            existing = list((root / mod / 'scenes/vfx/generated').rglob(name + '.tscn'))
            if not existing:
                raise FileNotFoundError(name)
            return 'res://' + existing[0].relative_to(root).as_posix()
        layers = plistlib.loads((source / (name + '.cfx')).read_bytes())['primitive']
        if any(layer.get('format') == 'particle' for layer in layers):
            # Reuse the established MeiLin particle converter. It writes these
            # mixed Spine/particle composites into generated/skill_x and keeps
            # particle textures in the existing shared image directory.
            import generate_meilin_vfx as meilin_vfx
            meilin_vfx.SOURCE_EFFECT_DIR = source
            meilin_vfx.COMMON_EFFECT_DIR = Path('E:/DATA/GODOT/res/1027/effect')
            cfx_path = source / (name + '.cfx')
            active_layers = meilin_vfx.read_cfx_layers(cfx_path)
            particle_files = []
            for layer in active_layers:
                if layer.get('format') == 'particle':
                    particle_files.append(meilin_vfx.find_particle_source(str(layer['source'])))
            texture_map = meilin_vfx.copy_particle_textures(particle_files)
            meilin_vfx.generate_cfx_scene(cfx_path, active_layers, texture_map, set())
            return meilin_vfx.scene_path_for_effect(name)
        layers.sort(key=lambda x: float(x.get('z') or 0))
        lines = [f'[gd_scene load_steps={len(layers)+2} format=3]',
                 f'[ext_resource type="Material" path="res://{mod}/materials/spine_pma.tres" id="pma"]']
        for i, layer in enumerate(layers):
            assert layer['format'] == 'spine', layer
            lines.append(f'[ext_resource type="SpineSkeletonDataResource" path="{spine(layer["source"])}" id="{i}"]')
        lines.append(f'[node name="{name}" type="Node2D"]')
        for i, layer in enumerate(layers):
            anim = layer.get('ani') or 'animation'
            x, y, scale = float(layer.get('x') or 0), -float(layer.get('y') or 0), float(layer.get('scale') or 1)
            lines += [f'[node name="{layer["source"]}" type="SpineSprite" parent="."]',
                      'normal_material = ExtResource("pma")',
                      f'position = Vector2({x}, {y})', f'scale = Vector2({scale}, {scale})',
                      f'z_index = {max(-2000, min(2000, int(float(layer.get("z") or 0) / 10)))}', f'skeleton_data_res = ExtResource("{i}")',
                      f'preview_animation = "{anim}"', 'preview_frame = false',
                      f'metadata/ug_duration = {duration(json.loads((source / (layer["source"]+".json")).read_text())["animations"][anim])}']
        (out / (name + '.tscn')).write_text('\n'.join(lines)+'\n', encoding='utf-8')
        return f'res://{mod}/scenes/vfx/{presentation}/{name}.tscn'

    tracks = {}
    for event in events:
        if event['etty'] in ['CAM_SPINE', 'NODE_ANI']:
            name = event['file_name']
            tracks[name] = json.loads((source / (name + '.json')).read_text())
        if event['etty'] != 'EFFECT' or not event.get('file_name'):
            continue
        name = event['file_name']
        scene = composite(name)
        # Never retain a completed Spine on its opaque final frame.
        cfx = source / (name + '.cfx')
        life = 0.6
        if cfx.exists():
            for layer in plistlib.loads(cfx.read_bytes())['primitive']:
                path = source / (str(layer.get('source')) + '.json')
                if path.exists():
                    anims = json.loads(path.read_text()).get('animations', {})
                    life = max(life, duration(anims.get(layer.get('ani') or 'animation', anims)))
        effects.append(dict(event, scene=scene, life=round(life + 0.05, 4)))
    hit = min(e['at'] for e in events if e['etty'] in ['HIT', 'DAMAGE'] and e.get('play_action'))
    total = max([e['at'] + max(0, float(e.get('duration') or 0))/1000 for e in events] +
                [e['at'] + e['life'] for e in effects]) + 0.1
    data = dict(character=char, presentation=presentation, hit=hit, total=round(total, 4), events=events, effects=effects, tracks=tracks)
    if presentation == 'ux':
        candidates = sorted(
            (p for p in source.glob('*.webp') if re.fullmatch(r'[0-9a-fA-F]{32}\.webp', p.name)),
            key=lambda p: p.stat().st_mtime,
            reverse=True)
        if candidates:
            webp = candidates[0]
            frames_dir = root / mod / 'images/vfx/ux_video'
            frames_dir.mkdir(parents=True, exist_ok=True)
            for old in frames_dir.glob('frame_*.png'):
                old.unlink()
            image = Image.open(webp)
            cutin_frames = []
            for index in range(image.n_frames):
                image.seek(index)
                frame = image.convert('RGBA')
                left = max(0, (frame.width - 1280) // 2)
                top = max(0, (frame.height - 720) // 2)
                frame = frame.crop((left, top, min(left + 1280, frame.width), min(top + 720, frame.height)))
                path = frames_dir / f'frame_{index:03d}.png'
                frame.save(path, compress_level=6)
                cutin_frames.append({
                    'path': f'res://{mod}/images/vfx/ux_video/{path.name}',
                    'duration': round(float(image.info.get('duration', 100)) / 1000.0, 4),
                })
            data['cutin'] = {'at': 0.0 if char == 'meilin' else 0.6, 'frames': cutin_frames}
    (out / 'timeline.json').write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    actor_resource = (f'res://{mod}/spine/q/q.tres' if char == 'meilin'
                      else f'res://{mod}/spine/q/{ident}_skel_data.tres')
    lines = ['[gd_scene format=3]',
             f'[ext_resource type="Script" path="res://{mod}/scenes/vfx/{presentation}/ug_stage.gd" id="script"]',
             f'[ext_resource type="SpineSkeletonDataResource" path="{actor_resource}" id="actor"]',
             f'[ext_resource type="Material" path="res://{mod}/materials/spine_pma.tres" id="pma"]']
    for i, e in enumerate(effects):
        lines.append(f'[ext_resource type="PackedScene" path="{e["scene"]}" id="fx{i}"]')
    lines += ['[node name="UgPresentation" type="CanvasLayer"]', 'layer = 4090', 'script = ExtResource("script")',
              f'config_path = "res://{mod}/scenes/vfx/{presentation}/timeline.json"',
              '[node name="Stage" type="Node2D" parent="."]',
              '[node name="Blackout" type="ColorRect" parent="Stage"]',
              'offset_left = -4096.0', 'offset_top = -4096.0', 'offset_right = 4096.0', 'offset_bottom = 4096.0',
              'mouse_filter = 2', 'color = Color(0, 0, 0, 1)', 'z_index = -4096',
              '[node name="World" type="Node2D" parent="Stage"]',
              '[node name="Actor" type="SpineSprite" parent="Stage/World"]',
              'normal_material = ExtResource("pma")',
              'skeleton_data_res = ExtResource("actor")', 'position = Vector2(-320, 175)',
              'visible = false', 'z_index = 0']
    for i, e in enumerate(effects):
        parent = 'Stage' if e['type'] == 'SCREEN' else 'Stage/World'
        lines += [f'[node name="Fx{i}" parent="{parent}" instance=ExtResource("fx{i}")]', 'visible = false']
    (out / 'presentation.tscn').write_text('\n'.join(lines)+'\n', encoding='utf-8')
    if presentation == 'ux':
        shutil.copy2(root / mod / 'scenes/vfx/ug/ug_stage.gd', out / 'ug_stage.gd')
    print(f'{mod} {presentation.upper()}: {len(effects)} effects, {len(resources)} new skeletons, HIT {hit}s, end {total:.4f}s')


if __name__ == '__main__':
    main()
