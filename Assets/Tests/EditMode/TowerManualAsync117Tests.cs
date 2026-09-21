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
    public sealed class TowerManualAsync117Tests
    {
        const string SourceHash = "511D162B27A4FCD6371BE562EF37BD0FEB4B27EC29C81F83B03A09C5381DDC1E";
        const string DefaultSource = @"C:\SecondDimension\BuildEvidence\Reset110\R288_TowerSoak301_16x_1800s\EarnedReviewSave097.json";
        const string Prefix = "SecondDimensionTowerManual117_";
        string _source, _directory;
        readonly List<M1RuntimeCoordinator> _owners = new List<M1RuntimeCoordinator>();

        [SetUp]
        public void SetUp117()
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
        public IEnumerator TearDown117()
        {
            foreach (var owner in _owners) { owner.CancelTowerManualBeforeSave117(); owner.CancelTowerAutoBeforeSave116(); }
            foreach (var owner in _owners)
            {
                var pending = owner.PendingTowerManualTransition117 ?? owner.PendingTowerAutoTransition116;
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
        public IEnumerator RealBankAndStartMatchSynchronousAuthorityOneSaveEachAndRejectRetries117()
        {
            var sync = Copy(_source, "sync.json", out var syncPath);
            var claim = sync.ClaimBattleRewards();
            Assert.That(claim.Succeeded, Is.True, claim.Message);
            var owner = Copy(syncPath, "async.json", out var path);
            var claimed = State(owner);
            var claimedHash = CanonicalJson.Sha256Hex(claimed);
            var expectedBank = sync.BankTowerVictory110();
            Assert.That(expectedBank.Succeeded, Is.True, expectedBank.Message);
            var bankedHash = CanonicalJson.Sha256Hex(State(sync));
            var changes = 0;
            owner.Changed += () => changes++;
            var dispatch = Stopwatch.StartNew();
            var bank = owner.BankTowerVictoryAsync117();
            Assert.That(dispatch.Elapsed.TotalMilliseconds, Is.LessThan(250d));
            Assert.That(owner.PendingTowerManualTransition117, Is.SameAs(bank));
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null, "Manual work must not attach an Auto continuation observer.");
            yield return Wait(bank);
            Assert.That(bank.Result.Succeeded, Is.True, bank.Result.Message);
            Assert.That(bank.Result.Message, Is.EqualTo(expectedBank.Message));
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(bankedHash));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(bankedHash));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(claimedHash), "One bank save, without beginning the next floor.");
            Assert.That(CanonicalJson.Sha256Hex(claimed), Is.EqualTo(claimedHash));
            Assert.That(owner.CampaignProgression022.ActiveAbyssOperationId, Is.Null.Or.Empty);
            Assert.That(changes, Is.EqualTo(1));
            var primaryBytes = File.ReadAllBytes(path);
            var backupBytes = File.ReadAllBytes(path + ".bak");
            var duplicateBank = owner.BankTowerVictoryAsync117();
            yield return Wait(duplicateBank);
            Assert.That(duplicateBank.Result.Succeeded, Is.False);
            CollectionAssert.AreEqual(primaryBytes, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backupBytes, File.ReadAllBytes(path + ".bak"));

            var expectedStart = sync.StartTowerBattle110();
            Assert.That(expectedStart.Succeeded, Is.True, expectedStart.Message);
            var nextHash = CanonicalJson.Sha256Hex(State(sync));
            dispatch.Restart();
            var start = owner.StartTowerBattleAsync117();
            Assert.That(dispatch.Elapsed.TotalMilliseconds, Is.LessThan(250d));
            yield return Wait(start);
            Assert.That(start.Result.Succeeded, Is.True, start.Result.Message);
            Assert.That(start.Result.Message, Is.EqualTo(expectedStart.Message));
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(nextHash));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(bankedHash), "One Start save contains encounter and battle.");
            Assert.That(changes, Is.EqualTo(2));
            var reloaded = new M1RuntimeCoordinator(ContentRoot, path);
            Assert.That(CanonicalJson.Sha256Hex(State(reloaded)), Is.EqualTo(nextHash));
            Assert.That(reloaded.CampaignProgression022.TowerFloorNumber, Is.EqualTo(316));
            Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(315));
            primaryBytes = File.ReadAllBytes(path);
            backupBytes = File.ReadAllBytes(path + ".bak");
            var duplicateStart = owner.StartTowerBattleAsync117();
            yield return Wait(duplicateStart);
            Assert.That(duplicateStart.Result.Succeeded, Is.False);
            CollectionAssert.AreEqual(primaryBytes, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backupBytes, File.ReadAllBytes(path + ".bak"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator ManualCancelRetainsClaimedVictoryAndSharedLeaseAllowsRetry117()
        {
            var preparation = Copy(_source, "preparation.json", out var preparationPath);
            Assert.That(preparation.ClaimBattleRewards().Succeeded, Is.True);
            var owner = Copy(preparationPath, "canceled.json", out var path);
            var original = State(owner);
            var bytes = File.ReadAllBytes(path);
            var task = owner.BankTowerVictoryAsync117();
            Assert.That(owner.BankTowerVictory110().Succeeded, Is.False);
            Assert.That(owner.StartTowerBattle110().Succeeded, Is.False);
            Assert.That(owner.SaveAndReloadProof().Succeeded, Is.False);
            Assert.That(owner.AdvanceTowerAutoAfterVictoryAsync116().Result.Succeeded, Is.False);
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            // Hiding the unrelated battle controller cannot cancel manual work.
            owner.CancelTowerAutoBeforeSave116();
            Assert.That(owner.PendingTowerManualTransition117, Is.SameAs(task));
            owner.CancelTowerManualBeforeSave117();
            yield return Wait(task);
            Assert.That(task.Result.Succeeded, Is.False);
            Assert.That(task.Result.Message, Does.Contain("canceled before saving"));
            Assert.That(State(owner), Is.SameAs(original));
            CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Assert.That(File.Exists(path + ".bak"), Is.False);
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            var bank = owner.BankTowerVictory110();
            Assert.That(bank.Succeeded, Is.True, bank.Message);
            var start = owner.StartTowerBattleAsync117();
            var timer = Stopwatch.StartNew();
            while (!owner.TowerManualIsSaving117 && !start.IsCompleted)
            { Assert.That(timer.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(owner.TowerManualIsSaving117, Is.True);
            owner.CancelTowerManualBeforeSave117();
            yield return Wait(start);
            Assert.That(start.Result.Succeeded, Is.True, start.Result.Message);
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(State(owner))));
            Assert.That(new M1RuntimeCoordinator(ContentRoot, path).CampaignProgression022.TowerFloorNumber, Is.EqualTo(316));
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
