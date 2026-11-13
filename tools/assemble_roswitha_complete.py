"""Merge verified deliveries into one archive; latest files win, no old ZIP needed."""
from pathlib import Path
import hashlib
import json
import shutil
import zipfile

ROOT=Path(__file__).resolve().parents[1]
TMP=ROOT/'tmp'
OUTPUT=TMP/'Roswitha_UG_UX_Complete_2026-10-30.zip'

def main():
    sources=[
        TMP/'Roswitha_UG_UX_Integration_2026-10-29.zip',
        TMP/'Roswitha_UG_UX_v2_Corrections.zip',
        TMP/'Roswitha_UX_WebP_Update.zip',
    ]
    archives=[zipfile.ZipFile(p) for p in sources]
    try:
        selected={}
        for index,archive in enumerate(archives):
            for info in archive.infolist():
                if info.is_dir():continue
                rel=info.filename.split('/',1)[1]
                if index==0 and not rel.startswith('sources/'):continue
                if any(x in rel for x in ['SHA256SUMS.json','__pycache__/','.godot/']):continue
                if rel in ['cutin_hd_mapping.json','tools/recover_roswitha_hd_cutin.py','tools/roswitha_cutin_hd_mapping.json']:continue
                # The current config only loads cutin/base and cutin/skin.
                if rel.startswith('NativeProject/RoswithaV2/cutin/') and len(rel.split('/'))==4:continue
                selected[rel]=(archive,info)
        cfg=json.loads(archives[2].read('Roswitha_UG_UX_v2/NativeProject/RoswithaV2/config.json'))
        for mode,count in [('base_ug',121),('skin_ug',121),('base_ux',356),('skin_ux',356)]:
            assert sum(p.startswith('RGBA/'+mode+'/frame_') and p.endswith('.png') for p in selected)==count,mode
            assert 'Preview/'+mode+'.mp4' in selected
            assert len(cfg[mode]['layers'])>0
        assert cfg['base_ux']['cutin_source']!=cfg['skin_ux']['cutin_source']
        readme=(ROOT/'docs/roswitha-ugux-v2.md').read_text(encoding='utf-8')
        readme=readme.replace('旧完整包里的原始素材 sources/ 可继续用于重建；v2 修正包不再次复制全部原始声音和未使用素材。','本整合包已经包含原始素材 sources/ 和最新 OriginalWebP/，无需另外下载旧包或叠加更新包。')
        readme=readme.replace('## 使用旧包原始素材重建 v2','## 从本整合包重建')
        readme=readme.replace('仅在要修改源配方时才需要旧完整包的 sources。','原始素材也已放在本包 sources/ 中。')
        readme=readme.replace("$env:ROSWITHA_SOURCE = '旧完整包解压目录/sources/ux与ug'","$env:ROSWITHA_SOURCE = Join-Path (Get-Location) 'sources/ux与ug'")
        readme=readme.replace('完整原始图片保留在旧包；','原始图片保留在本包 sources/；').replace('无需旧包即可运行烟雾测试','无需其他包即可运行烟雾测试')
        marker='## WebP 更新包'
        if marker in readme:readme=readme[:readme.index(marker)]
        readme='# UG / UX 四版本整合包（2026-10-30）\n\n包含原皮 UG、皮肤 UG、原皮 UX、皮肤 UX，以及两套原始 WebP 开场。解压即包含全部交付内容，无需叠加以前的 ZIP。游戏侧仍需按下文接入，不能把 ZIP 直接当作已安装 Mod。\n\n'+readme
        readme+='\n## 整合包校验\n\nSHA256SUMS.json 重新覆盖所有文件。来源按原始素材包、v2、WebP 更新包的顺序合并，后者覆盖前者。浏览器打开 Preview/preview.html；运行时使用 NativeProject；透明帧在 RGBA/。\n'
        (ROOT/'docs/roswitha-complete-package.md').write_text(readme,encoding='utf-8')
        manifest={}
        prefix='Roswitha_UG_UX_Complete/'
        with zipfile.ZipFile(OUTPUT,'w',zipfile.ZIP_DEFLATED,compresslevel=3) as target:
            for rel,(archive,info) in sorted(selected.items()):
                if rel=='README.md':continue
                digest=hashlib.sha256();size=0
                with archive.open(info) as src,target.open(prefix+rel,'w',force_zip64=True) as dst:
                    while True:
                        block=src.read(1024*1024)
                        if not block:break
                        dst.write(block);digest.update(block);size+=len(block)
                manifest[rel]=dict(bytes=size,sha256=digest.hexdigest())
            encoded=readme.encode('utf-8');target.writestr(prefix+'README.md',encoded)
            manifest['README.md']=dict(bytes=len(encoded),sha256=hashlib.sha256(encoded).hexdigest())
            target.writestr(prefix+'SHA256SUMS.json',json.dumps(manifest,ensure_ascii=False,indent=2).encode('utf-8'))
        with zipfile.ZipFile(OUTPUT) as archive:assert archive.testzip() is None
        print(json.dumps(dict(path=str(OUTPUT),files=len(manifest)+1,MiB=round(OUTPUT.stat().st_size/2**20,1),sha256=hashlib.file_digest(OUTPUT.open('rb'),'sha256').hexdigest())))
    finally:
        for archive in archives:archive.close()

if __name__=='__main__':main()
