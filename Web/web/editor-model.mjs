import {EVENTS,REGIONS,STAT_NAMES,WEAPONS,SKILLS,COMPANIONS,ENEMIES} from './content.mjs';
export const DRAFT_KEY='arvito.event-workshop.v1', BACKUP_KEY=DRAFT_KEY+'.backup';
export const ART_IDS=Object.keys(EVENTS);
export const NUMBER_EFFECTS={gold:'골드',rations:'식량',potions:'회복약',hp:'주인공 체력',heal:'일행 회복',stress:'일행 긴장',xp:'경험치',rep:'평판',bond:'유대'};
export const ID_OPTIONS={stat:STAT_NAMES,weapon:WEAPONS,skill:SKILLS,recruit:COMPANIONS,region:REGIONS};
export const freshChoice=()=>({label:'',weeks:1,body:'',effects:{}});
export function nextId(events,prefix='event') {let n=1;while(events.some(e=>e.id===`${prefix}_${String(n).padStart(3,'0')}`))n++;return `${prefix}_${String(n).padStart(3,'0')}`;}
export function freshEvent(events=[]){return {id:nextId(events),title:'',place:'',chapter:'',tag:'사건',illustrationId:'',art:'camp',regions:['village'],minStage:0,stageWeights:[10,8,4],once:true,text:[''],choices:[freshChoice(),freshChoice()]};}
const plain=x=>!!x&&typeof x==='object'&&!Array.isArray(x)&&Object.getPrototypeOf(x)===Object.prototype;
const str=x=>typeof x==='string';
const finite=(x,min=-10000,max=10000)=>Number.isFinite(x)&&x>=min&&x<=max;
const identifier=x=>str(x)&&/^[a-z][a-z0-9_]{1,63}$/.test(x)&&!['constructor','prototype','__proto__'].includes(x);
const tables={weapon:WEAPONS,skill:SKILLS,recruit:COMPANIONS,region:REGIONS,stat:STAT_NAMES};
const eventKeys=new Set(['id','title','place','chapter','tag','art','illustrationId','regions','minStage','stageWeights','once','text','choices','minWeeks','minLevel','minRep','minParty','requiredFlag','excludedFlag','minFlagWeeks','requiredWeapon','excludedFallen']);
const choiceKeys=new Set(['label','weeks','body','effects','check','failure','need','battle','repeat','hint']);
export function validateEvent(e,all=[e]){
 const errors=[],warnings=[];const err=m=>errors.push(m);
 if(!plain(e))return {errors:['이벤트 형식이 올바르지 않습니다.'],warnings};
 for(const k of Object.keys(e))if(!eventKeys.has(k))err(`지원하지 않는 이벤트 항목: ${k}`);
 if(!identifier(e.id))err('ID는 영문 소문자로 시작하는 2~64자의 영문·숫자·밑줄이어야 합니다.');
 if(typeof e.once!=='boolean')err('일회성 설정을 확인해 주세요.');
 for(const k of ['chapter','tag','art'])if(e[k]!==undefined&&!str(e[k]))err(`${k}: 문자열이어야 합니다.`);
 if(all.filter(x=>x.id===e.id).length>1)err('같은 ID의 이벤트가 있습니다.');
 for(const [k,label] of [['title','제목'],['place','장소']])if(!str(e[k])||!e[k].trim())err(`${label}을 입력해 주세요.`);
 if(!Array.isArray(e.text)||!e.text.length||!e.text.every(p=>str(p)&&p.trim()))err('본문을 한 문단 이상 입력해 주세요.');
 if(!Array.isArray(e.regions)||!e.regions.every(r=>Object.hasOwn(REGIONS,r)))err('지역을 확인해 주세요.');
 if(![0,1,2].includes(e.minStage))err('최소 진행 단계는 초반·중반·후반 중 하나여야 합니다.');
 if(!Array.isArray(e.stageWeights)||e.stageWeights.length!==3||!e.stageWeights.every(n=>finite(n,0,1000)))err('단계별 가중치는 0~1000이어야 합니다.');
 else if(!e.stageWeights.slice(e.minStage).some(n=>n>0))err('등장 가능한 진행 단계에 양수 가중치가 필요합니다.');
 if(e.illustrationId&&!ART_IDS.includes(e.illustrationId))err('목록에 있는 삽화를 선택해 주세요.');
 if(!e.illustrationId)warnings.push('삽화가 아직 선택되지 않았습니다.');
 for(const k of ['requiredFlag','excludedFlag'])if(e[k]&&!identifier(e[k]))err(`${k}: 표식 ID 형식이 올바르지 않습니다.`);
 if(e.requiredFlag&&e.requiredFlag===e.excludedFlag)err('필수 표식과 제외 표식이 같아 등장할 수 없습니다.');
 for(const k of ['minWeeks','minLevel','minRep','minParty','minFlagWeeks'])if(e[k]!==undefined&&(!Number.isInteger(e[k])||!finite(e[k],0,k==='minParty'?4:10000)))err(`${k}: 0 이상의 유효한 정수가 필요합니다.`);
 if(e.minFlagWeeks&&!e.requiredFlag)err('표식 대기 기간에는 필수 표식이 필요합니다.');
 if(e.requiredWeapon&&!Object.hasOwn(WEAPONS,e.requiredWeapon))err('필수 무기가 올바르지 않습니다.');
 if(e.excludedFallen&&!Object.hasOwn(COMPANIONS,e.excludedFallen))err('사망 시 제외할 동료가 올바르지 않습니다.');
 const effects=(fx,label)=>{
  if(!plain(fx)){err(`${label}: 결과 수치 형식이 올바르지 않습니다.`);return;}
  for(const [k,v] of Object.entries(fx)){
   if(Object.hasOwn(NUMBER_EFFECTS,k)){if(!Number.isInteger(v)||!finite(v)||(['heal','xp'].includes(k)&&v<0))err(`${label}: ${NUMBER_EFFECTS[k]} 수치를 확인해 주세요.`);}
   else if(Object.hasOwn(tables,k)){if(!Object.hasOwn(tables[k],v))err(`${label}: ${k} 값을 확인해 주세요.`);}
   else if(k==='flag'){if(!identifier(v))err(`${label}: 획득 표식 ID를 확인해 주세요.`);}
   else if(k==='fullHeal'){if(typeof v!=='boolean')err(`${label}: 완전 회복 형식을 확인해 주세요.`);}
   else err(`${label}: 지원하지 않는 결과 항목 ${k}`);
  }
 };
 if(!Array.isArray(e.choices)||e.choices.length<2||e.choices.length>6)err('선택지는 2~6개로 작성해 주세요.');
 else e.choices.forEach((c,i)=>{
  const label=`선택 ${i+1}`;if(!plain(c)){err(`${label}의 형식이 올바르지 않습니다.`);return;}
  for(const k of Object.keys(c))if(!choiceKeys.has(k))err(`${label}: 지원하지 않는 항목 ${k}`);
  if(!str(c.label)||!c.label.trim())err(`${label}: 행동을 입력해 주세요.`);
  if(!str(c.body)||!c.body.trim())err(`${label}: 결과 본문을 입력해 주세요.`);
  if(!Number.isInteger(c.weeks)||!finite(c.weeks,1,520))err(`${label}: 소요 시간은 1~520주여야 합니다.`);
  effects(c.effects,label);
  if(c.check){
   if(!plain(c.check)||!Object.hasOwn(STAT_NAMES,c.check.stat)||!Number.isInteger(c.check.dc)||!finite(c.check.dc,1,40))err(`${label}: 판정 능력치·난이도를 확인해 주세요.`);
   if(!plain(c.failure)||!str(c.failure.body)||!c.failure.body.trim())err(`${label}: 실패 결과를 입력해 주세요.`);
   else effects(c.failure.effects,`${label} 실패`);
  }
  if(c.need){if(!plain(c.need))err(`${label}: 필요 조건의 형식이 올바르지 않습니다.`);else for(const [k,v] of Object.entries(c.need)){
   if(['gold','rations','rep'].includes(k)){if(!Number.isInteger(v)||!finite(v,0))err(`${label}: 필요 수치를 확인해 주세요.`);}
   else if(k==='flag'){if(!identifier(v))err(`${label}: 필요 표식 ID를 확인해 주세요.`);}
   else if(k==='weapon'){if(!Object.hasOwn(WEAPONS,v))err(`${label}: 필요 무기를 확인해 주세요.`);}
   else if(k==='partySpace'){if(typeof v!=='boolean')err(`${label}: 일행 자리 조건을 확인해 주세요.`);}
   else err(`${label}: 지원하지 않는 필요 조건 ${k}`);
  }}
  if(c.battle&&(!Array.isArray(c.battle)||c.battle.length>4||!c.battle.every(id=>Object.hasOwn(ENEMIES,id))))err(`${label}: 적은 목록에서 최대 4명까지 고르세요.`);
  for(const fx of [c.effects,c.failure?.effects].filter(plain))for(const k of ['gold','rations'])if(fx[k]<0&&!(c.need?.[k]>=-fx[k]))err(`${label}: ${-fx[k]} ${k==='gold'?'골드':'식량'} 소모에 맞는 필요 조건을 넣어 주세요.`);
 });
 if(e.choices?.every(c=>c.need&&Object.values(c.need).some(Boolean)))warnings.push('모든 선택지에 조건이 있습니다. 자원이 없는 플레이어가 막히지 않는지 확인하세요.');
 if(e.requiredFlag)warnings.push('필수 표식을 얻는 선행 사건이 있는지 확인하세요.');
 return {errors,warnings};
}
function safeTree(x,depth=0){if(depth>14)throw Error('파일 구조가 너무 깊습니다.');if(x&&typeof x==='object')for(const [k,v] of Object.entries(x)){if(['__proto__','constructor','prototype'].includes(k))throw Error('허용되지 않는 파일 항목입니다.');safeTree(v,depth+1);}}
export function parsePack(raw){
 if(typeof raw!=='string'||raw.length>3_000_000)throw Error('파일은 3MB 이하여야 합니다.');
 const pack=JSON.parse(raw);safeTree(pack);
 if(!plain(pack)||!['arvito-event-draft','arvito-event-pack'].includes(pack.format)||pack.version!==1||!Array.isArray(pack.events)||pack.events.length>1000)throw Error('아르비토 이벤트 JSON 파일이 아닙니다.');
 for(const e of pack.events){if(!plain(e)||!str(e.id)||!str(e.title)||!Array.isArray(e.text)||!e.text.every(str)||!Array.isArray(e.choices)||e.choices.length>6||!e.choices.every(c=>plain(c)&&str(c.label)&&str(c.body)&&plain(c.effects)&&(!c.failure||(plain(c.failure)&&str(c.failure.body)&&plain(c.failure.effects))))||!Array.isArray(e.regions)||!Array.isArray(e.stageWeights))throw Error('이벤트 편집 데이터의 구조가 올바르지 않습니다.');}
 if(new Set(pack.events.map(e=>e.id)).size!==pack.events.length)throw Error('파일 안에 중복 이벤트 ID가 있습니다.');
 return structuredClone(pack.events);
}
export function packEvents(events,ready=false){
 if(ready){const issues=events.flatMap(e=>validateEvent(e,events).errors.map(m=>`${e.id}: ${m}`));if(!events.length)issues.push('이벤트가 없습니다.');if(issues.length)throw Error(issues.slice(0,8).join('\n'));}
 return JSON.stringify({format:ready?'arvito-event-pack':'arvito-event-draft',version:1,events},null,2);
}
export function loadDraft(storage){
 try{
  const raw=storage.getItem(DRAFT_KEY);if(!raw)return {events:[],warning:''};
  try{return {events:parsePack(raw),warning:''};}catch{
   const backup=storage.getItem(BACKUP_KEY);if(backup)return {events:parsePack(backup),warning:'초안을 읽지 못해 이전 자동 저장본을 복구했습니다.'};
   return {events:[],warning:'저장된 초안을 읽지 못했습니다. 원본을 덮어쓰기 전에 백업 파일을 확인해 주세요.',blocked:true};
  }
 }catch{return {events:[],warning:'브라우저 저장을 사용할 수 없습니다. JSON으로 초안을 보관해 주세요.',blocked:true};}
}
export function saveDraft(storage,events){
 const next=packEvents(events);parsePack(next);
 const previous=storage.getItem(DRAFT_KEY);if(previous===next)return;
 if(previous){let valid=false;try{parsePack(previous);valid=true;}catch{}if(valid)storage.setItem(BACKUP_KEY,previous);}
 storage.setItem(DRAFT_KEY,next);
}
