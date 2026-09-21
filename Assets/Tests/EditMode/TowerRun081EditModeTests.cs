#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerRun081EditModeTests
    {
        const string Campaign083FloorOneArtSha256 =
            "0EEC14FC782F262B98578BEF3B41C17A3A585400DCBB20A6E374F1D28582E633";
        const string PreservedLegacyFloorOneArtSha256 =
            "2B708EEBE18731EF5004E4EE81ED9459213EC84672855A749C7BCC14D76A0BBE";

        [Test]
        public void PublicCoordinatorContractExposesTheThreeSimpleTowerCommands()
        {
            var contract = typeof(ICampaignProgressionPresentationCoordinator022);

            Assert.That(contract.GetMethod("BeginTowerRun081"), Is.Not.Null);
            Assert.That(contract.GetMethod("AdvanceTowerRun081"), Is.Not.Null);
            Assert.That(contract.GetMethod("RetreatTowerRun081"), Is.Not.Null);

            var coordinator = typeof(M1RuntimeCoordinator);
            Assert.That(coordinator.GetMethod("BeginTowerRun081"), Is.Not.Null);
            Assert.That(coordinator.GetMethod("AdvanceTowerRun081"), Is.Not.Null);
            Assert.That(coordinator.GetMethod("RetreatTowerRun081"), Is.Not.Null);
            Assert.That(
                coordinator.GetMethod(
                    "AdvanceAutomaticTowerSteps081",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Null,
                "One Tower click must reveal exactly one authored room; automatic room compression would skip the board-game play.");
        }

        [Test]
        public void FloorOneUsesExactCampaign083BattlefieldAndPreservesLegacyPlate()
        {
            const string legacyResourcePath =
                "SecondDimension/Campaign022/UI/Abyss/Floor_01_Mud_Trenches";
            var generatedAssetPath = Path.Combine(
                Application.dataPath,
                "Resources",
                "SecondDimension",
                "Art",
                "Campaign083",
                TowerRunRules081.FloorOneCampaign083BattleArtTextureName + ".png");
            var legacyAssetPath = Path.Combine(
                Application.dataPath,
                "Resources",
                "SecondDimension",
                "Campaign022",
                "UI",
                "Abyss",
                "Floor_01_Mud_Trenches.jpg");

            Assert.That(
                TowerRunRules081.ArtResourcePath(1),
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtResourcePath));
            Assert.That(
                TowerRunRules081.ArtResourcePath(1),
                Is.Not.EqualTo(legacyResourcePath),
                "Floor 1 must not route players back to the schematic legacy plate.");
            Assert.That(File.Exists(generatedAssetPath), Is.True, generatedAssetPath);
            Assert.That(FileSha256(generatedAssetPath), Is.EqualTo(Campaign083FloorOneArtSha256));
            Assert.That(File.Exists(legacyAssetPath), Is.True, legacyAssetPath);
            Assert.That(FileSha256(legacyAssetPath), Is.EqualTo(PreservedLegacyFloorOneArtSha256),
                "The original Floor 1 plate must remain byte-for-byte preserved.");

            var generated = Resources.Load<Texture2D>(
                TowerRunRules081.FloorOneCampaign083BattleArtResourcePath);
            var legacy = Resources.Load<Texture2D>(legacyResourcePath);
            Assert.That(generated, Is.Not.Null);
            Assert.That(generated.name,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtTextureName));
            Assert.That(generated.width, Is.EqualTo(1672));
            Assert.That(generated.height, Is.EqualTo(941));
            Assert.That(legacy, Is.Not.Null, "Preserving the source plate also keeps it importable.");
            Assert.That(generated, Is.Not.SameAs(legacy));
        }

        [Test]
        public void CriticalTowerAndForecastTextPairsMeetAaContrast()
        {
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.FirstClimbHeadingColor083,
                    TowerRunRules081.TowerRibbonSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.LightSurfaceInkColor083,
                    TowerRunRules081.LightActionSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.LightSurfaceInkColor083,
                    TowerRunRules081.LightActionDisabledSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.LightSurfaceInkColor083,
                    TowerRunRules081.FirstClimbHeadingColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.ForecastTextColor083,
                    TowerRunRules081.ForecastSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
        }

        [Test]
        public void ShippingTowerVisualSourcesRejectSchematicPlaceholderCopy()
        {
            var relativeSources = new[]
            {
                Path.Combine("SecondDimension", "Presentation", "Campaign022", "GuildCityFlowPresenter022.cs"),
                Path.Combine("SecondDimension", "Presentation", "Battle", "M1FlowPresenter.Invocation022.cs"),
                Path.Combine("SecondDimension", "Presentation", "Battle", "M2FullScreenUnionCommandStage.cs"),
                Path.Combine("SecondDimension", "Presentation", "Battle3D", "M2Battle3DWorld.cs")
            };
            var combined = string.Join("\n", relativeSources.Select(relative =>
                File.ReadAllText(Path.Combine(Application.dataPath, relative))));
            var contractsSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "Campaign022",
                "Campaign022PresentationContracts.cs"));

            Assert.That(TowerRunRules081.ContainsForbiddenPlaceholderCopy083(combined), Is.False);
            Assert.That(contractsSource,
                Does.Contain("FloorOneCampaign083BattleArtResourcePath"),
                "The shipping Tower sources must bind the versioned generated resource.");
            Assert.That(combined,
                Does.Contain("ApplyLightSurfaceForecastContrast083"),
                "The Echo/Forecast light-surface ink guard must remain wired.");
            Assert.That(combined,
                Does.Contain("ActiveBackdropResourceKey083"),
                "The perspective battlefield must expose its exact active backdrop for smoke proof.");
        }

        [Test]
        public void FirstTenFloorsSelectGuardianAndPostTenRunsSelectFloorTenTrial()
        {
            var registry = CampaignRegistry022.LoadFromResources();

            for (var highestCleared = 0; highestCleared < TowerRunRules081.OpeningFloorCount; highestCleared++)
            {
                var floorNumber = TowerRunRules081.NextFloorNumber(highestCleared);
                var operationId = TowerRunRules081.OperationId(floorNumber, true);

                Assert.That(floorNumber, Is.EqualTo(highestCleared + 1));
                Assert.That(operationId, Is.EqualTo($"ABYSS_OP022_{floorNumber:00}_GUARDIAN"));
                Assert.That(registry.AbyssOperations.ContainsKey(operationId), Is.True);

                var operation = registry.AbyssOperations[operationId];
                Assert.That(operation.kind, Is.EqualTo("GUARDIAN"));
                Assert.That(operation.firstClearOnly, Is.True);
                Assert.That(registry.Floors[operation.floorId].floor, Is.EqualTo(floorNumber));
            }

            var repeatId = TowerRunRules081.OperationId(
                TowerRunRules081.NextFloorNumber(TowerRunRules081.OpeningFloorCount),
                false);
            Assert.That(repeatId, Is.EqualTo("ABYSS_OP022_10_TRIAL"));
            Assert.That(registry.AbyssOperations[repeatId].kind, Is.EqualTo("TRIAL"));
            Assert.That(registry.AbyssOperations[repeatId].firstClearOnly, Is.False);
            Assert.That(TowerRunRules081.RunNumber(10, 1), Is.EqualTo(2));
            Assert.That(TowerRunRules081.RunNumber(10, 2), Is.EqualTo(3));
        }

        [Test]
        public void SelectedTowerOperationsHaveOneBattleGateAndCompressibleBookends()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var selected = Enumerable.Range(1, TowerRunRules081.OpeningFloorCount)
                .Select(floor => TowerRunRules081.OperationId(floor, true))
                .Concat(new[] { TowerRunRules081.OperationId(10, false) })
                .Select(id => registry.AbyssOperations[id])
                .ToArray();

            foreach (var operation in selected)
            {
                Assert.That(operation.steps, Has.Length.EqualTo(5), operation.operationId);
                Assert.That(operation.steps.Count(step => step.requiresBattle), Is.EqualTo(1), operation.operationId);
                Assert.That(operation.steps[3].requiresBattle, Is.True, operation.operationId);
                Assert.That(operation.steps.Take(3).All(step => !step.requiresBattle), Is.True, operation.operationId);
                Assert.That(operation.steps[4].requiresBattle, Is.False, operation.operationId);
                Assert.That(operation.steps.All(step => step.exactOnce), Is.True, operation.operationId);
                Assert.That(operation.exactOnceReceipts, Is.True, operation.operationId);
            }
        }

        [Test]
        public void EnemyUnionScalingIsMonotonicRepeatSensitiveAndCappedAtTen()
        {
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(1, 0), Is.EqualTo(1));
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(2, 0), Is.EqualTo(1));
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(3, 0), Is.EqualTo(2));
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, 0), Is.EqualTo(5));
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, 1), Is.EqualTo(6));
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, 99), Is.EqualTo(10));

            var firstClearCounts = Enumerable.Range(1, 10)
                .Select(floor => CampaignProgressionCommandService022.TowerEnemyUnionCount081(floor, 0))
                .ToArray();
            for (var index = 1; index < firstClearCounts.Length; index++)
                Assert.That(firstClearCounts[index], Is.GreaterThanOrEqualTo(firstClearCounts[index - 1]));

            var repeatCounts = Enumerable.Range(0, 10)
                .Select(clearCount => CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, clearCount))
                .ToArray();
            for (var index = 1; index < repeatCounts.Length; index++)
                Assert.That(repeatCounts[index], Is.GreaterThanOrEqualTo(repeatCounts[index - 1]));
            Assert.That(repeatCounts.All(value => value >= 1 && value <= 10), Is.True);
        }

        [Test]
        public void TenTowerThreatTiersHaveDeterministicCertifiedPowerScaling089()
        {
            for (var floor = 1; floor <= 10; floor++)
            {
                Assert.That(
                    CampaignProgressionCommandService022.TowerThreatTier089(floor),
                    Is.EqualTo(floor));
                Assert.That(
                    CampaignProgressionCommandService022.TowerThreatModifier089(floor),
                    Is.EqualTo("TOWER_THREAT_TIER_" + floor.ToString("00")));
            }
            Assert.That(
                CampaignProgressionCommandService022.TowerThreatHpBasisPoints089(1),
                Is.Zero);
            Assert.That(
                CampaignProgressionCommandService022.TowerThreatHpBasisPoints089(10),
                Is.EqualTo(4500));
            Assert.That(
                CampaignProgressionCommandService022.TowerThreatOffenseBasisPoints089(10),
                Is.EqualTo(2700));

            var baseMember = new BattleMemberState(
                "TOWER_THREAT_MEMBER_089", "Tower Threat", "ENEMY",
                100, 100, 20, 20, 100, 80, Array.Empty<string>(),
                false, false, false, Array.Empty<string>(), 0, 0,
                string.Empty);
            var baseUnion = new BattleUnionState(
                "TOWER_THREAT_UNION_089", "Tower Threat Union",
                BattleSide.Enemy, baseMember.MemberId, new[] { baseMember },
                "FORMATION_TOWER_089", "Tower", true, string.Empty,
                12, 12, 70, 10000, EngagementState.Open, false, false, 0);
            var scaler = typeof(M2BattleCommandService).GetMethod(
                "ApplyTowerThreatPower089",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(scaler, Is.Not.Null,
                "Tower power must remain inside the certified battle engine.");
            var apex = (BattleUnionState)scaler.Invoke(null,
                new object[] { baseUnion, 10 });
            Assert.That(apex.Members[0].MaximumHp, Is.EqualTo(145));
            Assert.That(apex.Members[0].Attack, Is.EqualTo(127));
            Assert.That(apex.Members[0].MagicAttack, Is.EqualTo(102));
            Assert.That(apex.MaximumAp, Is.EqualTo(15));
            Assert.That(apex.Cohesion, Is.EqualTo(79));
        }

        [Test]
        public void EveryFiftiethTowerClearBanksOneSavedSRankRecruitLead089()
        {
            Assert.That(CampaignProgressionCommandService022
                .IsTowerMajorRecruitMilestone089(49), Is.False);
            Assert.That(CampaignProgressionCommandService022
                .IsTowerMajorRecruitMilestone089(50), Is.True);
            Assert.That(CampaignProgressionCommandService022
                .IsTowerMajorRecruitMilestone089(100), Is.True);
            Assert.That(CampaignProgressionCommandService022
                .TowerMajorRecruitStableId089(100,
                    new[] { "HERO_REC_011" }), Is.EqualTo("HERO_REC_014"));

            var registry = CampaignRegistry022.LoadFromResources();
            var service = new CampaignProgressionCommandService022();
            var campaign = CreateTowerCampaign();
            for (var clear = 1; clear < 50; clear++)
                campaign = CompleteNextTowerFloor089(
                    campaign, service, registry, clear);

            Assert.That(Progression(campaign).AbyssFloors.Sum(x => x.ClearCount),
                Is.EqualTo(49));
            Assert.That(Progression(campaign).AppliedReceiptIds.Any(id =>
                id.StartsWith("TOWERRECRUIT089_", StringComparison.Ordinal)),
                Is.False);
            Assert.That(campaign.Guild.GuildCity.Strategic017H.Campaign019
                .Playable020.WorldGate023.ExpeditionRecruitLeadIds089,
                Is.Empty);

            var committed = CommitNextTowerCompletion089(
                campaign, service, registry, 50);
            var first = Require(service.ApplyAbyssOperationCompletion(
                committed, registry));
            var replayFromSameCommittedSave = Require(
                service.ApplyAbyssOperationCompletion(committed, registry));
            Assert.That(JsonConvert.SerializeObject(replayFromSameCommittedSave),
                Is.EqualTo(JsonConvert.SerializeObject(first)),
                "Replaying the same committed save must produce the same single receipt.");

            var milestoneReceipts = Progression(first).AppliedReceiptIds.Where(id =>
                id.StartsWith("TOWERRECRUIT089_", StringComparison.Ordinal)).ToArray();
            Assert.That(milestoneReceipts, Has.Length.EqualTo(1));
            Assert.That(first.Guild.Development.AppliedAdventureAuthorityIds.Count(id =>
                StringComparer.Ordinal.Equals(id, milestoneReceipts[0])),
                Is.EqualTo(1));
            var lead = first.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023.ExpeditionRecruitLeadIds089.Single();
            Assert.That(lead, Is.EqualTo("HERO_REC_011"));
            Assert.That(Progression(first).ActiveAbyssOperation, Is.Null,
                "The milestone stays battle → reward → next battle; no card operation is inserted.");

            var catalogSource = Resources.Load<TextAsset>(
                "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300");
            Assert.That(catalogSource, Is.Not.Null);
            var hero = HeroMaster300Catalog087.FromJson(catalogSource.text)
                .AcceptedHeroes.Single(value =>
                    StringComparer.Ordinal.Equals(value.StableId, lead));
            Assert.That(hero.Rank, Is.EqualTo(HeroMasterRank087.S));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(first));
            Assert.That(reloaded, Is.Not.Null);
            Assert.That(reloaded.Guild.GuildCity.Strategic017H.Campaign019
                .Playable020.WorldGate023.ExpeditionRecruitLeadIds089,
                Is.EquivalentTo(new[] { lead }));
            Assert.That(Progression(reloaded).AppliedReceiptIds.Count(id =>
                id.StartsWith("TOWERRECRUIT089_", StringComparison.Ordinal)),
                Is.EqualTo(1));
            Assert.That(service.BeginAbyssOperation(reloaded, registry,
                    TowerRunRules081.OperationId(10, false)).IsSuccess,
                Is.True,
                "The saved milestone receipt must coexist with the canonical Tower authority chain and unlock the next battle.");
        }

        [Test]
        public void EveryNewTowerBattleIdCarriesItsFloorAndResolvesThatAuthoredPlate()
        {
            const string hashSuffix = "0123456789ABCDEF01234567";

            for (var floor = 1; floor <= TowerRunRules081.OpeningFloorCount; floor++)
            {
                var battleId = CampaignProgressionCommandService022.TowerBattleId081(floor, hashSuffix);
                var expectedPath = TowerRunRules081.ArtResourcePath(floor);

                Assert.That(
                    battleId,
                    Does.StartWith($"ABYSS_BATTLE022_FLOOR_{floor:00}_"),
                    "The dynamic ID must retain enough presentation identity to recover the floor.");
                Assert.That(
                    M1VisualAssets.TryResolveTowerBattleBackdropResourceKey081(
                        battleId,
                        out var selectedPath),
                    Is.True,
                    battleId);
                Assert.That(selectedPath, Is.EqualTo(expectedPath), battleId);
                Assert.That(
                    M1VisualAssets.TryResolveBattleBackdrop(battleId, out var sprite, out var loadedPath),
                    Is.True,
                    expectedPath);
                Assert.That(sprite, Is.Not.Null, expectedPath);
                Assert.That(loadedPath, Is.EqualTo(expectedPath), battleId);
            }
        }

        [Test]
        public void LegacyOpaqueTowerBattleIdsUseTowerArtWithoutClaimingAFloorIdentity()
        {
            const string legacyId = "ABYSS_BATTLE022_0123456789ABCDEF01234567";
            var safeLegacyPath = TowerRunRules081.ArtResourcePath(1);

            Assert.That(
                M1VisualAssets.TryResolveTowerBattleBackdropResourceKey081(
                    legacyId,
                    out var selectedPath),
                Is.True);
            Assert.That(selectedPath, Is.EqualTo(safeLegacyPath));
            Assert.That(
                M1VisualAssets.TryResolveBattleBackdrop(legacyId, out var sprite, out var loadedPath),
                Is.True);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(loadedPath, Is.EqualTo(safeLegacyPath));

            Assert.That(
                M1VisualAssets.TryResolveTowerBattleBackdropResourceKey081(
                    "BATTLE_GATEHOUSE_BOSS",
                    out var unrelatedPath),
                Is.False);
            Assert.That(unrelatedPath, Is.Empty);
        }

        [Test]
        public void EverySelectedFightAdvertisesPositiveImmediateRewards()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var selectedIds = Enumerable.Range(1, TowerRunRules081.OpeningFloorCount)
                .Select(floor => TowerRunRules081.OperationId(floor, true))
                .Concat(new[] { TowerRunRules081.OperationId(10, false) });

            foreach (var operationId in selectedIds)
            {
                var operation = registry.AbyssOperations[operationId];
                Assert.That(operation.guildXp, Is.GreaterThan(0), operationId);
                Assert.That(operation.hallXp, Is.GreaterThan(0), operationId);
                Assert.That(operation.rewardMaterialIds, Is.Not.Null.And.Not.Empty, operationId);
                Assert.That(
                    operation.rewardMaterialIds.Distinct(StringComparer.Ordinal).Count(),
                    Is.EqualTo(operation.rewardMaterialIds.Length),
                    operationId);
                Assert.That(operation.existingEquipmentRewardRemainsAuthoritative, Is.True, operationId);
            }
        }

        [Test]
        public void ActiveTowerBattleIdentityRejectsForeignDefeatAndInProgressRetreat()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var service = new CampaignProgressionCommandService022();
            var committed = CommitFloorOneTowerEncounter(service, registry);
            var request = committed.Guild.GuildCity.PendingEncounter;
            Assert.That(request.BattleId, Does.StartWith("ABYSS_BATTLE022_FLOOR_01_"));
            var matching = WithBattle(committed, request.BattleId, BattlePhase.ForecastSelection, BattleOutcome.InProgress, null);

            Assert.That(service.HasMatchingActiveAbyssBattle(matching, registry), Is.True);
            var rejected = service.RetreatAbyssOperation(matching, registry);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors, Does.Contain("CAMPAIGN022_ACTIVE_BATTLE_MUST_FINISH"));

            var foreignDefeat = WithBattle(
                committed,
                request.BattleId + "_FOREIGN",
                BattlePhase.Resolved,
                BattleOutcome.Defeat,
                DefeatReward("TOWER081_FOREIGN_DEFEAT"));
            Assert.That(service.HasMatchingActiveAbyssBattle(foreignDefeat, registry), Is.False,
                "A resolved defeat may only drive Tower retreat/reward handling when its exact BattleId belongs to the active operation.");
        }

        [Test]
        public void ExactTowerIdentitySurvivesClaimedReturnWithoutPrefixFallback()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var service = new CampaignProgressionCommandService022();
            var committed = CommitFloorOneTowerEncounter(service, registry);
            var request = committed.Guild.GuildCity.PendingEncounter;
            var claimed = WithClaimedVictory(committed, request, "TOWER081_CLAIMED_RETURN");
            var returnCommitted = Require(service.CommitAbyssBattleReturn(claimed, registry));
            var returned = Require(service.ApplyAbyssBattleReturnExactlyOnce(returnCommitted, registry));

            Assert.That(returned.Guild.GuildCity.PendingEncounter, Is.Null);
            Assert.That(service.HasMatchingActiveAbyssBattle(returned, registry), Is.True,
                "The applied exact return receipt must retain Tower identity after PendingEncounter is cleared.");

            var foreign = WithBattle(
                returned,
                "ABYSS_BATTLE022_FOREIGN_PREFIX_ONLY",
                BattlePhase.Resolved,
                BattleOutcome.Victory,
                returned.Battle.Reward);
            Assert.That(service.HasMatchingActiveAbyssBattle(foreign, registry), Is.False,
                "An ABYSS_BATTLE022_ prefix alone must never identify the active Tower battle.");
        }

        static CampaignState CommitFloorOneTowerEncounter(
            CampaignProgressionCommandService022 service,
            CampaignRegistry022 registry)
        {
            var campaign = Require(service.BeginAbyssOperation(
                CreateTowerCampaign(), registry, "ABYSS_OP022_01_GUARDIAN"));
            var operation = registry.AbyssOperations["ABYSS_OP022_01_GUARDIAN"];
            while (!operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle)
            {
                campaign = Require(service.CommitAbyssStep(campaign, registry, "SUCCESS"));
                campaign = Require(service.ApplyAbyssStep(campaign, registry));
            }
            return Require(service.CommitAbyssBattleEncounter(campaign, registry));
        }

        static CampaignState CompleteNextTowerFloor089(
            CampaignState campaign,
            CampaignProgressionCommandService022 service,
            CampaignRegistry022 registry,
            int rewardOrdinal)
        {
            var committed = CommitNextTowerCompletion089(
                campaign, service, registry, rewardOrdinal);
            return Require(service.ApplyAbyssOperationCompletion(
                committed, registry));
        }

        static CampaignState CommitNextTowerCompletion089(
            CampaignState campaign,
            CampaignProgressionCommandService022 service,
            CampaignRegistry022 registry,
            int rewardOrdinal)
        {
            var highest = Progression(campaign).AbyssFloors
                .Where(value => value.ClearCount > 0 &&
                                registry.Floors.ContainsKey(value.FloorId))
                .Select(value => registry.Floors[value.FloorId].floor)
                .DefaultIfEmpty(0).Max();
            var operationId = highest < 10
                ? TowerRunRules081.OperationId(highest + 1, true)
                : TowerRunRules081.OperationId(10, false);
            campaign = Require(service.BeginAbyssOperation(
                campaign, registry, operationId));
            var operation = registry.AbyssOperations[operationId];
            while (Progression(campaign).ActiveAbyssOperation.Status ==
                   AbyssOperationStatus022.Active &&
                   !operation.steps[Progression(campaign).ActiveAbyssOperation
                       .CurrentStepIndex].requiresBattle)
            {
                campaign = Require(service.CommitAbyssStep(
                    campaign, registry, "SUCCESS"));
                campaign = Require(service.ApplyAbyssStep(campaign, registry));
            }

            campaign = Require(service.CommitAbyssBattleEncounter(
                campaign, registry));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(request.RouteModifiers,
                Does.Contain(CampaignProgressionCommandService022
                    .TowerThreatModifier089(
                        CampaignProgressionCommandService022
                            .TowerThreatTier089(
                                registry.Floors[operation.floorId].floor))));
            campaign = WithClaimedVictory(
                campaign, request, "TOWER081_REWARD_" + rewardOrdinal);
            campaign = Require(service.CommitAbyssBattleReturn(
                campaign, registry));
            campaign = Require(service.ApplyAbyssBattleReturnExactlyOnce(
                campaign, registry));
            campaign = Require(service.CommitAbyssBattleResult(
                campaign, registry));
            campaign = Require(service.ApplyAbyssBattleAndFinalize(
                campaign, registry));

            while (Progression(campaign).ActiveAbyssOperation.Status ==
                   AbyssOperationStatus022.Active)
            {
                campaign = Require(service.CommitAbyssStep(
                    campaign, registry, "SUCCESS"));
                campaign = Require(service.ApplyAbyssStep(campaign, registry));
            }
            return Require(service.CommitAbyssOperationCompletion(
                campaign, registry));
        }

        static CampaignState CreateTowerCampaign()
        {
            const string recruitId = "RECRUIT_TOWER_081_TEST";
            const string unionId = "UNION_TOWER_081_TEST";
            var weapon = new EquipmentItemState(
                "TOWER_WEAPON_081_TEST", "TOWER_SWORD_081_TEST", "Tower Test Sword",
                new[] { EquipmentSlotIds.MainHand }, new[] { "SWORD", "WEAPON" },
                "QUALITY_STANDARD", 10000, false);
            var recruit = new RecruitState(
                recruitId, 150, 150, 40, 40, "Tower Tester", RecruitOriginKind.Procedural,
                string.Empty, "HUMAN", "SKYHOME", "CLASS_TEND_GUARDIAN", "Observed", 7000,
                RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                new EquipmentLoadoutState(new[] { new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, weapon) }),
                true, string.Empty, string.Empty, 60, 60);
            var union = new UnionState(
                unionId, "Tower Test Union", UnionKind.Normal, recruitId, new[] { recruitId },
                "FORMATION_SHIELD_WALL", "DOCTRINE_BALANCED", 18, 8500);
            var guild = new GuildState("GUILD_TOWER_081_TEST", 0, new[] { recruit }, new[] { union }, Array.Empty<EquipmentItemState>());
            var profile = new NewGuildProfileState(
                "Tower Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(
                OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true, null, false,
                439, 0, true, true, true, false, "tower_081_test_ready");
            var campaign = new CampaignState(
                "00000000-0000-0000-0000-000000000281", 22081, "1.0",
                ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
            return WithProgression(campaign, CampaignProgressionState022.Default());
        }

        static CampaignState WithClaimedVictory(
            CampaignState campaign,
            EncounterLaunchRequest017D request,
            string rewardId)
        {
            var memberReward = new BattleMemberRewardState(
                "TOWER_MEMBER_081_TEST", "Tower Tester", 1, 1, 1, 0, 0, 0, 0, 0, 0, 0);
            var reward = new BattleRewardState(
                rewardId, "TOWER_TEST_REWARD_RULES", BattleOutcome.Victory,
                1, 1, 1000, 1000, 100, 100, 7, 5, new[] { memberReward }, true);
            var campaignWithBattle = WithBattle(
                campaign, request.BattleId, BattlePhase.Resolved, BattleOutcome.Victory, reward);
            var battle = campaignWithBattle.Battle.With(
                finalStateHash: M2BattleCommandService.AuthoritativeStateHash(campaignWithBattle.Battle));
            var development = campaign.Guild.Development.RecordBattleReward(
                rewardId, reward.GuildTreasuryXpAward, reward.HallEnhancementXpAward);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp + reward.GuildTreasuryXpAward,
                campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow).WithBattle(battle);
        }

        static BattleRewardState DefeatReward(string rewardId) => new BattleRewardState(
            rewardId, "TOWER_TEST_DEFEAT_RULES", BattleOutcome.Defeat,
            1, 1, 1000, 1000, 100, 100, 1, 1,
            new[]
            {
                new BattleMemberRewardState(
                    "TOWER_MEMBER_081_TEST", "Tower Tester", 1, 1, 1, 0, 0, 0, 0, 0, 0, 0)
            },
            false);

        static CampaignState WithBattle(
            CampaignState campaign,
            string battleId,
            BattlePhase phase,
            BattleOutcome outcome,
            BattleRewardState reward)
        {
            var battle = new BattleState(
                battleId, "TOWER_TEST", 1, phase, outcome, "Tower test objective",
                Array.Empty<BattleUnionState>(), Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(), Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(), Array.Empty<BattleRoundRecordState>(),
                "TOWER_FORECAST_HASH_081", "TOWER_INITIAL_HASH_081",
                string.Empty, string.Empty, string.Empty, false, reward);
            return campaign.WithBattle(battle);
        }

        static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        static CampaignState WithProgression(CampaignState campaign, CampaignProgressionState022 state)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                progression022: state, replaceProgression022: true, lastCheckpointId: state.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true, lastCheckpointId: state.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true, lastCheckpointId: state.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true, lastCheckpointId: state.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        static string FileSha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
#endif
