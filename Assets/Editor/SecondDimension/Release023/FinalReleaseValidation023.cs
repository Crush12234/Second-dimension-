#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Release023;

namespace SecondDimension.Editor.Release023
{
    public static class FinalReleaseValidation023
    {
        [MenuItem("Second Dimension/Final Release 023/Validate Complete Game")]
        public static void Validate()
        {
            var registry = FinalReleaseRegistry023.LoadFromResources();
            var snapshot = FinalReleaseReadinessService023.BuildSnapshot();
            if (!snapshot.IsAvailable) throw new InvalidOperationException(snapshot.Error);
            var m = registry.Manifest;
            Require(snapshot.CityBuildings == m.cityBuildingCount, "city buildings");
            Require(snapshot.DefenseProfiles == m.defenseProfileCount, "defense profiles");
            Require(snapshot.CanonEvents == m.canonEventCount, "canon events");
            Require(snapshot.SignatureRecruits == m.signatureRecruitCount, "signature recruits");
            Require(snapshot.ActiveCampaignChapters == m.activeCampaignChapterCount, "active campaign chapters");
            Require(snapshot.WorldGateBoards == m.worldGateBoardCount, "World Gate boards");
            Require(snapshot.WorldGateNodes == m.worldGateNodeCount, "World Gate nodes");
            Require(snapshot.WorldGateTravelRoutes == m.worldGateTravelRouteCount, "World Gate travel routes");
            Require(snapshot.WorldGateStandingSystems == m.worldGateStandingCount, "World Gate standing systems");
            Require(snapshot.WorldGateRecruitUnlockSets == m.worldGateRecruitUnlockCount, "World Gate recruit unlocks");
            Require(snapshot.FutureQuests == m.futureQuestAuthorityCount, "future quests");
            Require(snapshot.FutureFortresses == m.futureFortressAuthorityCount, "future fortresses");
            Require(snapshot.FutureEncounters == m.futureEncounterAuthorityCount, "future encounters");
            Require(snapshot.ArtBindings == m.artPresentationBindingCount, "Art presentation bindings");
            Require(snapshot.WeaponRecipes == m.weaponEvolutionRecipeCount, "weapon recipes");
            Require(snapshot.ArmorRecipes == m.armorEvolutionRecipeCount, "armor recipes");
            var abyssRegistry = SecondDimension.Presentation.Campaign022.CampaignRegistry022
                .LoadFromResources();
            var endlessCount = abyssRegistry.AbyssOperations.Values.Count(value =>
                value.kind == SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022
                    .EndlessBattleKind094);
            Require(snapshot.AbyssFloors == m.abyssFloorCount &&
                abyssRegistry.AbyssOperations.Count - endlessCount == m.abyssOperationCount &&
                endlessCount == 10 &&
                snapshot.AbyssOperations == m.abyssOperationCount + endlessCount,
                "historical Abyss content plus ten endless battle definitions");
            Require(snapshot.GreatCovenants == m.greatCovenantCount, "Great Covenants");
            Require(snapshot.AlliedUnionCapacity == 10 && snapshot.EnemyUnionCapacity == 10, "20-Union capacity");
            Debug.Log("FINAL GAME RELEASE CANDIDATE 023 VALIDATION PASS\n" + string.Join("\n", snapshot.SummaryLines));
        }

        public static void ValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        static void Require(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("Final Release 023 count mismatch: " + label);
        }
    }
}
#endif
