using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;

namespace SecondDimension.Presentation.Release030
{
    [Serializable]
    public sealed class TowerFloorEvidence097
    {
        public int floor, template, rounds, enemyUnions, enemyMembers;
        public long treasuryBefore, treasuryAfter, hpBefore, hpAfter, saveBytes;
        public string operationId, battleId, outcome, rewardId, status, canonicalHash;
        public string[] enemyNames, enemyArtBases, enemyArtVariants, threatModifiers;
        public int authorityEntries, newHeroResultRecords;
        public string heroRewardSummary;
        public DeepCampaignBattleEvidence097 combat = new DeepCampaignBattleEvidence097();
    }

    [Serializable]
    public sealed class TowerLoopReport097
    {
        public string status = "RUNNING", failure, sourceSave, sourceSha256, isolatedSave,
            startedUtc, completedUtc, lastAction, finalCanonicalHash;
        public string proofScope = "Actual earned-save copy; shipping coordinator, Auto/Forecast, combat, rewards and Tower transitions. No injected XP, HP, Arts, victory, clears or receipts. This is authority execution, not rendered visual QA.";
        public bool sourceUnchanged, defeatPathExercised, defeatRetryEntered, defeatProbe;
        public int startFloor, targetFloor, highestCleared, reloadChecks, replayChecks, prebattleRetries;
        public double seconds;
        public List<TowerFloorEvidence097> floors = new List<TowerFloorEvidence097>();
    }

    public sealed class TowerLoopVerification097
    {
        readonly AtomicSaveStore _store = new AtomicSaveStore();
        M1RuntimeCoordinator _coordinator;
        TowerLoopReport097 _report;
        string _contentRoot, _evidence;
        Action<string> _log;
        Stopwatch _clock;
        int _secondsLimit;

