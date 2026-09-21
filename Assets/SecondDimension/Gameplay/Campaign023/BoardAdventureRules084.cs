using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.Campaign023
{
    /// <summary>
    /// Closed-world projection of the authored World Gate boards into the compact
    /// move / flip / reveal loop.  It never creates a node, reward, check, route,
    /// or battle: the Campaign 023 board and receipt services remain authoritative.
    /// </summary>
    public static class BoardAdventureRules084
    {
        private static readonly HashSet<string> OperationKinds084 =
            new HashSet<string>(new[] { "CHAPTER", "REPEATABLE", "CRISIS" },
                StringComparer.Ordinal);

        private static readonly HashSet<string> NodeKinds084 =
            new HashSet<string>(new[]
            {
                "START", "ROUTE", "EVENT", "CHECK", "CAMP", "DIPLOMACY",
                "RESOURCE", "OBJECTIVE", "BATTLE", "DEFENSE_PREP", "EXIT"
            }, StringComparer.Ordinal);

        private static readonly Regex AuthorityToken084 = new Regex(
            @"\b(?:CH018|BOARD023|B023|REPEAT020|CRISIS020|MAT020|ENEMYPACK020|LOOTPROFILE020)_[A-Z0-9_]+\b",
            RegexOptions.CultureInvariant);

        public static bool IsCompatible084(WorldGateBoardRule023 board) =>
            IsCompatible084(board, out _);

        public static bool IsCompatible084(WorldGateBoardRule023 board, out string reason)
        {
            reason = string.Empty;
            if (board == null)
            {
                reason = "BOARD_REQUIRED";
                return false;
            }
            if (string.IsNullOrWhiteSpace(board.BoardId) ||
                string.IsNullOrWhiteSpace(board.DefinitionId) ||
                string.IsNullOrWhiteSpace(board.WorldId) ||
                string.IsNullOrWhiteSpace(board.Title) ||
                string.IsNullOrWhiteSpace(board.StartNodeId) ||
                string.IsNullOrWhiteSpace(board.ExitNodeId))
            {
                reason = "BOARD_IDENTITY_REQUIRED";
                return false;
            }
            if (!OperationKinds084.Contains(board.OperationKind ?? string.Empty))
            {
                reason = "OPERATION_KIND_CLOSED_WORLD";
                return false;
            }
            if (board.MaximumAlliedUnions < 1 || board.MaximumAlliedUnions > 10 ||
                board.MaximumEnemyUnions < 1 || board.MaximumEnemyUnions > 10)
            {
                reason = "UNION_CAP_INVALID";
                return false;
            }
            if (board.Nodes == null || board.Nodes.Count < 2)
            {
                reason = "BOARD_NODES_REQUIRED";
                return false;
            }

            var byId = new Dictionary<string, WorldGateNodeRule023>(StringComparer.Ordinal);
            for (var index = 0; index < board.Nodes.Count; index++)
            {
                var node = board.Nodes[index];
                if (node == null || string.IsNullOrWhiteSpace(node.NodeId) ||
                    string.IsNullOrWhiteSpace(node.Title))
                {
                    reason = "NODE_IDENTITY_REQUIRED";
                    return false;
                }
                if (!NodeKinds084.Contains(node.Kind ?? string.Empty))
                {
                    reason = "NODE_KIND_CLOSED_WORLD:" + (node.Kind ?? string.Empty);
                    return false;
                }
                if (byId.ContainsKey(node.NodeId))
                {
                    reason = "DUPLICATE_NODE";
                    return false;
                }
                byId.Add(node.NodeId, node);
                if (node.RequiresCertifiedBattle &&
                    (!StringComparer.Ordinal.Equals(node.Kind, "BATTLE") ||
                     node.EnemyUnionCount < 1 ||
                     node.EnemyUnionCount > board.MaximumEnemyUnions))
                {
                    reason = "BATTLE_AUTHORITY_INVALID";
                    return false;
                }
                if (!node.RequiresCertifiedBattle &&
                    (StringComparer.Ordinal.Equals(node.Kind, "BATTLE") ||
                     node.EnemyUnionCount != 0))
                {
                    reason = "BATTLE_AUTHORITY_REQUIRED";
                    return false;
                }
                if (node.CheckDifficulty < 0 || node.GuildXp < 0 || node.HallXp < 0)
                {
                    reason = "NODE_NUMBERS_INVALID";
                    return false;
                }
            }
            if (board.UsesCertifiedBattle !=
                board.Nodes.Any(node => node.RequiresCertifiedBattle))
            {
                reason = "BOARD_BATTLE_METADATA_MISMATCH";
                return false;
            }
            if (board.Nodes.Count(node => node.RequiresCertifiedBattle) > 1)
            {
                reason = "BATTLE_TILE_COUNT_CLOSED_WORLD";
                return false;
            }
            if (!byId.ContainsKey(board.StartNodeId) || !byId.ContainsKey(board.ExitNodeId))
            {
                reason = "BOARD_ENDPOINT_UNKNOWN";
                return false;
            }
            if (!StringComparer.Ordinal.Equals(byId[board.StartNodeId].Kind, "START") ||
                !StringComparer.Ordinal.Equals(byId[board.ExitNodeId].Kind, "EXIT"))
            {
                reason = "BOARD_ENDPOINT_KIND_INVALID";
                return false;
            }

            foreach (var node in board.Nodes)
            {
                var next = node.NextNodeIds ?? Array.Empty<string>();
                var choices = node.ChoiceIds ?? Array.Empty<string>();
                if (choices.Count > 3 || choices.Any(string.IsNullOrWhiteSpace) ||
                    choices.Distinct(StringComparer.Ordinal).Count() != choices.Count)
                {
                    reason = choices.Count > 3
                        ? "CHOICE_COUNT_CLOSED_WORLD"
                        : "CHOICE_IDENTITY_INVALID";
                    return false;
                }
                for (var index = 0; index < next.Count; index++)
                    if (!byId.ContainsKey(next[index]))
                    {
                        reason = "NEXT_NODE_UNKNOWN";
                        return false;
                    }
                if (StringComparer.Ordinal.Equals(node.Kind, "EXIT") && next.Count != 0)
                {
                    reason = "EXIT_MUST_CLOSE_BOARD";
                    return false;
                }
                if (!StringComparer.Ordinal.Equals(node.Kind, "EXIT") && next.Count == 0)
                {
                    reason = "NON_EXIT_DEAD_END";
                    return false;
                }
                var routed = new HashSet<string>(StringComparer.Ordinal);
                var legalChoices = choices.Count == 0
                    ? new[] { "CONTINUE" }
                    : choices;
                for (var index = 0; index < legalChoices.Count; index++)
                {
                    if (!TryNextNode084(node, legalChoices[index], out var destination))
                    {
                        reason = "CHOICE_ROUTE_UNSUPPORTED";
                        return false;
                    }
                    if (!string.IsNullOrWhiteSpace(destination)) routed.Add(destination);
                }
                if (!routed.SetEquals(next))
                {
                    reason = "AUTHORED_EDGE_UNREACHABLE";
                    return false;
                }
            }

            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(board.StartNodeId);
            while (queue.Count > 0)
            {
                var id = queue.Dequeue();
                if (!reachable.Add(id)) continue;
                foreach (var next in byId[id].NextNodeIds ?? Array.Empty<string>())
                    queue.Enqueue(next);
            }
            if (reachable.Count != byId.Count || !reachable.Contains(board.ExitNodeId))
            {
                reason = "BOARD_ROUTE_NOT_CLOSED";
                return false;
            }
            if (HasCycle084(board.StartNodeId, byId,
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal)))
            {
                reason = "BOARD_ROUTE_CYCLE_FORBIDDEN";
                return false;
            }
            var reachesExit = new HashSet<string>(StringComparer.Ordinal)
            {
                board.ExitNodeId
            };
            var reverse = new Queue<string>();
            reverse.Enqueue(board.ExitNodeId);
            while (reverse.Count > 0)
            {
                var destination = reverse.Dequeue();
                foreach (var predecessor in board.Nodes.Where(value =>
                    (value.NextNodeIds ?? Array.Empty<string>()).Contains(destination)))
                    if (reachesExit.Add(predecessor.NodeId))
                        reverse.Enqueue(predecessor.NodeId);
            }
            if (reachesExit.Count != byId.Count)
            {
                reason = "BOARD_ROUTE_CANNOT_RETURN";
                return false;
            }
            return true;
        }

        static bool HasCycle084(
            string nodeId,
            IReadOnlyDictionary<string, WorldGateNodeRule023> nodes,
            HashSet<string> visiting,
            HashSet<string> visited)
        {
            if (visiting.Contains(nodeId)) return true;
            if (!visited.Add(nodeId)) return false;
            visiting.Add(nodeId);
            foreach (var next in nodes[nodeId].NextNodeIds ?? Array.Empty<string>())
                if (HasCycle084(next, nodes, visiting, visited)) return true;
            visiting.Remove(nodeId);
            return false;
        }

        public static bool IsKnownNodeKind084(string kind) =>
            NodeKinds084.Contains((kind ?? string.Empty).ToUpperInvariant());

        public static string RoomKind084(string authoredKind)
        {
            switch ((authoredKind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return "START";
                case "ROUTE": return "TRAIL";
                case "EVENT": return "FATE";
                case "CHECK": return "CHALLENGE";
                case "CAMP": return "CAMPFIRE";
                case "DIPLOMACY": return "STORY";
                case "RESOURCE": return "TREASURE";
                case "OBJECTIVE": return "OBJECTIVE";
                case "BATTLE": return "MONSTER";
                case "DEFENSE_PREP": return "PREPARATION";
                case "EXIT": return "RETURN";
                default: return string.Empty;
            }
        }

        public static string RoomTitle084(string authoredKind)
        {
            switch (RoomKind084(authoredKind))
            {
                case "START": return "PLACE THE GUILD PAWN";
                case "TRAIL": return "HIDDEN ROUTE";
                case "FATE": return "FATE TILE";
                case "CHALLENGE": return "DICE CHALLENGE";
                case "CAMPFIRE": return "CAMPFIRE";
                case "STORY": return "STORY DECISION";
                case "TREASURE": return "TREASURE & SUPPLIES";
                case "OBJECTIVE": return "QUEST OBJECTIVE";
                case "MONSTER": return "MONSTER BATTLE";
                case "PREPARATION": return "DEFENSE PREPARATION";
                case "RETURN": return "RETURN TO THE GUILD";
                default: return "UNSUPPORTED TILE";
            }
        }

        public static string RewardPreview084(
            WorldGateNodeRule023 node,
            int rewardPermille = 10000)
        {
            if (node == null || !IsKnownNodeKind084(node.Kind)) return string.Empty;
            var parts = new List<string>();
            if (node.RequiresCertifiedBattle)
                parts.Add("WIN THE EXISTING UNION BATTLE");
            if (node.CheckDifficulty > 0)
                parts.Add("ROLL 2D6 • TARGET " + node.CheckDifficulty);
            var guildXp = ScaledReward084(node.GuildXp, rewardPermille);
            var hallXp = ScaledReward084(node.HallXp, rewardPermille);
            var materialCount = ScaledMaterialCount084(
                node.MaterialIds?.Count ?? 0,
                rewardPermille);
            if (guildXp > 0) parts.Add("+" + guildXp + " GUILD XP");
            if (hallXp > 0) parts.Add("+" + hallXp + " HALL XP");
            if (materialCount > 0)
                parts.Add("MATERIAL REWARD ×" + materialCount);
            if (rewardPermille <= 0 &&
                (node.GuildXp > 0 || node.HallXp > 0 ||
                 (node.MaterialIds?.Count ?? 0) > 0))
                parts.Add("REPEAT REWARDS DEPLETED THIS RUN");
            AppendDelta084(parts, node.SupplyDelta, "SUPPLY");
            AppendDelta084(parts, -node.FatigueDelta, "FATIGUE RELIEF", "FATIGUE");
            AppendDelta084(parts, node.UrgencyDelta, "URGENCY");
            AppendDelta084(parts, -node.ThreatDelta, "THREAT RELIEF", "THREAT");
            AppendDelta084(parts, node.TrustDelta, "TRUST");
            AppendDelta084(parts, node.TensionDelta, "TENSION");
            AppendDelta084(parts, node.CivilianSupportDelta, "CIVILIAN SUPPORT");
            return parts.Count == 0
                ? "STORY PROGRESS SAVED • NO REROLL"
                : string.Join("  •  ", parts);
        }

        public static int RewardPermille084(
            WorldGateOperationState023 operation,
            WorldGateRewardPolicy023 policy,
            WorldGateRuntimeState023 runtime)
        {
            if (operation == null ||
                !StringComparer.Ordinal.Equals(operation.OperationKind, "REPEATABLE"))
                return 10000;
            var basis = policy?.RepeatableConsecutiveRewardBasisPoints;
            if (basis == null || basis.Count == 0) return 10000;
            var streak = runtime != null &&
                         StringComparer.Ordinal.Equals(
                             runtime.RepeatableStreakDefinitionId,
                             operation.DefinitionId)
                ? runtime.RepeatableStreakCount
                : 0;
            return Math.Max(0, basis[Math.Min(streak, basis.Count - 1)]);
        }

        /// <summary>
        /// Returns the exact reward multiplier that authority will freeze when a
        /// not-yet-started board begins. Presentation uses the same rule so a quest
        /// card never advertises unscaled repeat rewards.
        /// </summary>
        public static int RewardPermilleForNextRun084(
            string operationKind,
            string definitionId,
            WorldGateRewardPolicy023 policy,
            WorldGateRuntimeState023 runtime)
        {
            if (!StringComparer.Ordinal.Equals(operationKind, "REPEATABLE"))
                return 10000;
            var basis = policy?.RepeatableConsecutiveRewardBasisPoints;
            if (basis == null || basis.Count == 0) return 10000;
            var streak = runtime != null && StringComparer.Ordinal.Equals(
                runtime.RepeatableStreakDefinitionId, definitionId)
                ? runtime.RepeatableStreakCount
                : 0;
            return Math.Max(0, basis[Math.Min(streak, basis.Count - 1)]);
        }

        public static int ScaledReward084(int value, int rewardPermille) =>
            Math.Max(0, (value * Math.Max(0, rewardPermille)) / 10000);

        public static int ScaledMaterialCount084(int count, int rewardPermille)
        {
            if (count <= 0 || rewardPermille <= 0) return 0;
            return Math.Min(
                count,
                Math.Max(1, (count * rewardPermille) / 10000));
        }

        public static string ReceiptReward084(WorldGateNodeReceipt023 receipt)
        {
            if (receipt == null) return string.Empty;
            var parts = new List<string>();
            if (receipt.GuildXp > 0) parts.Add("+" + receipt.GuildXp + " GUILD XP");
            if (receipt.HallXp > 0) parts.Add("+" + receipt.HallXp + " HALL XP");
            if ((receipt.MaterialIds?.Count ?? 0) > 0)
                parts.Add("MATERIAL REWARD ×" + receipt.MaterialIds.Count);
            AppendDelta084(parts, receipt.SupplyDelta, "SUPPLY");
            AppendDelta084(parts, -receipt.FatigueDelta, "FATIGUE RELIEF", "FATIGUE");
            AppendDelta084(parts, receipt.UrgencyDelta, "URGENCY");
            AppendDelta084(parts, -receipt.ThreatDelta, "THREAT RELIEF", "THREAT");
            AppendDelta084(parts, receipt.TrustDelta, "TRUST");
            AppendDelta084(parts, receipt.TensionDelta, "TENSION");
            AppendDelta084(parts, receipt.CivilianSupportDelta, "CIVILIAN SUPPORT");
            return parts.Count == 0
                ? "STORY PROGRESS SAVED"
                : string.Join("  •  ", parts);
        }

        public static string OutcomeTitle084(string outcome)
        {
            switch ((outcome ?? string.Empty).ToUpperInvariant())
            {
                case "EXCEPTIONAL": return "PERFECT ROLL";
                case "FULL_SUCCESS": return "CLEAN SUCCESS";
                case "SUCCESS": return "SUCCESS";
                case "VICTORY": return "VICTORY — MONSTER ROOM CLEARED";
                case "DEFEAT": return "DEFEAT — RECOVERY ROUTE OPEN";
                case "SUCCESS_WITH_COST": return "SUCCESS — WITH A COST";
                case "SETBACK": return "SETBACK — THE QUEST CONTINUES";
                case "SEVERE_SETBACK": return "HARD SETBACK — RECOVERY ROUTE OPEN";
                default: return "RESULT COMMITTED";
            }
        }

        public static string ChoiceLabel084(string choiceId)
        {
            if (string.IsNullOrWhiteSpace(choiceId)) return "MOVE FORWARD";
            var words = choiceId.Replace('_', ' ').Trim().ToLowerInvariant();
            return string.Join(" ", words.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(word => char.ToUpperInvariant(word[0]) + word.Substring(1)));
        }

        public static IReadOnlyList<string> OrderedChoices084(
            string operationId,
            string nodeId,
            IReadOnlyList<string> choices)
        {
            var result = new List<string>();
            if (choices != null)
                for (var index = 0; index < choices.Count; index++)
                    if (!string.IsNullOrWhiteSpace(choices[index]) &&
                        !result.Contains(choices[index]))
                        result.Add(choices[index]);
            result.Sort((left, right) =>
            {
                var comparison = StringComparer.Ordinal.Compare(
                    RouteOrderKey084(operationId, nodeId, left),
                    RouteOrderKey084(operationId, nodeId, right));
                return comparison != 0
                    ? comparison
                    : StringComparer.Ordinal.Compare(left, right);
            });
            return result.AsReadOnly();
        }

        /// <summary>
        /// Selects the first stable authored route for the one-button board loop.
        /// Certified battle rooms deliberately have no automatic choice: their
        /// authored encounter must be completed before board movement resumes.
        /// </summary>
        public static string AutomaticChoice084(
            string operationId,
            WorldGateNodeRule023 node)
        {
            if (node == null || node.RequiresCertifiedBattle)
                return string.Empty;
            var ordered = OrderedChoices084(operationId, node.NodeId,
                node.ChoiceIds ?? Array.Empty<string>());
            var choice = ordered.Count > 0 ? ordered[0] : "CONTINUE";
            return TryNextNode084(node, choice, out _) ? choice : string.Empty;
        }

        /// <summary>
        /// Closed-world authored choice routing. Campaign CAMP tiles deliberately
        /// expose three verbs over two paths: SCOUT discovers the optional resource
        /// room; REST and MENTOR continue to diplomacy. All other unequal arities
        /// in the certified catalog converge on their one authored destination.
        /// </summary>
        public static bool TryNextNode084(
            WorldGateNodeRule023 node,
            string choiceId,
            out string nextNodeId)
        {
            nextNodeId = string.Empty;
            if (node == null || !IsKnownNodeKind084(node.Kind)) return false;
            var next = node.NextNodeIds ?? Array.Empty<string>();
            var choices = node.ChoiceIds ?? Array.Empty<string>();
            if (StringComparer.Ordinal.Equals(node.Kind, "EXIT"))
                return next.Count == 0;
            if (next.Count == 0) return false;
            if (next.Count == 1)
            {
                nextNodeId = next[0];
                return true;
            }
            if (choices.Count == next.Count)
            {
                for (var index = 0; index < choices.Count; index++)
                    if (StringComparer.Ordinal.Equals(choices[index], choiceId))
                    {
                        nextNodeId = next[index];
                        return true;
                    }
                return false;
            }
            if (StringComparer.Ordinal.Equals(node.Kind, "CAMP") &&
                next.Count == 2 && choices.Count == 3 &&
                choices.Contains("REST") && choices.Contains("SCOUT") &&
                choices.Contains("MENTOR"))
            {
                nextNodeId = StringComparer.Ordinal.Equals(choiceId, "SCOUT")
                    ? next[1]
                    : StringComparer.Ordinal.Equals(choiceId, "REST") ||
                      StringComparer.Ordinal.Equals(choiceId, "MENTOR")
                        ? next[0]
                        : string.Empty;
                return !string.IsNullOrWhiteSpace(nextNodeId);
            }
            return false;
        }

        public static string RouteOrderKey084(
            string operationId,
            string nodeId,
            string choiceId) => CanonicalJson.Sha256Hex(new
        {
            Rule = "BOARD_ADVENTURE_ROUTE_ORDER_084",
            Operation = operationId ?? string.Empty,
            Node = nodeId ?? string.Empty,
            Choice = choiceId ?? string.Empty
        });

        public static string PlayerCopy084(string value, params string[] authorityIds)
        {
            var result = value ?? string.Empty;
            if (authorityIds != null)
                for (var index = 0; index < authorityIds.Length; index++)
                    if (!string.IsNullOrWhiteSpace(authorityIds[index]))
                        result = result.Replace(authorityIds[index], "the current quest");
            result = AuthorityToken084.Replace(result, "the current quest");
            while (result.Contains("the current quest the current quest"))
                result = result.Replace("the current quest the current quest", "the current quest");
            return result.Trim();
        }

        /// <summary>
        /// Returns true only when player copy contains a complete opaque authority
        /// identifier. Plain word choices such as ASK, HELP, or BOLD are deliberately
        /// excluded: once projected as player language they are indistinguishable from
        /// ordinary words (for example CURRENT TASK or FLIP BOLD ROUTE).
        /// </summary>
        public static bool ContainsRawOpaqueAuthorityToken084(
            string playerCopy,
            string authorityToken)
        {
            if (string.IsNullOrWhiteSpace(playerCopy) ||
                string.IsNullOrWhiteSpace(authorityToken) ||
                !authorityToken.Any(value =>
                    value == '_' || value == '-' || char.IsDigit(value)))
                return false;

            var searchFrom = 0;
            while (searchFrom <= playerCopy.Length - authorityToken.Length)
            {
                var index = playerCopy.IndexOf(
                    authorityToken,
                    searchFrom,
                    StringComparison.OrdinalIgnoreCase);
                if (index < 0) return false;

                var end = index + authorityToken.Length;
                var startsAtBoundary = index == 0 ||
                    !IsAuthorityIdentifierCharacter084(playerCopy[index - 1]);
                var endsAtBoundary = end == playerCopy.Length ||
                    !IsAuthorityIdentifierCharacter084(playerCopy[end]);
                if (startsAtBoundary && endsAtBoundary) return true;
                searchFrom = index + 1;
            }

            return false;
        }

        private static bool IsAuthorityIdentifierCharacter084(char value) =>
            char.IsLetterOrDigit(value) || value == '_' || value == '-';

        public static int TrackPhase084(int completedNodes, int totalNodes, int phaseCount = 6)
        {
            if (phaseCount < 2) throw new ArgumentOutOfRangeException(nameof(phaseCount));
            if (totalNodes <= 1) return 0;
            var completed = Math.Max(0, Math.Min(totalNodes - 1, completedNodes));
            return Math.Max(0, Math.Min(phaseCount - 1,
                (completed * (phaseCount - 1)) / (totalNodes - 1)));
        }

        public static int SemanticTrackPhase084(string roomKind, string operationStatus)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(operationStatus, "ReadyToFinalize") ||
                StringComparer.Ordinal.Equals(roomKind, "RETURN")) return 5;
            switch ((roomKind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return 0;
                case "TRAIL":
                case "PREPARATION": return 1;
                case "FATE":
                case "CHALLENGE":
                case "CAMPFIRE":
                case "TREASURE": return 2;
                case "STORY":
                case "MONSTER": return 3;
                case "OBJECTIVE": return 4;
                default: return 0;
            }
        }

        public static int MonotonicSemanticTrackPhase084(
            string currentRoomKind,
            string operationStatus,
            IEnumerable<string> completedRoomKinds)
        {
            var phase = SemanticTrackPhase084(currentRoomKind, operationStatus);
            if (completedRoomKinds == null) return phase;
            foreach (var kind in completedRoomKinds)
                phase = Math.Max(phase, SemanticTrackPhase084(kind, "Active"));
            return Math.Max(0, Math.Min(5, phase));
        }

        private static void AppendDelta084(
            ICollection<string> parts,
            int value,
            string positiveLabel,
            string negativeLabel = null)
        {
            if (value == 0) return;
            if (value > 0)
                parts.Add("+" + value + " " + positiveLabel);
            else
                parts.Add(value + " " + (negativeLabel ?? positiveLabel));
        }
    }
}
