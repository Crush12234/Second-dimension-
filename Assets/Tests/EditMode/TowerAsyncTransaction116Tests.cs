#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerAsyncTransaction116Tests
    {
        const string SourceHash = "511D162B27A4FCD6371BE562EF37BD0FEB4B27EC29C81F83B03A09C5381DDC1E";
        const string SourcePath = @"C:\SecondDimension\BuildEvidence\Reset110\R288_TowerSoak301_16x_1800s\EarnedReviewSave097.json";
        const string Prefix = "SecondDimensionTowerAsync116_";
        string _source, _directory;
        readonly List<M1RuntimeCoordinator> _owners = new List<M1RuntimeCoordinator>();

        [SetUp]
        public void SetUp116()
        {
            _source = Environment.GetEnvironmentVariable("SD_TOWER_INTERRUPTED115_SOURCE");
            if (!string.IsNullOrWhiteSpace(_source) && !File.Exists(_source))
                Assert.Fail("Configured interrupted source does not exist: " + _source);
            if (string.IsNullOrWhiteSpace(_source)) _source = SourcePath;
            if (!File.Exists(_source)) Assert.Ignore("Use the preserved interrupted R288 save via SD_TOWER_INTERRUPTED115_SOURCE.");
            Assert.That(Hash(_source), Is.EqualTo(SourceHash));
            _directory = Path.Combine(Path.GetTempPath(), Prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [UnityTearDown]
        public IEnumerator TearDown116()
        {
            foreach (var owner in _owners) owner.CancelTowerAutoBeforeSave116();
            foreach (var owner in _owners)
            {
                var pending = owner.PendingTowerAutoTransition116;
                if (pending != null) yield return Wait(pending);
            }
            _owners.Clear();
            if (File.Exists(_source)) Assert.That(Hash(_source), Is.EqualTo(SourceHash));
            if (!string.IsNullOrWhiteSpace(_directory) && Directory.Exists(_directory))
            {
                var full = Path.GetFullPath(_directory);
                var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.That(full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith(Prefix), Is.True);
                Directory.Delete(full, true);
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator Async315MatchesSynchronousAuthorityAndPublishesOnceOnMainThread116()
        {
            var sync = Create("sync.json", out var syncPath);
            var expected = sync.AdvanceTowerAutoAfterVictory108();
            Assert.That(expected.Succeeded, Is.True, expected.Message);
            var expectedHash = CanonicalJson.Sha256Hex(Campaign(sync));
            var owner = Create("async.json", out var path);
            var main = Thread.CurrentThread.ManagedThreadId;
            var changes = 0;
            var publishThread = 0;
            owner.Changed += () => { changes++; publishThread = Thread.CurrentThread.ManagedThreadId; };
            var start = Stopwatch.StartNew();
            var task = owner.AdvanceTowerAutoAfterVictoryAsync116();
            var dispatchMs = start.Elapsed.TotalMilliseconds;
            Assert.That(dispatchMs, Is.LessThan(250d), "Dispatch must not execute the multi-second authority transaction on the game thread.");
            var frames = 0;
            var maximumGap = 0d;
            var previous = start.Elapsed.TotalMilliseconds;
            while (!task.IsCompleted)
            {
                yield return null;
                frames++;
                var now = start.Elapsed.TotalMilliseconds;
                maximumGap = Math.Max(maximumGap, now - previous);
                previous = now;
                Assert.That(start.Elapsed.TotalSeconds, Is.LessThan(300d));
            }
            Assert.That(task.Result.Succeeded, Is.True, task.Result.Message);
            Assert.That(frames, Is.GreaterThan(1), "The main loop must keep advancing while the actual R288 transaction runs.");
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(publishThread, Is.EqualTo(main));
            Assert.That(CanonicalJson.Sha256Hex(Campaign(owner)), Is.EqualTo(expectedHash), "Worker and synchronous authorities must produce identical gameplay state.");
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(expectedHash));
            Assert.That(CanonicalJson.Sha256Hex(Campaign(new M1RuntimeCoordinator(ContentRoot, path))), Is.EqualTo(expectedHash));
            Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(315));
            Assert.That(owner.CampaignProgression022.TowerFloorNumber, Is.EqualTo(316));
            TestContext.Progress.WriteLine("TOWER_ASYNC116 dispatchMs=" + dispatchMs.ToString("F2") + " frames=" + frames +
                " maxEditorFrameGapMs=" + maximumGap.ToString("F2") + " elapsedMs=" + start.Elapsed.TotalMilliseconds.ToString("F2"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CanceledPreparationBlocksEverySaveAndSamePathReloadThenAllowsRetry116()
        {
            var owner = Create("canceled.json", out var path);
            var source = Campaign(owner);
            var bytes = File.ReadAllBytes(path);
            var changes = 0;
            owner.Changed += () => changes++;
            var pending = owner.AdvanceTowerAutoAfterVictoryAsync116();
            Assert.That(owner.PendingTowerAutoTransition116, Is.SameAs(pending));
            foreach (var blocked in new[] { owner.AdvanceTowerAutoAfterVictory108(), owner.ClaimBattleRewards(),
                owner.BankTowerVictory110(), owner.StartTowerBattle110(), owner.SaveAndReloadProof(), owner.CreateGuild(null),
                owner.EquipItem("not-an-owned-id", "not-a-slot", "not-an-item") })
            {
                Assert.That(blocked.Succeeded, Is.False);
                Assert.That(blocked.Message, Does.Contain("Tower rewards are being saved"));
            }
            Assert.That(owner.AdvanceTowerAutoAfterVictoryAsync116().Result.Succeeded, Is.False);
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            owner.CancelTowerAutoBeforeSave116();
            yield return Wait(pending);
            Assert.That(pending.Result.Succeeded, Is.False);
            Assert.That(pending.Result.Message, Does.Contain("canceled before saving"));
            Assert.That(Campaign(owner), Is.SameAs(source));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.That(File.Exists(path + ".bak"), Is.False);
            Assert.That(changes, Is.Zero);
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Assert.That(Campaign(new M1RuntimeCoordinator(ContentRoot, path)).Battle.Reward.Claimed, Is.False);
            var retried = owner.AdvanceTowerAutoAfterVictoryAsync116();
            yield return Wait(retried);
            Assert.That(retried.Result.Succeeded, Is.True, retried.Result.Message);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(Campaign(owner).Guild.Development.ClaimedBattleRewardIds.Count, Is.EqualTo(643));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator WriteFailureRetainsVictoryAndPostSaveObserverFailureCannotRollBackRetry116()
        {
            var owner = Create("failure.json", out var path);
            var source = Campaign(owner);
            var bytes = File.ReadAllBytes(path);
            Directory.CreateDirectory(path + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex("SAVE_WRITE_FAILED109"));
            var failed = owner.AdvanceTowerAutoAfterVictoryAsync116();
            yield return Wait(failed);
            Assert.That(failed.Result.Succeeded, Is.False);
            Assert.That(Campaign(owner), Is.SameAs(source));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Directory.Delete(path + ".tmp");
            var observed = 0;
            owner.Changed += () => { observed++; throw new InvalidOperationException("observer probe116"); };
            LogAssert.Expect(LogType.Error, new Regex("POST_SAVE_PRESENTATION_REFRESH_FAILED109"));
            var retried = owner.AdvanceTowerAutoAfterVictoryAsync116();
            yield return Wait(retried);
            Assert.That(retried.Result.Succeeded, Is.True, retried.Result.Message);
            Assert.That(observed, Is.EqualTo(1));
            var committedHash = CanonicalJson.Sha256Hex(Campaign(owner));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(committedHash));
            Assert.That(CanonicalJson.Sha256Hex(Campaign(new M1RuntimeCoordinator(ContentRoot, path))), Is.EqualTo(committedHash));
            var primary = Hash(path);
            var backup = Hash(path + ".bak");
            Assert.That(owner.AdvanceTowerAutoAfterVictoryAsync116().Result.Succeeded, Is.False);
            Assert.That(owner.ClaimBattleRewards().Succeeded, Is.False);
            Assert.That(Hash(path), Is.EqualTo(primary));
            Assert.That(Hash(path + ".bak"), Is.EqualTo(backup));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CancellationAfterStoreEntryStillPublishesDurable316AndReleasesLease116()
        {
            var owner = Create("store_entered.json", out var path);
            var pending = owner.AdvanceTowerAutoAfterVictoryAsync116();
            var wait = Stopwatch.StartNew();
            while (!owner.TowerAutoIsSaving116 && !pending.IsCompleted)
            {
                Assert.That(wait.Elapsed.TotalSeconds, Is.LessThan(300d));
                yield return null;
            }
            Assert.That(owner.TowerAutoIsSaving116, Is.True, "The preserved large save must exercise the real in-flight store boundary.");
            owner.CancelTowerAutoBeforeSave116();
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            yield return Wait(pending);
            Assert.That(pending.Result.Succeeded, Is.True, pending.Result.Message);
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            var reloaded = new M1RuntimeCoordinator(ContentRoot, path);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(CanonicalJson.Sha256Hex(Campaign(owner))));
            Assert.That(reloaded.CampaignProgression022.TowerFloorNumber, Is.EqualTo(316));
            Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(315));
        }

        M1RuntimeCoordinator Create(string name, out string path)
        {
            path = Path.Combine(_directory, name);
            File.Copy(_source, path);
            var owner = new M1RuntimeCoordinator(ContentRoot, path);
            _owners.Add(owner);
            var view = owner.CampaignProgression022; // Preload/validate the registry on the main thread.
            Assert.That(view.TowerAuthorityError094, Is.Empty);
            Assert.That(view.TowerFloorNumber, Is.EqualTo(315));
            return owner;
        }
        static IEnumerator Wait(Task<M1CommandResult> task)
        {
            var clock = Stopwatch.StartNew();
            while (!task.IsCompleted) { Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
        }
        static SaveEnvelopeV1 Read(string path)
        {
            var saved = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
            return saved.Value;
        }
        static string Hash(string path)
        {
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static CampaignState Campaign(M1RuntimeCoordinator owner) => (CampaignState)typeof(M1RuntimeCoordinator)
            .GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
#endif
