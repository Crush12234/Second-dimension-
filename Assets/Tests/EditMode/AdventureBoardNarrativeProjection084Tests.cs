#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign023;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AdventureBoardNarrativeProjection084Tests
    {
        static readonly Regex AuthorityToken084 = new Regex(
            @"\b(?:CH018|BOARD023|B023|REPEAT020|CRISIS020|MAT020|ENEMYPACK020|LOOTPROFILE020)_[A-Z0-9_]+\b",
            RegexOptions.CultureInvariant);

        CampaignRegistry019 _chapters;
        CampaignRegistry023 _boards;

        [SetUp]
        public void SetUp()
        {
            _chapters = CampaignRegistry019.LoadFromResources();
            _boards = CampaignRegistry023.LoadFromResources();
        }

        [Test]
        public void All130BoardsProjectDistinctTruthfulPlayerCopyWithoutRawIds()
        {
            Assert.That(_boards.Boards, Has.Count.EqualTo(130));
            var allTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allDescriptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var allObjectives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var boardSignatures = new HashSet<string>(StringComparer.Ordinal);
            var roomCount = 0;

            foreach (var board in _boards.Boards.Values
                         .OrderBy(value => value.definitionId, StringComparer.Ordinal))
            {
                var world = _boards.Standing[board.worldId].displayName;
                _chapters.Chapters.TryGetValue(board.definitionId, out var chapter);
                if (StringComparer.Ordinal.Equals(board.operationKind, "CHAPTER"))
                    Assert.That(chapter, Is.Not.Null, board.definitionId);
                else
                    Assert.That(chapter, Is.Null, board.definitionId);

                var copies = new Dictionary<string,
                    AdventureBoardNarrativeProjection084.RoomCopy084>(StringComparer.Ordinal);
                foreach (var node in board.nodes)
                {
                    var copy = AdventureBoardNarrativeProjection084.Project(
                        board, node, chapter, world);
                    var repeated = AdventureBoardNarrativeProjection084.Project(
                        board, node, chapter, world);
                    AssertCopyEqual084(copy, repeated, board.definitionId, node.nodeId);
                    AssertPlayerCopy084(board, node, copy);
                    Assert.That(copy.Description, Does.Not.Contain("choose").IgnoreCase,
                        board.definitionId + " / " + node.nodeId +
                        " must describe the automatic reveal, not a false choice.");
                    Assert.That(copy.Objective, Does.Not.Contain("choose").IgnoreCase,
                        board.definitionId + " / " + node.nodeId +
                        " must expose one phone-simple action.");
                    Assert.That(allTitles.Add(copy.Title), Is.True,
                        "Repeated projected title: " + copy.Title);
                    Assert.That(allDescriptions.Add(copy.Description), Is.True,
                        "Repeated projected description: " + copy.Description);
                    Assert.That(allObjectives.Add(copy.Objective), Is.True,
                        "Repeated projected objective: " + copy.Objective);
                    Assert.That(Normalize084(copy.StoryFlavor),
                        Is.Not.EqualTo(Normalize084(copy.Description)).IgnoreCase,
                        board.definitionId + " / " + node.nodeId);
                    Assert.That(copy.Title.Length, Is.LessThanOrEqualTo(96), node.nodeId);
                    Assert.That(copy.Description.Length, Is.LessThanOrEqualTo(220), node.nodeId);
                    Assert.That(copy.StoryFlavor.Length, Is.LessThanOrEqualTo(340), node.nodeId);
                    Assert.That(copy.Objective.Length, Is.LessThanOrEqualTo(180), node.nodeId);

                    if (StringComparer.OrdinalIgnoreCase.Equals(node.kind, "CAMP"))
                        AssertCampMakesNoUnsupportedPromise084(board, node, copy);
                    if (StringComparer.Ordinal.Equals(board.operationKind, "CRISIS"))
                        AssertCrisisMakesNoUnsupportedPromise084(board, node, copy);
                    if (StringComparer.OrdinalIgnoreCase.Equals(node.kind, "EXIT"))
                        AssertPlainExit084(board, node, copy);

                    foreach (var choice in node.choiceIds ?? Array.Empty<string>())
                    {
                        var label = BoardAdventureRules084.ChoiceLabel084(choice);
                        Assert.That(label, Is.Not.Empty, node.nodeId);
                        Assert.That(label.Length, Is.LessThanOrEqualTo(24), node.nodeId);
                        Assert.That(AuthorityToken084.IsMatch(label), Is.False, node.nodeId);
                        Assert.That(label, Does.Not.Contain("_"), node.nodeId);
                    }
                    copies.Add(node.nodeId, copy);
                    roomCount++;
                }

                Assert.That(copies.Values.Select(value => value.Title).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(board.nodes.Length),
                    board.definitionId + " room titles");
                Assert.That(copies.Values.Select(value => value.Description).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(board.nodes.Length),
                    board.definitionId + " room descriptions");
                Assert.That(copies.Values.Select(value => value.Objective).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(board.nodes.Length),
                    board.definitionId + " room objectives");
                Assert.That(copies.Values.Select(value => value.StoryFlavor).Distinct(
                    StringComparer.OrdinalIgnoreCase).Count(),
                    Is.GreaterThanOrEqualTo(board.nodes.Length - 1),
                    board.definitionId + " story beats");

                var boardObjective = AdventureBoardNarrativeProjection084.BoardObjective(
                    board, chapter, world);
                Assert.That(boardObjective, Is.Not.Empty, board.definitionId);
                Assert.That(AuthorityToken084.IsMatch(boardObjective), Is.False,
                    board.definitionId);
                var signature = boardObjective + "\n" + string.Join("\n",
                    board.nodes.Select(value => copies[value.nodeId].Title + "|" +
                        copies[value.nodeId].Description + "|" +
                        copies[value.nodeId].StoryFlavor));
                Assert.That(boardSignatures.Add(signature), Is.True,
                    "Repeated full board presentation: " + board.definitionId);

                var labels = copies.ToDictionary(
                    value => value.Key, value => value.Value.Title, StringComparer.Ordinal);
                var track = AdventureBoardTrackProjection084.Project(
                    board, board.startNodeId, Array.Empty<string>(), null, labels);
                Assert.That(track.Count, Is.EqualTo(board.nodes.Length), board.definitionId);
                Assert.That(track.Single(value => value.State ==
                    AdventureBoardTrackProjection084.CurrentState).RevealedLabel,
                    Is.EqualTo(copies[board.startNodeId].Title), board.definitionId);
                Assert.That(track.Where(value => value.State !=
                        AdventureBoardTrackProjection084.CurrentState)
                    .All(value => string.IsNullOrWhiteSpace(value.RevealedLabel)), Is.True,
                    board.definitionId + " future room leak");
            }

            Assert.That(roomCount, Is.EqualTo(1372));
            Assert.That(allTitles, Has.Count.EqualTo(1372));
            Assert.That(allDescriptions, Has.Count.EqualTo(1372));
            Assert.That(allObjectives, Has.Count.EqualTo(1372));
            Assert.That(boardSignatures, Has.Count.EqualTo(130));
        }

        static void AssertPlayerCopy084(
            BoardDto023 board,
            NodeDto023 node,
            AdventureBoardNarrativeProjection084.RoomCopy084 copy)
        {
            foreach (var value in new[]
                     {
                         copy.Title, copy.Description, copy.StoryFlavor, copy.Objective
                     })
            {
                Assert.That(value, Is.Not.Null.And.Not.Empty,
                    board.definitionId + " / " + node.nodeId);
                Assert.That(AuthorityToken084.IsMatch(value), Is.False,
                    board.definitionId + " / " + node.nodeId + ": " + value);
                foreach (var id in new[]
                         {
                             node.nodeId, node.sourceId,
                             board.boardId, board.definitionId
                         })
                    if (!string.IsNullOrWhiteSpace(id))
                        Assert.That(value, Does.Not.Contain(id),
                            board.definitionId + " / " + node.nodeId);
            }
        }

        static void AssertCampMakesNoUnsupportedPromise084(
            BoardDto023 board,
            NodeDto023 node,
            AdventureBoardNarrativeProjection084.RoomCopy084 copy)
        {
            var visible = Combined084(copy);
            foreach (var forbidden in new[]
                     {
                         "relationship memory", "relationship memories",
                         "relationship growth", "cohesion reward", "modifier"
                     })
                Assert.That(visible, Does.Not.Contain(forbidden).IgnoreCase,
                    board.definitionId + " / " + node.nodeId);
        }

        static void AssertCrisisMakesNoUnsupportedPromise084(
            BoardDto023 board,
            NodeDto023 node,
            AdventureBoardNarrativeProjection084.RoomCopy084 copy)
        {
            var visible = Combined084(copy);
            foreach (var forbidden in new[]
                     {
                         "assign complete unions", "assign buildings", "buildings",
                         "reserves", "warnings", "relationship growth", "modifier"
                     })
                Assert.That(visible, Does.Not.Contain(forbidden).IgnoreCase,
                    board.definitionId + " / " + node.nodeId);
        }

        static void AssertPlainExit084(
            BoardDto023 board,
            NodeDto023 node,
            AdventureBoardNarrativeProjection084.RoomCopy084 copy)
        {
            Assert.That(copy.Description, Does.Contain("Return to the Guild"),
                board.definitionId + " / " + node.nodeId);
            Assert.That(copy.Description, Does.Not.Contain("authored").IgnoreCase,
                board.definitionId + " / " + node.nodeId);
            Assert.That(copy.Description, Does.Not.Contain("current quest").IgnoreCase,
                board.definitionId + " / " + node.nodeId);
        }

        static void AssertCopyEqual084(
            AdventureBoardNarrativeProjection084.RoomCopy084 first,
            AdventureBoardNarrativeProjection084.RoomCopy084 second,
            string boardId,
            string nodeId)
        {
            Assert.That(second.Title, Is.EqualTo(first.Title), boardId + " / " + nodeId);
            Assert.That(second.Description, Is.EqualTo(first.Description), boardId + " / " + nodeId);
            Assert.That(second.StoryFlavor, Is.EqualTo(first.StoryFlavor), boardId + " / " + nodeId);
            Assert.That(second.Objective, Is.EqualTo(first.Objective), boardId + " / " + nodeId);
        }

        static string Combined084(
            AdventureBoardNarrativeProjection084.RoomCopy084 copy) =>
            copy.Title + "\n" + copy.Description + "\n" +
            copy.StoryFlavor + "\n" + copy.Objective;

        static string Normalize084(string value) => string.Join(" ",
            (value ?? string.Empty).Split(
                new[] {' ', '\r', '\n', '\t'},
                StringSplitOptions.RemoveEmptyEntries)).Trim().TrimEnd('.');
    }
}
#endif
