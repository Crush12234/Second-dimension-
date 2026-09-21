using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRecoveredSource100099Tests
    {
        [TestCase("HERO_REC_035", 0, 604, 0, 768, "6B66247E095521E9AD870E1237BD2AF737EC322EB7FC387E4186F8869338984C", "70C7317A467494259B85038422F3E8B0EC3CD14F0B44A450E43551FBF447B2C6")]
        public void ExactRecoveredPairKeepsSourceBytesAndExistingResolutionPriority099(string id,
            int idleX, int idleWidth, int actionX, int actionWidth, string idleSha, string actionSha)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == id);
            Assert.That(HeroRemasterAtlas093.ContainsIdentity093(id), Is.False, "Never replace an existing remaster.");
            Assert.That(HeroRecoveredSource100099.TryGetSourceRects099(id, 768, 1024, out var idleCell, out var actionCell), Is.True);
            Assert.That(idleCell, Is.EqualTo(new Rect(idleX, 0, idleWidth, 1024)));
            Assert.That(actionCell, Is.EqualTo(new Rect(actionX, 0, actionWidth, 1024)));
            var idleTexture = CheckTexture099(id, false, idleSha);
            var actionTexture = CheckTexture099(id, true, actionSha);
            Assert.That(idleTexture, Is.Not.SameAs(actionTexture));
            Assert.That(M1VisualAssets.TryResolveBattleStandee(id, id, hero.Race, id, out var idle, out var idleKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose(id, id, hero.Race, id, out var action, out var actionKey), Is.True);
            Assert.That(idleKey, Is.EqualTo(HeroRecoveredSource100099.Root099 + id + "_IDLE_099"));
            Assert.That(actionKey, Is.EqualTo(HeroRecoveredSource100099.Root099 + id + "_ACTION_099"));
            Assert.That(idle.texture, Is.SameAs(idleTexture));
            Assert.That(action.texture, Is.SameAs(actionTexture));
            Assert.That(M1VisualAssets.TryResolvePortrait(id, id, hero.Race, id, out var dossier, out _), Is.True);
            Assert.That(dossier, Is.SameAs(idle));
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, true, out var again, out _), Is.True);
            Assert.That(again, Is.SameAs(action), "No per-frame allocation/alpha scan.");
            CheckCell099(idleTexture, idleCell, idle);
            CheckCell099(actionTexture, actionCell, action);
            var row = HeroRosterAudit093.Census093().Single(value => value.stableId == id);
            HeroRosterAudit093.BindArt093(hero, row);
            Assert.That(row.artCategory, Is.EqualTo(
                "RECOVERED_EXACT_ID_TWO_POSE_SOURCE_PAIR_RIGHTS_PENDING_REQUIRES_VISUAL_REVIEW"));
            Assert.That(row.professionalArtReviewed || row.runtimeUiVerified, Is.False,
                "A source binding label must not certify professional art or a Windows render.");
        }

        static Texture2D CheckTexture099(string id, bool action, string hash)
        {
            var texture = Resources.Load<Texture2D>(HeroRecoveredSource100099.Root099 + id + (action ? "_ACTION_099" : "_IDLE_099"));
            Assert.That(texture, Is.Not.Null);
            var path = AssetDatabase.GetAssetPath(texture);
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""), Is.EqualTo(hash));
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.isReadable, Is.True);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(texture.width, Is.EqualTo(768));
            Assert.That(texture.height, Is.EqualTo(1024));
            return texture;
        }

        static void CheckCell099(Texture2D texture, Rect cell, Sprite frame)
        {
            var pixels = texture.GetPixels32();
            int visible = 0, edge = 0, lost = 0;
            for (int y = 0; y < 1024; y++)
            for (int x = (int)cell.xMin; x < (int)cell.xMax; x++)
            {
                if (pixels[y * 768 + x].a < 16) continue;
                visible++;
                if (x <= (int)cell.xMin || x >= (int)cell.xMax - 1 || y <= 0 || y >= 1023) edge++;
                if (!frame.rect.Contains(new Vector2(x, y))) lost++;
            }
            Assert.That(visible, Is.GreaterThan(10000));
            Assert.That(edge, Is.Zero, "Reviewed source cells must not intersect opaque geometry.");
            Assert.That(lost, Is.Zero, "No retained body/equipment pixel may be lost.");
            Assert.That(frame.rect.xMin, Is.GreaterThanOrEqualTo(cell.xMin));
            Assert.That(frame.rect.xMax, Is.LessThanOrEqualTo(cell.xMax));
        }

        [TestCase("HERO_REC_011")]
        [TestCase("HERO_REC_016")]
        [TestCase("HERO_REC_169")]
        [TestCase("HERO_REC_031")]
        [TestCase("HERO_REC_032")]
        [TestCase("HERO_REC_033")]
        [TestCase("HERO_REC_034")]
        [TestCase("HERO_REC_039")]
        [TestCase("HERO_REC_040")]
        [TestCase("HERO_REC_042")]
        [TestCase("HERO_REC_043")]
        [TestCase("HERO_REC_045")]
        [TestCase("HERO_REC_047")]
        [TestCase("HERO_REC_048")]
        [TestCase("HERO_REC_041")]
        [TestCase("HERO_REC_111")]
        [TestCase("SIGREC_MAREN_HOLT")]
        [TestCase("SIGREC_VEYRA_ASHGLASS")]
        [TestCase("SSS_OMEGA")]
        [TestCase("DWARF")]
        [TestCase("hero_rec_031")]
        [TestCase(null)]
        public void NoLegacyQuarantinedRemasteredOrFamilyIdentityIsSubstituted099(string id)
        {
            Assert.That(HeroRecoveredSource100099.TryResolve099(id, false, out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BothPoseBoundsRemainExactAfterWindowsCpuRelease099(bool discard)
        {
            var idle = Fixture099("IDLE", new RectInt(80, 60, 140, 780));
            var action = Fixture099("ACTION", new RectInt(100, 60, 430, 660));
            HeroRecoveredSource100099.Pair099 pair = null;
            try
            {
                pair = HeroRecoveredSource100099.BuildPair099(idle, action,
                    new Rect(0, 0, 600, 1024), new Rect(50, 0, 718, 1024), discard);
                Assert.That(pair, Is.Not.Null);
                Assert.That(idle.isReadable, Is.EqualTo(!discard));
                Assert.That(action.isReadable, Is.EqualTo(!discard));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Idle), Is.EqualTo(new Rect(80, 60, 140, 780)));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Action), Is.EqualTo(new Rect(100, 60, 430, 660)));
                Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(pair.Action), Is.SameAs(pair.Action));
            }
            finally { HeroRecoveredSource100099.RetirePair099(pair); Object.DestroyImmediate(idle); Object.DestroyImmediate(action); }
        }

        [TestCase("empty")]
        [TestCase("edge")]
        [TestCase("outside")]
        [TestCase("fractional")]
        [TestCase("same-texture")]
        public void InvalidCellsFailWithoutDiscardingOrChangingSource099(string defect)
        {
            var idle = Fixture099("IDLE", new RectInt(80, 60, 140, 780));
            var action = Fixture099("ACTION", defect == "empty" ? new RectInt(0, 0, 0, 0) :
                defect == "edge" ? new RectInt(0, 0, 768, 1024) : new RectInt(100, 60, 430, 660));
            var cell = defect == "outside" ? new Rect(0, 0, 769, 1024) :
                defect == "fractional" ? new Rect(0.5f, 0, 600, 1024) : new Rect(0, 0, 768, 1024);
            try
            {
                Assert.That(HeroRecoveredSource100099.BuildPair099(idle,
                    defect == "same-texture" ? idle : action, cell, new Rect(0, 0, 768, 1024), true), Is.Null);
                Assert.That(idle.isReadable && action.isReadable, Is.True);
            }
            finally { Object.DestroyImmediate(idle); Object.DestroyImmediate(action); }
        }

        static Texture2D Fixture099(string name, RectInt filled)
        {
            var texture = new Texture2D(768, 1024, TextureFormat.RGBA32, false) { name = name };
            var pixels = new Color32[768 * 1024];
            for (int y = filled.yMin; y < filled.yMax; y++)
            for (int x = filled.xMin; x < filled.xMax; x++) pixels[y * 768 + x] = new Color32(100, 140, 220, 255);
            texture.SetPixels32(pixels); texture.Apply();
            return texture;
        }
    }
}
