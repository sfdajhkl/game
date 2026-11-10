#!/usr/bin/env node
// Export the editable integer pixel grid. No resampling, blending or dithering.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {deflateSync} from 'node:zlib';

const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const art=JSON.parse(fs.readFileSync(path.join(root,'Content/Art/characters.json'),'utf8'));
const {tileSize,columns,rows,symbols,palette,sprites}=art;
const width=tileSize*columns,height=tileSize*rows;
const pixels=Buffer.alloc(width*height*4);
function require(condition,message) { if(!condition) throw new Error(message); }
require(tileSize===64 && columns===4 && rows===6,'Expected a 4x6 sheet of 64px cells');
require(palette.length===33 && symbols.length===33 && new Set(symbols).size===33,'Expected transparent + 32 shared colors');
require(palette[0]==='00000000' && palette.slice(1).every(c=>/^[0-9a-f]{6}ff$/.test(c)),'Only binary alpha is allowed');
require(new Set(sprites.map(s=>s.slot)).size===21 && sprites.length===21,'Expected 21 unique slots');
require(new Set(sprites.map(s=>s.id)).size===21,'Duplicate sprite IDs');
for(const sprite of sprites) {
    require(sprite.slot>=0 && sprite.slot<22 && sprite.slot!==15,sprite.id+': invalid slot');
    require(sprite.rows.length===64 && sprite.rows.every(row=>row.length===64),sprite.id+': invalid raster');
    const used=new Set(sprite.rows.join(''));used.delete('.');
    require(used.size>1 && used.size<=16,sprite.id+': must use at most 16 opaque colors');
    for(let y=0;y<64;y++) for(let x=0;x<64;x++) {
        const symbol=sprite.rows[y][x],index=symbols.indexOf(symbol);
        require(index>=0,sprite.id+': unknown color symbol');
        require((x>=2 && x<62 && y>=2 && y<62)||index===0,sprite.id+': needs 2px clear padding');
        const offset=((Math.floor(sprite.slot/4)*64+y)*width+sprite.slot%4*64+x)*4;
        Buffer.from(palette[index],'hex').copy(pixels,offset);
    }
    // A neighboring atlas cell or a dropped one-pixel line must not leave floating fragments.
    const opaque=[];
    for(let y=0;y<64;y++)for(let x=0;x<64;x++)if(sprite.rows[y][x]!=='.')opaque.push(y*64+x);
    const visited=new Set([opaque[0]]),queue=[opaque[0]];
    while(queue.length){
        const cell=queue.pop(),x=cell%64,y=Math.floor(cell/64);
        for(let dy=-1;dy<=1;dy++)for(let dx=-1;dx<=1;dx++){
            const nx=x+dx,ny=y+dy,n=ny*64+nx;
            if(nx<0||ny<0||nx>=64||ny>=64||visited.has(n)||sprite.rows[ny][nx]==='.')continue;
            visited.add(n);queue.push(n);
        }
    }
    require(visited.size===opaque.length,sprite.id+': detached raster fragment');
}
const seams=JSON.parse(fs.readFileSync(path.join(root,'Content/Art/pixel-seams.json'),'utf8'));
for(const seam of seams.bowstrings){
    const sprite=sprites.find(s=>s.id===seam.id);
    require(sprite && symbols.includes(seam.color),'Unknown bowstring sprite/color');
    let [x,y]=seam.from;const [tx,ty]=seam.to;
    const dx=Math.abs(tx-x),dy=-Math.abs(ty-y),sx=x<tx?1:-1,sy=y<ty?1:-1;let err=dx+dy;
    while(true){
        require(sprite.rows[y]?.[x]===seam.color,seam.id+`: broken bowstring at ${x},${y}`);
        if(x===tx && y===ty)break;
        const e=2*err;if(e>=dy){err+=dy;x+=sx;}if(e<=dx){err+=dx;y+=sy;}
    }
}
function crc32(buffer) {
    let crc=0xffffffff;
    for(const byte of buffer) {crc^=byte;for(let n=0;n<8;n++)crc=(crc>>>1)^((crc&1)?0xedb88320:0);}
    return (crc^0xffffffff)>>>0;
}
function chunk(type,data) {
    const buffer=Buffer.alloc(data.length+12);buffer.writeUInt32BE(data.length);
    buffer.write(type,4);data.copy(buffer,8);buffer.writeUInt32BE(crc32(buffer.subarray(4,-4)),buffer.length-4);return buffer;
}
const header=Buffer.alloc(13);header.writeUInt32BE(width);header.writeUInt32BE(height,4);header[8]=8;header[9]=6;
const scanlines=Buffer.alloc((width*4+1)*height);
for(let y=0;y<height;y++)pixels.copy(scanlines,y*(width*4+1)+1,y*width*4,(y+1)*width*4);
const png=Buffer.concat([Buffer.from('89504e470d0a1a0a','hex'),chunk('IHDR',header),chunk('IDAT',deflateSync(scanlines,{level:9})),chunk('IEND',Buffer.alloc(0))]);
const output=path.join(root,'Unity/Arvito/Assets/Resources/Arvito/Art/characters.png');
if(process.argv.includes('--check'))require(fs.readFileSync(output).equals(png),'characters.png differs from the editable pixel grid');
else fs.writeFileSync(output,png);
console.log(`PASS: ${sprites.length} connected native 64x64 sprites; 32 shared colors; <=16 colors per sprite; binary alpha; 2px padding; ${seams.bowstrings.length} continuous bowstrings; ${width}x${height} PNG${process.argv.includes('--check')?' matches source':''}.`);
