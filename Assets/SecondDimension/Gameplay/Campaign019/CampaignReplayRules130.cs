using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign019
{
    public static class CampaignReplayRules130
    {
        public const int ChapterCount=82;
        public const int DefaultGrowthPercent=25;
        // This is the shipped campaign's closed set, never a caller-selected subset.
        static readonly IReadOnlyList<string> RequiredIds=Array.AsReadOnly(
            Enumerable.Range(1,ChapterCount).Select(value=>
                "CH018_"+value.ToString("000",CultureInfo.InvariantCulture)).ToArray());
        public static IReadOnlyList<string> RequiredChapterIds=>RequiredIds;
        public static bool IsAllowedGrowth(int value)=>value==25||value==50||value==100;
        public static bool IsChapterId(string value)=>value!=null&&RequiredIds.Contains(value);
        public static int CurrentCycle(CampaignProgressState019 progress)=>progress?.Replay130?.CurrentCycle??1;
        public static int GrowthPercent(CampaignProgressState019 progress)=>progress?.Replay130?.GrowthPercent??DefaultGrowthPercent;
        public static IReadOnlyList<string> CurrentCompleted(CampaignProgressState019 progress)=>
            CampaignRunRecoveryCommands151.ActiveRun(progress)?.HasCurrentRun==true ? progress.RunRecovery151.CompletedChapterIds :
            progress?.Replay130?.CurrentCycleCompletedChapterIds??progress?.CompletedChapterIds??Array.Empty<string>();
        public static int CycleStartOrdinal(CampaignProgressState019 progress)=>
            progress?.Replay130?.CycleStarts.Last().OperationOrdinalAtStart??-1;
        public static bool HasCompleteChapterSet(IReadOnlyList<string> completed)=>
            completed!=null&&completed.Count==ChapterCount&&
            completed.Distinct(StringComparer.Ordinal).Count()==ChapterCount&&
            RequiredIds.All(completed.Contains);
        public static int CycleForCommittedOrdinal(CampaignProgressState019 progress,int committedOrdinal)
        {
            if(committedOrdinal<0)throw new ArgumentOutOfRangeException(nameof(committedOrdinal));
            var cycle=1;
            if(progress?.Replay130!=null)
                foreach(var boundary in progress.Replay130.CycleStarts)
                {
                    if(committedOrdinal<=boundary.OperationOrdinalAtStart)break;
                    cycle=boundary.CycleNumber;
                }
            return cycle;
        }
        public static bool IsCurrentCycleProof(CampaignProgressState019 progress,WorldGateCompletionProof023 proof)=>
            proof!=null && proof.CommittedOperationOrdinal>(CampaignRunRecoveryCommands151.ActiveRun(progress)?.StartOrdinal??-1) && (progress?.Replay130==null||
                (proof.CommittedOperationOrdinal>CycleStartOrdinal(progress)&&
                 CycleForCommittedOrdinal(progress,proof.CommittedOperationOrdinal)==CurrentCycle(progress)));
        public static string FrozenObjectiveTag(CampaignProgressState019 progress)=>
            FrozenObjectiveTagForCycle132(progress,CurrentCycle(progress));
        public static string FrozenObjectiveTagForCycle132(CampaignProgressState019 progress,int cycle)
        {
            var floor=progress?.Replay130?.CycleStarts.FirstOrDefault(value=>value.CycleNumber==cycle)?.Finale132;
            return floor==null?CampaignReplayThreat130.FormatTag130(cycle,GrowthPercent(progress)):
                CampaignReplayThreat130.FormatFinaleTag132(cycle,GrowthPercent(progress),floor);
        }
        public static CampaignReplayState130 WithChapterCompleted(CampaignProgressState019 progress,string chapterId)
        {
            if(progress.Replay130==null)return null;
            if(!IsChapterId(chapterId))throw new ArgumentException("Unknown campaign chapter.",nameof(chapterId));
            var completed=new List<string>(progress.Replay130.CurrentCycleCompletedChapterIds);
            if(!completed.Contains(chapterId))completed.Add(chapterId);
            return progress.Replay130.WithCompleted(completed);
        }
        public static string BoundaryReceiptId(CampaignState campaign,int cycle,int growth,
            int ordinal,int campaignProgress,string previousStateHash,CampaignFinaleProof132 finale132=null)=>
            finale132!=null?"CAMPAIGN_CYCLE132_"+CanonicalJson.Sha256Hex(new{
                campaign.CampaignGuid,campaign.CampaignSeed,Cycle=cycle,GrowthPercent=growth,
                OperationOrdinalAtStart=ordinal,CampaignProgressAtStart=campaignProgress,
                PreviousStateHash=previousStateHash,Finale132=finale132}).Substring(0,24).ToUpperInvariant():
            "CAMPAIGN_CYCLE130_"+CanonicalJson.Sha256Hex(new{
                campaign.CampaignGuid,campaign.CampaignSeed,Cycle=cycle,GrowthPercent=growth,
                OperationOrdinalAtStart=ordinal,CampaignProgressAtStart=campaignProgress,
                PreviousStateHash=previousStateHash}).Substring(0,24).ToUpperInvariant();
        public static bool ValidateCatalog(ICampaignRuleCatalog019 catalog)
        {
            if(catalog==null)return false;
            foreach(var id in RequiredIds)
                if(!catalog.TryGetChapter(id,out var chapter)||chapter==null||chapter.ChapterId!=id||
                   !catalog.TryGetArc(chapter.ArcId,out var arc)||arc==null||arc.ArcId!=chapter.ArcId||
                   arc.ChapterIds==null||arc.ChapterIds.Count(value=>value==id)!=1)
                    return false;
            return true;
        }
        public static bool ValidateState(CampaignState campaign,out string error)
        {
            error="CAMPAIGN130_CYCLE_STATE_INVALID";
            var city=campaign?.Guild?.GuildCity;
            var progress=city?.Strategic017H?.Campaign019;
            if(!CampaignRunRecoveryCommands151.ValidateHistory(campaign))return false;
            if(progress?.Replay130==null){error=string.Empty;return true;}
            var replay=progress.Replay130;
            if(!HasCompleteChapterSet(progress.CompletedChapterIds))return false;
            foreach(var boundary in replay.CycleStarts)
                if(boundary.OperationOrdinalAtStart>city.OperationOrdinal||
                   boundary.CampaignProgressAtStart>progress.CampaignProgress||
                   boundary.TransitionReceiptId!=BoundaryReceiptId(campaign,boundary.CycleNumber,
                       replay.GrowthPercent,boundary.OperationOrdinalAtStart,
                       boundary.CampaignProgressAtStart,boundary.PreviousStateHash,boundary.Finale132))return false;
            error=string.Empty;return true;
        }
    }
}
