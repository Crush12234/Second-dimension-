using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SpecialRelicP0Combat001Tests
    {
        private static readonly object[] ExactRules =
        {
            new object[] { "RELIC001_ART_001", "Seal of Worldsplitter", "Worldsplitter", "TREE_CA002_WPN_SWORD_N11", "WTRACK022_01", "CMD_ALL_OUT", 14, 0 },
            new object[] { "RELIC001_ART_002", "Seal of Aegis Absolute", "Aegis Absolute", "TREE_CA002_WPN_SHIELD_N12", "WTRACK022_07", "CMD_GUARD", 14, 0 },
            new object[] { "RELIC001_ART_003", "Seal of Heavenfall Barrage", "Heavenfall Barrage", "TREE_CA002_WPN_BOW_N12", "WTRACK022_05", "CMD_ALL_OUT", 14, 0 },
            new object[] { "RELIC001_ART_004", "Seal of Thousand Fang Tempest", "Thousand Fang Tempest", "TREE_CA002_WPN_DAGGER_N12", "WTRACK022_06", "CMD_ALL_OUT", 14, 0 },
            new object[] { "RELIC001_ART_005", "Seal of Starfire Cataclysm", "Starfire Cataclysm", "TREE_CA002_MYS_FLAME_N12", "WTRACK022_09", "CMD_MYSTIC", 15, 20 },
            new object[] { "RELIC001_ART_006", "Seal of Grand Restoration", "Grand Restoration", "TREE_CA002_MYS_RESTORATION_N12", "WTRACK022_10", "CMD_HEAL", 15, 20 }
        };

        private static readonly string[] RelicIds =
        {
            "RELIC001_ART_001", "RELIC001_ART_002", "RELIC001_ART_003",
            "RELIC001_ART_004", "RELIC001_ART_005", "RELIC001_ART_006"
        };

        [TestCaseSource(nameof(ExactRules))]
        public void SixRulesBindExactP0IdentityAndUnderlyingArt(
            string relicId,
            string displayName,
            string ultimateArtName,
            string artId,
            string trackId,
            string commandId,
            int ap,
            int mp)
        {
            Assert.That(SpecialRelicUltimateArtBattleHook001.All.Count, Is.EqualTo(6));
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryGetRule(relicId, out var rule), Is.True);
            
            {
                Assert.That(rule.DisplayName, Is.EqualTo(displayName));
                Assert.That(rule.UltimateArtName, Is.EqualTo(ultimateArtName));
                Assert.That(rule.UnderlyingArtId, Is.EqualTo(artId));
                Assert.That(rule.EquipmentTrackId, Is.EqualTo(trackId));
                Assert.That(rule.PreferredCommandId, Is.EqualTo(commandId));
                Assert.That(rule.RequiredActionSharedApCost, Is.EqualTo(ap));
                Assert.That(rule.RequiredActionPersonalMpCost, Is.EqualTo(mp));
                Assert.That(rule.ActiveDisplayName, Is.EqualTo(displayName + " • ACTIVE P0"));
            }
        }

        [Test]
        public void GrantShapeRequiresManualToolRelicEquipmentAndExactUnion()
        {
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                RelicIds[0], "SEAL_GRANT_001", out var item, out var error), Is.True, error);
            
            {
                Assert.That(item.DefinitionId, Is.EqualTo(RelicIds[0]));
                Assert.That(item.ValidSlotIds, Is.EqualTo(new[] { EquipmentSlotIds.ToolRelic }));
                Assert.That(item.EquipmentTags, Is.EquivalentTo(new[]
                {
                    SpecialRelicUltimateArtBattleHook001.SealEquipmentTag,
                    SpecialRelicUltimateArtBattleHook001.P0ActiveEquipmentTag
                }));
                Assert.That(item.QualityId, Is.EqualTo("EPIC"));
                Assert.That(item.ConditionBasisPoints, Is.EqualTo(10000));
                Assert.That(item.PlayerLocked, Is.False);
            }

            var equipped = BuildScenario(RelicIds[5]);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seal(
                equipped.Campaign, equipped.Source, out var rule, out var resolved, out error),
                Is.True, error);
            Assert.That(rule.RelicId, Is.EqualTo(RelicIds[5]));
            Assert.That(resolved.InstanceId, Is.EqualTo(equipped.Seal.InstanceId));
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seal(
                equipped.Campaign, equipped.Battle.PlayerUnions[1], out _, out _, out _), Is.False,
                "A seal equipped in the source Union must not authorize another Union.");

            var inventoryOnly = BuildScenario(RelicIds[0], equipSeal: false);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seal(
                inventoryOnly.Campaign, inventoryOnly.Source, out _, out _, out _), Is.False,
                "Owning an unequipped seal is not battle authority.");
        }

        [Test]
        public void OwnershipFailsClosedOnDuplicateOrCorruptP0ButAllowsGenericArt007()
        {
            var withArt007 = BuildScenario(RelicIds[0], includeGenericArt007: true);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryValidateGuildP0SealOwnership(
                withArt007.Campaign.Guild, out var error), Is.True, error);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seal(
                withArt007.Campaign, withArt007.Source, out _, out _, out error), Is.True, error);

            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                RelicIds[0], "SEAL_DUPLICATE_001", out var duplicate, out error), Is.True, error);
            var duplicateCampaign = WithInventory(withArt007, withArt007.Campaign.Guild.Inventory.Concat(
                new[] { duplicate }).ToArray());
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryValidateGuildP0SealOwnership(
                duplicateCampaign.Guild, out _), Is.False);

            var taggedUnknown = new EquipmentItemState(
                "ART007_CORRUPT_TAGGED", "RELIC001_ART_007", "Improper P0 ART 007",
                new[] { EquipmentSlotIds.ToolRelic },
                new[]
                {
                    SpecialRelicUltimateArtBattleHook001.SealEquipmentTag,
                    SpecialRelicUltimateArtBattleHook001.P0ActiveEquipmentTag
                },
                "EPIC", 10000, false);
            var corruptCampaign = WithInventory(withArt007, withArt007.Campaign.Guild.Inventory.Concat(
                new[] { taggedUnknown }).ToArray());
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryValidateGuildP0SealOwnership(
                corruptCampaign.Guild, out error), Is.False);
            Assert.That(error, Does.StartWith("SPECIAL_RELIC001_NON_P0_ULTIMATE_SEALED"));
        }

        [Test]
        public void DistinctEquippedSealsCoexistAndResolveAsAStableSet()
        {
            var scenario = BuildMultiSealScenario();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seals(
                scenario.Campaign, scenario.Source, out var equipped, out var error), Is.True, error);
            Assert.That(equipped.Select(value => value.Rule.RelicId),
                Is.EqualTo(new[] { RelicIds[0], RelicIds[2] }));
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryResolveEquippedP0Seal(
                scenario.Campaign, scenario.Source, out _, out _, out error), Is.True, error,
                "The compatibility resolver must not reject distinct valid seals.");
        }

        [Test]
        public void MultipleEligibleSealsChooseOneDeterministicallyAcrossReplayReloadAndRound()
        {
            var scenario = BuildMultiSealScenario();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.Forecast, out var first), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.Forecast, out var replay), Is.True);
            Assert.That(replay.ReceiptId, Is.EqualTo(first.ReceiptId));
            Assert.That(replay.SealItemInstanceId, Is.EqualTo(first.SealItemInstanceId));

            var reloadedCampaign = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(scenario.Campaign));
            var reloadedForecast = JsonConvert.DeserializeObject<BattleForecastState>(
                JsonConvert.SerializeObject(scenario.Forecast));
            var reloadedSource = reloadedCampaign.Battle.PlayerUnions[0];
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                reloadedCampaign, reloadedCampaign.Battle, reloadedSource, 0,
                reloadedForecast, out var afterReload), Is.True);
            Assert.That(afterReload.ReceiptId, Is.EqualTo(first.ReceiptId));
            Assert.That(afterReload.SealItemInstanceId, Is.EqualTo(first.SealItemInstanceId));

            var augmented0 = SpecialRelicUltimateArtBattleHook001.ApplyForecastAugmentation(
                scenario.Forecast, first);
            var identity1 = ExpectedBaseIdentity(
                scenario.Campaign, scenario.Battle, scenario.Source, 1);
            var forecast1 = CopyForecast(scenario.Forecast, generationIdentity: identity1);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 1,
                forecast1, out var secondSlot), Is.True);
            var augmented1 = SpecialRelicUltimateArtBattleHook001.ApplyForecastAugmentation(
                forecast1, secondSlot);
            var players = scenario.Battle.PlayerUnions.ToList();
            var enemies = scenario.Battle.EnemyUnions.ToList();
            var events = new List<BattleEventState>();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented0, 0,
                players, enemies, events, out _, false), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented1, 0,
                players, enemies, events, out _, false), Is.False,
                "Only one Ultimate may trigger for a Union in a round even through a different slot receipt.");
            Assert.That(events.Count(value => value.EventType ==
                SpecialRelicUltimateArtBattleHook001.EventType), Is.EqualTo(1));
        }

        [Test]
        public void WhenOnlyOneOfSeveralEquippedSealsIsEligibleItWins()
        {
            var scenario = BuildMultiSealScenario(secondSealMastered: false);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.Forecast, out var selected), Is.True);
            Assert.That(selected.Rule.RelicId, Is.EqualTo(RelicIds[0]));
            Assert.That(selected.SealItemInstanceId, Is.EqualTo("SEAL_MULTI_001"));
        }

        [Test]
        public void DifferentCommandCardsEachPreviewAtMostOneSealButExecutionStillTriggersOne()
        {
            var scenario = BuildCrossCommandScenario();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                scenario.Campaign, scenario.Battle, scenario.Source, 1,
                scenario.AllOut, out var allOut), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                scenario.Campaign, scenario.Battle, scenario.Source, 2,
                scenario.Guard, out var guard), Is.True);
            Assert.That(CountToken(allOut.LearningOpportunity,
                SpecialRelicUltimateArtBattleHook001.ArtMarker), Is.EqualTo(1));
            Assert.That(CountToken(guard.LearningOpportunity,
                SpecialRelicUltimateArtBattleHook001.ArtMarker), Is.EqualTo(1));
            Assert.That(allOut.LearningOpportunity, Does.Contain(RelicIds[0]));
            Assert.That(guard.LearningOpportunity, Does.Contain(RelicIds[1]));

            var players = scenario.Battle.PlayerUnions.ToList();
            var enemies = scenario.Battle.EnemyUnions.ToList();
            var events = new List<BattleEventState>();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, guard, 0,
                players, enemies, events, out _, false), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, allOut, 0,
                players, enemies, events, out _, false), Is.False);
            Assert.That(events.Count(value => value.EventType ==
                SpecialRelicUltimateArtBattleHook001.EventType), Is.EqualTo(1));
        }

        [Test]
        public void PlayerLockedEquippedSealRemainsEligibleWhileGrantDefaultsUnlocked()
        {
            var scenario = BuildScenario(RelicIds[0], playerLockedSeal: true);
            Assert.That(scenario.Seal.PlayerLocked, Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.BaseForecast, out _), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                RelicIds[0], "UNLOCKED_GRANT", out var grant, out _), Is.True);
            Assert.That(grant.PlayerLocked, Is.False);
        }

        [TestCaseSource(nameof(RelicIds))]
        public void EveryUltimateSelectsDeterministicallyWithoutRerollOrCostMutation(string relicId)
        {
            var scenario = BuildScenario(relicId);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0, scenario.BaseForecast,
                out var first), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, scenario.Battle, scenario.Source, 0, scenario.BaseForecast,
                out var replay), Is.True);
            
            {
                Assert.That(replay.ReceiptId, Is.EqualTo(first.ReceiptId));
                Assert.That(replay.AugmentedGenerationIdentity, Is.EqualTo(first.AugmentedGenerationIdentity));
                Assert.That(replay.InvokerMemberId, Is.EqualTo(first.InvokerMemberId));
            }

            var augmented = SpecialRelicUltimateArtBattleHook001.ApplyForecastAugmentation(
                scenario.BaseForecast, first);
            
            {
                Assert.That(augmented.ForecastId, Is.EqualTo(first.ForecastId));
                Assert.That(augmented.MemberActions.Select(value => value.ArtId),
                    Is.EqualTo(scenario.BaseForecast.MemberActions.Select(value => value.ArtId)));
                Assert.That(augmented.MemberActions.Select(value => value.SharedApCost),
                    Is.EqualTo(scenario.BaseForecast.MemberActions.Select(value => value.SharedApCost)));
                Assert.That(augmented.MemberActions.Select(value => value.PersonalMpCost),
                    Is.EqualTo(scenario.BaseForecast.MemberActions.Select(value => value.PersonalMpCost)));
                Assert.That(augmented.SharedApCost, Is.EqualTo(scenario.BaseForecast.SharedApCost));
                Assert.That(augmented.CombinedMpCost, Is.EqualTo(scenario.BaseForecast.CombinedMpCost));
                Assert.That(augmented.GenerationIdentity, Is.Not.EqualTo(scenario.BaseForecast.GenerationIdentity));
                Assert.That(augmented.DeterministicDebugEvidence, Does.Contain("\"HiddenSharedApSurcharge\":0"));
                Assert.That(augmented.DeterministicDebugEvidence, Does.Contain("\"HiddenPersonalMpSurcharge\":0"));
            }
        }

        [TestCaseSource(nameof(RelicIds))]
        public void EachUltimateBattleStateAndTargetGateFailsClosed(string relicId)
        {
            var invalid = BuildScenario(relicId, validBattleStateGate: false);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                invalid.Campaign, invalid.Battle, invalid.Source, 0, invalid.BaseForecast, out _),
                Is.False);
        }

        [Test]
        public void FormationMasteryRoundApMpForecastAndBattleLifecycleGatesFailClosed()
        {
            var scenario = BuildScenario(RelicIds[0]);
            var rule = scenario.Rule;

            AssertRejected(scenario, scenario.Battle.With(round: rule.MinimumRound - 1), scenario.Source, scenario.BaseForecast);
            AssertRejected(scenario, scenario.Battle.With(phase: BattlePhase.Resolved), scenario.Source, scenario.BaseForecast);

            var lowFormation = scenario.Source.With(
                formationConditionBasisPoints: rule.MinimumFormationBasisPoints - 1);
            AssertRejected(scenario, WithSource(scenario.Battle, lowFormation), lowFormation, scenario.BaseForecast);
            var lowCohesion = scenario.Source.With(cohesion: rule.MinimumCohesion - 1);
            AssertRejected(scenario, WithSource(scenario.Battle, lowCohesion), lowCohesion, scenario.BaseForecast);
            var lowUse = scenario.Source.With(
                unionMeaningfulUsePoints: rule.MinimumUnionMeaningfulUsePoints - 1);
            AssertRejected(scenario, WithSource(scenario.Battle, lowUse), lowUse, scenario.BaseForecast);
            var lowAp = scenario.Source.With(currentAp: rule.RequiredActionSharedApCost - 1);
            AssertRejected(scenario, WithSource(scenario.Battle, lowAp), lowAp, scenario.BaseForecast);
            var retreated = scenario.Source.With(retreated: true);
            AssertRejected(scenario, WithSource(scenario.Battle, retreated), retreated, scenario.BaseForecast);

            var member = scenario.Source.Members[0];
            var lowMpMember = member.With(currentMp: Math.Max(0, rule.RequiredActionPersonalMpCost - 1));
            var lowMp = scenario.Source.With(members: new[] { lowMpMember });
            if (rule.RequiredActionPersonalMpCost > 0)
                AssertRejected(scenario, WithSource(scenario.Battle, lowMp), lowMp, scenario.BaseForecast);
            var unmasteredMember = member.With(artProgress: new[]
            {
                new BattleArtProgressState(rule.UnderlyingArtId, "TEST", rule.MinimumMeaningfulUses - 1, 5)
            });
            var unmastered = scenario.Source.With(members: new[] { unmasteredMember });
            AssertRejected(scenario, WithSource(scenario.Battle, unmastered), unmastered, scenario.BaseForecast);

            var wrongCommand = CopyForecast(scenario.BaseForecast, commandId: "CMD_BALANCED");
            AssertRejected(scenario, scenario.Battle, scenario.Source, wrongCommand);
            var wrongCostAction = Action(rule, scenario.Source, scenario.Target,
                sharedAp: rule.RequiredActionSharedApCost + 1);
            var wrongCost = CopyForecast(scenario.BaseForecast,
                actions: new[] { wrongCostAction },
                sharedApCost: rule.RequiredActionSharedApCost + 1);
            AssertRejected(scenario, scenario.Battle, scenario.Source, wrongCost);
        }

        [Test]
        public void ApplicationEmitsOneValidatedEventAndRejectsDuplicateMarkersAndReceipts()
        {
            var scenario = BuildScenario(RelicIds[0]);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                scenario.Campaign, scenario.Battle, scenario.Source, 0, scenario.BaseForecast,
                out var augmented), Is.True);
            var players = scenario.Battle.PlayerUnions.ToList();
            var enemies = scenario.Battle.EnemyUnions.ToList();
            var events = new List<BattleEventState>();

            var duplicateMarker = CopyForecast(augmented,
                learning: augmented.LearningOpportunity + " · " +
                          SpecialRelicUltimateArtBattleHook001.ArtMarker + RelicIds[0]);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, duplicateMarker, 0,
                players, enemies, events, out _, false), Is.False);

            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented, 0,
                players, enemies, events, out var proof, false), Is.True);
            
            {
                Assert.That(events.Count, Is.EqualTo(1));
                Assert.That(events[0].EventType, Is.EqualTo(SpecialRelicUltimateArtBattleHook001.EventType));
                Assert.That(events[0].ArtId, Is.EqualTo(scenario.Rule.UnderlyingArtId));
                Assert.That(events[0].Text, Does.Contain(proof.ReceiptId));
                Assert.That(proof.SealItemInstanceId, Is.EqualTo(scenario.Seal.InstanceId));
                Assert.That(proof.ForecastId, Is.EqualTo(augmented.ForecastId));
            }
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented, 0,
                players, enemies, events, out _, false), Is.False,
                "A Union/round receipt must apply at most once.");
        }

        [Test]
        public void ProgressionIsExactOnceRejectsTamperedReplayAndDoesNotChangeSummonResonance()
        {
            var application = AppliedScenario(RelicIds[0]);
            var startingProgression = Progression(application.Campaign).With(summonResonance: 9);
            var campaign = WithProgression(application.Campaign, startingProgression);
            var applied = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                campaign, new[] { application.Proof });
            Assert.That(applied.IsSuccess, Is.True, string.Join(" | ", applied.Errors));
            var state = Progression(applied.Value);
            var evolution = state.EquipmentEvolution.Single(value =>
                value.ItemInstanceId == application.Proof.SealItemInstanceId);
            
            {
                Assert.That(evolution.TrackId, Is.EqualTo(application.Scenario.Rule.EquipmentTrackId));
                Assert.That(evolution.MeaningfulUses, Is.EqualTo(1));
                Assert.That(evolution.MasteryPoints,
                    Is.EqualTo(SpecialRelicUltimateArtBattleHook001.MeaningfulUseMasteryGain));
                Assert.That(evolution.HistoryTag, Does.Contain(application.Proof.ReceiptId));
                Assert.That(state.AppliedReceiptIds, Does.Contain(application.Proof.ReceiptId));
                Assert.That(state.SummonResonance, Is.EqualTo(9),
                    "Ultimate mastery must not cross-pollute Invocation resonance.");
            }

            var replay = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                applied.Value, new[] { application.Proof });
            Assert.That(replay.IsSuccess, Is.True, string.Join(" | ", replay.Errors));
            var replayEvolution = Progression(replay.Value).EquipmentEvolution.Single();
            Assert.That(replayEvolution.MeaningfulUses, Is.EqualTo(1));
            Assert.That(replayEvolution.MasteryPoints,
                Is.EqualTo(SpecialRelicUltimateArtBattleHook001.MeaningfulUseMasteryGain));

            var missingEvolution = WithProgression(applied.Value,
                state.With(equipmentEvolution: Array.Empty<EquipmentEvolutionState022>()));
            Assert.That(SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                missingEvolution, new[] { application.Proof }).IsSuccess, Is.False);

            var forgedEvolution = new EquipmentEvolutionState022(
                evolution.ItemInstanceId, evolution.TrackId, evolution.TierId,
                evolution.MeaningfulUses, evolution.MasteryPoints,
                evolution.AppliedRecipeIds, "FORGED_HISTORY");
            var forgedHistory = WithProgression(applied.Value,
                state.With(equipmentEvolution: new[] { forgedEvolution }));
            Assert.That(SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                forgedHistory, new[] { application.Proof }).IsSuccess, Is.False);

            var evidence = applied.Value.Battle.EventLog[0];
            var forgedEvent = new BattleEventState(
                evidence.Sequence, evidence.Round, evidence.EventType, evidence.Side,
                evidence.UnionId, evidence.MemberId, evidence.ArtId, evidence.Text,
                evidence.Amount, "FORGED_EVENT_HASH", evidence.ActorUnionId,
                evidence.ActorMemberId, evidence.TargetUnionId, evidence.TargetMemberId);
            var forgedReplay = applied.Value.WithBattle(applied.Value.Battle.With(
                eventLog: new[] { forgedEvent }));
            Assert.That(SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                forgedReplay, new[] { application.Proof }).IsSuccess, Is.False,
                "Even an already-recorded receipt must revalidate its event proof.");
        }

        [Test]
        public void OrphanedIncomingOrFutureEvolutionReceiptFailsWithoutMutation()
        {
            var application = AppliedScenario(RelicIds[0]);
            var baseState = Progression(application.Campaign);
            var incomingOrphan = new EquipmentEvolutionState022(
                application.Proof.SealItemInstanceId,
                application.Scenario.Rule.EquipmentTrackId,
                "TRAINING", 1, 5, Array.Empty<string>(),
                application.Proof.ReceiptId);
            var incomingCampaign = WithProgression(
                application.Campaign,
                baseState.With(
                    equipmentEvolution: new[] { incomingOrphan },
                    appliedReceiptIds: Array.Empty<string>()));
            var incomingBefore = CanonicalJson.Serialize(incomingCampaign);
            var incomingRejected = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                incomingCampaign, new[] { application.Proof });
            Assert.That(incomingRejected.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(incomingCampaign), Is.EqualTo(incomingBefore));

            const string futureReceipt = "ULTREC001_ABCDEF0123456789ABCDEF01";
            var futureOrphan = new EquipmentEvolutionState022(
                application.Proof.SealItemInstanceId,
                application.Scenario.Rule.EquipmentTrackId,
                "TRAINING", 1, 5, Array.Empty<string>(),
                "UNRELATED_HISTORY | " + futureReceipt);
            var futureCampaign = WithProgression(
                application.Campaign,
                baseState.With(
                    equipmentEvolution: new[] { futureOrphan },
                    appliedReceiptIds: Array.Empty<string>()));
            var futureBefore = CanonicalJson.Serialize(futureCampaign);
            var futureRejected = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                futureCampaign, new[] { application.Proof });
            Assert.That(futureRejected.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(futureCampaign), Is.EqualTo(futureBefore));

            var unrelated = new EquipmentEvolutionState022(
                application.Proof.SealItemInstanceId,
                application.Scenario.Rule.EquipmentTrackId,
                "TRAINING", 0, 0, Array.Empty<string>(), "UNRELATED_HISTORY");
            var unrelatedCampaign = WithProgression(
                application.Campaign,
                baseState.With(
                    equipmentEvolution: new[] { unrelated },
                    appliedReceiptIds: Array.Empty<string>()));
            var accepted = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                unrelatedCampaign, new[] { application.Proof });
            Assert.That(accepted.IsSuccess, Is.True, string.Join(" | ", accepted.Errors));
            Assert.That(Progression(accepted.Value).EquipmentEvolution.Single().HistoryTag,
                Does.StartWith("UNRELATED_HISTORY | " + application.Proof.ReceiptId));
        }

        [TestCase("campaign022_echo_pending:ECHOREC022_0123456789ABCDEF01234567")]
        [TestCase("campaign022_covenant_battle_pending:COVBATTLE022_89ABCDEF0123456789ABCDEF")]
        public void UltimateProgressionPreservesCanonicalInvocationPendingCheckpoint(
            string pendingCheckpoint)
        {
            var application = AppliedScenario(RelicIds[0]);
            var campaign = WithProgression(
                application.Campaign,
                Progression(application.Campaign).With(
                    lastCheckpointId: pendingCheckpoint));
            var applied = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                campaign, new[] { application.Proof });
            Assert.That(applied.IsSuccess, Is.True, string.Join(" | ", applied.Errors));
            var value = applied.Value;
            Assert.That(Progression(value).LastCheckpointId, Is.EqualTo(pendingCheckpoint));
            Assert.That(value.Guild.GuildCity.Strategic017H.Campaign019.Playable020.LastCheckpointId,
                Is.EqualTo(pendingCheckpoint));
            Assert.That(value.Guild.GuildCity.Strategic017H.Campaign019.LastCheckpointId,
                Is.EqualTo(pendingCheckpoint));
            Assert.That(value.Guild.GuildCity.Strategic017H.LastCheckpointId,
                Is.EqualTo(pendingCheckpoint));
            Assert.That(value.Guild.GuildCity.LastCheckpointId, Is.EqualTo(pendingCheckpoint));
        }

        [TestCase("campaign022_echo_pending:")]
        [TestCase("campaign022_echo_pending:ECHOREC022_ABC")]
        [TestCase("campaign022_echo_pending:ECHOREC022_0123456789abcdef01234567")]
        [TestCase("campaign022_echo_pending:ECHOREC022_0123456789ABCDEFG1234567")]
        [TestCase("campaign022_covenant_battle_pending:COVBATTLE022_ABC")]
        [TestCase("campaign022_other_pending:ECHOREC022_0123456789ABCDEF01234567")]
        public void InvalidOrUnknownPendingCheckpointFailsClosed(string checkpoint)
        {
            var application = AppliedScenario(RelicIds[0]);
            var campaign = WithProgression(
                application.Campaign,
                Progression(application.Campaign).With(lastCheckpointId: checkpoint));
            Assert.That(SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                campaign, new[] { application.Proof }).IsSuccess, Is.False);
        }

        [Test]
        public void DistinctCanonicalPendingCheckpointsAcrossNestedStateFailClosed()
        {
            const string echo =
                "campaign022_echo_pending:ECHOREC022_0123456789ABCDEF01234567";
            const string covenant =
                "campaign022_covenant_battle_pending:COVBATTLE022_89ABCDEF0123456789ABCDEF";
            var application = AppliedScenario(RelicIds[0]);
            var campaign = WithProgression(
                application.Campaign,
                Progression(application.Campaign).With(lastCheckpointId: echo));
            var conflictingCity = campaign.Guild.GuildCity.With(
                lastCheckpointId: covenant);
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(conflictingCity), campaign.OpeningFlow);
            var rejected = SpecialRelicUltimateArtBattleHook001.ApplyProgression(
                campaign, new[] { application.Proof });
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain(
                "SPECIAL_RELIC001_INVOCATION_PENDING_CHECKPOINT_CONFLICT"));
        }

        [Test]
        public void EchoAndUltimateInDifferentUnionsCommitAtomicallyAndEchoFinalizes()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var combat = M2CombatContent.LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var battles = new M2BattleCommandService();
            var progression = new CampaignProgressionCommandService022();
            var campaign = CreateCombinedEchoUltimateCampaign();
            var started = battles.StartTutorialBattle(campaign, combat);
            Assert.That(started.IsSuccess, Is.True, string.Join(" | ", started.Errors));
            campaign = PrepareCombinedRound(started.Value, out var echoForecastId, out var ultimateForecastId);
            var echoUnion = campaign.Battle.PlayerUnions.Single(value => value.UnionId == "UNION_ECHO_COMBINED");
            var ultimateUnion = campaign.Battle.PlayerUnions.Single(value => value.UnionId == "UNION_ULTIMATE_COMBINED");
            var selectedEcho = battles.SelectForecast(campaign, echoUnion.UnionId, echoForecastId);
            Assert.That(selectedEcho.IsSuccess, Is.True, string.Join(" | ", selectedEcho.Errors));
            var selectedUltimate = battles.SelectForecast(
                selectedEcho.Value, ultimateUnion.UnionId, ultimateForecastId);
            Assert.That(selectedUltimate.IsSuccess, Is.True,
                string.Join(" | ", selectedUltimate.Errors));

            var combined = progression.InvokeEligibleEchoForecast(
                selectedUltimate.Value, registry, battles, combat);
            Assert.That(combined.IsSuccess, Is.True, string.Join(" | ", combined.Errors));
            var state = Progression(combined.Value);
            Assert.That(combined.Value.Battle.EventLog.Count(value =>
                value.EventType == "SUMMON_ECHO_INVOKED"), Is.EqualTo(1));
            Assert.That(combined.Value.Battle.EventLog.Count(value =>
                value.EventType == SpecialRelicUltimateArtBattleHook001.EventType), Is.EqualTo(1));
            Assert.That(state.AppliedReceiptIds.Count(value =>
                value.StartsWith("ECHOREC022_", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(state.AppliedReceiptIds.Count(value =>
                value.StartsWith("ULTREC001_", StringComparison.Ordinal)), Is.EqualTo(1));
            Assert.That(state.InvocationArtifacts.Single(value =>
                value.InstanceId == "INVOCATION_COMBINED_001").Resonance, Is.EqualTo(1));
            Assert.That(state.SummonResonance, Is.EqualTo(1),
                "Only the finalized Echo increments Invocation resonance.");
            var sealGrowth = state.EquipmentEvolution.Single(value =>
                value.ItemInstanceId == "SEAL_COMBINED_ULTIMATE");
            Assert.That(sealGrowth.MeaningfulUses, Is.EqualTo(1));
            Assert.That(sealGrowth.MasteryPoints,
                Is.EqualTo(SpecialRelicUltimateArtBattleHook001.MeaningfulUseMasteryGain));
            Assert.That(state.LastCheckpointId, Does.StartWith("campaign022_echo_invoked:"),
                "InvokeEligibleEchoForecast can finalize only if Ultimate progression preserved its pending checkpoint.");
        }

        [Test]
        public void GrandRestorationPreservesCrossUnionTargetWhenAdapterIsRequested()
        {
            var scenario = BuildScenario(RelicIds[5]);
            Assert.That(scenario.Target.UnionId, Is.Not.EqualTo(scenario.Source.UnionId));
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.BaseForecast, out var augmented), Is.True);
            var players = scenario.Battle.PlayerUnions.ToList();
            var enemies = scenario.Battle.EnemyUnions.ToList();
            var events = new List<BattleEventState>();
            var hpBefore = players[1].Members[0].CurrentHp;

            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented, 0,
                players, enemies, events, out var proof, true), Is.True);
            
            {
                Assert.That(proof.UnderlyingArtId, Is.EqualTo("TREE_CA002_MYS_RESTORATION_N12"));
                Assert.That(players[1].Members[0].CurrentHp, Is.EqualTo(hpBefore),
                    "The mapped member Art, not the source-prioritized adapter, owns cross-Union healing.");
                Assert.That(events.Single().TargetUnionId, Is.EqualTo(scenario.Target.UnionId));
                Assert.That(events.Single().Amount, Is.Zero);
                Assert.That(events.Single().Text, Does.Contain("cross-Union Restoration target"));
            }
        }

        private static void AssertRejected(
            Scenario scenario,
            BattleState battle,
            BattleUnionState source,
            BattleForecastState forecast) =>
            Assert.That(SpecialRelicUltimateArtBattleHook001.TrySelectForecastAugmentation(
                scenario.Campaign, battle, source, 0, forecast, out _), Is.False);

        private static Applied AppliedScenario(string relicId)
        {
            var scenario = BuildScenario(relicId);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                scenario.Campaign, scenario.Battle, scenario.Source, 0,
                scenario.BaseForecast, out var augmented), Is.True);
            var players = scenario.Battle.PlayerUnions.ToList();
            var enemies = scenario.Battle.EnemyUnions.ToList();
            var events = new List<BattleEventState>();
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryApplySelectedForecast(
                scenario.Campaign, scenario.Battle, augmented, 0,
                players, enemies, events, out var proof, false), Is.True);
            var campaign = scenario.Campaign.WithBattle(scenario.Battle.With(eventLog: events));
            return new Applied(scenario, campaign, proof);
        }

        private static CampaignState CreateCombinedEchoUltimateCampaign()
        {
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                RelicIds[0], "SEAL_COMBINED_ULTIMATE", out var seal, out _), Is.True);
            var echoWeapon = new EquipmentItemState(
                "ECHO_COMBINED_WEAPON", "ECHO_COMBINED_SHIELD", "Combined Echo Shield",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SHIELD", "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var echoTool = new EquipmentItemState(
                "INVOCATION_COMBINED_001", "INVOCATION_BASE022_02_01",
                "Combined Great Weapons Echo Relic",
                new[] { EquipmentSlotIds.ToolRelic },
                new[] { "INVOCATION_ARTIFACT", "WEAPON_FAMILY_GREAT_WEAPON" },
                "UNCOMMON", 10000, false);
            var ultimateWeapon = new EquipmentItemState(
                "ULTIMATE_COMBINED_WEAPON", "ULTIMATE_COMBINED_SWORD", "Combined Sword",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var echoRecruit = Recruit("RECRUIT_ECHO_COMBINED",
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, echoWeapon),
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic, echoTool)
                }));
            var ultimateRecruit = Recruit("RECRUIT_ULTIMATE_COMBINED",
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, ultimateWeapon),
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic, seal)
                }));
            var guild = new GuildState(
                "GUILD_COMBINED_ECHO_ULTIMATE", 0,
                new[] { echoRecruit, ultimateRecruit },
                new[]
                {
                    new UnionState(
                        "UNION_ECHO_COMBINED", "Echo Union", UnionKind.Normal,
                        echoRecruit.RecruitId, new[] { echoRecruit.RecruitId },
                        "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 40, 9000),
                    new UnionState(
                        "UNION_ULTIMATE_COMBINED", "Ultimate Union", UnionKind.Normal,
                        ultimateRecruit.RecruitId, new[] { ultimateRecruit.RecruitId },
                        "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 40, 9000)
                });
            var profile = new NewGuildProfileState(
                "Combined Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "combined_ready");
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000000995", 995, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
            var state = CampaignProgressionState022.Default().With(
                abyssFloors: new[]
                {
                    new AbyssFloorProgressState022(
                        "ABYSS_FLOOR_001_MUD_TRENCHES", 1, 1, true, false,
                        Array.Empty<string>())
                },
                invocationArtifacts: new[]
                {
                    new InvocationArtifactState022(
                        echoTool.InstanceId, echoTool.DefinitionId, Array.Empty<string>(),
                        "INVOCATION_PATH022_02", 0, 0, false)
                },
                lastCheckpointId: "combined_ready");
            return WithProgression(campaign, state);
        }

        private static CampaignState PrepareCombinedRound(
            CampaignState campaign,
            out string echoForecastId,
            out string ultimateForecastId)
        {
            var battle = campaign.Battle;
            var rule = SpecialRelicUltimateArtBattleHook001.All.Single(value =>
                value.RelicId == RelicIds[0]);
            var echoUnion = battle.PlayerUnions.Single(value =>
                value.UnionId == "UNION_ECHO_COMBINED").With(currentAp: 40);
            var ultimateSource = battle.PlayerUnions.Single(value =>
                value.UnionId == "UNION_ULTIMATE_COMBINED");
            var ultimateMember = ultimateSource.Members[0].With(
                currentMp: Math.Max(ultimateSource.Members[0].CurrentMp, 30),
                learnedArtIds: ultimateSource.Members[0].LearnedArtIds.Concat(
                    new[] { rule.UnderlyingArtId }).Distinct(StringComparer.Ordinal).ToArray(),
                meaningfulUsePoints: 30,
                artProgress: new[]
                {
                    new BattleArtProgressState(
                        rule.UnderlyingArtId, "Martial", rule.MinimumMeaningfulUses, 5)
                });
            ultimateSource = new BattleUnionState(
                ultimateSource.UnionId, ultimateSource.DisplayName, ultimateSource.Side,
                ultimateMember.MemberId, new[] { ultimateMember },
                ultimateSource.FormationId, ultimateSource.FormationName,
                true, string.Empty, 40, Math.Max(40, ultimateSource.MaximumAp),
                90, 9000, EngagementState.Engaged, false, false, 30);
            var enemy = battle.EnemyUnions[0];
            enemy = enemy.With(engagement: EngagementState.Engaged);
            battle = battle.With(
                round: 2,
                playerUnions: new[] { echoUnion, ultimateSource },
                enemyUnions: new[] { enemy },
                selections: Array.Empty<BattleForecastSelectionState>());
            campaign = campaign.WithBattle(battle);

            var echoForecast = battle.CommittedForecasts.Single(value =>
                value.UnionId == echoUnion.UnionId && value.CommandId == "CMD_GUARD");
            var identity = ExpectedBaseIdentity(campaign, battle, ultimateSource, 1);
            var ultimateBase = Forecast(rule, ultimateSource, enemy, identity);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryAugmentForecast(
                campaign, battle, ultimateSource, 1, ultimateBase,
                out var ultimateForecast), Is.True);
            battle = battle.With(
                committedForecasts: new[] { echoForecast, ultimateForecast },
                selections: Array.Empty<BattleForecastSelectionState>());
            echoForecastId = echoForecast.ForecastId;
            ultimateForecastId = ultimateForecast.ForecastId;
            return campaign.WithBattle(battle);
        }

        private static MultiScenario BuildMultiSealScenario(bool secondSealMastered = true)
        {
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryGetRule(
                RelicIds[0], out var firstRule), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryGetRule(
                RelicIds[2], out var secondRule), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                firstRule.RelicId, "SEAL_MULTI_001", out var firstSeal, out _), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                secondRule.RelicId, "SEAL_MULTI_003", out var secondSeal, out _), Is.True);
            var firstMember = Member("MULTI_MEMBER_001", firstRule);
            var secondMember = Member("MULTI_MEMBER_003", secondRule);
            if (!secondSealMastered)
                secondMember = secondMember.With(artProgress: new[]
                {
                    new BattleArtProgressState(
                        secondRule.UnderlyingArtId, "TEST",
                        secondRule.MinimumMeaningfulUses - 1, 5)
                });
            var source = Union(
                "PU_MULTI", BattleSide.Player, new[] { firstMember, secondMember },
                9000, 90, 30, 40, EngagementState.Engaged);
            var target = Enemy("EU_MULTI", 50, 5000, 50, EngagementState.Engaged);
            var battle = Battle(3, new[] { source }, new[] { target });
            var recruits = new[]
            {
                Recruit(firstMember.MemberId, Loadout(firstSeal)),
                Recruit(secondMember.MemberId, Loadout(secondSeal))
            };
            var guild = new GuildState(
                "GUILD_MULTI_SEAL_001", 0, recruits,
                new[] { GuildUnion(source.UnionId, recruits.Select(value => value.RecruitId).ToArray()) });
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000000993", 993, "TEST",
                ModeRuleSnapshot.StandardDefaults(), guild).WithBattle(battle);
            var actions = new[]
            {
                Action(firstRule, source, target),
                new BattlePlannedActionState(
                    secondMember.MemberId, secondMember.DisplayName,
                    target.UnionId, target.Members[0].MemberId,
                    secondRule.UnderlyingArtId, secondRule.UltimateArtName,
                    Kind(secondRule), secondRule.RequiredActionSharedApCost,
                    secondRule.RequiredActionPersonalMpCost, -20, 0, 0,
                    "Exact second underlying Art", true, false,
                    "ULTIMATE_TEST", secondRule.EffectRole, 5)
            };
            var identity = ExpectedBaseIdentity(campaign, battle, source, 0);
            var forecast = new BattleForecastState(
                "BASE_MULTI_FORECAST", source.UnionId, "CMD_ALL_OUT", "All Out",
                "Both mastered Arts align.", "Multi-seal deterministic test",
                target.UnionId, target.DisplayName, actions,
                actions.Sum(value => value.SharedApCost), 0,
                actions.Sum(value => value.PersonalMpCost), "Two exact actions",
                "Test risk", string.Empty, "Fallback", identity, "MULTI_DEBUG");
            return new MultiScenario(campaign, battle, source, target, forecast);
        }

        private static CrossCommandScenario BuildCrossCommandScenario()
        {
            var allOutRule = SpecialRelicUltimateArtBattleHook001.All.Single(value =>
                value.RelicId == RelicIds[0]);
            var guardRule = SpecialRelicUltimateArtBattleHook001.All.Single(value =>
                value.RelicId == RelicIds[1]);
            SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                allOutRule.RelicId, "SEAL_CROSS_ALL_OUT", out var allOutSeal, out _);
            SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                guardRule.RelicId, "SEAL_CROSS_GUARD", out var guardSeal, out _);
            var allOutMember = Member("CROSS_MEMBER_ALL_OUT", allOutRule, 50);
            var guardMember = Member("CROSS_MEMBER_GUARD", guardRule, 50);
            var source = Union(
                "PU_CROSS_COMMAND", BattleSide.Player,
                new[] { allOutMember, guardMember },
                9000, 90, 30, 40, EngagementState.Engaged);
            var target = Enemy("EU_CROSS_COMMAND", 70, 9000, 90, EngagementState.Engaged);
            var battle = Battle(3, new[] { source }, new[] { target });
            var recruits = new[]
            {
                Recruit(allOutMember.MemberId, Loadout(allOutSeal)),
                Recruit(guardMember.MemberId, Loadout(guardSeal))
            };
            var guild = new GuildState(
                "GUILD_CROSS_COMMAND", 0, recruits,
                new[] { GuildUnion(source.UnionId, recruits.Select(value => value.RecruitId).ToArray()) });
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000000994", 994, "TEST",
                ModeRuleSnapshot.StandardDefaults(), guild).WithBattle(battle);
            var allOutActions = new[]
            {
                Action(allOutRule, source, target),
                FillerAction(guardMember, target)
            };
            var guardActions = new[]
            {
                FillerAction(allOutMember, source),
                new BattlePlannedActionState(
                    guardMember.MemberId, guardMember.DisplayName,
                    source.UnionId, source.Members[0].MemberId,
                    guardRule.UnderlyingArtId, guardRule.UltimateArtName,
                    BattleActionKind.Guard, guardRule.RequiredActionSharedApCost,
                    guardRule.RequiredActionPersonalMpCost, 0, 0, 0,
                    "Exact shield Art", true, false, "GUARD", "Guard", 5)
            };
            var allOut = CompleteForecast(
                source, target, allOutRule.PreferredCommandId, allOutActions,
                ExpectedBaseIdentity(campaign, battle, source, 1));
            var guard = CompleteForecast(
                source, source, guardRule.PreferredCommandId, guardActions,
                ExpectedBaseIdentity(campaign, battle, source, 2));
            return new CrossCommandScenario(campaign, battle, source, allOut, guard);
        }

        private static BattlePlannedActionState FillerAction(
            BattleMemberState member,
            BattleUnionState target) =>
            new BattlePlannedActionState(
                member.MemberId, member.DisplayName, target.UnionId,
                target.Members[0].MemberId, "ART_GUARD", "Guard",
                BattleActionKind.Guard, 0, 0, 0, 0, 0,
                "Context filler", false, false, "GUARD");

        private static BattleForecastState CompleteForecast(
            BattleUnionState source,
            BattleUnionState target,
            string commandId,
            IReadOnlyList<BattlePlannedActionState> actions,
            string identity) =>
            new BattleForecastState(
                "BASE_" + commandId, source.UnionId, commandId, commandId,
                "Complete card", "Contextual card", target.UnionId, target.DisplayName,
                actions, actions.Sum(value => value.SharedApCost), 0,
                actions.Sum(value => value.PersonalMpCost), "Effect", "Risk",
                string.Empty, "Fallback", identity, "DEBUG");

        private static int CountToken(string value, string token)
        {
            var count = 0;
            var start = 0;
            while (!string.IsNullOrEmpty(value) &&
                   (start = value.IndexOf(token, start, StringComparison.Ordinal)) >= 0)
            {
                count++;
                start += token.Length;
            }
            return count;
        }

        private static Scenario BuildScenario(
            string relicId,
            bool equipSeal = true,
            bool validBattleStateGate = true,
            bool includeGenericArt007 = false,
            bool playerLockedSeal = false)
        {
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryGetRule(relicId, out var rule), Is.True);
            Assert.That(SpecialRelicUltimateArtBattleHook001.TryCreateP0SealGrantItem(
                relicId, "SEAL_" + relicId, out var seal, out var error), Is.True, error);
            if (playerLockedSeal)
                seal = new EquipmentItemState(
                    seal.InstanceId, seal.DefinitionId, seal.DisplayName,
                    seal.ValidSlotIds, seal.EquipmentTags, seal.QualityId,
                    seal.ConditionBasisPoints, true);

            var sourceHp = relicId == RelicIds[1] && validBattleStateGate ? 50 : 100;
            var source = Union("PU_SOURCE", BattleSide.Player,
                new[] { Member("RECRUIT_SOURCE", rule, sourceHp) },
                9000, 90, 30, 40, EngagementState.Engaged);

            var players = new List<BattleUnionState> { source };
            var recruits = new List<RecruitState>
            {
                Recruit("RECRUIT_SOURCE", equipSeal ? Loadout(seal) : EquipmentLoadoutState.Empty())
            };
            var guildUnions = new List<UnionState>
            {
                GuildUnion("PU_SOURCE", new[] { "RECRUIT_SOURCE" })
            };

            BattleUnionState target;
            if (relicId == RelicIds[5])
            {
                target = Union("PU_ALLY", BattleSide.Player,
                    new[] { PlainMember("RECRUIT_ALLY", validBattleStateGate ? 40 : 100) },
                    8000, 80, 20, 30, EngagementState.Supporting);
                players.Add(target);
                recruits.Add(Recruit("RECRUIT_ALLY", EquipmentLoadoutState.Empty()));
                guildUnions.Add(GuildUnion("PU_ALLY", new[] { "RECRUIT_ALLY" }));
            }
            else
            {
                var hp = (relicId == RelicIds[2] || relicId == RelicIds[4]) && validBattleStateGate
                    ? 50
                    : 100;
                var formation = relicId == RelicIds[3] && validBattleStateGate ? 5000 : 9000;
                var cohesion = 90;
                var engagement = relicId == RelicIds[0] && !validBattleStateGate
                    ? EngagementState.Open
                    : relicId == RelicIds[3] && validBattleStateGate
                        ? EngagementState.Broken
                        : EngagementState.Engaged;
                target = Enemy("EU_TARGET", hp, formation, cohesion, engagement);
            }

            if (relicId == RelicIds[1] && !validBattleStateGate)
                source = source.With(members: new[] { Member("RECRUIT_SOURCE", rule, 100) });

            var enemies = relicId == RelicIds[5]
                ? new[] { Enemy("EU_TARGET", 100, 9000, 90, EngagementState.Engaged) }
                : new[] { target };
            var battle = Battle(Math.Max(3, rule.MinimumRound), players.Select(value =>
                value.UnionId == source.UnionId ? source : value).ToArray(), enemies);

            var inventory = new List<EquipmentItemState>();
            if (!equipSeal) inventory.Add(seal);
            if (includeGenericArt007)
                inventory.Add(new EquipmentItemState(
                    "GENERIC_ART007", "RELIC001_ART_007", "Generic sealed ART 007",
                    new[] { EquipmentSlotIds.ToolRelic }, new[] { "SPECIAL_RELIC_DATA_ONLY" },
                    "EPIC", 10000, false));
            var guild = new GuildState(
                "GUILD_SPECIAL_RELIC_COMBAT_001", 0, recruits, guildUnions, inventory);
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000000901", 901, "TEST",
                ModeRuleSnapshot.StandardDefaults(), guild).WithBattle(battle);
            source = battle.PlayerUnions.Single(value => value.UnionId == "PU_SOURCE");
            target = relicId == RelicIds[5]
                ? battle.PlayerUnions.Single(value => value.UnionId == "PU_ALLY")
                : relicId == RelicIds[1]
                    ? source
                    : battle.EnemyUnions[0];
            var generationIdentity = ExpectedBaseIdentity(campaign, battle, source, 0);
            var forecast = Forecast(rule, source, target, generationIdentity);
            return new Scenario(rule, seal, campaign, battle, source, target, forecast);
        }

        private static CampaignState WithInventory(Scenario scenario, IReadOnlyList<EquipmentItemState> inventory)
        {
            var guild = new GuildState(
                scenario.Campaign.Guild.GuildId,
                scenario.Campaign.Guild.TreasuryXp,
                scenario.Campaign.Guild.Recruits,
                scenario.Campaign.Guild.Unions,
                inventory,
                scenario.Campaign.Guild.Development,
                scenario.Campaign.Guild.GuildCity);
            return scenario.Campaign.With(guild, scenario.Campaign.OpeningFlow);
        }

        private static RecruitState Recruit(string id, EquipmentLoadoutState equipment) =>
            new RecruitState(
                id, 100, 100, 50, 50, id, RecruitOriginKind.Procedural,
                string.Empty, "HUMAN", "TEST_WORLD", "TEST_CLASS", "Observed", 7000,
                RecruitAuthorityKind.Normal, string.Empty, string.Empty, equipment, true);

        private static EquipmentLoadoutState Loadout(EquipmentItemState item) =>
            new EquipmentLoadoutState(new[]
            {
                new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic, item)
            });

        private static UnionState GuildUnion(string id, IReadOnlyList<string> members) =>
            new UnionState(id, id, UnionKind.Normal, members[0], members,
                "FORMATION_TEST", "DOCTRINE_TEST", 40, 9000);

        private static BattleMemberState Member(
            string id,
            SpecialRelicUltimateArtRule001 rule,
            int hp = 100) =>
            new BattleMemberState(
                id, id, "TEST_CLASS", hp, 100, 50, 50, 20, 20,
                new[] { "TEST_EQUIPMENT" }, false, false, false,
                new[] { rule.UnderlyingArtId }, 30, 0, string.Empty,
                new[]
                {
                    new BattleArtProgressState(
                        rule.UnderlyingArtId, "TEST", rule.MinimumMeaningfulUses, 5)
                });

        private static BattleMemberState PlainMember(string id, int hp) =>
            new BattleMemberState(
                id, id, "TEST_CLASS", hp, 100, 50, 50, 20, 20,
                Array.Empty<string>(), false, false, false,
                Array.Empty<string>(), 0, 0, string.Empty);

        private static BattleUnionState Union(
            string id,
            BattleSide side,
            IReadOnlyList<BattleMemberState> members,
            int formation,
            int cohesion,
            int uses,
            int ap,
            EngagementState engagement) =>
            new BattleUnionState(
                id, id, side, members[0].MemberId, members,
                "FORMATION_TEST", "Formation Test", true, string.Empty,
                ap, 60, cohesion, formation, engagement, false, false, uses);

        private static BattleUnionState Enemy(
            string id,
            int hp,
            int formation,
            int cohesion,
            EngagementState engagement) =>
            Union(id, BattleSide.Enemy, new[] { PlainMember(id + "_M", hp) },
                formation, cohesion, 0, 30, engagement);

        private static BattleState Battle(
            int round,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies) =>
            new BattleState(
                "BATTLE_SPECIAL_RELIC_001", "CONTENT_TEST_001", round,
                BattlePhase.ForecastSelection, BattleOutcome.InProgress, "Test Ultimate Arts",
                players, enemies, Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(), Array.Empty<BattleEventState>(),
                Array.Empty<BattleRoundRecordState>(), "FORECAST_BASIS_001",
                "INITIAL_HASH_001", string.Empty, string.Empty, string.Empty, false);

        private static BattleForecastState Forecast(
            SpecialRelicUltimateArtRule001 rule,
            BattleUnionState source,
            BattleUnionState target,
            string generationIdentity)
        {
            var action = Action(rule, source, target);
            return new BattleForecastState(
                "BASE_FORECAST_001", source.UnionId, rule.PreferredCommandId,
                "Base command", "Base phrase", "Base intent", target.UnionId,
                target.DisplayName, new[] { action }, action.SharedApCost, 0,
                action.PersonalMpCost, "Base effect", "Base risk", string.Empty,
                "Base fallback", generationIdentity, "BASE_DEBUG_001");
        }

        private static BattlePlannedActionState Action(
            SpecialRelicUltimateArtRule001 rule,
            BattleUnionState source,
            BattleUnionState target,
            int? sharedAp = null) =>
            new BattlePlannedActionState(
                source.Members[0].MemberId, source.Members[0].DisplayName,
                target.UnionId, target.Members[0].MemberId,
                rule.UnderlyingArtId, rule.UltimateArtName,
                Kind(rule), sharedAp ?? rule.RequiredActionSharedApCost,
                rule.RequiredActionPersonalMpCost, -20, 0, 0,
                "Exact underlying Art", true, false, "ULTIMATE_TEST", rule.EffectRole, 5);

        private static BattleActionKind Kind(SpecialRelicUltimateArtRule001 rule)
        {
            switch (rule.EffectRole)
            {
                case "MYSTIC": return BattleActionKind.Mystic;
                case "RESTORATION": return BattleActionKind.Restoration;
                case "GUARD": return BattleActionKind.Guard;
                case "TACTICAL": return BattleActionKind.Tactical;
                default: return BattleActionKind.Martial;
            }
        }

        private static string ExpectedBaseIdentity(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState union,
            int slot) =>
            CanonicalJson.Sha256Hex(new
            {
                CampaignSeed = campaign.CampaignSeed,
                battle.BattleId,
                battle.Round,
                union.UnionId,
                ForecastSlot = slot,
                CurrentAuthoritativeStateHash = battle.ForecastStateBasisHash,
                battle.ContentVersion
            });

        private static BattleState WithSource(BattleState battle, BattleUnionState source)
        {
            var players = battle.PlayerUnions.ToList();
            var index = players.FindIndex(value => value.UnionId == source.UnionId);
            players[index] = source;
            return battle.With(playerUnions: players);
        }

        private static BattleForecastState CopyForecast(
            BattleForecastState source,
            string commandId = null,
            IReadOnlyList<BattlePlannedActionState> actions = null,
            int? sharedApCost = null,
            string learning = null,
            string generationIdentity = null) =>
            new BattleForecastState(
                source.ForecastId, source.UnionId, commandId ?? source.CommandId,
                source.CommandName, source.Phrase, source.TacticalIntent,
                source.TargetId, source.TargetName, actions ?? source.MemberActions,
                sharedApCost ?? source.SharedApCost, source.ApRecovery,
                actions == null ? source.CombinedMpCost : actions.Sum(value => value.PersonalMpCost),
                source.ExpectedEffect, source.Risk, learning ?? source.LearningOpportunity,
                source.FallbackBehavior, generationIdentity ?? source.GenerationIdentity,
                source.DeterministicDebugEvidence);

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
                progression022: state, replaceProgression022: true,
                lastCheckpointId: state.LastCheckpointId);
            progress = progress.With(
                playable020: playable, replacePlayable020: true,
                lastCheckpointId: state.LastCheckpointId);
            strategic = strategic.With(
                campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: state.LastCheckpointId);
            city = city.With(
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: state.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private sealed class Scenario
        {
            public Scenario(
                SpecialRelicUltimateArtRule001 rule,
                EquipmentItemState seal,
                CampaignState campaign,
                BattleState battle,
                BattleUnionState source,
                BattleUnionState target,
                BattleForecastState baseForecast)
            {
                Rule = rule;
                Seal = seal;
                Campaign = campaign;
                Battle = battle;
                Source = source;
                Target = target;
                BaseForecast = baseForecast;
            }

            public SpecialRelicUltimateArtRule001 Rule { get; }
            public EquipmentItemState Seal { get; }
            public CampaignState Campaign { get; }
            public BattleState Battle { get; }
            public BattleUnionState Source { get; }
            public BattleUnionState Target { get; }
            public BattleForecastState BaseForecast { get; }
        }

        private sealed class Applied
        {
            public Applied(
                Scenario scenario,
                CampaignState campaign,
                SpecialRelicUltimateArtApplicationProof001 proof)
            {
                Scenario = scenario;
                Campaign = campaign;
                Proof = proof;
            }

            public Scenario Scenario { get; }
            public CampaignState Campaign { get; }
            public SpecialRelicUltimateArtApplicationProof001 Proof { get; }
        }

        private sealed class MultiScenario
        {
            public MultiScenario(
                CampaignState campaign,
                BattleState battle,
                BattleUnionState source,
                BattleUnionState target,
                BattleForecastState forecast)
            {
                Campaign = campaign;
                Battle = battle;
                Source = source;
                Target = target;
                Forecast = forecast;
            }

            public CampaignState Campaign { get; }
            public BattleState Battle { get; }
            public BattleUnionState Source { get; }
            public BattleUnionState Target { get; }
            public BattleForecastState Forecast { get; }
        }

        private sealed class CrossCommandScenario
        {
            public CrossCommandScenario(
                CampaignState campaign,
                BattleState battle,
                BattleUnionState source,
                BattleForecastState allOut,
                BattleForecastState guard)
            {
                Campaign = campaign;
                Battle = battle;
                Source = source;
                AllOut = allOut;
                Guard = guard;
            }

            public CampaignState Campaign { get; }
            public BattleState Battle { get; }
            public BattleUnionState Source { get; }
            public BattleForecastState AllOut { get; }
            public BattleForecastState Guard { get; }
        }
    }
}
