using System;
using System.Collections.Generic;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.GuildCity017D
{
    /// <summary>
    /// Save-authoritative adapter for the initial BOARD_TOWER_ENHANCEMENT_PACK_001
    /// room slice. The source catalog is design data; only the stable room identity
    /// is committed here. Unspecified prose effects never become hidden mechanics.
    /// </summary>
    public static class BoardTowerEnhancementRules001
    {
        public const string PackageId001 = "BOARD_TOWER_ENHANCEMENT_PACK_001";
        public const string SelectedRoomFlagPrefix001 = "BTR001_ROOM_COMMITTED_";

        private static readonly IReadOnlyDictionary<string, string[]> P0RoomIdsByBoardKind001 =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "MONSTER", new[] { "BTR001_BATTLE_01", "BTR001_BATTLE_02" } },
                { "TREASURE", new[] { "BTR001_CHEST_01", "BTR001_CHEST_02" } },
                { "SKILL", new[] { "BTR001_BOON_01", "BTR001_BOON_02" } },
                { "BLESSING", new[] { "BTR001_BOON_01", "BTR001_BOON_02" } },
                { "FATE", new[] { "BTR001_HAZARD_01", "BTR001_HAZARD_02" } },
                { "CAMPFIRE", new[] { "BTR001_CAMP_01", "BTR001_CAMP_02" } },
                { "STORY", new[] { "BTR001_STORY_01", "BTR001_STORY_02" } },
                { "DISCOVERY", new[] { "BTR001_RESOURCE_01", "BTR001_RESOURCE_02" } },
                { "TOWER", new[] { "BTR001_TOWER_01", "BTR001_TOWER_02" } }
            };

        public static string SelectP0RoomModuleId001(
            string expeditionId,
            string nodeId,
            string boardRoomKind)
        {
            var kind = (boardRoomKind ?? string.Empty).Trim().ToUpperInvariant();
            if (!P0RoomIdsByBoardKind001.TryGetValue(kind, out var candidates) ||
                candidates == null || candidates.Length == 0)
                return string.Empty;

            var hash = CanonicalJson.Sha256Hex(new
            {
                Rule = "BOARD_TOWER_P0_ROOM_001",
                Expedition = expeditionId ?? string.Empty,
                Node = nodeId ?? string.Empty,
                Kind = kind
            });
            var index = Convert.ToUInt32(hash.Substring(0, 8), 16);
            return candidates[(int)(index % candidates.Length)];
        }

        public static string SelectedRoomFlag001(string nodeId, string roomModuleId)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(roomModuleId))
                return string.Empty;
            var nodeProof = CanonicalJson.Sha256Hex(new
            {
                Rule = "BOARD_TOWER_P0_NODE_PROOF_001",
                Node = nodeId
            }).Substring(0, 16).ToUpperInvariant();
            return SelectedRoomFlagPrefix001 + nodeProof + "_" + roomModuleId;
        }

        public static bool IsRoomModuleCommitted001(
            IReadOnlyList<string> objectiveFlags,
            string nodeId,
            string roomModuleId)
        {
            var expected = SelectedRoomFlag001(nodeId, roomModuleId);
            if (string.IsNullOrWhiteSpace(expected)) return false;
            var flags = objectiveFlags ?? Array.Empty<string>();
            for (var index = 0; index < flags.Count; index++)
                if (StringComparer.Ordinal.Equals(flags[index], expected)) return true;
            return false;
        }

        public static IReadOnlyList<string> P0RoomIdsForBoardKind001(string boardRoomKind)
        {
            var kind = (boardRoomKind ?? string.Empty).Trim().ToUpperInvariant();
            return P0RoomIdsByBoardKind001.TryGetValue(kind, out var values)
                ? values
                : Array.Empty<string>();
        }
    }
}
