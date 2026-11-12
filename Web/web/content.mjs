import catalog from './catalog.mjs';
const map=items=>Object.fromEntries(items.map(x=>[x.id,x]));
export const WEAPONS=map(catalog.weapons),SKILLS=map(catalog.skills.map(x=>({accuracy:100,...x}))),COMPANIONS=map(catalog.companions),ENEMIES=map(catalog.enemies),EVENTS=map(catalog.events),REGIONS=map(catalog.regions);
export const STAT_NAMES={strength:'힘',agility:'민첩',wit:'지혜',will:'의지'};
for(const e of Object.values(EVENTS)){e.regions??=[];e.stageWeights??=[10,10,10];for(const c of e.choices){c.effects??={};if(c.failure)c.failure.effects??={};}}
