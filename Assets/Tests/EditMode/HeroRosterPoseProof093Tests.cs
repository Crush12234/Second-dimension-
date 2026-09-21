using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRosterPoseProof093Tests
    {
        [TestCase(12, "HERO_REC_012", true, false)]
        [TestCase(25, "HERO_REC_025", true, false)]
        [TestCase(29, "HERO_REC_029", true, false)]
        [TestCase(15, "HERO_REC_015", true, false)]
        [TestCase(19, "HERO_REC_019", true, false)]
        [TestCase(22, "HERO_REC_022", true, false)]
        [TestCase(46, "HERO_REC_046", true, false)]
        [TestCase(56, "HERO_REC_056", true, false)]
        [TestCase(66, "HERO_REC_066", true, false)]
        [TestCase(79, "HERO_REC_079", true, false)]
        [TestCase(69, "HERO_REC_069", true, false)]
        [TestCase(106, "HERO_REC_106", true, false)]
        [TestCase(169, "HERO_REC_169", true, true)]
        [TestCase(170, "HERO_REC_170", true, true)]
        [TestCase(171, "HERO_REC_171", true, true)]
        [TestCase(172, "HERO_REC_172", true, true)]
        [TestCase(173, "HERO_REC_173", true, true)]
        [TestCase(174, "HERO_REC_174", true, true)]
        [TestCase(175, "HERO_REC_175", true, true)]
        [TestCase(176, "HERO_REC_176", true, true)]
        [TestCase(177, "HERO_REC_177", true, true)]
        [TestCase(178, "HERO_REC_178", true, true)]
        [TestCase(179, "HERO_REC_179", true, true)]
        [TestCase(180, "HERO_REC_180", true, true)]
        [TestCase(181, "HERO_REC_181", true, true)]
        [TestCase(185, "HERO_REC_185", true, true)]
        [TestCase(184, "HERO_REC_184", true, false)]
        [TestCase(183, "HERO_REC_183", true, true)]
        [TestCase(181, "HERO_REC_181", false, true)]
        [TestCase(168, "HERO_REC_168", false, false)]
        [TestCase(186, "HERO_REC_186", true, false)]
        public void ReviewedPoseRangeNeverDisplaysUnreviewedPlaceholderRows093(int rosterId, string identity,
            bool reviewedOnly, bool expected)
        {
            Assert.That(HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(rosterId, identity, 169, 185,
                reviewedOnly), Is.EqualTo(expected));
        }

        [Test]
        public void WideReviewedSelectionContainsExactlyTheApprovedIdentities093()
        {
            var selected = HeroRosterAudit093.Catalog093.AcceptedHeroes.Where(hero =>
                HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(hero.RosterId, hero.StableId, 1, 300, true))
                .Select(hero => hero.StableId).OrderBy(id => id).ToArray();
            CollectionAssert.AreEqual(new[] {
                "HERO_REC_011",
                "HERO_REC_012",
                "HERO_REC_013",
                "HERO_REC_014",
                "HERO_REC_015",
                "HERO_REC_016",
                "HERO_REC_017",
                "HERO_REC_018",
                "HERO_REC_019",
                "HERO_REC_020",
                "HERO_REC_021",
                "HERO_REC_022",
                "HERO_REC_023",
                "HERO_REC_024",
                "HERO_REC_025",
                "HERO_REC_026",
                "HERO_REC_027",
                "HERO_REC_028",
                "HERO_REC_029",
                "HERO_REC_030",
                "HERO_REC_031",
                "HERO_REC_032",
                "HERO_REC_033",
                "HERO_REC_034",
                "HERO_REC_035",
                "HERO_REC_036",
                "HERO_REC_037",
                "HERO_REC_038",
                "HERO_REC_039",
                "HERO_REC_040",
                "HERO_REC_041",
                "HERO_REC_042",
                "HERO_REC_043",
                "HERO_REC_044",
                "HERO_REC_045",
                "HERO_REC_046",
                "HERO_REC_047",
                "HERO_REC_048",
                "HERO_REC_049",
                "HERO_REC_050",
                "HERO_REC_051",
                "HERO_REC_052",
                "HERO_REC_053",
                "HERO_REC_054",
                "HERO_REC_055",
                "HERO_REC_056",
                "HERO_REC_057",
                "HERO_REC_058",
                "HERO_REC_059",
                "HERO_REC_060",
                "HERO_REC_061",
                "HERO_REC_062",
                "HERO_REC_063",
                "HERO_REC_064",
                "HERO_REC_065",
                "HERO_REC_066",
                "HERO_REC_067",
                "HERO_REC_068",
                "HERO_REC_069",
                "HERO_REC_070",
                "HERO_REC_071",
                "HERO_REC_072",
                "HERO_REC_073",
                "HERO_REC_074",
                "HERO_REC_075",
                "HERO_REC_076",
                "HERO_REC_077",
                "HERO_REC_078",
                "HERO_REC_079",
                "HERO_REC_080",
                "HERO_REC_081",
                "HERO_REC_082",
                "HERO_REC_083",
                "HERO_REC_084",
                "HERO_REC_085",
                "HERO_REC_086",
                "HERO_REC_087",
                "HERO_REC_088",
                "HERO_REC_089",
                "HERO_REC_090",
                "HERO_REC_091",
                "HERO_REC_092",
                "HERO_REC_093",
                "HERO_REC_094",
                "HERO_REC_095",
                "HERO_REC_096",
                "HERO_REC_097",
                "HERO_REC_098",
                "HERO_REC_099",
                "HERO_REC_100",
                "HERO_REC_101",
                "HERO_REC_102",
                "HERO_REC_103",
                "HERO_REC_104",
                "HERO_REC_105",
                "HERO_REC_106",
                "HERO_REC_107",
                "HERO_REC_108",
                "HERO_REC_109",
                "HERO_REC_110",
                "HERO_REC_169",
                "HERO_REC_170",
                "HERO_REC_171",
                "HERO_REC_172",
                "HERO_REC_173",
                "HERO_REC_174",
                "HERO_REC_175",
                "HERO_REC_176",
                "HERO_REC_177",
                "HERO_REC_178",
                "HERO_REC_179",
                "HERO_REC_180",
                "HERO_REC_181",
                "HERO_REC_183",
                "HERO_REC_185",
                "HERO_REC_186",
                "HERO_REC_187",
                "HERO_REC_188",
                "HERO_REC_189",
                "HERO_REC_191",
                "HERO_REC_192",
                "HERO_REC_193",
                "HERO_REC_195",
                "HERO_REC_196",
                "HERO_REC_197",
                "HERO_REC_198",
                "HERO_REC_219",
                "HERO_REC_220",
                "HERO_REC_221",
                "HERO_REC_222",
                "HERO_REC_223",
                "HERO_REC_225",
                "HERO_REC_228",
                "HERO_REC_232",
                "HERO_REC_233",
                "HERO_REC_234",
                "HERO_REC_236",
                "HERO_REC_237",
                "HERO_REC_238",
                "HERO_REC_239",
                "HERO_REC_240",
                "HERO_REC_241",
                "HERO_REC_245",
                "HERO_REC_248",
                "HERO_REC_249",
                "HERO_REC_250",
                "HERO_REC_253",
                "HERO_REC_256",
                "HERO_REC_257",
                "HERO_REC_258",
                "HERO_REC_259",
                "HERO_REC_260",
                "HERO_REC_261",
                "HERO_REC_262",
                "HERO_REC_263",
                "HERO_REC_265",
                "HERO_REC_266",
                "HERO_REC_267",
                "HERO_REC_268",
                "HERO_REC_269",
                "HERO_REC_270",
                "HERO_REC_271",
                "HERO_REC_272",
                "HERO_REC_274",
                "HERO_REC_275",
                "HERO_REC_276",
                "HERO_REC_277",
                "HERO_REC_278",
                "HERO_REC_279",
                "HERO_REC_280",
                "HERO_REC_281",
                "HERO_REC_282",
                "HERO_REC_284",
                "HERO_REC_285",
                "HERO_REC_286",
                "HERO_REC_288",
                "HERO_REC_289",
                "HERO_REC_290",
                "HERO_REC_292",
                "HERO_REC_293",
                "HERO_REC_294",
                "HERO_REC_295",
                "HERO_REC_296",
                "HERO_REC_297",
                "HERO_REC_298",
                "HERO_REC_299",
                "HERO_REC_300"
            }, selected);
            Assert.That(HeroRosterAudit093.Catalog093.AcceptedHeroes.Any(hero =>
                HeroRosterBuiltPlayerAudit093.IncludesHeroForCapture093(hero.RosterId, hero.StableId, 184, 184, true)), Is.False,
                "A reviewed-only range with no approved identity must not silently fall back to placeholders.");
        }

        [TestCase("HERO_REC_012")]
        [TestCase("HERO_REC_025")]
        [TestCase("HERO_REC_029")]
        [TestCase("HERO_REC_015")]
        [TestCase("HERO_REC_019")]
        [TestCase("HERO_REC_022")]
        [TestCase("HERO_REC_046")]
        [TestCase("HERO_REC_056")]
        [TestCase("HERO_REC_066")]
        [TestCase("HERO_REC_079")]
        [TestCase("HERO_REC_069")]
        [TestCase("HERO_REC_106")]
        [TestCase("HERO_REC_169")]
        [TestCase("HERO_REC_170")]
        [TestCase("HERO_REC_171")]
        [TestCase("HERO_REC_172")]
        [TestCase("HERO_REC_173")]
        [TestCase("HERO_REC_174")]
        [TestCase("HERO_REC_175")]
        [TestCase("HERO_REC_176")]
        [TestCase("HERO_REC_177")]
        [TestCase("HERO_REC_178")]
        [TestCase("HERO_REC_179")]
        [TestCase("HERO_REC_180")]
        public void PoseProofRejectsWrongHalfEvenWhenTextureIdentityMatches093(string identity)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var idle, out var idleKey), Is.True);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out var actionKey), Is.True);
            Assert.That(idle.texture, Is.SameAs(action.texture));
            var host = new GameObject("Real pose Image proof", typeof(RectTransform), typeof(Image));
            Sprite entireAtlas = null;
            try
            {
                var image = host.GetComponent<Image>(); image.preserveAspect = true; image.sprite = idle;
                var before = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, identity, "IDLE", false, false);
                Assert.That(before.exactCellBound && before.neighborCellExcluded, Is.True);
                Assert.That(before.expectedResource, Is.EqualTo(idleKey));
                Assert.That(before.campaignUnchanged, Is.False, "Only the actual runtime coordinator can establish unchanged state.");
                image.sprite = action;
                Assert.Throws<InvalidOperationException>(() => HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(
                    image, identity, "IDLE", false, false), "Checking only texture identity would incorrectly accept the other pose.");
                var active = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, identity, "ACTION", true, false);
                Assert.That(active.expectedResource, Is.EqualTo(actionKey));
                Assert.That(active.spriteRect, Is.Not.EqualTo(before.spriteRect));
                Assert.Throws<InvalidOperationException>(() => HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(
                    image, identity, "ACTION", true, true), "Editor pixels remain readable; this is not Windows CPU-discard proof.");
                entireAtlas = Sprite.Create(idle.texture, new Rect(0, 0, idle.texture.width, idle.texture.height), Vector2.one * 0.5f);
                image.sprite = entireAtlas;
                Assert.Throws<InvalidOperationException>(() => HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(
                    image, identity, "IDLE", false, false), "Never pass an Image displaying both neighboring poses.");
                image.sprite = idle;
                var restored = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, identity, "IDLE RESTORED", false, false);
                Assert.That(restored.spriteRect, Is.EqualTo(before.spriteRect));
                Assert.That(restored.expectedResource, Is.EqualTo(idleKey));
            }
            finally { if (entireAtlas != null) Object.DestroyImmediate(entireAtlas); Object.DestroyImmediate(host); }
        }
        [TestCase("both", true, "HERO_REC_180", true, true)]
        [TestCase("main", true, "HERO_REC_180", true, false)]
        [TestCase("keep", true, "HERO_REC_180", true, false)]
        [TestCase("none", true, "HERO_REC_180", true, false)]
        [TestCase("both", false, "HERO_REC_180", true, false)]
        [TestCase("both", true, "HERO_REC_180", false, false)]
        [TestCase("both", true, "HERO_REC_181", true, true)]
        [TestCase("both", true, "HERO_REC_184", true, false)]
        [TestCase("both", true, "UNKNOWN", true, false)]
        [TestCase("null", true, "HERO_REC_180", true, false)]
        public void KeepOpenIsExplicitAndOnlyAvailableForSuccessfulLiveReviewedBattle093(string flags,
            bool successful, string lastIdentity, bool live, bool expected)
        {
            var arguments = flags == "both" ? new[] { "--sd-roster-audit-093", "--sd-roster-audit-keep-open" } :
                flags == "main" ? new[] { "--sd-roster-audit-093" } : flags == "keep"
                    ? new[] { "--sd-roster-audit-keep-open" } : flags == "null" ? null : Array.Empty<string>();
            Assert.That(HeroRosterBuiltPlayerAudit093.CanKeepReviewedBattleOpen093(arguments, successful, lastIdentity, live),
                Is.EqualTo(expected));
        }

        [Test]
        public void PoseCampaignHashReadsSavedAuthorityNotMutablePresentationSnapshot093()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd_pose_authority_093_" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "isolated_fixture.json");
            Directory.CreateDirectory(root);
            try
            {
                var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == "SIGREC_VAELIS_NOCT");
                var campaign = HeroRosterAudit093.CreateFixture093(hero);
                HeroRosterAudit093.Write093(path, campaign);
                var coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, path);
                var savedHash = CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path));
                Assert.That(HeroRosterBuiltPlayerAudit093.CoordinatorCampaignHash093(coordinator), Is.EqualTo(savedHash));
                var snapshot = coordinator.State;
                snapshot.StatusMessage = "DISPLAY-ONLY CHANGE";
                snapshot.TreasuryXp = -777;
                snapshot.CanonicalStateHash = "MUTABLE DTO MUST NOT BE AUTHORITY";
                Assert.That(HeroRosterBuiltPlayerAudit093.CoordinatorCampaignHash093(coordinator), Is.EqualTo(savedHash),
                    "Read the fresh coordinator-derived canonical hash; never hash or trust a modified old DTO.");
                Assert.That(coordinator.RedeemCreatorCode028(hero.SsGenerationCode).Succeeded, Is.True);
                var updatedHash = HeroRosterBuiltPlayerAudit093.CoordinatorCampaignHash093(coordinator);
                Assert.That(updatedHash, Is.Not.EqualTo(savedHash), "A real authorized command must change the authority proof.");
                Assert.That(updatedHash, Is.EqualTo(CanonicalJson.Sha256Hex(HeroRosterAudit093.Read093(path))));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
        }
    }
}
