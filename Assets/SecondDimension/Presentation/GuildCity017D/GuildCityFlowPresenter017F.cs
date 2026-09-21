using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017F;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void AddHallNarrative017F(Transform body,GuildCityPresentationState017D state)
        {
            var scene=GuildCityNarrativeRegistry017F.OnboardingForOperation(state.OperationOrdinal);
            if (scene != null && !StringComparer.Ordinal.Equals(state.TutorialDepthId,"FastCharter"))
            {
                var copy = StringComparer.Ordinal.Equals(state.TutorialDepthId,"ContextualTips")
                    ? scene.narration+"\nNEXT  •  "+scene.instruction
                    : scene.narration+"\n\nGUILDMASTER  •  “"+scene.guildmasterLine+"”\n\nNEXT  •  "+scene.instruction;
                AddMessagePanel(body,scene.title,copy,RuntimeUi.Accent);
            }

            var placed=new List<string>();
            for (var i=0;i<state.Plots.Count;i++) if (!string.IsNullOrWhiteSpace(state.Plots[i].BuildingId)) placed.Add(state.Plots[i].BuildingId);
            var ambient=GuildCityNarrativeRegistry017F.Load().hallAmbient
                .Where(value=>value != null && (StringComparer.Ordinal.Equals(value.facilityId,"RUINED_ANNEX") || placed.Contains(value.facilityId)))
                .OrderByDescending(value=>value.weight).ThenBy(value=>value.id,StringComparer.Ordinal).Take(4).ToArray();
            if (ambient.Length == 0) return;
            var row=AddRow(body,"Living Hall Activity 017F",12f,220f);
            foreach (var value in ambient)
            {
                var panel=AddColumnPanel(row,"Hall Activity "+value.id,1f,205f);
                RuntimeUi.AddText(panel,"Hall Role "+value.id,value.roleTag.ToUpperInvariant(),22,TextAnchor.MiddleLeft,RuntimeUi.Warning,FontStyle.Bold);
                RuntimeUi.AddText(panel,"Hall Action "+value.id,value.actionText+"\n"+value.statusText,24,TextAnchor.UpperLeft,RuntimeUi.Text);
            }
        }

        private void AddCityNarrative017F(Transform body,GuildCityPresentationState017D state)
        {
            var placed=state.Plots.Where(value=>!string.IsNullOrWhiteSpace(value.BuildingId)).Take(6).ToArray();
            if (placed.Length == 0)
            {
                AddMessagePanel(body,"THE FIRST BUILDING IS A PROMISE","Choose a facility for the work it enables. Placement is immediate when costs are met; there is no real-time timer or empty day requirement.",RuntimeUi.Warning);
                return;
            }
            var row=AddRow(body,"Facility State Gallery 017F",12f,300f);
            foreach (var plot in placed)
            {
                var facility=GuildCityNarrativeRegistry017F.Facility(plot.BuildingId); if (facility == null) continue;
                var panel=AddColumnPanel(row,"Facility State "+plot.PlotId,1f,285f);
                var stateName=plot.StaffRecruitIds.Count>0?"STAFFED":"COMPLETE";
                GuildCityNarrativeRegistry017F.AddImage(panel,"Facility Image "+plot.PlotId,GuildCityNarrativeRegistry017F.FacilityStatePath(plot.BuildingId,stateName),155f);
                RuntimeUi.AddText(panel,"Facility Story "+plot.PlotId,facility.displayName.ToUpperInvariant()+"  •  "+stateName+"\n"+(stateName=="STAFFED"?facility.staffedLine:facility.constructionComplete),22,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
            }
        }

        private void AddContractNarrative017F(Transform panel,GuildCityContractView017D contract)
        {
            var arc=GuildCityNarrativeRegistry017F.Contract(contract.ContractId); if (arc == null) return;
            RuntimeUi.AddText(panel,"Contract Narrative 017F "+contract.ContractId,
                arc.openingTitle+"\n"+arc.sponsorName.ToUpperInvariant()+"  •  “"+arc.sponsorLine+"”\n\nGUILDMASTER  •  “"+arc.guildmasterPrompt+"”\nDEPLOYMENT  •  "+arc.deploymentLine,
                27,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
        }

        private void AddExpeditionNarrative017F(Transform body,GuildCityExpeditionView017D expedition)
        {
            var node=GuildCityNarrativeRegistry017F.Node(expedition.BoardId,expedition.CurrentNodeId);
            if (node != null)
                AddMessagePanel(body,node.title,node.arrivalText+"\n\nDECISION  •  "+node.decisionPrompt+"\n"+node.riskLine,RuntimeUi.Accent);
            if (!string.IsNullOrWhiteSpace(expedition.CurrentEventId))
            {
                var evt=GuildCityNarrativeRegistry017F.Event(expedition.CurrentEventId);
                if (evt != null)
                    RuntimeUi.AddText(body,"Event Narrative 017F "+evt.id,
                        evt.speakerTag.ToUpperInvariant()+"  •  "+string.Join("\n",evt.openingLines ?? Array.Empty<string>())+"\n\nPOSSIBLE APPROACHES\n• "+string.Join("\n• ",evt.choicePrompts ?? Array.Empty<string>()),
                        28,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
            }
        }

        private void AddRelationshipNarrative017F(Transform body,GuildCityPresentationState017D state)
        {
            var memory=state.Relationships.FirstOrDefault(value=>!value.Viewed) ?? state.Relationships.FirstOrDefault();
            if (memory == null) return;
            var scene=GuildCityNarrativeRegistry017F.RelationshipForSummary(memory.Summary); if (scene == null) return;
            AddMessagePanel(body,scene.title,scene.setup+"\n\n“"+scene.firstLine+"”\n“"+scene.secondLine+"”\n\n"+scene.closing+"\nFREE SCENE  •  ZERO OPERATION COST  •  NEVER EXPIRES",RuntimeUi.Positive);
        }
    }
}
