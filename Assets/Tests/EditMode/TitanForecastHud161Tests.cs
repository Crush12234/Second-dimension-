using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;
namespace SecondDimension.Tests.EditMode
{
    public sealed class TitanForecastHud161Tests
    {
        [TestCase(5)][TestCase(6)][TestCase(29)]
        public void EveryOrdinaryGoldAndEchoForecastIsReachableThroughRealHudButtons161(int count)
        {
            var host=new GameObject("Titan HUD QA161",typeof(RectTransform),typeof(Canvas));
            try
            {
                var selected=new List<string>();var hud=host.AddComponent<M2BattleCommandHud072>();
                hud.Initialize(host.GetComponent<RectTransform>(),_=>{},(_,__)=>{},(_,id)=>selected.Add(id),()=>{});
                var forecasts=Enumerable.Range(0,count).Select(i=>new M2ForecastView{ForecastId="F161_"+i,UnionId="U161",
                    CommandId=i<5?"CMD_BALANCED":i<17?"TITAN161_ECHO_TITAN_"+(i-4).ToString("000"):"TITAN161_GOLD_TITAN_"+(i-16).ToString("000"),
                    CommandName=i<5?"Ordinary order "+i:i<17?"Eidran echo "+(i-4):"Personal Gold "+(i-16),TargetName="Committed target",SharedApCost=6,CombinedMpCost=10}).ToArray();
                var battle=new M2BattleView{BattleId="HUD161",Round=1,Forecasts=forecasts,
                    PlayerUnions=new[]{new M2BattleUnionView{UnionId="U161",DisplayName="Eidran and allies",CanAct=true,Side="Player",CurrentAp=30,MaximumAp=30,Cohesion=100}}};
                string immutable=CanonicalJson.Serialize(battle);
                hud.Refresh(battle,"U161");
                var all=host.GetComponentsInChildren<Button>(true);
                var next=all.Single(b=>b.name=="Next Forecast Page 161");
                var previous=all.Single(b=>b.name=="Previous Forecast Page 161");
                Assert.That(next.gameObject.activeSelf,Is.EqualTo(count>5));
                for(int page=0;page<20;page++)
                {
                    var cards=all.Where(b=>b.name.StartsWith("Complete Forecast Order ",StringComparison.Ordinal)&&b.gameObject.activeInHierarchy).ToArray();
                    Assert.That(cards.Length,Is.InRange(1,count>5?3:5));
                    foreach(var card in cards)
                    {
                        Assert.That(card.interactable,Is.True);card.onClick.Invoke();
                        var rect=card.GetComponent<RectTransform>();
                        if(count>5){Assert.That(rect.anchorMin.x,Is.GreaterThan(previous.GetComponent<RectTransform>().anchorMax.x));
                            Assert.That(rect.anchorMax.x,Is.LessThan(next.GetComponent<RectTransform>().anchorMin.x));}
                    }
                    if(!next.gameObject.activeInHierarchy||!next.interactable)break;
                    next.onClick.Invoke();
                }
                Assert.That(selected,Is.EqualTo(forecasts.Select(f=>f.ForecastId)),"Every displayed choice must invoke its exact original forecast ID once.");
                Assert.That(CanonicalJson.Serialize(battle),Is.EqualTo(immutable),"Paging is read-only and never spends or commits an order itself.");
                if(count>5)
                {
                    Assert.That(previous.interactable,Is.True);
                    while(previous.interactable)previous.onClick.Invoke();
                    int before=selected.Count;
                    all.First(b=>b.name.StartsWith("Complete Forecast Order ",StringComparison.Ordinal)&&b.gameObject.activeInHierarchy).onClick.Invoke();
                    Assert.That(selected[before],Is.EqualTo("F161_0"));
                    hud.SetInteractable(false);Assert.That(next.interactable,Is.False);
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
