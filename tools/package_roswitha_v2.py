"""Preserve RGBA frames; flatten only the separate battle-background preview."""
from pathlib import Path
import json,math,subprocess
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'tmp/roswitha_v2'

def matte_at(mode,t,return_at=7.9097):
    clamp=lambda x:max(0,min(1,x))
    if mode.endswith('ug'):return clamp((t-.10)/.10)*(1-clamp((t-1.766)/.20))
    return clamp((t-.65)/.12)*(1-clamp((t-return_at)/.25))

def main():
    configs=json.loads((OUT/'config.json').read_text(encoding='utf-8'))
    bg=Image.open(OUT/'preview_battle.png').convert('RGBA')
    metadata={}
    for mode,cfg in configs.items():
        frames=sorted((OUT/mode).glob('frame_*.png'))
        assert len(frames)==math.ceil(cfg['total']*30),(mode,len(frames))
        encoder=subprocess.Popen([r'E:/SOFT/ShareX/ffmpeg.exe','-y','-loglevel','error','-f','rawvideo','-pixel_format','rgb24','-video_size','1280x720','-framerate','30','-i','-','-i',str(OUT/(mode+'.wav')),'-c:v','libx264','-crf','17','-pix_fmt','yuv420p','-c:a','aac','-t',str(len(frames)/30),'-movflags','+faststart',str(OUT/(mode+'.mp4'))],stdin=subprocess.PIPE)
        for i,p in enumerate(frames):
            # Restore known blackout opacity in the capture's alpha channel;
            # Spine additive/multiply slots can otherwise alter viewport alpha.
            im=Image.open(p).convert('RGBA');a=np.array(im)
            a[:,:,3]=np.maximum(a[:,:,3],round(matte_at(mode,i/30,cfg["return_at"])*255))
            im=Image.fromarray(a);im.save(p,compress_level=1)
            encoder.stdin.write(Image.alpha_composite(bg,im).convert('RGB').tobytes())
        encoder.stdin.close()
        assert encoder.wait()==0,mode
        # Transparency at the end is a substantive requirement, not a video effect.
        with Image.open(frames[-1]) as final:assert final.getextrema()[3][0]==0,mode
        metadata[mode]=dict(frames=len(frames),fps=30,alpha=True,blackout_end=1.966 if mode.endswith('ug') else cfg['return_at']+.25)
    (OUT/'manifest.json').write_text(json.dumps(metadata,indent=2))
    (OUT/'preview.html').write_text('''<!doctype html><meta charset="utf-8"><title>罗丝薇瑟 原皮/皮肤 UG/UX v2</title>
<style>body{background:#18181d;color:#eee;font:16px system-ui;max-width:1280px;margin:24px auto}video{width:100%;background:#222}select,button{padding:8px;margin:10px}small{color:#bbb}</style>
<h2>罗丝薇瑟 · 四版本预览 v2</h2><select id="pick"><option value="base_ug">原皮 UG</option><option value="skin_ug">皮肤 UG</option><option value="base_ux">原皮 UX</option><option value="skin_ux">皮肤 UX</option></select>
<video id="movie" src="base_ug.mp4?v=webp3" controls></video>
<p>背景是静态战场参考，仅用于检查开场与收尾是否透回战场；不属于演出素材。</p>
<small>原皮/皮肤采用各自 SRMD 与 CFX。UX 开场已经分别采用原皮/皮肤的原始 1580×720 动画 WebP，46 帧、2.742 秒，按逐帧时长播放；不再共用片段或从参考录像取帧。PMA 使用 MeiLin 材质。粒子仍为近似。查看对应目录的 RGBA PNG 或 NativeProject 原生舞台获取透明演出。</small>
<script>pick.onchange=()=>{movie.src=pick.value+'.mp4?v=webp3';movie.play()}</script>''',encoding='utf-8')
    print(metadata)

if __name__=='__main__':main()
