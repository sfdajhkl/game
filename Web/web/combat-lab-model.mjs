import * as G from './engine.mjs';
import {WEAPONS,COMPANIONS,ENEMIES,SKILLS,STAT_NAMES} from './content.mjs';
import {normalizeCombatRules,DEFAULT_COMBAT_RULES} from './combat-rules.mjs';
export const LAB_KEY='arvito.combat-lab.v1';
export const DEFAULT_SETUP={seed:20261112,level:3,hp:38,potions:3,weapon:'sword',heroRank:2,stats:G.initialStats(),companions:['borin','mira'],enemies:['wolf','wolf'],rules:{...DEFAULT_COMBAT_RULES}};
export const PRESETS={wolves:{name:'늑대 두 마리',enemies:['wolf','wolf']},bandits:{name:'도적과 궁수',enemies:['bandit','archer']},undead:{name:'망자와 치유 사제',enemies:['skeleton','cultist']},warden:{name:'유적 파수꾼',enemies:['warden','skeleton']},dragon:{name:'용과 사제',enemies:['dragon','cultist']}};
const number=(value,fallback,min,max,integer=true)=>{const n=Number(value);return Number.isFinite(n)?Math.max(min,Math.min(max,integer?Math.round(n):n)):fallback;};
export function normalizeSetup(input={}){
 if(!input||typeof input!=='object'||Array.isArray(input))throw Error('설정 형식이 올바르지 않습니다.');
 const enemies=input.enemies??DEFAULT_SETUP.enemies,companions=input.companions??DEFAULT_SETUP.companions;
 if(!Array.isArray(enemies)||enemies.length<1||enemies.length>4||enemies.some(id=>!Object.hasOwn(ENEMIES,id)))throw Error('적을 1~4명 선택하세요.');
 if(!Array.isArray(companions)||companions.length>3||new Set(companions).size!==companions.length||companions.some(id=>!Object.hasOwn(COMPANIONS,id)))throw Error('동료는 중복 없이 최대 3명까지 선택하세요.');
 if(input.weapon&&!Object.hasOwn(WEAPONS,input.weapon))throw Error('무기를 확인하세요.');
 return {seed:number(input.seed,DEFAULT_SETUP.seed,1,4294967295),level:number(input.level,3,1,20),hp:number(input.hp,38,1,500),potions:number(input.potions,3,0,20),weapon:input.weapon||'sword',heroRank:number(input.heroRank,2,1,companions.length+1),stats:Object.fromEntries(Object.keys(STAT_NAMES).map(k=>[k,number(input.stats?.[k],4,0,30)])),companions:[...companions],enemies:[...enemies],rules:normalizeCombatRules(input.rules||{})};
}
export function createLabBattle(input){const config=normalizeSetup(input),s=G.newGame('여행자',config.seed);s.hero.level=config.level;s.hero.power=config.level;s.hero.stats={...config.stats};s.hero.maxHp=s.hero.hp=Math.max(1,config.hp+2*(config.stats.constitution-4));s.hero.skills=['rally'];if(!s.inventory.includes(config.weapon))s.inventory.push(config.weapon);s.hero.weapon=config.weapon;s.potions=config.potions;
 for(const id of config.companions)G.applyEffects(s,{recruit:id});
 const pos=s.formation.indexOf('hero');s.formation.splice(pos,1);s.formation.splice(config.heroRank-1,0,'hero');
 const combat=G.startCombat(s,config.enemies,{title:'전투 시험',body:'모든 적을 물리쳤습니다.',effects:{}},config.rules);
 return {state:s,config,combat};
}
export function serializeSetup(config){return JSON.stringify({format:'arvito-combat-setup',version:1,config:normalizeSetup(config)},null,2);}
export function parseSetup(raw){if(typeof raw!=='string'||raw.length>50000)throw Error('설정 파일은 50KB 이하여야 합니다.');const data=JSON.parse(raw);if(data?.format!=='arvito-combat-setup'||data.version!==1)throw Error('전투 설정 JSON이 아닙니다.');return normalizeSetup(data.config);}
export function runBatch(input,count=30){const base=normalizeSetup(input);count=number(count,30,1,100);let wins=0,losses=0,timeouts=0,rounds=0,hp=0;
 for(let i=0;i<count;i++){
  const run=createLabBattle({...base,seed:((base.seed+i*7919)>>>0)||1}),s=run.state,c=run.combat;let actions=0;
  while(s.mode==='combat'&&actions++<300){
   if(s.hero.hp<s.hero.maxHp*.35&&!G.skillLock(s,'potion')){G.act(s,'potion','hero');continue;}
   const attacks=G.availableSkills(s,s.hero).filter(k=>SKILLS[k].power&&!G.skillLock(s,k)).flatMap(k=>G.validTargets(s,k).map(t=>({k,t,score:SKILLS[k].power*G.attackPreview(s.hero,t,SKILLS[k],s.combat.rules).chance+(t.hp<10?10:0)}))).sort((a,b)=>b.score-a.score||a.t.hp-b.t.hp);
   if(attacks.length)G.act(s,attacks[0].k,attacks[0].t.id);else G.act(s,'focus','hero');
  }
  if(s.mode==='combat')timeouts++;else if(s.outcome?.combatWin)wins++;else losses++;
  rounds+=c?.round||0;hp+=s.hero.hp;
 }
 return {count,wins,losses,timeouts,winRate:Math.round(wins/count*100),averageRounds:Math.round(rounds/count*10)/10,averageHp:Math.round(hp/count*10)/10};
}
