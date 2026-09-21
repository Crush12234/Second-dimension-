using System;
using UnityEngine;

namespace SecondDimension.Presentation.GuildCity017D
{
    /// <summary>
    /// Player-facing locomotion phases. Presentation code can bind these states to
    /// authored clips without inferring animation from transform deltas.
    /// </summary>
    public enum WorldCharacterMotionState070
    {
        Idle = 0,
        Starting = 1,
        Walking = 2,
        Running = 3,
        Stopping = 4,
        Turning = 5,
        Dodging = 6,
        Interacting = 7
    }

    /// <summary>Serializable movement values shared by Hall and expedition avatars.</summary>
    [Serializable]
    public sealed class WorldCharacterMotorTuning070
    {
        public float WalkSpeed = 4.6f;
        public float RunSpeed = 7.2f;
        public float Acceleration = 18f;
        public float Braking = 24f;
        public float TurnSpeedDegrees = 720f;
        public float TurnAnticipationSeconds = 0.10f;
        public float StartAnticipationSeconds = 0.10f;
        public float StopSettleSeconds = 0.12f;
        public float ReverseTurnAngle = 105f;
        public float InputDeadZone = 0.08f;
        public float Gravity = -24f;
        public float GroundStickVelocity = -2f;
        public float DodgeSpeed = 11.8f;
        public float DodgeExitSpeedMultiplier = 0.62f;
        public float DodgeDuration = 0.34f;
        public float DodgeCooldown = 0.42f;
        public float InteractionLockSeconds = 0.28f;

        /// <summary>
        /// Returns a defensive copy. Bad inspector or authority values cannot turn
        /// movement into a teleport, divide-by-zero, or an upward gravity launch.
        /// </summary>
        public WorldCharacterMotorTuning070 SanitizedCopy070()
        {
            var walk = Mathf.Clamp(FiniteOrDefault070(WalkSpeed, 4.6f), 0.25f, 20f);
            return new WorldCharacterMotorTuning070
            {
                WalkSpeed = walk,
                RunSpeed = Mathf.Clamp(FiniteOrDefault070(RunSpeed, 7.2f), walk, 30f),
                Acceleration = Mathf.Clamp(FiniteOrDefault070(Acceleration, 18f), 0.5f, 100f),
                Braking = Mathf.Clamp(FiniteOrDefault070(Braking, 24f), 0.5f, 120f),
                TurnSpeedDegrees = Mathf.Clamp(FiniteOrDefault070(TurnSpeedDegrees, 720f), 30f, 2160f),
                TurnAnticipationSeconds = Mathf.Clamp(FiniteOrDefault070(TurnAnticipationSeconds, 0.10f), 0f, 0.5f),
                StartAnticipationSeconds = Mathf.Clamp(FiniteOrDefault070(StartAnticipationSeconds, 0.10f), 0f, 0.5f),
                StopSettleSeconds = Mathf.Clamp(FiniteOrDefault070(StopSettleSeconds, 0.12f), 0f, 0.6f),
                ReverseTurnAngle = Mathf.Clamp(FiniteOrDefault070(ReverseTurnAngle, 105f), 60f, 179f),
                InputDeadZone = Mathf.Clamp(FiniteOrDefault070(InputDeadZone, 0.08f), 0f, 0.5f),
                Gravity = Mathf.Clamp(FiniteOrDefault070(Gravity, -24f), -100f, -0.1f),
                GroundStickVelocity = Mathf.Clamp(FiniteOrDefault070(GroundStickVelocity, -2f), -20f, -0.01f),
                DodgeSpeed = Mathf.Clamp(FiniteOrDefault070(DodgeSpeed, 11.8f), walk, 40f),
                DodgeExitSpeedMultiplier = Mathf.Clamp(FiniteOrDefault070(DodgeExitSpeedMultiplier, 0.62f), 0.1f, 1f),
                DodgeDuration = Mathf.Clamp(FiniteOrDefault070(DodgeDuration, 0.34f), 0.08f, 1.2f),
                DodgeCooldown = Mathf.Clamp(FiniteOrDefault070(DodgeCooldown, 0.42f), 0f, 3f),
                InteractionLockSeconds = Mathf.Clamp(FiniteOrDefault070(InteractionLockSeconds, 0.28f), 0.02f, 3f)
            };
        }

        private static float FiniteOrDefault070(float value, float fallback) =>
            float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
    }

