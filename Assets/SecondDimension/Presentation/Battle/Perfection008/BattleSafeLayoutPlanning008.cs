using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public static class BattleSafeLayoutPlanning008
    {
        public static BattleSafeLayout008 Plan(
            BattleUnionDeploymentPlan020 deployment,
            float aspectRatio,
            Rect normalizedSafeArea,
            bool commandPhase)
        {
            var safe = Clamp01(normalizedSafeArea.width <= 0f || normalizedSafeArea.height <= 0f
                ? new Rect(0f, 0f, 1f, 1f)
                : normalizedSafeArea);
            var aspect = Mathf.Max(0.75f, aspectRatio);
            var wide = aspect >= 1.95f;
            var compact = aspect < 1.5f;
            var margin = wide ? 0.012f : 0.016f;
            var railWidth = wide ? 0.215f : compact ? 0.145f : 0.108f;
            var panelHeight = commandPhase ? (wide ? 0.185f : 0.205f) : 0.11f;

            var leftNavigator = new Rect(
                safe.xMin + margin,
                safe.yMin + panelHeight + margin,
                safe.width * railWidth,
                safe.height - panelHeight - margin * 2f);
            var rightNavigator = new Rect(
                safe.xMax - margin - safe.width * railWidth,
                safe.yMin + panelHeight + margin,
                safe.width * railWidth,
                safe.height - panelHeight - margin * 2f);

            var battlefield = Rect.MinMaxRect(
                leftNavigator.xMax + margin,
                safe.yMin + panelHeight + margin,
                rightNavigator.xMin - margin,
                safe.yMax - margin);

            var commandWidth = wide ? 0.50f : compact ? 0.70f : 0.58f;
            var commandPanel = new Rect(
                safe.center.x - safe.width * commandWidth * 0.5f,
                safe.yMin + margin,
                safe.width * commandWidth,
                Math.Max(0.01f, panelHeight - margin * 2f));
            var forecastDetails = commandPanel;
            var chips = PlanNavigatorChips(deployment, leftNavigator, rightNavigator, commandPhase || !wide);
            return new BattleSafeLayout008(
                safe, battlefield, leftNavigator, rightNavigator,
                commandPanel, forecastDetails, chips);
        }

        private static IReadOnlyList<BattleNavigatorChip008> PlanNavigatorChips(
            BattleUnionDeploymentPlan020 deployment,
            Rect leftRail,
            Rect rightRail,
            bool singleColumn)
        {
            var result = new List<BattleNavigatorChip008>();
            if (deployment == null) return result.AsReadOnly();

            var columns = singleColumn ? 1 : 2;
            var rows = singleColumn ? 10 : 5;
            var gapX = 0.008f;
            var gapY = 0.008f;
            foreach (var slot in deployment.Slots)
            {
                if (slot == null) continue;
                var rail = slot.Enemy ? rightRail : leftRail;
                var ordinalIndex = Mathf.Clamp(slot.SideOrdinal - 1, 0, 9);
                var column = singleColumn ? 0 : ordinalIndex / rows;
                var row = singleColumn ? ordinalIndex : ordinalIndex % rows;
                var chipWidth = (rail.width - gapX * (columns - 1)) / columns;
                var chipHeight = (rail.height - gapY * (rows - 1)) / rows;
                var x = rail.xMin + column * (chipWidth + gapX);
                var y = rail.yMax - (row + 1) * chipHeight - row * gapY;
                result.Add(new BattleNavigatorChip008(
                    slot.UnionId, slot.Enemy, slot.SideOrdinal,
                    Clamp01(new Rect(x, y, chipWidth, chipHeight))));
            }
            return result.AsReadOnly();
        }

        private static Rect Clamp01(Rect value)
        {
            var xMin = Mathf.Clamp01(Mathf.Min(value.xMin, value.xMax));
            var yMin = Mathf.Clamp01(Mathf.Min(value.yMin, value.yMax));
            var xMax = Mathf.Clamp01(Mathf.Max(value.xMin, value.xMax));
            var yMax = Mathf.Clamp01(Mathf.Max(value.yMin, value.yMax));
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }
    }
}
