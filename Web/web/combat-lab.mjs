import * as G from './engine.mjs';
import {WEAPONS,SKILLS,COMPANIONS,ENEMIES,STAT_NAMES} from './content.mjs';
import {DEFAULT_SETUP,PRESETS,LAB_KEY,normalizeSetup,createLabBattle,serializeSetup,parseSetup,runBatch} from './combat-lab-model.mjs';
import {RULE_LIMITS} from './combat-rules.mjs';
import {combatSelection, chooseCombatCommand, combatAttributes, renderCombat, fitCombatSprites, renderCombatLog} from './combat-view.mjs';
const $=s=>document.querySelector(s),esc=v=>String(v??'').replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
let config=structuredClone(DEFAULT_SETUP),run=null,combatChoice={},storage,fitCleanup;
function message(text,error=false){$('#lab-message').textContent=text;$('#lab-message').classList.toggle('error',error);}
try{storage=localStorage;const raw=storage.getItem(LAB_KEY);if(raw)config=parseSetup(raw);}catch{message('저장된 전투 설정을 읽지 못했습니다. 기본 설정으로 시작합니다.',true);}
const field=(label,path,value,min,max,step=1)=>`<label>${esc(label)}<input data-field="${path}" type="number" value="${value}" min="${min}" max="${max}" step="${step}"></label>`;
const options=(table,value,empty=false)=>(empty?'<option value="">배치 안 함</option>':'')+Object.entries(table).map(([id,item])=>`<option value="${id}" ${value===id?'selected':''}>${esc(item.name)}</option>`).join('');
function renderSetup(){const preset=Object.keys(PRESETS).find(k=>JSON.stringify(PRESETS[k].enemies)===JSON.stringify(config.enemies));
 $('#setup').innerHTML=`<div class="setup-grid"><label class="wide">적 조합<select id="scenario"><option value="">직접 구성</option>${Object.entries(PRESETS).map(([id,p])=>`<option value="${id}" ${id===preset?'selected':''}>${esc(p.name)}</option>`).join('')}</select></label>${Array.from({length:4},(_,i)=>`<label>적 ${i+1}<select data-enemy="${i}">${options(ENEMIES,config.enemies[i]||'',true)}</select></label>`).join('')}${field('시드','seed',config.seed,1,4294967295)}${field('성장 단계','level',config.level,1,20)}${field('기본 최대 체력','hp',config.hp,1,500)}${field('회복약','potions',config.potions,0,20)}<label class="wide">주인공 무기<select data-field="weapon">${options(WEAPONS,config.weapon)}</select></label>${field('주인공 위치 (열)','heroRank',config.heroRank,1,4)}</div><div class="stats-grid">${Object.entries(STAT_NAMES).map(([k,label])=>field(label,'stats.'+k,config.stats[k],0,30)).join('')}</div><p class="rule-note">능력치 4 → 보정 0, 6 → +1. 건강은 4보다 1 높을 때 최대 체력 +2.</p><h3 style="margin-top:16px">자동 행동 동료</h3><div class="chips">${Object.values(COMPANIONS).map(a=>`<label><input type="checkbox" data-companion="${a.id}" ${config.companions.includes(a.id)?'checked':''}>${esc(a.name)}</label>`).join('')}</div><details><summary>d20 규칙 조정</summary><div class="setup-grid" style="margin-top:10px">${Object.entries({attackBonus:'기본 공격 보정',defenseBase:'기본 방어도',guardBonus:'방어 행동 보너스',criticalMultiplier:'치명타 피해 배수',enemyHealthScale:'적 체력 배수',enemyDamageScale:'적 피해 배수'}).map(([k,label])=>field(label,'rules.'+k,config.rules[k],...RULE_LIMITS[k],k.endsWith('Scale')||k==='criticalMultiplier'?.25:1)).join('')}</div><p class="rule-note">공격: d20 + 기본 보정 + 성장 보정 + 능력치 보정 + 기술 보정.<br>방어: 기본 방어도 + 민첩 보정 + 방어구. 마법은 민첩·방어구 대신 지혜 보정.<br>1은 자동 빗나감, 20은 자동 명중·치명타. 동점은 명중.</p></details>`;
}
function readSetup(){const next={stats:{},rules:{},enemies:[...document.querySelectorAll('[data-enemy]')].map(e=>e.value).filter(Boolean),companions:[...document.querySelectorAll('[data-companion]:checked')].map(e=>e.dataset.companion)};for(const el of document.querySelectorAll('[data-field]')){const [group,key]=el.dataset.field.split('.'),value=el.type==='number'?Number(el.value):el.value;if(key)next[group][key]=value;else next[group]=value;}return normalizeSetup(next);}
function saveSetup(){try{storage?.setItem(LAB_KEY,serializeSetup(config));if(!storage)throw Error();}catch{message('설정 자동 저장을 사용할 수 없습니다. 설정 내보내기로 보관하세요.',true);}}
function start(){config=readSetup();saveSetup();run=createLabBattle(config);combatChoice={};document.body.classList.remove('is-preparing');render();}
function render(){
 fitCleanup?.();
 const active=document.activeElement?.dataset,focus=active?.action?{...active}:null;
 const inCombat=run?.state.mode==='combat';
 $('#actions').hidden=!!inCombat;
 if(!run){$('#arena').innerHTML='<div class="battle-ended"><h2>전투를 구성하세요</h2><p>적과 동료를 고른 뒤 시작하세요.</p></div>';$('#actions').innerHTML='';$('#combat-log').innerHTML='';return;}
 const s=run.state,c=run.combat;
 if(inCombat){
  combatChoice=combatSelection(s,combatChoice);
  $('#arena').innerHTML=combatAttributes(s)+renderCombat(s,combatChoice);
  fitCleanup=fitCombatSprites(document.querySelector('.parchment-combat'));
  if(focus){const next=[...document.querySelectorAll('#arena [data-action]')].find(e=>e.dataset.action===focus.action&&e.dataset.id===focus.id&&e.dataset.category===focus.category);if(next&&!next.disabled)next.focus({preventScroll:true});}
 }else{
  $('#arena').innerHTML=`<div class="battle-ended"><h2>${s.outcome?.combatWin?'승리':s.mode==='ended'?'패배':'퇴각'}</h2><p>주인공 남은 체력 ${s.hero.hp}/${s.hero.maxHp} · ${c.round}라운드</p></div>`;
  $('#actions').innerHTML='<button class="primary" data-action="restart">같은 설정과 시드로 다시</button><button data-action="toggle-setup">전투 준비</button>';
 }
 $('#combat-log').innerHTML=c.log.map(line=>`<p>${esc(line)}</p>`).join('');
}
function showDialog(title,body){const dialog=$('#lab-dialog');dialog.innerHTML=`<h2>${title}</h2><div class="combat-log-content">${body}</div><button data-action="close-dialog">돌아가기</button>`;if(!dialog.open)dialog.showModal();}

