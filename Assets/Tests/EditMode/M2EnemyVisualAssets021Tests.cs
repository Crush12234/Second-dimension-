using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2EnemyVisualAssets021Tests
    {
        private static readonly string[] CanonicalMemberIds =
        {
            "ENEMY_GATE_GNAWER_01",
            "ENEMY_GATE_GNAWER_02",
            "ENEMY_GATE_GNAWER_03"
        };

        private static readonly string[] ExpectedIdleKeys =
        {
            M1VisualAssets.BattleRoot + "/ENEMY_GATE_GNAWER_A",
            M1VisualAssets.BattleRoot + "/ENEMY_GATE_GNAWER_SCOUT",
            M1VisualAssets.BattleRoot + "/ENEMY_GATE_GNAWER_BULWARK"
        };

        private static readonly string[] ExpectedActionKeys =
        {
            M1VisualAssets.BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_A",
            M1VisualAssets.BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_SCOUT",
            M1VisualAssets.BattleRoot + "/ACTION_ENEMY_GATE_GNAWER_BULWARK"
        };

        [Test]
        public void CanonicalGateGnawerMembersResolveDistinctIdleAndActionCutouts()
        {
            var idleKeys = new List<string>();
            var actionKeys = new List<string>();
            var idleSprites = new List<Sprite>();
            var actionSprites = new List<Sprite>();

            for (var index = 0; index < CanonicalMemberIds.Length; index++)
            {
                var memberId = CanonicalMemberIds[index];
                Assert.That(
                    M1VisualAssets.TryResolveEnemyBattleStandee(memberId, out var idle, out var idleKey),
                    Is.True,
                    memberId + " idle standee did not resolve.");
                Assert.That(
                    M1VisualAssets.TryResolveBattleActionPose(memberId, out var action, out var actionKey),
                    Is.True,
                    memberId + " action pose did not resolve.");

                Assert.That(idle, Is.Not.Null, memberId + " idle sprite was null.");
                Assert.That(action, Is.Not.Null, memberId + " action sprite was null.");
                Assert.That(idleKey, Is.EqualTo(ExpectedIdleKeys[index]), memberId);
                Assert.That(actionKey, Is.EqualTo(ExpectedActionKeys[index]), memberId);

                idleKeys.Add(idleKey);
                actionKeys.Add(actionKey);
                idleSprites.Add(idle);
                actionSprites.Add(action);
            }

            Assert.That(idleKeys.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            Assert.That(actionKeys.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(3));
            Assert.That(idleKeys.Concat(actionKeys).Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(6));
            Assert.That(idleSprites.Select(value => value.texture.GetInstanceID()).Distinct().Count(), Is.EqualTo(3));
            Assert.That(actionSprites.Select(value => value.texture.GetInstanceID()).Distinct().Count(), Is.EqualTo(3));

            foreach (var resourceKey in idleKeys.Concat(actionKeys))
                AssertSourcePngHasVisibleTransparency(resourceKey);
        }

        [Test]
        public void SixGateGnawerCutoutsHaveUniqueValidUnityGuids()
        {
            var resourceKeys = ExpectedIdleKeys.Concat(ExpectedActionKeys).ToArray();
            var guids = resourceKeys.Select(ReadMetaGuid).ToArray();

            foreach (var guid in guids)
                Assert.That(
                    Regex.IsMatch(guid ?? string.Empty, "^[0-9a-f]{32}$", RegexOptions.CultureInvariant),
                    Is.True,
                    guid + " is not a valid Unity GUID.");
            Assert.That(guids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(resourceKeys.Length),
                "Every idle/action cutout must have its own Unity asset GUID.");
        }

        [Test]
        public void NonEnemySuffixesCannotAccidentallySelectScoutOrBulwarkArtwork()
        {
            Assert.That(
                M1VisualAssets.TryResolveEnemyBattleStandee("UNRELATED_MEMBER_02", out _, out var resourceKey),
                Is.True);
            Assert.That(resourceKey, Is.EqualTo(M1VisualAssets.BattleRoot + "/ENEMY_GATE_GNAWER_A"));
        }

        [Test]
        public void Version70EnemySpawnsUseDistinctFamilySilhouettesWhenAuthoredCutoutsArePending()
        {
            Assert.That(M1VisualAssets.TryResolveEnemyBattleStandee(
                "ENEMY_RUSTBACK_HOUND_01_SPAWN070_A1B2C3D4E5F6",
                out var hound,
                out var houndKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveEnemyBattleStandee(
                "ENEMY_HINGE_EATER_COLOSSUS_01_SPAWN070_1029384756AB",
                out var colossus,
                out var colossusKey), Is.True);

            Assert.That(hound, Is.Not.Null);
            Assert.That(colossus, Is.Not.Null);
            Assert.That(houndKey, Does.StartWith("RUNTIME_ENEMY_SILHOUETTE_070/"));
            Assert.That(colossusKey, Does.StartWith("RUNTIME_ENEMY_SILHOUETTE_070/"));
            Assert.That(houndKey, Is.Not.EqualTo(colossusKey));
            Assert.That(hound.texture.GetInstanceID(), Is.Not.EqualTo(colossus.texture.GetInstanceID()));
            Assert.That(houndKey, Is.Not.EqualTo(M1VisualAssets.BattleRoot + "/ENEMY_GATE_GNAWER_A"));
        }

        private static void AssertSourcePngHasVisibleTransparency(string resourceKey)
        {
            var assetPath = ResourceAssetPath(resourceKey);
            Assert.That(File.Exists(assetPath), Is.True, assetPath);

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath), false), Is.True, assetPath);
                var pixels = texture.GetPixels32();
                Assert.That(pixels.Any(value => value.a == 0), Is.True,
                    assetPath + " needs transparent cutout pixels.");
                Assert.That(pixels.Any(value => value.a > 0), Is.True,
                    assetPath + " needs visible character pixels.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static string ReadMetaGuid(string resourceKey)
        {
            var metaPath = ResourceAssetPath(resourceKey) + ".meta";
            Assert.That(File.Exists(metaPath), Is.True, metaPath);
            var line = File.ReadLines(metaPath).FirstOrDefault(value =>
                value.StartsWith("guid: ", StringComparison.Ordinal));
            Assert.That(line, Is.Not.Null, metaPath + " is missing a GUID.");
            return line.Substring("guid: ".Length).Trim();
        }

        private static string ResourceAssetPath(string resourceKey) =>
            Path.Combine(Application.dataPath, "Resources", resourceKey.Replace('/', Path.DirectorySeparatorChar)) + ".png";
    }
}
