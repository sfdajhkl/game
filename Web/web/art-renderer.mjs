import {twoStepPixels} from './art-shading.mjs';
const cache=new Map(),pending=new Map(),seen=new WeakMap();let serial=0,worker;
try{
 worker=new Worker(new URL('./art-worker.mjs',import.meta.url),{type:'module'});
 worker.onmessage=({data})=>{const job=pending.get(data.id);if(!job)return;pending.delete(data.id);data.error?job.reject(Error(data.error)):job.resolve(new Uint8ClampedArray(data.pixels));};
 worker.onerror=()=>{worker.terminate();worker=null;for(const job of pending.values())job.reject(Error('Art worker unavailable'));pending.clear();};
}catch{}
const decode=url=>new Promise((resolve,reject)=>{const img=new Image();img.onload=()=>resolve(img);img.onerror=()=>reject(Error('Art unavailable: '+url));img.src=url;});
async function convert(url){
 const img=await decode(url),canvas=document.createElement('canvas');canvas.width=img.naturalWidth;canvas.height=img.naturalHeight;
 const context=canvas.getContext('2d',{willReadFrequently:true});context.imageSmoothingEnabled=false;context.drawImage(img,0,0);
 const data=context.getImageData(0,0,canvas.width,canvas.height);
 let pixels;
 if(worker){try{pixels=await new Promise((resolve,reject)=>{const id=++serial;pending.set(id,{resolve,reject});const copy=data.data.slice();worker.postMessage({id,pixels:copy.buffer},[copy.buffer]);});}catch{pixels=twoStepPixels(data.data);}}
 else pixels=twoStepPixels(data.data);
 data.data.set(pixels);context.putImageData(data,0,0);
 const blob=await new Promise(resolve=>canvas.toBlob(resolve,'image/png'));canvas.width=canvas.height=0;
 if(!blob)throw Error('Art conversion failed');return URL.createObjectURL(blob);
}
function art(url){
 url=new URL(url,document.baseURI).href;
 if(!cache.has(url))cache.set(url,convert(url).catch(error=>{cache.delete(url);throw error;}));
 return cache.get(url);
}
function processImage(img){
 const src=img.getAttribute('src')||'';
 if(!/(?:^|\/)art\//.test(src)||/(?:paper-grain|natural-parchment)\.png/.test(src)||seen.get(img)===src)return;
 seen.set(img,src);img.classList.add('art-pending');
 art(src).then(url=>{if(seen.get(img)!==src||!img.isConnected)return;img.src=url;img.classList.remove('art-pending');img.dataset.shading='two-step';})
 .catch(()=>{if(seen.get(img)===src){img.classList.remove('art-pending');img.dataset.shading='unavailable';}});
}
function scan(node){if(node.nodeType!==1)return;if(node.matches('img'))processImage(node);node.querySelectorAll('img').forEach(processImage);}
const observer=new MutationObserver(records=>{for(const record of records){if(record.type==='attributes')processImage(record.target);else record.addedNodes.forEach(scan);}});
observer.observe(document.body,{childList:true,subtree:true,attributes:true,attributeFilter:['src']});scan(document.body);
for(const [property,file] of [['--character-art','characters.png'],['--combat-scenery','combat-parchment.png']]){
 const original=new URL('art/'+file,import.meta.url).href;
 art(original).then(url=>document.documentElement.style.setProperty(property,`url("${url}")`))
 .catch(()=>document.documentElement.style.setProperty(property,`url("${original}")`));
}
// The page is short-lived; URL caching avoids reprocessing art on every turn.
window.addEventListener('pagehide',event=>{if(event.persisted)return;observer.disconnect();worker?.terminate();for(const task of cache.values())task.then(URL.revokeObjectURL,()=>{});},{once:true});
