using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    // Live shipping-rig binding/lease tests. A pass is NOT artistic acceptance:
    // base021 currently has recorded source-pixel crop defects needing repair.
    public sealed class EnemyCoverage098RenderTests
    {
        [SetUp] public void SetUp() => EnemyArt700Runtime090.ResetForTests090();
        [TearDown] public void TearDown() => EnemyArt700Runtime090.ResetForTests090();

        [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
        [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)]
        public void Base021AllTenVariantsUseShippingEnemyRigWithoutFallback098(int variant)
        {
            VerifyRig("ENEMY_REC_021", "ENEMY_REC_021_VAR_" + variant.ToString("00"));
        }

        [TestCase("ENEMY_REC_001", "ENEMY_REC_001_VAR_01")]
        [TestCase("ENEMY_REC_035", "ENEMY_REC_035_VAR_05")]
        [TestCase("ENEMY_REC_070", "ENEMY_REC_070_VAR_10")]
        public void RepresentativeOtherFamiliesRetainExactArtThroughPoseChanges098(string baseId, string variantId)
        {
            VerifyRig(baseId, variantId);
        }

        [Test, Explicit("Loads 1,380 archived battle PNGs plus one corrected paired Resource with ten exact material themes through 700 shipping rigs. One repaired body, not ten drawings; binding evidence, not natural encounters or finished-art approval.")]
        [Category("EnemyArt700ActualRigQA")]
        public void EveryCatalogPairCanBindToShippingRigAndReleaseLeases098()
        {
            var variants = EnemyArt700Runtime090.Catalog090.variants
                .OrderBy(value => value.variantId, StringComparer.Ordinal).ToArray();
            Assert.That(variants.Length, Is.EqualTo(700));
            var completed = 0;
            foreach (var variant in variants)
            {
                VerifyRig(variant.baseEnemyId, variant.variantId);
                completed++;
                if (completed % 50 == 0)
                    TestContext.Progress.WriteLine("Actual enemy rig bindings completed " + completed + "/700.");
            }
            Assert.That(completed, Is.EqualTo(700));
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().OutstandingLeaseCount, Is.Zero);
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().CachedSpriteCount,
                Is.LessThanOrEqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090));
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().ResidentSpriteCount,
                Is.LessThanOrEqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090 + EnemyArtRemaster098.MaximumResidentSprites098),
                "The separately owned single pair adds at most two resident sprites to the 96-entry raw LRU.");
        }

        static void VerifyRig(string baseId, string variantId)
        {
            var host = new GameObject("Actual enemy pose binding QA098", typeof(RectTransform));
            host.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 700);
            var variant = EnemyArt700Runtime090.Catalog090.variants.Single(value => value.variantId == variantId);
            var member = new M2BattleMemberView {
                MemberId = "ENEMY_RENDER_FIXTURE098_" + variantId,
                DisplayName = variant.baseDisplayName, CurrentHp = 100, MaximumHp = 100,
                EnemyArtBaseId090 = baseId, EnemyArtVariantId090 = variantId
            };
            var union = new M2BattleUnionView {
                UnionId = "ENEMY_BINDING098", DisplayName = "Enemy binding fixture", Side = "Enemy",
                Members = new[] { member }, CanAct = true
            };
            M2BattleActorRig072 actor = null;
            try
            {
                actor = new M2BattleActorRig072(host.GetComponent<RectTransform>(), union, member, true);
                Sprite firstIdle = null;
                foreach (var action in new[] { false, true, false })
                {
                    Assert.That(actor.SetPoseImmediate(action ? BattleArtPoseDirector011.ActionPrimary :
                        BattleArtPoseDirector011.Idle), Is.True, variantId);
                    var pose = action ? EnemyArt700Pose090.Attack : EnemyArt700Pose090.Idle;
                    Assert.That(EnemyArt700Runtime090.TryLoadSprite090(baseId, variantId, pose,
                        out var expected, out var error), Is.True, error);
                    Assert.That(actor.CurrentArtwork076.sprite, Is.SameAs(expected), variantId);
                    var expectedPath = EnemyArtRemaster098.Handles098(baseId, variantId)
                        ? EnemyArtRemaster098.ResourcePath098 + (action ? "#ACTION" : "#IDLE")
                        : "EnemyArt700/" + variantId + (action ? "/ATTACK" : "/IDLE");
                    Assert.That(actor.CurrentResourcePath, Is.EqualTo(expectedPath),
                        "A fallback or archived path must never masquerade as the actual remaster source.");
                    Assert.That(actor.CurrentArtwork076.preserveAspect, Is.True);
                    Assert.That(actor.EnemyArtworkShaderName075, Is.EqualTo(M2BattleActorRig072.EnemyCutoutShaderName075));
                    Assert.That(expected.texture.isReadable, Is.False, "Runtime CPU pixels must be released after framing.");
                    Assert.That(expected.rect.width, Is.GreaterThan(0));
                    Assert.That(expected.rect.height, Is.GreaterThan(0));
                    if (action) Assert.That(expected, Is.Not.SameAs(firstIdle));
                    else if (firstIdle == null) firstIdle = expected;
                    else Assert.That(expected, Is.SameAs(firstIdle), "Idle/action/idle must reuse the same owned source.");
                    Assert.That(EnemyArt700Runtime090.GetDiagnostics090().OutstandingLeaseCount, Is.GreaterThan(0));
                }
            }
            finally
            {
                actor?.Dispose();
                Object.DestroyImmediate(host);
            }
            Assert.That(EnemyArt700Runtime090.GetDiagnostics090().OutstandingLeaseCount, Is.Zero,
                variantId + " left an owner lease behind.");
        }
    }
}
