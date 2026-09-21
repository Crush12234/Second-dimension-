using System;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.TitanHeroes161;
namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator
    {
        public static string TitanHeroProgressionSummary161(CampaignState campaign,string heroId)
        {
            int slot=TitanHeroCatalog161.Slot(heroId);if(slot==0)return string.Empty;
            if(slot<13)
            {
                int best=campaign?.TitanTrials160?.Progress(slot).HighestTier??0;
                int rank=Math.Max(1,best);long bonus=500L*(rank-1);
                return TitanHeroCatalog161.Recipe("GOLD_TITAN_"+slot.ToString("000")).Name+" • PERSONAL GOLD RANK "+rank+
                    " • +"+(bonus/100)+"% MAGNITUDE\n6 AP + 10 MP • ONCE PER BATTLE • HIGHEST TITAN TIER "+best;
            }
            int unlocked=0;long wins=0;
            if(campaign?.TitanTrials160!=null)for(int i=1;i<=12;i++)
            {var progress=campaign.TitanTrials160.Progress(i);if(progress.Victories>0)unlocked++;wins=checked(wins+progress.Victories);}
            return "ACCORD OF THE TWELVE • "+unlocked+" / 12 ECHOES • "+wins+" TITAN WINS\n"+
                "CHOOSE ONE EARNED ECHO IN FORECAST • ONE SHARED USE • 6 AP + 10 MP";
        }
    }
}
