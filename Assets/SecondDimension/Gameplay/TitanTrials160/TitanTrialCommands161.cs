using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanHeroes161;

namespace SecondDimension.Gameplay.TitanTrials160
{
    public sealed class TitanTrialQuote161
    {
        public int Slot { get; } public int Tier { get; }
        public string Revision { get; } public string CatalogIdentity { get; }
        internal TitanTrialQuote161(int slot,int tier,string revision,string catalog)
        {Slot=slot;Tier=tier;Revision=revision;CatalogIdentity=catalog;}
    }

    // Prepare the complete start/return in memory; the presentation coordinator
    // publishes it with the existing atomic save. Other sector records stay live.
    public static class TitanTrialCommands161
    {
        public static TitanTrialState160 Read(CampaignState campaign) =>
            campaign.TitanTrials160 ?? new TitanTrialState160(campaign.CampaignGuid);

        public static string BlockReason(CampaignState campaign,M2CombatContent content,int slot,int tier)
        {
            if(campaign?.Guild?.GuildCity==null||content==null)return "Open your Guild first.";
            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"TITANS"))return "Resume the saved Titan activity before starting another attempt.";
            if(campaign.TitanTrials160?.Active!=null)return "Continue the saved Titan battle or claim its result first.";
            var reason=IndependentProgression159.BlockReason(campaign);
            if(reason!=null)return reason;
            try
            {
                var army=M2BattleCommandService.TitanArmyUnionIds161(campaign,content);
                return TitanTrialRules160.Eligibility(Read(campaign),TownProgression159.Level(campaign,0),slot,tier,army.Count);
            }
            catch(Exception e){return "Your army is not ready: "+e.Message;}
        }

        public static Result<TitanTrialQuote161> Quote(CampaignState campaign,M2CombatContent content,
            TitanTrialCatalog160 catalog,int slot,int tier)
        {
            if(catalog==null)return Result<TitanTrialQuote161>.Failure("Titan content is unavailable.");
            var reason=BlockReason(campaign,content,slot,tier);
            if(reason!=null)return Result<TitanTrialQuote161>.Failure(reason);
            try
            {
                catalog.Trial(slot).Stats(tier);
                return Result<TitanTrialQuote161>.Success(new TitanTrialQuote161(slot,tier,
                    CanonicalJson.Sha256Hex(campaign),catalog.Identity));
            }
            catch(Exception e){return Result<TitanTrialQuote161>.Failure(e.Message);}
        }

        public static Result<CampaignState> Begin(CampaignState campaign,M2BattleCommandService battles,
            M2CombatContent content,TitanTrialCatalog160 catalog,TitanTrialQuote161 quote)
        {
            if(campaign==null||battles==null||content==null||catalog==null||quote==null)
                return Result<CampaignState>.Failure("Review the Titan encounter first.");
            if(quote.CatalogIdentity!=catalog.Identity||quote.Revision!=CanonicalJson.Sha256Hex(campaign))
                return Result<CampaignState>.Failure("Your Guild changed. Review this Titan again.");
            var reason=BlockReason(campaign,content,quote.Slot,quote.Tier);
            if(reason!=null)return Result<CampaignState>.Failure(reason);
            try
            {
                var state=Read(campaign);
                var army=M2BattleCommandService.TitanArmyUnionIds161(campaign,content);
                var attempt=TitanTrialRules160.PrepareAttempt(campaign,state,catalog,
                    TownProgression159.Level(campaign,0),quote.Slot,quote.Tier,army);
                var trial=catalog.Trial(quote.Slot);
                var request=new EncounterLaunchRequest017D(attempt.AttemptId,trial.Id,attempt.AttemptId,
                    "TITAN_BOARD161",trial.Id,trial.Id,attempt.BattleId,trial.Name+" · Tier "+quote.Tier,
                    1,attempt.RecipeIdentity,army,Array.Empty<string>(),new[]{trial.Id},
                    new[]{"TITAN_TRIAL161", "TITAN_TIER161_"+quote.Tier},0,0,0,"TITAN_RETURN161",quote.Revision);
                var authority=GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request);
                var g=campaign.Guild;
                if(!g.Development.CanRecordAdventureAuthority(authority))
                    return Result<CampaignState>.Failure("The saved encounter ledger cannot accept another entry.");
                var city=g.GuildCity.With(pendingEncounter:request,replacePendingEncounter:true,lastCheckpointId:"titan_start161");
                var candidate=campaign.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,
                    g.Development.RecordAdventureAuthority(authority)).WithGuildCity(city),campaign.OpeningFlow)
                    .WithTitanTrials160(state.WithActive(attempt));
                return battles.StartCommittedEncounterBattle017D(candidate,content,request,
                    M2BattleCommandService.CreateTitanRoster161(content,attempt));
            }
            catch(Exception e){return Result<CampaignState>.Failure("Titan start rejected: "+e.Message);}
        }

        public static bool OwnsBattle(CampaignState campaign) => campaign?.TitanTrials160?.Active!=null &&
            campaign.Battle?.BattleId==campaign.TitanTrials160.Active.BattleId;

        public static Result<CampaignState> ApplyClaimedReturn(CampaignState campaign,TitanTrialCatalog160 catalog)
        {
            if(campaign==null||catalog==null)return Result<CampaignState>.Failure("Titan return content is unavailable.");
            try
            {
                var state=Read(campaign);
                var proposal=TitanSettlementPlanner160.Prepare(campaign,state,catalog);
                if(proposal.AlreadyApplied)return Result<CampaignState>.Success(campaign);
                var active=state.Active;var city=campaign.Guild.GuildCity;var request=city.PendingEncounter;
                if(request==null||request.RequestId!=active.AttemptId||request.BattleId!=active.BattleId||
                    request.EncounterId!=catalog.Trial(active.Slot).Id||
                    !campaign.Guild.Development.HasAdventureAuthority(
                        GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)))
                    return Result<CampaignState>.Failure("The Titan result is missing its committed encounter authority.");
                var granted=TitanHeroRewards161.Apply(campaign,catalog,proposal);
                if(!granted.IsSuccess)return granted;
                var next=granted.Value;var g=next.Guild;
                if(!g.Development.CanRecordAdventureAuthority(proposal.Receipt.RewardReceiptId))
                    return Result<CampaignState>.Failure("The saved reward ledger cannot accept this result.");
                city=g.GuildCity.With(pendingEncounter:null,replacePendingEncounter:true,
                    pendingBattleReturn:null,replacePendingBattleReturn:true,lastCheckpointId:"titan_return161");
                // No operation-ordinal advance: Campaign and Tower retain their
                // committed checkpoint identities. Native battle XP is already claimed.
                next=next.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,
                    g.Development.RecordAdventureAuthority(proposal.Receipt.RewardReceiptId)).WithGuildCity(city),next.OpeningFlow)
                    .WithTitanTrials160(proposal.StateAfterNativeGrants);
                return Result<CampaignState>.Success(next);
            }
            catch(Exception e){return Result<CampaignState>.Failure("Titan reward rejected: "+e.Message);}
        }
    }
}
