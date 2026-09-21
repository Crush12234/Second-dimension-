using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EncounterRosterResolver070Tests
    {
        private EncounterRosterResolver070 _resolver;

        [SetUp]
        public void SetUp()
        {
            _resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
        }

        [Test]
        public void Pass03AuthorityExposesRealEnemyVarietyInsteadOfOneTutorialUnion()
        {
            Assert.That(_resolver.EnemyDefinitionCount, Is.EqualTo(30));
            Assert.That(_resolver.EnemyUnionDefinitionCount, Is.EqualTo(13));
            Assert.That(_resolver.EnemyFamilyCount, Is.EqualTo(15));
            Assert.That(_resolver.EnemyFamilyIds, Does.Contain("ENEMY_FAMILY_GATE_GNAWER"));
            Assert.That(_resolver.EnemyFamilyIds, Does.Contain("ENEMY_FAMILY_ECHO_STALKER"));
            Assert.That(_resolver.EnemyFamilyIds, Does.Contain("ENEMY_FAMILY_HINGE_EATER_COLOSSUS"));
        }

        [Test]
        public void RescueEncounterKeepsGnawerCanonButAddsDistinctRealFamiliesAndSpawnIds()
        {
            var roster = _resolver.Resolve(
                20260828L,
                "CONTRACT_BELL_BENEATH_GATE",
                "BOARD_BELL_BENEATH_GATE_069",
                "ENCOUNTER_GATE_GNAWER_RESCUE",
                4,
                "OPENING_RESCUE_SEED");

            Assert.That(roster.Unions, Has.Count.EqualTo(4));
            Assert.That(roster.Unions.Select(value => value.SourceUnionId),
                Does.Contain("EU_GNAWER_PACK"));
            Assert.That(roster.Unions.Select(value => value.SourceUnionId).Distinct().Count(),
                Is.EqualTo(4));
            Assert.That(roster.Unions.All(value => value.Definition.Id == value.UnionId), Is.True);
            Assert.That(roster.Unions.All(value =>
                value.Definition.MemberIds.Contains(value.LeaderMemberId)), Is.True);
            Assert.That(roster.FamilyIds.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(roster.FamilyIds.Any(value => value != "ENEMY_FAMILY_GATE_GNAWER"), Is.True);

            var members = roster.Unions.SelectMany(value => value.Members).ToArray();
            Assert.That(members.Select(value => value.MemberId).Distinct().Count(), Is.EqualTo(members.Length),
                "Battle-local member IDs must stay unique even where authored Unions share support enemies.");
            Assert.That(members.All(value => value.Definition.Id == value.SourceEnemyId), Is.True);
            Assert.That(members.All(value => !string.IsNullOrWhiteSpace(value.Definition.Name)), Is.True);
        }

        [Test]
        public void ExactEncounterContextReopensWithoutReroll()
        {
            var request = Request("ENCOUNTER_ROUTE_RESCUE", 5, "COMMITTED_ROUTE_SEED");
            var first = _resolver.Resolve(712003L, request);
            var reopened = _resolver.Resolve(712003L, request);

            Assert.That(CanonicalJson.Sha256Hex(reopened), Is.EqualTo(CanonicalJson.Sha256Hex(first)));
            Assert.That(reopened.RosterId, Is.EqualTo(first.RosterId));
        }

        [Test]
        public void CampaignSeedChangesInvestigationOppositionAcrossAvailableUnions()
        {
            var firstUnions = new HashSet<string>();
            for (var seed = 1L; seed <= 64L; seed++)
            {
                var roster = _resolver.Resolve(
                    seed,
                    "CONTRACT_LINES_NOT_RETURNED",
                    "BOARD_LINES_NOT_RETURNED",
                    "ENCOUNTER_MISSING_SURVEY_TEAM",
                    1,
                    "SURVEY_ROUTE");
                firstUnions.Add(roster.Unions[0].SourceUnionId);
            }

            Assert.That(firstUnions.Count, Is.GreaterThanOrEqualTo(3),
                "Different committed campaigns must draw from more than the Gate Gnawer tutorial Union.");
            Assert.That(firstUnions.All(value => value != "EU_GNAWER_PACK"), Is.True,
                "Investigation affinity should resolve to investigation opposition, not the tutorial pack.");
        }

        [Test]
        public void BossContextSelectsTheAuthoredHingeEaterUnion()
        {
            var roster = _resolver.Resolve(
                9911L,
                "CONTRACT_GATEHEART_BREACH",
                "BOARD_GATEHEART_BREACH",
                "ENCOUNTER_HINGE_EATER_COLOSSUS_BOSS",
                1,
                "BOSS_COMMITMENT");

            Assert.That(roster.Unions[0].SourceUnionId, Is.EqualTo("EU_HINGE_EATER"));
            Assert.That(roster.FamilyIds, Does.Contain("ENEMY_FAMILY_HINGE_EATER_COLOSSUS"));
        }

        private static EncounterLaunchRequest017D Request(
            string encounterId,
            int unionCount,
            string seedIdentity)
        {
            return new EncounterLaunchRequest017D(
                "REQ070_TEST",
                "CONTRACT_RELIEF_ROAD",
                "EXP070_TEST",
                "BOARD_RELIEF_ROAD",
                "N06",
                encounterId,
                "BATTLE070_TEST",
                "Protect the relief road.",
                unionCount,
                seedIdentity,
                new[] { "UNION_TEST" },
                new string[0],
                new[] { "OBJECTIVE_TEST" },
                new[] { "ROAD_ESCORT" },
                10,
                0,
                1,
                "RETURN070_TEST",
                "PREBATTLE070_TEST_HASH");
        }
    }
}
