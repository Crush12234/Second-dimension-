using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    // Opt-in isolated performance evidence. No timing assertion, HUD, or save-policy change.
    public sealed class ResetPerformance110Tests
    {
        [Serializable]
        public sealed class Metric110
        {
            public string name, error;
            public int calls, collectionCount;
            public double totalMs, minMs, maxMs;
            public List<double> samplesMs = new List<double>();
        }

        [Serializable]
        public sealed class Report110
        {
            public string status = "RUNNING", source, sourceHash, isolated, startedUtc, endedUtc, failure;
            public string scope = "EditMode isolated coordinator/read/save costs. No rendered UI, click latency, animation, or whole-floor certification.";
            public int samples, recruits, inventoryItems, unions, battleEvents, ledgerEntries;
            public int stateReads, battleReads, guildReads, progressionReads, directLoadoutReads;
            public int towerDescriptions, heroRewardDescriptions, snapshotCreates, isolatedWrites, commands, changedNotifications;
            public long sourceBytes, isolatedBytes;
            public bool sourceUnchanged, readOnlyCampaignUnchanged, commandSucceeded;
            public string command = "none", commandMessage;
            public List<Metric110> metrics = new List<Metric110>();
        }

        [Test, Timeout(300000)]
        public void ProfilePreservedResetSave110()
        {
            var source = Environment.GetEnvironmentVariable("SD_RESET110_PERF_SOURCE");
            var evidence = Environment.GetEnvironmentVariable("SD_RESET110_PERF_EVIDENCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source) || string.IsNullOrWhiteSpace(evidence))
                Assert.Ignore("Set SD_RESET110_PERF_SOURCE and a fresh SD_RESET110_PERF_EVIDENCE directory.");
            source = Path.GetFullPath(source);
            evidence = Path.GetFullPath(evidence).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Assert.That(evidence, Is.Not.EqualTo(Path.GetPathRoot(evidence).TrimEnd(Path.DirectorySeparatorChar)), "Evidence cannot be a drive root.");
            Assert.That(evidence, Is.Not.EqualTo(Path.GetDirectoryName(source)).IgnoreCase, "Evidence must be separate from the source profile.");
            Assert.That(!Directory.Exists(evidence) || !Directory.EnumerateFileSystemEntries(evidence).Any(), Is.True, "Use a fresh evidence directory; older reports are preserved.");
            Directory.CreateDirectory(evidence);
            var isolated = Path.Combine(evidence, "profile_isolated110.json");
            File.Copy(source, isolated, false);
            var command = Environment.GetEnvironmentVariable("SD_RESET110_PERF_COMMAND") ?? "none";
            Assert.That(command == "none" || command == "claim-resolved", Is.True, "Only none or claim-resolved is supported.");
            var report = new Report110 {
                source = source, sourceHash = Hash110(source), sourceBytes = new FileInfo(source).Length,
                isolated = isolated, startedUtc = DateTime.UtcNow.ToString("O"), command = command,
                samples = int.TryParse(Environment.GetEnvironmentVariable("SD_RESET110_PERF_SAMPLES"), out var requested)
                    ? Math.Max(1, Math.Min(3, requested)) : 1
            };
            var reportPath = Path.Combine(evidence, "reset_performance110.json");
            M1RuntimeCoordinator coordinator = null;
            Action changed = () => report.changedNotifications++;
            try
            {
                Measure110(report, "coordinator.construct", 1, () => coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), isolated));
                coordinator.Changed += changed;
                var campaign = Campaign110(coordinator);
                report.recruits = campaign.Guild.Recruits.Count;
                report.inventoryItems = campaign.Guild.Inventory.Count;
                report.unions = campaign.Guild.Unions.Count;
                report.battleEvents = campaign.Battle?.EventLog.Count ?? 0;
                report.ledgerEntries = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022.AbyssAuthorityEntries.Count;
                var initialHash = CanonicalJson.Sha256Hex(campaign);
                var loadouts = typeof(M1RuntimeCoordinator).GetMethod("BuildLoadouts", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(loadouts, Is.Not.Null, "Keep the reflection hook aligned with production BuildLoadouts.");
                var registryMethod = typeof(M1RuntimeCoordinator).GetMethod("Registry022", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.That(registryMethod, Is.Not.Null);
                ICampaignRegistry022 registry = null;
                Measure110(report, "registry022.first_access", 1, () => registry = (ICampaignRegistry022)registryMethod.Invoke(coordinator, null));
                var recruitment = (GuildCityRecruitmentService017D)typeof(M1RuntimeCoordinator)
                    .GetField("_guildCityRecruitment", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coordinator);
                Assert.That(recruitment, Is.Not.Null);

                Measure110(report, "loadouts.direct", report.samples, () => { report.directLoadoutReads++; return loadouts.Invoke(coordinator, null); });
                Measure110(report, "state.full", report.samples, () => { report.stateReads++; return coordinator.State; });
                Measure110(report, "battle.narrow", report.samples, () => { report.battleReads++; return M2BattleViewAccess098.Read(coordinator); });
                Measure110(report, "guild_city.projection", report.samples, () => { report.guildReads++; return coordinator.GuildCity017D; });
                Measure110(report, "tower.describe_floors", report.samples, () => {
                    report.towerDescriptions++;
                    var result = CampaignProgressionCommandService022.DescribeTowerFloors094(campaign, registry);
                    Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value;
                });
                Measure110(report, "tower.describe_latest_hero", report.samples, () => {
                    report.heroRewardDescriptions++;
                    var result = recruitment.DescribeLatestTowerHeroReward094(campaign, registry);
                    Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value;
                });
                Measure110(report, "progression.full", report.samples, () => { report.progressionReads++; return coordinator.CampaignProgression022; });
                SaveEnvelopeV1 snapshot = null;
                Measure110(report, "save.envelope_create", report.samples, () => { report.snapshotCreates++; return snapshot = SaveEnvelopeV1.Create(campaign, DateTime.UtcNow); });
                var probePath = Path.Combine(evidence, "write_probe110.json");
                Measure110(report, "save.atomic_write_same_snapshot", 1, () => {
                    report.isolatedWrites++; new AtomicSaveStore().Write(probePath, snapshot); return null;
                });
                report.readOnlyCampaignUnchanged = StringComparer.Ordinal.Equals(initialHash, CanonicalJson.Sha256Hex(Campaign110(coordinator)));
                Assert.That(report.readOnlyCampaignUnchanged, Is.True, "Read-only projections altered campaign authority.");
                if (command == "claim-resolved")
                {
                    var battle = Campaign110(coordinator).Battle;
                    Assert.That(battle != null && battle.Reward != null && !battle.Reward.Claimed &&
                        battle.Outcome != SecondDimension.Gameplay.M2.BattleOutcome.InProgress, Is.True,
                        "The opt-in claim requires a resolved unclaimed battle in the isolated copy.");
                    Measure110(report, "command.claim_battle_rewards_no_ui_observer", 1, () => {
                        report.commands++; var result = coordinator.ClaimBattleRewards();
                        report.commandSucceeded = result.Succeeded; report.commandMessage = result.Message; return result;
                    });
                    Assert.That(report.commandSucceeded, Is.True, report.commandMessage);
                }
                report.status = "PASS";
            }
            catch (Exception exception)
            {
                report.status = "FAILED";
                report.failure = (exception is TargetInvocationException target && target.InnerException != null
                    ? target.InnerException : exception).ToString();
                throw;
            }
            finally
            {
                if (coordinator != null) coordinator.Changed -= changed;
                report.sourceUnchanged = StringComparer.Ordinal.Equals(report.sourceHash, Hash110(source));
                report.isolatedBytes = File.Exists(isolated) ? new FileInfo(isolated).Length : 0;
                report.endedUtc = DateTime.UtcNow.ToString("O");
                if (!report.sourceUnchanged) { report.status = "FAILED"; report.failure += " Source file changed."; }
                File.WriteAllText(reportPath, JsonConvert.SerializeObject(report, Formatting.Indented));
                TestContext.Progress.WriteLine("RESET_PERFORMANCE110 " + report.status + " " + reportPath);
                Assert.That(report.sourceUnchanged, Is.True, "The protected source profile changed.");
            }
        }

        static CampaignState Campaign110(M1RuntimeCoordinator coordinator) =>
            (CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(coordinator);

        static void Measure110(Report110 report, string name, int count, Func<object> action)
        {
            var metric = new Metric110 { name = name, minMs = double.MaxValue };
            report.metrics.Add(metric);
            for (var index = 0; index < count; index++)
            {
                var started = Stopwatch.GetTimestamp();
                object result = null;
                try { result = action(); }
                catch (Exception exception) { metric.error = exception.Message; throw; }
                finally
                {
                    var elapsed = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
                    metric.calls++; metric.samplesMs.Add(elapsed); metric.totalMs += elapsed;
                    metric.minMs = Math.Min(metric.minMs, elapsed); metric.maxMs = Math.Max(metric.maxMs, elapsed);
                }
                if (result is ICollection values) metric.collectionCount = values.Count;
                GC.KeepAlive(result);
            }
        }

        static string Hash110(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
    }
}
