using System;
using System.Collections.Generic;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only procedural cue bank for the M2 vertical slice. These small,
    /// original one-shot placeholders make every cinematic family audible without
    /// importing unverified third-party audio. They never feed timing or results back
    /// into the authoritative combat domain and are explicitly replaceable by authored
    /// clips through the same cue identifiers.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class M2BattleAudioDirector : MonoBehaviour
    {
        private const int SampleRate = 22050;
        private readonly Dictionary<string, AudioClip> _clips =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly HashSet<AudioClip> _generatedClips = new HashSet<AudioClip>();
        private AudioSource _source;
        private AudioSource _musicSource;

        public int OneShotPlaybackCount076 { get; private set; }
        public int ResourceOneShotPlaybackCount076 { get; private set; }
        public int SynthesizedOneShotPlaybackCount076 { get; private set; }
        public string LastOneShotCueId076 { get; private set; } = string.Empty;
        public bool LastOneShotUsedAuthoredResource076 { get; private set; }

        private void Awake()
        {
            EnsureActiveListener();
            _source = gameObject.GetComponent<AudioSource>();
            if (_source == null) _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.volume = 0.24f;
            _source.ignoreListenerPause = true;
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.playOnAwake = false;
            _musicSource.loop = true;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = 0.20f;
            _musicSource.ignoreListenerPause = true;
        }

        private void EnsureActiveListener()
        {
            foreach (var listener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy) return;
            }

            var fallback = gameObject.GetComponent<AudioListener>();
            if (fallback == null) fallback = gameObject.AddComponent<AudioListener>();
            fallback.enabled = true;
        }

        public void PlayCue(string cueId)
        {
            TryPlayCue(cueId);
        }

        public bool TryPlayCue(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId) || _source == null) return false;
            if (!_clips.TryGetValue(cueId, out var clip))
            {
                clip = BuildCue(cueId);
                _clips.Add(cueId, clip);
                if (clip != null) _generatedClips.Add(clip);
            }
            if (clip == null) return false;
            _source.PlayOneShot(clip);
            RecordOneShot076(cueId, false);
            return true;
        }

        /// <summary>Plays a short authored local Resources cue. Missing clips fall back silently
        /// without changing battle resolution or event timing.</summary>
        public void PlayResourceCue(string resourcePath)
        {
            TryPlayResourceCue(resourcePath);
        }

        public bool TryPlayResourceCue(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath) || _source == null) return false;
            var key = "RESOURCE:" + resourcePath;
            if (!_clips.TryGetValue(key, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>(resourcePath);
                if (clip != null) _clips[key] = clip;
            }
            if (clip == null) return false;
            _source.PlayOneShot(clip);
            RecordOneShot076(key, true);
            return true;
        }

        private void RecordOneShot076(string cueId, bool authoredResource)
        {
            OneShotPlaybackCount076++;
            if (authoredResource) ResourceOneShotPlaybackCount076++;
            else SynthesizedOneShotPlaybackCount076++;
            LastOneShotCueId076 = cueId ?? string.Empty;
            LastOneShotUsedAuthoredResource076 = authoredResource;
        }

        public void SetBattleMusic(AudioClip authoredTrack)
        {
            if (_musicSource == null) return;
            if (_musicSource.clip == authoredTrack && _musicSource.isPlaying) return;
            _musicSource.Stop();
            _musicSource.clip = authoredTrack;
            if (authoredTrack != null) _musicSource.Play();
        }

        public void StopBattleMusic()
        {
            if (_musicSource == null) return;
            _musicSource.Stop();
            _musicSource.clip = null;
        }

        private static AudioClip BuildCue(string cueId)
        {
            var values = SynthesizeCueSamples(cueId);
            var samples = values.Length;
            var clip = AudioClip.Create("M2 Original Placeholder · " + cueId, samples, 1, SampleRate, false);
            clip.SetData(values, 0);
            return clip;
        }

        /// <summary>
        /// Returns the deterministic mono signal used by the runtime cue. Keeping the
        /// synthesis observable lets EditMode coverage prove that every battle cue is
        /// non-silent without depending on an operating-system audio device or recorder.
        /// </summary>
        public static float[] SynthesizeCueSamples(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId)) return Array.Empty<float>();
            var duration = CueDuration(cueId);
            var samples = Math.Max(1, Mathf.RoundToInt(duration * SampleRate));
            var values = new float[samples];
            var seed = StableSeed(cueId);
            var frequency = CueFrequency(cueId);
            var noise = cueId.IndexOf("IMPACT", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        cueId.IndexOf("DOWNED", StringComparison.OrdinalIgnoreCase) >= 0;
            var shimmer = cueId.IndexOf("MYSTIC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cueId.IndexOf("RESTORATION", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          cueId.IndexOf("BREAKTHROUGH", StringComparison.OrdinalIgnoreCase) >= 0;

            for (var index = 0; index < samples; index++)
            {
                var time = (float)index / SampleRate;
                var normalized = (float)index / samples;
                var envelope = Mathf.Pow(1f - normalized, noise ? 3.2f : 1.8f);
                var tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
                if (shimmer)
                    tone = tone * 0.72f + Mathf.Sin(2f * Mathf.PI * frequency * 1.51f * time) * 0.28f;
                if (noise)
                {
                    seed = seed * 1664525u + 1013904223u;
                    var white = ((seed >> 8) / 16777215f) * 2f - 1f;
                    tone = tone * 0.36f + white * 0.64f;
                }
                values[index] = Mathf.Clamp(tone * envelope * 0.55f, -1f, 1f);
            }
            return values;
        }

        private static float CueDuration(string cueId)
        {
            if (cueId.IndexOf("BREAKTHROUGH", StringComparison.OrdinalIgnoreCase) >= 0) return 0.68f;
            if (cueId.IndexOf("VICTORY", StringComparison.OrdinalIgnoreCase) >= 0 ||
                cueId.IndexOf("RESULT", StringComparison.OrdinalIgnoreCase) >= 0) return 0.55f;
            if (cueId.IndexOf("IMPACT", StringComparison.OrdinalIgnoreCase) >= 0) return 0.18f;
            return 0.32f;
        }

        private static float CueFrequency(string cueId)
        {
            if (cueId.IndexOf("RESTORATION", StringComparison.OrdinalIgnoreCase) >= 0) return 620f;
            if (cueId.IndexOf("MYSTIC", StringComparison.OrdinalIgnoreCase) >= 0) return 430f;
            if (cueId.IndexOf("GUARD", StringComparison.OrdinalIgnoreCase) >= 0) return 190f;
            if (cueId.IndexOf("BREAKTHROUGH", StringComparison.OrdinalIgnoreCase) >= 0) return 760f;
            if (cueId.IndexOf("IMPACT", StringComparison.OrdinalIgnoreCase) >= 0) return 120f;
            return 280f;
        }

        private static uint StableSeed(string value)
        {
            var result = 2166136261u;
            for (var index = 0; index < value.Length; index++) result = (result ^ value[index]) * 16777619u;
            return result;
        }

        private void OnDestroy()
        {
            // Only runtime-synthesized clips are owned by this component. Resources-loaded
            // authored clips are Unity assets and must never be destroyed by a presenter.
            foreach (var clip in _generatedClips)
                if (clip != null) Destroy(clip);
            _generatedClips.Clear();
            _clips.Clear();
        }
    }
}
