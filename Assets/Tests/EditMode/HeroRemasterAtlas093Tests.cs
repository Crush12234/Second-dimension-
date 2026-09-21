using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRemasterAtlas093Tests
    {
        [TestCase("HERO_REC_011", 634, "CB120AE494BEF05D3D5BBA347CC7BB726E2503DFEC1C4CF490BEC44BB35F3EF4")]
        [TestCase("HERO_REC_016", 679, "86C23FDE255FFD547CE4762395F676FD4CE0C93E8792E112D604EEB626544F79")]
        [TestCase("HERO_REC_012", 773, "29EE4B8253F01312BC3410C88FBBC58193AB50E8EC45230F6663FF691DBD81F7")]
        [TestCase("HERO_REC_025", 697, "533D163CFD92BE5E57819AAEE21E6C9277836B28AE94BDCFDAF3BB6FC1BB01DF")]
        [TestCase("HERO_REC_029", 704, "8E5617EB44A3209581BF9D3E266F888933A3837236181C39654CAA1E4387F953")]
        [TestCase("HERO_REC_015", 803, "9785DF09717BC9078BE7D3E0E8159614FE9A0EB5C561DC30BE100B846C414EF5")]
        [TestCase("HERO_REC_019", 752, "7A24B5A8A8719FB27544E86078B83724F191BACF79379E6A3A3625AC624D2A50")]
        [TestCase("HERO_REC_022", 746, "BC497D8AAF35F6774F2D3C338B6C1B4453EA4A1DEBD85F81E1CBDC9DB84F9D2F")]
        [TestCase("HERO_REC_046", 741, "3F5399E6208B913816F2CB3D116F66D43D1F791A164DCBBD23EA030257675828")]
        [TestCase("HERO_REC_056", 715, "5DBECBE47DE0EADFE00BC66C95EC14BF52552DFEC1DAAE993AE45579943E9A5B")]
        [TestCase("HERO_REC_066", 764, "08E8472C726E315935D15D06C4CAB999177E7DDF8E0B31F2D82A5088EAA41728")]
        [TestCase("HERO_REC_079", 676, "D52A7EADC4D644F04BDB3B1370FD62C7BDAF3BE94CE10368BC6921F3DEF66AA8")]
        [TestCase("HERO_REC_069", 725, "876A75778CEC4BC99FD1C496850D5E2197745FD0745646BF4C99D866995BBD9C")]
        [TestCase("HERO_REC_106", 692, "FCEBC167A183593E80F2B93818F4F489337EC66383B08C53517BF85FDB31E651")]
        [TestCase("HERO_REC_169", 664, "EA3AD0AA25538E9EAB509F071475C4308763EC6F21D4A9BEB451418CD5D73D3B")]
        [TestCase("HERO_REC_170", 672, "174566393D7FB3894053ED148FA79EDB5DBEE54EB15ECDB4777BD221F71FBC62")]
        [TestCase("HERO_REC_171", 712, "D80BE4D7F0BFE01F14CC45D107737469846DC0F62B2CE8B88848011F0E68183B")]
        [TestCase("HERO_REC_172", 647, "236B1FC52C8822E9592106171850AC2E66AEB80F7E8CA8A27B72BB31DA21E36F")]
        [TestCase("HERO_REC_173", 570, "CBF55B640B18F6DCC9E6E0E7D253C370B9304FD9AC9CBCDCEADF81FC1BF8B8CF")]
        [TestCase("HERO_REC_174", 705, "D3D4A9A29CC7367373356B0CB9DECE71F113FD29F63A7B6005322454260FA1B0")]
        [TestCase("HERO_REC_175", 679, "41FC2713AF9A686006AB4A8AB48078164239D2F94A2AD88C777E4238A7591877")]
        [TestCase("HERO_REC_176", 622, "F44E9855C089305225B5358E7E33D0DE17E531FB5A8FB490F81041AD814A014C")]
        [TestCase("HERO_REC_177", 708, "19745013D26DC3CA92631BC95C785591A9D4811CC8DA908B73056B0F2CB22FDE")]
        [TestCase("HERO_REC_178", 705, "AE69B7A836C4D3CD08D410EDA6A8C3CBC62D9EBE7CBB8A64673B9E944D745992")]
        [TestCase("HERO_REC_179", 670, "4B84891B1598677E0966B72D644970C3E60EFD194E2D2A1C39E9C1FBD5600D27")]
        [TestCase("HERO_REC_180", 670, "1C77D103444095899335F579591C446C8A2CF1B0CA1E73B7FDF81623C6FDC048")]
        [TestCase("HERO_REC_265", 756, "4034BFE67F7AF6A2B326800434E4BAC9A05D88E31F26589E6D9572489A71E8C1")]
        public void ExactIdentityUsesInspectedUnequalFramesAndDistinctPoses093(string identity, int split, string reviewedSha256)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == identity);
            var path = HeroRemasterAtlas093.Root093 + identity + "_PAIR_093";
            var texture = Resources.Load<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, "Missing original reviewed atlas is not a finished-art pass.");
            using (var sha = SHA256.Create())
                Assert.That(BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(AssetDatabase.GetAssetPath(texture)))).Replace("-", ""),
                    Is.EqualTo(reviewedSha256), "Only the reviewed original version may bind; clipped/rejected versions remain excluded.");
            Assert.That(HeroRemasterAtlas093.TryGetFrameRects093(identity, texture.width, texture.height,
                out var idleFrame, out var actionFrame), Is.True);
            Assert.That(idleFrame, Is.EqualTo(new Rect(0, 0, split, 1024)));
            Assert.That(actionFrame, Is.EqualTo(new Rect(split, 0, 1536 - split, 1024)));
            Assert.That(idleFrame.width, Is.Not.EqualTo(actionFrame.width), "Never guess equal halves.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
            Assert.That(importer.isReadable, Is.True, "These reviewed atlases alone need a first-load alpha scan.");
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(M1VisualAssets.TryResolveBattleStandee(identity, identity, hero.Race, identity,
                out var idle, out var idleKey), Is.True);
            Assert.That(M1VisualAssets.TryResolveBattleActionPose(identity, identity, hero.Race, identity,
                out var action, out var actionKey), Is.True);
            Assert.That(idle.texture, Is.SameAs(texture));
            Assert.That(action.texture, Is.SameAs(texture));
            Assert.That(idleKey, Is.EqualTo(path + "#IDLE"));
            Assert.That(actionKey, Is.EqualTo(path + "#ACTION"));
            Assert.That(action.rect, Is.Not.EqualTo(idle.rect), "A shared atlas is not a reused static pose.");
            AssertExactCellPixels093(idle, idleFrame);
            AssertExactCellPixels093(action, actionFrame);
            Assert.That(idle.rect.xMax, Is.LessThanOrEqualTo(split));
            Assert.That(action.rect.xMin, Is.GreaterThanOrEqualTo(split));
            Assert.That(M1VisualAssets.TryResolvePortrait(identity, identity, hero.Race, identity,
                out var dossier, out _), Is.True);
            Assert.That(dossier, Is.SameAs(idle), "Menus and dossier show this full-body idle, not the entire pair.");
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var again, out _), Is.True);
            Assert.That(again, Is.SameAs(action), "Repeated pose changes reuse cached sprites.");
            var row = HeroRosterAudit093.Census093().Single(value => value.stableId == identity);
            HeroRosterAudit093.BindArt093(hero, row);
            Assert.That(row.artCategory, Is.EqualTo("ORIGINAL_EXACT_ID_TWO_POSE_REMASTER_ATLAS"));
            Assert.That(row.professionalArtReviewed || row.runtimeUiVerified, Is.False,
                "An automated binding test cannot claim the user accepted the art or a Windows run occurred.");
        }

        [TestCase("HERO_REC_184")]
        [TestCase("HERO_REC_181_SUFFIX")]
        [TestCase("UNKNOWN")]
        [TestCase(null)]
        [TestCase("SIGREC_ODELIA_FEN")]
        [TestCase("DWARF")]
        [TestCase("ELF")]
        public void RemasterNeverSubstitutesAnotherIdentityOrAnEntireFamily093(string identity)
        {
            Assert.That(HeroRemasterAtlas093.ContainsIdentity093(identity), Is.False);
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var sprite, out var key), Is.False);
            Assert.That(sprite, Is.Null);
            Assert.That(key, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void UnequalCellsRetainSeparateAlphaGeometryAfterWindowsCpuRelease093(bool discard)
        {
            var texture = new Texture2D(96, 64, TextureFormat.RGBA32, false) { name = "UNEQUAL_ATLAS_FIXTURE_093" };
            var pixels = new Color32[96 * 64];
            for (var y = 8; y < 58; y++)
            for (var x = 8; x < 24; x++) pixels[y * 96 + x] = new Color32(255, 40, 40, 255);
            for (var y = 12; y < 54; y++)
            for (var x = 36; x < 90; x++) pixels[y * 96 + x] = new Color32(40, 70, 255, 255);
            texture.SetPixels32(pixels); texture.Apply();
            HeroRemasterAtlas093.Pair093 pair = null;
            try
            {
                pair = HeroRemasterAtlas093.BuildPair093(texture, new Rect(0, 0, 32, 64), new Rect(32, 0, 64, 64), discard);
                Assert.That(pair, Is.Not.Null);
                Assert.That(texture.isReadable, Is.EqualTo(!discard));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Idle), Is.EqualTo(new Rect(8, 8, 16, 50)));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(pair.Action), Is.EqualTo(new Rect(36, 12, 54, 42)));
                Assert.That(pair.Idle.rect.xMax, Is.LessThanOrEqualTo(32));
                Assert.That(pair.Action.rect.xMin, Is.GreaterThanOrEqualTo(32));
                Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(pair.Idle), Is.SameAs(pair.Idle));
                Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(pair.Action), Is.SameAs(pair.Action));
                if (!discard) CollectionAssert.AreEqual(pixels, texture.GetPixels32(), "Framing never rewrites pixels.");
            }
            finally { HeroRemasterAtlas093.RetireSources093(pair); Object.DestroyImmediate(texture); }
        }

        [TestCase("opaque")]
        [TestCase("empty-action")]
        [TestCase("boundary-crossing")]
        [TestCase("overlap")]
        public void UnusableOrOverlappingAtlasCellsFailClosed093(string defect)
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (var y = 8; y < 56; y++)
            for (var x = 4; x < 28; x++) pixels[y * 64 + x] = new Color32(100, 100, 100, 255);
            if (defect != "empty-action")
                for (var y = 8; y < 56; y++)
                for (var x = 36; x < 60; x++) pixels[y * 64 + x] = new Color32(100, 100, 100, 255);
            if (defect == "opaque") for (var index = 0; index < pixels.Length; index++) pixels[index].a = 255;
            if (defect == "boundary-crossing") pixels[20 * 64 + 32] = new Color32(100, 100, 100, 255);
            texture.SetPixels32(pixels); texture.Apply();
            try
            {
                var right = defect == "overlap" ? new Rect(24, 0, 40, 64) : new Rect(32, 0, 32, 64);
                Assert.That(HeroRemasterAtlas093.BuildPair093(texture, new Rect(0, 0, 32, 64), right, true), Is.Null);
                Assert.That(texture.isReadable, Is.True, "Failed validation must not discard source CPU pixels.");
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [TestCase("HERO_REC_011")]
        [TestCase("HERO_REC_016")]
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
        [TestCase("HERO_REC_265")]
        public void ActualMenuBuilderBindsOnlyThisHeroIdleCell093(string identity)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == identity);
            var host = new GameObject("Reviewed hero actual menu frame", typeof(RectTransform), typeof(Image));
            host.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 480);
            try
            {
                var build = typeof(M1FlowPresenter).GetMethod("PopulateMenuStandeeFrame091", BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(build, Is.Not.Null);
                build.Invoke(null, new object[] { host.GetComponent<Image>(), identity, identity, hero.Race,
                    identity, hero.Name, null, hero.Role, hero.Weapon, false });
                Canvas.ForceUpdateCanvases();
                var image = host.GetComponentsInChildren<Image>(true).Single(value => value.name == "Standing Hero Sprite 090");
                Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var idle, out _), Is.True);
                Assert.That(image.sprite, Is.SameAs(idle));
                Assert.That(image.preserveAspect, Is.True);
                Assert.That(image.rectTransform.pivot.y, Is.Zero);
                var geometry = MeshBounds093(image, out var uv);
                Assert.That(geometry.size.x, Is.LessThanOrEqualTo(image.rectTransform.rect.width + 0.1f));
                Assert.That(geometry.size.y, Is.LessThanOrEqualTo(image.rectTransform.rect.height + 0.1f));
                Assert.That(geometry.size.x / uv.width, Is.EqualTo(geometry.size.y / uv.height *
                    idle.texture.width / idle.texture.height).Within(0.01f), "The mesh preserves original pixel proportions.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase("HERO_REC_011", 1920, 1080)]
        [TestCase("HERO_REC_011", 1280, 800)]
        [TestCase("HERO_REC_016", 1920, 1080)]
        [TestCase("HERO_REC_016", 1280, 800)]
        [TestCase("HERO_REC_012", 1920, 1080)]
        [TestCase("HERO_REC_012", 1280, 800)]
        [TestCase("HERO_REC_025", 1920, 1080)]
        [TestCase("HERO_REC_025", 1280, 800)]
        [TestCase("HERO_REC_029", 1920, 1080)]
        [TestCase("HERO_REC_029", 1280, 800)]
        [TestCase("HERO_REC_015", 1920, 1080)]
        [TestCase("HERO_REC_015", 1280, 800)]
        [TestCase("HERO_REC_019", 1920, 1080)]
        [TestCase("HERO_REC_019", 1280, 800)]
        [TestCase("HERO_REC_022", 1920, 1080)]
        [TestCase("HERO_REC_022", 1280, 800)]
        [TestCase("HERO_REC_046", 1920, 1080)]
        [TestCase("HERO_REC_046", 1280, 800)]
        [TestCase("HERO_REC_056", 1920, 1080)]
        [TestCase("HERO_REC_056", 1280, 800)]
        [TestCase("HERO_REC_066", 1920, 1080)]
        [TestCase("HERO_REC_066", 1280, 800)]
        [TestCase("HERO_REC_079", 1920, 1080)]
        [TestCase("HERO_REC_079", 1280, 800)]
        [TestCase("HERO_REC_069", 1920, 1080)]
        [TestCase("HERO_REC_069", 1280, 800)]
        [TestCase("HERO_REC_106", 1920, 1080)]
        [TestCase("HERO_REC_106", 1280, 800)]
        [TestCase("HERO_REC_169", 1920, 1080)]
        [TestCase("HERO_REC_169", 1280, 800)]
        [TestCase("HERO_REC_170", 1920, 1080)]
        [TestCase("HERO_REC_170", 1280, 800)]
        [TestCase("HERO_REC_171", 1920, 1080)]
        [TestCase("HERO_REC_171", 1280, 800)]
        [TestCase("HERO_REC_172", 1920, 1080)]
        [TestCase("HERO_REC_172", 1280, 800)]
        [TestCase("HERO_REC_173", 1920, 1080)]
        [TestCase("HERO_REC_173", 1280, 800)]
        [TestCase("HERO_REC_174", 1920, 1080)]
        [TestCase("HERO_REC_174", 1280, 800)]
        [TestCase("HERO_REC_175", 1920, 1080)]
        [TestCase("HERO_REC_175", 1280, 800)]
        [TestCase("HERO_REC_176", 1920, 1080)]
        [TestCase("HERO_REC_176", 1280, 800)]
        [TestCase("HERO_REC_177", 1920, 1080)]
        [TestCase("HERO_REC_177", 1280, 800)]
        [TestCase("HERO_REC_178", 1920, 1080)]
        [TestCase("HERO_REC_178", 1280, 800)]
        [TestCase("HERO_REC_179", 1920, 1080)]
        [TestCase("HERO_REC_179", 1280, 800)]
        [TestCase("HERO_REC_180", 1920, 1080)]
        [TestCase("HERO_REC_180", 1280, 800)]
        [TestCase("HERO_REC_265", 1920, 1080)]
        [TestCase("HERO_REC_265", 1280, 800)]
        public void ActualBattleBuilderUsesSeparatePosesAtReadableScreenScale093(string identity, int width, int height)
        {
            var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == identity);
            var host = new GameObject("Exact hero shipping battle pose proof", typeof(RectTransform));
            var hostRect = host.GetComponent<RectTransform>();
            hostRect.sizeDelta = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(width, height);
            hostRect.localScale = Vector3.one * (width / hostRect.sizeDelta.x);
            var view = host.AddComponent<M2BattleDioramaView072>();
            var allies = new M2BattleUnionView
            {
                UnionId = "REMASTER_ALLY_093", DisplayName = hero.Name, Side = "Player", CanAct = true,
                Members = new[] { new M2BattleMemberView { MemberId = identity, PortraitAuthorityId = identity,
                    DisplayName = hero.Name, RaceId = hero.Race, ClassName = hero.Role, CurrentHp = 100, MaximumHp = 100 } }
            };
            var enemies = new M2BattleUnionView
            {
                UnionId = "REMASTER_ENEMY_093", DisplayName = "Scale control", Side = "Enemy",
                Members = new[] { new M2BattleMemberView { MemberId = "ENEMY_GATE_GNAWER_01",
                    DisplayName = "Gate Gnawer", CurrentHp = 100, MaximumHp = 100 } }
            };
            try
            {
                view.Initialize(hostRect);
                view.Refresh(new M2BattleView { BattleId = "BTL_M1_GATE_APPROACH_001",
                    PlayerUnions = new[] { allies }, EnemyUnions = new[] { enemies } }, allies.UnionId);
                Canvas.ForceUpdateCanvases();
                var rig = view.ResolveActor(identity, allies.UnionId);
                Assert.That(rig, Is.Not.Null);
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle, BattleArtPoseDirector011.ActionPrimary,
                             BattleArtPoseDirector011.Idle })
                {
                    Assert.That(rig.SetPoseImmediate(pose), Is.True);
                    Assert.That(HeroRemasterAtlas093.TryResolve093(identity, pose == BattleArtPoseDirector011.ActionPrimary,
                        out var expected, out _), Is.True);
                    var image = rig.CurrentArtwork076;
                    Assert.That(image.sprite, Is.SameAs(expected), "The live actor must bind the actual pose cell.");
                    Assert.That(image.preserveAspect, Is.True);
                    var mesh = MeshBounds093(image, out var uv);
                    var alpha = M1SilhouetteFraming091.VisibleRect091(expected);
                    var scaleX = mesh.size.x / (uv.width * expected.texture.width);
                    var scaleY = mesh.size.y / (uv.height * expected.texture.height);
                    Assert.That(scaleX, Is.EqualTo(scaleY).Within(0.0001f));
                    var localBottom = mesh.min + new Vector3((alpha.xMin - uv.xMin * expected.texture.width) * scaleX,
                        (alpha.yMin - uv.yMin * expected.texture.height) * scaleY);
                    var bottom = image.rectTransform.TransformPoint(localBottom);
                    var top = image.rectTransform.TransformPoint(localBottom + new Vector3(alpha.width * scaleX, alpha.height * scaleY));
                    var readableHeightFraction099 = 0.28f;
                    if (identity == "HERO_REC_011" && pose == BattleArtPoseDirector011.ActionPrimary)
                    {
                        // The lunge is shorter than the upright spear. Preserve
                        // equal source-pixel body scale rather than re-inflating it.
                        Assert.That(HeroRemasterAtlas093.TryResolve093(identity, false, out var calibratedIdle099, out _), Is.True);
                        readableHeightFraction099 *= alpha.height /
                            M1SilhouetteFraming091.VisibleRect091(calibratedIdle099).height;
                    }
                    Assert.That(top.y - bottom.y, Is.GreaterThan(height * readableHeightFraction099), "A nonblank but tiny hero is not acceptable.");
                    var foot = rig.Root.TransformPoint(new Vector3(0, rig.Root.rect.height * 0.07f));
                    Assert.That(bottom.y, Is.EqualTo(foot.y).Within(2f));
                    var headerBottom = hostRect.TransformPoint(new Vector3(0,
                        hostRect.rect.yMin + hostRect.rect.height * M2BattleDioramaView072.FocusedUnionHpRibbonMinY076));
                    Assert.That(top.y, Is.LessThanOrEqualTo(headerBottom.y - 8f));
                    Assert.That(bottom.x, Is.GreaterThanOrEqualTo(hostRect.TransformPoint(hostRect.rect.min).x));
                    Assert.That(top.x, Is.LessThanOrEqualTo(hostRect.TransformPoint(hostRect.rect.max).x));
                }
            }
            finally
            {
                view.ResolveActor(identity, allies.UnionId)?.Dispose();
                view.ResolveActor("ENEMY_GATE_GNAWER_01", enemies.UnionId)?.Dispose();
                var root = host.transform.Find("Battle Diorama Experience 072");
                if (root != null) Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(host);
            }
        }

        static void AssertExactCellPixels093(Sprite sprite, Rect cell)
        {
            var alpha = M1SilhouetteFraming091.VisibleRect091(sprite.texture, cell);
            Assert.That(M1SilhouetteFraming091.VisibleRect091(sprite), Is.EqualTo(alpha));
            Assert.That(sprite.rect.xMin, Is.LessThanOrEqualTo(alpha.xMin));
            Assert.That(sprite.rect.xMax, Is.GreaterThanOrEqualTo(alpha.xMax));
            Assert.That(sprite.rect.yMin, Is.LessThanOrEqualTo(alpha.yMin));
            Assert.That(sprite.rect.yMax, Is.GreaterThanOrEqualTo(alpha.yMax));
            Assert.That(alpha.xMin, Is.GreaterThan(cell.xMin));
            Assert.That(alpha.xMax, Is.LessThan(cell.xMax));
            Assert.That(alpha.yMin, Is.GreaterThan(cell.yMin));
            Assert.That(alpha.yMax, Is.LessThan(cell.yMax));
            var pixels = sprite.texture.GetPixels32();
            var visible = 0; var hidden = 0;
            for (var y = (int)cell.yMin; y < cell.yMax; y++)
            for (var x = (int)cell.xMin; x < cell.xMax; x++)
                if (pixels[y * sprite.texture.width + x].a >= 16) visible++; else hidden++;
            Assert.That(visible, Is.GreaterThan(10000));
            Assert.That(hidden, Is.GreaterThan(cell.width * cell.height * 0.05f));
        }

        static Bounds MeshBounds093(Image image, out Rect uv)
        {
            using (var helper = new VertexHelper())
            {
                typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(image, new object[] { helper });
                Assert.That(helper.currentVertCount, Is.GreaterThan(0));
                var vertex = new UIVertex(); helper.PopulateUIVertex(ref vertex, 0);
                var bounds = new Bounds(vertex.position, Vector3.zero);
                var min = (Vector2)vertex.uv0; var max = min;
                for (var index = 1; index < helper.currentVertCount; index++)
                {
                    helper.PopulateUIVertex(ref vertex, index); bounds.Encapsulate(vertex.position);
                    min = Vector2.Min(min, vertex.uv0); max = Vector2.Max(max, vertex.uv0);
                }
                uv = Rect.MinMaxRect(min.x, min.y, max.x, max.y); return bounds;
            }
        }
    }
}
