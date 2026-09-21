#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class TowerRun081PlayModeTests
    {
        [UnityTest]
        public IEnumerator EveryVisibleTowerFloorHasDistinctLoadableArt()
        {
            yield return null;
            var paths = new HashSet<string>();

            for (var floor = 1; floor <= TowerRunRules081.OpeningFloorCount; floor++)
            {
                var path = TowerRunRules081.ArtResourcePath(floor);
                Assert.That(paths.Add(path), Is.True, $"Floor {floor} reused another floor's art path.");
                Assert.That(Resources.Load<Texture2D>(path), Is.Not.Null, $"Floor {floor} art was missing at {path}.");
            }
        }

        [UnityTest]
        public IEnumerator FloorOneRuntimeAndBattleResolverUseCampaign083ArtWithoutPlaceholderCopy()
        {
            yield return null;
            const string legacyPath =
                "SecondDimension/Campaign022/UI/Abyss/Floor_01_Mud_Trenches";
            const string battleHash = "0123456789ABCDEF01234567";
            var mappedPath = TowerRunRules081.ArtResourcePath(1);
            var texture = Resources.Load<Texture2D>(mappedPath);

            Assert.That(mappedPath,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtResourcePath));
            Assert.That(mappedPath, Is.Not.EqualTo(legacyPath));
            Assert.That(texture, Is.Not.Null);
            Assert.That(texture.name,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtTextureName));
            Assert.That(texture.width, Is.EqualTo(1672));
            Assert.That(texture.height, Is.EqualTo(941));
            Assert.That(TowerRunRules081.ContainsForbiddenPlaceholderCopy083(texture.name), Is.False);
            Assert.That(Resources.Load<Texture2D>(legacyPath), Is.Not.Null,
                "The source schematic is retained as history but must not be selected.");

            var battleId = CampaignProgressionCommandService022.TowerBattleId081(1, battleHash);
            Assert.That(
                M1VisualAssets.TryResolveBattleBackdrop(
                    battleId,
                    out var battleSprite,
                    out var battleResourcePath),
                Is.True);
            Assert.That(battleResourcePath,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtResourcePath));
            Assert.That(battleSprite, Is.Not.Null);
            Assert.That(battleSprite.texture.name,
                Is.EqualTo(TowerRunRules081.FloorOneCampaign083BattleArtTextureName));
        }

        [UnityTest]
        public IEnumerator CriticalTowerAndForecastColorsRemainReadableAtRuntime()
        {
            yield return null;
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.FirstClimbHeadingColor083,
                    TowerRunRules081.TowerRibbonSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.LightSurfaceInkColor083,
                    TowerRunRules081.LightActionSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.LightSurfaceInkColor083,
                    TowerRunRules081.LightActionDisabledSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
            Assert.That(
                TowerRunRules081.ContrastRatio083(
                    TowerRunRules081.ForecastTextColor083,
                    TowerRunRules081.ForecastSurfaceColor083),
                Is.GreaterThanOrEqualTo(TowerRunRules081.MinimumReadableContrast083));
        }

        [UnityTest]
        public IEnumerator RuntimeRegistrySupportsGuardianFirstClearAndFloorTenTrialRepeat()
        {
            yield return null;
            var registry = CampaignRegistry022.LoadFromResources();

            for (var floor = 1; floor <= TowerRunRules081.OpeningFloorCount; floor++)
            {
                var guardianId = TowerRunRules081.OperationId(floor, true);
                Assert.That(registry.AbyssOperations.ContainsKey(guardianId), Is.True);
                Assert.That(registry.AbyssOperations[guardianId].firstClearOnly, Is.True);
                Assert.That(registry.AbyssOperations[guardianId].steps[3].requiresBattle, Is.True);
            }

            var repeatId = TowerRunRules081.OperationId(10, false);
            Assert.That(repeatId, Is.EqualTo("ABYSS_OP022_10_TRIAL"));
            Assert.That(registry.AbyssOperations.ContainsKey(repeatId), Is.True);
            Assert.That(registry.AbyssOperations[repeatId].firstClearOnly, Is.False);
            Assert.That(registry.AbyssOperations[repeatId].steps[3].requiresBattle, Is.True);
            Assert.That(CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, 1), Is.GreaterThan(
                CampaignProgressionCommandService022.TowerEnemyUnionCount081(10, 0)));
        }
    }
}
#endif
