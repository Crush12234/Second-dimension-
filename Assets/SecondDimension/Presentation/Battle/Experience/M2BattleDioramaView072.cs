using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using SecondDimension.Gameplay.M2;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed class M2BattleDioramaStats072
    {
        public M2BattleDioramaStats072(
            int activeActors,
            int spawnedActors,
            int retiredActors,
            int refreshes,
            int focusTransitions,
            int playedBeats,
            int authoredVfx,
            int missingPoses)
        {
            ActiveActors = activeActors;
            SpawnedActors = spawnedActors;
            RetiredActors = retiredActors;
            Refreshes = refreshes;
            FocusTransitions = focusTransitions;
            PlayedBeats = playedBeats;
            AuthoredVfx = authoredVfx;
            MissingPoses = missingPoses;
        }

        public int ActiveActors { get; }
        public int SpawnedActors { get; }
        public int RetiredActors { get; }
        public int Refreshes { get; }
        public int FocusTransitions { get; }
        public int PlayedBeats { get; }
        public int AuthoredVfx { get; }
        public int MissingPoses { get; }
    }

    /// <summary>
    /// Read-only presentation telemetry for the exact first-hour Art path. These
    /// values are deliberately disconnected from the battle coordinator: they can
    /// certify what the 072 renderer displayed, but cannot select an Art, alter a
    /// command, or feed timing back into deterministic combat.
    /// </summary>
    public sealed class M2BattleLiveArtRecipeDiagnostics076
    {
        public M2BattleLiveArtRecipeDiagnostics076(
            int startedExactBeatCount,
            int observedImpactExactBeatCount,
            int completedExactBeatCount,
            int motionPassCount,
            int vfxSpawnCount,
            int cameraIntentCount,
            int traceGeometryCount,
            string lastCompletedArtId,
            string lastCompletedRecipeId,
            string lastCompletedMotionSignature,
            string lastCompletedVfxSignature,
            string lastCompletedSfxSignature,
            string lastCompletedMotionRecipe,
            string lastCompletedVfxRecipe,
            string lastCompletedCameraRecipe,
            string lastCompletedTraceRecipe,
            int lastCompletedDurationMilliseconds,
            int lastCompletedImpactMilliseconds,
            int lastCompletedObservedImpactMilliseconds,
            int lastCompletedObservedCompletionMilliseconds,
            int lastCompletedTraceGeometryCount,
            bool lastCompletedReducedMotion,
            float lastCompletedReactionDisplacementPixels,
            float lastCompletedActorScaleDelta,
            float lastCompletedVfxRotationDegrees,
            float lastCompletedVfxPulseDelta,
            float lastCompletedProjectileArcPixels,
            float lastCompletedProjectileRotationDegrees,
            float lastCompletedCameraOffsetPixels,
            float lastCompletedCameraScaleDelta,
            float lastCompletedCameraRotationDegrees)
        {
            StartedExactBeatCount = startedExactBeatCount;
            ObservedImpactExactBeatCount = observedImpactExactBeatCount;
            CompletedExactBeatCount = completedExactBeatCount;
            MotionPassCount = motionPassCount;
            VfxSpawnCount = vfxSpawnCount;
            CameraIntentCount = cameraIntentCount;
            TraceGeometryCount = traceGeometryCount;
            LastCompletedArtId = lastCompletedArtId ?? string.Empty;
            LastCompletedRecipeId = lastCompletedRecipeId ?? string.Empty;
            LastCompletedMotionSignature = lastCompletedMotionSignature ?? string.Empty;
            LastCompletedVfxSignature = lastCompletedVfxSignature ?? string.Empty;
            LastCompletedSfxSignature = lastCompletedSfxSignature ?? string.Empty;
            LastCompletedMotionRecipe = lastCompletedMotionRecipe ?? string.Empty;
            LastCompletedVfxRecipe = lastCompletedVfxRecipe ?? string.Empty;
            LastCompletedCameraRecipe = lastCompletedCameraRecipe ?? string.Empty;
            LastCompletedTraceRecipe = lastCompletedTraceRecipe ?? string.Empty;
            LastCompletedDurationMilliseconds = lastCompletedDurationMilliseconds;
            LastCompletedImpactMilliseconds = lastCompletedImpactMilliseconds;
            LastCompletedObservedImpactMilliseconds = lastCompletedObservedImpactMilliseconds;
            LastCompletedObservedCompletionMilliseconds = lastCompletedObservedCompletionMilliseconds;
            LastCompletedTraceGeometryCount = lastCompletedTraceGeometryCount;
            LastCompletedReducedMotion = lastCompletedReducedMotion;
            LastCompletedReactionDisplacementPixels = lastCompletedReactionDisplacementPixels;
            LastCompletedActorScaleDelta = lastCompletedActorScaleDelta;
            LastCompletedVfxRotationDegrees = lastCompletedVfxRotationDegrees;
            LastCompletedVfxPulseDelta = lastCompletedVfxPulseDelta;
            LastCompletedProjectileArcPixels = lastCompletedProjectileArcPixels;
            LastCompletedProjectileRotationDegrees = lastCompletedProjectileRotationDegrees;
            LastCompletedCameraOffsetPixels = lastCompletedCameraOffsetPixels;
            LastCompletedCameraScaleDelta = lastCompletedCameraScaleDelta;
            LastCompletedCameraRotationDegrees = lastCompletedCameraRotationDegrees;
        }

        public int StartedExactBeatCount { get; }
        public int ObservedImpactExactBeatCount { get; }
        public int CompletedExactBeatCount { get; }
        public int MotionPassCount { get; }
        public int VfxSpawnCount { get; }
        public int CameraIntentCount { get; }
        public int TraceGeometryCount { get; }
        public string LastCompletedArtId { get; }
        public string LastCompletedRecipeId { get; }
        public string LastCompletedMotionSignature { get; }
        public string LastCompletedVfxSignature { get; }
        public string LastCompletedSfxSignature { get; }
        public string LastCompletedMotionRecipe { get; }
        public string LastCompletedVfxRecipe { get; }
        public string LastCompletedCameraRecipe { get; }
        public string LastCompletedTraceRecipe { get; }
        public int LastCompletedDurationMilliseconds { get; }
        public int LastCompletedImpactMilliseconds { get; }
        public int LastCompletedObservedImpactMilliseconds { get; }
        public int LastCompletedObservedCompletionMilliseconds { get; }
        public int LastCompletedTraceGeometryCount { get; }
        public bool LastCompletedReducedMotion { get; }
        public float LastCompletedReactionDisplacementPixels { get; }
        public float LastCompletedActorScaleDelta { get; }
        public float LastCompletedVfxRotationDegrees { get; }
        public float LastCompletedVfxPulseDelta { get; }
        public float LastCompletedProjectileArcPixels { get; }
        public float LastCompletedProjectileRotationDegrees { get; }
        public float LastCompletedCameraOffsetPixels { get; }
        public float LastCompletedCameraScaleDelta { get; }
        public float LastCompletedCameraRotationDegrees { get; }

        // Compatibility aliases for callers compiled against the provisional 076_6
        // diagnostic. They now describe the last fully completed exact beat only.
        public int BeatCount => StartedExactBeatCount;
        public int ImpactBoundaryCount => ObservedImpactExactBeatCount;
        public string ArtId => LastCompletedArtId;
        public string RecipeId => LastCompletedRecipeId;
        public string MotionSignature => LastCompletedMotionSignature;
        public string VfxSignature => LastCompletedVfxSignature;
        public string SfxSignature => LastCompletedSfxSignature;
        public string MotionRecipe => LastCompletedMotionRecipe;
        public string VfxRecipe => LastCompletedVfxRecipe;
        public int DurationMilliseconds => LastCompletedDurationMilliseconds;
        public int ImpactMilliseconds => LastCompletedImpactMilliseconds;
        public bool LastBeatFullyConsumed => CompletedExactBeatCount > 0;

        public bool HasCompleteLiveConsumption =>
            CompletedExactBeatCount > 0 && ObservedImpactExactBeatCount >= CompletedExactBeatCount &&
            MotionPassCount > 0 && VfxSpawnCount > 0 && CameraIntentCount > 0 &&
            TraceGeometryCount > 0 && LastCompletedTraceGeometryCount > 0 &&
            !string.IsNullOrWhiteSpace(LastCompletedArtId) &&
            !string.IsNullOrWhiteSpace(LastCompletedRecipeId) &&
            !string.IsNullOrWhiteSpace(LastCompletedMotionSignature) &&
            !string.IsNullOrWhiteSpace(LastCompletedVfxSignature) &&
            !string.IsNullOrWhiteSpace(LastCompletedSfxSignature) &&
            !string.IsNullOrWhiteSpace(LastCompletedCameraRecipe) &&
            !string.IsNullOrWhiteSpace(LastCompletedTraceRecipe) &&
            LastCompletedDurationMilliseconds > 0 && LastCompletedImpactMilliseconds > 0 &&
            LastCompletedImpactMilliseconds < LastCompletedDurationMilliseconds &&
            LastCompletedObservedImpactMilliseconds > 0 &&
            LastCompletedObservedCompletionMilliseconds > LastCompletedObservedImpactMilliseconds;
    }

    /// <summary>
    /// Persistent 2.5D battle presentation for a focused allied Union and its current
    /// enemy target. This class owns presentation objects only. It never generates a
    /// command, selects an Art, changes HP/AP/MP, writes a save, or mutates M2BattleView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2BattleDioramaView072 : MonoBehaviour
    {
        private const string RuntimeEnemySilhouette070 = "RUNTIME_ENEMY_SILHOUETTE_070";
        public const int MinimumCriticalTopHudFontSize074 = 21;
        public const int MinimumBreakthroughHeadingFontSize074 = 26;
        public const int MinimumHpImpactFontSize076 = 22;
        public const int MinimumFocusedUnionHpFontSize076 = 22;
        public const float MinimumHpImpactVisibleSeconds076 = 1.35f;
        public const float BreakthroughNotificationSeconds076 = 3f;
        public const float BossPresentationScaleMultiplier076 = 1.72f;
        public const float TacticalHeaderMinY076 = 0.746f;
        public const float FocusedUnionHpRibbonMinY076 = 0.662f;
        public const float FocusedUnionHpRibbonMaxY076 = 0.742f;
        public const float BattlefieldStageMinY078 = 0.04f;
        public const float BattlefieldStageMaxY078 = 0.94f;
        public const float MinimumActorDockClearanceNormalized078 = 0.010f;
        public const float ActorCommandBaselineY078 =
            (M2BattleCommandHud072.MaximumTrayAnchorY +
             MinimumActorDockClearanceNormalized078 - BattlefieldStageMinY078) /
            (BattlefieldStageMaxY078 - BattlefieldStageMinY078);

        private readonly Dictionary<string, M2BattleActorRig072> _actors =
            new Dictionary<string, M2BattleActorRig072>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _presentedHpByActor076 =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly HashSet<string> _stagedDownedActors =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<M2BattleEventView> _breakthroughNotifications076 =
            new Queue<M2BattleEventView>();
        private readonly HashSet<string> _seenBreakthroughNotifications076 =
            new HashSet<string>(StringComparer.Ordinal);

        private M2BattleView _battle;
        private RectTransform _root;
        private RectTransform _stage;
        private RectTransform _effectLayer;
        private Image _backdrop;
        private Image _allyFocusPlate;
        private Image _enemyFocusPlate;
        private Image _allyMoraleFill;
        private Image _enemyMoraleFill;
        private Image _unionFocusHeader;
        private Image _tacticalRibbon;
        private Image _focusedUnionHpRibbon076;
        private Image _focusedAllyHpRail076;
        private Image _focusedAllyHpFill076;
        private Image _focusedEnemyHpRail076;
        private Image _focusedEnemyHpFill076;
        private Image _breakthroughCallout074;
        private Image _hpImpactCallout076;
        private Text _allyUnionLabel;
        private Text _enemyUnionLabel;
        private Text _allyUnionStatus076;
        private Text _enemyIntent076;
        private Text _roundChainLabel;
        private Text _moraleLabel;
        private Text _engagementLabel;
        private Text _focusedAllyHpLabel076;
        private Text _focusedEnemyHpLabel076;
        private Text _breakthroughHeading074;
        private Text _breakthroughDetail074;
        private Text _hpImpactHeading076;
        private Text _hpImpactDetail076;
        private string _backdropBattleId = string.Empty;
        private string _activeBackdropResourceKey083 = string.Empty;
        private string _roundOutcome = string.Empty;
        private string _focusedPlayerUnionId = string.Empty;
        private string _targetEnemyUnionId = string.Empty;
        private string _sameSideSupportSourceUnionId086 = string.Empty;
        private string _sameSideSupportTargetUnionId086 = string.Empty;
        private bool _sameSideSupportEnemy086;
        private int _spawnedActors;
        private int _retiredActors;
        private int _refreshes;
        private int _focusTransitions;
        private int _playedBeats;
        private int _authoredVfx;
        private int _exactRecipeBeatCount076;
        private int _exactRecipeImpactBoundaryCount076;
        private int _completedExactBeatCount076;
        private int _exactRecipeMotionPassCount076;
        private int _exactRecipeVfxSpawnCount076;
        private int _exactRecipeCameraIntentCount076;
        private int _exactRecipeTraceGeometryCount076;
        private string _lastExactArtId076 = string.Empty;
        private string _lastExactRecipeId076 = string.Empty;
        private string _lastExactMotionSignature076 = string.Empty;
        private string _lastExactVfxSignature076 = string.Empty;
        private string _lastExactSfxSignature076 = string.Empty;
        private string _lastExactMotionRecipe076 = string.Empty;
        private string _lastExactVfxRecipe076 = string.Empty;
        private string _lastExactCameraRecipe076 = string.Empty;
        private string _lastExactTraceRecipe076 = string.Empty;
        private int _lastExactDurationMilliseconds076;
        private int _lastExactImpactMilliseconds076;
        private int _lastExactObservedImpactMilliseconds076;
        private int _lastExactObservedCompletionMilliseconds076;
        private int _lastExactTraceGeometryCount076;
        private bool _lastExactReducedMotion076;
        private float _lastExactReactionDisplacementPixels076;
        private float _lastExactActorScaleDelta076;
        private float _lastExactVfxRotationDegrees076;
        private float _lastExactVfxPulseDelta076;
        private float _lastExactProjectileArcPixels076;
        private float _lastExactProjectileRotationDegrees076;
        private float _lastExactCameraOffsetPixels076;
        private float _lastExactCameraScaleDelta076;
        private float _lastExactCameraRotationDegrees076;
        private BattleArtProfile011 _currentExactProfile076;
        private Battle3DArtChoreography071 _currentExactRecipe076;
        private float _currentExactStartedRealtime076;
        private float _currentExactImpactRealtime076;
        private int _currentExactTraceGeometryCount076;
        private bool _currentExactBeatActive076;
        private bool _currentExactImpactObserved076;
        private bool _currentExactMotionConsumed076;
        private bool _currentExactVfxSpawned076;
        private bool _currentExactCameraConsumed076;
        private bool _currentExactTraceConsumed076;
        private bool _currentExactReducedMotion076;
        private AppliedMotionEnvelope076 _currentExactEnvelope076;
        private bool _tacticalOverlayVisible = true;
        private float _hpImpactVisibleUntil076;
        private int _hpImpactPresentationCount076;
        private int _focusedAllyCurrentHp076;
        private int _focusedAllyMaximumHp076;
        private int _focusedEnemyCurrentHp076;
        private int _focusedEnemyMaximumHp076;
        private string _lastHpImpactUnionId076 = string.Empty;
        private string _lastHpImpactMemberId076 = string.Empty;
        private int _lastHpImpactBefore076;
        private int _lastHpImpactAfter076;
        private int _lastHpImpactMaximum076;
        private int _lastHpImpactDelta076;
        private bool _lastHpImpactEnemy076;
        private float _breakthroughVisibleUntil076;
        private int _breakthroughNotificationPresentationCount076;
        private string _activeBreakthroughSignature076 = string.Empty;

        public RectTransform Root => _root;
        public bool IsReady => _root != null && _backdrop != null;
        public string ActiveBackdropResourceKey083 => _activeBackdropResourceKey083;
        public string ActiveBackdropTextureName083 =>
            _backdrop?.sprite?.texture?.name ?? string.Empty;
        public string FocusedPlayerUnionId => _focusedPlayerUnionId;
        public string TargetEnemyUnionId => _targetEnemyUnionId;
        public int ActiveActorCount => _actors.Count;
        public Image HpImpactCallout076 => _hpImpactCallout076;
        public Text HpImpactHeading076 => _hpImpactHeading076;
        public Text HpImpactDetail076 => _hpImpactDetail076;
        public Text FocusedAllyHpLabel076 => _focusedAllyHpLabel076;
        public Text FocusedEnemyHpLabel076 => _focusedEnemyHpLabel076;
        public Image FocusedAllyHpRail076 => _focusedAllyHpRail076;
        public Image FocusedAllyHpFill076 => _focusedAllyHpFill076;
        public Image FocusedEnemyHpRail076 => _focusedEnemyHpRail076;
        public Image FocusedEnemyHpFill076 => _focusedEnemyHpFill076;
        public int HpImpactPresentationCount076 => _hpImpactPresentationCount076;
        public int FocusedAllyCurrentHp076 => _focusedAllyCurrentHp076;
        public int FocusedAllyMaximumHp076 => _focusedAllyMaximumHp076;
        public int FocusedEnemyCurrentHp076 => _focusedEnemyCurrentHp076;
        public int FocusedEnemyMaximumHp076 => _focusedEnemyMaximumHp076;
        public string LastHpImpactUnionId076 => _lastHpImpactUnionId076;
        public string LastHpImpactMemberId076 => _lastHpImpactMemberId076;
        public int LastHpImpactBefore076 => _lastHpImpactBefore076;
        public int LastHpImpactAfter076 => _lastHpImpactAfter076;
        public int LastHpImpactMaximum076 => _lastHpImpactMaximum076;
        public int LastHpImpactDelta076 => _lastHpImpactDelta076;
        public bool LastHpImpactEnemy076 => _lastHpImpactEnemy076;
        public Image BreakthroughNotificationCallout076 => _breakthroughCallout074;
        public Text BreakthroughNotificationHeading076 => _breakthroughHeading074;
        public Text BreakthroughNotificationDetail076 => _breakthroughDetail074;
        public int BreakthroughNotificationPresentationCount076 =>
            _breakthroughNotificationPresentationCount076;
        public int QueuedBreakthroughNotificationCount076 => _breakthroughNotifications076.Count;
        public string ActiveBreakthroughNotificationSignature076 =>
            _activeBreakthroughSignature076;
        public bool BreakthroughNotificationVisible076 =>
            _breakthroughCallout074 != null &&
            _breakthroughCallout074.gameObject.activeInHierarchy;
        public float BreakthroughNotificationRemainingSeconds076 =>
            BreakthroughNotificationVisible076
                ? Mathf.Max(0f, _breakthroughVisibleUntil076 - Time.unscaledTime)
                : 0f;
        public bool HpImpactVisible076 =>
            _hpImpactCallout076 != null && _hpImpactCallout076.gameObject.activeInHierarchy;
        public float HpImpactRemainingSeconds076 =>
            HpImpactVisible076
                ? Mathf.Max(0f, _hpImpactVisibleUntil076 - Time.unscaledTime)
                : 0f;

        private void Update()
        {
            if (HpImpactVisible076 && Time.unscaledTime >= _hpImpactVisibleUntil076)
                HideHpImpact076();
            if (BreakthroughNotificationVisible076 &&
                Time.unscaledTime >= _breakthroughVisibleUntil076)
                ShowNextBreakthroughNotification076();
        }

        public M2BattleLiveArtRecipeDiagnostics076 LiveArtRecipeDiagnostics076 =>
            new M2BattleLiveArtRecipeDiagnostics076(
                _exactRecipeBeatCount076,
                _exactRecipeImpactBoundaryCount076,
                _completedExactBeatCount076,
                _exactRecipeMotionPassCount076,
                _exactRecipeVfxSpawnCount076,
                _exactRecipeCameraIntentCount076,
                _exactRecipeTraceGeometryCount076,
                _lastExactArtId076,
                _lastExactRecipeId076,
                _lastExactMotionSignature076,
                _lastExactVfxSignature076,
                _lastExactSfxSignature076,
                _lastExactMotionRecipe076,
                _lastExactVfxRecipe076,
                _lastExactCameraRecipe076,
                _lastExactTraceRecipe076,
                _lastExactDurationMilliseconds076,
                _lastExactImpactMilliseconds076,
                _lastExactObservedImpactMilliseconds076,
                _lastExactObservedCompletionMilliseconds076,
                _lastExactTraceGeometryCount076,
                _lastExactReducedMotion076,
                _lastExactReactionDisplacementPixels076,
                _lastExactActorScaleDelta076,
                _lastExactVfxRotationDegrees076,
                _lastExactVfxPulseDelta076,
                _lastExactProjectileArcPixels076,
                _lastExactProjectileRotationDegrees076,
                _lastExactCameraOffsetPixels076,
                _lastExactCameraScaleDelta076,
                _lastExactCameraRotationDegrees076);

        public IReadOnlyDictionary<string, int> ActorInstanceIds
        {
            get
            {
                var snapshot = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var entry in _actors) snapshot[entry.Key] = entry.Value.InstanceId;
                return snapshot;
            }
        }

        public M2BattleDioramaStats072 Stats
        {
            get
            {
                var missingPoses = 0;
                foreach (var actor in _actors.Values) missingPoses += actor.MissingPoseCount;
                return new M2BattleDioramaStats072(
                    _actors.Count,
                    _spawnedActors,
                    _retiredActors,
                    _refreshes,
                    _focusTransitions,
                    _playedBeats,
                    _authoredVfx,
                    missingPoses);
            }
        }

        public void Initialize(RectTransform host)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (_root == null && _actors.Count > 0)
            {
                // The host can be replaced when the flow changes screens. Unity has
                // already retired those child objects, so discard their stale handles
                // and let the next Focus create live rigs under the new host.
                _retiredActors += _actors.Count;
                _actors.Clear();
            }
            if (_root != null)
            {
                if (_root.parent == host) return;
                Dispose();
            }

            var rootObject = new GameObject(
                "Battle Diorama Experience 072",
                typeof(RectTransform), typeof(RectMask2D), typeof(CanvasGroup));
            _root = rootObject.GetComponent<RectTransform>();
            _root.SetParent(host, false);
            Stretch(_root);
            var canvas = rootObject.GetComponent<CanvasGroup>();
            canvas.interactable = false;
            canvas.blocksRaycasts = false;

            _backdrop = CreateImage(
                _root, "Authored Encounter Backdrop 072",
                Vector2.zero, Vector2.one, Color.white);
            _backdrop.raycastTarget = false;

            var atmosphere = CreateImage(
                _root, "Cinematic Readability Veil 072",
                Vector2.zero, Vector2.one, new Color(0.015f, 0.025f, 0.05f, 0.20f));
            atmosphere.raycastTarget = false;

            _stage = CreateRect(
                _root, "Focused Union Diorama Stage 072",
                new Vector2(0.015f, BattlefieldStageMinY078),
                new Vector2(0.985f, BattlefieldStageMaxY078));

            _allyFocusPlate = CreateImage(
                _stage, "Authored Ally Grounding Wash 076",
                new Vector2(0.025f, 0.175f), new Vector2(0.495f, 0.425f),
                new Color(0.48f, 0.86f, 1f, 0.36f));
            ApplyGroundShadow076(_allyFocusPlate);

            _enemyFocusPlate = CreateImage(
                _stage, "Authored Enemy Grounding Wash 076",
                new Vector2(0.505f, 0.175f), new Vector2(0.975f, 0.425f),
                new Color(1f, 0.52f, 0.42f, 0.36f));
            ApplyGroundShadow076(_enemyFocusPlate);

            _effectLayer = CreateRect(
                _stage, "Authored Battle Effects 072", Vector2.zero, Vector2.one);
            _effectLayer.SetAsLastSibling();

            _unionFocusHeader = CreateImage(
                _root, "Union Focus Header 072",
                new Vector2(0.025f, TacticalHeaderMinY076), new Vector2(0.975f, 0.818f),
                new Color(0.01f, 0.025f, 0.045f, 0.92f));
            _unionFocusHeader.raycastTarget = false;
            _allyUnionLabel = CreateText(
                _unionFocusHeader.rectTransform, "Focused Ally Union Name 072",
                new Vector2(0.02f, 0.48f), new Vector2(0.48f, 0.96f),
                TextAnchor.MiddleLeft, new Color(0.76f, 0.93f, 1f, 1f));
            _enemyUnionLabel = CreateText(
                _unionFocusHeader.rectTransform, "Target Enemy Union Name 072",
                new Vector2(0.52f, 0.48f), new Vector2(0.98f, 0.96f),
                TextAnchor.MiddleRight, new Color(1f, 0.78f, 0.72f, 1f));
            ConfigureOverlayText(_allyUnionLabel, 34, 24);
            ConfigureOverlayText(_enemyUnionLabel, 34, 24);

            _allyUnionStatus076 = CreateText(
                _unionFocusHeader.rectTransform, "Active Union AP and Formation 076",
                new Vector2(0.02f, 0.03f), new Vector2(0.48f, 0.49f),
                TextAnchor.MiddleLeft, RuntimeUi.Accent);
            _enemyIntent076 = CreateText(
                _unionFocusHeader.rectTransform, "Enemy Intent 076",
                new Vector2(0.52f, 0.03f), new Vector2(0.98f, 0.49f),
                TextAnchor.MiddleRight, RuntimeUi.Warning);
            ConfigureOverlayText(_allyUnionStatus076, 27, 19);
            ConfigureOverlayText(_enemyIntent076, 27, 19);

            _tacticalRibbon = CreateImage(
                _root, "Battle Morale and Engagement Ribbon 074",
                new Vector2(0.025f, 0.823f), new Vector2(0.975f, 0.861f),
                new Color(0.006f, 0.015f, 0.03f, 0.94f));
            _tacticalRibbon.raycastTarget = false;

            _roundChainLabel = CreateText(
                _tacticalRibbon.rectTransform, "Round and Combat Chain 074",
                new Vector2(0.012f, 0f), new Vector2(0.27f, 1f),
                TextAnchor.MiddleLeft, RuntimeUi.Text);
            ConfigureOverlayText(_roundChainLabel, 29, MinimumCriticalTopHudFontSize074);

            var moraleRail = CreateImage(
                _tacticalRibbon.rectTransform, "Allies Versus Enemies Morale Rail 074",
                new Vector2(0.29f, 0.17f), new Vector2(0.71f, 0.83f),
                new Color(0.02f, 0.035f, 0.055f, 0.96f));
            moraleRail.raycastTarget = false;
            _allyMoraleFill = CreateImage(
                moraleRail.rectTransform, "Allied Morale Fill 074",
                new Vector2(0.006f, 0.08f), new Vector2(0.5f, 0.92f),
                new Color(0.15f, 0.80f, 0.94f, 0.92f));
            _enemyMoraleFill = CreateImage(
                moraleRail.rectTransform, "Enemy Morale Fill 074",
                new Vector2(0.5f, 0.08f), new Vector2(0.994f, 0.92f),
                new Color(0.92f, 0.24f, 0.22f, 0.92f));
            _allyMoraleFill.raycastTarget = false;
            _enemyMoraleFill.raycastTarget = false;
            _moraleLabel = CreateText(
                _tacticalRibbon.rectTransform, "Allies Versus Enemies Morale Label 074",
                new Vector2(0.285f, 0f), new Vector2(0.715f, 1f),
                TextAnchor.MiddleCenter, Color.white);
            ConfigureOverlayText(_moraleLabel, 25, 18);

            _engagementLabel = CreateText(
                _tacticalRibbon.rectTransform, "Focused Engagement State 074",
                new Vector2(0.73f, 0f), new Vector2(0.988f, 1f),
                TextAnchor.MiddleRight, RuntimeUi.Warning);
            ConfigureOverlayText(_engagementLabel, 29, MinimumCriticalTopHudFontSize074);

            _focusedUnionHpRibbon076 = CreateImage(
                _root, "Focused Ally Enemy Union HP Ribbon 076",
                new Vector2(0.025f, FocusedUnionHpRibbonMinY076),
                new Vector2(0.975f, FocusedUnionHpRibbonMaxY076),
                new Color(0.006f, 0.018f, 0.034f, 0.97f));
            _focusedUnionHpRibbon076.raycastTarget = false;
            var focusedHpOutline076 = _focusedUnionHpRibbon076.gameObject.AddComponent<Outline>();
            focusedHpOutline076.effectColor = new Color(0.42f, 0.63f, 0.82f, 0.76f);
            focusedHpOutline076.effectDistance = new Vector2(1f, -1f);
            _focusedAllyHpLabel076 = CreateText(
                _focusedUnionHpRibbon076.rectTransform, "Focused Ally Union HP Value 076",
                new Vector2(0.018f, 0.41f), new Vector2(0.485f, 0.96f),
                TextAnchor.MiddleLeft, new Color(0.72f, 0.96f, 0.89f, 1f));
            _focusedEnemyHpLabel076 = CreateText(
                _focusedUnionHpRibbon076.rectTransform, "Focused Enemy Union HP Value 076",
                new Vector2(0.515f, 0.41f), new Vector2(0.982f, 0.96f),
                TextAnchor.MiddleRight, new Color(1f, 0.75f, 0.68f, 1f));
            _focusedAllyHpLabel076.fontStyle = FontStyle.Bold;
            _focusedEnemyHpLabel076.fontStyle = FontStyle.Bold;
            ConfigureOverlayText(
                _focusedAllyHpLabel076,
                31,
                MinimumFocusedUnionHpFontSize076);
            ConfigureOverlayText(
                _focusedEnemyHpLabel076,
                31,
                MinimumFocusedUnionHpFontSize076);
            _focusedAllyHpRail076 = CreateImage(
                _focusedUnionHpRibbon076.rectTransform, "Focused Ally Union HP Rail 076",
                new Vector2(0.018f, 0.08f), new Vector2(0.485f, 0.37f),
                new Color(0.012f, 0.035f, 0.052f, 1f));
            _focusedEnemyHpRail076 = CreateImage(
                _focusedUnionHpRibbon076.rectTransform, "Focused Enemy Union HP Rail 076",
                new Vector2(0.515f, 0.08f), new Vector2(0.982f, 0.37f),
                new Color(0.052f, 0.018f, 0.020f, 1f));
            _focusedAllyHpFill076 = CreateImage(
                _focusedAllyHpRail076.rectTransform, "Focused Ally Union HP Fill 076",
                new Vector2(0.008f, 0.12f), new Vector2(0.992f, 0.88f),
                new Color(0.18f, 0.92f, 0.64f, 1f));
            _focusedEnemyHpFill076 = CreateImage(
                _focusedEnemyHpRail076.rectTransform, "Focused Enemy Union HP Fill 076",
                new Vector2(0.008f, 0.12f), new Vector2(0.992f, 0.88f),
                new Color(1f, 0.24f, 0.20f, 1f));
            _focusedAllyHpRail076.raycastTarget = false;
            _focusedEnemyHpRail076.raycastTarget = false;
            _focusedAllyHpFill076.raycastTarget = false;
            _focusedEnemyHpFill076.raycastTarget = false;
            _focusedUnionHpRibbon076.gameObject.SetActive(false);

            _breakthroughCallout074 = CreateImage(
                _root, "Persistent Breakthrough Callout 074",
                new Vector2(0.025f, 0.485f), new Vector2(0.295f, 0.645f),
                new Color(0.025f, 0.12f, 0.13f, 0.98f));
            M1PremiumUi.StylePanel(_breakthroughCallout074, M1PremiumUi.Surface.Positive);
            _breakthroughCallout074.raycastTarget = false;
            _breakthroughHeading074 = CreateText(
                _breakthroughCallout074.rectTransform, "Breakthrough Heading 074",
                new Vector2(0.045f, 0.57f), new Vector2(0.955f, 0.94f),
                TextAnchor.MiddleCenter, new Color(0.52f, 1f, 0.80f, 1f));
            _breakthroughHeading074.text = "NEW ART LEARNED";
            _breakthroughHeading074.fontStyle = FontStyle.Bold;
            ConfigureOverlayText(_breakthroughHeading074, 36, MinimumBreakthroughHeadingFontSize074);
            _breakthroughDetail074 = CreateText(
                _breakthroughCallout074.rectTransform, "Breakthrough Learned Art 074",
                new Vector2(0.045f, 0.08f), new Vector2(0.955f, 0.59f),
                TextAnchor.MiddleCenter, RuntimeUi.Text);
            _breakthroughDetail074.fontStyle = FontStyle.Bold;
            ConfigureOverlayText(_breakthroughDetail074, 24, 18);
            _breakthroughCallout074.gameObject.SetActive(false);

            _hpImpactCallout076 = CreateImage(
                _root, "Persistent HP Impact Readout 076",
                new Vector2(0.305f, 0.515f), new Vector2(0.695f, 0.645f),
                new Color(0.30f, 0.055f, 0.035f, 0.98f));
            _hpImpactCallout076.raycastTarget = false;
            var hpImpactOutline076 = _hpImpactCallout076.gameObject.AddComponent<Outline>();
            hpImpactOutline076.effectColor = new Color(1f, 0.74f, 0.42f, 0.96f);
            hpImpactOutline076.effectDistance = new Vector2(2f, -2f);
            _hpImpactHeading076 = CreateText(
                _hpImpactCallout076.rectTransform, "Signed HP Impact Heading 076",
                new Vector2(0.035f, 0.50f), new Vector2(0.965f, 0.95f),
                TextAnchor.MiddleCenter, Color.white);
            _hpImpactHeading076.fontStyle = FontStyle.Bold;
            ConfigureOverlayText(_hpImpactHeading076, 35, 24);
            _hpImpactDetail076 = CreateText(
                _hpImpactCallout076.rectTransform, "Before After HP Impact Value 076",
                new Vector2(0.035f, 0.06f), new Vector2(0.965f, 0.55f),
                TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.78f, 1f));
            _hpImpactDetail076.fontStyle = FontStyle.Bold;
            ConfigureOverlayText(
                _hpImpactDetail076,
                31,
                MinimumHpImpactFontSize076);
            _hpImpactCallout076.gameObject.SetActive(false);
        }

        public void Refresh(M2BattleView battle, string focusedUnionId)
        {
            EnsureInitialized();
            ClearBreakthroughNotifications076();
            var previousBattleId076 = _battle?.BattleId ?? string.Empty;
            var nextBattleId076 = battle?.BattleId ?? string.Empty;
            if (!StringComparer.Ordinal.Equals(previousBattleId076, nextBattleId076))
            {
                HideHpImpact076();
                _hpImpactPresentationCount076 = 0;
                _lastHpImpactUnionId076 = string.Empty;
                _lastHpImpactMemberId076 = string.Empty;
                _lastHpImpactBefore076 = 0;
                _lastHpImpactAfter076 = 0;
                _lastHpImpactMaximum076 = 0;
                _lastHpImpactDelta076 = 0;
                _lastHpImpactEnemy076 = false;
                _seenBreakthroughNotifications076.Clear();
                _breakthroughNotificationPresentationCount076 = 0;
            }
            _battle = battle;
            _presentedHpByActor076.Clear();
            SeedPresentedHp076(battle?.PlayerUnions, false);
            SeedPresentedHp076(battle?.EnemyUnions, true);
            _roundOutcome = battle?.Outcome ?? string.Empty;
            _stagedDownedActors.Clear();
            _refreshes++;

            if (battle == null)
            {
                RetireAllActors();
                SetLabel(_allyUnionLabel, string.Empty);
                SetLabel(_enemyUnionLabel, string.Empty);
                SetLabel(_allyUnionStatus076, string.Empty);
                SetLabel(_enemyIntent076, string.Empty);
                SetLabel(_roundChainLabel, string.Empty);
                SetLabel(_moraleLabel, string.Empty);
                SetLabel(_engagementLabel, string.Empty);
                SetFocusedUnionHpReadouts076(null, null);
                if (_breakthroughCallout074 != null) _breakthroughCallout074.gameObject.SetActive(false);
                return;
            }

            RefreshBackdrop(battle.BattleId);
            var player = FindUnion(battle.PlayerUnions, focusedUnionId) ??
                         FindSelectedPlayerUnion(battle.PlayerUnions) ??
                         FirstUnion(battle.PlayerUnions);
            var targetId = PreferredNarrativeTargetUnionId079(battle, player?.UnionId);
            if (string.IsNullOrWhiteSpace(targetId) &&
                FindUnion(battle.EnemyUnions, _targetEnemyUnionId) != null)
                targetId = _targetEnemyUnionId;
            var enemy = FindUnion(battle.EnemyUnions, targetId) ?? FirstLivingUnion(battle.EnemyUnions) ??
                        FirstUnion(battle.EnemyUnions);
            Focus(player?.UnionId, enemy?.UnionId);
        }

        public void SetTacticalOverlayVisible(bool visible)
        {
            _tacticalOverlayVisible = visible;
            if (_unionFocusHeader != null) _unionFocusHeader.gameObject.SetActive(visible);
            if (_tacticalRibbon != null) _tacticalRibbon.gameObject.SetActive(visible);
            if (_focusedUnionHpRibbon076 != null)
                _focusedUnionHpRibbon076.gameObject.SetActive(visible && _battle != null);
            if (!visible) ClearBreakthroughNotifications076();
        }

        /// <summary>
        /// Supplies the already-authoritative post-round outcome without snapping the
        /// visible actors to their final poses before the event choreography plays.
        /// </summary>
        public void PrepareResolvedRound(M2BattleView after)
        {
            _roundOutcome = after?.Outcome ?? string.Empty;
        }

        public void Focus(string playerUnionId, string targetUnionId)
        {
            EnsureInitialized();
            ClearSameSideSupportFocus086();
            var player = FindUnion(_battle?.PlayerUnions, playerUnionId) ??
                         FindUnion(_battle?.PlayerUnions, _focusedPlayerUnionId) ??
                         FirstUnion(_battle?.PlayerUnions);
            var enemy = FindUnion(_battle?.EnemyUnions, targetUnionId) ??
                        FindUnion(_battle?.EnemyUnions, _targetEnemyUnionId) ??
                        FirstLivingUnion(_battle?.EnemyUnions) ?? FirstUnion(_battle?.EnemyUnions);

            var nextPlayer = player?.UnionId ?? string.Empty;
            var nextEnemy = enemy?.UnionId ?? string.Empty;
            if (!StringComparer.Ordinal.Equals(nextPlayer, _focusedPlayerUnionId) ||
                !StringComparer.Ordinal.Equals(nextEnemy, _targetEnemyUnionId))
                _focusTransitions++;

            _focusedPlayerUnionId = nextPlayer;
            _targetEnemyUnionId = nextEnemy;
            SyncVisibleActors(player, enemy);
            LayoutUnion(player, false, false);
            LayoutUnion(enemy, true, true);
            ApplyFocusSideColors086(false, true);
            if (_effectLayer != null) _effectLayer.SetAsLastSibling();
            SetUnionHeader(_allyUnionLabel, player, "Guild Union", false);
            SetUnionHeader(_enemyUnionLabel, enemy, "Enemy Union", true);
            SetLabel(_allyUnionStatus076, ActiveUnionStatus076(player));
            SetLabel(_enemyIntent076, EnemyIntent076(enemy, player, _battle));
            SetFocusedUnionHpReadouts076(player, enemy);
            UpdateTacticalReadout(player, enemy);
            ReturnToOverview();
        }

        public M2BattleActorRig072 ResolveActor(string memberId, string unionId)
        {
            if (!string.IsNullOrWhiteSpace(unionId) && !string.IsNullOrWhiteSpace(memberId))
            {
                var playerKey = ActorKey(false, unionId, memberId);
                if (_actors.TryGetValue(playerKey, out var player)) return player;
                var enemyKey = ActorKey(true, unionId, memberId);
                if (_actors.TryGetValue(enemyKey, out var enemy)) return enemy;
            }

            M2BattleActorRig072 unionLeaderFallback = null;
            foreach (var actor in _actors.Values)
            {
                if (!string.IsNullOrWhiteSpace(memberId) &&
                    StringComparer.Ordinal.Equals(actor.MemberId, memberId) &&
                    (string.IsNullOrWhiteSpace(unionId) ||
                     StringComparer.Ordinal.Equals(actor.UnionId, unionId)))
                    return actor;
                if (!string.IsNullOrWhiteSpace(unionId) &&
                    StringComparer.Ordinal.Equals(actor.UnionId, unionId) &&
                    unionLeaderFallback == null && !actor.Downed)
                    unionLeaderFallback = actor;
            }
            return unionLeaderFallback;
        }

        public bool TryGetPresentedMemberHp076(
            string memberId,
            string unionId,
            out int currentHp,
            out int maximumHp)
        {
            currentHp = 0;
            maximumHp = 0;
            if (string.IsNullOrWhiteSpace(memberId) || string.IsNullOrWhiteSpace(unionId))
                return false;
            var enemy = false;
            var member = FindMember076(_battle?.PlayerUnions, unionId, memberId);
            if (member == null)
            {
                enemy = true;
                member = FindMember076(_battle?.EnemyUnions, unionId, memberId);
            }
            if (member == null) return false;
            maximumHp = Math.Max(0, member.MaximumHp);
            var key = ActorKey(enemy, unionId, memberId);
            currentHp = _presentedHpByActor076.TryGetValue(key, out var presented)
                ? Mathf.Clamp(presented, 0, maximumHp)
                : Mathf.Clamp(member.CurrentHp, 0, maximumHp);
            return true;
        }

        /// <summary>
        /// Applies the HP meaning of an authoritative battle event to the visible
        /// presentation ledger. It never changes M2BattleView or any combat state.
        /// Keeping this ledger separate also prevents the pre-round snapshot rebound
        /// performed by each camera focus from restoring an already-presented hit.
        /// </summary>
        public bool PresentHpEvent076(M2BattleEventView item)
        {
            if (item == null || item.Amount <= 0) return false;
            var eventType = (item.EventType ?? string.Empty).Trim().ToUpperInvariant();
            var signedDelta = 0;
            switch (eventType)
            {
                case "MARTIAL_HIT":
                case "MYSTIC_HIT":
                case "TACTICAL_HIT":
                case "ENEMY_HIT":
                case "INTERCEPTION":
                    signedDelta = -item.Amount;
                    break;
                case "RESTORATION":
                case "REVIVED":
                    signedDelta = item.Amount;
                    break;
                default:
                    return false;
            }

            var unionId = string.IsNullOrWhiteSpace(item.TargetUnionId)
                ? item.UnionId
                : item.TargetUnionId;
            var memberId = string.IsNullOrWhiteSpace(item.TargetMemberId)
                ? item.MemberId
                : item.TargetMemberId;
            if (string.IsNullOrWhiteSpace(unionId) || string.IsNullOrWhiteSpace(memberId)) return false;

            var actor = ResolveActor(memberId, unionId);
            if (actor != null && !StringComparer.Ordinal.Equals(actor.MemberId, memberId)) actor = null;
            var enemy = actor != null ? actor.Enemy : IsEnemyUnion(unionId);
            var key = ActorKey(enemy, unionId, memberId);
            var member = FindMember076(enemy ? _battle?.EnemyUnions : _battle?.PlayerUnions, unionId, memberId);
            var maximumHp = actor != null
                ? actor.PresentedMaximumHp076
                : member?.MaximumHp ?? 0;
            if (maximumHp <= 0) return false;

            if (!_presentedHpByActor076.TryGetValue(key, out var currentHp))
                currentHp = actor != null ? actor.PresentedCurrentHp076 : member?.CurrentHp ?? 0;
            var nextHp = Mathf.Clamp(currentHp + signedDelta, 0, maximumHp);
            var actualDelta = nextHp - currentHp;
            if (actualDelta == 0) return false;
            _presentedHpByActor076[key] = nextHp;
            if (StringComparer.Ordinal.Equals(eventType, "REVIVED") && nextHp > 0)
                _stagedDownedActors.Remove(key);
            actor?.PresentHp076(nextHp, maximumHp);
            if (HasSameSideSupportFocus086())
            {
                var side086 = _sameSideSupportEnemy086
                    ? _battle?.EnemyUnions
                    : _battle?.PlayerUnions;
                var source086 = FindUnion(side086, _sameSideSupportSourceUnionId086);
                var recipient086 = FindUnion(side086, _sameSideSupportTargetUnionId086);
                SetUnionHeader(
                    _allyUnionLabel,
                    source086,
                    _sameSideSupportEnemy086 ? "Enemy Support Source" : "Guild Support Source",
                    _sameSideSupportEnemy086);
                SetUnionHeader(
                    _enemyUnionLabel,
                    recipient086,
                    _sameSideSupportEnemy086 ? "Enemy Rescue Target" : "Allied Rescue Target",
                    _sameSideSupportEnemy086);
                SetSameSideSupportReadout086(source086, recipient086, _sameSideSupportEnemy086);
            }
            else
            {
                var focusedPlayer076 = FindUnion(_battle?.PlayerUnions, _focusedPlayerUnionId);
                var focusedEnemy076 = FindUnion(_battle?.EnemyUnions, _targetEnemyUnionId);
                SetUnionHeader(_allyUnionLabel, focusedPlayer076, "Guild Union", false);
                SetUnionHeader(_enemyUnionLabel, focusedEnemy076, "Enemy Union", true);
                SetFocusedUnionHpReadouts076(focusedPlayer076, focusedEnemy076);
            }
            PresentHpImpact076(
                unionId,
                memberId,
                actor,
                member,
                enemy,
                currentHp,
                nextHp,
                maximumHp,
                actualDelta);
            return true;
        }

        private void PresentHpImpact076(
            string unionId,
            string memberId,
            M2BattleActorRig072 actor,
            M2BattleMemberView member,
            bool enemy,
            int beforeHp,
            int afterHp,
            int maximumHp,
            int signedDelta)
        {
            if (_hpImpactCallout076 == null ||
                _hpImpactHeading076 == null ||
                _hpImpactDetail076 == null)
                return;

            var displayName076 = actor?.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName076))
                displayName076 = member?.DisplayName;
            if (string.IsNullOrWhiteSpace(displayName076))
                displayName076 = enemy ? "Enemy" : "Guild Member";
            var signed076 = (signedDelta > 0 ? "+" : string.Empty) +
                            signedDelta.ToString(CultureInfo.InvariantCulture);
            var bossMember076 = enemy && (actor?.BossEnemy075 == true ||
                                           M2BattleActorRig072.IsGateEaterBossMember076(memberId));
            _hpImpactHeading076.text =
                (bossMember076 ? "BOSS MEMBER" : enemy ? "ENEMY" : "ALLY") + "  •  " +
                displayName076.ToUpperInvariant() + "  •  " +
                signed076 + " HP";
            var impactedUnion076 = FindUnion(
                enemy ? _battle?.EnemyUnions : _battle?.PlayerUnions,
                unionId);
            PresentedUnionHp076(
                impactedUnion076,
                enemy,
                out var unionCurrentHp076,
                out var unionMaximumHp076);
            _hpImpactDetail076.text =
                (bossMember076 ? "BOSS MEMBER HP " : "MEMBER HP ") +
                beforeHp.ToString(CultureInfo.InvariantCulture) +
                "  →  " + afterHp.ToString(CultureInfo.InvariantCulture) +
                " / " + maximumHp.ToString(CultureInfo.InvariantCulture) + "\n" +
                (enemy ? "ENEMY" : "GUILD") + " UNION TOTAL HP " +
                unionCurrentHp076.ToString(CultureInfo.InvariantCulture) + " / " +
                unionMaximumHp076.ToString(CultureInfo.InvariantCulture);

            var restored076 = signedDelta > 0;
            _hpImpactCallout076.color = restored076
                ? new Color(0.025f, 0.24f, 0.15f, 0.98f)
                : enemy
                    ? new Color(0.34f, 0.045f, 0.035f, 0.98f)
                    : new Color(0.34f, 0.095f, 0.025f, 0.98f);
            _hpImpactHeading076.color = restored076
                ? new Color(0.68f, 1f, 0.80f, 1f)
                : new Color(1f, 0.82f, 0.68f, 1f);
            _hpImpactDetail076.color = Color.white;
            _lastHpImpactUnionId076 = unionId ?? string.Empty;
            _lastHpImpactMemberId076 = memberId ?? string.Empty;
            _lastHpImpactBefore076 = beforeHp;
            _lastHpImpactAfter076 = afterHp;
            _lastHpImpactMaximum076 = maximumHp;
            _lastHpImpactDelta076 = signedDelta;
            _lastHpImpactEnemy076 = enemy;
            _hpImpactVisibleUntil076 = Time.unscaledTime + MinimumHpImpactVisibleSeconds076;
            _hpImpactPresentationCount076++;
            _hpImpactCallout076.gameObject.SetActive(true);
            _hpImpactCallout076.transform.SetAsLastSibling();
        }

        private void HideHpImpact076()
        {
            _hpImpactVisibleUntil076 = 0f;
            if (_hpImpactCallout076 != null)
                _hpImpactCallout076.gameObject.SetActive(false);
        }

        private void SetFocusedUnionHpReadouts076(
            M2BattleUnionView player,
            M2BattleUnionView enemy)
        {
            SetFocusedUnionHpReadouts086(
                player,
                false,
                "ALLY UNION HP",
                enemy,
                true,
                "ENEMY UNION HP");
        }

        private void SetSameSideSupportReadout086(
            M2BattleUnionView source,
            M2BattleUnionView recipient,
            bool enemy)
        {
            SetLabel(
                _allyUnionStatus076,
                (enemy ? "ENEMY SUPPORT SOURCE" : "SUPPORT SOURCE") + "  •  AP " +
                Math.Max(0, source?.CurrentAp ?? 0).ToString(CultureInfo.InvariantCulture) + "/" +
                Math.Max(0, source?.MaximumAp ?? 0).ToString(CultureInfo.InvariantCulture));
            SetLabel(
                _enemyIntent076,
                (enemy ? "ENEMY RESCUE TARGET" : "ALLY RESCUE TARGET") +
                "  •  SUPPORT IN TRANSIT");
            SetFocusedUnionHpReadouts086(
                source,
                enemy,
                enemy ? "ENEMY SUPPORT SOURCE HP" : "SUPPORT SOURCE HP",
                recipient,
                enemy,
                enemy ? "ENEMY RESCUE TARGET HP" : "ALLY RESCUE TARGET HP");
        }

        private void SetFocusedUnionHpReadouts086(
            M2BattleUnionView left,
            bool leftEnemy,
            string leftHeading,
            M2BattleUnionView right,
            bool rightEnemy,
            string rightHeading)
        {
            PresentedUnionHp076(
                left,
                leftEnemy,
                out _focusedAllyCurrentHp076,
                out _focusedAllyMaximumHp076);
            PresentedUnionHp076(
                right,
                rightEnemy,
                out _focusedEnemyCurrentHp076,
                out _focusedEnemyMaximumHp076);
            SetLabel(
                _focusedAllyHpLabel076,
                left == null
                    ? string.Empty
                    : leftHeading + "  " +
                      _focusedAllyCurrentHp076.ToString(CultureInfo.InvariantCulture) + " / " +
                      _focusedAllyMaximumHp076.ToString(CultureInfo.InvariantCulture));
            SetLabel(
                _focusedEnemyHpLabel076,
                right == null
                    ? string.Empty
                    : rightHeading + "  " +
                      _focusedEnemyCurrentHp076.ToString(CultureInfo.InvariantCulture) + " / " +
                      _focusedEnemyMaximumHp076.ToString(CultureInfo.InvariantCulture));
            SetGeometryFill076(
                _focusedAllyHpFill076,
                _focusedAllyMaximumHp076 <= 0
                    ? 0f
                    : _focusedAllyCurrentHp076 / (float)_focusedAllyMaximumHp076);
            SetGeometryFill076(
                _focusedEnemyHpFill076,
                _focusedEnemyMaximumHp076 <= 0
                    ? 0f
                    : _focusedEnemyCurrentHp076 / (float)_focusedEnemyMaximumHp076);
            if (_focusedUnionHpRibbon076 != null)
                _focusedUnionHpRibbon076.gameObject.SetActive(
                    _tacticalOverlayVisible && (left != null || right != null));
        }

        private bool HasSameSideSupportFocus086() =>
            !string.IsNullOrWhiteSpace(_sameSideSupportSourceUnionId086) &&
            !string.IsNullOrWhiteSpace(_sameSideSupportTargetUnionId086) &&
            !StringComparer.Ordinal.Equals(
                _sameSideSupportSourceUnionId086,
                _sameSideSupportTargetUnionId086);

        private void ClearSameSideSupportFocus086()
        {
            _sameSideSupportSourceUnionId086 = string.Empty;
            _sameSideSupportTargetUnionId086 = string.Empty;
            _sameSideSupportEnemy086 = false;
        }

        private void ApplyFocusSideColors086(bool leftEnemy, bool rightEnemy)
        {
            if (_allyFocusPlate != null)
                _allyFocusPlate.color = FocusGroundColor086(leftEnemy);
            if (_enemyFocusPlate != null)
                _enemyFocusPlate.color = FocusGroundColor086(rightEnemy);
            if (_allyUnionLabel != null)
                _allyUnionLabel.color = FocusHeaderColor086(leftEnemy);
            if (_enemyUnionLabel != null)
                _enemyUnionLabel.color = FocusHeaderColor086(rightEnemy);
            if (_allyUnionStatus076 != null)
                _allyUnionStatus076.color = leftEnemy ? RuntimeUi.Warning : RuntimeUi.Accent;
            if (_enemyIntent076 != null)
                _enemyIntent076.color = rightEnemy ? RuntimeUi.Warning : RuntimeUi.Accent;
            if (_focusedAllyHpLabel076 != null)
                _focusedAllyHpLabel076.color = FocusHpLabelColor086(leftEnemy);
            if (_focusedEnemyHpLabel076 != null)
                _focusedEnemyHpLabel076.color = FocusHpLabelColor086(rightEnemy);
            if (_focusedAllyHpRail076 != null)
                _focusedAllyHpRail076.color = FocusHpRailColor086(leftEnemy);
            if (_focusedEnemyHpRail076 != null)
                _focusedEnemyHpRail076.color = FocusHpRailColor086(rightEnemy);
            if (_focusedAllyHpFill076 != null)
                _focusedAllyHpFill076.color = FocusHpFillColor086(leftEnemy);
            if (_focusedEnemyHpFill076 != null)
                _focusedEnemyHpFill076.color = FocusHpFillColor086(rightEnemy);
        }

        private static Color FocusGroundColor086(bool enemy) => enemy
            ? new Color(1f, 0.52f, 0.42f, 0.36f)
            : new Color(0.48f, 0.86f, 1f, 0.36f);

        private static Color FocusHeaderColor086(bool enemy) => enemy
            ? new Color(1f, 0.78f, 0.72f, 1f)
            : new Color(0.76f, 0.93f, 1f, 1f);

        private static Color FocusHpLabelColor086(bool enemy) => enemy
            ? new Color(1f, 0.75f, 0.68f, 1f)
            : new Color(0.72f, 0.96f, 0.89f, 1f);

        private static Color FocusHpRailColor086(bool enemy) => enemy
            ? new Color(0.052f, 0.018f, 0.020f, 1f)
            : new Color(0.012f, 0.035f, 0.052f, 1f);

        private static Color FocusHpFillColor086(bool enemy) => enemy
            ? new Color(1f, 0.24f, 0.20f, 1f)
            : new Color(0.18f, 0.92f, 0.64f, 1f);

        private void PresentedUnionHp076(
            M2BattleUnionView union,
            bool enemy,
            out int currentHp,
            out int maximumHp)
        {
            currentHp = 0;
            maximumHp = 0;
            if (union?.Members == null) return;
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                if (member == null) continue;
                var memberMaximum076 = Math.Max(0, member.MaximumHp);
                maximumHp += memberMaximum076;
                var key = ActorKey(enemy, union.UnionId, member.MemberId);
                var memberCurrent076 = _presentedHpByActor076.TryGetValue(key, out var presented)
                    ? presented
                    : member.CurrentHp;
                currentHp += Mathf.Clamp(memberCurrent076, 0, memberMaximum076);
            }
        }

        private static void SetGeometryFill076(Image fill, float ratio)
        {
            if (fill == null) return;
            var maximumAnchor076 = fill.rectTransform.anchorMax;
            maximumAnchor076.x = Mathf.Lerp(0.008f, 0.992f, Mathf.Clamp01(ratio));
            fill.rectTransform.anchorMax = maximumAnchor076;
        }

        public IEnumerator PlayBeat(
            BattlePresentationBeat beat,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip,
            Action contact = null,
            int artLevel089 = M2ArtLevelPresentation089.MinimumLevel)
        {
            EnsureInitialized();
            if (beat == null || !beat.VisuallyStaged ||
                beat.Family == BattleBeatFamily.IntentionallyNonVisual)
                yield break;

            _playedBeats++;
            FocusForBeat(beat);

            if (beat.Family == BattleBeatFamily.Establishing)
            {
                ReturnToOverview();
                yield return WaitScaled(reducedMotion ? 0.08f : 0.32f, speed, skip);
                yield break;
            }

            if (beat.Family == BattleBeatFamily.Result)
            {
                yield return PlayResult(reducedMotion, speed, skip);
                yield break;
            }

            var actor = ResolveActor(beat.ActorMemberId, beat.ActorUnionId);
            var target = ResolveActor(beat.TargetMemberId, beat.TargetUnionId);
            if (beat.Family == BattleBeatFamily.Downed)
            {
                var downed = target ?? actor;
                if (downed != null)
                {
                    _stagedDownedActors.Add(ActorKey(
                        downed.Enemy,
                        downed.UnionId,
                        downed.MemberId));
                    Emphasize(downed, null);
                    yield return downed.CrossfadeToPose(
                        BattleArtPoseDirector011.Downed,
                        reducedMotion ? 0.04f : 0.18f,
                        speed,
                        skip);
                }
                yield break;
            }

            if (actor == null)
            {
                ReturnToOverview();
                yield break;
            }

            var profile = BattleArtRuntimeRegistry011.ResolveProfile(beat.ArtId, beat.Family);
            var hasExactRecipe076 = TryResolveLiveExactRecipe076(
                profile,
                beat.ArtId,
                out var exactRecipe076);
            var harmful = IsHarmful(beat.Family);
            var supportive = IsSupportive(beat.Family);
            var nonContact076 = beat.Family == BattleBeatFamily.CommandCommit ||
                                beat.Family == BattleBeatFamily.Positioning ||
                                beat.Family == BattleBeatFamily.Learning ||
                                beat.Family == BattleBeatFamily.Breakthrough ||
                                beat.Family == BattleBeatFamily.Retreat;
            if (hasExactRecipe076)
                BeginExactRecipeBeat076(
                    profile,
                    exactRecipe076,
                    reducedMotion,
                    target != null && !nonContact076,
                    harmful);
            var primaryColor = BattleArtRuntimeRegistry011.ParseColor(
                profile?.primaryColor,
                actor.Enemy ? new Color(1f, 0.34f, 0.28f, 1f) : new Color(0.30f, 0.82f, 1f, 1f));
            var artEffectScale089 = M2ArtLevelPresentation089.EffectScale(artLevel089);
            var artMotionScale089 = M2ArtLevelPresentation089.MotionScale(artLevel089);
            if (nonContact076)
            {
                yield return PlayNonContactBeat(actor, beat.Family, profile, primaryColor,
                    exactRecipe076, reducedMotion, speed, skip, contact);
                yield break;
            }
            Emphasize(actor, target);

            // A manifest path is only a promise, not a loadable presentation object.
            // Resolve that promise before reserving projectile time so a missing or
            // still-refreshing exact sprite cannot silently erase a support transfer.
            // Support beats may reuse the existing authored family effect as their
            // travel token; this changes presentation only and never battle authority.
            var projectilePath086 = target == null
                ? string.Empty
                : ResolveLoadableProjectilePath086(
                    profile?.projectileResourcePath,
                    beat.Family,
                    supportive);
            var hasProjectile076 = !string.IsNullOrWhiteSpace(projectilePath086);
            var timing076 = ResolveLiveArtTiming076(
                exactRecipe076,
                profile,
                harmful,
                hasProjectile076,
                reducedMotion);
            var anticipationDuration = timing076.AnticipationSeconds;
            var fieldPath = profile?.fieldResourcePath ?? string.Empty;
            yield return RunTogether(
                actor.CrossfadeToPose(
                    beat.Family == BattleBeatFamily.Recovery
                        ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.Anticipation,
                    anticipationDuration,
                    speed,
                    skip),
                AnimateEffect(
                    fieldPath,
                    actor,
                    exactRecipe076 == null
                        ? reducedMotion ? 0.06f : 0.26f
                        : anticipationDuration,
                    250f * artEffectScale089,
                    primaryColor,
                    speed,
                    skip,
                    reducedMotion,
                    exactRecipe076),
                AnimateStageCameraIntent076(
                    exactRecipe076,
                    anticipationDuration,
                    reducedMotion,
                    speed,
                    skip));

            var activePose = ExactMotionPose076(exactRecipe076, beat.Family);
            var actionDuration = timing076.ActionSeconds;
            var approachOffset = harmful && target != null
                ? ApproachOffset(actor, target, reducedMotion ? 42f : 150f)
                : new Vector2(0f, reducedMotion ? 5f : 18f);
            if (!reducedMotion) approachOffset *= artMotionScale089;
            approachOffset = ApplyExactMotionOffset076(
                approachOffset,
                actor,
                exactRecipe076,
                reducedMotion);
            if (hasExactRecipe076)
            {
                _exactRecipeMotionPassCount076++;
                _currentExactMotionConsumed076 = true;
            }
            yield return RunTogether(
                actor.CrossfadeToPose(activePose, actionDuration, speed, skip),
                AnimateActorOffset(
                    actor,
                    approachOffset,
                    actionDuration,
                    ExactMotionScale076(exactRecipe076, reducedMotion),
                    speed,
                    skip),
                AnimateEffect(
                    profile?.trailResourcePath ?? string.Empty,
                    actor,
                    actionDuration,
                    230f * artEffectScale089,
                    primaryColor,
                    speed,
                    skip,
                    reducedMotion,
                    exactRecipe076),
                AnimateExactTrace076(
                    exactRecipe076,
                    actor,
                    target,
                    actionDuration,
                    primaryColor,
                    reducedMotion,
                    speed,
                    skip));

            if (hasProjectile076)
                yield return AnimateProjectile(
                    projectilePath086,
                    actor,
                    target,
                    timing076.ProjectileSeconds,
                    150f * artEffectScale089,
                    primaryColor,
                    speed,
                    skip,
                    reducedMotion,
                    exactRecipe076);

            if (ShouldSkip(skip))
            {
                ReturnToOverview();
                AbandonCurrentExactBeat076();
                yield break;
            }
            // Audio remains owned by M2BattleSequenceDirector072. This callback gives
            // that director one frame-accurate impact boundary without coupling the
            // visual diorama to any sound source or playing a cue twice.
            contact?.Invoke();
            if (hasExactRecipe076) ObserveCurrentExactImpact076();
            var impactPath = profile?.impactResourcePath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(impactPath))
                impactPath = FallbackAuthoredEffectPath(beat.Family);
            var impactDuration = timing076.ImpactEffectSeconds;
            var impactTarget = target ?? actor;
            var reactionPose = beat.Family == BattleBeatFamily.Recovery
                ? BattleArtPoseDirector011.Idle
                : supportive
                ? BattleArtPoseDirector011.Recovery
                : BattleArtPoseDirector011.HitReaction;
            var impactSize089 = (harmful ? 320f : 270f) * artEffectScale089;
            yield return RunTogether(
                AnimateEffect(
                    impactPath,
                    impactTarget,
                    impactDuration,
                    impactSize089,
                    primaryColor,
                    speed,
                    skip,
                    reducedMotion,
                    exactRecipe076),
                AnimateArtLevelAccents089(
                    impactPath,
                    impactTarget,
                    impactDuration,
                    impactSize089,
                    primaryColor,
                    artLevel089,
                    speed,
                    skip,
                    reducedMotion),
                target == null
                    ? Empty()
                    : target.CrossfadeToPose(reactionPose, impactDuration, speed, skip),
                target == null
                    ? Empty()
                    : AnimateReaction(
                        target,
                        harmful,
                        impactDuration,
                        reducedMotion,
                        speed,
                        skip));

            var hitStop = timing076.HitStopSeconds;
            if (!reducedMotion && harmful)
                yield return WaitScaled(hitStop, speed, skip);

            var recoveryDuration = timing076.RecoverySeconds;
            yield return RunTogether(
                actor.CrossfadeToPose(
                    beat.Family == BattleBeatFamily.Recovery
                        ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.Recovery,
                    recoveryDuration,
                    speed,
                    skip),
                AnimateActorHome(actor, recoveryDuration, speed, skip),
                target == null
                    ? Empty()
                    : target.CrossfadeToPose(
                        target.Downed
                            ? BattleArtPoseDirector011.Downed
                            : beat.Family == BattleBeatFamily.Recovery
                                ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.Recovery,
                        recoveryDuration,
                        speed,
                        skip),
                target == null
                    ? Empty()
                    : AnimateActorHome(target, recoveryDuration, speed, skip),
                AnimateStageCameraHome076(recoveryDuration, speed, skip));

            actor.SetPoseImmediate(actor.Downed
                ? BattleArtPoseDirector011.Downed
                : BattleArtPoseDirector011.Idle);
            if (target != null)
                target.SetPoseImmediate(target.Downed
                    ? BattleArtPoseDirector011.Downed
                    : BattleArtPoseDirector011.Idle);
            ReturnToOverview();
            if (hasExactRecipe076 && !ShouldSkip(skip))
                CompleteCurrentExactBeat076(actor, target);
            else if (hasExactRecipe076)
                AbandonCurrentExactBeat076();
        }

        private IEnumerator AnimateArtLevelAccents089(
            string effectPath,
            M2BattleActorRig072 target,
            float impactDuration,
            float impactSize,
            Color baseColor,
            int artLevel,
            Func<float> speed,
            Func<bool> skip,
            bool reducedMotion)
        {
            var pulses = reducedMotion
                ? 0
                : M2ArtLevelPresentation089.AccentPulseCount(artLevel);
            if (pulses <= 0 || target == null) yield break;

            var accent = M2ArtLevelPresentation089.AccentColor(baseColor, artLevel);
            for (var index = 0; index < pulses && !ShouldSkip(skip); index++)
            {
                var size = impactSize * (0.62f + index * 0.16f);
                var duration = Mathf.Clamp(impactDuration * 0.42f, 0.07f, 0.16f);
                // Accent passes deliberately omit the exact-recipe descriptor. The
                // authored recipe remains a single authority-backed action while
                // mastery adds presentation-only echo pulses.
                yield return AnimateEffect(
                    effectPath,
                    target,
                    duration,
                    size,
                    accent,
                    speed,
                    skip,
                    false,
                    null);
            }
        }

        private IEnumerator PlayNonContactBeat(
            M2BattleActorRig072 actor,
            BattleBeatFamily family,
            BattleArtProfile011 profile,
            Color tint,
            Battle3DArtChoreography071 exactRecipe076,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip,
            Action contact)
        {
            Emphasize(actor, null);
            var timing076 = ResolveLiveArtTiming076(
                exactRecipe076,
                profile,
                false,
                false,
                reducedMotion);
            var anticipation = timing076.AnticipationSeconds;
            yield return RunTogether(
                actor.CrossfadeToPose(
                    family == BattleBeatFamily.Recovery
                        ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.Anticipation,
                    anticipation, speed, skip),
                AnimateStageCameraIntent076(
                    exactRecipe076,
                    anticipation,
                    reducedMotion,
                    speed,
                    skip));

            var celebration = family == BattleBeatFamily.Learning ||
                              family == BattleBeatFamily.Breakthrough;
            var actionPose = family == BattleBeatFamily.Retreat
                ? BattleArtPoseDirector011.Recovery
                : celebration
                    ? BattleArtPoseDirector011.Victory
                    : family == BattleBeatFamily.CommandCommit
                        ? BattleArtPoseDirector011.Anticipation
                        : ExactMotionPose076(exactRecipe076, family);
            var offset = family == BattleBeatFamily.Positioning
                ? new Vector2(actor.Enemy ? -92f : 92f, 12f)
                : celebration
                    ? new Vector2(0f, reducedMotion ? 7f : 24f)
                    : family == BattleBeatFamily.Retreat
                        ? new Vector2(actor.Enemy ? 72f : -72f, 0f)
                        : new Vector2(0f, reducedMotion ? 4f : 12f);
            var effectPath = family == BattleBeatFamily.Positioning
                ? profile?.trailResourcePath ?? string.Empty
                : !string.IsNullOrWhiteSpace(profile?.fieldResourcePath)
                    ? profile.fieldResourcePath
                    : profile?.impactResourcePath ?? string.Empty;
            offset = ApplyExactMotionOffset076(offset, actor, exactRecipe076, reducedMotion);
            var duration = timing076.ActionSeconds;
            if (exactRecipe076 != null)
            {
                _exactRecipeMotionPassCount076++;
                _currentExactMotionConsumed076 = true;
            }
            yield return RunTogether(
                actor.CrossfadeToPose(actionPose, duration, speed, skip),
                AnimateActorOffset(
                    actor,
                    offset,
                    duration,
                    ExactMotionScale076(exactRecipe076, reducedMotion),
                    speed,
                    skip),
                family == BattleBeatFamily.Positioning || celebration
                    ? AnimateEffect(
                        effectPath,
                        actor,
                        duration,
                        210f,
                        tint,
                        speed,
                        skip,
                        reducedMotion,
                        exactRecipe076)
                    : Empty(),
                AnimateExactTrace076(
                    exactRecipe076,
                    actor,
                    null,
                    duration,
                    tint,
                    reducedMotion,
                    speed,
                    skip));
            if (!ShouldSkip(skip))
            {
                contact?.Invoke();
                if (exactRecipe076 != null) ObserveCurrentExactImpact076();
            }
            var nonContactRecovery076 = exactRecipe076 == null
                ? timing076.RecoverySeconds
                : timing076.ImpactEffectSeconds + timing076.HitStopSeconds +
                  timing076.RecoverySeconds;
            yield return RunTogether(
                actor.CrossfadeToPose(
                    family == BattleBeatFamily.Recovery
                        ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.Recovery,
                    nonContactRecovery076,
                    speed,
                    skip),
                AnimateActorHome(actor, nonContactRecovery076, speed, skip),
                AnimateStageCameraHome076(nonContactRecovery076, speed, skip));
            ReturnToOverview();
            if (exactRecipe076 != null && !ShouldSkip(skip))
                CompleteCurrentExactBeat076(actor, null);
            else if (exactRecipe076 != null)
                AbandonCurrentExactBeat076();
        }

        public void ReturnToOverview()
        {
            ResetStageCamera076();
            foreach (var actor in _actors.Values)
            {
                var downed = IsPresentedDown(actor);
                actor.ReturnHome();
                actor.SetEmphasis(downed ? 0.58f : 1f, 1f);
                actor.SetPoseImmediate(downed
                    ? BattleArtPoseDirector011.Downed
                    : BattleArtPoseDirector011.Idle);
            }
        }

        private void ResetStageCamera076()
        {
            if (_stage == null) return;
            _stage.anchoredPosition = Vector2.zero;
            _stage.localScale = Vector3.one;
            _stage.localRotation = Quaternion.identity;
        }

        public void Dispose()
        {
            RetireAllActors();
            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
            _stage = null;
            _effectLayer = null;
            _backdrop = null;
            _allyFocusPlate = null;
            _enemyFocusPlate = null;
            _unionFocusHeader = null;
            _tacticalRibbon = null;
            _focusedUnionHpRibbon076 = null;
            _focusedAllyHpRail076 = null;
            _focusedAllyHpFill076 = null;
            _focusedEnemyHpRail076 = null;
            _focusedEnemyHpFill076 = null;
            _breakthroughCallout074 = null;
            _hpImpactCallout076 = null;
            _allyUnionLabel = null;
            _enemyUnionLabel = null;
            _allyUnionStatus076 = null;
            _enemyIntent076 = null;
            _focusedAllyHpLabel076 = null;
            _focusedEnemyHpLabel076 = null;
            _breakthroughHeading074 = null;
            _breakthroughDetail074 = null;
            _hpImpactHeading076 = null;
            _hpImpactDetail076 = null;
            _hpImpactVisibleUntil076 = 0f;
            _hpImpactPresentationCount076 = 0;
            _focusedAllyCurrentHp076 = 0;
            _focusedAllyMaximumHp076 = 0;
            _focusedEnemyCurrentHp076 = 0;
            _focusedEnemyMaximumHp076 = 0;
            _lastHpImpactUnionId076 = string.Empty;
            _lastHpImpactMemberId076 = string.Empty;
            _lastHpImpactBefore076 = 0;
            _lastHpImpactAfter076 = 0;
            _lastHpImpactMaximum076 = 0;
            _lastHpImpactDelta076 = 0;
            _lastHpImpactEnemy076 = false;
            _breakthroughNotifications076.Clear();
            _seenBreakthroughNotifications076.Clear();
            _breakthroughVisibleUntil076 = 0f;
            _breakthroughNotificationPresentationCount076 = 0;
            _activeBreakthroughSignature076 = string.Empty;
            _battle = null;
            _backdropBattleId = string.Empty;
            _activeBackdropResourceKey083 = string.Empty;
            _roundOutcome = string.Empty;
            _stagedDownedActors.Clear();
            _presentedHpByActor076.Clear();
            _focusedPlayerUnionId = string.Empty;
            _targetEnemyUnionId = string.Empty;
            ClearSameSideSupportFocus086();
            _tacticalOverlayVisible = true;
        }

        private void OnDestroy()
        {
            if (_root != null) Dispose();
            else
            {
                _actors.Clear();
                _presentedHpByActor076.Clear();
            }
        }

        private void RefreshBackdrop(string battleId)
        {
            var normalized = battleId ?? string.Empty;
            if (StringComparer.Ordinal.Equals(normalized, _backdropBattleId) && _backdrop.sprite != null) return;
            _backdropBattleId = normalized;
            if (M1VisualAssets.TryResolveBattleBackdrop(
                    normalized,
                    out var sprite,
                    out var resourceKey083) &&
                sprite != null)
            {
                _backdrop.sprite = sprite;
                _backdrop.color = Color.white;
                _activeBackdropResourceKey083 = resourceKey083 ?? string.Empty;
            }
            else
            {
                _backdrop.sprite = null;
                _backdrop.color = new Color(0.025f, 0.04f, 0.07f, 1f);
                _activeBackdropResourceKey083 = string.Empty;
            }
        }

        private void SyncVisibleActors(M2BattleUnionView player, M2BattleUnionView enemy)
        {
            SyncVisibleActors(player, false, enemy, true);
        }

        private void SyncVisibleActors(
            M2BattleUnionView left,
            bool leftEnemy,
            M2BattleUnionView right,
            bool rightEnemy)
        {
            var visible = new HashSet<string>(StringComparer.Ordinal);
            AddOrRefreshUnionActors(left, leftEnemy, visible);
            AddOrRefreshUnionActors(right, rightEnemy, visible);

            var retire = new List<string>();
            foreach (var key in _actors.Keys)
                if (!visible.Contains(key)) retire.Add(key);
            foreach (var key in retire)
            {
                _actors[key].Dispose();
                _actors.Remove(key);
                _retiredActors++;
            }
            if (_effectLayer != null) _effectLayer.SetAsLastSibling();
        }

        private void AddOrRefreshUnionActors(
            M2BattleUnionView union,
            bool enemy,
            ISet<string> visible)
        {
            if (union?.Members == null) return;
            for (var index = 0; index < union.Members.Count; index++)
            {
                var member = union.Members[index];
                if (member == null) continue;
                var key = ActorKey(enemy, union.UnionId, member.MemberId);
                visible.Add(key);
                if (!_presentedHpByActor076.TryGetValue(key, out var presentedHp076))
                {
                    presentedHp076 = member.CurrentHp;
                    _presentedHpByActor076[key] = presentedHp076;
                }
                if (_actors.TryGetValue(key, out var actor))
                {
                    actor.Refresh(member);
                    actor.PresentHp076(presentedHp076, member.MaximumHp);
                    continue;
                }
                actor = new M2BattleActorRig072(_stage, union, member, enemy);
                actor.PresentHp076(presentedHp076, member.MaximumHp);
                _actors[key] = actor;
                _spawnedActors++;
            }
        }

        private void SeedPresentedHp076(IReadOnlyList<M2BattleUnionView> unions, bool enemy)
        {
            if (unions == null) return;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                if (union?.Members == null) continue;
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    if (member == null) continue;
                    _presentedHpByActor076[ActorKey(enemy, union.UnionId, member.MemberId)] = member.CurrentHp;
                }
            }
        }

        private static M2BattleMemberView FindMember076(
            IReadOnlyList<M2BattleUnionView> unions,
            string unionId,
            string memberId)
        {
            var union = FindUnion(unions, unionId);
            if (union?.Members == null) return null;
            for (var index = 0; index < union.Members.Count; index++)
                if (union.Members[index] != null && StringComparer.Ordinal.Equals(
                        union.Members[index].MemberId,
                        memberId))
                    return union.Members[index];
            return null;
        }

        private void LayoutUnion(M2BattleUnionView union, bool enemyIdentity, bool rightLane)
        {
            if (union?.Members == null || union.Members.Count == 0) return;
            var count = union.Members.Count;
            var scale = count <= 1 ? 1.02f : count == 2 ? 0.90f : count == 3 ? 0.79f : 0.68f;
            var minimum = rightLane ? 0.60f : 0.13f;
            var maximum = rightLane ? 0.87f : 0.40f;
            // A full allied Union needs the available friendly half of the stage.
            // Keeping six bodies inside the trio's narrow lane made their art tiny.
            // Enemy spacing and every combat/targeting authority remain unchanged.
            if (!enemyIdentity && count > 3)
            {
                minimum = rightLane ? 0.52f : 0.055f;
                maximum = rightLane ? 0.945f : 0.48f;
            }
            var hasBoss = enemyIdentity && UnionContainsGateEater076(union);
            var nonBossCount = hasBoss ? Math.Max(0, count - 1) : count;
            var nonBossOrdinal = 0;
            var alliedHeight091 = 0f;
            var alliedWidth091 = 288f;
            if (!enemyIdentity)
            {
                _root.ForceUpdateRectTransforms();
                _stage.ForceUpdateRectTransforms();
                // Size relative to the production canvas, not an unscaled pixel
                // constant. The 2796x1290 CanvasScaler made the old 288-unit cap
                // much smaller in the actual Windows player than isolated tests.
                alliedWidth091 = Mathf.Max(288f, _stage.rect.width * 0.28f / scale);
                if (count > 1 && _stage.rect.width > 1f)
                    alliedWidth091 = _stage.rect.width * (maximum - minimum) / (count - 1) * 0.90f / scale;
                var widestStandingFrame091 = 0.01f;
                foreach (var member091 in union.Members)
                    if (member091 != null && _actors.TryGetValue(
                            ActorKey(false, union.UnionId, member091.MemberId), out var actor091))
                        widestStandingFrame091 = Mathf.Max(widestStandingFrame091,
                            actor091.AlliedStandingFrameAspect091);
                var heightFraction093 = count == 1 ? 0.31f : count == 2 ? 0.27f : count == 3 ? 0.245f : 0.19f;
                alliedHeight091 = Mathf.Min(Mathf.Max(560f * 0.67f, _root.rect.height * heightFraction093 / scale),
                    alliedWidth091 / widestStandingFrame091);
                // The tactical HP strip is above the stage but overlaps its rect.
                // Reserve real headroom for the highest existing depth row, including
                // a one-member Tower union, without changing root/count/depth scale.
                if (_root.rect.height > 1f)
                {
                    var headerBottom091 = (FocusedUnionHpRibbonMinY076 - BattlefieldStageMinY078) * _root.rect.height;
                    var highestFoot091 = (ActorCommandBaselineY078 + (count > 1 ? 0.035f : 0f)) *
                        _stage.rect.height + 560f * 0.07f * scale;
                    var headroomUnits093 = 12f / Mathf.Max(0.001f, Mathf.Abs(_root.lossyScale.y));
                    alliedHeight091 = Mathf.Min(alliedHeight091,
                        Mathf.Max(1f, (headerBottom091 - highestFoot091 - headroomUnits093) / scale));
                }
            }
            for (var index = 0; index < count; index++)
            {
                var member = union.Members[index];
                if (member == null) continue;
                var key = ActorKey(enemyIdentity, union.UnionId, member.MemberId);
                if (!_actors.TryGetValue(key, out var actor)) continue;
                var boss = actor.BossEnemy075;
                var t = count == 1 ? 0.5f : index / (float)(count - 1);
                var x = Mathf.Lerp(minimum, maximum, t);
                var actorScale = scale;
                var actorSize = new Vector2(300f, 560f);
                if (hasBoss)
                {
                    if (boss)
                    {
                        x = 0.665f;
                        actorScale = scale * BossPresentationScaleMultiplier076;
                        actorSize = new Vector2(320f, 475f);
                    }
                    else
                    {
                        var minionT = nonBossCount <= 1
                            ? 0.5f
                            : nonBossOrdinal / (float)(nonBossCount - 1);
                        x = Mathf.Lerp(0.835f, 0.925f, minionT);
                        actorScale = Mathf.Min(scale, 0.62f);
                        actorSize = new Vector2(260f, 500f);
                        nonBossOrdinal++;
                    }
                }
                else if (actor.ProminentEnemyPresentation086)
                {
                    // Battle086 leaders use a visual tier that is intentionally
                    // independent of BossEnemy075. This gives the Gateheart Warden
                    // and Captain Ravel a readable battlefield silhouette without
                    // opting either actor into Gate-Eater encounter behavior.
                    actorScale *= actor.OriginalEnemyPresentationScale086;
                    actorSize = actor.MajorEnemyPresentation086
                        ? new Vector2(320f, 540f)
                        : new Vector2(310f, 550f);
                }
                if (enemyIdentity)
                {
                    var familyScale090 = actor.EnemyArtFamilyScale090;
                    // Existing encounter boss authority remains primary. Cosmetic
                    // family scale only contributes a bounded secondary adjustment.
                    if (boss) familyScale090 = Mathf.Clamp(familyScale090, 0.94f, 1.08f);
                    actorScale *= familyScale090;
                }
                // Ground every actor above the shipping command tray, including the
                // taller Gate-Eater rig. The previous .18/.225 baselines still assumed
                // the retired .21 tray and left the exact-HP half of even lower-thirds
                // behind today's .25 command dock at both supported resolutions.
                var y = ActorCommandBaselineY078 + (index % 2 == 0 ? 0f : 0.035f);
                actor.SetLayout(
                    new Vector2(x, y),
                    actorSize,
                    actorScale,
                    2 + index + (rightLane ? 10 : 0));
                if (!enemyIdentity)
                    actor.ConfigureAlliedSilhouetteFit091(alliedHeight091, alliedWidth091,
                        _stage.rect.width * Mathf.Min(x, 1f - x) * 1.85f / actorScale);
            }
        }

        private static bool UnionContainsGateEater076(M2BattleUnionView union)
        {
            var members = union?.Members;
            if (members == null) return false;
            for (var index = 0; index < members.Count; index++)
                if (members[index] != null &&
                    M2BattleActorRig072.IsGateEaterBossMember076(members[index].MemberId))
                    return true;
            return false;
        }

        private void FocusForBeat(BattlePresentationBeat beat)
        {
            if (beat != null &&
                !StringComparer.Ordinal.Equals(beat.ActorUnionId, beat.TargetUnionId))
            {
                if (IsPlayerUnion(beat.ActorUnionId) && IsPlayerUnion(beat.TargetUnionId))
                {
                    FocusSameSideSupport086(beat.ActorUnionId, beat.TargetUnionId, false);
                    return;
                }
                if (IsEnemyUnion(beat.ActorUnionId) && IsEnemyUnion(beat.TargetUnionId))
                {
                    FocusSameSideSupport086(beat.ActorUnionId, beat.TargetUnionId, true);
                    return;
                }
            }

            var playerId = IsPlayerUnion(beat.ActorUnionId)
                ? beat.ActorUnionId
                : IsPlayerUnion(beat.TargetUnionId) ? beat.TargetUnionId : _focusedPlayerUnionId;
            var enemyId = IsEnemyUnion(beat.ActorUnionId)
                ? beat.ActorUnionId
                : IsEnemyUnion(beat.TargetUnionId) ? beat.TargetUnionId : _targetEnemyUnionId;
            Focus(playerId, enemyId);
        }

        private void FocusSameSideSupport086(string sourceUnionId, string targetUnionId, bool enemy)
        {
            var side = enemy ? _battle?.EnemyUnions : _battle?.PlayerUnions;
            var source = FindUnion(side, sourceUnionId);
            var target = FindUnion(side, targetUnionId);
            if (source == null || target == null || StringComparer.Ordinal.Equals(source.UnionId, target.UnionId))
            {
                Focus(
                    enemy ? _focusedPlayerUnionId : sourceUnionId,
                    enemy ? sourceUnionId : _targetEnemyUnionId);
                return;
            }

            if (!StringComparer.Ordinal.Equals(source.UnionId, _sameSideSupportSourceUnionId086) ||
                !StringComparer.Ordinal.Equals(target.UnionId, _sameSideSupportTargetUnionId086) ||
                enemy != _sameSideSupportEnemy086)
                _focusTransitions++;

            _sameSideSupportSourceUnionId086 = source.UnionId ?? string.Empty;
            _sameSideSupportTargetUnionId086 = target.UnionId ?? string.Empty;
            _sameSideSupportEnemy086 = enemy;
            SyncVisibleActors(source, enemy, target, enemy);
            LayoutUnion(source, enemy, false);
            LayoutUnion(target, enemy, true);
            ApplyFocusSideColors086(enemy, enemy);
            if (_effectLayer != null) _effectLayer.SetAsLastSibling();
            SetUnionHeader(
                _allyUnionLabel,
                source,
                enemy ? "Enemy Support Source" : "Guild Support Source",
                enemy);
            SetUnionHeader(
                _enemyUnionLabel,
                target,
                enemy ? "Enemy Rescue Target" : "Allied Rescue Target",
                enemy);
            SetSameSideSupportReadout086(source, target, enemy);
            ReturnToOverview();
        }

        private IEnumerator PlayResult(bool reducedMotion, Func<float> speed, Func<bool> skip)
        {
            var playerVictory = (_roundOutcome ?? string.Empty).IndexOf(
                "VICTOR", StringComparison.OrdinalIgnoreCase) >= 0;
            var routines = new List<IEnumerator>();
            foreach (var actor in _actors.Values)
            {
                if (IsPresentedDown(actor)) continue;
                if ((playerVictory && !actor.Enemy) || (!playerVictory && actor.Enemy))
                    routines.Add(actor.CrossfadeToPose(
                        BattleArtPoseDirector011.Victory,
                        reducedMotion ? 0.05f : 0.22f,
                        speed,
                        skip));
                else
                    routines.Add(actor.CrossfadeToPose(
                        BattleArtPoseDirector011.Downed,
                        reducedMotion ? 0.05f : 0.22f,
                        speed,
                        skip));
            }
            yield return RunTogether(routines.ToArray());
            yield return WaitScaled(reducedMotion ? 0.10f : 0.65f, speed, skip);
        }

        private bool IsPresentedDown(M2BattleActorRig072 actor)
        {
            if (actor == null) return false;
            return actor.Downed || _stagedDownedActors.Contains(ActorKey(
                actor.Enemy,
                actor.UnionId,
                actor.MemberId));
        }

        private void Emphasize(M2BattleActorRig072 actor, M2BattleActorRig072 target)
        {
            foreach (var value in _actors.Values)
                value.SetEmphasis(value == actor || value == target ? 1f : 0.40f,
                    value == actor ? 1.08f : value == target ? 1.03f : 0.96f);
        }

        private IEnumerator AnimateActorOffset(
            M2BattleActorRig072 actor,
            Vector2 offset,
            float duration,
            float scaleMultiplier,
            Func<float> speed,
            Func<bool> skip)
        {
            if (actor?.Root == null) yield break;
            var start = actor.Root.anchoredPosition;
            var end = actor.HomePosition + offset;
            var startScale = actor.Root.localScale;
            var endScale = actor.HomeScale * scaleMultiplier;
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                actor.Root.anchoredPosition = Vector2.Lerp(start, end, t);
                actor.Root.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            actor.Root.anchoredPosition = end;
            actor.Root.localScale = endScale;
        }

        private IEnumerator AnimateActorHome(
            M2BattleActorRig072 actor,
            float duration,
            Func<float> speed,
            Func<bool> skip)
        {
            if (actor?.Root == null) yield break;
            var start = actor.Root.anchoredPosition;
            var startScale = actor.Root.localScale;
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                actor.Root.anchoredPosition = Vector2.Lerp(start, actor.HomePosition, t);
                actor.Root.localScale = Vector3.Lerp(startScale, actor.HomeScale, t);
                yield return null;
            }
            actor.ReturnHome();
        }

        private IEnumerator AnimateReaction(
            M2BattleActorRig072 target,
            bool harmful,
            float duration,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip)
        {
            if (target?.Root == null) yield break;
            var origin = target.Root.anchoredPosition;
            var direction = target.Enemy ? Vector2.right : Vector2.left;
            var maximumDisplacement076 = harmful
                ? reducedMotion ? 9f : 34f
                : reducedMotion ? 2f : 8f;
            var maximumScalePulse076 = harmful
                ? reducedMotion ? -0.015f : -0.06f
                : reducedMotion ? 0.012f : 0.04f;
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var t = Mathf.Clamp01(elapsed / duration);
                var pulse = Mathf.Sin(t * Mathf.PI);
                target.Root.anchoredPosition = origin + direction * pulse * maximumDisplacement076;
                target.Root.localScale = target.HomeScale * (1f + pulse * maximumScalePulse076);
                yield return null;
            }
            target.Root.anchoredPosition = origin;
            target.Root.localScale = target.HomeScale;
        }

        private IEnumerator AnimateEffect(
            string resourcePath,
            M2BattleActorRig072 anchor,
            float duration,
            float size,
            Color tint,
            Func<float> speed,
            Func<bool> skip,
            bool reducedMotion,
            Battle3DArtChoreography071 exactRecipe076 = null)
        {
            if (anchor?.Root == null || string.IsNullOrWhiteSpace(resourcePath) ||
                IsRuntimeSilhouettePath(resourcePath))
                yield break;
            var sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
            if (sprite == null) yield break;

            var effect = CreateImage(
                _effectLayer, "Authored Battle Effect 072",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Color.clear);
            effect.sprite = sprite;
            effect.preserveAspect = true;
            effect.raycastTarget = false;
            effect.rectTransform.sizeDelta = new Vector2(size, size);
            _authoredVfx++;
            if (exactRecipe076 != null)
            {
                _exactRecipeVfxSpawnCount076++;
                _currentExactVfxSpawned076 = true;
            }

            var vfxScale076 = ExactVfxScale076(exactRecipe076, reducedMotion);
            var rotationScale076 = reducedMotion ? 0.22f : 1f;
            var rotationStart076 = ExactVfxRotationStart076(exactRecipe076) * rotationScale076;
            var rotationEnd076 = ExactVfxRotationEnd076(exactRecipe076) * rotationScale076;
            var recipeTint076 = ExactVfxTint076(exactRecipe076, tint);

            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var t = Mathf.Clamp01(elapsed / duration);
                effect.rectTransform.position = ActorEffectPoint(anchor);
                var pulse076 = ExactVfxPulse076(exactRecipe076, t, reducedMotion);
                var baseScale076 = reducedMotion
                    ? Mathf.Lerp(0.92f, 1.04f, t)
                    : Mathf.Lerp(0.68f, 1.18f, t);
                effect.rectTransform.localScale = Vector3.one *
                                                  (baseScale076 *
                                                   vfxScale076 * pulse076);
                effect.rectTransform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(rotationStart076, rotationEnd076, t));
                var color = recipeTint076;
                color.a *= Mathf.Sin(t * Mathf.PI) * ExactVfxAlpha076(exactRecipe076, t);
                effect.color = color;
                yield return null;
            }
            if (effect != null) UnityEngine.Object.Destroy(effect.gameObject);
        }

        private IEnumerator AnimateProjectile(
            string resourcePath,
            M2BattleActorRig072 actor,
            M2BattleActorRig072 target,
            float duration,
            float size,
            Color tint,
            Func<float> speed,
            Func<bool> skip,
            bool reducedMotion,
            Battle3DArtChoreography071 exactRecipe076 = null)
        {
            if (actor?.Root == null || target?.Root == null || string.IsNullOrWhiteSpace(resourcePath) ||
                IsRuntimeSilhouettePath(resourcePath))
                yield break;
            var sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
            if (sprite == null) yield break;

            var effect = CreateImage(
                _effectLayer, "Authored Battle Projectile 072",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), tint);
            effect.sprite = sprite;
            effect.preserveAspect = true;
            effect.raycastTarget = false;
            effect.rectTransform.sizeDelta = new Vector2(size, size);
            _authoredVfx++;
            if (exactRecipe076 != null)
            {
                _exactRecipeVfxSpawnCount076++;
                _currentExactVfxSpawned076 = true;
            }
            var from = ActorEffectPoint(actor);
            var recipeTint076 = ExactVfxTint076(exactRecipe076, tint);
            effect.color = recipeTint076;
            var arcHeight076 = exactRecipe076 == null
                ? 0f
                : (24f + exactRecipe076.SemanticLiftBias * 150f) *
                  (reducedMotion ? 0.20f : 1f);
            var projectileRotationScale076 = reducedMotion ? 0.18f : 1f;
            var projectilePulse076 = reducedMotion ? 0.05f : 0.24f;
            // Keep an observable source and landing frame. Without these anchors a
            // low-frame-rate player (and an independent UI observer) can miss the
            // complete support transfer when the travel loop advances and destroys
            // the Image in the same rendered frame.
            effect.rectTransform.position = from;
            if (!ShouldSkip(skip)) yield return null;
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                var point076 = Vector3.Lerp(from, ActorEffectPoint(target), t);
                point076.y += Mathf.Sin(t * Mathf.PI) * arcHeight076;
                effect.rectTransform.position = point076;
                effect.rectTransform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(
                        ExactVfxRotationStart076(exactRecipe076),
                        ExactVfxRotationEnd076(exactRecipe076) + 160f,
                        t) * projectileRotationScale076);
                effect.rectTransform.localScale = Vector3.one *
                                                  ((0.94f + Mathf.Sin(t * Mathf.PI) * projectilePulse076) *
                                                   ExactVfxScale076(exactRecipe076, reducedMotion));
                yield return null;
            }
            if (effect != null && !ShouldSkip(skip))
            {
                effect.rectTransform.position = ActorEffectPoint(target);
                // Hold the exact recipient contact point across two rendered frames.
                // A single coroutine yield could be consumed before an observer's
                // update on very fast/batch frames, making a correct cross-Union
                // restoration look as if it vanished short of the wounded Union.
                yield return null;
                if (!ShouldSkip(skip)) yield return null;
            }
            if (effect != null) UnityEngine.Object.Destroy(effect.gameObject);
        }

        private IEnumerator AnimateStageCameraIntent076(
            Battle3DArtChoreography071 recipe076,
            float duration,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip)
        {
            if (recipe076 == null || _stage == null || ShouldSkip(skip)) yield break;
            ResolveStageCameraTarget076(
                recipe076,
                reducedMotion,
                out var targetPosition076,
                out var targetScale076,
                out var targetRotation076);
            if (_currentExactBeatActive076 && !_currentExactCameraConsumed076)
            {
                _currentExactCameraConsumed076 = true;
                _exactRecipeCameraIntentCount076++;
            }

            var startPosition076 = _stage.anchoredPosition;
            var startScale076 = _stage.localScale;
            var startRotation076 = _stage.localRotation;
            var elapsed076 = 0f;
            while (elapsed076 < duration && !ShouldSkip(skip))
            {
                elapsed076 += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var amount076 = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed076 / Mathf.Max(0.001f, duration)));
                _stage.anchoredPosition = Vector2.Lerp(
                    startPosition076,
                    targetPosition076,
                    amount076);
                _stage.localScale = Vector3.Lerp(startScale076, targetScale076, amount076);
                _stage.localRotation = Quaternion.Slerp(
                    startRotation076,
                    targetRotation076,
                    amount076);
                yield return null;
            }
            _stage.anchoredPosition = targetPosition076;
            _stage.localScale = targetScale076;
            _stage.localRotation = targetRotation076;
        }

        private IEnumerator AnimateStageCameraHome076(
            float duration,
            Func<float> speed,
            Func<bool> skip)
        {
            if (_stage == null) yield break;
            var startPosition076 = _stage.anchoredPosition;
            var startScale076 = _stage.localScale;
            var startRotation076 = _stage.localRotation;
            var elapsed076 = 0f;
            while (elapsed076 < duration && !ShouldSkip(skip))
            {
                elapsed076 += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var amount076 = Mathf.SmoothStep(
                    0f,
                    1f,
                    Mathf.Clamp01(elapsed076 / Mathf.Max(0.001f, duration)));
                _stage.anchoredPosition = Vector2.Lerp(startPosition076, Vector2.zero, amount076);
                _stage.localScale = Vector3.Lerp(startScale076, Vector3.one, amount076);
                _stage.localRotation = Quaternion.Slerp(
                    startRotation076,
                    Quaternion.identity,
                    amount076);
                yield return null;
            }
            ResetStageCamera076();
        }

        private static void ResolveStageCameraTarget076(
            Battle3DArtChoreography071 recipe076,
            bool reducedMotion,
            out Vector2 position076,
            out Vector3 scale076,
            out Quaternion rotation076)
        {
            position076 = Vector2.zero;
            var scaleAmount076 = 0f;
            var rotationAmount076 = 0f;
            if (recipe076 != null)
            {
                var lateral076 = recipe076.CameraLateralOffset;
                var rotationSign076 = (recipe076.CameraVisualSeed & 1u) == 0u ? -1f : 1f;
                switch (recipe076.CameraRecipe)
                {
                    case Battle3DArtCameraRecipe071.MediumThreeQuarterTrackIn:
                        position076 = new Vector2(lateral076 * 32f, 7f);
                        scaleAmount076 = 0.035f;
                        rotationAmount076 = rotationSign076 * 0.8f;
                        break;
                    case Battle3DArtCameraRecipe071.CloseProfileLateralTrack:
                        position076 = new Vector2(lateral076 * 58f, 4f);
                        scaleAmount076 = 0.065f;
                        rotationAmount076 = rotationSign076 * 1.15f;
                        break;
                    case Battle3DArtCameraRecipe071.WideTacticalOrbit:
                        position076 = new Vector2(lateral076 * 26f, -6f);
                        scaleAmount076 = -0.045f;
                        rotationAmount076 = rotationSign076 * 1.65f;
                        break;
                    case Battle3DArtCameraRecipe071.OverShoulderReactionSnap:
                        position076 = new Vector2(lateral076 * 46f, 9f);
                        scaleAmount076 = 0.055f;
                        rotationAmount076 = rotationSign076 * 2.1f;
                        break;
                }
            }
            if (reducedMotion)
            {
                position076 *= 0.18f;
                scaleAmount076 *= 0.18f;
                rotationAmount076 *= 0.12f;
            }
            scale076 = Vector3.one * (1f + scaleAmount076);
            scale076.z = 1f;
            rotation076 = Quaternion.Euler(0f, 0f, rotationAmount076);
        }

        private IEnumerator AnimateExactTrace076(
            Battle3DArtChoreography071 recipe076,
            M2BattleActorRig072 actor,
            M2BattleActorRig072 target,
            float duration,
            Color tint,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip)
        {
            if (recipe076 == null || actor?.Root == null || _effectLayer == null ||
                recipe076.TraceRecipe == Battle3DArtTraceRecipe071.None || ShouldSkip(skip))
                yield break;

            var segments076 = new List<Image>();
            var pointCount076 = Mathf.Clamp(recipe076.SemanticTracePointCount, 6, 12);
            var layerCount076 = Mathf.Clamp(recipe076.SemanticTraceLayerCount, 1, 2);
            var start076 = (Vector2)ActorEffectPoint(actor);
            var end076 = target?.Root == null
                ? start076 + new Vector2(actor.Enemy ? -250f : 250f, 0f)
                : (Vector2)ActorEffectPoint(target);
            for (var layer076 = 0; layer076 < layerCount076; layer076++)
            {
                var points076 = new Vector2[pointCount076];
                for (var pointIndex076 = 0; pointIndex076 < pointCount076; pointIndex076++)
                {
                    var progress076 = pointCount076 <= 1
                        ? 0f
                        : pointIndex076 / (float)(pointCount076 - 1);
                    points076[pointIndex076] = ExactTracePoint076(
                        recipe076,
                        start076,
                        end076,
                        progress076,
                        layer076,
                        reducedMotion);
                }

                for (var pointIndex076 = 0; pointIndex076 < pointCount076 - 1; pointIndex076++)
                {
                    var from076 = points076[pointIndex076];
                    var to076 = points076[pointIndex076 + 1];
                    var delta076 = to076 - from076;
                    var segment076 = CreateImage(
                        _effectLayer,
                        "Exact Art Trace Segment 076 · " + recipe076.TraceRecipe + " · " +
                        layer076 + " · " + pointIndex076,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(0.5f, 0.5f),
                        Color.clear);
                    segment076.raycastTarget = false;
                    segment076.rectTransform.position = (from076 + to076) * 0.5f;
                    segment076.rectTransform.sizeDelta = new Vector2(
                        Mathf.Max(3f, delta076.magnitude + 2f),
                        reducedMotion ? 2.5f : 4.5f + layer076);
                    segment076.rectTransform.localRotation = Quaternion.Euler(
                        0f,
                        0f,
                        Mathf.Atan2(delta076.y, delta076.x) * Mathf.Rad2Deg);
                    segments076.Add(segment076);
                }
            }

            if (segments076.Count <= 0) yield break;
            _currentExactTraceGeometryCount076 += segments076.Count;
            _exactRecipeTraceGeometryCount076 += segments076.Count;
            _currentExactTraceConsumed076 = true;
            _currentExactVfxSpawned076 = true;
            _exactRecipeVfxSpawnCount076++;
            var traceTint076 = ExactVfxTint076(recipe076, tint);
            var elapsed076 = 0f;
            while (elapsed076 < duration && !ShouldSkip(skip))
            {
                elapsed076 += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var progress076 = Mathf.Clamp01(elapsed076 / Mathf.Max(0.001f, duration));
                var alpha076 = Mathf.Sin(progress076 * Mathf.PI) *
                               (reducedMotion ? 0.62f : 0.88f);
                var pulse076 = reducedMotion
                    ? 1f + Mathf.Sin(progress076 * Mathf.PI) * 0.025f
                    : 1f + Mathf.Sin(progress076 * Mathf.PI * 2f) * 0.10f;
                for (var index076 = 0; index076 < segments076.Count; index076++)
                {
                    if (segments076[index076] == null) continue;
                    var color076 = traceTint076;
                    color076.a *= alpha076;
                    segments076[index076].color = color076;
                    segments076[index076].rectTransform.localScale = Vector3.one * pulse076;
                }
                yield return null;
            }
            for (var index076 = 0; index076 < segments076.Count; index076++)
                if (segments076[index076] != null)
                    UnityEngine.Object.Destroy(segments076[index076].gameObject);
        }

        private static Vector2 ExactTracePoint076(
            Battle3DArtChoreography071 recipe076,
            Vector2 start076,
            Vector2 end076,
            float progress076,
            int layer076,
            bool reducedMotion)
        {
            var forward076 = end076 - start076;
            if (forward076.sqrMagnitude < 0.01f) forward076 = Vector2.right * 250f;
            var direction076 = forward076.normalized;
            var perpendicular076 = new Vector2(-direction076.y, direction076.x);
            var radius076 = (48f + recipe076.SemanticTraceRadius * 48f + layer076 * 14f) *
                            (reducedMotion ? 0.72f : 1f);
            var rotation076 = recipe076.SemanticTraceRotationDegrees * Mathf.Deg2Rad;
            switch (recipe076.TraceRecipe)
            {
                case Battle3DArtTraceRecipe071.EdgeArc:
                case Battle3DArtTraceRecipe071.TwinTrail:
                {
                    var angle076 = Mathf.Lerp(-1.85f, 0.95f, progress076) + rotation076;
                    return start076 + new Vector2(Mathf.Cos(angle076), Mathf.Sin(angle076)) * radius076 +
                           perpendicular076 * layer076 * 10f;
                }
                case Battle3DArtTraceRecipe071.WeightedShock:
                case Battle3DArtTraceRecipe071.ImpactRing:
                {
                    var angle076 = progress076 * Mathf.PI * 2f + rotation076;
                    return end076 + new Vector2(Mathf.Cos(angle076), Mathf.Sin(angle076)) * radius076;
                }
                case Battle3DArtTraceRecipe071.GuardPlate:
                case Battle3DArtTraceRecipe071.WardDome:
                {
                    var angle076 = Mathf.Lerp(Mathf.PI, 0f, progress076) + rotation076 * 0.25f;
                    return start076 + new Vector2(Mathf.Cos(angle076), Mathf.Sin(angle076)) * radius076;
                }
                case Battle3DArtTraceRecipe071.ArcaneOrbit:
                case Battle3DArtTraceRecipe071.RelicConvergence:
                case Battle3DArtTraceRecipe071.ElementalSpiral:
                case Battle3DArtTraceRecipe071.HealingBloom:
                {
                    var angle076 = progress076 * Mathf.PI * (2f + layer076) + rotation076;
                    var spiral076 = radius076 * (0.42f + progress076 * 0.58f);
                    return start076 + new Vector2(Mathf.Cos(angle076), Mathf.Sin(angle076)) * spiral076;
                }
                case Battle3DArtTraceRecipe071.ToolDiagram:
                case Battle3DArtTraceRecipe071.TacticalMarker:
                {
                    var angle076 = Mathf.Floor(progress076 * 4f) * Mathf.PI * 0.5f + rotation076;
                    return start076 + new Vector2(Mathf.Cos(angle076), Mathf.Sin(angle076)) * radius076;
                }
                case Battle3DArtTraceRecipe071.TacticalWave:
                    return Vector2.Lerp(start076, end076, progress076) + perpendicular076 *
                           Mathf.Sin(progress076 * Mathf.PI * 3f) * radius076 * 0.38f;
                case Battle3DArtTraceRecipe071.ReachLine:
                case Battle3DArtTraceRecipe071.ProjectileFlight:
                case Battle3DArtTraceRecipe071.TacticalLine:
                default:
                    return Vector2.Lerp(start076, end076, progress076) + perpendicular076 *
                           Mathf.Sin(progress076 * Mathf.PI) * radius076 * 0.18f;
            }
        }

        /// <summary>
        /// Accepts an exact recipe only when every presentation field on the live
        /// 011 profile agrees with the canonical 071 choreography. This prevents a
        /// stale metadata copy from being certified as live consumption.
        /// </summary>
        public static bool TryResolveLiveExactRecipe076(
            BattleArtProfile011 profile,
            string artId,
            out Battle3DArtChoreography071 choreography)
        {
            choreography = null;
            if (profile == null || !profile.exactFirstHourRecipe ||
                string.IsNullOrWhiteSpace(artId) ||
                !StringComparer.Ordinal.Equals(profile.artId, artId) ||
                !Battle3DArtChoreography071.TryCreate(artId, out var resolved076) ||
                resolved076 == null)
                return false;

            if (!StringComparer.Ordinal.Equals(profile.presentationRecipeId, resolved076.ClipRecipeId) ||
                !StringComparer.Ordinal.Equals(profile.presentationTreeId, resolved076.TreeId) ||
                !StringComparer.Ordinal.Equals(profile.presentationMotionSignature, resolved076.MotionSignature) ||
                !StringComparer.Ordinal.Equals(profile.presentationVfxSignature, resolved076.VfxSignature) ||
                !StringComparer.Ordinal.Equals(profile.presentationSfxSignature, resolved076.SfxSignature) ||
                profile.presentationDurationMilliseconds != resolved076.DurationMilliseconds ||
                profile.presentationImpactMilliseconds != resolved076.ImpactMilliseconds ||
                resolved076.CameraRecipe == Battle3DArtCameraRecipe071.None ||
                resolved076.MotionRecipe == Battle3DArtMotionRecipe071.None ||
                resolved076.VfxRecipe == Battle3DArtVfxRecipe071.None ||
                resolved076.TraceRecipe == Battle3DArtTraceRecipe071.None)
                return false;

            choreography = resolved076;
            return true;
        }

        private void BeginExactRecipeBeat076(
            BattleArtProfile011 profile,
            Battle3DArtChoreography071 choreography,
            bool reducedMotion,
            bool hasReaction076,
            bool harmfulReaction076)
        {
            if (profile == null || choreography == null) return;
            AbandonCurrentExactBeat076();
            _exactRecipeBeatCount076++;
            _currentExactProfile076 = profile;
            _currentExactRecipe076 = choreography;
            _currentExactBeatActive076 = true;
            _currentExactStartedRealtime076 = Time.realtimeSinceStartup;
            _currentExactImpactRealtime076 = 0f;
            _currentExactImpactObserved076 = false;
            _currentExactMotionConsumed076 = false;
            _currentExactVfxSpawned076 = false;
            _currentExactCameraConsumed076 = false;
            _currentExactTraceConsumed076 = false;
            _currentExactTraceGeometryCount076 = 0;
            _currentExactReducedMotion076 = reducedMotion;
            _currentExactEnvelope076 = ResolveAppliedMotionEnvelope076(
                choreography,
                profile,
                reducedMotion,
                hasReaction076,
                harmfulReaction076);
        }

        private void ObserveCurrentExactImpact076()
        {
            if (!_currentExactBeatActive076 || _currentExactImpactObserved076) return;
            _currentExactImpactObserved076 = true;
            _currentExactImpactRealtime076 = Time.realtimeSinceStartup;
            _exactRecipeImpactBoundaryCount076++;
        }

        private void CompleteCurrentExactBeat076(
            M2BattleActorRig072 actor,
            M2BattleActorRig072 target)
        {
            if (!_currentExactBeatActive076 || _currentExactProfile076 == null ||
                _currentExactRecipe076 == null || !_currentExactImpactObserved076 ||
                !_currentExactMotionConsumed076 || !_currentExactVfxSpawned076 ||
                !_currentExactCameraConsumed076 || !_currentExactTraceConsumed076 ||
                _currentExactTraceGeometryCount076 <= 0 || !ActorAtHome076(actor) ||
                (target != null && !ActorAtHome076(target)) || !StageCameraAtHome076())
            {
                AbandonCurrentExactBeat076();
                return;
            }

            var completedRealtime076 = Time.realtimeSinceStartup;
            var observedImpact076 = Mathf.Max(
                1,
                Mathf.RoundToInt(
                    (_currentExactImpactRealtime076 - _currentExactStartedRealtime076) * 1000f));
            var observedCompletion076 = Mathf.Max(
                observedImpact076 + 1,
                Mathf.RoundToInt(
                    (completedRealtime076 - _currentExactStartedRealtime076) * 1000f));
            _completedExactBeatCount076++;
            _lastExactArtId076 = _currentExactRecipe076.StableArtId;
            _lastExactRecipeId076 = _currentExactProfile076.presentationRecipeId;
            _lastExactMotionSignature076 = _currentExactProfile076.presentationMotionSignature;
            _lastExactVfxSignature076 = _currentExactProfile076.presentationVfxSignature;
            _lastExactSfxSignature076 = _currentExactProfile076.presentationSfxSignature;
            _lastExactMotionRecipe076 = _currentExactRecipe076.MotionRecipe.ToString();
            _lastExactVfxRecipe076 = _currentExactRecipe076.VfxRecipe.ToString();
            _lastExactCameraRecipe076 = _currentExactRecipe076.CameraRecipe.ToString();
            _lastExactTraceRecipe076 = _currentExactRecipe076.TraceRecipe.ToString();
            _lastExactDurationMilliseconds076 =
                _currentExactProfile076.presentationDurationMilliseconds;
            _lastExactImpactMilliseconds076 =
                _currentExactProfile076.presentationImpactMilliseconds;
            _lastExactObservedImpactMilliseconds076 = observedImpact076;
            _lastExactObservedCompletionMilliseconds076 = observedCompletion076;
            _lastExactTraceGeometryCount076 = _currentExactTraceGeometryCount076;
            _lastExactReducedMotion076 = _currentExactReducedMotion076;
            _lastExactReactionDisplacementPixels076 =
                _currentExactEnvelope076.ReactionDisplacementPixels;
            _lastExactActorScaleDelta076 = _currentExactEnvelope076.ActorScaleDelta;
            _lastExactVfxRotationDegrees076 = _currentExactEnvelope076.VfxRotationDegrees;
            _lastExactVfxPulseDelta076 = _currentExactEnvelope076.VfxPulseDelta;
            _lastExactProjectileArcPixels076 = _currentExactEnvelope076.ProjectileArcPixels;
            _lastExactProjectileRotationDegrees076 =
                _currentExactEnvelope076.ProjectileRotationDegrees;
            _lastExactCameraOffsetPixels076 = _currentExactEnvelope076.CameraOffsetPixels;
            _lastExactCameraScaleDelta076 = _currentExactEnvelope076.CameraScaleDelta;
            _lastExactCameraRotationDegrees076 =
                _currentExactEnvelope076.CameraRotationDegrees;
            AbandonCurrentExactBeat076();
        }

        private void AbandonCurrentExactBeat076()
        {
            _currentExactProfile076 = null;
            _currentExactRecipe076 = null;
            _currentExactBeatActive076 = false;
            _currentExactImpactObserved076 = false;
            _currentExactMotionConsumed076 = false;
            _currentExactVfxSpawned076 = false;
            _currentExactCameraConsumed076 = false;
            _currentExactTraceConsumed076 = false;
            _currentExactTraceGeometryCount076 = 0;
        }

        private static bool ActorAtHome076(M2BattleActorRig072 actor)
        {
            if (actor?.Root == null) return false;
            return Vector2.Distance(actor.Root.anchoredPosition, actor.HomePosition) <= 0.05f &&
                   Vector3.Distance(actor.Root.localScale, actor.HomeScale) <= 0.005f;
        }

        private bool StageCameraAtHome076()
        {
            if (_stage == null) return false;
            return _stage.anchoredPosition.sqrMagnitude <= 0.0025f &&
                   Vector3.Distance(_stage.localScale, Vector3.one) <= 0.005f &&
                   Quaternion.Angle(_stage.localRotation, Quaternion.identity) <= 0.05f;
        }

        private readonly struct LiveArtTiming076
        {
            public LiveArtTiming076(
                float anticipationSeconds,
                float actionSeconds,
                float projectileSeconds,
                float impactEffectSeconds,
                float hitStopSeconds,
                float recoverySeconds)
            {
                AnticipationSeconds = anticipationSeconds;
                ActionSeconds = actionSeconds;
                ProjectileSeconds = projectileSeconds;
                ImpactEffectSeconds = impactEffectSeconds;
                HitStopSeconds = hitStopSeconds;
                RecoverySeconds = recoverySeconds;
            }

            public float AnticipationSeconds { get; }
            public float ActionSeconds { get; }
            public float ProjectileSeconds { get; }
            public float ImpactEffectSeconds { get; }
            public float HitStopSeconds { get; }
            public float RecoverySeconds { get; }
        }

        private readonly struct AppliedMotionEnvelope076
        {
            public AppliedMotionEnvelope076(
                float reactionDisplacementPixels,
                float actorScaleDelta,
                float vfxRotationDegrees,
                float vfxPulseDelta,
                float projectileArcPixels,
                float projectileRotationDegrees,
                float cameraOffsetPixels,
                float cameraScaleDelta,
                float cameraRotationDegrees)
            {
                ReactionDisplacementPixels = reactionDisplacementPixels;
                ActorScaleDelta = actorScaleDelta;
                VfxRotationDegrees = vfxRotationDegrees;
                VfxPulseDelta = vfxPulseDelta;
                ProjectileArcPixels = projectileArcPixels;
                ProjectileRotationDegrees = projectileRotationDegrees;
                CameraOffsetPixels = cameraOffsetPixels;
                CameraScaleDelta = cameraScaleDelta;
                CameraRotationDegrees = cameraRotationDegrees;
            }

            public float ReactionDisplacementPixels { get; }
            public float ActorScaleDelta { get; }
            public float VfxRotationDegrees { get; }
            public float VfxPulseDelta { get; }
            public float ProjectileArcPixels { get; }
            public float ProjectileRotationDegrees { get; }
            public float CameraOffsetPixels { get; }
            public float CameraScaleDelta { get; }
            public float CameraRotationDegrees { get; }
        }

        private static AppliedMotionEnvelope076 ResolveAppliedMotionEnvelope076(
            Battle3DArtChoreography071 recipe076,
            BattleArtProfile011 profile076,
            bool reducedMotion,
            bool hasReaction076,
            bool harmfulReaction076)
        {
            if (recipe076 == null) return default;
            var rotationScale076 = reducedMotion ? 0.22f : 1f;
            var vfxStart076 = ExactVfxRotationStart076(recipe076) * rotationScale076;
            var vfxEnd076 = ExactVfxRotationEnd076(recipe076) * rotationScale076;
            var vfxPulseDelta076 = 0f;
            for (var index076 = 0; index076 <= 32; index076++)
            {
                var pulse076 = ExactVfxPulse076(
                    recipe076,
                    index076 / 32f,
                    reducedMotion);
                vfxPulseDelta076 = Mathf.Max(vfxPulseDelta076, Mathf.Abs(pulse076 - 1f));
            }

            var hasProjectile076 = !string.IsNullOrWhiteSpace(profile076?.projectileResourcePath);
            var projectileArc076 = hasProjectile076
                ? (24f + recipe076.SemanticLiftBias * 150f) * (reducedMotion ? 0.20f : 1f)
                : 0f;
            var projectileRotation076 = hasProjectile076
                ? Mathf.Max(
                      Mathf.Abs(ExactVfxRotationStart076(recipe076)),
                      Mathf.Abs(ExactVfxRotationEnd076(recipe076) + 160f)) *
                  (reducedMotion ? 0.18f : 1f)
                : 0f;
            ResolveStageCameraTarget076(
                recipe076,
                reducedMotion,
                out var cameraPosition076,
                out var cameraScale076,
                out var cameraRotation076);
            var reactionDisplacement076 = !hasReaction076
                ? 0f
                : harmfulReaction076
                    ? reducedMotion ? 9f : 34f
                    : reducedMotion ? 2f : 8f;
            return new AppliedMotionEnvelope076(
                reactionDisplacement076,
                Mathf.Abs(ExactMotionScale076(recipe076, reducedMotion) - 1f),
                Mathf.Max(Mathf.Abs(vfxStart076), Mathf.Abs(vfxEnd076)),
                vfxPulseDelta076,
                projectileArc076,
                projectileRotation076,
                cameraPosition076.magnitude,
                Mathf.Abs(cameraScale076.x - 1f),
                Quaternion.Angle(cameraRotation076, Quaternion.identity));
        }

        private static LiveArtTiming076 ResolveLiveArtTiming076(
            Battle3DArtChoreography071 recipe076,
            BattleArtProfile011 profile,
            bool harmful,
            bool hasProjectile,
            bool reducedMotion)
        {
            if (recipe076 == null)
                return new LiveArtTiming076(
                    reducedMotion ? 0.04f : 0.14f,
                    reducedMotion ? 0.05f : 0.18f,
                    hasProjectile ? reducedMotion ? 0.06f : 0.22f : 0f,
                    reducedMotion ? 0.06f : 0.22f,
                    reducedMotion || !harmful
                        ? 0f
                        : profile == null
                            ? 0.045f
                            : Mathf.Clamp(profile.hitStopMilliseconds / 1000f, 0.025f, 0.14f),
                    reducedMotion ? 0.05f : 0.20f);

            // The exact recipe defines the visible contact point and overall cadence.
            // Reduced motion preserves their ratio while shortening screen travel.
            var motionScale076 = reducedMotion ? 0.34f : 1f;
            var total076 = Mathf.Max(0.32f, recipe076.DurationSeconds) * motionScale076;
            var toImpact076 = Mathf.Clamp(
                recipe076.ImpactSeconds * motionScale076,
                total076 * 0.35f,
                total076 * 0.82f);
            var anticipation076 = toImpact076 * 0.30f;
            var projectile076 = hasProjectile ? toImpact076 * 0.22f : 0f;
            var action076 = Mathf.Max(0.025f, toImpact076 - anticipation076 - projectile076);
            var afterImpact076 = Mathf.Max(0.06f, total076 - toImpact076);
            var authoredHitStop076 = profile == null
                ? 0f
                : Mathf.Max(0f, profile.hitStopMilliseconds / 1000f) * motionScale076;
            var hitStop076 = harmful && !reducedMotion
                ? Mathf.Min(authoredHitStop076, afterImpact076 * 0.22f)
                : 0f;
            var impactEffect076 = afterImpact076 *
                                  (recipe076.VfxRecipe == Battle3DArtVfxRecipe071.ReactionBurst
                                      ? 0.56f
                                      : recipe076.VfxRecipe == Battle3DArtVfxRecipe071.ExpandingPattern
                                          ? 0.52f
                                          : 0.44f);
            var recovery076 = Mathf.Max(0.025f, afterImpact076 - impactEffect076 - hitStop076);
            return new LiveArtTiming076(
                anticipation076,
                action076,
                projectile076,
                impactEffect076,
                hitStop076,
                recovery076);
        }

        private static string ExactMotionPose076(
            Battle3DArtChoreography071 recipe076,
            BattleBeatFamily fallbackFamily)
        {
            if (recipe076 == null) return BattleArtPoseDirector011.ActivePoseFor(fallbackFamily);
            switch (recipe076.MotionRecipe)
            {
                case Battle3DArtMotionRecipe071.SetAdvancePrimaryRecover:
                    return BattleArtPoseDirector011.ActionPrimary;
                case Battle3DArtMotionRecipe071.FeintChainSecondaryRecover:
                    return BattleArtPoseDirector011.ActionPrimary;
                case Battle3DArtMotionRecipe071.CommitExpandFullRecover:
                    return BattleArtPoseDirector011.RolePrimary;
                case Battle3DArtMotionRecipe071.ReadReactCounterRecover:
                    return BattleArtPoseDirector011.RolePrimary;
                default:
                    return BattleArtPoseDirector011.ActivePoseFor(fallbackFamily);
            }
        }

        private static Vector2 ApplyExactMotionOffset076(
            Vector2 baseOffset,
            M2BattleActorRig072 actor,
            Battle3DArtChoreography071 recipe076,
            bool reducedMotion)
        {
            if (recipe076 == null) return baseOffset;
            var direction076 = baseOffset.sqrMagnitude > 0.01f
                ? baseOffset.normalized
                : new Vector2(actor != null && actor.Enemy ? -1f : 1f, 0f);
            var perpendicular076 = new Vector2(-direction076.y, direction076.x);
            var magnitude076 = (reducedMotion ? 18f : 72f) * recipe076.MotionMagnitude;
            var forward076 = direction076 * magnitude076 * (1f + recipe076.SemanticForwardBias);
            var lateral076 = perpendicular076 * magnitude076 * recipe076.SemanticLateralBias;
            var lift076 = Vector2.up * magnitude076 * recipe076.SemanticLiftBias;
            switch (recipe076.MotionRecipe)
            {
                case Battle3DArtMotionRecipe071.SetAdvancePrimaryRecover:
                    return baseOffset + forward076 + lift076;
                case Battle3DArtMotionRecipe071.FeintChainSecondaryRecover:
                    return baseOffset * 0.86f + forward076 * 0.55f + lateral076 * 3.2f + lift076;
                case Battle3DArtMotionRecipe071.CommitExpandFullRecover:
                    return baseOffset * 1.12f + forward076 * 1.25f + lateral076 + lift076 * 1.6f;
                case Battle3DArtMotionRecipe071.ReadReactCounterRecover:
                    return baseOffset * 0.72f - forward076 * 0.38f + lateral076 * 2.1f + lift076;
                default:
                    return baseOffset;
            }
        }

        private static float ExactMotionScale076(
            Battle3DArtChoreography071 recipe076,
            bool reducedMotion)
        {
            float fullScale076;
            if (recipe076 == null) fullScale076 = 1.07f;
            else
            switch (recipe076.MotionRecipe)
            {
                case Battle3DArtMotionRecipe071.CommitExpandFullRecover: fullScale076 = 1.13f; break;
                case Battle3DArtMotionRecipe071.ReadReactCounterRecover: fullScale076 = 1.04f; break;
                case Battle3DArtMotionRecipe071.FeintChainSecondaryRecover: fullScale076 = 1.09f; break;
                default: fullScale076 = 1.07f; break;
            }
            return reducedMotion ? Mathf.Lerp(1f, fullScale076, 0.24f) : fullScale076;
        }

        private static float ExactVfxScale076(
            Battle3DArtChoreography071 recipe076,
            bool reducedMotion)
        {
            float fullScale076;
            if (recipe076 == null) fullScale076 = 1f;
            else
            switch (recipe076.VfxRecipe)
            {
                case Battle3DArtVfxRecipe071.SecondaryAccent: fullScale076 = 0.88f; break;
                case Battle3DArtVfxRecipe071.ExpandingPattern: fullScale076 = 1.42f; break;
                case Battle3DArtVfxRecipe071.ReactionBurst: fullScale076 = 1.18f; break;
                default: fullScale076 = 1f; break;
            }
            return reducedMotion ? Mathf.Lerp(1f, fullScale076, 0.22f) : fullScale076;
        }

        private static float ExactVfxRotationStart076(Battle3DArtChoreography071 recipe076)
        {
            if (recipe076 == null) return -10f;
            var seeded076 = ((recipe076.VfxVisualSeed >> 8) & 0xffu) / 255f * 28f - 14f;
            return recipe076.VfxRecipe == Battle3DArtVfxRecipe071.SecondaryAccent
                ? 24f + seeded076
                : -18f + seeded076;
        }

        private static float ExactVfxRotationEnd076(Battle3DArtChoreography071 recipe076)
        {
            if (recipe076 == null) return 22f;
            switch (recipe076.VfxRecipe)
            {
                case Battle3DArtVfxRecipe071.SecondaryAccent: return -48f;
                case Battle3DArtVfxRecipe071.ExpandingPattern: return 72f;
                case Battle3DArtVfxRecipe071.ReactionBurst: return 118f;
                default: return 34f;
            }
        }

        private static float ExactVfxPulse076(
            Battle3DArtChoreography071 recipe076,
            float progress076,
            bool reducedMotion)
        {
            if (recipe076 == null) return 1f;
            float fullPulse076;
            switch (recipe076.VfxRecipe)
            {
                case Battle3DArtVfxRecipe071.SecondaryAccent:
                    fullPulse076 = 0.92f + Mathf.Sin(progress076 * Mathf.PI * 2f) * 0.08f;
                    break;
                case Battle3DArtVfxRecipe071.ExpandingPattern:
                    fullPulse076 = 0.72f + progress076 * 0.52f;
                    break;
                case Battle3DArtVfxRecipe071.ReactionBurst:
                    fullPulse076 = 0.82f + Mathf.Sin(progress076 * Mathf.PI * 3f) * 0.18f;
                    break;
                default:
                    fullPulse076 = 1f;
                    break;
            }
            return reducedMotion ? Mathf.Lerp(1f, fullPulse076, 0.20f) : fullPulse076;
        }

        private static float ExactVfxAlpha076(
            Battle3DArtChoreography071 recipe076,
            float progress076)
        {
            if (recipe076 == null) return 1f;
            return recipe076.VfxRecipe == Battle3DArtVfxRecipe071.ReactionBurst
                ? 0.72f + Mathf.Abs(Mathf.Sin(progress076 * Mathf.PI * 3f)) * 0.28f
                : 1f;
        }

        private static Color ExactVfxTint076(
            Battle3DArtChoreography071 recipe076,
            Color fallback)
        {
            if (recipe076 == null) return fallback;
            var semantic076 = Color.HSVToRGB(
                recipe076.SemanticPaletteHue01,
                recipe076.SemanticPaletteSaturation,
                recipe076.SemanticPaletteValue);
            var result076 = Color.Lerp(fallback, semantic076, 0.38f);
            result076.a = fallback.a;
            return result076;
        }

        private static IEnumerator RunTogether(params IEnumerator[] routines)
        {
            if (routines == null || routines.Length == 0) yield break;
            var active = new List<IEnumerator>();
            for (var index = 0; index < routines.Length; index++)
                if (routines[index] != null) active.Add(routines[index]);
            while (active.Count > 0)
            {
                for (var index = active.Count - 1; index >= 0; index--)
                    if (!active[index].MoveNext()) active.RemoveAt(index);
                if (active.Count > 0) yield return null;
            }
        }

        private static IEnumerator WaitScaled(float duration, Func<float> speed, Func<bool> skip)
        {
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                yield return null;
            }
        }

        private static IEnumerator Empty()
        {
            yield break;
        }

        private static Vector2 ApproachOffset(
            M2BattleActorRig072 actor,
            M2BattleActorRig072 target,
            float maximumDistance)
        {
            if (actor?.Root == null || target?.Root == null || actor.Root.parent == null)
                return Vector2.zero;
            // Each actor uses a different normalized anchor, so anchoredPosition is
            // intentionally zero for every home. Compare their actual stage-space
            // positions to derive a meaningful approach direction.
            var worldDelta = target.Root.TransformPoint(Vector3.zero) -
                             actor.Root.TransformPoint(Vector3.zero);
            var localDelta = actor.Root.parent.InverseTransformVector(worldDelta);
            var delta = new Vector2(localDelta.x, localDelta.y);
            if (delta.sqrMagnitude < 0.01f) return Vector2.zero;
            return delta.normalized * Mathf.Min(maximumDistance, delta.magnitude * 0.42f);
        }

        private static Vector3 ActorEffectPoint(M2BattleActorRig072 actor)
        {
            if (actor?.Root == null) return Vector3.zero;
            return actor.Root.TransformPoint(new Vector3(0f, actor.Root.rect.height * 0.55f, 0f));
        }

        private string FallbackAuthoredEffectPath(BattleBeatFamily family)
        {
            string effectId;
            switch (family)
            {
                case BattleBeatFamily.Mystic:
                case BattleBeatFamily.Invocation:
                    effectId = "MYSTIC_BURST";
                    break;
                case BattleBeatFamily.Restoration:
                case BattleBeatFamily.Recovery:
                    effectId = "RESTORATION_BLOOM";
                    break;
                case BattleBeatFamily.Guard:
                case BattleBeatFamily.Interception:
                case BattleBeatFamily.Formation:
                    effectId = "GUARD_IMPACT";
                    break;
                default:
                    effectId = "WEAPON_ARC";
                    break;
            }
            return M1VisualAssets.TryResolveBattleVfx(effectId, out _, out var resourcePath)
                ? resourcePath
                : string.Empty;
        }

        private string ResolveLoadableProjectilePath086(
            string preferredResourcePath,
            BattleBeatFamily family,
            bool supportive)
        {
            if (IsLoadableProjectilePath086(preferredResourcePath))
                return preferredResourcePath;
            if (!supportive) return string.Empty;

            var fallbackResourcePath086 = FallbackAuthoredEffectPath(family);
            return IsLoadableProjectilePath086(fallbackResourcePath086)
                ? fallbackResourcePath086
                : string.Empty;
        }

        private static bool IsLoadableProjectilePath086(string resourcePath)
        {
            return !string.IsNullOrWhiteSpace(resourcePath) &&
                   !IsRuntimeSilhouettePath(resourcePath) &&
                   BattleArtRuntimeRegistry011.LoadSprite(resourcePath) != null;
        }

        private bool IsPlayerUnion(string unionId) =>
            FindUnion(_battle?.PlayerUnions, unionId) != null;

        private bool IsEnemyUnion(string unionId) =>
            FindUnion(_battle?.EnemyUnions, unionId) != null;

        private static bool IsHarmful(BattleBeatFamily family) =>
            family == BattleBeatFamily.BasicMartial ||
            family == BattleBeatFamily.CombatArt ||
            family == BattleBeatFamily.Mystic ||
            family == BattleBeatFamily.Tactical ||
            family == BattleBeatFamily.Invocation ||
            family == BattleBeatFamily.Interception;

        private static bool IsSupportive(BattleBeatFamily family) =>
            family == BattleBeatFamily.Restoration ||
            family == BattleBeatFamily.Recovery ||
            family == BattleBeatFamily.Guard ||
            family == BattleBeatFamily.Formation ||
            family == BattleBeatFamily.Learning ||
            family == BattleBeatFamily.Breakthrough;

        private static M2BattleUnionView FindUnion(
            IReadOnlyList<M2BattleUnionView> unions,
            string unionId)
        {
            if (unions == null || string.IsNullOrWhiteSpace(unionId)) return null;
            for (var index = 0; index < unions.Count; index++)
                if (unions[index] != null &&
                    StringComparer.Ordinal.Equals(unions[index].UnionId, unionId))
                    return unions[index];
            return null;
        }

        private static M2BattleUnionView FindSelectedPlayerUnion(IReadOnlyList<M2BattleUnionView> unions)
        {
            if (unions == null) return null;
            for (var index = 0; index < unions.Count; index++)
                if (unions[index] != null && unions[index].IsSelected) return unions[index];
            for (var index = 0; index < unions.Count; index++)
                if (unions[index] != null && unions[index].CanAct) return unions[index];
            return null;
        }

        private static M2BattleUnionView FirstUnion(IReadOnlyList<M2BattleUnionView> unions)
        {
            if (unions == null) return null;
            for (var index = 0; index < unions.Count; index++)
                if (unions[index] != null) return unions[index];
            return null;
        }

        private static M2BattleUnionView FirstLivingUnion(IReadOnlyList<M2BattleUnionView> unions)
        {
            if (unions == null) return null;
            for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
            {
                var union = unions[unionIndex];
                if (union?.Members == null) continue;
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                    if (union.Members[memberIndex] != null && !union.Members[memberIndex].Downed)
                        return union;
            }
            return null;
        }

        private static string SelectedTargetUnionId(M2BattleView battle, string playerUnionId)
        {
            if (battle?.Forecasts == null || string.IsNullOrWhiteSpace(playerUnionId)) return string.Empty;
            string firstTarget = null;
            for (var index = 0; index < battle.Forecasts.Count; index++)
            {
                var forecast = battle.Forecasts[index];
                if (forecast == null || !StringComparer.Ordinal.Equals(forecast.UnionId, playerUnionId)) continue;
                if (string.IsNullOrWhiteSpace(firstTarget)) firstTarget = forecast.TargetId;
                if (forecast.IsSelected) return forecast.TargetId ?? string.Empty;
            }
            return firstTarget ?? string.Empty;
        }

        /// <summary>
        /// Chooses the enemy staged when a battle first opens. A player's selected
        /// forecast always wins. Before the Gate-Eater has a selected forecast, its
        /// signature Union is preferred over escort Unions so the authored boss name,
        /// scale, and cutout establish the threat immediately.
        /// </summary>
        public static string PreferredNarrativeTargetUnionId079(
            M2BattleView battle,
            string playerUnionId)
        {
            if (battle == null) return string.Empty;

            if (battle.Forecasts != null && !string.IsNullOrWhiteSpace(playerUnionId))
                for (var forecastIndex = 0; forecastIndex < battle.Forecasts.Count; forecastIndex++)
                {
                    var forecast = battle.Forecasts[forecastIndex];
                    if (forecast == null || !forecast.IsSelected ||
                        !StringComparer.Ordinal.Equals(forecast.UnionId, playerUnionId) ||
                        string.IsNullOrWhiteSpace(forecast.TargetId))
                        continue;
                    return forecast.TargetId;
                }

            var gateEater = !string.IsNullOrWhiteSpace(battle.BattleId) &&
                            battle.BattleId.IndexOf(
                                "ENCOUNTER071_GATE_EATER",
                                StringComparison.OrdinalIgnoreCase) >= 0 ||
                            !string.IsNullOrWhiteSpace(battle.Objective) &&
                            battle.Objective.IndexOf(
                                "Gate-Eater",
                                StringComparison.OrdinalIgnoreCase) >= 0;
            if (gateEater && battle.EnemyUnions != null)
                for (var unionIndex = 0; unionIndex < battle.EnemyUnions.Count; unionIndex++)
                {
                    var union = battle.EnemyUnions[unionIndex];
                    if (union?.Members == null) continue;
                    for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                    {
                        var member = union.Members[memberIndex];
                        if (member != null && !member.Downed &&
                            M2BattleActorRig072.IsGateEaterBossMember076(member.MemberId))
                            return union.UnionId ?? string.Empty;
                    }
                }

            return SelectedTargetUnionId(battle, playerUnionId);
        }

        private void SetUnionHeader(
            Text label,
            M2BattleUnionView union,
            string fallback,
            bool enemy)
        {
            if (label == null) return;
            if (union == null)
            {
                label.text = fallback;
                return;
            }
            PresentedUnionHp076(union, enemy, out var currentHp076, out var maximumHp076);
            var living076 = 0;
            var total076 = union.Members?.Count ?? 0;
            if (union.Members != null)
                for (var index076 = 0; index076 < union.Members.Count; index076++)
                {
                    var member076 = union.Members[index076];
                    if (member076 == null) continue;
                    var key076 = ActorKey(enemy, union.UnionId, member076.MemberId);
                    var presented076 = _presentedHpByActor076.TryGetValue(key076, out var value076)
                        ? value076
                        : member076.CurrentHp;
                    if (presented076 > 0) living076++;
                }
            var summary076 = M2BattleTacticalReadout074.FriendlyName(
                                 union.DisplayName,
                                 fallback) +
                             "  •  HP " + currentHp076.ToString(CultureInfo.InvariantCulture) +
                             "/" + maximumHp076.ToString(CultureInfo.InvariantCulture) +
                             "  •  AP " + Math.Max(0, union.CurrentAp).ToString(CultureInfo.InvariantCulture) +
                             "/" + Math.Max(0, union.MaximumAp).ToString(CultureInfo.InvariantCulture) +
                             "  •  " + living076.ToString(CultureInfo.InvariantCulture) +
                             "/" + total076.ToString(CultureInfo.InvariantCulture) + " UP";
            label.text = M2BattleActorRig072.NormalizeBossIdentity076(summary076).ToUpperInvariant();
        }

        public static string ActiveUnionStatus076(M2BattleUnionView union)
        {
            if (union == null) return "ACTIVE UNION  •  NO ORDER AVAILABLE";
            var formation = M2BattleTacticalReadout074.FriendlyName(union.Formation, "Formation")
                .ToUpperInvariant();
            return "ACTIVE UNION  •  AP " + Math.Max(0, union.CurrentAp).ToString(CultureInfo.InvariantCulture) +
                   "/" + Math.Max(0, union.MaximumAp).ToString(CultureInfo.InvariantCulture) +
                   "  •  " + formation;
        }

        public static string EnemyIntent076(
            M2BattleUnionView enemy,
            M2BattleUnionView ally,
            M2BattleView battle = null)
        {
            if (enemy == null) return "ENEMY INTENT  •  NO ACTIVE THREAT";
            if (IsGateEaterClimax076(battle, enemy))
            {
                if (GateEaterIsDown076(enemy))
                    return "BOSS DOWN  •  SKYHOME SAFE";
                var remaining = Math.Max(
                    1,
                    M2BattleCommandService.GateEaterDeadlineRounds076 -
                    Math.Max(1, battle.Round) + 1);
                return "BOSS CLOCK  •  " + remaining +
                       (remaining == 1 ? " ROUND" : " ROUNDS") +
                       "  •  BREACH SKYHOME";
            }
            // This is a player-facing translation of the authoritative engagement
            // posture. It never predicts or invents a hidden enemy command.
            var engagement = M2BattleTacticalReadout074.EngagementLabel(ally, enemy);
            var intent = StringComparer.Ordinal.Equals(engagement, "FLANK")
                ? "CUT OFF YOUR ACTIVE UNION"
                : StringComparer.Ordinal.Equals(engagement, "INTERFERENCE")
                    ? "DISRUPT YOUR COMPLETE ORDER"
                    : StringComparer.Ordinal.Equals(engagement, "BREAKTHROUGH")
                        ? "REGROUP UNDER PRESSURE"
                        : StringComparer.Ordinal.Equals(engagement, "DEADLOCK")
                            ? "HOLD THE DEADLOCK"
                            : StringComparer.Ordinal.Equals(engagement, "UNION BROKEN")
                                ? "PRESS YOUR BROKEN FORMATION"
                                : "PRESS THE ACTIVE UNION";
            return "ENEMY INTENT  •  " + intent;
        }

        private static bool IsGateEaterClimax076(M2BattleView battle, M2BattleUnionView enemy)
        {
            if (battle == null || string.IsNullOrWhiteSpace(battle.BattleId) ||
                battle.BattleId.IndexOf(
                    M2BattleCommandService.GateEaterBattleToken076,
                    StringComparison.OrdinalIgnoreCase) < 0)
                return false;
            return UnionContainsGateEater076(enemy);
        }

        private static bool GateEaterIsDown076(M2BattleUnionView enemy)
        {
            var members = enemy?.Members ?? Array.Empty<M2BattleMemberView>();
            var foundBoss = false;
            for (var index = 0; index < members.Count; index++)
            {
                var member = members[index];
                if (member == null ||
                    !M2BattleActorRig072.IsGateEaterBossMember076(member.MemberId))
                    continue;
                foundBoss = true;
                if (!member.Downed && member.CurrentHp > 0) return false;
            }
            return foundBoss;
        }

        private void UpdateTacticalReadout(M2BattleUnionView player, M2BattleUnionView enemy)
        {
            var allyShare = M2BattleTacticalReadout074.AllyMoraleShare(_battle);
            if (_allyMoraleFill != null)
            {
                var rect = _allyMoraleFill.rectTransform;
                rect.anchorMin = new Vector2(0.006f, 0.08f);
                rect.anchorMax = new Vector2(Mathf.Clamp(allyShare, 0.006f, 0.994f), 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            if (_enemyMoraleFill != null)
            {
                var rect = _enemyMoraleFill.rectTransform;
                rect.anchorMin = new Vector2(Mathf.Clamp(allyShare, 0.006f, 0.994f), 0.08f);
                rect.anchorMax = new Vector2(0.994f, 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            var allyPercent = Mathf.RoundToInt(allyShare * 100f);
            SetLabel(_moraleLabel, "ALLIES " + allyPercent + "%   BATTLE MORALE   " +
                                      (100 - allyPercent) + "% ENEMIES");
            SetLabel(_roundChainLabel, "ROUND " + Math.Max(1, _battle?.Round ?? 1) +
                                       "   •   CHAIN " + M2BattleTacticalReadout074.CombatChain(_battle));
            var engagement = M2BattleTacticalReadout074.EngagementLabel(player, enemy);
            SetLabel(_engagementLabel, engagement);
            if (_engagementLabel != null) _engagementLabel.color = EngagementColor(engagement);
        }

        public bool PresentBreakthroughEvent076(M2BattleEventView breakthrough)
        {
            // The authoritative event may resolve the final enemy. The tactical
            // overlay remains active until the sequence returns, so terminal-action
            // learning still deserves its bounded in-battle notice before Results.
            if (breakthrough == null || !_tacticalOverlayVisible ||
                !StringComparer.OrdinalIgnoreCase.Equals(
                    breakthrough.EventType,
                    "BREAKTHROUGH"))
                return false;
            var signature076 = BreakthroughNotificationSignature076(breakthrough);
            if (!_seenBreakthroughNotifications076.Add(signature076)) return false;
            _breakthroughNotifications076.Enqueue(breakthrough);
            if (!BreakthroughNotificationVisible076)
                ShowNextBreakthroughNotification076();
            return true;
        }

        public IEnumerator WaitForBreakthroughNotifications076(Func<bool> stopRequested)
        {
            while (BreakthroughNotificationVisible076 ||
                   _breakthroughNotifications076.Count > 0)
            {
                if (stopRequested != null && stopRequested())
                {
                    ClearBreakthroughNotifications076();
                    yield break;
                }
                if (!BreakthroughNotificationVisible076)
                    ShowNextBreakthroughNotification076();
                yield return null;
            }
        }

        private void ShowNextBreakthroughNotification076()
        {
            if (_breakthroughCallout074 == null) return;
            _breakthroughCallout074.gameObject.SetActive(false);
            _breakthroughVisibleUntil076 = 0f;
            _activeBreakthroughSignature076 = string.Empty;
            if (!_tacticalOverlayVisible || _breakthroughNotifications076.Count == 0)
                return;

            var breakthrough = _breakthroughNotifications076.Dequeue();
            var memberId = breakthrough.MemberId ?? breakthrough.ActorMemberId ?? string.Empty;
            var memberName = BattleMemberName074(_battle?.PlayerUnions, memberId);
            var artName = M2BattleReadableText021.ArtDisplayName(
                breakthrough.ArtId,
                breakthrough.Text,
                true);
            if (string.IsNullOrWhiteSpace(artName)) artName = "New Art";
            _breakthroughHeading074.text = "NEW ART LEARNED";
            _breakthroughDetail074.text = memberName.ToUpperInvariant() + " LEARNED\n" +
                                           artName.ToUpperInvariant() +
                                           "\nREADY FOR FUTURE BATTLES";
            _activeBreakthroughSignature076 = BreakthroughNotificationSignature076(breakthrough);
            _breakthroughVisibleUntil076 =
                Time.unscaledTime + BreakthroughNotificationSeconds076;
            _breakthroughNotificationPresentationCount076++;
            _breakthroughCallout074.gameObject.SetActive(true);
            _breakthroughCallout074.transform.SetAsLastSibling();
        }

        private void ClearBreakthroughNotifications076()
        {
            _breakthroughNotifications076.Clear();
            _breakthroughVisibleUntil076 = 0f;
            _activeBreakthroughSignature076 = string.Empty;
            if (_breakthroughCallout074 != null)
                _breakthroughCallout074.gameObject.SetActive(false);
        }

        private string BreakthroughNotificationSignature076(M2BattleEventView breakthrough) =>
            string.Join("|", new[]
            {
                _battle?.BattleId ?? string.Empty,
                breakthrough == null
                    ? string.Empty
                    : breakthrough.Round.ToString(CultureInfo.InvariantCulture),
                breakthrough == null
                    ? string.Empty
                    : breakthrough.Sequence.ToString(CultureInfo.InvariantCulture),
                breakthrough?.MemberId ?? breakthrough?.ActorMemberId ?? string.Empty,
                breakthrough?.ArtId ?? string.Empty
            });

        private static string BattleMemberName074(
            IReadOnlyList<M2BattleUnionView> unions,
            string memberId)
        {
            if (unions != null)
                for (var unionIndex = 0; unionIndex < unions.Count; unionIndex++)
                {
                    var members = unions[unionIndex]?.Members;
                    if (members == null) continue;
                    for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                    {
                        var member = members[memberIndex];
                        if (member == null || !StringComparer.Ordinal.Equals(member.MemberId, memberId)) continue;
                        return string.IsNullOrWhiteSpace(member.DisplayName)
                            ? "Adventurer"
                            : member.DisplayName.Trim();
                    }
                }
            return "Adventurer";
        }

        private static Color EngagementColor(string engagement)
        {
            if (StringComparer.Ordinal.Equals(engagement, "FLANK") ||
                StringComparer.Ordinal.Equals(engagement, "BREAKTHROUGH"))
                return new Color(0.35f, 1f, 0.78f, 1f);
            if (StringComparer.Ordinal.Equals(engagement, "INTERFERENCE") ||
                StringComparer.Ordinal.Equals(engagement, "UNION BROKEN"))
                return new Color(1f, 0.36f, 0.30f, 1f);
            if (StringComparer.Ordinal.Equals(engagement, "DEADLOCK")) return RuntimeUi.Warning;
            return new Color(0.70f, 0.90f, 1f, 1f);
        }

        private static void ConfigureOverlayText(Text text, int maximumSize, int minimumSize)
        {
            if (text == null) return;
            text.fontSize = maximumSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static string PlayerFacingUnionName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var result = value.Trim();
            if (Guid.TryParse(result, out _)) return fallback;
            if (result.IndexOf('_') < 0) return result;
            var prefixes = new[] { "PLAYER_UNION_", "ENEMY_UNION_", "UNION_", "ENEMY_" };
            for (var index = 0; index < prefixes.Length; index++)
            {
                if (!result.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) continue;
                result = result.Substring(prefixes[index].Length);
                break;
            }
            result = result.Replace('_', ' ').Trim();
            return string.IsNullOrWhiteSpace(result)
                ? fallback
                : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result.ToLowerInvariant());
        }

        private static void SetLabel(Text label, string value)
        {
            if (label != null) label.text = value ?? string.Empty;
        }

        private static string ActorKey(bool enemy, string unionId, string memberId) =>
            (enemy ? "E" : "P") + "|" + (unionId ?? string.Empty) + "|" + (memberId ?? string.Empty);

        private static bool IsRuntimeSilhouettePath(string resourcePath) =>
            !string.IsNullOrWhiteSpace(resourcePath) &&
            resourcePath.IndexOf(RuntimeEnemySilhouette070, StringComparison.OrdinalIgnoreCase) >= 0;

        private void ApplyUiSprite(Image image, string assetId)
        {
            if (image == null) return;
            var priorColor = image.color;
            if (BattleArtRuntimeRegistry011.TryResolveUiAsset(assetId, out _, out var sprite) && sprite != null)
            {
                image.sprite = sprite;
                image.preserveAspect = false;
                image.type = Image.Type.Sliced;
                image.color = priorColor;
            }
        }

        private static void ApplyGroundShadow076(Image image)
        {
            if (image == null) return;
            var tint = image.color;
            if (BattleArtRuntimeRegistry011.TryResolveUiAsset(
                    "SHARED_GROUND_SHADOW", out _, out var sprite) && sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = tint;
                image.raycastTarget = false;
                return;
            }
            image.color = Color.clear;
        }

        private void RetireAllActors()
        {
            foreach (var actor in _actors.Values) actor.Dispose();
            _retiredActors += _actors.Count;
            _actors.Clear();
        }

        private void EnsureInitialized()
        {
            if (_root == null || _stage == null)
                throw new InvalidOperationException("Initialize(host) must be called before using the battle diorama.");
        }

        private static RectTransform CreateRect(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var transform = child.GetComponent<RectTransform>();
            transform.SetParent(parent, false);
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            return transform;
        }

        private static Image CreateImage(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var transform = child.GetComponent<RectTransform>();
            transform.SetParent(parent, false);
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            var image = child.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            TextAnchor alignment,
            Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var transform = child.GetComponent<RectTransform>();
            transform.SetParent(parent, false);
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = color;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform transform)
        {
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
        }

        private static bool ShouldSkip(Func<bool> skip) => skip != null && skip();

        private static float ResolveSpeed(Func<float> speed) =>
            Mathf.Clamp(speed == null ? 1f : speed(), 0.1f,
                M2BattleExperienceController072.MaximumPlaybackSpeed108);
    }
}
