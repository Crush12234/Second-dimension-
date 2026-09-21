using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class ResolvedRecoveryPresentation105PlayModeTests
    {
        [UnityTest]
        public IEnumerator Real072RecoveryUsesStandingArtworkAndAuthoritativeCaptionWithoutHpImpact105()
        {
            var hostObject = new GameObject("Recovery 105 Host", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            host.sizeDelta = new Vector2(1280f, 720f);
            var owner = new GameObject("Recovery 105 Owner");
            try
            {
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                var sequence = owner.AddComponent<M2BattleSequenceDirector072>();
                diorama.Initialize(host);
                var captions = new List<string>();
                sequence.Initialize(diorama, null, (title, detail) => captions.Add(title + "|" + detail));
                var battle = Battle105();
                var before = Snapshot105(battle);
                Assert.That(before, Does.Contain("Gara Redtail").And.Contain("CurrentHp").And.Contain("MaximumAp"),
                    "The snapshot must include nested public DTO properties, not an empty serializer object.");
                diorama.Refresh(battle, "UNION_OPENING_01");
                yield return null;
                var actor = diorama.ResolveActor("PROC_36344E2400DC98B6", "UNION_OPENING_01");
                var enemy = diorama.ResolveActor("ENEMY_GATE_GNAWER", "ENEMY_105");
                Assert.That(actor, Is.Not.Null);
                Assert.That(enemy, Is.Not.Null);
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
                var standing = actor.CurrentResourcePath;
                var standingSprite = actor.CurrentArtwork076.sprite;
                Assert.That(standingSprite, Is.Not.Null);
                var standingTexture = standingSprite.texture;
                var standingRect = standingSprite.rect;
                Assert.That(actor.SetPoseImmediate(BattleArtPoseDirector011.ActionPrimary), Is.True);
                var attack = actor.CurrentResourcePath;
                Assert.That(attack, Is.Not.EqualTo(standing), "The fixture must have distinct standing and attack art.");
                actor.SetPoseImmediate(BattleArtPoseDirector011.Idle);
                var events = new[] {
                    Event105(1, "RECOVERY", "ART_RECOVER_BREATH", 0, "Gara Redtail conserves strength; no MP was missing."),
                    Event105(2, "RECOVERY", "ART_RECOVER_BREATH", 2, "Gara Redtail conserves strength and recovers 2 MP."),
                    Event105(3, "AP_RECOVERY", "CMD_AP_RECOVERY", 6, "Opening Union 1 recovers 6 shared AP.") };
                var eventBefore = Snapshot105(events);
                Assert.That(eventBefore, Does.Contain("RECOVERY").And.Contain("2 MP").And.Contain("Amount"));
                sequence.StartCoroutine(sequence.PlayRound(battle, battle, events, false, () => 1f, () => false));
                Assert.That(sequence.IsPlaying, Is.True);
                var neutralFrames = 0;
                var deadline = Time.realtimeSinceStartup + 12f;
                while (sequence.IsPlaying && Time.realtimeSinceStartup < deadline)
                {
                    Assert.That(actor.CurrentPoseId, Is.EqualTo(BattleArtPoseDirector011.Idle),
                        "Anticipation, action, impact and recovery must all stay neutral for a recovery event.");
                    Assert.That(actor.CurrentResourcePath, Is.EqualTo(standing));
                    Assert.That(actor.CurrentResourcePath, Is.Not.EqualTo(attack));
                    Assert.That(actor.CurrentArtwork076.sprite.texture, Is.SameAs(standingTexture));
                    Assert.That(actor.CurrentArtwork076.sprite.rect, Is.EqualTo(standingRect));
                    var poseLayers = actor.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true)
                        .Where(image => image.name.StartsWith("Authored Pose ", StringComparison.Ordinal)).ToArray();
                    Assert.That(poseLayers.Length, Is.EqualTo(2), "Inspect both crossfade layers, not only the current pose ID.");
                    foreach (var layer in poseLayers.Where(image => image.gameObject.activeInHierarchy && image.color.a > 0.01f))
                    {
                        Assert.That(layer.sprite, Is.Not.Null);
                        Assert.That(layer.sprite.texture, Is.SameAs(standingTexture));
                        Assert.That(layer.sprite.rect, Is.EqualTo(standingRect));
                    }
                    neutralFrames++;
                    Assert.That(actor.PresentedCurrentHp076, Is.EqualTo(236));
                    Assert.That(enemy.PresentedCurrentHp076, Is.EqualTo(135));
                    yield return null;
                }
                Assert.That(sequence.IsPlaying, Is.False, "Recovery sequence exceeded the hang guard.");
                Assert.That(neutralFrames, Is.GreaterThan(3), "Observe real normal-speed neutral playback across multiple frames.");
                Assert.That(sequence.LastPresentedBeatCount, Is.EqualTo(3));
                Assert.That(captions, Does.Contain("RECOVER BREATH|Gara Redtail conserves strength; no MP was missing."));
                Assert.That(captions, Does.Contain("RECOVER BREATH|Gara Redtail conserves strength and recovers 2 MP."));
                Assert.That(captions, Does.Contain("AP RECOVERY|Opening Union 1 recovers 6 shared AP."));
                Assert.That(diorama.HpImpactPresentationCount076, Is.Zero);
                Assert.That(Snapshot105(battle), Is.EqualTo(before));
                Assert.That(Snapshot105(events), Is.EqualTo(eventBefore));
            }
            finally
            {
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(hostObject);
            }
            yield return null;
        }

        // These presentation DTOs expose properties. Walk those properties and
        // ordered collections explicitly without a JSON package or JsonUtility.
        private static string Snapshot105(object value)
        {
            if (value == null) return "NULL;";
            var type = value.GetType();
            if (value is string text) return Atom105(text);
            if (type.IsPrimitive || type.IsEnum || value is decimal)
                return Atom105(type.FullName) + Atom105(Convert.ToString(value, CultureInfo.InvariantCulture));
            if (value is IEnumerable sequence)
            {
                var entries = new List<string>();
                foreach (var item in sequence) entries.Add(Snapshot105(item));
                return "LIST:" + entries.Count + ":" + string.Concat(entries.Select(Atom105));
            }
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .Select(property => Atom105(property.Name) + Atom105(Snapshot105(property.GetValue(value))));
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .Select(field => Atom105(field.Name) + Atom105(Snapshot105(field.GetValue(value))));
            return Atom105(type.FullName) + Atom105(string.Concat(properties)) + Atom105(string.Concat(fields));
        }

        private static string Atom105(string value) => (value ?? string.Empty).Length.ToString(CultureInfo.InvariantCulture)
            + ":" + (value ?? string.Empty);
        private static M2BattleEventView Event105(int sequence, string type, string art, int amount, string prose) =>
            new M2BattleEventView { Sequence=sequence, Round=4, EventType=type, ArtId=art, Amount=amount, Text=prose,
                ActorUnionId="UNION_OPENING_01", TargetUnionId="UNION_OPENING_01",
                ActorMemberId=type=="AP_RECOVERY" ? "" : "PROC_36344E2400DC98B6",
                TargetMemberId=type=="AP_RECOVERY" ? "" : "PROC_36344E2400DC98B6" };

        private static M2BattleView Battle105() => new M2BattleView {
            BattleId="RECOVERY_PRESENTATION_105", Round=4, Outcome="In Progress", Objective="Recover shared resources.",
            PlayerUnions=new[] { new M2BattleUnionView { UnionId="UNION_OPENING_01", DisplayName="Skyhome Gatewardens",
                Side="Player", LeaderMemberId="PROC_36344E2400DC98B6", CurrentAp=3, MaximumAp=18,
                Cohesion=100, FormationConditionPercent=100, Members=new[] { new M2BattleMemberView {
                    MemberId="PROC_36344E2400DC98B6", DisplayName="Gara Redtail", ClassName="Guardian", RaceId="DOG_TRIBE",
                    VisualSeed="234B7103DA5CD0DEC716E0C3", PortraitAuthorityId="PROC_36344E2400DC98B6",
                    CurrentHp=236, MaximumHp=325 } } } },
            EnemyUnions=new[] { new M2BattleUnionView { UnionId="ENEMY_105", DisplayName="Hollow Wall", Side="Enemy",
                LeaderMemberId="ENEMY_GATE_GNAWER", CurrentAp=14, MaximumAp=14, Cohesion=100, FormationConditionPercent=100,
                Members=new[] { new M2BattleMemberView { MemberId="ENEMY_GATE_GNAWER", DisplayName="Gate Gnawer",
                    ClassName="Raider", CurrentHp=135, MaximumHp=253 } } } } };
    }
}
