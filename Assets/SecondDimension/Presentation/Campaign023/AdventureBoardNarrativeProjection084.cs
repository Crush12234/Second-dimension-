using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Presentation.Campaign019;

namespace SecondDimension.Presentation.Campaign023
{
    /// <summary>
    /// Player-copy projection for the compact World Gate board. The authored board
    /// remains the sole gameplay authority; this class only gives each revealed
    /// room a concise mission-specific title, objective, and story beat.
    /// </summary>
    public static class AdventureBoardNarrativeProjection084
    {
        public sealed class RoomCopy084
        {
            public string Title = string.Empty;
            public string Description = string.Empty;
            public string StoryFlavor = string.Empty;
            public string Objective = string.Empty;
        }

        public static RoomCopy084 Project(
            BoardDto023 board,
            NodeDto023 node,
            ChapterNarrative019 chapter,
            string worldDisplayName)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (node == null) throw new ArgumentNullException(nameof(node));

            var ids = new[]
            {
                node.nodeId, node.sourceId, board.definitionId, board.boardId
            };
            var world = FriendlyWorld084(worldDisplayName, board.worldId);
            var boardTitle = Clean084(board.title, ids);
            var authoredTitle = Clean084(node.title, ids);
            var kind = (node.kind ?? string.Empty).ToUpperInvariant();
            var isChapter = StringComparer.Ordinal.Equals(
                board.operationKind, "CHAPTER") && chapter != null;

            string title;
            string description;
            string storyFlavor;
            string objective;
            if (isChapter)
            {
                var chapterTitle = Clean084(
                    First084(chapter.title, PlainBoardTitle084(boardTitle)), ids);
                var region = Clean084(First084(chapter.regionFocus, world), ids);
                title = ChapterRoomTitle084(kind, authoredTitle, chapterTitle);
                description = ChapterDescription084(
                    kind, authoredTitle, chapterTitle, region);
                storyFlavor = ChapterStory084(
                    board, node, chapter, chapterTitle, region, ids);
                objective = ChapterObjective084(kind, authoredTitle, chapterTitle,
                    Clean084(chapter.primaryObjectiveText, ids));
            }
            else
            {
                var theme = MissionTheme084(boardTitle);
                var tag = BoardTag084(world, theme);
                var mission = theme + " in " + world;
                title = ContractRoomTitle084(kind, authoredTitle, tag);
                description = ContractDescription084(kind, authoredTitle, mission);
                storyFlavor = ContractStory084(kind, authoredTitle, theme, world);
                objective = ContractObjective084(kind, authoredTitle, theme, world);
            }

            title = Clean084(title, ids);
            description = Clean084(description, ids);
            storyFlavor = Clean084(storyFlavor, ids);
            objective = Clean084(objective, ids);
            if (SameCopy084(storyFlavor, description))
                storyFlavor = string.Empty;
            if (string.IsNullOrWhiteSpace(storyFlavor))
                storyFlavor = "The Guild turns this room face up and follows the revealed route.";
            return new RoomCopy084
            {
                Title = title,
                Description = description,
                StoryFlavor = storyFlavor,
                Objective = objective
            };
        }

        public static string BoardObjective(
            BoardDto023 board,
            ChapterNarrative019 chapter,
            string worldDisplayName)
        {
            if (board == null) return string.Empty;
            var ids = new[] {board.definitionId, board.boardId};
            if (StringComparer.Ordinal.Equals(board.operationKind, "CHAPTER") &&
                chapter != null &&
                !string.IsNullOrWhiteSpace(chapter.primaryObjectiveText))
                return Clean084(chapter.primaryObjectiveText, ids);

            var world = FriendlyWorld084(worldDisplayName, board.worldId);
            var theme = MissionTheme084(Clean084(board.title, ids));
            var verb = StringComparer.Ordinal.Equals(board.operationKind, "CRISIS")
                ? "Resolve"
                : "Complete";
            return verb + " " + theme + " in " + world +
                   ", collect only the listed tile rewards, and return to the Guild.";
        }

        /// <summary>
        /// Builds a truthful successful-run reward range from the authored acyclic
        /// board. Branches are evaluated as paths, so mutually exclusive room
        /// rewards are never added together. The supplied multiplier is the same
        /// value authority will freeze when the run begins.
        /// </summary>
        public static string BoardRewardPreview(
            BoardDto023 board,
            int rewardPermille)
        {
            var nodes = (board?.nodes ?? Array.Empty<NodeDto023>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.nodeId))
                .GroupBy(value => value.nodeId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(),
                    StringComparer.Ordinal);
            if (nodes.Count == 0 || string.IsNullOrWhiteSpace(board?.startNodeId) ||
                !nodes.ContainsKey(board.startNodeId))
                return "STORY PROGRESS";

