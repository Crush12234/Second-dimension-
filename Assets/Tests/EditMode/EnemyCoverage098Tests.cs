using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyCoverage098Tests
    {
        const string Hash = "0123456789ABCDEF01234567";

        [TestCase(1, 1, 1)]
        [TestCase(10, 10, 10)]
        [TestCase(11, 1, 2)]
        [TestCase(13, 3, 4)]
        [TestCase(50, 10, 4)]
        [TestCase(100, 10, 9)]
        [TestCase(101, 1, 1)]
        [TestCase(int.MaxValue, 7, 1)]
        public void NewCommittedFloorRotatesPresentationIndependentlyOfNumericThreat098(int actual, int template, int variant)
        {
            Assert.That(TowerEnemyArtCycle098.TryResolve098(NewId(actual), out var floor,
                out var content, out var color), Is.True);
            Assert.That(floor, Is.EqualTo(actual));
            Assert.That(content, Is.EqualTo(template));
            Assert.That(color, Is.EqualTo(variant));
        }

        [TestCase("ABYSS_BATTLE022_FLOOR_03_ACTUAL098_0_0123456789ABCDEF01234567")]
        [TestCase("ABYSS_BATTLE022_FLOOR_03_ACTUAL098_013_0123456789ABCDEF01234567")]
        [TestCase("ABYSS_BATTLE022_FLOOR_02_ACTUAL098_13_0123456789ABCDEF01234567")]
        [TestCase("ABYSS_BATTLE022_FLOOR_03_ACTUAL098_2147483648_0123456789ABCDEF01234567")]
        [TestCase("ABYSS_BATTLE022_FLOOR_03_ACTUAL098_13_NOT_A_REQUEST_HASH")]
        [TestCase("prefix_ABYSS_BATTLE022_FLOOR_03_ACTUAL098_13_0123456789ABCDEF01234567")]
        [TestCase("ABYSS_BATTLE022_FLOOR_03_ACTUAL098_13_0123456789ABCDEF01234567\n")]
        public void MalformedNewReservedIdentityFailsClosedInsteadOfSilentlyBecomingLegacy098(string battleId)
        {
            Assert.That(TowerEnemyArtCycle098.TryResolve098(battleId, out _, out _, out _), Is.False);
            Assert.Throws<ArgumentException>(() => EnemyArtIdentity090.Resolve090(
                battleId, 0, 0, Member(), out _, out _, out _));
        }

        [Test]
        public void LegacyTemplateBindingsRemainByteExactForAllSeventyBaseSlots098()
        {
            for (var template = 1; template <= 10; template++)
            for (var slot = 0; slot < 7; slot++)
            {
                var legacyId = "ABYSS_BATTLE022_FLOOR_" + template.ToString("00") + "_" + Hash;
                Assert.That(TowerEnemyArtCycle098.TryResolve098(legacyId, out _, out _, out _), Is.False);
                EnemyArtIdentity090.Resolve090(legacyId, slot / 3, slot % 3, Member(),
                    out var baseId, out var variantId, out var seed);
                var expectedBase = (template - 1) * 7 + slot + 1;
                Assert.That(baseId, Is.EqualTo("ENEMY_REC_" + expectedBase.ToString("000")));
                Assert.That(variantId, Is.EqualTo(baseId + "_VAR_" + template.ToString("00")));
                Assert.That(seed, Is.EqualTo(template));
            }
        }

        [Test]
        public void OneHundredFloorAddressabilityFixtureRoutesAllSevenHundredActualCatalogPairs098()
        {
            // This fixture places seven legal index slots at each floor. It proves
            // selector reachability, not 100 victories or that early floors spawn seven.
            var variants = new HashSet<string>(StringComparer.Ordinal);
            for (var floor = 1; floor <= 100; floor++)
            for (var slot = 0; slot < 7; slot++)
            {
                EnemyArtIdentity090.Resolve090(NewId(floor), slot / 3, slot % 3, Member(),
                    out var baseId, out var variantId, out _);
                Assert.That(EnemyArt700Runtime090.TryResolveVariant090(baseId, variantId,
                    out var actual, out var error), Is.True, error);
                Assert.That(actual.variantId, Is.EqualTo(variantId));
                Assert.That(actual.baseEnemyId, Is.EqualTo(baseId));
                Assert.That(actual.runtimeStatRulesProvided, Is.False,
                    "A Frost/Venom/etc asset label must never supply combat rules.");
                variants.Add(actual.variantId);
            }
            Assert.That(variants.Count, Is.EqualTo(700));
            Assert.That(variants, Is.EquivalentTo(EnemyArt700Runtime090.Catalog090.variants
                .Select(value => value.variantId)));
        }

        [Test]
        public void ActualLaterTowerSpawnCountsExposeAllSevenHundredCatalogPairs098()
        {
            // Real098 spawn counts and the shipping three-member Tower Union
            // shape, not injected victories or a claim of 100 completed floors.
            var variants = new HashSet<string>(StringComparer.Ordinal);
            var routedMembers = 0;
            for (var floor = 101; floor <= 200; floor++)
            {
                var unionCount = TowerThreatRules098.EnemyUnionCount098(floor);
                Assert.That(unionCount, Is.InRange(3, 10), "Later floors must expose the third Union's seventh base slot.");
                var battleId = TowerThreatRules098.BattleId098(floor, Hash);
                for (var unionIndex = 0; unionIndex < unionCount; unionIndex++)
                for (var memberIndex = 0; memberIndex < 3; memberIndex++)
                {
                    EnemyArtIdentity090.Resolve090(battleId, unionIndex, memberIndex, Member(),
                        out var baseId, out var variantId, out _);
                    Assert.That(EnemyArt700Runtime090.TryResolveVariant090(baseId, variantId,
                        out var actual, out var error), Is.True, error);
                    Assert.That(actual.baseEnemyId, Is.EqualTo(baseId));
                    Assert.That(actual.variantId, Is.EqualTo(variantId));
                    Assert.That(actual.runtimeStatRulesProvided, Is.False);
                    variants.Add(actual.variantId);
                    routedMembers++;
                }
            }
            Assert.That(routedMembers, Is.GreaterThanOrEqualTo(900));
            Assert.That(variants.Count, Is.EqualTo(700));
            Assert.That(variants, Is.EquivalentTo(EnemyArt700Runtime090.Catalog090.variants
                .Select(value => value.variantId)));
        }

        [Test]
        public void MissingObservedBase021IsReachableAtTheThirdUnionOfTemplateThree098()
        {
            EnemyArtIdentity090.Resolve090("ABYSS_BATTLE022_FLOOR_03_" + Hash,
                2, 0, Member(), out var legacyBase, out var legacyVariant, out _);
            Assert.That(legacyBase, Is.EqualTo("ENEMY_REC_021"));
            Assert.That(legacyVariant, Is.EqualTo("ENEMY_REC_021_VAR_03"));
            EnemyArtIdentity090.Resolve090(NewId(13), 2, 0, Member(),
                out var nextBase, out var nextVariant, out _);
            Assert.That(nextBase, Is.EqualTo(legacyBase));
            Assert.That(nextVariant, Is.EqualTo("ENEMY_REC_021_VAR_04"));
        }

        [Test]
        public void VisualCycleCopiesPreserveAllCombatFieldsAndSavedPairIdentity098()
        {
            var original = Member();
            var union = new BattleUnionState("ENEMY_UNION098", "Identity-only fixture", BattleSide.Enemy,
                original.MemberId, new[] { original }, "FORMATION", "Formation", true, string.Empty,
                12, 12, 80, 10000, EngagementState.Open, false, false, 0);
            var result = EnemyArtIdentity090.CommitIdentities090(new[] { union }, NewId(13))[0];
            var member = result.Members[0];
            Assert.That(CanonicalJson.Serialize(member.WithEnemyArt090(null, null, 0)),
                Is.EqualTo(CanonicalJson.Serialize(original)),
                "Only three explicit art fields may differ: no HP/MP/Arts/equipment/AI edits.");
            var reload = JsonConvert.DeserializeObject<BattleUnionState>(JsonConvert.SerializeObject(result));
            Assert.That(CanonicalJson.Serialize(reload), Is.EqualTo(CanonicalJson.Serialize(result)));
            var damaged = reload.Members[0].With(currentHp: 5);
            Assert.That(damaged.EnemyArtBaseId090, Is.EqualTo(member.EnemyArtBaseId090));
            Assert.That(damaged.EnemyArtVariantId090, Is.EqualTo(member.EnemyArtVariantId090));
            Assert.That(damaged.VisualVariantSeed090, Is.EqualTo(member.VisualVariantSeed090));
        }

        static string NewId(int actual) => "ABYSS_BATTLE022_FLOOR_" +
            ((actual - 1) % 10 + 1).ToString("00", CultureInfo.InvariantCulture) +
            "_ACTUAL098_" + actual.ToString(CultureInfo.InvariantCulture) + "_" + Hash;

        static BattleMemberState Member() => new BattleMemberState(
            "ENEMY_GATE_GNAWER_01", "Enemy identity fixture", "ENEMY_GATE_GNAWER_01",
            120, 120, 20, 20, 18, 14, new[] { "ENEMY_FAMILY_GATE_GNAWER" },
            false, false, false, new[] { "ART_QUICK_CUT" }, 0, 0, string.Empty,
            artProgress: null, equippedMainHandInstanceId: null,
            enemyArtBaseId090: null, enemyArtVariantId090: null, visualVariantSeed090: 0);
    }
}
