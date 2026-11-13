"""Recover higher-resolution cutin frames from the user's reference; no invented detail."""
import argparse
import json
from pathlib import Path
import cv2

def recover(reference, lowres, output, mapping):
    output.mkdir(parents=True, exist_ok=True)
    ref = cv2.VideoCapture(str(reference))
    source = cv2.VideoCapture(str(lowres))
    for entry in json.loads(mapping.read_text(encoding='utf-8')):
        i = entry['frame']
        if entry['correlation'] >= .9:
            ref.set(cv2.CAP_PROP_POS_MSEC, entry['reference_time']*1000)
            ok, frame = ref.read()
        else:
            source.set(cv2.CAP_PROP_POS_FRAMES, i)
            ok, frame = source.read()
            if ok:
                h,w=frame.shape[:2]; width=int(h*16/9)
                frame=frame[:,(w-width)//2:(w+width)//2]
        if not ok: raise RuntimeError(f'Cannot decode cutin frame {i}')
        cv2.imwrite(str(output/f'{i:03d}.png'),cv2.resize(frame,(1280,720),interpolation=cv2.INTER_AREA))
    ref.release(); source.release()

if __name__=='__main__':
    p=argparse.ArgumentParser()
    p.add_argument('--reference',type=Path,required=True)
    p.add_argument('--lowres',type=Path,required=True)
    p.add_argument('--output',type=Path,required=True)
    a=p.parse_args()
    recover(a.reference,a.lowres,a.output,Path(__file__).with_name('roswitha_cutin_hd_mapping.json'))
