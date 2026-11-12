import test from 'node:test';import assert from 'node:assert/strict';
import * as G from '../web/engine.mjs';import {EVENTS,REGIONS,SKILLS} from '../web/content.mjs';import * as Store from '../web/storage.mjs';
const fresh=(seed=42)=>G.newGame('루엔',seed),roundtrip=s=>G.deserialize(G.serialize(s));
function battle(s){G.startCombat(s,['wolf','wolf'],{title:'사냥',body:'살아남았다',effects:{gold:16,xp:20}});return s;}
function fight(s){let steps=0;while(s.mode==='combat'&&steps++<400){assert.equal(s.combat.active,'hero');const k=G.availableSkills(s,s.hero).find(id=>SKILLS[id].power>0&&!G.skillLock(s,id))||'focus';assert(G.act(s,k,G.validTargets(s,k)[0].id));assert(roundtrip(s));}assert.notEqual(s.mode,'combat');}
test('500 events have all illustrations and valid content',async()=>{const fs=await import('node:fs');assert.equal(Object.keys(EVENTS).length,500);for(const e of Object.values(EVENTS)){assert(fs.existsSync(new URL(`../web/art/Events/${e.id}.png`,import.meta.url)));assert(e.choices.every(c=>Number.isInteger(c.weeks)&&c.weeks>0));}});
test('first event and region vary; every draw respects conditions',()=>{const events=new Set(),regions=new Set();for(let seed=1;seed<=100;seed++){const s=fresh(seed*9182);events.add(s.eventId);regions.add(s.region);assert(G.eligible(s,EVENTS[s.eventId]));}assert(events.size>20);assert.equal(regions.size,5);});
test('region/stage/flags/repetition alter the weighted pool',()=>{const s=fresh();for(const r of Object.keys(REGIONS)){s.region=r;for(const stage of [0,1,2]){s.hero.level=stage===2?7:stage===1?3:1;for(let i=0;i<50;i++){const id=G.drawEvent(s);assert(G.eligible(s,EVENTS[id]));s.recent.push(id);s.recent=s.recent.slice(-12);}}}const e=EVENTS[G.drawEvent(s)],before=G.eventWeight(s,e);s.seen[e.id]=2;assert(G.eventWeight(s,e)<before);});
test('every event choice executes into valid state',()=>{for(const e of Object.values(EVENTS))for(let i=0;i<e.choices.length;i++){const s=fresh(888+i);s.eventId=e.id;s.region=e.regions[0]||s.region;s.gold=10000;s.rations=500;s.rep=100;const n=e.choices[i].need||{};if(n.flag)s.flags[n.flag]=true;if(n.weapon)s.inventory.push(n.weapon);assert(G.choose(s,i));assert(roundtrip(s),`${e.id} choice ${i}`);}});
test('50 seeded battles return only hero turns and terminate',()=>{for(let seed=1;seed<=50;seed++){const s=fresh(seed);G.applyEffects(s,{recruit:'mira'});battle(s);fight(s);}});
test('combat reload preserves RNG and next result',()=>{const a=battle(fresh(8)),b=roundtrip(a);const k=G.availableSkills(a,a.hero).find(id=>SKILLS[id].power>0&&!G.skillLock(a,id));G.act(a,k,G.validTargets(a,k)[0].id);G.act(b,k,G.validTargets(b,k)[0].id);assert.deepEqual(a,b);});
test('ally healer acts automatically without consuming potion when spell is ready',()=>{const s=fresh(19);G.applyEffects(s,{recruit:'elian'});battle(s);s.hero.hp=8;s.combat.active='elian';s.combat.queue=[];s.companions[0].energy=4;const potions=s.potions;const loaded=roundtrip(s);assert(loaded);assert.equal(loaded.combat?.active,'hero');assert.equal(loaded.potions,potions);assert(loaded.combat?.log.some(x=>x.includes('체력 +')));});
test('lethal wound resolves before XP; full heal reaches increased max',()=>{const s=fresh();s.hero.hp=1;G.applyEffects(s,{hp:-5,xp:100,fullHeal:true});assert.equal(s.mode,'ended');assert.equal(s.hero.hp,0);const t=fresh();t.hero.maxHp=114;t.hero.hp=1;G.applyEffects(t,{xp:45,fullHeal:true});assert.equal(t.hero.hp,t.hero.maxHp);});
test('appearance never consumes RNG',()=>{const a=G.newGame('루엔',9182,false),b=G.newGame('루엔',9182,true);assert.equal(a.rng,b.rng);assert.equal(a.eventId,b.eventId);assert.equal(G.spriteId(b.hero),'hero_female');});
test('travel charges two weeks and one ration; locked travel leaves state unchanged',()=>{const s=fresh();const route=G.routes(s)[0];assert(route);const w=s.weeks,r=s.rations;G.travel(s,route.id);assert.equal(s.weeks,w+2);assert.equal(s.rations,r-1);assert.equal(s.region,route.id);const before=G.serialize(s);G.travel(s,'missing');assert.equal(G.serialize(s),before);});
test('delayed reunion starts its wait at flag acquisition',()=>{const s=fresh();s.weeks=1040;s.region='village';G.applyEffects(s,{flag:'saved_child'});assert(!G.eligible(s,EVENTS.reunion));s.weeks+=103;assert(!G.eligible(s,EVENTS.reunion));s.weeks++;assert(G.eligible(s,EVENTS.reunion));});
test('saves recover valid backup and preserve current on storage failure',()=>{const data=new Map(),storage={getItem:k=>data.get(k)??null,setItem:(k,v)=>data.set(k,v)};const s=fresh();Store.save(storage,s);s.gold++;Store.save(storage,s);assert.equal(Store.load(storage).state.gold,s.gold);data.set(Store.SAVE,'broken');assert(Store.load(storage).notice);assert.equal(Store.load(storage).state.gold,s.gold-1);Store.save(storage,s);const before=data.get(Store.SAVE);assert.throws(()=>Store.save({...storage,setItem(){throw Error('quota');}},s));assert.equal(data.get(Store.SAVE),before);});
test('invalid or injected combat save is rejected',()=>{const s=battle(fresh());s.combat.queue.push('enemy_fake');assert.equal(roundtrip(s),null);});

