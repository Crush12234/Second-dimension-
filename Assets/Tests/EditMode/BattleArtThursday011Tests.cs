using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleArtThursday011Tests
    {
        [SetUp]
        public void Reset() => BattleArtRuntimeRegistry011.ReloadForTests();

        [Test]
        public void Manifest_ContainsExpectedThursdayCountsAndLaws()
        {
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            Assert.That(manifest.characterCount, Is.EqualTo(9));
            Assert.That(manifest.characters.Length, Is.EqualTo(9));
            Assert.That(manifest.runtimeArtProfileCount, Is.EqualTo(240));
            Assert.That(manifest.runtimeArtProfiles.Length, Is.EqualTo(240));
            Assert.That(manifest.uiAssetCount, Is.EqualTo(21));
            Assert.That(manifest.uiAssets.Length, Is.EqualTo(21));
            Assert.That(manifest.legacyAliasCount, Is.GreaterThanOrEqualTo(100));
            Assert.That(manifest.law.runtimeGenerativeAi, Is.False);
            Assert.That(manifest.law.presentationNeverChangesResolution, Is.True);
            Assert.That(manifest.law.playerSelectsCompleteUnionForecast, Is.True);
            Assert.That(manifest.law.memberActionClickable, Is.False);
            Assert.That(manifest.law.maximumAllyUnions, Is.EqualTo(10));
            Assert.That(manifest.law.maximumEnemyUnions, Is.EqualTo(10));
        }

        [Test]
        public void EveryTutorialCombatant_HasEightCleanPoseAssets()
        {
            foreach (var character in BattleArtRuntimeRegistry011.Manifest.characters)
            {
                Assert.That(character.poses.Length, Is.EqualTo(8), character.memberId);
                foreach (var pose in character.poses)
                {
                    Assert.That(BattleArtRuntimeRegistry011.TryResolvePose(character.memberId, pose.poseId, out var sprite, out _),
                        Is.True, character.memberId + " " + pose.poseId);
                    Assert.That(sprite, Is.Not.Null);
                }
            }
        }

        [Test]
        public void WeaponAndMysticFamilies_HaveReusableVisualAssets()
        {
            var manifest = BattleArtRuntimeRegistry011.Manifest;
            var trails = manifest.vfx.Where(value => value.kind == "WEAPON_TRAIL").ToArray();
            Assert.That(trails.Length, Is.EqualTo(12));
            foreach (var trail in trails) Assert.That(Resources.Load<Sprite>(trail.resourcePath), Is.Not.Null, trail.vfxId);

            foreach (var school in new[] { "FLAME", "FROST", "STORM", "EARTH", "AETHER", "SHADOW", "RESTORATION", "WARDING" })
            {
                var schoolVfx = manifest.vfx.Where(value => value.schoolId == school).ToArray();
                Assert.That(schoolVfx.Select(value => value.kind), Does.Contain("PROJECTILE"), school);
                Assert.That(schoolVfx.Select(value => value.kind), Does.Contain("IMPACT"), school);
                Assert.That(schoolVfx.Select(value => value.kind), Does.Contain("FIELD"), school);
                foreach (var value in schoolVfx) Assert.That(Resources.Load<Sprite>(value.resourcePath), Is.Not.Null, value.vfxId);
            }
        }

        [Test]
        public void UiAssetsAndAudioPhases_AreDirectlyLoadable()
        {
            foreach (var asset in BattleArtRuntimeRegistry011.Manifest.uiAssets)
            {
                Assert.That(BattleArtRuntimeRegistry011.TryResolveUiAsset(asset.assetId, out _, out var sprite),
                    Is.True, asset.assetId);
                Assert.That(sprite, Is.Not.Null, asset.assetId);
            }
            foreach (var profile in BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles)
            {
                Assert.That(profile.windupAudioResourcePath, Is.Not.Empty, profile.artId + " windup");
                Assert.That(profile.impactAudioResourcePath, Is.Not.Empty, profile.artId + " impact");
                Assert.That(Resources.Load<AudioClip>(profile.windupAudioResourcePath), Is.Not.Null,
                    profile.artId + " windup clip");
                Assert.That(Resources.Load<AudioClip>(profile.impactAudioResourcePath), Is.Not.Null,
                    profile.artId + " impact clip");
            }
        }

        [Test]
        public void EveryUiIconFrameAndGroundShadow_LoadsAsSprite()
        {
            var assets = BattleArtRuntimeRegistry011.Manifest.uiAssets;
            Assert.That(assets.Length, Is.EqualTo(21));
            foreach (var asset in assets)
                Assert.That(Resources.Load<Sprite>(asset.resourcePath), Is.Not.Null, asset.assetId);
        }

        [Test]
        public void EveryRuntimeArt_HasLoadableStartAndImpactAudio()
        {
            foreach (var profile in BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles)
            {
                Assert.That(profile.startAudioResourcePath, Is.Not.Empty, profile.artId + " start cue");
                Assert.That(profile.impactAudioResourcePath, Is.Not.Empty, profile.artId + " impact cue");
                Assert.That(Resources.Load<AudioClip>(profile.startAudioResourcePath), Is.Not.Null,
                    profile.artId + " start cue");
                Assert.That(Resources.Load<AudioClip>(profile.impactAudioResourcePath), Is.Not.Null,
                    profile.artId + " impact cue");
            }
        }

        [Test]
        public void EveryRuntimeArt_RemainsForecastOnlyAndNonClickable()
        {
            foreach (var profile in BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles)
            {
                Assert.That(profile.playerDirectlySelectableInStandard, Is.False, profile.artId);
                Assert.That(profile.memberActionClickable, Is.False, profile.artId);
                Assert.That(profile.returnToFormationRequired ||
                    profile.motionProfile.Contains("NO_ACTIVE_MOTION") ||
                    profile.motionProfile.Contains("STANCE_ENTER_HOLD_EXIT"), Is.True, profile.artId);
            }
        }

        [Test]
        public void LegacyTutorialArts_ResolveToPresentationProfiles()
        {
            foreach (var id in new[]
            {
                "ART_BASIC_SABER_CUT", "ART_BASIC_THRUST", "ART_POWER_CUT", "ART_PIERCING_SHOT",
                "ART_EMBER_BOLT", "ART_MINOR_REMEDY", "ART_GUARD", "ART_SHIELD_BRACE"
            })
            {
                var profile = BattleArtRuntimeRegistry011.ResolveProfile(id, BattleBeatFamily.CombatArt);
                Assert.That(profile, Is.Not.Null, id);
                Assert.That(profile.playerDirectlySelectableInStandard, Is.False, id);
            }
        }


        [Test]
        public void UnknownArtFallback_RespectsPresentationFamily()
        {
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.Mystic).artClass,
                Is.EqualTo("MYSTIC_ATTACK"));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.Restoration).artClass,
                Is.EqualTo("RESTORATION_ART"));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.Guard).artClass,
                Is.EqualTo("WARDING_ART"));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.Formation).artClass,
                Is.EqualTo("WARDING_ART"));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.Recovery).artClass,
                Is.EqualTo("RESTORATION_ART"));
            Assert.That(BattleArtRuntimeRegistry011.ResolveProfile("UNKNOWN", BattleBeatFamily.BasicMartial).artClass,
                Is.EqualTo("COMBAT_ART"));
        }



        [Test]
        public void PoseDirector_SeparatesWeaponRoleHitAndRecoveryPhases()
        {
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(BattleBeatFamily.CombatArt),
                Is.EqualTo(BattleArtPoseDirector011.ActionPrimary));
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(BattleBeatFamily.Mystic),
                Is.EqualTo(BattleArtPoseDirector011.RolePrimary));
            Assert.That(BattleArtPoseDirector011.ActivePoseFor(BattleBeatFamily.Restoration),
                Is.EqualTo(BattleArtPoseDirector011.RolePrimary));
            Assert.That(BattleArtPoseDirector011.IsSupportedPoseId(BattleArtPoseDirector011.Anticipation), Is.True);
            Assert.That(BattleArtPoseDirector011.IsSupportedPoseId(BattleArtPoseDirector011.HitReaction), Is.True);
            Assert.That(BattleArtPoseDirector011.IsSupportedPoseId(BattleArtPoseDirector011.Recovery), Is.True);
        }


        [Test]
        public void EveryActiveArt_HasPhasedAudioAndPresentationAssets()
        {
            foreach (var profile in BattleArtRuntimeRegistry011.Manifest.runtimeArtProfiles)
            {
                Assert.That(profile.startAudioResourcePath, Is.Not.Empty, profile.artId);
                Assert.That(profile.impactAudioResourcePath, Is.Not.Empty, profile.artId);
                if (profile.artClass == "MYSTIC_ATTACK" || profile.artClass == "RESTORATION_ART" ||
                    profile.artClass == "WARDING_ART")
                    Assert.That(profile.fieldResourcePath, Is.Not.Empty, profile.artId);
            }
        }

        [Test]
        public void StaticRuntimeValidation_Passes()
        {
            Assert.That(BattleArtRuntimeRegistry011.ValidateRuntime(), Is.Empty);
        }
    }
}
