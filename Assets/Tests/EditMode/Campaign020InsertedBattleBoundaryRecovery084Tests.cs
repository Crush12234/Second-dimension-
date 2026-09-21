using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;

namespace SecondDimension.Tests.EditMode
{
    [TestFixture]
    public sealed class Campaign020InsertedBattleBoundaryRecovery084Tests
    {
        static readonly string[] ChangedChapters=
        {
            "CH018_003","CH018_006","CH018_009","CH018_012",
            "CH018_015","CH018_018","CH018_025","CH018_028",
            "CH018_031","CH018_037","CH018_040","CH018_045",
            "CH018_048","CH018_053","CH018_056","CH018_061",
            "CH018_064","CH018_069","CH018_072","CH018_077",
            "CH018_080"
        };

        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignPlayableCommandService020 _campaign020;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignWorldGateCommandService023 _worldGate023;

        [SetUp]
        public void SetUp()
        {
            _registry020=CampaignRegistry020.LoadFromResources();
            _catalog020=new Campaign020RuleCatalogAdapter(_registry020);
            _campaign020=new CampaignPlayableCommandService020();
            _registry023=CampaignRegistry023.LoadFromResources();
            _catalog023=new Campaign023RuleCatalogAdapter(_registry023);
            _worldGate023=new CampaignWorldGateCommandService023();
        }

        [Test]
        public void RecoveryScope_IsExactlyTheTwentyOneInsertedBattleChapters()
        {
            Assert.That(typeof(CampaignPlayablePresentationState020).GetField(
                "InsertedBattleBoundaryRecoveryRequired"),Is.Not.Null,
                "Player presentation must expose the distinct safe recovery route.");
            var field=typeof(CampaignPlayableCommandService020).GetField(
                "InsertedBattleChapters084",
                BindingFlags.NonPublic|BindingFlags.Static);
            Assert.That(field,Is.Not.Null);
            var actual=(HashSet<string>)field.GetValue(null);
            CollectionAssert.AreEquivalent(ChangedChapters,actual);
            Assert.That(ChangedChapters.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(21));
            foreach(var chapterId in ChangedChapters)
                Assert.That(
                    CampaignPlayableCommandService020
                        .IsInsertedBattleChapter084(chapterId),
                    Is.True,chapterId);
            Assert.That(CampaignPlayableCommandService020
                .IsInsertedBattleChapter084("CH018_021"),Is.False,
                "An original certified-battle chapter must never enter insertion recovery.");
            Assert.That(CampaignPlayableCommandService020
                .IsInsertedBattleChapter084("CH018_032"),Is.False);
        }

        [Test]
        public void ForgedDuplicateStepReceipts_FailClosedWithoutThrowing()
        {
            var registry019=CampaignRegistry019.LoadFromResources();
            var catalog019=new CampaignRuleCatalogAdapter019(registry019.Base018);
            var commands019=new CampaignCommandService019();
            var campaign=CreateRosterCampaign(84303);
            campaign=WithCompletedPrologue(campaign);
            campaign=Require(commands019.StartChapter(campaign,catalog019,
                "CH018_003",new[]{"BOARD_UNION_084"},true));
            campaign=Require(_campaign020.BeginOperation(campaign,_catalog020,
                "CH018_003"));
            var operation=Playable(campaign).ActiveOperation;
            Assert.That(_catalog020.TryGetBlueprint("CH018_003",
                out var blueprint),Is.True);
            var battleIndex=blueprint.Steps.Count-2;
            var receipts=Enumerable.Range(0,battleIndex).Select(index=>
                new CampaignStepReceipt020("DUPREC084_"+index,
                    operation.OperationId,
                    index==battleIndex-1
                        ?blueprint.Steps[0].StepId
                        :blueprint.Steps[index].StepId,
                    "SUCCESS","NONBATTLE:DUMMY_"+index,string.Empty,0,0,
                    Array.Empty<WorldMaterialAmount020>(),0)).ToArray();
            var forged=operation.With(currentStepIndex:battleIndex,
                status:CampaignPlayableOperationStatus020.Active,
                completedStepIds:blueprint.Steps.Take(battleIndex)
                    .Select(value=>value.StepId).ToArray(),
                appliedReceiptIds:receipts.Select(value=>value.ReceiptId).ToArray(),
                pendingReceipt:null,replacePendingReceipt:true,
                lastCheckpointId:"campaign020_step_committed",
                appliedReceipts:receipts);
            campaign=WithPlayableOperationAndLedger(campaign,forged,receipts);

            var required=false;
            Assert.DoesNotThrow(()=>required=_campaign020
                .RequiresInsertedBattleBoundaryRecovery084(campaign,_catalog020));
            Assert.That(required,Is.False);
        }

