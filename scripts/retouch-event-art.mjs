#!/usr/bin/env node
// Painting is done with image_gen. This script only crops, samples, palettes,
// packages and records the generated artwork; it never synthesizes scene content.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {execFileSync} from 'node:child_process';
import {fileURLToPath} from 'node:url';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const meta=path.join(root,'Content/EventArt/retouch');
const work=process.env.ARVITO_RETOUCH_WORK || '/workspace/artifacts/Arvito-Event-Retouch';
const runtime=path.join(root,'Unity/Arvito/Assets/Resources/Arvito/Art/Events');
const study='/workspace/artifacts/Arvito-Art-Study-5';
const read=p=>JSON.parse(fs.readFileSync(p,'utf8'));
const write=(p,x)=>{fs.mkdirSync(path.dirname(p),{recursive:true});fs.writeFileSync(p,JSON.stringify(x,null,2)+'\n');};
const sha=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const run=(cmd,args)=>execFileSync(cmd,args,{encoding:'utf8',stdio:['ignore','pipe','pipe']});
const approved=['q073_bargain','q080_offer','q081_offer','q082_bargain','q090_offer'];
const planPath=path.join(meta,'plan.json');
const scenePlan=read(path.join(root,'Content/EventArt/scenes.json')).scenes;
const findBatch=(plan,key)=>[...plan.batches,...(plan.repairBatches||[])].find(b=>b.key===key);

function status(){
  const plan=read(planPath);
  const records=fs.readdirSync(path.join(meta,'records')).filter(f=>f.endsWith('.json')).map(f=>read(path.join(meta,'records',f)));
  const repairs=fs.existsSync(path.join(meta,'repairs.json'))?read(path.join(meta,'repairs.json')).filter(r=>r.status!=='resolved'):[];
  const pendingRepairIds=new Set(repairs.map(r=>r.id));
  const done=new Set(records.filter(r=>r.status==='reviewed' && r.styleRevision===plan.styleRevision && !pendingRepairIds.has(r.id)).map(r=>r.id));
  const state={goal:'Retouch all 500 event illustrations: approved staging, unified parchment/sepia palette and two tones per material; create one illustrated event UI design sample',baseCommit:plan.baseCommit,styleRevision:plan.styleRevision,total:500,reviewed:done.size,remaining:500-done.size,pendingRepairs:repairs,completed: [...done].sort(),pendingBatches:plan.batches.filter(b=>b.ids.some(id=>!done.has(id))).map(b=>b.key),updatedAt:new Date().toISOString()};
  write(path.join(work,'WORK_STATE.json'),state);
  console.log(JSON.stringify({reviewed:state.reviewed,remaining:state.remaining,next:state.pendingBatches.slice(0,8)}));
}

