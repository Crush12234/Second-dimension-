using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation.Release023
{
    public sealed class FinalGameSmokeDashboard023 : MonoBehaviour
    {
        Vector2 _scroll;
        FinalReleaseRegistry023 _registry;
        FinalReleaseContentSnapshot023 _snapshot;
        public bool IsReady { get; private set; }
        public string Error { get; private set; }

        void Awake()
        {
            try
            {
                _registry = FinalReleaseRegistry023.LoadFromResources();
                _snapshot = FinalReleaseReadinessService023.BuildSnapshot();
                IsReady = _snapshot.IsAvailable;
                Error = _snapshot.Error ?? string.Empty;
            }
            catch (System.Exception exception)
            {
                IsReady = false;
                Error = exception.ToString();
            }
        }

        void OnGUI()
        {
            GUI.Box(new Rect(18, 18, Screen.width - 36, Screen.height - 36), "SECOND DIMENSION — FINAL GAME RELEASE CANDIDATE 023");
            GUILayout.BeginArea(new Rect(42, 62, Screen.width - 84, Screen.height - 102));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(IsReady ? "STATIC CONTENT READY — UNITY EXECUTION GATES REMAIN" : "RELEASE CONTENT ERROR");
            if (!string.IsNullOrWhiteSpace(Error)) GUILayout.TextArea(Error);
            if (_snapshot != null && _snapshot.SummaryLines != null)
                foreach (var line in _snapshot.SummaryLines) GUILayout.Label(line);
            GUILayout.Space(12);
            if (_registry != null)
            {
                GUILayout.Label("OWNER-REVIEW SMOKE MATRIX — " + _registry.SmokeCases.Count + " CASES");
                foreach (var smoke in _registry.SmokeCases.Values.OrderBy(value => value.caseId))
                    GUILayout.Label(smoke.caseId + " • " + smoke.category + " • " + smoke.title + "\n" + smoke.expectedEvidence);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