        [Test]
        public void PendingOldResults_RecoveryClearsOnlyPendingReceipt_AndCannotSkipBattle()
        {
            var campaign=CreateAtInsertedBattleBoundary();
            var legacyCatalog=new LegacyWithoutInsertedBattleCatalog020(
                _catalog020,"CH018_003");
            campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                legacyCatalog,"SUCCESS"));
            var pending=Playable(campaign).ActiveOperation.PendingReceipt;
            Assert.That(pending,Is.Not.Null);
            Assert.That(_campaign020.RequiresInsertedBattleBoundaryRecovery084(
                campaign,_catalog020),Is.True);
            var treasury=campaign.Guild.TreasuryXp;
            var hallXp=campaign.Guild.Development.HallEnhancementXp;
            var materials=MaterialLedger(campaign);
            var authorityCount=campaign.Guild.Development
                .AppliedAdventureAuthorityIds.Count;

            var blocked=_campaign020.CommitNonBattleStep(campaign,_catalog020,
                "SUCCESS");
            Assert.That(blocked.IsSuccess,Is.False);
            CollectionAssert.Contains(blocked.Errors,
                "CAMPAIGN020_INSERTED_BATTLE_RECOVERY_REQUIRED");
            campaign=Require(_campaign020.RecoverInsertedBattleBoundary084(
                campaign,_catalog020));
            var operation=Playable(campaign).ActiveOperation;
            Assert.That(operation.PendingReceipt,Is.Null);
            Assert.That(operation.Status,
                Is.EqualTo(CampaignPlayableOperationStatus020.Active));
            Assert.That(_catalog020.TryGetBlueprint("CH018_003",
                out var blueprint),Is.True);
            Assert.That(operation.CurrentStepIndex,
                Is.EqualTo(blueprint.Steps.Count-2));
            Assert.That(blueprint.Steps[operation.CurrentStepIndex]
                .RequiresCertifiedBattle,Is.True);
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                pending.ReceiptId),Is.False);
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(authorityCount));
            Assert.That(campaign.Guild.TreasuryXp,Is.EqualTo(treasury));
            Assert.That(campaign.Guild.Development.HallEnhancementXp,
                Is.EqualTo(hallXp));
            Assert.That(MaterialLedger(campaign),Is.EqualTo(materials));
            var cannotSkip=_campaign020.CommitNonBattleStep(campaign,_catalog020,
                "SUCCESS");
            Assert.That(cannotSkip.IsSuccess,Is.False);
            CollectionAssert.Contains(cannotSkip.Errors,
                "CAMPAIGN020_CERTIFIED_BATTLE_REQUIRED");
            Assert.That(_campaign020.IsReadyForChapterReceipt084(campaign,
                _catalog020,"CH018_003",out _),Is.False);
        }

        [Test]
        public void PendingOldResults_WithContradictoryClaimedReward_FailsClosed()
        {
            var campaign=CreateAtInsertedBattleBoundary();
            var legacyCatalog=new LegacyWithoutInsertedBattleCatalog020(
                _catalog020,"CH018_003");
            campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                legacyCatalog,"SUCCESS"));
            var pending=Playable(campaign).ActiveOperation.PendingReceipt;
            var development=campaign.Guild.Development.RecordBattleReward(
                pending.ReceiptId,1,1);
            var guild=campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development);
            campaign=campaign.With(guild,campaign.OpeningFlow);

            Assert.That(_campaign020.RequiresInsertedBattleBoundaryRecovery084(
                campaign,_catalog020),Is.False);
            var result=_campaign020.RecoverInsertedBattleBoundary084(campaign,
                _catalog020);
            Assert.That(result.IsSuccess,Is.False);
            CollectionAssert.Contains(result.Errors,
                "CAMPAIGN020_INSERTED_BATTLE_RECOVERY_NOT_REQUIRED");
            Assert.That(Playable(campaign).ActiveOperation.PendingReceipt.ReceiptId,
                Is.EqualTo(pending.ReceiptId));
        }

        [Test]
        public void AppliedOldResults_RecoveryPreservesGlobalExactOnceAuthority_AndRewindsLocalLedger()
        {
            var campaign=CreateAtInsertedBattleBoundary();
            var legacyCatalog=new LegacyWithoutInsertedBattleCatalog020(
                _catalog020,"CH018_003");
            campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                legacyCatalog,"SUCCESS"));
            var oldResults=Playable(campaign).ActiveOperation.PendingReceipt;
            campaign=Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                legacyCatalog));
            Assert.That(Playable(campaign).ActiveOperation.Status,
                Is.EqualTo(CampaignPlayableOperationStatus020.ReadyToFinalize));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                oldResults.ReceiptId),Is.True);
            var treasury=campaign.Guild.TreasuryXp;
            var hallXp=campaign.Guild.Development.HallEnhancementXp;
            var materials=MaterialLedger(campaign);
            var authorityCount=campaign.Guild.Development
                .AppliedAdventureAuthorityIds.Count;

            campaign=Require(_campaign020.RecoverInsertedBattleBoundary084(
                campaign,_catalog020));
            var operation=Playable(campaign).ActiveOperation;
            Assert.That(operation.Status,
                Is.EqualTo(CampaignPlayableOperationStatus020.Active));
            Assert.That(_catalog020.TryGetBlueprint("CH018_003",
                out var blueprint),Is.True);
            Assert.That(operation.CurrentStepIndex,
                Is.EqualTo(blueprint.Steps.Count-2));
            Assert.That(operation.CompletedStepIds,Does.Not.Contain(
                oldResults.StepId));
            Assert.That(operation.AppliedReceiptIds,Does.Not.Contain(
                oldResults.ReceiptId));
            Assert.That(operation.AppliedReceipts.Select(value=>value.ReceiptId),
                Does.Not.Contain(oldResults.ReceiptId));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                oldResults.ReceiptId),Is.True,
                "Recovery must retain the already-applied global exact-once proof.");
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(authorityCount+1),
                "Only the zero-reward migration provenance marker may be added.");
            Assert.That(campaign.Guild.TreasuryXp,Is.EqualTo(treasury));
            Assert.That(campaign.Guild.Development.HallEnhancementXp,
                Is.EqualTo(hallXp));
            Assert.That(MaterialLedger(campaign),Is.EqualTo(materials));
            Assert.That(_campaign020.IsReadyForChapterReceipt084(campaign,
                _catalog020,"CH018_003",out _),Is.False,
                "The old return-home receipt must never bypass the new battle.");

            var registry019=CampaignRegistry019.LoadFromResources();
            var catalog019=new CampaignRuleCatalogAdapter019(registry019.Base018);
            var commands019=new CampaignCommandService019();
            campaign=Require(commands019.CommitCertifiedBattle(campaign,catalog019));
            campaign=Require(_campaign020.MarkBattleCommitted(campaign,_catalog020));
            var encounter=campaign.Guild.GuildCity.PendingEncounter;
            campaign=WithClaimedBattle(campaign,encounter.BattleId,
                "M2_C020_MIGRATION_084");
            campaign=Require(commands019.CommitClaimedBattleReceiptAndClearEncounter(
                campaign,catalog019));
            campaign=Require(_campaign020.CommitBattleStepReceipt(campaign,
                _catalog020));
            var battleStepReceipt=Playable(campaign).ActiveOperation.PendingReceipt;
            var beforeBattleApplyTreasury=campaign.Guild.TreasuryXp;
            var beforeBattleApplyHall=campaign.Guild.Development.HallEnhancementXp;
            var beforeBattleApplyAuthorities=campaign.Guild.Development
                .AppliedAdventureAuthorityIds.Count;
            campaign=Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                _catalog020));
            operation=Playable(campaign).ActiveOperation;
            Assert.That(operation.Status,
                Is.EqualTo(CampaignPlayableOperationStatus020.ReadyToFinalize));
            Assert.That(operation.CurrentStepIndex,
                Is.EqualTo(blueprint.Steps.Count));
            Assert.That(operation.CompletedStepIds,
                Does.Contain(blueprint.Steps[blueprint.Steps.Count-2].StepId));
            Assert.That(operation.CompletedStepIds,Does.Contain(oldResults.StepId));
            Assert.That(operation.AppliedReceipts.Count(value=>
                StringComparer.Ordinal.Equals(value.ReceiptId,
                    oldResults.ReceiptId)),Is.EqualTo(1));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                oldResults.ReceiptId),Is.True);
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(beforeBattleApplyAuthorities+1),
                "Only the certified battle step authority may be added after recovery.");
            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(beforeBattleApplyTreasury+battleStepReceipt.GuildXp));
            Assert.That(campaign.Guild.Development.HallEnhancementXp,
                Is.EqualTo(beforeBattleApplyHall+battleStepReceipt.HallXp));
            Assert.That(_campaign020.IsReadyForChapterReceipt084(campaign,
                _catalog020,"CH018_003",out _),Is.True);
        }

        [Test]
        public void AppliedOldResults_RecoveryRequiresMarkerAndBattleLedgerCapacity()
        {
            var campaign=CreateAppliedLegacyResults(out var oldResults);
            var source=campaign.Guild.Development;
            var authorities=new List<string>(source.AppliedAdventureAuthorityIds);
            for(var index=0;
                authorities.Count<GuildDevelopmentState.AdventureAuthorityEntryLimit-1;
                index++)authorities.Add("CAPACITY084_"+index.ToString("D8"));
            var development=new GuildDevelopmentState(source.HallStageIndex,
                source.HallStageId,source.HallEnhancementXp,
                source.LifetimeTreasuryXpEarned,source.Facilities,
                source.ClaimedBattleRewardIds,authorities);
            var guild=campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development);
            campaign=campaign.With(guild,campaign.OpeningFlow);
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                oldResults.ReceiptId),Is.True);
            Assert.That(_campaign020.RequiresInsertedBattleBoundaryRecovery084(
                campaign,_catalog020),Is.True);

            var result=_campaign020.RecoverInsertedBattleBoundary084(campaign,
                _catalog020);
            Assert.That(result.IsSuccess,Is.False);
            CollectionAssert.Contains(result.Errors,
                "CAMPAIGN020_ADVENTURE_AUTHORITY_LEDGER_FULL");
            Assert.That(campaign.Guild.Development.AppliedAdventureAuthorityIds.Count,
                Is.EqualTo(GuildDevelopmentState.AdventureAuthorityEntryLimit-1));
            Assert.That(Playable(campaign).ActiveOperation.Status,
                Is.EqualTo(CampaignPlayableOperationStatus020.ReadyToFinalize),
                "Capacity failure must leave the legacy operation untouched.");
        }

        CampaignState CreateAppliedLegacyResults(
            out CampaignStepReceipt020 oldResults)
        {
            var campaign=CreateAtInsertedBattleBoundary();
            var legacyCatalog=new LegacyWithoutInsertedBattleCatalog020(
                _catalog020,"CH018_003");
            campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                legacyCatalog,"SUCCESS"));
            oldResults=Playable(campaign).ActiveOperation.PendingReceipt;
            return Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                legacyCatalog));
        }

        CampaignState CreateAtInsertedBattleBoundary()
        {
            var registry019=CampaignRegistry019.LoadFromResources();
            var catalog019=new CampaignRuleCatalogAdapter019(registry019.Base018);
            var commands019=new CampaignCommandService019();
            var campaign=WithCompletedPrologue(CreateRosterCampaign(84303));
            campaign=Require(commands019.StartChapter(campaign,catalog019,
                "CH018_003",new[]{"BOARD_UNION_084"},true));
            campaign=Require(_campaign020.BeginOperation(campaign,_catalog020,
                "CH018_003"));
            campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                _catalog020,"SUCCESS"));
            campaign=Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                _catalog020));
            campaign=CompleteWorldBoardMainPath(campaign,"CH018_003","SKYHOME");
            campaign=Require(_campaign020.CommitCompletedWorldBoardStep(campaign,
                _catalog020));
            campaign=Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                _catalog020));
            Assert.That(_catalog020.TryGetBlueprint("CH018_003",
                out var blueprint),Is.True);
            while(Playable(campaign).ActiveOperation.CurrentStepIndex<
                  blueprint.Steps.Count-2)
            {
                campaign=Require(_campaign020.CommitNonBattleStep(campaign,
                    _catalog020,"SUCCESS"));
                campaign=Require(_campaign020.ApplyStepReceiptExactlyOnce(campaign,
                    _catalog020));
            }
            return campaign;
        }

        CampaignState CompleteWorldBoardMainPath(CampaignState campaign,
            string chapterId,string worldId)
        {
            campaign=Require(_worldGate023.Travel(campaign,_catalog023,worldId,
                _catalog020));
            campaign=Require(_worldGate023.BeginOperation(campaign,_catalog023,
                chapterId,new[]{"BOARD_UNION_084"},_catalog020));
            while(WorldGate(campaign).ActiveOperation.Status!=
                  WorldGateOperationStatus023.ReadyToFinalize)
            {
                var operation=WorldGate(campaign).ActiveOperation;
                var board=_catalog023.AllBoards.Single(value=>
                    value.DefinitionId==chapterId);
                var node=board.Nodes.Single(value=>
                    value.NodeId==operation.CurrentNodeId);
                Assert.That(node.RequiresCertifiedBattle,Is.False,
                    "CH003 migration fixture must stay nonbattle inside C023.");
                campaign=Require(_worldGate023.CommitNodeChoice(campaign,
                    _catalog023,node.ChoiceIds.FirstOrDefault()??"CONTINUE",
                    "BOARD_RECRUIT_A_084","BOARD_RECRUIT_B_084",0));
                campaign=Require(_worldGate023.ApplyNodeReceiptExactlyOnce(
                    campaign,_catalog023));
            }
            return Require(_worldGate023.FinalizeOperation(campaign,_catalog023));
        }

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            Playable(campaign).WorldGate023;

        static string MaterialLedger(CampaignState campaign) =>
            string.Join("|",Playable(campaign).WorldMaterials.Select(value=>
                value.MaterialId+":"+value.Amount));

        static CampaignState WithClaimedBattle(CampaignState campaign,
            string battleId,string rewardId)
        {
            var member=new BattleMemberRewardState("BOARD_RECRUIT_A_084",
                "Board Tester",1,1,1,0,0,0,0,0,0,0);
            var reward=new BattleRewardState(rewardId,"BOARD_084_REWARD",
                BattleOutcome.Victory,1,1,1000,1000,100,100,7,5,
                new[]{member},true);
            var playerUnions=campaign.Guild.Unions.Where(value=>value!=null&&
                value.Kind==UnionKind.Normal).Select(value=>
            {
                var battleMember=new BattleMemberState(value.LeaderRecruitId,
                    value.LeaderRecruitId,"BOARD_TEST_CLASS_084",100,100,20,20,
                    20,20,Array.Empty<string>(),false,false,false,
                    Array.Empty<string>(),0,0,string.Empty);
                return new BattleUnionState(value.UnionId,value.DisplayName,
                    BattleSide.Player,value.LeaderRecruitId,new[]{battleMember},
                    value.FormationId,value.FormationId,true,string.Empty,1,1,
                    Math.Max(0,Math.Min(100,value.CohesionBasisPoints/100)),
                    value.CohesionBasisPoints,EngagementState.Open,false,false,0);
            }).ToArray();
            var battle=new BattleState(battleId,"BOARD_084",1,
                BattlePhase.Resolved,BattleOutcome.Victory,"Board objective",
                playerUnions,Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(),
                Array.Empty<BattleRoundRecordState>(),"BOARD_FORECAST_HASH_084",
                "BOARD_INITIAL_HASH_084",string.Empty,string.Empty,string.Empty,
                false,reward);
            battle=battle.With(finalStateHash:
                M2BattleCommandService.AuthoritativeStateHash(battle));
            var development=campaign.Guild.Development.RecordBattleReward(rewardId,
                reward.GuildTreasuryXpAward,reward.HallEnhancementXpAward);
            var guild=campaign.Guild.With(
                campaign.Guild.TreasuryXp+reward.GuildTreasuryXpAward,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development);
            return campaign.With(guild,campaign.OpeningFlow).WithBattle(battle);
        }

        sealed class LegacyWithoutInsertedBattleCatalog020 :
            ICampaignPlayableCatalog020
        {
            readonly ICampaignPlayableCatalog020 _source;
            readonly string _chapterId;
            readonly CampaignBlueprintRule020 _legacy;

            public LegacyWithoutInsertedBattleCatalog020(
                ICampaignPlayableCatalog020 source,string chapterId)
            {
                _source=source;
                _chapterId=chapterId;
                if(!source.TryGetBlueprint(chapterId,out var current))
                    throw new ArgumentException("Unknown chapter.",nameof(chapterId));
                _legacy=new CampaignBlueprintRule020
                {
                    BlueprintId=current.BlueprintId,
                    ChapterId=current.ChapterId,
                    ArcId=current.ArcId,
                    WorldId=current.WorldId,
                    MapId=current.MapId,
                    SiegeId=current.SiegeId,
                    EnemyPackId=current.EnemyPackId,
                    LootProfileId=current.LootProfileId,
                    MaterialIds=current.MaterialIds,
                    RepeatableUnlockIds=current.RepeatableUnlockIds,
                    Steps=current.Steps.Where(value=>
                        !value.RequiresCertifiedBattle).ToArray(),
                    RequiresCertifiedBattle=false,
                    MaximumAlliedUnions=current.MaximumAlliedUnions,
                    MaximumEnemyUnions=current.MaximumEnemyUnions
                };
                Assert.That(CampaignAdventureRules084.IsCompatible084(_legacy,
                    out var error),Is.True,error);
            }

            public bool TryGetBlueprint(string chapterId,
                out CampaignBlueprintRule020 blueprint)
            {
                if(StringComparer.Ordinal.Equals(chapterId,_chapterId))
                {
                    blueprint=_legacy;
                    return true;
                }
                return _source.TryGetBlueprint(chapterId,out blueprint);
            }

            public IReadOnlyList<string> RepeatableContractsForWorld(
                string worldId) => _source.RepeatableContractsForWorld(worldId);

            public bool IsKnownMaterial(string materialId) =>
                _source.IsKnownMaterial(materialId);

            public IWorldGateOperationsCatalog023 WorldGateProofCatalog023 =>
                _source.WorldGateProofCatalog023;
        }

        static CampaignState WithCompletedPrologue(CampaignState campaign)
        {
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H;
            var progress=strategic.Campaign019.With(
                completedChapterIds:new[]{"CH018_001","CH018_002"},
                lastCheckpointId:"migration_fixture");
            strategic=strategic.With(campaign019:progress,
                replaceCampaign019:true,lastCheckpointId:progress.LastCheckpointId);
            city=city.With(strategic017H:strategic,replaceStrategic017H:true,
                lastCheckpointId:progress.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        static CampaignState WithPlayableOperationAndLedger(
            CampaignState campaign,CampaignPlayableOperationState020 operation,
            IReadOnlyList<CampaignStepReceipt020> ledger)
        {
            var city=campaign.Guild.GuildCity;
            var strategic=city.Strategic017H;
            var progress=strategic.Campaign019;
            var playable=progress.Playable020.With(activeOperation:operation,
                replaceActiveOperation:true,activeStepLedger:ledger,
                lastCheckpointId:operation.LastCheckpointId);
            progress=progress.With(playable020:playable,replacePlayable020:true,
                lastCheckpointId:operation.LastCheckpointId);
            strategic=strategic.With(campaign019:progress,
                replaceCampaign019:true,lastCheckpointId:operation.LastCheckpointId);
            city=city.With(strategic017H:strategic,replaceStrategic017H:true,
                lastCheckpointId:operation.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        static CampaignState CreateRosterCampaign(long seed)
        {
            var recruitA=new RecruitState("BOARD_RECRUIT_A_084",100,100,20,20);
            var recruitB=new RecruitState("BOARD_RECRUIT_B_084",100,100,20,20);
            var union=new UnionState("BOARD_UNION_084","Adventure Union",
                UnionKind.Normal,recruitA.RecruitId,
                new[]{recruitA.RecruitId,recruitB.RecruitId},"FORMATION_LINE",
                "DOCTRINE_BALANCED",20,8000);
            var source=CampaignFactory.CreateM0Proof(seed);
            var guild=new GuildState(source.Guild.GuildId,
                source.Guild.TreasuryXp,new[]{recruitA,recruitB},new[]{union},
                source.Guild.Inventory,source.Guild.Development,guildCity:null);
            return source.With(guild,source.OpeningFlow);
        }

        static CampaignPlayableState020 Playable(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));
            return result.Value;
        }
    }
}