    /// <summary>
    /// Collision-safe, grounded character motor for the walkable Guild Hall and
    /// expeditions. All world displacement goes through CharacterController.Move;
    /// presentation never slides or clamps the transform directly.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class WorldCharacterMotor070 : MonoBehaviour
    {
        [SerializeField] private WorldCharacterMotorTuning070 _tuning = new WorldCharacterMotorTuning070();
        [SerializeField] private bool _driveFromLegacyKeyboard = true;

        private CharacterController _controller;
        private Vector2 _movementInput;
        private Vector3 _planarVelocity;
        private Vector3 _facingDirection = Vector3.forward;
        private Vector3 _lastTravelDirection = Vector3.forward;
        private Vector3 _dodgeDirection = Vector3.forward;
        private bool _runHeld;
        private bool _dodgeQueued;
        private bool _interactionQueued;
        private float _verticalVelocity;
        private float _dodgeRemaining;
        private float _dodgeCooldownRemaining;
        private float _interactionRemaining;
        private float _stateElapsed;
        private CollisionFlags _lastCollisionFlags;
        private WorldCharacterMotionState070 _state = WorldCharacterMotionState070.Idle;

        public event Action<WorldCharacterMotionState070, WorldCharacterMotionState070> StateChanged070;
        public event Action InteractionRequested070;

        public CharacterController Controller070 => EnsureController070();
        public WorldCharacterMotionState070 MotionState070 => _state;
        public Vector3 PlanarVelocity070 => _planarVelocity;
        public Vector3 FacingDirection070 => _facingDirection;
        public float PlanarSpeed070 => _planarVelocity.magnitude;
        public float NormalizedSpeed070 => Mathf.Clamp01(PlanarSpeed070 / Mathf.Max(0.01f, _tuning.RunSpeed));
        public float StateElapsed070 => _stateElapsed;
        public float DodgeNormalizedTime070 => _state == WorldCharacterMotionState070.Dodging
            ? 1f - Mathf.Clamp01(_dodgeRemaining / Mathf.Max(0.01f, _tuning.DodgeDuration))
            : 0f;
        public bool IsGrounded070 => EnsureController070().isGrounded ||
                                     (_lastCollisionFlags & CollisionFlags.Below) != 0;
        public bool CanDodge070 => _dodgeCooldownRemaining <= 0f &&
                                   _state != WorldCharacterMotionState070.Dodging &&
                                   _state != WorldCharacterMotionState070.Interacting;
        public CollisionFlags LastCollisionFlags070 => _lastCollisionFlags;
        public WorldCharacterMotorTuning070 TuningForVerification070 => _tuning.SanitizedCopy070();

        private void Awake()
        {
            EnsureController070();
            _tuning = (_tuning ?? new WorldCharacterMotorTuning070()).SanitizedCopy070();
            var initialForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (initialForward.sqrMagnitude > 0.001f)
            {
                _facingDirection = initialForward.normalized;
                _lastTravelDirection = _facingDirection;
            }
        }

        private void OnDisable()
        {
            _movementInput = Vector2.zero;
            _runHeld = false;
            _dodgeQueued = false;
            _interactionQueued = false;
            _planarVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _dodgeRemaining = 0f;
            _interactionRemaining = 0f;
            SetState070(WorldCharacterMotionState070.Idle);
        }

        private void Update()
        {
            if (_driveFromLegacyKeyboard)
            {
                var horizontal = 0f;
                var vertical = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
                SetInput070(
                    new Vector2(horizontal, vertical),
                    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                    Input.GetKeyDown(KeyCode.Space),
                    Input.GetKeyDown(KeyCode.E));
            }

            Tick070(Time.unscaledDeltaTime);
        }

        /// <summary>Configures the motor without requiring inspector mutation.</summary>
        public void Configure070(WorldCharacterMotorTuning070 tuning, bool driveFromLegacyKeyboard)
        {
            EnsureController070();
            _tuning = (tuning ?? new WorldCharacterMotorTuning070()).SanitizedCopy070();
            _driveFromLegacyKeyboard = driveFromLegacyKeyboard;
        }

        /// <summary>
        /// Supplies desired input. Dodge and interaction arguments are edge events;
        /// movement and run are retained until the next call.
        /// </summary>
        public void SetInput070(Vector2 movement, bool runHeld, bool dodgePressed, bool interactionPressed)
        {
            _movementInput = Vector2.ClampMagnitude(movement, 1f);
            _runHeld = runHeld;
            _dodgeQueued |= dodgePressed;
            _interactionQueued |= interactionPressed;
        }

        /// <summary>Deterministic seam for PlayMode tests and host-owned input systems.</summary>
        public void Simulate070(
            Vector2 movement,
            bool runHeld,
            bool dodgePressed,
            bool interactionPressed,
            float deltaTime)
        {
            SetInput070(movement, runHeld, dodgePressed, interactionPressed);
            if (float.IsNaN(deltaTime) || deltaTime <= 0f)
            {
                Tick070(0f);
                return;
            }

            // Runtime frames still advance once through Update. This explicit seam
            // also supports host verification calls that represent a longer interval;
            // consume that interval in the same safe 100 ms steps used by Tick070 so
            // acceleration, dodge duration, gravity and collision are not truncated.
            var remaining = Mathf.Min(deltaTime, 10f);
            while (remaining > 0f)
            {
                var step = Mathf.Min(remaining, 0.1f);
                Tick070(step);
                if (this == null || !enabled || !gameObject.activeInHierarchy) break;
                remaining -= step;
            }
        }

        /// <summary>Advances motion once. Negative and giant frame deltas are contained.</summary>
        public void Tick070(float deltaTime)
        {
            var controller = EnsureController070();
            if (!enabled || !gameObject.activeInHierarchy || !controller.enabled) return;

            var safeDelta = Mathf.Clamp(deltaTime, 0f, 0.1f);
            if (safeDelta <= 0f)
            {
                _dodgeQueued = false;
                _interactionQueued = false;
                return;
            }

            _stateElapsed += safeDelta;
            _dodgeCooldownRemaining = Mathf.Max(0f, _dodgeCooldownRemaining - safeDelta);

            var interactionRequested = _interactionQueued;
            _interactionQueued = false;
            if (interactionRequested && _state != WorldCharacterMotionState070.Dodging)
            {
                BeginInteraction070(_tuning.InteractionLockSeconds);
                InteractionRequested070?.Invoke();
                if (this == null || !enabled || !gameObject.activeInHierarchy) return;
            }

            var desiredDirection = DesiredWorldDirection070();
            if (desiredDirection.sqrMagnitude > 0.001f) _lastTravelDirection = desiredDirection;

            if (_dodgeQueued && CanDodge070)
            {
                _dodgeDirection = desiredDirection.sqrMagnitude > 0.001f
                    ? desiredDirection
                    : _lastTravelDirection;
                if (_dodgeDirection.sqrMagnitude < 0.001f) _dodgeDirection = _facingDirection;
                _dodgeDirection.Normalize();
                _dodgeRemaining = _tuning.DodgeDuration;
                _dodgeCooldownRemaining = _tuning.DodgeDuration + _tuning.DodgeCooldown;
                SetState070(WorldCharacterMotionState070.Dodging);
            }
            _dodgeQueued = false;

            if (_state == WorldCharacterMotionState070.Interacting)
            {
                _interactionRemaining = Mathf.Max(0f, _interactionRemaining - safeDelta);
                _planarVelocity = Vector3.MoveTowards(
                    _planarVelocity,
                    Vector3.zero,
                    _tuning.Braking * safeDelta);
                MoveGrounded070(safeDelta);
                if (_interactionRemaining <= 0f) SelectPostActionState070(desiredDirection);
                return;
            }

            if (_state == WorldCharacterMotionState070.Dodging)
            {
                _dodgeRemaining = Mathf.Max(0f, _dodgeRemaining - safeDelta);
                var progress = DodgeNormalizedTime070;
                var speedMultiplier = Mathf.Lerp(1f, _tuning.DodgeExitSpeedMultiplier, progress);
                _planarVelocity = _dodgeDirection * (_tuning.DodgeSpeed * speedMultiplier);
                FaceToward070(_dodgeDirection, safeDelta, 1.6f);
                MoveGrounded070(safeDelta);
                if (_dodgeRemaining <= 0f) SelectPostActionState070(desiredDirection);
                return;
            }

            var hasInput = desiredDirection.sqrMagnitude > 0.001f;
            var facingAngle = hasInput ? Vector3.Angle(_facingDirection, desiredDirection) : 0f;
            var reverseTurn = hasInput && PlanarSpeed070 > 0.15f && facingAngle >= _tuning.ReverseTurnAngle;

            if (reverseTurn && _state != WorldCharacterMotionState070.Turning)
                SetState070(WorldCharacterMotionState070.Turning);

            var anticipatingTurn = _state == WorldCharacterMotionState070.Turning &&
                                   _stateElapsed < _tuning.TurnAnticipationSeconds;
            if (hasInput) FaceToward070(desiredDirection, safeDelta, anticipatingTurn ? 1.35f : 1f);

            var targetSpeed = hasInput && !anticipatingTurn
                ? (_runHeld ? _tuning.RunSpeed : _tuning.WalkSpeed)
                : 0f;
            var targetVelocity = desiredDirection * targetSpeed;
            var rate = targetSpeed > PlanarSpeed070 ? _tuning.Acceleration : _tuning.Braking;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, rate * safeDelta);

            SelectLocomotionState070(hasInput, anticipatingTurn);
            MoveGrounded070(safeDelta);
        }

