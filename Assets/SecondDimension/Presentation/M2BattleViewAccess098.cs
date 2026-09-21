namespace SecondDimension.Presentation
{
    /// <summary>
    /// Optional narrow reader for battle consumers. Returns the same fresh DTO
    /// projection as State.Battle without building unrelated guild/loadout views.
    /// It never selects, resolves, saves, caches or changes battle authority.
    /// </summary>
    public interface IM2BattleViewReader098
    {
        M2BattleView ReadBattleView098();
    }

    public static class M2BattleViewAccess098
    {
        public static M2BattleView Read(IM1PresentationCoordinator coordinator) =>
            coordinator is IM2BattleViewReader098 reader
                ? reader.ReadBattleView098()
                : coordinator?.State?.Battle;
    }
}
