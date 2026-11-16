import {twoStepPixels} from './art-shading.mjs';
self.onmessage=({data:{id,pixels}})=>{
 try{const result=twoStepPixels(new Uint8ClampedArray(pixels));self.postMessage({id,pixels:result.buffer},[result.buffer]);}
 catch(error){self.postMessage({id,error:error.message});}
};
