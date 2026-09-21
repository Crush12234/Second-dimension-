#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Creator028;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        [Test]
        public void OptionalBattleCost093NextCardTenRoundsCompletionAndReloadRemainCanonical()
        {
            var campaign = ActualOptionalBattleReturn093();
            var returned = WorldGate(campaign).ActiveOperation;
            Assert.That(returned.OptionalBattleCosts093, Has.Count.EqualTo(1));
            var cost = returned.OptionalBattleCosts093[0];
            Assert.That(returned.Supplies, Is.EqualTo(Math.Max(0,
                cost.Request.Supplies - cost.BattleReturn.SupplyConsumption)));
            Assert.That(returned.Fatigue, Is.EqualTo(Math.Max(0,
                cost.Request.Fatigue + cost.BattleReturn.FatigueDelta)));
            Assert.That(CampaignWorldGateCommandService023.ValidateActiveAuthority093(
                campaign, _catalog023, out var error), Is.True, error);
            campaign = RoundTripCost093(campaign);
            campaign = FinishActualDeck093(campaign);
            var proof = WorldGate(campaign).LastCompletionProof;
            Assert.That(proof.OptionalBattleCosts093, Has.Count.EqualTo(1));
            Assert.That(proof.DelegatedCheckReceipts093, Is.Not.Empty,
                "This path must preserve genuinely modified delegated checks.");
            Assert.That(CampaignWorldGateCommandService023.ValidateCompletionProof084(
                campaign, _catalog023, proof), Is.True);
            campaign = RoundTripCost093(campaign);
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                campaign, _catalog023, WorldGate(campaign)), Is.True);
        }

        [Test]
        public void OptionalBattleCost093OldActivePostBattleMigratesOnlyVerifiedProofExactlyOnce()
        {
            var current = ActualOptionalBattleReturn093();
            var old = RemoveNewCostProof093(current);
            var xp = old.Guild.TreasuryXp;
            var roster = CanonicalJson.Serialize(old.Guild.Recruits);
            var inventory = CanonicalJson.Serialize(old.Guild.Inventory);
            var rewardIds = CanonicalJson.Serialize(old.Guild.Development.ClaimedBattleRewardIds);
            var oldResources = WorldGate(old).ActiveOperation;
            var recovered = Require(_commands.RecoverVerifiedOptionalBattleCost093(old, _catalog023));
            Assert.That(recovered.Guild.TreasuryXp, Is.EqualTo(xp));
            Assert.That(CanonicalJson.Serialize(recovered.Guild.Recruits), Is.EqualTo(roster));
            Assert.That(CanonicalJson.Serialize(recovered.Guild.Inventory), Is.EqualTo(inventory));
            Assert.That(CanonicalJson.Serialize(recovered.Guild.Development.ClaimedBattleRewardIds), Is.EqualTo(rewardIds));
            Assert.That(WorldGate(recovered).ActiveOperation.Supplies, Is.EqualTo(oldResources.Supplies));
            Assert.That(WorldGate(recovered).ActiveOperation.Fatigue, Is.EqualTo(oldResources.Fatigue));
            Assert.That(CanonicalJson.Serialize(Require(_commands.RecoverVerifiedOptionalBattleCost093(recovered, _catalog023))),
                Is.EqualTo(CanonicalJson.Serialize(recovered)));
            recovered = RoundTripCost093(recovered);
            Assert.That(CampaignWorldGateCommandService023.ValidateActiveAuthority093(
                recovered, _catalog023, out var error), Is.True, error);
            FinishActualDeck093(recovered);
        }

        [Test]
        public void OptionalBattleCost093OldActiveMissingRealBattleFailsWithoutAnyGrant()
        {
            var old = RemoveNewCostProof093(ActualOptionalBattleReturn093());
            var document = JObject.Parse(JsonConvert.SerializeObject(old));
            document["Battle"] = null;
            var broken = document.ToObject<CampaignState>();
            var before = CanonicalJson.Serialize(broken);
            var result = _commands.RecoverVerifiedOptionalBattleCost093(broken, _catalog023);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(string.Join(";", result.Errors), Does.Contain("REQUIRES_VERIFIED_CHECKPOINT"));
            Assert.That(CanonicalJson.Serialize(broken), Is.EqualTo(before));
        }

        [TestCase("duplicate")]
        [TestCase("wrong_node")]
        [TestCase("resource_tamper")]
        public void OptionalBattleCost093ForgedOrDuplicateCostsNeverPassAuthority(string mutation)
        {
            var campaign = ActualOptionalBattleReturn093();
            var document = JObject.Parse(JsonConvert.SerializeObject(campaign));
            var operation = (JObject)document.SelectToken(CostOperationPath093);
            var costs = (JArray)operation["OptionalBattleCosts093"];
            if (mutation == "duplicate") costs.Add(costs[0].DeepClone());
            else if (mutation == "wrong_node") costs[0]["NodeId"] = "NOT_AN_EARNED_NODE";
            else costs[0]["BattleReturn"]["FatigueDelta"] = 99;
            var tampered = document.ToObject<CampaignState>();
            Assert.That(CampaignWorldGateCommandService023.ValidateActiveAuthority093(
                tampered, _catalog023, out _), Is.False);
        }

        [Test]
        public void OptionalBattleCost093OldNoOptionalActiveAndCompletedSaveHashesStayUnchanged()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020,
                HeroMaster300CreatorRegistry087.Load().Source));
            Assert.That(CanonicalJson.Serialize(campaign), Does.Not.Contain("OptionalBattleCosts093"));
            var oldHash = CanonicalJson.Sha256Hex(campaign);
            var recovered = Require(_commands.RecoverVerifiedOptionalBattleCost093(campaign, _catalog023));
            Assert.That(CanonicalJson.Sha256Hex(RoundTripCost093(recovered)), Is.EqualTo(oldHash));
            // Exercise the old canonical route command contract, which has no
            // deck-selected modifiers and therefore no new proof extensions.
            var completed = FinishLegacyCanonicalRoute093(recovered);
            Assert.That(CanonicalJson.Serialize(completed), Does.Not.Contain("OptionalBattleCosts093"));
            Assert.That(CanonicalJson.Serialize(completed), Does.Not.Contain("DelegatedCheckReceipts093"));
            var proofHash = WorldGate(completed).LastCompletionProof.CompletionLedgerHash;
            var reloaded = RoundTripCost093(completed);
            Assert.That(WorldGate(reloaded).LastCompletionProof.CompletionLedgerHash, Is.EqualTo(proofHash));
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                reloaded, _catalog023, WorldGate(reloaded)), Is.True);
        }

        [Test]
        public void OptionalBattleCost093ModifiedDeckWithoutBattleCompletesWithExactCheckProof()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020,
                HeroMaster300CreatorRegistry087.Load().Source));
            campaign = RoundTripCost093(FinishActualDeck093(campaign));
            var proof = WorldGate(campaign).LastCompletionProof;
            Assert.That(proof.OptionalBattleCosts093, Is.Null);
            Assert.That(proof.DelegatedCheckReceipts093, Is.Not.Empty);
            foreach (var card in proof.DelegatedCheckReceipts093)
            {
                Assert.That(card.BaseCheckModifier, Is.Not.Zero);
                Assert.That(campaign.Guild.Development.HasAdventureAuthority(card.ReceiptId), Is.True);
                Assert.That(proof.AppliedReceipts.Single(value => value.NodeId == card.NodeId).Modifier,
                    Is.EqualTo(card.EffectiveModifier));
            }
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(
                campaign, _catalog023, WorldGate(campaign)), Is.True);
        }

        [TestCase("modifier")]
        [TestCase("receipt_authority")]
        [TestCase("duplicate")]
        public void OptionalBattleCost093TamperedDelegatedCheckProofFailsEvenWithRehashedEnvelope(string mutation)
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023,
                "CH018_001", new[] { "DECK_UNION_089" }, _catalog020,
                HeroMaster300CreatorRegistry087.Load().Source));
            campaign = FinishActualDeck093(campaign);
            var document = JObject.Parse(JsonConvert.SerializeObject(WorldGate(campaign).LastCompletionProof));
            var checks = (JArray)document["DelegatedCheckReceipts093"];
            Assert.That(checks, Is.Not.Empty);
            if (mutation == "duplicate") checks.Add(checks[0].DeepClone());
            else if (mutation == "modifier") checks[0]["BaseCheckModifier"] = 99;
            else checks[0]["ReceiptId"] = "EXPREC089_NOT_COMMITTED";
            var altered = document.ToObject<WorldGateCompletionProof023>();
            var hashMethod = typeof(CampaignWorldGateCommandService023).GetMethod(
                "CompletionLedgerHash084", System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);
            Assert.That(hashMethod, Is.Not.Null);
            document["CompletionLedgerHash"] = (string)hashMethod.Invoke(null, new object[] { altered });
            Assert.That(CampaignWorldGateCommandService023.ValidateCompletionProof084(
                campaign, _catalog023, document.ToObject<WorldGateCompletionProof023>()), Is.False);
        }

        private const string CostOperationPath093 = "Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation";

        private CampaignState ActualOptionalBattleReturn093()
        {
            var campaign = CreateAtWorldBoard("CH018_001", "SKYHOME", 89304);
            campaign = Require(_worldGate.BeginOperation(campaign, _catalog023, "CH018_001",
                new[] { "DECK_UNION_089" }, _catalog020, HeroMaster300CreatorRegistry087.Load().Source));
            var card = WorldGate(campaign).ActiveOperation.ExpeditionDeck089.CurrentRow.Single(
                ExpeditionDeckService089.IsOptionalBattleCard089);
            campaign = Require(_commands.CommitRouteCard(campaign, _catalog023, card.CardId,
                "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089"));
            campaign = Require(_battleBridge.StartCertifiedEncounter(campaign, _battles, _combatContent));
            campaign = ResolveBattle089(campaign);
            campaign = Require(_battles.ClaimBattleRewards(campaign));
            campaign = Require(_battleBridge.CommitBattleReturn(campaign));
            campaign = Require(_battleBridge.ApplyBattleReturnExactlyOnce(campaign));
            return Require(_commands.SynchronizeOptionalBattleAfterClaim089(campaign, _catalog023));
        }

        private CampaignState FinishActualDeck093(CampaignState campaign)
        {
            for (var guard = 0; guard < 100; guard++)
            {
                var operation = WorldGate(campaign).ActiveOperation;
                if (operation.Status == WorldGateOperationStatus023.ReadyToFinalize)
                {
                    Assert.That(operation.ExpeditionDeck089.AppliedReceiptIds.Count, Is.GreaterThanOrEqualTo(10));
                    return Require(_worldGate.FinalizeOperation(campaign, _catalog023));
                }
                var card = operation.ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                    !ExpeditionDeckService089.IsOptionalBattleCard089(value) && value.TreasuryXpCost <= campaign.Guild.TreasuryXp);
                Assert.That(card, Is.Not.Null, "No legal nonbattle test card.");
                campaign = Require(_commands.CommitRouteCard(campaign, _catalog023, card.CardId,
                    "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089"));
                var sealedEffect132 = WorldGate(campaign).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                if (sealedEffect132?.Effect132 != null && !sealedEffect132.Effect132.IsRolled132)
                    campaign = Require(_commands.RollCommittedEffect132(campaign, _catalog023,
                        sealedEffect132.ReceiptId));
                campaign = Require(_commands.ApplyWorldGateReceiptExactlyOnce(campaign, _catalog023));
            }
            Assert.Fail("Normal deck commands did not reach the exit.");
            return campaign;
        }

        private CampaignState FinishLegacyCanonicalRoute093(CampaignState campaign)
        {
            for (var guard = 0; guard < 30; guard++)
            {
                var operation = WorldGate(campaign).ActiveOperation;
                if (operation.Status == WorldGateOperationStatus023.ReadyToFinalize)
                    return Require(_worldGate.FinalizeOperation(campaign, _catalog023));
                Assert.That(_catalog023.TryGetBoard(operation.DefinitionId, out var board), Is.True);
                var node = board.Nodes.Single(value => value.NodeId == operation.CurrentNodeId);
                Assert.That(node.RequiresCertifiedBattle, Is.False);
                campaign = Require(_worldGate.CommitNodeChoice(campaign, _catalog023,
                    node.ChoiceIds.FirstOrDefault() ?? "CONTINUE",
                    "DECK_RECRUIT_A_089", "DECK_RECRUIT_B_089", 0));
                campaign = Require(_worldGate.ApplyNodeReceiptExactlyOnce(campaign, _catalog023));
            }
            Assert.Fail("Legacy canonical route did not reach exit.");
            return campaign;
        }

        private static CampaignState RoundTripCost093(CampaignState campaign)
        {
            var reloaded = JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign));
            Assert.That(CanonicalJson.Sha256Hex(reloaded), Is.EqualTo(CanonicalJson.Sha256Hex(campaign)));
            return reloaded;
        }

        private static CampaignState RemoveNewCostProof093(CampaignState campaign)
        {
            // Models the pre-093 persisted shape, retaining the actual battle,
            // committed card, claim, request authority and return authorities.
            var document = JObject.Parse(JsonConvert.SerializeObject(campaign));
            ((JObject)document.SelectToken(CostOperationPath093)).Remove("OptionalBattleCosts093");
            var authorities = (JArray)document.SelectToken("Guild.Development.AppliedAdventureAuthorityIds");
            Assert.That(authorities, Is.Not.Null);
            foreach (var authority in authorities.Where(value => value.ToString().StartsWith("EXPCOSTAUTH093_", StringComparison.Ordinal)).ToArray())
                authority.Remove();
            return document.ToObject<CampaignState>();
        }
    }
}
#endif
