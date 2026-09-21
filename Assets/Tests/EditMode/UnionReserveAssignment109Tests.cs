using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class UnionReserveAssignment109Tests
    {
        readonly M1CommandService _commands = new M1CommandService();

        [Test]
        public void ExactEmptyClickRejectsGapsAndNeverRedirects109()
        {
            var state = Fixture109();
            var before = CanonicalJson.Serialize(state);
            Assert.That(_commands.AssignReserveRecruitToUnion109(state, "R4", 0, 5, null).Errors,
                Does.Contain("M1_FILL_UNION_SLOTS_IN_ORDER"));
            Assert.That(CanonicalJson.Serialize(state), Is.EqualTo(before));
            var placed = Require109(_commands.AssignReserveRecruitToUnion109(state, "R4", 0, 2, null));
            Assert.That(placed.Guild.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "R0", "R1", "R4" }));
            AssertRoster109(state, placed);
        }

        [Test]
        public void OccupiedClickRequiresExactConfirmationAndDisplacedHeroReturnsToReserve109()
        {
            var state = Fixture109();
            Assert.That(_commands.AssignReserveRecruitToUnion109(state, "R4", 0, 0, null).Errors,
                Does.Contain("M1_RESERVE_DESTINATION_CHANGED"));
            Assert.That(_commands.AssignRecruitToUnion(state, "R4", 0, 0).IsSuccess, Is.False,
                "Unconfirmed legacy callers must not gain implicit occupied-slot swaps.");
            var swapped = Require109(_commands.AssignReserveRecruitToUnion109(state, "R4", 0, 0, "R0"));
            Assert.That(swapped.Guild.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "R4", "R1" }));
            Assert.That(swapped.Guild.Unions[0].LeaderRecruitId, Is.EqualTo("R4"));
            Assert.That(swapped.Guild.Unions.SelectMany(value => value.MemberRecruitIds), Does.Not.Contain("R0"));
            Assert.That(swapped.Guild.Recruits.Any(value => value.RecruitId == "R0"), Is.True);
            AssertRoster109(state, swapped);
            Assert.That(_commands.AssignReserveRecruitToUnion109(swapped, "R5", 0, 0, "R0").Errors,
                Does.Contain("M1_RESERVE_DESTINATION_CHANGED"));
            Assert.That(_commands.AssignReserveRecruitToUnion109(swapped, "R4", 1, 0, "R2").Errors,
                Does.Contain("M1_SELECTED_HERO_NO_LONGER_IN_RESERVE"));
        }

        [Test]
        public void AssignedCrossUnionDragStillSwapsWithoutDuplicates109()
        {
            var state = Fixture109();
            var moved = Require109(_commands.AssignRecruitToUnion(state, "R0", 1, 1));
            Assert.That(moved.Guild.Unions[0].MemberRecruitIds, Is.EqualTo(new[] { "R3", "R1" }));
            Assert.That(moved.Guild.Unions[1].MemberRecruitIds, Is.EqualTo(new[] { "R2", "R0" }));
            AssertRoster109(state, moved);
        }

        [Test]
        public void AffectedSixtyOneOwnedRosterSwapPersistsAndFeedsProductionBattleMapping109()
        {
            var source = Environment.GetEnvironmentVariable("SECOND_DIMENSION_RESET_PROFILE");
            if (string.IsNullOrWhiteSpace(source))
                Assert.Ignore("Set SECOND_DIMENSION_RESET_PROFILE to the preserved affected profile copy.");
            var sourceBytes = File.ReadAllBytes(source);
            var directory = Path.Combine(Path.GetTempPath(), "SecondDimension_Union109_" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "isolated.json");
            Directory.CreateDirectory(directory);
            File.Copy(source, path);
            try
            {
                var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
                var coordinator = new M1RuntimeCoordinator(contentRoot, path);
                // The preserved source legitimately owns an unbanked Tower
                // victory. Finish that reward/return boundary through the real
                // commands before testing a mutable Guild roster.
                var terminal = M2BattleViewAccess098.Read(coordinator);
                if (terminal?.IsResolved == true && terminal.Reward?.Claimed == false)
                {
                    var claimed = coordinator.ClaimBattleRewards();
                    Assert.That(claimed.Succeeded, Is.True, claimed.Message);
                }
                for (var guard = 0;
                     !string.IsNullOrWhiteSpace(coordinator.CampaignProgression022.ActiveAbyssOperationId) && guard < 32;
                     guard++)
                {
                    var returned = coordinator.AdvanceTowerRun081();
                    Assert.That(returned.Succeeded, Is.True, returned.Message);
                }
                Assert.That(coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Empty);
                var campaign = (CampaignState)Field109(typeof(M1RuntimeCoordinator), "_campaign").GetValue(coordinator);
                Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(61));
                var assigned = new HashSet<string>(campaign.Guild.Unions.SelectMany(value => value.MemberRecruitIds));
                var reserve = campaign.Guild.Recruits.First(value => !assigned.Contains(value.RecruitId));
                var unionIndex = campaign.Guild.Unions.ToList().FindIndex(value => value.MemberRecruitIds.Count > 0);
                var outgoing = campaign.Guild.Unions[unionIndex].MemberRecruitIds[0];
                var result = coordinator.AssignReserveRecruitToUnion109(reserve.RecruitId, unionIndex, 0, outgoing);
                Assert.That(result.Succeeded, Is.True, result.Message);
                var reloaded = new M1RuntimeCoordinator(contentRoot, path);
                var saved = (CampaignState)Field109(typeof(M1RuntimeCoordinator), "_campaign").GetValue(reloaded);
                AssertRoster109(campaign, saved);
                Assert.That(saved.Guild.Unions[unionIndex].MemberRecruitIds[0], Is.EqualTo(reserve.RecruitId));
                Assert.That(saved.Guild.Unions.SelectMany(value => value.MemberRecruitIds), Does.Not.Contain(outgoing));
                var combat = (M2CombatContent)Field109(typeof(M1RuntimeCoordinator), "_combatContent").GetValue(reloaded);
                var projected = (IReadOnlyList<BattleUnionState>)typeof(M2BattleCommandService)
                    .GetMethod("CreatePlayerUnions", BindingFlags.NonPublic | BindingFlags.Static)
                    .Invoke(null, new object[] { saved, combat, null, 10 });
                var projectedUnion = projected.Single(value => value.UnionId == saved.Guild.Unions[unionIndex].UnionId);
                Assert.That(projectedUnion.Members.Select(value => value.MemberId), Does.Contain(reserve.RecruitId));
                Assert.That(projected.SelectMany(value => value.Members).Select(value => value.MemberId),
                    Does.Not.Contain(outgoing));
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(sourceBytes));
            }
            finally
            {
                if (Directory.Exists(directory) && Path.GetFullPath(directory).StartsWith(
                        Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase))
                    Directory.Delete(directory, true);
            }
        }

        static FieldInfo Field109(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        static void Set109(object target, string name, object value) => Field109(target.GetType(), name).SetValue(target, value);
        static T Require109<T>(Result<T> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            return result.Value;
        }
        static void AssertRoster109(CampaignState before, CampaignState after)
        {
            Assert.That(CanonicalJson.Serialize(after.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(before.Guild.Recruits)));
            Assert.That(CanonicalJson.Serialize(after.Guild.Inventory), Is.EqualTo(CanonicalJson.Serialize(before.Guild.Inventory)));
            Assert.That(after.Guild.TreasuryXp, Is.EqualTo(before.Guild.TreasuryXp));
            var members = after.Guild.Unions.SelectMany(value => value.MemberRecruitIds).ToArray();
            Assert.That(members.Distinct().Count(), Is.EqualTo(members.Length));
        }
        CampaignState Fixture109()
        {
            var profile = new NewGuildProfileState("Union regression", GameMode.Standard, TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(), false);
            var campaign = Require109(_commands.CreateNewGuild(new NewGuildCommand(
                "00000000-0000-0000-0000-000000000109", 109L, "1.0", "GUILD109", profile)));
            var recruits = Enumerable.Range(0, 6).Select(index => new RecruitState("R" + index, 100, 100, 20, 20)).ToArray();
            var unions = Enumerable.Range(0, 2).Select(index => new UnionState("U" + index, "Union " + index,
                UnionKind.Normal, "R" + (index * 2), new[] { "R" + (index * 2), "R" + (index * 2 + 1) },
                "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000)).ToArray();
            return campaign.With(new GuildState("GUILD109", 0, recruits, unions, Array.Empty<EquipmentItemState>()), campaign.OpeningFlow);
        }

    }
}
