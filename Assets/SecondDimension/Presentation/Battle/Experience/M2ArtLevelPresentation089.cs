using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only intensity derived from the already-authoritative Art level
    /// carried by a committed Forecast. It never changes battle state, timing order,
    /// damage, AP/MP, mastery, or legal action selection.
    /// </summary>
    public static class M2ArtLevelPresentation089
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 10;

        public static int ClampLevel(int level) =>
            Mathf.Clamp(level <= 0 ? MinimumLevel : level, MinimumLevel, MaximumLevel);

        public static float EffectScale(int level) =>
            1f + (ClampLevel(level) - MinimumLevel) * 0.04f;

        public static float MotionScale(int level) =>
            1f + (ClampLevel(level) - MinimumLevel) * 0.0125f;

        public static int AccentPulseCount(int level)
        {
            var value = ClampLevel(level);
            if (value >= 10) return 3;
            if (value >= 7) return 2;
            return value >= 4 ? 1 : 0;
        }

        public static string CaptionSuffix(int level)
        {
            var value = ClampLevel(level);
            if (value <= 1) return string.Empty;
            if (value >= 10) return "  ·  LV10 MASTERED";
            if (value >= 7) return "  ·  LV" + value + " SURGING";
            if (value >= 4) return "  ·  LV" + value + " REFINED";
            return "  ·  LV" + value;
        }

        public static Color AccentColor(Color source, int level)
        {
            var value = ClampLevel(level);
            if (value >= 10)
                return Color.Lerp(source, new Color(1f, 0.78f, 0.24f, 1f), 0.56f);
            if (value >= 7)
                return Color.Lerp(source, new Color(0.72f, 0.95f, 1f, 1f), 0.48f);
            return Color.Lerp(source, Color.white, 0.30f);
        }

        public static int ResolveCommittedLevel(
            M2BattleView battle,
            string actorMemberId,
            string artId)
        {
            if (battle?.Forecasts == null ||
                string.IsNullOrWhiteSpace(actorMemberId) ||
                string.IsNullOrWhiteSpace(artId))
                return MinimumLevel;

            var resolved = MinimumLevel;
            for (var forecastIndex = 0; forecastIndex < battle.Forecasts.Count; forecastIndex++)
            {
                var actions = battle.Forecasts[forecastIndex]?.MemberActions;
                if (actions == null) continue;
                for (var actionIndex = 0; actionIndex < actions.Count; actionIndex++)
                {
                    var action = actions[actionIndex];
                    if (action == null ||
                        !StringComparer.Ordinal.Equals(action.ActorMemberId, actorMemberId) ||
                        !StringComparer.Ordinal.Equals(action.ArtId, artId))
                        continue;
                    resolved = Math.Max(resolved, ClampLevel(action.ArtLevel));
                }
            }
            return resolved;
        }
    }
}
