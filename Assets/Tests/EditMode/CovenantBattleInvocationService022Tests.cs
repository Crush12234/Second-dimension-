using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CovenantBattleInvocationService022Tests
    {
        CampaignRegistry022 _registry;CampaignProgressionCommandService022 _progression;CovenantBattleInvocationService022 _covenants;M2BattleCommandService _battles;M2CombatContent _content;

        [SetUp] public void SetUp(){_registry=CampaignRegistry022.LoadFromResources();_progression=new CampaignProgressionCommandService022();_covenants=new CovenantBattleInvocationService022();_battles=new M2BattleCommandService();_content=M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"));}

        [Test]
        public void AllEightAcceptedCovenantsExecuteTheirSevenRolesOnlyThroughCompleteForecasts()
        {
            var accepted=AcceptEveryCovenant();
            CollectionAssert.AreEquivalent(new[]{"COMBAT","GUARD","MYSTIC","RESTORATION","SUPPORT","TACTICAL","WARDING"},_registry.Covenants.Values.Select(x=>x.role).Distinct().ToArray());
            Assert.AreEqual(8,_registry.Covenants.Count);
            foreach(var definition in _registry.Covenants.Values.OrderBy(x=>x.covenantId,StringComparer.Ordinal))
            {
                var selected=SelectCompleteRound(Require(_battles.StartTutorialBattle(accepted,_content)),CommandForRole(definition.role),out var forecast,out var unionBefore,out var actorBefore);
                var first=Require(_covenants.InvokeAcceptedCovenantForecast(selected,_registry,_progression,_battles,_content,definition.covenantId));
                var replay=Require(_covenants.InvokeAcceptedCovenantForecast(selected,_registry,_progression,_battles,_content,definition.covenantId));
                Assert.AreEqual(CanonicalJson.Serialize(first),CanonicalJson.Serialize(replay),definition.covenantId+" must replay deterministically from the same committed input.");
                var events=first.Battle.EventLog.Where(x=>x.EventType=="GREAT_COVENANT_INVOKED"&&x.ArtId==definition.covenantId).ToArray();
                Assert.AreEqual(1,events.Length,definition.covenantId);Assert.Greater(events[0].Amount,0,definition.covenantId);
                var state=Progression(first);var receipt=state.AppliedReceiptIds.Single(x=>x.StartsWith("COVBATTLE022_",StringComparison.Ordinal));
                Assert.AreEqual(1,state.AppliedReceiptIds.Count(x=>x==receipt));StringAssert.Contains(receipt,events[0].Text);
                Assert.AreEqual(1,first.Battle.RoundRecords.Single().Events.Count(x=>x.EventType=="GREAT_COVENANT_INVOKED"));
                Assert.True(first.Battle.RoundRecords.Single().Selections.Any(x=>x.UnionId==forecast.UnionId&&x.ForecastId==forecast.ForecastId));
                CovenantCosts(definition.role,out var ap,out var mp);var unionAfter=first.Battle.PlayerUnions.Single(x=>x.UnionId==unionBefore.UnionId);var actorAfter=unionAfter.Members.Single(x=>x.MemberId==actorBefore.MemberId);var action=forecast.MemberActions.Single(x=>x.ActorMemberId==actorBefore.MemberId);var committed=first.Battle.RoundRecords.Single().Events.Single(x=>x.EventType=="FORECAST_COMMITTED"&&x.UnionId==unionBefore.UnionId);
                Assert.AreEqual(forecast.SharedApCost+ap,committed.Amount,definition.covenantId+" shared AP commit evidence");
                var expectedMp=actorBefore.CurrentMp-action.PersonalMpCost-mp;if(action.Kind==BattleActionKind.Recovery)expectedMp=Math.Min(actorBefore.MaximumMp,expectedMp+2);
                Assert.AreEqual(expectedMp,actorAfter.CurrentMp,definition.covenantId+" personal MP");
                Assert.LessOrEqual(unionAfter.CurrentAp,Math.Min(unionBefore.MaximumAp,unionBefore.CurrentAp-forecast.SharedApCost-ap+forecast.ApRecovery+3),definition.covenantId+" shared AP");
            }
        }

        [Test]
        public void CovenantInvocationRejectsIncompleteRoundWrongRoleAndUnacceptedAuthority()
        {
            var accepted=AcceptEveryCovenant();var battle=Require(_battles.StartTutorialBattle(accepted,_content));var firstUnion=battle.Battle.PlayerUnions.First();var firstForecast=battle.Battle.CommittedForecasts.First(x=>x.UnionId==firstUnion.UnionId);var partial=Require(_battles.SelectForecast(battle,firstUnion.UnionId,firstForecast.ForecastId));
            var incomplete=_covenants.InvokeAcceptedCovenantForecast(partial,_registry,_progression,_battles,_content,"COVENANT022_WILDROAD_LEVIATHAN");Assert.False(incomplete.IsSuccess);CollectionAssert.Contains(incomplete.Errors,"CAMPAIGN022_COVENANT_COMPLETE_FORECAST_ROUND_REQUIRED");
            var guardSelected=SelectCompleteRound(battle,"CMD_GUARD",out _,out _,out _,"CMD_GUARD");var wrongRole=_covenants.InvokeAcceptedCovenantForecast(guardSelected,_registry,_progression,_battles,_content,"COVENANT022_WILDROAD_LEVIATHAN");Assert.False(wrongRole.IsSuccess);CollectionAssert.Contains(wrongRole.Errors,"CAMPAIGN022_COVENANT_FORECAST_ROLE_REQUIRED");
            var unopened=CreateBattleCampaign();var noAuthority=_covenants.InvokeAcceptedCovenantForecast(unopened,_registry,_progression,_battles,_content,"COVENANT022_WILDROAD_LEVIATHAN");Assert.False(noAuthority.IsSuccess);CollectionAssert.Contains(noAuthority.Errors,"CAMPAIGN022_COVENANT_ACCEPTED_AUTHORITY_REQUIRED");
        }

        [Test]
        public void PublicSurfaceSelectsCovenantButNeverMemberOrArt()
        {
            var method=typeof(CovenantBattleInvocationService022).GetMethod("InvokeAcceptedCovenantForecast");Assert.NotNull(method);Assert.AreEqual(1,method.GetParameters().Count(x=>x.ParameterType==typeof(string)));Assert.False(method.GetParameters().Any(x=>x.Name.IndexOf("member",StringComparison.OrdinalIgnoreCase)>=0||x.Name.IndexOf("artId",StringComparison.OrdinalIgnoreCase)>=0));
            Assert.True(_registry.Covenants.Values.All(x=>!x.sentientOwnershipAllowed&&x.voluntaryAcceptanceRequired&&x.usesSharedAp&&x.usesIndividualMp&&!x.directIndividualSelection&&!x.realMoneyGacha&&x.standardControl=="STORY_AND_COVENANT_FORECASTS"));
        }

        [Test]
        public void SharedCinematicPlannerStagesEchoAndGreatCovenantAsVisibleInvocationBeats()
        {
            var beats=BattlePresentationPlanner.Plan(new[]{new M2BattleEventView{Sequence=1,Round=1,EventType="SUMMON_ECHO_INVOKED",ActorUnionId="U",ActorMemberId="M",TargetUnionId="E",TargetMemberId="X",ArtId="SUMMON_ECHO022_01",Amount=2,Text="Echo"},new M2BattleEventView{Sequence=2,Round=1,EventType="GREAT_COVENANT_INVOKED",ActorUnionId="U",ActorMemberId="M",TargetUnionId="E",TargetMemberId="X",ArtId="COVENANT022_WILDROAD_LEVIATHAN",Amount=12,Text="Great Covenant"}});Assert.AreEqual(2,beats.Count);Assert.True(beats.All(x=>x.VisuallyStaged&&x.Family==BattleBeatFamily.Invocation));Assert.AreEqual(BattleCameraShot.Breakthrough,beats[1].Camera);Assert.Greater(beats[1].Amount,0);
        }

        CampaignState AcceptEveryCovenant()
        {
            var campaign=CreateBattleCampaign();
            for(var floor=1;floor<=10;floor++)
            {
                var operationId="ABYSS_OP022_"+floor.ToString("00")+"_GUARDIAN";campaign=Require(_progression.BeginAbyssOperation(campaign,_registry,operationId));var operation=_registry.AbyssOperations[operationId];
                for(var step=0;step<operation.steps.Length;step++)
                {
                    if(operation.steps[step].requiresBattle)
                    {
                        campaign=Require(_progression.CommitAbyssBattleEncounter(campaign,_registry));
                        var request=campaign.Guild.GuildCity.PendingEncounter;
                        campaign=WithClaimedAbyssBattle084(campaign,
                            "COVENANT_GUARDIAN_REWARD_022_"+floor,request);
                        campaign=Require(_progression.CommitAbyssBattleReturn(campaign,_registry));
                        campaign=Require(_progression.ApplyAbyssBattleReturnExactlyOnce(campaign,_registry));
                        campaign=Require(_progression.CommitAbyssBattleResult(campaign,_registry));
                        campaign=Require(_progression.ApplyAbyssBattleAndFinalize(campaign,_registry));
                    }
                    else
                    {
                        campaign=Require(_progression.CommitAbyssStep(campaign,_registry,"SUCCESS"));
                        campaign=Require(_progression.ApplyAbyssStep(campaign,_registry));
                    }
                }
                campaign=Require(_progression.CommitAbyssOperationCompletion(campaign,_registry));campaign=Require(_progression.ApplyAbyssOperationCompletion(campaign,_registry));
            }
            foreach(var id in _registry.Covenants.Keys.OrderBy(x=>x,StringComparer.Ordinal)){for(var stage=0;stage<4;stage++)campaign=Require(_progression.AdvanceCovenantTrial(campaign,_registry,id));campaign=Require(_progression.AcceptCovenant(campaign,_registry,id));}
            return campaign;
        }

        static CampaignState WithClaimedAbyssBattle084(
            CampaignState campaign,string rewardId,
            SecondDimension.Gameplay.GuildCity017D.EncounterLaunchRequest017D request)
        {
            var memberReward=new BattleMemberRewardState(
                "COVENANT_GUARDIAN_MEMBER_022","Covenant Guardian",1,1,1,
                0,0,0,0,0,0,0);
            var reward=new BattleRewardState(rewardId,
                "COVENANT_GUARDIAN_REWARD_RULE_022",BattleOutcome.Victory,
                1,1,1000,1000,100,100,7,5,new[]{memberReward},true);
            var battle=new BattleState(request.BattleId,
                "COVENANT_GUARDIAN_022",1,BattlePhase.Resolved,
                BattleOutcome.Victory,request.Objective,
                Array.Empty<BattleUnionState>(),Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(),Array.Empty<BattleRoundRecordState>(),
                "COVENANT_GUARDIAN_FORECAST_022",
                "COVENANT_GUARDIAN_INITIAL_022",string.Empty,string.Empty,
                string.Empty,false,reward);
            battle=battle.With(finalStateHash:
                M2BattleCommandService.AuthoritativeStateHash(battle));
            var development=campaign.Guild.Development.RecordBattleReward(
                rewardId,reward.GuildTreasuryXpAward,
                reward.HallEnhancementXpAward);
            var guild=campaign.Guild.With(
                campaign.Guild.TreasuryXp+reward.GuildTreasuryXpAward,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development);
            return campaign.With(guild,campaign.OpeningFlow).WithBattle(battle);
        }

        CampaignState SelectCompleteRound(CampaignState campaign,string commandId,out BattleForecastState selectedForecast,out BattleUnionState selectedUnion,out BattleMemberState selectedActor,string otherCommandId="CMD_BALANCED")
        {
            selectedForecast=campaign.Battle.CommittedForecasts.FirstOrDefault(x=>x.CommandId==commandId);Assert.NotNull(selectedForecast,"Missing authored command "+commandId);var selectedForecastValue=selectedForecast;selectedUnion=campaign.Battle.PlayerUnions.Single(x=>x.UnionId==selectedForecastValue.UnionId);var selectedUnionValue=selectedUnion;selectedActor=selectedUnionValue.Members.Single(x=>x.MemberId==(selectedForecastValue.MemberActions.FirstOrDefault(a=>a.ActorMemberId==selectedUnionValue.LeaderMemberId)??selectedForecastValue.MemberActions.OrderBy(a=>a.ActorMemberId,StringComparer.Ordinal).First()).ActorMemberId);
            foreach(var union in campaign.Battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated))
            {
                var forecast=union.UnionId==selectedForecastValue.UnionId?selectedForecastValue:campaign.Battle.CommittedForecasts.First(x=>x.UnionId==union.UnionId&&x.CommandId==otherCommandId);campaign=Require(_battles.SelectForecast(campaign,union.UnionId,forecast.ForecastId));
            }
            return campaign;
        }

        static string CommandForRole(string role){switch(role){case "COMBAT":return "CMD_ALL_OUT";case "MYSTIC":return "CMD_MYSTIC";case "RESTORATION":return "CMD_HEAL";case "TACTICAL":return "CMD_FLANK";case "SUPPORT":return "CMD_SUPPORT";default:return "CMD_GUARD";}}
        static void CovenantCosts(string role,out int ap,out int mp){switch(role){case "GUARD":ap=2;mp=6;break;case "WARDING":case "SUPPORT":ap=3;mp=7;break;case "RESTORATION":ap=3;mp=8;break;case "MYSTIC":ap=4;mp=10;break;default:ap=4;mp=8;break;}}

        static CampaignState CreateBattleCampaign()
        {
            var classes=new[]{"CLASS_TEND_GUARDIAN","CLASS_TEND_WARRIOR","CLASS_TEND_RANGER","CLASS_TEND_PRIEST","CLASS_TEND_MAGE","CLASS_TEND_ROGUE"};var tags=new[]{new[]{"SHIELD","SWORD","WEAPON"},new[]{"SWORD","WEAPON"},new[]{"BOW","WEAPON"},new[]{"STAFF","HEALING"},new[]{"WAND","FOCUS_TOOL"},new[]{"DAGGER","WEAPON"}};var recruits=new List<RecruitState>();
            for(var i=0;i<6;i++){var item=new EquipmentItemState("COV_ITEM_"+i,"COV_DEF_"+i,"Covenant Weapon "+i,new[]{EquipmentSlotIds.MainHand},tags[i],"QUALITY_STANDARD",10000,false);recruits.Add(new RecruitState("COV_RECRUIT_"+i,130+i*5,130+i*5,35+i*2,35+i*2,"Covenant Recruit "+i,RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME",classes[i],"Observed",7000,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(new[]{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,item)}),true,string.Empty,string.Empty,60+i,60+i));}
            var unions=new[]{new UnionState("COV_UNION_A","Covenant Vanguard",UnionKind.Normal,recruits[0].RecruitId,recruits.Take(3).Select(x=>x.RecruitId).ToArray(),"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",24,8500),new UnionState("COV_UNION_B","Covenant Arcanum",UnionKind.Normal,recruits[3].RecruitId,recruits.Skip(3).Take(3).Select(x=>x.RecruitId).ToArray(),"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",24,8500)};
            var guild=new GuildState("GUILD_COVENANT_BATTLE_022",0,recruits,unions);var profile=new NewGuildProfileState("Covenant Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"covenant_battle_ready");var campaign=new CampaignState("00000000-0000-0000-0000-000000002222",2222,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);return WithProgression(campaign,CampaignProgressionState022.Default());
        }

        static CampaignProgressionState022 Progression(CampaignState c)=>c.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;
        static CampaignState WithProgression(CampaignState campaign,CampaignProgressionState022 state){var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var progress=strategic.Campaign019;var playable=progress.Playable020.With(progression022:state,replaceProgression022:true,lastCheckpointId:state.LastCheckpointId);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:state.LastCheckpointId);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:state.LastCheckpointId);return campaign.With(campaign.Guild.WithGuildCity(city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:state.LastCheckpointId)),campaign.OpeningFlow);}
        static CampaignState Require(Result<CampaignState> result){Assert.True(result.IsSuccess,string.Join("\n",result.Errors));return result.Value;}
    }
}
