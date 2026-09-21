using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class VisualIdentityRemaster091Tests
    {
        private const string ProceduralRoot = "SecondDimension/Art/Standees/Procedural091/";
        private const string FreyaKey = "SecondDimension/Art/Standees/HeroRemaster091/HERO_REC_287_IDLE_091";

        // Exact identities and equipment families observed in the R26 production save.
        [TestCase("PROC_7D38E6BC96230D84", "762FB0AE2553312369314AC4", "DEMON_HERITAGE", "CLASS_WARRIOR", "Hookstaff", "DEMON_WARRIOR_HOOKSTAFF_091")]
        [TestCase("PROC_898D89ACCD685A95", "BAA9950ED3F216B79EFA54AD", "DEMON_HERITAGE", "CLASS_WARRIOR", "Axe", "DEMON_WARRIOR_AXE_091")]
        [TestCase("PROC_D0A09F96EBD75CB9", "C716D643EB2716AFDAD04644", "DEMON_HERITAGE", "CLASS_RANGER", "Shortbow", "DEMON_RANGER_BOW_091")]
        [TestCase("PROC_3AD6E00DAF294A8E", "58D78D1245CF57B005105BDD", "GOBLIN", "CLASS_MAGE", "Signal Horn", "GOBLIN_MAGE_HORN_091")]
        [TestCase("PROC_4BAE60E1403AF3B3", "129AD658615BED4172D82DE2", "DARK_ELF", "CLASS_RANGER", "Travel Spear", "DARK_ELF_RANGER_SPEAR_091")]
        [TestCase("PROC_362FA9DB3A4AB70E", "94E04009CCB65D37964947AC", "HUMAN", "CLASS_MAGE", "Signal Horn", "HUMAN_MAGE_HORN_091")]
        public void ProductionSelfAuthorityAndEquipmentResolveFullBodyInActualSceneFrame(
            string id, string seed, string race, string role, string equipment, string asset)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee091(
                id, seed, race, id, role, equipment, out var sprite, out var key), Is.True, id);
            Assert.That(key, Is.EqualTo(ProceduralRoot + asset));
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(1024));
            Assert.That(sprite.texture.height, Is.EqualTo(1536));
            Assert.That(sprite.rect.height, Is.GreaterThan(sprite.rect.width));
            Assert.That(M1VisualAssets.TryResolveMenuStandee091(
                id, seed, "RACE_" + race, id, role.Substring("CLASS_".Length), equipment,
                out var repeated, out var repeatedKey), Is.True);
            Assert.That(repeated, Is.SameAs(sprite));
            Assert.That(repeatedKey, Is.EqualTo(key));

            var frame = new GameObject("Scene integrated applicant", typeof(RectTransform), typeof(Image));
            try
            {
                var populate = typeof(M1FlowPresenter).GetMethod(
                    "PopulateMenuStandeeFrame091", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(populate, Is.Not.Null);
                populate.Invoke(null, new object[]
                {
                    frame.GetComponent<Image>(), id, seed, race, id,
                    "Production applicant", null, role, equipment, true
                });
                var artwork = frame.GetComponentsInChildren<Image>(true).Single(value =>
                    value.name == "Standing Hero Sprite 090");
                Assert.That(artwork.sprite, Is.SameAs(sprite));
                Assert.That(artwork.preserveAspect, Is.True);
                Assert.That(artwork.raycastTarget, Is.False);
                Assert.That(frame.GetComponent<Image>().color.a, Is.Zero,
                    "Scene integration must not restore the opaque portrait box.");
                Assert.That(frame.GetComponentsInChildren<Text>(true).Any(value =>
                    value.name == "Standing Hero Fallback 090"), Is.False);
            }
            finally { Object.DestroyImmediate(frame); }
        }

        [TestCase("DEMON_HERITAGE", "CLASS_WARRIOR", "Staff")]
        [TestCase("DEMON_HERITAGE", "CLASS_RANGER", "Axe")]
        [TestCase("DEMON_HERITAGE", "CLASS_HEALER", "Hookstaff")]
        [TestCase("GOBLIN", "CLASS_MAGE", "Wand")]
        [TestCase("GOBLIN", "CLASS_WARRIOR", "Signal Horn")]
        [TestCase("DARK_ELF", "CLASS_RANGER", "Shortbow")]
        [TestCase("HUMAN", "CLASS_MAGE", "Staff")]
        [TestCase("HUMAN", "CLASS_RANGER", "Signal Horn")]
        [TestCase("HUMAN", "CLASS_MAGE", "")]
        public void WrongEquipmentOrRoleDoesNotBorrowANewGenericSprite(string race, string role, string gear)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee091(
                "PROC_UNAUTHORED_091", "UNAUTHORED_SEED_091", race, "PROC_UNAUTHORED_091",
                role, gear, out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [TestCase("PROC_UNAUTHORED_091", "PROC_DIFFERENT_091")]
        [TestCase("PROC_UNAUTHORED_091", "SIGREC_UNAUTHORED_091")]
        [TestCase("HERO_REC_UNAUTHORED_091", null)]
        [TestCase("SIGREC_UNAUTHORED_091", null)]
        public void GenericEquipmentMatchCannotOverrideAnotherIdentity(string id, string authority)
        {
            Assert.That(M1VisualAssets.TryResolveMenuStandee091(id, "UNAUTHORED_SEED_091",
                "DEMON_HERITAGE", authority, "CLASS_WARRIOR", "Hookstaff",
                out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [Test]
        public void FreyaCampaignInstanceUsesOneIntactBodyForDossierStandeeAndExistingActionRig()
        {
            const string instance = "RUNTIME_FREYA_INSTANCE_091";
            Assert.That(M1VisualAssets.TryResolvePortrait(instance, "VISUAL_FREYA_091", "HUMAN",
                "HERO_REC_287", out var portrait, out var portraitKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleStandee(instance, "VISUAL_FREYA_091", "HUMAN",
                "HERO_REC_287", out var standee, out var standeeKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose(instance, "VISUAL_FREYA_091", "HUMAN",
                "HERO_REC_287", out var action, out var actionKey), Is.True);
            Assert.That(new[] { portraitKey, standeeKey, actionKey }, Is.All.EqualTo(FreyaKey));
            Assert.That(portrait, Is.SameAs(standee));
            Assert.That(action, Is.SameAs(standee),
                "The complete idle raster is moved by the existing action rig, not a claimed new action pose.");
            Assert.That(standee.texture.width, Is.EqualTo(1024));
            Assert.That(standee.texture.height, Is.EqualTo(1536));
        }

        [TestCase("HERO_REC_283")]
        [TestCase("HERO_REC_291")]
        public void NeighboringAuthoredHeroesKeepTheirOwnStandingAndActionArt(string heroId)
        {
            Assert.That(M1VisualAssets.TryResolveBattleStandee("RUNTIME_" + heroId,
                "HERO_REC_287", "HUMAN", heroId, out _, out var standing), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose("RUNTIME_" + heroId,
                "HERO_REC_287", "HUMAN", heroId, out _, out var action), Is.True);
            Assert.That(standing, Is.EqualTo(M1VisualAssets.BattleRoot + "/STANDEE_" + heroId));
            Assert.That(action, Is.EqualTo(M1VisualAssets.BattleRoot + "/ACTION_" + heroId));
        }

        [Test]
        public void AllTenSssIdentitiesKeepAuthoredDossierIdleAndAttackPriority()
        {
            Assert.That(SssTenV4Roster090.All.Count, Is.EqualTo(10));
            foreach (var hero in SssTenV4Roster090.All)
            {
                var instance = "RUNTIME_" + hero.HeroId;
                Assert.That(M1VisualAssets.TryResolvePortrait(instance, "HERO_REC_287", "HUMAN",
                    hero.HeroId, out var portrait, out var portraitKey), Is.True);
                Assert.That(M1VisualAssets.TryResolveBattleStandee(instance, "HERO_REC_287", "HUMAN",
                    hero.HeroId, out var idle, out var idleKey), Is.True);
                Assert.That(M1VisualAssets.TryResolveBattleActionPose(instance, "HERO_REC_287", "HUMAN",
                    hero.HeroId, out var attack, out var attackKey), Is.True);
                Assert.That(portraitKey, Is.EqualTo(hero.PortraitArtResourcePath));
                Assert.That(idleKey, Is.EqualTo(hero.IdleArtResourcePath));
                Assert.That(attackKey, Is.EqualTo(hero.AttackArtResourcePath));
                Assert.That(portrait, Is.Not.Null);
                Assert.That(idle, Is.Not.Null);
                Assert.That(attack, Is.Not.Null);
            }
        }
    }
}
