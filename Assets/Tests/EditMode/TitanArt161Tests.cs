using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TitanArt161Tests
    {
        [Test]
        public void EveryTitanAndRewardHeroHasExactPortraitAndDistinctActionPose()
        {
            Assert.IsTrue(TitanArt161.ValidateAll161(out var detail), detail);
            for (var slot = 1; slot <= 13; slot++)
            {
                var hero = "HERO_TITAN_" + slot.ToString("D3");
                Assert.IsTrue(M1VisualAssets.TryResolvePortrait(hero, "unrelated-seed", "HUMAN", hero,
                    "unrelated-role", out var portrait, out var portraitKey), hero);
                Assert.AreSame(TitanArt161.HeroPortrait(hero), portrait);
                StringAssert.Contains(hero, portraitKey);
                Assert.IsTrue(BattleArtRuntimeRegistry011.TryResolvePose(hero, BattleArtPoseDirector011.Idle,
                    out var idle, out var idleKey), hero);
                Assert.IsTrue(BattleArtRuntimeRegistry011.TryResolvePose(hero, BattleArtPoseDirector011.ActionPrimary,
                    out var attack, out var attackKey), hero);
                Assert.AreNotSame(idle, attack, hero + " needs its authored action pose");
                Assert.AreNotEqual(idleKey, attackKey);
                Assert.IsTrue(M1VisualAssets.TryResolveBattleStandee(hero, "different-seed", "ORC", hero,
                    out var standee, out _), hero);
                Assert.AreSame(idle, standee);
                Assert.IsTrue(M1VisualAssets.TryResolveBattleActionPose(hero, "different-seed", "ORC", hero,
                    out var action, out _), hero);
                Assert.AreSame(attack, action);
                Assert.Greater(idle.rect.height, 100); Assert.Greater(attack.rect.height, 100);
                if (slot > 1)
                {
                    // An atlas shares one GPU texture, but never the neighboring
                    // portrait/other pose pixels. Unity coordinates start below.
                    Assert.AreSame(idle.texture, attack.texture);
                    Assert.AreSame(idle.texture, portrait.texture);
                    Assert.IsFalse(idle.rect.Overlaps(attack.rect), "Idle and action must remain separated");
                    Assert.IsFalse(idle.rect.Overlaps(portrait.rect), "Portrait must not leak into idle");
                    Assert.IsFalse(attack.rect.Overlaps(portrait.rect), "Portrait must not leak into action");
                    Assert.Greater(idle.rect.yMin, portrait.rect.yMin);
                    Assert.Greater(attack.rect.xMin, idle.rect.xMin);
                    Assert.IsFalse(idle.texture.isReadable, "CPU atlas copy should be released after framing");
                }
            }
            for (var slot = 1; slot <= 12; slot++)
            {
                var boss = "TITAN_TRIAL_" + slot.ToString("D3");
                var body = boss + "_BOSS";
                Assert.AreSame(TitanArt161.BossPortrait(boss), TitanArt161.BossPortrait(body));
                Assert.IsTrue(BattleArtRuntimeRegistry011.TryResolvePose(body, BattleArtPoseDirector011.Idle,
                    out var idle, out _));
                Assert.IsTrue(BattleArtRuntimeRegistry011.TryResolvePose(body, BattleArtPoseDirector011.ActionPrimary,
                    out var attack, out _));
                Assert.AreNotSame(idle.texture, attack.texture, boss + " must use both original authored source poses");
                Assert.AreSame(TitanArt161.BossPose(boss, false), idle);
            }
        }

        [TestCase("HERO_TITAN_000")]
        [TestCase("HERO_TITAN_014")]
        [TestCase("HERO_TITAN_001_COPY_1")]
        [TestCase("HERO_TITAN_001/../../other")]
        [TestCase("TITAN_TRIAL_013_BOSS")]
        [TestCase("ENEMY_REC_044")]
        [TestCase("HERO_REC_001")]
        [TestCase(null)]
        public void UnknownIdentitiesDoNotAcquireAnotherCharactersArt(string id)
        {
            Assert.IsFalse(TitanArt161.IsMember(id));
            Assert.IsNull(TitanArt161.HeroPortrait(id));
            Assert.IsNull(TitanArt161.BossPortrait(id));
            Assert.IsFalse(TitanArt161.TryResolvePose(id, BattleArtPoseDirector011.Idle, out var sprite, out var key));
            Assert.IsNull(sprite); Assert.IsEmpty(key);
        }
    }
}
