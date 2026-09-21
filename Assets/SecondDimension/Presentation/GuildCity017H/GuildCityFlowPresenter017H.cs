using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void BuildGuildCityDefense017H(Transform body,GuildCity017H.IGuildCityStrategicPresentationCoordinator017H coordinator,GuildCity017H.GuildCityStrategicPresentationState017H state)
        {
            if(state==null||!state.IsAvailable){AddMessagePanel(body,"CITY DEFENSE",state?.Error??"Defense authority unavailable.",RuntimeUi.Warning);return;}
            AddMessagePanel(body,"SKYHOME DEFENSE NETWORK",state.AggregateCombatSummary+"\n"+state.AggregateXpSummary+"\nEvery facility contributes to combat, mastery, or institutional XP. Decisive breaches use the existing Union battle engine.",RuntimeUi.Accent);
            if(state.ActiveDefense==null)
            {
                foreach(var profile in state.DefenseProfiles.Where(x=>x.Available))
                {
                    var panel=AddMessagePanel(body,profile.DisplayName,profile.Summary+"\n"+profile.Classification+" • "+profile.WaveCount+" waves • "+string.Join(" / ",profile.LaneNames),profile.FutureLocked?RuntimeUi.Warning:RuntimeUi.ButtonNormal);
                    RuntimeUi.AddButton(panel,"Start defense "+profile.ProfileId,"PREPARE DEFENSE",()=>RunGuildCityCommand017D(()=>coordinator.StartGuildCityDefense017H(profile.ProfileId)),130f,RuntimeUi.Accent);
                }
                return;
            }
            var active=state.ActiveDefense;AddMessagePanel(body,active.DisplayName,"Status: "+active.Status+" • Wave "+(active.CurrentWave+1)+"/"+active.TotalWaves+" • City "+active.CityIntegrity+" • Barrier "+active.BarrierIntegrity+"\n"+active.CurrentWaveName+(active.CurrentWaveDecisiveBattle?" • CERTIFIED UNION BATTLE":" • STRATEGIC SUPPORT WAVE"),RuntimeUi.Accent);
            foreach(var lane in active.Lanes)
            {
                var panel=AddMessagePanel(body,lane.DisplayName,lane.Summary+"\nAssigned: "+(lane.AssignedUnionIds.Count==0?"None":string.Join(", ",lane.AssignedUnionIds)),RuntimeUi.ButtonNormal);
                var unions017H=_coordinator.State?.Unions;
                if(unions017H!=null)foreach(var union017H in unions017H.Where(x=>x.MemberRecruitIds!=null&&x.MemberRecruitIds.Count>0))
                {
                    var capturedUnionId017H=union017H.UnionId;var capturedLaneId017H=lane.LaneId;
                    RuntimeUi.AddButton(panel,"Assign defense "+capturedLaneId017H+capturedUnionId017H,"ASSIGN "+union017H.DisplayName,()=>RunGuildCityCommand017D(()=>coordinator.AssignGuildCityDefenseUnion017H(capturedLaneId017H,capturedUnionId017H)),100f);
                }
            }
            RuntimeUi.AddButton(body,"Commit defense wave 017H",active.CurrentWaveDecisiveBattle?"COMMIT DECISIVE WAVE":"RESOLVE DEFENSE WAVE",()=>RunGuildCityCommand017D(()=>coordinator.CommitGuildCityDefenseWave017H()),132f,RuntimeUi.Positive);
            if(active.CurrentWaveDecisiveBattle)RuntimeUi.AddButton(body,"Start defense battle 017H","ENTER UNION BATTLE",()=>RunGuildCityCommand017D(coordinator.StartCommittedGuildCityDefenseBattle017H),132f,RuntimeUi.Accent);
            if(StringComparer.Ordinal.Equals(active.Status,"Completed")||StringComparer.Ordinal.Equals(active.Status,"Failed"))RuntimeUi.AddButton(body,"Finalize defense 017H","FINALIZE DEFENSE",()=>RunGuildCityCommand017D(coordinator.FinalizeGuildCityDefense017H),120f,RuntimeUi.Positive);
        }
        private string FirstNormalUnionId017H(){var state=_coordinator.State;return state?.Unions?.FirstOrDefault(x=>x.MemberRecruitIds!=null&&x.MemberRecruitIds.Count>0)?.UnionId??string.Empty;}
        private void BuildGuildCityChronicle017H(Transform body,GuildCity017H.IGuildCityStrategicPresentationCoordinator017H coordinator,GuildCity017H.GuildCityStrategicPresentationState017H state)
        {
            if(state==null||!state.IsAvailable){AddMessagePanel(body,"CHRONICLE",state?.Error??"Canon authority unavailable.",RuntimeUi.Warning);return;}
            RuntimeUi.AddButton(body,"Sync canon events 017H","REFRESH CHRONICLE",()=>RunGuildCityCommand017D(coordinator.SynchronizeGuildCityCanonEvents017H),110f);
            AddMessagePanel(body,"CANON EVENT LAW","Historical Chronicles preserve fixed outcomes. Canon Echoes create new variable operations without replacing published events. Future campaigns remain locked until their story gates. What-if content is noncanon and disabled.",RuntimeUi.Accent);
            foreach(var item in state.CanonEvents)
            {
                var color=item.Available?RuntimeUi.ButtonNormal:RuntimeUi.Warning;var panel=AddMessagePanel(body,item.DisplayName,item.Classification+" • "+item.Status+"\n"+item.Summary+(string.IsNullOrWhiteSpace(item.RequiredStoryGate)?string.Empty:"\nGate: "+item.RequiredStoryGate),color);
                if(item.Available)
                {
                    RuntimeUi.AddButton(panel,"View canon "+item.EventId,"VIEW",()=>RunGuildCityCommand017D(()=>coordinator.ViewGuildCityCanonEvent017H(item.EventId)),92f);
                    RuntimeUi.AddButton(panel,"Resolve canon "+item.EventId,item.OutcomeLocked?"ACKNOWLEDGE LOCKED OUTCOME":"RESOLVE EVENT",()=>RunGuildCityCommand017D(()=>coordinator.ResolveGuildCityCanonEvent017H(item.EventId,"DEFAULT")),92f,RuntimeUi.Accent);
                }
            }
        }
    }
}