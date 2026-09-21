using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class MenuPresentationPolish091Tests
    {
        private GameObject _root;
        private GameObject _ownedEventSystem;
        private Button _button;
        private RectTransform _label;
        private Component _feedback;
        private PointerEventData _pointer;
        private static Type FeedbackType091 => typeof(M1FlowPresenter).Assembly.GetType(
            "SecondDimension.Presentation.M1ButtonFeedback091", true);

        [SetUp]
        public void SetUp091()
        {
            _root = new GameObject("Menu Presentation Polish Test 091",
                typeof(RectTransform), typeof(Image), typeof(Button));
            _button = _root.GetComponent<Button>();
            _button.targetGraphic = _root.GetComponent<Image>();
            _root.transform.localScale = new Vector3(1.2f, 0.9f, 1f);
            var labelObject = new GameObject("Label [Responsive 062]", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(_root.transform, false);
            _label = labelObject.GetComponent<RectTransform>();
            _label.localScale = new Vector3(0.85f, 1.1f, 1f);
            var text = labelObject.GetComponent<Text>();
            text.text = "RECRUIT MEMBER";
            text.raycastTarget = false;

            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                _ownedEventSystem = new GameObject("Menu Presentation EventSystem Test 091", typeof(EventSystem));
                eventSystem = _ownedEventSystem.GetComponent<EventSystem>();
            }
            _pointer = new PointerEventData(eventSystem)
            {
                button = PointerEventData.InputButton.Left,
                pointerId = -1
            };
            _feedback = _root.AddComponent(FeedbackType091);
            ConfigureFeedback091();
        }

        [TearDown]
        public void TearDown091()
        {
            UnityEngine.Object.DestroyImmediate(_root);
            if (_ownedEventSystem != null) UnityEngine.Object.DestroyImmediate(_ownedEventSystem);
        }

        [Test]
        public void ClickIsImmediateExactlyOnceWhileFeedbackIsActive091()
        {
            var clicks = 0;
            _button.onClick.AddListener(() => clicks++);
            ((IPointerDownHandler)_feedback).OnPointerDown(_pointer);
            Tick091(0.05f);
            ((IPointerUpHandler)_feedback).OnPointerUp(_pointer);
            ExecuteEvents.Execute(_root, _pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clicks, Is.EqualTo(1),
                "The existing Button action must execute synchronously; a visual pulse cannot delay it.");

            for (var index = 0; index < 60; index++) Tick091(1f / 60f);
            Assert.That(clicks, Is.EqualTo(1), "Completing the pulse must never replay a command.");
            Assert.That(TouchLight091.raycastTarget, Is.False);
            Assert.That(TouchLight091.GetComponent<LayoutElement>().ignoreLayout, Is.True);
        }

        [Test]
        public void PressAndReleaseAnimateResponsiveLabelWithoutMovingCardRoot091()
        {
            var rootScale = _root.transform.localScale;
            var homeScale = _label.localScale;
            ((IPointerEnterHandler)_feedback).OnPointerEnter(_pointer);
            ((IPointerDownHandler)_feedback).OnPointerDown(_pointer);
            Tick091(0.08f);
            var heldScale = _label.localScale;
            Assert.That(heldScale.x, Is.LessThan(homeScale.x));
            Assert.That(TouchLight091.color.a, Is.GreaterThan(0f));
            Assert.That(_root.transform.localScale, Is.EqualTo(rootScale),
                "The shared effect must not fight board flips or facility-card transform animations.");

            ((IPointerUpHandler)_feedback).OnPointerUp(_pointer);
            Tick091(0.08f);
            Assert.That(_label.localScale.x, Is.GreaterThan(heldScale.x));
            ((IPointerExitHandler)_feedback).OnPointerExit(_pointer);
            for (var index = 0; index < 90; index++) Tick091(1f / 60f);
            Assert.That(_label.localScale, Is.EqualTo(homeScale));
            Assert.That(_root.transform.localScale, Is.EqualTo(rootScale));
        }

        [Test]
        public void DisablingControlClearsPressAndFocusBeforeReenable091()
        {
            var homeScale = _label.localScale;
            ((ISelectHandler)_feedback).OnSelect(_pointer);
            ((IPointerDownHandler)_feedback).OnPointerDown(_pointer);
            Tick091(0.1f);
            _button.interactable = false;
            Tick091(0.1f);
            Assert.That(_label.localScale, Is.EqualTo(homeScale));
            Assert.That(TouchLight091.color.a, Is.Zero);

            _button.interactable = true;
            Tick091(0.1f);
            Assert.That(_label.localScale, Is.EqualTo(homeScale));
            Assert.That(TouchLight091.color.a, Is.Zero,
                "A disabled menu cannot retain old hover, focus or press illumination.");
        }

        [Test]
        public void NavigationCleanupRestoresRestPoseAndRestylingKeepsOneOverlay091()
        {
            var homeScale = _label.localScale;
            ((IPointerDownHandler)_feedback).OnPointerDown(_pointer);
            Tick091(0.1f);
            Invoke091("OnDisable");
            Assert.That(_label.localScale, Is.EqualTo(homeScale));
            Assert.That(TouchLight091.color.a, Is.Zero);
            ConfigureFeedback091();
            ConfigureFeedback091();
            Assert.That(_root.GetComponentsInChildren<Image>(true).Count(
                value => value.name == "Button Touch Light 091"), Is.EqualTo(1));
        }

        [Test]
        public void ApplicantStandeeKeepsResponsiveLabelBesideArtwork091()
        {
            var applicant = new GuildCityApplicantView017D
            {
                RecruitId = "HERO_REC_287",
                DisplayName = "Freya Wolfkin",
                RaceId = "WOLFKIN",
                ClassTendencyId = "WARRIOR",
                VisualSeed = "MENU_LAYOUT_091"
            };
            var method = typeof(M1FlowPresenter).GetMethod(
                "AddMenuApplicantStandeeToButton090", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(null, new object[] { _button, applicant });

            var thumbnail = _root.GetComponentsInChildren<RectTransform>(true)
                .Single(value => value.name.StartsWith("Applicant Standee Thumbnail 090 ", StringComparison.Ordinal));
            Assert.That(_label.anchorMin.x, Is.GreaterThanOrEqualTo(0.35f),
                "Responsive text naming must not prevent the recruit label from moving out of the art column.");
            Assert.That(thumbnail.anchorMax.x, Is.LessThan(0.35f));
            Assert.That(_label.GetComponent<Text>().text, Is.EqualTo("RECRUIT MEMBER"));
            Assert.That(applicant.RecruitId, Is.EqualTo("HERO_REC_287"));
        }

        [TestCase("normal")]
        [TestCase("highlighted")]
        [TestCase("selected")]
        [TestCase("pressed")]
        public void PositiveActionRetainsReadableDarkInkInEveryInteractiveState091(string state)
        {
            var assembly = typeof(M1FlowPresenter).Assembly;
            var runtime = assembly.GetType("SecondDimension.Presentation.RuntimeUi", true);
            var positive = (Color)runtime.GetField("Positive", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var colors = _button.colors;
            colors.normalColor = positive;
            _button.colors = colors;
            var premium = assembly.GetType("SecondDimension.Presentation.M1PremiumUi", true);
            var role = premium.GetNestedType("ButtonRole", BindingFlags.NonPublic);
            premium.GetMethod("StyleButton", BindingFlags.Static | BindingFlags.Public)
                .Invoke(null, new[] { (object)_button, Enum.Parse(role, "Primary") });

            colors = _button.colors;
            var surface = state == "normal" ? colors.normalColor
                : state == "highlighted" ? colors.highlightedColor
                : state == "selected" ? colors.selectedColor : colors.pressedColor;
            var ink = _label.GetComponent<Text>().color;
            Assert.That(surface.g, Is.GreaterThan(surface.r));
            Assert.That(surface.g, Is.GreaterThan(surface.b), "Focus must stay green, never turn dark blue under ink.");
            Assert.That(ink.maxColorComponent, Is.LessThan(0.15f));
            // Include the darkest interior modulation of the generated button
            // material, not just the nominal ColorBlock tint.
            var luminance = RelativeLuminance091(surface * 0.89f);
            var contrast = (luminance + 0.05f) / (RelativeLuminance091(ink) + 0.05f);
            Assert.That(contrast, Is.GreaterThanOrEqualTo(4.5f), state + " label contrast");
        }

        private static float RelativeLuminance091(Color color)
        {
            return 0.2126f * LinearChannel091(color.r) +
                   0.7152f * LinearChannel091(color.g) +
                   0.0722f * LinearChannel091(color.b);
        }

        private static float LinearChannel091(float channel) => channel <= 0.04045f
            ? channel / 12.92f
            : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

        [Test]
        public void MainRecruitArtUsesTheSceneWithoutAnOpaquePortraitSlab091()
        {
            var presenterObject = new GameObject("Recruit Scene Layout Presenter Test 091");
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                presenter.enabled = false;
                typeof(M1FlowPresenter).GetMethod("BuildRecruitmentPortrait074",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter, new object[]
                    {
                        _root.transform,
                        new GuildCityApplicantView017D
                        {
                            RecruitId = "HERO_REC_287", DisplayName = "Freya Wolfkin",
                            RaceId = "WOLFKIN", ClassTendencyId = "WARRIOR"
                        }
                    });
                var frame = _root.transform.Find("Selected Applicant Portrait 074").GetComponent<Image>();
                Assert.That(frame.color.a, Is.Zero);
                Assert.That(frame.raycastTarget, Is.False);
                var mask = frame.transform.Find("Standee Backing 090").GetComponent<Mask>();
                Assert.That(mask, Is.Not.Null);
                Assert.That(mask.showMaskGraphic, Is.False,
                    "The selected adventurer should stand in the illustrated room, not inside a large solid backing.");
                Assert.That(frame.transform.Find("Standee Identity Plate 090").GetComponent<Image>().color.a, Is.Zero);
                Assert.That(frame.rectTransform.anchorMin.y, Is.GreaterThanOrEqualTo(0.24f));
                Assert.That(frame.rectTransform.anchorMax.x, Is.LessThan(0.46f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [Test]
        public void ApplicantChoicesShareBottomRailWithoutCoveringSelectedHero091()
        {
            var presenterObject = new GameObject("Recruit Rail Layout Presenter Test 091");
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                presenter.enabled = false;
                var applicants = Enumerable.Range(0, 3).Select(index => new GuildCityApplicantView017D
                {
                    RecruitId = "CANDIDATE_" + index, DisplayName = "Applicant " + index,
                    ClassTendencyId = "WARRIOR", RaceId = "HUMAN", Slot = index
                }).ToArray();
                typeof(M1FlowPresenter).GetMethod("BuildRecruitmentCandidateList074",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter, new object[]
                    {
                        _root.transform, applicants, null, new GuildCityPresentationState017D()
                    });
                var rail = _root.transform.Find("Applicant Shortlist 074") as RectTransform;
                Assert.That(rail, Is.Not.Null);
                Assert.That(rail.anchorMax.y, Is.LessThanOrEqualTo(0.225f));
                var buttons = rail.GetComponentsInChildren<Button>()
                    .Where(value => value.name.StartsWith("Applicant Shortlist CANDIDATE_", StringComparison.Ordinal))
                    .Select(value => value.GetComponent<RectTransform>()).ToArray();
                Assert.That(buttons, Has.Length.EqualTo(3));
                Assert.That(buttons.Select(value => value.anchorMin.y).Distinct().Count(), Is.EqualTo(1));
                Assert.That(buttons.Select(value => value.anchorMin.x).Distinct().Count(), Is.EqualTo(3));
                for (var index = 1; index < buttons.Length; index++)
                    Assert.That(buttons[index].anchorMin.x, Is.GreaterThan(buttons[index - 1].anchorMax.x));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 720f)]
        public void CompactApplicantPagingUsesVisibleFittedWordLabels091(float width, float height)
        {
            var presenterObject = new GameObject("Recruit Pager Layout Presenter Test 091");
            try
            {
                var presenter = presenterObject.AddComponent<M1FlowPresenter>();
                presenter.enabled = false;
                _root.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
                var applicants = Enumerable.Range(0, 4).Select(index => new GuildCityApplicantView017D
                {
                    RecruitId = "PAGED_CANDIDATE_" + index, DisplayName = "Applicant " + index,
                    ClassTendencyId = "WARRIOR", RaceId = "HUMAN", Slot = index
                }).ToArray();
                typeof(M1FlowPresenter).GetMethod("BuildRecruitmentCandidateList074",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(presenter, new object[]
                    {
                        _root.transform, applicants, null, new GuildCityPresentationState017D()
                    });
                var rail = _root.transform.Find("Applicant Shortlist 074") as RectTransform;
                rail.ForceUpdateRectTransforms();
                var previous = rail.Find("Previous Applicant Page 074").GetComponent<Button>();
                var next = rail.Find("Next Applicant Page 074").GetComponent<Button>();
                Assert.That(previous.interactable, Is.False);
                Assert.That(next.interactable, Is.True);
                foreach (var button in new[] { previous, next })
                {
                    var label = button.GetComponentInChildren<Text>();
                    Assert.That(label.text, Is.EqualTo(button == previous ? "BACK" : "NEXT"));
                    Assert.That(label.resizeTextForBestFit, Is.True);
                    Assert.That(label.resizeTextMaxSize, Is.LessThanOrEqualTo(22));
                    Assert.That(label.raycastTarget, Is.False);
                    label.rectTransform.ForceUpdateRectTransforms();
                    var bounds = label.rectTransform.rect.size;
                    Assert.That(bounds.y, Is.GreaterThan(22f), "The compact rail must leave a real text line.");
                    var settings = label.GetGenerationSettings(bounds);
                    settings.resizeTextForBestFit = false;
                    settings.fontSize = label.resizeTextMaxSize;
                    settings.verticalOverflow = VerticalWrapMode.Truncate;
                    using (var generator = new TextGenerator())
                    {
                        Assert.That(generator.Populate(label.text, settings), Is.True);
                        Assert.That(generator.characterCountVisible, Is.EqualTo(label.text.Length),
                            "Every paging letter must actually render, including the disabled BACK control.");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(presenterObject);
            }
        }

        private Image TouchLight091 => _root.transform.Find("Button Touch Light 091").GetComponent<Image>();

        private void ConfigureFeedback091()
        {
            var premium = typeof(M1FlowPresenter).Assembly.GetType("SecondDimension.Presentation.M1PremiumUi", true);
            var sprite = premium.GetProperty("RoundedMask091", BindingFlags.Static | BindingFlags.Public).GetValue(null);
            FeedbackType091.GetMethod("Configure091", BindingFlags.Instance | BindingFlags.Public)
                .Invoke(_feedback, new[] { (object)_button, sprite });
        }

        private void Tick091(float deltaTime) => Invoke091("Tick091", deltaTime);

        private void Invoke091(string method, params object[] arguments)
        {
            FeedbackType091.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(_feedback, arguments);
        }
    }
}
