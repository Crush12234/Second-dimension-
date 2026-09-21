using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017D
{
    /// <summary>The device that most recently supplied meaningful world input.</summary>
    public enum WorldInputSource071
    {
        Keyboard = 0,
        Controller = 1,
        Touch = 2
    }

    /// <summary>
    /// One presentation-frame of normalized world controls. The movement systems
    /// consume this value without knowing which platform supplied it.
    /// </summary>
    public readonly struct WorldInputFrame071
    {
        public WorldInputFrame071(
            Vector2 movement,
            bool runHeld,
            bool dodgePressed,
            bool interactPressed,
            bool backPressed,
            bool quickPlayPressed,
            int routeChoiceIndex,
            WorldInputSource071 source)
        {
            Movement071 = Vector2.ClampMagnitude(movement, 1f);
            RunHeld071 = runHeld;
            DodgePressed071 = dodgePressed;
            InteractPressed071 = interactPressed;
            BackPressed071 = backPressed;
            QuickPlayPressed071 = quickPlayPressed;
            RouteChoiceIndex071 = routeChoiceIndex >= 0 && routeChoiceIndex <= 1
                ? routeChoiceIndex
                : -1;
            Source071 = source;
        }

        public Vector2 Movement071 { get; }
        public bool RunHeld071 { get; }
        public bool DodgePressed071 { get; }
        public bool InteractPressed071 { get; }
        public bool BackPressed071 { get; }
        public bool QuickPlayPressed071 { get; }
        public int RouteChoiceIndex071 { get; }
        public WorldInputSource071 Source071 { get; }
    }

    internal enum WorldTouchAction071
    {
        Interact = 0,
        Dodge = 1,
        Back = 2,
        QuickPlay = 3
    }

    /// <summary>
    /// Release 071 cross-platform input adapter for walkable world spaces. This is
    /// deliberately presentation-only: it translates devices into an input frame
    /// and never owns movement, quest, battle, save, or deterministic state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldInput071 : MonoBehaviour
    {
        public const float TouchStickDeadZone071 = 0.10f;
        public const float TouchRunThreshold071 = 0.86f;
        public const float MinimumTouchTarget071 = 156f;

        private RectTransform _touchRoot071;
        private WorldVirtualStick071 _virtualStick071;
        private Vector2 _touchMovement071;
        private bool _touchDodgePressed071;
        private bool _touchInteractPressed071;
        private bool _touchBackPressed071;
        private bool _touchQuickPlayPressed071;
        private bool _touchVisibilityForced071;
        private WorldInputSource071 _lastSource071 = WorldInputSource071.Keyboard;

        public WorldInputSource071 LastSource071 => _lastSource071;
        public bool TouchControlsBuilt071 => _touchRoot071 != null && _virtualStick071 != null;
        public bool TouchControlsVisible071 =>
            _touchRoot071 != null && _touchRoot071.gameObject.activeInHierarchy;
        public RectTransform TouchRootForVerification071 => _touchRoot071;
        public Vector2 TouchMovementForVerification071 => _touchMovement071;
        public string InteractionPrompt071 => _lastSource071 == WorldInputSource071.Touch
            ? "ACT"
            : _lastSource071 == WorldInputSource071.Controller
                ? "A / CROSS"
                : "E";

        /// <summary>
        /// Builds a notch-safe mobile control layer under the supplied Safe Area.
        /// The hierarchy exists on every platform for verification, but auto-shows
        /// only on touch-capable/mobile players so desktop footage stays clean.
        /// </summary>
        public void BuildTouchControls071(
            RectTransform safeRoot,
            bool includeBack,
            bool includeQuickPlay)
        {
            if (safeRoot == null || _touchRoot071 != null) return;

            var rootObject = new GameObject(
                "World Touch Controls 071",
                typeof(RectTransform),
                typeof(CanvasGroup));
            rootObject.transform.SetParent(safeRoot, false);
            _touchRoot071 = rootObject.GetComponent<RectTransform>();
            _touchRoot071.anchorMin = Vector2.zero;
            _touchRoot071.anchorMax = Vector2.one;
            _touchRoot071.offsetMin = Vector2.zero;
            _touchRoot071.offsetMax = Vector2.zero;
            var group = rootObject.GetComponent<CanvasGroup>();
            group.alpha = 0.94f;
            group.interactable = true;
            group.blocksRaycasts = true;

            _virtualStick071 = BuildStick071(_touchRoot071);
            BuildTouchButton071(
                _touchRoot071,
                "World Touch Act 071",
                "ACT",
                WorldTouchAction071.Interact,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-54f, 52f),
                new Vector2(224f, 224f),
                new Color(0.16f, 0.52f, 0.66f, 0.93f));
            BuildTouchButton071(
                _touchRoot071,
                "World Touch Roll 071",
                "ROLL",
                WorldTouchAction071.Dodge,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-304f, 46f),
                new Vector2(184f, 184f),
                new Color(0.29f, 0.20f, 0.48f, 0.93f));

            if (includeBack)
            {
                BuildTouchButton071(
                    _touchRoot071,
                    "World Touch Back 071",
                    "BACK",
                    WorldTouchAction071.Back,
                    new Vector2(0f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(34f, -178f),
                    new Vector2(168f, 112f),
                    new Color(0.08f, 0.13f, 0.20f, 0.92f));
            }

            if (includeQuickPlay)
            {
                BuildTouchButton071(
                    _touchRoot071,
                    "World Touch Quick Play 071",
                    "STORY",
                    WorldTouchAction071.QuickPlay,
                    new Vector2(1f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(-34f, -34f),
                    new Vector2(190f, 112f),
                    new Color(0.64f, 0.45f, 0.12f, 0.94f));
            }

            SetTouchControlsVisible071(ShouldAutoShowTouch071(), false);
        }

        /// <summary>Reads keyboard, gamepad and touch exactly once for the host Update.</summary>
        public WorldInputFrame071 CaptureFrame071()
        {
            if (_touchRoot071 != null && !_touchVisibilityForced071 && Input.touchCount > 0)
                SetTouchControlsVisible071(true, false);

            var keyboardMovement = ReadKeyboardMovement071();
            var combinedAxis = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            var controllerMovement = keyboardMovement.sqrMagnitude > 0.001f
                ? Vector2.zero
                : Vector2.ClampMagnitude(combinedAxis, 1f);

            var keyboardInteract = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return);
            var controllerInteract = Input.GetKeyDown(KeyCode.JoystickButton0);
            var keyboardDodge = Input.GetKeyDown(KeyCode.Space);
            var controllerDodge = Input.GetKeyDown(KeyCode.JoystickButton1);
            var keyboardBack = Input.GetKeyDown(KeyCode.Escape);
            var controllerBack = Input.GetKeyDown(KeyCode.JoystickButton7);
            var keyboardQuickPlay = Input.GetKeyDown(KeyCode.Q);
            var controllerQuickPlay = Input.GetKeyDown(KeyCode.JoystickButton3);
            var keyboardRouteOne = Input.GetKeyDown(KeyCode.Alpha1) ||
                                   Input.GetKeyDown(KeyCode.Keypad1);
            var keyboardRouteTwo = Input.GetKeyDown(KeyCode.Alpha2) ||
                                   Input.GetKeyDown(KeyCode.Keypad2);
            var controllerRouteOne = Input.GetKeyDown(KeyCode.JoystickButton4);
            var controllerRouteTwo = Input.GetKeyDown(KeyCode.JoystickButton5);
            var controllerActivity = controllerMovement.sqrMagnitude > 0.001f ||
                                     controllerInteract || controllerDodge || controllerBack ||
                                     controllerQuickPlay || controllerRouteOne || controllerRouteTwo;
            var keyboardActivity = keyboardMovement.sqrMagnitude > 0.001f ||
                                   keyboardInteract || keyboardDodge || keyboardBack ||
                                   keyboardQuickPlay || keyboardRouteOne || keyboardRouteTwo;
            var touchActivity = _touchMovement071.sqrMagnitude > 0.001f ||
                                _touchInteractPressed071 || _touchDodgePressed071 ||
                                _touchBackPressed071 || _touchQuickPlayPressed071;

            var movement = SelectMovement071(
                keyboardMovement,
                controllerMovement,
                _touchMovement071,
                out var movementSource);
            if (touchActivity) _lastSource071 = WorldInputSource071.Touch;
            else if (controllerActivity) _lastSource071 = WorldInputSource071.Controller;
            else if (keyboardActivity) _lastSource071 = WorldInputSource071.Keyboard;
            else if (movement.sqrMagnitude > 0.001f) _lastSource071 = movementSource;

            var routeChoice = -1;
            if (keyboardRouteOne || controllerRouteOne) routeChoice = 0;
            else if (keyboardRouteTwo || controllerRouteTwo) routeChoice = 1;

            var frame = new WorldInputFrame071(
                movement,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                Input.GetKey(KeyCode.JoystickButton2) ||
                ((_lastSource071 == WorldInputSource071.Touch ||
                  _lastSource071 == WorldInputSource071.Controller) &&
                 movement.magnitude >= TouchRunThreshold071),
                keyboardDodge || controllerDodge || _touchDodgePressed071,
                keyboardInteract || controllerInteract || _touchInteractPressed071,
                keyboardBack || controllerBack || _touchBackPressed071,
                keyboardQuickPlay || controllerQuickPlay || _touchQuickPlayPressed071,
                routeChoice,
                _lastSource071);
            ConsumeTouchEdges071();
            return frame;
        }

        /// <summary>Legacy-only fallback used if a runtime HUD could not be constructed.</summary>
        public static WorldInputFrame071 CaptureLegacyFrame071()
        {
            var movement = ReadKeyboardMovement071();
            var combinedAxis = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            var source = movement.sqrMagnitude > 0.001f
                ? WorldInputSource071.Keyboard
                : WorldInputSource071.Controller;
            if (movement.sqrMagnitude <= 0.001f)
                movement = Vector2.ClampMagnitude(combinedAxis, 1f);
            var routeChoice = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) ||
                              Input.GetKeyDown(KeyCode.JoystickButton4)
                ? 0
                : Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) ||
                  Input.GetKeyDown(KeyCode.JoystickButton5)
                    ? 1
                    : -1;
            return new WorldInputFrame071(
                movement,
                Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
                Input.GetKey(KeyCode.JoystickButton2) ||
                (source == WorldInputSource071.Controller &&
                 movement.magnitude >= TouchRunThreshold071),
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton1),
                Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.JoystickButton0),
                Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7),
                Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.JoystickButton3),
                routeChoice,
                source);
        }

        /// <summary>Pure composition seam for EditMode tests and future remapping.</summary>
        public static Vector2 SelectMovement071(
            Vector2 keyboard,
            Vector2 controller,
            Vector2 touch,
            out WorldInputSource071 source)
        {
            if (touch.sqrMagnitude > TouchStickDeadZone071 * TouchStickDeadZone071)
            {
                source = WorldInputSource071.Touch;
                return Vector2.ClampMagnitude(touch, 1f);
            }
            if (keyboard.sqrMagnitude > 0.001f)
            {
                source = WorldInputSource071.Keyboard;
                return Vector2.ClampMagnitude(keyboard, 1f);
            }
            if (controller.sqrMagnitude > 0.001f)
            {
                source = WorldInputSource071.Controller;
                return Vector2.ClampMagnitude(controller, 1f);
            }
            source = WorldInputSource071.Keyboard;
            return Vector2.zero;
        }

        /// <summary>Allows settings/accessibility screens and tests to override auto visibility.</summary>
        public void SetTouchControlsVisibleForVerification071(bool visible)
        {
            _touchVisibilityForced071 = true;
            SetTouchControlsVisible071(visible, true);
        }

        public void SetTouchMovementForVerification071(Vector2 normalizedMovement) =>
            SetTouchMovement071(normalizedMovement);

        public void QueueTouchInteractForVerification071() =>
            QueueTouchAction071(WorldTouchAction071.Interact);

        public void QueueTouchDodgeForVerification071() =>
            QueueTouchAction071(WorldTouchAction071.Dodge);

        /// <summary>Hardware-independent seam proving touch edges are single-frame pulses.</summary>
        public WorldInputFrame071 ReadTouchFrameForVerification071()
        {
            var movement = SelectMovement071(
                Vector2.zero,
                Vector2.zero,
                _touchMovement071,
                out _);
            _lastSource071 = WorldInputSource071.Touch;
            var frame = new WorldInputFrame071(
                movement,
                movement.magnitude >= TouchRunThreshold071,
                _touchDodgePressed071,
                _touchInteractPressed071,
                _touchBackPressed071,
                _touchQuickPlayPressed071,
                -1,
                WorldInputSource071.Touch);
            ConsumeTouchEdges071();
            return frame;
        }

        internal void SetTouchMovement071(Vector2 normalizedMovement)
        {
            _touchMovement071 = Vector2.ClampMagnitude(normalizedMovement, 1f);
            if (_touchMovement071.sqrMagnitude <
                TouchStickDeadZone071 * TouchStickDeadZone071)
                _touchMovement071 = Vector2.zero;
            if (_touchMovement071.sqrMagnitude > 0.001f)
                _lastSource071 = WorldInputSource071.Touch;
        }

        internal void QueueTouchAction071(WorldTouchAction071 action)
        {
            _lastSource071 = WorldInputSource071.Touch;
            switch (action)
            {
                case WorldTouchAction071.Interact:
                    _touchInteractPressed071 = true;
                    break;
                case WorldTouchAction071.Dodge:
                    _touchDodgePressed071 = true;
                    break;
                case WorldTouchAction071.Back:
                    _touchBackPressed071 = true;
                    break;
                case WorldTouchAction071.QuickPlay:
                    _touchQuickPlayPressed071 = true;
                    break;
            }
        }

        private static Vector2 ReadKeyboardMovement071()
        {
            var horizontal = 0f;
            var vertical = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

        private bool ShouldAutoShowTouch071() => Application.isMobilePlatform;

        private void SetTouchControlsVisible071(bool visible, bool preserveOverride)
        {
            if (_touchRoot071 != null) _touchRoot071.gameObject.SetActive(visible);
            if (!preserveOverride) _touchVisibilityForced071 = false;
            if (!visible)
            {
                _touchMovement071 = Vector2.zero;
                if (_virtualStick071 != null) _virtualStick071.ResetStick071();
            }
        }

        private void ConsumeTouchEdges071()
        {
            _touchDodgePressed071 = false;
            _touchInteractPressed071 = false;
            _touchBackPressed071 = false;
            _touchQuickPlayPressed071 = false;
        }

        private WorldVirtualStick071 BuildStick071(RectTransform parent)
        {
            var baseObject = new GameObject(
                "World Virtual Stick 071",
                typeof(RectTransform),
                typeof(Image),
                typeof(WorldVirtualStick071));
            baseObject.transform.SetParent(parent, false);
            var rect = baseObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(52f, 48f);
            rect.sizeDelta = new Vector2(330f, 330f);
            var image = baseObject.GetComponent<Image>();
            image.color = new Color(0.04f, 0.08f, 0.13f, 0.68f);

            var handleObject = new GameObject(
                "World Virtual Stick Handle 071",
                typeof(RectTransform),
                typeof(Image));
            handleObject.transform.SetParent(rect, false);
            var handle = handleObject.GetComponent<RectTransform>();
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(138f, 138f);
            var handleImage = handleObject.GetComponent<Image>();
            handleImage.color = new Color(0.82f, 0.68f, 0.39f, 0.94f);
            handleImage.raycastTarget = false;

            var stick = baseObject.GetComponent<WorldVirtualStick071>();
            stick.Configure071(this, handle);
            return stick;
        }

        private void BuildTouchButton071(
            RectTransform parent,
            string objectName,
            string label,
            WorldTouchAction071 action,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size,
            Color normalColor)
        {
            size.x = Mathf.Max(MinimumTouchTarget071, size.x);
            size.y = Mathf.Max(MinimumTouchTarget071, size.y);
            var buttonObject = new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image),
                typeof(WorldTouchButton071));
            buttonObject.transform.SetParent(parent, false);
            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var image = buttonObject.GetComponent<Image>();
            image.color = normalColor;

            var labelObject = new GameObject(
                objectName + " Label",
                typeof(RectTransform),
                typeof(Text));
            labelObject.transform.SetParent(rect, false);
            var text = labelObject.GetComponent<Text>();
            text.font = RuntimeUi.Font;
            text.fontSize = action == WorldTouchAction071.Back || action == WorldTouchAction071.QuickPlay
                ? 38
                : 46;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = RuntimeUi.Text;
            text.text = label;
            text.raycastTarget = false;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 8f);
            textRect.offsetMax = new Vector2(-12f, -8f);

            buttonObject.GetComponent<WorldTouchButton071>().Configure071(
                this,
                action,
                image,
                normalColor);
        }

        private void OnDisable()
        {
            _touchMovement071 = Vector2.zero;
            ConsumeTouchEdges071();
            if (_virtualStick071 != null) _virtualStick071.ResetStick071();
        }
    }

    /// <summary>Multi-touch-safe virtual stick that owns only its active pointer.</summary>
    public sealed class WorldVirtualStick071 : MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        ICancelHandler
    {
        private WorldInput071 _owner071;
        private RectTransform _base071;
        private RectTransform _handle071;
        private int _activePointer071 = int.MinValue;

        public void Configure071(WorldInput071 owner, RectTransform handle)
        {
            _owner071 = owner;
            _base071 = transform as RectTransform;
            _handle071 = handle;
            ResetStick071();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointer071 != int.MinValue || eventData == null) return;
            _activePointer071 = eventData.pointerId;
            ApplyPointer071(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != _activePointer071) return;
            ApplyPointer071(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != _activePointer071) return;
            ResetStick071();
        }

        public void OnCancel(BaseEventData eventData) => ResetStick071();

        public void ResetStick071()
        {
            _activePointer071 = int.MinValue;
            if (_handle071 != null) _handle071.anchoredPosition = Vector2.zero;
            if (_owner071 != null) _owner071.SetTouchMovement071(Vector2.zero);
        }

        private void ApplyPointer071(PointerEventData eventData)
        {
            if (_base071 == null || _owner071 == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _base071,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var local)) return;
            local -= _base071.rect.center;
            var travel = Mathf.Max(1f, Mathf.Min(_base071.rect.width, _base071.rect.height) * 0.30f);
            var normalized = Vector2.ClampMagnitude(local / travel, 1f);
            if (_handle071 != null) _handle071.anchoredPosition = normalized * travel;
            _owner071.SetTouchMovement071(normalized);
            eventData.Use();
        }

        private void OnDisable() => ResetStick071();
    }

    /// <summary>Pointer-down action button for low-latency mobile roll and interaction.</summary>
    public sealed class WorldTouchButton071 : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler,
        IPointerExitHandler,
        ICancelHandler
    {
        private WorldInput071 _owner071;
        private WorldTouchAction071 _action071;
        private Image _image071;
        private Color _normalColor071;
        private int _activePointer071 = int.MinValue;

        internal void Configure071(
            WorldInput071 owner,
            WorldTouchAction071 action,
            Image image,
            Color normalColor)
        {
            _owner071 = owner;
            _action071 = action;
            _image071 = image;
            _normalColor071 = normalColor;
            SetPressed071(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_owner071 == null || eventData == null || _activePointer071 != int.MinValue) return;
            _activePointer071 = eventData.pointerId;
            SetPressed071(true);
            _owner071.QueueTouchAction071(_action071);
            eventData.Use();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != _activePointer071) return;
            ResetButton071();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData == null || eventData.pointerId != _activePointer071) return;
            SetPressed071(false);
        }

        public void OnCancel(BaseEventData eventData) => ResetButton071();

        private void ResetButton071()
        {
            _activePointer071 = int.MinValue;
            SetPressed071(false);
        }

        private void SetPressed071(bool pressed)
        {
            if (_image071 == null) return;
            _image071.color = pressed
                ? Color.Lerp(_normalColor071, Color.white, 0.28f)
                : _normalColor071;
        }

        private void OnDisable() => ResetButton071();
    }
}
