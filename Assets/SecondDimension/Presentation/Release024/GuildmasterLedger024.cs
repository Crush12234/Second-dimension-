using System;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void BuildGuildmasterLedger024(
            Transform body,
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var health = Release024.FullGameIntegrationHealthService024.BuildSnapshot();
            AddMessagePanel(
                body,
                health.IsReady ? "FULL GAME INTEGRATION READY" : "FULL GAME INTEGRATION NEEDS ATTENTION",
                health.SummaryLines == null || health.SummaryLines.Length == 0
                    ? health.Error
                    : string.Join("\n", health.SummaryLines),
                health.IsReady ? RuntimeUi.Positive : RuntimeUi.Warning);

            if (!health.IsReady && health.Issues != null && health.Issues.Length > 0)
                AddMessagePanel(
                    body,
                    "INTEGRATION ISSUES",
                    string.Join("\n", health.Issues),
                    RuntimeUi.Warning);

            var strategicCoordinator =
                _coordinator as GuildCity017H.IGuildCityStrategicPresentationCoordinator017H;
            var strategic = strategicCoordinator?.GuildCityStrategic017H;
            var campaignCoordinator =
                _coordinator as Campaign019.ICampaignPresentationCoordinator019;
            var campaign = campaignCoordinator?.Campaign019;
            var playableCoordinator =
                _coordinator as Campaign020.ICampaignPlayablePresentationCoordinator020;
            var playable = playableCoordinator?.CampaignPlayable020;
            var worldGateCoordinator =
                _coordinator as Campaign023.ICampaignWorldGatePresentationCoordinator023;
            var worldGate = worldGateCoordinator?.CampaignWorldGate023;
            var progressionCoordinator =
                _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
            var progression = progressionCoordinator?.CampaignProgression022;

            var nextTab = "GUIDE";
            var nextTitle = "Review the contextual Guide";
            var nextReason = "The current systems are stable. Continue from the next unfinished meaningful action.";
            var nextScreen = M1Screen.GuildOperations;

            if (state.HasUnclaimedBattleReward)
            {
                nextTab = "EXPEDITION";
                nextTitle = "Claim the committed battle reward";
                nextReason = "An existing exact-once equipment reward is waiting. Claim it before starting another battle.";
            }
            else if (worldGate != null && !string.IsNullOrWhiteSpace(worldGate.ActiveOperationId))
            {
                nextTab = "WORLD GATE";
                nextTitle = "Resume the active World Gate operation";
                nextReason = worldGate.ActiveDefinitionId + " • " + worldGate.ActiveStatus;
            }
            else if (state.HasPendingEncounter || state.HasPendingBattleReturn)
            {
                nextTab = "EXPEDITION";
                nextTitle = "Resolve the active expedition encounter";
                nextReason = "The committed board and battle return point are already saved.";
            }
            else if (strategic?.ActiveDefense != null)
            {
                nextTab = "DEFENSE";
                nextTitle = "Resume the active Skyhome defense";
                nextReason = strategic.ActiveDefense.DisplayName + " • " + strategic.ActiveDefense.Status;
            }
            else if (campaign != null && !string.IsNullOrWhiteSpace(campaign.ActiveChapterId))
            {
                nextTab = "CAMPAIGN";
                nextTitle = "Resume the active campaign chapter";
                nextReason = campaign.ActiveChapterId;
            }
            else if (!state.HasRecruitmentBoard || state.SignedApplicantCount == 0)
            {
                nextTab = "APPLICANTS";
                nextTitle = "Recruit the next permanent member";
                nextReason = "The Guild needs a committed Applicant Board and a signed recruit.";
            }
            else if (state.EquippedRecruitCount < Math.Min(3, state.TotalRecruitCount))
            {
                nextTitle = "Equip the active roster";
                nextReason = "Manual equipment controls legal weapon Arts and Forecast predictions.";
                nextScreen = M1Screen.Equipment;
            }
            else if (state.NormalUnionCount < 2)
            {
                nextTitle = "Build two legal Unions";
                nextReason = "The opening expedition and later multi-lane defense require prepared Unions.";
                nextScreen = M1Screen.UnionBuilder;
            }
            else if (state.PlacedBuildingCount == 0)
            {
                nextTab = "CITY";
                nextTitle = "Place the first city facility";
                nextReason = "Every building contributes to operations and earned progression.";
            }
            else if (!state.HasActiveContract)
            {
                nextTab = "CONTRACTS";
                nextTitle = "Choose the next Guild responsibility";
                nextReason = "Contracts commit objectives, risks, rewards, and expedition boards.";
            }

            var priority = AddMessagePanel(
                body,
                "CONTINUE HERE — " + nextTitle.ToUpperInvariant(),
                nextReason,
                RuntimeUi.Accent);
            RuntimeUi.AddButton(
                priority,
                "Ledger Continue 024",
                "CONTINUE",
                () =>
                {
                    if (nextScreen != M1Screen.GuildOperations)
                    {
                        Navigate(nextScreen);
                        return;
                    }
                    _guildCityTab017D = nextTab;
                    BuildCurrentScreen();
                },
                126f,
                RuntimeUi.Positive);

            AddMessagePanel(
                body,
                "GUILD & CITY",
                "RECRUITS " + state.TotalRecruitCount +
                " • EQUIPPED " + state.EquippedRecruitCount +
                " • UNIONS " + state.NormalUnionCount +
                " • INVENTORY " + state.InventoryItemCount +
                "\nBUILDINGS " + state.PlacedBuildingCount +
                " • STAFFED " + state.StaffedBuildingCount +
                " • UPGRADED " + state.UpgradedBuildingCount +
                " • ADJACENCY " + state.AdjacencyBonusCount +
                "\nCIVIC TRUST " + state.CivicTrust +
                " • HALL XP " + state.HallEnhancementXp +
                " • OPERATION " + state.OperationOrdinal,
                RuntimeUi.ButtonNormal);

            AddMessagePanel(
                body,
                "OPEN RESPONSIBILITIES",
                "ACTIVE CONTRACT " + (state.HasActiveContract ? "YES" : "NO") +
                " • PENDING ENCOUNTER " + (state.HasPendingEncounter ? "YES" : "NO") +
                " • UNCLAIMED REWARD " + (state.HasUnclaimedBattleReward ? "YES" : "NO") +
                "\nUNVIEWED RELATIONSHIP SCENES " + state.UnviewedRelationshipCount +
                " • VISITED EXPEDITION NODES " + state.ExpeditionVisitedNodeCount +
                " • COMMITTED CHECKS " + state.CommittedCheckCount,
                state.HasUnclaimedBattleReward ? RuntimeUi.Warning : RuntimeUi.Text);

            if (strategic != null && strategic.IsAvailable)
                AddMessagePanel(
                    body,
                    "CITY DEFENSE",
                    "ACTIVE CONTRIBUTION BUILDINGS " + strategic.ActiveContributionBuildingCount +
                    "/" + strategic.TotalBuildingCount +
                    " • DEFENSE XP " + strategic.DefenseMasteryXp +
                    " • WINS " + strategic.DefensesWon +
                    " • LOSSES " + strategic.DefensesLost +
                    (strategic.ActiveDefense == null
                        ? "\nNO ACTIVE DEFENSE"
                        : "\n" + strategic.ActiveDefense.DisplayName +
                          " • WAVE " + strategic.ActiveDefense.CurrentWave +
                          "/" + strategic.ActiveDefense.TotalWaves +
                          " • CITY " + strategic.ActiveDefense.CityIntegrity +
                          " • BARRIER " + strategic.ActiveDefense.BarrierIntegrity),
                    RuntimeUi.ButtonNormal);

            if (worldGate != null && worldGate.IsAvailable)
                AddMessagePanel(
                    body,
                    "WORLD GATE",
                    "CURRENT WORLD " + worldGate.CurrentWorldId +
                    " • TRAVEL SUPPLIES " + worldGate.TravelSupplies +
                    " • AVAILABLE BOARDS " + (worldGate.Boards?.Count ?? 0) +
                    "\nACTIVE " + (string.IsNullOrWhiteSpace(worldGate.ActiveOperationId)
                        ? "NONE"
                        : worldGate.ActiveDefinitionId + " • " + worldGate.ActiveStatus) +
                    " • UNLOCKED RECRUIT ORIGINS " +
                    (worldGate.UnlockedRecruitOriginIds?.Count ?? 0),
                    RuntimeUi.ButtonNormal);

            if (playable != null && playable.IsAvailable)
                AddMessagePanel(
                    body,
                    "CAMPAIGN",
                    "ACTIVE OPERATION " +
                    (string.IsNullOrWhiteSpace(playable.ActiveOperationId)
                        ? "NONE"
                        : playable.ActiveOperationId + " • " + playable.Status) +
                    "\nWORLD " + playable.WorldId +
                    " • CHAPTER " + playable.ActiveChapterId +
                    " • STEP " + playable.CurrentStepIndex + "/" + playable.TotalSteps,
                    RuntimeUi.ButtonNormal);

            if (progression != null && progression.IsAvailable)
                AddMessagePanel(
                    body,
                    "PROGRESSION & ENDGAME",
                    progression.WeaponTracks + " WEAPON TRACKS • " +
                    progression.AdvancedClasses + " ADVANCED CLASSES • " +
                    progression.AbyssFloors + " ABYSS FLOORS • " +
                    progression.Covenants + " COVENANTS" +
                    (string.IsNullOrWhiteSpace(progression.ActiveAbyssOperationId)
                        ? string.Empty
                        : "\nACTIVE ABYSS " + progression.ActiveAbyssOperationId +
                          " • " + progression.ActiveAbyssStatus),
                    RuntimeUi.ButtonNormal);

            var links = AddRow(body, "Guildmaster Ledger Links 024", 10f, 126f);
            AddLedgerLink024(links, "APPLICANTS");
            AddLedgerLink024(links, "CITY");
            AddLedgerLink024(links, "CONTRACTS");
            AddLedgerLink024(links, "EXPEDITION");
            AddLedgerLink024(links, "DEFENSE");
            AddLedgerLink024(links, "CAMPAIGN");
            AddLedgerLink024(links, "WORLD GATE");
            AddLedgerLink024(links, "PROGRESSION");
        }

        private void AddLedgerLink024(Transform parent, string tab)
        {
            RuntimeUi.AddButton(
                parent,
                "Ledger Link " + tab,
                tab,
                () =>
                {
                    _guildCityTab017D = tab;
                    BuildCurrentScreen();
                },
                110f,
                RuntimeUi.ButtonNormal);
        }
    }
}
