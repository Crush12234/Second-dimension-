using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class ElaraBattleScale099GpuTests
    {
        [UnityTest]
        public IEnumerator ActualShippingRigGpuRetainsWholeSpearAndEqualSourcePixelScale099()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "This is GPU evidence; run with graphics, not -nographics.");
            const int extent = 1024;
            var requested = Environment.GetEnvironmentVariable("SD_ELARA_FIT099_EVIDENCE");
            var parent = string.IsNullOrWhiteSpace(requested) ? Application.temporaryCachePath : requested;
            Assert.That(Path.IsPathRooted(parent), Is.True);
            var folder = Path.Combine(Path.GetFullPath(parent), "ElaraFit099_" + Guid.NewGuid().ToString("N"));
            Assert.That(Directory.Exists(folder), Is.False); Directory.CreateDirectory(folder);
            var target = new RenderTexture(extent, extent, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("Elara fit GPU camera099", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 30;
            camera.targetTexture = target; camera.transform.position = new Vector3(0, 0, -10);
            var canvasObject = new GameObject("Elara fit shipping canvas099", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var hostObject = new GameObject("Elara fit host099", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>(); host.SetParent(canvasObject.transform, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.pivot = new Vector2(0.5f, 0); host.sizeDelta = new Vector2(700, 560);
            host.anchoredPosition = new Vector2(0, -240);
            M2BattleActorRig072 actor = null; Texture2D readback = null;
            var priorTarget = RenderTexture.active;
            try
            {
                Assert.That(target.Create(), Is.True);
                actor = new M2BattleActorRig072(host,
                    new M2BattleUnionView { UnionId = "GPU_ELARA099", DisplayName = "Isolated art placement only" },
                    new M2BattleMemberView { MemberId = "HERO_REC_011", PortraitAuthorityId = "HERO_REC_011",
                        DisplayName = "Elara Steelsong", CurrentHp = 100, MaximumHp = 100 }, false);
                actor.SetLayout(new Vector2(0.5f, 0), new Vector2(300, 560), 1, 0);
                actor.ConfigureAlliedSilhouetteFit091(420, 260, 620);
                // Isolate body alpha from the separately authored shadow and lower
                // third, without replacing the shipping rig, sprite, or material.
                actor.GroundShadow076.gameObject.SetActive(false);
                actor.NamePlate076.gameObject.SetActive(false);
                foreach (var transform in canvasObject.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
                readback = new Texture2D(extent, extent, TextureFormat.RGBA32, false);
                Rect? idlePixels = null; float idleSourcePixelScale = 0;
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle, BattleArtPoseDirector011.ActionPrimary,
                    BattleArtPoseDirector011.Idle })
                {
                    Assert.That(actor.SetPoseImmediate(pose), Is.True);
                    Canvas.ForceUpdateCanvases(); yield return null;
                    Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, extent, extent), 0, 0); readback.Apply(false, false);
                    var pixels = AlphaBounds099(readback);
                    Assert.That(pixels.xMin, Is.GreaterThan(4)); Assert.That(pixels.xMax, Is.LessThan(extent - 4));
                    Assert.That(pixels.yMin, Is.GreaterThan(4)); Assert.That(pixels.yMax, Is.LessThan(extent - 4));
                    var sprite = actor.CurrentArtwork076.sprite;
                    var alpha = M1SilhouetteFraming091.VisibleRect091(sprite);
                    var sourcePixelScale = actor.CurrentArtwork076.rectTransform.rect.width / sprite.rect.width;
                    Assert.That(pixels.height, Is.EqualTo(alpha.height * sourcePixelScale).Within(7),
                        "GPU silhouette includes full authored alpha plus the rig rim, not a crop or blank quad.");
                    Assert.That(pixels.width, Is.EqualTo(alpha.width * sourcePixelScale).Within(7));
                    if (!idlePixels.HasValue)
                    { idlePixels = pixels; idleSourcePixelScale = sourcePixelScale; }
                    else
                    {
                        Assert.That(pixels.yMin, Is.EqualTo(idlePixels.Value.yMin).Within(4), "Feet stay anchored.");
                        Assert.That(sourcePixelScale, Is.EqualTo(idleSourcePixelScale).Within(0.0001f),
                            "Action body may lunge lower, but source pixels must not inflate by31%.");
                        if (pose == BattleArtPoseDirector011.ActionPrimary)
                            Assert.That(pixels.height / idlePixels.Value.height, Is.EqualTo(720f / 945f).Within(0.025f));
                        else Assert.That(pixels, Is.EqualTo(idlePixels.Value));
                    }
                    var path = Path.Combine(folder, pose == BattleArtPoseDirector011.Idle
                        ? "elara_idle_shipping_rig099.png" : "elara_action_shipping_rig099.png");
                    // Repeated idle verifies deterministic return; preserve its first proof.
                    if (!File.Exists(path)) File.WriteAllBytes(path, ImageConversion.EncodeToPNG(readback));
                }
                var message = "ELARA099_GPU_PLACEMENT_ONLY " + folder;
                LogAssert.Expect(LogType.Log, message); Debug.Log(message);
            }
            finally
            {
                RenderTexture.active = priorTarget; actor?.Dispose(); camera.targetTexture = null;
                target.Release(); Object.Destroy(target); if (readback != null) Object.Destroy(readback);
                Object.Destroy(canvasObject); Object.Destroy(cameraObject);
            }
        }

        static Rect AlphaBounds099(Texture2D image)
        {
            var pixels = image.GetPixels32(); var left = image.width; var bottom = image.height;
            var right = -1; var top = -1; var count = 0;
            for (var y = 0; y < image.height; y++) for (var x = 0; x < image.width; x++)
            {
                if (pixels[y * image.width + x].a < 16) continue;
                count++; left = Math.Min(left, x); right = Math.Max(right, x);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y);
            }
            Assert.That(count, Is.GreaterThan(5000), "A nonempty resource alone is not rendered art proof.");
            return Rect.MinMaxRect(left, bottom, right + 1, top + 1);
        }
    }
}
