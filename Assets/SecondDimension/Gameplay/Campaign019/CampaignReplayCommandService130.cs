using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign019
{
    public sealed class CampaignReplayCommandService130
    {
        public Result<CampaignState> StartNextCycle(CampaignState campaign,
            ICampaignRuleCatalog019 chapters,IWorldGateOperationsCatalog023 boards,
            int expectedCurrentCycle,int growthPercent=CampaignReplayRules130.DefaultGrowthPercent)
        {
            var city=campaign?.Guild?.GuildCity;
            var strategic=city?.Strategic017H;
            var progress=strategic?.Campaign019;
            var playable=progress?.Playable020;
            if(progress==null||playable==null||boards==null||
               !CampaignReplayRules130.ValidateCatalog(chapters))
                return Result<CampaignState>.Failure("CAMPAIGN130_COMPLETE_CATALOG_REQUIRED");
            if(!CampaignReplayRules130.ValidateState(campaign,out var stateError))
                return Result<CampaignState>.Failure(stateError);
            if(expectedCurrentCycle!=CampaignReplayRules130.CurrentCycle(progress))
                return Result<CampaignState>.Failure("CAMPAIGN130_CYCLE_ALREADY_CHANGED");
            if(!CampaignReplayRules130.IsAllowedGrowth(growthPercent)||
               (progress.Replay130!=null&&growthPercent!=progress.Replay130.GrowthPercent))
                return Result<CampaignState>.Failure("CAMPAIGN130_GROWTH_SETTING_INVALID");
            if(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign)||
               !string.IsNullOrWhiteSpace(progress.ActiveChapterId)||
               playable.ActiveOperationGrant!=null||playable.ActiveStepLedger.Count!=0)
                return Result<CampaignState>.Failure("CAMPAIGN130_FINISH_ACTIVE_ADVENTURE_FIRST");
            if(!CampaignReplayRules130.HasCompleteChapterSet(CampaignReplayRules130.CurrentCompleted(progress)))
                return Result<CampaignState>.Failure("CAMPAIGN130_COMPLETE_ALL_82_CHAPTERS_FIRST");
            var runtime=playable.WorldGate023;
            if(runtime==null||!CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                   campaign,boards,runtime)||runtime.ActiveNodeLedger.Count!=0||
               !CampaignReplayRules130.RequiredChapterIds.All(id=>
                   runtime.CompletedDefinitionIds.Contains(id)&&
                   runtime.CompletionProofs.Any(proof=>proof.DefinitionId==id&&
                       CampaignReplayRules130.IsCurrentCycleProof(progress,proof)))||
               progress.AppliedReceiptIds.Count<CampaignReplayRules130.ChapterCount||
               progress.AppliedReceiptIds.Any(id=>!campaign.Guild.Development.HasClaimedReward(id)))
                return Result<CampaignState>.Failure("CAMPAIGN130_VERIFIED_CHAPTER_REWARDS_REQUIRED");
            if(runtime.AuthorityEntries.Count>=CampaignWorldGateCommandService023.AuthorityChainEntryLimit084)
                return Result<CampaignState>.Failure("CAMPAIGN023_AUTHORITY_CHAIN_CAP_REACHED");
            var nextCycle=checked(expectedCurrentCycle+1);
            var previousHash=CanonicalJson.Sha256Hex(campaign);
            var receiptId=CampaignReplayRules130.BoundaryReceiptId(campaign,nextCycle,growthPercent,
                city.OperationOrdinal,progress.CampaignProgress,previousHash);
            var starts=new List<CampaignCycleBoundary130>(progress.Replay130?.CycleStarts??Array.Empty<CampaignCycleBoundary130>())
            {
                new CampaignCycleBoundary130(nextCycle,city.OperationOrdinal,progress.CampaignProgress,previousHash,receiptId)
            };
            var replay=new CampaignReplayState130(nextCycle,growthPercent,Array.Empty<string>(),starts);
            progress=progress.With(activeArcId:"ARC018_FIRST_GATE_ECHOES",replay130:replay,
                replaceReplay130:true,lastCheckpointId:receiptId);
            strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:receiptId);
            city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:receiptId);
            return Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
        }
    }
}
