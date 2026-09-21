using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SecondDimension.Gameplay.GuildCity017D
{
    /// <summary>
    /// Keeps stable authority IDs available to systems while ensuring the default
    /// player view receives a compact human label rather than EQ_ / TREE_ / PASS data.
    /// </summary>
    public sealed class PlayerFacingLabelService070
    {
        private static readonly string[] RawPrefixes =
        {
            "BATTLE_ITEM_", "EQUIPMENT_", "FORMATION_", "DOCTRINE_", "WEAPON_",
            "CLASS_", "TREE_", "PASS_", "LOOT_", "ITEM_", "EQ_", "ART_",
            "SIGI_", "SIGREC_", "PROC_"
        };

        private static readonly HashSet<string> HiddenAuthorityTokens =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "BATTLE", "EQUIPMENT", "FORMATION", "DOCTRINE", "WEAPON",
                "CLASS", "TREE", "PASS", "LOOT", "ITEM", "EQ", "ART", "PROC",
                "CA002", "WPN", "ROLE", "MYSTIC", "MYS", "SIGREC", "TEND", "FAMILY",
                "QUALITY", "STABLE", "NODE", "SIGI", "MAIN", "OFF", "HAND",
                "HEAD", "BODY", "ARMS", "LEGS", "ACCESSORY", "TOOL", "RELIC"
            };

        public string DefaultLabel(string authoredLabel, string authorityId, string fallback = "Unknown")
        {
            if (!string.IsNullOrWhiteSpace(authoredLabel) && !LooksLikeAuthorityId(authoredLabel))
                return authoredLabel.Trim();

            var humanized = HumanizeAuthorityId(authorityId);
            return string.IsNullOrWhiteSpace(humanized)
                ? (string.IsNullOrWhiteSpace(fallback) ? "Unknown" : fallback.Trim())
                : humanized;
        }

        public string HumanizeAuthorityId(string authorityId)
        {
            if (string.IsNullOrWhiteSpace(authorityId)) return string.Empty;
            var tokens = authorityId.Trim().Split(new[] { '_', '-', '.', '/', ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            var visible = new List<string>();
            for (var index = 0; index < tokens.Length; index++)
            {
                var token = tokens[index];
                int number;
                if (HiddenAuthorityTokens.Contains(token) || IsOpaqueHexToken(token) ||
                    int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                    continue;
                visible.Add(TitleToken(token));
            }
            return string.Join(" ", visible.ToArray()).Trim();
        }

        public bool LooksLikeAuthorityId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var trimmed = value.Trim();
            for (var index = 0; index < RawPrefixes.Length; index++)
                if (trimmed.StartsWith(RawPrefixes[index], StringComparison.OrdinalIgnoreCase))
                    return true;
            var tokens = trimmed.Split(new[] { '_', '-', '.', '/', ' ', '\t' },
                StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < tokens.Length; index++)
                if (IsOpaqueHexToken(tokens[index])) return true;
            return false;
        }

        private static bool IsOpaqueHexToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 12) return false;
            var hasLetter = false;
            var hasDigit = false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                var isHex = character >= '0' && character <= '9' ||
                            character >= 'a' && character <= 'f' ||
                            character >= 'A' && character <= 'F';
                if (!isHex) return false;
                hasLetter |= char.IsLetter(character);
                hasDigit |= char.IsDigit(character);
            }
            return hasLetter && hasDigit;
        }

        public string QuickPlayFailureMessage(IReadOnlyList<string> errors)
        {
            var first = FirstNonEmpty(errors);
            if (string.IsNullOrEmpty(first))
                return "Quick Play could not continue. Speak with the Guide or check Party Setup, then try again.";

            if (first.IndexOf("GUILD_REQUIRED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                first.IndexOf("INPUT_REQUIRED", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Load or create a Guild before using Quick Play.";
            if (first.IndexOf("STORY_CONTRACT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                first.IndexOf("CONTRACT_NOT_FOUND", StringComparison.OrdinalIgnoreCase) >= 0)
                return "No story contract is available yet. Speak with the Guide in the Guild Hall.";
            if (first.IndexOf("RECURRING_RECRUIT_REQUIRED", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Meet the applicants and recruit one permanent adventurer before leaving.";

            if (LooksLikeStableErrorCode(first))
                return "Quick Play could not continue. Speak with the Guide or check Party Setup, then try again.";

            var visible = new List<string>();
            if (errors != null)
            {
                for (var index = 0; index < errors.Count; index++)
                {
                    var error = errors[index];
                    if (!string.IsNullOrWhiteSpace(error) && !LooksLikeStableErrorCode(error))
                        visible.Add(error.Trim());
                }
            }
            return visible.Count == 0
                ? "Quick Play could not continue. Speak with the Guide or check Party Setup, then try again."
                : string.Join(" • ", visible.ToArray());
        }

        private static string FirstNonEmpty(IReadOnlyList<string> values)
        {
            if (values == null) return string.Empty;
            for (var index = 0; index < values.Count; index++)
                if (!string.IsNullOrWhiteSpace(values[index])) return values[index].Trim();
            return string.Empty;
        }

        private static bool LooksLikeStableErrorCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var separator = value.IndexOf(':');
            var candidate = (separator < 0 ? value : value.Substring(0, separator)).Trim();
            if (candidate.IndexOf('_') < 0) return false;
            for (var index = 0; index < candidate.Length; index++)
            {
                var character = candidate[index];
                if (char.IsLetter(character) && char.IsLower(character)) return false;
                if (!(char.IsLetterOrDigit(character) || character == '_')) return false;
            }
            return true;
        }

        private static string TitleToken(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= 2) return value.ToUpperInvariant();
            var builder = new StringBuilder(value.Length);
            builder.Append(char.ToUpperInvariant(value[0]));
            for (var index = 1; index < value.Length; index++)
                builder.Append(char.ToLowerInvariant(value[index]));
            return builder.ToString();
        }
    }
}
