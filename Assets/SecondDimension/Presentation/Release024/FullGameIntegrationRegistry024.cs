using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SecondDimension.Presentation.Release024
{
    [Serializable]
    public sealed class FullGameIntegrationManifestDto024
    {
        public string contentVersion;
        public string title;
        public string baseFinalReleaseVersion;
        public string worldGateRuntimeVersion;
        public int saveFormatVersion;
        public int cityBuildingCount;
        public int defenseProfileCount;
        public int canonEventCount;
        public int signatureRecruitCount;
        public int activeCampaignChapterCount;
        public int repeatableOperationCount;
        public int crisisOperationCount;
        public int worldGateBoardCount;
        public int worldGateNodeCount;
        public int worldGateTravelRouteCount;
        public int worldGateStandingCount;
        public int worldGateRecruitUnlockCount;
        public int artPresentationBindingCount;
        public int weaponFamilyCount;
        public int alliedUnionCapacity;
        public int enemyUnionCapacity;
        public int smokeCaseCount;
        public string[] hardLaws;
    }

    [Serializable]
    public sealed class FullGameIntegrationSmokeCaseDto024
    {
        public string caseId;
        public string title;
        public string category;
        public string[] requiredSystems;
        public string expectedEvidence;
        public bool exactOnceRequired;
        public bool requiredForOwnerReview;
    }

    [Serializable]
    public sealed class FullGameIntegrationSmokeRoot024
    {
        public string contentVersion;
        public FullGameIntegrationSmokeCaseDto024[] cases;
    }

    [Serializable]
    public sealed class FullGameIntegrationAcceptanceDto024
    {
        public string contentVersion;
        public float minimumOwnerAverage;
        public float minimumCategoryScore;
        public int maximumSeverity1Open;
        public int maximumSeverity2Open;
        public bool requiresZeroConsoleErrors;
        public bool requiresAllEditModeTests;
        public bool requiresAllPlayModeTests;
        public bool requiresWindowsSmokeTest;
        public bool requiresSaveRelaunch;
        public bool requiresAudibleCapture;
        public bool requiresOwnerApproval;
        public bool hardStopAfterOwnerReviewBuild;
    }

    public sealed class FullGameIntegrationRegistry024
    {
        public FullGameIntegrationManifestDto024 Manifest { get; private set; }
        public IReadOnlyDictionary<string, FullGameIntegrationSmokeCaseDto024> SmokeCases { get; private set; }
        public FullGameIntegrationAcceptanceDto024 Acceptance { get; private set; }

        public static FullGameIntegrationRegistry024 LoadFromResources()
        {
            var registry = new FullGameIntegrationRegistry024
            {
                Manifest = Load<FullGameIntegrationManifestDto024>(
                    "SecondDimension/Release024/Data/FullGameIntegrationManifest024"),
                Acceptance = Load<FullGameIntegrationAcceptanceDto024>(
                    "SecondDimension/Release024/Data/FullGameIntegrationAcceptance024")
            };
            var root = Load<FullGameIntegrationSmokeRoot024>(
                "SecondDimension/Release024/Data/FullGameSmokeMatrix024");
            registry.SmokeCases = (root.cases ?? Array.Empty<FullGameIntegrationSmokeCaseDto024>())
                .ToDictionary(value => value.caseId, StringComparer.Ordinal);
            registry.ValidateOrThrow();
            return registry;
        }

        public void ValidateOrThrow()
        {
            if (Manifest == null ||
                !StringComparer.Ordinal.Equals(
                    Manifest.contentVersion,
                    "FINAL_GAME_WORLD_GATE_INTEGRATION_024_1.0"))
                throw new InvalidOperationException("Full Game Integration 024 manifest is missing or invalid.");

            if (Manifest.saveFormatVersion != SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion ||
                Manifest.saveFormatVersion != 11)
                throw new InvalidOperationException("Full Game Integration 024 requires save format 11.");

            if (Manifest.worldGateBoardCount != 130 ||
                Manifest.worldGateNodeCount != 1372 ||
                Manifest.activeCampaignChapterCount != 82 ||
                Manifest.repeatableOperationCount != 32 ||
                Manifest.crisisOperationCount != 16)
                throw new InvalidOperationException("Full Game Integration 024 operation coverage is incomplete.");

            if (Manifest.alliedUnionCapacity != 10 ||
                Manifest.enemyUnionCapacity != 10 ||
                Manifest.artPresentationBindingCount != 240)
                throw new InvalidOperationException("Full Game Integration 024 battle capacity is invalid.");

            if (SmokeCases == null ||
                SmokeCases.Count != Manifest.smokeCaseCount ||
                SmokeCases.Count < 36 ||
                SmokeCases.Values.Any(value =>
                    value == null ||
                    string.IsNullOrWhiteSpace(value.title) ||
                    string.IsNullOrWhiteSpace(value.expectedEvidence) ||
                    value.requiredSystems == null ||
                    value.requiredSystems.Length == 0))
                throw new InvalidOperationException("Full Game Integration 024 smoke coverage is incomplete.");

            if (Manifest.hardLaws == null ||
                Manifest.hardLaws.Length < 12 ||
                !Manifest.hardLaws.Any(value =>
                    value.IndexOf("one runtime", StringComparison.OrdinalIgnoreCase) >= 0) ||
                !Manifest.hardLaws.Any(value =>
                    value.IndexOf("World Gate", StringComparison.OrdinalIgnoreCase) >= 0) ||
                !Manifest.hardLaws.Any(value =>
                    value.IndexOf("No grid", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Full Game Integration 024 hard-law register is incomplete.");

            if (Acceptance == null ||
                Acceptance.minimumOwnerAverage < 9f ||
                Acceptance.minimumCategoryScore < 8.5f ||
                !Acceptance.requiresOwnerApproval ||
                !Acceptance.requiresSaveRelaunch)
                throw new InvalidOperationException("Full Game Integration 024 acceptance gate is too weak.");
        }

        private static T Load<T>(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
                throw new InvalidOperationException("Missing resource: " + resourcePath);
            var value = JsonConvert.DeserializeObject<T>(asset.text);
            if (value == null)
                throw new InvalidOperationException("Invalid resource: " + resourcePath);
            return value;
        }
    }
}
