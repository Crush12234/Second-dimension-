using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Gameplay.Campaign022
{
    /// <summary>
    /// Closed-world presentation rules for the thirty historical Abyss operations
    /// and ten separately authored battle-only repeat definitions.
    /// Progression receipts and battle rewards remain entirely owned by
    /// CampaignProgressionCommandService022.
    /// </summary>
    public static class TowerAdventureRules084
    {
        private static readonly HashSet<string> OperationKinds084 =
            new HashSet<string>(new[] { "RECON", "TRIAL", "GUARDIAN",
                CampaignProgressionCommandService022.EndlessBattleKind094 },
                StringComparer.Ordinal);

        private static readonly HashSet<string> StepKinds084 =
            new HashSet<string>(new[]
            {
                "BRIEFING", "BOARD", "EVENT", "OBJECTIVE",
                "CERTIFIED_BATTLE", "RESULTS"
            }, StringComparer.Ordinal);

        public static bool IsCompatible084(AbyssOperationDto022 operation) =>
            IsCompatible084(operation, out _);

        public static bool IsCompatible084(
            AbyssOperationDto022 operation,
            out string reason)
        {
            reason = string.Empty;
            if (operation == null || string.IsNullOrWhiteSpace(operation.operationId) ||
                string.IsNullOrWhiteSpace(operation.floorId) ||
                string.IsNullOrWhiteSpace(operation.displayName))
            {
                reason = "TOWER_OPERATION_IDENTITY_REQUIRED";
                return false;
            }
            if (!OperationKinds084.Contains(operation.kind ?? string.Empty))
            {
                reason = "TOWER_OPERATION_KIND_CLOSED_WORLD";
                return false;
            }
            if (operation.firstClearOnly !=
                StringComparer.Ordinal.Equals(operation.kind, "GUARDIAN"))
            {
                reason = "TOWER_FIRST_CLEAR_KIND_INVALID";
                return false;
            }
            if (operation.steps == null || operation.steps.Length != 5)
            {
                reason = "TOWER_FIVE_TILE_PATH_REQUIRED";
                return false;
            }
            if (operation.steps.Any(step => step == null ||
                string.IsNullOrWhiteSpace(step.stepId) ||
                string.IsNullOrWhiteSpace(step.title) ||
                !StepKinds084.Contains(step.kind ?? string.Empty)))
            {
                reason = "TOWER_STEP_CLOSED_WORLD";
                return false;
            }
            if (operation.steps.Select(step => step.stepId)
                .Distinct(StringComparer.Ordinal).Count() != operation.steps.Length)
            {
                reason = "TOWER_STEP_ID_DUPLICATE";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(operation.steps[0].kind, "BRIEFING") ||
                !StringComparer.Ordinal.Equals(operation.steps[1].kind, "BOARD") ||
                !StringComparer.Ordinal.Equals(operation.steps[2].kind, "EVENT") ||
                !(StringComparer.Ordinal.Equals(operation.steps[3].kind, "OBJECTIVE") ||
                  StringComparer.Ordinal.Equals(operation.steps[3].kind, "CERTIFIED_BATTLE")) ||
                !StringComparer.Ordinal.Equals(operation.steps[4].kind, "RESULTS"))
            {
                reason = "TOWER_TILE_ORDER_INVALID";
                return false;
            }
            var battleSteps = operation.steps.Where(step => step.requiresBattle).ToArray();
            var guardian = StringComparer.Ordinal.Equals(operation.kind, "GUARDIAN");
            var recon = StringComparer.Ordinal.Equals(operation.kind, "RECON");
            var endless = StringComparer.Ordinal.Equals(operation.kind,
                CampaignProgressionCommandService022.EndlessBattleKind094);
            if (battleSteps.Length > 1 ||
                (endless && (battleSteps.Length != 1 ||
                    !StringComparer.Ordinal.Equals(battleSteps[0].kind, "OBJECTIVE") ||
                    string.IsNullOrWhiteSpace(battleSteps[0].bossId))) ||
                (guardian && (battleSteps.Length != 1 ||
                    !StringComparer.Ordinal.Equals(battleSteps[0].kind,
                        "CERTIFIED_BATTLE"))) ||
                (recon && battleSteps.Length != 0) ||
                (!guardian && operation.steps.Any(step =>
                    StringComparer.Ordinal.Equals(step.kind, "CERTIFIED_BATTLE"))) ||
                battleSteps
                    .Any(step => !StringComparer.Ordinal.Equals(step.kind, "OBJECTIVE") &&
                                  !StringComparer.Ordinal.Equals(step.kind, "CERTIFIED_BATTLE")) ||
                operation.steps.Any(step =>
                    StringComparer.Ordinal.Equals(step.kind, "CERTIFIED_BATTLE") &&
                    !step.requiresBattle))
            {
                reason = "TOWER_BATTLE_TILE_AUTHORITY_INVALID";
                return false;
            }
            return true;
        }

        public static string PlayerOperationKind084(string kind)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "RECON": return "SCOUTING ROUTE";
                case "TRIAL": return "REPEATABLE TRIAL";
                case CampaignProgressionCommandService022.EndlessBattleKind094: return "REPEATABLE UNION BATTLE";
                case "GUARDIAN": return "FIRST-CLEAR GUARDIAN";
                default: return "UNSUPPORTED ROUTE";
            }
        }

        public static string TileLabel084(string kind, bool requiresBattle)
        {
            if (requiresBattle) return "UNION BATTLE";
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "BRIEFING": return "ENTER";
                case "BOARD": return "HIDDEN ROOM";
                case "EVENT": return "TOWER EVENT";
                case "OBJECTIVE": return "FLOOR OBJECTIVE";
                case "RESULTS": return "RETURN";
                default: return "UNSUPPORTED TILE";
            }
        }

        public static string TileDescription084(
            string kind,
            bool requiresBattle,
            string operationKind)
        {
            if (requiresBattle)
                return "The next room opens into the existing Union battle. Its battle reward remains authoritative and can be claimed only once.";
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "BRIEFING":
                    return "Place the Guild pawn at the floor entrance and commit this climb before the route is revealed.";
                case "BOARD":
                    return "Flip the face-down route tile. The authored room is committed and saved before the pawn can move.";
                case "EVENT":
                    return "Reveal the floor event. It advances through the existing exact-once Tower receipt without adding a hidden cost.";
                case "OBJECTIVE":
                    return "Complete the authored floor objective, then move the pawn toward the return tile.";
                case "RESULTS":
                    return "Return to the Guild with this route's committed result. The floor clear is banked only after its final receipt is applied.";
                default:
                    return PlayerOperationKind084(operationKind) + " cannot advance through an unknown tile.";
            }
        }

        public static int TrackPhase084(
            int currentStepIndex,
            AbyssOperationStatus022 status)
        {
            if (status == AbyssOperationStatus022.ReadyToFinalize ||
                status == AbyssOperationStatus022.Completed) return 4;
            return Math.Max(0, Math.Min(4, currentStepIndex));
        }

        public static bool IsAvailable084(
            AbyssOperationDto022 operation,
            int floorNumber,
            int floorClearCount,
            bool previousFloorCleared,
            bool floorFirstClearApplied)
        {
            if (!IsCompatible084(operation) || floorNumber < 1 || floorClearCount < 0)
                return false;
            if (operation.requiresPreviousFloorClear && !previousFloorCleared)
                return false;
            if (operation.firstClearOnly && floorClearCount > 0)
                return false;
            if (!operation.firstClearOnly &&
                (floorClearCount <= 0 || !floorFirstClearApplied))
                return false;
            return true;
        }

        public static string LockedReason084(
            AbyssOperationDto022 operation,
            int floorClearCount,
            bool previousFloorCleared,
            bool floorFirstClearApplied)
        {
            if (operation == null) return "ROUTE UNAVAILABLE";
            if (operation.requiresPreviousFloorClear && !previousFloorCleared)
                return "CLEAR THE PREVIOUS FLOOR FIRST";
            if (operation.firstClearOnly && floorClearCount > 0)
                return "FIRST-CLEAR GUARDIAN ALREADY DEFEATED";
            if (!operation.firstClearOnly &&
                (floorClearCount <= 0 || !floorFirstClearApplied))
                return "CLEAR THIS FLOOR'S GUARDIAN FIRST";
            return string.Empty;
        }

        public static string ReceiptReward084(ProgressionReceipt022 receipt)
        {
            if (receipt == null) return string.Empty;
            var parts = new List<string>();
            if (receipt.GuildXp > 0) parts.Add("+" + receipt.GuildXp + " GUILD XP");
            if (receipt.HallXp > 0) parts.Add("+" + receipt.HallXp + " HALL XP");
            if (receipt.SummonResonance > 0)
                parts.Add("+" + receipt.SummonResonance + " RESONANCE");
            if ((receipt.MaterialIds?.Count ?? 0) > 0)
                parts.Add("MATERIALS ×" + receipt.MaterialIds.Count);
            return parts.Count == 0
                ? "ROUTE PROGRESS SAVED"
                : string.Join("  •  ", parts);
        }
    }
}
