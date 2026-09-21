using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// Commits an already accepted sentient Covenant to one selected complete Union
    /// Forecast.  The public command selects only the Covenant; member Arts, the MP
    /// bearer, target and effect remain deterministic consequences of the Forecast.
    /// </summary>
    public sealed class CovenantBattleInvocationService022
    {
        public Result<CampaignState> InvokeAcceptedCovenantForecast(
            CampaignState campaign,
            ICampaignRegistry022 registry,
            CampaignProgressionCommandService022 progressionAuthority,
            M2BattleCommandService battleCommands,
            M2CombatContent combatContent,
            string covenantId)
        {
            if(campaign==null||registry==null||progressionAuthority==null||battleCommands==null||combatContent==null)
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_INPUT_REQUIRED");
            if(string.IsNullOrWhiteSpace(covenantId))return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_ID_REQUIRED");
            if(!progressionAuthority.HasCanonicalAcceptedCovenantAuthority(campaign,registry,covenantId,out var definition,out var acceptanceReceiptId))
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_ACCEPTED_AUTHORITY_REQUIRED");
            if(!IsCanonicalDefinition(definition)||!CovenantInvocationForecastAuthority022.TryCosts(definition.role,out var sharedApCost,out var personalMpCost))
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_LAW_INVALID");
            var battle=campaign.Battle;
            if(battle==null||battle.Outcome!=BattleOutcome.InProgress||battle.Phase!=BattlePhase.ForecastSelection)
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_ACTIVE_FORECAST_REQUIRED");
            if(!HasExactlyOneSelectionPerActiveUnion(battle))
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_COMPLETE_FORECAST_ROUND_REQUIRED");

            BattleUnionState selectedUnion=null;BattleForecastState selectedForecast=null;BattlePlannedActionState invokerAction=null;
            foreach(var selection in battle.Selections.OrderBy(x=>x.UnionId,StringComparer.Ordinal).ThenBy(x=>x.ForecastId,StringComparer.Ordinal))
            {
                var union=battle.PlayerUnions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId));
                var forecast=battle.CommittedForecasts.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,selection.ForecastId));
                if(union==null||forecast==null||union.IsDefeated||union.Retreated||!CovenantInvocationForecastAuthority022.RoleMatchesForecast(definition.role,forecast))continue;
                var action=forecast.MemberActions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ActorMemberId,union.LeaderMemberId))??forecast.MemberActions.OrderBy(x=>x.ActorMemberId,StringComparer.Ordinal).FirstOrDefault();
                var member=action==null?null:union.Members.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.MemberId,action.ActorMemberId));
                if(action==null||member==null||member.Downed)continue;
                if(forecast.SharedApCost+sharedApCost>union.CurrentAp||action.PersonalMpCost+personalMpCost>member.CurrentMp)continue;
                selectedUnion=union;selectedForecast=forecast;invokerAction=action;break;
            }
            if(selectedForecast==null)
            {
                var categoryExists=battle.Selections.Any(selection=>
                {
                    var forecast=battle.CommittedForecasts.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.UnionId,selection.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,selection.ForecastId));
                    return CovenantInvocationForecastAuthority022.RoleMatchesForecast(definition.role,forecast);
                });
                return Result<CampaignState>.Failure(categoryExists?"CAMPAIGN022_COVENANT_RESOURCE_BUDGET_REQUIRED":"CAMPAIGN022_COVENANT_FORECAST_ROLE_REQUIRED");
            }

            var invocation=CovenantInvocationForecastAuthority022.Create(campaign,battle,selectedForecast,definition,acceptanceReceiptId,invokerAction.ActorMemberId,sharedApCost,personalMpCost);
            var state=Progression(campaign);if(state==null)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_PROGRESSION_REQUIRED");
            if(state.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,invocation.ReceiptId))!=0)
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_RECEIPT_STATE_INCONSISTENT");
            var committed=ApplyToCompleteForecast(selectedForecast,invokerAction,invocation);
            var forecasts=new List<BattleForecastState>(battle.CommittedForecasts);
            var forecastIndex=forecasts.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,committed.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,committed.ForecastId));
            if(forecastIndex<0)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_SELECTED_FORECAST_REQUIRED");
            forecasts[forecastIndex]=committed;
            var pending=state.With(lastCheckpointId:CovenantInvocationForecastAuthority022.PendingCheckpoint(invocation.ReceiptId));
            var pendingCampaign=WithProgression(campaign,pending).WithBattle(battle.With(committedForecasts:forecasts.AsReadOnly()));
            var resolved=battleCommands.ConfirmRound(pendingCampaign,combatContent);
            if(!resolved.IsSuccess)return Result<CampaignState>.Failure(resolved.Errors.ToArray());
            var eventText=CovenantInvocationForecastAuthority022.EventText(selectedUnion.DisplayName,invocation);
            var evidence=resolved.Value.Battle.EventLog.Where(x=>StringComparer.Ordinal.Equals(x.EventType,CovenantInvocationForecastAuthority022.EventType)&&StringComparer.Ordinal.Equals(x.ArtId,invocation.CovenantId)&&StringComparer.Ordinal.Equals(x.MemberId,invocation.InvokerMemberId)&&StringComparer.Ordinal.Equals(x.Text,eventText)&&x.Amount>0).ToArray();
            if(evidence.Length!=1)return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_EVIDENCE_REQUIRED");
            var resolvedState=Progression(resolved.Value);
            if(resolvedState==null||!StringComparer.Ordinal.Equals(resolvedState.LastCheckpointId,CovenantInvocationForecastAuthority022.PendingCheckpoint(invocation.ReceiptId))||resolvedState.AppliedReceiptIds.Contains(invocation.ReceiptId))
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_BATTLE_PENDING_AUTHORITY_REQUIRED");
            if(!progressionAuthority.HasCanonicalAcceptedCovenantAuthority(resolved.Value,registry,covenantId,out _,out var resolvedAcceptanceReceiptId)||!StringComparer.Ordinal.Equals(acceptanceReceiptId,resolvedAcceptanceReceiptId))
                return Result<CampaignState>.Failure("CAMPAIGN022_COVENANT_ACCEPTED_AUTHORITY_REQUIRED");
            var applied=new List<string>(resolvedState.AppliedReceiptIds){invocation.ReceiptId};
            var finalState=resolvedState.With(appliedReceiptIds:applied.AsReadOnly(),lastCheckpointId:"campaign022_covenant_battle_invoked:"+invocation.CovenantId+":"+invocation.ReceiptId);
            return Result<CampaignState>.Success(WithProgression(resolved.Value,finalState));
        }

        static bool IsCanonicalDefinition(CovenantDto022 value)=>value!=null&&!value.sentientOwnershipAllowed&&value.voluntaryAcceptanceRequired&&value.usesSharedAp&&value.usesIndividualMp&&!value.directIndividualSelection&&!value.realMoneyGacha&&StringComparer.Ordinal.Equals(value.standardControl,"STORY_AND_COVENANT_FORECASTS")&&value.trustMaximum==100;

        static bool HasExactlyOneSelectionPerActiveUnion(BattleState battle)
        {
            var active=battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated).Select(x=>x.UnionId).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
            if(battle.Selections.Count!=active.Length||battle.Selections.Select(x=>x.UnionId).Distinct(StringComparer.Ordinal).Count()!=active.Length)return false;
            for(var i=0;i<active.Length;i++)
            {
                var matches=battle.Selections.Where(x=>StringComparer.Ordinal.Equals(x.UnionId,active[i])).ToArray();
                if(matches.Length!=1||battle.CommittedForecasts.Count(x=>StringComparer.Ordinal.Equals(x.UnionId,active[i])&&StringComparer.Ordinal.Equals(x.ForecastId,matches[0].ForecastId))!=1)return false;
            }
            return true;
        }

        static BattleForecastState ApplyToCompleteForecast(BattleForecastState forecast,BattlePlannedActionState invoker,CovenantForecastInvocation022 invocation)
        {
            var actions=new List<BattlePlannedActionState>();
            for(var i=0;i<forecast.MemberActions.Count;i++)
            {
                var action=forecast.MemberActions[i];
                actions.Add(!StringComparer.Ordinal.Equals(action.ActorMemberId,invoker.ActorMemberId)?action:new BattlePlannedActionState(action.ActorMemberId,action.ActorName,action.TargetUnionId,action.TargetMemberId,action.ArtId,action.ArtName,action.Kind,checked(action.SharedApCost+invocation.SharedApCost),checked(action.PersonalMpCost+invocation.PersonalMpCost),action.PredictedHpDelta,action.PredictedCohesionDelta,action.PredictedFormationDelta,action.Prediction+" · Great Covenant AP and personal MP are committed inside this complete Forecast.",action.MeaningfulUse,action.BreakthroughOpportunity,action.AnimationTag,action.Discipline,action.PredictedGrowth,action.BreakthroughTargetArtId,action.BreakthroughTargetArtName,action.AreaActionPlan095));
            }
            var marker=CovenantInvocationForecastAuthority022.Marker(invocation);
            var debug=forecast.DeterministicDebugEvidence+"\n"+CanonicalJson.Serialize(new{GreatCovenant=invocation.CovenantId,invocation.Role,invocation.AcceptanceReceiptId,invocation.InvokerMemberId,invocation.SharedApCost,invocation.PersonalMpCost,invocation.ReceiptId,DirectIndividualSelection=false});
            return new BattleForecastState(forecast.ForecastId,forecast.UnionId,forecast.CommandId,"GREAT COVENANT — "+invocation.DisplayName+" • "+forecast.CommandName,"Call “"+invocation.DisplayName+"” only through this complete Union Forecast. "+forecast.Phrase,forecast.TacticalIntent,forecast.TargetId,forecast.TargetName,actions.AsReadOnly(),checked(forecast.SharedApCost+invocation.SharedApCost),forecast.ApRecovery,checked(forecast.CombinedMpCost+invocation.PersonalMpCost),forecast.ExpectedEffect+" · "+invocation.DisplayName+" answers the accepted Covenant.",forecast.Risk,forecast.LearningOpportunity+" · "+marker,forecast.FallbackBehavior,forecast.GenerationIdentity,debug);
        }

        static CampaignProgressionState022 Progression(CampaignState campaign)=>campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022;

        static CampaignState WithProgression(CampaignState campaign,CampaignProgressionState022 state)
        {
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();var progress=strategic.Campaign019??CampaignProgressState019.Default();var playable=progress.Playable020??CampaignPlayableState020.Default();
            playable=playable.With(progression022:state,replaceProgression022:true,lastCheckpointId:state.LastCheckpointId);progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:state.LastCheckpointId);strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:state.LastCheckpointId);city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:state.LastCheckpointId);return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
        }
    }

    internal sealed class CovenantForecastInvocation022
    {
        internal CovenantForecastInvocation022(string covenantId,string displayName,string role,string acceptanceReceiptId,string invokerMemberId,string unionId,string forecastId,int sharedApCost,int personalMpCost,string authoritativeHash,string receiptId)
        {CovenantId=covenantId;DisplayName=displayName;Role=role;AcceptanceReceiptId=acceptanceReceiptId;InvokerMemberId=invokerMemberId;UnionId=unionId;ForecastId=forecastId;SharedApCost=sharedApCost;PersonalMpCost=personalMpCost;AuthoritativeHash=authoritativeHash;ReceiptId=receiptId;}
        internal string CovenantId{get;}internal string DisplayName{get;}internal string Role{get;}internal string AcceptanceReceiptId{get;}internal string InvokerMemberId{get;}internal string UnionId{get;}internal string ForecastId{get;}internal int SharedApCost{get;}internal int PersonalMpCost{get;}internal string AuthoritativeHash{get;}internal string ReceiptId{get;}
    }

    internal static class CovenantInvocationForecastAuthority022
    {
        internal const string EventType="GREAT_COVENANT_INVOKED";
        const string MarkerPrefix="GREAT_COVENANT_INVOCATION022=";
        const string PendingPrefix="campaign022_covenant_battle_pending:";

        internal static CovenantForecastInvocation022 Create(CampaignState campaign,BattleState battle,BattleForecastState forecast,CovenantDto022 definition,string acceptanceReceiptId,string invokerMemberId,int ap,int mp)
        {
            var role=(definition.role??string.Empty).ToUpperInvariant();var hash=Hash(campaign,battle,forecast,definition.covenantId,role,acceptanceReceiptId,invokerMemberId,ap,mp);return new CovenantForecastInvocation022(definition.covenantId,definition.displayName,role,acceptanceReceiptId,invokerMemberId,forecast.UnionId,forecast.ForecastId,ap,mp,hash,"COVBATTLE022_"+hash.Substring(0,24).ToUpperInvariant());
        }

        internal static string Marker(CovenantForecastInvocation022 value)=>MarkerPrefix+string.Join("|",new[]{value.CovenantId,value.Role,value.AcceptanceReceiptId,value.InvokerMemberId,value.UnionId,value.ForecastId,value.SharedApCost.ToString(),value.PersonalMpCost.ToString(),value.AuthoritativeHash,value.ReceiptId,Uri.EscapeDataString(value.DisplayName??value.CovenantId)});
        internal static string PendingCheckpoint(string receiptId)=>PendingPrefix+receiptId;
        internal static string EventText(string unionName,CovenantForecastInvocation022 value)=>(unionName??value.UnionId)+" calls "+value.DisplayName+" through the selected complete Union Forecast; the voluntary Covenant answers under "+value.ReceiptId+".";

        internal static bool TryAuthorize(CampaignState campaign,BattleState battle,BattleForecastState forecast,out CovenantForecastInvocation022 invocation)
        {
            invocation=null;if(campaign==null||battle==null||forecast==null||string.IsNullOrWhiteSpace(forecast.LearningOpportunity))return false;
            var markerIndex=forecast.LearningOpportunity.IndexOf(MarkerPrefix,StringComparison.Ordinal);if(markerIndex<0||markerIndex!=forecast.LearningOpportunity.LastIndexOf(MarkerPrefix,StringComparison.Ordinal))return false;
            var encoded=forecast.LearningOpportunity.Substring(markerIndex+MarkerPrefix.Length).Trim();var separator=encoded.IndexOfAny(new[]{' ','·',';'});if(separator>=0)encoded=encoded.Substring(0,separator);var parts=encoded.Split('|');
            if(parts.Length!=11||!TryCosts(parts[1],out var expectedAp,out var expectedMp)||!int.TryParse(parts[6],out var ap)||!int.TryParse(parts[7],out var mp)||ap!=expectedAp||mp!=expectedMp)return false;
            if(!StringComparer.Ordinal.Equals(parts[4],forecast.UnionId)||!StringComparer.Ordinal.Equals(parts[5],forecast.ForecastId)||!RoleMatchesForecast(parts[1],forecast))return false;
            var hash=Hash(campaign,battle,forecast,parts[0],parts[1],parts[2],parts[3],ap,mp);var receipt="COVBATTLE022_"+hash.Substring(0,24).ToUpperInvariant();
            if(!StringComparer.Ordinal.Equals(parts[8],hash)||!StringComparer.Ordinal.Equals(parts[9],receipt))return false;
            if(battle.Selections.Count(x=>StringComparer.Ordinal.Equals(x.UnionId,forecast.UnionId)&&StringComparer.Ordinal.Equals(x.ForecastId,forecast.ForecastId))!=1)return false;
            var progression=campaign.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.Progression022;if(progression==null||!StringComparer.Ordinal.Equals(progression.LastCheckpointId,PendingCheckpoint(receipt))||progression.AppliedReceiptIds.Contains(receipt))return false;
            var accepted=progression.Covenants.Where(x=>StringComparer.Ordinal.Equals(x.CovenantId,parts[0])&&x.Status==CovenantStatus022.Accepted).ToArray();if(accepted.Length!=1||accepted[0].AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,parts[2]))!=1||progression.AppliedReceiptIds.Count(x=>StringComparer.Ordinal.Equals(x,parts[2]))!=1||!parts[2].StartsWith("COVACCEPTREC022_",StringComparison.Ordinal))return false;
            var action=forecast.MemberActions.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.ActorMemberId,parts[3]));if(action==null||action.SharedApCost<ap||action.PersonalMpCost<mp)return false;
            string displayName;try{displayName=Uri.UnescapeDataString(parts[10]);}catch{ return false; }if(string.IsNullOrWhiteSpace(displayName))return false;
            invocation=new CovenantForecastInvocation022(parts[0],displayName,parts[1],parts[2],parts[3],parts[4],parts[5],ap,mp,hash,receipt);return true;
        }

        internal static bool TryCosts(string role,out int ap,out int mp)
        {
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "GUARD":ap=2;mp=6;return true;
                case "WARDING":ap=3;mp=7;return true;
                case "SUPPORT":ap=3;mp=7;return true;
                case "RESTORATION":ap=3;mp=8;return true;
                case "COMBAT":ap=4;mp=8;return true;
                case "TACTICAL":ap=4;mp=8;return true;
                case "MYSTIC":ap=4;mp=10;return true;
                default:ap=0;mp=0;return false;
            }
        }

        internal static bool RoleMatchesForecast(string role,BattleForecastState forecast)
        {
            if(forecast==null)return false;
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "COMBAT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_ALL_OUT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_BALANCED");
                case "MYSTIC":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_MYSTIC")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Mystic||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Mystic"));
                case "RESTORATION":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_HEAL")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Restoration||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Restoration"));
                case "WARDING":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Warding"));
                case "GUARD":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_GUARD")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Guard||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Guard"));
                case "SUPPORT":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_SUPPORT")||StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_AP_RECOVERY")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Recovery||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Support"));
                case "TACTICAL":return StringComparer.Ordinal.Equals(forecast.CommandId,"CMD_FLANK")||forecast.MemberActions.Any(x=>x.Kind==BattleActionKind.Tactical||StringComparer.OrdinalIgnoreCase.Equals(x.Discipline,"Tactical"));
                default:return false;
            }
        }

        static string Hash(CampaignState campaign,BattleState battle,BattleForecastState forecast,string covenantId,string role,string acceptanceReceiptId,string invokerMemberId,int ap,int mp)=>CanonicalJson.Sha256Hex(new{campaign.CampaignGuid,battle.BattleId,battle.Round,forecast.ForecastId,forecast.GenerationIdentity,forecast.UnionId,covenantId,role,acceptanceReceiptId,invokerMemberId,sharedApCost=ap,personalMpCost=mp,control="ACCEPTED_COVENANT_COMPLETE_UNION_FORECAST_ONLY"});
    }

    internal sealed class InvocationBattleEffect022
    {
        internal InvocationBattleEffect022(int amount,string targetUnionId,string targetMemberId,string summary){Amount=amount;TargetUnionId=targetUnionId??string.Empty;TargetMemberId=targetMemberId??string.Empty;Summary=summary??string.Empty;}
        internal int Amount{get;}internal string TargetUnionId{get;}internal string TargetMemberId{get;}internal string Summary{get;}
    }

    internal static class InvocationBattleEffects022
    {
        internal static InvocationBattleEffect022 Apply(string role,int potency,string sourceUnionId,string invokerMemberId,string preferredTargetUnionId,List<BattleUnionState> players,List<BattleUnionState> enemies)
        {
            potency=Math.Max(1,potency);var sourceIndex=players.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,sourceUnionId));if(sourceIndex<0)return new InvocationBattleEffect022(0,string.Empty,string.Empty,"No allied Union remained.");
            switch((role??string.Empty).ToUpperInvariant())
            {
                case "COMBAT":return Damage(enemies,preferredTargetUnionId,potency*3,"restrained combat force");
                case "MYSTIC":return Damage(enemies,preferredTargetUnionId,potency*4,"mystic covenant force");
                case "RESTORATION":return Restore(players,sourceIndex,invokerMemberId,preferredTargetUnionId,potency*3);
                case "WARDING":return Ward(players,FriendlyTarget(players,sourceIndex,preferredTargetUnionId),potency,false);
                case "GUARD":return Ward(players,sourceIndex,potency,true);
                case "SUPPORT":return Support(players,FriendlyTarget(players,sourceIndex,preferredTargetUnionId),potency);
                case "TACTICAL":return Tactical(enemies,preferredTargetUnionId,potency);
                default:return new InvocationBattleEffect022(0,string.Empty,string.Empty,"Unknown invocation role.");
            }
        }

        static InvocationBattleEffect022 Damage(List<BattleUnionState> enemies,string preferred,int requested,string summary)
        {
            var unionIndex=enemies.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,preferred)&&!x.IsDefeated);if(unionIndex<0)unionIndex=enemies.FindIndex(x=>!x.IsDefeated);if(unionIndex<0)return new InvocationBattleEffect022(0,string.Empty,string.Empty,"No enemy remained.");var union=enemies[unionIndex];var memberIndex=FirstActiveMember(union);if(memberIndex<0)return new InvocationBattleEffect022(0,union.UnionId,string.Empty,"No enemy member remained.");var members=new List<BattleMemberState>(union.Members);var member=members[memberIndex];var amount=Math.Min(member.CurrentHp,Math.Max(1,requested));members[memberIndex]=member.With(currentHp:member.CurrentHp-amount);enemies[unionIndex]=union.With(members:members.AsReadOnly());return new InvocationBattleEffect022(amount,union.UnionId,member.MemberId,summary+" deals "+amount+" damage");
        }

        static InvocationBattleEffect022 Restore(List<BattleUnionState> players,int sourceIndex,string invokerMemberId,string preferredTargetUnionId,int requested)
        {
            var preferredIndex=players.FindIndex(x=>
                StringComparer.Ordinal.Equals(x.UnionId,preferredTargetUnionId)&&!x.Retreated);
            var order=Enumerable.Range(0,players.Count)
                .OrderBy(x=>x==preferredIndex?0:x==sourceIndex?1:2)
                .ThenBy(x=>players[x].UnionId,StringComparer.Ordinal);
            foreach(var unionIndex in order)
            {
                var union=players[unionIndex];for(var memberIndex=0;memberIndex<union.Members.Count;memberIndex++)
                {
                    var member=union.Members[memberIndex];if(member.CurrentHp>=member.MaximumHp)continue;var amount=Math.Min(requested,member.MaximumHp-member.CurrentHp);var members=new List<BattleMemberState>(union.Members);members[memberIndex]=member.With(currentHp:member.CurrentHp+amount,stabilized:false);players[unionIndex]=union.With(members:members.AsReadOnly());return new InvocationBattleEffect022(amount,union.UnionId,member.MemberId,"restores "+amount+" HP");
                }
            }
            var source=players[sourceIndex];var gain=Math.Min(Math.Max(1,requested/3),Math.Max(0,100-source.Cohesion));if(gain>0){players[sourceIndex]=source.With(cohesion:source.Cohesion+gain);return new InvocationBattleEffect022(gain,source.UnionId,invokerMemberId,"restores "+gain+" Cohesion");}return Ward(players,sourceIndex,Math.Max(1,requested/3),false);
        }

        static InvocationBattleEffect022 Ward(List<BattleUnionState> players,int sourceIndex,int potency,bool guard)
        {
            var union=players[sourceIndex];var formationGain=Math.Min(potency*100,Math.Max(0,10000-union.FormationConditionBasisPoints));if(formationGain>0){players[sourceIndex]=union.With(formationConditionBasisPoints:union.FormationConditionBasisPoints+formationGain,guarding:guard||union.Guarding,engagement:guard?EngagementState.Guarded:union.Engagement);return new InvocationBattleEffect022(Math.Max(1,formationGain/100),union.UnionId,union.LeaderMemberId,"raises formation condition by "+formationGain/100+"%");}var cohesionGain=Math.Min(potency,Math.Max(0,100-union.Cohesion));if(cohesionGain>0){players[sourceIndex]=union.With(cohesion:union.Cohesion+cohesionGain,guarding:guard||union.Guarding,engagement:guard?EngagementState.Guarded:union.Engagement);return new InvocationBattleEffect022(cohesionGain,union.UnionId,union.LeaderMemberId,"raises Cohesion by "+cohesionGain);}players[sourceIndex]=union.With(guarding:true,engagement:EngagementState.Guarded);return new InvocationBattleEffect022(1,union.UnionId,union.LeaderMemberId,"establishes a guarded engagement");
        }

        static InvocationBattleEffect022 Support(List<BattleUnionState> players,int sourceIndex,int potency)
        {
            var union=players[sourceIndex];var cohesionGain=Math.Min(potency*2,Math.Max(0,100-union.Cohesion));if(cohesionGain>0){players[sourceIndex]=union.With(cohesion:union.Cohesion+cohesionGain,engagement:EngagementState.Supporting);return new InvocationBattleEffect022(cohesionGain,union.UnionId,union.LeaderMemberId,"restores "+cohesionGain+" Cohesion");}var formationGain=Math.Min(potency*100,Math.Max(0,10000-union.FormationConditionBasisPoints));if(formationGain>0){players[sourceIndex]=union.With(formationConditionBasisPoints:union.FormationConditionBasisPoints+formationGain,engagement:EngagementState.Supporting);return new InvocationBattleEffect022(Math.Max(1,formationGain/100),union.UnionId,union.LeaderMemberId,"restores formation condition");}var apGain=Math.Min(potency,Math.Max(0,union.MaximumAp-union.CurrentAp));if(apGain>0){players[sourceIndex]=union.With(currentAp:union.CurrentAp+apGain,engagement:EngagementState.Supporting);return new InvocationBattleEffect022(apGain,union.UnionId,union.LeaderMemberId,"restores "+apGain+" shared AP");}players[sourceIndex]=union.With(engagement:EngagementState.Supporting);return new InvocationBattleEffect022(1,union.UnionId,union.LeaderMemberId,"establishes supporting engagement");
        }

        static InvocationBattleEffect022 Tactical(List<BattleUnionState> enemies,string preferred,int potency)
        {
            var index=enemies.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,preferred)&&!x.IsDefeated);if(index<0)index=enemies.FindIndex(x=>!x.IsDefeated);if(index<0)return new InvocationBattleEffect022(0,string.Empty,string.Empty,"No enemy remained.");var union=enemies[index];var formationLoss=Math.Min(potency*100,Math.Max(0,union.FormationConditionBasisPoints));if(formationLoss>0){enemies[index]=union.With(formationConditionBasisPoints:union.FormationConditionBasisPoints-formationLoss,engagement:EngagementState.RearPressure);return new InvocationBattleEffect022(Math.Max(1,formationLoss/100),union.UnionId,union.LeaderMemberId,"breaks "+formationLoss/100+"% formation condition");}var cohesionLoss=Math.Min(potency*2,Math.Max(0,union.Cohesion));if(cohesionLoss>0){enemies[index]=union.With(cohesion:union.Cohesion-cohesionLoss,engagement:EngagementState.RearPressure);return new InvocationBattleEffect022(cohesionLoss,union.UnionId,union.LeaderMemberId,"breaks "+cohesionLoss+" Cohesion");}var next=union.Engagement==EngagementState.Broken?EngagementState.RearPressure:EngagementState.Broken;enemies[index]=union.With(engagement:next);return new InvocationBattleEffect022(1,union.UnionId,union.LeaderMemberId,"forces "+next+" engagement");
        }

        static int FriendlyTarget(List<BattleUnionState> players,int sourceIndex,string preferred)
        {
            var preferredIndex=players.FindIndex(x=>
                StringComparer.Ordinal.Equals(x.UnionId,preferred)&&!x.Retreated&&!x.IsDefeated);
            return preferredIndex>=0?preferredIndex:sourceIndex;
        }

        static int FirstActiveMember(BattleUnionState union){for(var i=0;i<union.Members.Count;i++)if(!union.Members[i].Downed)return i;return -1;}
    }
}
