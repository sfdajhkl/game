import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {parseHTML} from 'linkedom';
import * as Model from '../web/editor-model.mjs';
import * as Runner from '../web/editor-test.mjs';
import * as Content from '../web/content.mjs';
import * as G from '../web/engine.mjs';
const example=()=>({...structuredClone(Content.EVENTS.q002_offer),illustrationId:'q002_offer'});
const store=()=>{const data=new Map([['arvito.browser.v1','live-play-save']]);return {data,getItem:k=>data.get(k)??null,setItem:(k,v)=>data.set(k,v)};};
test('blank workspace saves incomplete events but cannot export them as finished',()=>{
 const e=Model.freshEvent();assert(e.title==='');assert(Model.validateEvent(e).errors.length>0);
 assert.equal(Model.parsePack(Model.packEvents([e]))[0].id,e.id);assert.throws(()=>Model.packEvents([e],true));
 const s=store();Model.saveDraft(s,[e]);assert.deepEqual(Model.loadDraft(s).events,[e]);assert.equal(s.getItem('arvito.browser.v1'),'live-play-save');
});
test('all existing events can be used as independently validated reference drafts',()=>{
 for(const source of Object.values(Content.EVENTS)){
  const e={...structuredClone(source),illustrationId:source.id};assert.deepEqual(Model.validateEvent(e).errors,[],e.id);
  assert.deepEqual(Model.parsePack(Model.packEvents([e],true)),[e]);
 }
});
test('validation rejects broken checks, costs, IDs, unreachable stages and references',()=>{
 const mutate=fn=>{const e=example();fn(e);assert(Model.validateEvent(e).errors.length>0);};
 mutate(e=>e.id='../../bad');mutate(e=>e.stageWeights=[0,0,0]);mutate(e=>e.choices[0].failure.body='');
 mutate(e=>e.choices[0].effects.weapon='missing');mutate(e=>e.choices[1].effects.gold=-30);
 mutate(e=>e.choices[1].battle=['unknown']);mutate(e=>e.choices[0].weeks=0);mutate(e=>e.requiredFlag=e.excludedFlag='same_flag');
 const e=example();assert(Model.validateEvent(e,[e,e]).errors.length>0);
});
test('imports reject wrong shapes, duplicate IDs and prototype keys',()=>{
 assert.throws(()=>Model.parsePack('{"format":"arvito-event-draft","version":1,"events":[{"__proto__":{}}]}'));
 assert.throws(()=>Model.parsePack('{"format":"arvito-event-draft","version":1,"events":[null]}'));
 assert.throws(()=>Model.parsePack(Model.packEvents([example(),example()])));
 assert.throws(()=>Model.parsePack(JSON.stringify({events:[example()]})));
});
test('autosave preserves current data on backup failure and recovers a corrupt current copy',()=>{
 const s=store(),a=example();Model.saveDraft(s,[a]);a.title='다음 초안';Model.saveDraft(s,[a]);
 s.setItem(Model.DRAFT_KEY,'broken');assert.equal(Model.loadDraft(s).events[0].title,Content.EVENTS.q002_offer.title);
 Model.saveDraft(s,[a]);const before=s.getItem(Model.DRAFT_KEY);
 assert.throws(()=>Model.saveDraft({...s,setItem(k,v){if(k===Model.BACKUP_KEY)throw Error('storage denied');s.setItem(k,v);}},[Model.freshEvent()]));
 assert.equal(s.getItem(Model.DRAFT_KEY),before);assert.equal(s.getItem('arvito.browser.v1'),'live-play-save');
});
test('choice tests use the engine without changing the live event catalog',()=>{
 const before=JSON.stringify(Content.EVENTS),e=example();const run=Runner.prepareTest(e,{seed:42,gold:10});
 assert(Runner.testChoice(run,0));assert.equal(run.state.mode,'outcome');assert(run.state.outcome.check);
 assert.equal(JSON.stringify(Content.EVENTS),before);
 const locked=example();locked.choices[1].need={gold:50};const s=Runner.prepareTest(locked,{gold:0});assert(!Runner.testChoice(s,1));assert.equal(s.state.mode,'story');
});
test('combat test runs hero actions and automatic allies without persisting a play save',()=>{
 const e={...structuredClone(Content.EVENTS.first_hunt),illustrationId:'first_hunt'};
 const s=Runner.prepareTest(e,{seed:22,hp:200,companions:['mira'],stats:{strength:15}});assert(Runner.testChoice(s,0));
 let turns=0;while(s.state.mode==='combat'&&turns++<100){assert.equal(s.state.combat.active,'hero');const skill=G.availableSkills(s.state,s.state.hero).find(k=>Content.SKILLS[k].power&&!G.skillLock(s.state,k));assert(skill);assert(Runner.testAction(s,skill,G.validTargets(s.state,skill)[0].id));}
 assert.equal(s.state.mode,'outcome');assert(s.state.outcome.combatWin);assert(!Content.EVENTS.__workshop_test__);
 const t=Runner.prepareTest(e,{hp:200});Runner.testChoice(t,0);if(t.state.mode==='combat'){assert(Runner.testFlee(t));assert.equal(t.state.mode,'outcome');}
});
test('workshop DOM starts empty and supports creating, editing, copying, removing and undoing a draft',async()=>{
 const html=readFileSync(new URL('../web/editor.html',import.meta.url),'utf8');const {window,document}=parseHTML(html);const storage=store();
 // DOM behavior only; layout and browser file dialogs are not simulated.
 document.querySelector('#search').value='';document.querySelector('#library-search').value='';
 document.querySelector('#library').showModal=()=>{};document.querySelector('#library').close=()=>{};
 Object.assign(globalThis,{window,document,localStorage:storage,confirm:()=>true});
 await import('../web/editor.mjs');
 const click=selector=>document.querySelector(selector).dispatchEvent(new window.Event('click',{bubbles:true}));
 assert.equal(document.querySelectorAll('.event-link').length,0);
 click('[data-action="new"]');assert.equal(document.querySelectorAll('.event-link').length,1);
 const title=document.querySelector('[data-path="title"]');title.value='<img src=x onerror=alert(1)>';title.dispatchEvent(new window.Event('input',{bubbles:true}));
 assert.equal(document.querySelector('#preview h3').textContent,title.value);assert.equal(document.querySelector('#preview h3 img'),null);
 click('[data-action="save"]');assert.equal(Model.loadDraft(storage).events[0].title,title.value);
 click('[data-action="duplicate"]');assert.equal(document.querySelectorAll('.event-link').length,2);
 click('[data-action="delete"]');assert.equal(document.querySelectorAll('.event-link').length,1);
 click('[data-action="undo"]');assert.equal(document.querySelectorAll('.event-link').length,2);
 assert.equal(storage.getItem('arvito.browser.v1'),'live-play-save');
});
