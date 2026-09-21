using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Battle.ArtProduction011;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleArt012Tests
    {
        [Test]
        public void EveryPoseVfxAndIconLoadsFromItsStableResourcesPath()
        {
            BattleArtManifest011 manifest = BattleArtManifestLoader011.Load();
            IEnumerable<string> paths = manifest.characters.SelectMany(character => character.poses)
                .Select(pose => pose.resourcesPath)
                .Concat(manifest.vfx.Select(vfx => vfx.resourcesPath))
                .Concat(manifest.icons.Select(icon => icon.resourcesPath));

            foreach (string path in paths)
                Assert.That(Resources.Load<Sprite>(path), Is.Not.Null, path);
        }

        [Test]
        public void ManifestPathsAreUniqueAndPoseSetsAreComplete()
        {
            BattleArtManifest011 manifest = BattleArtManifestLoader011.Load();
            string[] expectedPoses =
            {
                "IDLE_READY", "ANTICIPATION", "PRIMARY_ACTION", "ROLE_ACTION",
                "GUARD_CAST_SUPPORT", "HIT_REACTION", "DOWNED", "VICTORY",
            };

            foreach (CharacterPoseSet011 character in manifest.characters)
            {
                CollectionAssert.AreEquivalent(expectedPoses, character.poses.Select(pose => pose.poseId));
                Assert.That(character.poses.All(pose => !string.IsNullOrWhiteSpace(pose.productionStatus)), Is.True);
            }

            string[] allPaths = manifest.characters.SelectMany(character => character.poses)
                .Select(pose => pose.resourcesPath)
                .Concat(manifest.vfx.Select(vfx => vfx.resourcesPath))
                .Concat(manifest.icons.Select(icon => icon.resourcesPath))
                .ToArray();
            Assert.That(allPaths.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(allPaths.Length));
        }

        [Test]
        public void ProductionStatusesRemainHonestAndRuntimeGenerationStaysOff()
        {
            BattleArtManifest011 manifest = BattleArtManifestLoader011.Load();
            int proxyCount = manifest.characters.SelectMany(character => character.poses)
                .Count(pose => pose.productionStatus.Contains("PROXY"));
            Assert.That(proxyCount, Is.GreaterThan(0), "Proxy art must remain honestly identified until replaced.");
            Assert.That(manifest.laws.runtimeGenerativeAI, Is.False);
            Assert.That(manifest.laws.presentationOnly, Is.True);
            Assert.That(manifest.laws.changesBattleResolution, Is.False);
        }
    }
}
