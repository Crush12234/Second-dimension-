using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class GuildMemberDutyRoute097Tests
    {
        private string _savePath;
        private GameObject _host;
        private M1RuntimeCoordinator _coordinator;
        private M1FlowPresenter _presenter;

        [SetUp]
        public void SetUp097()
        {
            _savePath = Path.Combine(Path.GetTempPath(), "sd_duty_route_097_" +
                Guid.NewGuid().ToString("N") + ".json");
            // A fresh Fast Charter has no members. Exercise paging with the
            // existing earned multi-member fixture, copied without edits.
            File.Copy(Path.Combine(Application.dataPath, "Tests", "Fixtures",
                "EarnedChapter001094.fixture.json"), _savePath, false);
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _savePath);
            _host = new GameObject("Guild Member Duty Route Test 097");
            _presenter = _host.AddComponent<M1FlowPresenter>();
            _presenter.enabled = false;
            _presenter.Initialize(_coordinator);
            // Only establish the starting home. All route changes below use the
            // shipping buttons and callbacks, never the duty-panel builder.
            typeof(M1FlowPresenter).GetMethod("ReturnToWalkableHall069",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, null);
        }

        [TearDown]
        public void TearDown097()
        {
            if (_presenter != null)
            {
                var canvas = Field097<Canvas>("_canvas");
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (File.Exists(_savePath)) File.Delete(_savePath);
            if (File.Exists(_savePath + ".bak")) File.Delete(_savePath + ".bak");
        }

        [Test]
        public void CurrentFourActionHomeOpensHeroesThenLivePagedDutiesAndReturnsWithoutSaveMutation110()
        {
            var before = File.ReadAllBytes(_savePath);
            var expected = ((IGuildCityPresentationCoordinator017D)_coordinator).GuildCity017D.Assignments
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId))
                .Select(value => value.RecruitId).ToArray();
            Assert.That(expected, Is.Not.Empty);
            Assert.That(Buttons097().Count(value => value.name.StartsWith(
                "Living Guild Hub Facility ", StringComparison.Ordinal)), Is.EqualTo(4));

            Click097("Living Guild Hub Facility " + WalkableGuildHall069.PartyDestinationId069 + " 074");
            Assert.That(Root097().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == "Union Planner 074"), Is.True);
            var duties = Button097("Union Planner Member Duties 097");
            Assert.That(duties.interactable, Is.True);
            Assert.That(Buttons097().Any(value => value != duties &&
                (value.navigation.selectOnLeft == duties || value.navigation.selectOnRight == duties)),
                Is.True, "The new shortcut must also enter the existing controller navigation chain.");
            duties.onClick.Invoke();

            Assert.That(Field097<string>("_guildCityTab017D"), Is.EqualTo("DUTIES"));
            Assert.That(Root097().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == "Parallel Member Assignments 017D"), Is.True);
            Assert.That(Root097().GetComponentsInChildren<Text>(false)
                .Single(value => value.name == "Screen Title [Responsive 062]").text, Is.EqualTo("MEMBER DUTIES"));
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Field097<RectTransform>("_activeContent"));
            var pageSummary097 = Root097().GetComponentsInChildren<Text>(false)
                .Single(value => value.name == "Member Duty Page Summary 097");
            Assert.That(pageSummary097.GetComponent<LayoutElement>().minHeight, Is.GreaterThanOrEqualTo(48f));
            Assert.That(pageSummary097.rectTransform.rect.height, Is.GreaterThanOrEqualTo(48f),
                "The page counter must have visible line height in the actual scroll layout.");
            Assert.That(pageSummary097.text, Does.Contain("PAGE 1 /"));

            var seen = new List<string>();
            var pages = (expected.Length + 5) / 6;
            for (var page = 0; page < pages; page++)
            {
                Assert.That(Field097<string>("_guildCityTab017D"), Is.EqualTo("DUTIES"),
                    "The real page callback must return to the same live duty dispatcher.");
                var actualPage = Buttons097().Where(value => value.name.StartsWith("Reserve ",
                    StringComparison.Ordinal)).Select(value => value.name.Substring("Reserve ".Length)).ToArray();
                Assert.That(actualPage, Is.EqualTo(expected.Skip(page * 6).Take(6).ToArray()));
                seen.AddRange(actualPage);
                if (page + 1 < pages) Click097("Next Member Duties 097");
            }
            Assert.That(seen, Is.EqualTo(expected));
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before),
                "Navigation and paging must not change recovery, Union membership, XP, or the save.");

            Click097("Back");
            Assert.That(Root097().GetComponentsInChildren<RectTransform>(false)
                .Any(value => value.name == "Union Planner 074"), Is.True,
                "Back must return to the planner even when entered through the current Guild Home.");
            Assert.That(Button097("Union Planner Member Duties 097").interactable, Is.True);
            Assert.That(File.ReadAllBytes(_savePath), Is.EqualTo(before));
        }

        private T Field097<T>(string name) => (T)typeof(M1FlowPresenter)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);
        private RectTransform Root097() => Field097<RectTransform>("_screenRoot");
        private Button[] Buttons097() => Root097().GetComponentsInChildren<Button>(false);
        private Button Button097(string name) => Buttons097().Single(value => value.name == name);
        private void Click097(string name)
        {
            var button = Button097(name);
            Assert.That(button.interactable, Is.True, name);
            button.onClick.Invoke();
        }
    }
}
