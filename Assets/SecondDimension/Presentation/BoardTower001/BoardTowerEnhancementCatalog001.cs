using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Gameplay.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Presentation.BoardTower001
{
    [Serializable]
    public sealed class BoardTowerItem001
    {
        public string itemId;
        public string displayName;
        public string familyId;
        public string familyName;
        public string rarity;
        public int powerBudget;
        public string itemClass;
        public string preparationSlot;
        public string validModes;
        public string useTiming;
        public int stackLimit;
        public string effectSummary;
        public string drawbackOrCost;
        public string duration;
        public int storyMinChapter;
        public int towerMinFloor;
        public string unlockBuilding;
        public string naturalAcquisition;
        public bool codeEligible;
        public string firstHourPriority;
        public string determinismRule;
        public string integrationTags;
    }

    [Serializable]
    public sealed class BoardTowerRoom001
    {
        public string roomId;
        public string displayName;
        public string roomType;
        public string rarity;
        public string allowedModes;
        public int baseWeight;
        public int minChapter;
        public int minTowerFloor;
        public string revealText;
        public int choiceCount;
        public string resolutionPattern;
        public string battleCondition;
        public string rewardDirection;
        public string threatDelta;
        public string supplyDelta;
        public bool canCarryStoryAnchor;
        public string textLimit;
        public string determinismRule;
        public string firstHourPriority;
        public string tags;
    }

    [Serializable]
    public sealed class BoardTowerRunEffect001
    {
        public string effectId;
        public string displayName;
        public string effectType;
        public string rarity;
        public string validModes;
        public string duration;
        public string mechanicalEffect;
        public string visibleRule;
        public string stackRule;
        public string cleanseMethod;
        public string firstHourPriority;
        public string determinismRule;
    }

    [Serializable]
    public sealed class BoardTowerTurningPoint001
    {
        public string turningPointId;
        public string displayName;
        public string trigger;
        public string permanentBenefit;
        public string permanentCostOrDuty;
        public string evolutionCondition;
        public string evolvedName;
        public string validCharacters;
        public string sourceModes;
        public string firstHourPriority;
        public string rule;
    }

    [Serializable]
    public sealed class BoardTowerRewardHook001
    {
        public string hookId;
        public string system;
        public string trigger;
        public string frequency;
        public string rewardDirection;
        public string rule;
        public string antiExploit;
    }

    [Serializable]
    public sealed class BoardTowerP0Reference001
    {
        public string contentType;
        public string stableId;
        public int integrationOrder;
        public string purpose;
    }

    [Serializable]
    public sealed class BoardTowerCounts001
    {
        public string packageId;
        public int items;
        public int itemFamilies;
        public int rooms;
        public int roomTypes;
        public int runEffects;
        public int boons;
        public int burdens;
        public int curses;
        public int turningPoints;
        public int rewardHooks;
        public int p0Items;
        public int p0Rooms;
        public int p0Effects;
        public int p0TurningPoints;
        public string[] existingPreparationNamesExcluded;
    }

    /// <summary>
    /// Fail-closed runtime reader for the verified data-only enhancement pack.
    /// The catalog remains design authority: only adapters with complete numeric
    /// contracts may surface or execute an item/effect. P0 rooms are safe because
    /// they decorate existing committed board actions and never replace them.
    /// </summary>
    public sealed class BoardTowerEnhancementCatalog001
    {
        public const string BaseResourcePath001 = "SecondDimension/BoardTower001/Data/";
        public const int ExpectedItemCount001 = 144;
        public const int ExpectedRoomCount001 = 72;
        public const int ExpectedEffectCount001 = 72;
        public const int ExpectedTurningPointCount001 = 24;
        public const int ExpectedHookCount001 = 30;
        public const int ExpectedP0Count001 = 76;

        private static BoardTowerEnhancementCatalog001 _cached;

        private readonly IReadOnlyDictionary<string, BoardTowerItem001> _items;
        private readonly IReadOnlyDictionary<string, BoardTowerRoom001> _rooms;
        private readonly IReadOnlyDictionary<string, BoardTowerRunEffect001> _effects;
        private readonly IReadOnlyDictionary<string, BoardTowerTurningPoint001> _turningPoints;
        private readonly IReadOnlyDictionary<string, BoardTowerRewardHook001> _hooks;
        private readonly IReadOnlyList<BoardTowerP0Reference001> _p0;

        private BoardTowerEnhancementCatalog001(
            IReadOnlyDictionary<string, BoardTowerItem001> items,
            IReadOnlyDictionary<string, BoardTowerRoom001> rooms,
            IReadOnlyDictionary<string, BoardTowerRunEffect001> effects,
            IReadOnlyDictionary<string, BoardTowerTurningPoint001> turningPoints,
            IReadOnlyDictionary<string, BoardTowerRewardHook001> hooks,
            IReadOnlyList<BoardTowerP0Reference001> p0,
            BoardTowerCounts001 counts)
        {
            _items = items;
            _rooms = rooms;
            _effects = effects;
            _turningPoints = turningPoints;
            _hooks = hooks;
            _p0 = p0;
            Counts = counts;
        }

        public IReadOnlyDictionary<string, BoardTowerItem001> Items => _items;
        public IReadOnlyDictionary<string, BoardTowerRoom001> Rooms => _rooms;
        public IReadOnlyDictionary<string, BoardTowerRunEffect001> Effects => _effects;
        public IReadOnlyDictionary<string, BoardTowerTurningPoint001> TurningPoints => _turningPoints;
        public IReadOnlyDictionary<string, BoardTowerRewardHook001> Hooks => _hooks;
        public IReadOnlyList<BoardTowerP0Reference001> P0 => _p0;
        public BoardTowerCounts001 Counts { get; }

        public static BoardTowerEnhancementCatalog001 LoadFromResources()
        {
            if (_cached != null) return _cached;
            _cached = ParseAndValidate(
                LoadText("BOARD_TOWER_ITEM_CATALOG_144_001"),
                LoadText("ROOM_MODULE_CATALOG_72_001"),
                LoadText("RUN_EFFECT_CATALOG_72_001"),
                LoadText("TURNING_POINT_SEEDS_24_001"),
                LoadText("NATURAL_REWARD_HOOKS_30_001"),
                LoadText("P0_FIRST_HOUR_AND_TOWER_SUBSET_001"),
                LoadText("CONTENT_COUNTS_AND_VALIDATION_001"));
            return _cached;
        }

        public static BoardTowerEnhancementCatalog001 ParseAndValidate(
            string itemJson,
            string roomJson,
            string effectJson,
            string turningPointJson,
            string hookJson,
            string p0Json,
            string countsJson)
        {
            try
            {
                var items = ParseArray<BoardTowerItem001>(itemJson, "items");
                var rooms = ParseArray<BoardTowerRoom001>(roomJson, "rooms");
                var effects = ParseArray<BoardTowerRunEffect001>(effectJson, "effects");
                var turningPoints = ParseArray<BoardTowerTurningPoint001>(turningPointJson, "turning points");
                var hooks = ParseArray<BoardTowerRewardHook001>(hookJson, "reward hooks");
                var p0 = ParseArray<BoardTowerP0Reference001>(p0Json, "P0 references");
                var counts = JsonConvert.DeserializeObject<BoardTowerCounts001>(countsJson);
                if (counts == null) throw Invalid("counts file did not parse");

                var itemMap = Index(items, value => value.itemId, "item", "BTE001_");
                var roomMap = Index(rooms, value => value.roomId, "room", "BTR001_");
                var effectMap = Index(effects, value => value.effectId, "effect", "BTEFF001_");
                var turningPointMap = Index(turningPoints, value => value.turningPointId, "turning point", "BTP001_");
                var hookMap = Index(hooks, value => value.hookId, "hook", "HOOK_BTE001_");

                Require(counts.packageId == BoardTowerEnhancementRules001.PackageId001,
                    "package ID mismatch");
                Require(items.Length == ExpectedItemCount001 && counts.items == items.Length,
                    "item count mismatch");
                Require(rooms.Length == ExpectedRoomCount001 && counts.rooms == rooms.Length,
                    "room count mismatch");
                Require(effects.Length == ExpectedEffectCount001 && counts.runEffects == effects.Length,
                    "effect count mismatch");
                Require(turningPoints.Length == ExpectedTurningPointCount001 &&
                        counts.turningPoints == turningPoints.Length,
                    "turning-point count mismatch");
                Require(hooks.Length == ExpectedHookCount001 && counts.rewardHooks == hooks.Length,
                    "reward-hook count mismatch");
                Require(p0.Length == ExpectedP0Count001, "P0 reference count mismatch");
                Require(items.Select(value => value.familyId).Distinct(StringComparer.Ordinal).Count() ==
                        counts.itemFamilies && counts.itemFamilies == 12,
                    "item-family count mismatch");
                Require(rooms.Select(value => value.roomType).Distinct(StringComparer.Ordinal).Count() ==
                        counts.roomTypes && counts.roomTypes == 12,
                    "room-type count mismatch");
                Require(effects.Count(value => value.effectType == "BOON") == counts.boons && counts.boons == 36,
                    "boon count mismatch");
                Require(effects.Count(value => value.effectType == "BURDEN") == counts.burdens && counts.burdens == 24,
                    "burden count mismatch");
                Require(effects.Count(value => value.effectType == "CURSE") == counts.curses && counts.curses == 12,
                    "curse count mismatch");

                ValidateP0(p0, itemMap, roomMap, effectMap, turningPointMap, counts);
                ValidateDisplayNames(items, rooms, effects, turningPoints);
                ValidateExcludedPreparationNames(items, counts.existingPreparationNamesExcluded);
                Require(hooks.All(value => !string.IsNullOrWhiteSpace(value.antiExploit) &&
                                           value.antiExploit.IndexOf("exact-once", StringComparison.OrdinalIgnoreCase) >= 0),
                    "reward hook missing exact-once law");
                Require(rooms.Where(value => value.canCarryStoryAnchor)
                        .All(value => value.roomType == "STORY"),
                    "non-story room may carry a story anchor");

                return new BoardTowerEnhancementCatalog001(
                    itemMap, roomMap, effectMap, turningPointMap, hookMap,
                    Array.AsReadOnly(p0.OrderBy(value => value.contentType, StringComparer.Ordinal)
                        .ThenBy(value => value.integrationOrder).ToArray()), counts);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException(
                    "BOARD_TOWER_ENHANCEMENT_PACK_001 JSON could not be parsed.", exception);
            }
        }

        public BoardTowerRoom001 CommittedBoardRoom001(
            string expeditionId,
            string nodeId,
            string boardRoomKind,
            IReadOnlyList<string> objectiveFlags)
        {
            var roomId = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                expeditionId, nodeId, boardRoomKind);
            if (!BoardTowerEnhancementRules001.IsRoomModuleCommitted001(
                    objectiveFlags, nodeId, roomId)) return null;
            return _rooms.TryGetValue(roomId, out var room) ? room : null;
        }

        public BoardTowerRoom001 DeterministicTowerRoom001(string committedRunId, int floorNumber)
        {
            if (string.IsNullOrWhiteSpace(committedRunId)) return null;
            var roomId = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                committedRunId, "FLOOR_" + Math.Max(1, floorNumber), "TOWER");
            return _rooms.TryGetValue(roomId, out var room) ? room : null;
        }

        private static void ValidateP0(
            IReadOnlyList<BoardTowerP0Reference001> p0,
            IReadOnlyDictionary<string, BoardTowerItem001> items,
            IReadOnlyDictionary<string, BoardTowerRoom001> rooms,
            IReadOnlyDictionary<string, BoardTowerRunEffect001> effects,
            IReadOnlyDictionary<string, BoardTowerTurningPoint001> turningPoints,
            BoardTowerCounts001 counts)
        {
            Require(p0.Select(value => value.contentType + "|" + value.stableId)
                    .Distinct(StringComparer.Ordinal).Count() == p0.Count,
                "duplicate P0 reference");
            ValidateP0Type("ITEM", counts.p0Items, p0, items,
                value => value.firstHourPriority, 36);
            ValidateP0Type("ROOM", counts.p0Rooms, p0, rooms,
                value => value.firstHourPriority, 18);
            ValidateP0Type("RUN_EFFECT", counts.p0Effects, p0, effects,
                value => value.firstHourPriority, 14);
            ValidateP0Type("TURNING_POINT", counts.p0TurningPoints, p0, turningPoints,
                value => value.firstHourPriority, 8);
            Require(p0.All(value => value.integrationOrder > 0),
                "P0 integration order must be positive within each content type");
        }

        private static void ValidateP0Type<T>(
            string type,
            int declaredCount,
            IReadOnlyList<BoardTowerP0Reference001> p0,
            IReadOnlyDictionary<string, T> values,
            Func<T, string> priority,
            int expectedCount)
        {
            var refs = p0.Where(value => value.contentType == type).ToArray();
            Require(refs.Length == declaredCount && refs.Length == expectedCount,
                type + " P0 count mismatch");
            Require(refs.All(value => values.ContainsKey(value.stableId)),
                type + " P0 reference is unresolved");
            Require(refs.All(value => priority(values[value.stableId]) == "P0"),
                type + " P0 priority mismatch");
            Require(refs.Select(value => value.integrationOrder).Distinct().Count() == refs.Length &&
                    refs.Min(value => value.integrationOrder) == 1 &&
                    refs.Max(value => value.integrationOrder) == refs.Length,
                type + " integration order is not contiguous");
        }

        private static IReadOnlyDictionary<string, T> Index<T>(
            IEnumerable<T> values,
            Func<T, string> id,
            string label,
            string prefix)
        {
            var result = new Dictionary<string, T>(StringComparer.Ordinal);
            foreach (var value in values ?? Array.Empty<T>())
            {
                var key = id(value);
                if (string.IsNullOrWhiteSpace(key) || !key.StartsWith(prefix, StringComparison.Ordinal) ||
                    result.ContainsKey(key))
                    throw Invalid("invalid or duplicate " + label + " ID: " + (key ?? "<null>"));
                result.Add(key, value);
            }
            return result;
        }

        private static T[] ParseArray<T>(string json, string label)
        {
            if (string.IsNullOrWhiteSpace(json)) throw Invalid(label + " JSON is empty");
            return JsonConvert.DeserializeObject<T[]>(json) ??
                   throw Invalid(label + " JSON did not contain an array");
        }

        private static void ValidateDisplayNames(
            IEnumerable<BoardTowerItem001> items,
            IEnumerable<BoardTowerRoom001> rooms,
            IEnumerable<BoardTowerRunEffect001> effects,
            IEnumerable<BoardTowerTurningPoint001> turningPoints)
        {
            var names = items.Select(value => value.displayName)
                .Concat(rooms.Select(value => value.displayName))
                .Concat(effects.Select(value => value.displayName))
                .Concat(turningPoints.Select(value => value.displayName))
                .ToArray();
            Require(names.All(value => !string.IsNullOrWhiteSpace(value)),
                "blank player-facing display name");
            Require(names.Distinct(StringComparer.Ordinal).Count() == names.Length,
                "duplicate player-facing display name");
        }

        private static void ValidateExcludedPreparationNames(
            IEnumerable<BoardTowerItem001> items,
            IReadOnlyList<string> excluded)
        {
            var existing = excluded ?? Array.Empty<string>();
            Require(existing.Count == 17 && existing.Distinct(StringComparer.Ordinal).Count() == 17,
                "existing preparation exclusion list mismatch");
            var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);
            Require(items.All(value => !existingSet.Contains(value.displayName)),
                "new item duplicates an existing preparation display name");
        }

        private static string LoadText(string name)
        {
            var asset = Resources.Load<TextAsset>(BaseResourcePath001 + name);
            if (asset == null)
                throw Invalid("missing runtime resource " + BaseResourcePath001 + name);
            return asset.text;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw Invalid(message);
        }

        private static InvalidOperationException Invalid(string message) =>
            new InvalidOperationException("BOARD_TOWER_ENHANCEMENT_PACK_001: " + message + ".");
    }
}
