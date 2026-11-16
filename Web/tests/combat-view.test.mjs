import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import {parseHTML} from 'linkedom';
import * as G from '../web/engine.mjs';
import * as Content from '../web/content.mjs';
import * as View from '../web/combat-view.mjs';
import {createLabBattle,DEFAULT_SETUP} from '../web/combat-lab-model.mjs';
const battle=(extra={})=>createLabBattle({...DEFAULT_SETUP,hp:200,...extra}).state;
const dom=html=>parseHTML(`<html><body>${html}</body></html>`).document;

test('parchment battle keeps every enemy, automatic companion and current target visible without consuming RNG',()=>{
 const s=battle({enemies:['bandit','archer','wolf','skeleton'],companions:['mira','borin','elian']});
 const before=G.serialize(s),ui=View.combatSelection(s),document=dom(View.renderCombat(s,ui));
 assert.equal(G.serialize(s),before);
 assert.equal(document.querySelectorAll('.combat-enemy').length,4);
 assert.equal(document.querySelectorAll('.combat-ally').length,4);
 assert.equal(document.querySelectorAll('.combat-ally[data-action="target"]').length,4);
 assert.equal(document.querySelectorAll('.combat-ally [data-action="act"]').length,0);
 assert.equal(document.querySelectorAll('.combat-command-list button').length,4);
 assert(document.querySelector('.combat-preview').textContent.includes('d20'));
 assert(document.querySelector('.combat-target').textContent.includes(G.actorById(s,ui.target).name));
 assert.equal(dom(View.combatAttributes(s)).querySelectorAll('[data-stat]').length,6);
});
test('commands select real attacks, defensive actions and ally healing without spending a turn',()=>{
 const s=battle(),before=G.serialize(s);
 const defend=View.chooseCombatCommand(s,{},'defend');assert.equal(defend.skill,'guard');assert.equal(defend.target,'hero');
 const item=View.chooseCombatCommand(s,defend,'items');assert.equal(item.skill,'potion');assert(G.validTargets(s,item.skill,'hero').some(a=>a.id===item.target));
 const document=dom(View.renderCombat(s,item));assert(document.querySelector('.combat-execute').textContent.includes('회복약'));
 for(const enemy of document.querySelectorAll('.combat-enemy'))assert(enemy.disabled);
 s.potions=0;assert(dom(View.renderCombat(s,item)).querySelector('[data-action="act"]').disabled);s.potions=3;
 assert.equal(G.serialize(s),before);
});
test('every learned skill stays reachable through bounded skill pages including lock reasons',()=>{
 const s=battle();s.hero.skills=Object.keys(Content.SKILLS);
 const all=G.availableSkills(s,s.hero),rendered=[];
 for(let page=0;page<Math.ceil(all.length/4);page++){
  const document=dom(View.renderCombat(s,{category:'skills',page}));
  for(const button of document.querySelectorAll('[data-action="skill"]')){
   rendered.push(button.dataset.id);const lock=G.skillLock(s,button.dataset.id);assert.equal(button.disabled,!!lock);if(lock)assert(button.textContent.includes(lock));
  }
 }
 assert.deepEqual(rendered,all);
 assert.equal(View.combatSelection(s,{page:-100}).page,0);
 assert.equal(View.combatSelection(s,{page:999}).page,Math.ceil(all.length/4)-1);
});
test('legacy saves retain honest previews, names are escaped, and combat records are completely paginated',()=>{
 const s=battle();delete s.combat.rules;s.hero.name='<img src=x onerror=alert(1)>';
 const html=View.renderCombat(s,{category:'defend',skill:'guard',target:'hero'}),document=dom(html);
 assert.equal(document.querySelectorAll('img').length,0);assert(!document.querySelector('.combat-preview').textContent.includes('d20'));
 const log=Array.from({length:19},(_,i)=>'기록 '+i+' <script>');let found=[];
 for(let page=0;page<4;page++){const d=dom(View.renderCombatLog(log,page));found.push(...[...d.querySelectorAll('.combat-log-page p')].map(p=>p.textContent));assert.equal(d.querySelectorAll('script').length,0);}
 assert.deepEqual(found,log);
});
test('sprite enlargement uses integer physical texels and fits supported enemy slots',()=>{
 for(const dpr of [1,1.25,1.5,2,3])for(const [w,h] of [[64,64],[75,130],[180,310],[280,500]]){
  const size=View.combatSpriteFit(w,h,dpr);assert(size<=w&&size<=h);assert.equal(size*dpr/64,Math.round(size*dpr/64));
 }
});
test('actual game renders combat without story pagination and executes the selected hero action',()=>{
 const {document,window}=parseHTML('<html><body><div id="app"></div><dialog id="modal"></dialog><div id="toast"></div></body></html>');
 const source=readFileSync(new URL('../web/app.mjs',import.meta.url),'utf8').replace(/^import .*;\n/gm,'').split("document.addEventListener('click'")[0];
 let pages=0,saves=0;
 const context=vm.createContext({document,window,G,...Content,...View,Store:{load:()=>({state:null,notice:''}),save:()=>saves++},localStorage:{},decoratePaper:()=>()=>{},fitCombatSprites:()=>()=>{},fitPages:()=>{pages++;return ()=>{};},setTimeout,clearTimeout,console});
 vm.runInContext(source+'\nglobalThis.showBattle=s=>{state=s;title=false;tab="journey";render();};globalThis.use=selector=>{const el=document.querySelector(selector);execute(el.dataset.action,el);};',context);
 const s=battle();context.showBattle(s);assert.equal(pages,0);assert.equal(document.querySelectorAll('.combat-attributes .attribute').length,6);assert(!document.querySelector('.character-panel'));
 context.use('[data-category="items"]');assert.equal(document.querySelectorAll('.combat-enemy:disabled').length,2);
 context.use('[data-category="defend"]');assert(document.querySelector('[data-action="act"]').textContent.includes('방어'));
 const before=G.serialize(s);context.use('[data-action="act"]');assert.notEqual(G.serialize(s),before);assert.equal(saves,1);assert.equal(pages,0);
});
