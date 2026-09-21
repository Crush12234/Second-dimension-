namespace SecondDimension.Presentation
{
    public static class BattlePerformancePlanning008
    {
        public static BattlePerformanceBudget008 For(
            BattleDeviceProfile008 profile,
            bool reducedMotion)
        {
            switch (profile)
            {
                case BattleDeviceProfile008.MobileLow:
                    return new BattlePerformanceBudget008(
                        2, 2, reducedMotion ? 22 : 26,
                        2, 12, reducedMotion ? 8 : 12, reducedMotion ? 4 : 6);
                case BattleDeviceProfile008.MobileHigh:
                    return new BattlePerformanceBudget008(
                        2, 2, reducedMotion ? 26 : 30,
                        3, 18, reducedMotion ? 10 : 15, reducedMotion ? 4 : 8);
                default:
                    return new BattlePerformanceBudget008(
                        2, 2, reducedMotion ? 30 : 34,
                        4, 24, reducedMotion ? 10 : 15, reducedMotion ? 4 : 8);
            }
        }
    }
}
