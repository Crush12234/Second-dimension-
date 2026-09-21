using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class FortuneWheel132PlayModeTests
    {
        [UnityTest]
        public IEnumerator WheelTwelveWedgesLandOnExactlyTheSavedPrizeKindAndConcealUntilStopped132()
        {
            var root=new GameObject("Wheel132 visual test",typeof(RectTransform),typeof(Canvas));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var holder=new GameObject("Wheel stage132",typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(root.transform,false);holder.anchorMin=holder.anchorMax=new Vector2(.5f,.5f);
            holder.sizeDelta=new Vector2(650,650);
            try
            {
                for(var sector=0;sector<3;sector++)
                {
                    var settled=0;
                    var effect=CommittedFateEffect132.Play132(holder,true,sector,Color.yellow,false,()=>settled++);
                    yield return null;yield return null;
                    var graphic=effect.GetComponentInChildren<FatePolyhedronGraphic132>();
                    var labels=effect.GetComponentsInChildren<Text>().Where(x=>x.name.StartsWith("Wheel Prize Type ")).ToArray();
                    Assert.That(labels.Length,Is.EqualTo(12));
                    foreach(var kind in new[]{"XP","CACHE","GEAR"})Assert.That(labels.Count(x=>x.text==kind),Is.EqualTo(4));
                    Assert.That(effect.GetComponentsInChildren<Text>().Single(x=>x.name=="Saved Fate Face 132").text,Is.Empty);
                    Assert.That(graphic.HighlightWedge132,Is.EqualTo(-1));Assert.That(settled,Is.Zero);
                    Assert.That(effect.GetComponentsInChildren<Text>().Single(x=>x.name=="Fortune Wheel Pointer 132").text,Is.EqualTo("▼"));
                    yield return new WaitForSecondsRealtime(2.95f);
                    Assert.That(effect.Settled132,Is.True);Assert.That(settled,Is.EqualTo(1));
                    // Inspect which physically positioned label reached the
                    // fixed top pointer, independently of the saved-kind field.
                    var atPointer=labels.OrderByDescending(x=>x.rectTransform.anchoredPosition.y).First();
                    Assert.That(atPointer.text,Is.EqualTo(new[]{"XP","CACHE","GEAR"}[sector]));
                    Assert.That(Mathf.Abs(atPointer.rectTransform.anchoredPosition.x),Is.LessThan(.5f));
                    effect.Settle132();Assert.That(settled,Is.EqualTo(1));
                    Object.Destroy(effect.gameObject);yield return null;
                }
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}