document.addEventListener('change',ev=>{try{if(ev.target.id==='scenario'&&PRESETS[ev.target.value]){config={...config,enemies:[...PRESETS[ev.target.value].enemies]};renderSetup();saveSetup();message('다음 전투에 적용됩니다.');}else if(ev.target.closest('#setup')){config=readSetup();saveSetup();message('설정을 저장했습니다. 다음 전투에 적용됩니다.');}}catch(error){message(error.message,true);}});
document.addEventListener('click',async ev=>{const button=ev.target.closest('[data-action]');if(!button||button.disabled)return;const action=button.dataset.action;try{
 if(action==='start'){start();message('시험 전투를 시작했습니다.');}
 else if(action==='restart'){if(run){config=structuredClone(run.config);renderSetup();}start();message('같은 설정과 시드로 다시 시작했습니다.');}
 else if(action==='new-seed'){config=readSetup();config.seed=(Date.now()>>>0)||1;renderSetup();start();message('새 시드로 시작했습니다.');}
 else if(action==='toggle-setup'){document.body.classList.toggle('is-preparing');}
 else if(action==='combat-command'&&run?.state.mode==='combat'){combatChoice=chooseCombatCommand(run.state,combatChoice,button.dataset.category);render();}
 else if(action==='combat-skill-page'){combatChoice.page=Number(button.dataset.page);render();}
 else if(action==='skill'){combatChoice={...combatChoice,skill:button.dataset.id,category:'skills'};render();}
 else if(action==='target'){combatChoice.target=button.dataset.id;render();}
 else if(action==='act'&&run?.state.mode==='combat'){G.act(run.state,combatChoice.skill,combatChoice.target);combatChoice={};render();}
 else if(action==='flee'&&run?.state.mode==='combat'){showDialog('퇴각','<p>이 시험 전투에서 물러날까요?</p><button data-action="confirm-retreat">퇴각한다</button>');}
 else if(action==='confirm-retreat'&&run?.state.mode==='combat'){G.flee(run.state);$('#lab-dialog').close();render();}
 else if(action==='combat-help'){showDialog(esc(SKILLS[button.dataset.id].name),`<p>${esc(G.skillDescription(run.state,button.dataset.id))}</p>`);}
 else if(action==='combat-log'||action==='combat-log-page'){showDialog('전투 기록',renderCombatLog(run.combat.log,button.dataset.page===undefined?undefined:Number(button.dataset.page)));}
 else if(action==='stat-info'){const key=button.dataset.stat;showDialog(STAT_NAMES[key],`<p>현재 능력치 ${run.state.hero.stats[key]} · 보정 ${G.abilityModifier(run.state.hero.stats[key])}</p>`);}
 else if(action==='close-dialog')$('#lab-dialog').close();
 else if(action==='batch'){const setup=readSetup();button.disabled=true;$('#batch-result').textContent='30회 시험 중…';await new Promise(resolve=>setTimeout(resolve,0));const result=runBatch(setup,30);$('#batch-result').textContent=`승리 ${result.wins} · 패배 ${result.losses} · 시간 초과 ${result.timeouts}\n승률 ${result.winRate}% · 평균 ${result.averageRounds}라운드\n평균 남은 체력 ${result.averageHp}`;button.disabled=false;}
 else if(action==='export'){const blob=new Blob([serializeSetup(readSetup())],{type:'application/json'}),url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='arvito-combat-setup.json';a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
 else if(action==='import')$('#setup-file').click();
 }catch(error){button.disabled=false;message(error.message,true);}});
$('#setup-file').addEventListener('change',async ev=>{const file=ev.target.files?.[0];if(!file)return;try{if(file.size>50000)throw Error('설정 파일은 50KB 이하여야 합니다.');config=parseSetup(await file.text());renderSetup();saveSetup();message('설정을 불러왔습니다. 시작 버튼을 누르면 적용됩니다.');}catch(error){message(error.message,true);}finally{ev.target.value='';}});
renderSetup();render();
