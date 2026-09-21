using System;
using System.Collections;
using NUnit.Framework;
using SecondDimension.Presentation.Boot;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerSoakReadiness122Tests
    {
        GameObject _host, _events;
        Button _button;
        Image _blocker;
        int _clicks;

        [UnitySetUp]
        public IEnumerator SetUp122()
        {
            _clicks = 0;
            if (EventSystem.current == null)
                _events = new GameObject("Soak Readiness Events 122", typeof(EventSystem), typeof(StandaloneInputModule));
            _host = new GameObject("Soak Readiness Canvas 122", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = _host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            var scaler = _host.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var buttonObject = new GameObject("Bank Tower battle victory 088", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(_host.transform, false);
            _button = buttonObject.GetComponent<Button>();
            _button.targetGraphic = buttonObject.GetComponent<Image>();
            _button.onClick.AddListener(() => _clicks++);
            Center122((RectTransform)buttonObject.transform);
            var blockerObject = new GameObject("Transient Prior Page 122", typeof(RectTransform), typeof(Image));
            blockerObject.transform.SetParent(_host.transform, false);
            _blocker = blockerObject.GetComponent<Image>();
            Center122((RectTransform)blockerObject.transform);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown122()
        {
            if (_host != null) UnityEngine.Object.Destroy(_host);
            if (_events != null) UnityEngine.Object.Destroy(_events);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NewBankControlWaitsForVisibleStableFramesThenDispatchesOneActualPointer122()
        {
            var readiness = new TowerAutoPlayerSoak110.ButtonReadiness122();
            Assert.That(_button.IsInteractable(), Is.True, "Interactable alone does not establish visible input ownership.");
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False);
            Assert.That(readiness.LastDiagnostic, Does.Contain("Transient Prior Page 122"));
            Assert.That(_clicks, Is.Zero);
            // The UI retires its prior page naturally. The readiness observer
            // itself never hides the blocker, scrolls, or changes the control.
            _blocker.gameObject.SetActive(false);
            yield return null;
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False);
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False,
                "Two observations in the same rendered frame cannot establish readiness.");
            ((RectTransform)_button.transform).anchoredPosition += new Vector2(18f, 0f);
            yield return null;
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False,
                "A later layout change restarts the stable-geometry observation.");
            yield return null;
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.True);
            Assert.That(TowerAutoPlayerSoak110.ButtonReadiness122.TryHit(_button, out var pointer, out _, out _), Is.True);
            ExecuteEvents.Execute(_button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(_clicks, Is.EqualTo(1));
            readiness.Reset();
            Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False);
        }

        [UnityTest]
        public IEnumerator PersistentBlockerNeverDispatchesAndDeadlineReportsActualHit122()
        {
            var readiness = new TowerAutoPlayerSoak110.ButtonReadiness122();
            for (var frame = 0; frame < 5; frame++)
            {
                Assert.That(readiness.Observe(_button, Time.renderedFrameCount), Is.False);
                yield return null;
            }
            var exception = Assert.Throws<InvalidOperationException>(() => readiness.CheckDeadline(180d, 180d));
            Assert.That(exception.Message, Does.Contain("Bank Tower battle victory 088"));
            Assert.That(exception.Message, Does.Contain("topHit=Soak Readiness Canvas 122/Transient Prior Page 122"));
            Assert.That(exception.Message, Does.Contain("within 180 seconds"));
            Assert.That(_button.IsInteractable(), Is.True);
            Assert.That(_blocker.gameObject.activeSelf, Is.True);
            Assert.That(_clicks, Is.Zero);
        }

        [TestCase(300, 1, 1, true)]
        [TestCase(300, 1, 300, false)]
        [TestCase(300, 1, 2, false)]
        [TestCase(3, 3, 3, true)]
        public void SavedTowerSequenceUsesLatestCompletionWithoutPassingOnOldRecord130(
            int record, int latestBanked, int observed, bool matches)
        {
            // Projection-only unit input. No campaign, receipt, battle or save
            // is constructed or edited to claim a completed floor.
            var floors = new SecondDimension.Gameplay.Campaign022.TowerFloorProgress094();
            typeof(SecondDimension.Gameplay.Campaign022.TowerFloorProgress094)
                .GetProperty("HighestActualFloor").SetValue(floors, record);
            typeof(SecondDimension.Gameplay.Campaign022.TowerFloorProgress094)
                .GetProperty("LatestCompletedActualFloor130").SetValue(floors, latestBanked);
            if (matches)
                Assert.DoesNotThrow(() => TowerAutoPlayerSoak110.RequireSavedTowerCompletion130(floors, observed));
            else
                Assert.Throws<InvalidOperationException>(() =>
                    TowerAutoPlayerSoak110.RequireSavedTowerCompletion130(floors, observed));
        }

        static void Center122(RectTransform rect)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(360f, 160f);
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
