using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation.GuildCity017D
{
    /// <summary>
    /// Bridges WorldCharacterMotor070 to an authored Animator when available and
    /// supplies a restrained procedural gait while art clips are being produced.
    /// The fallback has readable steps, weight transfer, start/stop anticipation,
    /// turn, dodge, and interaction poses instead of moving a rigid cutout.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldCharacterAnimator070 : MonoBehaviour
    {
        private static readonly int MotionStateHash070 = Animator.StringToHash("MotionState");
        private static readonly int SpeedHash070 = Animator.StringToHash("Speed");
        private static readonly int GroundedHash070 = Animator.StringToHash("Grounded");
        private static readonly int DodgeTimeHash070 = Animator.StringToHash("DodgeTime");

        [SerializeField] private WorldCharacterMotor070 _motor;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Transform _leftFoot;
        [SerializeField] private Transform _rightFoot;
        [SerializeField] private bool _automaticTick = true;
        [SerializeField] private bool _proceduralFallback = true;

        private Vector3 _visualHomePosition;
        private Quaternion _visualHomeRotation = Quaternion.identity;
        private Vector3 _visualHomeScale = Vector3.one;
        private Vector3 _leftFootHome;
        private Vector3 _rightFootHome;
        private SpriteRenderer[] _spriteRenderers = Array.Empty<SpriteRenderer>();
        private readonly List<Sprite> _runtimePoseSprites070 = new List<Sprite>();
        private SpriteRenderer _poseRenderer070;
        private Sprite _originalSprite070;
        private Sprite _idlePose070;
        private Sprite _anticipationPose070;
        private Sprite _recoveryPose070;
        private Sprite _actionPose070;
        private Sprite _rolePose070;
        private Vector3 _poseRendererHomeScale070 = Vector3.one;
        private float _poseReferenceHeight070 = 1f;
        private float _locomotionPhase;
        private float _stateTime;
        private WorldCharacterMotionState070 _state;
        private bool _hasStateParameter;
        private bool _hasSpeedParameter;
        private bool _hasGroundedParameter;
        private bool _hasDodgeTimeParameter;
        private bool _subscribed;

        public WorldCharacterMotionState070 MotionState070 => _state;
        public float LocomotionPhase070 => _locomotionPhase;
        public bool HasVisibleGait070 { get; private set; }
        public Transform VisualRoot070 => _visualRoot;
        public bool UsesSpritePoses070 => _poseRenderer070 != null && _idlePose070 != null;

        private void Awake()
        {
            ResolveReferences070();
            CaptureHomePose070();
            CacheAnimatorParameters070();
            SyncState070();
        }

        private void OnEnable()
        {
            ResolveReferences070();
            Subscribe070();
        }

        private void OnDisable()
        {
            Unsubscribe070();
            RestoreHomePose070();
        }

        private void OnDestroy()
        {
            ClearSpritePoses070();
        }

        private void LateUpdate()
        {
            if (_automaticTick) Tick070(Time.unscaledDeltaTime);
        }

        /// <summary>Explicit binding seam for runtime-built character prefabs.</summary>
        public void Configure070(
            WorldCharacterMotor070 motor,
            Transform visualRoot,
            Animator animator = null,
            Transform leftFoot = null,
            Transform rightFoot = null,
            bool automaticTick = true,
            bool proceduralFallback = true)
        {
            Unsubscribe070();
            RestoreHomePose070();
            _motor = motor;
            _visualRoot = visualRoot;
            _animator = animator;
            _leftFoot = leftFoot;
            _rightFoot = rightFoot;
            _automaticTick = automaticTick;
            _proceduralFallback = proceduralFallback;
            ResolveReferences070();
            CaptureHomePose070();
            CacheAnimatorParameters070();
            SyncState070();
            Subscribe070();
        }

        /// <summary>
        /// Binds the existing transparent battle poses to world locomotion. The
        /// motor remains authoritative; these sprites only make start, stride,
        /// stop, dodge, and interaction states visually distinct.
        /// </summary>
        public bool ConfigureSpritePoses070(string characterPoseResourceRoot)
        {
            ClearSpritePoses070();
            if (string.IsNullOrWhiteSpace(characterPoseResourceRoot)) return false;
            ResolveReferences070();
            if (_spriteRenderers.Length == 0) return false;

            _poseRenderer070 = _spriteRenderers[0];
            _originalSprite070 = _poseRenderer070.sprite;
            _poseRendererHomeScale070 = _poseRenderer070.transform.localScale;
            _poseReferenceHeight070 = _originalSprite070 != null
                ? Mathf.Max(0.01f, _originalSprite070.bounds.size.y)
                : 1f;

            _idlePose070 = LoadPose070(characterPoseResourceRoot, "POSE_IDLE");
            _anticipationPose070 = LoadPose070(characterPoseResourceRoot, "POSE_ANTICIPATION");
            _recoveryPose070 = LoadPose070(characterPoseResourceRoot, "POSE_RECOVERY");
            _actionPose070 = LoadPose070(characterPoseResourceRoot, "POSE_ACTION_PRIMARY");
            _rolePose070 = LoadPose070(characterPoseResourceRoot, "POSE_ROLE_PRIMARY");
            if (_idlePose070 == null)
            {
                ClearSpritePoses070();
                return false;
            }

            ApplyPoseSprite070(_idlePose070);
            return true;
        }

        /// <summary>Advances presentation without changing authoritative world position.</summary>
        public void Tick070(float deltaTime)
        {
            if (_motor == null || _visualRoot == null) return;
            var safeDelta = Mathf.Clamp(deltaTime, 0f, 0.1f);
            _stateTime += safeDelta;
            _state = _motor.MotionState070;

            var speed = _motor.NormalizedSpeed070;
            var gaitRate = Mathf.Lerp(2.4f, 9.5f, speed);
            if (_state == WorldCharacterMotionState070.Walking ||
                _state == WorldCharacterMotionState070.Running ||
                _state == WorldCharacterMotionState070.Starting)
                _locomotionPhase += safeDelta * gaitRate * Mathf.PI * 2f;

            ApplyAnimatorParameters070(speed);
            ApplySpritePose070();
            if (_proceduralFallback) ApplyProceduralPose070(speed);
            UpdateSpriteFacing070();
        }

        private void ResolveReferences070()
        {
            if (_motor == null) _motor = GetComponent<WorldCharacterMotor070>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>(true);
            if (_visualRoot == null)
            {
                if (_animator != null && _animator.transform != transform)
                    _visualRoot = _animator.transform;
                else if (transform.childCount > 0)
                    _visualRoot = transform.GetChild(0);
            }
            _spriteRenderers = _visualRoot != null
                ? _visualRoot.GetComponentsInChildren<SpriteRenderer>(true)
                : Array.Empty<SpriteRenderer>();
        }

        private void CaptureHomePose070()
        {
            if (_visualRoot != null)
            {
                _visualHomePosition = _visualRoot.localPosition;
                _visualHomeRotation = _visualRoot.localRotation;
                _visualHomeScale = _visualRoot.localScale;
            }
            if (_leftFoot != null) _leftFootHome = _leftFoot.localPosition;
            if (_rightFoot != null) _rightFootHome = _rightFoot.localPosition;
        }

        private void RestoreHomePose070()
        {
            if (_visualRoot != null)
            {
                _visualRoot.localPosition = _visualHomePosition;
                _visualRoot.localRotation = _visualHomeRotation;
                _visualRoot.localScale = _visualHomeScale;
            }
            if (_leftFoot != null) _leftFoot.localPosition = _leftFootHome;
            if (_rightFoot != null) _rightFoot.localPosition = _rightFootHome;
            HasVisibleGait070 = false;
        }

        private void Subscribe070()
        {
            if (_subscribed || _motor == null) return;
            _motor.StateChanged070 += HandleStateChanged070;
            _subscribed = true;
        }

        private void Unsubscribe070()
        {
            if (!_subscribed || _motor == null) return;
            _motor.StateChanged070 -= HandleStateChanged070;
            _subscribed = false;
        }

        private void HandleStateChanged070(
            WorldCharacterMotionState070 previous,
            WorldCharacterMotionState070 next)
        {
            _state = next;
            _stateTime = 0f;
            if (next == WorldCharacterMotionState070.Idle) _locomotionPhase = 0f;
        }

        private void SyncState070()
        {
            _state = _motor != null ? _motor.MotionState070 : WorldCharacterMotionState070.Idle;
            _stateTime = 0f;
        }

        private void CacheAnimatorParameters070()
        {
            _hasStateParameter = false;
            _hasSpeedParameter = false;
            _hasGroundedParameter = false;
            _hasDodgeTimeParameter = false;
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            var parameters = _animator.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (parameter.nameHash == MotionStateHash070 && parameter.type == AnimatorControllerParameterType.Int)
                    _hasStateParameter = true;
                else if (parameter.nameHash == SpeedHash070 && parameter.type == AnimatorControllerParameterType.Float)
                    _hasSpeedParameter = true;
                else if (parameter.nameHash == GroundedHash070 && parameter.type == AnimatorControllerParameterType.Bool)
                    _hasGroundedParameter = true;
                else if (parameter.nameHash == DodgeTimeHash070 && parameter.type == AnimatorControllerParameterType.Float)
                    _hasDodgeTimeParameter = true;
            }
        }

        private void ApplyAnimatorParameters070(float speed)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null) return;
            if (_hasStateParameter) _animator.SetInteger(MotionStateHash070, (int)_state);
            if (_hasSpeedParameter) _animator.SetFloat(SpeedHash070, speed, 0.08f, Time.unscaledDeltaTime);
            if (_hasGroundedParameter) _animator.SetBool(GroundedHash070, _motor.IsGrounded070);
            if (_hasDodgeTimeParameter) _animator.SetFloat(DodgeTimeHash070, _motor.DodgeNormalizedTime070);
        }

        private void ApplyProceduralPose070(float speed)
        {
            var offset = Vector3.zero;
            var euler = Vector3.zero;
            var scale = Vector3.one;
            var footLift = 0f;
            var footTravel = 0f;

            switch (_state)
            {
                case WorldCharacterMotionState070.Idle:
                {
                    var breathe = Mathf.Sin(_stateTime * 2.1f);
                    offset.y = breathe * 0.012f;
                    scale.y = 1f + breathe * 0.006f;
                    scale.x = 1f - breathe * 0.003f;
                    break;
                }
                case WorldCharacterMotionState070.Starting:
                {
                    var settle = Mathf.Clamp01(_stateTime / 0.12f);
                    euler.x = Mathf.Lerp(8f, 3f, settle);
                    scale.y = Mathf.Lerp(0.96f, 1f, settle);
                    footLift = 0.035f * settle;
                    footTravel = 0.06f * settle;
                    break;
                }
                case WorldCharacterMotionState070.Walking:
                {
                    var step = Mathf.Sin(_locomotionPhase);
                    offset.y = Mathf.Abs(step) * 0.055f;
                    euler.z = step * 1.6f;
                    euler.x = -1.5f;
                    footLift = 0.09f;
                    footTravel = 0.14f;
                    break;
                }
                case WorldCharacterMotionState070.Running:
                {
                    var stride = Mathf.Sin(_locomotionPhase);
                    offset.y = Mathf.Abs(stride) * 0.085f;
                    euler.z = stride * 2.6f;
                    euler.x = -6f;
                    scale.y = 1f + Mathf.Abs(stride) * 0.025f;
                    scale.x = 1f - Mathf.Abs(stride) * 0.012f;
                    footLift = 0.15f;
                    footTravel = 0.23f;
                    break;
                }
                case WorldCharacterMotionState070.Stopping:
                {
                    var settle = 1f - Mathf.Clamp01(_stateTime / 0.16f);
                    euler.x = -5f * settle;
                    offset.y = 0.025f * settle;
                    scale.y = 1f - 0.025f * settle;
                    scale.x = 1f + 0.014f * settle;
                    break;
                }
                case WorldCharacterMotionState070.Turning:
                {
                    var anticipation = Mathf.Sin(Mathf.Clamp01(_stateTime / 0.12f) * Mathf.PI);
                    euler.z = -7f * anticipation;
                    offset.y = -0.035f * anticipation;
                    scale.y = 1f - 0.055f * anticipation;
                    scale.x = 1f + 0.035f * anticipation;
                    break;
                }
                case WorldCharacterMotionState070.Dodging:
                {
                    var dodge = _motor.DodgeNormalizedTime070;
                    var arc = Mathf.Sin(dodge * Mathf.PI);
                    euler.x = -14f + dodge * 8f;
                    euler.z = -10f * arc;
                    offset.y = -0.12f + arc * 0.08f;
                    scale.y = 0.84f + arc * 0.06f;
                    scale.x = 1.12f - arc * 0.04f;
                    break;
                }
                case WorldCharacterMotionState070.Interacting:
                {
                    var gesture = Mathf.Sin(Mathf.Clamp01(_stateTime / 0.24f) * Mathf.PI);
                    euler.x = 3f * gesture;
                    offset.y = 0.018f * gesture;
                    scale.y = 1f - 0.015f * gesture;
                    scale.x = 1f + 0.008f * gesture;
                    break;
                }
            }

            if (UsesSpritePoses070)
            {
                // The authored silhouettes supply the readable pose change. Keep
                // only a hint of weight transfer so the art does not wobble like
                // a paper cutout while traversing the room.
                offset *= 0.28f;
                euler *= 0.32f;
                scale = Vector3.Lerp(Vector3.one, scale, 0.28f);
            }

            _visualRoot.localPosition = _visualHomePosition + offset;
            _visualRoot.localRotation = _visualHomeRotation * Quaternion.Euler(euler);
            _visualRoot.localScale = Vector3.Scale(_visualHomeScale, scale);
            ApplyFootPose070(footLift, footTravel);
            HasVisibleGait070 = _state != WorldCharacterMotionState070.Idle ||
                                Mathf.Abs(offset.y) > 0.001f ||
                                speed > 0.01f;
        }

        private void ApplyFootPose070(float lift, float travel)
        {
            if (_leftFoot == null && _rightFoot == null) return;
            var stride = Mathf.Sin(_locomotionPhase);
            var leftLift = Mathf.Max(0f, stride) * lift;
            var rightLift = Mathf.Max(0f, -stride) * lift;
            if (_leftFoot != null)
                _leftFoot.localPosition = _leftFootHome + new Vector3(0f, leftLift, stride * travel);
            if (_rightFoot != null)
                _rightFoot.localPosition = _rightFootHome + new Vector3(0f, rightLift, -stride * travel);
        }

        private void UpdateSpriteFacing070()
        {
            if (_motor == null || Mathf.Abs(_motor.FacingDirection070.x) < 0.05f) return;
            var faceLeft = _motor.FacingDirection070.x < 0f;
            for (var index = 0; index < _spriteRenderers.Length; index++)
            {
                var renderer = _spriteRenderers[index];
                if (renderer != null) renderer.flipX = faceLeft;
            }
        }

        private void ApplySpritePose070()
        {
            if (!UsesSpritePoses070) return;
            Sprite requested;
            switch (_state)
            {
                case WorldCharacterMotionState070.Starting:
                case WorldCharacterMotionState070.Turning:
                    requested = _anticipationPose070 ?? _idlePose070;
                    break;
                case WorldCharacterMotionState070.Walking:
                    requested = Mathf.Sin(_locomotionPhase) >= 0f
                        ? _idlePose070
                        : _recoveryPose070 ?? _idlePose070;
                    break;
                case WorldCharacterMotionState070.Running:
                    requested = Mathf.Sin(_locomotionPhase) >= 0f
                        ? _anticipationPose070 ?? _idlePose070
                        : _recoveryPose070 ?? _idlePose070;
                    break;
                case WorldCharacterMotionState070.Stopping:
                    requested = _recoveryPose070 ?? _idlePose070;
                    break;
                case WorldCharacterMotionState070.Dodging:
                    requested = _actionPose070 ?? _anticipationPose070 ?? _idlePose070;
                    break;
                case WorldCharacterMotionState070.Interacting:
                    requested = _rolePose070 ?? _actionPose070 ?? _idlePose070;
                    break;
                default:
                    requested = _idlePose070;
                    break;
            }
            ApplyPoseSprite070(requested);
        }

        private void ApplyPoseSprite070(Sprite sprite)
        {
            if (_poseRenderer070 == null || sprite == null || _poseRenderer070.sprite == sprite) return;
            _poseRenderer070.sprite = sprite;
            var requestedHeight = Mathf.Max(0.01f, sprite.bounds.size.y);
            _poseRenderer070.transform.localScale = _poseRendererHomeScale070 *
                                                     (_poseReferenceHeight070 / requestedHeight);
        }

        private Sprite LoadPose070(string resourceRoot, string poseName)
        {
            var texture = Resources.Load<Texture2D>(resourceRoot.TrimEnd('/') + "/" + poseName);
            if (texture == null) return null;
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.02f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "_WORLD_POSE_070";
            _runtimePoseSprites070.Add(sprite);
            return sprite;
        }

        private void ClearSpritePoses070()
        {
            if (_poseRenderer070 != null)
            {
                _poseRenderer070.sprite = _originalSprite070;
                _poseRenderer070.transform.localScale = _poseRendererHomeScale070;
            }
            for (var index = 0; index < _runtimePoseSprites070.Count; index++)
            {
                var sprite = _runtimePoseSprites070[index];
                if (sprite == null) continue;
                if (Application.isPlaying) Destroy(sprite);
                else DestroyImmediate(sprite);
            }
            _runtimePoseSprites070.Clear();
            _poseRenderer070 = null;
            _originalSprite070 = null;
            _idlePose070 = null;
            _anticipationPose070 = null;
            _recoveryPose070 = null;
            _actionPose070 = null;
            _rolePose070 = null;
            _poseRendererHomeScale070 = Vector3.one;
            _poseReferenceHeight070 = 1f;
        }
    }
}
