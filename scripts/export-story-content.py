#!/usr/bin/env python3
"""Export the authored quest scenes and original events into Unity's runtime catalog."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
catalog = json.loads((ROOT / 'Content/base-catalog.json').read_text())
REGIONS = [
    ('village', '브린 마을', '익숙한 문턱에도 남의 사정은 숨어 있다.', 'village', 0, ['forest','road','river']),
    ('forest', '이끼 숲', '나무 사이에서는 약속도 길을 잃는다.', 'forest', 0, ['village','road','swamp']),
    ('road', '세 갈래 교역로', '서로 다른 목적지가 잠시 같은 길을 쓴다.', 'village', 0, ['village','forest','coast','mountain','citadel']),
    ('river', '은빛 강 유역', '강물은 물건과 소문을 같은 속도로 운반하지 않는다.', 'forest', 0, ['village','coast','swamp','ruins']),
    ('coast', '안개 항구', '돌아오는 배와 돌아오지 않는 이름을 센다.', 'village', 0, ['road','river','citadel']),
    ('mountain', '서리등 산맥', '눈 아래 묻힌 길을 아는 사람은 많지 않다.', 'forest', 1, ['road','ruins','frontier']),
    ('ruins', '별무리 유적', '옛 왕국은 무너져도 그 빚은 남는다.', 'ruins', 1, ['river','mountain','citadel','frontier']),
    ('swamp', '검은갈대 습지', '안개 속에서는 발자국보다 기억을 믿어야 한다.', 'ruins', 1, ['forest','river','ruins']),
    ('citadel', '새벽 성채', '높은 성벽은 안쪽의 불안도 가둔다.', 'village', 1, ['road','coast','ruins','frontier']),
    ('frontier', '재의 변경', '전설을 끝내도 이곳에는 살아갈 사람이 남는다.', 'battle', 2, ['mountain','ruins','citadel']),
]
catalog['regions'] = [dict(zip(('id','name','description','art','minStage','routes'), r)) for r in REGIONS]
regions = {r['id']: r for r in catalog['regions']}
legacy_regions = {
    'departure':['village','forest','road'], 'first_hunt':['forest','road'], 'crossroads':['village','road','coast'],
    'lost_child':['forest','river'], 'reunion':['village','coast','citadel'], 'shrine':['forest','ruins'],
    'shieldbearer':['road','mountain'], 'bandit_road':['road','mountain'], 'old_ruin':['ruins'],
    'warden':['ruins'], 'dragon':['frontier'], 'caravan':['road','coast'], 'mentor':['village','forest'],
    'smith':['village','citadel'], 'spring':['forest','river'], 'winter':['mountain','village'],
    'whisper':['swamp'], 'bridge':['river'], 'scholar':['ruins','citadel'], 'homecoming':['village'],
    'veteran':['village','citadel'], 'quiet_years':['river','coast'], 'market':['road','coast','village'],
}
for event in catalog['events']:
    event['regions'] = legacy_regions[event['id']]
    event['stageWeights'] = [8,5,2] if event['id'] in ('departure','first_hunt','crossroads') else [5,6,5]
    event['chapter'] = event.get('chapter', '길 위의 기록')
    if event['id'] == 'departure': event['excludedFlag'] = 'helped_mira'
    if event['id'] == 'dragon': event['minStage'] = 2
    for choice in event['choices']: choice.pop('next', None)
first_hunt = next(e for e in catalog['events'] if e['id']=='first_hunt')
next(e for e in catalog['events'] if e['id']=='departure')['text'][0] = '길을 나선 여행자의 주머니에는 동전 몇 닢과 낡은 도구가 들어 있다. 낯선 만남이 그 길의 방향을 바꿀 수도 있다.'
first_hunt['text'][1] = '수풀 속 이빨이 달빛을 받는다. 길을 지킬지, 가진 식량으로 위험을 비켜 갈지 결정해야 한다.'
first_hunt['choices'][0]['battle'] = ['wolf']
first_hunt['choices'][0]['hint'] = '굶주린 늑대 · 턴제 전투'
first_hunt['choices'][0]['body'] = '늑대가 숲 안으로 물러났다. 떨리던 손을 가라앉히고 다시 길을 찾았다.'
crossroads = next(e for e in catalog['events'] if e['id']=='crossroads')
crossroads['text'][1] = '그는 당신의 낡은 도구를 살피더니 무기 하나를 골라 보라고 한다. 언젠가 이 길에서 다른 여행자를 도와주는 것으로 값을 대신하라는 조건이다.'
catalog['skills'].append(dict(id='strike', name='기본 공격', desc='무기와 위치에 관계없이 사용할 수 있는 약한 공격.', icon='sword',
    **{'from':[1,2,3,4], 'to':[1,2,3,4]}, power=.65, accuracy=100, cost=0))
for enemy in [
    dict(id='slime',name='유리빛 점액체',portrait='slime',maxHp=14,power=3,speed=1,armor=0,attack='산성 방울'),
    dict(id='boar',name='성난 멧돼지',portrait='boar',maxHp=22,power=5,speed=3,armor=1,attack='돌진'),
    dict(id='spider',name='갈대 거미',portrait='spider',maxHp=19,power=5,speed=5,armor=0,attack='독니',bleed=True),
    dict(id='wisp',name='길 잃은 불빛',portrait='wisp',maxHp=21,power=6,speed=5,armor=0,attack='서늘한 속삭임',ranged=True,stress=7),
]: catalog['enemies'].append(enemy)

encounters = {
    'village':['bandit'], 'forest':['wolf','boar'], 'road':['bandit','archer'], 'river':['slime','slime'],
    'coast':['bandit','slime'], 'mountain':['wolf','wolf'], 'ruins':['skeleton','wisp'],
    'swamp':['spider','wisp'], 'citadel':['bandit','cultist'], 'frontier':['skeleton','cultist','wisp'],
}
stats = {'rescue':'strength','mystery':'wit','pact':'will','hunt':'agility','dispute':'will','relic':'wit','craft':'wit','delivery':'agility','ritual':'will','survival':'strength'}
def choice(label, body, weeks=2, effects=None, **extra):
    return dict(label=label, body=body, weeks=weeks, effects=effects or {}, **extra)

rows = []
for line in (ROOT / 'Content/quests.tsv').read_text().splitlines():
    if not line or line.startswith('#'): continue
    fields = line.split('|')
    if len(fields)!=11: raise ValueError(f'Expected 11 fields, got {len(fields)}: {line[:90]}')
    rows.append(fields)
if len(rows)!=95: raise ValueError(f'Expected 95 authored quest arcs, got {len(rows)}')
for index, (region,stage,kind,title,hook,clue,alabel,aresult,blabel,bresult,echo) in enumerate(rows,1):
    stage = int(stage); r = regions[region]; stem = f'q{index:03d}'
    weight = ([10,5,2],[0,10,5],[0,0,10])[stage]
    stat = stats[kind]; dc = 4 + stage*3; reward = 12 + stage*18
    def scene(suffix, subtitle, text, choices, flag=None, delay=0):
        event = dict(id=stem+'_'+suffix,title=title+' · '+subtitle, chapter=r['name']+'의 사람들', place=r['name'], art=r['art'],
            tag={'rescue':'구조','mystery':'추리','pact':'약속','hunt':'토벌','dispute':'중재','relic':'유물','craft':'기술','delivery':'운송','ritual':'의식','survival':'생존'}[kind],
            regions=[region],minStage=stage,stageWeights=weight if suffix=='offer' else [6,8,6],once=True,text=text,choices=choices)
        if flag: event.update(requiredFlag=flag,minFlagWeeks=delay)
        if suffix=='offer' and kind=='rescue' and stage>0: event['minParty']=2
        if suffix=='offer' and kind=='dispute' and stage==2: event['minRep']=4
        catalog['events'].append(event)
    scene('offer','갈림길',[hook,clue],[
        choice(alabel, '당신은 '+clue+' 그 흔적을 따라 직접 답을 찾기로 했다.', effects={'flag':stem+'_a','xp':3+stage*3}),
        choice(blabel, '누구를 설득하고 무엇을 내어 줄지부터 생각했다. '+hook, effects={'flag':stem+'_b','stress':-3}),
        choice('지금 맡은 길을 우선한다', '이번 일은 다른 이의 손에 남겨 두었다. '+echo, weeks=1,effects={'rations':1})])
    enemies = encounters[region][:1] if stage==0 else encounters[region]
    investigation = choice(alabel+' — 끝까지 해낸다',aresult,weeks=3,effects={'flag':stem+'_solved_a','xp':12+stage*12},hint='위험을 감수하는 해결')
    if kind in ('hunt','rescue','survival','relic'):
        investigation.update(battle=enemies,hint='전투 · '+str(len(enemies))+'명의 적')
    else:
        investigation.update(check={'stat':stat,'dc':dc},failure={'body':'단서가 가리킨 답과 현실은 달랐다. '+echo,'effects':{'hp':-3-stage*2,'stress':5,'xp':4}})
    scene('trail','흔적을 좇다',[clue,hook,'이제 처음의 약속을 행동으로 옮길 때다.'],[
        investigation,
        choice('충분한 준비와 도움을 구한다',aresult,weeks=5,effects={'gold':-(8+stage*10),'flag':stem+'_solved_a','xp':8+stage*8},need={'gold':8+stage*10},hint='시간과 비용으로 위험을 줄인다'),
        choice('위험한 지점을 기록하고 물러난다',echo+' 해결하지 못한 문제도 연대기에 적었다.',weeks=1,effects={'xp':3,'stress':3})],stem+'_a',2)
    if kind in ('relic','ritual'):
        catalog['events'][-1]['choices'].insert(1,choice('지팡이로 남은 마력의 흐름을 읽는다',aresult,weeks=3,effects={'flag':stem+'_solved_a','xp':14+stage*10},need={'weapon':'staff'},hint='지팡이 소지 · 장비를 활용한 해법'))
    if kind=='hunt':
        catalog['events'][-1]['choices'].insert(1,choice('활로 거리를 벌려 흔적을 추적한다',aresult,weeks=4,effects={'flag':stem+'_solved_a','xp':12+stage*10},need={'weapon':'bow'},check={'stat':'agility','dc':dc},failure={'body':'안전한 거리는 벌렸지만 단서를 놓쳤다. '+echo,'effects':{'stress':4,'xp':5}},hint='활 소지 · 민첩 판정'))
    scene('bargain','다른 해법',[hook,'처음 고른 길 대신 사람들의 이해관계와 남은 자원을 살폈다.',clue],[
        choice(blabel+' — 약속을 지킨다',bresult,weeks=3,effects={'gold':-(6+stage*9),'flag':stem+'_solved_b','rep':1},need={'gold':6+stage*9},hint='대가를 치르고 합의를 이끈다'),
        choice('서로 잃게 될 것을 설명한다',bresult,weeks=2,effects={'flag':stem+'_solved_b','xp':10+stage*10},check={'stat':'will' if kind in ('dispute','pact','ritual') else 'wit','dc':dc},failure={'body':'한 번의 말로 좁힐 수 없는 간격도 있었다. '+echo,'effects':{'stress':7,'xp':4}}),
        choice('식량을 보태고 다른 이에게 일을 잇는다',echo+' 당신이 남긴 몫은 다음 사람을 위한 시작이 되었다.',weeks=1,effects={'rations':-1,'rep':1},need={'rations':1})],stem+'_b',2)
    scene('echo_a','선택의 여운',[aresult,echo],[
        choice('약속한 보수를 받는다',echo+' 손에 남은 보수는 다음 길을 준비하는 데 썼다.',weeks=1,effects={'gold':reward,'xp':8+stage*5}),
        choice('보수 대신 배운 것을 정리한다',echo+' 이번에 얻은 판단을 오래 기억하기로 했다.',weeks=4,effects={'xp':18+stage*12,'stress':-8,'rep':1})],stem+'_solved_a',4)
    scene('echo_b','다시 이어진 인연',[bresult,echo],[
        choice('사람들과 마지막 뒷일을 돌본다',echo+' 끝난 뒤의 노동까지 함께하자 약속은 믿음이 되었다.',weeks=3,effects={'gold':reward//2,'rep':2,'bond':1,'xp':8}),
        choice('작별 인사와 함께 길을 나선다',echo+' 새로운 길에서도 이 만남을 잊지 않기로 했다.',weeks=1,effects={'rations':2,'potions':1,'stress':-10})],stem+'_solved_b',4)

catalog['events'] += [
    dict(id='wayside_rest',title='이정표 아래의 작은 쉼',chapter='길 위의 숨',place='이름 없는 쉼터',art='camp',tag='쉼',regions=[],stageWeights=[3,3,3],text=['이정표 아래에는 먼저 쉬어 간 사람이 고른 평평한 돌들이 놓여 있다. 바람이 가방의 먼지를 털어 준다.','급한 길도 오늘의 몸을 대신 걸어 주지는 못한다. 잠깐 짐을 내려놓을 여유가 생겼다.'],choices=[
        choice('짐을 내려놓고 몸을 돌본다','덧난 곳에 새 붕대를 감았다. 먼 곳을 생각하는 동안 가까운 상처를 잊고 있었다.',weeks=2,effects={'heal':12,'stress':-12}),
        choice('주변에서 먹을 것을 찾는다','먹을 수 있는 풀과 마른 열매를 골라 담았다. 다음 여정의 작은 여유가 생겼다.',weeks=2,effects={'rations':2,'xp':4})]),
    dict(id='roaming_post',title='지역 사이를 잇는 우편마차',chapter='다른 땅의 소식',place='우편마차 정류소',art='village',tag='여행',regions=[],stageWeights=[3,3,3],text=['낡은 우편마차가 잠시 멈춘다. 짐칸에는 이 땅을 건너갈 편지와 되돌아온 소포가 함께 묶여 있다.','마부는 사람들의 소식을 나누며 다음 출발을 준비한다. 잠시 일손을 보태거나 다른 지역의 이야기를 들을 수 있다.'],choices=[
        choice('우편물 분류를 돕고 품삯을 받는다','수많은 주소 속에서 이 땅에 사는 사람들을 떠올렸다. 마부가 다음 길의 소식을 전해 주었다.',weeks=2,effects={'gold':10,'xp':5}),
        choice('낯선 사람들의 이야기를 듣는다','같은 사건도 강 너머에서는 다른 이름으로 불렸다. 그 차이를 연대기의 여백에 적었다.',weeks=1,effects={'stress':-8,'xp':4})])]
assert len(catalog['events']) == 500
assert len({e['id'] for e in catalog['events']}) == 500
assert len({e['title'] for e in catalog['events']}) == 500
target = ROOT/'Unity/Arvito/Assets/Resources/Arvito/catalog.json'
rendered = json.dumps(catalog,ensure_ascii=False,indent=2)+'\n'
if '--check' in sys.argv:
    if target.read_text() != rendered: raise SystemExit('Runtime catalog is stale. Run scripts/export-story-content.py.')
else:
    target.write_text(rendered)
print(f'Exported {len(catalog["events"])} events: 23 original, 95 five-scene quests, 2 roaming events; 10 regions.')
