#!/usr/bin/env python3
"""Validate current retouch pixels, palette, unchanged Unity GUIDs and review provenance.

Semantic/visual review is recorded by the author; this script verifies that those
records apply to the exact files being shipped. Pillow is only used for reading.
"""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

parser = argparse.ArgumentParser()
parser.add_argument('--partial', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
meta = root/'Content/EventArt/retouch'
runtime = root/'Unity/Arvito/Assets/Resources/Arvito/Art/Events'
plan = json.loads((meta/'plan.json').read_text())
catalog = json.loads((root/'Unity/Arvito/Assets/Resources/Arvito/catalog.json').read_text())
expected = {e['id'] for e in catalog['events']}
assert len(expected) == len(catalog['events']) == 500
assert set(plan['baseline']) == expected
planned = [eid for b in plan['batches'] for eid in b['ids']]
assert len(planned) == len(set(planned)) == 500 and set(planned) == expected
palette = {tuple(bytes.fromhex(h.lstrip('#'))) for h in plan['palette']}
digest = lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
records = {p.stem: json.loads(p.read_text()) for p in (meta/'records').glob('*.json')}
assert not records.keys()-expected
current = {eid: r for eid,r in records.items() if r.get('styleRevision') == plan['styleRevision']}
assert args.partial or current.keys() == expected, f'{500-len(current)} images lack current retouch records'
pixel_hashes = {}
for eid, record in current.items():
    assert record['id'] == eid and record['status'] == 'reviewed'
    image_path = runtime/(eid+'.png')
    assert digest(image_path) == record['afterSha256'], f'{eid}: image changed after visual review'
    assert record['beforeSha256'] == plan['baseline'][eid]['sha256']
    assert record['afterSha256'] != record['beforeSha256'], f'{eid}: old artwork was not replaced'
    assert digest(Path(str(image_path)+'.meta')) == plan['baseline'][eid]['metaSha256'], f'{eid}: Unity metadata changed'
    assert record.get('review','').strip(), f'{eid}: empty visual review'
    batch = json.loads((meta/'batches'/f"{record['batch']}.json").read_text())
    assert batch['status'] == 'reviewed' and batch['styleRevision'] == plan['styleRevision']
    assert batch['events'][record['cell']] == eid
    assert batch['sourceSha256'] == record['sourceSha256']
    assert digest(meta/'prompts'/f"{record['batch']}.txt") == record['promptSha256'] == batch['promptSha256']
    with Image.open(image_path) as image:
        assert image.format == 'PNG' and image.size == (192,128)
        rgba = image.convert('RGBA')
        assert rgba.getextrema()[3] == (255,255), f'{eid}: transparent pixels'
        colors = {rgb for _,rgb in image.convert('RGB').getcolors(192*128)}
        assert 2 <= len(colors) <= plan['maxColors'] and colors <= palette, f'{eid}: inconsistent palette'
        h = hashlib.sha256(rgba.tobytes()).hexdigest()
        assert h not in pixel_hashes, f'{eid}: duplicate of {pixel_hashes.get(h)}'
        pixel_hashes[h] = eid
repairs = json.loads((meta/'repairs.json').read_text())
pending = [r['id'] for r in repairs if r.get('status') != 'resolved']
for repair in repairs:
    if repair.get('status') == 'resolved':
        assert repair['resolvedSha256'] == records[repair['id']]['afterSha256'], f"{repair['id']}: stale repair review"
assert args.partial or not pending, f'Unresolved visual corrections: {pending}'
print(f'PASS: {len(current)}/500 current retouches; 192x128 opaque; shared {len(palette)}-color parchment palette; exact file/review hashes and unchanged Unity metadata.')
if pending: print('IN PROGRESS: visual corrections pending: '+', '.join(pending))
if not args.partial: print('PASS: all 500 catalog events retouched and visually reviewed; no pending corrections.')
