using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017D
{
    public sealed class GuildCityBattleBridgeService017D
    {
        public const string EncounterRequestAuthorityPrefix084="ENCREQAUTH084_";
        public const string BattleReturnApplyAuthorityPrefix084=
            "BATTLEAPPLYAUTH084_";

        public static string EncounterRequestAuthorityId084(
            EncounterLaunchRequest017D request) => request==null?string.Empty:
            EncounterRequestAuthorityPrefix084+CanonicalJson.Sha256Hex(request)
                .Substring(0,24).ToUpperInvariant();

        public static string BattleReturnApplyAuthorityId084(
            BattleReturnReceipt017D receipt) => receipt==null?string.Empty:
            BattleReturnApplyAuthorityPrefix084+CanonicalJson.Sha256Hex(receipt)
                .Substring(0,24).ToUpperInvariant();

        public static bool RequiresEncounterRequestAuthority084(
            EncounterLaunchRequest017D request) => request!=null;

        public Result<CampaignState> StartCertifiedEncounter(CampaignState campaign, M2BattleCommandService battles, M2CombatContent content)
        {
            return StartCertifiedEncounter(campaign, battles, content, null);
        }

        public Result<CampaignState> StartCertifiedEncounter(
            CampaignState campaign,
            M2BattleCommandService battles,
            M2CombatContent content,
            EncounterRosterResolver070 encounterRosterResolver)
        {
            if(campaign==null||battles==null||content==null)return Result<CampaignState>.Failure("GC017D_BATTLE_INPUT_REQUIRED");
            var request=campaign.Guild.GuildCity.PendingEncounter;
            if(request==null)return Result<CampaignState>.Failure("GC017D_PENDING_ENCOUNTER_REQUIRED");
            Result<CampaignState> started;
            if (encounterRosterResolver == null)
            {
                try
                {
                    EnemyForceProfile094.ValidateRoster094(request, null);
                }
                catch (InvalidOperationException exception)
                {
                    return Result<CampaignState>.Failure(
                        "GC017D_ENEMY_ROSTER_070_REJECTED: " + exception.Message);
                }
                started=battles.StartCommittedEncounterBattle017D(
                    campaign, content, request);
            }
            else
            {
                try
                {
                    var roster = encounterRosterResolver.Resolve(campaign.CampaignSeed, request);
                    if (roster.Unions.Count != request.EnemyUnionCount)
                        return Result<CampaignState>.Failure("GC017D_ENEMY_ROSTER_070_COUNT_MISMATCH");
                    started = battles.StartCommittedEncounterBattle017D(
                        campaign, content, request, roster);
                }
                catch (Exception exception)
                {
                    return Result<CampaignState>.Failure(
                        "GC017D_ENEMY_ROSTER_070_REJECTED: " + exception.Message);
                }
            }
            if(!started.IsSuccess)return started;
            if(started.Value.Battle==null||!StringComparer.Ordinal.Equals(started.Value.Battle.BattleId,request.BattleId))
                return Result<CampaignState>.Failure("GC017D_BATTLE_ID_MISMATCH");
            return started;
        }

        public Result<CampaignState> CommitBattleReturn(CampaignState campaign)
        {
            if(!TryCreateCanonicalBattleReturn084(campaign,out var receipt,out var error))
                return Result<CampaignState>.Failure(error);
            var city=campaign.Guild.GuildCity;
            var request=city.PendingEncounter;
            if(city.PendingBattleReturn!=null)
            {
                if(!StringComparer.Ordinal.Equals(city.PendingBattleReturn.LaunchRequestId,request.RequestId)||
                   !StringComparer.Ordinal.Equals(city.PendingBattleReturn.BattleRunId,request.BattleId))
                    return Result<CampaignState>.Failure("GC017D_BATTLE_RETURN_ID_MISMATCH");
                if(!SameBattleReturn084(city.PendingBattleReturn,receipt))
                    return Result<CampaignState>.Failure(
                        "GC017D_BATTLE_RETURN_RECEIPT_MISMATCH");
                return Result<CampaignState>.Success(campaign);
            }
            return Success(campaign,city.With(pendingBattleReturn:receipt,replacePendingBattleReturn:true,lastCheckpointId:"battle_return_committed"));
        }

        public Result<CampaignState> ApplyBattleReturnExactlyOnce(CampaignState campaign)
        {
            if(campaign==null)return Result<CampaignState>.Failure("GC017D_CAMPAIGN_REQUIRED");
            var city=campaign.Guild.GuildCity; var receipt=city.PendingBattleReturn;
            if(receipt==null)
            {
                if(city.PendingEncounter==null && city.AppliedBattleReturnIds.Count>0 &&
                    StringComparer.Ordinal.Equals(city.LastCheckpointId,"battle_return_applied"))
                    return Result<CampaignState>.Success(campaign);
                return Result<CampaignState>.Failure("GC017D_BATTLE_RETURN_REQUIRED");
            }
            if(Contains(city.AppliedBattleReturnIds,receipt.ReceiptId))return Result<CampaignState>.Success(campaign);
            var request=city.PendingEncounter;
            if(request==null)return Result<CampaignState>.Failure("GC017D_PENDING_ENCOUNTER_REQUIRED");
            if(!StringComparer.Ordinal.Equals(receipt.LaunchRequestId,request.RequestId)||
               !StringComparer.Ordinal.Equals(receipt.BattleRunId,request.BattleId))
                return Result<CampaignState>.Failure("GC017D_BATTLE_RETURN_ID_MISMATCH");
            if(!TryCreateCanonicalBattleReturn084(campaign,out var expected,out var error))
                return Result<CampaignState>.Failure(error);
            if(!SameBattleReturn084(receipt,expected))
                return Result<CampaignState>.Failure(
                    "GC017D_BATTLE_RETURN_RECEIPT_MISMATCH");
            if(campaign.Battle==null||!StringComparer.Ordinal.Equals(campaign.Battle.BattleId,request.BattleId))
                return Result<CampaignState>.Failure("GC017D_BATTLE_ID_MISMATCH");
            if(campaign.Battle?.Reward==null||!campaign.Battle.Reward.Claimed)return Result<CampaignState>.Failure("GC017D_CLAIM_EXISTING_BATTLE_REWARD_FIRST");
            if(!StringComparer.Ordinal.Equals(receipt.EquipmentRewardReceiptId,campaign.Battle.Reward.RewardId))
                return Result<CampaignState>.Failure("GC017D_BATTLE_REWARD_ID_MISMATCH");
            var returnAuthorityId=BattleReturnApplyAuthorityId084(expected);
            if(campaign.Guild.Development.HasAdventureAuthority(returnAuthorityId))
                return Result<CampaignState>.Failure(
                    "GC017D_GLOBAL_BATTLE_RETURN_AUTHORITY_ALREADY_APPLIED");
            if(!campaign.Guild.Development.CanRecordAdventureAuthority(
                   returnAuthorityId))
                return Result<CampaignState>.Failure(
                    "GC017D_ADVENTURE_AUTHORITY_CAP_REACHED");
            var materials=MergeMaterials(city.Materials,receipt.MaterialRewards); var memories=new List<RelationshipMemoryState017D>(city.RelationshipMemories);
            for(var i=0;i<receipt.RelationshipMemories.Count;i++)if(!ContainsMemory(memories,receipt.RelationshipMemories[i].MemoryId))memories.Add(receipt.RelationshipMemories[i]);
            var applied=new List<string>(city.AppliedBattleReturnIds){receipt.ReceiptId}; applied.Sort(StringComparer.Ordinal);
            var expedition=city.Expedition;
            if(expedition==null)return Result<CampaignState>.Failure("GC017D_EXPEDITION_REQUIRED");
            var outcome=receipt.Outcome;
            var status=StringComparer.Ordinal.Equals(outcome,BattleOutcome.Defeat.ToString())?ExpeditionStatus017D.Failed:ExpeditionStatus017D.Active;
            var objectives=new List<string>(expedition.ObjectiveFlags); for(var i=0;i<receipt.ObjectiveFlags.Count;i++)if(!objectives.Contains(receipt.ObjectiveFlags[i]))objectives.Add(receipt.ObjectiveFlags[i]); objectives.Sort(StringComparer.Ordinal);
            expedition=expedition.With(status:status,supplies:Math.Max(0,expedition.Supplies-receipt.SupplyConsumption),fatigue:Math.Max(0,expedition.Fatigue+receipt.FatigueDelta),
                urgency:Math.Max(0,expedition.Urgency+receipt.UrgencyDelta),objectiveFlags:objectives.AsReadOnly(),lastCheckpointId:receipt.ReturnCheckpointId);
            var plots = GuildCityCommandService017D.AdvanceOpeningCityProject(
                city.CityPlots, receipt.CityProjectContribution);
            var updated=city.With(materials:materials.AsReadOnly(),relationshipMemories:memories.AsReadOnly(),cityPlots:plots,expedition:expedition,replaceExpedition:true,
                pendingEncounter:null,replacePendingEncounter:true,pendingBattleReturn:null,replacePendingBattleReturn:true,appliedBattleReturnIds:applied.AsReadOnly(),lastCheckpointId:"battle_return_applied");
            var development=campaign.Guild.Development.RecordAdventureAuthority(
                returnAuthorityId);
            var guild=campaign.Guild.With(campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,campaign.Guild.Unions,
                campaign.Guild.Inventory,development).WithGuildCity(updated);
            return Result<CampaignState>.Success(campaign.With(
                guild,campaign.OpeningFlow));
        }

        public static bool TryCreateCanonicalBattleReturn084(
            CampaignState campaign,
            out BattleReturnReceipt017D receipt,
            out string error)
        {
            receipt=null;
            error="GC017D_TERMINAL_BATTLE_REQUIRED";
            if(campaign?.Battle==null||campaign.Guild?.GuildCity==null)return false;
            var city=campaign.Guild.GuildCity;
            var request=city.PendingEncounter;
            if(request==null)
            {
                error="GC017D_PENDING_ENCOUNTER_REQUIRED";
                return false;
            }
            return TryCreateCanonicalBattleReturn084(campaign.Battle,request,
                SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.ExecutionOrdinal(campaign),out receipt,out error);
        }

        public static bool TryCreateCanonicalBattleReturn084(
            BattleState battle,
            EncounterLaunchRequest017D request,
            int operationOrdinal,
            out BattleReturnReceipt017D receipt,
            out string error)
        {
            receipt=null;
            error="GC017D_TERMINAL_BATTLE_REQUIRED";
            if(battle==null)return false;
            if(request==null)
            {
                error="GC017D_PENDING_ENCOUNTER_REQUIRED";
                return false;
            }
            if(!StringComparer.Ordinal.Equals(battle.BattleId,request.BattleId))
            {
                error="GC017D_BATTLE_ID_MISMATCH";
                return false;
            }
            if(battle.Outcome==BattleOutcome.InProgress||battle.Phase!=BattlePhase.Resolved)
                return false;
            if(battle.Reward==null)
            {
                error="GC017D_BATTLE_REWARD_REQUIRED";
                return false;
            }
            if(battle.Reward.Outcome!=battle.Outcome||
               !M2BattleCommandService.HasValidFinalStateHash090(battle))
            {
                error="GC017D_BATTLE_AUTHORITY_INVALID";
                return false;
            }

            var hash=CanonicalJson.Sha256Hex(new
            {
                request.RequestId,battle.BattleId,battle.FinalStateHash,
                battle.Outcome,battle.Reward.RewardId
            });
            var flags=new List<string>();
            if(battle.Outcome==BattleOutcome.Victory)
            {
                flags.Add("OBJECTIVE_ENCOUNTER_CLEARED");
                flags.Add(GuildCityExpeditionService017D.EncounterClearedFlag(
                    request.NodeId));
                if(request.EncounterId.IndexOf("RESCUE",
                       StringComparison.OrdinalIgnoreCase)>=0)
                    flags.Add("PRIMARY_OBJECTIVE_RESCUE_COMPLETE");
            }
            var materials=new List<GuildMaterialState017D>();
            if(battle.Outcome==BattleOutcome.Victory)
            {
                materials.Add(new GuildMaterialState017D("MAT_SALVAGED_TIMBER",4));
                materials.Add(new GuildMaterialState017D("MAT_GATE_IRON",2));
            }
            else materials.Add(new GuildMaterialState017D("MAT_SALVAGED_TIMBER",1));
            var memories=new List<RelationshipMemoryState017D>();
            if(battle.PlayerUnions.Count>0&&battle.PlayerUnions[0].Members.Count>=2)
            {
                var first=battle.PlayerUnions[0].Members[0].MemberId;
                var second=battle.PlayerUnions[0].Members[1].MemberId;
                memories.Add(new RelationshipMemoryState017D(
                    "REL_MEMORY_"+hash.Substring(0,20).ToUpperInvariant(),first,
                    second,battle.BattleId,"They completed the encounter together.",
                    Math.Max(0,operationOrdinal),2,
                    "REL_SCENE_AFTER_"+battle.BattleId,false));
            }
            receipt=new BattleReturnReceipt017D(
                "BATTLE_RETURN_"+hash.Substring(0,24).ToUpperInvariant(),
                request.RequestId,battle.BattleId,battle.Outcome.ToString(),
                battle.FinalStateHash,1,
                battle.Outcome==BattleOutcome.Victory?1:2,-1,
                flags.AsReadOnly(),materials.AsReadOnly(),battle.Reward.RewardId,
                memories.AsReadOnly(),battle.Outcome==BattleOutcome.Victory?20:5,
                request.ReturnCheckpointId,false);
            error=string.Empty;
            return true;
        }

        private static bool SameBattleReturn084(
            BattleReturnReceipt017D actual,
            BattleReturnReceipt017D expected) =>
            actual!=null&&expected!=null&&StringComparer.Ordinal.Equals(
                CanonicalJson.Serialize(actual),CanonicalJson.Serialize(expected));

        private static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city)=>Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
        private static bool Contains(IReadOnlyList<string> values,string value){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i],value))return true;return false;}
        private static bool ContainsMemory(IReadOnlyList<RelationshipMemoryState017D> values,string value){for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].MemoryId,value))return true;return false;}
        private static List<GuildMaterialState017D> MergeMaterials(IReadOnlyList<GuildMaterialState017D> existing,IReadOnlyList<GuildMaterialState017D> additions)
        {
            var result=new List<GuildMaterialState017D>(existing);
            for(var i=0;i<additions.Count;i++)
            {
                var found=false;for(var j=0;j<result.Count;j++)if(StringComparer.Ordinal.Equals(result[j].MaterialId,additions[i].MaterialId)){result[j]=result[j].WithAmount(result[j].Amount+additions[i].Amount);found=true;break;}
                if(!found)result.Add(additions[i]);
            }
            result.Sort((a,b)=>StringComparer.Ordinal.Compare(a.MaterialId,b.MaterialId));return result;
        }
    }
}
