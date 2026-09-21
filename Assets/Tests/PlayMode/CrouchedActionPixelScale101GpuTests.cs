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
    /// <summary>Actual shipping-rig render evidence, not a simulated battle outcome.</summary>
    public sealed class CrouchedActionPixelScale101GpuTests
    {
        [UnityTest]
        public IEnumerator ActualShippingRigRendersThreeCrouchesWithoutBodyInflation101()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "Run this GPU case without -nographics.");
            var evidenceParent = Environment.GetEnvironmentVariable("SD_CROUCH_FIT101_EVIDENCE");
            if (string.IsNullOrWhiteSpace(evidenceParent)) evidenceParent = Application.temporaryCachePath;
            Assert.That(Path.IsPathRooted(evidenceParent), Is.True);
            var folder = Path.Combine(Path.GetFullPath(evidenceParent), "CrouchFit101_" + Guid.NewGuid().ToString("N"));
            Assert.That(Directory.Exists(folder), Is.False);
            Directory.CreateDirectory(folder);
            const int extent = 1024;
            var target = new RenderTexture(extent, extent, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("Crouch pixel-scale camera101", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear; camera.cullingMask = 1 << 30;
            camera.targetTexture = target; camera.transform.position = new Vector3(0, 0, -10);
            var canvasObject = new GameObject("Shipping crouch scale canvas101", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            var hostObject = new GameObject("Crouch scale host101", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>(); host.SetParent(canvasObject.transform, false);
            host.anchorMin = host.anchorMax = new Vector2(0.5f, 0.5f);
            host.pivot = new Vector2(0.5f, 0); host.sizeDelta = new Vector2(900, 560);
            host.anchoredPosition = new Vector2(0, -240);
            var previous = RenderTexture.active;
            Texture2D readback = null; M2BattleActorRig072 actor = null;
            try
            {
                Assert.That(target.Create(), Is.True);
                readback = new Texture2D(extent, extent, TextureFormat.RGBA32, false);
                var identities = new[] { "HERO_REC_105", "HERO_REC_221", "HERO_REC_257" };
                var heightRatios = new[] { 863f / 981f, 791f / 868f, 837f / 974f };
                for (var hero = 0; hero < identities.Length; hero++)
                {
                    var identity = identities[hero];
                    actor = new M2BattleActorRig072(host,
                        new M2BattleUnionView { UnionId = "CROUCH_GPU101", DisplayName = "Art placement fixture only" },
                        new M2BattleMemberView { MemberId = identity, PortraitAuthorityId = identity,
                            DisplayName = identity, CurrentHp = 100, MaximumHp = 100 }, false);
                    actor.SetLayout(new Vector2(0.5f, 0), new Vector2(500, 560), 1, 0);
                    actor.ConfigureAlliedSilhouetteFit091(420, 800, 900);
                    actor.GroundShadow076.gameObject.SetActive(false);
                    actor.NamePlate076.gameObject.SetActive(false);
                    foreach (var transform in canvasObject.GetComponentsInChildren<Transform>(true))
                        transform.gameObject.layer = 30;
                    Rect idleBounds = default; float idlePixelScale = 0; byte[] firstIdlePng = null;
                    var poses = new[] { BattleArtPoseDirector011.Idle, BattleArtPoseDirector011.ActionPrimary,
                        BattleArtPoseDirector011.Idle };
                    for (var index = 0; index < poses.Length; index++)
                    {
                        Assert.That(actor.SetPoseImmediate(poses[index]), Is.True);
                        Canvas.ForceUpdateCanvases(); yield return null; Canvas.ForceUpdateCanvases();
                        camera.Render(); RenderTexture.active = target;
                        readback.ReadPixels(new Rect(0, 0, extent, extent), 0, 0); readback.Apply(false, false);
                        var pixels = AlphaBounds101(readback);
                        Assert.That(pixels.xMin, Is.GreaterThan(4)); Assert.That(pixels.xMax, Is.LessThan(extent - 4));
                        Assert.That(pixels.yMin, Is.GreaterThan(4)); Assert.That(pixels.yMax, Is.LessThan(extent - 4));
                        var sprite = actor.CurrentArtwork076.sprite;
                        var alpha = M1SilhouetteFraming091.VisibleRect091(sprite);
                        var pixelScale = actor.CurrentArtwork076.rectTransform.rect.width / sprite.rect.width;
                        Assert.That(actor.CurrentArtwork076.rectTransform.rect.height / sprite.rect.height,
                            Is.EqualTo(pixelScale).Within(0.0001f), "Never stretch either axis.");
                        Assert.That(pixels.height, Is.EqualTo(alpha.height * pixelScale).Within(7));
                        Assert.That(pixels.width, Is.EqualTo(alpha.width * pixelScale).Within(7),
                            "Rendered alpha includes the complete authored body and weapon, plus rim.");
                        Assert.That(actor.CurrentResourcePath,
                            Is.EqualTo(HeroRemasterAtlas093.Root093 + identity + "_PAIR_093" +
                                (index == 1 ? "#ACTION" : "#IDLE")));
                        var png = ImageConversion.EncodeToPNG(readback);
                        if (index == 0)
                        { idleBounds = pixels; idlePixelScale = pixelScale; firstIdlePng = png; }
                        else
                        {
                            Assert.That(pixelScale, Is.EqualTo(idlePixelScale).Within(0.0001f),
                                "Crouching lowers the silhouette, not magnifies the body.");
                            Assert.That(pixels.yMin, Is.EqualTo(idleBounds.yMin).Within(4), "Boots/hooves remain grounded.");
                            if (index == 1)
                                Assert.That(pixels.height / idleBounds.height,
                                    Is.EqualTo(heightRatios[hero]).Within(0.025f));
                            else
                            {
                                Assert.That(pixels, Is.EqualTo(idleBounds));
                                Assert.That(png, Is.EqualTo(firstIdlePng), "Deterministic restored idle, no stale action.");
                            }
                        }
                        var name = identity + "_" + index + (index == 1 ? "_action101.png" : "_idle101.png");
                        File.WriteAllBytes(Path.Combine(folder, name), png);
                    }
                    actor.Dispose(); actor = null; yield return null;
                }
                var message = "CROUCH101_GPU_PLACEMENT_ONLY " + folder;
                LogAssert.Expect(LogType.Log, message); Debug.Log(message);
            }
            finally
            {
                RenderTexture.active = previous; actor?.Dispose(); camera.targetTexture = null;
                target.Release(); Object.Destroy(target); if (readback != null) Object.Destroy(readback);
                Object.Destroy(canvasObject); Object.Destroy(cameraObject);
            }
        }

        static Rect AlphaBounds101(Texture2D texture)
        {
            var pixels = texture.GetPixels32(); var left = texture.width; var right = -1;
            var bottom = texture.height; var top = -1; var count = 0;
            for (var y = 0; y < texture.height; y++) for (var x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a < 16) continue;
                count++; left = Math.Min(left, x); right = Math.Max(right, x);
                bottom = Math.Min(bottom, y); top = Math.Max(top, y);
            }
            Assert.That(count, Is.GreaterThan(5000), "Non-null sprites alone are not rendered evidence.");
            return Rect.MinMaxRect(left, bottom, right + 1, top + 1);
        }
    }
}
