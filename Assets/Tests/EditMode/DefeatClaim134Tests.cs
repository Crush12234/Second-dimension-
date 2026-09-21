using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class DefeatClaim134Tests
    {
        [Test, Timeout(120000)]
        public void EarnedObjectiveDefeatClaimsOnceClearsEncounterAndSurvivesReload134()
        {
            var source = Environment.GetEnvironmentVariable("SD_DEFEAT134_SOURCE");
            if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
                Assert.Ignore("Set SD_DEFEAT134_SOURCE to the preserved actual objective-defeat save.");
            var original = File.ReadAllBytes(source);
            var fixtureRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "SecondDimensionDefeat134"));
            var directory = Path.Combine(fixtureRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var save = Path.Combine(directory, "isolated.json");
            try
            {
                File.WriteAllBytes(save, original);
                var owner = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), save);
                var state = State134(owner);
                Assert.That(state.Battle.Outcome, Is.EqualTo(BattleOutcome.Defeat));
                Assert.That(state.Battle.Reward.Claimed, Is.False);
                Assert.That(state.Guild.GuildCity.PendingEncounter, Is.Not.Null);
                Assert.That(state.Battle.PlayerUnions.SelectMany(union => union.Members).Count(member => !member.Downed && member.CurrentHp > 0), Is.GreaterThan(0));
                var sourceCanonical = CanonicalJson.Sha256Hex(state);
                var projected = owner.ReadBattleView098();
                Assert.That(projected.Events.Single(item => item.EventType == "BATTLE_RESULT").Text,
                    Does.Contain("Surviving heroes"));
                Assert.That(CanonicalJson.Sha256Hex(State134(owner)), Is.EqualTo(sourceCanonical), "Presentation cannot rewrite hashed historical events.");
                var expected = new M2BattleCommandService().ClaimBattleRewards(state);
                Assert.That(expected.IsSuccess, Is.True, string.Join("; ", expected.Errors));
                var result = owner.ClaimBattleRewards(); Assert.That(result.Succeeded, Is.True, result.Message);
                var claimed = State134(owner);
                Assert.That(claimed.Battle.Reward.Claimed, Is.True);
                Assert.That(claimed.Guild.GuildCity.PendingEncounter, Is.Null);
                Assert.That(claimed.Guild.TreasuryXp, Is.EqualTo(expected.Value.Guild.TreasuryXp));
                foreach (var reward in state.Battle.Reward.MemberRewards)
                    Assert.That(CanonicalJson.Sha256Hex(claimed.Guild.Recruits.Single(x => x.RecruitId == reward.MemberId).Progression),
                        Is.EqualTo(CanonicalJson.Sha256Hex(expected.Value.Guild.Recruits.Single(x => x.RecruitId == reward.MemberId).Progression)));
                var finalCanonical = CanonicalJson.Sha256Hex(claimed);
                var repeated = owner.ClaimBattleRewards(); Assert.That(repeated.Succeeded, Is.True, repeated.Message);
                Assert.That(CanonicalJson.Sha256Hex(State134(owner)), Is.EqualTo(finalCanonical), "Repeated terminal claim cannot repeat XP, Arts, loot or return.");
                var reloaded = new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), save);
                Assert.That(CanonicalJson.Sha256Hex(State134(reloaded)), Is.EqualTo(finalCanonical));
                CollectionAssert.AreEqual(original, File.ReadAllBytes(source));
            }
            finally
            {
                // Resolve and bound the exact generated directory before recursive cleanup.
                var resolved = Path.GetFullPath(directory);
                Assert.That(Path.GetDirectoryName(resolved), Is.EqualTo(fixtureRoot).IgnoreCase);
                Assert.That(Guid.TryParseExact(Path.GetFileName(resolved), "N", out _), Is.True);
                if (Directory.Exists(resolved))
                {
                    Assert.That(File.GetAttributes(fixtureRoot) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Assert.That(File.GetAttributes(resolved) & FileAttributes.ReparsePoint, Is.EqualTo((FileAttributes)0));
                    Directory.Delete(resolved, true);
                }
            }
        }
        static CampaignState State134(M1RuntimeCoordinator owner) => (CampaignState)typeof(M1RuntimeCoordinator)
            .GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }
}
