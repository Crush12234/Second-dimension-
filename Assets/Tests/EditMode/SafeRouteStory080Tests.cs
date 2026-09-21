using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017E;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SafeRouteStory080Tests
    {
        private GuildCityContent017D _content;
        private GuildCityExpeditionService017D _expeditions;

        [SetUp]
        public void SetUp()
        {
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT",
                "GUILD_CITY_017D"));
            _expeditions = new GuildCityExpeditionService017D();
        }

        [TestCase(
            GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
            "BOARD_BELL_BENEATH_GATE",
            GuildCityExpeditionService017D.FirstStorySafePassageEventId080,
            "Gara Redtail",
            "wounded Lanterns",
            "route ledger")]
        [TestCase(
            GuildCityExpeditionService017D.SecondStoryBoardId076,
            GuildCityExpeditionService017D.SecondStoryBoardId076,
            GuildCityExpeditionService017D.SecondStorySafeDescentEventId080,
            "Gara Redtail",
            "Sella Vey",
            "field book")]
        public void SafeRouteIsANamedAuthoredDecisionWithAnExplicitConsequence080(
            string liveBoardId,
            string presentationBoardId,
            string eventId,
            string namedPerson,
            string protectedPeople,
            string relinquishedEvidence)
        {
            var node = _content.Board(liveBoardId).Node("N12");
            var authored = _content.Event(eventId);
            var presentationNode = GuildCityOpeningExperienceRegistry017E.Node(
                presentationBoardId,
                "N12");
            var presentationEvent = GuildCityOpeningExperienceRegistry017E.Event(eventId);

            Assert.That(node.Kind, Is.EqualTo("SAFE_ROUTE"));
            Assert.That(node.EventId, Is.EqualTo(eventId));
            Assert.That(node.Links, Is.EqualTo(new[] { "N13" }));
            Assert.That(GuildCityExpeditionService017D.IsCommittedCheckNode(node), Is.True);
            Assert.That(GuildCityExpeditionService017D.CurrentNodeRequiresResolution(node), Is.True);
            Assert.That(GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                    liveBoardId,
                    authored),
                Is.EqualTo(StringComparer.Ordinal.Equals(
                    liveBoardId,
                    GuildCityExpeditionService017D.SecondStoryBoardId076)));
            Assert.That(authored.EligibleSkills, Has.Length.GreaterThanOrEqualTo(2));
            Assert.That(presentationNode, Is.Not.Null);
            Assert.That(presentationEvent, Is.Not.Null);

            var completeStory = string.Join("\n", new[]
            {
                presentationNode.displayName,
                presentationNode.summary,
                authored.Title,
                authored.Problem,
                authored.ConsequenceIdentity,
                authored.ExceptionalText,
                authored.FullSuccessText,
                authored.SuccessWithCostText,
                authored.SetbackText,
                authored.SevereSetbackText,
                authored.RelationshipMemorySummary
            });
            Assert.That(completeStory, Does.Contain(namedPerson));
            Assert.That(completeStory, Does.Contain(protectedPeople));
            Assert.That(completeStory, Does.Contain(relinquishedEvidence));
            foreach (var forbidden in new[]
                     {
                         "route graph", "route decision", "random", "rng", "roll 2d6",
                         "deterministic 2d6", "developer", "soft lock"
                     })
                Assert.That(completeStory, Does.Not.Contain(forbidden).IgnoreCase, eventId);

            var orders = ExpeditionBoardProjection074.EventApproachLabels076(eventId);
            Assert.That(orders, Has.Count.EqualTo(2));
            var evidenceToken = relinquishedEvidence.Split(' ').Last().ToUpperInvariant();
            Assert.That(string.Join("\n", orders),
                Does.Contain("GARA").And.Contain(evidenceToken));
            Assert.That(ExpeditionBoardProjection074.EventCommitActionLabel076(eventId, 0),
                Does.StartWith("COMMIT"));
        }

        [TestCase(
            GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
            GuildCityExpeditionService017D.FirstStorySafePassageEventId080)]
        [TestCase(
            GuildCityExpeditionService017D.SecondStoryBoardId076,
            GuildCityExpeditionService017D.SecondStorySafeDescentEventId080)]
        public void SafeRouteBlocksTheClimaxUntilItsCommittedOrderIsResolved080(
            string boardId,
            string eventId)
        {
            var staged = CreateCampaignAtSafeRoute080(boardId);

            var blocked = _expeditions.CommitMove(staged, _content, "N13");
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors,
                Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));

            var lowSubmittedInput = Require(_expeditions.ResolveCommittedCheck(
                staged,
                _content,
                eventId,
                "R1",
                "R2",
                -99));
            var highSubmittedInput = Require(_expeditions.ResolveCommittedCheck(
                staged,
                _content,
                eventId,
                "R1",
                "R2",
                99));
            var lowCheck = lowSubmittedInput.Guild.GuildCity.Expedition.CommittedChecks.Single();
            var highCheck = highSubmittedInput.Guild.GuildCity.Expedition.CommittedChecks.Single();

            var usesCommitted2d6 = StringComparer.Ordinal.Equals(
                boardId,
                GuildCityExpeditionService017D.SecondStoryBoardId076);
            if (usesCommitted2d6)
            {
                Assert.That(lowCheck.DieOne, Is.InRange(1, 6));
                Assert.That(lowCheck.DieTwo, Is.InRange(1, 6));
                Assert.That(highCheck.DieOne, Is.EqualTo(lowCheck.DieOne));
                Assert.That(highCheck.DieTwo, Is.EqualTo(lowCheck.DieTwo));
                Assert.That(lowCheck.Outcome, Is.EqualTo("SEVERE_SETBACK"));
                Assert.That(highCheck.Outcome, Is.EqualTo("EXCEPTIONAL"));
                Assert.That(highCheck.Total, Is.EqualTo(lowCheck.Total + 198));
            }
            else
            {
                Assert.That(lowCheck.DieOne, Is.Zero);
                Assert.That(lowCheck.DieTwo, Is.Zero);
                Assert.That(highCheck.Outcome, Is.EqualTo(lowCheck.Outcome));
                Assert.That(highCheck.Total, Is.EqualTo(lowCheck.Total));
            }
            Assert.That(highCheck.CanonicalSeedIdentity,
                Is.EqualTo(lowCheck.CanonicalSeedIdentity));
            Assert.That(lowSubmittedInput.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Contain("EVENT_RESOLVED_" + eventId));
            Assert.That(lowSubmittedInput.Guild.GuildCity.RelationshipMemories.Single(
                    value => value.SourceId == eventId).Summary,
                Does.Contain("Gara Redtail"));

            var rejoined = Require(_expeditions.CommitMove(
                lowSubmittedInput,
                _content,
                "N13"));
            Assert.That(rejoined.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N13"));
        }

        [Test]
        public void SafeRoutePresentationAuthorityCopiesRemainByteIdentical080()
        {
            var resources = Path.Combine(
                Application.dataPath,
                "Resources",
                "SecondDimension",
                "GuildCity017E",
                "Data",
                "OPENING_EXPERIENCE_017E.json");
            var streaming = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT",
                "GUILD_CITY_017E",
                "OPENING_EXPERIENCE_017E.json");
            Assert.That(File.ReadAllBytes(streaming), Is.EqualTo(File.ReadAllBytes(resources)));
        }

        private static CampaignState CreateCampaignAtSafeRoute080(string boardId)
        {
            var recruits = Enumerable.Range(1, 10)
                .Select(index => new RecruitState("R" + index, 100, 100, 20, 20))
                .ToArray();
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3", "R4", "R5", "R6" },
                    "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R7",
                    new[] { "R7", "R8", "R9", "R10" },
                    "FORMATION_SKIRMISH_LINE", "DOCTRINE_BALANCED", 30, 7000)
            };
            var guild = new GuildState("GUILD_SAFE_ROUTE_080", 0, recruits, unions);
            var expedition = new ExpeditionState017D(
                "EXPEDITION_SAFE_ROUTE_080_" + boardId,
                "CONTRACT_COMMIT_SAFE_ROUTE_080",
                boardId,
                "N12",
                ExpeditionStatus017D.Active,
                10,
                0,
                0,
                10,
                new[] { "N00", "N10", "N12" },
                new[] { "N00", "N10", "N12", "N13" },
                Array.Empty<string>(),
                Array.Empty<CommittedCheckState017D>(),
                Array.Empty<string>(),
                "safe_route_story_080");
            var city = guild.GuildCity.With(
                expedition: expedition,
                replaceExpedition: true,
                lastCheckpointId: "safe_route_story_080");
            guild = guild.WithGuildCity(city);
            var flow = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                true,
                "complete");
            return new CampaignState(
                "00000000-0000-0000-0000-000000080080",
                80080,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild,
                new NewGuildProfileState(
                    "Tester",
                    GameMode.Standard,
                    TutorialDepth.FullTutorial,
                    AccessibilitySettingsState.Defaults(),
                    false),
                flow);
        }

        private static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
