using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.TitanTrials160;

namespace SecondDimension.Gameplay.Navigation164
{
    [Serializable]
    public sealed class LoopMemberProgress164
    {
        [JsonConstructor]
        public LoopMemberProgress164(string memberId,RecruitProgressionState progression)
        {if(string.IsNullOrWhiteSpace(memberId)||progression==null)throw new ArgumentException("Invalid checkpoint member.");MemberId=memberId;Progression=progression;}
        public string MemberId {get;}
        public RecruitProgressionState Progression {get;}
    }
    // Immutable execution checkpoints. Progress, inventories and reward ledgers
    // are deliberately never restored from these records.
    [Serializable]
    public sealed class LoopExecution164
    {
        [JsonConstructor]
        public LoopExecution164(string loopId, int operationOrdinal, BattleState battle,
            ContractCommitState017D contract, ExpeditionState017D expedition,
            EncounterLaunchRequest017D encounter, BattleReturnReceipt017D battleReturn,
            CampaignProgressState019 campaign, CampaignProgressionState022 tower,
            TitanAttempt160 titan, CampaignRecovery150 recovery, string authorityHash = null,
            IReadOnlyList<LoopMemberProgress164> members = null, IReadOnlyList<LoopMemberProgress164> rewardMembers = null,
            CityDefenseOperationState017H defense = null)
        {
            if(!LoopCheckpoint164.Adventure(loopId) || operationOrdinal<0)
                throw new ArgumentException("Invalid loop checkpoint.");
            if(loopId!=LoopCheckpoint164.Campaign&&(campaign!=null||recovery!=null||contract!=null||expedition!=null||defense!=null)||
                loopId!=LoopCheckpoint164.Tower&&tower!=null||loopId!=LoopCheckpoint164.Titans&&titan!=null)
                throw new ArgumentException("The checkpoint contains another activity's execution owner.");
            if(tower!=null&&(tower.ActiveAbyssOperation==null||tower.ActiveAbyssOperation.CommittedOperationOrdinal!=operationOrdinal)||
                titan!=null&&(encounter==null||encounter.BattleId!=titan.BattleId))
                throw new ArgumentException("The checkpoint execution identity is inconsistent.");
            LoopId=loopId; OperationOrdinal=operationOrdinal; Battle=battle;
            Contract=contract; Expedition=expedition; Encounter=encounter; BattleReturn=battleReturn;
            Campaign=campaign; Tower=tower; Titan=titan; Recovery=recovery;
            Defense=defense;
            Members=CopyMembers(members);RewardMembers=CopyMembers(rewardMembers);
            var authority=JObject.FromObject(new {LoopId,OperationOrdinal,Battle,Contract,Expedition,Encounter,BattleReturn,Campaign,Tower,Titan,Recovery});
            // Optional extensions retain the hash shape of checkpoints written
            // before these baselines existed; old saves still omit Loops164.
            if(Members!=null)authority["Members"]=JToken.FromObject(Members);
            if(RewardMembers!=null)authority["RewardMembers"]=JToken.FromObject(RewardMembers);
            if(Defense!=null)authority["Defense"]=JToken.FromObject(Defense);
            var expected=CanonicalJson.Sha256Hex(authority);
            if(authorityHash!=null&&authorityHash!=expected)throw new ArgumentException("Loop checkpoint integrity mismatch.");
            AuthorityHash=expected;
            if(encounter!=null && battle!=null && encounter.BattleId!=battle.BattleId)
                throw new ArgumentException("The checkpoint encounter does not own its battle.");
        }
        static IReadOnlyList<LoopMemberProgress164> CopyMembers(IReadOnlyList<LoopMemberProgress164> source)
        {
            if(source==null)return null;
            var values=source.ToArray();
            if(values.Any(x=>x==null)||values.Select(x=>x.MemberId).Distinct().Count()!=values.Length)throw new ArgumentException("Duplicate checkpoint member.");
            return Array.AsReadOnly(values.OrderBy(x=>x.MemberId,StringComparer.Ordinal).ToArray());
        }
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public IReadOnlyList<LoopMemberProgress164> Members {get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public IReadOnlyList<LoopMemberProgress164> RewardMembers {get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public CityDefenseOperationState017H Defense {get;}
        public string AuthorityHash {get;}
        public string LoopId {get;}
        public int OperationOrdinal {get;}
        public BattleState Battle {get;}
        public ContractCommitState017D Contract {get;}
        public ExpeditionState017D Expedition {get;}
        public EncounterLaunchRequest017D Encounter {get;}
        public BattleReturnReceipt017D BattleReturn {get;}
        public CampaignProgressState019 Campaign {get;}
        public CampaignProgressionState022 Tower {get;}
        public TitanAttempt160 Titan {get;}
        public CampaignRecovery150 Recovery {get;}
    }

    [Serializable]
    public sealed class LoopCheckpointState164
    {
        [JsonConstructor]
        public LoopCheckpointState164(string profileId,string activeLoop,IReadOnlyList<LoopExecution164> checkpoints)
        {
            if(string.IsNullOrWhiteSpace(profileId)||!LoopCheckpoint164.Valid(activeLoop))
                throw new ArgumentException("Invalid loop navigation state.");
            var entries=(checkpoints??Array.Empty<LoopExecution164>()).ToArray();
            if(entries.Any(x=>x==null)||entries.Select(x=>x.LoopId).Distinct().Count()!=entries.Length||entries.Length>3||entries.Where(x=>x.Battle!=null).Select(x=>x.Battle.BattleId).Distinct().Count()!=entries.Count(x=>x.Battle!=null))
                throw new ArgumentException("Duplicate loop checkpoint.");
            if(entries.Any(x=>x.Titan!=null&&x.Titan.ProfileId!=profileId||x.Recovery!=null&&x.Recovery.CampaignGuid!=profileId))
                throw new ArgumentException("Checkpoint belongs to a different profile.");
            ProfileId=profileId;ActiveLoop=activeLoop;
            Checkpoints=Array.AsReadOnly(entries.OrderBy(x=>x.LoopId,StringComparer.Ordinal).ToArray());
        }
        public string ProfileId {get;}
        public string ActiveLoop {get;}
        public IReadOnlyList<LoopExecution164> Checkpoints {get;}
        public LoopExecution164 Find(string loop)=>Checkpoints.FirstOrDefault(x=>x.LoopId==loop);
    }

    public static class LoopCheckpoint164
    {
        public const string Campaign="CAMPAIGN",Tower="TOWER",Titans="TITANS",Town="TOWN";
        public static bool Adventure(string id)=>id==Campaign||id==Tower||id==Titans;
        public static bool Valid(string id)=>Adventure(id)||id==Town;
        // Editing a formation while another activity is parked uses the existing
        // next-battle plan. The committed Guild roster remains its authority root.
        public static bool Unfinished(LoopExecution164 x)=>x!=null&&(x.Defense!=null||x.Tower?.ActiveAbyssOperation!=null||x.Campaign?.ActiveOperation!=null||
            x.Campaign?.PendingReceipt!=null||x.Campaign?.Playable020?.ActiveOperation!=null||x.Campaign?.Playable020?.WorldGate023?.ActiveOperation!=null||
            x.Recovery?.Paused==true||x.Titan!=null||x.Encounter!=null||x.BattleReturn!=null||
            x.Contract!=null&&!x.Contract.Completed&&!x.Contract.Failed||
            x.Battle!=null&&(x.Battle.Outcome==BattleOutcome.InProgress||x.Battle.Reward?.Claimed!=true));
        public static bool HasParked(CampaignState s,string loop)=>s?.Loops164!=null&&s.Loops164.ActiveLoop!=loop&&Unfinished(s.Loops164.Find(loop));
        public static bool HasParkedRoster(CampaignState s)=>s?.Loops164?.Checkpoints.Any(x=>x.LoopId!=s.Loops164.ActiveLoop&&Unfinished(x))==true;
        static CampaignProgressState019 Progress(CampaignState s)=>s.Guild.GuildCity.Strategic017H.Campaign019;
        public static string BattleOwner(CampaignState s)
        {
            var id=s?.Battle?.BattleId;var encounter=s?.Guild?.GuildCity?.PendingEncounter;
            if(s?.TitanTrials160?.Active!=null&&(id==s.TitanTrials160.Active.BattleId||encounter?.BattleId==s.TitanTrials160.Active.BattleId))return Titans;
            if(id!=null&&s.TitanTrials160?.Settlements.Any(x=>x.BattleId==id)==true)return Titans;
            if(encounter?.RequestId.StartsWith("ABYSS_ENCOUNTER022_",StringComparison.Ordinal)==true||
                id?.StartsWith("TOWER",StringComparison.Ordinal)==true||id?.StartsWith("ABYSS",StringComparison.Ordinal)==true)return Tower;
            return Campaign;
        }
        public static string Current(CampaignState s)
        {
            if(s?.Loops164!=null)
            {
                if(s.Loops164.ProfileId!=s.CampaignGuid)throw new ArgumentException("Loop profile mismatch.");
                return s.Loops164.ActiveLoop;
            }
            if(s?.Battle!=null&&(s.Battle.Outcome==BattleOutcome.InProgress||s.Battle.Reward?.Claimed!=true))return BattleOwner(s);
            if(s?.TitanTrials160?.Active!=null)return Titans;
            if(s?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022?.ActiveAbyssOperation!=null)return Tower;
            return Campaign;
        }
        public static int ExecutionOrdinal(CampaignState s)
        {
            var saved=s?.Loops164?.Find(Current(s));var city=s?.Guild?.GuildCity;
            return saved!=null&&saved.OperationOrdinal<=city.OperationOrdinal&&
                saved.Encounter?.RequestId==city.PendingEncounter?.RequestId&&saved.Encounter!=null
                ? saved.OperationOrdinal:city?.OperationOrdinal??0;
        }
        public static bool MatchesTower(CampaignState s,AbyssOperationState022 active)
        {
            var saved=s?.Loops164?.Find(Tower)?.Tower?.ActiveAbyssOperation;
            return Current(s)==Tower&&saved!=null&&active!=null&&saved.OperationInstanceId==active.OperationInstanceId&&
                saved.BeginAuthorityHash==active.BeginAuthorityHash&&saved.CommittedOperationOrdinal==active.CommittedOperationOrdinal&&
                active.CommittedOperationOrdinal<=s.Guild.GuildCity.OperationOrdinal;
        }
        public static int WorldOrdinal(CampaignState s,int fallback)
        {
            var saved=s?.Loops164?.Find(Campaign);var current=Progress(s).Playable020.WorldGate023.ActiveOperation;
            return Current(s)==Campaign&&saved?.Campaign?.Playable020?.WorldGate023?.ActiveOperation?.OperationId==current?.OperationId&&
                current!=null&&saved.Contract!=null&&saved.Contract.AcceptedOperationOrdinal<=s.Guild.GuildCity.OperationOrdinal
                ? saved.Contract.AcceptedOperationOrdinal:fallback;
        }
        static IReadOnlyList<LoopMemberProgress164> CaptureMembers(CampaignState s,BattleState battle)
        {
            if(battle==null)return null;
            var ids=new HashSet<string>(battle.PlayerUnions.SelectMany(x=>x.Members).Select(x=>x.MemberId),StringComparer.Ordinal);
            return s.Guild.Recruits.Where(x=>ids.Contains(x.RecruitId)).Select(x=>new LoopMemberProgress164(x.RecruitId,x.Progression)).ToArray();
        }
        public static Result<CampaignState> Switch(CampaignState s,string destination)
        {
            if(s?.Guild?.GuildCity==null||!Valid(destination)||s.Loops164!=null&&s.Loops164.ProfileId!=s.CampaignGuid)return Result<CampaignState>.Failure("Choose a valid activity.");
            if(s.Loops164?.Checkpoints.Any(x=>x.OperationOrdinal>s.Guild.GuildCity.OperationOrdinal)==true)
                return Result<CampaignState>.Failure("A saved activity has an invalid operation counter.");
            if(s.Loops164?.ActiveLoop==destination)return Result<CampaignState>.Success(s);
            try
            {
                var entries=(s.Loops164?.Checkpoints??Array.Empty<LoopExecution164>()).ToDictionary(x=>x.LoopId,StringComparer.Ordinal);
                var current=Current(s);var owner=BattleOwner(s);var city=s.Guild.GuildCity;var progress=Progress(s);
                var playable=progress.Playable020;var tower=playable.Progression022;
                foreach(var loop in new[]{Campaign,Tower,Titans})
                {
                    var battle=owner==loop?s.Battle:null;
                    var encounter=owner==loop?city.PendingEncounter:null;
                    var ret=owner==loop?city.PendingBattleReturn:null;
                    var isCampaign=loop==Campaign;
                    var hasCampaign=isCampaign&&(city.Strategic017H.ActiveDefense!=null||progress.ActiveOperation!=null||progress.PendingReceipt!=null||playable.ActiveOperation!=null||
                        playable.WorldGate023.ActiveOperation!=null||city.ActiveContract!=null||city.Expedition!=null||s.Recovery150!=null);
                    var hasTower=loop==Tower&&tower.ActiveAbyssOperation!=null;
                    var hasTitan=loop==Titans&&s.TitanTrials160?.Active!=null;
                    if(battle!=null||encounter!=null||ret!=null||hasCampaign||hasTower||hasTitan)
                    {
                        var previous=entries.ContainsKey(loop)?entries[loop]:null;
                        var ordinal=hasTower?tower.ActiveAbyssOperation.CommittedOperationOrdinal:
                            hasCampaign&&city.ActiveContract!=null?city.ActiveContract.AcceptedOperationOrdinal:
                            previous?.Encounter?.RequestId==encounter?.RequestId&&encounter!=null?previous.OperationOrdinal:city.OperationOrdinal;
                        var sameBattle=battle!=null&&previous?.Battle?.BattleId==battle.BattleId&&previous.Battle.Reward?.Claimed!=true&&
                            previous.Battle.InitialBattleStateHash==battle.InitialBattleStateHash&&
                            previous.Battle.InitialIntegrityStateHash090==battle.InitialIntegrityStateHash090;
                        var members=sameBattle&&previous.Members!=null?previous.Members:CaptureMembers(s,battle);
                        var rewardMembers=sameBattle&&previous.Battle.Reward?.RewardId==battle.Reward?.RewardId&&previous.RewardMembers!=null?
                            previous.RewardMembers:battle?.Reward!=null&&!battle.Reward.Claimed?CaptureMembers(s,battle):null;
                        entries[loop]=new LoopExecution164(loop,Math.Max(0,ordinal),battle,isCampaign?city.ActiveContract:null,
                            isCampaign?city.Expedition:null,encounter,ret,hasCampaign?progress:null,hasTower?tower:null,
                            hasTitan?s.TitanTrials160.Active:null,isCampaign?s.Recovery150:null,members:members,rewardMembers:rewardMembers,
                            defense:isCampaign?city.Strategic017H.ActiveDefense:null);
                    }
                    else if(loop==current)entries.Remove(loop);
                }
                // Clear execution owners only. All monotonic progress and global
                // receipt ledgers remain in their live records.
                var gate=playable.WorldGate023.With(activeOperation:null,replaceActiveOperation:true,activeNodeLedger:Array.Empty<WorldGateNodeReceipt023>());
                tower=tower.With(activeAbyssOperation:null,replaceActiveAbyssOperation:true,activeAbyssBeginGrant:null,
                    replaceActiveAbyssBeginGrant:true,activeAbyssStepLedger:Array.Empty<AbyssStepReceiptProof022>());
                playable=playable.With(activeOperation:null,replaceActiveOperation:true,activeOperationGrant:null,replaceActiveOperationGrant:true,
                    activeStepLedger:Array.Empty<CampaignStepReceipt020>(),worldGate023:gate,replaceWorldGate023:true,progression022:tower,replaceProgression022:true);
                progress=progress.With(activeOperation:null,replaceActiveOperation:true,pendingReceipt:null,replacePendingReceipt:true,playable020:playable,replacePlayable020:true);
                city=city.With(activeContract:null,replaceActiveContract:true,expedition:null,replaceExpedition:true,pendingEncounter:null,
                    replacePendingEncounter:true,pendingBattleReturn:null,replacePendingBattleReturn:true,
                    strategic017H:city.Strategic017H.With(campaign019:progress,replaceCampaign019:true,activeDefense:null,replaceActiveDefense:true),replaceStrategic017H:true);
                var next=s.With(s.Guild.WithGuildCity(city),s.OpeningFlow).WithBattle(null).WithRecovery150(null)
                    .WithTitanTrials160(s.TitanTrials160?.WithActive(null))
                    .WithLoops164(new LoopCheckpointState164(s.CampaignGuid,destination,entries.Values.ToArray()));
                if(!entries.TryGetValue(destination,out var saved))return Result<CampaignState>.Success(next);
                city=next.Guild.GuildCity;progress=Progress(next);playable=progress.Playable020;
                if(saved.Campaign!=null)
                {
                    var cp=saved.Campaign;var pp=cp.Playable020;
                    gate=playable.WorldGate023.With(activeOperation:pp.WorldGate023.ActiveOperation,replaceActiveOperation:true,activeNodeLedger:pp.WorldGate023.ActiveNodeLedger);
                    playable=playable.With(activeOperation:pp.ActiveOperation,replaceActiveOperation:true,activeOperationGrant:pp.ActiveOperationGrant,
                        replaceActiveOperationGrant:true,activeStepLedger:pp.ActiveStepLedger,worldGate023:gate,replaceWorldGate023:true);
                    progress=progress.With(activeOperation:cp.ActiveOperation,replaceActiveOperation:true,pendingReceipt:cp.PendingReceipt,
                        replacePendingReceipt:true,playable020:playable,replacePlayable020:true);
                }
                if(saved.Tower!=null)
                {
                    tower=playable.Progression022.With(activeAbyssOperation:saved.Tower.ActiveAbyssOperation,replaceActiveAbyssOperation:true,
                        activeAbyssBeginGrant:saved.Tower.ActiveAbyssBeginGrant,replaceActiveAbyssBeginGrant:true,activeAbyssStepLedger:saved.Tower.ActiveAbyssStepLedger);
                    playable=playable.With(progression022:tower,replaceProgression022:true);
                    progress=progress.With(playable020:playable,replacePlayable020:true);
                }
                city=city.With(activeContract:saved.Contract,replaceActiveContract:true,expedition:saved.Expedition,replaceExpedition:true,
                    pendingEncounter:saved.Encounter,replacePendingEncounter:true,pendingBattleReturn:saved.BattleReturn,replacePendingBattleReturn:true,
                    strategic017H:city.Strategic017H.With(campaign019:progress,replaceCampaign019:true,activeDefense:saved.Defense,replaceActiveDefense:true),replaceStrategic017H:true);
                next=next.With(next.Guild.WithGuildCity(city),next.OpeningFlow).WithBattle(saved.Battle).WithRecovery150(saved.Recovery);
                if(saved.Titan!=null)next=next.WithTitanTrials160(next.TitanTrials160.WithActive(saved.Titan));
                return Result<CampaignState>.Success(next);
            }
            catch(Exception e){return Result<CampaignState>.Failure("The saved activity could not be switched: "+e.Message);}
        }
    }
}
