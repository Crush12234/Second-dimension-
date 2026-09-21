using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    // Opt-in, isolated earned-save diagnostic. No selection, resolution, reward,
    // HP, roster, equipment, progression or production save mutation is invoked.
    public static class M1ProjectionCost098Batch
    {
        [Serializable] public sealed class Metric098
        {
            public string name, status = "MEASURED";
            public int samples;
            public double totalMilliseconds, meanMilliseconds, maximumMilliseconds;
            public long allocatedBytes = -1;
        }
        [Serializable] public sealed class Report098
        {
            public string status = "RUNNING", failure, source, sourceSha256, isolatedSave, canonicalHash;
            public bool sourceUnchanged, isolatedUnchanged, allocationCounterResponsive;
            public int recruits, inventoryItems, activePlayerUnions, enemyMembers;
            public bool battleResolved, autoRankingEligible;
            public int committedForecasts, projectedForecasts, projectedCanActUnions, autoChosenOrders;
            public long saveBytes;
            public string scope = "Pure fresh presentation projection/ranking timing in Editor; no gameplay commands, save writes after copy/load, native rendering or frame-time claim.";
            public List<Metric098> metrics = new List<Metric098>();
        }
        static readonly MethodInfo Allocation098 = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread", Type.EmptyTypes);
        static bool AllocationResponsive098;
        static readonly FieldInfo Campaign098 = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);

        public static void Run()
        {
            var report = new Report098();
            string evidence = null;
            var ownsEvidence = false;
            try
            {
                var args = Environment.GetCommandLineArgs();
                report.source = Path.GetFullPath(Value098(args, "--sd-projection-source="));
                evidence = Path.GetFullPath(Value098(args, "--sd-projection-evidence="));
                var sampleText = Value098(args, "--sd-projection-samples=");
                var samples = string.IsNullOrEmpty(sampleText) ? 3 : int.Parse(sampleText);
                if (samples < 1 || samples > 5) throw new ArgumentOutOfRangeException(nameof(samples));
                report.isolatedSave = DeepCampaignVerification093.IsolatedSavePath093(report.source, evidence);
                if (!File.Exists(report.source)) throw new FileNotFoundException("Earned source required", report.source);
                if (Directory.Exists(evidence) && Directory.EnumerateFileSystemEntries(evidence).Any())
                    throw new IOException("Evidence directory must be fresh.");
                report.sourceSha256 = Hash098(report.source);
                Directory.CreateDirectory(evidence);
                ownsEvidence = true;
                File.Copy(report.source, report.isolatedSave, false);
                var isolatedHash = Hash098(report.isolatedSave);
                var coordinator = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath,
                    "Authority", "CONTENT"), report.isolatedSave);
                if (Hash098(report.isolatedSave) != isolatedHash)
                    throw new InvalidOperationException("Constructor changed the copy; stop and inspect migration before benchmarking.");
                var campaign = (CampaignState)Campaign098.GetValue(coordinator);
                if (campaign?.Battle == null) throw new InvalidOperationException("Use an earned save containing a real battle.");
                report.canonicalHash = CanonicalJson.Sha256Hex(campaign);
                report.recruits = campaign.Guild.Recruits.Count;
                report.inventoryItems = campaign.Guild.Inventory.Count;
                report.activePlayerUnions = campaign.Battle.PlayerUnions.Count(value => !value.IsDefeated && !value.Retreated);
                report.enemyMembers = campaign.Battle.EnemyUnions.Sum(value => value.Members.Count);
                report.saveBytes = new FileInfo(report.isolatedSave).Length;
                AllocationResponsive098 = report.allocationCounterResponsive = ProbeAllocationCounter098();
                Measure098(report, "State first fresh projection", 1, () => coordinator.State);
                Measure098(report, "State repeated fresh projection", samples, () => coordinator.State);
                foreach (var methodName in new[] { "BuildApplicants", "BuildLoadouts", "BuildUnions", "BuildBattleView" })
                {
                    var method = typeof(M1RuntimeCoordinator).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
                    if (method == null) throw new MissingMethodException(methodName);
                    Measure098(report, methodName, samples, () => method.Invoke(coordinator, null));
                }
                Measure098(report, "Full campaign canonical SHA256", samples, () => CanonicalJson.Sha256Hex(campaign));
                Measure098(report, "Fresh public battle-only reader098", samples, () => M2BattleViewAccess098.Read(coordinator));
                Measure098(report, "Save envelope hash plus JSON only (NO disk write)", samples,
                    () => JsonConvert.SerializeObject(SaveEnvelopeV1.Create(campaign, DateTime.UtcNow), CanonicalJson.DefaultSettings()));
                var battle = coordinator.State.Battle;
                report.battleResolved = battle.IsResolved;
                report.committedForecasts = campaign.Battle.CommittedForecasts.Count;
                report.projectedForecasts = battle.Forecasts?.Count ?? 0;
                report.projectedCanActUnions = battle.PlayerUnions.Count(value => value.CanAct);
                report.autoRankingEligible = M2BattleAutoOrders091.HasLivingOpposition(battle) && report.projectedCanActUnions > 0;
                report.autoChosenOrders = battle.PlayerUnions.Where(value => value.CanAct)
                    .Count(value => M2BattleAutoOrders091.Choose(battle, value.UnionId) != null);
                if (report.autoRankingEligible)
                    Measure098(report, "Auto Choose all active Unions from ONE existing DTO", samples,
                        () => battle.PlayerUnions.Where(value => value.CanAct)
                            .Select(value => M2BattleAutoOrders091.Choose(battle, value.UnionId)?.ForecastId).ToArray());
                else report.metrics.Add(new Metric098 { name = "Auto Choose all active Unions from ONE existing DTO",
                    status = "SKIPPED_TERMINAL_OR_NO_OPPOSITION", samples = 0 });
                report.sourceUnchanged = Hash098(report.source) == report.sourceSha256;
                report.isolatedUnchanged = Hash098(report.isolatedSave) == isolatedHash &&
                    CanonicalJson.Sha256Hex((CampaignState)Campaign098.GetValue(coordinator)) == report.canonicalHash;
                report.status = report.sourceUnchanged && report.isolatedUnchanged ? "PASS_READ_ONLY_PROFILE" : "FAIL_MUTATION";
            }
            catch (Exception exception) { report.status = "BLOCKED"; report.failure = exception.ToString(); }
            if (ownsEvidence && evidence != null && Directory.Exists(evidence))
                File.WriteAllText(Path.Combine(evidence, "projection_cost098_report.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
            UnityEngine.Debug.Log("PROJECTION098 " + JsonConvert.SerializeObject(report));
            EditorApplication.Exit(report.status == "PASS_READ_ONLY_PROFILE" ? 0 : 98);
        }

        static void Measure098(Report098 report, string name, int samples, Func<object> projection)
        {
            var metric = new Metric098 { name = name, samples = samples, allocatedBytes = AllocationResponsive098 ? 0 : -1 };
            for (var index = 0; index < samples; index++)
            {
                var before = AllocationResponsive098 ? (long)Allocation098.Invoke(null, null) : 0L;
                var clock = Stopwatch.StartNew();
                var result = projection();
                clock.Stop();
                metric.totalMilliseconds += clock.Elapsed.TotalMilliseconds;
                metric.maximumMilliseconds = Math.Max(metric.maximumMilliseconds, clock.Elapsed.TotalMilliseconds);
                if (AllocationResponsive098) metric.allocatedBytes += (long)Allocation098.Invoke(null, null) - before;
                GC.KeepAlive(result);
            }
            metric.meanMilliseconds = metric.totalMilliseconds / samples;
            report.metrics.Add(metric);
        }
        static string Value098(string[] args, string prefix) => args.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
        static bool ProbeAllocationCounter098()
        {
            if (Allocation098 == null) return false;
            var before = (long)Allocation098.Invoke(null, null);
            var probe = new byte[4096];
            var after = (long)Allocation098.Invoke(null, null);
            GC.KeepAlive(probe);
            return after > before;
        }
        static string Hash098(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    }
}
