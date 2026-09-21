using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class HeroLevelQuote152
    {
        public string RecruitId{get;} public string Name{get;} public string Revision{get;}
        public string RequestId{get;} public int AddedLevels{get;} public long Cost{get;}
        public long Wallet{get;} public RecruitProgressionState Before{get;} public RecruitProgressionState After{get;}
        public long PersonalXpAdded=>After.TotalPersonalXp-Before.TotalPersonalXp;
        public bool Affordable=>Cost<=Wallet; public bool IsCatchUp153{get;}
        internal HeroLevelQuote152(string id,string name,string revision,string request,int add,long cost,long wallet,
            RecruitProgressionState before,RecruitProgressionState after,bool catchUp153=false)
        {RecruitId=id;Name=name;Revision=revision;RequestId=request;AddedLevels=add;Cost=cost;Wallet=wallet;Before=before;After=after;IsCatchUp153=catchUp153;}
    }
    public static class HeroPaidLevels152
    {
        static string Request(CampaignState s,string revision,string hero,int added,bool catchUp=false)=>
            (catchUp?"HERO_CATCHUP153_":"HERO_LEVEL152_")+CanonicalJson.Sha256Hex(new{s.CampaignGuid,Revision=revision,Hero=hero,AddedLevels=added,
                Policy=catchUp?RecruitGrowthRules152.Policy+"_CATCHUP_R50":RecruitGrowthRules152.Policy,PricePolicy="NATIVE_CLASS_TRAINING_067_PROPORTIONAL"}).Substring(0,24).ToUpperInvariant();

        public static Result<HeroLevelQuote152> Quote(CampaignState state,string hero,int added)=>QuoteCore(state,hero,added,false);
        public static Result<HeroLevelQuote152> QuoteCatchUp153(CampaignState state,string hero)
        {
            var benchmark=CatchUpTraining153.Benchmark(state);
            if(!benchmark.IsSuccess)return Result<HeroLevelQuote152>.Failure(benchmark.Errors.ToArray());
            var owned=state.Guild.Recruits.FirstOrDefault(r=>r.RecruitId==hero);
            if(owned==null)return Result<HeroLevelQuote152>.Failure("Choose an owned hero.");
            if(!state.Guild.Development.Facilities.Any(f=>f.FacilityId=="FACILITY_TRAINING_HALL"&&f.Level>=1))
                return Result<HeroLevelQuote152>.Failure("Training Grounds level 1 is required.");
            if(owned.Progression.Level>=benchmark.Value)return Result<HeroLevelQuote152>.Failure("This hero has already reached the current Catch Up target.");
            return QuoteCore(state,hero,checked(benchmark.Value-owned.Progression.Level),true);
        }
        static Result<HeroLevelQuote152> QuoteCore(CampaignState state,string hero,int added,bool catchUp)
        {
            if(catchUp?added<1:added!=0&&added!=1&&added!=5)return Result<HeroLevelQuote152>.Failure("Choose one or five levels.");
            var blocked=IndependentProgression159.BlockReason(state);
            if(blocked!=null)return Result<HeroLevelQuote152>.Failure(blocked);
            var recruit=state.Guild.Recruits.FirstOrDefault(r=>r.RecruitId==hero);
            if(recruit==null||!ProtectedActorPolicy.CanUseNormalEquipment(recruit))
                return Result<HeroLevelQuote152>.Failure("This hero cannot use ordinary level training.");
            var assignment=state.Guild.GuildCity.MemberAssignments.FirstOrDefault(a=>a.RecruitId==hero);
            if(assignment==null||(assignment.Kind!=GuildMemberAssignmentKind017D.Active&&
                assignment.Kind!=GuildMemberAssignmentKind017D.Reserve&&assignment.Kind!=GuildMemberAssignmentKind017D.Training))
                return Result<HeroLevelQuote152>.Failure("Return this hero from their assigned duty before development.");
            try
            {
                var before=recruit.Progression;
                var reconciled=before.MigrateGrowth152(CatchUpTraining153.NativeClass(recruit.ClassTendencyId));
                // Banked levels are reconciled separately, without disguising them as a purchase.
                if(added>0&&reconciled.Level!=before.Level)
                    return Result<HeroLevelQuote152>.Failure("Apply this hero's already-earned banked levels before purchasing more.");
                if(added==0&&(before.ProgressionVersion152!=0||reconciled.Level==before.Level))
                    return Result<HeroLevelQuote152>.Failure("This hero has no banked levels to apply.");
                var target=checked(reconciled.Level+added);
                var gap=added==0?0:checked(RecruitGrowthRules152.Threshold(target)-reconciled.TotalPersonalXp);
                var after=gap==0?reconciled:reconciled.GainPersonalXp(gap,CatchUpTraining153.NativeClass(recruit.ClassTendencyId));
                var cost=gap==0?0:RecruitGrowthRules152.Price(gap,GuildCityRecruitmentService017D.TrainingPersonalXpForPotential067(recruit.PotentialBasisPoints));
                var revision=CanonicalJson.Sha256Hex(state);
                if(after.Level!=target||gap<0||added>0&&cost<=0)throw new InvalidOperationException("Level quote has no verified benefit.");
                // Check the actual native stat storage BEFORE any debit. Wider combat storage
                // remains a separate migration; never truncate or saturate this purchase.
                checked { var hp=recruit.MaximumHp+after.MaximumHpBonus;var mp=recruit.MaximumMp+after.MaximumMpBonus;
                    if(hp<=0||mp<0)throw new OverflowException(); }
                return Result<HeroLevelQuote152>.Success(new HeroLevelQuote152(hero,recruit.DisplayName,revision,
                    Request(state,revision,hero,added,catchUp),added,cost,state.Guild.TreasuryXp,before,after,catchUp));
            }
            catch(Exception e) when(e is OverflowException||e is ArgumentException||e is InvalidOperationException)
            {return Result<HeroLevelQuote152>.Failure("This level target needs progression reconciliation: "+e.Message);}
        }

        public static Result<CampaignState> Confirm(CampaignState state,HeroLevelQuote152 quote)
        {
            if(state==null||quote==null||quote.RequestId!=Request(state,quote.Revision,quote.RecruitId,quote.AddedLevels,quote.IsCatchUp153))
                return Result<CampaignState>.Failure("Review this level purchase again.");
            // The existing native ledger resolves retries before checking changed level/revision.
            if(state.Guild.Development.HasAdventureAuthority(quote.RequestId))return Result<CampaignState>.Success(state);
            if(CanonicalJson.Sha256Hex(state)!=quote.Revision)return Result<CampaignState>.Failure("Your Guild changed. Review the current level cost again.");
            var current=quote.IsCatchUp153?QuoteCatchUp153(state,quote.RecruitId):Quote(state,quote.RecruitId,quote.AddedLevels);
            if(!current.IsSuccess)return Result<CampaignState>.Failure(current.Errors.ToArray());
            var q=current.Value;
            if(q.RequestId!=quote.RequestId||q.Cost!=quote.Cost||CanonicalJson.Sha256Hex(q.After)!=CanonicalJson.Sha256Hex(quote.After))
                return Result<CampaignState>.Failure("The level quote changed. Review it again.");
            if(!q.Affordable)return Result<CampaignState>.Failure("Not enough Treasury XP for this level purchase.");
            if(state.Guild.Development.AppliedAdventureAuthorityIds.Count>=GuildDevelopmentState.AdventureAuthorityEntryLimit)
                return Result<CampaignState>.Failure("The reward history needs a capacity update before this purchase.");
            var recruits=state.Guild.Recruits.Select(r=>r.RecruitId==q.RecruitId?r.WithProgression(q.After):r).ToArray();
            var guild=state.Guild.With(checked(state.Guild.TreasuryXp-q.Cost),recruits,state.Guild.Unions,state.Guild.Inventory,
                state.Guild.Development.RecordAdventureAuthority(q.RequestId));
            // No battle reward path, duty reassignment, mastery, course credit or ascension.
            return Result<CampaignState>.Success(state.With(guild,state.OpeningFlow));
        }
    }
}
