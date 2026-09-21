using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M2BattleExperienceController072
    {
        public const float AutoFeedMaximumAnchorY110 = 0.10f;
        public const float BattleContentMinimumAnchorY110 = 0.105f;
        public const float BattleContentMaximumAnchorY110 = 0.985f;
        public const int MaximumAutoFeedLineCharacters110 = 120;
        private const string ExecutedActionPrefix110 = "EXECUTED  •  ";
        private string _liveAutoDecision110 = string.Empty;
        private RectTransform _autoHistoryPanel110;
        private RectTransform _autoHistoryContent110;
        private readonly List<Text> _autoHistoryEntries110 = new List<Text>();
        private ScrollRect _autoHistoryScroll110;

        private void BuildAutoDecisionFeed108()
        {
            // HUD and sprites use the same transform so their existing separation
            // survives the reserved feed strip. The story ribbon remains in root space.
            SetAnchors(_dioramaLayer, new Vector2(0f, BattleContentMinimumAnchorY110),
                new Vector2(1f, BattleContentMaximumAnchorY110));
            SetAnchors(_hudLayer, new Vector2(0f, BattleContentMinimumAnchorY110),
                new Vector2(1f, BattleContentMaximumAnchorY110));
            _autoDecisionPanel108 = RuntimeUi.AddPanel(_root, "Tower Auto Decision Feed 108",
                new Color(0.008f, 0.018f, 0.033f, 0.96f));
            SetAnchors(_autoDecisionPanel108.rectTransform, new Vector2(0.008f, 0.004f),
                new Vector2(0.992f, AutoFeedMaximumAnchorY110));
            _autoDecisionPanel108.raycastTarget = false;
            var textViewport = RuntimeUi.AddStretchRect(_autoDecisionPanel108.transform,
                "Tower Auto Live Text Viewport 110");
            SetAnchors(textViewport, new Vector2(0.012f, 0.04f), new Vector2(0.87f, 0.96f));
            textViewport.gameObject.AddComponent<RectMask2D>();
            _autoDecisionText108 = RuntimeUi.AddText(textViewport,
                "Tower Auto Decision Feed Text 108", string.Empty, 32,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            SetAnchors(_autoDecisionText108.rectTransform, Vector2.zero, Vector2.one);
            _autoDecisionText108.resizeTextForBestFit = true;
            _autoDecisionText108.resizeTextMinSize = 28;
            _autoDecisionText108.resizeTextMaxSize = 32;
            _autoDecisionText108.horizontalOverflow = HorizontalWrapMode.Overflow;
            _autoDecisionText108.verticalOverflow = VerticalWrapMode.Truncate;
            _autoDecisionText108.raycastTarget = false;
            var history = RuntimeUi.AddButton(_autoDecisionPanel108.transform,
                "Tower Auto History Button 110", "HISTORY", OpenAutoHistory110);
            SetAnchors(history.GetComponent<RectTransform>(), new Vector2(0.882f, 0.12f),
                new Vector2(0.99f, 0.88f));
            var label = history.GetComponentInChildren<Text>();
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 28;
            label.resizeTextMaxSize = 32;
            RefreshAutoDecisionFeed108();
        }

        private IReadOnlyDictionary<string, string> CaptureCommittedAutoDecisions110(
            M2BattleView before, M2BattleView after)
        {
            var blocks = BuildCommittedAutoDecisionBlocks108(before, after);
            // Capture at the existing completed command boundary, including unions
            // with no playback beat or skipped animation. This does not read authority.
            foreach (var block in blocks.Values)
                AppendAutoHistory110(_autoDecisionHistory108, block);
            _liveAutoDecision110 = blocks.Values.FirstOrDefault() ?? string.Empty;
            RefreshAutoDecisionFeed108();
            return blocks;
        }

        private void ShowCommittedAutoDecision108(string unionId)
        {
            if (!_autoOrders091 || string.IsNullOrWhiteSpace(unionId) ||
                _pendingAutoDecisions108 == null ||
                !_pendingAutoDecisions108.TryGetValue(unionId, out var decision)) return;
            _liveAutoDecision110 = decision ?? string.Empty;
            RefreshAutoDecisionFeed108();
        }

        private void RefreshAutoDecisionFeed108()
        {
            if (_autoDecisionPanel108 == null || _autoDecisionText108 == null) return;
            if (_resultLayer != null && _resultLayer.gameObject.activeSelf &&
                (_coordinator as M1RuntimeCoordinator)?.CurrentBattleIsTitan161 == true)
            {
                _autoDecisionPanel108.gameObject.SetActive(false);
                CloseAutoHistory110();
                return;
            }
            var visible = _autoOrders091 || _autoDecisionHistory108.Count > 0 ||
                !string.IsNullOrWhiteSpace(_towerRewardNotice108);
            _autoDecisionPanel108.gameObject.SetActive(visible && !CompactBattlePhone164);
            if (!visible) return;
            _autoDecisionText108.text = BuildCompactAutoDecisionText110(
                _liveAutoDecision110, _autoOrders091, AnimationSpeed, _towerRewardNotice108);
            _autoDecisionPanel108.transform.SetAsLastSibling();
            KeepAutoHistoryOnTop110();
        }

        public static void AppendAutoHistory110(Queue<string> history, string entry)
        {
            if (history == null || string.IsNullOrWhiteSpace(entry)) return;
            history.Enqueue(entry);
            while (history.Count > MaximumAutoDecisionHistory108) history.Dequeue();
        }

        public static string[] SnapshotAutoHistory110(IEnumerable<string> history) =>
            (history ?? Array.Empty<string>()).ToArray();

        public static string FormatPlayerAutoHistory110(string entry) =>
            string.Join("\n", (entry ?? string.Empty).Split('\n').Where(line =>
                !line.StartsWith("BATTLE  •  ", StringComparison.Ordinal) &&
                !line.StartsWith("COMMITTED FORECAST  •  ", StringComparison.Ordinal) &&
                !line.StartsWith("FALLBACK  •  ", StringComparison.Ordinal)));

        public static string BuildCompactAutoDecisionText110(
            string committedBlock, bool running, float speed, string savedRewardNotice)
        {
            var lines = (committedBlock ?? string.Empty).Split('\n');
            var command = lines.FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(command)) command = "Waiting for a committed Union order.";
            var actual = lines.Where(line => line.StartsWith(ExecutedActionPrefix110,
                StringComparison.Ordinal)).ToArray();
            var action = actual.Length > 0 ? CompactExecutedAction110(actual[0].Substring(ExecutedActionPrefix110.Length)) :
                lines.Any(line => line.StartsWith("CANCELED  •  ", StringComparison.Ordinal))
                    ? "No confirmed action • canceled action recorded in History."
                    : "No confirmed action • see History for the committed forecast.";
            var more = actual.Length > 1 ? "  (+" + (actual.Length - 1).ToString(
                CultureInfo.InvariantCulture) + " more)" : string.Empty;
            action = Shorten108(action, MaximumAutoFeedLineCharacters110 - more.Length) + more;
            var status = "AUTO " + (running ? "ON" : "OFF") + "  " +
                speed.ToString("0.##", CultureInfo.InvariantCulture) + "×  •  ";
            var reward = string.IsNullOrWhiteSpace(savedRewardNotice)
                ? "No new saved floor rewards."
                : string.Join("  •  ", savedRewardNotice.Split('\n')
                    .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
            return Shorten108(status + command, MaximumAutoFeedLineCharacters110) + "\n" +
                action + "\n" + Shorten108(reward, MaximumAutoFeedLineCharacters110);
        }

        private static string CompactExecutedAction110(string action)
        {
            var parts = action.Split(new[] { " → " }, StringSplitOptions.None);
            if (parts.Length != 3) return action;
            var actor = parts[0];
            var unionDivider = actor.LastIndexOf(" / ", StringComparison.Ordinal);
            if (unionDivider >= 0) actor = actor.Substring(unionDivider + 3);
            return Shorten108(actor, 30) + " → " + Shorten108(parts[1], 30) + " → " + Shorten108(parts[2], 35);
        }

        private void OpenAutoHistory110()
        {
            if (_root == null) return;
            if (_autoHistoryPanel110 == null) BuildAutoHistory110();
            var snapshot = SnapshotAutoHistory110(_autoDecisionHistory108);
            var entries = snapshot.Length == 0
                ? new[] { "No committed Auto orders or saved reward receipts yet." }
                : snapshot.Reverse().Select(FormatPlayerAutoHistory110).ToArray();
            // One Text per entry avoids Unity's single-text mesh vertex ceiling.
            // Reuse at most 64 rows and never rebuild them on live Auto updates.
            for (var index = 0; index < entries.Length; index++)
            {
                if (index == _autoHistoryEntries110.Count)
                {
                    var row = RuntimeUi.AddText(_autoHistoryContent110,
                        "Tower Auto History Entry 110 " + index, string.Empty, 32,
                        TextAnchor.UpperLeft, RuntimeUi.Text);
                    row.verticalOverflow = VerticalWrapMode.Overflow;
                    row.raycastTarget = false;
                    _autoHistoryEntries110.Add(row);
                }
                _autoHistoryEntries110[index].text = entries[index];
                _autoHistoryEntries110[index].gameObject.SetActive(true);
            }
            for (var index = entries.Length; index < _autoHistoryEntries110.Count; index++)
                _autoHistoryEntries110[index].gameObject.SetActive(false);
            _autoHistoryPanel110.gameObject.SetActive(true);
            _autoHistoryPanel110.SetAsLastSibling();
            // Layout only when opened. The snapshot stays stable as Auto continues.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_autoHistoryContent110);
            _autoHistoryScroll110.StopMovement();
            _autoHistoryScroll110.verticalNormalizedPosition = 1f;
        }

        private void CloseAutoHistory110()
        {
            if (_autoHistoryPanel110 != null) _autoHistoryPanel110.gameObject.SetActive(false);
        }

        private void KeepAutoHistoryOnTop110()
        {
            if (_autoHistoryPanel110 != null && _autoHistoryPanel110.gameObject.activeSelf)
                _autoHistoryPanel110.SetAsLastSibling();
        }

        private void BuildAutoHistory110()
        {
            var panel = RuntimeUi.AddPanel(_root, "Tower Auto History 110",
                new Color(0.008f, 0.018f, 0.033f, 0.99f));
            _autoHistoryPanel110 = panel.rectTransform;
            SetAnchors(_autoHistoryPanel110, new Vector2(0.008f, 0.11f), new Vector2(0.992f, 0.995f));
            var title = RuntimeUi.AddText(panel.transform, "Tower Auto History Title 110",
                "AUTO HISTORY  •  SNAPSHOT  •  NEWEST FIRST", 32,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            SetAnchors(title.rectTransform, new Vector2(0.022f, 0.91f), new Vector2(0.72f, 0.99f));
            var back = RuntimeUi.AddButton(panel.transform, "Back To Battle 110",
                "BACK TO BATTLE", CloseAutoHistory110);
            SetAnchors(back.GetComponent<RectTransform>(), new Vector2(0.745f, 0.912f),
                new Vector2(0.98f, 0.989f));
            var viewport = RuntimeUi.AddPanel(panel.transform, "Tower Auto History Viewport 110",
                new Color(0f, 0f, 0f, 0.01f));
            SetAnchors(viewport.rectTransform, new Vector2(0.022f, 0.025f), new Vector2(0.978f, 0.90f));
            viewport.gameObject.AddComponent<RectMask2D>();
            _autoHistoryEntries110.Clear();
            var content = RuntimeUi.AddStretchRect(viewport.transform, "Tower Auto History Content 110");
            _autoHistoryContent110 = content;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _autoHistoryScroll110 = viewport.gameObject.AddComponent<ScrollRect>();
            _autoHistoryScroll110.viewport = viewport.rectTransform;
            _autoHistoryScroll110.content = content;
            _autoHistoryScroll110.horizontal = false;
            _autoHistoryScroll110.vertical = true;
            _autoHistoryScroll110.movementType = ScrollRect.MovementType.Clamped;
            _autoHistoryScroll110.scrollSensitivity = 32f;
        }

        public static IReadOnlyDictionary<string, string> BuildCommittedAutoDecisionBlocks108(
            M2BattleView before, M2BattleView after)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (before == null || after == null) return result;
            var sameBattle = string.IsNullOrWhiteSpace(before.BattleId) ||
                string.IsNullOrWhiteSpace(after.BattleId) ||
                StringComparer.Ordinal.Equals(before.BattleId, after.BattleId);
            var events = sameBattle
                ? (after.LastResolvedRoundEvents ?? Array.Empty<M2BattleEventView>())
                    .Where(value => value != null &&
                        (value.Round <= 0 || before.Round <= 0 || value.Round == before.Round)).ToArray()
                : Array.Empty<M2BattleEventView>();
            foreach (var union in before.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
            {
                if (union == null || string.IsNullOrWhiteSpace(union.UnionId)) continue;
                var selectedId = union.SelectedForecastId;
                var forecast = (before.Forecasts ?? Array.Empty<M2ForecastView>())
                    .FirstOrDefault(value => value != null &&
                        StringComparer.Ordinal.Equals(value.UnionId, union.UnionId) &&
                        (StringComparer.Ordinal.Equals(value.ForecastId, selectedId) ||
                         string.IsNullOrWhiteSpace(selectedId) && value.IsSelected));
                if (forecast == null) continue;
                var unionName = SafeName108(union.DisplayName, union.UnionId);
                var lines = new List<string>
                {
                    "R" + Math.Max(1, before.Round).ToString(CultureInfo.InvariantCulture) +
                    "  •  " + unionName + " → " + SafeName108(forecast.CommandName, forecast.CommandId),
                    "BATTLE  •  " + SafeName108(before.BattleId, "Current battle"),
                    "WHY  •  " + string.Join("  •  ", new[] { forecast.TacticalIntent, forecast.ExpectedEffect }
                        .Where(value => !string.IsNullOrWhiteSpace(value))),
                    "COMMITTED FORECAST  •  " + SafeName108(forecast.ForecastId, forecast.CommandId) +
                    "  •  AP " + forecast.SharedApCost.ToString(CultureInfo.InvariantCulture) +
                    "  •  MP " + forecast.CombinedMpCost.ToString(CultureInfo.InvariantCulture) +
                    "  •  AP recovery " + forecast.ApRecovery.ToString(CultureInfo.InvariantCulture)
                };
                AddForecastDetail110(lines, "PHRASE", forecast.Phrase);
                AddForecastDetail110(lines, "FORECAST TARGET", forecast.TargetName);
                AddForecastDetail110(lines, "RISK", forecast.Risk);
                AddForecastDetail110(lines, "LEARNING", forecast.LearningOpportunity);
                AddForecastDetail110(lines, "FALLBACK", forecast.FallbackBehavior);
                var memberIds = new HashSet<string>((union.Members ?? Array.Empty<M2BattleMemberView>())
                    .Where(value => value != null).Select(value => value.MemberId), StringComparer.Ordinal);
                var actual = events.Where(value => IsActualActionEvent108(value) &&
                    (StringComparer.Ordinal.Equals(value.ActorUnionId, union.UnionId) ||
                     string.IsNullOrWhiteSpace(value.ActorUnionId) && memberIds.Contains(value.ActorMemberId)))
                    .OrderBy(value => value.Sequence).ToArray();
                foreach (var planned in forecast.MemberActions ?? Array.Empty<M2PredictedActionView>())
                {
                    if (planned == null || string.IsNullOrWhiteSpace(planned.ActorMemberId)) continue;
                    var actor = SafeName108(planned.ActorName, BattleMemberName108(before, planned.ActorMemberId));
                    lines.Add("PLANNED  •  " + unionName + " / " + actor + " → " +
                        SafeName108(planned.ArtName, planned.ArtId) + " → " +
                        SafeName108(planned.TargetName, BattleTargetName108(before,
                            planned.TargetUnionId, planned.TargetMemberId)) +
                        "  •  AP " + planned.SharedApCost.ToString(CultureInfo.InvariantCulture) +
                        "  •  MP " + planned.PersonalMpCost.ToString(CultureInfo.InvariantCulture));
                    AddForecastDetail110(lines, "PREDICTION", planned.Prediction);
                    if (actual.Any(value => StringComparer.Ordinal.Equals(
                        value.ActorMemberId, planned.ActorMemberId))) continue;
                    var canceled = events.FirstOrDefault(value =>
                        !string.IsNullOrWhiteSpace(value.EventType) &&
                        value.EventType.StartsWith("ACTION_CANCELED", StringComparison.Ordinal) &&
                        StringComparer.Ordinal.Equals(value.ActorMemberId, planned.ActorMemberId) &&
                        (string.IsNullOrWhiteSpace(value.ActorUnionId) ||
                         StringComparer.Ordinal.Equals(value.ActorUnionId, union.UnionId)));
                    lines.Add(canceled != null
                        ? "CANCELED  •  " + actor + "  •  " + SafeName108(canceled.Text, canceled.EventType)
                        : "UNCONFIRMED  •  " + actor + "  •  No resolved action event recorded.");
                }
                foreach (var action in actual.GroupBy(value => new { value.ActorMemberId, value.ArtId }))
                {
                    var first = action.First();
                    var targets = action.Select(value => BattleTargetName108(after,
                            value.TargetUnionId, value.TargetMemberId))
                        .Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.Ordinal);
                    lines.Add(ExecutedActionPrefix110 + unionName + " / " +
                        SafeName108(BattleMemberName108(after, first.ActorMemberId), first.ActorMemberId) +
                        " → " + SafeName108(M2BattleReadableText021.ArtDisplayName(first.ArtId, first.Text, true),
                            "Art not recorded") + " → " + SafeName108(string.Join(", ", targets), "Target not recorded"));
                }
                result[union.UnionId] = string.Join("\n", lines);
            }
            return result;
        }

        private static void AddForecastDetail110(List<string> lines, string label, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) lines.Add(label + "  •  " + value.Trim());
        }
    }
}
