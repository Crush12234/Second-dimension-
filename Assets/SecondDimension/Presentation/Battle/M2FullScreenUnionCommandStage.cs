using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Full-screen, non-grid command framing. It uses logical Union relationships
    /// already present in the committed battle view and never writes gameplay state.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        private sealed class BattleCommandFocus
        {
            public M2BattleUnionView Union;
            public M2BattleUnionView Target;
            public IReadOnlyList<M2ForecastView> Forecasts;
            public M2ForecastView Selected;
            public int ActiveOrdinal;
        }

        private BattleCommandFocus ResolveBattleCommandFocus(M2BattleView battle)
        {
            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var union = active.FirstOrDefault(value =>
                            StringComparer.Ordinal.Equals(value.UnionId, _expandedBattleCommandUnionId)) ??
                        active.FirstOrDefault(value => !value.IsSelected) ?? active.FirstOrDefault();
            if (union == null) return null;
            _expandedBattleCommandUnionId = union.UnionId;

            var forecasts = battle.Forecasts
                .Where(value => StringComparer.Ordinal.Equals(value.UnionId, union.UnionId))
                .ToArray();
            var selected = forecasts.FirstOrDefault(value => value.IsSelected);
            var targetId = selected?.TargetId ?? forecasts.FirstOrDefault()?.TargetId;
            var target = battle.EnemyUnions.FirstOrDefault(value =>
                             StringComparer.Ordinal.Equals(value.UnionId, targetId)) ??
                         battle.EnemyUnions.FirstOrDefault(value => value.CanAct) ??
                         battle.EnemyUnions.FirstOrDefault();
            return new BattleCommandFocus
            {
                Union = union,
                Target = target,
                Forecasts = forecasts,
                Selected = selected,
                ActiveOrdinal = Array.FindIndex(active, value =>
                    StringComparer.Ordinal.Equals(value.UnionId, union.UnionId))
            };
        }

        private void BuildCommandPhaseUnionStaging(Transform parent, M2BattleView battle)
        {
            var focus = ResolveBattleCommandFocus(battle);
            if (focus == null)
            {
                BuildPlayerUnionStaging(parent, battle.PlayerUnions, false);
                BuildEnemyUnionStaging(parent, battle.EnemyUnions, false);
                return;
            }

            var fieldWash = AddAnchoredPanel(parent, "Full Screen Tactical Field Wash",
                new Color(0.005f, 0.012f, 0.025f, _highContrast ? 0.18f : 0.06f), Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            fieldWash.raycastTarget = false;

            var wing = battle.PlayerUnions.FirstOrDefault(value => value.CanAct &&
                !StringComparer.Ordinal.Equals(value.UnionId, focus.Union.UnionId));
            // Relationship geometry belongs behind the formations. In the previous
            // build these vectors were later siblings and cut directly through Union
            // names, resources, and faces.
            AddTacticalRelationshipVectors(parent, focus, wing);
            if (wing != null)
            {
                AddCommandPhaseUnionGroup(parent, wing, false, false,
                    new Vector2(0.335f, 0.705f), new Vector2(0.535f, 0.895f),
                    "ALLY WING · " + WingRelationshipLabel(focus, battle));
            }

            var additionalEnemies = battle.EnemyUnions.Where(value => focus.Target == null ||
                !StringComparer.Ordinal.Equals(value.UnionId, focus.Target.UnionId)).Take(1).ToArray();
            if (additionalEnemies.Length > 0)
            {
                AddCommandPhaseUnionGroup(parent, additionalEnemies[0], true, false,
                    new Vector2(0.805f, 0.715f), new Vector2(0.975f, 0.900f),
                    "ENEMY WING · " + additionalEnemies[0].Engagement.ToUpperInvariant());
            }

            AddCommandPhaseUnionGroup(parent, focus.Union, false, true,
                new Vector2(0.305f, 0.235f), new Vector2(0.625f, 0.810f),
                "ACTIVE UNION · " + FocusRelationshipLabel(focus, battle));

            if (focus.Target != null)
            {
                AddCommandPhaseUnionGroup(parent, focus.Target, true, true,
                    new Vector2(0.650f, 0.235f), new Vector2(0.985f, 0.840f),
                    "TARGETED ENEMY UNION · " + EnemyPressureLabel(focus.Target));
            }
            AddTacticalRelationshipLabels(parent, focus, wing);
        }

        private void AddCommandPhaseUnionGroup(
            Transform parent,
            M2BattleUnionView union,
            bool enemy,
            bool focused,
            Vector2 anchorMin,
            Vector2 anchorMax,
            string role)
        {
            var group = AddAnchoredPanel(parent,
                (focused ? "Focused " : "Wing ") + (enemy ? "Enemy" : "Player") + " Formation " + union.UnionId,
                new Color(enemy ? 0.22f : 0.015f, enemy ? 0.02f : 0.10f,
                    enemy ? 0.035f : 0.16f, focused ? 0.065f : 0.035f),
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            group.raycastTarget = false;

            var rail = AddAnchoredPanel(group.transform, "Full Screen Union Rail " + union.UnionId,
                new Color(0.008f, 0.016f, 0.032f, focused ? 0.88f : 0.72f),
                new Vector2(0f, focused ? 0.805f : 0.72f), Vector2.one, Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(rail, focused ? M1PremiumUi.Surface.Iron : M1PremiumUi.Surface.EtchedGlass);
            rail.color = new Color(1f, 1f, 1f, focused ? 0.88f : 0.72f);
            var unionName = AddAnchoredText(rail.transform, "Full Screen Union Name " + union.UnionId,
                union.DisplayName.ToUpperInvariant(), focused ? 36 : 26,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                focused ? new Vector2(0.025f, 0.40f) : new Vector2(0.035f, 0.43f),
                focused ? new Vector2(0.56f, 0.96f) : new Vector2(0.965f, 0.96f));
            M1PremiumUi.ConfigureDisplayText(unionName);
            unionName.resizeTextForBestFit = true;
            unionName.resizeTextMinSize = focused ? 24 : 18;
            unionName.resizeTextMaxSize = focused ? 36 : 26;
            AddAnchoredText(rail.transform, "Full Screen Union Role " + union.UnionId,
                role, focused ? (enemy ? 18 : 22) : 18, TextAnchor.MiddleLeft,
                enemy ? BattleEnemy : BattleCohesion, FontStyle.Bold,
                focused ? new Vector2(0.025f, 0.05f) : new Vector2(0.035f, 0.05f),
                focused ? new Vector2(0.60f, 0.40f) : new Vector2(0.965f, 0.43f));
            if (focused)
            {
                AddAnchoredText(rail.transform, "Full Screen Union Totals " + union.UnionId,
                    "HP " + union.Members.Sum(value => value.CurrentHp) + "/" +
                    union.Members.Sum(value => value.MaximumHp) + "   AP " + union.CurrentAp + "/" + union.MaximumAp +
                    "   COH " + union.Cohesion, 24,
                    TextAnchor.MiddleRight, BattleAp, FontStyle.Bold,
                    new Vector2(0.56f, 0.40f), new Vector2(0.98f, 0.96f));
                AddAnchoredText(rail.transform, "Full Screen Formation State " + union.UnionId,
                    union.Formation.ToUpperInvariant() + " · " + union.Engagement.ToUpperInvariant() +
                    " · " + union.FormationConditionPercent + "%", 22,
                    TextAnchor.MiddleRight,
                    union.FormationBenefitActive ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold, new Vector2(0.58f, 0.05f), new Vector2(0.98f, 0.40f));
            }

            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                CommandFormationSlot(union.Members.Count, memberIndex, focused, enemy,
                    out var memberMin, out var memberMax);
                AddCinematicCombatant(group.transform, union, union.Members[memberIndex], enemy,
                    memberMin, memberMax, memberIndex, 0, focused, false);
            }
        }

        private static void CommandFormationSlot(
            int memberCount,
            int memberIndex,
            bool focused,
            bool enemy,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            if (!focused)
            {
                var count = Math.Max(1, memberCount);
                var width = Mathf.Min(0.33f, 0.94f / count);
                var center = count == 1 ? 0.5f : 0.06f + 0.88f * memberIndex / Math.Max(1, count - 1);
                anchorMin = new Vector2(Mathf.Clamp(center - width * 0.5f, 0.01f, 0.99f - width), 0.01f);
                anchorMax = new Vector2(anchorMin.x + width, 0.72f);
                return;
            }

            if (memberCount <= 1)
            {
                anchorMin = new Vector2(0.20f, 0.015f);
                anchorMax = new Vector2(0.80f, 0.80f);
                return;
            }
            if (memberCount == 2)
            {
                var left = memberIndex == 0;
                anchorMin = new Vector2(left ? 0.04f : 0.48f, left ? 0.01f : 0.08f);
                anchorMax = new Vector2(left ? 0.54f : 0.98f, left ? 0.78f : 0.76f);
                return;
            }

            if (memberIndex == 0)
            {
                anchorMin = new Vector2(0.30f, 0.01f);
                anchorMax = new Vector2(0.72f, 0.79f);
                return;
            }
            if (memberIndex == 1)
            {
                anchorMin = new Vector2(enemy ? 0.60f : 0.00f, 0.12f);
                anchorMax = new Vector2(enemy ? 0.98f : 0.38f, 0.74f);
                return;
            }
            if (memberIndex == 2)
            {
                anchorMin = new Vector2(enemy ? 0.00f : 0.62f, 0.12f);
                anchorMax = new Vector2(enemy ? 0.38f : 1.00f, 0.74f);
                return;
            }

            var remaining = Math.Max(1, memberCount - 3);
            var widthExtra = Mathf.Min(0.25f, 0.92f / remaining);
            var extra = memberIndex - 3;
            var extraCenter = 0.05f + 0.90f * extra / Math.Max(1, remaining - 1);
            anchorMin = new Vector2(Mathf.Clamp(extraCenter - widthExtra * 0.5f, 0.01f, 0.99f - widthExtra), 0.20f);
            anchorMax = new Vector2(anchorMin.x + widthExtra, 0.66f);
        }

        private void AddTacticalRelationshipVectors(
            Transform parent,
            BattleCommandFocus focus,
            M2BattleUnionView wing)
        {
            var deadlock = AddAnchoredPanel(parent, "Deadlock Engagement Vector",
                new Color(0.92f, 0.66f, 0.18f, 0.62f),
                new Vector2(0.590f, 0.465f), new Vector2(0.685f, 0.472f), Vector2.zero, Vector2.zero);
            deadlock.raycastTarget = false;

            if (wing != null)
            {
                var route = AddAnchoredPanel(parent, "Side Strike Engagement Vector",
                    new Color(0.30f, 0.88f, 0.94f, 0.58f),
                    new Vector2(0.515f, 0.625f), new Vector2(0.715f, 0.632f), Vector2.zero, Vector2.zero);
                route.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                route.raycastTarget = false;
            }
        }

        private void AddTacticalRelationshipLabels(
            Transform parent,
            BattleCommandFocus focus,
            M2BattleUnionView wing)
        {
            AddTacticalLabelPlaque(parent, "Deadlock Relationship Plaque",
                new Vector2(0.56f, 0.48f), new Vector2(0.715f, 0.535f));
            AddAnchoredText(parent, "Deadlock Relationship Label",
                focus.Union.Engagement.Equals("Open", StringComparison.OrdinalIgnoreCase)
                    ? "APPROACHING DEADLOCK  ▶"
                    : "DEADLOCK  ▶", 22, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.555f, 0.475f), new Vector2(0.720f, 0.535f));
            if (wing != null)
            {
                AddTacticalLabelPlaque(parent, "Side Strike Relationship Plaque",
                    new Vector2(0.495f, 0.56f), new Vector2(0.695f, 0.62f));
                AddAnchoredText(parent, "Side Strike Relationship Label", "SIDE STRIKE ROUTE  ↘", 22,
                    TextAnchor.MiddleCenter, BattleCohesion, FontStyle.Bold,
                    new Vector2(0.49f, 0.555f), new Vector2(0.70f, 0.62f));
            }
            AddTacticalLabelPlaque(parent, "Enemy Blind Side Plaque",
                new Vector2(0.90f, 0.495f), new Vector2(0.973f, 0.55f));
            AddAnchoredText(parent, "Enemy Blind Side Marker", "BLIND SIDE", 21,
                TextAnchor.MiddleCenter, BattleEnemy, FontStyle.Bold,
                new Vector2(0.895f, 0.49f), new Vector2(0.975f, 0.55f));
        }

        private static void AddTacticalLabelPlaque(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var plaque = AddAnchoredPanel(parent, name, new Color(0.006f, 0.012f, 0.022f, 0.64f),
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            plaque.raycastTarget = false;
        }

        private void BuildFullScreenUnionCommandOverlay(Transform root, M2BattleView battle)
        {
            var focus = ResolveBattleCommandFocus(battle);
            if (focus == null) return;

            var overlay = AddAnchoredPanel(root, "Always Visible Union Command Tray", Color.clear,
                new Vector2(0.008f, 0.012f), new Vector2(0.992f, 0.885f), Vector2.zero, Vector2.zero);
            overlay.raycastTarget = false;

            var commandWell = AddAnchoredPanel(overlay.transform, "Full Screen Union Order List",
                new Color(0.006f, 0.012f, 0.024f, 0.92f),
                new Vector2(0.006f, 0.045f), new Vector2(0.292f, 0.985f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(commandWell, M1PremiumUi.Surface.Iron);
            commandWell.color = new Color(1f, 1f, 1f, _highContrast ? 1f : 0.91f);

            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var denseNavigator = active.Length > 5;
            var tabs = AddAnchoredPanel(commandWell.transform, "Active Union Command Tabs", Color.clear,
                new Vector2(0.025f, denseNavigator ? 0.815f : 0.885f),
                new Vector2(0.975f, 0.985f), Vector2.zero, Vector2.zero);
            for (var index = 0; index < active.Length; index++)
            {
                var captured = active[index];
                UnionNavigatorCell020(index, active.Length, out var cellMin, out var cellMax);
                var chipYInset = denseNavigator ? 0.005f : 0.02f;
                var tab = AddAnchoredButton(tabs.transform, "Focus Union Command " + captured.UnionId,
                    UnionNavigatorLabel020(captured, index, active.Length), () =>
                    {
                        _expandedBattleCommandUnionId = captured.UnionId;
                        BuildCurrentScreen();
                    }, StringComparer.Ordinal.Equals(captured.UnionId, focus.Union.UnionId)
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal,
                    new Vector2(cellMin.x + 0.008f, cellMin.y + chipYInset),
                    new Vector2(cellMax.x - 0.008f, cellMax.y - chipYInset));
                StyleUnionNavigatorChip020(tab, denseNavigator, active.Length > 2 ? 28 : 32);
            }

            AddAnchoredText(commandWell.transform, "Union Command Step 020",
                "UNION " + (focus.ActiveOrdinal + 1) + " OF " + active.Length, 24,
                TextAnchor.MiddleLeft, BattleCohesion, FontStyle.Bold,
                new Vector2(0.045f, denseNavigator ? 0.775f : 0.842f),
                new Vector2(0.955f, denseNavigator ? 0.815f : 0.885f));
            var prompt = AddAnchoredText(commandWell.transform, "Last Remnant Union Order Prompt",
                "CHOOSE AN ORDER FOR " + focus.Union.DisplayName.ToUpperInvariant(), 35,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.045f, denseNavigator ? 0.720f : 0.795f),
                new Vector2(0.955f, denseNavigator ? 0.775f : 0.845f));
            M1PremiumUi.ConfigureDisplayText(prompt);
            AddAnchoredText(commandWell.transform, "Focused Union Resource Decision",
                "AP " + focus.Union.CurrentAp + "/" + focus.Union.MaximumAp + "   ·   COH " + focus.Union.Cohesion +
                "   ·   FORM " + focus.Union.FormationConditionPercent + "%", 32,
                TextAnchor.MiddleLeft, BattleAp, FontStyle.Bold,
                new Vector2(0.045f, denseNavigator ? 0.665f : 0.745f),
                new Vector2(0.955f, denseNavigator ? 0.720f : 0.80f));

            var commands = AddAnchoredPanel(commandWell.transform, "Immediate Complete Union Commands",
                new Color(0.002f, 0.006f, 0.014f, 0.22f),
                new Vector2(0.025f, denseNavigator ? 0.180f : 0.245f),
                new Vector2(0.975f, denseNavigator ? 0.650f : 0.73f), Vector2.zero, Vector2.zero);
            var count = Math.Max(1, focus.Forecasts.Count);
            for (var index = 0; index < focus.Forecasts.Count; index++)
            {
                var forecast = focus.Forecasts[index];
                var top = 1f - (float)index / count;
                var bottom = 1f - (float)(index + 1) / count;
                var button = AddAnchoredButton(commands.transform,
                    "Complete Union Command " + forecast.ForecastId,
                    PlayerCommandLabel(forecast),
                    () => SelectCompleteForecast(forecast.UnionId, forecast.ForecastId),
                    forecast.IsSelected ? TowerRunRules081.LightActionSurfaceColor083 : BattleCommandSurface,
                    new Vector2(0.01f, bottom + 0.012f), new Vector2(0.99f, top - 0.012f));
                StyleBattleCommandButton(button, forecast, count);
                button.interactable = !_battleResolving;
            }

            AddAnchoredText(commandWell.transform, "Full Screen Tactical Opportunity",
                TacticalOpportunityLabel(focus, battle), 29,
                TextAnchor.MiddleCenter, TacticalOpportunityColor(focus, battle), FontStyle.Bold,
                new Vector2(0.04f, denseNavigator ? 0.090f : 0.135f),
                new Vector2(0.96f, denseNavigator ? 0.170f : 0.235f));
            AddAnchoredText(commandWell.transform, "Full Screen Target Intel",
                "TARGET · " + (focus.Target?.DisplayName ?? "BATTLE OBJECTIVE").ToUpperInvariant() +
                "\n" + (focus.Target == null ? "NO TARGET DATA" : EnemyPressureLabel(focus.Target)), 27,
                TextAnchor.MiddleCenter, BattleEnemy, FontStyle.Bold,
                new Vector2(0.04f, denseNavigator ? 0.015f : 0.025f),
                new Vector2(0.96f, denseNavigator ? 0.085f : 0.13f));

            AddFullScreenCommandDetail(overlay.transform, focus);

            var confirm = AddAnchoredButton(overlay.transform, "Confirm Complete Forecast Round",
                battle.CanConfirmRound ? "CONFIRM ROUND" : "ORDER EVERY UNION",
                BeginResolveRound, battle.CanConfirmRound ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                new Vector2(0.835f, 0.02f), new Vector2(0.988f, 0.205f));
            SetButtonFont(confirm, 44);
            confirm.interactable = battle.CanConfirmRound && _coordinator is IM2PresentationCoordinator;
            AddInvocationForecastControls022(overlay.transform,battle,new Vector2(0.835f,0.215f),new Vector2(0.988f,0.43f),18);

            if (isActiveAndEnabled) StartCoroutine(RevealBattleCommandOverlay(overlay.rectTransform));
        }

        private static void UnionNavigatorCell020(
            int index,
            int count,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            var safeCount = Math.Max(1, count);
            if (safeCount <= 5)
            {
                anchorMin = new Vector2((float)index / safeCount, 0f);
                anchorMax = new Vector2((float)(index + 1) / safeCount, 1f);
                return;
            }

            const int columns = 5;
            var rows = Math.Max(2, Mathf.CeilToInt(safeCount / (float)columns));
            var row = index / columns;
            var column = index % columns;
            var maxY = 1f - (float)row / rows;
            var minY = 1f - (float)(row + 1) / rows;
            anchorMin = new Vector2((float)column / columns, minY);
            anchorMax = new Vector2((float)(column + 1) / columns, maxY);
        }

        private static string UnionNavigatorLabel020(M2BattleUnionView union, int index, int count)
        {
            var displayName = CompactUnionNavigatorName086(union?.DisplayName);
            if (count <= 5)
                return (union?.IsSelected == true ? "CHANGE · " : string.Empty) + displayName;
            return (index + 1).ToString("00") + " · " + displayName + "\n" +
                   (union?.IsSelected == true ? "READY · CHANGE" : "ORDER NEEDED");
        }

        private static string CompactUnionNavigatorName086(string displayName)
        {
            var value = string.IsNullOrWhiteSpace(displayName)
                ? "UNION"
                : displayName.Trim().ToUpperInvariant();
            // These story qualifiers are already communicated by the battle and
            // active-Union headers. Removing them here keeps each tab readable at
            // the certified 1280-wide frame without losing the Union's identity.
            foreach (var prefix in new[] { "SKYHOME ", "ZORIN'S " })
                if (value.StartsWith(prefix, StringComparison.Ordinal) &&
                    value.Length > prefix.Length)
                    return value.Substring(prefix.Length);
            return value;
        }

        private static void StyleUnionNavigatorChip020(Button button, bool dense, int regularFontSize)
        {
            SetButtonFont(button, dense ? 17 : Math.Min(22, regularFontSize));
            if (button == null) return;
            var label = button.GetComponentInChildren<Text>();
            if (label == null) return;
            // RuntimeUi buttons reserve generous handset padding. These compact
            // battle tabs need the full width so complete Union names shrink as
            // words instead of breaking at an arbitrary letter at 1280x800.
            label.rectTransform.offsetMin = new Vector2(4f, 3f);
            label.rectTransform.offsetMax = new Vector2(-4f, -3f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = dense ? 9 : 8;
            label.resizeTextMaxSize = dense ? 17 : Math.Min(22, regularFontSize);
            label.lineSpacing = dense ? 0.82f : 0.88f;
        }

        private IEnumerator RevealBattleCommandOverlay(RectTransform overlay)
        {
            if (overlay == null) yield break;
            var group = EnsureBattleComponent<CanvasGroup>(overlay.gameObject);
            if (_reducedMotion)
            {
                group.alpha = 1f;
                yield break;
            }

            var home = overlay.anchoredPosition;
            group.alpha = 0.68f;
            overlay.anchoredPosition = home + new Vector2(-18f, 0f);
            var elapsed = 0f;
            const float duration = 0.16f;
            while (elapsed < duration && overlay != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                group.alpha = Mathf.Lerp(0.68f, 1f, t);
                overlay.anchoredPosition = Vector2.Lerp(home + new Vector2(-18f, 0f), home, t);
                yield return null;
            }
            if (overlay != null)
            {
                group.alpha = 1f;
                overlay.anchoredPosition = home;
            }
        }

        private void AddFullScreenCommandDetail(Transform parent, BattleCommandFocus focus)
        {
            var detail = AddAnchoredPanel(parent, "Selected Complete Command Detail",
                new Color(0.008f, 0.025f, 0.045f, 0.86f),
                new Vector2(0.305f, 0.02f), new Vector2(0.825f, 0.205f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(detail,
                focus.Selected == null ? M1PremiumUi.Surface.EtchedGlass : M1PremiumUi.Surface.WorldRibbon);
            detail.color = new Color(1f, 1f, 1f, _highContrast ? 1f : 0.86f);
            AddAnchoredText(detail.transform, "Predicted Arts Interaction Law",
                "PREDICTED MEMBER ARTS", 19,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.48f, 0.86f), new Vector2(0.98f, 0.985f));
            if (focus.Selected == null)
            {
                AddAnchoredText(detail.transform, "Choose Command Prompt",
                    "SELECT A COMPLETE ORDER\nThe battlefield will preview its outcome here.", 42,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold,
                    new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.82f));
                return;
            }

            var summary = AddAnchoredText(detail.transform, "Selected Command Summary",
                PlayerCommandLabel(focus.Selected) + "  →  " + focus.Selected.TargetName.ToUpperInvariant() +
                "   ·   AP " + focus.Selected.SharedApCost +
                "\n" + focus.Selected.ExpectedEffect,
                24, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.025f, 0.60f), new Vector2(0.98f, 0.86f));
            M1PremiumUi.ConfigureDisplayText(summary);

            var actionCount = Math.Max(1, focus.Selected.MemberActions.Count);
            for (var index = 0; index < focus.Selected.MemberActions.Count; index++)
            {
                var action = focus.Selected.MemberActions[index];
                var minX = 0.025f + 0.95f * index / actionCount;
                var maxX = 0.025f + 0.95f * (index + 1) / actionCount;
                AddAnchoredText(detail.transform, "Predicted Arts Non Clickable " + action.ActorMemberId,
                    CompactPredictedActionCard(action), 18, TextAnchor.MiddleCenter,
                    action.BreakthroughOpportunity ? RuntimeUi.Warning : RuntimeUi.Text,
                    action.BreakthroughOpportunity ? FontStyle.Bold : FontStyle.Normal,
                    new Vector2(minX, 0.25f), new Vector2(maxX, 0.59f));
            }
            AddAnchoredText(detail.transform, "Selected Command Risk Learning",
                "LEARN · " + focus.Selected.LearningOpportunity + "   |   RISK · " + focus.Selected.Risk +
                "\nFALLBACK · " + focus.Selected.FallbackBehavior,
                16, TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Normal,
                new Vector2(0.025f, 0.015f), new Vector2(0.975f, 0.24f));
        }

        private static readonly Color BattleCommandSurface = TowerRunRules081.ForecastSurfaceColor083;

        private static void StyleBattleCommandButton(Button button, M2ForecastView forecast, int count)
        {
            if (button == null || forecast == null) return;
            var selected = forecast.IsSelected;
            var colors = button.colors;
            colors.normalColor = selected ? TowerRunRules081.LightActionSurfaceColor083 : BattleCommandSurface;
            colors.highlightedColor = selected
                ? new Color(0.94f, 0.78f, 0.42f, 1f)
                : new Color(0.11f, 0.21f, 0.30f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = selected
                ? new Color(0.72f, 0.57f, 0.25f, 1f)
                : new Color(0.045f, 0.085f, 0.13f, 1f);
            colors.disabledColor = selected
                ? TowerRunRules081.LightActionDisabledSurfaceColor083
                : TowerRunRules081.ForecastSurfaceColor083;
            button.colors = colors;

            // Battle rows use one family-color rail and the cut-corner edge. The
            // general M1 button skin adds another brass frame and cyan rail to every
            // control; hiding those repeated decorations removes the box-on-box look.
            SetBattleDecorationActive(button.transform, "Premium Button Brass Edge", false);
            SetBattleDecorationActive(button.transform, "Premium Gate Mark", false);
            SetBattleDecorationActive(button.transform, "Premium Selected Gate Mark", false);
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = PlayerCommandLabel(forecast);
                label.font = RuntimeUi.DisplayFont;
                label.fontSize = count >= 5 ? 38 : 46;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleLeft;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = count >= 5 ? 29 : 34;
                label.resizeTextMaxSize = label.fontSize;
                label.rectTransform.anchorMin = new Vector2(0.11f, 0.42f);
                label.rectTransform.anchorMax = new Vector2(0.93f, 0.96f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.color = selected
                    ? TowerRunRules081.LightSurfaceInkColor083
                    : TowerRunRules081.ForecastTextColor083;
            }

            var target = forecast.TargetName ?? "OBJECTIVE";
            if (target.Length > 24) target = target.Substring(0, 23).TrimEnd() + "…";
            var meta = AddAnchoredText(button.transform, "Battle Command Metadata",
                "AP " + forecast.SharedApCost + "   ·   " + target.ToUpperInvariant(), count >= 5 ? 27 : 30,
                TextAnchor.MiddleLeft,
                selected
                    ? TowerRunRules081.LightSurfaceInkColor083
                    : TowerRunRules081.ForecastTextColor083,
                FontStyle.Bold, new Vector2(0.11f, 0.06f), new Vector2(0.93f, 0.45f));
            meta.raycastTarget = false;

            var family = AddAnchoredPanel(button.transform, "Battle Command Family Rail",
                CommandFamilyColor(forecast.CommandId),
                new Vector2(0.018f, 0.14f), new Vector2(0.042f, 0.86f), Vector2.zero, Vector2.zero);
            family.raycastTarget = false;
            AddAnchoredText(button.transform, "Battle Command Chevron", selected ? "◆" : "›", count >= 5 ? 29 : 36,
                TextAnchor.MiddleCenter,
                selected ? new Color(0.10f, 0.085f, 0.04f, 1f) : CommandFamilyColor(forecast.CommandId),
                FontStyle.Bold, new Vector2(0.045f, 0.24f), new Vector2(0.11f, 0.76f)).raycastTarget = false;
        }

        private static void SetBattleDecorationActive(Transform parent, string name, bool active)
        {
            var decoration = parent == null ? null : parent.Find(name);
            if (decoration != null) decoration.gameObject.SetActive(active);
        }

        private static string CompactPredictedActionCard(M2PredictedActionView action)
        {
            var growth = action.BreakthroughOpportunity
                ? "  ·  NEW " + action.BreakthroughTargetArtName
                : action.PredictedGrowth > 0 ? "  ·  LEARN +" + action.PredictedGrowth : string.Empty;
            var level = action.ArtLevel > 0
                ? "  ·  LV" + action.ArtLevel +
                  (string.IsNullOrWhiteSpace(action.ArtPowerCue)
                      ? string.Empty
                      : " " + action.ArtPowerCue)
                : string.Empty;
            return action.ActorName + ": " + action.ArtName + level + "\n→ " + action.TargetName +
                   "\nMP " + action.PersonalMpCost + growth;
        }

        private static string FocusRelationshipLabel(BattleCommandFocus focus, M2BattleView battle)
        {
            if (focus.Selected != null && StringComparer.Ordinal.Equals(focus.Selected.CommandId, "CMD_FLANK"))
                return "BLIND SIDE ATTACK LOCKED";
            if (focus.Union.Engagement.IndexOf("Flank", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FLANKING";
            if (focus.ActiveOrdinal > 0 && battle.PlayerUnions.Count(value => value.CanAct) > 1)
                return "SIDE STRIKE OPEN";
            return focus.Union.Engagement.Equals("Open", StringComparison.OrdinalIgnoreCase)
                ? "LEAD DEADLOCK"
                : focus.Union.Engagement.ToUpperInvariant();
        }

        private static string WingRelationshipLabel(BattleCommandFocus focus, M2BattleView battle) =>
            focus.ActiveOrdinal == 0 && battle.PlayerUnions.Count(value => value.CanAct) > 1
                ? "SIDE STRIKE READY"
                : "HOLDS THE DEADLOCK";

        private static string EnemyPressureLabel(M2BattleUnionView enemy)
        {
            if (enemy == null) return "UNKNOWN";
            if (enemy.Engagement.IndexOf("Rear", StringComparison.OrdinalIgnoreCase) >= 0)
                return "REAR PRESSURE · BLIND SIDE EXPOSED";
            if (enemy.Engagement.IndexOf("Guard", StringComparison.OrdinalIgnoreCase) >= 0)
                return "GUARDED · BLIND SIDE EXPOSED";
            if (enemy.Engagement.IndexOf("Broken", StringComparison.OrdinalIgnoreCase) >= 0)
                return "FORMATION BROKEN";
            return enemy.Engagement.ToUpperInvariant() + " · FRONT FACING";
        }

        private static string TacticalOpportunityLabel(BattleCommandFocus focus, M2BattleView battle)
        {
            if (focus.Selected != null && StringComparer.Ordinal.Equals(focus.Selected.CommandId, "CMD_FLANK"))
                return "BLIND SIDE LOCKED · SIDE STRIKE WILL OPEN REAR PRESSURE";
            if (focus.Forecasts.Any(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_FLANK")))
                return "SIDE STRIKE OPEN · A FLANK ORDER CAN HIT THE BLIND SIDE";
            if (focus.ActiveOrdinal == 0 && battle.PlayerUnions.Count(value => value.CanAct) > 1)
                return "DEADLOCK ROLE · THE OTHER UNION HAS THE SIDE-STRIKE ROUTE";
            return focus.Union.Engagement.ToUpperInvariant() + " · COMPARE COMPLETE UNION ORDERS";
        }

        private static Color TacticalOpportunityColor(BattleCommandFocus focus, M2BattleView battle) =>
            focus.Forecasts.Any(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_FLANK"))
                ? RuntimeUi.Warning
                : BattleCohesion;
    }
}
