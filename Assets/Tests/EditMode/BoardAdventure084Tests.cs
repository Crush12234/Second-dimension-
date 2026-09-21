#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardAdventure084Tests
    {
        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignRegistry022 _registry022;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignPlayableCommandService020 _campaign020;
        CampaignWorldGateCommandService023 _worldGate023;

        [SetUp]
        public void SetUp()
        {
            _registry020 = CampaignRegistry020.LoadFromResources();
            _catalog020 = new Campaign020RuleCatalogAdapter(_registry020);
            _registry022 = CampaignRegistry022.LoadFromResources();
            _registry023 = CampaignRegistry023.LoadFromResources();
            _catalog023 = new Campaign023RuleCatalogAdapter(_registry023);
            _campaign020 = new CampaignPlayableCommandService020();
            _worldGate023 = new CampaignWorldGateCommandService023();
        }

        [Test]
        public void EveryCampaign020ChapterIsAClosedWorldSavedTilePath()
        {
            Assert.That(_registry020.Blueprints.Count, Is.EqualTo(82));
            Assert.That(_registry020.Blueprints.Values.Sum(value => value.steps.Length),
                Is.EqualTo(482));
            foreach (var source in _registry020.Blueprints.Values)
            {
                Assert.That(_catalog020.TryGetBlueprint(source.chapterId, out var board),
                    Is.True, source.chapterId);
                Assert.That(CampaignAdventureRules084.IsCompatible084(board, out var error),
                    Is.True, source.chapterId + ": " + error);
                Assert.That(board.Steps.Count(step =>
                    CampaignAdventureRules084.IsWorldBoardStep084(step)), Is.EqualTo(1),
                    source.chapterId);

                var operation = new CampaignPlayableOperationState020(
                    "OP020_TEST_" + source.chapterId, board.BlueprintId,
                    board.ChapterId, board.WorldId, "SEED_" + source.chapterId,
                    0, CampaignPlayableOperationStatus020.Active,
                    Array.Empty<string>(), Array.Empty<string>(), null,
                    string.Empty, "test");
                var reloaded = JsonConvert.DeserializeObject<CampaignPlayableOperationState020>(
                    JsonConvert.SerializeObject(operation));
                Assert.That(reloaded, Is.Not.Null, source.chapterId);
                for (var index = 0; index < board.Steps.Count; index++)
                    Assert.That(CampaignAdventureRules084.TileOrderKey084(
                            operation.OperationId, board.Steps[index].StepId, index),
                        Is.EqualTo(CampaignAdventureRules084.TileOrderKey084(
                            reloaded.OperationId, board.Steps[index].StepId, index)),
                        source.chapterId + " step " + index);
            }
        }

        [Test]
        public void Campaign020CertifiedBattlesExactlyMatchCampaign019Narratives()
        {
            Assert.That(_registry020.Base019.Chapters.Values.Count(value =>
                value.battleRequired), Is.EqualTo(37));
            Assert.That(_registry020.Blueprints.Values.Count(value =>
                value.requiresCertifiedBattle), Is.EqualTo(37));
            Assert.That(_registry020.Blueprints.Values.Sum(value =>
                value.steps.Count(step => step.requiresCertifiedBattle)),
                Is.EqualTo(37));

            foreach (var blueprint in _registry020.Blueprints.Values)
            {
                Assert.That(_registry020.Base019.Chapters.TryGetValue(
                    blueprint.chapterId, out var narrative), Is.True,
                    blueprint.chapterId);
                var battleSteps = blueprint.steps.Where(step =>
                    step.requiresCertifiedBattle).ToArray();
                Assert.That(blueprint.requiresCertifiedBattle,
                    Is.EqualTo(narrative.battleRequired), blueprint.chapterId);
                Assert.That(battleSteps.Length,
                    Is.EqualTo(narrative.battleRequired ? 1 : 0),
                    blueprint.chapterId);
                Assert.That(battleSteps.All(step =>
                    StringComparer.Ordinal.Equals(step.kind, "CERTIFIED_BATTLE")),
                    Is.True, blueprint.chapterId);
            }
        }

        [Test]
        public void EveryCampaign023BoardRoutesEveryAuthoredBranchAndSurvivesReload()
        {
            Assert.That(_catalog023.AllBoards.Count, Is.EqualTo(130));
            Assert.That(_catalog023.AllBoards.Sum(board => board.Nodes.Count),
                Is.EqualTo(1372));
            Assert.That(_catalog023.AllBoards.Count(board => board.OperationKind == "CHAPTER"),
                Is.EqualTo(82));
            Assert.That(_catalog023.AllBoards.Count(board => board.OperationKind == "REPEATABLE"),
                Is.EqualTo(32));
            Assert.That(_catalog023.AllBoards.Count(board => board.OperationKind == "CRISIS"),
                Is.EqualTo(16));

            var index = 0;
            foreach (var board in _catalog023.AllBoards.OrderBy(value => value.DefinitionId))
            {
                Assert.That(BoardAdventureRules084.IsCompatible084(board, out var error),
                    Is.True, board.DefinitionId + ": " + error);
                var operation = new WorldGateOperationState023(
                    "WGOP023_TEST_" + index.ToString("000"), board.DefinitionId,
                    board.BoardId, board.OperationKind, board.WorldId,
                    "SEED_" + index, board.StartNodeId, WorldGateOperationStatus023.Active,
                    12, 0, 0, 0, 0, 25, 0, new[] { "UNION_TEST" },
                    Array.Empty<string>(), Array.Empty<string>(), null,
                    string.Empty, "test");
                var reloaded = JsonConvert.DeserializeObject<WorldGateOperationState023>(
                    JsonConvert.SerializeObject(operation));
                Assert.That(reloaded, Is.Not.Null, board.DefinitionId);
                Assert.That(reloaded.CurrentNodeId, Is.EqualTo(board.StartNodeId));

                var byId = board.Nodes.ToDictionary(value => value.NodeId,
                    StringComparer.Ordinal);
                var reached = new HashSet<string>(StringComparer.Ordinal);
                var pending = new Queue<string>();
                pending.Enqueue(board.StartNodeId);
                while (pending.Count > 0)
                {
                    var nodeId = pending.Dequeue();
                    if (!reached.Add(nodeId)) continue;
                    var node = byId[nodeId];
                    var choices = node.ChoiceIds.Count == 0
                        ? new[] { "CONTINUE" }
                        : node.ChoiceIds;
                    var originalOrder = BoardAdventureRules084.OrderedChoices084(
                        operation.OperationId, node.NodeId, node.ChoiceIds);
                    var reloadOrder = BoardAdventureRules084.OrderedChoices084(
                        reloaded.OperationId, node.NodeId, node.ChoiceIds);
                    CollectionAssert.AreEqual(originalOrder, reloadOrder,
                        board.DefinitionId + " / " + node.NodeId);
                    foreach (var choice in choices)
                    {
                        Assert.That(BoardAdventureRules084.TryNextNode084(
                                node, choice, out var next), Is.True,
                            board.DefinitionId + " / " + node.NodeId + " / " + choice);
                        if (!string.IsNullOrWhiteSpace(next)) pending.Enqueue(next);
                    }
                }
                CollectionAssert.AreEquivalent(byId.Keys, reached, board.DefinitionId);
                Assert.That(reached, Does.Contain(board.ExitNodeId), board.DefinitionId);
                AssertRoutesNeverMovePawnBackward(board, board.StartNodeId,
                    new List<string>(), new HashSet<string>(StringComparer.Ordinal));
                index++;
            }
        }

        [Test]
        public void AllChapterCampScoutChoicesReachTheOptionalDiscovery()
        {
            var camps = _catalog023.AllBoards
                .Where(board => board.OperationKind == "CHAPTER")
                .SelectMany(board => board.Nodes)
                .Where(node => node.Kind == "CAMP" && node.NextNodeIds.Count == 2)
                .ToArray();
            Assert.That(camps.Length, Is.EqualTo(82));
            foreach (var camp in camps)
            {
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    camp, "SCOUT", out var scout), Is.True);
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    camp, "REST", out var rest), Is.True);
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    camp, "MENTOR", out var mentor), Is.True);
                Assert.That(scout, Is.EqualTo(camp.NextNodeIds[1]));
                Assert.That(rest, Is.EqualTo(camp.NextNodeIds[0]));
                Assert.That(mentor, Is.EqualTo(camp.NextNodeIds[0]));
                Assert.That(scout, Is.Not.EqualTo(rest));
            }
        }

        [Test]
        public void BoardProjectionNeverLeaksAuthorityIdsAndReturnAlwaysReachesPawnEnd()
        {
            foreach (var board in _catalog023.AllBoards)
            foreach (var node in board.Nodes)
            {
                var title = BoardAdventureRules084.PlayerCopy084(node.Title,
                    node.NodeId, node.SourceId, board.BoardId, board.DefinitionId);
                var description = BoardAdventureRules084.PlayerCopy084(node.Description,
                    node.NodeId, node.SourceId, board.BoardId, board.DefinitionId);
                foreach (var id in new[]
                    { node.NodeId, node.SourceId, board.BoardId, board.DefinitionId })
                {
                    if (string.IsNullOrWhiteSpace(id)) continue;
                    Assert.That(title, Does.Not.Contain(id), board.DefinitionId);
                    Assert.That(description, Does.Not.Contain(id), board.DefinitionId);
                }
                Assert.That(BoardAdventureRules084.PlayerCopy084(node.NodeId, node.NodeId),
                    Is.EqualTo("the current quest"));
            }
            Assert.That(BoardAdventureRules084.SemanticTrackPhase084(
                "TRAIL", WorldGateOperationStatus023.ReadyToFinalize.ToString()),
                Is.EqualTo(5));
            Assert.That(BoardAdventureRules084.SemanticTrackPhase084("RETURN", "Active"),
                Is.EqualTo(5));
        }

        [Test]
        public void RawAuthorityCopyGuardRejectsOpaqueIdsWithoutRejectingPlayerWords()
        {
            const string legitimateCopy =
                "CURRENT TASK  •  Begin the quest.\nFLIP  •  BOLD ROUTE";
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    legitimateCopy, "ASK"), Is.False,
                "ASK inside TASK is ordinary player copy, not an authority leak.");
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    legitimateCopy, "BOLD"), Is.False,
                "Single-word semantic choices are valid player language.");
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    legitimateCopy, "BOLD_ROUTE"), Is.False,
                "The projected label must not be mistaken for its underscored ID.");

            const string nodeId = "B023_CH018_032_00";
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    "ROOM REVEAL  •  " + nodeId + ".", nodeId), Is.True);
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    "ROOM REVEAL  •  b023_ch018_032_00.", nodeId), Is.True,
                "Authority IDs remain forbidden after a casing change.");
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    "ROOM REVEAL  •  XB023_CH018_032_00Z.", nodeId), Is.False,
                "A token substring inside another identifier is not the same authority ID.");
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    "FLIP  •  SAFE_ROUTE", "SAFE_ROUTE"), Is.True);
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    "FLIP  •  SAFE ROUTE", "SAFE_ROUTE"), Is.False);
        }

        [Test]
        public void CertifiedChapterProjectionDoesNotTreatCurrentTaskAsAskAuthorityLeak()
        {
            var board = _registry023.Boards["CH018_032"];
            var chapter = _registry020.Base019.Chapters[board.definitionId];
            var world = _registry023.Standing[board.worldId].displayName;
            var current = board.nodes.Single(value =>
                StringComparer.Ordinal.Equals(value.nodeId, board.startNodeId));
            var room = AdventureBoardNarrativeProjection084.Project(
                board, current, chapter, world);
            var visibleCopy =
                "STORY QUEST  •  " + BoardAdventureRules084.PlayerCopy084(
                    board.title, board.definitionId, board.boardId).ToUpperInvariant() +
                "\nBOARD OBJECTIVE  •  " +
                AdventureBoardNarrativeProjection084.BoardObjective(board, chapter, world) +
                "\nCURRENT TASK  •  " + room.Objective +
                "\nCURRENT TASK  •  " + room.Title.ToUpperInvariant() +
                "\nMISSION  •  " + room.StoryFlavor +
                "\nROOM REVEAL  •  " + room.Description +
                "\n" + string.Join("\n", (current.choiceIds ?? Array.Empty<string>())
                    .Select(value => "FLIP  •  " +
                        BoardAdventureRules084.ChoiceLabel084(value).ToUpperInvariant()));

            Assert.That(visibleCopy, Does.Contain("CURRENT TASK"));
            Assert.That(visibleCopy.IndexOf("ASK", StringComparison.OrdinalIgnoreCase),
                Is.GreaterThanOrEqualTo(0),
                "This pins the packaged-smoke false positive caused by ASK inside TASK.");

            var authorityTokens = new[] {board.definitionId, board.boardId}
                .Concat((board.nodes ?? Array.Empty<NodeDto023>()).Where(value => value != null)
                    .SelectMany(value => new[] {value.nodeId, value.sourceId}
                        .Concat(value.choiceIds ?? Array.Empty<string>())))
                .Where(value => !string.IsNullOrWhiteSpace(value));
            foreach (var token in authorityTokens)
                Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                        visibleCopy, token), Is.False, token);

            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    visibleCopy + "\nDEBUG " + board.startNodeId, board.startNodeId),
                Is.True, "A real opaque-node leak must still fail the certification guard.");
        }

        [Test]
        public void CertifiedLongestRepeatableProjectionDoesNotTreatCompleteAsAuthorityLeak()
        {
            var board = _registry023.Boards["REPEAT020_SKYHOME_04"];
            var world = _registry023.Standing[board.worldId].displayName;
            var current = board.nodes.Single(value =>
                StringComparer.Ordinal.Equals(value.nodeId, board.startNodeId));
            var room = AdventureBoardNarrativeProjection084.Project(
                board, current, null, world);
            var boardObjective = AdventureBoardNarrativeProjection084.BoardObjective(
                board, null, world);
            var visibleCopy =
                "GUILD CONTRACT  •  " + BoardAdventureRules084.PlayerCopy084(
                    board.title, board.definitionId, board.boardId).ToUpperInvariant() +
                "\nBOARD OBJECTIVE  •  " + boardObjective +
                "\nCURRENT TASK  •  " + room.Objective +
                "\nCURRENT TASK  •  " + room.Title.ToUpperInvariant() +
                "\nROOM REVEAL  •  " + room.Description +
                "\nSTORY  •  " + room.StoryFlavor +
                "\n" + string.Join("\n", (current.choiceIds ?? Array.Empty<string>())
                    .Select(value => "FLIP  •  " +
                        BoardAdventureRules084.ChoiceLabel084(value).ToUpperInvariant()));

            Assert.That(boardObjective, Does.StartWith("Complete "));
            Assert.That(visibleCopy.IndexOf("COMPLETE", StringComparison.OrdinalIgnoreCase),
                Is.GreaterThanOrEqualTo(0),
                "This pins the second packaged-smoke collision with its future COMPLETE choice.");

            var authorityTokens = new[] {board.definitionId, board.boardId}
                .Concat((board.nodes ?? Array.Empty<NodeDto023>()).Where(value => value != null)
                    .SelectMany(value => new[] {value.nodeId, value.sourceId}
                        .Concat(value.choiceIds ?? Array.Empty<string>())))
                .Where(value => !string.IsNullOrWhiteSpace(value));
            foreach (var token in authorityTokens)
                Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                        visibleCopy, token), Is.False, token);

            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    visibleCopy + "\nDEBUG " + board.startNodeId, board.startNodeId),
                Is.True, "A real repeatable-board node ID must still fail the guard.");
            Assert.That(BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                    visibleCopy + "\nDEBUG SAFE_ROUTE", "SAFE_ROUTE"),
                Is.True, "A real underscored choice ID must still fail the guard.");
        }

        [Test]
        public void UnknownBoardStepAndNodeKindsFailClosed()
        {
            var unknownStep = new CampaignBlueprintRule020
            {
                BlueprintId = "UNKNOWN_BLUEPRINT", ChapterId = "UNKNOWN_CHAPTER",
                WorldId = "SKYHOME", MaximumAlliedUnions = 1, MaximumEnemyUnions = 1,
                Steps = new[]
                {
                    Step("S0", "BRIEFING"), Step("S1", "WORLD_BOARD"),
                    Step("S2", "UNREVIEWED_FUTURE_KIND"), Step("S3", "RESULTS")
                }
            };
            Assert.That(CampaignAdventureRules084.IsCompatible084(
                unknownStep, out var campaignError), Is.False);
            Assert.That(campaignError, Does.StartWith("STEP_KIND_CLOSED_WORLD"));

            var unsupportedNode = new WorldGateNodeRule023
            {
                NodeId = "N0", Kind = "UNREVIEWED_FUTURE_KIND", Title = "Unknown",
                NextNodeIds = new[] { "N1" }, ChoiceIds = new[] { "CONTINUE" }
            };
            var exit = new WorldGateNodeRule023
            {
                NodeId = "N1", Kind = "EXIT", Title = "Return",
                NextNodeIds = Array.Empty<string>(), ChoiceIds = new[] { "RETURN" }
            };
            var unknownBoard = new WorldGateBoardRule023
            {
                BoardId = "UNKNOWN_BOARD", DefinitionId = "UNKNOWN_DEFINITION",
                OperationKind = "CHAPTER", WorldId = "SKYHOME", Title = "Unknown",
                StartNodeId = "N0", ExitNodeId = "N1", MaximumAlliedUnions = 1,
                MaximumEnemyUnions = 1, Nodes = new[] { unsupportedNode, exit }
            };
            Assert.That(BoardAdventureRules084.IsCompatible084(
                unknownBoard, out var boardError), Is.False);
            Assert.That(boardError, Does.StartWith("NODE_KIND_CLOSED_WORLD"));
            Assert.That(BoardAdventureRules084.TryNextNode084(
                unsupportedNode, "CONTINUE", out _), Is.False);
        }

        [Test]
        public void RepeatRewardsMaterialsAndTravelReachZeroWithoutFalsePreview()
        {
            var policy = _catalog023.RewardPolicy;
            var operation = new WorldGateOperationState023(
                "WGOP023_REPEAT_TEST", "REPEAT020_TEST", "BOARD_TEST", "REPEATABLE",
                "SKYHOME", "SEED", "N0", WorldGateOperationStatus023.Active,
                10, 0, 0, 0, 0, 0, 0, new[] { "UNION" }, Array.Empty<string>(),
                Array.Empty<string>(), null, string.Empty, "test");
            var runtime = WorldGateRuntimeState023.Default();
            Assert.That(BoardAdventureRules084.RewardPermille084(operation, policy, runtime),
                Is.EqualTo(10000));
            runtime = runtime.With(repeatableStreakDefinitionId: operation.DefinitionId,
                repeatableStreakCount: 3);
            Assert.That(BoardAdventureRules084.RewardPermille084(operation, policy, runtime),
                Is.Zero);
            Assert.That(BoardAdventureRules084.ScaledMaterialCount084(3, 0), Is.Zero);
            var preview = BoardAdventureRules084.RewardPreview084(new WorldGateNodeRule023
            {
                NodeId = "N0", Kind = "RESOURCE", Title = "Cache", GuildXp = 4,
                HallXp = 4, MaterialIds = new[] { "M1", "M2", "M3" },
                NextNodeIds = new[] { "N1" }, ChoiceIds = new[] { "CONTINUE" }
            }, 0);
            Assert.That(preview, Does.Contain("REPEAT REWARDS DEPLETED THIS RUN"));
            Assert.That(preview, Does.Not.Contain("MATERIAL REWARD"));
            Assert.That(preview, Does.Not.Contain("+4 GUILD XP"));
        }

        [Test]
        public void EveryStoryBoardCompletesThroughSavedCampaign023Proof()
        {
            var blueprints = _registry020.Blueprints.Values
                .OrderBy(value => value.chapterId, StringComparer.Ordinal).ToArray();
            for (var index = 0; index < blueprints.Length; index++)
            {
                var source = blueprints[index];
                var campaign = CreateAtWorldBoard(source.chapterId, source.worldId,
                    84000 + index);
                campaign = Require(_worldGate023.Travel(campaign, _catalog023,
                    source.worldId, _catalog020));
                campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                    source.chapterId, new[] { "BOARD_UNION_084" }, _catalog020));

                var reloadedOnce = false;
                for (var guard = 0; guard < 16; guard++)
                {
                    var active = WorldGate(campaign).ActiveOperation;
                    Assert.That(active, Is.Not.Null, source.chapterId);
                    if (active.Status == WorldGateOperationStatus023.ReadyToFinalize) break;
                    Assert.That(_catalog023.TryGetBoard(active.DefinitionId, out var board),
                        Is.True);
                    var node = board.Nodes.Single(value =>
                        value.NodeId == active.CurrentNodeId);
                    Assert.That(node.RequiresCertifiedBattle, Is.False,
                        "Chapter boards must leave certified battles to Campaign020.");
                    var choice = node.ChoiceIds.FirstOrDefault() ?? "CONTINUE";
                    campaign = Require(_worldGate023.CommitNodeChoice(campaign,
                        _catalog023, choice, "BOARD_RECRUIT_A_084",
                        "BOARD_RECRUIT_B_084", 0));
                    if (!reloadedOnce)
                    {
                        campaign = JsonConvert.DeserializeObject<CampaignState>(
                            JsonConvert.SerializeObject(campaign));
                        Assert.That(campaign, Is.Not.Null);
                        reloadedOnce = true;
                    }
                    campaign = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                        campaign, _catalog023));
                }
                Assert.That(WorldGate(campaign).ActiveOperation.Status,
                    Is.EqualTo(WorldGateOperationStatus023.ReadyToFinalize),
                    source.chapterId);
                campaign = Require(_worldGate023.FinalizeOperation(campaign, _catalog023));
                var committed = _campaign020.CommitCompletedWorldBoardStep(
                    campaign, _catalog020);
                Assert.That(committed.IsSuccess, Is.True,
                    source.chapterId + ": " + string.Join("\n", committed.Errors));
            }
        }

        [Test]
        public void TamperedWorldGateReceiptFailsClosedAndCannotChangeReward()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 84999);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "BOARD_UNION_084" }, _catalog020));
            var active = WorldGate(campaign).ActiveOperation;
            Assert.That(_catalog023.TryGetBoard(active.DefinitionId, out var board), Is.True);
            var node = board.Nodes.Single(value => value.NodeId == active.CurrentNodeId);
            campaign = Require(_worldGate023.CommitNodeChoice(campaign, _catalog023,
                node.ChoiceIds[0], "BOARD_RECRUIT_A_084", "BOARD_RECRUIT_B_084", 0));
            active = WorldGate(campaign).ActiveOperation;
            var receipt = active.PendingReceipt;
            var tampered = new WorldGateNodeReceipt023(
                receipt.ReceiptId, receipt.OperationId, receipt.NodeId, receipt.ChoiceId,
                receipt.NextNodeId, receipt.Outcome, receipt.AuthoritativeHash,
                receipt.SupplyDelta, receipt.FatigueDelta, receipt.UrgencyDelta,
                receipt.ThreatDelta, receipt.TrustDelta, receipt.TensionDelta,
                receipt.CivilianSupportDelta, receipt.GuildXp + 50, receipt.HallXp,
                receipt.MaterialIds, receipt.ActorRecruitId, receipt.AssistantRecruitId,
                receipt.DieOne, receipt.DieTwo, receipt.Modifier, receipt.AppliedVersion);
            campaign = WithWorldGate(campaign, WorldGate(campaign).With(
                activeOperation: active.With(pendingReceipt: tampered,
                    replacePendingReceipt: true), replaceActiveOperation: true));
            var treasury = campaign.Guild.TreasuryXp;
            var rejected = _worldGate023.ApplyNodeReceiptExactlyOnce(campaign, _catalog023);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "CAMPAIGN023_PENDING_RECEIPT_INVALID");
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasury));
        }

        [Test]
        public void ThirtyHistoricalBoardsAndTenEndlessBattlesKeepSavedSemanticTiles()
        {
            Assert.That(_registry022.AbyssOperations.Count, Is.EqualTo(40));
            Assert.That(_registry022.AbyssOperations.Values.Sum(value => value.steps.Length),
                Is.EqualTo(200));
            Assert.That(_registry022.AbyssOperations.Values.Count(value => value.kind == "RECON"),
                Is.EqualTo(10));
            Assert.That(_registry022.AbyssOperations.Values.Count(value => value.kind == "TRIAL"),
                Is.EqualTo(10));
            Assert.That(_registry022.AbyssOperations.Values.Count(value => value.kind == "GUARDIAN"),
                Is.EqualTo(10));
            Assert.That(_registry022.AbyssOperations.Values.Count(value =>
                value.kind == CampaignProgressionCommandService022.EndlessBattleKind094), Is.EqualTo(10));
            foreach (var operation in _registry022.AbyssOperations.Values)
            {
                Assert.That(TowerAdventureRules084.IsCompatible084(
                    operation, out var error), Is.True, operation.operationId + ": " + error);
                var state = new AbyssOperationState022(
                    "ABYSSRUN022_TEST_" + operation.operationId,
                    operation.operationId, operation.floorId, 0,
                    AbyssOperationStatus022.Active, Array.Empty<string>(), null,
                    string.Empty, "SEED_" + operation.operationId, 0,
                    Array.Empty<AbyssStepReceiptProof022>());
                var reloaded = JsonConvert.DeserializeObject<AbyssOperationState022>(
                    JsonConvert.SerializeObject(state));
                Assert.That(reloaded.OperationDefinitionId,
                    Is.EqualTo(state.OperationDefinitionId));
                Assert.That(TowerAdventureRules084.TrackPhase084(
                        reloaded.CurrentStepIndex, reloaded.Status), Is.Zero);
                var tileLabels = operation.steps.Select(step =>
                    TowerAdventureRules084.TileLabel084(step.kind,
                        step.requiresBattle)).ToArray();
                Assert.That(tileLabels.Length, Is.EqualTo(5));
            }
            Assert.That(TowerAdventureRules084.TrackPhase084(
                5, AbyssOperationStatus022.ReadyToFinalize), Is.EqualTo(4));
        }

        [Test]
        public void TowerSelectionGatesFirstClearAndPreviousFloorWithoutHidingRoutes()
        {
            foreach (var operation in _registry022.AbyssOperations.Values)
            {
                var floor = _registry022.Floors[operation.floorId];
                Assert.That(TowerAdventureRules084.IsAvailable084(
                    operation, floor.floor, 0, floor.floor <= 1, false),
                    Is.EqualTo(floor.floor <= 1 && operation.firstClearOnly),
                    operation.operationId);
                Assert.That(TowerAdventureRules084.IsAvailable084(
                        operation, floor.floor, 0, true, false),
                    Is.EqualTo(operation.firstClearOnly), operation.operationId);
                Assert.That(TowerAdventureRules084.IsAvailable084(
                        operation, floor.floor, 1, true, true),
                    Is.EqualTo(!operation.firstClearOnly), operation.operationId);
            }
        }

        [Test]
        public void TowerServiceRejectsReconBeforeGuardianAndAllowsItAfterFirstClear()
        {
            var floor = _registry022.Floors.Values.Single(value => value.floor == 1);
            var guardian = _registry022.AbyssOperations.Values.Single(value =>
                value.floorId == floor.floorId && value.kind == "GUARDIAN");
            var recon = _registry022.AbyssOperations.Values.Single(value =>
                value.floorId == floor.floorId && value.kind == "RECON");
            var service = new CampaignProgressionCommandService022();
            var fresh = CreateTowerRosterCampaign084(85084);

            var rejected = service.BeginAbyssOperation(fresh, _registry022,
                recon.operationId);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "CAMPAIGN022_GUARDIAN_FIRST_CLEAR_REQUIRED");
            Assert.That(service.BeginAbyssOperation(fresh, _registry022,
                guardian.operationId).IsSuccess, Is.True);

            var cleared = new CampaignProgressionState022(
                "CAMPAIGN_PROGRESSION_ABYSS_COVENANT_022_1.0",
                Array.Empty<EquipmentEvolutionState022>(),
                Array.Empty<ClassCertificationState022>(),
                new[] { new AbyssFloorProgressState022(floor.floorId, 1, 1,
                    true, false, Array.Empty<string>()) }, null,
                Array.Empty<InvocationArtifactState022>(),
                Array.Empty<CovenantProgressState022>(), Array.Empty<string>(),
                Array.Empty<string>(), 0, "guardian_first_clear_legacy_084");
            var legacy = WithProgression(fresh, cleared);
            var postGuardian = Require(
                service.MigrateInactiveLegacyAbyssState084(legacy));
            var accepted = service.BeginAbyssOperation(postGuardian, _registry022,
                recon.operationId);
            Assert.That(accepted.IsSuccess, Is.True,
                string.Join("\n", accepted.Errors));
        }

        static CampaignStepRule020 Step(string id, string kind) =>
            new CampaignStepRule020
            {
                StepId = id, Kind = kind, Title = kind,
                RequiresCertifiedBattle = kind == "CERTIFIED_BATTLE"
            };

        static void AssertRoutesNeverMovePawnBackward(
            WorldGateBoardRule023 board,
            string nodeId,
            List<string> completedKinds,
            HashSet<string> path)
        {
            Assert.That(path.Add(nodeId), Is.True,
                board.DefinitionId + " contains a cycle at " + nodeId);
            var node = board.Nodes.Single(value => value.NodeId == nodeId);
            var room = BoardAdventureRules084.RoomKind084(node.Kind);
            var before = BoardAdventureRules084.MonotonicSemanticTrackPhase084(
                room, "Active", completedKinds);
            var nextCompleted = new List<string>(completedKinds) { room };
            var choices = node.ChoiceIds.Count == 0
                ? new[] { "CONTINUE" }
                : node.ChoiceIds;
            foreach (var choice in choices)
            {
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    node, choice, out var next), Is.True);
                if (string.IsNullOrWhiteSpace(next)) continue;
                var nextNode = board.Nodes.Single(value => value.NodeId == next);
                var after = BoardAdventureRules084.MonotonicSemanticTrackPhase084(
                    BoardAdventureRules084.RoomKind084(nextNode.Kind),
                    "Active", nextCompleted);
                Assert.That(after, Is.GreaterThanOrEqualTo(before),
                    board.DefinitionId + " pawn regressed from " + node.NodeId +
                    " to " + nextNode.NodeId);
                AssertRoutesNeverMovePawnBackward(board, next,
                    nextCompleted, new HashSet<string>(path, StringComparer.Ordinal));
            }
        }

        CampaignState CreateAtWorldBoard(string chapterId, string worldId, long seed)
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
            var campaign = source.With(guild, source.OpeningFlow);
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
            campaign = Require(_campaign020.BeginOperation(campaign, _catalog020, chapterId));
            campaign = Require(_campaign020.CommitNonBattleStep(
                campaign, _catalog020, "SUCCESS"));
            campaign = Require(_campaign020.ApplyStepReceiptExactlyOnce(
                campaign, _catalog020));
            return campaign;
        }

        static CampaignState CreateTowerRosterCampaign084(long seed)
        {
            var recruitA = new RecruitState("TOWER_RECRUIT_A_084", 100, 100, 20, 20);
            var recruitB = new RecruitState("TOWER_RECRUIT_B_084", 100, 100, 20, 20);
            var union = new UnionState("TOWER_UNION_084", "Tower Union",
                UnionKind.Normal, recruitA.RecruitId,
                new[] { recruitA.RecruitId, recruitB.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(source.Guild.GuildId,
                source.Guild.TreasuryXp, new[] { recruitA, recruitB },
                new[] { union }, source.Guild.Inventory,
                source.Guild.Development, guildCity: null);
            return source.With(guild, source.OpeningFlow);
        }

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023;

        static CampaignState WithWorldGate(
            CampaignState campaign,
            WorldGateRuntimeState023 runtime)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(worldGate023: runtime,
                replaceWorldGate023: true, lastCheckpointId: runtime.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: runtime.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: runtime.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: runtime.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState WithProgression(
            CampaignState campaign,
            CampaignProgressionState022 progression022)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(
                progression022: progression022, replaceProgression022: true,
                lastCheckpointId: progression022.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: progression022.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: progression022.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progression022.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
