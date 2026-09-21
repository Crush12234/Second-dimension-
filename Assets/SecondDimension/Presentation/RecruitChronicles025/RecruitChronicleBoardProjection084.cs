using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Presentation.People029;

namespace SecondDimension.Presentation.RecruitChronicles025
{
    /// <summary>
    /// Read-only, Barcadia-style projection of the persisted Recruit Chronicles 025 graph.
    /// It never resolves authored checks, consumes operation costs, grants rewards, rolls dice,
    /// or advances a quest. Stable IDs remain available only for the existing command service.
    /// </summary>
    public static class RecruitChronicleBoardProjection084
    {
        public const string ClearedState = "CLEARED";
        public const string CurrentState = "CURRENT";
        public const string RevealedState = "REVEALED";
        public const string FaceDownState = "FACE_DOWN";
        public const string SelectableFaceDownState = "SELECTABLE_FACE_DOWN";
        public const string UnchosenState = "UNCHOSEN_ROUTE";
        public const string ReturnedState = "RETURNED";

        public static PersonalQuestView029 Project(
            PersonalQuestBoardDefinition025 board,
            RecruitChronicleProfile025 profile,
            PersonalQuestProgressState025 progress)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (board.nodes == null || board.nodes.Length == 0)
                throw new ArgumentException("A personal Chronicle board requires authored rooms.", nameof(board));

            var started = progress != null;
            var currentNodeId = started && !string.IsNullOrWhiteSpace(progress.CurrentNodeId)
                ? progress.CurrentNodeId
                : board.entryNodeId;
            var current = RecruitChronicleRules025.FindNode(board, currentNodeId) ?? board.nodes[0];
            var currentIndex = Array.FindIndex(board.nodes,
                value => value != null && StringComparer.Ordinal.Equals(value.nodeId, current.nodeId));
            if (currentIndex < 0) currentIndex = 0;

