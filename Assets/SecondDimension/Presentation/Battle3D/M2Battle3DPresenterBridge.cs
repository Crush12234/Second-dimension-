using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Keeps the proven M2 screen hierarchy and command overlay intact while placing
    /// the perspective battlefield behind it. The legacy field remains as a complete,
    /// named fallback and is only made transparent after the 3D world reports ready.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        private M2Battle3DWorld _battle3DWorld;
        private bool _battle3DActive;
        private Coroutine _battle3DCommandFocus;

        private bool TryActivateCinematic3DBattle(
            M2BattleView battle,
            bool resolving,
            Image legacyFrame,
            RectTransform legacyBattlefield)
        {
            if (battle == null || legacyFrame == null || legacyBattlefield == null) return false;
            try
            {
                if (_battle3DCommandFocus != null)
                {
                    StopCoroutine(_battle3DCommandFocus);
                    _battle3DCommandFocus = null;
                }
                if (_battle3DWorld == null)
                    _battle3DWorld = EnsureBattleComponent<M2Battle3DWorld>(gameObject);
                if (!_battle3DWorld.ShowBattle(battle, resolving, _reducedMotion))
                {
                    _battle3DActive = false;
                    return false;
                }

                _battle3DActive = true;
                _battle3DWorld.HighlightUnion(resolving ? null : _expandedBattleCommandUnionId);
                if (!resolving && !string.IsNullOrWhiteSpace(_expandedBattleCommandUnionId))
                    _battle3DCommandFocus = StartCoroutine(
                        FocusCinematic3DCommandUnion019(_expandedBattleCommandUnionId));

                // Alpha does not deactivate or remove the legacy descendants. Named
                // hierarchy, artwork objects, HUD data, and the 2.5D fallback all remain.
                var legacyGroup = EnsureBattleComponent<CanvasGroup>(legacyBattlefield.gameObject);
                legacyGroup.alpha = 0f;
                legacyGroup.interactable = false;
                legacyGroup.blocksRaycasts = false;
                legacyFrame.color = Color.clear;
                return true;
            }
            catch (Exception exception)
            {
                _battle3DActive = false;
                if (_battle3DWorld != null) _battle3DWorld.SetVisible(false);
                Debug.LogWarning("Battle 3D presentation could not start; the complete 2.5D fallback remains active. " +
                                 exception.Message, this);
                return false;
            }
        }

        private bool CanStageCinematic3DBeat() =>
            _battle3DActive && _battle3DWorld != null && _battle3DWorld.IsReady;

        private IEnumerator StageCinematic3DBeat(BattlePresentationBeat beat)
        {
            var directive = M2Battle3DPresentationPolicy.CreateDirective(beat);
            yield return StageCinematic3DDirective071(directive);
        }

        private IEnumerator StageCinematic3DDirective071(Battle3DPresentationDirective directive)
        {
            if (!CanStageCinematic3DBeat()) yield break;
            yield return _battle3DWorld.PlayDirective(
                directive,
                () => _battleAnimationPaused ? 0f : _battleAnimationSpeed,
                _reducedMotion,
                () => _skipCurrentBattleBeat || _skipBattleAnimation,
                _battleShakeStrength,
                _reducedFlash);
        }

        private IEnumerator FocusCinematic3DUnionTurn019(string unionId)
        {
            if (!CanStageCinematic3DBeat()) yield break;
            yield return _battle3DWorld.FocusUnionTurn(
                unionId,
                () => _battleAnimationPaused ? 0f : _battleAnimationSpeed,
                _reducedMotion,
                () => _skipCurrentBattleBeat || _skipBattleAnimation);
        }

        private IEnumerator FocusCinematic3DCommandUnion019(string unionId)
        {
            if (!CanStageCinematic3DBeat()) yield break;
            yield return _battle3DWorld.FocusUnionTurn(
                unionId,
                () => 1f,
                _reducedMotion,
                () => !_battle3DActive);
            _battle3DCommandFocus = null;
        }

        private IEnumerator ReturnCinematic3DToTacticalOverview019()
        {
            if (!CanStageCinematic3DBeat()) yield break;
            yield return _battle3DWorld.ReturnToTacticalOverview(
                () => _battleAnimationPaused ? 0f : _battleAnimationSpeed,
                _reducedMotion,
                () => _skipCurrentBattleBeat || _skipBattleAnimation);
        }

        private void SetCinematic3DWorldVisible(bool visible)
        {
            if (!visible) _battle3DActive = false;
            if (!visible && _battle3DCommandFocus != null)
            {
                StopCoroutine(_battle3DCommandFocus);
                _battle3DCommandFocus = null;
            }
            if (_battle3DWorld != null) _battle3DWorld.SetVisible(visible);
        }
    }
}
