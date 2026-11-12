import {WEAPONS,SKILLS,COMPANIONS,ENEMIES,EVENTS,STAT_NAMES,REGIONS} from './content.mjs';

export const VERSION=1;
export const clamp=(n,a,b)=>Math.max(a,Math.min(b,n));
export const ageOf=s=>16+Math.floor(s.weeks/52);
export const ageText=s=>`${ageOf(s)}세 ${s.weeks%52}주`;
export const duration=n=>n>=52?`${Math.floor(n/52)}년${n%52?' '+n%52+'주':''}`:`${n}주`;
export function random(s){let x=s.rng>>>0; x^=x<<13;x^=x>>>17;x^=x<<5;s.rng=x>>>0;return s.rng/4294967296;}
const roll=(s,a,b)=>a+Math.floor(random(s)*(b-a+1));
export function naturalAnnualRisk(age,vitality=1){return age<40?0:Math.min(.98,.0015*Math.exp((age-40)/8)/vitality);}
export const weeklyMortality=(age,vitality=1)=>1-Math.pow(1-naturalAnnualRisk(age,vitality),1/52);
export const stressTaken=(s,actor,amount)=>Math.max(1,amount-Math.floor((actor.id==='hero'?s.hero.stats.will:4)/4));
export function newGame(name='여행자',seed=Date.now(),femaleAppearance=false){
 const hero={id:'hero',name:(name.trim()||'여행자').slice(0,12),subtitle:'이름 없는 모험가',weapon:'sword',maxHp:38,hp:38,stress:0,energy:4,speed:3,power:1,portrait:'hero',femaleAppearance,status:{},skills:[],level:1,xp:0,stats:{strength:4,agility:4,wit:4,will:4},mastery:{}};
 const s={version:VERSION,rng:(seed>>>0)||1,weeks:0,vitality:1,hero,companions:[],formation:['hero'],fallen:[],inventory:['sword'],gold:24,rations:4,potions:3,rep:0,bond:0,flags:{},flagWeeks:{},seen:{},recent:[],eventId:'departure',mode:'story',outcome:null,combat:null,ending:null,journal:[],events:0,victories:0,startedAt:new Date().toISOString()};
 s.vitality=.8+random(s)*.4;
 const starts=Object.values(REGIONS).filter(r=>!r.minStage);s.region=starts[roll(s,0,starts.length-1)].id;s.eventId=drawEvent(s);
 record(s,'첫 장',`열여섯의 봄, ${regionName(s)}에서 당신의 이야기가 시작되었다.`,'birth');
 return s;
}
export function party(s){return s.formation.map(id=>id==='hero'?s.hero:s.companions.find(a=>a.id===id)).filter(a=>a&&a.hp>0);}
export function record(s,title,body,type='event'){s.journal.unshift({week:s.weeks,title,body,type});s.journal=s.journal.slice(0,350);}
export function endLife(s,cause){
 s.mode='ended';s.combat=null;
 const title=cause==='old_age'?'마지막으로 넘기는 페이지':'길 위에 남겨진 이름';
 let legacy=s.flags.dragon_defeated?'재를 걷어낸 전설':s.flags.mentor?'다음 세대의 등불':s.rep>=15?'오래 기억될 선한 이름':s.flags.warden_defeated?'새벽의 수호자':s.victories>=8?'수많은 길을 건넌 자':'자신의 길을 걸었던 사람';
 s.ending={cause,title,legacy,age:ageOf(s),body:cause==='old_age'?'어느 고요한 아침, 당신은 더 이상 가방을 챙기지 않았다. 길은 끝났지만 당신이 건넨 온기와 함께 걸었던 이들의 기억은 남았다.':'당신의 발걸음은 이곳에서 멈췄다. 그러나 한 생은 마지막 순간 하나로 정해지지 않는다. 당신이 선택했던 모든 길이 이 책에 남아 있다.'};
 record(s,title,`${ageText(s)} · ${legacy}`,'end');
 return false;
}
export function advanceTime(s,weeks){
 if(s.mode==='ended')return false;
 if(!Number.isInteger(weeks)||weeks<1)throw new Error('사건 시간은 최소 1주여야 합니다.');
 for(let i=0;i<weeks;i++){s.weeks++;if(random(s)<weeklyMortality(16+s.weeks/52,s.vitality))return endLife(s,'old_age');}
 return true;
}
export function checkChance(s,check){return clamp(62+(s.hero.stats[check.stat]-check.dc)*7+Math.floor(s.rep/5)-Math.floor(s.hero.stress/20)*3,15,95);}
export function choiceLock(s,c){
 const n=c.need||{};
 if(n.gold&&s.gold<n.gold)return `${n.gold}골드 필요`;
 if(n.rations&&s.rations<n.rations)return '식량이 부족합니다';
 if(n.rep&&s.rep<n.rep)return `평판 ${n.rep} 필요`;
 if(n.weapon&&!s.inventory.includes(n.weapon))return `${WEAPONS[n.weapon].name} 필요`;
 if(n.partySpace&&party(s).length>=4)return '일행은 최대 4명입니다';
 if(n.flag&&!s.flags[n.flag])return '단서가 필요합니다';
 return '';
}
function healParty(s,value){for(const a of party(s))a.hp=clamp(a.hp+value,0,a.maxHp);}
export function applyEffects(s,e={}){
 const changes=[];if(s.mode==='ended')return changes;
 if(e.hp){s.hero.hp=clamp(s.hero.hp+e.hp,0,s.hero.maxHp);changes.push(`체력 ${e.hp}`);}
 if(s.hero.hp<=0){endLife(s,'wounds');return changes;}
 for(const [key,label] of [['gold','골드'],['rations','식량'],['potions','회복약'],['rep','평판'],['bond','유대']])if(e[key]){const old=s[key];s[key]=Math.max(0,s[key]+e[key]);changes.push(`${label} ${s[key]-old>=0?'+':''}${s[key]-old}`);}
 if(e.flag&&!s.flags[e.flag]){s.flags[e.flag]=true;s.flagWeeks[e.flag]=s.weeks;}
 if(e.region&&REGIONS[e.region]){s.region=e.region;changes.push(`${regionName(s)} 도착`);}
 if(e.weapon){if(!s.inventory.includes(e.weapon)){s.inventory.push(e.weapon);changes.push(`${WEAPONS[e.weapon].name} 획득`);}else{s.gold+=12;changes.push('중복 장비 판매 +12골드');}}
 if(e.skill&&!s.hero.skills.includes(e.skill)){s.hero.skills.push(e.skill);changes.push(`${SKILLS[e.skill].name} 습득`);}
 if(e.recruit&&!s.companions.some(a=>a.id===e.recruit)&&!s.fallen.includes(e.recruit)&&party(s).length<4){const t=COMPANIONS[e.recruit];s.companions.push({...structuredClone(t),skills:[...(t.skills||[])],hp:t.maxHp,stress:0,energy:4,level:1,xp:0,stats:{strength:4,agility:4,wit:4,will:4},mastery:{},status:{}});if(e.recruit==='borin')s.formation.unshift(e.recruit);else s.formation.push(e.recruit);changes.push(`${t.name} 동행 시작`);}
 if(e.stat){s.hero.stats[e.stat]++;changes.push(`${STAT_NAMES[e.stat]} +1`);}
 if(e.xp){s.hero.xp+=e.xp;changes.push(`경험 +${e.xp}`);while(s.hero.xp>=s.hero.level*45){s.hero.xp-=s.hero.level*45;s.hero.level++;s.hero.maxHp+=4;s.hero.hp=Math.min(s.hero.maxHp,s.hero.hp+8);s.hero.power++;changes.push(`성장 단계 ${s.hero.level}`);}}
 if(e.fullHeal||e.heal){for(const a of party(s))a.hp=e.fullHeal||e.heal>=99?a.maxHp:clamp(a.hp+e.heal,0,a.maxHp);changes.push(e.fullHeal||e.heal>=99?'일행 완전 회복':`일행 체력 +${e.heal}`);}
 if(e.stress){for(const a of party(s))a.stress=clamp(a.stress+e.stress,0,100);changes.push(`긴장 ${e.stress>0?'+':''}${e.stress}`);}
 return changes;
}
export function choose(s,index){
 if(s.mode!=='story')return false;
 const event=EVENTS[s.eventId],c=event?.choices[index];if(!c||choiceLock(s,c))return false;
 const success=!c.check?.stat||random(s)*100<checkChance(s,c.check);
 const outcome=success?c:c.failure;
 const choiceRecord={title:event.title,body:outcome.body,effects:outcome.effects||{},next:c.next||null,weeks:c.weeks,check:c.check?.stat?{success,chance:checkChance(s,c.check),stat:STAT_NAMES[c.check.stat]}:null};
 s.events++;s.seen[s.eventId]=(s.seen[s.eventId]||0)+1;
 if(c.repeat)s.seen[s.eventId]=0;
 if(!advanceTime(s,c.weeks))return true;
 if(success&&c.battle?.length){startCombat(s,c.battle,choiceRecord);return true;}
 const changes=applyEffects(s,choiceRecord.effects);
 if(s.mode==='ended')return true;
 s.mode='outcome';s.outcome={...choiceRecord,changes,weeks:c.weeks};
 record(s,event.title,`${c.label} · ${outcome.body}`);
 return true;
}
export const stage=s=>s.hero.level>=7||s.events>=65||s.weeks>=1040?2:s.hero.level>=3||s.events>=18||s.weeks>=260?1:0;
export const stageName=s=>['초반 · 낯선 길','중반 · 쌓이는 이름','후반 · 남길 이야기'][stage(s)];
export const regionName=s=>REGIONS[s.region]?.name||'브린 근교';
export function eligible(s,e){return (!e.once||!s.seen[e.id])&&s.weeks>=(e.minWeeks||0)&&s.hero.level>=(e.minLevel||0)&&stage(s)>=(e.minStage||0)&&s.rep>=(e.minRep||0)&&party(s).length>=(e.minParty||0)&&(!e.regions.length||e.regions.includes(s.region))&&(!e.requiredFlag||s.flags[e.requiredFlag])&&(!e.excludedFlag||!s.flags[e.excludedFlag])&&(!e.requiredWeapon||s.inventory.includes(e.requiredWeapon))&&(!e.minFlagWeeks||(Number.isInteger(s.flagWeeks[e.requiredFlag])&&s.weeks-s.flagWeeks[e.requiredFlag]>=e.minFlagWeeks))&&(!e.excludedFallen||!s.fallen.includes(e.excludedFallen));}
export const eventWeight=(s,e)=>eligible(s,e)?Math.max(0,e.stageWeights[stage(s)])/(1+(s.seen[e.id]||0)*.75):0;
export function availableEvents(s){return Object.keys(EVENTS).filter(id=>eligible(s,EVENTS[id])&&!s.recent.includes(id));}
export function drawEvent(s){let list=availableEvents(s).filter(id=>eventWeight(s,EVENTS[id])>0);if(!list.length)list=Object.keys(EVENTS).filter(id=>eventWeight(s,EVENTS[id])>0);if(!list.length)throw Error('현재 지역에 가능한 사건이 없습니다.');let ticket=random(s)*list.reduce((n,id)=>n+eventWeight(s,EVENTS[id]),0);for(const id of list){ticket-=eventWeight(s,EVENTS[id]);if(ticket<0)return id;}return list.at(-1);}
export function continueStory(s){if(s.mode!=='outcome')return false;s.recent.push(s.eventId);s.recent=s.recent.slice(-12);s.eventId=drawEvent(s);s.outcome=null;s.mode='story';return true;}
export const routes=s=>Object.values(REGIONS).filter(r=>r.id!==s.region&&(r.minStage||0)<=stage(s)&&REGIONS[s.region].routes.includes(r.id));
export function travel(s,id){if(s.mode!=='story'||!routes(s).some(r=>r.id===id))return '지금은 갈 수 없는 길입니다.';if(s.rations<1&&s.gold<6)return '식량 1개 또는 6골드가 필요합니다.';if(s.rations>0)s.rations--;else s.gold-=6;if(!advanceTime(s,2))return '마지막 여정이 기록되었습니다.';s.region=id;s.eventId=drawEvent(s);record(s,'새로운 땅의 바람',`${regionName(s)}에 도착했다. 2주가 흘렀다.`,'travel');return `${regionName(s)} 도착 · 2주 경과`;}
export function camp(s){
 if(s.mode!=='story')return {ok:false,reason:'이 사건을 마친 뒤 야영할 수 있습니다.'};
 if(!s.rations&&s.gold<8)return {ok:false,reason:'야영에는 식량 1개 또는 8골드가 필요합니다.'};
 const cost=s.rations?'식량 1':'8골드';if(s.rations)s.rations--;else s.gold-=8;
 if(!advanceTime(s,1))return {ok:true};
 healParty(s,Math.ceil(s.hero.maxHp*.45));for(const a of party(s))a.stress=Math.max(0,a.stress-20);
 record(s,'불가의 작은 쉼',`${cost}를 쓰고 1주 동안 쉬었다. 일행의 상처와 긴장이 가라앉았다.`,'camp');
 return {ok:true,reason:'1주 경과 · 일행 체력 회복 · 긴장 -20'};
}
export function equip(s,weapon){if(s.mode==='combat'||s.mode==='ended'||!s.inventory.includes(weapon))return false;s.hero.weapon=weapon;return true;}
export function reorder(s,id,delta){if(s.mode==='combat'||s.mode==='ended')return false;const p=s.formation.indexOf(id),q=p+delta;if(p<0||q<0||q>=s.formation.length)return false;[s.formation[p],s.formation[q]]=[s.formation[q],s.formation[p]];return true;}
export function availableSkills(s,actor){return [...new Set([...WEAPONS[actor.weapon].skills,...actor.skills,'strike','guard','focus','potion','move'])];}
export function rankOf(s,id){return party(s).findIndex(a=>a.id===id)+1;}
export function actorById(s,id){return party(s).find(a=>a.id===id)||s.combat?.enemies.find(e=>e.id===id);}
export function validTargets(s,skillId,actorId=s.combat?.active){
 const sk=SKILLS[skillId],a=actorById(s,actorId);if(!a||!sk)return [];
 if(sk.target==='self')return [a];
 if(sk.target==='ally')return party(s);
 if(sk.target==='otherAlly')return party(s).filter(t=>t.id!==a.id);
 return (s.combat?.enemies||[]).filter(e=>e.hp>0).filter((e,i)=>sk.to.includes(i+1));
}
export function skillLock(s,id,actorId=s.combat?.active){
 if(s.mode!=='combat')return '전투 중에만 사용';
 const a=actorById(s,actorId),sk=SKILLS[id];if(!a||!sk||!availableSkills(s,a).includes(id))return '사용 불가';
 if(!sk.from.includes(rankOf(s,a.id)))return `${sk.from.join('·')}열에서 사용`;
 if(a.energy<sk.cost)return '기력 부족';
 if(sk.item&&!s[sk.item])return '회복약 없음';
 if(!validTargets(s,id,actorId).length)return '대상 없음';
 return '';
}
function log(s,text){s.combat.log.push(text);s.combat.log=s.combat.log.slice(-80);}
export function startCombat(s,enemyTypes,pending){
 s.mode='combat';s.outcome=null;
 s.combat={round:0,active:null,queue:[],log:['일행이 진형을 갖춥니다.'],pending,enemies:enemyTypes.map((id,i)=>({...ENEMIES[id],id:`enemy_${i}`,type:id,hp:ENEMIES[id].maxHp,energy:4,stress:0,status:{},femaleAppearance:enemyFemale(id,s.rng,i)}))};
 for(const a of party(s)){a.energy=4;a.status={};}
 advanceTurn(s);
}
function newRound(s){
 const c=s.combat;c.round++;
 c.queue=[...party(s),...c.enemies.filter(e=>e.hp>0)].map(a=>({id:a.id,initiative:a.speed+(a.weapon?WEAPONS[a.weapon].speed:0)+roll(s,0,4)})).sort((a,b)=>b.initiative-a.initiative).map(a=>a.id);
 log(s,`— ${c.round}번째 라운드 —`);
}
function deathCheck(s){
 const c=s.combat;if(!c)return true;
 if(s.hero.hp<=0){record(s,'마지막 전투',c.log.slice(-5).join(' '),'battle');endLife(s,'battle');return true;}
 for(const a of s.companions.filter(a=>a.hp<=0&&!s.fallen.includes(a.id))){s.fallen.push(a.id);s.formation=s.formation.filter(id=>id!==a.id);record(s,`${a.name}와의 작별`,'길 위에서 함께했던 동료가 전투 중 쓰러졌다.','loss');log(s,`${a.name}이(가) 쓰러졌습니다. 다시 돌아오지 못합니다.`);for(const p of party(s))p.stress=clamp(p.stress+15,0,100);}
 if(c.enemies.every(e=>e.hp<=0)){
  const p=c.pending;s.victories++;s.combat=null;
  const changes=applyEffects(s,p.effects);
  for(const a of party(s))a.status={};
  if(s.mode==='ended')return true;
  s.mode='outcome';s.outcome={...p,title:'살아서, 다음 페이지로',changes,combatWin:true};
  record(s,p.title,p.body,'victory');return true;
 }
 return false;
}
function enemyAction(s,e){
 const targets=party(s);const pool=e.ranged?targets:targets.slice(0,2);
 const t=pool[roll(s,0,pool.length-1)];if(!t)return;
 const miss=random(s)<.08;
 if(miss){log(s,`${e.name}의 ${e.attack} — ${t.name} 회피!`);return;}
 const damage=Math.max(1,Math.round((e.power+roll(s,-1,2))*(t.status.guard ? .35 : 1)));
 const tension=stressTaken(s,t,e.stress||3);
 t.hp=Math.max(0,t.hp-damage);t.stress=clamp(t.stress+tension,0,100);
 if(e.bleed&&random(s)<.3)t.status.bleed=2;
 log(s,`${e.name} · ${e.attack} → ${t.name} 체력 -${damage}${e.stress?`, 긴장 +${tension}`:''}`);
}
function advanceTurn(s){
 for(let safety=0;s.mode==='combat'&&safety<100;safety++){
  if(deathCheck(s))return;
  const c=s.combat;if(!c.queue.length)newRound(s);
  const id=c.queue.shift(),a=actorById(s,id);if(!a||a.hp<=0)continue;
  a.status||={};delete a.status.guard;
  if(a.status.bleed>0){a.status.bleed--;a.hp=Math.max(0,a.hp-2);log(s,`${a.name} · 출혈 피해 2`);if(deathCheck(s))return;if(a.hp<=0)continue;}
  if(a.status.stun>0){a.status.stun--;log(s,`${a.name} · 기절로 차례를 넘깁니다.`);continue;}
  if(!id.startsWith('enemy_')&&a.stress>=100){a.stress=65;log(s,`${a.name} · 압도된 마음. 한 차례를 잃고 긴장이 65로 낮아집니다.`);continue;}
  if(id.startsWith('enemy_')){enemyAction(s,a);continue;}
  c.active=id;if(id!=='hero'){companionAction(s,a);c.active=null;continue;}return;
 }
 if(s.mode==='combat')throw new Error('전투 턴 진행 한도 초과');
}
export function act(s,skillId,targetId){
 if(s.mode!=='combat'||s.combat.active!=='hero'||skillLock(s,skillId)||!validTargets(s,skillId).some(t=>t.id===targetId))return false;
 resolveAction(s,skillId,targetId);s.combat.active=null;if(!deathCheck(s))advanceTurn(s);return true;
}
function resolveAction(s,skillId,targetId){
 const c=s.combat,a=actorById(s,c.active),sk=SKILLS[skillId];
 const t=validTargets(s,skillId).find(t=>t.id===targetId);if(!t)return false;
 a.energy-=sk.cost;if(sk.item)s[sk.item]--;
 if(sk.move){const p=s.formation.indexOf(a.id),q=s.formation.indexOf(t.id);[s.formation[p],s.formation[q]]=[s.formation[q],s.formation[p]];log(s,`${a.name} ↔ ${t.name} 자리 교대`);}
 else if(sk.heal){const value=sk.item?sk.heal:sk.heal+roll(s,-2,2);const restored=Math.min(t.maxHp-t.hp,value);t.hp+=restored;t.status.bleed=0;log(s,`${a.name} · ${sk.name} → ${t.name} 체력 +${restored}, 출혈 해제`);}
 else if(sk.calm){t.stress=Math.max(0,t.stress-sk.calm);t.energy=Math.min(5,t.energy+1);log(s,`${a.name} · ${sk.name} → ${t.name} 긴장 -${sk.calm}, 기력 +1`);}
 else if(sk.focus){a.energy=Math.min(5,a.energy+2);a.stress=Math.max(0,a.stress-6);log(s,`${a.name} · 숨 고르기, 기력 +2`);}
 else if(sk.power){
  if(random(s)*100>sk.accuracy){log(s,`${a.name} · ${sk.name} → ${t.name} 빗나감`);}
  else{
   const mastery=a.id==='hero'?Math.min(3,Math.floor((a.mastery[a.weapon]||0)/8)):0;
   const stat=a.id==='hero'?Math.floor(a.stats[sk.magic?'wit':['bow','dagger'].includes(a.weapon)?'agility':'strength']/3):0;
   const crit=random(s)<.08;
   const base=(WEAPONS[a.weapon].power+a.power+mastery+stat+roll(s,-1,2))*sk.power;
   const damage=Math.max(1,Math.round((base-(sk.magic?0:t.armor||0))*(crit?1.6:1)*(t.status.guard ? .35 : 1)));
   t.hp=Math.max(0,t.hp-damage);
   if(sk.bleed)t.status.bleed=sk.bleed;
   const stunned=sk.stun&&random(s)*100<sk.stun;if(stunned)t.status.stun=1;
   log(s,`${a.name} · ${sk.name} → ${t.name} ${crit?'치명타! ':''}-${damage}${sk.bleed?' · 출혈':''}${stunned?' · 기절':''}`);
  }
  if(a.id==='hero')a.mastery[a.weapon]=(a.mastery[a.weapon]||0)+1;
 }
 if(sk.guard){a.status.guard=true;if(!sk.power)a.energy=Math.min(5,a.energy+1);log(s,`${a.name} · 다음 차례까지 피해 65% 감소`);}
 return true;
}
export function flee(s){
 if(s.mode!=='combat')return false;
 const c=s.combat;const lost=Math.min(s.gold,Math.ceil(s.gold*.2));s.gold-=lost;
 for(const a of party(s))a.stress=clamp(a.stress+12,0,100);
 // Fixed authored encounter follow-ups remain reachable even if the tutorial is fled.
 const next=c.pending.next;const event=EVENTS[s.eventId];if(event.once)s.seen[s.eventId]=0;
 s.combat=null;s.mode='outcome';s.outcome={title:'살아남는 것도 선택이다',body:'동료들과 함께 전장을 벗어났다. 잃은 것은 다시 얻을 수 있다. 아직 다음 이야기가 남아 있다.',changes:[`골드 -${lost}`,'일행 긴장 +12'],next};
 record(s,'물러선 발걸음',`전투에서 퇴각했다. ${lost}골드를 잃었지만 일행은 살아남았다.`,'flee');return true;
}

