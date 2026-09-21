using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;

namespace SecondDimension.Presentation.BoardTower001
{
    public sealed class TowerRevealedRoomView001
    {
        public string RoomId;
        public int StepNumber;
        public string Title;
        public string Flavor;
    }

    /// <summary>
    /// Presentation-only projection for the most recently applied Tower step.
    /// The P0 Tower records contain useful room identities, but their revealText
    /// promises modifiers and boons whose numeric contracts are not implemented.
    /// This adapter therefore exposes only the committed identity and curated
    /// narrative atmosphere. It never changes or supplements the saved outcome.
    /// </summary>
    public static class TowerRevealedRoomProjection001
    {
        public static TowerRevealedRoomView001 Project(
            BoardTowerEnhancementCatalog001 catalog,
            string committedRunId,
            int floorNumber,
            IReadOnlyList<string> completedStepIds)
        {
            if (catalog == null || string.IsNullOrWhiteSpace(committedRunId) ||
                completedStepIds == null || completedStepIds.Count == 0 ||
                completedStepIds.Any(string.IsNullOrWhiteSpace))
                return null;

            var candidates = BoardTowerEnhancementRules001
                .P0RoomIdsForBoardKind001("TOWER")
                .Select(roomId => catalog.Rooms.TryGetValue(roomId, out var room)
                    ? room
                    : null)
                .Where(room => room != null &&
                               room.minTowerFloor <= Math.Max(1, floorNumber) &&
                               (room.allowedModes ?? string.Empty).IndexOf(
                                   "Endless Tower", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            if (candidates.Length == 0) return null;

            var anchor = catalog.DeterministicTowerRoom001(
                committedRunId, Math.Max(1, floorNumber));
            var anchorIndex = Array.FindIndex(candidates, value =>
                StringComparer.Ordinal.Equals(value.roomId, anchor?.roomId));
            if (anchorIndex < 0) anchorIndex = 0;

            // Adjacent saved steps rotate through the verified P0 room identities.
            // The run/floor anchor keeps reloads deterministic while the completed
            // proof count ensures unrevealed steps cannot influence the projection.
            var revealedIndex = completedStepIds.Count - 1;
            var selected = candidates[(anchorIndex + revealedIndex) % candidates.Length];
            var title = SafeNarrativeTitle001(selected.roomId);
            var flavor = SafeNarrativeFlavor001(selected.roomId);
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(flavor))
                return null;

            return new TowerRevealedRoomView001
            {
                RoomId = selected.roomId,
                StepNumber = revealedIndex + 1,
                Title = title,
                Flavor = flavor
            };
        }

        static string SafeNarrativeTitle001(string roomId)
        {
            if (StringComparer.Ordinal.Equals(roomId, "BTR001_TOWER_01"))
                return "Shifting Floor";
            if (StringComparer.Ordinal.Equals(roomId, "BTR001_TOWER_02"))
                return "Sealed Landing";
            return string.Empty;
        }

        static string SafeNarrativeFlavor001(string roomId)
        {
            if (StringComparer.Ordinal.Equals(roomId, "BTR001_TOWER_01"))
                return "Stone plates turn and settle as the Guild marks a newly revealed path.";
            if (StringComparer.Ordinal.Equals(roomId, "BTR001_TOWER_02"))
                return "Sealed emblems overlook the landing as the Guild records the newly revealed route.";
            return string.Empty;
        }
    }
}
