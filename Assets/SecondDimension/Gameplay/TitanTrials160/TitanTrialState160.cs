using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.TitanTrials160
{
    [Serializable]
    public sealed class TitanTrialProgress160
    {
        public int Slot{get;} public int HighestTier{get;}public long Victories{get;}
        [JsonConstructor] public TitanTrialProgress160(int slot,int highestTier,long victories)
        {if(slot<1||slot>12||highestTier<0||victories<highestTier)throw new ArgumentException("TITAN160_PROGRESS_INVALID");Slot=slot;HighestTier=highestTier;Victories=victories;}
        // A rank is earned only by distinct sequential harder tiers.
        [JsonIgnore] public int PersonalGoldRank=>HighestTier;
    }
    [Serializable]
    public sealed class TitanBossWindow160
    {
        public int PhaseIndex{get;} public int ActionIndex{get;}public int CompletedCycles{get;}public int LastResolvedRound{get;}
        public int HealChannelsAccepted{get;}public bool ExhaustedHealCycle{get;}
        public string WarningKey{get;}public string WarningChannel{get;}public int WarningRound{get;}
        public IReadOnlyList<string> WarningTargets{get;}public int WarningBossHp{get;}
        [JsonConstructor] public TitanBossWindow160(int phaseIndex=0,int actionIndex=0,int completedCycles=0,int lastResolvedRound=0,
            int healChannelsAccepted=0,bool exhaustedHealCycle=false,string warningKey=null,string warningChannel=null,int warningRound=0,
            IReadOnlyList<string> warningTargets=null,int warningBossHp=0)
        {
            if(phaseIndex<0||phaseIndex>2||actionIndex<0||actionIndex>3||completedCycles<0||lastResolvedRound<0||
               healChannelsAccepted<0||healChannelsAccepted>2||warningRound<0||warningBossHp<0)
                throw new ArgumentException("TITAN160_BOSS_WINDOW_INVALID");
            PhaseIndex=phaseIndex;ActionIndex=actionIndex;CompletedCycles=completedCycles;LastResolvedRound=lastResolvedRound;
            HealChannelsAccepted=healChannelsAccepted;ExhaustedHealCycle=exhaustedHealCycle;WarningKey=warningKey??"";
            WarningChannel=warningChannel??"";WarningRound=warningRound;WarningTargets=Array.AsReadOnly((warningTargets??Array.Empty<string>()).ToArray());
            WarningBossHp=warningBossHp;
            if(WarningTargets.Any(string.IsNullOrWhiteSpace)||WarningTargets.Distinct(StringComparer.Ordinal).Count()!=WarningTargets.Count||
               (WarningKey.Length==0&&(WarningRound!=0||WarningTargets.Count!=0||WarningChannel.Length!=0)))
                throw new ArgumentException("TITAN160_WARNING_INVALID");
        }
    }
    [Serializable]
    public sealed class TitanAttempt160
    {
        public string AttemptId{get;}public string BattleId{get;}public string ProfileId{get;}public long Ordinal{get;}
        public int Slot{get;}public int Tier{get;}public string CatalogIdentity{get;}public string RecipeIdentity{get;}
        public string AlliedRosterIdentity{get;}public IReadOnlyList<string> AlliedUnionIds{get;}public TitanBossWindow160 BossWindow{get;}
        [JsonConstructor] public TitanAttempt160(string attemptId,string battleId,string profileId,long ordinal,int slot,int tier,
            string catalogIdentity,string recipeIdentity,string alliedRosterIdentity,IReadOnlyList<string> alliedUnionIds,TitanBossWindow160 bossWindow=null)
        {
            if(new[]{attemptId,battleId,profileId,catalogIdentity,recipeIdentity,alliedRosterIdentity}.Any(string.IsNullOrWhiteSpace)||
               ordinal<1||slot<1||slot>12||tier<1||alliedUnionIds==null||alliedUnionIds.Count<1||alliedUnionIds.Count>10||
               alliedUnionIds.Any(string.IsNullOrWhiteSpace)||alliedUnionIds.Distinct(StringComparer.Ordinal).Count()!=alliedUnionIds.Count)
                throw new ArgumentException("TITAN160_ATTEMPT_INVALID");
            AttemptId=attemptId;BattleId=battleId;ProfileId=profileId;Ordinal=ordinal;Slot=slot;Tier=tier;
            CatalogIdentity=catalogIdentity;RecipeIdentity=recipeIdentity;AlliedRosterIdentity=alliedRosterIdentity;
            AlliedUnionIds=Array.AsReadOnly(alliedUnionIds.ToArray());BossWindow=bossWindow??new TitanBossWindow160();
        }
        public TitanAttempt160 WithWindow(TitanBossWindow160 window)=>new TitanAttempt160(AttemptId,BattleId,ProfileId,Ordinal,Slot,Tier,
            CatalogIdentity,RecipeIdentity,AlliedRosterIdentity,AlliedUnionIds,window??throw new ArgumentNullException(nameof(window)));
    }
    [Serializable]
    public sealed class TitanSettlement160
    {
        public string AttemptId{get;}public int Slot{get;}public int Tier{get;}public string BattleId{get;}
        public string FinalBattleHash{get;}public string NativeBattleRewardId{get;}public BattleOutcome Outcome{get;}
        public string RewardReceiptId{get;}
        [JsonConstructor] public TitanSettlement160(string attemptId,int slot,int tier,string battleId,string finalBattleHash,
            string nativeBattleRewardId,BattleOutcome outcome,string rewardReceiptId)
        {
            if(new[]{attemptId,battleId,finalBattleHash,nativeBattleRewardId,rewardReceiptId}.Any(string.IsNullOrWhiteSpace)||
               slot<1||slot>12||tier<1||outcome==BattleOutcome.InProgress||!Enum.IsDefined(typeof(BattleOutcome),outcome))
                throw new ArgumentException("TITAN160_SETTLEMENT_INVALID");
            AttemptId=attemptId;Slot=slot;Tier=tier;BattleId=battleId;FinalBattleHash=finalBattleHash;
            NativeBattleRewardId=nativeBattleRewardId;Outcome=outcome;RewardReceiptId=rewardReceiptId;
        }
    }
    [Serializable]
    public sealed class TitanTrialState160
    {
        public string ProfileId{get;}public long AttemptOrdinal{get;}public TitanAttempt160 Active{get;}
        public IReadOnlyList<TitanSettlement160> Settlements{get;}
        [JsonConstructor] public TitanTrialState160(string profileId,long attemptOrdinal=0,TitanAttempt160 active=null,IReadOnlyList<TitanSettlement160> settlements=null)
        {
            if(string.IsNullOrWhiteSpace(profileId)||attemptOrdinal<0||active!=null&&(active.ProfileId!=profileId||active.Ordinal!=attemptOrdinal))
                throw new ArgumentException("TITAN160_STATE_INVALID");
            ProfileId=profileId;AttemptOrdinal=attemptOrdinal;Active=active;
            Settlements=Array.AsReadOnly((settlements??Array.Empty<TitanSettlement160>()).ToArray());
            if(Settlements.Any(x=>x==null)||Settlements.Count>attemptOrdinal||
               Settlements.Select(x=>x.AttemptId).Distinct(StringComparer.Ordinal).Count()!=Settlements.Count||
               Settlements.Select(x=>x.NativeBattleRewardId).Distinct(StringComparer.Ordinal).Count()!=Settlements.Count||
               Settlements.Select(x=>x.BattleId).Distinct(StringComparer.Ordinal).Count()!=Settlements.Count||
               Settlements.Select(x=>x.RewardReceiptId).Distinct(StringComparer.Ordinal).Count()!=Settlements.Count||
               active!=null&&Settlements.Any(x=>x.AttemptId==active.AttemptId))throw new ArgumentException("TITAN160_DUPLICATE_SETTLEMENT");
            // Reject skipped harder tiers on read. Same-tier replay is legal.
            var highest=new int[12];
            foreach(var entry in Settlements)
            {
                if(entry.Tier>highest[entry.Slot-1]+1||
                   entry.Slot>1&&highest[entry.Slot-2]<entry.Tier||
                   entry.Slot==1&&entry.Tier>1&&highest[11]<entry.Tier-1)
                    throw new ArgumentException("TITAN160_SKIPPED_CYCLE");
                if(entry.Outcome==BattleOutcome.Victory)highest[entry.Slot-1]=Math.Max(highest[entry.Slot-1],entry.Tier);
            }
            for(int slot=1;slot<=12;slot++)Progress(slot);
        }
        public TitanTrialProgress160 Progress(int slot)
        {
            if(slot<1||slot>12)throw new ArgumentOutOfRangeException(nameof(slot));int highest=0;long victories=0;
            foreach(var entry in Settlements.Where(x=>x.Slot==slot&&x.Outcome==BattleOutcome.Victory))
            {if(entry.Tier>highest+1)throw new ArgumentException("TITAN160_SKIPPED_TIER");highest=Math.Max(highest,entry.Tier);victories=checked(victories+1);}
            return new TitanTrialProgress160(slot,highest,victories);
        }
        public bool HasSettlement(string receipt)=>Settlements.Any(x=>x.RewardReceiptId==receipt);
        public TitanTrialState160 WithActive(TitanAttempt160 active)=>new TitanTrialState160(ProfileId,active?.Ordinal??AttemptOrdinal,active,Settlements);
    }
    public static class TitanTrialRules160
    {
        // Hall level must be read from TownProgression159's purchased Hall in
        // the eventual native coordinator. GuildLevel is NOT this facility.
        public static string Eligibility(TitanTrialState160 state,int hallLevel,int slot,int tier,int deployedUnionCount)
        {
            if(state==null)return "Open your Guild first.";
            if(hallLevel<10)return "Guild Hall level 10 is required.";
            if(state.Active!=null)return "Finish the current Titan attempt.";
            if(slot<1||slot>12||tier<1)return "Choose a valid Titan and tier.";
            if(deployedUnionCount<1||deployedUnionCount>10)return "Assign a legal army.";
            if(tier>state.Progress(slot).HighestTier+1)return "Clear the previous tier of this Titan first.";
            if(slot>1&&state.Progress(slot-1).HighestTier<tier)return "Clear the previous Titan in this tier first.";
            if(slot==1&&tier>1&&state.Progress(12).HighestTier<tier-1)return "Finish the twelve Titans in the previous tier first.";
            return null;
        }
        public static TitanAttempt160 PrepareAttempt(CampaignState campaign,TitanTrialState160 state,TitanTrialCatalog160 catalog,int hallLevel,int slot,int tier)
            =>PrepareAttempt(campaign,state,catalog,hallLevel,slot,tier,
                campaign?.Guild?.Unions.Where(x=>x.Kind==UnionKind.Normal).OrderBy(x=>x.UnionId,StringComparer.Ordinal).Select(x=>x.UnionId).ToArray());
        public static TitanAttempt160 PrepareAttempt(CampaignState campaign,TitanTrialState160 state,TitanTrialCatalog160 catalog,int hallLevel,int slot,int tier,IReadOnlyList<string> legalUnionIds)
        {
            if(campaign==null||catalog==null||state==null||state.ProfileId!=campaign.CampaignGuid)throw new ArgumentException("TITAN160_PROFILE_REQUIRED");
            if(legalUnionIds==null||legalUnionIds.Count<1||legalUnionIds.Count>10||legalUnionIds.Any(string.IsNullOrWhiteSpace)||legalUnionIds.Distinct(StringComparer.Ordinal).Count()!=legalUnionIds.Count)
                throw new ArgumentException("TITAN161_LEGAL_DEPLOYMENT_REQUIRED");
            var owned=campaign.Guild.Recruits.Select(x=>x.RecruitId).ToHashSet(StringComparer.Ordinal);
            var unions=legalUnionIds.Select(id=>campaign.Guild.Unions.Single(x=>x.UnionId==id&&x.Kind==UnionKind.Normal&&x.MemberRecruitIds.Any(owned.Contains))).ToArray();
            var reason=Eligibility(state,hallLevel,slot,tier,unions.Length);if(reason!=null)throw new InvalidOperationException(reason);
            var definition=catalog.Trial(slot);definition.Stats(tier);
            var ordinal=checked(state.AttemptOrdinal+1);
            var roster=CanonicalJson.Sha256Hex(new{Unions=unions,Recruits=campaign.Guild.Recruits.Where(x=>unions.Any(u=>u.MemberRecruitIds.Contains(x.RecruitId))).OrderBy(x=>x.RecruitId,StringComparer.Ordinal).ToArray()});
            var hash=CanonicalJson.Sha256Hex(new{TitanTrialCatalog160.Policy,campaign.CampaignGuid,campaign.CampaignSeed,ordinal,slot,tier,
                catalog.Identity,definition.RecipeIdentity,AlliedRosterIdentity=roster});
            return new TitanAttempt160("TITAN_ATTEMPT160_"+hash.ToUpperInvariant(),"TITAN_BATTLE160_"+hash.ToUpperInvariant(),
                campaign.CampaignGuid,ordinal,slot,tier,catalog.Identity,definition.RecipeIdentity,roster,unions.Select(x=>x.UnionId).ToArray());
        }
    }
}
