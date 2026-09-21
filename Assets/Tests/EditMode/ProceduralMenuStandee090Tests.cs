using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ProceduralMenuStandee090Tests
    {
        private const string IriId = "PROC_2D1D3685B3048514";
        private const string IriSeed = "5C7ADE46184E13CCE944F4AE";
        private const string EzzraId = "PROC_A0431F9383D6506D";
        private const string EzzraSeed = "E15F82083E6C18E69B106A2A";
        private const string Root = "SecondDimension/Art/Standees/Procedural090/";

        [TestCase(IriId, IriSeed, "DOG_TRIBE", "CLASS_ROGUE", "DOG_TRIBE_ROGUE_090")]
        [TestCase(EzzraId, EzzraSeed, "DEMON_HERITAGE", "CLASS_MAGE", "DEMON_HERITAGE_MAGE_090")]
        [TestCase(IriId, IriSeed, "RACE_DOG_TRIBE", "Rogue", "DOG_TRIBE_ROGUE_090")]
        [TestCase(EzzraId, EzzraSeed, "RACE_DEMON_HERITAGE", "Mage", "DEMON_HERITAGE_MAGE_090")]
        public void R16ProceduralApplicantsResolveCompatibleFullBodyMenuArtStably(
            string recruitId, string seed, string race, string role, string assetName)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                    recruitId, seed, race, null, role, out var sprite, out var key),
                Is.True, recruitId);
            Assert.That(key, Is.EqualTo(Root + assetName));
            Assert.That(sprite, Is.Not.Null);
            Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                    recruitId, seed, race, string.Empty, role,
                    out var repeatedSprite, out var repeatedKey), Is.True);
            Assert.That(repeatedKey, Is.EqualTo(key));
            Assert.That(repeatedSprite, Is.SameAs(sprite));

            // The menu improvement must not add this generic asset to combat's
            // identity lookup or silently invent an authored combat standee.
            Assert.That(M1VisualAssets.TryResolveBattleStandee(
                    recruitId, seed, race, null, out var battleSprite, out _), Is.False);
            Assert.That(battleSprite, Is.Null);
        }

        [TestCase("DOG_TRIBE", "CLASS_MAGE")]
        [TestCase("DOG_TRIBE", "CLASS_RANGER")]
        [TestCase("DOG_TRIBE", "CLASS_ROGUE_ARCHER")]
        [TestCase("DEMON_HERITAGE", "CLASS_ROGUE")]
        [TestCase("DEMON_HERITAGE", "CLASS_HEALER")]
        [TestCase("HUMAN", "CLASS_MAGE")]
        [TestCase("GOBLIN", "CLASS_ROGUE")]
        [TestCase("DOG_TRIBE", "")]
        public void UnsupportedRaceOrRoleDoesNotBorrowAnotherClassesArt(string race, string role)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                    IriId, IriSeed, race, null, role, out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [TestCase("SIGREC_UNAUTHORED_TEST", null)]
        [TestCase("HERO_REC_UNAUTHORED_TEST", null)]
        [TestCase(IriId, "SIGREC_UNAUTHORED_TEST")]
        [TestCase(IriId, EzzraId)]
        public void GenericCutoutsRequireProceduralIdentityWithoutAnotherPortraitAuthority(
            string recruitId, string authority)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                    recruitId, IriSeed, "DOG_TRIBE", authority, "CLASS_ROGUE",
                    out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [Test]
        public void SavedProceduralApplicantsUseTheirProductionProjectedAuthorityAndRenderArt()
        {
            // Identity fields are those of the R19 packaged Recruitment Desk.
            // Exercise save -> coordinator -> actual menu frame, rather than
            // assuming the projection leaves PortraitAuthorityId empty.
            var candidates = new[]
            {
                SavedApplicant090(1, IriId, "Iri Dustear", IriSeed, "DOG_TRIBE", "CLASS_ROGUE"),
                SavedApplicant090(2, EzzraId, "Ezzra Smokeveil", EzzraSeed, "DEMON_HERITAGE", "CLASS_MAGE")
            };
            var board = new ApplicantBoardState(
                "BOARD_PROCEDURAL_ART_090", "GC017D_RECRUITMENT_OP_0000_R_0000",
                0, true, candidates, string.Empty);
            var campaign = CampaignFactory.CreateM0Proof(90300);
            var city = campaign.Guild.GuildCity.With(
                recruitmentBoard: board, replaceRecruitmentBoard: true);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
            var savePath = Path.Combine(Path.GetTempPath(),
                "sd_procedural_menu_090_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                new AtomicSaveStore().Write(savePath,
                    SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
                var saveBytes = File.ReadAllBytes(savePath);
                var coordinator = new M1RuntimeCoordinator(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), savePath);
                foreach (var expected in candidates)
                {
                    var projected = coordinator.GuildCity017D.Applicants.Single(
                        value => value.RecruitId == expected.RecruitId);
                    Assert.That(projected.PortraitAuthorityId, Is.EqualTo(projected.RecruitId),
                        "A procedural applicant's production portrait authority is its own ID.");
                    Assert.That(projected.ClassTendencyId, Is.EqualTo(expected.ClassTendencyId));
                    Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                        projected.RecruitId, projected.VisualSeed, projected.RaceId,
                        projected.PortraitAuthorityId, projected.ClassTendencyId,
                        out var sprite, out var key), Is.True, projected.DisplayName);
                    Assert.That(key, Is.EqualTo(Root +
                        (expected.RecruitId == IriId ? "DOG_TRIBE_ROGUE_090" : "DEMON_HERITAGE_MAGE_090")));
                    Assert.That(sprite, Is.Not.Null);
                    var frameObject = new GameObject("Projected applicant frame", typeof(RectTransform), typeof(Image));
                    try
                    {
                        var populate = typeof(M1FlowPresenter).GetMethod(
                            "PopulateMenuStandeeFrame090", BindingFlags.Static | BindingFlags.NonPublic);
                        Assert.That(populate, Is.Not.Null);
                        populate.Invoke(null, new object[]
                        {
                            frameObject.GetComponent<Image>(), projected.RecruitId, projected.VisualSeed,
                            projected.RaceId, projected.PortraitAuthorityId, projected.DisplayName,
                            null, projected.ClassTendencyId
                        });
                        Assert.That(frameObject.GetComponentsInChildren<Image>(true).Any(value =>
                            value.name == "Standing Hero Sprite 090" && value.sprite == sprite), Is.True,
                            projected.DisplayName + " must show the actual sprite in the rendered menu frame.");
                        Assert.That(frameObject.GetComponentsInChildren<Text>(true).Any(value =>
                            value.name == "Standing Hero Fallback 090"), Is.False);
                    }
                    finally { Object.DestroyImmediate(frameObject); }
                }
                CollectionAssert.AreEqual(saveBytes, File.ReadAllBytes(savePath),
                    "Art projection must not rewrite the save or its identities.");
            }
            finally
            {
                foreach (var suffix in new[] { string.Empty, ".bak", ".tmp" })
                    if (File.Exists(savePath + suffix)) File.Delete(savePath + suffix);
            }
        }

        private static ApplicantSnapshotState SavedApplicant090(
            int slot, string id, string name, string seed, string race, string role) =>
            new ApplicantSnapshotState(slot, id, name, ApplicantKind.Procedural,
                seed, string.Empty, race, "SKYHOME", role, string.Empty,
                100, 100, 30, 30, 77, 607,
                "{\"visualSeed\":\"" + seed + "\",\"startingClassId\":\"" + role + "\"}",
                string.Empty, Array.Empty<EquipmentItemState>());

        [TestCase("PROC_36344E2400DC98B6", null, "DOG_TRIBE", "CLASS_ROGUE")]
        [TestCase("PROC_F85A4CAA747BC8C6", null, "DEMON_HERITAGE", "CLASS_MAGE")]
        [TestCase(IriId, "SIGREC_MAREN_HOLT", "DOG_TRIBE", "CLASS_ROGUE")]
        public void ExistingAuthoredStandeeWinsEvenWhenGenericPairMatches(
            string recruitId, string authority, string race, string role)
        {
            Assert.That(M1VisualAssets.TryResolveBattleStandee(
                    recruitId, string.Empty, race, authority,
                    out var authoredSprite, out var authoredKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveMenuStandee090(
                    recruitId, string.Empty, race, authority, role,
                    out var menuSprite, out var menuKey), Is.True);
            Assert.That(menuKey, Is.EqualTo(authoredKey));
            Assert.That(menuSprite, Is.SameAs(authoredSprite));
            Assert.That(menuKey, Does.Not.StartWith(Root));
        }

        [TestCase(IriId, IriSeed, "DOG_TRIBE", "CLASS_ROGUE", "Iri Dustear", true)]
        [TestCase(EzzraId, EzzraSeed, "DEMON_HERITAGE", "CLASS_MAGE", "Ezzra Smokeveil", true)]
        [TestCase(IriId, IriSeed, "DOG_TRIBE", "CLASS_RANGER", "Iri Dustear", false)]
        public void ActualMenuFrameUsesCompatibleArtAndKeepsInitialsForUnsupportedPairs(
            string recruitId, string seed, string race, string role, string name, bool hasArt)
        {
            var frameObject = new GameObject("Menu frame test", typeof(RectTransform), typeof(Image));
            try
            {
                var populate = typeof(M1FlowPresenter).GetMethod(
                    "PopulateMenuStandeeFrame090", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(populate, Is.Not.Null);
                populate.Invoke(null, new object[]
                {
                    frameObject.GetComponent<Image>(), recruitId, seed, race, null,
                    name, null, role
                });

                var backing = frameObject.transform.Find("Standee Backing 090");
                Assert.That(backing, Is.Not.Null);
                var artwork = backing.Find("Standing Hero Sprite 090");
                var fallback = backing.Find("Standing Hero Fallback 090");
                if (hasArt)
                {
                    Assert.That(artwork, Is.Not.Null);
                    Assert.That(artwork.GetComponent<Image>().sprite, Is.Not.Null);
                    Assert.That(artwork.GetComponent<Image>().preserveAspect, Is.True);
                    Assert.That(fallback, Is.Null);
                }
                else
                {
                    Assert.That(artwork, Is.Null);
                    Assert.That(fallback, Is.Not.Null);
                    Assert.That(fallback.GetComponent<Text>().text,
                        Does.Contain(M1VisualAssets.Initials(name)));
                }
            }
            finally
            {
                Object.DestroyImmediate(frameObject);
            }
        }
    }
}