test('legacy saves retain progress and migrate old abilities without replaying rewards',()=>{
 const s=fresh(77);s.version=1;s.hero.stats={strength:7,agility:6,wit:9,will:8};s.hero.hp=17;s.hero.maxHp=50;
 const loaded=roundtrip(s);assert(loaded);assert.equal(loaded.version,2);
 assert.deepEqual(loaded.hero.stats,{strength:7,agility:6,intelligence:9,wisdom:8,charisma:4,constitution:4});
 assert.equal(loaded.hero.hp,17);assert.equal(loaded.hero.maxHp,50);assert.equal(loaded.rng,s.rng);assert.equal(loaded.eventId,s.eventId);assert.deepEqual(roundtrip(loaded),loaded);
 const combat=battle(fresh(12));combat.version=1;combat.hero.stats={strength:4,agility:4,wit:4,will:4};combat.combat.pending.effects={stat:'will'};
 assert.equal(roundtrip(combat).combat.pending.effects.stat,'wisdom');
 delete s.hero.stats.wit;assert.equal(roundtrip(s),null);
});
test('all six abilities have authored checks and growth rewards; obsolete keys are gone',()=>{
 const keys=['strength','agility','intelligence','wisdom','charisma','constitution'],checks=new Set(),rewards=new Set();
 for(const e of Object.values(EVENTS))for(const c of e.choices){if(c.check?.stat){assert(keys.includes(c.check.stat));checks.add(c.check.stat);}for(const fx of [c.effects,c.failure?.effects])if(fx?.stat){assert(keys.includes(fx.stat));rewards.add(fx.stat);}}
 assert.equal(checks.size,6);assert.equal(rewards.size,6);
 const s=fresh();for(const stat of keys){const before=G.checkChance(s,{stat,dc:4});s.hero.stats[stat]++;assert.equal(G.checkChance(s,{stat,dc:4}),before+7);}
});
test('constitution growth improves max HP and resting; wisdom reduces tension',()=>{
 const a=fresh(919),b=structuredClone(a);a.hero.hp=b.hero.hp=1;
 G.applyEffects(a,{stat:'constitution'});assert.equal(a.hero.maxHp,b.hero.maxHp+2);assert.equal(a.hero.hp,3);
 a.hero.hp=1;G.camp(a);G.camp(b);assert(a.hero.hp>b.hero.hp);
 assert.equal(G.stressTaken(b,b.hero,8),7);b.hero.stats.wisdom=8;assert.equal(G.stressTaken(b,b.hero,8),6);
 const invalid=fresh();delete invalid.hero.stats.charisma;assert.equal(roundtrip(invalid),null);
});
test('intelligence increases magic damage with identical rolls',()=>{
 const a=fresh(75);G.applyEffects(a,{recruit:'borin'});a.inventory.push('staff');G.equip(a,'staff');battle(a);
 const b=structuredClone(a);b.hero.stats.intelligence+=6;
 const sk=G.availableSkills(a,a.hero).find(id=>SKILLS[id].magic&&SKILLS[id].power&&!G.skillLock(a,id));assert(sk);
 const target=a.combat.enemies[0];target.hp=target.maxHp=1000;b.combat.enemies[0].hp=b.combat.enemies[0].maxHp=1000;
 G.act(a,sk,target.id);G.act(b,sk,target.id);assert(b.combat.enemies[0].hp<a.combat.enemies[0].hp);
});
