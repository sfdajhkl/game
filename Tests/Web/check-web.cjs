// Browser shell + real .jslib contract checks. This does NOT execute a Unity/Wasm player.
const fs=require('fs'),path=require('path'),http=require('http'),assert=require('assert/strict');
const {chromium}=require(process.env.ARVITO_PLAYWRIGHT || 'playwright');
const root=path.resolve(__dirname,'../..');
let template=fs.readFileSync(path.join(root,'Unity/Arvito/Assets/WebGLTemplates/Arvito/index.html'),'utf8');
for(const [key,value] of Object.entries({DATA_FILENAME:'test.data',FRAMEWORK_FILENAME:'test.framework.js',CODE_FILENAME:'test.wasm',LOADER_FILENAME:'test.loader.js','JSON.stringify(COMPANY_NAME)':'"Arvito"','JSON.stringify(PRODUCT_NAME)':'"아르비토"','JSON.stringify(PRODUCT_VERSION)':'"test"'}))template=template.split('{{{ '+key+' }}}').join(value);
assert(!template.includes('{{{'));
let scenario='success';
const server=http.createServer((req,res)=>{
 if(req.url==='/'){res.setHeader('Content-Type','text/html; charset=utf-8');return res.end(template);}
 if(req.url==='/Build/test.loader.js'){
  if(scenario==='missing'){res.writeHead(404);return res.end();}
  res.setHeader('Content-Type','text/javascript');
  return res.end(scenario==='reject'?'window.createUnityInstance=()=>Promise.reject(new Error("test load failure"));':'window.createUnityInstance=(canvas,config,progress)=>{window.checkedConfig=config;progress(1);return Promise.resolve({SetFullscreen:()=>{window.fullscreenRequested=true;}});};');
 }
 res.writeHead(404).end();
});
(async()=>{
 await new Promise(r=>server.listen(0,'127.0.0.1',r));
 const browser=await chromium.launch({executablePath:process.env.ARVITO_CHROMIUM || '/usr/bin/chromium',headless:true,args:['--no-sandbox']});
 try {
 const page=await browser.newPage();const url='http://127.0.0.1:'+server.address().port;
 const errors=[];page.on('pageerror',e=>errors.push(e.message));
 await page.goto(url);await page.locator('#loading').waitFor({state:'hidden'});
 await page.locator('#fullscreen').click();assert(await page.evaluate(()=>window.fullscreenRequested));
 for(const width of [1440,390]){await page.setViewportSize({width,height:900});assert(await page.evaluate(()=>document.documentElement.scrollWidth<=innerWidth));}
 const bridge=fs.readFileSync(path.join(root,'Unity/Arvito/Assets/Plugins/WebGL/ArvitoStorage.jslib'),'utf8');
 async function installBridge(){
  await page.evaluate(()=>{
   window.LibraryManager={library:{}};window.mergeInto=(target,source)=>Object.assign(target,source);
   const heap=new Uint8Array(4*1024*1024);let next=8;
   window._malloc=n=>{const p=next;next+=n;return p;};
   window.lengthBytesUTF8=s=>new TextEncoder().encode(s).length;
   window.stringToUTF8=(s,p,n)=>{const bytes=new TextEncoder().encode(s);heap.set(bytes.subarray(0,n-1),p);heap[p+Math.min(bytes.length,n-1)]=0;};
   window.UTF8ToString=p=>{let end=p;while(heap[end])end++;return new TextDecoder().decode(heap.subarray(p,end));};
   window.pointer=s=>{const n=lengthBytesUTF8(s)+1,p=_malloc(n);stringToUTF8(s,p,n);return p;};
  });
  await page.addScriptTag({content:bridge});
  await page.evaluate(()=>{window.ArvitoStorageState=LibraryManager.library.$ArvitoStorageState;});
 }
 await installBridge();
 assert.equal(await page.evaluate(()=>LibraryManager.library.ArvitoStorageWrite(pointer('arvito.test'),pointer('여행자 뷁힣 · 16세'))),0);
 await page.reload();await page.locator('#loading').waitFor({state:'hidden'});await installBridge();
 assert.equal(await page.evaluate(()=>UTF8ToString(LibraryManager.library.ArvitoStorageRead(pointer('arvito.test')))),'여행자 뷁힣 · 16세');
 assert.equal(await page.evaluate(()=>LibraryManager.library.ArvitoStorageRead(pointer('missing'))),0);
 assert.equal(await page.evaluate(()=>LibraryManager.library.ArvitoStorageReadFailed()),0);
 assert.equal(await page.evaluate(()=>{
   const original=Storage.prototype.setItem;Storage.prototype.setItem=()=>{throw new DOMException('full','QuotaExceededError');};
   try{return LibraryManager.library.ArvitoStorageWrite(pointer('arvito.test'),pointer('overwrite'));}finally{Storage.prototype.setItem=original;}
 }),1);
 assert.equal(await page.evaluate(()=>localStorage.getItem('arvito.test')),'여행자 뷁힣 · 16세');
 assert.equal(await page.evaluate(()=>{
   const original=Storage.prototype.getItem;Storage.prototype.getItem=()=>{throw new DOMException('denied','SecurityError');};
   try{LibraryManager.library.ArvitoStorageRead(pointer('arvito.test'));return LibraryManager.library.ArvitoStorageReadFailed();}finally{Storage.prototype.getItem=original;}
 }),1);
 for(const mode of ['missing','reject']){scenario=mode;await page.goto(url);await page.locator('#retry').waitFor({state:'visible'});assert.equal(await page.locator('#fullscreen').isDisabled(),true);}
 assert.deepEqual(errors,[]);
 console.log('PASS: web shell load/fullscreen/error UI, desktop/mobile overflow, .jslib Korean reload persistence and storage failures.');
 console.log('Unity loader is a test double; actual WebGL/IL2CPP gameplay still requires a licensed build.');
 } finally {await browser.close();server.close();}
})().catch(e=>{console.error(e);server.close();process.exitCode=1;});
