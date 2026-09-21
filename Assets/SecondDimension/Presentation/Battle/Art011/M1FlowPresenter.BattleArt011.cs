using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private BattleArtProfile011 ResolveBattleArt011(BattlePresentationBeat beat) =>
            BattleArtRuntimeRegistry011.ResolveProfile(beat?.ArtId, beat?.Family ?? BattleBeatFamily.BasicMartial);

        private Color BattleArtColor011(BattleArtProfile011 profile, Color fallback) =>
            BattleArtRuntimeRegistry011.ParseColor(profile?.primaryColor, fallback);


        private bool SetBattleArtPose011(BattleCombatantRuntimeView view, string poseId)
        {
            if (view == null || view.ActionArtwork == null || string.IsNullOrWhiteSpace(poseId)) return false;
            if (!BattleArtRuntimeRegistry011.TryResolvePose(view.MemberId, poseId, out var sprite, out _)) return false;
            view.ActionArtwork.sprite = sprite;
            view.ActionArtwork.preserveAspect = true;
            return true;
        }

        private IEnumerator BlendBattleArtPose011(
            BattleCombatantRuntimeView view,
            string poseId,
            bool visible,
            float duration)
        {
            if (visible) SetBattleArtPose011(view, poseId);
            yield return BlendActionPose(view, visible, duration);
        }

        private void PlayBattleArtWindupAudio011(BattleArtProfile011 profile)
        {
            if (_battleAudio == null || profile == null) return;
            var path = profile.startAudioResourcePath;
            if (string.IsNullOrWhiteSpace(path)) path = profile.windupAudioResourcePath;
            if (string.IsNullOrWhiteSpace(path)) path = profile.audioResourcePath;
            if (!string.IsNullOrWhiteSpace(path)) _battleAudio.PlayResourceCue(path);
        }

        private void PlayBattleArtAudio011(BattleArtProfile011 profile, bool impact = false)
        {
            if (_battleAudio == null || profile == null) return;
            var path = impact ? profile.impactAudioResourcePath : profile.startAudioResourcePath;
            if (string.IsNullOrWhiteSpace(path) && !impact) path = profile.windupAudioResourcePath;
            if (string.IsNullOrWhiteSpace(path)) path = profile.audioResourcePath;
            if (!string.IsNullOrWhiteSpace(path)) _battleAudio.PlayResourceCue(path);
        }

        private static int BattleArtImpactFrames011(BattleArtProfile011 profile, int fallback)
        {
            if (profile == null || profile.hitStopMilliseconds <= 0) return Mathf.Max(1, fallback);
            return Mathf.Clamp(Mathf.RoundToInt(profile.hitStopMilliseconds / (1000f / 60f)), 1, 8);
        }

        private static float BattleArtShakeMagnitude011(BattleArtProfile011 profile, float fallback)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.screenShake)) return fallback;
            switch (profile.screenShake.Trim().ToUpperInvariant())
            {
                case "NONE": return 0f;
                case "LIGHT": return 12f;
                case "MEDIUM": return 20f;
                case "HEAVY": return 30f;
                case "EXTREME": return 38f;
                default: return fallback;
            }
        }

        private IEnumerator PlayBattleArtField011(
            BattleArtProfile011 profile,
            BattleCombatantRuntimeView target,
            float size,
            float duration)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.fieldResourcePath)) yield break;
            yield return PlayBattleArtVfx011(profile.fieldResourcePath, target, size, 0f, duration, Color.white);
        }

        private void ApplyBattleArtUiAsset011(
            Image image,
            string assetId,
            Color tint,
            bool sliced = true)
        {
            if (image == null || string.IsNullOrWhiteSpace(assetId)) return;
            if (!BattleArtRuntimeRegistry011.TryResolveUiAsset(assetId, out _, out var sprite) || sprite == null) return;
            image.sprite = sprite;
            image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            image.preserveAspect = !sliced;
            image.color = tint;
        }

        private static string CommandIconAssetId011(string commandId)
        {
            var value = (commandId ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Contains("BREAKTHROUGH") || value.Contains("SIGNATURE")) return "ICON_ACTION_BREAKTHROUGH";
            if (value.Contains("MYSTIC") || value.Contains("MAGIC") || value.Contains("SORCER")) return "ICON_ACTION_MYSTIC";
            if (value.Contains("HEAL") || value.Contains("RESTOR") || value.Contains("RECOVER")) return "ICON_ACTION_RESTORATION";
            if (value.Contains("GUARD") || value.Contains("WARD") || value.Contains("PROTECT")) return "ICON_ACTION_WARDING";
            if (value.Contains("SUPPORT") || value.Contains("TACTIC") || value.Contains("RESCUE") || value.Contains("CONSERV"))
                return "ICON_ACTION_TACTICAL";
            return "ICON_ACTION_COMBAT";
        }

        private static string RelationshipIconAssetId011(M2BattleView battle)
        {
            if (battle == null) return "ICON_REL_OPEN";
            var states = new System.Collections.Generic.List<string>();
            if (battle.PlayerUnions != null)
                states.AddRange(battle.PlayerUnions.Select(value => value?.Engagement ?? string.Empty));
            if (battle.EnemyUnions != null)
                states.AddRange(battle.EnemyUnions.Select(value => value?.Engagement ?? string.Empty));
            var joined = string.Join("|", states).ToUpperInvariant();
            if (joined.Contains("REAR") || joined.Contains("BLIND")) return "ICON_REL_REAR_ATTACK";
            if (joined.Contains("SIDE") || joined.Contains("FLANK")) return "ICON_REL_SIDE_STRIKE";
            if (joined.Contains("INTERFER")) return "ICON_REL_INTERFERENCE";
            if (joined.Contains("DEADLOCK") || joined.Contains("ENGAGED")) return "ICON_REL_DEADLOCK";
            if (joined.Contains("PROTECT")) return "ICON_REL_PROTECT";
            if (joined.Contains("RESCUE")) return "ICON_REL_RESCUE";
            if (joined.Contains("GUARD") || joined.Contains("INTERCEPT")) return "ICON_REL_GUARD";
            if (joined.Contains("SUPPORT") || joined.Contains("REINFORC")) return "ICON_REL_SUPPORT";
            return "ICON_REL_OPEN";
        }

        private IEnumerator PlayBattleArtVfx011(
            string resourcePath,
            BattleCombatantRuntimeView target,
            float size,
            float rotation,
            float duration,
            Color tint)
        {
            if (_battleVfx == null || string.IsNullOrWhiteSpace(resourcePath)) yield break;
            var sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
            if (sprite == null) yield break;
            var effect = _battleVfx.SpawnPanel("Battle Art 011 VFX " + resourcePath, tint);
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
                var color = tint;
                color.a *= Mathf.Sin(t * Mathf.PI) * (_reducedFlash ? 0.62f : 1f);
                effect.color = color;
                effect.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.60f, 1.18f, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            if (effect != null) _battleVfx.Release(effect, 0f);
        }

        private IEnumerator AnimateBattleArtProjectile011(
            BattleArtProfile011 profile,
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target)
        {
            if (profile == null || string.IsNullOrWhiteSpace(profile.projectileResourcePath))
            {
                yield return AnimateProjectile(actor, target, profile?.glyph ?? "✦", BattleArtColor011(profile, new Color(0.30f, 0.82f, 1f, 1f)));
                yield break;
            }
            var sprite = BattleArtRuntimeRegistry011.LoadSprite(profile.projectileResourcePath);
            if (sprite == null)
            {
                yield return AnimateProjectile(actor, target, profile.glyph ?? "✦", BattleArtColor011(profile, new Color(0.30f, 0.82f, 1f, 1f)));
                yield break;
            }
            var projectile = _battleVfx == null ? null : _battleVfx.SpawnPanel("Battle Art 011 Projectile", Color.white);
            if (projectile == null) yield break;
            projectile.sprite = sprite;
            projectile.preserveAspect = true;
            projectile.rectTransform.anchorMin = projectile.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            projectile.rectTransform.sizeDelta = new Vector2(190f, 190f);
            var from = EffectPoint(actor);
            var to = EffectPoint(target);
            projectile.rectTransform.anchoredPosition = from;
            var elapsed = 0f;
            const float duration = 0.34f;
            while (elapsed < duration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                projectile.rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
                projectile.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.78f, 1.18f, Mathf.Sin(t * Mathf.PI));
                projectile.rectTransform.localRotation = Quaternion.Euler(0f, 0f, t * 180f);
                yield return null;
            }
            if (projectile != null && _battleVfx != null) _battleVfx.Release(projectile, 0f);
        }
    }
}