const [cmd,key,source,...notes]=process.argv.slice(2);
if(cmd==='init'){
  if(fs.existsSync(planPath)) throw Error('Already initialized; refusing to overwrite baseline');
  for(const folder of ['before','sources','targets','previews','candidates']) fs.mkdirSync(path.join(work,folder),{recursive:true});
  for(const folder of ['records','batches','prompts']) fs.mkdirSync(path.join(meta,folder),{recursive:true});
  const baseline={};
  for(const e of scenePlan){
    const p=path.join(runtime,e.id+'.png');
    fs.copyFileSync(p,path.join(work,'before',e.id+'.png'));
    baseline[e.id]={sha256:sha(p),metaSha256:sha(p+'.meta')};
  }
  const groups=[],echoes=[];
  const base=scenePlan.filter(e=>!/^q\d{3}_/.test(e.id)).map(e=>e.id);
  for(let i=0;i<base.length;i+=4) groups.push({key:'base-'+String(i/4+1).padStart(2,'0'),ids:base.slice(i,i+4)});
  // A full arc has four closely related core scenes; the fifth goes into an
  // explicitly described aftermath sheet. Approved phases are reused verbatim.
  for(let q=1;q<=95;q++){
    const arc='q'+String(q).padStart(3,'0');
    const ids=['offer','trail','bargain','echo_a','echo_b'].map(p=>arc+'_'+p).filter(id=>!approved.includes(id));
    groups.push({key:arc,ids:ids.slice(0,4)});
    echoes.push(...ids.slice(4));
  }
  // Fill the last base sheet with spare aftermaths, then pack the remainder.
  const lastBase=groups.find(b=>b.key==='base-07');
  while(lastBase.ids.length<4 && echoes.length) lastBase.ids.push(echoes.shift());
  for(let i=0;i<echoes.length;i+=4) groups.push({key:'echo-'+String(i/4+1).padStart(2,'0'),ids:echoes.slice(i,i+4)});
  const baseCommit=run('git',['-C',root,'rev-parse','HEAD']).trim();
  write(planPath,{version:2,baseCommit,width:192,height:128,maxColors:24,approvedStudies:approved,baseline,batches:groups});
  write(path.join(meta,'directions.json'),{});
  for(const id of approved){
    const original=path.join(study,'after',id+'.png');
    const raw=path.join(study,'sources',id+'-source.png');
    fs.copyFileSync(original,path.join(runtime,id+'.png'));
    fs.copyFileSync(raw,path.join(work,'sources',id+'-approved.png'));
    write(path.join(meta,'records',id+'.json'),{id,status:'reviewed',kind:'approved-study',beforeSha256:baseline[id].sha256,afterSha256:sha(original),sourceFile:id+'-approved.png',sourceSha256:sha(raw),review:'User approved the five-study before/after comparison; native PNG reused byte-for-byte.'});
  }
  status();
}else if(cmd==='prepare'){
  const plan=read(planPath), batch=findBatch(plan,key);
  if(!batch)throw Error('Unknown batch '+key);
  const directions=read(path.join(meta,'directions.json'));
  const positions=['TOP LEFT','TOP RIGHT','BOTTOM LEFT','BOTTOM RIGHT'];
  let prompt=fs.readFileSync(path.join(meta,'art-direction.txt'),'utf8')+'\n\nFOUR SCENES:\n';
  for(let i=0;i<4;i++){
    const id=batch.ids[i];
    if(!id){prompt+=`\n${positions[i]}: unused spare cell, completely flat parchment background, no people or objects. This cell is not imported.\n`;continue;}
    const e=scenePlan.find(e=>e.id===id);
    const direction=batch.directions?.[id]||directions[id];
    if(!direction)throw Error('Missing authored direction: '+id);
    prompt+=`\n${positions[i]} — ${id}: ${e.title}\nAUTHORITATIVE NEW COMPOSITION: ${direction}\nEVENT FACTS / permitted cast (staging here is replaced by the new composition above): ${e.brief}\nKorean source: ${e.sourceText.join(' ')}\n`;
  }
  fs.writeFileSync(path.join(meta,'prompts',key+'.txt'),prompt);
  const imgs=batch.ids.map(id=>batch.referenceMode==='current'?path.join(runtime,id+'.png'):path.join(work,'before',id+'.png'));
  if(imgs.length<4)imgs.push(...Array(4-imgs.length).fill(path.join(work,'before','wayside_rest.png')));
  const target=path.join(work,'targets',key+'.png');
  run('magick',['montage',...imgs,'-tile','2x2','-geometry','+0+0',target]);
  run('magick',[target,'-sample','400%',target]);
  console.log(JSON.stringify({key,ids:batch.ids,prompt:path.join(meta,'prompts',key+'.txt'),target}));
}else if(cmd==='import'){
  const plan=read(planPath),batch=findBatch(plan,key);
  if(!batch||!source)throw Error('Usage: import <batch> <source PNG>');
  const size=run('magick',['identify','-format','%wx%h',source]).trim();
  if(size!=='1536x1024')throw Error('Expected a 1536x1024 2x2 sheet; got '+size);
  const sourceHash=sha(source),basename=key+'-'+sourceHash.slice(0,12)+'.png';
  const saved=path.join(work,'sources',basename);fs.copyFileSync(source,saved);
  const dest=path.join(work,'candidates',key);fs.mkdirSync(dest,{recursive:true});
  const images=[];
  for(let i=0;i<batch.ids.length;i++){
    const out=path.join(dest,batch.ids[i]+'.png');
    const paletteArgs=plan.paletteFile?['-remap',path.join(meta,plan.paletteFile)]:['-colors',String(plan.maxColors)];
    run('magick',[saved,'-crop',`768x512+${(i%2)*768}+${Math.floor(i/2)*512}`,'+repage','-sample','192x128!','-alpha','off','-dither','None',...paletteArgs,'-strip','PNG8:'+out]);
    images.push(out);
  }
  const preview=path.join(work,'previews',key+'.png');
  run('magick',['montage',...images,'-tile','2x2','-geometry','+0+0','-background','#20232a',preview]);
  run('magick',[preview,'-sample','400%',preview]);
  const prompt=path.join(meta,'prompts',key+'.txt');
  write(path.join(meta,'batches',key+'.json'),{key,events:batch.ids,styleRevision:plan.styleRevision,status:'pending-review',sourceFile:basename,sourceSha256:sourceHash,promptSha256:sha(prompt),images:images.map((p,i)=>({id:batch.ids[i],sha256:sha(p)}))});
  console.log(JSON.stringify({key,preview,events:batch.ids}));
}else if(cmd==='approve'){
  const plan=read(planPath),batch=read(path.join(meta,'batches',key+'.json'));
  const review=[source,...notes].filter(Boolean).join(' ');
  if(!review)throw Error('A concrete visual review note is required');
  if(batch.styleRevision!==plan.styleRevision)throw Error('Stale style revision');
  if(sha(path.join(meta,'prompts',key+'.txt'))!==batch.promptSha256)throw Error('Prompt changed since generation');
  if(sha(path.join(work,'sources',batch.sourceFile))!==batch.sourceSha256)throw Error('Source changed');
  for(let i=0;i<batch.images.length;i++){
    const e=batch.images[i],p=path.join(work,'candidates',key,e.id+'.png');
    if(sha(p)!==e.sha256)throw Error('Candidate changed since import');
    fs.copyFileSync(p,path.join(runtime,e.id+'.png'));
    write(path.join(meta,'records',e.id+'.json'),{id:e.id,status:'reviewed',styleRevision:batch.styleRevision,kind:'generated-retouch',batch:key,cell:i,beforeSha256:plan.baseline[e.id].sha256,afterSha256:e.sha256,sourceFile:batch.sourceFile,sourceSha256:batch.sourceSha256,promptSha256:batch.promptSha256,review});
  }
  batch.status='reviewed';batch.review=review;write(path.join(meta,'batches',key+'.json'),batch);status();
}else if(cmd==='resolve'){
  const plan=read(planPath),repairsPath=path.join(meta,'repairs.json'),repairs=read(repairsPath);
  const repair=repairs.find(r=>r.id===key),record=read(path.join(meta,'records',key+'.json'));
  const review=[source,...notes].filter(Boolean).join(' ');
  if(!repair||!review)throw Error('An existing repair and specific visual review are required');
  if(!(plan.repairBatches||[]).some(b=>b.key===record.batch&&b.ids.includes(key)))throw Error('No reviewed repair batch for '+key);
  if(record.styleRevision!==plan.styleRevision||record.status!=='reviewed'||sha(path.join(runtime,key+'.png'))!==record.afterSha256)throw Error('Stale repair record');
  if(repair.originalSha256===record.afterSha256)throw Error('Repair did not change the artwork');
  repair.status='resolved';repair.resolvedSha256=record.afterSha256;repair.review=review;
  write(repairsPath,repairs);status();
}else if(cmd==='status'){status();}
else throw Error('Use init | prepare KEY | import KEY SOURCE | approve KEY NOTE | resolve ID NOTE | status');
