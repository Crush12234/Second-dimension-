using System;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Additional existing-roster commands for V4 family preparation and the
    /// completed Omega-hunt claim. Gameplay mutation remains in the SSS/M2 host
    /// authorities; this interface only lets the inventory presenter invoke it.
    /// </summary>
    public interface ISssPreparedFamilyPresentationCoordinator090
    {
        SssPreparedFamilyLoadoutView090 ReadSssPreparedFamilies090(string heroId);
        M1CommandResult CycleSssPreparedFamily090(string heroId, int slotIndex);
        M1CommandResult ClaimSssSignatureWeapon090(string heroId);
    }

    public sealed partial class M1RuntimeCoordinator
    {
        public SssPreparedFamilyLoadoutView090 ReadSssPreparedFamilies090(
            string heroId)
        {
            if (_campaign?.Guild == null)
                throw new InvalidOperationException("No active Guild campaign.");
            return SssPreparedFamilyService090.View(_campaign, heroId);
        }

        public M1CommandResult CycleSssPreparedFamily090(
            string heroId,
            int slotIndex)
        {
            try
            {
                if (_campaign?.Guild == null)
                    return M1CommandResult.Failure("No active Guild campaign.");
                var hero = SssTenV4Roster090.Get(heroId);
                var result = SssPreparedFamilyService090.CycleSlot(
                    _campaign, hero.HeroId, slotIndex);
                if (!result.IsSuccess)
                    return ApplyAndPersist(
                        result,
                        true,
                        "Gold Art family preparation failed.");
                var nextView = SssPreparedFamilyService090.View(
                    result.Value, hero.HeroId);
                var familyId = nextView.PreparedFamilyIds[slotIndex];
                return ApplyAndPersist(
                    result,
                    true,
                    hero.DisplayName + " prepared " +
                    SssPreparedFamilyService090.FamilyName(familyId) +
                    " in Gold slot " + (slotIndex + 1) + ".");
            }
            catch (Exception exception)
            {
                return M1CommandResult.Failure(exception.Message);
            }
        }

        public M1CommandResult ClaimSssSignatureWeapon090(string heroId)
        {
            try
            {
                if (_campaign?.Guild == null)
                    return M1CommandResult.Failure("No active Guild campaign.");
                var hero = SssTenV4Roster090.Get(heroId);
                var candidate = SssTenV4Inventory090.CreateSignatureWeapon(
                    _campaign, hero.HeroId);
                var audit = M2EquipmentPowerBudget087.AuditSssSignature090(
                    candidate);
                var liveOmegaResolved = audit.BudgetParityResolved &&
                                        audit.EffectHandlerReady;
                return ApplyAndPersist(
                    SssTenV4AcquisitionService090.ClaimSignatureWeapon090(
                        _campaign,
                        hero.HeroId,
                        liveOmegaResolved),
                    true,
                    hero.WeaponName +
                    " claimed to inventory. Open its slot to compare and equip it manually.");
            }
            catch (Exception exception)
            {
                return M1CommandResult.Failure(exception.Message);
            }
        }
    }
}
