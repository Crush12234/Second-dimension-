using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class EnemyArt700BattlePresentation090PlayModeTests
    {
        [SetUp]
        public void SetUp090()
        {
            EnemyArt700Runtime090.ResetForTests090();
        }

        [UnityTearDown]
        public IEnumerator TearDown090()
        {
            yield return null;
            EnemyArt700Runtime090.ResetForTests090();
        }

        [UnityTest]
        public IEnumerator CertifiedActorRigUsesPairedIdleAttackIdleAndCutoutShader090()
        {
            var hostObject090 = new GameObject(
                "Enemy Art 700 Pose Lifecycle Host 090",
                typeof(RectTransform));
            var host090 = hostObject090.GetComponent<RectTransform>();
            var union090 = new M2BattleUnionView
            {
                UnionId = "ENEMY_ART_700_UNION_090",
                DisplayName = "Recovered Enemy Union"
            };
            var member090 = new M2BattleMemberView
            {
                MemberId = "ENEMY_ART_700_MEMBER_090",
                DisplayName = "Recovered Enemy",
                CurrentHp = 300,
                MaximumHp = 300,
                EnemyArtBaseId090 = "ENEMY_REC_035",
                EnemyArtVariantId090 = "ENEMY_REC_035_VAR_05"
            };
            M2BattleActorRig072 actor090 = null;
            try
            {
                actor090 = new M2BattleActorRig072(
                    host090,
                    union090,
                    member090,
                    true);
                yield return null;

                Assert.That(actor090.CurrentPoseId,
                    Is.EqualTo(BattleArtPoseDirector011.Idle));
                Assert.That(actor090.CurrentResourcePath,
                    Is.EqualTo("EnemyArt700/ENEMY_REC_035_VAR_05/IDLE"));
                Assert.That(actor090.CurrentArtwork076.sprite, Is.Not.Null);
                Assert.That(actor090.EnemyArtworkShaderName075,
                    Is.EqualTo(M2BattleActorRig072.EnemyCutoutShaderName075));
                var firstIdle090 = actor090.CurrentArtwork076.sprite;

                yield return actor090.CrossfadeToPose(
                    BattleArtPoseDirector011.ActionPrimary,
                    0.01f,
                    () => 20f,
                    () => false);
                Assert.That(actor090.CurrentPoseId,
                    Is.EqualTo(BattleArtPoseDirector011.ActionPrimary));
                Assert.That(actor090.CurrentResourcePath,
                    Is.EqualTo("EnemyArt700/ENEMY_REC_035_VAR_05/ATTACK"));
                Assert.That(actor090.CurrentArtwork076.sprite,
                    Is.Not.Null.And.Not.SameAs(firstIdle090));

                yield return actor090.CrossfadeToPose(
                    BattleArtPoseDirector011.Idle,
                    0.01f,
                    () => 20f,
                    () => false);
                Assert.That(actor090.CurrentPoseId,
                    Is.EqualTo(BattleArtPoseDirector011.Idle));
                Assert.That(actor090.CurrentResourcePath,
                    Is.EqualTo("EnemyArt700/ENEMY_REC_035_VAR_05/IDLE"));
                Assert.That(actor090.CurrentArtwork076.sprite,
                    Is.SameAs(firstIdle090),
                    "Returning to idle must reuse the paired cached idle sprite.");

                var diagnostics090 = EnemyArt700Runtime090.GetDiagnostics090();
                Assert.That(diagnostics090.PngReadCount, Is.EqualTo(2));
                Assert.That(diagnostics090.CacheHitCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(diagnostics090.CachedSpriteCount,
                    Is.LessThanOrEqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090));
            }
            finally
            {
                actor090?.Dispose();
                Object.Destroy(hostObject090);
            }
        }

        [UnityTest]
        public IEnumerator ActiveActorSpriteSurvivesMoreThanNinetySixOtherLoadsAndCleansUp090()
        {
            EnemyArt700Runtime090.ConfigureCacheCapacity090(
                EnemyArt700Runtime090.MaximumCacheCapacity090);
            var hostObject090 = new GameObject(
                "Enemy Art 700 Lease Stress Host 090",
                typeof(RectTransform));
            var host090 = hostObject090.GetComponent<RectTransform>();
            var union090 = new M2BattleUnionView
            {
                UnionId = "ENEMY_ART_700_LEASE_UNION_090",
                DisplayName = "Lease Stress Union"
            };
            var member090 = new M2BattleMemberView
            {
                MemberId = "ENEMY_ART_700_LEASE_MEMBER_090",
                DisplayName = "Lease Stress Enemy",
                CurrentHp = 300,
                MaximumHp = 300,
                EnemyArtBaseId090 = "ENEMY_REC_035",
                EnemyArtVariantId090 = "ENEMY_REC_035_VAR_05"
            };
            M2BattleActorRig072 actor090 = null;
            Sprite activeSprite090 = null;
            try
            {
                actor090 = new M2BattleActorRig072(
                    host090,
                    union090,
                    member090,
                    true);
                yield return null;

                activeSprite090 = actor090.CurrentArtwork076.sprite;
                Assert.That(activeSprite090, Is.Not.Null);
                Assert.That(activeSprite090.texture, Is.Not.Null);

                var otherUniqueLoads090 = 0;
                var variants090 = EnemyArt700Runtime090.Catalog090.variants;
                for (var index090 = 0;
                     index090 < variants090.Length && otherUniqueLoads090 < 100;
                     index090++)
                {
                    var variant090 = variants090[index090];
                    if (variant090.variantId == member090.EnemyArtVariantId090) continue;
                    Assert.That(EnemyArt700Runtime090.TryLoadSprite090(
                        variant090.baseEnemyId,
                        variant090.variantId,
                        EnemyArt700Pose090.Idle,
                        out var churnSprite090,
                        out var churnError090), Is.True,
                        variant090.variantId + ": " + churnError090);
                    Assert.That(churnSprite090, Is.Not.Null);
                    otherUniqueLoads090++;
                }

                Assert.That(otherUniqueLoads090, Is.GreaterThan(96));
                yield return null;
                Assert.That(actor090.CurrentArtwork076.sprite,
                    Is.SameAs(activeSprite090));
                Assert.That(activeSprite090, Is.Not.Null);
                Assert.That(activeSprite090.texture, Is.Not.Null);

                var stressed090 = EnemyArt700Runtime090.GetDiagnostics090();
                Assert.That(stressed090.CachedSpriteCount,
                    Is.EqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090));
                Assert.That(stressed090.ResidentSpriteCount,
                    Is.EqualTo(EnemyArt700Runtime090.MaximumCacheCapacity090));
                Assert.That(stressed090.PinnedSpriteCount, Is.EqualTo(1));
                Assert.That(stressed090.OutstandingLeaseCount, Is.EqualTo(1));
                Assert.That(stressed090.RetiredSpriteCount, Is.Zero);
                Assert.That(stressed090.EvictionCount, Is.GreaterThan(0));

                actor090.Dispose();
                actor090 = null;
                EnemyArt700Runtime090.ClearCache090();
                Object.Destroy(hostObject090);
                yield return null;

                var cleaned090 = EnemyArt700Runtime090.GetDiagnostics090();
                Assert.That(cleaned090.CachedSpriteCount, Is.Zero);
                Assert.That(cleaned090.ResidentSpriteCount, Is.Zero);
                Assert.That(cleaned090.PinnedSpriteCount, Is.Zero);
                Assert.That(cleaned090.OutstandingLeaseCount, Is.Zero);
                Assert.That(cleaned090.RetiredSpriteCount, Is.Zero);
                Assert.That(activeSprite090 == null, Is.True,
                    "Released cached Sprite must be destroyed under Unity object semantics.");
            }
            finally
            {
                actor090?.Dispose();
                if (hostObject090 != null) Object.Destroy(hostObject090);
                EnemyArt700Runtime090.ClearCache090();
            }
        }
    }
}
