import test from 'node:test';
import assert from 'node:assert/strict';
import * as G from '../web/engine.mjs';
import {EVENTS,WEAPONS,SKILLS} from '../web/content.mjs';

const battle=(seed=42)=>{const s=G.newGame('테스트',seed);s.eventId='first_hunt';G.applyEffects(s,{recruit:'mira'});G.startCombat(s,['wolf','wolf'],{title:'첫 전투',body:'승리했다.',effects:{gold:16,xp:20},next:'crossroads'});return s;};
function autoAction(s){
 const a=G.actorById(s,s.combat.active);
 const attacks=G.availableSkills(s,a).filter(id=>SKILLS[id].power&&!G.skillLock(s,id));
 let id=attacks[0]||'guard';
 if(a.hp<a.maxHp*.4&&s.potions>0)id='potion';
 const targets=G.validTargets(s,id);return G.act(s,id,id==='potion'?a.id:targets[0].id);
}

test('시작은 16세, 무기만 있을 뿐 고정 직업은 없다',()=>{
 const s=G.newGame('루엔',99);assert.equal(G.ageOf(s),16);assert.equal(s.hero.name,'루엔');assert.equal(s.hero.class,undefined);assert.equal(s.hero.weapon,'sword');assert.deepEqual(G.deserialize(G.serialize(s)),s);
});
test('모든 사건은 최소 1주, 참조하는 장비와 기술이 존재한다',()=>{
 for(const [id,e] of Object.entries(EVENTS)){assert.ok(e.choices.length>=2,id);for(const c of e.choices){assert.ok(Number.isInteger(c.weeks)&&c.weeks>=1,id);if(c.next)assert.ok(EVENTS[c.next]);if(c.effects.weapon)assert.ok(WEAPONS[c.effects.weapon]);if(c.effects.skill)assert.ok(SKILLS[c.effects.skill]);if(c.check)assert.ok(c.failure);}}
});
test('시작 사건은 동료를 영입하고 다음 사건과 시간이 유지된다',()=>{
 const s=G.newGame('루엔',1);assert.ok(G.choose(s,0));assert.equal(s.weeks,1);assert.equal(G.party(s).length,2);assert.equal(s.flags.helped_mira,true);assert.equal(s.mode,'outcome');assert.ok(G.continueStory(s));assert.equal(s.eventId,'first_hunt');
});
test('보급 부족 선택은 시간이나 상태를 바꾸지 않는다',()=>{
 const s=G.newGame();s.eventId='market';s.gold=0;const snapshot=G.serialize(s);assert.equal(G.choose(s,0),false);assert.equal(G.serialize(s),snapshot);
});
test('일행은 네 명까지이며 중복 영입과 죽은 동료 재영입을 막는다',()=>{
 const s=G.newGame();for(const recruit of ['mira','elian','borin','mira'])G.applyEffects(s,{recruit});assert.equal(G.party(s).length,4);assert.equal(s.formation[0],'borin');s.companions[0].hp=0;s.fallen.push('mira');s.formation=s.formation.filter(id=>id!=='mira');G.applyEffects(s,{recruit:'mira'});assert.equal(G.party(s).length,3);
});
test('다년간 사건도 매주 노화 판정하며 나이에 따라 위험이 증가한다',()=>{
 assert.equal(G.weeklyMortality(39),0);
 for(let age=40;age<100;age++)assert.ok(G.weeklyMortality(age+1)>=G.weeklyMortality(age));
 const a=G.newGame('A',41),b=G.deserialize(G.serialize(a));a.weeks=b.weeks=44*52;
 G.advanceTime(a,104);for(let i=0;i<104&&b.mode!=='ended';i++)G.advanceTime(b,1);
 assert.equal(a.weeks,b.weeks);assert.equal(a.rng,b.rng);assert.equal(a.mode,b.mode);
});
test('40세 전에는 노화로 죽지 않고 1주 미만 사건을 거부한다',()=>{
 const s=G.newGame('A',321);assert.equal(G.advanceTime(s,23*52),true);assert.equal(G.ageOf(s),39);assert.throws(()=>G.advanceTime(s,0));
});
test('긴 사건 도중 수명이 끝나면 미완료 보상이 지급되지 않는다',()=>{
 const s=G.newGame('A',87);s.weeks=90*52;s.eventId='quiet_years';const gold=s.gold;G.choose(s,0);assert.equal(s.mode,'ended');assert.equal(s.ending.cause,'old_age');assert.equal(s.gold,gold);assert.ok(s.weeks<95*52);
});
test('초기 친절은 이후 재회 사건을 열고 본 적 없는 사건을 강제하지 않는다',()=>{
 const s=G.newGame();s.weeks=104;assert.ok(!G.availableEvents(s).includes('reunion'));s.flags.saved_child=true;assert.ok(G.availableEvents(s).includes('reunion'));s.seen.reunion=1;assert.ok(!G.availableEvents(s).includes('reunion'));
});
test('다른 무기로 바꾸면 무기 기술만 교체되고 배운 기술은 남는다',()=>{
 const s=G.newGame();G.applyEffects(s,{weapon:'staff',skill:'rally'});G.equip(s,'staff');const ids=G.availableSkills(s,s.hero);assert.ok(ids.includes('ember'));assert.ok(ids.includes('rally'));assert.ok(!ids.includes('slash'));
});
test('전투에서 열 제한, 잘못된 대상, 기력 부족은 턴을 소모하지 않는다',()=>{
 const s=battle();s.combat.active='hero';s.hero.weapon='bow';const before=G.serialize(s);assert.ok(G.skillLock(s,'shot'));assert.equal(G.act(s,'shot','enemy_0'),false);assert.equal(G.serialize(s),before);
 s.hero.weapon='sword';assert.equal(G.act(s,'slash','mira'),false);s.combat.active='mira';s.companions[0].energy=0;assert.equal(G.act(s,'pin','enemy_0'),false);
});
test('자리 교대는 진형을 바꾸며 하나의 차례를 소비한다',()=>{
 const s=battle();s.combat.active='hero';s.combat.queue=['mira'];assert.ok(G.act(s,'move','mira'));assert.deepEqual(s.formation,['mira','hero']);assert.equal(s.combat.active,'mira');
});
test('회복약은 한 개만 소모하며 출혈을 제거하고 최대 체력을 넘지 않는다',()=>{
 const s=battle();s.combat.active='hero';s.combat.queue=['mira'];s.hero.hp=s.hero.maxHp-3;s.hero.status.bleed=3;const n=s.potions;G.act(s,'potion','hero');assert.equal(s.hero.hp,s.hero.maxHp);assert.equal(s.hero.status.bleed,0);assert.equal(s.potions,n-1);assert.equal(s.combat.active,'mira');
});
test('생각 없이 반복 클릭해도 결과 화면에서 보상이 중복 지급되지 않는다',()=>{
 const s=G.newGame();G.choose(s,0);const str=G.serialize(s);assert.equal(G.choose(s,0),false);assert.equal(G.serialize(s),str);
});
test('첫 전투를 수동 행동으로 끝내고 보상 및 다음 사건으로 진행한다',()=>{
 for(let seed=1;seed<=30;seed++){
  const s=battle(seed);let n=0;while(s.mode==='combat'&&n++<100)assert.ok(autoAction(s));
  assert.equal(s.mode,'outcome',`seed ${seed}`);assert.equal(s.victories,1);assert.equal(s.gold,40);assert.ok(n<100);G.continueStory(s);assert.equal(s.eventId,'crossroads');
 }
});
test('행동 직후 저장을 복원하면 난수와 다음 차례가 같아진다',()=>{
 const a=battle(9);autoAction(a);const b=G.deserialize(G.serialize(a));assert.ok(b);assert.equal(G.serialize(a),G.serialize(b));autoAction(a);autoAction(b);assert.equal(G.serialize(a),G.serialize(b));
});
test('출혈로 주인공이 쓰러지면 즉시 생애가 끝난다',()=>{
 const s=battle();s.hero.hp=2;s.hero.status.bleed=1;s.combat.active='mira';s.combat.queue=['hero'];G.act(s,'guard','mira');assert.equal(s.mode,'ended');assert.equal(s.ending.cause,'battle');
});
test('동료 사망은 진형에서 제거되며 살아있는 아군으로 이어진다',()=>{
 const s=battle();const m=s.companions[0];m.hp=2;m.status.bleed=1;s.combat.active='hero';s.combat.queue=['mira','hero'];G.act(s,'guard','hero');assert.deepEqual(s.fallen,['mira']);assert.deepEqual(s.formation,['hero']);assert.equal(s.combat.active,'hero');assert.ok(s.journal.some(j=>j.type==='loss'));
});
test('기절은 한 차례만 건너뛰고 방어는 자기 차례에 해제된다',()=>{
 const s=battle();s.companions[0].status.stun=1;s.combat.active='hero';s.combat.queue=['mira','hero'];G.act(s,'guard','hero');assert.equal(s.combat.active,'hero');assert.equal(s.companions[0].status.stun,0);assert.equal(s.hero.status.guard,undefined);
});
test('긴장 100은 한 차례를 잃고 의지가 적의 긴장 피해를 줄인다',()=>{
 const s=battle();s.companions[0].stress=100;s.combat.active='hero';s.combat.queue=['mira','hero'];G.act(s,'guard','hero');assert.equal(s.combat.active,'hero');assert.equal(s.companions[0].stress,65);s.hero.stats.will=12;assert.equal(G.stressTaken(s,s.hero,10),7);
});
test('퇴각은 승리 보상 없이 안전하게 끝나고 일회성 도전을 다시 연다',()=>{
 const s=battle();s.gold=31;s.seen.first_hunt=1;G.flee(s);assert.equal(s.gold,24);assert.equal(s.victories,0);assert.equal(s.mode,'outcome');assert.equal(s.seen.first_hunt,0);G.continueStory(s);assert.equal(s.eventId,'crossroads');
});
test('야영은 자원을 사용하고 시간을 전진시키며 종료 후에는 불가능하다',()=>{
 const s=G.newGame();s.hero.hp=10;s.hero.stress=35;G.camp(s);assert.equal(s.rations,3);assert.equal(s.weeks,1);assert.equal(s.hero.stress,15);assert.ok(s.hero.hp>10);G.endLife(s,'old_age');assert.equal(G.camp(s).ok,false);
});
test('깨진 저장과 알 수 없는 버전은 안전하게 거부한다',()=>{
 assert.equal(G.deserialize('{broken'),null);assert.equal(G.deserialize('{}'),null);const s=G.newGame();s.version=999;assert.equal(G.deserialize(G.serialize(s)),null);
});