            var completed = new HashSet<string>(
                progress?.CompletedNodeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var next = new HashSet<string>(current.nextNodeIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var tiles = new List<PersonalQuestTileView029>(board.nodes.Length);
            for (var index = 0; index < board.nodes.Length; index++)
            {
                var node = board.nodes[index];
                if (node == null) continue;
                var room = RoomLabel(node.kind, index);
                var isCurrent = started && StringComparer.Ordinal.Equals(node.nodeId, current.nodeId);
                var isCleared = completed.Contains(node.nodeId);
                var isReturned = isCurrent && progress.Completed;
                var isRevealed = !started
                    ? index == 0
                    : false;
                var isSelectable = started && !isCurrent && !isCleared && next.Contains(node.nodeId);
                var isUnchosen = started && !isCurrent && !isCleared && !isSelectable && index < currentIndex;
                var state = isReturned ? ReturnedState :
                    isCurrent ? CurrentState :
                    isCleared ? ClearedState :
                    isRevealed ? RevealedState :
                    isSelectable ? SelectableFaceDownState :
                    isUnchosen ? UnchosenState : FaceDownState;
                var routeIndex = Array.FindIndex(current.nextNodeIds ?? Array.Empty<string>(),
                    value => StringComparer.Ordinal.Equals(value, node.nodeId));
                var visibleRoom = isSelectable || StringComparer.Ordinal.Equals(state, FaceDownState)
                    ? "Face-down route"
                    : isUnchosen ? "Closed route" : room;
                tiles.Add(new PersonalQuestTileView029
                {
                    NodeId = node.nodeId,
                    Kind = node.kind ?? string.Empty,
                    Sequence = index + 1,
                    RoomLabel = visibleRoom,
                    State = state,
                    DisplayLabel = TileDisplayLabel(index + 1, visibleRoom, state,
                        isSelectable ? RouteLabel(routeIndex, current.nextNodeIds?.Length ?? 0) : string.Empty),
                    IsCleared = isCleared,
                    IsCurrent = isCurrent,
                    IsRevealed = isRevealed,
                    IsFaceDown = StringComparer.Ordinal.Equals(state, FaceDownState) || isSelectable,
                    IsUnchosenRoute = isUnchosen
                });
            }

            var destinations = new List<PersonalQuestDestinationView029>();
            var destinationIds = current.nextNodeIds ?? Array.Empty<string>();
            for (var destinationOrder = 0; destinationOrder < destinationIds.Length; destinationOrder++)
            {
                var destinationId = destinationIds[destinationOrder];
                var destination = RecruitChronicleRules025.FindNode(board, destinationId);
                if (destination == null) continue;
                var index = Array.FindIndex(board.nodes,
                    value => value != null && StringComparer.Ordinal.Equals(value.nodeId, destination.nodeId));
                var routeLabel = RouteLabel(destinationOrder, destinationIds.Length);
                destinations.Add(new PersonalQuestDestinationView029
                {
                    DestinationNodeId = destination.nodeId,
                    Kind = destination.kind ?? string.Empty,
                    Sequence = index + 1,
                    RoomLabel = "Face-down route",
                    ButtonLabel = destinationIds.Length == 1 ? "MOVE & FLIP THE NEXT ROOM" : "CHOOSE " + routeLabel,
                    Description = "The room stays hidden until this saved move is committed."
                });
            }

            var boardIds = profile.personalQuestBoardIds ?? Array.Empty<string>();
            var boardIndex = Array.FindIndex(boardIds,
                value => StringComparer.Ordinal.Equals(value, board.boardId));
            var completedRooms = completed.Count + (progress?.Completed == true ? 1 : 0);
            return new PersonalQuestView029
            {
                BoardId = board.boardId ?? string.Empty,
                RecruitId = profile.recruitId ?? string.Empty,
                CurrentNodeId = current.nodeId ?? board.entryNodeId ?? string.Empty,
                NextNodeIds = Array.AsReadOnly((current.nextNodeIds ?? Array.Empty<string>()).ToArray()),
                DisplayName = board.displayName ?? "Personal Chronicle",
                RecruitDisplayName = profile.displayName ?? "Guild member",
                ChronicleTitle = profile.chronicleTitle ?? board.displayName ?? "Personal Chronicle",
                ChapterLabel = ChapterLabel(boardIndex),
                WorldName = FriendlyWorldName(profile.worldId),
                Theme = board.theme ?? string.Empty,
                StoryContext = StoryContext(profile, board),
                Objective = "Reach the Personal Objective room, then return safely to the Guild.",
                ProgressLabel = progress?.Completed == true
                    ? "Returned to the Guild • " + completedRooms + " rooms reached • Chronicle complete"
                    : started
                        ? Math.Min(board.nodes.Length, completedRooms + 1) + " of " + board.nodes.Length + " rooms reached"
                        : "Board ready • first room waiting",
                CurrentNodeTitle = RoomLabel(current.kind, currentIndex),
                CurrentNodeKind = current.kind ?? string.Empty,
                CurrentRoomLabel = RoomLabel(current.kind, currentIndex),
                CurrentNodeDescription = CurrentRoomStory(profile, board, current.kind),
                CurrentInstruction = CurrentInstruction(current.kind),
                Tiles = tiles.AsReadOnly(),
                Destinations = destinations.AsReadOnly(),
                Started = started,
                Complete = progress?.Completed ?? false,
                RequiresCertifiedBattle = StringComparer.Ordinal.Equals(current.kind, "BATTLE"),
                CompletedRooms = completedRooms
            };
        }

        public static string FriendlyWorldName(string worldId)
        {
            if (string.IsNullOrWhiteSpace(worldId)) return "Unknown road";
            var value = worldId.StartsWith("WORLD_", StringComparison.Ordinal)
                ? worldId.Substring("WORLD_".Length)
                : worldId;
            var pieces = value.Split(new[] {'_'}, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (pieces.Count > 0 && pieces[pieces.Count - 1].All(char.IsDigit))
                pieces.RemoveAt(pieces.Count - 1);
            if (pieces.Count == 0) return "Unknown road";
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                string.Join(" ", pieces).ToLowerInvariant());
        }

        public static string FriendlyTag(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                value.Replace('_', ' ').ToLowerInvariant());
        }

        static string StoryContext(RecruitChronicleProfile025 profile, PersonalQuestBoardDefinition025 board)
        {
            var conflict = !string.IsNullOrWhiteSpace(board.theme) ? board.theme : profile.coreWound;
            var fear = profile.fear ?? string.Empty;
            if (string.IsNullOrWhiteSpace(fear)) return conflict ?? string.Empty;
            if (string.IsNullOrWhiteSpace(conflict)) return "What is at stake: " + fear;
            return conflict + "\nWHAT IS AT STAKE • " + fear;
        }

