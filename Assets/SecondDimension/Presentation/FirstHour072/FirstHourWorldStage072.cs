using System;
using UnityEngine;

namespace SecondDimension.Presentation.FirstHour072
{
    /// <summary>
    /// The authored first-hour expedition is presented as a sequence of small,
    /// readable rooms. This helper owns only presentation: a world-anchored plate,
    /// collision boundaries matched to its foreground lane, and a restrained
    /// dead-zone camera. Quest state and transitions remain with the M1 services.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstHourWorldStage072 : MonoBehaviour
    {
        private Camera _camera;
        private Transform _focus;
        private Transform _roomRoot;
        private Transform _backdrop;
        private Material _backdropMaterial;
        private Vector3 _backdropHome;
        private Vector2 _xBounds = new Vector2(-6.8f, 6.8f);
        private Vector2 _zBounds = new Vector2(-2.1f, 2.6f);
        private Vector3 _cameraHome;
        private Quaternion _cameraRotation = Quaternion.identity;
        private Vector3 _cameraVelocity;
        private float _horizontalDeadZone = 1.15f;
        private float _maximumHorizontalFollow = 0.72f;
        private float _backdropParallaxFactor = 0.24f;
        private float _backdropParallaxTravel;
        private int _worldLayer;

        public Transform Backdrop072 => _backdrop;
        public Vector2 HorizontalBounds072 => _xBounds;
        public Vector2 DepthBounds072 => _zBounds;
        public bool HasWorldAnchoredBackdrop072 =>
            _backdrop != null && _camera != null && _backdrop.parent != _camera.transform;
        public bool UsesBackdropParallax072 =>
            HasWorldAnchoredBackdrop072 && _backdropParallaxFactor > 0f;
        public float BackdropParallaxTravel072 => _backdropParallaxTravel;
        public float BackdropWorldWidth072 =>
            _backdrop == null ? 0f : Mathf.Abs(_backdrop.lossyScale.x);

        public bool CoversViewportAtAspect072(float aspect)
        {
            if (_backdrop == null || _camera == null) return false;
            var requestedAspect = Mathf.Max(1f, aspect);
            var requiredWidth = _camera.orthographicSize * 2f * requestedAspect;
            return BackdropWorldWidth072 >= requiredWidth * 1.02f;
        }

        public void Configure072(
            Camera worldCamera,
            Transform focus,
            int worldLayer,
            string backdropResource,
            string fallbackResource,
            Vector2 horizontalBounds,
            Vector2 depthBounds,
            Vector3 cameraPosition,
            Vector3 cameraLookTarget,
            float orthographicSize = 5.45f,
            float horizontalDeadZone = 1.15f,
            float maximumHorizontalFollow = 0.72f,
            float backdropParallaxFactor = 0.24f)
        {
            if (worldCamera == null) throw new ArgumentNullException(nameof(worldCamera));
            if (focus == null) throw new ArgumentNullException(nameof(focus));

            ClearRoom072();
            _camera = worldCamera;
            _focus = focus;
            _worldLayer = Mathf.Clamp(worldLayer, 0, 31);
            _xBounds = OrderedBounds072(horizontalBounds, -6.8f, 6.8f);
            _zBounds = OrderedBounds072(depthBounds, -2.1f, 2.6f);
            _horizontalDeadZone = Mathf.Max(0f, horizontalDeadZone);
            _maximumHorizontalFollow = Mathf.Max(0f, maximumHorizontalFollow);
            _backdropParallaxFactor = Mathf.Clamp(backdropParallaxFactor, 0f, 0.65f);
            _backdropParallaxTravel = 0f;
            _cameraVelocity = Vector3.zero;

            var room = new GameObject("First Hour Authored Room 072");
            room.layer = _worldLayer;
            room.transform.SetParent(transform, false);
            _roomRoot = room.transform;

            ConfigureCamera072(cameraPosition, cameraLookTarget, orthographicSize);
            BuildInvisibleCollisionLane072();
            BuildWorldAnchoredBackdrop072(backdropResource, fallbackResource);
        }

        public void TickCamera072(float deltaTime)
        {
            if (_camera == null || _focus == null) return;
            var center = (_xBounds.x + _xBounds.y) * 0.5f;
            var horizontal = _focus.position.x - center;
            var magnitudePastDeadZone = Mathf.Max(0f, Mathf.Abs(horizontal) - _horizontalDeadZone);
            var follow = Mathf.Sign(horizontal) * Mathf.Min(
                _maximumHorizontalFollow,
                magnitudePastDeadZone * 0.22f);
            var target = _cameraHome + Vector3.right * follow;
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                target,
                ref _cameraVelocity,
                0.18f,
                8f,
                Mathf.Clamp(deltaTime, 0.001f, 0.1f));
            _camera.transform.rotation = _cameraRotation;
            if (_backdrop != null)
            {
                var cameraTravel = Vector3.Dot(
                    _camera.transform.position - _cameraHome,
                    Vector3.right);
                var parallax = cameraTravel * _backdropParallaxFactor;
                _backdrop.position = _backdropHome + Vector3.right * parallax;
                _backdropParallaxTravel = Mathf.Abs(parallax);
            }
        }

        public Vector3 ClampToLane072(Vector3 worldPosition)
        {
            worldPosition.x = Mathf.Clamp(worldPosition.x, _xBounds.x, _xBounds.y);
            worldPosition.z = Mathf.Clamp(worldPosition.z, _zBounds.x, _zBounds.y);
            return worldPosition;
        }

