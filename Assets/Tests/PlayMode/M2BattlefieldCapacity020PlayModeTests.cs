using System.Collections;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class M2BattlefieldCapacity020PlayModeTests
    {
        [UnityTearDown]
        public IEnumerator CleanupBattleWorlds()
        {
            foreach (var world in Object.FindObjectsByType<M2Battle3DWorld>(FindObjectsSortMode.None))
                if (world != null) Object.Destroy(world.gameObject);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwentyOccupiedUnionSlotsRemainVisibleAtWideAndFourThreeAspects()
        {
            var battle = BuildTwentyUnionView();
            var originalPlayerIds = battle.PlayerUnions.Select(value => value.UnionId).ToArray();
            var originalEnemyIds = battle.EnemyUnions.Select(value => value.UnionId).ToArray();
            var host = new GameObject("Twenty Union Capacity Test Host 020");
            var world = host.AddComponent<M2Battle3DWorld>();

            Assert.That(world.ShowBattle(battle, false, false), Is.True);
            yield return null;

            var camera = GameObject.Find("Battle 3D Perspective Camera")?.GetComponent<Camera>();
            Assert.That(camera, Is.Not.Null);
            AssertNamedCount("Player Tactical Union Slot 020", 10);
            AssertNamedCount("Enemy Tactical Union Slot 020", 10);
            AssertNamedCount("Player Tactical Union Standard 020", 10);
            AssertNamedCount("Enemy Tactical Union Standard 020", 10);
            AssertReadableTacticalWorldLabels();

            camera.aspect = 16f / 9f;
            Assert.That(world.ShowBattle(battle, false, false), Is.True);
            yield return null;
            AssertAllCapacityMarkersInsideViewport(camera);

            camera.aspect = 4f / 3f;
            Assert.That(world.ShowBattle(battle, false, false), Is.True);
            yield return null;
            AssertAllCapacityMarkersInsideViewport(camera);

            world.HighlightUnion("PLAYER_UNION_10");
            Assert.That(world.IsReady, Is.True);
            Assert.That(battle.PlayerUnions.Select(value => value.UnionId), Is.EqualTo(originalPlayerIds));
            Assert.That(battle.EnemyUnions.Select(value => value.UnionId), Is.EqualTo(originalEnemyIds));
            Assert.That(battle.StateHash, Is.EqualTo("CAPACITY_AUTHORITY_UNCHANGED"));

            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ResolutionCloseupsHideTacticalWorldLabelsThatWouldCoverTheAction()
        {
            var host = new GameObject("Resolution Label Visibility Test Host 021");
            var world = host.AddComponent<M2Battle3DWorld>();

            Assert.That(world.ShowBattle(BuildTwentyUnionView(), true, false), Is.True);
            yield return null;

            var labels = Resources.FindObjectsOfTypeAll<TextMesh>()
                .Where(value => value != null && value.gameObject.scene.IsValid() &&
                                value.name.StartsWith("Tactical Union Label 020", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(labels.Length, Is.EqualTo(20));
            Assert.That(labels.All(value => !value.gameObject.activeInHierarchy), Is.True,
                "Turn tiles and close cameras carry resolution context; tactical standards must not cover combat.");

            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReducedMotionDamageUsesReadableHpLabelAndTheMatchingFontAtlas()
        {
            var host = new GameObject("Readable Damage Test Host 021");
            var world = host.AddComponent<M2Battle3DWorld>();
            Assert.That(world.ShowBattle(BuildTwentyUnionView(), true, true), Is.True);
            yield return null;

            var beat = new BattlePresentationBeat(
                1, 1, "MARTIAL_HIT", BattleBeatFamily.BasicMartial, BattleCameraShot.Impact,
                "PLAYER_UNION_01", "PLAYER_UNION_01_LEADER",
                "ENEMY_UNION_01", "ENEMY_UNION_01_LEADER",
                "ART_BASIC_SABER_CUT", 49, "Guild Union 1 Leader uses Saber Cut for 49 HP.",
                "VFX_WEAPON_IMPACT", "SFX_WEAPON_IMPACT", true);
            var directive = M2Battle3DPresentationPolicy.CreateDirective(beat);
            world.StartCoroutine(world.PlayDirective(directive, () => 1f, true, () => false, 0f, false));

            var amount = default(TextMesh);
            for (var frame = 0; frame < 30 && amount == null; frame++)
            {
                amount = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                    .FirstOrDefault(value => value.text == "-49 HP");
                yield return null;
            }

            Assert.That(amount, Is.Not.Null, "The authoritative 49 damage must appear as '-49 HP'.");
            AssertMatchingFontAtlas(amount);
            Object.Destroy(host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReducedMotionLearningShowsNamedArtXpInsteadOfFalseHpDamage()
        {
            var host = new GameObject("Readable Art XP Test Host 021");
            var world = host.AddComponent<M2Battle3DWorld>();
            Assert.That(world.ShowBattle(BuildTwentyUnionView(), true, true), Is.True);
            yield return null;

            var beat = new BattlePresentationBeat(
                2, 1, "ART_GROWTH", BattleBeatFamily.Learning, BattleCameraShot.ActingUnion,
                "PLAYER_UNION_01", "PLAYER_UNION_01_LEADER",
                "PLAYER_UNION_01", "PLAYER_UNION_01_LEADER",
                "ART_BASIC_SABER_CUT", 5,
                "Guild Union 1 Leader grows Saber Cut through meaningful use: +5 mastery.",
                "VFX_ART_GROWTH", "SFX_LEARNING", true);
            var directive = M2Battle3DPresentationPolicy.CreateDirective(beat);
            world.StartCoroutine(world.PlayDirective(directive, () => 1f, true, () => false, 0f, false));

            var growth = default(TextMesh);
            for (var frame = 0; frame < 30 && growth == null; frame++)
            {
                growth = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                    .FirstOrDefault(value => value.text == "SABER CUT\nART XP +5");
                yield return null;
            }

            Assert.That(growth, Is.Not.Null, "Meaningful use must show the real Art and its Art XP gain.");
            Assert.That(Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None).Any(value => value.text == "-5 HP"), Is.False,
                "Art XP must never be presented as hit-point loss in reduced-motion playback.");
            AssertMatchingFontAtlas(growth);
            Object.Destroy(host);
            yield return null;
        }

        private static M2BattleView BuildTwentyUnionView() => new M2BattleView
        {
            BattleId = "PRESENTATION_CAPACITY_020",
            StateHash = "CAPACITY_AUTHORITY_UNCHANGED",
            PlayerUnions = Enumerable.Range(1, 10)
                .Select(index => Union("PLAYER_UNION_" + index.ToString("00"), "Guild Union " + index, false))
                .ToArray(),
            EnemyUnions = Enumerable.Range(1, 10)
                .Select(index => Union("ENEMY_UNION_" + index.ToString("00"), "Enemy Union " + index, true))
                .ToArray()
        };

        private static M2BattleUnionView Union(string id, string name, bool enemy)
        {
            var memberId = id + "_LEADER";
            return new M2BattleUnionView
            {
                UnionId = id,
                DisplayName = name,
                Side = enemy ? "Enemy" : "Player",
                LeaderMemberId = memberId,
                Formation = "Capacity Formation",
                Engagement = "Open",
                CanAct = true,
                Members = new[]
                {
                    new M2BattleMemberView
                    {
                        MemberId = memberId,
                        DisplayName = name + " Leader",
                        VisualSeed = "PROC_36344E2400DC98B6",
                        RaceId = enemy ? "BEAST" : "HUMAN",
                        CurrentHp = 100,
                        MaximumHp = 100
                    }
                }
            };
        }

        private static void AssertNamedCount(string prefix, int expected)
        {
            var count = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Count(value => value.name.StartsWith(prefix, System.StringComparison.Ordinal));
            Assert.That(count, Is.EqualTo(expected), prefix);
        }

        private static void AssertReadableTacticalWorldLabels()
        {
            var labels = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Tactical Union Label 020", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(labels.Length, Is.EqualTo(20));
            var first = labels.Single(value => value.name.EndsWith("PLAYER_UNION_01", System.StringComparison.Ordinal));
            Assert.That(first.text, Is.EqualTo("UNION 01\nGUILD UNION 1"));
            foreach (var label in labels)
            {
                Assert.That(label.text, Does.Not.Contain("_"), label.name);
                Assert.That(label.text.All(character => character != '\uFFFD'), Is.True, label.name);
                AssertMatchingFontAtlas(label);
            }
        }

        private static void AssertMatchingFontAtlas(TextMesh text)
        {
            Assert.That(text, Is.Not.Null);
            Assert.That(text.font, Is.Not.Null, text.name);
            var renderer = text.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null, text.name);
            Assert.That(renderer.sharedMaterial, Is.SameAs(text.font.material),
                text.name + " is using glyph coordinates from one font with another font's texture.");
            Assert.That(renderer.sharedMaterial.mainTexture, Is.SameAs(text.font.material.mainTexture), text.name);
            foreach (var character in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-'")
                Assert.That(text.font.HasCharacter(character), Is.True,
                    text.font.name + " is missing required player-facing character " + character + ".");
        }

        private static void AssertAllCapacityMarkersInsideViewport(Camera camera)
        {
            var markers = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                .Where(value => value.name.StartsWith("Player Tactical Union Slot 020", System.StringComparison.Ordinal) ||
                                value.name.StartsWith("Enemy Tactical Union Slot 020", System.StringComparison.Ordinal))
                .ToArray();
            Assert.That(markers.Length, Is.EqualTo(20));
            foreach (var marker in markers)
            {
                var viewport = camera.WorldToViewportPoint(marker.position + Vector3.up * 0.2f);
                Assert.That(viewport.z, Is.GreaterThan(0f), marker.name + " is behind the camera.");
                Assert.That(viewport.x, Is.InRange(0.01f, 0.99f), marker.name + " is outside the horizontal frame.");
                Assert.That(viewport.y, Is.InRange(0.01f, 0.99f), marker.name + " is outside the vertical frame.");
            }
        }
    }
}
