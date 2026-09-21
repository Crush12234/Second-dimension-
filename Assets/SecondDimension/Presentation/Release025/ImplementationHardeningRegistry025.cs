using System;
using Newtonsoft.Json;
using UnityEngine;

namespace SecondDimension.Presentation.Release025
{
    [Serializable]
    public sealed class CompileRiskGateDto025
    {
        public string gateId;
        public string category;
        public string description;
        public string automatedBy;
        public bool blocking;
    }

    [Serializable]
    public sealed class CompileRiskGateRoot025
    {
        public string contentVersion;
        public CompileRiskGateDto025[] gates;
    }

    [Serializable]
    public sealed class ArchitectureOwnerDto025
    {
        public string domain;
        public string owner;
        public bool duplicateForbidden;
    }

    [Serializable]
    public sealed class ArchitectureOwnerRoot025
    {
        public string contentVersion;
        public ArchitectureOwnerDto025[] owners;
    }

    [Serializable]
    public sealed class ImplementationHardeningManifestDto025
    {
        public string contentVersion;
        public string title;
        public string baseVersion;
        public string unityVersion;
        public int saveFormatVersion;
        public int worldGateBoardCount;
        public int worldGateNodeCount;
        public int cityBuildingCount;
        public int defenseProfileCount;
        public int canonEventCount;
        public int artPresentationBindingCount;
        public int weaponFamilyCount;
        public int alliedUnionCapacity;
        public int enemyUnionCapacity;
        public int ownerReviewCaseCount;
        public int assemblyBoundaryRepairCount;
        public int compileRiskGateCount;
        public string[] requiredPackages;
        public string[] hardeningLaws;
    }

    [Serializable]
    public sealed class ImplementationHardeningAcceptanceDto025
    {
        public string contentVersion;
        public bool requiresZeroConsoleErrors;
        public bool requiresAssemblyBoundaryPass;
        public bool requiresAllEditModeTests;
        public bool requiresAllPlayModeTests;
        public bool requiresBootSceneFirst;
        public bool requiresRuntimeAuthorityInStreamingAssets;
        public bool requiresWindowsBuild;
        public bool requiresBuiltPlayerSmoke;
        public bool requiresSaveRelaunch;
        public bool requiresOwnerApproval;
        public float minimumOwnerAverage;
        public float minimumCategoryScore;
        public int maximumSeverity1Open;
        public int maximumSeverity2Open;
    }

    public sealed class ImplementationHardeningRegistry025
    {
        public ImplementationHardeningManifestDto025 Manifest { get; private set; }
        public ImplementationHardeningAcceptanceDto025 Acceptance { get; private set; }
        public CompileRiskGateRoot025 CompileRiskGates { get; private set; }
        public ArchitectureOwnerRoot025 ArchitectureOwnership { get; private set; }

        public static ImplementationHardeningRegistry025 LoadFromResources()
        {
            var registry = new ImplementationHardeningRegistry025
            {
                Manifest = Load<ImplementationHardeningManifestDto025>(
                    "SecondDimension/Release025/Data/ImplementationHardeningManifest025"),
                Acceptance = Load<ImplementationHardeningAcceptanceDto025>(
                    "SecondDimension/Release025/Data/ImplementationHardeningAcceptance025"),
                CompileRiskGates = Load<CompileRiskGateRoot025>(
                    "SecondDimension/Release025/Data/CompileRiskGateMatrix025"),
                ArchitectureOwnership = Load<ArchitectureOwnerRoot025>(
                    "SecondDimension/Release025/Data/ArchitectureOwnership025")
            };
            registry.ValidateOrThrow();
            return registry;
        }

        public void ValidateOrThrow()
        {
            if (Manifest == null || !StringComparer.Ordinal.Equals(
                    Manifest.contentVersion,
                    "FINAL_IMPLEMENTATION_HARDENING_025_1.0"))
                throw new InvalidOperationException(
                    "Implementation Hardening 025 manifest is invalid.");
            if (Manifest.saveFormatVersion != SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion ||
                Manifest.saveFormatVersion != 11)
                throw new InvalidOperationException(
                    "Implementation Hardening 025 requires save format 11.");
            if (Manifest.worldGateBoardCount != 130 || Manifest.worldGateNodeCount != 1372 ||
                Manifest.cityBuildingCount != 30 || Manifest.defenseProfileCount != 12 ||
                Manifest.canonEventCount != 36 || Manifest.artPresentationBindingCount != 240 ||
                Manifest.weaponFamilyCount != 12 || Manifest.alliedUnionCapacity != 10 ||
                Manifest.enemyUnionCapacity != 10 || Manifest.ownerReviewCaseCount != 36)
                throw new InvalidOperationException(
                    "Implementation Hardening 025 feature counts are incomplete.");
            if (Manifest.assemblyBoundaryRepairCount < 1 || Manifest.compileRiskGateCount != 16)
                throw new InvalidOperationException(
                    "Implementation Hardening 025 compile gates are incomplete.");
            if (Manifest.requiredPackages == null || Manifest.requiredPackages.Length < 3 ||
                Manifest.hardeningLaws == null || Manifest.hardeningLaws.Length < 10)
                throw new InvalidOperationException(
                    "Implementation Hardening 025 law register is incomplete.");
            if (CompileRiskGates == null || CompileRiskGates.gates == null ||
                CompileRiskGates.gates.Length != Manifest.compileRiskGateCount ||
                Array.Exists(CompileRiskGates.gates, value => value == null ||
                    string.IsNullOrWhiteSpace(value.gateId) || !value.blocking))
                throw new InvalidOperationException(
                    "Compile-risk gate matrix is incomplete.");
            if (ArchitectureOwnership == null || ArchitectureOwnership.owners == null ||
                ArchitectureOwnership.owners.Length < 12 ||
                Array.Exists(ArchitectureOwnership.owners, value => value == null ||
                    string.IsNullOrWhiteSpace(value.domain) ||
                    string.IsNullOrWhiteSpace(value.owner) || !value.duplicateForbidden))
                throw new InvalidOperationException(
                    "Architecture ownership register is incomplete.");
            if (Acceptance == null || !Acceptance.requiresZeroConsoleErrors ||
                !Acceptance.requiresAssemblyBoundaryPass || !Acceptance.requiresAllEditModeTests ||
                !Acceptance.requiresAllPlayModeTests || !Acceptance.requiresWindowsBuild ||
                !Acceptance.requiresBuiltPlayerSmoke || !Acceptance.requiresSaveRelaunch ||
                !Acceptance.requiresOwnerApproval || Acceptance.minimumOwnerAverage < 9f ||
                Acceptance.minimumCategoryScore < 8.5f || Acceptance.maximumSeverity1Open != 0 ||
                Acceptance.maximumSeverity2Open != 0)
                throw new InvalidOperationException(
                    "Implementation Hardening 025 acceptance gate is too weak.");
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
