using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanTrials160;

namespace SecondDimension.Gameplay.TitanHeroes161
{
    public static class TitanHeroRewards161
    {
        // Proposal, native reward and these grants are persisted by the caller once.
        public static Result<CampaignState> Apply(CampaignState candidate,
            TitanTrialCatalog160 catalog, TitanSettlementProposal160 proposal)
        {
            try
            {
                if(candidate?.Guild?.GuildCity==null||catalog==null||proposal?.Receipt==null||
                   proposal.StateAfterNativeGrants.ProfileId!=candidate.CampaignGuid)
                    return Result<CampaignState>.Failure("TITAN161_GRANT_CONTEXT_REQUIRED");
                var r=proposal.Receipt;var battle=candidate.Battle;
                if(battle?.Reward==null||!battle.Reward.Claimed||battle.BattleId!=r.BattleId||
                   battle.FinalStateHash!=r.FinalBattleHash||battle.Reward.RewardId!=r.NativeBattleRewardId||
                   battle.Outcome!=r.Outcome||!M2BattleCommandService.HasValidFinalStateHash090(battle))
                    return Result<CampaignState>.Failure("TITAN161_NATIVE_REWARD_REQUIRED");
                string receipt="TITAN_HERO_GRANT161_"+r.RewardReceiptId;
                var sss=SssTenV4CampaignAccessor090.Read(candidate);
                if(sss.SpecialUseReceiptIds.Contains(receipt))return Result<CampaignState>.Success(candidate);
                if(proposal.AlreadyApplied)return Result<CampaignState>.Failure("TITAN161_GRANT_RECEIPT_MISSING");
                var next=candidate;
                if(r.Outcome==BattleOutcome.Victory)
                {
                    var trial=catalog.Trial(r.Slot);
                    if(!string.IsNullOrEmpty(proposal.FirstClearHeroId))
                    {
                        if(proposal.FirstClearHeroId!=trial.HeroId)throw new InvalidOperationException("TITAN161_HERO_MISMATCH");
                        next=GrantCopy(next,trial.HeroId,receipt+"_FIRST");
                    }
                    string repeat=RepeatOutcome161(candidate,proposal);
                    if(repeat=="EIDRAN")next=GrantCopy(next,TitanHeroCatalog161.EidranId,receipt+"_REPEAT");
                    else if(repeat=="MATCHED_COPY")next=GrantCopy(next,trial.HeroId,receipt+"_REPEAT");
                }
                sss=SssTenV4CampaignAccessor090.Read(next);
                return Result<CampaignState>.Success(next.WithSssV4090(sss.With(
                    specialUseReceiptIds:sss.SpecialUseReceiptIds.Concat(new[]{receipt}))));
            }
            catch(Exception e){return Result<CampaignState>.Failure("TITAN161_GRANT_REJECTED:"+e.Message);}
        }

        public static string RepeatOutcome161(CampaignState campaign,TitanSettlementProposal160 proposal)
        {
            if(campaign==null||proposal==null||!proposal.RequiresRepeatBonusResolution||
               proposal.Receipt.Outcome!=BattleOutcome.Victory)return "NONE";
            long opportunities=0;
            for(int slot=1;slot<=12;slot++)opportunities=checked(opportunities+proposal.StateAfterNativeGrants.Progress(slot).Victories/2);
            bool owned=SssTenV4Roster090.RosterContains(campaign.Guild.Recruits,TitanHeroCatalog161.EidranId);
            var seed=SemanticSeed.Derive(campaign.CampaignSeed,"TITAN_REPEAT161",campaign.CampaignGuid,proposal.Receipt.RewardReceiptId);
            int ticket=new Pcg32(seed.Seed,seed.Stream).NextInclusive(0,9999);
            return RepeatResultForTicket161(ticket,opportunities,owned);
        }

        // Read the committed copy receipt, never sample again or infer ownership
        // before the claim from a roster which already contains the granted hero.
        public static string RepeatGrantedHero161(CampaignState campaign,TitanSettlement160 receipt)
        {
            if(campaign==null||receipt==null||receipt.Outcome!=BattleOutcome.Victory)return null;
            string overall="TITAN_HERO_GRANT161_"+receipt.RewardReceiptId;
            var applied=SssTenV4CampaignAccessor090.Read(campaign).SpecialUseReceiptIds;
            if(!applied.Contains(overall))return null;
            string matched=TitanHeroCatalog161.HeroId(receipt.Slot);
            bool eidran=applied.Contains(CopyReceipt161(campaign,TitanHeroCatalog161.EidranId,overall+"_REPEAT"));
            bool copy=applied.Contains(CopyReceipt161(campaign,matched,overall+"_REPEAT"));
            if(eidran&&copy)throw new InvalidOperationException("TITAN161_REPEAT_RECEIPT_COLLISION");
            return eidran?TitanHeroCatalog161.EidranId:copy?matched:null;
        }

        static string CopyReceipt161(CampaignState campaign,string heroId,string authorityReceipt)=>
            "TITAN_COPY161_"+CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,heroId,authorityReceipt});

        public static string RepeatResultForTicket161(int ticket,long opportunities,bool eidranOwned)
        {
            if(ticket<0||ticket>=10000||opportunities<1)throw new ArgumentOutOfRangeException(nameof(ticket));
            if(!eidranOwned&&opportunities>=100)return "EIDRAN";
            return ticket<50?"EIDRAN":ticket<2550?"MATCHED_COPY":"NONE";
        }

        public static CampaignState GrantCopy(CampaignState campaign,string heroId,string authorityReceipt)
        {
            if(campaign?.Guild?.GuildCity==null||!TitanHeroCatalog161.TryGet(heroId,out _)||string.IsNullOrWhiteSpace(authorityReceipt))
                throw new ArgumentException("TITAN161_COPY_CONTEXT_REQUIRED");
            string receipt=CopyReceipt161(campaign,heroId,authorityReceipt);
            var sss=SssTenV4CampaignAccessor090.Read(campaign);
            if(sss.SpecialUseReceiptIds.Contains(receipt))return campaign;
            var guild=campaign.Guild;
            if(SssTenV4Roster090.RosterContains(guild.Recruits,heroId))
            {
                // Native, hero-bound A0–A10 resource. An A10 duplicate never makes another body.
                var inventory=new List<EquipmentItemState>(guild.Inventory);
                var credit=SssTenV4Inventory090.CreateAscensionCredit(campaign,heroId,receipt);
                if(!inventory.Any(x=>x.InstanceId==credit.InstanceId))inventory.Add(credit);
                guild=guild.With(guild.TreasuryXp,guild.Recruits,guild.Unions,inventory.AsReadOnly(),guild.Development);
            }
            else
            {
                var recruit=SssTenV4Roster090.MaterializeGrant(heroId).Recruit;
                var recruits=new List<RecruitState>(guild.Recruits){recruit};
                var assignments=new List<GuildMemberAssignmentState017D>(guild.GuildCity.MemberAssignments)
                    {new GuildMemberAssignmentState017D(recruit.RecruitId,GuildMemberAssignmentKind017D.Reserve,string.Empty,0,0,0)};
                guild=guild.With(guild.TreasuryXp,recruits.AsReadOnly(),guild.Unions,guild.Inventory,guild.Development)
                    .WithGuildCity(guild.GuildCity.With(memberAssignments:assignments.AsReadOnly(),lastCheckpointId:"titan161_hero_granted"));
            }
            return campaign.With(guild,campaign.OpeningFlow).WithSssV4090(sss.With(
                specialUseReceiptIds:sss.SpecialUseReceiptIds.Concat(new[]{receipt})));
        }
    }
}
