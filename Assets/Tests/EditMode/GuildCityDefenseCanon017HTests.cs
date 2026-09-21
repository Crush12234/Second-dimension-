using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityDefenseCanon017HTests
    {
        private GuildCityContent017D _cityContent;
        private GuildCityStrategicContent017H _strategic;
        private GuildCityDefenseService017H _defense;
        private GuildCityCanonEventService017H _canon;

        [SetUp]
        public void SetUp()
        {
            var root=Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT");
            _cityContent=GuildCityContent017D.LoadFromDirectory(Path.Combine(root,"GUILD_CITY_017D"));
            _strategic=GuildCityStrategicContent017H.LoadFromDirectory(Path.Combine(root,"GUILD_CITY_017H"));
            _defense=new GuildCityDefenseService017H();_canon=new GuildCityCanonEventService017H();
        }

        [Test] public void ThirtyBuildingsAllHaveCombatAndXpContributions()
        {
            Assert.That(_cityContent.Buildings.Count,Is.EqualTo(30));Assert.That(_strategic.Contributions.Count,Is.EqualTo(30));
            foreach(var building in _cityContent.Buildings.Values)
            {
                Assert.That(_strategic.Contributions.ContainsKey(building.Id),Is.True,building.Id);
                var v=_strategic.Contribution(building.Id).PerLevel;
                var xp=v.PersonalXpBp+v.CombatArtMasteryBp+v.MysticMasteryBp+v.RestorationMasteryBp+v.WardingMasteryBp+v.UnionDisciplineBp+v.GuildTreasuryXpBp+v.CivicHallXpBp+v.StaffDutyXpFlat;
                var combat=v.StartingApFlat+v.StartingCohesionFlat+v.StartingMpFlat+v.EnemyCohesionDamageFlat+v.SuppliesFlat+v.ScoutingFlat+v.DefensePowerFlat+v.BarrierIntegrityFlat+v.ResupplyFlat+v.ReinforcementReadinessFlat+v.GuardPreparationFlat;
                Assert.That(xp,Is.GreaterThan(0),building.Id+" needs an XP/mastery contribution.");
                Assert.That(combat,Is.GreaterThan(0),building.Id+" needs a combat/operation contribution.");
            }
        }

        [Test] public void ContributionStackingIsCappedAndCreatesOnlyHighLevelBattleModifiers()
        {
            var campaign=CreateCampaign();var snapshot=new GuildCityBuildingContributionService017H().Calculate(campaign.Guild.GuildCity,_strategic);
            Assert.That(snapshot.PersonalXpBp,Is.LessThanOrEqualTo(3000));Assert.That(snapshot.DefensePowerFlat,Is.LessThanOrEqualTo(50));
            foreach(var modifier in snapshot.ToRouteModifiers())Assert.That(modifier,Does.Not.Contain("SELECT_ART"));
        }

        [Test] public void OpeningDefenseUsesStrategicWavesThenCertifiedBattleRequest()
        {
            var campaign=Require(_defense.StartDefense(CreateCampaign(),_strategic,"DEF_PROFILE_FIRST_BELL_DRILL"));
            campaign=Require(_defense.AssignUnion(campaign,_strategic,"DEF_LANE_MAIN_GATE","U1"));
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter,Is.Null);
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter,Is.Null);
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter,Is.Not.Null);
            Assert.That(campaign.Guild.GuildCity.PendingEncounter.EnemyUnionCount,Is.InRange(1,10));
            Assert.That(campaign.Guild.GuildCity.PendingEncounter.RouteModifiers,Contains.Item("CITY_DEFENSE_WAVE"));
        }

        [Test] public void MutatedSavedDefenseEncounterCannotCrossSharedBattleBoundary()
        {
            var campaign=Require(_defense.StartDefense(CreateCampaign(),_strategic,
                "DEF_PROFILE_FIRST_BELL_DRILL"));
            campaign=Require(_defense.AssignUnion(campaign,_strategic,
                "DEF_LANE_MAIN_GATE","U1"));
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            campaign=Require(_defense.CommitCurrentWave(campaign,_strategic));
            var request=campaign.Guild.GuildCity.PendingEncounter;
            var retry=Require(_defense.CommitCurrentWave(campaign,_strategic));
            Assert.That(CanonicalJson.Serialize(retry),
                Is.EqualTo(CanonicalJson.Serialize(campaign)));
            Assert.That(campaign.Guild.Development.HasAdventureAuthority(
                GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(
                    request)),Is.True);

            var foreignBattle=WithForeignClaimedBattle084(campaign);
            var foreignHash=CanonicalJson.Sha256Hex(foreignBattle);
            var foreignRejected=_defense.ApplyDecisiveBattleAfterClaim(
                foreignBattle,_strategic);
            Assert.That(foreignRejected.IsSuccess,Is.False);
            Assert.That(foreignRejected.Errors,Does.Contain(
                "GC017H_DEFENSE_BATTLE_AUTHORITY_INVALID"));
            Assert.That(CanonicalJson.Sha256Hex(foreignBattle),
                Is.EqualTo(foreignHash));

            var mutatedRequest=new EncounterLaunchRequest017D(
                request.RequestId,request.ContractId,request.ExpeditionId,
                request.BoardId,request.NodeId,request.EncounterId,request.BattleId,
                request.Objective+" (mutated saved request)",request.EnemyUnionCount,
                request.CanonicalSeedIdentity,request.AlliedUnionIds,
                request.ReserveUnionIds,request.ObjectiveIds,request.RouteModifiers,
                request.Supplies,request.Fatigue,request.Urgency,
                request.ReturnCheckpointId,request.PreBattleStateHash);
            var mutatedCity=campaign.Guild.GuildCity.With(
                pendingEncounter:mutatedRequest,replacePendingEncounter:true,
                lastCheckpointId:"mutated_saved_defense_request_084");
            var mutated=campaign.With(campaign.Guild.WithGuildCity(mutatedCity),
                campaign.OpeningFlow);
            var before=CanonicalJson.Sha256Hex(mutated);
            var combat=M2CombatContent.LoadFromDirectory(Path.Combine(
                Application.streamingAssetsPath,"Authority","CONTENT"));
            var bridge=new GuildCityBattleBridgeService017D();
            var battles=new M2BattleCommandService();
            var rejected=bridge.StartCertifiedEncounter(mutated,battles,combat);
            Assert.That(rejected.IsSuccess,Is.False);
            Assert.That(rejected.Errors,Does.Contain(
                "M2_COMMITTED_ENCOUNTER_AUTHORITY_REQUIRED"));
            Assert.That(CanonicalJson.Sha256Hex(mutated),Is.EqualTo(before));
            Assert.That(mutated.Battle,Is.Null);

            var canonical=bridge.StartCertifiedEncounter(campaign,battles,combat);
            Assert.That(canonical.IsSuccess,Is.True,string.Join("\n",canonical.Errors));
            Assert.That(canonical.Value.Battle.BattleId,Is.EqualTo(request.BattleId));
        }

        [Test] public void GoblinFortressWarCannotTriggerBeforeItsStoryGate()
        {
            var result=_defense.StartDefense(CreateCampaign(),_strategic,"DEF_PROFILE_GOBLIN_FORTRESS_WAR");
            Assert.That(result.IsSuccess,Is.False);Assert.That(result.Errors,Contains.Item("GC017H_DEFENSE_STORY_GATE_LOCKED"));
        }

        [Test] public void CanonEventsSeparateChronicleEchoAndFutureLocks()
        {
            Assert.That(_strategic.CanonEvents.Values.Count(x=>x.Classification=="HISTORICAL_CHRONICLE"),Is.EqualTo(12));
            Assert.That(_strategic.CanonEvents.Values.Count(x=>x.Classification=="CANON_ECHO"),Is.EqualTo(12));
            Assert.That(_strategic.CanonEvents.Values.Count(x=>x.Classification=="FUTURE_LOCKED"),Is.EqualTo(12));
            Assert.That(_strategic.CanonEvents.Values.All(x=>!x.CanRewritePublishedCanon&&!x.RevealsProtectedSystemTruth&&!x.CanCauseInvoluntaryDeparture),Is.True);
        }

        [Test] public void OpeningCanonEchoIsAvailableWithoutConsumingAnOperation()
        {
            var campaign=CreateCampaign();var before=campaign.Guild.GuildCity.OperationOrdinal;
            campaign=Require(_canon.SynchronizeAvailability(campaign,_strategic));
            var echo=campaign.Guild.GuildCity.Strategic017H.CanonEvents.Single(x=>x.EventId=="CANON_ECHO_BELL_WARNING");
            Assert.That(echo.Status,Is.EqualTo(CanonEventStatus017H.Available));
            campaign=Require(_canon.ResolveEvent(campaign,_strategic,echo.EventId,"PREPARE"));
            Assert.That(campaign.Guild.GuildCity.OperationOrdinal,Is.EqualTo(before));
        }


        [Test] public void MultipleUnionsCanBeAssignedAcrossDefenseLanes()
        {
            var campaign=Require(_defense.StartDefense(CreateCampaign(),_strategic,"DEF_PROFILE_FIRST_BELL_DRILL"));
            campaign=Require(_defense.AssignUnion(campaign,_strategic,"DEF_LANE_MAIN_GATE","U1"));
            campaign=Require(_defense.AssignUnion(campaign,_strategic,"DEF_LANE_MARKET_ROAD","U2"));
            var assignments=campaign.Guild.GuildCity.Strategic017H.ActiveDefense.LaneAssignments;
            Assert.That(assignments.SelectMany(value=>value.UnionIds).Distinct().Count(),Is.EqualTo(2));
        }

        [Test] public void EveryDefenseProfileHasCertifiedDecisiveWavesWithinScope()
        {
            foreach(var profile in _strategic.Profiles.Values)
            {
                Assert.That(profile.Waves,Is.Not.Null,profile.Id);
                Assert.That(profile.Waves.Length,Is.EqualTo(3),profile.Id);
                var decisiveCount=profile.Waves.Count(value=>value.DecisiveBattle);
                Assert.That(decisiveCount,Is.GreaterThanOrEqualTo(1),profile.Id);
                if(profile.AvailableInOpening)Assert.That(decisiveCount,Is.EqualTo(1),profile.Id);
            }
        }

        [Test] public void ChronicleBuildingUnlocksHistoricalChroniclesWithoutFutureSagaLeak()
        {
            var campaign=CreateCampaign();
            var city=campaign.Guild.GuildCity;
            var plots=new List<CityPlotState017D>(city.CityPlots);
            plots[0]=plots[0].With(unlocked:true,buildingId:"GC017H_BUILD_CHRONICLE_PLAZA",buildingLevel:1);
            city=city.With(cityPlots:plots.AsReadOnly(),lastCheckpointId:"chronicle_test_built");
            campaign=campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
            campaign=Require(GuildCityStoryGateService017H.SynchronizeDerivedGates(campaign));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.StoryGates,Contains.Item("STORY_GATE_CHRONICLE_UNLOCKED"));
            campaign=Require(_canon.SynchronizeAvailability(campaign,_strategic));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.CanonEvents.Single(value=>value.EventId=="CANON_HIST_FIVE_MINUTE_WAR").Status,Is.EqualTo(CanonEventStatus017H.Available));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.CanonEvents.Single(value=>value.EventId=="CANON_FUTURE_GOBLIN_WAR").Status,Is.EqualTo(CanonEventStatus017H.Locked));
        }

        [Test] public void CanonResolutionIsExactOnceAcrossRepeatedCommands()
        {
            var campaign=Require(_canon.SynchronizeAvailability(CreateCampaign(),_strategic));
            campaign=Require(_canon.ResolveEvent(campaign,_strategic,"CANON_ECHO_BELL_WARNING","PREPARE"));
            var treasury=campaign.Guild.TreasuryXp;
            var defenseMastery=campaign.Guild.GuildCity.Strategic017H.DefenseMasteryXp;
            var receiptCount=campaign.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds.Count;
            campaign=Require(_canon.ResolveEvent(campaign,_strategic,"CANON_ECHO_BELL_WARNING","PREPARE"));
            Assert.That(campaign.Guild.TreasuryXp,Is.EqualTo(treasury));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.DefenseMasteryXp,Is.EqualTo(defenseMastery));
            Assert.That(campaign.Guild.GuildCity.Strategic017H.AppliedStrategicReceiptIds.Count,Is.EqualTo(receiptCount));
        }

        [Test] public void StateRoundTripKeepsStrategicDefenseAndStoryGates()
        {
            var campaign=Require(_defense.StartDefense(CreateCampaign(),_strategic,"DEF_PROFILE_FIRST_BELL_DRILL"));
            var json=SecondDimension.Determinism.CanonicalJson.Serialize(campaign.Guild.GuildCity);
            var round=Newtonsoft.Json.JsonConvert.DeserializeObject<GuildCityState017D>(json);
            Assert.That(round.Strategic017H.ActiveDefense.ProfileId,Is.EqualTo("DEF_PROFILE_FIRST_BELL_DRILL"));
            Assert.That(round.Strategic017H.StoryGates,Contains.Item("STORY_GATE_OPENING_GUILD"));
        }

        private static CampaignState CreateCampaign()
        {
            var recruits=new[]{new RecruitState("R1",100,100,20,20),new RecruitState("R2",100,100,20,20),new RecruitState("R3",100,100,20,20),new RecruitState("R4",100,100,20,20),new RecruitState("R5",100,100,20,20),new RecruitState("R6",100,100,20,20)};
            var unions=new[]{new UnionState("U1","First Union",UnionKind.Normal,"R1",new[]{"R1","R2","R3"},"FORMATION_LINE","DOCTRINE_BALANCED",30,7000),new UnionState("U2","Second Union",UnionKind.Normal,"R4",new[]{"R4","R5","R6"},"FORMATION_LINE","DOCTRINE_BALANCED",30,7000)};
            var guild=new GuildState("GUILD_TEST",1000,recruits,unions);var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"complete");
            return new CampaignState("00000000-0000-0000-0000-000000017018",17018,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,new NewGuildProfileState("Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),flow);
        }
        private static CampaignState WithForeignClaimedBattle084(
            CampaignState campaign)
        {
            const string rewardId="DEFENSE_FOREIGN_REWARD_084";
            var member=new BattleMemberRewardState("R1","Foreign Defender",1,1,1,
                0,0,0,0,0,0,0);
            var reward=new BattleRewardState(rewardId,"DEFENSE_FOREIGN_084",
                BattleOutcome.Victory,1,1,1000,1000,100,100,7,5,
                new[]{member},true);
            var battle=new BattleState("FOREIGN_DEFENSE_BATTLE_084",
                "DEFENSE_FOREIGN_084",1,BattlePhase.Resolved,
                BattleOutcome.Victory,"Foreign battle",
                Array.Empty<BattleUnionState>(),Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(),
                Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(),
                Array.Empty<BattleRoundRecordState>(),"FOREIGN_FORECAST_084",
                "FOREIGN_INITIAL_084",string.Empty,string.Empty,string.Empty,
                false,reward);
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
        private static CampaignState Require(Result<CampaignState> result){Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
    }
}
