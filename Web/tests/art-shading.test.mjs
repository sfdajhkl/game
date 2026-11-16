import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {ART_PALETTES,materialFamily,twoStepPixels} from '../web/art-shading.mjs';

test('every sampled RGB color resolves to one of exactly two flat colors in its material family',()=>{
 const input=[];for(let r=0;r<=255;r+=17)for(let g=0;g<=255;g+=17)for(let b=0;b<=255;b+=17)input.push(r,g,b,255);
 const source=Uint8ClampedArray.from(input),before=source.slice(),output=twoStepPixels(source),sets=ART_PALETTES.map(()=>new Set());
 assert.deepEqual(source,before);assert.equal(output.length,source.length);
 for(let i=0;i<source.length;i+=4){
  const family=materialFamily(...source.slice(i,i+3)),rgb=[...output.slice(i,i+3)];
  assert(ART_PALETTES[family].base.every((x,k)=>x===rgb[k])||ART_PALETTES[family].shadow.every((x,k)=>x===rgb[k]));
  sets[family].add(rgb.join(','));assert.equal(output[i+3],255);
 }
 for(const set of sets)assert.equal(set.size,2);
 assert.equal(new Set(sets.flatMap(x=>[...x])).size,12);
});
test('native pixel positions, dimensions and transparency remain intact without resampling',()=>{
 const input=new Uint8ClampedArray([0,0,0,0,18,15,10,255,218,180,100,255,10,15,30,0]),output=twoStepPixels(input);
 assert.equal(output.length,input.length);assert.deepEqual([output[3],output[7],output[11],output[15]],[0,255,255,0]);assert.deepEqual([...output.slice(0,4)],[0,0,0,0]);
});
test('flat, dark, empty and alpha-edge inputs stay bounded and deterministic',()=>{
 for(const input of [new Uint8ClampedArray(),new Uint8ClampedArray([20,20,20,255]),new Uint8ClampedArray([250,240,220,255]),new Uint8ClampedArray([50,60,70,127,50,60,70,128])]){
  assert.deepEqual(twoStepPixels(input),twoStepPixels(input));for(let i=3;i<input.length;i+=4)assert.equal(twoStepPixels(input)[i],input[i]<128?0:255);
 }
 assert.throws(()=>twoStepPixels(new Uint8ClampedArray(3)));
});
test('game, combat lab and event workshop all load the shared two-step renderer',()=>{
 for(const file of ['index.html','combat-lab.html','editor.html']){
  const html=readFileSync(new URL('../web/'+file,import.meta.url),'utf8');
  assert(html.includes('src="art-renderer.mjs"'),file);assert(html.includes('href="art-style.css"'),file);
 }
 const css=readFileSync(new URL('../web/art-style.css',import.meta.url),'utf8');
 assert(css.includes('.sprite,.combat-sprite'));assert(css.includes('--combat-scenery'));assert(!css.includes('body{filter'));
});
