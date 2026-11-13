"""Package the baked PNGs as GIF previews and a local sequence player."""
import json
from pathlib import Path
from PIL import Image

out = Path(__file__).resolve().parents[1] / 'tmp/roswitha_cinematics'
manifest = {}
for mode, expected in [('ug', 135), ('ux', 251)]:
    files = sorted((out / mode).glob('frame_*.png'))
    if len(files) != expected:
        raise RuntimeError(f'{mode}: {len(files)}/{expected} frames; bake still running?')
    manifest[mode] = dict(fps=30, count=len(files), duration=len(files)/30,
                          size=Image.open(files[0]).size,
                          frames=[f'{mode}/{p.name}' for p in files])
    frames = [Image.open(p).convert('RGB').resize((640, 360)) for p in files[::2]]
    frames[0].save(out / (mode + '.gif'), save_all=True, append_images=frames[1:], duration=67, loop=0)
(out / 'frames.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
html = '''<!doctype html><meta charset="utf-8"><title>罗丝薇瑟 UG / UX 序列帧预览</title>
<style>body{background:#15151b;color:#eee;font:16px system-ui;margin:30px auto;max-width:1152px}canvas{width:100%;background:#000}button,select,input{margin:12px 6px;padding:8px}input{width:55%}small{color:#aaa}</style>
<h2>罗丝薇瑟 UG / UX 序列帧预览</h2><canvas width="1152" height="648"></canvas>
<select><option value="ug">UG · 变身</option><option value="ux">UX · 攻击</option></select><button>暂停</button><input type="range" min="0" value="0"><span></span>
<p><small>30 FPS PNG 序列；UX 为 CFX 组合预览，未加入目标模型、原版粒子及音频。时间依据归集清单，尚未核对原始 SRMD。</small></p>
<script>const data=MANIFEST,canvas=document.querySelector('canvas'),ctx=canvas.getContext('2d'),select=document.querySelector('select'),button=document.querySelector('button'),slider=document.querySelector('input'),label=document.querySelector('span');
let mode='ug',images=[],playing=true,epoch=performance.now(),frame=0,generation=0;
function load(){const id=++generation;images=data[mode].frames.map(src=>{const im=new Image();im.src=src;return im});slider.max=images.length-1;epoch=performance.now();frame=0}
select.onchange=()=>{mode=select.value;load()};button.onclick=()=>{playing=!playing;button.textContent=playing?'暂停':'播放';epoch=performance.now()-frame/30*1000};slider.oninput=()=>{frame=+slider.value;epoch=performance.now()-frame/30*1000};
function tick(t){if(playing)frame=Math.floor((t-epoch)/1000*30)%images.length;const im=images[frame];if(im.complete&&im.naturalWidth){ctx.clearRect(0,0,canvas.width,canvas.height);ctx.drawImage(im,0,0,canvas.width,canvas.height)}slider.value=frame;label.textContent=(frame/30).toFixed(2)+' / '+(images.length/30).toFixed(2)+' 秒';requestAnimationFrame(tick)}load();requestAnimationFrame(tick);
</script>'''.replace('MANIFEST', json.dumps(manifest))
(out / 'preview.html').write_text(html, encoding='utf-8')
print(json.dumps(manifest, default=str)[:180])
