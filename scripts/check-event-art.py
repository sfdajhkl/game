#!/usr/bin/env python3
"""Check complete, distinct event coverage and actual native PNG pixels (requires Pillow)."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--partial', action='store_true', help='Report unfinished scenes during authoring')
args = parser.parse_args()
plan = json.loads((root/'Content/EventArt/scenes.json').read_text())
events = json.loads((root/'Unity/Arvito/Assets/Resources/Arvito/catalog.json').read_text())['events']
out = root/'Unity/Arvito/Assets/Resources/Arvito/Art/Events'
expected = {e['id'] for e in events}
planned = {e['id'] for e in plan['scenes']}
assert len(expected) == len(events) == len(planned) == len(plan['scenes']) == 500
assert expected == planned, 'Scene plan differs from catalog'
actual = {p.stem for p in out.glob('*.png')}
assert not actual - expected, f'Unexpected images: {actual - expected}'
missing = expected - actual
assert args.partial or not missing, f'Missing {len(missing)} scenes: {sorted(missing)}'
hashes = {}
colors = []
for eid in sorted(actual):
    file = out/f'{eid}.png'
    with Image.open(file) as image:
        assert image.format == 'PNG' and image.size == (192,128), f'{eid}: incorrect native size'
        image = image.convert('RGBA')
        assert image.getextrema()[3] == (255,255), f'{eid}: unintended transparent pixels'
        count = len(image.getcolors(192*128))
        assert 2 <= count <= 32, f'{eid}: {count} colors'
        colors.append(count)
        digest = hashlib.sha256(image.tobytes()).hexdigest()
        assert digest not in hashes, f'Duplicate illustration: {eid} and {hashes.get(digest)}'
        hashes[digest] = eid
    if not args.partial:
        assert Path(str(file)+'.meta').exists(), f'{eid}: missing Unity metadata'
print(f'PASS: {len(actual)}/500 distinct illustrations; 192x128 opaque pixels; {min(colors)}-{max(colors)} colors each.')
if missing: print(f'IN PROGRESS: {len(missing)} scenes still need illustrations.')
else: print('PASS: every catalog event has a matching image, scene brief and Unity metadata.')

if not args.partial and (root/'Content/EventArt/retouch/plan.json').exists():
    import subprocess
    import sys
    subprocess.run([sys.executable, str(root/'scripts/check-event-retouch.py')], check=True)
elif not args.partial:
    sources = json.loads((root/'Content/EventArt/sources.json').read_text())
    reviews = json.loads((root/'Content/EventArt/review.json').read_text())
    repairs = json.loads((root/'Content/EventArt/repairs.json').read_text())
    for sheet in range(1, 33):
        key = str(sheet)
        expected_order = [e['id'] for e in plan['scenes'] if e.get('sheet') == sheet]
        assert key in sources, f'Sheet {key}: missing source provenance'
        assert sources[key]['events'] == expected_order, f'Sheet {key}: incorrect scene order'
    for key, source in sources.items():
        review = reviews.get(key, {})
        assert review.get('sourceSha256') == source['sha256'], f'Sheet {key}: stale or absent visual review'
        assert review.get('status') in ('reviewed', 'reviewed-with-repairs'), f'Sheet {key}: unresolved visual corrections'
    repaired = {eid for key, source in sources.items() if key.startswith('repair-') for eid in source['events']}
    pending = {e['id'] for e in repairs} - repaired
    assert not pending, f'Queued repairs not imported: {sorted(pending)}'
    print(f'PASS: all 32 source sheets reviewed; {len(repaired)} scene corrections imported and reviewed.')