        public void BeginInteraction070(float lockSeconds = -1f)
        {
            if (_state == WorldCharacterMotionState070.Dodging) return;
            _interactionRemaining = lockSeconds > 0f
                ? Mathf.Clamp(lockSeconds, 0.02f, 3f)
                : _tuning.InteractionLockSeconds;
            SetState070(WorldCharacterMotionState070.Interacting);
        }

        public void EndInteraction070()
        {
            if (_state != WorldCharacterMotionState070.Interacting) return;
            _interactionRemaining = 0f;
            SelectPostActionState070(DesiredWorldDirection070());
        }

        public void StopImmediately070()
        {
            _movementInput = Vector2.zero;
            _runHeld = false;
            _dodgeQueued = false;
            _interactionQueued = false;
            _planarVelocity = Vector3.zero;
            _dodgeRemaining = 0f;
            _interactionRemaining = 0f;
            SetState070(WorldCharacterMotionState070.Idle);
        }

        private CharacterController EnsureController070()
        {
            if (_controller == null) _controller = GetComponent<CharacterController>();
            return _controller;
        }

        private Vector3 DesiredWorldDirection070()
        {
            if (_movementInput.sqrMagnitude <= _tuning.InputDeadZone * _tuning.InputDeadZone)
                return Vector3.zero;
            var direction = new Vector3(_movementInput.x, 0f, _movementInput.y);
            return direction.sqrMagnitude > 1f ? direction.normalized : direction;
        }

