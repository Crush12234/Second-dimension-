using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.SpecialRelic001
{
    /// <summary>
    /// Authored runtime bindings for the six P0 Invocation Relics.  The source pack
    /// intentionally contains design prose rather than executable balance numbers;
    /// these values are therefore local game-balance authority, not Library values.
    /// </summary>
    public sealed class SpecialRelicInvocationRule001
    {
        internal SpecialRelicInvocationRule001(
            string relicId,
            string displayName,
            string summonName,
            string canonicalBaseId,
            string equipmentTrackId,
            string role,
            int sharedApCost,
            int personalMpCost)
        {
            RelicId = relicId;
            DisplayName = displayName;
            SummonName = summonName;
            CanonicalBaseId = canonicalBaseId;
            EquipmentTrackId = equipmentTrackId;
            Role = role;
            SharedApCost = sharedApCost;
            PersonalMpCost = personalMpCost;
        }

        public string RelicId { get; }
        public string DisplayName { get; }
        public string SummonName { get; }
        public string CanonicalBaseId { get; }
        public string EquipmentTrackId { get; }
        public string Role { get; }
        public int SharedApCost { get; }
        public int PersonalMpCost { get; }
        public string EchoId => RelicId + "_ECHO";
        public string ActiveDisplayName => DisplayName + " • ACTIVE P0";

        internal EchoDto022 CreateEcho() => new EchoDto022
        {
            echoId = EchoId,
            displayName = SummonName,
            unlockFloor = 0,
            forecastCategory = Role,
            description = "Temporary " + SummonName + " invocation through a selected complete Union Forecast.",
            sharedApCost = SharedApCost,
            personalMpCost = PersonalMpCost,
            directIndividualSelection = false,
            realMoneyGacha = false
        };
    }

    public static class SpecialRelicInvocationRules001
    {
        public const string QualityId = "RARE";
        public const string SpecialRelicEquipmentTag = "SPECIAL_RELIC_001";
        public const string ManualEquipOnlyTag = "MANUAL_EQUIP_ONLY";
        public const int MeaningfulUseMasteryGain = 5;
        public const string LocalBalanceAuthority = "SPECIAL_RELIC_INVOCATION_LOCAL_BALANCE_001";

        private static readonly SpecialRelicInvocationRule001[] Rules =
        {
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_001", "Aegis Hound Invocation Relic", "Aegis Hound",
                "INVOCATION_BASE022_07_01", "WTRACK022_07", "GUARD", 8, 14),
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_002", "Ember Roc Invocation Relic", "Ember Roc",
                "INVOCATION_BASE022_09_01", "WTRACK022_09", "MYSTIC", 8, 14),
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_003", "Tide Serpent Invocation Relic", "Tide Serpent",
                "INVOCATION_BASE022_10_01", "WTRACK022_10", "RESTORATION", 7, 16),
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_004", "Stone Colossus Invocation Relic", "Stone Colossus",
                "INVOCATION_BASE022_02_01", "WTRACK022_02", "WARDING", 10, 17),
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_005", "Moon Stag Invocation Relic", "Moon Stag",
                "INVOCATION_BASE022_12_01", "WTRACK022_12", "SUPPORT", 8, 13),
            new SpecialRelicInvocationRule001(
                "RELIC001_INV_006", "Storm Kirin Invocation Relic", "Storm Kirin",
                "INVOCATION_BASE022_04_01", "WTRACK022_04", "TACTICAL", 7, 12)
        };

        private static readonly Dictionary<string, SpecialRelicInvocationRule001> ByRelicId =
            Rules.ToDictionary(value => value.RelicId, value => value, StringComparer.Ordinal);

        public static IReadOnlyList<SpecialRelicInvocationRule001> All => Array.AsReadOnly(Rules);

        public static bool TryGet(string relicId, out SpecialRelicInvocationRule001 rule) =>
            ByRelicId.TryGetValue(relicId ?? string.Empty, out rule);

        public static bool IsInvocationDefinition(string definitionId) =>
            TryGet(definitionId, out _);
    }
}
