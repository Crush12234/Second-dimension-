using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ChestRewardPresentation091Tests
    {
        [Test]
        public void LegacyTreasureRoomDescribesOnlyItsActualSupplyAndSalvageReward()
        {
            Assert.That(BoardQuestRules081.RoomTitle081("TREASURE"), Is.EqualTo("SUPPLY CACHE"));
            var reward = BoardQuestRules081.RoomReward081("TREASURE");
            Assert.That(reward, Does.Contain("+1 SUPPLY"));
            Assert.That(reward, Does.Contain("SALVAGE"));
            Assert.That(reward, Does.Not.Contain("CHEST"));
            Assert.That(reward, Does.Not.Contain("EQUIPMENT"));
            Assert.That(reward, Does.Not.Contain("INVENTORY"));
        }

        [TestCase("CHEST", "Epic Skyglass Blade", "+4 XP")]
        [TestCase("MERCHANT", "Rare Starlantern Staff", "−45 XP")]
        public void CommittedGearReceiptRetainsExactItemRarityAndClearlyNamesInventory(
            string category, string item, string xp)
        {
            var preview = item + " • PWR +11 • MYS +4 • " + xp;
            var card = new GuildQuestCardView090
            {
                CardId = "QUESTCARD091_DISPLAY_ONLY", Category = category,
                ItemName = item, RewardPreview = preview
            };
            var method = typeof(M1FlowPresenter).GetMethod("ResolvedQuestCardReward090",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var resolved = (string)method.Invoke(null, new object[] { card });
            Assert.That(resolved, Does.StartWith(preview));
            Assert.That(resolved, Does.Contain("ADDED TO INVENTORY"));
            Assert.That(card.RewardPreview, Is.EqualTo(preview),
                "Resolved copy must not change the pre-choice reward or committed identity.");
            Assert.That(card.CardId, Is.EqualTo("QUESTCARD091_DISPLAY_ONLY"));
        }

        [Test]
        public void NonEquipmentRewardDoesNotClaimAnInventoryItem()
        {
            var card = new GuildQuestCardView090 { Category = "XP", RewardPreview = "+20 XP" };
            var method = typeof(M1FlowPresenter).GetMethod("ResolvedQuestCardReward090",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That((string)method.Invoke(null, new object[] { card }), Is.EqualTo("+20 XP"));
        }
    }
}
