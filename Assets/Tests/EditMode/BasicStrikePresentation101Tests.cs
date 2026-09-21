using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BasicStrikePresentation101Tests
    {
        [TestCase("MARTIAL_HIT")]
        [TestCase("TACTICAL_HIT")]
        public void ExactBasicStrikeTitleDoesNotBorrowPhysicalProfileHeroArtName101(string eventType)
        {
            var beat = Beat101("ART_BASIC_STRIKE_101", eventType, "An ally uses Basic Strike on an enemy for 25 HP.");
            var profile = BattleArtRuntimeRegistry011.ResolveProfile(beat.ArtId, beat.Family);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.artClass, Is.EqualTo("COMBAT_ART"));
            Assert.That(profile.artId, Is.EqualTo("ART_BASIC_STRIKE_101"));
            Assert.That(profile.displayName, Is.EqualTo("Basic Strike"));
            Assert.That(Title101(profile, beat), Is.EqualTo("BASIC STRIKE"));
            Assert.That(BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles.First(value => value.artClass == "COMBAT_ART")
                .displayName, Is.EqualTo("Ready Cut"), "Do not rename the shared authored physical profile.");
        }

        [Test]
        public void NewIdAndAnimationTagUseLoadablePhysicalHitAndExactActorActionPose101()
        {
            var action = new M2PredictedActionView { ArtId = "ART_BASIC_STRIKE_101", ArtName = "Basic Strike",
                ActionKind = "Martial", Discipline = "Martial", AnimationTag = "ART_BASIC_STRIKE_ANIM" };
            Assert.That(BattleActionPresentationPlanning008.ResolveFamily(action, BattleRelationshipKind008.Deadlock, false),
                Is.EqualTo(BattleActionFamily008.BasicMartial));
            var beat = Beat101(action.ArtId, "MARTIAL_HIT", "An ally uses Basic Strike on an enemy for 25 HP.");
            Assert.That(beat.Family, Is.EqualTo(BattleBeatFamily.BasicMartial));
            Assert.That(beat.VfxCue, Is.EqualTo("VFX_WEAPON_IMPACT"));
            Assert.That(beat.AudioCue, Is.EqualTo("SFX_WEAPON_IMPACT"));
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(beat.Family), Is.EqualTo(BattleArtPoseDirector011.ActionPrimary));
            var profile = BattleArtRuntimeRegistry011.ResolveProfile(beat.ArtId, beat.Family);
            Assert.That(profile.motionProfile, Is.EqualTo("MARTIAL_APPROACH_CONTACT_RETURN"));
            Assert.That(profile.exactFirstHourRecipe, Is.False);
            Assert.That(string.IsNullOrEmpty(profile.presentationRecipeId), Is.True);
            Assert.That(string.IsNullOrEmpty(profile.presentationTreeId), Is.True);
            Assert.That(string.IsNullOrEmpty(profile.presentationMotionSignature), Is.True);
            Assert.That(string.IsNullOrEmpty(profile.presentationVfxSignature), Is.True);
            Assert.That(string.IsNullOrEmpty(profile.presentationSfxSignature), Is.True);
            Assert.That(profile.presentationDurationMilliseconds, Is.Zero);
            Assert.That(profile.presentationImpactMilliseconds, Is.Zero);
            Assert.That(profile.projectileResourcePath, Is.Empty);
            Assert.That(BattleArtRuntimeRegistry011.LoadSprite(profile.trailResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadSprite(profile.impactResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.startAudioResourcePath), Is.Not.Null);
            Assert.That(BattleArtRuntimeRegistry011.LoadAudio(profile.impactAudioResourcePath), Is.Not.Null);
            Assert.That(M2BattleDioramaView072.TryResolveLiveExactRecipe076(profile, beat.ArtId, out _), Is.False,
                "This is a shared physical-family presentation, not a falsely claimed unique Ready Cut choreography.");
        }

        [TestCase("ART_ASSIST_ALLY", "ALLY_SUPPORT")]
        [TestCase("TREE_CA002_WPN_SWORD_N01", "MARTIAL_HIT")]
        [TestCase("ART_BASIC_STRIKE_101_SUFFIX", "MARTIAL_HIT")]
        public void ExistingAssistReadyCutAndNonExactIdsKeepTheirPreviousTitlePath101(string artId, string eventType)
        {
            // This exact new-ID fix deliberately does not rewrite the pre-existing
            // Assist Ally->Ready Cut legacy presentation alias or arbitrary Arts.
            var beat = Beat101(artId, eventType, "Existing authoritative event prose.");
            var profile = BattleArtRuntimeRegistry011.ResolveProfile(artId, beat.Family);
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.displayName, Is.EqualTo("Ready Cut"));
            Assert.That(Title101(profile, beat), Is.EqualTo("READY CUT"));
            Assert.That(Title101(profile, beat), Is.Not.EqualTo("BASIC STRIKE"));
        }

        [TestCase("ART_BASIC_STRIKE_101", "", "Basic Strike")]
        [TestCase("ART_BASIC_STRIKE_101", "Explicit Display Override", "Explicit Display Override")]
        [TestCase("art_basic_strike_101", "", "Art Basic Strike 101")]
        [TestCase("ART_BASIC_STRIKE_101_SUFFIX", "", "Strike 101 Suffix")]
        [TestCase("ART_ASSIST_ALLY", "", "Assist Ally")]
        [TestCase("TREE_CA002_WPN_SWORD_N01", "Ready Cut", "Ready Cut")]
        public void ExactNoCaptionHumanizerPreservesRegisteredMetadataAndOtherIdRules101(string id, string registered, string expected)
        {
            Assert.That(M2BattleReadableText021.ArtDisplayName(id, string.Empty, false, registered), Is.EqualTo(expected));
        }

        [Test]
        public void CachedExactProfileCopiesOnlyIdentityAndResetsWithoutMutatingManifest101()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            var count = manifest.runtimeArtProfiles.Length;
            var source = manifest.runtimeArtProfiles.First(value => value.artClass == "COMBAT_ART");
            var profile = BattleArtRuntimeRegistry011.ResolveProfile("ART_BASIC_STRIKE_101", BattleBeatFamily.BasicMartial);
            Assert.That(profile, Is.Not.SameAs(source));
            foreach (var field in typeof(BattleArtProfile011).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (field.Name != "artId" && field.Name != "displayName")
                    Assert.That(field.GetValue(profile), Is.EqualTo(field.GetValue(source)), field.Name);
            Assert.That(source.artId, Is.EqualTo("TREE_CA002_WPN_SWORD_N01"));
            Assert.That(source.displayName, Is.EqualTo("Ready Cut"));
            Assert.That(manifest.runtimeArtProfiles.Length, Is.EqualTo(count));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("ART_BASIC_STRIKE_101", BattleBeatFamily.BasicMartial), Is.SameAs(profile));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile(" art_basic_strike_101 ", BattleBeatFamily.BasicMartial), Is.SameAs(profile),
                "Keep the registry's existing normalization policy; the raw-ID humanizer remains ordinal.");
            BattleArtRuntimeRegistry011.ReloadForTests();
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("ART_BASIC_STRIKE_101", BattleBeatFamily.BasicMartial), Is.Not.SameAs(profile));
            Assert.That(source.displayName, Is.EqualTo("Ready Cut"));
        }

        static BattlePresentationBeat Beat101(string artId, string eventType, string text) =>
            BattlePresentationPlanner.Plan(new[] { new M2BattleEventView { Sequence = 1, Round = 1,
                EventType = eventType, ArtId = artId, Text = text, Amount = 25, ActorUnionId = "PLAYER",
                ActorMemberId = "ACTOR", TargetUnionId = "ENEMY", TargetMemberId = "TARGET" } })[0];

        static string Title101(BattleArtProfile011 profile, BattlePresentationBeat beat)
        {
            var method = typeof(M2BattleSequenceDirector072).GetMethod("ActionTitle", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (string)method.Invoke(null, new object[] { profile, beat });
        }
    }
}
