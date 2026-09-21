using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Persistent player-facing owner for one M2 battle. Forecast selection refreshes
    /// the existing view hierarchy; it never rebuilds the screen, camera, or actors.
    /// The deterministic coordinator remains the only authority for every command.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class M2BattleExperienceController072 : MonoBehaviour
    {
        public const float FirstBattleCoachMinY076 = 0.540f;
        public const float FirstBattleCoachMaxY076 = 0.653f;

        public const int MinimumStoryRibbonFontSize074 = 20;
        public const float MaximumPlaybackSpeed108 = 16f;
        public const float TowerAutoBaseDelaySeconds108 = 0.8f;
        public const int MaximumAutoDecisionHistory108 = 64;
        private const string SelectCue =
            "SecondDimension/Audio/Battle011/UI/SFX_UI_SELECT";
        private const string ConfirmCue =
            "SecondDimension/Audio/Battle011/UI/SFX_COMMAND_CONFIRM";

        private IM2PresentationCoordinator _coordinator;
        private bool _coordinatorSubscribed098;
        private Canvas _ownedCanvas;
        private RectTransform _root;
        private RectTransform _dioramaLayer;
        private RectTransform _hudLayer;
        private RectTransform _resultLayer;
        private Image _storyRibbon;
        private Text _encounterTitleText076;
        private Text _phaseText;
        private Text _objectiveText;
        private Text _statusText;
        private RectTransform _firstBattleCoach076;
        private Button _firstBattleCoachAction076;
        private M2BattleDioramaView072 _diorama;
        private M2BattleCommandHud072 _hud;
        private M2BattleResultsView072 _results;
        private M2BattleSequenceDirector072 _sequence;
        private M2BattleAudioDirector _audio;
        private Coroutine _roundRoutine;
        private M2BattleView _pendingView;
        private string _focusedUnionId = string.Empty;
        private bool _resolving;
        private bool _claiming;
        private bool _skipRequested;
        private bool _firstBattleCoachDismissed076;
        private bool _autoOrders091;
        private bool _autoSelecting091;
        private float _nextAutoOrderAt091;
        private string _autoBattleId091 = string.Empty;
        private Text _autoOrdersLabel091;
        private Button _autoOrdersButton091;
        private string _towerAutoFailure107 = string.Empty;
        private Image _autoDecisionPanel108;
        private Text _autoDecisionText108;
        private readonly Queue<string> _autoDecisionHistory108 = new Queue<string>();
        private IReadOnlyDictionary<string, string> _pendingAutoDecisions108 =
            new Dictionary<string, string>();
        private string _towerRewardNotice108 = string.Empty;
        private float _towerFloorStartedAt108;
        private float _lastAutoRoundCompletedAt108;

        public int LastCompletedTowerFloor108 { get; private set; }
        public float LastTowerFloorSeconds108 { get; private set; }
        public float LastTowerTransitionSeconds108 { get; private set; }
        public float LastAutoActionGapSeconds108 { get; private set; }

        // The FlowPresenter supplies this only for an active Tower floor. It uses
        // the ordinary saved claim/advance/begin commands and never resolves combat.
        public Func<M1CommandResult> ContinueTowerAuto107 { get; set; }

        public event Action BattleCompleted;

        public bool IsActive => _root != null && _root.gameObject.activeSelf;
        public bool OwnsCoordinatorNotifications098(IM1PresentationCoordinator coordinator) =>
            _coordinatorSubscribed098 && ReferenceEquals(_coordinator, coordinator) &&
            _coordinator != null && isActiveAndEnabled && IsActive && _root.gameObject.activeInHierarchy;
        public bool IsResolving => StoryBattleIntroHeld132 || _resolving || _claiming || _towerWait116 != null ||
            (_coordinator as Campaign022.ITowerAutoAsyncTransition116)?.PendingTowerAutoTransition116 != null ||
            (_coordinator as Campaign022.ITowerManualAsyncTransition117)?.PendingTowerManualTransition117 != null || _towerClaimWait120 != null ||
            (_coordinator as Campaign022.ITowerClaimAsyncTransition120)?.PendingTowerClaim120 != null;
        public string FocusedUnionId => _focusedUnionId;
        public M2BattleDioramaView072 OwnedDiorama078 => _diorama;
        public M2BattleCommandHud072 OwnedCommandHud078 => _hud;
        public M2BattleResultsView072 OwnedResultsView078 => _results;
        public M2BattleSequenceDirector072 OwnedSequenceDirector078 => _sequence;
        public bool ReducedMotion { get; set; }
        public bool AutoOrdersEnabled091 => _autoOrders091;

        public void ToggleAutoOrders091() => SetAutoOrders091(!_autoOrders091);

        public void SetAutoOrders091(bool enabled)
        {
            if (enabled && StoryBattleIntroHeld132) return;
            if (!enabled) (_coordinator as Campaign022.ITowerAutoAsyncTransition116)?.CancelTowerAutoBeforeSave116();
            // OFF is local presentation state. It must not project the complete
            // guild, inventory and battle merely to stop future Auto inputs.
            var battle = enabled ? M2BattleViewAccess098.Read(_coordinator) : null;
            _autoOrders091 = enabled && M2BattleAutoOrders091.HasLivingOpposition(battle);
            if (_autoOrders091) _towerAutoFailure107 = string.Empty;
            else _towerFloorStartedAt108 = 0f;
            _autoBattleId091 = _autoOrders091 ? battle.BattleId : string.Empty;
            ScheduleNextAutoOrder108();
            if (_autoOrdersLabel091 != null)
                _autoOrdersLabel091.text = _autoOrders091 ? "AUTO  ON" : "AUTO  OFF";
            if (_autoOrdersButton091 != null)
                _autoOrdersButton091.image.color = _autoOrders091 ? RuntimeUi.Accent : RuntimeUi.ButtonNormal;
            RefreshAutoDecisionFeed108();
            if (_autoOrders091)
            {
                if (_towerFloorStartedAt108 <= 0f) _towerFloorStartedAt108 = Time.realtimeSinceStartup;
                _firstBattleCoachDismissed076 = true;
                SetFirstBattleCoachVisible076(false);
                SetCaption("AUTO ORDERS ON", "Revive first, then rescue allies. Tap AUTO to take control.");
            }
        }

        private void Update()
        {
            if (!_autoOrders091) return;
            if (_coordinator == null || !IsActive || !_root.gameObject.activeInHierarchy)
            {
                SetAutoOrders091(false);
                return;
            }
            // No command can be submitted during these intervals. In particular,
            // playback must not rebuild every recruit's equipment choices and
            // both canonical hashes once per animation frame.
            if (StoryBattleIntroHeld132 || _resolving || _claiming || _autoSelecting091 || Time.unscaledTime < _nextAutoOrderAt091) return;
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (!M2BattleAutoOrders091.HasLivingOpposition(battle) || battle.BattleId != _autoBattleId091)
            {
                SetAutoOrders091(false);
                return;
            }
            _autoSelecting091 = true;
            if (_lastAutoRoundCompletedAt108 > 0f)
                LastAutoActionGapSeconds108 = Mathf.Max(
                    0f, Time.realtimeSinceStartup - _lastAutoRoundCompletedAt108);
            bool complete;
            string failure;
            M2BattleView committed;
            M2BattleView resolved;
            _resolving = true;
            _skipRequested = false;
            _hud?.SetInteractable(false);
            SetCaption("COMMANDS IN MOTION", "The committed Union orders now resolve.");
            _audio?.PlayResourceCue(ConfirmCue);
            try
            {
                complete = M2BattleAutoOrders091.SelectAndResolveCompletePlan108(
                    _coordinator, battle, out committed, out resolved, out failure);
            }
            catch
            {
                _resolving = false;
                _hud?.SetInteractable(true);
                throw;
            }
            finally
            {
                _autoSelecting091 = false;
            }
            if (!_autoOrders091) return;
            if (!complete)
            {
                _resolving = false;
                _hud?.SetInteractable(true);
                SetAutoOrders091(false);
                SetCaption("AUTO PAUSED", string.IsNullOrWhiteSpace(failure)
                    ? "Choose the next Union orders to continue." : HumanFailure(failure));
                return;
            }
            _pendingAutoDecisions108 = CaptureCommittedAutoDecisions110(committed, resolved);
            _pendingView = resolved;
            _roundRoutine = StartCoroutine(PlayResolvedRound(committed, resolved));
            ScheduleNextAutoOrder108();
        }
        private float _animationSpeed091 = 1f;
        private Text _playbackSpeedLabel091;
        public Action<float> PlaybackSpeedChanged091 { get; set; }
        public float AnimationSpeed
        {
            get => _animationSpeed091;
            set
            {
                _animationSpeed091 = float.IsNaN(value) || float.IsInfinity(value)
                    ? 1f : Mathf.Clamp(value, 0.25f, MaximumPlaybackSpeed108);
                if (_playbackSpeedLabel091 != null)
                    _playbackSpeedLabel091.text = "SPEED " +
                        _animationSpeed091.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "×";
                RefreshAutoDecisionFeed108();
            }
        }

        public void CyclePlaybackSpeed091()
        {
            AnimationSpeed = AnimationSpeed < 1.5f ? 2f :
                AnimationSpeed < 3f ? 4f : AnimationSpeed < 10f ? 16f : 1f;
            PlaybackSpeedChanged091?.Invoke(AnimationSpeed);
        }

        public static float AutoSchedulerDelay108(float speed)
        {
            var safe = float.IsNaN(speed) || float.IsInfinity(speed)
                ? 1f : Mathf.Clamp(speed, 0.25f, MaximumPlaybackSpeed108);
            return Mathf.Clamp(TowerAutoBaseDelaySeconds108 / safe, 0.05f,
                TowerAutoBaseDelaySeconds108);
        }

        private void ScheduleNextAutoOrder108() =>
            _nextAutoOrderAt091 = Time.unscaledTime + AutoSchedulerDelay108(AnimationSpeed);
        public M2BattleLiveArtRecipeDiagnostics076 LiveArtRecipeDiagnostics076 =>
            _diorama?.LiveArtRecipeDiagnostics076;
        public int CompletedExactBeatCount076 =>
            LiveArtRecipeDiagnostics076?.CompletedExactBeatCount ?? 0;
        public string LastCompletedExactArtId076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedArtId ?? string.Empty;
        public string LastCompletedExactRecipeId076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedRecipeId ?? string.Empty;
        public string LastCompletedExactMotionSignature076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedMotionSignature ?? string.Empty;
        public string LastCompletedExactVfxSignature076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedVfxSignature ?? string.Empty;
        public string LastCompletedExactSfxSignature076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedSfxSignature ?? string.Empty;
        public string LastCompletedExactMotionRecipe076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedMotionRecipe ?? string.Empty;
        public string LastCompletedExactVfxRecipe076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedVfxRecipe ?? string.Empty;
        public string LastCompletedExactCameraRecipe076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedCameraRecipe ?? string.Empty;
        public string LastCompletedExactTraceRecipe076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedTraceRecipe ?? string.Empty;
        public int LastCompletedExactDurationMilliseconds076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedDurationMilliseconds ?? 0;
        public int LastCompletedExactImpactMilliseconds076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedImpactMilliseconds ?? 0;
        public int LastCompletedExactObservedImpactMilliseconds076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedObservedImpactMilliseconds ?? 0;
        public int LastCompletedExactObservedCompletionMilliseconds076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedObservedCompletionMilliseconds ?? 0;
        public int LastCompletedExactTraceGeometryCount076 =>
            LiveArtRecipeDiagnostics076?.LastCompletedTraceGeometryCount ?? 0;
        public bool HasCompleteLiveArtRecipeConsumption076 =>
            LiveArtRecipeDiagnostics076?.HasCompleteLiveConsumption ?? false;
        public int ExactRecipeStartSfxCount076 => _sequence?.ExactRecipeStartSfxCount076 ?? 0;
        public int ExactRecipeImpactSfxCount076 => _sequence?.ExactRecipeImpactSfxCount076 ?? 0;
        public bool HasConsumedExactRecipeSfxSignature076(string signature076) =>
            _sequence?.HasConsumedExactRecipeSfxSignature076(signature076) ?? false;
        public bool HasConsumedLiveExactArtRecipe076
        {
            get
            {
                var diagnostics076 = LiveArtRecipeDiagnostics076;
                return diagnostics076 != null &&
                       diagnostics076.HasCompleteLiveConsumption &&
                       _sequence != null &&
                       _sequence.HasConsumedExactRecipeSfxSignature076(
                           diagnostics076.LastCompletedSfxSignature);
            }
        }

        public void Initialize(IM2PresentationCoordinator coordinator, RectTransform host = null)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            if (_coordinator != null && !ReferenceEquals(_coordinator, coordinator))
            {
                CancelStoryBattleIntro132();
                DetachTowerWait116();
            }
            if (_coordinator != null && !ReferenceEquals(_coordinator, coordinator))
                _coordinator.Changed -= HandleCoordinatorChanged;
            _coordinator = coordinator;
            _coordinator.Changed -= HandleCoordinatorChanged;
            _coordinator.Changed += HandleCoordinatorChanged;
            _coordinatorSubscribed098 = true;
            EnsureView(host);
        }

        public bool EnterCurrentBattle()
        {
            if (_coordinator == null) return false;
            EnsureView(null);
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null)
            {
                SetCaption("NO ACTIVE BATTLE", "The story has not started this encounter yet.");
                return false;
            }

            _root.gameObject.SetActive(true);
            Refresh(battle);
            return true;
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                CancelStoryBattleIntro132();
                DetachTowerWait116();
                SetAutoOrders091(false);
                CloseAutoHistory110();
            }
            if (_root != null) _root.gameObject.SetActive(visible);
        }

        // Read-only copy selection. The native Tower projection proves the active
        // operation/request identity; exact current BattleId equality binds it to
        // this displayed result. No battle-name inference or save changes.
        private int ReadTowerPayoffFloor156B(M2BattleView displayed)
        {
            if (displayed == null || !displayed.IsResolved ||
                !StringComparer.OrdinalIgnoreCase.Equals(displayed.Outcome, "Victory") ||
                string.IsNullOrWhiteSpace(displayed.BattleId) ||
                !(_coordinator is Campaign022.ICampaignProgressionPresentationCoordinator022 tower))
                return 0;
            var current = M2BattleViewAccess098.Read(_coordinator);
            if (current == null || !StringComparer.Ordinal.Equals(current.BattleId, displayed.BattleId))
                return 0;
            var authority = tower.CampaignProgression022;
            return authority != null && authority.IsAvailable &&
                   (authority.TowerBattleRewardAwaitingClaim || authority.TowerBattleWon)
                ? Math.Max(0, authority.TowerFloorNumber) : 0;
        }

        public void Refresh(M2BattleView battle)
        {
            if (battle == null || _root == null) return;
            _pendingView = battle;
            // Coordinator notifications are synchronous. During round resolution the
            // authoritative view may already contain terminal results, but showing that
            // result layer here would cover the choreography before it has played.
            if (_resolving || _claiming) return;
            if (ObservePendingTowerClaim120(battle)) return;
            if (ObservePendingTowerTransaction116(battle)) return;
            var nextTowerBattle107 = TryContinueTowerVictory107(battle);
            if (nextTowerBattle107 != null)
            {
                Refresh(nextTowerBattle107);
                return;
            }
            if (_claiming) return;
            // A failed continuation may already have safely claimed this reward.
            battle = _pendingView ?? battle;
            ResolveFocus(battle);
            var objective = string.IsNullOrWhiteSpace(battle.Objective)
                ? "Hold the line and defeat the enemy Union."
                : battle.Objective.Trim();
            if (_encounterTitleText076 != null)
                _encounterTitleText076.text = EncounterTitle076(battle);
            var floor = ResolveTowerFloorLabel(battle.BattleId);
            var objectiveText = "STAKES  •  " + EncounterStakes076(battle);
            if (!string.IsNullOrWhiteSpace(floor))
                objectiveText += "\n" + floor;
            objectiveText += "\nOBJECTIVE  •  " + objective;
            var titanNotice161 = (_coordinator as M1RuntimeCoordinator)?.TitanForecastNotice161;
            if (!string.IsNullOrWhiteSpace(titanNotice161)) objectiveText = titanNotice161;
            if (_objectiveText != null)
                _objectiveText.text = objectiveText;

            _diorama.Refresh(battle, _focusedUnionId);
            if (battle.IsResolved)
            {
                SetFirstBattleCoachVisible076(false);
                _hudLayer.gameObject.SetActive(false);
                _diorama.SetTacticalOverlayVisible(false);
                if (_storyRibbon != null) _storyRibbon.gameObject.SetActive(false);
                _resultLayer.gameObject.SetActive(true);
                _resultLayer.SetAsLastSibling();
                _results.Show(battle);
                if (!string.IsNullOrWhiteSpace(_towerAutoFailure107))
                    _results.ShowClaimFailure107(_towerAutoFailure107);
                var victory = string.Equals(
                    battle.Outcome,
                    "Victory",
                    StringComparison.OrdinalIgnoreCase);
                var resolvedHeading = HumanOutcomeHeading(battle);
                if (!string.IsNullOrWhiteSpace(floor))
                    resolvedHeading = floor + "  •  " + resolvedHeading;
                SetCaption(
                    resolvedHeading,
                    victory
                        ? "Review the rewards secured by victory, then continue."
                        : "Regroup and retry. The Hall Breach grants no rewards until victory.");
            }
            else
            {
                _resultLayer.gameObject.SetActive(false);
            ApplyPhoneBattleLayout164();
                _results.Hide();
                _diorama.SetTacticalOverlayVisible(true);
                if (_storyRibbon != null)
                {
                    _storyRibbon.gameObject.SetActive(true);
                    _storyRibbon.transform.SetAsLastSibling();
                }
                _hudLayer.gameObject.SetActive(true);
                _hud.Refresh(battle, _focusedUnionId);
                _hud.SetInteractable(!StoryBattleIntroHeld132 && !_resolving && !_claiming);
                var active = battle.PlayerUnions == null
                    ? Array.Empty<M2BattleUnionView>()
                    : battle.PlayerUnions.Where(value => value != null && value.CanAct).ToArray();
                var ordered = active.Count(value => value.IsSelected ||
                    !string.IsNullOrWhiteSpace(value.SelectedForecastId));
                var ordinal = Array.FindIndex(active, value =>
                    StringComparer.Ordinal.Equals(value.UnionId, _focusedUnionId));
                var firstBattle = IsFirstBattleTutorial076(battle);
                var floorPrefix = string.IsNullOrWhiteSpace(floor) ? string.Empty : floor + "  •  ";
                var guidance = firstBattle && _firstBattleCoachDismissed076
                    ? ordered <= 0
                        ? "STEP 1/3  •  CHOOSE ONE UNION ORDER"
                        : ordered < active.Length
                            ? "STEP 2/3  •  READY THE NEXT UNION"
                            : "STEP 3/3  •  EXECUTE THE COMPLETE PLAN"
                    : ordered + "/" + active.Length +
                      " ORDERS READY  •  DIRECT THE ACTIVE UNION";
                SetCaption(
                    "ROUND " + Math.Max(1, battle.Round) + "  •  " +
                    floorPrefix + "COMMAND UNION " +
                    Math.Max(1, ordinal + 1) + "/" + Math.Max(1, active.Length),
                    guidance);
                SetFirstBattleCoachVisible076(
                    firstBattle && !_firstBattleCoachDismissed076);
            }
            RefreshAutoDecisionFeed108();
            KeepAutoHistoryOnTop110();
            if (StoryBattleIntroHeld132) _storyBattleIntro132.transform.SetAsLastSibling();
            ApplyPhoneBattleLayout164();
        }

        public void RequestSkipCurrentRound()
        {
            if (_resolving) _skipRequested = true;
        }

        private void EnsureView(RectTransform requestedHost)
        {
            if (_root != null) return;
            _phoneHistory164=null;_phoneObjectiveScroll164=null;_phoneStatusSource164=null;_phoneBattleApplied164=false;_phoneButtonInsets164.Clear();

            var host = requestedHost;
            if (host == null)
            {
                RuntimeUi.EnsureEventSystem();
                _ownedCanvas = RuntimeUi.CreateCanvas("First Hour Battle Experience Canvas 072");
                host = RuntimeUi.AddSafeArea(_ownedCanvas.transform);
            }

            _root = RuntimeUi.AddStretchRect(host, "Persistent First Hour Battle Experience 072");
            _dioramaLayer = RuntimeUi.AddStretchRect(_root, "Authored Battle Diorama Layer 072");
            _hudLayer = RuntimeUi.AddStretchRect(_root, "Compact Union Command Layer 072");
            _resultLayer = RuntimeUi.AddStretchRect(_root, "Staged Battle Result Layer 072");

            _storyRibbon = RuntimeUi.AddPanel(
                _root,
                "Battle Story Ribbon 072",
                new Color(0.012f, 0.025f, 0.045f, 0.86f));
            SetAnchors(_storyRibbon.rectTransform, new Vector2(0.012f, 0.868f), new Vector2(0.988f, 0.995f));
            _storyRibbon.raycastTarget = false;

            _encounterTitleText076 = RuntimeUi.AddText(
                _storyRibbon.transform,
                "Battle Encounter Title 076",
                "HALL BREACH",
                54,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors(_encounterTitleText076.rectTransform,
                new Vector2(0.018f, 0.49f), new Vector2(0.44f, 0.96f));
            _encounterTitleText076.resizeTextForBestFit = true;
            _encounterTitleText076.resizeTextMinSize = 36;
            _encounterTitleText076.resizeTextMaxSize = 54;
            _encounterTitleText076.raycastTarget = false;

            _phaseText = RuntimeUi.AddText(
                _storyRibbon.transform,
                "Battle Phase 072",
                "UNION COMMAND",
                34,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            SetAnchors(_phaseText.rectTransform, new Vector2(0.018f, 0.04f), new Vector2(0.44f, 0.49f));
            _phaseText.resizeTextForBestFit = true;
            _phaseText.resizeTextMinSize = 24;
            _phaseText.resizeTextMaxSize = 34;
            _phaseText.raycastTarget = false;

            _objectiveText = RuntimeUi.AddText(
                _storyRibbon.transform,
                "Battle Objective 072",
                string.Empty,
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors(_objectiveText.rectTransform, new Vector2(0.47f, 0.45f), new Vector2(0.855f, 0.96f));
            _objectiveText.resizeTextForBestFit = true;
            _objectiveText.resizeTextMinSize = 21;
            _objectiveText.resizeTextMaxSize = 30;
            _objectiveText.raycastTarget = false;

            _statusText = RuntimeUi.AddText(
                _storyRibbon.transform,
                "Battle Presentation Status 072",
                string.Empty,
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.MutedText,
                FontStyle.Bold);
            SetAnchors(_statusText.rectTransform, new Vector2(0.47f, 0.04f), new Vector2(0.855f, 0.45f));
            _statusText.resizeTextForBestFit = true;
            _statusText.resizeTextMinSize = MinimumStoryRibbonFontSize074;
            _statusText.resizeTextMaxSize = 28;
            _statusText.raycastTarget = false;

            // Keep speed on the story ribbon, which stays visible during playback.
            // The command HUD is intentionally hidden while actions resolve.
            BuildPlaybackSpeedControl091(_storyRibbon.transform);
            BuildAutoOrdersControl091(_storyRibbon.transform);
            BuildAutoDecisionFeed108();

            BuildFirstBattleCoach076();

            // The flow replaces its screen host when moving between the Hall, field,
            // and results. These presenter components deliberately live on the
            // controller so their diagnostics survive that replacement; reuse them
            // and let Initialize rebuild only their visual roots under the new host.
            // Creating a new component stack here leaks stale renderers and makes an
            // arbitrary GetComponentInChildren lookup observe an earlier battle.
            if (_diorama == null)
                _diorama = CreateComponent<M2BattleDioramaView072>("Battle Diorama Presenter 072");
            _diorama.Initialize(_dioramaLayer);
            if (_hud == null)
                _hud = CreateComponent<M2BattleCommandHud072>("Battle Command HUD 072");
            _hud.Initialize(_hudLayer, FocusUnion, PreviewForecast, SelectForecast, ConfirmRound);
            if (_results == null)
                _results = CreateComponent<M2BattleResultsView072>("Battle Results Presenter 072");
            _results.Initialize(_resultLayer, ClaimAndComplete, ReadTowerPayoffFloor156B);
            if (_sequence == null)
                _sequence = CreateComponent<M2BattleSequenceDirector072>("Battle Sequence Director 072");
            _audio = gameObject.GetComponent<M2BattleAudioDirector>();
            if (_audio == null) _audio = gameObject.AddComponent<M2BattleAudioDirector>();
            _sequence.Initialize(_diorama, _audio, SetCaption);
            _resultLayer.gameObject.SetActive(false);
        }

        private Button BuildPlaybackSpeedControl091(Transform parent)
        {
            var button = RuntimeUi.AddButton(parent,
                "Battle Playback Speed 091", "SPEED 1×", CyclePlaybackSpeed091,
                90f, RuntimeUi.ButtonNormal);
            SetAnchors(button.GetComponent<RectTransform>(),
                new Vector2(0.875f, 0.51f), new Vector2(0.982f, 0.99f));
            _playbackSpeedLabel091 = button.GetComponentInChildren<Text>();
            _playbackSpeedLabel091.resizeTextForBestFit = true;
            _playbackSpeedLabel091.resizeTextMinSize = 22;
            _playbackSpeedLabel091.resizeTextMaxSize = 31;
            AnimationSpeed = _animationSpeed091;
            return button;
        }

        private Button BuildAutoOrdersControl091(Transform parent)
        {
            _autoOrdersButton091 = RuntimeUi.AddButton(parent,
                "Battle Auto Orders 091", "AUTO  OFF", ToggleAutoOrders091,
                54f, RuntimeUi.ButtonNormal);
            SetAnchors(_autoOrdersButton091.GetComponent<RectTransform>(),
                new Vector2(0.875f, 0.01f), new Vector2(0.982f, 0.49f));
            _autoOrdersLabel091 = _autoOrdersButton091.GetComponentInChildren<Text>();
            _autoOrdersLabel091.resizeTextForBestFit = true;
            _autoOrdersLabel091.resizeTextMinSize = 20;
            _autoOrdersLabel091.resizeTextMaxSize = 28;
            _autoOrdersLabel091.text = _autoOrders091 ? "AUTO  ON" : "AUTO  OFF";
            return _autoOrdersButton091;
        }

        private void BuildFirstBattleCoach076()
        {
            var panel = RuntimeUi.AddPanel(
                _root,
                "First Battle Union Coach 076",
                new Color(0.006f, 0.016f, 0.030f, 0.985f));
            _firstBattleCoach076 = panel.rectTransform;
            SetAnchors(
                _firstBattleCoach076,
                new Vector2(0.075f, FirstBattleCoachMinY076),
                new Vector2(0.925f, FirstBattleCoachMaxY076));
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.HighContrast);

            var eyebrow = RuntimeUi.AddText(
                panel.transform,
                "First Battle Coach Eyebrow 076",
                "FIRST BATTLE  •  COMMAND WHOLE UNIONS",
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            SetAnchors(
                eyebrow.rectTransform,
                new Vector2(0.025f, 0.55f),
                new Vector2(0.68f, 0.93f));
            ConfigureCoachText076(eyebrow, 22, 30);

            var lesson = RuntimeUi.AddText(
                panel.transform,
                "First Battle Coach Lesson 076",
                "1  SELECT UNION   •   2  CHOOSE ORDER + CHECK AP   •   3  READY ALL",
                25,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            SetAnchors(
                lesson.rectTransform,
                new Vector2(0.025f, 0.08f),
                new Vector2(0.70f, 0.56f));
            ConfigureCoachText076(lesson, 21, 25);

            var begin = RuntimeUi.AddButton(
                panel.transform,
                "Dismiss First Battle Union Coach 076",
                "TAKE COMMAND",
                DismissFirstBattleCoach076,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            _firstBattleCoachAction076 = begin;
            SetAnchors(
                begin.GetComponent<RectTransform>(),
                new Vector2(0.72f, 0.12f),
                new Vector2(0.975f, 0.88f));
            ConfigureCoachText076(begin.GetComponentInChildren<Text>(), 24, 34);
            _firstBattleCoach076.gameObject.SetActive(false);
        }

        private void DismissFirstBattleCoach076()
        {
            _firstBattleCoachDismissed076 = true;
            SetFirstBattleCoachVisible076(false);
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle != null) Refresh(battle);
        }

        private void SetFirstBattleCoachVisible076(bool visible)
        {
            if (_firstBattleCoach076 == null) return;
            _firstBattleCoach076.gameObject.SetActive(visible);
            if (visible)
            {
                _firstBattleCoach076.SetAsLastSibling();
                _firstBattleCoachAction076?.Select();
            }
        }

        public static bool IsFirstBattleTutorial076(M2BattleView battle) =>
            battle != null &&
            !string.IsNullOrWhiteSpace(battle.BattleId) &&
            battle.BattleId.IndexOf("HALL_BREACH", StringComparison.OrdinalIgnoreCase) >= 0;

        public static string EncounterTitle076(M2BattleView battle)
        {
            var id = battle?.BattleId ?? string.Empty;
            var objective = battle?.Objective ?? string.Empty;
            if (Contains076(id, "HALL_BREACH") || Contains076(objective, "Guild Hall"))
                return "HALL BREACH";
            if (Contains076(id, "LANTERN_ROAD_AMBUSH") || Contains076(objective, "Lantern Road ambush"))
                return "LANTERN ROAD AMBUSH";
            if (Contains076(id, "GATE_EATER") || Contains076(objective, "Gate-Eater"))
                return "THE GATE-EATER";
            if (Contains076(id, "SURVEYOR_RESCUE"))
                return "THE LAST FALSE LINE";
            if (Contains076(id, "FOG_STALKERS") || Contains076(objective, "missing survey crew"))
                return "THE FOG STALKERS";
            return "UNION BATTLE";
        }

        public static string EncounterStakes076(M2BattleView battle)
        {
            var id = battle?.BattleId ?? string.Empty;
            var objective = battle?.Objective ?? string.Empty;
            if (Contains076(id, "HALL_BREACH") || Contains076(objective, "Guild Hall"))
                return "Keep the founding Hall standing.";
            if (Contains076(id, "LANTERN_ROAD_AMBUSH") || Contains076(objective, "Lantern Road ambush"))
                return "Reopen the only road to Zorin's patrol.";
            if (Contains076(id, "GATE_EATER") || Contains076(objective, "Gate-Eater"))
                return "Save the patrol, the Wayglass, and Skyhome.";
            if (Contains076(id, "SURVEYOR_RESCUE"))
                return "Free Orra's survey crew and hold the unrecorded door.";
            if (Contains076(id, "FOG_STALKERS") || Contains076(objective, "missing survey crew"))
                return "Keep Sella alive and hold Orra's true Wayglass line.";
            return "Win the field without breaking your Unions.";
        }

        private static bool Contains076(string value, string fragment) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static void ConfigureCoachText076(
            Text text,
            int minimum,
            int maximum)
        {
            if (text == null) return;
            text.fontSize = maximum;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimum;
            text.resizeTextMaxSize = maximum;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private T CreateComponent<T>(string objectName) where T : Component
        {
            var item = new GameObject(objectName);
            item.transform.SetParent(transform, false);
            return item.AddComponent<T>();
        }

        private void HandleCoordinatorChanged()
        {
            // C# events can still call disabled/hidden MonoBehaviours. The
            // FlowPresenter retains its fallback when this owner is unavailable.
            if (!OwnsCoordinatorNotifications098(_coordinator)) return;
            // Auto commits a complete round plan through one authoritative save.
            // The controller already owns the before/after projections, so do not
            // rebuild this large battle while that synchronous commit is running.
            if (_autoSelecting091) return;
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null) return;
            _pendingView = battle;
            if (_resolving || _claiming) return;
            Refresh(battle);
        }

        private void FocusUnion(string unionId)
        {
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null || _resolving) return;
            var union = battle.PlayerUnions?.FirstOrDefault(value =>
                value != null && string.Equals(value.UnionId, unionId, StringComparison.Ordinal));
            if (union == null) return;
            // Focus is also an inspection action. Spent or down Unions are safe to
            // inspect because their complete Forecast controls remain disabled.
            _focusedUnionId = union.UnionId;
            _hud.Refresh(battle, _focusedUnionId);
            _diorama.Focus(_focusedUnionId, PreferredTargetId(battle, _focusedUnionId));
            _audio?.PlayResourceCue(SelectCue);
        }

        private void PreviewForecast(string unionId, string forecastId)
        {
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null || _resolving || string.IsNullOrWhiteSpace(forecastId)) return;
            var forecast = battle.Forecasts?.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.UnionId, unionId) &&
                StringComparer.Ordinal.Equals(value.ForecastId, forecastId));
            if (forecast == null) return;
            _diorama.Focus(unionId, forecast.TargetId);
        }

        private void SelectForecast(string unionId, string forecastId)
        {
            if (StoryBattleIntroHeld132 || _coordinator == null || _resolving || _claiming) return;
            _focusedUnionId = unionId ?? string.Empty;
            _hud.SetInteractable(false);
            var result = _coordinator.SelectForecast(unionId, forecastId);
            if (!result.Succeeded)
            {
                _hud.RejectPendingSelection(unionId);
                _hud.SetInteractable(true);
                SetCaption("ORDER NOT AVAILABLE", HumanFailure(result.Message));
                return;
            }
            _audio?.PlayResourceCue(SelectCue);
            var after = M2BattleViewAccess098.Read(_coordinator);
            var next = after?.PlayerUnions?.FirstOrDefault(value => value != null && value.CanAct &&
                !value.IsSelected && string.IsNullOrWhiteSpace(value.SelectedForecastId) &&
                !StringComparer.Ordinal.Equals(value.UnionId, unionId));
            _focusedUnionId = next?.UnionId ?? unionId ?? string.Empty;
            // Changed is synchronous for the runtime coordinator, but explicit refresh
            // keeps alternate/test coordinators responsive without recreating anything.
            Refresh(after);
        }

        private void ConfirmRound()
        {
            if (StoryBattleIntroHeld132 || _coordinator == null || _resolving || _claiming) return;
            var before = M2BattleViewAccess098.Read(_coordinator);
            if (before == null || !before.CanConfirmRound)
            {
                SetCaption("ORDERS INCOMPLETE", "Choose one complete order for every active Union.");
                return;
            }

            _resolving = true;
            _skipRequested = false;
            _hud.SetInteractable(false);
            SetCaption("COMMANDS IN MOTION", "The committed Union orders now resolve.");
            _audio?.PlayResourceCue(ConfirmCue);
            var result = _coordinator.ConfirmBattleRound();
            if (!result.Succeeded)
            {
                _resolving = false;
                _hud.SetInteractable(true);
                SetCaption("ROUND COULD NOT START", HumanFailure(result.Message));
                return;
            }

            var after = M2BattleViewAccess098.Read(_coordinator);
            if (after == null)
            {
                _resolving = false;
                SetCaption("BATTLE RECORD LOST", "The resolved battle view was unavailable.");
                return;
            }
            _pendingAutoDecisions108 = _autoOrders091
                ? CaptureCommittedAutoDecisions110(before, after)
                : new Dictionary<string, string>();
            _pendingView = after;
            _roundRoutine = StartCoroutine(PlayResolvedRound(before, after));
        }

        private IEnumerator PlayResolvedRound(M2BattleView before, M2BattleView after)
        {
            _hudLayer.gameObject.SetActive(false);
            var events = after.LastResolvedRoundEvents ?? Array.Empty<M2BattleEventView>();
            yield return _sequence.PlayRound(
                before,
                after,
                events,
                ReducedMotion,
                () => Mathf.Clamp(AnimationSpeed, 0.25f, MaximumPlaybackSpeed108),
                () => _skipRequested,
                ShowCommittedAutoDecision108);

            _roundRoutine = null;
            _resolving = false;
            _skipRequested = false;
            _lastAutoRoundCompletedAt108 = Time.realtimeSinceStartup;
            // Allow the next planning state to become visible before another order.
            ScheduleNextAutoOrder108();
            Refresh(_pendingView ?? after);
        }

        private void ClaimAndComplete()
        {
            if (!IsActive || _coordinator == null || _claiming || _resolving) return;
            var battle = M2BattleViewAccess098.Read(_coordinator);
            if (battle == null || !battle.IsResolved) return;
            // A Tower wipe keeps its original admission until the player chooses
            // restart or retreat; those authorities claim the defeat exactly once.
            // Other terminal outcomes must save their reward AND battle return
            // before leaving results, including an objective loss with survivors.
            if (_coordinator is Campaign022.ITowerRestartAsyncTransition130 restart130 &&
                restart130.CanRestartTowerAfterPartyDefeat130)
            {
                SetVisible(false);
                BattleCompleted?.Invoke();
                return;
            }
            if (TryBeginTowerClaim120()) return;
            _claiming = true;
            M1CommandResult result;
            try
            {
                result = _coordinator.ClaimBattleRewards();
            }
            catch (Exception exception)
            {
                result = M1CommandResult.Failure(exception.Message);
            }
            finally
            {
                _claiming = false;
            }
            if (result == null || !result.Succeeded)
            {
                _results.ShowClaimFailure107(result?.Message ?? "The reward claim returned no result. Try again.");
                return;
            }

            SetVisible(false);
            BattleCompleted?.Invoke();
        }

        private M2BattleView TryContinueTowerVictory107(M2BattleView battle)
        {
            if (!_autoOrders091 || ContinueTowerAuto107 == null || battle == null ||
                !battle.IsResolved || !StringComparer.Ordinal.Equals(battle.BattleId, _autoBattleId091))
                return null;
            if (!StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory"))
            {
                SetAutoOrders091(false);
                return null;
            }

            if (_coordinator is Campaign022.ITowerAutoAsyncTransition116 asyncTransition116)
            {
                BeginTowerTransaction116(asyncTransition116, battle);
                return null;
            }

            // Changed events fire synchronously during every saved transition.
            // Keep their terminal snapshots out of the view until the next battle
            // is committed, and keep the original result usable on any failure.
            _claiming = true;
            var transitionStarted108 = Time.realtimeSinceStartup;
            try
            {
                var savedRewardReader110 = _coordinator as Campaign022.ITowerSavedRewardReader110;
                var clearedProgression = savedRewardReader110 == null
                    ? (_coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022)
                        ?.CampaignProgression022
                    : null;
                var clearedFloor = clearedProgression?.TowerFloorNumber ??
                    ParseTowerFloorNumber108(battle.BattleId);
                var battleReward = battle.Reward;
                M1CommandResult advanced;
                if (_coordinator is Campaign022.ITowerAutoTransitionCoordinator108 automaticTransition108)
                {
                    advanced = automaticTransition108.AdvanceTowerAutoAfterVictory108();
                }
                else
                {
                    var claim = _coordinator.ClaimBattleRewards();
                    if (claim == null || !claim.Succeeded)
                        return StopTowerAuto107(claim?.Message ?? "The reward claim returned no result.");
                    if (!_autoOrders091) return null;
                    advanced = ContinueTowerAuto107();
                }
                if (advanced == null || !advanced.Succeeded)
                    return StopTowerAuto107(advanced?.Message ?? "The next Tower floor could not be prepared.");
                var next = M2BattleViewAccess098.Read(_coordinator);
                if (!M2BattleAutoOrders091.HasLivingOpposition(next) ||
                    StringComparer.Ordinal.Equals(next.BattleId, battle.BattleId))
                    return StopTowerAuto107("The next Tower battle is unavailable. Return to the Tower to continue.");
                _pendingView = next;
                if (_autoOrders091) _autoBattleId091 = next.BattleId;
                var savedReward110 = savedRewardReader110?.ReadTowerSavedReward110(battle.BattleId,next.BattleId);
                var savedProgression = savedRewardReader110 == null
                    ? (_coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022)
                        ?.CampaignProgression022
                    : null;
                if(savedReward110 != null)
                {
                    clearedFloor=savedReward110.Floor;
                    clearedProgression=new Campaign022.CampaignProgressionPresentationState022 {
                        TowerGuildXpReward=savedReward110.GuildXp,
                        TowerHallXpReward=savedReward110.HallXp,
                        TowerRewardMaterialIds108=savedReward110.MaterialIds };
                    savedProgression=new Campaign022.CampaignProgressionPresentationState022 {
                        TowerLastHeroRewardFloor094=savedReward110.Floor,
                        TowerLastHeroRewardSummary094=savedReward110.HeroSummary };
                }
                _towerRewardNotice108 = BuildTowerRewardNotice108(
                    clearedFloor, clearedProgression, battleReward, savedProgression);
                AppendAutoHistory110(_autoDecisionHistory108, _towerRewardNotice108);
                var completedAt108 = Time.realtimeSinceStartup;
                LastCompletedTowerFloor108 = clearedFloor;
                LastTowerTransitionSeconds108 = Mathf.Max(0f, completedAt108 - transitionStarted108);
                LastTowerFloorSeconds108 = _towerFloorStartedAt108 > 0f
                    ? Mathf.Max(0f, completedAt108 - _towerFloorStartedAt108)
                    : LastTowerTransitionSeconds108;
                _towerFloorStartedAt108 = completedAt108;
                RefreshAutoDecisionFeed108();
                ScheduleNextAutoOrder108();
                _towerAutoFailure107 = string.Empty;
                return next;
            }
            catch (Exception exception)
            {
                return StopTowerAuto107(exception.Message);
            }
            finally
            {
                _claiming = false;
            }
        }

        private M2BattleView StopTowerAuto107(string message)
        {
            SetAutoOrders091(false);
            _towerAutoFailure107 = "AUTO PAUSED: " + (string.IsNullOrWhiteSpace(message)
                ? "The Tower transition failed. Try again from the Tower." : message);
            return null;
        }

        public static string BuildTowerRewardNotice108(
            int floor,
            Campaign022.CampaignProgressionPresentationState022 cleared,
            M2BattleRewardView battleReward,
            Campaign022.CampaignProgressionPresentationState022 saved)
        {
            var rewards = new List<string>();
            if (battleReward != null)
            {
                var battleParts = new List<string>();
                if (battleReward.GuildTreasuryXpAward > 0)
                    battleParts.Add("+" + battleReward.GuildTreasuryXpAward.ToString(CultureInfo.InvariantCulture) +
                        " BATTLE GUILD XP");
                if (battleReward.HallEnhancementXpAward > 0)
                    battleParts.Add("+" + battleReward.HallEnhancementXpAward.ToString(CultureInfo.InvariantCulture) +
                        " BATTLE HALL XP");
                if (!string.IsNullOrWhiteSpace(battleReward.EquipmentRewardDisplayName))
                    battleParts.Add(battleReward.EquipmentRewardDisplayName.Trim());
                if (battleParts.Count > 0) rewards.Add(string.Join("  •  ", battleParts));
            }
            if (cleared != null)
            {
                var floorParts = new List<string>();
                if (cleared.TowerGuildXpReward > 0)
                    floorParts.Add("+" + cleared.TowerGuildXpReward.ToString(CultureInfo.InvariantCulture) +
                        " FLOOR GUILD XP");
                if (cleared.TowerHallXpReward > 0)
                    floorParts.Add("+" + cleared.TowerHallXpReward.ToString(CultureInfo.InvariantCulture) +
                        " FLOOR HALL XP");
                foreach (var materialId in cleared.TowerRewardMaterialIds108 ?? Array.Empty<string>())
                    if (!string.IsNullOrWhiteSpace(materialId))
                        floorParts.Add("+4 " + materialId.Trim());
                if (floorParts.Count > 0) rewards.Add(string.Join("  •  ", floorParts));
            }
            if (saved != null && saved.TowerLastHeroRewardFloor094 == floor &&
                !string.IsNullOrWhiteSpace(saved.TowerLastHeroRewardSummary094))
                rewards.Add(saved.TowerLastHeroRewardSummary094.Trim());
            if (rewards.Count == 0) rewards.Add("Saved reward receipts applied.");
            return "FLOOR " + Math.Max(1, floor).ToString(CultureInfo.InvariantCulture) +
                " CLEARED\n" + string.Join("\n", rewards) +
                "\nREWARDS CLAIMED  •  PROGRESS SAVED";
        }

        private static bool IsActualActionEvent108(M2BattleEventView value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ActorMemberId) ||
                string.IsNullOrWhiteSpace(value.EventType)) return false;
            var kind = value.EventType.Trim().ToUpperInvariant();
            return kind == "MARTIAL_HIT" || kind == "MYSTIC_HIT" || kind == "TACTICAL_HIT" ||
                   kind == "RESTORATION" || kind == "REVIVED" || kind == "CLEANSED" ||
                   kind == "STABILIZED" || kind == "GUARD" || kind == "INTERCEPTION" ||
                   kind == "RECOVERY" || kind == "AP_RECOVERY" || kind == "FORMATION_RECOVERY" ||
                   kind == "ALLY_SUPPORT" || kind == "ALLY_PROTECTED" || kind == "ENEMY_HIT";
        }

        private static string BattleTargetName108(M2BattleView battle, string unionId, string memberId)
        {
            var member = BattleMemberName108(battle, memberId);
            if (!string.IsNullOrWhiteSpace(member)) return member;
            foreach (var union in (battle?.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
                         .Concat(battle?.EnemyUnions ?? Array.Empty<M2BattleUnionView>()))
                if (union != null && StringComparer.Ordinal.Equals(union.UnionId, unionId))
                    return SafeName108(union.DisplayName, unionId);
            return unionId ?? string.Empty;
        }

        private static string BattleMemberName108(M2BattleView battle, string memberId)
        {
            if (battle == null || string.IsNullOrWhiteSpace(memberId)) return string.Empty;
            foreach (var union in (battle.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
                         .Concat(battle.EnemyUnions ?? Array.Empty<M2BattleUnionView>()))
            {
                var member = union?.Members?.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.MemberId, memberId));
                if (member != null) return SafeName108(member.DisplayName, memberId);
            }
            return memberId;
        }

        private static string SafeName108(string preferred, string fallback) =>
            !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() :
            !string.IsNullOrWhiteSpace(fallback) ? fallback.Trim() : "Unknown";

        private static string Shorten108(string value, int maximum)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var clean = value.Trim();
            return clean.Length <= maximum ? clean : clean.Substring(0, Math.Max(1, maximum - 1)).TrimEnd() + "…";
        }

        private static int ParseTowerFloorNumber108(string battleId)
        {
            if (string.IsNullOrWhiteSpace(battleId)) return 1;
            const string marker = "_ACTUAL098_";
            var start = battleId.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return 1;
            start += marker.Length;
            var end = battleId.IndexOf('_', start);
            var value = end < 0 ? battleId.Substring(start) : battleId.Substring(start, end - start);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var floor)
                ? Math.Max(1, floor) : 1;
        }

        private void ResolveFocus(M2BattleView battle)
        {
            var playerUnions = battle?.PlayerUnions ?? Array.Empty<M2BattleUnionView>();
            var existing = playerUnions.FirstOrDefault(value =>
                value != null && value.CanAct &&
                string.Equals(value.UnionId, _focusedUnionId, StringComparison.Ordinal));
            if (existing != null) return;
            var next = playerUnions.FirstOrDefault(value => value != null && value.CanAct && !value.IsSelected) ??
                       playerUnions.FirstOrDefault(value => value != null && value.CanAct);
            _focusedUnionId = next?.UnionId ?? string.Empty;
        }

        private static string PreferredTargetId(M2BattleView battle, string unionId)
        {
            if (battle == null) return string.Empty;
            var narrativeTarget = M2BattleDioramaView072.PreferredNarrativeTargetUnionId079(
                battle, unionId);
            if (!string.IsNullOrWhiteSpace(narrativeTarget)) return narrativeTarget;
            return battle.EnemyUnions?.FirstOrDefault(value => value != null &&
                       value.Members != null && value.Members.Any(member => member != null && !member.Downed))
                       ?.UnionId ?? string.Empty;
        }

        private void SetCaption(string heading, string detail)
        {
            if (_phaseText != null) _phaseText.text = heading ?? string.Empty;
            if (_statusText != null) _statusText.text = detail ?? string.Empty;
        }

        private static string HumanFailure(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "That order cannot be used right now.";
            if (message.IndexOf("ONE_FORECAST", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Choose one complete order for every active Union.";
            if (message.IndexOf("AP", StringComparison.OrdinalIgnoreCase) >= 0)
                return "That Union does not have enough AP for this order.";
            if (message.IndexOf("MP", StringComparison.OrdinalIgnoreCase) >= 0)
                return "A member does not have enough MP for the predicted Art.";
            return "That order cannot be used right now.";
        }

        private static string HumanOutcomeHeading(M2BattleView battle)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.Outcome)) return "BATTLE COMPLETE";
            if (string.Equals(battle.Outcome, "Victory", StringComparison.OrdinalIgnoreCase)) return "VICTORY";
            if (string.Equals(battle.Outcome, "Defeat", StringComparison.OrdinalIgnoreCase)) return "DEFEAT";
            if (string.Equals(battle.Outcome, "Retreat", StringComparison.OrdinalIgnoreCase)) return "WITHDRAWAL";
            return "BATTLE COMPLETE";
        }

        private static string ResolveTowerFloorLabel(string battleId)
        {
            if (string.IsNullOrWhiteSpace(battleId)) return string.Empty;

            const string battlePrefix = "ABYSS_BATTLE022_FLOOR_";
            const string actualMarker = "_ACTUAL098_";
            var normalized = battleId.Trim().ToUpperInvariant();
            var floorStart = normalized.IndexOf(battlePrefix, StringComparison.Ordinal);
            if (floorStart < 0) return string.Empty;

            var digitsStart = floorStart + battlePrefix.Length;
            if (digitsStart + 1 >= normalized.Length ||
                !int.TryParse(normalized.Substring(digitsStart, 2), NumberStyles.None,
                    CultureInfo.InvariantCulture, out var templateFloor) ||
                templateFloor < 1 || templateFloor > 10)
                return string.Empty;

            var actualMarkerStart = normalized.IndexOf(actualMarker, StringComparison.Ordinal);
            if (actualMarkerStart >= 0)
            {
                var actualStart = actualMarkerStart + actualMarker.Length;
                if (actualStart < normalized.Length && char.IsDigit(normalized[actualStart]))
                {
                    var actualLength = 0;
                    while (actualStart + actualLength < normalized.Length &&
                        char.IsDigit(normalized[actualStart + actualLength]))
                    {
                        actualLength++;
                    }

                    if (int.TryParse(normalized.Substring(actualStart, actualLength),
                        NumberStyles.None, CultureInfo.InvariantCulture, out var actualFloor) &&
                        actualFloor > 0)
                    {
                        return "FLOOR " + actualFloor.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            return "FLOOR " + templateFloor.ToString("00", CultureInfo.InvariantCulture);
        }

        private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            CancelStoryBattleIntro132();
            DetachTowerWait116();
            _coordinatorSubscribed098 = false;
            if (_coordinator != null) _coordinator.Changed -= HandleCoordinatorChanged;
            if (_roundRoutine != null) StopCoroutine(_roundRoutine);
            if (_ownedCanvas != null) Destroy(_ownedCanvas.gameObject);
        }
    }
}
