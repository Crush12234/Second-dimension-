using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    /// <summary>
    /// Deterministic first-hour enemy-variety audit over twenty independent campaign
    /// seeds. Roles are read from the same Pass 03 authority used to build the
    /// resolver because M2EnemyDefinition intentionally carries combat values only.
    /// </summary>
    public sealed class EnemyEncounterVariety089AuditTests
    {
        private EncounterRosterResolver070 _resolver;
        private IReadOnlyDictionary<string, string> _roleByEnemyId;

        [SetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            _resolver = EncounterRosterResolver070.LoadFromContentRoot(contentRoot);
            _roleByEnemyId = ReadRoles(Path.Combine(
                contentRoot, "PASS_03", "ENEMY_DEFINITIONS.json"));
        }

        [Test]
        public void TwentySeedAuditExercisesDistinctIdsFamiliesRolesAndFormations089()
        {
            var sourceEnemyIds = new HashSet<string>(StringComparer.Ordinal);
            var familyIds = new HashSet<string>(StringComparer.Ordinal);
            var roles = new HashSet<string>(StringComparer.Ordinal);
            var formationIds = new HashSet<string>(StringComparer.Ordinal);
            var fullRosterSignatures = new List<string>();

            for (var offset = 0; offset < 20; offset++)
            {
                var seed = 89001L + offset;
                var roster = ResolveAuditRoster(seed);
                var reopened = ResolveAuditRoster(seed);

                Assert.That(
                    CanonicalJson.Sha256Hex(reopened),
                    Is.EqualTo(CanonicalJson.Sha256Hex(roster)),
                    "The same seed must reopen the exact committed encounter.");

                foreach (var union in roster.Unions)
                {
                    formationIds.Add(union.SourceDefinition.FormationId);
                    foreach (var member in union.Members)
                    {
                        sourceEnemyIds.Add(member.SourceEnemyId);
                        familyIds.Add(member.FamilyId);
                        Assert.That(_roleByEnemyId.TryGetValue(
                            member.SourceEnemyId, out var role), Is.True,
                            "Every resolved source enemy must map back to Pass 03 role authority.");
                        roles.Add(role);
                    }
                }

                fullRosterSignatures.Add(string.Join("|", roster.Unions.Select(
                    union => union.SourceUnionId + "@" +
                             union.SourceDefinition.FormationId)));
            }

            Assert.That(sourceEnemyIds.Count, Is.GreaterThanOrEqualTo(12),
                "Twenty seeds should expose substantially more than a repeated tutorial pack.");
            Assert.That(familyIds.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(roles.Count, Is.GreaterThanOrEqualTo(8));
            Assert.That(formationIds.Count, Is.GreaterThanOrEqualTo(4));

            for (var index = 1; index < fullRosterSignatures.Count; index++)
                Assert.That(fullRosterSignatures[index],
                    Is.Not.EqualTo(fullRosterSignatures[index - 1]),
                    "The audited generator sequence must not immediately repeat the exact " +
                    "ordered source-Union/formation roster. This does not claim that its " +
                    "stateless resolver forbids every individual Union repeat.");
        }

        [Test]
        public void FirstHourEncounterSampleLoadsEveryLiveRigSpriteAndEightDistinctPaths089()
        {
            // These are the four combat identities exposed by the actual first-hour
            // board. Five campaign seeds across the four encounters produce twenty
            // deterministic encounter commitments while retaining their real routing
            // context (including the fixed Gate-Eater climax composition).
            var encounterIds = new[]
            {
                GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071,
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                "ENCOUNTER_GATE_GNAWER_ELITE",
                GuildCityExpeditionService017D.FirstHourGateEaterEncounterId071
            };
            var enemyUnionCounts = new[] { 2, 2, 2, 3 };
            var resolvedResourcePaths = new HashSet<string>(StringComparer.Ordinal);
            var resolvedEnemyCount = 0;
            var stageObject = new GameObject(
                "First Hour Enemy Visual Audit 089", typeof(RectTransform));
            var stage = stageObject.GetComponent<RectTransform>();

            try
            {
                for (var seedOffset = 0; seedOffset < 5; seedOffset++)
                {
                    var campaignSeed = 89001L + seedOffset;
                    for (var encounterIndex = 0;
                         encounterIndex < encounterIds.Length;
                         encounterIndex++)
                    {
                        var roster = ResolveFirstHourVisualRoster089(
                            campaignSeed,
                            encounterIds[encounterIndex],
                            enemyUnionCounts[encounterIndex]);

                        foreach (var enemyUnion in roster.Unions)
                        {
                            var unionView = new M2BattleUnionView
                            {
                                UnionId = enemyUnion.UnionId,
                                DisplayName = enemyUnion.SourceDefinition.Name,
                                Side = "ENEMY"
                            };
                            foreach (var enemyMember in enemyUnion.Members)
                            {
                                M2BattleActorRig072 rig = null;
                                try
                                {
                                    // Match M2BattleCommandService's live projection:
                                    // the battle-local spawn ID is MemberId and the
                                    // stable Pass 03 source is PortraitAuthorityId.
                                    rig = new M2BattleActorRig072(
                                        stage,
                                        unionView,
                                        new M2BattleMemberView
                                        {
                                            MemberId = enemyMember.MemberId,
                                            PortraitAuthorityId = enemyMember.SourceEnemyId,
                                            DisplayName = enemyMember.Definition.Name,
                                            CurrentHp = enemyMember.Definition.MaximumHp,
                                            MaximumHp = enemyMember.Definition.MaximumHp,
                                            CurrentMp = enemyMember.Definition.MaximumMp,
                                            MaximumMp = enemyMember.Definition.MaximumMp
                                        },
                                        true);

                                    var path = rig.CurrentResourcePath;
                                    Assert.That(path, Is.Not.Null.And.Not.Empty,
                                        "The live actor-rig resolver returned no path for " +
                                        enemyMember.SourceEnemyId + ".");
                                    Assert.That(rig.CurrentArtwork076.sprite, Is.Not.Null,
                                        "The live actor rig rendered no sprite for " + path + ".");
                                    Assert.That(Resources.Load<Sprite>(path), Is.Not.Null,
                                        "The live path must be a packaged Resources Sprite: " + path);
                                    Assert.That(path, Does.Not.Contain(
                                            "RUNTIME_ENEMY_SILHOUETTE_070"),
                                        "A runtime placeholder must never satisfy the visual audit.");
                                    resolvedResourcePaths.Add(path);
                                    resolvedEnemyCount++;
                                }
                                finally
                                {
                                    DestroyRigImmediately089(rig);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (stageObject != null)
                    UnityEngine.Object.DestroyImmediate(stageObject);
            }

            Assert.That(resolvedEnemyCount, Is.GreaterThan(0));
            Assert.That(resolvedResourcePaths.Count, Is.GreaterThanOrEqualTo(8),
                "The sampled first-hour battles must visibly resolve to at least eight " +
                "different packaged enemy standees, not merely different source IDs.");
        }

        [Test]
        public void AllThirtyEnemyAuthoritiesRenderAcrossMoreThanTwoHundredLiveThreatVariants089()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            var root = JObject.Parse(File.ReadAllText(Path.Combine(
                contentRoot, "PASS_03", "ENEMY_DEFINITIONS.json")));
            var enemies = root["enemies"] as JArray ??
                          throw new InvalidDataException(
                              "Pass 03 enemies array is missing.");
            Assert.That(enemies.Count, Is.EqualTo(30),
                "The variant proof must enumerate the real combat authority.");

            var stageObject = new GameObject(
                "All Enemy Threat Variants 089", typeof(RectTransform));
            var stage = stageObject.GetComponent<RectTransform>();
            var rendered = new HashSet<string>(StringComparer.Ordinal);
            var exactPaths = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                foreach (var token in enemies)
                {
                    var item = token as JObject ??
                               throw new InvalidDataException(
                                   "Pass 03 enemy entry is invalid.");
                    var sourceId = item["id"]?.Value<string>();
                    var displayName = item["displayName"]?.Value<string>();
                    var maximumHp = item["maxHp"]?.Value<int>() ?? 0;
                    var maximumMp = item["maxMp"]?.Value<int>() ?? 0;
                    Assert.That(sourceId, Is.Not.Null.And.Not.Empty);
                    Assert.That(displayName, Is.Not.Null.And.Not.Empty);

                    for (var requestedTier = 1;
                         requestedTier <= M2EnemyThreatPalette089.MaximumTier;
                         requestedTier++)
                    {
                        M2BattleActorRig072 rig = null;
                        try
                        {
                            var memberId = sourceId + "_SPAWN070_VARIANT_" +
                                           requestedTier;
                            rig = new M2BattleActorRig072(
                                stage,
                                new M2BattleUnionView
                                {
                                    UnionId = "ENEMY_VARIANT_UNION_" + sourceId,
                                    DisplayName = displayName,
                                    Side = "ENEMY"
                                },
                                new M2BattleMemberView
                                {
                                    MemberId = memberId,
                                    PortraitAuthorityId = sourceId,
                                    DisplayName = displayName,
                                    EnemyThreatTier089 = requestedTier,
                                    CurrentHp = maximumHp,
                                    MaximumHp = maximumHp,
                                    CurrentMp = maximumMp,
                                    MaximumMp = maximumMp
                                },
                                true);

                            Assert.That(rig.CurrentArtwork076.sprite, Is.Not.Null,
                                sourceId + " must render a real Sprite.");
                            Assert.That(rig.CurrentResourcePath,
                                Is.Not.Null.And.Not.Empty);
                            Assert.That(Resources.Load<Sprite>(
                                rig.CurrentResourcePath), Is.Not.Null,
                                rig.CurrentResourcePath);
                            Assert.That(rig.CurrentResourcePath,
                                Does.Not.Contain("RUNTIME_ENEMY_SILHOUETTE_070"));

                            var expectedTier = sourceId.IndexOf(
                                                   "HINGE_EATER_COLOSSUS",
                                                   StringComparison.Ordinal) >= 0 ||
                                               sourceId.IndexOf(
                                                   "GATEHEART_WARDEN",
                                                   StringComparison.Ordinal) >= 0
                                ? M2EnemyThreatPalette089.MaximumTier
                                : requestedTier;
                            Assert.That(rig.EnemyThreatTier089,
                                Is.EqualTo(expectedTier), sourceId);
                            var expectedTint = M2EnemyThreatPalette089
                                .Visual(expectedTier).ArtworkTint;
                            Assert.That(rig.CurrentArtwork076.color.r,
                                Is.EqualTo(expectedTint.r).Within(0.001f));
                            Assert.That(rig.CurrentArtwork076.color.g,
                                Is.EqualTo(expectedTint.g).Within(0.001f));
                            Assert.That(rig.CurrentArtwork076.color.b,
                                Is.EqualTo(expectedTint.b).Within(0.001f));

                            // Count what the player can actually see: a packaged art
                            // path under a distinct threat-grade treatment. Source IDs
                            // alone would overstate variety when a family deliberately
                            // shares one silhouette across scout/veteran authorities.
                            rendered.Add(rig.CurrentResourcePath + "|" + expectedTier);
                            exactPaths[sourceId] = rig.CurrentResourcePath;
                        }
                        finally
                        {
                            DestroyRigImmediately089(rig);
                        }
                    }
                }
            }
            finally
            {
                if (stageObject != null)
                    UnityEngine.Object.DestroyImmediate(stageObject);
            }

            Assert.That(rendered.Count, Is.GreaterThan(200),
                "The packaged silhouettes across ten visible difficulty colours, " +
                "with the two apex bosses locked to Threat X, must produce more than " +
                "two hundred genuinely different art-path/colour combinations.");
            Assert.That(exactPaths["ENEMY_CHAINCALLER_01"],
                Does.EndWith("/CHAINCALLER_IDLE_089"));
            Assert.That(exactPaths["ENEMY_ECHO_STALKER_01"],
                Does.EndWith("/ECHO_STALKER_IDLE_089"));
            Assert.That(exactPaths["ENEMY_ECHO_STALKER_02"],
                Does.EndWith("/ECHO_STALKER_IDLE_089"));
        }

        private EncounterRoster070 ResolveAuditRoster(long campaignSeed) =>
            _resolver.Resolve(
                campaignSeed,
                "CONTRACT_FIRST_HOUR_VARIETY_AUDIT_089",
                "BOARD_SKYHOME_VARIETY_AUDIT_089",
                "ENCOUNTER_ROAMING_OPPOSITION_089",
                4,
                "FIRST_HOUR_VARIETY_AUDIT_089");

        private EncounterRoster070 ResolveFirstHourVisualRoster089(
            long campaignSeed,
            string encounterId,
            int enemyUnionCount) =>
            _resolver.Resolve(
                campaignSeed,
                GuildCityExpeditionService017D.FirstStoryContractId066,
                GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071,
                encounterId,
                enemyUnionCount,
                "FIRST_HOUR_VISUAL_AUDIT_089_" + encounterId);

        private static void DestroyRigImmediately089(M2BattleActorRig072 rig)
        {
            if (rig == null) return;

            // M2BattleActorRig072.Dispose deliberately uses delayed destruction for
            // play mode. EditMode must destroy its transient hierarchy and its unique
            // enemy fringe-cleanup material synchronously instead.
            var ownedMaterial = string.IsNullOrWhiteSpace(rig.EnemyArtworkShaderName075)
                ? null
                : rig.CurrentArtwork076.material;
            if (rig.Root != null)
            {
                if (ownedMaterial != null)
                {
                    foreach (var image in rig.Root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                        if (image != null && image.material == ownedMaterial)
                            image.material = null;
                }
                UnityEngine.Object.DestroyImmediate(rig.Root.gameObject);
            }
            if (ownedMaterial != null)
                UnityEngine.Object.DestroyImmediate(ownedMaterial);
        }

        private static IReadOnlyDictionary<string, string> ReadRoles(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path));
            var enemies = root["enemies"] as JArray ??
                          throw new InvalidDataException("Pass 03 enemies array is missing.");
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var token in enemies)
            {
                var item = token as JObject ??
                           throw new InvalidDataException("Pass 03 enemy entry is invalid.");
                var id = item["id"]?.Value<string>();
                var role = item["role"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(role))
                    throw new InvalidDataException("Pass 03 enemy id/role is missing.");
                result.Add(id, role);
            }
            return result;
        }
    }
}
