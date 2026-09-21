using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourCharacterArt071Tests
    {
        private static readonly string[] PlayableRosterIds =
        {
            "PROC_36344E2400DC98B6",
            "PROC_F85A4CAA747BC8C6",
            "PROC_5B14E7816E55FFB5",
            "PROC_748DD03A23E1FEB0",
            "SIGREC_MAREN_HOLT",
            "SIGREC_ODELIA_FEN",
            "SIGREC_TALA_STORMROAD",
            "SIGREC_ORREN_CLAY",
            "SIGREC_BESSA_BRASSWHISTLE",
            "SIGREC_VAELIS_NOCT",
            "SIGREC_ZORIN_BRAMBLECROSS",
            "SIGREC_UNA_QUEENSREST",
            "SIGREC_JUNIA_SKYWARD",
            "SIGREC_DAIN_DEEPWELL",
            "SIGREC_WILLOW_LONGSTRIDE",
            "SIGREC_QUIN_LOWEN",
            "SIGREC_ASTER_MARSHLIGHT",
            "SIGREC_PETRA_RUNEBROOK",
            "SIGREC_QUIN_CROWNHILL",
            "SIGREC_YVES_THORNFIELD"
        };

        private static readonly string[] LanternPatrolPortraitIds076 =
        {
            "SIGREC_ZORIN_BRAMBLECROSS",
            "SIGREC_UNA_QUEENSREST",
            "SIGREC_JUNIA_SKYWARD",
            "SIGREC_DAIN_DEEPWELL",
            "SIGREC_WILLOW_LONGSTRIDE",
            "SIGREC_QUIN_LOWEN",
            "SIGREC_ASTER_MARSHLIGHT",
            "SIGREC_PETRA_RUNEBROOK",
            "SIGREC_QUIN_CROWNHILL",
            "SIGREC_YVES_THORNFIELD"
        };

        [Test]
        public void AllTwentyPlayableMembersResolvePortraitStandeeAndActionArtwork()
        {
            var portraitKeys = new HashSet<string>(StringComparer.Ordinal);
            var standeeKeys = new HashSet<string>(StringComparer.Ordinal);
            var actionKeys = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(PlayableRosterIds, Has.Length.EqualTo(20));
            foreach (var recruitId in PlayableRosterIds)
            {
                Assert.That(M1VisualAssets.TryResolvePortrait(
                        recruitId, string.Empty, string.Empty, recruitId,
                        out var portrait, out var portraitKey),
                    Is.True, recruitId + " portrait");
                Assert.That(portrait, Is.Not.Null, recruitId + " portrait");
                Assert.That(portraitKeys.Add(portraitKey), Is.True,
                    recruitId + " must not share a portrait resource key.");

                Assert.That(M1VisualAssets.TryResolveBattleStandee(
                        recruitId, string.Empty, string.Empty, recruitId,
                        out var standee, out var standeeKey),
                    Is.True, recruitId + " standee");
                Assert.That(standee, Is.Not.Null, recruitId + " standee");
                Assert.That(standeeKeys.Add(standeeKey), Is.True,
                    recruitId + " must not share a standee resource key.");

                Assert.That(M1VisualAssets.TryResolveBattleActionPose(
                        recruitId, string.Empty, string.Empty, recruitId,
                        out var action, out var actionKey),
                    Is.True, recruitId + " action pose");
                Assert.That(action, Is.Not.Null, recruitId + " action pose");
                Assert.That(actionKeys.Add(actionKey), Is.True,
                    recruitId + " must not share an action resource key.");
            }

            Assert.That(portraitKeys, Has.Count.EqualTo(20));
            Assert.That(standeeKeys, Has.Count.EqualTo(20));
            Assert.That(actionKeys, Has.Count.EqualTo(20));
        }

        [Test]
        public void LanternPatrolUsesTenVersionedStudioPortraitsWithoutUniformTemplateFallback076()
        {
            var resourceKeys = new HashSet<string>(StringComparer.Ordinal);
            var assetGuids = new HashSet<string>(StringComparer.Ordinal);
            var sourceHashes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var recruitId in LanternPatrolPortraitIds076)
            {
                var expectedKey = M1VisualAssets.FirstHourPortraitRoot076 + "/" +
                                  recruitId + "_PORTRAIT_076";
                Assert.That(M1VisualAssets.TryResolvePortrait(
                        "RUNTIME_" + recruitId,
                        "SEED_" + recruitId,
                        string.Empty,
                        recruitId,
                        out var portrait,
                        out var resourceKey),
                    Is.True,
                    recruitId + " release portrait");
                Assert.That(resourceKey, Is.EqualTo(expectedKey), recruitId);
                Assert.That(resourceKeys.Add(resourceKey), Is.True, recruitId);
                Assert.That(portrait, Is.Not.Null, recruitId);
                Assert.That(portrait.texture.width, Is.EqualTo(1254), recruitId);
                Assert.That(portrait.texture.height, Is.EqualTo(1254), recruitId);

                var assetPath = "Assets/Resources/" + expectedKey + ".png";
                Assert.That(assetGuids.Add(AssetDatabase.AssetPathToGUID(assetPath)), Is.True,
                    recruitId + " must have a unique Unity asset GUID.");
                using (var stream = File.OpenRead(Path.GetFullPath(assetPath)))
                using (var sha = SHA256.Create())
                {
                    var hash = BitConverter.ToString(sha.ComputeHash(stream));
                    Assert.That(sourceHashes.Add(hash), Is.True,
                        recruitId + " must not be a renamed copy of another portrait.");
                }

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                Assert.That(importer, Is.Not.Null, assetPath);
                Assert.That(importer.mipmapEnabled, Is.False, assetPath);
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None),
                    assetPath + " must preserve the 1254-square authored composition.");
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), assetPath);
                var standalone = importer.GetPlatformTextureSettings("Standalone");
                Assert.That(standalone.overridden, Is.True, assetPath);
                Assert.That(standalone.textureCompression,
                    Is.EqualTo(TextureImporterCompression.Uncompressed), assetPath);
            }

            Assert.That(resourceKeys, Has.Count.EqualTo(10));
            Assert.That(assetGuids, Has.Count.EqualTo(10));
            Assert.That(sourceHashes, Has.Count.EqualTo(10));
        }

        [Test]
        public void LanternPatrolUsesTenMatchedVersionedBattleIdentitiesWithRealAlpha076()
        {
            var standeeKeys = new HashSet<string>(StringComparer.Ordinal);
            var actionKeys = new HashSet<string>(StringComparer.Ordinal);
            var assetGuids = new HashSet<string>(StringComparer.Ordinal);
            var sourceHashes = new HashSet<string>(StringComparer.Ordinal);

            foreach (var recruitId in LanternPatrolPortraitIds076)
            {
                var expectedStandeeKey = M1VisualAssets.FirstHourBattleRoot076 + "/STANDEE_" +
                                         recruitId + "_076";
                Assert.That(M1VisualAssets.TryResolveBattleStandee(
                        "RUNTIME_" + recruitId,
                        "SEED_" + recruitId,
                        string.Empty,
                        recruitId,
                        out var standee,
                        out var standeeKey),
                    Is.True,
                    recruitId + " Release 076 standee");
                Assert.That(standeeKey, Is.EqualTo(expectedStandeeKey), recruitId);
                Assert.That(standeeKeys.Add(standeeKey), Is.True, recruitId);
                AssertBattleCutout076(
                    recruitId + " standee",
                    standee,
                    expectedStandeeKey,
                    assetGuids,
                    sourceHashes);

                var expectedActionKey = M1VisualAssets.FirstHourBattleRoot076 + "/ACTION_" +
                                        recruitId + "_076";
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(
                        "RUNTIME_" + recruitId,
                        "SEED_" + recruitId,
                        string.Empty,
                        recruitId,
                        out var action,
                        out var actionKey),
                    Is.True,
                    recruitId + " Release 076 action");
                Assert.That(actionKey, Is.EqualTo(expectedActionKey), recruitId);
                Assert.That(actionKeys.Add(actionKey), Is.True, recruitId);
                AssertBattleCutout076(
                    recruitId + " action",
                    action,
                    expectedActionKey,
                    assetGuids,
                    sourceHashes);
            }

            Assert.That(standeeKeys, Has.Count.EqualTo(10));
            Assert.That(actionKeys, Has.Count.EqualTo(10));
            Assert.That(assetGuids, Has.Count.EqualTo(20));
            Assert.That(sourceHashes, Has.Count.EqualTo(20),
                "No standee or action may be a renamed duplicate of another battle cutout.");
        }

        private static void AssertBattleCutout076(
            string label,
            Sprite sprite,
            string resourceKey,
            ISet<string> assetGuids,
            ISet<string> sourceHashes)
        {
            Assert.That(sprite, Is.Not.Null, label);
            Assert.That(sprite.texture.width, Is.GreaterThanOrEqualTo(850), label);
            Assert.That(sprite.texture.height, Is.GreaterThanOrEqualTo(1500), label);
            Assert.That(sprite.texture.height, Is.GreaterThan(sprite.texture.width), label);

            var assetPath = "Assets/Resources/" + resourceKey + ".png";
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            Assert.That(guid, Is.Not.Empty, assetPath);
            Assert.That(assetGuids.Add(guid), Is.True,
                label + " must have a unique Unity asset GUID.");

            var bytes = File.ReadAllBytes(Path.GetFullPath(assetPath));
            Assert.That(bytes, Has.Length.GreaterThan(26), assetPath);
            Assert.That(bytes[25], Is.EqualTo(6),
                assetPath + " must be a true RGBA PNG, not a painted checkerboard RGB image.");
            using (var stream = new MemoryStream(bytes, false))
            using (var sha = SHA256.Create())
            {
                var hash = BitConverter.ToString(sha.ComputeHash(stream));
                Assert.That(sourceHashes.Add(hash), Is.True,
                    label + " must not duplicate another battle image.");
            }

            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, assetPath);
            Assert.That(importer.mipmapEnabled, Is.False, assetPath);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), assetPath);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), assetPath);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), assetPath);
            Assert.That(importer.alphaIsTransparency, Is.True, assetPath);
            Assert.That(importer.maxTextureSize, Is.EqualTo(2048), assetPath);
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            Assert.That(standalone.overridden, Is.True, assetPath);
            Assert.That(standalone.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed), assetPath);
            Assert.That(standalone.maxTextureSize, Is.EqualTo(2048), assetPath);
        }
    }
}
