using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign022
{
    public sealed partial class CampaignProgressionCommandService022
    {
        public static bool UsesActualThreatPolicy098(CampaignState campaign, AbyssOperationState022 active) =>
            ActualThreatFloor098(campaign, active) > 0;

        // Lookup the ORIGINAL begin binding, including historical completion
        // reconstruction. Never infer its policy from today's highest floor.
        static int ActualThreatFloor098(CampaignState campaign, AbyssOperationState022 active)
        {
            if (!IsEndlessTowerOperation094(active?.OperationInstanceId)) return 0;
            if (!TryReadTowerFloorBinding098(campaign, active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.OperationInstanceId, active.OperationDefinitionId, active.FloorId, out var floor, out var policy))
                throw new InvalidOperationException("TOWER098_ORIGINAL_FLOOR_BINDING_INVALID");
            return policy == TowerThreatRules098.Policy098 || policy == TowerEnemyPartyRules137.Policy137 ||
                (policy == TowerScalingRules138.Policy138 || policy == TowerEconomy159.Policy159) ? floor : 0;
        }

        public static bool UsesFullTowerParties137(CampaignState campaign, AbyssOperationState022 active)
        {
            if (!IsEndlessTowerOperation094(active?.OperationInstanceId)) return false;
            if (!TryReadTowerFloorBinding098(campaign, active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.OperationInstanceId, active.OperationDefinitionId, active.FloorId, out _, out var policy))
                throw new InvalidOperationException("TOWER137_ORIGINAL_FLOOR_BINDING_INVALID");
            return policy == TowerEnemyPartyRules137.Policy137 || policy == TowerScalingRules138.Policy138 || policy == TowerEconomy159.Policy159;
        }

        public static bool UsesTowerScaling138(CampaignState campaign, AbyssOperationState022 active)
        {
            if (!IsEndlessTowerOperation094(active?.OperationInstanceId)) return false;
            if (!TryReadTowerFloorBinding098(campaign, active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.OperationInstanceId, active.OperationDefinitionId, active.FloorId, out _, out var policy))
                throw new InvalidOperationException("TOWER138_ORIGINAL_FLOOR_BINDING_INVALID");
            return policy == TowerScalingRules138.Policy138 || policy == TowerEconomy159.Policy159;
        }

        public static bool UsesTowerEconomy159(CampaignState campaign, AbyssOperationState022 active)
        {
            if (!IsEndlessTowerOperation094(active?.OperationInstanceId)) return false;
            if (!TryReadTowerFloorBinding098(campaign, active.BeginAuthorityHash, active.PreviousAuthorityHash,
                active.OperationInstanceId, active.OperationDefinitionId, active.FloorId, out _, out var policy))
                throw new InvalidOperationException("TOWER159_ORIGINAL_FLOOR_BINDING_INVALID");
            return policy == TowerEconomy159.Policy159;
        }

        public static void ValidateCommittedTowerThreat098(CampaignState campaign,
            EncounterLaunchRequest017D request, string battleId, IReadOnlyList<string> modifiers)
        {
            var actualFloor = TowerThreatRules098.ReadFloor098(modifiers);
            var markedBattle = battleId?.Contains(TowerThreatRules098.BattleMarker098) == true;
            if (actualFloor == 0 && !markedBattle && !TowerEnemyPartyRules137.HasMarker137(modifiers) &&
                !TowerScalingRules138.HasMarker138(modifiers) && !TowerEconomy159.HasMarker159(modifiers)) return;
            var active = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.Playable020?
                .Progression022?.ActiveAbyssOperation;
            var pending = campaign?.Guild?.GuildCity?.PendingEncounter;
            if (request == null || pending == null || active == null || actualFloor == 0 || !markedBattle ||
                ActualThreatFloor098(campaign, active) != actualFloor ||
                request.BattleId != battleId ||
                CanonicalJson.Serialize(request) != CanonicalJson.Serialize(pending) ||
                !(modifiers ?? Array.Empty<string>()).SequenceEqual(request.RouteModifiers) ||
                !campaign.Guild.Development.HasAdventureAuthority(
                    GuildCityBattleBridgeService017D.EncounterRequestAuthorityId084(request)))
                throw new InvalidOperationException("TOWER098_COMMITTED_REQUEST_REQUIRED");
            if (UsesFullTowerParties137(campaign, active) != TowerEnemyPartyRules137.ReadCommitted137(request))
                throw new InvalidOperationException("TOWER137_COMPOSITION_BINDING_REQUIRED");
            if (UsesTowerScaling138(campaign, active) != TowerScalingRules138.ReadCommitted138(request))
                throw new InvalidOperationException("TOWER138_SCALING_BINDING_REQUIRED");
            if (UsesTowerEconomy159(campaign, active) != TowerEconomy159.HasMarker159(modifiers))
                throw new InvalidOperationException("TOWER159_REWARD_POLICY_REQUIRED");
            TowerEconomy159.ReadBonus159(modifiers);
            var suffix = battleId.Substring(Math.Max(0, battleId.Length - 24));
            if (battleId != TowerThreatRules098.BattleId098(actualFloor, suffix))
                throw new InvalidOperationException("TOWER098_COMMITTED_BATTLE_ID_INVALID");
        }
    }
}
