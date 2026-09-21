using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.PlayMode
{
    /// <summary>
    /// Isolated presentation routing proof for the five overnight elite standees.
    /// No encounter or battle authority is changed by these mappings.
    /// </summary>
    public sealed class M2EnemyVariant089PlayModeTests
    {
        private const string ResourceRoot089 =
            "SecondDimension/Art/Battle086/Enemies/";

        [TestCase("ENEMY_RUSTBACK_HOUND_03", "RUSTBACK_HOUND_LEADER_IDLE_089")]
        [TestCase("enemy_rustback_hound_03_spawn070_live", "RUSTBACK_HOUND_LEADER_IDLE_089")]
        [TestCase("ENEMY_HOLLOW_SALVAGER_03", "HOLLOW_SALVAGER_LEADER_IDLE_089")]
        [TestCase("ENEMY_HOLLOW_SALVAGER_03_SPAWN070_LIVE", "HOLLOW_SALVAGER_LEADER_IDLE_089")]
        [TestCase("ENEMY_SHARDWING_SWARM_03", "SHARDWING_SIGNAL_QUEEN_IDLE_089")]
        [TestCase("ENEMY_SHARDWING_SWARM_03_SPAWN070_LIVE", "SHARDWING_SIGNAL_QUEEN_IDLE_089")]
        [TestCase("ENEMY_TOLLROAD_CUTTER_03", "TOLLROAD_CUTTER_LEADER_IDLE_089")]
        [TestCase("ENEMY_TOLLROAD_CUTTER_03_SPAWN070_LIVE", "TOLLROAD_CUTTER_LEADER_IDLE_089")]
        [TestCase("ENEMY_RIFT_MOLD_CREEPER_03", "RIFT_MOLD_CROWN_IDLE_089")]
        [TestCase("ENEMY_RIFT_MOLD_CREEPER_03_SPAWN070_LIVE", "RIFT_MOLD_CROWN_IDLE_089")]
        public void StableAndSpawnIdentityResolveTheSameEliteStandee089(
            string sourceIdentity,
            string assetName)
        {
            var path = ResolveOriginalEnemyPath089(sourceIdentity);
            Assert.That(path, Is.EqualTo(ResourceRoot089 + assetName));
            Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                "The exact elite route must resolve to a packaged Sprite: " + path);
        }

        [TestCase("ENEMY_RUSTBACK_HOUND_01", "RUSTBACK_HOUND_IDLE_086")]
        [TestCase("ENEMY_RUSTBACK_HOUND_02_SPAWN070_STANDARD", "RUSTBACK_HOUND_IDLE_086")]
        [TestCase("ENEMY_HOLLOW_SALVAGER_01", "HOLLOW_SALVAGER_IDLE_086")]
        [TestCase("ENEMY_SHARDWING_SWARM_02", "SHARDWING_SWARM_IDLE_086")]
        [TestCase("ENEMY_TOLLROAD_CUTTER_01", "TOLLROAD_CUTTER_IDLE_087")]
        [TestCase("ENEMY_RIFT_MOLD_CREEPER_02_PACK_SECOND_WAVE", "RIFT_MOLD_CREEPER_IDLE_086")]
        public void NonEliteFamilyVariantsKeepTheirExistingFallback089(
            string sourceIdentity,
            string assetName)
        {
            var path = ResolveOriginalEnemyPath089(sourceIdentity);
            Assert.That(path, Is.EqualTo(ResourceRoot089 + assetName));
            Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                "The family fallback must remain packaged: " + path);
        }

        private static string ResolveOriginalEnemyPath089(string sourceIdentity)
        {
            var method = typeof(M2BattleActorRig072).GetMethod(
                "TryResolveOriginalEnemyResourcePath086",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null,
                "Original enemy art resolver must remain available.");

            var arguments = new object[] { sourceIdentity, null };
            var resolved = (bool)method.Invoke(null, arguments);
            Assert.That(resolved, Is.True,
                "Expected an authored enemy standee for " + sourceIdentity + ".");
            return arguments[1] as string;
        }
    }
}