        public TowerLoopReport097 Run(string contentRoot, string sourceSave, string evidence,
            int targetFloor = 12, int secondsLimit = 1800, bool defeatProbe = false, Action<string> log = null)
        {
            if (targetFloor < 1 || targetFloor > 550) throw new ArgumentOutOfRangeException(nameof(targetFloor));
            if (secondsLimit < 30 || secondsLimit > 10800) throw new ArgumentOutOfRangeException(nameof(secondsLimit));
            var isolated = DeepCampaignVerification093.IsolatedSavePath093(sourceSave, evidence);
            if (!File.Exists(sourceSave)) throw new FileNotFoundException("Earned Tower source required.", sourceSave);
            if (Directory.Exists(evidence) && Directory.EnumerateFileSystemEntries(evidence).Any())
                throw new IOException("Use a fresh evidence directory; old proof is never overwritten.");
            _contentRoot = contentRoot; _evidence = Path.GetFullPath(evidence);
            _secondsLimit = secondsLimit; _log = log ?? (_ => { }); _clock = Stopwatch.StartNew();
            _report = new TowerLoopReport097 { sourceSave = Path.GetFullPath(sourceSave),
                sourceSha256 = HashFile(sourceSave), isolatedSave = isolated,
                targetFloor = targetFloor, startedUtc = DateTime.UtcNow.ToString("O"), defeatProbe = defeatProbe };
            Directory.CreateDirectory(_evidence);
            File.Copy(sourceSave, isolated, false);
            try
            {
                Reload("initial earned source");
                var initial = Read().CampaignState;
                Require(Progression(initial).ActiveAbyssOperation == null,
                    "Finish or leave an existing Tower run before beginning this sequential-run assessment.");
                Require(string.IsNullOrEmpty(_coordinator.CampaignProgression022.TowerAuthorityError094),
                    _coordinator.CampaignProgression022.TowerAuthorityError094);
                _report.startFloor = _coordinator.CampaignProgression022.HighestClearedTowerFloor;
                _report.highestCleared = _report.startFloor;
                Require(targetFloor > _report.startFloor, "The target must exceed the saved actual floor; zero battles are not a pass.");
                if (defeatProbe) PrepareDefeatProbe();
                while (_report.highestCleared < targetFloor)
                {
                    CheckBudget();
                    var expected = _report.highestCleared + 1;
                    Command("begin actual floor " + expected, () => _coordinator.BeginTowerRun081());
                    Require(_coordinator.CampaignProgression022.TowerFloorNumber == expected, "Actual floor was skipped.");
                    if (!defeatProbe && (expected == 1 || expected == 11))
                    {
                        var first = Progression(Read().CampaignState).ActiveAbyssOperation.OperationInstanceId;
                        Command("leave before combat", () => _coordinator.RetreatTowerRun081());
                        Require(_coordinator.CampaignProgression022.TowerFloorNumber == expected, "Leaving skipped a floor.");
                        Reload("prebattle retreat saved");
                        Command("retry same actual floor", () => _coordinator.BeginTowerRun081());
                        Require(_coordinator.CampaignProgression022.TowerFloorNumber == expected, "Retry rerolled the floor number.");
                        Require(Progression(Read().CampaignState).ActiveAbyssOperation.OperationInstanceId != first,
                            "Retry did not issue a distinct attempt identity.");
                        _report.prebattleRetries++;
                    }
                    PrepareBattle();
                    Reload("prepared actual floor " + expected);
                    var source = Read().CampaignState;
                    var row = new TowerFloorEvidence097 { floor = expected,
                        template = _coordinator.CampaignProgression022.TowerContentTemplateFloor094,
                        operationId = Progression(source).ActiveAbyssOperation.OperationInstanceId,
                        treasuryBefore = source.Guild.TreasuryXp,
                        hpBefore = source.Guild.Recruits.Sum(value => (long)value.CurrentHp), status = "RUNNING" };
                    var priorHeroResults = HeroResults(source);
                    _report.floors.Add(row);
                    Require(row.template == CampaignProgressionCommandService022.TowerContentTemplate094(expected),
                        "Wrong retained content template.");
                    Command("enter certified Tower battle", () => _coordinator.EnterAbyssBattle022());
                    var battle = Read().CampaignState.Battle;
                    Require(battle?.Outcome == BattleOutcome.InProgress, "No real in-progress battle was launched.");
                    row.battleId = battle.BattleId;
                    Require(_report.floors.Count(value => value.battleId == row.battleId) == 1,
                        "A new Tower floor reused a previous battle identity.");
                    row.combat.battleId = battle.BattleId;
                    row.combat.label = "Actual Tower floor " + expected;
                    DeepCampaignVerification093.CaptureBattle097(row.combat, battle);
                    row.enemyUnions = battle.EnemyUnions.Count;
                    var enemies = battle.EnemyUnions.SelectMany(value => value.Members).ToArray();
                    row.enemyMembers = enemies.Length;
                    row.enemyNames = enemies.Select(value => value.DisplayName).Distinct().ToArray();
                    row.enemyArtBases = enemies.Select(value => value.EnemyArtBaseId090).Distinct().ToArray();
                    row.enemyArtVariants = enemies.Select(value => value.EnemyArtVariantId090).Distinct().ToArray();
                    row.threatModifiers = Read().CampaignState.Guild.GuildCity.PendingEncounter.RouteModifiers.ToArray();
                    RejectedUnchanged("advance cannot skip live battle", () => _coordinator.AdvanceTowerRun081());
                    RejectedUnchanged("leave cannot erase live battle", () => _coordinator.RetreatTowerRun081());
                    RejectedUnchanged("begin cannot replace live battle", () => _coordinator.BeginTowerRun081());
                    Reload("live battle before real commands");
                    for (var round = 0; round < 128 && Read().CampaignState.Battle.Outcome == BattleOutcome.InProgress; round++)
                    {
                        CheckBudget();
                        Require(Read().CampaignState.Battle.BattleId == row.battleId, "Battle identity changed.");
                        if (defeatProbe) SelectGuardPlan();
                        else Require(M2BattleAutoOrders091.SelectCompletePlan(_coordinator, out var error),
                            "Shipping Auto failed: " + error);
                        Command("resolve actual round " + expected + "/" + (round + 1),
                            () => _coordinator.ConfirmBattleRound());
                        row.rounds++;
                        DeepCampaignVerification093.CaptureBattle097(row.combat, Read().CampaignState.Battle);
                        if (round == 0) Reload("after first actual round");
                        WriteReport();
                    }
                    battle = Read().CampaignState.Battle;
                    Require(battle.Outcome != BattleOutcome.InProgress, "Battle exceeded 128 real rounds; no forced outcome.");
                    row.outcome = battle.Outcome.ToString();
                    row.rewardId = battle.Reward?.RewardId;
                    Reload("actual terminal battle before reward");
                    if (battle.Outcome != BattleOutcome.Victory)
                    {
                        VerifyDefeatRetry(expected, row);
                        _report.status = defeatProbe ? "PASS_DEFEAT_RETRY_ONLY" : "STOPPED_BY_REAL_DEFEAT";
                        break;
                    }
                    Require(!defeatProbe, "Guard-only solo probe won; defeat recovery remains untested.");
                    Command("claim actual victory", () => _coordinator.ClaimBattleRewards());
                    Require(Read().CampaignState.Guild.Development.ClaimedBattleRewardIds.Count(
                        value => value == row.rewardId) == 1, "Victory receipt not applied exactly once.");
                    ReplayUnchanged("repeat victory reward", () => _coordinator.ClaimBattleRewards());
                    for (var transition = 0; transition < 32 &&
                         Progression(Read().CampaignState).ActiveAbyssOperation != null; transition++)
                    {
                        CheckBudget();
                        var active = Progression(Read().CampaignState).ActiveAbyssOperation;
                        if (active.Status == AbyssOperationStatus022.ReadyToFinalize && active.PendingReceipt != null)
                        {
                            var committedReceipt = active.PendingReceipt.ReceiptId;
                            Reload("committed completion receipt before exact-once application");
                            Require(Progression(Read().CampaignState).ActiveAbyssOperation.PendingReceipt?.ReceiptId == committedReceipt,
                                "Reload replaced the already-committed completion receipt.");
                        }
                        Command("apply real Tower return/reward", () => _coordinator.AdvanceTowerRun081());
                    }
                    Require(Progression(Read().CampaignState).ActiveAbyssOperation == null, "Floor finalizer stalled.");
                    var completed = Read().CampaignState;
                    Require(_coordinator.CampaignProgression022.HighestClearedTowerFloor == expected,
                        "Real victory did not advance exactly one floor.");
                    RejectedUnchanged("repeat completed floor finalizer", () => _coordinator.AdvanceTowerRun081());
                    Reload("completed actual floor " + expected);
                    row.status = "PASS_REAL_COMBAT";
                    row.treasuryAfter = completed.Guild.TreasuryXp;
                    row.hpAfter = completed.Guild.Recruits.Sum(value => (long)value.CurrentHp);
                    row.canonicalHash = Read().CanonicalStateHash;
                    row.authorityEntries = completed.Guild.Development.AppliedAdventureAuthorityIds.Count;
                    row.saveBytes = new FileInfo(_report.isolatedSave).Length;
                    var heroResults = HeroResults(completed);
                    Require(priorHeroResults.All(value => heroResults.Contains(value, StringComparer.Ordinal)),
                        "A later floor replaced or removed a saved hero-result outcome.");
                    row.newHeroResultRecords = heroResults.Except(priorHeroResults, StringComparer.Ordinal).Count();
                    Require(row.newHeroResultRecords == (TowerHeroRewardRules094.IsRewardFloor(expected) ? 1 : 0),
                        "Reward floor did not persist exactly one outcome (a saved chance miss also counts as one outcome).");
                    if (TowerHeroRewardRules094.IsRewardFloor(expected))
                    {
                        Require(_coordinator.CampaignProgression022.TowerLastHeroRewardFloor094 == expected,
                            "Live reward recap did not validate the current actual floor.");
                        if (TowerHeroRewardRules094.IsGuaranteedFloor(expected))
                            Require(_coordinator.CampaignProgression022.TowerLastHeroRewardWon094,
                                "Guaranteed floor did not award its authored hero acquisition/growth result.");
                        row.heroRewardSummary = _coordinator.CampaignProgression022.TowerLastHeroRewardSummary094;
                    }
                    _report.highestCleared = expected;
                    if (expected == 1 || expected == 10 || expected == 11 || expected == 12 ||
                        expected % 50 == 0 || expected == targetFloor)
                        File.Copy(_report.isolatedSave, Path.Combine(_evidence, "floor_" + expected.ToString("D4") + "_earned.json"), false);
                    WriteReport();
                }
                if (_report.status == "RUNNING")
                {
                    Require(_report.highestCleared == targetFloor, "Requested horizon was not reached.");
                    _report.status = "PASS_REAL_COMBAT_TO_TARGET";
                }
            }
            catch (Exception e) { _report.status = "BLOCKED"; _report.failure = e.ToString(); _log("TOWER097 " + e.Message); }
            finally
            {
                _report.sourceUnchanged = HashFile(_report.sourceSave) == _report.sourceSha256;
                if (!_report.sourceUnchanged) { _report.status = "BLOCKED"; _report.failure += "\nOriginal source changed."; }
                _report.completedUtc = DateTime.UtcNow.ToString("O"); _report.seconds = _clock.Elapsed.TotalSeconds;
                var final = _store.ReadWithRecovery(_report.isolatedSave);
                if (final.IsSuccess) _report.finalCanonicalHash = final.Value.CanonicalStateHash;
                WriteReport();
            }
            return _report;
        }

