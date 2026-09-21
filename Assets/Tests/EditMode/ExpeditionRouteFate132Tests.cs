#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        CampaignState PairedRouteFateFixture132(string category, bool naturalTwenty,
            out CampaignState rolled)
        {
            for(var seed=132800;seed<132992;seed++)
            {
                var campaign=CreateAtWorldBoard("CH018_001","SKYHOME",seed);
                campaign=Require(_worldGate.BeginOperation(campaign,_catalog023,"CH018_001",
                    new[]{"DECK_UNION_089"},_catalog020));
                var runtime=WorldGate(campaign);var operation=runtime.ActiveOperation;
                Assert.That(_catalog023.TryGetBoard(operation.DefinitionId,out var board),Is.True);
                // Synthetic mature-deck fixture, using the real authored board
                // and generated cards. Every node/edge/check below is advanced
                // by actual commands; neither die nor outcome is overwritten.
                var deck=_deck.Create(campaign.CampaignSeed,operation.OperationId,board,campaign.Guild.Recruits,
                    campaign.Guild.Inventory,null,runtime.ExpeditionRecruitLeadIds089,guildLevel:6);
                campaign=WithWorldGate089(campaign,runtime.With(activeOperation:operation.With(
                    expeditionDeck089:deck,replaceExpeditionDeck089:true),replaceActiveOperation:true));
                for(var step=0;step<24;step++)
                {
                    operation=WorldGate(campaign).ActiveOperation;
                    if(operation.Status==WorldGateOperationStatus023.ReadyToFinalize)break;
                    deck=operation.ExpeditionDeck089;
                    var node=board.Nodes.Single(x=>x.NodeId==operation.CurrentNodeId);
                    if(node.CheckDifficulty>0 && deck.CurrentRow.All(x=>x.AdvancesRoute))
                    {
                        var offered=AllCards(deck).FirstOrDefault(x=>x.NodeId==node.NodeId && x.AdvancesRoute &&
                            x.Category==(category=="PERMANENT"?"BUFF":category));
                        if(offered!=null)
                        {
                            if(category=="PERMANENT")
                            {
                                // Compatibility fixture for an old uncommitted
                                // permanent route card, with its existing owned
                                // target and stable generated route CardId.
                                offered=new ExpeditionRouteCardState089(offered.CardId,offered.NodeId,offered.ChoiceId,
                                    "PERMANENT","BUFF","Legacy route boon","Legacy fate","PERMANENT","SUCCESS","Hero boon",
                                    offered.ResolutionDifficulty,offered.ItemCheckModifier,4,4,Array.Empty<string>(),0,
                                    "","","","CAMPAIGN023|LEGACY_ROUTE_FATE132",advancesRoute:true,
                                    permanentHeroRecruitId:"DECK_RECRUIT_A_089",permanentHeroName:"Deck Recruit A",
                                    permanentHeroEffectKind:"BOON",permanentHeroEffectId:ExpeditionDeckService089.PermanentBoonPrefix089+"OLD_ROUTE132",
                                    permanentHeroCheckModifier:1);
                            }
                            var row=new[]{offered}.Concat(AllCards(deck).Where(x=>x.NodeId==node.NodeId &&
                                x.AdvancesRoute && x.CardId!=offered.CardId).Take(2)).ToArray();
                            Assert.That(row.Length,Is.EqualTo(3));
                            var ids=row.Select(x=>x.CardId).ToArray();
                            deck=deck.With(currentRow:row,drawPile:AllCards(deck).Where(x=>!ids.Contains(x.CardId) &&
                                !deck.DiscardPile.Any(old=>old.CardId==x.CardId) && !deck.BanishedCards.Any(old=>old.CardId==x.CardId)).ToArray());
                            runtime=WorldGate(campaign);
                            campaign=WithWorldGate089(campaign,runtime.With(activeOperation:operation.With(
                                expeditionDeck089:deck,replaceExpeditionDeck089:true),replaceActiveOperation:true));
                            var sealedState=Require(_commands.CommitRouteCard(campaign,_catalog023,offered.CardId,
                                "DECK_RECRUIT_A_089","DECK_RECRUIT_B_089"));
                            var receipt=WorldGate(sealedState).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                            var candidate=Require(_commands.RollCommittedEffect132(sealedState,_catalog023,receipt.ReceiptId));
                            if((WorldGate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt.Effect132.D20==20)==naturalTwenty)
                            {rolled=candidate;return sealedState;}
                            break;
                        }
                    }
                    var next=deck.CurrentRow.First(x=>!ExpeditionDeckService089.IsOptionalBattleCard089(x) && x.TreasuryXpCost==0);
                    campaign=Require(_commands.CommitRouteCard(campaign,_catalog023,next.CardId,"DECK_RECRUIT_A_089","DECK_RECRUIT_B_089"));
                    var pending=WorldGate(campaign).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                    if(pending.Effect132!=null && !pending.Effect132.IsRolled132)
                        campaign=Require(_commands.RollCommittedEffect132(campaign,_catalog023,pending.ReceiptId));
                    campaign=Require(_commands.ApplyWorldGateReceiptExactlyOnce(campaign,_catalog023));
                }
            }
            throw new InvalidOperationException("No matching actual paired route D20 outcome in bounded fixture seeds.");
        }

        [TestCase("BUFF",false)]
        [TestCase("BUFF",true)]
        [TestCase("PERMANENT",true)]
        [TestCase("HAZARD",true)]
        public void RouteFateKeepsPairedAuthoredCheckAndAppliesD20BenefitExactlyOnce132(string category,bool naturalTwenty)
        {
            var sealedState=PairedRouteFateFixture132(category,naturalTwenty,out var rolled);
            var sealedOperation=WorldGate(sealedState).ActiveOperation;
            var sealedReceipt=sealedOperation.ExpeditionDeck089.PendingReceipt;
            var original=ExpeditionDeckService089.BaseRouteReceipt132(sealedReceipt);
            var worldGateJson=CanonicalJson.Serialize(sealedOperation.PendingReceipt);
            Assert.That(original.DieOne,Is.GreaterThan(0),"This is a real paired authored check, not a zero-dice route substitute.");
            Assert.That(original.DelegatedToWorldGateCheck,Is.True);
            Assert.That(sealedReceipt.Effect132.IsRolled132,Is.False);
            Assert.That(_deck.ValidateRouteFatePair132(sealedOperation),Is.True);
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(sealedState,_catalog023).IsSuccess,Is.False);
            Assert.That(_worldGate.ApplyNodeReceiptExactlyOnce(sealedState,_catalog023).IsSuccess,Is.False,
                "The old direct route authority cannot bypass a sealed D20.");
            var reRolled=Require(_commands.RollCommittedEffect132(ReloadEffect132(sealedState),_catalog023,sealedReceipt.ReceiptId));
            Assert.That(CanonicalJson.Serialize(reRolled),Is.EqualTo(CanonicalJson.Serialize(rolled)));
            rolled=ReloadEffect132(rolled);
            var op=WorldGate(rolled).ActiveOperation;var receipt=op.ExpeditionDeck089.PendingReceipt;
            Assert.That(CanonicalJson.Serialize(op.PendingReceipt),Is.EqualTo(worldGateJson));
            Assert.That(CanonicalJson.Serialize(ExpeditionDeckService089.BaseRouteReceipt132(receipt)),Is.EqualTo(CanonicalJson.Serialize(original)));
            Assert.That(_commands.RollCommittedEffect132(rolled,_catalog023,receipt.ReceiptId).IsSuccess,Is.False);
            var after=ReloadEffect132(Require(_commands.ApplyWorldGateReceiptExactlyOnce(rolled,_catalog023)));
            var current=WorldGate(after).ActiveOperation;
            Assert.That(current.CurrentNodeId,Is.EqualTo(op.PendingReceipt.NextNodeId));
            Assert.That(CanonicalJson.Serialize(current.AppliedReceipts.Single(x=>x.ReceiptId==op.PendingReceipt.ReceiptId)),Is.EqualTo(worldGateJson));
            Assert.That(current.ExpeditionDeck089.Momentum,Is.EqualTo(Math.Max(-2,Math.Min(2,
                op.ExpeditionDeck089.Momentum+ExpeditionDeckService089.AppliedMomentum132(receipt)))));
            Assert.That(after.Guild.TreasuryXp-rolled.Guild.TreasuryXp,
                Is.EqualTo(op.PendingReceipt.GuildXp+original.GuildXp-original.TreasuryXpCost));
            Assert.That(after.Guild.GuildCity.Materials.Sum(x=>x.Amount)-rolled.Guild.GuildCity.Materials.Sum(x=>x.Amount),
                Is.EqualTo(op.PendingReceipt.MaterialIds.Count+original.MaterialIds.Count));
            Assert.That(CanonicalJson.Serialize(after.Guild.Inventory),Is.EqualTo(CanonicalJson.Serialize(rolled.Guild.Inventory)));
            foreach(var hero in rolled.Guild.Recruits)
            {
                var expected=hero;
                if(category!="HAZARD" && naturalTwenty && hero.RecruitId==receipt.Effect132.TargetRecruitId)
                    expected=hero.WithProgression(hero.Progression.WithUnlockedTrees(hero.Progression.UnlockedTreeIds
                        .Concat(new[]{ExpeditionDeckService089.PermanentEffectId132(receipt)}).OrderBy(x=>x,StringComparer.Ordinal).ToArray()));
                Assert.That(CanonicalJson.Serialize(after.Guild.Recruits.Single(x=>x.RecruitId==hero.RecruitId)),Is.EqualTo(CanonicalJson.Serialize(expected)));
            }
            if(category=="HAZARD")Assert.That(receipt.Effect132.TargetRecruitId,Is.Empty,"Even natural20 curses are temporary.");
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(after,_catalog023).IsSuccess,Is.False);
            var completed=RoundTripCost093(FinishActualDeck093(after));
            Assert.That(CampaignWorldGateCommandService023.ValidateStoredCompletionProofs084(completed,_catalog023,WorldGate(completed)),Is.True);
        }

        [Test]
        public void RouteFateRejectsChangedPairedReceiptAndPreservesCommittedLegacyShape132()
        {
            var sealedState=PairedRouteFateFixture132("BUFF",false,out _);
            var op=WorldGate(sealedState).ActiveOperation;var receipt=op.ExpeditionDeck089.PendingReceipt;
            var document=JObject.Parse(JsonConvert.SerializeObject(op.PendingReceipt));
            document["GuildXp"]=op.PendingReceipt.GuildXp+1;
            var mutated=WithWorldGate089(sealedState,WorldGate(sealedState).With(activeOperation:op.With(
                pendingReceipt:document.ToObject<WorldGateNodeReceipt023>(),replacePendingReceipt:true),replaceActiveOperation:true));
            Assert.That(_commands.RollCommittedEffect132(mutated,_catalog023,receipt.ReceiptId).IsSuccess,Is.False);
            // A byte-compatible already committed legacy route has no extension.
            var legacy=ExpeditionDeckService089.BaseRouteReceipt132(receipt);
            var json=CanonicalJson.Serialize(legacy);
            Assert.That(json,Does.Not.Contain("Effect132"));
            var legacyState=WithWorldGate089(sealedState,WorldGate(sealedState).With(activeOperation:op.With(
                expeditionDeck089:op.ExpeditionDeck089.With(pendingReceipt:legacy,replacePendingReceipt:true),
                replaceExpeditionDeck089:true),replaceActiveOperation:true));
            var reloaded=ReloadEffect132(legacyState);
            Assert.That(CanonicalJson.Serialize(WorldGate(reloaded).ActiveOperation.ExpeditionDeck089.PendingReceipt),Is.EqualTo(json));
            var applied=Require(_commands.ApplyWorldGateReceiptExactlyOnce(reloaded,_catalog023));
            Assert.That(WorldGate(applied).ActiveOperation.ExpeditionDeck089.Momentum,
                Is.EqualTo(Math.Max(-2,Math.Min(2,op.ExpeditionDeck089.Momentum+legacy.MomentumDelta))));
        }
    }
}
#endif
