using System;
using System.Collections.Generic;
using SecondDimension.Presentation.Release024;

namespace SecondDimension.Presentation.Release025
{
    [Serializable]
    public sealed class FinalImplementationHealthSnapshot025
    {
        public bool IsReady;
        public string Error;
        public string UnityVersion;
        public int SaveFormatVersion;
        public int OwnerReviewCases;
        public int WorldGateBoards;
        public int WorldGateNodes;
        public int CityBuildings;
        public int ArtBindings;
        public int AlliedUnionCapacity;
        public int EnemyUnionCapacity;
        public string[] Issues = Array.Empty<string>();
        public string[] SummaryLines = Array.Empty<string>();
    }

    /// <summary>
    /// Release 025 compatibility bridge for the Release 026 execution gate.
    /// All values are delegated to the authoritative Release 025 readiness and
    /// Release 024 integration services; this class owns no duplicate state.
    /// </summary>
    public static class FinalImplementationHealthService025
    {
        public static FinalImplementationHealthSnapshot025 BuildSnapshot()
        {
            var issues = new List<string>();
            try
            {
                var readiness = ImplementationReadinessService025.BuildSnapshot();
                var integration = FullGameIntegrationHealthService024.BuildSnapshot();

                if (!readiness.IsReady)
                    issues.Add("Implementation Readiness 025 failed: " + readiness.Error);
                if (!integration.IsReady)
                    issues.Add("Full Game Integration 024 failed: " + integration.Error);

                return new FinalImplementationHealthSnapshot025
                {
                    IsReady = issues.Count == 0,
                    Error = issues.Count == 0 ? string.Empty : string.Join("\n", issues),
                    UnityVersion = readiness.UnityVersion,
                    SaveFormatVersion = readiness.SaveFormatVersion,
                    OwnerReviewCases = readiness.OwnerReviewCases,
                    WorldGateBoards = readiness.WorldGateBoards,
                    WorldGateNodes = readiness.WorldGateNodes,
                    CityBuildings = integration.CityBuildings,
                    ArtBindings = integration.ArtBindings,
                    AlliedUnionCapacity = readiness.AlliedUnionCapacity,
                    EnemyUnionCapacity = readiness.EnemyUnionCapacity,
                    Issues = issues.ToArray(),
                    SummaryLines = new[]
                    {
                        "UNITY " + readiness.UnityVersion + " • SAVE v" + readiness.SaveFormatVersion,
                        readiness.OwnerReviewCases + " HARDENED OWNER-REVIEW CASES",
                        readiness.WorldGateBoards + " WORLD GATE BOARDS • " + readiness.WorldGateNodes + " NODES",
                        integration.CityBuildings + " BUILDINGS • " + integration.ArtBindings + " ART BINDINGS",
                        readiness.AlliedUnionCapacity + " ALLIED / " + readiness.EnemyUnionCapacity + " ENEMY UNION CAPACITY"
                    }
                };
            }
            catch (Exception exception)
            {
                return new FinalImplementationHealthSnapshot025
                {
                    IsReady = false,
                    Error = exception.ToString(),
                    Issues = new[] { exception.Message }
                };
            }
        }
    }

    /// <summary>
    /// Compatibility facade retained for historical Release 025 tooling.
    /// The authoritative implementation is ImplementationReadinessService025.
    /// </summary>
    public static class FinalImplementationHealth025
    {
        public static ImplementationReadinessSnapshot025 BuildSnapshot()
        {
            return ImplementationReadinessService025.BuildSnapshot();
        }
    }
}
