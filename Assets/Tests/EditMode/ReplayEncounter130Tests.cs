#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ReplayEncounter130Tests
    {
        const string Chapter="CH018_001";
        const string Union="REPLAY_UNION130";
        const string Tag="CAMPAIGN_REPLAY130_V1_C2_P25";
        readonly CampaignReplay130Tests _fixture=new CampaignReplay130Tests();
        readonly CampaignCommandService019 _chapters=new CampaignCommandService019();
        readonly CampaignPlayableCommandService020 _steps=new CampaignPlayableCommandService020();
        readonly CampaignWorldGateCommandService023 _boards=new CampaignWorldGateCommandService023();
        readonly CampaignReplayCommandService130 _cycles=new CampaignReplayCommandService130();
        readonly GuildCityBattleBridgeService017D _bridge=new GuildCityBattleBridgeService017D();
        readonly M2BattleCommandService _battles=new M2BattleCommandService();
        CampaignState _cycleOneComplete,_cycleTwoReady;
        M2CombatContent _combat;
        BattleChapterCatalog130 _battleChapters;

        [OneTimeSetUp]
        public void EarnSyntheticCycleAndLoadActualCombatAuthority130()
        {
            // Same bounded synthetic82 authority fixture as the base tests. Only
            // this test's CH001 wrapper requests certified combat; no live source,
            // planted completion flag, simulated result, or shipping82 playthrough.
            _fixture.EarnSyntheticCompleteCycleThroughEveryAuthority130();
            _cycleOneComplete=_fixture.CompletedSyntheticCycle130;
            _cycleTwoReady=Require(_cycles.StartNextCycle(_cycleOneComplete,
                _fixture.SyntheticChapters130,_fixture.SyntheticBoards130,1,25));
            _battleChapters=new BattleChapterCatalog130(_fixture.SyntheticChapters130);
            _combat=M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"));
        }

        [Test]
        public void Certified019CommittedAndReloadedReplayRequestActuallyScalesM2EnemiesOnly130()
        {
            var originalHash=CanonicalJson.Sha256Hex(_cycleTwoReady);
            var oldHistory=CanonicalJson.Serialize(Gate(_cycleTwoReady).AuthorityEntries);
            var fresh=FreshCycleOne130();
            var legacyCommitted=Commit019(fresh);
            var legacyRequest=legacyCommitted.Guild.GuildCity.PendingEncounter;
            Assert.That(_battleChapters.TryGetChapter(Chapter,out var chapterRule),Is.True);
            Assert.That(legacyRequest.BattleId,Is.EqualTo(chapterRule.BattleId),"Cycle1 preserves the exact legacy ID.");
            Assert.That(CampaignReplayThreat130.ParseCommittedRoutes(legacyRequest.RouteModifiers),Is.Null);
            Assert.That(legacyRequest.ObjectiveIds.Any(value=>value.StartsWith("CAMPAIGN_REPLAY130",StringComparison.Ordinal)),Is.False);
            var baseline=Require(_bridge.StartCertifiedEncounter(legacyCommitted,_battles,_combat));

            var committed=Commit019(_cycleTwoReady);
            var request=committed.Guild.GuildCity.PendingEncounter;
            var operation=Progress(committed).ActiveOperation;
            Assert.That(request.BattleId,Is.EqualTo(chapterRule.BattleId+"_REPLAY130_"+
                CanonicalJson.Sha256Hex(new{operation.RequestId}).Substring(0,24).ToUpperInvariant()));
            Assert.That(request.BattleId,Is.Not.EqualTo(legacyRequest.BattleId));
            Assert.That(request.RequestId,Is.Not.EqualTo(legacyRequest.RequestId));
            Assert.That(request.ObjectiveIds,Does.Contain(Tag));
            Assert.That(request.RouteModifiers.Count(value=>value==Tag),Is.EqualTo(1));
            Assert.That(committed.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)),Is.True);
            var reloaded=RoundTrip(committed);
            Assert.That(CanonicalJson.Serialize(reloaded.Guild.GuildCity.PendingEncounter),Is.EqualTo(CanonicalJson.Serialize(request)));
            Assert.That(CanonicalJson.Sha256Hex(Require(_chapters.CommitCertifiedBattle(reloaded,_battleChapters))),
                Is.EqualTo(CanonicalJson.Sha256Hex(reloaded)),"Reconstructing the already committed019 request must be idempotent.");
            var active=Require(_bridge.StartCertifiedEncounter(reloaded,_battles,_combat));
            Assert.That(active.Battle.Phase,Is.Not.EqualTo(BattlePhase.Resolved));
            Assert.That(active.Battle.Outcome,Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(active.Battle.BattleId,Is.EqualTo(request.BattleId));
            CollectionAssert.AreEqual(baseline.Battle.EnemyUnions.Select(value=>value.UnionId),
                active.Battle.EnemyUnions.Select(value=>value.UnionId));
            for(var index=0;index<active.Battle.EnemyUnions.Count;index++)
            {
                var before=baseline.Battle.EnemyUnions[index];var after=active.Battle.EnemyUnions[index];
                Assert.That(after.CurrentAp,Is.EqualTo(before.CurrentAp));
                Assert.That(after.MaximumAp,Is.EqualTo(before.MaximumAp));
                Assert.That(after.FormationId,Is.EqualTo(before.FormationId));
                Assert.That(after.Cohesion,Is.EqualTo(before.Cohesion));
                CollectionAssert.AreEqual(before.Members.Select(value=>value.MemberId),after.Members.Select(value=>value.MemberId));
                for(var memberIndex=0;memberIndex<before.Members.Count;memberIndex++)
                {
                    var a=before.Members[memberIndex];var b=after.Members[memberIndex];
                    Assert.That(a.MaximumHp,Is.LessThan(CampaignReplayThreat130.MaximumMemberHp130));
                    Assert.That(b.MaximumHp,Is.EqualTo((a.MaximumHp*125+99)/100));
                    Assert.That(b.CurrentHp,Is.EqualTo(b.MaximumHp));
                    Assert.That(b.Attack,Is.EqualTo((a.Attack*125+99)/100));
                    Assert.That(b.MagicAttack,Is.EqualTo((a.MagicAttack*125+99)/100));
                    Assert.That(b.CurrentMp,Is.EqualTo(a.CurrentMp));
                    Assert.That(b.MaximumMp,Is.EqualTo(a.MaximumMp));
                    CollectionAssert.AreEqual(a.LearnedArtIds,b.LearnedArtIds);
                    CollectionAssert.AreEqual(a.EquipmentTags,b.EquipmentTags);
                }
            }
            Assert.That(CanonicalJson.Serialize(active.Battle.PlayerUnions),Is.EqualTo(CanonicalJson.Serialize(baseline.Battle.PlayerUnions)),
                "Both genuine battles use the same unchanged owned test heroes; replay applies only to enemies.");
            Assert.That(CanonicalJson.Serialize(active.Guild.Recruits),Is.EqualTo(CanonicalJson.Serialize(_cycleTwoReady.Guild.Recruits)));
            Assert.That(CanonicalJson.Serialize(active.Guild.Inventory),Is.EqualTo(CanonicalJson.Serialize(_cycleTwoReady.Guild.Inventory)));
            Assert.That(CanonicalJson.Serialize(Gate(active).AuthorityEntries),Is.EqualTo(oldHistory));
            Assert.That(CanonicalJson.Sha256Hex(_cycleTwoReady),Is.EqualTo(originalHash));
            RoundTrip(active);

            // A changed saved request is a negative fixture, never an accepted
            // battle. The original committed authority must reject stripping130.
            var stripped=CopyRequestWithoutReplay130(request);
            var tampered=committed.With(committed.Guild.WithGuildCity(committed.Guild.GuildCity.With(
                pendingEncounter:stripped,replacePendingEncounter:true)),committed.OpeningFlow);
            var tamperedHash=CanonicalJson.Sha256Hex(tampered);
            var rejected=_bridge.StartCertifiedEncounter(tampered,_battles,_combat);
            Assert.That(rejected.IsSuccess,Is.False);
            Assert.That(string.Join(";",rejected.Errors),Does.Contain("AUTHORITY"));
            Assert.That(CanonicalJson.Sha256Hex(tampered),Is.EqualTo(tamperedHash));
        }

        [Test, Timeout(600000)]
        public void RequestFactories023And089KeepSavedOrdinalPolicyAfterLaterCycle130()
        {
            var committed019=Commit019(_cycleTwoReady);
            var request019=committed019.Guild.GuildCity.PendingEncounter;
            var operation019=Progress(committed019).ActiveOperation;
            Assert.That(_battleChapters.TryGetChapter(Chapter,out var chapter019),Is.True);
            var boardContext=StartActualSyntheticBoard130(_cycleTwoReady);
            var city=boardContext.Guild.GuildCity;
            var operation=Gate(boardContext).ActiveOperation;
            Assert.That(CampaignWorldGateCommandService023.ValidateActiveAuthority093(boardContext,
                _fixture.SyntheticBoards130,out var error),Is.True,error);
            Assert.That(CampaignReplayRules130.CycleForCommittedOrdinal(Progress(boardContext),
                city.ActiveContract.AcceptedOperationOrdinal),Is.EqualTo(2));
            var beforeHash=CanonicalJson.Sha256Hex(boardContext);

            // These two factory checks deliberately supply an encounter node and
            // a receipt-shaped test input to an already authentic active board.
            // They verify route propagation/reconstruction ONLY: no receipt is
            // recorded, no node/card outcome is authorized, and no battle starts.
            var node=new WorldGateNodeRule023{NodeId=operation.CurrentNodeId,Kind="BATTLE",
                Title="Synthetic factory probe",RequiresCertifiedBattle=true,EnemyUnionCount=1,
                Objective="Factory propagation only",SourceId="SYNTHETIC_REPLAY130_FACTORY"};
            var request023=Factory023(boardContext,city,operation,node,beforeHash);
            var offered=operation.ExpeditionDeck089.CurrentRow.First();
            // Explicit factory-only battle input: the offered START row has no
            // encounter. This detached probe is never committed to the board.
            var card=new ExpeditionRouteCardState089(offered.CardId,offered.NodeId,offered.ChoiceId,
                "BATTLE","BATTLE","Synthetic factory probe",string.Empty,string.Empty,string.Empty,
                string.Empty,0,0,0,0,Array.Empty<string>(),0,null,null,null,"SYNTHETIC_FACTORY_ONLY",
                advancesRoute:false,encounterId:"ENCOUNTER_RELIEF_ROAD",enemyUnionCount:1);
            var receipt=new ExpeditionCardReceipt089("SYNTHETIC_REPLAY130_RECEIPT",operation.OperationId,
                card.CardId,card.NodeId,card.ChoiceId,"REPLAY_RECRUIT130",null,0,0,0,0,0,"BATTLE_READY",0,0,
                Array.Empty<string>(),0,null,"SYNTHETIC_FACTORY_INPUT_ONLY",false,
                requiresCertifiedBattle:true,battleRequestId:"SYNTHETIC_REPLAY130_REQUEST",
                battleId:"SYNTHETIC_REPLAY130_OPTIONAL_BATTLE",battlePreStateHash:beforeHash,
                battleReturnCheckpointId:"SYNTHETIC_REPLAY130_RETURN");
            var request089=Factory089(boardContext,operation,card,receipt);
            Assert.That(request023.RouteModifiers.Count(value=>value==Tag),Is.EqualTo(1));
            Assert.That(request089.RouteModifiers.Count(value=>value==Tag),Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(boardContext),Is.EqualTo(beforeHash));

            // Earn cycle3 on a separate immutable branch through every command.
            // The captured cycle2 branch and its request inputs remain untouched.
            var third=_fixture.CompleteSecondCycleAndBeginThird130();
            Assert.That(Progress(third).Replay130.CurrentCycle,Is.EqualTo(3));
            Assert.That(CampaignReplayRules130.CycleForCommittedOrdinal(Progress(third),
                city.ActiveContract.AcceptedOperationOrdinal),Is.EqualTo(2));
            Assert.That(CanonicalJson.Serialize(Factory023(third,city,operation,node,beforeHash)),
                Is.EqualTo(CanonicalJson.Serialize(request023)));
            var rebuilt019=(EncounterLaunchRequest017D)typeof(CampaignCommandService019)
                .GetMethod("CreateCertifiedEncounterRequest019",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{third,operation019,chapter019,false});
            Assert.That(CanonicalJson.Serialize(rebuilt019),Is.EqualTo(CanonicalJson.Serialize(request019)),
                "Certified019 reconstruction uses the captured operation objective tag, never today's cycle3.");

            // The optional factory reads its city from the campaign. This local
            // detached reconstruction context contains the captured old contract
            // and new immutable boundary history; it is NOT dispatched to any
            // command or presented as a valid active campaign save.
            var archivedProgress=Progress(boardContext).With(replay130:Progress(third).Replay130,replaceReplay130:true);
            var archivedCity=city.With(strategic017H:city.Strategic017H.With(
                campaign019:archivedProgress,replaceCampaign019:true),replaceStrategic017H:true);
            var archivedContext=boardContext.With(boardContext.Guild.WithGuildCity(archivedCity),boardContext.OpeningFlow);
            Assert.That(CanonicalJson.Serialize(Factory089(archivedContext,operation,card,receipt)),
                Is.EqualTo(CanonicalJson.Serialize(request089)));
            Assert.That(CanonicalJson.Sha256Hex(boardContext),Is.EqualTo(beforeHash));
            Assert.That(CanonicalJson.Serialize(Gate(third).AuthorityEntries.Take(Gate(_cycleOneComplete).AuthorityEntries.Count)),
                Is.EqualTo(CanonicalJson.Serialize(Gate(_cycleOneComplete).AuthorityEntries)));
        }

        CampaignState Commit019(CampaignState source)
        {
            var started=Require(_chapters.StartChapter(source,_battleChapters,Chapter,new[]{Union},true));
            return Require(_chapters.CommitCertifiedBattle(started,_battleChapters));
        }
        CampaignState FreshCycleOne130()
        {
            var source=CampaignFactory.CreateM0Proof(130082);
            var guild=new GuildState(source.Guild.GuildId,source.Guild.TreasuryXp,
                _cycleOneComplete.Guild.Recruits,_cycleOneComplete.Guild.Unions,
                source.Guild.Inventory,source.Guild.Development,guildCity:null);
            return new CampaignState(source.CampaignGuid,source.CampaignSeed,source.ContentAuthorityVersion,
                source.Rules,guild,_cycleOneComplete.Profile,_cycleOneComplete.OpeningFlow,source.Battle);
        }
        CampaignState StartActualSyntheticBoard130(CampaignState source)
        {
            var campaign=Require(_chapters.StartChapter(source,_fixture.SyntheticChapters130,Chapter,new[]{Union},true));
            campaign=Require(_steps.BeginOperation(campaign,_fixture.SyntheticSteps130,Chapter));
            campaign=Require(_steps.CommitNonBattleStep(campaign,_fixture.SyntheticSteps130,"SUCCESS"));
            campaign=Require(_steps.ApplyStepReceiptExactlyOnce(campaign,_fixture.SyntheticSteps130));
            return Require(_boards.BeginOperation(campaign,_fixture.SyntheticBoards130,Chapter,new[]{Union},_fixture.SyntheticSteps130));
        }
        static CampaignProgressState019 Progress(CampaignState state)=>state.Guild.GuildCity.Strategic017H.Campaign019;
        static WorldGateRuntimeState023 Gate(CampaignState state)=>Progress(state).Playable020.WorldGate023;
        static CampaignState Require(Result<CampaignState> result)
        {Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors));return result.Value;}
        static CampaignState RoundTrip(CampaignState state)
        {
            var result=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(state));
            Assert.That(CanonicalJson.Sha256Hex(result),Is.EqualTo(CanonicalJson.Sha256Hex(state)));
            return result;
        }
        static EncounterLaunchRequest017D Factory023(CampaignState state,GuildCityState017D city,
            WorldGateOperationState023 operation,WorldGateNodeRule023 node,string preHash)=>
            (EncounterLaunchRequest017D)typeof(CampaignWorldGateCommandService023).GetMethod("CreateEncounterRequest084",
                BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{state,city,operation,node,preHash,true});
        static EncounterLaunchRequest017D Factory089(CampaignState state,WorldGateOperationState023 operation,
            ExpeditionRouteCardState089 card,ExpeditionCardReceipt089 receipt)=>
            (EncounterLaunchRequest017D)typeof(ExpeditionDeckCommandService089).GetMethod("CreateOptionalBattleRequest089",
                BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{state,operation,card,receipt,true});
        static EncounterLaunchRequest017D CopyRequestWithoutReplay130(EncounterLaunchRequest017D request)=>
            new EncounterLaunchRequest017D(request.RequestId,request.ContractId,request.ExpeditionId,
                request.BoardId,request.NodeId,request.EncounterId,request.BattleId,request.Objective,request.EnemyUnionCount,
                request.CanonicalSeedIdentity,request.AlliedUnionIds,request.ReserveUnionIds,request.ObjectiveIds,
                request.RouteModifiers.Where(value=>value!=Tag).ToArray(),request.Supplies,request.Fatigue,request.Urgency,
                request.ReturnCheckpointId,request.PreBattleStateHash);
        sealed class BattleChapterCatalog130:ICampaignRuleCatalog019
        {
            readonly ICampaignRuleCatalog019 _source;
            public BattleChapterCatalog130(ICampaignRuleCatalog019 source){_source=source;}
            public bool TryGetChapter(string id,out CampaignChapterRule019 chapter)
            {
                if(!_source.TryGetChapter(id,out chapter))return false;
                if(id==Chapter)chapter=new CampaignChapterRule019{ChapterId=id,ArcId=chapter.ArcId,WorldId=chapter.WorldId,
                    MapIds=chapter.MapIds,SiegeId=chapter.SiegeId,PrimaryObjective=chapter.PrimaryObjective,
                    GuildXp=chapter.GuildXp,HallXp=chapter.HallXp,Materials=chapter.Materials,StoryGates=chapter.StoryGates,
                    BattleRequired=true,EnemyUnionCount=1,BattleId="SYNTHETIC_REPLAY130_CERTIFIED_CH018_001"};
                return true;
            }
            public bool TryGetArc(string id,out CampaignArcRule019 arc)=>_source.TryGetArc(id,out arc);
            public string LastChapterId(string id)=>_source.LastChapterId(id);
        }
    }
}
#endif
