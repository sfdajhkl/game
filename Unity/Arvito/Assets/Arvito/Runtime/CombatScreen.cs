using System;
using System.Linq;
using Arvito.Core;
using TMPro;
using UnityEngine;

namespace Arvito.Runtime
{
    public sealed partial class GameApp
    {
        void CombatScreen()
        {
            var combat = game.State.combat; var active = game.Actor(combat.active);
            if (lastActor != active.id) { selectedSkill = selectedTarget = null; lastActor = active.id; }
            if (string.IsNullOrEmpty(selectedSkill) || game.SkillLock(selectedSkill) != "")
                selectedSkill = game.Skills(active).FirstOrDefault(id => data.Skill(id).power > 0 && game.SkillLock(id) == "") ?? "guard";
            if (!game.Targets(selectedSkill).Contains(selectedTarget)) selectedTarget = game.Targets(selectedSkill).FirstOrDefault();

            // Keep the battlefield and execute button visible while skills and details scroll independently.
            var arena = Ui.Node("Battlefield", main); Ui.Fill(arena); arena.anchorMin = new Vector2(0, 1); arena.offsetMin = new Vector2(0, -282);
            Ui.FixedText(arena, "ROUND " + combat.round.ToString("00") + "   /   " + game.Event.place, 0, 0, 700, 25, 14, Ui.Gold);
            var artPanel = Ui.Node("Battle panorama", arena); Ui.Fill(artPanel, 0, 0, 35, 125);
            var raw = artPanel.gameObject.AddComponent<UnityEngine.UI.RawImage>(); raw.texture = art["battle"]; raw.raycastTarget = false; artPanel.gameObject.AddComponent<ArtCrop>();
            Ui.FixedText(arena, "적의 진형   <size=12>전열 → 후열 · 대상을 클릭하세요</size>", 0, 166, 700, 24, 15, Ui.Gold);
            var targets = game.Targets(selectedSkill);
            var enemies = combat.enemies.Where(e => e.hp > 0).ToList();
            var enemyRow = Ui.Node("Enemy formation", arena); Ui.Fill(enemyRow, 0, 0, 197, 0);
            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i]; string id = enemy.id; bool legal = targets.Contains(id);
                string conditions = (enemy.status.bleed > 0 ? " · 출혈 " + enemy.status.bleed : "") + (enemy.status.stun > 0 ? " · 기절" : "");
                var button = Ui.Button(enemyRow, (i + 1) + "열  " + enemy.name + "\n<size=12>체력 " + enemy.hp + "/" + enemy.maxHp + " · 방어 " + enemy.armor + conditions + "</size>", () => { selectedTarget = id; Render(); }, 85, legal, selectedTarget == id);
                var rt = (RectTransform)button.transform; rt.anchorMin = new Vector2((float)i / enemies.Count, 0); rt.anchorMax = new Vector2((float)(i + 1) / enemies.Count, 1); rt.offsetMin = new Vector2(i == 0 ? 0 : 5, 0); rt.offsetMax = new Vector2(i == enemies.Count - 1 ? 0 : -5, 0);
            }
            var commandArea = Ui.Node("Commands", main); Ui.Fill(commandArea, 0, 0, 305);
            var turnLabel = Ui.Text(commandArea, Ui.Safe(active.name) + "의 차례   <size=14>" + game.Rank(active.id) + "열 · 기력 " + active.energy + "/5</size>", 23, Ui.Gold);
            Ui.Fill(turnLabel.rectTransform); turnLabel.rectTransform.anchorMin = new Vector2(0, 1); turnLabel.rectTransform.offsetMin = new Vector2(0, -36);
            var skillPanel = Ui.Node("Skills", commandArea); Ui.Fill(skillPanel, 0, 12, 50, 54); skillPanel.anchorMax = new Vector2(.46f, 1);
            var stack = Ui.Scroll(skillPanel, "Skills scroll"); stack.GetComponent<UnityEngine.UI.VerticalLayoutGroup>().spacing = 10;
            var skills = game.Skills(active);
            for (int index = 0; index < skills.Count; index++)
            {
                    string id = skills[index]; var sk = data.Skill(id); string locked = game.SkillLock(id);
                    Action select = () => { selectedSkill = id; selectedTarget = game.Targets(id).FirstOrDefault(); Render(); };
                    shortcuts.Add(locked == "" ? select : null);
                    string hint = locked != "" ? locked : sk.cost > 0 ? "기력 " + sk.cost + " 소모" : "기력 소모 없음";
                    Ui.Button(stack, (index + 1) + "  " + sk.name + "\n<size=12>" + hint + "</size>", select, 58 * Ui.TextScale, locked == "", selectedSkill == id);
            }
            var detailPanel = Ui.Card(commandArea, "Selected technique"); Ui.Fill(detailPanel, 12, 0, 50, 54); detailPanel.anchorMin = new Vector2(.46f, 0);
            var detailViewport = Ui.Node("Details", detailPanel); Ui.Fill(detailViewport, 15, 15, 15, 84);
            var details = Ui.Scroll(detailViewport, "Technique scroll");
            var chosen = data.Skill(selectedSkill);
            Ui.Text(details, chosen.name, 22, Ui.Gold);
            Ui.Text(details, chosen.desc + "\n<size=13>사용 위치 " + string.Join("·", chosen.from) + "열" + (chosen.power > 0 ? "   /   대상 " + string.Join("·", chosen.to) + "열\n명중 " + chosen.accuracy + "%" : "") + "</size>", 17, Ui.Muted);
            if (chosen.target == "ally" || chosen.target == "otherAlly" || chosen.target == "self")
            {
                foreach (string id in targets)
                {
                    var ally = game.Actor(id); string targetId = id;
                    Ui.Button(details, Ui.Safe(ally.name) + "   " + ally.hp + "/" + ally.maxHp + "   긴장 " + ally.stress, () => { selectedTarget = targetId; Render(); }, 45, true, selectedTarget == id);
                }
            }
            string targetName = game.Actor(selectedTarget)?.name ?? combat.enemies.FirstOrDefault(e => e.id == selectedTarget)?.name ?? "대상 선택";
            enterAction = () =>
            {
                string skill = selectedSkill, target = selectedTarget;
                Change(() => { if (game.Act(skill, target)) { selectedSkill = selectedTarget = lastActor = null; } });
            };
            var execute = Ui.Button(detailPanel, chosen.name + " → " + Ui.Safe(targetName) + "\n<size=12>Enter · 행동 확정</size>", enterAction, 60, game.SkillLock(selectedSkill) == "" && targets.Contains(selectedTarget), true);
            var er = (RectTransform)execute.transform; er.anchorMin = Vector2.zero; er.anchorMax = new Vector2(1, 0); er.offsetMin = new Vector2(15, 15); er.offsetMax = new Vector2(-15, 75);
            Ui.Text(details, "다가오는 차례\n" + string.Join(" → ", combat.queue.Take(5).Select(id => game.Actor(id)?.name ?? combat.enemies.FirstOrDefault(e => e.id == id && e.hp > 0)?.name).Where(n => n != null)), 13, Ui.Muted);
            Ui.Text(details, "최근 전투 기록", 16, Ui.Gold);
            Ui.Text(details, string.Join("\n", combat.log.Skip(Math.Max(0, combat.log.Count - 5))), 14);
            var retreat = Ui.Button(commandArea, "퇴각한다  ·  골드 20%와 긴장 +12", () =>
            {
                var d = Dialog("다음 장을 위해 물러서다", "전투 보상은 얻지 못합니다. 골드의 20%를 잃고 일행의 긴장이 12 오릅니다.\n\n지금까지 함께 살아남은 동료들은 당신과 함께합니다.");
                Ui.Button(d, "함께 퇴각한다", () => { CloseDialog(); Change(() => game.Flee()); }, 54, true, true);
                Ui.Button(d, "전투를 계속한다", CloseDialog);
            }, 48);
            var rr = (RectTransform)retreat.transform; rr.anchorMin = Vector2.zero; rr.anchorMax = new Vector2(1, 0); rr.offsetMin = Vector2.zero; rr.offsetMax = new Vector2(0, 42);
        }
    }
}
