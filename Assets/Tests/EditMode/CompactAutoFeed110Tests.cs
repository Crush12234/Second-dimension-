using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CompactAutoFeed110Tests
    {
        [Test]
        public void LiveFeed110HasThreeBoundedLinesOneActualActionAndSavedRewards()
        {
            var block = "R7  •  Vanguard → Restore\nWHY  •  " + new string('w', 600) +
                "\nPLANNED  •  Never presented as actual\nEXECUTED  •  Sella → Healing Light → Lowen" +
                "\nEXECUTED  •  Kiran → Ember → Enemy\nEXECUTED  •  Lita → Guard → Ally";
            var text = M2BattleExperienceController072.BuildCompactAutoDecisionText110(
                block, true, 16f, "FLOOR 220 CLEARED\n+315 BATTLE GUILD XP\nPROGRESS SAVED");
            var lines = text.Split('\n');
            Assert.That(lines.Length, Is.EqualTo(3));
            Assert.That(lines.All(line => line.Length <=
                M2BattleExperienceController072.MaximumAutoFeedLineCharacters110), Is.True);
            Assert.That(lines[0], Does.Contain("AUTO ON  16×").And.Contain("Vanguard → Restore"));
            Assert.That(lines[1], Is.EqualTo("Sella → Healing Light → Lowen  (+2 more)"));
            Assert.That(lines[2], Does.Contain("+315 BATTLE GUILD XP"));
            Assert.That(text, Does.Not.Contain("Never presented").And.Not.Contain("Ember").And.Not.Contain("WHY"));
            var longText = M2BattleExperienceController072.BuildCompactAutoDecisionText110(
                new string('c', 800) + "\nEXECUTED  •  " + new string('a', 800) +
                "\nEXECUTED  •  second", false, 1f, new string('r', 800));
            Assert.That(longText.Split('\n').All(line => line.Length <= 160), Is.True);
            Assert.That(longText.Split('\n')[1], Does.EndWith("(+1 more)"));
        }

        [Test]
        public void PlayerHistory110KeepsNamedDecisionsAndRewardsWithoutInternalIdentifiers()
        {
            var entry = "R7  •  Vanguard → Restore\nBATTLE  •  ABYSS_INTERNAL_ID" +
                "\nWHY  •  A wounded ally needs healing." +
                "\nCOMMITTED FORECAST  •  FORECAST_INTERNAL_ID" +
                "\nFALLBACK  •  HM300_INTERNAL_ID / ART_INTERNAL_ID / state hash" +
                "\nPLANNED  •  Sella → Healing Light → Lowen" +
                "\nCANCELED  •  Kiran had no remaining legal target." +
                "\nEXECUTED  •  Sella → Healing Light → Lowen";
            var history = new Queue<string>(new[] { entry });
            var shown = M2BattleExperienceController072.FormatPlayerAutoHistory110(history.Peek());
            Assert.That(shown, Does.Contain("A wounded ally needs healing.")
                .And.Contain("PLANNED  •  Sella → Healing Light → Lowen")
                .And.Contain("CANCELED  •  Kiran had no remaining legal target.")
                .And.Contain("EXECUTED  •  Sella → Healing Light → Lowen"));
            Assert.That(shown, Does.Not.Contain("INTERNAL_ID").And.Not.Contain("state hash"));
            Assert.That(history.Peek(), Is.EqualTo(entry), "Formatting must not rewrite captured evidence.");
            var reward = "FLOOR 302 CLEARED\n+330 BATTLE GUILD XP\nREWARDS CLAIMED  •  PROGRESS SAVED";
            Assert.That(M2BattleExperienceController072.FormatPlayerAutoHistory110(reward), Is.EqualTo(reward));
        }

        [Test]
        public void History110EvictsOldestAt64AndSnapshotsCannotPoisonOrFollowLiveQueue()
        {
            var history = new Queue<string>();
            for (var i = 0; i < 70; i++)
                M2BattleExperienceController072.AppendAutoHistory110(history, "entry-" + i);
            Assert.That(history.Count, Is.EqualTo(64));
            Assert.That(history.Peek(), Is.EqualTo("entry-6"));
            var snapshot = M2BattleExperienceController072.SnapshotAutoHistory110(history);
            snapshot[0] = "poison";
            M2BattleExperienceController072.AppendAutoHistory110(history, "saved reward receipt");
            M2BattleExperienceController072.AppendAutoHistory110(history, " ");
            Assert.That(history.Count, Is.EqualTo(64));
            Assert.That(history.Peek(), Is.EqualTo("entry-7"));
            Assert.That(history, Does.Not.Contain("poison"));
            Assert.That(snapshot.Last(), Is.EqualTo("entry-69"));
            Assert.That(history.Last(), Is.EqualTo("saved reward receipt"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingExecution110IsUnconfirmedOrExplicitlyCanceled(bool canceled)
        {
            var before = Before();
            var after = After(canceled ? new[] { Event("ACTION_CANCELED_DEAD_TARGET", "H1", "ART_HEAL", "E1") }
                : Array.Empty<M2BattleEventView>());
            var text = M2BattleExperienceController072.BuildCommittedAutoDecisionBlocks108(before, after)["U1"];
            Assert.That(text, Does.Contain("PLANNED  •  Vanguard / Sella → Healing Light → Planned Target"));
            Assert.That(text, Does.Contain(canceled ? "CANCELED  •  Sella" : "UNCONFIRMED  •  Sella"));
            Assert.That(text, Does.Not.Contain("EXECUTED  •  "));
            var compact = M2BattleExperienceController072.BuildCompactAutoDecisionText110(text, true, 16f, null);
            Assert.That(compact, Does.Contain("No confirmed action"));
            Assert.That(compact, Does.Not.Contain("Healing Light → Planned Target"));
        }

        [Test]
        public void Capture110RetainsAnOrderWithNoPlaybackBeatAndPlaybackCannotDuplicateIt()
        {
            var owner = new GameObject("Capture test controller");
            try
            {
                var controller = owner.AddComponent<M2BattleExperienceController072>();
                Set(controller, "_autoOrders091", true);
                var blocks = (IReadOnlyDictionary<string, string>)controller.GetType()
                    .GetMethod("CaptureCommittedAutoDecisions110", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(controller, new object[] { Before(), After(Array.Empty<M2BattleEventView>()) });
                var queue = Get<Queue<string>>(controller, "_autoDecisionHistory108");
                Assert.That(queue.Count, Is.EqualTo(1));
                Assert.That(queue.Peek(), Does.Contain("UNCONFIRMED").And.Contain("BATTLE110"));
                Set(controller, "_pendingAutoDecisions108", blocks);
                controller.GetType().GetMethod("ShowCommittedAutoDecision108",
                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, new object[] { "U1" });
                Assert.That(queue.Count, Is.EqualTo(1));
                Assert.That(Get<string>(controller, "_liveAutoDecision110"), Is.EqualTo(blocks["U1"]));
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [Test]
        public void History110KeepsFullForecastAndEveryActualTargetWithoutInventingPlannedExecution()
        {
            var before = Before();
            var forecast = before.Forecasts[0];
            forecast.ExpectedEffect = "Detailed " + new string('x', 240) + " final explanation.";
            forecast.FallbackBehavior = "Retarget a living enemy.";
            forecast.Risk = "Ally might fall before the order.";
            forecast.LearningOpportunity = "Growth for every contributing Art.";
            var after = After(new[]
            {
                Event("MARTIAL_HIT", "H1", "ART_ACTUAL", "E1"),
                Event("MARTIAL_HIT", "H1", "ART_ACTUAL", "E2"),
                Event("MARTIAL_HIT", "H1", "ART_ACTUAL", "E3"),
                Event("GUARD", "H2", "ART_GUARD", "H1")
            });
            var text = M2BattleExperienceController072.BuildCommittedAutoDecisionBlocks108(before, after)["U1"];
            Assert.That(text, Does.Contain(forecast.ExpectedEffect));
            Assert.That(text, Does.Contain("FALLBACK  •  Retarget a living enemy."));
            Assert.That(text, Does.Contain("RISK  •  Ally might fall before the order."));
            Assert.That(text, Does.Contain("LEARNING  •  Growth for every contributing Art."));
            Assert.That(text, Does.Contain("Lowen, Neris, Orrin"));
            Assert.That(text.Split('\n').Count(line => line.StartsWith("EXECUTED  •  ")), Is.EqualTo(2));
            Assert.That(text.Split('\n').Where(line => line.StartsWith("EXECUTED  •  "))
                .All(line => !line.Contains("Planned Target")), Is.True);
            Assert.That(text, Does.Not.Contain("UNCONFIRMED"));
        }

        [TestCase("round")]
        [TestCase("union")]
        [TestCase("battle")]
        public void ForeignResolvedEvents110CannotConfirmThisCommittedAction(string mismatch)
        {
            var actual = Event("RESTORATION", "H1", "ART_HEAL", "E1");
            var after = After(new[] { actual });
            if (mismatch == "round") actual.Round = 6;
            if (mismatch == "union") actual.ActorUnionId = "OTHER_UNION";
            if (mismatch == "battle") after.BattleId = "OTHER_BATTLE";
            var text = M2BattleExperienceController072.BuildCommittedAutoDecisionBlocks108(Before(), after)["U1"];
            Assert.That(text, Does.Contain("UNCONFIRMED  •  Sella"));
            Assert.That(text, Does.Not.Contain("EXECUTED  •  "));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 720f)]
        public void FeedAndHistory110ReserveSharedViewportAndLeaveAutoScheduleUntouched(float width, float height)
        {
            var canvas = (Canvas)typeof(M2BattleExperienceController072).Assembly
                .GetType("SecondDimension.Presentation.RuntimeUi")
                .GetMethod("CreateCanvas", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { "Feed test root" });
            var host = canvas.gameObject;
            var owner = new GameObject("Feed test controller");
            try
            {
                var root = host.GetComponent<RectTransform>();
                var scaler = host.GetComponent<CanvasScaler>();
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                var scale = Mathf.Pow(width / scaler.referenceResolution.x, 1f - scaler.matchWidthOrHeight) *
                    Mathf.Pow(height / scaler.referenceResolution.y, scaler.matchWidthOrHeight);
                // Model the requested output resolution using the actual shipped
                // reference/match settings, independent of the Editor Game view size.
                scaler.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.scaleFactor = scale;
                root.sizeDelta = new Vector2(width / scale, height / scale);
                var controller = owner.AddComponent<M2BattleExperienceController072>();
                Set(controller, "_root", root);
                var diorama = Stretch(root, "Diorama");
                var hud = Stretch(root, "HUD");
                Set(controller, "_dioramaLayer", diorama);
                Set(controller, "_hudLayer", hud);
                Set(controller, "_autoOrders091", true);
                Set(controller, "_nextAutoOrderAt091", 123.456f);
                Invoke(controller, "BuildAutoDecisionFeed108");
                var panel = Get<Image>(controller, "_autoDecisionPanel108").rectTransform;
                Assert.That(panel.rect.height / root.rect.height, Is.LessThanOrEqualTo(0.10f));
                var liveText = Get<Text>(controller, "_autoDecisionText108");
                Assert.That(liveText.resizeTextMinSize * scale, Is.GreaterThanOrEqualTo(14f));
                Assert.That(diorama.anchorMin, Is.EqualTo(hud.anchorMin));
                Assert.That(diorama.anchorMax, Is.EqualTo(hud.anchorMax));
                Assert.That(diorama.anchorMin.y, Is.GreaterThan(panel.anchorMax.y));
                Assert.That(diorama.anchorMax.y, Is.EqualTo(0.985f));
                var queue = Get<Queue<string>>(controller, "_autoDecisionHistory108");
                for (var i = 0; i < 64; i++)
                    M2BattleExperienceController072.AppendAutoHistory110(queue, "Committed decision " + i + new string('x', 160));
                Invoke(controller, "OpenAutoHistory110");
                var history = Get<RectTransform>(controller, "_autoHistoryPanel110");
                var historyRows = Get<List<Text>>(controller, "_autoHistoryEntries110");
                var snapshotText = historyRows.Select(row => row.text).ToArray();
                var scroll = Get<ScrollRect>(controller, "_autoHistoryScroll110");
                Assert.That(history.gameObject.activeSelf, Is.True);
                Assert.That(scroll.vertical, Is.True);
                Assert.That(scroll.horizontal, Is.False);
                Assert.That(scroll.viewport.GetComponent<RectMask2D>(), Is.Not.Null);
                Assert.That(scroll.content, Is.SameAs(Get<RectTransform>(controller, "_autoHistoryContent110")));
                Assert.That(historyRows.Count, Is.EqualTo(64));
                Assert.That(historyRows.Min(row => row.fontSize) * scale, Is.GreaterThanOrEqualTo(16f));
                M2BattleExperienceController072.AppendAutoHistory110(queue, "Next real saved receipt");
                Invoke(controller, "RefreshAutoDecisionFeed108");
                Assert.That(historyRows.Select(row => row.text), Is.EqualTo(snapshotText));
                Assert.That(history.GetSiblingIndex(), Is.EqualTo(root.childCount - 1));
                hud.SetAsLastSibling();
                Invoke(controller, "KeepAutoHistoryOnTop110");
                Assert.That(history.GetSiblingIndex(), Is.EqualTo(root.childCount - 1));
                Assert.That(Get<bool>(controller, "_autoOrders091"), Is.True);
                Assert.That(Get<float>(controller, "_nextAutoOrderAt091"), Is.EqualTo(123.456f));
                Assert.That(controller.AnimationSpeed, Is.EqualTo(1f));
                history.GetComponentsInChildren<Button>().Single().onClick.Invoke();
                Assert.That(history.gameObject.activeSelf, Is.False);
                Assert.That(Get<bool>(controller, "_autoOrders091"), Is.True);
                Assert.That(Get<float>(controller, "_nextAutoOrderAt091"), Is.EqualTo(123.456f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static M2BattleView Before() => new M2BattleView
        {
            BattleId = "BATTLE110", Round = 7,
            PlayerUnions = new[] { Player() }, EnemyUnions = new[] { Enemy() },
            Forecasts = new[] { new M2ForecastView
            {
                ForecastId = "F1", UnionId = "U1", CommandId = "RESTORE", CommandName = "Restore",
                TacticalIntent = "RESTORE", ExpectedEffect = "Heal an ally.",
                MemberActions = new[] { new M2PredictedActionView
                {
                    ActorMemberId = "H1", ActorName = "Sella", ArtId = "ART_HEAL", ArtName = "Healing Light",
                    TargetMemberId = "PLANNED", TargetName = "Planned Target", ActionKind = "Restoration"
                } }
            } }
        };

        private static M2BattleView After(M2BattleEventView[] events) => new M2BattleView
        {
            BattleId = "BATTLE110", Round = 8, PlayerUnions = new[] { Player() },
            EnemyUnions = new[] { Enemy() }, LastResolvedRoundEvents = events
        };

        private static M2BattleUnionView Player() => new M2BattleUnionView
        {
            UnionId = "U1", DisplayName = "Vanguard", SelectedForecastId = "F1",
            Members = new[] { new M2BattleMemberView { MemberId = "H1", DisplayName = "Sella" },
                new M2BattleMemberView { MemberId = "H2", DisplayName = "Kiran" } }
        };

        private static M2BattleUnionView Enemy() => new M2BattleUnionView
        {
            UnionId = "E", DisplayName = "Enemies", Members = new[]
            {
                new M2BattleMemberView { MemberId = "E1", DisplayName = "Lowen" },
                new M2BattleMemberView { MemberId = "E2", DisplayName = "Neris" },
                new M2BattleMemberView { MemberId = "E3", DisplayName = "Orrin" }
            }
        };

        private static M2BattleEventView Event(string kind, string actor, string art, string target) =>
            new M2BattleEventView
            {
                EventType = kind, ActorUnionId = "U1", ActorMemberId = actor, ArtId = art,
                TargetUnionId = "E", TargetMemberId = target, Round = 7, Sequence = 1,
                Text = kind.StartsWith("ACTION_CANCELED", StringComparison.Ordinal)
                    ? "Action canceled because the target was defeated." : "Sella uses Healing Light on Lowen."
            };

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        private static T Get<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Invoke(object target, string name) =>
            target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);

        private static RectTransform Stretch(Transform parent, string name)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
