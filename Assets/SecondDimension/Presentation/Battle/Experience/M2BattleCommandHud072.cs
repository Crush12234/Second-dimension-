using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// A persistent, compact command surface for complete Union Forecasts. The HUD
    /// deliberately exposes no member-level Art buttons and never writes battle state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class M2BattleCommandHud072 : MonoBehaviour
    {
        public const float MaximumTrayAnchorY = 0.250f;
        public const float MinimumUnionSelectorHeight074 = 44f;
        public const float MinimumUnionSelectorWidth074 = 150f;
        public const int MinimumCriticalOrderFontSize074 = 25;
        public const int MaximumSupportedPlayerUnions078 = 10;
        public const int UnionSelectorPageSize078 = 6;
        public const int MaximumFocusedMemberStates078 = 6;
        private const int MaximumVisibleForecasts = 5;
        private const int PagedForecastCount161 = 3;
        private const int MaximumVisibleMemberArts = 3;

        private readonly List<UnionChip> _unionChips = new List<UnionChip>();
        private readonly List<ForecastSlot> _forecastSlots = new List<ForecastSlot>();
        private readonly List<Image> _previewActionIcons = new List<Image>();
        private readonly List<Text> _focusedMemberStates078 = new List<Text>();
        private readonly List<Text> _focusedMemberProgressValues078 = new List<Text>();
        private readonly List<Image> _focusedMemberNextArtRails078 = new List<Image>();
        private readonly List<Image> _focusedMemberNextArtFills078 = new List<Image>();
        private readonly Dictionary<string, string> _optimisticSelections =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private RectTransform _root;
        private Text _activeUnionEyebrow;
        private Text _activeUnionHeading;
        private Text _activeUnionStats;
        private Text _activeUnionState;
        private Button _previousUnionPage078;
        private Button _nextUnionPage078;
        private Text _previousUnionPageLabel078;
        private Text _nextUnionPageLabel078;
        private Text _previewHeading;
        private Text _previewSummary;
        private Text _previewActions;
        private Button _confirmButton;
        private Text _confirmLabel;
        private Action<string> _focusUnion;
        private Action<string, string> _previewForecast;
        private Action<string, string> _selectForecast;
        private Action _confirm;
        private M2BattleView _battle;
        private bool _interactable = true;
        private string _previewUnionId = string.Empty;
        private string _previewForecastId = string.Empty;
        private int _unionPageStart078;
        private int _forecastPageStart161;
        private int _forecastTotal161;
        private string _forecastPageKey161 = string.Empty;
        private string _lastForecastSelection161 = string.Empty;
        private Button _previousForecastPage161;
        private Button _nextForecastPage161;

        public string FocusedUnionId { get; private set; } = string.Empty;

        public void Initialize(
            RectTransform host,
            Action<string> focusUnion,
            Action<string, string> previewForecast,
            Action<string, string> selectForecast,
            Action confirm)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            DisposeVisualRoot();
            _phoneUnionDetails164=null;_phonePreviewDetails164=null;_phoneLastUnion164=null;_phoneLastPreview164=null;_phoneHudApplied164=false;
            _focusUnion = focusUnion;
            _previewForecast = previewForecast;
            _selectForecast = selectForecast;
            _confirm = confirm;
            _unionChips.Clear();
            _forecastSlots.Clear();
            _previewActionIcons.Clear();
            _focusedMemberStates078.Clear();
            _focusedMemberProgressValues078.Clear();
            _focusedMemberNextArtRails078.Clear();
            _focusedMemberNextArtFills078.Clear();
            _optimisticSelections.Clear();
            _battle = null;
            FocusedUnionId = string.Empty;
            _previewUnionId = string.Empty;
            _previewForecastId = string.Empty;
            _unionPageStart078 = 0;
            _forecastPageStart161 = 0;
            _forecastTotal161 = 0;
            _forecastPageKey161 = string.Empty;
            _lastForecastSelection161 = string.Empty;

            var tray = RuntimeUi.AddPanel(host, "Battle Command HUD 072", new Color(0.015f, 0.026f, 0.045f, 0.965f));
            _root = tray.rectTransform;
            Anchor(_root, new Vector2(0.008f, 0.008f),
                new Vector2(0.992f, MaximumTrayAnchorY));
            M1PremiumUi.StylePanel(tray, M1PremiumUi.Surface.Iron);

            var unionRail = RuntimeUi.AddPanel(_root, "Focused Union Rail 072", new Color(0.025f, 0.055f, 0.085f, 0.94f));
            Anchor(unionRail.rectTransform, new Vector2(0.008f, 0.000f), new Vector2(0.270f, 0.720f),
                new Vector2(4f, 0f), new Vector2(-4f, 0f));
            M1PremiumUi.StylePanel(unionRail, M1PremiumUi.Surface.EtchedGlass);
            BuildActiveUnionPanel(unionRail.rectTransform);

            var orders = RuntimeUi.AddPanel(_root, "Complete Forecast Orders 072", new Color(0.008f, 0.018f, 0.033f, 0.90f));
            Anchor(orders.rectTransform, new Vector2(0.278f, 0.025f), new Vector2(0.692f, 0.655f),
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            CreateForecastSlots(orders.rectTransform);

            var preview = RuntimeUi.AddPanel(_root, "Selected Forecast Preview 072", new Color(0.02f, 0.045f, 0.072f, 0.98f));
            Anchor(preview.rectTransform, new Vector2(0.700f, 0.025f), new Vector2(0.868f, 0.655f),
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            M1PremiumUi.StylePanel(preview, M1PremiumUi.Surface.EtchedGlass);
            BuildPreview(preview.rectTransform);

            _confirmButton = RuntimeUi.AddButton(
                _root,
                "Confirm Complete Union Forecasts 072",
                "SELECT ORDERS",
                HandleConfirm,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            Anchor(_confirmButton.GetComponent<RectTransform>(), new Vector2(0.878f, 0.025f), new Vector2(0.992f, 0.655f),
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            _confirmLabel = _confirmButton.GetComponentInChildren<Text>();
            ConfigureButtonLabel(_confirmLabel, 42, 27);

            // The navigator keeps six large targets per page. Forces seven through ten
            // use a second page so 1280x800 never shrinks a Union below touch/readability
            // minimums, while every deployed Union remains reachable by mouse or pad.
            var navigator = RuntimeUi.AddPanel(_root, "Union Order Navigator 074",
                new Color(0.006f, 0.015f, 0.029f, 0.94f));
            Anchor(navigator.rectTransform, new Vector2(0.008f, 0.725f), new Vector2(0.992f, 0.995f));
            CreateUnionChips(navigator.rectTransform);
            SetInteractable(true);
            ApplyPhoneLayout164();
        }

        public void Refresh(M2BattleView battle, string focusedUnionId)
        {
            _battle = battle;
            if (_root == null) return;

            var unions = battle == null || battle.PlayerUnions == null
                ? Array.Empty<M2BattleUnionView>()
                : battle.PlayerUnions;
            var resolvedFocus = ResolveFocusedUnionId(unions, focusedUnionId);
            var focusChanged078 = !StringComparer.Ordinal.Equals(resolvedFocus, FocusedUnionId);
            if (focusChanged078)
            {
                _previewUnionId = string.Empty;
                _previewForecastId = string.Empty;
                ShowFocusedUnionPage078(unions, resolvedFocus);
            }
            FocusedUnionId = resolvedFocus;

            RefreshUnionChips(unions);
            var focusedUnion = FindUnion(unions, FocusedUnionId);
            var forecasts = CollectForecasts(battle, FocusedUnionId);
            RefreshActiveUnionPanel(
                unions,
                focusedUnion,
                FindPreviewForecast(forecasts, focusedUnion));
            RefreshForecastSlots(forecasts, focusedUnion);
            RefreshPreview(forecasts, focusedUnion);
            RefreshConfirmButton(battle);
            EnsureControllerFocus();
            ApplyPhoneLayout164();
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            RefreshButtonInteractivity();
        }

        public void RejectPendingSelection(string unionId)
        {
            if (!string.IsNullOrWhiteSpace(unionId)) _optimisticSelections.Remove(unionId);
            if (_battle != null) Refresh(_battle, FocusedUnionId);
        }

        private void BuildActiveUnionPanel(RectTransform parent)
        {
            // Keep the name/ordinal and tactical context on one honest line each. At
            // 1280x800 the former 9.5%-high header was only ~12.6px tall, so Unity
            // wrapped the eyebrow and silently rendered a two-line 32px layout in it.
            _activeUnionEyebrow = AddAnchoredText(parent, "Active Union Eyebrow 074", "FORMATION • ENGAGEMENT", 12,
                TextAnchor.MiddleRight, RuntimeUi.Accent, FontStyle.Bold,
                new Vector2(0.435f, 0.877f), new Vector2(0.975f, 0.995f));
            ConfigureTextFit(_activeUnionEyebrow, 12, 10);
            SetTextInsets078(_activeUnionEyebrow, 5f);
            _activeUnionHeading = AddAnchoredText(parent, "Active Union Heading 074", "Union", 12,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.025f, 0.877f), new Vector2(0.435f, 0.995f));
            ConfigureTextFit(_activeUnionHeading, 12, 10);
            SetTextInsets078(_activeUnionHeading, 5f);
            _activeUnionStats = AddAnchoredText(parent, "Active Union HP AP Members 074", string.Empty, 20,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.008f, 0.702f), new Vector2(0.992f, 0.877f));
            ConfigureTextFit(_activeUnionStats, 20, 20);
            SetTextInsets078(_activeUnionStats, 1f);
            _activeUnionState = AddAnchoredText(parent, "Active Union Formation Engagement 074", string.Empty, 12,
                TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.035f, 0.480f), new Vector2(0.965f, 0.480f));
            ConfigureTextFit(_activeUnionState, 12, 10);
            SetTextInsets078(_activeUnionState, 5f);

            for (var index = 0; index < MaximumFocusedMemberStates078; index++)
            {
                var row = index / 2;
                var column = index % 2;
                var left = column == 0 ? 0.035f : 0.515f;
                var right = column == 0 ? 0.495f : 0.965f;
                // Each cell owns two independent one-line Text objects. Unity's
                // vertical truncation can silently discard the second line of one
                // multiline Text at 1280x800, even when the source contains '\n'.
                // Dedicated identity/HP and next-Art labels make both lines explicit.
                const float memberRowHeight078 = 0.234f;
                const float progressRailHeight078 = 0.031f;
                var top = 0.702f - row * memberRowHeight078;
                var bottom = top - memberRowHeight078;
                var lowerTextBottom = bottom + progressRailHeight078;
                var middle = lowerTextBottom + (top - lowerTextBottom) * 0.50f;
                var progressRail = RuntimeUi.AddPanel(
                    parent,
                    "Active Union Member Next Art Progress Rail " +
                    (index + 1).ToString(CultureInfo.InvariantCulture) + " 078",
                    new Color(0.012f, 0.048f, 0.052f, 0.92f));
                Anchor(
                    progressRail.rectTransform,
                    new Vector2(left, bottom),
                    new Vector2(right, lowerTextBottom),
                    new Vector2(2f, 0f),
                    new Vector2(-2f, 0f));
                progressRail.raycastTarget = false;
                var progressFill = RuntimeUi.AddPanel(
                    progressRail.transform,
                    "Active Union Member Next Art Progress Fill " +
                    (index + 1).ToString(CultureInfo.InvariantCulture) + " 078",
                    new Color(0.30f, 1f, 0.70f, 0.96f));
                Anchor(progressFill.rectTransform, Vector2.zero, Vector2.one);
                progressFill.raycastTarget = false;
                progressRail.gameObject.SetActive(false);
                _focusedMemberNextArtRails078.Add(progressRail);
                _focusedMemberNextArtFills078.Add(progressFill);
                var cell = AddAnchoredText(
                    parent,
                    "Active Union Member State " +
                    (index + 1).ToString(CultureInfo.InvariantCulture) + " 078",
                    string.Empty,
                    12,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text,
                    FontStyle.Bold,
                    new Vector2(left, middle),
                    new Vector2(right, top));
                ConfigureTextFit(cell, 12, 10);
                SetTextInsets078(cell, 2f);
                cell.gameObject.SetActive(false);
                _focusedMemberStates078.Add(cell);
                var progressValue = AddAnchoredText(
                    parent,
                    "Active Union Member Next Art Progress Value " +
                    (index + 1).ToString(CultureInfo.InvariantCulture) + " 078",
                    string.Empty,
                    11,
                    TextAnchor.MiddleLeft,
                    new Color(0.66f, 1f, 0.84f, 1f),
                    FontStyle.Bold,
                    new Vector2(left, lowerTextBottom),
                    new Vector2(right, middle));
                ConfigureTextFit(progressValue, 11, 10);
                SetTextInsets078(progressValue, 2f);
                progressValue.gameObject.SetActive(false);
                _focusedMemberProgressValues078.Add(progressValue);
            }
        }

        private void CreateUnionChips(RectTransform parent)
        {
            for (var index = 0; index < UnionSelectorPageSize078; index++)
            {
                var capturedIndex = index;
                var button = RuntimeUi.AddButton(
                    parent,
                    "Union Focus Chip " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 072",
                    "UNION",
                    () => HandleUnionFocus(capturedIndex),
                    RuntimeUi.MinimumTouchPixels,
                    RuntimeUi.ButtonNormal);
                var text = button.GetComponentInChildren<Text>();
                ConfigureButtonLabel(text, 27, 19);
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.Automatic;
                button.navigation = navigation;
                _unionChips.Add(new UnionChip(button, text));
            }

            _previousUnionPage078 = RuntimeUi.AddButton(
                parent,
                "Previous Union Page 078",
                "<",
                () => ChangeUnionPage078(-1),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            _previousUnionPageLabel078 = _previousUnionPage078.GetComponentInChildren<Text>();
            ConfigureButtonLabel(_previousUnionPageLabel078, 24, 18);
            _previousUnionPage078.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            _nextUnionPage078 = RuntimeUi.AddButton(
                parent,
                "Next Union Page 078",
                ">",
                () => ChangeUnionPage078(1),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.ButtonNormal);
            _nextUnionPageLabel078 = _nextUnionPage078.GetComponentInChildren<Text>();
            ConfigureButtonLabel(_nextUnionPageLabel078, 24, 18);
            _nextUnionPage078.navigation = new Navigation { mode = Navigation.Mode.Automatic };

            LayoutUnionNavigator(_unionChips, UnionSelectorPageSize078, paging: false);
            Anchor(_previousUnionPage078.GetComponent<RectTransform>(),
                new Vector2(0.004f, 0.055f), new Vector2(0.046f, 0.945f));
            Anchor(_nextUnionPage078.GetComponent<RectTransform>(),
                new Vector2(0.954f, 0.055f), new Vector2(0.996f, 0.945f));
            _previousUnionPage078.gameObject.SetActive(false);
            _nextUnionPage078.gameObject.SetActive(false);
        }

        private void CreateForecastSlots(RectTransform parent)
        {
            for (var index = 0; index < MaximumVisibleForecasts; index++)
            {
                var capturedIndex = index;
                var button = RuntimeUi.AddButton(
                    parent,
                    "Complete Forecast Order " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 072",
                    "ORDER",
                    () => HandleForecastSelection(capturedIndex),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.ButtonNormal);
                var text = button.GetComponentInChildren<Text>();
                ConfigureButtonLabel(text, 39, MinimumCriticalOrderFontSize074);
                // Forecast cards are narrower than full-page buttons. Preserve the
                // readable font floor while recovering room for whole command words.
                text.rectTransform.offsetMin = new Vector2(4f, 4f);
                text.rectTransform.offsetMax = new Vector2(-4f, -4f);
                text.alignment = TextAnchor.MiddleCenter;
                text.lineSpacing = 0.90f;
                var slot = new ForecastSlot(button, text);
                _forecastSlots.Add(slot);
                AddForecastFocusTrigger(button, capturedIndex, EventTriggerType.PointerEnter);
                AddForecastFocusTrigger(button, capturedIndex, EventTriggerType.Select);
            }
            LayoutActiveButtons(_forecastSlots, MaximumVisibleForecasts);
            // Overflow keeps three complete Forecast cards between Previous and
            // Next. Paging controls need less width than an Art name and its cost.
            _previousForecastPage161 = RuntimeUi.AddButton(parent,
                "Previous Forecast Page 161", "‹", () => ChangeForecastPage161(-1),
                RuntimeUi.PrimaryTouchPixels, RuntimeUi.ButtonNormal);
            _nextForecastPage161 = RuntimeUi.AddButton(parent,
                "Next Forecast Page 161", "›", () => ChangeForecastPage161(1),
                RuntimeUi.PrimaryTouchPixels, RuntimeUi.ButtonNormal);
            foreach (var pageButton in new[] { _previousForecastPage161, _nextForecastPage161 })
            {
                var pageLabel = pageButton.GetComponentInChildren<Text>();
                ConfigureButtonLabel(pageLabel, 32, 24);
                pageLabel.rectTransform.offsetMin = new Vector2(4f, 4f);
                pageLabel.rectTransform.offsetMax = new Vector2(-4f, -4f);
                pageButton.navigation = new Navigation { mode = Navigation.Mode.Automatic };
                pageButton.gameObject.SetActive(false);
            }
        }

        private void AddForecastFocusTrigger(Button button, int slotIndex, EventTriggerType eventType)
        {
            if (button == null) return;
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry
            {
                eventID = eventType,
                callback = new EventTrigger.TriggerEvent()
            };
            entry.callback.AddListener(_ => HandleForecastFocus(slotIndex));
            trigger.triggers.Add(entry);
        }

        private void BuildPreview(RectTransform parent)
        {
            _previewHeading = AddAnchoredText(parent, "Forecast Preview Heading 072", "Choose an order", 36,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.055f, 0.78f), new Vector2(0.945f, 0.975f));
            ConfigureTextFit(_previewHeading, 32, 22);

            _previewSummary = AddAnchoredText(parent, "Forecast Target Cost Effect 072",
                "Select an order to inspect the result.", 27,
                TextAnchor.UpperLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.055f, 0.46f), new Vector2(0.945f, 0.79f));
            ConfigureTextFit(_previewSummary, 23, 16);

            _previewActions = AddAnchoredText(parent, "Predicted Member Arts Preview 072",
                "Predicted Arts appear here.", 23,
                TextAnchor.UpperLeft, RuntimeUi.Text, FontStyle.Normal,
                new Vector2(0.235f, 0.035f), new Vector2(0.945f, 0.47f));
            ConfigureTextFit(_previewActions, 20, 14);

            // These icons are semantic (combat, mystic, restoration, warding,
            // tactical, breakthrough), never member-level command buttons. They let
            // a cold player parse each forecast at a glance while preserving the
            // Last Remnant rule that the complete Union order is the selectable unit.
            for (var index = 0; index < MaximumVisibleMemberArts; index++)
            {
                var image = RuntimeUi.AddPanel(parent,
                    "Predicted Member Art Icon " + (index + 1).ToString(CultureInfo.InvariantCulture) + " 076",
                    Color.clear);
                var top = 0.455f - index * 0.142f;
                Anchor(image.rectTransform,
                    new Vector2(0.060f, top - 0.115f),
                    new Vector2(0.205f, top));
                image.preserveAspect = true;
                image.raycastTarget = false;
                image.gameObject.SetActive(false);
                _previewActionIcons.Add(image);
            }
        }

        private void RefreshActiveUnionPanel(
            IReadOnlyList<M2BattleUnionView> unions,
            M2BattleUnionView focusedUnion,
            M2ForecastView previewForecast)
        {
            var ordinal = 0;
            for (var index = 0; index < unions.Count; index++)
                if (unions[index] != null &&
                    StringComparer.Ordinal.Equals(unions[index].UnionId, focusedUnion?.UnionId))
                {
                    ordinal = index + 1;
                    break;
                }

            var formation = Friendly(focusedUnion?.Formation, "Formation");
            var engagement = M2BattleTacticalReadout074.EngagementLabel(focusedUnion, null);
            if (_activeUnionEyebrow != null)
            {
                _activeUnionEyebrow.text = Compact(formation, 16).ToUpperInvariant() +
                                           " • " + engagement;
                _activeUnionEyebrow.color = engagement == "UNION BROKEN"
                    ? RuntimeUi.Warning
                    : RuntimeUi.Accent;
            }
            if (_activeUnionHeading != null)
                _activeUnionHeading.text = ActiveUnionHeadingForVerification079(
                    focusedUnion?.DisplayName,
                    ordinal,
                    unions.Count);
            if (_activeUnionStats != null)
            {
                var currentHp = M2BattleTacticalReadout074.CurrentHp(focusedUnion);
                var maximumHp = M2BattleTacticalReadout074.MaximumHp(focusedUnion);
                var living = M2BattleTacticalReadout074.LivingMembers(focusedUnion);
                var members = focusedUnion?.Members?.Count ?? 0;
                _activeUnionStats.text = "HP" + currentHp.ToString(CultureInfo.InvariantCulture) + "/" +
                                         maximumHp.ToString(CultureInfo.InvariantCulture) + " AP" +
                                         Math.Max(0, focusedUnion?.CurrentAp ?? 0).ToString(CultureInfo.InvariantCulture) + "/" +
                                         Math.Max(0, focusedUnion?.MaximumAp ?? 0).ToString(CultureInfo.InvariantCulture) +
                                         " UP" + living.ToString(CultureInfo.InvariantCulture) + "/" +
                                         members.ToString(CultureInfo.InvariantCulture);
            }
            if (_activeUnionState != null)
            {
                _activeUnionState.text = string.Empty;
            }
            RefreshFocusedMemberStates078(focusedUnion, previewForecast);
        }

        public static string ActiveUnionHeadingForVerification079(
            string displayName,
            int ordinal,
            int unionCount)
        {
            var focusedUnionName = Friendly(displayName, "Choose a Union");
            var redundantOrdinalSuffix = " Union " + ordinal.ToString(CultureInfo.InvariantCulture);
            if (ordinal > 0 && focusedUnionName.EndsWith(
                    redundantOrdinalSuffix,
                    StringComparison.OrdinalIgnoreCase))
                focusedUnionName = focusedUnionName.Substring(
                    0,
                    focusedUnionName.Length - redundantOrdinalSuffix.Length).TrimEnd();
            return Math.Max(0, ordinal).ToString("00", CultureInfo.InvariantCulture) +
                   "/" + Math.Max(1, unionCount).ToString("00", CultureInfo.InvariantCulture) +
                   " " + Compact(focusedUnionName, 18).ToUpperInvariant();
        }

        private void RefreshFocusedMemberStates078(
            M2BattleUnionView union,
            M2ForecastView forecast)
        {
            for (var index = 0; index < _focusedMemberStates078.Count; index++)
            {
                var label = _focusedMemberStates078[index];
                var progressLabel = index < _focusedMemberProgressValues078.Count
                    ? _focusedMemberProgressValues078[index]
                    : null;
                var member = union?.Members != null && index < union.Members.Count
                    ? union.Members[index]
                    : null;
                if (label == null) continue;
                label.gameObject.SetActive(member != null);
                if (progressLabel != null) progressLabel.gameObject.SetActive(member != null);
                if (member == null)
                {
                    label.text = string.Empty;
                    if (progressLabel != null) progressLabel.text = string.Empty;
                    RefreshFocusedMemberNextArtProgress078(index, null);
                    continue;
                }

                var action = PredictedActionForMember078(forecast, member, index);
                label.text = FocusedMemberIdentityLabel078(member);
                if (progressLabel != null)
                    progressLabel.text = FocusedMemberProgressLabel078(member, action);
                label.color = member.Downed || member.CurrentHp <= 0
                    ? RuntimeUi.Warning
                    : RuntimeUi.Text;
                RefreshFocusedMemberNextArtProgress078(index, member.NextSkillProgress);
            }
        }

        private void RefreshFocusedMemberNextArtProgress078(
            int index,
            M2BattleSkillProgressView progress)
        {
            if (index < 0 || index >= _focusedMemberNextArtRails078.Count ||
                index >= _focusedMemberNextArtFills078.Count)
                return;
            var rail = _focusedMemberNextArtRails078[index];
            var fill = _focusedMemberNextArtFills078[index];
            var visible = progress != null && !string.IsNullOrWhiteSpace(progress.DisplayName);
            if (rail != null) rail.gameObject.SetActive(visible);
            if (fill == null) return;
            var rect = fill.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(
                visible ? FocusedMemberNextArtProgressRatio078(progress) : 0f,
                1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string FocusedMemberIdentityLabel078(M2BattleMemberView member)
        {
            if (member == null) return string.Empty;
            var hp = member.Downed || member.CurrentHp <= 0
                ? "DOWN"
                : "HP" + Math.Max(0, member.CurrentHp).ToString(CultureInfo.InvariantCulture) + "/" +
                  Math.Max(0, member.MaximumHp).ToString(CultureInfo.InvariantCulture);
            var rawName = FirstName076(Friendly(member.DisplayName, "Member")).ToUpperInvariant();
            var name = CompactMemberToken078(rawName, 12, "MEMBER");
            return name + " • " + hp;
        }

        private static string FocusedMemberProgressLabel078(
            M2BattleMemberView member,
            M2PredictedActionView action)
        {
            if (member == null) return string.Empty;
            var progress = FocusedMemberNextArtProgressFraction078(member.NextSkillProgress);
            return string.IsNullOrWhiteSpace(progress)
                ? "ORDER  •  " + MemberArtState078(member, action, 14)
                : "NEXT ART  •  " + progress.Replace("/", " / ");
        }

        public static float FocusedMemberNextArtProgressRatio078(M2BattleSkillProgressView progress)
        {
            if (progress == null) return 0f;
            if (progress.RequiredPoints <= 0) return 1f;
            return Mathf.Clamp01(Math.Max(0, progress.CurrentPoints) /
                                 (float)progress.RequiredPoints);
        }

        public static string FocusedMemberNextArtProgressFraction078(
            M2BattleSkillProgressView progress)
        {
            if (progress == null || string.IsNullOrWhiteSpace(progress.DisplayName))
                return string.Empty;
            var required = Math.Max(0, progress.RequiredPoints);
            if (required <= 0) return "READY";
            var current = Math.Min(Math.Max(0, progress.CurrentPoints), required);
            return current.ToString(CultureInfo.InvariantCulture) + "/" +
                   required.ToString(CultureInfo.InvariantCulture);
        }

        public static string FocusedMemberStateLabel078(
            M2BattleMemberView member,
            M2PredictedActionView action)
        {
            if (member == null) return string.Empty;
            var name = FirstName076(Friendly(member.DisplayName, "Member")).ToUpperInvariant();
            var hp = member.Downed || member.CurrentHp <= 0
                ? "DOWN"
                : "HP" + Math.Max(0, member.CurrentHp).ToString(CultureInfo.InvariantCulture) + "/" +
                  Math.Max(0, member.MaximumHp).ToString(CultureInfo.InvariantCulture);
            var constrainedVitals = hp.Length >= 10;
            var compactVitals = hp.Length >= 9;
            name = CompactMemberToken078(name, constrainedVitals ? 3 : compactVitals ? 4 : 6, "MEM");
            var art = MemberArtState078(member, action, constrainedVitals ? 5 : compactVitals ? 7 : 9);
            return compactVitals
                ? name + " " + hp + "·" + art
                : name + " " + hp + " · " + art;
        }

        private static M2PredictedActionView PredictedActionForMember078(
            M2ForecastView forecast,
            M2BattleMemberView member,
            int memberIndex)
        {
            if (forecast?.MemberActions == null || member == null) return null;
            for (var index = 0; index < forecast.MemberActions.Count; index++)
            {
                var candidate = forecast.MemberActions[index];
                if (candidate != null && !string.IsNullOrWhiteSpace(candidate.ActorMemberId) &&
                    StringComparer.Ordinal.Equals(candidate.ActorMemberId, member.MemberId))
                    return candidate;
            }
            for (var index = 0; index < forecast.MemberActions.Count; index++)
            {
                var candidate = forecast.MemberActions[index];
                if (candidate != null && !string.IsNullOrWhiteSpace(candidate.ActorName) &&
                    StringComparer.OrdinalIgnoreCase.Equals(candidate.ActorName, member.DisplayName))
                    return candidate;
            }
            return memberIndex >= 0 && memberIndex < forecast.MemberActions.Count
                ? forecast.MemberActions[memberIndex]
                : null;
        }

        private static string MemberArtState078(
            M2BattleMemberView member,
            M2PredictedActionView action,
            int maximumCharacters)
        {
            var value = action == null
                ? string.Empty
                : M2BattleReadableText021.ArtDisplayName(
                    action.ArtId,
                    string.Empty,
                    false,
                    action.ArtName);
            if (string.IsNullOrWhiteSpace(value)) value = member.NextSkillProgress?.DisplayName;
            if (string.IsNullOrWhiteSpace(value)) value = member.ArtGrowthSummary;
            if (string.IsNullOrWhiteSpace(value)) value = "Ready";
            var prefix = action != null && action.BreakthroughOpportunity ? "★" : string.Empty;
            return prefix + CompactMemberToken078(
                value.ToUpperInvariant(),
                maximumCharacters,
                "READY");
        }

        private static string CompactMemberToken078(string value, int maximum, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var normalized = value.Trim();
            if (normalized.Length <= maximum) return normalized;
            var separator = normalized.IndexOf(' ');
            if (separator > 0 && separator < normalized.Length - 1 && maximum >= 5)
            {
                var first = normalized.Substring(0, separator);
                var second = normalized.Substring(separator + 1).TrimStart();
                var secondSeparator = second.IndexOf(' ');
                if (secondSeparator > 0) second = second.Substring(0, secondSeparator);
                var firstLength = Math.Min(first.Length, Math.Max(1, maximum - 4));
                var secondLength = Math.Min(second.Length, 2);
                return first.Substring(0, firstLength) + " " +
                       second.Substring(0, secondLength) + "…";
            }
            return normalized.Substring(0, Math.Max(1, maximum - 1)).TrimEnd() + "…";
        }

        private void RefreshUnionChips(IReadOnlyList<M2BattleUnionView> unions)
        {
            var supported = SupportedUnionCount078(unions);
            ClampUnionPage078(supported);
            var visible = Math.Min(
                UnionSelectorPageSize078,
                Math.Max(0, supported - _unionPageStart078));
            for (var index = 0; index < _unionChips.Count; index++)
            {
                var chip = _unionChips[index];
                var unionIndex = _unionPageStart078 + index;
                var active = index < visible && unionIndex < supported && unions[unionIndex] != null;
                chip.Button.gameObject.SetActive(active);
                chip.UnionId = active ? unions[unionIndex].UnionId ?? string.Empty : string.Empty;
                if (!active) continue;

                var union = unions[unionIndex];
                var focused = StringComparer.Ordinal.Equals(union.UnionId, FocusedUnionId);
                var ordered = union.IsSelected || !string.IsNullOrWhiteSpace(union.SelectedForecastId);
                chip.Label.text = BuildUnionChipLabel(union, unionIndex);
                StyleUnionSelection(chip.Button, focused, ordered);
                chip.Button.interactable = _interactable && !IsBattleLocked() && union.CanAct;
            }
            var paging = supported > UnionSelectorPageSize078;
            LayoutUnionNavigator(_unionChips, visible, paging);
            RefreshUnionPageButtons078(supported, paging);
        }

        private void RefreshUnionPageButtons078(int supported, bool paging)
        {
            if (_previousUnionPage078 == null || _nextUnionPage078 == null) return;
            _previousUnionPage078.gameObject.SetActive(paging);
            _nextUnionPage078.gameObject.SetActive(paging);
            if (!paging) return;

            var unlocked = _interactable && !IsBattleLocked();
            _previousUnionPage078.interactable = unlocked && _unionPageStart078 > 0;
            _nextUnionPage078.interactable = unlocked &&
                                                _unionPageStart078 + UnionSelectorPageSize078 < supported;
            if (_previousUnionPageLabel078 != null)
                _previousUnionPageLabel078.text = "<\n" +
                                                  Math.Max(
                                                          1,
                                                          _unionPageStart078 - UnionSelectorPageSize078 + 1)
                                                      .ToString("00", CultureInfo.InvariantCulture);
            if (_nextUnionPageLabel078 != null)
                _nextUnionPageLabel078.text = Math.Min(
                        supported,
                        _unionPageStart078 + UnionSelectorPageSize078 + 1)
                    .ToString("00", CultureInfo.InvariantCulture) + "\n>";
        }

        private void ChangeUnionPage078(int direction)
        {
            if (!_interactable || _battle?.PlayerUnions == null || IsBattleLocked() || direction == 0) return;
            var supported = SupportedUnionCount078(_battle.PlayerUnions);
            var requested = _unionPageStart078 + direction * UnionSelectorPageSize078;
            var maximumStart = supported <= 0
                ? 0
                : (supported - 1) / UnionSelectorPageSize078 * UnionSelectorPageSize078;
            var next = Math.Max(0, Math.Min(maximumStart, requested));
            if (next == _unionPageStart078) return;
            _unionPageStart078 = next;
            var destination = FirstUnionOnCurrentPage078(_battle.PlayerUnions, supported);
            if (destination != null && !string.IsNullOrWhiteSpace(destination.UnionId))
            {
                Refresh(_battle, destination.UnionId);
                _focusUnion?.Invoke(destination.UnionId);
            }
            else
            {
                RefreshUnionChips(_battle.PlayerUnions);
            }
            FocusFirstVisibleUnionChip078();
        }

        private M2BattleUnionView FirstUnionOnCurrentPage078(
            IReadOnlyList<M2BattleUnionView> unions,
            int supported)
        {
            var end = Math.Min(supported, _unionPageStart078 + UnionSelectorPageSize078);
            for (var index = _unionPageStart078; index < end; index++)
                if (unions[index] != null && unions[index].CanAct &&
                    !string.IsNullOrWhiteSpace(unions[index].UnionId))
                    return unions[index];
            for (var index = _unionPageStart078; index < end; index++)
                if (unions[index] != null && !string.IsNullOrWhiteSpace(unions[index].UnionId))
                    return unions[index];
            return null;
        }

        private void FocusFirstVisibleUnionChip078()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null) return;
            for (var index = 0; index < _unionChips.Count; index++)
            {
                var button = _unionChips[index].Button;
                if (button == null || !button.gameObject.activeInHierarchy || !button.interactable) continue;
                eventSystem.SetSelectedGameObject(button.gameObject);
                return;
            }
            var fallback = _previousUnionPage078 != null && _previousUnionPage078.interactable
                ? _previousUnionPage078
                : _nextUnionPage078;
            if (fallback != null && fallback.gameObject.activeInHierarchy && fallback.interactable)
                eventSystem.SetSelectedGameObject(fallback.gameObject);
        }

        private void ShowFocusedUnionPage078(
            IReadOnlyList<M2BattleUnionView> unions,
            string unionId)
        {
            var supported = SupportedUnionCount078(unions);
            for (var index = 0; index < supported; index++)
            {
                if (unions[index] == null ||
                    !StringComparer.Ordinal.Equals(unions[index].UnionId, unionId)) continue;
                _unionPageStart078 = index / UnionSelectorPageSize078 * UnionSelectorPageSize078;
                return;
            }
            ClampUnionPage078(supported);
        }

        private void ClampUnionPage078(int supported)
        {
            var maximumStart = supported <= 0
                ? 0
                : (supported - 1) / UnionSelectorPageSize078 * UnionSelectorPageSize078;
            _unionPageStart078 = Math.Max(0, Math.Min(maximumStart, _unionPageStart078));
        }

        private static int SupportedUnionCount078(IReadOnlyList<M2BattleUnionView> unions) =>
            Math.Min(unions?.Count ?? 0, MaximumSupportedPlayerUnions078);

        private void RefreshForecastSlots(IReadOnlyList<M2ForecastView> forecasts, M2BattleUnionView focusedUnion)
        {
            var visible = Math.Min(forecasts.Count, ForecastVisibleLimit164);
            var selectedId = ResolveSelectedForecastId(focusedUnion, forecasts);
            for (var index = 0; index < _forecastSlots.Count; index++)
            {
                var slot = _forecastSlots[index];
                var active = index < visible && forecasts[index] != null;
                slot.Button.gameObject.SetActive(active);
                slot.UnionId = active ? forecasts[index].UnionId ?? string.Empty : string.Empty;
                slot.ForecastId = active ? forecasts[index].ForecastId ?? string.Empty : string.Empty;
                if (!active) continue;

                var forecast = forecasts[index];
                var selected = StringComparer.Ordinal.Equals(forecast.ForecastId, selectedId) || forecast.IsSelected;
                var previewed = StringComparer.Ordinal.Equals(forecast.UnionId, _previewUnionId) &&
                                StringComparer.Ordinal.Equals(forecast.ForecastId, _previewForecastId);
                slot.Label.text = BuildForecastButtonLabel(forecast, index, selected);
                StyleForecastSelection(slot.Button, selected, previewed, forecast.CommandId);
                slot.Button.interactable = _interactable && !IsBattleLocked() && focusedUnion != null && focusedUnion.CanAct;
            }
            LayoutActiveButtons(_forecastSlots, visible);
            RefreshForecastPages161(visible);
        }

        private void ChangeForecastPage161(int direction)
        {
            if (!_interactable || IsBattleLocked() || _forecastTotal161 <= ForecastVisibleLimit164) return;
            var maximum = (_forecastTotal161 - 1) / ForecastPageSize164 * ForecastPageSize164;
            var next = Math.Max(0, Math.Min(maximum, _forecastPageStart161 + direction * ForecastPageSize164));
            if (next == _forecastPageStart161) return;
            _forecastPageStart161 = next;
            _previewUnionId = string.Empty;
            _previewForecastId = string.Empty;
            Refresh(_battle, FocusedUnionId);
        }

        private void RefreshForecastPages161(int visible)
        {
            if (_previousForecastPage161 == null || _nextForecastPage161 == null) return;
            var paging = _forecastTotal161 > ForecastVisibleLimit164;
            _previousForecastPage161.gameObject.SetActive(paging);
            _nextForecastPage161.gameObject.SetActive(paging);
            if (!paging) return;
            var enabled = _interactable && !IsBattleLocked();
            _previousForecastPage161.interactable = enabled && _forecastPageStart161 > 0;
            _nextForecastPage161.interactable = enabled && _forecastPageStart161 + ForecastPageSize164 < _forecastTotal161;
            var page = (_forecastPageStart161 / ForecastPageSize164 + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                       ((_forecastTotal161 + ForecastPageSize164 - 1) / ForecastPageSize164).ToString(CultureInfo.InvariantCulture);
            _previousForecastPage161.GetComponentInChildren<Text>().text = "‹\n" + page;
            _nextForecastPage161.GetComponentInChildren<Text>().text = "›\n" + page;
            const float gap = 0.006f;
            const float pageWidth = 0.100f;
            var width = (1f - 2f * pageWidth - gap * (ForecastPageSize164 + 3)) / ForecastPageSize164;
            Anchor(_previousForecastPage161.GetComponent<RectTransform>(), new Vector2(gap, 0.03f), new Vector2(gap + pageWidth, 0.97f));
            for (var index = 0; index < visible; index++)
            {
                var left = pageWidth + 2f * gap + index * (width + gap);
                Anchor(_forecastSlots[index].Button.GetComponent<RectTransform>(), new Vector2(left, 0.03f), new Vector2(left + width, 0.97f));
            }
            var right = 1f - gap - pageWidth;
            Anchor(_nextForecastPage161.GetComponent<RectTransform>(), new Vector2(right, 0.03f), new Vector2(right + pageWidth, 0.97f));
        }

        private void RefreshPreview(IReadOnlyList<M2ForecastView> forecasts, M2BattleUnionView focusedUnion)
        {
            var forecast = FindPreviewForecast(forecasts, focusedUnion);
            if (forecast == null)
            {
                _previewHeading.text = "Choose an order";
                _previewSummary.text = focusedUnion == null || !focusedUnion.CanAct
                    ? "This Union cannot act this round."
                    : "Move across an order to inspect its target, cost, and forecast.";
                _previewActions.text = string.Empty;
                RefreshPredictedActionIcons076(null);
                return;
            }

            var selectedId = ResolveSelectedForecastId(focusedUnion, forecasts);
            var ready = forecast.IsSelected || StringComparer.Ordinal.Equals(forecast.ForecastId, selectedId);
            _previewHeading.text = (ready ? "✓ READY  •  " : "FORECAST  •  ") +
                                   ReadableOrderTitle074(Friendly(
                                       forecast.CommandName,
                                       Friendly(forecast.Phrase, "Union order"))).ToUpperInvariant();
            _previewSummary.text = BuildForecastSummary(forecast, focusedUnion);
            _previewActions.text = BuildPredictedActions(forecast);
            RefreshPredictedActionIcons076(forecast);
        }

        private void RefreshPredictedActionIcons076(M2ForecastView forecast)
        {
            for (var index = 0; index < _previewActionIcons.Count; index++)
            {
                var image = _previewActionIcons[index];
                var action = forecast?.MemberActions != null && index < forecast.MemberActions.Count
                    ? forecast.MemberActions[index]
                    : null;
                if (image == null || action == null ||
                    !BattleArtRuntimeRegistry011.TryResolveSemanticIcon076(
                        action.ArtId,
                        PredictedActionFamily076(action),
                        action.BreakthroughOpportunity,
                        out var assetId,
                        out var sprite) || sprite == null)
                {
                    if (image != null)
                    {
                        image.sprite = null;
                        image.gameObject.SetActive(false);
                    }
                    continue;
                }

                image.sprite = sprite;
                image.color = Color.white;
                image.gameObject.name = "Predicted Member Art Icon " +
                                        (index + 1).ToString(CultureInfo.InvariantCulture) +
                                        " 076 · " + assetId;
                image.gameObject.SetActive(true);
            }
        }

        private static BattleBeatFamily PredictedActionFamily076(M2PredictedActionView action)
        {
            var descriptor = ((action?.ActionKind ?? string.Empty) + " " +
                              (action?.Discipline ?? string.Empty)).ToUpperInvariant();
            if (descriptor.Contains("RESTOR") || descriptor.Contains("HEAL") || descriptor.Contains("MEDIC"))
                return BattleBeatFamily.Restoration;
            if (descriptor.Contains("WARD") || descriptor.Contains("GUARD") ||
                descriptor.Contains("PROTECT") || descriptor.Contains("FORMATION"))
                return BattleBeatFamily.Guard;
            if (descriptor.Contains("MYST") || descriptor.Contains("MAGIC") ||
                descriptor.Contains("INVOCATION"))
                return BattleBeatFamily.Mystic;
            if (descriptor.Contains("TACT") || descriptor.Contains("SUPPORT") || descriptor.Contains("ROLE"))
                return BattleBeatFamily.Tactical;
            return BattleBeatFamily.CombatArt;
        }

        private void RefreshConfirmButton(M2BattleView battle)
        {
            var ready = battle != null && !battle.IsResolved && battle.CanConfirmRound;
            var active = 0;
            var ordered = 0;
            if (battle?.PlayerUnions != null)
                for (var index = 0; index < battle.PlayerUnions.Count; index++)
                {
                    var union = battle.PlayerUnions[index];
                    if (union == null || !union.CanAct) continue;
                    active++;
                    if (union.IsSelected || !string.IsNullOrWhiteSpace(union.SelectedForecastId)) ordered++;
                }
            if (_confirmLabel != null)
            {
                _confirmLabel.text = ready
                    ? "EXECUTE ROUND\nALL ORDERS READY"
                    : "SET ORDERS\nLOCKED  " + ordered.ToString(CultureInfo.InvariantCulture) + "/" +
                      active.ToString(CultureInfo.InvariantCulture);
                _confirmLabel.color = ready
                    ? new Color(0.045f, 0.055f, 0.065f, 1f)
                    : new Color(0.58f, 0.64f, 0.70f, 1f);
            }
            if (_confirmButton != null)
            {
                var colors = _confirmButton.colors;
                colors.normalColor = ready ? new Color(0.32f, 0.92f, 0.70f, 1f) : RuntimeUi.ButtonNormal;
                colors.selectedColor = ready ? new Color(0.48f, 1f, 0.78f, 1f) : RuntimeUi.ButtonHighlighted;
                colors.highlightedColor = ready ? new Color(0.48f, 1f, 0.78f, 1f) : RuntimeUi.ButtonHighlighted;
                colors.disabledColor = new Color(0.035f, 0.045f, 0.060f, 1f);
                _confirmButton.colors = colors;
                _confirmButton.interactable = _interactable && ready;
            }
        }

        private void RefreshButtonInteractivity()
        {
            if (_battle == null)
            {
                for (var index = 0; index < _unionChips.Count; index++)
                    _unionChips[index].Button.interactable = false;
                for (var index = 0; index < _forecastSlots.Count; index++)
                    _forecastSlots[index].Button.interactable = false;
                if (_previousUnionPage078 != null) _previousUnionPage078.interactable = false;
                if (_nextUnionPage078 != null) _nextUnionPage078.interactable = false;
                if (_previousForecastPage161 != null) _previousForecastPage161.interactable = false;
                if (_nextForecastPage161 != null) _nextForecastPage161.interactable = false;
                if (_confirmButton != null) _confirmButton.interactable = false;
                return;
            }

            Refresh(_battle, FocusedUnionId);
        }

        private void HandleUnionFocus(int chipIndex)
        {
            if (!_interactable || chipIndex < 0 || chipIndex >= _unionChips.Count) return;
            var unionId = _unionChips[chipIndex].UnionId;
            if (string.IsNullOrWhiteSpace(unionId)) return;

            FocusedUnionId = unionId;
            _previewUnionId = string.Empty;
            _previewForecastId = string.Empty;
            Refresh(_battle, unionId);
            _focusUnion?.Invoke(unionId);
        }

        private void HandleForecastFocus(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _forecastSlots.Count || _battle == null) return;
            var slot = _forecastSlots[slotIndex];
            if (string.IsNullOrWhiteSpace(slot.UnionId) || string.IsNullOrWhiteSpace(slot.ForecastId)) return;

            _previewUnionId = slot.UnionId;
            _previewForecastId = slot.ForecastId;
            var focusedUnion = FindUnion(_battle.PlayerUnions, FocusedUnionId);
            var forecasts = CollectForecasts(_battle, FocusedUnionId);
            RefreshPreview(forecasts, focusedUnion);
            RefreshFocusedMemberStates078(
                focusedUnion,
                FindPreviewForecast(forecasts, focusedUnion));
            RefreshForecastStyles(forecasts, focusedUnion);
            _previewForecast?.Invoke(slot.UnionId, slot.ForecastId);
        }

        private void HandleForecastSelection(int slotIndex)
        {
            if (!_interactable || slotIndex < 0 || slotIndex >= _forecastSlots.Count) return;
            var slot = _forecastSlots[slotIndex];
            if (string.IsNullOrWhiteSpace(slot.UnionId) || string.IsNullOrWhiteSpace(slot.ForecastId)) return;

            _previewUnionId = slot.UnionId;
            _previewForecastId = slot.ForecastId;
            _optimisticSelections[slot.UnionId] = slot.ForecastId;
            Refresh(_battle, FocusedUnionId);
            _selectForecast?.Invoke(slot.UnionId, slot.ForecastId);
        }

        private void HandleConfirm()
        {
            if (!_interactable || _battle == null || _battle.IsResolved || !_battle.CanConfirmRound) return;
            _confirm?.Invoke();
        }

        private bool IsBattleLocked()
        {
            return _battle == null || _battle.IsResolved;
        }

        private void EnsureControllerFocus()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || _root == null || !_root.gameObject.activeInHierarchy) return;
            var current = eventSystem.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(_root))
            {
                var selectable = current.GetComponent<Selectable>();
                if (selectable == null || selectable.IsInteractable()) return;
            }

            // Selecting an order briefly disables the HUD while the coordinator
            // commits it. Unity clears selection when that button is disabled.
            // Restore the displayed/committed order before the generic first
            // button: its Select event otherwise changes the preview to Basic.
            var focusedUnion = FindUnion(_battle?.PlayerUnions ??
                Array.Empty<M2BattleUnionView>(), FocusedUnionId);
            var forecasts = CollectForecasts(_battle, FocusedUnionId);
            var preferred = FindPreviewForecast(forecasts, focusedUnion);
            if (preferred != null)
                for (var index = 0; index < _forecastSlots.Count; index++)
                {
                    var slot = _forecastSlots[index];
                    if (!StringComparer.Ordinal.Equals(slot.UnionId, preferred.UnionId) ||
                        !StringComparer.Ordinal.Equals(slot.ForecastId, preferred.ForecastId) ||
                        slot.Button == null || !slot.Button.gameObject.activeInHierarchy ||
                        !slot.Button.interactable) continue;
                    eventSystem.SetSelectedGameObject(slot.Button.gameObject);
                    return;
                }
            for (var index = 0; index < _forecastSlots.Count; index++)
            {
                var button = _forecastSlots[index].Button;
                if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                {
                    eventSystem.SetSelectedGameObject(button.gameObject);
                    return;
                }
            }
            for (var index = 0; index < _unionChips.Count; index++)
            {
                var button = _unionChips[index].Button;
                if (button != null && button.gameObject.activeInHierarchy && button.interactable)
                {
                    eventSystem.SetSelectedGameObject(button.gameObject);
                    return;
                }
            }
            if (_previousUnionPage078 != null &&
                _previousUnionPage078.gameObject.activeInHierarchy &&
                _previousUnionPage078.interactable)
            {
                eventSystem.SetSelectedGameObject(_previousUnionPage078.gameObject);
                return;
            }
            if (_nextUnionPage078 != null &&
                _nextUnionPage078.gameObject.activeInHierarchy &&
                _nextUnionPage078.interactable)
            {
                eventSystem.SetSelectedGameObject(_nextUnionPage078.gameObject);
                return;
            }
            if (_confirmButton != null && _confirmButton.gameObject.activeInHierarchy && _confirmButton.interactable)
                eventSystem.SetSelectedGameObject(_confirmButton.gameObject);
        }

        private string ResolveFocusedUnionId(IReadOnlyList<M2BattleUnionView> unions, string requested)
        {
            var supported = SupportedUnionCount078(unions);
            if (FindUnionIndex078(unions, requested, supported) >= 0) return requested;
            if (FindUnionIndex078(unions, FocusedUnionId, supported) >= 0) return FocusedUnionId;

            for (var index = 0; index < supported; index++)
            {
                var union = unions[index];
                if (union != null && union.CanAct && !string.IsNullOrWhiteSpace(union.UnionId)) return union.UnionId;
            }
            for (var index = 0; index < supported; index++)
            {
                var union = unions[index];
                if (union != null && !string.IsNullOrWhiteSpace(union.UnionId)) return union.UnionId;
            }
            return string.Empty;
        }

        private static int FindUnionIndex078(
            IReadOnlyList<M2BattleUnionView> unions,
            string unionId,
            int supported)
        {
            if (unions == null || string.IsNullOrWhiteSpace(unionId)) return -1;
            for (var index = 0; index < supported; index++)
                if (unions[index] != null &&
                    StringComparer.Ordinal.Equals(unions[index].UnionId, unionId))
                    return index;
            return -1;
        }

        private string ResolveSelectedForecastId(M2BattleUnionView union, IReadOnlyList<M2ForecastView> forecasts)
        {
            if (union != null && !string.IsNullOrWhiteSpace(union.SelectedForecastId))
            {
                _optimisticSelections.Remove(union.UnionId ?? string.Empty);
                return union.SelectedForecastId;
            }
            if (union != null && _optimisticSelections.TryGetValue(union.UnionId ?? string.Empty, out var optimistic))
                return optimistic;
            for (var index = 0; index < forecasts.Count; index++)
                if (forecasts[index] != null && forecasts[index].IsSelected) return forecasts[index].ForecastId;
            return string.Empty;
        }

        private static M2BattleUnionView FindUnion(IReadOnlyList<M2BattleUnionView> unions, string unionId)
        {
            if (string.IsNullOrWhiteSpace(unionId)) return null;
            for (var index = 0; index < unions.Count; index++)
            {
                var union = unions[index];
                if (union != null && StringComparer.Ordinal.Equals(union.UnionId, unionId)) return union;
            }
            return null;
        }

        private List<M2ForecastView> CollectForecasts(M2BattleView battle, string unionId)
        {
            var values = new List<M2ForecastView>();
            if (battle == null || battle.Forecasts == null || string.IsNullOrWhiteSpace(unionId))
            { _forecastTotal161 = 0; return values; }
            var selectedIndex = -1;
            var selectedId = FindUnion(battle.PlayerUnions, unionId)?.SelectedForecastId ?? string.Empty;
            for (var index = 0; index < battle.Forecasts.Count; index++)
            {
                var forecast = battle.Forecasts[index];
                if (forecast == null || !StringComparer.Ordinal.Equals(forecast.UnionId, unionId)) continue;
                if (forecast.IsSelected || forecast.ForecastId == selectedId)
                { selectedIndex = values.Count; selectedId = forecast.ForecastId; }
                values.Add(forecast);
            }
            _forecastTotal161 = values.Count;
            var key = (battle.BattleId ?? string.Empty) + "|" + battle.Round.ToString(CultureInfo.InvariantCulture) + "|" + unionId;
            if (_forecastPageKey161 != key || selectedId != _lastForecastSelection161 && selectedIndex >= 0)
                _forecastPageStart161 = selectedIndex < 0 ? 0 : selectedIndex / ForecastPageSize164 * ForecastPageSize164;
            _forecastPageKey161 = key;
            _lastForecastSelection161 = selectedId;
            if (values.Count <= ForecastVisibleLimit164) { _forecastPageStart161 = 0; return values; }
            _forecastPageStart161 = Math.Max(0, Math.Min((values.Count - 1) / ForecastPageSize164 * ForecastPageSize164, _forecastPageStart161));
            return values.GetRange(_forecastPageStart161, Math.Min(ForecastPageSize164, values.Count - _forecastPageStart161));
        }

        private static M2ForecastView FindSelectedForecast(IReadOnlyList<M2ForecastView> forecasts, string selectedId)
        {
            for (var index = 0; index < forecasts.Count; index++)
            {
                var forecast = forecasts[index];
                if (forecast == null) continue;
                if (forecast.IsSelected || (!string.IsNullOrWhiteSpace(selectedId) &&
                                            StringComparer.Ordinal.Equals(forecast.ForecastId, selectedId)))
                    return forecast;
            }
            return null;
        }

        private M2ForecastView FindPreviewForecast(
            IReadOnlyList<M2ForecastView> forecasts,
            M2BattleUnionView focusedUnion)
        {
            if (StringComparer.Ordinal.Equals(_previewUnionId, FocusedUnionId) &&
                !string.IsNullOrWhiteSpace(_previewForecastId))
                for (var index = 0; index < forecasts.Count; index++)
                    if (forecasts[index] != null &&
                        StringComparer.Ordinal.Equals(forecasts[index].ForecastId, _previewForecastId))
                        return forecasts[index];

            var selected = FindSelectedForecast(
                forecasts,
                ResolveSelectedForecastId(focusedUnion, forecasts));
            return selected ?? (forecasts.Count > 0 ? forecasts[0] : null);
        }

        private void RefreshForecastStyles(
            IReadOnlyList<M2ForecastView> forecasts,
            M2BattleUnionView focusedUnion)
        {
            var selectedId = ResolveSelectedForecastId(focusedUnion, forecasts);
            for (var index = 0; index < _forecastSlots.Count && index < forecasts.Count; index++)
            {
                var forecast = forecasts[index];
                if (forecast == null) continue;
                var selected = forecast.IsSelected || StringComparer.Ordinal.Equals(forecast.ForecastId, selectedId);
                var previewed = StringComparer.Ordinal.Equals(forecast.UnionId, _previewUnionId) &&
                                StringComparer.Ordinal.Equals(forecast.ForecastId, _previewForecastId);
                StyleForecastSelection(
                    _forecastSlots[index].Button,
                    selected,
                    previewed,
                    forecast.CommandId);
            }
        }

        private static string BuildUnionChipLabel(M2BattleUnionView union, int index)
        {
            var name = Friendly(union.DisplayName, "Union " + (index + 1).ToString(CultureInfo.InvariantCulture));
            var ready = union.IsSelected || !string.IsNullOrWhiteSpace(union.SelectedForecastId);
            var state = !union.CanAct ? "UNION DOWN" : ready ? "ORDER READY" : "ORDER NEEDED";
            return (index + 1).ToString("00", CultureInfo.InvariantCulture) + "  " +
                   BuildUnionCallsign074(name, index) + "\n" + state;
        }

        private static string BuildUnionCallsign074(string displayName, int index)
        {
            var tokens = (displayName ?? string.Empty).Split(
                new[] { ' ', '_', '-', '—' },
                StringSplitOptions.RemoveEmptyEntries);
            var callsign = string.Empty;
            for (var tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
            {
                var token = tokens[tokenIndex].Trim();
                if (token.Length == 0 ||
                    StringComparer.OrdinalIgnoreCase.Equals(token, "UNION") ||
                    int.TryParse(token, out _))
                    continue;

                var candidate = callsign.Length == 0 ? token : callsign + " " + token;
                if (candidate.Length > 12) break;
                callsign = candidate;
            }

            if (string.IsNullOrWhiteSpace(callsign))
                callsign = "UNION " + (index + 1).ToString(CultureInfo.InvariantCulture);
            return callsign.ToUpperInvariant();
        }

        private static string BuildForecastButtonLabel(
            M2ForecastView forecast,
            int index,
            bool selected)
        {
            if (IsTitanGoldForecast162(forecast))
                return (selected ? "✓ GOLD" : "GOLD") + "\n" +
                       TitanGoldArtName162(forecast) + "\n" + TitanGoldCost162(forecast);
            var name = Friendly(forecast.CommandName,
                Friendly(forecast.Phrase, "Order " + (index + 1).ToString(CultureInfo.InvariantCulture)));
            var effect = OrderCardEffect076(forecast);
            var title = ReadableOrderTitle074(name).ToUpperInvariant();
            var role = OrderRoleLabel076(forecast.CommandId);
            var roleLine = StringComparer.OrdinalIgnoreCase.Equals(title, role)
                ? string.Empty : role + "\n";
            return title + "\n" + (selected ? "✓ " : string.Empty) +
                   roleLine + "AP " +
                   Math.Max(0, forecast.SharedApCost).ToString(CultureInfo.InvariantCulture) +
                   (forecast.CombinedMpCost > 0
                        ? "  •  MP " + forecast.CombinedMpCost.ToString(CultureInfo.InvariantCulture)
                        : string.Empty) + "\n" + effect;
        }

        private static bool IsTitanGoldForecast162(M2ForecastView forecast) =>
            forecast?.CommandId?.StartsWith("TITAN161_", StringComparison.Ordinal) == true;

        private static string TitanGoldArtName162(M2ForecastView forecast)
        {
            if (forecast.MemberActions != null)
                foreach (var action in forecast.MemberActions)
                    if (action != null && StringComparer.Ordinal.Equals(
                            "TITAN161_" + action.ArtId, forecast.CommandId))
                        return Friendly(action.ArtName, Friendly(forecast.Phrase, "Gold Art"));
            return Friendly(forecast.Phrase, "Gold Art");
        }

        private static string TitanGoldCost162(M2ForecastView forecast) =>
            "AP " + Math.Max(0, forecast.SharedApCost).ToString(CultureInfo.InvariantCulture) +
            "  MP " + Math.Max(0, forecast.CombinedMpCost).ToString(CultureInfo.InvariantCulture);

        private static string ReadableOrderTitle074(string value)
        {
            var normalized = Friendly(value, "Union order").Trim();
            if (normalized.IndexOf("ATTACK!", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Attack";
            if (normalized.IndexOf("ATTACK USING COMBAT ART", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Arts";
            if (normalized.IndexOf("USE MYSTIC ARTS", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Mystic";
            if (normalized.IndexOf("RESTORE FORMATION", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Restore";
            if (normalized.IndexOf("HOLD THE LINE", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Guard";
            if (normalized.IndexOf("HEAL THE WOUNDED", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Heal";
            if (normalized.IndexOf("RECOVER AP", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Recover";
            if (normalized.IndexOf("SIDE STRIKE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                normalized.IndexOf("BLIND SIDE", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Flank";
            if (normalized.IndexOf("GET OUT", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Retreat";
            return WholeWordLabel074(normalized, 13);
        }

        private static string OrderRoleLabel076(string commandId)
        {
            switch ((commandId ?? string.Empty).ToUpperInvariant())
            {
                case "CMD_BALANCED": return "BASIC";
                case "CMD_ALL_OUT": return "ARTS";
                case "CMD_MYSTIC": return "MYSTIC";
                case "CMD_GUARD": return "GUARD";
                case "CMD_HEAL": return "HEAL";
                case "CMD_AP_RECOVERY": return "RECOVER";
                case "CMD_SUPPORT": return "FORM UP";
                case "CMD_FLANK": return "FLANK";
                case "CMD_RETREAT": return "EXIT";
                default: return "TACTICAL";
            }
        }

        private static string OrderCardEffect076(M2ForecastView forecast)
        {
            switch ((forecast?.CommandId ?? string.Empty).ToUpperInvariant())
            {
                case "CMD_GUARD": return "GUARD READY";
                case "CMD_HEAL": return "HP RESTORE";
                case "CMD_AP_RECOVERY":
                    return "AP +" + Math.Max(0, forecast.ApRecovery).ToString(CultureInfo.InvariantCulture);
                case "CMD_SUPPORT":
                {
                    var percent = SignedPercent076(forecast.ExpectedEffect);
                    return string.IsNullOrWhiteSpace(percent) ? "FORM UP" : "FORM " + percent;
                }
                case "CMD_FLANK": return "REAR PRESSURE";
                case "CMD_RETREAT": return "WITHDRAW";
                default:
                    return ReadableForecastEffect074(FirstForecastClause(
                        Friendly(forecast?.ExpectedEffect, "Tactical response"), 24));
            }
        }

        private static string SignedPercent076(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var percent = value.IndexOf('%');
            if (percent <= 0) return string.Empty;
            var start = percent - 1;
            while (start >= 0 && char.IsDigit(value[start])) start--;
            if (start >= 0 && (value[start] == '+' || value[start] == '-')) start--;
            var token = value.Substring(start + 1, percent - start);
            return token.Length <= 6 ? token : string.Empty;
        }

        private static string ReadableTargetTitle074(string value)
        {
            var normalized = Friendly(value, "Selected target").Trim();
            if (normalized.IndexOf("HINGE-EATER", StringComparison.OrdinalIgnoreCase) >= 0 ||
                StringComparer.OrdinalIgnoreCase.Equals(normalized, "Gate-Eater") ||
                StringComparer.OrdinalIgnoreCase.Equals(normalized, "The Gate-Eater"))
                return M2BattleActorRig072.GateEaterDisplayName076;
            if (normalized.IndexOf("GATEHEART BASTION", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Gateheart Guard";
            if (normalized.IndexOf("GATEIRON ESCORT", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Gateiron";
            if (normalized.IndexOf("GATE GNAWER PACK", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Gnawer Pack";
            return WholeWordLabel074(normalized, 13);
        }

        private static string ReadableForecastEffect074(string value)
        {
            var readable = Friendly(value, "Tactical response")
                .Replace("about ", "~")
                .Replace("About ", "~")
                .Replace(" damage", string.Empty)
                .Replace(" Damage", string.Empty);
            return WholeWordLabel074(readable, 14);
        }

        private static string WholeWordLabel074(string value, int limit)
        {
            var normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
            if (normalized.Length <= limit) return normalized;
            var cut = normalized.LastIndexOf(' ', Math.Min(limit, normalized.Length - 1));
            // A long single token is kept intact and allowed to best-fit. Cutting it
            // would create the mid-word fragments seen on 1280-wide order cards.
            if (cut <= 0) return normalized;
            return normalized.Substring(0, cut).TrimEnd(' ', '.', ',', ':', ';', '-', '—');
        }

        private static string BuildForecastSummary(M2ForecastView forecast, M2BattleUnionView union)
        {
            var target = M2BattleActorRig072.NormalizeBossIdentity076(
                Friendly(forecast.TargetName, "Selected target"));
            var area = AreaTargetSummaryForVerification095(forecast);
            if (!string.IsNullOrEmpty(area)) target = area;
            // The personal Art name is on its card. Put the complete Union cost
            // first while preserving a target identifier in the compact preview.
            if (IsTitanGoldForecast162(forecast))
                return TitanGoldCost162(forecast) + "\n" + Compact(target, 22);
            var apNow = union == null ? 0 : Math.Max(0, union.CurrentAp);
            var apAfter = Math.Max(0, apNow - Math.Max(0, forecast.SharedApCost) + Math.Max(0, forecast.ApRecovery));
            var effect = ReadableForecastEffect074(FirstForecastClause(
                Friendly(forecast.ExpectedEffect, "Tactical response"), 24));
            return (string.IsNullOrEmpty(area) ? TargetAffordanceForVerification080(forecast, union) : "AREA ATTACK") + "  •  " +
                   target.ToUpperInvariant() + "\nAP " +
                   apNow.ToString(CultureInfo.InvariantCulture) + " → " +
                   apAfter.ToString(CultureInfo.InvariantCulture) + "  •  MP " +
                   Math.Max(0, forecast.CombinedMpCost).ToString(CultureInfo.InvariantCulture) +
                   "  •  " + effect.ToUpperInvariant();
        }

        public static string TargetAffordanceForVerification080(
            M2ForecastView forecast,
            M2BattleUnionView sourceUnion)
        {
            if (forecast == null) return "TARGET";
            if (sourceUnion != null &&
                StringComparer.Ordinal.Equals(forecast.TargetId, sourceUnion.UnionId))
                return "OWN UNION";
            if (StringComparer.Ordinal.Equals(forecast.CommandId, "CMD_HEAL") ||
                StringComparer.Ordinal.Equals(forecast.CommandId, "CMD_SUPPORT"))
                return "ALLY UNION";
            return "TARGET";
        }

        private static string BuildPredictedActions(M2ForecastView forecast)
        {
            if (forecast.MemberActions == null || forecast.MemberActions.Count == 0)
                return "UNION PLAN\nThe group will adapt together.";

            var lines = new List<string>();
            var additionalMembers = 0;
            for (var index = 0; index < forecast.MemberActions.Count; index++)
            {
                var action = forecast.MemberActions[index];
                if (action == null) continue;
                if (lines.Count >= MaximumVisibleMemberArts)
                {
                    additionalMembers++;
                    continue;
                }
                var artName = M2BattleReadableText021.ArtDisplayName(
                    action.ArtId, string.Empty, false, action.ArtName);
                if (string.IsNullOrWhiteSpace(artName)) artName = "Adaptive action";
                var actorName = FirstName076(Friendly(action.ActorName, "Member"));
                var level = action.ArtLevel > 0
                    ? "  ·  LV" + action.ArtLevel +
                      (string.IsNullOrWhiteSpace(action.ArtPowerCue)
                          ? string.Empty
                          : " " + action.ArtPowerCue)
                    : string.Empty;
                lines.Add((action.BreakthroughOpportunity ? "★ " : string.Empty) +
                          actorName.ToUpperInvariant() + "  •  " + artName.ToUpperInvariant() +
                          level + (action.AreaTargetCount095 > 0
                              ? "  ·  " + action.AreaTargetCount095 + " TARGET" +
                                (action.AreaTargetCount095 == 1 ? string.Empty : "S")
                              : string.Empty));
            }
            if (additionalMembers > 0)
                lines.Add("+" + additionalMembers.ToString(CultureInfo.InvariantCulture) +
                          " MEMBERS  •  SEE UNION");
            return lines.Count == 0
                ? "UNION PLAN\nThe group will adapt together."
                : string.Join("\n", lines);
        }

        public static string AreaTargetSummaryForVerification095(M2ForecastView forecast)
        {
            var unions = new HashSet<string>(StringComparer.Ordinal);
            var targets = 0;
            if (forecast?.MemberActions != null)
                foreach (var action in forecast.MemberActions)
                {
                    if (action == null || action.AreaTargetCount095 <= 0) continue;
                    targets = Math.Max(targets, action.AreaTargetCount095);
                    foreach (var id in action.AreaUnionIds095 ?? Array.Empty<string>())
                        if (!string.IsNullOrEmpty(id)) unions.Add(id);
                }
            if (targets == 0) return string.Empty;
            return unions.Count > 1 ? unions.Count + " ENEMY UNIONS" :
                targets + " TARGET" + (targets == 1 ? string.Empty : "S") + " IN ONE UNION";
        }

        private static string FirstName076(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Member";
            var split = value.Trim().IndexOf(' ');
            return split <= 0 ? value.Trim() : value.Substring(0, split).Trim();
        }

        private static string Friendly(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var trimmed = value.Trim();
            if (Guid.TryParse(trimmed, out _)) return fallback;
            if (!LooksLikeIdentifier(trimmed)) return trimmed;

            var prefixes = new[]
            {
                "FORECAST_", "COMMAND_", "CMD_", "UNION_", "ENEMY_", "BATTLE_", "MEMBER_",
                "RECRUIT_", "QUALITY_", "EQUIPMENT_", "EQ_", "ITEM_", "CLASS_", "RACE_", "WORLD_"
            };
            for (var index = 0; index < prefixes.Length; index++)
            {
                if (!trimmed.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) continue;
                trimmed = trimmed.Substring(prefixes[index].Length);
                break;
            }
            trimmed = trimmed.Replace('_', ' ').Replace('-', ' ').Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return fallback;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        private static bool LooksLikeIdentifier(string value)
        {
            if (value.IndexOf('_') >= 0) return true;
            if (value.IndexOf(' ') >= 0 || value.Length < 5) return false;
            var hasLetter = false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!char.IsLetter(character)) continue;
                hasLetter = true;
                if (char.IsLower(character)) return false;
            }
            return hasLetter;
        }

        private static string Compact(string value, int maximumCharacters)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var singleLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (singleLine.IndexOf("  ", StringComparison.Ordinal) >= 0)
                singleLine = singleLine.Replace("  ", " ");
            if (singleLine.Length <= maximumCharacters) return singleLine;
            return singleLine.Substring(0, Math.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private static string FirstForecastClause(string value, int maximumCharacters)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var end = value.IndexOf(',');
            if (end < 0) end = value.IndexOf(';');
            var clause = end > 0 ? value.Substring(0, end) : value;
            return Compact(clause, maximumCharacters);
        }

        private static void ConfigureButtonLabel(Text label, int maximumSize, int minimumSize)
        {
            if (label == null) return;
            label.fontSize = maximumSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = minimumSize;
            label.resizeTextMaxSize = maximumSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.lineSpacing = 0.88f;
            label.raycastTarget = false;
        }

        private static void ConfigureTextFit(Text text, int maximumSize, int minimumSize)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static void SetTextInsets078(Text text, float horizontal)
        {
            if (text == null) return;
            text.rectTransform.offsetMin = new Vector2(horizontal, 0f);
            text.rectTransform.offsetMax = new Vector2(-horizontal, 0f);
        }

        private static Text AddAnchoredText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var text = RuntimeUi.AddText(parent, name, value, fontSize, alignment, color, style);
            Anchor(text.rectTransform, anchorMin, anchorMax, new Vector2(5f, 3f), new Vector2(-5f, -3f));
            text.raycastTarget = false;
            return text;
        }

        private static void StyleUnionSelection(Button button, bool focused, bool ordered)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = focused
                ? RuntimeUi.Accent
                : ordered ? new Color(0.12f, 0.43f, 0.34f, 1f) : RuntimeUi.ButtonNormal;
            colors.selectedColor = focused ? RuntimeUi.Accent : RuntimeUi.ButtonHighlighted;
            colors.highlightedColor = focused ? RuntimeUi.Warning : RuntimeUi.ButtonHighlighted;
            colors.pressedColor = RuntimeUi.ButtonPressed;
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = focused
                ? new Color(0.055f, 0.065f, 0.075f, 1f)
                : ordered ? new Color(0.82f, 1f, 0.91f, 1f) : RuntimeUi.Text;
        }

        private static void StyleForecastSelection(
            Button button,
            bool selected,
            bool previewed,
            string commandId)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = selected
                ? RuntimeUi.Accent
                : previewed ? Brighten076(OrderColor076(commandId), 1.28f) : OrderColor076(commandId);
            colors.selectedColor = selected ? RuntimeUi.Accent : RuntimeUi.ButtonHighlighted;
            colors.highlightedColor = selected ? RuntimeUi.Warning : Brighten076(OrderColor076(commandId), 1.45f);
            colors.pressedColor = RuntimeUi.ButtonPressed;
            colors.disabledColor = new Color(0.07f, 0.08f, 0.10f, 1f);
            button.colors = colors;
            var label = button.GetComponentInChildren<Text>();
            if (label != null) label.color = selected
                ? new Color(0.055f, 0.065f, 0.075f, 1f)
                 : RuntimeUi.Text;
        }

        private static Color OrderColor076(string commandId)
        {
            switch ((commandId ?? string.Empty).ToUpperInvariant())
            {
                case "CMD_ALL_OUT": return new Color(0.34f, 0.095f, 0.085f, 1f);
                case "CMD_MYSTIC": return new Color(0.22f, 0.11f, 0.38f, 1f);
                case "CMD_GUARD": return new Color(0.055f, 0.25f, 0.34f, 1f);
                case "CMD_HEAL": return new Color(0.06f, 0.29f, 0.20f, 1f);
                case "CMD_AP_RECOVERY": return new Color(0.31f, 0.22f, 0.07f, 1f);
                case "CMD_SUPPORT": return new Color(0.07f, 0.30f, 0.28f, 1f);
                case "CMD_FLANK": return new Color(0.38f, 0.18f, 0.06f, 1f);
                case "CMD_RETREAT": return new Color(0.24f, 0.10f, 0.12f, 1f);
                default: return new Color(0.075f, 0.19f, 0.31f, 1f);
            }
        }

        private static Color Brighten076(Color color, float multiplier) =>
            new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a);

        private static void LayoutUnionNavigator(
            IReadOnlyList<UnionChip> values,
            int visible,
            bool paging)
        {
            if (visible <= 0) return;
            var columns = visible;
            const float gapX = 0.008f;
            const float gapY = 0.055f;
            var minimumX = paging ? 0.052f : 0f;
            var maximumX = paging ? 0.948f : 1f;
            var available = maximumX - minimumX;
            var width = (available - gapX * (columns + 1)) / columns;
            var height = 1f - gapY * 2f;
            for (var index = 0; index < values.Count && index < visible; index++)
            {
                var column = index;
                var left = minimumX + gapX + column * (width + gapX);
                Anchor(values[index].Button.GetComponent<RectTransform>(),
                    new Vector2(left, gapY), new Vector2(left + width, gapY + height));
            }
        }

        private static void LayoutActiveButtons<T>(IReadOnlyList<T> values, int visible) where T : ButtonSlot
        {
            if (visible <= 0) return;
            var gap = 0.006f;
            var width = (1f - gap * (visible + 1)) / visible;
            for (var index = 0; index < values.Count; index++)
            {
                if (index >= visible) continue;
                var left = gap + index * (width + gap);
                Anchor(values[index].Button.GetComponent<RectTransform>(), new Vector2(left, 0.03f),
                    new Vector2(left + width, 0.97f), Vector2.zero, Vector2.zero);
            }
        }

        private static void Anchor(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            Anchor(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        }

        private static void Anchor(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void DisposeVisualRoot()
        {
            if (_root == null) return;
            _root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
        }

        private abstract class ButtonSlot
        {
            protected ButtonSlot(Button button, Text label)
            {
                Button = button;
                Label = label;
            }

            public Button Button { get; }
            public Text Label { get; }
        }

        private sealed class UnionChip : ButtonSlot
        {
            public UnionChip(Button button, Text label) : base(button, label) { }
            public string UnionId { get; set; } = string.Empty;
        }

        private sealed class ForecastSlot : ButtonSlot
        {
            public ForecastSlot(Button button, Text label) : base(button, label) { }
            public string UnionId { get; set; } = string.Empty;
            public string ForecastId { get; set; } = string.Empty;
        }
    }
}
