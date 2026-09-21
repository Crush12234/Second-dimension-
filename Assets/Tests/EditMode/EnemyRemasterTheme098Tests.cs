using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyRemasterTheme098Tests
    {
        [SetUp] public void SetUp098() => EnemyArt700Runtime090.ResetForTests090();
        [TearDown] public void TearDown098() => EnemyArt700Runtime090.ResetForTests090();

        [TestCase(1, "Standard")]
        [TestCase(2, "Veteran")]
        [TestCase(3, "Armored")]
        [TestCase(4, "Swift")]
        [TestCase(5, "Frost")]
        [TestCase(6, "Storm")]
        [TestCase(7, "Venom")]
        [TestCase(8, "Void")]
        [TestCase(9, "Tower Elite")]
        [TestCase(10, "Apex")]
        public void ShippingActorBindsExactCatalogThemeOnItsOwnMaterial098(int index, string name)
        {
            var variant = "ENEMY_REC_021_VAR_" + index.ToString("00");
            Assert.That(EnemyRemasterTheme098.TryResolve098("ENEMY_REC_021", variant, out var theme), Is.True);
            Assert.That(theme.VariantIndex098, Is.EqualTo(index));
            Assert.That(theme.VariantId098, Is.EqualTo(variant));
            Assert.That(theme.Name098, Is.EqualTo(name));
            Assert.That(EnemyArt700Runtime090.TryResolveVariant090("ENEMY_REC_021", variant, out var record,
                out var error), Is.True, error);
            Assert.That(record.theme, Is.EqualTo(name));
            Assert.That(record.runtimeStatRulesProvided, Is.False);
            var host = new GameObject("Myrmidon Theme Rig098", typeof(RectTransform));
            M2BattleActorRig072 actor = null;
            try
            {
                actor = CreateActor098(host.GetComponent<RectTransform>(), variant);
                Assert.That(actor.CurrentEnemyRemasterVariantId098, Is.EqualTo(variant));
                Assert.That(actor.CurrentEnemyRemasterThemeName098, Is.EqualTo(name));
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(EnemyArtRemaster098.ResourcePath098 + "#IDLE"));
                var material = actor.CurrentArtwork076.material;
                AssertAccent098(material.GetColor(EnemyRemasterTheme098.AccentProperty098), theme.Accent098);
                Assert.That(material.GetFloat(EnemyRemasterTheme098.AmountProperty098), Is.EqualTo(index == 3 ? 0f : 1f));
                Assert.That(actor.CurrentArtwork076.color, Is.EqualTo(Color.white),
                    "A separate difficulty tint must not muddy exact catalog theme colors.");
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
                Assert.That(actor.CurrentArtwork076.material, Is.SameAs(material));
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(EnemyArtRemaster098.ResourcePath098 + "#ACTION"));
                Assert.That(actor.CurrentEnemyRemasterVariantId098, Is.EqualTo(variant));
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Downed), Is.True);
                Assert.That(actor.CurrentArtwork076.color, Is.EqualTo(new Color(0.46f, 0.50f, 0.58f, 1f)),
                    "Downed desaturation and alpha remain separate from the variant accent.");
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                Assert.That(actor.CurrentArtwork076.color, Is.EqualTo(Color.white));
                AssertAccent098(material.GetColor(EnemyRemasterTheme098.AccentProperty098), theme.Accent098);
            }
            finally { actor?.Dispose(); UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void ExactFamilyOnlyAndConcurrentMaterialsRemainIsolated098()
        {
            Assert.That(EnemyArt700Runtime090.Catalog090.variants.Count(value =>
                EnemyArtRemaster098.Handles098(value.baseEnemyId, value.variantId)), Is.EqualTo(10));
            foreach (var malformed in new[] { null, "ENEMY_REC_021_VAR_00", "ENEMY_REC_021_VAR_11",
                "ENEMY_REC_021_VAR_4", "ENEMY_REC_021_VAR_04 ", "enemy_rec_021_var_04" })
                Assert.That(EnemyArtRemaster098.Handles098("ENEMY_REC_021", malformed), Is.False);
            Assert.That(EnemyArtRemaster098.Handles098("ENEMY_REC_020", "ENEMY_REC_021_VAR_04"), Is.False);
            var host = new GameObject("Simultaneous Exact Themes098", typeof(RectTransform));
            M2BattleActorRig072 swift = null, venom = null;
            try
            {
                swift = CreateActor098(host.GetComponent<RectTransform>(), "ENEMY_REC_021_VAR_04");
                venom = CreateActor098(host.GetComponent<RectTransform>(), "ENEMY_REC_021_VAR_07");
                Assert.That(swift.CurrentArtwork076.sprite, Is.SameAs(venom.CurrentArtwork076.sprite),
                    "This is deliberately one complete body with per-actor material variants.");
                var swiftMaterial = swift.CurrentArtwork076.material;
                var venomMaterial = venom.CurrentArtwork076.material;
                Assert.That(swiftMaterial, Is.Not.SameAs(venomMaterial));
                var swiftAccent = swiftMaterial.GetColor(EnemyRemasterTheme098.AccentProperty098);
                Assert.That(swiftAccent, Is.Not.EqualTo(venomMaterial.GetColor(EnemyRemasterTheme098.AccentProperty098)));
                Assert.That(venom.SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
                Assert.That(swiftMaterial.GetColor(EnemyRemasterTheme098.AccentProperty098), Is.EqualTo(swiftAccent));
                Assert.That(EnemyArtRemaster098.ResourceLoadCount098, Is.EqualTo(1));
                Assert.That(EnemyArtRemaster098.ResidentSpriteCount098, Is.EqualTo(2));
            }
            finally { swift?.Dispose(); venom?.Dispose(); UnityEngine.Object.DestroyImmediate(host); }
        }

        [Test]
        public void RawOrUnrelatedSourceNeverReceivesRemasterAccent098()
        {
            var shader = Resources.Load<Shader>(M2BattleActorRig072.EnemyCutoutShaderResource075);
            var material = new Material(shader);
            try
            {
                Assert.That(material.GetFloat(EnemyRemasterTheme098.AmountProperty098), Is.Zero);
                Assert.That(EnemyArt700Runtime090.TryLoadSourceSpriteForVerification098("ENEMY_REC_021",
                    "ENEMY_REC_021_VAR_04", EnemyArt700Pose090.Idle, out var archived, out var error), Is.True, error);
                material.SetFloat(EnemyRemasterTheme098.AmountProperty098, 1f);
                Assert.That(EnemyRemasterTheme098.ApplyToMaterial098(material, "ENEMY_REC_021",
                    "ENEMY_REC_021_VAR_04", archived, out var theme), Is.False);
                Assert.That(theme, Is.Null);
                Assert.That(material.GetFloat(EnemyRemasterTheme098.AmountProperty098), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        static void AssertAccent098(Color actual, Color expected)
        {
            // Native material color round-trips can differ by a few float ULPs.
            // This is far below one display byte; GPU tests enforce visible differences.
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.00001f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.00001f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.00001f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(0.00001f));
        }

        static M2BattleActorRig072 CreateActor098(RectTransform host, string variant) => new M2BattleActorRig072(host,
            new M2BattleUnionView { UnionId = "THEME_UNION098", DisplayName = "Theme fixture" },
            new M2BattleMemberView { MemberId = "THEME_MEMBER098_" + variant, DisplayName = "Crystal Myrmidon",
                CurrentHp = 300, MaximumHp = 300, EnemyArtBaseId090 = "ENEMY_REC_021", EnemyArtVariantId090 = variant,
                EnemyThreatTier089 = 10 }, true);
    }
}
