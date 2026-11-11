#!/usr/bin/env python3
"""Build a portable searchable gallery of reviewed native event illustrations."""
import argparse
import hashlib
import html
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=Path('/workspace/artifacts/Arvito-Event-Art'))
parser.add_argument('--before', type=Path, help='Original native PNG directory for comparison')
parser.add_argument('--partial', action='store_true', help='Include only reviewed current retouches')
args = parser.parse_args()
out = args.output
out.mkdir(parents=True, exist_ok=True)
(out / 'images').mkdir(exist_ok=True)
if args.before:
    (out / 'before').mkdir(exist_ok=True)
scenes = json.loads((root / 'Content/EventArt/scenes.json').read_text())['scenes']
source = root / 'Unity/Arvito/Assets/Resources/Arvito/Art/Events'
meta = root / 'Content/EventArt/retouch'
plan = json.loads((meta / 'plan.json').read_text())
pending = {r['id'] for r in json.loads((meta / 'repairs.json').read_text()) if r['status'] != 'resolved'}
cards, included, places = [], [], set()
for event in scenes:
    eid = event['id']
    rp = meta / 'records' / (eid + '.json')
    record = json.loads(rp.read_text()) if rp.exists() else {}
    ready = record.get('status') == 'reviewed' and record.get('styleRevision') == plan['styleRevision'] and eid not in pending
    if not ready:
        if args.partial:
            continue
        raise SystemExit(f'Unreviewed current artwork: {eid}. Use --partial for an in-progress gallery.')
    image = source / event['image']
    if hashlib.sha256(image.read_bytes()).hexdigest() != record['afterSha256']:
        raise SystemExit(f'Reviewed image changed: {eid}')
    shutil.copy2(image, out / 'images' / image.name)
    title, place, escaped_id = map(html.escape, (event['title'], event['place'], eid))
    summary = html.escape(' '.join(event['sourceText']))
    before = ''
    if args.before:
        original = args.before / image.name
        if hashlib.sha256(original.read_bytes()).hexdigest() != plan['baseline'][eid]['sha256']:
            raise SystemExit(f'Original image does not match baseline: {eid}')
        shutil.copy2(original, out / 'before' / image.name)
        before = f'<figure class="before"><figcaption>수정 전</figcaption><a href="before/{escaped_id}.png" target="_blank" rel="noopener"><img loading="lazy" width="192" height="128" src="before/{escaped_id}.png" alt="{title} 수정 전"></a></figure>'
    cards.append(f'<article data-search="{title} {place} {escaped_id}" data-place="{place}"><div class="pictures">{before}<figure class="after"><figcaption>수정 후</figcaption><a href="images/{escaped_id}.png" target="_blank" rel="noopener"><img loading="lazy" width="192" height="128" src="images/{escaped_id}.png" alt="{title} 수정 후"></a></figure></div><div class="caption"><small>{place} · {escaped_id}</small><h2>{title}</h2><p>{summary}</p></div></article>')
    included.append(eid)
    places.add(event['place'])
