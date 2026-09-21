using UnityEngine;

namespace SecondDimension.Presentation.GuildCity017F
{
    [DisallowMultipleComponent]
    public sealed class GuildCityAmbientAudio017F : MonoBehaviour
    {
        private AudioSource _source;
        public AudioSource Source => _source;

        private void Awake()
        {
            _source=GetComponent<AudioSource>();
            if (_source == null) _source=gameObject.AddComponent<AudioSource>();
            _source.playOnAwake=false; _source.spatialBlend=0f;
        }

        public bool PlayCue(string cueId)
        {
            if (_source == null) Awake();
            var cue=GuildCityNarrativeRegistry017F.AudioCue(cueId); var clip=GuildCityNarrativeRegistry017F.Audio(cueId);
            if (cue == null || clip == null) return false;
            _source.loop=string.Equals(cue.mode,"loop",System.StringComparison.OrdinalIgnoreCase);
            _source.clip=clip; _source.Play(); return true;
        }

        public void StopCue()
        {
            if (_source != null) { _source.Stop(); _source.clip=null; }
        }
    }
}
