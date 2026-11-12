import {deserialize,serialize} from './engine.mjs';
export const SAVE='arvito.browser.v1',BACKUP=SAVE+'.backup',ARCHIVE=SAVE+'.previous';
export function load(storage){try{const raw=storage.getItem(SAVE),state=deserialize(raw);if(state)return {state,notice:''};const backup=deserialize(storage.getItem(BACKUP));return {state:backup,notice:backup?'이전 저장 기록을 복구했습니다.':raw?'저장 기록을 읽지 못했습니다. 파일을 보관한 뒤 다시 시작해 주세요.':''};}catch{return {state:null,notice:'브라우저 저장을 사용할 수 없습니다. 저장 권한을 확인해 주세요.'};}}
export function save(storage,state){const next=serialize(state);if(!deserialize(next))throw Error('저장할 상태가 올바르지 않습니다.');const previous=storage.getItem(SAVE);if(deserialize(previous))storage.setItem(BACKUP,previous);storage.setItem(SAVE,next);}
