"""Encode lossless source frames with sound and a synchronized reference viewer."""
import json, shutil, subprocess
import sys
sys.dont_write_bytecode = True
from pathlib import Path
from PIL import Image, ImageDraw
from build_roswitha_full import OUT, SRC

def main():
    config=json.loads((OUT/'config.json').read_text(encoding='utf-8'))
    manifest={}
    for mode,c in config.items():
        frames=sorted((OUT/mode).glob('frame_*.png'))
        expected=__import__('math').ceil(c['total']*30)
        if len(frames)!=expected:raise RuntimeError(f'{mode}: {len(frames)}/{expected}')
        subprocess.run([r'E:/SOFT/ShareX/ffmpeg.exe','-y','-loglevel','error','-framerate','30','-i',str(OUT/mode/'frame_%04d.png'),'-i',str(OUT/(mode+'.wav')),
            '-c:v','libx264','-crf','18','-pix_fmt','yuv420p','-c:a','aac','-b:a','192k','-af','apad','-shortest','-movflags','+faststart',str(OUT/(mode+'.mp4'))],check=True)
        selected=[frames[min(len(frames)-1,int(t*30))] for t in ([.3,.6,1.0,1.4,2,2.8,3.2,3.6] if mode=='ug' else [.4,1,2,2.8,3.5,4.5,5.5,6.5,7.2,8.2,9,10])]
        sheet=Image.new('RGB',(1280,180*((len(selected)+3)//4)));draw=ImageDraw.Draw(sheet)
        for i,p in enumerate(selected):
            with Image.open(p) as im:sheet.paste(im.resize((320,180)),(i%4*320,i//4*180))
            draw.text((i%4*320+5,i//4*180+5),f'{int(p.stem.split("_")[1])/30:.2f}s',fill='red')
        sheet.save(OUT/(mode+'_final.jpg'))
        manifest[mode]=dict(fps=30,frames=len(frames),size=Image.open(frames[0]).size,total=len(frames)/30)
    refs=list((SRC/'预览').glob('*.mp4'))
    for mode,ref in [('ug',next(p for p in refs if p.name.startswith('9DA'))),('ux',next(p for p in refs if not p.name.startswith('9DA')))]:shutil.copy2(ref,OUT/('reference_'+mode+'.mp4'))
    (OUT/'manifest.json').write_text(json.dumps(manifest,indent=2))
    (OUT/'preview.html').write_text('''<!doctype html><meta charset="utf-8"><title>罗丝薇瑟完整演出</title>
<style>body{background:#17171c;color:#eee;font:16px system-ui;margin:24px}main{display:flex;gap:12px}section{width:50%}video{width:100%;background:black}button,select,input{padding:8px;margin:10px}small{color:#aaa}</style>
<h2>罗丝薇瑟 · 皮肤版 UG / UX</h2><select id="mode"><option value="ug">UG · 神眷者</option><option value="ux">UX · 攻击</option></select>
<button id="play">同步播放</button><label>参考起点（秒）<input id="offset" type="number" step="0.05" value="0.7"></label>
<main><section>重新制作<video id="render" controls src="ug.mp4"></video></section><section>原始参考<video id="ref" muted controls src="reference_ug.mp4"></video></section></main>
<p><small>1280×720 · 30 FPS · 源 PNG 序列与音效。参考起点可调；参考录像包含原游戏战场与敌人，本演出文件使用独立黑底舞台。粒子为源参数近似采样。</small></p>
<script>const a=document.querySelector('#render'),b=document.querySelector('#ref'),mode=document.querySelector('#mode'),off=document.querySelector('#offset');
mode.onchange=()=>{a.pause();b.pause();a.src=mode.value+'.mp4?v=aspect2';b.src='reference_'+mode.value+'.mp4';off.value=mode.value==='ug'?0.7:6.4};
document.querySelector('#play').onclick=()=>{b.currentTime=a.currentTime+Number(off.value);a.play();b.play()};a.onpause=()=>b.pause();a.onseeking=()=>{b.currentTime=a.currentTime+Number(off.value)};
</script>''',encoding='utf-8')
    print(manifest)

if __name__=='__main__':main()
