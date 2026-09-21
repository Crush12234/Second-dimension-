using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourGoldCampaign023ProjectionContracts090Tests
    {
        private static string ReadSource(params string[] relativeParts)
        {
            var parts = new string[relativeParts.Length + 1];
            parts[0] = Application.dataPath;
            relativeParts.CopyTo(parts, 1);
            return File.ReadAllText(Path.Combine(parts));
        }

        [Test]
        public void SmokeRequiresTheShippingDynamicRouteHeading090()
        {
            var smokeSource = ReadSource(
                "SecondDimension", "Presentation", "FirstHour071",
                "FirstHourGoldSmoke071.cs");
            var deckSource = ReadSource(
                "SecondDimension", "Presentation", "Campaign023",
                "M1FlowPresenter.ExpeditionDeck089.cs");

            Assert.That(deckSource, Does.Contain(
                "\"Expedition route row heading text 089\""));
            Assert.That(deckSource, Does.Contain("\"ROUTE ROUND \" + choiceRound"));
            Assert.That(deckSource, Does.Contain("\"+ CHOICES THIS QUEST\""));
            Assert.That(smokeSource, Does.Contain(
                "\"Expedition route row heading text 089\""));
            Assert.That(smokeSource, Does.Contain("\"ROUTE ROUND 1\""));
            Assert.That(smokeSource, Does.Contain("\"10+ CHOICES THIS QUEST\""));
            Assert.That(smokeSource, Does.Not.Contain(
                "CHOOSE YOUR NEXT ROUTE  •  THREE CARDS DRAWN"),
                "The certified smoke must follow the live round/minimum-choice heading, not retired fixed copy.");
        }

        [Test]
        public void SmokeCountsExactProjectedRouteWrappersRatherThanTheirSharedPrefix090()
        {
            var smokeSource = ReadSource(
                "SecondDimension", "Presentation", "FirstHour071",
                "FirstHourGoldSmoke071.cs");
            var deckSource = ReadSource(
                "SecondDimension", "Presentation", "Campaign023",
                "M1FlowPresenter.ExpeditionDeck089.cs");

            Assert.That(deckSource, Does.Contain(
                "\"Expedition route card \" + card.CardId + \" 089\""));
            Assert.That(deckSource, Does.Contain(
                "\"Expedition route card surface \" + card.CardId + \" 089\""),
                "The surface deliberately shares the wrapper prefix, so a prefix count is ambiguous.");
            Assert.That(smokeSource, Does.Not.Contain(
                "CountActiveNamedObjects076(\"Expedition route card \""));
            Assert.That(smokeSource, Does.Contain("projectedRouteCards084"));
            Assert.That(smokeSource, Does.Contain(
                "\"Expedition route card \" + value084.CardId + \" 089\""));
            Assert.That(smokeSource, Does.Contain("activeProjectedRouteCards084 == 3"));
        }

        [Test]
        public void SmokeRequiresTheShippingBranchingQuestMapProgressHeading090()
        {
            var smokeSource = ReadSource(
                "SecondDimension", "Presentation", "FirstHour071",
                "FirstHourGoldSmoke071.cs");
            var boardSource = ReadSource(
                "SecondDimension", "Presentation", "Campaign023",
                "GuildCityAdventureBoard084.cs");

            Assert.That(boardSource, Does.Contain(
                "\"QUEST MAP  •  BRANCHING PATH  •  ROOM \""));
            Assert.That(smokeSource, Does.Contain("\"QUEST MAP\""));
            Assert.That(smokeSource, Does.Contain("\"BRANCHING PATH\""));
            Assert.That(smokeSource, Does.Not.Contain("ONE TAP = ONE ROOM"),
                "The retired one-tap progress copy must not return to the C023 certification.");
        }

        [Test]
        public void EarnedHeroEvidenceWaitsForItsExactRouteCardToFinishRevealing090()
        {
            var recruitSmokeSource = ReadSource(
                "SecondDimension", "Presentation", "FirstHour071",
                "FirstHourGoldSmoke071.RecruitAscension089.cs");
            var deckSource = ReadSource(
                "SecondDimension", "Presentation", "Campaign023",
                "M1FlowPresenter.ExpeditionDeck089.cs");
            var animationSource = ReadSource(
                "SecondDimension", "Presentation",
                "M1FlowPresenter.BoardAdventureAnimation084.cs");

            Assert.That(deckSource, Does.Contain(
                "\"Expedition route card \" + card.CardId + \" 089\""));
            Assert.That(deckSource, Does.Contain(
                "\"Choose Expedition route card \" + card.CardId + \" 089\""));
            Assert.That(deckSource, Does.Contain("button.interactable = false"));
            Assert.That(animationSource, Does.Contain(
                "\"Board Adventure Card Flip Stage 086\""));
            var stageRemoval = animationSource.LastIndexOf(
                "cardStage.SetActive(false)", System.StringComparison.Ordinal);
            var revealCompletion = animationSource.LastIndexOf(
                "revealComplete?.Invoke()", System.StringComparison.Ordinal);
            Assert.That(stageRemoval, Is.GreaterThanOrEqualTo(0));
            Assert.That(revealCompletion, Is.GreaterThanOrEqualTo(0));
            Assert.That(stageRemoval, Is.LessThan(revealCompletion),
                "The choose button's reveal callback must remain later than the opaque stage removal.");

            Assert.That(recruitSmokeSource, Does.Contain(
                "private static IEnumerator WaitForVisibleExpeditionRouteCard089("));
            Assert.That(recruitSmokeSource, Does.Contain(
                "Time.realtimeSinceStartup + 12f"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "wrapper.GetComponentsInChildren<Transform>(true).Any"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "value.name, \"Board Adventure Card Flip Stage 086\""));
            Assert.That(recruitSmokeSource, Does.Contain(
                "!activeFlipStage && button != null && button.IsInteractable()"));

            var recruitShow = recruitSmokeSource.IndexOf(
                "_presenter.ShowFirstHourGoldAdventureBoard084(",
                System.StringComparison.Ordinal);
            var recruitWait = recruitSmokeSource.IndexOf(
                "yield return WaitForVisibleExpeditionRouteCard089(",
                recruitShow, System.StringComparison.Ordinal);
            var recruitCapture = recruitSmokeSource.IndexOf(
                "yield return Capture071(\"earned_recruit_card_exact_hero\")",
                System.StringComparison.Ordinal);
            var ascensionShow = recruitSmokeSource.IndexOf(
                "_presenter.ShowFirstHourGoldAdventureBoard084(",
                recruitCapture, System.StringComparison.Ordinal);
            var ascensionWait = recruitSmokeSource.IndexOf(
                "yield return WaitForVisibleExpeditionRouteCard089(",
                ascensionShow, System.StringComparison.Ordinal);
            var ascensionCapture = recruitSmokeSource.IndexOf(
                "yield return Capture071(\"earned_ascension_card_exact_hero\")",
                System.StringComparison.Ordinal);

            Assert.That(recruitShow, Is.GreaterThanOrEqualTo(0));
            Assert.That(recruitWait, Is.GreaterThan(recruitShow));
            Assert.That(recruitCapture, Is.GreaterThan(recruitWait));
            Assert.That(ascensionShow, Is.GreaterThan(recruitCapture));
            Assert.That(ascensionWait, Is.GreaterThan(ascensionShow));
            Assert.That(ascensionCapture, Is.GreaterThan(ascensionWait));
        }

        [Test]
        public void EarnedRecruitPlanAdvancesTheShippingEncounterRowBeforeSamplingRoutes090()
        {
            var recruitSmokeSource = ReadSource(
                "SecondDimension", "Presentation", "FirstHour071",
                "FirstHourGoldSmoke071.RecruitAscension089.cs");
            var serviceSource = ReadSource(
                "SecondDimension", "Gameplay", "Campaign023",
                "ExpeditionDeckService089.cs");

            Assert.That(serviceSource, Does.Contain(
                "var hasEncounterRow = nodeCards.Any(value => !value.AdvancesRoute);"));
            Assert.That(serviceSource, Does.Contain(
                "var encounterResolved = deck.AppliedReceipts.Any"));
            Assert.That(serviceSource, Does.Contain(
                "var drawRoute = !hasEncounterRow || encounterResolved;"),
                "The production deck intentionally deals encounter cards before route cards.");
            Assert.That(serviceSource, Does.Contain(
                "? new[] { \"HAZARD\", \"CHANCE\", \"RECRUIT\" }"),
                "The START room's Recruit offer is scheduled in its second, route-phase row.");

            var planMethod = recruitSmokeSource.IndexOf(
                "private static EarnedRecruitPlan089 FindNewRecruitPlan089(",
                System.StringComparison.Ordinal);
            var ascensionMethod = recruitSmokeSource.IndexOf(
                "private static EarnedRecruitPlan089 FindAscensionPlan089(",
                planMethod, System.StringComparison.Ordinal);
            Assert.That(planMethod, Is.GreaterThanOrEqualTo(0));
            Assert.That(ascensionMethod, Is.GreaterThan(planMethod));
            var planBody = recruitSmokeSource.Substring(
                planMethod, ascensionMethod - planMethod);
            Assert.That(planBody, Does.Contain("!value.AdvancesRoute"));
            Assert.That(planBody, Does.Contain(
                "state = ResolveCard089(\n                    state, encounter.CardId"));
            Assert.That(planBody, Does.Contain(
                "ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>\n" +
                "                        StringComparer.Ordinal.Equals(value.Category, \"RECRUIT\")"));
            Assert.That(planBody, Does.Contain(
                "AdvanceCardIds = new[] { encounter.CardId }"));

            var begin = recruitSmokeSource.IndexOf(
                "BeginWorldGateOperation023(\n                RecruitSmokeChapter089)",
                System.StringComparison.Ordinal);
            var advance = recruitSmokeSource.IndexOf(
                "foreach (var advanceCardId in firstPlan.AdvanceCardIds)",
                begin, System.StringComparison.Ordinal);
            var targetLookup = recruitSmokeSource.IndexOf(
                "var recruitRoute = recruitCoordinator.CampaignWorldGate023.RouteCards",
                advance, System.StringComparison.Ordinal);
            Assert.That(begin, Is.GreaterThanOrEqualTo(0));
            Assert.That(advance, Is.GreaterThan(begin));
            Assert.That(targetLookup, Is.GreaterThan(advance),
                "The packaged smoke must replay the planned encounter receipt before checking the live Recruit route row.");
        }
    }
}
