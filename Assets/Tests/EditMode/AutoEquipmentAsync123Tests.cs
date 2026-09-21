#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AutoEquipmentAsync123Tests
    {
        const string SourceHash = "D605FB18B9945A4F1BC7816909A6F775D7F489E2073B1842DFF00BB10F92424B";
        const string DefaultSource = @"C:\SecondDimension\BuildEvidence\Reset110\R280_GateA_NativeReload\EarnedReviewSave097.json";
        const string Prefix = "SecondDimensionAutoEquipment123_";
        string _source, _directory;
        readonly List<M1RuntimeCoordinator> _owners = new List<M1RuntimeCoordinator>();

        [SetUp]
        public void SetUp123()
        {
            _source = Environment.GetEnvironmentVariable("SD_AUTO_EQUIPMENT123_SOURCE");
            if (!string.IsNullOrWhiteSpace(_source) && !File.Exists(_source))
                Assert.Fail("Configured equipment source is missing: " + _source);
            if (string.IsNullOrWhiteSpace(_source)) _source = DefaultSource;
            if (!File.Exists(_source)) Assert.Ignore("Use the pinned R280 copy via SD_AUTO_EQUIPMENT123_SOURCE.");
            Assert.That(Hash(_source), Is.EqualTo(SourceHash), "Never use or modify the live profile.");
            _directory = Path.Combine(Path.GetTempPath(), Prefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [UnityTearDown]
        public IEnumerator TearDown123()
        {
            foreach (var owner in _owners)
            {
                owner.CancelAutoEquipmentBeforeSave123();
                owner.CancelTowerAutoBeforeSave116();
                owner.CancelTowerManualBeforeSave117();
                owner.CancelTowerClaimBeforeSave120();
            }
            foreach (var owner in _owners)
            {
                var pending = owner.PendingAutoEquipment123 ?? owner.PendingTowerAutoTransition116 ??
                    owner.PendingTowerManualTransition117 ?? owner.PendingTowerClaim120;
                if (pending != null) yield return Wait(pending);
            }
            _owners.Clear();
            if (!string.IsNullOrWhiteSpace(_source) && File.Exists(_source))
                Assert.That(Hash(_source), Is.EqualTo(SourceHash));
            if (!string.IsNullOrWhiteSpace(_directory) && Directory.Exists(_directory))
            {
                var full = Path.GetFullPath(_directory);
                var temp = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.That(full.StartsWith(temp, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(full).StartsWith(Prefix, StringComparison.Ordinal), Is.True);
                Directory.Delete(full, true);
            }
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator RealHeroAllAndUndoMatchSynchronousAuthorityWithOneSaveAndNoOpRetainsUndo123()
        {
            var sync = Ready("sync.json", out var syncPath, out var selected);
            // Create a real owned upgrade opportunity without inventing an item.
            Require(sync.SetEquipmentLock(selected[0], EquipmentSlotIds.MainHand, true));
            Require(sync.UnequipItem(selected[0], EquipmentSlotIds.BodyArmor));
            var owner = Copy(syncPath, "async.json", out var path);
            var beforeHero = State(owner);
            yield return MatchOnce(sync, owner, path, () => sync.AutoEquipHero112(selected[0]),
                () => owner.AutoEquipHeroAsync123(selected[0]));
            var equippedHero = State(owner);
            Assert.That(equippedHero.EquipmentUndo112, Is.Not.Null);
            Assert.That(equippedHero.EquipmentUndo112.Changes, Is.Not.Empty);
            Assert.That(equippedHero.EquipmentUndo112.Changes.All(change => change.RecruitId == selected[0]), Is.True);
            AssertUnselectedEquipment(beforeHero, equippedHero, new HashSet<string> { selected[0] });
            owner = Reload(path, equippedHero);
            Assert.That(owner.CanUndoAutoEquip112, Is.True);
            yield return MatchOnce(sync, owner, path, sync.UndoAutoEquip112, owner.UndoAutoEquipAsync123);
            Assert.That(Owners(State(owner)), Is.EqualTo(Owners(beforeHero)));
            Assert.That(State(owner).EquipmentUndo112, Is.Null);

            Require(sync.UnequipItem(selected[1], EquipmentSlotIds.BodyArmor));
            Require(owner.UnequipItem(selected[1], EquipmentSlotIds.BodyArmor));
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(CanonicalJson.Sha256Hex(State(sync))));
            var beforeAll = State(owner);
            yield return MatchOnce(sync, owner, path, sync.AutoEquipAllUnions112, owner.AutoEquipAllUnionsAsync123);
            var equippedAll = State(owner);
            Assert.That(equippedAll.EquipmentUndo112.Changes.Select(change => change.RecruitId).Distinct().Count(),
                Is.GreaterThanOrEqualTo(2), "Both real empty body slots must be filled by the batch.");
            var active = new HashSet<string>(beforeAll.Guild.Unions.Where(union => union.Kind == UnionKind.Normal)
                .SelectMany(union => union.MemberRecruitIds), StringComparer.Ordinal);
            AssertUnselectedEquipment(beforeAll, equippedAll, active);
            Assert.That(CanonicalJson.Serialize(equippedAll.Guild.Recruits.Single(recruit => recruit.RecruitId == selected[0])
                    .Equipment.Find(EquipmentSlotIds.MainHand).Item),
                Is.EqualTo(CanonicalJson.Serialize(beforeHero.Guild.Recruits.Single(recruit => recruit.RecruitId == selected[0])
                    .Equipment.Find(EquipmentSlotIds.MainHand).Item)), "The actual locked weapon stays on its owner.");

            var primary = File.ReadAllBytes(path);
            var backup = File.ReadAllBytes(path + ".bak");
            var notifications = 0;
            void NoOpChanged() => notifications++;
            owner.Changed += NoOpChanged;
            var expectedNoOp = sync.AutoEquipAllUnions112();
            var noOp = owner.AutoEquipAllUnionsAsync123();
            yield return Wait(noOp);
            owner.Changed -= NoOpChanged;
            Require(expectedNoOp);
            Require(noOp.Result);
            Assert.That(noOp.Result.Message, Is.EqualTo(expectedNoOp.Message));
            Assert.That(State(owner), Is.SameAs(equippedAll), "No-upgrade preserves the immutable source and its Undo.");
            Assert.That(notifications, Is.Zero);
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
            owner = Reload(path, equippedAll);
            Assert.That(owner.CanUndoAutoEquip112, Is.True);
            yield return MatchOnce(sync, owner, path, sync.UndoAutoEquip112, owner.UndoAutoEquipAsync123);
            Assert.That(Owners(State(owner)), Is.EqualTo(Owners(beforeAll)));
            Assert.That(State(owner).EquipmentUndo112, Is.Null);
            var undone = State(owner);
            owner = Reload(path, undone);
            Assert.That(owner.CanUndoAutoEquip112, Is.False);
            var reloadedUndone = State(owner);
            primary = File.ReadAllBytes(path);
            backup = File.ReadAllBytes(path + ".bak");
            var repeatUndo = owner.UndoAutoEquipAsync123();
            yield return Wait(repeatUndo);
            Assert.That(repeatUndo.Result.Succeeded, Is.False);
            Assert.That(State(owner), Is.SameAs(reloadedUndone));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator CancelBeforeWritePreservesFilesAndLeaseBlocksOtherModesThenStoreCutoffCommits123()
        {
            var owner = Ready("cancel.json", out var path, out var selected);
            Require(owner.UnequipItem(selected[0], EquipmentSlotIds.BodyArmor));
            var original = State(owner);
            var originalHash = CanonicalJson.Sha256Hex(original);
            var primary = File.ReadAllBytes(path);
            var backup = File.ReadAllBytes(path + ".bak");
            var notifications = 0;
            owner.Changed += () => notifications++;
            var pending = owner.AutoEquipHeroAsync123(selected[0]);
            Assert.That(pending.IsCompleted, Is.False, "Actual candidate work must yield to the main context.");
            Assert.That(owner.PendingAutoEquipment123, Is.SameAs(pending));
            AssertOtherModesHidden(owner);
            foreach (var blocked in new[] { owner.AutoEquipHero112(selected[0]), owner.AutoEquipAllUnions112(),
                owner.UndoAutoEquip112(), owner.BankTowerVictory110(), owner.StartTowerBattle110(),
                owner.ClaimBattleRewards(), owner.SaveAndReloadProof(), owner.CreateGuild(null) })
                Assert.That(blocked.Succeeded, Is.False, "The equipment path lease blocks concurrent write commands.");
            foreach (var blocked in new[] { owner.AutoEquipAllUnionsAsync123(), owner.UndoAutoEquipAsync123(),
                owner.AdvanceTowerAutoAfterVictoryAsync116(), owner.BankTowerVictoryAsync117(),
                owner.StartTowerBattleAsync117(), owner.ClaimTowerBattleVictoryAsync120() })
            {
                Assert.That(blocked.IsCompleted, Is.True);
                Assert.That(blocked.Result.Succeeded, Is.False);
            }
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            owner.CancelAutoEquipmentBeforeSave123();
            yield return Wait(pending);
            Assert.That(pending.Result.Succeeded, Is.False);
            Assert.That(pending.Result.Message, Does.Contain("canceled before saving"));
            Assert.That(State(owner), Is.SameAs(original));
            Assert.That(CanonicalJson.Sha256Hex(original), Is.EqualTo(originalHash));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
            Assert.That(notifications, Is.Zero);
            Assert.That(owner.PendingAutoEquipment123, Is.Null);
            Reload(path, original); // Same-path construction becomes legal after settlement.

            var retry = owner.AutoEquipHeroAsync123(selected[0]);
            owner.CancelTowerAutoBeforeSave116();
            owner.CancelTowerManualBeforeSave117();
            owner.CancelTowerClaimBeforeSave120();
            Assert.That(owner.PendingAutoEquipment123, Is.SameAs(retry));
            AssertOtherModesHidden(owner);
            var clock = Stopwatch.StartNew();
            while (!owner.AutoEquipmentIsSaving123 && !retry.IsCompleted)
            { Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
            Assert.That(owner.AutoEquipmentIsSaving123, Is.True, "Observe the actual atomic-store cutoff.");
            owner.CancelAutoEquipmentBeforeSave123();
            Assert.Throws<InvalidOperationException>(() => new M1RuntimeCoordinator(ContentRoot, path));
            yield return Wait(retry);
            Require(retry.Result);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(owner.PendingAutoEquipment123, Is.Null);
            Assert.That(owner.AutoEquipmentIsSaving123, Is.False);
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path + ".bak"));
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(State(owner))));
            AssertRetained(original, State(owner));
            Reload(path, State(owner));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FailedStorePreservesEquipmentAndRetrySurvivesObserverFailureAndReload123()
        {
            var owner = Ready("failure.json", out var path, out var selected);
            Require(owner.UnequipItem(selected[0], EquipmentSlotIds.BodyArmor));
            var original = State(owner);
            var primary = File.ReadAllBytes(path);
            var backup = File.ReadAllBytes(path + ".bak");
            Directory.CreateDirectory(path + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex("SAVE_WRITE_FAILED109"));
            var failed = owner.AutoEquipHeroAsync123(selected[0]);
            yield return Wait(failed);
            Assert.That(failed.Result.Succeeded, Is.False);
            Assert.That(State(owner), Is.SameAs(original));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path));
            CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
            Assert.That(owner.PendingAutoEquipment123, Is.Null);
            Directory.Delete(path + ".tmp");
            var notifications = 0;
            owner.Changed += () => { notifications++; throw new InvalidOperationException("equipment observer123"); };
            LogAssert.Expect(LogType.Error, new Regex("POST_SAVE_PRESENTATION_REFRESH_FAILED109"));
            var retry = owner.AutoEquipHeroAsync123(selected[0]);
            yield return Wait(retry);
            Require(retry.Result);
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(owner.PendingAutoEquipment123, Is.Null);
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(State(owner))));
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(path + ".bak"));
            AssertRetained(original, State(owner));
            Reload(path, State(owner));
        }

        IEnumerator MatchOnce(M1RuntimeCoordinator sync, M1RuntimeCoordinator owner, string path,
            Func<M1CommandResult> synchronous, Func<Task<M1CommandResult>> asynchronous)
        {
            var before = State(owner);
            var beforeHash = CanonicalJson.Sha256Hex(before);
            var precedingPrimary = File.ReadAllBytes(path);
            var expected = synchronous();
            Require(expected);
            var expectedHash = CanonicalJson.Sha256Hex(State(sync));
            var notifications = 0;
            var mainThread = Thread.CurrentThread.ManagedThreadId;
            var publishedThread = -1;
            void Changed() { notifications++; publishedThread = Thread.CurrentThread.ManagedThreadId; }
            owner.Changed += Changed;
            var pending = asynchronous();
            Assert.That(owner.PendingAutoEquipment123, Is.SameAs(pending));
            AssertOtherModesHidden(owner);
            yield return Wait(pending);
            owner.Changed -= Changed;
            Require(pending.Result);
            Assert.That(pending.Result.Message, Is.EqualTo(expected.Message));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(publishedThread, Is.EqualTo(mainThread));
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(expectedHash));
            Assert.That(CanonicalJson.Sha256Hex(before), Is.EqualTo(beforeHash), "Worker planning must not mutate its source.");
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(expectedHash));
            CollectionAssert.AreEqual(precedingPrimary, File.ReadAllBytes(path + ".bak"),
                "Exactly one atomic write; no intermediate unequip or partial allocation save.");
            Assert.That(File.Exists(path + ".tmp"), Is.False);
            Assert.That(owner.PendingAutoEquipment123, Is.Null);
            AssertRetained(before, State(owner));
        }

        M1RuntimeCoordinator Ready(string name, out string path, out string[] selected)
        {
            var owner = Copy(_source, name, out path);
            var state = State(owner);
            Assert.That(state.Guild.Recruits.Count, Is.EqualTo(69));
            Assert.That(AutoEquipmentService112.MutationBlockedReason(state), Is.Empty);
            var active = new HashSet<string>(state.Guild.Unions.Where(union => union.Kind == UnionKind.Normal)
                .SelectMany(union => union.MemberRecruitIds), StringComparer.Ordinal);
            selected = state.Guild.Recruits.Where(recruit => active.Contains(recruit.RecruitId) &&
                    ProtectedActorPolicy.CanUseNormalEquipment(recruit) &&
                    recruit.Equipment.Find(EquipmentSlotIds.BodyArmor)?.Item.PlayerLocked == false &&
                    recruit.Equipment.Find(EquipmentSlotIds.MainHand)?.Item.PlayerLocked == false)
                .OrderBy(recruit => recruit.RecruitId, StringComparer.Ordinal).Take(2).Select(recruit => recruit.RecruitId).ToArray();
            Assert.That(selected, Has.Length.EqualTo(2));
            return owner;
        }

        M1RuntimeCoordinator Copy(string source, string name, out string path)
        {
            path = Path.Combine(_directory, name);
            File.Copy(source, path);
            var owner = new M1RuntimeCoordinator(ContentRoot, path);
            _owners.Add(owner);
            return owner;
        }

        M1RuntimeCoordinator Reload(string path, CampaignState expected)
        {
            var owner = new M1RuntimeCoordinator(ContentRoot, path);
            _owners.Add(owner);
            Assert.That(CanonicalJson.Sha256Hex(State(owner)), Is.EqualTo(CanonicalJson.Sha256Hex(expected)));
            return owner;
        }

        static void AssertOtherModesHidden(M1RuntimeCoordinator owner)
        {
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            Assert.That(owner.TowerAutoIsSaving116, Is.False);
            Assert.That(owner.TowerManualIsSaving117, Is.False);
            Assert.That(owner.TowerClaimIsSaving120, Is.False);
        }

        static void AssertRetained(CampaignState before, CampaignState after)
        {
            string[] Items(CampaignState state) => state.Guild.Inventory.Concat(state.Guild.Recruits
                    .SelectMany(recruit => recruit.Equipment.Assignments.Select(slot => slot.Item)))
                .Select(CanonicalJson.Serialize).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            string WithoutEquipment(RecruitState recruit)
            { var row = JObject.FromObject(recruit); row.Remove("Equipment"); return CanonicalJson.Serialize(row); }
            Assert.That(Items(after), Is.EqualTo(Items(before)));
            Assert.That(after.Guild.Recruits.Select(WithoutEquipment), Is.EqualTo(before.Guild.Recruits.Select(WithoutEquipment)),
                "Keep every identity, vital, learned Art and progression record.");
            Assert.That(CanonicalJson.Serialize(after.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(before.Guild.Unions)));
            Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
        }

        static void AssertUnselectedEquipment(CampaignState before, CampaignState after, ISet<string> selected)
        {
            foreach (var recruit in before.Guild.Recruits.Where(recruit => !selected.Contains(recruit.RecruitId)))
                Assert.That(CanonicalJson.Serialize(after.Guild.Recruits.Single(value => value.RecruitId == recruit.RecruitId).Equipment),
                    Is.EqualTo(CanonicalJson.Serialize(recruit.Equipment)));
        }

        static string[] Owners(CampaignState state) => state.Guild.Recruits.SelectMany(recruit => recruit.Equipment.Assignments
            .Select(slot => recruit.RecruitId + ":" + slot.SlotId + ":" + slot.Item.InstanceId)).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        static void Require(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);
        static IEnumerator Wait(Task<M1CommandResult> task)
        {
            var clock = Stopwatch.StartNew();
            while (!task.IsCompleted) { Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(300d)); yield return null; }
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