        public bool Contains072(Vector3 worldPosition, float tolerance = 0.05f) =>
            worldPosition.x >= _xBounds.x - tolerance &&
            worldPosition.x <= _xBounds.y + tolerance &&
            worldPosition.z >= _zBounds.x - tolerance &&
            worldPosition.z <= _zBounds.y + tolerance;

        private void ConfigureCamera072(
            Vector3 cameraPosition,
            Vector3 cameraLookTarget,
            float orthographicSize)
        {
            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Clamp(orthographicSize, 3.5f, 9f);
            _camera.fieldOfView = 52f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 120f;
            _camera.transform.position = cameraPosition;
            _camera.transform.LookAt(cameraLookTarget);
            _cameraHome = cameraPosition;
            _cameraRotation = _camera.transform.rotation;
        }

        private void BuildInvisibleCollisionLane072()
        {
            var centerX = (_xBounds.x + _xBounds.y) * 0.5f;
            var centerZ = (_zBounds.x + _zBounds.y) * 0.5f;
            var width = _xBounds.y - _xBounds.x;
            var depth = _zBounds.y - _zBounds.x;
            CreateCollider072(
                "First Hour Walkable Floor 072",
                new Vector3(centerX, -0.36f, centerZ),
                new Vector3(width + 1.2f, 0.7f, depth + 1.2f));
            CreateCollider072(
                "First Hour Lane Left Boundary 072",
                new Vector3(_xBounds.x - 0.28f, 1.6f, centerZ),
                new Vector3(0.55f, 4f, depth + 1.1f));
            CreateCollider072(
                "First Hour Lane Right Boundary 072",
                new Vector3(_xBounds.y + 0.28f, 1.6f, centerZ),
                new Vector3(0.55f, 4f, depth + 1.1f));
            CreateCollider072(
                "First Hour Lane Near Boundary 072",
                new Vector3(centerX, 1.6f, _zBounds.x - 0.28f),
                new Vector3(width + 1.1f, 4f, 0.55f));
            CreateCollider072(
                "First Hour Lane Far Boundary 072",
                new Vector3(centerX, 1.6f, _zBounds.y + 0.28f),
                new Vector3(width + 1.1f, 4f, 0.55f));
        }

        private void CreateCollider072(string name, Vector3 position, Vector3 size)
        {
            var item = new GameObject(name);
            item.layer = _worldLayer;
            item.transform.SetParent(_roomRoot, false);
            item.transform.position = position;
            var collider = item.AddComponent<BoxCollider>();
            collider.size = size;
        }

        private void BuildWorldAnchoredBackdrop072(string resource, string fallbackResource)
        {
            var texture = LoadTexture072(resource) ?? LoadTexture072(fallbackResource);
            if (texture == null) return;

            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdrop.name = "Gateworks Painted Skyline 068";
            backdrop.layer = _worldLayer;
            backdrop.transform.SetParent(_roomRoot, false);
            var collider = backdrop.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }

            var shader = Shader.Find("Unlit/Texture") ??
                         Shader.Find("Sprites/Default") ??
                         Shader.Find("Standard");
            if (shader == null) return;
            _backdropMaterial = new Material(shader)
            {
                mainTexture = texture,
                // The Lantern Road plate was authored for a cinematic grade. A
                // modest lift keeps routes, silhouettes, and landmarks readable
                // on ordinary monitors without flattening the night palette.
                color = new Color(1.12f, 1.09f, 1.04f, 1f)
            };
            if (_backdropMaterial.HasProperty("_Cull")) _backdropMaterial.SetInt("_Cull", 0);
            backdrop.GetComponent<Renderer>().sharedMaterial = _backdropMaterial;

            const float backdropDistance = 54f;
            var viewHeight = _camera.orthographicSize * 2f;
            var textureAspect = (float)Mathf.Max(1, texture.width) / Mathf.Max(1, texture.height);
            var requiredAspect = Mathf.Max(textureAspect, Mathf.Max(1f, _camera.aspect));
            backdrop.transform.position = _cameraHome + _camera.transform.forward * backdropDistance;
            backdrop.transform.rotation = _cameraRotation;
            backdrop.transform.localScale = new Vector3(
                viewHeight * requiredAspect * 1.32f,
                viewHeight * 1.32f,
                1f);
            _backdrop = backdrop.transform;
            _backdropHome = backdrop.transform.position;
        }

        private static Texture2D LoadTexture072(string resource)
        {
            return string.IsNullOrWhiteSpace(resource)
                ? null
                : Resources.Load<Texture2D>(resource);
        }

        private static Vector2 OrderedBounds072(Vector2 requested, float fallbackMin, float fallbackMax)
        {
            if (float.IsNaN(requested.x) || float.IsInfinity(requested.x) ||
                float.IsNaN(requested.y) || float.IsInfinity(requested.y) ||
                requested.y - requested.x < 1f)
                return new Vector2(fallbackMin, fallbackMax);
            return requested.x <= requested.y
                ? requested
                : new Vector2(requested.y, requested.x);
        }

        private void ClearRoom072()
        {
            if (_roomRoot != null)
            {
                _roomRoot.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(_roomRoot.gameObject);
                else DestroyImmediate(_roomRoot.gameObject);
                _roomRoot = null;
            }
            if (_backdropMaterial != null)
            {
                if (Application.isPlaying) Destroy(_backdropMaterial);
                else DestroyImmediate(_backdropMaterial);
                _backdropMaterial = null;
            }
            _backdrop = null;
            _backdropHome = Vector3.zero;
            _backdropParallaxTravel = 0f;
        }

        private void OnDestroy()
        {
            ClearRoom072();
        }
    }
}
