using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign023;

namespace SecondDimension.Presentation.Campaign023
{
    /// <summary>
    /// Presentation-only graph projection. Gameplay remains authoritative; this
    /// maps every authored node to a numbered room-space without revealing future
    /// story copy. Every legal edge in the certified board pack advances one space.
    /// </summary>
    public static class AdventureBoardTrackProjection084
    {
        public const string ClearedState = "CLEARED";
        public const string CurrentState = "CURRENT";
        public const string FaceDownState = "FACE_DOWN";
        public const string ClosedState = "CLOSED";

        public static IReadOnlyList<AdventureTrackTileView023> Project(
            BoardDto023 board,
            string currentNodeId,
            IReadOnlyList<string> completedNodeIds,
            string committedNextNodeId = null,
            IReadOnlyDictionary<string, string> revealedLabels = null)
        {
            var nodes = board?.nodes ?? Array.Empty<NodeDto023>();
            if (nodes.Length == 0) return Array.Empty<AdventureTrackTileView023>();

            var byId = nodes
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.nodeId))
                .ToDictionary(value => value.nodeId, StringComparer.Ordinal);
            if (byId.Count == 0) return Array.Empty<AdventureTrackTileView023>();
            var start = !string.IsNullOrWhiteSpace(board.startNodeId) &&
                        byId.ContainsKey(board.startNodeId)
                ? board.startNodeId
                : nodes.First(value => value != null &&
                    !string.IsNullOrWhiteSpace(value.nodeId)).nodeId;
            var depth = BuildDepths084(start, byId);
            var nextFallbackDepth = depth.Count == 0 ? 0 : depth.Values.Max() + 1;
            foreach (var node in nodes)
                if (node != null && !string.IsNullOrWhiteSpace(node.nodeId) &&
                    !depth.ContainsKey(node.nodeId))
                    depth[node.nodeId] = nextFallbackDepth++;

            var completed = new HashSet<string>(
                completedNodeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var reachabilityRoot = !string.IsNullOrWhiteSpace(committedNextNodeId) &&
                                   byId.ContainsKey(committedNextNodeId)
                ? committedNextNodeId
                : currentNodeId;
            var reachable = ReachableFrom084(reachabilityRoot, byId);
            var authoredOrder = nodes
                .Select((node, index) => new {node, index})
                .Where(value => value.node != null &&
                    !string.IsNullOrWhiteSpace(value.node.nodeId))
                .ToDictionary(value => value.node.nodeId, value => value.index,
                    StringComparer.Ordinal);
            var branchCounts = nodes
                .Where(value => value != null &&
                    !string.IsNullOrWhiteSpace(value.nodeId))
                .GroupBy(value => depth[value.nodeId])
                .ToDictionary(value => value.Key, value => value.Count());
            var branchIndexes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var group in nodes
                         .Where(value => value != null &&
                             !string.IsNullOrWhiteSpace(value.nodeId))
                         .GroupBy(value => depth[value.nodeId]))
            {
                var index = 0;
                foreach (var node in group.OrderBy(value => authoredOrder[value.nodeId]))
                    branchIndexes[node.nodeId] = index++;
            }

            var result = new List<AdventureTrackTileView023>();
            foreach (var node in nodes.Where(value => value != null &&
                         !string.IsNullOrWhiteSpace(value.nodeId)))
            {
                var isCurrent = StringComparer.Ordinal.Equals(
                    node.nodeId, currentNodeId);
                var state = isCurrent
                    ? CurrentState
                    : completed.Contains(node.nodeId)
                        ? ClearedState
                        : reachable.Contains(node.nodeId)
                            ? FaceDownState
                            : ClosedState;
                result.Add(new AdventureTrackTileView023
                {
                    NodeId = node.nodeId,
                    AuthoredOrder = authoredOrder[node.nodeId],
                    SpaceNumber = depth[node.nodeId] + 1,
                    BranchIndex = branchIndexes[node.nodeId],
                    BranchCount = branchCounts[depth[node.nodeId]],
                    State = state,
                    RevealedLabel = state == CurrentState || state == ClearedState
                        ? RevealedLabel084(board, node, revealedLabels)
                        : string.Empty
                });
            }
            return result
                .OrderBy(value => value.SpaceNumber)
                .ThenBy(value => value.AuthoredOrder)
                .ToArray();
        }

        static string RevealedLabel084(
            BoardDto023 board,
            NodeDto023 node,
            IReadOnlyDictionary<string, string> revealedLabels)
        {
            if (revealedLabels != null &&
                revealedLabels.TryGetValue(node.nodeId, out var projected) &&
                !string.IsNullOrWhiteSpace(projected))
                return BoardAdventureRules084.PlayerCopy084(
                    projected, node.nodeId, node.sourceId,
                    board.definitionId, board.boardId);
            return BoardAdventureRules084.PlayerCopy084(
                node.title, node.nodeId, node.sourceId,
                board.definitionId, board.boardId);
        }

        static Dictionary<string, int> BuildDepths084(
            string start,
            IReadOnlyDictionary<string, NodeDto023> byId)
        {
            var depths = new Dictionary<string, int>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(start) || !byId.ContainsKey(start))
                return depths;
            var queue = new Queue<string>();
            depths[start] = 0;
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                var nextDepth = depths[nodeId] + 1;
                foreach (var next in byId[nodeId].nextNodeIds ?? Array.Empty<string>())
                {
                    if (!byId.ContainsKey(next)) continue;
                    if (depths.TryGetValue(next, out var existing) &&
                        existing <= nextDepth) continue;
                    depths[next] = nextDepth;
                    queue.Enqueue(next);
                }
            }
            return depths;
        }

        static HashSet<string> ReachableFrom084(
            string start,
            IReadOnlyDictionary<string, NodeDto023> byId)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(start) || !byId.ContainsKey(start))
                return result;
            var queue = new Queue<string>();
            result.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                foreach (var next in byId[nodeId].nextNodeIds ?? Array.Empty<string>())
                    if (byId.ContainsKey(next) && result.Add(next))
                        queue.Enqueue(next);
            }
            return result;
        }
    }
}
