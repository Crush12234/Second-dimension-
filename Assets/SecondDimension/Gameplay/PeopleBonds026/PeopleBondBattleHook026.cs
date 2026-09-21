using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.PeopleBonds026
{
    internal sealed class LinkArtRuntimeRule026
    {
        public LinkArtRuntimeRule026(string id,string name,string minimumTierId,int participants,IReadOnlyList<string> disciplines,int apSurcharge,int cohesionBonus,int enemyCohesionPressure,string intent)
        { LinkArtId=id;DisplayName=name;MinimumTierId=minimumTierId;RequiredParticipants=participants;RequiredDisciplines=disciplines;SharedApSurcharge=apSurcharge;CohesionBonus=cohesionBonus;EnemyCohesionPressure=enemyCohesionPressure;ForecastIntent=intent; }
        public string LinkArtId{get;} public string DisplayName{get;} public string MinimumTierId{get;} public int RequiredParticipants{get;} public IReadOnlyList<string> RequiredDisciplines{get;} public int SharedApSurcharge{get;} public int CohesionBonus{get;} public int EnemyCohesionPressure{get;} public string ForecastIntent{get;}
    }

    internal static class PeopleBondBattleHook026
    {
        const string Marker="LINK_ART_ID=";
        static readonly LinkArtRuntimeRule026[] Rules={
            new LinkArtRuntimeRule026("LINK_ART026_00","Shield and Spear","BOND_TIER026_2",3,new[]{"Combat", "Guard"},1,3,2,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_01","Crossfire Promise","BOND_TIER026_3",2,new[]{"Combat", "Mystic"},2,4,3,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_02","Twin Comet","BOND_TIER026_4",2,new[]{"Restoration", "Guard"},3,5,4,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_03","Mercy Line","BOND_TIER026_5",2,new[]{"Mystic", "Warding"},1,6,5,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_04","Stormbound","BOND_TIER026_2",2,new[]{"Support", "Tactical"},2,7,2,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_05","Ironroot Accord","BOND_TIER026_3",2,new[]{"Combat", "Restoration"},3,3,3,"Flank"),
            new LinkArtRuntimeRule026("LINK_ART026_06","Nightglass Veil","BOND_TIER026_4",2,new[]{"Combat", "Guard"},1,4,4,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_07","Furnace Chorus","BOND_TIER026_5",2,new[]{"Combat", "Mystic"},2,5,5,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_08","Moonroad Hunt","BOND_TIER026_2",2,new[]{"Restoration", "Guard"},3,6,2,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_09","Gateward Oath","BOND_TIER026_3",3,new[]{"Mystic", "Warding"},1,7,3,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_10","Broken Chain","BOND_TIER026_4",2,new[]{"Support", "Tactical"},2,3,4,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_11","Archive of Two","BOND_TIER026_5",2,new[]{"Combat", "Restoration"},3,4,5,"Flank"),
            new LinkArtRuntimeRule026("LINK_ART026_12","Open Hand Reversal","BOND_TIER026_2",2,new[]{"Combat", "Guard"},1,5,2,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_13","Rescue Spiral","BOND_TIER026_3",2,new[]{"Combat", "Mystic"},2,6,3,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_14","Ward and Blade","BOND_TIER026_4",2,new[]{"Restoration", "Guard"},3,7,4,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_15","Hunter’s Signal","BOND_TIER026_5",2,new[]{"Mystic", "Warding"},1,3,5,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_16","Silent Thunder","BOND_TIER026_2",2,new[]{"Support", "Tactical"},2,4,2,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_17","Bridge of Ash","BOND_TIER026_3",2,new[]{"Combat", "Restoration"},3,5,3,"Flank"),
            new LinkArtRuntimeRule026("LINK_ART026_18","First Light Relay","BOND_TIER026_4",3,new[]{"Combat", "Guard"},1,6,4,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_19","Last Line Together","BOND_TIER026_5",2,new[]{"Combat", "Mystic"},2,7,5,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_20","Wild Council Rush","BOND_TIER026_2",2,new[]{"Restoration", "Guard"},3,3,2,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_21","Debtless Road","BOND_TIER026_3",2,new[]{"Mystic", "Warding"},1,4,3,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_22","Mercy Index","BOND_TIER026_4",2,new[]{"Support", "Tactical"},2,5,4,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_23","Truthstrike","BOND_TIER026_5",2,new[]{"Combat", "Restoration"},3,6,5,"Flank"),
            new LinkArtRuntimeRule026("LINK_ART026_24","Lantern Patrol","BOND_TIER026_2",2,new[]{"Combat", "Guard"},1,7,2,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_25","Burrow Harmony","BOND_TIER026_3",2,new[]{"Combat", "Mystic"},2,3,3,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_26","Cogspire Sequence","BOND_TIER026_4",2,new[]{"Restoration", "Guard"},3,4,4,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_27","Hearthshield","BOND_TIER026_5",3,new[]{"Mystic", "Warding"},1,5,5,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_28","Dreamglass Duet","BOND_TIER026_2",2,new[]{"Support", "Tactical"},2,6,2,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_29","Duskwood Pincer","BOND_TIER026_3",2,new[]{"Combat", "Restoration"},3,7,3,"Flank"),
            new LinkArtRuntimeRule026("LINK_ART026_30","Roadbell Convoy","BOND_TIER026_4",2,new[]{"Combat", "Guard"},1,3,4,"Protect"),
            new LinkArtRuntimeRule026("LINK_ART026_31","Stormpaw Pursuit","BOND_TIER026_5",2,new[]{"Combat", "Mystic"},2,4,5,"Assault"),
            new LinkArtRuntimeRule026("LINK_ART026_32","Foundry Refrain","BOND_TIER026_2",2,new[]{"Restoration", "Guard"},3,5,2,"Restore"),
            new LinkArtRuntimeRule026("LINK_ART026_33","World Gate Resonance","BOND_TIER026_3",2,new[]{"Mystic", "Warding"},1,6,3,"Control"),
            new LinkArtRuntimeRule026("LINK_ART026_34","First Alliance Echo","BOND_TIER026_4",2,new[]{"Support", "Tactical"},2,7,4,"Support"),
            new LinkArtRuntimeRule026("LINK_ART026_35","Second Dimension Accord","BOND_TIER026_5",2,new[]{"Combat", "Restoration"},3,3,5,"Flank")
        };

        public static PeopleBondState026 GetState(CampaignState campaign)=>campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?.PeopleBonds026??PeopleBondState026.Default();

        public static bool TrySelectForecastRule(CampaignState campaign,BattleUnionState union,int round,int slot,IReadOnlyList<BattlePlannedActionState> actions,int currentApCost,out LinkArtRuntimeRule026 rule)
        {
            rule=null; if(campaign==null||union==null||actions==null)return false;
            var activeIds=union.Members.Where(x=>!x.Downed).Select(x=>x.MemberId).ToArray(); if(activeIds.Length<2)return false;
            var pair=PeopleBondService026.StrongestPair(GetState(campaign),activeIds); if(pair==null)return false;
            var tier=PeopleBondService026.TierRank(pair.TierId); if(tier<2)return false;
            var eligible=Rules.Where(x=>PeopleBondService026.TierRank(x.MinimumTierId)<=tier&&x.RequiredParticipants<=activeIds.Length&&currentApCost+x.SharedApSurcharge<=union.CurrentAp&&DisciplinesSatisfied(x.RequiredDisciplines,actions)).ToArray();
            if(eligible.Length==0)return false;
            var hash=CanonicalJson.Sha256Hex(new{campaign.CampaignSeed,pair.PairId,union.UnionId,round,slot});
            var index=(int)(Convert.ToUInt32(hash.Substring(0,8),16)%(uint)eligible.Length); rule=eligible[index]; return true;
        }

        public static PeopleBondState026 ApplySelectedForecast(CampaignState campaign,BattleState battle,BattleForecastState forecast,int unionIndex,List<BattleUnionState> players,List<BattleUnionState> enemies,List<BattleEventState> events,PeopleBondState026 state)
        {
            if(forecast==null||state==null||unionIndex<0||unionIndex>=players.Count)return state;
            var linkId=ExtractLinkArtId(forecast.LearningOpportunity); if(string.IsNullOrWhiteSpace(linkId))return state;
            var runtime=Rules.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.LinkArtId,linkId)); if(runtime==null)return state;
            var union=players[unionIndex]; var roundKey=union.UnionId+"@"+battle.Round;
            var unionStates=state.Unions.ToList(); var stateIndex=unionStates.FindIndex(x=>StringComparer.Ordinal.Equals(x.UnionId,union.UnionId));
            var unionState=stateIndex>=0?unionStates[stateIndex]:new UnionBondRuntimeState026(union.UnionId,string.Empty,Array.Empty<string>(),0);
            if(unionState.TriggeredRoundKeys.Contains(roundKey))return state;
            var activeIds=union.Members.Where(x=>!x.Downed).Select(x=>x.MemberId).ToArray(); var pair=PeopleBondService026.StrongestPair(state,activeIds);
            if(pair==null||PeopleBondService026.TierRank(pair.TierId)<PeopleBondService026.TierRank(runtime.MinimumTierId))return state;
            var keys=unionState.TriggeredRoundKeys.ToList(); keys.Add(roundKey); unionState=unionState.With(triggeredRoundKeys:keys.AsReadOnly(),linkArtTriggerCount:unionState.LinkArtTriggerCount+1);
            if(stateIndex>=0)unionStates[stateIndex]=unionState;else unionStates.Add(unionState);
            players[unionIndex]=union.With(cohesion:Math.Min(100,union.Cohesion+runtime.CohesionBonus));
            var enemyIndex=FirstActiveEnemy(enemies); if(enemyIndex>=0){var enemy=enemies[enemyIndex];enemies[enemyIndex]=enemy.With(cohesion:Math.Max(0,enemy.Cohesion-runtime.EnemyCohesionPressure));}
            var text=union.DisplayName+" coordinates “"+runtime.DisplayName+"” without replacing any member's legal Art: +"+runtime.CohesionBonus+" Cohesion"+(enemyIndex>=0?", "+runtime.EnemyCohesionPressure+" enemy Cohesion pressure.":".");
            events.Add(new BattleEventState(events.Count,battle.Round,"LINK_ART_TRIGGERED",BattleSide.Player,union.UnionId,union.LeaderMemberId,runtime.LinkArtId,text,runtime.CohesionBonus,CanonicalJson.Sha256Hex(new{battle.BattleId,battle.Round,union.UnionId,runtime.LinkArtId,text}),union.UnionId,union.LeaderMemberId,enemyIndex>=0?enemies[enemyIndex].UnionId:string.Empty,string.Empty));
            return state.With(unions:unionStates.AsReadOnly(),lastCheckpointId:"link_art:"+runtime.LinkArtId+":"+roundKey);
        }

        public static CampaignState WithState(CampaignState campaign,PeopleBondState026 bonds)
        {
            if(campaign?.Guild?.GuildCity==null)return campaign; var city=campaign.Guild.GuildCity; var strategic=city.Strategic017H??GuildCityStrategicState017H.Default(); var progress=strategic.Campaign019??CampaignProgressState019.Default(); var playable=progress.Playable020??CampaignPlayableState020.Default();
            playable=playable.With(peopleBonds026:bonds??PeopleBondState026.Default(),replacePeopleBonds026:true,lastCheckpointId:bonds?.LastCheckpointId??playable.LastCheckpointId); progress=progress.With(playable020:playable,replacePlayable020:true,lastCheckpointId:playable.LastCheckpointId); strategic=strategic.With(campaign019:progress,replaceCampaign019:true,lastCheckpointId:progress.LastCheckpointId); city=city.With(strategic017H:strategic,replaceStrategic017H:true,lastCheckpointId:strategic.LastCheckpointId); return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);
        }

        public static string MarkerText(LinkArtRuntimeRule026 rule)=>rule==null?string.Empty:Marker+rule.LinkArtId+" · Complete Forecast only · underlying member Art IDs preserved · one trigger per Union per round.";
        static string ExtractLinkArtId(string text){if(string.IsNullOrWhiteSpace(text))return string.Empty;var start=text.IndexOf(Marker,StringComparison.Ordinal);if(start<0)return string.Empty;start+=Marker.Length;var end=text.IndexOfAny(new[]{' ','·','|',';'},start);return end<0?text.Substring(start).Trim():text.Substring(start,end-start).Trim();}
        static int FirstActiveEnemy(IReadOnlyList<BattleUnionState> enemies){for(var i=0;i<(enemies?.Count??0);i++)if(!enemies[i].IsDefeated&&!enemies[i].Retreated)return i;return -1;}
        static bool DisciplinesSatisfied(IReadOnlyList<string> required,IReadOnlyList<BattlePlannedActionState> actions){if(required==null||required.Count==0)return true;for(var i=0;i<required.Count;i++){var found=false;for(var j=0;j<actions.Count;j++)if(Compatible(required[i],actions[j].Discipline,actions[j].Kind)){found=true;break;}if(!found)return false;}return true;}
        static bool Compatible(string required,string actual,BattleActionKind kind){if(StringComparer.OrdinalIgnoreCase.Equals(required,actual))return true;if(StringComparer.OrdinalIgnoreCase.Equals(required,"Combat"))return kind==BattleActionKind.Martial||kind==BattleActionKind.Tactical;if(StringComparer.OrdinalIgnoreCase.Equals(required,"Guard"))return kind==BattleActionKind.Guard;if(StringComparer.OrdinalIgnoreCase.Equals(required,"Restoration"))return kind==BattleActionKind.Restoration;if(StringComparer.OrdinalIgnoreCase.Equals(required,"Mystic"))return kind==BattleActionKind.Mystic;if(StringComparer.OrdinalIgnoreCase.Equals(required,"Support"))return kind==BattleActionKind.Recovery;return false;}
    }
}
