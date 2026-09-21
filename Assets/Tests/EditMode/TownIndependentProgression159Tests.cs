using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TownIndependentProgression159Tests
    {
        GuildCityContent017D Content()=>GuildCityContent017D.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT","GUILD_CITY_017D"));
        static CampaignState State(long wallet=1000000)
        {
            var recruits=new[]{new RecruitState("R1",100,100,20,20)};
            var guild=new GuildState("TOWN159_TEST",wallet,recruits,Array.Empty<UnionState>());
            guild=guild.WithGuildCity(guild.GuildCity.With(charterBuildCredits:0,materials:Array.Empty<GuildMaterialState017D>()));
            var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete");
            return new CampaignState("00000000-0000-0000-0000-000000159159",159159,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,
                new NewGuildProfileState("Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),flow);
        }
        static CampaignState Require(Result<CampaignState> result)
        {Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors));return result.Value;}

        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)][TestCase(6)][TestCase(7)]
        public void EveryFacilityCanReachElevenWithoutAnotherLoopOrAnotherFacility(int index)
        {
            var state=State();var content=Content();var hall=state.Guild.Development.HallEnhancementXp;
            var materials=CanonicalJson.Sha256Hex(state.Guild.GuildCity.Materials);
            for(int target=1;target<=11;target++)
            {
                var before=CanonicalJson.Sha256Hex(state);var wallet=state.Guild.TreasuryXp;
                var quote=TownService153.QuoteBuilding(state,content,TownService153.FacilityIds[index]);
                Assert.That(quote.IsSuccess&&quote.Value.Affordable,Is.True, string.Join(";",quote.Errors));
                Assert.That(CanonicalJson.Sha256Hex(state),Is.EqualTo(before),"Preview mutated state");
                state=Require(TownService153.ConfirmBuilding(state,content,quote.Value));
                Assert.That(TownProgression159.Level(state,index),Is.EqualTo(target));
                Assert.That(state.Guild.TreasuryXp,Is.EqualTo(wallet-TownProgression159.Cost(index,target)));
                Assert.That(state.Guild.Development.HallEnhancementXp,Is.EqualTo(hall));
                Assert.That(CanonicalJson.Sha256Hex(state.Guild.GuildCity.Materials),Is.EqualTo(materials));
                Assert.That(state.Guild.GuildCity.OperationOrdinal,Is.EqualTo(0));
                for(int other=0;other<8;other++)if(other!=index)Assert.That(TownProgression159.Level(state,other),Is.EqualTo(0));
                var once=CanonicalJson.Sha256Hex(state);
                state=Require(TownService153.ConfirmBuilding(state,content,quote.Value));
                Assert.That(CanonicalJson.Sha256Hex(state),Is.EqualTo(once),"Duplicate charged twice");
            }
            var loaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(state));
            Assert.That(TownProgression159.Level(loaded,index),Is.EqualTo(11));
            Assert.That(loaded.Guild.TreasuryXp,Is.EqualTo(state.Guild.TreasuryXp));
        }

        [Test] public void BuildingFailureAndStaleQuoteNeverSpend()
        {
            var state=State(0);var content=Content();var hash=CanonicalJson.Sha256Hex(state);
            var poor=TownService153.QuoteBuilding(state,content,"GUILD_HALL");
            Assert.That(poor.IsSuccess&&!poor.Value.Affordable,Is.True);
            Assert.That(TownService153.ConfirmBuilding(state,content,poor.Value).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Sha256Hex(state),Is.EqualTo(hash));
            state=State();var quote=TownService153.QuoteBuilding(state,content,"GUILD_HALL").Value;
            var purchase=TownService153.QuotePurchase(state,TownService153.OfferId(state,0,0)).Value;
            state=Require(TownService153.ConfirmPurchase(state,purchase));hash=CanonicalJson.Sha256Hex(state);
            Assert.That(TownService153.ConfirmBuilding(state,content,quote).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Sha256Hex(state),Is.EqualTo(hash));
        }

        [Test] public void MerchantRestockNeedsNoConstructionOrAdventureAndRetainsOwnedItems()
        {
            var state=State();var offer=TownService153.OfferId(state,0,0);
            Assert.That(offer,Is.EqualTo("TOWN153_0_0_0"));
            var purchase=TownService153.QuotePurchase(state,offer);
            Assert.That(purchase.IsSuccess,Is.True);
            state=Require(TownService153.ConfirmPurchase(state,purchase.Value));
            var item=purchase.Value.Item.InstanceId;var wallet=state.Guild.TreasuryXp;
            var quote=TownService153.QuoteRestock159(state).Value;
            state=Require(TownService153.ConfirmRestock159(state,quote));
            Assert.That(state.Guild.TreasuryXp,Is.EqualTo(wallet-25));
            Assert.That(state.Guild.Inventory.Any(i=>i.InstanceId==item),Is.True);
            Assert.That(TownService153.OfferId(state,0,0),Is.EqualTo("TOWN153_0_0_0_R1"));
            Assert.That(state.Guild.GuildCity.OperationOrdinal,Is.EqualTo(0));
            Assert.That(TownService153.QuotePurchase(state,offer).IsSuccess,Is.False);
            var hash=CanonicalJson.Sha256Hex(state);
            Assert.That(CanonicalJson.Sha256Hex(Require(TownService153.ConfirmRestock159(state,quote))),Is.EqualTo(hash));
            var loaded=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(state));
            Assert.That(TownService153.RestockGeneration159(loaded),Is.EqualTo(1));
            Assert.That(loaded.Guild.Inventory.Any(i=>i.InstanceId==item),Is.True);
            Assert.That(CanonicalJson.Sha256Hex(Require(TownService153.ConfirmPurchase(loaded,purchase.Value))),Is.EqualTo(hash));
        }

        [Test] public void PreviousFacilityInvestmentIsPreservedAndHallXpLevelIsSeparate()
        {
            var state=State();var g=state.Guild;
            state=state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,g.Development.SetFacilityLevel("FACILITY_GUILD_FORGE",4,150)),state.OpeningFlow);
            Assert.That(TownProgression159.Level(state,2),Is.EqualTo(4));
            Assert.That(TownProgression159.Level(state,0),Is.EqualTo(0));
            var q=TownService153.QuoteBuilding(state,Content(),"FORGE").Value;
            state=Require(TownService153.ConfirmBuilding(state,Content(),q));
            Assert.That(TownProgression159.Level(state,2),Is.EqualTo(5));
            var native=state.Guild.Development.Facilities.Single(f=>f.FacilityId=="FACILITY_GUILD_FORGE");
            Assert.That(native.Level,Is.EqualTo(4));Assert.That(native.TotalFacilityXp,Is.EqualTo(150));
        }

        [Test] public void DiscountsAndPostTenTariffsAreExactAtNumericBoundaries()
        {
            Assert.That(TownProgression159.DiscountAtLevel(0,10),Is.EqualTo(0));
            Assert.That(TownProgression159.DiscountAtLevel(140,10),Is.EqualTo(128));
            Assert.That(TownProgression159.DiscountAtLevel(1,int.MaxValue),Is.EqualTo(1));
            Assert.That(TownProgression159.DiscountAtLevel(long.MaxValue,0),Is.EqualTo(long.MaxValue));
            Assert.That(TownProgression159.Cost(0,10),Is.EqualTo(21760));
            Assert.That(TownProgression159.Cost(0,11),Is.EqualTo(21760));
            Assert.That(TownProgression159.Cost(0,12),Is.EqualTo(21978));
            Assert.That(TownProgression159.Cost(0,int.MaxValue),Is.GreaterThan(TownProgression159.Cost(0,12)));
        }

        [Test] public void NativeUpgradesAfterPaidTownInvestmentSkipAlreadyEarnedLevels()
        {
            var state=State();var content=Content();
            state=Require(TownService153.ConfirmBuilding(state,content,TownService153.QuoteBuilding(state,content,"FORGE").Value));
            Assert.That(TownProgression159.Level(state,2),Is.EqualTo(1));
            var g=state.Guild;
            var development=g.Development.RecordBattleReward("TOWN159_NATIVE_UPGRADE_FIXTURE",1,1000);
            var city=g.GuildCity.With(materials:new[]{new GuildMaterialState017D("MAT_SALVAGED_TIMBER",100),new GuildMaterialState017D("MAT_GATE_IRON",100)});
            state=state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,development).WithGuildCity(city),state.OpeningFlow);
            var plot=state.Guild.GuildCity.CityPlots.Single(p=>p.BuildingId=="GC017D_BUILD_FORGE").PlotId;
            var command=new GuildCityCommandService017D();
            for(int level=2;level<=4;level++)state=Require(command.UpgradeBuilding(state,content,plot));
            Assert.That(state.Guild.Development.Facilities.Single(f=>f.FacilityId=="CITYTAB_FORGE").Level,Is.EqualTo(1));
            Assert.That(TownProgression159.Level(state,2),Is.EqualTo(4));
            var nativeBefore=state.Guild.Development.Facilities.Single(f=>f.FacilityId=="FACILITY_GUILD_FORGE");
            var wallet=state.Guild.TreasuryXp;var hall=state.Guild.Development.HallEnhancementXp;
            var quote=TownService153.QuoteBuilding(state,content,"FORGE");
            Assert.That(quote.IsSuccess&&quote.Value.Affordable,Is.True);
            Assert.That(quote.Value.Summary,Does.Contain("Level 4 → 5"));
            state=Require(TownService153.ConfirmBuilding(state,content,quote.Value));
            Assert.That(TownProgression159.Level(state,2),Is.EqualTo(5));
            Assert.That(state.Guild.TreasuryXp,Is.EqualTo(wallet-TownProgression159.Cost(2,5)));
            Assert.That(state.Guild.Development.HallEnhancementXp,Is.EqualTo(hall));
            var nativeAfter=state.Guild.Development.Facilities.Single(f=>f.FacilityId=="FACILITY_GUILD_FORGE");
            Assert.That(nativeAfter.Level,Is.EqualTo(nativeBefore.Level));
            Assert.That(nativeAfter.TotalFacilityXp,Is.EqualTo(nativeBefore.TotalFacilityXp));
            Assert.That(state.Guild.GuildCity.CityPlots.Single(p=>p.PlotId==plot).BuildingLevel,Is.EqualTo(4));
        }

        [Test] public void MalformedOrOtherProfileRestockReceiptsDoNotAdvanceTheShelf()
        {
            var state=State();var request=TownService153.QuoteRestock159(state).Value.RequestId;
            var prefix=request.Substring(0,request.Length-1);
            var g=state.Guild;var d=g.Development;
            foreach(var tail in new[]{"0009","+9"," 9","9 ","-1","2147483648","1_extra",""})d=d.RecordAdventureAuthority(prefix+tail);
            d=d.RecordAdventureAuthority("TOWN_RESTOCK159_OTHER_PROFILE_999");
            state=state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,d),state.OpeningFlow);
            Assert.That(TownService153.RestockGeneration159(state),Is.EqualTo(0));
            Assert.That(TownService153.OfferId(state,0,0),Is.EqualTo("TOWN153_0_0_0"));
            var quote=TownService153.QuoteRestock159(state).Value;
            Assert.That(quote.Generation,Is.EqualTo(1));
            var next=Require(TownService153.ConfirmRestock159(state,quote));
            Assert.That(TownService153.RestockGeneration159(next),Is.EqualTo(1));
            Assert.That(TownService153.RestockGeneration159(state),Is.EqualTo(0),"Immutable snapshot cache leaked across states");
        }

        [Test] public void VeryHighInfirmaryCannotOverflowEitherRecoverySettlementPath()
        {
            var state=State();var g=state.Guild;
            var development=g.Development.SetFacilityLevel("CITYTAB_INFIRMARY",int.MaxValue,0);
            var city=g.GuildCity.With(memberAssignments:new[]{new GuildMemberAssignmentState017D("R1",GuildMemberAssignmentKind017D.Recovering,"",int.MaxValue-2,0,0)});
            state=state.With(g.With(g.TreasuryXp,g.Recruits,g.Unions,g.Inventory,development).WithGuildCity(city),state.OpeningFlow);
            var content=Content();var effects=new GuildCityEffectService017D();
            Assert.That(effects.RecoveryProgressPerOperation(development,city,content),Is.EqualTo(int.MaxValue));
            var completed=Require(new GuildCityCommandService017D().CompleteMeaningfulOperation(state,content));
            Assert.That(completed.Guild.GuildCity.MemberAssignments.Single().RecoveryProgress,Is.EqualTo(int.MaxValue));
            var method=typeof(GuildCityExpeditionService017D).GetMethod("AdvanceAssignments",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null);
            var settled=(System.Collections.Generic.IReadOnlyList<GuildMemberAssignmentState017D>)method.Invoke(null,new object[]{city.MemberAssignments,development,city,content,effects});
            Assert.That(settled.Single().RecoveryProgress,Is.EqualTo(int.MaxValue));
        }
    }
}
