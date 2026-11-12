import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import {parseHTML} from 'linkedom';
import {fitPages,illustrationCover} from '../web/viewport.mjs';
import * as G from '../web/engine.mjs';
import * as Content from '../web/content.mjs';

// These are DOM/behavior checks with explicit synthetic measurements, NOT
// browser rendering or CSS layout tests. Actual browser QA is recorded separately.
function fixture(markup, height = 160) {
 const {document,window}=parseHTML(`<html><body><div id="view">${markup}</div><nav id="controls" hidden></nav></body></html>`);
 const view=document.querySelector('#view'),controls=document.querySelector('#controls');
 document.fonts={ready:new Promise(()=>{})};
 Object.assign(globalThis,{document,window,ResizeObserver:class{observe(){}disconnect(){}},requestAnimationFrame:()=>0,cancelAnimationFrame:()=>{}});
 const measure=node=>node.matches('p,h1,h2,small,button')?Math.max(20,Math.ceil(node.textContent.length/20)*20):[...node.children].reduce((h,n)=>h+measure(n),0);
 Object.defineProperties(view,{clientHeight:{get:()=>height-(controls.hidden?0:28)},clientWidth:{get:()=>400},scrollWidth:{get:()=>400},scrollHeight:{get:()=>[...view.children].reduce((h,n)=>h+measure(n),0)}});
 return {view,controls};
}
function readPages(view,controls){const pages=[];for(let safety=0;safety<150;safety++){
 assert(view.scrollHeight<=view.clientHeight+1,`page ${pages.length} height ${view.scrollHeight}/${view.clientHeight}: ${view.innerHTML.slice(0,500)}`);
 pages.push(view.innerHTML);
 const next=controls.querySelector('button:last-child');if(!next||next.disabled)return pages;
 next.onclick();
 }throw Error('pagination never terminated');}

test('pagination preserves long prose and every actionable choice',()=>{
 const text='긴 기록에서도 문장이 사라지지 않아야 한다. '.repeat(45);
 const {view,controls}=fixture(`<div class="event-grid"><section class="story"><p>${text}</p></section><section class="event-choices"><div class="choices">${Array.from({length:4},(_,i)=>`<button data-action="select" data-index="${i}">선택 ${i+1}</button>`).join('')}</div><button data-action="confirm">선택 확정</button></section></div>`);
 const cleanup=fitPages(view,controls,'long-story-test');
 const pages=readPages(view,controls);assert(pages.length>1);
 const {document}=parseHTML('<html><body>'+pages.join('')+'</body></html>');
 assert.equal([...document.querySelectorAll('.story p')].map(p=>p.textContent).join(''),text);
 assert.deepEqual([...document.querySelectorAll('[data-action="select"]')].map(x=>x.dataset.index),['0','1','2','3']);
 assert.equal(document.querySelectorAll('[data-action="confirm"]').length,1);
 cleanup();
});
test('short content stays on one page without navigation controls',()=>{
 const {view,controls}=fixture('<p>짧은 사건</p><button data-action="continue">계속</button>',300);
 const cleanup=fitPages(view,controls,'short-story-test');assert(controls.hidden);assert.equal(view.querySelector('p').textContent,'짧은 사건');cleanup();
});
test('all event screens preserve story and choices with the approved right summary',()=>{
 const {document,window}=parseHTML('<html><body><div id="app"></div><dialog id="modal"></dialog><div id="toast"></div></body></html>');
 const source=readFileSync(new URL('../web/app.mjs',import.meta.url),'utf8').replace(/^import .*;\n/gm,'').split("document.addEventListener('click'")[0];
 const context=vm.createContext({document,window,G,...Content,Store:{load:()=>({state:null,notice:''})},localStorage:{},decoratePaper:()=>()=>{},fitPages:()=>()=>{},setTimeout,clearTimeout,console});
 vm.runInContext(source+'\nglobalThis.showEvent=s=>{state=s;title=false;tab="journey";render();};',context);
 for(const event of Object.values(Content.EVENTS)){
  const state=G.newGame('테스트',55);state.eventId=event.id;context.showEvent(state);
  assert.equal(document.querySelector('.brand').textContent,'ARVITO');
  assert.equal(document.querySelectorAll('.character-panel .attribute').length,6);
  assert.equal(document.querySelectorAll('#game-view [data-action="select"]').length,event.choices.length,event.id);
  assert.deepEqual([...document.querySelectorAll('.story>p')].map(p=>p.textContent),event.text,event.id);
  assert.equal(document.querySelectorAll('.event-choices [data-action="confirm"]').length,1,event.id);
 }
});


test('illustration covers its frame with square integer physical pixels at every zoom',()=>{
 for(const dpr of [1,1.25,1.5,2,3])for(const [w,h] of [[840,720],[960,850],[374,240],[320,460]]){
  const box=illustrationCover(w,h,dpr);
  assert(box.width>=w&&box.height>=h);
  assert(Math.abs(box.width/box.height-1.5)<1e-9);
  for(const value of [box.width/192*dpr,box.height/128*dpr,box.left*dpr,box.top*dpr])assert(Math.abs(value-Math.round(value))<1e-9);
  assert(box.left<=0&&box.top<=0);assert(box.left+box.width>=w-1e-9&&box.top+box.height>=h-1e-9);
 }
});
test('illustration remains visible on every reading page without duplicating choices or losing prose',()=>{
 const text='새로운 사건의 본문을 끝까지 읽습니다. '.repeat(35);
 const {view,controls}=fixture(`<div class="event-grid"><section class="event-art"><figure><div class="picture"><img src="art/Events/q080_offer.png" alt="인장을 삼킨 상자"></div></figure></section><section class="story"><h1>인장을 삼킨 상자</h1><p>${text}</p></section><section class="event-choices"><button data-action="select">상자와 협상한다</button><button data-action="confirm">선택 확정</button></section></div>`);
 const cleanup=fitPages(view,controls,'persistent-illustration');
 assert(view.classList.contains('illustrated'));
 const art=view.querySelector('.event-backdrop');assert(art);
 const pages=readPages(view,controls);assert(pages.length>1);
 assert.equal(view.querySelector('.event-backdrop'),art);
 for(const html of pages){const {document}=parseHTML('<html><body>'+html+'</body></html>');assert.equal(document.querySelectorAll('.event-backdrop img').length,1);}
 const {document}=parseHTML('<html><body>'+pages.join('')+'</body></html>');
 assert.equal([...document.querySelectorAll('.story p')].map(p=>p.textContent).join(''),text);
 assert.equal(document.querySelectorAll('[data-action="select"]').length,1);
 assert.equal(document.querySelectorAll('[data-action="confirm"]').length,1);
 cleanup();
});
