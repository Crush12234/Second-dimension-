using System;
using System.Collections.Generic;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.FirstHour071
{
    /// <summary>
    /// The first playable two minutes of a new Guild. The player crosses Skyhome
    /// Market beside Maren, learns the shared world controls, and reaches the Guild
    /// Hall before any charter or roster menu appears.
    /// </summary>
    public sealed class WalkableSkyhomeArrival071 : MonoBehaviour
    {
        private const int WorldLayer071 = 25;
        private const float MinimumX071 = -12.2f;
        private const float MaximumX071 = 12.2f;
        private const float MinimumZ071 = -2.15f;
        private const float MaximumZ071 = 1.55f;
        private const float HallInteractionRadius071 = 2.35f;
        private const string MarenPortraitResource071 =
            "SecondDimension/Art/Portraits/Recruits/SIGREC_MAREN_HOLT";

        private static readonly Vector3 PlayerStart071 = new Vector3(-10.8f, 0.04f, -0.85f);
        private static readonly Vector3 HallDoor071 = new Vector3(10.65f, 0.04f, 0.28f);

        private readonly List<Sprite> _ownedSprites071 = new List<Sprite>();
        private readonly List<Texture2D> _ownedTextures071 = new List<Texture2D>();
        private readonly List<Material> _ownedMaterials071 = new List<Material>();
        private readonly List<Transform> _billboards071 = new List<Transform>();

        private Action _reachHall071;
        private Action _returnToTitle071;
        private GameObject _worldRoot071;
        private Camera _camera071;
        private Canvas _hud071;
        private CharacterController _controller071;
        private WorldCharacterMotor070 _motor071;
        private WorldCharacterAnimator070 _animator071;
        private WorldInput071 _input071;
        private Transform _player071;
        private Transform _playerBillboard071;
        private Transform _playerVisual071;
        private Transform _playerShadow071;
        private Transform _maren071;
        private Transform _marenBillboard071;
        private Transform _marenVisual071;
        private Transform _marenShadow071;
        private CharacterController _marenController071;
        private WorldCharacterMotor070 _marenMotor071;
        private WorldCharacterAnimator070 _marenAnimator071;
        private Transform _objectiveMarker071;
        private Transform _objectivePulse071;
        private Sprite _softEllipseSprite071;
        private Text _objectiveText071;
        private Text _routeText071;
        private Image _routeFill071;
        private Text _promptText071;
        private Image _promptPanel071;
        private Image _controlsPanel071;
        private Text _controlsText071;
        private RectTransform _storyCard071;
        private Image _storyPortrait071;
        private Image _storyProgressFill071;
        private Text _storySpeakerText071;
        private Text _storyText071;
        private Text _storyProgressText071;
        private Vector3 _cameraVelocity071;
        private int _currentStoryBeat071;
        private bool _midpointBeatShown071;
        private bool _doorBeatShown071;
        private bool _interactionLocked071;
        private bool _shutdown071 = true;

        public bool IsActive071 => !_shutdown071 && _worldRoot071 != null && _worldRoot071.activeInHierarchy;
        public bool UsesAuthoredSkyhomeArt071 { get; private set; }
        public bool HasMarenCompanion071 => _maren071 != null && _marenVisual071 != null;
        public bool UsesCrossPlatformWorldInput071 => _input071 != null;
        public bool HasTouchControls071 => _input071 != null && _input071.TouchControlsBuilt071;
        public Transform ControlledAvatar071 => _player071;
        public Camera WorldCamera071 => _camera071;
        public Canvas HudCanvas071 => _hud071;
        public bool UsesAuthoredMarenPortrait071 { get; private set; }
        public bool UsesAuthoredGuildmasterStandee076 { get; private set; }
        public string GuildmasterStandeeResourceKeyForVerification076 { get; private set; } =
            string.Empty;
        public string GuildmasterPoseResourceRootForVerification076 { get; private set; } =
            string.Empty;
        public RectTransform StoryDialogueCard071 => _storyCard071;
        public Text StoryProgressTextForVerification076 => _storyProgressText071;
        public Text DesktopControlsTextForVerification076 => _controlsText071;
        public Image DesktopControlsPanelForVerification076 => _controlsPanel071;
        public string StorySpeaker071 => _storySpeakerText071 != null
            ? _storySpeakerText071.text
            : string.Empty;
        public string StoryBody071 => _storyText071 != null ? _storyText071.text : string.Empty;
        public string StoryProgress071 => _storyProgressText071 != null
            ? _storyProgressText071.text
            : string.Empty;
        public int CurrentStoryBeat071 => _currentStoryBeat071;
        /// <summary>
        /// Read-only certification telemetry for the same Hall objective shown to
        /// the player. The direction is expressed in the X/Z input plane used by
        /// ApplyMovementForVerification071; reading it never moves the avatar.
        /// </summary>
        public Vector3 CurrentObjectivePositionForVerification076 => HallDoor071;
        public float CurrentObjectiveDistanceForVerification076 => DistanceToGuildHall071;
        public float CurrentObjectiveInteractionRadiusForVerification076 =>
            HallInteractionRadius071;
        public bool IsWithinCurrentObjectiveInteractionRangeForVerification076 =>
            CurrentObjectiveDistanceForVerification076 <=
            CurrentObjectiveInteractionRadiusForVerification076;
        public Vector2 CurrentObjectiveDirectionForVerification076
        {
            get
            {
                if (_shutdown071 || _player071 == null) return Vector2.zero;
                var delta071 = HallDoor071 - _player071.position;
                var direction071 = new Vector2(delta071.x, delta071.z);
                return direction071.sqrMagnitude > 0.000001f
                    ? direction071.normalized
                    : Vector2.zero;
            }
        }
        public float DistanceToGuildHall071
        {
            get
            {
                if (_player071 == null) return float.PositiveInfinity;
                var delta = HallDoor071 - _player071.position;
                delta.y = 0f;
                return delta.magnitude;
            }
        }

        public void Begin071(Action reachHall, Action returnToTitle)
        {
            Shutdown071();
            _shutdown071 = false;
            enabled = true;
            _reachHall071 = reachHall;
            _returnToTitle071 = returnToTitle;
            _interactionLocked071 = false;
            _midpointBeatShown071 = false;
            _doorBeatShown071 = false;
            _currentStoryBeat071 = 0;

            BuildWorld071();
            // Runtime primitives are positioned and scaled after their colliders
            // are created. This project deliberately disables automatic transform
            // syncing, so publish the complete Market geometry before the player
            // can provide same-frame input. Otherwise PhysX can briefly retain the
            // walkable floor as a unit cube at the origin and block the route.
            Physics.SyncTransforms();
            BuildHud071();
            UpdateCamera071(1f);
            UpdateMaren071(1f);
            UpdatePresentation071();
            UpdateGuidance071();
        }

        private void Update()
        {
            if (_shutdown071 || _controller071 == null || !_controller071.enabled) return;
            var frame071 = _input071 != null
                ? _input071.CaptureFrame071()
                : WorldInput071.CaptureLegacyFrame071();

            if (frame071.BackPressed071 && _returnToTitle071 != null)
            {
                InvokeHost071(_returnToTitle071);
                return;
            }

            _motor071?.SetInput070(
                _interactionLocked071 ? Vector2.zero : frame071.Movement071,
                !_interactionLocked071 && frame071.RunHeld071,
                !_interactionLocked071 && frame071.DodgePressed071,
                false);

            UpdateCamera071(Time.unscaledDeltaTime);
            UpdateMaren071(Time.unscaledDeltaTime);
            UpdatePresentation071();
            UpdateStoryBeats071();
            UpdateGuidance071();

            if (!_interactionLocked071 && frame071.InteractPressed071)
                ApplyInteraction071();
        }

        private void LateUpdate()
        {
            if (_shutdown071 || _player071 == null || _controller071 == null) return;
            var current071 = _player071.position;
            var laneCenter071 = MarketLaneCenterZ071(current071.x);
            var laneHalfWidth071 = MarketLaneHalfWidth071(current071.x);
            var clamped071 = new Vector3(
                Mathf.Clamp(current071.x, MinimumX071, MaximumX071),
                PlayerStart071.y,
                Mathf.Clamp(
                    current071.z,
                    Mathf.Max(MinimumZ071, laneCenter071 - laneHalfWidth071),
                    Mathf.Min(MaximumZ071, laneCenter071 + laneHalfWidth071)));
            var correction071 = clamped071 - current071;
            if (correction071.sqrMagnitude > 0.000001f) _controller071.Move(correction071);
            ClampMarenToMarketLane071();
            UpdateActorGrounding071();
            FaceBillboards071();
        }

        private void ClampMarenToMarketLane071()
        {
            if (_maren071 == null || _marenController071 == null || !_marenController071.enabled) return;
            var current071 = _maren071.position;
            var clampedX071 = Mathf.Clamp(current071.x, MinimumX071, MaximumX071);
            var laneCenter071 = MarketLaneCenterZ071(clampedX071);
            var laneHalfWidth071 = MarketLaneHalfWidth071(clampedX071);
            var clamped071 = new Vector3(
                clampedX071,
                PlayerStart071.y,
                Mathf.Clamp(
                    current071.z,
                    Mathf.Max(MinimumZ071, laneCenter071 - laneHalfWidth071),
                    Mathf.Min(MaximumZ071, laneCenter071 + laneHalfWidth071)));
            var correction071 = clamped071 - current071;
            if (correction071.sqrMagnitude > 0.000001f) _marenController071.Move(correction071);
        }

        private static float MarketLaneCenterZ071(float worldX071)
        {
            var progress071 = Mathf.InverseLerp(MinimumX071, MaximumX071, worldX071);
            return Mathf.Lerp(-0.92f, 0.46f, progress071);
        }

        private static float MarketLaneHalfWidth071(float worldX071)
        {
            var progress071 = Mathf.InverseLerp(MinimumX071, MaximumX071, worldX071);
            return Mathf.Lerp(1.18f, 0.68f, progress071);
        }

        public void ApplyMovementForVerification071(
            Vector2 movement,
            bool run,
            bool dodge,
            float deltaTime)
        {
            if (_shutdown071 || _motor071 == null || _interactionLocked071) return;
            _motor071.Simulate070(movement, run, dodge, false, deltaTime);
            LateUpdate();
            UpdateCamera071(Mathf.Max(0.001f, deltaTime));
            UpdateMaren071(Mathf.Max(0.001f, deltaTime));
            UpdateStoryBeats071();
            UpdateGuidance071();
        }

        public bool TeleportToGuildHallForVerification071()
        {
            if (_player071 == null) return false;
            _player071.position = HallDoor071 + new Vector3(-1.05f, 0f, -0.25f);
            UpdateCamera071(1f);
            UpdateMaren071(1f);
            UpdateStoryBeats071();
            UpdateGuidance071();
            return true;
        }

        public void ApplyInteractionForVerification071() => ApplyInteraction071();

        private void ApplyInteraction071()
        {
            if (_shutdown071 || _interactionLocked071) return;
            if (DistanceToGuildHall071 > HallInteractionRadius071)
            {
                if (_storyText071 != null)
                    _storyText071.text = "The Hall doors are ahead. Stay with me.";
                if (_storyCard071 != null)
                {
                    _storyCard071.gameObject.SetActive(true);
                }
                return;
            }
            InvokeHost071(_reachHall071);
        }

        private void InvokeHost071(Action callback)
        {
            if (callback == null || _interactionLocked071 || _shutdown071) return;
            _interactionLocked071 = true;
            _motor071?.StopImmediately070();
            callback.Invoke();
        }

        private void BuildWorld071()
        {
            _worldRoot071 = new GameObject("Walkable Skyhome Market Arrival 071");
            _worldRoot071.layer = WorldLayer071;
            BuildCamera071();
            BuildPaintedSkyhome071();
            BuildWalkableFloor071();
            BuildPlayer071();
            BuildMaren071();
            BuildMarketLife071();
            BuildGuildHallMarker071();
        }

        private void BuildCamera071()
        {
            var cameraObject071 = new GameObject("Skyhome Market Following Camera 071");
            cameraObject071.transform.SetParent(_worldRoot071.transform, false);
            cameraObject071.layer = WorldLayer071;
            _camera071 = cameraObject071.AddComponent<Camera>();
            _camera071.clearFlags = CameraClearFlags.SolidColor;
            _camera071.backgroundColor = new Color(0.44f, 0.66f, 0.82f, 1f);
            // The Market is a navigation space, not a camera showcase. A level
            // orthographic frame keeps the authored plate edge-to-edge and prevents
            // the blue void/tilted-postcard failure seen in the previous build.
            _camera071.orthographic = true;
            _camera071.orthographicSize = 5.15f;
            _camera071.fieldOfView = 52f;
            _camera071.nearClipPlane = 0.15f;
            _camera071.farClipPlane = 90f;
            _camera071.cullingMask = 1 << WorldLayer071;
            _camera071.depth = 90f;
            _camera071.transform.position = new Vector3(-7.0f, 4.35f, -14.25f);
            _camera071.transform.LookAt(new Vector3(-7.0f, 4.35f, 0.65f));
        }

        private void BuildPaintedSkyhome071()
        {
            var sprite071 = LoadSprite071(
                "SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071",
                new Vector2(0.5f, 0.5f));
            if (sprite071 == null)
                sprite071 = LoadSprite071(
                    "SecondDimension/Art/Guided063/TITLE_SKYHOME_DAWN_V63",
                    new Vector2(0.5f, 0.5f));
            if (sprite071 == null) return;

            var backdrop071 = new GameObject("Authored Skyhome Market Backdrop 071");
            backdrop071.transform.SetParent(_worldRoot071.transform, false);
            backdrop071.transform.position = new Vector3(0f, 6.25f, 5.25f);
            backdrop071.layer = WorldLayer071;
            var renderer071 = backdrop071.AddComponent<SpriteRenderer>();
            renderer071.sprite = sprite071;
            renderer071.sortingOrder = -100;
            var width071 = Mathf.Max(0.01f, sprite071.bounds.size.x);
            backdrop071.transform.localScale = Vector3.one * (44.0f / width071);
            UsesAuthoredSkyhomeArt071 = true;
        }

        private void BuildWalkableFloor071()
        {
            var floor071 = CreatePrimitive071(
                PrimitiveType.Cube,
                "Skyhome Market Invisible Walkable Street 071",
                new Vector3(0f, -0.30f, -0.10f),
                new Vector3(25.4f, 0.60f, 4.4f),
                new Color(0.16f, 0.12f, 0.08f, 1f),
                true);
            var renderer071 = floor071.GetComponent<Renderer>();
            if (renderer071 != null) renderer071.enabled = !UsesAuthoredSkyhomeArt071;

            CreateInvisibleBoundary071("Skyhome Market Left Boundary 071",
                new Vector3(MinimumX071 - 0.35f, 1f, -0.1f), new Vector3(0.4f, 2f, 5f));
            CreateInvisibleBoundary071("Skyhome Market Right Boundary 071",
                new Vector3(MaximumX071 + 0.35f, 1f, -0.1f), new Vector3(0.4f, 2f, 5f));
        }

        private void BuildPlayer071()
        {
            var playerObject071 = new GameObject("Controlled New Guildmaster 071");
            playerObject071.transform.SetParent(_worldRoot071.transform, false);
            playerObject071.transform.position = PlayerStart071;
            playerObject071.layer = WorldLayer071;
            _player071 = playerObject071.transform;

            _controller071 = playerObject071.AddComponent<CharacterController>();
            _controller071.radius = 0.38f;
            _controller071.height = 1.75f;
            _controller071.center = new Vector3(0f, 0.88f, 0f);
            _controller071.stepOffset = 0.28f;
            _controller071.skinWidth = 0.05f;

            var pivot071 = new GameObject("Guildmaster Camera Facing Pivot 071").transform;
            pivot071.SetParent(_player071, false);
            pivot071.gameObject.layer = WorldLayer071;
            _playerBillboard071 = pivot071;
            GuildmasterStandeeResourceKeyForVerification076 =
                M1VisualAssets.GuildmasterStandeeResourceKey076;
            var sprite071 = LoadSprite071(
                GuildmasterStandeeResourceKeyForVerification076,
                new Vector2(0.5f, 0.02f));
            UsesAuthoredGuildmasterStandee076 = sprite071 != null;
            _playerVisual071 = sprite071 != null
                ? CreateSpriteObject071("Guildmaster Standee 071", pivot071, sprite071, Vector3.zero, 3.75f, 30)
                : CreateFallbackCharacter071("Guildmaster Fallback 071", pivot071,
                    new Color(0.12f, 0.58f, 0.78f));
            _billboards071.Add(pivot071);
            CreateWorldLabel071(pivot071, "YOU", new Vector3(0f, 4.0f, 0f), RuntimeUi.Accent, 90);
            _playerShadow071 = CreateSoftFloorSprite071(
                _player071,
                "Guildmaster Grounded Contact Shadow 071",
                new Vector3(0f, 0.026f, 0.04f),
                new Vector2(1.18f, 0.48f),
                new Color(0.025f, 0.020f, 0.024f, 0.34f),
                18);

            _motor071 = playerObject071.AddComponent<WorldCharacterMotor070>();
            _motor071.Configure070(new WorldCharacterMotorTuning070
            {
                WalkSpeed = 5.4f,
                RunSpeed = 8.4f,
                DodgeSpeed = 13.0f,
                DodgeDuration = 0.36f,
                Acceleration = 22f,
                Braking = 28f,
                TurnSpeedDegrees = 760f
            }, driveFromLegacyKeyboard: false);
            _animator071 = playerObject071.AddComponent<WorldCharacterAnimator070>();
            _animator071.Configure070(
                _motor071,
                _playerVisual071,
                automaticTick: true,
                proceduralFallback: true);
            GuildmasterPoseResourceRootForVerification076 =
                M1VisualAssets.GuildmasterPoseResourceRoot076;
            _animator071.ConfigureSpritePoses070(GuildmasterPoseResourceRootForVerification076);
        }

        private void BuildMaren071()
        {
            _maren071 = new GameObject("Maren Holt Walking Companion 071").transform;
            _maren071.SetParent(_worldRoot071.transform, false);
            _maren071.position = PlayerStart071 + new Vector3(-1.35f, 0f, 0.72f);
            _maren071.gameObject.layer = WorldLayer071;
            _marenController071 = _maren071.gameObject.AddComponent<CharacterController>();
            _marenController071.radius = 0.30f;
            _marenController071.height = 1.68f;
            _marenController071.center = new Vector3(0f, 0.84f, 0f);
            _marenController071.stepOffset = 0.24f;
            _marenController071.skinWidth = 0.045f;
            // Maren is a companion, not a movable obstacle. Keep her grounded by
            // the same world collision while allowing the player to pass cleanly.
            if (_controller071 != null)
                Physics.IgnoreCollision(_controller071, _marenController071, true);
            _marenBillboard071 = new GameObject("Maren Holt Camera Facing Pivot 071").transform;
            _marenBillboard071.SetParent(_maren071, false);
            _marenBillboard071.gameObject.layer = WorldLayer071;
            var sprite071 = LoadSprite071(
                "SecondDimension/Art/Battle/STANDEE_SIGREC_MAREN_HOLT",
                new Vector2(0.5f, 0.02f));
            _marenVisual071 = sprite071 != null
                ? CreateSpriteObject071("Maren Holt Companion Standee 071", _marenBillboard071,
                    sprite071, Vector3.zero, 3.45f, 29)
                : CreateFallbackCharacter071("Maren Holt Fallback 071", _marenBillboard071,
                    new Color(0.82f, 0.54f, 0.20f));
            _billboards071.Add(_marenBillboard071);
            CreateWorldLabel071(_marenBillboard071, "MAREN", new Vector3(0f, 3.65f, 0f), RuntimeUi.Text, 72);
            _marenShadow071 = CreateSoftFloorSprite071(
                _maren071,
                "Maren Holt Grounded Contact Shadow 071",
                new Vector3(0f, 0.026f, 0.04f),
                new Vector2(1.12f, 0.45f),
                new Color(0.025f, 0.020f, 0.024f, 0.31f),
                17);
            _marenMotor071 = _maren071.gameObject.AddComponent<WorldCharacterMotor070>();
            _marenMotor071.Configure070(new WorldCharacterMotorTuning070
            {
                WalkSpeed = 4.9f,
                RunSpeed = 7.5f,
                DodgeSpeed = 10.5f,
                DodgeDuration = 0.32f,
                Acceleration = 17f,
                Braking = 24f,
                TurnSpeedDegrees = 680f
            }, driveFromLegacyKeyboard: false);
            _marenAnimator071 = _maren071.gameObject.AddComponent<WorldCharacterAnimator070>();
            _marenAnimator071.Configure070(
                _marenMotor071,
                _marenVisual071,
                automaticTick: true,
                proceduralFallback: true);
            _marenAnimator071.ConfigureSpritePoses070(
                "SecondDimension/Art/Battle011/Characters/SIGREC_MAREN_HOLT");
        }

        private void BuildMarketLife071()
        {
            CreateMarketNpc071(
                "Skyhome Market Porter 071",
                "SecondDimension/Art/Battle/STANDEE_PROC_F85A4CAA747BC8C6",
                new Vector3(-3.8f, 0.02f, 0.82f),
                8);
            CreateMarketNpc071(
                "Skyhome Lantern Keeper 071",
                "SecondDimension/Art/Battle/STANDEE_PROC_748DD03A23E1FEB0",
                new Vector3(4.4f, 0.02f, 0.96f),
                8);
        }

        private void CreateMarketNpc071(string name, string resourceKey, Vector3 position, int sortingOrder)
        {
            var root071 = new GameObject(name).transform;
            root071.SetParent(_worldRoot071.transform, false);
            root071.position = position;
            root071.gameObject.layer = WorldLayer071;
            var billboard071 = new GameObject(name + " Camera Facing Pivot").transform;
            billboard071.SetParent(root071, false);
            billboard071.gameObject.layer = WorldLayer071;
            var sprite071 = LoadSprite071(resourceKey, new Vector2(0.5f, 0.02f));
            if (sprite071 != null)
                CreateSpriteObject071(name + " Standee", billboard071, sprite071, Vector3.zero, 2.90f, sortingOrder);
            CreateSoftFloorSprite071(
                root071,
                name + " Contact Shadow",
                new Vector3(0f, 0.024f, 0.04f),
                new Vector2(1.02f, 0.40f),
                new Color(0.025f, 0.020f, 0.024f, 0.24f),
                sortingOrder - 1);
            root071.localScale = Vector3.one * MarketPerspectiveScale071(position.x);
            _billboards071.Add(billboard071);
        }

        private void BuildGuildHallMarker071()
        {
            _objectiveMarker071 = new GameObject("Skyhome Guild Hall Threshold Anchor 071").transform;
            _objectiveMarker071.SetParent(_worldRoot071.transform, false);
            _objectiveMarker071.position = HallDoor071;
            _objectiveMarker071.gameObject.layer = WorldLayer071;

            _objectivePulse071 = CreateSoftFloorSprite071(
                _objectiveMarker071,
                "Warm Guild Hall Threshold Light 071",
                new Vector3(0f, 0.03f, 0f),
                new Vector2(2.30f, 0.82f),
                new Color(1f, 0.64f, 0.16f, 0.20f),
                7);
            var labelPivot071 = new GameObject("Guild Hall Destination Billboard 073").transform;
            labelPivot071.SetParent(_objectiveMarker071, false);
            labelPivot071.gameObject.layer = WorldLayer071;
            CreateWorldLabel071(
                labelPivot071,
                "NEXT  •  GUILD HALL\nENTER HERE",
                new Vector3(0f, 2.15f, 0f),
                RuntimeUi.Warning,
                92);
            _billboards071.Add(labelPivot071);
        }

        private void BuildHud071()
        {
            RuntimeUi.EnsureEventSystem();
            _hud071 = RuntimeUi.CreateCanvas("Skyhome Arrival HUD 071");
            _hud071.overrideSorting = true;
            _hud071.sortingOrder = 130;
            var safe071 = RuntimeUi.AddSafeArea(_hud071.transform);
            var root071 = RuntimeUi.AddStretchRect(safe071, "Skyhome Arrival Safe HUD Root 071");
            _input071 = _hud071.gameObject.AddComponent<WorldInput071>();

            var location071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Location 071",
                new Color(0.012f, 0.020f, 0.032f, 0.78f));
            Anchor071(location071.rectTransform, new Vector2(0.025f, 0.915f), new Vector2(0.225f, 0.975f));
            var locationText071 = RuntimeUi.AddText(location071.transform, "Skyhome Arrival Location Text 071",
                "CHAPTER 1  •  SKYHOME MARKET", 22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch071(locationText071.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));

            var objective071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Objective 071",
                new Color(0.012f, 0.020f, 0.032f, 0.82f));
            Anchor071(objective071.rectTransform, new Vector2(0.245f, 0.895f), new Vector2(0.975f, 0.975f));
            _objectiveText071 = RuntimeUi.AddText(objective071.transform, "Skyhome Arrival Objective Text 071",
                "NEXT  •  Walk with Maren to the Guild Hall",
                30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            Stretch071(_objectiveText071.rectTransform, new Vector2(18f, 5f), new Vector2(-18f, -5f));

            var route071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Route Strip 073",
                new Color(0.012f, 0.020f, 0.032f, 0.90f));
            Anchor071(route071.rectTransform, new Vector2(0.245f, 0.842f), new Vector2(0.975f, 0.888f));
            _routeText071 = RuntimeUi.AddText(route071.transform, "Skyhome Arrival Route Text 073",
                "YOU  ━━━━━━━━━━━━━━━━━━━━━  GUILD HALL", 20,
                TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch071(_routeText071.rectTransform, new Vector2(14f, 3f), new Vector2(-14f, -3f));
            var routeTrack071 = RuntimeUi.AddPanel(route071.transform, "Skyhome Arrival Route Track 073",
                new Color(1f, 1f, 1f, 0.12f));
            Anchor071(routeTrack071.rectTransform, new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.11f));
            _routeFill071 = RuntimeUi.AddPanel(routeTrack071.transform, "Skyhome Arrival Route Fill 073",
                RuntimeUi.Warning);
            _routeFill071.raycastTarget = false;

            var story071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Dialogue Card 071",
                new Color(0.025f, 0.022f, 0.038f, 0.96f));
            story071.raycastTarget = false;
            _storyCard071 = story071.rectTransform;
            // A compact, persistent story strip: large enough to remain readable
            // at 1280x800 without covering the walkable route or mobile controls.
            Anchor071(_storyCard071, new Vector2(0.08f, 0.185f), new Vector2(0.92f, 0.385f));

            var portraitFrame071 = RuntimeUi.AddPanel(story071.transform,
                "Maren Holt Dialogue Portrait Frame 071",
                new Color(0.82f, 0.64f, 0.26f, 0.96f));
            portraitFrame071.raycastTarget = false;
            Anchor071(portraitFrame071.rectTransform,
                new Vector2(0.014f, 0.10f), new Vector2(0.090f, 0.90f));
            var portraitObject071 = new GameObject(
                "Maren Holt Authored Dialogue Portrait 071",
                typeof(RectTransform),
                typeof(Image));
            portraitObject071.transform.SetParent(portraitFrame071.transform, false);
            _storyPortrait071 = portraitObject071.GetComponent<Image>();
            _storyPortrait071.raycastTarget = false;
            _storyPortrait071.preserveAspect = true;
            _storyPortrait071.color = Color.white;
            Stretch071(_storyPortrait071.rectTransform,
                new Vector2(8f, 8f), new Vector2(-8f, -8f));
            _storyPortrait071.sprite = LoadSprite071(MarenPortraitResource071, new Vector2(0.5f, 0.5f));
            UsesAuthoredMarenPortrait071 = _storyPortrait071.sprite != null;
            if (!UsesAuthoredMarenPortrait071)
            {
                var fallback071 = RuntimeUi.AddText(portraitFrame071.transform,
                    "Maren Holt Portrait Fallback 071", "MH", 48,
                    TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
                fallback071.raycastTarget = false;
                Stretch071(fallback071.rectTransform,
                    new Vector2(8f, 8f), new Vector2(-8f, -8f));
            }

            _storySpeakerText071 = RuntimeUi.AddText(story071.transform,
                "Skyhome Arrival Dialogue Speaker 071", string.Empty,
                22, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            _storySpeakerText071.raycastTarget = false;
            Anchor071(_storySpeakerText071.rectTransform,
                new Vector2(0.105f, 0.65f), new Vector2(0.985f, 0.92f));

            _storyText071 = RuntimeUi.AddText(story071.transform,
                "Skyhome Arrival Dialogue Body 071", string.Empty,
                28, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Normal);
            _storyText071.raycastTarget = false;
            Anchor071(_storyText071.rectTransform,
                new Vector2(0.105f, 0.23f), new Vector2(0.985f, 0.70f));

            _storyProgressText071 = RuntimeUi.AddText(story071.transform,
                "Skyhome Arrival Dialogue Progress 071", string.Empty,
                20, TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Bold);
            _storyProgressText071.raycastTarget = false;
            Anchor071(_storyProgressText071.rectTransform,
                new Vector2(0.105f, 0.035f), new Vector2(0.42f, 0.23f));

            var progressTrack071 = RuntimeUi.AddPanel(story071.transform,
                "Skyhome Arrival Dialogue Progress Track 071",
                new Color(0.19f, 0.22f, 0.30f, 0.95f));
            progressTrack071.raycastTarget = false;
            Anchor071(progressTrack071.rectTransform,
                new Vector2(0.44f, 0.10f), new Vector2(0.985f, 0.16f));
            _storyProgressFill071 = RuntimeUi.AddPanel(progressTrack071.transform,
                "Skyhome Arrival Dialogue Progress Fill 071", RuntimeUi.Accent);
            _storyProgressFill071.raycastTarget = false;
            SetStoryBeat071(1);

            var prompt071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Prompt 071",
                new Color(0.07f, 0.042f, 0.012f, 0.96f));
            _promptPanel071 = prompt071;
            Anchor071(prompt071.rectTransform, new Vector2(0.27f, 0.075f), new Vector2(0.73f, 0.15f));
            _promptText071 = RuntimeUi.AddText(prompt071.transform, "Skyhome Arrival Prompt Text 071",
                string.Empty, 30, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold);
            Stretch071(_promptText071.rectTransform, new Vector2(12f, 4f), new Vector2(-12f, -4f));

            var controls071 = RuntimeUi.AddPanel(root071, "Skyhome Arrival Controls 071",
                new Color(0.012f, 0.020f, 0.032f, 0.90f));
            _controlsPanel071 = controls071;
            Anchor071(controls071.rectTransform, new Vector2(0.025f, 0.018f), new Vector2(0.56f, 0.078f));
            _controlsText071 = RuntimeUi.AddText(controls071.transform, "Skyhome Arrival Controls Text 071",
                "MOVE  WASD / STICK   •   RUN  SHIFT   •   ROLL  SPACE / B   •   ACT  E / A",
                24, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Stretch071(_controlsText071.rectTransform, new Vector2(8f, 3f), new Vector2(-8f, -3f));

            _input071.BuildTouchControls071(root071, includeBack: true, includeQuickPlay: false);
            // Some Windows touch-capable laptops report touch support. The actual
            // platform gate prevents mobile controls from leaking into desktop play.
            _input071.SetTouchControlsVisibleForVerification071(
                Application.isMobilePlatform && Input.touchSupported);
            UpdateControlLegendVisibility071();
        }

        private void UpdateStoryBeats071()
        {
            if (_player071 == null || _storyText071 == null) return;
            if (!_midpointBeatShown071 && _player071.position.x >= -1.5f)
            {
                _midpointBeatShown071 = true;
                SetStoryBeat071(2);
            }
            if (!_doorBeatShown071 && _player071.position.x >= 7.6f)
            {
                _doorBeatShown071 = true;
                SetStoryBeat071(3);
            }
        }

        private void SetStoryBeat071(int beat071)
        {
            _currentStoryBeat071 = Mathf.Clamp(beat071, 1, 3);
            if (_storyCard071 != null) _storyCard071.gameObject.SetActive(true);
            if (_storySpeakerText071 != null)
                _storySpeakerText071.text = "MAREN HOLT  •  LANTERN GUIDE";
            if (_storyText071 != null)
            {
                switch (_currentStoryBeat071)
                {
                    case 1:
                        _storyText071.text =
                            "Skyhome takes in people the gates leave behind. Tonight, its sealed bell is ringing without a rope.";
                        break;
                    case 2:
                        _storyText071.text =
                            "The Founders guard the gates. Our guild protects whoever falls between them.";
                        break;
                    default:
                        _storyText071.text =
                            "Kiri is waiting with an emergency charter. Reach the Hall doors and we'll learn who needs us.";
                        break;
                }
            }
            if (_storyProgressText071 != null)
                _storyProgressText071.text =
                    "CROSSING SKYHOME MARKET  •  " + _currentStoryBeat071 + " OF 3";
            if (_storyProgressFill071 != null)
            {
                var fill071 = _storyProgressFill071.rectTransform;
                fill071.anchorMin = Vector2.zero;
                fill071.anchorMax = new Vector2(_currentStoryBeat071 / 3f, 1f);
                fill071.offsetMin = Vector2.zero;
                fill071.offsetMax = Vector2.zero;
            }
        }

        private void UpdateGuidance071()
        {
            if (_promptText071 == null || _promptPanel071 == null) return;
            var distance071 = DistanceToGuildHall071;
            var progress071 = _player071 == null
                ? 0f
                : Mathf.InverseLerp(PlayerStart071.x, HallDoor071.x, _player071.position.x);
            if (_objectiveText071 != null)
                _objectiveText071.text = "NEXT  •  Reach the Guild Hall with Maren  •  " +
                                         Mathf.CeilToInt(distance071) + " m  →";
            if (_routeText071 != null)
                _routeText071.text = "YOU  •  " + Mathf.RoundToInt(progress071 * 100f) +
                                     "% OF THE WAY  •  GUILD HALL  →";
            if (_routeFill071 != null)
            {
                var fill071 = _routeFill071.rectTransform;
                fill071.anchorMin = Vector2.zero;
                fill071.anchorMax = new Vector2(Mathf.Clamp01(progress071), 1f);
                fill071.offsetMin = Vector2.zero;
                fill071.offsetMax = Vector2.zero;
            }
            if (distance071 <= HallInteractionRadius071)
            {
                var action071 = _input071 != null ? _input071.InteractionPrompt071 : "E";
                _promptText071.text = action071 + "  •  ENTER THE GUILD HALL";
                _promptPanel071.gameObject.SetActive(true);
            }
            else
            {
                _promptText071.text = string.Empty;
                _promptPanel071.gameObject.SetActive(false);
            }
        }

        private void UpdateCamera071(float deltaTime)
        {
            if (_camera071 == null || _player071 == null) return;
            var targetX071 = Mathf.Clamp(_player071.position.x, -6.90f, 6.90f);
            var desired071 = new Vector3(targetX071, 4.35f, -14.25f);
            _camera071.transform.position = Vector3.SmoothDamp(
                _camera071.transform.position,
                desired071,
                ref _cameraVelocity071,
                0.18f,
                80f,
                Mathf.Max(0.001f, deltaTime));
            _camera071.transform.LookAt(new Vector3(
                _camera071.transform.position.x,
                4.35f,
                0.65f));
        }

        private void UpdateMaren071(float deltaTime)
        {
            if (_maren071 == null || _player071 == null || _marenMotor071 == null) return;
            var target071 = _player071.position + new Vector3(-1.25f, 0f, 0.72f);
            target071.x = Mathf.Clamp(target071.x, MinimumX071, MaximumX071);
            var laneCenter071 = MarketLaneCenterZ071(target071.x);
            var laneHalfWidth071 = MarketLaneHalfWidth071(target071.x);
            target071.z = Mathf.Clamp(
                target071.z,
                Mathf.Max(MinimumZ071, laneCenter071 - laneHalfWidth071),
                Mathf.Min(MaximumZ071, laneCenter071 + laneHalfWidth071));
            var delta071 = target071 - _maren071.position;
            delta071.y = 0f;
            var distance071 = delta071.magnitude;
            var movement071 = distance071 > 1.15f
                ? new Vector2(delta071.x, delta071.z).normalized
                : Vector2.zero;
            _marenMotor071.SetInput070(
                movement071,
                distance071 > 3.0f,
                false,
                false);
        }

        private void UpdatePresentation071()
        {
            FaceBillboards071();
            if (_objectivePulse071 != null)
            {
                var pulse071 = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.045f;
                _objectivePulse071.localScale = new Vector3(3.60f * pulse071, 2.56f * pulse071, 1f);
            }
            // Story context stays visible during exploration. The previous timed
            // fade left players walking through an unexplained scene with no idea
            // why the Guild Hall mattered.
            if (_storyCard071 != null && !_storyCard071.gameObject.activeSelf)
                _storyCard071.gameObject.SetActive(true);
            if (_controlsPanel071 != null)
                UpdateControlLegendVisibility071();
            UpdateActorGrounding071();
        }

        private void UpdateControlLegendVisibility071()
        {
            if (_controlsPanel071 == null) return;
            var touchVisible071 = _input071 != null && _input071.TouchControlsVisible071;
            _controlsPanel071.gameObject.SetActive(!touchVisible071);
        }

        public void RefreshControlLegendForVerification076()
        {
            UpdateControlLegendVisibility071();
        }

        private void UpdateActorGrounding071()
        {
            if (_playerBillboard071 != null && _player071 != null)
            {
                var scale071 = MarketPerspectiveScale071(_player071.position.x);
                _playerBillboard071.localScale = Vector3.one * scale071;
                if (_playerShadow071 != null)
                    _playerShadow071.localScale = new Vector3(1.84375f * scale071, 1.50f * scale071, 1f);
            }
            if (_marenBillboard071 != null && _maren071 != null)
            {
                var scale071 = MarketPerspectiveScale071(_maren071.position.x);
                _marenBillboard071.localScale = Vector3.one * scale071;
                if (_marenShadow071 != null)
                    _marenShadow071.localScale = new Vector3(1.75f * scale071, 1.40625f * scale071, 1f);
            }
            if (_playerShadow071 != null)
                _playerShadow071.gameObject.SetActive(_playerVisual071 != null && _playerVisual071.gameObject.activeInHierarchy);
            if (_marenShadow071 != null)
                _marenShadow071.gameObject.SetActive(_marenVisual071 != null && _marenVisual071.gameObject.activeInHierarchy);
        }

        private static float MarketPerspectiveScale071(float worldX071)
        {
            // The painted road narrows toward the Hall door on the right.
            return Mathf.Lerp(1.06f, 0.82f,
                Mathf.InverseLerp(MinimumX071, MaximumX071, worldX071));
        }

        private void FaceBillboards071()
        {
            if (_camera071 == null) return;
            for (var index071 = 0; index071 < _billboards071.Count; index071++)
            {
                var billboard071 = _billboards071[index071];
                if (billboard071 != null) billboard071.rotation = _camera071.transform.rotation;
            }
        }

        private Transform CreateSpriteObject071(
            string name,
            Transform parent,
            Sprite sprite,
            Vector3 localPosition,
            float worldHeight,
            int sortingOrder)
        {
            var art071 = new GameObject(name);
            art071.transform.SetParent(parent, false);
            art071.transform.localPosition = localPosition;
            art071.layer = WorldLayer071;
            var renderer071 = art071.AddComponent<SpriteRenderer>();
            renderer071.sprite = sprite;
            renderer071.sortingOrder = sortingOrder;
            art071.transform.localScale = Vector3.one *
                                          (worldHeight / Mathf.Max(0.01f, sprite.bounds.size.y));
            return art071.transform;
        }

        private TextMesh CreateWorldLabel071(
            Transform parent071,
            string text071,
            Vector3 localPosition071,
            Color color071,
            int sortingOrder071)
        {
            var labelObject071 = new GameObject("Readable World Label 073");
            labelObject071.transform.SetParent(parent071, false);
            labelObject071.transform.localPosition = localPosition071;
            labelObject071.layer = WorldLayer071;
            var label071 = labelObject071.AddComponent<TextMesh>();
            label071.text = text071 ?? string.Empty;
            label071.anchor = TextAnchor.LowerCenter;
            label071.alignment = TextAlignment.Center;
            label071.fontSize = 72;
            label071.characterSize = 0.030f;
            label071.fontStyle = FontStyle.Bold;
            label071.color = color071;
            var renderer071 = labelObject071.GetComponent<MeshRenderer>();
            if (renderer071 != null) renderer071.sortingOrder = sortingOrder071;
            return label071;
        }

        private Transform CreateSoftFloorSprite071(
            Transform parent071,
            string name071,
            Vector3 localPosition071,
            Vector2 worldSize071,
            Color color071,
            int sortingOrder071)
        {
            var sprite071 = SoftEllipseSprite071();
            if (sprite071 == null || parent071 == null) return null;
            var object071 = new GameObject(name071);
            object071.transform.SetParent(parent071, false);
            object071.transform.localPosition = localPosition071;
            object071.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            object071.layer = WorldLayer071;
            var renderer071 = object071.AddComponent<SpriteRenderer>();
            renderer071.sprite = sprite071;
            renderer071.color = color071;
            renderer071.sortingOrder = sortingOrder071;
            object071.transform.localScale = new Vector3(
                worldSize071.x / Mathf.Max(0.01f, sprite071.bounds.size.x),
                worldSize071.y / Mathf.Max(0.01f, sprite071.bounds.size.y),
                1f);
            return object071.transform;
        }

        private Sprite SoftEllipseSprite071()
        {
            if (_softEllipseSprite071 != null) return _softEllipseSprite071;
            const int width071 = 64;
            const int height071 = 32;
            var texture071 = new Texture2D(
                width071,
                height071,
                TextureFormat.RGBA32,
                false,
                true)
            {
                name = "Skyhome Soft Ground Ellipse 071",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            for (var y071 = 0; y071 < height071; y071++)
            {
                for (var x071 = 0; x071 < width071; x071++)
                {
                    var normalizedX071 = (x071 + 0.5f) / width071 * 2f - 1f;
                    var normalizedY071 = (y071 + 0.5f) / height071 * 2f - 1f;
                    var radius071 = Mathf.Sqrt(
                        normalizedX071 * normalizedX071 + normalizedY071 * normalizedY071);
                    var alpha071 = Mathf.Pow(Mathf.Clamp01(1f - radius071), 1.65f);
                    texture071.SetPixel(x071, y071, new Color(1f, 1f, 1f, alpha071));
                }
            }
            texture071.Apply(false, true);
            _ownedTextures071.Add(texture071);
            _softEllipseSprite071 = Sprite.Create(
                texture071,
                new Rect(0f, 0f, width071, height071),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            _softEllipseSprite071.name = "Skyhome Soft Ground Ellipse Sprite 071";
            _ownedSprites071.Add(_softEllipseSprite071);
            return _softEllipseSprite071;
        }

        private Transform CreateFallbackCharacter071(string name, Transform parent, Color color)
        {
            var fallback071 = CreatePrimitive071(
                PrimitiveType.Capsule,
                name,
                Vector3.zero,
                new Vector3(0.70f, 0.92f, 0.70f),
                color,
                false);
            fallback071.transform.SetParent(parent, false);
            fallback071.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            return fallback071.transform;
        }

        private Sprite LoadSprite071(string resourceKey, Vector2 pivot)
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) return null;
            var imported071 = Resources.Load<Sprite>(resourceKey);
            if (imported071 != null) return imported071;
            var texture071 = Resources.Load<Texture2D>(resourceKey);
            if (texture071 == null) return null;
            var sprite071 = Sprite.Create(
                texture071,
                new Rect(0f, 0f, texture071.width, texture071.height),
                pivot,
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite071.name = texture071.name + "_RUNTIME_ARRIVAL_071";
            _ownedSprites071.Add(sprite071);
            return sprite071;
        }

        private GameObject CreatePrimitive071(
            PrimitiveType type,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            bool collider)
        {
            var primitive071 = GameObject.CreatePrimitive(type);
            primitive071.name = name;
            primitive071.transform.SetParent(_worldRoot071.transform, false);
            primitive071.transform.position = position;
            primitive071.transform.localScale = scale;
            SetLayer071(primitive071.transform);
            var collider071 = primitive071.GetComponent<Collider>();
            if (collider071 != null) collider071.enabled = collider;
            var renderer071 = primitive071.GetComponent<Renderer>();
            if (renderer071 != null)
            {
                var shader071 = Shader.Find("Universal Render Pipeline/Unlit") ??
                                Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
                if (shader071 != null)
                {
                    var material071 = new Material(shader071) { color = color };
                    if (material071.HasProperty("_BaseColor"))
                        material071.SetColor("_BaseColor", color);
                    renderer071.sharedMaterial = material071;
                    _ownedMaterials071.Add(material071);
                }
            }
            return primitive071;
        }

        private void CreateInvisibleBoundary071(string name, Vector3 position, Vector3 scale)
        {
            var boundary071 = CreatePrimitive071(
                PrimitiveType.Cube, name, position, scale, Color.clear, true);
            var renderer071 = boundary071.GetComponent<Renderer>();
            if (renderer071 != null) renderer071.enabled = false;
        }

        private static void SetLayer071(Transform root)
        {
            root.gameObject.layer = WorldLayer071;
            for (var index071 = 0; index071 < root.childCount; index071++)
                SetLayer071(root.GetChild(index071));
        }

        private static void Anchor071(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch071(RectTransform rect, Vector2 minimumOffset, Vector2 maximumOffset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = minimumOffset;
            rect.offsetMax = maximumOffset;
        }

        public void Shutdown071()
        {
            if (_shutdown071 && _worldRoot071 == null && _hud071 == null) return;
            _shutdown071 = true;
            StopAllCoroutines();
            _motor071?.StopImmediately070();
            _marenMotor071?.StopImmediately070();

            if (_worldRoot071 != null) Destroy(_worldRoot071);
            if (_hud071 != null) Destroy(_hud071.gameObject);
            _worldRoot071 = null;
            _hud071 = null;
            _camera071 = null;
            _controller071 = null;
            _motor071 = null;
            _animator071 = null;
            _input071 = null;
            _player071 = null;
            _playerBillboard071 = null;
            _playerVisual071 = null;
            _playerShadow071 = null;
            _maren071 = null;
            _marenBillboard071 = null;
            _marenVisual071 = null;
            _marenShadow071 = null;
            _marenController071 = null;
            _marenMotor071 = null;
            _marenAnimator071 = null;
            _objectiveMarker071 = null;
            _objectivePulse071 = null;
            _objectiveText071 = null;
            _promptText071 = null;
            _promptPanel071 = null;
            _controlsPanel071 = null;
            _controlsText071 = null;
            _storyCard071 = null;
            _storyPortrait071 = null;
            _storyProgressFill071 = null;
            _storySpeakerText071 = null;
            _storyText071 = null;
            _storyProgressText071 = null;
            _currentStoryBeat071 = 0;
            UsesAuthoredMarenPortrait071 = false;
            UsesAuthoredGuildmasterStandee076 = false;
            GuildmasterStandeeResourceKeyForVerification076 = string.Empty;
            GuildmasterPoseResourceRootForVerification076 = string.Empty;
            UsesAuthoredSkyhomeArt071 = false;
            _billboards071.Clear();

            for (var index071 = 0; index071 < _ownedSprites071.Count; index071++)
                if (_ownedSprites071[index071] != null) Destroy(_ownedSprites071[index071]);
            _ownedSprites071.Clear();
            _softEllipseSprite071 = null;
            for (var index071 = 0; index071 < _ownedTextures071.Count; index071++)
                if (_ownedTextures071[index071] != null) Destroy(_ownedTextures071[index071]);
            _ownedTextures071.Clear();
            for (var index071 = 0; index071 < _ownedMaterials071.Count; index071++)
                if (_ownedMaterials071[index071] != null) Destroy(_ownedMaterials071[index071]);
            _ownedMaterials071.Clear();
            enabled = false;
        }

        private void OnDestroy() => Shutdown071();
    }
}
