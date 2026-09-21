using System;
using Newtonsoft.Json;
using SecondDimension.Core;

namespace SecondDimension.Gameplay.M1
{
    public enum TutorialDepth
    {
        FullTutorial,
        ContextualTips,
        FastCharter
    }

    [Serializable]
    public sealed class AccessibilitySettingsState
    {
        public AccessibilitySettingsState(int textScalePercent, bool highContrast, bool reducedMotion)
            : this(
                textScalePercent,
                highContrast,
                reducedMotion,
                screenShakePercent: 60,
                flashIntensityPercent: 70,
                combatSpeed: 1,
                autoAdvanceText: false,
                holdAlternatives: "tap toggle available",
                forecastDetail: "full")
        {
        }

        [JsonConstructor]
        public AccessibilitySettingsState(
            int textScalePercent,
            bool highContrast,
            bool reducedMotion,
            int screenShakePercent,
            int flashIntensityPercent,
            int combatSpeed,
            bool autoAdvanceText,
            string holdAlternatives,
            string forecastDetail)
        {
            if (textScalePercent < 85 || textScalePercent > 145)
            {
                throw new ArgumentOutOfRangeException(nameof(textScalePercent));
            }
            if (screenShakePercent < 0 || screenShakePercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(screenShakePercent));
            }
            if (flashIntensityPercent < 0 || flashIntensityPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(flashIntensityPercent));
            }
            if (combatSpeed < 1 || combatSpeed > 4) throw new ArgumentOutOfRangeException(nameof(combatSpeed));
            TextScalePercent = textScalePercent;
            HighContrast = highContrast;
            ReducedMotion = reducedMotion;
            ScreenShakePercent = screenShakePercent;
            FlashIntensityPercent = flashIntensityPercent;
            CombatSpeed = combatSpeed;
            AutoAdvanceText = autoAdvanceText;
            HoldAlternatives = string.IsNullOrWhiteSpace(holdAlternatives)
                ? throw new ArgumentException("Hold alternative is required.", nameof(holdAlternatives))
                : holdAlternatives;
            ForecastDetail = string.IsNullOrWhiteSpace(forecastDetail)
                ? throw new ArgumentException("Forecast detail is required.", nameof(forecastDetail))
                : forecastDetail;
        }

        public int TextScalePercent { get; }
        public bool HighContrast { get; }
        public bool ReducedMotion { get; }
        public int ScreenShakePercent { get; }
        public int FlashIntensityPercent { get; }
        public int CombatSpeed { get; }
        public bool AutoAdvanceText { get; }
        public string HoldAlternatives { get; }
        public string ForecastDetail { get; }

        public static AccessibilitySettingsState Defaults() =>
            new AccessibilitySettingsState(100, highContrast: false, reducedMotion: false);
    }

    [Serializable]
    public sealed class NewGuildProfileState
    {
        [JsonConstructor]
        public NewGuildProfileState(
            string guildmasterName,
            GameMode mode,
            TutorialDepth tutorialDepth,
            AccessibilitySettingsState accessibility,
            bool ironConsequencesAcknowledged)
        {
            GuildmasterName = string.IsNullOrWhiteSpace(guildmasterName)
                ? throw new ArgumentException("Guildmaster name is required.", nameof(guildmasterName))
                : guildmasterName.Trim();
            Mode = mode;
            TutorialDepth = tutorialDepth;
            Accessibility = accessibility ?? throw new ArgumentNullException(nameof(accessibility));
            if (mode == GameMode.Iron && !ironConsequencesAcknowledged)
            {
                throw new ArgumentException(
                    "Iron mode requires visible confirmation of its higher injury and recovery pressure.",
                    nameof(ironConsequencesAcknowledged));
            }
            IronConsequencesAcknowledged = ironConsequencesAcknowledged;
        }

        public string GuildmasterName { get; }
        public GameMode Mode { get; }
        public TutorialDepth TutorialDepth { get; }
        public AccessibilitySettingsState Accessibility { get; }
        public bool IronConsequencesAcknowledged { get; }
    }
}
