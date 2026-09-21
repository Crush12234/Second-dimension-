using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.People029;
using SecondDimension.Presentation.RecruitChronicles025;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RecruitChronicleBoardProjection084Tests
    {
        [Test]
        public void AllThreeHundredSixtyAuthoredBoardsProjectAllThreeThousandSixHundredRooms()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            var projectedBoards = 0;
            var projectedRooms = 0;

            foreach (var board in registry.Boards.Values.OrderBy(value => value.boardId, StringComparer.Ordinal))
            {
                var view = RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], null);
                projectedBoards++;
                projectedRooms += view.Tiles.Count;
                Assert.That(view.Tiles.Count, Is.EqualTo(board.nodes.Length), board.boardId);
                Assert.That(view.Tiles.Select(value => value.NodeId),
                    Is.EqualTo(board.nodes.Select(value => value.nodeId)), board.boardId);
                Assert.That(view.Tiles[0].State,
                    Is.EqualTo(RecruitChronicleBoardProjection084.RevealedState), board.boardId);
                Assert.That(view.Tiles.Skip(1).All(value => value.IsFaceDown), Is.True, board.boardId);
                Assert.That(view.Destinations.Select(value => value.DestinationNodeId),
                    Is.EqualTo(board.nodes[0].nextNodeIds), board.boardId);
            }

            Assert.That(projectedBoards, Is.EqualTo(360));
            Assert.That(projectedRooms, Is.EqualTo(3600));
        }

        [Test]
        public void SixCardChronicleShelfPagesExposeEveryAuthoredBoardExactlyOnce()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            var quests = registry.Boards.Values
                .OrderBy(value => value.boardId, StringComparer.Ordinal)
                .Select(board => RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], null))
                .ToArray();

            var pageCount = M1FlowPresenter.PersonalQuestShelfPageCountForVerification029(quests.Length);
            Assert.That(pageCount, Is.EqualTo(60));

            var visibleAcrossPages = new List<PersonalQuestView029>();
            for (var page = 0; page < pageCount; page++)
            {
                var pageQuests = M1FlowPresenter.PersonalQuestShelfPageForVerification029(quests, page);
                Assert.That(pageQuests.Count, Is.EqualTo(6), "page " + page);
                visibleAcrossPages.AddRange(pageQuests);
            }

            Assert.That(visibleAcrossPages.Select(value => value.BoardId),
                Is.EqualTo(quests.Select(value => value.BoardId)));
            Assert.That(visibleAcrossPages.Select(value => value.BoardId).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(360));
            Assert.That(M1FlowPresenter.PersonalQuestShelfPageForVerification029(quests, -1)
                .Select(value => value.BoardId),
                Is.EqualTo(quests.Take(6).Select(value => value.BoardId)));
            Assert.That(M1FlowPresenter.PersonalQuestShelfPageForVerification029(quests, int.MaxValue)
                .Select(value => value.BoardId),
                Is.EqualTo(quests.Skip(354).Select(value => value.BoardId)));
        }

        [Test]
        public void EveryProjectedRoomUsesOnlyExistingCommandDestinations()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            var projectedRooms = 0;
            var projectedRoutes = 0;
            var battleRooms = 0;

            foreach (var board in registry.Boards.Values)
            foreach (var node in board.nodes)
            {
                var progress = ProgressAt(board, node.nodeId, Array.Empty<string>(), false);
                var view = RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], progress);
                var destinations = view.Destinations.Select(value => value.DestinationNodeId).ToArray();
                Assert.That(destinations, Is.EqualTo(node.nextNodeIds), node.nodeId);
                Assert.That(destinations.All(value => RecruitChronicleRules025.CanMove(board, node.nodeId, value)),
                    Is.True, node.nodeId);
                Assert.That(view.RequiresCertifiedBattle,
                    Is.EqualTo(StringComparer.Ordinal.Equals(node.kind, "BATTLE")), node.nodeId);
                projectedRooms++;
                projectedRoutes += destinations.Length;
                if (view.RequiresCertifiedBattle) battleRooms++;
            }

            Assert.That(projectedRooms, Is.EqualTo(3600));
            Assert.That(projectedRoutes, Is.EqualTo(3960));
            Assert.That(battleRooms, Is.EqualTo(360));
        }

        [Test]
        public void BranchProgressProducesClearedCurrentRevealedClosedAndFaceDownTiles()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var profile = ProfilesByBoard(registry)[board.boardId];
            var completed = board.nodes.Take(3).Select(value => value.nodeId).ToArray();
            var progress = ProgressAt(board, board.nodes[4].nodeId, completed, false);
            var view = RecruitChronicleBoardProjection084.Project(board, profile, progress);

            Assert.That(view.Tiles.Take(3).All(value => value.State == RecruitChronicleBoardProjection084.ClearedState), Is.True);
            Assert.That(view.Tiles[3].State, Is.EqualTo(RecruitChronicleBoardProjection084.UnchosenState));
            Assert.That(view.Tiles[4].State, Is.EqualTo(RecruitChronicleBoardProjection084.CurrentState));
            Assert.That(view.Tiles[5].State, Is.EqualTo(RecruitChronicleBoardProjection084.SelectableFaceDownState));
            Assert.That(view.Tiles[6].State, Is.EqualTo(RecruitChronicleBoardProjection084.SelectableFaceDownState));
            Assert.That(view.Tiles[5].RoomLabel, Is.EqualTo("Face-down route"));
            Assert.That(view.Tiles[6].RoomLabel, Is.EqualTo("Face-down route"));
            Assert.That(view.Tiles.Skip(7).All(value => value.State == RecruitChronicleBoardProjection084.FaceDownState), Is.True);
            Assert.That(view.Destinations.Select(value => value.DestinationNodeId), Is.EqualTo(board.nodes[4].nextNodeIds));
            Assert.That(view.Destinations.Select(value => value.ButtonLabel),
                Is.EqualTo(new[] {"CHOOSE LEFT PATH", "CHOOSE RIGHT PATH"}));
            Assert.That(VisibleCopy(view), Does.Not.Contain("Danger Room"));
            Assert.That(VisibleCopy(view), Does.Not.Contain("Turning Point"));

            var restored = JsonConvert.DeserializeObject<PersonalQuestProgressState025>(
                JsonConvert.SerializeObject(progress));
            var afterReload = RecruitChronicleBoardProjection084.Project(board, profile, restored);
            Assert.That(ProjectionSnapshot(afterReload), Is.EqualTo(ProjectionSnapshot(view)));
        }

        [Test]
        public void VisibleBoardCopyNeverLeaksAuthorityOrDestinationIds()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            foreach (var board in registry.Boards.Values)
            {
                var profile = profiles[board.boardId];
                var progress = ProgressAt(board, board.nodes[4].nodeId,
                    board.nodes.Take(4).Select(value => value.nodeId).ToArray(), false);
                var visible = VisibleCopy(RecruitChronicleBoardProjection084.Project(board, profile, progress));
                var rawIds = new List<string> {board.boardId, board.recruitId, profile.worldId};
                foreach (var node in board.nodes)
                {
                    rawIds.Add(node.nodeId);
                    rawIds.Add(node.battleProfileId);
                    rawIds.Add(node.checkDimensionId);
                    rawIds.Add(node.rewardMemoryId);
                    rawIds.AddRange(node.choiceIds ?? Array.Empty<string>());
                }
                foreach (var rawId in rawIds.Where(value => !string.IsNullOrWhiteSpace(value)))
                    Assert.That(visible.IndexOf(rawId, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1),
                        board.boardId + " leaked " + rawId);
                Assert.That(visible, Does.Not.Contain("PQ025_"));
                Assert.That(visible, Does.Not.Contain("SIGREC_"));
                Assert.That(visible, Does.Not.Contain("WORLD_"));
                Assert.That(visible, Does.Not.Contain("RELDIM025_"));
                Assert.That(visible, Does.Not.Contain("ENCOUNTER025_"));
                Assert.That(visible, Does.Not.Contain("MEMORY025_"));
                Assert.That(visible, Does.Not.Contain(": Step "));
                Assert.That(visible, Does.Not.Contain(" step tests "));
            }
        }

        [Test]
        public void EveryAuthoredBoardStopsAtBattleAndCompletesOnlyThroughCanonicalVictoryReturn()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            var commands = new RecruitChronicleCommandService025(registry);
            var battles = new RecruitChronicleBattleService025(registry);
            var bridge = new GuildCityBattleBridgeService017D();
            var completedBoards = 0;

            foreach (var board in registry.Boards.Values.OrderBy(value => value.boardId, StringComparer.Ordinal))
            {
                var campaign = CreateCampaign(board.recruitId);
                var operationOrdinal = campaign.Guild.GuildCity.OperationOrdinal;
                campaign = Require(commands.StartPersonalQuest(campaign, board), board.boardId);
                var guard = 0;
                var progress = Quest(campaign, board.boardId);
                while (!StringComparer.Ordinal.Equals(
                           RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId).kind, "BATTLE"))
                {
                    var node = RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId);
                    Assert.That(node, Is.Not.Null, board.boardId);
                    var view = RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], progress);
                    var destination = NextTowardKind(board, node, "BATTLE");
                    Assert.That(view.Destinations.Any(value => value.DestinationNodeId == destination), Is.True, node.nodeId);
                    var receipt = RecruitChronicleRules025.PersonalQuestMoveReceiptId(
                        campaign.CampaignSeed, board.boardId, destination, board.recruitId);
                    campaign = Require(commands.AdvancePersonalQuest(campaign, board, destination, receipt), node.nodeId);
                    progress = Quest(campaign, board.boardId);
                    guard++;
                    Assert.That(guard, Is.LessThan(11), board.boardId);
                }

                var battleNode = RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId);
                var battleDestination = battleNode.nextNodeIds.Single();
                var directReceipt = RecruitChronicleRules025.PersonalQuestMoveReceiptId(
                    campaign.CampaignSeed, board.boardId, battleDestination, board.recruitId);
                var beforeDirectSkip = CanonicalJson.Sha256Hex(campaign);
                var directSkip = commands.AdvancePersonalQuest(campaign, board, battleDestination, directReceipt);
                Assert.That(directSkip.IsSuccess, Is.False, board.boardId);
                Assert.That(directSkip.Errors, Does.Contain("CHRONICLE025_CERTIFIED_BATTLE_RETURN_REQUIRED"), board.boardId);
                Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(beforeDirectSkip), board.boardId);

                campaign = Require(battles.CommitEncounter(campaign, board, new[] {"U1"}), board.boardId);
                campaign = AttachClaimedBattle(campaign, CreateTerminalBattle(campaign.Guild.GuildCity.PendingEncounter,
                    board.recruitId, BattleOutcome.Victory, true));
                campaign = Require(bridge.CommitBattleReturn(campaign), board.boardId);
                campaign = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
                campaign = Require(battles.ApplyClaimedBattleReturn(campaign, board), board.boardId);
                progress = Quest(campaign, board.boardId);
                Assert.That(progress.CurrentNodeId, Is.EqualTo(battleDestination), board.boardId);
                Assert.That(progress.ExistingBattleRewardReceiptId, Is.EqualTo(campaign.Battle.Reward.RewardId), board.boardId);
                Assert.That(progress.AppliedReceiptIds.Count(value=>value.StartsWith(
                    "PQ_BATTLE_PROOF025_",StringComparison.Ordinal)),Is.EqualTo(1),board.boardId);
                Assert.That(campaign.Guild.GuildCity.PendingEncounter, Is.Null, board.boardId);
                Assert.That(campaign.Guild.GuildCity.PendingBattleReturn, Is.Null, board.boardId);

                var beforeReplay = CanonicalJson.Sha256Hex(campaign);
                var replay = battles.ApplyClaimedBattleReturn(campaign, board);
                Assert.That(replay.IsSuccess, Is.False, board.boardId);
                Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(beforeReplay), board.boardId);

                guard = 0;
                while (!progress.Completed)
                {
                    var node = RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId);
                    Assert.That(node.kind, Is.Not.EqualTo("BATTLE"), board.boardId);
                    var destination = node.nextNodeIds[0];
                    var receipt = RecruitChronicleRules025.PersonalQuestMoveReceiptId(
                        campaign.CampaignSeed, board.boardId, destination, board.recruitId);
                    campaign = Require(commands.AdvancePersonalQuest(campaign, board, destination, receipt), node.nodeId);
                    progress = Quest(campaign, board.boardId);
                    guard++;
                    Assert.That(guard, Is.LessThan(11), board.boardId);
                }

                Assert.That(progress.Completed, Is.True, board.boardId);
                Assert.That(RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId).kind,
                    Is.EqualTo("RETURN"), board.boardId);
                Assert.That(campaign.Guild.GuildCity.OperationOrdinal, Is.EqualTo(operationOrdinal), board.boardId);

                var beforeReload = RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], progress);
                var restored = JsonConvert.DeserializeObject<PersonalQuestProgressState025>(
                    JsonConvert.SerializeObject(progress));
                var afterReload = RecruitChronicleBoardProjection084.Project(board, profiles[board.boardId], restored);
                Assert.That(ProjectionSnapshot(afterReload), Is.EqualTo(ProjectionSnapshot(beforeReload)), board.boardId);
                Assert.That(afterReload.Tiles.Last().State,
                    Is.EqualTo(RecruitChronicleBoardProjection084.ReturnedState), board.boardId);
                completedBoards++;
            }

            Assert.That(completedBoards, Is.EqualTo(360));
        }

        [Test]
        public void OnlyOneChronicleCanBeActiveWhileSameBoardStartRemainsIdempotent()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profile = registry.Profiles.Values.First(value => value.personalQuestBoardIds.Length == 2);
            var firstBoard = registry.Boards[profile.personalQuestBoardIds[0]];
            var secondBoard = registry.Boards[profile.personalQuestBoardIds[1]];
            var commands = new RecruitChronicleCommandService025(registry);
            var campaign = Require(commands.StartPersonalQuest(CreateCampaign(profile.recruitId), firstBoard), firstBoard.boardId);
            var afterFirst = CanonicalJson.Sha256Hex(campaign);

            campaign = Require(commands.StartPersonalQuest(campaign, firstBoard), firstBoard.boardId);
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(afterFirst));

            var blocked = commands.StartPersonalQuest(campaign, secondBoard);
            AssertRejectedWithoutDelta(blocked, campaign, "CHRONICLE025_FINISH_ACTIVE_QUEST_FIRST");
        }

        [Test]
        public void InvalidLegacyChronicleIsArchivedWithoutLossAndRestartedFromCanonicalEntry()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profile = registry.Profiles.Values.First(value => value.personalQuestBoardIds.Length == 2);
            var board = registry.Boards[profile.personalQuestBoardIds[0]];
            var completedBoard = registry.Boards[profile.personalQuestBoardIds[1]];
            var commands = new RecruitChronicleCommandService025(registry);
            var campaign = CreateCampaign(profile.recruitId);
            var development = campaign.Guild.Development.RecordBattleReward("LEGACY_REWARD_084", 7, 5);
            campaign = campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory, development),
                campaign.OpeningFlow);
            var invalid = new PersonalQuestProgressState025(board.boardId, board.recruitId,
                board.nodes[4].nodeId, new[] {board.entryNodeId}, new[] {"FORGED_LEGACY_LEDGER_084"},
                false, "LEGACY_REWARD_084", "legacy_invalid");
            var completed = new PersonalQuestProgressState025(completedBoard.boardId, completedBoard.recruitId,
                completedBoard.nodes.Last().nodeId, completedBoard.nodes.Take(9).Select(value => value.nodeId).ToArray(),
                new[] {"LEGACY_COMPLETED_HISTORY_084"}, true, "LEGACY_REWARD_084", "legacy_complete");
            var chronicles = RecruitChronicleState025.Default().With(personalQuests: new[] {invalid, completed});
            campaign = WithChronicles(campaign, chronicles);
            var developmentBefore = CanonicalJson.Sha256Hex(campaign.Guild.Development);
            var completedBefore = CanonicalJson.Sha256Hex(completed);
            var invalidBefore = CanonicalJson.Sha256Hex(invalid);

            var recovered = Require(commands.RecoverInvalidPersonalQuest(campaign, board.boardId), board.boardId);
            var recoveredState = recovered.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
            var archived = recoveredState.PersonalQuests.Single(value =>
                CanonicalJson.Sha256Hex(value) == invalidBefore);
            var clean = RecruitChronicleRules025.ActiveQuest(recoveredState, recovered.CampaignSeed, board.boardId);
            Assert.That(RecruitChronicleRules025.IsArchivedLegacyQuest(recoveredState,
                recovered.CampaignSeed, archived), Is.True);
            Assert.That(clean, Is.Not.Null);
            Assert.That(clean.CurrentNodeId, Is.EqualTo(board.entryNodeId));
            Assert.That(clean.CompletedNodeIds, Is.Empty);
            Assert.That(clean.AppliedReceiptIds, Is.Empty);
            Assert.That(clean.ExistingBattleRewardReceiptId, Is.Empty);
            Assert.That(recoveredState.PersonalQuests.Any(value =>
                CanonicalJson.Sha256Hex(value) == completedBefore), Is.True,
                "Completed Chronicle history must remain byte-for-byte equivalent.");
            Assert.That(CanonicalJson.Sha256Hex(recovered.Guild.Development), Is.EqualTo(developmentBefore));
            Assert.That(recovered.Guild.Development.HasClaimedReward("LEGACY_REWARD_084"), Is.True);

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(recovered));
            var reloadedState = reloaded.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
            Assert.That(RecruitChronicleRules025.ActiveQuest(reloadedState,reloaded.CampaignSeed,board.boardId).CurrentNodeId,
                Is.EqualTo(board.entryNodeId));
            Assert.That(RecruitChronicleRules025.IsArchivedLegacyQuest(reloadedState,reloaded.CampaignSeed,
                reloadedState.PersonalQuests.Single(value => CanonicalJson.Sha256Hex(value) == invalidBefore)), Is.True);
        }

        [Test]
        public void UnknownLegacyChronicleIsPreservedAndArchivedSoKnownBoardsCanStart()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands = new RecruitChronicleCommandService025(registry);
            var campaign = CreateCampaign(board.recruitId);
            var unknown = new PersonalQuestProgressState025("UNKNOWN_LEGACY_BOARD_084", board.recruitId,
                "UNKNOWN_LEGACY_ROOM_084", new[] {"OLD_ROOM_084"}, new[] {"OLD_RECEIPT_084"},
                false, string.Empty, "legacy_unknown");
            campaign = WithChronicles(campaign, RecruitChronicleState025.Default().With(
                personalQuests: new[] {unknown}));
            var unknownBefore = CanonicalJson.Sha256Hex(unknown);

            campaign = Require(commands.RecoverInvalidPersonalQuest(campaign, unknown.BoardId), unknown.BoardId);
            var recoveredState = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
            var preserved = recoveredState.PersonalQuests.Single(value => CanonicalJson.Sha256Hex(value) == unknownBefore);
            Assert.That(RecruitChronicleRules025.IsArchivedLegacyQuest(recoveredState,
                campaign.CampaignSeed, preserved), Is.True);
            Assert.That(RecruitChronicleRules025.ActiveQuest(recoveredState,campaign.CampaignSeed,unknown.BoardId), Is.Null);
            campaign = Require(commands.StartPersonalQuest(campaign, board), board.boardId);
            Assert.That(RecruitChronicleRules025.ActiveQuest(
                campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025,
                campaign.CampaignSeed,board.boardId), Is.Not.Null);
        }

        [Test]
        public void ArchivedUnknownChronicleQuarantinesOnlyItsCanonicalHandoffWithoutGrantingAReward()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);
            var bridge=new GuildCityBattleBridgeService017D();
            var campaign=CreateCampaign(board.recruitId);
            var archived=new PersonalQuestProgressState025("UNKNOWN_LEGACY_BOARD_084",board.recruitId,
                "UNKNOWN_LEGACY_BATTLE_ROOM_084",new[]{"OLD_ROOM_084"},new[]{"OLD_RECEIPT_084"},
                false,string.Empty,"legacy_archived");
            var completed=new PersonalQuestProgressState025("COMPLETED_HISTORY_084",board.recruitId,
                "RETURNED_HOME_084",new[]{"OLD_COMPLETE_ROOM_084"},new[]{"OLD_COMPLETE_RECEIPT_084"},
                true,"OLD_EARNED_REWARD_084","legacy_complete");
            var archiveReceipt=RecruitChronicleRules025.LegacyQuestArchiveReceiptId(campaign.CampaignSeed,archived);
            campaign=WithChronicles(campaign,RecruitChronicleState025.Default().With(
                personalQuests:new[]{archived,completed},appliedReceiptIds:new[]{archiveReceipt}));
            var request=CreateLegacyChronicleRequest(campaign,archived);
            campaign=WithPendingHandoff(campaign,request);
            var battle=CreateTerminalBattle(request,board.recruitId,BattleOutcome.Victory,false);
            campaign=Require(bridge.CommitBattleReturn(campaign.WithBattle(battle)),archived.BoardId);
            var developmentBefore=CanonicalJson.Sha256Hex(campaign.Guild.Development);
            var inventoryBefore=CanonicalJson.Sha256Hex(campaign.Guild.Inventory);
            var completedBefore=CanonicalJson.Sha256Hex(completed);
            var archivedBefore=CanonicalJson.Sha256Hex(archived);
            var appliedReturnsBefore=CanonicalJson.Sha256Hex(campaign.Guild.GuildCity.AppliedBattleReturnIds);

            var recovered=Require(commands.RecoverInvalidPersonalQuest(campaign,archived.BoardId),archived.BoardId);
            var state=recovered.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
            Assert.That(recovered.Guild.GuildCity.PendingEncounter,Is.Null);
            Assert.That(recovered.Guild.GuildCity.PendingBattleReturn,Is.Null);
            Assert.That(recovered.Battle,Is.Null,"Only the exactly linked legacy battle is quarantined.");
            Assert.That(state.PersonalQuests.Any(value=>CanonicalJson.Sha256Hex(value)==archivedBefore),Is.True);
            Assert.That(state.PersonalQuests.Any(value=>CanonicalJson.Sha256Hex(value)==completedBefore),Is.True);
            Assert.That(state.AppliedReceiptIds.Any(value=>value.StartsWith(
                "CHRONICLE_HANDOFF_ARCHIVE025_",StringComparison.Ordinal)),Is.True);
            Assert.That(CanonicalJson.Sha256Hex(recovered.Guild.Development),Is.EqualTo(developmentBefore));
            Assert.That(CanonicalJson.Sha256Hex(recovered.Guild.Inventory),Is.EqualTo(inventoryBefore));
            Assert.That(recovered.Guild.TreasuryXp,Is.EqualTo(campaign.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Sha256Hex(recovered.Guild.GuildCity.AppliedBattleReturnIds),
                Is.EqualTo(appliedReturnsBefore),"Quarantine is not a battle-return application.");
            Assert.That(recovered.Guild.Development.HasClaimedReward(battle.Reward.RewardId),Is.False);

            var reloaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(recovered));
            Assert.That(reloaded.Guild.GuildCity.PendingEncounter,Is.Null);
            Assert.That(reloaded.Guild.GuildCity.PendingBattleReturn,Is.Null);
            Assert.That(reloaded.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025
                .AppliedReceiptIds.Any(value=>value.StartsWith("CHRONICLE_HANDOFF_ARCHIVE025_",StringComparison.Ordinal)),Is.True);
        }

        [Test]
        public void ArchivedChronicleRecoveryFailsClosedForTamperedOrOrphanedHandoffWithoutStateDelta()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);
            var bridge=new GuildCityBattleBridgeService017D();
            var archived=new PersonalQuestProgressState025("UNKNOWN_LEGACY_BOARD_084",board.recruitId,
                "UNKNOWN_LEGACY_BATTLE_ROOM_084",Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"legacy_archived");
            var campaign=CreateCampaign(board.recruitId);
            campaign=WithChronicles(campaign,RecruitChronicleState025.Default().With(personalQuests:new[]{archived},
                appliedReceiptIds:new[]{RecruitChronicleRules025.LegacyQuestArchiveReceiptId(campaign.CampaignSeed,archived)}));
            var canonical=CreateLegacyChronicleRequest(campaign,archived);
            var tampered=CopyEncounter(canonical,routeModifiers:new[]{"PERSONAL_QUEST_029","RECRUIT_FORGED_084"});
            var tamperedCampaign=WithPendingHandoff(campaign,tampered);
            AssertRejectedWithoutDelta(commands.RecoverInvalidPersonalQuest(tamperedCampaign,archived.BoardId),
                tamperedCampaign,"CHRONICLE025_RECOVERY_HANDOFF_LINK_UNPROVEN");

            var withReturn=Require(bridge.CommitBattleReturn(
                WithPendingHandoff(campaign,canonical).WithBattle(
                    CreateTerminalBattle(canonical,board.recruitId,BattleOutcome.Defeat,false))),archived.BoardId);
            var orphanedCity=withReturn.Guild.GuildCity.With(pendingEncounter:null,replacePendingEncounter:true);
            var orphanedReturn=withReturn.With(withReturn.Guild.WithGuildCity(orphanedCity),withReturn.OpeningFlow);
            AssertRejectedWithoutDelta(commands.RecoverInvalidPersonalQuest(orphanedReturn,archived.BoardId),
                orphanedReturn,"CHRONICLE025_RECOVERY_HANDOFF_LINK_UNPROVEN");
        }

        [Test]
        public void ArchivedChronicleQuarantinePreservesAnUnrelatedBattleExactly()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);
            var archived=new PersonalQuestProgressState025("UNKNOWN_LEGACY_BOARD_084",board.recruitId,
                "UNKNOWN_LEGACY_BATTLE_ROOM_084",Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"legacy_archived");
            var campaign=CreateCampaign(board.recruitId);
            campaign=WithChronicles(campaign,RecruitChronicleState025.Default().With(personalQuests:new[]{archived},
                appliedReceiptIds:new[]{RecruitChronicleRules025.LegacyQuestArchiveReceiptId(campaign.CampaignSeed,archived)}));
            var request=CreateLegacyChronicleRequest(campaign,archived);
            var unrelatedProgress=new PersonalQuestProgressState025("UNRELATED_BOARD_084",board.recruitId,
                "UNRELATED_ROOM_084",Array.Empty<string>(),Array.Empty<string>(),false,string.Empty,"unrelated");
            var unrelatedBattle=CreateTerminalBattle(CreateLegacyChronicleRequest(campaign,unrelatedProgress),
                board.recruitId,BattleOutcome.Defeat,false);
            var candidate=WithPendingHandoff(campaign,request).WithBattle(unrelatedBattle);
            var unrelatedHash=CanonicalJson.Sha256Hex(unrelatedBattle);

            var recovered=Require(commands.RecoverInvalidPersonalQuest(candidate,archived.BoardId),archived.BoardId);
            Assert.That(recovered.Guild.GuildCity.PendingEncounter,Is.Null);
            Assert.That(recovered.Battle,Is.Not.Null);
            Assert.That(CanonicalJson.Sha256Hex(recovered.Battle),Is.EqualTo(unrelatedHash));
        }

        [Test]
        public void ValidChronicleCannotBeArchivedThroughRecoveryRoute()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands = new RecruitChronicleCommandService025(registry);
            var campaign = Require(commands.StartPersonalQuest(CreateCampaign(board.recruitId), board), board.boardId);
            AssertRejectedWithoutDelta(commands.RecoverInvalidPersonalQuest(campaign,board.boardId),campaign,
                "CHRONICLE025_RECOVERY_NOT_REQUIRED");
        }

        [Test]
        public void CampaignOperationBlocksChronicleStartAndBattleCommitWithoutStateDelta()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);
            var battles=new RecruitChronicleBattleService025(registry);
            var operation=new SecondDimension.Gameplay.Campaign020.CampaignPlayableOperationState020(
                "OPERATION_ACTIVE_084","BLUEPRINT_ACTIVE_084","CHAPTER_ACTIVE_084","SKYHOME",
                "SEED_ACTIVE_084",0,SecondDimension.Gameplay.Campaign020.CampaignPlayableOperationStatus020.Active,
                Array.Empty<string>(),Array.Empty<string>(),null,string.Empty,"active");
            var activeCampaign=WithCampaignOperation(CreateCampaign(board.recruitId),operation);
            AssertRejectedWithoutDelta(commands.StartPersonalQuest(activeCampaign,board),activeCampaign,
                "CHRONICLE025_FINISH_ACTIVE_ADVENTURE_FIRST");

            var chronicle=Require(commands.StartPersonalQuest(CreateCampaign(board.recruitId),board),board.boardId);
            chronicle=AdvanceToBattle(chronicle,board,commands);
            chronicle=WithCampaignOperation(chronicle,operation);
            AssertRejectedWithoutDelta(battles.CommitEncounter(chronicle,board,new[]{"U1"}),chronicle,
                "CHRONICLE025_CAMPAIGN_OPERATION_ACTIVE");
        }

        [Test]
        public void ChronicleSavedEncounterMutationCannotCrossTheSharedBattleBoundary()
        {
            var registry=RecruitChronicleRegistry025.LoadFromResources();
            var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);
            var chronicleBattles=new RecruitChronicleBattleService025(registry);
            var bridge=new GuildCityBattleBridgeService017D();
            var campaign=Require(commands.StartPersonalQuest(
                CreateCampaign(board.recruitId),board),board.boardId);
            campaign=AdvanceToBattle(campaign,board,commands);
            campaign=Require(chronicleBattles.CommitEncounter(
                campaign,board,new[]{"U1"}),board.boardId);
            var request=campaign.Guild.GuildCity.PendingEncounter;
            var authorityId=GuildCityBattleBridgeService017D
                .EncounterRequestAuthorityId084(request);
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                authorityId),Is.True);

            var claimed=AttachClaimedBattle(campaign,CreateTerminalBattle(
                request,board.recruitId,BattleOutcome.Victory,true));
            var committedReturn=Require(bridge.CommitBattleReturn(claimed),
                board.boardId);
            var unanchored=WithoutAdventureAuthority084(
                committedReturn,authorityId);
            var unanchoredHash=CanonicalJson.Sha256Hex(unanchored);
            var unanchoredRejected=chronicleBattles.ApplyClaimedBattleReturn(
                unanchored,board);
            Assert.That(unanchoredRejected.IsSuccess,Is.False);
            Assert.That(unanchoredRejected.Errors,Does.Contain(
                "CHRONICLE025_ENCOUNTER_AUTHORITY_INVALID"));
            Assert.That(CanonicalJson.Sha256Hex(unanchored),
                Is.EqualTo(unanchoredHash));

            var mutatedRequest=CopyEncounter(request,
                objective:request.Objective+" (mutated saved request)");
            var mutated=WithPendingHandoff(campaign,mutatedRequest);
            var before=CanonicalJson.Sha256Hex(mutated);
            var combat=M2CombatContent.LoadFromDirectory(System.IO.Path.Combine(
                Application.streamingAssetsPath,"Authority","CONTENT"));
            var rejected=bridge.StartCertifiedEncounter(
                mutated,new M2BattleCommandService(),combat);
            Assert.That(rejected.IsSuccess,Is.False);
            Assert.That(rejected.Errors,Does.Contain(
                "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED"));
            Assert.That(CanonicalJson.Sha256Hex(mutated),Is.EqualTo(before));
            Assert.That(mutated.Battle,Is.Null);

            var canonical=bridge.StartCertifiedEncounter(
                campaign,new M2BattleCommandService(),combat);
            Assert.That(canonical.IsSuccess,Is.True,
                string.Join("\n",canonical.Errors));
            Assert.That(canonical.Value.Battle.BattleId,Is.EqualTo(request.BattleId));
        }

        [Test]
        public void ClosedWorldBoardBattleAndProgressAuthorityRejectEveryForgedSurfaceWithoutDelta()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands = new RecruitChronicleCommandService025(registry);
            var battles = new RecruitChronicleBattleService025(registry);
            var bridge = new GuildCityBattleBridgeService017D();
            var campaign = AddInvalidTestUnions(CreateCampaign(board.recruitId), board.recruitId);
            var alteredNodes = board.nodes.Select(CopyNode).ToArray();
            alteredNodes[0].nextNodeIds = new[] {board.nodes[board.nodes.Length - 1].nodeId};
            var altered = CopyBoard(board, nodes: alteredNodes);
            var unknown = CopyBoard(board, boardId: "PQ025_UNKNOWN_FORGED_084");
            var wrongRecruit = CopyBoard(board, recruitId: "FORGED_SIGNED_RECRUIT_084");

            AssertRejectedWithoutDelta(commands.StartPersonalQuest(campaign, altered), campaign,
                "CHRONICLE025_BOARD_CONTENT_MISMATCH");
            AssertRejectedWithoutDelta(commands.StartPersonalQuest(campaign, unknown), campaign,
                "CHRONICLE025_UNKNOWN_BOARD");
            AssertRejectedWithoutDelta(commands.StartPersonalQuest(campaign, wrongRecruit), campaign,
                "CHRONICLE025_BOARD_RECRUIT_MISMATCH");

            campaign = Require(commands.StartPersonalQuest(campaign, board), board.boardId);
            var entry = RecruitChronicleRules025.FindNode(board, board.entryNodeId);
            var entryDestination = entry.nextNodeIds[0];
            AssertRejectedWithoutDelta(commands.AdvancePersonalQuest(campaign, altered, entryDestination,
                RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed, board.boardId,
                    entryDestination, board.recruitId)), campaign, "CHRONICLE025_BOARD_CONTENT_MISMATCH");
            AssertRejectedWithoutDelta(commands.AdvancePersonalQuest(campaign, board, entryDestination,
                "FORGED_RECEIPT_084"), campaign, "CHRONICLE025_CANONICAL_MOVE_RECEIPT_REQUIRED");

            campaign = AdvanceToBattle(campaign, board, commands);
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, altered, new[] {"U1"}), campaign,
                "CHRONICLE025_BOARD_CONTENT_MISMATCH");
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, board, Array.Empty<string>()), campaign,
                "CHRONICLE025_ALLIED_UNION_REQUIRED");
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, board, new[] {"EXTERNAL_UNION_084"}), campaign,
                "CHRONICLE025_ALLIED_UNION_NOT_OWNED");
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, board, new[] {"PROTECTED_UNION_084"}), campaign,
                "CHRONICLE025_ALLIED_UNION_NORMAL_REQUIRED");
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, board, new[] {"EMPTY_UNION_084"}), campaign,
                "CHRONICLE025_ALLIED_UNION_NONEMPTY_REQUIRED");
            AssertRejectedWithoutDelta(battles.CommitEncounter(campaign, board, Enumerable.Repeat("U1", 11).ToArray()), campaign,
                "CHRONICLE025_ALLIED_UNION_LIMIT");

            campaign = Require(battles.CommitEncounter(campaign, board, new[] {"U1"}), board.boardId);
            var battle = CreateTerminalBattle(campaign.Guild.GuildCity.PendingEncounter,
                board.recruitId, BattleOutcome.Victory, true);
            var unrecorded = Require(bridge.CommitBattleReturn(campaign.WithBattle(battle)), board.boardId);
            AssertRejectedWithoutDelta(battles.ApplyClaimedBattleReturn(unrecorded, board), unrecorded,
                "CHRONICLE025_EXISTING_REWARD_LINK_REQUIRED");

            var canonicalReturn = Require(bridge.CommitBattleReturn(AttachClaimedBattle(campaign, battle)), board.boardId);
            AssertRejectedWithoutDelta(battles.ApplyClaimedBattleReturn(canonicalReturn, altered), canonicalReturn,
                "CHRONICLE025_BOARD_CONTENT_MISMATCH");

            var forgedHashBattle = canonicalReturn.Battle.With(finalStateHash: new string('0', 64));
            var forgedHash = canonicalReturn.WithBattle(forgedHashBattle);
            AssertRejectedWithoutDelta(battles.ApplyClaimedBattleReturn(forgedHash, board), forgedHash,
                "CHRONICLE025_BATTLE_HASH_INVALID");

            var forgedReceipt = CopyBattleReturn(canonicalReturn.Guild.GuildCity.PendingBattleReturn,
                battleResultHash: new string('1', 64));
            var forgedReceiptCity = canonicalReturn.Guild.GuildCity.With(pendingBattleReturn: forgedReceipt,
                replacePendingBattleReturn: true);
            var forgedReturn = canonicalReturn.With(canonicalReturn.Guild.WithGuildCity(forgedReceiptCity),
                canonicalReturn.OpeningFlow);
            AssertRejectedWithoutDelta(battles.ApplyClaimedBattleReturn(forgedReturn, board), forgedReturn,
                "CHRONICLE025_BATTLE_RETURN_IDENTITY_MISMATCH");

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(canonicalReturn));
            var completedBattle = Require(battles.ApplyClaimedBattleReturn(reloaded, board), board.boardId);
            Assert.That(Quest(completedBattle, board.boardId).CurrentNodeId,
                Is.EqualTo(RecruitChronicleRules025.FindNode(board, board.nodes[5].nodeId).nextNodeIds[0]));
            var withoutCurrentBattle=completedBattle.WithBattle(null);
            var postBattleQuest=Quest(withoutCurrentBattle,board.boardId);
            var postBattleNode=RecruitChronicleRules025.FindNode(board,postBattleQuest.CurrentNodeId);
            var nextAfterBattle=postBattleNode.nextNodeIds[0];
            var nextReceipt=RecruitChronicleRules025.PersonalQuestMoveReceiptId(withoutCurrentBattle.CampaignSeed,
                board.boardId,nextAfterBattle,board.recruitId);
            Assert.That(commands.AdvancePersonalQuest(withoutCurrentBattle,board,nextAfterBattle,nextReceipt).IsSuccess,
                Is.True,"The persisted board-bound battle proof must survive after another system clears BattleState.");
            var proofless=postBattleQuest.With(appliedReceiptIds:postBattleQuest.AppliedReceiptIds.Where(value=>
                !value.StartsWith("PQ_BATTLE_PROOF025_",StringComparison.Ordinal)).ToArray());
            var prooflessState=withoutCurrentBattle.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .RecruitChronicles025.With(personalQuests:new[]{proofless});
            var prooflessCampaign=WithChronicles(withoutCurrentBattle,prooflessState);
            AssertRejectedWithoutDelta(commands.AdvancePersonalQuest(prooflessCampaign,board,nextAfterBattle,nextReceipt),
                prooflessCampaign,"CHRONICLE025_PROGRESS");
        }

        [Test]
        public void ForgedPersistedPathCompletionAndReceiptLedgersAreRejected()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands = new RecruitChronicleCommandService025(registry);
            var valid = Require(commands.StartPersonalQuest(CreateCampaign(board.recruitId), board), board.boardId);
            var state = valid.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
            var cases = new[]
            {
                new PersonalQuestProgressState025(board.boardId, board.recruitId, "FORGED_NODE_084",
                    Array.Empty<string>(), Array.Empty<string>(), false, string.Empty, "forged"),
                new PersonalQuestProgressState025(board.boardId, board.recruitId, board.nodes[4].nodeId,
                    new[] {board.nodes[0].nodeId}, Array.Empty<string>(), false, string.Empty, "forged"),
                new PersonalQuestProgressState025(board.boardId, board.recruitId, board.nodes[9].nodeId,
                    board.nodes.Take(9).Select(value => value.nodeId).ToArray(), new[] {"FORGED_RECEIPT_084"},
                    true, string.Empty, "forged")
            };
            foreach (var forged in cases)
            {
                var forgedState = state.With(personalQuests: new[] {forged});
                var forgedCampaign = WithChronicles(valid, forgedState);
                var destination = board.nodes[0].nextNodeIds[0];
                var result = commands.AdvancePersonalQuest(forgedCampaign, board, destination,
                    RecruitChronicleRules025.PersonalQuestMoveReceiptId(forgedCampaign.CampaignSeed,
                        board.boardId, destination, board.recruitId));
                AssertRejectedWithoutDelta(result, forgedCampaign, forged.Completed
                    ? "CHRONICLE025_QUEST_NOT_STARTED"
                    : "CHRONICLE025_PROGRESS");
            }
        }

        [Test]
        public void EveryCurrentRoomUsesChronicleCopyInsteadOfAuthoredPlaceholderSteps()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var profiles = ProfilesByBoard(registry);
            var inspected = 0;
            foreach (var board in registry.Boards.Values)
            foreach (var node in board.nodes)
            {
                var path = PathToNode(board, node.nodeId);
                var progress = ProgressAt(board, node.nodeId,
                    path.Take(Math.Max(0, path.Count - 1)).Select(value => value.nodeId).ToArray(),
                    StringComparer.Ordinal.Equals(node.kind, "RETURN"));
                var visible = VisibleCopy(RecruitChronicleBoardProjection084.Project(board,
                    profiles[board.boardId], progress));
                Assert.That(visible, Does.Not.Contain(": Step "), node.nodeId);
                Assert.That(visible, Does.Not.Contain(" step tests "), node.nodeId);
                Assert.That(visible, Does.Not.Contain(node.title), node.nodeId);
                Assert.That(visible, Does.Not.Contain(node.description), node.nodeId);
                inspected++;
            }
            Assert.That(inspected, Is.EqualTo(3600));
        }

        [Test]
        public void ConstructedPeopleBoardUiUsesFriendlyCopyAndKeepsIdsInsideCommands()
        {
            var registry = RecruitChronicleRegistry025.LoadFromResources();
            var board = registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var profile = ProfilesByBoard(registry)[board.boardId];
            var progress = ProgressAt(board, board.nodes[4].nodeId,
                board.nodes.Take(3).Select(value => value.nodeId).ToArray(), false);
            var quest = RecruitChronicleBoardProjection084.Project(board, profile, progress);
            var pausedBoard = registry.Boards["PQ025_SIGREC_MAREN_HOLT_02"];
            var pausedQuest = RecruitChronicleBoardProjection084.Project(pausedBoard, profile,
                ProgressAt(pausedBoard, pausedBoard.entryNodeId, Array.Empty<string>(), false));
            var state = new PeopleRuntimePresentationState029
            {
                IsAvailable = true,
                SignatureProfiles = 300,
                PersonalQuestBoards = 360,
                PersonalQuestNodes = 3600,
                Recruits = new[]
                {
                    new RecruitPeopleView029
                    {
                        RecruitId = profile.recruitId, BoardId = board.boardId,
                        DisplayName = profile.displayName, ChronicleTitle = profile.chronicleTitle,
                        WorldId = profile.worldId, WorldName = quest.WorldName,
                        QuestProgressLabel = "0 of 2 Chronicles returned", TotalQuests = 2,
                        ActiveQuests = 1
                    }
                },
                Quests = new[] {pausedQuest, quest},
                AvailableHallScenes = new[]
                {
                    new HallSceneView029
                    {
                        SceneId = "HALL025_000", DisplayName = "A Quiet Supper",
                        Summary = "Two Guild members share a quiet moment.", LocationTag = "COMMONS",
                        LocationName = "Commons"
                    }
                },
                Bonds = new[]
                {
                    new BondPairView029
                    {
                        PairId = "PAIR_SIGREC_MAREN_HOLT_SIGREC_ODELIA_FEN",
                        FirstRecruitId = "SIGREC_MAREN_HOLT", SecondRecruitId = "SIGREC_ODELIA_FEN",
                        FirstDisplayName = "Maren Holt", SecondDisplayName = "Odelia Fen",
                        TierId = "BOND_TIER026_1", TierDisplayName = "Trusted Companions",
                        TierRank = 1, Trust = 20, Respect = 15, Familiarity = 10, SharedMemories = 2
                    }
                },
                Doctrines = new[]
                {
                    new DoctrineView029
                    {
                        DoctrineId = "BOND_DOCTRINE026_00", DisplayName = "Protective Cadence"
                    }
                },
                Unions = new[]
                {
                    new UnionPeopleView029
                    {
                        UnionId = "UNION_TEST_084", DisplayName = "First Union",
                        BondDoctrineId = "BOND_DOCTRINE026_00", BondDoctrineName = "Protective Cadence"
                    }
                }
            };
            var presenterObject = new GameObject("People Board Presenter Test 084");
            var rootObject = new GameObject("People Board Root Test 084", typeof(RectTransform));
            try
            {
                var rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(1280f, 800f);
                var rootLayout = rootObject.AddComponent<VerticalLayoutGroup>();
                rootLayout.spacing = 12f;
                rootLayout.childControlWidth = true;
                rootLayout.childControlHeight = true;
                rootLayout.childForceExpandWidth = true;
                rootLayout.childForceExpandHeight = false;
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                typeof(M1FlowPresenter).GetField("_reducedMotion",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(presenter, true);
                var method = typeof(M1FlowPresenter).GetMethod("BuildPeopleRuntime029",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                method.Invoke(presenter, new object[]
                {
                    rootObject.transform, new FriendlyPeopleCoordinator029(state), state
                });
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                var visible = string.Join("\n", rootObject.GetComponentsInChildren<Text>(true)
                    .Select(value => value.text));
                Assert.That(visible, Does.Contain("YOUR PAWN"));
                Assert.That(visible, Does.Contain("FACE DOWN"));
                Assert.That(visible, Does.Contain("CHOOSE LEFT PATH"));
                Assert.That(visible, Does.Contain("CHOOSE RIGHT PATH"));
                Assert.That(visible, Does.Not.Contain("DANGER ROOM"),
                    "A face-down destination must not reveal its room kind before the saved move.");
                Assert.That(visible, Does.Contain("MAREN HOLT"));
                Assert.That(visible, Does.Not.Contain("Odelia Fen"),
                    "Bond detail must stay suppressed while the active board owns the screen.");
                Assert.That(visible, Does.Not.Contain("EARNED BONDS"));
                Assert.That(visible, Does.Contain("1 legacy Chronicle is safely paused"));
                Assert.That(rootObject.transform.GetChild(0).name,
                    Does.StartWith("People Compact Header ACTIVE PERSONAL CHRONICLE"));

                var boardPanels = rootObject.GetComponentsInChildren<RectTransform>(true).Where(value =>
                    StringComparer.Ordinal.Equals(value.name,
                        "Personal Quest Board "+board.boardId)).ToArray();
                Assert.That(boardPanels, Has.Length.EqualTo(1));
                var boardPanel = boardPanels[0];
                Assert.That(boardPanel.name, Does.Contain(board.boardId));
                var boardLayout = boardPanel.GetComponent<LayoutElement>();
                Assert.That(boardLayout.preferredHeight, Is.EqualTo(680f));
                var headerLayout = rootObject.transform.GetChild(0).GetComponent<LayoutElement>();
                Assert.That(headerLayout.preferredHeight + boardLayout.preferredHeight + rootLayout.spacing,
                    Is.LessThanOrEqualTo(800f), "The active Chronicle's primary stack must fit 1280×800.");
                var track = rootObject.GetComponentsInChildren<RectTransform>(true).Single(value =>
                    value.name.StartsWith("Personal Quest Face Down Track ", StringComparison.Ordinal));
                LayoutRebuilder.ForceRebuildLayoutImmediate(boardPanel);
                LayoutRebuilder.ForceRebuildLayoutImmediate(track);
                var rows = track.Cast<Transform>().Where(value =>
                    value.name.StartsWith("Personal Quest Track Row ",
                        StringComparison.Ordinal))
                    .Select(value => value.GetComponent<RectTransform>()).ToArray();
                Assert.That(rows, Has.Length.EqualTo(2));
                Assert.That(rows.All(value => value.childCount == 5), Is.True);
                foreach (var row in rows) LayoutRebuilder.ForceRebuildLayoutImmediate(row);
                foreach (var tile in rows.SelectMany(value => value.Cast<Transform>()).Select(value => value.GetComponent<RectTransform>()))
                {
                    Assert.That(tile.GetComponent<LayoutElement>().preferredHeight, Is.EqualTo(84f));
                    var tileGroup = tile.GetComponent<CanvasGroup>();
                    Assert.That(tileGroup, Is.Not.Null,
                        tile.name + " must own its reveal alpha instead of relying on a late-added component.");
                    Assert.That(tileGroup.alpha, Is.EqualTo(1f),
                        tile.name + " must be fully settled when reduced motion is enabled.");
                    Assert.That(tile.rect.width, Is.GreaterThanOrEqualTo(160f),
                        tile.name + " must remain readable at 1280×800.");
                }
                var currentTile = rows.SelectMany(value => value.Cast<Transform>())
                    .Select(value => value.GetComponent<RectTransform>())
                    .Single(value => value.name.EndsWith(quest.CurrentNodeId, StringComparison.Ordinal));
                Assert.That(currentTile.localScale, Is.EqualTo(Vector3.one),
                    "Reduced motion must settle the pawn tile immediately.");
                Assert.That(currentTile.GetComponent<CanvasGroup>().alpha, Is.EqualTo(1f));
                Assert.That(rootObject.GetComponentsInChildren<Button>(true).Where(value =>
                        value.name.StartsWith("Personal Quest Move ", StringComparison.Ordinal))
                    .All(value => value.GetComponent<LayoutElement>().minHeight >= 64f), Is.True);
                foreach (var raw in new[]
                {
                    board.boardId, profile.recruitId, profile.worldId, "HALL025_000",
                    "PAIR_SIGREC_MAREN_HOLT_SIGREC_ODELIA_FEN", "BOND_TIER026_1",
                    "BOND_DOCTRINE026_00", "UNION_TEST_084"
                }.Concat(board.nodes.Select(value => value.nodeId)))
                    Assert.That(visible.IndexOf(raw, StringComparison.OrdinalIgnoreCase), Is.EqualTo(-1), raw);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        static string NextTowardKind(PersonalQuestBoardDefinition025 board,
            PersonalQuestNodeDefinition025 current, string targetKind)
        {
            foreach (var destination in current.nextNodeIds ?? Array.Empty<string>())
                if (CanReachKind(board, destination, targetKind, new HashSet<string>(StringComparer.Ordinal)))
                    return destination;
            Assert.Fail(current.nodeId + " cannot reach " + targetKind);
            return string.Empty;
        }

        static bool CanReachKind(PersonalQuestBoardDefinition025 board, string nodeId, string targetKind,
            ISet<string> visited)
        {
            if (!visited.Add(nodeId)) return false;
            var node = RecruitChronicleRules025.FindNode(board, nodeId);
            if (node == null) return false;
            if (StringComparer.Ordinal.Equals(node.kind, targetKind)) return true;
            return (node.nextNodeIds ?? Array.Empty<string>()).Any(destination =>
                CanReachKind(board, destination, targetKind, visited));
        }

        static CampaignState AdvanceToBattle(CampaignState campaign, PersonalQuestBoardDefinition025 board,
            RecruitChronicleCommandService025 commands)
        {
            var guard = 0;
            while (true)
            {
                var progress = Quest(campaign, board.boardId);
                var node = RecruitChronicleRules025.FindNode(board, progress.CurrentNodeId);
                if (StringComparer.Ordinal.Equals(node.kind, "BATTLE")) return campaign;
                var destination = NextTowardKind(board, node, "BATTLE");
                var receipt = RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed,
                    board.boardId, destination, board.recruitId);
                campaign = Require(commands.AdvancePersonalQuest(campaign, board, destination, receipt), node.nodeId);
                Assert.That(++guard, Is.LessThan(11), board.boardId);
            }
        }

        static IReadOnlyList<PersonalQuestNodeDefinition025> PathToNode(
            PersonalQuestBoardDefinition025 board, string destinationNodeId)
        {
            var path = new List<PersonalQuestNodeDefinition025>();
            Assert.That(TryFindPath(board, board.entryNodeId, destinationNodeId,
                new HashSet<string>(StringComparer.Ordinal), path), Is.True, destinationNodeId);
            return path;
        }

        static bool TryFindPath(PersonalQuestBoardDefinition025 board, string nodeId, string destinationNodeId,
            ISet<string> visited, IList<PersonalQuestNodeDefinition025> path)
        {
            if (!visited.Add(nodeId)) return false;
            var node = RecruitChronicleRules025.FindNode(board, nodeId);
            if (node == null) return false;
            path.Add(node);
            if (StringComparer.Ordinal.Equals(nodeId, destinationNodeId)) return true;
            foreach (var next in node.nextNodeIds ?? Array.Empty<string>())
                if (TryFindPath(board, next, destinationNodeId,
                    new HashSet<string>(visited, StringComparer.Ordinal), path)) return true;
            path.RemoveAt(path.Count - 1);
            return false;
        }

        static PersonalQuestNodeDefinition025 CopyNode(PersonalQuestNodeDefinition025 source) =>
            new PersonalQuestNodeDefinition025
            {
                nodeId = source.nodeId, kind = source.kind, title = source.title,
                description = source.description, nextNodeIds = source.nextNodeIds,
                choiceIds = source.choiceIds, battleProfileId = source.battleProfileId,
                checkDimensionId = source.checkDimensionId, difficulty = source.difficulty,
                rewardMemoryId = source.rewardMemoryId, operationCost = source.operationCost
            };

        static PersonalQuestBoardDefinition025 CopyBoard(PersonalQuestBoardDefinition025 source,
            string boardId = null, string recruitId = null, PersonalQuestNodeDefinition025[] nodes = null) =>
            new PersonalQuestBoardDefinition025
            {
                boardId = boardId ?? source.boardId, recruitId = recruitId ?? source.recruitId,
                displayName = source.displayName, theme = source.theme, entryNodeId = source.entryNodeId,
                nodes = nodes ?? source.nodes, deterministic = source.deterministic,
                reloadCannotReroll = source.reloadCannotReroll, failureRecoverable = source.failureRecoverable,
                canCauseDeparture = source.canCauseDeparture
            };

        static CampaignState AddInvalidTestUnions(CampaignState campaign, string recruitId)
        {
            var unions = campaign.Guild.Unions.Concat(new[]
            {
                new UnionState("PROTECTED_UNION_084", "Protected", UnionKind.ProtectedSpecial,
                    recruitId, new[] {recruitId}, "FORMATION_TEST", "DOCTRINE_TEST", 20, 8000),
                new UnionState("EMPTY_UNION_084", "Empty", UnionKind.Normal,
                    recruitId, Array.Empty<string>(), "FORMATION_TEST", "DOCTRINE_TEST", 20, 8000)
            }).ToArray();
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                unions, campaign.Guild.Inventory, campaign.Guild.Development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        static BattleReturnReceipt017D CopyBattleReturn(BattleReturnReceipt017D source,
            string battleResultHash = null, string rewardId = null) =>
            new BattleReturnReceipt017D(source.ReceiptId, source.LaunchRequestId, source.BattleRunId,
                source.Outcome, battleResultHash ?? source.BattleResultHash, source.SupplyConsumption,
                source.FatigueDelta, source.UrgencyDelta, source.ObjectiveFlags, source.MaterialRewards,
                rewardId ?? source.EquipmentRewardReceiptId, source.RelationshipMemories,
                source.CityProjectContribution, source.ReturnCheckpointId, source.Applied);

        static EncounterLaunchRequest017D CreateLegacyChronicleRequest(CampaignState campaign,
            PersonalQuestProgressState025 progress)
        {
            var allies=new[]{"U1"}.OrderBy(value=>value,StringComparer.Ordinal).ToArray();
            var seed=CanonicalJson.Sha256Hex(new
                {campaign.CampaignSeed,boardId=progress.BoardId,nodeId=progress.CurrentNodeId,allies});
            return new EncounterLaunchRequest017D(
                "PQ_ENCOUNTER_029_"+seed.Substring(0,24).ToUpperInvariant(),
                RecruitChronicleBattleService025.ContractPrefix+progress.BoardId,
                "PQ_EXPEDITION_029_"+progress.BoardId,progress.BoardId,progress.CurrentNodeId,
                "LEGACY_BATTLE_PROFILE_084","BATTLE_PQ_029_"+seed.Substring(0,18).ToUpperInvariant(),
                "Stand with this Guild companion and survive the archived room.",
                1+(int)(Convert.ToUInt32(seed.Substring(0,8),16)%3u),seed,allies,Array.Empty<string>(),
                new[]{"OBJECTIVE_PERSONAL_QUEST_029","OBJECTIVE_"+progress.CurrentNodeId},
                new[]{"PERSONAL_QUEST_029","RECRUIT_"+progress.RecruitId},10,0,0,
                "RETURN_PERSONAL_QUEST_029_"+progress.CurrentNodeId,CanonicalJson.Sha256Hex(campaign));
        }

        static EncounterLaunchRequest017D CopyEncounter(EncounterLaunchRequest017D source,
            IReadOnlyList<string> routeModifiers=null,string objective=null)
        {
            return new EncounterLaunchRequest017D(source.RequestId,source.ContractId,source.ExpeditionId,
                source.BoardId,source.NodeId,source.EncounterId,source.BattleId,objective??source.Objective,
                source.EnemyUnionCount,source.CanonicalSeedIdentity,source.AlliedUnionIds,source.ReserveUnionIds,
                source.ObjectiveIds,routeModifiers??source.RouteModifiers,source.Supplies,source.Fatigue,source.Urgency,
                source.ReturnCheckpointId,source.PreBattleStateHash);
        }

        static CampaignState WithPendingHandoff(CampaignState campaign,EncounterLaunchRequest017D request,
            BattleReturnReceipt017D battleReturn=null)
        {
            var city=campaign.Guild.GuildCity.With(pendingEncounter:request,replacePendingEncounter:true,
                pendingBattleReturn:battleReturn,replacePendingBattleReturn:true);
            return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
        }

        static CampaignState WithoutAdventureAuthority084(
            CampaignState campaign,string authorityId)
        {
            var source=campaign.Guild.Development;
            var development=new GuildDevelopmentState(source.HallStageIndex,
                source.HallStageId,source.HallEnhancementXp,
                source.LifetimeTreasuryXpEarned,source.Facilities,
                source.ClaimedBattleRewardIds,source.AppliedAdventureAuthorityIds
                    .Where(value=>!StringComparer.Ordinal.Equals(value,authorityId))
                    .ToArray());
            return campaign.With(campaign.Guild.With(
                campaign.Guild.TreasuryXp,campaign.Guild.Recruits,
                campaign.Guild.Unions,campaign.Guild.Inventory,development),
                campaign.OpeningFlow);
        }

        static CampaignState WithChronicles(CampaignState campaign, RecruitChronicleState025 chronicles)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(recruitChronicles025: chronicles,
                replaceRecruitChronicles025: true);
            progress = progress.With(playable020: playable, replacePlayable020: true);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState WithCampaignOperation(CampaignState campaign,
            SecondDimension.Gameplay.Campaign020.CampaignPlayableOperationState020 operation)
        {
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H;
            var progress=strategic.Campaign019;
            var playable=progress.Playable020.With(activeOperation:operation,replaceActiveOperation:true);
            progress=progress.With(playable020:playable,replacePlayable020:true);
            strategic=strategic.With(campaign019:progress,replaceCampaign019:true);
            city=city.With(strategic017H:strategic,replaceStrategic017H:true);
            return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
        }

        static void AssertRejectedWithoutDelta(Result<CampaignState> result, CampaignState unchanged,
            string expectedErrorFragment)
        {
            var before = CanonicalJson.Sha256Hex(unchanged);
            Assert.That(result.IsSuccess, Is.False, expectedErrorFragment);
            Assert.That(result.Errors.Any(value => value.IndexOf(expectedErrorFragment,
                StringComparison.Ordinal) >= 0), Is.True, string.Join("\n", result.Errors));
            Assert.That(CanonicalJson.Sha256Hex(unchanged), Is.EqualTo(before));
        }

        static BattleState CreateTerminalBattle(EncounterLaunchRequest017D request, string recruitId,
            BattleOutcome outcome, bool claimed)
        {
            var member = new BattleMemberState(recruitId, recruitId, "CLASS_TEST_084",
                120, 120, 30, 30, 20, 20, Array.Empty<string>(), false, false, false,
                Array.Empty<string>(), 0, 0, string.Empty);
            var unions = request.AlliedUnionIds.Select(unionId => new BattleUnionState(
                unionId, unionId, BattleSide.Player, recruitId, new[] {member},
                "FORMATION_TEST_084", "Test Formation", true, string.Empty,
                20, 20, 90, 10000, EngagementState.Open, false, false, 0)).ToArray();
            var rewardSeed = CanonicalJson.Sha256Hex(new {request.RequestId, request.BattleId, outcome});
            var reward = new BattleRewardState(
                "REWARD_PQ_084_" + rewardSeed.Substring(0, 20).ToUpperInvariant(),
                "TEST_REWARD_084", outcome, 1, 1, 1000, 1000, 100, 100, 1, 1,
                new[] {new BattleMemberRewardState(recruitId, recruitId, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0)},
                claimed);
            var battle = new BattleState(request.BattleId, "TEST_BATTLE_084", 1, BattlePhase.Resolved, outcome,
                request.Objective, unions, Array.Empty<BattleUnionState>(), Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(), Array.Empty<BattleEventState>(),
                Array.Empty<BattleRoundRecordState>(), string.Empty, string.Empty, string.Empty,
                string.Empty, string.Empty, false, reward);
            return battle.With(finalStateHash: M2BattleCommandService.AuthoritativeStateHash(battle));
        }

        static CampaignState AttachClaimedBattle(CampaignState campaign, BattleState battle)
        {
            var development = campaign.Guild.Development.RecordBattleReward(battle.Reward.RewardId, 1, 1);
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow).WithBattle(battle);
        }

        static IReadOnlyDictionary<string, RecruitChronicleProfile025> ProfilesByBoard(
            RecruitChronicleRegistry025 registry)
        {
            var result = new Dictionary<string, RecruitChronicleProfile025>(StringComparer.Ordinal);
            foreach (var profile in registry.Profiles.Values)
            foreach (var boardId in profile.personalQuestBoardIds ?? Array.Empty<string>())
                result.Add(boardId, profile);
            Assert.That(result.Count, Is.EqualTo(360));
            return result;
        }

        static PersonalQuestProgressState025 ProgressAt(
            PersonalQuestBoardDefinition025 board,
            string nodeId,
            IReadOnlyList<string> completed,
            bool complete)
        {
            return new PersonalQuestProgressState025(board.boardId, board.recruitId, nodeId, completed,
                Array.Empty<string>(), complete, string.Empty, "projection_test");
        }

        static string VisibleCopy(PersonalQuestView029 view)
        {
            return string.Join("\n", new[]
            {
                view.DisplayName, view.RecruitDisplayName, view.ChronicleTitle, view.ChapterLabel,
                view.WorldName, view.Theme, view.StoryContext, view.Objective, view.ProgressLabel,
                view.CurrentNodeTitle, view.CurrentRoomLabel, view.CurrentNodeDescription,
                view.CurrentInstruction
            }.Concat(view.Tiles.Select(value => value.RoomLabel + "\n" + value.DisplayLabel))
             .Concat(view.Destinations.Select(value => value.RoomLabel + "\n" + value.ButtonLabel + "\n" + value.Description)));
        }

        static string ProjectionSnapshot(PersonalQuestView029 view)
        {
            return string.Join("|", new[]
            {
                view.BoardId, view.CurrentNodeId, view.ProgressLabel,
                string.Join(",", view.NextNodeIds),
                string.Join(",", view.Tiles.Select(value => value.NodeId + ":" + value.State + ":" + value.DisplayLabel)),
                string.Join(",", view.Destinations.Select(value => value.DestinationNodeId + ":" + value.ButtonLabel)),
                view.Complete.ToString(), view.CompletedRooms.ToString()
            });
        }

        static CampaignState CreateCampaign(string recruitId)
        {
            var recruit = new RecruitState(recruitId, 120, 120, 30, 30);
            var other = new RecruitState("FORGED_SIGNED_RECRUIT_084", 120, 120, 30, 30);
            var union = new UnionState("U1", "First Union", UnionKind.Normal, recruitId,
                new[] {recruitId}, "FORMATION_LINE", "DOCTRINE_BALANCED", 30, 8500);
            var guild = new GuildState("GUILD_084", 0, new[] {recruit, other}, new[] {union});
            var profile = new NewGuildProfileState("Board Tester", GameMode.Standard,
                TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false);
            var flow = new OpeningFlowState(OpeningStage.Complete, "SDGOW_TUTORIAL_V1_001", true,
                null, false, 439, 0, true, true, true, false, "complete");
            return new CampaignState("00000000-0000-0000-0000-000000000084", 84025L,
                "1.0", ModeRuleSnapshot.StandardDefaults(), guild, profile, flow);
        }

        static PersonalQuestProgressState025 Quest(CampaignState campaign, string boardId)
        {
            return campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025
                .PersonalQuests.Single(value => StringComparer.Ordinal.Equals(value.BoardId, boardId));
        }

        static CampaignState Require(Result<CampaignState> result, string context)
        {
            Assert.That(result.IsSuccess, Is.True, context + "\n" + string.Join("\n", result.Errors));
            return result.Value;
        }

        sealed class FriendlyPeopleCoordinator029 : IPeopleRuntimePresentationCoordinator029
        {
            public FriendlyPeopleCoordinator029(PeopleRuntimePresentationState029 state) { PeopleRuntime029 = state; }
            public PeopleRuntimePresentationState029 PeopleRuntime029 { get; }
            public M1CommandResult StartPersonalQuest029(string boardId) => M1CommandResult.Success();
            public M1CommandResult RecoverPersonalQuest029(string boardId) => M1CommandResult.Success();
            public M1CommandResult AdvancePersonalQuest029(string boardId, string destinationNodeId) => M1CommandResult.Success();
            public M1CommandResult EnterPersonalQuestBattle029(string boardId) => M1CommandResult.Success();
            public M1CommandResult ViewHallScene029(string sceneId) => M1CommandResult.Success();
            public M1CommandResult DeferHallScene029(string sceneId) => M1CommandResult.Success();
            public M1CommandResult CompleteMentorship029(string lessonId, string mentorId, string studentId) => M1CommandResult.Success();
            public M1CommandResult RecordRelationshipMemory029(string memoryId, string firstRecruitId, string secondRecruitId, string sourceId) => M1CommandResult.Success();
            public M1CommandResult UnlockLegendTechnique029(string recruitId) => M1CommandResult.Success();
            public M1CommandResult SetUnionBondDoctrine029(string unionId, string doctrineId) => M1CommandResult.Success();
        }
    }
}
