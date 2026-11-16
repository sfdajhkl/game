// Two flat values per material family, shared by every illustration and sprite.
// Paper UI textures are materials of the interface, not illustrations.
export const ART_PALETTES = Object.freeze([
 {name:'stone',shadow:[81,71,55],base:[201,190,161]},
 {name:'leather',shadow:[80,54,41],base:[155,115,87]},
 {name:'ochre',shadow:[122,99,68],base:[200,176,135]},
 {name:'olive',shadow:[75,77,52],base:[145,137,99]},
 {name:'steel',shadow:[70,80,77],base:[140,150,144]},
 {name:'plum',shadow:[79,65,73],base:[145,130,132]}
]);
export function materialFamily(r,g,b) {
 const max=Math.max(r,g,b),min=Math.min(r,g,b),delta=max-min;
 if(delta<=18 || delta/Math.max(1,max)<.12)return 0;
 let hue=(max===r?(g-b)/delta:max===g?(b-r)/delta+2:(r-g)/delta+4)*60;
 if(hue<0)hue+=360;
 return hue<25||hue>=345?1:hue<65?2:hue<175?3:hue<265?4:5;
}
function threshold(histogram) {
 let total=0,sum=0;for(let i=0;i<256;i++){total+=histogram[i];sum+=i*histogram[i];}
 if(!total)return 128;
 let weight=0,leftSum=0,best=-1,cut=sum/total;
 for(let i=0;i<255;i++){
  weight+=histogram[i];leftSum+=i*histogram[i];if(!weight||weight===total)continue;
  const distance=leftSum/weight-(sum-leftSum)/(total-weight),score=weight*(total-weight)*distance*distance;
  if(score>best){best=score;cut=i;}
 }
 return Math.max(60,Math.min(180,cut));
}
export function twoStepPixels(input) {
 if(input.length%4)throw new Error('RGBA pixel data required');
 const count=input.length/4,families=new Uint8Array(count),lights=new Uint8Array(count),histograms=ART_PALETTES.map(()=>new Uint32Array(256));
 for(let p=0;p<count;p++){
  const i=p*4;if(input[i+3]<128)continue;
  const family=materialFamily(input[i],input[i+1],input[i+2]),light=Math.round(.2126*input[i]+.7152*input[i+1]+.0722*input[i+2]);
  families[p]=family;lights[p]=light;histograms[family][light]++;
 }
 const cuts=histograms.map(threshold),output=new Uint8ClampedArray(input.length);
 for(let p=0;p<count;p++){
  const i=p*4;if(input[i+3]<128)continue;
  const family=families[p],color=ART_PALETTES[family][lights[p]<=cuts[family]?'shadow':'base'];
  output[i]=color[0];output[i+1]=color[1];output[i+2]=color[2];output[i+3]=255;
 }
 return output;
}
