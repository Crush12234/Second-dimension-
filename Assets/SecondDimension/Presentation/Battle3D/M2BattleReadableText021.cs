using System;
using System.Globalization;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Player-facing English labels used by both world-space battle text and its tests.
    /// Formatting is deliberately culture-invariant so damage and progression never
    /// change numeral systems with the machine locale.
    /// </summary>
    public static class M2BattleReadableText021
    {
        public static string HitPointChange(int amount, bool healing)
        {
            var magnitude = Math.Abs((long)amount).ToString(CultureInfo.InvariantCulture);
            return (healing ? "+" : "-") + magnitude + " HP";
        }

        public static string ArtXpGain(string artId, string caption, int amount)
        {
            var artName = ArtDisplayName(artId, caption, false);
            var magnitude = Math.Abs((long)amount).ToString(CultureInfo.InvariantCulture);
            var gain = amount == 0 ? "ART XP GAINED" : "ART XP +" + magnitude;
            return string.IsNullOrWhiteSpace(artName)
                ? gain
                : artName.ToUpperInvariant() + "\n" + gain;
        }

        public static string NewArtLearned(string artId, string caption)
        {
            var artName = ArtDisplayName(artId, caption, true);
            return string.IsNullOrWhiteSpace(artName)
                ? "NEW ART LEARNED"
                : "NEW ART LEARNED\n" + artName.ToUpperInvariant();
        }

        public static string ArtDisplayName(string artId, string caption, bool breakthrough)
        {
            return ArtDisplayName(artId, caption, breakthrough, string.Empty);
        }

        /// <summary>
        /// Resolves an Art name from the same player-facing metadata used by the
        /// Forecast, then from authoritative event prose. Stable IDs are only a
        /// last-resort input and are never returned to the HUD verbatim.
        /// </summary>
        public static string ArtDisplayName(
            string artId,
            string caption,
            bool breakthrough,
            string registeredDisplayName)
        {
            if (!string.IsNullOrWhiteSpace(registeredDisplayName))
                return CleanArtName(registeredDisplayName);

            var extracted = ExtractCaptionArtName(caption, breakthrough);
            if (!string.IsNullOrWhiteSpace(extracted)) return CleanArtName(extracted);
            return HumanizeArtId(artId);
        }

        public static string ArtBannerTitle(
            string familyLabel,
            string artId,
            string caption,
            string registeredDisplayName = "")
        {
            var family = CleanLine(familyLabel).ToUpperInvariant();
            var artName = ArtDisplayName(artId, caption, false, registeredDisplayName);
            return string.IsNullOrWhiteSpace(artName)
                ? family
                : family + " · " + artName.ToUpperInvariant();
        }

        private static string HumanizeArtId(string artId)
        {
            if (string.IsNullOrWhiteSpace(artId)) return string.Empty;
            if (StringComparer.Ordinal.Equals(artId, "ART_BASIC_STRIKE_101")) return "Basic Strike";
            var value = artId.Trim();
            if (TryHumanizeDeepNodeId(value, out var deepNodeName)) return deepNodeName;
            if (value.StartsWith("ART_BASIC_", StringComparison.Ordinal))
                value = value.Substring("ART_BASIC_".Length);
            else if (value.StartsWith("ART_", StringComparison.Ordinal))
                value = value.Substring("ART_".Length);
            else if (value.StartsWith("ECHO_", StringComparison.Ordinal))
                value = value.Substring("ECHO_".Length);
            else if (value.StartsWith("COVENANT_", StringComparison.Ordinal))
                value = value.Substring("COVENANT_".Length);
            value = CleanLine(value.Replace('_', ' ').ToLowerInvariant());
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
        }

        private static string ExtractCaptionArtName(string caption, bool breakthrough)
        {
            if (string.IsNullOrWhiteSpace(caption)) return string.Empty;
            if (breakthrough)
            {
                // The shipping combat authority records the learned Art's actual
                // content name in past tense. Do not fall back to "Sword Art 4"
                // merely because older presentation fixtures said "learns".
                // Connective words are lowercase in committed authority prose;
                // title-case Art names such as Command Through Chaos are not delimiters.
                var committed = ExtractAfterLastUntil(caption, " learned ", StringComparison.Ordinal, " through ", "!", ".");
                if (!string.IsNullOrWhiteSpace(committed)) return committed;
                var learned = ExtractAfterLastUntil(caption, " learns ", "!", ".");
                if (!string.IsNullOrWhiteSpace(learned)) return learned;
            }

            var growth = ExtractBetween(caption, " grows ", " through meaningful use");
            if (!string.IsNullOrWhiteSpace(growth)) return growth;

            foreach (var opening in new[] { " uses ", " casts ", " performs ", " calls ", " invokes " })
            {
                var action = ExtractAfterUntil(
                    caption,
                    opening,
                    " on ",
                    " against ",
                    " at ",
                    " for ",
                    " through ",
                    "!",
                    ".");
                if (!string.IsNullOrWhiteSpace(action)) return action;
            }

            return string.Empty;
        }

        private static string CleanArtName(string value)
        {
            var cleaned = CleanLine(value).Trim(' ', '\t', '\r', '\n', '"', '\'', '\u2018', '\u2019', '\u201c', '\u201d', '.', '!', ':');
            return LooksLikeStableIdentifier(cleaned) ? HumanizeArtId(cleaned) : cleaned;
        }

        private static bool LooksLikeStableIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (value.StartsWith("TREE_", StringComparison.Ordinal) ||
                value.StartsWith("ART_", StringComparison.Ordinal) ||
                value.StartsWith("ECHO_", StringComparison.Ordinal) ||
                value.StartsWith("COVENANT_", StringComparison.Ordinal)) return true;
            if (value.IndexOf('_') < 0) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsLetter(character) && !char.IsUpper(character)) return false;
            }
            return true;
        }

        private static bool TryHumanizeDeepNodeId(string value, out string playerFacingName)
        {
            const string prefix = "TREE_CA002_";
            playerFacingName = string.Empty;
            if (!value.StartsWith(prefix, StringComparison.Ordinal)) return false;

            var nodeMarker = value.LastIndexOf("_N", StringComparison.Ordinal);
            if (nodeMarker <= prefix.Length || nodeMarker + 2 >= value.Length) return false;
            if (!int.TryParse(value.Substring(nodeMarker + 2), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var nodeIndex)) return false;

            var treeToken = value.Substring(prefix.Length, nodeMarker - prefix.Length);
            string familyToken;
            string suffix;
            if (treeToken.StartsWith("WPN_", StringComparison.Ordinal))
            {
                familyToken = treeToken.Substring("WPN_".Length);
                suffix = " Art";
            }
            else if (treeToken.StartsWith("MYS_", StringComparison.Ordinal))
            {
                familyToken = treeToken.Substring("MYS_".Length);
                suffix = familyToken == "RESTORATION" || familyToken == "WARDING"
                    ? " Art"
                    : " Mystic";
            }
            else if (treeToken.StartsWith("ROLE_", StringComparison.Ordinal))
            {
                familyToken = treeToken.Substring("ROLE_".Length);
                suffix = " Discipline";
            }
            else return false;

            playerFacingName = FriendlyFamilyName(familyToken) + suffix + " " +
                               nodeIndex.ToString(CultureInfo.InvariantCulture);
            return true;
        }

        private static string FriendlyFamilyName(string familyToken)
        {
            switch (familyToken)
            {
                case "SPEAR_POLEARM": return "Spear & Polearm";
                case "FOCUS": return "Focus & Catalyst";
                case "ENGINEERING": return "Engineering Tool";
                case "HYBRID_RELIC": return "Hybrid Relic Weapon";
                default:
                    var value = CleanLine((familyToken ?? string.Empty).Replace('_', ' ').ToLowerInvariant());
                    return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
            }
        }

        private static string ExtractBetween(string source, string opening, string closing)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var start = source.IndexOf(opening, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return string.Empty;
            start += opening.Length;
            var end = source.IndexOf(closing, start, StringComparison.OrdinalIgnoreCase);
            return end <= start ? string.Empty : source.Substring(start, end - start);
        }

        private static string ExtractAfterUntil(string source, string opening, params string[] closings)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var start = source.IndexOf(opening, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return string.Empty;
            start += opening.Length;
            var end = source.Length;
            foreach (var closing in closings)
            {
                var candidate = source.IndexOf(closing, start, StringComparison.OrdinalIgnoreCase);
                if (candidate >= start && candidate < end) end = candidate;
            }
            return end <= start ? string.Empty : source.Substring(start, end - start);
        }

        private static string ExtractAfterLastUntil(string source, string opening, params string[] closings)
            => ExtractAfterLastUntil(source, opening, StringComparison.OrdinalIgnoreCase, closings);

        private static string ExtractAfterLastUntil(string source, string opening,
            StringComparison comparison, params string[] closings)
        {
            if (string.IsNullOrWhiteSpace(source)) return string.Empty;
            var start = source.LastIndexOf(opening, comparison);
            if (start < 0) return string.Empty;
            start += opening.Length;
            var end = source.Length;
            foreach (var closing in closings)
            {
                var candidate = source.IndexOf(closing, start, comparison);
                if (candidate >= start && candidate < end) end = candidate;
            }
            return end <= start ? string.Empty : source.Substring(start, end - start);
        }

        private static string CleanLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var words = value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", words).Trim();
        }
    }
}
