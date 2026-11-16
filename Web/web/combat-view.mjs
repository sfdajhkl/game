import * as G from './engine.mjs';
import {SKILLS, STAT_NAMES, STAT_DETAILS} from './content.mjs';
import {physicalDamage, signed} from './combat-rules.mjs';

const esc = value => String(value ?? '').replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
const slots = {hero:0,mira:1,elian:2,borin:3,bandit:4,archer:5,skeleton:6,cultist:7,wolf:8,slime:9,boar:10,spider:11,wisp:12,warden:13,dragon:14,hero_female:16,elian_female:17,borin_female:18,bandit_female:19,archer_female:20,cultist_female:21};
const paths = {
 attack:'m4 20 5-5m-4-3 7 7m-3-4L19 3l2 2-12 10M3 21l2-2',
 skills:'m12 2 3 7 7 3-7 3-3 7-3-7-7-3 7-3 3-7Z',
 defend:'m12 2 8 3v7c0 5-8 10-8 10S4 17 4 12V5l8-3Zm0 4v12',
 items:'M9 2h6v5l5 7v7H4v-7l5-7V2Zm0 2h6M5 15h14',
 retreat:'m9 5 4-2 3 3-1 5 5 3m-6-6-4 5-5-1m5 1 1 5-5 3m9-10-1 8 5 2',
 log:'M5 3h14v18H5V3Zm4 5h6m-6 4h6m-6 4h4',
 strength:'m5 18 2-8 4-4 3 2-2 4 6 1 2 5H5Z',agility:'M4 20 19 5M6 16C2 5 13 2 20 3c1 8-5 15-14 13Z',
 intelligence:'M3 4h7l2 2 2-2h7v16h-7l-2 1-2-1H3V4Zm9 2v15',wisdom:'M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Zm13 0a3 3 0 1 0-6 0 3 3 0 0 0 6 0',charisma:'m3 7 4 3 5-7 5 7 4-3-2 13H5L3 7Z',constitution:'M20 5c-3-3-7-1-8 1-1-2-5-4-8-1-5 5 4 12 8 15 4-3 13-10 8-15Z'
};
const icon = name => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" aria-hidden="true"><path d="${paths[name] || paths.skills}"/></svg>`;
const sprite = actor => {const n=slots[G.spriteId(actor)]??0;return `<span class="combat-sprite" role="img" aria-label="${esc(actor.name)}" style="--sprite-col:${n%4};--sprite-row:${Math.floor(n/4)}"></span>`;};
const hp = actor => `<span class="combat-hp" aria-label="체력 ${actor.hp}/${actor.maxHp}"><i style="width:${G.clamp(actor.hp/actor.maxHp*100,0,100)}%"></i><span>${actor.hp} / ${actor.maxHp}</span></span>`;
const button = (action, text, attrs='', cls='') => `<button type="button" data-action="${action}" class="${cls}" ${attrs}>${text}</button>`;
const skillsFor = s => G.availableSkills(s,s.hero);

// UI selection only. These functions never roll dice, act, or write a save.
export function combatSelection(s, choice={}) {
 const all=skillsFor(s), legal=all.filter(id=>!G.skillLock(s,id));
 const category=['attack','skills','defend','items'].includes(choice.category)?choice.category:'attack';
 const defaultAttack=legal.find(id=>SKILLS[id].power && !SKILLS[id].cost) || 'strike';
 const defaults={attack:defaultAttack,skills:defaultAttack,defend:'guard',items:'potion'};
 const skill=all.includes(choice.skill)?choice.skill:defaults[category];
 const targets=G.validTargets(s,skill,'hero');
 const target=targets.some(a=>a.id===choice.target)?choice.target:targets[0]?.id || '';
 return {category,skill,target,page:Math.max(0,Math.min(Math.ceil(all.length/4)-1,Math.trunc(choice.page)||0))};
}
export function chooseCombatCommand(s, choice, category) {
 return combatSelection(s,{...choice,category,skill:'',page:0});
}
export function combatAttributes(s) {
 return `<section class="combat-attributes" aria-label="주인공 능력치">${Object.entries(STAT_NAMES).map(([key,label])=>button('stat-info',`${icon(key)}<span>${label}</span><strong>${s.hero.stats[key]}</strong>`,`data-stat="${key}" style="--stat-color:${STAT_DETAILS[key].color}" aria-label="${label} ${s.hero.stats[key]}, 설명 보기"`)).join('')}</section>`;
}

