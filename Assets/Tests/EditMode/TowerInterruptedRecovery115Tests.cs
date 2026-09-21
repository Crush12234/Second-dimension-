#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    // Replays the untouched interrupted R288 save. No battle, roll, receipt,
    // opening flag, or expected combat outcome is constructed by this fixture.
    public sealed class TowerInterruptedRecovery115Tests
    {
        const string SourceVariable = "SD_TOWER_INTERRUPTED115_SOURCE";
        const string PreservedSource = @"C:\SecondDimension\BuildEvidence\Reset110\R288_TowerSoak301_16x_1800s\EarnedReviewSave097.json";
        const string PreservedSha256 = "511D162B27A4FCD6371BE562EF37BD0FEB4B27EC29C81F83B03A09C5381DDC1E";
        const string TemporaryPrefix = "SecondDimensionTowerInterrupted115_";

        [Test, Timeout(600000), Category("PreservedEarnedSave115")]
        public void InterruptedR288Victory315BanksOnceStarts316AndSurvivesReloadAndRetries115()
        {
            var configured = Environment.GetEnvironmentVariable(SourceVariable);
            var source = string.IsNullOrWhiteSpace(configured) ? PreservedSource : configured;
            if (!File.Exists(source))
            {
                if (!string.IsNullOrWhiteSpace(configured))
                    Assert.Fail(SourceVariable + " names a missing preserved save: " + source);
                Assert.Ignore("The preserved R288 save is unavailable. Set " + SourceVariable +
                    " to an exact copy with SHA256 " + PreservedSha256 + ".");
            }
            Assert.That(HashFile(source), Is.EqualTo(PreservedSha256),
                "Only the verified interrupted R288 save is accepted; do not substitute a synthesized fixture.");
            var directory = Path.Combine(Path.GetTempPath(), TemporaryPrefix + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "interrupted_r288.json");
            try
            {
                File.Copy(source, path);
                Assert.That(HashFile(path), Is.EqualTo(PreservedSha256));
                var preserved = ReadSaved(path);
                var clock = Stopwatch.StartNew();
                var coordinator = new M1RuntimeCoordinator(ContentRoot, path);
                var loadMs = clock.Elapsed.TotalMilliseconds;
                var before = Campaign(coordinator);
                Assert.That(CanonicalJson.Sha256Hex(before), Is.EqualTo(preserved.CanonicalStateHash),
                    "The real loader must retain the interrupted earned state.");
                var viewBefore = ValidTowerView(coordinator);
                Assert.That(viewBefore.TowerFloorNumber, Is.EqualTo(315));
                Assert.That(viewBefore.HighestClearedTowerFloor, Is.EqualTo(314));
                Assert.That(viewBefore.TowerBattleRewardAwaitingClaim, Is.True);
                Assert.That(before.Battle.Phase, Is.EqualTo(BattlePhase.Resolved));
                Assert.That(before.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory));
                Assert.That(before.Battle.Reward.Claimed, Is.False);
                Assert.That(before.Guild.Recruits.Count, Is.EqualTo(61));
                var originalClaims = before.Guild.Development.ClaimedBattleRewardIds.ToArray();
                Assert.That(originalClaims.Length, Is.EqualTo(641));
                Assert.That(originalClaims.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(641));
                var reward = before.Battle.Reward;
                Assert.That(originalClaims, Does.Not.Contain(reward.RewardId));
                Assert.That(reward.EquipmentReward, Is.Not.Null);
                Assert.That(OwnedItems(before).ContainsKey(reward.EquipmentReward.InstanceId), Is.False);
                var operationId = Progression(before).ActiveAbyssOperation.OperationInstanceId;
                var changed = 0;
                coordinator.Changed += () => changed++;

                clock.Restart();
                var advanced = coordinator.AdvanceTowerAutoAfterVictory108();
                var advanceMs = clock.Elapsed.TotalMilliseconds;
                Assert.That(advanced.Succeeded, Is.True, advanced.Message);
                Assert.That(changed, Is.EqualTo(1), "The recovered claim and next battle commit as one transition.");
                var after = Campaign(coordinator);
                Assert.That(after, Is.Not.SameAs(before));
                var viewAfter = ValidTowerView(coordinator);
                Assert.That(viewAfter.HighestClearedTowerFloor, Is.EqualTo(315));
                Assert.That(viewAfter.TowerFloorNumber, Is.EqualTo(316));
                Assert.That(viewAfter.TotalTowerClears, Is.EqualTo(viewBefore.TotalTowerClears + 1));
                Assert.That(viewAfter.TowerBattleInProgress, Is.True);
                Assert.That(viewAfter.TowerBattleRewardAwaitingClaim, Is.False);
                Assert.That(after.Battle.Outcome, Is.EqualTo(BattleOutcome.InProgress));
                Assert.That(after.Battle.BattleId, Is.Not.EqualTo(before.Battle.BattleId));
                Assert.That(Progression(after).ActiveAbyssOperation.OperationInstanceId, Is.Not.EqualTo(operationId));

                // Read the completion proof committed by the real transition;
                // never call a generator to manufacture an expected receipt.
                var completion = Progression(after).AbyssAuthorityEntries.Single(
                    entry => entry.OperationInstanceId == operationId);
                Assert.That(completion.Aborted, Is.False);
                Assert.That(completion.CompletionProof.ExistingBattleRewardReceiptId, Is.EqualTo(reward.RewardId));
                Assert.That(completion.CompletionProof.StepReceipts.Any(step =>
                    step.BattleId == before.Battle.BattleId &&
                    step.ExistingBattleRewardReceiptId == reward.RewardId &&
                    step.BattleFinalStateHash == before.Battle.FinalStateHash), Is.True);
                var floorReceipt = completion.CompletionProof.CompletionReceipt;
                Assert.That(floorReceipt.AppliedVersion, Is.EqualTo(1));
                Assert.That(floorReceipt.Outcome, Is.EqualTo("OPERATION_COMPLETE"));
                CollectionAssert.AreEquivalent(originalClaims.Concat(new[] { reward.RewardId, floorReceipt.ReceiptId }),
                    after.Guild.Development.ClaimedBattleRewardIds,
                    "Only the saved battle reward and its legitimate floor completion may enter the claim ledger.");
                Assert.That(after.Guild.Development.ClaimedBattleRewardIds.Distinct(StringComparer.Ordinal).Count(),
                    Is.EqualTo(originalClaims.Length + 2));
                Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp +
                    reward.GuildTreasuryXpAward + floorReceipt.GuildXp));
                Assert.That(after.Guild.Development.HallEnhancementXp, Is.EqualTo(
                    before.Guild.Development.HallEnhancementXp + reward.HallEnhancementXpAward + Math.Max(1, floorReceipt.HallXp)));
                AssertRetainedAndRewarded(before, after, reward);

                var afterHash = CanonicalJson.Sha256Hex(after);
                var saved = ReadSaved(path);
                Assert.That(saved.CanonicalStateHash, Is.EqualTo(afterHash));
                Assert.That(CanonicalJson.Sha256Hex(saved.CampaignState), Is.EqualTo(afterHash));
                AssertRejectedRetriesDoNotWrite(coordinator, directory, afterHash);
                Assert.That(changed, Is.EqualTo(1));
                var beforeReloadFiles = FileSignature(directory);

                clock.Restart();
                var reloaded = new M1RuntimeCoordinator(ContentRoot, path);
                var reloadMs = clock.Elapsed.TotalMilliseconds;
                Assert.That(FileSignature(directory), Is.EqualTo(beforeReloadFiles), "A valid durable reload must not rewrite the earned save.");
                Assert.That(CanonicalJson.Sha256Hex(Campaign(reloaded)), Is.EqualTo(afterHash));
                var reloadedView = ValidTowerView(reloaded);
                Assert.That(reloadedView.HighestClearedTowerFloor, Is.EqualTo(315));
                Assert.That(reloadedView.TowerFloorNumber, Is.EqualTo(316));
                Assert.That(reloadedView.TowerBattleInProgress, Is.True);
                Assert.That(Campaign(reloaded).Battle.BattleId, Is.EqualTo(after.Battle.BattleId));
                AssertRejectedRetriesDoNotWrite(reloaded, directory, afterHash);
                Assert.That(ReadSaved(path).CanonicalStateHash, Is.EqualTo(afterHash));
                TestContext.Progress.WriteLine("TOWER_INTERRUPTED115 sourceSha256=" + PreservedSha256 +
                    " recoveredFloor=315 nextFloor=316 recruits=" + after.Guild.Recruits.Count +
                    " claimsBefore=" + originalClaims.Length + " claimsAfter=" + after.Guild.Development.ClaimedBattleRewardIds.Count +
                    " loadMs=" + loadMs.ToString("F1") + " advanceMs=" + advanceMs.ToString("F1") +
                    " reloadMs=" + reloadMs.ToString("F1") + " duplicateRetries=8");
            }
            finally
            {
                try
                {
                    Assert.That(HashFile(source), Is.EqualTo(PreservedSha256), "The preserved source must remain untouched even if recovery fails.");
                }
                finally
                {
                    var full = Path.GetFullPath(directory);
                    var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                    if (full.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) &&
                        Path.GetFileName(full).StartsWith(TemporaryPrefix, StringComparison.Ordinal) && Directory.Exists(full))
                        Directory.Delete(full, true);
                }
            }
        }

        static void AssertRetainedAndRewarded(CampaignState before, CampaignState after, BattleRewardState reward)
        {
            // This captured non-milestone floor adds no recruit; retain every identity and equipped instance.
            CollectionAssert.AreEquivalent(before.Guild.Recruits.Select(recruit => recruit.RecruitId),
                after.Guild.Recruits.Select(recruit => recruit.RecruitId));
            var memberRewards = reward.MemberRewards.ToDictionary(member => member.MemberId, StringComparer.Ordinal);
            foreach (var original in before.Guild.Recruits)
            {
                var retained = after.Guild.Recruits.Single(recruit => recruit.RecruitId == original.RecruitId);
                Assert.That(retained.AuthoredStableRecruitId, Is.EqualTo(original.AuthoredStableRecruitId));
                Assert.That(CanonicalJson.Serialize(retained.Equipment), Is.EqualTo(CanonicalJson.Serialize(original.Equipment)), original.RecruitId);
                var xpAward = memberRewards.TryGetValue(original.RecruitId, out var memberReward) ? memberReward.PersonalXp : 0;
                Assert.That(retained.Progression.TotalPersonalXp,
                    Is.EqualTo(original.Progression.TotalPersonalXp + xpAward), original.RecruitId + " saved XP must apply once.");
                CollectionAssert.IsSubsetOf(original.Progression.LearnedArtIds, retained.Progression.LearnedArtIds, original.RecruitId);
                CollectionAssert.IsSubsetOf(original.Progression.UnlockedTreeIds, retained.Progression.UnlockedTreeIds, original.RecruitId);
                foreach (var art in original.Progression.ArtMastery)
                {
                    var retainedArt = retained.Progression.ArtMastery.Single(current => current.ArtId == art.ArtId);
                    Assert.That(retainedArt.MeaningfulUses, Is.GreaterThanOrEqualTo(art.MeaningfulUses), original.RecruitId + "/" + art.ArtId);
                    Assert.That(retainedArt.MasteryPoints, Is.GreaterThanOrEqualTo(art.MasteryPoints), original.RecruitId + "/" + art.ArtId);
                }
            }
            var ownedAfter = OwnedItems(after);
            foreach (var item in OwnedItems(before))
                Assert.That(ownedAfter.TryGetValue(item.Key, out var retainedItem) ? CanonicalJson.Serialize(retainedItem) : null,
                    Is.EqualTo(CanonicalJson.Serialize(item.Value)), "Retain owned instance " + item.Key);
            Assert.That(CanonicalJson.Serialize(ownedAfter[reward.EquipmentReward.InstanceId]),
                Is.EqualTo(CanonicalJson.Serialize(reward.EquipmentReward)), "The pending saved item must be granted exactly once.");
            CollectionAssert.IsSubsetOf(before.Guild.Development.AppliedAdventureAuthorityIds,
                after.Guild.Development.AppliedAdventureAuthorityIds);
            CollectionAssert.IsSubsetOf(Progression(before).AppliedReceiptIds, Progression(after).AppliedReceiptIds);
            foreach (var entry in Progression(before).AbyssAuthorityEntries)
                Assert.That(CanonicalJson.Serialize(Progression(after).AbyssAuthorityEntries.Single(current => current.EntryHash == entry.EntryHash)),
                    Is.EqualTo(CanonicalJson.Serialize(entry)), "Previous Tower proof must remain intact.");
        }

        static void AssertRejectedRetriesDoNotWrite(M1RuntimeCoordinator coordinator, string directory, string expectedHash)
        {
            var files = FileSignature(directory);
            var state = Campaign(coordinator);
            var changed = 0;
            Action changedHandler = () => changed++;
            coordinator.Changed += changedHandler;
            try
            {
                var attempts = new Func<M1CommandResult>[] {
                    coordinator.AdvanceTowerAutoAfterVictory108, coordinator.ClaimBattleRewards,
                    coordinator.BankTowerVictory110, coordinator.AdvanceTowerRun081 };
                foreach (var attempt in attempts)
                {
                    var result = attempt();
                    Assert.That(result.Succeeded, Is.False, attempt.Method.Name + " must not reapply floor315 or finish unresolved floor316. " + result.Message);
                    Assert.That(Campaign(coordinator), Is.SameAs(state), attempt.Method.Name);
                    Assert.That(FileSignature(directory), Is.EqualTo(files), attempt.Method.Name + " changed primary/backup bytes, timestamps, or temporary files.");
                }
                Assert.That(changed, Is.Zero);
                Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)), Is.EqualTo(expectedHash));
            }
            finally { coordinator.Changed -= changedHandler; }
        }

        static Dictionary<string, EquipmentItemState> OwnedItems(CampaignState state)
        {
            var items = state.Guild.Inventory.Concat(state.Guild.Recruits.SelectMany(recruit =>
                recruit.Equipment.Assignments.Where(slot => slot.Item != null).Select(slot => slot.Item))).ToArray();
            Assert.That(items.Select(item => item.InstanceId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(items.Length),
                "Owned item instances must remain unique across inventory and every equipment slot.");
            return items.ToDictionary(item => item.InstanceId, StringComparer.Ordinal);
        }

        static CampaignProgressionPresentationState022 ValidTowerView(M1RuntimeCoordinator coordinator)
        {
            var view = coordinator.CampaignProgression022;
            Assert.That(view.IsAvailable, Is.True, view.Error);
            Assert.That(view.TowerAuthorityError094, Is.Empty);
            return view;
        }
        static SaveEnvelopeV1 ReadSaved(string path)
        {
            var read = new AtomicSaveStore().ReadWithRecovery(path);
            Assert.That(read.IsSuccess, Is.True, string.Join("; ", read.Errors));
            return read.Value;
        }
        static string FileSignature(string directory) => string.Join("\n", Directory.GetFiles(directory)
            .OrderBy(path => path, StringComparer.Ordinal).Select(path => Path.GetFileName(path) + "|" +
                new FileInfo(path).Length + "|" + File.GetLastWriteTimeUtc(path).Ticks + "|" + HashFile(path)));
        static string HashFile(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
        }
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static readonly FieldInfo CampaignField = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
        // Read-only inspection: no reflection setter or fixture-field mutation.
        static CampaignState Campaign(M1RuntimeCoordinator coordinator) => (CampaignState)CampaignField.GetValue(coordinator);
        static CampaignProgressionState022 Progression(CampaignState campaign) => campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
    }
}
#endif
