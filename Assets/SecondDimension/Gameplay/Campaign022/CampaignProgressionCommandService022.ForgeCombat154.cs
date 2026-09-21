using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    // Reuse the existing quality-to-combat policy. No new price or damage multiplier.
    public static class WeaponEvolutionCombat154
    {
        public static MaterialCostDto022[] MaterialCosts159(CampaignState campaign, MaterialCostDto022[] costs) =>
            (costs ?? Array.Empty<MaterialCostDto022>()).Select(cost => new MaterialCostDto022
            {
                materialId = cost.materialId,
                amount = checked((int)TownProgression159.Discount(campaign, 2, cost.amount))
            }).ToArray();

        public static int TierIndex(string qualityOrTier)
        {
            var key = (qualityOrTier ?? string.Empty).Trim().ToUpperInvariant();
            if (key.StartsWith("QUALITY_", StringComparison.Ordinal)) key = key.Substring(8);
            switch (key)
            {
                case "": case "TRAINING": case "STARTER": return 0;
                case "COMMON": return 1;
                case "UNCOMMON": return 2;
                case "RARE": return 3;
                case "EPIC": return 4;
                case "LEGENDARY": return 5;
                case "GODLY": return 6;
                // Existing bespoke weapons keep their identity, effects and budget.
                case "SSS_SIGNATURE": case "CREATOR_OMEGA": return 7;
                default: return -1;
            }
        }

        // Existing drops/commissions can begin above their legacy TRAINING
        // history. Read the legitimate owned quality as a baseline, never as a
        // newly granted upgrade. No history, receipt, mastery or item is changed
        // here. The next actual paid recipe writes only its real final tier.
        public static string EffectiveRecipeTier154(EquipmentEvolutionState022 history,
            EquipmentItemState owned)
        {
            if (history == null || owned == null || history.ItemInstanceId != owned.InstanceId ||
                owned.InventoryOnly || !owned.CanEquipIn(EquipmentSlotIds.MainHand)) return null;
            int saved = TierIndex(history.TierId), actual = TierIndex(owned.QualityId);
            if (saved < 0 || actual < 0) return null;
            int effective = Math.Max(saved, actual);
            // GODLY and bespoke SSS/Creator equipment has no higher native recipe.
            if (effective >= 6) return null;
            return new[] { "TRAINING", "COMMON", "UNCOMMON", "RARE", "EPIC", "LEGENDARY" }[effective];
        }

        public static Result<EquipmentItemState> Project(EquipmentItemState item, string targetTier)
        {
            if (item == null || item.InventoryOnly || !item.CanEquipIn(EquipmentSlotIds.MainHand))
                return Result<EquipmentItemState>.Failure("FORGE154_OWNED_WEAPON_REQUIRED");
            if (string.IsNullOrWhiteSpace(targetTier) || targetTier.StartsWith("QUALITY_", StringComparison.OrdinalIgnoreCase))
                return Result<EquipmentItemState>.Failure("FORGE154_NATIVE_QUALITY_BINDING_REQUIRED");
            int from = TierIndex(item.QualityId), to = TierIndex(targetTier);
            if (from < 0 || to < 1 || to > 6)
                return Result<EquipmentItemState>.Failure("FORGE154_NATIVE_QUALITY_BINDING_REQUIRED");
            if (to <= from)
                return Result<EquipmentItemState>.Failure("This weapon already has this quality or better. No materials were spent.");
            var tags = item.EquipmentTags.ToList();
            if (!tags.Contains("MODIFIED_WEAPON")) tags.Add("MODIFIED_WEAPON");
            tags.Sort(StringComparer.Ordinal);
            var promoted = new EquipmentItemState(item.InstanceId, item.DefinitionId, item.DisplayName,
                item.ValidSlotIds, tags.AsReadOnly(), "QUALITY_" + targetTier.Trim().ToUpperInvariant(),
                item.ConditionBasisPoints, item.PlayerLocked, item.InventoryOnly);
            var before = M2EquipmentPowerPolicy087.Resolve(item);
            var after = M2EquipmentPowerPolicy087.Resolve(promoted);
            if (after.PhysicalAttack < before.PhysicalAttack || after.MysticAttack < before.MysticAttack ||
                (after.PhysicalAttack == before.PhysicalAttack && after.MysticAttack == before.MysticAttack))
                return Result<EquipmentItemState>.Failure("This recipe has no verified combat improvement. No materials were spent.");
            return Result<EquipmentItemState>.Success(promoted);
        }

        public static EquipmentItemState FindUnique(CampaignState state, string itemId)
        {
            var owned = state.Guild.Inventory.Concat(state.Guild.Recruits.SelectMany(r =>
                r.Equipment.Assignments.Select(a => a.Item))).Where(i => i.InstanceId == itemId).ToArray();
            if (owned.Length != 1) throw new InvalidOperationException("FORGE154_OWNERSHIP_MISSING_OR_DUPLICATED");
            return owned[0];
        }

        internal static CampaignState ReplaceSameOwnedInstance(CampaignState state, EquipmentItemState replacement)
        {
            FindUnique(state, replacement.InstanceId);
            var inventory = state.Guild.Inventory.Select(i => i.InstanceId == replacement.InstanceId ? replacement : i).ToArray();
            var recruits = state.Guild.Recruits.Select(r =>
            {
                if (!r.Equipment.Assignments.Any(a => a.Item.InstanceId == replacement.InstanceId)) return r;
                if (!ProtectedActorPolicy.CanUseNormalEquipment(r))
                    throw new InvalidOperationException("FORGE154_PROTECTED_EQUIPMENT_FORBIDDEN");
                var assignments = r.Equipment.Assignments.Select(a => a.Item.InstanceId == replacement.InstanceId
                    ? new EquipmentSlotAssignmentState(a.SlotId, replacement) : a).ToArray();
                var changed = r.WithEquipment(new EquipmentLoadoutState(assignments));
                // Match the actual native player attack projection BEFORE committing an
                // upgraded equipped weapon. Any future wider numeric migration is separate.
                var power = M2EquipmentPowerPolicy087.Resolve(changed.Equipment);
                checked
                {
                    int physical = 18 + changed.PotentialBasisPoints / 500 + changed.TacticalAptitude / 8 +
                        changed.Progression.StrengthBonus + changed.Progression.AgilityBonus / 2 + power.PhysicalAttack;
                    int mystic = 12 + changed.MaximumMp / 4 + changed.PotentialBasisPoints / 750 +
                        changed.Progression.MagicBonus + changed.Progression.WillBonus / 2 + power.MysticAttack;
                    if (physical <= 0 || mystic <= 0) throw new OverflowException("FORGE154_BATTLE_POWER_RANGE");
                }
                return changed;
            }).ToArray();
            return state.With(state.Guild.With(state.Guild.TreasuryXp, recruits,
                state.Guild.Unions, inventory), state.OpeningFlow);
        }
    }

    public sealed partial class CampaignProgressionCommandService022
    {
        // The existing EvolveWeapon body delegates here. Existing UI/coordinator signature
        // and one-save boundary remain unchanged. Show explicit confirmation for a locked
        // weapon; its lock, identity and slot are preserved, never removed or moved.
        public Result<CampaignState> EvolveWeaponWithCombat154(CampaignState campaign,
            ICampaignRegistry022 registry, string itemInstanceId, string recipeId)
        {
            try
            {
                if (!TryContext(campaign, out var city, out var strategic, out var progress,
                    out var playable, out var state, out var error) || registry == null)
                    return Result<CampaignState>.Failure(error ?? "CAMPAIGN022_INPUT_REQUIRED");
                if (!registry.WeaponRecipes.TryGetValue(recipeId, out var recipe))
                    return Result<CampaignState>.Failure("CAMPAIGN022_RECIPE_UNKNOWN");
                var list = state.EquipmentEvolution.ToList();
                var matches = list.Count(x => x.ItemInstanceId == itemInstanceId);
                if (matches != 1) return Result<CampaignState>.Failure("CAMPAIGN022_ITEM_USE_HISTORY_REQUIRED");
                int index = list.FindIndex(x => x.ItemInstanceId == itemInstanceId);
                var history = list[index];
                if (history.TrackId != recipe.trackId || !registry.WeaponTracks.TryGetValue(history.TrackId, out var track) ||
                    track.weaponFamilyId != recipe.weaponFamilyId)
                    return Result<CampaignState>.Failure("CAMPAIGN022_RECIPE_TIER_ILLEGAL");
                // Previously applied receipts resolve before current ownership/gates/costs.
                // Legacy metadata-only grants use the separate reconciliation method below.
                if (history.AppliedRecipeIds.Contains(recipeId)) return Result<CampaignState>.Success(campaign);
                var blocked159 = IndependentProgression159.BlockReason(campaign);
                if (blocked159 != null) return Result<CampaignState>.Failure(blocked159);
                var owned = WeaponEvolutionCombat154.FindUnique(campaign, itemInstanceId);
                var effectiveTier = WeaponEvolutionCombat154.EffectiveRecipeTier154(history, owned);
                if (effectiveTier == null || recipe.fromTier != effectiveTier)
                    return Result<CampaignState>.Failure("CAMPAIGN022_RECIPE_TIER_ILLEGAL");
                // The native city building stops at four, while the existing
                // recipe ladder needs six. Paid Town levels are the same Forge
                // authority and must unlock their advertised recipe level.
                if (TownProgression159.Level(campaign, 2) < recipe.forgeLevelRequired)
                    return Result<CampaignState>.Failure("CAMPAIGN022_FORGE_LEVEL_REQUIRED");
                if (history.MasteryPoints < recipe.requiredMasteryPoints || history.MeaningfulUses < recipe.requiredMeaningfulUses)
                    return Result<CampaignState>.Failure("CAMPAIGN022_MEANINGFUL_HISTORY_INSUFFICIENT");
                if (recipe.godlyRequiresStoryOrAbyssGate && !state.AboveGroundChangeIds.Contains("CITY_CHANGE_AEGIS_COVENANT_DESK"))
                    return Result<CampaignState>.Failure("CAMPAIGN022_GODLY_GATE_REQUIRED");
                var projection = WeaponEvolutionCombat154.Project(owned, recipe.toTier);
                if (!projection.IsSuccess) return Result<CampaignState>.Failure(projection.Errors.ToArray());
                // Reject malformed/duplicate authored costs before existing native spending.
                var costs = recipe.materialCosts ?? Array.Empty<MaterialCostDto022>();
                if (costs.Any(c => c == null || string.IsNullOrWhiteSpace(c.materialId) || c.amount < 0) ||
                    costs.Select(c => c.materialId).Distinct(StringComparer.Ordinal).Count() != costs.Length)
                    return Result<CampaignState>.Failure("FORGE154_INVALID_AUTHORED_MATERIAL_COST");
                var effectiveCosts159 = WeaponEvolutionCombat154.MaterialCosts159(campaign, costs);
                if (!TrySpendForgeMaterials154(playable.WorldMaterials, city.Materials, effectiveCosts159,
                    out var materials, out var cityMaterials))
                    return Result<CampaignState>.Failure("CAMPAIGN022_MATERIALS_INSUFFICIENT");
                var changed = WeaponEvolutionCombat154.ReplaceSameOwnedInstance(campaign, projection.Value);
                list[index] = history.With(tierId: recipe.toTier,
                    appliedRecipeIds: history.AppliedRecipeIds.Concat(new[] { recipeId }).ToArray());
                var next = state.With(equipmentEvolution: list, lastCheckpointId: "campaign022_weapon_evolved_with_combat154");
                return Success(changed, city.With(materials: cityMaterials), strategic, progress,
                    playable.With(worldMaterials: materials, progression022: next, replaceProgression022: true,
                        lastCheckpointId: next.LastCheckpointId), next);
            }
            catch (Exception e) when (e is InvalidOperationException || e is ArgumentException || e is OverflowException)
            { return Result<CampaignState>.Failure("FORGE154_EVOLUTION_REJECTED: " + e.Message); }
        }

        // These are two existing authoritative reward stores, not duplicate mirrors:
        // WorldGate023.ApplyReceipt awards City.Materials; Playable020.ApplyReceipt awards
        // WorldMaterials under separate native receipts. Consume from the legacy Forge
        // pool first, then the actual City balance. Never copy or grant either balance.
        static bool TrySpendForgeMaterials154(IReadOnlyList<WorldMaterialAmount020> world,
            IReadOnlyList<GuildMaterialState017D> city, IReadOnlyList<MaterialCostDto022> costs,
            out IReadOnlyList<WorldMaterialAmount020> nextWorld,
            out IReadOnlyList<GuildMaterialState017D> nextCity)
        {
            nextWorld = world; nextCity = city;
            var a = world.ToDictionary(x => x.MaterialId, x => x.Amount, StringComparer.Ordinal);
            var b = city.ToDictionary(x => x.MaterialId, x => x.Amount, StringComparer.Ordinal);
            foreach (var cost in costs)
            {
                a.TryGetValue(cost.materialId, out var aw); b.TryGetValue(cost.materialId, out var bc);
                if ((long)aw + bc < cost.amount) return false;
            }
            foreach (var cost in costs)
            {
                if (cost.amount == 0) continue;
                a.TryGetValue(cost.materialId, out var aw); b.TryGetValue(cost.materialId, out var bc);
                int fromWorld = Math.Min(aw, cost.amount);
                int fromCity = cost.amount - fromWorld;
                if (fromWorld > 0) a[cost.materialId] = aw - fromWorld;
                if (fromCity > 0) b[cost.materialId] = bc - fromCity;
            }
            nextWorld = a.Where(x => x.Value > 0).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new WorldMaterialAmount020(x.Key, x.Value)).ToArray();
            nextCity = b.Where(x => x.Value > 0).OrderBy(x => x.Key, StringComparer.Ordinal)
                .Select(x => new GuildMaterialState017D(x.Key, x.Value)).ToArray();
            return true;
        }

        // Explicit safe-boundary repair for paid historical records. Never re-spend or
        // silently alter already committed battles. Existing higher-quality drops stay.
        public Result<CampaignState> ReconcileEarnedWeaponQuality154(CampaignState campaign,
            ICampaignRegistry022 registry, string itemInstanceId)
        {
            try
            {
                if (campaign == null || registry == null ||
                    IndependentProgression159.BlockReason(campaign) != null)
                    return Result<CampaignState>.Failure("FORGE154_SAFE_GUILD_BOUNDARY_REQUIRED");
                var state = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
                var history = state.EquipmentEvolution.SingleOrDefault(x => x.ItemInstanceId == itemInstanceId);
                if (history == null || history.AppliedRecipeIds.Count == 0)
                    return Result<CampaignState>.Failure("FORGE154_EARNED_EVOLUTION_RECEIPT_REQUIRED");
                // Require the saved final tier to be supported by an actual applied native
                // recipe for the same track. Do not trust a loose level/tag by itself.
                bool earned = history.AppliedRecipeIds.Any(id => registry.WeaponRecipes.TryGetValue(id, out var r) &&
                    r.trackId == history.TrackId && r.toTier == history.TierId);
                if (!earned) return Result<CampaignState>.Failure("FORGE154_HISTORY_RECONCILIATION_REQUIRED");
                var item = WeaponEvolutionCombat154.FindUnique(campaign, itemInstanceId);
                int current = WeaponEvolutionCombat154.TierIndex(item.QualityId), target = WeaponEvolutionCombat154.TierIndex(history.TierId);
                if (current >= target && current >= 0) return Result<CampaignState>.Success(campaign);
                var projection = WeaponEvolutionCombat154.Project(item, history.TierId);
                return projection.IsSuccess
                    ? Result<CampaignState>.Success(WeaponEvolutionCombat154.ReplaceSameOwnedInstance(campaign, projection.Value))
                    : Result<CampaignState>.Failure(projection.Errors.ToArray());
            }
            catch (Exception e) when (e is InvalidOperationException || e is ArgumentException || e is OverflowException)
            { return Result<CampaignState>.Failure("FORGE154_RECONCILIATION_REJECTED: " + e.Message); }
        }
    }
}
