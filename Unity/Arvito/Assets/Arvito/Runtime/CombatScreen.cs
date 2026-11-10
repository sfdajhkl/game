using System;
using System.Linq;
using Arvito.Core;
using TMPro;
using UnityEngine;

namespace Arvito.Runtime
{
    public sealed partial class GameApp
    {
        static RectTransform BattlePanel(Transform parent, string name, float x0, float y0, float x1, float y1, Color? color = null)
        {
            var rt = color.HasValue ? Ui.Card(parent,name,color) : Ui.Node(name,parent);
            rt.anchorMin=new Vector2(x0,y0); rt.anchorMax=new Vector2(x1,y1); rt.offsetMin=rt.offsetMax=Vector2.zero; return rt;
        }
        void CombatScreen()
        {
            var combat=game.State.combat; var active=game.State.hero;
            if (lastActor!=active.id) { selectedSkill=selectedTarget=null; lastActor=active.id; }
            if (string.IsNullOrEmpty(selectedSkill)||game.SkillLock(selectedSkill)!="")
                selectedSkill=game.Skills(active).FirstOrDefault(id=>data.Skill(id).power>0&&game.SkillLock(id)=="")??"guard";
            if (!game.Targets(selectedSkill).Contains(selectedTarget)) selectedTarget=game.Targets(selectedSkill).FirstOrDefault();
            var targets=game.Targets(selectedSkill); var chosen=data.Skill(selectedSkill);

            // Front-view battlefield: enemy battlers share a baseline above the party HUD.
            var arena=BattlePanel(main,"Front-view battlefield",0,.46f,1,1);
            var backdrop=arena.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            backdrop.texture=art[game.Event.art??"battle"]; backdrop.raycastTarget=false; arena.gameObject.AddComponent<ArtCrop>();
            var shade=Ui.Card(arena,"Battle shade",new Color(.02f,.04f,.05f,.35f)); Ui.Fill(shade); shade.GetComponent<UnityEngine.UI.Image>().raycastTarget=false;
            Ui.FixedText(arena,"ROUND "+combat.round.ToString("00")+"    /    "+game.RegionName+"    /    "+game.Event.title,20,12,1250,32,19,Ui.Paper);
            var enemies=combat.enemies.Where(e=>e.hp>0).ToList();
            var row=BattlePanel(arena,"Enemy battlers",.06f,.04f,.94f,.88f);
            for(int i=0;i<enemies.Count;i++)
            {
                var enemy=enemies[i]; string id=enemy.id; bool legal=targets.Contains(id), selected=id==selectedTarget;
                var slot=BattlePanel(row,enemy.name,(float)i/enemies.Count+.008f,0,(float)(i+1)/enemies.Count-.008f,1);
                var imageHost=BattlePanel(slot,"Battler image frame",.08f,.24f,.92f,.99f);
                var imageArea=Ui.Node("Battler image",imageHost);Ui.Fill(imageArea);
                var enemyImage=imageArea.gameObject.AddComponent<UnityEngine.UI.RawImage>(); enemyImage.texture=BattlerArt.Get(enemy.portrait); enemyImage.raycastTarget=false;
                // A square rect preserves pixel proportions at every window aspect ratio.
                var fit=imageArea.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>(); fit.aspectMode=UnityEngine.UI.AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio=1;
                var plate=Ui.Button(slot,(selected?"▶ ":"")+enemy.name+"\n<size=13>체력 "+enemy.hp+" / "+enemy.maxHp+
                    (enemy.status.bleed>0?"   출혈":"")+(enemy.status.stun>0?"   기절":"")+"</size>",()=>{selectedTarget=id;Render();},58,legal,selected);
                var pr=(RectTransform)plate.transform; pr.anchorMin=new Vector2(.04f,0);pr.anchorMax=new Vector2(.96f,.23f);pr.offsetMin=pr.offsetMax=Vector2.zero;
                // The entire visible battler is clickable; ally-target skills disable enemy selection.
                var hit=slot.gameObject.AddComponent<UnityEngine.UI.Image>();hit.color=new Color(1,1,1,.002f);
                var button=slot.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=hit;button.interactable=legal;button.transition=UnityEngine.UI.Selectable.Transition.None;
                button.onClick.AddListener(()=>{selectedTarget=id;Render();});
            }

            var party=BattlePanel(main,"Party status",0,.32f,1,.44f);
            var members=game.Party;
            for(int i=0;i<members.Count;i++)
            {
                var a=members[i];var card=BattlePanel(party,a.id,(float)i/members.Count+.004f,0,(float)(i+1)/members.Count-.004f,1,Ui.Panel);Ui.Border(card,a.id=="hero"?Ui.Gold:Ui.Line);
                var name=Ui.Text(card,Ui.Safe(a.name)+"   <size=12>"+(a.id=="hero"?"직접 조작":"자동 행동")+"</size>",19,a.id=="hero"?Ui.Gold:Ui.Paper);
                name.textWrappingMode=TextWrappingModes.NoWrap;name.enableAutoSizing=true;name.fontSizeMin=12;name.fontSizeMax=19*Ui.TextScale;
                Ui.Fill(name.rectTransform,14,10,8,50);
                var status=Ui.Text(card,"체력 "+a.hp+" / "+a.maxHp+"     기력 "+a.energy+" / 5\n<size=12>긴장 "+a.stress+"   "+game.Rank(a.id)+"열</size>",15,Ui.Paper);
                Ui.Fill(status.rectTransform,14,10,38,6);
                var track=BattlePanel(card,"HP track",.04f,.035f,.96f,.075f,Ui.Line);
                BattlePanel(track,"HP",0,0,(float)a.hp/a.maxHp,1,a.hp<a.maxHp*.3f?Ui.Red:Ui.Green);
            }
            var commands=BattlePanel(main,"Battle commands",0,0,.25f,.30f,Ui.Panel);Ui.Border(commands);
            var skillHost=Ui.Node("Technique list",commands);Ui.Fill(skillHost,12,12,36,12);
            Ui.FixedText(commands,"명령  ·  "+Ui.Safe(active.name),14,4,300,28,19,Ui.Gold);
            var list=Ui.Scroll(skillHost,"Technique scroll");list.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing=7;
            var skills=game.Skills(active);
            for(int i=0;i<skills.Count;i++)
            {
                string id=skills[i];var skill=data.Skill(id);string locked=game.SkillLock(id);
                Action select=()=>{selectedSkill=id;selectedTarget=game.Targets(id).FirstOrDefault();Render();};
                shortcuts.Add(locked==""?select:null);
                Ui.Button(list,(i+1)+"  "+skill.name+"   <size=12>"+(locked!=""?locked:skill.cost>0?"기력 "+skill.cost:"")+"</size>",select,43*Ui.TextScale,locked=="",selectedSkill==id);
            }
            var detail=BattlePanel(main,"Command details",.265f,0,.555f,.30f,Ui.Panel);Ui.Border(detail);
            var detailHost=Ui.Node("Detail viewport",detail);Ui.Fill(detailHost,14,14,10,66);
            var info=Ui.Scroll(detailHost,"Detail scroll");info.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing=8;
            Ui.Text(info,chosen.name,21,Ui.Gold);Ui.Text(info,chosen.desc,16);
            Ui.Text(info,"사용 위치 "+string.Join("·",chosen.from)+"열"+(chosen.power>0?"  /  명중 "+chosen.accuracy+"%":""),12,Ui.Muted);
            if(chosen.target=="ally"||chosen.target=="otherAlly"||chosen.target=="self")
                foreach(string id in targets)
                { var ally=game.Actor(id);string target=id;Ui.Button(info,Ui.Safe(ally.name)+"  "+ally.hp+"/"+ally.maxHp,()=>{selectedTarget=target;Render();},38,true,selectedTarget==id); }
            string targetName=game.Actor(selectedTarget)?.name??combat.enemies.FirstOrDefault(e=>e.id==selectedTarget)?.name??"대상 선택";
            enterAction=()=>{string skill=selectedSkill,target=selectedTarget;Change(()=>{if(game.Act(skill,target))selectedSkill=selectedTarget=lastActor=null;});};
            var execute=Ui.Button(detail,chosen.name+" → "+Ui.Safe(targetName)+"\n<size=12>Enter · 행동 확정</size>",enterAction,54,game.SkillLock(selectedSkill)==""&&targets.Contains(selectedTarget),true);
            var er=(RectTransform)execute.transform;er.anchorMin=Vector2.zero;er.anchorMax=new Vector2(1,0);er.offsetMin=new Vector2(12,10);er.offsetMax=new Vector2(-12,62);

            var logPanel=BattlePanel(main,"Battle messages",.57f,0,1,.30f,Ui.Panel);Ui.Border(logPanel);
            var logHost=Ui.Node("Message viewport",logPanel);Ui.Fill(logHost,16,16,10,52);
            var log=Ui.Scroll(logHost,"Battle log scroll");log.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing=7;
            Ui.Text(log,"전투 기록  <size=12>동료는 스스로 판단합니다</size>",18,Ui.Gold);
            Ui.Text(log,string.Join("\n",combat.log.Skip(Math.Max(0,combat.log.Count-5))),15);
            var retreat=Ui.Button(logPanel,"퇴각  ·  골드 20% / 긴장 +12",()=>{
                var d=Dialog("살아남아 다음 길로","전투 보상을 포기하고 골드 20%를 잃습니다. 살아 있는 일행은 함께 퇴각합니다.");
                Ui.Button(d,"함께 물러난다",()=>{CloseDialog();Change(()=>game.Flee());},50,true,true);Ui.Button(d,"계속 싸운다",CloseDialog);
            },36);
            var rr=(RectTransform)retreat.transform;rr.anchorMin=Vector2.zero;rr.anchorMax=new Vector2(1,0);rr.offsetMin=new Vector2(12,8);rr.offsetMax=new Vector2(-12,44);
        }
    }
}