        static string ChapterLabel(int boardIndex)
        {
            if (boardIndex == 0) return "FIRST CHRONICLE";
            if (boardIndex == 1) return "SECOND CHRONICLE";
            return "PERSONAL CHRONICLE " + Math.Max(1, boardIndex + 1);
        }

        static string RoomLabel(string kind, int index)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return "Guild Hall Door";
                case "EVENT": return index <= 1 ? "Story Encounter" : "Turning Point";
                case "CHECK": return index <= 2 ? "First Crossroads" : "Final Test";
                case "CAMP": return "Campfire";
                case "CHOICE": return "Fork in the Road";
                case "BATTLE": return "Danger Room";
                case "OBJECTIVE": return "Personal Objective";
                case "RETURN": return "Homecoming";
                default: return "Unknown Room";
            }
        }

        static string RouteLabel(int routeIndex, int routeCount)
        {
            if (routeCount <= 1) return "NEXT ROOM";
            if (routeCount == 2) return routeIndex <= 0 ? "LEFT PATH" : "RIGHT PATH";
            return "DOOR " + (char)('A' + Math.Max(0, Math.Min(25, routeIndex)));
        }

        static string CurrentRoomStory(RecruitChronicleProfile025 profile, PersonalQuestBoardDefinition025 board, string kind)
        {
            var name = string.IsNullOrWhiteSpace(profile.displayName) ? "Your Guild member" : profile.displayName;
            var theme = string.IsNullOrWhiteSpace(board.theme) ? profile.coreWound : board.theme;
            var stake = string.IsNullOrWhiteSpace(profile.fear) ? theme : profile.fear;
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return name + " asks the Guild to stand beside them. " + theme;
                case "EVENT": return name + " finds the journey turning personal. " + theme;
                case "CHECK": return "The road presses on what " + name + " fears most: " + stake;
                case "CAMP": return name + " shares what this journey means before the party moves on. " + theme;
                case "CHOICE": return name + " cannot take every road. The party must choose which hidden path to trust.";
                case "BATTLE": return name + " has reached the danger they could not face alone. Win together to continue.";
                case "OBJECTIVE": return name + " is finally face to face with the heart of this Chronicle. " + theme;
                case "RETURN": return name + " returns to the Guild with this chapter behind them.";
                default: return name + " continues their personal Chronicle with the Guild beside them.";
            }
        }

        static string TileDisplayLabel(int sequence, string room, string state, string routeLabel)
        {
            switch (state)
            {
                case ClearedState: return "✓ " + room.ToUpperInvariant() + "\nCLEARED";
                case CurrentState: return "● YOUR PAWN\n" + room.ToUpperInvariant();
                case RevealedState: return "◇ NEXT ROOM\n" + room.ToUpperInvariant();
                case SelectableFaceDownState: return "? " + routeLabel + "\nFACE DOWN";
                case UnchosenState: return "× PATH CLOSED\nROOM " + sequence;
                case ReturnedState: return "★ RETURNED\n" + room.ToUpperInvariant();
                default: return "? FACE DOWN\nROOM " + sequence;
            }
        }

        static string CurrentInstruction(string kind)
        {
            switch ((kind ?? string.Empty).ToUpperInvariant())
            {
                case "START": return "Turn over the first story card and take the road together.";
                case "EVENT": return "Read this revealed story beat, then turn over the next room.";
                case "CHECK": return "Choose a face-down route. The destination is revealed only after the saved move.";
                case "CAMP": return "Share the quiet moment, then continue when you are ready.";
                case "CHOICE": return "Choose a face-down path. The board flips that room after your move is saved.";
                case "BATTLE": return "Enter the certified Union battle. Victory opens the next room; defeat leaves this Chronicle recoverable.";
                case "OBJECTIVE": return "Resolve the personal objective, then turn over the homecoming tile.";
                case "RETURN": return "The Chronicle is complete. Your party has returned to the Guild.";
                default: return "Continue along an authored route.";
            }
        }
    }
}
