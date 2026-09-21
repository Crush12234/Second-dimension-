using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017F;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class GuildCityNarrativeFacility017FPlayModeTests
    {
        [UnityTest] public IEnumerator AmbientAudioComponentLoadsLocalCue()
        {
            var go=new GameObject("Guild City Audio 017F");
            var audio=go.AddComponent<GuildCityAmbientAudio017F>();
            yield return null;
            Assert.That(audio.PlayCue("AUDIO_017F_CONTRACT_ACCEPT"),Is.True);
            Assert.That(audio.Source,Is.Not.Null);
            Assert.That(audio.Source.clip,Is.Not.Null);
            Object.Destroy(go);
        }
    }
}
