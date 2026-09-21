using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.GuildCity017D
{
    [Serializable]
    public sealed class GuildCityCostDefinition017D
    {
        public int Level;
        public long HallXp;
        public GuildMaterialDefinition017D[] Materials;
    }

    [Serializable]
    public sealed class GuildMaterialDefinition017D
    {
        public string MaterialId;
        public int Amount;
    }

    [Serializable]
    public sealed class GuildCityBuildingDefinition017D
    {
        public string Id;
        public string DisplayName;
        public string DistrictId;
        public string FacilityId;
        public int MaxLevel;
        public string EffectIdentity;
        public string[] AdjacencyBuildingIds;
        public string[] StaffRoles;
        public GuildCityCostDefinition017D[] Costs;
        public int RealTimeTimerSeconds;
        public bool AllowsInvoluntaryDeparture;

        public GuildCityCostDefinition017D CostForLevel(int level)
        {
            if (Costs == null) return null;
            for (var i = 0; i < Costs.Length; i++) if (Costs[i].Level == level) return Costs[i];
            return null;
        }
    }

    [Serializable] internal sealed class BuildingRoot017D { public string ContentVersion; public GuildCityBuildingDefinition017D[] Buildings; }
    [Serializable] internal sealed class PlotRoot017D { public string ContentVersion; public string CenterId; public CityPlotDefinition017D[] Plots; }
    [Serializable] public sealed class CityPlotDefinition017D { public string PlotId; public string DistrictId; public string Size; public bool InitiallyUnlocked; public bool RoadConnected; public string UnlockRequirementId; public string[] AdjacentPlotIds; }
    [Serializable] internal sealed class ContractRoot017D { public string ContentVersion; public ContractDefinition017D[] Contracts; }
    [Serializable] public sealed class ContractDefinition017D
    {
        public string Id; public string DisplayName; public string Sponsor; public string Family; public string Hook;
        public string PrimaryObjective; public string[] OptionalObjectives; public string BoardId;
        public int ExpectedEnemyUnionMin; public int ExpectedEnemyUnionMax; public string[] Hazards;
        public string[] RecommendedSkills; public long BaseGuildXp; public long BaseHallXp;
        public GuildMaterialDefinition017D[] MaterialReward; public string CityHook;
        public string Rank; public int EstimatedMinutes; public string Pressure; public string FailureConsequence;
        public string RetreatConsequence; public string[] EquipmentPossibilities; public string CityUnlock;
    }
    [Serializable] internal sealed class BoardRoot017D { public string ContentVersion; public BoardDefinition017D[] Boards; }
    [Serializable] public sealed class BoardDefinition017D
    {
        public string Id; public string DisplayName; public string StartNodeId; public string ObjectiveNodeId; public string ExitNodeId;
        public int StartingSupplies; public int StartingUrgency; public BoardNodeDefinition017D[] Nodes;
        public BoardNodeDefinition017D Node(string nodeId)
        {
            if (Nodes != null) for (var i=0;i<Nodes.Length;i++) if (StringComparer.Ordinal.Equals(Nodes[i].Id,nodeId)) return Nodes[i];
            throw new KeyNotFoundException("Unknown board node: " + nodeId);
        }
    }
    [Serializable] public sealed class BoardNodeDefinition017D
    {
        public string Id; public string Kind; public string[] Links; public string EventId; public string CheckSkillId; public string EncounterId;
        public int SupplyCost; public int FatigueCost; public int UrgencyCost;
        public string DisplayName; public string Summary; public string Risk; public string IconResourcePath;
    }
    [Serializable] internal sealed class EventRoot017D { public string ContentVersion; public EventDefinition017D[] Events; }
    [Serializable] public sealed class EventDefinition017D
    {
        public string Id; public string Title; public string Problem; public string[] EligibleSkills; public string ConsequenceIdentity;
        public bool UsesCommitted2d6; public bool RelationshipSceneCostsOperation; public bool CanCauseDeparture;
        public string ExceptionalText; public string FullSuccessText; public string SuccessWithCostText;
        public string SetbackText; public string SevereSetbackText; public string RelationshipMemorySummary;
    }

    public sealed class GuildCityContent017D
    {
        private const string LegacyFirstRescueBoardId069 = "BOARD_BELL_BENEATH_GATE";
        private const string StreamlinedFirstRescueBoardId069 = "BOARD_BELL_BENEATH_GATE_069";
        private const string FirstHourThreeBattleBoardId071 = "BOARD_BELL_BENEATH_GATE_071";

        private readonly Dictionary<string,GuildCityBuildingDefinition017D> _buildings;
        private readonly Dictionary<string,ContractDefinition017D> _contracts;
        private readonly Dictionary<string,BoardDefinition017D> _boards;
        private readonly IReadOnlyDictionary<string,BoardDefinition017D> _boardAuthorityView069;
        private readonly Dictionary<string,EventDefinition017D> _events;
        private readonly CityPlotDefinition017D[] _plots;
        private readonly Dictionary<string,CityPlotDefinition017D> _plotsById;

        private GuildCityContent017D(BuildingRoot017D buildingRoot, PlotRoot017D plotRoot,
            ContractRoot017D contractRoot, BoardRoot017D boardRoot, EventRoot017D eventRoot)
        {
            _buildings = Index(buildingRoot.Buildings, value => value.Id, "building");
            _contracts = Index(contractRoot.Contracts, value => value.Id, "contract");
            _boards = Index(boardRoot.Boards, value => value.Id, "board");
            if (_boards.ContainsKey(StreamlinedFirstRescueBoardId069) ||
                _boards.ContainsKey(FirstHourThreeBattleBoardId071))
                throw new InvalidDataException(
                    "First-hour compatibility boards must remain runtime projections, not serialized authority.");
            var streamlinedFirstRescue069 = CreateStreamlinedFirstRescueBoard069(_boards);
            var firstHourThreeBattle071 = CreateFirstHourThreeBattleBoard071(_boards);
            _boardAuthorityView069 = new BoardAuthorityView069(
                _boards,
                new Dictionary<string,BoardDefinition017D>(StringComparer.Ordinal)
                {
                    { StreamlinedFirstRescueBoardId069, streamlinedFirstRescue069 },
                    { FirstHourThreeBattleBoardId071, firstHourThreeBattle071 }
                });
            _events = Index(eventRoot.Events, value => value.Id, "event");
            _plots = plotRoot.Plots ?? Array.Empty<CityPlotDefinition017D>();
            _plotsById = Index(_plots, value => value.PlotId, "plot");
            Validate();
        }

        public IReadOnlyDictionary<string,GuildCityBuildingDefinition017D> Buildings => _buildings;
        public IReadOnlyDictionary<string,ContractDefinition017D> Contracts => _contracts;
        /// <summary>
        /// Enumerates only the three serialized, certified 15-node boards.  The
        /// Version 69 and Release 071 first-rescue IDs are lookup aliases for
        /// compatibility/runtime projections and never change the certified
        /// 3-board / 45-node serialized authority.
        /// </summary>
        public IReadOnlyDictionary<string,BoardDefinition017D> Boards => _boardAuthorityView069;
        public IReadOnlyDictionary<string,EventDefinition017D> Events => _events;
        public IReadOnlyList<CityPlotDefinition017D> Plots => Array.AsReadOnly(_plots);
        public GuildCityBuildingDefinition017D Building(string id) => _buildings[id];
        public ContractDefinition017D Contract(string id) => _contracts[id];
        public BoardDefinition017D Board(string id) => _boardAuthorityView069[id];
        public EventDefinition017D Event(string id) => _events[id];
        public CityPlotDefinition017D Plot(string id) => _plotsById[id];

        public static GuildCityContent017D LoadFromDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Content directory is required.", nameof(directory));
            return new GuildCityContent017D(
                Read<BuildingRoot017D>(directory, "GUILD_CITY_BUILDINGS_017D.json"),
                Read<PlotRoot017D>(directory, "OPENING_CITY_LAYOUT_017D.json"),
                Read<ContractRoot017D>(directory, "OPENING_CONTRACTS_017D.json"),
                Read<BoardRoot017D>(directory, "OPENING_BOARD_017D.json"),
                Read<EventRoot017D>(directory, "OPENING_EVENTS_017D.json"));
        }

        private void Validate()
        {
            if (_plots.Length != 12) throw new InvalidDataException("Opening city must contain exactly 12 plots.");
            var unlocked = 0;
            for (var i=0;i<_plots.Length;i++) if (_plots[i].InitiallyUnlocked) unlocked++;
            if (unlocked != 6) throw new InvalidDataException("Opening city must contain exactly 6 unlocked plots.");
            foreach (var plot in _plotsById.Values)
            {
                if (plot.AdjacentPlotIds == null) throw new InvalidDataException("Every city plot needs adjacency data: " + plot.PlotId);
                for (var index = 0; index < plot.AdjacentPlotIds.Length; index++)
                {
                    if (!_plotsById.ContainsKey(plot.AdjacentPlotIds[index]))
                        throw new InvalidDataException("Unknown adjacent plot: " + plot.AdjacentPlotIds[index]);
                    var reverse = _plotsById[plot.AdjacentPlotIds[index]].AdjacentPlotIds ?? Array.Empty<string>();
                    var foundReverse = false;
                    for (var reverseIndex = 0; reverseIndex < reverse.Length; reverseIndex++)
                        if (StringComparer.Ordinal.Equals(reverse[reverseIndex], plot.PlotId)) foundReverse = true;
                    if (!foundReverse) throw new InvalidDataException("Plot adjacency must be bidirectional: " + plot.PlotId);
                }
            }
            foreach (var building in _buildings.Values)
            {
                if (building.RealTimeTimerSeconds != 0) throw new InvalidDataException("Real-time timers are forbidden.");
                if (building.AllowsInvoluntaryDeparture) throw new InvalidDataException("Buildings cannot cause recruit departure.");
            }
            foreach (var eventDefinition in _events.Values)
            {
                if (eventDefinition.RelationshipSceneCostsOperation) throw new InvalidDataException("Relationship scenes cannot cost an operation.");
                if (eventDefinition.CanCauseDeparture) throw new InvalidDataException("Events cannot cause recruit departure.");
            }
            ValidateMinimumQuestCardRoute090(
                _boardAuthorityView069[FirstHourThreeBattleBoardId071]);
            ValidateMinimumQuestCardRoute090(
                _boardAuthorityView069[GuildCityExpeditionService017D.SecondStoryBoardId076]);
            ValidateMinimumQuestCardRoute090(
                _boardAuthorityView069[GuildCityExpeditionService017D.ReliefRoadBoardId081]);
        }

        /// <summary>
        /// Every advertised card quest must contain at least ten actual card
        /// choices on its shortest legal route. Locked story battles remain on
        /// their authored nodes and are deliberately not counted as card rounds.
        /// </summary>
        public static int MinimumQuestCardMovesToExit090(
            BoardDefinition017D board)
        {
            if (board == null || string.IsNullOrWhiteSpace(board.StartNodeId) ||
                string.IsNullOrWhiteSpace(board.ExitNodeId))
                return -1;
            var byId = (board.Nodes ?? Array.Empty<BoardNodeDefinition017D>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.Id))
                .ToDictionary(value => value.Id, value => value,
                    StringComparer.Ordinal);
            if (!byId.ContainsKey(board.StartNodeId) ||
                !byId.ContainsKey(board.ExitNodeId))
                return -1;

            var distance = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { board.StartNodeId, 0 }
            };
            var pending = new Queue<string>();
            pending.Enqueue(board.StartNodeId);
            while (pending.Count > 0)
            {
                var currentId = pending.Dequeue();
                if (StringComparer.Ordinal.Equals(currentId, board.ExitNodeId))
                    return distance[currentId];
                var current = byId[currentId];
                foreach (var linkedId in current.Links ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(linkedId) ||
                        !byId.ContainsKey(linkedId) ||
                        distance.ContainsKey(linkedId))
                        continue;
                    distance.Add(linkedId, checked(distance[currentId] + 1));
                    pending.Enqueue(linkedId);
                }
            }
            return -1;
        }

        private static void ValidateMinimumQuestCardRoute090(
            BoardDefinition017D board)
        {
            var minimum = MinimumQuestCardMovesToExit090(board);
            if (minimum < GuildCityExpeditionService017D.MinimumQuestCardRounds090)
                throw new InvalidDataException(
                    "Advertised card quest " + (board?.Id ?? "UNKNOWN") +
                    " has only " + minimum + " card choices on its shortest route; " +
                    GuildCityExpeditionService017D.MinimumQuestCardRounds090 +
                    " are required outside locked story battles.");
        }

        private static BoardDefinition017D CreateStreamlinedFirstRescueBoard069(
            IReadOnlyDictionary<string,BoardDefinition017D> boards)
        {
            BoardDefinition017D legacy;
            if (boards == null || !boards.TryGetValue(LegacyFirstRescueBoardId069, out legacy))
                throw new InvalidDataException("Legacy first-rescue board authority is missing.");

            return new BoardDefinition017D
            {
                Id = StreamlinedFirstRescueBoardId069,
                DisplayName = "The Bell Beneath Skyhome — Lantern Road",
                StartNodeId = "N00",
                ObjectiveNodeId = "N06",
                ExitNodeId = "N14",
                StartingSupplies = 12,
                StartingUrgency = 14,
                Nodes = new[]
                {
                    ProjectFirstRescueNode069(legacy, "N00", "START", new[] { "N01" },
                        string.Empty, string.Empty, string.Empty, 0, 0, 0),
                    ProjectFirstRescueNode069(legacy, "N01", "ROUTE", new[] { "N04" },
                        string.Empty, string.Empty, string.Empty, 0, 0, 0),
                    ProjectFirstRescueNode069(legacy, "N04", "SKILL_CHECK", new[] { "N06" },
                        "EVENT_COLLAPSED_HANDRAIL", "Engineering", string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N06", "ENCOUNTER", new[] { "N14" },
                        string.Empty, string.Empty, "ENCOUNTER_GATE_GNAWER_RESCUE", 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N14", "EXIT", Array.Empty<string>(),
                        string.Empty, string.Empty, string.Empty, 0, 0, 0)
                }
            };
        }

        /// <summary>
        /// The player-facing first operation keeps the three authored combat
        /// payoffs, but otherwise restores the complete Adventure Guild board loop:
        /// a connected rescue story, a camp, scouting and resource beats, committed
        /// character checks, an optional elite, a final approach choice, the rescue,
        /// and the extraction.  The previous six-node projection made a nominal first hour
        /// play like a short corridor and hid most of the approved Guildmaster loop.
        /// All state changes still use the same committed move/check/encounter
        /// services and exact-once battle-return authority.
        /// </summary>
        private static BoardDefinition017D CreateFirstHourThreeBattleBoard071(
            IReadOnlyDictionary<string,BoardDefinition017D> boards)
        {
            BoardDefinition017D legacy;
            if (boards == null || !boards.TryGetValue(LegacyFirstRescueBoardId069, out legacy))
                throw new InvalidDataException("Legacy first-rescue board authority is missing.");

            return new BoardDefinition017D
            {
                Id = FirstHourThreeBattleBoardId071,
                DisplayName = "The Bell Beneath Skyhome — First Hour",
                StartNodeId = "N00",
                ObjectiveNodeId = "N13",
                ExitNodeId = "N14",
                StartingSupplies = 12,
                StartingUrgency = 14,
                Nodes = new[]
                {
                    ProjectFirstRescueNode069(legacy, "N00", "START", new[] { "N01" },
                        string.Empty, string.Empty, string.Empty, 0, 0, 0),
                    // The rescue is an authored company story, not a collection of
                    // mutually exclusive ledger branches.  Una's warning, Tazren's
                    // trail, Quin's rescue, the camp, and the Wayglass incident now
                    // happen in a readable sequence.  Player choice changes how each
                    // problem is handled instead of silently deleting half the cast.
                    ProjectFirstRescueNode069(legacy, "N01", "ENCOUNTER", new[] { "N02" },
                        string.Empty, string.Empty, "ENCOUNTER071_HALL_BREACH", 0, 0, 0),
                    ProjectFirstRescueNode069(legacy, "N02", "EVENT", new[] { "N03" },
                        "EVENT_INJURED_COURIER", string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N03", "SCOUTING", new[] { "N04" },
                        string.Empty, string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N04", "SKILL_CHECK", new[] { "N05" },
                        "EVENT_COLLAPSED_HANDRAIL", "Engineering", string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N05", "RESOURCE", new[] { "N06" },
                        string.Empty, string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N06", "ENCOUNTER", new[] { "N07" },
                        string.Empty, string.Empty, "ENCOUNTER071_LANTERN_ROAD_AMBUSH", 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N07", "CAMP", new[] { "N08" },
                        "EVENT_LANTERN_WATCH_CAMP", string.Empty, string.Empty, 0, 0, 0),
                    // Hunting the Gatehouse Stalker remains optional.  Bypassing it
                    // advances directly to Petra; fighting it rejoins at the same
                    // rescue beat with its route ledger reward intact.
                    ProjectFirstRescueNode069(legacy, "N08", "EVENT", new[] { "N09", "N10" },
                        "EVENT_UNSTABLE_BELL_CHAIN", string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N09", "OPTIONAL_ELITE", new[] { "N10" },
                        string.Empty, string.Empty, "ENCOUNTER_GATE_GNAWER_ELITE", 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N10", "SKILL_CHECK", new[] { "N11", "N12" },
                        "EVENT_TRAPPED_FOREMAN", "Medicine", string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N11", "SECONDARY_OBJECTIVE", new[] { "N13" },
                        "EVENT_GATEGLASS_PULSE", string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N12", "SAFE_ROUTE", new[] { "N13" },
                        GuildCityExpeditionService017D.FirstStorySafePassageEventId080,
                        string.Empty, string.Empty, 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N13", "MAIN_OBJECTIVE", new[] { "N14" },
                        string.Empty, string.Empty, "ENCOUNTER071_GATE_EATER", 1, 1, 1),
                    ProjectFirstRescueNode069(legacy, "N14", "EXIT", Array.Empty<string>(),
                        string.Empty, string.Empty, string.Empty, 0, 0, 0)
                }
            };
        }

        private static BoardNodeDefinition017D ProjectFirstRescueNode069(
            BoardDefinition017D legacy,
            string id,
            string kind,
            string[] links,
            string eventId,
            string checkSkillId,
            string encounterId,
            int supplyCost,
            int fatigueCost,
            int urgencyCost)
        {
            var source = legacy.Node(id);
            return new BoardNodeDefinition017D
            {
                Id = id,
                Kind = kind,
                Links = links,
                EventId = eventId,
                CheckSkillId = checkSkillId,
                EncounterId = encounterId,
                SupplyCost = supplyCost,
                FatigueCost = fatigueCost,
                UrgencyCost = urgencyCost,
                DisplayName = source.DisplayName,
                Summary = source.Summary,
                Risk = source.Risk,
                IconResourcePath = source.IconResourcePath
            };
        }

        private sealed class BoardAuthorityView069 :
            IReadOnlyDictionary<string,BoardDefinition017D>
        {
            private readonly IReadOnlyDictionary<string,BoardDefinition017D> _authority;
            private readonly IReadOnlyDictionary<string,BoardDefinition017D> _aliases;

            public BoardAuthorityView069(
                IReadOnlyDictionary<string,BoardDefinition017D> authority,
                IReadOnlyDictionary<string,BoardDefinition017D> aliases)
            {
                _authority = authority ?? throw new ArgumentNullException(nameof(authority));
                _aliases = aliases ?? throw new ArgumentNullException(nameof(aliases));
            }

            public int Count => _authority.Count;
            public IEnumerable<string> Keys => _authority.Keys;
            public IEnumerable<BoardDefinition017D> Values => _authority.Values;

            public BoardDefinition017D this[string key]
            {
                get
                {
                    BoardDefinition017D alias;
                    if (_aliases.TryGetValue(key, out alias)) return alias;
                    return _authority[key];
                }
            }

            public bool ContainsKey(string key) =>
                _aliases.ContainsKey(key) || _authority.ContainsKey(key);

            public bool TryGetValue(string key, out BoardDefinition017D value)
            {
                if (_aliases.TryGetValue(key, out value)) return true;
                return _authority.TryGetValue(key, out value);
            }

            public IEnumerator<KeyValuePair<string,BoardDefinition017D>> GetEnumerator() =>
                _authority.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static T Read<T>(string directory, string file)
        {
            var path = Path.Combine(directory, file);
            if (!File.Exists(path)) throw new FileNotFoundException("Guild/city authority file is missing.", path);
            var result = JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
            if (result == null) throw new InvalidDataException("Guild/city authority file is empty: " + file);
            return result;
        }

        private static Dictionary<string,T> Index<T>(IReadOnlyList<T> values, Func<T,string> id, string label)
        {
            var result = new Dictionary<string,T>(StringComparer.Ordinal);
            if (values != null)
                for (var i=0;i<values.Count;i++)
                {
                    var key = id(values[i]);
                    if (string.IsNullOrWhiteSpace(key) || result.ContainsKey(key))
                        throw new InvalidDataException("Invalid or duplicate " + label + " ID: " + key);
                    result.Add(key, values[i]);
                }
            return result;
        }
    }

    public static class GuildCityOpeningContent017D
    {
        public static IReadOnlyList<CityPlotState017D> CreateInitialPlotStates()
        {
            var result = new List<CityPlotState017D>();
            var districts = new[] { "DISTRICT_GUILD_CORE","DISTRICT_GUILD_CORE","DISTRICT_GUILD_CORE","DISTRICT_GUILD_CORE",
                "DISTRICT_CRAFT","DISTRICT_CRAFT","DISTRICT_CARE_COMMONS","DISTRICT_CARE_COMMONS",
                "DISTRICT_KNOWLEDGE","DISTRICT_KNOWLEDGE","DISTRICT_GATE_DEFENSE","DISTRICT_GATE_DEFENSE" };
            for (var i=0;i<12;i++)
                result.Add(new CityPlotState017D("GC017D_PLOT_" + (i+1).ToString("00"), districts[i],
                    i==3 || i==11 ? "LARGE" : "STANDARD", i<8, i<6,
                    string.Empty, 0, 0, Array.Empty<string>()));
            return result.AsReadOnly();
        }
    }
}
