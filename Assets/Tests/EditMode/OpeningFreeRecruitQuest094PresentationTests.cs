using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class OpeningFreeRecruitQuest094PresentationTests
    {
        [TestCase("FREE_RECRUIT", true)]
        [TestCase("FATE", true)]
        [TestCase("RECRUIT", false)]
        [TestCase("CHEST", false)]
        public void FreeEncounterUsesExistingPhysicalDicePolicyAndPaidHiringDoesNot094(
            string category, bool expected)
        {
            var method = typeof(M1FlowPresenter).GetMethod("QuestCardUsesPhysicalDice094",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Assert.That((bool)method.Invoke(null, new object[] {
                new GuildQuestCardView090 { Category = category } }), Is.EqualTo(expected));
        }

        [Test]
        public void FreeEncounterReusesExistingRecruitCardFace094()
        {
            Assert.That(GuildQuestCardPresentation090.VisualResourcePath090("FREE_RECRUIT"),
                Is.EqualTo(GuildQuestCardPresentation090.VisualResourcePath090("RECRUIT")),
                "The shared presentation contract requires the one-case FREE_RECRUIT mapping.");
        }

        [TestCase(5, 2, 0, "INVITATION EARNED")]
        [TestCase(3, 2, 1, "NO INVITATION THIS TIME")]
        public void ActualResolutionBuildsTwoPipDiceAndHonestWinOrMissCopy094(
            int dieOne, int dieTwo, int modifier, string expectedOutcome)
        {
            var presenterObject = new GameObject("Opening Free Recruit Presenter Test 094");
            var rootObject = new GameObject("Opening Free Recruit Resolution Test 094", typeof(RectTransform));
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                var card = new GuildQuestCardView090 {
                    CardId = "FREE_RECRUIT_RESOLUTION_FIXTURE_094", Category = "FREE_RECRUIT",
                    Title = "A Fellow Adventurer", HeroName = "Authored Hero Test Name",
                    DieOne = dieOne, DieTwo = dieTwo, FateCheckModifier = modifier, Target = 7,
                    TreasuryXpCost = 0, CanChoose = true,
                    VisualResourcePath = GuildQuestCardPresentation090.VisualResourcePath090("FREE_RECRUIT") };
                SetField(presenter, "_reducedMotion", true);
                SetField(presenter, "_lastBoardQuestCard090", card);
                SetField(presenter, "_boardQuestCardAwaitingAcknowledgement090", true);
                var build = typeof(M1FlowPresenter).GetMethod("BuildBoardQuestCardResolution090",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(build, Is.Not.Null);
                build.Invoke(presenter, new object[] { rootObject.transform,
                    new GuildCityPresentationState017D { IsAvailable = true } });

                var children = rootObject.GetComponentsInChildren<Transform>(true);
                Assert.That(children.Count(value => value.name == "Authoritative Dice Roll 084"), Is.EqualTo(1));
                var first = children.Single(value => value.name == "First Authoritative Die 084");
                var second = children.Single(value => value.name == "Second Authoritative Die 084");
                Assert.That(ActivePips(first), Is.EqualTo(dieOne));
                Assert.That(ActivePips(second), Is.EqualTo(dieTwo));
                var visible = string.Join("\n", rootObject.GetComponentsInChildren<Text>(true)
                    .Select(value => value.text));
                Assert.That(visible, Does.Contain("DICE SETTLED  •  SAVED RESULT"));
                Assert.That(visible, Does.Contain("DICE TOTAL " + (dieOne + dieTwo + modifier)));
                Assert.That(visible, Does.Contain(expectedOutcome));
                Assert.That(visible, Does.Contain("REVIEW THE RESULT, THEN CONTINUE"));
                if (dieOne + dieTwo + modifier >= 7)
                {
                    Assert.That(visible, Does.Contain(card.HeroName));
                    Assert.That(visible, Does.Contain("CLAIM FREE AFTER THIS ADVENTURE"));
                }
                else
                {
                    Assert.That(visible, Does.Contain("NO XP SPENT"));
                    Assert.That(visible, Does.Not.Contain("INVITATION EARNED"));
                }
                var acknowledge = rootObject.GetComponentsInChildren<Button>(true)
                    .Single(value => value.name == "Acknowledge Board Quest Card Resolution 090");
                Assert.That(acknowledge.GetComponentInChildren<Text>().text, Is.EqualTo("CONTINUE"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootObject);
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        static int ActivePips(Transform face) => face.GetComponentsInChildren<Image>(true)
            .Count(value => value.name.StartsWith("Die Pip ", StringComparison.Ordinal) &&
                value.gameObject.activeSelf && value.sprite != null);

        static void SetField(M1FlowPresenter presenter, string name, object value)
        {
            var field = typeof(M1FlowPresenter).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(presenter, value);
        }
    }
}
