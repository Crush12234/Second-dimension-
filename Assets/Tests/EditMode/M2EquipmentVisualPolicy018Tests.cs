using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2EquipmentVisualPolicy018Tests
    {
        [TestCase("SWORD", "SHIELD", "SWORD_SHIELD", "ANIMSET_SWORD_SHIELD", true)]
        [TestCase("GREAT_AXE", "ARMOR_HEAVY", "HEAVY", "ANIMSET_HEAVY", false)]
        [TestCase("STAFF", "FOCUS", "STAFF_MYSTIC", "ANIMSET_STAFF_MYSTIC", false)]
        [TestCase("BOW", "SCOUTING", "RANGED", "ANIMSET_RANGED", false)]
        [TestCase("SPEAR", "WEAPON", "POLEARM", "ANIMSET_POLEARM", false)]
        [TestCase("DAGGER", "TOOL", "DAGGER", "ANIMSET_DAGGER", false)]
        [TestCase("GAUNTLET", "FOCUS_TOOL", "GAUNTLET", "ANIMSET_GAUNTLET", false)]
        public void EquipmentTagsSelectStableRigAndAnimationFamily(
            string firstTag,
            string secondTag,
            string expectedFamily,
            string expectedAnimator,
            bool expectedOffHand)
        {
            var profile = M2EquipmentVisualPolicy018.Resolve(new[] { firstTag, secondTag });

            Assert.That(profile.WeaponFamilyId, Is.EqualTo(expectedFamily));
            Assert.That(profile.AnimatorSetId, Is.EqualTo(expectedAnimator));
            Assert.That(profile.MainHandSocketId, Is.EqualTo(M2EquipmentVisualPolicy018.RightHandSocket));
            Assert.That(profile.BodyArmorSocketId, Is.EqualTo(M2EquipmentVisualPolicy018.BodyArmorSocket));
            Assert.That(profile.UsesOffHand, Is.EqualTo(expectedOffHand));
            Assert.That(profile.OffHandSocketId,
                Is.EqualTo(expectedOffHand ? M2EquipmentVisualPolicy018.LeftHandSocket : string.Empty));
        }

        [TestCase("ARMOR_LIGHT", "LIGHT", "ARMORSET_LIGHT")]
        [TestCase("ARMOR_MEDIUM", "MEDIUM", "ARMORSET_MEDIUM")]
        [TestCase("ARMOR_HEAVY", "HEAVY", "ARMORSET_HEAVY")]
        public void ArmorTagsSelectAStableBodyMeshFamily(
            string armorTag,
            string expectedFamily,
            string expectedMeshSet)
        {
            var profile = M2EquipmentVisualPolicy018.Resolve(new[] { "SWORD", armorTag });

            Assert.That(profile.ArmorFamilyId, Is.EqualTo(expectedFamily));
            Assert.That(profile.ArmorMeshSetId, Is.EqualTo(expectedMeshSet));
            Assert.That(profile.PlayerFacingSummary.ToUpperInvariant(), Does.Contain(expectedFamily));

            var proceduralProfile = M2EquipmentVisualPolicy018.Resolve(
                new[] { "SWORD", "ARMOR", armorTag.Replace("ARMOR_", string.Empty) });
            Assert.That(proceduralProfile.ArmorFamilyId, Is.EqualTo(expectedFamily),
                "Existing procedural LIGHT/MEDIUM/HEAVY loadouts must retain the same visual family.");
            Assert.That(proceduralProfile.ArmorMeshSetId, Is.EqualTo(expectedMeshSet));

            var weaponOnly = M2EquipmentVisualPolicy018.Resolve(
                new[] { "AXE", armorTag.Replace("ARMOR_", string.Empty) });
            Assert.That(weaponOnly.ArmorFamilyId, Is.EqualTo("UNSPECIFIED"),
                "A legacy weapon weight tag must not masquerade as equipped body armor.");
        }

        [Test]
        public void VisualResolutionIsReadOnlyAndOrderIndependent()
        {
            var original = new[] { "SHIELD", "SWORD", "ARMOR" };
            var first = M2EquipmentVisualPolicy018.Resolve(original);
            var second = M2EquipmentVisualPolicy018.Resolve(new[] { "ARMOR", "SWORD", "SHIELD" });

            Assert.That(first.WeaponFamilyId, Is.EqualTo(second.WeaponFamilyId));
            Assert.That(first.AnimatorSetId, Is.EqualTo(second.AnimatorSetId));
            Assert.That(original, Is.EqualTo(new[] { "SHIELD", "SWORD", "ARMOR" }),
                "Presentation resolution must never reorder or mutate authoritative equipment tags.");

            var spearAndShield = M2EquipmentVisualPolicy018.Resolve(new[] { "SPEAR", "SHIELD" });
            Assert.That(spearAndShield.WeaponFamilyId, Is.EqualTo("POLEARM"));
            Assert.That(spearAndShield.UsesOffHand, Is.True,
                "A shield-bearing spear loadout must not lose its off-hand presentation state.");
        }

        [Test]
        public void ArtsSummaryExplainsForecastEffectWithoutOfferingArtButtons()
        {
            var summary = M2EquipmentVisualPolicy018.ArtsAccessSummary(new[] { "SWORD", "SHIELD" });

            Assert.That(summary, Does.Contain("Union forecasts"));
            Assert.That(summary, Does.Contain("shield Arts"));
            Assert.That(summary.ToUpperInvariant(), Does.Not.Contain("SELECT AN ART"));
        }
    }
}
