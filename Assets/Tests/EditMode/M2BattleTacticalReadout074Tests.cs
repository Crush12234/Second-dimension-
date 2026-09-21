using NUnit.Framework;
using SecondDimension.Presentation;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2BattleTacticalReadout074Tests
    {
        [Test]
        public void MoraleRailFavorsHealthierLivingSideWithoutBecomingAbsolute()
        {
            var battle = new M2BattleView
            {
                PlayerUnions = new[] { Union("ALLY", "Open", 100, 100, false) },
                EnemyUnions = new[] { Union("ENEMY", "Engaged", 25, 100, false) }
            };

            var share = M2BattleTacticalReadout074.AllyMoraleShare(battle);

            Assert.That(share, Is.GreaterThan(0.60f));
            Assert.That(share, Is.LessThan(0.96f));
        }

        [TestCase("Open", "Open", "APPROACH")]
        [TestCase("Engaged", "Engaged", "DEADLOCK")]
        [TestCase("Flanking", "Engaged", "FLANK")]
        [TestCase("Open", "Intercepting", "INTERFERENCE")]
        [TestCase("Engaged", "Broken", "BREAKTHROUGH")]
        public void EngagementLabelSurfacesBattlefieldRelationship(
            string allyState,
            string enemyState,
            string expected)
        {
            Assert.That(
                M2BattleTacticalReadout074.EngagementLabel(
                    Union("ALLY", allyState, 100, 100, false),
                    Union("ENEMY", enemyState, 100, 100, false)),
                Is.EqualTo(expected));
        }

        [Test]
        public void CombatChainCountsImpactEventsFromLastResolvedRound()
        {
            var battle = new M2BattleView
            {
                LastResolvedRoundEvents = new[]
                {
                    new M2BattleEventView { EventType = "FORECAST_COMMITTED" },
                    new M2BattleEventView { EventType = "PLAYER_HIT" },
                    new M2BattleEventView { EventType = "ENEMY_HIT" },
                    new M2BattleEventView { EventType = "INTERCEPTION" },
                    new M2BattleEventView { EventType = "AP_RECOVERY" }
                }
            };

            Assert.That(M2BattleTacticalReadout074.CombatChain(battle), Is.EqualTo(3));
        }

        [Test]
        public void UnionSummaryKeepsUnionScaleResourcesAndLivingCountTogether()
        {
            var union = new M2BattleUnionView
            {
                DisplayName = "Lantern Guard",
                CurrentAp = 8,
                MaximumAp = 14,
                Members = new[]
                {
                    Member("A", 45, 60, false),
                    Member("B", 0, 50, true)
                }
            };

            var summary = M2BattleTacticalReadout074.UnionSummary(union, "Guild Union");

            Assert.That(summary, Does.Contain("Lantern Guard"));
            Assert.That(summary, Does.Contain("HP 45/110"));
            Assert.That(summary, Does.Contain("AP 8/14"));
            Assert.That(summary, Does.Contain("1/2 UP"));
        }

        [Test]
        public void Battle075GeneratedEnemyCutoutsUseDeterministicCleanWindowsImports()
        {
            var generatedCutouts = new[]
            {
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_BULWARK_ACTION.png", "d1b462c9c5a144a8a9fea8f8acfc50d6"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_BULWARK_IDLE.png", "9f92025eb7d9498a965c8fee4814ff15"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_SCOUT_ACTION.png", "70b028f967ed4c0e92dbb6f7c5e49b82"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_SCOUT_IDLE.png", "b55225cb33904acb8cf94b567c4d8042"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_STANDARD_ACTION.png", "ededa9513fe840aeb94ba807a9fd980b"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/GATE_GNAWER_STANDARD_IDLE.png", "13777c8d18b849d99942e3f4760cafe9"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/HINGE_EATER_COLOSSUS_ACTION.png", "4e79194df500401289bc4e2d7245e276"),
                ("Assets/Resources/SecondDimension/Art/Battle075/Enemies/HINGE_EATER_COLOSSUS_IDLE.png", "baf63efaf37d455bb949b9ebfb1d7aa8")
            };
            foreach (var generated in generatedCutouts)
            {
                Assert.That(AssetDatabase.AssetPathToGUID(generated.Item1),
                    Is.EqualTo(generated.Item2), generated.Item1 + " must keep its release GUID.");
                AssertCleanCutoutImport(generated.Item1, requireSprite: false);
            }
        }

        [Test]
        public void Battle075EnemyOnlyShaderIsAResourceWithUiClipAndFringeRepair()
        {
            const string path =
                "Assets/Resources/SecondDimension/UI/EnemyCutoutClean075.shader";
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Assert.That(shader, Is.Not.Null);
            Assert.That(shader.name, Is.EqualTo(M2BattleActorRig072.EnemyCutoutShaderName075));
            Assert.That(AssetDatabase.AssetPathToGUID(path),
                Is.EqualTo("af0a27be64ed49aeb4bfd26ea666d5cb"));

            var source = File.ReadAllText(Path.GetFullPath(path));
            Assert.That(source, Does.Contain("UnityGet2DClipping"),
                "The enemy material must remain compatible with UI masks and clipping.");
            Assert.That(source, Does.Contain("PreferMoreOpaque075"),
                "Low-alpha fringe RGB must be replaced by the nearest opaque source color.");
            Assert.That(source, Does.Contain("smoothstep"));
            Assert.That(source, Does.Contain("Blend SrcAlpha OneMinusSrcAlpha"));
        }

        private static void AssertCleanCutoutImport(string path, bool requireSprite)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path + " must bleed transparent RGB safely.");
            Assert.That(importer.mipmapEnabled, Is.False, path + " must not generate colored cutout mip fringes.");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
            Assert.That(importer.maxTextureSize, Is.EqualTo(2048), path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None),
                path + " must preserve the authored boss and variant aspect ratios.");
            var standalone = importer.GetPlatformTextureSettings("Standalone");
            Assert.That(standalone.overridden, Is.True,
                path + " must keep the Windows player override explicit.");
            Assert.That(standalone.maxTextureSize, Is.EqualTo(2048), path);
            Assert.That(standalone.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed),
                path + " must preserve its antialiased alpha edge in the release player.");
            var serializedMeta = File.ReadAllText(Path.GetFullPath(path + ".meta"));
            Assert.That(serializedMeta, Does.Contain("spriteExtrude: 4"),
                path + " needs enough transparent-edge extrusion for scaled UI presentation.");
            if (requireSprite)
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
        }

        private static M2BattleUnionView Union(
            string id,
            string engagement,
            int currentHp,
            int maximumHp,
            bool downed) =>
            new M2BattleUnionView
            {
                UnionId = id,
                DisplayName = id,
                Engagement = engagement,
                CurrentAp = 10,
                MaximumAp = 15,
                Members = new[] { Member(id + "_MEMBER", currentHp, maximumHp, downed) }
            };

        private static M2BattleMemberView Member(
            string id,
            int currentHp,
            int maximumHp,
            bool downed) =>
            new M2BattleMemberView
            {
                MemberId = id,
                DisplayName = id,
                CurrentHp = currentHp,
                MaximumHp = maximumHp,
                Downed = downed
            };
    }
}
