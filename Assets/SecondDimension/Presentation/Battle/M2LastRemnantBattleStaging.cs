using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Owner-directed cinematic staging pass. The gameplay layer still submits only
    /// complete Union command IDs and returns deterministic events; this file turns
    /// those events into large readable 2.5D action shots.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        private sealed class BattleActionFocus
        {
            public GameObject Root;
            public BattleCombatantRuntimeView Actor;
            public BattleCombatantRuntimeView Target;
            public CanvasGroup Battlefield;
            public float PriorBattlefieldAlpha;
            public bool EventBannerWasActive;
        }

        private void BuildCinematicCommandTray(Transform root, M2BattleView battle) =>
            BuildFullScreenUnionCommandOverlay(root, battle);

        private void BuildUnionCommandOverlay(Transform root, M2BattleView battle)
        {
            var tray = AddAnchoredPanel(root, "Always Visible Union Command Tray",
                new Color(0.012f, 0.022f, 0.038f, 0.94f),
                new Vector2(0.012f, 0.018f), new Vector2(0.988f, 0.405f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(tray, M1PremiumUi.Surface.Iron);

            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var focus = active.FirstOrDefault(value =>
                            StringComparer.Ordinal.Equals(value.UnionId, _expandedBattleCommandUnionId)) ??
                        active.FirstOrDefault(value => !value.IsSelected) ?? active.FirstOrDefault();
            if (focus == null) return;
            _expandedBattleCommandUnionId = focus.UnionId;

            var denseNavigator = active.Length > 5;
            var tabs = AddAnchoredPanel(tray.transform, "Active Union Command Tabs", Color.clear,
                new Vector2(0.012f, denseNavigator ? 0.670f : 0.825f),
                new Vector2(0.40f, 0.985f), Vector2.zero, Vector2.zero);
            for (var index = 0; index < active.Length; index++)
            {
                var captured = active[index];
                UnionNavigatorCell020(index, active.Length, out var cellMin, out var cellMax);
                var chipYInset = denseNavigator ? 0.005f : 0.02f;
                var tab = AddAnchoredButton(tabs.transform, "Focus Union Command " + captured.UnionId,
                    UnionNavigatorLabel020(captured, index, active.Length), () =>
                    {
                        _expandedBattleCommandUnionId = captured.UnionId;
                        BuildCurrentScreen();
                    }, StringComparer.Ordinal.Equals(captured.UnionId, focus.UnionId)
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal,
                    new Vector2(cellMin.x + 0.008f, cellMin.y + chipYInset),
                    new Vector2(cellMax.x - 0.008f, cellMax.y - chipYInset));
                StyleUnionNavigatorChip020(tab, denseNavigator, 22);
            }

            AddAnchoredText(tray.transform, "Last Remnant Union Order Prompt",
                "CHOOSE AN ORDER FOR " + focus.DisplayName.ToUpperInvariant(), 31,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.415f, 0.86f), new Vector2(0.73f, 0.985f));
            AddAnchoredText(tray.transform, "Focused Union Resource Decision",
                "AP " + focus.CurrentAp + "/" + focus.MaximumAp + "   ◆   COH " + focus.Cohesion +
                "   ◆   FORM " + focus.FormationConditionPercent + "%   ◆   " + focus.Engagement.ToUpperInvariant(),
                23, TextAnchor.MiddleRight, BattleAp, FontStyle.Bold,
                new Vector2(0.70f, 0.84f), new Vector2(0.985f, 0.985f));

            var forecasts = battle.Forecasts
                .Where(value => StringComparer.Ordinal.Equals(value.UnionId, focus.UnionId))
                .ToArray();
            var commands = AddAnchoredPanel(tray.transform, "Immediate Complete Union Commands",
                new Color(0.006f, 0.012f, 0.022f, 0.74f),
                new Vector2(0.012f, 0.035f), new Vector2(0.415f, denseNavigator ? 0.655f : 0.815f),
                Vector2.zero, Vector2.zero);
            var count = Math.Max(1, forecasts.Length);
            for (var index = 0; index < forecasts.Length; index++)
            {
                var forecast = forecasts[index];
                var top = 1f - (float)index / count;
                var bottom = 1f - (float)(index + 1) / count;
                var button = AddAnchoredButton(commands.transform,
                    "Complete Union Command " + forecast.ForecastId,
                    LastRemnantCommandLabel(forecast),
                    () => SelectCompleteForecast(forecast.UnionId, forecast.ForecastId),
                    forecast.IsSelected ? RuntimeUi.Accent : CommandFamilyColor(forecast.CommandId),
                    new Vector2(0.01f, bottom + 0.007f), new Vector2(0.99f, top - 0.007f));
                SetButtonFont(button, forecasts.Length > 5 ? 18 : 21);
                button.interactable = !_battleResolving;
            }

            var selected = forecasts.FirstOrDefault(value => value.IsSelected);
            var detail = AddAnchoredPanel(tray.transform, "Selected Complete Command Detail",
                new Color(0.018f, 0.047f, 0.070f, 0.91f),
                new Vector2(0.425f, 0.035f), new Vector2(0.815f, 0.815f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(detail,
                selected == null ? M1PremiumUi.Surface.EtchedGlass : M1PremiumUi.Surface.Warning);
            AddAnchoredText(detail.transform, "Predicted Arts Interaction Law",
                "PREDICTED MEMBER ARTS", 15,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.025f, 0.90f), new Vector2(0.975f, 0.995f));
            if (selected == null)
            {
                AddAnchoredText(detail.transform, "Choose Command Prompt",
                    "SELECT ONE COMPLETE UNION ORDER\n\nEach member's Art is predicted automatically.", 28,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold,
                    new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.88f));
            }
            else
            {
                AddAnchoredText(detail.transform, "Selected Command Summary",
                    PlayerCommandLabel(selected) + "  →  " + selected.TargetName.ToUpperInvariant() +
                    "\nSHARED AP " + selected.SharedApCost + "   ◆   " + selected.ExpectedEffect,
                    22, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                    new Vector2(0.025f, 0.72f), new Vector2(0.975f, 0.895f));
                var actionCount = Math.Max(1, selected.MemberActions.Count);
                for (var actionIndex = 0; actionIndex < selected.MemberActions.Count; actionIndex++)
                {
                    var action = selected.MemberActions[actionIndex];
                    var top = 0.70f - 0.43f * actionIndex / actionCount;
                    var bottom = 0.70f - 0.43f * (actionIndex + 1) / actionCount;
                    AddAnchoredText(detail.transform, "Predicted Arts Non Clickable " + action.ActorMemberId,
                        CompactPredictedAction(action), 19, TextAnchor.MiddleLeft,
                        action.BreakthroughOpportunity ? RuntimeUi.Warning : RuntimeUi.Text,
                        action.BreakthroughOpportunity ? FontStyle.Bold : FontStyle.Normal,
                        new Vector2(0.025f, bottom), new Vector2(0.975f, top));
                }
                AddAnchoredText(detail.transform, "Selected Command Risk Learning",
                    "LEARN · " + selected.LearningOpportunity + "\nRISK · " + selected.Risk +
                    "\nFALLBACK · " + selected.FallbackBehavior,
                    16, TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Normal,
                    new Vector2(0.025f, 0.015f), new Vector2(0.975f, 0.24f));
            }

            var confirm = AddAnchoredButton(tray.transform, "Confirm Complete Forecast Round",
                battle.CanConfirmRound ? "CONFIRM ROUND" : "ORDER EVERY UNION",
                BeginResolveRound, battle.CanConfirmRound ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                new Vector2(0.83f, 0.035f), new Vector2(0.985f, 0.53f));
            SetButtonFont(confirm, 31);
            confirm.interactable = battle.CanConfirmRound && _coordinator is IM2PresentationCoordinator;
            AddInvocationForecastControls022(tray.transform,battle,new Vector2(0.815f,0.55f),new Vector2(0.985f,0.985f),17);
        }

        private static string LastRemnantCommandLabel(M2ForecastView forecast)
        {
            var name = PlayerCommandLabel(forecast);
            var target = forecast.TargetName ?? "OBJECTIVE";
            if (target.Length > 28) target = target.Substring(0, 27).TrimEnd() + "…";
            return name + "\n◆  AP " + forecast.SharedApCost + "   →   " + target.ToUpperInvariant();
        }

        private BattleActionFocus BeginActionFocus(
            BattlePresentationBeat beat,
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target)
        {
            // The perspective presenter owns the actor, target, VFX, and camera whenever
            // it can stage this beat. Keeping this guard here prevents a future caller
            // from accidentally layering the legacy portrait clones over the 3D action.
            // If the 3D world is unavailable, the complete legacy path remains intact.
            if (CanStageCinematic3DBeat()) return null;
            if (_battleEffectRoot == null || !UsesActionCloseup(beat.Family) || (actor == null && target == null))
                return null;

            var focusRoot = new GameObject("Cinematic Action Closeup", typeof(RectTransform));
            var rootRect = focusRoot.GetComponent<RectTransform>();
            rootRect.SetParent(_battleEffectRoot, false);
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var shade = AddAnchoredPanel(rootRect, "Cinematic Action Depth Shade",
                new Color(0.005f, 0.008f, 0.018f,
                    _battle3DActive ? 0.18f : _highContrast ? 0.70f : 0.46f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            shade.raycastTarget = false;

            var battlefield = _battlefieldCameraRoot == null
                ? null
                : EnsureBattleComponent<CanvasGroup>(_battlefieldCameraRoot.gameObject);
            var priorAlpha = battlefield == null ? 1f : battlefield.alpha;
            // The perspective battlefield stays visible during 3D cut-ins. Only the
            // cloned actor/target portraits and light depth shade are layered over it;
            // the old painted arena remains at alpha zero.
            if (battlefield != null && !_battle3DActive) battlefield.alpha = 0.48f;

            var sameSide = actor != null && target != null && actor.Enemy == target.Enemy;
            BattleCombatantRuntimeView actorClose = null;
            BattleCombatantRuntimeView targetClose = null;
            if (actor != null)
            {
                var actorLeft = sameSide ? !actor.Enemy : !actor.Enemy;
                actorClose = CloneForAction(rootRect, actor, "Cinematic Acting Member Closeup",
                    actorLeft ? new Vector2(0.015f, 0.035f) : new Vector2(0.61f, 0.035f),
                    actorLeft ? new Vector2(0.405f, 0.94f) : new Vector2(0.995f, 0.94f));
            }
            if (target != null && !StringComparer.Ordinal.Equals(actor?.MemberId, target.MemberId))
            {
                var targetLeft = sameSide ? actor == null || actor.Enemy : !target.Enemy;
                targetClose = CloneForAction(rootRect, target, "Cinematic Target Member Closeup",
                    targetLeft ? new Vector2(0.015f, 0.035f) : new Vector2(0.61f, 0.035f),
                    targetLeft ? new Vector2(0.405f, 0.94f) : new Vector2(0.995f, 0.94f));
            }

            var title = BeatTitle(beat);
            var eventBannerWasActive = _battleEventBanner != null && _battleEventBanner.gameObject.activeSelf;
            if (_battleEventBanner != null) _battleEventBanner.gameObject.SetActive(false);
            AddAnchoredText(rootRect, "Cinematic Art Name Callout", title, 39,
                TextAnchor.MiddleCenter, BeatColor(beat), FontStyle.Bold,
                new Vector2(0.30f, 0.76f), new Vector2(0.70f, 0.94f));
            AddAnchoredText(rootRect, "Cinematic Acting Member Name",
                actor?.DisplayName ?? string.Empty, 25, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.025f, 0.01f), new Vector2(0.40f, 0.10f));
            AddAnchoredText(rootRect, "Cinematic Target Member Name",
                target?.DisplayName ?? string.Empty, 25, TextAnchor.MiddleRight, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.60f, 0.01f), new Vector2(0.975f, 0.10f));

            return new BattleActionFocus
            {
                Root = focusRoot,
                Actor = actorClose ?? actor,
                Target = targetClose ?? target,
                Battlefield = battlefield,
                PriorBattlefieldAlpha = priorAlpha,
                EventBannerWasActive = eventBannerWasActive
            };
        }

        private BattleCombatantRuntimeView CloneForAction(
            Transform parent,
            BattleCombatantRuntimeView source,
            string objectName,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var root = AddAnchoredPanel(parent, objectName, Color.clear,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var artwork = RuntimeUi.AddPanel(root.transform, objectName + " Artwork", source.Artwork.color);
            Stretch(artwork.rectTransform);
            artwork.sprite = source.Artwork.sprite;
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            Image actionArtwork = null;
            Sprite actionSprite = null;
            string actionResourcePath = string.Empty;
            EnemyArt700SpriteLease090 actionEnemyArtLease090 = null;
            var hasActionPose = source.Enemy &&
                                !string.IsNullOrWhiteSpace(source.EnemyArtBaseId090) &&
                                !string.IsNullOrWhiteSpace(source.EnemyArtVariantId090) &&
                                EnemyArt700Runtime090.TryAcquireSprite090(
                                    source.EnemyArtBaseId090,
                                    source.EnemyArtVariantId090,
                                    EnemyArt700Pose090.Attack,
                                    out actionSprite,
                                    out actionEnemyArtLease090,
                                    out actionResourcePath);
            if (actionEnemyArtLease090 != null)
                _battleTransientEnemyArtLeases090.Add(actionEnemyArtLease090);
            if (!hasActionPose)
                hasActionPose = BattleArtRuntimeRegistry011.TryResolvePose(
                    source.MemberId, "POSE_ACTION_PRIMARY", out actionSprite, out actionResourcePath);
            if (!hasActionPose)
                hasActionPose = M1VisualAssets.TryResolveBattleActionPose(
                    source.MemberId,
                    source.VisualSeed,
                    source.RaceId,
                    source.PortraitAuthorityId,
                    out actionSprite,
                    out actionResourcePath);
            if (hasActionPose)
            {
                actionArtwork = RuntimeUi.AddPanel(root.transform, "Cinematic Authored Action Pose " + source.MemberId,
                    new Color(1f, 1f, 1f, 0f));
                Stretch(actionArtwork.rectTransform);
                actionArtwork.sprite = actionSprite;
                actionArtwork.preserveAspect = true;
                actionArtwork.raycastTarget = false;
            }
            var canvas = EnsureBattleComponent<CanvasGroup>(root.gameObject);
            canvas.alpha = 0f;
            root.rectTransform.localScale = new Vector3(0.78f, 0.78f, 1f);
            return new BattleCombatantRuntimeView
            {
                MemberId = source.MemberId,
                DisplayName = source.DisplayName,
                VisualSeed = source.VisualSeed,
                RaceId = source.RaceId,
                PortraitAuthorityId = source.PortraitAuthorityId,
                EnemyArtBaseId090 = source.EnemyArtBaseId090,
                EnemyArtVariantId090 = source.EnemyArtVariantId090,
                Enemy = source.Enemy,
                Root = root.rectTransform,
                Artwork = artwork,
                ActionArtwork = actionArtwork,
                EnemyArtActionLease090 = actionEnemyArtLease090,
                Canvas = canvas,
                Home = root.rectTransform.anchoredPosition,
                HomeRotation = Quaternion.identity
            };
        }

        private IEnumerator RevealActionFocus(BattleActionFocus focus)
        {
            if (focus == null) yield break;
            var elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                RevealCloseup(focus.Actor, t);
                RevealCloseup(focus.Target, t);
                yield return null;
            }
            RevealCloseup(focus.Actor, 1f);
            RevealCloseup(focus.Target, 1f);
        }

        private static void RevealCloseup(BattleCombatantRuntimeView view, float t)
        {
            if (view == null || view.Canvas == null || view.Root == null) return;
            view.Canvas.alpha = t;
            view.Root.localScale = Vector3.Lerp(new Vector3(0.78f, 0.78f, 1f), Vector3.one, t);
        }

        private void EndActionFocus(BattleActionFocus focus)
        {
            if (focus == null) return;
            if (focus.Root != null) focus.Root.SetActive(false);
            ReleaseTransientEnemyArtLease090(focus.Actor);
            if (!ReferenceEquals(focus.Actor, focus.Target))
                ReleaseTransientEnemyArtLease090(focus.Target);
            if (focus.Battlefield != null) focus.Battlefield.alpha = focus.PriorBattlefieldAlpha;
            if (_battleEventBanner != null) _battleEventBanner.gameObject.SetActive(focus.EventBannerWasActive);
            if (focus.Root != null) Destroy(focus.Root);
        }

        private void ReleaseTransientEnemyArtLease090(
            BattleCombatantRuntimeView view090)
        {
            if (view090?.EnemyArtActionLease090 == null) return;
            var lease090 = view090.EnemyArtActionLease090;
            view090.EnemyArtActionLease090 = null;
            _battleTransientEnemyArtLeases090.Remove(lease090);
            lease090.Dispose();
        }

        private static bool UsesActionCloseup(BattleBeatFamily family)
        {
            switch (family)
            {
                case BattleBeatFamily.BasicMartial:
                case BattleBeatFamily.CombatArt:
                case BattleBeatFamily.Tactical:
                case BattleBeatFamily.Mystic:
                case BattleBeatFamily.Invocation:
                case BattleBeatFamily.Restoration:
                case BattleBeatFamily.Guard:
                case BattleBeatFamily.Interception:
                case BattleBeatFamily.Downed:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsActionCloseup(BattleCombatantRuntimeView view) =>
            view != null && view.Root != null &&
            view.Root.name.IndexOf("Closeup", StringComparison.Ordinal) >= 0;

        private IEnumerator AnimatePresentedHpChange(BattlePresentationBeat beat)
        {
            if (beat == null || string.IsNullOrWhiteSpace(beat.TargetMemberId) ||
                !_battleCombatants.TryGetValue(beat.TargetMemberId, out var view) ||
                view.MaximumHp <= 0 || view.HpFill == null || view.HpLabel == null)
            {
                yield break;
            }

            var start = view.CurrentHp;
            var end = start;
            switch (beat.Family)
            {
                case BattleBeatFamily.BasicMartial:
                case BattleBeatFamily.CombatArt:
                case BattleBeatFamily.Tactical:
                case BattleBeatFamily.Mystic:
                    end = Mathf.Max(0, start - Math.Abs(beat.Amount));
                    break;
                case BattleBeatFamily.Restoration:
                    end = Mathf.Min(view.MaximumHp, start + Math.Abs(beat.Amount));
                    break;
                case BattleBeatFamily.Downed:
                    end = 0;
                    break;
                default:
                    yield break;
            }
            if (end == start) yield break;

            var elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var shown = Mathf.RoundToInt(Mathf.Lerp(start, end, Mathf.Clamp01(elapsed / duration)));
                SetPresentedHp(view, shown);
                yield return null;
            }
            SetPresentedHp(view, end);
        }

        private static void SetPresentedHp(BattleCombatantRuntimeView view, int hp)
        {
            if (view == null) return;
            view.CurrentHp = Mathf.Clamp(hp, 0, Math.Max(0, view.MaximumHp));
            if (view.HpFill != null)
            {
                var max = view.HpFill.rectTransform.anchorMax;
                max.x = view.MaximumHp <= 0 ? 0f : Mathf.Clamp01((float)view.CurrentHp / view.MaximumHp);
                view.HpFill.rectTransform.anchorMax = max;
            }
            if (view.HpLabel != null)
                view.HpLabel.text = "HP " + view.CurrentHp + "/" + view.MaximumHp;
        }

        private IEnumerator PlayAuthoredBattleVfx(
            string effectId,
            BattleCombatantRuntimeView target,
            float size,
            float rotation,
            float duration)
        {
            if (_battleVfx == null ||
                !M1VisualAssets.TryResolveBattleVfx(effectId, out var sprite, out _))
            {
                yield break;
            }

            var effect = _battleVfx.SpawnPanel("Authored Battle VFX " + effectId, Color.white);
            if (effect == null) yield break;
            effect.sprite = sprite;
            effect.preserveAspect = true;
            effect.rectTransform.anchorMin = effect.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            effect.rectTransform.sizeDelta = new Vector2(size, size);
            effect.rectTransform.anchoredPosition = EffectPoint(target);
            effect.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            var elapsed = 0f;
            while (elapsed < duration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = Mathf.Sin(t * Mathf.PI) * (_reducedFlash ? 0.58f : 1f);
                effect.color = new Color(1f, 1f, 1f, alpha);
                effect.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.58f, 1.16f, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            if (effect != null) _battleVfx.Release(effect, 0f);
        }
    }
}
