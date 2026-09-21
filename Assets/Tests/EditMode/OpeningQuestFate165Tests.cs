using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Navigation164;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Tests.EditMode
{
    // Controlled native cohorts; only campaign inputs vary to cover outcomes.
    // No dice, receipts, victory, route completion, or treasury is injected into a saved player profile.
    public sealed class OpeningQuestFate165Tests
    {
        GuildCityContent017D Content => GuildCityContent017D.LoadFromDirectory(Path.Combine(
            Environment.GetEnvironmentVariable("SD_FATE165_CONTENT"),"GUILD_CITY_017D"));
        readonly GuildCityExpeditionService017D service=new GuildCityExpeditionService017D();

        static CampaignState Fresh(long seed=165000)
        {
            var recruits=Enumerable.Range(1,10).Select(i=>new RecruitState("R"+i,100,100,20,20)).ToArray();
            var unions=new[]{new UnionState("U1","First Union",UnionKind.Normal,"R1",new[]{"R1","R2","R3"},
                "FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000),
                new UnionState("U2","Second Union",UnionKind.Normal,"R4",new[]{"R4","R5","R6"},
                "FORMATION_SKIRMISH_LINE","DOCTRINE_BALANCED",30,7000)};
            return new CampaignState("00000000-0000-0000-0000-000000165000",seed,"1.0",ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD_FATE_165",5000,recruits,unions),
                new NewGuildProfileState("Fate Regression",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),
                new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete"));
        }
        CampaignState Begin(string contract,long seed=165000)
        {
            return BeginFrom(Fresh(seed),contract);
        }
        CampaignState BeginFrom(CampaignState source,string contract)
        {
            var c=Require(service.AcceptContract(source,Content,contract));
            c=Require(service.StartExpedition(c,Content));
            if(contract==GuildCityExpeditionService017D.SecondStoryContractId076)
                c=Require(service.ResolveCommittedCheck(c,Content,"EVENT_FOUND_APPRENTICE","R1","R2",0));
            return c;
        }
        static CampaignState Require(Result<CampaignState> result)
        {Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
        static CampaignState Reload(CampaignState campaign)
        {
            var text=CanonicalJson.Serialize(campaign);var loaded=JsonConvert.DeserializeObject<CampaignState>(text);
            Assert.That(CanonicalJson.Serialize(loaded),Is.EqualTo(text));return loaded;
        }
        CampaignState Seal(CampaignState c,string category)
        {
            var card=service.BuildQuestCardRow090(c,Content).Single(x=>x.Category==category);
            return Require(service.CommitQuestCard090(c,Content,null,card.CardId));
        }
        CampaignState Roll(CampaignState c)=>Require(service.RollQuestFate165(c,Content,null,c.Guild.GuildCity.Expedition.PendingQuestFate165.ReceiptId));
        CampaignState Collect(CampaignState c)=>Require(service.CollectQuestFate165(c,Content,null,c.Guild.GuildCity.Expedition.PendingQuestFate165.ReceiptId));

        [TestCase(GuildCityExpeditionService017D.FirstStoryContractId066,"BOON")]
        [TestCase(GuildCityExpeditionService017D.FirstStoryContractId066,"FATE")]
        [TestCase(GuildCityExpeditionService017D.SecondStoryContractId076,"BOON")]
        [TestCase(GuildCityExpeditionService017D.SecondStoryContractId076,"FATE")]
        [TestCase("CONTRACT_RELIEF_ROAD","BOON")]
        [TestCase("CONTRACT_RELIEF_ROAD","FATE")]
        public void OpeningQuestsExplicitlySealRollAndCollectExactlyOnce(string contract,string category)
        {
            var before=Begin(contract);var originalRoute=CanonicalJson.Serialize(before.Guild.GuildCity.Expedition);
            var chosen=service.BuildQuestCardRow090(before,Content).Single(x=>x.Category==category);
            Assert.That(chosen.EffectKind165,Is.EqualTo(category=="FATE"?ExpeditionDeckService089.Wheel132:ExpeditionDeckService089.BoonD20132));
            var sealedState=Reload(Seal(before,category));var receipt=sealedState.Guild.GuildCity.Expedition.PendingQuestFate165;
            Assert.That(receipt.IsRolled165,Is.False);Assert.That(receipt.D20,Is.Zero);Assert.That(receipt.WheelSector,Is.EqualTo(-1));
            Assert.That(sealedState.Guild.TreasuryXp,Is.EqualTo(before.Guild.TreasuryXp));
            Assert.That(CanonicalJson.Serialize(sealedState.Guild.GuildCity.Expedition.With(pendingQuestFate165:null,replacePendingQuestFate165:true)),Is.EqualTo(originalRoute));
            Assert.That(service.BuildQuestCardRow090(sealedState,Content),Is.Empty);
            Assert.That(service.CollectQuestFate165(sealedState,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
            Assert.That(service.CommitMove(sealedState,Content,chosen.DestinationNodeId).IsSuccess,Is.False);
            Assert.That(service.CommitQuestCard090(sealedState,Content,null,chosen.CardId).IsSuccess,Is.False);
            var rolled=Reload(Roll(sealedState));var expected=CanonicalJson.Serialize(rolled.Guild.GuildCity.Expedition.PendingQuestFate165);
            Assert.That(rolled.Guild.TreasuryXp,Is.EqualTo(before.Guild.TreasuryXp));
            Assert.That(rolled.Guild.GuildCity.Expedition.Supplies,Is.EqualTo(before.Guild.GuildCity.Expedition.Supplies));
            Assert.That(rolled.Guild.GuildCity.Expedition.Fatigue,Is.EqualTo(before.Guild.GuildCity.Expedition.Fatigue));
            Assert.That(service.RollQuestFate165(rolled,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
            var parked=Reload(Require(LoopCheckpoint164.Switch(rolled,LoopCheckpoint164.Town)));
            var resumed=Reload(Require(LoopCheckpoint164.Switch(parked,LoopCheckpoint164.Campaign)));
            Assert.That(CanonicalJson.Serialize(resumed.Guild.GuildCity.Expedition.PendingQuestFate165),Is.EqualTo(expected));
            Assert.That(service.DescribeQuestFate165(resumed,Content).EffectRolled165,Is.True);
            var collected=Reload(Collect(resumed));Assert.That(collected.Guild.GuildCity.Expedition.PendingQuestFate165,Is.Null);
            if(category=="BOON")
            {
                var moved=Require(service.CommitMove(before,Content,chosen.DestinationNodeId));
                Assert.That(collected.Guild.GuildCity.Expedition.Supplies,Is.EqualTo(moved.Guild.GuildCity.Expedition.Supplies+1));
            }
            Assert.That(collected.Guild.GuildCity.Expedition.CurrentNodeId,Is.EqualTo(chosen.DestinationNodeId));
            Assert.That(collected.Guild.Development.HasAdventureAuthority(receipt.ReceiptId),Is.True);
            Assert.That(service.CollectQuestFate165(collected,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
            Assert.That(service.RollQuestFate165(collected,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
        }

        [Test]
        public void LegacyNullFateAndTamperedOrForeignPendingReceiptsAreRejected()
        {
            var before=Begin(GuildCityExpeditionService017D.FirstStoryContractId066);
            Assert.That(CanonicalJson.Serialize(Reload(before)),Does.Not.Contain("PendingQuestFate165"));
            var rolled=Roll(Seal(before,"BOON"));var receipt=rolled.Guild.GuildCity.Expedition.PendingQuestFate165;
            foreach(var field in new[]{"D20","CampaignGuid","RouteHash","CardId"})
            {
                var json=JObject.Parse(CanonicalJson.Serialize(rolled));var pending=(JObject)json["Guild"]["GuildCity"]["Expedition"]["PendingQuestFate165"];
                pending[field]=field=="D20"?new JValue(21):new JValue("FOREIGN165");
                var changed=json.ToObject<CampaignState>();
                Assert.That(service.DescribeQuestFate165(changed,Content),Is.Null);
                Assert.That(service.CollectQuestFate165(changed,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void EveryNativeWheelSectorAppliesItsExactRewardAndKeepsAuthoredStoryBattle(int sector)
        {
            CampaignState rolled=null;
            for(var seed=165000;seed<165128;seed++)
            {
                var trial=Roll(Seal(Begin(GuildCityExpeditionService017D.FirstStoryContractId066,seed),"FATE"));
                if(trial.Guild.GuildCity.Expedition.PendingQuestFate165.WheelSector==sector){rolled=trial;break;}
            }
            Assert.That(rolled,Is.Not.Null,"Bounded native seeds must cover all sectors.");
            var card=service.DescribeQuestFate165(rolled,Content);var receipt=rolled.Guild.GuildCity.Expedition.PendingQuestFate165;
            var baseCity=rolled.Guild.GuildCity.With(expedition:rolled.Guild.GuildCity.Expedition.With(pendingQuestFate165:null,replacePendingQuestFate165:true),replaceExpedition:true);
            var nativeMove=Require(service.CommitMove(rolled.With(rolled.Guild.WithGuildCity(baseCity),rolled.OpeningFlow),Content,card.DestinationNodeId));
            var result=Collect(Reload(rolled));
            Assert.That(result.Guild.TreasuryXp-nativeMove.Guild.TreasuryXp,Is.EqualTo(sector==0?20:0));
            Assert.That(result.Guild.Inventory.Count-nativeMove.Guild.Inventory.Count,Is.EqualTo(sector==2?1:0));
            Assert.That(result.Guild.GuildCity.Expedition.Supplies-nativeMove.Guild.GuildCity.Expedition.Supplies,Is.EqualTo(sector==1?3:0));
            Assert.That(result.Guild.GuildCity.Expedition.CurrentNodeId,Is.EqualTo("N01"));
            Assert.That(GuildCityExpeditionService017D.IsEncounterCleared(result.Guild.GuildCity.Expedition,Content.Board(result.Guild.GuildCity.Expedition.BoardId).Node("N01")),Is.False);
        }

        [Test]
        public void NaturalTwentyUsesExistingHeroBoonAuthorityAndCappedTargetStillCanContinue()
        {
            CampaignState rolled=null;
            for(var seed=165000;seed<165256;seed++)
            {
                var trial=Roll(Seal(Begin(GuildCityExpeditionService017D.FirstStoryContractId066,seed),"BOON"));
                if(trial.Guild.GuildCity.Expedition.PendingQuestFate165.D20==20){rolled=trial;break;}
            }
            Assert.That(rolled,Is.Not.Null);var receipt=rolled.Guild.GuildCity.Expedition.PendingQuestFate165;
            var result=Collect(Reload(rolled));
            var hero=result.Guild.Recruits.Single(x=>x.RecruitId==receipt.TargetRecruitId);
            Assert.That(hero.Progression.UnlockedTreeIds,Does.Contain(ExpeditionDeckService089.PermanentBoonPrefix089+"NATURAL20_165_"+receipt.ReceiptId));
            // Controlled current-roster mutation models another independent loop
            // granting the target's two blessings while this saved result waits.
            var recruits=rolled.Guild.Recruits.Select(x=>x.RecruitId!=receipt.TargetRecruitId?x:
                x.WithProgression(x.Progression.WithUnlockedTrees(new[]{ExpeditionDeckService089.PermanentBoonPrefix089+"A",ExpeditionDeckService089.PermanentBoonPrefix089+"B"}))).ToArray();
            var capped=Reload(rolled.With(rolled.Guild.With(rolled.Guild.TreasuryXp,recruits,rolled.Guild.Unions,rolled.Guild.Inventory,rolled.Guild.Development),rolled.OpeningFlow));
            Assert.That(service.DescribeQuestFate165(capped,Content).CheckModifierDelta,Is.EqualTo(2));
            Assert.That(CanonicalJson.Serialize(capped.Guild.GuildCity.Expedition.PendingQuestFate165),Is.EqualTo(CanonicalJson.Serialize(receipt)));
            var fallback=Collect(capped);
            Assert.That(GuildCityExpeditionService017D.QuestCardRunCheckModifier090(fallback.Guild.GuildCity.Expedition.ObjectiveFlags),Is.EqualTo(2));
            Assert.That(fallback.Guild.Recruits.Single(x=>x.RecruitId==receipt.TargetRecruitId).Progression.UnlockedTreeIds.Count,Is.EqualTo(2));
            Assert.That(GuildCityExpeditionService017D.HeroCheckBoon165(result,receipt.TargetRecruitId),Is.EqualTo(1));
            Assert.That(GuildCityExpeditionService017D.HeroCheckBoon165(capped,receipt.TargetRecruitId),Is.EqualTo(2));
            // Use that natively earned hero blessing in a controlled fresh
            // opening-check cohort; both branches commit the same authored roll.
            var ordinary=Fresh();var blessed=ordinary.With(ordinary.Guild.With(ordinary.Guild.TreasuryXp,
                result.Guild.Recruits,ordinary.Guild.Unions,ordinary.Guild.Inventory,ordinary.Guild.Development),ordinary.OpeningFlow);
            ordinary=Require(service.StartExpedition(Require(service.AcceptContract(ordinary,Content,
                GuildCityExpeditionService017D.SecondStoryContractId076)),Content));
            blessed=Require(service.StartExpedition(Require(service.AcceptContract(blessed,Content,
                GuildCityExpeditionService017D.SecondStoryContractId076)),Content));
            var ordinaryCheck=Require(service.ResolveCommittedCheck(ordinary,Content,"EVENT_FOUND_APPRENTICE",receipt.TargetRecruitId,null,2))
                .Guild.GuildCity.Expedition.CommittedChecks.Last();
            var blessedCheck=Require(service.ResolveCommittedCheck(blessed,Content,"EVENT_FOUND_APPRENTICE",receipt.TargetRecruitId,null,2))
                .Guild.GuildCity.Expedition.CommittedChecks.Last();
            Assert.That(blessedCheck.DieOne,Is.EqualTo(ordinaryCheck.DieOne));
            Assert.That(blessedCheck.DieTwo,Is.EqualTo(ordinaryCheck.DieTwo));
            Assert.That(blessedCheck.Modifier,Is.EqualTo(ordinaryCheck.Modifier+1),"Permanent blessing stacks with the same temporary/crew modifier.");
        }

        [Test]
        public void EveryAuthoredLaterBoardUsesNativeFateAtEveryGuildDeckTier()
        {
            var path=Environment.GetEnvironmentVariable("SD_FATE165_BOARDS");
            var boards=JObject.Parse(File.ReadAllText(path))["boards"].ToObject<WorldGateBoardRule023[]>();
            var service089=new ExpeditionDeckService089();var sample=Fresh();var count=0;
            foreach(var board in boards)
            foreach(var level in new[]{1,3,6,10})
            {
                var deck=service089.Create(165000,"CATALOG165_"+board.DefinitionId+"_"+level,board,sample.Guild.Recruits,
                    sample.Guild.Inventory,null,Array.Empty<string>(),guildLevel:level);
                var cards=deck.CurrentRow.Concat(deck.DrawPile).Concat(deck.DiscardPile).ToArray();
                var dice=cards.Count(x=>ExpeditionDeckService089.EffectKind132(x)==ExpeditionDeckService089.BoonD20132||
                    ExpeditionDeckService089.EffectKind132(x)==ExpeditionDeckService089.CurseD20132||ExpeditionDeckService089.CanOfferRouteFate132(x));
                var wheel=cards.Count(x=>ExpeditionDeckService089.EffectKind132(x)==ExpeditionDeckService089.Wheel132);
                Assert.That(dice,Is.GreaterThan(0),board.DefinitionId+" level "+level+" D20");
                Assert.That(wheel,Is.GreaterThan(0),board.DefinitionId+" level "+level+" Wheel");
                count++;
            }
            Console.WriteLine("CATALOG_FATE165 "+boards.Length+" boards / "+count+" native generated decks");
        }

        [Test]
        public void AddedOwnedUnionSurvivesParkReloadAndEntersNextOpeningBattle()
        {
            var first=Begin(GuildCityExpeditionService017D.FirstStoryContractId066);
            var commands=new M1CommandService();
            var originalRoster=CanonicalJson.Serialize(first.Guild.Unions);
            var originalRoute=CanonicalJson.Serialize(first.Guild.GuildCity.Expedition);
            var town=Reload(Require(LoopCheckpoint164.Switch(first,LoopCheckpoint164.Town)));
            var added=Reload(Require(commands.AddOpeningUnion(town)));
            Assert.That(added.Guild.Unions.Count,Is.EqualTo(2));
            Assert.That(UnionBattlePlanRules132.Read(added).Count,Is.EqualTo(3));
            var newId=UnionBattlePlanRules132.Read(added)[2].UnionId;
            added=Require(commands.AssignRecruitToUnion(added,"R7",2,0));
            added=Require(commands.AssignRecruitToUnion(added,"R8",2,1));
            added=Require(commands.AssignRecruitToUnion(added,"R9",2,2));
            added=Reload(added);
            Assert.That(service.DescribeQuestFate165(added,Content),Is.Null);
            var resumed=Reload(Require(LoopCheckpoint164.Switch(added,LoopCheckpoint164.Campaign)));
            Assert.That(CanonicalJson.Serialize(resumed.Guild.Unions),Is.EqualTo(originalRoster));
            Assert.That(CanonicalJson.Serialize(resumed.Guild.GuildCity.Expedition),Is.EqualTo(originalRoute));
            Assert.That(resumed.NextBattleUnions132.Unions[2].MemberRecruitIds,Is.EqualTo(new[]{"R7","R8","R9"}));
            var card=service.BuildQuestCardRow090(resumed,Content).Single(x=>x.Category=="CHEST");
            resumed=Require(service.CommitQuestCard090(resumed,Content,null,card.CardId));
            var request=Reload(Require(service.CommitEncounter(resumed,Content,GuildCityExpeditionService017D.FirstHourHallBreachEncounterId071)));
            Assert.That(request.Guild.GuildCity.PendingEncounter.AlliedUnionIds,Does.Contain(newId));
            var combat=M2CombatContent.LoadFromDirectory(Environment.GetEnvironmentVariable("SD_FATE165_CONTENT"));
            var started=Require(new GuildCityBattleBridgeService017D().StartCertifiedEncounter(request,new M2BattleCommandService(),combat));
            Assert.That(started.Battle.PlayerUnions.Any(x=>x.UnionId==newId&&x.Members.Count==3),Is.True);
            var committedBattle=CanonicalJson.Serialize(started.Battle);
            Assert.That(commands.AddOpeningUnion(started).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Serialize(started.Battle),Is.EqualTo(committedBattle));
        }

        [Test]
        public void QuestThreeCanAddAssignAndRemoveDraftsWithoutReplacingParkedRoute()
        {
            var active=Begin("CONTRACT_RELIEF_ROAD");var commands=new M1CommandService();
            var route=CanonicalJson.Serialize(active.Guild.GuildCity.Expedition);
            var changed=Require(commands.AddOpeningUnion(active));
            changed=Require(commands.AssignRecruitToUnion(changed,"R7",2,0));
            Assert.That(changed.NextBattleUnions132.Unions.Count,Is.EqualTo(3));
            Assert.That(CanonicalJson.Serialize(changed.Guild.GuildCity.Expedition),Is.EqualTo(route));
            Assert.That(commands.RemoveOpeningUnion(changed,2).IsSuccess,Is.False,"Filled Union cannot be removed.");
            changed=Require(commands.AddOpeningUnion(changed));
            changed=Reload(Require(commands.RemoveOpeningUnion(changed,3)));
            Assert.That(changed.NextBattleUnions132.Unions.Count,Is.EqualTo(3));
            var plans=changed.NextBattleUnions132.Unions.Concat(new[]{changed.NextBattleUnions132.Unions[0]}).ToArray();
            Assert.Throws<InvalidOperationException>(()=>new UnionBattlePlan132(changed.NextBattleUnions132.SourceRosterHash,plans).Validate(changed.Guild));
            while(UnionBattlePlanRules132.Read(changed).Count<10)changed=Require(commands.AddOpeningUnion(changed));
            Assert.That(commands.AddOpeningUnion(changed).IsSuccess,Is.False,"Native ten-Union cap remains.");
            Assert.That(CanonicalJson.Serialize(Reload(changed).Guild.Unions),Is.EqualTo(CanonicalJson.Serialize(active.Guild.Unions)));
        }

        CampaignState BuyAndActivateLuck(CampaignState source)
        {
            var offer=TownService153.OfferId165(source,0,0);
            var quote=TownService153.QuotePurchase(source,offer);
            Assert.That(quote.IsSuccess,Is.True,string.Join(";",quote.Errors));
            var bought=Require(TownService153.ConfirmPurchase(source,quote.Value));
            Assert.That(bought.Guild.TreasuryXp,Is.EqualTo(source.Guild.TreasuryXp-quote.Value.Cost));
            var item=bought.Guild.Inventory.Single(TownLuckConsumables165.IsLuckItem);
            var activated=Reload(Require(TownLuckConsumables165.Activate(bought,item.InstanceId,CanonicalJson.Sha256Hex(bought))));
            Assert.That(activated.Guild.Inventory.Any(x=>x.InstanceId==item.InstanceId),Is.False);
            Assert.That(TownLuckConsumables165.ActiveChargeId(activated),Is.Not.Empty);
            return activated;
        }

        [Test]
        public void PurchasedLuckIsConsumedByOneNewBlessingAndKeepsHigherNativeD20AcrossReload()
        {
            bool improved=false;
            for(int seed=165000;seed<165012;seed++)
            {
                var original=Fresh(seed);var lucky=BuyAndActivateLuck(original);
                var charge=TownLuckConsumables165.ActiveChargeId(lucky);
                var ordinaryRoll=Roll(Seal(BeginFrom(original,GuildCityExpeditionService017D.FirstStoryContractId066),"BOON"));
                var seal=Reload(Seal(BeginFrom(lucky,GuildCityExpeditionService017D.FirstStoryContractId066),"BOON"));
                var receipt=seal.Guild.GuildCity.Expedition.PendingQuestFate165;
                Assert.That(receipt.LuckChargeId165,Is.EqualTo(charge));
                Assert.That(TownLuckConsumables165.ActiveChargeId(seal),Is.Empty);
                Assert.That(TownLuckConsumables165.WasConsumedFor(seal,charge,receipt.ReceiptId),Is.True);
                Assert.That(receipt.IsRolled165,Is.False);
                var rolled=Reload(Roll(seal));var die=rolled.Guild.GuildCity.Expedition.PendingQuestFate165.D20;
                var baseline=ordinaryRoll.Guild.GuildCity.Expedition.PendingQuestFate165.D20;
                Assert.That(die,Is.InRange(baseline,20));
                Assert.That(service.DescribeQuestFate165(rolled,Content).Lucky165,Is.True);
                Assert.That(service.RollQuestFate165(rolled,Content,null,receipt.ReceiptId).IsSuccess,Is.False);
                Assert.That(Collect(rolled).Guild.Development.HasAdventureAuthority(receipt.ReceiptId),Is.True);
                improved|=die>baseline;
            }
            Assert.That(improved,Is.True,"The second native die must be able to improve the result.");
        }

        [Test]
        public void BuyingLuckAfterSealingCannotChangeOrConsumeTheExistingSavedResult()
        {
            var seal=Reload(Seal(Begin(GuildCityExpeditionService017D.FirstStoryContractId066),"BOON"));
            var original=CanonicalJson.Serialize(seal.Guild.GuildCity.Expedition.PendingQuestFate165);
            var expected=Roll(seal).Guild.GuildCity.Expedition.PendingQuestFate165.D20;
            var town=Require(LoopCheckpoint164.Switch(seal,LoopCheckpoint164.Town));
            town=BuyAndActivateLuck(town);
            var charge=TownLuckConsumables165.ActiveChargeId(town);
            var returned=Reload(Require(LoopCheckpoint164.Switch(town,LoopCheckpoint164.Campaign)));
            Assert.That(CanonicalJson.Serialize(returned.Guild.GuildCity.Expedition.PendingQuestFate165),Is.EqualTo(original));
            var actual=Roll(returned);
            Assert.That(actual.Guild.GuildCity.Expedition.PendingQuestFate165.D20,Is.EqualTo(expected));
            Assert.That(actual.Guild.GuildCity.Expedition.PendingQuestFate165.LuckChargeId165,Is.Null);
            Assert.That(TownLuckConsumables165.ActiveChargeId(actual),Is.EqualTo(charge));
        }

        [Test]
        public void NativeHexRetainsItsXpFatigueAndThreatOnlyWhenCollected()
        {
            var state=Begin("CONTRACT_RELIEF_ROAD");
            foreach(var category in new[]{"CHEST","MERCHANT"})
            {
                var move=service.BuildQuestCardRow090(state,Content).Single(card=>card.Category==category);
                state=Require(service.CommitQuestCard090(state,Content,null,move.CardId));
            }
            var node=Content.Board(state.Guild.GuildCity.Expedition.BoardId).Node(state.Guild.GuildCity.Expedition.CurrentNodeId);
            if(!string.IsNullOrEmpty(node.EventId))
                state=Require(service.ResolveCommittedCheck(state,Content,node.EventId,"R1","R2",0));
            var card=service.BuildQuestCardRow090(state,Content).Single(value=>value.Category=="SCAR");
            var nativeMove=Require(service.CommitMove(state,Content,card.DestinationNodeId));
            var sealedState=Reload(Seal(state,"SCAR"));var rolled=Reload(Roll(sealedState));
            Assert.That(rolled.Guild.TreasuryXp,Is.EqualTo(state.Guild.TreasuryXp));
            Assert.That(rolled.Guild.GuildCity.Expedition.Fatigue,Is.EqualTo(state.Guild.GuildCity.Expedition.Fatigue));
            Assert.That(rolled.Guild.GuildCity.Expedition.Threat,Is.EqualTo(state.Guild.GuildCity.Expedition.Threat));
            var outcome=service.DescribeQuestFate165(rolled,Content);var after=Reload(Collect(rolled));
            Assert.That(after.Guild.TreasuryXp,Is.EqualTo(nativeMove.Guild.TreasuryXp+24));
            Assert.That(after.Guild.GuildCity.Expedition.Fatigue,Is.EqualTo(nativeMove.Guild.GuildCity.Expedition.Fatigue+2));
            Assert.That(after.Guild.GuildCity.Expedition.Threat,Is.EqualTo(nativeMove.Guild.GuildCity.Expedition.Threat+1));
            Assert.That(GuildCityExpeditionService017D.QuestCardRunCheckModifier090(after.Guild.GuildCity.Expedition.ObjectiveFlags),Is.EqualTo(outcome.D20165<10?-2:-1));
            Assert.That(service.CollectQuestFate165(after,Content,null,outcome.ReceiptId165).IsSuccess,Is.False);
        }
    }
}
