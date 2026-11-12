import {STAT_NAMES,WEAPONS} from './content.mjs';
export const DEFAULT_COMBAT_RULES=Object.freeze({system:'d20',version:1,attackBonus:2,defenseBase:10,guardBonus:3,criticalMultiplier:2,enemyHealthScale:1,enemyDamageScale:1});
export const RULE_LIMITS={attackBonus:[0,8],defenseBase:[6,20],guardBonus:[0,8],criticalMultiplier:[1,3],enemyHealthScale:[.25,3],enemyDamageScale:[.25,3]};
export function normalizeCombatRules(input={}){const rules={...DEFAULT_COMBAT_RULES};for(const [k,[lo,hi]] of Object.entries(RULE_LIMITS)){const n=Number(input[k]);if(Number.isFinite(n))rules[k]=Math.max(lo,Math.min(hi,k.endsWith('Scale')||k==='criticalMultiplier'?n:Math.round(n)));}return rules;}
export function validCombatRules(r){return !!r&&r.system==='d20'&&r.version===1&&Object.entries(RULE_LIMITS).every(([k,[lo,hi]])=>Number.isFinite(r[k])&&r[k]>=lo&&r[k]<=hi&&(k.endsWith('Scale')||k==='criticalMultiplier'||Number.isInteger(r[k])));}
export const abilityModifier=score=>Math.floor(((Number.isFinite(score)?score:4)-4)/2);
const profiles={wolf:[6,8,2,6,2,6],bandit:[6,6,4,4,4,6],archer:[4,8,4,4,4,4],skeleton:[8,2,2,4,2,8],cultist:[4,4,8,8,6,4],warden:[12,2,4,8,4,12],dragon:[14,8,10,10,10,14],slime:[4,2,2,2,2,8],boar:[8,4,2,4,2,8],spider:[4,10,2,4,2,4],wisp:[2,10,8,8,4,4]};
export function combatStats(actor){return actor.stats||Object.fromEntries(Object.keys(STAT_NAMES).map((k,i)=>[k,(profiles[actor.type]||[4,4,4,4,4,4])[i]]));}
export function attackStat(actor,skill={}){return skill.magic?'intelligence':actor.weapon&&['bow','dagger'].includes(actor.weapon)||!actor.weapon&&actor.ranged?'agility':'strength';}
export function defenseValue(actor,skill={},rules=DEFAULT_COMBAT_RULES){const stats=combatStats(actor);return rules.defenseBase+abilityModifier(stats[skill.magic?'wisdom':'agility'])+(skill.magic?0:actor.armor||0)+(actor.status?.guard?rules.guardBonus:0);}
export function attackBonus(actor,skill={},rules=DEFAULT_COMBAT_RULES){const ability=abilityModifier(combatStats(actor)[attackStat(actor,skill)]);return rules.attackBonus+Math.ceil((actor.level||1)/3)+ability+Math.floor(((skill.accuracy??90)-90)/10);}
export function resolveD20(die,bonus,defense){const critical=die===20,hit=critical||(die!==1&&die+bonus>=defense);return {die,bonus,total:die+bonus,defense,hit,critical};}
export function hitChance(bonus,defense){let hits=0;for(let die=1;die<=20;die++)if(resolveD20(die,bonus,defense).hit)hits++;return hits*5;}
export function attackPreview(actor,target,skill={},rules=DEFAULT_COMBAT_RULES){const bonus=attackBonus(actor,skill,rules),defense=defenseValue(target,skill,rules);return {bonus,defense,chance:hitChance(bonus,defense),stat:attackStat(actor,skill)};}
export function physicalDamage(actor,skill,variation=0){const mastery=actor.id==='hero'?Math.min(3,Math.floor((actor.mastery?.[actor.weapon]||0)/8)):0;const attribute=abilityModifier(combatStats(actor)[attackStat(actor,skill)]);return Math.max(1,Math.round(((WEAPONS[actor.weapon]?.power||0)+(actor.power||0)+mastery+attribute+variation)*(skill.power||1)));}
export function signed(n){return n>=0?'+'+n:String(n);}
