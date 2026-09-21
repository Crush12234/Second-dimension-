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
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    // Synthetic fresh guild, real opening/encounter/forecast/round/reward commands.
    // No positive fixture edits battle HP, outcome, phase, seed or authority hashes.
    // This is deterministic integration coverage, not a native player wipe claim.
    public sealed class TowerRestart130Tests
    {
        const string Prefix = "SecondDimensionTowerRestart130_";
        string _fixtureDirectory, _directory;
        CampaignState _defeat, _beforeDefeat;
        M2CombatContent _combat;
        readonly List<M1RuntimeCoordinator> _owners = new List<M1RuntimeCoordinator>();
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static CampaignProgressionState022 Progress(CampaignState value) => value.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        [OneTimeSetUp]
        public void ReachPartyDefeatThroughRealCombatCommands130()
        {
            _fixtureDirectory = NewDirectory();
            var opening = new M1OpeningFlowTests(); opening.SetUp();
            var fresh = (CampaignState)typeof(M1OpeningFlowTests).GetMethod("BuildCompletedOpening", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(opening, null);
            var path = Path.Combine(_fixtureDirectory, "fresh-command-fixture.json");
            Write(path, fresh);
            var coordinator = new M1RuntimeCoordinator(ContentRoot, path);
            _combat = (M2CombatContent)Field("_combatContent").GetValue(coordinator);
            // Two real victories establish a record above the replay's first floor.
            for (var floor = 1; floor <= 3; floor++)
            {
                Command(coordinator.StartTowerBattle110());
                var started = State(coordinator);
                Assert.That(Tower(started).ActiveActualFloor, Is.EqualTo(floor));
                if (floor == 3) _beforeDefeat = started;
                var terminal = Play(started, floor == 3 ? "CMD_GUARD" : null);
                Assert.That(terminal.Battle.Outcome, Is.EqualTo(floor == 3 ? BattleOutcome.Defeat : BattleOutcome.Victory),
                    "The deterministic real-command fixture must naturally reach its required outcome; do not manufacture it.");
                Assert.That(M2BattleCommandService.HasValidFinalStateHash090(terminal.Battle), Is.True);
                if (floor == 3) { _defeat = terminal; break; }
                Write(path, terminal);
                coordinator = new M1RuntimeCoordinator(ContentRoot, path);
                Command(coordinator.ClaimBattleRewards());
                Command(coordinator.BankTowerVictory110());
            }
            Assert.That(CampaignProgressionCommandService022.IsFullPartyDefeat130(_defeat.Battle), Is.True);
            Assert.That(Tower(_defeat).HighestActualFloor, Is.EqualTo(2));
            TestContext.Progress.WriteLine("Tower130 synthetic fresh guild: two real victories, then real Guard-command full-party defeat on floor 3, round " + _defeat.Battle.Round + ".");
            var export = Environment.GetEnvironmentVariable("SD_TOWER130_DEFEAT_OUTPUT");
            if (!string.IsNullOrWhiteSpace(export))
            {
                var full = Path.GetFullPath(export);
                const string allowedRoot = @"C:\SecondDimension\BuildEvidence\Reset110\";
                Assert.That(full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) &&
                    Path.GetFileName(full) == "CommandEarnedDefeat130.json", Is.True,
                    "Optional fixture export must be a new named file beneath Reset110 evidence, never the personal profile.");
                Assert.That(File.Exists(full) || File.Exists(full + ".bak") || File.Exists(full + ".tmp"), Is.False);
                var parent = new DirectoryInfo(Path.GetDirectoryName(full));
                while (parent != null && parent.FullName.StartsWith(allowedRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                { if (parent.Exists) Assert.That((parent.Attributes & FileAttributes.ReparsePoint) == 0, Is.True); parent = parent.Parent; }
                Write(full, _defeat);
                TestContext.Progress.WriteLine("COMMAND-EARNED TEST FIXTURE (synthetic fresh guild, real combat commands; not personal/native defeat): " +
                    full + " SHA256=" + Hash(full) + " Canonical=" + CanonicalJson.Sha256Hex(_defeat));
            }
        }

        [SetUp] public void SetUp130() { _directory = NewDirectory(); }
        [UnityTearDown] public IEnumerator TearDown130()
        {
            foreach (var owner in _owners) owner.CancelTowerRestartBeforeSave130();
            foreach (var owner in _owners) if (owner.PendingTowerRestart130 != null) yield return Wait(owner.PendingTowerRestart130);
            _owners.Clear(); RemoveDirectory(_directory);
        }
        [OneTimeTearDown] public void End130() { RemoveDirectory(_fixtureDirectory); }

        [UnityTest, Timeout(600000)]
        public IEnumerator RealDefeatRestartsInOneSaveAndPreservesRecordThenRealReplayUsesDistinctReceipts130()
        {
            var owner = Owner(_defeat, "restart.json", out var path);
            var source = State(owner); var sourceHash = CanonicalJson.Sha256Hex(source);
            var expectedClaim = Require(new M2BattleCommandService().ClaimBattleRewards(source));
            var changes = 0; owner.Changed += () => changes++;
            var task = owner.RestartTowerFromFloorOneAsync130();
            Assert.That(owner.PendingTowerRestart130, Is.SameAs(task));
            Assert.That(owner.PendingTowerAutoTransition116, Is.Null);
            Assert.That(owner.PendingTowerManualTransition117, Is.Null);
            Assert.That(owner.PendingTowerClaim120, Is.Null);
            Assert.That(owner.StartTowerBattle110().Succeeded, Is.False);
            Assert.That(owner.RestartTowerFromFloorOneAsync130().Result.Succeeded, Is.False);
            // Unrelated views cannot cancel this mode's lease.
            owner.CancelTowerAutoBeforeSave116(); owner.CancelTowerManualBeforeSave117(); owner.CancelTowerClaimBeforeSave120();
            yield return Wait(task); Command(task.Result);
            Assert.That(changes, Is.EqualTo(1));
            var restarted = State(owner); var view = Tower(restarted);
            Assert.That(view.HighestActualFloor, Is.EqualTo(2));
            Assert.That(view.CurrentRunClearedFloor130, Is.Zero);
            Assert.That(view.ActiveActualFloor, Is.EqualTo(1));
            Assert.That(view.NextActualFloor, Is.EqualTo(1));
            Assert.That(view.LatestCompletedActualFloor130, Is.EqualTo(2));
            Assert.That(Progress(restarted).ActiveAbyssOperation.Status, Is.EqualTo(AbyssOperationStatus022.Active));
            Assert.That(restarted.Battle.BattleId, Is.EqualTo(source.Battle.BattleId), "Restart prepares floor 1 without silently starting its battle.");
            Assert.That(Progress(restarted).ActiveAbyssOperation.OperationDefinitionId, Does.EndWith("ENDLESS_BATTLE"));
            Assert.That(Progress(source).AbyssAuthorityEntries.Select(x => x.OperationInstanceId), Does.Not.Contain(Progress(restarted).ActiveAbyssOperation.OperationInstanceId));
            Assert.That(restarted.Guild.TreasuryXp, Is.EqualTo(expectedClaim.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Sha256Hex(restarted.Guild.Unions), Is.EqualTo(CanonicalJson.Sha256Hex(source.Guild.Unions)));
            CollectionAssert.AreEqual(source.Guild.Recruits.Select(x => x.RecruitId), restarted.Guild.Recruits.Select(x => x.RecruitId));
            foreach (var item in source.Guild.Inventory)
                Assert.That(CanonicalJson.Sha256Hex(restarted.Guild.Inventory.Single(x => x.InstanceId == item.InstanceId)), Is.EqualTo(CanonicalJson.Sha256Hex(item)), "Restart preserves every previously owned inventory instance.");
            foreach (var recruit in source.Guild.Recruits)
                Assert.That(CanonicalJson.Sha256Hex(restarted.Guild.Recruits.Single(x => x.RecruitId == recruit.RecruitId).Equipment), Is.EqualTo(CanonicalJson.Sha256Hex(recruit.Equipment)), "Defeat claim and restart never replace an existing loadout or lock.");
            Assert.That(CanonicalJson.Sha256Hex(Progress(restarted).AbyssFloors), Is.EqualTo(CanonicalJson.Sha256Hex(Progress(source).AbyssFloors)));
            Assert.That(Progress(restarted).AbyssAuthorityEntries.Count, Is.EqualTo(Progress(source).AbyssAuthorityEntries.Count + 1));
            CollectionAssert.AreEqual(Progress(source).AbyssAuthorityEntries.Select(x => x.EntryHash), Progress(restarted).AbyssAuthorityEntries.Take(Progress(source).AbyssAuthorityEntries.Count).Select(x => x.EntryHash));
            var restartEntry = Progress(restarted).AbyssAuthorityEntries.Last();
            Assert.That(restartEntry.TowerRestart130.DefeatBattle.BattleId, Is.EqualTo(source.Battle.BattleId));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(sourceHash), "One canonical transaction includes defeat claim, restart proof and idle floor 1.");
            Assert.That(Read(path).CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(restarted)));
            Assert.That(CanonicalJson.Sha256Hex(source), Is.EqualTo(sourceHash));
            var reloaded = new M1RuntimeCoordinator(ContentRoot, path);
            Assert.That(CanonicalJson.Sha256Hex(State(reloaded)), Is.EqualTo(CanonicalJson.Sha256Hex(restarted)));
            Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(2));
            Assert.That(reloaded.CampaignProgression022.TowerFloorNumber, Is.EqualTo(1));
            var saved = File.ReadAllBytes(path); var backup = File.ReadAllBytes(path + ".bak");
            var duplicate = reloaded.RestartTowerFromFloorOneAsync130(); yield return Wait(duplicate);
            Assert.That(duplicate.Result.Succeeded, Is.False);
            CollectionAssert.AreEqual(saved, File.ReadAllBytes(path)); CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));

            Command(reloaded.StartTowerBattle110());
            var replay = Play(State(reloaded), null);
            Assert.That(replay.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            var oldBattleRewardIds = Progress(source).AbyssAuthorityEntries.Where(x => !x.Aborted).Select(x => x.CompletionProof.ExistingBattleRewardReceiptId).ToArray();
            Assert.That(oldBattleRewardIds, Does.Not.Contain(replay.Battle.Reward.RewardId));
            Write(path, replay); reloaded = new M1RuntimeCoordinator(ContentRoot, path);
            Command(reloaded.ClaimBattleRewards()); var beforeReplayBank = State(reloaded); Command(reloaded.BankTowerVictory110());
            var replayed = State(reloaded); view = Tower(replayed);
            Assert.That(view.HighestActualFloor, Is.EqualTo(2));
            Assert.That(view.CurrentRunClearedFloor130, Is.EqualTo(1));
            Assert.That(view.NextActualFloor, Is.EqualTo(2));
            Assert.That(view.LatestCompletedActualFloor130, Is.EqualTo(1));
            var recap = (SecondDimension.Presentation.Campaign022.TowerSavedReward110)typeof(M1RuntimeCoordinator)
                .GetMethod("PrepareTowerRewardDisplay110", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(reloaded, new object[] { beforeReplayBank, replayed, CampaignRegistry022.LoadFromResources() });
            Assert.That(recap.Floor, Is.EqualTo(1), "Recap names the actually completed floor below the retained record.");
            Assert.That(Progress(replayed).AbyssAuthorityEntries.Select(x => x.EntryHash).Distinct().Count(), Is.EqualTo(Progress(replayed).AbyssAuthorityEntries.Count));
            Assert.That(Tower(State(new M1RuntimeCoordinator(ContentRoot, path))).HighestActualFloor, Is.EqualTo(2));
            var movedRecruit = replayed.Guild.Unions.First(x => x.MemberRecruitIds.Count > 1).MemberRecruitIds.Last();
            Command(reloaded.UnassignRecruitFromUnion(movedRecruit));
            Assert.That(Tower(State(new M1RuntimeCoordinator(ContentRoot, path))).HighestActualFloor, Is.EqualTo(2),
                "Retained original admission remains valid after a legitimate later roster change.");
        }

        [Test]
        public void OnlyMatchingFullPartyDeathQualifiesAndOrdinaryRetreatKeepsItsFloor130()
        {
            var service = new CampaignProgressionCommandService022(); var registry = CampaignRegistry022.LoadFromResources();
            Assert.That(service.CanRestartTowerAfterPartyDefeat130(_beforeDefeat, registry), Is.False);
            Assert.That(service.CanRestartTowerAfterPartyDefeat130(_defeat, registry), Is.True);
            Assert.That(service.RestartTowerAfterPartyDefeat130(_defeat, registry).IsSuccess, Is.False, "Unclaimed defeat is not accepted by the pure restart authority.");
            var owner = Owner(_defeat, "ordinary-defeat-return.json", out _);
            Command(owner.RetreatTowerRun081());
            Assert.That(Tower(State(owner)).NextActualFloor, Is.EqualTo(3));
            Assert.That(Tower(State(owner)).HighestActualFloor, Is.EqualTo(2));
            Assert.That(Progress(State(owner)).AbyssAuthorityEntries.Last().TowerRestart130, Is.Null);
            // Explicit negative tampering: even a Defeat outcome with a surviving
            // member cannot qualify. The positive fixture above never does this.
            var unions = _defeat.Battle.PlayerUnions.ToArray();
            unions[0] = unions[0].With(members: unions[0].Members.Select((m, i) => i == 0 ? m.With(currentHp: 1) : m).ToArray(), retreated: true);
            Assert.That(CampaignProgressionCommandService022.IsFullPartyDefeat130(_defeat.Battle.With(playerUnions: unions)), Is.False);
            Assert.That(CampaignProgressionCommandService022.IsFullPartyDefeat130(_defeat.Battle.With(outcome: BattleOutcome.Retreat, playerUnions: unions)), Is.False);
            var withdrawnDowned = _defeat.Battle.PlayerUnions.Select(x => x.With(retreated: true)).ToArray();
            Assert.That(CampaignProgressionCommandService022.IsFullPartyDefeat130(_defeat.Battle.With(playerUnions: withdrawnDowned)), Is.False);
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator FailedWriteAndCancellationKeepExactDefeatAndRetryCommits130()
        {
            var owner = Owner(_defeat, "failure.json", out var path);
            var original = State(owner); var bytes = File.ReadAllBytes(path);
            var canceled = owner.RestartTowerFromFloorOneAsync130(); owner.CancelTowerRestartBeforeSave130();
            yield return Wait(canceled); Assert.That(canceled.Result.Succeeded, Is.False);
            Assert.That(State(owner), Is.SameAs(original)); CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Directory.CreateDirectory(path + ".tmp");
            LogAssert.Expect(LogType.Error, new Regex("SAVE_WRITE_FAILED109"));
            var failed = owner.RestartTowerFromFloorOneAsync130(); yield return Wait(failed);
            Assert.That(failed.Result.Succeeded, Is.False);
            Assert.That(State(owner), Is.SameAs(original)); CollectionAssert.AreEqual(bytes, File.ReadAllBytes(path));
            Directory.Delete(path + ".tmp");
            var retry = owner.RestartTowerFromFloorOneAsync130();
            yield return Wait(retry); Command(retry.Result);
            Assert.That(Tower(Read(path).CampaignState).ActiveActualFloor, Is.EqualTo(1));
            Assert.That(Read(path + ".bak").CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(original)));
        }

        [Test]
        public void RetainedProofRejectsRemovalForeignAdmissionAndAlteredDeath130()
        {
            var claimed = Require(new M2BattleCommandService().ClaimBattleRewards(_defeat));
            var candidate = Require(new CampaignProgressionCommandService022().RestartTowerAfterPartyDefeat130(claimed, CampaignRegistry022.LoadFromResources()));
            Assert.That(Tower(candidate).ActiveActualFloor, Is.EqualTo(1));
            foreach (var change in new Action<JObject>[] {
                proof => proof.Remove("TowerRestart130"),
                proof => proof["TowerRestart130"]["Encounter"]["BattleId"] = "FOREIGN_NEGATIVE_FIXTURE_130",
                proof => proof["TowerRestart130"]["DefeatBattle"]["PlayerUnions"][0]["Members"][0]["CurrentHp"] = 1 })
            {
                var json = JObject.FromObject(candidate);
                var entries = (JArray)json["Guild"]["GuildCity"]["Strategic017H"]["Campaign019"]["Playable020"]["Progression022"]["AbyssAuthorityEntries"];
                change((JObject)entries.Last);
                var forged = json.ToObject<CampaignState>();
                Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(forged, CampaignRegistry022.LoadFromResources()).IsSuccess, Is.False);
            }
        }

        CampaignState Play(CampaignState source, string command)
        {
            var service = new M2BattleCommandService(); var state = source;
            for (var rounds = 0; state.Battle.Outcome == BattleOutcome.InProgress; rounds++)
            {
                Assert.That(rounds, Is.LessThan(240), "Real combat fixture did not resolve within its bounded round limit: " + command);
                foreach (var union in state.Battle.PlayerUnions.Where(x => !x.IsDefeated && !x.Retreated).ToArray())
                {
                    var candidates = state.Battle.CommittedForecasts.Where(x => x.UnionId == union.UnionId && x.SharedApCost <= union.CurrentAp);
                    var forecast = command == null
                        ? candidates.Where(x => x.CommandId == "CMD_ALL_OUT" || x.CommandId == "CMD_BALANCED")
                            .OrderBy(x => x.MemberActions.Sum(action => action.PredictedHpDelta)).ThenBy(x => x.ForecastId, StringComparer.Ordinal).FirstOrDefault()
                        : candidates.Where(x => x.CommandId == command).OrderBy(x => x.ForecastId, StringComparer.Ordinal).FirstOrDefault();
                    Assert.That(forecast, Is.Not.Null, "Required actual committed command unavailable: " + command + " at round " + state.Battle.Round);
                    state = Require(service.SelectForecast(state, union.UnionId, forecast.ForecastId));
                }
                state = Require(service.ConfirmRound(state, _combat));
            }
            return state;
        }
        M1RuntimeCoordinator Owner(CampaignState state, string name, out string path)
        { path = Path.Combine(_directory, name); Write(path, state); var owner = new M1RuntimeCoordinator(ContentRoot, path); _owners.Add(owner); return owner; }
        static TowerFloorProgress094 Tower(CampaignState value) => Require(CampaignProgressionCommandService022.DescribeTowerFloors094(value, CampaignRegistry022.LoadFromResources()));
        static FieldInfo Field(string name) => typeof(M1RuntimeCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static CampaignState State(M1RuntimeCoordinator owner) => (CampaignState)Field("_campaign").GetValue(owner);
        static T Require<T>(Result<T> result) { Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors)); return result.Value; }
        static void Command(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);
        static void Write(string path, CampaignState state) => new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(state, DateTime.UtcNow));
        static SaveEnvelopeV1 Read(string path) => Require(new AtomicSaveStore().ReadWithRecovery(path));
        static IEnumerator Wait(Task<M1CommandResult> task)
        { var clock = Stopwatch.StartNew(); while (!task.IsCompleted) { Assert.That(clock.Elapsed.TotalSeconds, Is.LessThan(300)); yield return null; } Assert.That(task.IsFaulted || task.IsCanceled, Is.False, task.Exception?.ToString()); }
        static string Hash(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        static string NewDirectory() { var path = Path.Combine(Path.GetTempPath(), Prefix + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path; }
        static void RemoveDirectory(string path)
        { if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return; var full = Path.GetFullPath(path); Assert.That(full.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase) && Path.GetFileName(full).StartsWith(Prefix), Is.True); Directory.Delete(full, true); }
    }

    public sealed class TowerRestartLegacy130Tests
    {
        const string LegacySource = @"C:\Users\simon\Documents\ChatGPT\second dimension\SaveBackups\20260912_083507_reset_session_baseline\Profile\second_dimension_first_hour_slice_071.json";
        const string LegacyHash = "1668BC89991E6A41D2BCEFD66084753715E7C5311B22A4BA534E680AF86F3A75";
        static CampaignProgressionState022 Progress(CampaignState value) => value.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static string Hash(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        [Test]
        public void OriginalAffectedEnvelopeStillLoadsWithExactlyItsCanonicalHash130()
        {
            Assert.That(File.Exists(LegacySource), Is.True, "The pinned original61 source is required; do not replace it with a later run.");
            Assert.That(Hash(LegacySource), Is.EqualTo(LegacyHash));
            var path = Path.Combine(Path.GetTempPath(), "SecondDimensionTowerRestart130Legacy_" + Guid.NewGuid().ToString("N") + ".json"); File.Copy(LegacySource, path);
            var read = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(read.IsSuccess, Is.True, string.Join("; ", read.Errors));
            var original = read.Value;
            Assert.That(CanonicalJson.Sha256Hex(original.CampaignState), Is.EqualTo(original.CanonicalStateHash));
            Assert.That(Progress(original.CampaignState).AbyssAuthorityEntries.All(x => x.TowerRestart130 == null), Is.True);
            Assert.That(JsonConvert.SerializeObject(original.CampaignState), Does.Not.Contain("TowerRestart130"));
            var floors = CampaignProgressionCommandService022.DescribeTowerFloors094(original.CampaignState, CampaignRegistry022.LoadFromResources());
            Assert.That(floors.IsSuccess, Is.True, string.Join("; ", floors.Errors));
            Assert.That(floors.Value.HighestActualFloor, Is.EqualTo(300));
            Assert.That(Hash(path), Is.EqualTo(LegacyHash)); Assert.That(Hash(LegacySource), Is.EqualTo(LegacyHash));
            File.Delete(path);
        }

    }
}
#endif
