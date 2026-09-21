using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only equipment contract for the shared combat rig. It reads
    /// authoritative equipment tags but never equips, removes, evolves, consumes,
    /// or otherwise mutates an item. The same profile can drive proxy sockets now
    /// and authored meshes/Animator layers later.
    /// </summary>
    public sealed class M2EquipmentVisualProfile018
    {
        public M2EquipmentVisualProfile018(
            string weaponFamilyId,
            string animatorSetId,
            string armorFamilyId,
            string armorMeshSetId,
            string mainHandSocketId,
            string offHandSocketId,
            string bodyArmorSocketId,
            bool usesOffHand,
            string playerFacingSummary)
        {
            WeaponFamilyId = weaponFamilyId ?? string.Empty;
            AnimatorSetId = animatorSetId ?? string.Empty;
            ArmorFamilyId = armorFamilyId ?? string.Empty;
            ArmorMeshSetId = armorMeshSetId ?? string.Empty;
            MainHandSocketId = mainHandSocketId ?? string.Empty;
            OffHandSocketId = offHandSocketId ?? string.Empty;
            BodyArmorSocketId = bodyArmorSocketId ?? string.Empty;
            UsesOffHand = usesOffHand;
            PlayerFacingSummary = playerFacingSummary ?? string.Empty;
        }

        public string WeaponFamilyId { get; }
        public string AnimatorSetId { get; }
        public string ArmorFamilyId { get; }
        public string ArmorMeshSetId { get; }
        public string MainHandSocketId { get; }
        public string OffHandSocketId { get; }
        public string BodyArmorSocketId { get; }
        public bool UsesOffHand { get; }
        public string PlayerFacingSummary { get; }
    }

    public static class M2EquipmentVisualPolicy018
    {
        public const string RightHandSocket = "SOCKET_HAND_R";
        public const string LeftHandSocket = "SOCKET_HAND_L";
        public const string BodyArmorSocket = "SOCKET_BODY_ARMOR";

        public static M2EquipmentVisualProfile018 Resolve(IReadOnlyList<string> equipmentTags)
        {
            var shield = Has(equipmentTags, "SHIELD");
            if (Has(equipmentTags, "BOW") || Has(equipmentTags, "LONGBOW") ||
                Has(equipmentTags, "SHORTBOW") || Has(equipmentTags, "RANGED"))
                return Profile(equipmentTags, "RANGED", "ANIMSET_RANGED", false, "Bow / ranged weapon");
            if (Has(equipmentTags, "SPEAR") || Has(equipmentTags, "POLEARM"))
                return Profile(equipmentTags, "POLEARM", "ANIMSET_POLEARM", shield, "Spear / polearm");
            if (Has(equipmentTags, "GREAT_AXE") || Has(equipmentTags, "HAMMER") ||
                Has(equipmentTags, "HEAVY_WEAPON") || Has(equipmentTags, "TWO_HANDED"))
                return Profile(equipmentTags, "HEAVY", "ANIMSET_HEAVY", false, "Heavy weapon");
            if (Has(equipmentTags, "SWORD") || Has(equipmentTags, "BLADE"))
                return Profile(equipmentTags, shield ? "SWORD_SHIELD" : "ONE_HANDED",
                    shield ? "ANIMSET_SWORD_SHIELD" : "ANIMSET_ONE_HANDED",
                    shield, shield ? "Sword and shield" : "One-handed weapon");
            if (Has(equipmentTags, "AXE"))
                return Profile(equipmentTags, shield ? "AXE_SHIELD" : "ONE_HANDED_AXE",
                    shield ? "ANIMSET_AXE_SHIELD" : "ANIMSET_ONE_HANDED_AXE",
                    shield, shield ? "Axe and shield" : "One-handed axe");
            if (Has(equipmentTags, "DAGGER"))
                return Profile(equipmentTags, "DAGGER", "ANIMSET_DAGGER", shield, "Dagger / precision weapon");
            if (Has(equipmentTags, "GAUNTLET"))
                return Profile(equipmentTags, "GAUNTLET", "ANIMSET_GAUNTLET", false, "Gauntlet / close combat");
            if (Has(equipmentTags, "STAFF"))
                return Profile(equipmentTags, "STAFF_MYSTIC", "ANIMSET_STAFF_MYSTIC", false, "Staff / mystic focus");
            if (Has(equipmentTags, "WAND") || Has(equipmentTags, "FOCUS") ||
                Has(equipmentTags, "FOCUS_TOOL") || Has(equipmentTags, "ARCANE_CONDUCTOR"))
                return Profile(equipmentTags, "FOCUS_MYSTIC", "ANIMSET_FOCUS_MYSTIC", false, "Wand / mystic focus");
            if (Has(equipmentTags, "TOOL") || Has(equipmentTags, "SIGNAL_HORN") || Has(equipmentTags, "ALCHEMY"))
                return Profile(equipmentTags, "UTILITY", "ANIMSET_UTILITY", shield, "Field tool / support focus");
            if (Has(equipmentTags, "WEAPON"))
                return Profile(equipmentTags, shield ? "ONE_HANDED_SHIELD" : "ONE_HANDED",
                    shield ? "ANIMSET_ONE_HANDED_SHIELD" : "ANIMSET_ONE_HANDED", shield,
                    shield ? "One-handed weapon and shield" : "One-handed weapon");
            return Profile(equipmentTags, shield ? "SHIELD_ONLY" : "UNARMED",
                shield ? "ANIMSET_SHIELD" : "ANIMSET_UNARMED",
                shield, shield ? "Shield specialist" : "Unarmed / utility");
        }

        public static string ArtsAccessSummary(IReadOnlyList<string> equipmentTags)
        {
            var profile = Resolve(equipmentTags);
            switch (profile.WeaponFamilyId)
            {
                case "STAFF_MYSTIC": return "Mystic, restoration, and staff Arts may enter Union forecasts.";
                case "RANGED": return "Ranged and tactical Arts may enter Union forecasts.";
                case "POLEARM": return "Spear and formation-pressure Arts may enter Union forecasts.";
                case "HEAVY": return "Heavy martial and break Arts may enter Union forecasts.";
                case "SWORD_SHIELD": return "Blade, guard, interception, and shield Arts may enter Union forecasts.";
                case "ONE_HANDED": return "One-handed martial Arts may enter Union forecasts.";
                case "AXE_SHIELD": return "Axe, guard, interception, and shield Arts may enter Union forecasts.";
                case "ONE_HANDED_AXE": return "Axe and break Arts may enter Union forecasts.";
                case "DAGGER": return "Precision, tactical, and utility Arts may enter Union forecasts.";
                case "GAUNTLET": return "Close-combat and focus Arts may enter Union forecasts.";
                case "FOCUS_MYSTIC": return "Mystic, restoration, and focus Arts may enter Union forecasts.";
                case "UTILITY": return "Support, remedy, and utility Arts may enter Union forecasts.";
                case "SHIELD_ONLY": return "Guard and shield-support Arts may enter Union forecasts.";
                default: return "Universal and utility Arts may enter Union forecasts.";
            }
        }

        private static M2EquipmentVisualProfile018 Profile(
            IReadOnlyList<string> equipmentTags,
            string family,
            string animator,
            bool offHand,
            string summary)
        {
            var armorFamily = ArmorFamily(equipmentTags);
            return new M2EquipmentVisualProfile018(
                family,
                animator,
                armorFamily,
                "ARMORSET_" + armorFamily,
                RightHandSocket,
                offHand ? LeftHandSocket : string.Empty,
                BodyArmorSocket,
                offHand,
                summary + " · " + ArmorSummary(armorFamily));
        }

        private static string ArmorFamily(IReadOnlyList<string> tags)
        {
            if (Has(tags, "ARMOR_HEAVY")) return "HEAVY";
            if (Has(tags, "ARMOR_MEDIUM")) return "MEDIUM";
            if (Has(tags, "ARMOR_LIGHT")) return "LIGHT";
            // Opening procedural loadouts predate the catalog prefixes and use
            // LIGHT/MEDIUM/HEAVY. Keep those saved loadouts visually compatible.
            if (Has(tags, "ARMOR") && Has(tags, "HEAVY")) return "HEAVY";
            if (Has(tags, "ARMOR") && Has(tags, "MEDIUM")) return "MEDIUM";
            if (Has(tags, "ARMOR") && Has(tags, "LIGHT")) return "LIGHT";
            return "UNSPECIFIED";
        }

        private static string ArmorSummary(string armorFamily)
        {
            switch (armorFamily)
            {
                case "HEAVY": return "heavy armor";
                case "MEDIUM": return "medium armor";
                case "LIGHT": return "light armor";
                default: return "armor pending visual assignment";
            }
        }

        private static bool Has(IReadOnlyList<string> tags, string expected)
        {
            if (tags == null) return false;
            for (var index = 0; index < tags.Count; index++)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(tags[index], expected)) return true;
            }
            return false;
        }
    }
}
