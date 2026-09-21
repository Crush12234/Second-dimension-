#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignReplay130Tests
    {
        readonly SyntheticCatalog130 _rules=new SyntheticCatalog130();
        readonly CampaignCommandService019 _chapters=new CampaignCommandService019();
        readonly CampaignPlayableCommandService020 _steps=new CampaignPlayableCommandService020();
        readonly CampaignWorldGateCommandService023 _boards=new CampaignWorldGateCommandService023();
        readonly CampaignReplayCommandService130 _cycles=new CampaignReplayCommandService130();
        CampaignState _completed;
        static readonly object EarnedFixtureLock130=new object();
        static CampaignState _earnedFirstCycle130,_earnedThirdStart130;
        static string _earnedFirstHash130,_earnedThirdHash130;
        internal CampaignState CompletedSyntheticCycle130=>_completed;
        internal ICampaignRuleCatalog019 SyntheticChapters130=>_rules;
        internal ICampaignPlayableCatalog020 SyntheticSteps130=>_rules;
        internal IWorldGateOperationsCatalog023 SyntheticBoards130=>_rules;
        internal CampaignState CompleteSyntheticChapter130(CampaignState state,string chapterId)=>Complete(state,chapterId);

        [OneTimeSetUp]
        public void EarnSyntheticCompleteCycleThroughEveryAuthority130()
        {
            lock(EarnedFixtureLock130)
            {
                if(_earnedFirstCycle130==null)
                {
                    var earned=BuildCompleteSyntheticCycle130();
                    _earnedFirstHash130=CanonicalJson.Sha256Hex(earned);
                    _earnedFirstCycle130=earned;
                }
                Assert.That(CanonicalJson.Sha256Hex(_earnedFirstCycle130),Is.EqualTo(_earnedFirstHash130),
                    "Only a fully earned immutable in-process fixture may be reused.");
                _completed=_earnedFirstCycle130;
            }
        }

        CampaignState BuildCompleteSyntheticCycle130()
        {
            // Explicit synthetic command-authority fixture: 82 different chapters,
            // each with a briefing, actual two-node World Gate, and results. No
            // completion flags, receipts, ledger hashes, or live saves are planted.
            var source=CampaignFactory.CreateM0Proof(130082);
            var recruit=new RecruitState("REPLAY_RECRUIT130",100,100,20,20);
            var union=new UnionState("REPLAY_UNION130","Replay test Union",UnionKind.Normal,
                recruit.RecruitId,new[]{recruit.RecruitId},"FORMATION_LINE","DOCTRINE_BALANCED",20,8000);
            var guild=new GuildState(source.Guild.GuildId,source.Guild.TreasuryXp,
                new[]{recruit},new[]{union},source.Guild.Inventory,source.Guild.Development,guildCity:null);
            var opening=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",
                true,null,false,439,0,true,true,true,false,"synthetic_replay130");
            var profile=new NewGuildProfileState("Synthetic replay fixture",GameMode.Standard,
                TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);
            var completed=new CampaignState(source.CampaignGuid,source.CampaignSeed,source.ContentAuthorityVersion,
                source.Rules,guild,profile,opening,source.Battle);
            foreach(var id in CampaignReplayRules130.RequiredChapterIds)completed=Complete(completed,id);
            Assert.That(Progress(completed).CompletedChapterIds,Has.Count.EqualTo(82));
            Assert.That(Gate(completed).CompletionProofs,Has.Count.EqualTo(82));
            return completed;
        }

        // Test-only, memory-only reuse across these two fixtures. Publish a cache
        // entry only after all82 real command chains and the cycle transition
        // succeed. No persistent artifact, seeded receipt, or test ordering input.
        internal CampaignState CompleteSecondCycleAndBeginThird130()
        {
            EarnSyntheticCompleteCycleThroughEveryAuthority130();
            lock(EarnedFixtureLock130)
            {
                if(_earnedThirdStart130==null)
                {
                    var second=Require(_cycles.StartNextCycle(_completed,_rules,_rules,1));
                    foreach(var id in CampaignReplayRules130.RequiredChapterIds)second=Complete(second,id);
                    var third=Require(_cycles.StartNextCycle(second,_rules,_rules,2));
                    Assert.That(Progress(third).Replay130.CurrentCycle,Is.EqualTo(3));
                    Assert.That(Gate(third).AuthorityEntries,Has.Count.EqualTo(164));
                    _earnedThirdHash130=CanonicalJson.Sha256Hex(third);
                    _earnedThirdStart130=third;
                }
                Assert.That(CanonicalJson.Sha256Hex(_earnedFirstCycle130),Is.EqualTo(_earnedFirstHash130));
                Assert.That(CanonicalJson.Sha256Hex(_earnedThirdStart130),Is.EqualTo(_earnedThirdHash130));
                return _earnedThirdStart130;
            }
        }

        [Test]
        public void ExactShippingCatalogAndLegacyRoundTripKeepAllOldHashes130()
        {
            var registry=CampaignRegistry019.LoadFromResources();
            var rules=new CampaignRuleCatalogAdapter019(registry.Base018);
            CollectionAssert.AreEquivalent(registry.Chapters.Keys,CampaignReplayRules130.RequiredChapterIds);
            Assert.That(CampaignReplayRules130.ValidateCatalog(rules),Is.True);
            var json=CanonicalJson.Serialize(_completed);
            Assert.That(json,Does.Not.Contain("Replay130"));
            var copy=RoundTrip(_completed);
            Assert.That(CanonicalJson.Serialize(copy),Is.EqualTo(json));
            Assert.That(CanonicalJson.Serialize(Progress(copy).With()),Does.Not.Contain("Replay130"));
            Assert.That(CampaignReplayRules130.CurrentCycle(Progress(copy)),Is.EqualTo(1));
            Assert.That(CampaignReplayRules130.CurrentCompleted(Progress(copy)),Has.Count.EqualTo(82));
        }

        [Test]
        public void NewCyclePreservesLifetimeRewardsWorldHistoryAndRejectsDuplicateStart130()
        {
            var before=CanonicalJson.Serialize(_completed);
            var next=Require(_cycles.StartNextCycle(_completed,_rules,_rules,1,25));
            var progress=Progress(next);
            Assert.That(progress.Replay130.CurrentCycle,Is.EqualTo(2));
            Assert.That(progress.Replay130.CurrentCycleCompletedChapterIds,Is.Empty);
            Assert.That(progress.Replay130.CycleStarts.Single().OperationOrdinalAtStart,
                Is.EqualTo(_completed.Guild.GuildCity.OperationOrdinal));
            Assert.That(CanonicalJson.Serialize(next.Guild.Recruits),Is.EqualTo(CanonicalJson.Serialize(_completed.Guild.Recruits)));
            Assert.That(CanonicalJson.Serialize(next.Guild.Unions),Is.EqualTo(CanonicalJson.Serialize(_completed.Guild.Unions)));
            Assert.That(CanonicalJson.Serialize(next.Guild.Inventory),Is.EqualTo(CanonicalJson.Serialize(_completed.Guild.Inventory)));
            Assert.That(CanonicalJson.Serialize(next.Guild.Development),Is.EqualTo(CanonicalJson.Serialize(_completed.Guild.Development)));
            Assert.That(CanonicalJson.Serialize(progress.Playable020),Is.EqualTo(CanonicalJson.Serialize(Progress(_completed).Playable020)));
            CollectionAssert.AreEqual(progress.CompletedChapterIds,Progress(_completed).CompletedChapterIds);
            CollectionAssert.AreEqual(progress.AppliedReceiptIds,Progress(_completed).AppliedReceiptIds);
            CollectionAssert.AreEqual(progress.UnlockedArcIds,Progress(_completed).UnlockedArcIds);
            Assert.That(next.Guild.TreasuryXp,Is.EqualTo(_completed.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(_completed),Is.EqualTo(before));
            AssertRejected(_cycles.StartNextCycle(next,_rules,_rules,1,25),"CYCLE_ALREADY_CHANGED");
            AssertRejected(_cycles.StartNextCycle(next,_rules,_rules,2,25),"COMPLETE_ALL_82");
            AssertRejected(_cycles.StartNextCycle(next,_rules,_rules,2,50),"GROWTH_SETTING_INVALID");
            RoundTrip(next);
        }

        [Test]
        public void ReplayCannotSkipOldBoardProofAndNewRewardsRemainExactlyOnce130()
        {
            var next=Require(_cycles.StartNextCycle(_completed,_rules,_rules,1));
            var oldProofs=Gate(next).CompletionProofs.ToArray();
            var oldEntries=Gate(next).AuthorityEntries.Select(CanonicalJson.Serialize).ToArray();
            AssertRejected(_chapters.StartChapter(next,_rules,"CH018_002",new[]{"REPLAY_UNION130"},true),"PREVIOUS_CHAPTER_REQUIRED");
            var started=StartAtBoard(next,"CH018_001");
            Assert.That(Progress(started).ActiveOperation.ObjectiveIds,
                Does.Contain("CAMPAIGN_REPLAY130_V1_C2_P25"));
            AssertRejected(_steps.CommitCompletedWorldBoardStep(started,_rules),"WORLD_BOARD");
            AssertRejected(_cycles.StartNextCycle(started,_rules,_rules,2),"FINISH_ACTIVE");
            var claimed=CompleteFromBoard(started,"CH018_001");
            var refreshed=Gate(claimed).CompletionProofs.Single(proof=>proof.DefinitionId=="CH018_001");
            Assert.That(refreshed.OperationId,Is.Not.EqualTo(oldProofs[0].OperationId));
            Assert.That(Gate(claimed).CompletionProofs,Has.Count.EqualTo(82));
            Assert.That(Gate(claimed).AuthorityEntries,Has.Count.EqualTo(oldEntries.Length+1));
            CollectionAssert.AreEqual(oldEntries,Gate(claimed).AuthorityEntries.Take(oldEntries.Length).Select(CanonicalJson.Serialize));
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(claimed,_rules,Gate(claimed)),Is.True);
            Assert.That(Progress(claimed).CompletedChapterIds,Has.Count.EqualTo(82));
            CollectionAssert.AreEqual(new[]{"CH018_001"},Progress(claimed).Replay130.CurrentCycleCompletedChapterIds);
            Assert.That(Progress(claimed).AppliedReceiptIds.Count,Is.EqualTo(Progress(next).AppliedReceiptIds.Count+1));
            Assert.That(claimed.Guild.TreasuryXp,Is.GreaterThan(next.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(claimed.Guild.Recruits),Is.EqualTo(CanonicalJson.Serialize(next.Guild.Recruits)),
                "Replay never regrants lifetime chapter invitations or changes owned heroes.");
            var hash=CanonicalJson.Sha256Hex(claimed);
            Assert.That(_chapters.ApplyReceiptExactlyOnce(claimed,_rules,_rules).IsSuccess,Is.False);
            Assert.That(_boards.FinalizeOperation(claimed,_rules).IsSuccess,Is.False);
            Assert.That(_steps.ApplyStepReceiptExactlyOnce(claimed,_rules).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Sha256Hex(RoundTrip(claimed)),Is.EqualTo(hash));
        }

        [Test, Timeout(600000)]
        public void ASecondFullCycleCanFinishWithoutChangingHistoricalCycleAssignments130()
        {
            var third=CompleteSecondCycleAndBeginThird130();
            var boundary=Progress(third).Replay130.CycleStarts.First();
            Assert.That(Progress(third).Replay130.CurrentCycle,Is.EqualTo(3));
            Assert.That(Progress(third).Replay130.CurrentCycleCompletedChapterIds,Is.Empty);
            Assert.That(Gate(third).CompletionProofs,Has.Count.EqualTo(82));
            Assert.That(Gate(third).AuthorityEntries,Has.Count.EqualTo(164));
            Assert.That(CampaignReplayRules130.CycleForCommittedOrdinal(Progress(third),boundary.OperationOrdinalAtStart),Is.EqualTo(1));
            Assert.That(CampaignReplayRules130.CycleForCommittedOrdinal(Progress(third),boundary.OperationOrdinalAtStart+1),Is.EqualTo(2));
            Assert.That(CampaignReplayRules130.CycleForCommittedOrdinal(Progress(third),third.Guild.GuildCity.OperationOrdinal+1),Is.EqualTo(3));
            RoundTrip(third);
        }

        [Test]
        public void MissingCatalogOrForgedCompletionListCannotAuthorizeCycle130()
        {
            AssertRejected(_cycles.StartNextCycle(_completed,new MissingCatalog130(_rules),_rules,1),"COMPLETE_CATALOG_REQUIRED");
            var next=Require(_cycles.StartNextCycle(_completed,_rules,_rules,1));
            var progress=Progress(next);
            var forged=WithProgress(next,progress.With(replay130:progress.Replay130.WithCompleted(
                CampaignReplayRules130.RequiredChapterIds),replaceReplay130:true));
            AssertRejected(_cycles.StartNextCycle(forged,_rules,_rules,2),"VERIFIED_CHAPTER_REWARDS_REQUIRED");
            Assert.That(CampaignReplayRules130.HasCompleteChapterSet(new[]{"CH018_001"}),Is.False);
            Assert.Throws<ArgumentException>(()=>new CampaignReplayState130(2,25,
                new[]{"CH018_001","CH018_001"},progress.Replay130.CycleStarts));
        }

        [Test]
        public void RealAtomicWriteFailureLeavesOldCycleAndRetryReloadsExactlyOneTransition130()
        {
            var directory=Path.Combine(Path.GetTempPath(),"SecondDimensionReplay130_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"isolated.json");
            var store=new AtomicSaveStore();
            try
            {
                store.Write(path,SaveEnvelopeV1.Create(_completed,DateTime.UtcNow));
                var oldBytes=File.ReadAllBytes(path);
                var candidate=Require(_cycles.StartNextCycle(_completed,_rules,_rules,1));
                Directory.CreateDirectory(path+".tmp");
                var failure=Assert.Catch(()=>store.Write(path,SaveEnvelopeV1.Create(candidate,DateTime.UtcNow)));
                Assert.That(failure,Is.InstanceOf<UnauthorizedAccessException>().Or.InstanceOf<IOException>());
                CollectionAssert.AreEqual(oldBytes,File.ReadAllBytes(path));
                Assert.That(File.Exists(path+".bak"),Is.False);
                Directory.Delete(path+".tmp");
                var retry=Require(_cycles.StartNextCycle(store.ReadWithRecovery(path).Value.CampaignState,_rules,_rules,1));
                Assert.That(CanonicalJson.Sha256Hex(retry),Is.EqualTo(CanonicalJson.Sha256Hex(candidate)));
                store.Write(path,SaveEnvelopeV1.Create(retry,DateTime.UtcNow));
                CollectionAssert.AreEqual(oldBytes,File.ReadAllBytes(path+".bak"));
                var loaded=store.ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess,Is.True,string.Join(";",loaded.Errors));
                Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState),Is.EqualTo(CanonicalJson.Sha256Hex(candidate)));
                var savedBytes=File.ReadAllBytes(path);
                AssertRejected(_cycles.StartNextCycle(loaded.Value.CampaignState,_rules,_rules,1),"CYCLE_ALREADY_CHANGED");
                CollectionAssert.AreEqual(savedBytes,File.ReadAllBytes(path));
            }
            finally{Directory.Delete(directory,true);}
        }

        CampaignState Complete(CampaignState state,string id)=>CompleteFromBoard(StartAtBoard(state,id),id);
        CampaignState StartAtBoard(CampaignState state,string id)
        {
            state=Require(_chapters.StartChapter(state,_rules,id,new[]{"REPLAY_UNION130"},true));
            state=Require(_steps.BeginOperation(state,_rules,id));
            state=Require(_steps.CommitNonBattleStep(state,_rules,"SUCCESS"));
            return Require(_steps.ApplyStepReceiptExactlyOnce(state,_rules));
        }
        CampaignState CompleteFromBoard(CampaignState state,string id)
        {
            state=Require(_boards.BeginOperation(state,_rules,id,new[]{"REPLAY_UNION130"},_rules));
            for(var guard=0;guard<3&&Gate(state).ActiveOperation.Status!=WorldGateOperationStatus023.ReadyToFinalize;guard++)
            {
                state=Require(_boards.CommitNodeChoice(state,_rules,"CONTINUE","REPLAY_RECRUIT130",null,0));
                state=Require(_boards.ApplyNodeReceiptExactlyOnce(state,_rules));
            }
            state=Require(_boards.FinalizeOperation(state,_rules));
            state=Require(_steps.CommitCompletedWorldBoardStep(state,_rules));
            state=Require(_steps.ApplyStepReceiptExactlyOnce(state,_rules));
            state=Require(_steps.CommitNonBattleStep(state,_rules,"SUCCESS"));
            state=Require(_steps.ApplyStepReceiptExactlyOnce(state,_rules));
            state=Require(_chapters.CommitNonCombatReceipt(state,_rules,"SUCCESS"));
            state=Require(_chapters.ApplyReceiptExactlyOnce(state,_rules,_rules));
            return Require(_steps.CloseCompletedOperation(state,_rules));
        }
        static CampaignProgressState019 Progress(CampaignState state)=>state.Guild.GuildCity.Strategic017H.Campaign019;
        static WorldGateRuntimeState023 Gate(CampaignState state)=>Progress(state).Playable020.WorldGate023;
        static CampaignState WithProgress(CampaignState state,CampaignProgressState019 progress)
        {
            var city=state.Guild.GuildCity;
            return state.With(state.Guild.WithGuildCity(city.With(strategic017H:
                city.Strategic017H.With(campaign019:progress,replaceCampaign019:true),replaceStrategic017H:true)),state.OpeningFlow);
        }
        static CampaignState RoundTrip(CampaignState state)
        {
            var copy=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(state));
            Assert.That(CanonicalJson.Sha256Hex(copy),Is.EqualTo(CanonicalJson.Sha256Hex(state)));
            return copy;
        }
        static CampaignState Require(Result<CampaignState> result)
        {Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors));return result.Value;}
        static void AssertRejected(Result<CampaignState> result,string reason)
        {Assert.That(result.IsSuccess,Is.False);Assert.That(string.Join(";",result.Errors),Does.Contain(reason));}

        sealed class MissingCatalog130:ICampaignRuleCatalog019
        {
            readonly ICampaignRuleCatalog019 _source;
            public MissingCatalog130(ICampaignRuleCatalog019 source){_source=source;}
            public bool TryGetChapter(string id,out CampaignChapterRule019 value)
            {if(id=="CH018_082"){value=null;return false;}return _source.TryGetChapter(id,out value);}
            public bool TryGetArc(string id,out CampaignArcRule019 value)=>_source.TryGetArc(id,out value);
            public string LastChapterId(string id)=>_source.LastChapterId(id);
        }
        // Small authored test catalogs retain all 82 shipping IDs. This does not
        // certify shipping combat, pacing, or art; those use their own catalog tests.
        sealed class SyntheticCatalog130:ICampaignRuleCatalog019,ICampaignPlayableCatalog020,IWorldGateOperationsCatalog023
        {
            const string Arc="ARC018_FIRST_GATE_ECHOES";
            readonly Dictionary<string,WorldGateBoardRule023> _boards=new Dictionary<string,WorldGateBoardRule023>();
            public SyntheticCatalog130()
            {
                foreach(var id in CampaignReplayRules130.RequiredChapterIds)
                {
                    var start="SYNTHETIC130_"+id+"_START";var end="SYNTHETIC130_"+id+"_EXIT";
                    _boards[id]=new WorldGateBoardRule023{DefinitionId=id,BoardId="SYNTHETIC130_"+id,
                        OperationKind="CHAPTER",WorldId="SKYHOME",Title="Synthetic replay authority fixture",
                        StartNodeId=start,ExitNodeId=end,MaximumAlliedUnions=1,MaximumEnemyUnions=1,
                        Nodes=new[]{new WorldGateNodeRule023{NodeId=start,Title="Synthetic start",Kind="START",
                            NextNodeIds=new[]{end},ChoiceIds=new[]{"CONTINUE"},GuildXp=1,HallXp=1},
                            new WorldGateNodeRule023{NodeId=end,Title="Synthetic exit",Kind="EXIT",
                                ChoiceIds=new[]{"CONTINUE"},GuildXp=1,HallXp=1}}};
                }
            }
            public bool TryGetChapter(string id,out CampaignChapterRule019 value)
            {
                value=CampaignReplayRules130.IsChapterId(id)?new CampaignChapterRule019{
                    ChapterId=id,ArcId=Arc,WorldId="SKYHOME",PrimaryObjective="Complete the synthetic route",
                    GuildXp=1,HallXp=1,MapIds=new[]{"SYNTHETIC130_MAP"},BattleRequired=false}:null;
                return value!=null;
            }
            public bool TryGetArc(string id,out CampaignArcRule019 value)
            {value=id==Arc?new CampaignArcRule019{ArcId=Arc,WorldId="SKYHOME",
                ChapterIds=CampaignReplayRules130.RequiredChapterIds.ToArray(),UnlockGates=Array.Empty<string>()}:null;return value!=null;}
            public string LastChapterId(string id)=>id==Arc?"CH018_082":string.Empty;
            public bool TryGetBlueprint(string id,out CampaignBlueprintRule020 value)
            {
                value=CampaignReplayRules130.IsChapterId(id)?new CampaignBlueprintRule020{
                    ChapterId=id,BlueprintId="SYNTHETIC130_"+id,ArcId=Arc,WorldId="SKYHOME",
                    MaximumAlliedUnions=1,MaximumEnemyUnions=1,
                    Steps=new[]{new CampaignStepRule020{StepId=id+"_BRIEF",Kind="BRIEFING",Title="Synthetic brief"},
                        new CampaignStepRule020{StepId=id+"_BOARD",Kind="WORLD_BOARD",Title="Synthetic board"},
                        new CampaignStepRule020{StepId=id+"_RESULT",Kind="RESULTS",Title="Synthetic result"}}}:null;
                return value!=null;
            }
            public IReadOnlyList<string> RepeatableContractsForWorld(string id)=>Array.Empty<string>();
            public bool IsKnownMaterial(string id)=>false;
            public IWorldGateOperationsCatalog023 WorldGateProofCatalog023=>this;
            public bool TryGetBoard(string id,out WorldGateBoardRule023 value)=>_boards.TryGetValue(id,out value);
            public bool TryGetTravel(string id,out WorldTravelRule023 value)
            {value=id=="SKYHOME"?new WorldTravelRule023{TravelId="SYNTHETIC130_HOME",WorldId=id,DisplayName="Skyhome"}:null;return value!=null;}
            public bool TryGetStanding(string id,out WorldStandingRule023 value){value=null;return false;}
            public bool TryGetRecruitUnlock(string id,out RecruitUnlockRule023 value){value=null;return false;}
            public WorldGateRewardPolicy023 RewardPolicy{get;}=new WorldGateRewardPolicy023{
                RepeatableConsecutiveRewardBasisPoints=new[]{10000,7000,4000,0},
                ResetOnDifferentCompletedOperation=true,TravelSupplyReserveDefault=30,TravelSupplyRewardPerCompletedOperation=1};
            public IReadOnlyList<WorldGateBoardRule023> AllBoards=>_boards.Values.ToArray();
        }
    }

    public sealed class CampaignReplayLegacy130Tests
    {
        [Test]
        public void OriginalAffected1668EnvelopeRemainsCanonicalWithoutReplayDefaults130()
        {
            var path=Environment.GetEnvironmentVariable("SD_CAMPAIGN130_LEGACY_SOURCE");
            if(string.IsNullOrWhiteSpace(path))path=@"C:\Users\simon\Documents\ChatGPT\second dimension\SaveBackups\20260912_083507_reset_session_baseline\Profile\second_dimension_first_hour_slice_071.json";
            if(!File.Exists(path))Assert.Ignore("Use the preserved original1668 backup via SD_CAMPAIGN130_LEGACY_SOURCE.");
            var original=File.ReadAllBytes(path);
            using(var sha=SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(original)).Replace("-",""),
                    Is.EqualTo("1668BC89991E6A41D2BCEFD66084753715E7C5311B22A4BA534E680AF86F3A75"));
            var loaded=new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(loaded.IsSuccess,Is.True,string.Join(";",loaded.Errors));
            Assert.That(loaded.Value.CanonicalStateHash,
                Is.EqualTo("cf0c36d96d6f2884c9047c0b908eed2639c0faaae85f54d0df3164dd6ecec0d7"));
            Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState),Is.EqualTo(loaded.Value.CanonicalStateHash));
            Assert.That(loaded.Value.CampaignState.Guild.GuildCity.Strategic017H.Campaign019.Replay130,Is.Null);
            Assert.That(CanonicalJson.Serialize(loaded.Value.CampaignState),Does.Not.Contain("Replay130"));
            CollectionAssert.AreEqual(original,File.ReadAllBytes(path));
        }

        [Test]
        public void FreshCoordinatorCanProjectCycleOneWithoutCreatingSave130()
        {
            var directory=Path.Combine(Path.GetTempPath(),"SecondDimensionReplay130_Fresh_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path=Path.Combine(directory,"uncreated.json");
            try
            {
                var owner=new M1RuntimeCoordinator(Path.Combine(UnityEngine.Application.streamingAssetsPath,"Authority","CONTENT"),path);
                var state=owner.Campaign019;
                Assert.That(state.IsAvailable,Is.True,state.Error);
                Assert.That(state.CurrentCycle130,Is.EqualTo(1));
                Assert.That(state.CycleCompleted130,Is.Zero);
                Assert.That(state.CanStartNextCycle130,Is.False);
                Assert.That(state.Chapters,Has.Count.EqualTo(82));
                Assert.That(File.Exists(path),Is.False,"A presentation read never starts or saves a cycle.");
            }
            finally{Directory.Delete(directory,true);}
        }
    }
}
#endif
