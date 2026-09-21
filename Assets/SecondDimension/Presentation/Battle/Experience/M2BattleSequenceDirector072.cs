using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Plays an already-authoritative M2 round through the persistent first-hour
    /// diorama. This component never selects Arts, changes costs, calculates damage,
    /// or writes campaign state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2BattleSequenceDirector072 : MonoBehaviour
    {
        private const string BreakthroughCue =
            "SecondDimension/Audio/Battle011/UI/SFX_BREAKTHROUGH";
        private const string DownedCue =
            "SecondDimension/Audio/Battle011/UI/SFX_DOWNED";
        private const string VictoryCue =
            "SecondDimension/Audio/Battle011/UI/SFX_VICTORY";

        private M2BattleDioramaView072 _diorama;
        private M2BattleAudioDirector _audio;
        private Action<string, string> _setCaption;
        private int _exactRecipeStartSfxCount076;
        private int _exactRecipeImpactSfxCount076;
        private string _lastExactRecipeSfxSignature076 = string.Empty;
        private string _lastExactRecipeStartCue076 = string.Empty;
        private string _lastExactRecipeImpactCue076 = string.Empty;
        private readonly HashSet<string> _exactRecipeStartSfxSignatures076 =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _exactRecipeImpactSfxSignatures076 =
            new HashSet<string>(StringComparer.Ordinal);

        public bool IsPlaying { get; private set; }
        public int LastPresentedBeatCount { get; private set; }
        public int ExactRecipeStartSfxCount076 => _exactRecipeStartSfxCount076;
        public int ExactRecipeImpactSfxCount076 => _exactRecipeImpactSfxCount076;
        public string LastExactRecipeSfxSignature076 => _lastExactRecipeSfxSignature076;
        public string LastExactRecipeStartCue076 => _lastExactRecipeStartCue076;
        public string LastExactRecipeImpactCue076 => _lastExactRecipeImpactCue076;
        public bool HasConsumedExactRecipeSfx076 =>
            _exactRecipeStartSfxSignatures076.Overlaps(
                _exactRecipeImpactSfxSignatures076);

        public bool HasConsumedExactRecipeSfxSignature076(string signature076)
        {
            return !string.IsNullOrWhiteSpace(signature076) &&
                   _exactRecipeStartSfxSignatures076.Contains(signature076) &&
                   _exactRecipeImpactSfxSignatures076.Contains(signature076);
        }

        public void Initialize(
            M2BattleDioramaView072 diorama,
            M2BattleAudioDirector audio,
            Action<string, string> setCaption)
        {
            _diorama = diorama ?? throw new ArgumentNullException(nameof(diorama));
            _audio = audio;
            _setCaption = setCaption;
        }

        public IEnumerator PlayRound(
            M2BattleView before,
            M2BattleView after,
            IReadOnlyList<M2BattleEventView> events,
            bool reducedMotion,
            Func<float> speed,
            Func<bool> skip,
            Action<string> committedUnion = null)
        {
            if (_diorama == null) yield break;
            if (IsPlaying) yield break;

            IsPlaying = true;
            LastPresentedBeatCount = 0;
            try
            {
                _diorama.PrepareResolvedRound(after);
                var sourceEvents = events ?? Array.Empty<M2BattleEventView>();
                var beats = BattlePresentationPlanner.Plan(sourceEvents);
                var turns = BattleUnionTurnPlanner019.Plan(before ?? after, sourceEvents);
                var turnsByEventIndex = turns
                    .Where(value => value != null)
                    .GroupBy(value => value.FirstEventIndex)
                    .ToDictionary(value => value.Key, value => value.First());
                var stagedActionSignatures108 = new HashSet<string>(StringComparer.Ordinal);
                var stagedActionCounts108 = new Dictionary<string, int>(StringComparer.Ordinal);
                var stagedPositioningUnions108 = new HashSet<string>(StringComparer.Ordinal);

                for (var index = 0; index < beats.Count; index++)
                {
                    if (SkipRequested(skip)) break;
                    if (turnsByEventIndex.TryGetValue(index, out var turn))
                    {
                        committedUnion?.Invoke(turn.UnionId);
                        _setCaption?.Invoke(
                            turn.TurnBadge,
                            string.IsNullOrWhiteSpace(turn.DisplayName)
                                ? "A Union commits its order."
                                : turn.DisplayName + " commits its order.");
                        _diorama.Focus(turn.UnionId, string.Empty);
                        yield return Wait(reducedMotion ? 0.08f : 0.20f, speed, skip);
                    }

                    var beat = beats[index];
                    var sourceEvent = index < sourceEvents.Count ? sourceEvents[index] : null;
                    if (beat == null || !beat.VisuallyStaged) continue;
                    var playbackSpeed108 = CurrentSpeed108(speed);
                    // At high Auto speeds, mastery remains authoritative and visible
                    // in the saved hero state without serializing one extra animation
                    // after every action. Breakthroughs retain their authored beat.
                    if (playbackSpeed108 >= 4f && beat.Family == BattleBeatFamily.Learning)
                        continue;
                    if (playbackSpeed108 >= 16f && beat.Family == BattleBeatFamily.CommandCommit)
                        continue;
                    if (playbackSpeed108 >= 4f && beat.Family == BattleBeatFamily.Positioning)
                    {
                        var positioningKey108 = beat.ActorUnionId ?? string.Empty;
                        if (playbackSpeed108 >= 16f ||
                            !stagedPositioningUnions108.Add(positioningKey108)) continue;
                    }
                    if (playbackSpeed108 >= 16f && beat.Family == BattleBeatFamily.Downed)
                    {
                        _diorama.PresentHpEvent076(sourceEvent);
                        continue;
                    }
                    // At 16x the exact committed action remains in the decision
                    // feed while its resolved HP/state is reconciled immediately.
                    // This prevents one routine cast from consuming several render
                    // frames in large ten-Union Tower rounds.
                    if (playbackSpeed108 >= 16f && IsActionBeat108(beat.Family))
                    {
                        _diorama.PresentHpEvent076(sourceEvent);
                        continue;
                    }
                    // One Union Art may resolve across many recipients. Present the
                    // first impact and reconcile later recipients immediately instead
                    // of replaying the same cast for every target.
                    if (playbackSpeed108 >= 4f && IsActionBeat108(beat.Family))
                    {
                        var signature108 = string.Join("|", beat.Round.ToString(),
                            beat.ActorUnionId, beat.ActorMemberId, beat.ArtId,
                            beat.Family.ToString());
                        if (!stagedActionSignatures108.Add(signature108))
                        {
                            _diorama.PresentHpEvent076(sourceEvent);
                            continue;
                        }
                        var actionKey108 = beat.ActorUnionId ?? string.Empty;
                        stagedActionCounts108.TryGetValue(actionKey108, out var actionCount108);
                        const int actionLimit108 = 1;
                        if (actionCount108 >= actionLimit108)
                        {
                            _diorama.PresentHpEvent076(sourceEvent);
                            continue;
                        }
                        stagedActionCounts108[actionKey108] = actionCount108 + 1;
                    }

                    if (beat.Family == BattleBeatFamily.Learning ||
                        beat.Family == BattleBeatFamily.Breakthrough)
                    {
                        var breakthrough = beat.Family == BattleBeatFamily.Breakthrough;
                        _setCaption?.Invoke(
                            breakthrough ? "NEW ART LEARNED" : "ART MASTERY",
                            breakthrough
                                ? BreakthroughCaptionForVerification076(sourceEvent)
                                : SafeCaption(beat.Caption, "Practice deepened an Art."));
                        if (breakthrough)
                            _diorama.PresentBreakthroughEvent076(sourceEvent);
                        if (breakthrough && _audio != null) _audio.PlayResourceCue(BreakthroughCue);
                        yield return _diorama.PlayBeat(beat, reducedMotion, speed, skip);
                        yield return Wait(reducedMotion ? 0.08f : breakthrough ? 0.28f : 0.12f, speed, skip);
                        LastPresentedBeatCount++;
                        continue;
                    }

                    if (beat.Family == BattleBeatFamily.Result)
                    {
                        _setCaption?.Invoke(
                            after != null && string.Equals(after.Outcome, "Victory", StringComparison.OrdinalIgnoreCase)
                                ? "VICTORY"
                                : "BATTLE COMPLETE",
                            HumanOutcome(after));
                        if (_audio != null && after != null &&
                            string.Equals(after.Outcome, "Victory", StringComparison.OrdinalIgnoreCase))
                            _audio.PlayResourceCue(VictoryCue);
                        yield return _diorama.PlayBeat(beat, reducedMotion, speed, skip);
                        yield return Wait(reducedMotion ? 0.08f : 0.22f, speed, skip);
                        LastPresentedBeatCount++;
                        continue;
                    }

                    var profile = BattleArtRuntimeRegistry011.ResolveProfile(beat.ArtId, beat.Family);
                    var artLevel089 = M2ArtLevelPresentation089.ResolveCommittedLevel(
                        before ?? after,
                        beat.ActorMemberId,
                        beat.ArtId);
                    _setCaption?.Invoke(
                        ActionTitle(profile, beat) +
                        (UsesArtAudio(beat.Family)
                            ? M2ArtLevelPresentation089.CaptionSuffix(artLevel089)
                            : string.Empty),
                        ActionSubtitle(profile, beat));
                    if (UsesArtAudio(beat.Family)) PlayStartAudio(profile, beat.ArtId);
                    else if (beat.Family == BattleBeatFamily.Downed && _audio != null)
                        _audio.PlayResourceCue(DownedCue);
                    var contactObserved076 = false;
                    yield return _diorama.PlayBeat(
                        beat,
                        reducedMotion,
                        speed,
                        skip,
                        () =>
                        {
                            if (contactObserved076) return;
                            contactObserved076 = true;
                            PlayImpactAudio(profile, beat.ArtId);
                            _diorama.PresentHpEvent076(sourceEvent);
                        },
                        artLevel089);
                    // A missing actor, reduced-motion skip, or recovery-path pose can
                    // end visual choreography before its contact callback. Authority
                    // has still resolved the event, so reconcile presentation once at
                    // the end of that beat instead of leaving HP stale until Refresh.
                    if (!contactObserved076)
                        _diorama.PresentHpEvent076(sourceEvent);
                    LastPresentedBeatCount++;
                }

                // Learned-Art notices remain on screen and advance through their
                // bounded queue, but 4x/16x Auto does not block the next round on
                // the fixed three-second callout timer.
                if (CurrentSpeed108(speed) < 4f)
                    yield return _diorama.WaitForBreakthroughNotifications076(skip);
                _diorama.ReturnToOverview();
            }
            finally
            {
                IsPlaying = false;
            }
        }

        private void PlayStartAudio(BattleArtProfile011 profile, string artId)
        {
            if (_audio == null || profile == null) return;
            var exactCue076 = ExactRecipeAudioCueId076(profile, artId, false);
            var path = FirstNonEmpty(
                profile.startAudioResourcePath,
                profile.windupAudioResourcePath,
                profile.audioResourcePath);
            if (PlayExactPhaseAudio076(path, exactCue076, out var selectedCue076) &&
                !string.IsNullOrWhiteSpace(exactCue076))
            {
                _exactRecipeStartSfxCount076++;
                _lastExactRecipeSfxSignature076 = profile.presentationSfxSignature;
                _lastExactRecipeStartCue076 = selectedCue076;
                _exactRecipeStartSfxSignatures076.Add(profile.presentationSfxSignature);
            }
        }

        private void PlayImpactAudio(BattleArtProfile011 profile, string artId)
        {
            if (_audio == null || profile == null) return;
            var exactCue076 = ExactRecipeAudioCueId076(profile, artId, true);
            var path = FirstNonEmpty(profile.impactAudioResourcePath, profile.audioResourcePath);
            if (PlayExactPhaseAudio076(path, exactCue076, out var selectedCue076) &&
                !string.IsNullOrWhiteSpace(exactCue076))
            {
                _exactRecipeImpactSfxCount076++;
                _lastExactRecipeSfxSignature076 = profile.presentationSfxSignature;
                _lastExactRecipeImpactCue076 = selectedCue076;
                _exactRecipeImpactSfxSignatures076.Add(profile.presentationSfxSignature);
            }
        }

        private bool PlayExactPhaseAudio076(
            string resourcePath076,
            string fallbackCue076,
            out string selectedCue076)
        {
            selectedCue076 = string.Empty;
            if (_audio == null) return false;
            if (_audio.TryPlayResourceCue(resourcePath076))
            {
                selectedCue076 = "RESOURCE:" + resourcePath076;
                return true;
            }
            if (!_audio.TryPlayCue(fallbackCue076)) return false;
            selectedCue076 = fallbackCue076;
            return true;
        }

        /// <summary>
        /// Maps an exact authored SFX signature to the deterministic local fallback
        /// cue bank. A valid Resources cue is preferred; synthesis occurs only when
        /// that authored one-shot is unavailable.
        /// </summary>
        public static string ExactRecipeAudioCueId076(
            BattleArtProfile011 profile,
            string artId,
            bool impact)
        {
            if (!M2BattleDioramaView072.TryResolveLiveExactRecipe076(
                    profile,
                    artId,
                    out var choreography076) ||
                choreography076 == null ||
                string.IsNullOrWhiteSpace(profile.presentationSfxSignature))
                return string.Empty;
            return (impact ? "FH071_EXACT_IMPACT|" : "FH071_EXACT_START|") +
                   profile.presentationSfxSignature;
        }

        private static string ActionTitle(BattleArtProfile011 profile, BattlePresentationBeat beat)
        {
            if (beat.Family == BattleBeatFamily.Recovery)
                return StringComparer.OrdinalIgnoreCase.Equals(beat.EventType, "AP_RECOVERY")
                    ? "AP RECOVERY"
                    : StringComparer.Ordinal.Equals(beat.ArtId, "ART_RECOVER_BREATH")
                        ? "RECOVER BREATH" : "RECOVERY";
            if (UsesArtAudio(beat.Family) && !string.IsNullOrWhiteSpace(beat.ArtId) && profile != null &&
                !string.IsNullOrWhiteSpace(profile.displayName))
                return M2BattleReadableText021.ArtDisplayName(
                    beat.ArtId,
                    beat.Caption,
                    false,
                    profile.displayName).ToUpperInvariant();
            switch (beat.Family)
            {
                case BattleBeatFamily.Establishing: return "BATTLE JOINED";
                case BattleBeatFamily.CommandCommit: return "ORDER COMMITTED";
                case BattleBeatFamily.BasicMartial: return "BASIC ATTACK";
                case BattleBeatFamily.Mystic: return "MYSTIC ART";
                case BattleBeatFamily.Restoration: return "RESTORATION";
                case BattleBeatFamily.Guard: return "HOLD THE LINE";
                case BattleBeatFamily.Interception: return "INTERCEPT";
                case BattleBeatFamily.Formation: return "REFORM";
                case BattleBeatFamily.Positioning: return "REPOSITION";
                case BattleBeatFamily.Tactical: return "TACTICAL ART";
                case BattleBeatFamily.Downed: return "DOWNED";
                case BattleBeatFamily.Retreat: return "WITHDRAW";
                default: return "UNION ACTION";
            }
        }

        private static string ActionSubtitle(BattleArtProfile011 profile, BattlePresentationBeat beat)
        {
            if (beat.Family == BattleBeatFamily.Recovery)
                return SafeCaption(beat.Caption, "The Union conserves strength.");
            if (profile != null && profile.exactFirstHourRecipe &&
                !string.IsNullOrWhiteSpace(profile.presentationTreeId) &&
                profile.presentationTreeId.IndexOf("_ROLE_", StringComparison.Ordinal) >= 0)
                return FirstHourRoleSubtitle076(profile.presentationTreeId);
            if (UsesArtAudio(beat.Family) && !string.IsNullOrWhiteSpace(beat.ArtId) && profile != null &&
                !string.IsNullOrWhiteSpace(profile.artClass))
            {
                switch (profile.artClass.Trim().ToUpperInvariant())
                {
                    case "MYSTIC_ATTACK": return "Mystic force gathers and releases.";
                    case "RESTORATION_ART": return "Restoration reaches a wounded ally.";
                    case "WARDING_ART": return "The Union braces behind a ward.";
                    case "COMBAT_ART": return "The Union presses the attack.";
                }
            }
            return SafeCaption(beat.Caption, "The committed Union order resolves.");
        }

        private static string FirstHourRoleSubtitle076(string treeId)
        {
            var value = (treeId ?? string.Empty).ToUpperInvariant();
            if (value.Contains("BREAKER")) return "The Union tears open the enemy line.";
            if (value.Contains("COMMANDER")) return "A clear signal sharpens the Union's timing.";
            if (value.Contains("DUELIST")) return "Measured footwork creates a precise opening.";
            if (value.Contains("FIELD_MEDIC")) return "Field care steadies a wounded companion.";
            if (value.Contains("FORMATION")) return "The Union reforms around a stronger shape.";
            if (value.Contains("GUARDIAN")) return "A guardian steps into the threatened lane.";
            if (value.Contains("PROVOCATION")) return "The enemy's attention is pulled off balance.";
            if (value.Contains("RESONANCE")) return "Allied Arts resonate through the formation.";
            if (value.Contains("SABOTEUR")) return "A weakness is marked and quietly exploited.";
            if (value.Contains("SCOUT")) return "The battlefield is read one move ahead.";
            return "A specialist role strengthens the complete Union order.";
        }

        private static bool UsesArtAudio(BattleBeatFamily family)
        {
            return family == BattleBeatFamily.BasicMartial ||
                   family == BattleBeatFamily.CombatArt ||
                   family == BattleBeatFamily.Mystic ||
                   family == BattleBeatFamily.Restoration ||
                   family == BattleBeatFamily.Guard ||
                   family == BattleBeatFamily.Interception ||
                   family == BattleBeatFamily.Recovery ||
                   family == BattleBeatFamily.Formation ||
                   family == BattleBeatFamily.Tactical ||
                   family == BattleBeatFamily.Invocation;
        }

        private static string SafeCaption(string caption, string fallback)
        {
            if (string.IsNullOrWhiteSpace(caption)) return fallback;
            var value = caption.Trim();
            if (value.Length > 132) value = value.Substring(0, 129).TrimEnd() + "...";
            return value;
        }

        public static string BreakthroughCaptionForVerification076(
            M2BattleEventView breakthrough)
        {
            var artName = M2BattleReadableText021.ArtDisplayName(
                breakthrough?.ArtId,
                breakthrough?.Text,
                true);
            if (string.IsNullOrWhiteSpace(artName)) artName = "New Art";
            return artName.Trim().ToUpperInvariant() +
                   " LEARNED  •  READY FOR FUTURE BATTLES";
        }

        private static string HumanOutcome(M2BattleView battle)
        {
            if (battle == null) return "The round is complete.";
            if (string.Equals(battle.Outcome, "Victory", StringComparison.OrdinalIgnoreCase))
                return "The enemy line breaks. The Guild holds the field.";
            if (string.Equals(battle.Outcome, "Retreat", StringComparison.OrdinalIgnoreCase))
                return "The Guild withdraws with its surviving members.";
            if (string.Equals(battle.Outcome, "Defeat", StringComparison.OrdinalIgnoreCase))
                return "The Guild line falls. Regroup and return stronger.";
            return "The round is complete.";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null) return string.Empty;
            for (var index = 0; index < values.Length; index++)
                if (!string.IsNullOrWhiteSpace(values[index])) return values[index];
            return string.Empty;
        }

        private static bool SkipRequested(Func<bool> skip) => skip != null && skip();

        private static bool IsActionBeat108(BattleBeatFamily family) =>
            family == BattleBeatFamily.BasicMartial || family == BattleBeatFamily.CombatArt ||
            family == BattleBeatFamily.Mystic || family == BattleBeatFamily.Tactical ||
            family == BattleBeatFamily.Restoration || family == BattleBeatFamily.Guard ||
            family == BattleBeatFamily.Recovery;

        private static float CurrentSpeed108(Func<float> speed) =>
            Mathf.Clamp(speed == null ? 1f : speed(), 0.25f,
                M2BattleExperienceController072.MaximumPlaybackSpeed108);

        private static IEnumerator Wait(float seconds, Func<float> speed, Func<bool> skip)
        {
            var elapsed = 0f;
            while (elapsed < seconds && !SkipRequested(skip))
            {
                var multiplier = CurrentSpeed108(speed);
                elapsed += Time.unscaledDeltaTime * multiplier;
                yield return null;
            }
        }
    }
}
