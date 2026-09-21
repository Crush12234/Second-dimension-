using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyArtIdentity090Tests
    {
        [Test]
        public void CatalogAddressSpaceContainsExactlySeventyByTenStableIds()
        {
            var ids = new HashSet<string>();
            for (var baseIndex = 1; baseIndex <= EnemyArtIdentity090.BaseFamilyCount; baseIndex++)
            for (var variantIndex = 1; variantIndex <= EnemyArtIdentity090.VariantsPerBase; variantIndex++)
                Assert.That(ids.Add(EnemyArtIdentity090.VariantId090(baseIndex, variantIndex)),
                    Is.True);

            Assert.That(ids.Count, Is.EqualTo(700));
            Assert.That(ids, Does.Contain("ENEMY_REC_001_VAR_01"));
            Assert.That(ids, Does.Contain("ENEMY_REC_070_VAR_10"));
        }

        [Test]
        public void TowerUsesSevenFamilyBucketAndThreatVariantForEachFloor()
        {
            var member = Member("ENEMY_FORMATION_NUISANCE", "ENEMY_GATE_GNAWER_01");
            EnemyArtIdentity090.Resolve090(
                "ABYSS_BATTLE022_FLOOR_10_A1B2C3", 2, 0, member,
                out var firstBase, out var firstVariant, out var firstSeed);
            EnemyArtIdentity090.Resolve090(
                "ABYSS_BATTLE022_FLOOR_10_A1B2C3", 4, 0, member,
                out var wrappedBase, out var wrappedVariant, out var wrappedSeed);

            Assert.That(firstBase, Is.EqualTo("ENEMY_REC_070"));
            Assert.That(firstVariant, Is.EqualTo("ENEMY_REC_070_VAR_10"));
            Assert.That(firstSeed, Is.EqualTo(10));
            Assert.That(wrappedBase, Is.EqualTo("ENEMY_REC_069"));
            Assert.That(wrappedVariant, Is.EqualTo("ENEMY_REC_069_VAR_10"));
            Assert.That(wrappedSeed, Is.EqualTo(10));
        }

        [Test]
        public void CampaignFamilyUsesExplicitSilhouetteMapAndAuthoredRank()
        {
            var hound = Member(
                "ENEMY_RUSTBACK_HOUND_03",
                "ENEMY_RUSTBACK_HOUND_03_SPAWN070_ABC",
                new[] { "ENEMY_FAMILY_RUSTBACK_HOUND" },
                3);
            EnemyArtIdentity090.Resolve090(
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH", 0, 0, hound,
                out var baseId, out var variantId, out var seed);

            Assert.That(baseId, Is.EqualTo("ENEMY_REC_001"));
            Assert.That(variantId, Is.EqualTo("ENEMY_REC_001_VAR_03"));
            Assert.That(seed, Is.EqualTo(3));
        }

        [Test]
        public void CommitChangesOnlyVisualIdentityFields()
        {
            var member = Member(
                "ENEMY_HINGE_EATER_COLOSSUS_01",
                "ENEMY_HINGE_EATER_COLOSSUS_01_SPAWN070_BOSS",
                new[] { "ENEMY_FAMILY_HINGE_EATER_COLOSSUS", "WEAPON" },
                1);
            var union = new BattleUnionState(
                "ENEMY_UNION", "Boss Union", BattleSide.Enemy, member.MemberId,
                new[] { member }, "FORMATION", "Formation", true, string.Empty,
                12, 12, 80, 10000, EngagementState.Open, false, false, 0);

            var committed = EnemyArtIdentity090.CommitIdentities090(
                new[] { union }, "ENCOUNTER071_GATE_EATER")[0].Members[0];

            Assert.That(committed.EnemyArtBaseId090, Is.EqualTo("ENEMY_REC_053"));
            Assert.That(committed.EnemyArtVariantId090, Is.EqualTo("ENEMY_REC_053_VAR_01"));
            Assert.That(committed.MemberId, Is.EqualTo(member.MemberId));
            Assert.That(committed.ClassId, Is.EqualTo(member.ClassId));
            Assert.That(committed.CurrentHp, Is.EqualTo(member.CurrentHp));
            Assert.That(committed.MaximumHp, Is.EqualTo(member.MaximumHp));
            Assert.That(committed.Attack, Is.EqualTo(member.Attack));
            Assert.That(committed.MagicAttack, Is.EqualTo(member.MagicAttack));
            Assert.That(committed.LearnedArtIds, Is.EqualTo(member.LearnedArtIds));
            Assert.That(committed.EquipmentTags, Is.EqualTo(member.EquipmentTags));
        }

        [Test]
        public void ExplicitEnemyArtIdentitySurvivesJsonAndMemberCopies()
        {
            var original = Member(
                    "ENEMY_RIFT_MOLD_CREEPER_02",
                    "ENEMY_RIFT_MOLD_CREEPER_02_SPAWN070_SAVE",
                    new[] { "ENEMY_FAMILY_RIFT_MOLD_CREEPER" },
                    2)
                .WithEnemyArt090("ENEMY_REC_024", "ENEMY_REC_024_VAR_02", 2);

            var reopened = JsonConvert.DeserializeObject<BattleMemberState>(
                JsonConvert.SerializeObject(original));
            var damaged = reopened.With(currentHp: reopened.CurrentHp - 1);

            Assert.That(reopened.EnemyArtBaseId090, Is.EqualTo("ENEMY_REC_024"));
            Assert.That(reopened.EnemyArtVariantId090, Is.EqualTo("ENEMY_REC_024_VAR_02"));
            Assert.That(reopened.VisualVariantSeed090, Is.EqualTo(2));
            Assert.That(damaged.EnemyArtBaseId090, Is.EqualTo(reopened.EnemyArtBaseId090));
            Assert.That(damaged.EnemyArtVariantId090, Is.EqualTo(reopened.EnemyArtVariantId090));
            Assert.That(damaged.VisualVariantSeed090, Is.EqualTo(reopened.VisualVariantSeed090));
        }

        private static BattleMemberState Member(
            string classId,
            string memberId,
            IReadOnlyList<string> equipmentTags = null,
            int visualSeed = 0) =>
            new BattleMemberState(
                memberId, memberId, classId, 120, 120, 20, 20,
                18, 14, equipmentTags ?? new string[0], false, false, false,
                new[] { "ART_QUICK_CUT" }, 0, 0, string.Empty,
                artProgress: null,
                equippedMainHandInstanceId: null,
                enemyArtBaseId090: null,
                enemyArtVariantId090: null,
                visualVariantSeed090: visualSeed);
    }
}
