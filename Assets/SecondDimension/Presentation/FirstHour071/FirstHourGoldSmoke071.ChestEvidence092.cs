using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.FirstHour071
{
    public sealed partial class FirstHourGoldSmoke071
    {
        private bool _chestCaptureVerified092;

        private IEnumerator CaptureCommittedQuestChest092(string cardId)
        {
            var result = GameObject.Find("Board Quest Card Resolution 090");
            var reveal = result == null ? null : result.GetComponentInChildren<BoardChestReveal092>();
            Require071(reveal != null && reveal.IsPlaying092 && reveal.FrameIndex092 == 0,
                "The genuinely chosen chest did not begin with its authored closed pose.");
            var reward = result.GetComponentsInChildren<Text>(true).FirstOrDefault(value =>
                value.name.StartsWith("Board Quest Resolved Card Reward 090", StringComparison.Ordinal));
            var loot = reveal.transform.Find("Chest Earned Item Art 092")?.GetComponent<Image>();
            Require071(reward != null && !string.IsNullOrWhiteSpace(reward.text) &&
                       loot != null && loot.sprite != null && File.Exists(_savePath),
                "The committed chest did not bind its real reward copy, item sprite, and saved campaign.");
            var savedAfterCommit = File.ReadAllBytes(_savePath);
            var receiptCopy = reward.text;
            var releaseResult = _presenter.HoldChestResultForCapture092();
            try
            {
                for (var pose = 0; pose < 3; pose++)
                {
                    var deadline = Time.realtimeSinceStartup + 6f;
                    while (Time.realtimeSinceStartup < deadline && reveal != null &&
                           (reveal.FrameIndex092 < pose || pose == 2 && reveal.IsPlaying092))
                        yield return null;
                    Require071(reveal != null && reveal.FrameIndex092 == pose &&
                               (pose < 2 ? !reveal.IsLootRevealed092 : reveal.IsLootRevealed092),
                        "The real chest animation skipped required evidence pose " + pose + ".");
                    reveal.HoldCaptureClock092(true);
                    try
                    {
                        var capturedPose = pose;
                        yield return CaptureImmediate071(
                            new[] { "quest_chest_closed_092", "quest_chest_half_open_092", "quest_chest_open_exact_loot_092" }[pose],
                            () => reveal != null && reveal.FrameIndex092 == capturedPose &&
                                  reward != null && StringComparer.Ordinal.Equals(reward.text, receiptCopy) &&
                                  reward.GetComponent<CanvasGroup>().alpha == (capturedPose == 2 ? 1f : 0f),
                            "The chest pose or exact reward changed before its rendered evidence was latched.");
                    }
                    finally { if (reveal != null) reveal.HoldCaptureClock092(false); }
                }
                Require071(File.ReadAllBytes(_savePath).SequenceEqual(savedAfterCommit),
                    "Chest animation/capture changed the committed save; presentation must not grant the reward again.");
                File.WriteAllText(Path.Combine(_evidenceRoot, "quest_chest_receipt_092.txt"),
                    "Actual selected card: " + cardId + Environment.NewLine +
                    "Committed reward: " + receiptCopy + Environment.NewLine +
                    "Bound item sprite: " + loot.sprite.name + Environment.NewLine +
                    "Closed, half-open, and open poses reached naturally; only presentation clocks held during PNG encoding." + Environment.NewLine +
                    "Committed save bytes unchanged during all three captures.");
                _chestCaptureVerified092 = true;
            }
            finally
            {
                if (reveal != null) reveal.HoldCaptureClock092(false);
                releaseResult();
            }
        }
    }
}
