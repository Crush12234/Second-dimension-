using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class RecoveryAssignmentDeployment097Tests
    {
        const string Aster097 = "SIGI_1C166701BBF16F1C";
        string _directory, _copy, _source;
        AtomicSaveStore _store;

        [SetUp]
        public void CopyActualEarnedFixture097()
        {
            _source = Path.Combine(Application.dataPath, "Tests", "Fixtures", "EarnedChapter001094.fixture.json");
            _directory = Path.Combine(Path.GetTempPath(), "SecondDimension_Recovery097_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _copy = Path.Combine(_directory, "Save.json");
            File.Copy(_source, _copy, false);
            _store = new AtomicSaveStore();
        }

        [TearDown]
        public void Cleanup097()
        { if (_directory != null && Directory.Exists(_directory)) Directory.Delete(_directory, true); }

        CampaignState Read097() => _store.ReadWithRecovery(_copy).Value.CampaignState;

        [Test]
        public void ActualAsterReturnsThroughNormalReserveCommandWithoutChangingIdentityVitalsOrGear097()
        {
            var originalBytes = File.ReadAllBytes(_source);
            var initial = Read097();
            var initialHash = CanonicalJson.Sha256Hex(initial);
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var coordinator = new M1RuntimeCoordinator(contentRoot, _copy);
            Assert.That(CanonicalJson.Sha256Hex(Read097()), Is.EqualTo(initialHash), "Do not silently migrate the evidence fixture.");
            var content = (M2CombatContent)typeof(M1RuntimeCoordinator).GetField("_combatContent",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(coordinator);
            var aster = initial.Guild.Recruits.Single(value => value.RecruitId == Aster097);
            Assert.That(aster.DisplayName, Is.EqualTo("Aster Marshlight"));
            Assert.That(aster.AuthoredStableRecruitId, Is.EqualTo("SIGREC_ASTER_MARSHLIGHT"));
            Assert.That(aster.CurrentHp, Is.EqualTo(223));
            Assert.That(aster.CurrentHp, Is.EqualTo(aster.MaximumHp));
            Assert.That(initial.Guild.Unions.SelectMany(value => value.MemberRecruitIds), Does.Contain(Aster097));
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(initial.Guild.GuildCity, Aster097), Is.False);
            var before = Project097(initial, content);
            Assert.That(before.SelectMany(union => union.Members).Any(member => member.MemberId == Aster097), Is.False);

            var returned = coordinator.SetGuildCityAssignment017D(Aster097, "Reserve");
            Assert.That(returned.Succeeded, Is.True, returned.Message);
            var after = Read097();
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(after.Guild.GuildCity, Aster097), Is.True);
            Assert.That(after.Guild.TreasuryXp, Is.EqualTo(initial.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(after.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(initial.Guild.Recruits)));
            Assert.That(CanonicalJson.Serialize(after.Guild.Unions), Is.EqualTo(CanonicalJson.Serialize(initial.Guild.Unions)));
            Assert.That(Project097(after, content).Sum(union => union.Members.Count), Is.EqualTo(before.Sum(union => union.Members.Count) + 1));
            var afterHash = CanonicalJson.Sha256Hex(after);
            coordinator = new M1RuntimeCoordinator(contentRoot, _copy);
            Assert.That(coordinator.State.CanonicalStateHash, Is.EqualTo(afterHash));
            Assert.That(CanonicalJson.Sha256Hex(Read097()), Is.EqualTo(afterHash));
            var second = coordinator.SetGuildCityAssignment017D(Aster097, "Reserve");
            Assert.That(second.Succeeded, Is.True, second.Message);
            Assert.That(CanonicalJson.Sha256Hex(Read097()), Is.EqualTo(afterHash));

            // Existing actual battle authority, not an extra projection engine.
            var battle = new M2BattleCommandService().StartEncounterBattle(Read097(), content,
                "RECOVERY_DEPLOYMENT_097", "Verify the same recalled Aster joins the battle.", 1);
            Assert.That(battle.IsSuccess, Is.True, string.Join("; ", battle.Errors));
            var member = battle.Value.Battle.PlayerUnions.SelectMany(union => union.Members)
                .Single(value => value.MemberId == Aster097);
            Assert.That(member.DisplayName, Is.EqualTo(aster.DisplayName));
            Assert.That(member.LearnedArtIds, Does.Contain("TREE_CA002_WPN_BOW_N01"));
            Assert.That(File.ReadAllBytes(_source), Is.EqualTo(originalBytes));
        }

        [TestCase(75)]
        [TestCase(550)]
        [TestCase(600)]
        public void RecoveryProgressDoesNotSilentlyDischargeAssignmentOrChangeHp097(int progress)
        {
            var initial = Read097();
            var assignments = initial.Guild.GuildCity.MemberAssignments.Select(value =>
                value.RecruitId == Aster097 ? value.With(recoveryProgress: progress) : value).ToArray();
            // Explicit unit boundary only; the on-disk earned fixture is never edited.
            var boundary = initial.With(initial.Guild.WithGuildCity(initial.Guild.GuildCity.With(
                memberAssignments: assignments)), initial.OpeningFlow);
            var result = new GuildCityCommandService017D().CompleteMeaningfulOperation(boundary);
            Assert.That(result.IsSuccess, Is.True, string.Join("; ", result.Errors));
            var assignment = result.Value.Guild.GuildCity.MemberAssignments.Single(value => value.RecruitId == Aster097);
            Assert.That(assignment.Kind, Is.EqualTo(GuildMemberAssignmentKind017D.Recovering));
            Assert.That(assignment.RecoveryProgress, Is.GreaterThan(progress));
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(result.Value.Guild.GuildCity, Aster097), Is.False);
            Assert.That(CanonicalJson.Serialize(result.Value.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(boundary.Guild.Recruits)));
        }

        [TestCase(GuildMemberAssignmentKind017D.Recovering)]
        [TestCase(GuildMemberAssignmentKind017D.Training)]
        [TestCase(GuildMemberAssignmentKind017D.Staff)]
        public void RepeatingSameDutyPreservesEarnedProgress156(GuildMemberAssignmentKind017D kind)
        {
            var sourceBytes = File.ReadAllBytes(_source);
            var initial = Read097();
            var service = new GuildCityCommandService017D();
            var facility = kind == GuildMemberAssignmentKind017D.Staff ? "FACILITY_INFIRMARY" : string.Empty;
            var assigned = service.SetAssignment(initial, Aster097, kind, facility);
            Assert.That(assigned.IsSuccess, Is.True, string.Join("; ", assigned.Errors));
            // Native operation-boundary command on the isolated in-memory fixture; no grants or disk mutation.
            var advanced = service.CompleteMeaningfulOperation(assigned.Value);
            Assert.That(advanced.IsSuccess, Is.True, string.Join("; ", advanced.Errors));
            var progress = advanced.Value.Guild.GuildCity.MemberAssignments.Single(value => value.RecruitId == Aster097);
            Assert.That(progress.RecoveryProgress + progress.TrainingProgress + progress.DutyProgress, Is.GreaterThan(0));
            var beforeHash = CanonicalJson.Sha256Hex(advanced.Value);

            var repeated = service.SetAssignment(advanced.Value, Aster097, kind, facility);
            Assert.That(repeated.IsSuccess, Is.True, string.Join("; ", repeated.Errors));
            Assert.That(repeated.Value, Is.SameAs(advanced.Value));
            Assert.That(CanonicalJson.Sha256Hex(repeated.Value), Is.EqualTo(beforeHash));
            Assert.That(CanonicalJson.Serialize(repeated.Value.Guild.Recruits), Is.EqualTo(CanonicalJson.Serialize(initial.Guild.Recruits)));

            // A different duty, or a different staff facility, remains a real assignment change.
            var nextKind = kind == GuildMemberAssignmentKind017D.Staff ? kind : GuildMemberAssignmentKind017D.Reserve;
            var nextFacility = kind == GuildMemberAssignmentKind017D.Staff ? "FACILITY_HERBAL_GARDEN" : string.Empty;
            var changed = service.SetAssignment(repeated.Value, Aster097, nextKind, nextFacility);
            Assert.That(changed.IsSuccess, Is.True, string.Join("; ", changed.Errors));
            var after = changed.Value.Guild.GuildCity.MemberAssignments.Single(value => value.RecruitId == Aster097);
            Assert.That(after.Kind, Is.EqualTo(nextKind));
            Assert.That(after.FacilityId, Is.EqualTo(nextFacility));
            Assert.That(after.RecoveryProgress + after.TrainingProgress + after.DutyProgress, Is.Zero);
            Assert.That(CanonicalJson.Sha256Hex(advanced.Value), Is.EqualTo(beforeHash));
            Assert.That(File.ReadAllBytes(_source), Is.EqualTo(sourceBytes));
            Assert.That(CanonicalJson.Sha256Hex(Read097()), Is.EqualTo(CanonicalJson.Sha256Hex(initial)));
        }

        static IReadOnlyList<BattleUnionState> Project097(CampaignState campaign, M2CombatContent content) =>
            (IReadOnlyList<BattleUnionState>)typeof(M2BattleCommandService).GetMethod("CreatePlayerUnions",
                BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { campaign, content, null, 10 });
    }
}
