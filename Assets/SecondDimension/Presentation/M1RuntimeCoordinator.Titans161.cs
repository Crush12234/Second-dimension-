using System;
using System.IO;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.TitanTrials160;
using SecondDimension.Gameplay.TitanHeroes161;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed class TitanTrialView161
    {
        public int Slot,Tier,HighestTier,Hp,Attack,Magic; public long Wins;
        public string Id,Name,HeroId,HeroName,GoldArtId,Reason,Reward,Pattern;
        public bool CanStart;
    }
    public sealed class TitanBoardView161
    {
        public int HallLevel; public bool Available,HasSavedBattle; public string Status;
        public TitanTrialView161[] Trials;
    }
    public sealed partial class M1RuntimeCoordinator
    {
        private TitanTrialCatalog160 _titanCatalog161;
        private string _titanLoadError161;
        public bool CurrentBattleIsTitan161 => TitanTrialCommands161.OwnsBattle(_campaign) ||
            _campaign?.TitanTrials160?.Settlements.Any(s=>s.BattleId==_campaign.Battle?.BattleId)==true;
        public string TitanForecastNotice161 => M2BattleCommandService.TitanForecastNotice161(_campaign?.Battle);
        private string TitanRewardHeroId161()
        {
            if(!CurrentBattleIsTitan161||_titanCatalog161==null)return null;
            var slot=_campaign.TitanTrials160.Active?.Slot??_campaign.TitanTrials160.Settlements.Last(s=>s.BattleId==_campaign.Battle.BattleId).Slot;
            return _titanCatalog161.Trial(slot).HeroId;
        }
        private string TitanRewardSummary161()
        {
            if(!CurrentBattleIsTitan161||_titanCatalog161==null||_campaign.Battle.Outcome==BattleOutcome.InProgress)return null;
            if(_campaign.Battle.Outcome!=BattleOutcome.Victory)return "This attempt did not earn a Titan hero or Gold rank. Claim the battle result, prepare your army and retry freely.";
            var state=_campaign.TitanTrials160;var prior=state.Settlements.LastOrDefault(s=>s.BattleId==_campaign.Battle.BattleId);
            var slot=state.Active?.Slot??prior.Slot;var tier=state.Active?.Tier??prior.Tier;var trial=_titanCatalog161.Trial(slot);
            var wins=state.Settlements.Where(s=>s.BattleId!=_campaign.Battle.BattleId&&s.Slot==slot&&s.Outcome==BattleOutcome.Victory).ToArray();
            var highest=wins.Select(s=>s.Tier).DefaultIfEmpty(0).Max();
            var text=highest==0?trial.HeroName+" joins your roster with personal Gold Art rank 1.":
                tier>highest?trial.HeroName+"'s personal Gold Art reaches rank "+tier+".":"Replay victory: battle XP earned. Gold rank is unchanged.";
            if((wins.Length+1)%2==0)
            {
                if(prior==null)text+=" This victory also resolves a hero bonus roll on claim.";
                else
                {
                    var bonus=TitanHeroRewards161.RepeatGrantedHero161(_campaign,prior);
                    text+=bonus==TitanHeroCatalog161.EidranId?" Bonus awarded: Eidran (or a bound Eidran ascension copy).":
                        bonus==trial.HeroId?" Bonus awarded: a bound "+trial.HeroName+" ascension copy.":" Bonus roll: no extra hero copy this time.";
                }
            }
            return (_campaign.Battle.Reward?.Claimed==true?"SAVED · ":"ON CLAIM · ")+text;
        }
        private void InitializeTitans161()
        {
            try
            {
                var path=Path.Combine(Application.streamingAssetsPath,"SecondDimension","TitanTrials160");
                _titanCatalog161=TitanTrialCatalog160.LoadFromDirectory(path);
                _combatContent=_combatContent.WithTitanTrials161(_titanCatalog161).WithTitanHeroes161();
            }
            catch(Exception e){_titanCatalog161=null;_titanLoadError161=e.Message;AppendStartupNotice("Titan content could not be loaded: "+e.Message);}
        }
        public TitanBoardView161 ReadTitans161(int tier=1)
        {
            if(_campaign==null||_titanCatalog161==null)
                return new TitanBoardView161{Status=_titanLoadError161??"Open your Guild first.",Trials=Array.Empty<TitanTrialView161>()};
            var progress=TitanTrialCommands161.Read(_campaign);
            var saved=TitanTrialCommands161.OwnsBattle(_campaign);
            var rows=_titanCatalog161.Trials.Select(t=>
            {
                var p=progress.Progress(t.Slot); var selected=Math.Max(1,tier);
                var reason=TitanTrialCommands161.BlockReason(_campaign,_combatContent,t.Slot,selected);
                int hp=0,attack=0,magic=0;
                try{var s=t.Stats(selected);hp=s.MaximumHp;attack=s.Attack;magic=s.MagicAttack;}
                catch(Exception e){reason=e.Message;}
                return new TitanTrialView161{Slot=t.Slot,Tier=selected,HighestTier=p.HighestTier,Wins=p.Victories,
                    Hp=hp,Attack=attack,Magic=magic,Id=t.Id,Name=t.Name,HeroId=t.HeroId,HeroName=t.HeroName,GoldArtId=t.GoldArtId,
                    CanStart=reason==null,Reason=reason??"Ready",Reward=(p.HighestTier==0
                        ? "First victory: "+t.HeroName+" + personal Gold Art rank 1"
                        :selected>p.HighestTier?"First clear of this tier: "+t.HeroName+"'s Gold Art reaches rank "+selected
                        :"Replay: battle XP; personal Gold rank stays unchanged.")+
                        "\nEvery second win: 25% matched ascension copy, 0.5% Eidran. First Eidran guaranteed by bonus roll 100 across all Titans.",
                    Pattern=string.Join("\n",t.Phases[0].Cycle.Select(k=>t.Action(k).PlayerCopy))};
            }).ToArray();
            return new TitanBoardView161{Available=true,HallLevel=TownProgression159.Level(_campaign,0),HasSavedBattle=saved,
                Status=saved?"Continue your saved Titan battle or claim its result.":
                    "Free attempts · Clear Titans in order · Complete all twelve to open the next tier",Trials=rows};
        }
        public Result<TitanTrialQuote161> QuoteTitan161(int slot,int tier) => TowerWriteBusy116()
            ?Result<TitanTrialQuote161>.Failure(TowerBusy116)
            :TitanTrialCommands161.Quote(_campaign,_combatContent,_titanCatalog161,slot,tier);
        public M1CommandResult BeginTitan161(TitanTrialQuote161 quote)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            return ApplyAndPersist(TitanTrialCommands161.Begin(_campaign,_battleCommands,_combatContent,_titanCatalog161,quote),
                true,"Titan encounter saved. Read the Forecast warnings before committing your army.");
        }
    }
}
