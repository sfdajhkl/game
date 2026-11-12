import fs from 'node:fs';
fs.rmSync('dist',{recursive:true,force:true});fs.cpSync('web','dist',{recursive:true});console.log('Arvito browser game built without Unity.');
