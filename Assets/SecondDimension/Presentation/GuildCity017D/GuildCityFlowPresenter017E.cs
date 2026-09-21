using System;
using System.Linq;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017E;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void AddHallOverviewVisual017E(Transform body,GuildCityPresentationState017D state)
        {
            var root = GuildCityOpeningExperienceRegistry017E.Load();
            var row = AddRow(body,"Guild District Strip 017E",14f,180f);
            foreach (var district in root.districts)
            {
                var panel = AddColumnPanel(row,"District "+district.id,1f,170f);
                GuildCityOpeningExperienceRegistry017E.AddImage(panel,"District Icon "+district.id,district.iconResourcePath,72f);
                RuntimeUi.AddText(panel,"District Name "+district.id,district.name.ToUpperInvariant(),24,TextAnchor.MiddleCenter,RuntimeUi.Accent,FontStyle.Bold);
            }
        }

        private void AddCityOverviewVisual017E(Transform body,GuildCityPresentationState017D state)
        {
            GuildCityOpeningExperienceRegistry017E.AddImage(body,"Opening City Map 017E",GuildCityOpeningExperienceRegistry017E.CityMapPath,470f);
            var root = GuildCityOpeningExperienceRegistry017E.Load();
            var row = AddRow(body,"District Identity Row 017E",12f,210f);
            foreach (var district in root.districts)
            {
                var panel = AddColumnPanel(row,"District Detail "+district.id,1f,200f);
                GuildCityOpeningExperienceRegistry017E.AddImage(panel,"District Detail Icon "+district.id,district.iconResourcePath,64f);
                RuntimeUi.AddText(panel,"District Detail Text "+district.id,district.name.ToUpperInvariant()+"\n"+district.identity,23,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
            }
        }

        private void AddContractVisual017E(Transform panel,GuildCityContractView017D contract)
        {
            var detail = GuildCityOpeningExperienceRegistry017E.Contract(contract.ContractId);
            if (detail == null) return;
            var row = AddRow(panel,"Contract Visual "+contract.ContractId,12f,170f);
            GuildCityOpeningExperienceRegistry017E.AddImage(row,"Contract Seal "+contract.ContractId,detail.sealResourcePath,150f);
            RuntimeUi.AddText(row,"Contract Brief "+contract.ContractId,
                "RANK "+detail.rank+"  •  "+detail.estimatedMinutes+" MIN  •  "+detail.pressure.ToUpperInvariant()+"\n"+
                detail.briefing+"\nEQUIPMENT POSSIBILITIES  •  "+string.Join("  •  ",detail.equipmentPossibilities ?? Array.Empty<string>()),
                27,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
        }

        private string FormatContractDetail017E(GuildCityContractView017D contract)
        {
            var detail = GuildCityOpeningExperienceRegistry017E.Contract(contract.ContractId);
            var result = contract.Hook+"\n\nPRIMARY  •  "+contract.PrimaryObjective+
                "\nOPTIONAL  •  "+string.Join("  •  ",contract.OptionalObjectives)+
                "\nHAZARDS  •  "+string.Join("  •  ",contract.Hazards)+
                "\nRECOMMENDED  •  "+string.Join("  •  ",contract.RecommendedSkills)+
                "\nREWARD  •  GUILD XP "+contract.GuildXp+"  •  HALL XP "+contract.HallXp+
                "\nCITY  •  "+contract.CityHook;
            if (detail == null) return result;
            return result+"\nFAILURE  •  "+detail.failureConsequence+"\nRETREAT  •  "+detail.retreatConsequence+"\nUNLOCK  •  "+detail.cityUnlock;
        }

        private void AddExpeditionVisual017E(Transform body,GuildCityExpeditionView017D expedition)
        {
            var mapPath = GuildCityOpeningExperienceRegistry017E.BoardMapPath(expedition.BoardId);
            if (!string.IsNullOrWhiteSpace(mapPath)) GuildCityOpeningExperienceRegistry017E.AddImage(body,"Expedition Map 017E",mapPath,430f);
            var node = GuildCityOpeningExperienceRegistry017E.Node(expedition.BoardId,expedition.CurrentNodeId);
            if (node == null) return;
            var panel = AddRow(body,"Current Node Detail 017E",12f,180f);
            GuildCityOpeningExperienceRegistry017E.AddImage(panel,"Current Node Icon 017E",node.iconResourcePath,138f);
            RuntimeUi.AddText(panel,"Current Node Narrative 017E",
                node.displayName.ToUpperInvariant()+"  •  RISK "+node.risk.ToUpperInvariant()+"\n"+node.summary+
                "\n"+node.routeCostSummary+(string.IsNullOrWhiteSpace(node.recommendedSkill)?string.Empty:"  •  RECOMMENDED "+node.recommendedSkill.ToUpperInvariant()),
                29,TextAnchor.UpperLeft,RuntimeUi.Text,FontStyle.Bold);
        }

        private string FormatRouteChoice017E(string boardId,string destinationNodeId)
        {
            var node = GuildCityOpeningExperienceRegistry017E.Node(boardId,destinationNodeId);
            return node == null ? "MOVE TO "+destinationNodeId : node.displayName.ToUpperInvariant()+"\n"+node.routeCostSummary;
        }
    }
}
