using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SecondDimension.Presentation.Release023
{
    [Serializable] public sealed class FinalReleaseManifestDto023
    {
        public string contentVersion; public string title; public string baseContentVersion; public string futureCampaignAuthorityVersion;
        public int saveFormatVersion; public int cityBuildingCount; public int defenseProfileCount; public int canonEventCount;
        public int signatureRecruitCount; public int activeCampaignChapterCount; public int futureQuestAuthorityCount;
        public int futureMapAuthorityCount; public int futureFortressAuthorityCount; public int futureEncounterAuthorityCount; public int futureBossAuthorityCount;
        public int worldThemeCount; public int enemyPresentationCount; public int bossPresentationCount; public int recruitOriginCount;
        public int lootEntryCount; public int materialCount; public int repeatableContractCount; public int crisisOperationCount; public int audioCueCount;
        public int artPresentationBindingCount; public int weaponTrackCount; public int weaponEvolutionRecipeCount; public int armorEvolutionRecipeCount;
        public int advancedClassCount; public int certificationPathCount; public int abyssFloorCount; public int abyssOperationCount;
        public int abyssEventCount; public int abyssBossCount; public int invocationArtifactBaseCount; public int invocationAffixCount;
        public int summonEchoCount; public int greatCovenantCount; public int worldGateBoardCount; public int worldGateNodeCount; public int worldGateTravelRouteCount; public int worldGateStandingCount; public int worldGateRecruitUnlockCount; public int alliedUnionCapacity; public int enemyUnionCapacity; public int smokeCaseCount;
        public string[] hardLaws;
    }

    [Serializable] public sealed class FinalSmokeCaseDto023
    {
        public string caseId; public string title; public string category; public string[] requiredSystems;
        public string expectedEvidence; public bool exactOnceRequired; public bool requiredForOwnerReview;
    }
    [Serializable] public sealed class FinalSmokeMatrixRoot023 { public string contentVersion; public FinalSmokeCaseDto023[] cases; }
    [Serializable] public sealed class FinalReleaseAcceptanceDto023
    {
        public string contentVersion; public float minimumOwnerAverage; public float minimumCategoryScore;
        public int maximumSeverity1Open; public int maximumSeverity2Open; public bool requiresZeroConsoleErrors;
        public bool requiresAllEditModeTests; public bool requiresAllPlayModeTests; public bool requiresWindowsSmokeTest;
        public bool requiresSaveRelaunch; public bool requiresAudibleCapture; public bool requiresOwnerApproval; public bool hardStopAfterOwnerReviewBuild;
    }

    public sealed class FinalReleaseRegistry023
    {
        public FinalReleaseManifestDto023 Manifest { get; private set; }
        public IReadOnlyDictionary<string, FinalSmokeCaseDto023> SmokeCases { get; private set; }
        public FinalReleaseAcceptanceDto023 Acceptance { get; private set; }

        public static FinalReleaseRegistry023 LoadFromResources()
        {
            var registry = new FinalReleaseRegistry023
            {
                Manifest = Load<FinalReleaseManifestDto023>("SecondDimension/Release023/Data/FinalGameReleaseManifest023"),
                Acceptance = Load<FinalReleaseAcceptanceDto023>("SecondDimension/Release023/Data/FinalReleaseAcceptance023")
            };
            var root = Load<FinalSmokeMatrixRoot023>("SecondDimension/Release023/Data/FullGameSmokeMatrix023");
            registry.SmokeCases = (root.cases ?? Array.Empty<FinalSmokeCaseDto023>()).ToDictionary(value => value.caseId, StringComparer.Ordinal);
            registry.ValidateOrThrow();
            return registry;
        }

        public void ValidateOrThrow()
        {
            if (Manifest == null || Manifest.contentVersion != "FINAL_GAME_RELEASE_CANDIDATE_023_1.0")
                throw new InvalidOperationException("Final Release 023 manifest is missing or invalid.");
            if (Manifest.saveFormatVersion != SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion)
                throw new InvalidOperationException("Final Release 023 save version does not match the current envelope.");
            if (SmokeCases == null || SmokeCases.Count != Manifest.smokeCaseCount || SmokeCases.Count < 30)
                throw new InvalidOperationException("Final Release 023 smoke matrix is incomplete.");
            if (SmokeCases.Values.Any(value => string.IsNullOrWhiteSpace(value.title) || string.IsNullOrWhiteSpace(value.expectedEvidence) || value.requiredSystems == null || value.requiredSystems.Length == 0))
                throw new InvalidOperationException("Final Release 023 contains an incomplete smoke case.");
            if (Manifest.hardLaws == null || Manifest.hardLaws.Length < 10 ||
                !Manifest.hardLaws.Any(value => value.IndexOf("No grid", StringComparison.OrdinalIgnoreCase) >= 0) ||
                !Manifest.hardLaws.Any(value => value.IndexOf("Great Covenants", StringComparison.OrdinalIgnoreCase) >= 0) ||
                !Manifest.hardLaws.Any(value => value.IndexOf("second campaign runtime", StringComparison.OrdinalIgnoreCase) >= 0))
                throw new InvalidOperationException("Final Release 023 hard-law register is incomplete.");
            if (Acceptance == null || Acceptance.minimumOwnerAverage < 9f || Acceptance.minimumCategoryScore < 8.5f || !Acceptance.requiresOwnerApproval)
                throw new InvalidOperationException("Final Release 023 acceptance threshold is too weak.");
        }

        static T Load<T>(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new InvalidOperationException("Missing resource: " + resourcePath);
            var value = JsonConvert.DeserializeObject<T>(asset.text);
            if (value == null) throw new InvalidOperationException("Invalid resource: " + resourcePath);
            return value;
        }
    }
}