        void PrepareBattle()
        {
            for (var i = 0; i < 32; i++)
            {
                CheckBudget();
                var active = Progression(Read().CampaignState).ActiveAbyssOperation;
                Require(active != null, "A floor auto-cleared without its required battle.");
                if (active.Status == AbyssOperationStatus022.Active &&
                    _coordinator.CampaignProgression022.ActiveStepRequiresBattle) return;
                Command("prepare battle-only Tower step", () => _coordinator.AdvanceTowerRun081());
            }
            throw new InvalidOperationException("Tower preparation exceeded 32 transitions.");
        }

        void PrepareDefeatProbe()
        {
            var saved = Read().CampaignState;
            var hero = saved.Guild.Recruits.Where(value => value.AuthorityKind == RecruitAuthorityKind.Normal && value.CurrentHp > 0)
                .OrderBy(value => value.MaximumHp).ThenBy(value => value.RecruitId, StringComparer.Ordinal).First();
            foreach (var id in saved.Guild.Unions.SelectMany(value => value.MemberRecruitIds).Distinct().ToArray())
                Command("isolated defeat probe: return member to reserve", () => _coordinator.UnassignRecruitFromUnion(id));
            Command("isolated defeat probe: assign one earned member", () => _coordinator.AssignRecruitToUnion(hero.RecruitId, 0, 0));
            var now = Read().CampaignState;
            Require(CanonicalJson.Serialize(now.Guild.Recruits) == CanonicalJson.Serialize(saved.Guild.Recruits),
                "Changing deployment modified hero health/stats/gear.");
            Require(now.Guild.TreasuryXp == saved.Guild.TreasuryXp, "Deployment spent XP.");
            _report.proofScope += " Defeat-only probe: legal deployment of one already-owned lowest-HP hero; guard commands only. Original roster/save untouched, no stat nerf.";
        }

