using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017H
{
    public sealed class GuildCityDefenseService017H
    {
        private readonly GuildCityBuildingContributionService017H _contributions=new GuildCityBuildingContributionService017H();

        public Result<CampaignState> StartDefense(CampaignState campaign,GuildCityStrategicContent017H content,string profileId)
        {
            if(campaign==null||content==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_INPUT_REQUIRED");
            if(SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParked(campaign,"CAMPAIGN"))return Result<CampaignState>.Failure("Resume the saved Campaign activity first.");
            var synchronized = GuildCityStoryGateService017H.SynchronizeDerivedGates(campaign);
            if (!synchronized.IsSuccess) return synchronized;
            campaign = synchronized.Value;
            if(GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(campaign))
                return Result<CampaignState>.Failure("GC017H_OPERATION_ALREADY_ACTIVE");
            var profile=content.DefenseProfile(profileId);var strategic=campaign.Guild.GuildCity.Strategic017H??GuildCityStrategicState017H.Default();
            if(strategic.ActiveDefense!=null)return Result<CampaignState>.Failure("GC017H_DEFENSE_ALREADY_ACTIVE");
            if(profile.FutureLocked&&!Contains(strategic.StoryGates,profile.RequiredStoryGate))return Result<CampaignState>.Failure("GC017H_DEFENSE_STORY_GATE_LOCKED");
            if(!Contains(strategic.StoryGates,profile.RequiredStoryGate))return Result<CampaignState>.Failure("GC017H_DEFENSE_STORY_GATE_REQUIRED");
            var snapshot=_contributions.Calculate(campaign.Guild.GuildCity,content);var seed=SemanticSeed.Derive(campaign.CampaignSeed,campaign.Guild.GuildCity.OperationOrdinal,profile.Id);
            var opId="DEFENSE_OP_"+CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,profile.Id,Seed=seed.ToString(),campaign.Guild.GuildCity.OperationOrdinal}).Substring(0,24).ToUpperInvariant();
            var active=new CityDefenseOperationState017H(opId,profile.Id,0,CityDefenseStatus017H.Planning,100,Math.Min(50,snapshot.BarrierIntegrityFlat*2),Array.Empty<DefenseLaneAssignmentState017H>(),Array.Empty<string>(),string.Empty,"defense_started");
            return Success(campaign,campaign.Guild.GuildCity.With(strategic017H:strategic.With(activeDefense:active,replaceActiveDefense:true,lastCheckpointId:"defense_started"),replaceStrategic017H:true,lastCheckpointId:"defense_started"));
        }

        public Result<CampaignState> AssignUnion(CampaignState campaign,GuildCityStrategicContent017H content,string laneId,string unionId)
        {
            if(campaign==null||content==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_INPUT_REQUIRED");
            if(!content.Lanes.ContainsKey(laneId))return Result<CampaignState>.Failure("GC017H_LANE_NOT_FOUND");
            if(!OwnsUnion(campaign.Guild.Unions,unionId))return Result<CampaignState>.Failure("GC017H_UNION_NOT_OWNED");
            var strategic=campaign.Guild.GuildCity.Strategic017H;var active=strategic?.ActiveDefense;
            if(active==null||(active.Status!=CityDefenseStatus017H.Planning&&active.Status!=CityDefenseStatus017H.Active))
                return Result<CampaignState>.Failure("GC017H_DEFENSE_PLANNING_REQUIRED");
            var assignments=new List<DefenseLaneAssignmentState017H>();var found=false;
            for(var i=0;i<active.LaneAssignments.Count;i++)
            {
                var existing=active.LaneAssignments[i];var unions=new List<string>(existing.UnionIds);unions.Remove(unionId);
                if(StringComparer.Ordinal.Equals(existing.LaneId,laneId)){if(!unions.Contains(unionId))unions.Add(unionId);unions.Sort(StringComparer.Ordinal);found=true;}
                if(unions.Count>0)assignments.Add(new DefenseLaneAssignmentState017H(existing.LaneId,unions.AsReadOnly()));
            }
            if(!found)assignments.Add(new DefenseLaneAssignmentState017H(laneId,new[]{unionId}));
            assignments.Sort((a,b)=>StringComparer.Ordinal.Compare(a.LaneId,b.LaneId));
            var updated=active.With(laneAssignments:assignments.AsReadOnly(),status:CityDefenseStatus017H.Active,lastCheckpointId:"defense_union_assigned");
            return Success(campaign,campaign.Guild.GuildCity.With(strategic017H:strategic.With(activeDefense:updated,replaceActiveDefense:true,lastCheckpointId:"defense_union_assigned"),replaceStrategic017H:true,lastCheckpointId:"defense_union_assigned"));
        }

        public Result<CampaignState> CommitCurrentWave(CampaignState campaign,GuildCityStrategicContent017H content)
        {
            if(campaign==null||content==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_INPUT_REQUIRED");
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var active=strategic?.ActiveDefense;
            if(active==null||(active.Status!=CityDefenseStatus017H.Active&&active.Status!=CityDefenseStatus017H.Planning&&active.Status!=CityDefenseStatus017H.AwaitingBattle))return Result<CampaignState>.Failure("GC017H_ACTIVE_DEFENSE_REQUIRED");
            var profile=content.DefenseProfile(active.ProfileId);if(active.CurrentWaveIndex>=profile.Waves.Length)return Result<CampaignState>.Failure("GC017H_DEFENSE_WAVES_COMPLETE");
            var wave=profile.WaveAt(active.CurrentWaveIndex);var waveId=wave.StableWaveId(profile.Id);if(Contains(active.ResolvedWaveIds,waveId))return Result<CampaignState>.Success(campaign);
            var snapshot=_contributions.Calculate(city,content);var assignedUnionIds=AssignedUnions(active,campaign.Guild.Unions);if(assignedUnionIds.Count==0)return Result<CampaignState>.Failure("GC017H_DEFENSE_UNION_REQUIRED");
            if(wave.DecisiveBattle)
            {
                var modifiers=new List<string>(snapshot.ToRouteModifiers()){ "CITY_DEFENSE_WAVE", "CITY_DEFENSE_PROFILE_"+profile.Id, "CITY_DEFENSE_WAVE_"+wave.WaveIndex };
                modifiers.Sort(StringComparer.Ordinal);var seed=SemanticSeed.Derive(campaign.CampaignSeed,active.OperationId,waveId);
                var requestHash=CanonicalJson.Sha256Hex(new{active.OperationId,profile.Id,waveId,Seed=seed.ToString(),assignedUnionIds,modifiers});
                var preBattleStateHash=city.PendingEncounter!=null?city.PendingEncounter.PreBattleStateHash:CanonicalJson.Sha256Hex(active);
                var request=new EncounterLaunchRequest017D("DEFENSE_ENCOUNTER_"+requestHash.Substring(0,24).ToUpperInvariant(),"CITY_DEFENSE_"+profile.Id,active.OperationId,"CITY_DEFENSE_BOARD_017H",waveId,"DEFENSE_"+waveId,"BATTLE_DEFENSE_"+waveId,wave.Objective,wave.EnemyUnionCount,seed.ToString(),assignedUnionIds,Array.Empty<string>(),new[]{"OBJECTIVE_DEFEND_CITY","OBJECTIVE_"+waveId},modifiers.AsReadOnly(),Math.Max(0,10+snapshot.SuppliesFlat),0,Math.Max(0,profile.Difficulty-snapshot.UrgencyReductionFlat),"RETURN_DEFENSE_"+waveId,preBattleStateHash);
                var requestAuthority=GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request);
                if(city.PendingEncounter!=null)
                {
                    if(active.Status!=CityDefenseStatus017H.AwaitingBattle||
                       !StringComparer.Ordinal.Equals(CanonicalJson.Sha256Hex(city.PendingEncounter),CanonicalJson.Sha256Hex(request))||
                       !campaign.Guild.Development.HasAdventureAuthority(requestAuthority))
                        return Result<CampaignState>.Failure("GC017H_DEFENSE_ENCOUNTER_INVALID");
                    return Result<CampaignState>.Success(campaign);
                }
                var development=campaign.Guild.Development;
                if(development.HasAdventureAuthority(requestAuthority))return Result<CampaignState>.Failure("GC017H_ENCOUNTER_AUTHORITY_ALREADY_COMMITTED");
                if(!development.CanRecordAdventureAuthority(requestAuthority))return Result<CampaignState>.Failure("GC017H_ADVENTURE_AUTHORITY_LEDGER_FULL");
                development=development.RecordAdventureAuthority(requestAuthority);
                campaign=campaign.With(campaign.Guild.With(campaign.Guild.TreasuryXp,campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development),campaign.OpeningFlow);
                var waiting=active.With(status:CityDefenseStatus017H.AwaitingBattle,pendingWaveId:waveId,lastCheckpointId:"defense_battle_committed");
                var updatedStrategic=strategic.With(activeDefense:waiting,replaceActiveDefense:true,lastCheckpointId:"defense_battle_committed");
                return Success(campaign,city.With(pendingEncounter:request,replacePendingEncounter:true,strategic017H:updatedStrategic,replaceStrategic017H:true,lastCheckpointId:"defense_battle_committed"));
            }
            return ResolveSupportWave(campaign,content,profile,wave,waveId,snapshot);
        }

        public Result<CampaignState> ApplyDecisiveBattleAfterClaim(CampaignState campaign,GuildCityStrategicContent017H content)
        {
            if(campaign?.Battle==null||content==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_BATTLE_REQUIRED");
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var active=strategic?.ActiveDefense;
            if(active==null||active.Status!=CityDefenseStatus017H.AwaitingBattle)return Result<CampaignState>.Failure("GC017H_DEFENSE_AWAITING_BATTLE_REQUIRED");
            var profile=content.DefenseProfile(active.ProfileId);var wave=profile.WaveAt(active.CurrentWaveIndex);var waveId=wave.StableWaveId(profile.Id);
            var pending=city.PendingEncounter;
            if(pending==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_ENCOUNTER_REQUIRED");
            var snapshot=_contributions.Calculate(city,content);
            var assignedUnionIds=AssignedUnions(active,campaign.Guild.Unions);
            if(assignedUnionIds.Count==0)return Result<CampaignState>.Failure("GC017H_DEFENSE_UNION_REQUIRED");
            var modifiers=new List<string>(snapshot.ToRouteModifiers())
            {
                "CITY_DEFENSE_WAVE", "CITY_DEFENSE_PROFILE_"+profile.Id,
                "CITY_DEFENSE_WAVE_"+wave.WaveIndex
            };
            modifiers.Sort(StringComparer.Ordinal);
            var seed=SemanticSeed.Derive(campaign.CampaignSeed,active.OperationId,waveId);
            var requestHash=CanonicalJson.Sha256Hex(new
                {active.OperationId,profile.Id,waveId,Seed=seed.ToString(),assignedUnionIds,modifiers});
            var expectedRequest=new EncounterLaunchRequest017D(
                "DEFENSE_ENCOUNTER_"+requestHash.Substring(0,24).ToUpperInvariant(),
                "CITY_DEFENSE_"+profile.Id,active.OperationId,
                "CITY_DEFENSE_BOARD_017H",waveId,"DEFENSE_"+waveId,
                "BATTLE_DEFENSE_"+waveId,wave.Objective,wave.EnemyUnionCount,
                seed.ToString(),assignedUnionIds,Array.Empty<string>(),
                new[]{"OBJECTIVE_DEFEND_CITY","OBJECTIVE_"+waveId},
                modifiers.AsReadOnly(),Math.Max(0,10+snapshot.SuppliesFlat),0,
                Math.Max(0,profile.Difficulty-snapshot.UrgencyReductionFlat),
                "RETURN_DEFENSE_"+waveId,pending.PreBattleStateHash);
            if(!StringComparer.Ordinal.Equals(CanonicalJson.Serialize(pending),
                   CanonicalJson.Serialize(expectedRequest))||
               !campaign.Guild.Development.HasAdventureAuthority(
                   GuildCityBattleBridgeService017D
                       .EncounterRequestAuthorityId084(expectedRequest)))
                return Result<CampaignState>.Failure("GC017H_DEFENSE_ENCOUNTER_INVALID");
            var battle=campaign.Battle;
            if(battle.Outcome==BattleOutcome.InProgress||
               battle.Phase!=BattlePhase.Resolved||battle.Reward==null||
               !battle.Reward.Claimed)
                return Result<CampaignState>.Failure("GC017H_CLAIM_BATTLE_REWARD_FIRST");
            if(!StringComparer.Ordinal.Equals(battle.BattleId,expectedRequest.BattleId)||
               battle.Reward.Outcome!=battle.Outcome||
               !M2BattleCommandService.HasValidFinalStateHash090(battle)||
               !campaign.Guild.Development.HasClaimedReward(battle.Reward.RewardId)||
               !battle.PlayerUnions.Select(value=>value.UnionId)
                   .OrderBy(value=>value,StringComparer.Ordinal)
                   .SequenceEqual(expectedRequest.AlliedUnionIds
                       .OrderBy(value=>value,StringComparer.Ordinal),StringComparer.Ordinal))
                return Result<CampaignState>.Failure("GC017H_DEFENSE_BATTLE_AUTHORITY_INVALID");
            var receiptId="DEFENSE_RECEIPT_"+CanonicalJson.Sha256Hex(new{active.OperationId,waveId,battle.BattleId,battle.FinalStateHash,battle.Outcome,battle.Reward.RewardId}).Substring(0,24).ToUpperInvariant();
            if(Contains(strategic.AppliedStrategicReceiptIds,receiptId))return Result<CampaignState>.Success(campaign);
            var won=battle.Outcome==BattleOutcome.Victory;var integrityLoss=won?0:Math.Max(5,wave.EnemyPower/3);var updated=CompleteWave(active,waveId,won,integrityLoss,profile.Waves.Length,finalWaveLossIsFailure:true);
            var applied=new List<string>(strategic.AppliedStrategicReceiptIds){receiptId};applied.Sort(StringComparer.Ordinal);
            var nextStrategic=strategic.With(activeDefense:updated,replaceActiveDefense:true,appliedStrategicReceiptIds:applied.AsReadOnly(),defenseMasteryXp:strategic.DefenseMasteryXp+wave.DefenseMasteryXp,lastCheckpointId:"defense_battle_applied");
            var reward=ApplyInstitutionalReward(campaign,receiptId,wave.RewardGuildXp,wave.RewardCivicHallXp);
            city=reward.Guild.GuildCity.With(pendingEncounter:null,replacePendingEncounter:true,strategic017H:nextStrategic,replaceStrategic017H:true,lastCheckpointId:"defense_battle_applied");
            return Result<CampaignState>.Success(reward.With(reward.Guild.WithGuildCity(city),reward.OpeningFlow));
        }

        public Result<CampaignState> FinalizeDefense(CampaignState campaign)
        {
            if(campaign==null)return Result<CampaignState>.Failure("GC017H_CAMPAIGN_REQUIRED");var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var active=strategic?.ActiveDefense;
            if(active==null)return Result<CampaignState>.Failure("GC017H_DEFENSE_REQUIRED");if(active.Status!=CityDefenseStatus017H.Completed&&active.Status!=CityDefenseStatus017H.Failed)return Result<CampaignState>.Failure("GC017H_DEFENSE_TERMINAL_REQUIRED");
            var won=active.Status==CityDefenseStatus017H.Completed;var next=strategic.With(activeDefense:null,replaceActiveDefense:true,totalDefensesWon:strategic.TotalDefensesWon+(won?1:0),totalDefensesLost:strategic.TotalDefensesLost+(won?0:1),lastCheckpointId:"defense_finalized");
            return Success(campaign,city.With(strategic017H:next,replaceStrategic017H:true,civicTrust:city.CivicTrust+(won?2:0),lastCheckpointId:"defense_finalized"));
        }

        private Result<CampaignState> ResolveSupportWave(CampaignState campaign,GuildCityStrategicContent017H content,DefenseProfileDefinition017H profile,DefenseWaveDefinition017H wave,string waveId,BuildingContributionSnapshot017H snapshot)
        {
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var active=strategic.ActiveDefense;
            var unionPower=AssignedUnions(active,campaign.Guild.Unions).Count*4;var total=snapshot.DefensePowerFlat+snapshot.BarrierIntegrityFlat+snapshot.ScoutingFlat+snapshot.ReinforcementReadinessFlat+unionPower;
            var success=total>=wave.SupportResolutionThreshold;var loss=success?Math.Max(0,wave.EnemyPower-total):Math.Max(3,wave.EnemyPower-total);var updated=CompleteWave(active,waveId,success,loss,profile.Waves.Length,finalWaveLossIsFailure:false);
            var receiptId="DEFENSE_SUPPORT_"+CanonicalJson.Sha256Hex(new{active.OperationId,waveId,total,wave.EnemyPower,success}).Substring(0,24).ToUpperInvariant();
            if(Contains(strategic.AppliedStrategicReceiptIds,receiptId))return Result<CampaignState>.Success(campaign);
            var applied=new List<string>(strategic.AppliedStrategicReceiptIds){receiptId};applied.Sort(StringComparer.Ordinal);
            var next=strategic.With(activeDefense:updated,replaceActiveDefense:true,appliedStrategicReceiptIds:applied.AsReadOnly(),defenseMasteryXp:strategic.DefenseMasteryXp+wave.DefenseMasteryXp,lastCheckpointId:"defense_support_wave_resolved");
            var reward=ApplyInstitutionalReward(campaign,receiptId,wave.RewardGuildXp,wave.RewardCivicHallXp);city=reward.Guild.GuildCity.With(strategic017H:next,replaceStrategic017H:true,lastCheckpointId:"defense_support_wave_resolved");
            return Result<CampaignState>.Success(reward.With(reward.Guild.WithGuildCity(city),reward.OpeningFlow));
        }
        private static CityDefenseOperationState017H CompleteWave(
            CityDefenseOperationState017H active,
            string waveId,
            bool success,
            int integrityLoss,
            int totalWaveCount,
            bool finalWaveLossIsFailure)
        {
            var resolved = new List<string>(active.ResolvedWaveIds);
            if (!resolved.Contains(waveId)) resolved.Add(waveId);
            resolved.Sort(StringComparer.Ordinal);
            var nextIndex = active.CurrentWaveIndex + 1;
            var integrity = Math.Max(0, active.CityIntegrity - integrityLoss);
            var finalWave = nextIndex >= Math.Max(1, totalWaveCount);
            var terminal = integrity <= 0 || (finalWave && finalWaveLossIsFailure && !success)
                ? CityDefenseStatus017H.Failed
                : finalWave
                    ? CityDefenseStatus017H.Completed
                    : CityDefenseStatus017H.Active;
            return active.With(
                currentWaveIndex: nextIndex,
                status: terminal,
                cityIntegrity: integrity,
                resolvedWaveIds: resolved.AsReadOnly(),
                pendingWaveId: string.Empty,
                lastCheckpointId: success ? "defense_wave_held" : "defense_wave_breached");
        }
        private static CampaignState ApplyInstitutionalReward(CampaignState campaign,string rewardId,long guildXp,long hallXp)
        {
            guildXp=Math.Max(1,guildXp);hallXp=Math.Max(1,hallXp);var development=campaign.Guild.Development.RecordBattleReward(rewardId,guildXp,hallXp);var guild=campaign.Guild.With(checked(campaign.Guild.TreasuryXp+guildXp),campaign.Guild.Recruits,campaign.Guild.Unions,campaign.Guild.Inventory,development);return campaign.With(guild,campaign.OpeningFlow);
        }
        private static IReadOnlyList<string> AssignedUnions(CityDefenseOperationState017H active,IReadOnlyList<UnionState> owned){var result=new List<string>();for(var i=0;i<active.LaneAssignments.Count;i++)for(var j=0;j<active.LaneAssignments[i].UnionIds.Count;j++)if(OwnsUnion(owned,active.LaneAssignments[i].UnionIds[j])&&!result.Contains(active.LaneAssignments[i].UnionIds[j]))result.Add(active.LaneAssignments[i].UnionIds[j]);if(result.Count==0)for(var i=0;i<owned.Count&&result.Count<10;i++)if(owned[i].Kind==UnionKind.Normal&&owned[i].MemberRecruitIds.Count>0)result.Add(owned[i].UnionId);result.Sort(StringComparer.Ordinal);return result.AsReadOnly();}
        private static bool OwnsUnion(IReadOnlyList<UnionState> values,string id){if(values!=null)for(var i=0;i<values.Count;i++)if(values[i].Kind==UnionKind.Normal&&StringComparer.Ordinal.Equals(values[i].UnionId,id))return true;return false;}
        private static bool Contains(IReadOnlyList<string> values,string value){if(string.IsNullOrWhiteSpace(value))return true;if(values!=null)for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i],value))return true;return false;}
        private static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city)=>Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
    }
}
