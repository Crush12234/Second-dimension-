using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation.GuildCity017D
{
    public enum ExpeditionStationKind070
    {
        Arrival = 0,
        Guide = 1,
        Supply = 2,
        Hazard = 3,
        Secret = 4,
        Encounter = 5,
        Objective = 6,
        Return = 7,
        CreatorRoom = 8,
        CodeConsole = 9
    }

    /// <summary>One authored point in a physically walkable expedition space.</summary>
    [Serializable]
    public sealed class ExpeditionStationDefinition070
    {
        [SerializeField] private string _stationId = string.Empty;
        [SerializeField] private string _displayName = string.Empty;
        [SerializeField] private string _interactionPrompt = string.Empty;
        [SerializeField] private ExpeditionStationKind070 _kind;
        [SerializeField] private Vector3 _localPosition;
        [SerializeField] private float _interactionRadius = 2.2f;
        [SerializeField] private bool _startsEnabled = true;

        public ExpeditionStationDefinition070()
        {
        }

        public ExpeditionStationDefinition070(
            string stationId,
            string displayName,
            string interactionPrompt,
            ExpeditionStationKind070 kind,
            Vector3 localPosition,
            float interactionRadius = 2.2f,
            bool startsEnabled = true)
        {
            _stationId = stationId;
            _displayName = displayName;
            _interactionPrompt = interactionPrompt;
            _kind = kind;
            _localPosition = localPosition;
            _interactionRadius = interactionRadius;
            _startsEnabled = startsEnabled;
        }

        public string StationId070 => _stationId ?? string.Empty;
        public string DisplayName070 => _displayName ?? string.Empty;
        public string InteractionPrompt070 => _interactionPrompt ?? string.Empty;
        public ExpeditionStationKind070 Kind070 => _kind;
        public Vector3 LocalPosition070 => _localPosition;
        public float InteractionRadius070 => _interactionRadius;
        public bool StartsEnabled070 => _startsEnabled;

        public bool TryCreateValidatedCopy070(out ExpeditionStationDefinition070 validated, out string error)
        {
            var normalizedId = NormalizeId070(_stationId);
            if (string.IsNullOrEmpty(normalizedId))
            {
                validated = null;
                error = "Station ID is required.";
                return false;
            }
            if (!IsFinite070(_localPosition))
            {
                validated = null;
                error = "Station " + normalizedId + " has a non-finite position.";
                return false;
            }

            var display = string.IsNullOrWhiteSpace(_displayName)
                ? HumanizeId070(normalizedId)
                : _displayName.Trim();
            var prompt = string.IsNullOrWhiteSpace(_interactionPrompt)
                ? "INTERACT WITH " + display.ToUpperInvariant()
                : _interactionPrompt.Trim();
            validated = new ExpeditionStationDefinition070(
                normalizedId,
                display,
                prompt,
                _kind,
                _localPosition,
                Mathf.Clamp(_interactionRadius, 0.5f, 12f),
                _startsEnabled);
            error = string.Empty;
            return true;
        }

        public static string NormalizeId070(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var input = value.Trim().ToUpperInvariant();
            var output = new char[input.Length];
            var length = 0;
            var previousUnderscore = false;
            for (var index = 0; index < input.Length; index++)
            {
                var character = input[index];
                var accepted = char.IsLetterOrDigit(character) ? character : '_';
                if (accepted == '_' && previousUnderscore) continue;
                output[length++] = accepted;
                previousUnderscore = accepted == '_';
            }
            while (length > 0 && output[length - 1] == '_') length--;
            return length > 0 ? new string(output, 0, length) : string.Empty;
        }

        private static string HumanizeId070(string value)
        {
            return string.IsNullOrEmpty(value) ? "Station" : value.Replace('_', ' ');
        }

        private static bool IsFinite070(Vector3 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        }
    }

    /// <summary>Runtime marker generated from an ExpeditionStationDefinition070.</summary>
    [DisallowMultipleComponent]
    public sealed class ExpeditionStationMarker070 : MonoBehaviour
    {
        private ExpeditionStationDefinition070 _definition;
        private SphereCollider _trigger;
        private Transform _ring;
        private Transform _beacon;
        private Renderer _ringRenderer;
        private Renderer _beaconRenderer;
        private Vector3 _ringHomeScale;
        private bool _isObjective;
        private float _phase;

        public string StationId070 => _definition != null ? _definition.StationId070 : string.Empty;
        public string DisplayName070 => _definition != null ? _definition.DisplayName070 : string.Empty;
        public string InteractionPrompt070 => _definition != null ? _definition.InteractionPrompt070 : string.Empty;
        public ExpeditionStationKind070 Kind070 => _definition != null
            ? _definition.Kind070
            : ExpeditionStationKind070.Arrival;
        public float InteractionRadius070 => _definition != null ? _definition.InteractionRadius070 : 0f;
        public bool IsObjective070 => _isObjective;
        public bool IsAvailable070 => gameObject.activeInHierarchy;

        internal void Initialize070(ExpeditionStationDefinition070 definition, bool buildVisibleMarker)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            name = "Expedition Station 070 - " + definition.StationId070;
            transform.localPosition = definition.LocalPosition070;
            gameObject.SetActive(definition.StartsEnabled070);

            _trigger = gameObject.AddComponent<SphereCollider>();
            _trigger.isTrigger = true;
            _trigger.radius = definition.InteractionRadius070;
            _phase = (StableHash070(definition.StationId070) % 628u) / 100f;
            if (buildVisibleMarker) BuildVisibleMarker070();
        }

        public void SetAvailable070(bool available)
        {
            gameObject.SetActive(available);
        }

        internal void SetObjective070(bool objective)
        {
            _isObjective = objective;
            if (_beacon != null) _beacon.gameObject.SetActive(objective);
            SetColor070(_ringRenderer, objective
                ? new Color(1f, 0.69f, 0.12f, 1f)
                : ColorForKind070(Kind070));
            if (_beaconRenderer != null)
                SetColor070(_beaconRenderer, new Color(1f, 0.75f, 0.18f, 0.82f));
        }

        private void Update()
        {
            if (_ring == null) return;
            var rate = _isObjective ? 4.5f : 2.1f;
            var amplitude = _isObjective ? 0.10f : 0.035f;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * rate + _phase) * amplitude;
            _ring.localScale = new Vector3(
                _ringHomeScale.x * pulse,
                _ringHomeScale.y,
                _ringHomeScale.z * pulse);
        }

        private void BuildVisibleMarker070()
        {
            var ringObject = CreatePrimitiveWithoutCollision070(
                PrimitiveType.Cylinder,
                "Station Floor Marker 070",
                transform);
            _ring = ringObject.transform;
            _ring.localPosition = new Vector3(0f, 0.035f, 0f);
            _ring.localScale = new Vector3(0.78f, 0.025f, 0.78f);
            _ringHomeScale = _ring.localScale;
            _ringRenderer = ringObject.GetComponent<Renderer>();

            var beaconObject = CreatePrimitiveWithoutCollision070(
                PrimitiveType.Cylinder,
                "Active Objective Beacon 070",
                transform);
            _beacon = beaconObject.transform;
            _beacon.localPosition = new Vector3(0f, 1.05f, 0f);
            _beacon.localScale = new Vector3(0.055f, 1.05f, 0.055f);
            _beaconRenderer = beaconObject.GetComponent<Renderer>();

            var labelObject = new GameObject("Station Name 070");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 1.82f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.text = DisplayName070.ToUpperInvariant();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 36;
            label.characterSize = 0.045f;
            label.color = ColorForKind070(Kind070);
            SetObjective070(false);
        }

        private static GameObject CreatePrimitiveWithoutCollision070(
            PrimitiveType type,
            string objectName,
            Transform parent)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            return primitive;
        }

        private static void SetColor070(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        private static Color ColorForKind070(ExpeditionStationKind070 kind)
        {
            switch (kind)
            {
                case ExpeditionStationKind070.Encounter: return new Color(0.92f, 0.20f, 0.16f);
                case ExpeditionStationKind070.Objective: return new Color(1f, 0.69f, 0.12f);
                case ExpeditionStationKind070.Hazard: return new Color(0.96f, 0.48f, 0.12f);
                case ExpeditionStationKind070.Secret: return new Color(0.68f, 0.35f, 0.94f);
                case ExpeditionStationKind070.CreatorRoom:
                case ExpeditionStationKind070.CodeConsole: return new Color(0.24f, 0.82f, 0.92f);
                case ExpeditionStationKind070.Return: return new Color(0.34f, 0.82f, 0.40f);
                default: return new Color(0.32f, 0.64f, 0.92f);
            }
        }

        private static uint StableHash070(string value)
        {
            var result = 2166136261u;
            value = value ?? string.Empty;
            for (var index = 0; index < value.Length; index++) result = (result ^ value[index]) * 16777619u;
            return result;
        }
    }

    /// <summary>
    /// Data-driven station registry for a continuous walkable expedition. It binds
    /// interaction input to nearby world markers and owns no quest or reward state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExpeditionSpace070 : MonoBehaviour
    {
        [SerializeField] private List<ExpeditionStationDefinition070> _stationDefinitions =
            new List<ExpeditionStationDefinition070>();
        [SerializeField] private Transform _avatar;
        [SerializeField] private WorldCharacterMotor070 _motor;
        [SerializeField] private bool _buildVisibleMarkers = true;

        private readonly List<ExpeditionStationMarker070> _markers =
            new List<ExpeditionStationMarker070>();
        private readonly Dictionary<string, ExpeditionStationMarker070> _markersById =
            new Dictionary<string, ExpeditionStationMarker070>(StringComparer.Ordinal);
        private readonly Dictionary<string, Action<ExpeditionStationMarker070>> _callbacks =
            new Dictionary<string, Action<ExpeditionStationMarker070>>(StringComparer.Ordinal);
        private string _objectiveStationId = string.Empty;
        private bool _motorSubscribed;

        public event Action<ExpeditionStationMarker070> StationInteractionRequested070;

        public int StationCount070 => _markers.Count;
        public string ObjectiveStationId070 => _objectiveStationId;
        public IReadOnlyList<ExpeditionStationMarker070> Stations070 => _markers;

        private void Awake()
        {
            if (_stationDefinitions != null && _stationDefinitions.Count > 0)
                BuildFromDefinitions070(_stationDefinitions, _buildVisibleMarkers);
            SubscribeMotor070();
        }

        private void OnEnable()
        {
            SubscribeMotor070();
        }

        private void OnDisable()
        {
            UnsubscribeMotor070();
        }

        public void Configure070(
            IEnumerable<ExpeditionStationDefinition070> stationDefinitions,
            Transform avatar,
            bool buildVisibleMarkers = true)
        {
            _avatar = avatar;
            _buildVisibleMarkers = buildVisibleMarkers;
            var requestedDefinitions = stationDefinitions != null
                ? new List<ExpeditionStationDefinition070>(stationDefinitions)
                : new List<ExpeditionStationDefinition070>();
            BuildFromDefinitions070(requestedDefinitions, buildVisibleMarkers);
        }

        public void BindMotor070(WorldCharacterMotor070 motor)
        {
            UnsubscribeMotor070();
            _motor = motor;
            if (_avatar == null && motor != null) _avatar = motor.transform;
            SubscribeMotor070();
        }

        public void SetAvatar070(Transform avatar)
        {
            _avatar = avatar;
        }

        public void RegisterInteraction070(
            string stationId,
            Action<ExpeditionStationMarker070> callback)
        {
            var normalized = ExpeditionStationDefinition070.NormalizeId070(stationId);
            if (string.IsNullOrEmpty(normalized))
                throw new ArgumentException("A station ID is required.", nameof(stationId));
            if (callback == null) _callbacks.Remove(normalized);
            else _callbacks[normalized] = callback;
        }

        public bool SetObjective070(string stationId)
        {
            var normalized = ExpeditionStationDefinition070.NormalizeId070(stationId);
            if (!_markersById.TryGetValue(normalized, out var objective) || !objective.IsAvailable070)
                return false;
            for (var index = 0; index < _markers.Count; index++)
                _markers[index].SetObjective070(_markers[index] == objective);
            _objectiveStationId = normalized;
            return true;
        }

        public bool SetStationAvailable070(string stationId, bool available)
        {
            if (!TryGetStation070(stationId, out var marker)) return false;
            marker.SetAvailable070(available);
            if (!available && marker.StationId070 == _objectiveStationId)
                _objectiveStationId = string.Empty;
            return true;
        }

        public bool TryGetStation070(string stationId, out ExpeditionStationMarker070 marker)
        {
            return _markersById.TryGetValue(
                ExpeditionStationDefinition070.NormalizeId070(stationId),
                out marker);
        }

        public bool TryGetNearestStation070(
            Vector3 worldPosition,
            bool requireInteractionRange,
            out ExpeditionStationMarker070 marker)
        {
            marker = null;
            var bestDistance = float.MaxValue;
            for (var index = 0; index < _markers.Count; index++)
            {
                var candidate = _markers[index];
                if (candidate == null || !candidate.IsAvailable070) continue;
                var delta = candidate.transform.position - worldPosition;
                delta.y = 0f;
                var distance = delta.magnitude;
                if (requireInteractionRange && distance > candidate.InteractionRadius070) continue;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                marker = candidate;
            }
            return marker != null;
        }

        public bool TryInteractNearest070(out string stationId)
        {
            stationId = string.Empty;
            if (_avatar == null ||
                !TryGetNearestStation070(_avatar.position, true, out var marker)) return false;
            stationId = marker.StationId070;
            StationInteractionRequested070?.Invoke(marker);
            if (_callbacks.TryGetValue(stationId, out var callback)) callback?.Invoke(marker);
            return true;
        }

        public bool TryGetObjectiveDirection070(out Vector3 direction, out float distance)
        {
            direction = Vector3.zero;
            distance = 0f;
            if (_avatar == null ||
                !_markersById.TryGetValue(_objectiveStationId, out var marker) ||
                !marker.IsAvailable070) return false;
            var delta = marker.transform.position - _avatar.position;
            delta.y = 0f;
            distance = delta.magnitude;
            direction = distance > 0.001f ? delta / distance : Vector3.zero;
            return true;
        }

        private void BuildFromDefinitions070(
            IEnumerable<ExpeditionStationDefinition070> definitions,
            bool buildVisibleMarkers)
        {
            var validatedDefinitions = new List<ExpeditionStationDefinition070>();
            var validatedIds = new HashSet<string>(StringComparer.Ordinal);
            if (definitions != null)
            {
                foreach (var definition in definitions)
                {
                    if (definition == null)
                        throw new ArgumentException("Invalid null expedition station definition.");
                    if (!definition.TryCreateValidatedCopy070(out var validated, out var error))
                        throw new ArgumentException(error ?? "Invalid expedition station definition.");
                    if (!validatedIds.Add(validated.StationId070))
                        throw new ArgumentException("Duplicate expedition station ID: " + validated.StationId070);
                    validatedDefinitions.Add(validated);
                }
            }

            ClearGeneratedMarkers070();
            _stationDefinitions = validatedDefinitions;
            for (var index = 0; index < validatedDefinitions.Count; index++)
            {
                var validated = validatedDefinitions[index];

                var markerObject = new GameObject("Expedition Station 070 - " + validated.StationId070);
                markerObject.transform.SetParent(transform, false);
                var marker = markerObject.AddComponent<ExpeditionStationMarker070>();
                marker.Initialize070(validated, buildVisibleMarkers);
                _markers.Add(marker);
                _markersById.Add(validated.StationId070, marker);
            }
        }

        private void ClearGeneratedMarkers070()
        {
            for (var index = 0; index < _markers.Count; index++)
            {
                var marker = _markers[index];
                if (marker == null) continue;
                marker.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(marker.gameObject);
                else DestroyImmediate(marker.gameObject);
            }
            _markers.Clear();
            _markersById.Clear();
            _objectiveStationId = string.Empty;
        }

        private void SubscribeMotor070()
        {
            if (_motorSubscribed || _motor == null || !isActiveAndEnabled) return;
            _motor.InteractionRequested070 += HandleMotorInteraction070;
            _motorSubscribed = true;
        }

        private void UnsubscribeMotor070()
        {
            if (!_motorSubscribed || _motor == null) return;
            _motor.InteractionRequested070 -= HandleMotorInteraction070;
            _motorSubscribed = false;
        }

        private void HandleMotorInteraction070()
        {
            TryInteractNearest070(out _);
        }
    }
}