        void SelectGuardPlan()
        {
            foreach (var id in _coordinator.State.Battle.PlayerUnions.Where(value => value.CanAct).Select(value => value.UnionId).ToArray())
            {
                var forecast = _coordinator.State.Battle.Forecasts.Where(value => value.UnionId == id && value.CommandId == "CMD_GUARD")
                    .OrderBy(value => value.ForecastId, StringComparer.Ordinal).FirstOrDefault();
                Require(forecast != null, "No legal guard Forecast for defeat probe.");
                Command("choose existing guard Forecast", () => _coordinator.SelectForecast(id, forecast.ForecastId));
            }
        }

        void VerifyDefeatRetry(int expected, TowerFloorEvidence097 row)
        {
            _report.defeatPathExercised = true;
            var previousHighest = _coordinator.CampaignProgression022.HighestClearedTowerFloor;
            Command("retreat after real defeat and preserve rewards", () => _coordinator.RetreatTowerRun081());
            Require(_coordinator.CampaignProgression022.HighestClearedTowerFloor == previousHighest &&
                    _coordinator.CampaignProgression022.TowerFloorNumber == expected, "Defeat advanced the Tower.");
            RejectedUnchanged("repeat defeat retreat", () => _coordinator.RetreatTowerRun081());
            Reload("saved real defeat retreat");
            Command("retry defeated actual floor", () => _coordinator.BeginTowerRun081());
            PrepareBattle();
            Command("enter retry through certified battle", () => _coordinator.EnterAbyssBattle022());
            Require(Read().CampaignState.Battle.Outcome == BattleOutcome.InProgress &&
                    Read().CampaignState.Battle.BattleId != row.battleId, "Retry did not start its own real battle.");
            Require(_coordinator.State.Battle.PlayerUnions.Any(value => value.CanAct &&
                    value.Members.Any(member => !member.Downed && member.CurrentHp > 0)),
                "Retry opened a battle with no living actionable player Union; recovery is not playable.");
            _report.defeatRetryEntered = true; row.status = "REAL_DEFEAT_RETRY_ENTERED";
            Reload("retry battle saved");
            var retried = Read();
            row.treasuryAfter = retried.CampaignState.Guild.TreasuryXp;
            row.hpAfter = retried.CampaignState.Guild.Recruits.Sum(value => (long)value.CurrentHp);
            row.canonicalHash = retried.CanonicalStateHash;
            row.authorityEntries = retried.CampaignState.Guild.Development.AppliedAdventureAuthorityIds.Count;
            row.saveBytes = new FileInfo(_report.isolatedSave).Length;
        }

