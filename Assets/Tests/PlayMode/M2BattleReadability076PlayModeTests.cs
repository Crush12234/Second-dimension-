using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2BattleReadability076PlayModeTests
    {
        [Test]
        public void BuiltPlayerSmokeParsesMemberImpactSeparatelyFromUnionTotal()
        {
            const string heading = "ENEMY  •  GATE GNAWER  •  -35 HP";
            const string detail =
                "MEMBER HP 120  →  85 / 120\nENEMY UNION TOTAL HP 285 / 320";

            Assert.That(
                FirstHourGoldSmoke071.TryParseSignedHpImpact076(heading, out var signedDelta),
                Is.True);
            Assert.That(
                FirstHourGoldSmoke071.TryParseBeforeAfterHpImpact076(
                    detail,
                    out var beforeHp,
                    out var afterHp,
                    out var maximumHp),
                Is.True);
            Assert.That(signedDelta, Is.EqualTo(-35));
            Assert.That(beforeHp, Is.EqualTo(120));
            Assert.That(afterHp, Is.EqualTo(85));
            Assert.That(maximumHp, Is.EqualTo(120),
                "The member parser must not consume the second-line Union maximum of 320.");
            Assert.That(signedDelta, Is.EqualTo(afterHp - beforeHp));
        }

        [UnityTest]
        public IEnumerator DamageAndHealingChangeBothSidesAtContactAndRemainReadable()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("HP Readability Test Host 076 " + size.x, size);
                var owner = new GameObject("HP Readability Test Owner 076 " + size.x);
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                var sequence = owner.AddComponent<M2BattleSequenceDirector072>();
                diorama.Initialize(host);
                sequence.Initialize(diorama, null, (_, __) => { });
                var initial = BattleWithHp(100, 120);
                diorama.Refresh(initial, "ALLY_076");
                yield return null;
                Canvas.ForceUpdateCanvases();

                var enemyActor = diorama.ResolveActor("ENEMY_MEMBER_076", "ENEMY_076");
                var allyActor = diorama.ResolveActor("ALLY_MEMBER_076", "ALLY_076");
                Assert.That(enemyActor, Is.Not.Null);
                Assert.That(allyActor, Is.Not.Null);
                var enemyFullWidth = Bounds(host, enemyActor.HealthFill076.rectTransform).size.x;
                var focusedEnemyFullWidth = Bounds(
                    host,
                    diorama.FocusedEnemyHpFill076.rectTransform).size.x;

                var firstHit = DamageEvent(35, 1);
                yield return sequence.PlayRound(
                    initial,
                    BattleWithHp(100, 85),
                    new[] { firstHit },
                    true,
                    () => 4f,
                    () => false);
                Canvas.ForceUpdateCanvases();
                Assert.That(enemyActor.PresentedCurrentHp076, Is.EqualTo(85));
                Assert.That(enemyActor.PresentedHpFill076, Is.EqualTo(85f / 120f).Within(0.001f));
                Assert.That(enemyActor.HealthLabel076.text, Is.EqualTo("HP 85 / 120"));
                Assert.That(
                    Bounds(host, enemyActor.HealthFill076.rectTransform).size.x,
                    Is.EqualTo(enemyFullWidth * (85f / 120f)).Within(1.5f),
                    "The sprite-less HP fill must contract its rendered geometry, not only fillAmount.");
                Assert.That(diorama.FocusedEnemyCurrentHp076, Is.EqualTo(285));
                Assert.That(diorama.FocusedEnemyMaximumHp076, Is.EqualTo(320));
                Assert.That(diorama.FocusedEnemyHpLabel076.text,
                    Is.EqualTo("ENEMY UNION HP  285 / 320"));
                Assert.That(GameObject.Find("Target Enemy Union Name 072").GetComponent<Text>().text,
                    Does.Contain("HP 285/320"),
                    "The established top header must use the same live presentation ledger.");
                Assert.That(
                    Bounds(host, diorama.FocusedEnemyHpFill076.rectTransform).size.x,
                    Is.EqualTo(focusedEnemyFullWidth * (285f / 320f)).Within(2f));
                AssertImpact(
                    diorama,
                    "ENEMY  •  GATE GNAWER  •  -35 HP",
                    "MEMBER HP 120  →  85 / 120\nENEMY UNION TOTAL HP 285 / 320",
                    "ENEMY_076",
                    "ENEMY_MEMBER_076",
                    120,
                    85,
                    120,
                    -35,
                    true);

                diorama.Focus("ALLY_076", "ENEMY_076");
                Assert.That(enemyActor.PresentedCurrentHp076, Is.EqualTo(85),
                    "Camera/focus rebinding must not restore the pre-round HP snapshot after an impact.");
                Assert.That(diorama.HpImpactVisible076, Is.True,
                    "Focus rebinding must not erase the impact readout.");

                var allyFullWidth = Bounds(host, allyActor.HealthFill076.rectTransform).size.x;
                var focusedAllyFullWidth = Bounds(
                    host,
                    diorama.FocusedAllyHpFill076.rectTransform).size.x;
                var alliedHit = EnemyDamageEvent(30, 2);
                yield return sequence.PlayRound(
                    BattleWithHp(100, 85),
                    BattleWithHp(70, 85),
                    new[] { alliedHit },
                    true,
                    () => 4f,
                    () => false);
                Canvas.ForceUpdateCanvases();
                Assert.That(allyActor.PresentedCurrentHp076, Is.EqualTo(70));
                Assert.That(allyActor.PresentedHpFill076, Is.EqualTo(0.70f).Within(0.001f));
                Assert.That(allyActor.HealthLabel076.text, Is.EqualTo("HP 70 / 100"));
                Assert.That(
                    Bounds(host, allyActor.HealthFill076.rectTransform).size.x,
                    Is.EqualTo(allyFullWidth * 0.70f).Within(1.5f));
                Assert.That(diorama.FocusedAllyCurrentHp076, Is.EqualTo(270));
                Assert.That(diorama.FocusedAllyMaximumHp076, Is.EqualTo(300));
                Assert.That(diorama.FocusedAllyHpLabel076.text,
                    Is.EqualTo("ALLY UNION HP  270 / 300"));
                Assert.That(GameObject.Find("Focused Ally Union Name 072").GetComponent<Text>().text,
                    Does.Contain("HP 270/300"),
                    "The established ally header must not retain the pre-round snapshot.");
                Assert.That(
                    Bounds(host, diorama.FocusedAllyHpFill076.rectTransform).size.x,
                    Is.EqualTo(focusedAllyFullWidth * 0.90f).Within(2f));
                AssertImpact(
                    diorama,
                    "ALLY  •  ASTER VALE  •  -30 HP",
                    "MEMBER HP 100  →  70 / 100\nGUILD UNION TOTAL HP 270 / 300",
                    "ALLY_076",
                    "ALLY_MEMBER_076",
                    100,
                    70,
                    100,
                    -30,
                    false);
                yield return new WaitForSecondsRealtime(
                    M2BattleDioramaView072.MinimumHpImpactVisibleSeconds076 * 0.55f);
                Assert.That(diorama.HpImpactVisible076, Is.True,
                    "The allied-hit readout vanished before the minimum impact window.");

                var restoration = RestorationEvent(20, 3);
                yield return sequence.PlayRound(
                    BattleWithHp(70, 85),
                    BattleWithHp(90, 85),
                    new[] { restoration },
                    true,
                    () => 4f,
                    () => false);
                Canvas.ForceUpdateCanvases();
                Assert.That(allyActor.PresentedCurrentHp076, Is.EqualTo(90));
                Assert.That(allyActor.PresentedHpFill076, Is.EqualTo(0.90f).Within(0.001f));
                Assert.That(diorama.FocusedAllyCurrentHp076, Is.EqualTo(290));
                Assert.That(diorama.FocusedAllyHpLabel076.text,
                    Is.EqualTo("ALLY UNION HP  290 / 300"));
                AssertImpact(
                    diorama,
                    "ALLY  •  ASTER VALE  •  +20 HP",
                    "MEMBER HP 70  →  90 / 100\nGUILD UNION TOTAL HP 290 / 300",
                    "ALLY_076",
                    "ALLY_MEMBER_076",
                    70,
                    90,
                    100,
                    20,
                    false);

                diorama.Refresh(BattleWithHp(90, 85), "ALLY_076");
                allyActor = diorama.ResolveActor("ALLY_MEMBER_076", "ALLY_076");
                enemyActor = diorama.ResolveActor("ENEMY_MEMBER_076", "ENEMY_076");
                Assert.That(allyActor.PresentedCurrentHp076, Is.EqualTo(90));
                Assert.That(enemyActor.PresentedCurrentHp076, Is.EqualTo(85));
                Assert.That(diorama.HpImpactVisible076, Is.True,
                    "Authoritative refresh must preserve a still-active impact readout.");
                Assert.That(diorama.TryGetPresentedMemberHp076(
                    "ALLY_MEMBER_076", "ALLY_076", out var allyCurrent, out var allyMaximum), Is.True);
                Assert.That(allyCurrent, Is.EqualTo(90));
                Assert.That(allyMaximum, Is.EqualTo(100));

                var effectiveScale = EffectiveCanvasScale(size);
                Assert.That(Bounds(host, enemyActor.HealthRail076.rectTransform).size.y * effectiveScale,
                    Is.GreaterThanOrEqualTo(7f),
                    "A three-member actor HP rail must survive shipping Canvas scaling.");
                Assert.That(Bounds(host, diorama.FocusedEnemyHpRail076.rectTransform).size.y * effectiveScale,
                    Is.GreaterThanOrEqualTo(7f),
                    "The prominent enemy Union HP rail must survive shipping Canvas scaling.");
                Assert.That(diorama.FocusedEnemyHpLabel076.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumFocusedUnionHpFontSize076));
                AssertInside(host, diorama.FocusedAllyHpLabel076.rectTransform, "focused ally HP readout");
                AssertInside(host, diorama.FocusedEnemyHpLabel076.rectTransform, "focused enemy HP readout");
                AssertInside(host, diorama.HpImpactCallout076.rectTransform, "HP impact callout");

                yield return new WaitForSecondsRealtime(
                    M2BattleDioramaView072.MinimumHpImpactVisibleSeconds076 + 0.10f);
                Assert.That(diorama.HpImpactVisible076, Is.False,
                    "The impact readout must clear after its readable window.");

                diorama.Dispose();
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator GateEaterLethalReadoutSeparatesBossHpFromUnionTotalAndKeepsEveryLowerThirdNamed()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Gate-Eater Lethal Readout Host 078 " + size.x, size);
                var owner = new GameObject("Gate-Eater Lethal Readout Owner 078 " + size.x);
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                diorama.Initialize(host);
                var battle = BattleWithHp(100, 21);
                battle.BattleId = "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER";
                battle.EnemyUnions[0].DisplayName = "The Gate-Eater";
                battle.EnemyUnions[0].LeaderMemberId = "ENEMY_HINGE_EATER_COLOSSUS_076";
                battle.EnemyUnions[0].Members = new[]
                {
                    new M2BattleMemberView
                    {
                        MemberId = "ENEMY_HINGE_EATER_COLOSSUS_076",
                        DisplayName = "Hinge-Eater Colossus",
                        ClassName = "Boss",
                        CurrentHp = 21,
                        MaximumHp = 720
                    },
                    new M2BattleMemberView
                    {
                        MemberId = "GATE_EATER_ESCORT_SCOUT_076",
                        DisplayName = "Gate-Eater Scout",
                        ClassName = "Raider",
                        CurrentHp = 0,
                        MaximumHp = 90,
                        Downed = true
                    },
                    new M2BattleMemberView
                    {
                        MemberId = "GATE_EATER_ESCORT_MEDIC_076",
                        DisplayName = "Ash Medic",
                        ClassName = "Raider",
                        CurrentHp = 0,
                        MaximumHp = 90,
                        Downed = true
                    }
                };
                diorama.Refresh(battle, "ALLY_076");
                yield return null;

                var lethal = DamageEvent(21, 78);
                lethal.TargetUnionId = "ENEMY_076";
                lethal.TargetMemberId = "ENEMY_HINGE_EATER_COLOSSUS_076";
                lethal.MemberId = "ENEMY_HINGE_EATER_COLOSSUS_076";
                Assert.That(diorama.PresentHpEvent076(lethal), Is.True);
                Canvas.ForceUpdateCanvases();

                Assert.That(diorama.HpImpactHeading076.text,
                    Is.EqualTo("BOSS MEMBER  •  THE GATE-EATER  •  -21 HP"));
                Assert.That(diorama.HpImpactDetail076.text,
                    Is.EqualTo("BOSS MEMBER HP 21  →  0 / 720\nENEMY UNION TOTAL HP 0 / 900"),
                    "The lethal card must not make 0/720 boss HP look like the 0/900 Union total.");
                Assert.That(diorama.FocusedEnemyHpLabel076.text,
                    Is.EqualTo("ENEMY UNION HP  0 / 900"));

                var expected = new[]
                {
                    new[] { "ENEMY_HINGE_EATER_COLOSSUS_076", "THE GATE-EATER  •  THREAT X  •  DOWN", "HP 0 / 720" },
                    new[] { "GATE_EATER_ESCORT_SCOUT_076", "Gate-Eater Scout  •  THREAT I  •  DOWN", "HP 0 / 90" },
                    new[] { "GATE_EATER_ESCORT_MEDIC_076", "Ash Medic  •  THREAT I  •  DOWN", "HP 0 / 90" }
                };
                foreach (var row in expected)
                {
                    var actor = diorama.ResolveActor(row[0], "ENEMY_076");
                    Assert.That(actor, Is.Not.Null, row[0]);
                    actor.SetEmphasis(0.40f, 0.96f);
                    Assert.That(actor.NameLabel076.text, Is.EqualTo(row[1]));
                    Assert.That(actor.HealthLabel076.text, Is.EqualTo(row[2]));
                    Assert.That(actor.NamePlate076.gameObject.activeInHierarchy, Is.True);
                    Assert.That(actor.LowerThirdCanvas076.ignoreParentGroups, Is.True,
                        row[0] + " lower third must ignore cinematic root dimming.");
                    Assert.That(actor.LowerThirdCanvas076.alpha, Is.EqualTo(1f));
                    Assert.That(actor.NameLabel076.resizeTextMinSize, Is.GreaterThanOrEqualTo(15));
                    Assert.That(actor.HealthLabel076.resizeTextMinSize, Is.GreaterThanOrEqualTo(16));
                }

                battle.EnemyUnions[0].Members[0].CurrentHp = 0;
                battle.EnemyUnions[0].Members[0].Downed = true;
                battle.EnemyUnions[0].CanAct = false;
                var defeatedIntent = M2BattleDioramaView072.EnemyIntent076(
                    battle.EnemyUnions[0],
                    battle.PlayerUnions[0],
                    battle);
                Assert.That(defeatedIntent, Is.EqualTo("BOSS DOWN  •  SKYHOME SAFE"));
                Assert.That(defeatedIntent, Does.Not.Contain("BREACH"),
                    "A zero-HP downed Gate-Eater must never keep advertising a live boss clock.");
                diorama.Refresh(battle, "ALLY_076");
                yield return null;
                Assert.That(GameObject.Find("Enemy Intent 076").GetComponent<Text>().text,
                    Is.EqualTo("BOSS DOWN  •  SKYHOME SAFE"),
                    "The live battlefield intent must retire the boss clock after lethal damage.");

                AssertInside(host, diorama.HpImpactCallout076.rectTransform, "boss-member HP impact callout");
                diorama.Dispose();
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LethalLegacyArtStillPresentsZeroHpDownedAndVictoryWithoutExactRecipeCredit()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var host = Host("Lethal Legacy Art Test Host 076", new Vector2(1280f, 800f));
            var owner = new GameObject("Lethal Legacy Art Test Owner 076");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            var audio = owner.AddComponent<M2BattleAudioDirector>();
            var sequence = owner.AddComponent<M2BattleSequenceDirector072>();
            var victoryCaptionObserved = false;
            diorama.Initialize(host);
            sequence.Initialize(diorama, audio, (heading, _) =>
            {
                if (StringComparer.Ordinal.Equals(heading, "VICTORY"))
                    victoryCaptionObserved = true;
            });

            var before = BattleWithHp(100, 47);
            before.Round = 4;
            before.EnemyUnions[0].Members = before.EnemyUnions[0].Members.Take(1).ToArray();
            before.PlayerUnions[0].Members[0].DisplayName = "Maren Holt";
            before.PlayerUnions[0].Members[0].PortraitAuthorityId = "SIGREC_MAREN_HOLT";
            var after = BattleWithHp(100, 0);
            after.Round = 4;
            after.LastResolvedRound = 4;
            after.Outcome = "Victory";
            after.IsResolved = true;
            after.EnemyUnions[0].Members = after.EnemyUnions[0].Members.Take(1).ToArray();
            after.PlayerUnions[0].Members[0].DisplayName = "Maren Holt";
            after.PlayerUnions[0].Members[0].PortraitAuthorityId = "SIGREC_MAREN_HOLT";

            var lethalHit = DamageEvent(47, 71);
            lethalHit.Round = 4;
            lethalHit.ArtId = "ART_BASIC_THRUST";
            lethalHit.Text = "The final thrust deals 47 HP.";
            var events = new[]
            {
                lethalHit,
                new M2BattleEventView
                {
                    Sequence = 72,
                    Round = 4,
                    EventType = "DOWNED",
                    Side = "Enemy",
                    UnionId = "ENEMY_076",
                    MemberId = "ENEMY_MEMBER_076",
                    ActorUnionId = "ALLY_076",
                    ActorMemberId = "ALLY_MEMBER_076",
                    TargetUnionId = "ENEMY_076",
                    TargetMemberId = "ENEMY_MEMBER_076",
                    ArtId = "ART_BASIC_THRUST",
                    Text = "The Gate Gnawer is Downed — not dead."
                },
                new M2BattleEventView
                {
                    Sequence = 73,
                    Round = 4,
                    EventType = "BATTLE_RESULT",
                    Text = "The enemy Union is defeated. Victory."
                }
            };
            after.LastResolvedRoundEvents = events;
            after.Events = events;

            diorama.Refresh(before, "ALLY_076");
            yield return null;
            var target = diorama.ResolveActor("ENEMY_MEMBER_076", "ENEMY_076");
            var victor = diorama.ResolveActor("ALLY_MEMBER_076", "ALLY_076");
            Assert.That(target, Is.Not.Null);
            Assert.That(victor, Is.Not.Null);
            Assert.That(target.CurrentPoseId, Is.Not.EqualTo(BattleArtPoseDirector011.Downed));

            sequence.StartCoroutine(sequence.PlayRound(
                before,
                after,
                events,
                false,
                () => 4f,
                () => false));
            var startedDeadline = Time.realtimeSinceStartup + 3f;
            while (!sequence.IsPlaying && Time.realtimeSinceStartup < startedDeadline)
                yield return null;
            Assert.That(sequence.IsPlaying, Is.True);

            var sawVisibleLethalImpact = false;
            var sawVictoryPose = false;
            var completionDeadline = Time.realtimeSinceStartup + 12f;
            while (sequence.IsPlaying && Time.realtimeSinceStartup < completionDeadline)
            {
                sawVisibleLethalImpact = sawVisibleLethalImpact ||
                    (diorama.HpImpactVisible076 &&
                     diorama.LastHpImpactBefore076 == 47 &&
                     diorama.LastHpImpactAfter076 == 0 &&
                     diorama.LastHpImpactDelta076 == -47 &&
                     target.PresentedCurrentHp076 == 0 &&
                     Mathf.Approximately(target.PresentedHpFill076, 0f) &&
                     target.HealthLabel076.text == "HP 0 / 120");
                sawVictoryPose = sawVictoryPose ||
                    StringComparer.Ordinal.Equals(
                        victor.CurrentPoseId,
                        BattleArtPoseDirector011.Victory);
                yield return null;
            }

            Assert.That(sequence.IsPlaying, Is.False, "The unskipped lethal round did not complete.");
            Assert.That(sawVisibleLethalImpact, Is.True,
                "The legacy final hit never visibly changed 47 HP to zero at contact.");
            Assert.That(sawVictoryPose, Is.True,
                "The terminal BATTLE_RESULT never presented the allied victory pose.");
            Assert.That(victoryCaptionObserved, Is.True);
            Assert.That(sequence.LastPresentedBeatCount, Is.EqualTo(3));
            Assert.That(target.PresentedCurrentHp076, Is.Zero);
            Assert.That(target.PresentedHpFill076, Is.Zero);
            Assert.That(target.HealthLabel076.text, Is.EqualTo("HP 0 / 120"));
            Assert.That(target.CurrentPoseId, Is.EqualTo(BattleArtPoseDirector011.Downed));
            var downedArtwork = target.CurrentArtwork076;
            Assert.That(downedArtwork, Is.Not.Null);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                    0f,
                    downedArtwork.rectTransform.localEulerAngles.z)),
                Is.EqualTo(M2BattleActorRig072.DownedArtworkTiltDegrees076).Within(0.1f),
                "DOWNED must visibly topple the character artwork even when an enemy reuses its idle sprite.");
            Assert.That(downedArtwork.rectTransform.anchoredPosition.y,
                Is.EqualTo(-M2BattleActorRig072.DownedArtworkSettlePixels076).Within(0.1f));
            Assert.That(downedArtwork.rectTransform.localScale.x,
                Is.EqualTo(M2BattleActorRig072.DownedArtworkScale076).Within(0.001f));
            Assert.That(downedArtwork.color.r, Is.LessThan(0.60f),
                "The defeated artwork must be visibly muted rather than presenting the live idle palette.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0f, target.Root.localEulerAngles.z)),
                Is.LessThan(0.01f),
                "Artwork treatment must never rotate the actor root used by exact-beat home checks.");
            Assert.That(target.Root.anchoredPosition, Is.EqualTo(target.HomePosition));
            Assert.That(target.Root.localScale, Is.EqualTo(target.HomeScale));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                    0f,
                    target.NamePlate076.rectTransform.localEulerAngles.z)),
                Is.LessThan(0.01f),
                "The HP rail and lower third must stay upright after a lethal hit.");
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                    0f,
                    target.HealthLabel076.rectTransform.localEulerAngles.z)),
                Is.LessThan(0.01f));
            Assert.That(diorama.LiveArtRecipeDiagnostics076.StartedExactBeatCount, Is.Zero);
            Assert.That(diorama.LiveArtRecipeDiagnostics076.CompletedExactBeatCount, Is.Zero,
                "A legacy alias must present lethality without claiming canonical exact-recipe credit.");
            Assert.That(sequence.ExactRecipeImpactSfxCount076, Is.Zero);

            diorama.Refresh(after, "ALLY_076");
            yield return null;
            target = diorama.ResolveActor("ENEMY_MEMBER_076", "ENEMY_076");
            Assert.That(target, Is.Not.Null);
            Assert.That(target.Downed, Is.True);
            Assert.That(target.PresentedCurrentHp076, Is.Zero);
            Assert.That(target.CurrentPoseId, Is.EqualTo(BattleArtPoseDirector011.Downed));

            Assert.That(target.SetPoseImmediate(BattleArtPoseDirector011.Idle), Is.True);
            Assert.That(target.CurrentArtwork076.rectTransform.anchoredPosition,
                Is.EqualTo(Vector2.zero),
                "Every non-DOWNED pose must clear the artwork settle offset.");
            Assert.That(target.CurrentArtwork076.rectTransform.localScale,
                Is.EqualTo(Vector3.one));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                    0f,
                    target.CurrentArtwork076.rectTransform.localEulerAngles.z)),
                Is.LessThan(0.01f));
            Assert.That(target.EnemyThreatTier089, Is.EqualTo(2));
            Assert.That(target.CurrentArtwork076.color,
                Is.EqualTo(M2EnemyThreatPalette089.Visual(2).ArtworkTint),
                "Returning to idle must restore the deterministic Threat II tint, not erase threat readability.");
            Assert.That(target.SetPoseImmediate(BattleArtPoseDirector011.Downed), Is.True);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(
                    0f,
                    target.CurrentArtwork076.rectTransform.localEulerAngles.z)),
                Is.EqualTo(M2BattleActorRig072.DownedArtworkTiltDegrees076).Within(0.1f),
                "The immediate path must restore the same deterministic DOWNED treatment.");

            diorama.Dispose();
            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BreakthroughNotificationsAutoDismissQueueAndClearOnRefresh()
        {
            var host = Host("Breakthrough Queue Test Host 076", new Vector2(1280f, 800f));
            var owner = new GameObject("Breakthrough Queue Test Owner 076");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var battle = BattleWithHp(100, 120);
            var first = BreakthroughEvent(
                10,
                "ALLY_MEMBER_076",
                "ART_POWER_CUT",
                "Power Cut");
            var second = BreakthroughEvent(
                11,
                "ALLY_HEALER_076",
                "ART_FLAME_SPIRAL",
                "Flame Spiral");
            battle.Events = new[] { first };
            diorama.Refresh(battle, "ALLY_076");
            yield return null;

            Assert.That(diorama.BreakthroughNotificationVisible076, Is.False,
                "Refreshing a battle with historical discoveries must not reopen the toast.");
            Assert.That(diorama.PresentBreakthroughEvent076(first), Is.True);
            Assert.That(diorama.BreakthroughNotificationVisible076, Is.True);
            Assert.That(diorama.BreakthroughNotificationPresentationCount076, Is.EqualTo(1));
            Assert.That(diorama.BreakthroughNotificationHeading076.text,
                Is.EqualTo("NEW ART LEARNED"));
            Assert.That(diorama.BreakthroughNotificationDetail076.text,
                Does.Contain("ASTER VALE LEARNED").And.Contain("POWER CUT"));
            Assert.That(
                FirstHourGoldSmoke071.HasReadableTransientLearnedArtCopy078(
                    diorama.BreakthroughNotificationHeading076.text,
                    diorama.BreakthroughNotificationDetail076.text),
                Is.True,
                "The built-player smoke must accept the exact transient notice shown in battle.");
            Assert.That(diorama.BreakthroughNotificationRemainingSeconds076,
                Is.GreaterThan(2.5f));
            Assert.That(diorama.PresentBreakthroughEvent076(first), Is.False,
                "The same authoritative discovery must notify only once.");
            Assert.That(diorama.PresentBreakthroughEvent076(BreakthroughEvent(
                    10,
                    "ALLY_MEMBER_076",
                    "ART_POWER_CUT",
                    "Localized copy changed, but this is still the same discovery.")), Is.False,
                "Display-copy changes must not defeat authoritative learned-Art deduplication.");

            Assert.That(diorama.PresentBreakthroughEvent076(second), Is.True);
            Assert.That(diorama.QueuedBreakthroughNotificationCount076, Is.EqualTo(1));
            Assert.That(diorama.BreakthroughNotificationDetail076.text,
                Does.Contain("POWER CUT"),
                "A queued discovery must not overwrite the notification being read.");

            yield return new WaitForSecondsRealtime(
                M2BattleDioramaView072.BreakthroughNotificationSeconds076 + 0.10f);
            Assert.That(diorama.BreakthroughNotificationVisible076, Is.True);
            Assert.That(diorama.BreakthroughNotificationPresentationCount076, Is.EqualTo(2));
            Assert.That(diorama.QueuedBreakthroughNotificationCount076, Is.Zero);
            Assert.That(diorama.BreakthroughNotificationDetail076.text,
                Does.Contain("MIRA DAWN LEARNED").And.Contain("FLAME SPIRAL"));
            Assert.That(
                FirstHourGoldSmoke071.HasReadableTransientLearnedArtCopy078(
                    diorama.BreakthroughNotificationHeading076.text,
                    diorama.BreakthroughNotificationDetail076.text),
                Is.True);

            yield return new WaitForSecondsRealtime(
                M2BattleDioramaView072.BreakthroughNotificationSeconds076 + 0.10f);
            Assert.That(diorama.BreakthroughNotificationVisible076, Is.False);
            Assert.That(diorama.QueuedBreakthroughNotificationCount076, Is.Zero);

            var nextCommandDiscovery = BreakthroughEvent(
                12,
                "ALLY_MAGE_076",
                "ART_AETHER_LANCE",
                "Aether Lance");
            Assert.That(diorama.PresentBreakthroughEvent076(nextCommandDiscovery), Is.True);
            Assert.That(diorama.BreakthroughNotificationVisible076, Is.True);
            diorama.Refresh(battle, "ALLY_076");
            Assert.That(diorama.BreakthroughNotificationVisible076, Is.False,
                "The live notification must never obstruct the next command state.");
            Assert.That(diorama.QueuedBreakthroughNotificationCount076, Is.Zero);

            diorama.Dispose();
            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(host.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator LearnedArtToastStaysClearOfExactHpImpactAndUsesCompactPlayerCopy()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Learned Art Lane Test Host 078 " + size.x, size);
                var owner = new GameObject("Learned Art Lane Test Owner 078 " + size.x);
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                diorama.Initialize(host);
                var battle = BattleWithHp(100, 120);
                diorama.Refresh(battle, "ALLY_076");
                yield return null;

                var impact = DamageEvent(35, 20);
                var learned = BreakthroughEvent(
                    21,
                    "ALLY_MEMBER_076",
                    "ART_POWER_CUT",
                    "Aster Vale turns meaningful Hammerfall use into a breakthrough and learns " +
                    "Power Cut! It enters later legal Forecast pools.");
                Assert.That(diorama.PresentHpEvent076(impact), Is.True);
                Assert.That(diorama.PresentBreakthroughEvent076(learned), Is.True);
                Canvas.ForceUpdateCanvases();

                var toast = Bounds(host,
                    diorama.BreakthroughNotificationCallout076.rectTransform);
                var hpImpact = Bounds(host, diorama.HpImpactCallout076.rectTransform);
                var separated = toast.max.x <= hpImpact.min.x - 1f ||
                                toast.min.x >= hpImpact.max.x + 1f ||
                                toast.max.y <= hpImpact.min.y - 1f ||
                                toast.min.y >= hpImpact.max.y + 1f;
                Assert.That(separated, Is.True,
                    "The three-second learned-Art toast must never cover exact before/after HP.");
                Assert.That(diorama.BreakthroughNotificationHeading076.text,
                    Is.EqualTo("NEW ART LEARNED"));
                Assert.That(diorama.BreakthroughNotificationDetail076.text,
                    Is.EqualTo("ASTER VALE LEARNED\nPOWER CUT\nREADY FOR FUTURE BATTLES"));
                Assert.That(diorama.BreakthroughNotificationDetail076.text,
                    Does.Not.Contain("LEGAL").And.Not.Contain("FORECAST").And.Not.Contain("..."));
                Assert.That(
                    M2BattleSequenceDirector072.BreakthroughCaptionForVerification076(learned),
                    Is.EqualTo("POWER CUT LEARNED  •  READY FOR FUTURE BATTLES"));
                Assert.That(diorama.BreakthroughNotificationRemainingSeconds076,
                    Is.GreaterThan(2.5f),
                    "Moving the toast must preserve the existing three-second lifecycle.");
                AssertInside(host,
                    diorama.BreakthroughNotificationCallout076.rectTransform,
                    "learned-Art toast");

                var detail = diorama.BreakthroughNotificationDetail076;
                var settings = detail.GetGenerationSettings(detail.rectTransform.rect.size);
                Assert.That(detail.cachedTextGenerator.Populate(detail.text, settings), Is.True);
                Assert.That(detail.cachedTextGenerator.lines.Count, Is.EqualTo(3),
                    size + " learned-Art copy wrapped beyond its three authored lines.");

                diorama.Dispose();
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NextArtProgressUsesExactCurrentAndRequiredValuesAtSupportedResolutions()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Skill Progress Test Host 076 " + size.x, size);
                var owner = new GameObject("Skill Progress Test Owner 076 " + size.x);
                var results = owner.AddComponent<M2BattleResultsView072>();
                results.Initialize(host, () => { });
                results.Show(ResolvedBattleWithSkillProgress());
                yield return null;
                Canvas.ForceUpdateCanvases();

                var label = GameObject.Find("Next Art Progress Label 076").GetComponent<Text>();
                var rail = GameObject.Find("Next Art Progress Rail 076").GetComponent<RectTransform>();
                var fill = GameObject.Find("Next Art Progress Fill 076").GetComponent<RectTransform>();
                Assert.That(label.gameObject.activeInHierarchy, Is.True);
                Assert.That(label.text,
                    Is.EqualTo("NEXT ART  •  FLAME SPIRAL\nDISCOVERY  34 / 80  •  46 TO GO"));
                Assert.That(label.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleResultsView072.MinimumSkillProgressFontSize076));
                Assert.That(M2BattleResultsView072.SkillProgressRatio076(
                        ResolvedBattleWithSkillProgress().PlayerUnions[0].Members[0].NextSkillProgress),
                    Is.EqualTo(34f / 80f).Within(0.001f));
                Assert.That(fill.rect.width, Is.EqualTo(rail.rect.width * (34f / 80f)).Within(1f),
                    "The fill must visualize the exact authority-projected discovery fraction.");
                Assert.That(rail.rect.height, Is.GreaterThanOrEqualTo(8f),
                    "The next-Art rail must remain visibly thick at " + size.x + "x" + size.y + ".");
                AssertInside(host, label.rectTransform, "next-Art progress label");
                AssertInside(host, rail, "next-Art progress rail");

                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator FullDiscoveryUsesProjectedLevelRewardWithoutRequiredContradiction()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Projected Skill Gate Test Host 078 " + size.x, size);
                var owner = new GameObject("Projected Skill Gate Test Owner 078 " + size.x);
                var results = owner.AddComponent<M2BattleResultsView072>();
                results.Initialize(host, () => { });
                var battle = ResolvedBattleWithSkillProgress();
                var progress = battle.PlayerUnions[0].Members[0].NextSkillProgress;
                progress.CurrentPoints = 12;
                progress.RequiredPoints = 12;
                progress.RequiredLevel = 2;
                progress.LevelGateMet = false;
                battle.Reward.MemberRewards[0].ProjectedLevel = 2;
                battle.Reward.MemberRewards[0].LevelsGained = 1;

                results.Show(battle);
                yield return null;
                Canvas.ForceUpdateCanvases();

                var label = GameObject.Find("Next Art Progress Label 076").GetComponent<Text>();
                var level = GameObject.Find("Featured Adventurer Level 076").GetComponent<Text>();
                var rail = GameObject.Find("Next Art Progress Rail 076").GetComponent<RectTransform>();
                var fill = GameObject.Find("Next Art Progress Fill 076").GetComponent<RectTransform>();
                Assert.That(level.text, Is.EqualTo("LEVEL 1  →  2"));
                Assert.That(label.text, Is.EqualTo(
                    "NEXT ART  •  FLAME SPIRAL\n" +
                    "DISCOVERY 12/12  •  LEVEL 2 EARNED  •  READY NEXT USE"));
                Assert.That(label.text, Does.Not.Contain("LEVEL 2 REQUIRED"));
                Assert.That(M2BattleResultsView072.SkillProgressLabel076(progress, 2),
                    Is.EqualTo(label.text));
                Assert.That(fill.rect.width, Is.EqualTo(rail.rect.width).Within(1f));
                AssertInside(host, label.rectTransform, "projected-level skill progress");
                var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
                Assert.That(label.cachedTextGenerator.Populate(label.text, settings), Is.True);
                Assert.That(label.cachedTextGenerator.lines.Count, Is.EqualTo(2),
                    size + " projected-level wording wrapped or clipped.");
                Assert.That(label.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleResultsView072.MinimumSkillProgressFontSize076));

                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SixMemberDioramaKeepsAlliedAndEnemyHpReadableAtSupportedResolutions()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Six Member HP Test Host 078 " + size.x, size);
                var owner = new GameObject("Six Member HP Test Owner 078 " + size.x);
                var diorama = owner.AddComponent<M2BattleDioramaView072>();
                var sequence = owner.AddComponent<M2BattleSequenceDirector072>();
                diorama.Initialize(host);
                sequence.Initialize(diorama, null, (_, __) => { });
                var initial = SixMemberBattleWithHp(100, 120);
                diorama.Refresh(initial, "ALLY_076");
                yield return null;
                Canvas.ForceUpdateCanvases();

                Assert.That(diorama.ActiveActorCount, Is.EqualTo(12));
                var enemyActor = diorama.ResolveActor("ENEMY_MEMBER_076", "ENEMY_076");
                var allyActor = diorama.ResolveActor("ALLY_MEMBER_076", "ALLY_076");
                Assert.That(enemyActor, Is.Not.Null);
                Assert.That(allyActor, Is.Not.Null);
                var enemyFullWidth = Bounds(host, enemyActor.HealthFill076.rectTransform).size.x;
                var focusedEnemyFullWidth = Bounds(
                    host,
                    diorama.FocusedEnemyHpFill076.rectTransform).size.x;

                yield return sequence.PlayRound(
                    initial,
                    SixMemberBattleWithHp(100, 85),
                    new[] { DamageEvent(35, 20) },
                    true,
                    () => 4f,
                    () => false);
                Canvas.ForceUpdateCanvases();

                Assert.That(enemyActor.HealthLabel076.text, Is.EqualTo("HP 85 / 120"));
                Assert.That(
                    Bounds(host, enemyActor.HealthFill076.rectTransform).size.x,
                    Is.EqualTo(enemyFullWidth * (85f / 120f)).Within(1.5f));
                Assert.That(diorama.FocusedEnemyHpLabel076.text,
                    Is.EqualTo("ENEMY UNION HP  585 / 620"));
                Assert.That(
                    Bounds(host, diorama.FocusedEnemyHpFill076.rectTransform).size.x,
                    Is.EqualTo(focusedEnemyFullWidth * (585f / 620f)).Within(2f));

                yield return sequence.PlayRound(
                    SixMemberBattleWithHp(100, 85),
                    SixMemberBattleWithHp(70, 85),
                    new[] { EnemyDamageEvent(30, 21) },
                    true,
                    () => 4f,
                    () => false);
                Canvas.ForceUpdateCanvases();

                Assert.That(allyActor.HealthLabel076.text, Is.EqualTo("HP 70 / 100"));
                Assert.That(diorama.FocusedAllyHpLabel076.text,
                    Is.EqualTo("ALLY UNION HP  570 / 600"));
                Assert.That(diorama.FocusedAllyHpLabel076.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumFocusedUnionHpFontSize076));
                Assert.That(diorama.FocusedEnemyHpLabel076.resizeTextMinSize,
                    Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumFocusedUnionHpFontSize076));
                Assert.That(Bounds(host, diorama.FocusedAllyHpRail076.rectTransform).size.y,
                    Is.GreaterThanOrEqualTo(7f));
                Assert.That(Bounds(host, diorama.FocusedEnemyHpRail076.rectTransform).size.y,
                    Is.GreaterThanOrEqualTo(7f));
                AssertInside(host, diorama.FocusedAllyHpLabel076.rectTransform,
                    "six-member focused ally HP readout");
                AssertInside(host, diorama.FocusedEnemyHpLabel076.rectTransform,
                    "six-member focused enemy HP readout");

                diorama.Dispose();
                UnityEngine.Object.Destroy(owner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator BattlefieldActorLowerThirdsClearCommandDockAtSupportedResolutions()
        {
            foreach (var size in SupportedResolutions())
            {
                var host = Host("Actor Dock Clearance Host 078 " + size.x, size);
                var dioramaOwner = new GameObject("Actor Dock Clearance Diorama 078 " + size.x);
                var hudOwner = new GameObject("Actor Dock Clearance HUD 078 " + size.x);
                var diorama = dioramaOwner.AddComponent<M2BattleDioramaView072>();
                var hud = hudOwner.AddComponent<M2BattleCommandHud072>();
                diorama.Initialize(host);
                hud.Initialize(host, _ => { }, (_, __) => { }, (_, __) => { }, () => { });

                var battle = SixMemberBattleWithHp(100, 120);
                battle.BattleId = "BATTLE_CONTRACT_ENCOUNTER071_GATE_EATER";
                battle.EnemyUnions[0].DisplayName = "The Gate-Eater";
                battle.EnemyUnions[0].LeaderMemberId = "ENEMY_HINGE_EATER_COLOSSUS_076";
                battle.EnemyUnions[0].Members[0].MemberId =
                    "ENEMY_HINGE_EATER_COLOSSUS_076";
                battle.EnemyUnions[0].Members[0].DisplayName = "Hinge-Eater Colossus";
                battle.EnemyUnions[0].Members[0].ClassName = "Boss";
                diorama.Refresh(battle, "ALLY_076");
                hud.Refresh(battle, "ALLY_076");
                yield return null;
                Canvas.ForceUpdateCanvases();

                var commandDock = host.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name == "Battle Command HUD 072");
                var commandDockBounds = Bounds(host, commandDock);
                var lowerThirds = host.GetComponentsInChildren<Image>(true)
                    .Where(value => value.name == "Authored Actor Lower Third 076" &&
                                    value.gameObject.activeInHierarchy)
                    .ToArray();
                Assert.That(lowerThirds.Length, Is.EqualTo(12),
                    "Six allied and six enemy battlefield actors must retain their own lower-thirds.");
                foreach (var lowerThird in lowerThirds)
                {
                    var clearance = Bounds(host, lowerThird.rectTransform).min.y -
                                    commandDockBounds.max.y;
                    Assert.That(clearance, Is.GreaterThanOrEqualTo(6f),
                        lowerThird.transform.parent.name +
                        " must keep its exact member HP/nameplate above the command dock at " + size + ".");
                }

                var focusedMemberCells = host.GetComponentsInChildren<Text>(true)
                    .Where(value => value.name.StartsWith(
                                        "Active Union Member State ",
                                        StringComparison.Ordinal) &&
                                    value.gameObject.activeInHierarchy)
                    .ToArray();
                Assert.That(focusedMemberCells.Length, Is.EqualTo(6),
                    "The focused-Union dock telemetry is separate from the twelve actor nameplates.");
                Assert.That(focusedMemberCells.All(value =>
                        Bounds(host, value.rectTransform).max.y <= commandDockBounds.max.y + 1f),
                    Is.True,
                    "Focused-member telemetry must remain inside the command dock while actor HP stays above it.");

                diorama.Dispose();
                UnityEngine.Object.Destroy(dioramaOwner);
                UnityEngine.Object.Destroy(hudOwner);
                UnityEngine.Object.Destroy(host.gameObject);
                yield return null;
            }
        }

        private static Vector2[] SupportedResolutions() => new[]
        {
            new Vector2(1280f, 800f),
            new Vector2(1920f, 1080f)
        };

        private static RectTransform Host(string name, Vector2 size)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        private static M2BattleEventView DamageEvent(int amount, int sequence) =>
            new M2BattleEventView
            {
                Sequence = sequence,
                Round = 1,
                EventType = "MARTIAL_HIT",
                Amount = amount,
                ActorUnionId = "ALLY_076",
                ActorMemberId = "ALLY_MEMBER_076",
                TargetUnionId = "ENEMY_076",
                TargetMemberId = "ENEMY_MEMBER_076",
                ArtId = "ART_BASIC_SABER_CUT",
                Text = "The attack deals " + amount + " HP."
            };

        private static M2BattleEventView EnemyDamageEvent(int amount, int sequence) =>
            new M2BattleEventView
            {
                Sequence = sequence,
                Round = 1,
                EventType = "ENEMY_HIT",
                Side = "Enemy",
                UnionId = "ALLY_076",
                MemberId = "ALLY_MEMBER_076",
                Amount = amount,
                ActorUnionId = "ENEMY_076",
                ActorMemberId = "ENEMY_MEMBER_076",
                TargetUnionId = "ALLY_076",
                TargetMemberId = "ALLY_MEMBER_076",
                ArtId = "ART_ENEMY_RAVAGE",
                Text = "The enemy attack deals " + amount + " HP."
            };

        private static M2BattleEventView RestorationEvent(int amount, int sequence) =>
            new M2BattleEventView
            {
                Sequence = sequence,
                Round = 1,
                EventType = "RESTORATION",
                Side = "Player",
                UnionId = "ALLY_076",
                MemberId = "ALLY_MEMBER_076",
                Amount = amount,
                ActorUnionId = "ALLY_076",
                ActorMemberId = "ALLY_HEALER_076",
                TargetUnionId = "ALLY_076",
                TargetMemberId = "ALLY_MEMBER_076",
                ArtId = "ART_RESTORE_LIGHT",
                Text = "Restoration returns " + amount + " HP."
            };

        private static M2BattleEventView BreakthroughEvent(
            int sequence,
            string memberId,
            string artId,
            string text) =>
            new M2BattleEventView
            {
                Sequence = sequence,
                Round = 1,
                EventType = "BREAKTHROUGH",
                Side = "Player",
                UnionId = "ALLY_076",
                MemberId = memberId,
                ActorUnionId = "ALLY_076",
                ActorMemberId = memberId,
                ArtId = artId,
                Text = text
            };

        private static M2BattleView BattleWithHp(int allyHp, int enemyHp) =>
            new M2BattleView
            {
                BattleId = "BATTLE_READABILITY_076",
                Round = 1,
                Outcome = "In Progress",
                Objective = "Prove visible battle damage.",
                PlayerUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "ALLY_076",
                        DisplayName = "First Union",
                        Side = "Player",
                        LeaderMemberId = "ALLY_MEMBER_076",
                        CurrentAp = 12,
                        MaximumAp = 12,
                        Cohesion = 100,
                        FormationConditionPercent = 100,
                        Members = new[]
                        {
                            new M2BattleMemberView
                            {
                                MemberId = "ALLY_MEMBER_076",
                                DisplayName = "Aster Vale",
                                ClassName = "Warrior",
                                CurrentHp = allyHp,
                                MaximumHp = 100
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ALLY_HEALER_076",
                                DisplayName = "Mira Dawn",
                                ClassName = "Healer",
                                CurrentHp = 100,
                                MaximumHp = 100
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ALLY_MAGE_076",
                                DisplayName = "Kiri Aetherheart",
                                ClassName = "Mage",
                                CurrentHp = 100,
                                MaximumHp = 100
                            }
                        }
                    }
                },
                EnemyUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "ENEMY_076",
                        DisplayName = "Gate Gnawers",
                        Side = "Enemy",
                        LeaderMemberId = "ENEMY_MEMBER_076",
                        CurrentAp = 9,
                        MaximumAp = 9,
                        Cohesion = 100,
                        FormationConditionPercent = 100,
                        Members = new[]
                        {
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_MEMBER_076",
                                DisplayName = "Gate Gnawer",
                                ClassName = "Raider",
                                CurrentHp = enemyHp,
                                MaximumHp = 120,
                                Downed = enemyHp <= 0
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_MEMBER_2_076",
                                DisplayName = "Gate Scraper",
                                ClassName = "Raider",
                                CurrentHp = 100,
                                MaximumHp = 100
                            },
                            new M2BattleMemberView
                            {
                                MemberId = "ENEMY_MEMBER_3_076",
                                DisplayName = "Gate Biter",
                                ClassName = "Raider",
                                CurrentHp = 100,
                                MaximumHp = 100
                            }
                        }
                    }
                }
            };

        private static M2BattleView SixMemberBattleWithHp(int allyHp, int enemyHp)
        {
            var battle = BattleWithHp(allyHp, enemyHp);
            battle.PlayerUnions[0].Members = Enumerable.Range(0, 6)
                .Select(index => new M2BattleMemberView
                {
                    MemberId = index == 0
                        ? "ALLY_MEMBER_076"
                        : index == 1
                            ? "ALLY_HEALER_076"
                            : index == 2 ? "ALLY_MAGE_076" : "ALLY_MEMBER_076_" + index,
                    DisplayName = index == 0 ? "Aster Vale" : "Guild Member " + (index + 1),
                    ClassName = index == 1 ? "Healer" : index == 2 ? "Mage" : "Warrior",
                    CurrentHp = index == 0 ? allyHp : 100,
                    MaximumHp = 100,
                    Downed = index == 0 && allyHp <= 0
                })
                .ToArray();
            battle.EnemyUnions[0].Members = Enumerable.Range(0, 6)
                .Select(index => new M2BattleMemberView
                {
                    MemberId = index == 0
                        ? "ENEMY_MEMBER_076"
                        : "ENEMY_MEMBER_076_" + index,
                    DisplayName = index == 0 ? "Gate Gnawer" : "Gate Raider " + (index + 1),
                    ClassName = "Raider",
                    CurrentHp = index == 0 ? enemyHp : 100,
                    MaximumHp = index == 0 ? 120 : 100,
                    Downed = index == 0 && enemyHp <= 0
                })
                .ToArray();
            return battle;
        }

        private static M2BattleView ResolvedBattleWithSkillProgress()
        {
            var member = new M2BattleMemberView
            {
                MemberId = "ALLY_MEMBER_076",
                DisplayName = "Aster Vale",
                ClassName = "Mage",
                CurrentHp = 72,
                MaximumHp = 100,
                ArtGrowthSummary = "Ember Thread M34",
                NextSkillProgress = new M2BattleSkillProgressView
                {
                    SkillId = "ART_FLAME_SPIRAL",
                    DisplayName = "Flame Spiral",
                    ProgressKind = "Discovery",
                    CurrentPoints = 34,
                    RequiredPoints = 80,
                    RequiredLevel = 2,
                    LevelGateMet = true
                }
            };
            return new M2BattleView
            {
                BattleId = "BATTLE_READABILITY_RESULT_076",
                Round = 2,
                LastResolvedRound = 2,
                Outcome = "Victory",
                Objective = "Drive the raiders from the road.",
                IsResolved = true,
                PlayerUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "ALLY_076",
                        DisplayName = "First Union",
                        Side = "Player",
                        Members = new[] { member }
                    }
                },
                EnemyUnions = Array.Empty<M2BattleUnionView>(),
                Events = Array.Empty<M2BattleEventView>(),
                Reward = new M2BattleRewardView
                {
                    Outcome = "Victory",
                    CanClaim = true,
                    GuildTreasuryXpAward = 25,
                    HallEnhancementXpAward = 18,
                    GuildPreviousLevel = 1,
                    GuildProjectedLevel = 1,
                    EquipmentRewardDisplayName = "Roadwarden Staff",
                    EquipmentRewardQualityId = "Field",
                    EquipmentRewardValidSlotIds = new[] { "SLOT_MAIN_HAND" },
                    MemberRewards = new[]
                    {
                        new M2BattleMemberRewardView
                        {
                            MemberId = member.MemberId,
                            DisplayName = member.DisplayName,
                            PersonalXp = 40,
                            PreviousLevel = 1,
                            ProjectedLevel = 1
                        }
                    }
                },
                FinalStateHash = "READABILITY_RESULT_076"
            };
        }

        private static void AssertImpact(
            M2BattleDioramaView072 diorama,
            string heading,
            string detail,
            string unionId,
            string memberId,
            int before,
            int after,
            int maximum,
            int delta,
            bool enemy)
        {
            Assert.That(diorama.HpImpactVisible076, Is.True);
            Assert.That(diorama.HpImpactHeading076.text, Is.EqualTo(heading));
            Assert.That(diorama.HpImpactDetail076.text, Is.EqualTo(detail));
            Assert.That(diorama.HpImpactHeading076.resizeTextMinSize,
                Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumHpImpactFontSize076));
            Assert.That(diorama.HpImpactDetail076.resizeTextMinSize,
                Is.GreaterThanOrEqualTo(M2BattleDioramaView072.MinimumHpImpactFontSize076));
            var detailSettings = diorama.HpImpactDetail076.GetGenerationSettings(
                diorama.HpImpactDetail076.rectTransform.rect.size);
            Assert.That(diorama.HpImpactDetail076.cachedTextGenerator.Populate(
                diorama.HpImpactDetail076.text,
                detailSettings), Is.True);
            Assert.That(diorama.HpImpactDetail076.cachedTextGenerator.lines.Count, Is.EqualTo(2),
                "Member and Union totals must own separate, fully generated lines.");
            Assert.That(diorama.HpImpactRemainingSeconds076, Is.GreaterThan(0f));
            Assert.That(diorama.HpImpactPresentationCount076, Is.GreaterThan(0));
            Assert.That(diorama.LastHpImpactUnionId076, Is.EqualTo(unionId));
            Assert.That(diorama.LastHpImpactMemberId076, Is.EqualTo(memberId));
            Assert.That(diorama.LastHpImpactBefore076, Is.EqualTo(before));
            Assert.That(diorama.LastHpImpactAfter076, Is.EqualTo(after));
            Assert.That(diorama.LastHpImpactMaximum076, Is.EqualTo(maximum));
            Assert.That(diorama.LastHpImpactDelta076, Is.EqualTo(delta));
            Assert.That(diorama.LastHpImpactEnemy076, Is.EqualTo(enemy));
        }

        private static Bounds Bounds(RectTransform host, RectTransform item) =>
            RectTransformUtility.CalculateRelativeRectTransformBounds(host, item);

        private static float EffectiveCanvasScale(Vector2 size) =>
            Mathf.Sqrt((size.x / 2796f) * (size.y / 1290f));

        private static void AssertInside(RectTransform host, RectTransform item, string label)
        {
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(host, item);
            var rect = host.rect;
            Assert.That(bounds.min.x, Is.GreaterThanOrEqualTo(rect.xMin - 1f), label + " left edge");
            Assert.That(bounds.max.x, Is.LessThanOrEqualTo(rect.xMax + 1f), label + " right edge");
            Assert.That(bounds.min.y, Is.GreaterThanOrEqualTo(rect.yMin - 1f), label + " bottom edge");
            Assert.That(bounds.max.y, Is.LessThanOrEqualTo(rect.yMax + 1f), label + " top edge");
        }
    }
}
