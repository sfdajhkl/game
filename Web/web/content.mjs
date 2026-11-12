import catalog from './catalog.mjs';
import events from './events.mjs';
const map=items=>Object.fromEntries(items.map(x=>[x.id,x]));
export const WEAPONS=map(catalog.weapons),SKILLS=map(catalog.skills.map(x=>({accuracy:100,...x}))),COMPANIONS=map(catalog.companions),ENEMIES=map(catalog.enemies),EVENTS=map(events),REGIONS=map(catalog.regions);
export const STAT_NAMES={strength:'힘',agility:'민첩',intelligence:'지능',wisdom:'지혜',charisma:'매력',constitution:'건강'};
export const STAT_DETAILS={
 strength:{icon:'sword',color:'#884336',description:'근접 무기의 피해와 무거운 물건을 다루는 판정에 영향을 줍니다.'},
 agility:{icon:'feather',color:'#416343',description:'활·단검의 피해와 재빠른 행동의 판정에 영향을 줍니다.'},
 intelligence:{icon:'rune',color:'#405d7b',description:'마법 피해와 지식·해독·장치 조사 판정에 영향을 줍니다.'},
 wisdom:{icon:'eye',color:'#72517b',description:'관찰·통찰 판정과 긴장 저항에 영향을 줍니다. 4마다 받는 긴장이 1 줄어듭니다.'},
 charisma:{icon:'speech',color:'#835d25',description:'설득·협상·증언을 모으는 판정에 영향을 줍니다.'},
 constitution:{icon:'shield',color:'#3e6a66',description:'위험한 환경을 견디는 판정에 영향을 줍니다. 1 오를 때 최대 체력 +2, 야영 시 주인공 회복량 +1입니다.'}
};
for(const e of Object.values(EVENTS)){e.regions??=[];e.stageWeights??=[10,10,10];for(const c of e.choices){c.effects??={};if(c.failure)c.failure.effects??={};}}
