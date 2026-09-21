using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityImplementation017DTests
    {
        private GuildCityContent017D _content;
        private GuildCityCommandService017D _city;
        private GuildCityExpeditionService017D _expedition;
        private RecruitmentContent _recruitmentContent;
        private GuildCityRecruitmentService017D _recruitment;
        private M2CombatContent _combatContent;
        private M2BattleCommandService _battles;
        private GuildCityBattleBridgeService017D _bridge;

        [SetUp]
        public void SetUp()
        {
            _content = GuildCityContent017D.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT","GUILD_CITY_017D"));
            _city = new GuildCityCommandService017D();
            _expedition = new GuildCityExpeditionService017D();
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _recruitmentContent = RecruitmentContent.LoadFromDirectory(contentRoot);
            _recruitment = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)));
            _combatContent = M2CombatContent.LoadFromDirectory(contentRoot);
            _battles = new M2BattleCommandService();
            _bridge = new GuildCityBattleBridgeService017D();
        }

        [Test] public void OpeningCityHasTwelvePlotsAndSixUnlocked()
        {
            var state=CreateCampaign().Guild.GuildCity; var unlocked=0; for(var i=0;i<state.CityPlots.Count;i++)if(state.CityPlots[i].Unlocked)unlocked++;
            Assert.That(state.CityPlots.Count,Is.EqualTo(12)); Assert.That(unlocked,Is.EqualTo(6)); Assert.That(state.CharterBuildCredits,Is.EqualTo(1));
        }

        [Test] public void MeaningfulOperationProgressUnlocksTheNextCityPlotWithoutWaiting()
        {
            var initial = CreateCampaign().Guild.GuildCity.CityPlots;
            var afterBattle = GuildCityCommandService017D.AdvanceOpeningCityProject(initial, 20);
            Assert.That(afterBattle[6].Unlocked, Is.False);
            Assert.That(afterBattle[6].ConstructionProgress, Is.EqualTo(20));

            var afterOperation = GuildCityCommandService017D.AdvanceOpeningCityProject(afterBattle, 30);
            Assert.That(afterOperation[6].Unlocked, Is.True);
            Assert.That(afterOperation[6].ConstructionProgress,
                Is.EqualTo(GuildCityCommandService017D.OpeningPlotUnlockProgress));
            Assert.That(afterOperation.Count(value => value.Unlocked), Is.EqualTo(7));
        }

        [Test] public void SignedNormalRecruitCannotBeAutomaticallyRemoved()
        {
            Assert.That(GuildMemberRetentionPolicy017D.CanAutomaticallyRemoveNormalRecruit("LOW_MORALE"),Is.False);
            Assert.That(GuildMemberRetentionPolicy017D.CanAutomaticallyRemoveNormalRecruit("RELATIONSHIP_FRICTION"),Is.False);
            Assert.That(GuildMemberRetentionPolicy017D.EmptyWaitOperationAllowed,Is.False);
        }

        [Test] public void RelationshipSceneCostsNoOperationAndCanBeDeferred()
        {
            var campaign=CreateCampaign(); var before=campaign.Guild.GuildCity.OperationOrdinal;
            campaign=Require(_city.AddRelationshipMemory(campaign,"R1","R2","CAMP_TEST","They chose the route together.",2,"SCENE_CAMP_TEST"));
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,Is.EqualTo(before));
            campaign=Require(_city.ViewRelationshipScene(campaign,"SCENE_CAMP_TEST"));
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,Is.EqualTo(before));
            Assert.That(campaign.Guild.GuildCity.RelationshipMemories[0].Viewed,Is.True);
        }

        [Test] public void FirstBuildingUsesCharterCreditAndStaffCanReturnToDeployment()
        {
            var campaign=CreateCampaign();
            campaign=Require(_city.PlaceBuilding(campaign,_content,"GC017D_PLOT_01","GC017D_BUILD_RECRUITMENT_OFFICE"));
            Assert.That(campaign.Guild.GuildCity.CharterBuildCredits,Is.Zero);
            campaign=Require(_city.AssignStaff(campaign,"GC017D_PLOT_01","R1"));
            Assert.That(campaign.Guild.GuildCity.CityPlots[0].StaffRecruitIds,Contains.Item("R1"));
            campaign=Require(_city.DeployStaffMember(campaign,"R1"));
            Assert.That(campaign.Guild.GuildCity.CityPlots[0].StaffRecruitIds,Does.Not.Contain("R1"));
            Assert.That(Assignment(campaign,"R1").Kind,Is.EqualTo(GuildMemberAssignmentKind017D.Active));
        }

        [Test] public void ContractBoardAndChecksAreCommittedDeterministically()
        {
            var first=Require(_expedition.AcceptContract(CreateBattleReadyCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            var second=Require(_expedition.AcceptContract(CreateBattleReadyCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            Assert.That(CanonicalJson.Serialize(first.Guild.GuildCity.ActiveContract),Is.EqualTo(CanonicalJson.Serialize(second.Guild.GuildCity.ActiveContract)));
            first=ReachFirstHourEngineeringCheck071(first);
            var checkedOnce=Require(_expedition.ResolveCommittedCheck(first,_content,"EVENT_COLLAPSED_HANDRAIL","R1","R2",2));
            var checkedTwice=Require(_expedition.ResolveCommittedCheck(checkedOnce,_content,"EVENT_COLLAPSED_HANDRAIL","R1","R2",2));
            Assert.That(CanonicalJson.Serialize(checkedTwice.Guild.GuildCity.Expedition.CommittedChecks),Is.EqualTo(CanonicalJson.Serialize(checkedOnce.Guild.GuildCity.Expedition.CommittedChecks)));
            var atSecret = WithLegacyExpeditionAt(checkedTwice, "N05",
                new[] { "N00", "N01", "N04", "N05" }, new[] { "N05", "N06" });
            var suppliesBeforeSecret = atSecret.Guild.GuildCity.Expedition.Supplies;
            var secretOnce = Require(_expedition.DiscoverGateworksMaintenancePassage066(atSecret));
            var secretTwice = Require(_expedition.DiscoverGateworksMaintenancePassage066(secretOnce));
            Assert.That(secretOnce.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.GateworksMaintenancePassageFlag066));
            Assert.That(secretOnce.Guild.GuildCity.Expedition.Supplies, Is.EqualTo(suppliesBeforeSecret + 1));
            Assert.That(CanonicalJson.Serialize(secretTwice), Is.EqualTo(CanonicalJson.Serialize(secretOnce)),
                "Reopening the discovered passage must not grant the supply reward twice.");
        }

        [Test] public void FieldApproachesTradePressureForAssistanceAndCommitOnce076()
        {
            var carefulReady = Require(_expedition.AcceptContract(
                CreateBattleReadyCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            carefulReady = ReachFirstHourEngineeringCheck071(carefulReady);
            var carefulPressureBefore = carefulReady.Guild.GuildCity.Expedition.Urgency;
            var carefulMemoriesBefore = carefulReady.Guild.GuildCity.RelationshipMemories.Count;

            var careful = Require(_expedition.ResolveCommittedCheck(
                carefulReady,
                _content,
                "EVENT_COLLAPSED_HANDRAIL",
                "R1",
                "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            var carefulCheck = careful.Guild.GuildCity.Expedition.CommittedChecks.Single(
                value => value.NodeId == "N04");
            Assert.That(careful.Guild.GuildCity.Expedition.Urgency,
                Is.EqualTo(carefulPressureBefore -
                           GuildCityExpeditionService017D.CarefulApproachPressureCost076));
            Assert.That(carefulCheck.Modifier,
                Is.EqualTo(GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(carefulCheck.AssistantRecruitId, Is.EqualTo("R2"));
            Assert.That(careful.Guild.GuildCity.RelationshipMemories,
                Has.Count.EqualTo(carefulMemoriesBefore + 1));

            var carefulReplay = Require(_expedition.ResolveCommittedCheck(
                careful,
                _content,
                "EVENT_COLLAPSED_HANDRAIL",
                "R1",
                "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(carefulReplay.Guild.GuildCity.Expedition.Urgency,
                Is.EqualTo(careful.Guild.GuildCity.Expedition.Urgency),
                "Reloading or repeating the committed check must not spend Pressure twice.");

            var swiftReady = Require(_expedition.AcceptContract(
                CreateBattleReadyCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            swiftReady = ReachFirstHourEngineeringCheck071(swiftReady);
            var swiftPressureBefore = swiftReady.Guild.GuildCity.Expedition.Urgency;
            var swiftMemoriesBefore = swiftReady.Guild.GuildCity.RelationshipMemories.Count;

            var swift = Require(_expedition.ResolveCommittedCheck(
                swiftReady,
                _content,
                "EVENT_COLLAPSED_HANDRAIL",
                "R1",
                string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            var swiftCheck = swift.Guild.GuildCity.Expedition.CommittedChecks.Single(
                value => value.NodeId == "N04");
            Assert.That(swift.Guild.GuildCity.Expedition.Urgency,
                Is.EqualTo(swiftPressureBefore));
            Assert.That(swiftCheck.Modifier,
                Is.EqualTo(GuildCityExpeditionService017D.SwiftApproachModifier076));
            Assert.That(swiftCheck.AssistantRecruitId, Is.Empty);
            Assert.That(swift.Guild.GuildCity.RelationshipMemories,
                Has.Count.EqualTo(swiftMemoriesBefore));
        }

        [Test] public void CarefulFieldOrderCannotSpendPressureThatDoesNotExist079()
        {
            var ready = Require(_expedition.AcceptContract(
                CreateBattleReadyCampaign(),
                _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            ready = ReachFirstHourEngineeringCheck071(ready);
            var zeroPressureExpedition = ready.Guild.GuildCity.Expedition.With(urgency: 0);
            var zeroPressureCity = ready.Guild.GuildCity.With(
                expedition: zeroPressureExpedition,
                replaceExpedition: true);
            ready = ready.With(
                ready.Guild.WithGuildCity(zeroPressureCity),
                ready.OpeningFlow);
            var beforeRejectedOrderHash = CanonicalJson.Sha256Hex(ready);
            var committedCheckCountBefore = ready.Guild.GuildCity.Expedition.CommittedChecks.Count;

            var careful = _expedition.ResolveCommittedCheck(
                ready,
                _content,
                "EVENT_COLLAPSED_HANDRAIL",
                "R1",
                "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076);

            Assert.That(careful.IsSuccess, Is.False);
            Assert.That(careful.Errors,
                Does.Contain("GC017D_CAREFUL_APPROACH_PRESSURE_REQUIRED"));
            Assert.That(CanonicalJson.Sha256Hex(ready), Is.EqualTo(beforeRejectedOrderHash),
                "A rejected order must not mutate the saved expedition.");

            var swift = Require(_expedition.ResolveCommittedCheck(
                ready,
                _content,
                "EVENT_COLLAPSED_HANDRAIL",
                "R1",
                string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            Assert.That(swift.Guild.GuildCity.Expedition.Urgency, Is.Zero);
            Assert.That(swift.Guild.GuildCity.Expedition.CommittedChecks,
                Has.Count.EqualTo(committedCheckCountBefore + 1));
            Assert.That(swift.Guild.GuildCity.Expedition.CommittedChecks
                .Single(value => value.NodeId == "N04").AssistantRecruitId, Is.Empty);
        }

        [Test] public void ChapterTwoStartsAtTheSavedApprenticeThresholdAndCommitsTheChosenRoute078()
        {
            var accepted = Require(_expedition.AcceptContract(
                CreateCampaign(),
                _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            var begun = Require(_expedition.StartExpedition(accepted, _content));
            var threshold = begun.Guild.GuildCity.Expedition;
            var board = _content.Board(GuildCityExpeditionService017D.SecondStoryBoardId076);

            Assert.That(threshold.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.SecondStoryBoardId076));
            Assert.That(threshold.CurrentNodeId, Is.EqualTo("N00"));
            Assert.That(threshold.VisitedNodeIds, Is.EquivalentTo(new[] { "N00" }));
            Assert.That(threshold.CommittedMoveIds, Is.Empty,
                "The threshold scene must not be silently consumed as an invisible move.");
            Assert.That(threshold.Supplies, Is.EqualTo(board.StartingSupplies));
            Assert.That(threshold.Fatigue, Is.Zero);
            Assert.That(threshold.Urgency, Is.EqualTo(board.StartingUrgency));
            Assert.That(threshold.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.ChapterTwoWayglassOpenedFlag076));
            Assert.That(threshold.LastCheckpointId, Is.EqualTo("chapter_two_threshold_078"));
            Assert.That(begun.Guild.GuildCity.LastCheckpointId, Is.EqualTo("chapter_two_threshold_078"));
            Assert.That(board.Node("N00").EventId, Is.EqualTo("EVENT_FOUND_APPRENTICE"));

            var prematureMove = _expedition.CommitMove(begun, _content, "N01");
            Assert.That(prematureMove.IsSuccess, Is.False);
            Assert.That(prematureMove.Errors,
                Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));

            var resolved = Require(_expedition.ResolveCommittedCheck(
                begun,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(resolved.Guild.GuildCity.Expedition.CommittedChecks, Has.Count.EqualTo(1));
            Assert.That(resolved.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item("EVENT_RESOLVED_EVENT_FOUND_APPRENTICE"));
            Assert.That(resolved.Guild.GuildCity.RelationshipMemories, Has.Count.EqualTo(1));
            Assert.That(resolved.Guild.GuildCity.RelationshipMemories[0].Summary,
                Does.Contain("Sella Vey").And.Contain("brass line-tag"));

            var replayed = Require(_expedition.ResolveCommittedCheck(
                resolved,
                _content,
                "EVENT_FOUND_APPRENTICE",
                "R1",
                "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(replayed.Guild.GuildCity.Expedition.CommittedChecks, Has.Count.EqualTo(1));
            Assert.That(replayed.Guild.GuildCity.RelationshipMemories, Has.Count.EqualTo(1));
            Assert.That(replayed.Guild.GuildCity.Expedition.Urgency,
                Is.EqualTo(resolved.Guild.GuildCity.Expedition.Urgency),
                "Reloading the threshold must not recommit its check or pressure cost.");

            var junction = Require(_expedition.CommitMove(replayed, _content, "N01"));
            var chosen = Require(_expedition.CommitMove(junction, _content, "N02"));
            Assert.That(chosen.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N02"));
            Assert.That(chosen.Guild.GuildCity.Expedition.CommittedMoveIds.Count, Is.EqualTo(2));
            Assert.That(board.Node("N02").EventId,
                Is.EqualTo("EVENT_WRONG_ROUTE_MARKS"));
            Assert.That(chosen.Guild.GuildCity.Expedition.LastCheckpointId,
                Does.StartWith("move_MOVE_"),
                "The selected route must own an immutable move receipt for save/reload continuity.");
        }

        [Test] public void ChapterTwoContentCarriesNamedEvidenceThroughCampAndSecondaryObjective078()
        {
            var board = _content.Board(GuildCityExpeditionService017D.SecondStoryBoardId076);
            Assert.That(board.Nodes.Select(value => value.Id), Is.EqualTo(new[]
            {
                "N00", "N01", "N02", "N03", "N04", "N05", "N06", "N07",
                "N08", "N09", "N10", "N11", "N12", "N13", "N14"
            }), "Save-bound Chapter 2 node IDs and positions must remain stable.");
            Assert.That(board.Node("N00").EventId, Is.EqualTo("EVENT_FOUND_APPRENTICE"));
            Assert.That(board.Node("N00").Links, Is.EqualTo(new[] { "N01" }));
            Assert.That(board.Node("N01").Links, Is.EqualTo(new[] { "N02", "N04" }));
            Assert.That(board.Node("N02").CheckSkillId, Is.EqualTo("Perception"));
            Assert.That(board.Node("N07").Kind, Is.EqualTo("CAMP"));
            Assert.That(board.Node("N07").EventId, Is.EqualTo("EVENT_CAMP_ARGUMENT"));
            Assert.That(board.Node("N07").CheckSkillId, Is.EqualTo("Resolve"));
            Assert.That(board.Node("N07").Links, Is.EqualTo(new[] { "N08" }));
            Assert.That(board.Node("N08").Links, Is.EqualTo(new[] { "N09", "N10" }));
            Assert.That(GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                GuildCityExpeditionService017D.SecondStoryBoardId076,
                _content.Event("EVENT_CAMP_ARGUMENT")), Is.True);
            Assert.That(board.Node("N10").Links, Is.EqualTo(new[] { "N11", "N12" }));
            Assert.That(board.Node("N11").Kind, Is.EqualTo("SECONDARY_OBJECTIVE"));
            Assert.That(board.Node("N11").EventId, Is.EqualTo("EVENT_ABANDONED_SURVEY_PACK"));
            Assert.That(board.Node("N11").CheckSkillId, Is.EqualTo("Perception"));
            Assert.That(board.Node("N12").Kind, Is.EqualTo("SAFE_ROUTE"));
            Assert.That(board.Node("N12").EventId,
                Is.EqualTo(GuildCityExpeditionService017D.SecondStorySafeDescentEventId080));

            var routeEventIds = new[]
            {
                "EVENT_FOUND_APPRENTICE", "EVENT_WRONG_ROUTE_MARKS",
                "EVENT_BROKEN_SURVEY_BRIDGE", "EVENT_CAMP_ARGUMENT",
                "EVENT_FOG_ECHO", "EVENT_BROKEN_ASTROLABE",
                "EVENT_ABANDONED_SURVEY_PACK",
                GuildCityExpeditionService017D.SecondStorySafeDescentEventId080
            };
            var uncommittedCostClaims079 = new[]
            {
                "costs route pressure", "spends supplies", "supply cost",
                "raises threat", "costs supplies", "supplies or fatigue",
                "delay costs"
            };
            foreach (var eventId in routeEventIds)
            {
                var authored = _content.Event(eventId);
                Assert.That(authored.Problem, Is.Not.Empty, eventId);
                Assert.That(authored.ConsequenceIdentity, Is.Not.Empty, eventId);
                Assert.That(authored.ExceptionalText, Is.Not.Empty, eventId);
                Assert.That(authored.FullSuccessText, Is.Not.Empty, eventId);
                Assert.That(authored.SuccessWithCostText, Is.Not.Empty, eventId);
                Assert.That(authored.SetbackText, Is.Not.Empty, eventId);
                Assert.That(authored.SevereSetbackText, Is.Not.Empty, eventId);
                Assert.That(authored.RelationshipMemorySummary, Is.Not.Empty, eventId);
                foreach (var falseClaim079 in uncommittedCostClaims079)
                    Assert.That(authored.SuccessWithCostText,
                        Does.Not.Contain(falseClaim079).IgnoreCase,
                        eventId + " cannot promise a numerical cost the committed resolver does not guarantee.");
            }
            Assert.That(_content.Event("EVENT_FOUND_APPRENTICE").Problem,
                Does.Contain("Sella Vey").And.Contain("Orra Vale").And.Contain("brass line-tag"));
            Assert.That(_content.Event("EVENT_CAMP_ARGUMENT").Problem,
                Does.Contain("Sella Vey").And.Contain("Orra Vale")
                    .And.Contain("seventh-return mark"));
            Assert.That(_content.Event("EVENT_ABANDONED_SURVEY_PACK").ConsequenceIdentity,
                Does.Contain("field book").And.Contain("Civic Trust"));
        }

        [Test] public void ChapterTwoCommittedCheckUsesDeterministicDiceAndSubmittedModifier081()
        {
            var accepted = Require(_expedition.AcceptContract(
                CreateCampaign(), _content, GuildCityExpeditionService017D.SecondStoryContractId076));
            var begun = Require(_expedition.StartExpedition(accepted, _content));

            var lowInput = Require(_expedition.ResolveCommittedCheck(
                begun, _content, "EVENT_FOUND_APPRENTICE", "R1", "R2", -99));
            var highInput = Require(_expedition.ResolveCommittedCheck(
                begun, _content, "EVENT_FOUND_APPRENTICE", "R1", "R2", 99));
            var lowCheck = lowInput.Guild.GuildCity.Expedition.CommittedChecks.Single();
            var highCheck = highInput.Guild.GuildCity.Expedition.CommittedChecks.Single();

            Assert.That(lowCheck.DieOne, Is.InRange(1, 6));
            Assert.That(lowCheck.DieTwo, Is.InRange(1, 6));
            Assert.That(highCheck.DieOne, Is.EqualTo(lowCheck.DieOne));
            Assert.That(highCheck.DieTwo, Is.EqualTo(lowCheck.DieTwo));
            Assert.That(highCheck.CanonicalSeedIdentity,
                Is.EqualTo(lowCheck.CanonicalSeedIdentity));
            Assert.That(lowCheck.CanonicalSeedIdentity,
                Does.Not.StartWith("AUTHORED079_"));
            Assert.That(lowCheck.Outcome, Is.EqualTo("SEVERE_SETBACK"));
            Assert.That(highCheck.Outcome, Is.EqualTo("EXCEPTIONAL"));
            Assert.That(highCheck.Total, Is.EqualTo(lowCheck.Total + 198),
                "Committed Chapter 2 dice stay replay-safe while the submitted modifier changes the result.");
            Assert.That(lowInput.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item("EVENT_RESOLVED_EVENT_FOUND_APPRENTICE"));
            var chapterOneCamp = _content.Event("EVENT_LANTERN_WATCH_CAMP");
            var chapterTwoCamp = _content.Event("EVENT_CAMP_ARGUMENT");
            Assert.That(GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
                chapterOneCamp), Is.True);
            Assert.That(GuildCityExpeditionService017D.UsesCommitted2d6ForBoard079(
                GuildCityExpeditionService017D.SecondStoryBoardId076,
                chapterTwoCamp), Is.True,
                "Chapter 2 board events must use the committed 2d6 authority.");
            Assert.That(chapterOneCamp.Problem,
                Does.Contain("Maren").And.Contain("Jazzi").And.Contain("Zorin")
                    .And.Contain("Lantern Watch"));
            Assert.That(chapterTwoCamp.Problem,
                Does.Contain("Sella Vey").And.Contain("Orra Vale"));
        }

        [Test] public void ChapterTwoCampAndFieldBookAreCommittedRecoverableChecks078()
        {
            var accepted = Require(_expedition.AcceptContract(
                CreateBattleReadyCampaign(), _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            var begun = Require(_expedition.StartExpedition(accepted, _content));
            var source = begun.Guild.GuildCity.Expedition;
            var stagedFlags = source.ObjectiveFlags
                .Concat(new[] { GuildCityExpeditionService017D.EncounterClearedFlag("N06") })
                .ToArray();
            var stagedExpedition = source.With(
                currentNodeId: "N06",
                supplies: 10,
                fatigue: 5,
                urgency: 10,
                visitedNodeIds: new[] { "N00", "N01", "N04", "N05", "N06" },
                revealedNodeIds: new[] { "N00", "N01", "N04", "N05", "N06", "N07", "N08" },
                objectiveFlags: stagedFlags,
                lastCheckpointId: "chapter_two_post_fog_stalkers_test");
            var stagedCity = begun.Guild.GuildCity.With(
                expedition: stagedExpedition,
                replaceExpedition: true,
                lastCheckpointId: "chapter_two_post_fog_stalkers_test");
            var staged = begun.With(begun.Guild.WithGuildCity(stagedCity), begun.OpeningFlow);

            var camp = Require(_expedition.CommitMove(staged, _content, "N07"));
            Assert.That(camp.Guild.GuildCity.Expedition.Fatigue, Is.EqualTo(2),
                "The authored regroup must retain the camp's real fatigue recovery.");
            Assert.That(GuildCityExpeditionService017D.CurrentNodeRequiresResolution(
                _content.Board(GuildCityExpeditionService017D.SecondStoryBoardId076).Node("N07")), Is.True);
            Assert.That(_expedition.CommitMove(camp, _content, "N08").Errors,
                Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));

            camp = Require(_expedition.ResolveCommittedCheck(
                camp, _content, "EVENT_CAMP_ARGUMENT", "R1", "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            var chapterTwoCampCheck = camp.Guild.GuildCity.Expedition.CommittedChecks.Single(
                value => value.NodeId == "N07");
            Assert.That(chapterTwoCampCheck.EventId, Is.EqualTo("EVENT_CAMP_ARGUMENT"));
            Assert.That(chapterTwoCampCheck.DieOne, Is.InRange(1, 6));
            Assert.That(chapterTwoCampCheck.DieTwo, Is.InRange(1, 6));
            Assert.That(chapterTwoCampCheck.Total, Is.EqualTo(
                chapterTwoCampCheck.DieOne + chapterTwoCampCheck.DieTwo +
                chapterTwoCampCheck.Modifier));
            Assert.That(chapterTwoCampCheck.CanonicalSeedIdentity,
                Does.Not.StartWith("AUTHORED079_"));
            Assert.That(camp.Guild.GuildCity.RelationshipMemories.Single(
                    value => value.SourceId == "EVENT_CAMP_ARGUMENT").Summary,
                Does.Contain("Sella Vey").And.Contain("Orra Vale"));

            var fogEcho = Require(_expedition.CommitMove(camp, _content, "N08"));
            Assert.That(_expedition.CommitMove(fogEcho, _content, "N10").Errors,
                Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));
            fogEcho = Require(_expedition.ResolveCommittedCheck(
                fogEcho, _content, "EVENT_FOG_ECHO", "R1", "R2",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(fogEcho.Guild.GuildCity.Expedition.CommittedChecks.Single(
                    value => value.NodeId == "N08").EventId,
                Is.EqualTo("EVENT_FOG_ECHO"));

            var astrolabe = Require(_expedition.CommitMove(fogEcho, _content, "N10"));
            astrolabe = Require(_expedition.ResolveCommittedCheck(
                astrolabe, _content, "EVENT_BROKEN_ASTROLABE", "R3", string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            var fieldBook = Require(_expedition.CommitMove(astrolabe, _content, "N11"));
            Assert.That(fieldBook.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item("SECONDARY_OBJECTIVE_REACHED_N11"));
            Assert.That(_expedition.CommitMove(fieldBook, _content, "N13").Errors,
                Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));

            fieldBook = Require(_expedition.ResolveCommittedCheck(
                fieldBook, _content, "EVENT_ABANDONED_SURVEY_PACK", "R4", "R5",
                GuildCityExpeditionService017D.CarefulApproachModifier076));
            Assert.That(fieldBook.Guild.GuildCity.Expedition.CommittedChecks.Single(
                    value => value.NodeId == "N11").EventId,
                Is.EqualTo("EVENT_ABANDONED_SURVEY_PACK"));
            Assert.That(fieldBook.Guild.GuildCity.RelationshipMemories.Single(
                    value => value.SourceId == "EVENT_ABANDONED_SURVEY_PACK").Summary,
                Does.Contain("field book").And.Contain("Sella Vey"));

            var rescue = Require(_expedition.CommitMove(fieldBook, _content, "N13"));
            Assert.That(rescue.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N13"),
                "Even a failed field-book outcome must remain recoverable and lead to the main rescue.");
            var receiptsBeforeClimax = rescue.Guild.Development.ClaimedBattleRewardIds.Count;
            rescue = CompleteCurrentFirstHourEncounter071(
                rescue,
                "ENCOUNTER_SURVEYOR_RESCUE",
                3,
                true);
            Assert.That(rescue.Guild.GuildCity.Expedition.ObjectiveFlags,
                Is.SupersetOf(new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N13"),
                    "PRIMARY_OBJECTIVE_RESCUE_COMPLETE"
                }),
                "The Chapter 2 climax must save both the encounter and surveyor-rescue consequence.");

            var testimony = Require(_expedition.CommitMove(rescue, _content, "N14"));
            Assert.That(testimony.Guild.GuildCity.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.Completed));
            Assert.That(testimony.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N14"));

            var returned = Require(_expedition.FinalizeCompletedExpedition(testimony, _content));
            Assert.That(returned.Guild.GuildCity.Expedition, Is.Null,
                "Returning the survey crew must close the Chapter 2 expedition.");
            Assert.That(returned.Guild.GuildCity.ActiveContract.ContractId,
                Is.EqualTo(GuildCityExpeditionService017D.SecondStoryContractId076));
            Assert.That(returned.Guild.GuildCity.ActiveContract.Completed, Is.True);
            Assert.That(returned.Guild.Development.ClaimedBattleRewardIds.Count,
                Is.EqualTo(receiptsBeforeClimax + 2),
                "The climax must persist one Surveyor Rescue battle reward and one Chapter 2 contract reward.");
        }

        [Test] public void ChapterTwoCannotStartWithIllegalUnionPlans076()
        {
            var accepted = Require(_expedition.AcceptContract(
                CreateCampaign(),
                _content,
                GuildCityExpeditionService017D.SecondStoryContractId076));
            var invalidGuild = accepted.Guild.With(
                accepted.Guild.TreasuryXp,
                accepted.Guild.Recruits,
                new[] { accepted.Guild.Unions[0] },
                accepted.Guild.Inventory);
            var invalid = accepted.With(invalidGuild, accepted.OpeningFlow);

            var result = _expedition.StartExpedition(invalid, _content);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Errors, Does.Contain("GC017D_UNION_PLANS_NOT_READY"));
            Assert.That(invalid.Guild.GuildCity.Expedition, Is.Null);
        }

        [Test] public void FreshFirstContractUsesCompleteFirstHourBoard()
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            var contract = _content.Contract(GuildCityExpeditionService017D.FirstStoryContractId066);
            Assert.That(contract.Hook, Is.EqualTo(
                "Kael holds the Hall breach. Follow Lantern Road: Zorin's missing patrol carries Skyhome's Wayglass toward the old Gatehouse."));
            Assert.That(contract.BaseGuildXp, Is.EqualTo(42));
            Assert.That(contract.BaseHallXp, Is.EqualTo(44),
                "Guild XP and Hall XP are distinct authored reward streams.");
            Assert.That(campaign.Guild.GuildCity.ActiveContract.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071));
            var fresh = _content.Board(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071);
            Assert.That(fresh.DisplayName, Is.EqualTo("The Bell Beneath Skyhome — First Hour"));
            Assert.That(fresh.StartNodeId, Is.EqualTo("N00"));
            Assert.That(fresh.ObjectiveNodeId, Is.EqualTo("N13"));
            Assert.That(fresh.ExitNodeId, Is.EqualTo("N14"));
            Assert.That(fresh.Nodes, Has.Length.EqualTo(15));
            Assert.That(fresh.Node("N00").Links, Is.EqualTo(new[] { "N01" }));
            Assert.That(fresh.Node("N01").Links, Is.EqualTo(new[] { "N02" }));
            Assert.That(fresh.Node("N02").Kind, Is.EqualTo("EVENT"));
            Assert.That(fresh.Node("N03").Links, Is.EqualTo(new[] { "N04" }));
            Assert.That(fresh.Node("N04").Links, Is.EqualTo(new[] { "N05" }));
            Assert.That(fresh.Node("N06").EncounterId,
                Is.EqualTo("ENCOUNTER071_LANTERN_ROAD_AMBUSH"));
            Assert.That(fresh.Node("N06").Links, Is.EqualTo(new[] { "N07" }));
            Assert.That(fresh.Node("N07").Kind, Is.EqualTo("CAMP"));
            Assert.That(fresh.Node("N07").Links, Is.EqualTo(new[] { "N08" }));
            Assert.That(fresh.Node("N07").EventId,
                Is.EqualTo("EVENT_LANTERN_WATCH_CAMP"));
            var chapterOneCamp = _content.Event(fresh.Node("N07").EventId);
            Assert.That(chapterOneCamp.UsesCommitted2d6, Is.True);
            Assert.That(chapterOneCamp.Problem,
                Does.Contain("Maren").And.Contain("Jazzi").And.Contain("Zorin")
                    .And.Contain("Lantern Watch"));
            Assert.That(fresh.Node("N08").Kind, Is.EqualTo("EVENT"));
            Assert.That(fresh.Node("N08").Links, Is.EqualTo(new[] { "N09", "N10" }));
            Assert.That(fresh.Node("N11").Kind, Is.EqualTo("SECONDARY_OBJECTIVE"));
            Assert.That(fresh.Node("N13").EncounterId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071));
            Assert.That(_content.Event("EVENT_COLLAPSED_HANDRAIL").Title,
                Is.EqualTo("Quin at the Broken Waymarker"));
            Assert.That(_content.Event("EVENT_UNSTABLE_BELL_CHAIN").Title,
                Is.EqualTo("Wayglass Resonance"));

            var streamlined = _content.Board(
                GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069);
            Assert.That(streamlined.Node("N01").Links, Is.EqualTo(new[] { "N04" }));
            Assert.That(streamlined.Node("N04").Links, Is.EqualTo(new[] { "N06" }));
            Assert.That(streamlined.Node("N06").EncounterId,
                Is.EqualTo("ENCOUNTER_GATE_GNAWER_RESCUE"));
            Assert.That(streamlined.Node("N06").Links, Is.EqualTo(new[] { "N14" }),
                "The safe migration projection remains a short route for already-committed legacy saves only.");

            var legacy = _content.Board("BOARD_BELL_BENEATH_GATE");
            Assert.That(legacy.Node("N01").Links, Is.EqualTo(new[] { "N02", "N04" }));
            Assert.That(legacy.Node("N06").Links, Is.EqualTo(new[] { "N07", "N08" }));
            Assert.That(legacy.Node("N13").EncounterId, Is.EqualTo("ENCOUNTER_GATE_GNAWER_RESCUE"));
        }

        [Test] public void LegacyFirstRescueMigrationRestartsOnlyTheActiveRouteAndPreservesTheGuild069()
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = WithLegacyExpeditionAt(
                campaign, "N04", new[] { "N00", "N01", "N04" },
                new[] { "N00", "N01", "N04", "N05" });
            campaign = Require(_expedition.ResolveCommittedCheck(campaign, _content,
                "EVENT_COLLAPSED_HANDRAIL", "R1", "R2", 2));
            campaign = WithLegacyExpeditionAt(campaign, "N05",
                new[] { "N00", "N01", "N04", "N05" },
                new[] { "N00", "N01", "N04", "N05", "N06" });

            var rosterBefore = CanonicalJson.Serialize(campaign.Guild.Recruits);
            var unionsBefore = CanonicalJson.Serialize(campaign.Guild.Unions);
            var inventoryBefore = CanonicalJson.Serialize(campaign.Guild.Inventory);
            var developmentBefore = CanonicalJson.Serialize(campaign.Guild.Development);
            var materialsBefore = CanonicalJson.Serialize(campaign.Guild.GuildCity.Materials);
            var plotsBefore = CanonicalJson.Serialize(campaign.Guild.GuildCity.CityPlots);
            var relationshipsBefore = CanonicalJson.Serialize(campaign.Guild.GuildCity.RelationshipMemories);
            var assignmentsBefore = CanonicalJson.Serialize(campaign.Guild.GuildCity.MemberAssignments);
            var completedChecksBefore = CanonicalJson.Serialize(
                campaign.Guild.GuildCity.Expedition.CommittedChecks);
            var operationBefore = campaign.Guild.GuildCity.OperationOrdinal;
            var commitBefore = campaign.Guild.GuildCity.ActiveContract;

            Assert.That(GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(campaign),
                Is.True);
            campaign = Require(_expedition.MigrateLegacyFirstRescue069(campaign, _content));

            Assert.That(campaign.Guild.GuildCity.ActiveContract.ContractId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstStoryContractId066));
            Assert.That(campaign.Guild.GuildCity.ActiveContract.CommitId,
                Is.EqualTo(commitBefore.CommitId),
                "The already-accepted contract identity must remain the completion-reward authority.");
            Assert.That(campaign.Guild.GuildCity.ActiveContract.AcceptedOperationOrdinal,
                Is.EqualTo(commitBefore.AcceptedOperationOrdinal));
            Assert.That(campaign.Guild.GuildCity.ActiveContract.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069));
            Assert.That(campaign.Guild.GuildCity.Expedition.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.StreamlinedFirstRescueBoardId069));
            Assert.That(campaign.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N00"));
            Assert.That(campaign.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.LegacyFirstRescueMigratedFlag069));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.Expedition.CommittedChecks),
                Is.EqualTo(completedChecksBefore),
                "A 2d6 check already committed on the shared N04 step must not be rolled again.");
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("legacy_first_rescue_restarted_069"));
            Assert.That(GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(campaign),
                Is.False, "The migration must be idempotently ineligible after its one saved application.");

            Assert.That(CanonicalJson.Serialize(campaign.Guild.Recruits), Is.EqualTo(rosterBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Unions), Is.EqualTo(unionsBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Inventory), Is.EqualTo(inventoryBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Development), Is.EqualTo(developmentBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.Materials), Is.EqualTo(materialsBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.CityPlots), Is.EqualTo(plotsBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.RelationshipMemories),
                Is.EqualTo(relationshipsBefore));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.MemberAssignments),
                Is.EqualTo(assignmentsBefore));
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal, Is.EqualTo(operationBefore));
        }

        [Test] public void ClaimedLegacyRescueBattleMigratesToWalkHomeWithoutAnotherBattle069()
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = WithLegacyExpeditionAt(campaign, "N13",
                new[] { "N00", "N01", "N04", "N06", "N08", "N12", "N13" },
                new[] { "N00", "N01", "N04", "N06", "N08", "N12", "N13", "N14" });
            var legacy = campaign.Guild.GuildCity.Expedition.With(
                supplies: 7,
                fatigue: 5,
                urgency: 6,
                objectiveFlags: new[]
                {
                    "OBJECTIVE_ENCOUNTER_CLEARED",
                    GuildCityExpeditionService017D.EncounterClearedFlag("N13"),
                    "PRIMARY_OBJECTIVE_RESCUE_COMPLETE"
                },
                lastCheckpointId: "legacy_rescue_battle_return_applied");
            var city = campaign.Guild.GuildCity.With(
                expedition: legacy,
                replaceExpedition: true,
                lastCheckpointId: "battle_return_applied");
            var development = campaign.Guild.Development.RecordBattleReward(
                "BATTLE_REWARD_LEGACY_RESCUE_069", 25, 30);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp + 25,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory,
                development).WithGuildCity(city);
            campaign = campaign.With(guild, campaign.OpeningFlow);
            var claimedRewardsBefore = CanonicalJson.Serialize(campaign.Guild.Development);
            var treasuryBefore = campaign.Guild.TreasuryXp;

            campaign = Require(_expedition.MigrateLegacyFirstRescue069(campaign, _content));

            Assert.That(campaign.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N06"));
            Assert.That(campaign.Guild.GuildCity.Expedition.Supplies, Is.EqualTo(7));
            Assert.That(campaign.Guild.GuildCity.Expedition.Fatigue, Is.EqualTo(5));
            Assert.That(campaign.Guild.GuildCity.Expedition.Urgency, Is.EqualTo(6));
            Assert.That(campaign.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.EncounterClearedFlag("N06")));
            Assert.That(campaign.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item("PRIMARY_OBJECTIVE_RESCUE_COMPLETE"));
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("legacy_first_rescue_reward_preserved_walk_home_069"));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Development),
                Is.EqualTo(claimedRewardsBefore), "Claimed XP/equipment reward authority cannot be granted again.");
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasuryBefore));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter, Is.Null);

            var home = _expedition.CommitMove(campaign, _content, "N14");
            Assert.That(home.IsSuccess, Is.True, string.Join("\n", home.Errors));
            Assert.That(home.Value.Guild.GuildCity.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.Completed));
            Assert.That(home.Value.Guild.GuildCity.PendingEncounter, Is.Null,
                "A claimed rescue battle must never be committed a second time by migration.");
        }

        [Test] public void LegacyRescueMigrationRefusesPendingBattleStateWithoutChangingTheSave069()
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = WithLegacyExpeditionAt(campaign, "N13",
                new[] { "N00", "N13" }, new[] { "N00", "N13", "N14" });
            campaign = Require(_expedition.CommitEncounter(
                campaign, _content, "ENCOUNTER_GATE_GNAWER_RESCUE"));
            var before = CanonicalJson.Serialize(campaign);

            var rejected = _expedition.MigrateLegacyFirstRescue069(campaign, _content);

            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(rejected.Errors,
                Contains.Item("GC017D_V69_RESOLVE_PENDING_ENCOUNTER_FIRST"));
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(before),
                "Refusing an unsafe migration must not mutate any part of the save.");
        }

        [TestCase(ExpeditionStatus017D.Completed)]
        [TestCase(ExpeditionStatus017D.Failed)]
        public void TerminalLegacyRescueRemainsEligibleForNormalReturnInsteadOfMigrationLoop069(
            ExpeditionStatus017D terminalStatus)
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = WithLegacyExpeditionAt(campaign, "N14",
                new[] { "N00", "N13", "N14" }, new[] { "N00", "N13", "N14" });
            var terminal = campaign.Guild.GuildCity.Expedition.With(
                status: terminalStatus,
                lastCheckpointId: terminalStatus == ExpeditionStatus017D.Completed
                    ? "expedition_exit_reached"
                    : "legacy_expedition_failed");
            var city = campaign.Guild.GuildCity.With(
                expedition: terminal,
                replaceExpedition: true,
                lastCheckpointId: terminal.LastCheckpointId);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
            var before = CanonicalJson.Serialize(campaign);

            Assert.That(GuildCityExpeditionService017D.NeedsLegacyFirstRescueMigration069(campaign),
                Is.False,
                "A terminal legacy route must remain on its normal finalize/return path instead of reopening migration every time the Hall is entered.");
            var rejected = _expedition.MigrateLegacyFirstRescue069(campaign, _content);
            Assert.That(rejected.IsSuccess, Is.False);
            Assert.That(CanonicalJson.Serialize(campaign), Is.EqualTo(before));
            var finalized = _expedition.FinalizeCompletedExpedition(campaign, _content);
            Assert.That(finalized.IsSuccess, Is.True, string.Join("\n", finalized.Errors));
            Assert.That(finalized.Value.Guild.GuildCity.Expedition, Is.Null,
                "Terminal legacy saves must still take their existing normal return/finalize path.");
        }

        [Test] public void EncounterLaunchIsCommittedAndCannotReroll()
        {
            var campaign=Require(_expedition.AcceptContract(CreateCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            campaign=Require(_expedition.StartExpedition(campaign,_content));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N01"));
            const string encounterId = GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071;
            var first=Require(_expedition.CommitEncounter(campaign,_content,encounterId));
            var second=Require(_expedition.CommitEncounter(first,_content,encounterId));
            Assert.That(CanonicalJson.Serialize(second.Guild.GuildCity.PendingEncounter),Is.EqualTo(CanonicalJson.Serialize(first.Guild.GuildCity.PendingEncounter)));
            Assert.That(first.Guild.GuildCity.Expedition.Status,Is.EqualTo(ExpeditionStatus017D.AwaitingBattle));
            Assert.That(first.Guild.GuildCity.PendingEncounter.EnemyUnionCount,Is.EqualTo(2));
        }

        [Test] public void MutatedSavedGuildEncounterCannotCrossSharedBattleBoundary()
        {
            var campaign=Require(_expedition.AcceptContract(
                CreateBattleReadyCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            campaign=Require(_expedition.StartExpedition(campaign,_content));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N01"));
            campaign=Require(_expedition.CommitEncounter(campaign,_content,
                GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071));
            var request=campaign.Guild.GuildCity.PendingEncounter;
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                    request)),Is.True);

            var mutatedRequest=new EncounterLaunchRequest017D(
                request.RequestId,request.ContractId,request.ExpeditionId,
                request.BoardId,request.NodeId,request.EncounterId,request.BattleId,
                request.Objective+" (mutated saved request)",request.EnemyUnionCount,
                request.CanonicalSeedIdentity,request.AlliedUnionIds,
                request.ReserveUnionIds,request.ObjectiveIds,request.RouteModifiers,
                request.Supplies,request.Fatigue,request.Urgency,
                request.ReturnCheckpointId,request.PreBattleStateHash);
            var mutatedCity=campaign.Guild.GuildCity.With(
                pendingEncounter:mutatedRequest,replacePendingEncounter:true,
                lastCheckpointId:"mutated_saved_guild_request_084");
            var mutated=campaign.With(campaign.Guild.WithGuildCity(mutatedCity),
                campaign.OpeningFlow);
            var before=CanonicalJson.Sha256Hex(mutated);
            var rejected=_bridge.StartCertifiedEncounter(
                mutated,_battles,_combatContent);
            Assert.That(rejected.IsSuccess,Is.False);
            Assert.That(rejected.Errors,Does.Contain(
                "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED"));
            Assert.That(CanonicalJson.Sha256Hex(mutated),Is.EqualTo(before));
            Assert.That(mutated.Battle,Is.Null);

            var canonical=_bridge.StartCertifiedEncounter(
                campaign,_battles,_combatContent);
            Assert.That(canonical.IsSuccess,Is.True,string.Join("\n",canonical.Errors));
            Assert.That(canonical.Value.Battle.BattleId,Is.EqualTo(request.BattleId));
        }

        [Test] public void EventAndRequiredEncounterNodesCannotBeSkipped()
        {
            var campaign=Require(_expedition.AcceptContract(CreateBattleReadyCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            campaign=Require(_expedition.StartExpedition(campaign,_content));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N01"));
            var skippedHallBattle=_expedition.CommitMove(campaign,_content,"N04");
            Assert.That(skippedHallBattle.IsSuccess,Is.False);
            Assert.That(skippedHallBattle.Errors,Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));
            var wrongHallEncounter=_expedition.CommitEncounter(campaign,_content,"ENCOUNTER_GATE_GNAWER_ELITE");
            Assert.That(wrongHallEncounter.IsSuccess,Is.False);
            Assert.That(wrongHallEncounter.Errors,Does.Contain("GC017D_ENCOUNTER_DOES_NOT_MATCH_CURRENT_NODE"));
            campaign=CompleteCurrentFirstHourEncounter071(
                campaign, GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071, 2);
            campaign=Require(_expedition.CommitMove(campaign,_content,"N02"));
            var skippedEvent=_expedition.CommitMove(campaign,_content,"N03");
            Assert.That(skippedEvent.IsSuccess,Is.False);
            Assert.That(skippedEvent.Errors,Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));
            var wrongEvent=_expedition.ResolveCommittedCheck(campaign,_content,"EVENT_COLLAPSED_HANDRAIL","R1","R2",2);
            Assert.That(wrongEvent.IsSuccess,Is.False);
            Assert.That(wrongEvent.Errors,Does.Contain("GC017D_EVENT_DOES_NOT_MATCH_CURRENT_NODE"));
            campaign=Require(_expedition.ResolveCommittedCheck(campaign,_content,"EVENT_INJURED_COURIER","R1","R2",2));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N03"));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N04"));
            var skippedQuin=_expedition.CommitMove(campaign,_content,"N05");
            Assert.That(skippedQuin.IsSuccess,Is.False);
            Assert.That(skippedQuin.Errors,Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));
            campaign=Require(_expedition.ResolveCommittedCheck(campaign,_content,"EVENT_COLLAPSED_HANDRAIL","R1","R2",2));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N05"));
            campaign=Require(_expedition.CommitMove(campaign,_content,"N06"));
            var skippedBattle=_expedition.CommitMove(campaign,_content,"N08");
            Assert.That(skippedBattle.IsSuccess,Is.False);
            Assert.That(skippedBattle.Errors,Does.Contain("GC017D_CURRENT_NODE_REQUIRES_RESOLUTION"));
            var wrongEncounter=_expedition.CommitEncounter(campaign,_content,"ENCOUNTER_GATE_GNAWER_ELITE");
            Assert.That(wrongEncounter.IsSuccess,Is.False);
            Assert.That(wrongEncounter.Errors,Does.Contain("GC017D_ENCOUNTER_DOES_NOT_MATCH_CURRENT_NODE"));
        }

        [Test] public void OptionalEliteCanBeFoughtOrSkippedWithoutBlockingTheRoute()
        {
            var campaign = Require(_expedition.AcceptContract(CreateCampaign(), _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = StageActiveExpeditionAt(campaign, "N09",
                new[] { "N00", "N09" }, new[] { "N00", "N09", "N10" });
            var board = _content.Board(
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071);
            var optionalElite = board.Node("N09");
            Assert.That(GuildCityExpeditionService017D.IsEncounterNode(optionalElite), Is.True);
            Assert.That(GuildCityExpeditionService017D.CurrentNodeRequiresResolution(optionalElite), Is.False,
                "Optional elite combat must remain a player choice rather than a mandatory route lock.");

            var expedition = campaign.Guild.GuildCity.Expedition.With(
                status: ExpeditionStatus017D.Active,
                visitedNodeIds: new[] { board.StartNodeId, optionalElite.Id },
                revealedNodeIds: new[] { board.StartNodeId, optionalElite.Id, "N10" },
                lastCheckpointId: "test_optional_elite");
            var city = campaign.Guild.GuildCity.With(
                expedition: expedition,
                replaceExpedition: true,
                lastCheckpointId: "test_optional_elite");
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);

            var fight = _expedition.CommitEncounter(campaign, _content, optionalElite.EncounterId);
            Assert.That(fight.IsSuccess, Is.True, string.Join("\n", fight.Errors));
            Assert.That(fight.Value.Guild.GuildCity.PendingEncounter.EncounterId,
                Is.EqualTo(optionalElite.EncounterId));
            Assert.That(fight.Value.Guild.GuildCity.PendingEncounter.EnemyUnionCount, Is.EqualTo(3));

            var skip = _expedition.CommitMove(campaign, _content, "N10");
            Assert.That(skip.IsSuccess, Is.True, string.Join("\n", skip.Errors));
            Assert.That(skip.Value.Guild.GuildCity.Expedition.CurrentNodeId, Is.EqualTo("N10"));
            Assert.That(skip.Value.Guild.GuildCity.Expedition.ObjectiveFlags,
                Does.Not.Contain(GuildCityExpeditionService017D.EncounterClearedFlag("N09")));
        }

        [Test] public void CheckCommitsOncePerNodeEvenWhenActorChoiceChanges()
        {
            var campaign=Require(_expedition.AcceptContract(CreateBattleReadyCampaign(),_content,"CONTRACT_BELL_BENEATH_GATE"));
            campaign=ReachFirstHourEngineeringCheck071(campaign);
            campaign=Require(_expedition.ResolveCommittedCheck(campaign,_content,"EVENT_COLLAPSED_HANDRAIL","R1","R2",2));
            var canonical=CanonicalJson.Serialize(campaign.Guild.GuildCity.Expedition.CommittedChecks);
            campaign=Require(_expedition.ResolveCommittedCheck(campaign,_content,"EVENT_COLLAPSED_HANDRAIL","R3","R4",99));
            Assert.That(CanonicalJson.Serialize(campaign.Guild.GuildCity.Expedition.CommittedChecks),Is.EqualTo(canonical));
            Assert.That(campaign.Guild.GuildCity.Expedition.CommittedChecks.Count(
                value => value.NodeId == "N04"),Is.EqualTo(1));
        }

        [Test] public void RecurringApplicantBoardIsCommittedAndCannotReroll()
        {
            var first = Require(LegacyApplicantBoardFixture124.Commit(CreateFundedCampaign(), _recruitmentContent, _content));
            var second = Require(_recruitment.CommitBoard(first, _recruitmentContent, _content));
            Assert.That(first.Guild.GuildCity.RecruitmentBoard, Is.Not.Null);
            Assert.That(second, Is.SameAs(first), "Every existing unconsumed offer remains committed without a new lead.");
        }

        [Test] public void GuildHousingRosterCapacityMatchesAuthoredProgressionAndClampsAtMax()
        {
            var housing = _content.Building("GC017D_BUILD_GUILD_HOUSING");
            var effects = new GuildCityEffectService017D();

            Assert.That(housing.FacilityId, Is.EqualTo("FACILITY_DORMITORIES"));
            Assert.That(housing.MaxLevel,
                Is.EqualTo(GuildCityEffectService017D.MaximumAuthoredGuildHousingLevel));
            Assert.That(Enumerable.Range(0, housing.MaxLevel + 1)
                    .Select(effects.RosterCapacityAtDormitoryLevel),
                Is.EqualTo(new[] { 12, 24, 40, 75, 125 }));
            Assert.That(effects.RosterCapacityAtDormitoryLevel(-1), Is.EqualTo(12));
            Assert.That(effects.RosterCapacityAtDormitoryLevel(housing.MaxLevel + 1),
                Is.EqualTo(125));
        }

        [Test] public void NormalRecruitmentReachesSixtyAndStopsOnlyAtAuthoredDormitoryCapacity()
        {
            var campaign = CreateCityFundedCampaign();
            var city = campaign.Guild.GuildCity.With(
                materials: new[]
                {
                    new GuildMaterialState017D("MAT_SALVAGED_TIMBER", 40),
                    new GuildMaterialState017D("MAT_GATE_IRON", 20),
                    new GuildMaterialState017D("MAT_GATEGLASS", 4)
                },
                cityPlots: GuildCityCommandService017D.AdvanceOpeningCityProject(
                    campaign.Guild.GuildCity.CityPlots,
                    GuildCityCommandService017D.OpeningPlotUnlockProgress));
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
            campaign = Require(_city.PlaceBuilding(
                campaign, _content, "GC017D_PLOT_07", "GC017D_BUILD_GUILD_HOUSING"));
            campaign = Require(_city.UpgradeBuilding(campaign, _content, "GC017D_PLOT_07"));
            campaign = Require(_city.UpgradeBuilding(campaign, _content, "GC017D_PLOT_07"));

            var effects = new GuildCityEffectService017D();
            Assert.That(campaign.Guild.GuildCity.CityPlots.Single(value =>
                value.PlotId == "GC017D_PLOT_07").BuildingLevel, Is.EqualTo(3));
            Assert.That(effects.RosterCapacity(campaign.Guild.Development), Is.EqualTo(75));

            var funded = campaign.Guild.With(
                100000,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            campaign = campaign.With(funded, campaign.OpeningFlow);
            campaign = WithRosterCount(campaign, 59);
            campaign = Require(LegacyApplicantBoardFixture124.Commit(
                campaign, _recruitmentContent, _content));
            var applicants = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .Where(value => !campaign.Guild.Recruits.Any(recruit =>
                    StringComparer.Ordinal.Equals(recruit.RecruitId, value.RecruitId)))
                .Take(4)
                .ToArray();
            Assert.That(applicants, Has.Length.EqualTo(4));

            campaign = Require(_recruitment.SignApplicant(campaign, applicants[0].RecruitId));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(60),
                "Ten complete six-member Unions must be reachable through normal signing.");
            campaign = Require(_recruitment.SignApplicant(campaign, applicants[1].RecruitId));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(61),
                "Sixty is a formation target, not an early recruitment wall.");

            campaign = WithRosterCount(campaign, 74);
            campaign = Require(_recruitment.SignApplicant(campaign, applicants[2].RecruitId));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(75));
            var treasuryAtCapacity = campaign.Guild.TreasuryXp;
            var blocked = _recruitment.SignApplicant(campaign, applicants[3].RecruitId);
            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Does.Contain("GC017D_ROSTER_CAPACITY_REACHED"));
            Assert.That(campaign.Guild.Recruits, Has.Count.EqualTo(75));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasuryAtCapacity));
        }

        [Test] public void RecurringApplicantBoardWithoutEarnedContactsDoesNotGenerateTravelers124()
        {
            var source = CreateFundedCampaign();
            var campaign = Require(_recruitment.CommitBoard(source, _recruitmentContent, _content));
            Assert.That(campaign, Is.SameAs(source));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard, Is.Null);
        }

        [Test] public void ApplicantsExposeFixedClassWeaponAndRolePathsBeforeSigning()
        {
            var campaign = Require(LegacyApplicantBoardFixture124.Commit(
                CreateFundedCampaign(), _recruitmentContent, _content));
            var applicants = campaign.Guild.GuildCity.RecruitmentBoard.Applicants;
            var paths = applicants.Select(_recruitment.DescribeApplicant067).ToArray();
            Assert.That(paths, Has.Length.EqualTo(applicants.Count));
            Assert.That(paths.All(value => !string.IsNullOrWhiteSpace(value.StartingClassId)), Is.True);
            Assert.That(paths.All(value => !string.IsNullOrWhiteSpace(value.FixedWeaponFamilyId)), Is.True);
            Assert.That(paths.All(value => !string.IsNullOrWhiteSpace(value.WeaponTreeId)), Is.True);
            Assert.That(paths.All(value => !string.IsNullOrWhiteSpace(value.PrimaryRoleTreeId)), Is.True);
            Assert.That(paths.All(value => value.StartingStableNodeIds.Count > 0), Is.True);
            for (var index = 0; index < applicants.Count; index++)
            {
                Assert.That(paths[index].PotentialScore,
                    Is.EqualTo(applicants[index].PotentialBasisPoints));
                Assert.That(paths[index].ClassTrainingXp,
                    Is.EqualTo(GuildCityRecruitmentService017D.TrainingPersonalXpForPotential067(
                        applicants[index].PotentialBasisPoints)));
            }
            var weakest = paths.OrderBy(value => value.PotentialScore).First();
            var strongest = paths.OrderByDescending(value => value.PotentialScore).First();
            Assert.That(strongest.ClassTrainingXp, Is.GreaterThanOrEqualTo(weakest.ClassTrainingXp));
            Assert.That(GuildCityRecruitmentService017D.PotentialBandForScore067(620), Is.EqualTo("STEADY"));
            Assert.That(GuildCityRecruitmentService017D.PotentialBandForScore067(690), Is.EqualTo("HIGH POTENTIAL"));
            Assert.That(GuildCityRecruitmentService017D.PotentialBandForScore067(735), Is.EqualTo("EXCEPTIONAL"));
        }

        [TestCase(0, 50)]
        [TestCase(10, 46)]
        [TestCase(100, 25)]
        public void ClassTrainingConsumesTreasuryAndPersistsPersonalGrowth(int trainingLevel159, int cost159)
        {
            var campaign = CreateFundedCampaign();
            var guild159 = campaign.Guild;
            campaign = campaign.With(guild159.With(guild159.TreasuryXp, guild159.Recruits,
                guild159.Unions, guild159.Inventory, guild159.Development.SetFacilityLevel(
                    "CITYTAB_TRAINING", trainingLevel159, 0)), campaign.OpeningFlow);
            var assignmentBefore = Assignment(campaign, "R1").Kind;
            var treasuryBefore = campaign.Guild.TreasuryXp;
            campaign = Require(_recruitment.TrainGuildMember067(campaign, "R1",
                GuildCityRecruitmentService017D.ClassTrainingFocus067));
            var first = campaign.Guild.Recruits.First(value => value.RecruitId == "R1");
            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(treasuryBefore - cost159));
            Assert.That(first.Progression.TotalPersonalXp,
                Is.EqualTo(GuildCityRecruitmentService017D.TrainingPersonalXpForPotential067(
                    first.PotentialBasisPoints)));
            Assert.That(Assignment(campaign, "R1").Kind,
                Is.EqualTo(assignmentBefore), "One paid session must not remove a hero from their current duty.");
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(campaign.Guild.GuildCity, "R1"), Is.True);
            Assert.That(Assignment(campaign, "R1").TrainingProgress, Is.EqualTo(100));
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("guild_member_class_training_067"));

            campaign = Require(_recruitment.TrainGuildMember067(campaign, "R1",
                GuildCityRecruitmentService017D.ClassTrainingFocus067));
            var second = campaign.Guild.Recruits.First(value => value.RecruitId == "R1");
            Assert.That(second.Progression.TotalPersonalXp,
                Is.EqualTo(first.Progression.TotalPersonalXp * 2));
            Assert.That(second.Progression.Level, Is.GreaterThan(first.Progression.Level));
            Assert.That(Assignment(campaign, "R1").TrainingProgress, Is.EqualTo(200));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasuryBefore - 2 * cost159));
        }

        [TestCase(0, 50)]
        [TestCase(10, 46)]
        public void ArtPracticeGuaranteesMeaningfulUseMasteryAndPersonalXp(int trainingLevel159, int cost159)
        {
            var campaign = Require(LegacyApplicantBoardFixture124.Commit(
                CreateFundedCampaign(), _recruitmentContent, _content));
            var applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants[0];
            campaign = Require(_recruitment.SignApplicant(campaign, applicant.RecruitId));
            var guild159 = campaign.Guild;
            campaign = campaign.With(guild159.With(guild159.TreasuryXp, guild159.Recruits,
                guild159.Unions, guild159.Inventory, guild159.Development.SetFacilityLevel(
                    "CITYTAB_TRAINING", trainingLevel159, 0)), campaign.OpeningFlow);
            var before = campaign.Guild.Recruits.First(value =>
                value.RecruitId == applicant.RecruitId);
            var practicedBefore = before.Progression.ArtMastery
                .OrderBy(value => value.MasteryPoints)
                .ThenBy(value => value.MeaningfulUses)
                .ThenBy(value => value.ArtId, StringComparer.Ordinal)
                .First();
            var treasuryBefore = campaign.Guild.TreasuryXp;

            var assignmentBefore = Assignment(campaign, applicant.RecruitId).Kind;

            campaign = Require(_recruitment.TrainGuildMember067(campaign, applicant.RecruitId,
                GuildCityRecruitmentService017D.ArtPracticeFocus067));
            var after = campaign.Guild.Recruits.First(value =>
                value.RecruitId == applicant.RecruitId);
            var practicedAfter = after.Progression.ArtMastery.First(value =>
                value.ArtId == practicedBefore.ArtId);
            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(treasuryBefore - cost159));
            Assert.That(practicedAfter.MeaningfulUses,
                Is.EqualTo(practicedBefore.MeaningfulUses + 1));
            Assert.That(practicedAfter.MasteryPoints,
                Is.EqualTo(practicedBefore.MasteryPoints +
                    GuildCityRecruitmentService017D.ArtPracticeMasteryPointsForPotential067(
                        after.PotentialBasisPoints)));
            Assert.That(after.Progression.TotalPersonalXp,
                Is.EqualTo(before.Progression.TotalPersonalXp +
                    GuildCityRecruitmentService017D.ArtPracticePersonalXpForPotential067(
                        after.PotentialBasisPoints)));
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("guild_member_art_practice_067"));
            Assert.That(Assignment(campaign, applicant.RecruitId).Kind, Is.EqualTo(assignmentBefore));
        }

        [Test] public void MemberDevelopmentUnlocksBothEarnableTreesAndPersistsTheirFirstArts()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var catalog = DeepProgressionCatalog070.LoadFromContentRoot(contentRoot);
            var progression = new RecruitTreeProgressionService070(catalog);
            var funded = CreateFundedCampaign();
            var city = funded.Guild.GuildCity;
            var strategic = city.Strategic017H.With(storyGates: city.Strategic017H.StoryGates
                .Concat(new[] { "IRON_WALL_TRUST_1" }).ToArray());
            city = city.With(
                recruitmentDryStreak: DetailedApplicantBoardGenerator.MercyThresholdBoards,
                strategic017H: strategic,
                replaceStrategic017H: true);
            funded = funded.With(funded.Guild.WithGuildCity(city), funded.OpeningFlow);
            var campaign = Require(LegacyApplicantBoardFixture124.Commit(
                funded, _recruitmentContent, _content));
            var applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .Single(value => StringComparer.Ordinal.Equals(
                    value.SignatureId, "SIG_W01_02"));
            campaign = Require(_recruitment.SignApplicant(campaign, applicant.RecruitId));
            campaign = Require(_city.SetAssignment(campaign, applicant.RecruitId, GuildMemberAssignmentKind017D.Active));
            var signed = campaign.Guild.Recruits.First(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, applicant.RecruitId));
            var plan = catalog.RecruitPlan(signed.SignatureId);
            var mysticTree = plan.Slot(RecruitTreeSlotKind070.Mystic).TreeId;
            var secondaryTree = plan.Slot(RecruitTreeSlotKind070.SecondaryRole).TreeId;
            var mysticFirstArt = catalog.Tree(mysticTree).RootNodeId;
            var secondaryFirstArt = catalog.Tree(secondaryTree).RootNodeId;
            var treasuryBefore = campaign.Guild.TreasuryXp;
            var trainingBefore = Assignment(campaign, signed.RecruitId).TrainingProgress;
            var assignmentBefore159 = Assignment(campaign, signed.RecruitId).Kind;
            var unionsBefore159 = CanonicalJson.Serialize(campaign.Guild.Unions);

            campaign = Require(_recruitment.UnlockGuildMemberTree070(
                campaign,
                signed.RecruitId,
                mysticTree,
                progression));
            Assert.That(Assignment(campaign, signed.RecruitId).Kind, Is.EqualTo(assignmentBefore159));
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(campaign.Guild.GuildCity, signed.RecruitId), Is.True);
            campaign = Require(_recruitment.UnlockGuildMemberTree070(
                campaign,
                signed.RecruitId,
                secondaryTree,
                progression));

            var unlocked = campaign.Guild.Recruits.First(value =>
                StringComparer.Ordinal.Equals(value.RecruitId, signed.RecruitId));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(
                treasuryBefore - 2 * GuildCityRecruitmentService017D.MemberTreeUnlockCostTreasuryXp070));
            Assert.That(unlocked.Progression.UnlockedTreeIds, Does.Contain(mysticTree));
            Assert.That(unlocked.Progression.UnlockedTreeIds, Does.Contain(secondaryTree));
            Assert.That(unlocked.Progression.LearnedArtIds, Does.Contain(mysticFirstArt));
            Assert.That(unlocked.Progression.LearnedArtIds, Does.Contain(secondaryFirstArt));
            Assert.That(Assignment(campaign, signed.RecruitId).Kind,
                Is.EqualTo(assignmentBefore159), "Instant tree development must preserve the hero's active duty.");
            Assert.That(GuildMemberDeploymentPolicy017D.IsRecruitDeployable(campaign.Guild.GuildCity, signed.RecruitId), Is.True);
            Assert.That(CanonicalJson.Serialize(campaign.Guild.Unions), Is.EqualTo(unionsBefore159));
            Assert.That(Assignment(campaign, signed.RecruitId).TrainingProgress,
                Is.EqualTo(trainingBefore + 200));
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("guild_member_tree_unlocked_070"));

            var duplicate = _recruitment.UnlockGuildMemberTree070(
                campaign,
                signed.RecruitId,
                mysticTree,
                progression);
            Assert.That(duplicate.IsSuccess, Is.False);
            Assert.That(duplicate.Errors, Does.Contain("GC017D_MEMBER_TREE_ALREADY_UNLOCKED"));
        }

        [Test] public void DecliningAnInterviewRemovesOnlyThatApplicantAndNeverTouchesTheRoster()
        {
            var campaign = Require(LegacyApplicantBoardFixture124.Commit(
                CreateFundedCampaign(), _recruitmentContent, _content));
            var boardBefore = campaign.Guild.GuildCity.RecruitmentBoard;
            var declined = boardBefore.Applicants[0];
            var rosterBefore = campaign.Guild.Recruits
                .Select(value => value.RecruitId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            campaign = Require(_recruitment.DeclineApplicant(campaign, declined.RecruitId));

            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.Applicants.Count,
                Is.EqualTo(boardBefore.Applicants.Count - 1));
            Assert.That(campaign.Guild.GuildCity.RecruitmentBoard.FindApplicant(declined.RecruitId), Is.Null);
            Assert.That(campaign.Guild.Recruits.Select(value => value.RecruitId)
                .OrderBy(value => value, StringComparer.Ordinal), Is.EqualTo(rosterBefore));
            Assert.That(campaign.Guild.GuildCity.LastCheckpointId,
                Is.EqualTo("recurring_applicant_declined"));
        }

        [Test] public void FirstRecurringApplicantUsesCharterCreditThenNormalCostsResume()
        {
            var foundingCampaign = CreateFoundingCampaign();
            var blockedContract = _expedition.AcceptContract(
                foundingCampaign, _content, GuildCityExpeditionService017D.FirstStoryContractId066);
            Assert.That(blockedContract.IsSuccess, Is.False);
            Assert.That(blockedContract.Errors,
                Does.Contain("GC017D_FIRST_RECURRING_RECRUIT_REQUIRED"));

            var campaign = Require(LegacyApplicantBoardFixture124.Commit(
                foundingCampaign, _recruitmentContent, _content));
            var applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants[0];
            Assert.That(applicant.SigningCostTreasuryXp, Is.GreaterThan(0),
                "The charter covers the consequence without rewriting the applicant's normal cost.");
            var treasuryBefore = campaign.Guild.TreasuryXp;
            campaign = Require(_recruitment.SignApplicant(campaign, applicant.RecruitId));
            var signed = campaign.Guild.Recruits.First(value => value.RecruitId == applicant.RecruitId);
            Assert.That(signed.Progression.LearnedArtIds.Count, Is.GreaterThan(0));
            Assert.That(signed.Equipment.Assignments.Count, Is.GreaterThan(0),
                "The signed recruit must arrive with a legal loadout that the equipment screen can review.");
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasuryBefore),
                "A fresh completed opening with zero Treasury XP must still be able to recruit once.");
            var recruitCount = campaign.Guild.Recruits.Count;
            var treasuryAfter = campaign.Guild.TreasuryXp;
            campaign = Require(_recruitment.SignApplicant(campaign, applicant.RecruitId));
            Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(recruitCount));
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(treasuryAfter));
            Assert.That(Assignment(campaign, applicant.RecruitId).Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Reserve));

            var opening = new M1CommandService();
            campaign = Require(opening.AddOpeningUnion(campaign));
            var newUnionIndex = campaign.Guild.Unions.Count - 1;
            campaign = Require(opening.AssignRecruitToUnion(
                campaign, applicant.RecruitId, newUnionIndex, 0));
            Assert.That(campaign.Guild.Unions[newUnionIndex].MemberRecruitIds,
                Does.Contain(applicant.RecruitId));
            Assert.That(campaign.Guild.Unions[newUnionIndex].LeaderRecruitId,
                Is.EqualTo(applicant.RecruitId));
            var unionValidation = opening.ValidateGuildUnionPlans(campaign.Guild);
            Assert.That(unionValidation.IsSuccess, Is.True,
                "Post-opening plans may include the new member while the six-founder opening rule remains unchanged.\n" +
                string.Join("\n", unionValidation.Errors));
            campaign = Require(opening.CompleteOpening(campaign));
            Assert.That(campaign.OpeningFlow.Stage, Is.EqualTo(OpeningStage.Complete));

            var accepted = _expedition.AcceptContract(
                campaign, _content, GuildCityExpeditionService017D.FirstStoryContractId066);
            Assert.That(accepted.IsSuccess, Is.True, string.Join("\n", accepted.Errors));
            var begun = Require(_expedition.StartExpedition(accepted.Value, _content));
            Assert.That(Assignment(begun, applicant.RecruitId).Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Deployed),
                "Every nonempty ready Union plan shown by the builder must join the expedition.");

            var fundedGuild = campaign.Guild.With(5000, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory);
            campaign = campaign.With(fundedGuild, campaign.OpeningFlow);
            var nextApplicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .First(value => !campaign.Guild.Recruits.Any(recruit =>
                    StringComparer.Ordinal.Equals(recruit.RecruitId, value.RecruitId)));
            campaign = Require(_recruitment.SignApplicant(campaign, nextApplicant.RecruitId));
            Assert.That(campaign.Guild.TreasuryXp,
                Is.EqualTo(5000 - nextApplicant.SigningCostTreasuryXp),
                "Only the first recurring signature is charter-covered; later costs remain authoritative.");
        }

        [Test] public void UnionBuilderMigratesRetiredLineFormationWithoutMakingItSelectable()
        {
            var campaign = CreateFoundingCampaign();
            var legacyUnions = campaign.Guild.Unions.Select(union => new UnionState(
                union.UnionId,
                union.DisplayName,
                union.Kind,
                union.LeaderRecruitId,
                union.MemberRecruitIds,
                "FORMATION_LINE",
                union.DoctrineId,
                union.SharedAp,
                union.CohesionBasisPoints)).ToArray();
            var legacyGuild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                legacyUnions,
                campaign.Guild.Inventory);
            campaign = campaign.With(legacyGuild, campaign.OpeningFlow);

            var opening = new M1CommandService();
            Assert.That(opening.ValidateGuildUnionPlans(campaign.Guild).IsSuccess, Is.False);
            var completedLegacySave = Require(opening.CompleteOpening(campaign));
            Assert.That(completedLegacySave.Guild.Unions
                .All(union => union.FormationId == "FORMATION_SKIRMISH_LINE"), Is.True);
            Assert.That(opening.ValidateGuildUnionPlans(completedLegacySave.Guild).IsSuccess, Is.True);

            campaign = Require(opening.AddOpeningUnion(campaign));

            Assert.That(campaign.Guild.Unions.Take(2)
                .All(union => union.FormationId == "FORMATION_SKIRMISH_LINE"), Is.True);
            Assert.That(OpeningUnionCatalog.IsFormation("FORMATION_LINE"), Is.False);
            Assert.That(opening.ValidateGuildUnionPlans(campaign.Guild).IsSuccess, Is.True);
        }

        [Test] public void BoardRefreshConsumesNoOperationAndIsDeterministic()
        {
            Assert.That(
                GuildCityRecruitmentService017D.NextBoardRefreshCostTreasuryXp067(0),
                Is.EqualTo(100L));
            Assert.That(
                GuildCityRecruitmentService017D.NextBoardRefreshCostTreasuryXp067(1),
                Is.EqualTo(125L));
            var campaign = Require(LegacyApplicantBoardFixture124.Commit(CreateFundedCampaign(), _recruitmentContent, _content));
            var operation = campaign.Guild.GuildCity.OperationOrdinal;
            var first = Require(_recruitment.RefreshBoard(campaign, _recruitmentContent, _content));
            var fresh = Require(LegacyApplicantBoardFixture124.Commit(CreateFundedCampaign(), _recruitmentContent, _content));
            fresh = Require(_recruitment.RefreshBoard(fresh, _recruitmentContent, _content));
            Assert.That(first.Guild.GuildCity.OperationOrdinal, Is.EqualTo(operation));
            Assert.That(first, Is.SameAs(campaign), "No earned addition means no notice charge or refresh ordinal advance.");
            Assert.That(first.Guild.GuildCity.RecruitmentRefreshOrdinal, Is.EqualTo(campaign.Guild.GuildCity.RecruitmentRefreshOrdinal));
            Assert.That(CanonicalJson.Serialize(first.Guild.GuildCity.RecruitmentBoard),
                Is.EqualTo(CanonicalJson.Serialize(fresh.Guild.GuildCity.RecruitmentBoard)));
        }

        [Test] public void StaffAssignmentIsUniqueAndPausesDuringDeploymentThenRestores()
        {
            var campaign = CreateCityFundedCampaign();
            campaign = Require(_city.PlaceBuilding(campaign, _content,
                "GC017D_PLOT_01", "GC017D_BUILD_RECRUITMENT_OFFICE"));
            campaign = Require(_city.PlaceBuilding(campaign, _content,
                "GC017D_PLOT_02", "GC017D_BUILD_CONTRACT_HOUSE"));
            campaign = Require(_city.AssignStaff(campaign, "GC017D_PLOT_01", "R1"));
            campaign = Require(_city.AssignStaff(campaign, "GC017D_PLOT_02", "R1"));
            Assert.That(campaign.Guild.GuildCity.CityPlots.Single(value => value.PlotId == "GC017D_PLOT_01")
                .StaffRecruitIds, Does.Not.Contain("R1"));
            Assert.That(campaign.Guild.GuildCity.CityPlots.Single(value => value.PlotId == "GC017D_PLOT_02")
                .StaffRecruitIds, Contains.Item("R1"));
            var effects = new GuildCityEffectService017D();
            Assert.That(effects.ActiveStaffCount(campaign.Guild.GuildCity,
                "GC017D_BUILD_CONTRACT_HOUSE"), Is.EqualTo(1));

            campaign = Require(_expedition.AcceptContract(campaign, _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            Assert.That(Assignment(campaign, "R1").Kind, Is.EqualTo(GuildMemberAssignmentKind017D.Deployed));
            Assert.That(effects.ActiveStaffCount(campaign.Guild.GuildCity,
                "GC017D_BUILD_CONTRACT_HOUSE"), Is.Zero,
                "Deployment pauses the facility contribution but does not remove ownership.");
            Assert.That(campaign.Guild.GuildCity.CityPlots.Single(value => value.PlotId == "GC017D_PLOT_02")
                .StaffRecruitIds, Contains.Item("R1"));

            var city = campaign.Guild.GuildCity;
            var terminal = city.Expedition.With(status: ExpeditionStatus017D.Completed);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city.With(
                expedition: terminal, replaceExpedition: true)), campaign.OpeningFlow);
            campaign = Require(_expedition.FinalizeCompletedExpedition(campaign, _content));
            Assert.That(campaign.Guild.GuildCity.Expedition, Is.Null,
                "Returning through the physical exit must close the finished expedition instead of reopening it forever.");
            Assert.That(campaign.Guild.GuildCity.ActiveContract.Completed, Is.True);
            Assert.That(Assignment(campaign, "R1").Kind, Is.EqualTo(GuildMemberAssignmentKind017D.Staff));
            Assert.That(effects.ActiveStaffCount(campaign.Guild.GuildCity,
                "GC017D_BUILD_CONTRACT_HOUSE"), Is.EqualTo(1));
        }

        [Test] public void TrainingAssignmentCreatesRealDeploymentOpportunityCost()
        {
            var opening = new M1CommandService();
            var campaign = CreateBattleReadyCampaign();
            campaign = Require(_city.SetAssignment(
                campaign, "R7", GuildMemberAssignmentKind017D.Training));

            var assignTrainingReserve = opening.AssignRecruitToUnion(campaign, "R7", 0, 0);
            Assert.That(assignTrainingReserve.IsSuccess, Is.False);
            Assert.That(assignTrainingReserve.Errors,
                Does.Contain(GuildMemberDeploymentPolicy017D.TrainingUnavailableError),
                "A member who is training must stay unavailable to the Union builder until reassigned.");

            campaign = Require(_city.SetAssignment(
                campaign, "R1", GuildMemberAssignmentKind017D.Training));
            Assert.That(Assignment(campaign, "R1").Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Training));
            var invalidPlan = opening.ValidateGuildUnionPlans(campaign.Guild);
            Assert.That(invalidPlan.IsSuccess, Is.False);
            Assert.That(invalidPlan.Errors,
                Does.Contain(GuildMemberDeploymentPolicy017D.TrainingUnavailableError));

            var accepted = Require(_expedition.AcceptContract(
                campaign, _content, GuildCityExpeditionService017D.FirstStoryContractId066));
            var blockedLaunch = _expedition.StartExpedition(accepted, _content);
            Assert.That(blockedLaunch.IsSuccess, Is.False);
            Assert.That(blockedLaunch.Errors,
                Does.Contain(GuildMemberDeploymentPolicy017D.TrainingUnavailableError));
            Assert.That(accepted.Guild.GuildCity.Expedition, Is.Null);
            Assert.That(Assignment(accepted, "R1").Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Training));

            var compatibilityBattle = Require(_battles.StartEncounterBattle(
                campaign,
                _combatContent,
                "BATTLE_TRAINING_OPPORTUNITY_COST_079",
                "Confirm that training members stay home.",
                1));
            Assert.That(compatibilityBattle.Battle.PlayerUnions
                    .SelectMany(value => value.Members)
                    .Select(value => value.MemberId),
                Does.Not.Contain("R1"),
                "Battle materialization must preserve the opportunity cost even if a legacy caller bypasses plan validation.");

            campaign = Require(_city.SetAssignment(
                accepted, "R1", GuildMemberAssignmentKind017D.Active));
            Assert.That(opening.ValidateGuildUnionPlans(campaign.Guild).IsSuccess, Is.True,
                "Explicitly reassigning the member to Active must make the saved Union legal again.");
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            Assert.That(Assignment(campaign, "R1").Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Deployed));
        }

        [Test] public void TrainingMemberStaysTrainingAndGainsProgressAfterCompletedOperation()
        {
            var campaign = CreateBattleReadyCampaign();
            campaign = Require(_city.SetAssignment(
                campaign, "R7", GuildMemberAssignmentKind017D.Training));
            var before = Assignment(campaign, "R7");
            var operationBefore = campaign.Guild.GuildCity.OperationOrdinal;
            var expectedGain = new GuildCityEffectService017D().TrainingProgressPerOperation(
                campaign.Guild.Development,
                campaign.Guild.GuildCity,
                _content);

            campaign = Require(_expedition.AcceptContract(
                campaign, _content, GuildCityExpeditionService017D.FirstStoryContractId066));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            Assert.That(Assignment(campaign, "R7").Kind,
                Is.EqualTo(GuildMemberAssignmentKind017D.Training),
                "Starting an operation must not overwrite an off-duty trainee as Deployed.");
            Assert.That(Assignment(campaign, "R7").TrainingProgress,
                Is.EqualTo(before.TrainingProgress));

            var city = campaign.Guild.GuildCity;
            var completed = city.Expedition.With(status: ExpeditionStatus017D.Completed);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city.With(
                expedition: completed,
                replaceExpedition: true)), campaign.OpeningFlow);
            campaign = Require(_expedition.FinalizeCompletedExpedition(campaign, _content));

            var after = Assignment(campaign, "R7");
            Assert.That(after.Kind, Is.EqualTo(GuildMemberAssignmentKind017D.Training));
            Assert.That(after.TrainingProgress,
                Is.EqualTo(before.TrainingProgress + expectedGain),
                "A completed operation must grant exactly the progress promised by the Training assignment.");
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,
                Is.EqualTo(operationBefore + 1));
        }

        [Test] public void CityBuildingsStaffAndAdjacencyChangeRealOperationalEffects()
        {
            var campaign = CreateCityFundedCampaign();
            var effects = new GuildCityEffectService017D();
            var baselineSlots = effects.ApplicantBoardBonusSlots(campaign.Guild.Development,
                campaign.Guild.GuildCity, _content);
            campaign = Require(_city.PlaceBuilding(campaign, _content,
                "GC017D_PLOT_01", "GC017D_BUILD_RECRUITMENT_OFFICE"));
            Assert.That(effects.ApplicantBoardBonusSlots(campaign.Guild.Development,
                campaign.Guild.GuildCity, _content), Is.EqualTo(baselineSlots + 1));
            campaign = Require(_city.AssignStaff(campaign, "GC017D_PLOT_01", "R1"));
            Assert.That(effects.ApplicantBoardBonusSlots(campaign.Guild.Development,
                campaign.Guild.GuildCity, _content), Is.EqualTo(baselineSlots + 2));

            campaign = Require(_city.PlaceBuilding(campaign, _content,
                "GC017D_PLOT_05", "GC017D_BUILD_FORGE"));
            var suppliesBeforeWarehouse = effects.StartingSupplyBonus(campaign.Guild.Development,
                campaign.Guild.GuildCity, _content);
            campaign = Require(_city.PlaceBuilding(campaign, _content,
                "GC017D_PLOT_06", "GC017D_BUILD_WAREHOUSE"));
            Assert.That(effects.HasAdjacency(campaign.Guild.GuildCity, _content,
                "GC017D_BUILD_FORGE", "GC017D_BUILD_WAREHOUSE"), Is.True);
            Assert.That(effects.StartingSupplyBonus(campaign.Guild.Development,
                campaign.Guild.GuildCity, _content), Is.GreaterThan(suppliesBeforeWarehouse));
        }

        [Test] public void BoardToBattleReturnIsExactOnceAndUsesSelectedAlliedUnions()
        {
            var campaign = CreateBattleReadyCampaign();
            campaign = Require(_expedition.AcceptContract(campaign, _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N01"));
            campaign = CompleteCurrentFirstHourEncounter071(
                campaign, GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071, 2, true);

            campaign = Require(_expedition.CommitMove(campaign, _content, "N02"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_INJURED_COURIER", "R1", "R2", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N03"));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N04"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_COLLAPSED_HANDRAIL", "R1", "R2", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N05"));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N06"));
            campaign = CompleteCurrentFirstHourEncounter071(
                campaign, "ENCOUNTER071_LANTERN_ROAD_AMBUSH", 2, true);

            campaign = Require(_expedition.CommitMove(campaign, _content, "N07"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_LANTERN_WATCH_CAMP", "R1", "R2", 2));
            var chapterOneCampCheck = campaign.Guild.GuildCity.Expedition.CommittedChecks
                .Single(value => value.NodeId == "N07");
            Assert.That(chapterOneCampCheck.EventId,
                Is.EqualTo("EVENT_LANTERN_WATCH_CAMP"));
            Assert.That(chapterOneCampCheck.DieOne, Is.InRange(1, 6));
            Assert.That(chapterOneCampCheck.DieTwo, Is.InRange(1, 6));
            Assert.That(chapterOneCampCheck.CanonicalSeedIdentity,
                Does.Not.StartWith("AUTHORED079_"));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N08"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_UNSTABLE_BELL_CHAIN", "R3", "R4", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N10"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_TRAPPED_FOREMAN", "R3", "R4", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N11"));
            Assert.That(campaign.Guild.GuildCity.Expedition.Urgency, Is.Zero,
                "The full route has spent its Pressure before the final field order.");
            Assert.That(GuildCityExpeditionService017D.CanUseCarefulApproach076(
                campaign.Guild.GuildCity.Expedition.Urgency), Is.False);
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign,
                _content,
                "EVENT_GATEGLASS_PULSE",
                "R5",
                string.Empty,
                GuildCityExpeditionService017D.SwiftApproachModifier076));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N13"));
            campaign = Require(_expedition.MarkFirstHourLanternPatrolRescued071(campaign, _content));
            campaign = CompleteCurrentFirstHourEncounter071(
                campaign, GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071, 3, true);

            Assert.That(campaign.Guild.GuildCity.Expedition.ObjectiveFlags,
                Is.SupersetOf(new[]
                {
                    GuildCityExpeditionService017D.EncounterClearedFlag("N01"),
                    GuildCityExpeditionService017D.EncounterClearedFlag("N06"),
                    GuildCityExpeditionService017D.EncounterClearedFlag("N13")
                }));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N14"));
            Assert.That(campaign.Guild.GuildCity.Expedition.Status,
                Is.EqualTo(ExpeditionStatus017D.Completed));
        }

        [Test] public void CertifiedVersion70BattleUsesTheCommittedPass03Roster()
        {
            var campaign = CreateBattleReadyCampaign();
            campaign = Require(_expedition.AcceptContract(campaign, _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            campaign = ReachFirstHourLanternAmbush071(campaign);
            campaign = Require(_expedition.CommitEncounter(campaign, _content,
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH"));

            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));
            var expected = resolver.Resolve(
                campaign.CampaignSeed,
                campaign.Guild.GuildCity.PendingEncounter);
            campaign = Require(_bridge.StartCertifiedEncounter(
                campaign, _battles, _combatContent, resolver));

            Assert.That(campaign.Battle.EnemyUnions.Select(value => value.UnionId),
                Is.EquivalentTo(expected.Unions.Select(value => value.UnionId)));
            Assert.That(campaign.Battle.EnemyUnions.Select(value => value.DisplayName),
                Is.EquivalentTo(expected.Unions.Select(value => value.Definition.Name)));
            Assert.That(campaign.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Select(value => value.ClassId),
                Is.EquivalentTo(expected.Unions.SelectMany(value => value.Members)
                    .Select(value => value.SourceEnemyId)));
            Assert.That(campaign.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .All(value => value.LearnedArtIds.Count > 0), Is.True);
            Assert.That(campaign.Battle.EnemyUnions.SelectMany(value => value.Members)
                    .Select(value => value.ClassId).Distinct().Count(),
                Is.GreaterThan(3), "The real battle cannot be cloned Gate Gnawer state.");
            Assert.That(campaign.Battle.EnemyUnions.Last().Members.Any(value =>
                    value.ClassId.IndexOf(
                        M2BattleCommandService.GateironBruteClassToken076,
                        StringComparison.OrdinalIgnoreCase) >= 0),
                Is.True,
                "The Road escorts must screen the signature Brute until it can retaliate.");
            var leadUnion = campaign.Battle.PlayerUnions.First();
            var expectedPressure = leadUnion.Members.Sum(value => value.MaximumHp) *
                                   M2BattleCommandService.LanternRoadThreatPercent076 / 100;
            Assert.That(campaign.Battle.EventLog.Single(value =>
                    value.EventType == M2BattleCommandService.StoryThreatTelegraphEventType076).Amount,
                Is.EqualTo(expectedPressure));
            Assert.That(campaign.Battle.CommittedForecasts.Single(value =>
                    value.UnionId == leadUnion.UnionId && value.CommandId == "CMD_GUARD").Risk,
                Does.Contain(expectedPressure + " HP pressure"));
        }

        [Test] public void AuthoredHallBreachGuaranteesItsRealMeaningfulUseBreakthrough076()
        {
            var campaign = CreateBattleReadyCampaign();
            var tenRecruits = campaign.Guild.Recruits.Concat(new[]
            {
                new RecruitState("R8", 100, 100, 20, 20),
                new RecruitState("R9", 100, 100, 20, 20),
                new RecruitState("R10", 100, 100, 20, 20)
            }).ToArray();
            campaign = campaign.With(campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                tenRecruits,
                campaign.Guild.Unions,
                campaign.Guild.Inventory), campaign.OpeningFlow);
            campaign = Require(_expedition.AcceptContract(
                campaign, _content, GuildCityExpeditionService017D.FirstStoryContractId066));
            Assert.That(campaign.Guild.GuildCity.ActiveContract.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N01"));
            campaign = Require(_expedition.CommitEncounter(
                campaign, _content, GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071));
            var resolver = EncounterRosterResolver070.LoadFromContentRoot(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"));

            campaign = Require(_bridge.StartCertifiedEncounter(
                campaign, _battles, _combatContent, resolver));

            Assert.That(campaign.Battle.TutorialBreakthroughMemberId, Is.Not.Empty,
                "The committed Hall Breach must author one eligible learner in battle state, not fake a result toast.");
            Assert.That(campaign.Battle.CommittedForecasts
                    .Where(value => StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT"))
                    .SelectMany(value => value.MemberActions)
                    .Any(value => value.BreakthroughOpportunity), Is.True,
                "The player's first aggressive Union order must visibly forecast the authored learning moment.");

            var active = campaign.Battle.PlayerUnions
                .Where(value => !value.Retreated && !value.IsDefeated)
                .ToArray();
            foreach (var union in active)
            {
                var forecast = campaign.Battle.CommittedForecasts.FirstOrDefault(value =>
                                   StringComparer.Ordinal.Equals(value.UnionId, union.UnionId) &&
                                   StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT")) ??
                               campaign.Battle.CommittedForecasts.First(value =>
                                   StringComparer.Ordinal.Equals(value.UnionId, union.UnionId));
                campaign = Require(_battles.SelectForecast(campaign, union.UnionId, forecast.ForecastId));
            }
            campaign = Require(_battles.ConfirmRound(campaign, _combatContent));

            Assert.That(campaign.Battle.TutorialBreakthroughOccurred, Is.True);
            Assert.That(campaign.Battle.EventLog.Any(value =>
                StringComparer.Ordinal.Equals(value.EventType, "BREAKTHROUGH")), Is.True,
                "The breakthrough must be emitted by authoritative round resolution after meaningful Art use.");
        }

        [Test] public void QuickBattleCannotMasqueradeAsACommittedStoryBattle()
        {
            var campaign = CreateBattleReadyCampaign();
            campaign = Require(_expedition.AcceptContract(campaign, _content,
                "CONTRACT_BELL_BENEATH_GATE"));
            campaign = Require(_expedition.StartExpedition(campaign, _content));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N01"));
            campaign = Require(_expedition.CommitEncounter(campaign, _content,
                GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071));
            var committedBattleId = campaign.Guild.GuildCity.PendingEncounter.BattleId;

            var directQuickBattle = _battles.StartEncounterBattle(
                campaign,
                _combatContent,
                "BATTLE_QUICK_PLAY_069",
                "Optional practice battle.",
                1);
            Assert.That(directQuickBattle.IsSuccess, Is.False);
            Assert.That(directQuickBattle.Errors,
                Does.Contain("M2_PENDING_ENCOUNTER_REQUIRES_COMMITTED_BRIDGE"),
                "A quick battle must be rejected before it can replace a committed story encounter.");

            // Older saves could already contain an unrelated active battle beside a
            // committed encounter. Reconstruct that legacy shape directly so the
            // bridge's second line of defense still proves it cannot masquerade as
            // the exact story battle.
            var legacyQuickBattle = Require(_battles.StartEncounterBattle(
                CreateBattleReadyCampaign(),
                _combatContent,
                "BATTLE_QUICK_PLAY_069",
                "Optional practice battle.",
                1));
            campaign = campaign.WithBattle(legacyQuickBattle.Battle);
            var blocked = _bridge.StartCertifiedEncounter(campaign, _battles, _combatContent);

            Assert.That(blocked.IsSuccess, Is.False);
            Assert.That(blocked.Errors, Does.Contain("M2_ACTIVE_BATTLE_MUST_FINISH"));
            Assert.That(campaign.Battle.BattleId, Is.EqualTo("BATTLE_QUICK_PLAY_069"));
            Assert.That(campaign.Battle.BattleId, Is.Not.EqualTo(committedBattleId));
            var returnBlocked = _bridge.CommitBattleReturn(campaign);
            Assert.That(returnBlocked.IsSuccess, Is.False);
            Assert.That(returnBlocked.Errors, Does.Contain("GC017D_BATTLE_ID_MISMATCH"));
        }

        [Test] public void SaveFormatSevenRoundTripsGuildCityState()
        {
            var path=Path.Combine(Path.GetTempPath(),"SecondDimensionGuildCity017D_"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var campaign=Require(_city.PlaceBuilding(CreateFundedCampaign(),_content,"GC017D_PLOT_01","GC017D_BUILD_RECRUITMENT_OFFICE"));
                campaign=Require(LegacyApplicantBoardFixture124.Commit(campaign,_recruitmentContent,_content));
                var boardJson=CanonicalJson.Serialize(campaign.Guild.GuildCity.RecruitmentBoard);
                var store=new AtomicSaveStore(); store.Write(path,SaveEnvelopeV1.Create(campaign,new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)));
                var loaded=store.ReadWithRecovery(path); Assert.That(loaded.IsSuccess,Is.True,string.Join("\n",loaded.Errors));
                Assert.That(loaded.Value.SaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
                Assert.That(loaded.Value.CampaignState.Guild.GuildCity.CityPlots[0].BuildingId,Is.EqualTo("GC017D_BUILD_RECRUITMENT_OFFICE"));
                Assert.That(CanonicalJson.Serialize(loaded.Value.CampaignState.Guild.GuildCity.RecruitmentBoard),Is.EqualTo(boardJson));
            }
            finally { if(File.Exists(path))File.Delete(path); if(File.Exists(path+".bak"))File.Delete(path+".bak"); }
        }

        [Test] public void StandardRelationshipDecayIsZero()
        {
            Assert.That(ModeCatalog.GetPreset(SecondDimension.Core.GameMode.Standard).RelationshipDecayPct,Is.Zero);
        }

        private static CampaignState CreateCityFundedCampaign()
        {
            var campaign = CreateFundedCampaign();
            var development = campaign.Guild.Development.RecordBattleReward(
                "CITY_TEST_FUND_017D", 500, 500);
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits, campaign.Guild.Unions, campaign.Guild.Inventory, development);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState WithRosterCount(CampaignState campaign, int targetCount)
        {
            if (campaign.Guild.Recruits.Count > targetCount)
                throw new ArgumentOutOfRangeException(nameof(targetCount));
            var recruits = new List<RecruitState>(campaign.Guild.Recruits);
            for (var ordinal = 0; recruits.Count < targetCount; ordinal++)
            {
                var recruitId = "CAPACITY_TEST_MEMBER_" + ordinal.ToString("000");
                if (recruits.Any(value =>
                        StringComparer.Ordinal.Equals(value.RecruitId, recruitId)))
                    continue;
                recruits.Add(new RecruitState(recruitId, 100, 100, 20, 20));
            }
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState CreateBattleReadyCampaign()
        {
            var classes = new[]
            {
                "CLASS_TEND_GUARDIAN", "CLASS_TEND_WARRIOR", "CLASS_TEND_RANGER",
                "CLASS_TEND_PRIEST", "CLASS_TEND_MAGE", "CLASS_TEND_ROGUE"
            };
            var tags = new[]
            {
                new[] { "SHIELD", "SWORD", "WEAPON" }, new[] { "SWORD", "WEAPON" },
                new[] { "BOW", "WEAPON" }, new[] { "STAFF", "HEALING" },
                new[] { "WAND", "FOCUS_TOOL" }, new[] { "DAGGER", "WEAPON" }
            };
            var recruits = new List<RecruitState>();
            for (var index = 0; index < 6; index++)
            {
                var weapon = new EquipmentItemState("ITEM_BATTLE_WEAPON_" + index,
                    "EQ_BATTLE_WEAPON_" + index, "Battle Weapon " + index,
                    new[] { EquipmentSlotIds.MainHand }, tags[index],
                    "QUALITY_STANDARD", 10000, false);
                var armor = new EquipmentItemState("ITEM_BATTLE_ARMOR_" + index,
                    "EQ_BATTLE_ARMOR_" + index, "Battle Armor " + index,
                    new[] { EquipmentSlotIds.BodyArmor }, new[] { "ARMOR" },
                    "QUALITY_STANDARD", 10000, false);
                recruits.Add(new RecruitState("R" + (index + 1), 320, 320, 60, 60,
                    "Recruit " + (index + 1), RecruitOriginKind.Procedural, string.Empty,
                    "HUMAN", "WORLD_GATE_01", classes[index], "Observed", 20000,
                    RecruitAuthorityKind.Normal, string.Empty, string.Empty,
                    new EquipmentLoadoutState(new[]
                    {
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand, weapon),
                        new EquipmentSlotAssignmentState(EquipmentSlotIds.BodyArmor, armor)
                    }), true, string.Empty, string.Empty, 95 + index, 90 + index));
            }
            recruits.Add(new RecruitState("R7", 100, 100, 20, 20));
            var unions = new[]
            {
                new UnionState("U1", "First Union", UnionKind.Normal, "R1",
                    new[] { "R1", "R2", "R3" }, "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED", 36, 9800),
                new UnionState("U2", "Second Union", UnionKind.Normal, "R4",
                    new[] { "R4", "R5", "R6" }, "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED", 36, 9800)
            };
            var guild = new GuildState("GUILD_TEST", 5000, recruits.AsReadOnly(), unions);
            var flow = new OpeningFlowState(OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001", true, null, false, 439, 0,
                true, true, true, true, "complete");
            return new CampaignState("00000000-0000-0000-0000-000000017117", 17117,
                "1.0", ModeRuleSnapshot.StandardDefaults(), guild,
                new NewGuildProfileState("Tester", SecondDimension.Core.GameMode.Standard,
                    TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false), flow);
        }

        private static CampaignState CreateFundedCampaign()
        {
            var campaign = CreateCampaign();
            var guild = campaign.Guild.With(5000, campaign.Guild.Recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState CreateCampaign()
        {
            var campaign = CreateFoundingCampaign();
            var recruits = campaign.Guild.Recruits.Concat(new[]
            {
                new RecruitState("R7", 100, 100, 20, 20)
            }).ToArray();
            var guild = campaign.Guild.With(campaign.Guild.TreasuryXp, recruits,
                campaign.Guild.Unions, campaign.Guild.Inventory);
            return campaign.With(guild, campaign.OpeningFlow);
        }

        private static CampaignState CreateFoundingCampaign()
        {
            var recruits=new[]{new RecruitState("R1",100,100,20,20),new RecruitState("R2",100,100,20,20),new RecruitState("R3",100,100,20,20),new RecruitState("R4",100,100,20,20),new RecruitState("R5",100,100,20,20),new RecruitState("R6",100,100,20,20)};
            var unions=new[]{new UnionState("U1","First Union",UnionKind.Normal,"R1",new[]{"R1","R2","R3"},"FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000),new UnionState("U2","Second Union",UnionKind.Normal,"R4",new[]{"R4","R5","R6"},"FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000)};
            var guild=new GuildState("GUILD_TEST",0,recruits,unions);
            var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete");
            return new CampaignState("00000000-0000-0000-0000-000000017017",17017,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,new NewGuildProfileState("Tester",SecondDimension.Core.GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),flow);
        }

        private CampaignState ReachFirstHourEngineeringCheck071(CampaignState accepted)
        {
            Assert.That(accepted.Guild.GuildCity.ActiveContract.BoardId,
                Is.EqualTo(GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071),
                "Every legal post-recruit opening roster must enter the complete first-hour board.");
            var campaign = Require(_expedition.StartExpedition(accepted, _content));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N01"));
            campaign = CompleteCurrentFirstHourEncounter071(
                campaign,
                GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071,
                2);
            campaign = Require(_expedition.CommitMove(campaign, _content, "N02"));
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_INJURED_COURIER", "R1", "R2", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N03"));
            return Require(_expedition.CommitMove(campaign, _content, "N04"));
        }

        private CampaignState ReachFirstHourLanternAmbush071(CampaignState accepted)
        {
            var campaign = ReachFirstHourEngineeringCheck071(accepted);
            campaign = Require(_expedition.ResolveCommittedCheck(
                campaign, _content, "EVENT_COLLAPSED_HANDRAIL", "R1", "R2", 2));
            campaign = Require(_expedition.CommitMove(campaign, _content, "N05"));
            return Require(_expedition.CommitMove(campaign, _content, "N06"));
        }

        private CampaignState CompleteCurrentFirstHourEncounter071(
            CampaignState campaign,
            string encounterId,
            int expectedEnemyUnionCount,
            bool verifyExactOnce = false)
        {
            var nodeId = campaign.Guild.GuildCity.Expedition.CurrentNodeId;
            campaign = Require(_expedition.CommitEncounter(campaign, _content, encounterId));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter.NodeId, Is.EqualTo(nodeId));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter.EnemyUnionCount,
                Is.EqualTo(expectedEnemyUnionCount));
            campaign = Require(_bridge.StartCertifiedEncounter(campaign, _battles, _combatContent));
            Assert.That(campaign.Battle.PlayerUnions.Count, Is.EqualTo(2));
            Assert.That(campaign.Battle.EnemyUnions.Count, Is.EqualTo(expectedEnemyUnionCount));

            for (var round = 0;
                 round < 80 && campaign.Battle.Outcome == BattleOutcome.InProgress;
                 round++)
            {
                var active = campaign.Battle.PlayerUnions
                    .Where(value => !value.Retreated && !value.IsDefeated)
                    .ToArray();
                for (var index = 0; index < active.Length; index++)
                {
                    var forecast = campaign.Battle.CommittedForecasts.FirstOrDefault(value =>
                                       StringComparer.Ordinal.Equals(value.UnionId, active[index].UnionId) &&
                                       StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT")) ??
                                   campaign.Battle.CommittedForecasts.First(value =>
                                       StringComparer.Ordinal.Equals(value.UnionId, active[index].UnionId));
                    campaign = Require(_battles.SelectForecast(
                        campaign, active[index].UnionId, forecast.ForecastId));
                }
                campaign = Require(_battles.ConfirmRound(campaign, _combatContent));
            }

            Assert.That(campaign.Battle.Outcome, Is.EqualTo(BattleOutcome.Victory),
                nodeId + " must be won before the authored route can continue.");
            campaign = Require(_bridge.CommitBattleReturn(campaign));
            campaign = Require(_battles.ClaimBattleRewards(campaign));
            var materialsBefore = campaign.Guild.GuildCity.Materials.Sum(value => value.Amount);
            campaign = Require(_bridge.ApplyBattleReturnExactlyOnce(campaign));
            Assert.That(campaign.Guild.GuildCity.Expedition.ObjectiveFlags,
                Contains.Item(GuildCityExpeditionService017D.EncounterClearedFlag(nodeId)));
            Assert.That(campaign.Guild.GuildCity.Materials.Sum(value => value.Amount),
                Is.GreaterThan(materialsBefore));

            if (!verifyExactOnce) return campaign;
            var appliedHash = CanonicalJson.Sha256Hex(campaign);
            campaign = Require(_bridge.ApplyBattleReturnExactlyOnce(campaign));
            Assert.That(CanonicalJson.Sha256Hex(campaign), Is.EqualTo(appliedHash),
                nodeId + " battle return must remain idempotent after its receipt is applied.");
            return campaign;
        }

        private static CampaignState StageActiveExpeditionAt(
            CampaignState campaign,
            string nodeId,
            IReadOnlyList<string> visited,
            IReadOnlyList<string> revealed)
        {
            var city = campaign.Guild.GuildCity;
            var staged = city.Expedition.With(
                currentNodeId: nodeId,
                status: ExpeditionStatus017D.Active,
                visitedNodeIds: visited,
                revealedNodeIds: revealed,
                lastCheckpointId: "current_first_hour_board_test_stage");
            city = city.With(
                expedition: staged,
                replaceExpedition: true,
                pendingEncounter: null,
                replacePendingEncounter: true,
                pendingBattleReturn: null,
                replacePendingBattleReturn: true,
                lastCheckpointId: "current_first_hour_board_test_stage");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState WithLegacyExpeditionAt(
            CampaignState campaign,
            string nodeId,
            IReadOnlyList<string> visited,
            IReadOnlyList<string> revealed)
        {
            var source = campaign.Guild.GuildCity.Expedition;
            var legacy = new ExpeditionState017D(
                source.ExpeditionId,
                source.ContractCommitId,
                "BOARD_BELL_BENEATH_GATE",
                nodeId,
                ExpeditionStatus017D.Active,
                source.Supplies,
                source.Fatigue,
                source.Threat,
                source.Urgency,
                visited,
                revealed,
                source.CommittedMoveIds,
                source.CommittedChecks,
                source.ObjectiveFlags,
                "legacy_route_test");
            var sourceContract = campaign.Guild.GuildCity.ActiveContract;
            var legacyContract = new ContractCommitState017D(
                sourceContract.CommitId,
                sourceContract.ContractId,
                GuildCityExpeditionService017D.LegacyFirstRescueBoardId069,
                sourceContract.CanonicalSeedIdentity,
                sourceContract.AcceptedOperationOrdinal,
                sourceContract.Completed,
                sourceContract.Failed);
            var city = campaign.Guild.GuildCity.With(
                activeContract: legacyContract,
                replaceActiveContract: true,
                expedition: legacy,
                replaceExpedition: true,
                lastCheckpointId: "legacy_route_test");
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        private static CampaignState Require(SecondDimension.Core.Result<CampaignState> result){Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
        private static GuildMemberAssignmentState017D Assignment(CampaignState campaign,string id){for(var i=0;i<campaign.Guild.GuildCity.MemberAssignments.Count;i++)if(campaign.Guild.GuildCity.MemberAssignments[i].RecruitId==id)return campaign.Guild.GuildCity.MemberAssignments[i];throw new Exception();}
    }
}
