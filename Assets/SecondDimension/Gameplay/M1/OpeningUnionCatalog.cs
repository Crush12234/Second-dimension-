using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.M1
{
    [Serializable]
    public sealed class CatalogOptionState
    {
        [JsonConstructor]
        public CatalogOptionState(string id, string displayName, string summary = null)
        {
            Id = string.IsNullOrWhiteSpace(id)
                ? throw new ArgumentException("Catalog ID is required.", nameof(id))
                : id;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
            Summary = summary ?? string.Empty;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string Summary { get; }
    }

    /// <summary>
    /// M1 subset of frozen formation and early-doctrine authority. The opening
    /// builder may save partially filled plans for player freedom; those plans
    /// carry the chosen formation identity, while full formation bonuses remain
    /// subject to the later combat engine's member-count rules.
    /// </summary>
    public static class OpeningUnionCatalog
    {
        private static readonly CatalogOptionState[] FormationValues =
        {
            new CatalogOptionState(
                "FORMATION_SHIELD_WALL",
                "Shield Wall",
                "OPENING: UP TO +8% COHESION • M2 ROLE: GUARD / COHESION"),
            new CatalogOptionState(
                "FORMATION_WEDGE",
                "Wedge",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: MARTIAL / BREAK"),
            new CatalogOptionState(
                "FORMATION_SKIRMISH_LINE",
                "Skirmish Line",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: TACTICAL / AGILITY"),
            new CatalogOptionState(
                "FORMATION_ARCANE_CIRCLE",
                "Arcane Circle",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: MYSTIC / SUPPORT"),
            new CatalogOptionState(
                "FORMATION_CRESCENT",
                "Crescent",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: COUNTER / FLANK DEFENSE"),
            new CatalogOptionState(
                "FORMATION_RESCUE_COLUMN",
                "Rescue Column",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: RESCUE / RESTORATION"),
            new CatalogOptionState(
                "FORMATION_ARROWHEAD",
                "Arrowhead",
                "OPENING: NO RAW STAT CHANGE • M2 ROLE: RUSH / BREAK"),
            new CatalogOptionState(
                "FORMATION_VEILED_ECHELON",
                "Veiled Echelon",
                "OPENING: UP TO +2% COHESION • M2 ROLE: AMBUSH / TACTICAL")
        };

        private static readonly CatalogOptionState[] DoctrineValues =
        {
            new CatalogOptionState(
                "DOCTRINE_BALANCED",
                "Read the Field",
                "FORECAST BIAS: BALANCED OFFENSE / GUARD / HEAL / TACTICAL • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_AGGRESSIVE",
                "Press Until They Break",
                "FORECAST BIAS: OFFENSE / BREAK • LESS GUARD AND HEALING • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_GUARDIAN",
                "No One Falls",
                "FORECAST BIAS: GUARD / RESCUE / HEALING • LESS OFFENSE • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_MYSTIC_PRESSURE",
                "Control with Arts",
                "FORECAST BIAS: MYSTIC / SUPPORT / PRESSURE • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_CONSERVATIVE",
                "Spend Only What Matters",
                "FORECAST BIAS: LOW RESOURCE / GUARD / RETREAT • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_RESCUE_FIRST",
                "Bring Everyone Home",
                "FORECAST BIAS: RESCUE / HEALING / GUARD • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_OBJECTIVE_FIRST",
                "Win the Actual Mission",
                "FORECAST BIAS: OBJECTIVE / TACTICAL • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_AMBUSH",
                "Strike What They Hide",
                "FORECAST BIAS: AMBUSH / TACTICAL / OFFENSE • LESS GUARD • NO STAT BONUS"),
            new CatalogOptionState(
                "DOCTRINE_SUPPORT_NETWORK",
                "Make Every Union Better",
                "FORECAST BIAS: SUPPORT / TACTICAL / HEALING • LESS OFFENSE • NO STAT BONUS")
        };

        public static IReadOnlyList<CatalogOptionState> Formations => Array.AsReadOnly(FormationValues);
        public static IReadOnlyList<CatalogOptionState> Doctrines => Array.AsReadOnly(DoctrineValues);

        public static bool IsFormation(string id) => Contains(FormationValues, id);
        public static bool IsDoctrine(string id) => Contains(DoctrineValues, id);

        private static bool Contains(IReadOnlyList<CatalogOptionState> values, string id)
        {
            for (var index = 0; index < values.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(values[index].Id, id)) return true;
            }
            return false;
        }
    }
}
