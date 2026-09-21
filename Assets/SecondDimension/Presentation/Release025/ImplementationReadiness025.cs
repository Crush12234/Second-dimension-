using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.Release024;
using UnityEngine;

namespace SecondDimension.Presentation.Release025
{
    [Serializable]
    public sealed class ImplementationReadinessSnapshot025
    {
        public bool IsReady;
        public string Error;
        public string UnityVersion;
        public int SaveFormatVersion;
        public int OwnerReviewCases;
        public int WorldGateBoards;
        public int WorldGateNodes;
        public int AlliedUnionCapacity;
        public int EnemyUnionCapacity;
        public bool BuiltInFontReady;
        public bool EmbeddedUiFontAbsent;
        public bool EmbeddedDisplayFontAbsent;
        public string[] Issues = Array.Empty<string>();
        public string[] SummaryLines = Array.Empty<string>();
    }

    public static class ImplementationReadinessService025
    {
        public static ImplementationReadinessSnapshot025 BuildSnapshot()
        {
            var issues = new List<string>();
            try
            {
                var registry = ImplementationHardeningRegistry025.LoadFromResources();
                var ownerReview = FullGameIntegrationRegistry024.LoadFromResources();
                var integration = FullGameIntegrationHealthService024.BuildSnapshot();
                if (!integration.IsReady)
                    issues.Add("Final Integration 024 health failed: " + integration.Error);

                var builtIn = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                var embeddedUi = Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionUISans");
                var embeddedDisplay = Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionDisplay");
                if (builtIn == null)
                    issues.Add("Unity built-in runtime font is unavailable.");
                if (embeddedUi != null || embeddedDisplay != null)
                    issues.Add("Embedded font binaries remain in the active Resources tree.");
                if (!StringComparer.Ordinal.Equals(Application.unityVersion, registry.Manifest.unityVersion))
                    issues.Add(
                        "Unity version mismatch. Expected " + registry.Manifest.unityVersion +
                        ", running " + Application.unityVersion + ".");

                var snapshot = new ImplementationReadinessSnapshot025
                {
                    IsReady = issues.Count == 0,
                    Error = issues.Count == 0 ? string.Empty : string.Join("\n", issues),
                    UnityVersion = Application.unityVersion,
                    SaveFormatVersion = SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion,
                    OwnerReviewCases = ownerReview.SmokeCases.Count,
                    WorldGateBoards = integration.WorldGateBoards,
                    WorldGateNodes = integration.WorldGateNodes,
                    AlliedUnionCapacity = integration.AlliedUnionCapacity,
                    EnemyUnionCapacity = integration.EnemyUnionCapacity,
                    BuiltInFontReady = builtIn != null,
                    EmbeddedUiFontAbsent = embeddedUi == null,
                    EmbeddedDisplayFontAbsent = embeddedDisplay == null,
                    Issues = issues.ToArray()
                };
                snapshot.SummaryLines = new[]
                {
                    "UNITY " + snapshot.UnityVersion + " • SAVE v" + snapshot.SaveFormatVersion,
                    snapshot.OwnerReviewCases + " HARDENED OWNER-REVIEW CASES",
                    snapshot.WorldGateBoards + " WORLD GATE BOARDS • " + snapshot.WorldGateNodes + " NODES",
                    snapshot.AlliedUnionCapacity + " ALLIED / " + snapshot.EnemyUnionCapacity + " ENEMY UNION CAPACITY",
                    "BUILT-IN FONT " + (snapshot.BuiltInFontReady ? "READY" : "MISSING") +
                    " • EMBEDDED FONT BINARIES " +
                    (snapshot.EmbeddedUiFontAbsent && snapshot.EmbeddedDisplayFontAbsent ? "REMOVED" : "PRESENT")
                };
                return snapshot;
            }
            catch (Exception exception)
            {
                return new ImplementationReadinessSnapshot025
                {
                    IsReady = false,
                    Error = exception.ToString(),
                    Issues = new[] { exception.Message },
                    SummaryLines = Array.Empty<string>()
                };
            }
        }
    }

    public sealed class ImplementationHardeningDashboard025 : MonoBehaviour
    {
        private Vector2 _scroll;
        private ImplementationHardeningRegistry025 _registry;
        private FullGameIntegrationRegistry024 _ownerReview;
        private ImplementationReadinessSnapshot025 _snapshot;

        public bool IsReady { get; private set; }
        public string Error { get; private set; }

        private void Awake()
        {
            _registry = ImplementationHardeningRegistry025.LoadFromResources();
            _ownerReview = FullGameIntegrationRegistry024.LoadFromResources();
            _snapshot = ImplementationReadinessService025.BuildSnapshot();
            IsReady = _snapshot.IsReady;
            Error = _snapshot.Error ?? string.Empty;
        }

        private void OnGUI()
        {
            GUI.Box(
                new Rect(18, 18, Screen.width - 36, Screen.height - 36),
                "SECOND DIMENSION — FINAL IMPLEMENTATION HARDENING 025");
            GUILayout.BeginArea(new Rect(42, 62, Screen.width - 84, Screen.height - 102));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(IsReady ? "IMPLEMENTATION READY FOR UNITY EXECUTION" : "IMPLEMENTATION BLOCKED");
            if (!string.IsNullOrWhiteSpace(Error))
                GUILayout.TextArea(Error);
            if (_snapshot != null && _snapshot.SummaryLines != null)
                foreach (var line in _snapshot.SummaryLines)
                    GUILayout.Label(line);
            GUILayout.Space(12);
            if (_registry != null && _ownerReview != null)
            {
                GUILayout.Label("HARDENED OWNER-REVIEW MATRIX — " + _ownerReview.SmokeCases.Count + " CASES");
                foreach (var smoke in _ownerReview.SmokeCases.Values.OrderBy(value => value.caseId))
                    GUILayout.Label(smoke.caseId + " • " + smoke.category + "\n" + smoke.expectedEvidence);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
