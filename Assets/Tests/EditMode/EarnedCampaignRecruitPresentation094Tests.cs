using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EarnedCampaignRecruitPresentation094Tests
    {
        [Test]
        public void CampaignRewardAccessRemainsReachableFromStoryWithController094()
        {
            var story = new GameObject("Story", typeof(RectTransform), typeof(Button));
            var reward = new GameObject("Rewards", typeof(RectTransform), typeof(Button));
            try
            {
                var storyButton = story.GetComponent<Button>();
                var rewardButton = reward.GetComponent<Button>();
                M1FlowPresenter.ConfigureEarnedRecruitNavigation094(storyButton, rewardButton);
                Assert.That(storyButton.navigation.selectOnUp, Is.SameAs(rewardButton));
                Assert.That(rewardButton.navigation.selectOnDown, Is.SameAs(storyButton));
                Assert.That(rewardButton.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            }
            finally { Object.DestroyImmediate(story); Object.DestroyImmediate(reward); }
        }

        [Test]
        public void CampaignRewardPolicyKeepsOrdinaryHiringPaid094()
        {
            Assert.That(M1FlowPresenter.EarnedRecruitPolicyCopy094, Does.Contain("3 new members"));
            Assert.That(M1FlowPresenter.EarnedRecruitPolicyCopy094, Does.Contain("Regular hiring still costs XP"));
        }

        [TestCase("Chapter CH018_001", "CHAPTER 1 REWARD")]
        [TestCase("Chapter CH018_021", "CHAPTER 21 REWARD")]
        [TestCase("Chapter CH018_050", "CHAPTER 50 REWARD")]
        [TestCase("Opening story", "OPENING STORY REWARD")]
        [TestCase("Quest-card reward", "QUEST-CARD REWARD")]
        public void RewardSourceUsesPlayerReadableNames094(string source, string expected)
        {
            Assert.That(M1FlowPresenter.EarnedRecruitSourceCopy094(source), Is.EqualTo(expected));
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void ThreeHeroCardsRemainLargeSeparateAndClearOfNavigation094(int width, int height)
        {
            for (var index = 0; index < 3; index++)
            {
                var card = M1FlowPresenter.EarnedRecruitCardRect094(index);
                Assert.That(card.width * width, Is.GreaterThanOrEqualTo(350));
                Assert.That(card.height * height, Is.GreaterThanOrEqualTo(450));
                Assert.That(card.yMin, Is.GreaterThan(0.245f));
                Assert.That(card.yMax, Is.LessThan(0.845f));
                Assert.That(card.xMin, Is.GreaterThanOrEqualTo(0.05f));
                Assert.That(card.xMax, Is.LessThanOrEqualTo(0.95f));
                if (index > 0)
                    Assert.That((card.xMin - M1FlowPresenter.EarnedRecruitCardRect094(index - 1).xMax) * width,
                        Is.GreaterThanOrEqualTo(12));
            }
        }
    }
}
