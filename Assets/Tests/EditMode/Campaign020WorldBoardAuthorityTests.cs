#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;

namespace SecondDimension.Tests.EditMode
{
    public sealed class Campaign020WorldBoardAuthorityTests
    {
        CampaignPlayableCommandService020 _campaignService;
        Campaign020RuleCatalogAdapter _campaignCatalog;
        CampaignWorldGateCommandService023 _worldGateService;
        Campaign023RuleCatalogAdapter _worldGateCatalog;

        [SetUp]
        public void SetUp()
        {
            _campaignService = new CampaignPlayableCommandService020();
            _campaignCatalog = new Campaign020RuleCatalogAdapter(
                SecondDimension.Presentation.Campaign020.CampaignRegistry020.LoadFromResources());
            _worldGateService = new CampaignWorldGateCommandService023();
            _worldGateCatalog = new Campaign023RuleCatalogAdapter(
                SecondDimension.Presentation.Campaign023.CampaignRegistry023.LoadFromResources());
        }

        [Test]
        public void CallerSuppliedTrueCannotBypassChapterWorldBoardAuthority()
        {
            var atWorldBoard = CreateCampaignAtWorldBoard();
            var rejected = _worldGateService.BeginOperation(atWorldBoard, _worldGateCatalog,
                "CH018_001", new[] { "WORLD_BOARD_UNION_030" }, true);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors, "CAMPAIGN023_CHAPTER_WORLD_BOARD_STEP_REQUIRED");
        }

        [Test]
        public void ExactCanonicalActiveWorldBoardStepAuthorizesChapterBoard()
        {
            var atWorldBoard = CreateCampaignAtWorldBoard();
            var accepted = _worldGateService.BeginOperation(atWorldBoard, _worldGateCatalog,
                "CH018_001", new[] { "WORLD_BOARD_UNION_030" }, _campaignCatalog);
            Assert.That(accepted.IsSuccess, Is.True, string.Join("\n", accepted.Errors));
            Assert.That(accepted.Value.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023.ActiveOperation.DefinitionId, Is.EqualTo("CH018_001"));
        }

        [Test]
        public void CanonicalChapterBeforeWorldBoardStepIsRejected()
        {
            var beforeWorldBoard = CreateStartedCampaign();
            var rejected = _worldGateService.BeginOperation(beforeWorldBoard, _worldGateCatalog,
                "CH018_001", new[] { "WORLD_BOARD_UNION_030" }, _campaignCatalog);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors, "CAMPAIGN023_CHAPTER_WORLD_BOARD_STEP_REQUIRED");
        }

        [Test]
        public void WorldBoardCannotBeSkippedByGenericNonBattleResolution()
        {
            var atWorldBoard = CreateCampaignAtWorldBoard();
            var rejected = _campaignService.CommitNonBattleStep(
                atWorldBoard, _campaignCatalog, "SUCCESS");
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors,
                "CAMPAIGN020_WORLD_BOARD_EXPEDITION_REQUIRED");
        }

        [Test]
        public void TileAdventurePresenterOwnsTheLiveCampaign020Route()
        {
            var methods = typeof(SecondDimension.Presentation.M1FlowPresenter)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(methods.Count(value =>
                value.Name == "BuildGuildCityCampaign020"), Is.EqualTo(1));
            Assert.That(methods.Count(value =>
                value.Name == "BuildGuildCityCampaignLegacy020"), Is.EqualTo(1));
        }

        [Test]
        public void ForgedCampaignOperationIdentityIsRejectedAtWorldBoardStep()
        {
            var campaign = CreateCampaignAtWorldBoard();
            var playable = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;
            var operation = playable.ActiveOperation;
            var forged = new CampaignPlayableOperationState020(
                "OP020_000000000000000000000000", operation.BlueprintId, operation.ChapterId,
                operation.WorldId, "FORGED_CANONICAL_SEED_IDENTITY", operation.CurrentStepIndex,
                operation.Status, operation.CompletedStepIds, operation.AppliedReceiptIds,
                operation.PendingReceipt, operation.ExistingBattleRewardReceiptId, "forged_world_board_030");
            campaign = WithPlayable(campaign, playable.With(activeOperation: forged,
                replaceActiveOperation: true, lastCheckpointId: "forged_world_board_030"));

            var rejected = _worldGateService.BeginOperation(campaign, _worldGateCatalog,
                "CH018_001", new[] { "WORLD_BOARD_UNION_030" }, _campaignCatalog);
            Assert.That(rejected.IsSuccess, Is.False);
            CollectionAssert.Contains(rejected.Errors, "CAMPAIGN023_CHAPTER_WORLD_BOARD_STEP_REQUIRED");
        }

        CampaignState CreateCampaignAtWorldBoard()
        {
            var campaign = CreateStartedCampaign();
            campaign = Require(_campaignService.CommitNonBattleStep(campaign, _campaignCatalog, "SUCCESS"));
            campaign = Require(_campaignService.ApplyStepReceiptExactlyOnce(campaign, _campaignCatalog));
            var operation = campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.ActiveOperation;
            Assert.That(_campaignCatalog.TryGetBlueprint("CH018_001", out var blueprint), Is.True);
            Assert.That(blueprint.Steps[operation.CurrentStepIndex].Kind, Is.EqualTo("WORLD_BOARD"));
            return campaign;
        }

        CampaignState CreateStartedCampaign()
        {
            var recruit = new RecruitState("WORLD_BOARD_RECRUIT_030", 100, 100, 20, 20);
            var union = new UnionState("WORLD_BOARD_UNION_030", "World Board Union", UnionKind.Normal,
                recruit.RecruitId, new[] { recruit.RecruitId }, "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(30020);
            var guild = new GuildState(source.Guild.GuildId, source.Guild.TreasuryXp,
                new[] { recruit }, new[] { union }, source.Guild.Inventory, source.Guild.Development,
                guildCity: null);
            var campaign = source.With(guild, source.OpeningFlow);
            var progress = campaign.Guild.GuildCity.Strategic017H.Campaign019.With(
                activeChapterId: "CH018_001", lastCheckpointId: "chapter_001_active_030");
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId);
            campaign = campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
            return Require(_campaignService.BeginOperation(campaign, _campaignCatalog, "CH018_001"));
        }

        static CampaignState WithPlayable(CampaignState campaign, CampaignPlayableState020 playable)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: playable.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: playable.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: playable.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
