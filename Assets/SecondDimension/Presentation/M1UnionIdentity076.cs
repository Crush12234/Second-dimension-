using System;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Converts legacy numbered opening-Union labels into stable story-role names
    /// at the presentation boundary. Canonical Union IDs and saved display names
    /// remain untouched, and any genuinely custom name always wins.
    /// </summary>
    public static class M1UnionIdentity076
    {
        public static string Resolve(string unionId, string displayName, int zeroBasedIndex)
        {
            if (!IsGenericOpeningName(displayName)) return displayName.Trim();
            return StoryNameForOrdinal(ResolveOrdinal(unionId, zeroBasedIndex));
        }

        private static string StoryNameForOrdinal(int ordinal)
        {
            switch (ordinal)
            {
                case 1: return "Skyhome Gatewardens";
                case 2: return "Lantern Spear";
                case 3: return "Wayfinders";
                case 4: return "Zorin's Vanguard";
                case 5: return "Wayglass Guard";
                case 6: return "Skyhome Rearguard";
                default: return "Guild Union";
            }
        }

        public static string ResolveTab(string unionId, string displayName, int zeroBasedIndex)
        {
            var ordinal = ResolveOrdinal(unionId, zeroBasedIndex);
            var resolved = Resolve(unionId, displayName, zeroBasedIndex);
            var canonicalStoryName = StoryNameForOrdinal(ordinal);
            var usesOpeningIdentity = IsGenericOpeningName(displayName) ||
                                      (!string.IsNullOrWhiteSpace(unionId) &&
                                       unionId.StartsWith("UNION_OPENING_", StringComparison.Ordinal) &&
                                       StringComparer.OrdinalIgnoreCase.Equals(
                                           resolved,
                                           canonicalStoryName));
            if (!usesOpeningIdentity) return resolved;
            switch (ordinal)
            {
                case 1: return "Gatewardens";
                case 2: return "Lantern Spear";
                case 3: return "Wayfinders";
                case 4: return "Vanguard";
                case 5: return "Wayglass";
                case 6: return "Rearguard";
                default: return resolved;
            }
        }

        public static string ResolveCanonicalOpeningUnion(string unionId, string displayName)
        {
            if (string.IsNullOrWhiteSpace(unionId) ||
                !unionId.StartsWith("UNION_OPENING_", StringComparison.Ordinal))
                return string.IsNullOrWhiteSpace(displayName) ? "Guild Union" : displayName.Trim();
            return Resolve(unionId, displayName, -1);
        }

        public static string FormationRole(string formationId)
        {
            switch (formationId ?? string.Empty)
            {
                case "FORMATION_SHIELD_WALL": return "HOLD  •  protects allies under pressure";
                case "FORMATION_WEDGE": return "BREAK  •  focuses force on one opening";
                case "FORMATION_SKIRMISH_LINE": return "MOVE  •  answers changing threats";
                case "FORMATION_ARCANE_CIRCLE": return "CHANNEL  •  steadies mystic and support Arts";
                case "FORMATION_CRESCENT": return "COUNTER  •  watches both flanks";
                case "FORMATION_RESCUE_COLUMN": return "RESCUE  •  protects recovery and extraction";
                case "FORMATION_ARROWHEAD": return "RUSH  •  drives through a decisive opening";
                case "FORMATION_VEILED_ECHELON": return "AMBUSH  •  conceals a tactical strike";
                default: return "ADAPT  •  follows the leader's field call";
            }
        }

        private static bool IsGenericOpeningName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return true;
            var value = displayName.Trim();
            return IsNumberedLabel(value, "Opening Union ") ||
                   IsNumberedLabel(value, "Union ");
        }

        private static bool IsNumberedLabel(string value, string prefix)
        {
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            var suffix = value.Substring(prefix.Length).Trim();
            return int.TryParse(suffix, out var ordinal) && ordinal > 0;
        }

        private static int ResolveOrdinal(string unionId, int zeroBasedIndex)
        {
            if (!string.IsNullOrWhiteSpace(unionId))
            {
                var cursor = unionId.Length - 1;
                while (cursor >= 0 && char.IsDigit(unionId[cursor])) cursor--;
                if (cursor < unionId.Length - 1 &&
                    int.TryParse(unionId.Substring(cursor + 1), out var parsed) && parsed > 0)
                    return parsed;
            }
            return zeroBasedIndex >= 0 ? zeroBasedIndex + 1 : 0;
        }
    }
}
