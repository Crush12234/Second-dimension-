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
    public sealed class GuildCityOpeningExperience017EPlayModeTests
    {
        [UnityTearDown] public IEnumerator Cleanup()
        {
            foreach(var presenter in UnityEngine.Object.FindObjectsByType<GuildCityVerticalSlicePresenter017D>(FindObjectsSortMode.None)) UnityEngine.Object.Destroy(presenter.gameObject);
            foreach(var canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(StringComparer.Ordinal.Equals(canvas.name,"Guild City 017D Canvas")) UnityEngine.Object.Destroy(canvas.gameObject);
            yield return null;
        }

        [UnityTest] public IEnumerator ContractAndBoardPresentationUsesAuthoredNamesInsteadOfOnlyRawNodeIds()
        {
            var root=new GameObject("Guild City 017E Presentation Test"); root.AddComponent<GuildCityVerticalSlicePresenter017D>();
            yield return null; yield return null;
            var contracts=FindButton("CONTRACTS"); Assert.That(contracts,Is.Not.Null); contracts.onClick.Invoke(); yield return null;
            AssertText("The Bell Beneath Skyhome"); AssertText("The Door Inside"); AssertText("Keep the Relief Road Open");
        }

        private static Button FindButton(string text)=>UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(value=>value.GetComponentsInChildren<Text>().Any(t=>t.text==text));
        private static void AssertText(string expected)=>Assert.That(UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).Any(t=>t.text!=null&&t.text.IndexOf(expected,StringComparison.OrdinalIgnoreCase)>=0),Is.True,expected);
    }
}
