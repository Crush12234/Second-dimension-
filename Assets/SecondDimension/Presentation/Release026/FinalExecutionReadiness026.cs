using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign021;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017H;
using SecondDimension.Presentation.Release025;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation.Release026
{
    [Serializable]
    public sealed class FinalExecutionManifest026
    {
        public string contentVersion;
        public int saveFormatVersion;
        public string requiredUnityVersion;
        public int expectedCoordinatorInterfaceCount;
        public int expectedWorldGateBoards;
        public int expectedWorldGateNodes;
        public int expectedCityBuildings;
        public int expectedArtBindings;
        public int alliedUnionCapacity;
        public int enemyUnionCapacity;
        public int expectedCompileRiskGates;
    }

    [Serializable]
    public sealed class FinalExecutionHealthSnapshot026
    {
        public bool IsReady;
        public string Error;
        public int CoordinatorInterfacesExpected;
        public int CoordinatorInterfacesImplemented;
        public int CoordinatorCommandMethods;
        public int SaveFormatVersion;
        public int WorldGateBoards;
        public int WorldGateNodes;
        public int CityBuildings;
        public int ArtBindings;
        public int AlliedUnionCapacity;
        public int EnemyUnionCapacity;
        public string[] SummaryLines = Array.Empty<string>();
        public string[] Issues = Array.Empty<string>();
    }

    public static class FinalExecutionHealthService026
    {
        private const string ManifestPath = "SecondDimension/Release026/FINAL_EXECUTION_MANIFEST_026";

        public static FinalExecutionManifest026 LoadManifest()
        {
            var asset = Resources.Load<TextAsset>(ManifestPath);
            if (asset == null) throw new InvalidOperationException("Missing Final Execution 026 manifest resource.");
            var value = JsonUtility.FromJson<FinalExecutionManifest026>(asset.text);
            if (value == null || string.IsNullOrWhiteSpace(value.contentVersion))
                throw new InvalidOperationException("Invalid Final Execution 026 manifest resource.");
            return value;
        }

        public static Type[] ExpectedCoordinatorInterfaces() => new[]
        {
            typeof(IM1PresentationCoordinator),
            typeof(IM2PresentationCoordinator),
            typeof(IGuildCityPresentationCoordinator017D),
            typeof(IGuildCityStrategicPresentationCoordinator017H),
            typeof(ICampaignPresentationCoordinator019),
            typeof(ICampaignPlayablePresentationCoordinator020),
            typeof(ICampaignPresentationCoordinator021),
            typeof(ICampaignProgressionPresentationCoordinator022),
            typeof(ICampaignWorldGatePresentationCoordinator023)
        };

        public static FinalExecutionHealthSnapshot026 BuildSnapshot()
        {
            var issues = new List<string>();
            try
            {
                var manifest = LoadManifest();
                var inherited = FinalImplementationHealthService025.BuildSnapshot();
                if (!inherited.IsReady) issues.Add("Inherited 025 health failed: " + inherited.Error);

                var coordinator = typeof(M1RuntimeCoordinator);
                var expected = ExpectedCoordinatorInterfaces();
                var implemented = expected.Count(value => value.IsAssignableFrom(coordinator));
                foreach (var contract in expected)
                    if (!contract.IsAssignableFrom(coordinator))
                        issues.Add("M1RuntimeCoordinator does not implement " + contract.FullName + ".");

                var commands = expected.SelectMany(value => value.GetMethods())
                    .Where(value => !value.IsSpecialName)
                    .Select(value => value.Name)
                    .Distinct(StringComparer.Ordinal)
                    .Count();

                if (expected.Length != manifest.expectedCoordinatorInterfaceCount)
                    issues.Add("Coordinator-interface manifest count is stale.");
                if (SaveEnvelopeV1.CurrentFormatVersion != manifest.saveFormatVersion)
                    issues.Add("Save format does not match Final Execution 026 manifest.");
                if (inherited.WorldGateBoards != manifest.expectedWorldGateBoards ||
                    inherited.WorldGateNodes != manifest.expectedWorldGateNodes)
                    issues.Add("World Gate content does not match Final Execution 026 manifest.");
                if (inherited.CityBuildings != manifest.expectedCityBuildings ||
                    inherited.ArtBindings != manifest.expectedArtBindings)
                    issues.Add("City or Art authority does not match Final Execution 026 manifest.");
                if (inherited.AlliedUnionCapacity != manifest.alliedUnionCapacity ||
                    inherited.EnemyUnionCapacity != manifest.enemyUnionCapacity)
                    issues.Add("Union capacity does not match Final Execution 026 manifest.");

                return new FinalExecutionHealthSnapshot026
                {
                    IsReady = issues.Count == 0,
                    Error = issues.Count == 0 ? string.Empty : string.Join("\n", issues),
                    CoordinatorInterfacesExpected = expected.Length,
                    CoordinatorInterfacesImplemented = implemented,
                    CoordinatorCommandMethods = commands,
                    SaveFormatVersion = inherited.SaveFormatVersion,
                    WorldGateBoards = inherited.WorldGateBoards,
                    WorldGateNodes = inherited.WorldGateNodes,
                    CityBuildings = inherited.CityBuildings,
                    ArtBindings = inherited.ArtBindings,
                    AlliedUnionCapacity = inherited.AlliedUnionCapacity,
                    EnemyUnionCapacity = inherited.EnemyUnionCapacity,
                    Issues = issues.ToArray(),
                    SummaryLines = new[]
                    {
                        implemented + "/" + expected.Length + " PLAYER-FACING COORDINATOR CONTRACTS REACHABLE",
                        commands + " DISTINCT COORDINATOR COMMANDS",
                        "SAVE v" + inherited.SaveFormatVersion + " • " + inherited.WorldGateBoards + " WORLD GATE BOARDS • " + inherited.WorldGateNodes + " NODES",
                        inherited.CityBuildings + " BUILDINGS • " + inherited.ArtBindings + " ART BINDINGS",
                        inherited.AlliedUnionCapacity + " ALLIED / " + inherited.EnemyUnionCapacity + " ENEMY UNION CAPACITY"
                    }
                };
            }
            catch (Exception exception)
            {
                return new FinalExecutionHealthSnapshot026
                {
                    IsReady = false,
                    Error = exception.ToString(),
                    Issues = new[] { exception.Message }
                };
            }
        }
    }

    public static class FinalExecutionRuntimeDiagnostics026
    {
        private static bool _validated;
        public static FinalExecutionHealthSnapshot026 LastSnapshot { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ValidateAfterBoot()
        {
            if (_validated) return;
            _validated = true;
            LastSnapshot = FinalExecutionHealthService026.BuildSnapshot();
            if (LastSnapshot.IsReady)
                Debug.Log("FINAL EXECUTION READINESS 026 PASS\n" + string.Join("\n", LastSnapshot.SummaryLines));
            else
                Debug.LogError("FINAL EXECUTION READINESS 026 FAIL\n" + LastSnapshot.Error);
        }
    }

    public sealed class FinalExecutionDashboard026 : MonoBehaviour
    {
        private Vector2 _scroll;
        private FinalExecutionHealthSnapshot026 _snapshot;
        public bool IsReady { get; private set; }
        public string Error { get; private set; }

        private void Awake()
        {
            _snapshot = FinalExecutionHealthService026.BuildSnapshot();
            IsReady = _snapshot.IsReady;
            Error = _snapshot.Error ?? string.Empty;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(18, 18, Screen.width - 36, Screen.height - 36),
                "SECOND DIMENSION — FINAL EXECUTION READINESS 026");
            GUILayout.BeginArea(new Rect(42, 62, Screen.width - 84, Screen.height - 102));
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(IsReady ? "EXECUTION HEALTH READY" : "EXECUTION HEALTH BLOCKED");
            if (!string.IsNullOrWhiteSpace(Error)) GUILayout.TextArea(Error);
            if (_snapshot != null && _snapshot.SummaryLines != null)
                foreach (var line in _snapshot.SummaryLines) GUILayout.Label(line);
            GUILayout.Space(16);
            GUILayout.Label("This dashboard is read-only and cannot mutate authoritative gameplay state.");
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }
    }
}
