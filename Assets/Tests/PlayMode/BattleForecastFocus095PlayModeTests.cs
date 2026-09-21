using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class BattleForecastFocus095PlayModeTests
    {
        [UnityTest]
        public IEnumerator SelectedArtsPreviewSurvivesRealButtonDisableAndReenable095() => Run095(false);

        [UnityTest]
        public IEnumerator RestoredCommittedArtsForecastGetsControllerFocus095() => Run095(true);

        static IEnumerator Run095(bool restoredSelection)
        {
            var originalEventSystem = EventSystem.current;
            var priorSelected = originalEventSystem?.currentSelectedGameObject;
            var eventsOwner = originalEventSystem == null
                ? new GameObject("Forecast Focus EventSystem 095", typeof(EventSystem)) : null;
            var events = originalEventSystem ?? eventsOwner.GetComponent<EventSystem>();
            EventSystem.current = events;
            events.SetSelectedGameObject(null);
            var hostOwner = new GameObject("Forecast Focus Host 095", typeof(RectTransform));
            var host = hostOwner.GetComponent<RectTransform>();
            host.sizeDelta = new Vector2(1920f, 1080f);
            var owner = new GameObject("Forecast Focus HUD 095");
            try
            {
                var hud = owner.AddComponent<M2BattleCommandHud072>();
                var battle = Battle095(restoredSelection);
                var committedCount = 0;
                hud.Initialize(host, _ => { }, (_, __) => { }, (unionId, forecastId) =>
                {
                    // This is the synchronous production controller order:
                    // disable, coordinator publishes selection, refresh, enable.
                    hud.SetInteractable(false);
                    Assert.That(events.currentSelectedGameObject, Is.Null,
                        "Unity clears the selected command when its button is disabled.");
                    battle.PlayerUnions[0].SelectedForecastId = forecastId;
                    battle.PlayerUnions[0].IsSelected = true;
                    foreach (var forecast in battle.Forecasts)
                        forecast.IsSelected = forecast.ForecastId == forecastId;
                    hud.Refresh(battle, unionId);
                    hud.SetInteractable(true);
                    committedCount++;
                }, () => { });
                hud.Refresh(battle, "ALLY_095");
                var buttons = host.GetComponentsInChildren<Button>(true);
                var basic = buttons.Single(value => value.name == "Complete Forecast Order 1 072");
                var arts = buttons.Single(value => value.name == "Complete Forecast Order 2 072");
                if (!restoredSelection)
                {
                    events.SetSelectedGameObject(arts.gameObject);
                    arts.onClick.Invoke();
                    Assert.That(committedCount, Is.EqualTo(1));
                }
                Assert.That(events.currentSelectedGameObject, Is.EqualTo(arts.gameObject),
                    "Restoring controller focus must not preview Basic while Arts is the committed order.");
                var heading = host.GetComponentsInChildren<Text>(true)
                    .Single(value => value.name == "Forecast Preview Heading 072");
                var summary = host.GetComponentsInChildren<Text>(true)
                    .Single(value => value.name == "Forecast Target Cost Effect 072");
                var actions = host.GetComponentsInChildren<Text>(true)
                    .Single(value => value.name == "Predicted Member Arts Preview 072");
                Assert.That(heading.text, Does.StartWith("✓ READY"));
                Assert.That(summary.text, Does.Contain("47 HP"));
                Assert.That(actions.text, Does.Contain("POWER CUT"));
                Assert.That(actions.text, Does.Not.Contain("BASIC CUT"));
                Assert.That(battle.PlayerUnions[0].SelectedForecastId, Is.EqualTo("ARTS_095"));

                // Intentional inspection must still work without changing the
                // selected command or calling the coordinator a second time.
                ExecuteEvents.Execute(basic.gameObject, new PointerEventData(events), ExecuteEvents.pointerEnterHandler);
                Assert.That(summary.text, Does.Contain("39 HP"));
                Assert.That(actions.text, Does.Contain("BASIC CUT"));
                Assert.That(battle.PlayerUnions[0].SelectedForecastId, Is.EqualTo("ARTS_095"));
                Assert.That(committedCount, Is.EqualTo(restoredSelection ? 0 : 1));
                yield return null;
            }
            finally
            {
                events.SetSelectedGameObject(null);
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(hostOwner);
                if (eventsOwner != null) UnityEngine.Object.Destroy(eventsOwner);
                else if (priorSelected != null) events.SetSelectedGameObject(priorSelected);
            }
            yield return null;
        }

        static M2BattleView Battle095(bool selected)
        {
            var member = new M2BattleMemberView { MemberId = "MEMBER_095", DisplayName = "Kara Emberhalt",
                CurrentHp = 100, MaximumHp = 100, CurrentMp = 20, MaximumMp = 20 };
            return new M2BattleView
            {
                BattleId = "FOCUS_PRESENTATION_ONLY_095",
                PlayerUnions = new[] { new M2BattleUnionView { UnionId = "ALLY_095", DisplayName = "Kara Union",
                    CanAct = true, CurrentAp = 12, MaximumAp = 12, Members = new[] { member },
                    IsSelected = selected, SelectedForecastId = selected ? "ARTS_095" : string.Empty } },
                EnemyUnions = new[] { new M2BattleUnionView { UnionId = "ENEMY_095", DisplayName = "Scout",
                    CanAct = true, Members = new[] { new M2BattleMemberView { MemberId = "SCOUT_095",
                        DisplayName = "Scout", CurrentHp = 100, MaximumHp = 100 } } } },
                Forecasts = new[]
                {
                    Forecast095("BASIC_095", "CMD_BALANCED", "Attack!", "Basic Cut", 39, false),
                    Forecast095("ARTS_095", "CMD_ALL_OUT", "Attack Using Combat Arts!", "Power Cut", 47, selected)
                }
            };
        }

        static M2ForecastView Forecast095(string id, string command, string name, string art, int hp, bool selected) =>
            new M2ForecastView
            {
                ForecastId = id, UnionId = "ALLY_095", CommandId = command, CommandName = name,
                TargetId = "ENEMY_095", TargetName = "Scout", SharedApCost = 1,
                ExpectedEffect = "about " + hp + " HP damage", IsSelected = selected,
                MemberActions = new[] { new M2PredictedActionView { ActorMemberId = "MEMBER_095",
                    ActorName = "Kara Emberhalt", ArtName = art, ArtLevel = 1,
                    Prediction = "about " + hp + " HP damage" } }
            };
    }
}
