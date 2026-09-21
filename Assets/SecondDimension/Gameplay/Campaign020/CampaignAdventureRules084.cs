using System;
using System.Collections.Generic;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.Campaign020
{
    /// <summary>
    /// Player-facing tile projection for the 82 authored chapter blueprints.
    /// Step and reward authority stays in CampaignPlayableCommandService020.
    /// Unknown future step kinds fail closed instead of being shown as a generic
    /// button that could accidentally skip authored gameplay.
    /// </summary>
    public static class CampaignAdventureRules084
    {
        public const string WorldBoardStepKind084 = "WORLD_BOARD";

        private static readonly HashSet<string> StepKinds084 =
            new HashSet<string>(new[]
            {
                "BRIEFING", "WORLD_BOARD", "CIVIC_EVENT", "DIPLOMACY",
                "FORTRESS_PREPARATION", "CERTIFIED_BATTLE",
                "NONCOMBAT_RESOLUTION", "RESULTS"
            }, StringComparer.Ordinal);

        public static bool IsKnownStepKind084(string kind) =>
            StepKinds084.Contains((kind ?? string.Empty).ToUpperInvariant());

        public static bool IsWorldBoardStep084(CampaignStepRule020 step) =>
            step != null &&
            StringComparer.Ordinal.Equals(step.Kind, WorldBoardStepKind084);

        public static bool IsCompatible084(CampaignBlueprintRule020 blueprint) =>
            IsCompatible084(blueprint, out _);

        public static bool IsCompatible084(
            CampaignBlueprintRule020 blueprint,
            out string reason)
        {
            reason = string.Empty;
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.BlueprintId) ||
                string.IsNullOrWhiteSpace(blueprint.ChapterId) ||
                string.IsNullOrWhiteSpace(blueprint.WorldId))
            {
                reason = "BLUEPRINT_IDENTITY_REQUIRED";
                return false;
            }
            if (blueprint.Steps == null || blueprint.Steps.Count < 3)
            {
                reason = "CHAPTER_TILES_REQUIRED";
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var boardCount = 0;
            var battleCount = 0;
            for (var index = 0; index < blueprint.Steps.Count; index++)
            {
                var step = blueprint.Steps[index];
                if (step == null || string.IsNullOrWhiteSpace(step.StepId) ||
                    string.IsNullOrWhiteSpace(step.Title) ||
                    !ids.Add(step.StepId))
                {
                    reason = "STEP_IDENTITY_INVALID";
                    return false;
                }
                if (!IsKnownStepKind084(step.Kind))
                {
                    reason = "STEP_KIND_CLOSED_WORLD:" + (step.Kind ?? string.Empty);
                    return false;
                }
                if (IsWorldBoardStep084(step)) boardCount++;
                if (step.RequiresCertifiedBattle) battleCount++;
                if (step.RequiresCertifiedBattle !=
                    StringComparer.Ordinal.Equals(step.Kind, "CERTIFIED_BATTLE"))
                {
                    reason = "BATTLE_STEP_AUTHORITY_INVALID";
                    return false;
                }
            }
            if (boardCount != 1)
            {
                reason = "EXACT_WORLD_BOARD_TILE_REQUIRED";
                return false;
            }
            if (battleCount > 1)
            {
                reason = "BATTLE_STEP_COUNT_CLOSED_WORLD";
                return false;
            }
            if (blueprint.RequiresCertifiedBattle != (battleCount == 1))
            {
                reason = "BATTLE_BLUEPRINT_AUTHORITY_INVALID";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(blueprint.Steps[0].Kind, "BRIEFING") ||
                !StringComparer.Ordinal.Equals(
                    blueprint.Steps[blueprint.Steps.Count - 1].Kind,
                    "RESULTS"))
            {
                reason = "CHAPTER_ENDPOINT_TILES_INVALID";
                return false;
            }
            return true;
        }

        public static string TileType084(string kind)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "BRIEFING": return "QUEST BRIEF";
                case "WORLD_BOARD": return "ADVENTURE BOARD";
                case "CIVIC_EVENT": return "STORY EVENT";
                case "DIPLOMACY": return "CHARACTER DECISION";
                case "FORTRESS_PREPARATION": return "BATTLE PREPARATION";
                case "CERTIFIED_BATTLE": return "UNION BATTLE";
                case "NONCOMBAT_RESOLUTION": return "QUEST OBJECTIVE";
                case "RESULTS": return "RETURN HOME";
                default: return "UNSUPPORTED TILE";
            }
        }

        public static string ActionLabel084(string kind)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "BRIEFING": return "ACCEPT THE QUEST & FLIP THE ROUTE";
                case "WORLD_BOARD": return "OPEN THE TILE-FLIP QUEST BOARD";
                case "CIVIC_EVENT": return "FLIP THE STORY EVENT";
                case "DIPLOMACY": return "FLIP THE CHARACTER DECISION";
                case "FORTRESS_PREPARATION": return "SET THE UNIONS & FLIP PREPARATION";
                case "CERTIFIED_BATTLE": return "ENTER THE UNION BATTLE";
                case "NONCOMBAT_RESOLUTION": return "COMPLETE THE QUEST OBJECTIVE";
                case "RESULTS": return "FLIP THE RETURN-HOME TILE";
                default: return "UNSUPPORTED TILE";
            }
        }

        public static string RewardPreview084(CampaignStepRule020 step)
        {
            if (step == null || !IsKnownStepKind084(step.Kind)) return string.Empty;
            if (step.RequiresCertifiedBattle)
                return "EXISTING BATTLE EQUIPMENT + 4 GUILD XP + 4 HALL XP • SAVED EXACTLY ONCE";
            if (IsWorldBoardStep084(step))
                return "MOVE YOUR PAWN • FLIP ROOMS • STORY, CHECKS, TREASURE, CAMPS, AND BATTLES • BOARD REWARDS ARE COMMITTED INSIDE THE QUEST AND SAVED EXACTLY ONCE";
            return step.ConsumesOperation
                ? "+2 GUILD XP + 2 HALL XP • WORLD MATERIAL • SAVED EXACTLY ONCE"
                : "STORY REVEAL • NO MENU XP • NO DAY COST";
        }

        public static string TileOrderKey084(
            string operationId,
            string stepId,
            int index) => CanonicalJson.Sha256Hex(new
        {
            Rule = "CAMPAIGN_ADVENTURE_TILE_084",
            Operation = operationId ?? string.Empty,
            Step = stepId ?? string.Empty,
            Index = index
        });
    }
}
