using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourGoldSmokeCommittedDiceRace090Tests
    {
        [Test]
        public void PostCaptureCommittedDiceRoutesThroughStrictVisibleResultWait090()
        {
            var sourcePath090 = Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.cs");
            var source090 = File.ReadAllText(sourcePath090);

            var commit090 = CompactWhitespace090(Slice090(
                source090,
                "private IEnumerator CommitExpeditionPrimaryOrder078()",
                "private IEnumerator MoveExpeditionBoard078"));
            Assert.That(commit090, Does.Contain(
                    "if (expedition?.HasCommittedCheckAtCurrentNode == true || " +
                    "(expedition?.CurrentEventUsesCommitted2d6 == true && " +
                    "boardView081?.ActionKind == ExpeditionBoardActionKind074.ResolveCheck))"),
                "The asynchronous screenshot can resume after the automatic roll has " +
                "already advanced ResolveCheck to ChooseRoute. That committed state must " +
                "still use the dice-result waiter instead of looking for a route button.");

            var diceWait090 = commit090.IndexOf(
                "yield return WaitForAutomaticBoardQuestDice081(",
                StringComparison.Ordinal);
            var diceExit090 = commit090.IndexOf(
                "yield break;",
                Math.Max(0, diceWait090),
                StringComparison.Ordinal);
            var genericButtonWait090 = commit090.IndexOf(
                "yield return WaitForInteractableButtonByName076(",
                StringComparison.Ordinal);
            Assert.That(diceWait090, Is.GreaterThanOrEqualTo(0));
            Assert.That(diceExit090, Is.GreaterThan(diceWait090));
            Assert.That(genericButtonWait090, Is.GreaterThan(diceExit090),
                "Committed dice must never fall through to the generic primary-button wait.");

            var strictWait090 = CompactWhitespace090(Slice090(
                source090,
                "private IEnumerator WaitForAutomaticBoardQuestDice081",
                "private void RequireQuestCardRounds090"));
            Assert.That(strictWait090, Does.Contain(
                    "expedition081.HasCommittedCheckAtCurrentNode && " +
                    "expedition081.ResolutionComplete && " +
                    "GameObject.Find(\"Board Quest Dice Result 081\") != null)"),
                "The race recovery must still require both saved authority and the live " +
                "physical dice-result surface; state alone is not visual certification.");
            Assert.That(CountOccurrences090(strictWait090, "yield break;"), Is.EqualTo(1),
                "No logic-only alternate success exit may bypass the visible dice result.");
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

        private static int CountOccurrences090(string value090, string token090)
        {
            var count090 = 0;
            var offset090 = 0;
            while (!string.IsNullOrEmpty(value090) &&
                   !string.IsNullOrEmpty(token090) &&
                   (offset090 = value090.IndexOf(
                       token090,
                       offset090,
                       StringComparison.Ordinal)) >= 0)
            {
                count090++;
                offset090 += token090.Length;
            }
            return count090;
        }
    }
}
