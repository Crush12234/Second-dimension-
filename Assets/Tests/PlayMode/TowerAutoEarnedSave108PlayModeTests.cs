using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerAutoEarnedSave108PlayModeTests
    {
        [Test, Timeout(300000)]
        public void ResetAffectedVictoryCanContinueWithRewardsExactlyOnce110()
        {
            var source = Environment.GetEnvironmentVariable("SD_RESET110_SOURCE");
            var output = Environment.GetEnvironmentVariable("SD_RESET110_EVIDENCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source) || string.IsNullOrWhiteSpace(output))
                Assert.Ignore("Set the preserved reset-session save and isolated evidence directory.");
            Directory.CreateDirectory(output);
            var save = Path.Combine(output, "reset_affected_victory.json");
            File.Copy(source, save, true);
            var sourceHash = Hash108(source);
            var coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), save);
            var before = new AtomicSaveStore().ReadWithRecovery(save).Value.CampaignState;
            var terminal = M2BattleViewAccess098.Read(coordinator);
            Assert.That(terminal.IsResolved, Is.True);
            Assert.That(terminal.Outcome, Is.EqualTo("Victory"));
            Assert.That(terminal.Reward.Claimed, Is.False);
            var floor = coordinator.CampaignProgression022.TowerFloorNumber;
            var advanced = coordinator.AdvanceTowerAutoAfterVictory108();
            Assert.That(advanced.Succeeded, Is.True, advanced.Message);
            var persisted = new AtomicSaveStore().ReadWithRecovery(save);
            Assert.That(persisted.IsSuccess, Is.True, string.Join("; ", persisted.Errors));
            var after = persisted.Value.CampaignState;
            Assert.That(before.Guild.Recruits.Select(x => x.RecruitId),
                Is.SubsetOf(after.Guild.Recruits.Select(x => x.RecruitId)));
            Assert.That(after.Guild.Development.ClaimedBattleRewardIds.Count(x => x == terminal.Reward.RewardId), Is.EqualTo(1));
            Assert.That(after.Guild.TreasuryXp, Is.GreaterThanOrEqualTo(before.Guild.TreasuryXp));
            var reloaded = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), save);
            Assert.That(reloaded.CampaignProgression022.TowerFloorNumber, Is.EqualTo(floor + 1));
            Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(floor));
            Assert.That(reloaded.CampaignProgression022.TowerBattleInProgress, Is.True);
            var durableHash = Hash108(save);
            Assert.That(reloaded.AdvanceTowerAutoAfterVictory108().Succeeded, Is.False);
            Assert.That(Hash108(save), Is.EqualTo(durableHash));
            Assert.That(Hash108(source), Is.EqualTo(sourceHash));
        }

        [UnityTest]
        public IEnumerator EarnedFloorAtFourTimes108() => RunEarnedFloor108(4f, "4x");

        [UnityTest]
        public IEnumerator EarnedFloorAtSixteenTimes108() => RunEarnedFloor108(16f, "16x");

        [UnityTest]
        public IEnumerator AffectedSaveConsecutiveFloorsAtSixteenTimes109() =>
            RunEarnedFloor108(16f, "affected_16x_2", 2);

        [UnityTest]
        public IEnumerator AffectedResolvedVictorySurvivesPostSaveObserverFailure109()
        {
            var source = Environment.GetEnvironmentVariable("SD_TOWER108_SOURCE");
            var evidenceRoot = Environment.GetEnvironmentVariable("SD_TOWER108_EVIDENCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source) ||
                string.IsNullOrWhiteSpace(evidenceRoot))
            {
                Assert.Ignore("Set SD_TOWER108_SOURCE and SD_TOWER108_EVIDENCE for the affected-save recovery proof.");
                yield break;
            }

            var sourceHash = Hash108(source);
            var runFolder = Path.Combine(evidenceRoot, "affected_post_commit_recovery");
            Directory.CreateDirectory(runFolder);
            var isolated = Path.Combine(runFolder, "resolved_victory_isolated.json");
            File.Copy(source, isolated, true);
            var content = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var coordinator = new M1RuntimeCoordinator(content, isolated);
            var clearedFloor = coordinator.CampaignProgression022.TowerFloorNumber;
            var root = new GameObject("Affected Post-Save Recovery 109");
            var host = new GameObject("Affected Post-Save Recovery Host 109", typeof(RectTransform));
            var controller = root.AddComponent<M2BattleExperienceController072>();
            try
            {
                controller.Initialize(coordinator, host.GetComponent<RectTransform>());
                controller.AnimationSpeed = 16f;
                Assert.That(controller.EnterCurrentBattle(), Is.True);
                controller.SetAutoOrders091(true);

                M2BattleView terminal = null;
                var deadline = Time.realtimeSinceStartup + 150f;
                while (terminal == null || !terminal.IsResolved)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                        "The isolated affected battle did not reach its real victory state.");
                    Assert.That(controller.AutoOrdersEnabled091, Is.True,
                        "Auto paused before producing the isolated victory state.");
                    yield return null;
                    terminal = M2BattleViewAccess098.Read(coordinator);
                }
                controller.SetAutoOrders091(false);
                Assert.That(terminal.Outcome, Is.EqualTo("Victory"));

                var laterObserverCalled = 0;
                Action rejectedObserver = () =>
                    throw new InvalidOperationException("simulated post-save presentation failure 109");
                Action laterObserver = () => laterObserverCalled++;
                coordinator.Changed += rejectedObserver;
                coordinator.Changed += laterObserver;
                var priorIgnore = LogAssert.ignoreFailingMessages;
                LogAssert.ignoreFailingMessages = true;
                M1CommandResult advanced;
                try
                {
                    advanced = coordinator.AdvanceTowerAutoAfterVictory108();
                }
                finally
                {
                    LogAssert.ignoreFailingMessages = priorIgnore;
                    coordinator.Changed -= rejectedObserver;
                    coordinator.Changed -= laterObserver;
                }

                Assert.That(advanced.Succeeded, Is.True, advanced.Message);
                Assert.That(laterObserverCalled, Is.EqualTo(1),
                    "One failing presentation observer must not block later refresh observers.");
                var next = coordinator.CampaignProgression022;
                Assert.That(next.TowerFloorNumber, Is.EqualTo(clearedFloor + 1));
                Assert.That(next.TowerBattleInProgress, Is.True);
                Assert.That(M2BattleViewAccess098.Read(coordinator).BattleId,
                    Is.Not.EqualTo(terminal.BattleId));

                var store = new AtomicSaveStore();
                var persisted = store.ReadWithRecovery(isolated);
                Assert.That(persisted.IsSuccess, Is.True, string.Join("; ", persisted.Errors));
                Assert.That(persisted.Value.CanonicalStateHash, Is.Not.Empty,
                    "The recovered next-floor save must retain its verified canonical state hash.");
                var durableHash = Hash108(isolated);
                var duplicate = coordinator.AdvanceTowerAutoAfterVictory108();
                Assert.That(duplicate.Succeeded, Is.False,
                    "An in-progress next floor cannot be claimed as the old victory twice.");
                Assert.That(Hash108(isolated), Is.EqualTo(durableHash));
                Assert.That(Hash108(source), Is.EqualTo(sourceHash),
                    "The captured affected source save changed.");
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(host);
            }
        }

        private static IEnumerator RunEarnedFloor108(float speed, string label, int floorCount = 1)
        {
            var source = Environment.GetEnvironmentVariable("SD_TOWER108_SOURCE");
            var evidenceRoot = Environment.GetEnvironmentVariable("SD_TOWER108_EVIDENCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source) ||
                string.IsNullOrWhiteSpace(evidenceRoot))
            {
                Assert.Ignore("Set SD_TOWER108_SOURCE and SD_TOWER108_EVIDENCE for the earned-save runtime proof.");
                yield break;
            }

            var sourceHash = Hash108(source);
            var runFolder = Path.Combine(evidenceRoot, label);
            Directory.CreateDirectory(runFolder);
            var isolated = Path.Combine(runFolder, "earned_floor_isolated.json");
            File.Copy(source, isolated, true);
            var store = new AtomicSaveStore();
            var beforeSaved = store.ReadWithRecovery(isolated);
            Assert.That(beforeSaved.IsSuccess, Is.True, string.Join("; ", beforeSaved.Errors));
            var claimedBefore = beforeSaved.Value.CampaignState.Guild.Development
                .ClaimedBattleRewardIds.ToArray();
            var content = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var coordinator = new M1RuntimeCoordinator(content, isolated);
            var start = coordinator.CampaignProgression022;
            Assert.That(start.IsAvailable, Is.True, start.Error);
            Assert.That(start.TowerBattleInProgress, Is.True, "The copied earned save must resume its real battle.");
            var floor = start.TowerFloorNumber;
            var targetFloor = floor + Math.Max(1, floorCount) - 1;
            var battleId = M2BattleViewAccess098.Read(coordinator).BattleId;
            var rewardId = M2BattleViewAccess098.Read(coordinator).Reward?.RewardId;

            var root = new GameObject("Earned Tower Auto 108 " + label);
            var host = new GameObject("Earned Tower Auto Host 108 " + label, typeof(RectTransform));
            var controller = root.AddComponent<M2BattleExperienceController072>();
            try
            {
                controller.Initialize(coordinator, host.GetComponent<RectTransform>());
                controller.ContinueTowerAuto107 = () => (M1CommandResult)typeof(M1FlowPresenter)
                    .GetMethod("PrepareNextTowerAutoBattle107", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { coordinator });
                controller.AnimationSpeed = speed;
                Assert.That(controller.EnterCurrentBattle(), Is.True);
                controller.SetAutoOrders091(true);
                Assert.That(controller.AutoOrdersEnabled091, Is.True);

                var deadline = Time.realtimeSinceStartup + (speed >= 16f ? 150f : 240f);
                while (controller.LastCompletedTowerFloor108 < targetFloor)
                {
                    Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline),
                        label + " did not complete the copied earned Tower floor in its bounded runtime window.");
                    Assert.That(controller.AutoOrdersEnabled091, Is.True,
                        label + " Auto paused before the copied earned floor was safely completed.");
                    yield return null;
                }
                controller.SetAutoOrders091(false);

                var next = coordinator.CampaignProgression022;
                Assert.That(next.HighestClearedTowerFloor, Is.EqualTo(targetFloor));
                Assert.That(next.TowerFloorNumber, Is.EqualTo(targetFloor + 1));
                Assert.That(next.TowerBattleInProgress, Is.True);
                Assert.That(M2BattleViewAccess098.Read(coordinator).BattleId, Is.Not.EqualTo(battleId));
                var feed = host.GetComponentsInChildren<Text>(true)
                    .First(value => value.name == "Tower Auto Decision Feed Text 108").text;
                Assert.That(feed, Does.Contain("AUTO DECISIONS"));
                Assert.That(feed, Does.Contain("FLOOR " + targetFloor + " CLEARED"));
                Assert.That(feed, Does.Contain("REWARDS CLAIMED  •  PROGRESS SAVED"));
                Assert.That(feed, Does.Contain(" → "), "The runtime feed did not show committed Union/Hero actions.");

                var saved = store.ReadWithRecovery(isolated);
                Assert.That(saved.IsSuccess, Is.True, string.Join("; ", saved.Errors));
                var claimedAfter = saved.Value.CampaignState.Guild.Development
                    .ClaimedBattleRewardIds.ToArray();
                var newReceipts = claimedAfter.Where(value => !claimedBefore.Contains(value))
                    .Distinct(StringComparer.Ordinal).ToArray();
                Assert.That(newReceipts.Length, Is.EqualTo(2 * Math.Max(1, floorCount)),
                    "Each Tower clear adds one battle reward receipt and one floor reward receipt.");
                Assert.That(newReceipts.Count(value => value.StartsWith("ABYSSREC022_", StringComparison.Ordinal)),
                    Is.EqualTo(Math.Max(1, floorCount)));
                Assert.That(newReceipts.All(receipt => claimedAfter.Count(value => value == receipt) == 1),
                    Is.True, "Every new reward receipt must occur exactly once.");
                if (!string.IsNullOrWhiteSpace(rewardId))
                    Assert.That(saved.Value.CampaignState.Guild.Development.ClaimedBattleRewardIds
                        .Count(value => value == rewardId), Is.EqualTo(1));
                var reloaded = new M1RuntimeCoordinator(content, isolated);
                Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(targetFloor));
                Assert.That(reloaded.CampaignProgression022.TowerFloorNumber, Is.EqualTo(targetFloor + 1));
                Assert.That(reloaded.CampaignProgression022.TowerBattleInProgress, Is.True);
                Assert.That(Hash108(source), Is.EqualTo(sourceHash), "The protected source save changed.");

                File.WriteAllLines(Path.Combine(runFolder, "timing108.txt"), new[]
                {
                    "speed=" + speed.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x",
                    "sourceFloor=" + floor,
                    "completedFloor=" + controller.LastCompletedTowerFloor108,
                    "floorCount=" + Math.Max(1, floorCount),
                    "actionGapSeconds=" + controller.LastAutoActionGapSeconds108.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    "victoryToNextBattleSeconds=" + controller.LastTowerTransitionSeconds108.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    "totalFloorSeconds=" + controller.LastTowerFloorSeconds108.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    "sourceHash=" + sourceHash,
                    "isolatedHash=" + Hash108(isolated),
                    "rewardReceiptDelta=2 (one battle + one floor; each exactly once)",
                    "reloadHighestFloor=" + reloaded.CampaignProgression022.HighestClearedTowerFloor
                });
            }
            finally
            {
                UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(host);
            }
        }

        private static string Hash108(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

    }
}
