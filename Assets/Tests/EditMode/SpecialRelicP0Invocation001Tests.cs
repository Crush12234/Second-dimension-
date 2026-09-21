using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.SpecialRelic001;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SpecialRelicP0Invocation001Tests
    {
        private const string SourceRecruitId = "SPECIAL_INV_RECRUIT_A";
        private const string AllyRecruitId = "SPECIAL_INV_RECRUIT_B";
        private const string ExtraRecruitId = "SPECIAL_INV_RECRUIT_C";
        private const string SourceUnionId = "SPECIAL_INV_UNION_A";
        private const string AllyUnionId = "SPECIAL_INV_UNION_B";
        private const string ExtraUnionId = "SPECIAL_INV_UNION_C";
        private const string EnemyUnionId = "SPECIAL_INV_ENEMY";

        private CampaignRegistry022 _registry;
        private SpecialRelicRegistry001 _catalog;
        private CampaignProgressionCommandService022 _progression;
        private M2BattleCommandService _battles;
        private M2CombatContent _content;

        private static readonly object[] InvocationCases =
        {
            new object[]
            {
                "RELIC001_INV_001", "Aegis Hound Invocation Relic", "Aegis Hound",
                "INVOCATION_BASE022_07_01", "WTRACK022_07", "GUARD", 8, 14,
                "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard"
            },
            new object[]
            {
                "RELIC001_INV_002", "Ember Roc Invocation Relic", "Ember Roc",
                "INVOCATION_BASE022_09_01", "WTRACK022_09", "MYSTIC", 8, 14,
                "CMD_MYSTIC", "ART_BASIC_RUNE_BOLT", BattleActionKind.Mystic, "Mystic"
            },
            new object[]
            {
                "RELIC001_INV_003", "Tide Serpent Invocation Relic", "Tide Serpent",
                "INVOCATION_BASE022_10_01", "WTRACK022_10", "RESTORATION", 7, 16,
                "CMD_HEAL", "ART_MINOR_REMEDY", BattleActionKind.Restoration, "Restoration"
            },
            new object[]
            {
                "RELIC001_INV_004", "Stone Colossus Invocation Relic", "Stone Colossus",
                "INVOCATION_BASE022_02_01", "WTRACK022_02", "WARDING", 10, 17,
                "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Warding"
            },
            new object[]
            {
                "RELIC001_INV_005", "Moon Stag Invocation Relic", "Moon Stag",
                "INVOCATION_BASE022_12_01", "WTRACK022_12", "SUPPORT", 8, 13,
                "CMD_SUPPORT", "ART_ASSIST_ALLY", BattleActionKind.Recovery, "Support"
            },
            new object[]
            {
                "RELIC001_INV_006", "Storm Kirin Invocation Relic", "Storm Kirin",
                "INVOCATION_BASE022_04_01", "WTRACK022_04", "TACTICAL", 7, 12,
                "CMD_FLANK", "ART_STEP_BACK", BattleActionKind.Tactical, "Tactical"
            }
        };

        [SetUp]
        public void SetUp()
        {
            _registry = CampaignRegistry022.LoadFromResources();
            _catalog = SpecialRelicRegistry001.Load();
            _progression = new CampaignProgressionCommandService022();
            _battles = new M2BattleCommandService();
            _content = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
        }

        [TestCaseSource(nameof(InvocationCases))]
        public void SixRulesGrantDeterministicManualRareToolRelics(
            string relicId,
            string displayName,
            string summonName,
            string baseId,
            string trackId,
            string role,
            int sharedAp,
            int personalMp,
            string commandId,
            string artId,
            BattleActionKind kind,
            string discipline)
        {
            Assert.That(SpecialRelicInvocationRules001.TryGet(relicId, out var rule), Is.True);
            // Unity's bundled NUnit does not expose Assert.Multiple.
            {
                Assert.That(rule.DisplayName, Is.EqualTo(displayName));
                Assert.That(rule.SummonName, Is.EqualTo(summonName));
                Assert.That(rule.CanonicalBaseId, Is.EqualTo(baseId));
                Assert.That(rule.EquipmentTrackId, Is.EqualTo(trackId));
                Assert.That(rule.Role, Is.EqualTo(role));
                Assert.That(rule.SharedApCost, Is.EqualTo(sharedAp));
                Assert.That(rule.PersonalMpCost, Is.EqualTo(personalMp));
                Assert.That(SpecialRelicInvocationRules001.LocalBalanceAuthority,
                    Is.EqualTo("SPECIAL_RELIC_INVOCATION_LOCAL_BALANCE_001"));
            }

            var sourceReceipt = "TEST_SOURCE_" + relicId;
            var granted = Require(_progression.GrantSpecialInvocationRelic(
                CreateCampaign(), _registry, _catalog, relicId, sourceReceipt));
            var replay = Require(_progression.GrantSpecialInvocationRelic(
                granted, _registry, _catalog, relicId, sourceReceipt));
            Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(granted)));

            var item = granted.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var artifact = Progression(granted).InvocationArtifacts.Single(
                value => value.InstanceId == item.InstanceId);
            // Keep each assertion visible to the Unity 6 NUnit adapter.
            {
                Assert.That(item.DisplayName, Is.EqualTo(rule.ActiveDisplayName));
                Assert.That(item.QualityId, Is.EqualTo(SpecialRelicInvocationRules001.QualityId));
                Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { EquipmentSlotIds.ToolRelic }));
                Assert.That(item.EquipmentTags, Is.EquivalentTo(new[]
                {
                    InvocationArtifactAuthority022.InvocationEquipmentTag,
                    _registry.ArtifactBases[baseId].weaponFamilyId,
                    SpecialRelicInvocationRules001.SpecialRelicEquipmentTag,
                    SpecialRelicInvocationRules001.ManualEquipOnlyTag
                }));
                Assert.That(granted.Guild.Recruits.All(value =>
                    value.Equipment.Find(EquipmentSlotIds.ToolRelic) == null), Is.True);
                Assert.That(artifact.BaseId, Is.EqualTo(baseId));
            }
            Assert.That(InvocationArtifactAuthority022.TryValidateArtifact(
                _registry, artifact, item, out _, out _, out var validationError),
                Is.True, validationError);

            var equipped = Require(new M1CommandService().EquipItem(
                granted, SourceRecruitId, EquipmentSlotIds.ToolRelic, item.InstanceId));
            Assert.That(equipped.Guild.Inventory.Any(value => value.InstanceId == item.InstanceId), Is.False);
            Assert.That(equipped.Guild.Recruits.Single(value => value.RecruitId == SourceRecruitId)
                .Equipment.Find(EquipmentSlotIds.ToolRelic).Item.InstanceId, Is.EqualTo(item.InstanceId));
        }

        [TestCaseSource(nameof(InvocationCases))]
        public void EveryInvocationExecutesThroughOneSelectedCompleteForecastWithExactGrowth(
            string relicId,
            string displayName,
            string summonName,
            string baseId,
            string trackId,
            string role,
            int sharedAp,
            int personalMp,
            string commandId,
            string artId,
            BattleActionKind kind,
            string discipline)
        {
            var selected = GrantedEquippedBattle(
                relicId, commandId, artId, kind, discipline, sourceAp: 99, sourceMp: 99);
            var before = Progression(selected);
            Assert.That(before.AbyssFloors, Is.Empty,
                "P0 Invocation relics must not require an Abyss-floor unlock.");

            var first = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var replay = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            Assert.That(CanonicalJson.Serialize(replay), Is.EqualTo(CanonicalJson.Serialize(first)),
                "Identical selected complete Forecast state must resolve byte-identically.");

            var rule = SpecialRelicInvocationRules001.All.Single(value => value.RelicId == relicId);
            var state = Progression(first);
            var receipt = state.AppliedReceiptIds.Single(value =>
                value.StartsWith("ECHOREC022_", StringComparison.Ordinal));
            var evidence = first.Battle.EventLog.Where(value =>
                value.EventType == "SUMMON_ECHO_INVOKED" && value.ArtId == rule.EchoId).ToArray();
            var artifact = state.InvocationArtifacts.Single(value => value.BaseId == baseId);
            var evolution = state.EquipmentEvolution.Single(value =>
                value.ItemInstanceId == artifact.InstanceId);
            var source = first.Battle.PlayerUnions.Single(value => value.UnionId == SourceUnionId);
            var actor = source.Members.Single(value => value.MemberId == SourceRecruitId);
            var art = _content.Art(artId);
            var expectedMp = 99 - art.PersonalMpCost - personalMp +
                (kind == BattleActionKind.Recovery ? 2 : 0);
            var expectedAp = 99 - art.SharedApCost - sharedAp + 3;

            // Keep each assertion visible to the Unity 6 NUnit adapter.
            {
                Assert.That(evidence, Has.Length.EqualTo(1));
                Assert.That(evidence[0].Amount, Is.GreaterThan(0));
                Assert.That(evidence[0].Text, Does.Contain(receipt));
                Assert.That(state.AppliedReceiptIds.Count(value => value == receipt), Is.EqualTo(1));
                Assert.That(artifact.Resonance, Is.EqualTo(1));
                Assert.That(state.SummonResonance, Is.EqualTo(before.SummonResonance + 1));
                Assert.That(evolution.TrackId, Is.EqualTo(trackId));
                Assert.That(evolution.MeaningfulUses, Is.EqualTo(1));
                Assert.That(evolution.MasteryPoints,
                    Is.EqualTo(SpecialRelicInvocationRules001.MeaningfulUseMasteryGain));
                Assert.That(evolution.HistoryTag, Does.Contain(receipt));
                Assert.That(source.CurrentAp, Is.EqualTo(expectedAp));
                Assert.That(actor.CurrentMp, Is.EqualTo(expectedMp));
                Assert.That(first.Battle.EventLog.Single(value =>
                    value.EventType == "FORECAST_COMMITTED" && value.UnionId == SourceUnionId).Amount,
                    Is.EqualTo(art.SharedApCost + sharedAp));
            }
            if (role == "RESTORATION")
            {
                Assert.That(evidence[0].TargetUnionId, Is.EqualTo(AllyUnionId),
                    "Tide Serpent must be able to restore a different friendly Union.");
                var restoration = first.Battle.EventLog.Single(value =>
                    value.EventType == "RESTORATION" &&
                    value.ActorMemberId == SourceRecruitId &&
                    value.ArtId == artId);
                Assert.That(restoration.TargetUnionId, Is.EqualTo(AllyUnionId),
                    "The authorized Invocation surcharge must not invalidate its base Restoration Art.");
                Assert.That(first.Battle.EventLog.Any(value =>
                    value.EventType == "RESTORATION_WITHHELD" &&
                    value.ActorMemberId == SourceRecruitId &&
                    value.ArtId == artId), Is.False);
            }
        }

        [Test]
        public void InvocationRejectsApOrIndividualMpShortfallWithoutMutation()
        {
            const string relicId = "RELIC001_INV_001";
            Assert.That(SpecialRelicInvocationRules001.TryGet(relicId, out var rule), Is.True);
            var lowAp = GrantedEquippedBattle(
                relicId, "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard",
                sourceAp: rule.SharedApCost - 1, sourceMp: 99);
            AssertRejectedWithoutMutation(lowAp, "CAMPAIGN022_ECHO_RESOURCE_BUDGET_REQUIRED");

            var lowMp = GrantedEquippedBattle(
                relicId, "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard",
                sourceAp: 99, sourceMp: rule.PersonalMpCost - 1);
            AssertRejectedWithoutMutation(lowMp, "CAMPAIGN022_ECHO_RESOURCE_BUDGET_REQUIRED");
        }

        [TestCase("ART_GUARD", BattleActionKind.Guard, "Guard")]
        [TestCase("ART_QUICK_CUT", BattleActionKind.Tactical, "Tactical")]
        public void TerminalEmberRocPaysInvocationMpAndStopsPendingArtWithoutGrowth(
            string baseArtId, BattleActionKind baseKind, string baseDiscipline)
        {
            var selected = GrantedEquippedBattle(
                "RELIC001_INV_002", "CMD_MYSTIC", baseArtId,
                baseKind, baseDiscipline, 99, 99);
            var enemies = selected.Battle.EnemyUnions.Select(union => union.With(
                members: union.Members.Select(member => member.With(currentHp: 1))
                    .ToArray())).ToArray();
            selected = selected.WithBattle(selected.Battle.With(enemyUnions: enemies));
            Assert.That(SpecialRelicInvocationRules001.TryGet(
                "RELIC001_INV_002", out var rule), Is.True);

            var resolved = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var events = resolved.Battle.EventLog;
            var actor = resolved.Battle.PlayerUnions.Single(value =>
                    value.UnionId == SourceUnionId).Members.Single(value =>
                    value.MemberId == SourceRecruitId);

            Assert.That(events.Count(value => value.EventType ==
                "SUMMON_ECHO_INVOKED"), Is.EqualTo(1));
            Assert.That(events.Count(value => value.EventType ==
                "VICTORY_SEQUENCE_STOP"), Is.EqualTo(1));
            Assert.That(events.Any(value => value.EventType == "GUARD" &&
                value.ActorMemberId == SourceRecruitId), Is.False);
            Assert.That(events.Any(value => value.EventType == "TACTICAL_HIT" &&
                value.ActorMemberId == SourceRecruitId), Is.False);
            Assert.That(events.Any(value => value.EventType == "ART_GROWTH" &&
                value.ActorMemberId == SourceRecruitId), Is.False);
            Assert.That(events.Any(value => value.EventType == "BREAKTHROUGH" &&
                value.ActorMemberId == SourceRecruitId), Is.False);
            Assert.That(actor.CurrentMp, Is.EqualTo(99 - rule.PersonalMpCost),
                "The terminal Invocation pays only its own MP; its pending base Art never executes.");
            var source = resolved.Battle.PlayerUnions.Single(value =>
                value.UnionId == SourceUnionId);
            Assert.That(source.CurrentAp, Is.EqualTo(Math.Min(source.MaximumAp,
                99 - rule.SharedApCost + 3)),
                "The executed Invocation stays paid; only the pending base Art AP is returned.");
        }

        [Test]
        public void TideSerpentThatRemovesHealingNeedKeepsInvocationCostAndRefundsOnlyBaseArt()
        {
            var selected = GrantedEquippedBattle(
                "RELIC001_INV_003", "CMD_HEAL", "ART_MINOR_REMEDY",
                BattleActionKind.Restoration, "Restoration", 99, 99);
            var players = selected.Battle.PlayerUnions.ToArray();
            players[1] = players[1].With(members: players[1].Members.Select(
                member => member.With(currentHp: member.MaximumHp - 1)).ToArray());
            selected = selected.WithBattle(selected.Battle.With(
                playerUnions: players));
            Assert.That(SpecialRelicInvocationRules001.TryGet(
                "RELIC001_INV_003", out var rule), Is.True);

            var resolved = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var source = resolved.Battle.PlayerUnions.Single(value =>
                value.UnionId == SourceUnionId);
            var actor = source.Members.Single(value =>
                value.MemberId == SourceRecruitId);
            var baseArt = _content.Art("ART_MINOR_REMEDY");

            Assert.That(resolved.Battle.EventLog.Count(value =>
                value.EventType == "SUMMON_ECHO_INVOKED"), Is.EqualTo(1));
            Assert.That(resolved.Battle.EventLog.Count(value =>
                value.EventType == "RESTORATION_WITHHELD" &&
                value.ActorMemberId == SourceRecruitId), Is.EqualTo(1));
            Assert.That(actor.CurrentMp, Is.EqualTo(99 - rule.PersonalMpCost));
            Assert.That(source.CurrentAp, Is.EqualTo(Math.Min(source.MaximumAp,
                99 - rule.SharedApCost + 3)),
                "Only the unused base Art AP is refunded; Invocation AP stays paid before round recovery.");
            Assert.That(resolved.Battle.EventLog.Any(value =>
                value.EventType == "ART_GROWTH" && value.ArtId == baseArt.Id &&
                value.ActorMemberId == SourceRecruitId), Is.False);
        }

        [Test]
        public void InvocationRejectsOrphanedEvolutionReceiptInsteadOfDoubleAdvancingGrowth()
        {
            var selected = GrantedEquippedBattle(
                "RELIC001_INV_001", "CMD_GUARD", "ART_GUARD",
                BattleActionKind.Guard, "Guard", 99, 99);
            var applied = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var receipt = Progression(applied).AppliedReceiptIds.Single(value =>
                value.StartsWith("ECHOREC022_", StringComparison.Ordinal));
            var orphanedEvolution = Progression(applied).EquipmentEvolution.Single();
            Assert.That(orphanedEvolution.HistoryTag, Does.Contain(receipt));

            var selectedState = Progression(selected);
            var corruptedState = selectedState.With(
                equipmentEvolution: new[] { orphanedEvolution },
                lastCheckpointId: selectedState.LastCheckpointId);
            var corrupted = WithProgression(selected, corruptedState);
            var before = CanonicalJson.Serialize(corrupted);
            var result = _progression.InvokeEligibleEchoForecast(
                corrupted, _registry, _battles, _content);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors,
                Contains.Item("SPECIAL_RELIC001_INVOCATION_RECEIPT_STATE_INCONSISTENT"));
            Assert.That(CanonicalJson.Serialize(corrupted), Is.EqualTo(before));
        }

        [Test]
        public void TideSerpentPrioritizesSelectedAllyAcrossThreeWoundedUnions()
        {
            var selected = GrantedEquippedBattle(
                "RELIC001_INV_003", "CMD_HEAL", "ART_MINOR_REMEDY",
                BattleActionKind.Restoration, "Restoration", 99, 99);
            var battle = selected.Battle;
            var players = new List<BattleUnionState>(battle.PlayerUnions);
            players[0] = players[0].With(members: new[]
            {
                players[0].Members[0].With(currentHp: 100)
            });
            players[1] = players[1].With(members: new[]
            {
                players[1].Members[0].With(currentHp: 100)
            });
            var extraMember = BattleMember(
                ExtraRecruitId, "More Wounded Non-Target", 1, 200, 99, 99,
                new[] { "ART_BASIC_SABER_CUT" });
            var extraUnion = new BattleUnionState(
                ExtraUnionId, "Non-Target Ally", BattleSide.Player, ExtraRecruitId,
                new[] { extraMember }, "FORMATION_SHIELD_WALL", "Shield Wall", true,
                string.Empty, 99, 99, 30, 3000, EngagementState.Engaged,
                false, false, 30);
            players.Add(extraUnion);

            var extraAction = new BattlePlannedActionState(
                ExtraRecruitId, "More Wounded Non-Target", EnemyUnionId,
                "SPECIAL_INV_ENEMY_MEMBER", "ART_BASIC_SABER_CUT", "Saber Cut",
                BattleActionKind.Martial, 0, 0, -10, 0, 0,
                "Legal non-target ally action.", true, false,
                "ART_BASIC_SABER_CUT_ANIM", "Martial", 1);
            var extraForecast = new BattleForecastState(
                "FORECAST_SPECIAL_INV_EXTRA", ExtraUnionId, "CMD_BALANCED", "Balanced Pressure",
                "Hold the third line.", "BALANCED", EnemyUnionId, EnemyUnionId,
                new[] { extraAction }, 0, 0, 0, "Steady pressure", "Controlled",
                string.Empty, "No fallback required", "SPECIAL_INV_EXTRA_GENERATION",
                "SPECIAL_INV_EXTRA_DEBUG");
            var forecasts = new List<BattleForecastState>(battle.CommittedForecasts)
            {
                extraForecast
            };
            var selections = new List<BattleForecastSelectionState>(battle.Selections)
            {
                new BattleForecastSelectionState(ExtraUnionId, extraForecast.ForecastId)
            };
            selected = selected.WithBattle(battle.With(
                playerUnions: players.AsReadOnly(),
                committedForecasts: forecasts.AsReadOnly(),
                selections: selections.AsReadOnly()));

            var resolved = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var evidence = resolved.Battle.EventLog.Single(value =>
                value.EventType == "SUMMON_ECHO_INVOKED" &&
                value.ArtId == "RELIC001_INV_003_ECHO");
            // Keep each assertion visible to the Unity 6 NUnit adapter.
            {
                Assert.That(evidence.TargetUnionId, Is.EqualTo(AllyUnionId));
                Assert.That(evidence.TargetMemberId, Is.EqualTo(AllyRecruitId));
                Assert.That(evidence.TargetUnionId, Is.Not.EqualTo(SourceUnionId));
                Assert.That(evidence.TargetUnionId, Is.Not.EqualTo(ExtraUnionId));
            }
        }

        [TestCase(
            "RELIC001_INV_004", "CMD_GUARD", "ART_GUARD",
            BattleActionKind.Guard, "Warding")]
        [TestCase(
            "RELIC001_INV_005", "CMD_SUPPORT", "ART_ASSIST_ALLY",
            BattleActionKind.Recovery, "Support")]
        public void FriendlyWardingAndSupportFollowTheSelectedAllyForecastTarget(
            string relicId,
            string commandId,
            string artId,
            BattleActionKind kind,
            string discipline)
        {
            var selected = GrantedEquippedBattle(
                relicId, commandId, artId, kind, discipline, 99, 99);
            var battle = selected.Battle;
            var forecasts = new List<BattleForecastState>(battle.CommittedForecasts);
            var sourceIndex = forecasts.FindIndex(value => value.UnionId == SourceUnionId);
            var source = forecasts[sourceIndex];
            var actions = source.MemberActions.Select(value =>
                new BattlePlannedActionState(
                    value.ActorMemberId, value.ActorName, AllyUnionId, AllyRecruitId,
                    value.ArtId, value.ArtName, value.Kind, value.SharedApCost,
                    value.PersonalMpCost, value.PredictedHpDelta,
                    value.PredictedCohesionDelta, value.PredictedFormationDelta,
                    value.Prediction, value.MeaningfulUse, value.BreakthroughOpportunity,
                    value.AnimationTag, value.Discipline, value.PredictedGrowth,
                    value.BreakthroughTargetArtId, value.BreakthroughTargetArtName)).ToArray();
            forecasts[sourceIndex] = new BattleForecastState(
                source.ForecastId, source.UnionId, source.CommandId, source.CommandName,
                source.Phrase, source.TacticalIntent, AllyUnionId, "Ally Union", actions,
                source.SharedApCost, source.ApRecovery, source.CombinedMpCost,
                source.ExpectedEffect, source.Risk, source.LearningOpportunity,
                source.FallbackBehavior, source.GenerationIdentity,
                source.DeterministicDebugEvidence);
            selected = selected.WithBattle(battle.With(committedForecasts: forecasts.AsReadOnly()));

            var resolved = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var evidence = resolved.Battle.EventLog.Single(value =>
                value.EventType == "SUMMON_ECHO_INVOKED" &&
                value.ArtId == relicId + "_ECHO");
            // Keep each assertion visible to the Unity 6 NUnit adapter.
            {
                Assert.That(evidence.TargetUnionId, Is.EqualTo(AllyUnionId));
                Assert.That(evidence.TargetMemberId, Is.EqualTo(AllyRecruitId));
                Assert.That(evidence.TargetUnionId, Is.Not.EqualTo(SourceUnionId));
                if (relicId == "RELIC001_INV_005")
                    Assert.That(resolved.Battle.EventLog.Any(value =>
                        value.EventType == "ALLY_SUPPORT" &&
                        value.ActorUnionId == SourceUnionId &&
                        value.TargetUnionId == AllyUnionId), Is.True,
                        "The authorized Invocation surcharge must not invalidate the underlying cross-Union support Art.");
            }
        }

        [Test]
        public void InvocationRequiresExactManualUnionOwnershipAndRejectsDuplicateOrCorruptState()
        {
            const string relicId = "RELIC001_INV_001";
            var granted = Require(_progression.GrantSpecialInvocationRelic(
                CreateCampaign(), _registry, _catalog, relicId, "OWNERSHIP_SOURCE"));
            var unequipped = AttachBattle(
                granted, "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard", 99, 99);
            AssertRejectedWithoutMutation(
                unequipped, "CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");

            var item = granted.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var wrongUnion = Require(new M1CommandService().EquipItem(
                granted, AllyRecruitId, EquipmentSlotIds.ToolRelic, item.InstanceId));
            wrongUnion = AttachBattle(
                wrongUnion, "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard", 99, 99);
            AssertRejectedWithoutMutation(wrongUnion, "CAMPAIGN022_ECHO_FORECAST_CATEGORY_REQUIRED");

            var equipped = Require(new M1CommandService().EquipItem(
                granted, SourceRecruitId, EquipmentSlotIds.ToolRelic, item.InstanceId));
            var inventory = new List<EquipmentItemState>(equipped.Guild.Inventory) { item };
            var duplicatedGuild = equipped.Guild.With(
                equipped.Guild.TreasuryXp,
                equipped.Guild.Recruits,
                equipped.Guild.Unions,
                inventory.AsReadOnly(),
                equipped.Guild.Development);
            var duplicated = AttachBattle(
                equipped.With(duplicatedGuild, equipped.OpeningFlow),
                "CMD_GUARD", "ART_GUARD", BattleActionKind.Guard, "Guard", 99, 99);
            AssertRejectedWithoutMutation(
                duplicated, "CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");

            var corrupt = new EquipmentItemState(
                item.InstanceId, item.DefinitionId, item.DisplayName,
                item.ValidSlotIds,
                new[]
                {
                    InvocationArtifactAuthority022.InvocationEquipmentTag,
                    _registry.ArtifactBases["INVOCATION_BASE022_07_01"].weaponFamilyId,
                    SpecialRelicInvocationRules001.SpecialRelicEquipmentTag
                },
                item.QualityId, item.ConditionBasisPoints, item.PlayerLocked);
            var artifact = Progression(granted).InvocationArtifacts.Single();
            Assert.That(InvocationArtifactAuthority022.TryValidateArtifact(
                _registry, artifact, corrupt, out _, out _, out _), Is.False);

            var collisionCampaign = CreateCampaign();
            var collisionA = new EquipmentItemState(
                item.InstanceId, "UNRELATED_COLLISION_A", "Collision A",
                new[] { EquipmentSlotIds.ToolRelic }, new[] { "GENERIC_RELIC" },
                "COMMON", 10000, false);
            var collisionGuild = collisionCampaign.Guild.With(
                collisionCampaign.Guild.TreasuryXp,
                collisionCampaign.Guild.Recruits,
                collisionCampaign.Guild.Unions,
                new[] { collisionA },
                collisionCampaign.Guild.Development);
            var collisionResult = _progression.GrantSpecialInvocationRelic(
                collisionCampaign.With(collisionGuild, collisionCampaign.OpeningFlow),
                _registry, _catalog, relicId, "OWNERSHIP_SOURCE");
            Assert.That(collisionResult.IsSuccess, Is.False);
            Assert.That(collisionResult.Errors, Contains.Item("SPECIAL_RELIC001_INSTANCE_ID_COLLISION"));
        }

        [Test]
        public void InvocationGrowthAndReceiptsRoundTripThroughExistingV11Save()
        {
            var selected = GrantedEquippedBattle(
                "RELIC001_INV_003", "CMD_HEAL", "ART_MINOR_REMEDY",
                BattleActionKind.Restoration, "Restoration", 99, 99);
            var resolved = Require(_progression.InvokeEligibleEchoForecast(
                selected, _registry, _battles, _content));
            var path = Path.Combine(
                Path.GetTempPath(), "special_relic_invocation_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var store = new AtomicSaveStore();
                store.Write(path, SaveEnvelopeV1.Create(
                    resolved, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
                var loaded = store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess, Is.True, string.Join("\n", loaded.Errors));
                // Keep each assertion visible to the Unity 6 NUnit adapter.
                {
                    Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(11));
                    Assert.That(CanonicalJson.Serialize(loaded.Value.CampaignState),
                        Is.EqualTo(CanonicalJson.Serialize(resolved)));
                    Assert.That(Progression(loaded.Value.CampaignState).EquipmentEvolution,
                        Has.Count.EqualTo(1));
                }
            }
            finally
            {
                DeleteIfPresent(path);
                DeleteIfPresent(path + ".bak");
                DeleteIfPresent(path + ".tmp");
            }
        }

        private CampaignState GrantedEquippedBattle(
            string relicId,
            string commandId,
            string artId,
            BattleActionKind kind,
            string discipline,
            int sourceAp,
            int sourceMp)
        {
            var granted = Require(_progression.GrantSpecialInvocationRelic(
                CreateCampaign(), _registry, _catalog, relicId, "BATTLE_SOURCE_" + relicId));
            var item = granted.Guild.Inventory.Single(value => value.DefinitionId == relicId);
            var equipped = Require(new M1CommandService().EquipItem(
                granted, SourceRecruitId, EquipmentSlotIds.ToolRelic, item.InstanceId));
            return AttachBattle(equipped, commandId, artId, kind, discipline, sourceAp, sourceMp);
        }

        private CampaignState AttachBattle(
            CampaignState campaign,
            string commandId,
            string artId,
            BattleActionKind kind,
            string discipline,
            int sourceAp,
            int sourceMp)
        {
            var sourceMember = BattleMember(
                SourceRecruitId, "Relic Caller", 200, 200, sourceMp, 99,
                new[] { artId, "ART_GUARD", "ART_BASIC_SABER_CUT" });
            var allyMember = BattleMember(
                AllyRecruitId, "Wounded Ally", 1, 200, 99, 99,
                new[] { "ART_GUARD", "ART_BASIC_SABER_CUT" });
            var enemyMember = BattleMember(
                "SPECIAL_INV_ENEMY_MEMBER", "Training Colossus", 1000, 1000, 99, 99,
                new[] { "ART_BASIC_SABER_CUT" });
            var sourceUnion = new BattleUnionState(
                SourceUnionId, "Caller Union", BattleSide.Player, SourceRecruitId,
                new[] { sourceMember }, "FORMATION_SHIELD_WALL", "Shield Wall", true,
                string.Empty, sourceAp, 99, 50, 5000, EngagementState.Engaged,
                false, false, 30);
            var allyUnion = new BattleUnionState(
                AllyUnionId, "Ally Union", BattleSide.Player, AllyRecruitId,
                new[] { allyMember }, "FORMATION_SHIELD_WALL", "Shield Wall", true,
                string.Empty, 99, 99, 40, 4000, EngagementState.Engaged,
                false, false, 30);
            var enemyUnion = new BattleUnionState(
                EnemyUnionId, "Enemy Union", BattleSide.Enemy, enemyMember.MemberId,
                new[] { enemyMember }, "FORMATION_RAID_WEDGE", "Raid Wedge", true,
                string.Empty, 99, 99, 80, 8000, EngagementState.Engaged,
                false, false, 30);

            var art = _content.Art(artId);
            var friendlyTarget = kind == BattleActionKind.Restoration || kind == BattleActionKind.Recovery;
            var sourceTargetUnion = kind == BattleActionKind.Guard
                ? SourceUnionId
                : friendlyTarget ? AllyUnionId : EnemyUnionId;
            var sourceTargetMember = kind == BattleActionKind.Guard
                ? SourceRecruitId
                : friendlyTarget ? AllyRecruitId : enemyMember.MemberId;
            var sourceAction = new BattlePlannedActionState(
                SourceRecruitId, "Relic Caller", sourceTargetUnion, sourceTargetMember,
                artId, art.Name, kind, art.SharedApCost, art.PersonalMpCost,
                kind == BattleActionKind.Restoration ? 30 : -20,
                kind == BattleActionKind.Guard || kind == BattleActionKind.Recovery ? 8 : -4,
                kind == BattleActionKind.Guard || kind == BattleActionKind.Recovery ? 500 : -400,
                "Legal authored action retained inside the complete Forecast.", true, false,
                art.AnimationTag, discipline, 5);
            var sourceForecast = new BattleForecastState(
                "FORECAST_SPECIAL_INV_SOURCE", SourceUnionId, commandId, commandId,
                "Resolve the complete Union Forecast.", discipline, sourceTargetUnion,
                sourceTargetUnion, new[] { sourceAction }, art.SharedApCost, 0,
                art.PersonalMpCost, "Meaningful effect", "Controlled", string.Empty,
                "No fallback required", "SPECIAL_INV_SOURCE_GENERATION", "SPECIAL_INV_SOURCE_DEBUG");

            var allyAction = new BattlePlannedActionState(
                AllyRecruitId, "Wounded Ally", EnemyUnionId, enemyMember.MemberId,
                "ART_BASIC_SABER_CUT", "Saber Cut", BattleActionKind.Martial,
                0, 0, -10, 0, 0, "Legal ally action.", true, false,
                "ART_BASIC_SABER_CUT_ANIM", "Martial", 1);
            var allyForecast = new BattleForecastState(
                "FORECAST_SPECIAL_INV_ALLY", AllyUnionId, "CMD_BALANCED", "Balanced Pressure",
                "Hold the supporting line.", "BALANCED", EnemyUnionId, EnemyUnionId,
                new[] { allyAction }, 0, 0, 0, "Steady pressure", "Controlled", string.Empty,
                "No fallback required", "SPECIAL_INV_ALLY_GENERATION", "SPECIAL_INV_ALLY_DEBUG");
            var battle = new BattleState(
                "BATTLE_SPECIAL_INVOCATION_001", _content.ContentVersion, 1,
                BattlePhase.ForecastSelection, BattleOutcome.InProgress,
                "Prove deterministic special-relic invocation.",
                new[] { sourceUnion, allyUnion }, new[] { enemyUnion },
                new[] { sourceForecast, allyForecast },
                new[]
                {
                    new BattleForecastSelectionState(SourceUnionId, sourceForecast.ForecastId),
                    new BattleForecastSelectionState(AllyUnionId, allyForecast.ForecastId)
                },
                Array.Empty<BattleEventState>(), Array.Empty<BattleRoundRecordState>(),
                "SPECIAL_INV_BASIS", "SPECIAL_INV_INITIAL", string.Empty,
                string.Empty, string.Empty, false);
            return campaign.WithBattle(battle);
        }

        private static BattleMemberState BattleMember(
            string id,
            string name,
            int currentHp,
            int maximumHp,
            int currentMp,
            int maximumMp,
            IReadOnlyList<string> learnedArts)
        {
            var canonicalLearnedArts = (learnedArts ?? Array.Empty<string>())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            return new BattleMemberState(
                id, name, "CLASS_TEND_GUARDIAN", currentHp, maximumHp, currentMp, maximumMp,
                35, 35,
                new[] { "SWORD", "SHIELD", "STAFF", "FOCUS", "SPEAR", "WEAPON" },
                false, false, false, canonicalLearnedArts, 30, 0, string.Empty,
                canonicalLearnedArts.Select(value =>
                    new BattleArtProgressState(value, string.Empty, 5, 25)).ToArray(),
                "SPECIAL_INV_MAIN_" + id);
        }

        private static CampaignState CreateCampaign()
        {
            var sourceMain = new EquipmentItemState(
                "SPECIAL_INV_MAIN_A", "SPECIAL_INV_SWORD_A", "Relic Caller Sword",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var allyMain = new EquipmentItemState(
                "SPECIAL_INV_MAIN_B", "SPECIAL_INV_SWORD_B", "Ally Sword",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var extraMain = new EquipmentItemState(
                "SPECIAL_INV_MAIN_C", "SPECIAL_INV_SWORD_C", "Extra Ally Sword",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var source = Recruit(
                SourceRecruitId, "Relic Caller",
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, sourceMain)
                }));
            var ally = Recruit(
                AllyRecruitId, "Wounded Ally",
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, allyMain)
                }));
            var extra = Recruit(
                ExtraRecruitId, "More Wounded Non-Target",
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, extraMain)
                }));
            var unions = new[]
            {
                new UnionState(
                    SourceUnionId, "Caller Union", UnionKind.Normal, SourceRecruitId,
                    new[] { SourceRecruitId }, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 30, 8500),
                new UnionState(
                    AllyUnionId, "Ally Union", UnionKind.Normal, AllyRecruitId,
                    new[] { AllyRecruitId }, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 30, 8500),
                new UnionState(
                    ExtraUnionId, "Non-Target Ally", UnionKind.Normal, ExtraRecruitId,
                    new[] { ExtraRecruitId }, "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 30, 8500)
            };
            var guild = new GuildState(
                "GUILD_SPECIAL_INVOCATION_001", 0,
                new[] { source, ally, extra }, unions, Array.Empty<EquipmentItemState>());
            var profile = new NewGuildProfileState(
                "Special Invocation", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var opening = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "special_invocation_ready");
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000009101", 9101, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, opening);
            return WithProgression(campaign, CampaignProgressionState022.Default());
        }

        private static RecruitState Recruit(
            string id,
            string name,
            EquipmentLoadoutState equipment) =>
            new RecruitState(
                id, 200, 200, 99, 99, name, RecruitOriginKind.Procedural,
                string.Empty, "HUMAN", "SKYHOME", "CLASS_TEND_GUARDIAN", "Observed",
                7500, RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                equipment, true, string.Empty, string.Empty, 70, 70);

        private void AssertRejectedWithoutMutation(CampaignState campaign, string expectedError)
        {
            var before = CanonicalJson.Serialize(campaign);
            var result = _progression.InvokeEligibleEchoForecast(
                campaign, _registry, _battles, _content);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Contains.Item(expectedError));
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(before));
            Assert.That(Progression(campaign).AppliedReceiptIds.Any(value =>
                value.StartsWith("ECHOREC022_", StringComparison.Ordinal)), Is.False);
        }

        private static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        private static CampaignState WithProgression(
            CampaignState campaign,
            CampaignProgressionState022 state)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                progression022: state,
                replaceProgression022: true,
                lastCheckpointId: state.LastCheckpointId);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: state.LastCheckpointId);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: state.LastCheckpointId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: state.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        private static void DeleteIfPresent(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
