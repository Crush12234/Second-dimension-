using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.M1;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// New Hero Master records only: authored primary weapon intent is recorded in
    /// canonical applicant flags. Existing saved records have no marker and retain
    /// their original deterministic recipe. This introduces no weapon family or Art.
    /// </summary>
    public static class HeroMasterPrimaryWeapon096
    {
        public const string PrimaryPrefix = "HERO_MASTER_PRIMARY_WEAPON096_";
        public const string FirearmFallback = "HERO_MASTER_FIREARM_FALLBACK096";

        public static string RequestedFamily(HeroMaster300Hero087 hero)
        {
            if (hero == null) return string.Empty;
            var weapon = (hero.Weapon ?? string.Empty).ToUpperInvariant();
            switch (hero.ArtTree1)
            {
                case "TREE_POLEARM":
                    return Has(weapon, "SPEAR", "LANCE", "POLEARM") ? "WEAPON_FAMILY_SPEAR_POLEARM" : "";
                case "TREE_LONGBOW":
                    return Has(weapon, "BOW") ? "WEAPON_FAMILY_BOW" : "";
                case "TREE_HEAVY_AXE":
                    return Has(weapon, "AXE") ? "WEAPON_FAMILY_AXE" : "";
                case "TREE_HAMMER":
                    return Has(weapon, "HAMMER", "MACE") ? "WEAPON_FAMILY_GREAT_WEAPON" : "";
                case "TREE_MARTIAL":
                    return Has(weapon, "GAUNTLET", "FIST", "CLAW", "BRAWL", "GREAVE", "KICK")
                        ? "WEAPON_FAMILY_GAUNTLET" : "";
                case "TREE_BLADE":
                    return Has(weapon, "SWORD", "BLADE") && !Has(weapon, "DAGGER")
                        ? "WEAPON_FAMILY_SWORD" : "";
                default:
                    // Dual blades, sword/shield and mystic focus have multiple
                    // legitimate existing family interpretations; do not guess.
                    return string.Empty;
            }
        }

        public static string ClassForNewRecord(HeroMaster300Hero087 hero, string legacyClass)
        {
            // Reviewed Ryka023 new applicants only: Elementalist / Flame focus
            // previously fell through the legacy keyword classifier to Warrior.
            // Keep that classifier unchanged so exact old Warrior records still
            // validate and regenerate their saved Sword Arts and equipment.
            if (hero?.StableId == "HERO_REC_023" && legacyClass == "CLASS_WARRIOR" &&
                hero.Role == "Elementalist" && hero.Weapon == "Flame focus" &&
                hero.ArtTree1 == "TREE_BLADE" && hero.ArtTree2 == "TREE_COMBAT_MASTERY")
                return "CLASS_MAGE";

            // Reviewed Thalion179 correction only: "Magic" hid his explicit spear.
            // Do not reclassify other authored mages whose valid Hybrid recipe
            // would change before the existing recipe-preservation check runs.
            return hero?.StableId == "HERO_REC_179" && legacyClass == "CLASS_MAGE" &&
                   RequestedFamily(hero) == "WEAPON_FAMILY_SPEAR_POLEARM"
                ? "CLASS_SPELLBLADE" : legacyClass;
        }

        public static IReadOnlyList<string> FlagsForNewRecord(HeroMaster300Hero087 hero,
            IReadOnlyList<string> previous, IReadOnlyDictionary<string, int> legacyAptitudes)
        {
            var result = new List<string>(previous ?? Array.Empty<string>());
            var family = RequestedFamily(hero);
            if (!string.IsNullOrEmpty(family)) result.Add(PrimaryPrefix + family);
            var weapon = (hero?.Weapon ?? string.Empty).ToUpperInvariant();
            if (hero?.ArtTree1 == "TREE_FIREARMS" &&
                Has(weapon, "CANNON", "RIFLE", "PISTOL", "LONG GUN") &&
                legacyAptitudes != null && legacyAptitudes.Count == 1 &&
                legacyAptitudes.TryGetValue("SWORD", out var sword) && sword == 80)
                result.Add(FirearmFallback);
            return result.AsReadOnly();
        }

        public static string RequestedFamily(OpeningRecruitRecord record, string previousFamily)
        {
            if (record?.SourceType != "PROCEDURAL" ||
                record.VariantFlags == null || !record.VariantFlags.Contains("HERO_MASTER_300") ||
                !(record.GenerationSeed ?? "").StartsWith("HERO_MASTER_300_", StringComparison.Ordinal))
                return string.Empty;
            // Preserve explicit existing hybrid builds, including Darius175's
            // committed-compatible Hybrid Relic recipe. Not every gun is remapped.
            if (previousFamily == "WEAPON_FAMILY_HYBRID_RELIC_WEAPON") return string.Empty;
            if (record.VariantFlags.Contains(FirearmFallback) &&
                previousFamily == "WEAPON_FAMILY_SWORD")
                return "WEAPON_FAMILY_ENGINEERING_TOOL";
            var flags = record.VariantFlags.Where(value =>
                value != null && value.StartsWith(PrimaryPrefix, StringComparison.Ordinal)).ToArray();
            return flags.Length == 1 ? flags[0].Substring(PrimaryPrefix.Length) : string.Empty;
        }

        static bool Has(string text, params string[] values) => values.Any(text.Contains);
    }
}
