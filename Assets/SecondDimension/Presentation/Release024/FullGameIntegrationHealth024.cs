using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Release023;
using UnityEngine;

namespace SecondDimension.Presentation.Release024
{
    [Serializable]
    public sealed class FullGameIntegrationHealthSnapshot024
    {
        public bool IsReady;
        public string Error;
        public int SaveFormatVersion;
        public int SmokeCases;
        public int CityBuildings;
        public int DefenseProfiles;
        public int CanonEvents;
        public int ActiveCampaignChapters;
        public int WorldGateBoards;
        public int WorldGateNodes;
        public int TravelRoutes;
        public int StandingSystems;
        public int RecruitUnlockSets;
        public int ArtBindings;
        public int AlliedUnionCapacity;
        public int EnemyUnionCapacity;
        public string[] Issues = Array.Empty<string>();
        public string[] SummaryLines = Array.Empty<string>();
    }

    public static class FullGameIntegrationHealthService024
    {
        public static FullGameIntegrationHealthSnapshot024 BuildSnapshot()
        {
            var issues = new List<string>();
            try
            {
                var integration = FullGameIntegrationRegistry024.LoadFromResources();
                var release = FinalReleaseRegistry023.LoadFromResources();
                var releaseSnapshot = FinalReleaseReadinessService023.BuildSnapshot();
                var worldGate = CampaignRegistry023.LoadFromResources();
                var nodeCount = worldGate.Boards.Values.Sum(value =>
                    value.nodes == null ? 0 : value.nodes.Length);

                if (!releaseSnapshot.IsAvailable)
                    issues.Add("Final Release 023 readiness failed: " + releaseSnapshot.Error);
                if (worldGate.Boards.Count != integration.Manifest.worldGateBoardCount)
                    issues.Add("World Gate board count does not match the 024 manifest.");
                if (nodeCount != integration.Manifest.worldGateNodeCount)
                    issues.Add("World Gate node count does not match the 024 manifest.");
                if (worldGate.Travel.Count != integration.Manifest.worldGateTravelRouteCount)
                    issues.Add("World Gate travel-route count is incomplete.");
                if (worldGate.Standing.Count != integration.Manifest.worldGateStandingCount)
                    issues.Add("World-standing coverage is incomplete.");
                if (worldGate.RecruitUnlocks.Count != integration.Manifest.worldGateRecruitUnlockCount)
                    issues.Add("World-origin recruitment coverage is incomplete.");
                if (release.Manifest.saveFormatVersion != 11)
                    issues.Add("Final Release 023 compatibility manifest is not synchronized to save v10.");

                var snapshot = new FullGameIntegrationHealthSnapshot024
                {
                    IsReady = issues.Count == 0,
                    Error = issues.Count == 0 ? string.Empty : string.Join("\n", issues),
                    SaveFormatVersion = SecondDimension.Save.SaveEnvelopeV1.CurrentFormatVersion,
                    SmokeCases = integration.SmokeCases.Count,
                    CityBuildings = releaseSnapshot.CityBuildings,
                    DefenseProfiles = releaseSnapshot.DefenseProfiles,
                    CanonEvents = releaseSnapshot.CanonEvents,
                    ActiveCampaignChapters = releaseSnapshot.ActiveCampaignChapters,
                    WorldGateBoards = worldGate.Boards.Count,
                    WorldGateNodes = nodeCount,
                    TravelRoutes = worldGate.Travel.Count,
                    StandingSystems = worldGate.Standing.Count,
                    RecruitUnlockSets = worldGate.RecruitUnlocks.Count,
                    ArtBindings = releaseSnapshot.ArtBindings,
                    AlliedUnionCapacity = releaseSnapshot.AlliedUnionCapacity,
                    EnemyUnionCapacity = releaseSnapshot.EnemyUnionCapacity,
                    Issues = issues.ToArray()
                };
                snapshot.SummaryLines = new[]
                {
                    "SAVE v" + snapshot.SaveFormatVersion + " • " + snapshot.SmokeCases + " OWNER-REVIEW CASES",
                    snapshot.CityBuildings + " BUILDINGS • " + snapshot.DefenseProfiles + " DEFENSE PROFILES • " + snapshot.CanonEvents + " CANON EVENTS",
                    snapshot.ActiveCampaignChapters + " CHAPTERS • " + snapshot.WorldGateBoards + " BOARDS • " + snapshot.WorldGateNodes + " NODES",
                    snapshot.TravelRoutes + " TRAVEL ROUTES • " + snapshot.StandingSystems + " STANDING SYSTEMS • " + snapshot.RecruitUnlockSets + " RECRUIT ORIGIN SETS",
                    snapshot.ArtBindings + " ART BINDINGS • " + snapshot.AlliedUnionCapacity + "v" + snapshot.EnemyUnionCapacity + " UNION CAPACITY"
                };
                return snapshot;
            }
            catch (Exception exception)
            {
                return new FullGameIntegrationHealthSnapshot024
                {
                    IsReady = false,
                    Error = exception.ToString(),
                    Issues = new[] { exception.Message },
                    SummaryLines = Array.Empty<string>()
                };
            }
        }
    }

    public sealed class FullGameIntegrationDashboard024 : MonoBehaviour
    {
        private Vector2 _scroll;
        private FullGameIntegrationRegistry024 _registry;
        private FullGameIntegrationHealthSnapshot024 _snapshot;

        public bool IsReady { get; private set; }
        public string Error { get; private set; }

        private void Awake()
        {
            _registry = FullGameIntegrationRegistry024.LoadFromResources();
            _snapshot = FullGameIntegrationHealthService024.BuildSnapshot();
            IsReady = _snapshot.IsReady;
            Error = _snapshot.Error ?? string.Empty;
        }

        private void OnGUI()
        {
            GUI.Box(
                new Rect(18, 18, Screen.width - 36, Screen.height - 36),
                "SECOND DIMENSION — FINAL GAME + WORLD GATE INTEGRATION 024");
            GUILayout.BeginArea(new Rect(42, 62, Screen.width - 84, Screen.height - 102));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(IsReady
                ? "INTEGRATED CONTENT READY — UNITY EXECUTION GATES REMAIN"
                : "INTEGRATION ERROR");
            if (!string.IsNullOrWhiteSpace(Error))
                GUILayout.TextArea(Error);
            if (_snapshot != null && _snapshot.SummaryLines != null)
                foreach (var line in _snapshot.SummaryLines)
                    GUILayout.Label(line);
            GUILayout.Space(12);
            if (_registry != null)
            {
                GUILayout.Label("OWNER-REVIEW MATRIX — " + _registry.SmokeCases.Count + " CASES");
                foreach (var smoke in _registry.SmokeCases.Values.OrderBy(value => value.caseId))
                    GUILayout.Label(
                        smoke.caseId + " • " + smoke.category + " • " + smoke.title +
                        "\n" + smoke.expectedEvidence);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