export function renderCombat(s, choice={}) {
 const c=s.combat, ui=combatSelection(s,choice), sk=SKILLS[ui.skill];
 const targets=G.validTargets(s,ui.skill,'hero'), selected=G.actorById(s,ui.target), canTarget=id=>targets.some(a=>a.id===id);
 const lock=G.skillLock(s,ui.skill), preview=selected&&sk.power&&c.rules?G.attackPreview(s.hero,selected,sk,c.rules):null;
 const all=skillsFor(s), pages=Math.ceil(all.length/4);
 const status=a=>[a.status.bleed?'출혈':'',a.status.stun?'기절':'',a.status.guard?'방어':''].filter(Boolean).join(' · ');
 const commands=ui.category==='skills'
  ? `<div class="combat-skill-picker"><div class="combat-picker-heading">${button('combat-command','← 행동','data-category="attack"')}<span>기술 ${ui.page+1}/${pages}</span></div><div class="combat-skill-grid">${all.slice(ui.page*4,ui.page*4+4).map(id=>{const reason=G.skillLock(s,id);return button('skill',`<strong>${esc(SKILLS[id].name)}</strong><small>${esc(reason||'기력 '+SKILLS[id].cost)}</small>`,`data-id="${id}" aria-pressed="${ui.skill===id}" title="${esc(reason||G.skillDescription(s,id))}" ${reason?'disabled':''}`,ui.skill===id?'is-selected':'');}).join('')}</div><div class="combat-picker-pages">${button('combat-skill-page','←','aria-label="이전 기술" data-page="'+(ui.page-1)+'" '+(ui.page===0?'disabled':''))}${button('combat-skill-page','→','aria-label="다음 기술" data-page="'+(ui.page+1)+'" '+(ui.page===pages-1?'disabled':''))}</div></div>`
  : `<div class="combat-command-list">${[['attack','공격'],['skills','기술'],['defend','방어'],['items','도구']].map(([id,label])=>button('combat-command',`${icon(id)}<span>${label}</span>${id==='items'?`<small>${s.potions}</small>`:''}`,`data-category="${id}" aria-pressed="${ui.category===id}"`,ui.category===id?'is-selected':'')).join('')}</div>`;
 return `<section class="parchment-combat" aria-label="전투">
 <div class="combat-stage"><div class="combat-enemies enemy-row" style="--enemy-count:${c.enemies.length}">${c.enemies.map(e=>button('target',`<span class="combat-enemy-figure"><span class="combat-pose"><span class="combat-intent">${esc(c.rules?G.enemyIntent(s,e):status(e)||'전투 중')}</span><span class="combat-sprite-frame">${sprite(e)}</span></span></span><strong class="combat-enemy-name">${esc(e.name)}</strong>${hp(e)}<small class="combat-status">${esc(status(e))}</small>`,`data-id="${e.id}" aria-pressed="${ui.target===e.id}" aria-label="${esc(e.name)}${e.hp<=0?' 쓰러짐':', 체력 '+e.hp+'/'+e.maxHp+(canTarget(e.id)?', 대상 선택':', 이 기술로 선택 불가')}" ${e.hp<=0||!canTarget(e.id)?'disabled':''}`,'combat-enemy combatant'+(ui.target===e.id?' is-selected':'')+(e.hp<=0?' is-defeated':''))).join('')}</div></div>
 <div class="combat-party" aria-label="일행 상태" style="--party-count:${G.party(s).length}">${G.party(s).map((a,i)=>button('target',`${sprite(a)}<span class="combat-ally-info"><span class="combat-ally-heading"><strong>${esc(a.name)}</strong><small>${a.id==='hero'?'주인공':'자동'}</small></span>${hp(a)}<span class="combat-ally-meta"><span>${i+1}열${status(a)?' · '+status(a):''}</span>${a.id==='hero'?`<span>기력 ${a.energy}/5</span>`:''}</span>${a.id==='hero'?`<span class="combat-energy" aria-label="기력 ${a.energy}/5">${Array.from({length:5},(_,i)=>`<i class="${i<a.energy?'filled':''}"></i>`).join('')}</span>`:''}</span>`,`data-id="${a.id}" aria-pressed="${ui.target===a.id}" ${!canTarget(a.id)?'disabled':''}`,'combat-ally'+(a.id==='hero'?' is-hero':'')+(ui.target===a.id?' is-selected':'')+(a.hp<=0?' is-defeated':''))).join('')}</div>
 <aside class="combat-controls" aria-label="주인공 행동">
  <div class="combat-round"><strong>${c.round} 라운드</strong><span>${esc(s.hero.name)}의 차례</span></div>
  <section class="combat-target" aria-label="대상 정보"><h2>대상 정보</h2>${selected?`<div class="combat-target-card">${sprite(selected)}<div><strong>${esc(selected.name)}</strong>${hp(selected)}<small>${c.rules?(sk.magic?'정신 방어 ':'방어도 ')+G.defenseValue(selected,sk,c.rules):'체력 '+selected.hp+'/'+selected.maxHp}</small></div></div>`:'<p>대상을 선택하세요</p>'}</section>
  <div class="combat-preview target-preview" aria-live="polite">${preview?`<strong>명중 ${preview.chance}%</strong><span>d20 ${signed(preview.bonus)} / 방어 ${preview.defense}</span><small>피해 ${physicalDamage(s.hero,sk,-1)}–${physicalDamage(s.hero,sk,2)}</small>`:`<strong>${esc(sk.name)}</strong><span>${sk.heal?'회복 행동':sk.guard?'자신을 방어':sk.move?'동료와 교대':sk.focus?'기력 회복':sk.calm?'긴장 완화':'대상을 선택하세요'}</span>`}</div>
  <div class="combat-commands">${commands}</div>
  ${button('combat-help',`${esc(sk.name)} · 기력 ${sk.cost} <span aria-hidden="true">ⓘ</span>`,`data-id="${ui.skill}" aria-label="${esc(sk.name)} 설명 보기"`,'combat-detail')}
  ${button('act',lock?esc(lock):`${esc(sk.name)} 실행 →`,`${lock||!ui.target?'disabled':''}`,'combat-execute')}
  <div class="combat-footer">${button('flee',icon('retreat')+'<span>퇴각</span>')}${button('combat-log',icon('log')+'<span>기록</span>')}</div>
 </aside></section>`;
}

