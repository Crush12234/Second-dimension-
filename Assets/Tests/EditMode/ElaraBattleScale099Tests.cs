using System;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ElaraBattleScale099Tests
    {
        [TestCase("HERO_REC_011", 420f, 260f, 620f)]
        [TestCase("HERO_REC_011", 230f, 130f, 380f)]
        [TestCase("HERO_REC_011", 420f, 90f, 170f)]
        [TestCase("HERO_REC_041", 420f, 260f, 620f)]
        [TestCase("HERO_REC_041", 230f, 130f, 380f)]
        [TestCase("HERO_REC_041", 420f, 90f, 170f)]
        [TestCase("HERO_REC_195", 420f, 260f, 620f)]
        [TestCase("HERO_REC_195", 230f, 130f, 380f)]
        [TestCase("HERO_REC_195", 420f, 90f, 170f)]
        public void ActualRigUsesIdlePixelScaleForExactReviewedAction100(string identity, float height, float idleWidth, float actionWidth)
        {
            var host = new GameObject("Elara calibrated shipping rig099", typeof(RectTransform));
            M2BattleActorRig072 actor = null;
            try
            {
                actor = CreateActor099(host.GetComponent<RectTransform>(), identity);
                actor.ConfigureAlliedSilhouetteFit091(height, idleWidth, actionWidth);
                Canvas.ForceUpdateCanvases();
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                var idle = actor.CurrentArtwork076.sprite;
                var idleScale = MeshPixelScale099(actor.CurrentArtwork076, out var idleFoot);
                foreach (var pose in new[] { BattleArtPoseDirector011.ActionPrimary, BattleArtPoseDirector011.RolePrimary })
                {
                    Assert.That(actor.SetPoseImmediate(pose), Is.True);
                    var action = actor.CurrentArtwork076.sprite;
                    Assert.That(action, Is.Not.SameAs(idle));
                    var actionScale = MeshPixelScale099(actor.CurrentArtwork076, out var actionFoot);
                    var expected = Mathf.Min(idleScale, actionWidth / action.rect.width);
                    Assert.That(actionScale, Is.EqualTo(expected).Within(0.0001f));
                    Assert.That(actionFoot, Is.EqualTo(idleFoot).Within(0.01f), "Full alpha silhouette remains grounded.");
                    Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var exactAction, out _), Is.True);
                    Assert.That(action, Is.SameAs(exactAction), "Sizing never substitutes or crops the original pose.");
                    if (identity == "HERO_REC_011")
                    {
                        Assert.That(action.rect.xMin, Is.GreaterThanOrEqualTo(634));
                        Assert.That(action.rect.xMax, Is.GreaterThanOrEqualTo(1523), "Complete spear/effect edge retained.");
                    }
                    Assert.That(actor.CurrentArtwork076.rectTransform.localScale, Is.EqualTo(Vector3.one));
                }
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                Assert.That(MeshPixelScale099(actor.CurrentArtwork076, out _), Is.EqualTo(idleScale).Within(0.0001f));
                Assert.That(actor.CurrentArtwork076.sprite, Is.SameAs(idle));
            }
            finally { actor?.Dispose(); Object.DestroyImmediate(host); }
        }

        [TestCase("HERO_REC_016")]
        [TestCase("HERO_REC_012")]
        [TestCase("HERO_REC_265")]
        public void OtherExactHeroesKeepOriginalPoseFit099(string identity)
        {
            var host = new GameObject("Unchanged pose fit control099", typeof(RectTransform));
            M2BattleActorRig072 actor = null;
            try
            {
                actor = CreateActor099(host.GetComponent<RectTransform>(), identity);
                actor.ConfigureAlliedSilhouetteFit091(420, 260, 620);
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle, BattleArtPoseDirector011.ActionPrimary })
                {
                    Assert.That(actor.SetPoseImmediate(pose), Is.True);
                    var image = actor.CurrentArtwork076;
                    var alpha = M1SilhouetteFraming091.VisibleRect091(image.sprite);
                    var oldScale = Mathf.Min(420 / alpha.height,
                        (pose == BattleArtPoseDirector011.Idle ? 260 : 620) / image.sprite.rect.width);
                    Assert.That(MeshPixelScale099(image, out _), Is.EqualTo(oldScale).Within(0.0001f));
                }
            }
            finally { actor?.Dispose(); Object.DestroyImmediate(host); }
        }

        [TestCase("HERO_REC_011")]
        [TestCase("HERO_REC_041")]
        [TestCase("HERO_REC_195")]
        public void SameTextureCellCopyIsNotCalibrationAuthority099(string identity)
        {
            Assert.That(HeroRemasterAtlas093.TryResolve093(identity, true, out var action, out _), Is.True);
            var forged = Sprite.Create(action.texture, action.rect,
                new Vector2(action.pivot.x / action.rect.width, action.pivot.y / action.rect.height),
                action.pixelsPerUnit, 0u, SpriteMeshType.FullRect);
            try { Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(forged, 1f, 100, 100), Is.EqualTo(1f)); }
            finally { Object.DestroyImmediate(forged); }
            Assert.That(HeroRemasterAtlas093.CalibrateBattlePixelScale099(null, 1f, 100, 100), Is.EqualTo(1f));
        }

        static M2BattleActorRig072 CreateActor099(RectTransform host, string identity) => new M2BattleActorRig072(host,
            new M2BattleUnionView { UnionId = "ART_FIT_UNION099", DisplayName = "Art fit fixture" },
            new M2BattleMemberView { MemberId = identity, PortraitAuthorityId = identity, DisplayName = identity,
                CurrentHp = 100, MaximumHp = 100 }, false);

        static float MeshPixelScale099(Image image, out float footY)
        {
            using (var mesh = new VertexHelper())
            {
                typeof(Image).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new[] { typeof(VertexHelper) }, null).Invoke(image, new object[] { mesh });
                Assert.That(mesh.currentVertCount, Is.GreaterThan(0));
                var vertex = new UIVertex(); mesh.PopulateUIVertex(ref vertex, 0);
                var min = vertex.position; var max = min; var uvMin = (Vector2)vertex.uv0; var uvMax = uvMin;
                for (var index = 1; index < mesh.currentVertCount; index++)
                {
                    mesh.PopulateUIVertex(ref vertex, index);
                    min = Vector3.Min(min, vertex.position); max = Vector3.Max(max, vertex.position);
                    uvMin = Vector2.Min(uvMin, vertex.uv0); uvMax = Vector2.Max(uvMax, vertex.uv0);
                }
                var scaleX = (max.x - min.x) / ((uvMax.x - uvMin.x) * image.sprite.texture.width);
                var scaleY = (max.y - min.y) / ((uvMax.y - uvMin.y) * image.sprite.texture.height);
                Assert.That(scaleX, Is.EqualTo(scaleY).Within(0.0001f));
                var alpha = M1SilhouetteFraming091.VisibleRect091(image.sprite);
                footY = image.rectTransform.TransformPoint(min + new Vector3(0,
                    (alpha.yMin - uvMin.y * image.sprite.texture.height) * scaleY)).y;
                return scaleX;
            }
        }
    }
}
