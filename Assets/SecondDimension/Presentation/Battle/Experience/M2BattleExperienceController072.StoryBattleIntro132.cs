using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M2BattleExperienceController072
    {
        CommittedGlassShatter132 _storyBattleIntro132;
        public bool StoryBattleIntroHeld132 => _storyBattleIntro132 != null &&
            _storyBattleIntro132.IsPending132 && _storyBattleIntro132.isActiveAndEnabled;

        public bool BeginStoryBattleIntro132(string committedRequestId, string title)
        {
            // EnterCurrentBattle has already rendered this exact authoritative view.
            // Never create an arena, opponent, result or command inside the effect.
            if (!IsActive || !isActiveAndEnabled || _resolving || _claiming || StoryBattleIntroHeld132 ||
                _pendingView == null || _pendingView.IsResolved ||
                string.IsNullOrWhiteSpace(committedRequestId)) return false;
            CancelStoryBattleIntro132();
            _hud?.SetInteractable(false);
            if (_autoOrdersButton091 != null) _autoOrdersButton091.interactable = false;
            var effect = CommittedGlassShatter132.PlayBattleIntro132(_root,
                committedRequestId, title, ReducedMotion, ReleaseStoryBattleIntro132);
            // Reduced motion completes synchronously before the returned object
            // can become the owned effect. Both paths dispose their own meshes.
            if (effect.IsPending132) _storyBattleIntro132 = effect;
            else Destroy(effect.gameObject);
            return true;
        }

        void ReleaseStoryBattleIntro132()
        {
            var effect = _storyBattleIntro132;
            _storyBattleIntro132 = null;
            if (effect != null) Destroy(effect.gameObject);
            _hud?.SetInteractable(!_resolving && !_claiming);
            if (_autoOrdersButton091 != null) _autoOrdersButton091.interactable = true;
            ScheduleNextAutoOrder108();
        }

        void CancelStoryBattleIntro132()
        {
            var effect = _storyBattleIntro132;
            _storyBattleIntro132 = null;
            if (effect != null)
            {
                effect.Cancel132();
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
            if (_autoOrdersButton091 != null) _autoOrdersButton091.interactable = true;
        }
    }
}
