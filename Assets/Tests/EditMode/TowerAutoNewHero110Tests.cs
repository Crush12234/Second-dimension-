#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    // Synthetic combat outcomes, real Tower begin/return/completion authorities,
    // real next-battle initialization and AtomicSaveStore. This is deliberately
    // separate from affected-save/UI playthrough and unattended soak evidence.
    public sealed class TowerAutoNewHero110Tests
    {
        CampaignState _terminalTenth;
        string _selectedHeroId;
        string _fixtureDirectory;
        string _testDirectory;
        string _savePath;
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");

        [OneTimeSetUp]
        public void PrepareSyntheticWinningTenthFloorThroughTowerAuthorities110()
        {
            _fixtureDirectory = Path.Combine(Path.GetTempPath(), "SecondDimensionTowerAuto110Fixture_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_fixtureDirectory);
            var fixture = new TowerHeroRewards094Tests();
            fixture.SetUp();
            var registry = CampaignRegistry022.LoadFromResources();
            var heroes = HeroMaster300Catalog087.FromJson(Resources.Load<TextAsset>(
                "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300").text);
            // The old one-member Tower authority fixture deliberately omits the
            // committed applicant board. A real coordinator reload would then
            // resume the unfinished charter and rightly reject a new battle.
            // Complete the actual opening commands before exercising disk reload.
            var opening = new M1OpeningFlowTests();
            opening.SetUp();
            var fresh = (CampaignState)typeof(M1OpeningFlowTests)
                .GetMethod("BuildCompletedOpening", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(opening, null);
            Assert.That(fresh.OpeningFlow.Stage, Is.EqualTo(OpeningStage.Complete));
            Assert.That(fresh.OpeningFlow.ApplicantBoard, Is.Not.Null);
            Assert.That(fresh.OpeningFlow.UnionBuilderCompleted, Is.True);
            CampaignState campaign = null;
            // Choose a test seed before beginning any run. The production 1% roll
            // and pool stay intact; no active floor, receipt, or reward is edited.
            for (var seed = 1; seed <= 4096; seed++)
            {
                var candidate = new CampaignState(fresh.CampaignGuid, seed,
                    fresh.ContentAuthorityVersion, fresh.Rules, fresh.Guild,
                    fresh.Profile, fresh.OpeningFlow, null, fresh.SssV4090);
                var plan = TowerHeroRewardRules094.BuildPlan(candidate, "SYNTHETIC_SEED_PROBE_110", 10, heroes);
                if (!plan.WinsHero || candidate.Guild.Recruits.Any(
                    recruit => recruit.AuthoredStableRecruitId == plan.SelectedHeroId)) continue;
                campaign = candidate;
                _selectedHeroId = plan.SelectedHeroId;
                break;
            }
            Assert.That(campaign, Is.Not.Null, "The bounded synthetic seed search must find the normal 1% reward branch.");
            // Settle the shipping opening migration before any synthetic legacy
            // battle exists: reloading that old helper's TOWER_TEST battle rules
            // intentionally starts the updated tutorial through the live loader.
            var preparedPath = Path.Combine(_fixtureDirectory, "prepared_before_milestone.json");
            new AtomicSaveStore().Write(preparedPath, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
            var coordinator = new M1RuntimeCoordinator(ContentRoot, preparedPath);
            campaign = Campaign(coordinator);
            Assert.That(campaign.OpeningFlow.Stage, Is.EqualTo(OpeningStage.Complete));
            Assert.That(campaign.OpeningFlow.ApplicantBoard, Is.Not.Null);
            Assert.That(campaign.OpeningFlow.UnionBuilderCompleted, Is.True);
            Assert.That(campaign.Guild.Recruits.Any(recruit => recruit.AuthoredStableRecruitId == _selectedHeroId), Is.False);

            for (var floor = 1; floor < 10; floor++)
                campaign = (CampaignState)typeof(TowerHeroRewards094Tests)
                    .GetMethod("CompleteNewFloor", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(fixture, new object[] { campaign, floor });
            // The normal resume migration adds the patrol after earned operation
            // progress. Use that same service now at the idle boundary, before
            // committing floor10's roster, without restarting a synthetic battle.
            var firstHour = (FirstHourRosterService071)Field("_firstHourRoster071").GetValue(coordinator);
            campaign = Require(firstHour.EnsureLanternPatrol(campaign));
            Assert.That(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign), Is.False,
                "The certified milestone must begin only after all prior floor authorities are complete.");

            var service = new CampaignProgressionCommandService022();
            campaign = Require(service.BeginTowerFloor094(campaign, registry));
            var operation = registry.AbyssOperations[Progression(campaign).ActiveAbyssOperation.OperationDefinitionId];
            for (var guard = 0; !operation.steps[Progression(campaign).ActiveAbyssOperation.CurrentStepIndex].requiresBattle; guard++)
            {
                Assert.That(guard, Is.LessThan(16));
                campaign = Require(service.CommitAbyssStep(campaign, registry, "SUCCESS"));
                campaign = Require(service.ApplyAbyssStep(campaign, registry));
            }
            campaign = Require(service.CommitAbyssBattleEncounter(campaign, registry));
            var combat = (M2CombatContent)Field("_combatContent").GetValue(coordinator);
            var roster = (EncounterRosterResolver070)Field("_encounterRosterResolver070").GetValue(coordinator);
            campaign = Require(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(
                campaign, new M2BattleCommandService(), combat, roster));

            // Only combat resolution is synthetic. Keep the actually initialized
            // roster, equipment snapshots, encounter and Tower identity, and let
            // the normal reward calculator create the pending award.
            var defeated = campaign.Battle.EnemyUnions.Select(union => union.With(
                members: union.Members.Select(member => member.With(currentHp: 0)).ToArray())).ToArray();
            var terminal = campaign.Battle.With(phase: BattlePhase.Resolved,
                outcome: BattleOutcome.Victory, enemyUnions: defeated);
            terminal = terminal.WithReward(M2ProgressionRewards.CreatePending(campaign, terminal, combat));
            terminal = terminal.With(finalStateHash: M2BattleCommandService.GameplayRngStateHash090(terminal),
                finalIntegrityStateHash090: M2BattleCommandService.AuthoritativeStateHash(terminal));
            _terminalTenth = campaign.WithBattle(terminal);
            Assert.That(M2BattleCommandService.HasValidFinalStateHash090(terminal), Is.True);
            Assert.That(terminal.Reward.Claimed, Is.False);
            Assert.That(Progression(_terminalTenth).ActiveAbyssOperation.OperationInstanceId,
                Does.StartWith("TOWERRUN094_000010_"));
            Assert.That(_terminalTenth.Guild.Recruits.Any(recruit => recruit.AuthoredStableRecruitId == _selectedHeroId), Is.False);
            TestContext.Progress.WriteLine("Synthetic Tower Auto fixture: normal floor10 win, seed " +
                campaign.CampaignSeed + ", expected catalog hero " + _selectedHeroId + ".");
        }

        [SetUp]
        public void CreateIsolatedMilestoneSave110()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "SecondDimensionTowerAuto110_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testDirectory);
            _savePath = Path.Combine(_testDirectory, "synthetic_milestone.json");
            new AtomicSaveStore().Write(_savePath, SaveEnvelopeV1.Create(_terminalTenth, DateTime.UtcNow));
        }

        [TearDown]
        public void RemoveOnlyIsolatedTestFiles110() => RemoveTestDirectory(_testDirectory);

        [OneTimeTearDown]
        public void RemoveSyntheticFixtureDirectory110() => RemoveTestDirectory(_fixtureDirectory);

        [Test, Timeout(180000)]
        public void WinningMilestoneAutoOutfitsNewHeroBeforeNextBattleAndReloadsExactlyOnce110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            AssertSuccessfulTransition(coordinator);
        }

        [Test, Timeout(180000)]
        public void OriginalMilestoneOrderReachesRealOutfittingGuardWithoutSaving110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            var before = Campaign(coordinator);
            var beforeHash = CanonicalJson.Sha256Hex(before);
            var beforeBytes = File.ReadAllBytes(_savePath);
            var backupBefore = File.Exists(_savePath + ".bak") ? File.ReadAllBytes(_savePath + ".bak") : null;
            var changed = 0;
            coordinator.Changed += () => changed++;
            var registry = CampaignRegistry022.LoadFromResources();
            var service = (CampaignProgressionCommandService022)Field("_campaignCommands022").GetValue(coordinator);
            var boundary = typeof(M1RuntimeCoordinator).GetMethod("AdvanceTowerToBattleBoundary108",
                BindingFlags.Instance | BindingFlags.NonPublic);

            // Replay the original ordering using the real candidate authorities:
            // claim floor10, grant its NEW catalog hero, then begin/start floor11
            // before the ordinary pre-save new-recruit equipment hook can run.
            // Production AdvanceTowerAutoAfterVictory108 remains repaired.
            var claimArguments = new object[] { before, null, false };
            var claim = (M1CommandResult)typeof(M1RuntimeCoordinator)
                .GetMethod("TryBuildClaimedBattleRewards108", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(coordinator, claimArguments);
            Assert.That(claim.Succeeded, Is.True, claim.Message);
            var candidate = Require((Result<CampaignState>)boundary.Invoke(coordinator,
                new object[] { (CampaignState)claimArguments[1], registry }));
            Assert.That(Progression(candidate).ActiveAbyssOperation, Is.Null);
            var newHero = candidate.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == _selectedHeroId);
            Assert.That(before.Guild.Recruits.Any(value => value.RecruitId == newHero.RecruitId), Is.False);
            Assert.That(newHero.Equipment.Assignments, Is.Empty);
            candidate = Require(service.BeginTowerFloor094(candidate, registry));
            candidate = Require((Result<CampaignState>)boundary.Invoke(coordinator, new object[] { candidate, registry }));
            candidate = Require(service.CommitAbyssBattleEncounter(candidate, registry));
            candidate = Require(((GuildCityBattleBridgeService017D)Field("_guildCityBattleBridge").GetValue(coordinator))
                .StartCertifiedEncounter(candidate,
                    (M2BattleCommandService)Field("_battleCommands").GetValue(coordinator),
                    (M2CombatContent)Field("_combatContent").GetValue(coordinator),
                    (EncounterRosterResolver070)Field("_encounterRosterResolver070").GetValue(coordinator)));
            Assert.That(candidate.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(candidate.Battle.BattleId, Is.Not.EqualTo(before.Battle.BattleId));
            Assert.That(Progression(candidate).ActiveAbyssOperation.OperationInstanceId,
                Does.StartWith("TOWERRUN094_000011_"));
            Assert.That(candidate.Guild.Recruits.Single(value => value.RecruitId == newHero.RecruitId)
                .Equipment.Assignments, Is.Empty);

            string guardLog = null;
            Application.LogCallback capture = (message, stack, type) =>
            {
                if (message.StartsWith("PRE_SAVE_STATE_PREPARATION_FAILED109", StringComparison.Ordinal))
                    guardLog = message + "\n" + stack;
            };
            Application.logMessageReceived += capture;
            try
            {
                // Unity LogMatch serializes Regex.ToString() and recreates it,
                // so multiline matching must be encoded in the pattern itself.
                LogAssert.Expect(LogType.Error, new Regex(
                    @"(?s)^PRE_SAVE_STATE_PREPARATION_FAILED109\r?\nSystem\.InvalidOperationException: Finish the battle before outfitting new recruits\.\r?\n\s+at SecondDimension\.Gameplay\.Recruitment\.RecruitStarterEquipment094\.ApplyToNewRecruits\b.*?\r?\n\s+at SecondDimension\.Presentation\.M1RuntimeCoordinator\.ApplyAndPersist\b"));
                var saved = (M1CommandResult)typeof(M1RuntimeCoordinator)
                    .GetMethod("ApplyAndPersist", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(coordinator, new object[] { Result<CampaignState>.Success(candidate), true, "Old-order diagnostic only." });
                Assert.That(saved.Succeeded, Is.False);
                Assert.That(saved.Message, Does.Contain("Finish the battle before outfitting new recruits."));
                Assert.That(guardLog, Does.Contain("RecruitStarterEquipment094.ApplyToNewRecruits"));
                Assert.That(guardLog, Does.Contain("M1RuntimeCoordinator.ApplyAndPersist"));
                TestContext.Progress.WriteLine("EXPECTED_ORIGINAL_MILESTONE_ORDER_GUARD110\n" + guardLog);
            }
            finally { Application.logMessageReceived -= capture; }

            Assert.That(changed, Is.Zero, "The failed original ordering must publish no committed state.");
            Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(beforeHash));
            Assert.That(Campaign(coordinator).Battle.Reward.Claimed, Is.False);
            Assert.That(Campaign(coordinator).Guild.Recruits.Any(value => value.RecruitId == newHero.RecruitId), Is.False);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(beforeBytes));
            Assert.That(File.Exists(_savePath + ".bak"), Is.EqualTo(backupBefore != null));
            if (backupBefore != null) Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(backupBefore));
            Assert.That(File.Exists(_savePath + ".tmp"), Is.False);
        }

        [Test, Timeout(180000)]
        public void RealDiskWriteFailureRetainsMilestoneVictoryAndRetryGrantsSameHeroOnce110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            var before = Campaign(coordinator);
            var beforeHash = CanonicalJson.Sha256Hex(before);
            var beforeBytes = File.ReadAllBytes(_savePath);
            // A directory where the actual store opens its temporary output
            // causes a real FileStream failure, independent of mock save APIs.
            Directory.CreateDirectory(_savePath + ".tmp");
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("^SAVE_WRITE_FAILED109", RegexOptions.Singleline));
                var failed = coordinator.AdvanceTowerAutoAfterVictory108();
                Assert.That(failed.Succeeded, Is.False);
                Assert.That(coordinator.ReadTowerSavedReward110(before.Battle.BattleId,before.Battle.BattleId),Is.Null);
                Assert.That(failed.Message, Does.Contain("while saving"),
                    "This must reach the disk-write phase, not fail new-recruit preparation first.");
                Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(beforeHash));
                Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(beforeBytes));
                Assert.That(Campaign(coordinator).Battle.Reward.Claimed, Is.False);
                Assert.That(Campaign(coordinator).Guild.Recruits.Any(recruit => recruit.AuthoredStableRecruitId == _selectedHeroId), Is.False);
            }
            finally
            {
                Directory.Delete(_savePath + ".tmp", false);
            }
            // Reload the last durable terminal outcome before retrying. Its RNG,
            // reward and operation identities must survive the failed attempt.
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(Campaign(reloaded).Battle.Reward.RewardId, Is.EqualTo(before.Battle.Reward.RewardId));
            AssertSuccessfulTransition(reloaded);
        }

        [Test, Timeout(180000)]
        public void ManualMilestoneBankMatchesStepAuthoritiesWithOneWriteAndNoNextBattle110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(coordinator.BankTowerVictory110().Succeeded, Is.False,
                "Manual Bank must retain the separate unclaimed-reward guard.");
            var claim = coordinator.ClaimBattleRewards();
            Assert.That(claim.Succeeded, Is.True, claim.Message);
            var before = Campaign(coordinator);

            // A separate isolated control uses the prior public six-save path.
            // Comparing final canonical state proves that batching preserves all
            // battle, operation, growth and arrival authorities exactly.
            var controlPath = Path.Combine(_testDirectory, "stepwise_control.json");
            new AtomicSaveStore().Write(controlPath, SaveEnvelopeV1.Create(before, DateTime.UtcNow));
            var control = new M1RuntimeCoordinator(ContentRoot, controlPath);
            var controlWrites = 0;
            control.Changed += () => controlWrites++;
            var controlClock = System.Diagnostics.Stopwatch.StartNew();
            while (Progression(Campaign(control)).ActiveAbyssOperation != null)
            {
                Assert.That(controlWrites, Is.LessThan(8));
                var step = control.AdvanceTowerRun081();
                Assert.That(step.Succeeded, Is.True, step.Message);
            }
            controlClock.Stop();
            Assert.That(controlWrites, Is.EqualTo(6));
            var expected = CanonicalJson.Sha256Hex(Campaign(control));
            var bankClock = System.Diagnostics.Stopwatch.StartNew();
            AssertSuccessfulManualBank110(coordinator);
            bankClock.Stop();
            Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(expected));
            TestContext.Progress.WriteLine("MANUAL_BANK110 control_writes=" + controlWrites +
                " bank_writes=1 control_ms=" + controlClock.ElapsedMilliseconds +
                " bank_ms=" + bankClock.ElapsedMilliseconds + "; no universal timing assertion.");
        }

        [Test, Timeout(180000)]
        public void ManualMilestoneBankDiskFailureRetainsClaimedVictoryAndRetriesOnce110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            var claim = coordinator.ClaimBattleRewards();
            Assert.That(claim.Succeeded, Is.True, claim.Message);
            var before = Campaign(coordinator);
            var beforeBytes = File.ReadAllBytes(_savePath);
            var beforeBackup = File.ReadAllBytes(_savePath + ".bak");
            var changed = 0;
            coordinator.Changed += () => changed++;
            Directory.CreateDirectory(_savePath + ".tmp");
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("^SAVE_WRITE_FAILED109"));
                var failed = coordinator.BankTowerVictory110();
                Assert.That(failed.Succeeded, Is.False);
                Assert.That(failed.Message, Does.Contain("while saving"));
                Assert.That(changed, Is.Zero);
                Assert.That(Campaign(coordinator), Is.SameAs(before));
                Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(beforeBytes));
                Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(beforeBackup));
                Assert.That(Campaign(coordinator).Battle.Reward.Claimed, Is.True);
                Assert.That(Campaign(coordinator).Guild.Recruits.Any(
                    recruit => recruit.AuthoredStableRecruitId == _selectedHeroId), Is.False);
            }
            finally { Directory.Delete(_savePath + ".tmp", false); }
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(CanonicalJson.Sha256Hex(before)));
            AssertSuccessfulManualBank110(reloaded);
        }

        void AssertSuccessfulManualBank110(M1RuntimeCoordinator coordinator)
        {
            var before = Campaign(coordinator);
            var beforeBytes = File.ReadAllBytes(_savePath);
            var oldIds = before.Guild.Recruits.Select(recruit => recruit.RecruitId).ToArray();
            var oldEquipment = before.Guild.Recruits.ToDictionary(recruit => recruit.RecruitId,
                recruit => CanonicalJson.Serialize(recruit.Equipment));
            var changed = 0;
            coordinator.Changed += () => changed++;
            var bank = coordinator.BankTowerVictory110();
            Assert.That(bank.Succeeded, Is.True, bank.Message);
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(beforeBytes),
                "Exactly one durable replacement must retain the pre-Bank save as backup.");
            var after = Campaign(coordinator);
            Assert.That(Progression(after).ActiveAbyssOperation, Is.Null);
            Assert.That(after.Guild.GuildCity.PendingEncounter, Is.Null);
            Assert.That(after.Battle.BattleId, Is.EqualTo(before.Battle.BattleId));
            Assert.That(after.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
            Assert.That(after.Battle.Reward.Claimed, Is.True);
            var arrived = after.Guild.Recruits.Where(recruit => !oldIds.Contains(recruit.RecruitId)).ToArray();
            Assert.That(arrived, Has.Length.EqualTo(1));
            Assert.That(arrived[0].AuthoredStableRecruitId, Is.EqualTo(_selectedHeroId));
            Assert.That(arrived[0].Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
            foreach (var recruit in after.Guild.Recruits.Where(recruit => oldIds.Contains(recruit.RecruitId)))
                Assert.That(CanonicalJson.Serialize(recruit.Equipment), Is.EqualTo(oldEquipment[recruit.RecruitId]));
            Assert.That(after.Guild.Development.ClaimedBattleRewardIds.Count(
                id => id == before.Battle.Reward.RewardId), Is.EqualTo(1));
            var outcomes = after.Guild.Development.AppliedAdventureAuthorityIds.Where(id =>
                id.StartsWith(GuildCityRecruitmentService017D.TowerHeroResultPrefix094, StringComparison.Ordinal)).ToArray();
            Assert.That(outcomes, Has.Length.EqualTo(1));
            Assert.That(outcomes[0], Does.Contain("|" + _selectedHeroId + "|"));
            var saved = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
            var expected = CanonicalJson.Sha256Hex(after);
            Assert.That(saved.Value.CanonicalStateHash, Is.EqualTo(expected));
            var bytes = File.ReadAllBytes(_savePath);
            Assert.That(coordinator.BankTowerVictory110().Succeeded, Is.False);
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(bytes));
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(expected));
            Assert.That(reloaded.BankTowerVictory110().Succeeded, Is.False);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(bytes));
        }

        [TestCase("idle"), TestCase("prepared"), TestCase("committed"), Timeout(180000)]
        public void ManualTowerStartMatchesStepwiseAuthoritiesWithOneDurableWrite110(string checkpoint)
        {
            var coordinator = PrepareManualStartCheckpoint110(checkpoint);
            var before = Campaign(coordinator);
            var beforeBytes = File.ReadAllBytes(_savePath);
            var controlPath = Path.Combine(_testDirectory, "manual_start_control.json");
            new AtomicSaveStore().Write(controlPath, SaveEnvelopeV1.Create(before, DateTime.UtcNow));
            var control = new M1RuntimeCoordinator(ContentRoot, controlPath);
            var controlWrites = 0;
            control.Changed += () => controlWrites++;
            if (Progression(Campaign(control)).ActiveAbyssOperation == null)
                AssertCommand110(control.BeginTowerRun081());
            PrepareToBattleStep110(control);
            AssertCommand110(control.EnterAbyssBattle022());
            Assert.That(controlWrites, Is.GreaterThan(1), "The comparison must exercise the old persisted authorities.");

            var changed = 0;
            coordinator.Changed += () => changed++;
            AssertCommand110(coordinator.StartTowerBattle110());
            Assert.That(changed, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(beforeBytes),
                "One durable replacement retains the exact pre-entry save as its backup.");
            var after = Campaign(coordinator);
            Assert.That(CanonicalJson.Sha256Hex(after), Is.EqualTo(CanonicalJson.Sha256Hex(Campaign(control))),
                "Begin, every preparation receipt, encounter and initialized battle must match the old public path.");
            AssertStartedBattleWithoutHiddenRewards110(before, after);
            AssertManualStartRejectedWithoutWrite110(coordinator);
            Assert.That(changed, Is.EqualTo(1));
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(CanonicalJson.Sha256Hex(after)));
            AssertManualStartRejectedWithoutWrite110(reloaded);
            TestContext.Progress.WriteLine("MANUAL_START110 checkpoint=" + checkpoint +
                " old_writes=" + controlWrites + " atomic_writes=1; no universal timing assertion.");
        }

        [Test, Timeout(180000)]
        public void ManualTowerStartDiskFailureRetainsPreparedReceiptAndRetriesAfterReload110()
        {
            var coordinator = PrepareManualStartCheckpoint110("prepared");
            var before = Campaign(coordinator);
            var beforeBytes = File.ReadAllBytes(_savePath);
            var beforeBackup = File.ReadAllBytes(_savePath + ".bak");
            var changed = 0;
            coordinator.Changed += () => changed++;
            Directory.CreateDirectory(_savePath + ".tmp");
            try
            {
                LogAssert.Expect(LogType.Error, new Regex("^SAVE_WRITE_FAILED109"));
                var failed = coordinator.StartTowerBattle110();
                Assert.That(failed.Succeeded, Is.False);
                Assert.That(failed.Message, Does.Contain("while saving"));
                Assert.That(changed, Is.Zero);
                Assert.That(Campaign(coordinator), Is.SameAs(before));
                Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(beforeBytes));
                Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(beforeBackup));
                Assert.That(Progression(Campaign(coordinator)).ActiveAbyssOperation.PendingReceipt, Is.Not.Null);
            }
            finally { Directory.Delete(_savePath + ".tmp", false); }
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(CanonicalJson.Sha256Hex(before)));
            var retryChanges = 0;
            reloaded.Changed += () => retryChanges++;
            AssertCommand110(reloaded.StartTowerBattle110());
            Assert.That(retryChanges, Is.EqualTo(1));
            Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(beforeBytes));
            AssertStartedBattleWithoutHiddenRewards110(before, Campaign(reloaded));
            AssertManualStartRejectedWithoutWrite110(reloaded);
        }

        [Test, Timeout(180000)]
        public void ManualTowerStartCannotClaimOrBankAnyUnfinishedVictoryCheckpoint110()
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            AssertManualStartRejectedWithoutWrite110(coordinator); // terminal, unclaimed
            AssertCommand110(coordinator.ClaimBattleRewards());
            AssertManualStartRejectedWithoutWrite110(coordinator); // returned, still unbanked
            AssertCommand110(coordinator.AdvanceTowerRun081());
            AssertManualStartRejectedWithoutWrite110(coordinator); // committed battle receipt
            AssertCommand110(coordinator.AdvanceTowerRun081());
            Assert.That(Progression(Campaign(coordinator)).ActiveAbyssOperation.Status,
                Is.EqualTo(AbyssOperationStatus022.Active));
            AssertManualStartRejectedWithoutWrite110(coordinator); // Active bookend after battle
            for (var guard = 0; Progression(Campaign(coordinator)).ActiveAbyssOperation.Status !=
                                  AbyssOperationStatus022.ReadyToFinalize; guard++)
            {
                Assert.That(guard, Is.LessThan(8));
                AssertCommand110(coordinator.AdvanceTowerRun081());
            }
            AssertManualStartRejectedWithoutWrite110(coordinator); // floor reward not yet banked
            Assert.That(Campaign(coordinator).Guild.Recruits.Any(
                recruit => recruit.AuthoredStableRecruitId == _selectedHeroId), Is.False);
        }

        M1RuntimeCoordinator PrepareManualStartCheckpoint110(string checkpoint)
        {
            var coordinator = new M1RuntimeCoordinator(ContentRoot, _savePath);
            AssertCommand110(coordinator.ClaimBattleRewards());
            AssertCommand110(coordinator.BankTowerVictory110());
            if (checkpoint == "idle") return coordinator;
            AssertCommand110(coordinator.BeginTowerRun081());
            if (checkpoint == "prepared")
            {
                AssertCommand110(coordinator.AdvanceTowerRun081());
                Assert.That(Progression(Campaign(coordinator)).ActiveAbyssOperation.PendingReceipt, Is.Not.Null);
                return coordinator;
            }
            Assert.That(checkpoint, Is.EqualTo("committed"));
            PrepareToBattleStep110(coordinator);
            // Capture the legitimate old two-write entry boundary. Only the real
            // authority creates this request; no live save, floor or reward edits.
            var committed = Require(new CampaignProgressionCommandService022().CommitAbyssBattleEncounter(
                Campaign(coordinator), CampaignRegistry022.LoadFromResources()));
            new AtomicSaveStore().Write(_savePath, SaveEnvelopeV1.Create(committed, DateTime.UtcNow));
            var resumed = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(Campaign(resumed).Guild.GuildCity.PendingEncounter, Is.Not.Null);
            Assert.That(Campaign(resumed).Battle.Reward.Claimed, Is.True);
            Assert.That(Campaign(resumed).Guild.GuildCity.PendingEncounter.BattleId,
                Is.Not.EqualTo(Campaign(resumed).Battle.BattleId));
            return resumed;
        }

        static void PrepareToBattleStep110(M1RuntimeCoordinator coordinator)
        {
            var registry = CampaignRegistry022.LoadFromResources();
            for (var guard = 0; guard < 16; guard++)
            {
                var active = Progression(Campaign(coordinator)).ActiveAbyssOperation;
                if (active.Status == AbyssOperationStatus022.AwaitingBattle ||
                    registry.AbyssOperations[active.OperationDefinitionId].steps[active.CurrentStepIndex].requiresBattle) return;
                AssertCommand110(coordinator.AdvanceTowerRun081());
            }
            Assert.Fail("The bounded fixture did not reach the real battle step.");
        }

        void AssertManualStartRejectedWithoutWrite110(M1RuntimeCoordinator coordinator)
        {
            var before = Campaign(coordinator);
            var primary = File.ReadAllBytes(_savePath);
            var backup = File.Exists(_savePath + ".bak") ? File.ReadAllBytes(_savePath + ".bak") : null;
            var timestamp = File.GetLastWriteTimeUtc(_savePath);
            var changed = 0;
            Action observer = () => changed++;
            coordinator.Changed += observer;
            try
            {
                Assert.That(coordinator.StartTowerBattle110().Succeeded, Is.False);
                Assert.That(Campaign(coordinator), Is.SameAs(before));
                Assert.That(changed, Is.Zero);
                Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(primary));
                Assert.That(File.GetLastWriteTimeUtc(_savePath), Is.EqualTo(timestamp));
                Assert.That(File.Exists(_savePath + ".bak"), Is.EqualTo(backup != null));
                if (backup != null) Assert.That(File.ReadAllBytes(_savePath + ".bak"), Is.EqualTo(backup));
            }
            finally { coordinator.Changed -= observer; }
        }

        static void AssertStartedBattleWithoutHiddenRewards110(CampaignState before, CampaignState after)
        {
            Assert.That(after.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(after.Battle.Phase, Is.EqualTo(BattlePhase.ForecastSelection));
            Assert.That(after.Battle.BattleId, Is.EqualTo(after.Guild.GuildCity.PendingEncounter.BattleId));
            Assert.That(after.Battle.BattleId, Is.Not.EqualTo(before.Battle.BattleId));
            Assert.That(Progression(after).ActiveAbyssOperation.Status, Is.EqualTo(AbyssOperationStatus022.AwaitingBattle));
            Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
            Assert.That(after.Guild.Development.ClaimedBattleRewardIds,
                Is.EqualTo(before.Guild.Development.ClaimedBattleRewardIds));
            Assert.That(after.Guild.Recruits.Select(recruit => recruit.RecruitId),
                Is.EqualTo(before.Guild.Recruits.Select(recruit => recruit.RecruitId)));
            foreach (var recruit in before.Guild.Recruits)
                Assert.That(CanonicalJson.Serialize(after.Guild.Recruits.Single(
                    other => other.RecruitId == recruit.RecruitId).Equipment),
                    Is.EqualTo(CanonicalJson.Serialize(recruit.Equipment)));
            var floors = CampaignProgressionCommandService022.DescribeTowerFloors094(after, CampaignRegistry022.LoadFromResources());
            Assert.That(floors.IsSuccess, Is.True, string.Join("; ", floors.Errors));
            Assert.That(floors.Value.HighestActualFloor, Is.EqualTo(10));
        }

        static void AssertCommand110(M1CommandResult result) => Assert.That(result.Succeeded, Is.True, result.Message);


        void AssertSuccessfulTransition(M1RuntimeCoordinator coordinator)
        {
            var before = Campaign(coordinator);
            var rewardId = before.Battle.Reward.RewardId;
            var oldEquipment = before.Guild.Recruits.ToDictionary(recruit => recruit.RecruitId,
                recruit => CanonicalJson.Serialize(recruit.Equipment));
            var oldIds = before.Guild.Recruits.Select(recruit => recruit.RecruitId).ToArray();
            var changed = 0;
            coordinator.Changed += () => changed++;
            Assert.That(coordinator.ReadTowerSavedReward110(before.Battle.BattleId,"not saved"),Is.Null);
            var rewardBefore110=coordinator.CampaignProgression022;
            var result = coordinator.AdvanceTowerAutoAfterVictory108();
            Assert.That(result.Succeeded, Is.True, result.Message);
            Assert.That(changed, Is.EqualTo(1), "One committed transition publishes one state update.");
            var receipt110=coordinator.ReadTowerSavedReward110(before.Battle.BattleId,Campaign(coordinator).Battle.BattleId);
            Assert.That(receipt110,Is.Not.Null);
            Assert.That(receipt110.Floor,Is.EqualTo(rewardBefore110.TowerFloorNumber));
            Assert.That(receipt110.GuildXp,Is.EqualTo(rewardBefore110.TowerGuildXpReward));
            Assert.That(receipt110.HallXp,Is.EqualTo(rewardBefore110.TowerHallXpReward));
            Assert.That(receipt110.MaterialIds,Is.EqualTo(rewardBefore110.TowerRewardMaterialIds108));
            Assert.That(receipt110.HeroSummary,Is.EqualTo(coordinator.CampaignProgression022.TowerLastHeroRewardSummary094));
            Assert.That(receipt110.HeroSummary,Does.Contain("HERO"));
            Assert.That(coordinator.ReadTowerSavedReward110("wrong battle",Campaign(coordinator).Battle.BattleId),Is.Null);
            Assert.That(coordinator.ReadTowerSavedReward110(before.Battle.BattleId,"wrong next battle"),Is.Null);
            var after = Campaign(coordinator);
            var arrived = after.Guild.Recruits.Where(recruit => !oldIds.Contains(recruit.RecruitId)).ToArray();
            Assert.That(arrived, Has.Length.EqualTo(1), "The milestone must actually exercise NEW hero acquisition.");
            Assert.That(arrived[0].AuthoredStableRecruitId, Is.EqualTo(_selectedHeroId));
            Assert.That(arrived[0].Equipment.Assignments, Is.Not.Empty,
                "New hero gear must be initialized at the completed battle boundary before floor11 starts.");
            Assert.That(arrived[0].Equipment.Find(EquipmentSlotIds.BodyArmor), Is.Not.Null);
            Assert.That(after.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
            Assert.That(after.Battle.BattleId, Is.Not.EqualTo(before.Battle.BattleId));
            Assert.That(coordinator.CampaignProgression022.TowerFloorNumber, Is.EqualTo(11));
            foreach (var recruit in after.Guild.Recruits.Where(recruit => oldIds.Contains(recruit.RecruitId)))
                Assert.That(CanonicalJson.Serialize(recruit.Equipment), Is.EqualTo(oldEquipment[recruit.RecruitId]),
                    "Reward processing must not auto-equip existing heroes.");
            Assert.That(after.Guild.Development.ClaimedBattleRewardIds.Count(id => id == rewardId), Is.EqualTo(1));
            var outcomes = after.Guild.Development.AppliedAdventureAuthorityIds.Where(id =>
                id.StartsWith(GuildCityRecruitmentService017D.TowerHeroResultPrefix094, StringComparison.Ordinal)).ToArray();
            Assert.That(outcomes, Has.Length.EqualTo(1));
            Assert.That(outcomes[0], Does.Contain("|" + _selectedHeroId + "|"));
            Assert.That(outcomes[0], Does.Not.Contain("|NO_HERO|"));
            var gearIds = after.Guild.Recruits.SelectMany(recruit => recruit.Equipment.Assignments)
                .Select(assignment => assignment.Item.InstanceId).ToArray();
            Assert.That(gearIds.Distinct().Count(), Is.EqualTo(gearIds.Length));
            Assert.That(after.Guild.Inventory.Any(item => gearIds.Contains(item.InstanceId)), Is.False);

            var saved = new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
            Assert.That(saved.Value.CanonicalStateHash, Is.EqualTo(CanonicalJson.Sha256Hex(after)));
            var bytes = File.ReadAllBytes(_savePath);
            Assert.That(coordinator.AdvanceTowerAutoAfterVictory108().Succeeded, Is.False);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(bytes));
            var reloaded = new M1RuntimeCoordinator(ContentRoot, _savePath);
            Assert.That(reloaded.ReadTowerSavedReward110(before.Battle.BattleId,after.Battle.BattleId),Is.Null,
                "A reloaded coordinator must not manufacture a new saved-completion notification.");
            var restoredHero = Campaign(reloaded).Guild.Recruits.Single(recruit => recruit.AuthoredStableRecruitId == _selectedHeroId);
            Assert.That(CanonicalJson.Serialize(restoredHero.Equipment), Is.EqualTo(CanonicalJson.Serialize(arrived[0].Equipment)));
            Assert.That(Campaign(reloaded).Battle.BattleId, Is.EqualTo(after.Battle.BattleId));
            Assert.That(Campaign(reloaded).Guild.Development.ClaimedBattleRewardIds.Count(id => id == rewardId), Is.EqualTo(1));
            Assert.That(reloaded.AdvanceTowerAutoAfterVictory108().Succeeded, Is.False);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(bytes));
        }

        static void RemoveTestDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;
            foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
            foreach (var child in Directory.GetDirectories(directory)) Directory.Delete(child, false);
            Directory.Delete(directory, false);
        }

        static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static FieldInfo Field(string name) => typeof(M1RuntimeCoordinator).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static CampaignState Campaign(M1RuntimeCoordinator coordinator) => (CampaignState)Field("_campaign").GetValue(coordinator);
        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            return result.Value;
        }
    }
}
#endif
