// One-time, reviewable migration of the authored prototype data. Unity runs C# only.
import fs from 'node:fs';
import path from 'node:path';
import {WEAPONS, SKILLS, COMPANIONS, ENEMIES, EVENTS} from '../Legacy/AndroidPrototype/web/content.mjs';
const rows = source => Object.entries(source).map(([id,value]) => ({id,...value}));
const gates = {
 reunion:{requiredFlag:'saved_child',minWeeks:104}, shrine:{excludedFallen:'elian'},
 shieldbearer:{excludedFallen:'borin',minWeeks:26}, warden:{requiredFlag:'ruin_clue',minLevel:3},
 dragon:{requiredFlag:'warden_defeated',minLevel:5}, veteran:{minWeeks:1040}, quiet_years:{minWeeks:1560}
};
const catalog={weapons:rows(WEAPONS),skills:rows(SKILLS),companions:rows(COMPANIONS),enemies:rows(ENEMIES),events:rows(EVENTS).map(e=>({...e,...gates[e.id]}))};
const output=process.argv[2]||'Unity/Arvito/Assets/Resources/Arvito/catalog.json';
fs.mkdirSync(path.dirname(output),{recursive:true});fs.writeFileSync(output,JSON.stringify(catalog,null,2)+'\n');
console.log(`${catalog.events.length} events, ${catalog.weapons.length} weapons, ${catalog.skills.length} skills exported`);
