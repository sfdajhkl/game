#!/usr/bin/env node
// Technical raster bake only: illustration content is authored by image generation.
// Crop the ordered sheet, sample to the game's native grid, and cap flat colors.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { execFileSync } from 'node:child_process';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const plan=JSON.parse(fs.readFileSync(path.join(root,'Content/EventArt/scenes.json'),'utf8'));
const [sheetArg,source,mode]=process.argv.slice(2);
const sheet=Number(sheetArg);
if (!Number.isInteger(sheet)||sheet<1||!source) throw new Error('Usage: node scripts/import-event-art.mjs SHEET SOURCE.png');
const repair=mode==='--repair';
const scenes=repair
  ? JSON.parse(fs.readFileSync(path.join(root,'Content/EventArt/repairs.json'),'utf8')).slice((sheet-1)*16,sheet*16).map((e,i)=>({...e,image:e.id+'.png',row:Math.floor(i/4)+1,column:i%4+1}))
  : plan.scenes.filter(e=>e.sheet===sheet);
const recordKey=repair?`repair-${sheet}`:String(sheet);
if(!scenes.length) throw new Error('Unknown sheet');
const size=execFileSync('magick',['identify','-format','%w %h',source],{encoding:'utf8'}).trim();
if(size!=='1536 1024') throw new Error(`Expected a 1536x1024 source sheet, got ${size}`);
const out=path.join(root,'Unity/Arvito/Assets/Resources/Arvito/Art/Events');
fs.mkdirSync(out,{recursive:true});
for(const scene of scenes){
  const crop=`384x256+${(scene.column-1)*384}+${(scene.row-1)*256}`;
  execFileSync('magick',[source,'-crop',crop,'+repage','-sample','192x128!','-alpha','off','-dither','None','-colors','32','-strip',`PNG8:${path.join(out,scene.image)}`]);
}
const provenance=path.join(root,'Content/EventArt/sources.json');
const lock=provenance+'.lock';
let locked=false;
for(let attempt=0;attempt<200&&!locked;attempt++){
  try { fs.mkdirSync(lock); locked=true; }
  catch(error){if(error.code!=='EEXIST')throw error;Atomics.wait(new Int32Array(new SharedArrayBuffer(4)),0,0,50);}
}
if(!locked)throw new Error('Another import is updating sources.json; retry after it finishes.');
try{
  const records=fs.existsSync(provenance)?JSON.parse(fs.readFileSync(provenance,'utf8')):{};
  records[recordKey]={source:path.basename(source),sha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),events:scenes.map(e=>e.id)};
  const temporary=provenance+`.${process.pid}.tmp`;
  fs.writeFileSync(temporary,JSON.stringify(records,null,2)+'\n');
  fs.renameSync(temporary,provenance);
}finally{fs.rmdirSync(lock);}
const reviews='/workspace/artifacts/event-art/batches';
fs.mkdirSync(reviews,{recursive:true});
const tag=repair?`repair-${sheet}`:`sheet-${String(sheet).padStart(2,'0')}`;
const native=path.join(reviews,`${tag}-native.png`);
execFileSync('magick',['montage',...scenes.map(e=>path.join(out,e.image)),'-background','#343030','-tile','4x4','-geometry','192x128+0+0',native]);
execFileSync('magick',[native,'-sample','200%',path.join(reviews,`${tag}-preview.png`)]);
console.log(`${tag}: imported ${scenes.length} distinct 192x128 scenes.`);