            var memo = new Dictionary<string, BoardRewardRange084>(
                StringComparer.Ordinal);
            var rewards = RewardRangeFrom084(board.startNodeId, nodes, memo,
                new HashSet<string>(StringComparer.Ordinal), rewardPermille);
            var parts = new List<string>();
            AddRewardRange084(parts, rewards.MinimumGuildXp,
                rewards.MaximumGuildXp, "GUILD XP");
            AddRewardRange084(parts, rewards.MinimumHallXp,
                rewards.MaximumHallXp, "HALL XP");
            AddMaterialRange084(parts, rewards.MinimumMaterials,
                rewards.MaximumMaterials);
            if (parts.Count > 0) return string.Join("  •  ", parts);

            var hasAuthoredReward = nodes.Values.Any(value =>
                value.guildXp > 0 || value.hallXp > 0 ||
                (value.materialIds?.Distinct(StringComparer.Ordinal).Count() ?? 0) > 0);
            return rewardPermille <= 0 && hasAuthoredReward
                ? "REPEAT REWARDS DEPLETED  •  STORY PROGRESS"
                : "STORY PROGRESS";
        }

        static BoardRewardRange084 RewardRangeFrom084(
            string nodeId,
            IReadOnlyDictionary<string, NodeDto023> nodes,
            IDictionary<string, BoardRewardRange084> memo,
            ISet<string> visiting,
            int rewardPermille)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || !nodes.TryGetValue(
                    nodeId, out var node) || visiting.Contains(nodeId))
                return new BoardRewardRange084();
            if (memo.TryGetValue(nodeId, out var saved)) return saved;

            visiting.Add(nodeId);
            var children = (node.nextNodeIds ?? Array.Empty<string>())
                .Where(nodes.ContainsKey)
                .Distinct(StringComparer.Ordinal)
                .Select(value => RewardRangeFrom084(value, nodes, memo,
                    visiting, rewardPermille))
                .ToArray();
            visiting.Remove(nodeId);

            var guildXp = BoardAdventureRules084.ScaledReward084(
                node.guildXp, rewardPermille);
            var hallXp = BoardAdventureRules084.ScaledReward084(
                node.hallXp, rewardPermille);
            var materialCount = BoardAdventureRules084.ScaledMaterialCount084(
                (node.materialIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .Count(), rewardPermille);
            var result = new BoardRewardRange084
            {
                MinimumGuildXp = guildXp +
                    (children.Length == 0 ? 0 : children.Min(value => value.MinimumGuildXp)),
                MaximumGuildXp = guildXp +
                    (children.Length == 0 ? 0 : children.Max(value => value.MaximumGuildXp)),
                MinimumHallXp = hallXp +
                    (children.Length == 0 ? 0 : children.Min(value => value.MinimumHallXp)),
                MaximumHallXp = hallXp +
                    (children.Length == 0 ? 0 : children.Max(value => value.MaximumHallXp)),
                MinimumMaterials = materialCount +
                    (children.Length == 0 ? 0 : children.Min(value => value.MinimumMaterials)),
                MaximumMaterials = materialCount +
                    (children.Length == 0 ? 0 : children.Max(value => value.MaximumMaterials))
            };
            memo[nodeId] = result;
            return result;
        }

        static void AddRewardRange084(
            ICollection<string> parts,
            int minimum,
            int maximum,
            string label)
        {
            if (maximum <= 0) return;
            parts.Add(minimum == maximum
                ? "+" + maximum + " " + label
                : minimum <= 0
                    ? "UP TO +" + maximum + " " + label
                    : "+" + minimum + "–" + maximum + " " + label);
        }

        static void AddMaterialRange084(
            ICollection<string> parts,
            int minimum,
            int maximum)
        {
            if (maximum <= 0) return;
            parts.Add(minimum == maximum
                ? "MATERIALS ×" + maximum
                : minimum <= 0
                    ? "UP TO " + maximum + " MATERIALS"
                    : "MATERIALS ×" + minimum + "–" + maximum);
        }

        sealed class BoardRewardRange084
        {
            public int MinimumGuildXp;
            public int MaximumGuildXp;
            public int MinimumHallXp;
            public int MaximumHallXp;
            public int MinimumMaterials;
            public int MaximumMaterials;
        }

        static string ChapterRoomTitle084(
            string kind,
            string authoredTitle,
            string chapterTitle)
        {
            if (StringComparer.Ordinal.Equals(kind, "START")) return chapterTitle;
            if (StringComparer.Ordinal.Equals(kind, "EXIT"))
                return "Road Home — " + chapterTitle;
            return authoredTitle + " — " + chapterTitle;
        }

        static string ChapterDescription084(
            string kind,
            string authoredTitle,
            string chapterTitle,
            string region)
        {
            switch (kind)
            {
                case "START":
                    return "Place the Guild pawn in " + region + " and begin " +
                           chapterTitle + ".";
                case "ROUTE":
                    return authoredTitle + " carries the Guild through " + region +
                           " toward " + chapterTitle + "; the unused path closes.";
                case "EVENT":
                    return "Hear how " + chapterTitle + " affects people in " + region +
                           "; this revealed room resolves the Guild's response automatically.";
                case "CHECK":
                    return "The committed crew rolls 2d6 on the road to " + chapterTitle +
                           "; the revealed result cannot be rerolled.";
                case "CAMP":
                    return "Pause near " + region +
                           ", then turn over the next room toward " + chapterTitle + ".";
                case "DIPLOMACY":
                    return "Hear the local position on " + chapterTitle +
                           "; this revealed room resolves the Guild's public answer.";
                case "RESOURCE":
                    return "Search this optional room on the " + chapterTitle +
                           " route; collect only the reward shown below.";
                case "OBJECTIVE":
                    return "Complete the main objective of " + chapterTitle +
                           ", then open the road home.";
                case "BATTLE":
                    return "Resolve the " + chapterTitle +
                           " encounter through the existing Union battle.";
                case "DEFENSE_PREP":
                    return "Resolve the Guild's defense response for " + chapterTitle +
                           "; this room records its listed result automatically.";
                case "EXIT":
                    return "The board route for " + chapterTitle +
                           " is complete. Return to the Guild and continue the story.";
                default:
                    return "Turn over the next room on the route to " + chapterTitle + ".";
            }
        }

        static string ChapterStory084(
            BoardDto023 board,
            NodeDto023 node,
            ChapterNarrative019 chapter,
            string chapterTitle,
            string region,
            string[] ids)
        {
            var kind = (node.kind ?? string.Empty).ToUpperInvariant();
            switch (kind)
            {
                case "START":
                    return Clean084(chapter.openingBriefing, ids);
                case "ROUTE":
                {
                    var routes = (board.nodes ?? Array.Empty<NodeDto023>())
                        .Where(value => value != null &&
                            StringComparer.OrdinalIgnoreCase.Equals(value.kind, "ROUTE"))
                        .ToArray();
                    var routeIndex = Array.FindIndex(routes, value =>
                        StringComparer.Ordinal.Equals(value.nodeId, node.nodeId));
                    return routeIndex <= 0
                        ? PlainRoute084(chapter.routeSummary, chapterTitle, region)
                        : First084(chapter.guildmasterQuestion,
                            "What must the Guild protect while approaching " +
                            chapterTitle + "?");
                }
                case "EVENT":
                    return Clean084(chapter.civilianScene, ids);
                case "CHECK":
                    return Clean084(First084(chapter.guideTip,
                        "The Guild weighs the risk before moving deeper into " +
                        chapterTitle + "."), ids);
                case "CAMP":
                    return "A quiet stop in " + region +
                           " gives the Guild one breath before the route continues.";
                case "DIPLOMACY":
                    return Clean084(chapter.factionTension, ids);
                case "RESOURCE":
                {
                    var optional = chapter.optionalObjectiveTexts ?? Array.Empty<string>();
                    return optional.Length > 0
                        ? "Optional lead: " + Clean084(optional[0], ids) + "."
                        : "An optional lead may help " + chapterTitle +
                          " without replacing its main objective.";
                }
                case "OBJECTIVE":
                    return Clean084(chapter.primaryObjectiveText, ids);
                case "BATTLE":
                    return Clean084(First084(chapter.battleBriefing,
                        chapter.primaryObjectiveText), ids);
                case "EXIT":
                    return Clean084(chapter.cityConsequenceText, ids);
                default:
                    return "The Guild follows the revealed route through " + region + ".";
            }
        }

        static string ChapterObjective084(
            string kind,
            string authoredTitle,
            string chapterTitle,
            string primaryObjective)
        {
            switch (kind)
            {
                case "START": return "Begin " + chapterTitle + ".";
                case "ROUTE": return "Take " + authoredTitle + " toward " + chapterTitle + ".";
                case "EVENT": return "Hear " + authoredTitle + " during " + chapterTitle + ".";
                case "CHECK": return "Resolve " + authoredTitle + " on the road to " + chapterTitle + ".";
                case "CAMP": return "Rest at " + authoredTitle + ", then reveal the next room toward " + chapterTitle + ".";
                case "DIPLOMACY": return "Answer " + authoredTitle + " during " + chapterTitle + ".";
                case "RESOURCE": return "Search " + authoredTitle + " without losing the route to " + chapterTitle + ".";
                case "OBJECTIVE": return "Complete " + authoredTitle + " for " + chapterTitle + ".";
                case "BATTLE": return "Win " + authoredTitle + " to complete " + chapterTitle + ".";
                case "EXIT": return "Return to the Guild and continue " + chapterTitle + ".";
                default: return "Move the pawn one room closer to " + chapterTitle + ".";
            }
        }

        static string ContractRoomTitle084(
            string kind,
            string authoredTitle,
            string boardTag)
        {
            if (StringComparer.Ordinal.Equals(kind, "START"))
                return boardTag + ": Mission Begins";
            if (StringComparer.Ordinal.Equals(kind, "EXIT"))
                return boardTag + ": Road Home";
            return boardTag + ": " + authoredTitle;
        }

        static string ContractDescription084(
            string kind,
            string authoredTitle,
            string mission)
        {
            switch (kind)
            {
                case "START":
                    return "Place the pawn and begin " + mission + ".";
                case "ROUTE":
                    return authoredTitle + " advances " + mission +
                           "; the unused path closes.";
                case "EVENT":
                    return authoredTitle + " changes how the Guild approaches " + mission + ".";
                case "CHECK":
                    return "At " + authoredTitle + ", the committed crew rolls 2d6 for " + mission +
                           "; the revealed result cannot be rerolled.";
                case "CAMP":
                    return "Pause at " + authoredTitle + " on the " + mission +
                           " route, then turn over the next face-down room.";
                case "DIPLOMACY":
                    return "At " + authoredTitle + ", hear the local position on " + mission +
                           "; the revealed room resolves the Guild's public response.";
                case "RESOURCE":
                    return "Search " + authoredTitle + " on the route for " + mission +
                           "; collect only the reward shown below.";
                case "DEFENSE_PREP":
                    return "Resolve " + authoredTitle + " as the Guild's response to " + mission +
                           "; this tile records its listed result automatically.";
                case "OBJECTIVE":
                    return "Complete " + authoredTitle + " for " + mission +
                           ", then open the road home.";
                case "BATTLE":
                    return "Resolve " + authoredTitle + " for the " + mission +
                           " encounter through the existing Union battle.";
                case "EXIT":
                    return "At " + authoredTitle + ", the route for " + mission +
                           " closes. Return to the Guild and save the result.";
                default:
                    return "Turn over " + authoredTitle + " for " + mission + ".";
            }
        }

        static string ContractStory084(
            string kind,
            string authoredTitle,
            string theme,
            string world)
        {
            switch (kind)
            {
                case "START":
                    return theme + " begins in " + world +
                           " with every later room face down.";
                case "ROUTE":
                    return authoredTitle + " changes how the Guild crosses " + world +
                           " during " + theme + ".";
                case "EVENT":
                    return "People in " + world + " turn " + theme +
                           " into a lived problem, not a ledger entry.";
                case "CHECK":
                    return "The dice settle this risk in " + theme +
                           "; the revealed result stands.";
                case "CAMP":
                    return "A quiet stop on the " + theme +
                           " route gives the Guild one breath before moving on.";
                case "DIPLOMACY":
                    return world + " residents ask what the Guild will stand for during " +
                           theme + ".";
                case "RESOURCE":
                    return "What the Guild recovers from " + authoredTitle +
                           " supports " + theme + " rather than conquest.";
                case "DEFENSE_PREP":
                    return "At " + authoredTitle + ", the " + theme +
                           " crisis narrows before the next room turns.";
                case "OBJECTIVE":
                    return "The pawn reaches the heart of " + theme + " in " + world + ".";
                case "BATTLE":
                    return "The " + theme +
                           " board hands this room to the existing Union battle.";
                case "EXIT":
                    return "The pawn reaches the Guild-bound edge of " + world +
                           " after " + theme + ".";
                default:
                    return "The next room turns over on the " + theme + " route.";
            }
        }

        static string ContractObjective084(
            string kind,
            string authoredTitle,
            string theme,
            string world)
        {
            switch (kind)
            {
                case "START": return "Begin " + theme + " in " + world + ".";
                case "ROUTE": return "Follow " + authoredTitle + " on the " + theme + " route in " + world + ".";
                case "EVENT": return "Resolve " + authoredTitle + " for " + theme + " in " + world + ".";
                case "CHECK": return "Resolve " + authoredTitle + " for " + theme + " in " + world + ".";
                case "CAMP": return "Rest at " + authoredTitle + ", then reveal the next room during " + theme + " in " + world + ".";
                case "DIPLOMACY": return "Answer " + authoredTitle + " for " + theme + " in " + world + ".";
                case "RESOURCE": return "Search " + authoredTitle + " during " + theme + " in " + world + ".";
                case "DEFENSE_PREP": return "Resolve " + authoredTitle + " for " + theme + " in " + world + ".";
                case "OBJECTIVE": return "Complete " + authoredTitle + " for " + theme + " in " + world + ".";
                case "BATTLE": return "Win " + authoredTitle + " for " + theme + " in " + world + ".";
                case "EXIT": return "Return to the Guild with the " + theme + " result from " + world + ".";
                default: return "Move through " + authoredTitle + " on the " + theme + " route in " + world + ".";
            }
        }

        static string PlainRoute084(string route, string chapterTitle, string region)
        {
            if (string.IsNullOrWhiteSpace(route))
                return "The planned route through " + region +
                       " leads to " + chapterTitle + " and then home.";
            var value = route.Replace(" → ", ", then ").Replace("→", ", then ").Trim();
            while (value.Contains("  ")) value = value.Replace("  ", " ");
            return "The planned route runs " + value.Trim(' ', ',', '.') + ".";
        }

        static string PlainBoardTitle084(string value)
        {
            const string suffix = " — Expedition Board";
            return value != null && value.EndsWith(suffix, StringComparison.Ordinal)
                ? value.Substring(0, value.Length - suffix.Length)
                : value ?? "Story Quest";
        }

        static string MissionTheme084(string boardTitle)
        {
            if (string.IsNullOrWhiteSpace(boardTitle)) return "Guild Mission";
            var colon = boardTitle.IndexOf(':');
            return colon >= 0 && colon + 1 < boardTitle.Length
                ? boardTitle.Substring(colon + 1).Trim()
                : PlainBoardTitle084(boardTitle);
        }

        static string BoardTag084(string world, string theme)
        {
            var worldTag = ShortWorld084(world);
            var themeTag = theme.IndexOf("Patrol", StringComparison.OrdinalIgnoreCase) >= 0
                ? "Patrol"
                : theme.IndexOf("Civilian", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "Rescue"
                    : theme.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "Logistics"
                        : theme.IndexOf("Elite", StringComparison.OrdinalIgnoreCase) >= 0 ||
                          theme.IndexOf("Fortress", StringComparison.OrdinalIgnoreCase) >= 0
                            ? "Fortress"
                            : theme.IndexOf("Multi-Front", StringComparison.OrdinalIgnoreCase) >= 0
                                ? "Defense"
                                : theme.IndexOf("Civic", StringComparison.OrdinalIgnoreCase) >= 0
                                    ? "Civic Crisis"
                                    : FirstWords084(theme, 2);
            return worldTag + " " + themeTag;
        }

        static string ShortWorld084(string world)
        {
            foreach (var known in new[]
                     {
                         "Skyhome", "Goblin", "Orc", "Beastkin", "Demon",
                         "Dark Elf", "Bunny", "Dog"
                     })
                if (world.IndexOf(known, StringComparison.OrdinalIgnoreCase) >= 0)
                    return known;
            return FirstWords084(world, 2);
        }

        static string FriendlyWorld084(string displayName, string worldId)
        {
            if (!string.IsNullOrWhiteSpace(displayName)) return displayName.Trim();
            var words = (worldId ?? "Guild World").Replace('_', ' ').ToLowerInvariant()
                .Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? "Guild World"
                : string.Join(" ", words.Select(value =>
                    char.ToUpperInvariant(value[0]) + value.Substring(1)));
        }

        static string FirstWords084(string value, int count)
        {
            var words = (value ?? string.Empty).Split(
                new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? "Guild Mission"
                : string.Join(" ", words.Take(Math.Max(1, count)));
        }

        static string First084(string preferred, string fallback) =>
            string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

        static string Clean084(string value, params string[] ids) =>
            BoardAdventureRules084.PlayerCopy084(value, ids);

        static bool SameCopy084(string left, string right) =>
            !string.IsNullOrWhiteSpace(left) &&
            StringComparer.OrdinalIgnoreCase.Equals(
                Normalize084(left), Normalize084(right));

        static string Normalize084(string value) => string.Join(" ",
            (value ?? string.Empty).Split(
                new[] {' ', '\r', '\n', '\t'},
                StringSplitOptions.RemoveEmptyEntries)).Trim().TrimEnd('.');
    }
}
