#!/usr/bin/env python3
"""Prepare one scene brief per catalog event and ordered generation sheets."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'Content/EventArt'
CATALOG = ROOT / 'Unity/Arvito/Assets/Resources/Arvito/catalog.json'
BASE = {
    'departure': 'Across a small wooden bridge, the female hunter Mira sits beside a tree with an injured ankle, raising one hand to ask the unseen viewer for help. No other adventurer.',
    'first_hunt': 'Two hungry wolves watch a torn provision sack on a moonlit forest road. Low view down the empty road, one wolf emerging from undergrowth.',
    'crossroads': 'A roadside merchant behind a faded cloth displaying a modest sword, wooden bow, staff and axe. Welcoming gesture toward the unseen customer; simple functional weapons.',
    'lost_child': 'A lost small child crouches between huge tree roots in fog, clutching a tiny wooden toy horse. The toy and worried posture are the focal point.',
    'reunion': 'An adult shop assistant presents a worn wooden toy horse on a shop counter, quietly grateful. Seen from the customer side, no customer visible.',
    'shrine': 'APPROVED EXISTING IMAGE: a healer bandages a seated injured traveler inside an abandoned stone shrine. Empty foreground; a dying candle and herbs on an altar.',
    'shieldbearer': 'Borin, a mature bearded veteran in scarred practical armor, sits alone by a campfire, staring into a cold cup. His battered shield rests unused beside him.',
    'bandit_road': 'A fallen tree blocks a mountain road. Two rough bandits and a bow-carrying bandit confront the unseen viewer from behind the trunk. Bowstring continuous.',
    'old_ruin': 'Below a collapsed tower, an immense closed stone door carries a simple carved constellation. A narrow cold blue slit contrasts with warm dusty stone. No people.',
    'warden': 'An ancient massive iron-and-stone guardian rises amid dust in a ruined hall, facing the unseen viewer. Simple heavy angular silhouette, tiny dim eye lights. No adventurers.',
    'dragon': 'A colossal muted red dragon spreads its wings over a stone citadel as ash falls. Tiny ordinary civilians retreat through a distant gate. No hero or party.',
    'caravan': 'A slow caravan of two covered wooden merchant wagons on a broad winding road. An older merchant walks beside the lead wagon and a mule. No adventuring escorts.',
    'mentor': 'An elderly fencing teacher stands alone in an open practice yard holding a plain wooden branch. Wooden training dummy and practice posts nearby. No student avatar.',
    'smith': 'A broad-shouldered blacksmith working a glowing iron blade on an anvil in a modest stone forge. Warm ember highlights, large simple shapes, no customer visible.',
    'spring': 'An unexpectedly flowering branch over an empty country path, a few pale blossoms against bare trees and subdued green spring grass. Quiet observational still life.',
    'winter': 'From inside an empty rustic inn, a window shows deep snow blocking a mountain pass. A fire, stacked split logs, empty stools and steaming kettle; no party members.',
    'whisper': 'A grey-robed grave priest turns toward the unseen viewer in a wet marsh graveyard. Faint blue will-o-wisps above leaning gravestones and two old skeletons in the shadows.',
    'bridge': 'A flood has torn out the center of a wooden bridge. Two farmers and their produce cart wait on the far bank. Fast brown river and broken beams dominate the view.',
    'scholar': 'An older scholar studies a simple star chart on a stone table in a roofless observatory, under a sparse night sky. Broken circular arch, no visiting hero.',
    'homecoming': 'An open cottage doorway in a familiar village square, ordinary new residents tending daily chores. Empty foreground welcomes the unseen returning traveler; no fixed family.',
    'veteran': 'A young novice traveler holds a worn sword lowered in both hands and looks earnestly toward the unseen viewer. Village roadside, small pack, no older hero depicted.',
    'quiet_years': 'An empty wooden chair beside a slow river under autumn branches, a closed travel journal on the seat. Peaceful late-afternoon landscape; no aged player body.',
    'market': 'A modest roadside stall beneath ochre and olive canvas. A merchant fills a bottle beside herbs, bread and clay jugs, facing the unseen customer. No customer hands.',
    'wayside_rest': 'An empty wayside rest beneath a weathered wooden signpost: flat sitting stones, shade, a clear path toward distant hills. No readable writing, no owner-specific gear.',
    'roaming_post': 'An old postal wagon stops at a crossroads. A driver sorts tied letters and returned parcels in its open back, preparing to leave. No passenger or player visible.',
}
PHASE = {
    'offer': 'FIRST ENCOUNTER. Wide establishing view of the unresolved problem in the hook. Include its specific object, location, and only the local NPCs named by the situation. Do not depict a solution yet.',
    'trail': 'INVESTIGATION. A closer, different camera view centered on the concrete clue and the physical work/site being investigated. Make this a person-free still life or environmental view: NO people, faces, body parts, hands or held tools. Rest maps and clues on a surface. Show the clue clearly; the investigator stays entirely outside the frame.',
    'bargain': 'NEGOTIATION. Depict the relevant LOCAL workers/residents facing one another around the disputed object or shared resources. Show practical stakes visually; no player negotiator, no generic tavern scene. A trapped or endangered victim is NOT already safe and joining this discussion: only available local helpers plan the rescue, while the victim remains in danger or off-screen.',
    'echo_a': 'AFTERMATH OF CHOICE A. Depict the specific changed place, repaired object, revealed evidence or local NPC activity in result A. Different framing from the first encounter. The hero who helped remains unseen.',
    'echo_b': 'AFTERMATH OF CHOICE B. Depict the specific communal solution or social consequence in result B. Local people and the physical outcome matter; distinct from result A. No hero or party.',
}
STYLE = '''Create a production contact sheet of SIXTEEN separate 3:2 fantasy RPG event illustrations, arranged in an EXACT 4-column by 4-row grid, total canvas 1536 x 1024. Every cell is exactly 384 x 256. No margins, no gutters, no borders, no labels, no numbers, no lettering, no UI. Images must end exactly at the cell edges and never continue into neighboring cells. Follow the row/column assignments exactly.

Match the reference's ORIGINAL restrained fantasy cartoon illustration style: naturally rounded human anatomy with modest angular accents, relaxed continuous outlines, mature but approachable proportions, quiet human storytelling, muted earth/olive/slate/cream palette and grounded medieval fantasy. Preserve the original level of softness. Do not exaggerate square jaws, triangular noses, heavy black outlines, bulky fists, blocky musculature, rugged caricatures or aggressive faces. Do not make the characters more angular than the reference. Vary age and build naturally without caricature. Each panel must read as a carefully composed hand-placed 192 x 128 pixel picture enlarged exactly 2x: square consistent 2x2 pixel blocks, clean coherent clusters, limited flat colors (around 24-32), sparse detail and purposeful continuous contours.

HUMAN BODY PROPORTIONS: avoid the shared short, round barrel-shaped torso and stubby limbs. Give most working adults a lean natural torso, visible waist, narrower hips, longer forearms and legs, and a relatively small head (roughly 6-7 heads tall when standing). Maintain natural curved anatomy and modest line weight, not faceted geometry. Distinct silhouettes by role: hunters/archers lean and athletic; scholars/elders slender with slightly stooped posture; warriors broad in the shoulders with a defined waist; smiths strong in the back and forearms without a round belly; merchants may be heavier but not every NPC. Children are naturally smaller, not chibi. Vary women and men, age, build and stance among local NPCs. Clothing hangs on a body rather than inflating into a round sack. No uniformly bulky hands, balloon sleeves, thick cylinders for legs or oversized round heads.

HUMAN EYES ARE OMITTED, without changing the face shape: no little eye dots, pupils, eye whites, eyelid lines or eyelashes. Leave that small area the same natural skin tone or existing soft facial shade, not a dark horizontal bar. No blindfolds, sunglasses, eye bands or large empty masks. Keep the original nose, mouth, cheek, hair, beard and natural head proportions. Emotion comes from the original mouth shape, head tilt and posture. Animal/monster eyes may remain minimal. Use scene-appropriate mouth shapes and posture: worried or grieving NPCs must not smile; injured and threatened people look guarded or tired. Do not give every person a friendly smile.

ALL OBJECT CLOSEUPS use the SAME flat outlined cartoon style as the people. Glass uses a few solid translucent shapes; metal uses two or three broad tones; stone uses simple broad blocks. No macro-photography, realistic rusty grain, rope fuzz, sand grains, reflective rendering or photographic texture under a pixel filter. Simplify materials rather than stippling them.

No smooth brushwork, photographic detail, anti-aliasing, dithering, tiny decorative texture, gradients, noisy speckles, shiny ornate armor, anime eyes or chibi proportions. Large areas of calm negative space and clear focal objects. Use subtle appropriate daylight, weather or lamplight; not every scene brown or dark.

CRITICAL COMPOSITION: The viewer is the unseen traveler. NEVER depict the playable hero or their companions, even as backs, silhouettes, shadows, hands, held weapons, reflections, or an anonymous adventurous party. Only fixed local event NPCs, animals and enemies described for a scene may appear. When a hook mentions just ONE local NPC, show that person ALONE, addressing the unseen viewer; never add a second traveler as their conversation partner. In negotiations every visible person must have an obvious local trade or community role, not look like an adventurer avatar. Do not depict a portrait or statue with a predefined likeness of the playable hero; show inscriptions or an unfinished plinth instead. Do not copy the shrine's healers into unrelated panels. Depict the event before its choice except explicit aftermath scenes. All visible writing must be abstract marks, not legible words. Each panel is a complete single scene, not a split scene.

SCENES, read left to right within each row:
'''

def write_atomic(path, contents):
    temporary = path.with_suffix(path.suffix + '.tmp')
    temporary.write_text(contents)
    temporary.replace(path)

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'prompts').mkdir(exist_ok=True)
    catalog = json.loads(CATALOG.read_text())
    quests = [line.split('|') for line in (ROOT/'Content/quests.tsv').read_text().splitlines() if line.strip() and not line.startswith('#')]
    cast = dict(line.split('|',1) for line in (OUT/'cast.tsv').read_text().splitlines() if line.strip())
    phase_cast = json.loads((OUT/'phase-cast.json').read_text())
    empty_offers = {15,20,38,50,51,52,53,54,58,60,61,66,67,68,70,75,77,83,87,91}
    solo_bargains = {13:'the mushroom gatherer alone, dictating toward the unseen viewer with paper and a quill resting unused on the table',15:'the village representative alone behind a desk, considering the forest contract',20:'the missing guide\'s apprentice alone beside maps and bone flutes',33:'the letter writer alone at a desk with the wet letter and fresh paper',88:'the demon and the mortal contract signer facing one another; no third negotiator'}
    plan = []
    for event in catalog['events']:
        eid = event['id']
        brief = BASE.get(eid)
        if brief is None:
            arc, phase = eid.split('_', 1)
            q = quests[int(arc[1:])-1]
            region, stage, kind, title, hook, clue, approach_a, result_a, approach_b, result_b, echo = q
            if phase == 'offer':
                content = f'Situation: {hook} Physical clue: {clue}'
            elif phase == 'trail':
                content = f'Situation: {hook} Focus clue: {clue} Investigation context: {approach_a}'
            elif phase == 'bargain':
                content = f'Situation: {hook} Local alternative being negotiated: {approach_b} Relevant clue: {clue}'
            elif phase == 'echo_a':
                content = f'Original situation: {hook} Now show this result: {result_a}'
            else:
                content = f'Original situation: {hook} Now show this result: {result_b}'
            brief = f'{PHASE[phase]} Location: {region}. {content}'
            number = int(arc[1:])
            if phase == 'trail' or phase == 'offer' and number in empty_offers:
                brief += ' VISIBLE CAST: NONE. NO people, bodies, hands, explorers or player silhouettes. Environmental/prop scene only.'
            elif eid in phase_cast:
                brief += f' VISIBLE CAST FOR THIS PHASE: {phase_cast[eid]} Do not add anyone else.'
            elif phase == 'bargain' and number in solo_bargains:
                brief += f' VISIBLE CAST: ONLY {solo_bargains[number]}. Do not add anyone else.'
            else:
                brief += f' Allowed LOCAL NPC roles: {cast[arc]}. Show only the roles needed in this exact phase. Never add an unassigned listener, visitor or foreground observer.'
        plan.append({'id':eid, 'title':event['title'], 'place':event.get('place',''), 'brief':brief, 'sourceText':event['text'], 'image':f'{eid}.png'})
    pending = [x for x in plan if x['id'] != 'shrine']
    for i, e in enumerate(pending):
        e.update(sheet=i//16+1, cell=i%16, row=i%16//4+1, column=i%4+1)
    for start in range(0,len(pending),16):
        group = pending[start:start+16]
        prompt = STYLE + '\n\n'.join(f"ROW {x['row']} COLUMN {x['column']}: {x['brief']}" for x in group)
        if len(group)<16:
            prompt += '\n\nUnused cells: plain dark slate, no scene. Only the assigned cells contain illustrations.'
        write_atomic(OUT/'prompts'/f'sheet-{start//16+1:02}.txt', prompt+'\n')
    write_atomic(OUT/'scenes.json', json.dumps({'width':192,'height':128,'maxColors':32,'approvedScene':'shrine','scenes':plan},ensure_ascii=False,indent=2)+'\n')
    repairs_path = OUT/'repairs.json'
    if repairs_path.exists():
        repairs = json.loads(repairs_path.read_text())
        (OUT/'repair-prompts').mkdir(exist_ok=True)
        for start in range(0, len(repairs), 16):
            group = repairs[start:start+16]
            prompt = STYLE + '\n\n'.join(f"ROW {i//4+1} COLUMN {i%4+1}: {scene['brief']}" for i, scene in enumerate(group))
            if len(group) < 16:
                prompt += '\n\nUnused cells: plain dark slate, no scene. Only the assigned cells contain illustrations.'
            write_atomic(OUT/'repair-prompts'/f'repair-{start//16+1:02}.txt', prompt+'\n')
    print(f'{len(plan)} scenes; {len(pending)} sheet-authored illustrations; {(len(pending)+15)//16} sheets')

if __name__ == '__main__': main()