export function serialize(s){return JSON.stringify(s);}
export function deserialize(raw){
 try{
  const s=JSON.parse(raw);if(!s)return null;
  const counter=v=>Array.isArray(v)?Object.fromEntries(v.map(x=>[x.id,x.value])):v;
  if(Array.isArray(s.flags))s.flags=Object.fromEntries(s.flags.map(id=>[id,true]));
  s.seen=counter(s.seen);s.flagWeeks=counter(s.flagWeeks)||{};if(s.hero)s.hero.mastery=counter(s.hero.mastery);
  for(const a of s.companions||[])a.mastery=counter(a.mastery)||{};
  s.region||=EVENTS[s.eventId]?.regions[0]||'village';
  if(!REGIONS[s.region])return null;
  for(const e of Object.values(EVENTS).filter(e=>e.minFlagWeeks&&s.flags?.[e.requiredFlag]))if(!Number.isInteger(s.flagWeeks[e.requiredFlag])){
    const sources=Object.values(EVENTS).flatMap(e=>e.choices.filter(c=>c.effects?.flag===e.requiredFlag).map(c=>({title:e.title,body:c.body})));
    const entry=(s.journal||[]).filter(j=>sources.some(src=>src.title===j.title&&j.body.endsWith(src.body))).sort((a,b)=>a.week-b.week)[0];s.flagWeeks[e.requiredFlag]=entry?.week??s.weeks;
  }
  const outcome=o=>{if(o&&!o.check&&o.hasCheck)o.check={success:o.success,chance:o.chance,stat:o.checkStat};};outcome(s.outcome);outcome(s.combat?.pending);
  if(s?.version!==VERSION||!['story','outcome','combat','ended'].includes(s.mode)||!EVENTS[s.eventId])return null;
  if(!s.hero||!Number.isFinite(s.hero.hp)||!Number.isInteger(s.weeks)||s.weeks<0||!Number.isInteger(s.rng)||s.rng<=0||s.rng>4294967295||!Number.isFinite(s.vitality)||s.vitality<=0)return null;
  if(!Array.isArray(s.formation)||!Array.isArray(s.companions)||!Array.isArray(s.inventory)||!Array.isArray(s.journal)||!Array.isArray(s.fallen)||!s.flags||!s.seen||!Array.isArray(s.recent))return null;
  if(!WEAPONS[s.hero.weapon]||!s.hero.stats||!s.hero.mastery||!Array.isArray(s.hero.skills))return null;
  if(s.hero.id!=='hero'||!['strength','agility','wit','will'].every(k=>Number.isFinite(s.hero.stats[k])))return null;
  if(!['gold','rations','potions','rep','bond','events','victories'].every(k=>Number.isFinite(s[k])&&s[k]>=0))return null;
  const actors=[s.hero,...s.companions];
  if(actors.some(a=>!Number.isFinite(a.maxHp)||a.maxHp<=0||!Number.isFinite(a.hp)||a.hp<0||a.hp>a.maxHp||!Number.isFinite(a.stress)||a.stress<0||a.stress>100))return null;
  if(s.formation.length>4||new Set(s.formation).size!==s.formation.length||!s.formation.includes('hero')||s.formation.some(id=>!actors.some(a=>a.id===id)))return null;
  if(s.inventory.some(w=>!WEAPONS[w])||[s.hero,...s.companions].some(a=>!WEAPONS[a.weapon]||!Array.isArray(a.skills)||a.skills.some(k=>!SKILLS[k])))return null;
  if(s.mode==='combat'&&(!s.combat||!Array.isArray(s.combat.enemies)||!Array.isArray(s.combat.queue)||!s.combat.pending||!s.combat.active))return null;
  if(s.mode!=='ended'&&s.hero.hp<=0)return null;
  if(new Set(actors.map(a=>a.id)).size!==actors.length||s.companions.some(a=>!COMPANIONS[a.id]))return null;
  if(actors.some(a=>!Number.isFinite(a.energy)||a.energy<0||a.energy>5||!a.status))return null;
  if(s.mode==='combat'){
   const c=s.combat;
   if(!party(s).some(a=>a.id===c.active)||!Array.isArray(c.log)||c.enemies.length<1||c.enemies.length>4||!c.enemies.some(e=>e.hp>0))return null;
   if(c.enemies.some(e=>!ENEMIES[e.type]||!e.status||!Number.isFinite(e.hp)||e.hp<0||e.hp>e.maxHp||!e.id.startsWith('enemy_'))||new Set(c.enemies.map(e=>e.id)).size!==c.enemies.length)return null;
   if(c.queue.some(id=>![...actors,...c.enemies].some(a=>a.id===id)))return null;
  }
  if(s.mode==='ended'&&!s.ending)return null;
  if(s.mode==='outcome'&&!s.outcome)return null;
  if(s.mode==='combat'&&s.combat.active!=='hero'){companionAction(s,actorById(s,s.combat.active));s.combat.active=null;if(!deathCheck(s))advanceTurn(s);}
  return s;
 }catch{return null;}
}

