using System;
using System.Linq;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    // R12 first-ten Treasury tariffs. Stored in the existing facility authority;
    // Guild XP remains earned history, separate from the purchased Guild Hall.
    public static class TownProgression159
    {
        public static readonly string[] StateIds={"CITYTAB_GUILD_HALL","CITYTAB_MERCHANT_ROW","CITYTAB_FORGE","CITYTAB_TRAINING","CITYTAB_RECRUITMENT","CITYTAB_EXPEDITION","CITYTAB_INFIRMARY","CITYTAB_ACADEMY"};
        public static readonly string[] NativeIds={"","FACILITY_TAVERN","FACILITY_GUILD_FORGE","FACILITY_TRAINING_HALL","FACILITY_RECRUITMENT_OFFICE","FACILITY_STRATEGY_ROOM","FACILITY_INFIRMARY","FACILITY_RESEARCH_ROOM"};
        static readonly long[] Common={0,140,370,700,1360,2240,3330,5400,7890,11790,17410};
        static readonly long[] Hall={0,170,460,870,1700,2800,4160,6750,9860,14740,21760};
        static readonly long[] Infirmary={0,110,280,530,1020,1680,2500,4050,5920,8840,13060};
        public static int Level(CampaignState state,int index)
        {
            if(index<0||index>=StateIds.Length)throw new ArgumentOutOfRangeException(nameof(index));
            if(state?.Guild==null)return 0;
            var saved=state.Guild.Development.Facilities.FirstOrDefault(f=>f.FacilityId==StateIds[index]);
            if(index==0)return saved?.Level??0;
            var facility=state.Guild.Development.Facilities.Where(f=>f.FacilityId==NativeIds[index]).Select(f=>f.Level).DefaultIfEmpty(0).Max();
            var building=state.Guild.GuildCity.CityPlots.Where(p=>p.BuildingId==TownService153.BuildingIds[index]).Select(p=>p.BuildingLevel).DefaultIfEmpty(0).Max();
            // Existing Hall/plot upgrades can follow a paid Town upgrade. Always
            // retain the greatest earned level instead of charging those levels again.
            return Math.Max(saved?.Level??0,Math.Max(facility,building));
        }
        public static long Cost(int index,int targetLevel)
        {
            if(index<0||index>=StateIds.Length||targetLevel<1)throw new ArgumentOutOfRangeException();
            var prices=index==0?Hall:index==6?Infirmary:Common;
            if(targetLevel<=10)return prices[targetLevel];
            // R12 linear target-channel continuation: level 11 starts at the
            // level-10 tariff; every later step adds 1% of that fixed tariff.
            return checked((prices[10]*(100L+targetLevel-11)+99)/100);
        }
        public static long Discount(CampaignState state,int index,long baseCost)=>DiscountAtLevel(baseCost,Level(state,index));
        public static long DiscountAtLevel(long baseCost,int level)
        {
            if(baseCost<0||level<0)throw new ArgumentOutOfRangeException();
            if(baseCost==0)return 0;
            long denominator=100L+level;
            // Split before multiplication so a legitimate Int64 tariff cannot overflow.
            return Math.Max(1,checked((baseCost/denominator)*100+((baseCost%denominator)*100+denominator-1)/denominator));
        }
        public static long TreasuryBonusBasisPoints(CampaignState state)=>checked(75L*Level(state,0)+25L*Level(state,5));
        public static long PersonalBonusBasisPoints(CampaignState state)=>checked(50L*Level(state,7));
        public static string Benefit(int index,int before,int after)
        {
            if(index==0)return "Battle Treasury XP bonus: +"+(before*.75m)+"% → +"+(after*.75m)+"%."+(before<10&&after>=10?"\nMeets the Guild Hall level 10 requirement for Titans. Titan battles are still being connected.":"");
            if(index==5)return "Battle Treasury XP bonus: +"+(before*.25m)+"% → +"+(after*.25m)+"%.";
            if(index==7)return "Personal battle XP bonus: +"+(before*.5m)+"% → +"+(after*.5m)+"%.";
            if(index==6)return "Recovery progress per operation: +"+(5L*before)+" → +"+(5L*after)+" above base recovery.";
            var name=index==1?"Merchant equipment and restock":index==2?"Forge material":index==3?"Class and Art training":"Recruit signing";
            return name+" costs: "+(10000m/(100L+before)).ToString("0.####")+"% → "+(10000m/(100L+after)).ToString("0.####")+" of base cost. Positive costs round up to at least 1 "+(index==2?"material unit.":"XP.");
        }
    }
}
