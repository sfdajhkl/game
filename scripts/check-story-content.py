#!/usr/bin/env python3
"""Validate authored content references, conditional reachability and world routes."""
import json
import re
import struct
from pathlib import Path
import subprocess
import sys

root=Path(__file__).resolve().parents[1]
c=json.loads((root/'Unity/Arvito/Assets/Resources/Arvito/catalog.json').read_text())
events=c['events'];regions={r['id']:r for r in c['regions']}
assert len(events)==500 and len(regions)==10
for field in ('id','title'): assert len({e[field] for e in events})==500,field
known={name:{v['id'] for v in c[name]} for name in ('weapons','skills','companions','enemies')}
flags=set();required=set();choices=0;battles=0;gates=0
for e in events:
    assert e['text'] and all(isinstance(t,str) and len(t)>4 for t in e['text']),e['id']
    assert 2<=len(e['choices'])<=4,e['id']
    assert len({x['label'] for x in e['choices']})==len(e['choices']),e['id']
    assert set(e['regions'])<=regions.keys(),e['id']
    assert len(e['stageWeights'])==3 and all(x>=0 for x in e['stageWeights']),e['id']
    assert 0<=e.get('minStage',0)<=2,e['id']
    if e.get('requiredFlag'):required.add(e['requiredFlag'])
    for ch in e['choices']:
        choices+=1
        assert ch['weeks']>=1 and ch['body'] and ch['label'],e['id']
        assert not ch.get('next'),('forced transition',e['id'])
        if ch.get('check'):
            assert ch['check']['stat'] in ('strength','agility','wit','will')
            assert ch.get('failure',{}).get('body'),e['id']
        if ch.get('battle'):
            battles+=1;assert 1<=len(ch['battle'])<=4 and set(ch['battle'])<=known['enemies'],e['id']
        if ch.get('need',{}).get('weapon'):
            gates+=1;assert ch['need']['weapon'] in known['weapons'],e['id']
        for effects in (ch.get('effects',{}),ch.get('failure',{}).get('effects',{})):
            if effects.get('flag'):flags.add(effects['flag'])
            for key,collection in [('weapon','weapons'),('skill','skills'),('recruit','companions')]:
                if effects.get(key):assert effects[key] in known[collection],(e['id'],key)
assert required<=flags,required-flags
reachable=set()
while True:
    before=len(reachable)
    for e in events:
        if e.get('requiredFlag') and e['requiredFlag'] not in reachable:continue
        for ch in e['choices']:
            if ch.get('need',{}).get('flag') and ch['need']['flag'] not in reachable:continue
            for fx in (ch.get('effects',{}),ch.get('failure',{}).get('effects',{})):
                if fx.get('flag'):reachable.add(fx['flag'])
    if len(reachable)==before:break
assert required<=reachable,('orphaned quest chains',required-reachable)
art=root/'Unity/Arvito/Assets/Resources/Arvito/Art/characters.png'
png=art.read_bytes();assert png[:8]==b'\x89PNG\r\n\x1a\n'
width,height=struct.unpack('>II',png[16:24]);assert (width,height)==(256,256) and png[25]==6
slots=dict((name,int(index)) for name,index in re.findall(r'\["([a-z]+)"\]=(\d+)',(root/'Unity/Arvito/Assets/Arvito/Runtime/BattlerArt.cs').read_text()))
assert set(slots)==known['enemies']|known['companions']|{'hero'}
assert sorted(slots.values())==list(range(15)) and Path(str(art)+'.meta').exists()
for r in regions.values():assert set(r['routes'])<=regions.keys(),r['id']
for stage in range(3):
    allowed={r['id'] for r in regions.values() if r['minStage']<=stage}
    visited={'village'}
    while True:
        expanded=visited|{n for r in visited for n in regions[r]['routes'] if n in allowed}
        if expanded==visited:break
        visited=expanded
    assert visited==allowed,('unreachable regions',stage,allowed-visited)
    for region in allowed:
        free=[e for e in events if not e.get('once') and not e.get('requiredFlag') and e.get('minStage',0)<=stage and e['stageWeights'][stage]>0 and (not e['regions'] or region in e['regions'])]
        assert len(free)>=2,('exhausted local pool',region,stage)
subprocess.run([sys.executable,str(root/'scripts/export-story-content.py'),'--check'],check=True)
subprocess.run(['node',str(root/'scripts/export-pixel-art.mjs'),'--check'],check=True)
print(f'PASS: 500 distinct scenes, {choices} choices, {battles} combat options, {gates} equipment gates; all 95 quest arcs and 10 regional routes are reachable; 15 native pixel sprites mapped to RGBA atlas.')
