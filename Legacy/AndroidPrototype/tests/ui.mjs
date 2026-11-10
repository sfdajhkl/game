import {chromium} from 'playwright';
import assert from 'node:assert/strict';
import fs from 'node:fs/promises';
import * as G from '../web/engine.mjs';
import {SKILLS} from '../web/content.mjs';

const BASE=process.env.TEST_URL||'http://127.0.0.1:4173';
const browser=await chromium.launch({executablePath:process.env.CHROMIUM_PATH||'/usr/bin/chromium',headless:true,args:['--no-sandbox']});
const errors=[],failed=[];
await fs.mkdir('dist',{recursive:true});
let page=await browser.newPage({viewport:{width:390,height:844},deviceScaleFactor:1});
function observe(){page.on('pageerror',e=>errors.push(e.message));page.on('requestfailed',r=>failed.push(r.url()));page.on('response',r=>{if(r.status()>=400)failed.push(`${r.status()} ${r.url()}`);});}
observe();
async function loadStorage(entries){
 await page.close();
 const context=await browser.newContext({viewport:{width:390,height:844},storageState:{cookies:[],origins:[{origin:BASE,localStorage:Object.entries(entries).map(([name,value])=>({name,value}))}]}});
 page=await context.newPage();observe();await page.goto(BASE);await page.evaluate(()=>document.fonts.ready);
}
const read=()=>page.evaluate(()=>JSON.parse(localStorage.getItem('dawn.chronicle.v1')));
const clickAction=(action)=>page.locator(`[data-action="${action}"]`).filter({visible:true}).first().click();
const nav=async(id)=>{await page.locator(`.bottom-nav [data-id="${id}"]`).click();};
const noOverflow=async()=>assert.ok(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth+1),'No horizontal overflow');
try{
 await page.goto(BASE);await page.evaluate(()=>document.fonts.ready);
 await page.screenshot({path:'dist/welcome-mobile.png',fullPage:true});
 await page.getByLabel('이 이야기에 남길 이름').fill('루엔');
 await page.getByRole('button',{name:'첫 페이지 펼치기'}).click();
 assert.equal((await read()).hero.name,'루엔');await noOverflow();
 await page.screenshot({path:'dist/exploration-mobile.png',fullPage:true});
 await page.locator('[data-action="choose"][data-index="0"]').click();
 assert.equal((await read()).companions.length,1);
 await clickAction('continue');
 await page.locator('[data-action="choose"][data-index="0"]').click();
 let s=await read();assert.equal(s.mode,'combat');
 const before=JSON.stringify(s);await page.reload();assert.equal(JSON.stringify(await read()),before);
 await noOverflow();
 await page.screenshot({path:'dist/combat-mobile.png',fullPage:true});
 let steps=0;
 while((s=await read()).mode==='combat'&&steps++<90){
  const a=G.actorById(s,s.combat.active);
  let id=G.availableSkills(s,a).find(k=>SKILLS[k].power&&!G.skillLock(s,k))||'guard';
  if(a.hp<a.maxHp*.4&&s.potions>0)id='potion';
  const targets=G.validTargets(s,id),target=id==='potion'?a.id:targets[0].id;
  await page.locator(`[data-action="skill"][data-id="${id}"]`).click();
  if(targets.length>1)await page.locator(`[data-action="target"][data-id="${target}"]`).click();
  await clickAction('act');
 }
 assert.equal(s.mode,'outcome');assert.equal(s.victories,1);
 await clickAction('continue');await page.locator('[data-action="choose"][data-index="1"]').click();
 await nav('inventory');await page.locator('[data-action="equip"][data-id="staff"]').click();assert.equal((await read()).hero.weapon,'staff');
 await page.screenshot({path:'dist/inventory-mobile.png',fullPage:true});
 await nav('party');await page.locator('[data-action="reorder"][data-id="hero"][data-delta="1"]').click();assert.equal((await read()).formation[1],'hero');
 await page.screenshot({path:'dist/party-mobile.png',fullPage:true});
 await nav('adventure');await clickAction('continue');let weeks=(await read()).weeks;
 await page.locator('.mobile-camp [data-action="camp"]').click();await clickAction('confirm-camp');assert.equal((await read()).weeks,weeks+1);
 await nav('journal');assert.ok(await page.locator('.timeline article').count()>=4);await noOverflow();
 await clickAction('settings');await clickAction('gallery');assert.equal(await page.locator('.gallery img').count(),5);
 assert.ok(await page.locator('.gallery img').evaluateAll(imgs=>imgs.every(i=>i.complete&&i.naturalWidth>0)));
 await clickAction('close');await nav('adventure');
 // Saving and resuming an end-of-life state must retain the ending and offer a new life.
 const aged=await read();aged.weeks=90*52;aged.rng=87;aged.eventId='quiet_years';aged.mode='story';aged.outcome=null;
 await loadStorage({'dawn.chronicle.v1':JSON.stringify(aged)});
 await page.locator('[data-action="choose"][data-index="0"]').click();assert.equal((await read()).mode,'ended');
 await page.screenshot({path:'dist/ending-mobile.png',fullPage:true});
 await page.reload();assert.equal((await read()).mode,'ended');
 await clickAction('new-life');await page.locator('#new-name').fill('새벽');await clickAction('confirm-new');s=await read();assert.equal(s.weeks,0);assert.equal(s.hero.name,'새벽');
 assert.equal(await page.evaluate(()=>JSON.parse(localStorage.getItem('dawn.chronicle.v1.lives')).length),1);
 // Malformed primary save falls back to a valid previous action instead of crashing.
 await loadStorage({'dawn.chronicle.v1':'broken','dawn.chronicle.v1.backup':JSON.stringify(await read())});assert.ok(await page.locator('.save-warning').count());
 // Fresh desktop preview and narrow-screen / large-type overflow checks.
 const fresh=G.newGame('여행자',418);await loadStorage({'dawn.chronicle.v1':JSON.stringify(fresh)});
 await page.setViewportSize({width:1440,height:1040});await page.reload();await page.evaluate(()=>document.fonts.ready);await noOverflow();await page.screenshot({path:'dist/exploration-desktop.png',fullPage:true});
 for(const width of [320,360,412,768]){
  await page.setViewportSize({width,height:850});await page.evaluate(()=>document.documentElement.dataset.text='large');await noOverflow();
 }
 assert.deepEqual(errors,[]);assert.deepEqual(failed,[]);
 console.log('PASS: mobile start, recruitment, combat, combat reload, weapon equip, formation, camp, journal, five images, mortality, new life, backup recovery, 320–1440px layouts.');
}finally{await browser.close();}
