"""Build an isolated Roswitha UG/UX Spine bake project; never edits the source mod."""
import json
import plistlib
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT.parent / '罗丝薇瑟皮肤mod'
OUT = ROOT / 'tmp/roswitha_cinematics'
CONVERTER = ROOT / 'MeiLinMod/spine/SpineSkeletonDataConverter.exe'


def duration(v):
    if isinstance(v, dict):
        return max([float(v.get('time', 0))] + [duration(x) for x in v.values() if isinstance(x, (dict, list))])
    return max([0] + [duration(x) for x in v]) if isinstance(v, list) else 0


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    shutil.copy2(ROOT / 'tools/roswitha_cinematic_bake.gd', OUT / 'bake.gd')
    project = SOURCE / 'project'
    shutil.copytree(project / 'bin', OUT / 'bin', dirs_exist_ok=True)
    base = Path('ArtWorks/modspine/effect/roswitha/roswitha_ug_buff')
    shutil.copytree(project / base, OUT / base, dirs_exist_ok=True)
    shutil.copy2(project / 'ArtWorks/modspine/scripts/roswitha_ug_stage.gd', OUT / 'ug_stage.gd')
    raw = SOURCE / 'ux与ug/ux/原始素材'
    assets = OUT / 'ux_assets'
    assets.mkdir(exist_ok=True)
    layers, missing, particles = [], [], []
    # Main composites: the second starts at 4.733s in the supplied command manifest.
    # Target effects need an enemy; keep them out of this standalone screen bake.
    for composite, start in [('rossweisse_30134_ux_attack_01', .010), ('rossweisse_30134_ux_attack_02', 4.733)]:
        recipe = plistlib.loads((raw / (composite + '.cfx')).read_bytes())
        for layer in recipe['primitive']:
            name = layer['source']
            if layer.get('format') != 'spine':
                particles.append(name)
                continue
            if not (raw / (name + '.json')).exists():
                missing.append(name)
                continue
            data = json.loads((raw / (name + '.json')).read_text(encoding='utf-8'))
            anim = layer.get('ani') or 'animation'
            if anim not in data['animations']:
                raise ValueError((name, anim, list(data['animations'])))
            for ext in ['atlas', 'png']:
                shutil.copy2(raw / (name + '.' + ext), assets / (name + '.' + ext))
            skel = assets / (name + '.skel')
            if not skel.exists():
                subprocess.run([str(CONVERTER), str(raw / (name + '.json')), str(skel), '-v', '4.2.11'], check=True, stdout=subprocess.DEVNULL)
            (assets / (name + '.tres')).write_text('[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]\n'
                f'[ext_resource type="SpineAtlasResource" path="res://ux_assets/{name}.atlas" id="1"]\n'
                f'[ext_resource type="SpineSkeletonFileResource" path="res://ux_assets/{name}.skel" id="2"]\n'
                '[resource]\natlas_res = ExtResource("1")\nskeleton_file_res = ExtResource("2")\n', encoding='utf-8')
            layers.append(dict(layer, at=start + float(layer.get('delay', 0))/1000, animation=anim, life=duration(data['animations'][anim])))
    (OUT / 'ux_layers.json').write_text(json.dumps(layers, ensure_ascii=False, indent=2), encoding='utf-8')
    report = dict(spine_layers=len(layers), missing=missing, omitted_particles=particles,
                  timing='CFX delays + command manifest; original SRMD not present in supplied mod',
                  limitations=['No target model / target hit effects', 'No original particle simulation', 'No game trigger or damage changes'])
    (OUT / 'report.json').write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
    (OUT / 'project.godot').write_text('config_version=5\n[application]\nconfig/name="Roswitha UG UX Preview"\n[rendering]\nrenderer/rendering_method="gl_compatibility"\ntextures/default_filters/use_nearest_mipmap_filter=false\n', encoding='utf-8')
    for mode in ['ug', 'ux']:
        (OUT / mode).mkdir(exist_ok=True)
    print(OUT)
    print(json.dumps(report, ensure_ascii=False))


if __name__ == '__main__':
    main()
