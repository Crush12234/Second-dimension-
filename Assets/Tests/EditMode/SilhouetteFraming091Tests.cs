using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class SilhouetteFraming091Tests
    {
        [TestCase(32, 16, 64, 160)]
        [TestCase(12, 100, 196, 70)]
        [TestCase(101, 30, 25, 195)]
        public void AlphaFramingFitsTallWideAndPaddedCutoutsWithoutChangingPixels091(
            int x, int y, int width, int height)
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            var pixels = new Color32[256 * 256];
            for (var row = y; row < y + height; row++)
            for (var column = x; column < x + width; column++)
                pixels[row * 256 + column] = new Color32(90, 160, 210, 255);
            texture.SetPixels32(pixels);
            texture.Apply();
            var source = Sprite.Create(texture, new Rect(0, 0, 256, 256), Vector2.one * 0.5f,
                100f, 0u, SpriteMeshType.FullRect);
            try
            {
                var frame = M1SilhouetteFraming091.FrameResourceSprite091(source);
                Assert.That(frame.texture, Is.SameAs(texture));
                Assert.That(M1SilhouetteFraming091.VisibleRect091(source), Is.EqualTo(new Rect(x, y, width, height)));
                Assert.That(frame.rect.Contains(new Vector2(x, y)), Is.True);
                Assert.That(frame.rect.xMax, Is.GreaterThanOrEqualTo(x + width));
                Assert.That(frame.rect.yMax, Is.GreaterThanOrEqualTo(y + height));
                Assert.That(frame.rect.width, Is.LessThan(256f));
                Assert.That(frame.rect.height, Is.LessThan(256f));
                Assert.That(Mathf.Max(width / frame.rect.width, height / frame.rect.height),
                    Is.InRange(0.90f, 1f), "Visible silhouette, not source canvas, sets the display scale.");
                Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(source), Is.SameAs(frame));
                CollectionAssert.AreEqual(pixels, texture.GetPixels32());
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(texture); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyOrOpaqueSourceIsNotInventivelyCropped091(bool opaque)
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            texture.SetPixels32(Enumerable.Repeat(new Color32(50, 60, 70, opaque ? (byte)255 : (byte)0), 256).ToArray());
            texture.Apply();
            var source = Sprite.Create(texture, new Rect(0, 0, 16, 16), Vector2.one * 0.5f);
            try { Assert.That(M1SilhouetteFraming091.FrameResourceSprite091(source), Is.SameAs(source)); }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(texture); }
        }

        [TestCase("SIGREC_ODELIA_FEN", "HUMAN", "Mage", "")]
        [TestCase("HERO_REC_287", "HUMAN", "Warrior", "")]
        [TestCase("PROC_SCALE_DOG_091", "DOG_TRIBE", "Rogue", "")]
        [TestCase("PROC_SCALE_DEMON_MAGE_091", "DEMON_HERITAGE", "Mage", "")]
        [TestCase("PROC_SCALE_DEMON_AXE_091", "DEMON_HERITAGE", "Warrior", "Axe")]
        [TestCase("PROC_SCALE_DEMON_STAFF_091", "DEMON_HERITAGE", "Warrior", "Hookstaff")]
        [TestCase("PROC_SCALE_DEMON_BOW_091", "DEMON_HERITAGE", "Ranger", "Bow")]
        [TestCase("PROC_SCALE_GOBLIN_HORN_091", "GOBLIN", "Mage", "Horn")]
        [TestCase("PROC_SCALE_ELF_SPEAR_091", "DARK_ELF", "Ranger", "Spear")]
        [TestCase("PROC_SCALE_HUMAN_091", "HUMAN", "Mage", "Horn")]
        public void ActualMenuBuilderUsesSilhouetteFrameWithSharedFeetAndPadding091(
            string id, string race, string role, string equipment)
        {
            var host = new GameObject("Real menu silhouette frame", typeof(RectTransform), typeof(Image));
            host.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 480f);
            try
            {
                var build = typeof(M1FlowPresenter).GetMethod("PopulateMenuStandeeFrame091",
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(build, Is.Not.Null);
                build.Invoke(null, new object[] { host.GetComponent<Image>(), id, "", race, id,
                    id, null, role, equipment, false });
                Canvas.ForceUpdateCanvases();
                var art = host.GetComponentsInChildren<Image>(true).Single(value => value.name == "Standing Hero Sprite 090");
                Assert.That(art.sprite, Is.Not.Null);
                Assert.That(art.preserveAspect, Is.True);
                Assert.That(art.rectTransform.pivot.y, Is.Zero);
                Assert.That(art.rectTransform.anchorMin.x, Is.EqualTo(0.07f).Within(0.00001f));
                Assert.That(art.rectTransform.anchorMin.y, Is.EqualTo(0.04f).Within(0.00001f));
                Assert.That(art.rectTransform.anchorMax.x, Is.EqualTo(0.93f).Within(0.00001f));
                Assert.That(art.rectTransform.anchorMax.y, Is.EqualTo(0.96f).Within(0.00001f));
                Assert.That(M1VisualAssets.TryResolveMenuStandee091(id, "", race, id, role,
                    equipment, out var resolved, out _), Is.True);
                Assert.That(art.sprite, Is.SameAs(resolved), "The live builder must use the framed resolver result.");
                var alpha = AssertAlphaOccupancy091(art.sprite, id != "SIGREC_ODELIA_FEN");
                var mesh = MeshBounds091(art, out var uv);
                Assert.That(mesh.size.x, Is.LessThanOrEqualTo(art.rectTransform.rect.width + 0.1f));
                Assert.That(mesh.size.y, Is.LessThanOrEqualTo(art.rectTransform.rect.height + 0.1f));
                var pixelScaleX = mesh.size.x / (uv.width * art.sprite.texture.width);
                var pixelScaleY = mesh.size.y / (uv.height * art.sprite.texture.height);
                Assert.That(pixelScaleX, Is.EqualTo(pixelScaleY).Within(0.001f),
                    "Sprite padding may trim the UI quad, but visible pixels must never stretch.");
                var visibleFoot = mesh.min.y +
                    (alpha.yMin - uv.yMin * art.sprite.texture.height) * pixelScaleY;
                Assert.That(visibleFoot / art.rectTransform.rect.height, Is.InRange(-0.001f, 0.07f),
                    "Measure the visible foot, not Unity's padding-trimmed mesh edge; only safe silhouette padding is allowed.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase("SIGREC_MAREN_HOLT")]
        [TestCase("SIGREC_ODELIA_FEN")]
        [TestCase("PROC_36344E2400DC98B6")]
        [TestCase("PROC_F85A4CAA747BC8C6")]
        [TestCase("PROC_5B14E7816E55FFB5")]
        [TestCase("PROC_748DD03A23E1FEB0")]
        public void EveryLegacyPlayerPoseRetainsARealSilhouetteWithoutReadableTextureCopies091(string identity)
        {
            foreach (var pose in new[] { "IDLE", "ANTICIPATION", "ACTION_PRIMARY", "ROLE_PRIMARY",
                         "HIT_REACTION", "RECOVERY", "DOWNED", "VICTORY" })
            {
                var source = Resources.Load<Sprite>("SecondDimension/Art/Battle011/Characters/" +
                    identity + "/POSE_" + pose);
                Assert.That(source, Is.Not.Null, identity + " " + pose);
                var framed = M1SilhouetteFraming091.FrameResourceSprite091(source);
                var alpha = AssertAlphaOccupancy091(framed, true);
                var measured = M1SilhouetteFraming091.VisibleRect091(framed);
                Assert.That(measured.height, Is.EqualTo(alpha.height).Within(4f),
                    "Derived FullRect sprites must retain the imported silhouette bounds, not their padded canvas.");
                Assert.That(framed.texture, Is.SameAs(source.texture), "Original authored pixels and identity are preserved.");
            }
        }

        [TestCase("ENEMY_GATE_GNAWER_01")]
        [TestCase("ENEMY_GATE_GNAWER_02")]
        [TestCase("ENEMY_GATE_GNAWER_03")]
        [TestCase("GUILDMASTER_076")]
        public void LegacyEnemyAndGuildmasterKeepTheirExistingFullRectImportPolicy091(string identity)
        {
            var path = "Assets/Resources/SecondDimension/Art/Battle011/Characters/" + identity + "/POSE_IDLE.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect),
                "The six-player fit must not alter enemy or guildmaster importer behavior.");
            var policy = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Editor.BattleArtAssetPostprocessor011");
            if (policy == null)
                policy = AppDomain.CurrentDomain.GetAssemblies().Select(assembly =>
                    assembly.GetType("SecondDimension.Editor.BattleArtAssetPostprocessor011")).FirstOrDefault(type => type != null);
            Assert.That(policy, Is.Not.Null);
            var select = policy.GetMethod("UsesAlliedSilhouette091", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(select, Is.Not.Null);
            Assert.That(select.Invoke(null, new object[] { path }), Is.False,
                "A future reimport must also retain the negative asset's existing policy.");
        }

        [TestCase(1920, 1080, 3)]
        [TestCase(1280, 800, 3)]
        [TestCase(1920, 1080, 1)]
        [TestCase(1280, 800, 1)]
        [TestCase(1920, 1080, 2)]
        [TestCase(1280, 800, 2)]
        [TestCase(1920, 1080, 6)]
        [TestCase(1280, 800, 6)]
        [TestCase(1920, 1080, 6, true)]
        [TestCase(1280, 800, 6, true)]
        public void RealDioramaSharesVisibleHeroHeightAndKeepsHeadsFeetAndNeighborsClear091(
            int width, int height, int memberCount, bool alliedRescueTarget093 = false)
        {
            var host = new GameObject("Shipping diorama silhouette proof", typeof(RectTransform));
            var hostRect = host.GetComponent<RectTransform>();
            var canvasSize093 = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(width, height);
            hostRect.sizeDelta = canvasSize093;
            // The real player uses a 2796x1290 reference canvas. A raw 1920-unit
            // host concealed the old tiny-sprite defect after screen scaling.
            hostRect.localScale = Vector3.one * (width / canvasSize093.x);
            var view = host.AddComponent<M2BattleDioramaView072>();
            var identities = memberCount == 1
                ? new[] { "SIGREC_YVES_THORNFIELD" }
                : memberCount == 2
                    ? new[] { "SIGREC_ODELIA_FEN", "SIGREC_VAELIS_NOCT" }
                    : memberCount == 3
                        ? new[] { "PROC_5B14E7816E55FFB5", "SIGREC_ODELIA_FEN", "SIGREC_VAELIS_NOCT" }
                        : new[] { "SIGREC_MAREN_HOLT", "SIGREC_ODELIA_FEN", "PROC_36344E2400DC98B6",
                            "PROC_F85A4CAA747BC8C6", "PROC_5B14E7816E55FFB5", "PROC_748DD03A23E1FEB0" };
            var allies = new M2BattleUnionView
            {
                UnionId = "ACTUAL_ALLIES_091", DisplayName = "Authored trio", Side = "Player", CanAct = true,
                Members = identities.Select(id => new M2BattleMemberView
                {
                    MemberId = id, PortraitAuthorityId = id, DisplayName = id, RaceId = "HUMAN",
                    ClassName = id == "SIGREC_ODELIA_FEN" ? "Mage" : "Warrior",
                    CurrentHp = 100, MaximumHp = 100
                }).ToArray()
            };
            var enemies = new M2BattleUnionView
            {
                UnionId = "ACTUAL_ENEMY_091", DisplayName = "Enemy scale control", Side = "Enemy",
                Members = new[] { new M2BattleMemberView { MemberId = "ENEMY_GATE_GNAWER_01",
                    DisplayName = "Gate Gnawer", CurrentHp = 100, MaximumHp = 100 } }
            };
            try
            {
                view.Initialize(hostRect);
                view.Refresh(new M2BattleView { BattleId = "BTL_M1_GATE_APPROACH_001",
                    PlayerUnions = new[] { allies }, EnemyUnions = new[] { enemies } }, allies.UnionId);
                if (alliedRescueTarget093)
                    typeof(M2BattleDioramaView072).GetMethod("LayoutUnion",
                        BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view,
                        new object[] { allies, false, true });
                Canvas.ForceUpdateCanvases();
                var rigs = identities.Select(id => view.ResolveActor(id, allies.UnionId)).ToArray();
                Assert.That(rigs.All(rig => rig != null), Is.True, "Use the actual shipping union builder, not isolated rigs.");
                var enemy = view.ResolveActor("ENEMY_GATE_GNAWER_01", enemies.UnionId);
                var enemyScale = enemy.Root.localScale;
                var enemySize = enemy.Root.sizeDelta;
                var idleSprites = rigs.Select(rig => rig.CurrentArtwork076.sprite).ToArray();
                float[] initialHeights = null;
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle,
                             BattleArtPoseDirector011.ActionPrimary, BattleArtPoseDirector011.Idle })
                {
                    var bounds = new Rect[rigs.Length];
                    for (var index = 0; index < rigs.Length; index++)
                    {
                        var rig = rigs[index];
                        Assert.That(rig.SetPoseImmediate(pose), Is.True);
                        var art = rig.CurrentArtwork076;
                        Assert.That(art.preserveAspect, Is.True);
                        bounds[index] = ActualAlphaWorldBounds091(art);
                        var foot = rig.Root.TransformPoint(new Vector3(0f, rig.Root.rect.height * 0.07f));
                        Assert.That(bounds[index].yMin, Is.EqualTo(foot.y).Within(2f),
                            identities[index] + " must stay on its existing depth-row foot baseline through a pose swap.");
                        var headerBottom = hostRect.TransformPoint(new Vector3(0f,
                            hostRect.rect.yMin + hostRect.rect.height * M2BattleDioramaView072.FocusedUnionHpRibbonMinY076));
                        Assert.That(bounds[index].yMax, Is.LessThanOrEqualTo(headerBottom.y - 8f),
                            identities[index] + " head/weapon must remain below the real focused-HP strip, including a lone Tower hero.");
                        var hostBottom = hostRect.TransformPoint(hostRect.rect.min);
                        var hostTop = hostRect.TransformPoint(hostRect.rect.max);
                        Assert.That(bounds[index].xMin, Is.GreaterThanOrEqualTo(hostBottom.x));
                        Assert.That(bounds[index].xMax, Is.LessThanOrEqualTo(hostTop.x));
                        var minimumScreenHeight093 = memberCount == 1 ? 0.28f : memberCount == 2 ? 0.24f :
                            memberCount == 3 ? 0.22f : 0.145f;
                        Assert.That(bounds[index].height, Is.GreaterThan(height * minimumScreenHeight093),
                            "Heroes must occupy a substantial actual-screen height, not merely fit their logical image canvas.");
                        Assert.That(rig.Root.localScale.x,
                            Is.EqualTo(memberCount == 1 ? 1.02f : memberCount == 2 ? 0.90f :
                                memberCount == 3 ? 0.79f : 0.68f).Within(0.0001f));
                    }
                    var heights = bounds.Select(bound => bound.height).ToArray();
                    Assert.That(heights.Max() / heights.Min(), Is.LessThanOrEqualTo(1.035f),
                        "Members must have the same visible height together, not just same-size image canvases.");
                    if (pose == BattleArtPoseDirector011.Idle)
                        for (var index = 1; index < bounds.Length; index++)
                            Assert.That(bounds[index].xMin - bounds[index - 1].xMax, Is.GreaterThanOrEqualTo(4f),
                                "Resting silhouettes need a real gap; a focused spell can extend beyond its resting lane.");
                    if (initialHeights == null) initialHeights = heights;
                    else for (var index = 0; index < heights.Length; index++)
                        Assert.That(heights[index], Is.EqualTo(initialHeights[index]).Within(3f),
                            "Idle/action swaps keep one standing height rather than popping between independent width fits.");
                }
                for (var index = 0; index < rigs.Length; index++)
                    Assert.That(rigs[index].CurrentArtwork076.sprite, Is.SameAs(idleSprites[index]));
                Assert.That(enemy.Root.localScale, Is.EqualTo(enemyScale));
                Assert.That(enemy.Root.sizeDelta, Is.EqualTo(enemySize));
                Assert.That(M2BattleDioramaView072.BossPresentationScaleMultiplier076, Is.EqualTo(1.72f));
            }
            finally
            {
                // Retire real rigs first; then destroy the child root before its
                // host in EditMode so the MonoBehaviour sees an already-retired UI.
                foreach (var id in identities) view.ResolveActor(id, allies.UnionId)?.Dispose();
                view.ResolveActor("ENEMY_GATE_GNAWER_01", enemies.UnionId)?.Dispose();
                var root = host.transform.Find("Battle Diorama Experience 072");
                if (root != null) Object.DestroyImmediate(root.gameObject);
                Object.DestroyImmediate(host);
            }
        }

        static Rect ActualAlphaWorldBounds091(Image image)
        {
            var alpha = AssertAlphaOccupancy091(image.sprite, true);
            var mesh = MeshBounds091(image, out var uv);
            var texture = image.sprite.texture;
            var scaleX = mesh.size.x / (uv.width * texture.width);
            var scaleY = mesh.size.y / (uv.height * texture.height);
            Assert.That(scaleX, Is.EqualTo(scaleY).Within(0.0001f), "Actual mesh pixels must not stretch.");
            var bottomLeft = mesh.min + new Vector3(
                (alpha.xMin - uv.xMin * texture.width) * scaleX,
                (alpha.yMin - uv.yMin * texture.height) * scaleY);
            var topRight = bottomLeft + new Vector3(alpha.width * scaleX, alpha.height * scaleY);
            var worldMin = image.rectTransform.TransformPoint(bottomLeft);
            var worldMax = image.rectTransform.TransformPoint(topRight);
            return Rect.MinMaxRect(worldMin.x, worldMin.y, worldMax.x, worldMax.y);
        }

        static Rect AssertAlphaOccupancy091(Sprite framed, bool requireTightImport)
        {
            var assetPath = AssetDatabase.GetAssetPath(framed.texture);
            Assert.That(assetPath, Is.Not.Empty);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null);
            var importSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(importSettings);
            if (requireTightImport)
            {
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite),
                    assetPath + " must import an actual alpha-meshed Sprite, not a Texture2D with an unused Tight flag.");
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
                Assert.That(importSettings.spriteMeshType, Is.EqualTo(SpriteMeshType.Tight),
                    assetPath + " needs a real alpha mesh; a non-readable FullRect cannot reveal transparent padding.");
                Assert.That(importer.isReadable, Is.False, "Framing must not retain CPU copies of every imported texture.");
            }
            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(decoded, File.ReadAllBytes(assetPath), false), Is.True);
                var alpha = M1SilhouetteFraming091.VisibleRect091(decoded,
                    new Rect(0, 0, decoded.width, decoded.height));
                var scale = new Vector2(framed.texture.width / (float)decoded.width,
                    framed.texture.height / (float)decoded.height);
                alpha = new Rect(Vector2.Scale(alpha.position, scale), Vector2.Scale(alpha.size, scale));
                Assert.That(framed.rect.xMin, Is.LessThanOrEqualTo(alpha.xMin + 1f));
                Assert.That(framed.rect.yMin, Is.LessThanOrEqualTo(alpha.yMin + 1f));
                Assert.That(framed.rect.xMax, Is.GreaterThanOrEqualTo(alpha.xMax - 1f));
                Assert.That(framed.rect.yMax, Is.GreaterThanOrEqualTo(alpha.yMax - 1f));
                Assert.That(Mathf.Max(alpha.width / framed.rect.width, alpha.height / framed.rect.height),
                    Is.InRange(0.87f, 1.01f), assetPath + " visible silhouette should fill the framed display with only safe padding.");
                Assert.That((alpha.yMin - framed.rect.yMin) / framed.rect.height,
                    Is.InRange(-0.01f, 0.07f), assetPath + " feet must not float on a large transparent source margin.");
                return alpha;
            }
            finally { Object.DestroyImmediate(decoded); }
        }

        [TestCase("ENEMY_REC_001", false)]
        [TestCase("ENEMY_REC_035", false)]
        [TestCase("ENEMY_REC_070", false)]
        [TestCase("SIGREC_ODELIA_FEN", true)]
        [TestCase("HERO_REC_287", true)]
        public void ActualBattleRigKeepsGroundedAspectAndCachedPoseFraming091(string identity, bool hero)
        {
            var host = new GameObject("Actual battle silhouette framing", typeof(RectTransform));
            var union = new M2BattleUnionView { UnionId = "FRAMING_091", DisplayName = "Framing proof" };
            var member = new M2BattleMemberView
            {
                MemberId = identity, PortraitAuthorityId = identity, DisplayName = identity,
                CurrentHp = 100, MaximumHp = 100, RaceId = "HUMAN", ClassName = "Mage",
                EnemyArtBaseId090 = hero ? null : identity,
                EnemyArtVariantId090 = hero ? null : identity + "_VAR_01"
            };
            M2BattleActorRig072 rig = null;
            try
            {
                rig = new M2BattleActorRig072(host.transform as RectTransform, union, member, !hero);
                rig.SetLayout(Vector2.zero, new Vector2(400f, 620f), 1f, 0);
                var idle = rig.CurrentArtwork076.sprite;
                Assert.That(idle, Is.Not.Null);
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle, BattleArtPoseDirector011.ActionPrimary, BattleArtPoseDirector011.Idle })
                {
                    Assert.That(rig.SetPoseImmediate(pose), Is.True);
                    var art = rig.CurrentArtwork076;
                    Assert.That(art.rectTransform.pivot.y, Is.Zero);
                    var mesh = MeshBounds091(art);
                    Assert.That(mesh.min.y, Is.EqualTo(0f).Within(0.1f), "Idle and attack remain grounded on the same edge.");
                    Assert.That(mesh.size.x / mesh.size.y,
                        Is.EqualTo(art.sprite.rect.width / art.sprite.rect.height).Within(0.01f));
                }
                Assert.That(rig.CurrentArtwork076.sprite, Is.SameAs(idle));
                Assert.That(M2BattleDioramaView072.BossPresentationScaleMultiplier076, Is.EqualTo(1.72f));
            }
            finally { rig?.Dispose(); Object.DestroyImmediate(host); }
        }

        static Bounds MeshBounds091(Image image) => MeshBounds091(image, out _);

        static Bounds MeshBounds091(Image image, out Rect uvBounds)
        {
            var populate = typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(VertexHelper) }, null);
            using (var vertices = new VertexHelper())
            {
                populate.Invoke(image, new object[] { vertices });
                Assert.That(vertices.currentVertCount, Is.GreaterThanOrEqualTo(4));
                var vertex = new UIVertex();
                vertices.PopulateUIVertex(ref vertex, 0);
                var bounds = new Bounds(vertex.position, Vector3.zero);
                var uvMin = new Vector2(vertex.uv0.x, vertex.uv0.y);
                var uvMax = uvMin;
                for (var index = 1; index < vertices.currentVertCount; index++)
                {
                    vertices.PopulateUIVertex(ref vertex, index);
                    bounds.Encapsulate(vertex.position);
                    var uv = new Vector2(vertex.uv0.x, vertex.uv0.y);
                    uvMin = Vector2.Min(uvMin, uv);
                    uvMax = Vector2.Max(uvMax, uv);
                }
                uvBounds = Rect.MinMaxRect(uvMin.x, uvMin.y, uvMax.x, uvMax.y);
                return bounds;
            }
        }
    }
}
