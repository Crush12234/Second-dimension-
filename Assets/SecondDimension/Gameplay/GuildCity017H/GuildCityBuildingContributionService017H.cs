using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Gameplay.GuildCity017H
{
    public sealed class BuildingContributionSnapshot017H
    {
        public int PersonalXpBp,CombatArtMasteryBp,MysticMasteryBp,RestorationMasteryBp,WardingMasteryBp,UnionDisciplineBp,GuildTreasuryXpBp,CivicHallXpBp,StaffDutyXpFlat,RelationshipMemoryBp,StartingApFlat,StartingCohesionFlat,StartingMpFlat,EnemyCohesionDamageFlat,SuppliesFlat,ScoutingFlat,DefensePowerFlat,BarrierIntegrityFlat,ResupplyFlat,ReinforcementReadinessFlat,GuardPreparationFlat,UrgencyReductionFlat;
        public IReadOnlyList<string> SourceBuildingIds=Array.Empty<string>();
        public IReadOnlyList<string> ToRouteModifiers()
        {
            var result=new List<string>();
            Add(result,"CITY_PERSONAL_XP_BP_",PersonalXpBp);Add(result,"CITY_COMBAT_MASTERY_BP_",CombatArtMasteryBp);Add(result,"CITY_MYSTIC_MASTERY_BP_",MysticMasteryBp);Add(result,"CITY_RESTORATION_MASTERY_BP_",RestorationMasteryBp);Add(result,"CITY_WARDING_MASTERY_BP_",WardingMasteryBp);Add(result,"CITY_UNION_XP_BP_",UnionDisciplineBp);Add(result,"CITY_GUILD_XP_BP_",GuildTreasuryXpBp);Add(result,"CITY_HALL_XP_BP_",CivicHallXpBp);
            Add(result,"CITY_AP_BONUS_",StartingApFlat);Add(result,"CITY_COHESION_BONUS_",StartingCohesionFlat);Add(result,"CITY_MP_BONUS_",StartingMpFlat);Add(result,"CITY_ENEMY_COHESION_DAMAGE_",EnemyCohesionDamageFlat);Add(result,"CITY_RESUPPLY_",ResupplyFlat);Add(result,"CITY_DEFENSE_POWER_",DefensePowerFlat);
            if(GuardPreparationFlat>0)result.Add("CITY_GUARD_PREPARED");if(ScoutingFlat>0)result.Add("CITY_WAVE_SCOUTED");if(ReinforcementReadinessFlat>0)result.Add("CITY_REINFORCEMENT_READY");
            result.Sort(StringComparer.Ordinal);return result.AsReadOnly();
        }
        private static void Add(List<string> values,string prefix,int amount){if(amount>0)values.Add(prefix+amount);}
    }

    public sealed class GuildCityBuildingContributionService017H
    {
        public BuildingContributionSnapshot017H Calculate(GuildCityState017D city,GuildCityStrategicContent017H content)
        {
            if(city==null)throw new ArgumentNullException(nameof(city));if(content==null)throw new ArgumentNullException(nameof(content));
            var s=new BuildingContributionSnapshot017H();var sources=new List<string>();
            for(var i=0;i<city.CityPlots.Count;i++)
            {
                var plot=city.CityPlots[i];if(string.IsNullOrWhiteSpace(plot.BuildingId)||plot.BuildingLevel<=0||!content.Contributions.TryGetValue(plot.BuildingId,out var def))continue;
                var level=Math.Min(Math.Max(1,plot.BuildingLevel),Math.Max(1,def.MaximumContributionLevel));var staffed=plot.StaffRecruitIds!=null&&plot.StaffRecruitIds.Count>0?1:0;var multiplier=level+staffed;
                Add(s,def.PerLevel,multiplier);sources.Add(plot.BuildingId);
            }
            Clamp(s,content.BasisPointCap,content.FlatCap);sources.Sort(StringComparer.Ordinal);s.SourceBuildingIds=sources.AsReadOnly();return s;
        }
        private static void Add(BuildingContributionSnapshot017H s,BuildingContributionValues017H v,int m)
        {
            if(v==null)return;s.PersonalXpBp+=v.PersonalXpBp*m;s.CombatArtMasteryBp+=v.CombatArtMasteryBp*m;s.MysticMasteryBp+=v.MysticMasteryBp*m;s.RestorationMasteryBp+=v.RestorationMasteryBp*m;s.WardingMasteryBp+=v.WardingMasteryBp*m;s.UnionDisciplineBp+=v.UnionDisciplineBp*m;s.GuildTreasuryXpBp+=v.GuildTreasuryXpBp*m;s.CivicHallXpBp+=v.CivicHallXpBp*m;s.StaffDutyXpFlat+=v.StaffDutyXpFlat*m;s.RelationshipMemoryBp+=v.RelationshipMemoryBp*m;s.StartingApFlat+=v.StartingApFlat*m;s.StartingCohesionFlat+=v.StartingCohesionFlat*m;s.StartingMpFlat+=v.StartingMpFlat*m;s.EnemyCohesionDamageFlat+=v.EnemyCohesionDamageFlat*m;s.SuppliesFlat+=v.SuppliesFlat*m;s.ScoutingFlat+=v.ScoutingFlat*m;s.DefensePowerFlat+=v.DefensePowerFlat*m;s.BarrierIntegrityFlat+=v.BarrierIntegrityFlat*m;s.ResupplyFlat+=v.ResupplyFlat*m;s.ReinforcementReadinessFlat+=v.ReinforcementReadinessFlat*m;s.GuardPreparationFlat+=v.GuardPreparationFlat*m;s.UrgencyReductionFlat+=v.UrgencyReductionFlat*m;
        }
        private static void Clamp(BuildingContributionSnapshot017H s,int bp,int flat)
        {
            s.PersonalXpBp=C(s.PersonalXpBp,bp);s.CombatArtMasteryBp=C(s.CombatArtMasteryBp,bp);s.MysticMasteryBp=C(s.MysticMasteryBp,bp);s.RestorationMasteryBp=C(s.RestorationMasteryBp,bp);s.WardingMasteryBp=C(s.WardingMasteryBp,bp);s.UnionDisciplineBp=C(s.UnionDisciplineBp,bp);s.GuildTreasuryXpBp=C(s.GuildTreasuryXpBp,bp);s.CivicHallXpBp=C(s.CivicHallXpBp,bp);s.RelationshipMemoryBp=C(s.RelationshipMemoryBp,bp);s.StaffDutyXpFlat=C(s.StaffDutyXpFlat,flat);s.StartingApFlat=C(s.StartingApFlat,flat);s.StartingCohesionFlat=C(s.StartingCohesionFlat,flat);s.StartingMpFlat=C(s.StartingMpFlat,flat);s.EnemyCohesionDamageFlat=C(s.EnemyCohesionDamageFlat,flat);s.SuppliesFlat=C(s.SuppliesFlat,flat);s.ScoutingFlat=C(s.ScoutingFlat,flat);s.DefensePowerFlat=C(s.DefensePowerFlat,flat);s.BarrierIntegrityFlat=C(s.BarrierIntegrityFlat,flat);s.ResupplyFlat=C(s.ResupplyFlat,flat);s.ReinforcementReadinessFlat=C(s.ReinforcementReadinessFlat,flat);s.GuardPreparationFlat=C(s.GuardPreparationFlat,flat);s.UrgencyReductionFlat=C(s.UrgencyReductionFlat,flat);
        }
        private static int C(int value,int max)=>Math.Max(0,Math.Min(max,value));
        public static long ApplyBasisPoints(long value,int basisPoints)=>value<=0?value:checked(value+(value*basisPoints/10000L));
    }
    public static class GuildCityBattleModifierRules017H
    {
        public static int Amount(IReadOnlyList<string> modifiers,string prefix)
        {
            if(modifiers==null||string.IsNullOrWhiteSpace(prefix))return 0;var result=0;
            for(var i=0;i<modifiers.Count;i++)
            {
                var value=modifiers[i];if(string.IsNullOrWhiteSpace(value)||!value.StartsWith(prefix,StringComparison.Ordinal))continue;
                if(int.TryParse(value.Substring(prefix.Length),out var parsed))result=Math.Max(result,Math.Max(0,parsed));
            }
            return result;
        }
        public static long ApplyBasisPoints(long value,int basisPoints)=>value<=0?value:checked(value+(value*Math.Max(0,basisPoints)/10000L));
        public static int ApplyBasisPoints(int value,int basisPoints)=>value<=0?value:checked(value+(value*Math.Max(0,basisPoints)/10000));
        public static int MasteryBasisPoints(IReadOnlyList<string> modifiers,string discipline)
        {
            if(string.IsNullOrWhiteSpace(discipline))return 0;var upper=discipline.ToUpperInvariant();
            if(upper.Contains("MYSTIC")||upper.Contains("MAGIC")||upper.Contains("AETHER")||upper.Contains("FLAME")||upper.Contains("FROST")||upper.Contains("STORM")||upper.Contains("EARTH")||upper.Contains("SHADOW"))return Amount(modifiers,"CITY_MYSTIC_MASTERY_BP_");
            if(upper.Contains("RESTOR")||upper.Contains("HEAL"))return Amount(modifiers,"CITY_RESTORATION_MASTERY_BP_");
            if(upper.Contains("WARD")||upper.Contains("GUARD"))return Amount(modifiers,"CITY_WARDING_MASTERY_BP_");
            return Amount(modifiers,"CITY_COMBAT_MASTERY_BP_");
        }
    }

}