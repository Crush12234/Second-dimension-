using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.SpecialRelic001;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// One fail-closed authority for Invocation Artifact definitions, affixes, paths and owned items.
    /// Presentation code may choose IDs, but it may never repair, truncate, or substitute them.
    /// </summary>
    public static class InvocationArtifactAuthority022
    {
        public const int MaximumAffixes = 2;
        public const string InvocationEquipmentTag = "INVOCATION_ARTIFACT";

        private static readonly string[] AffixCategories =
        {
            "COMBAT", "MYSTIC", "RESTORATION", "SUPPORT", "TACTICAL", "WARDING"
        };

        public static IReadOnlyList<string> CanonicalAffixCategories => Array.AsReadOnly(AffixCategories);

        public static bool IsCanonicalAffixCategory(string value) =>
            !string.IsNullOrWhiteSpace(value) &&
            AffixCategories.Any(category => StringComparer.Ordinal.Equals(category, value));

        public static bool TryCanonicalizeAffixIds(
            ICampaignRegistry022 registry,
            IReadOnlyList<string> requestedAffixIds,
            out IReadOnlyList<string> canonicalAffixIds,
            out string error)
        {
            canonicalAffixIds = Array.Empty<string>();
            error = string.Empty;
            if (registry == null)
            {
                error = "CAMPAIGN022_INPUT_REQUIRED";
                return false;
            }

            var requested = requestedAffixIds ?? Array.Empty<string>();
            if (requested.Count > MaximumAffixes)
            {
                error = "CAMPAIGN022_INVOCATION_AFFIX_COUNT_INVALID";
                return false;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            var canonical = new List<string>();
            for (var index = 0; index < requested.Count; index++)
            {
                var affixId = requested[index];
                if (string.IsNullOrWhiteSpace(affixId) || !registry.Affixes.TryGetValue(affixId, out var affix) || affix == null)
                {
                    error = "CAMPAIGN022_INVOCATION_AFFIX_UNKNOWN";
                    return false;
                }
                if (!unique.Add(affixId))
                {
                    error = "CAMPAIGN022_INVOCATION_AFFIX_DUPLICATE";
                    return false;
                }
                if (!IsCanonicalAffixCategory(affix.forecastCategory) || affix.effectPermille <= 0 ||
                    !affix.meaningfulUseRequired || !affix.zeroEffectSpamForbidden ||
                    affix.maximumCopiesPerArtifact != 1)
                {
                    error = "CAMPAIGN022_INVOCATION_AFFIX_LAW_INVALID";
                    return false;
                }
                canonical.Add(affixId);
            }
            canonical.Sort(StringComparer.Ordinal);
            canonicalAffixIds = canonical.AsReadOnly();
            return true;
        }

        public static bool TryResolveBaseAndPath(
            ICampaignRegistry022 registry,
            string baseId,
            out ArtifactBaseDto022 artifactBase,
            out ArtifactPathDto022 path,
            out string error)
        {
            artifactBase = null;
            path = null;
            error = string.Empty;
            if (registry == null)
            {
                error = "CAMPAIGN022_INPUT_REQUIRED";
                return false;
            }
            if (string.IsNullOrWhiteSpace(baseId) || !registry.ArtifactBases.TryGetValue(baseId, out artifactBase) || artifactBase == null)
            {
                error = "CAMPAIGN022_ARTIFACT_BASE_UNKNOWN";
                return false;
            }
            if (string.IsNullOrWhiteSpace(artifactBase.baseId) || string.IsNullOrWhiteSpace(artifactBase.displayName) ||
                !StringComparer.Ordinal.Equals(artifactBase.validSlotId, EquipmentSlotIds.ToolRelic) ||
                !StringComparer.Ordinal.Equals(artifactBase.ownership, "EQUIPMENT_INSTANCE") ||
                artifactBase.sentientOwnershipAllowed ||
                !StringComparer.Ordinal.Equals(artifactBase.standardControl, "UNION_FORECAST_ONLY") ||
                artifactBase.maxActivePerEarlyUnion != 1 ||
                !artifactBase.usesSharedAp || !artifactBase.usesIndividualMp || artifactBase.runtimeGenerativeAi ||
                !StringComparer.Ordinal.Equals(artifactBase.requiredFacility, "FACILITY_GATE_RESEARCH_ANNEX") ||
                string.IsNullOrWhiteSpace(artifactBase.weaponFamilyId) ||
                !IsCanonicalBaseForecastRole(artifactBase.baseForecastRole) ||
                !HasCanonicalMaterialCosts(artifactBase.materialCosts))
            {
                error = "CAMPAIGN022_INVOCATION_BASE_LAW_INVALID";
                return false;
            }

            var resolvedWeaponFamilyId = artifactBase.weaponFamilyId;
            var paths = registry.ArtifactPaths.Values
                .Where(candidate => candidate != null &&
                    StringComparer.Ordinal.Equals(candidate.weaponFamilyId, resolvedWeaponFamilyId))
                .OrderBy(candidate => candidate.pathId, StringComparer.Ordinal)
                .ToArray();
            if (paths.Length != 1)
            {
                error = "CAMPAIGN022_INVOCATION_PATH_AMBIGUOUS";
                return false;
            }
            path = paths[0];
            if (!IsCanonicalPath(path))
            {
                error = "CAMPAIGN022_INVOCATION_PATH_LAW_INVALID";
                return false;
            }
            return true;
        }

        public static bool TryValidateArtifact(
            ICampaignRegistry022 registry,
            InvocationArtifactState022 artifact,
            EquipmentItemState item,
            out ArtifactBaseDto022 artifactBase,
            out ArtifactPathDto022 path,
            out string error)
        {
            artifactBase = null;
            path = null;
            error = string.Empty;
            if (artifact == null || item == null)
            {
                error = "CAMPAIGN022_INVOCATION_ITEM_REQUIRED";
                return false;
            }
            if (!TryResolveBaseAndPath(registry, artifact.BaseId, out artifactBase, out path, out error)) return false;
            if (!StringComparer.Ordinal.Equals(artifact.EvolutionPathId, path.pathId))
            {
                error = "CAMPAIGN022_INVOCATION_PATH_MISMATCH";
                return false;
            }
            SpecialRelicInvocationRule001 specialRule;
            var isSpecialRelic = SpecialRelicInvocationRules001.TryGet(item.DefinitionId, out specialRule);
            var definitionMatches = StringComparer.Ordinal.Equals(item.DefinitionId, artifact.BaseId) ||
                (isSpecialRelic && StringComparer.Ordinal.Equals(specialRule.CanonicalBaseId, artifact.BaseId));
            var specialIdentityMatches = !isSpecialRelic ||
                (StringComparer.Ordinal.Equals(item.DisplayName, specialRule.ActiveDisplayName) &&
                 StringComparer.Ordinal.Equals(item.QualityId, SpecialRelicInvocationRules001.QualityId));
            var tagCountMatches = item.EquipmentTags.Count == (isSpecialRelic ? 4 : 2);
            var specialTagsMatch = !isSpecialRelic ||
                (item.EquipmentTags.Contains(SpecialRelicInvocationRules001.SpecialRelicEquipmentTag) &&
                 item.EquipmentTags.Contains(SpecialRelicInvocationRules001.ManualEquipOnlyTag));
            if (!StringComparer.Ordinal.Equals(item.InstanceId, artifact.InstanceId) ||
                !definitionMatches || !specialIdentityMatches || !tagCountMatches || !specialTagsMatch ||
                item.ValidSlotIds.Count != 1 ||
                !StringComparer.Ordinal.Equals(item.ValidSlotIds[0], artifactBase.validSlotId) ||
                item.ConditionBasisPoints <= 0 ||
                !item.EquipmentTags.Contains(InvocationEquipmentTag) ||
                !item.EquipmentTags.Contains(artifactBase.weaponFamilyId))
            {
                error = "CAMPAIGN022_INVOCATION_ITEM_MISMATCH";
                return false;
            }
            if (!TryCanonicalizeAffixIds(registry, artifact.AffixIds, out var canonicalAffixes, out error)) return false;
            if (!canonicalAffixes.SequenceEqual(artifact.AffixIds, StringComparer.Ordinal) ||
                artifact.EvolutionStage < 0 || artifact.EvolutionStage > path.stages.Length || artifact.Resonance < 0)
            {
                error = "CAMPAIGN022_INVOCATION_STATE_INVALID";
                return false;
            }
            if (artifact.EvolutionStage > 0)
            {
                var currentStage = path.stages.SingleOrDefault(stage => stage.stage == artifact.EvolutionStage);
                if (currentStage == null || artifact.Resonance < currentStage.resonanceRequired)
                {
                    error = "CAMPAIGN022_INVOCATION_STATE_INVALID";
                    return false;
                }
            }
            return true;
        }

        public static bool IsCanonicalPath(ArtifactPathDto022 path)
        {
            if (path == null || string.IsNullOrWhiteSpace(path.pathId) || string.IsNullOrWhiteSpace(path.weaponFamilyId) || string.IsNullOrWhiteSpace(path.displayName) ||
                !path.preserveArtifactInstance || !path.noSentientOwnership || !path.voluntaryReleaseAlwaysAvailable ||
                path.stages == null || path.stages.Length != 3) return false;
            var ordered = path.stages.OrderBy(stage => stage.stage).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                var stage = ordered[index];
                if (stage == null || stage.stage != index + 1 || string.IsNullOrWhiteSpace(stage.name) ||
                    stage.resonanceRequired <= 0 || stage.abyssFloorRequired <= 0) return false;
                if (index > 0 && (stage.resonanceRequired <= ordered[index - 1].resonanceRequired ||
                    stage.abyssFloorRequired <= ordered[index - 1].abyssFloorRequired)) return false;
            }
            return true;
        }

        private static bool IsCanonicalBaseForecastRole(string role) =>
            StringComparer.Ordinal.Equals(role, "OFFENSE") ||
            StringComparer.Ordinal.Equals(role, "DEFENSE") ||
            StringComparer.Ordinal.Equals(role, "SUPPORT") ||
            StringComparer.Ordinal.Equals(role, "TACTICAL");

        private static bool HasCanonicalMaterialCosts(IReadOnlyList<MaterialCostDto022> costs)
        {
            if (costs == null || costs.Count != 2) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < costs.Count; index++)
            {
                var cost = costs[index];
                if (cost == null || string.IsNullOrWhiteSpace(cost.materialId) || cost.amount <= 0 || !ids.Add(cost.materialId))
                    return false;
            }
            return true;
        }
    }
}
