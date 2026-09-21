
using System;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017G;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.GuildCity017G;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void AddOpeningGuideBanner017G(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            if (state == null) return;
            var snapshot = Snapshot017G(state);
            var current = GuildCityOpeningCoach017G.Current(snapshot);
            if (StringComparer.Ordinal.Equals(state.TutorialDepthId, "FastCharter") &&
                !state.HasUnclaimedBattleReward && !state.HasPendingEncounter) return;
            var copy = GuildCityOpeningCertificationRegistry017G.Step(current.Id);
            var detail = copy == null ? current.Summary : copy.shortBody + "\n" + copy.whyItMatters;
            var progress = GuildCityOpeningCoach017G.ProgressPercent(snapshot);
            var row = AddRow(body, "Opening Guidance Banner 017G", 14f, 190f);
            var panel = AddColumnPanel(row, "Opening Guidance Copy 017G", 3f, 180f);
            RuntimeUi.AddText(panel, "Opening Guidance Heading 017G",
                "OPENING GUIDANCE  •  " + progress + "%  •  " + current.Title.ToUpperInvariant(),
                31, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.AddText(panel, "Opening Guidance Detail 017G", detail,
                25, TextAnchor.UpperLeft, RuntimeUi.Text);
            AddGuideActionButton017G(row, coordinator, state, current, 1f);
        }

        private void BuildGuildCityGuide017G(Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var snapshot = Snapshot017G(state);
            var profile = GuildCityOpeningBalance017G.For(ParseGameMode017G(state.CampaignModeId));
            var current = GuildCityOpeningCoach017G.Current(snapshot);
            var progress = GuildCityOpeningCoach017G.ProgressPercent(snapshot);
            AddMessagePanel(body, "THE GUILDMASTER'S OPENING PLAN",
                "This guide is contextual, nonblocking, and derived from the real save. It never spends an operation or changes authoritative outcomes.\n\n" +
                "CURRENT  •  " + current.Title + "\nPROGRESS  •  " + progress + "%\nMODE  •  " + profile.DisplayName +
                "\nTUTORIAL  •  " + state.TutorialDepthId,
                RuntimeUi.Accent);

            var modeRow = AddRow(body, "Mode Accessibility Row 017G", 12f, 205f);
            var modePanel = AddColumnPanel(modeRow, "Mode Profile 017G", 1f, 195f);
            RuntimeUi.AddText(modePanel, "Mode Heading 017G", profile.DisplayName.ToUpperInvariant(),
                30, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold);
            RuntimeUi.AddText(modePanel, "Mode Detail 017G",
                "SUPPLIES " + Signed(profile.StartingSupplyBonus) + "  •  CHECKS " + Signed(profile.CheckModifier) +
                "\nFATIGUE COST " + Signed(profile.FatigueCostDelta) + "  •  URGENCY COST " + Signed(profile.UrgencyCostDelta) +
                "\nENEMY UNIONS " + Signed(profile.EnemyUnionDelta) +
                "\nGUILD/HALL REWARD  •  " + profile.GuildRewardBasisPoints / 100 + "% / " + profile.HallRewardBasisPoints / 100 + "%",
                24, TextAnchor.UpperLeft, RuntimeUi.Text);
            var accessPanel = AddColumnPanel(modeRow, "Accessibility Profile 017G", 1f, 195f);
            RuntimeUi.AddText(accessPanel, "Accessibility Heading 017G", "ACCESSIBILITY",
                30, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold);
            RuntimeUi.AddText(accessPanel, "Accessibility Detail 017G",
                "TEXT " + state.TextScalePercent + "%  •  HIGH CONTRAST " + YesNo(state.HighContrast) +
                "\nREDUCED MOTION " + YesNo(state.ReducedMotion) +
                "\nFORECAST DETAIL  •  " + state.ForecastDetail.ToUpperInvariant() +
                "\nCOMBAT SPEED  •  " + state.CombatSpeed + "X",
                24, TextAnchor.UpperLeft, RuntimeUi.Text);

            AddGuideActionButton017G(body, coordinator, state, current, 1f);

            var steps = GuildCityOpeningCoach017G.Evaluate(snapshot);
            foreach (var step in steps)
            {
                var copy = GuildCityOpeningCertificationRegistry017G.Step(step.Id);
                var row = AddRow(body, "Guide Step " + step.Id, 12f, 132f);
                RuntimeUi.AddText(row, "Guide Step Text " + step.Id,
                    (step.Completed ? "✓  " : "○  ") + step.Title.ToUpperInvariant() +
                    "\n" + (copy?.shortBody ?? step.Summary),
                    26, TextAnchor.MiddleLeft,
                    step.Completed ? RuntimeUi.Positive : RuntimeUi.Text,
                    step.Completed ? FontStyle.Normal : FontStyle.Bold);
                if (!step.Completed) AddGuideActionButton017G(row, coordinator, state, step, 0.55f);
            }

            var pacing = GuildCityOpeningCertificationRegistry017G.Load().pacingTargets ?? Array.Empty<GuildCityPacingTarget017G>();
            var pacePanel = AddColumnPanel(body, "Pacing Targets 017G", 1f, 310f);
            RuntimeUi.AddText(pacePanel, "Pacing Heading 017G", "OWNER-REVIEW PACING TARGETS",
                34, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            for (var index = 0; index < pacing.Length; index++)
                RuntimeUi.AddText(pacePanel, "Pacing " + pacing[index].id,
                    pacing[index].id.Replace('_',' ') + "  •  TARGET " + pacing[index].targetMinute + " MIN" +
                    "  •  ACCEPTABLE " + pacing[index].earliestMinute + "–" + pacing[index].latestMinute + " MIN\n" + pacing[index].evidence,
                    23, TextAnchor.UpperLeft, RuntimeUi.Text);
        }

        private GuildCityOpeningSnapshot017G Snapshot017G(GuildCityPresentationState017D state) =>
            new GuildCityOpeningSnapshot017G
            {
                OperationOrdinal = state.OperationOrdinal,
                HasRecruitmentBoard = state.HasRecruitmentBoard,
                SignedApplicantCount = state.SignedApplicantCount,
                TotalRecruitCount = state.TotalRecruitCount,
                EquippedRecruitCount = state.EquippedRecruitCount,
                NormalUnionCount = state.NormalUnionCount,
                PlacedBuildingCount = state.PlacedBuildingCount,
                StaffedBuildingCount = state.StaffedBuildingCount,
                UpgradedBuildingCount = state.UpgradedBuildingCount,
                AdjacencyBonusCount = state.AdjacencyBonusCount,
                HasActiveContract = state.HasActiveContract,
                HasExpedition = state.Expedition != null,
                ExpeditionVisitedNodeCount = state.ExpeditionVisitedNodeCount,
                CommittedCheckCount = state.CommittedCheckCount,
                CanCommitEncounter = state.Expedition != null && state.Expedition.CanCommitEncounter,
                HasPendingEncounter = state.HasPendingEncounter,
                HasActiveCertifiedBattle = state.IsCertifiedEncounterBattle,
                HasUnclaimedBattleReward = state.HasUnclaimedBattleReward,
                ClaimedBattleRewardCount = state.ClaimedBattleRewardCount,
                CanFinalizeOperation = state.Expedition != null && state.Expedition.CanFinalizeOperation,
                RelationshipCount = state.RelationshipCount,
                UnviewedRelationshipCount = state.UnviewedRelationshipCount
            };

        private void AddGuideActionButton017G(Transform parent,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            GuildCityGuideStep017G step,
            float flexibleWidth)
        {
            if (step == null || step.Destination == GuildCityGuideDestination017G.Complete) return;
            var button = RuntimeUi.AddButton(parent, "Guide Action " + step.Id,
                string.IsNullOrWhiteSpace(step.Action) ? "OPEN" : step.Action.ToUpperInvariant(),
                () => GoToGuideDestination017G(coordinator, state, step.Destination),
                118f, RuntimeUi.Accent);
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null) layout.flexibleWidth = flexibleWidth;
        }

        private void GoToGuideDestination017G(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            GuildCityGuideDestination017G destination)
        {
            switch (destination)
            {
                case GuildCityGuideDestination017G.Hall: _guildCityTab017D = "HALL"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.Applicants: _guildCityTab017D = "APPLICANTS"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.City: _guildCityTab017D = "CITY"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.Contracts: _guildCityTab017D = "CONTRACTS"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.Expedition: _guildCityTab017D = "EXPEDITION"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.Relationships: _guildCityTab017D = "RELATIONSHIPS"; BuildCurrentScreen(); break;
                case GuildCityGuideDestination017G.Equipment: Navigate(M1Screen.Equipment); break;
                case GuildCityGuideDestination017G.Unions: Navigate(M1Screen.UnionBuilder); break;
                case GuildCityGuideDestination017G.Battle:
                    if (state.HasPendingEncounter) EnterCommittedGuildCityBattle017D(coordinator);
                    else { _guildCityTab017D = "EXPEDITION"; BuildCurrentScreen(); }
                    break;
                case GuildCityGuideDestination017G.Results: Navigate(M1Screen.Battle); break;
                default: BuildCurrentScreen(); break;
            }
        }

        private static SecondDimension.Core.GameMode ParseGameMode017G(string value) =>
            Enum.TryParse(value, true, out SecondDimension.Core.GameMode mode)
                ? mode
                : SecondDimension.Core.GameMode.Standard;
        private static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
        private static string YesNo(bool value) => value ? "ON" : "OFF";
    }
}
