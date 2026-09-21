using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2ChapterTwoBattlePresentation079PlayModeTests
    {
        private const string FogBattleId =
            "BATTLE_CONTRACT_LINES_NOT_RETURNED_ENCOUNTER_FOG_STALKERS_STANDARD_079";

        [UnityTest]
        public IEnumerator FogStalkerDioramaUsesWayglassPlateNewFamilyArtAndReadableThreatGrades089()
        {
            var host = Host("Chapter Two Battle Host 079");
            var owner = new GameObject("Chapter Two Diorama Owner 079");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var battle = FogBattle();
            diorama.Refresh(battle, "PU_01");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var backdrop = host.GetComponentsInChildren<Image>(true)
                .Single(value => value.name == "Authored Encounter Backdrop 072");
            Assert.That(backdrop.sprite, Is.Not.Null);
            Assert.That(backdrop.sprite.name, Is.EqualTo("WAYGLASS_UNDERCROFT_BATTLE_PLATE_079"));

            var scout = diorama.ResolveActor(
                "ENEMY_ECHO_STALKER_01_SPAWN070_EU_ECHO_AMBUSH_0", "EU_ECHO_AMBUSH");
            var veteran = diorama.ResolveActor(
                "ENEMY_ECHO_STALKER_02_SPAWN070_EU_ECHO_AMBUSH_1", "EU_ECHO_AMBUSH");
            var chaincaller = diorama.ResolveActor(
                "ENEMY_CHAINCALLER_01_SPAWN070_EU_ECHO_AMBUSH_2", "EU_ECHO_AMBUSH");
            Assert.That(scout, Is.Not.Null);
            Assert.That(veteran, Is.Not.Null);
            Assert.That(chaincaller, Is.Not.Null);
            Assert.That(scout.CurrentResourcePath,
                Does.EndWith("/ECHO_STALKER_IDLE_089"));
            Assert.That(veteran.CurrentResourcePath,
                Does.EndWith("/ECHO_STALKER_IDLE_089"));
            Assert.That(chaincaller.CurrentResourcePath,
                Does.EndWith("/CHAINCALLER_IDLE_089"));
            var paths = new[]
            {
                scout.CurrentResourcePath,
                veteran.CurrentResourcePath,
                chaincaller.CurrentResourcePath
            };
            Assert.That(paths.Distinct().Count(), Is.EqualTo(2),
                "Echo ranks share a coherent generated family silhouette while the " +
                "Chaincaller remains a distinct generated enemy design.");
            Assert.That(paths.All(path =>
                path.IndexOf("GATE_GNAWER", StringComparison.OrdinalIgnoreCase) < 0 &&
                path.IndexOf("Battle075", StringComparison.OrdinalIgnoreCase) < 0), Is.True);
            Assert.That(scout.EnemyThreatTier089, Is.EqualTo(2));
            Assert.That(veteran.EnemyThreatTier089, Is.EqualTo(3));
            Assert.That(chaincaller.EnemyThreatTier089, Is.EqualTo(1));
            Assert.That(scout.EnemyThreatTint089,
                Is.Not.EqualTo(veteran.EnemyThreatTint089),
                "Shared family art must remain visibly rank-readable through the " +
                "five-grade threat treatment.");

            var player = diorama.ResolveActor("SIGREC_ASTER_MARSHLIGHT", "PU_01");
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Enemy, Is.False);
            Assert.That(player.CurrentResourcePath,
                Does.Not.Contain("ChapterTwo079"),
                "Fog-Stalker routing must not alter the established player standee path.");

            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FogStalkerResultsAreSpecificReadableAndDoNotRenameAuthorityReward()
        {
            var host = Host("Chapter Two Results Host 079");
            var owner = new GameObject("Chapter Two Results Owner 079");
            var results = owner.AddComponent<M2BattleResultsView072>();
            results.Initialize(host, () => { });
            var battle = FogBattle();
            battle.Outcome = "Victory";
            battle.IsResolved = true;
            battle.Round = 4;
            battle.LastResolvedRound = 3;
            var authorityReward = new M2BattleRewardView
            {
                RewardId = "AUTHORITY_REWARD_FOG_079",
                EquipmentRewardDefinitionId = "LOOT020_SKYHOME_01",
                EquipmentRewardDisplayName = "First-Gate Sword",
                EquipmentRewardQualityId = "QUALITY_STANDARD",
                EquipmentRewardValidSlotIds = new[] { "MAIN_HAND" },
                GuildTreasuryXpAward = 400,
                HallEnhancementXpAward = 100,
                GuildPreviousLevel = 2,
                GuildProjectedLevel = 2
            };
            battle.Reward = authorityReward;
            results.Show(battle);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(results.OutcomeKickerText079, Does.Contain("WAYGLASS LINE SECURED"));
            Assert.That(results.OutcomeTitleText079, Is.EqualTo("THE FALSE LINE IS BROKEN"));
            Assert.That(results.BattleSummaryText079,
                Does.Contain("FOG-STALKERS DOWN")
                    .And.Contain("ORRA'S LINE HOLDS")
                    .And.Not.Contain("...")
                    .And.Not.Contain("…"));
            Assert.That(results.StoryRewardHeadingText079, Is.EqualTo("WAYGLASS SIGNAL RESTORED"));
            Assert.That(results.LootHeadingText079, Is.EqualTo(
                "WAYGLASS RECOVERY  •  ECHO FILAMENT"));
            Assert.That(results.LootNameText079,
                Is.EqualTo(authorityReward.EquipmentRewardDisplayName),
                "The results item name must exactly match the item the player will receive.");
            Assert.That(results.NextObjectiveText079,
                Does.Contain("Sella").And.Contain("Orra"));

            var visibleCopy = string.Join("\n", host.GetComponentsInChildren<Text>(true)
                .Select(value => value.text));
            Assert.That(visibleCopy,
                Does.Not.Contain("CONTRACT SPOILS")
                    .And.Not.Contain("CONTRACT OBJECTIVE CLEARED"));
            var summary = host.GetComponentsInChildren<Text>(true)
                .Single(value => value.name == "Outcome Summary 072");
            Assert.That(summary.preferredWidth, Is.LessThanOrEqualTo(summary.rectTransform.rect.width + 1f));
            Assert.That(summary.preferredHeight, Is.LessThanOrEqualTo(summary.rectTransform.rect.height + 1f));

            Assert.That(battle.Reward, Is.SameAs(authorityReward));
            Assert.That(authorityReward.EquipmentRewardDefinitionId, Is.EqualTo("LOOT020_SKYHOME_01"));
            Assert.That(authorityReward.EquipmentRewardDisplayName, Is.EqualTo("First-Gate Sword"),
                "Encounter dressing is presentation-only; authoritative reward state must remain untouched.");

            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GateEaterPreRoundStagesBossNameArtAndScaleBeforeForecastSelection()
        {
            var host = Host("Gate Eater Opening Host 079");
            var owner = new GameObject("Gate Eater Diorama Owner 079");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var battle = GateEaterBattle();
            diorama.Refresh(battle, "PU_01");
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(diorama.TargetEnemyUnionId, Is.EqualTo("EU_GATE_EATER"));
            var boss = diorama.ResolveActor("HINGE_EATER_COLOSSUS", "EU_GATE_EATER");
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.BossEnemy075, Is.True);
            Assert.That(boss.DisplayName, Is.EqualTo(M2BattleActorRig072.GateEaterDisplayName076));
            Assert.That(boss.NameLabel076.text,
                Is.EqualTo(M2BattleActorRig072.GateEaterDisplayName076 +
                           "  •  THREAT X"),
                "The signature boss label must retain its name and expose the top threat grade.");
            Assert.That(boss.CurrentResourcePath,
                Is.EqualTo(M2BattleActorRig072.GateEaterIdleResourcePath076));
            Assert.That(boss.HomeScale.x, Is.GreaterThan(1.5f),
                "The opening focus must retain the authored signature boss scale.");
            Assert.That(diorama.ResolveActor("ENEMY_GATE_GNAWER_01", "EU_ESCORT"), Is.Null,
                "The escort Union must not displace the boss on the initial narrative stage.");

            battle.Forecasts = new[]
            {
                new M2ForecastView
                {
                    ForecastId = "F_SELECTED_ESCORT",
                    UnionId = "PU_01",
                    TargetId = "EU_ESCORT",
                    IsSelected = true
                }
            };
            diorama.Refresh(battle, "PU_01");
            yield return null;
            Assert.That(diorama.TargetEnemyUnionId, Is.EqualTo("EU_ESCORT"),
                "A selected player forecast must continue to override the opening boss focus.");

            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
        }

        private static RectTransform Host(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1280f, 800f);
            return rect;
        }

        private static M2BattleView FogBattle() => new M2BattleView
        {
            BattleId = FogBattleId,
            Objective = "Keep Sella alive and hold Orra's true Wayglass line against the Fog Stalkers.",
            PlayerUnions = new[]
            {
                Union("PU_01", new M2BattleMemberView
                {
                    MemberId = "SIGREC_ASTER_MARSHLIGHT",
                    DisplayName = "Aster Marshlight",
                    PortraitAuthorityId = "SIGREC_ASTER_MARSHLIGHT",
                    RaceId = "HUMAN",
                    CurrentHp = 240,
                    MaximumHp = 240
                })
            },
            EnemyUnions = new[]
            {
                Union("EU_ECHO_AMBUSH",
                    Enemy("ENEMY_ECHO_STALKER_01_SPAWN070_EU_ECHO_AMBUSH_0", "Echo Stalker — Scout"),
                    Enemy("ENEMY_ECHO_STALKER_02_SPAWN070_EU_ECHO_AMBUSH_1", "Echo Stalker — Veteran"),
                    Enemy("ENEMY_CHAINCALLER_01_SPAWN070_EU_ECHO_AMBUSH_2", "Chaincaller"))
            },
            Forecasts = Array.Empty<M2ForecastView>()
        };

        private static M2BattleView GateEaterBattle() => new M2BattleView
        {
            BattleId = "BATTLE_FIRST_HOUR_ENCOUNTER071_GATE_EATER_079",
            Objective = "Defeat the Gate-Eater and protect the road home.",
            PlayerUnions = new[]
            {
                Union("PU_01", Enemy("SIGREC_ASTER_MARSHLIGHT", "Aster Marshlight"))
            },
            EnemyUnions = new[]
            {
                Union("EU_ESCORT", Enemy("ENEMY_GATE_GNAWER_01", "Gate Gnawer Escort")),
                Union("EU_GATE_EATER", Enemy("HINGE_EATER_COLOSSUS", "Hinge-Eater Colossus"))
            },
            Forecasts = Array.Empty<M2ForecastView>()
        };

        private static M2BattleUnionView Union(
            string unionId,
            params M2BattleMemberView[] members) => new M2BattleUnionView
        {
            UnionId = unionId,
            DisplayName = unionId,
            CanAct = true,
            Members = members
        };

        private static M2BattleMemberView Enemy(string memberId, string displayName) =>
            new M2BattleMemberView
            {
                MemberId = memberId,
                DisplayName = displayName,
                CurrentHp = 300,
                MaximumHp = 300
            };
    }
}
