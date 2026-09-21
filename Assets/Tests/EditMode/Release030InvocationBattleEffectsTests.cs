using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class Release030InvocationBattleEffectsTests
    {
        CampaignRegistry022 _registry;CampaignProgressionCommandService022 _service;M2BattleCommandService _battles;M2CombatContent _content;
        [SetUp] public void SetUp(){_registry=CampaignRegistry022.LoadFromResources();_service=new CampaignProgressionCommandService022();_battles=new M2BattleCommandService();_content=M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"));}

        [Test]
        public void EchoProducesPositiveRealWardingEffectAndExactReceipt()
        {
            var selected=SelectRound(CreateEchoCampaign(),"CMD_GUARD",out _);
            var first=Require(_service.InvokeEligibleEchoForecast(selected,_registry,_battles,_content));
            var replay=Require(_service.InvokeEligibleEchoForecast(selected,_registry,_battles,_content));
            Assert.AreEqual(SecondDimension.Determinism.CanonicalJson.Serialize(first),SecondDimension.Determinism.CanonicalJson.Serialize(replay));
            var state=Progression(first);
            var receipt=state.AppliedReceiptIds.Single(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal));
            var events=first.Battle.EventLog.Where(x=>x.EventType=="SUMMON_ECHO_INVOKED").ToArray();
            Assert.AreEqual(1,events.Length);
            Assert.Greater(events[0].Amount,0);
            StringAssert.Contains(receipt,events[0].Text);
            Assert.AreEqual(1,state.AppliedReceiptIds.Count(x=>x==receipt));
            Assert.False(string.IsNullOrWhiteSpace(events[0].TargetUnionId));
            Assert.AreEqual(1,first.Battle.PlayerUnions.Count(x=>x.UnionId==events[0].TargetUnionId));
            // The positive invocation event is emitted only after the Warding mutation
            // succeeds. End-of-round totals are not a stable direct comparison because
            // the committed invocation intentionally changes the authoritative RNG basis
            // used by subsequent enemy actions.
        }

        [Test]
        public void TacticalEchoFallsBackToPositiveEngagementStatusWhenPressurePoolsAreEmpty()
        {
            var selected=SelectRound(CreateEchoCampaign(),"CMD_ALL_OUT",out _);var enemies=selected.Battle.EnemyUnions.Select(x=>x.With(cohesion:0,formationConditionBasisPoints:0,engagement:EngagementState.Open)).ToArray();selected=selected.WithBattle(selected.Battle.With(enemyUnions:enemies));var result=Require(_service.InvokeEligibleEchoForecast(selected,_registry,_battles,_content));var evidence=result.Battle.EventLog.Single(x=>x.EventType=="SUMMON_ECHO_INVOKED");Assert.Greater(evidence.Amount,0);var target=result.Battle.EnemyUnions.Single(x=>x.UnionId==evidence.TargetUnionId);Assert.That(target.Engagement,Is.EqualTo(EngagementState.Broken).Or.EqualTo(EngagementState.RearPressure));
        }

        [Test]
        public void EchoRejectsRepresentableThreeAffixArtifact()
        {
            AssertEchoRejected(CreateEchoCampaign(affixes:new[]{"INVOCATION_AFFIX022_01","INVOCATION_AFFIX022_02","INVOCATION_AFFIX022_03"}),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");
        }

        [Test]
        public void EchoRejectsIncompleteUnionForecastRound()
        {
            var campaign=Require(_battles.StartTutorialBattle(CreateEchoCampaign(),_content));var union=campaign.Battle.PlayerUnions.First(x=>x.UnionId=="ECHO_UNION_A");var forecast=campaign.Battle.CommittedForecasts.First(x=>x.UnionId==union.UnionId&&x.CommandId=="CMD_GUARD");campaign=Require(_battles.SelectForecast(campaign,union.UnionId,forecast.ForecastId));var result=_service.InvokeEligibleEchoForecast(campaign,_registry,_battles,_content);Assert.False(result.IsSuccess);CollectionAssert.Contains(result.Errors,"CAMPAIGN022_ECHO_SELECTED_COMPLETE_FORECAST_REQUIRED");
        }

        [Test]
        public void EchoRejectsForgedBaseAndPathLaws()
        {
            AssertEchoRejected(CreateEchoCampaign(itemBaseId:"INVOCATION_BASE022_01_01"),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");AssertEchoRejected(CreateEchoCampaign(pathId:"INVOCATION_PATH022_01"),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");var definition=_registry.ArtifactBases["INVOCATION_BASE022_02_01"];definition.maxActivePerEarlyUnion=2;try{AssertEchoRejected(CreateEchoCampaign(),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");}finally{definition.maxActivePerEarlyUnion=1;}
        }

        [Test]
        public void EchoRejectsForgedCategoryDuplicateOwnershipAndMultipleEquippedArtifacts()
        {
            var echo=_registry.Echoes["SUMMON_ECHO022_01"];var category=echo.forecastCategory;echo.forecastCategory="FORGED_CATEGORY";try{AssertEchoRejected(CreateEchoCampaign(),"CMD_GUARD","CAMPAIGN022_ECHO_FORECAST_CATEGORY_REQUIRED");}finally{echo.forecastCategory=category;}
            AssertEchoRejected(CreateEchoCampaign(duplicateInventory:true),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");AssertEchoRejected(CreateEchoCampaign(multipleEquipped:true),"CMD_GUARD","CAMPAIGN022_ECHO_EQUIPPED_INVOCATION_ARTIFACT_REQUIRED");
        }

        void AssertEchoRejected(CampaignState campaign,string commandId,string error)
        {
            var selected=SelectRound(campaign,commandId,out _);var before=SecondDimension.Determinism.CanonicalJson.Serialize(selected);var result=_service.InvokeEligibleEchoForecast(selected,_registry,_battles,_content);Assert.False(result.IsSuccess);CollectionAssert.Contains(result.Errors,error);Assert.AreEqual(before,SecondDimension.Determinism.CanonicalJson.Serialize(selected));Assert.Zero(Progression(selected).AppliedReceiptIds.Count(x=>x.StartsWith("ECHOREC022_",StringComparison.Ordinal)));
        }

        CampaignState SelectRound(CampaignState campaign,string commandId,out BattleForecastState target)
        {
            campaign=Require(_battles.StartTutorialBattle(campaign,_content));target=campaign.Battle.CommittedForecasts.First(x=>x.UnionId=="ECHO_UNION_A"&&x.CommandId==commandId);foreach(var union in campaign.Battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated)){var forecast=union.UnionId==target.UnionId?target:campaign.Battle.CommittedForecasts.First(x=>x.UnionId==union.UnionId&&x.CommandId=="CMD_BALANCED");campaign=Require(_battles.SelectForecast(campaign,union.UnionId,forecast.ForecastId));}return campaign;
        }

        static CampaignState CreateEchoCampaign(string itemBaseId="INVOCATION_BASE022_02_01",string pathId="INVOCATION_PATH022_02",IReadOnlyList<string> affixes=null,bool duplicateInventory=false,bool multipleEquipped=false)
        {
            const string artifactId="INVOCATION022_ECHO_AUTHORITY_A";var tool=new EquipmentItemState(artifactId,itemBaseId,"Echo Authority Relic",new[]{EquipmentSlotIds.ToolRelic},new[]{"INVOCATION_ARTIFACT","WEAPON_FAMILY_GREAT_WEAPON"},"UNCOMMON",10000,false);var mainA=new EquipmentItemState("ECHO_MAIN_A","ECHO_SHIELD_A","Echo Shield",new[]{EquipmentSlotIds.MainHand},new[]{"SHIELD","SWORD","WEAPON"},"QUALITY_STANDARD",10000,false);var equipmentA=new EquipmentLoadoutState(new[]{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,mainA),new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic,tool)});
            EquipmentItemState secondTool=null;var assignmentsB=new List<EquipmentSlotAssignmentState>{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,new EquipmentItemState("ECHO_MAIN_B","ECHO_BOW_B","Echo Bow",new[]{EquipmentSlotIds.MainHand},new[]{"BOW","WEAPON"},"QUALITY_STANDARD",10000,false))};if(multipleEquipped){secondTool=new EquipmentItemState("INVOCATION022_ECHO_AUTHORITY_B","INVOCATION_BASE022_01_01","Second Echo Relic",new[]{EquipmentSlotIds.ToolRelic},new[]{"INVOCATION_ARTIFACT","WEAPON_FAMILY_SWORD"},"UNCOMMON",10000,false);assignmentsB.Add(new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic,secondTool));}
            var recruitA=new RecruitState("ECHO_RECRUIT_A",160,160,45,45,"Echo Warden",RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME","CLASS_TEND_GUARDIAN","Observed",7500,RecruitAuthorityKind.Normal,string.Empty,string.Empty,equipmentA,true,string.Empty,string.Empty,65,65);var recruitB=new RecruitState("ECHO_RECRUIT_B",150,150,40,40,"Echo Archer",RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME","CLASS_TEND_RANGER","Observed",7200,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(assignmentsB),true,string.Empty,string.Empty,62,62);var recruitC=new RecruitState("ECHO_RECRUIT_C",150,150,40,40,"Echo Scout",RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME","CLASS_TEND_RANGER","Observed",7200,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(new[]{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,new EquipmentItemState("ECHO_MAIN_C","ECHO_BOW_C","Echo Bow",new[]{EquipmentSlotIds.MainHand},new[]{"BOW","WEAPON"},"QUALITY_STANDARD",10000,false))}),true,string.Empty,string.Empty,62,62);
            var unions=new[]{new UnionState("ECHO_UNION_A","Echo Authority Union",UnionKind.Normal,recruitA.RecruitId,new[]{recruitA.RecruitId,recruitB.RecruitId},"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",24,8500),new UnionState("ECHO_UNION_B","Echo Flank Union",UnionKind.Normal,recruitC.RecruitId,new[]{recruitC.RecruitId},"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",20,8500)};var inventory=duplicateInventory?new[]{tool}:Array.Empty<EquipmentItemState>();var guild=new GuildState("GUILD_ECHO_AUTHORITY_030",0,new[]{recruitA,recruitB,recruitC},unions,inventory);var profile=new NewGuildProfileState("Echo Authority",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"echo_authority_ready");var campaign=new CampaignState("00000000-0000-0000-0000-000000003030",3030,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);var artifacts=new List<InvocationArtifactState022>{new InvocationArtifactState022(artifactId,"INVOCATION_BASE022_02_01",affixes??Array.Empty<string>(),pathId,0,0,false)};if(secondTool!=null)artifacts.Add(new InvocationArtifactState022(secondTool.InstanceId,secondTool.DefinitionId,Array.Empty<string>(),"INVOCATION_PATH022_01",0,0,false));var floors=new[]{new AbyssFloorProgressState022("ABYSS_FLOOR_002_ARROW_RAIN_FIELD",1,1,true,false,Array.Empty<string>())};return WithProgression(campaign,CampaignProgressionState022.Default().With(abyssFloors:floors,invocationArtifacts:artifacts,lastCheckpointId:"echo_authority_ready"));
        }

        static CampaignProgressionState022 Progression(CampaignState c)=>c.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static CampaignState WithProgression(CampaignState campaign,CampaignProgressionState022 state){var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var progress=strategic.Campaign019;var playable=progress.Playable020.With(progression022:state,replaceProgression022:true,lastCheckpointId:state.LastCheckpointId);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:state.LastCheckpointId);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:state.LastCheckpointId);return campaign.With(campaign.Guild.WithGuildCity(city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:state.LastCheckpointId)),campaign.OpeningFlow);}
        static CampaignState Require(Result<CampaignState> result){Assert.True(result.IsSuccess,string.Join("\n",result.Errors));return result.Value;}
    }
}
