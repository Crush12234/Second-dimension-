using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2EnemyThreatPalette089Tests
    {
        [Test]
        public void TenThreatGradesMapEveryTowerFloorWithoutRandomTint089()
        {
            Assert.That(M2EnemyThreatPalette089.MaximumTier, Is.EqualTo(10));

            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_GATE_GNAWER_01", 80), Is.EqualTo(1));
            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_GATE_GNAWER_02_SPAWN070_01", 90), Is.EqualTo(2));
            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_GATE_GNAWER_03", 110), Is.EqualTo(3));
            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_BRASSJAW_PACKLORD_01", 160), Is.EqualTo(4));
            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_GATEHEART_WARDEN_01", 180), Is.EqualTo(10));

            for (var floor = 1; floor <= 10; floor++)
                Assert.That(M2EnemyThreatPalette089.ResolveTowerTier(
                    "ABYSS_BATTLE022_FLOOR_" + floor.ToString("00") + "_AAA"),
                    Is.EqualTo(floor));
            Assert.That(M2EnemyThreatPalette089.ResolveTowerTier(
                "CAMPAIGN_BATTLE_089"), Is.Zero);

            // A normal family uses its authored identity outside the Tower, but the
            // immutable Tower floor projection controls the live difficulty colour.
            Assert.That(M2EnemyThreatPalette089.ResolveTier(
                "ENEMY_GATE_GNAWER_01", 80, false, 4), Is.EqualTo(4));

            var tintKeys = new System.Collections.Generic.HashSet<string>();
            for (var tier = 1; tier <= 10; tier++)
            {
                var visual = M2EnemyThreatPalette089.Visual(tier);
                Assert.That(visual.Tier, Is.EqualTo(tier));
                Assert.That(visual.RomanTier, Is.Not.Empty);
                Assert.That(visual.Label, Is.Not.Empty);
                Assert.That(visual.ArtworkTint.a, Is.EqualTo(1f));
                Assert.That(visual.FrameColor.a, Is.GreaterThan(0f));
                tintKeys.Add(visual.ArtworkTint.r.ToString("F3") + "|" +
                             visual.ArtworkTint.g.ToString("F3") + "|" +
                             visual.ArtworkTint.b.ToString("F3"));
            }
            Assert.That(tintKeys, Has.Count.EqualTo(10),
                "Each Tower tier must remain visibly distinguishable.");
        }

        [Test]
        public void PaletteIsPresentationOnlyAndLeavesAllMemberAuthorityUntouched089()
        {
            var member = new M2BattleMemberView
            {
                MemberId = "ENEMY_PULSE_SCRIBE_02_SPAWN070_04",
                PortraitAuthorityId = "ENEMY_PULSE_SCRIBE_02",
                DisplayName = "Pulse Scribe",
                CurrentHp = 71,
                MaximumHp = 88
            };

            var visual = M2EnemyThreatPalette089.Resolve(member, true, false);

            Assert.That(visual.Tier, Is.EqualTo(3));
            Assert.That(member.MemberId,
                Is.EqualTo("ENEMY_PULSE_SCRIBE_02_SPAWN070_04"));
            Assert.That(member.PortraitAuthorityId,
                Is.EqualTo("ENEMY_PULSE_SCRIBE_02"));
            Assert.That(member.CurrentHp, Is.EqualTo(71));
            Assert.That(member.MaximumHp, Is.EqualTo(88));
        }
    }
}
