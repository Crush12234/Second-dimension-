using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.ProductionSlice014
{
    [Serializable]
    public sealed class MarenProofBeat014
    {
        public string stateName = "Idle";
        public string displayName = "IDLE";
        [Min(0.5f)] public float duration = 3f;
        public Vector3 cameraOffset = new Vector3(3.2f, 1.8f, -5f);
        public Vector3 lookAtOffset = new Vector3(0f, 1.25f, 0f);
        [Range(25f, 70f)] public float fieldOfView = 42f;
    }

    /// <summary>
    /// Lightweight presentation director for the Maren production-3D proof scene.
    /// It deliberately uses only UnityEngine APIs so it remains independent of
    /// Cinemachine, Input System, render-pipeline and UI package versions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MarenProofDirector014 : MonoBehaviour
    {
        [Header("Proof Subject")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform characterRoot;
        [SerializeField] private Camera proofCamera;

        [Header("Automatic Review")]
        [SerializeField] private List<MarenProofBeat014> beats = new List<MarenProofBeat014>();
        [SerializeField, Min(0.01f)] private float animationBlendSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float cameraSettleSeconds = 0.34f;
        [SerializeField] private bool loop = true;
        [SerializeField] private string modelMode = "PROXY";

        private int beatIndex;
        private float beatStartedAt;
        private Vector3 cameraVelocity;
        private bool cameraInitialized;
        private GUIStyle eyebrowStyle;
        private GUIStyle titleStyle;
        private GUIStyle detailStyle;

        public void Configure(
            Animator subjectAnimator,
            Transform subjectRoot,
            Camera sceneCamera,
            IEnumerable<MarenProofBeat014> reviewBeats,
            string detectedModelMode = "PROXY")
        {
            animator = subjectAnimator;
            characterRoot = subjectRoot;
            proofCamera = sceneCamera;
            beats = reviewBeats == null
                ? new List<MarenProofBeat014>()
                : new List<MarenProofBeat014>(reviewBeats);
            modelMode = string.IsNullOrWhiteSpace(detectedModelMode)
                ? "PROXY"
                : detectedModelMode.ToUpperInvariant();
        }

        private void Reset()
        {
            proofCamera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (proofCamera == null)
            {
                proofCamera = GetComponent<Camera>();
            }

            if (characterRoot == null && animator != null)
            {
                characterRoot = animator.transform;
            }

            if (beats.Count > 0)
            {
                EnterBeat(0, true);
            }
        }

        private void Update()
        {
            if (beats.Count == 0)
            {
                return;
            }

            MarenProofBeat014 activeBeat = beats[beatIndex];
            if (Time.unscaledTime - beatStartedAt < Mathf.Max(0.5f, activeBeat.duration))
            {
                return;
            }

            int nextIndex = beatIndex + 1;
            if (nextIndex >= beats.Count)
            {
                if (!loop)
                {
                    enabled = false;
                    return;
                }

                nextIndex = 0;
            }

            EnterBeat(nextIndex, false);
        }

        private void LateUpdate()
        {
            if (proofCamera == null || characterRoot == null || beats.Count == 0)
            {
                return;
            }

            MarenProofBeat014 activeBeat = beats[beatIndex];
            Vector3 desiredPosition = characterRoot.TransformPoint(activeBeat.cameraOffset);
            Vector3 lookAtPosition = characterRoot.TransformPoint(activeBeat.lookAtOffset);
            float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);

            if (!cameraInitialized)
            {
                proofCamera.transform.position = desiredPosition;
                proofCamera.fieldOfView = activeBeat.fieldOfView;
                cameraInitialized = true;
            }
            else
            {
                proofCamera.transform.position = Vector3.SmoothDamp(
                    proofCamera.transform.position,
                    desiredPosition,
                    ref cameraVelocity,
                    cameraSettleSeconds,
                    Mathf.Infinity,
                    deltaTime);
                proofCamera.fieldOfView = Mathf.Lerp(
                    proofCamera.fieldOfView,
                    activeBeat.fieldOfView,
                    1f - Mathf.Exp(-5f * deltaTime));
            }

            Vector3 viewDirection = lookAtPosition - proofCamera.transform.position;
            if (viewDirection.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(viewDirection.normalized, Vector3.up);
                proofCamera.transform.rotation = Quaternion.Slerp(
                    proofCamera.transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-8f * deltaTime));
            }
        }

        private void EnterBeat(int newIndex, bool immediate)
        {
            beatIndex = Mathf.Clamp(newIndex, 0, beats.Count - 1);
            beatStartedAt = Time.unscaledTime;
            cameraVelocity = Vector3.zero;

            if (immediate)
            {
                cameraInitialized = false;
            }

            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            MarenProofBeat014 beat = beats[beatIndex];
            int stateHash = Animator.StringToHash("Base Layer." + beat.stateName);
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, immediate ? 0f : animationBlendSeconds, 0, 0f);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || beats.Count == 0)
            {
                return;
            }

            EnsureStyles();

            float uiScale = Mathf.Clamp(Screen.height / 900f, 0.78f, 1.2f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

            float scaledWidth = Screen.width / uiScale;
            float scaledHeight = Screen.height / uiScale;
            DrawRect(new Rect(0f, 0f, scaledWidth, 18f), new Color(0.015f, 0.025f, 0.04f, 0.92f));
            DrawRect(new Rect(0f, scaledHeight - 18f, scaledWidth, 18f), new Color(0.015f, 0.025f, 0.04f, 0.92f));

            Rect panel = new Rect(28f, 40f, 342f, 104f);
            DrawRect(panel, new Color(0.025f, 0.055f, 0.09f, 0.90f));
            DrawRect(new Rect(panel.x, panel.y, 4f, panel.height), new Color(0.75f, 0.55f, 0.22f, 1f));

            MarenProofBeat014 beat = beats[beatIndex];
            string rigLabel = animator != null && animator.isHuman ? "HUMANOID" : "GENERIC";
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 13f, 300f, 18f),
                "MAREN HOLT  /  " + modelMode + " 3D",
                eyebrowStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 34f, 300f, 32f), beat.displayName, titleStyle);
            GUI.Label(
                new Rect(panel.x + 20f, panel.y + 70f, 300f, 18f),
                "AUTOMATIC " + rigLabel + " MOTION REVIEW",
                detailStyle);

            float progress = Mathf.Clamp01(
                (Time.unscaledTime - beatStartedAt) / Mathf.Max(0.5f, beat.duration));
            Rect track = new Rect(panel.x + 20f, panel.y + 92f, 300f, 2f);
            DrawRect(track, new Color(1f, 1f, 1f, 0.18f));
            DrawRect(new Rect(track.x, track.y, track.width * progress, track.height), new Color(0.75f, 0.55f, 0.22f, 1f));

            GUI.Label(
                new Rect(scaledWidth - 250f, scaledHeight - 43f, 220f, 20f),
                "UPDATE 014.5  ·  FBX / " + rigLabel,
                detailStyle);

            GUI.matrix = previousMatrix;
        }

        private void EnsureStyles()
        {
            if (eyebrowStyle != null)
            {
                return;
            }

            eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.55f, 0.22f, 1f) }
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.96f, 0.98f, 1f) }
            };
            detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.67f, 0.72f, 0.78f, 1f) }
            };
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }
    }
}
