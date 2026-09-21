#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ExpeditionDeck089PresentationContractTests
    {
        [Test]
        public void FinalTutorialAcknowledgementPersistsThroughCampaignRoundTrip()
        {
            var campaign = CampaignFactory.CreateM0Proof(89091);
            var before = WorldGate(campaign);
            Assert.That(before.ExpeditionDeckTutorialSeen089, Is.False);

            var acknowledged = new ExpeditionDeckCommandService089()
                .AcknowledgeTutorial(campaign);
            Assert.That(acknowledged.IsSuccess, Is.True,
                string.Join("\n", acknowledged.Errors));
            Assert.That(WorldGate(acknowledged.Value)
                .ExpeditionDeckTutorialSeen089, Is.True);
            Assert.That(WorldGate(acknowledged.Value).LastCheckpointId,
                Is.EqualTo("expedition_089_tutorial_seen"));

            var reloaded = JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(acknowledged.Value));
            Assert.That(WorldGate(reloaded).ExpeditionDeckTutorialSeen089,
                Is.True,
                "The first-time tutorial acknowledgement must belong to the save, not the presenter session.");

            var repeated = new ExpeditionDeckCommandService089()
                .AcknowledgeTutorial(reloaded);
            Assert.That(repeated.IsSuccess, Is.True);
            Assert.That(WorldGate(repeated.Value).ExpeditionDeckTutorialSeen089,
                Is.True);
        }

        [TestCase(3, "AMBUSH")]
        [TestCase(4, "ELITE")]
        [TestCase(8, "BOSS")]
        public void BattleTiersKeepDistinctCategoryAndSharedBattleFaceContract(
            int enemyUnionCount, string expectedCategory)
        {
            var board = BattleBoard(enemyUnionCount);
            var deck = new ExpeditionDeckService089().Create(
                89092 + enemyUnionCount, "OP_TIER_089_" + enemyUnionCount,
                board, Array.Empty<RecruitState>(),
                Array.Empty<EquipmentItemState>(), null, Array.Empty<string>());

            Assert.That(deck.CurrentRow, Has.Count.EqualTo(3));
            Assert.That(deck.CurrentRow.All(value =>
                value.Category == expectedCategory), Is.True);
            Assert.That(deck.CurrentRow.All(value =>
                value.VisualCategoryKey == "BATTLE"), Is.True,
                "Tier labels may change, but all certified encounter tiers must resolve the CARD_FACE_BATTLE_089 art contract.");
            Assert.That(deck.CurrentRow.All(value => value.Description.Contains(
                enemyUnionCount + " hostile Union")), Is.True);
            Assert.That(deck.CurrentRow.All(value => value.RewardPreview.Contains(
                "certified battle rewards")), Is.True);
        }

        [Test]
        public void AscensionCopyKeepsRecruitGameplayAuthorityButProjectsDistinctSafeVisual()
        {
            var card = new ExpeditionRouteCardState089(
                "CARD_ASCENSION_PRESENTATION_089",
                "NODE_ASCENSION_PRESENTATION_089",
                "CONTINUE",
                "RECRUIT",
                "RECRUIT",
                "Ascension Copy: Elara Steelsong",
                "Merge a matching hero copy.",
                "FAVORED",
                "SUCCESS • full listed reward",
                "Ascension 1/" + RecruitAscensionRules089.MaximumLevel,
                7,
                0,
                6,
                6,
                Array.Empty<string>(),
                0,
                "HERO_REC_011",
                "Elara Steelsong",
                "S",
                "TEST",
                ExpeditionDeckService089.AscensionOfferKind089);

            Assert.That(card.Category, Is.EqualTo("RECRUIT"),
                "Receipt and reward authority must continue to use RECRUIT.");
            Assert.That(M1RuntimeCoordinator.ExpeditionPresentationCategory089(card),
                Is.EqualTo("ASCENSION"));
            Assert.That(M1RuntimeCoordinator.ExpeditionPresentationFaceKey089(card),
                Is.EqualTo("RECRUIT"),
                "The distinct frame must reuse the installed nonblank Hero card face.");
        }

        static WorldGateBoardRule023 BattleBoard(int enemyUnionCount) =>
            new WorldGateBoardRule023
            {
                BoardId = "BOARD_TIER_089_" + enemyUnionCount,
                DefinitionId = "DEF_TIER_089_" + enemyUnionCount,
                OperationKind = "CHAPTER",
                WorldId = "SKYHOME",
                StartNodeId = "BATTLE_0",
                ExitNodeId = "EXIT_1",
                UsesCertifiedBattle = true,
                MaximumAlliedUnions = 10,
                MaximumEnemyUnions = 10,
                Nodes = new[]
                {
                    new WorldGateNodeRule023
                    {
                        NodeId = "BATTLE_0",
                        Kind = "BATTLE",
                        Title = "The Hostile Crossing",
                        Description = "A hostile force bars the expedition route.",
                        NextNodeIds = new[] {"EXIT_1"},
                        ChoiceIds = new[] {"ENTER_CERTIFIED_BATTLE"},
                        RequiresCertifiedBattle = true,
                        EnemyUnionCount = enemyUnionCount,
                        GuildXp = 40,
                        HallXp = 40
                    },
                    new WorldGateNodeRule023
                    {
                        NodeId = "EXIT_1",
                        Kind = "EXIT",
                        Title = "Return",
                        Description = "The expedition returns to the Guild.",
                        ChoiceIds = new[] {"RETURN_HOME"}
                    }
                }
            };

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023;
    }
}
#endif
