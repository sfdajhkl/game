import test from 'node:test';
import assert from 'node:assert/strict';
import * as G from '../web/engine.mjs';
import {EVENTS,REGIONS,STAT_NAMES,WEAPONS,SKILLS,COMPANIONS,ENEMIES} from '../web/content.mjs';
import LEGACY_EVENTS from '../web/legacy-events.mjs';
const funded=()=>{const s=G.newGame('검토',42);s.gold=1000;s.rations=100;s.potions=50;s.rep=20;s.hero.hp=s.hero.maxHp=500;return s;};
test('100 complete events have valid choices, effects, and an available alternative',()=>{
 assert.equal(Object.keys(EVENTS).length,100);assert.equal(new Set(Object.values(EVENTS).map(e=>e.title)).size,100);
 for(const e of Object.values(EVENTS)){
  assert(e.text.length===2&&e.text.every(p=>p.trim().length>20),e.id);assert(e.regions.every(r=>REGIONS[r]),e.id);
  assert(e.stageWeights.length===3&&e.stageWeights.every(n=>Number.isFinite(n)&&n>=0));
  assert(e.choices.some(c=>!c.need),e.id+' has no resource-free alternative');
  for(const c of e.choices){
   assert(!c.next,e.id+' must resolve within its own scene');assert(c.body?.length>20&&c.label?.length>3);
   if(c.check){assert(Object.hasOwn(STAT_NAMES,c.check.stat));assert(Number.isFinite(c.check.dc));assert(c.failure?.body?.length>20);}
   if(c.battle){assert(c.battle.every(id=>ENEMIES[id]));assert(c.label.includes('전투'));}
   for(const fx of [c.effects,c.failure?.effects].filter(Boolean)){
    if(fx.weapon)assert(WEAPONS[fx.weapon]);if(fx.skill)assert(SKILLS[fx.skill]);if(fx.recruit)assert(COMPANIONS[fx.recruit]);if(fx.stat)assert(Object.hasOwn(STAT_NAMES,fx.stat));
    for(const resource of ['gold','rations','potions'])if(fx[resource]<0)assert((c.need?.[resource]||0)>=-fx[resource],e.id+' unannounced cost');
   }
  }
 }
});
test('every authored check resolves both success and failure text and effects',()=>{
 let failSeed=1;while(G.random({rng:failSeed})<=.99)failSeed++;
 for(const e of Object.values(EVENTS))for(const [index,c] of e.choices.entries())if(c.check)for(const success of [true,false]){
  const s=funded();s.eventId=e.id;s.region=e.regions[0]||s.region;s.rng=success?1:failSeed;
  const fx=(success?c:c.failure).effects,gold=s.gold;
  assert(G.choose(s,index));assert.equal(s.mode,'outcome',e.id);assert.equal(s.outcome.check.success,success,e.id);
  assert.equal(s.outcome.body,(success?c:c.failure).body,e.id);assert.equal(s.gold,gold+(fx.gold||0),e.id);assert(G.deserialize(G.serialize(s)),e.id);
 }
});
test('all 400 retired IDs migrate without losing progress or consuming randomness',()=>{
 assert.equal(Object.keys(LEGACY_EVENTS).length,400);
 for(const [id,target] of Object.entries(LEGACY_EVENTS)){
  const s=funded();delete s.contentRevision;s.eventId=id;s.seen[id]=1;s.recent=[id];s.weeks=77;
  const before=structuredClone(s),loaded=G.deserialize(G.serialize(s));assert(loaded,id);assert.equal(loaded.eventId,target,id);assert.equal(loaded.contentRevision,2);
  for(const k of ['rng','weeks','gold','rations','potions','hero','journal','flags','flagWeeks'])assert.deepEqual(loaded[k],before[k],id+' '+k);
  assert.equal(loaded.seen[target],1);assert.deepEqual(loaded.recent,[target]);assert.deepEqual(G.deserialize(G.serialize(loaded)),loaded);
 }
});
test('retired event migration preserves pending outcome and battle rewards',()=>{
 for(const mode of ['outcome','combat']){
  const s=funded();s.eventId='q095_echo_b';delete s.contentRevision;
  const reward={title:'이전 사건',body:'이전 선택의 결과',effects:{gold:50},changes:['골드 +50'],weeks:1};
  if(mode==='combat')G.startCombat(s,['wolf'],reward);else{s.mode='outcome';s.outcome=reward;}
  const loaded=G.deserialize(G.serialize(s));assert(loaded);assert.equal(loaded.gold,s.gold);assert.equal(loaded.rng,s.rng);assert.deepEqual(loaded.outcome,s.outcome);assert.deepEqual(loaded.combat,s.combat);
 }
 const bad=funded();bad.eventId='not-a-real-event';assert.equal(G.deserialize(G.serialize(bad)),null);
});
test('all regions and stages remain playable after one-time events are exhausted',()=>{
 const s=funded();for(const e of Object.values(EVENTS))if(e.once)s.seen[e.id]=1;
 for(const region of Object.keys(REGIONS))for(const level of [1,3,7]){s.region=region;s.hero.level=level;s.recent=Object.keys(EVENTS);const id=G.drawEvent(s);assert(G.eligible(s,EVENTS[id]));assert(!EVENTS[id].once);}
});
