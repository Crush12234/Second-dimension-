using System;
using SecondDimension.Core;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public interface IAutoEquipmentCoordinator112
    {
        bool CanUndoAutoEquip112 { get; }
        M1CommandResult AutoEquipHero112(string recruitId);
        M1CommandResult AutoEquipAllUnions112();
        M1CommandResult UndoAutoEquip112();
    }

    public sealed partial class M1RuntimeCoordinator : IAutoEquipmentCoordinator112
    {
        public bool CanUndoAutoEquip112 => !TowerWriteBusy116() && string.IsNullOrEmpty(AutoEquipmentService112.UndoBlockedReason(_campaign));

        public M1CommandResult AutoEquipHero112(string recruitId) =>
            RunAutoEquipment112(service => service.EquipHero(_campaign, recruitId));

        public M1CommandResult AutoEquipAllUnions112() =>
            RunAutoEquipment112(service => service.EquipAllUnions(_campaign));

        M1CommandResult RunAutoEquipment112(Func<AutoEquipmentService112, Result<CampaignState>> action)
        {
            if (TowerWriteBusy116()) return M1CommandResult.Failure(EquipmentBusy123);
            if (_combatContent == null || _starterEquipment094 == null)
                return M1CommandResult.Failure("The approved equipment authority is unavailable.");
            var result = action(new AutoEquipmentService112(_combatContent, _starterEquipment094));
            if (result.IsSuccess && ReferenceEquals(result.Value, _campaign))
                return M1CommandResult.Success("No compatible upgrades found. Current equipment kept.");
            return ApplyAndPersist(result, notify: true,
                result.IsSuccess ? result.Value.EquipmentUndo112.Summary : "Equipment kept.");
        }

        public M1CommandResult UndoAutoEquip112() => TowerWriteBusy116() ? M1CommandResult.Failure(EquipmentBusy123) :
            ApplyAndPersist(AutoEquipmentService112.Undo(_campaign), notify: true,
                "Auto Equip undone. Previous equipment assignments restored and saved.");
    }
}
