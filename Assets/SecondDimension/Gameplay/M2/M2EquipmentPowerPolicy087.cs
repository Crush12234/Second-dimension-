using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Small, deterministic combat-power gains for progression equipment. Starting
    /// loadouts deliberately remain at the certified 084 values; only earned 070
    /// loot and manually equipped Creator weapons use this additive policy.
    /// </summary>
    public static class M2EquipmentPowerPolicy087
    {
        public const string VersionId = "M2_EQUIPMENT_POWER_087";
        private const string LootInstancePrefix = "LOOT_ITEM_070_";

        public static M2EquipmentPowerBonus087 Resolve(EquipmentItemState item)
        {
            if (item == null || !IsProgressionEquipment(item))
                return M2EquipmentPowerBonus087.None;

            var quality = QualityPower(item.QualityId);
            var physical = quality;
            var mystic = Math.Max(0, quality - 1);
            if (HasTag(item.EquipmentTags, "WF02_GREAT_WEAPON") ||
                HasTag(item.EquipmentTags, "GREAT_AXE") ||
                HasTag(item.EquipmentTags, "GREAT_WEAPON"))
            {
                physical += 5;
                mystic = Math.Max(0, quality - 3);
            }
            else if (HasTag(item.EquipmentTags, "WF03_AXE") ||
                     HasTag(item.EquipmentTags, "AXE"))
            {
                physical += 4;
                mystic = Math.Max(0, quality - 3);
            }
            else if (HasTag(item.EquipmentTags, "WF01_SWORD") ||
                     HasTag(item.EquipmentTags, "SWORD"))
            {
                physical += 3;
                mystic = Math.Max(0, quality - 2);
            }
            else if (HasTag(item.EquipmentTags, "WF04_SPEAR_POLEARM") ||
                     HasTag(item.EquipmentTags, "POLEARM") ||
                     HasTag(item.EquipmentTags, "SPEAR"))
            {
                physical += 3;
                mystic += 1;
            }
            else if (HasTag(item.EquipmentTags, "WF05_BOW") ||
                     HasTag(item.EquipmentTags, "BOW"))
            {
                physical += 3;
                mystic += 1;
            }
            else if (HasTag(item.EquipmentTags, "WF06_DAGGER") ||
                     HasTag(item.EquipmentTags, "DAGGER"))
            {
                physical += 2;
                mystic += 1;
            }
            else if (HasTag(item.EquipmentTags, "WF08_GAUNTLET") ||
                     HasTag(item.EquipmentTags, "GAUNTLET"))
            {
                physical += 4;
                mystic = Math.Max(0, quality - 2);
            }
            else if (HasTag(item.EquipmentTags, "WF09_STAFF") ||
                     HasTag(item.EquipmentTags, "STAFF"))
            {
                physical = Math.Max(1, quality - 2);
                mystic = quality + 4;
            }
            else if (HasTag(item.EquipmentTags, "WF10_CATALYST_FOCUS") ||
                     HasTag(item.EquipmentTags, "CATALYST") ||
                     HasTag(item.EquipmentTags, "FOCUS"))
            {
                physical = Math.Max(0, quality - 3);
                mystic = quality + 5;
            }
            else if (HasTag(item.EquipmentTags, "WF11_ENGINEERING_TOOL") ||
                     HasTag(item.EquipmentTags, "ENGINEERING_TOOL"))
            {
                physical += 2;
                mystic += 3;
            }
            else if (HasTag(item.EquipmentTags, "WF12_HYBRID_RELIC") ||
                     HasTag(item.EquipmentTags, "RELIC"))
            {
                physical += 3;
                mystic += 4;
            }
            else if (HasTag(item.EquipmentTags, "WF07_SHIELD") ||
                     HasTag(item.EquipmentTags, "SHIELD"))
            {
                physical += 1;
                mystic += 1;
            }

            return new M2EquipmentPowerBonus087(physical, mystic);
        }

        public static M2EquipmentPowerBonus087 Resolve(EquipmentLoadoutState loadout)
        {
            var physical = 0;
            var mystic = 0;
            var assignments = loadout?.Assignments ??
                              Array.Empty<EquipmentSlotAssignmentState>();
            for (var index = 0; index < assignments.Count; index++)
            {
                var bonus = Resolve(assignments[index]?.Item);
                physical = checked(physical + bonus.PhysicalAttack);
                mystic = checked(mystic + bonus.MysticAttack);
            }
            return new M2EquipmentPowerBonus087(physical, mystic);
        }

        public static bool IsProgressionEquipment(EquipmentItemState item) =>
            item != null &&
            ((!string.IsNullOrWhiteSpace(item.InstanceId) &&
              item.InstanceId.StartsWith(LootInstancePrefix, StringComparison.Ordinal)) ||
             HasTag(item.EquipmentTags, "CREATOR_GIVEAWAY") ||
             HasTag(item.EquipmentTags, "MODIFIED_WEAPON"));

        private static int QualityPower(string qualityId)
        {
            switch ((qualityId ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "QUALITY_STARTER": return 1;
                case "QUALITY_COMMON": return 2;
                case "QUALITY_UNCOMMON": return 3;
                case "QUALITY_RARE": return 4;
                case "QUALITY_EPIC": return 6;
                case "QUALITY_LEGENDARY": return 8;
                case "QUALITY_GODLY": return 12;
                // Creator Omega is the live comparison ceiling. SSS signatures
                // reserve four total attack points for their bounded bespoke effect.
                case "CREATOR_OMEGA":
                case "QUALITY_CREATOR_OMEGA": return 16;
                case "SSS_SIGNATURE":
                case "QUALITY_SSS_SIGNATURE": return 14;
                default: return 2;
            }
        }

        private static bool HasTag(IReadOnlyList<string> tags, string expected)
        {
            if (tags == null) return false;
            for (var index = 0; index < tags.Count; index++)
                if (StringComparer.Ordinal.Equals(tags[index], expected)) return true;
            return false;
        }
    }

    public sealed class M2EquipmentPowerBonus087
    {
        public static readonly M2EquipmentPowerBonus087 None =
            new M2EquipmentPowerBonus087(0, 0);

        public M2EquipmentPowerBonus087(int physicalAttack, int mysticAttack)
        {
            PhysicalAttack = Math.Max(0, physicalAttack);
            MysticAttack = Math.Max(0, mysticAttack);
        }

        public int PhysicalAttack { get; }
        public int MysticAttack { get; }
    }

    public sealed class M2EquipmentPowerBudgetAudit087
    {
        public string SignatureItemId { get; internal set; }
        public string ReferenceItemId { get; internal set; }
        public string WeaponFamilyId { get; internal set; }
        public int SignatureStatPoints { get; internal set; }
        public int ReservedEffectPoints { get; internal set; }
        public int SignatureTotalBudget { get; internal set; }
        public int OmegaReferenceStatPoints { get; internal set; }
        public bool BudgetParityResolved { get; internal set; }
        public bool EffectHandlerReady { get; internal set; }
        public string EffectReadiness { get; internal set; }
    }

    /// <summary>
    /// Executable budget proof for V4 signature weapons. Handler readiness is
    /// carried explicitly on each live inventory item so UI and tests never infer
    /// a shipped battle effect from the weapon's name or rarity.
    /// </summary>
    public static class M2EquipmentPowerBudget087
    {
        private const int ReservedSignatureEffectPoints = 4;

        public static M2EquipmentPowerBudgetAudit087 AuditSssSignature090(
            EquipmentItemState signatureItem)
        {
            if (signatureItem == null)
                throw new ArgumentNullException(nameof(signatureItem));
            var family = FindFamily(signatureItem.EquipmentTags);
            if (string.IsNullOrWhiteSpace(family) ||
                !Has(signatureItem.EquipmentTags, "SSS_SIGNATURE"))
                throw new ArgumentException(
                    "A tagged SSS signature weapon and canonical WF family are required.",
                    nameof(signatureItem));
            var slot = StringComparer.Ordinal.Equals(family, "WF07_SHIELD")
                ? EquipmentSlotIds.OffHand
                : EquipmentSlotIds.MainHand;
            var omegaDefinitionId = OmegaReferenceDefinitionId090(family);
            var omega = new EquipmentItemState(
                "OMEGA_REFERENCE_" + family,
                omegaDefinitionId,
                "Live Omega " + family,
                new[] { slot },
                new[] { family, "CREATOR_GIVEAWAY", "CREATOR_OMEGA" },
                "QUALITY_CREATOR_OMEGA",
                10000,
                false);
            var signature = M2EquipmentPowerPolicy087.Resolve(signatureItem);
            var reference = M2EquipmentPowerPolicy087.Resolve(omega);
            var signatureStats = checked(signature.PhysicalAttack + signature.MysticAttack);
            var omegaStats = checked(reference.PhysicalAttack + reference.MysticAttack);
            var total = checked(signatureStats + ReservedSignatureEffectPoints);
            var ready = Has(signatureItem.EquipmentTags, "SSS_EFFECT_HANDLER_READY");
            return new M2EquipmentPowerBudgetAudit087
            {
                SignatureItemId = signatureItem.DefinitionId,
                ReferenceItemId = omega.DefinitionId,
                WeaponFamilyId = family,
                SignatureStatPoints = signatureStats,
                ReservedEffectPoints = ReservedSignatureEffectPoints,
                SignatureTotalBudget = total,
                OmegaReferenceStatPoints = omegaStats,
                BudgetParityResolved = total == omegaStats,
                EffectHandlerReady = ready,
                EffectReadiness = ready
                    ? "Signature effect adapter active within its reserved budget."
                    : "Omega stat parity resolved; four effect points are reserved, but the bespoke battle handler is pending."
            };
        }

        private static string OmegaReferenceDefinitionId090(string family)
        {
            // These are the exact primary/template IDs in the live Creator 10,000
            // reward catalog. Keep the audit closed over that catalog instead of
            // silently inventing a reference for an unknown WF tag.
            switch (family)
            {
                case "WF01_SWORD": return "OMEGA_WF01_SWORD";
                case "WF02_GREAT_WEAPON": return "OMEGA_WF02_GREAT_WEAPON";
                case "WF03_AXE": return "OMEGA_WF03_AXE";
                case "WF04_SPEAR_POLEARM": return "OMEGA_WF04_SPEAR_POLEARM";
                case "WF05_BOW": return "OMEGA_WF05_BOW";
                case "WF06_DAGGER": return "OMEGA_WF06_DAGGER";
                case "WF07_SHIELD": return "OMEGA_WF07_SHIELD";
                case "WF08_GAUNTLET": return "OMEGA_WF08_GAUNTLET";
                case "WF09_STAFF": return "OMEGA_WF09_STAFF";
                case "WF10_CATALYST_FOCUS": return "OMEGA_WF10_CATALYST_FOCUS";
                case "WF11_ENGINEERING_TOOL": return "OMEGA_WF11_ENGINEERING_TOOL";
                case "WF12_HYBRID_RELIC": return "OMEGA_WF12_HYBRID_RELIC";
                default:
                    throw new ArgumentException(
                        "No live Creator Omega reference exists for weapon family " + family + ".",
                        nameof(family));
            }
        }

        private static string FindFamily(IReadOnlyList<string> tags)
        {
            if (tags == null) return string.Empty;
            for (var index = 0; index < tags.Count; index++)
                if (!string.IsNullOrWhiteSpace(tags[index]) &&
                    tags[index].StartsWith("WF", StringComparison.Ordinal) &&
                    tags[index].Length >= 4 &&
                    char.IsDigit(tags[index][2]) && char.IsDigit(tags[index][3]))
                    return tags[index];
            return string.Empty;
        }

        private static bool Has(IReadOnlyList<string> tags, string expected)
        {
            if (tags == null) return false;
            for (var index = 0; index < tags.Count; index++)
                if (StringComparer.Ordinal.Equals(tags[index], expected)) return true;
            return false;
        }
    }
}
