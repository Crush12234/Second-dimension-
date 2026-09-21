using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignReplayThreat130Tests
    {
        [Test]
        public void CycleOneKeepsExactRoutesUnionAndMemberObjects130()
        {
            var routes = new[] { "SCOUTED_APPROACH", null, "CITY_ENEMY_COHESION_DAMAGE_7" };
            Assert.That(CampaignReplayThreat130.FormatTag130(1, 25), Is.Null);
            Assert.That(CampaignReplayThreat130.AppendToRoutes(routes, 1, 25), Is.SameAs(routes));
            Assert.That(CampaignReplayThreat130.ParseCommittedRoutes(routes), Is.Null);
            Assert.That(CampaignReplayThreat130.ParseCommittedRoutes(null), Is.Null);
            var original = Union130();
            var before = CanonicalJson.Serialize(original);
            var unchanged = CampaignReplayThreat130.ApplyToEnemyUnion(original, null);
            Assert.That(unchanged, Is.SameAs(original));
            for (var index = 0; index < original.Members.Count; index++)
                Assert.That(unchanged.Members[index], Is.SameAs(original.Members[index]));
            Assert.That(CanonicalJson.Serialize(original), Is.EqualTo(before));
        }

        [TestCase(2, 25, 127, 9, 17)]
        [TestCase(3, 25, 159, 12, 22)]
        [TestCase(4, 25, 199, 15, 28)]
        [TestCase(2, 50, 152, 11, 20)]
        [TestCase(3, 50, 228, 17, 30)]
        [TestCase(4, 50, 342, 26, 45)]
        [TestCase(3, 100, 404, 28, 52)]
        public void EachCompletedCycleUsesPreviousIntegerValueWithCeiling130(
            int cycle, int growth, int hp, int attack, int magic)
        {
            var original = Union130();
            var actual = CampaignReplayThreat130.ApplyToEnemyUnion(original, Profile130(cycle, growth));
            Assert.That(actual.Members[0].MaximumHp, Is.EqualTo(hp));
            Assert.That(actual.Members[0].CurrentHp, Is.EqualTo(hp));
            Assert.That(actual.Members[0].Attack, Is.EqualTo(attack));
            Assert.That(actual.Members[0].MagicAttack, Is.EqualTo(magic));
            Assert.That(actual.Members[1].CurrentHp,
                Is.EqualTo((int)((long)original.Members[1].CurrentHp * hp / 101)));
            Assert.That(actual.Members[2].CurrentHp, Is.Zero);
        }

        [Test]
        public void ScalingChangesOnlyHpAndOffenseAndLeavesSourceUntouched130()
        {
            var original = Union130();
            var before = CanonicalJson.Serialize(original);
            var actual = CampaignReplayThreat130.ApplyToEnemyUnion(original, Profile130(5, 25));
            Assert.That(actual, Is.Not.SameAs(original));
            Assert.That(actual.Members.Count, Is.EqualTo(3));
            Assert.That(JToken.DeepEquals(WithoutHpAndOffense130(original),
                WithoutHpAndOffense130(actual)), Is.True,
                "All IDs, art/gear/visual bindings, MP, AP, formation, injury flags and ordering must carry unchanged.");
            Assert.That(CanonicalJson.Serialize(original), Is.EqualTo(before));
            Assert.That(actual.Members[2].Downed, Is.True);
            Assert.That(actual.Members[2].Stabilized, Is.True);
            Assert.That(actual.Members[0].Guarding, Is.True);
        }

        [Test]
        public void FrozenEncounterPolicyDoesNotReadLaterMutableRouteOrCampaignValues130()
        {
            var routes = new List<string> { "SCOUTED_APPROACH", "CAMPAIGN_REPLAY130_V1_C3_P25" };
            var frozen = CampaignReplayThreat130.ParseCommittedRoutes(routes);
            routes[1] = "CAMPAIGN_REPLAY130_V1_C6_P100";
            Assert.That(frozen.Cycle, Is.EqualTo(3));
            Assert.That(frozen.GrowthPercent, Is.EqualTo(25));
            var original = Union130();
            var saved = CampaignReplayThreat130.ApplyToEnemyUnion(original, frozen);
            var later = CampaignReplayThreat130.ApplyToEnemyUnion(original,
                CampaignReplayThreat130.ParseCommittedRoutes(routes));
            Assert.That(saved.Members[0].MaximumHp, Is.EqualTo(159));
            Assert.That(later.Members[0].MaximumHp, Is.EqualTo(3232));
            Assert.That(CanonicalJson.Serialize(CampaignReplayThreat130.ApplyToEnemyUnion(original, frozen)),
                Is.EqualTo(CanonicalJson.Serialize(saved)));
        }

        [Test]
        public void AppendingPreservesOtherRoutesAndCannotReplaceCommittedPolicy130()
        {
            var routes = new[] { "SCOUTED_APPROACH", null, "CITY_ENEMY_COHESION_DAMAGE_7" };
            var result = CampaignReplayThreat130.AppendToRoutes(routes, 2, 25);
            Assert.That(result, Is.EqualTo(new[] { routes[0], routes[1], routes[2],
                "CAMPAIGN_REPLAY130_V1_C2_P25" }));
            Assert.That(routes.Length, Is.EqualTo(3));
            Assert.That(((IList<string>)result).IsReadOnly, Is.True);
            Assert.That(CampaignReplayThreat130.AppendToRoutes(result, 2, 25), Is.SameAs(result));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(result, 3, 25));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(result, 2, 50));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(result, 1, 25));
        }

        [TestCase("CAMPAIGN_REPLAY130_V1_C1_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C0_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C-2_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C02_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2147483648_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P0")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P26")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P025")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P25\n")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P25_EXTRA")]
        [TestCase("CAMPAIGN_REPLAY130_V2_C2_P25")]
        [TestCase("campaign_replay130_V1_C2_P25")]
        [TestCase(" CAMPAIGN_REPLAY130_V1_C2_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C\u0662_P25")]
        [TestCase("CAMPAIGN_REPLAY130")]
        public void MalformedReservedTagFailsClosedAndCannotBeSilentlyAppendedOver130(string tag)
        {
            var routes = new[] { tag };
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.ParseCommittedRoutes(routes));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(routes, 2, 25));
        }

        [TestCase("CAMPAIGN_REPLAY130_V1_C2_P25")]
        [TestCase("CAMPAIGN_REPLAY130_V1_C3_P50")]
        public void DuplicateOrConflictingTagsCannotMultiplyThreatTwice130(string second)
        {
            var routes = new[] { "CAMPAIGN_REPLAY130_V1_C2_P25", second };
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.ParseCommittedRoutes(routes));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(routes, 2, 25));
        }

        [TestCase("TOWER_ACTUAL_FLOOR098_301")]
        [TestCase("TOWER_THREAT_TIER_10")]
        [TestCase("TOWER_ACTUAL_FLOOR098_invalid")]
        public void TowerAndReplayCannotStackOrSilentlyChooseOnePolicy130(string tower)
        {
            var towerOnly = new[] { tower, "SCOUTED_APPROACH" };
            Assert.That(CampaignReplayThreat130.ParseCommittedRoutes(towerOnly), Is.Null);
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.AppendToRoutes(towerOnly, 2, 25));
            Assert.Throws<InvalidOperationException>(() => CampaignReplayThreat130.ParseCommittedRoutes(
                new[] { tower, "CAMPAIGN_REPLAY130_V1_C2_P25" }));
        }

        [TestCase(0, 25)]
        [TestCase(-1, 25)]
        [TestCase(2, 0)]
        [TestCase(2, 26)]
        [TestCase(2, 101)]
        public void UnsupportedPolicyCannotBeFormattedOrCommitted130(int cycle, int growth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CampaignReplayThreat130.FormatTag130(cycle, growth));
            Assert.Throws<ArgumentOutOfRangeException>(() => CampaignReplayThreat130.AppendToRoutes(null, cycle, growth));
        }

        [TestCase(25)]
        [TestCase(50)]
        [TestCase(100)]
        public void MaximumCycleSaturatesWithoutOverflowOrUnboundedCycleWalk130(int growth)
        {
            var profile = Profile130(int.MaxValue, growth);
            Assert.That(profile.Cycle, Is.EqualTo(int.MaxValue));
            var actual = CampaignReplayThreat130.ApplyToEnemyUnion(Union130(1, 1, 1), profile);
            Assert.That(actual.Members[0].MaximumHp, Is.EqualTo(1000000));
            Assert.That(actual.Members[0].Attack, Is.EqualTo(10000));
            Assert.That(actual.Members[0].MagicAttack, Is.EqualTo(10000));
        }

        [Test]
        public void ExistingValuesAboveCapsAreNeverNerfedAndPlayerUnionsCannotBeScaled130()
        {
            var above = Union130(int.MaxValue, int.MaxValue, 10001);
            var result = CampaignReplayThreat130.ApplyToEnemyUnion(above, Profile130(int.MaxValue, 100));
            Assert.That(CanonicalJson.Serialize(result), Is.EqualTo(CanonicalJson.Serialize(above)));
            var player = Union130(side: BattleSide.Player);
            Assert.That(CampaignReplayThreat130.ApplyToEnemyUnion(player, null), Is.SameAs(player));
            Assert.Throws<InvalidOperationException>(() =>
                CampaignReplayThreat130.ApplyToEnemyUnion(player, Profile130(2, 25)));
        }

        static CampaignReplayThreatProfile130 Profile130(int cycle, int growth) =>
            CampaignReplayThreat130.ParseCommittedRoutes(new[] { CampaignReplayThreat130.FormatTag130(cycle, growth) });

        static JObject WithoutHpAndOffense130(BattleUnionState union)
        {
            var token = JObject.FromObject(union);
            foreach (var member in token["Members"])
                foreach (var field in new[] { "CurrentHp", "MaximumHp", "Attack", "MagicAttack" })
                    ((JObject)member).Remove(field);
            return token;
        }

        static BattleUnionState Union130(int hp = 101, int attack = 7, int magic = 13,
            BattleSide side = BattleSide.Enemy)
        {
            var members = new List<BattleMemberState>();
            for (var index = 0; index < 3; index++)
                members.Add(new BattleMemberState("ENEMY_MEMBER_" + index, "Existing enemy " + index,
                    "CLASS_EXISTING", index == 0 ? hp : index == 1 ? Math.Max(1, hp / 2) : 0,
                    hp, 13, 37, attack, magic, new[] { "SWORD", "HEAVY" }, index == 2,
                    index == 2, index == 0, new[] { "ART_EXISTING_A", "ART_EXISTING_B" },
                    23, 42, "ART_EXISTING_B", new[] { new BattleArtProgressState("ART_EXISTING_A", "MARTIAL", 5, 17) },
                    "ITEM_EXISTING_" + index, "ENEMY_ART_01", "ENEMY_ART_01_C", 72 + index));
            return new BattleUnionState("ENEMY_UNION", "Existing formation", side, members[1].MemberId,
                members.AsReadOnly(), "FORMATION_EXISTING", "Existing formation name", true,
                "Existing reason", 27, 40, 81, 7300, EngagementState.Flanking, true, false, 91);
        }
    }
}
