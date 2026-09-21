using System;
using System.Numerics;
using SecondDimension.Prepared.EndlessMathR10;
namespace SecondDimension.Gameplay.State
{
    public static class RecruitGrowthRules152
    {
        public const string Policy="RECRUIT_QUADRATIC_50_NATIVE_V2";
        // Identical to the established 021 curve at every previously supported level.
        public static int Level(long xp)=>checked((int)EndlessGrowthMath.LevelForQuadraticRecruitXp(xp,50));
        public static long Threshold(int level)=>checked((long)EndlessGrowthMath.QuadraticRecruitXpThreshold(level,50));
        public static long NextGap(int level)=>checked((long)(EndlessGrowthMath.QuadraticRecruitXpThreshold((BigInteger)level+1,50)-EndlessGrowthMath.QuadraticRecruitXpThreshold(level,50)));
        // Established native Class Training conversion, credited only for missing XP.
        public static long Price(long missing,int nativePersonalXpPerSession)=>checked((long)
            EndlessGrowthMath.TreasuryCostForPersonalXp(missing,
                GuildCity017D.GuildCityRecruitmentService017D.MemberTrainingCostTreasuryXp067,nativePersonalXpPerSession));
    }
}
