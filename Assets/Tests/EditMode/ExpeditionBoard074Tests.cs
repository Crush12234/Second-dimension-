using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ExpeditionBoard074Tests
    {
        [Test]
        public void BoardProjection_UsesFifteenHumanNamedDestinations()
        {
            var view = ExpeditionBoardProjection074.Build(ActiveRouteState074());

            Assert.That(view.Nodes.Count, Is.EqualTo(15));
            Assert.That(view.Nodes.Select(value => value.DisplayName).Distinct().Count(), Is.EqualTo(15));
            Assert.That(view.Nodes.All(value => !string.IsNullOrWhiteSpace(value.DisplayName)), Is.True);
            Assert.That(view.Nodes.All(value => !value.DisplayName.StartsWith("N0", StringComparison.Ordinal)), Is.True,
                "Save node IDs must never be used as destination names.");
            Assert.That(view.Nodes.Single(value => value.NodeId == "N07").DisplayName, Does.Contain("Lantern Watch"));
            Assert.That(view.Nodes.Single(value => value.NodeId == "N13").DisplayName, Does.Contain("Gatehouse"));
        }

        [Test]
        public void BoardProjection_SeparatesProgressAndSemanticNodeStates()
        {
            var view = ExpeditionBoardProjection074.Build(ActiveRouteState074());

            var current = view.Nodes.Single(value => value.NodeId == "N06");
            Assert.That(current.IsCurrent, Is.True);
            Assert.That(current.IsThreat, Is.True);
            Assert.That(current.StateLabel, Is.EqualTo("CURRENT • THREAT"));

            var camp = view.Nodes.Single(value => value.NodeId == "N07");
            Assert.That(camp.IsAvailable, Is.True);
            Assert.That(camp.IsCamp, Is.True);
            Assert.That(camp.StateLabel, Is.EqualTo("AVAILABLE • CAMP"));

            var objective = view.Nodes.Single(value => value.NodeId == "N13");
            Assert.That(objective.IsObjective, Is.True);
            Assert.That(objective.IsLocked, Is.True);
            Assert.That(objective.StateLabel, Is.EqualTo("LOCKED • OBJECTIVE • THREAT"));

            var visited = view.Nodes.Single(value => value.NodeId == "N00");
            Assert.That(visited.IsVisited, Is.True);
            Assert.That(visited.IsLocked, Is.False);
        }

        [Test]
        public void BoardProjection_CoversEveryOperationActionState()
        {
            var chooseContract = AvailableState074();
            AssertAction074(chooseContract, ExpeditionBoardActionKind074.ChooseContract);

            var begin = AvailableState074();
            begin.HasActiveContract = true;
            AssertAction074(begin, ExpeditionBoardActionKind074.BeginExpedition);

            var check = AvailableState074();
            check.HasActiveContract = true;
            check.Expedition = Expedition074("N04", "SKILL_CHECK", resolutionComplete: false);
            AssertAction074(check, ExpeditionBoardActionKind074.ResolveCheck);

            var safeRoute = AvailableState074();
            safeRoute.HasActiveContract = true;
            safeRoute.Expedition = Expedition074("N12", "SAFE_ROUTE", resolutionComplete: false);
            safeRoute.Expedition.CurrentEventId =
                GuildCityExpeditionService017D.FirstStorySafePassageEventId080;
            AssertAction074(safeRoute, ExpeditionBoardActionKind074.ResolveCheck);
            Assert.That(ExpeditionBoardProjection074.Build(safeRoute).StoryObjective,
                Does.Contain("Gara").And.Contain("wounded Lantern"));

            var commit = AvailableState074();
            commit.HasActiveContract = true;
            commit.Expedition = Expedition074("N06", "ENCOUNTER", canCommitEncounter: true);
            AssertAction074(commit, ExpeditionBoardActionKind074.CommitEncounter);

            var enter = AvailableState074();
            enter.HasActiveContract = true;
            enter.HasPendingEncounter = true;
            enter.Expedition = Expedition074("N06", "ENCOUNTER");
            AssertAction074(enter, ExpeditionBoardActionKind074.EnterBattle);

            var returnPending = AvailableState074();
            returnPending.HasActiveContract = true;
            returnPending.HasPendingEncounter = true;
            returnPending.HasPendingBattleReturn = true;
            returnPending.Expedition = Expedition074("N06", "ENCOUNTER");
            AssertAction074(returnPending, ExpeditionBoardActionKind074.AwaitBattleReturn);

            var move = ActiveRouteState074();
            AssertAction074(move, ExpeditionBoardActionKind074.ChooseRoute);

            var optionalElite = AvailableState074();
            optionalElite.HasActiveContract = true;
            optionalElite.Expedition = Expedition074(
                "N09",
                "OPTIONAL_ELITE",
                canMove: true,
                canCommitEncounter: true);
            optionalElite.Expedition.LinkedNodeIds = new[] { "N10" };
            AssertAction074(optionalElite, ExpeditionBoardActionKind074.ChooseRoute);
            Assert.That(ExpeditionBoardProjection074.Build(optionalElite).StoryObjective,
                Does.Contain("elite ambush").And.Contain("Win the Union battle"));

            var finalize = AvailableState074();
            finalize.Expedition = Expedition074("N14", "EXIT", canFinalize: true);
            AssertAction074(finalize, ExpeditionBoardActionKind074.FinalizeOperation);
        }

        [Test]
        public void FirstHourObjective_RequiresPatrolRescueBeforeBossCommit()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N13", "MAIN_OBJECTIVE", canCommitEncounter: true);

            Assert.That(ExpeditionBoardProjection074.NeedsFirstHourPatrolRescue074(state), Is.True);
            AssertAction074(state, ExpeditionBoardActionKind074.ResolveObjective);
            Assert.That(ExpeditionBoardProjection074.Build(state).StoryObjective,
                Is.EqualTo("The Lantern Patrol is alive at the Old Gatehouse. Rally Zorin's survivors, recover the Wayglass, then stop the Gate-Eater."));

            state.Expedition.ObjectiveFlags = new[]
            {
                GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071
            };
            Assert.That(ExpeditionBoardProjection074.NeedsFirstHourPatrolRescue074(state), Is.False,
                "The live Release 071 rescue receipt must unlock the Gate-Eater action.");
            AssertAction074(state, ExpeditionBoardActionKind074.CommitEncounter);

            state.Expedition.ObjectiveFlags = new[] { "PRIMARY_OBJECTIVE_RESCUE_COMPLETE" };
            Assert.That(ExpeditionBoardProjection074.NeedsFirstHourPatrolRescue074(state), Is.False);
            AssertAction074(state, ExpeditionBoardActionKind074.CommitEncounter);
        }

        [Test]
        public void ChapterTwoFreshMarksAndBridge_UseSurveyCrewStoryLanguage()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N02", "EVENT", resolutionComplete: false);
            state.Expedition.BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;

            var freshMarks = ExpeditionBoardProjection074.Build(state);
            var freshMarksBeat = ExpeditionBoardProjection074.CompanionStoryBeat076(
                state,
                freshMarks.Nodes.Single(value => value.IsCurrent));

            Assert.That(freshMarks.BoardTitle,
                Is.EqualTo(M1FlowPresenter.ChapterTwoArcTitle076));
            Assert.That(freshMarks.OperationTitle,
                Is.EqualTo("OPERATION 2  •  " + M1FlowPresenter.ChapterTwoOperationTitle076));
            Assert.That(freshMarks.RouteTitle,
                Is.EqualTo("ROUTE A  •  " + M1FlowPresenter.ChapterTwoFreshMarksRouteTitle076));
            Assert.That(freshMarks.CurrentLocationName, Is.EqualTo("Patrol Runner"));
            Assert.That(freshMarks.StoryObjective, Does.Contain("false Wayglass marks"));
            Assert.That(freshMarks.StoryObjective, Does.Contain("survey crew"));
            Assert.That(freshMarksBeat, Does.Contain("survey crew"));
            Assert.That(freshMarksBeat.ToLowerInvariant(), Does.Not.Contain("missing patrol"));

            state.Expedition.ResolutionComplete = true;
            state.Expedition.CanMove = true;
            state.Expedition.LinkedNodeIds = new[] { "N03" };
            Assert.That(ExpeditionBoardProjection074.Build(state).StoryObjective,
                Does.Contain("unrecorded door"));

            state.Expedition = Expedition074("N04", "SKILL_CHECK", resolutionComplete: false);
            state.Expedition.BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;
            var bridge = ExpeditionBoardProjection074.Build(state);
            var bridgeBeat = ExpeditionBoardProjection074.CompanionStoryBeat076(
                state,
                bridge.Nodes.Single(value => value.IsCurrent));

            Assert.That(bridge.BoardTitle,
                Is.EqualTo(M1FlowPresenter.ChapterTwoArcTitle076));
            Assert.That(bridge.OperationTitle,
                Is.EqualTo("OPERATION 2  •  " + M1FlowPresenter.ChapterTwoOperationTitle076));
            Assert.That(bridge.RouteTitle,
                Is.EqualTo("ROUTE B  •  " + M1FlowPresenter.ChapterTwoBrokenBridgeRouteTitle076));
            Assert.That(bridge.CurrentLocationName, Is.EqualTo("A Bridge Measured Twice"));
            Assert.That(bridge.StoryObjective, Does.Contain("survey crew"));
            Assert.That(bridge.StoryObjective, Does.Contain("unrecorded door"));
            Assert.That(bridgeBeat, Does.Contain("unrecorded door"));
            Assert.That(bridgeBeat.ToLowerInvariant(), Does.Not.Contain("missing patrol"));

            state.Expedition.ResolutionComplete = true;
            state.Expedition.CanMove = true;
            state.Expedition.LinkedNodeIds = new[] { "N05" };
            Assert.That(ExpeditionBoardProjection074.Build(state).StoryObjective,
                Does.Contain("unrecorded door"));
        }

        [Test]
        public void SecondaryObjective_IsExplicitlyOptionalAndDoesNotClaimToBlockTheRoute079()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074(
                "N11",
                "SECONDARY_OBJECTIVE",
                resolutionComplete: false);

            var objective = ExpeditionBoardProjection074.Build(state).StoryObjective;

            Assert.That(objective, Does.StartWith("OPTIONAL"));
            Assert.That(objective, Does.Contain("main rescue does not depend on it"));
            Assert.That(objective,
                Does.Not.Contain("before choosing the next route"));
        }

        [Test]
        public void PhoneSimpleObjectiveUsesMoveForwardInsteadOfRouteChoice084()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074(
                "N03",
                "EVENT",
                resolutionComplete: true,
                canMove: true);
            state.Expedition.LinkedNodeIds = new[] { "N04" };

            var ready = ExpeditionBoardProjection074.Build(state).StoryObjective;
            Assert.That(ready, Does.StartWith("Move forward one room"));
            Assert.That(ready.ToLowerInvariant(), Does.Not.Contain("choose"));
            Assert.That(ready.ToLowerInvariant(), Does.Not.Contain("route"));

            state.Expedition.ResolutionComplete = false;
            state.Expedition.CanMove = false;
            var unresolved = ExpeditionBoardProjection074.Build(state).StoryObjective;
            Assert.That(unresolved, Does.Contain("Move Forward unlocks"));
            Assert.That(unresolved.ToLowerInvariant(), Does.Not.Contain("choose"));
        }

        [Test]
        public void FreshMarksRouteCardUsesEvidenceActionAndNeverMetaStoryRouteCopy079()
        {
            var node = new ExpeditionBoardNodeView074
            {
                NodeId = "N02",
                DisplayName = "Patrol Runner",
                Kind = "EVENT"
            };

            var title = ExpeditionBoardProjection074.RouteChoiceTitle079(
                ExpeditionBoardProjection074.ChapterTwoBoardId074,
                node);
            var subtitle = ExpeditionBoardProjection074.RouteChoiceSubtitle079(
                ExpeditionBoardProjection074.ChapterTwoBoardId074,
                node);

            Assert.That(title, Is.EqualTo("TEST SELLA'S WARNING"));
            Assert.That(subtitle,
                Is.EqualTo("VERIFY FALSE MARKS  •  RECOVER THE BRASS LINE"));
            Assert.That(title + " " + subtitle,
                Does.Not.Contain("PATROL RUNNER").And.Not.Contain("STORY ROUTE"));
        }

        [Test]
        public void PlayerFacingRouteAndStoryConsequencesRemoveImplementationJargon079()
        {
            var route = ExpeditionBoardProjection074.PlayerFacingRouteSummary079(
                "Break the fog-stalker cordon in a certified Union battle before it erases both routes.");
            var consequence = ExpeditionBoardProjection074.PlayerFacingStoryConsequence079(
                "Sella's trust, the seventh-return warning, and recoverable relationship memory");

            Assert.That(route, Does.Contain("Union battle"));
            Assert.That(route, Does.Not.Contain("certified"));
            Assert.That(consequence,
                Does.Contain("bond formed while hearing her warning"));
            Assert.That(consequence,
                Does.Not.Contain("recoverable relationship memory"));
        }

        [Test]
        public void OrraFieldBookOrdersNameActionsAndConsequencesWithoutProtectPressMetaLabels079()
        {
            var approaches = ExpeditionBoardProjection074.EventApproachLabels076(
                "EVENT_ABANDONED_SURVEY_PACK");
            var careful = ExpeditionBoardProjection074.ChapterTwoApproachDisplayLabel079(
                approaches[0],
                0,
                "EVENT_ABANDONED_SURVEY_PACK");
            var swift = ExpeditionBoardProjection074.ChapterTwoApproachDisplayLabel079(
                approaches[1],
                1,
                "EVENT_ABANDONED_SURVEY_PACK");

            Assert.That(careful,
                Does.StartWith("RECOVER ORRA'S FIELD BOOK SAFELY")
                    .And.Contain("SELLA HOLDS THE LINE")
                    .And.Contain("PARTNER +2")
                    .And.Contain("COST 1 PRESSURE"));
            Assert.That(swift,
                Does.StartWith("TAKE ORRA'S FIELD BOOK NOW")
                    .And.Contain("BEAT THE WATCHERS")
                    .And.Contain("LEAD +0")
                    .And.Contain("COST 0 PRESSURE"));
            Assert.That(careful, Does.Not.StartWith("PROTECT"));
            Assert.That(swift, Does.Not.StartWith("PRESS"));
            Assert.That(
                ExpeditionBoardProjection074.EventCommitActionLabel076(
                    "EVENT_ABANDONED_SURVEY_PACK",
                    0),
                Is.EqualTo("COMMIT  •  SECURE FIELD BOOK"),
                "The field-book action must remain one readable line so its promised outcome is visible.");
            Assert.That(
                ExpeditionBoardProjection074.EventCommitActionLabel076(
                    "EVENT_ABANDONED_SURVEY_PACK",
                    1),
                Is.EqualTo("COMMIT  •  SECURE FIELD BOOK"));
        }

        [Test]
        public void SellaThresholdCommitLeavesRoomForTheCompletePromisedOutcome080()
        {
            Assert.That(
                ExpeditionBoardProjection074.EventCommitActionLabel076(
                    "EVENT_FOUND_APPRENTICE",
                    0),
                Is.EqualTo("COMMIT  •  HEAR SELLA"));
            Assert.That(
                ExpeditionBoardProjection074.EventCommitActionLabel076(
                    "EVENT_FOUND_APPRENTICE",
                    1),
                Is.EqualTo("COMMIT  •  HEAR SELLA"));
        }

        [TestCase("MAREN  •  “Follow the brass line.”", "MAREN")]
        [TestCase("TALA  •  “The fresh marks are false.”", "TALA")]
        [TestCase("ZORIN  •  “The marks are wrong.”", "ZORIN")]
        [TestCase("ORREN  •  “The road moves below us.”", "ORREN")]
        [TestCase("JAZZI  •  “Bind the wounds first.”", "JAZZI")]
        [TestCase("TAZREN  •  “Their instruments are close.”", "TAZREN")]
        public void ChapterTwoSpeakerCardUsesTheNamedRosterSpeakerForAuditedBeats079(
            string beat,
            string speaker)
        {
            Assert.That(ExpeditionBoardProjection074.ChapterTwoCompanionSpeaker079(beat),
                Is.EqualTo(speaker));
            Assert.That(ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(beat),
                Is.True,
                speaker + " must use their roster portrait/name instead of Sella's identity card.");
        }

        [TestCase("SELLA  •  “I saw the seventh mark erased.”")]
        [TestCase("ORRA  •  “That is my brass line.”")]
        public void ChapterTwoWitnessesKeepTheirAuthoredStoryPortraits079(string beat)
        {
            Assert.That(ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(beat),
                Is.False);
        }

        [TestCase("N00", true)]
        [TestCase("N13", true)]
        [TestCase("N14", true)]
        [TestCase("N02", false)]
        [TestCase("N11", false)]
        public void ChapterTwoStoryAnchorsAppearOnlyAtOpeningRescueAndReturn080(
            string nodeId,
            bool expected)
        {
            Assert.That(
                ExpeditionBoardProjection074.RequiresChapterTwoStoryAnchor079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    nodeId),
                Is.EqualTo(expected));
            Assert.That(
                ExpeditionBoardProjection074.RequiresChapterTwoStoryAnchor079(
                    ExpeditionBoardProjection074.FirstBoardId074,
                    nodeId),
                Is.False,
                "Story-anchor rules must never leak into another board.");
        }

        [Test]
        public void AuditedChapterTwoNodesBindZorinAndOrrenWhileKeepingSellaInStoryCopy079()
        {
            var state = AvailableState074();
            state.Expedition = Expedition074("N03", "SCOUTING", resolutionComplete: true);
            state.Expedition.BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;
            var zorin = ExpeditionBoardProjection074.CompanionStoryBeat076(
                state,
                new ExpeditionBoardNodeView074 { NodeId = "N03" });

            var orren = ExpeditionBoardProjection074.CompanionStoryBeat076(
                state,
                new ExpeditionBoardNodeView074 { NodeId = "N10" });

            Assert.That(zorin, Does.StartWith("ZORIN").And.Contain("Sella"));
            Assert.That(orren, Does.StartWith("ORREN").And.Contain("Sella"));
            Assert.That(ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(zorin),
                Is.True);
            Assert.That(ExpeditionBoardProjection074.ChapterTwoUsesRosterSpeakerIdentity079(orren),
                Is.True);
        }

        [Test]
        public void FieldApproachLabelsStateTheExactPressureTradeoff076()
        {
            Assert.That(ExpeditionBoardProjection074.CarefulApproachLabel076,
                Is.EqualTo("CAREFUL\nPARTNER +2\nCOST 1 PRESSURE"));
            Assert.That(ExpeditionBoardProjection074.SwiftApproachLabel076,
                Is.EqualTo("SWIFT\nLEAD +0\nCOST 0 PRESSURE"));
            Assert.That(GuildCityExpeditionService017D.CarefulApproachModifier076, Is.EqualTo(2));
            Assert.That(GuildCityExpeditionService017D.CarefulApproachPressureCost076, Is.EqualTo(1));
            Assert.That(GuildCityExpeditionService017D.SwiftApproachModifier076, Is.Zero);
            Assert.That(GuildCityExpeditionService017D.CanUseCarefulApproach076(0), Is.False);
            Assert.That(GuildCityExpeditionService017D.CanUseCarefulApproach076(1), Is.True);
            Assert.That(
                ExpeditionBoardProjection074.CarefulApproachAvailabilityLabel076(
                ExpeditionBoardProjection074.CarefulApproachLabel076,
                    0,
                    hasFieldPartner: true),
                Does.Contain("LOCKED  •  NEED 1 PRESSURE")
                    .And.Not.Contain("COST 1 PRESSURE")
                    .And.Not.Contain("PARTNER +2"));
            Assert.That(
                ExpeditionBoardProjection074.CarefulApproachAvailabilityLabel076(
                    ExpeditionBoardProjection074.CarefulApproachLabel076,
                    1,
                    hasFieldPartner: true),
                Is.EqualTo(ExpeditionBoardProjection074.CarefulApproachLabel076));
        }

        [TestCase("EVENT_INJURED_COURIER", "UNA", "UNA'S BEARING")]
        [TestCase("EVENT_COLLAPSED_HANDRAIL", "QUIN'S RESCUE LINE", "LIFT QUIN")]
        [TestCase("EVENT_UNSTABLE_BELL_CHAIN", "ODELIA", "CLEAR THE ROAD")]
        [TestCase("EVENT_TRAPPED_FOREMAN", "PETRA", "PULL PETRA CLEAR")]
        [TestCase("EVENT_GATEGLASS_PULSE", "ORREN", "GARA'S SAFE PASSAGE")]
        [TestCase("EVENT_LANTERN_WATCH_CAMP", "JAZZI", "ZORIN'S RESCUE ORDER")]
        public void ChapterOneFieldChoicesNameThePersonAndConcreteOrder079(
            string eventId,
            string carefulStoryFragment,
            string swiftStoryFragment)
        {
            var approaches = ExpeditionBoardProjection074.EventApproachLabels076(eventId);

            Assert.That(approaches, Has.Count.EqualTo(2));
            Assert.That(approaches[0], Does.Contain(carefulStoryFragment));
            Assert.That(approaches[0], Does.Contain("PARTNER +2"));
            Assert.That(approaches[0], Does.Contain("COST 1 PRESSURE"));
            Assert.That(approaches[1], Does.Contain(swiftStoryFragment));
            Assert.That(approaches[1], Does.Contain("LEAD +0"));
            Assert.That(approaches[1], Does.Contain("COST 0 PRESSURE"));
            Assert.That(approaches[0], Is.Not.EqualTo(ExpeditionBoardProjection074.CarefulApproachLabel076));
            Assert.That(approaches[1], Is.Not.EqualTo(ExpeditionBoardProjection074.SwiftApproachLabel076));
            Assert.That(ExpeditionBoardProjection074.EventCommitActionLabel076(eventId, 0),
                Does.Contain(carefulStoryFragment));
            Assert.That(ExpeditionBoardProjection074.EventCommitActionLabel076(eventId, 1),
                Does.Contain(swiftStoryFragment));
        }

        [Test]
        public void DedicatedCampEventsKeepEachChapterOnItsOwnStory079()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N07", "CAMP", resolutionComplete: false);
            state.Expedition.CurrentEventId = "EVENT_LANTERN_WATCH_CAMP";
            state.Expedition.CurrentEventTitle = "Maren's Lantern Watch";
            state.Expedition.CurrentEventProblem =
                "At Lantern Watch, Maren and Jazzi ready the Unions to bring Zorin home.";
            state.Expedition.CurrentEventConsequence =
                "Maren's Lantern Watch and Zorin's rescue order";
            state.Expedition.CurrentEventUsesCommitted2d6 = true;
            var firstHour = ExpeditionBoardProjection074.Build(state);
            var firstHourCamp = firstHour.Nodes.Single(value => value.IsCurrent);

            var firstHourProblem = ExpeditionBoardProjection074.CurrentMomentSummary076(
                state,
                firstHourCamp);
            Assert.That(firstHourProblem, Does.Contain("Maren").And.Contain("Jazzi").And.Contain("Zorin"));
            Assert.That(firstHourProblem, Does.Not.Contain("Sella").And.Not.Contain("Orra"));

            state.Expedition.ResolutionComplete = true;
            state.Expedition.HasCommittedCheckAtCurrentNode = true;
            state.Expedition.LastCheckActorRecruitId = "R1";
            state.Expedition.LastCheckAssistantRecruitId = "R2";
            state.Expedition.LastCheckDieOne = 3;
            state.Expedition.LastCheckDieTwo = 4;
            state.Expedition.LastCheckTotal = 7;
            state.Expedition.LastCheckOutcome = "SUCCESS_WITH_COST";
            state.Expedition.CurrentEventOutcomeText =
                "Maren keeps Lantern Watch together while Jazzi tends the strain; Zorin's rescue remains the shared order.";
            var firstHourResult = ExpeditionBoardProjection074.CurrentMomentSummary076(
                state,
                firstHourCamp);
            Assert.That(firstHourResult,
                Does.Contain("Maren").And.Contain("Lantern Watch")
                    .And.Contain("Jazzi").And.Contain("Zorin")
                    .And.Not.Contain("Sella").And.Not.Contain("Orra"));
            Assert.That(ExpeditionBoardProjection074.ProgressConsequence076(state, firstHourCamp),
                Is.EqualTo("LAST RESULT  ✓  SUCCESS WITH A COST  •  UNIONS RESTED  •  ZORIN RESCUE ORDER AGREED"));

            state.Expedition.BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;
            state.Expedition.ResolutionComplete = false;
            state.Expedition.HasCommittedCheckAtCurrentNode = false;
            state.Expedition.CurrentEventId = "EVENT_CAMP_ARGUMENT";
            state.Expedition.CurrentEventTitle = "The Seventh Name at Camp";
            state.Expedition.CurrentEventProblem =
                "Sella Vey names Orra Vale and the erased seventh-return mark.";
            state.Expedition.CurrentEventConsequence = "Sella's trust and Orra's warning";
            state.Expedition.CurrentEventUsesCommitted2d6 = true;
            var chapterTwo = ExpeditionBoardProjection074.Build(state);
            Assert.That(ExpeditionBoardProjection074.CurrentMomentSummary076(
                    state,
                    chapterTwo.Nodes.Single(value => value.IsCurrent)),
                Does.Contain("Sella Vey").And.Contain("Orra Vale"),
                "Chapter 2 must retain its named witness scene while using committed 2d6.");
        }

        [TestCase("N00", "SELLA VEY", "WAYGLASS WITNESS",
            ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079)]
        [TestCase("N12", "SELLA VEY", "SURVEY APPRENTICE",
            ExpeditionBoardProjection074.ChapterTwoSellaPortraitResource079)]
        [TestCase("N13", "ORRA VALE", "RESCUE OBJECTIVE",
            ExpeditionBoardProjection074.ChapterTwoOrraPortraitResource079)]
        [TestCase("N14", "ORRA VALE", "RESCUED",
            ExpeditionBoardProjection074.ChapterTwoOrraPortraitResource079)]
        public void ChapterTwoStoryIdentityTracksTheWitnessAndRescueObjective079(
            string nodeId,
            string expectedName,
            string expectedRoleFragment,
            string expectedPortraitResource)
        {
            var boardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;

            Assert.That(
                ExpeditionBoardProjection074.ChapterTwoStoryIdentityName079(boardId, nodeId),
                Is.EqualTo(expectedName));
            Assert.That(
                ExpeditionBoardProjection074.ChapterTwoStoryIdentityRole079(boardId, nodeId),
                Does.Contain(expectedRoleFragment));
            var displayText = ExpeditionBoardProjection074
                .ChapterTwoStoryIdentityDisplayText079(boardId, nodeId);
            Assert.That(displayText, Does.StartWith(expectedName + "\n"));
            Assert.That(displayText.Split('\n'),
                Has.Length.EqualTo(nodeId == "N14" ? 4 : 5));
            Assert.That(displayText, Does.Not.Contain("•"));
            foreach (var expectedWord in expectedRoleFragment.Split(' '))
                Assert.That(displayText, Does.Contain(expectedWord));
            Assert.That(
                ExpeditionBoardProjection074.ChapterTwoStoryIdentityPortraitResource079(
                    boardId,
                    nodeId),
                Is.EqualTo(expectedPortraitResource));
            Assert.That(
                SecondDimension.Presentation.GuildCity017E
                    .GuildCityOpeningExperienceRegistry017E.Sprite(expectedPortraitResource),
                Is.Not.Null,
                expectedName + " portrait must resolve through the production Resources path.");
        }

        [Test]
        public void ChapterTwoDecisionFramingUsesOneStorySceneAndProtectPressOrders079()
        {
            Assert.That(ExpeditionBoardProjection074.ChapterTwoStorySceneHeading079,
                Is.EqualTo("STORY SCENE"));
            Assert.That(ExpeditionBoardProjection074.ChapterTwoFieldOrderHeading079,
                Is.EqualTo("FIELD ORDER"));
            Assert.That(
                ExpeditionBoardProjection074.ChapterTwoApproachDisplayLabel079(
                    "KEEP SELLA SAFE\nPARTNER +2  •  COST 1 PRESSURE",
                    0),
                Does.StartWith("PROTECT  •  KEEP SELLA SAFE"));
            Assert.That(
                ExpeditionBoardProjection074.ChapterTwoApproachDisplayLabel079(
                    "FOLLOW THE BRASS LINE\nLEAD +0  •  COST 0 PRESSURE",
                    1),
                Does.StartWith("PRESS  •  FOLLOW THE BRASS LINE"));
        }

        [Test]
        public void ChapterTwoRouteHeadingFramesAuthoredPeopleAndConsequences079()
        {
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    "N01",
                    false,
                    2),
                Is.EqualTo("CHOOSE WHO YOU BACK"));
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    "N10",
                    false,
                    2),
                Is.EqualTo("CHOOSE THE GUILD'S PRIORITY"));
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    ExpeditionBoardProjection074.FirstBoardId074,
                    "N01",
                    false,
                    2),
                Is.EqualTo("CHOOSE A ROUTE"),
                "Non-Chapter-2 route panels retain their generic reusable framing.");
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    "N09",
                    true,
                    1),
                Is.EqualTo("FIGHT OR TAKE THE BYPASS"));
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionHeading079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    "N13",
                    false,
                    1),
                Is.EqualTo("STORY ROUTE  •  REQUIRED"));
        }

        [Test]
        public void ChapterTwoRouteCardsTestEvidenceAndShowImmediateOrderConsequences079()
        {
            var sella = new ExpeditionBoardNodeView074
            {
                NodeId = "N02",
                DisplayName = "Fresh Marks on an Old Wall",
                Summary = "Compare the reversed paint to Orra's seven-notch brass tag.",
                RouteCostSummary = "Supplies 1 • Fatigue 0 • Urgency 1"
            };
            var orra = new ExpeditionBoardNodeView074
            {
                NodeId = "N04",
                DisplayName = "A Bridge Measured Twice",
                Summary = "Stabilize Orra's five-pin bridge and preserve sabotage evidence.",
                RouteCostSummary = "Supplies 0 • Fatigue 1 • Urgency 0"
            };

            Assert.That(
                ExpeditionBoardProjection074.RouteChoiceTitle079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    sella),
                Is.EqualTo("TEST SELLA'S WARNING"));
            Assert.That(
                ExpeditionBoardProjection074.RouteChoiceTitle079(
                    ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    orra),
                Is.EqualTo("PROTECT ORRA'S BRASS LINE"));

            var sellaConsequence = ExpeditionBoardProjection074.RouteDecisionSummary079(
                ExpeditionBoardProjection074.ChapterTwoBoardId074,
                sella);
            Assert.That(sellaConsequence, Does.StartWith("ORDER CONSEQUENCE"));
            Assert.That(sellaConsequence, Does.Contain("SELLA'S WARNING"));
            Assert.That(sellaConsequence, Does.Contain("Orra's seven-notch brass tag"));
            Assert.That(sellaConsequence, Does.Not.Contain("DESTINATION").IgnoreCase);
            Assert.That(sellaConsequence, Does.Not.Contain("FORECAST").IgnoreCase);

            var emptyChapterTwo = ExpeditionBoardProjection074.RouteDecisionSummary079(
                ExpeditionBoardProjection074.ChapterTwoBoardId074,
                null);
            Assert.That(emptyChapterTwo,
                Is.EqualTo("SELECT AN ORDER TO SEE WHO IT HELPS AND WHAT EVIDENCE IT PRESERVES."));
            Assert.That(
                ExpeditionBoardProjection074.RouteDecisionSummary079(
                    ExpeditionBoardProjection074.FirstBoardId074,
                    sella),
                Does.StartWith("ARRIVAL FORECAST"),
                "The reusable non-Chapter-2 board may retain its generic travel framing.");
        }

        [TestCase("N01", "ENCOUNTER", "ENCOUNTER071_HALL_BREACH", "HALL BREACH")]
        [TestCase("N06", "ENCOUNTER", "ENCOUNTER071_LANTERN_ROAD_AMBUSH", "LANTERN ROAD AMBUSH")]
        [TestCase("N13", "MAIN_OBJECTIVE", "ENCOUNTER071_GATE_EATER", "GATE-EATER")]
        public void FirstHourBattleActions_NameTheExactStoryBattle(
            string nodeId,
            string nodeKind,
            string encounterId,
            string battleName)
        {
            var state = AvailableState074();
            state.Expedition = Expedition074(nodeId, nodeKind, canCommitEncounter: true);
            state.Expedition.CurrentEncounterId = encounterId;

            Assert.That(ExpeditionBoardProjection074.StoryBattleName074(state), Is.EqualTo(battleName));
            Assert.That(ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: false),
                Is.EqualTo("COMMIT  •  " + battleName));
            Assert.That(ExpeditionBoardProjection074.BattleActionLabel074(state, enterBattle: true),
                Is.EqualTo("ENTER BATTLE  •  " + battleName));
        }

        [Test]
        public void RescueCeremony_DistinguishesGatePresenceFieldCapacityAndGuildRoster()
        {
            var copy = ExpeditionBoardProjection074.RescueCeremonyRosterTruth076;

            Assert.That(copy, Does.Contain("Nineteen stand at this gate"));
            Assert.That(copy, Does.Contain("Eighteen are assigned to this fight"));
            Assert.That(copy, Does.Contain("Bessa holds the Hall"));
            Assert.That(copy, Does.Contain("All twenty belong to the Guild"));
            Assert.That(copy, Does.Contain("field sixty"));
            Assert.That(copy.ToLowerInvariant(), Does.Not.Contain("all twenty of us finish"));
            Assert.That(ExpeditionBoardProjection074.RescueCeremonyHeading076(0),
                Does.Contain("PAGE 1/2"));
            Assert.That(ExpeditionBoardProjection074.RescueCeremonyHeading076(1),
                Does.Contain("PAGE 2/2"));
        }

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void RescueCeremonyUsesOneReadableSurvivorHeroAndFourSeparatedCompanions(
            int width,
            int height)
        {
            var hero = M1FlowPresenter.LanternPatrolHeroRectForVerification076;
            var story = M1FlowPresenter.LanternPatrolStoryRectForVerification076;
            var companions = M1FlowPresenter.LanternPatrolCompanionRectsForVerification076();

            Assert.That(companions, Has.Count.EqualTo(4));
            Assert.That(hero.width * width, Is.GreaterThanOrEqualTo(410f));
            Assert.That(hero.height * height, Is.GreaterThanOrEqualTo(450f));
            Assert.That(hero.Overlaps(story), Is.False);
            foreach (var companion in companions)
            {
                Assert.That(companion.width * width, Is.GreaterThanOrEqualTo(180f));
                Assert.That(companion.height * height, Is.GreaterThanOrEqualTo(275f));
                Assert.That(companion.Overlaps(hero), Is.False);
                Assert.That(companion.Overlaps(story), Is.False);
            }
            for (var left = 0; left < companions.Count; left++)
            for (var right = left + 1; right < companions.Count; right++)
                Assert.That(companions[left].Overlaps(companions[right]), Is.False);
        }

        [Test]
        public void FirstHourFiveBeats_UseDistinctStoryBackdropsWithDeterministicFallback()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            var expectations = new[]
            {
                ("N00", ExpeditionBoardProjection074.ExpeditionHallBackdropResource076),
                ("N02", ExpeditionBoardProjection074.GuildUndercroftLedgerBackdropResource086),
                ("N04", ExpeditionBoardProjection074.ExpeditionLanternRoadBackdropResource076),
                ("N05", ExpeditionBoardProjection074.LanternRoadCacheBackdropResource086),
                ("N06", ExpeditionBoardProjection074.ExpeditionLanternRoadBackdropResource076),
                ("N10", ExpeditionBoardProjection074.WayglassThresholdBackdropResource086),
                ("N13", ExpeditionBoardProjection074.ExpeditionGatehouseBackdropResource076),
                ("N14", ExpeditionBoardProjection074.ExpeditionHallBackdropResource076)
            };

            foreach (var expectation in expectations)
            {
                state.Expedition = Expedition074(expectation.Item1, "SAFE_ROUTE");
                var view = ExpeditionBoardProjection074.Build(state);
                Assert.That(
                    ExpeditionBoardProjection074.StoryBackdropResource076(view),
                    Is.EqualTo(expectation.Item2),
                    "Unexpected first-hour backdrop for " + expectation.Item1 + ".");
            }

            state.Expedition = Expedition074("N04", "SKILL_CHECK");
            state.Expedition.BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074;
            var chapterTwo = ExpeditionBoardProjection074.Build(state);
            Assert.That(ExpeditionBoardProjection074.StoryBackdropResource076(chapterTwo),
                Is.EqualTo(ExpeditionBoardProjection074.BrokenSurveyBridgeBackdropResource086),
                "The fixed Chapter 2 story path must use graph-free route art, not the archival node-ledger image.");
            Assert.That(chapterTwo.MapResourcePath,
                Is.Not.EqualTo(ExpeditionBoardProjection074.StoryBackdropResource076(chapterTwo)),
                "The test fixture must prove the unsafe authored map is actively superseded.");
        }

        [Test]
        public void AuthoredStoryRoomsNeverRepeatTheSameBackdropOnConsecutiveSteps086()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            var ranges = new[]
            {
                new
                {
                    BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                    FirstNode = 8,
                    LastNode = 12
                },
                new
                {
                    BoardId = ExpeditionBoardProjection074.ChapterTwoBoardId074,
                    FirstNode = 0,
                    LastNode = 13
                }
            };
            foreach (var range in ranges)
            {
                string previous = null;
                for (var node = range.FirstNode; node <= range.LastNode; node++)
                {
                    state.Expedition = Expedition074(
                        "N" + node.ToString("00"),
                        node == 13 ? "MAIN_OBJECTIVE" : "EVENT");
                    state.Expedition.BoardId = range.BoardId;
                    var view = ExpeditionBoardProjection074.Build(state);
                    var current = ExpeditionBoardProjection074.StoryBackdropResource076(view);
                    Assert.That(current, Is.Not.EqualTo(previous),
                        range.BoardId + " repeats its room backdrop between N" +
                        (node - 1).ToString("00") + " and N" + node.ToString("00") + ".");
                    previous = current;
                }
            }
        }

        [Test]
        public void RouteDecisionAndFieldStatus_AreBoundedTwoStageCopyWithoutDanglingBullets()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N00", "START", canMove: true);
            state.Expedition.LinkedNodeIds = new[] { "N01" };
            state.Expedition.Supplies = 12;
            state.Expedition.Fatigue = 0;
            state.Expedition.Threat = 0;
            state.Expedition.Urgency = 14;
            var view = ExpeditionBoardProjection074.Build(state);
            var destination = view.Nodes.Single(value => value.NodeId == "N01");

            var routeCopy = ExpeditionBoardProjection074.RouteDecisionSummary076(destination);
            var fieldCopy = ExpeditionBoardProjection074.FieldConditionCopy076(
                state.Expedition,
                view.CurrentLocationName);

            Assert.That(routeCopy, Does.StartWith("ARRIVAL FORECAST"));
            Assert.That(routeCopy, Does.Contain("SUPPLIES −1"));
            Assert.That(routeCopy, Does.Contain("FATIGUE +1"));
            Assert.That(routeCopy, Does.Contain("PRESSURE −1"));
            Assert.That(routeCopy, Does.Contain("\nNEXT  •"));
            Assert.That(routeCopy, Does.Contain("Battle 1 of 3"));
            Assert.That(routeCopy, Does.EndWith("SAVE  ✓  AUTOSAVE ON ARRIVAL"));
            Assert.That(routeCopy, Does.Not.Contain(destination.DisplayName.ToUpperInvariant()),
                "The selected route card already owns the destination name; the bounded summary should explain consequence instead of repeating it.");
            Assert.That(M1FlowPresenter.ExpeditionRouteSummaryHeightForVerification074(),
                Is.GreaterThanOrEqualTo(272f),
                "The selected route consequence needs eight readable wrapped lines plus headroom before the CTA at both certification frames.");
            Assert.That(fieldCopy.Split('\n'), Has.Length.EqualTo(2));
            Assert.That(fieldCopy, Does.Contain("PRESSURE 14\nSAVE  ✓"));
            Assert.That(fieldCopy, Does.Not.Contain("•\n"));
            Assert.That(fieldCopy, Does.EndWith("LOWER HALL THRESHOLD AUTOSAVED"));

            Assert.That(
                ExpeditionBoardProjection074.RouteCostForecast076(
                    "Supplies 0 • Fatigue 0 • Urgency 0"),
                Is.EqualTo("SUPPLIES ±0  •  FATIGUE ±0  •  PRESSURE ±0"),
                "A zero-cost route must not imply that any resource moves up or down.");
        }

        [Test]
        public void FirstHourProgressCopy_StatesConsequencesAndOrdersPatrolBeforeBoss()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N04", "SKILL_CHECK");
            state.Expedition.ObjectiveFlags = new[] { "ENCOUNTER_CLEARED_N01" };
            var track = ExpeditionBoardProjection074.Build(state);
            Assert.That(ExpeditionBoardProjection074.ProgressConsequence076(
                    state,
                    track.Nodes.Single(value => value.IsCurrent)),
                Is.EqualTo("LAST RESULT  ✓  HALL BREACH WON  •  LANTERN ROAD OPEN"));

            state.Expedition = Expedition074("N13", "MAIN_OBJECTIVE", canCommitEncounter: true);
            state.Expedition.ObjectiveFlags = new[] { "ENCOUNTER_CLEARED_N06" };
            var secure = ExpeditionBoardProjection074.Build(state);
            var current = secure.Nodes.Single(value => value.IsCurrent);

            Assert.That(ExpeditionBoardProjection074.CurrentMomentSummary076(state, current),
                Does.StartWith("Rally Zorin's trapped patrol"));
            Assert.That(ExpeditionBoardProjection074.PatrolFoundDecisionCopy076,
                Does.Contain("Rally the survivors first"));
            Assert.That(ExpeditionBoardProjection074.PatrolFoundDecisionCopy076,
                Does.Not.Contain("recovered Wayglass"));
            Assert.That(ExpeditionBoardProjection074.ProgressConsequence076(state, current),
                Is.EqualTo("LAST RESULT  ✓  LANTERN ROAD AMBUSH WON  •  ROAD TO ZORIN OPEN"));

            state.Expedition.ObjectiveFlags = new[]
            {
                GuildCityExpeditionService017D.FirstHourLanternPatrolRescuedFlag071
            };
            Assert.That(ExpeditionBoardProjection074.CurrentMomentSummary076(state, current),
                Does.StartWith("The patrol and Wayglass are secure"));
            Assert.That(ExpeditionBoardProjection074.ProgressConsequence076(state, current),
                Does.Contain("LANTERN PATROL RALLIED"));
        }

        [Test]
        public void ClearedEncounter_ReportsExactSavedReturnPosition()
        {
            var state = ActiveRouteState074();
            state.Expedition.ObjectiveFlags = new[] { "ENCOUNTER_CLEARED_N06" };

            var view = ExpeditionBoardProjection074.Build(state);

            Assert.That(ExpeditionBoardProjection074.HasReturnedToCurrentNode074(state), Is.True);
            Assert.That(view.PositionStatus, Does.Contain("POSITION HELD"));
            Assert.That(view.PositionStatus, Does.Contain(view.CurrentLocationName.ToUpperInvariant()));
        }

        [Test]
        public void FifteenSaveNodes_ProjectIntoFiveReadableOperationPhases()
        {
            var view = ExpeditionBoardProjection074.Build(ActiveRouteState074());

            Assert.That(ExpeditionBoardProjection074.MinimumSupportedAspect074, Is.EqualTo(1.6f));
            Assert.That(ExpeditionBoardProjection074.ChapterPhaseCount074, Is.EqualTo(5));
            Assert.That(view.Nodes
                    .Select(value => ExpeditionBoardProjection074.PhaseIndexForNode074(value.NodeId))
                    .Distinct()
                    .Count(),
                Is.EqualTo(5));
            Assert.That(view.Nodes.Select(value =>
                    ExpeditionBoardProjection074.StepNumberForNode074(value.NodeId)),
                Is.EqualTo(Enumerable.Range(1, 15)));
            Assert.That(Enumerable.Range(0, ExpeditionBoardProjection074.ChapterPhaseCount074)
                    .All(index => !string.IsNullOrWhiteSpace(
                        ExpeditionBoardProjection074.PhaseTitle074(index))),
                Is.True);
            Assert.That(ExpeditionBoardProjection074.PhaseIndexForNode074("N06"), Is.EqualTo(2));
            Assert.That(ExpeditionBoardProjection074.PhaseIndexForNode074("N13"), Is.EqualTo(3));
            Assert.That(ExpeditionBoardProjection074.PhaseIndexForNode074("N14"), Is.EqualTo(4));
            Assert.That(ExpeditionBoardProjection074.ChapterLabel074(
                ExpeditionBoardProjection074.FirstBoardId074), Is.EqualTo("CHAPTER 1"));
            Assert.That(ExpeditionBoardProjection074.ChapterLabel074(
                "BOARD_LINES_NOT_RETURNED"), Is.EqualTo("CHAPTER 2"));
        }

        [TestCase(ExpeditionBoardActionKind074.ChooseContract)]
        [TestCase(ExpeditionBoardActionKind074.BeginExpedition)]
        [TestCase(ExpeditionBoardActionKind074.ResolveCheck)]
        [TestCase(ExpeditionBoardActionKind074.CommitEncounter)]
        [TestCase(ExpeditionBoardActionKind074.EnterBattle)]
        [TestCase(ExpeditionBoardActionKind074.AwaitBattleReturn)]
        [TestCase(ExpeditionBoardActionKind074.ChooseRoute)]
        [TestCase(ExpeditionBoardActionKind074.FinalizeOperation)]
        [TestCase(ExpeditionBoardActionKind074.ResolveObjective)]
        [TestCase(ExpeditionBoardActionKind074.Regroup)]
        public void EveryContextStateFitsFixedPanelAtMinimumSupportedHeight(
            ExpeditionBoardActionKind074 actionKind)
        {
            var available = M1FlowPresenter.ExpeditionContextAvailableHeightForVerification074(
                1280f,
                M1FlowPresenter.ExpeditionContextMinimumScreenHeight074);
            var required = M1FlowPresenter.ExpeditionContextRequiredHeightForVerification074(actionKind);

            Assert.That(required, Is.LessThanOrEqualTo(available),
                actionKind + " requires " + required + " px but the fixed context panel provides only " +
                 available + " px at 1280x800.");
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 800f)]
        public void FieldDecisionHeadingsAndTouchCardsFitBothCertificationFrames078(
            float width,
            float height)
        {
            var available = M1FlowPresenter.ExpeditionContextAvailableHeightForVerification074(
                width,
                height);
            var required = M1FlowPresenter.ExpeditionContextRequiredHeightForVerification074(
                ExpeditionBoardActionKind074.ResolveCheck);

            Assert.That(required, Is.LessThanOrEqualTo(available),
                "The complete field-lead and field-partner decision needs " + required +
                " layout units but owns only " + available + " at " + width + "x" + height + ".");
            Assert.That(M1FlowPresenter.ExpeditionFieldLeadLabel078, Is.EqualTo("FIELD LEAD"));
            Assert.That(M1FlowPresenter.ExpeditionFieldPartnerLabel078, Is.EqualTo("FIELD PARTNER"));
            Assert.That(M1FlowPresenter.ExpeditionTopFitLabel078, Is.EqualTo("TOP FIT"));
            Assert.That(M1FlowPresenter.ExpeditionTopFitLabel078, Does.Not.Contain("RECOMMENDED"));
            Assert.That(M1FlowPresenter.ExpeditionCompanionVoiceLabel078,
                Is.EqualTo("COMPANION VOICE"));
        }

        [Test]
        public void ThreeImmediateDecisionCardsFitAt1280By800()
        {
            var available = M1FlowPresenter.ExpeditionDecisionAvailableWidthForVerification074(
                1280f,
                800f);
            var required = M1FlowPresenter.ExpeditionDecisionRequiredWidthForVerification074();

            Assert.That(M1FlowPresenter.ExpeditionMaximumDecisionCards074, Is.EqualTo(3));
            Assert.That(required, Is.LessThanOrEqualTo(available),
                "Three immediate route cards require " + required +
                " px but the fixed next-order panel provides only " + available +
                " px at 1280x800.");
        }

        private static void AssertAction074(
            GuildCityPresentationState017D state,
            ExpeditionBoardActionKind074 expected)
        {
            Assert.That(ExpeditionBoardProjection074.Build(state).ActionKind, Is.EqualTo(expected));
        }

        private static GuildCityPresentationState017D ActiveRouteState074()
        {
            var state = AvailableState074();
            state.HasActiveContract = true;
            state.Expedition = Expedition074("N06", "ENCOUNTER", canMove: true);
            state.Expedition.LinkedNodeIds = new[] { "N07" };
            state.Expedition.VisitedNodeIds = new[]
                { "N00", "N01", "N02", "N03", "N04", "N05", "N06" };
            state.Expedition.RevealedNodeIds = new[]
                { "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07" };
            return state;
        }

        private static GuildCityPresentationState017D AvailableState074()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                Assignments = Array.Empty<GuildCityAssignmentView017D>()
            };
        }

        private static GuildCityExpeditionView017D Expedition074(
            string currentNodeId,
            string currentKind,
            bool resolutionComplete = true,
            bool canMove = false,
            bool canCommitEncounter = false,
            bool canFinalize = false)
        {
            return new GuildCityExpeditionView017D
            {
                BoardId = ExpeditionBoardProjection074.FirstBoardId074,
                CurrentNodeId = currentNodeId,
                CurrentNodeKind = currentKind,
                CurrentEventId = currentKind == "SKILL_CHECK" ? "EVENT_COLLAPSED_HANDRAIL" : string.Empty,
                CurrentEncounterId = currentKind == "ENCOUNTER" || currentKind == "MAIN_OBJECTIVE" ||
                                     currentKind == "OPTIONAL_ELITE"
                    ? "ENCOUNTER074"
                    : string.Empty,
                ResolutionComplete = resolutionComplete,
                CanMove = canMove,
                CanCommitEncounter = canCommitEncounter,
                CanFinalizeOperation = canFinalize,
                LinkedNodeIds = Array.Empty<string>(),
                VisitedNodeIds = new[] { currentNodeId },
                RevealedNodeIds = new[] { currentNodeId },
                ObjectiveFlags = Array.Empty<string>()
            };
        }
    }
}
