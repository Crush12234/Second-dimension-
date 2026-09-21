using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardCardVisual088Tests
    {
        const string CardBack088 =
            "SecondDimension/Art/Board086/SECOND_DIMENSION_CARD_BACK_088";

        [Test]
        public void OriginalSecondDimensionCardBackIsShippingSprite088()
        {
            var sprite = Resources.Load<Sprite>(CardBack088);
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(1024));
            Assert.That(sprite.texture.height, Is.GreaterThan(sprite.texture.width));
        }

        [TestCase("OBJECTIVE", "+500 GUILD XP", "XP")]
        [TestCase("OBJECTIVE", "RECRUIT NETWORK ×8", "RECRUIT")]
        [TestCase("RESOURCE", "MOONSTONE CHEST", "TREASURE")]
        [TestCase("BATTLE", "STORY PROGRESS", "MONSTER")]
        public void RewardCopySelectsReadableCardType088(
            string roomKind, string rewardCopy, string expected)
        {
            var method = typeof(M1FlowPresenter).GetMethod(
                "BoardAdventureVisualKind087",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            var actual = method.Invoke(null,
                new object[] {roomKind, rewardCopy, true}) as string;
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
