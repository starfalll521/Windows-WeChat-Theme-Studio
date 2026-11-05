import fs from 'node:fs';import zlib from 'node:zlib';import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
if (process.argv.length !== 3) throw Error('Usage: node tools/extract-ui-svg.mjs <original Weixin.dll>');
const b = fs.readFileSync(process.argv[2]);
if (crypto.createHash('sha256').update(b).digest('hex') !== 'e0428a7b691853de496532523c2c09aa095f90941c37c9f4cfb3f137f3c66556') throw Error('Original Weixin.dll fingerprint mismatch');
const pe = b.readUInt32LE(0x3c);
const count = b.readUInt16LE(pe + 6);
const first = pe + 24 + b.readUInt16LE(pe + 20);
function raw(rva) {
  for (let i = 0; i < count; i++) {
    const section = first + i * 40;
    const address = b.readUInt32LE(section + 12);
    const delta = rva - address;
    if (delta >= 0 && delta < b.readUInt32LE(section + 16)) return b.readUInt32LE(section + 20) + delta;
  }
  throw Error('Unmapped RVA');
}
const tree=raw(0x8ef0ad0),names=raw(0x8ed2ef0),data=raw(0x86a4dc0);const out=path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../dist/assets/native-svg');fs.mkdirSync(out,{recursive:true});let entries=[];const seen=new Set();
function visit(index,prefix,depth){if(depth>32||seen.has(index)||index>50000)throw Error('Invalid tree');seen.add(index);const o=tree+index*22,no=names+b.readUInt32BE(o),len=b.readUInt16BE(no);let name='';for(let i=0;i<len;i++)name+=String.fromCharCode(b.readUInt16BE(no+6+i*2));const path=prefix+(name?'/'+name:'');const flags=b.readUInt16BE(o+4);if(flags&2){let count=b.readUInt32BE(o+6),first=b.readUInt32BE(o+10);if(count>20000)throw Error('Count');for(let i=0;i<count;i++)visit(first+i,path,depth+1);}else if(path.endsWith('.svg')){const at=data+b.readUInt32BE(o+10),n=b.readUInt32BE(at);if(n>1e6)throw Error('Length');let svg=b.subarray(at+4,at+4+n);if(flags&1)svg=zlib.inflateSync(svg.subarray(4));if(flags&4)throw Error('Zstd SVG not supported');const text=svg.toString('utf8');if(!text.includes('<svg'))throw Error('Not SVG');const key=crypto.createHash('sha256').update(path).digest('hex');fs.writeFileSync(out+'/'+key+'.svg',svg);entries.push({uri:':'+path,file:key+'.svg'});}}
visit(0,'',0);fs.writeFileSync(out+'/manifest.json',JSON.stringify(entries,null,2));console.log(JSON.stringify({svg:entries.length,nodes:seen.size,samples:entries.filter(x=>x.uri.includes('bubble_circle_filled')).slice(0,3)}));

