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
    public sealed class ArtCoverage070Tests
    {
        private static readonly string[] AuthoredPortraitStandeeIdentities =
        {
            "SIGREC_MAREN_HOLT",
            "SIGREC_ODELIA_FEN",
            "SIGREC_BRAKKA_EMBERWALL",
            "SIGREC_VEYRA_ASHGLASS",
            "SIGREC_TALA_STORMROAD",
            "SIGREC_ORREN_CLAY",
            "SIGREC_ANSEL_WINTERGLASS",
            "SIGREC_TOVVI_COPPERSPARK",
            "SIGREC_RUSK_FENRUNNER",
            "SIGREC_BESSA_BRASSWHISTLE",
            "SIGREC_VAELIS_NOCT"
        };

        private static readonly string[] AuthoredActionIdentities =
        {
            "SIGREC_MAREN_HOLT",
            "SIGREC_ODELIA_FEN",
            "SIGREC_BRAKKA_EMBERWALL",
            "SIGREC_VEYRA_ASHGLASS",
            "SIGREC_TALA_STORMROAD",
            "SIGREC_ORREN_CLAY",
            "SIGREC_ANSEL_WINTERGLASS",
            "SIGREC_TOVVI_COPPERSPARK",
            "SIGREC_RUSK_FENRUNNER",
            "SIGREC_BESSA_BRASSWHISTLE",
            "SIGREC_VAELIS_NOCT"
        };

        private static readonly HashSet<string> HeroMasterSpriteDossierIdentities089 =
            new HashSet<string>(new[]
            {
                "SIGREC_MAREN_HOLT",
                "SIGREC_BRAKKA_EMBERWALL",
                "SIGREC_ODELIA_FEN",
                "SIGREC_TOVVI_COPPERSPARK",
                "SIGREC_RUSK_FENRUNNER",
                "SIGREC_TALA_STORMROAD",
                "SIGREC_ORREN_CLAY",
                "SIGREC_BESSA_BRASSWHISTLE",
                "SIGREC_VAELIS_NOCT"
            }, StringComparer.Ordinal);

        [Test]
        public void AuthoredIdentitySliceHasDistinctPortraitsAndBattleStandees()
        {
            var portraitKeys = new List<string>();
            var standeeKeys = new List<string>();
            var portraitTextures = new List<int>();
            var standeeTextures = new List<int>();

            for (var index = 0; index < AuthoredPortraitStandeeIdentities.Length; index++)
            {
                var recruitId = AuthoredPortraitStandeeIdentities[index];
                var expectedStandeeKey = M1VisualAssets.BattleRoot + "/STANDEE_" + recruitId;
                var usesSpriteDossier089 = HeroMasterSpriteDossierIdentities089.Contains(recruitId);
                var expectedPortraitKey = usesSpriteDossier089
                    ? expectedStandeeKey
                    : M1VisualAssets.PortraitRoot + "/Recruits/" + recruitId;

                Assert.That(M1VisualAssets.TryResolvePortrait(
                        "RUNTIME_INSTANCE_" + index,
                        "VISUAL_SEED_" + index,
                        "HUMAN",
                        recruitId,
                        out var portrait,
                        out var portraitKey),
                    Is.True,
                    recruitId + " portrait did not resolve through permanent authority.");
                Assert.That(M1VisualAssets.TryResolveBattleStandee(
                        "RUNTIME_INSTANCE_" + index,
                        "VISUAL_SEED_" + index,
                        "HUMAN",
                        recruitId,
                        out var standee,
                        out var standeeKey),
                    Is.True,
                    recruitId + " standee did not resolve through permanent authority.");

                Assert.That(portrait, Is.Not.Null, recruitId + " portrait is missing.");
                Assert.That(standee, Is.Not.Null, recruitId + " standee is missing.");
                Assert.That(portraitKey, Is.EqualTo(expectedPortraitKey), recruitId);
                Assert.That(standeeKey, Is.EqualTo(expectedStandeeKey), recruitId);
                Assert.That(portrait.texture.width, Is.EqualTo(1024), recruitId + " dossier width changed.");
                Assert.That(portrait.texture.height, Is.EqualTo(usesSpriteDossier089 ? 1536 : 1024),
                    recruitId + " dossier height changed.");
                Assert.That(standee.texture.width, Is.EqualTo(1024), recruitId + " standee width changed.");
                Assert.That(standee.texture.height, Is.EqualTo(1536), recruitId + " standee height changed.");

                portraitKeys.Add(portraitKey);
                standeeKeys.Add(standeeKey);
                portraitTextures.Add(portrait.texture.GetInstanceID());
                standeeTextures.Add(standee.texture.GetInstanceID());
            }

            Assert.That(portraitKeys.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(AuthoredPortraitStandeeIdentities.Length));
            Assert.That(standeeKeys.Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(AuthoredPortraitStandeeIdentities.Length));
            Assert.That(portraitTextures.Distinct().Count(), Is.EqualTo(AuthoredPortraitStandeeIdentities.Length));
            Assert.That(standeeTextures.Distinct().Count(), Is.EqualTo(AuthoredPortraitStandeeIdentities.Length));
        }

        [Test]
        public void BoardAndApplicantPolish086_LoadsDistinctProductionArtFromResources()
        {
            var boardKeys = new[]
            {
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.GuildUndercroftLedgerBackdropResource086,
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.LanternRoadCacheBackdropResource086,
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.WayglassThresholdBackdropResource086,
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.BrokenSurveyBridgeBackdropResource086,
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.LostSurveyCampBackdropResource086,
                SecondDimension.Presentation.GuildCity017D.ExpeditionBoardProjection074.UnrecordedDoorBackdropResource086
            };
            var boardTextures = boardKeys
                .Select(key => Resources.Load<Texture2D>(key))
                .ToArray();
            Assert.That(boardTextures, Has.All.Not.Null);
            Assert.That(boardTextures.Select(value => value.GetInstanceID()).Distinct().Count(),
                Is.EqualTo(boardKeys.Length));
            Assert.That(boardTextures.All(value => value.width == 1672 && value.height == 941),
                Is.True);

            var races = new[]
            {
                "HUMAN", "ORC", "GOBLIN", "DARK_ELF", "DOG_TRIBE", "BUNNY_TRIBE",
                "DEMON_HERITAGE"
            };
            var portraitTextureIds = new HashSet<int>();
            foreach (var race in races)
            {
                var path = M1VisualAssets.PortraitRoot + "/Races/" + race;
                var textures = Resources.LoadAll<Texture2D>(path)
                    .Where(value => value != null)
                    .OrderBy(value => value.name, StringComparer.Ordinal)
                    .ToArray();
                Assert.That(textures.Length, Is.GreaterThanOrEqualTo(2), race);
                Assert.That(textures.All(value => value.width == 1024 && value.height == 1536),
                    Is.True, race);
                foreach (var texture in textures) portraitTextureIds.Add(texture.GetInstanceID());

                var resolvedKeys = new[] { "A", "B", "C", "D", "E", "F", "G", "H" }
                    .Select(seed =>
                    {
                        Assert.That(M1VisualAssets.TryResolvePortrait(
                            "APPLICANT_" + race + "_" + seed,
                            race + "_VISUAL_086_" + seed,
                            race,
                            string.Empty,
                            out var portrait,
                            out var key), Is.True, race + " / " + seed);
                        Assert.That(portrait, Is.Not.Null);
                        return key;
                    })
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                Assert.That(resolvedKeys.Length, Is.GreaterThanOrEqualTo(2),
                    race + " applicant seeds collapsed to one face.");
            }
            Assert.That(portraitTextureIds.Count, Is.GreaterThanOrEqualTo(16));
        }

        [TestCase("ORC", "Restoration Mystic", "RESTORATION_MYSTIC")]
        [TestCase("ORC", "Shield Guardian", "SHIELD_GUARDIAN")]
        [TestCase("GOBLIN", "Field Engineer", "FIELD_ENGINEER")]
        [TestCase("GOBLIN", "Storm Mystic", "STORM_MYSTIC")]
        [TestCase("HUMAN", "Aether Mage", "AETHER_MAGE")]
        [TestCase("HUMAN", "Ranger", "HUMAN_RANGER")]
        [TestCase("DEMON_HERITAGE", "Restoration Priest", "RESTORATION_PRIEST")]
        [TestCase("DEMON_HERITAGE", "Shield Guardian", "SHIELD_GUARDIAN")]
        [TestCase("DEMON_HERITAGE", "Greatsword Vanguard", "GREATSWORD_VANGUARD")]
        [TestCase("DEMON_HERITAGE", "Bow Ranger", "BOW_RANGER")]
        public void ApplicantPortraits086_MatchTheVisibleRoleWhenThatRaceHasAnAuthoredOption(
            string race,
            string role,
            string expectedPortraitToken)
        {
            Assert.That(M1VisualAssets.TryResolvePortrait(
                "ROLE_MATCH_APPLICANT_086_" + race,
                "ROLE_MATCH_SEED_086_" + race,
                race,
                string.Empty,
                role,
                out var portrait,
                out var key), Is.True);
            Assert.That(portrait, Is.Not.Null);
            StringAssert.Contains(expectedPortraitToken, key);
        }

        [Test]
        public void AuthoredActionsResolveThroughStableAuthorityInsteadOfRuntimeInstanceIds()
        {
            var descriptor = M1VisualAssets.BuildPortraitDescriptor(
                "SIGI_RUNTIME_INSTANCE_070",
                "ANSEL_RUNTIME_SEED_070",
                "HUMAN",
                "SIGREC_ANSEL_WINTERGLASS");
            Assert.That(descriptor.IsBespokeSignature, Is.True);
            Assert.That(descriptor.ResourceKey, Is.EqualTo(
                M1VisualAssets.PortraitRoot + "/Recruits/SIGREC_ANSEL_WINTERGLASS"));

            var actionTextures = new List<int>();
            for (var index = 0; index < AuthoredActionIdentities.Length; index++)
            {
                var recruitId = AuthoredActionIdentities[index];
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(
                        "SIGI_RUNTIME_INSTANCE_070_" + index,
                        "RUNTIME_VISUAL_SEED_070_" + index,
                        "HUMAN",
                        recruitId,
                        out var action,
                        out var actionKey),
                    Is.True,
                    recruitId + " action art did not resolve through stable portrait authority.");

                Assert.That(action, Is.Not.Null);
                Assert.That(actionKey, Is.EqualTo(M1VisualAssets.BattleRoot + "/ACTION_" + recruitId));
                Assert.That(action.texture.width, Is.EqualTo(1024), recruitId);
                Assert.That(action.texture.height, Is.EqualTo(1536), recruitId);
                actionTextures.Add(action.texture.GetInstanceID());
            }

            Assert.That(actionTextures.Distinct().Count(), Is.EqualTo(AuthoredActionIdentities.Length));
        }

        [Test]
        public void AuthoredIdentitySliceHasUniqueValidMetadataAndTransparentBattleCutouts()
        {
            var portraitKeys = AuthoredPortraitStandeeIdentities.Select(value =>
                M1VisualAssets.PortraitRoot + "/Recruits/" + value);
            var battleKeys = AuthoredPortraitStandeeIdentities.Select(value =>
                    M1VisualAssets.BattleRoot + "/STANDEE_" + value)
                .Concat(AuthoredActionIdentities.Select(value =>
                    M1VisualAssets.BattleRoot + "/ACTION_" + value))
                .ToArray();
            var allKeys = portraitKeys.Concat(battleKeys).ToArray();
            var guids = allKeys.Select(ReadMetaGuid).ToArray();

            Assert.That(guids.All(value => Regex.IsMatch(
                value ?? string.Empty,
                "^[0-9a-f]{32}$",
                RegexOptions.CultureInvariant)), Is.True);
            Assert.That(guids.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(allKeys.Length),
                "Every V70 identity asset must have its own Unity GUID.");

            foreach (var resourceKey in battleKeys)
            {
                var meta = File.ReadAllText(ResourceAssetPath(resourceKey) + ".meta");
                Assert.That(meta, Does.Contain("alphaIsTransparency: 1"), resourceKey);
                Assert.That(meta, Does.Contain("nPOTScale: 0"),
                    resourceKey + " must preserve its authored 1024x1536 silhouette and aspect ratio.");
                AssertSourcePngHasVisibleTransparency(resourceKey);
            }
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
                    assetPath + " needs transparent background pixels.");
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
