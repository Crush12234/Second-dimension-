#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Release024;

namespace SecondDimension.Editor.Release024
{
    public static class FullGameIntegrationValidation024
    {
        [MenuItem("Second Dimension/Final Integration 024/Validate Complete Game")]
        public static void Validate()
        {
            var registry = FullGameIntegrationRegistry024.LoadFromResources();
            var snapshot = FullGameIntegrationHealthService024.BuildSnapshot();
            if (!snapshot.IsReady)
                throw new InvalidOperationException(snapshot.Error);

            Require(snapshot.SaveFormatVersion == registry.Manifest.saveFormatVersion, "save format");
            Require(snapshot.SmokeCases == registry.Manifest.smokeCaseCount, "smoke cases");
            Require(snapshot.CityBuildings == registry.Manifest.cityBuildingCount, "city buildings");
            Require(snapshot.DefenseProfiles == registry.Manifest.defenseProfileCount, "defense profiles");
            Require(snapshot.CanonEvents == registry.Manifest.canonEventCount, "canon events");
            Require(snapshot.ActiveCampaignChapters == registry.Manifest.activeCampaignChapterCount, "campaign chapters");
            Require(snapshot.WorldGateBoards == registry.Manifest.worldGateBoardCount, "World Gate boards");
            Require(snapshot.WorldGateNodes == registry.Manifest.worldGateNodeCount, "World Gate nodes");
            Require(snapshot.TravelRoutes == registry.Manifest.worldGateTravelRouteCount, "travel routes");
            Require(snapshot.StandingSystems == registry.Manifest.worldGateStandingCount, "standing systems");
            Require(snapshot.RecruitUnlockSets == registry.Manifest.worldGateRecruitUnlockCount, "recruit unlocks");
            Require(snapshot.ArtBindings == registry.Manifest.artPresentationBindingCount, "Art bindings");
            Require(snapshot.AlliedUnionCapacity == 10 && snapshot.EnemyUnionCapacity == 10, "20-Union capacity");

            Debug.Log(
                "FINAL GAME + WORLD GATE INTEGRATION 024 VALIDATION PASS\n" +
                string.Join("\n", snapshot.SummaryLines));
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void Require(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(
                    "Final Integration 024 count mismatch: " + label);
        }
    }
}
#endif