        void Command(string label, Func<M1CommandResult> command)
        {
            CheckBudget(); _report.lastAction = label; _log("TOWER097 " + label);
            var result = command(); Require(result?.Succeeded == true, label + ": " + result?.Message);
        }
        void ReplayUnchanged(string label, Func<M1CommandResult> command)
        {
            var hash = Read().CanonicalStateHash; command();
            Require(Read().CanonicalStateHash == hash && _coordinator.State.CanonicalStateHash == hash,
                "Replay changed state: " + label); _report.replayChecks++;
        }
        void RejectedUnchanged(string label, Func<M1CommandResult> command)
        {
            var hash = Read().CanonicalStateHash; var result = command();
            Require(result?.Succeeded == false, "Illegal command accepted: " + label);
            Require(Read().CanonicalStateHash == hash && _coordinator.State.CanonicalStateHash == hash,
                "Rejected command changed state: " + label); _report.replayChecks++;
        }
        void Reload(string label)
        {
            CheckBudget(); var hash = Read().CanonicalStateHash;
            _coordinator = new M1RuntimeCoordinator(_contentRoot, _report.isolatedSave);
            Require(Read().CanonicalStateHash == hash && _coordinator.State.CanonicalStateHash == hash,
                "Reload changed earned state at " + label);
            _report.reloadChecks++;
        }
        SaveEnvelopeV1 Read()
        {
            var result = _store.ReadWithRecovery(_report.isolatedSave);
            Require(result.IsSuccess, string.Join("; ", result.Errors));
            Require(CanonicalJson.Sha256Hex(result.Value.CampaignState) == result.Value.CanonicalStateHash, "Save hash invalid.");
            return result.Value;
        }
        void CheckBudget() { if (_clock.Elapsed.TotalSeconds > _secondsLimit) throw new TimeoutException("Bounded real Tower run reached its time budget."); }
        void WriteReport() => File.WriteAllText(Path.Combine(_evidence, "tower_loop097_report.json"),
            JsonConvert.SerializeObject(_report, Formatting.Indented));
        static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static string[] HeroResults(CampaignState campaign) => campaign.Guild.Development.AppliedAdventureAuthorityIds
            .Where(value => value.StartsWith(GuildCityRecruitmentService017D.TowerHeroResultPrefix094, StringComparison.Ordinal)).ToArray();
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static string HashFile(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
    }
}
