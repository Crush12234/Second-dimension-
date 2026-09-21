using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BoardQuestCommittedDiceHold090Tests
    {
        [Test]
        public void ActiveCommittedDiceHoldOutranksNextDraftAndRendersPhysicalResult090()
        {
            var sourcePath090 = Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "GuildCity017D",
                "M1FlowPresenter.BoardQuest081.cs");
            var source090 = File.ReadAllText(sourcePath090);

            var experience090 = CompactWhitespace090(Slice090(
                source090,
                "private void BuildBoardQuestExperience081",
                "private static void BuildBoardQuestHeader081"));
            Assert.That(experience090, Does.Contain(
                    "if (ShouldShowBoardQuestCardResolution090(state) || " +
                    "(!ShouldHoldBoardQuestAction081(state) && " +
                    "ShouldBuildBoardQuestCardDraft090(coordinator, state, view)))"),
                "An active reveal hold must prevent the newly available three-card " +
                "draft from preempting a committed dice result.");

            var draft090 = experience090.IndexOf(
                "actions = BuildBoardQuestCardDraft090(",
                StringComparison.Ordinal);
            var scene090 = experience090.IndexOf(
                "BuildBoardQuestScene081(root.transform, state, view);",
                StringComparison.Ordinal);
            var actions090 = experience090.IndexOf(
                "actions = BuildBoardQuestActions081(",
                StringComparison.Ordinal);
            Assert.That(draft090, Is.GreaterThanOrEqualTo(0));
            Assert.That(scene090, Is.GreaterThan(draft090),
                "The guarded draft branch must fall through to the revealed-room scene.");
            Assert.That(actions090, Is.GreaterThan(scene090),
                "The held scene must retain its action panel so the existing unlock can run.");

            var sceneBuilder090 = CompactWhitespace090(Slice090(
                source090,
                "private void BuildBoardQuestScene081",
                "private static string CompactBoardQuestCopy081"));
            Assert.That(sceneBuilder090, Does.Contain(
                    "if (state.Expedition?.HasCommittedCheckAtCurrentNode == true) " +
                    "BuildBoardQuestRollResult081(scene.transform, state.Expedition);"),
                "The held committed-check scene must choose the saved dice-result surface.");

            var resultBuilder090 = Slice090(
                source090,
                "private void BuildBoardQuestRollResult081",
                "private RectTransform BuildBoardQuestActions081");
            Assert.That(resultBuilder090, Does.Contain("BuildAuthoritativeDiceRoll084("));
            Assert.That(resultBuilder090, Does.Contain("Board Quest Dice Result 081"));
            Assert.That(resultBuilder090, Does.Contain("Board Quest Dice Reward 081"));

            var actionBuilder090 = CompactWhitespace090(Slice090(
                source090,
                "private RectTransform BuildBoardQuestActions081",
                "private void BuildBoardQuestDiceChoices081"));
            Assert.That(actionBuilder090, Does.Contain(
                    "if (ShouldHoldBoardQuestAction081(state))"));
            Assert.That(actionBuilder090, Does.Contain(
                    "ScheduleBoardQuestActionUnlock081(state?.Expedition?.CurrentNodeId);"),
                "The physical result hold must still release into the next card draft.");
        }

        private static string Slice090(
            string source090,
            string start090,
            string end090)
        {
            var startIndex090 = source090.IndexOf(start090, StringComparison.Ordinal);
            var endIndex090 = source090.IndexOf(
                end090,
                Math.Max(0, startIndex090 + start090.Length),
                StringComparison.Ordinal);
            Assert.That(startIndex090, Is.GreaterThanOrEqualTo(0),
                "Missing source anchor: " + start090);
            Assert.That(endIndex090, Is.GreaterThan(startIndex090),
                "Missing source anchor after " + start090 + ": " + end090);
            return source090.Substring(startIndex090, endIndex090 - startIndex090);
        }

        private static string CompactWhitespace090(string value090) =>
            string.Join(
                " ",
                (value090 ?? string.Empty).Split(
                    (char[])null,
                    StringSplitOptions.RemoveEmptyEntries));
    }
}
