#!/usr/bin/env python3
"""Build a portable, searchable offline art book from completed native event images."""
import argparse
import html
import json
import shutil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--output', type=Path, default=Path('/workspace/artifacts/Arvito-Event-Art'))
args = parser.parse_args()
out = args.output
out.mkdir(parents=True, exist_ok=True)
(out/'images').mkdir(exist_ok=True)
plan = json.loads((root/'Content/EventArt/scenes.json').read_text())
source = root/'Unity/Arvito/Assets/Resources/Arvito/Art/Events'
cards = []
for event in plan['scenes']:
    image = source/event['image']
    if not image.exists(): continue
    shutil.copy2(image, out/'images'/image.name)
    title, place, eid = map(html.escape, (event['title'],event['place'],event['id']))
    summary = html.escape(' '.join(event['sourceText']))
    cards.append(f'<article data-search="{title} {place} {eid}"><a href="images/{eid}.png" target="_blank"><img loading="lazy" width="192" height="128" src="images/{eid}.png" alt="{title}"></a><div class="caption"><small>{place} · {eid}</small><h2>{title}</h2><p>{summary}</p></div></article>')
page = '''<!doctype html><html lang="ko"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>아르비토 · 이벤트 삽화집</title>
<style>*{box-sizing:border-box}body{margin:0;background:#202221;color:#e5dfd1;font:16px/1.6 system-ui,sans-serif}header{max-width:1240px;margin:auto;padding:44px 24px 24px}h1{font-size:32px;letter-spacing:.03em;margin:0 0 8px}header p{margin:0 0 24px;color:#b1b0a4}input{display:block;width:100%;padding:14px;background:#30332e;border:1px solid #606151;border-radius:2px;color:#eee7d7;font:inherit}#count{display:block;margin-top:12px;color:#b7b198}main{max-width:1280px;padding:0 24px 48px;margin:auto;display:grid;grid-template-columns:repeat(auto-fit,minmax(386px,1fr));gap:24px}article{background:#2a2c27;overflow:hidden;border:1px solid #414337}article[hidden]{display:none}article>a{display:flex;align-items:center;justify-content:center;background:#171b19;min-height:256px}img{display:block;width:384px;height:256px;image-rendering:pixelated;flex-shrink:0}.caption{padding:18px}small{color:#b3ac95;font-size:12px}h2{font-size:18px;margin:5px 0 12px}article p{color:#bbb9ac;font-size:14px;margin:0}a:focus-visible,input:focus-visible{outline:2px solid #c3ab77;outline-offset:2px}@media(max-width:434px){main{padding:0 12px 24px;grid-template-columns:1fr}img{width:192px;height:128px}article>a{min-height:160px}header{padding:24px 12px}}
</style><header><h1>아르비토 · 이벤트 삽화집</h1><p>192×128 실제 픽셀 · 그림당 최대 32색 · 파티 구성에 종속되지 않는 사건 중심 구도<br>그림을 누르면 게임용 원본 PNG가 열립니다.</p><label for="search">이벤트 이름·장소·ID 검색</label><input id="search" type="search" placeholder="예: 숲, 우물, q001"><output id="count"></output></header><main>'''+''.join(cards)+'''</main><script>const cards=[...document.querySelectorAll('article')],search=document.querySelector('#search'),count=document.querySelector('#count');function filter(){const q=search.value.toLocaleLowerCase().trim();let n=0;for(const card of cards){card.hidden=!card.dataset.search.toLocaleLowerCase().includes(q);if(!card.hidden)n++}count.textContent=n+' / '+cards.length+'개 장면'}search.addEventListener('input',filter);filter()</script></html>'''
(out/'index.html').write_text(page)
print(f'{len(cards)} scenes in {out}/index.html')
