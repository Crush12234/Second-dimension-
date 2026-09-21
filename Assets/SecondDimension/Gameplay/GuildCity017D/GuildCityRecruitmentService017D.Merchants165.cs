using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class MerchantHeroPreview165
    {
        public string StableId,Name,Rank,Summary,Failure; public int Cost; public bool Duplicate;
    }
    public sealed partial class GuildCityRecruitmentService017D
    {
        internal HeroMaster300Hero087 MerchantHero165(CampaignState state,string offer,int slot)
        {
            if(_heroMasterCatalog089==null)return null;
            var level=TownProgression159.Level(state,1);
            var rank=level>=5?HeroMasterRank087.S:level>=3?HeroMasterRank087.A:HeroMasterRank087.B;
            var pool=_heroMasterCatalog089.AcceptedHeroes.Where(h=>h.IsNormalApplicantEligible&&h.Rank<=rank)
                .OrderBy(h=>h.StableId,StringComparer.Ordinal).ToArray();
            // The first exchange slot offers a matching owned hero when possible,
            // so an actual paid duplicate is a dependable Ascension path.
            var owned=pool.Where(h=>MatchingEarnedCardOwners099(state,h).Length==1&&
                MatchingEarnedCardOwners099(state,h)[0].AuthorityKind==RecruitAuthorityKind.Normal).ToArray();
            if(slot==0&&owned.Length>0)pool=owned;
            else if(slot>0){var best=pool.Where(h=>h.Rank==rank).ToArray();if(best.Length>0)pool=best;}
            return pool.OrderBy(h=>CanonicalJson.Sha256Hex(new{Rule="TOWN165_HERO",state.CampaignGuid,
                state.CampaignSeed,Offer=offer,Hero=h.StableId}),StringComparer.Ordinal).FirstOrDefault();
        }
        public MerchantHeroPreview165 DescribeMerchantHero165(CampaignState state,string offer,int slot)
        {
            var hero=MerchantHero165(state,offer,slot);
            if(hero==null)return new MerchantHeroPreview165{Failure="The trusted hero catalog is unavailable."};
            var owners=MatchingEarnedCardOwners099(state,hero);
            if(owners.Length>1||owners.Length==1&&owners[0].AuthorityKind!=RecruitAuthorityKind.Normal)
                return new MerchantHeroPreview165{StableId=hero.StableId,Name=hero.Name,Failure="This hero's ownership needs reconciliation."};
            var summary=owners.Length==1?BuildHeroMasterDuplicateForecast089(owners[0],state.Guild.Recruits.ToList().IndexOf(owners[0]),hero.StableId).Summary:
                "New "+hero.Rank+" hero • "+hero.Role+" • joins Reserve";
            return new MerchantHeroPreview165{StableId=hero.StableId,Name=hero.Name,Rank=hero.Rank.ToString(),
                Cost=Math.Max(50,hero.RecruitCostXp),Duplicate=owners.Length==1,Summary=summary,
                Failure=owners.Length==0&&state.Guild.Recruits.Count>=NormalHeroAuthorityCapacity094?"The hero roster is full.":null};
        }
        internal Result<CampaignState> GrantMerchantHero165(CampaignState state,string offer,int slot,string expectedHero,string request)
        {
            var hero=MerchantHero165(state,offer,slot);
            if(hero==null||hero.StableId!=expectedHero)return Result<CampaignState>.Failure("The exchange hero changed. Review the offer again.");
            var preview=DescribeMerchantHero165(state,offer,slot);if(preview.Failure!=null)return Result<CampaignState>.Failure(preview.Failure);
            var owners=MatchingEarnedCardOwners099(state,hero);
            if(owners.Length==1)
            {
                var recruits=state.Guild.Recruits.ToList();var index=recruits.IndexOf(owners[0]);
                var growth=BuildHeroMasterDuplicateForecast089(owners[0],index,hero.StableId);
                recruits[index]=owners[0].WithProgression(growth.ProjectedProgression);
                var g=state.Guild;
                return Result<CampaignState>.Success(state.With(g.With(g.TreasuryXp,recruits.AsReadOnly(),g.Unions,g.Inventory,g.Development),state.OpeningFlow));
            }
            var applicant=HeroMaster300ApplicantLead089.ToApplicant(hero,1,"WORLD_GATE_01");
            var grant=new CreatorAccessCommandService028().GrantCharacterReward(state,request,hero.StableId,
                new CreatorRecruitGrant028(MaterializeApplicant094(applicant),ApplicantInventory094(applicant)));
            if(!grant.IsSuccess)return grant;
            if(grant.Value.Guild.Recruits.Count!=state.Guild.Recruits.Count+1||!HeroMaster300ApplicantLead089.RosterContains(grant.Value.Guild.Recruits,hero))
                return Result<CampaignState>.Failure("Native hero acquisition did not create the offered identity.");
            return grant;
        }
    }
}
