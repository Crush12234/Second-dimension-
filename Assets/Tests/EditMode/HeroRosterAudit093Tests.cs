using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Boot;
using SecondDimension.Gameplay.State;
using SecondDimension.Determinism;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRosterAudit093Tests
    {
        public static IEnumerable<int> EverySuppliedRosterId093 => Enumerable.Range(1, 300);

        [TestCase("1", "10", true)]
        [TestCase("1", "300", true)]
        [TestCase("301", "300", false)]
        [TestCase("10", "1", false)]
        [TestCase("0", "300", false)]
        [TestCase("1", "301", false)]
        [TestCase("invalid", "300", false)]
        public void MalformedOrEmptyRosterRangesCannotClaimSuccess093(string first, string last, bool expected)
        {
            Assert.That(HeroRosterBuiltPlayerAudit093.TryResolveRange093(first, last, out _, out _), Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(6)]
        public void CompositionFixtureCopiesRecruitObjectsWithoutChangingSourceOrGrantingProgression093(int count)
        {
            // State-copy regression only: these deliberately labeled test records
            // never resolve sprites or claim visual quality. Windows composition
            // proof separately requires the copied real R54 recruited save.
            var names = new[] { "Yves Thornfield", "Odelia Fen", "Vaelis Noct", "Tazren Warmask",
                "Maren Holt", "Gara Redtail", "Daeven Fellstar", "Jazzi Wirewick" };
            var recruits = names.Select((name, index) => new RecruitState("COMPOSITION_STATE_ONLY_" + index,
                123 + index, 123 + index, 30, 30, name, RecruitOriginKind.Procedural, string.Empty,
                "HUMAN", "WORLD_GATE_01", "CLASS_GUARDIAN", string.Empty, 5000,
                RecruitAuthorityKind.Normal, string.Empty, string.Empty, EquipmentLoadoutState.Empty(), true)).ToArray();
            var original = HeroRosterAudit093.CreateFixture093(HeroRosterAudit093.Catalog093.AcceptedHeroes
                .Single(value => value.StableId == "SIGREC_VAELIS_NOCT"));
            original = original.With(new GuildState("STATE_COPY_ONLY_093", 4321, recruits,
                Array.Empty<UnionState>()), original.OpeningFlow);
            var before = CanonicalJson.Sha256Hex(original);
            var fixture = HeroRosterBuiltPlayerAudit093.CreateCompositionFixture093(original, count);
            Assert.That(CanonicalJson.Sha256Hex(original), Is.EqualTo(before));
            Assert.That(fixture.Guild.TreasuryXp, Is.EqualTo(4321));
            Assert.That(fixture.Battle.PlayerUnions.Sum(union => union.Members.Count), Is.EqualTo(count));
            foreach (var recruit in fixture.Guild.Recruits)
            {
                Assert.That(recruit, Is.SameAs(original.Guild.Recruits.Single(value => value.RecruitId == recruit.RecruitId)));
                Assert.That(fixture.Guild.Unions.Count(union => union.MemberRecruitIds.Contains(recruit.RecruitId)), Is.EqualTo(1));
            }
            Assert.That(fixture.Battle.Reward, Is.Null, "A layout fixture cannot claim or invent battle rewards.");
            Assert.That(original.Battle, Is.Null);
        }

        [Test]
        public void CensusSeparatesEveryRowAndNeverCallsPlaceholdersFinishedArt093()
        {
            var rows = HeroRosterAudit093.Census093();
            Assert.That(rows.Count, Is.EqualTo(300));
            Assert.That(rows.Select(value => value.rosterId).Distinct().Count(), Is.EqualTo(300));
            Assert.That(rows.Count(value => value.eligibility == "ACCEPTED"), Is.EqualTo(250));
            Assert.That(rows.Count(value => value.eligibility == "QUARANTINED"), Is.EqualTo(50));
            Assert.That(rows.Where(value => value.eligibility == "QUARANTINED")
                .All(value => value.quarantineReasons.Length > 0 && value.MechanicalStatus == "BLOCKED_DATA"), Is.True);
            Assert.That(rows.All(value => !value.professionalArtReviewed && !value.runtimeUiVerified), Is.True,
                "A catalog census is neither a Windows UI run nor finished artwork proof.");
        }

        [TestCaseSource(nameof(EverySuppliedRosterId093))]
        public void EveryAcceptedHeroUsesRealSigningUnionForecastAndOwnSpriteAuthorities093(int rosterId)
        {
            var row = HeroRosterAudit093.Census093().Single(value => value.rosterId == rosterId);
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.SingleOrDefault(value => value.RosterId == rosterId);
            if (hero == null)
            {
                Assert.That(row.eligibility, Is.EqualTo("QUARANTINED"));
                Assert.That(row.MechanicalStatus, Is.EqualTo("BLOCKED_DATA"));
                Assert.That(row.recruited || row.ownIdentityBound || row.runtimeUiVerified, Is.False);
                return; // Correct rejection is not a mechanical or artwork pass for this hero.
            }
            var campaign = HeroRosterAudit093.CreateFixture093(hero);
            Assert.That(campaign.Guild.Recruits, Is.Empty, hero.StableId);
            Assert.That(campaign.Guild.TreasuryXp, Is.EqualTo(HeroRosterAudit093.FixtureTreasuryXp093));
            campaign = HeroRosterAudit093.SignAndPlace093(campaign, hero, row);
            Assert.That(campaign.Guild.Recruits.Single().AuthoredStableRecruitId, Is.EqualTo(hero.StableId));
            Assert.That(campaign.Guild.Recruits.Single().Progression.LearnedArtIds, Is.Not.Empty, hero.StableId);
            campaign = HeroRosterAudit093.StartBattleAndVerifyForecast093(campaign, row);
            HeroRosterAudit093.BindArt093(hero, row);
            Assert.That(row.MechanicalStatus, Is.EqualTo("PASS_MECHANICAL_ONLY"),
                Newtonsoft.Json.JsonConvert.SerializeObject(row, Newtonsoft.Json.Formatting.Indented));
            Assert.That(row.forecastCommands, Is.Not.Empty, hero.StableId);
            Assert.That(row.professionalArtReviewed, Is.False, "Pixels are not a professional art review.");
            Assert.That(row.runtimeUiVerified, Is.False, "EditMode must not claim built Windows UI evidence.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GeneratedFallbackFramesExactAlphaBeforeCpuPixelsAreDiscarded093(bool discardCpuPixels)
        {
            const int width = 192, height = 256;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.name = "ROSTER_PARITY_FIXTURE_093";
            var pixels = new Color32[width * height];
            for (var y = 29; y < 221; y++)
            for (var x = 47; x < 145; x++) pixels[y * width + x] = new Color32(80, 170, 255, 255);
            texture.SetPixels32(pixels);
            Sprite sprite = null;
            try
            {
                var finalize = typeof(M1VisualAssets).GetMethod("FinalizeHeroFallbackTexture093",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(finalize, Is.Not.Null);
                sprite = (Sprite)finalize.Invoke(null, new object[] { texture, discardCpuPixels });
                Assert.That(texture.isReadable, Is.EqualTo(!discardCpuPixels));
                var visible = M1SilhouetteFraming091.VisibleRect091(sprite);
                Assert.That(visible, Is.EqualTo(new Rect(47, 29, 98, 192)),
                    "Windows must retain the original alpha bounds after CPU discard, not its FullRect mesh.");
                Assert.That(sprite.rect, Is.EqualTo(new Rect(40, 22, 112, 206)));
                Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(sprite), Is.SameAs(sprite));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(sprite), Is.EqualTo(visible),
                    "Repeated menu/pose lookup must use the cached silhouette without pixel readback.");
            }
            finally
            {
                if (sprite != null) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [TestCase("HERO_REC_181")]
        [TestCase("HERO_REC_183")]
        [TestCase("HERO_REC_185")]
        public void IsolatedFallbackGeneratorDoesNotBakeAFloorShadow093(string stableId)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == stableId);
            // Explicit fallback-generator unit coverage, not a claim that these
            // live heroes still display placeholders. Public routing is tested below.
            var generator = typeof(M1VisualAssets).GetMethod("TryResolveHeroMasterSpriteFallback089",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(generator, Is.Not.Null);
            var poseType = generator.GetParameters()[1].ParameterType;
            foreach (var pose in new[] { "Standing", "Action" })
            {
                var args = new object[] { hero, Enum.Parse(poseType, pose), null, null };
                Assert.That((bool)generator.Invoke(null, args), Is.True);
                var sprite = (Sprite)args[2];
                Assert.That(M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089((string)args[3]), Is.True);
                var pixels = sprite.texture.GetPixels32();
                Assert.That(pixels.Take(sprite.texture.width * 18).All(pixel => pixel.a < 16), Is.True,
                    "The old y8–31 baked floor ellipse must not expand the body silhouette below its boots.");
                Assert.That(M1SilhouetteFraming091.VisibleRect091(sprite).yMin, Is.EqualTo(18f));
                Assert.That(pixels.Any(pixel => pixel.a == 112), Is.False,
                    "The rig's separately grounded contact shadow must not be duplicated in the cutout.");
            }
        }

        [TestCase("HERO_REC_181")]
        [TestCase("HERO_REC_183")]
        [TestCase("HERO_REC_185")]
        public void FormerFallbackHeroesNowUseTheirOwnOriginalPoses100(string stableId)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == stableId);
            Assert.That(M1VisualAssets.TryResolveBattleStandee(stableId, stableId, hero.Race, stableId,
                out var idle, out var idleKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose(stableId, stableId, hero.Race, stableId,
                out var action, out var actionKey), Is.True);
            var root = HeroRemasterAtlas093.Root093 + stableId + "_PAIR_093";
            Assert.That(idleKey, Is.EqualTo(root + "#IDLE"));
            Assert.That(actionKey, Is.EqualTo(root + "#ACTION"));
            Assert.That(idle.texture, Is.SameAs(action.texture));
            Assert.That(idle.rect, Is.Not.EqualTo(action.rect));
            Assert.That(M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(idleKey), Is.False);
            Assert.That(M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(actionKey), Is.False);
        }

        [TestCase("valid")]
        [TestCase("missing")]
        [TestCase("personal")]
        [TestCase("personal-child")]
        [TestCase("personal-parent")]
        [TestCase("relative")]
        public void BootRoutesAuditBeforeAnyCoordinatorLoadOrWriteAndFailsClosed093(string mode)
        {
            var root = Path.Combine(Path.GetTempPath(), "sd_roster_boot_093_" + Guid.NewGuid().ToString("N"));
            var personal = Path.Combine(root, "personal");
            var temporary = Path.Combine(root, "temporary");
            var evidence = Path.Combine(root, "evidence");
            var personalSave = Path.Combine(personal, "second_dimension_first_hour_slice_071.json");
            Directory.CreateDirectory(personal);
            try
            {
                var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == "SIGREC_VAELIS_NOCT");
                var fixture = HeroRosterAudit093.CreateFixture093(hero);
                HeroRosterAudit093.Write093(personalSave, fixture);
                File.WriteAllText(personalSave + ".bak", "Personal backup sentinel — must remain byte-identical.");
                var before = File.ReadAllBytes(personalSave);
                var backupBefore = File.ReadAllBytes(personalSave + ".bak");
                var output = mode == "personal" ? personal : mode == "personal-child" ? Path.Combine(personal, "nested") :
                    mode == "personal-parent" ? root : mode == "relative" ? "relative_roster_output" : evidence;
                var args = mode == "missing" ? new[] { "--sd-roster-audit-093" } :
                    new[] { "--sd-roster-audit-093", "--sd-roster-audit-output", output };
                var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(args, personal, temporary);
                Assert.That(bootSave.StartsWith(personal + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.False);
                Assert.That(bootSave, Is.EqualTo(mode == "valid"
                    ? Path.Combine(evidence, "FixtureSaves", "isolated_bootstrap_093.json") : bootSave));
                if (mode != "valid") Assert.That(bootSave.StartsWith(temporary + Path.DirectorySeparatorChar), Is.True);
                // Exercise actual coordinator load and production code-save write,
                // not just string checks on the selected path.
                HeroRosterAudit093.Write093(bootSave, fixture);
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, bootSave);
                var claimed = coordinator.RedeemCreatorCode028(hero.SsGenerationCode);
                Assert.That(claimed.Succeeded, Is.True, claimed.Message);
                Assert.That(HeroRosterAudit093.Read093(bootSave).Guild.Recruits.Count, Is.EqualTo(1));
                Assert.That(File.ReadAllBytes(personalSave), Is.EqualTo(before));
                Assert.That(File.ReadAllBytes(personalSave + ".bak"), Is.EqualTo(backupBefore));
                Assert.That(Directory.GetFiles(personal).Length, Is.EqualTo(2));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }
    }
}
