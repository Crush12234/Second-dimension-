
#if UNITY_EDITOR
using System;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017G;
using SecondDimension.Presentation.GuildCity017G;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.GuildCity017G
{
    public static class GuildCityOpeningCertificationValidation017G
    {
        [MenuItem("Second Dimension/Guild City 017G/Validate Opening Balance and Tutorial")]
        public static void ValidateFromMenu() => Validate();

        public static void PrepareAndValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        public static void Validate()
        {
            var root = GuildCityOpeningCertificationRegistry017G.Load();
            Require(root.modeProfiles?.Length == 5, "017G requires five campaign mode profiles.");
            Require(root.tutorialSteps?.Length == 15, "017G requires fifteen contextual tutorial steps.");
            Require(root.pacingTargets?.Length == 8, "017G requires eight opening pacing targets.");
            Require(root.scoreCategories?.Length == 12, "017G requires twelve certification categories.");
            Require(root.accessibilityChecks?.Length >= 8, "017G requires the accessibility certification matrix.");
            Require(root.certification != null && root.certification.minimumAverageBasisPoints >= 9000,
                "017G certification average must be at least 9.0/10.");
            Require(GuildCityOpeningBalance017G.All.Count == Enum.GetValues(typeof(GameMode)).Length,
                "Every GameMode must have an opening balance profile.");
            foreach (GameMode mode in Enum.GetValues(typeof(GameMode)))
                Require(GuildCityOpeningBalance017G.For(mode).Mode == mode, "Missing mode profile: " + mode);
            Require(root.tutorialSteps.Select(value => value.id).Distinct(StringComparer.Ordinal).Count() == root.tutorialSteps.Length,
                "Tutorial step IDs must be unique.");
            Debug.Log("Guild City Opening Certification 017G validation PASS: modes=5, steps=15, pacing=8, score categories=12.");
        }

        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
