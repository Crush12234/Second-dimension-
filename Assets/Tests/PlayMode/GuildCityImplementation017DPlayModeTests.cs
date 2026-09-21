using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class GuildCityImplementation017DPlayModeTests
    {
        [UnityTearDown]
        public IEnumerator CleanupGuildCityProof()
        {
            foreach (var presenter in UnityEngine.Object.FindObjectsByType<GuildCityVerticalSlicePresenter017D>(FindObjectsSortMode.None))
                UnityEngine.Object.Destroy(presenter.gameObject);
            foreach (var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (StringComparer.Ordinal.Equals(canvas.name, "Guild City 017D Canvas"))
                    UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LivingHallAndCityBuildModeRenderWithoutWaitDay()
        {
            var root = new GameObject("Guild City 017D PlayMode Test");
            root.AddComponent<GuildCityVerticalSlicePresenter017D>();
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();

            AssertTextContains("LIVING RUINED ANNEX");
            AssertTextContains("never through an empty Wait Day");
            AssertNoButton("WAIT A DAY");
            AssertButton("CITY");

            Click("CITY");
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTextContains("CITY BUILD MODE — 12 SNAP PLOTS");
            AssertTextContains("Six plots begin usable");
            AssertButton("PLACE RECRUITMENT OFFICE");
            AssertButton("STAFF WITH R1");
            AssertNoButton("WAIT A DAY");
        }

        [UnityTest]
        public IEnumerator ThreeOpeningContractsAndExpeditionEntryAreReachable()
        {
            var root = new GameObject("Guild City 017D Contract Test");
            root.AddComponent<GuildCityVerticalSlicePresenter017D>();
            yield return null;
            yield return null;

            Click("CONTRACTS");
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertTextContains("The Bell Beneath Skyhome");
            AssertTextContains("The Door Inside");
            AssertTextContains("Keep the Relief Road Open");
            Assert.That(CountButtons("ACCEPT CONTRACT"), Is.EqualTo(3));

            var first = FindButtons("ACCEPT CONTRACT").First();
            first.onClick.Invoke();
            yield return null;
            Click("BOARD");
            yield return null;
            Canvas.ForceUpdateCanvases();
            AssertButton("BEGIN QUEST");
            AssertNoButton("SELECT INDIVIDUAL ART");
        }

        private static void Click(string label)
        {
            var button = FindButtons(label).FirstOrDefault();
            Assert.That(button, Is.Not.Null, "Button not found: " + label);
            Assert.That(button.interactable, Is.True, "Button was not interactable: " + label);
            button.onClick.Invoke();
        }

        private static void AssertButton(string label) =>
            Assert.That(FindButtons(label).Any(), Is.True, "Button not found: " + label);

        private static void AssertNoButton(string label) =>
            Assert.That(FindButtons(label).Any(), Is.False, "Forbidden button found: " + label);

        private static int CountButtons(string label) => FindButtons(label).Count();

        private static Button[] FindButtons(string label) =>
            UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(value => value.GetComponentsInChildren<Text>()
                    .Any(text => text != null && StringComparer.Ordinal.Equals(text.text, label)))
                .ToArray();

        private static void AssertTextContains(string expected)
        {
            var found = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Any(value => value.text != null &&
                    value.text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0);
            Assert.That(found, Is.True, "Visible text did not contain: " + expected);
        }
    }
}
