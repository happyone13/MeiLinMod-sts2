"""Offline skin cinematic reconstruction. Source mod is read-only."""
import json, plistlib, shutil, subprocess, hashlib
import sys
sys.dont_write_bytecode = True
import cv2
from pathlib import Path
from PIL import Image
from generate_ug_presentations import events_for, duration

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT.parent / '罗丝薇瑟皮肤mod/ux与ug'
OUT = ROOT / 'tmp/roswitha_v2'
CONVERTER = ROOT / 'MeiLinMod/spine/SpineSkeletonDataConverter.exe'
FILES = {p.name:p for p in SRC.rglob('*') if p.is_file() and '_node' not in p.parts and '_camera' not in p.parts and '预览' not in p.parts}

def num(v, default=0):
    return float(v) if v not in (None,'') else default

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    shutil.copytree(SRC.parent/'project/bin', OUT/'bin', dirs_exist_ok=True)
    shutil.copy2(ROOT/'tools/roswitha_v2_stage.gd', OUT/'stage.gd')
    shutil.copy2(ROOT/'tools/roswitha_particle_preview.gd', OUT/'particles.gd')
    char_dir=Path('ArtWorks/roswitha/character')
    shutil.copytree(SRC.parent/'project'/char_dir, OUT/char_dir, dirs_exist_ok=True)
    cutins={}
    cutin_sources={'base':'8f82e4fc26e7323de2ced17adf06c8ba.webp','skin':'704681867cdb2f971503baa7ccf79b32.webp'}
    for variant,filename in cutin_sources.items():
        folder=OUT/'cutin'/variant;folder.mkdir(parents=True,exist_ok=True)
        webp_path=SRC/'wbep'/filename
        if not webp_path.exists(): webp_path=ROOT/'OriginalWebP'/filename
        image=Image.open(webp_path);frames=[];end=0.0
        for index in range(image.n_frames):
            image.seek(index)
            rgba=image.convert('RGBA')
            ms=image.info.get('duration',0)
            if ms<=0: raise ValueError(f'Missing WebP duration {filename}:{index}')
            end+=ms/1000.0
            relative=f'cutin/{variant}/{index:03d}.png'
            rgba.save(OUT/relative)
            frames.append(dict(path=relative,duration=ms/1000.0,end=round(end,6)))
        cutins[variant]=dict(source=filename,frames=frames,total=round(end,6),size=image.size)
    shutil.copy2(ROOT / 'MeiLinMod/shaders/spine_pma.gdshader', OUT / 'spine_pma.gdshader')
    (OUT/'assets').mkdir(exist_ok=True)
    converted = set()
    def spine(name):
        if name in converted: return
        p = FILES[name+'.json']; data = json.loads(p.read_text())
        for ext in ['png','atlas']:
            shutil.copy2(FILES[name+'.'+ext], OUT/'assets'/(name+'.'+ext))
        dest = OUT/'assets'/(name+'.skel')
        if not dest.exists() or dest.stat().st_mtime < p.stat().st_mtime:
            subprocess.run([str(CONVERTER),str(p),str(dest),'-v','4.2.11'],check=True,stdout=subprocess.DEVNULL)
        (OUT/'assets'/(name+'.tres')).write_text('[gd_resource type="SpineSkeletonDataResource" load_steps=3 format=3]\n'
            f'[ext_resource type="SpineAtlasResource" path="res://assets/{name}.atlas" id="1"]\n'
            f'[ext_resource type="SpineSkeletonFileResource" path="res://assets/{name}.skel" id="2"]\n'
            '[resource]\natlas_res=ExtResource("1")\nskeleton_file_res=ExtResource("2")\ndefault_mix=0.0\n')
        # Source portrait textures are PMA; retain encoding and fade RGB with alpha.
        modes[name] = 'pma'
        imported=OUT/'assets'/(name+'.png.import')
        if modes[name]=='pma' and imported.exists():
            imported.write_text(imported.read_text().replace('process/fix_alpha_border=true','process/fix_alpha_border=false'))
        converted.add(name)
    modes={}
    def composite(name, at, life=None, group='main', offset=(0,0), scale=1):
        result=[]
        for spec in plistlib.loads(FILES[name+'.cfx'].read_bytes())['primitive']:
            s=dict(spec); n=s['source']; s['at']=at+num(s.get('delay'))/1000
            s['group']=group;s['offset']=offset;s['event_scale']=scale
            if s['format']=='spine':
                spine(n);d=json.loads(FILES[n+'.json'].read_text());ani=s.get('ani') or 'animation'
                s['animation']=ani.split(',')[0];s['next_animation']=ani.split(',')[1] if ',' in ani else ''
                s['life']=life if life is not None else duration(d['animations'][s['animation']])
                s['intro_duration']=duration(d['animations'][s['animation']])
                s['blend']=modes[n]
            else:
                s['emitters']=plistlib.loads(FILES[n+'.particle'].read_bytes()).get('emitters',[])
                s['life']=life if life is not None else (num(s.get('lifeTime'),1200)/1000)
                for e in s['emitters']:
                    tex=Path(e.get('textureFileName','')).name.replace('.sct','.png')
                    if tex not in FILES: raise FileNotFoundError(tex)
                    shutil.copy2(FILES[tex],OUT/'assets'/tex);e['texture']=tex
            result.append(s)
        return result
    configs={}
    for variant in ['base','skin']:
      commands=json.loads((SRC/'模型数据'/('30134.srmd' if variant=='base' else '30134_02.srmd')).read_text(encoding='utf-8-sig'))['command']
      for attack,command in [('ug','ug_buff'),('ux','ux_attack')]:
        mode=variant+'_'+attack
        events=events_for(commands[command]);layers=[]
        pause=cutins[variant]['total'] if attack=='ux' else 0
        for e in events:
            if e.get('etty')!='EFFECT': continue
            at=e['at']+(pause if attack=='ux' and e['at']>=.776 else 0)
            group='target' if e.get('type')=='TARGET' else 'main'
            layers+=composite(e['file_name'],at,group=group,scale=e.get('scale',1))
        total=max(s['at']+s['life'] for s in layers)
        configs[mode]=dict(events=events,layers=layers,total=total,pause=pause,design=[1280,720],cutin_frames=cutins[variant]['frames'] if attack=='ux' else [],cutin_source=cutins[variant]['source'] if attack=='ux' else None,return_at=5.5097+pause if attack=='ux' else 1.766,attack=attack,variant=variant)
        (OUT/mode).mkdir(exist_ok=True)
    (OUT/'config.json').write_text(json.dumps(configs,ensure_ascii=False,indent=2),encoding='utf-8')
    (OUT/'project.godot').write_text('config_version=5\n[application]\nconfig/name="Roswitha Full Cinematics"\n[display]\nwindow/size/viewport_width=1280\nwindow/size/viewport_height=720\nwindow/size/window_width_override=1280\nwindow/size/window_height_override=720\n[rendering]\nrenderer/rendering_method="gl_compatibility"\n')
    for variant in ['base','skin']:
      for attack,stem in [('ug','se_30134_ug_buff'),('ux','se_30134_ux_attack')]:shutil.copy2(FILES[stem+'.wav'],OUT/(variant+'_'+attack+'.wav'))
    print('Converted',len(converted),'spines; totals', {k:v['total'] for k,v in configs.items()})

if __name__=='__main__': main()
