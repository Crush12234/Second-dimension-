using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleAutoOrders091Tests
    {
        private M2CombatContent _content;
        private readonly M2BattleCommandService _commands = new M2BattleCommandService();
        private readonly List<string> _saveDirectories = new List<string>();

        [OneTimeSetUp]
        public void LoadAuthority() => _content = M2CombatContent.LoadFromDirectory(ContentRoot);
        private static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");

        [TearDown]
        public void Cleanup()
        {
            foreach (var path in _saveDirectories)
            {
                var resolved = Path.GetFullPath(path);
                var root = Path.GetDirectoryName(resolved);
                var ordinaryRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimensionAuto091"));
                var earnedRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimensionAuto134"));
                Assert.That(StringComparer.OrdinalIgnoreCase.Equals(root, ordinaryRoot) ||
                    StringComparer.OrdinalIgnoreCase.Equals(root, earnedRoot), Is.True);
                Assert.That(Guid.TryParseExact(Path.GetFileName(resolved), "N", out _), Is.True);
                if (Directory.Exists(resolved))
                {
                    Assert.That(File.GetAttributes(root) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Assert.That(File.GetAttributes(resolved) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Directory.Delete(resolved, true);
                }
            }
            _saveDirectories.Clear();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AutoChoosesNaturalRevivalForecastAndCoordinatorReallyRevivesSelfOrOtherUnion(bool crossUnion)
        {
            var campaign = Prepared(true, crossUnion);
            var coordinator = Coordinator(campaign);
            var view = coordinator.State.Battle;
            var healer = view.PlayerUnions.Single(union => union.Members.Any(member => member.MemberId == "AUTO_HERO_3"));
            var choice = M2BattleAutoOrders091.Choose(view, healer.UnionId);
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.CommandId, Is.EqualTo("CMD_HEAL"));
            var revive = choice.MemberActions.Single(action => action.IsRevival091);
            Assert.That(revive.ArtId, Is.EqualTo("ART_STAND_AGAIN"));
            Assert.That(revive.TargetUnionId == healer.UnionId, Is.EqualTo(!crossUnion));
            Assert.That(revive.TargetMemberId, Is.EqualTo(crossUnion ? "AUTO_HERO_0" : "AUTO_HERO_4"));
            Assert.That(healer.Members.Single(member => member.MemberId == "AUTO_HERO_3").CurrentHp,
                Is.GreaterThan(600), "A healthy acting healer must prioritize another member's revival.");

            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            Assert.That(coordinator.State.Battle.PlayerUnions.Where(union => union.CanAct)
                .All(union => !string.IsNullOrWhiteSpace(union.SelectedForecastId)), Is.True);
            var result = coordinator.ConfirmBattleRound();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(coordinator.State.Battle.LastResolvedRoundEvents.Any(item => item.EventType == "REVIVED" &&
                item.ArtId == "ART_STAND_AGAIN" && item.TargetMemberId == revive.TargetMemberId), Is.True,
                "Selecting Auto's complete Forecast must execute real legal revival, not only label a preview.");
        }

        [Test]
        public void AutoWithoutLearnedRevivalDoesNotInventItAndStillCommitsLegalOrders()
        {
            var coordinator = Coordinator(Prepared(false, true));
            Assert.That(coordinator.State.Battle.Forecasts.SelectMany(value => value.MemberActions)
                .Any(action => action.IsRevival091), Is.False);
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            var result = coordinator.ConfirmBattleRound();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(coordinator.State.Battle.LastResolvedRoundEvents.Any(item => item.EventType == "REVIVED"), Is.False);
        }

        [Test]
        public void AutoCommitsAllUnionOrdersThroughOneAtomicCoordinatorChange108()
        {
            var coordinator = Coordinator(Prepared(true, true));
            var changes = 0;
            coordinator.Changed += () => changes++;

            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure),
                Is.True, failure);

            Assert.That(changes, Is.EqualTo(1),
                "A complete Auto plan must write and publish once, not once per Union.");
            Assert.That(coordinator.State.Battle.PlayerUnions.Where(union => union.CanAct)
                .All(union => !string.IsNullOrWhiteSpace(union.SelectedForecastId)), Is.True);
        }

        [Test]
        public void AutoResolvesWholeRoundThroughOneAtomicCoordinatorChange108()
        {
            var coordinator = Coordinator(Prepared(true, true));
            var before = M2BattleViewAccess098.Read(coordinator);
            var changes = 0;
            coordinator.Changed += () => changes++;

            Assert.That(M2BattleAutoOrders091.SelectAndResolveCompletePlan108(
                coordinator, before, out var committed, out var resolved, out var failure),
                Is.True, failure);

            Assert.That(changes, Is.EqualTo(1),
                "Auto selection and resolution must use one authoritative save.");
            Assert.That(committed.PlayerUnions.Where(union => union.CanAct)
                .All(union => !string.IsNullOrWhiteSpace(union.SelectedForecastId)), Is.True);
            Assert.That(resolved.Round, Is.GreaterThan(before.Round));
            Assert.That(resolved.LastResolvedRoundEvents, Is.Not.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AutoActuallyRestoresCriticalLivingMemberInSelfOrOtherAssignedUnion107(bool crossUnion)
        {
            var coordinator = Coordinator(Prepared(false, crossUnion, 180));
            var before = coordinator.State.Battle;
            var healer = before.PlayerUnions.Single(union => union.Members.Any(member => member.MemberId == "AUTO_HERO_3"));
            var choice = M2BattleAutoOrders091.Choose(before, healer.UnionId);
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.CommandId, Is.EqualTo("CMD_HEAL"));
            var healing = choice.MemberActions.Single(action => action.ActionKind == "Restoration" && action.PredictedHpDelta097 > 0);
            Assert.That(healing.IsRevival091, Is.False);
            Assert.That(healing.TargetUnionId == healer.UnionId, Is.EqualTo(!crossUnion));
            Assert.That(healing.TargetMemberId, Is.EqualTo(crossUnion ? "AUTO_HERO_0" : "AUTO_HERO_4"));
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out var failure), Is.True, failure);
            var result = coordinator.ConfirmBattleRound();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(coordinator.State.Battle.LastResolvedRoundEvents.Any(item => item.EventType == "RESTORATION" &&
                item.ArtId == healing.ArtId && item.TargetMemberId == healing.TargetMemberId && item.Amount > 0), Is.True,
                "Auto's exact learned-Art Forecast must restore actual allied HP through the shipping coordinator.");
        }

        [Test]
        public void AutoPrefersCriticalAlliedHealingOverAttacksButNotMinorScratches()
        {
            var battle = SimpleView();
            var ally = battle.PlayerUnions[1].Members[0];
            ally.CurrentHp = 5;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("HEAL"));
            ally.CurrentHp = 99;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("ATTACK"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FinalBreachClockPrefersLegalDamageOverHealingOrRevival134(bool revival)
        {
            var battle = SimpleView();
            battle.Round = 4;
            var ally = battle.PlayerUnions[1].Members[0];
            ally.CurrentHp = revival ? 0 : 5;
            ally.Downed = revival;
            battle.Forecasts[1].MemberActions[0].IsRevival091 = revival;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId,
                Is.EqualTo("HEAL"), "Ordinary rescue priority must remain unchanged.");
            battle.LastResolvedRoundEvents = new[] { new M2BattleEventView
                { EventType = "BOSS_ADVANCE_CLOCK", Round = 3, Amount = 1 } };
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId,
                Is.EqualTo("ATTACK"), "Healing cannot postpone the authoritative breach deadline.");
            battle.Forecasts[0].SharedApCost = 999;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId,
                Is.EqualTo("HEAL"), "The deadline must never authorize an unaffordable order.");
        }

        [Test]
        public void StaleOrNonfinalBreachClockDoesNotDisableRescue134()
        {
            var battle = SimpleView();
            battle.Round = 3;
            battle.Events = new[] { new M2BattleEventView
                { EventType = "BOSS_ADVANCE_CLOCK", Round = 2, Amount = 2 } };
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("HEAL"));
            battle.Events[0].Amount = 1;
            battle.Events[0].Round = 1;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("HEAL"));
        }

        [Test, Timeout(120000)]
        public void EarnedFinalBreachRoundResolvesThroughRealAutoAuthority134()
        {
            var source = Environment.GetEnvironmentVariable("SD_BOSS134_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_BOSS134_SOURCE to the preserved earned pre-deadline save.");
            var sourceBytes = File.ReadAllBytes(source);
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionAuto134", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _saveDirectories.Add(directory);
            var save = Path.Combine(directory, "earned.json");
            File.WriteAllBytes(save, sourceBytes);
            var coordinator = new M1RuntimeCoordinator(ContentRoot, save);
            var before = M2BattleViewAccess098.Read(coordinator);
            Assert.That(before.Round, Is.EqualTo(M2BattleCommandService.GateEaterDeadlineRounds076));
            Assert.That(before.IsResolved, Is.False);
            Assert.That(before.EnemyUnions.SelectMany(u => u.Members).Sum(m => m.CurrentHp), Is.EqualTo(447));
            Assert.That(M2BattleAutoOrders091.SelectAndResolveCompletePlan108(
                coordinator, before, out _, out var resolved, out var failure), Is.True, failure);
            Assert.That(resolved.Outcome, Is.EqualTo("Victory"));
            Assert.That(resolved.LastResolvedRoundEvents.Any(e => e.EventType == "BOSS_BREACH"), Is.False);
            var reloaded = new M1RuntimeCoordinator(ContentRoot, save);
            Assert.That(M2BattleViewAccess098.Read(reloaded).Outcome, Is.EqualTo("Victory"));
            Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBytes));
        }

        [Test]
        public void AutoRefusesUnaffordableRevivalAndMissingOrdersWithoutPartialCommit()
        {
            var battle = SimpleView();
            battle.PlayerUnions[1].Members[0].CurrentHp = 0;
            battle.PlayerUnions[1].Members[0].Downed = true;
            var revive = battle.Forecasts[1];
            revive.MemberActions[0].IsRevival091 = true;
            revive.SharedApCost = 999;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("ATTACK"));
            revive.SharedApCost = 6;
            revive.MemberActions[0].PersonalMpCost = 999;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS").ForecastId, Is.EqualTo("ATTACK"));
            battle.Forecasts = Array.Empty<M2ForecastView>();
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS"), Is.Null);

            var campaign = Prepared(true, true);
            campaign = campaign.WithBattle(campaign.Battle.With(committedForecasts: Array.Empty<BattleForecastState>()));
            var coordinator = Coordinator(campaign);
            Assert.That(M2BattleAutoOrders091.SelectCompletePlan(coordinator, out _), Is.False);
            Assert.That(coordinator.State.Battle.PlayerUnions.All(union => !union.IsSelected), Is.True);
        }

        [Test]
        public void AutoStopsForTerminalOutcomeOrNoLivingOpponentsButNotUnavailableRange()
        {
            var battle = SimpleView();
            battle.IsResolved = true;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS"), Is.Null);
            battle.IsResolved = false;
            battle.EnemyUnions[0].Members[0].Downed = true;
            battle.EnemyUnions[0].Members[0].CurrentHp = 0;
            Assert.That(M2BattleAutoOrders091.Choose(battle, "HEALERS"), Is.Null);
            battle.EnemyUnions[0].Members[0].Downed = false;
            battle.EnemyUnions[0].Members[0].CurrentHp = 100;
            battle.EnemyUnions[0].CanAct = false;
            Assert.That(M2BattleAutoOrders091.HasLivingOpposition(battle), Is.True,
                "Enemy inactivity or lack of a targetable attack must not be treated as victory.");
        }

        [Test]
        public void ActualAutoButtonDefaultsOffAndDisablingDuringPlaybackDoesNotCancelCommittedRound()
        {
            var root = new GameObject("Auto Controller Test 091", typeof(RectTransform));
            try
            {
                var controller = root.AddComponent<M2BattleExperienceController072>();
                Assert.That(controller.AutoOrdersEnabled091, Is.False);
                var coordinator = Coordinator(Prepared(true, true));
                Field("_coordinator").SetValue(controller, coordinator);
                Field("_root").SetValue(controller, root.GetComponent<RectTransform>());
                var button = (Button)typeof(M2BattleExperienceController072).GetMethod("BuildAutoOrdersControl091",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { root.transform });
                button.onClick.Invoke();
                Assert.That(controller.AutoOrdersEnabled091, Is.True);
                Assert.That(button.GetComponentInChildren<Text>().text, Is.EqualTo("AUTO  ON"));
                Field("_resolving").SetValue(controller, true);
                Field("_nextAutoOrderAt091").SetValue(controller, -1f);
                typeof(M2BattleExperienceController072).GetMethod("Update",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                Assert.That(controller.AutoOrdersEnabled091, Is.True);
                Assert.That(coordinator.State.Battle.PlayerUnions.All(union => !union.IsSelected), Is.True,
                    "Auto must not queue the next round while committed action presentation is playing.");
                button.onClick.Invoke();
                Assert.That(controller.AutoOrdersEnabled091, Is.False);
                Assert.That(controller.IsResolving, Is.True, "OFF stops future rounds, not the committed choreography.");
                typeof(M2BattleExperienceController072).GetMethod("Update",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
                Assert.That(controller.IsResolving, Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static FieldInfo Field(string name) => typeof(M2BattleExperienceController072)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);

        private M1RuntimeCoordinator Coordinator(CampaignState campaign)
        {
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimensionAuto091", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            _saveDirectories.Add(directory);
            var coordinator = new M1RuntimeCoordinator(ContentRoot, Path.Combine(directory, "test.json"));
            typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(coordinator, campaign);
            return coordinator;
        }

        private CampaignState Prepared(bool revival, bool crossUnion, int targetHp107 = 0)
        {
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 6; index++)
            {
                var healer = index == 3;
                var gear = new EquipmentItemState("AUTO_GEAR_" + index, "AUTO_GEAR", "Test weapon",
                    new[] { EquipmentSlotIds.MainHand }, healer ? new[] { "STAFF", "HEALING" } : new[] { "SWORD", "WEAPON" },
                    "QUALITY_STANDARD", 10000, false);
                var recruit = new RecruitState("AUTO_HERO_" + index, 800, 800, 100, 100, "Auto Hero " + index,
                    RecruitOriginKind.Procedural, string.Empty, "HUMAN", "WORLD_GATE_01",
                    healer ? "CLASS_TEND_PRIEST" : "CLASS_TEND_WARRIOR", "Observed", 6000,
                    RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, gear) }),
                    true, string.Empty, string.Empty, 50, 50);
                if (healer && revival) recruit = recruit.WithProgression(recruit.Progression.WithArts(
                    recruit.Progression.LearnedArtIds.Concat(new[] { "ART_STAND_AGAIN" }).ToArray(), recruit.Progression.ArtMastery));
                recruits.Add(recruit);
            }
            var unions = Enumerable.Range(0, 2).Select(index => new UnionState("AUTO_UNION_" + index,
                "Auto Union " + index, UnionKind.Normal, recruits[index * 3].RecruitId,
                recruits.Skip(index * 3).Take(3).Select(value => value.RecruitId).ToArray(),
                "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 30, 8500)).ToArray();
            var campaign = new CampaignState("00000000-0000-0000-0000-000000000491", 20260907L, "1.0",
                ModeRuleSnapshot.StandardDefaults(), new GuildState("AUTO_TEST", 0, recruits, unions),
                new NewGuildProfileState("Auto Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null,
                    false, 439, 0, true, true, true, false, "autosave_unions"));
            campaign = Require(_commands.StartEncounterBattle(campaign, _content, "BATTLE_AUTO_091", "Rescue fallen allies.", 1));
            var targetId = crossUnion ? "AUTO_HERO_0" : "AUTO_HERO_4";
            campaign = campaign.WithBattle(campaign.Battle.With(playerUnions: campaign.Battle.PlayerUnions.Select(union =>
                union.With(members: union.Members.Select(member => member.MemberId == targetId
                    ? member.With(currentHp: targetHp107, stabilized: false) : member).ToArray())).ToArray()));
            foreach (var union in campaign.Battle.PlayerUnions)
            {
                var guard = campaign.Battle.CommittedForecasts.Single(value => value.UnionId == union.UnionId && value.CommandId == "CMD_GUARD");
                campaign = Require(_commands.SelectForecast(campaign, union.UnionId, guard.ForecastId));
            }
            return Require(_commands.ConfirmRound(campaign, _content));
        }

        private static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static M2BattleView SimpleView()
        {
            var healer = new M2BattleUnionView { UnionId = "HEALERS", CanAct = true, CurrentAp = 30,
                Members = new[] { new M2BattleMemberView { MemberId = "MEDIC", CurrentHp = 100, MaximumHp = 100, CurrentMp = 100 } } };
            var ally = new M2BattleUnionView { UnionId = "ALLY", CanAct = true,
                Members = new[] { new M2BattleMemberView { MemberId = "ALLY_MEMBER", CurrentHp = 5, MaximumHp = 100 } } };
            var enemy = new M2BattleUnionView { UnionId = "ENEMY", CanAct = true,
                Members = new[] { new M2BattleMemberView { MemberId = "FOE", CurrentHp = 100, MaximumHp = 100 } } };
            return new M2BattleView { BattleId = "AUTO_VIEW", PlayerUnions = new[] { healer, ally }, EnemyUnions = new[] { enemy },
                Forecasts = new[] {
                    new M2ForecastView { ForecastId = "ATTACK", UnionId = "HEALERS", CommandId = "CMD_BALANCED", SharedApCost = 1,
                        MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "MEDIC", ActionKind = "Martial", TargetUnionId = "ENEMY",
                            TargetMemberId = "FOE", PredictedHpDelta097 = -30 } } },
                    new M2ForecastView { ForecastId = "HEAL", UnionId = "HEALERS", CommandId = "CMD_HEAL", SharedApCost = 6,
                        MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "MEDIC", ActionKind = "Restoration", TargetUnionId = "ALLY",
                            TargetMemberId = "ALLY_MEMBER", PredictedHpDelta097 = 40, PersonalMpCost = 12 } } }
                } };
        }
    }
}
