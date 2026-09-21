using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleArtThursday011PlayModeTests
    {
        [UnityTest]
        public IEnumerator RegistryLoadsPosesVfxAndLocalAudioWithoutRuntimeGeneration()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            Assert.That(manifest.law.runtimeGenerativeAi, Is.False);
            Assert.That(BattleArtRuntimeRegistry011.TryResolvePose(
                "SIGREC_MAREN_HOLT", "POSE_ACTION_PRIMARY", out var pose, out _), Is.True);
            Assert.That(pose, Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadSprite(
                "SecondDimension/Art/Battle011/VFX/WeaponTrails/VFX_TRAIL_SPEAR_POLEARM"), Is.Not.Null);
            var profile = BattleArtRuntimeRegistry011.ResolveProfile(
                "TREE_CA002_WPN_SPEAR_POLEARM_N01", BattleBeatFamily.CombatArt);
            Assert.That(profile, Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.startAudioResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.impactAudioResourcePath), Is.Not.Null);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AudioDirectorAcceptsAuthoredCueAndMissingCueSafely()
        {
            var root = new GameObject("Battle Art 011 Audio Test");
            root.AddComponent<AudioSource>();
            var director = root.AddComponent<M2BattleAudioDirector>();
            yield return null;
            Assert.DoesNotThrow(() => director.PlayResourceCue(
                "SecondDimension/Audio/Battle011/Mystics/SFX_RESTORATION_CAST"));
            Assert.DoesNotThrow(() => director.PlayResourceCue(
                "SecondDimension/Audio/Battle011/DoesNotExist"));
            Object.Destroy(root);
            yield return null;
        }
    }
}
