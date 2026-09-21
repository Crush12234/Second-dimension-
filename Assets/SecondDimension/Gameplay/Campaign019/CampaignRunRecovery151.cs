using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign019
{
    [Serializable]
    public sealed class CampaignDefeatEvidence151
    {
        [JsonConstructor]
        public CampaignDefeatEvidence151(string attemptId,string rewardId,string returnAuthorityId,string finalHash)
        {
            if(string.IsNullOrWhiteSpace(attemptId)||string.IsNullOrWhiteSpace(rewardId)||
                string.IsNullOrWhiteSpace(returnAuthorityId)||finalHash==null||finalHash.Length!=64)
                throw new ArgumentException("A settled defeat needs its native receipt references.");
            AttemptId=attemptId;RewardId=rewardId;ReturnAuthorityId=returnAuthorityId;FinalHash=finalHash;
        }
        public string AttemptId{get;} public string RewardId{get;}
        public string ReturnAuthorityId{get;} public string FinalHash{get;}
    }

    [Serializable]
    public sealed class CampaignRunBoundary151
    {
        [JsonConstructor]
        public CampaignRunBoundary151(int cycle,int operationOrdinal,string beforeHash,string retiredOperationId,
            string retiredCommitmentHash,string requestId)
        {
            if(cycle<1||operationOrdinal<1||beforeHash==null||beforeHash.Length!=64||
                string.IsNullOrWhiteSpace(retiredOperationId)||retiredCommitmentHash==null||retiredCommitmentHash.Length!=64||string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("Invalid Campaign recovery boundary.");
            Cycle=cycle;OperationOrdinal=operationOrdinal;BeforeHash=beforeHash;
            RetiredOperationId=retiredOperationId;RetiredCommitmentHash=retiredCommitmentHash;RequestId=requestId;
        }
        public int Cycle{get;} public int OperationOrdinal{get;} public string BeforeHash{get;}
        public string RetiredOperationId{get;} public string RetiredCommitmentHash{get;} public string RequestId{get;}
    }

    [Serializable]
    public sealed class CampaignRunRecovery151
    {
        [JsonConstructor]
        public CampaignRunRecovery151(int cycle,string questKey,IReadOnlyList<CampaignDefeatEvidence151> defeats,
            IReadOnlyList<string> completedChapterIds,IReadOnlyList<CampaignRunBoundary151> boundaries)
        {
            if(cycle<1)throw new ArgumentOutOfRangeException(nameof(cycle));
            var failures=(defeats??Array.Empty<CampaignDefeatEvidence151>()).ToArray();
            var history=(boundaries??Array.Empty<CampaignRunBoundary151>()).ToArray();
            var completed=(completedChapterIds??Array.Empty<string>()).ToArray();
            if(failures.Length>10||failures.Any(f=>f==null)||failures.Select(f=>f.AttemptId).Distinct().Count()!=failures.Length||
                failures.Select(f=>f.RewardId).Distinct().Count()!=failures.Length||history.Length>4096||
                history.Any(h=>h==null)||history.Select(h=>h.RequestId).Distinct().Count()!=history.Length||
                completed.Any(id=>!CampaignReplayRules130.IsChapterId(id))||completed.Distinct().Count()!=completed.Length)
                throw new ArgumentException("Invalid Campaign recovery history.");
            for(int i=1;i<history.Length;i++)
                if(history[i].OperationOrdinal<=history[i-1].OperationOrdinal||history[i].Cycle<history[i-1].Cycle)
                    throw new ArgumentException("Campaign recovery boundaries must advance.");
            Cycle=cycle;QuestKey=questKey??string.Empty;Defeats=Array.AsReadOnly(failures);
            CompletedChapterIds=Array.AsReadOnly(completed);Boundaries=Array.AsReadOnly(history);
        }
        public int Cycle{get;} public string QuestKey{get;}
        public IReadOnlyList<CampaignDefeatEvidence151> Defeats{get;}
        public IReadOnlyList<string> CompletedChapterIds{get;}
        public IReadOnlyList<CampaignRunBoundary151> Boundaries{get;}
        [JsonIgnore] public bool HasCurrentRun=>Boundaries.Count>0&&Boundaries[Boundaries.Count-1].Cycle==Cycle;
        [JsonIgnore] public int StartOrdinal=>HasCurrentRun?Boundaries[Boundaries.Count-1].OperationOrdinal:-1;
        public CampaignRunRecovery151 Complete(string chapterId)
        {
            var completed=CompletedChapterIds.ToList();if(!completed.Contains(chapterId))completed.Add(chapterId);
            // Both internal finale parts belong to displayed Mission81.
            var reset=chapterId!=CampaignMissionMap132.FinaleOpeningChapterId;
            return new CampaignRunRecovery151(Cycle,reset?string.Empty:QuestKey,
                reset?Array.Empty<CampaignDefeatEvidence151>():Defeats,completed,Boundaries);
        }
    }

    public static class CampaignRunRecoveryCommands151
    {
        public static string QuestKey(string chapterId)=>CampaignMissionMap132.Number(chapterId).ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static CampaignRunRecovery151 ActiveRun(CampaignProgressState019 p)=>
            p?.RunRecovery151?.Cycle==CampaignReplayRules130.CurrentCycle(p)?p.RunRecovery151:null;
        public static CampaignRunRecovery151 AfterCompletion(CampaignProgressState019 p,string chapterId)
        {
            if(p.RunRecovery151==null)return null;
            var run=ActiveRun(p)??new CampaignRunRecovery151(CampaignReplayRules130.CurrentCycle(p),string.Empty,
                Array.Empty<CampaignDefeatEvidence151>(),CampaignReplayRules130.CurrentCompleted(p),p.RunRecovery151.Boundaries);
            return run.Complete(chapterId);
        }
        public static string BoundaryId(CampaignState s,int cycle,int ordinal,string revision,string retired)=>
            "CAMPAIGN_RUN151_"+CanonicalJson.Sha256Hex(new{s.CampaignGuid,ExpectedRevision=revision,Cycle=cycle,Ordinal=ordinal,Retired=retired}).Substring(0,24).ToUpperInvariant();
        public static bool ValidateHistory(CampaignState s)
        {
            var p=s?.Guild?.GuildCity?.Strategic017H?.Campaign019;var run=p?.RunRecovery151;
            return run==null||run.Cycle<=CampaignReplayRules130.CurrentCycle(p)&&run.Boundaries.All(b=>
                b.Cycle<=run.Cycle&&b.OperationOrdinal<=s.Guild.GuildCity.OperationOrdinal&&
                b.RequestId==BoundaryId(s,b.Cycle,b.OperationOrdinal,b.BeforeHash,b.RetiredCommitmentHash));
        }
        public static bool IsRestartReady(CampaignState s)
        {
            var p=s?.Guild?.GuildCity?.Strategic017H?.Campaign019;var run=ActiveRun(p);
            return run?.HasCurrentRun==true&&string.IsNullOrWhiteSpace(p.ActiveChapterId)&&
                p.ActiveOperation==null&&p.Playable020.ActiveOperation==null&&
                run.CompletedChapterIds.Count==0&&s.Guild.GuildCity.OperationOrdinal==run.StartOrdinal;
        }
        static string CurrentQuest(CampaignProgressState019 p)=>!string.IsNullOrWhiteSpace(p.ActiveChapterId)?p.ActiveChapterId:
            CampaignReplayRules130.RequiredChapterIds.FirstOrDefault(id=>!CampaignReplayRules130.CurrentCompleted(p).Contains(id));
        const string ChapterReceiptPrefix="CHAPTER_RECEIPT151:";
        static bool HasSettlement(CampaignState s,CampaignDefeatEvidence151 d)=>
            s.Guild.Development.HasClaimedReward(d.RewardId)&&
            (d.ReturnAuthorityId.StartsWith(ChapterReceiptPrefix,StringComparison.Ordinal)?
                s.Guild.Development.HasClaimedReward(d.ReturnAuthorityId.Substring(ChapterReceiptPrefix.Length)):
                s.Guild.Development.HasAdventureAuthority(d.ReturnAuthorityId));
        public static bool IsEligible(CampaignState state)
        {
            var p=state?.Guild?.GuildCity?.Strategic017H?.Campaign019;var run=ActiveRun(p);
            return run!=null&&run.Defeats.Count==10&&CurrentQuest(p)!=null&&
                run.QuestKey==QuestKey(CurrentQuest(p))&&run.Defeats.All(d=>HasSettlement(state,d));
        }

        // Called inside the existing complete reward/return candidate, before its one atomic save.
        public static Result<CampaignState> ObserveSettlement(CampaignState before,CampaignState after)
        {
            var p=before?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            var battle=before?.Battle;var request=before?.Guild?.GuildCity?.PendingEncounter;
            if(p==null||battle==null||battle.Outcome!=BattleOutcome.Defeat||battle.Reward==null||battle.Reward.Claimed||
                string.IsNullOrWhiteSpace(p.ActiveChapterId)||p.Playable020.ActiveOperation==null||before.Recovery150?.Paused==true||
                p.Playable020.Progression022.ActiveAbyssOperation!=null||request==null)
                return Result<CampaignState>.Success(after);
            var q=after.Guild.GuildCity.Strategic017H.Campaign019;
            if(q.ActiveChapterId!=p.ActiveChapterId||CampaignReplayRules130.CurrentCompleted(q).Contains(p.ActiveChapterId))
                return Result<CampaignState>.Success(after);
            if(before.CampaignGuid!=after.CampaignGuid||after.Battle?.Reward?.Claimed!=true||
                after.Battle.Reward.RewardId!=battle.Reward.RewardId||!after.Guild.Development.HasClaimedReward(battle.Reward.RewardId)||
                !M2BattleCommandService.HasValidFinalStateHash090(battle)||after.Guild.GuildCity.PendingEncounter!=null||after.Guild.GuildCity.PendingBattleReturn!=null)
                return Result<CampaignState>.Failure("CAMPAIGN151_DEFEAT_SETTLEMENT_NOT_VERIFIED");
            string authority;
            if(p.Playable020.WorldGate023.ActiveOperation==null)
            {
                var chapterReceipt=q.PendingReceipt;
                if(chapterReceipt==null||chapterReceipt.RequestId!=p.ActiveOperation?.RequestId||
                    chapterReceipt.ChapterId!=p.ActiveChapterId||chapterReceipt.AuthoritativeResultHash!=battle.FinalStateHash||
                    chapterReceipt.ExistingEquipmentRewardReceiptId!=battle.Reward.RewardId)
                    return Result<CampaignState>.Failure("CAMPAIGN151_CHAPTER_RETURN_NOT_VERIFIED");
                // Eligibility waits for this already-committed chapter receipt to settle normally.
                authority=ChapterReceiptPrefix+chapterReceipt.ReceiptId;
            }
            else
            {
                if(!GuildCityBattleBridgeService017D.TryCreateCanonicalBattleReturn084(battle,request,before.Guild.GuildCity.OperationOrdinal,out var receipt,out _))
                    return Result<CampaignState>.Failure("CAMPAIGN151_DEFEAT_RETURN_NOT_VERIFIED");
                authority=GuildCityBattleBridgeService017D.BattleReturnApplyAuthorityId084(receipt);
                if(!after.Guild.Development.HasAdventureAuthority(authority)||!after.Guild.GuildCity.AppliedBattleReturnIds.Contains(receipt.ReceiptId))
                    return Result<CampaignState>.Failure("CAMPAIGN151_DEFEAT_RETURN_NOT_VERIFIED");
            }
            var run=ActiveRun(q);var key=QuestKey(p.ActiveChapterId);
            var failures=run?.QuestKey==key?run.Defeats.ToList():new List<CampaignDefeatEvidence151>();
            if(failures.Any(d=>d.AttemptId==request.RequestId||d.RewardId==battle.Reward.RewardId)||failures.Count==10)
                return Result<CampaignState>.Success(after);
            failures.Add(new CampaignDefeatEvidence151(request.RequestId,battle.Reward.RewardId,authority,battle.FinalStateHash));
            var next=new CampaignRunRecovery151(CampaignReplayRules130.CurrentCycle(q),key,failures,
                run?.CompletedChapterIds??CampaignReplayRules130.CurrentCompleted(q),q.RunRecovery151?.Boundaries);
            return Result<CampaignState>.Success(WithProgress(after,q.With(runRecovery151:next,replaceRunRecovery151:true)));
        }

        public static Result<CampaignState> Restart(CampaignState state,IWorldGateOperationsCatalog023 boards,
            string expectedRevision,bool confirmed)
        {
            var p=state?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            if(p==null||string.IsNullOrWhiteSpace(expectedRevision))return Result<CampaignState>.Failure("CAMPAIGN151_SAVED_GUILD_REQUIRED");
            if(!confirmed)return Result<CampaignState>.Success(state);
            var old=p.RunRecovery151?.Boundaries.FirstOrDefault(b=>b.BeforeHash==expectedRevision);
            if(old!=null)return Result<CampaignState>.Success(state); // Resolve the same completed request first.
            if(CanonicalJson.Sha256Hex(state)!=expectedRevision)return Result<CampaignState>.Failure("Your Guild changed. Review the restart again.");
            if(!IsEligible(state))return Result<CampaignState>.Failure("Ten settled defeats on this quest are required.");
            var parked=CampaignRecoveryCommands150.Pause(state,boards);
            if(!parked.IsSuccess)return parked;
            var candidate=parked.Value;
            if(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(candidate,true))
                return Result<CampaignState>.Failure("Finish and bank the current activity before restarting Campaign.");
            var city=candidate.Guild.GuildCity;p=city.Strategic017H.Campaign019;
            var boundaries=(p.RunRecovery151?.Boundaries??Array.Empty<CampaignRunBoundary151>()).ToList();
            if(boundaries.Count>=4096||city.OperationOrdinal==int.MaxValue)return Result<CampaignState>.Failure("The Campaign history needs a capacity update before another restart.");
            var ordinal=city.OperationOrdinal+1;var cycle=CampaignReplayRules130.CurrentCycle(p);
            var retired=candidate.Recovery150;
            var commitment=CanonicalJson.Sha256Hex((object)retired??p.RunRecovery151);
            var request=BoundaryId(state,cycle,ordinal,expectedRevision,commitment);
            boundaries.Add(new CampaignRunBoundary151(cycle,ordinal,expectedRevision,retired?.Operation.OperationId??p.RunRecovery151.Defeats.Last().AttemptId,commitment,request));
            var run=new CampaignRunRecovery151(cycle,string.Empty,Array.Empty<CampaignDefeatEvidence151>(),Array.Empty<string>(),boundaries);
            p=p.With(activeArcId:"ARC018_FIRST_GATE_ECHOES",activeChapterId:string.Empty,
                runRecovery151:run,replaceRunRecovery151:true,lastCheckpointId:request);
            city=city.With(operationOrdinal:ordinal,strategic017H:city.Strategic017H.With(campaign019:p,replaceCampaign019:true),replaceStrategic017H:true,lastCheckpointId:request);
            // No Guild snapshot restoration, rewards, healing, cycle advance or next encounter.
            return Result<CampaignState>.Success(candidate.WithRecovery150(null).With(candidate.Guild.WithGuildCity(city),candidate.OpeningFlow));
        }
        static CampaignState WithProgress(CampaignState state,CampaignProgressState019 progress)
        {
            var city=state.Guild.GuildCity;
            return state.With(state.Guild.WithGuildCity(city.With(strategic017H:city.Strategic017H.With(campaign019:progress,replaceCampaign019:true),replaceStrategic017H:true)),state.OpeningFlow);
        }
    }
}
