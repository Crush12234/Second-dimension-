#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardAdventure084AuthorityTests
    {
        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignPlayableCommandService020 _campaign020;
        CampaignWorldGateCommandService023 _worldGate023;

        [SetUp]
        public void SetUp()
        {
            _registry020 = CampaignRegistry020.LoadFromResources();
            _catalog020 = new Campaign020RuleCatalogAdapter(_registry020);
            _registry023 = CampaignRegistry023.LoadFromResources();
            _catalog023 = new Campaign023RuleCatalogAdapter(_registry023);
            _campaign020 = new CampaignPlayableCommandService020();
            _worldGate023 = new CampaignWorldGateCommandService023();
        }

        [Test]
        public void AutomaticChoiceIsStableLegalAndBattleSafeAcrossEveryBoardNode084()
        {
            var boardCount = 0;
            var nodeCount = 0;
            var battleCount = 0;
            foreach (var definition in _registry023.Boards.Values)
            {
                Assert.That(_catalog023.TryGetBoard(definition.definitionId,
                    out var board), Is.True, definition.definitionId);
                boardCount++;
                foreach (var node in board.Nodes)
                {
                    nodeCount++;
                    var operationId = "WGOP023_AUTO_TEST_" + definition.definitionId;
                    var first = BoardAdventureRules084.AutomaticChoice084(
                        operationId, node);
                    var repeated = BoardAdventureRules084.AutomaticChoice084(
                        operationId, node);
                    Assert.That(repeated, Is.EqualTo(first), node.NodeId);
                    if (node.RequiresCertifiedBattle)
                    {
                        battleCount++;
                        Assert.That(first, Is.Empty,
                            "Automatic movement must stop at " + node.NodeId);
                        continue;
                    }

                    var ordered = BoardAdventureRules084.OrderedChoices084(
                        operationId, node.NodeId,
                        node.ChoiceIds ?? Array.Empty<string>());
                    var expected = ordered.Count > 0 ? ordered[0] : "CONTINUE";
                    Assert.That(first, Is.EqualTo(expected), node.NodeId);
                    Assert.That(BoardAdventureRules084.TryNextNode084(
                        node, first, out _), Is.True, node.NodeId);
                }
            }
            Assert.That(boardCount, Is.GreaterThan(0));
            Assert.That(nodeCount, Is.GreaterThan(boardCount));
            Assert.That(battleCount, Is.GreaterThan(0));
        }

        [Test]
        public void AutomaticChoiceCoversAuthoredBranchesAndCampConvergence084()
        {
            var camp = _registry023.Boards.Values
                .SelectMany(value =>
                {
                    Assert.That(_catalog023.TryGetBoard(value.definitionId,
                        out var rule), Is.True, value.definitionId);
                    return rule.Nodes;
                })
                .First(node => StringComparer.Ordinal.Equals(node.Kind, "CAMP") &&
                    node.ChoiceIds.Count == 3 && node.NextNodeIds.Count == 2);
            var campRoutes = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < 4096 &&
                 campRoutes.Count < camp.ChoiceIds.Count; index++)
            {
                var choice = BoardAdventureRules084.AutomaticChoice084(
                    "WGOP023_CAMP_BRANCH_" + index, camp);
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    camp, choice, out var destination), Is.True);
                campRoutes[choice] = destination;
            }
            CollectionAssert.AreEquivalent(camp.ChoiceIds, campRoutes.Keys);
            Assert.That(campRoutes["REST"], Is.EqualTo(campRoutes["MENTOR"]));
            Assert.That(campRoutes["SCOUT"], Is.Not.EqualTo(campRoutes["REST"]));

            var branch = _registry023.Boards.Values
                .SelectMany(value =>
                {
                    Assert.That(_catalog023.TryGetBoard(value.definitionId,
                        out var rule), Is.True, value.definitionId);
                    return rule.Nodes;
                })
                .First(node => !node.RequiresCertifiedBattle &&
                    node.ChoiceIds.Count >= 2 &&
                    node.ChoiceIds.Count == node.NextNodeIds.Count);
            var destinations = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < 4096 &&
                 destinations.Count < branch.NextNodeIds.Count; index++)
            {
                var choice = BoardAdventureRules084.AutomaticChoice084(
                    "WGOP023_ROUTE_BRANCH_" + index, branch);
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    branch, choice, out var destination), Is.True);
                destinations.Add(destination);
            }
            CollectionAssert.AreEquivalent(branch.NextNodeIds, destinations);
        }

        [Test]
        public void AutomaticCoordinatorProjectsPendingCheckAndRecapAcrossReload084()
        {
            Assert.That(_catalog023.TryGetBoard("CH018_001", out var board), Is.True);
            var route = RouteToNode084(board, node =>
                !node.RequiresCertifiedBattle && node.CheckDifficulty > 0);
            Assert.That(route, Is.Not.Null,
                "CH018_001 must retain a reachable authored check room.");
            var campaign = CreateAtWorldBoard(
                board.DefinitionId, board.WorldId, 8410841);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                board.DefinitionId, new[] { "BOARD_UNION_084" }, _catalog020));
            foreach (var choice in route)
            {
                campaign = Require(_worldGate023.CommitNodeChoice(campaign,
                    _catalog023, choice, "BOARD_RECRUIT_A_084",
                    "BOARD_RECRUIT_B_084", 0));
                campaign = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                    campaign, _catalog023));
            }
            var operation = WorldGate(campaign).ActiveOperation;
            var checkNode = board.Nodes.Single(value => StringComparer.Ordinal.Equals(
                value.NodeId, operation.CurrentNodeId));
            Assert.That(checkNode.CheckDifficulty, Is.GreaterThan(0));

            var savePath = Path.Combine(Path.GetTempPath(),
                "sd084_auto_room_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var contentRoot = Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT");
                var coordinator = new M1RuntimeCoordinator(contentRoot, savePath);
                var campaignField = typeof(M1RuntimeCoordinator).GetField(
                    "_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(campaignField, Is.Not.Null);
                campaignField.SetValue(coordinator, campaign);

                var expectedChoice = BoardAdventureRules084.AutomaticChoice084(
                    operation.OperationId, checkNode);
                var committed = coordinator.CommitAutomaticWorldGateRoom023();
                Assert.That(committed.Succeeded, Is.True, committed.Message);
                var committedCampaign = (CampaignState)campaignField.GetValue(coordinator);
                var receipt = WorldGate(committedCampaign).ActiveOperation.PendingReceipt;
                Assert.That(receipt, Is.Not.Null);

                var projected = coordinator.CampaignWorldGate023;
                Assert.That(projected.PendingHasCheck, Is.True);
                Assert.That(projected.PendingDieOne, Is.EqualTo(receipt.DieOne));
                Assert.That(projected.PendingDieTwo, Is.EqualTo(receipt.DieTwo));
                Assert.That(projected.PendingModifier, Is.EqualTo(receipt.Modifier));
                Assert.That(projected.PendingTotal, Is.EqualTo(
                    receipt.DieOne + receipt.DieTwo + receipt.Modifier));
                Assert.That(projected.PendingDifficulty,
                    Is.EqualTo(checkNode.CheckDifficulty));
                Assert.That(projected.PendingChoiceLabel, Is.EqualTo(
                    BoardAdventureRules084.ChoiceLabel084(expectedChoice)));

                var reloaded = new M1RuntimeCoordinator(contentRoot, savePath);
                var afterReload = reloaded.CampaignWorldGate023;
                Assert.That(afterReload.PendingHasCheck,
                    Is.EqualTo(projected.PendingHasCheck));
                Assert.That(afterReload.PendingDieOne,
                    Is.EqualTo(projected.PendingDieOne));
                Assert.That(afterReload.PendingDieTwo,
                    Is.EqualTo(projected.PendingDieTwo));
                Assert.That(afterReload.PendingModifier,
                    Is.EqualTo(projected.PendingModifier));
                Assert.That(afterReload.PendingTotal,
                    Is.EqualTo(projected.PendingTotal));
                Assert.That(afterReload.PendingDifficulty,
                    Is.EqualTo(projected.PendingDifficulty));
                Assert.That(afterReload.PendingChoiceLabel,
                    Is.EqualTo(projected.PendingChoiceLabel));

                var applied = reloaded.ApplyWorldGateReceipt023();
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                var recap = reloaded.CampaignWorldGate023;
                Assert.That(recap.PendingReceiptId, Is.Empty);
                Assert.That(recap.HasLastAppliedRoom, Is.True);
                Assert.That(recap.LastAppliedOutcomeTitle, Is.EqualTo(
                    BoardAdventureRules084.OutcomeTitle084(receipt.Outcome)));
                Assert.That(recap.LastAppliedReward, Is.EqualTo(
                    BoardAdventureRules084.ReceiptReward084(receipt)));
                Assert.That(recap.LastAppliedChoiceLabel, Is.EqualTo(
                    BoardAdventureRules084.ChoiceLabel084(receipt.ChoiceId)));
                Assert.That(recap.LastAppliedRoomTitle,
                    Does.Not.Contain(receipt.NodeId));
                Assert.That(recap.LastAppliedChoiceLabel,
                    Does.Not.Contain(receipt.OperationId));

                var recapReloaded = new M1RuntimeCoordinator(
                    contentRoot, savePath).CampaignWorldGate023;
                Assert.That(recapReloaded.HasLastAppliedRoom, Is.True);
                Assert.That(recapReloaded.LastAppliedRoomTitle,
                    Is.EqualTo(recap.LastAppliedRoomTitle));
                Assert.That(recapReloaded.LastAppliedOutcomeTitle,
                    Is.EqualTo(recap.LastAppliedOutcomeTitle));
                Assert.That(recapReloaded.LastAppliedReward,
                    Is.EqualTo(recap.LastAppliedReward));
                Assert.That(recapReloaded.LastAppliedChoiceLabel,
                    Is.EqualTo(recap.LastAppliedChoiceLabel));
            }
            finally
            {
                foreach (var path in new[]
                    { savePath, savePath + ".bak", savePath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void AutomaticCoordinatorCannotBypassOrCommitABattleRoom084()
        {
            var definition = _registry023.Boards.Values.First(value =>
                StringComparer.Ordinal.Equals(value.operationKind, "CRISIS") &&
                StringComparer.Ordinal.Equals(value.worldId, "SKYHOME") &&
                value.usesCertifiedBattle);
            Assert.That(_catalog023.TryGetBoard(
                definition.definitionId, out var board), Is.True);
            var route = RouteToNode084(
                board, node => node.RequiresCertifiedBattle);
            Assert.That(route, Is.Not.Null,
                "The campaign catalog must retain a reachable authored story battle.");
            var campaign = CreateRosterCampaign(8410842);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                board.DefinitionId, new[] { "BOARD_UNION_084" }, _catalog020));
            foreach (var choice in route)
            {
                campaign = Require(_worldGate023.CommitNodeChoice(campaign,
                    _catalog023, choice, "BOARD_RECRUIT_A_084",
                    "BOARD_RECRUIT_B_084", 0));
                campaign = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                    campaign, _catalog023));
            }
            var operation = WorldGate(campaign).ActiveOperation;
            var battleNode = board.Nodes.Single(value => StringComparer.Ordinal.Equals(
                value.NodeId, operation.CurrentNodeId));
            Assert.That(battleNode.RequiresCertifiedBattle, Is.True);
            Assert.That(BoardAdventureRules084.AutomaticChoice084(
                operation.OperationId, battleNode), Is.Empty);
            var before = CanonicalJson.Serialize(campaign);
            var savePath = Path.Combine(Path.GetTempPath(),
                "sd084_auto_battle_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(Path.Combine(
                    Application.streamingAssetsPath, "Authority", "CONTENT"), savePath);
                var campaignField = typeof(M1RuntimeCoordinator).GetField(
                    "_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(campaignField, Is.Not.Null);
                campaignField.SetValue(coordinator, campaign);

                var rejected = coordinator.CommitAutomaticWorldGateRoom023();
                Assert.That(rejected.Succeeded, Is.False);
                var unchanged = (CampaignState)campaignField.GetValue(coordinator);
                Assert.That(CanonicalJson.Serialize(unchanged), Is.EqualTo(before));
                Assert.That(WorldGate(unchanged).ActiveOperation.PendingReceipt, Is.Null);
                Assert.That(unchanged.Guild.GuildCity.PendingEncounter, Is.Null);
            }
            finally
            {
                foreach (var path in new[]
                    { savePath, savePath + ".bak", savePath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Campaign020RejectsForgedIdentityMetadataPrefixAndStatus()
        {
            var campaign = CreateStartedChapter("CH018_001", "SKYHOME", 84101);
            var original = Playable(campaign).ActiveOperation;
            var forgeries = new[]
            {
                new CampaignPlayableOperationState020(
                    original.OperationId, original.BlueprintId, original.ChapterId,
                    original.WorldId, "FORGED_SEED_084", original.CurrentStepIndex,
                    original.Status, original.CompletedStepIds, original.AppliedReceiptIds,
                    original.PendingReceipt, original.ExistingBattleRewardReceiptId, "forged"),
                new CampaignPlayableOperationState020(
                    original.OperationId, original.BlueprintId, original.ChapterId,
                    "FORGED_WORLD_084", original.CanonicalSeedIdentity,
                    original.CurrentStepIndex, original.Status, original.CompletedStepIds,
                    original.AppliedReceiptIds, original.PendingReceipt,
                    original.ExistingBattleRewardReceiptId, "forged"),
                new CampaignPlayableOperationState020(
                    original.OperationId, original.BlueprintId, original.ChapterId,
                    original.WorldId, original.CanonicalSeedIdentity,
                    original.CurrentStepIndex,
                    CampaignPlayableOperationStatus020.AwaitingBattle,
                    original.CompletedStepIds, original.AppliedReceiptIds,
                    original.PendingReceipt, original.ExistingBattleRewardReceiptId,
                    "forged", original.AppliedReceipts),
                new CampaignPlayableOperationState020(
                    original.OperationId, original.BlueprintId, original.ChapterId,
                    original.WorldId, original.CanonicalSeedIdentity, 0,
                    CampaignPlayableOperationStatus020.ReadyToFinalize,
                    Array.Empty<string>(), Array.Empty<string>(), null, string.Empty,
                    "forged")
            };
            var prematureTravel = _worldGate023.Travel(campaign, _catalog023,
                original.WorldId, _catalog020);
            Assert.That(prematureTravel.IsSuccess, Is.False);
            CollectionAssert.Contains(prematureTravel.Errors,
                "CAMPAIGN023_TRAVEL_BLOCKED_BY_OPERATION");
            foreach (var forgery in forgeries)
            {
                var staged = WithPlayableOperation(campaign, forgery);
                var rejected = _campaign020.CommitNonBattleStep(
                    staged, _catalog020, "SUCCESS");
                Assert.That(rejected.IsSuccess, Is.False);
                CollectionAssert.Contains(rejected.Errors,
                    "CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID");
                var idempotentBegin = _campaign020.BeginOperation(
                    staged, _catalog020, original.ChapterId);
                Assert.That(idempotentBegin.IsSuccess, Is.False);
                CollectionAssert.Contains(idempotentBegin.Errors,
                    "CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID");
            }
        }

        [Test]
        public void Campaign020NeverCommitsAnUnapplyableOutcomeAndRejectsRewardTamper()
        {
            var campaign = CreateStartedChapter("CH018_001", "SKYHOME", 84102);
            var unsupported = _campaign020.CommitNonBattleStep(
                campaign, _catalog020, "SETBACK");
            Assert.That(unsupported.IsSuccess, Is.False);
            CollectionAssert.Contains(unsupported.Errors,
                "CAMPAIGN020_OUTCOME_UNSUPPORTED");
            Assert.That(Playable(campaign).ActiveOperation.PendingReceipt, Is.Null);

            var committed = Require(_campaign020.CommitNonBattleStep(
                campaign, _catalog020, "SUCCESS"));
            var operation = Playable(committed).ActiveOperation;
            var receipt = operation.PendingReceipt;
            var tampered = new CampaignStepReceipt020(
                receipt.ReceiptId, receipt.OperationId, receipt.StepId, receipt.Outcome,
                receipt.AuthoritativeHash, receipt.ExistingEquipmentRewardReceiptId,
                receipt.GuildXp + 100, receipt.HallXp, receipt.Materials,
                receipt.AppliedVersion);
            var forged = WithPlayableOperation(committed, operation.With(
                pendingReceipt: tampered, replacePendingReceipt: true));
            var rejected = _campaign020.ApplyStepReceiptExactlyOnce(forged, _catalog020);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "CAMPAIGN020_STEP_RECEIPT_INVALID");

            var applied = Require(_campaign020.ApplyStepReceiptExactlyOnce(
                committed, _catalog020));
            Assert.That(Playable(applied).ActiveOperation.CurrentStepIndex, Is.EqualTo(1));
        }

        [Test]
        public void Campaign023ForgedActiveOperationCannotMoveSyncOrFinalize()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 84103);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "BOARD_UNION_084" }, _catalog020));
            var operation = WorldGate(campaign).ActiveOperation;
            Assert.That(_catalog023.TryGetBoard(operation.DefinitionId, out var board), Is.True);
            var forged = new WorldGateOperationState023(
                "WGOP023_FORGED_084", operation.DefinitionId, operation.BoardId,
                operation.OperationKind, operation.WorldId, "FORGED_SEED_084",
                operation.CurrentNodeId, operation.Status, operation.Supplies,
                operation.Fatigue, operation.Urgency, operation.Threat, operation.Trust,
                operation.Tension, operation.CivilianSupport, operation.AlliedUnionIds,
                operation.CompletedNodeIds, operation.AppliedReceiptIds,
                operation.PendingReceipt, operation.ExistingBattleRewardReceiptId,
                "forged", operation.AppliedReceipts, operation.InitialTrust,
                operation.InitialTension, operation.InitialCivilianSupport,
                 operation.RewardPermilleAtBegin, operation.AlliedRosterIdentity,
                 operation.AlliedRecruitIds, operation.PreviousAuthorityChainHash);
            var staged = WithWorldGate(campaign, WorldGate(campaign).With(
                activeOperation: forged, replaceActiveOperation: true));
            var current = board.Nodes.Single(value =>
                value.NodeId == operation.CurrentNodeId);
            var move = _worldGate023.CommitNodeChoice(staged, _catalog023,
                current.ChoiceIds.FirstOrDefault() ?? "CONTINUE",
                "BOARD_RECRUIT_A_084", "BOARD_RECRUIT_B_084", 0);
            AssertAuthorityInvalid(move);

            var forgedReady = new WorldGateOperationState023(
                forged.OperationId, forged.DefinitionId, forged.BoardId,
                forged.OperationKind, forged.WorldId, forged.CanonicalSeedIdentity,
                board.ExitNodeId, WorldGateOperationStatus023.ReadyToFinalize,
                forged.Supplies, forged.Fatigue, forged.Urgency, forged.Threat,
                forged.Trust, forged.Tension, forged.CivilianSupport,
                operation.AlliedUnionIds, Array.Empty<string>(),
                Array.Empty<string>(), null, string.Empty, "forged_ready",
                Array.Empty<WorldGateNodeReceipt023>(), operation.InitialTrust,
                operation.InitialTension, operation.InitialCivilianSupport,
                 operation.RewardPermilleAtBegin, operation.AlliedRosterIdentity,
                 operation.AlliedRecruitIds, operation.PreviousAuthorityChainHash);
            var readyCampaign = WithWorldGate(campaign, WorldGate(campaign).With(
                activeOperation: forgedReady, replaceActiveOperation: true));
            AssertAuthorityInvalid(_worldGate023.FinalizeOperation(
                readyCampaign, _catalog023));

            var forgedAwaiting = new WorldGateOperationState023(
                operation.OperationId, operation.DefinitionId, operation.BoardId,
                operation.OperationKind, operation.WorldId,
                operation.CanonicalSeedIdentity, operation.CurrentNodeId,
                WorldGateOperationStatus023.AwaitingBattle, operation.Supplies,
                operation.Fatigue, operation.Urgency, operation.Threat, operation.Trust,
                operation.Tension, operation.CivilianSupport, operation.AlliedUnionIds,
                operation.CompletedNodeIds, operation.AppliedReceiptIds, null,
                string.Empty, "forged_awaiting", operation.AppliedReceipts,
                operation.InitialTrust, operation.InitialTension,
                 operation.InitialCivilianSupport, operation.RewardPermilleAtBegin,
                 operation.AlliedRosterIdentity, operation.AlliedRecruitIds,
                 operation.PreviousAuthorityChainHash);
            var awaitingCampaign = WithWorldGate(campaign, WorldGate(campaign).With(
                activeOperation: forgedAwaiting, replaceActiveOperation: true));
            AssertAuthorityInvalid(_worldGate023.SynchronizeAfterClaimedBattle(
                awaitingCampaign, _catalog023));
        }

        [Test]
        public void RepeatableBoardCannotBypassItsCampaignUnlockAndStateDoesNotMove()
        {
            var campaign = CreateRosterCampaign(841031);
            var repeatable = _registry023.Boards.Values.First(value =>
                value.operationKind == "REPEATABLE" && value.worldId == "SKYHOME");
            var beforeOrdinal = campaign.Guild.GuildCity.OperationOrdinal;
            var beforeRuntime = WorldGate(campaign);

            var rejected = _worldGate023.BeginOperation(campaign, _catalog023,
                repeatable.definitionId, new[] { "BOARD_UNION_084" }, _catalog020);

            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "CAMPAIGN023_REPEATABLE_CONTRACT_NOT_UNLOCKED");
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,
                Is.EqualTo(beforeOrdinal));
            Assert.That(WorldGate(campaign).ActiveOperation, Is.SameAs(
                beforeRuntime.ActiveOperation));
        }

        [Test]
        public void GenericMeaningfulOperationCannotInvalidateAnActiveBoardIdentity()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 841032);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "BOARD_UNION_084" }, _catalog020));
            var beforeOrdinal = campaign.Guild.GuildCity.OperationOrdinal;
            var beforeOperationId = WorldGate(campaign).ActiveOperation.OperationId;

            var rejected = new GuildCityCommandService017D()
                .CompleteMeaningfulOperation(campaign);

            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "GC017D_FINISH_ACTIVE_OPERATION_FIRST");
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,
                Is.EqualTo(beforeOrdinal));
            Assert.That(WorldGate(campaign).ActiveOperation.OperationId,
                Is.EqualTo(beforeOperationId));
        }

        [Test]
        public void OperationStartsRejectReverseOrderOverlapWithoutChangingState()
        {
            var campaign = CreateRosterCampaign(841033);
            var towerRegistry = CampaignRegistry022.LoadFromResources();
            var towerDefinition = towerRegistry.AbyssOperations.Values.First();
            var tower = new AbyssOperationState022("ABYSS_INSTANCE_REVERSE_084",
                towerDefinition.operationId, towerDefinition.floorId, 0,
                AbyssOperationStatus022.Active, Array.Empty<string>(), null,
                string.Empty, "ABYSS_SEED_REVERSE_084");
            var towerCampaign = WithTowerOperation(campaign, tower);
            var beforeTower = CanonicalJson.Serialize(towerCampaign);
            var registry019 = CampaignRegistry019.LoadFromResources();
            var catalog019 = new CampaignRuleCatalogAdapter019(registry019.Base018);

            var chapterRejected = new CampaignCommandService019().StartChapter(
                towerCampaign, catalog019, "CH018_001",
                new[] { "BOARD_UNION_084" }, true);
            Assert.That(chapterRejected.IsSuccess, Is.False);
            CollectionAssert.Contains(chapterRejected.Errors,
                "CAMPAIGN019_FINISH_ACTIVE_ADVENTURE_FIRST");

            var repeatable = _registry023.Boards.Values.First(value =>
                value.operationKind == "REPEATABLE" && value.worldId == "SKYHOME");
            var worldGateRejected = _worldGate023.BeginOperation(towerCampaign,
                _catalog023, repeatable.definitionId,
                new[] { "BOARD_UNION_084" }, _catalog020);
            Assert.That(worldGateRejected.IsSuccess, Is.False);
            CollectionAssert.Contains(worldGateRejected.Errors,
                "CAMPAIGN023_FINISH_ACTIVE_ADVENTURE_FIRST");

            var travelRejected = _worldGate023.Travel(towerCampaign, _catalog023,
                "SKYHOME", _catalog020);
            Assert.That(travelRejected.IsSuccess, Is.False);
            CollectionAssert.Contains(travelRejected.Errors,
                "CAMPAIGN023_TRAVEL_BLOCKED_BY_OPERATION");

            var content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT",
                "GUILD_CITY_017D"));
            var contractId = content.Contracts.Keys.First(value =>
                !StringComparer.Ordinal.Equals(value,
                    GuildCityExpeditionService017D.FirstStoryContractId066));
            var contractRejected = new GuildCityExpeditionService017D()
                .AcceptContract(towerCampaign, content, contractId);
            Assert.That(contractRejected.IsSuccess, Is.False);
            CollectionAssert.Contains(contractRejected.Errors,
                "GC017D_FINISH_ACTIVE_ADVENTURE_FIRST");
            Assert.That(CanonicalJson.Serialize(towerCampaign),
                Is.EqualTo(beforeTower));

            var storyCommitted = Require(new CampaignCommandService019().StartChapter(
                campaign, catalog019, "CH018_001",
                new[] { "BOARD_UNION_084" }, true));
            var corruptOverlap = WithTowerOperation(storyCommitted, tower);
            var playableRejected = _campaign020.BeginOperation(corruptOverlap,
                _catalog020, "CH018_001");
            Assert.That(playableRejected.IsSuccess, Is.False);
            CollectionAssert.Contains(playableRejected.Errors,
                "CAMPAIGN020_FINISH_ACTIVE_ADVENTURE_FIRST");

            var guildCampaign = Require(new GuildCityExpeditionService017D()
                .AcceptContract(campaign, content, contractId));
            var beforeGuild = CanonicalJson.Serialize(guildCampaign);
            var guildThenChapter = new CampaignCommandService019().StartChapter(
                guildCampaign, catalog019, "CH018_001",
                new[] { "BOARD_UNION_084" }, true);
            Assert.That(guildThenChapter.IsSuccess, Is.False);
            CollectionAssert.Contains(guildThenChapter.Errors,
                "CAMPAIGN019_FINISH_ACTIVE_ADVENTURE_FIRST");
            Assert.That(CanonicalJson.Serialize(guildCampaign),
                Is.EqualTo(beforeGuild));
        }

        [Test]
        public void TowerClosedWorldRejectsGuardianWithoutBattleAndReconWithBattle()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var guardian = registry.AbyssOperations.Values.First(value =>
                value.kind == "GUARDIAN");
            var noBattleGuardian = CloneTowerOperation(guardian,
                guardian.steps.Select(step => CloneTowerStep(step, false)).ToArray());
            Assert.That(TowerAdventureRules084.IsCompatible084(
                noBattleGuardian, out var guardianError), Is.False);
            Assert.That(guardianError, Is.EqualTo("TOWER_BATTLE_TILE_AUTHORITY_INVALID"));

            var recon = registry.AbyssOperations.Values.First(value =>
                value.kind == "RECON");
            var reconSteps = recon.steps.Select(CloneTowerStep).ToArray();
            reconSteps[3].requiresBattle = true;
            var battleRecon = CloneTowerOperation(recon, reconSteps);
            Assert.That(TowerAdventureRules084.IsCompatible084(
                battleRecon, out var reconError), Is.False);
            Assert.That(reconError, Is.EqualTo("TOWER_BATTLE_TILE_AUTHORITY_INVALID"));
        }

        [Test]
        public void BoardClosedWorldRejectsCyclesDeadReturnsChoiceOverflowAndBattleMetadata()
        {
            var cycle = Board("CYCLE", false, new[]
            {
                Node("S", "START", new[] { "A" }, new[] { "GO" }),
                Node("A", "EVENT", new[] { "A", "E" }, new[] { "LOOP", "EXIT" }),
                Node("E", "EXIT", Array.Empty<string>(), new[] { "RETURN" })
            }, "S", "E");
            Assert.That(BoardAdventureRules084.IsCompatible084(cycle,
                out var cycleError), Is.False);
            Assert.That(cycleError, Is.EqualTo("BOARD_ROUTE_CYCLE_FORBIDDEN"));

            var deadReturn = Board("DEAD_RETURN", false, new[]
            {
                Node("S", "START", new[] { "E", "X" }, new[] { "SAFE", "TRAP" }),
                Node("E", "EXIT", Array.Empty<string>(), new[] { "RETURN" }),
                Node("X", "EXIT", Array.Empty<string>(), new[] { "RETURN" })
            }, "S", "E");
            Assert.That(BoardAdventureRules084.IsCompatible084(deadReturn,
                out var deadError), Is.False);
            Assert.That(deadError, Is.EqualTo("BOARD_ROUTE_CANNOT_RETURN"));

            var tooManyChoices = Board("CHOICES", false, new[]
            {
                Node("S", "START", new[] { "E" },
                    new[] { "A", "B", "C", "D" }),
                Node("E", "EXIT", Array.Empty<string>(), new[] { "RETURN" })
            }, "S", "E");
            Assert.That(BoardAdventureRules084.IsCompatible084(tooManyChoices,
                out var choiceError), Is.False);
            Assert.That(choiceError, Is.EqualTo("CHOICE_COUNT_CLOSED_WORLD"));

            var staleBoardBattleFlag = Board("BATTLE_FLAG", true, new[]
            {
                Node("S", "START", new[] { "E" }, new[] { "GO" }),
                Node("E", "EXIT", Array.Empty<string>(), new[] { "RETURN" })
            }, "S", "E");
            Assert.That(BoardAdventureRules084.IsCompatible084(staleBoardBattleFlag,
                out var battleError), Is.False);
            Assert.That(battleError, Is.EqualTo("BOARD_BATTLE_METADATA_MISMATCH"));

            var staleEnemy = Node("S", "START", new[] { "E" }, new[] { "GO" });
            staleEnemy.EnemyUnionCount = 1;
            var staleEnemyBoard = Board("STALE_ENEMY", false, new[]
            {
                staleEnemy,
                Node("E", "EXIT", Array.Empty<string>(), new[] { "RETURN" })
            }, "S", "E");
            Assert.That(BoardAdventureRules084.IsCompatible084(staleEnemyBoard,
                out var enemyError), Is.False);
            Assert.That(enemyError, Is.EqualTo("BATTLE_AUTHORITY_REQUIRED"));
        }

        [Test]
        public void LegacyWorldGateInactiveOneZeroRecoversAndActiveTwoTwoMigrates()
        {
            var inactive = CreateRosterCampaign(841035);
            var inactiveRuntime = CloneWorldGateVersion(WorldGate(inactive),
                "CAMPAIGN_WORLD_GATE_EXPEDITION_RUNTIME_023_1.0").With(
                expeditionRecruitLeadIds089: new[] { "HERO_RECOVERY_089" },
                expeditionDeckTutorialSeen089: true);
            inactive = WithWorldGate(inactive, inactiveRuntime);
            var treasury = inactive.Guild.TreasuryXp;
            var hall = inactive.Guild.Development.HallEnhancementXp;
            Assert.That(CampaignWorldGateCommandService023.RequiresLegacyRecovery084(
                inactiveRuntime), Is.True);

            var recovered = Require(_worldGate023
                .AbortUnverifiableLegacyOperation084(inactive));
            Assert.That(WorldGate(recovered).ContentAuthorityVersion,
                Is.EqualTo(WorldGateRuntimeState023.ContentVersion));
            Assert.That(WorldGate(recovered).ActiveOperation, Is.Null);
            Assert.That(recovered.Guild.TreasuryXp, Is.EqualTo(treasury));
            Assert.That(recovered.Guild.Development.HallEnhancementXp,
                Is.EqualTo(hall));
            CollectionAssert.AreEqual(new[] { "HERO_RECOVERY_089" },
                WorldGate(recovered).ExpeditionRecruitLeadIds089);
            Assert.That(WorldGate(recovered).ExpeditionDeckTutorialSeen089,
                Is.True);
            Assert.That(CampaignWorldGateCommandService023
                .ValidateStoredCompletionProofs084(recovered, _catalog023,
                    WorldGate(recovered)), Is.True,
                "Mutable Expedition leads/tutorial must survive recovery without changing the authority-chain base hash.");

            var active = CreateAtWorldBoard("CH018_001", "SKYHOME", 841036);
            active = Require(_worldGate023.BeginOperation(active, _catalog023,
                "CH018_001", new[] { "BOARD_UNION_084" }, _catalog020));
            var activeRuntime = CloneWorldGateVersion(WorldGate(active),
                "CAMPAIGN_WORLD_GATE_EXPEDITION_RUNTIME_023_2.2");
            active = WithWorldGate(active, activeRuntime);
            var activeOperationId = activeRuntime.ActiveOperation.OperationId;
            Assert.That(CampaignWorldGateCommandService023.RequiresLegacyMigration084(
                activeRuntime), Is.True);

            var migrated = Require(_worldGate023.MigrateLegacyNodeLedger084(
                active, _catalog023));
            Assert.That(WorldGate(migrated).ContentAuthorityVersion,
                Is.EqualTo(WorldGateRuntimeState023.ContentVersion));
            Assert.That(WorldGate(migrated).ActiveOperation.OperationId,
                Is.EqualTo(activeOperationId));
            Assert.That(WorldGate(migrated).ActiveNodeLedger, Is.Empty);
        }

        [Test]
        public void LegacyActiveStoryChapterCanRecoverIntoPlayableBoard()
        {
            var campaign = CreateRosterCampaign(84104);
            var registry019 = CampaignRegistry019.LoadFromResources();
            var catalog019 = new CampaignRuleCatalogAdapter019(registry019.Base018);
            campaign = Require(new CampaignCommandService019().StartChapter(
                campaign, catalog019, "CH018_001", new[] { "BOARD_UNION_084" }, true));
            Assert.That(Playable(campaign).ActiveOperation, Is.Null);
            var savePath = Path.Combine(Path.GetTempPath(),
                "sd084_campaign_recovery_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    savePath);
                var field = typeof(M1RuntimeCoordinator).GetField("_campaign",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(field, Is.Not.Null);
                field.SetValue(coordinator, campaign);
                var result = coordinator.StartPlayableChapter020("CH018_001");
                Assert.That(result.Succeeded, Is.True, result.Message);
                var recovered = (CampaignState)field.GetValue(coordinator);
                Assert.That(Playable(recovered).ActiveOperation, Is.Not.Null);
                Assert.That(Playable(recovered).ActiveOperation.ChapterId,
                    Is.EqualTo("CH018_001"));
            }
            finally
            {
                foreach (var path in new[] { savePath, savePath + ".bak", savePath + ".tmp" })
                    if (File.Exists(path)) File.Delete(path);
            }
        }

        static IReadOnlyList<string> RouteToNode084(
            WorldGateBoardRule023 board,
            Func<WorldGateNodeRule023, bool> target)
        {
            var byId = board.Nodes.ToDictionary(value => value.NodeId,
                value => value, StringComparer.Ordinal);
            var paths = new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.Ordinal)
            {
                [board.StartNodeId] = Array.Empty<string>()
            };
            var queue = new Queue<string>();
            queue.Enqueue(board.StartNodeId);
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                var node = byId[nodeId];
                if (target(node)) return paths[nodeId];
                if (node.RequiresCertifiedBattle) continue;
                var choices = node.ChoiceIds != null && node.ChoiceIds.Count > 0
                    ?node.ChoiceIds
                    :new[] { "CONTINUE" };
                foreach (var choice in choices)
                {
                    if (!BoardAdventureRules084.TryNextNode084(
                            node, choice, out var nextNodeId) ||
                        string.IsNullOrWhiteSpace(nextNodeId) ||
                        paths.ContainsKey(nextNodeId))
                        continue;
                    var path = new List<string>(paths[nodeId]) { choice };
                    paths[nextNodeId] = path.AsReadOnly();
                    queue.Enqueue(nextNodeId);
                }
            }
            return null;
        }

        static AbyssOperationDto022 CloneTowerOperation(
            AbyssOperationDto022 source, AbyssStepDto022[] steps) =>
            new AbyssOperationDto022
            {
                operationId = source.operationId, floorId = source.floorId,
                kind = source.kind, displayName = source.displayName, steps = steps,
                firstClearOnly = source.firstClearOnly,
                requiresPreviousFloorClear = source.requiresPreviousFloorClear,
                rewardMaterialIds = source.rewardMaterialIds, guildXp = source.guildXp,
                hallXp = source.hallXp, summonResonance = source.summonResonance,
                exactOnceReceipts = source.exactOnceReceipts,
                existingEquipmentRewardRemainsAuthoritative =
                    source.existingEquipmentRewardRemainsAuthoritative
            };

        static WorldGateNodeRule023 Node(
            string id, string kind, string[] next, string[] choices) =>
            new WorldGateNodeRule023
            {
                NodeId = id, Kind = kind, Title = id, Description = id,
                NextNodeIds = next, ChoiceIds = choices
            };

        static WorldGateBoardRule023 Board(
            string id, bool usesBattle, WorldGateNodeRule023[] nodes,
            string start, string exit) => new WorldGateBoardRule023
            {
                BoardId = "BOARD_" + id, DefinitionId = "DEF_" + id,
                OperationKind = "REPEATABLE", WorldId = "SKYHOME", Title = id,
                StartNodeId = start, ExitNodeId = exit, UsesCertifiedBattle = usesBattle,
                MaximumAlliedUnions = 10, MaximumEnemyUnions = 10,
                Nodes = nodes
            };

        static AbyssStepDto022 CloneTowerStep(AbyssStepDto022 source) =>
            CloneTowerStep(source, source.requiresBattle);

        static AbyssStepDto022 CloneTowerStep(
            AbyssStepDto022 source, bool requiresBattle) => new AbyssStepDto022
            {
                stepId = source.stepId, kind = source.kind, title = source.title,
                requiresBattle = requiresBattle, bossId = source.bossId,
                exactOnce = source.exactOnce
            };

        CampaignState CreateStartedChapter(string chapterId, string worldId, long seed)
        {
            var campaign = CreateRosterCampaign(seed);
            var city = campaign.Guild.GuildCity;
            var progress = city.Strategic017H.Campaign019.With(
                activeChapterId: chapterId,
                unlockedWorldIds: new[] { "SKYHOME", worldId }
                    .Distinct(StringComparer.Ordinal).ToArray(),
                lastCheckpointId: "chapter_active_084");
            var strategic = city.Strategic017H.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: progress.LastCheckpointId);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city.With(
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId)), campaign.OpeningFlow);
            return Require(_campaign020.BeginOperation(campaign, _catalog020, chapterId));
        }

        CampaignState CreateAtWorldBoard(string chapterId, string worldId, long seed)
        {
            var campaign = CreateStartedChapter(chapterId, worldId, seed);
            campaign = Require(_campaign020.CommitNonBattleStep(
                campaign, _catalog020, "SUCCESS"));
            return Require(_campaign020.ApplyStepReceiptExactlyOnce(
                campaign, _catalog020));
        }

        static CampaignState CreateRosterCampaign(long seed)
        {
            var recruitA = new RecruitState("BOARD_RECRUIT_A_084", 100, 100, 20, 20);
            var recruitB = new RecruitState("BOARD_RECRUIT_B_084", 100, 100, 20, 20);
            var union = new UnionState("BOARD_UNION_084", "Adventure Union",
                UnionKind.Normal, recruitA.RecruitId,
                new[] { recruitA.RecruitId, recruitB.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(source.Guild.GuildId, source.Guild.TreasuryXp,
                new[] { recruitA, recruitB }, new[] { union }, source.Guild.Inventory,
                source.Guild.Development, guildCity: null);
            return source.With(guild, source.OpeningFlow);
        }

        static CampaignPlayableState020 Playable(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            Playable(campaign).WorldGate023;

        static CampaignState WithTowerOperation(CampaignState campaign,
            AbyssOperationState022 operation)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var tower = playable.Progression022.With(activeAbyssOperation: operation,
                replaceActiveAbyssOperation: true,
                lastCheckpointId: "tower_reverse_overlap_084");
            playable = playable.With(progression022: tower,
                replaceProgression022: true,
                lastCheckpointId: "tower_reverse_overlap_084");
            progress = progress.With(playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: "tower_reverse_overlap_084");
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: "tower_reverse_overlap_084");
            city = city.With(strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: "tower_reverse_overlap_084");
            return campaign.With(campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        static CampaignState WithPlayableOperation(
            CampaignState campaign, CampaignPlayableOperationState020 operation)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(activeOperation: operation,
                replaceActiveOperation: true, lastCheckpointId: operation.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: operation.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: operation.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: operation.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState WithWorldGate(
            CampaignState campaign, WorldGateRuntimeState023 runtime)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(worldGate023: runtime,
                replaceWorldGate023: true, lastCheckpointId: runtime.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: runtime.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: runtime.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: runtime.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static WorldGateRuntimeState023 CloneWorldGateVersion(
            WorldGateRuntimeState023 source, string contentVersion) =>
            new WorldGateRuntimeState023(contentVersion, source.ActiveOperation,
                source.CurrentWorldId, source.UnlockedWorldIds, source.WorldStandings,
                source.CompletedDefinitionIds, source.UnlockedRecruitOriginIds,
                source.RepeatableStreakDefinitionId, source.RepeatableStreakCount,
                source.TravelSupplies, source.LastCheckpointId,
                source.LastCompletionProof, source.CompletionProofs,
                source.AuthorityChainBaseHash,
                source.AuthorityChainBaseOperationOrdinal,
                source.AuthorityChainBaseStandings,
                source.AuthorityChainBaseUnlockedOrigins,
                source.AuthorityChainBaseUnlockedWorlds,
                source.AuthorityChainBaseCompletedDefinitions,
                source.AuthorityEntries, source.AuthorityChainHash,
                source.AuthorityChainBaseCurrentWorldId,
                source.AuthorityChainBaseTravelSupplies,
                source.AuthorityChainBaseRepeatableStreakDefinitionId,
                source.AuthorityChainBaseRepeatableStreakCount,
                source.AuthorityChainMigrationReceiptId,
                source.ActiveNodeLedger, source.LegacyOperationArchives,
                source.ExpeditionRecruitLeadIds089,
                source.ExpeditionDeckTutorialSeen089);

        static void AssertAuthorityInvalid(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.False);
            CollectionAssert.Contains(result.Errors,
                "CAMPAIGN023_ACTIVE_OPERATION_AUTHORITY_INVALID");
        }

        static CampaignState Require(SecondDimension.Core.Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