export const femaleVariants=['hero','elian','borin','bandit','archer','cultist'];
export const spriteId=a=>(a.type||a.id)+(a.femaleAppearance&&femaleVariants.includes(a.type||a.id)?'_female':'');
function enemyFemale(type,seed,index){if(!femaleVariants.includes(type))return false;let value=(seed+Math.imul(index+1,0x9e3779b9))>>>0;value^=value>>>16;value=Math.imul(value,0x85ebca6b)>>>0;value^=value>>>13;return (value&1)!==0;}
export function setAppearance(s,id,female){const a=party(s).find(a=>a.id===id);if(!a||!femaleVariants.includes(id))return false;a.femaleAppearance=female;return true;}
function companionAction(s,a){
 const legal=availableSkills(s,a).filter(id=>!skillLock(s,id)).map(id=>SKILLS[id]);
 const wounded=[...party(s)].sort((a,b)=>a.hp/a.maxHp-b.hp/b.maxHp)[0];
 const heal=legal.find(k=>k.heal>0&&k.item!=='potions');
 if(heal&&(wounded.hp<wounded.maxHp*.6||wounded.status.bleed>0)){resolveAction(s,heal.id,wounded.id);return;}
 if(wounded.hp<wounded.maxHp*.3&&legal.some(k=>k.id==='potion')){resolveAction(s,'potion',wounded.id);return;}
 const canHeal=availableSkills(s,a).map(id=>SKILLS[id]).some(k=>k.heal>0&&k.item!=='potions'&&k.from.includes(rankOf(s,a.id)));
 if(canHeal&&!heal&&(wounded.hp<wounded.maxHp*.6||wounded.status.bleed>0)){resolveAction(s,'focus',a.id);return;}
 const anxious=[...party(s)].sort((a,b)=>b.stress-a.stress)[0],calm=legal.find(k=>k.calm>0);
 if(calm&&anxious.stress>=75){resolveAction(s,calm.id,anxious.id);return;}
 const attacks=legal.filter(k=>k.power>0);
 if(attacks.every(k=>k.id==='strike')&&legal.some(k=>k.move)){const partner=party(s).find(p=>p.id!==a.id&&WEAPONS[a.weapon].skills.some(id=>SKILLS[id].power>0&&SKILLS[id].from.includes(rankOf(s,p.id))));if(partner){resolveAction(s,'move',partner.id);return;}}
 const threat=s.combat.enemies.some(e=>e.hp>0&&e.power>=7&&!e.status.stun&&s.combat.queue.includes(e.id));
 const score=k=>k.power*k.accuracy/100+(k.bleed||0)*.12+(k.stun||0)*(threat?.008:.004)-(k.cost||0)*.08;
 const attack=attacks.sort((a,b)=>score(b)-score(a))[0];
 if(attack){const t=validTargets(s,attack.id).sort((a,b)=>(attack.stun?(a.status.stun>0)-(b.status.stun>0):0)||a.hp-b.hp)[0];resolveAction(s,attack.id,t.id);return;}
 resolveAction(s,a.energy<3?'focus':'guard',a.id);
}
