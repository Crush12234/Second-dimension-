#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TowerClaimAsync120Tests
    {
        const string SourceHash = "511D162B27A4FCD6371BE562EF37BD0FEB4B27EC29C81F83B03A09C5381DDC1E";
        const string DefaultSource = @"C:\SecondDimension\BuildEvidence\Reset110\R288_TowerSoak301_16x_1800s\EarnedReviewSave097.json";
        const string Prefix = "SecondDimensionTowerClaim120_";
        string _source, _directory;
        readonly List<M1RuntimeCoordinator> _owners = new List<M1RuntimeCoordinator>();

        [SetUp]
        public void SetUp120()
        {
            _source = Environment.GetEnvironmentVariable("SD_TOWER_INTERRUPTED115_SOURCE");
            if (!string.IsNullOrWhiteSpace(_source) && !File.Exists(_source)) Assert.Fail("Configured interrupted source is missing: " + _source);
            if (string.IsNullOrWhiteSpace(_source)) _source = DefaultSource;
            if (!File.Exists(_source)) Assert.Ignore("Use the pinned R288 copy via SD_TOWER_INTERRUPTED115_SOURCE.");
            Assert.That(Hash(_source), Is.EqualTo(SourceHash));
            _directory = Path.Combine(Path.GetTempPath(), Prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [UnityTearDown]
        public IEnumerator TearDown120()
        {
            foreach (var owner in _owners) { owner.CancelTowerClaimBeforeSave120(); owner.CancelTowerManualBeforeSave117(); owner.CancelTowerAutoBeforeSave116(); }
            foreach (var owner in _owners)
            {
                var pending = owner.PendingTowerClaim120 ?? owner.PendingTowerManualTransition117 ?? owner.PendingTowerAutoTransition116;
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
        public IEnumerator RealClaimMatchesSynchronousSavedAuthorityAndLeavesBankDeliberate120()
        {
            var sync = Copy(_source, "sync.json", out var syncPath);
            var expected = sync.ClaimBattleRewards();
            Assert.That(expected.Succeeded, Is.True, expected.Message);
            var expectedHash = CanonicalJson.Sha256Hex(State(sync));
            var owner = Copy(_source, "async.json", out var path);
            var original = State(owner);
            var originalHash = CanonicalJson.Sha256Hex(original);
            var highest = owner.CampaignProgression022.HighestClearedTowerFloor;
            var changes = 0;
            owner.Changed += () => changes++;
            Assert.That(owner.TowerVictoryAwaitingClaim120, Is.True);
            var timer = Stopwatch.StartNew();
            var claim = owner.ClaimTowerBattleVictoryAsync120();
            Assert.That(timer.Elapsed.TotalMilliseconds, Is.LessThan(250d));
            Assert.That(owner.PendingTowerClaim120, Is.SameAs(claim));
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            owner.CancelTowerAutoBeforeSave116();
            owner.CancelTowerManualBeforeSave117();
            Assert.That(owner.SaveAndReloadProof().Succeeded, Is.False);
            Assert.That(owner.BankTowerVictoryAsync117().Result.Succeeded, Is.False);
            Assert.That(owner.StartTowerBattleAsync117().Result.Succeeded, Is.False);
            Assert.That(owner.AdvanceTowerAutoAfterVictoryAsync116().Result.Succeeded, Is.False);
            var frames = 0;
            while (!claim.IsCompleted) { frames++; Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(claim.IsFaulted, Is.False, claim.Exception?.ToString());
            Assert.That(claim.Result.Succeeded, Is.True, claim.Result.Message);
            Assert.That(frames, Is.GreaterThan(1));
            Assert.That(claim.Result.Message, Is.EqualTo(expected.Message));
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(expectedHash));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(expectedHash));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(originalHash));
            Assert.That(CanonicalJson.Sha256Hex(original), Is.EqualTo(originalHash));
            Assert.That(CanonicalJson.Sha256Hex(State(new M1RuntimeCoordinator(ContentRoot, path))), Is.EqualTo(expectedHash));
            Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(highest));
            Assert.That(owner.CampaignProgression022.ActiveAbyssOperationId, Is.Not.Empty);
            Assert.That(owner.TowerVictoryAwaitingClaim120, Is.False);
            Assert.That(changes, Is.EqualTo(1));
            var bytes = File.ReadAllBytes(path);
            var backup = File.ReadAllBytes(path + ".bak");
            var retry = owner.ClaimTowerBattleVictoryAsync120();
            yield return Wait(retry);
            Assert.That(retry.Result.Succeeded, Is.False);
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));

            // The legacy synchronous API accepts an already claimed reward as an
            // idempotent success and persists again; no canonical reward may change.
            Assert.That(owner.ClaimBattleRewards().Succeeded, Is.True);
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(expectedHash));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(expectedHash));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(expectedHash));
            Assert.That(CanonicalJson.Sha256Hex(State(new M1RuntimeCoordinator(ContentRoot, path))), Is.EqualTo(expectedHash));
            Assert.That(changes, Is.EqualTo(2));

            // A distinct legitimate Bank owns only Manual117's pending/cancel seam.
            var bank = owner.BankTowerVictoryAsync117();
            Assert.That(owner.PendingTowerManualTransition117, Is.SameAs(bank));
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            owner.CancelTowerClaimBeforeSave120();
            owner.CancelTowerAutoBeforeSave116();
            yield return Wait(bank);
            Assert.That(bank.Result.Succeeded, Is.True, bank.Result.Message);
            Assert.That(owner.CampaignProgression022.ActiveAbyssOperationId, Is.Null.Or.Empty);
            Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(315));
            Assert.That(changes, Is.EqualTo(3));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CancelClaimBeforeWriteKeepsVictoryAndExplicitRetryAfterStoreEntryStillCommits120()
        {
            var owner = Copy(_source, "canceled.json", out var path);
            var original = State(owner);
            var bytes = File.ReadAllBytes(path);
            var pending = owner.ClaimTowerBattleVictoryAsync120();
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            owner.CancelTowerClaimBeforeSave120();
            yield return Wait(pending);
            Assert.That(pending.Result.Succeeded, Is.False);
            Assert.That(State(owner), Is.SameAs(original));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.That(File.Exists(path + ".bak"), Is.False);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            Assert.That(owner.TowerVictoryAwaitingClaim120, Is.True);
            var retry = owner.ClaimTowerBattleVictoryAsync120();
            var timer = Stopwatch.StartNew();
            while (!owner.TowerClaimIsSaving120 && !retry.IsCompleted)
            { Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(owner.TowerClaimIsSaving120, Is.True);
            Assert.That(owner.TowerManualIsSaving117, Is.False);
            Assert.That(owner.TowerAutoIsSaving116, Is.False);
            owner.CancelTowerClaimBeforeSave120();
            yield return Wait(retry);
            Assert.That(retry.Result.Succeeded, Is.True, retry.Result.Message);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            Assert.That(State(owner).Battle.Reward.Claimed, Is.True);
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(State(owner))));
            Assert.That(State(new M1RuntimeCoordinator(ContentRoot, path)).Battle.Reward.Claimed, Is.True);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ManualAndClaimCancellationCannotCancelActualAutoTransition120()
        {
            var owner = Copy(_source, "auto.json", out var path);
            var auto = owner.AdvanceTowerAutoAfterVictoryAsync116();
            Assert.That(owner.PendingTowerAutoTransition116, Is.SameAs(auto));
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            owner.CancelTowerManualBeforeSave117();
            owner.CancelTowerClaimBeforeSave120();
            yield return Wait(auto);
            Assert.That(auto.Result.Succeeded, Is.True, auto.Result.Message);
            Assert.That(owner.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(315));
            Assert.That(owner.CampaignProgression022.TowerFloorNumber, Is.EqualTo(316));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(State(owner))));
        }

        M1RuntimeCoordinator Copy(string source, string name, out string path)
        {
            path = Path.Combine(_directory, name);
            File.Copy(source, path);
            var owner = new M1RuntimeCoordinator(ContentRoot, path);
            _owners.Add(owner);
            Assert.That(owner.CampaignProgression022.TowerAuthorityError094, Is.Empty);
            return owner;
        }
        static IEnumerator Wait(Task<M1CommandResult> task)
        {
            var timer = Stopwatch.StartNew();
            while (!task.IsCompleted) { Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(task.IsFaulted, Is.False, task.Exception?.ToString());
            Assert.That(task.IsCanceled, Is.False);
        }
        static SaveEnvelopeV1 Read(string path)
        {
            var result = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            return result.Value;
        }
        static string Hash(string path)
        {
            using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static CampaignState State(M1RuntimeCoordinator owner) => (CampaignState)typeof(M1RuntimeCoordinator)
            .GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
#endif
