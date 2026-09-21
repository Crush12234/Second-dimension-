using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ResolvedRecoveryPresentation105Tests
    {
        [TestCase("ART_RECOVER_BREATH", "RECOVERY", 0,
            "Gara Redtail conserves strength; no MP was missing.", "RECOVER BREATH")]
        [TestCase("ART_RECOVER_BREATH", "RECOVERY", 2,
            "Tala Stormroad conserves strength and recovers 2 MP.", "RECOVER BREATH")]
        [TestCase("CMD_AP_RECOVERY", "AP_RECOVERY", 6,
            "Opening Union 1 recovers 6 shared AP.", "AP RECOVERY")]
        [TestCase("TREE_CA002_WPN_SWORD_N01", "RECOVERY", 0,
            "The attempted attack cannot be paid; the member conserves strength.", "RECOVERY")]
        [TestCase("TREE_CA002_ROLE_RESONANCE_N01", "RECOVERY", 0,
            "Yves Thornfield supports the line; no MP was missing.", "RECOVERY")]
        public void ResolvedRecoveryOwnsTheCompletePresentation105(
            string artId, string eventType, int amount, string prose, string title)
        {
            var item = new M2BattleEventView { Sequence=1, Round=4, EventType=eventType,
                ArtId=artId, Amount=amount, Text=prose, ActorUnionId="UNION_OPENING_01",
                TargetUnionId="UNION_OPENING_01", ActorMemberId="ACTOR", TargetMemberId="ACTOR" };
            var before = JsonConvert.SerializeObject(item);
            var beat = BattlePresentationPlanner.Plan(new[] { item }).Single();
            var profile = BattleArtRuntimeRegistry011.ResolveProfile(beat.ArtId, beat.Family);
            Assert.That(beat.Family, Is.EqualTo(BattleBeatFamily.Recovery));
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.artClass, Is.EqualTo("RESTORATION_ART"));
            Assert.That(profile.artId, Is.EqualTo(artId));
            Assert.That(profile.displayName, Is.Not.EqualTo("Ready Cut"));
            Assert.That(M2BattleDioramaView072.TryResolveLiveExactRecipe076(profile, artId, out _), Is.False,
                "An attempted Art's recipe must not override the resolved recovery event.");
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(beat.Family),
                Is.EqualTo(BattleArtPoseDirector011.Idle));
            Assert.That(Call("ActionTitle", profile, beat), Is.EqualTo(title));
            Assert.That(Call("ActionSubtitle", profile, beat), Is.EqualTo(prose));
            Assert.That(profile.trailResourcePath, Is.Empty);
            Assert.That(BattleArtRuntimeRegistry011.LoadSprite(profile.impactResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.startAudioResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.impactAudioResourcePath), Is.Not.Null);
            Assert.That(JsonConvert.SerializeObject(item), Is.EqualTo(before));
        }

        [Test]
        public void RecoveryDoesNotRenameOrOverwriteTheSharedSwordAndRestorationProfiles105()
        {
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            var before = JsonConvert.SerializeObject(manifest);
            var attack = BattleArtRuntimeRegistry011.ResolveProfile("TREE_CA002_WPN_SWORD_N01", BattleBeatFamily.CombatArt);
            BattleArtRuntimeRegistry011.ResolveProfile("TREE_CA002_WPN_SWORD_N01", BattleBeatFamily.Recovery);
            BattleArtRuntimeRegistry011.ResolveProfile("ART_RECOVER_BREATH", BattleBeatFamily.Recovery);
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("TREE_CA002_WPN_SWORD_N01", BattleBeatFamily.CombatArt), Is.SameAs(attack));
            Assert.That(attack.displayName, Is.EqualTo("Ready Cut"));
            Assert.That(attack.exactFirstHourRecipe, Is.True);
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(BattleBeatFamily.CombatArt), Is.EqualTo(BattleArtPoseDirector011.ActionPrimary));
            Assert.That(JsonConvert.SerializeObject(manifest), Is.EqualTo(before));
        }

        private static string Call(string name, BattleArtProfile011 profile, BattlePresentationBeat beat)
        {
            var method = typeof(M2BattleSequenceDirector072).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { profile, beat });
        }
    }
}
