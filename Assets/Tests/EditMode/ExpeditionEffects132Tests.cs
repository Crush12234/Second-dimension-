#if UNITY_EDITOR
using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    public sealed partial class ExpeditionDeck089Tests
    {
        // Synthetic authored-board fixture. Seeds are searched, never dice or
        // rewards overwritten. Existing helper deals a generated current-node
        // effect card into a valid three-card row so each rule is reproducible.
        CampaignState SealedEffectFixture132(string kind, Func<ExpeditionEffectReceipt132,bool> matches,
            out CampaignState rolled)
        {
            for(var attempt=0;attempt<192;attempt++)
            {
                var campaign=CreateAtWorldBoard("CH018_001","SKYHOME",132000+attempt);
                campaign=Require(_worldGate.BeginOperation(campaign,_catalog023,"CH018_001",
                    new[]{"DECK_UNION_089"},_catalog020));
                var runtime=WorldGate(campaign);var operation=runtime.ActiveOperation;
                Assert.That(_catalog023.TryGetBoard(operation.DefinitionId,out var board),Is.True);
                var deck=_deck.Create(campaign.CampaignSeed,operation.OperationId,board,campaign.Guild.Recruits,
                    campaign.Guild.Inventory,null,runtime.ExpeditionRecruitLeadIds089,guildLevel:6);
                var card=AllCards(deck).FirstOrDefault(value=>value.NodeId==operation.CurrentNodeId &&
                    ExpeditionDeckService089.EffectKind132(value)==kind);
                if(card==null)continue;
                deck=ForceEncounterCardIntoRow089(deck,card);
                campaign=WithWorldGate089(campaign,runtime.With(activeOperation:operation.With(
                    expeditionDeck089:deck,replaceExpeditionDeck089:true),replaceActiveOperation:true));
                var selected=Require(_commands.CommitRouteCard(campaign,_catalog023,card.CardId,
                    "DECK_RECRUIT_A_089","DECK_RECRUIT_B_089"));
                var receipt=WorldGate(selected).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                var result=Require(_commands.RollCommittedEffect132(selected,_catalog023,receipt.ReceiptId));
                if(!matches(WorldGate(result).ActiveOperation.ExpeditionDeck089.PendingReceipt.Effect132))continue;
                rolled=result;return selected;
            }
            throw new InvalidOperationException("No matching authored deterministic fate in bounded fixture seeds.");
        }
        static CampaignState ReloadEffect132(CampaignState source)
        {
            var json=CanonicalJson.Serialize(source);
            var result=JsonConvert.DeserializeObject<CampaignState>(json);
            Assert.That(CanonicalJson.Serialize(result),Is.EqualTo(json));return result;
        }

        [Test]
        public void ExplicitD20NaturalTwentyGrantsOneOwnedHeroPermanentCheckBoonAcrossReload132()
        {
            var sealedState=SealedEffectFixture132(ExpeditionDeckService089.BoonD20132,e=>e.D20==20,out var expected);
            var sealedReceipt=WorldGate(sealedState).ActiveOperation.ExpeditionDeck089.PendingReceipt;
            Assert.That(sealedReceipt.Effect132.IsRolled132,Is.False);
            Assert.That(sealedReceipt.Effect132.D20,Is.Zero);
            Assert.That(sealedReceipt.Effect132.TargetRecruitId,Is.Empty);
            Assert.That(sealedReceipt.GuildXp,Is.Zero);
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(sealedState,_catalog023).IsSuccess,Is.False);
            var rolled=Require(_commands.RollCommittedEffect132(ReloadEffect132(sealedState),_catalog023,sealedReceipt.ReceiptId));
            Assert.That(CanonicalJson.Serialize(rolled),Is.EqualTo(CanonicalJson.Serialize(expected)));
            rolled=ReloadEffect132(rolled);
            Assert.That(_commands.RollCommittedEffect132(rolled,_catalog023,sealedReceipt.ReceiptId).IsSuccess,Is.False);
            var receipt=WorldGate(rolled).ActiveOperation.ExpeditionDeck089.PendingReceipt;
            var target=receipt.Effect132.TargetRecruitId;
            Assert.That(rolled.Guild.Recruits.Any(hero=>hero.RecruitId==target),Is.True);
            var after=ReloadEffect132(Require(_commands.ApplyWorldGateReceiptExactlyOnce(rolled,_catalog023)));
            CollectionAssert.AreEqual(rolled.Guild.Recruits.Select(x=>x.RecruitId),after.Guild.Recruits.Select(x=>x.RecruitId));
            Assert.That(CanonicalJson.Serialize(after.Guild.Inventory),Is.EqualTo(CanonicalJson.Serialize(rolled.Guild.Inventory)));
            Assert.That(CanonicalJson.Serialize(after.Guild.Unions),Is.EqualTo(CanonicalJson.Serialize(rolled.Guild.Unions)));
            foreach(var hero in rolled.Guild.Recruits)
            {
                var actual=after.Guild.Recruits.Single(x=>x.RecruitId==hero.RecruitId);
                var expectedHero=hero.RecruitId==target ? hero.WithProgression(hero.Progression.WithUnlockedTrees(
                    hero.Progression.UnlockedTreeIds.Concat(new[]{ExpeditionDeckService089.PermanentEffectId132(receipt)})
                    .OrderBy(x=>x,StringComparer.Ordinal).ToArray())) : hero;
                Assert.That(CanonicalJson.Serialize(actual),Is.EqualTo(CanonicalJson.Serialize(expectedHero)));
            }
            Assert.That(after.Guild.TreasuryXp,Is.EqualTo(rolled.Guild.TreasuryXp+receipt.GuildXp));
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(after,_catalog023).IsSuccess,Is.False);
        }

        [TestCase(ExpeditionDeckService089.BoonD20132)]
        [TestCase(ExpeditionDeckService089.CurseD20132)]
        public void OrdinaryD20ChangesOnlyThisQuestCheckMomentumAndEarnedXp132(string kind)
        {
            SealedEffectFixture132(kind,e=>e.D20<20,out var rolled);
            var beforeDeck=WorldGate(rolled).ActiveOperation.ExpeditionDeck089;
            var receipt=beforeDeck.PendingReceipt;
            var after=ReloadEffect132(Require(_commands.ApplyWorldGateReceiptExactlyOnce(rolled,_catalog023)));
            var expected=kind==ExpeditionDeckService089.BoonD20132 ? receipt.Effect132.D20<10?1:2 : receipt.Effect132.D20<10?-2:-1;
            Assert.That(receipt.MomentumDelta,Is.EqualTo(expected));
            Assert.That(WorldGate(after).ActiveOperation.ExpeditionDeck089.Momentum,
                Is.EqualTo(Math.Max(-2,Math.Min(2,beforeDeck.Momentum+expected))));
            Assert.That(CanonicalJson.Serialize(after.Guild.Recruits),Is.EqualTo(CanonicalJson.Serialize(rolled.Guild.Recruits)),
                "Temporary dice effects never write permanent traits, equipment, stats or hero identity.");
            var fresh=_deck.Create(after.CampaignSeed,"NEXT_QUEST132",SimpleBoard(),after.Guild.Recruits,
                after.Guild.Inventory,null,Array.Empty<string>());
            Assert.That(fresh.Momentum,Is.Zero,"Quest momentum expires with its deck.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FortuneWheelUsesExactlyOneExistingRewardAuthorityAcrossReload132(int sector)
        {
            SealedEffectFixture132(ExpeditionDeckService089.Wheel132,e=>e.WheelSector==sector,out var rolled);
            var receipt=WorldGate(rolled).ActiveOperation.ExpeditionDeck089.PendingReceipt;
            var after=ReloadEffect132(Require(_commands.ApplyWorldGateReceiptExactlyOnce(ReloadEffect132(rolled),_catalog023)));
            Assert.That(after.Guild.TreasuryXp-rolled.Guild.TreasuryXp,Is.EqualTo(receipt.GuildXp));
            Assert.That(after.Guild.GuildCity.Materials.Sum(x=>x.Amount)-rolled.Guild.GuildCity.Materials.Sum(x=>x.Amount),
                Is.EqualTo(receipt.MaterialIds.Count));
            Assert.That(after.Guild.Inventory.Count-rolled.Guild.Inventory.Count,Is.EqualTo(sector==2?1:0));
            if(sector==2)
            {
                var expected=ExpeditionDeckService089.ChestEquipmentReward089(receipt.CardId,receipt.ReceiptId);
                Assert.That(CanonicalJson.Serialize(after.Guild.Inventory.Single(x=>x.InstanceId==expected.InstanceId)),
                    Is.EqualTo(CanonicalJson.Serialize(expected)));
            }
            Assert.That(CanonicalJson.Serialize(after.Guild.Recruits),Is.EqualTo(CanonicalJson.Serialize(rolled.Guild.Recruits)));
            Assert.That(_commands.ApplyWorldGateReceiptExactlyOnce(after,_catalog023).IsSuccess,Is.False);
        }

        [Test]
        public void D20TamperingAndMissingEffectDowngradeAreRejected132()
        {
            SealedEffectFixture132(ExpeditionDeckService089.BoonD20132,e=>e.D20==20,out var rolled);
            var deck=WorldGate(rolled).ActiveOperation.ExpeditionDeck089;var receipt=deck.PendingReceipt;
            foreach(var property in new[]{"D20","TargetRecruitId","Effect132"})
            {
                var json=JObject.Parse(JsonConvert.SerializeObject(receipt));
                if(property=="Effect132")json.Remove(property);
                else if(property=="D20")json["Effect132"][property]=1;
                else json["Effect132"][property]="UNOWNED132";
                Assert.That(_deck.ValidateCommittedReceipt(deck,json.ToObject<ExpeditionCardReceipt089>()),Is.False,property);
            }
        }

        [Test]
        public void UncommittedLegacyBuffKeepsExactCardIdAndBecomesSealedD20OnlyOnChoice132()
        {
            var sealedState=SealedEffectFixture132(ExpeditionDeckService089.BoonD20132,e=>e.D20<20,out _);
            var runtime=WorldGate(sealedState);var operation=runtime.ActiveOperation;var deck=operation.ExpeditionDeck089;
            var selected=deck.CurrentRow.Single(x=>x.CardId==deck.PendingReceipt.CardId);
            var legacy=new ExpeditionRouteCardState089(selected.CardId,selected.NodeId,selected.ChoiceId,
                "BUFF","BUFF","Saved old blessing","Old preview","BOON","SUCCESS","+1 momentum",
                0,0,4,4,Array.Empty<string>(),1,"","","","CAMPAIGN023|LEGACY132",advancesRoute:false);
            deck=deck.With(currentRow:deck.CurrentRow.Select(x=>x.CardId==legacy.CardId?legacy:x).ToArray(),
                pendingReceipt:null,replacePendingReceipt:true);
            var before=CanonicalJson.Serialize(deck);
            var campaign=WithWorldGate089(sealedState,runtime.With(activeOperation:operation.With(
                expeditionDeck089:deck,replaceExpeditionDeck089:true),replaceActiveOperation:true));
            var next=Require(_commands.CommitRouteCard(campaign,_catalog023,legacy.CardId,"DECK_RECRUIT_A_089","DECK_RECRUIT_B_089"));
            var result=WorldGate(next).ActiveOperation.ExpeditionDeck089;
            Assert.That(result.PendingReceipt.CardId,Is.EqualTo(legacy.CardId));
            Assert.That(result.PendingReceipt.Effect132.IsRolled132,Is.False);
            Assert.That(CanonicalJson.Serialize(deck),Is.EqualTo(before),"No mutation of the source save.");
            Assert.That(_commands.CommitRouteCard(next,_catalog023,legacy.CardId,"DECK_RECRUIT_A_089","DECK_RECRUIT_B_089").IsSuccess,Is.False);
        }
    }
}
#endif
