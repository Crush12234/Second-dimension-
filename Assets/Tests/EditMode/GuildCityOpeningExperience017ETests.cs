using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation.GuildCity017E;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityOpeningExperience017ETests
    {
        private static string ContentDirectory => Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT","GUILD_CITY_017D");

        [Test] public void ThreeOpeningContractsUseThreeDistinctBoards()
        {
            var content=GuildCityContent017D.LoadFromDirectory(ContentDirectory);
            Assert.That(content.Contracts.Count,Is.EqualTo(3));
            Assert.That(content.Contracts.Values.Select(value=>value.BoardId).Distinct(StringComparer.Ordinal).Count(),Is.EqualTo(3));
        }

        [Test] public void EveryOpeningBoardHasFifteenReachableNodesAndRequiredFlow()
        {
            var content=GuildCityContent017D.LoadFromDirectory(ContentDirectory);
            Assert.That(content.Boards.Count,Is.EqualTo(3));
            foreach(var board in content.Boards.Values)
            {
                Assert.That(board.Nodes.Length,Is.EqualTo(15),board.Id);
                var visited=new HashSet<string>(StringComparer.Ordinal); var queue=new Queue<string>(); queue.Enqueue(board.StartNodeId);
                while(queue.Count>0){var id=queue.Dequeue(); if(!visited.Add(id))continue; foreach(var link in board.Node(id).Links??Array.Empty<string>())queue.Enqueue(link);}
                Assert.That(visited.Count,Is.EqualTo(15),board.Id);
                Assert.That(board.Nodes.Any(value=>value.Kind=="CAMP"),Is.True,board.Id);
                Assert.That(board.Nodes.Any(value=>value.Kind=="OPTIONAL_ELITE"),Is.True,board.Id);
                Assert.That(board.Nodes.Any(value=>value.Kind=="MAIN_OBJECTIVE"),Is.True,board.Id);
                Assert.That(visited.Contains(board.ExitNodeId),Is.True,board.Id);
            }
        }

        [Test] public void OpeningEventLibraryIsBroadAndPreservesOwnerLaws()
        {
            var content=GuildCityContent017D.LoadFromDirectory(ContentDirectory);
            Assert.That(content.Events.Count,Is.EqualTo(21));
            var authoredDeterministicEvents = new[]
            {
                "EVENT_FOUND_APPRENTICE", "EVENT_WRONG_ROUTE_MARKS",
                "EVENT_BROKEN_SURVEY_BRIDGE", "EVENT_CAMP_ARGUMENT",
                "EVENT_FOG_ECHO", "EVENT_BROKEN_ASTROLABE",
                "EVENT_ABANDONED_SURVEY_PACK",
                GuildCityExpeditionService017D.FirstStorySafePassageEventId080,
                GuildCityExpeditionService017D.SecondStorySafeDescentEventId080
            };
            foreach(var value in content.Events.Values)
            {
                Assert.That(value.UsesCommitted2d6,
                    Is.EqualTo(!authoredDeterministicEvents.Contains(value.Id)), value.Id);
                Assert.That(value.RelationshipSceneCostsOperation,Is.False,value.Id);
                Assert.That(value.CanCauseDeparture,Is.False,value.Id);
                Assert.That(value.EligibleSkills.Length,Is.GreaterThanOrEqualTo(2),value.Id);
            }
        }

        [Test] public void ExperienceRegistryBindsAllCoreVisualContent()
        {
            var root=GuildCityOpeningExperienceRegistry017E.Load();
            Assert.That(root.contracts.Length,Is.EqualTo(3));
            Assert.That(root.nodes.Length,Is.EqualTo(45));
            Assert.That(root.events.Length,Is.EqualTo(20));
            Assert.That(root.buildings.Length,Is.EqualTo(30));
            Assert.That(root.districts.Length,Is.EqualTo(5));
            Assert.That(root.tutorialBeats.Length,Is.EqualTo(10));
            Assert.That(GuildCityOpeningExperienceRegistry017E.Contract("CONTRACT_BELL_BENEATH_GATE"),Is.Not.Null);
            Assert.That(GuildCityOpeningExperienceRegistry017E.Node("BOARD_RELIEF_ROAD","N13"),Is.Not.Null);
        }

        [Test] public void ChapterTwoPresentationSurfacesTheWitnessEvidenceAndRouteConsequences078()
        {
            const string boardId = "BOARD_LINES_NOT_RETURNED";
            var threshold = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N00");
            var junction = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N01");
            var camp = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N07");
            var fieldBook = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N11");
            var rescue = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N13");
            var returnHome = GuildCityOpeningExperienceRegistry017E.Node(boardId, "N14");

            Assert.That(threshold.kind, Is.EqualTo("EVENT"));
            Assert.That(threshold.displayName, Does.Contain("Sella Vey"));
            Assert.That(threshold.summary,
                Does.Contain("Orra Vale").And.Contain("brass line-tag").And.Contain("Wayglass"));
            Assert.That(junction.displayName, Does.Contain("Seventh-Return"));
            Assert.That(junction.summary, Does.Contain("fresh marks").And.Contain("survey bridge"));
            Assert.That(camp.displayName, Is.EqualTo("Quiet Lantern Camp"));
            Assert.That(camp.summary, Does.Contain("Sella").And.Contain("seventh-return mark"));
            Assert.That(fieldBook.displayName, Does.Contain("Orra's Field Book"));
            Assert.That(fieldBook.summary,
                Does.Contain("crew count").And.Contain("door sketch").And.Contain("sabotage notes"));
            Assert.That(fieldBook.recommendedSkill, Is.EqualTo("Perception"));
            Assert.That(rescue.displayName, Does.Contain("Door Inside"));
            Assert.That(returnHome.displayName, Is.EqualTo("Testimony to Skyhome"));
            Assert.That(returnHome.summary,
                Does.Contain("Sella").And.Contain("Orra").And.Contain("Skyhome")
                    .And.Contain("Wayglass table"),
                "The terminal Chapter 2 beat must tell the player who is returning, where they are going, and why.");

            var apprentice = GuildCityOpeningExperienceRegistry017E.Event("EVENT_FOUND_APPRENTICE");
            var campEvent = GuildCityOpeningExperienceRegistry017E.Event("EVENT_CAMP_ARGUMENT");
            var secondary = GuildCityOpeningExperienceRegistry017E.Event("EVENT_ABANDONED_SURVEY_PACK");
            Assert.That(apprentice.sceneText,
                Does.Contain("Sella Vey").And.Contain("Orra Vale").And.Contain("seventh-return stroke"));
            Assert.That(apprentice.exceptionalText,
                Does.Contain("seventh notch").And.Contain("both routes open"));
            Assert.That(apprentice.relationshipMemory, Does.Contain("brass line-tag"));
            Assert.That(campEvent.relationshipMemory,
                Does.Contain("Sella Vey").And.Contain("Orra Vale"));
            Assert.That(secondary.cityConsequence,
                Does.Contain("field book").And.Contain("Civic Trust"));

            var authorityPath = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT",
                "GUILD_CITY_017E", "OPENING_EXPERIENCE_017E.json");
            var authority = JsonUtility.FromJson<GuildCityOpeningExperienceRoot017E>(
                File.ReadAllText(authorityPath));
            var authorityThreshold = authority.nodes.Single(value =>
                value.boardId == boardId && value.nodeId == "N00");
            var authorityReturnHome = authority.nodes.Single(value =>
                value.boardId == boardId && value.nodeId == "N14");
            var authorityApprentice = authority.events.Single(value =>
                value.id == "EVENT_FOUND_APPRENTICE");
            Assert.That(authorityThreshold.displayName, Is.EqualTo(threshold.displayName),
                "Streaming and Resources presentation copies must not drift before packaging.");
            Assert.That(authorityThreshold.summary, Is.EqualTo(threshold.summary));
            Assert.That(authorityReturnHome.summary, Is.EqualTo(returnHome.summary));
            Assert.That(authorityApprentice.sceneText, Is.EqualTo(apprentice.sceneText));
            Assert.That(authorityApprentice.relationshipMemory, Is.EqualTo(apprentice.relationshipMemory));
        }

        [Test] public void ChapterTwoShippedNarrativePromisesNamedConsequencesWithoutDiceLanguage079()
        {
            const string boardId = "BOARD_LINES_NOT_RETURNED";
            var activeNodes = GuildCityOpeningExperienceRegistry017E.Load().nodes
                .Where(value => value.boardId == boardId)
                .ToArray();
            Assert.That(activeNodes, Has.Length.EqualTo(15));
            var activeCopy = string.Join("\n", activeNodes.Select(value =>
                value.displayName + "\n" + value.summary));
            Assert.That(activeCopy, Does.Contain("Sella Vey"));
            Assert.That(activeCopy, Does.Contain("Orra Vale"));
            Assert.That(activeCopy, Does.Contain("Wayglass"));
            Assert.That(activeCopy, Does.Not.Contain("Route Decision").IgnoreCase);
            Assert.That(activeCopy, Does.Not.Contain("deterministic 2d6").IgnoreCase);

            var legacyPath = Path.Combine(Application.dataPath, "Resources", "SecondDimension",
                "GuildCity017F", "Data", "OPENING_NARRATIVE_017F.json");
            var legacyRoot = JsonUtility.FromJson<
                SecondDimension.Presentation.GuildCity017F.GuildCityNarrativeRoot017F>(
                File.ReadAllText(legacyPath));
            var legacyNodes = legacyRoot.nodeNarratives
                .Where(value => value.boardId == boardId)
                .ToArray();
            Assert.That(legacyNodes, Has.Length.EqualTo(15));
            var legacyCopy = string.Join("\n", legacyNodes.Select(value => string.Join("\n", new[]
            {
                value.title, value.arrivalText, value.decisionPrompt
            })));
            Assert.That(legacyCopy, Does.Contain("Sella"));
            Assert.That(legacyCopy, Does.Contain("Orra"));
            Assert.That(legacyCopy, Does.Contain("Wayglass"));
            Assert.That(legacyCopy, Does.Not.Contain("Route Decision").IgnoreCase);
            Assert.That(legacyCopy, Does.Not.Contain("deterministic 2d6").IgnoreCase);

            var flowSource = File.ReadAllText(Path.Combine(Application.dataPath,
                "SecondDimension", "Presentation", "GuildCity017D", "GuildCityFlowPresenter017D.cs"));
            Assert.That(flowSource,
                Does.Contain("FOLLOW SELLA AND ORRA'S BRASS LINE"));
            Assert.That(flowSource, Does.Contain("EXPOSE THE WAYGLASS FORGERY"));
            Assert.That(flowSource,
                Does.Not.Contain("AMBUSH OR SUPPLY LOSS  •  YOUR FIRST TEST CHOOSES WHICH"));
        }

        [Test] public void ChapterOnePresentationAuthorsAllFifteenStoryNodesAndNamedOutcomes079()
        {
            const string sourceBoardId = "BOARD_BELL_BENEATH_GATE";
            const string liveBoardId = "BOARD_BELL_BENEATH_GATE_071";
            var root = GuildCityOpeningExperienceRegistry017E.Load();
            var nodes = root.nodes
                .Where(value => value.boardId == sourceBoardId)
                .OrderBy(value => value.nodeId, StringComparer.Ordinal)
                .ToArray();
            var requiredStoryByNode = new Dictionary<string, string[]>
            {
                ["N00"] = new[] { "Kael", "Maren", "Guild Hall" },
                ["N01"] = new[] { "Kael", "Kiri", "Battle 1 of 3", "Lantern Road" },
                ["N02"] = new[] { "Una Queensrest", "Zorin", "Wayglass" },
                ["N03"] = new[] { "Tazren", "Zorin", "ambush" },
                ["N04"] = new[] { "Orren", "Quin Lowen", "Lantern Road" },
                ["N05"] = new[] { "Bessa", "Zorin", "patrol cipher" },
                ["N06"] = new[] { "Maren", "Daeven", "Battle 2 of 3", "Zorin" },
                ["N07"] = new[] { "Maren", "Jazzi", "Zorin" },
                ["N08"] = new[] { "Odelia", "Wayglass", "Gatehouse" },
                ["N09"] = new[] { "Daeven", "Zorin", "Gara" },
                ["N10"] = new[] { "Jazzi", "Petra Runebrook", "Zorin", "ten Lanterns" },
                ["N11"] = new[] { "Orren", "Wayglass", "Gatehouse" },
                ["N12"] = new[] { "Gara", "wounded Lanterns", "route ledger" },
                ["N13"] = new[] { "Zorin", "Wayglass", "Gate-Eater", "Battle 3 of 3" },
                ["N14"] = new[] { "Kiri", "Zorin", "Wayglass", "Guild Hall" }
            };
            var forbiddenTemplateCopy = new[]
            {
                "Choose between routes", "recruit-centered problem", "Reveal future nodes",
                "deterministic 2d6", "Recover useful material", "required enemy force",
                "surface free relationship", "Fight for extra rewards", "Complete optional civic work",
                "Avoid some threat", "central crisis", "Extract, commit"
            };

            Assert.That(nodes, Has.Length.EqualTo(15));
            Assert.That(nodes.Select(value => value.nodeId),
                Is.EqualTo(requiredStoryByNode.Keys.OrderBy(value => value, StringComparer.Ordinal)));
            foreach (var node in nodes)
            {
                Assert.That(node.displayName, Is.Not.Empty, node.nodeId);
                Assert.That(node.summary, Is.Not.Empty, node.nodeId);
                Assert.That(node.summary.Length, Is.LessThanOrEqualTo(160),
                    node.nodeId + " should remain readable on the expedition card.");
                foreach (var fragment in requiredStoryByNode[node.nodeId])
                    Assert.That(node.summary, Does.Contain(fragment), node.nodeId);
                foreach (var placeholder in forbiddenTemplateCopy)
                    Assert.That(node.summary.IndexOf(placeholder, StringComparison.OrdinalIgnoreCase),
                        Is.EqualTo(-1),
                        node.nodeId + " still exposes generic board copy.");

                var live = GuildCityOpeningExperienceRegistry017E.Node(liveBoardId, node.nodeId);
                Assert.That(live, Is.Not.Null, node.nodeId);
                Assert.That(live.displayName, Is.EqualTo(node.displayName), node.nodeId);
                Assert.That(live.summary, Is.EqualTo(node.summary),
                    node.nodeId + " must not be replaced by a weaker live projection.");
            }
            Assert.That(GuildCityOpeningExperienceRegistry017E.Node(liveBoardId, "N01").kind,
                Is.EqualTo("ENCOUNTER"));

            var namedEvents = new Dictionary<string, string[]>
            {
                ["EVENT_INJURED_COURIER"] = new[] { "Una Queensrest", "Zorin", "Wayglass" },
                ["EVENT_COLLAPSED_HANDRAIL"] = new[] { "Quin Lowen", "Lantern Road", "Zorin" },
                ["EVENT_UNSTABLE_BELL_CHAIN"] = new[] { "Wayglass", "Odelia", "Zorin" },
                ["EVENT_TRAPPED_FOREMAN"] = new[] { "Petra Runebrook", "Zorin", "all ten Lanterns" },
                ["EVENT_GATEGLASS_PULSE"] = new[] { "Orren", "Zorin", "Gate-Eater" },
                [GuildCityExpeditionService017D.FirstStorySafePassageEventId080] =
                    new[] { "Gara Redtail", "wounded Lanterns", "route ledger" }
            };
            var liveContent = GuildCityContent017D.LoadFromDirectory(ContentDirectory);
            foreach (var expectation in namedEvents)
            {
                var scene = GuildCityOpeningExperienceRegistry017E.Event(expectation.Key);
                var liveEvent = liveContent.Event(expectation.Key);
                Assert.That(scene, Is.Not.Null, expectation.Key);
                var authoredCopy = string.Join(" ", new[]
                {
                    scene.title, scene.sceneText, scene.exceptionalText, scene.fullSuccessText,
                    scene.successWithCostText, scene.setbackText, scene.severeSetbackText,
                    scene.relationshipMemory, scene.cityConsequence
                });
                foreach (var fragment in expectation.Value)
                    Assert.That(authoredCopy, Does.Contain(fragment), expectation.Key);
                Assert.That(authoredCopy.IndexOf("problem is resolved", StringComparison.OrdinalIgnoreCase),
                    Is.EqualTo(-1),
                    expectation.Key);
                Assert.That(authoredCopy.IndexOf("solves the crisis cleanly", StringComparison.OrdinalIgnoreCase),
                    Is.EqualTo(-1),
                    expectation.Key);
                Assert.That(authoredCopy.IndexOf("recoverable setback", StringComparison.OrdinalIgnoreCase),
                    Is.EqualTo(-1),
                    expectation.Key);
                Assert.That(liveEvent.Title, Is.EqualTo(scene.title), expectation.Key);
                Assert.That(liveEvent.Problem, Is.EqualTo(scene.sceneText), expectation.Key);
                Assert.That(liveEvent.ExceptionalText, Is.EqualTo(scene.exceptionalText), expectation.Key);
                Assert.That(liveEvent.FullSuccessText, Is.EqualTo(scene.fullSuccessText), expectation.Key);
                Assert.That(liveEvent.SuccessWithCostText, Is.EqualTo(scene.successWithCostText), expectation.Key);
                Assert.That(liveEvent.SetbackText, Is.EqualTo(scene.setbackText), expectation.Key);
                Assert.That(liveEvent.SevereSetbackText, Is.EqualTo(scene.severeSetbackText), expectation.Key);
                Assert.That(liveEvent.RelationshipMemorySummary,
                    Is.EqualTo(scene.relationshipMemory), expectation.Key);
                Assert.That(liveEvent.ConsequenceIdentity,
                    Is.EqualTo(scene.cityConsequence), expectation.Key);
            }

            var authorityPath = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT",
                "GUILD_CITY_017E", "OPENING_EXPERIENCE_017E.json");
            var resourcesPath = Path.Combine(Application.dataPath, "Resources", "SecondDimension",
                "GuildCity017E", "Data", "OPENING_EXPERIENCE_017E.json");
            Assert.That(File.ReadAllBytes(resourcesPath), Is.EqualTo(File.ReadAllBytes(authorityPath)),
                "StreamingAssets and Resources opening-experience copies must remain byte-identical.");
        }

        [Test] public void EveryBuildingHasIconLevelsAndTwoSpecializationCandidates()
        {
            var root=GuildCityOpeningExperienceRegistry017E.Load();
            foreach(var building in root.buildings)
            {
                Assert.That(building.iconResourcePath,Is.Not.Empty,building.id);
                Assert.That(building.levelEffects.Length,Is.GreaterThanOrEqualTo(3),building.id);
                Assert.That(building.specializationOptions.Length,Is.EqualTo(2),building.id);
                Assert.That(GuildCityOpeningExperienceRegistry017E.Sprite(building.iconResourcePath),Is.Not.Null,building.id);
            }

            var chronicle = root.buildings.Single(value =>
                value.id == "GC017H_BUILD_CHRONICLE_PLAZA");
            var playerCopy = chronicle.shortDescription + "\n" +
                             string.Join("\n", chronicle.levelEffects);
            Assert.That(playerCopy, Does.Contain("recorded victories"));
            Assert.That(playerCopy, Does.Not.Contain("canon events").IgnoreCase,
                "Opening facility screens must use player-facing language, not development terminology.");
        }
    }
}
