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
require(tileSize===64 && columns===4 && rows===4,'Expected a 4x4 sheet of 64px cells');
require(palette.length===33 && symbols.length===33 && new Set(symbols).size===33,'Expected transparent + 32 shared colors');
require(palette[0]==='00000000' && palette.slice(1).every(c=>/^[0-9a-f]{6}ff$/.test(c)),'Only binary alpha is allowed');
require(new Set(sprites.map(s=>s.slot)).size===15 && sprites.length===15,'Expected 15 unique slots');
for(const sprite of sprites) {
    require(sprite.slot>=0 && sprite.slot<15,sprite.id+': invalid slot');
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
console.log(`PASS: ${sprites.length} native 64x64 sprites; 32 shared colors; <=16 colors per sprite; binary alpha; 2px padding; ${width}x${height} PNG${process.argv.includes('--check')?' matches source':''}.`);
