import test from 'node:test';import assert from 'node:assert/strict';import {readFileSync} from 'node:fs';import {parseHTML} from 'linkedom';
import * as G from '../web/engine.mjs';import {SKILLS} from '../web/content.mjs';
import {abilityModifier,resolveD20,hitChance,normalizeCombatRules,DEFAULT_COMBAT_RULES} from '../web/combat-rules.mjs';
import {DEFAULT_SETUP,LAB_KEY,createLabBattle,normalizeSetup,serializeSetup,parseSetup,runBatch} from '../web/combat-lab-model.mjs';
const roundtrip=s=>G.deserialize(G.serialize(s));
const attack=s=>{const id=G.availableSkills(s,s.hero).find(k=>SKILLS[k].power&&!G.skillLock(s,k));return [id,G.validTargets(s,id)[0].id];};
test('d20 natural 1 always misses, natural 20 always crits, ties hit, chance is bounded',()=>{
 assert(!resolveD20(1,100,0).hit);assert(resolveD20(20,-100,100).hit);assert(resolveD20(20,0,10).critical);
 assert(resolveD20(10,3,13).hit);assert(!resolveD20(9,3,13).hit);assert.equal(hitChance(100,0),95);assert.equal(hitChance(-100,100),5);assert.equal(hitChance(3,10),70);
 assert.deepEqual([0,2,4,6,8].map(abilityModifier),[-2,-1,0,1,2]);
});
test('all 20 faces match the probability shown to the player',()=>{
 for(const bonus of [-4,0,3,8,20])for(const defense of [6,10,15,24]){const hits=Array.from({length:20},(_,i)=>resolveD20(i+1,bonus,defense).hit).filter(Boolean).length;assert.equal(hitChance(bonus,defense),hits*5);}
});
test('ability-based attack, physical and mental defenses, and guard are reflected without consuming RNG',()=>{
 const {state:s}=createLabBattle(DEFAULT_SETUP),target=s.combat.enemies[0],before=G.serialize(s);
 const p=G.attackPreview(s.hero,target,SKILLS.slash,s.combat.rules);assert.equal(G.serialize(s),before);
 s.hero.stats.strength+=2;assert.equal(G.attackPreview(s.hero,target,SKILLS.slash,s.combat.rules).bonus,p.bonus+1);
 const base=G.defenseValue(s.hero,{},s.combat.rules);s.hero.stats.agility+=2;assert.equal(G.defenseValue(s.hero,{},s.combat.rules),base+1);
 const mental=G.defenseValue(s.hero,{magic:true},s.combat.rules);s.hero.stats.wisdom+=2;assert.equal(G.defenseValue(s.hero,{magic:true},s.combat.rules),mental+1);
 s.hero.status.guard=true;assert.equal(G.defenseValue(s.hero,{},s.combat.rules),base+1+s.combat.rules.guardBonus);
});
test('new d20 combat saves replay exact dice, intents, RNG and results',()=>{
 const {state:a}=createLabBattle({...DEFAULT_SETUP,enemies:['bandit','archer'],hp:200}),b=roundtrip(a);assert(b);assert.deepEqual(a,b);
 const [k,id]=attack(a);assert(G.act(a,k,id));assert(G.act(b,k,id));assert.deepEqual(a,b);
});
test('legacy battles without rules retain their old resolution after reloading',()=>{
 const a=G.newGame('예전 저장',99);a.hero.hp=a.hero.maxHp=200;G.startCombat(a,['wolf','wolf'],{title:'이전 전투',body:'완료',effects:{}},null);
 assert(!a.combat.rules);const b=roundtrip(a);assert(b);const [k,id]=attack(a);G.act(a,k,id);G.act(b,k,id);assert.deepEqual(a,b);assert(!a.combat?.log.some(x=>x.includes('d20 ')));
});
test('invalid saved d20 rules and intent targets are rejected',()=>{
 const {state:s}=createLabBattle(DEFAULT_SETUP);s.combat.rules.defenseBase=999;assert.equal(roundtrip(s),null);
 const {state:t}=createLabBattle(DEFAULT_SETUP);t.combat.intents[t.combat.enemies[0].id]={kind:'attack',targetId:'missing'};assert.equal(roundtrip(t),null);
});
test('enemy intents can be read repeatedly without advancing randomness or state',()=>{
 const {state:s}=createLabBattle({...DEFAULT_SETUP,enemies:['bandit','cultist']}),before=G.serialize(s);
 for(let i=0;i<20;i++)for(const e of s.combat.enemies)assert.equal(typeof G.enemyIntent(s,e),'string');assert.equal(G.serialize(s),before);
});
test('round three bandits announce guard and actually guard when their turn arrives',()=>{
 const {state:s}=createLabBattle({...DEFAULT_SETUP,companions:[],heroRank:1,hp:500,enemies:['bandit'],rules:{enemyDamageScale:.25}});
 let reached=false;
 for(let i=0;i<20&&s.mode==='combat';i++){if(s.combat.round===3){const e=s.combat.enemies[0];assert(e.status.guard||s.combat.intents[e.id]?.kind==='guard');reached=true;break;}G.act(s,'focus','hero');}
 assert(reached);
});
test('test setup roundtrips, rejects invalid rosters, and leaves default rules unchanged',()=>{
 assert.deepEqual(parseSetup(serializeSetup(DEFAULT_SETUP)),normalizeSetup(DEFAULT_SETUP));assert.throws(()=>normalizeSetup({...DEFAULT_SETUP,enemies:[]}));assert.throws(()=>normalizeSetup({...DEFAULT_SETUP,enemies:['fake']}));
 assert.throws(()=>normalizeSetup({...DEFAULT_SETUP,companions:['mira','mira']}));assert.throws(()=>parseSetup('{"format":"wrong","version":1}'));
 const before={...DEFAULT_COMBAT_RULES};const altered=createLabBattle({...DEFAULT_SETUP,rules:{defenseBase:16}});assert.equal(altered.combat.rules.defenseBase,16);assert.deepEqual(DEFAULT_COMBAT_RULES,before);assert.equal(normalizeCombatRules({}).defenseBase,10);
});
test('thirty-run batch is deterministic and reports wins, losses, and bounded timeouts',()=>{
 const a=runBatch(DEFAULT_SETUP,30),b=runBatch(DEFAULT_SETUP,30);assert.deepEqual(a,b);assert.equal(a.wins+a.losses+a.timeouts,30);assert(a.averageRounds>0);assert(a.winRate>=0&&a.winRate<=100);
});
test('lab DOM can launch a battle, show dice odds, and execute an action without touching play saves',async()=>{
 const {document,window}=parseHTML(readFileSync(new URL('../web/combat-lab.html',import.meta.url),'utf8'));
 const data=new Map([['arvito.browser.v1','untouched'],['arvito.event-workshop.v1','drafts']]);const storage={getItem:k=>data.get(k)??null,setItem:(k,v)=>data.set(k,v)};
 Object.assign(globalThis,{document,window,localStorage:storage});await import('../web/combat-lab.mjs');
 const click=sel=>document.querySelector(sel).dispatchEvent(new window.Event('click',{bubbles:true}));
 click('[data-action="start"]');assert(document.querySelector('.enemy-row'));assert(document.querySelector('.target-preview')?.textContent.includes('d20'));assert.equal(document.querySelectorAll('.enemy-row .combatant').length,2);
 const before=document.querySelector('#combat-log').textContent;click('[data-action="act"]');assert.notEqual(document.querySelector('#combat-log').textContent,before);
 assert.equal(data.get('arvito.browser.v1'),'untouched');assert.equal(data.get('arvito.event-workshop.v1'),'drafts');assert(data.has(LAB_KEY));
});