        private void FaceToward070(Vector3 direction, float deltaTime, float speedMultiplier)
        {
            if (direction.sqrMagnitude < 0.001f) return;
            direction.y = 0f;
            direction.Normalize();
            var targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _tuning.TurnSpeedDegrees * speedMultiplier * deltaTime);
            var projected = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            _facingDirection = projected.sqrMagnitude > 0.001f ? projected.normalized : direction;
        }

        private void MoveGrounded070(float deltaTime)
        {
            var controller = EnsureController070();
            if (controller.isGrounded && _verticalVelocity <= 0f)
                _verticalVelocity = _tuning.GroundStickVelocity;
            else
                _verticalVelocity += _tuning.Gravity * deltaTime;

            var displacementVelocity = _planarVelocity + Vector3.up * _verticalVelocity;
            _lastCollisionFlags = controller.Move(displacementVelocity * deltaTime);
            if ((_lastCollisionFlags & CollisionFlags.Below) != 0)
                _verticalVelocity = _tuning.GroundStickVelocity;
            if ((_lastCollisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
                _verticalVelocity = 0f;
        }

        private void SelectLocomotionState070(bool hasInput, bool anticipatingTurn)
        {
            if (anticipatingTurn) return;

            if (hasInput)
            {
                if (_state == WorldCharacterMotionState070.Turning ||
                    _state == WorldCharacterMotionState070.Idle ||
                    _state == WorldCharacterMotionState070.Stopping)
                {
                    SetState070(WorldCharacterMotionState070.Starting);
                    return;
                }

                if (_state == WorldCharacterMotionState070.Starting &&
                    _stateElapsed < _tuning.StartAnticipationSeconds) return;

                SetState070(_runHeld
                    ? WorldCharacterMotionState070.Running
                    : WorldCharacterMotionState070.Walking);
                return;
            }

            if (PlanarSpeed070 > 0.04f)
            {
                if (_state != WorldCharacterMotionState070.Stopping)
                    SetState070(WorldCharacterMotionState070.Stopping);
                return;
            }

            if (_state != WorldCharacterMotionState070.Stopping ||
                _stateElapsed >= _tuning.StopSettleSeconds)
                SetState070(WorldCharacterMotionState070.Idle);
        }

        private void SelectPostActionState070(Vector3 desiredDirection)
        {
            if (desiredDirection.sqrMagnitude > 0.001f)
                SetState070(WorldCharacterMotionState070.Starting);
            else if (PlanarSpeed070 > 0.04f)
                SetState070(WorldCharacterMotionState070.Stopping);
            else
                SetState070(WorldCharacterMotionState070.Idle);
        }

        private void SetState070(WorldCharacterMotionState070 next)
        {
            if (_state == next) return;
            var previous = _state;
            _state = next;
            _stateElapsed = 0f;
            StateChanged070?.Invoke(previous, next);
        }
    }
}
