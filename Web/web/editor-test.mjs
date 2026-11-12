// This module runs only in the workshop tab and never reads/writes play saves.
import * as G from './engine.mjs';
import {EVENTS,STAT_NAMES,COMPANIONS,WEAPONS} from './content.mjs';
import {validateEvent} from './editor-model.mjs';
const SLOT='__workshop_test__';
export function prepareTest(event,config={}){
 const issues=validateEvent(event).errors;if(issues.length)throw Error(issues.join('\n'));
 const state=G.newGame('테스트 여행자',Number(config.seed)||12345);
 state.eventId=SLOT;state.region=event.regions[0]||'village';
 state.hero.level=Math.max(1,Math.min(20,Number(config.level)||3));
 state.hero.maxHp=state.hero.hp=Number(config.hp)||80;state.hero.power=state.hero.level;
 state.gold=Number(config.gold??30);state.rations=Number(config.rations??4);state.potions=3;state.rep=Number(config.rep??0);
 for(const key of Object.keys(STAT_NAMES))state.hero.stats[key]=Number(config.stats?.[key]??4);
 state.flags=Object.fromEntries((config.flags||'').split(/[\s,]+/).filter(Boolean).map(k=>[k,true]));
 state.flagWeeks=Object.fromEntries(Object.keys(state.flags).map(k=>[k,0]));
 state.weeks=Number(config.weeks)||0;
 if(WEAPONS[config.weapon]){state.inventory.push(config.weapon);state.hero.weapon=config.weapon;}
 for(const id of config.companions||[])if(COMPANIONS[id])G.applyEffects(state,{recruit:id});
 const authored={...structuredClone(event),id:SLOT};
 return {state,event:authored,eligible:G.eventWeight(state,authored)>0};
}
function withEvent(session,action){const previous=EVENTS[SLOT];EVENTS[SLOT]=session.event;try{return action();}finally{if(previous)EVENTS[SLOT]=previous;else delete EVENTS[SLOT];}}
export function testChoice(session,index){return withEvent(session,()=>G.choose(session.state,index));}
export function testAction(session,skill,target){return withEvent(session,()=>G.act(session.state,skill,target));}
export function testFlee(session){return withEvent(session,()=>G.flee(session.state));}
