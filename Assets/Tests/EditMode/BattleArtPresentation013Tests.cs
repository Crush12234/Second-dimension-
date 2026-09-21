using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Battle.ArtProduction011;
using SecondDimension.Presentation.Battle.ArtProduction013;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleArtPresentation013Tests
    {
        [Test]
        public void AllTwoHundredFortyArtsHavePresentationBindings()
        {
            ArtPresentationBindingManifest013 manifest = ArtPresentationBindingLoader013.Load();
            Assert.That(manifest.bindingCount, Is.EqualTo(240));
            Assert.That(manifest.bindings, Has.Length.EqualTo(240));
            Assert.That(manifest.bindings.Select(value => value.stableArtId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(240));
        }

        [Test]
        public void WeaponMysticRestorationAndWardingCoverageIsExact()
        {
            ArtPresentationBinding013[] bindings = ArtPresentationBindingLoader013.Load().bindings;
            Assert.That(bindings.Count(value => value.artClass == "COMBAT_ART"), Is.EqualTo(144));
            Assert.That(bindings.Count(value => value.artClass == "MYSTIC_ATTACK"), Is.EqualTo(72));
            Assert.That(bindings.Count(value => value.artClass == "RESTORATION_ART"), Is.EqualTo(12));
            Assert.That(bindings.Count(value => value.artClass == "WARDING_ART"), Is.EqualTo(12));
            Assert.That(bindings.Where(value => !string.IsNullOrWhiteSpace(value.weaponFamilyId)).Select(value => value.weaponFamilyId).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(12));
        }

        [Test]
        public void EveryBindingReferencesLoadableVisualIds()
        {
            BattleArtManifest011 visuals = BattleArtManifestLoader013.Load();
            HashSet<string> ids = new HashSet<string>(visuals.vfx.Select(value => value.assetId).Concat(visuals.icons.Select(value => value.assetId)), StringComparer.Ordinal);
            foreach (ArtPresentationBinding013 binding in ArtPresentationBindingLoader013.Load().bindings)
            {
                Assert.That(ids.Contains(binding.primaryVfxId), Is.True, binding.stableArtId + "/" + binding.primaryVfxId);
                Assert.That(ids.Contains(binding.impactVfxId), Is.True, binding.stableArtId + "/" + binding.impactVfxId);
                Assert.That(ids.Contains(binding.semanticIconId), Is.True, binding.stableArtId + "/" + binding.semanticIconId);
            }
        }

        [Test]
        public void EveryVisualResourceLoadsAndTwentyUnionLawRemainsLocked()
        {
            BattleArtManifest011 visuals = BattleArtManifestLoader013.Load();
            foreach (CharacterPoseSet011 character in visuals.characters)
                foreach (PoseAsset011 pose in character.poses)
                    Assert.That(Resources.Load<Sprite>(pose.resourcesPath), Is.Not.Null, pose.resourcesPath);
            foreach (VfxAsset011 vfx in visuals.vfx)
                Assert.That(Resources.Load<Sprite>(vfx.resourcesPath), Is.Not.Null, vfx.resourcesPath);
            foreach (IconAsset011 icon in visuals.icons)
                Assert.That(Resources.Load<Sprite>(icon.resourcesPath), Is.Not.Null, icon.resourcesPath);
            Assert.That(visuals.laws.supportsAlliedUnionSlots, Is.EqualTo(10));
            Assert.That(visuals.laws.supportsEnemyUnionSlots, Is.EqualTo(10));
        }

        [Test]
        public void IndividualArtSelectionAndRuntimeGenerativeAiStayForbidden()
        {
            ArtPresentationBindingManifest013 manifest = ArtPresentationBindingLoader013.Load();
            Assert.That(manifest.laws.presentationOnly, Is.True);
            Assert.That(manifest.laws.changesBattleResolution, Is.False);
            Assert.That(manifest.laws.individualArtSelectableInStandard, Is.False);
            Assert.That(manifest.laws.runtimeGenerativeAI, Is.False);
            Assert.That(manifest.bindings.All(value => !value.memberActionClickable && !value.playerDirectlySelectableInStandard), Is.True);
        }

        [Test]
        public void ResolverUsesBespokeArtForKnownActorsWithoutChangingResolution()
        {
            ArtPresentationBinding013 first = ArtPresentationBindingLoader013.Load().bindings.First(value => value.artClass == "COMBAT_ART" && value.nodeType == "ACTION");
            Assert.That(BattleArtPresentationResolver013.TryResolve("SIGREC_MAREN_HOLT", first.stableArtId, null, out ResolvedArtPresentationPlan013 plan), Is.True);
            Assert.That(plan.visualSource, Is.EqualTo(BattleVisualSourceKind013.BespokePoseSet011));
            Assert.That(plan.presentationOnly, Is.True);
            Assert.That(plan.primaryVfxId, Is.Not.Empty);
            Assert.That(plan.impactVfxId, Is.Not.Empty);
        }
    }
}
