using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.TitanHeroes161
{
    [Serializable]
    public sealed class FrozenTitanHeroAbility161
    {
        [JsonConstructor]
        public FrozenTitanHeroAbility161(string heroId,string artId,int rank,long victories,int highestTier)
        {
            if(!TitanHeroCatalog161.TryGet(heroId,out _)||rank<1||victories<0||highestTier<0)throw new ArgumentException("TITAN161_FROZEN_ABILITY_INVALID");
            var recipe=TitanHeroCatalog161.Recipe(artId);
            if(recipe.IsEcho?(heroId!=TitanHeroCatalog161.EidranId||victories<1||highestTier<1):heroId!=TitanHeroCatalog161.HeroId(recipe.Slot))
                throw new ArgumentException("TITAN161_FROZEN_OWNER_INVALID");
            HeroId=heroId;ArtId=artId;Rank=rank;Victories=victories;HighestTier=highestTier;
        }
        public string HeroId{get;}public string ArtId{get;}public int Rank{get;}public long Victories{get;}public int HighestTier{get;}
        public int Scale(int clean)
        {
            if(clean<0)throw new ArgumentOutOfRangeException(nameof(clean));
            BigInteger coefficient=ArtId.StartsWith("ECHO_",StringComparison.Ordinal)?
                new BigInteger(10000)+new BigInteger(100)*(Victories-1)+new BigInteger(400)*(HighestTier-1):
                new BigInteger(10000)+new BigInteger(500)*(Rank-1);
            var value=(new BigInteger(clean)*coefficient+9999)/10000;
            return value>int.MaxValue?int.MaxValue:(int)value;
        }
    }
    [Serializable]
    public sealed class TitanHeroTimedEffect161
    {
        [JsonConstructor]
        public TitanHeroTimedEffect161(string kind,string unionId,string memberId,string sourceUnionId,string artId,int amount,int fromRound,int throughRound,string scope="ANY_DAMAGE")
        {
            if(string.IsNullOrWhiteSpace(kind)||string.IsNullOrWhiteSpace(unionId)||amount<0||fromRound<1||throughRound<fromRound)
                throw new ArgumentException("TITAN161_TIMED_EFFECT_INVALID");
            Kind=kind;UnionId=unionId;MemberId=memberId??"";SourceUnionId=sourceUnionId??"";ArtId=artId??"";Amount=amount;FromRound=fromRound;ThroughRound=throughRound;Scope=scope??"ANY_DAMAGE";
        }
        public string Kind{get;}public string UnionId{get;}public string MemberId{get;}public string SourceUnionId{get;}public string ArtId{get;}
        public int Amount{get;}public int FromRound{get;}public int ThroughRound{get;}public string Scope{get;}
        public TitanHeroTimedEffect161 WithAmount(int value)=>new TitanHeroTimedEffect161(Kind,UnionId,MemberId,SourceUnionId,ArtId,value,FromRound,ThroughRound,Scope);
    }
    [Serializable]
    public sealed class TitanHeroBattleRuntime161
    {
        [JsonConstructor]
        public TitanHeroBattleRuntime161(IEnumerable<FrozenTitanHeroAbility161> abilities=null,IEnumerable<string> usedHeroIds=null,IEnumerable<TitanHeroTimedEffect161> effects=null)
        {
            Abilities=Array.AsReadOnly((abilities??Array.Empty<FrozenTitanHeroAbility161>()).ToArray());
            if(Abilities.Any(x=>x==null)||Abilities.Select(x=>x.ArtId).Distinct(StringComparer.Ordinal).Count()!=Abilities.Count)throw new ArgumentException("TITAN161_DUPLICATE_FROZEN_ART");
            UsedHeroIds=Array.AsReadOnly((usedHeroIds??Array.Empty<string>()).Distinct(StringComparer.Ordinal).OrderBy(x=>x,StringComparer.Ordinal).ToArray());
            if(UsedHeroIds.Any(x=>!Abilities.Any(a=>a.HeroId==x)))throw new ArgumentException("TITAN161_UNKNOWN_USED_HERO");
            Effects=Array.AsReadOnly((effects??Array.Empty<TitanHeroTimedEffect161>()).ToArray());
        }
        public IReadOnlyList<FrozenTitanHeroAbility161> Abilities{get;}public IReadOnlyList<string> UsedHeroIds{get;}public IReadOnlyList<TitanHeroTimedEffect161> Effects{get;}
        public TitanHeroBattleRuntime161 With(IEnumerable<string> usedHeroIds=null,IEnumerable<TitanHeroTimedEffect161> effects=null)=>new TitanHeroBattleRuntime161(Abilities,usedHeroIds??UsedHeroIds,effects??Effects);
        public static TitanHeroBattleRuntime161 Capture(CampaignState campaign)
        {
            if(campaign?.Guild==null)return null;var list=new List<FrozenTitanHeroAbility161>();
            foreach(var recruit in campaign.Guild.Recruits)
            {
                int slot=TitanHeroCatalog161.Slot(recruit.RecruitId);if(slot==0)continue;
                if(slot<13)
                {
                    var p=campaign.TitanTrials160?.Progress(slot);
                    list.Add(new FrozenTitanHeroAbility161(recruit.RecruitId,"GOLD_TITAN_"+slot.ToString("000"),Math.Max(1,p?.HighestTier??0),p?.Victories??0,p?.HighestTier??0));
                }
                else if(campaign.TitanTrials160!=null)
                    for(int n=1;n<=12;n++){var p=campaign.TitanTrials160.Progress(n);if(p.Victories>0&&p.HighestTier>0)
                        list.Add(new FrozenTitanHeroAbility161(recruit.RecruitId,"ECHO_TITAN_"+n.ToString("000"),1,p.Victories,p.HighestTier));}
            }
            return list.Count==0?null:new TitanHeroBattleRuntime161(list);
        }
    }
}