export function renderCombatLog(log, requestedPage) {
 const pages=Math.max(1,Math.ceil(log.length/6)),page=Math.max(0,Math.min(pages-1,requestedPage??pages-1));
 return `<div class="combat-log-page" role="log">${log.slice(page*6,page*6+6).map(line=>`<p>${esc(line)}</p>`).join('')}</div><div class="combat-log-navigation">${button('combat-log-page','← 이전',`data-page="${page-1}" ${page===0?'disabled':''}`)}<span>${page+1} / ${pages}</span>${button('combat-log-page','다음 →',`data-page="${page+1}" ${page===pages-1?'disabled':''}`)}</div>`;
}

// One art texel is always an integer number of physical display pixels.
export function combatSpriteFit(width,height,dpr=1) {
 const scale=Math.max(1,Math.floor(Math.min(width,height)*dpr/64));
 return 64*scale/dpr;
}
export function fitCombatSprites(root) {
 if(typeof ResizeObserver==='undefined')return ()=>{};
 let raf=0,disposed=false;
 const fit=()=>{raf=0;if(disposed||!root.isConnected)return;for(const figure of root.querySelectorAll('.combat-enemy-figure')){
  const label=figure.querySelector('.combat-intent'),sprite=figure.querySelector('.combat-sprite');
  const size=combatSpriteFit(Math.max(64,figure.clientWidth-10),Math.max(64,figure.clientHeight-(label?.offsetHeight||22)-12),window.devicePixelRatio||1);
  sprite.style.setProperty('--sprite-size',size+'px');
 }};
 const schedule=()=>{if(!raf)raf=requestAnimationFrame(fit);};
 const observer=new ResizeObserver(schedule);observer.observe(root);document.fonts?.ready.then(schedule);window.addEventListener('resize',schedule);fit();
 return ()=>{disposed=true;observer.disconnect();cancelAnimationFrame(raf);window.removeEventListener('resize',schedule);};
}
