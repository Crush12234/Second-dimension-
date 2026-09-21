#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.GuildCity017H
{
    public static class GuildCityDefenseCanonValidation017H
    {
        [MenuItem("Second Dimension/Guild City 017H/Validate Buildings, Defense, and Canon")]
        public static void ValidateFromMenu() => Validate();

        public static void PrepareAndValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        public static void Validate()
        {
            var authorityRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var city = GuildCityContent017D.LoadFromDirectory(Path.Combine(authorityRoot, "GUILD_CITY_017D"));
            var strategic = GuildCityStrategicContent017H.LoadFromDirectory(Path.Combine(authorityRoot, "GUILD_CITY_017H"));

            Require(city.Buildings.Count == 30, "017H requires exactly 30 city buildings.");
            Require(strategic.Contributions.Count == 30, "Every building requires one contribution definition.");
            Require(strategic.Lanes.Count == 5, "017H requires five strategic defense lanes.");
            Require(strategic.Profiles.Count == 12, "017H requires twelve defense profiles.");
            Require(strategic.CanonEvents.Count == 36, "017H requires thirty-six canon-event definitions.");

            foreach (var building in city.Buildings.Values)
            {
                Require(strategic.Contributions.TryGetValue(building.Id, out var contribution),
                    "Missing building contribution: " + building.Id);
                var value = contribution.PerLevel;
                Require(value != null, "Building contribution values are missing: " + building.Id);
                var xp = value.PersonalXpBp + value.CombatArtMasteryBp + value.MysticMasteryBp +
                         value.RestorationMasteryBp + value.WardingMasteryBp + value.UnionDisciplineBp +
                         value.GuildTreasuryXpBp + value.CivicHallXpBp + value.StaffDutyXpFlat;
                var combat = value.StartingApFlat + value.StartingCohesionFlat + value.StartingMpFlat +
                             value.EnemyCohesionDamageFlat + value.SuppliesFlat + value.ScoutingFlat +
                             value.DefensePowerFlat + value.BarrierIntegrityFlat + value.ResupplyFlat +
                             value.ReinforcementReadinessFlat + value.GuardPreparationFlat;
                Require(xp > 0, "Building has no XP/mastery contribution: " + building.Id);
                Require(combat > 0, "Building has no combat/operation contribution: " + building.Id);
            }

            foreach (var profile in strategic.Profiles.Values)
            {
                Require(profile.Waves != null && profile.Waves.Length == 3,
                    "Every defense profile requires three waves: " + profile.Id);
                var decisiveCount = profile.Waves.Count(value => value.DecisiveBattle);
                Require(decisiveCount >= 1,
                    "Every defense profile requires at least one decisive certified battle: " + profile.Id);
                Require(!profile.AvailableInOpening || decisiveCount == 1,
                    "Opening defenses require exactly one decisive battle: " + profile.Id);
                Require(profile.Waves.All(value => value.EnemyUnionCount >= 0 && value.EnemyUnionCount <= 10),
                    "Enemy Union count must remain inside the certified 10-Union cap: " + profile.Id);
            }

            Require(strategic.CanonEvents.Values.Count(value => value.Classification == "HISTORICAL_CHRONICLE") == 12,
                "Historical Chronicle count must be twelve.");
            Require(strategic.CanonEvents.Values.Count(value => value.Classification == "CANON_ECHO") == 12,
                "Canon Echo count must be twelve.");
            Require(strategic.CanonEvents.Values.Count(value => value.Classification == "FUTURE_LOCKED") == 12,
                "Future Locked count must be twelve.");
            Require(strategic.CanonEvents.Values.All(value => !value.CanRewritePublishedCanon &&
                                                             !value.RevealsProtectedSystemTruth &&
                                                             !value.CanCauseInvoluntaryDeparture),
                "Canon-event safety law failed.");

            RequireAsset("Assets/Resources/SecondDimension/GuildCity017H/Defense/CITY_DEFENSE_MAP_017H.png");
            foreach (var lane in strategic.Lanes.Values)
                RequireAsset("Assets/Resources/SecondDimension/GuildCity017H/Defense/" + lane.Id + ".png");

            Debug.Log("Guild City Defense & Canon 017H validation PASS: buildings=30, lanes=5, profiles=12, waves=36, canon events=36.");
        }

        private static void RequireAsset(string path)
        {
            Require(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null,
                "Required 017H asset did not import: " + path);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
#endif
