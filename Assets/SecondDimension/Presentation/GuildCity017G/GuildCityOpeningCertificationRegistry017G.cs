
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation.GuildCity017G
{
    [Serializable] public sealed class GuildCityCertificationRoot017G
    {
        public string contentVersion;
        public GuildCityModeCopy017G[] modeProfiles;
        public GuildCityTutorialCopy017G[] tutorialSteps;
        public GuildCityPacingTarget017G[] pacingTargets;
        public GuildCityScoreCategory017G[] scoreCategories;
        public GuildCityAccessibilityCheck017G[] accessibilityChecks;
        public GuildCityCertificationThreshold017G certification;
    }
    [Serializable] public sealed class GuildCityModeCopy017G
    { public string id; public string displayName; public int startingSupplyBonus; public int checkModifier; public int fatigueCostDelta; public int urgencyCostDelta; public int enemyUnionDelta; public int guildRewardBasisPoints; public int hallRewardBasisPoints; public string playerPromise; }
    [Serializable] public sealed class GuildCityTutorialCopy017G
    { public string id; public string shortBody; public string whyItMatters; public string successCue; }
    [Serializable] public sealed class GuildCityPacingTarget017G
    { public string id; public int earliestMinute; public int targetMinute; public int latestMinute; public string evidence; }
    [Serializable] public sealed class GuildCityScoreCategory017G
    { public string id; public string displayName; public string acceptance; }
    [Serializable] public sealed class GuildCityAccessibilityCheck017G
    { public string id; public string displayName; public string requirement; }
    [Serializable] public sealed class GuildCityCertificationThreshold017G
    { public int minimumAverageBasisPoints; public int minimumCategoryBasisPoints; public int maxSeverityOne; public int maxSeverityTwo; public bool windowsSmokeRequired; public bool ownerApprovalRequired; }

    public static class GuildCityOpeningCertificationRegistry017G
    {
        private const string ResourcePath = "SecondDimension/GuildCity017G/Data/OPENING_BALANCE_TUTORIAL_017G";
        private static GuildCityCertificationRoot017G _root;
        private static Dictionary<string,GuildCityTutorialCopy017G> _steps;
        private static Dictionary<string,GuildCityPacingTarget017G> _pacing;

        public static GuildCityCertificationRoot017G Load()
        {
            if (_root != null) return _root;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing Guild City 017G certification data: " + ResourcePath);
            _root = JsonUtility.FromJson<GuildCityCertificationRoot017G>(asset.text);
            if (_root == null) throw new InvalidOperationException("Guild City 017G certification data did not parse.");
            _steps = Index(_root.tutorialSteps, value => value.id);
            _pacing = Index(_root.pacingTargets, value => value.id);
            return _root;
        }

        public static GuildCityTutorialCopy017G Step(string id)
        {
            Load(); return _steps != null && _steps.TryGetValue(id ?? string.Empty, out var value) ? value : null;
        }

        public static GuildCityPacingTarget017G Pacing(string id)
        {
            Load(); return _pacing != null && _pacing.TryGetValue(id ?? string.Empty, out var value) ? value : null;
        }

        private static Dictionary<string,T> Index<T>(T[] values, Func<T,string> id) where T:class
        {
            var result = new Dictionary<string,T>(StringComparer.Ordinal);
            if (values != null)
                for (var index = 0; index < values.Length; index++)
                    if (values[index] != null) result[id(values[index])] = values[index];
            return result;
        }
    }
}
