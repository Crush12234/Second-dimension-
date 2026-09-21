using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017D
{
    /// <summary>One concise instruction and the physical station that fulfills it.</summary>
    public sealed class GuildHallObjective069
    {
        public GuildHallObjective069(string targetHotspotId, string description)
        {
            TargetHotspotId = targetHotspotId ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string TargetHotspotId { get; }
        public string Description { get; }
    }

    /// <summary>
    /// Host-owned actions for the six physical Guild Hall destinations. The hall
    /// never mutates campaign state: it only gives the player a world-space way to
    /// reach the focused flows that already own those commands.
    /// </summary>
    public sealed class GuildHallDestinationCallbacks069
    {
        private Action _recruitment;
        private Action _armory;

        public Action Guide { get; set; }
        public Action Recruitment { get => _recruitment; set => _recruitment = value; }
        public Action Applicants { get => _recruitment; set => _recruitment = value; }
        public Action Armory { get => _armory; set => _armory = value; }
        public Action Inventory { get => _armory; set => _armory = value; }
        public Action Party { get; set; }
        public Action Contract { get; set; }
        public Action Practice { get; set; }
        public Action QuickPlay { get; set; }
        public Action Exit { get; set; }
        public Action Leave { get; set; }

        internal Action Resolve069(string destinationId)
        {
            switch (destinationId)
            {
                case WalkableGuildHall069.GuideDestinationId069: return Guide;
                case WalkableGuildHall069.RecruitmentDestinationId069: return Recruitment;
                case WalkableGuildHall069.ArmoryDestinationId069: return Armory;
                case WalkableGuildHall069.PartyDestinationId069: return Party;
                case WalkableGuildHall069.ContractDestinationId069: return Contract;
                case WalkableGuildHall069.PracticeDestinationId069: return Practice;
                default: return null;
            }
        }
    }

    /// <summary>
    /// A self-contained, side-on 2.5D Guild Hall. The painted Hall is the world,
    /// not a menu background: the founder walks between visible facilities and
    /// activates the nearest one with E. Gameplay state and navigation remain with
    /// the host callbacks so this presentation layer is safe to rebuild or destroy.
    /// </summary>
    public sealed class WalkableGuildHall069 : MonoBehaviour
    {
        public const string GuideDestinationId069 = "GUIDE";
        public const string RecruitmentDestinationId069 = "RECRUITMENT";
        public const string ApplicantsDestinationId069 = RecruitmentDestinationId069;
        public const string ArmoryDestinationId069 = "ARMORY";
        public const string InventoryDestinationId069 = ArmoryDestinationId069;
        public const string PartyDestinationId069 = "PARTY";
        public const string ContractDestinationId069 = "CONTRACT";
        public const string PracticeDestinationId069 = "PRACTICE";

        private const int WorldLayer069 = 28;
        private const float WalkSpeed069 = 5.8f;
        private const float DodgeSpeed069 = 13.2f;
        private const float DodgeDuration069 = 0.38f;
        private const float HallMinimumX069 = -16.5f;
        private const float HallMaximumX069 = 16.5f;
        private const float HallMinimumZ069 = -2.65f;
        private const float HallMaximumZ069 = 2.20f;
        private static readonly Vector3 InitialFounderPosition069 = new Vector3(-13.45f, 0.04f, -1.15f);

        private static readonly string[] DestinationOrder069 =
        {
            GuideDestinationId069,
            RecruitmentDestinationId069,
            ArmoryDestinationId069,
            PartyDestinationId069,
            ContractDestinationId069,
            PracticeDestinationId069
        };

        private sealed class HallDestination069
        {
            public string Id;
            public string DisplayName;
            public string InteractionTitle;
            public Transform Anchor;
            public float Radius;
            public Action Action;
            public Transform AreaHighlight;
            public SpriteRenderer AreaHighlightRenderer;
            public Vector3 AreaHighlightBaseScale;
            public Color Accent;
        }

        private GuildHallDestinationCallbacks069 _callbacks;
        private Func<GuildHallObjective069> _objectiveProvider;
        private Func<string> _progressionSummaryProvider073;
        private readonly List<HallDestination069> _destinations = new List<HallDestination069>();
        private readonly Dictionary<string, HallDestination069> _destinationsById =
            new Dictionary<string, HallDestination069>(StringComparer.Ordinal);
        private readonly Dictionary<string, Material> _materials =
            new Dictionary<string, Material>(StringComparer.Ordinal);
        private readonly List<Sprite> _ownedRuntimeSprites = new List<Sprite>();
        private readonly List<Texture2D> _ownedRuntimeTextures = new List<Texture2D>();
        private readonly List<Transform> _billboards = new List<Transform>();

        private GameObject _worldRoot;
        private Canvas _hudCanvas;
        private Camera _worldCamera;
        private AudioSource _hallAmbience069;
        private AudioSource _hallInteractionAudio069;
        private CharacterController _controller;
        private WorldCharacterMotor070 _worldMotor070;
        private WorldCharacterAnimator070 _worldAnimator070;
        private WorldInput071 _worldInput071;
        private Transform _founderRoot;
        private Transform _founderBillboard070;
        private Transform _founderVisual;
        private Transform _founderShadow071;
        private Transform _guideShadow071;
        private Transform _objectiveMarker;
        private Transform _objectivePulse;
        private TextMesh _objectiveWorldText073;
        private Sprite _softEllipseSprite071;
        private Text _objectiveText;
        private Text _routeText073;
        private Image _routeFill073;
        private Text _promptText;
        private Image _promptPanel071;
        private Text _locationText;
        private Text _progressionText073;
        private Button _quickPlayButton070;
        private Image _controlsPanel071;
        private Text _controlsText076;
        private string _objectiveDestinationId = GuideDestinationId069;
        private string _objectiveDescription = "Speak with the Guild guide to begin your first day.";
        private string _temporaryMessage = string.Empty;
        private Vector3 _lastMoveDirection = Vector3.forward;
        private Vector3 _cameraVelocity;
        private float _dodgeRemaining;
        private float _walkCycle;
        private float _temporaryMessageUntil;
        private bool _interactionLocked;
        private bool _hostSuspended;
        private bool _shutdown;

        public Transform ControlledAvatar069 => _founderRoot;
        public Transform WorldRootForVerification069 => _worldRoot != null ? _worldRoot.transform : null;
        public Camera WorldCameraForVerification069 => _worldCamera;
        public Canvas HudCanvasForVerification069 => _hudCanvas;
        public bool UsesTrueWorldMovement069 => _controller != null && _worldCamera != null;
        public bool UsesAuthoredHallArt069 { get; private set; }
        public bool UsesFounderStandee069 { get; private set; }
        public string FounderStandeeResourceKeyForVerification076 { get; private set; } =
            string.Empty;
        public string FounderPoseResourceRootForVerification076 { get; private set; } =
            string.Empty;
        public Text ControlsTextForVerification076 => _controlsText076;
        public bool DesktopControlsVisibleForVerification076 =>
            _controlsPanel071 != null &&
            _controlsPanel071.gameObject.activeInHierarchy;
        public bool IsActive069 => !_shutdown && _worldRoot != null && _worldRoot.activeInHierarchy;
        public bool InputSuspended069 => _hostSuspended;
        public int HotspotCountForVerification069 => _destinations.Count;
        public string CurrentObjectiveHotspotId069 => _objectiveDestinationId;
        public IReadOnlyList<string> DestinationIdsForVerification069 => DestinationOrder069;
        public bool HasQuickPlayForVerification070 =>
            _quickPlayButton070 != null && _quickPlayButton070.gameObject.activeInHierarchy;
        public bool UsesCrossPlatformWorldInput071 => _worldInput071 != null;
        public bool HasTouchControlsForVerification071 =>
            _worldInput071 != null && _worldInput071.TouchControlsBuilt071;
        public WorldInput071 WorldInputForVerification071 => _worldInput071;
        public string CurrentInteractionPromptForVerification071 =>
            _promptText != null ? _promptText.text : string.Empty;

        public void SetProgressionSummaryProvider073(Func<string> provider)
        {
            _progressionSummaryProvider073 = provider;
            UpdateProgressionSummary073();
        }

        /// <summary>Compatibility seam proving the preserved host callback remains callable.</summary>
        public void InvokeQuickPlayForVerification070() => InvokeQuickPlay070();

        /// <summary>PlayMode seam proving which physical facility currently receives E.</summary>
        public string NearestInteractionIdForVerification069
        {
            get
            {
                var nearest = NearestDestination069();
                return nearest != null ? nearest.Id : string.Empty;
            }
        }

        /// <summary>
        /// Builds the hall. Calling Begin again is supported and replaces the old
        /// world cleanly; it never creates two active Hall roots or cameras.
        /// </summary>
        public void Begin069(
            GuildHallDestinationCallbacks069 callbacks,
            string objectiveDestinationId,
            string objectiveDescription)
        {
            Shutdown069();
            _shutdown = false;
            enabled = true;
            _interactionLocked = false;
            _hostSuspended = false;
            _callbacks = callbacks ?? new GuildHallDestinationCallbacks069();
            _objectiveDestinationId = NormalizeDestinationId069(objectiveDestinationId);
            _objectiveDescription = string.IsNullOrWhiteSpace(objectiveDescription)
                ? "Follow the warm light to the next Hall facility."
                : objectiveDescription.Trim();

            BuildWorld069();
            BuildHud069();
            Refresh069(_objectiveDestinationId, _objectiveDescription);
            UpdateCamera069(1f);
            UpdateWorldPresentation069();
            UpdateInteractionPrompt069();
        }

        /// <summary>
        /// Preferred host integration. Parameterless Refresh069 re-reads this
        /// provider after any save/coordinator change and moves the objective anchor.
        /// </summary>
        public void Begin069(
            GuildHallDestinationCallbacks069 callbacks,
            Func<GuildHallObjective069> objectiveProvider)
        {
            _objectiveProvider = objectiveProvider;
            var objective = SafeObjectiveFromProvider069();
            Begin069(
                callbacks,
                objective != null ? objective.TargetHotspotId : GuideDestinationId069,
                objective != null ? objective.Description : "Speak with the Guild guide to begin your first day.");
            _objectiveProvider = objectiveProvider;
        }

        /// <summary>Convenience overload for host presenters that keep callbacks as fields.</summary>
        public void Begin069(
            Action openGuide,
            Action openRecruitment,
            Action openArmory,
            Action openParty,
            Action openContract,
            Action openPractice,
            string objectiveDestinationId,
            string objectiveDescription)
        {
            Begin069(new GuildHallDestinationCallbacks069
            {
                Guide = openGuide,
                Recruitment = openRecruitment,
                Armory = openArmory,
                Party = openParty,
                Contract = openContract,
                Practice = openPractice
            }, objectiveDestinationId, objectiveDescription);
        }

        private void Update()
        {
            if (_shutdown || _controller == null || !_controller.enabled) return;

            var input071 = _worldInput071 != null
                ? _worldInput071.CaptureFrame071()
                : WorldInput071.CaptureLegacyFrame071();

            var exitCallback = _callbacks != null ? _callbacks.Exit ?? _callbacks.Leave : null;
            if (input071.BackPressed071 && exitCallback != null)
            {
                InvokeHost069(exitCallback);
                return;
            }
            var movement070 = input071.Movement071;
            if (_worldMotor070 != null)
            {
                var movementLocked070 = _interactionLocked || _hostSuspended;
                _worldMotor070.SetInput070(
                    movementLocked070 ? Vector2.zero : movement070,
                    !movementLocked070 && input071.RunHeld071,
                    !movementLocked070 && input071.DodgePressed071,
                    false);
            }
            else
            {
                ApplyMovementInput069(
                    movement070,
                    input071.DodgePressed071,
                    Time.unscaledDeltaTime);
            }
            UpdateCamera069(Time.unscaledDeltaTime);
            UpdateWorldPresentation069();
            UpdateInteractionPrompt069();

            if (!_interactionLocked && input071.InteractPressed071)
                ApplyInteractionInput069();
        }

        /// <summary>Input seam shared by Update and PlayMode verification.</summary>
        public void ApplyMovementInput069(Vector2 input, bool beginDodge, float deltaTime)
        {
            if (_shutdown || _controller == null || !_controller.enabled || _interactionLocked) return;
            if (_worldMotor070 != null)
            {
                _worldMotor070.Simulate070(input, false, beginDodge, false, deltaTime);
                ClampFounderToHall070();
                return;
            }
            var direction = new Vector3(input.x, 0f, input.y);
            if (direction.sqrMagnitude > 1f) direction.Normalize();
            if (direction.sqrMagnitude > 0.001f)
            {
                _lastMoveDirection = direction.normalized;
                if (_founderVisual != null && Mathf.Abs(direction.x) > 0.05f)
                {
                    var scale = _founderVisual.localScale;
                    scale.x = Mathf.Abs(scale.x) * (direction.x < 0f ? -1f : 1f);
                    _founderVisual.localScale = scale;
                }
            }

            if (beginDodge && _dodgeRemaining <= 0f)
            {
                _dodgeRemaining = DodgeDuration069;
                if (direction.sqrMagnitude < 0.001f) direction = _lastMoveDirection;
            }

            var dodging = _dodgeRemaining > 0f;
            if (dodging)
            {
                direction = _lastMoveDirection;
                _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - Mathf.Max(0f, deltaTime));
            }

            var safeDelta = Mathf.Max(0f, deltaTime);
            _controller.Move(direction * (dodging ? DodgeSpeed069 : WalkSpeed069) * safeDelta);
            var position = _founderRoot.position;
            position.x = Mathf.Clamp(position.x, HallMinimumX069, HallMaximumX069);
            position.y = InitialFounderPosition069.y;
            position.z = Mathf.Clamp(position.z, HallMinimumZ069, HallMaximumZ069);
            _founderRoot.position = position;

            if (_founderVisual != null)
            {
                _walkCycle += direction.magnitude * safeDelta * 11f;
                var bounce = direction.sqrMagnitude > 0.001f
                    ? Mathf.Abs(Mathf.Sin(_walkCycle)) * 0.10f
                    : 0f;
                _founderVisual.localPosition = new Vector3(0f, bounce, 0f);
                if (dodging)
                {
                    var progress = 1f - _dodgeRemaining / DodgeDuration069;
                    _founderVisual.localRotation = Quaternion.Euler(0f, 0f, progress * 360f);
                }
                else _founderVisual.localRotation = Quaternion.identity;
            }
        }

        private void LateUpdate()
        {
            if (_shutdown || _worldMotor070 == null) return;
            ClampFounderToHall070();
            FaceFounderBillboardToCamera070();
        }

        private void ClampFounderToHall070()
        {
            if (_founderRoot == null || _controller == null || !_controller.enabled) return;
            var current = _founderRoot.position;
            var clampedX = Mathf.Clamp(current.x, HallMinimumX069, HallMaximumX069);
            var laneMinimumZ = HallLaneMinimumZ069(clampedX);
            var laneMaximumZ = HallLaneMaximumZ069(clampedX);
            var clamped = new Vector3(
                clampedX,
                InitialFounderPosition069.y,
                Mathf.Clamp(current.z, laneMinimumZ, laneMaximumZ));
            var correction = clamped - current;
            if (correction.sqrMagnitude > 0.000001f) _controller.Move(correction);
            UpdateFounderGrounding071();
        }

        private static float HallLaneMinimumZ069(float worldX)
        {
            var edge = Mathf.InverseLerp(0f, HallMaximumX069, Mathf.Abs(worldX));
            return Mathf.Lerp(-2.34f, -2.06f, edge);
        }

        private static float HallLaneMaximumZ069(float worldX)
        {
            var edge = Mathf.InverseLerp(0f, HallMaximumX069, Mathf.Abs(worldX));
            return Mathf.Lerp(1.64f, 1.34f, edge);
        }

        /// <summary>Proximity interaction seam shared by E and PlayMode verification.</summary>
        public void ApplyInteractionInput069()
        {
            if (_shutdown || _interactionLocked) return;
            var nearest = NearestDestination069();
            if (nearest == null)
            {
                ShowTemporaryMessage069("Move closer to a named Hall station, then use ACT.");
                return;
            }
            nearest.Action?.Invoke();
        }

        /// <summary>
        /// Lets a host-owned overlay keep the Hall visible without allowing movement
        /// or repeated E presses behind that overlay. Most full-page destinations can
        /// simply call Shutdown069 instead.
        /// </summary>
        public void SuspendInput069()
        {
            if (_shutdown) return;
            _hostSuspended = true;
            _interactionLocked = true;
            _worldMotor070?.StopImmediately070();
            UpdateInteractionPrompt069();
        }

        public void ResumeInput069()
        {
            if (_shutdown) return;
            _hostSuspended = false;
            _interactionLocked = false;
            UpdateInteractionPrompt069();
        }

        /// <summary>
        /// Projects host-owned progression into the Hall without rebuilding it. The
        /// subtle area light moves immediately, so the player always has one clear next step.
        /// </summary>
        public void Refresh069(string objectiveDestinationId, string objectiveDescription)
        {
            _objectiveDestinationId = NormalizeDestinationId069(objectiveDestinationId);
            _objectiveDescription = string.IsNullOrWhiteSpace(objectiveDescription)
                ? "Follow the warm light to the next Hall facility."
                : objectiveDescription.Trim();
            RefreshObjectiveMarker069();
            UpdateObjectiveText069();
            UpdateInteractionPrompt069();
        }

        /// <summary>Refreshes marker, objective copy, and prompts after a host state change.</summary>
        public void Refresh069()
        {
            var provided = SafeObjectiveFromProvider069();
            if (provided != null)
            {
                _objectiveDestinationId = NormalizeDestinationId069(provided.TargetHotspotId);
                _objectiveDescription = string.IsNullOrWhiteSpace(provided.Description)
                    ? "Follow the warm light to the next Hall facility."
                    : provided.Description.Trim();
            }
            RefreshObjectiveMarker069();
            UpdateObjectiveText069();
            UpdateInteractionPrompt069();
        }

        private GuildHallObjective069 SafeObjectiveFromProvider069()
        {
            if (_objectiveProvider == null) return null;
            try
            {
                return _objectiveProvider.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                return null;
            }
        }

        public bool TryGetHotspotPositionForVerification069(string destinationId, out Vector3 position)
        {
            var normalized = NormalizeDestinationId069(destinationId);
            if (_destinationsById.TryGetValue(normalized, out var destination) && destination.Anchor != null)
            {
                position = destination.Anchor.position;
                return true;
            }
            position = Vector3.zero;
            return false;
        }

        public bool TeleportToHotspotForVerification069(string destinationId)
        {
            if (_founderRoot == null ||
                !TryGetHotspotPositionForVerification069(destinationId, out var hotspotPosition)) return false;
            var targetZ = Mathf.Clamp(
                hotspotPosition.z - 0.90f,
                HallLaneMinimumZ069(hotspotPosition.x),
                HallLaneMaximumZ069(hotspotPosition.x));
            _founderRoot.position = new Vector3(
                hotspotPosition.x,
                InitialFounderPosition069.y,
                targetZ);
            UpdateCamera069(1f);
            UpdateFounderGrounding071();
            UpdateInteractionPrompt069();
            return true;
        }

        private void BuildWorld069()
        {
            _worldRoot = new GameObject("Walkable Guild Hall World 069");
            _worldRoot.layer = WorldLayer069;
            BuildHallAudio071();
            BuildCamera069();
            BuildPaintedHall069();
            BuildFloor069();
            BuildDestinations069();
            BuildFounder069();
            BuildObjectiveMarker069();
        }

        private void BuildHallAudio071()
        {
            var ambience = Resources.Load<AudioClip>(
                "SecondDimension/GuildCity017F/Audio/AMB_HALL_RUINED_017F");
            if (ambience != null)
            {
                _hallAmbience069 = _worldRoot.AddComponent<AudioSource>();
                _hallAmbience069.clip = ambience;
                _hallAmbience069.loop = true;
                _hallAmbience069.playOnAwake = false;
                _hallAmbience069.spatialBlend = 0f;
                _hallAmbience069.volume = 0.20f;
                _hallAmbience069.Play();
            }

            _hallInteractionAudio069 = _worldRoot.AddComponent<AudioSource>();
            _hallInteractionAudio069.playOnAwake = false;
            _hallInteractionAudio069.spatialBlend = 0f;
            _hallInteractionAudio069.volume = 0.52f;
        }

        private void BuildCamera069()
        {
            var cameraObject = new GameObject("Guild Hall 2.5D Following Camera 069");
            cameraObject.transform.SetParent(_worldRoot.transform, false);
            cameraObject.layer = WorldLayer069;
            _worldCamera = cameraObject.AddComponent<Camera>();
            _worldCamera.clearFlags = CameraClearFlags.SolidColor;
            _worldCamera.backgroundColor = new Color(0.018f, 0.022f, 0.035f, 1f);
            // A level orthographic frame makes the room navigable. It keeps the
            // player and destination on one readable plane and never exposes the
            // empty margins around the authored Hall plate.
            _worldCamera.orthographic = true;
            _worldCamera.orthographicSize = 5.25f;
            _worldCamera.fieldOfView = 52f;
            _worldCamera.nearClipPlane = 0.15f;
            _worldCamera.farClipPlane = 80f;
            _worldCamera.cullingMask = 1 << WorldLayer069;
            _worldCamera.depth = 88f;
            _worldCamera.transform.position = new Vector3(-8.0f, 4.45f, -14.35f);
            _worldCamera.transform.LookAt(new Vector3(-8.0f, 4.45f, 0.55f));
        }

        private void BuildPaintedHall069()
        {
            var sprite = LoadSprite069(
                "SecondDimension/Art/FirstHour071/Environments/GUILD_HALL_GAMEPLAY_PLATE_071",
                new Vector2(0.5f, 0.5f));
            if (sprite == null)
            {
                sprite = LoadSprite069(
                    "SecondDimension/Art/Backgrounds/BG_GUILD_HALL_STAGE_01",
                    new Vector2(0.5f, 0.5f));
            }
            if (sprite == null)
            {
                sprite = LoadSprite069(
                    "SecondDimension/Art/Guided063/GUILD_HALL_HOME_BACKGROUND_V63",
                    new Vector2(0.5f, 0.5f));
            }
            if (sprite == null) return;

            var backdrop = new GameObject("Authored Living Guild Hall Backdrop 069");
            backdrop.transform.SetParent(_worldRoot.transform, false);
            backdrop.transform.position = new Vector3(0f, 6.30f, 5.2f);
            backdrop.layer = WorldLayer069;
            var renderer = backdrop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -100;
            var width = Mathf.Max(0.01f, sprite.bounds.size.x);
            backdrop.transform.localScale = Vector3.one * (44.0f / width);
            UsesAuthoredHallArt069 = true;

        }

        private void BuildFloor069()
        {
            var walkableFloor = CreatePrimitive069(
                PrimitiveType.Cube,
                "Guild Hall Walkable Timber Floor 069",
                new Vector3(0f, -0.30f, -0.05f),
                new Vector3(35.2f, 0.60f, 6.7f),
                new Color(0.16f, 0.09f, 0.045f, 1f),
                true);
            var walkableFloorRenderer = walkableFloor.GetComponent<Renderer>();
            if (walkableFloorRenderer != null)
                walkableFloorRenderer.enabled = !UsesAuthoredHallArt069;

            // The authored Hall plate already contains its timber floor and runner.
            // Keep these fallback primitives only when no authored environment loaded;
            // the invisible floor collider above remains authoritative for movement.
            if (!UsesAuthoredHallArt069)
            {
                CreatePrimitive069(
                    PrimitiveType.Cube,
                    "Guild Hall Sapphire Runner 069",
                    new Vector3(0f, 0.018f, -0.45f),
                    new Vector3(34.0f, 0.035f, 1.65f),
                    new Color(0.045f, 0.13f, 0.22f, 1f),
                    false);

                for (var index = -8; index <= 8; index++)
                {
                    CreatePrimitive069(
                        PrimitiveType.Cube,
                        "Hall Runner Gold Stitch " + (index + 9) + " 069",
                        new Vector3(index * 2.0f, 0.04f, -0.45f),
                        new Vector3(0.035f, 0.025f, 1.58f),
                        new Color(0.54f, 0.37f, 0.10f, 1f),
                        false);
                }
            }

            CreatePrimitive069(PrimitiveType.Cube, "Hall Left Walk Boundary 069",
                new Vector3(HallMinimumX069 - 0.35f, 1.0f, 0f), new Vector3(0.35f, 2.0f, 7f),
                new Color(0f, 0f, 0f, 0f), true).GetComponent<Renderer>().enabled = false;
            CreatePrimitive069(PrimitiveType.Cube, "Hall Right Walk Boundary 069",
                new Vector3(HallMaximumX069 + 0.35f, 1.0f, 0f), new Vector3(0.35f, 2.0f, 7f),
                new Color(0f, 0f, 0f, 0f), true).GetComponent<Renderer>().enabled = false;
            CreatePrimitive069(PrimitiveType.Cube, "Hall Foreground Rail Collision 071",
                new Vector3(0f, 1.0f, HallMinimumZ069 - 0.20f), new Vector3(35.2f, 2.0f, 0.32f),
                Color.clear, true).GetComponent<Renderer>().enabled = false;
            CreatePrimitive069(PrimitiveType.Cube, "Hall Back Wall Collision 071",
                new Vector3(0f, 1.0f, 1.78f), new Vector3(35.2f, 2.0f, 0.32f),
                Color.clear, true).GetComponent<Renderer>().enabled = false;

            // Match the large strategy table painted into the center of the plate.
            // The collider is invisible; the authored furniture remains the only
            // visible body and the founder must walk around its foreground edge.
            CreatePrimitive069(PrimitiveType.Cube, "Strategy Table Art Matched Collision 071",
                new Vector3(2.35f, 0.86f, 0.58f), new Vector3(6.15f, 1.72f, 1.42f),
                Color.clear, true).GetComponent<Renderer>().enabled = false;
        }

        private void BuildDestinations069()
        {
            var guide = AddDestination069(
                GuideDestinationId069,
                "Mira, Guild Guide",
                "Mira: “The emergency charter is ready.”",
                new Vector3(-14.05f, 0.04f, -0.82f),
                2.35f,
                new Color(0.18f, 0.72f, 0.86f));
            AddDestination069(
                RecruitmentDestinationId069,
                "Recruitment Board",
                "Read today's applicant notices",
                new Vector3(-6.25f, 0.04f, 0.92f),
                2.20f,
                new Color(0.64f, 0.34f, 0.76f));
            AddDestination069(
                ArmoryDestinationId069,
                "Armory",
                "Inspect weapons and member equipment",
                new Vector3(-11.45f, 0.04f, 0.86f),
                2.15f,
                new Color(0.86f, 0.48f, 0.16f));
            AddDestination069(
                PartyDestinationId069,
                "Strategy Table",
                "Form and review your Unions",
                new Vector3(2.35f, 0.04f, -1.28f),
                2.30f,
                new Color(0.24f, 0.63f, 0.92f));
            AddDestination069(
                ContractDestinationId069,
                "Charter Bell",
                "Answer the active Guild contract",
                new Vector3(8.10f, 0.04f, 0.82f),
                2.25f,
                new Color(0.92f, 0.69f, 0.18f));
            AddDestination069(
                PracticeDestinationId069,
                "Training Yard",
                "Step outside to practice Union commands",
                new Vector3(14.55f, 0.04f, 0.38f),
                2.35f,
                new Color(0.82f, 0.24f, 0.18f));

            if (guide != null) CreateGuideCharacter069(guide.Anchor);
        }

        private HallDestination069 AddDestination069(
            string id,
            string displayName,
            string interactionTitle,
            Vector3 position,
            float radius,
            Color accent)
        {
            var anchorObject = new GameObject("Guild Hall " + displayName + " Hotspot 069");
            anchorObject.transform.SetParent(_worldRoot.transform, false);
            anchorObject.transform.position = position;
            anchorObject.layer = WorldLayer069;
            // Stations are already painted into the authored Hall. Runtime boxes,
            // pedestals, labels, and colored rings would sit on top of that art and
            // make the scene look like a debug map. A low-alpha pool of warm light
            // is the only world-space guidance, and appears only for the live goal
            // or the facility immediately beside the player.
            var areaHighlight069 = CreateSoftFloorSprite069(
                anchorObject.transform,
                displayName + " Subtle Area Light 071",
                new Vector3(0f, 0.028f, 0f),
                new Vector2(id == PartyDestinationId069 ? 2.85f : 2.20f, 0.76f),
                new Color(accent.r, accent.g, accent.b, 0.15f),
                6);
            var highlightRenderer069 = areaHighlight069 != null
                ? areaHighlight069.GetComponent<SpriteRenderer>()
                : null;

            var destination = new HallDestination069
            {
                Id = id,
                DisplayName = displayName,
                InteractionTitle = interactionTitle,
                Anchor = anchorObject.transform,
                Radius = radius,
                AreaHighlight = areaHighlight069,
                AreaHighlightRenderer = highlightRenderer069,
                AreaHighlightBaseScale = areaHighlight069 != null
                    ? areaHighlight069.localScale
                    : Vector3.one,
                Accent = accent
            };
            destination.Action = () => InvokeDestination069(destination);
            _destinations.Add(destination);
            _destinationsById[id] = destination;
            return destination;
        }

        private void CreateGuideCharacter069(Transform guideAnchor)
        {
            var guideSprite = LoadSprite069(
                "SecondDimension/Art/Backgrounds/GUILD_HALL_GUIDE_V62",
                new Vector2(0.50f, 0.03f));
            if (guideSprite == null) return;
            var guide = CreateSpriteObject069(
                "Mira Guild Guide Character 069",
                guideAnchor,
                guideSprite,
                new Vector3(-0.58f, 0.04f, 0.18f),
                3.55f,
                12);
            _billboards.Add(guide);
            CreateWorldLabel073(guideAnchor, "MIRA  •  GUILD GUIDE",
                new Vector3(-0.58f, 3.82f, 0.18f), RuntimeUi.Text, 72);
            _guideShadow071 = CreateSoftFloorSprite069(
                guideAnchor,
                "Mira Grounded Contact Shadow 071",
                new Vector3(-0.58f, 0.026f, 0.20f),
                new Vector2(1.10f, 0.44f),
                new Color(0.02f, 0.018f, 0.018f, 0.30f),
                10);
        }

        private void BuildFounder069()
        {
            var founderObject = new GameObject("Controlled Founder Avatar 069");
            founderObject.transform.SetParent(_worldRoot.transform, false);
            founderObject.transform.position = InitialFounderPosition069;
            founderObject.layer = WorldLayer069;
            _founderRoot = founderObject.transform;

            _controller = founderObject.AddComponent<CharacterController>();
            _controller.radius = 0.38f;
            _controller.height = 1.75f;
            _controller.center = new Vector3(0f, 0.88f, 0f);
            _controller.stepOffset = 0.28f;
            _controller.skinWidth = 0.05f;

            var billboardObject070 = new GameObject("Founder Camera-Facing Billboard Pivot 070");
            billboardObject070.transform.SetParent(_founderRoot, false);
            billboardObject070.layer = WorldLayer069;
            _founderBillboard070 = billboardObject070.transform;

            FounderStandeeResourceKeyForVerification076 =
                M1VisualAssets.GuildmasterStandeeResourceKey076;
            var sprite = LoadSprite069(
                FounderStandeeResourceKeyForVerification076,
                new Vector2(0.50f, 0.02f));
            if (sprite != null)
            {
                _founderVisual = CreateSpriteObject069(
                    "Founder Vanguard Standee 069",
                    _founderBillboard070,
                    sprite,
                    Vector3.zero,
                    3.80f,
                    30);
                UsesFounderStandee069 = true;
            }
            else
            {
                var fallback = CreatePrimitive069(PrimitiveType.Capsule, "Founder Fallback Body 069",
                    Vector3.zero, new Vector3(0.70f, 0.92f, 0.70f),
                    new Color(0.12f, 0.58f, 0.78f), false);
                fallback.transform.SetParent(_founderBillboard070, false);
                fallback.transform.localPosition = new Vector3(0f, 0.92f, 0f);
                _founderVisual = fallback.transform;
            }
            _billboards.Add(_founderBillboard070);
            CreateWorldLabel073(_founderBillboard070, "YOU",
                new Vector3(0f, 4.05f, 0f), RuntimeUi.Accent, 90);
            _founderShadow071 = CreateSoftFloorSprite069(
                _founderRoot,
                "Founder Grounded Contact Shadow 071",
                new Vector3(0f, 0.026f, 0.04f),
                new Vector2(1.18f, 0.48f),
                new Color(0.02f, 0.018f, 0.018f, 0.34f),
                18);

            _worldMotor070 = founderObject.AddComponent<WorldCharacterMotor070>();
            _worldMotor070.Configure070(new WorldCharacterMotorTuning070
            {
                WalkSpeed = WalkSpeed069,
                RunSpeed = 8.2f,
                DodgeSpeed = DodgeSpeed069,
                DodgeDuration = DodgeDuration069,
                Acceleration = 21f,
                Braking = 27f,
                TurnSpeedDegrees = 760f
            }, driveFromLegacyKeyboard: false);
            _worldAnimator070 = founderObject.AddComponent<WorldCharacterAnimator070>();
            _worldAnimator070.Configure070(
                _worldMotor070,
                _founderVisual,
                automaticTick: true,
                proceduralFallback: true);
            FounderPoseResourceRootForVerification076 =
                M1VisualAssets.GuildmasterPoseResourceRoot076;
            _worldAnimator070.ConfigureSpritePoses070(FounderPoseResourceRootForVerification076);
        }

        private void BuildObjectiveMarker069()
        {
            var markerObject = new GameObject("Guild Hall Current Objective Anchor 073");
            markerObject.transform.SetParent(_worldRoot.transform, false);
            markerObject.layer = WorldLayer069;
            _objectiveMarker = markerObject.transform;
            _objectivePulse = CreateSoftFloorSprite069(
                _objectiveMarker,
                "Guild Hall Gold Destination Pool 073",
                new Vector3(0f, 0.035f, 0f),
                new Vector2(3.1f, 1.0f),
                new Color(1f, 0.66f, 0.16f, 0.34f),
                8);
            _objectiveWorldText073 = CreateWorldLabel073(
                _objectiveMarker,
                "NEXT DESTINATION",
                new Vector3(0f, 2.35f, 0f),
                RuntimeUi.Warning,
                96);
        }

        private void BuildHud069()
        {
            RuntimeUi.EnsureEventSystem();
            _hudCanvas = RuntimeUi.CreateCanvas("Walkable Guild Hall Minimal HUD 069");
            _hudCanvas.overrideSorting = true;
            _hudCanvas.sortingOrder = 120;
            var safe = RuntimeUi.AddSafeArea(_hudCanvas.transform);
            var root = RuntimeUi.AddStretchRect(safe, "Guild Hall HUD Root 069");
            var canExit = _callbacks != null && (_callbacks.Exit != null || _callbacks.Leave != null);
            _worldInput071 = _hudCanvas.gameObject.AddComponent<WorldInput071>();

            var location = RuntimeUi.AddPanel(root, "Guild Hall Location Plate 069",
                new Color(0.012f, 0.020f, 0.032f, 0.76f));
            Anchor069(location.rectTransform, new Vector2(0.024f, 0.910f), new Vector2(0.23f, 0.976f));
            _locationText = RuntimeUi.AddText(location.transform, "Guild Hall Location Text 069",
                "CHAPTER 1  •  GUILD HALL", 22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch069(_locationText.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));

            var objective = RuntimeUi.AddPanel(root, "Guild Hall Single Objective Panel 069",
                new Color(0.012f, 0.020f, 0.032f, 0.82f));
            Anchor069(objective.rectTransform, new Vector2(0.25f, 0.890f), new Vector2(0.976f, 0.976f));
            _objectiveText = RuntimeUi.AddText(objective.transform, "Guild Hall Next Objective Text 069",
                string.Empty, 28, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            Stretch069(_objectiveText.rectTransform, new Vector2(18f, 5f), new Vector2(-18f, -5f));

            var route = RuntimeUi.AddPanel(root, "Guild Hall Route Strip 073",
                new Color(0.012f, 0.020f, 0.032f, 0.90f));
            Anchor069(route.rectTransform, new Vector2(0.25f, 0.838f), new Vector2(0.976f, 0.883f));
            _routeText073 = RuntimeUi.AddText(route.transform, "Guild Hall Route Text 073",
                string.Empty, 20, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch069(_routeText073.rectTransform, new Vector2(14f, 3f), new Vector2(-14f, -3f));
            var routeTrack = RuntimeUi.AddPanel(route.transform, "Guild Hall Route Track 073",
                new Color(1f, 1f, 1f, 0.12f));
            Anchor069(routeTrack.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.11f));
            _routeFill073 = RuntimeUi.AddPanel(routeTrack.transform, "Guild Hall Route Fill 073",
                RuntimeUi.Warning);
            _routeFill073.raycastTarget = false;

            // Quick Play remains available to the host verification seam only.
            // Normal players enter practice through the physical Training Yard.
            _quickPlayButton070 = null;

            var controls = RuntimeUi.AddPanel(root, "Guild Hall Controls Plate 069",
                new Color(0.012f, 0.020f, 0.032f, 0.90f));
            _controlsPanel071 = controls;
            Anchor069(controls.rectTransform, new Vector2(0.024f, 0.018f), new Vector2(0.235f, 0.160f));
            _controlsText076 = RuntimeUi.AddText(controls.transform, "Guild Hall Controls Text 069",
                "MOVE  WASD / STICK  •  ROLL  SPACE / B\nACT  E / A" +
                (canExit ? "  •  BACK  ESC / START" : string.Empty),
                22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch069(_controlsText076.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));

            var progression = RuntimeUi.AddPanel(root, "Guild Hall Spendable XP Plate 073",
                new Color(0.012f, 0.020f, 0.032f, 0.92f));
            Anchor069(progression.rectTransform, new Vector2(0.77f, 0.018f), new Vector2(0.976f, 0.068f));
            _progressionText073 = RuntimeUi.AddText(progression.transform,
                "Guild Hall Spendable XP Text 073", "GUILD XP  —  •  SPENDABLE XP  —",
                17, TextAnchor.MiddleCenter, RuntimeUi.Positive, FontStyle.Bold);
            Stretch069(_progressionText073.rectTransform, new Vector2(8f, 3f), new Vector2(-8f, -3f));

            var prompt = RuntimeUi.AddPanel(root, "Guild Hall Interaction Prompt Plate 069",
                new Color(0.07f, 0.042f, 0.012f, 0.95f));
            _promptPanel071 = prompt;
            Anchor069(prompt.rectTransform, new Vector2(0.24f, 0.070f), new Vector2(0.76f, 0.15f));
            _promptText = RuntimeUi.AddText(prompt.transform, "Guild Hall Interaction Prompt Text 069",
                string.Empty, 29, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold);
            Stretch069(_promptText.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));
            _worldInput071.BuildTouchControls071(root, canExit, false);
            _worldInput071.SetTouchControlsVisibleForVerification071(
                Application.isMobilePlatform && Input.touchSupported);
            controls.gameObject.SetActive(!_worldInput071.TouchControlsVisible071);
        }

        private void InvokeDestination069(HallDestination069 destination)
        {
            if (destination == null || _interactionLocked || _shutdown) return;
            var callback = _callbacks != null ? _callbacks.Resolve069(destination.Id) : null;
            if (callback == null)
            {
                ShowTemporaryMessage069(destination.DisplayName + " is not available yet.");
                return;
            }
            var interactionCue = Resources.Load<AudioClip>(
                "SecondDimension/Audio/Battle011/UI/SFX_UI_SELECT");
            if (_hallInteractionAudio069 != null && interactionCue != null)
                _hallInteractionAudio069.PlayOneShot(interactionCue);
            InvokeHost069(callback);
        }

        private void InvokeQuickPlay070()
        {
            var callback = _callbacks != null ? _callbacks.QuickPlay : null;
            if (callback == null)
            {
                ShowTemporaryMessage069("Quick Play is unavailable until a Guild is loaded.");
                return;
            }
            InvokeHost069(callback);
        }

        private void InvokeHost069(Action callback)
        {
            if (callback == null || _interactionLocked || _shutdown) return;
            _interactionLocked = true;
            _worldMotor070?.StopImmediately070();
            try
            {
                callback.Invoke();
            }
            finally
            {
                if (!_shutdown && !_hostSuspended) _interactionLocked = false;
            }
        }

        private void RefreshObjectiveMarker069()
        {
            if (_objectiveMarker == null) return;
            if (!_destinationsById.TryGetValue(_objectiveDestinationId, out var destination) ||
                destination.Anchor == null)
            {
                _objectiveMarker.gameObject.SetActive(false);
                return;
            }
            _objectiveMarker.gameObject.SetActive(true);
            _objectiveMarker.position = destination.Anchor.position;
            if (_objectiveWorldText073 != null)
                _objectiveWorldText073.text = "NEXT  •  " + destination.DisplayName.ToUpperInvariant();
        }

        private void UpdateObjectiveText069()
        {
            if (_objectiveText == null) return;
            var destinationName = _destinationsById.TryGetValue(_objectiveDestinationId, out var destination)
                ? destination.DisplayName
                : "GUILD HALL";
            var distance = _founderRoot != null && destination?.Anchor != null
                ? HorizontalDistance069(_founderRoot.position, destination.Anchor.position)
                : 0f;
            _objectiveText.text = "NEXT  •  " + destinationName + "  •  " +
                                  Mathf.CeilToInt(distance) + " m  •  " +
                                  CompactLine069(_objectiveDescription, 24);
        }

        private void UpdateInteractionPrompt069()
        {
            if (_promptText == null || _promptPanel071 == null) return;
            if (!string.IsNullOrEmpty(_temporaryMessage) && Time.unscaledTime < _temporaryMessageUntil)
            {
                _promptText.text = _temporaryMessage;
                _promptPanel071.gameObject.SetActive(true);
                return;
            }
            _temporaryMessage = string.Empty;
            if (_interactionLocked)
            {
                _promptText.text = "OPENING...";
                _promptPanel071.gameObject.SetActive(true);
                return;
            }
            var nearest = NearestDestination069();
            if (nearest != null)
            {
                var action = _worldInput071 != null ? _worldInput071.InteractionPrompt071 : "E";
                var interactionTitle = nearest.InteractionTitle;
                if (StringComparer.Ordinal.Equals(nearest.Id, GuideDestinationId069) &&
                    !string.IsNullOrWhiteSpace(_objectiveDescription))
                    interactionTitle = "Mira: “" + CompactLine069(_objectiveDescription, 82) + "”";
                _promptText.text = action + "  •  " + interactionTitle;
                _promptPanel071.gameObject.SetActive(true);
                return;
            }
            if (_founderRoot != null &&
                _destinationsById.TryGetValue(_objectiveDestinationId, out var objective) &&
                objective?.Anchor != null)
            {
                var delta = objective.Anchor.position - _founderRoot.position;
                delta.y = 0f;
                _promptText.text = DirectionHint073(delta) + "  •  " +
                                   Mathf.CeilToInt(delta.magnitude) + " m TO " +
                                   objective.DisplayName.ToUpperInvariant();
                _promptPanel071.gameObject.SetActive(true);
                return;
            }
            _promptText.text = "FOLLOW THE GOLD DESTINATION";
            _promptPanel071.gameObject.SetActive(true);
        }

        private static string CompactLine069(string value, int maximumCharacters)
        {
            var compact = string.IsNullOrWhiteSpace(value)
                ? "Continue the Guild story."
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (compact.Contains("  ")) compact = compact.Replace("  ", " ");
            if (compact.Length <= maximumCharacters) return compact;
            return compact.Substring(0, Mathf.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private HallDestination069 NearestDestination069()
        {
            if (_founderRoot == null) return null;
            HallDestination069 nearest = null;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < _destinations.Count; index++)
            {
                var destination = _destinations[index];
                if (destination == null || destination.Anchor == null ||
                    !destination.Anchor.gameObject.activeInHierarchy) continue;
                var delta = destination.Anchor.position - _founderRoot.position;
                delta.y = 0f;
                var distance = delta.magnitude;
                if (distance <= destination.Radius && distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = destination;
                }
            }
            return nearest;
        }

        private void UpdateCamera069(float deltaTime)
        {
            if (_worldCamera == null || _founderRoot == null) return;
            var targetX = Mathf.Clamp(_founderRoot.position.x, -7.95f, 7.95f);
            var desired = new Vector3(targetX, 4.45f, -14.35f);
            _worldCamera.transform.position = Vector3.SmoothDamp(
                _worldCamera.transform.position,
                desired,
                ref _cameraVelocity,
                0.18f,
                80f,
                Mathf.Max(0.001f, deltaTime));
            _worldCamera.transform.LookAt(new Vector3(
                _worldCamera.transform.position.x,
                4.45f,
                0.55f));
        }

        private void UpdateWorldPresentation069()
        {
            if (_worldCamera == null) return;
            UpdateProgressionSummary073();
            for (var index = 0; index < _billboards.Count; index++)
            {
                var billboard = _billboards[index];
                if (billboard == null) continue;
                if (billboard == _founderBillboard070 && _dodgeRemaining > 0f)
                {
                    var progress = 1f - _dodgeRemaining / DodgeDuration069;
                    billboard.rotation = _worldCamera.transform.rotation * Quaternion.Euler(0f, 0f, progress * 360f);
                }
                else billboard.rotation = _worldCamera.transform.rotation;
            }

            var nearest069 = NearestDestination069();
            for (var index = 0; index < _destinations.Count; index++)
            {
                var destination069 = _destinations[index];
                var showStationGuidance071 =
                    StringComparer.Ordinal.Equals(destination069.Id, _objectiveDestinationId) ||
                    destination069 == nearest069;
                var highlight071 = destination069.AreaHighlight;
                if (highlight071 == null) continue;
                highlight071.gameObject.SetActive(showStationGuidance071);
                var localPulse071 = 1f + Mathf.Sin(Time.unscaledTime * 2.0f + index) * 0.035f;
                highlight071.localScale = destination069.AreaHighlightBaseScale * localPulse071;
                if (destination069.AreaHighlightRenderer != null)
                {
                    var alpha071 = destination069 == nearest069 ? 0.20f : 0.14f;
                    var accent071 = destination069.Accent;
                    destination069.AreaHighlightRenderer.color = new Color(
                        accent071.r,
                        accent071.g,
                        accent071.b,
                        alpha071);
                }
            }
            if (_objectivePulse != null)
            {
                var pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.08f;
                _objectivePulse.localScale = new Vector3(4.84f * pulse, 3.125f * pulse, 1f);
            }
            if (_founderRoot != null &&
                _destinationsById.TryGetValue(_objectiveDestinationId, out var target073) &&
                target073?.Anchor != null)
            {
                var delta073 = target073.Anchor.position - _founderRoot.position;
                delta073.y = 0f;
                var fullDistance073 = Mathf.Max(0.1f,
                    HorizontalDistance069(InitialFounderPosition069, target073.Anchor.position));
                var completion073 = 1f - Mathf.Clamp01(delta073.magnitude / fullDistance073);
                if (_routeText073 != null)
                    _routeText073.text = "YOU  •  " + DirectionHint073(delta073) + "  •  " +
                                         Mathf.CeilToInt(delta073.magnitude) + " m  •  " +
                                         target073.DisplayName.ToUpperInvariant();
                if (_routeFill073 != null)
                {
                    var fill073 = _routeFill073.rectTransform;
                    fill073.anchorMin = Vector2.zero;
                    fill073.anchorMax = new Vector2(completion073, 1f);
                    fill073.offsetMin = Vector2.zero;
                    fill073.offsetMax = Vector2.zero;
                }
                UpdateObjectiveText069();
            }
            if (_controlsPanel071 != null)
            {
                var touchVisible071 = _worldInput071 != null &&
                                      _worldInput071.TouchControlsVisible071;
                _controlsPanel071.gameObject.SetActive(!touchVisible071);
            }
            UpdateFounderGrounding071();
        }

        private void UpdateFounderGrounding071()
        {
            if (_founderRoot == null || _founderBillboard070 == null) return;
            var depth071 = Mathf.InverseLerp(
                HallLaneMinimumZ069(_founderRoot.position.x),
                HallLaneMaximumZ069(_founderRoot.position.x),
                _founderRoot.position.z);
            var perspectiveScale071 = Mathf.Lerp(1.05f, 0.84f, depth071);
            _founderBillboard070.localScale = Vector3.one * perspectiveScale071;
            if (_founderShadow071 != null)
                _founderShadow071.localScale = new Vector3(
                    1.84375f * perspectiveScale071,
                    1.50f * perspectiveScale071,
                    1f);
        }

        private void UpdateProgressionSummary073()
        {
            if (_progressionText073 == null) return;
            var summary = _progressionSummaryProvider073?.Invoke();
            _progressionText073.text = string.IsNullOrWhiteSpace(summary)
                ? "GUILD XP  0 / 100  •  XP TO SPEND  0"
                : summary;
        }

        private void FaceFounderBillboardToCamera070()
        {
            if (_founderBillboard070 == null || _worldCamera == null) return;
            _founderBillboard070.rotation = _worldCamera.transform.rotation;
        }

        private void ShowTemporaryMessage069(string message)
        {
            _temporaryMessage = message ?? string.Empty;
            _temporaryMessageUntil = Time.unscaledTime + 2.4f;
            UpdateInteractionPrompt069();
        }

        private Transform CreateSpriteObject069(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            float worldHeight,
            int sortingOrder)
        {
            var artObject = new GameObject(name);
            artObject.transform.SetParent(parent, false);
            artObject.transform.localPosition = localPosition;
            artObject.layer = WorldLayer069;
            var renderer = artObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            var spriteHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
            artObject.transform.localScale = Vector3.one * (worldHeight / spriteHeight);
            return artObject.transform;
        }

        private TextMesh CreateWorldLabel073(
            Transform parent,
            string text,
            Vector3 localPosition,
            Color color,
            int sortingOrder)
        {
            var labelObject = new GameObject("Readable Hall World Label 073");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.layer = WorldLayer069;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = text ?? string.Empty;
            label.anchor = TextAnchor.LowerCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 72;
            label.characterSize = 0.030f;
            label.fontStyle = FontStyle.Bold;
            label.color = color;
            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = sortingOrder;
            _billboards.Add(labelObject.transform);
            return label;
        }

        private static float HorizontalDistance069(Vector3 left, Vector3 right)
        {
            var delta = left - right;
            delta.y = 0f;
            return delta.magnitude;
        }

        private static string DirectionHint073(Vector3 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.z))
                return delta.x >= 0f ? "GO RIGHT  →" : "GO LEFT  ←";
            return delta.z >= 0f ? "GO UPSTAGE  ↑" : "GO DOWNSTAGE  ↓";
        }

        private Transform CreateSoftFloorSprite069(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector2 worldSize,
            Color color,
            int sortingOrder)
        {
            var sprite = SoftEllipseSprite069();
            if (sprite == null || parent == null) return null;
            var floorObject = new GameObject(name);
            floorObject.transform.SetParent(parent, false);
            floorObject.transform.localPosition = localPosition;
            floorObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            floorObject.layer = WorldLayer069;
            var renderer = floorObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            floorObject.transform.localScale = new Vector3(
                worldSize.x / Mathf.Max(0.01f, sprite.bounds.size.x),
                worldSize.y / Mathf.Max(0.01f, sprite.bounds.size.y),
                1f);
            return floorObject.transform;
        }

        private Sprite SoftEllipseSprite069()
        {
            if (_softEllipseSprite071 != null) return _softEllipseSprite071;
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = "Guild Hall Soft Ground Ellipse 071",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalizedX = (x + 0.5f) / width * 2f - 1f;
                    var normalizedY = (y + 0.5f) / height * 2f - 1f;
                    var radius = Mathf.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    var alpha = Mathf.Pow(Mathf.Clamp01(1f - radius), 1.65f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply(false, true);
            _ownedRuntimeTextures.Add(texture);
            _softEllipseSprite071 = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            _softEllipseSprite071.name = "Guild Hall Soft Ground Ellipse Sprite 071";
            _ownedRuntimeSprites.Add(_softEllipseSprite071);
            return _softEllipseSprite071;
        }

        private Sprite LoadSprite069(string resourceKey, Vector2 pivot)
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) return null;
            var imported = Resources.Load<Sprite>(resourceKey);
            if (imported != null) return imported;
            var texture = Resources.Load<Texture2D>(resourceKey);
            if (texture == null) return null;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), pivot, 100f,
                0u, SpriteMeshType.FullRect);
            sprite.name = texture.name + "_RUNTIME_069";
            _ownedRuntimeSprites.Add(sprite);
            return sprite;
        }

        private GameObject CreatePrimitive069(
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool collider)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(_worldRoot.transform, false);
            primitive.transform.position = position;
            primitive.transform.localScale = scale;
            SetLayer069(primitive.transform);
            var primitiveCollider = primitive.GetComponent<Collider>();
            if (primitiveCollider != null) primitiveCollider.enabled = collider;
            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = Material069(color);
            return primitive;
        }

        private Material Material069(Color color)
        {
            var color32 = (Color32)color;
            var key = color32.r + ":" + color32.g + ":" + color32.b + ":" + color32.a;
            if (_materials.TryGetValue(key, out var existing) && existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Standard");
            if (shader == null)
                throw new InvalidOperationException("No supported runtime shader is available for the Guild Hall.");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            _materials[key] = material;
            return material;
        }

        private static string NormalizeDestinationId069(string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? GuideDestinationId069
                : value.Trim().ToUpperInvariant();
            if (StringComparer.Ordinal.Equals(normalized, "APPLICANTS")) normalized = RecruitmentDestinationId069;
            else if (StringComparer.Ordinal.Equals(normalized, "INVENTORY")) normalized = ArmoryDestinationId069;
            else if (StringComparer.Ordinal.Equals(normalized, "UNION") ||
                     StringComparer.Ordinal.Equals(normalized, "UNIONS")) normalized = PartyDestinationId069;
            else if (StringComparer.Ordinal.Equals(normalized, "CONTRACTS") ||
                     StringComparer.Ordinal.Equals(normalized, "QUEST")) normalized = ContractDestinationId069;
            else if (StringComparer.Ordinal.Equals(normalized, "QUICK_BATTLE") ||
                     StringComparer.Ordinal.Equals(normalized, "BATTLE")) normalized = PracticeDestinationId069;
            for (var index = 0; index < DestinationOrder069.Length; index++)
                if (StringComparer.Ordinal.Equals(DestinationOrder069[index], normalized)) return normalized;
            return GuideDestinationId069;
        }

        private static void SetLayer069(Transform root)
        {
            root.gameObject.layer = WorldLayer069;
            for (var index = 0; index < root.childCount; index++) SetLayer069(root.GetChild(index));
        }

        private static void Anchor069(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch069(RectTransform rect, Vector2 minimumOffset, Vector2 maximumOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimumOffset;
            rect.offsetMax = maximumOffset;
        }

        public void Shutdown069()
        {
            if (_shutdown && _worldRoot == null && _hudCanvas == null) return;
            _shutdown = true;
            StopAllCoroutines();
            if (_controller != null) _controller.enabled = false;
            if (_worldCamera != null) _worldCamera.enabled = false;
            if (_worldRoot != null)
            {
                _worldRoot.SetActive(false);
                DestroyOwnedObject069(_worldRoot);
            }
            if (_hudCanvas != null)
            {
                _hudCanvas.gameObject.SetActive(false);
                DestroyOwnedObject069(_hudCanvas.gameObject);
            }
            foreach (var material in _materials.Values)
                if (material != null) DestroyOwnedObject069(material);
            for (var index = 0; index < _ownedRuntimeSprites.Count; index++)
                if (_ownedRuntimeSprites[index] != null) DestroyOwnedObject069(_ownedRuntimeSprites[index]);
            for (var index = 0; index < _ownedRuntimeTextures.Count; index++)
                if (_ownedRuntimeTextures[index] != null) DestroyOwnedObject069(_ownedRuntimeTextures[index]);

            _materials.Clear();
            _ownedRuntimeSprites.Clear();
            _ownedRuntimeTextures.Clear();
            _billboards.Clear();
            _destinations.Clear();
            _destinationsById.Clear();
            _worldRoot = null;
            _hudCanvas = null;
            _worldCamera = null;
            _hallAmbience069 = null;
            _hallInteractionAudio069 = null;
            _controller = null;
            _worldMotor070 = null;
            _worldAnimator070 = null;
            _worldInput071 = null;
            _founderRoot = null;
            _founderBillboard070 = null;
            _founderVisual = null;
            _founderShadow071 = null;
            _guideShadow071 = null;
            _objectiveMarker = null;
            _objectivePulse = null;
            _softEllipseSprite071 = null;
            _objectiveText = null;
            _objectiveWorldText073 = null;
            _routeText073 = null;
            _routeFill073 = null;
            _promptText = null;
            _promptPanel071 = null;
            _locationText = null;
            _progressionText073 = null;
            _quickPlayButton070 = null;
            _callbacks = null;
            _objectiveProvider = null;
            _progressionSummaryProvider073 = null;
            _hostSuspended = false;
            UsesAuthoredHallArt069 = false;
            UsesFounderStandee069 = false;
            FounderStandeeResourceKeyForVerification076 = string.Empty;
            FounderPoseResourceRootForVerification076 = string.Empty;
        }

        private void DestroyOwnedObject069(UnityEngine.Object ownedObject)
        {
            if (ownedObject == null) return;
            if (Application.isPlaying) Destroy(ownedObject);
            else DestroyImmediate(ownedObject);
        }

        private void OnDisable()
        {
            Shutdown069();
        }

        private void OnDestroy()
        {
            Shutdown069();
        }
    }

}
