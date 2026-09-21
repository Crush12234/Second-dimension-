using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class StoryQuestInitialViewportPlayModeTests
    {
        string _source, _sourceHash, _directory, _save;
        GameObject _host;
        M1FlowPresenter _presenter;
        M1RuntimeCoordinator _coordinator;

        [UnityTest, Timeout(180000)]
        public IEnumerator Recovered61FirstStoryActionFitsInitialViewportAtBothAspects()
        {
            return VerifyInitialViewport(
                "SD_STORY_VIEWPORT_RECOVERED61_SOURCE",
                @"C:\SecondDimension\BuildEvidence\Reset110\R343_ExistingRun_RecoveryOnly\EarnedReviewSave097.json",
                "57EF125D1DA09D6E586024E829E988BC9D481E11C0C7A5A82701229FC503DB98",
                "CH018_001");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator Earned69SecondStoryActionFitsInitialViewportAtBothAspects()
        {
            return VerifyInitialViewport(
                "SD_STORY_VIEWPORT_EARNED69_SOURCE",
                @"C:\SecondDimension\BuildEvidence\Reset110\R280_GateA_NativeReload\EarnedReviewSave097.json",
                "D605FB18B9945A4F1BC7816909A6F775D7F489E2073B1842DFF00BB10F92424B",
                "CH018_002");
        }

        IEnumerator VerifyInitialViewport(string sourceVariable, string defaultSource,
            string expectedSourceHash, string chapterId)
        {
            var requested = Environment.GetEnvironmentVariable(sourceVariable);
            _source = string.IsNullOrWhiteSpace(requested) ? defaultSource : Path.GetFullPath(requested);
            _sourceHash = expectedSourceHash;
            if (!File.Exists(_source) && string.IsNullOrWhiteSpace(requested))
                Assert.Ignore("Preserved native story evidence is not available on this machine.");
            Assert.That(File.Exists(_source), Is.True, "An explicit evidence path must exist.");
            Assert.That(Hash(_source), Is.EqualTo(_sourceHash));
            _directory = Path.Combine(Path.GetTempPath(), "sd_story_viewport_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _save = Path.Combine(_directory, "StoryCopy.json");
            File.Copy(_source, _save);
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _save);
            var chapter = _coordinator.Campaign019.Chapters.Single(value => value.ChapterId == chapterId);
            Assert.That(chapter.Status, Is.EqualTo("AVAILABLE"));
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Null.Or.Empty);
            Assert.That(_coordinator.CampaignWorldGate023.ActiveOperationId, Is.Null.Or.Empty);
            var before = File.ReadAllBytes(_save);
            var backup = File.Exists(_save + ".bak") ? File.ReadAllBytes(_save + ".bak") : null;

            _host = new GameObject("Story Quest Initial Viewport Test");
            _presenter = _host.AddComponent<M1FlowPresenter>();
            _presenter.enabled = false;
            _presenter.Initialize(_coordinator);
            foreach (var resolution in new[] { new Vector2(1920f, 1080f), new Vector2(1280f, 800f) })
            {
                ConfigureCanvas(resolution);
                Invoke("ReturnToWalkableHall069");
                var missions = Button("Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074");
                Assert.That(missions.gameObject.activeInHierarchy && missions.IsInteractable(), Is.True);
                missions.onClick.Invoke();
                yield return null;
                yield return null;
                Canvas.ForceUpdateCanvases();

                Assert.That(Field<string>("_guildCityTab017D"), Is.EqualTo("CAMPAIGN"));
                var start = Button("Start compact playable campaign chapter " + chapterId + " 084");
                Assert.That(start.gameObject.activeInHierarchy && start.IsInteractable(), Is.True);
                Assert.That(start.GetComponentInChildren<Text>().text, Is.EqualTo("START THIS STORY QUEST"));
                var scroll = Field<ScrollRect>("_activeScroll");
                Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0f).Within(0.5f),
                    "The proof must use the initial page position, without scrolling to reveal the action.");
                var action = start.GetComponent<RectTransform>();
                var label = start.GetComponentInChildren<Text>().rectTransform;
                AssertInside(scroll.viewport, action, resolution + " entire Start action");
                AssertInside(scroll.viewport, label, resolution + " entire Start label");
                var dock = Field<RectTransform>("_screenRoot").GetComponentsInChildren<RectTransform>(false)
                    .Single(value => value.name == "Guild Mobile Bottom Navigation 062");
                Assert.That(Bounds(action).yMin, Is.GreaterThanOrEqualTo(Bounds(dock).yMax - 0.5f),
                    resolution + " the action must remain above the persistent bottom navigation");

                var texts = Field<RectTransform>("_screenRoot").GetComponentsInChildren<Text>(false);
                Assert.That(texts.Any(value => value.text.Contains("CAMPAIGN PROGRESS")), Is.False,
                    "Raw campaign flags must not be presented as a count of completed story quests.");
                var title = start.transform.parent.GetComponentsInChildren<Text>(false)
                    .Single(value => value.name == "Title [Responsive 062]" &&
                        value.text == "NEW QUEST  •  " + chapter.Title.ToUpperInvariant());
                Assert.That(title.text, Does.Contain(chapter.Title.ToUpperInvariant()));
                Assert.That(title.color.r, Is.GreaterThan(0.85f));
                Assert.That(title.color.g, Is.GreaterThan(0.85f));
                Assert.That(title.color.b, Is.GreaterThan(0.85f),
                    "The dark story card needs a light readable heading, not the dark button fill color.");
                CollectionAssert.AreEqual(before, File.ReadAllBytes(_save));
                if (backup == null) Assert.That(File.Exists(_save + ".bak"), Is.False);
                else CollectionAssert.AreEqual(backup, File.ReadAllBytes(_save + ".bak"));
                Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Null.Or.Empty,
                    "Opening or resizing the story list must never start its available quest.");
            }
        }

        void ConfigureCanvas(Vector2 resolution)
        {
            var canvas = Field<Canvas>("_canvas");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.WorldSpace;
            var size = M1FlowPresenter.ExpeditionCanvasSizeForVerification074(resolution.x, resolution.y);
            var rect = canvas.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
            rect.localScale = Vector3.one;
            Canvas.ForceUpdateCanvases();
            Assert.That(rect.rect.width / rect.rect.height,
                Is.EqualTo(resolution.x / resolution.y).Within(0.001f));
        }

        static Rect Bounds(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(value => value.x), corners.Min(value => value.y),
                corners.Max(value => value.x), corners.Max(value => value.y));
        }

        static void AssertInside(RectTransform outer, RectTransform inner, string description)
        {
            var containing = Bounds(outer);
            var content = Bounds(inner);
            Assert.That(content.width, Is.GreaterThan(0f), description);
            Assert.That(content.height, Is.GreaterThan(0f), description);
            Assert.That(content.xMin, Is.GreaterThanOrEqualTo(containing.xMin - 0.5f), description + " left");
            Assert.That(content.xMax, Is.LessThanOrEqualTo(containing.xMax + 0.5f), description + " right");
            Assert.That(content.yMin, Is.GreaterThanOrEqualTo(containing.yMin - 0.5f), description + " bottom");
            Assert.That(content.yMax, Is.LessThanOrEqualTo(containing.yMax + 0.5f), description + " top");
        }

        Button Button(string name) => Field<RectTransform>("_screenRoot")
            .GetComponentsInChildren<Button>(false).Single(value => value.name == name);
        T Field<T>(string name) => (T)typeof(M1FlowPresenter).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);
        void Invoke(string name) => typeof(M1FlowPresenter).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, null);
        static string Hash(string path)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            if (_presenter != null)
            {
                var canvas = Field<Canvas>("_canvas");
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (_directory != null && Directory.Exists(_directory))
            {
                var resolved = Path.GetFullPath(_directory);
                Assert.That(resolved.StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\', '/') +
                    Path.DirectorySeparatorChar + "sd_story_viewport_", StringComparison.OrdinalIgnoreCase), Is.True);
                Directory.Delete(resolved, true);
            }
            if (_source != null && File.Exists(_source)) Assert.That(Hash(_source), Is.EqualTo(_sourceHash));
        }
    }
}
