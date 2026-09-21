using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.Campaign019
{
    [Serializable]
    public sealed class CampaignCycleBoundary130
    {
        [JsonConstructor]
        public CampaignCycleBoundary130(int cycleNumber,int operationOrdinalAtStart,
            int campaignProgressAtStart,string previousStateHash,string transitionReceiptId,CampaignFinaleProof132 finale132=null)
        {
            if(cycleNumber<2||operationOrdinalAtStart<0||campaignProgressAtStart<0)
                throw new ArgumentOutOfRangeException(nameof(cycleNumber));
            if(string.IsNullOrWhiteSpace(previousStateHash)||previousStateHash.Length!=64||
               previousStateHash.Any(value=>!Uri.IsHexDigit(value))||
               string.IsNullOrWhiteSpace(transitionReceiptId))
                throw new ArgumentException("A saved cycle boundary requires its exact identity.");
            CycleNumber=cycleNumber;OperationOrdinalAtStart=operationOrdinalAtStart;
            CampaignProgressAtStart=campaignProgressAtStart;
            PreviousStateHash=previousStateHash;TransitionReceiptId=transitionReceiptId;Finale132=finale132;
        }
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public CampaignFinaleProof132 Finale132{get;}
        public int CycleNumber{get;}
        public int OperationOrdinalAtStart{get;}
        public int CampaignProgressAtStart{get;}
        public string PreviousStateHash{get;}
        public string TransitionReceiptId{get;}
    }

    [Serializable]
    public sealed class CampaignReplayState130
    {
        [JsonConstructor]
        public CampaignReplayState130(int currentCycle,int growthPercent,
            IReadOnlyList<string> currentCycleCompletedChapterIds,
            IReadOnlyList<CampaignCycleBoundary130> cycleStarts)
        {
            if(currentCycle<2||!CampaignReplayRules130.IsAllowedGrowth(growthPercent)||
               cycleStarts==null||cycleStarts.Count!=currentCycle-1||cycleStarts.Count>65536)
                throw new ArgumentException("Invalid campaign cycle sequence.");
            var boundaries=new List<CampaignCycleBoundary130>(cycleStarts.Count);
            for(var i=0;i<cycleStarts.Count;i++)
            {
                var boundary=cycleStarts[i];
                if(boundary==null||boundary.CycleNumber!=i+2||
                   (i>0&&(boundary.OperationOrdinalAtStart<=boundaries[i-1].OperationOrdinalAtStart||
                           boundary.CampaignProgressAtStart<=boundaries[i-1].CampaignProgressAtStart)))
                    throw new ArgumentException("Campaign cycle boundaries must be consecutive and immutable.");
                boundaries.Add(boundary);
            }
            var completed=(currentCycleCompletedChapterIds??Array.Empty<string>()).ToArray();
            if(completed.Length>CampaignReplayRules130.ChapterCount||
               completed.Any(value=>!CampaignReplayRules130.IsChapterId(value))||
               completed.Distinct(StringComparer.Ordinal).Count()!=completed.Length)
                throw new ArgumentException("Invalid current-cycle chapter completion set.");
            Array.Sort(completed,StringComparer.Ordinal);
            CurrentCycle=currentCycle;GrowthPercent=growthPercent;
            CycleStarts=boundaries.AsReadOnly();
            CurrentCycleCompletedChapterIds=Array.AsReadOnly(completed);
        }
        public int CurrentCycle{get;}
        public int GrowthPercent{get;}
        public IReadOnlyList<string> CurrentCycleCompletedChapterIds{get;}
        public IReadOnlyList<CampaignCycleBoundary130> CycleStarts{get;}
        public CampaignReplayState130 WithCompleted(IReadOnlyList<string> completed)=>
            new CampaignReplayState130(CurrentCycle,GrowthPercent,completed,CycleStarts);
    }
}
