namespace SecondDimension.Presentation
{
    public interface IUnionReserveAssignmentCoordinator109
    {
        M1CommandResult AssignReserveRecruitToUnion109(
            string recruitId, int unionIndex, int slotIndex, string expectedOccupantId);
    }

    public interface IUnionPlanningCoordinator132
    {
        bool UnionPlanEditable132 { get; }
        string UnionPlanStatus132 { get; }
    }

    public sealed partial class M1RuntimeCoordinator : IUnionReserveAssignmentCoordinator109, IUnionPlanningCoordinator132
    {
        public bool UnionPlanEditable132 => !TowerWriteBusy116() &&
            Gameplay.M1.UnionBattlePlanRules132.CanEdit(_campaign, out _);
        public string UnionPlanStatus132 => TowerWriteBusy116() ? "A Guild update is being saved."
            : !Gameplay.M1.UnionBattlePlanRules132.CanEdit(_campaign, out var reason132) ? reason132
            : Gameplay.M1.UnionBattlePlanRules132.HasCommittedRoster(_campaign) ? "Applies next battle."
            : "Select a reserve, then a destination slot.";

        public M1CommandResult AssignReserveRecruitToUnion109(
            string recruitId, int unionIndex, int slotIndex, string expectedOccupantId)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(TowerBusy116);
            var nextBattle132 = Gameplay.M1.UnionBattlePlanRules132.HasCommittedRoster(_campaign);
            return ApplyAndPersist(_commands.AssignReserveRecruitToUnion109(
                _campaign, recruitId, unionIndex, slotIndex, expectedOccupantId), notify: true,
                nextBattle132 ? "Union saved. Applies next battle."
                    : string.IsNullOrWhiteSpace(expectedOccupantId) ? "Hero assigned. Union saved."
                    : "Members swapped. Union saved.");
        }
    }
}