options = ''.join(f'<option>{html.escape(p)}</option>' for p in sorted(places))
button = '<button id="compare" type="button" aria-pressed="false">전후 비교 보기</button>' if args.before else ''
progress = f'작업 중 · 전체 {len(scenes)}장 중 검토 완료된 {len(cards)}장만 표시합니다.' if args.partial else f'{len(cards)}개 사건의 검토 완료본'
page = '''<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>아르비토 · 이벤트 삽화집</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#3b2a20;color:#3b2a20;font:16px/1.65 system-ui,sans-serif}header{max-width:1360px;margin:auto;padding:40px 24px 26px;color:#d5be96}h1{font-size:30px;margin:0 0 8px}header p{margin:0 0 20px}.controls{display:flex;gap:16px;align-items:end;flex-wrap:wrap}.search{flex:1;min-width:210px}label{display:block;font-size:14px;margin-bottom:5px}input,select,button{font:inherit;color:#3b2a20;background:#d5be96;border:2px solid #af8e62;padding:10px 12px;min-height:48px;border-radius:2px}input{width:100%}button{cursor:pointer}button[aria-pressed=true]{color:#d5be96;background:#79573a}#count{display:block;margin-top:16px}main{max-width:1360px;padding:0 24px 48px;margin:auto;display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,426px),1fr));gap:24px}article{background:#d5be96;border:2px solid #af8e62;overflow:hidden}article[hidden]{display:none}.pictures{display:flex;justify-content:center;align-items:start;flex-wrap:wrap;gap:16px;padding:16px 10px;background:#af8e62}figure{margin:0}figcaption{font-size:12px;margin-bottom:8px}figure.before,figcaption{display:none}img{display:block;width:384px;height:256px;image-rendering:pixelated;max-width:none}.comparing figure.before,.comparing figcaption{display:block}.comparing img{width:192px;height:128px}.caption{padding:18px}small{color:#79573a;font-size:12px}h2{font-size:18px;margin:5px 0 12px}article p{font-size:14px;margin:0}a:focus-visible,input:focus-visible,select:focus-visible,button:focus-visible{outline:3px solid #d5be96;outline-offset:4px}.pictures a:focus-visible{outline-color:#3b2a20}#empty{color:#d5be96;text-align:center;padding:30px}#empty[hidden]{display:none}@media(max-width:455px){header{padding:24px 14px}main{padding:0 14px 30px;gap:18px}h1{font-size:25px}img{width:192px;height:128px}.controls>*{width:100%}.search{min-width:0}.comparing .pictures{flex-direction:column;align-items:center}}
</style></head><body><header><h1>아르비토 · 이벤트 삽화집</h1><p>192×128 실제 픽셀 · 공통 양피지 4색 · 재질별 두 단계 명암 · 외곽선 유지<br>주인공의 파티 구성을 고정하지 않는 사건 중심 삽화입니다. 그림을 누르면 원본 PNG가 열립니다.</p><p>__PROGRESS__</p><div class="controls"><div class="search"><label for="search">이벤트 이름·장소·ID 검색</label><input id="search" type="search" placeholder="예: 우물, q001"></div><div><label for="place">장소</label><select id="place"><option value="">모든 장소</option>__OPTIONS__</select></div>__BUTTON__</div><output id="count" aria-live="polite"></output></header><main>__CARDS__</main><p id="empty" hidden>조건에 맞는 장면이 없습니다.</p><script>
const cards=[...document.querySelectorAll('article')],search=document.querySelector('#search'),place=document.querySelector('#place'),count=document.querySelector('#count'),empty=document.querySelector('#empty');
function filter(){const q=search.value.toLocaleLowerCase().trim();let n=0;for(const card of cards){card.hidden=!card.dataset.search.toLocaleLowerCase().includes(q)||(place.value!==''&&card.dataset.place!==place.value);if(!card.hidden)n++}count.textContent=n+' / '+cards.length+'개 장면';empty.hidden=n!==0}
search.addEventListener('input',filter);place.addEventListener('change',filter);document.querySelector('#compare')?.addEventListener('click',event=>{const on=document.body.classList.toggle('comparing');event.currentTarget.setAttribute('aria-pressed',String(on));event.currentTarget.textContent=on?'수정 후만 보기':'전후 비교 보기'});filter();
</script></body></html>'''
for key, value in {'PROGRESS': html.escape(progress), 'OPTIONS': options, 'BUTTON': button, 'CARDS': ''.join(cards)}.items():
    page = page.replace('__' + key + '__', value)
(out / 'index.html').write_text(page, encoding='utf-8')
(out / 'gallery.json').write_text(json.dumps({'styleRevision': plan['styleRevision'], 'partial': args.partial, 'total': len(scenes), 'included': included, 'comparison': bool(args.before)}, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
(out / 'README.txt').write_text(
    '아르비토 — 이벤트 삽화집\n\n'
    'ZIP을 풀고 index.html을 열면 이벤트 이름·장소·ID로 검색할 수 있습니다.\n'
    + ('전후 비교 보기 버튼으로 원본과 리터칭 결과를 나란히 볼 수 있습니다.\n' if args.before else '')
    + f'{progress}\n\n'
    'images/에는 게임에 적용한 192×128 PNG가 있습니다.\n'
    + ('before/에는 비교용 기존 PNG가 있습니다.\n' if args.before else '')
    + '현재 그림은 양피지 4색, 재질별 두 단계 명암과 절제된 외곽선을 사용합니다.\n'
    '삽화는 아르비토를 위해 생성·수정한 자료입니다.\n'
    '확대할 때는 보간 없이 정수 배율로 표시합니다.\n'
    '이 삽화집은 감상·비교 자료이며 게임 실행 파일이 아닙니다.\n',
    encoding='utf-8')
print(f'{len(cards)} reviewed scenes in {out}/index.html')
