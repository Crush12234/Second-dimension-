using NUnit.Framework;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2EquipmentPowerPolicy087Tests
    {
        [Test]
        public void StartingEquipmentDoesNotChangeCertifiedBattlePower087()
        {
            var starter = Item("STARTER_SWORD_087", "QUALITY_RARE", "SWORD");
            var bonus = M2EquipmentPowerPolicy087.Resolve(starter);

            Assert.That(bonus.PhysicalAttack, Is.Zero);
            Assert.That(bonus.MysticAttack, Is.Zero);
        }

        [Test]
        public void CreatorWeaponGetsVisibleDeterministicCombatPower087()
        {
            var creator = Item(
                "CRITEM10000_087",
                "QUALITY_RARE",
                "WF01_SWORD",
                "SWORD",
                "CREATOR_GIVEAWAY",
                "MODIFIED_WEAPON");
            var bonus = M2EquipmentPowerPolicy087.Resolve(creator);

            Assert.That(bonus.PhysicalAttack, Is.EqualTo(7));
            Assert.That(bonus.MysticAttack, Is.EqualTo(2));
        }

        [Test]
        public void EarnedStaffLootFavorsMysticPower087()
        {
            var staff = Item(
                "LOOT_ITEM_070_087",
                "QUALITY_EPIC",
                "WF09_STAFF",
                "STAFF");
            var bonus = M2EquipmentPowerPolicy087.Resolve(staff);

            Assert.That(bonus.PhysicalAttack, Is.EqualTo(4));
            Assert.That(bonus.MysticAttack, Is.EqualTo(10));
        }

        private static EquipmentItemState Item(
            string instanceId,
            string qualityId,
            params string[] tags) =>
            new EquipmentItemState(
                instanceId,
                "DEFINITION_" + instanceId,
                "Test equipment",
                new[] { EquipmentSlotIds.MainHand },
                tags,
                qualityId,
                10_000,
                false);
    }
}
