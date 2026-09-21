using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class EnemyRemasterTheme098GpuPlayModeTests
    {
        const int Width098 = 384, Height098 = 512;
        [UnitySetUp] public IEnumerator SetUp098()
        { EnemyArt700Runtime090.ResetForTests090(); yield return null; }
        [UnityTearDown] public IEnumerator TearDown098()
        { EnemyArt700Runtime090.ResetForTests090(); yield return null; LogAssert.NoUnexpectedReceived(); }

        [UnityTest]
        public IEnumerator TenActualActorMaterialsRenderDistinctThemesWithIdenticalAlphaAndNeutralMetal098()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null),
                "This is actual GPU evidence; run with a graphics device, not -nographics.");
            var hostObject = new GameObject("Ten Real Myrmidon Material Renders098", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            host.sizeDelta = new Vector2(440, 700);
            var rendered = new List<Color32[]>();
            M2BattleActorRig072 actor = null;
            try
            {
                for (var index = 1; index <= 10; index++)
                {
                    actor = CreateActor098(host, index);
                    Assert.That(actor.CurrentEnemyRemasterVariantId098,
                        Is.EqualTo("ENEMY_REC_021_VAR_" + index.ToString("00")));
                    Assert.That(actor.CurrentArtwork076.color, Is.EqualTo(Color.white));
                    rendered.Add(Render098(actor.CurrentArtwork076.sprite, actor.CurrentArtwork076.material));
                    actor.Dispose(); actor = null;
                    yield return null;
                }
                var original = rendered[2]; // VAR03 deliberately retains the reviewed original colors.
                var opaque = 0;
                var neutral = 0;
                var alphaMismatches = new int[10];
                var neutralMismatches = new int[10];
                for (var pixel = 0; pixel < original.Length; pixel++)
                {
                    var source = original[pixel];
                    if (source.a >= 250) opaque++;
                    var maximum = Math.Max(source.r, Math.Max(source.g, source.b));
                    var minimum = Math.Min(source.r, Math.Min(source.g, source.b));
                    var lowSaturationMetal = source.a >= 250 && maximum >= 16 && maximum - minimum <= 4;
                    if (lowSaturationMetal) neutral++;
                    for (var variant = 0; variant < rendered.Count; variant++)
                    {
                        var actual = rendered[variant][pixel];
                        if (actual.a != source.a) alphaMismatches[variant]++;
                        if (lowSaturationMetal)
                        {
                            if (Math.Abs(actual.r - source.r) > 1 || Math.Abs(actual.g - source.g) > 1 ||
                                Math.Abs(actual.b - source.b) > 1) neutralMismatches[variant]++;
                        }
                    }
                }
                Assert.That(opaque, Is.GreaterThan(10000), "The test must render the actual creature, not a blank target.");
                Assert.That(neutral, Is.GreaterThan(100), "Neutral armor preservation must be exercised by real sprite pixels.");
                for (var index = 0; index < 10; index++)
                {
                    Assert.That(alphaMismatches[index], Is.Zero, "Theme " + (index + 1) + " altered cutout alpha.");
                    Assert.That(neutralMismatches[index], Is.Zero, "Theme " + (index + 1) + " altered neutral armor.");
                }
                for (var first = 0; first < 10; first++)
                for (var second = first + 1; second < 10; second++)
                {
                    long rgbDifference = 0;
                    var visiblyChangedPixels = 0;
                    for (var pixel = 0; pixel < original.Length; pixel++)
                    {
                        if (original[pixel].a < 250) continue;
                        var a = rendered[first][pixel]; var b = rendered[second][pixel];
                        var delta = Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b);
                        rgbDifference += delta;
                        if (delta >= 24) visiblyChangedPixels++;
                    }
                    var pair = "Themes " + (first + 1) + " and " + (second + 1);
                    Assert.That(visiblyChangedPixels, Is.GreaterThan(opaque * 0.02f),
                        pair + " must visibly differ on meaningful body area, not a label or isolated pixel.");
                    Assert.That((double)rgbDifference / (opaque * 3.0 * 255.0), Is.GreaterThan(0.008),
                        pair + " must retain perceptible material variation.");
                }
                LogAssert.NoUnexpectedReceived();
            }
            finally { actor?.Dispose(); UnityEngine.Object.Destroy(hostObject); EnemyArt700Runtime090.ClearCache090(); }
        }

        [UnityTest]
        public IEnumerator DefaultDisabledUniformsDoNotAlterOrdinarySpriteOrReviewedVar03Output098()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            var shader = Resources.Load<Shader>(M2BattleActorRig072.EnemyCutoutShaderResource075);
            Assert.That(shader != null && shader.isSupported, Is.True);
            var defaults = new Material(shader);
            var disabled = new Material(shader);
            try
            {
                Assert.That(defaults.GetFloat(EnemyRemasterTheme098.AmountProperty098), Is.Zero);
                disabled.SetFloat(EnemyRemasterTheme098.AmountProperty098, 0f);
                disabled.SetColor(EnemyRemasterTheme098.AccentProperty098, new Color(0f, 1f, 0f, 0.13f));
                foreach (var pair in new[] { new[] { "ENEMY_REC_035", "ENEMY_REC_035_VAR_05" },
                    new[] { "ENEMY_REC_021", "ENEMY_REC_021_VAR_03" } })
                {
                    Assert.That(EnemyArt700Runtime090.TryLoadSprite090(pair[0], pair[1], EnemyArt700Pose090.Idle,
                        out var sprite, out var error), Is.True, error);
                    var baseline = Render098(sprite, defaults);
                    Assert.That(Render098(sprite, disabled), Is.EqualTo(baseline),
                        "Disabled optional parameters must be byte-identical even with arbitrary accent/alpha color.");
                    if (pair[0] == "ENEMY_REC_021")
                    {
                        Assert.That(EnemyRemasterTheme098.ApplyToMaterial098(disabled, pair[0], pair[1], sprite, out _), Is.True);
                        Assert.That(Render098(sprite, disabled), Is.EqualTo(baseline),
                            "Armored03 amount zero preserves the reviewed generated colors.");
                    }
                }
                yield return null;
                LogAssert.NoUnexpectedReceived();
            }
            finally { UnityEngine.Object.Destroy(defaults); UnityEngine.Object.Destroy(disabled); }
        }

        [UnityTest]
        public IEnumerator Swift04ShippingCrossfadeKeepsExactTurquoiseMaterialAndVariantIdentity098()
        {
            var hostObject = new GameObject("Swift04 Shipping Crossfade098", typeof(RectTransform));
            M2BattleActorRig072 actor = null;
            try
            {
                actor = CreateActor098(hostObject.GetComponent<RectTransform>(), 4);
                var material = actor.CurrentArtwork076.material;
                var accent = material.GetColor(EnemyRemasterTheme098.AccentProperty098);
                Assert.That(accent.r, Is.EqualTo(0.10f).Within(0.00001f));
                Assert.That(accent.g, Is.EqualTo(1f).Within(0.00001f));
                Assert.That(accent.b, Is.EqualTo(0.62f).Within(0.00001f));
                Assert.That(accent.a, Is.EqualTo(1f).Within(0.00001f));
                Assert.That(material.GetFloat(EnemyRemasterTheme098.AmountProperty098), Is.EqualTo(1f));
                yield return actor.CrossfadeToPose(BattleArtPoseDirector011.ActionPrimary, 0.08f, () => 1f, () => false);
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(EnemyArtRemaster098.ResourcePath098 + "#ACTION"));
                Assert.That(actor.CurrentEnemyRemasterThemeName098, Is.EqualTo("Swift"));
                Assert.That(actor.CurrentEnemyRemasterVariantId098, Is.EqualTo("ENEMY_REC_021_VAR_04"));
                Assert.That(actor.CurrentArtwork076.material, Is.SameAs(material));
                Assert.That(material.GetColor(EnemyRemasterTheme098.AccentProperty098), Is.EqualTo(accent));
                yield return actor.CrossfadeToPose(BattleArtPoseDirector011.Idle, 0.08f, () => 1f, () => false);
                Assert.That(actor.CurrentResourcePath, Is.EqualTo(EnemyArtRemaster098.ResourcePath098 + "#IDLE"));
                Assert.That(actor.CurrentArtwork076.material, Is.SameAs(material));
                Assert.That(actor.MissingPoseCount, Is.Zero);
                LogAssert.NoUnexpectedReceived();
            }
            finally { actor?.Dispose(); UnityEngine.Object.Destroy(hostObject); }
        }

        [UnityTest]
        public IEnumerator SaveFreshContactSheetFromTwentyActualCanvasActorPoses098()
        {
            Assert.That(SystemInfo.graphicsDeviceType, Is.Not.EqualTo(GraphicsDeviceType.Null));
            var requested = Environment.GetEnvironmentVariable("SD_ENEMY_THEME098_EVIDENCE");
            var parent = string.IsNullOrWhiteSpace(requested)
                ? Path.Combine(Application.temporaryCachePath, "EnemyTheme098") : requested;
            Assert.That(Path.IsPathRooted(parent), Is.True);
            var folder = Path.Combine(Path.GetFullPath(parent), "Myrmidon_" +
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + "_" + Guid.NewGuid().ToString("N"));
            Assert.That(Directory.Exists(folder), Is.False, "Never overwrite an earlier visual proof.");
            Directory.CreateDirectory(folder);
            const int sheetWidth = 2400, sheetHeight = 1240;
            var target = new RenderTexture(sheetWidth, sheetHeight, 24, RenderTextureFormat.ARGB32);
            var cameraObject = new GameObject("Myrmidon Actual Canvas QA Camera098", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.018f, 0.028f, 0.045f, 1f);
            camera.cullingMask = 1 << 30; camera.targetTexture = target;
            camera.transform.position = new Vector3(0, 0, -10);
            var canvasObject = new GameObject("Myrmidon Actual Shipping Rig Contact Canvas098",
                typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            var actors = new List<M2BattleActorRig072>();
            var records = new List<ContactPose098>();
            Texture2D readback = null;
            var previousTarget = RenderTexture.active;
            try
            {
                Assert.That(target.Create(), Is.True);
                for (var index = 1; index <= 10; index++)
                for (var poseIndex = 0; poseIndex < 2; poseIndex++)
                {
                    var column = ((index - 1) % 5) * 2 + poseIndex;
                    var row = (index - 1) / 5;
                    var hostObject = new GameObject("Exact Variant " + index + " Pose " + poseIndex, typeof(RectTransform));
                    var host = hostObject.GetComponent<RectTransform>();
                    host.SetParent(canvasObject.transform, false);
                    host.anchorMin = host.anchorMax = new Vector2(0, 1);
                    host.pivot = new Vector2(0, 1); host.sizeDelta = new Vector2(240, 620);
                    host.anchoredPosition = new Vector2(column * 240, -row * 620);
                    var actor = CreateActor098(host, index); actors.Add(actor);
                    actor.SetLayout(new Vector2(0.5f, 0), new Vector2(224, 530), 1f, 0);
                    actor.SetStageOffset(new Vector2(0, 22));
                    Canvas.ForceUpdateCanvases();
                    Assert.That(actor.SetPoseImmediate(poseIndex == 1 ? BattleArtPoseDirector011.ActionPrimary :
                        BattleArtPoseDirector011.Idle), Is.True, "Reframe either pose at its actual final Canvas size.");
                    var labelObject = new GameObject("Actual Variant Label098", typeof(RectTransform), typeof(Text));
                    var labelRect = labelObject.GetComponent<RectTransform>();
                    labelRect.SetParent(host, false); labelRect.anchorMin = new Vector2(0, 1); labelRect.anchorMax = Vector2.one;
                    labelRect.pivot = new Vector2(0.5f, 1); labelRect.anchoredPosition = new Vector2(0, -8);
                    labelRect.sizeDelta = new Vector2(0, 58);
                    var label = labelObject.GetComponent<Text>();
                    label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    label.fontSize = 20; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white;
                    label.text = index.ToString("00") + " " + actor.CurrentEnemyRemasterThemeName098 + "\n" +
                        (poseIndex == 0 ? "IDLE" : "ACTION");
                    records.Add(new ContactPose098 { variantId = actor.CurrentEnemyRemasterVariantId098,
                        theme = actor.CurrentEnemyRemasterThemeName098, pose = actor.CurrentPoseId,
                        resource = actor.CurrentResourcePath, x = column * 240, y = row * 620 });
                }
                foreach (var transform in canvasObject.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 30;
                Canvas.ForceUpdateCanvases();
                yield return null;
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active = target;
                readback = new Texture2D(sheetWidth, sheetHeight, TextureFormat.RGB24, false);
                readback.ReadPixels(new Rect(0, 0, sheetWidth, sheetHeight), 0, 0); readback.Apply(false, false);
                var png = ImageConversion.EncodeToPNG(readback);
                Assert.That(png.Length, Is.GreaterThan(50000), "A near-empty sheet is not visual proof.");
                File.WriteAllBytes(Path.Combine(folder, "myrmidon_actual_rigs_idle_action098.png"), png);
                File.WriteAllText(Path.Combine(folder, "contact098.json"), JsonUtility.ToJson(new ContactReport098
                { scope = "Actual shipping Canvas actor renders: one repaired body, ten material variants. Not natural victories or ten drawings.",
                    width = sheetWidth, height = sheetHeight, poses = records.ToArray() }, true));
                var evidenceMessage = "ENEMY_THEME098_ACTUAL_RIG_CONTACT_SHEET " +
                    Path.Combine(folder, "myrmidon_actual_rigs_idle_action098.png");
                LogAssert.Expect(LogType.Log, evidenceMessage);
                Debug.Log(evidenceMessage);
                Assert.That(records.Count, Is.EqualTo(20));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                RenderTexture.active = previousTarget;
                foreach (var actor in actors) actor.Dispose();
                camera.targetTexture = null; target.Release();
                UnityEngine.Object.Destroy(target); if (readback != null) UnityEngine.Object.Destroy(readback);
                UnityEngine.Object.Destroy(canvasObject); UnityEngine.Object.Destroy(cameraObject);
                EnemyArt700Runtime090.ClearCache090();
            }
        }

        [Serializable] sealed class ContactPose098
        { public string variantId, theme, pose, resource; public int x, y; }
        [Serializable] sealed class ContactReport098
        { public string scope; public int width, height; public ContactPose098[] poses; }

        static M2BattleActorRig072 CreateActor098(RectTransform host, int index) => new M2BattleActorRig072(host,
            new M2BattleUnionView { UnionId = "GPU_UNION098", DisplayName = "Material-only fixture" },
            new M2BattleMemberView { MemberId = "GPU_MYRMIDON098_" + index, DisplayName = "V" + index.ToString("00"),
                CurrentHp = 300, MaximumHp = 300, EnemyArtBaseId090 = "ENEMY_REC_021",
                EnemyArtVariantId090 = "ENEMY_REC_021_VAR_" + index.ToString("00"), EnemyThreatTier089 = 10 }, true);

        static Color32[] Render098(Sprite sprite, Material material)
        {
            var target = new RenderTexture(Width098, Height098, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Width098, Height098, TextureFormat.RGBA32, false, true);
            var previous = RenderTexture.active;
            try
            {
                target.Create(); RenderTexture.active = target;
                GL.Clear(true, true, Color.clear);
                GL.PushMatrix();
                try
                {
                    GL.LoadPixelMatrix(0, Width098, Height098, 0);
                    var rect = sprite.textureRect;
                    var scale = Mathf.Min(Width098 / rect.width, Height098 / rect.height);
                    var width = rect.width * scale; var height = rect.height * scale;
                    Graphics.DrawTexture(new Rect((Width098 - width) * 0.5f, (Height098 - height) * 0.5f, width, height),
                        sprite.texture, new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
                            rect.width / sprite.texture.width, rect.height / sprite.texture.height),
                        0, 0, 0, 0, Color.white, material);
                }
                finally { GL.PopMatrix(); }
                readback.ReadPixels(new Rect(0, 0, Width098, Height098), 0, 0);
                readback.Apply(false, false);
                return readback.GetPixels32();
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(readback);
            }
        }
    }
}
