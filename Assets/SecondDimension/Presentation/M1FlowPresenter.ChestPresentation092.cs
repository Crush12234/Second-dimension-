using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private readonly HashSet<string> _settledChestReveals092 = new HashSet<string>(StringComparer.Ordinal);

        internal Action HoldChestResultForCapture092()
        {
            // The existing result was already committed. Only its automatic
            // dismissal is held while the release harness captures three poses.
            var remaining = Mathf.Max(0f, _boardQuestActionUnlockAt081 - Time.unscaledTime);
            var started = Time.unscaledTime;
            if (_boardQuestActionUnlockRoutine081 != null)
            {
                StopCoroutine(_boardQuestActionUnlockRoutine081);
                _boardQuestActionUnlockRoutine081 = null;
            }
            _boardQuestActionUnlockAt081 = float.PositiveInfinity;
            return () =>
            {
                _boardQuestActionUnlockAt081 = Time.unscaledTime +
                    Mathf.Max(0.5f, remaining - (Time.unscaledTime - started));
                ScheduleBoardQuestActionUnlock081(_boardQuestActionUnlockNode081);
            };
        }

        private static void UseClosedChestCardArt092(RectTransform card, string artPrefix)
        {
            var art = card.GetComponentsInChildren<Image>(true).FirstOrDefault(value =>
                value.name.StartsWith(artPrefix, StringComparison.Ordinal));
            var sprite = BoardChestReveal092.Frame092(0);
            if (art == null || sprite == null) return;
            art.sprite = sprite;
            art.type = Image.Type.Simple;
            art.preserveAspect = true;
            var aspect = art.GetComponent<AspectRatioFitter>();
            if (aspect == null) aspect = art.gameObject.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        private void AttachChestRewardReveal092(RectTransform result, string artPrefix,
            string headingPrefix, string[] delayedCopyPrefixes, string rewardKey,
            string itemVisualId, string openedHeading, float leadIn = 0f)
        {
            if (result == null) return;
            var art = result.GetComponentsInChildren<Image>(true).FirstOrDefault(value =>
                value.name.StartsWith(artPrefix, StringComparison.Ordinal));
            if (art == null) return;
            var texts = result.GetComponentsInChildren<Text>(true);
            var heading = texts.FirstOrDefault(value => value.name.StartsWith(headingPrefix, StringComparison.Ordinal));
            var delayed = texts.Where(value => delayedCopyPrefixes.Any(prefix =>
                value.name.StartsWith(prefix, StringComparison.Ordinal))).Select(value =>
            {
                var group = value.GetComponent<CanvasGroup>();
                if (group == null) group = value.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                return group;
            }).ToArray();
            Sprite earnedItemArt = null;
            if (!string.IsNullOrWhiteSpace(itemVisualId))
                M1VisualAssets.TryResolveEquipment(itemVisualId, out earnedItemArt, out _);
            var reveal = art.GetComponent<BoardChestReveal092>();
            if (reveal != null) return;
            reveal = art.gameObject.AddComponent<BoardChestReveal092>();
            var animate = Application.isPlaying && !_reducedMotion &&
                          !string.IsNullOrWhiteSpace(rewardKey) && !_settledChestReveals092.Contains(rewardKey);
            var dice132 = result.GetComponentInChildren<CommittedQuestDice132>(true);
            if (dice132 != null && !dice132.IsSettled132) animate = Application.isPlaying;
            reveal.Configure092(art, earnedItemArt, delayed, heading, openedHeading, animate,
                dice132 == null ? leadIn : BoardAdventureRewardAfterDicePause084,
                () => { if ((dice132 == null || dice132.IsSettled132) &&
                    !string.IsNullOrWhiteSpace(rewardKey)) _settledChestReveals092.Add(rewardKey); },
                dice132 == null ? null : (Func<bool>)(() => dice132 != null && dice132.IsSettled132));
        }
    }
}
