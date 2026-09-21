using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2StableEnemyIdentity086PlayModeTests
    {
        [TestCase("ENEMY_GATE_GNAWER_01", "ENEMY_GATE_GNAWER_02")]
        [TestCase("ENEMY_GATE_GNAWER_02", "ENEMY_GATE_GNAWER_01")]
        [TestCase("ENEMY_GATE_GNAWER_03", "ENEMY_GATE_GNAWER_03")]
        [TestCase("enemy_gate_gnawer_01_spawn070_alpha", "ENEMY_GATE_GNAWER_02")]
        [TestCase("ENEMY_GATE_GNAWER_03_PACK_SECOND_WAVE", "ENEMY_GATE_GNAWER_03")]
        public void GateGnawerSourceIdentitySelectsStableAuthoredSilhouette086(
            string sourceIdentity,
            string expectedVisualIdentity)
        {
            Assert.That(ResolveStableIdentity(sourceIdentity), Is.EqualTo(expectedVisualIdentity));
        }

        [TestCase("")]
        [TestCase("ENEMY_ECHO_STALKER_01")]
        [TestCase("ENEMY_HINGE_EATER_01")]
        public void UnsupportedOrSpecialEnemyIdentityKeepsExistingRoutingAndHashFallback086(string sourceIdentity)
        {
            Assert.That(ResolveStableIdentity(sourceIdentity), Is.Empty);
        }

        [TestCase("ENEMY_RUSTBACK_HOUND_01", "RUSTBACK_HOUND_IDLE_086")]
        [TestCase("ENEMY_RUSTBACK_HOUND_03_SPAWN070_ALPHA", "RUSTBACK_HOUND_LEADER_IDLE_089")]
        [TestCase("ENEMY_HOLLOW_SALVAGER_02", "HOLLOW_SALVAGER_IDLE_086")]
        [TestCase("ENEMY_SHARDWING_SWARM_03_PACK_SECOND_WAVE", "SHARDWING_SIGNAL_QUEEN_IDLE_089")]
        [TestCase("ENEMY_RIFT_MOLD_CREEPER_01", "RIFT_MOLD_CREEPER_IDLE_086")]
        [TestCase("ENEMY_GATEIRON_BRUTE_02", "GATEIRON_BRUTE_IDLE_086")]
        [TestCase("ENEMY_ASH_MEDIC_01", "ASH_MEDIC_IDLE_086")]
        [TestCase("ENEMY_CAPTAIN_RAVEL_01", "CAPTAIN_RAVEL_IDLE_086")]
        [TestCase("ENEMY_GATEHEART_WARDEN_01", "GATEHEART_WARDEN_IDLE_086")]
        [TestCase("ENEMY_TOLLROAD_CUTTER_01", "TOLLROAD_CUTTER_IDLE_087")]
        [TestCase("ENEMY_BRASSJAW_PACKLORD_03_PACK_SECOND_WAVE", "BRASSJAW_PACKLORD_IDLE_087")]
        [TestCase("ENEMY_PULSE_SCRIBE_02_SPAWN070_ALPHA", "PULSE_SCRIBE_IDLE_087")]
        public void OriginalOuterGateEnemyFamiliesResolveDistinctPackagedStandees086(
            string sourceIdentity,
            string assetName)
        {
            var path = ResolveOriginalEnemyPath(sourceIdentity);
            Assert.That(path, Is.EqualTo(
                "SecondDimension/Art/Battle086/Enemies/" + assetName));
            Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                "The original enemy standee must import as a packaged Sprite.");
        }

        [UnityTest]
        public IEnumerator InstantiatedRigUsesAuthorityAndSpawnIdentityForPackagedArtwork086()
        {
            var hostObject = new GameObject(
                "Battle086 Live Enemy Rig Host", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            var union = new M2BattleUnionView { UnionId = "EU_LIVE_IDENTITY_086" };
            M2BattleActorRig072 authorityRig = null;
            M2BattleActorRig072 spawnRig = null;
            try
            {
                authorityRig = new M2BattleActorRig072(
                    host,
                    union,
                    new M2BattleMemberView
                    {
                        MemberId = "ENEMY_RUNTIME_PROXY_SPAWN070_AUTHORITY_TEST",
                        PortraitAuthorityId = "ENEMY_RUSTBACK_HOUND_02",
                        DisplayName = "Rustback Hound",
                        CurrentHp = 120,
                        MaximumHp = 120
                    },
                    true);
                spawnRig = new M2BattleActorRig072(
                    host,
                    union,
                    new M2BattleMemberView
                    {
                        MemberId = "ENEMY_HOLLOW_SALVAGER_03_SPAWN070_LIVE_TEST",
                        DisplayName = "Hollow Salvager",
                        CurrentHp = 120,
                        MaximumHp = 120
                    },
                    true);

                Assert.That(authorityRig.CurrentResourcePath, Is.EqualTo(
                    "SecondDimension/Art/Battle086/Enemies/RUSTBACK_HOUND_IDLE_086"));
                Assert.That(authorityRig.CurrentArtwork076.sprite, Is.Not.Null,
                    "A live rig must consume the authoritative PortraitAuthorityId standee.");
                Assert.That(spawnRig.CurrentResourcePath, Is.EqualTo(
                    "SecondDimension/Art/Battle086/Enemies/HOLLOW_SALVAGER_LEADER_IDLE_089"));
                Assert.That(spawnRig.CurrentArtwork076.sprite, Is.Not.Null,
                    "A live rig must strip its presentation-only spawn suffix and load the upgraded family leader standee.");
            }
            finally
            {
                authorityRig?.Dispose();
                spawnRig?.Dispose();
                UnityEngine.Object.Destroy(hostObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator DioramaStagesWardenAndRavelWithPresentationOnlyEmphasis086()
        {
            var hostObject = new GameObject(
                "Battle086 Enemy Emphasis Host", typeof(RectTransform));
            var host = hostObject.GetComponent<RectTransform>();
            host.sizeDelta = new Vector2(1280f, 800f);
            var owner = new GameObject("Battle086 Enemy Emphasis Diorama");
            var diorama = owner.AddComponent<M2BattleDioramaView072>();
            diorama.Initialize(host);
            var battle = new M2BattleView
            {
                BattleId = "BATTLE_PRESENTATION_ONLY_ENEMY_EMPHASIS_086",
                Objective = "Prove enemy presentation tiers without changing battle authority.",
                PlayerUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "PU_PRESENTATION_086",
                        DisplayName = "Guild Union",
                        CanAct = true,
                        Members = new[]
                        {
                            new M2BattleMemberView
                            {
                                MemberId = "SIGI_MAREN_PRESENTATION_086",
                                PortraitAuthorityId = "SIGREC_MAREN_HOLT",
                                DisplayName = "Maren Holt",
                                CurrentHp = 240,
                                MaximumHp = 240
                            }
                        }
                    }
                },
                EnemyUnions = new[]
                {
                    new M2BattleUnionView
                    {
                        UnionId = "EU_LEADERS_086",
                        DisplayName = "Outer Gate Command",
                        CanAct = true,
                        Members = new[]
                        {
                            Enemy("ENEMY_GATEHEART_WARDEN_01_SPAWN070_STAGE", "Gateheart Warden"),
                            Enemy("ENEMY_CAPTAIN_RAVEL_01_SPAWN070_STAGE", "Captain Ravel"),
                            Enemy("ENEMY_RUSTBACK_HOUND_01_SPAWN070_STAGE", "Rustback Hound")
                        }
                    }
                },
                Forecasts = Array.Empty<M2ForecastView>()
            };

            diorama.Refresh(battle, "PU_PRESENTATION_086");
            yield return null;
            Canvas.ForceUpdateCanvases();

            var warden = diorama.ResolveActor(
                "ENEMY_GATEHEART_WARDEN_01_SPAWN070_STAGE", "EU_LEADERS_086");
            var ravel = diorama.ResolveActor(
                "ENEMY_CAPTAIN_RAVEL_01_SPAWN070_STAGE", "EU_LEADERS_086");
            var standard = diorama.ResolveActor(
                "ENEMY_RUSTBACK_HOUND_01_SPAWN070_STAGE", "EU_LEADERS_086");
            Assert.That(warden, Is.Not.Null);
            Assert.That(ravel, Is.Not.Null);
            Assert.That(standard, Is.Not.Null);

            Assert.That(warden.BossEnemy075, Is.False,
                "Gateheart presentation must not opt into certified Gate-Eater boss behavior.");
            Assert.That(ravel.BossEnemy075, Is.False,
                "Captain Ravel presentation must not opt into certified Gate-Eater boss behavior.");
            Assert.That(warden.OriginalEnemyPresentationTier086,
                Is.EqualTo(M2BattleActorRig072.MajorEnemyPresentationTier086));
            Assert.That(ravel.OriginalEnemyPresentationTier086,
                Is.EqualTo(M2BattleActorRig072.MinibossEnemyPresentationTier086));
            Assert.That(standard.OriginalEnemyPresentationTier086,
                Is.EqualTo(M2BattleActorRig072.StandardEnemyPresentationTier086));
            Assert.That(warden.MajorEnemyPresentation086, Is.True);
            Assert.That(ravel.ProminentEnemyPresentation086, Is.True);
            Assert.That(ravel.MajorEnemyPresentation086, Is.False);
            Assert.That(standard.ProminentEnemyPresentation086, Is.False);
            Assert.That(warden.HomeScale.x, Is.GreaterThan(ravel.HomeScale.x));
            Assert.That(ravel.HomeScale.x, Is.GreaterThan(standard.HomeScale.x));
            Assert.That(warden.Root.sizeDelta.x, Is.GreaterThan(ravel.Root.sizeDelta.x));
            Assert.That(ravel.Root.sizeDelta.x, Is.GreaterThan(standard.Root.sizeDelta.x));
            Assert.That(warden.CurrentArtwork076.sprite, Is.Not.Null);
            Assert.That(ravel.CurrentArtwork076.sprite, Is.Not.Null);

            UnityEngine.Object.Destroy(owner);
            UnityEngine.Object.Destroy(hostObject);
            yield return null;
        }

        private static string ResolveStableIdentity(string sourceIdentity)
        {
            var method = typeof(M2BattleActorRig072).GetMethod(
                "StableGateGnawerIdentity086",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null, "Stable Gate Gnawer identity resolver must remain available.");
            return (string)method.Invoke(null, new object[] { sourceIdentity });
        }

        private static string ResolveOriginalEnemyPath(string sourceIdentity)
        {
            var method = typeof(M2BattleActorRig072).GetMethod(
                "TryResolveOriginalEnemyResourcePath086",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.That(method, Is.Not.Null,
                "Original enemy art resolver must remain available.");
            var arguments = new object[] { sourceIdentity, null };
            var resolved = (bool)method.Invoke(null, arguments);
            Assert.That(resolved, Is.True,
                "Expected an original standee for " + sourceIdentity + ".");
            return arguments[1] as string;
        }

        private static M2BattleMemberView Enemy(string memberId, string displayName) =>
            new M2BattleMemberView
            {
                MemberId = memberId,
                DisplayName = displayName,
                CurrentHp = 300,
                MaximumHp = 300
            };
    }
}
