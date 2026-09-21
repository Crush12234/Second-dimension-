namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : IM2BattleViewReader098
    {
        // Do not return a cached mutable presentation graph. Every read still
        // projects the current campaign's exact committed battle and metadata.
        public M2BattleView ReadBattleView098() => BuildBattleView();
    }
}
