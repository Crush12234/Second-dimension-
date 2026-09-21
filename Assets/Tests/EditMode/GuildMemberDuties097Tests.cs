using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildMemberDuties097Tests
    {
        private GameObject _host;
        private GameObject _body;
        private M1FlowPresenter _presenter;
        private readonly List<string> _commands = new List<string>();
        private static readonly MethodInfo Build097 = typeof(M1FlowPresenter).GetMethod(
            "BuildGuildMemberDuties097", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo Page097 = typeof(M1FlowPresenter).GetField(
            "_guildDutyPage097", BindingFlags.Instance | BindingFlags.NonPublic);

        [SetUp]
        public void Setup097()
        {
            _host = new GameObject("Duties Test 097", typeof(RectTransform));
            _presenter = _host.AddComponent<M1FlowPresenter>();
            _commands.Clear();
        }

        [TearDown]
        public void Cleanup097()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
        }

        [TestCase(7)]
        [TestCase(89)]
        [TestCase(310)]
        public void EveryMemberIsReachableExactlyOnceThroughActualPageButtons097(int count)
        {
            var assignments = Assignments097(count);
            var visited = new List<string>();
            var page = 0;
            while (true)
            {
                Render097(assignments);
                var reserve = Buttons097().Where(button => button.name.StartsWith("Reserve ", StringComparison.Ordinal)).ToArray();
                Assert.That(reserve.Length, Is.InRange(1, 6));
                foreach (var button in reserve) visited.Add(button.name.Substring("Reserve ".Length));
                var previous = Button097("Previous Member Duties 097");
                var next = Button097("Next Member Duties 097");
                Assert.That(previous.interactable, Is.EqualTo(page > 0));
                if (!next.interactable) break;
                next.onClick.Invoke();
                page++;
                Assert.That((int)Page097.GetValue(_presenter), Is.EqualTo(page));
                Assert.That(page, Is.LessThan(100));
            }
            Assert.That(visited, Is.EqualTo(assignments.Select(value => value.RecruitId)));
            Assert.That(visited.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(count));
            Assert.That(_commands, Is.Empty, "Paging must not assign, heal, reserve or spend anything.");
            Button097("Previous Member Duties 097").onClick.Invoke();
            Render097(assignments);
            Assert.That((int)Page097.GetValue(_presenter), Is.EqualTo(page - 1));
        }

        [Test]
        public void LateRecoveringMemberButtonsKeepExactIdAndExistingDutyKinds097()
        {
            var assignments = Assignments097(89);
            var late = assignments[88];
            late.RecruitId = "HERO_061_ASTER_BLOOMHEART_TEST097";
            late.RecruitName = "Aster Bloomheart";
            late.Kind = "Recovering";
            for (var page = 0; page < 14; page++)
            {
                Render097(assignments);
                Button097("Next Member Duties 097").onClick.Invoke();
            }
            Render097(assignments);
            Assert.That(_body.GetComponentsInChildren<Text>().Any(value =>
                value.name == "Member Duty Page Summary 097" && value.text.Contains("85–89 OF 89")), Is.True);
            Button097("Reserve " + late.RecruitId).onClick.Invoke();
            Button097("Train " + late.RecruitId).onClick.Invoke();
            Button097("Recover " + late.RecruitId).onClick.Invoke();
            Assert.That(_commands, Is.EqualTo(new[] {
                late.RecruitId + "|Reserve", late.RecruitId + "|Training", late.RecruitId + "|Recovering" }));
            Assert.That(late.Kind, Is.EqualTo("Recovering"), "The UI must not mutate its supplied assignment DTO.");
            Assert.That((int)Page097.GetValue(_presenter), Is.EqualTo(14));
        }

        [Test]
        public void EmptyAndShrinkingRostersClampPageWithoutHiddenEntries097()
        {
            Page097.SetValue(_presenter, 999);
            Render097(Assignments097(7));
            Assert.That((int)Page097.GetValue(_presenter), Is.EqualTo(1));
            Assert.That(Buttons097().Single(value => value.name.StartsWith("Reserve ", StringComparison.Ordinal)).name,
                Is.EqualTo("Reserve MEMBER_006"));
            Render097(Assignments097(1));
            Assert.That((int)Page097.GetValue(_presenter), Is.Zero);
            Assert.That(Buttons097().Any(value => value.name == "Next Member Duties 097"), Is.False);
            Render097(null);
            Assert.That(_body.GetComponentsInChildren<Text>().Any(value =>
                value.name == "Member Duty Page Summary 097" && value.text == "No member duties are available yet."), Is.True);
            Assert.That(Buttons097(), Is.Empty);
        }

        private void Render097(IReadOnlyList<GuildCityAssignmentView017D> assignments)
        {
            if (_body != null) UnityEngine.Object.DestroyImmediate(_body);
            _body = new GameObject("Duties Body 097", typeof(RectTransform));
            _body.transform.SetParent(_host.transform, false);
            Build097.Invoke(_presenter, new object[] { _body.transform, assignments,
                (Action<string, string>)((id, kind) => _commands.Add(id + "|" + kind)) });
        }
        private Button[] Buttons097() => _body.GetComponentsInChildren<Button>();
        private Button Button097(string name) => Buttons097().Single(value => value.name == name);
        private static GuildCityAssignmentView017D[] Assignments097(int count) => Enumerable.Range(0, count)
            .Select(index => new GuildCityAssignmentView017D {
                RecruitId = "MEMBER_" + index.ToString("D3"), RecruitName = "Member " + index,
                Kind = "Reserve", FacilityId = string.Empty
            }).ToArray();
    }
}
