using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Tests.EditMode
{
    // Controlled native cohorts: facility levels/starting wallet are explicit test inputs.
    // No user save, purchased result, random die, or earned victory is injected.
    public sealed class Merchant165Tests
    {
        static CampaignState Fresh(int level=0,long seed=165900)
        {
            var heroes=Enumerable.Range(1,6).Select(i=>new RecruitState("M165_R"+i,100,100,20,20)).ToArray();
            var unions=new[]{new UnionState("M165_U1","Test Union",UnionKind.Normal,heroes[0].RecruitId,heroes.Select(r=>r.RecruitId).ToArray(),
                "FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000)};
            var g=new GuildState("M165_G",100000,heroes,unions);
            g=g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,g.Development.SetFacilityLevel(TownProgression159.StateIds[1],level,0));
            return new CampaignState("00000000-0000-0000-0000-000000165900",seed,"1.0",ModeRuleSnapshot.StandardDefaults(),g,
                new NewGuildProfileState("Merchant Regression",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),
                new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete"));
        }
        static GuildCityRecruitmentService017D Recruiter()=>new GuildCityRecruitmentService017D(
            new RecruitAutoGenerator010(RecruitAutoGenerationCatalog010.LoadFromContentRoot(Environment.GetEnvironmentVariable("SD_FATE165_CONTENT"))),
            HeroMaster300Catalog087.FromJson(File.ReadAllText(Environment.GetEnvironmentVariable("SD_MERCHANT165_HEROES"))));
        static T Ok<T>(Result<T> result){Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
        static CampaignState Reload(CampaignState state){var json=CanonicalJson.Serialize(state);var r=JsonConvert.DeserializeObject<CampaignState>(json);Assert.That(CanonicalJson.Serialize(r),Is.EqualTo(json));return r;}
        static TownPurchaseQuote153 Quote(CampaignState s,int m,int slot,GuildCityRecruitmentService017D recruitment=null)=>Ok(TownService153.QuotePurchase(s,TownService153.OfferId165(s,m,slot),recruitment));
        [Test]
        public void FiveMerchantsHaveDistinctNativeInventories()
        {
            var s=Fresh(8);var recruiter=Recruiter();var signatures=new HashSet<string>();
            for(int m=0;m<5;m++)
            {
                var offers=Enumerable.Range(0,MerchantStockProgression163.SlotsPerMerchant(s)).Select(n=>TownService153.ReadOffer(s,TownService153.OfferId165(s,m,n),"",recruiter)).ToArray();
                Assert.That(offers.Length,Is.EqualTo(6));Assert.That(offers.All(q=>q.Cost>0&&!string.IsNullOrWhiteSpace(q.Name)),Is.True);
                signatures.Add(string.Join("|",offers.Select(q=>q.Kind165+":"+(q.Item?.DefinitionId??q.HeroStableId165))));
                if(m==2)Assert.That(offers.All(q=>q.Item.ValidSlotIds.Contains(EquipmentSlotIds.AccessoryOne)&&q.Item.ValidSlotIds.Contains(EquipmentSlotIds.AccessoryTwo)),Is.True);
                if(m==4)Assert.That(offers.Where(q=>q.Item.EquipmentTags.Contains("RELIC")).All(q=>q.Item.EquipmentTags.Contains("AMULET")),Is.True);
                if(m==1)Assert.That(offers[5].Item.EquipmentTags,Does.Contain("SHIELD"));
                if(m==3)Assert.That(offers.All(q=>q.Kind165=="HERO"&&q.Item==null&&q.ArtKey165=="HERO:"+q.HeroStableId165),Is.True);
            }
            Assert.That(signatures.Count,Is.EqualTo(5));Assert.That(Quote(s,0,0).Kind165,Is.EqualTo("CONSUMABLE"));
        }
        [Test]
        public void AccessoriesScaleThroughMarketMilestonesAndRetainNativePower()
        {
            int previous=0;foreach(var level in new[]{0,3,5,8,10})
            {
                var q=Quote(Fresh(level),2,0);var power=M2EquipmentPowerPolicy087.Resolve(q.Item);
                Assert.That(power.PhysicalAttack,Is.GreaterThanOrEqualTo(previous));previous=power.PhysicalAttack;
                Assert.That(q.Item.InventoryOnly,Is.False);Assert.That(q.Item.ValidSlotIds,Does.Contain(EquipmentSlotIds.AccessoryOne));Assert.That(q.Item.ValidSlotIds,Does.Contain(EquipmentSlotIds.AccessoryTwo));
                if(level==10)Assert.That(q.Item.QualityId,Is.EqualTo("QUALITY_GODLY"));
            }
        }
        [Test]
        public void EquipmentPurchaseAndReloadDebitExactlyOnceAndRejectStaleQuote()
        {
            var s=Fresh();var q=Quote(s,2,0);var stale=Quote(s,1,1);var purchased=Reload(Ok(TownService153.ConfirmPurchase(s,q)));
            Assert.That(purchased.Guild.TreasuryXp,Is.EqualTo(s.Guild.TreasuryXp-q.Cost));
            Assert.That(purchased.Guild.Inventory.Single().InstanceId,Is.EqualTo(q.Item.InstanceId));
            Assert.That(CanonicalJson.Serialize(Ok(TownService153.ConfirmPurchase(purchased,q))),Is.EqualTo(CanonicalJson.Serialize(purchased)));
            Assert.That(TownService153.ConfirmPurchase(purchased,stale).IsSuccess,Is.False);
            Assert.That(TownService153.Sold(purchased,q.OfferId),Is.True);
        }
        [Test]
        public void HeroPurchaseThenOwnedDuplicateUsesActualAscensionStats()
        {
            var recruiter=Recruiter();var s=Fresh();var q=Quote(s,3,0,recruiter);
            var acquired=Reload(Ok(TownService153.ConfirmPurchase(s,q,recruiter)));
            Assert.That(acquired.Guild.Recruits.Count,Is.EqualTo(s.Guild.Recruits.Count+1));
            Assert.That(acquired.Guild.TreasuryXp,Is.EqualTo(s.Guild.TreasuryXp-q.Cost));
            Assert.That(CanonicalJson.Serialize(Ok(TownService153.ConfirmPurchase(acquired,q,recruiter))),Is.EqualTo(CanonicalJson.Serialize(acquired)));
            var restock=Ok(TownService153.QuoteRestock159(acquired));var ready=Reload(Ok(TownService153.ConfirmRestock159(acquired,restock)));
            var duplicate=Quote(ready,3,0,recruiter);Assert.That(duplicate.HeroStableId165,Is.EqualTo(q.HeroStableId165));
            var before=ready.Guild.Recruits.Single(r=>r.AuthoredStableRecruitId==q.HeroStableId165);
            var merged=Reload(Ok(TownService153.ConfirmPurchase(ready,duplicate,recruiter)));
            var after=merged.Guild.Recruits.Single(r=>r.RecruitId==before.RecruitId);
            Assert.That(merged.Guild.Recruits.Count,Is.EqualTo(ready.Guild.Recruits.Count));
            Assert.That(after.Progression.AscensionLevel,Is.EqualTo(before.Progression.AscensionLevel+1));
            Assert.That(after.Progression.MaximumHpBonus-before.Progression.MaximumHpBonus,Is.EqualTo(RecruitAscensionRules089.MaximumHpBonusPerLevel));
            Assert.That(after.Progression.StrengthBonus-before.Progression.StrengthBonus,Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel));
            Assert.That(merged.Guild.TreasuryXp,Is.EqualTo(ready.Guild.TreasuryXp-duplicate.Cost));
            Assert.That(CanonicalJson.Serialize(Ok(TownService153.ConfirmPurchase(merged,duplicate,recruiter))),Is.EqualTo(CanonicalJson.Serialize(merged)));
        }
        [Test]
        public void LuckInventoryActivationAndConsumptionPersistExactlyOnce()
        {
            var s=Fresh();var q=Quote(s,0,0);var bought=Reload(Ok(TownService153.ConfirmPurchase(s,q)));
            Assert.That(TownLuckConsumables165.IsLuckItem(bought.Guild.Inventory.Single()),Is.True);
            Assert.That(TownLuckConsumables165.Activate(bought,q.Item.InstanceId,"stale").IsSuccess,Is.False);
            var active=Reload(Ok(TownLuckConsumables165.Activate(bought,q.Item.InstanceId,CanonicalJson.Sha256Hex(bought))));
            var charge=TownLuckConsumables165.ActiveChargeId(active);Assert.That(charge.Length,Is.EqualTo(64));
            Assert.That(active.Guild.Inventory,Is.Empty);Assert.That(active.Guild.TreasuryXp,Is.EqualTo(bought.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(Ok(TownLuckConsumables165.Activate(active,q.Item.InstanceId,"stale"))),Is.EqualTo(CanonicalJson.Serialize(active)));
            var consumed=Reload(TownLuckConsumables165.ConsumeForSeal(active,charge,"TEST_FATE165_NATIVE_CHARGE"));
            Assert.That(TownLuckConsumables165.ActiveChargeId(consumed),Is.Empty);
            Assert.That(TownLuckConsumables165.WasConsumedFor(consumed,charge,"TEST_FATE165_NATIVE_CHARGE"),Is.True);
            Assert.That(CanonicalJson.Serialize(TownLuckConsumables165.ConsumeForSeal(consumed,charge,"TEST_FATE165_NATIVE_CHARGE")),Is.EqualTo(CanonicalJson.Serialize(consumed)));
            Assert.Throws<InvalidOperationException>(()=>TownLuckConsumables165.ConsumeForSeal(consumed,charge,"DIFFERENT_FATE"));
        }
        sealed class Catalog : IWorldGateOperationsCatalog023
        {
            readonly WorldGateBoardRule023 board;public Catalog(WorldGateBoardRule023 b){board=b;}
            public IReadOnlyList<WorldGateBoardRule023> AllBoards=>new[]{board};public WorldGateRewardPolicy023 RewardPolicy=>new WorldGateRewardPolicy023();
            public bool TryGetBoard(string id,out WorldGateBoardRule023 value){value=board;return id==board.DefinitionId;}
            public bool TryGetTravel(string id,out WorldTravelRule023 value){value=null;return false;}
            public bool TryGetStanding(string id,out WorldStandingRule023 value){value=null;return false;}
            public bool TryGetRecruitUnlock(string id,out RecruitUnlockRule023 value){value=null;return false;}
        }
        static CampaignState World(CampaignState s,WorldGateOperationState023 operation)
        {
            var city=s.Guild.GuildCity;var strategic=city.Strategic017H;var progress=strategic.Campaign019;var playable=progress.Playable020;
            playable=playable.With(worldGate023:playable.WorldGate023.With(activeOperation:operation,replaceActiveOperation:true),replaceWorldGate023:true);
            progress=progress.With(playable020:playable,replacePlayable020:true);strategic=strategic.With(campaign019:progress,replaceCampaign019:true);
            return s.With(s.Guild.WithGuildCity(city.With(strategic017H:strategic,replaceStrategic017H:true)),s.OpeningFlow);
        }
        [Test]
        public void WorldGateBlessingConsumesLuckAtSealAndNativeRollKeepsHigher()
        {
            var s=Fresh();var q=Quote(s,0,0);var purchased=Ok(TownService153.ConfirmPurchase(s,q));
            var active=Ok(TownLuckConsumables165.Activate(purchased,q.Item.InstanceId,CanonicalJson.Sha256Hex(purchased)));
            var charge=TownLuckConsumables165.ActiveChargeId(active);var deckService=new ExpeditionDeckService089();
            var board=JObject.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("SD_FATE165_BOARDS")))["boards"].ToObject<WorldGateBoardRule023[]>().First();
            var deck=deckService.Create(s.CampaignSeed,"M165_WORLD",board,s.Guild.Recruits,s.Guild.Inventory,null,Array.Empty<string>(),1);
            var card=deck.CurrentRow.Concat(deck.DrawPile).First(c=>ExpeditionDeckService089.EffectKind132(c)==ExpeditionDeckService089.BoonD20132);
            deck=deck.With(currentRow:new[]{card},drawPile:deck.DrawPile.Where(c=>c.CardId!=card.CardId).ToArray());
            var operation=new WorldGateOperationState023("M165_WORLD",board.DefinitionId,board.BoardId,board.OperationKind,board.WorldId,deck.ShuffleSeedIdentity,
                card.NodeId,WorldGateOperationStatus023.Active,30,0,0,0,0,0,0,s.Guild.Unions.Select(u=>u.UnionId).ToArray(),Array.Empty<string>(),Array.Empty<string>(),null,"","TEST165",
                alliedRecruitIds:s.Guild.Recruits.Select(r=>r.RecruitId).ToArray(),expeditionDeck089:deck);
            var commands=new ExpeditionDeckCommandService089();var catalog=new Catalog(board);
            var sealedState=Reload(Ok(commands.CommitRouteCard(World(active,operation),catalog,card.CardId,"","")));
            var pending=sealedState.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation.ExpeditionDeck089.PendingReceipt;
            Assert.That(pending.Effect132.LuckChargeId165,Is.EqualTo(charge));Assert.That(pending.Effect132.IsRolled132,Is.False);
            Assert.That(TownLuckConsumables165.ActiveChargeId(sealedState),Is.Empty);Assert.That(TownLuckConsumables165.WasConsumedFor(sealedState,charge,pending.ReceiptId),Is.True);
            var rng=Pcg32.FromParts("EXPEDITION_EFFECT_132",deck.ShuffleSeedIdentity,deck.OperationId,deck.DeckId,card.CardId);
            var expected=Math.Max(rng.NextInclusive(1,20),rng.NextInclusive(1,20));
            var rolled=Reload(Ok(commands.RollCommittedEffect132(sealedState,catalog,pending.ReceiptId)));
            var final=rolled.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation.ExpeditionDeck089.PendingReceipt;
            Assert.That(final.Effect132.D20,Is.EqualTo(expected));Assert.That(commands.RollCommittedEffect132(rolled,catalog,pending.ReceiptId).IsSuccess,Is.False);
            Assert.That(rolled.Guild.TreasuryXp,Is.EqualTo(sealedState.Guild.TreasuryXp));
            Assert.That(deckService.ValidateCommittedReceipt(rolled.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation.ExpeditionDeck089,final),Is.True);
        }
        [Test]
        public void HistoricalMerchant153EquipmentQuotesKeepNativeAuthority()
        {
            var s=Fresh();var old=Ok(TownService153.QuotePurchase(s,TownService153.OfferId(s,0,0)));
            Assert.That(old.Kind165,Is.EqualTo("EQUIPMENT"));Assert.That(old.Item,Is.Not.Null);
            var result=Reload(Ok(TownService153.ConfirmPurchase(s,old)));Assert.That(result.Guild.Inventory.Single().InstanceId,Is.EqualTo(old.Item.InstanceId));
            Assert.That(result.Guild.TreasuryXp,Is.EqualTo(s.Guild.TreasuryXp-old.Cost));
        }
    }
}
