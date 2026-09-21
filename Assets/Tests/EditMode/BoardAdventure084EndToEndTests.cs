#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Determinism;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardAdventure084EndToEndTests
    {
        CampaignRegistry019 _registry019;
        CampaignRuleCatalogAdapter019 _catalog019;
        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignCommandService019 _campaign019;
        CampaignPlayableCommandService020 _campaign020;
        CampaignWorldGateCommandService023 _worldGate023;
        GuildCityBattleBridgeService017D _battleBridge;

        [SetUp]
        public void SetUp()
        {
            _registry019 = CampaignRegistry019.LoadFromResources();
            _catalog019 = new CampaignRuleCatalogAdapter019(_registry019.Base018);
            _registry020 = CampaignRegistry020.LoadFromResources();
            _catalog020 = new Campaign020RuleCatalogAdapter(_registry020);
            _registry023 = CampaignRegistry023.LoadFromResources();
            _catalog023 = new Campaign023RuleCatalogAdapter(_registry023);
            _campaign019 = new CampaignCommandService019();
            _campaign020 = new CampaignPlayableCommandService020();
            _worldGate023 = new CampaignWorldGateCommandService023();
            _battleBridge = new GuildCityBattleBridgeService017D();
        }

        [Test]
        public void RepeatableThresholdRules110PreserveAuthoredDefinitions()
        {
            var rules=((ICampaignRepeatableUnlockCatalog020)_catalog020)
                .RepeatableUnlockRules;
            Assert.That(rules.Count,Is.EqualTo(_registry020.Repeatables.Count));
            foreach(var source in _registry020.Repeatables.Values)
            {
                var rule=rules.Single(value=>value.ContractId==source.contractId);
                Assert.That(rule.WorldId,Is.EqualTo(source.worldId));
                Assert.That(rule.UnlockedByChapterCount,
                    Is.EqualTo(source.unlockedByChapterCount));
            }
            Assert.That(rules.Single(value=>
                value.ContractId=="REPEAT020_SKYHOME_04").UnlockedByChapterCount,
                Is.EqualTo(7));
            Assert.That(_registry020.Blueprints.Values.Any(value=>
                (value.repeatableUnlockIds??Array.Empty<string>())
                    .Contains("REPEAT020_SKYHOME_04")),Is.False,
                "The repair must preserve existing operation content and hashes.");
        }

        [Test]
        public void SeventhVerifiedSkyhomeClosure110UnlocksOrphanAndNextClosureRepairsOmission()
        {
            const string orphan="REPEAT020_SKYHOME_04";
            var campaign=CreateRosterCampaign(110007);
            for(var number=1;number<=8;number++)
            {
                var chapterId="CH018_"+number.ToString("000");
                campaign=Require(_campaign019.StartChapter(campaign,_catalog019,
                    chapterId,new[]{"BOARD_UNION_084"},true));
                campaign=Require(_campaign020.BeginOperation(campaign,_catalog020,
                    chapterId));
                Assert.That(_catalog020.TryGetBlueprint(chapterId,
                    out var blueprint),Is.True);
                campaign=ApplyCampaign020NonBattle(campaign,"briefing");
                campaign=CompleteWorldBoardMainPath(campaign,chapterId,"SKYHOME");
                campaign=ApplyCampaign020ReceiptExactly(Require(
                    _campaign020.CommitCompletedWorldBoardStep(campaign,_catalog020)),
                    chapterId+" world board");
                while(Playable(campaign).ActiveOperation.Status!=
                      CampaignPlayableOperationStatus020.ReadyToFinalize)
                {
                    var operation=Playable(campaign).ActiveOperation;
                    var step=blueprint.Steps[operation.CurrentStepIndex];
                    if(step.RequiresCertifiedBattle)
                    {
                        campaign=Require(_campaign019.CommitCertifiedBattle(
                            campaign,_catalog019));
                        campaign=Require(_campaign020.MarkBattleCommitted(
                            campaign,_catalog020));
                        campaign=WithClaimedBattle(campaign,
                            campaign.Guild.GuildCity.PendingEncounter.BattleId,
                            "REPEATABLE110_BATTLE_"+chapterId);
                        campaign=Require(_campaign019
                            .CommitClaimedBattleReceiptAndClearEncounter(
                                campaign,_catalog019));
                        campaign=ApplyCampaign020ReceiptExactly(Require(
                            _campaign020.CommitBattleStepReceipt(campaign,
                                _catalog020)),chapterId+" certified battle");
                    }
                    else campaign=ApplyCampaign020NonBattle(campaign,step.StepId);
                }
                Assert.That(Playable(campaign).UnlockedRepeatableContractIds,
                    Does.Not.Contain(orphan),chapterId+" before chapter application");
                Assert.That(_campaign020.CloseCompletedOperation(campaign,
                    _catalog020).IsSuccess,Is.False,
                    "Ready steps cannot bypass the authenticated chapter receipt.");
                if(campaign.Guild.GuildCity.Strategic017H.Campaign019.PendingReceipt==null)
                    campaign=Require(_campaign019.CommitNonCombatReceipt(campaign,
                        _catalog019,"SUCCESS"));
                campaign=Require(_campaign019.ApplyReceiptExactlyOnce(campaign,
                    _catalog019,_catalog020));
                if(number==1)
                {
                    var city=campaign.Guild.GuildCity;
                    var strategic=city.Strategic017H;
                    var forgedProgress=strategic.Campaign019.With(
                        completedChapterIds:_registry019.Base018.Chapters.Keys.ToArray());
                    var forged=campaign.With(campaign.Guild.WithGuildCity(city.With(
                        strategic017H:strategic.With(campaign019:forgedProgress,
                            replaceCampaign019:true),replaceStrategic017H:true)),
                        campaign.OpeningFlow);
                    var attempted=Require(_campaign020.CloseCompletedOperation(
                        forged,_catalog020));
                    Assert.That(Playable(attempted).UnlockedRepeatableContractIds,
                        Does.Not.Contain(orphan),
                        "Completion IDs without chapter proofs must not count.");
                }
                var beforeClose=campaign;
                var proofState=CanonicalJson.Serialize(WorldGate(beforeClose));
                var inventory=CanonicalJson.Serialize(beforeClose.Guild.Inventory);
                var development=CanonicalJson.Serialize(beforeClose.Guild.Development);
                campaign=Require(_campaign020.CloseCompletedOperation(beforeClose,
                    _catalog020));
                Assert.That(Playable(campaign).UnlockedRepeatableContractIds
                    .Contains(orphan),Is.EqualTo(number>=7),chapterId);
                Assert.That(Playable(campaign).UnlockedRepeatableContractIds.Any(id=>
                    _registry020.Repeatables[id].worldId!="SKYHOME"),Is.False,
                    "Counts are per world, not global or per arc.");
                Assert.That(CanonicalJson.Serialize(WorldGate(campaign)),
                    Is.EqualTo(proofState));
                Assert.That(CanonicalJson.Serialize(campaign.Guild.Inventory),
                    Is.EqualTo(inventory));
                Assert.That(CanonicalJson.Serialize(campaign.Guild.Development),
                    Is.EqualTo(development));
                Assert.That(CanonicalJson.Serialize(Require(
                    _campaign020.CloseCompletedOperation(beforeClose,_catalog020))),
                    Is.EqualTo(CanonicalJson.Serialize(campaign)));
                Assert.That(_campaign020.CloseCompletedOperation(campaign,
                    _catalog020).IsSuccess,Is.False);
                campaign=JsonConvert.DeserializeObject<CampaignState>(
                    JsonConvert.SerializeObject(campaign));
                Assert.That(CampaignWorldGateCommandService023
                    .ValidateStoredCompletionProofs084(campaign,_catalog023,
                        WorldGate(campaign)),Is.True);
                if(number==7)
                {
                    // Reproduce the old omission using already authenticated
                    // history. Only the next real closure may repair this copy.
                    var city=campaign.Guild.GuildCity;
                    var strategic=city.Strategic017H;
                    var progress=strategic.Campaign019;
                    progress=progress.With(playable020:progress.Playable020.With(
                        unlockedRepeatableContractIds:progress.Playable020
                            .UnlockedRepeatableContractIds.Where(id=>id!=orphan).ToArray()),
                        replacePlayable020:true);
                    campaign=campaign.With(campaign.Guild.WithGuildCity(city.With(
                        strategic017H:strategic.With(campaign019:progress,
                            replaceCampaign019:true),replaceStrategic017H:true)),
                        campaign.OpeningFlow);
                }
            }
        }

        [Test]
        public void AllEightyTwoCampaignsRunBoardRemainingStepsAndCloseExactlyOnce()
        {
            var sources = _registry020.Blueprints.Values
                .OrderBy(value => value.chapterId, StringComparer.Ordinal).ToArray();
            var battleBranches = 0;
            var campaign019BattleAuthorityChecks = 0;
            var campaign019ReceiptAuthorityChecks = 0;
            foreach (var source in sources)
            {
                var campaign = CreateCanonicalStoryChapter(source.chapterId,
                    84200 + Array.IndexOf(sources, source));
                Assert.That(_catalog020.TryGetBlueprint(source.chapterId,
                    out var blueprint), Is.True, source.chapterId);
                campaign = ApplyCampaign020NonBattle(campaign, "briefing");
                campaign = CompleteWorldBoardMainPath(campaign, source.chapterId,
                    source.worldId);

                var beforeWrapperVisit = GateVisitCount(campaign, source.worldId);
                var wrapperCommitted = Require(_campaign020.CommitCompletedWorldBoardStep(
                    campaign, _catalog020));
                Assert.That(Playable(wrapperCommitted).ActiveOperation.PendingReceipt.GuildXp,
                    Is.Zero, source.chapterId);
                Assert.That(Playable(wrapperCommitted).ActiveOperation.PendingReceipt.HallXp,
                    Is.Zero, source.chapterId);
                Assert.That(Playable(wrapperCommitted).ActiveOperation.PendingReceipt.Materials
                    .Sum(value => value.Amount), Is.Zero, source.chapterId);
                campaign = ApplyCampaign020ReceiptExactly(wrapperCommitted,
                    source.chapterId + " world-board wrapper");
                Assert.That(GateVisitCount(campaign, source.worldId),
                    Is.EqualTo(beforeWrapperVisit));

                while (Playable(campaign).ActiveOperation.Status !=
                       CampaignPlayableOperationStatus020.ReadyToFinalize)
                {
                    var operation = Playable(campaign).ActiveOperation;
                    var step = blueprint.Steps[operation.CurrentStepIndex];
                    if (step.RequiresCertifiedBattle)
                    {
                        battleBranches++;
                        campaign = Require(_campaign019.CommitCertifiedBattle(
                            campaign, _catalog019));
                        campaign = Require(_campaign020.MarkBattleCommitted(
                            campaign, _catalog020));
                        var encounter = campaign.Guild.GuildCity.PendingEncounter;
                        campaign = WithClaimedBattle(campaign, encounter.BattleId,
                            "M2_C020_084_" + source.chapterId);
                        if (campaign019BattleAuthorityChecks == 0)
                        {
                            var forgedBattle = campaign.Battle.With(
                                finalStateHash: new string('0', 64));
                            var forgedBattleCampaign = campaign.WithBattle(forgedBattle);
                            var forgedBattleHash = CanonicalJson.Sha256Hex(
                                forgedBattleCampaign);
                            var rejectedBattle = _campaign019
                                .CommitClaimedBattleReceiptAndClearEncounter(
                                    forgedBattleCampaign, _catalog019);
                            Assert.That(rejectedBattle.IsSuccess, Is.False);
                            CollectionAssert.Contains(rejectedBattle.Errors,
                                "CAMPAIGN019_BATTLE_AUTHORITY_INVALID");
                            Assert.That(CanonicalJson.Sha256Hex(forgedBattleCampaign),
                                Is.EqualTo(forgedBattleHash));
                            campaign019BattleAuthorityChecks++;
                        }
                        campaign = Require(_campaign019
                            .CommitClaimedBattleReceiptAndClearEncounter(
                                campaign, _catalog019));
                        campaign = Require(_campaign020.CommitBattleStepReceipt(
                            campaign, _catalog020));
                        campaign = ApplyCampaign020ReceiptExactly(campaign,
                            source.chapterId + " certified battle");
                    }
                    else
                    {
                        campaign = ApplyCampaign020NonBattle(campaign,
                            source.chapterId + " / " + step.StepId);
                    }
                }

                var chapterCommitted = Playable(campaign).ActiveOperation;
                Assert.That(chapterCommitted.CompletedStepIds.Count,
                    Is.EqualTo(blueprint.Steps.Count), source.chapterId);
                var earlyClose = _campaign020.CloseCompletedOperation(campaign, _catalog020);
                Assert.That(earlyClose.IsSuccess, Is.False, source.chapterId);
                CollectionAssert.Contains(earlyClose.Errors,
                    "CAMPAIGN020_APPLY_CHAPTER_RECEIPT_FIRST");
                if (campaign.Guild.GuildCity.Strategic017H.Campaign019.PendingReceipt == null)
                    campaign = Require(_campaign019.CommitNonCombatReceipt(
                        campaign, _catalog019, "SUCCESS"));
                var beforeChapterApply = campaign;
                var storyReceipt = beforeChapterApply.Guild.GuildCity.Strategic017H
                    .Campaign019.PendingReceipt;
                if (source.requiresCertifiedBattle &&
                    campaign019ReceiptAuthorityChecks == 0)
                {
                    var forgedReceipt = new CampaignOperationReceipt019(
                        storyReceipt.ReceiptId, storyReceipt.RequestId,
                        storyReceipt.ChapterId, storyReceipt.Outcome,
                        storyReceipt.AuthoritativeResultHash,
                        storyReceipt.ExistingEquipmentRewardReceiptId,
                        storyReceipt.GuildXp + 100000,
                        storyReceipt.HallXp + 100000,
                        storyReceipt.Materials + 100000,
                        storyReceipt.RelationshipMemoryIds,
                        storyReceipt.CityUnlockIds, storyReceipt.StoryGateIds,
                        storyReceipt.ReturnCheckpointId,
                        storyReceipt.AppliedVersion);
                    var forgedCity = beforeChapterApply.Guild.GuildCity;
                    var forgedStrategic = forgedCity.Strategic017H;
                    var forgedProgress = forgedStrategic.Campaign019.With(
                        pendingReceipt: forgedReceipt,
                        replacePendingReceipt: true,
                        lastCheckpointId: "forged_campaign019_rewards_084");
                    forgedStrategic = forgedStrategic.With(
                        campaign019: forgedProgress,
                        replaceCampaign019: true,
                        lastCheckpointId: forgedProgress.LastCheckpointId);
                    forgedCity = forgedCity.With(
                        strategic017H: forgedStrategic,
                        replaceStrategic017H: true,
                        lastCheckpointId: forgedProgress.LastCheckpointId);
                    var forgedCampaign = beforeChapterApply.With(
                        beforeChapterApply.Guild.WithGuildCity(forgedCity),
                        beforeChapterApply.OpeningFlow);
                    var forgedHash = CanonicalJson.Sha256Hex(forgedCampaign);
                    var rejectedReceipt = _campaign019.ApplyReceiptExactlyOnce(
                        forgedCampaign, _catalog019, _catalog020);
                    Assert.That(rejectedReceipt.IsSuccess, Is.False);
                    CollectionAssert.Contains(rejectedReceipt.Errors,
                        "CAMPAIGN019_RECEIPT_AUTHORITY_INVALID");
                    Assert.That(CanonicalJson.Sha256Hex(forgedCampaign),
                        Is.EqualTo(forgedHash));
                    campaign019ReceiptAuthorityChecks++;
                }
                var storyApplied = Require(_campaign019.ApplyReceiptExactlyOnce(
                    beforeChapterApply, _catalog019,_catalog020));
                Assert.That(storyApplied.Guild.TreasuryXp -
                    beforeChapterApply.Guild.TreasuryXp, Is.EqualTo(storyReceipt.GuildXp));
                Assert.That(storyApplied.Guild.Development.HallEnhancementXp -
                    beforeChapterApply.Guild.Development.HallEnhancementXp,
                    Is.EqualTo(storyReceipt.HallXp));
                var storyReplay = Require(_campaign019.ApplyReceiptExactlyOnce(
                    beforeChapterApply, _catalog019,_catalog020));
                Assert.That(CanonicalJson.Serialize(storyReplay),
                    Is.EqualTo(CanonicalJson.Serialize(storyApplied)));
                Assert.That(_campaign019.ApplyReceiptExactlyOnce(
                    storyApplied, _catalog019).IsSuccess, Is.False);

                if (source.chapterId == "CH018_001")
                {
                    var canonical = Playable(storyApplied).ActiveOperation;
                    var forgedIdentity = new CampaignPlayableOperationState020(
                        "OP020_FORGED_CLOSE_084", canonical.BlueprintId,
                        canonical.ChapterId, canonical.WorldId,
                        canonical.CanonicalSeedIdentity, canonical.CurrentStepIndex,
                        canonical.Status, canonical.CompletedStepIds,
                        canonical.AppliedReceiptIds, canonical.PendingReceipt,
                        canonical.ExistingBattleRewardReceiptId, "forged_close",
                        canonical.AppliedReceipts);
                    var forgedClose = _campaign020.CloseCompletedOperation(
                        WithPlayableOperation(storyApplied, forgedIdentity), _catalog020);
                    Assert.That(forgedClose.IsSuccess, Is.False);
                    CollectionAssert.Contains(forgedClose.Errors,
                        "CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID");

                    var receiptIds = canonical.AppliedReceiptIds.ToArray();
                    receiptIds[receiptIds.Length - 1] = "STEPREC020_FORGED_PREFIX_084";
                    var forgedPrefix = new CampaignPlayableOperationState020(
                        canonical.OperationId, canonical.BlueprintId,
                        canonical.ChapterId, canonical.WorldId,
                        canonical.CanonicalSeedIdentity, canonical.CurrentStepIndex,
                        canonical.Status, canonical.CompletedStepIds, receiptIds,
                        canonical.PendingReceipt,
                        canonical.ExistingBattleRewardReceiptId, "forged_prefix",
                        canonical.AppliedReceipts);
                    var prefixClose = _campaign020.CloseCompletedOperation(
                        WithPlayableOperation(storyApplied, forgedPrefix), _catalog020);
                    Assert.That(prefixClose.IsSuccess, Is.False);
                    CollectionAssert.Contains(prefixClose.Errors,
                        "CAMPAIGN020_ACTIVE_OPERATION_AUTHORITY_INVALID");
                }

                var beforeCloseVisit = GateVisitCount(storyApplied, source.worldId);
                var closed = Require(_campaign020.CloseCompletedOperation(
                    storyApplied, _catalog020));
                var closeReplay = Require(_campaign020.CloseCompletedOperation(
                    storyApplied, _catalog020));
                Assert.That(CanonicalJson.Serialize(closeReplay),
                    Is.EqualTo(CanonicalJson.Serialize(closed)));
                Assert.That(Playable(closed).ActiveOperation, Is.Null, source.chapterId);
                Assert.That(GateVisitCount(closed, source.worldId),
                    Is.EqualTo(beforeCloseVisit).And.EqualTo(1), source.chapterId);
                Assert.That(closed.Guild.GuildCity.Strategic017H.Campaign019
                    .CompletedChapterIds, Does.Contain(source.chapterId));
                Assert.That(_campaign020.CloseCompletedOperation(
                    closed, _catalog020).IsSuccess, Is.False);
            }
            Assert.That(battleBranches, Is.EqualTo(37));
            Assert.That(campaign019BattleAuthorityChecks, Is.EqualTo(1));
            Assert.That(campaign019ReceiptAuthorityChecks, Is.EqualTo(1));
        }

        [Test]
        public void Campaign019SavedEncounterMutationCannotCrossTheSharedBattleBoundary()
        {
            var source = _registry020.Blueprints.Values
                .OrderBy(value => value.chapterId, StringComparer.Ordinal)
                .First(value => value.steps.Any(step => step.requiresCertifiedBattle));
            var campaign = CreateCanonicalStoryChapter(source.chapterId, 84919);
            Assert.That(_catalog020.TryGetBlueprint(source.chapterId,
                out var blueprint), Is.True);
            campaign = ApplyCampaign020NonBattle(campaign, "briefing");
            campaign = CompleteWorldBoardMainPath(campaign, source.chapterId,
                source.worldId);
            campaign = ApplyCampaign020ReceiptExactly(Require(
                _campaign020.CommitCompletedWorldBoardStep(campaign, _catalog020)),
                "campaign019 request-authority world board");
            while (!blueprint.Steps[Playable(campaign).ActiveOperation.CurrentStepIndex]
                       .RequiresCertifiedBattle)
                campaign = ApplyCampaign020NonBattle(campaign,
                    "campaign019 request-authority setup");

            campaign = Require(_campaign019.CommitCertifiedBattle(
                campaign, _catalog019));
            campaign = Require(_campaign020.MarkBattleCommitted(
                campaign, _catalog020));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(request, Is.Not.Null);
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                    request)), Is.True);

            var mutatedRequest = new EncounterLaunchRequest017D(
                request.RequestId, request.ContractId, request.ExpeditionId,
                request.BoardId, request.NodeId, request.EncounterId,
                request.BattleId, request.Objective + " (mutated saved request)",
                request.EnemyUnionCount, request.CanonicalSeedIdentity,
                request.AlliedUnionIds, request.ReserveUnionIds,
                request.ObjectiveIds, request.RouteModifiers, request.Supplies,
                request.Fatigue, request.Urgency, request.ReturnCheckpointId,
                request.PreBattleStateHash);
            var mutatedCity = campaign.Guild.GuildCity.With(
                pendingEncounter: mutatedRequest, replacePendingEncounter: true,
                lastCheckpointId: "mutated_saved_campaign019_request_084");
            var mutated = campaign.With(
                campaign.Guild.WithGuildCity(mutatedCity), campaign.OpeningFlow);
            var before = CanonicalJson.Sha256Hex(mutated);
            var combat = M2CombatContent.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
            var rejected = _battleBridge.StartCertifiedEncounter(
                mutated, new M2BattleCommandService(), combat);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED");
            Assert.That(CanonicalJson.Sha256Hex(mutated), Is.EqualTo(before));
            Assert.That(mutated.Battle, Is.Null);

            var canonical = _battleBridge.StartCertifiedEncounter(
                campaign, new M2BattleCommandService(), combat);
            Assert.That(canonical.IsSuccess, Is.True,
                string.Join("\n", canonical.Errors));
            Assert.That(canonical.Value.Battle.BattleId,
                Is.EqualTo(request.BattleId));
        }

        [Test]
        public void IndependentAdventureLedgerRejectsTerminalStateSubstitution()
        {
            var story = CreateCanonicalStoryChapter("CH018_001", 84920);
            story = ApplyCampaign020NonBattle(story, "independent ledger briefing");
            story = CompleteWorldBoardMainPath(story, "CH018_001", "SKYHOME");
            story = ApplyCampaign020ReceiptExactly(Require(
                _campaign020.CommitCompletedWorldBoardStep(story, _catalog020)),
                "independent ledger world board");
            var honestPrefix = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(story));
            Assert.That(honestPrefix, Is.Not.Null);
            var terminal = story;
            while (Playable(terminal).ActiveOperation.Status !=
                   CampaignPlayableOperationStatus020.ReadyToFinalize)
                terminal = ApplyCampaign020NonBattle(terminal,
                    "independent ledger canonical suffix");
            var forgedStory = WithPlayableOperationAndLedger(
                honestPrefix, Playable(terminal).ActiveOperation,
                Playable(terminal).ActiveStepLedger);
            Assert.That(_campaign020.IsReadyForChapterReceipt084(
                forgedStory, _catalog020, "CH018_001", out _), Is.False,
                "Replacing only the nested active operation and mirror ledger must not synthesize applied steps.");
            var stagedReceipt = Require(_campaign019.CommitNonCombatReceipt(
                forgedStory, _catalog019, "SUCCESS"));
            var stagedHash = CanonicalJson.Sha256Hex(stagedReceipt);
            var rejectedStory = _campaign019.ApplyReceiptExactlyOnce(
                stagedReceipt, _catalog019, _catalog020);
            Assert.That(rejectedStory.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(stagedReceipt), Is.EqualTo(stagedHash));

            var board = _catalog023.AllBoards
                .Where(value => value.OperationKind == "REPEATABLE" &&
                                value.Nodes.All(node => !node.RequiresCertifiedBattle))
                .OrderBy(value => value.DefinitionId, StringComparer.Ordinal).First();
            var boardBase = CreateBoardReadyCampaign(board, 84921);
            boardBase = Require(_worldGate023.BeginOperation(boardBase, _catalog023,
                board.DefinitionId, new[] { "BOARD_UNION_084" }, null));
            var honestBoardPrefix = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(boardBase));
            Assert.That(honestBoardPrefix, Is.Not.Null);
            var completedBoard = boardBase;
            foreach (var choice in SemanticPaths(board).First())
            {
                completedBoard = Require(_worldGate023.CommitNodeChoice(
                    completedBoard, _catalog023, choice, "BOARD_RECRUIT_A_084",
                    "BOARD_RECRUIT_B_084", 0));
                completedBoard = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                    completedBoard, _catalog023));
            }
            Assert.That(WorldGate(completedBoard).ActiveOperation.Status,
                Is.EqualTo(WorldGateOperationStatus023.ReadyToFinalize));
            var forgedBoard = WithWorldGateOperationLedgerAndExpedition(
                honestBoardPrefix, WorldGate(completedBoard),
                completedBoard.Guild.GuildCity.Expedition);
            var forgedBoardHash = CanonicalJson.Sha256Hex(forgedBoard);
            var rejectedBoard = _worldGate023.FinalizeOperation(
                forgedBoard, _catalog023);
            Assert.That(rejectedBoard.IsSuccess, Is.False);
            CollectionAssert.Contains(rejectedBoard.Errors,
                "CAMPAIGN023_ACTIVE_OPERATION_AUTHORITY_INVALID");
            Assert.That(CanonicalJson.Sha256Hex(forgedBoard),
                Is.EqualTo(forgedBoardHash));
        }

        [Test]
        public void EverySemanticPathAcrossAllOneHundredThirtyBoardsExecutesAndReloads()
        {
            var boards = _catalog023.AllBoards
                .OrderBy(value => value.DefinitionId, StringComparer.Ordinal).ToArray();
            var executedNodes = new HashSet<string>(StringComparer.Ordinal);
            var executedBattleNodes = new HashSet<string>(StringComparer.Ordinal);
            var pathCount = 0;
            var seed = 85000L;
            foreach (var board in boards)
            {
                var paths = SemanticPaths(board);
                Assert.That(paths.Count, Is.GreaterThanOrEqualTo(2), board.DefinitionId);
                foreach (var choices in paths)
                {
                    pathCount++;
                    var campaign = CreateBoardReadyCampaign(board, seed++);
                    campaign = Require(_worldGate023.BeginOperation(campaign,
                        _catalog023, board.DefinitionId,
                        new[] { "BOARD_UNION_084" },
                        board.OperationKind == "CHAPTER" ? _catalog020 : null));
                    for (var choiceIndex = 0; choiceIndex < choices.Count; choiceIndex++)
                    {
                        var operation = WorldGate(campaign).ActiveOperation;
                        var node = board.Nodes.Single(value =>
                            value.NodeId == operation.CurrentNodeId);
                        executedNodes.Add(board.DefinitionId + "/" + node.NodeId);
                        CampaignState committed;
                        if (node.RequiresCertifiedBattle)
                        {
                            executedBattleNodes.Add(board.DefinitionId + "/" + node.NodeId);
                            campaign = Require(_worldGate023.CommitEncounter(
                                campaign, _catalog023));
                            var encounter = campaign.Guild.GuildCity.PendingEncounter;
                            campaign = WithClaimedBattle(campaign, encounter.BattleId,
                                "M2_C023_084_" + board.DefinitionId + "_" + pathCount);
                            campaign = Require(_battleBridge.CommitBattleReturn(campaign));
                            campaign = Require(_battleBridge.ApplyBattleReturnExactlyOnce(campaign));
                            committed = Require(_worldGate023.SynchronizeAfterClaimedBattle(
                                campaign, _catalog023));
                        }
                        else
                        {
                            committed = Require(_worldGate023.CommitNodeChoice(
                                campaign, _catalog023, choices[choiceIndex],
                                "BOARD_RECRUIT_A_084", "BOARD_RECRUIT_B_084", 0));
                        }

                        var saved = JsonConvert.DeserializeObject<CampaignState>(
                            JsonConvert.SerializeObject(committed));
                        Assert.That(saved, Is.Not.Null);
                        var savedReceipt = WorldGate(saved).ActiveOperation.PendingReceipt;
                        Assert.That(savedReceipt, Is.Not.Null,
                            board.DefinitionId + " / " + node.NodeId);
                        var treasury = saved.Guild.TreasuryXp;
                        var hall = saved.Guild.Development.HallEnhancementXp;
                        var materials = CityMaterialTotal(saved);
                        var applied = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                            saved, _catalog023));
                        Assert.That(applied.Guild.TreasuryXp - treasury,
                            Is.EqualTo(savedReceipt.GuildXp));
                        Assert.That(applied.Guild.Development.HallEnhancementXp - hall,
                            Is.EqualTo(savedReceipt.HallXp));
                        Assert.That(CityMaterialTotal(applied) - materials,
                            Is.EqualTo(savedReceipt.MaterialIds.Count));
                        var replay = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                            saved, _catalog023));
                        Assert.That(CanonicalJson.Serialize(replay),
                            Is.EqualTo(CanonicalJson.Serialize(applied)));
                        Assert.That(_worldGate023.ApplyNodeReceiptExactlyOnce(
                            applied, _catalog023).IsSuccess, Is.False);
                        campaign = applied;
                    }
                    var ready = WorldGate(campaign).ActiveOperation;
                    Assert.That(ready.Status,
                        Is.EqualTo(WorldGateOperationStatus023.ReadyToFinalize),
                        board.DefinitionId);
                    var supplies = WorldGate(campaign).TravelSupplies;
                    var finalized = Require(_worldGate023.FinalizeOperation(
                        campaign, _catalog023));
                    var finalizeReplay = Require(_worldGate023.FinalizeOperation(
                        campaign, _catalog023));
                    Assert.That(CanonicalJson.Serialize(finalizeReplay),
                        Is.EqualTo(CanonicalJson.Serialize(finalized)));
                    Assert.That(WorldGate(finalized).ActiveOperation, Is.Null);
                    Assert.That(WorldGate(finalized).CompletedDefinitionIds,
                        Does.Contain(board.DefinitionId));
                    Assert.That(WorldGate(finalized).TravelSupplies,
                        Is.GreaterThanOrEqualTo(supplies));
                    Assert.That(_worldGate023.FinalizeOperation(
                        finalized, _catalog023).IsSuccess, Is.False);
                }
            }
            Assert.That(pathCount, Is.EqualTo(424));
            Assert.That(executedNodes.Count, Is.EqualTo(1372));
            Assert.That(executedBattleNodes.Count, Is.EqualTo(32));
        }

        [Test]
        public void EveryExposedNodeChoiceCommitsTheSameCanonicalReceiptAfterReload()
        {
            var executed = new HashSet<string>(StringComparer.Ordinal);
            var seed = 91000L;
            var battleOrdinal = 0;
            foreach (var board in _catalog023.AllBoards
                         .OrderBy(value => value.DefinitionId, StringComparer.Ordinal))
            {
                foreach (var target in board.Nodes
                             .OrderBy(value => value.NodeId, StringComparer.Ordinal))
                {
                    var campaign = CreateBoardReadyCampaign(board, seed++);
                    campaign = Require(_worldGate023.BeginOperation(campaign,
                        _catalog023, board.DefinitionId,
                        new[] { "BOARD_UNION_084" },
                        board.OperationKind == "CHAPTER" ? _catalog020 : null));
                    foreach (var prefixChoice in PrefixChoices084(board, target.NodeId))
                        campaign = CommitAndApplyWorldGateNode084(campaign, board,
                            prefixChoice, ref battleOrdinal);

                    Assert.That(WorldGate(campaign).ActiveOperation.CurrentNodeId,
                        Is.EqualTo(target.NodeId));
                    var choices = target.ChoiceIds.Count == 0
                        ? new[] { "CONTINUE" }
                        : target.ChoiceIds;
                    foreach (var choice in choices)
                    {
                        var key = board.DefinitionId + "/" + target.NodeId + "/" + choice;
                        Assert.That(executed.Add(key), Is.True, key);
                        var firstBase = JsonConvert.DeserializeObject<CampaignState>(
                            JsonConvert.SerializeObject(campaign));
                        var secondBase = JsonConvert.DeserializeObject<CampaignState>(
                            JsonConvert.SerializeObject(campaign));
                        Assert.That(firstBase, Is.Not.Null);
                        Assert.That(secondBase, Is.Not.Null);
                        var first = CommitWorldGateNodeOnly084(firstBase, target, choice,
                            "M2_MATRIX_084_" + battleOrdinal);
                        var second = CommitWorldGateNodeOnly084(secondBase, target, choice,
                            "M2_MATRIX_084_" + battleOrdinal);
                        var firstReceipt = WorldGate(first).ActiveOperation.PendingReceipt;
                        var secondReceipt = WorldGate(second).ActiveOperation.PendingReceipt;
                        Assert.That(firstReceipt, Is.Not.Null, key);
                        Assert.That(CanonicalJson.Serialize(secondReceipt),
                            Is.EqualTo(CanonicalJson.Serialize(firstReceipt)), key);
                        Assert.That(firstReceipt.ChoiceId, Is.EqualTo(choice), key);
                        if (target.CheckDifficulty > 0)
                            Assert.That(firstReceipt.DieOne, Is.InRange(1, 6), key);
                        battleOrdinal++;
                    }
                }
            }
            Assert.That(executed.Count, Is.EqualTo(3036));
        }

        [Test]
        public void WorldGatePendingBattleRejectsFreeFormM2StartAndUsesCommittedBridge()
        {
            var board = _catalog023.AllBoards
                .Where(value => value.Nodes.Any(node => node.RequiresCertifiedBattle))
                .OrderByDescending(value => value.Nodes.Max(node => node.EnemyUnionCount))
                .ThenBy(value => value.DefinitionId, StringComparer.Ordinal)
                .First();
            var battleNode = board.Nodes
                .Where(value => value.RequiresCertifiedBattle)
                .OrderByDescending(value => value.EnemyUnionCount)
                .ThenBy(value => value.NodeId, StringComparer.Ordinal)
                .First();
            var campaign = CreateBoardReadyCampaign(board, 93423);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                board.DefinitionId, new[] { "BOARD_UNION_084" },
                board.OperationKind == "CHAPTER" ? _catalog020 : null));
            var battleOrdinal = 0;
            foreach (var choice in PrefixChoices084(board, battleNode.NodeId))
                campaign = CommitAndApplyWorldGateNode084(
                    campaign, board, choice, ref battleOrdinal);
            Assert.That(WorldGate(campaign).ActiveOperation.CurrentNodeId,
                Is.EqualTo(battleNode.NodeId));
            campaign = Require(_worldGate023.CommitEncounter(campaign, _catalog023));
            var request = campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(request, Is.Not.Null);

            var combat = M2CombatContent.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT"));
            var battles = new M2BattleCommandService();
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                    request)), Is.True);
            var tamperedRequest = new EncounterLaunchRequest017D(
                request.RequestId, request.ContractId, request.ExpeditionId,
                request.BoardId, request.NodeId, request.EncounterId,
                request.BattleId, request.Objective + " (mutated saved request)",
                request.EnemyUnionCount, request.CanonicalSeedIdentity,
                request.AlliedUnionIds, request.ReserveUnionIds,
                request.ObjectiveIds, request.RouteModifiers, request.Supplies,
                request.Fatigue, request.Urgency, request.ReturnCheckpointId,
                request.PreBattleStateHash);
            var tamperedCity = campaign.Guild.GuildCity.With(
                pendingEncounter: tamperedRequest, replacePendingEncounter: true,
                lastCheckpointId: "mutated_saved_world_gate_request_084");
            var tamperedCampaign = campaign.With(
                campaign.Guild.WithGuildCity(tamperedCity), campaign.OpeningFlow);
            var tamperedStart = new GuildCityBattleBridgeService017D()
                .StartCertifiedEncounter(tamperedCampaign, battles, combat, resolver);
            Assert.That(tamperedStart.IsSuccess, Is.False);
            CollectionAssert.Contains(tamperedStart.Errors,
                "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED");
            Assert.That(tamperedCampaign.Battle, Is.Null);

            var weakened = battles.StartEncounterBattle(campaign, combat,
                request.BattleId, request.Objective + " (weakened direct call)",
                1, Array.Empty<string>(), request.AlliedUnionIds);
            Assert.That(weakened.IsSuccess, Is.False);
            CollectionAssert.Contains(weakened.Errors,
                "M2_PENDING_ENCOUNTER_REQUIRES_COMMITTED_BRIDGE");
            Assert.That(campaign.Battle, Is.Null);

            var roster = resolver.Resolve(campaign.CampaignSeed, request);
            var rosterBypass = battles.StartEncounterBattleWithRoster070(
                campaign, combat, request.BattleId,
                request.Objective + " (custom-roster direct call)", roster,
                Array.Empty<string>(), request.AlliedUnionIds);
            Assert.That(rosterBypass.IsSuccess, Is.False);
            CollectionAssert.Contains(rosterBypass.Errors,
                "M2_PENDING_ENCOUNTER_REQUIRES_COMMITTED_BRIDGE");

            var canonical = Require(new GuildCityBattleBridgeService017D()
                .StartCertifiedEncounter(campaign, battles, combat, resolver));
            Assert.That(canonical.Battle, Is.Not.Null);
            Assert.That(canonical.Battle.BattleId, Is.EqualTo(request.BattleId));
            Assert.That(canonical.Battle.EnemyUnions.Count,
                Is.EqualTo(request.EnemyUnionCount));

            var claimed = WithClaimedBattle(canonical, request.BattleId,
                "BOARD_RETURN_AUTHORITY_084");
            var returnCommitted = Require(_battleBridge.CommitBattleReturn(claimed));
            var returnReceipt = returnCommitted.Guild.GuildCity.PendingBattleReturn;
            var forgedReceipt = new BattleReturnReceipt017D(
                returnReceipt.ReceiptId, returnReceipt.LaunchRequestId,
                returnReceipt.BattleRunId, returnReceipt.Outcome,
                returnReceipt.BattleResultHash, returnReceipt.SupplyConsumption,
                returnReceipt.FatigueDelta, returnReceipt.UrgencyDelta,
                returnReceipt.ObjectiveFlags,
                new[] { new GuildMaterialState017D("MAT_GATE_IRON", 999999) },
                returnReceipt.EquipmentRewardReceiptId,
                returnReceipt.RelationshipMemories,
                returnReceipt.CityProjectContribution,
                returnReceipt.ReturnCheckpointId, returnReceipt.Applied);
            var forgedCity = returnCommitted.Guild.GuildCity.With(
                pendingBattleReturn: forgedReceipt,
                replacePendingBattleReturn: true,
                lastCheckpointId: "forged_world_gate_side_reward_084");
            var forged = returnCommitted.With(
                returnCommitted.Guild.WithGuildCity(forgedCity),
                returnCommitted.OpeningFlow);
            var forgedHash = CanonicalJson.Sha256Hex(forged);
            var rejectedReturn = _battleBridge.ApplyBattleReturnExactlyOnce(forged);
            Assert.That(rejectedReturn.IsSuccess, Is.False);
            CollectionAssert.Contains(rejectedReturn.Errors,
                "GC017D_BATTLE_RETURN_RECEIPT_MISMATCH");
            Assert.That(CanonicalJson.Sha256Hex(forged), Is.EqualTo(forgedHash));

            var returned = Require(_battleBridge.ApplyBattleReturnExactlyOnce(
                returnCommitted));
            var returnAuthorityId =
                GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(
                    returnReceipt);
            Assert.That(returned.Guild.Development.HasAdventureAuthority(
                returnAuthorityId), Is.True);
            var unanchoredReturn = WithoutAdventureAuthority084(
                returned, returnAuthorityId);
            var unanchoredHash = CanonicalJson.Sha256Hex(unanchoredReturn);
            var unanchoredRejected = _worldGate023.SynchronizeAfterClaimedBattle(
                unanchoredReturn, _catalog023);
            Assert.That(unanchoredRejected.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Sha256Hex(unanchoredReturn),
                Is.EqualTo(unanchoredHash));
            var synchronized = Require(_worldGate023.SynchronizeAfterClaimedBattle(
                returned, _catalog023));
            Assert.That(WorldGate(synchronized).ActiveOperation.PendingReceipt,
                Is.Not.Null);
        }

        [Test]
        public void CheckActorsMustBelongToTheCommittedUnionAndReloadCannotReroll()
        {
            var board = _catalog023.AllBoards.First(value =>
                value.Nodes.Any(node => node.CheckDifficulty > 0));
            var campaign = CreateBoardReadyCampaign(board, 89984);
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                board.DefinitionId, new[] { "BOARD_UNION_084" },
                board.OperationKind == "CHAPTER" ? _catalog020 : null));
            while (true)
            {
                var operation = WorldGate(campaign).ActiveOperation;
                var node = board.Nodes.Single(value =>
                    value.NodeId == operation.CurrentNodeId);
                if (node.CheckDifficulty > 0)
                {
                    var choice = node.ChoiceIds.First();
                    var rejected = _worldGate023.CommitNodeChoice(campaign,
                        _catalog023, choice, "BOARD_OUTSIDER_084",
                        "BOARD_RECRUIT_B_084", 0);
                    Assert.That(rejected.IsSuccess, Is.False);
                    CollectionAssert.Contains(rejected.Errors,
                        "CAMPAIGN023_CHECK_ACTOR_REQUIRED");
                    var committed = Require(_worldGate023.CommitNodeChoice(campaign,
                        _catalog023, choice, "BOARD_RECRUIT_A_084",
                        "BOARD_RECRUIT_B_084", 0));
                    var receipt = WorldGate(committed).ActiveOperation.PendingReceipt;
                    var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                        JsonConvert.SerializeObject(campaign));
                    var recommitted = Require(_worldGate023.CommitNodeChoice(reloaded,
                        _catalog023, choice, "BOARD_RECRUIT_A_084",
                        "BOARD_RECRUIT_B_084", 0));
                    var second = WorldGate(recommitted).ActiveOperation.PendingReceipt;
                    Assert.That(second.ReceiptId, Is.EqualTo(receipt.ReceiptId));
                    Assert.That(second.DieOne, Is.EqualTo(receipt.DieOne));
                    Assert.That(second.DieTwo, Is.EqualTo(receipt.DieTwo));
                    break;
                }
                var move = node.ChoiceIds.FirstOrDefault() ?? "CONTINUE";
                campaign = Require(_worldGate023.CommitNodeChoice(campaign,
                    _catalog023, move, "BOARD_RECRUIT_A_084",
                    "BOARD_RECRUIT_B_084", 0));
                campaign = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                    campaign, _catalog023));
            }
        }

        [Test]
        public void ClosedChapterProofSurvivesLegalUnionEditAndNextChapterCanBegin()
        {
            var campaign = CreateCanonicalStoryChapter("CH018_001", 93984,
                "CH018_002");
            campaign = ApplyCampaign020NonBattle(campaign, "chapter one briefing");
            campaign = CompleteWorldBoardMainPath(campaign, "CH018_001", "SKYHOME");
            campaign = ApplyCampaign020ReceiptExactly(
                Require(_campaign020.CommitCompletedWorldBoardStep(campaign,
                    _catalog020)), "chapter one world board");
            Assert.That(_catalog020.TryGetBlueprint("CH018_001", out var blueprint),
                Is.True);
            while (Playable(campaign).ActiveOperation.Status !=
                   CampaignPlayableOperationStatus020.ReadyToFinalize)
                campaign = ApplyCampaign020NonBattle(campaign, "chapter one close path");
            campaign = Require(_campaign019.CommitNonCombatReceipt(campaign,
                _catalog019, "SUCCESS"));
            campaign = Require(_campaign019.ApplyReceiptExactlyOnce(campaign,
                _catalog019,_catalog020));
            campaign = Require(_campaign020.CloseCompletedOperation(campaign,
                _catalog020));
            Assert.That(WorldGate(campaign).CompletionProofs.Count, Is.EqualTo(1));
            var proof = WorldGate(campaign).CompletionProofs.Single();
            var firstReceipt = proof.AppliedReceipts[0];
            var checkReceipt = proof.AppliedReceipts.First(value => value.DieOne > 0);
            var proofForgeries = new[]
            {
                RehashCompletionProof084(proof, proof.AppliedReceipts.Select(value =>
                    ReferenceEquals(value, firstReceipt)
                        ? CloneReceipt084(value, choiceId: "FORGED_CHOICE_084")
                        : value).ToArray()),
                RehashCompletionProof084(proof, proof.AppliedReceipts.Select(value =>
                    ReferenceEquals(value, checkReceipt)
                        ? CloneReceipt084(value,
                            dieOne: checkReceipt.DieOne == 6 ? 5 : checkReceipt.DieOne + 1)
                        : value).ToArray()),
                RehashCompletionProof084(proof, proof.AppliedReceipts.Select(value =>
                    ReferenceEquals(value, firstReceipt)
                        ? CloneReceipt084(value, supplyDelta: value.SupplyDelta + 1)
                        : value).ToArray()),
                RehashCompletionProof084(proof, proof.AppliedReceipts.Select(value =>
                    ReferenceEquals(value, firstReceipt)
                        ? CloneReceipt084(value,
                            authoritativeHash:
                                "A84F" + value.AuthoritativeHash.Substring(4),
                            receiptId: "WGREC023_" +
                                ("A84F" + value.AuthoritativeHash.Substring(4))
                                .Substring(0, 24).ToUpperInvariant())
                        : value).ToArray())
            };
            foreach (var forgery in proofForgeries)
                Assert.That(CampaignWorldGateCommandService023.ValidateCompletionProof084(
                    campaign, _catalog023, forgery), Is.False,
                    "A self-rehashed completion ledger must still replay against authored choices, dice, deltas, and hashes.");

            var union = campaign.Guild.Unions.Single(value =>
                value.UnionId == "BOARD_UNION_084");
            var editedUnion = new UnionState(union.UnionId, union.DisplayName, union.Kind,
                union.LeaderRecruitId, union.MemberRecruitIds, "FORMATION_WEDGE_084",
                union.DoctrineId, union.SharedAp, union.CohesionBasisPoints);
            var editedGuild = campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits, new[] { editedUnion }, campaign.Guild.Inventory,
                campaign.Guild.Development);
            campaign = campaign.With(editedGuild, campaign.OpeningFlow);
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                campaign, _catalog023, WorldGate(campaign)), Is.True);

            var canonicalRuntime = WorldGate(campaign);
            var runtimeForgeries = new[]
            {
                canonicalRuntime.With(currentWorldId: "GOBLIN"),
                canonicalRuntime.With(
                    travelSupplies: canonicalRuntime.TravelSupplies + 1000),
                canonicalRuntime.With(
                    repeatableStreakDefinitionId: "REPEAT020_SKYHOME_01",
                    repeatableStreakCount: 99),
                canonicalRuntime.With(
                    authorityChainBaseHash: string.Empty,
                    authorityChainBaseOperationOrdinal: 0,
                    authorityChainBaseStandings:
                        Array.Empty<WorldStandingState023>(),
                    authorityChainBaseUnlockedOrigins: Array.Empty<string>(),
                    authorityChainBaseUnlockedWorlds: Array.Empty<string>(),
                    authorityChainBaseCompletedDefinitions: Array.Empty<string>(),
                    authorityEntries: Array.Empty<WorldGateAuthorityEntry023>(),
                    authorityChainHash: string.Empty)
            };
            foreach (var forgery in runtimeForgeries)
                Assert.That(CampaignWorldGateCommandService023
                    .ValidateStoredCompletionProofs084(campaign, _catalog023,
                        forgery), Is.False,
                    "Travel, supplies, repeat streak, and a stripped chain must fail closed.");
            Assert.That(CampaignWorldGateCommandService023
                .AuthorityChainEntryLimit084, Is.GreaterThanOrEqualTo(65536));

            campaign = Require(_campaign019.StartChapter(campaign, _catalog019,
                "CH018_002", new[] { "BOARD_UNION_084" }, true));
            campaign = Require(_campaign020.BeginOperation(campaign, _catalog020,
                "CH018_002"));
            campaign = ApplyCampaign020NonBattle(campaign, "chapter two briefing");
            var next = _worldGate023.BeginOperation(campaign, _catalog023,
                "CH018_002", new[] { "BOARD_UNION_084" }, _catalog020);
            Assert.That(next.IsSuccess, Is.True, string.Join("\n", next.Errors));
        }

        CampaignState ApplyCampaign020NonBattle(CampaignState campaign, string label)
        {
            var committed = Require(_campaign020.CommitNonBattleStep(
                campaign, _catalog020, "SUCCESS"));
            return ApplyCampaign020ReceiptExactly(committed, label);
        }

        static WorldGateNodeReceipt023 CloneReceipt084(
            WorldGateNodeReceipt023 source, string choiceId = null,
            int? dieOne = null, int? supplyDelta = null,
            string authoritativeHash = null, string receiptId = null) =>
            new WorldGateNodeReceipt023(
                receiptId ?? source.ReceiptId, source.OperationId, source.NodeId,
                choiceId ?? source.ChoiceId, source.NextNodeId, source.Outcome,
                authoritativeHash ?? source.AuthoritativeHash,
                supplyDelta ?? source.SupplyDelta, source.FatigueDelta,
                source.UrgencyDelta, source.ThreatDelta, source.TrustDelta,
                source.TensionDelta, source.CivilianSupportDelta, source.GuildXp,
                source.HallXp, source.MaterialIds, source.ActorRecruitId,
                 source.AssistantRecruitId, dieOne ?? source.DieOne, source.DieTwo,
                 source.Modifier, source.AppliedVersion,
                 source.BattleReturnAuthorityId);

        static WorldGateCompletionProof023 RehashCompletionProof084(
            WorldGateCompletionProof023 source,
            System.Collections.Generic.IReadOnlyList<WorldGateNodeReceipt023> receipts)
        {
            WorldGateCompletionProof023 Build(string ledgerHash) =>
                new WorldGateCompletionProof023(
                    source.OperationId, source.DefinitionId, source.BoardId,
                    source.WorldId, source.CommittedOperationOrdinal,
                    source.InitialTrust, source.InitialTension,
                    source.InitialCivilianSupport, source.AlliedUnionIds,
                    source.RewardPermilleAtBegin, source.AlliedRosterIdentity,
                    source.CanonicalSeedIdentity, source.StartNodeId,
                    source.ExitNodeId, source.CompletedNodeIds, receipts,
                    source.InitialSupplies, source.FinalSupplies,
                    source.FinalFatigue, source.FinalUrgency, source.FinalThreat,
                    source.FinalTrust, source.FinalTension,
                    source.FinalCivilianSupport,
                    source.ExistingBattleRewardReceiptId, ledgerHash,
                    source.ProofVersion, source.BattleId,
                    source.BattleFinalStateHash, source.BattleOutcome,
                    source.BattleRewardId, source.AlliedRecruitIds,
                    source.PreviousAuthorityChainHash);
            var draft = Build(string.Empty);
            var method = typeof(CampaignWorldGateCommandService023).GetMethod(
                "CompletionLedgerHash084",
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var hash = (string)method.Invoke(null, new object[] { draft });
            return Build(hash);
        }

        CampaignState ApplyCampaign020ReceiptExactly(
            CampaignState committed, string label)
        {
            committed = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(committed));
            var receipt = Playable(committed).ActiveOperation.PendingReceipt;
            Assert.That(receipt, Is.Not.Null, label);
            var treasury = committed.Guild.TreasuryXp;
            var hall = committed.Guild.Development.HallEnhancementXp;
            var materials = WorldMaterialTotal(committed);
            var applied = Require(_campaign020.ApplyStepReceiptExactlyOnce(
                committed, _catalog020));
            Assert.That(applied.Guild.TreasuryXp - treasury,
                Is.EqualTo(receipt.GuildXp), label);
            Assert.That(applied.Guild.Development.HallEnhancementXp - hall,
                Is.EqualTo(receipt.HallXp), label);
            Assert.That(WorldMaterialTotal(applied) - materials,
                Is.EqualTo(receipt.Materials.Sum(value => value.Amount)), label);
            var replay = Require(_campaign020.ApplyStepReceiptExactlyOnce(
                committed, _catalog020));
            Assert.That(CanonicalJson.Serialize(replay),
                Is.EqualTo(CanonicalJson.Serialize(applied)), label);
            Assert.That(_campaign020.ApplyStepReceiptExactlyOnce(
                applied, _catalog020).IsSuccess, Is.False, label);
            return applied;
        }

        CampaignState CompleteWorldBoardMainPath(
            CampaignState campaign, string chapterId, string worldId)
        {
            campaign = Require(_worldGate023.Travel(campaign, _catalog023, worldId,
                _catalog020));
            campaign = Require(_worldGate023.BeginOperation(campaign, _catalog023,
                chapterId, new[] { "BOARD_UNION_084" }, _catalog020));
            while (WorldGate(campaign).ActiveOperation.Status !=
                   WorldGateOperationStatus023.ReadyToFinalize)
            {
                var operation = WorldGate(campaign).ActiveOperation;
                var board = _catalog023.AllBoards.Single(value =>
                    value.DefinitionId == chapterId);
                var node = board.Nodes.Single(value =>
                    value.NodeId == operation.CurrentNodeId);
                var committed = Require(_worldGate023.CommitNodeChoice(campaign,
                    _catalog023, node.ChoiceIds.FirstOrDefault() ?? "CONTINUE",
                    "BOARD_RECRUIT_A_084", "BOARD_RECRUIT_B_084", 0));
                campaign = ApplyWorldGateReceiptExactly(committed,
                    chapterId + " / " + node.NodeId);
            }
            return Require(_worldGate023.FinalizeOperation(campaign, _catalog023));
        }

        CampaignState ApplyWorldGateReceiptExactly(
            CampaignState committed, string label)
        {
            var receipt = WorldGate(committed).ActiveOperation.PendingReceipt;
            var treasury = committed.Guild.TreasuryXp;
            var hall = committed.Guild.Development.HallEnhancementXp;
            var materials = CityMaterialTotal(committed);
            var applied = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                committed, _catalog023));
            Assert.That(applied.Guild.TreasuryXp - treasury,
                Is.EqualTo(receipt.GuildXp), label);
            Assert.That(applied.Guild.Development.HallEnhancementXp - hall,
                Is.EqualTo(receipt.HallXp), label);
            Assert.That(CityMaterialTotal(applied) - materials,
                Is.EqualTo(receipt.MaterialIds.Count), label);
            var replay = Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                committed, _catalog023));
            Assert.That(CanonicalJson.Serialize(replay),
                Is.EqualTo(CanonicalJson.Serialize(applied)), label);
            return applied;
        }

        CampaignState CreateCanonicalStoryChapter(string chapterId, long seed,
            string additionalIncompleteChapterId = null)
        {
            var campaign = CreateRosterCampaign(seed);
            var allOtherChapters = _registry019.Base018.Chapters.Keys
                .Where(value => value != chapterId &&
                    value != additionalIncompleteChapterId).ToArray();
            var allStoryGates = _registry019.Base018.Arcs.Values
                .SelectMany(value => value.unlockGates ?? Array.Empty<string>())
                .Where(value => value.StartsWith("STORY:", StringComparison.Ordinal))
                .Select(value => value.Substring(6)).Distinct(StringComparer.Ordinal)
                .ToArray();
            var city = campaign.Guild.GuildCity;
            var progress = city.Strategic017H.Campaign019.With(
                completedChapterIds: allOtherChapters,
                unlockedArcIds: _registry019.Base018.Arcs.Keys.ToArray(),
                unlockedWorldIds: _registry020.Travel.Keys
                    .Concat(new[] { "SKYHOME" }).Distinct(StringComparer.Ordinal).ToArray(),
                lastCheckpointId: "all_prerequisites_084");
            var strategic = city.Strategic017H.With(storyGates: allStoryGates,
                campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city.With(
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId)), campaign.OpeningFlow);
            campaign = Require(_campaign019.StartChapter(campaign, _catalog019,
                chapterId, new[] { "BOARD_UNION_084" }, true));
            return Require(_campaign020.BeginOperation(campaign, _catalog020, chapterId));
        }

        CampaignState CreateBoardReadyCampaign(WorldGateBoardRule023 board, long seed)
        {
            if (board.OperationKind == "CHAPTER")
            {
                var campaign = CreateFakeStartedChapter(board.DefinitionId,
                    board.WorldId, seed);
                campaign = ApplyCampaign020NonBattle(campaign, "board fixture briefing");
                return Require(_worldGate023.Travel(campaign, _catalog023,
                    board.WorldId, _catalog020));
            }
            var standalone = CreateRosterCampaign(seed);
            var city = standalone.Guild.GuildCity;
            var progress = city.Strategic017H.Campaign019;
            var playable = progress.Playable020;
            if (StringComparer.Ordinal.Equals(board.OperationKind, "REPEATABLE"))
                playable = playable.With(unlockedRepeatableContractIds:
                    playable.UnlockedRepeatableContractIds.Concat(
                        new[] { board.DefinitionId }).Distinct(StringComparer.Ordinal)
                        .ToArray(), lastCheckpointId: "board_ready_084");
            progress = progress.With(
                unlockedWorldIds: new[] { "SKYHOME", board.WorldId }
                    .Distinct(StringComparer.Ordinal).ToArray(),
                playable020: playable, replacePlayable020: true,
                lastCheckpointId: "board_ready_084");
            var strategic = city.Strategic017H.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: "board_ready_084");
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: "board_ready_084");
            standalone = standalone.With(standalone.Guild.WithGuildCity(city),
                standalone.OpeningFlow);
            return Require(_worldGate023.Travel(standalone, _catalog023,
                board.WorldId));
        }

        CampaignState CreateFakeStartedChapter(
            string chapterId, string worldId, long seed)
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

        static CampaignState CreateRosterCampaign(long seed)
        {
            var recruitA = new RecruitState("BOARD_RECRUIT_A_084", 100, 100, 20, 20);
            var recruitB = new RecruitState("BOARD_RECRUIT_B_084", 100, 100, 20, 20);
            var outsider = new RecruitState("BOARD_OUTSIDER_084", 100, 100, 20, 20);
            var union = new UnionState("BOARD_UNION_084", "Adventure Union",
                UnionKind.Normal, recruitA.RecruitId,
                new[] { recruitA.RecruitId, recruitB.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(source.Guild.GuildId, source.Guild.TreasuryXp,
                new[] { recruitA, recruitB, outsider }, new[] { union },
                source.Guild.Inventory, source.Guild.Development, guildCity: null);
            var opening = new OpeningFlowState(OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0,
                true, true, true, false, "board_adventure_ready_084");
            var profile = new NewGuildProfileState(
                "Board Tester", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            return new CampaignState(
                source.CampaignGuid, source.CampaignSeed,
                source.ContentAuthorityVersion, source.Rules, guild,
                profile, opening, source.Battle);
        }

        static CampaignState WithClaimedBattle(
            CampaignState campaign, string battleId, string rewardId)
        {
            var member = new BattleMemberRewardState("BOARD_RECRUIT_A_084",
                "Board Tester", 1, 1, 1, 0, 0, 0, 0, 0, 0, 0);
            var reward = new BattleRewardState(rewardId, "BOARD_084_REWARD",
                BattleOutcome.Victory, 1, 1, 1000, 1000, 100, 100, 7, 5,
                new[] { member }, true);
            var playerUnions = campaign.Guild.Unions
                .Where(value => value != null && value.Kind == UnionKind.Normal)
                .Select(value =>
                {
                    var battleMember = new BattleMemberState(
                        value.LeaderRecruitId, value.LeaderRecruitId,
                        "BOARD_TEST_CLASS_084", 100, 100, 20, 20, 20, 20,
                        Array.Empty<string>(), false, false, false,
                        Array.Empty<string>(), 0, 0, string.Empty);
                    return new BattleUnionState(
                        value.UnionId, value.DisplayName, BattleSide.Player,
                        value.LeaderRecruitId, new[] { battleMember },
                        value.FormationId, value.FormationId, true, string.Empty,
                        1, 1, Math.Max(0, Math.Min(100,
                            value.CohesionBasisPoints / 100)),
                        value.CohesionBasisPoints, EngagementState.Open,
                        false, false, 0);
                }).ToArray();
            var battle = new BattleState(battleId, "BOARD_084", 1,
                BattlePhase.Resolved, BattleOutcome.Victory, "Board objective",
                playerUnions, Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(), Array.Empty<BattleRoundRecordState>(),
                "BOARD_FORECAST_HASH_084", "BOARD_INITIAL_HASH_084", string.Empty,
                string.Empty, string.Empty, false, reward);
            battle = battle.With(finalStateHash:
                M2BattleCommandService.AuthoritativeStateHash(battle));
            var development = campaign.Guild.Development.RecordBattleReward(
                rewardId, reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp + reward.GuildTreasuryXpAward,
                campaign.Guild.Recruits, campaign.Guild.Unions,
                campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow).WithBattle(battle);
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

        static CampaignState WithoutAdventureAuthority084(
            CampaignState campaign, string authorityId)
        {
            var source = campaign.Guild.Development;
            var development = new GuildDevelopmentState(
                source.HallStageIndex, source.HallStageId,
                source.HallEnhancementXp, source.LifetimeTreasuryXpEarned,
                source.Facilities, source.ClaimedBattleRewardIds,
                source.AppliedAdventureAuthorityIds.Where(value =>
                    !StringComparer.Ordinal.Equals(value, authorityId)).ToArray());
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits, campaign.Guild.Unions,
                campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        static CampaignState WithPlayableOperationAndLedger(
            CampaignState campaign, CampaignPlayableOperationState020 operation,
            IReadOnlyList<CampaignStepReceipt020> ledger)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(activeOperation: operation,
                replaceActiveOperation: true, activeStepLedger: ledger,
                lastCheckpointId: operation.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: operation.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: operation.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: operation.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState WithWorldGateOperationLedgerAndExpedition(
            CampaignState campaign, WorldGateRuntimeState023 source,
            ExpeditionState017D expedition)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var runtime = playable.WorldGate023.With(
                activeOperation: source.ActiveOperation, replaceActiveOperation: true,
                worldStandings: source.WorldStandings,
                activeNodeLedger: source.ActiveNodeLedger,
                lastCheckpointId: source.LastCheckpointId);
            playable = playable.With(worldGate023: runtime,
                replaceWorldGate023: true, lastCheckpointId: source.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: source.LastCheckpointId);
            strategic = strategic.With(campaign019: progress,
                replaceCampaign019: true, lastCheckpointId: source.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                expedition: expedition, replaceExpedition: true,
                lastCheckpointId: source.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static IReadOnlyList<IReadOnlyList<string>> SemanticPaths(
            WorldGateBoardRule023 board)
        {
            var result = new List<IReadOnlyList<string>>();
            BuildSemanticPaths(board, board.StartNodeId, new List<string>(), result);
            return result.AsReadOnly();
        }

        static void BuildSemanticPaths(
            WorldGateBoardRule023 board, string nodeId, List<string> route,
            List<IReadOnlyList<string>> result)
        {
            var node = board.Nodes.Single(value => value.NodeId == nodeId);
            var choices = node.ChoiceIds.Count == 0
                ? new[] { "CONTINUE" }
                : node.ChoiceIds;
            var distinctDestinations = new List<KeyValuePair<string, string>>();
            foreach (var choice in choices)
            {
                Assert.That(BoardAdventureRules084.TryNextNode084(
                    node, choice, out var destination), Is.True);
                if (distinctDestinations.All(value => value.Value != destination))
                    distinctDestinations.Add(
                        new KeyValuePair<string, string>(choice, destination));
            }
            foreach (var option in distinctDestinations)
            {
                var nextRoute = new List<string>(route) { option.Key };
                if (string.IsNullOrWhiteSpace(option.Value))
                    result.Add(nextRoute.AsReadOnly());
                else
                    BuildSemanticPaths(board, option.Value, nextRoute, result);
            }
        }

        static IReadOnlyList<string> PrefixChoices084(
            WorldGateBoardRule023 board, string targetNodeId)
        {
            var queue = new Queue<KeyValuePair<string, IReadOnlyList<string>>>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            queue.Enqueue(new KeyValuePair<string, IReadOnlyList<string>>(
                board.StartNodeId, Array.Empty<string>()));
            while (queue.Count > 0)
            {
                var entry = queue.Dequeue();
                if (!visited.Add(entry.Key)) continue;
                if (StringComparer.Ordinal.Equals(entry.Key, targetNodeId))
                    return entry.Value;
                var node = board.Nodes.Single(value => value.NodeId == entry.Key);
                var choices = node.ChoiceIds.Count == 0
                    ? new[] { "CONTINUE" }
                    : node.ChoiceIds;
                var destinations = new HashSet<string>(StringComparer.Ordinal);
                foreach (var choice in choices)
                {
                    Assert.That(BoardAdventureRules084.TryNextNode084(
                        node, choice, out var destination), Is.True);
                    if (string.IsNullOrWhiteSpace(destination) ||
                        !destinations.Add(destination)) continue;
                    queue.Enqueue(new KeyValuePair<string, IReadOnlyList<string>>(
                        destination,
                        new List<string>(entry.Value) { choice }.AsReadOnly()));
                }
            }
            Assert.Fail("No canonical prefix reaches " + board.DefinitionId + "/" +
                        targetNodeId);
            return Array.Empty<string>();
        }

        CampaignState CommitAndApplyWorldGateNode084(
            CampaignState campaign, WorldGateBoardRule023 board, string choice,
            ref int battleOrdinal)
        {
            var operation = WorldGate(campaign).ActiveOperation;
            var node = board.Nodes.Single(value =>
                value.NodeId == operation.CurrentNodeId);
            var committed = CommitWorldGateNodeOnly084(campaign, node, choice,
                "M2_PREFIX_084_" + battleOrdinal++);
            return Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                committed, _catalog023));
        }

        CampaignState CommitWorldGateNodeOnly084(
            CampaignState campaign, WorldGateNodeRule023 node, string choice,
            string rewardId)
        {
            if (!node.RequiresCertifiedBattle)
                return Require(_worldGate023.CommitNodeChoice(campaign, _catalog023,
                    choice, "BOARD_RECRUIT_A_084", "BOARD_RECRUIT_B_084", 0));
            campaign = Require(_worldGate023.CommitEncounter(campaign, _catalog023));
            var encounter = campaign.Guild.GuildCity.PendingEncounter;
            campaign = WithClaimedBattle(campaign, encounter.BattleId, rewardId);
            campaign = Require(_battleBridge.CommitBattleReturn(campaign));
            campaign = Require(_battleBridge.ApplyBattleReturnExactlyOnce(campaign));
            return Require(_worldGate023.SynchronizeAfterClaimedBattle(
                campaign, _catalog023));
        }

        static CampaignPlayableState020 Playable(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            Playable(campaign).WorldGate023;

        static int WorldMaterialTotal(CampaignState campaign) =>
            Playable(campaign).WorldMaterials.Sum(value => value.Amount);

        static int CityMaterialTotal(CampaignState campaign) =>
            campaign.Guild.GuildCity.Materials.Sum(value => value.Amount);

        static int GateVisitCount(CampaignState campaign, string worldId)
        {
            var gate = Playable(campaign).WorldGates.FirstOrDefault(value =>
                value.WorldId == worldId);
            return gate?.VisitCount ?? 0;
        }

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
