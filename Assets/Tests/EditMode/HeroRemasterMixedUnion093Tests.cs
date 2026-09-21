using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class HeroRemasterMixedUnion093Tests
    {
        // Actual approved broad/narrow standees, not repeated starter art or
        // artificial rectangles. These fixtures prove layout, not acquisition.
        static readonly string[] MixedIds093 = { "HERO_REC_169", "HERO_REC_171", "HERO_REC_172",
            "HERO_REC_175", "HERO_REC_177", "HERO_REC_180" };

        [TestCase(1920, 1080)]
        [TestCase(1280, 800)]
        public void ShippingSixMemberUnionKeepsMixedRemastersLargeGroundedAndSeparated093(int width, int height)
        {
            var factory = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.RuntimeUi");
            Assert.That(factory, Is.Not.Null);
            var create = factory.GetMethod("CreateCanvas", BindingFlags.Public | BindingFlags.Static);
            Assert.That(create, Is.Not.Null);
            var canvas = (Canvas)create.Invoke(null, new object[] { "Actual shipping Canvas mixed-remaster proof 093" });
            var root = canvas.GetComponent<RectTransform>();
            var scaler = canvas.GetComponent<CanvasScaler>();
            Assert.That(canvas.renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(2796, 1290)));
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
            // EditMode has no controllable target player Screen. Keep the real
            // factory Canvas, but calibrate its plane to the production scaler's
            // exact logical dimensions. One world unit then equals one output
            // pixel at the requested resolution; no raw-1920 logical rectangle.
            scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            root.sizeDelta = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(width, height);
            root.localScale = Vector3.one * (width / root.sizeDelta.x);
            var view = canvas.gameObject.AddComponent<M2BattleDioramaView072>();
            var heroes = MixedIds093.Select(id => HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(hero => hero.StableId == id)).ToArray();
            var ally = new M2BattleUnionView
            {
                UnionId = "MIXED_REMASTER_SIX_093", DisplayName = "Mixed approved hero art", Side = "Player", CanAct = true,
                Members = heroes.Select(hero => new M2BattleMemberView { MemberId = hero.StableId,
                    PortraitAuthorityId = hero.StableId, DisplayName = hero.Name, RaceId = hero.Race,
                    ClassName = hero.Role, CurrentHp = 100, MaximumHp = 100 }).ToArray()
            };
            var enemy = new M2BattleUnionView
            {
                UnionId = "MIXED_REMASTER_ENEMY_093", DisplayName = "Untouched enemy scale", Side = "Enemy",
                Members = new[] { new M2BattleMemberView { MemberId = "ENEMY_GATE_GNAWER_01",
                    DisplayName = "Gate Gnawer", CurrentHp = 100, MaximumHp = 100 } }
            };
            try
            {
                view.Initialize(root);
                view.Refresh(new M2BattleView { BattleId = "BTL_M1_GATE_APPROACH_001",
                    PlayerUnions = new[] { ally }, EnemyUnions = new[] { enemy } }, ally.UnionId);
                Canvas.ForceUpdateCanvases();
                var rigs = MixedIds093.Select(id => view.ResolveActor(id, ally.UnionId)).ToArray();
                Assert.That(rigs.All(rig => rig != null), Is.True, "All six exact identities must bind in one real Union.");
                var opponent = view.ResolveActor("ENEMY_GATE_GNAWER_01", enemy.UnionId);
                Assert.That(opponent, Is.Not.Null);
                var enemySize = opponent.Root.sizeDelta;
                var enemyScale = opponent.Root.localScale;
                var resting = new Rect[rigs.Length];
                var restingSprites = new Sprite[rigs.Length];
                for (var index = 0; index < rigs.Length; index++)
                {
                    Assert.That(rigs[index].SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                    resting[index] = AssertPose093(rigs[index], MixedIds093[index], false, root, width, height);
                    restingSprites[index] = rigs[index].CurrentArtwork076.sprite;
                }
                var sorted = resting.OrderBy(rect => rect.xMin).ToArray();
                for (var index = 1; index < sorted.Length; index++)
                    Assert.That(sorted[index].xMin - sorted[index - 1].xMax, Is.GreaterThanOrEqualTo(4f),
                        "Every resting silhouette needs a real four-pixel gap; wide coats/weapons may not hide its neighbor.");

                // One real actor pose changes at a time. An action may extend
                // into its presentation lane, but no other actor is rescaled or
                // rebound and every body must return to its original idle slot.
                for (var acting = 0; acting < rigs.Length; acting++)
                {
                    Assert.That(rigs[acting].SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
                    AssertPose093(rigs[acting], MixedIds093[acting], true, root, width, height);
                    for (var other = 0; other < rigs.Length; other++)
                    {
                        if (other == acting) continue;
                        Assert.That(rigs[other].CurrentArtwork076.sprite, Is.SameAs(restingSprites[other]));
                        AssertRect093(VisibleWorldRect093(rigs[other].CurrentArtwork076), resting[other],
                            "Another member's action cannot shrink or displace a resting neighbor.");
                    }
                    Assert.That(rigs[acting].SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                    Assert.That(rigs[acting].CurrentArtwork076.sprite, Is.SameAs(restingSprites[acting]));
                    AssertRect093(AssertPose093(rigs[acting], MixedIds093[acting], false, root, width, height),
                        resting[acting], "Idle must return to the same pixel footprint after action.");
                }
                Assert.That(opponent.Root.sizeDelta, Is.EqualTo(enemySize));
                Assert.That(opponent.Root.localScale, Is.EqualTo(enemyScale));
                Assert.That(M2BattleDioramaView072.BossPresentationScaleMultiplier076, Is.EqualTo(1.72f));
            }
            finally
            {
                foreach (var id in MixedIds093) view.ResolveActor(id, ally.UnionId)?.Dispose();
                view.ResolveActor("ENEMY_GATE_GNAWER_01", enemy.UnionId)?.Dispose();
                var stage = root.Find("Battle Diorama Experience 072");
                if (stage != null) Object.DestroyImmediate(stage.gameObject);
                Object.DestroyImmediate(canvas.gameObject);
            }
        }

        static Rect AssertPose093(M2BattleActorRig072 rig, string identity, bool action,
            RectTransform root, int width, int height)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, action, out var expected, out var key), Is.True);
            var image = rig.CurrentArtwork076;
            Assert.That(image.sprite, Is.SameAs(expected), identity + " must use its own exact pose, not another atlas cell.");
            Assert.That(rig.CurrentResourcePath, Is.EqualTo(key));
            Assert.That(image.preserveAspect, Is.True);
            var proof = HeroRosterBuiltPlayerAudit093.InspectExactRemasterCell093(image, identity,
                action ? "MIXED ACTION" : "MIXED IDLE", action, false);
            Assert.That(proof.exactCellBound && proof.neighborCellExcluded, Is.True);
            var bounds = VisibleWorldRect093(image);
            Assert.That(bounds.height, Is.GreaterThan(height * 0.145f),
                identity + " is too small in a full Union; no weakened raw-canvas threshold.");
            var foot = rig.Root.TransformPoint(new Vector3(0, rig.Root.rect.height * 0.07f));
            Assert.That(bounds.yMin, Is.EqualTo(foot.y).Within(2f), identity + " floats or sinks at its existing depth baseline.");
            var header = root.TransformPoint(new Vector3(0,
                root.rect.yMin + root.rect.height * M2BattleDioramaView072.FocusedUnionHpRibbonMinY076));
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(header.y - 8f), identity + " is hidden under the focused HP strip.");
            var minimum = root.TransformPoint(root.rect.min);
            var maximum = root.TransformPoint(root.rect.max);
            Assert.That(maximum.x - minimum.x, Is.EqualTo(width).Within(0.1f));
            Assert.That(maximum.y - minimum.y, Is.EqualTo(height).Within(0.1f));
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(minimum.x));
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(maximum.x));
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(minimum.y));
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(maximum.y));
            return bounds;
        }

        static Rect VisibleWorldRect093(Image image)
        {
            var sprite = image.sprite;
            var alpha = M1SilhouetteFraming091.VisibleRect091(sprite.texture, sprite.rect);
            Assert.That(M1SilhouetteFraming091.VisibleRect091(sprite), Is.EqualTo(alpha),
                "Measure actual alpha pixels, not transparent canvas or authored sprite mesh assumptions.");
            using (var helper = new VertexHelper())
            {
                typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(image, new object[] { helper });
                Assert.That(helper.currentVertCount, Is.GreaterThan(0));
                var vertex = new UIVertex(); helper.PopulateUIVertex(ref vertex, 0);
                var mesh = new Bounds(vertex.position, Vector3.zero);
                var uvMin = (Vector2)vertex.uv0; var uvMax = uvMin;
                for (var index = 1; index < helper.currentVertCount; index++)
                {
                    helper.PopulateUIVertex(ref vertex, index); mesh.Encapsulate(vertex.position);
                    uvMin = Vector2.Min(uvMin, vertex.uv0); uvMax = Vector2.Max(uvMax, vertex.uv0);
                }
                var scaleX = mesh.size.x / ((uvMax.x - uvMin.x) * sprite.texture.width);
                var scaleY = mesh.size.y / ((uvMax.y - uvMin.y) * sprite.texture.height);
                Assert.That(scaleX, Is.EqualTo(scaleY).Within(0.0001f), "Authored body proportions may not be stretched.");
                var localMin = mesh.min + new Vector3((alpha.xMin - uvMin.x * sprite.texture.width) * scaleX,
                    (alpha.yMin - uvMin.y * sprite.texture.height) * scaleY);
                var low = image.rectTransform.TransformPoint(localMin);
                var high = image.rectTransform.TransformPoint(localMin + new Vector3(alpha.width * scaleX, alpha.height * scaleY));
                return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            }
        }

        static void AssertRect093(Rect actual, Rect expected, string reason)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.25f), reason);
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.25f), reason);
            Assert.That(actual.width, Is.EqualTo(expected.width).Within(0.25f), reason);
            Assert.That(actual.height, Is.EqualTo(expected.height).Within(0.25f), reason);
        }
    }
}
