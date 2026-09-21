using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.Progression070
{
    public enum RecruitTreeSlotKind070
    {
        Weapon,
        PrimaryRole,
        Mystic,
        SecondaryRole
    }

    public sealed class DeepTreeDefinition070
    {
        internal DeepTreeDefinition070(
            string treeId,
            string displayName,
            string category,
            string discipline,
            string weaponFamilyId,
            IReadOnlyList<string> nodeIds)
        {
            TreeId = treeId;
            DisplayName = displayName;
            Category = category;
            Discipline = discipline;
            WeaponFamilyId = weaponFamilyId;
            NodeIds = nodeIds;
        }

        public string TreeId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string Discipline { get; }
        public string WeaponFamilyId { get; }
        public IReadOnlyList<string> NodeIds { get; }
        public string RootNodeId => NodeIds[0];
    }

    public sealed class DeepNodeDefinition070
    {
        internal DeepNodeDefinition070(
            string nodeId,
            string treeId,
            int index,
            int tier,
            string displayName,
            string nodeType,
            string discipline,
            string description,
            int sharedApCost,
            int personalMpCost,
            string targetRule,
            IReadOnlyList<string> forecastIntentTags,
            IReadOnlyList<string> effectTypes,
            string meaningfulUseDefinition,
            IReadOnlyList<string> prerequisiteNodeIds,
            int suggestedCharacterLevel,
            int discoveryMeaningfulUsePoints,
            IReadOnlyList<string> requiredEquipmentTagsAny,
            IReadOnlyList<string> animationTags,
            int powerCoefficientPermille,
            IReadOnlyList<string> legacyArtIds = null)
        {
            NodeId = nodeId;
            TreeId = treeId;
            Index = index;
            Tier = tier;
            DisplayName = displayName;
            NodeType = nodeType;
            Discipline = discipline;
            Description = description;
            SharedApCost = sharedApCost;
            PersonalMpCost = personalMpCost;
            TargetRule = targetRule;
            ForecastIntentTags = forecastIntentTags;
            EffectTypes = effectTypes;
            MeaningfulUseDefinition = meaningfulUseDefinition;
            PrerequisiteNodeIds = prerequisiteNodeIds;
            SuggestedCharacterLevel = suggestedCharacterLevel;
            DiscoveryMeaningfulUsePoints = discoveryMeaningfulUsePoints;
            RequiredEquipmentTagsAny = requiredEquipmentTagsAny;
            AnimationTags = animationTags;
            PowerCoefficientPermille = powerCoefficientPermille;
            LegacyArtIds = legacyArtIds ?? Array.Empty<string>();
        }

        public string NodeId { get; }
        public string TreeId { get; }
        public int Index { get; }
        public int Tier { get; }
        public string DisplayName { get; }
        public string NodeType { get; }
        public string Discipline { get; }
        public string Description { get; }
        public int SharedApCost { get; }
        public int PersonalMpCost { get; }
        public string TargetRule { get; }
        public IReadOnlyList<string> ForecastIntentTags { get; }
        public IReadOnlyList<string> EffectTypes { get; }
        public string MeaningfulUseDefinition { get; }
        public IReadOnlyList<string> PrerequisiteNodeIds { get; }
        public int SuggestedCharacterLevel { get; }
        public int DiscoveryMeaningfulUsePoints { get; }
        public IReadOnlyList<string> RequiredEquipmentTagsAny { get; }
        public IReadOnlyList<string> AnimationTags { get; }
        public int PowerCoefficientPermille { get; }
        public IReadOnlyList<string> LegacyArtIds { get; }
    }

    /// <summary>
    /// Runtime-facing Art authority. The 240 weapon and Mystic entries are copied from
    /// Battle Arts 009. The 120 role entries are deterministic fallbacks derived from
    /// their complete skill-node authority until authored battle profiles replace them.
    /// </summary>
    public sealed class RuntimeArtDefinition070
    {
        internal RuntimeArtDefinition070(
            DeepNodeDefinition070 node,
            string artClass,
            string resolutionSummary,
            bool derivedRoleFallback, int maximumTargets095 = 1,
            int damageBudgetCoefficient095 = -1, int cohesionBudgetCoefficient095 = -1,
            int formationBudgetCoefficient095 = -1)
        {
            NodeId = node.NodeId;
            TreeId = node.TreeId;
            DisplayName = node.DisplayName;
            ArtClass = artClass;
            NodeType = node.NodeType;
            Tier = node.Tier;
            SharedApCost = node.SharedApCost;
            PersonalMpCost = node.PersonalMpCost;
            TargetRule = node.TargetRule;
            ForecastIntentTags = node.ForecastIntentTags;
            EffectTypes = node.EffectTypes;
            MeaningfulUseDefinition = node.MeaningfulUseDefinition;
            ResolutionSummary = resolutionSummary;
            IsDerivedRoleFallback = derivedRoleFallback;
            Discipline = node.Discipline;
            RequiredEquipmentTagsAny = node.RequiredEquipmentTagsAny;
            AnimationTags = node.AnimationTags;
            PowerCoefficientPermille = node.PowerCoefficientPermille;
            LegacyArtIds = node.LegacyArtIds;
            MaximumTargets095 = maximumTargets095;
            DamageBudgetCoefficient095 = damageBudgetCoefficient095;
            CohesionBudgetCoefficient095 = cohesionBudgetCoefficient095;
            FormationBudgetCoefficient095 = formationBudgetCoefficient095;
        }

        public string NodeId { get; }
        public string TreeId { get; }
        public string DisplayName { get; }
        public string ArtClass { get; }
        public string NodeType { get; }
        public int Tier { get; }
        public int SharedApCost { get; }
        public int PersonalMpCost { get; }
        public string TargetRule { get; }
        public IReadOnlyList<string> ForecastIntentTags { get; }
        public IReadOnlyList<string> EffectTypes { get; }
        public string MeaningfulUseDefinition { get; }
        public string ResolutionSummary { get; }
        public bool IsDerivedRoleFallback { get; }
        public string Discipline { get; }
        public IReadOnlyList<string> RequiredEquipmentTagsAny { get; }
        public IReadOnlyList<string> AnimationTags { get; }
        public int PowerCoefficientPermille { get; }
        public IReadOnlyList<string> LegacyArtIds { get; }
        public int MaximumTargets095 { get; }
        public int DamageBudgetCoefficient095 { get; }
        public int CohesionBudgetCoefficient095 { get; }
        public int FormationBudgetCoefficient095 { get; }
    }

    public sealed class RecruitTreeSlotDefinition070
    {
        internal RecruitTreeSlotDefinition070(
            RecruitTreeSlotKind070 slotKind,
            string treeId,
            bool initiallyUnlocked,
            bool earnable,
            bool permanentlyFixed)
        {
            SlotKind = slotKind;
            TreeId = treeId;
            InitiallyUnlocked = initiallyUnlocked;
            Earnable = earnable;
            PermanentlyFixed = permanentlyFixed;
        }

        public RecruitTreeSlotKind070 SlotKind { get; }
        public string TreeId { get; }
        public bool InitiallyUnlocked { get; }
        public bool Earnable { get; }
        public bool PermanentlyFixed { get; }
    }

    public sealed class RecruitTreePlan070
    {
        internal RecruitTreePlan070(
            string signatureRecruitId,
            string stableRecruitId,
            string displayName,
            string fixedWeaponFamilyId,
            IReadOnlyList<RecruitTreeSlotDefinition070> slots,
            IReadOnlyList<string> authoredStartingNodeIds,
            IReadOnlyList<string> normalizedStartingNodeIds,
            IReadOnlyList<string> dormantStartingNodeIds,
            IReadOnlyList<string> rejectedStartingNodeIds)
        {
            SignatureRecruitId = signatureRecruitId;
            StableRecruitId = stableRecruitId;
            DisplayName = displayName;
            FixedWeaponFamilyId = fixedWeaponFamilyId;
            Slots = slots;
            AuthoredStartingNodeIds = authoredStartingNodeIds;
            NormalizedStartingNodeIds = normalizedStartingNodeIds;
            DormantStartingNodeIds = dormantStartingNodeIds;
            RejectedStartingNodeIds = rejectedStartingNodeIds;
        }

        public string SignatureRecruitId { get; }
        public string StableRecruitId { get; }
        public string DisplayName { get; }
        public string FixedWeaponFamilyId { get; }
        public IReadOnlyList<RecruitTreeSlotDefinition070> Slots { get; }
        public IReadOnlyList<string> AuthoredStartingNodeIds { get; }
        public IReadOnlyList<string> NormalizedStartingNodeIds { get; }
        public IReadOnlyList<string> DormantStartingNodeIds { get; }
        public IReadOnlyList<string> RejectedStartingNodeIds { get; }

        public RecruitTreeSlotDefinition070 Slot(RecruitTreeSlotKind070 slotKind) =>
            Slots.Single(value => value.SlotKind == slotKind);

        public bool ContainsTree(string treeId) =>
            Slots.Any(value => StringComparer.Ordinal.Equals(value.TreeId, treeId));
    }

    /// <summary>
    /// Validates and joins the existing 300-recruit, 30-tree, 360-node, 264-weapon
    /// progression authorities into one immutable runtime catalog.
    /// </summary>
    public sealed class DeepProgressionCatalog070
    {
        public const string ContentVersion = "DEEP_PROGRESSION_070_1.0";
        public const int ExpectedRecruitCount = 300;
        public const int ExpectedTreeCount = 30;
        public const int ExpectedNodeCount = 360;
        public const int ExpectedWeaponCount = 264;
        public const int ExpectedBindingCount = 264;
        public const int ExpectedAuthoredArtProfileCount = 240;
        public const int ExpectedDerivedRoleArtCount = 120;

        private readonly Dictionary<string, DeepTreeDefinition070> _trees;
        private readonly Dictionary<string, DeepNodeDefinition070> _nodes;
        private readonly Dictionary<string, RuntimeArtDefinition070> _runtimeArts;
        private readonly Dictionary<string, RecruitTreePlan070> _plansBySignature;
        private readonly Dictionary<string, RecruitTreePlan070> _plansByStableRecruit;
        private readonly IReadOnlyList<RuntimeArtDefinition070> _runtimeArtList;

        private DeepProgressionCatalog070(
            Dictionary<string, DeepTreeDefinition070> trees,
            Dictionary<string, DeepNodeDefinition070> nodes,
            Dictionary<string, RuntimeArtDefinition070> runtimeArts,
            Dictionary<string, RecruitTreePlan070> plansBySignature,
            Dictionary<string, RecruitTreePlan070> plansByStableRecruit)
        {
            _trees = trees;
            _nodes = nodes;
            _runtimeArts = runtimeArts;
            _plansBySignature = plansBySignature;
            _plansByStableRecruit = plansByStableRecruit;
            _runtimeArtList = runtimeArts.Values.OrderBy(value => value.NodeId, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        public int SignatureRecruitCount => _plansBySignature.Count;
        public int TreeCount => _trees.Count;
        public int NodeCount => _nodes.Count;
        public int WeaponCount => ExpectedWeaponCount;
        public int WeaponArtBindingCount => ExpectedBindingCount;
        public int AuthoredArtProfileCount => ExpectedAuthoredArtProfileCount;
        public int RuntimeArtCount => _runtimeArts.Count;
        public int DerivedRoleArtCount => _runtimeArts.Values.Count(value => value.IsDerivedRoleFallback);
        public IReadOnlyList<RuntimeArtDefinition070> RuntimeArts => _runtimeArtList;

        public static DeepProgressionCatalog070 LoadFromContentRoot(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot))
                throw new ArgumentException("Content root is required.", nameof(contentRoot));

            var signatureDocument = Parse(contentRoot, "CONTENT_AUTHORITY_002", "DATA", "SIGNATURE_RECRUITS_300.json");
            var treeDocument = Parse(contentRoot, "CONTENT_AUTHORITY_002", "DATA", "SKILL_TREES_30.json");
            var nodeDocument = Parse(contentRoot, "CONTENT_AUTHORITY_002", "DATA", "SKILL_NODES_360.json");
            var weaponDocument = Parse(contentRoot, "CONTENT_AUTHORITY_002", "DATA", "WEAPON_CATALOG_264.json");
            var bindingDocument = Parse(contentRoot, "CONTENT_AUTHORITY_003_BRIDGE", "DATA", "WEAPON_ART_BINDINGS_264.json");
            var manifestDocument = Parse(contentRoot, "CONTENT_AUTHORITY_003_BRIDGE", "DATA", "CHARACTER_PROGRESSION_MANIFESTS_300.json");
            var artDocument = Parse(contentRoot, "BATTLE_ARTS_009", "DATA", "BATTLE_ART_PROFILES_240.json");

            RequireVersion(signatureDocument, "GOW_CONTENT_AUTHORITY_002", "signature recruits");
            RequireVersion(treeDocument, "GOW_CONTENT_AUTHORITY_002", "skill trees");
            RequireVersion(nodeDocument, "GOW_CONTENT_AUTHORITY_002", "skill nodes");
            RequireVersion(weaponDocument, "GOW_CONTENT_AUTHORITY_002", "weapons");
            RequireVersion(bindingDocument, "GOW_CONTENT_AUTHORITY_BRIDGE_003", "weapon Art bindings");
            RequireVersion(manifestDocument, "GOW_CONTENT_AUTHORITY_BRIDGE_003", "character progression manifests");
            RequireVersion(artDocument, "BATTLE_ARTS_009", "battle Art profiles");

            var signatures = Index(signatureDocument, "signatureRecruits", "signatureId");
            var treeTokens = Index(treeDocument, "skillTrees", "id");
            var nodeTokens = Index(nodeDocument, "skillNodes", "id");
            var weapons = Index(weaponDocument, "weapons", "id");
            var bindings = Index(bindingDocument, "weaponArtBindings", "weaponId");
            var manifests = Index(manifestDocument, "characterProgressionManifests", "signatureRecruitId");
            var authoredProfiles = Index(artDocument, "profiles", "stableNodeId");

            RequireCount(signatures, ExpectedRecruitCount, "signature recruits");
            RequireCount(treeTokens, ExpectedTreeCount, "skill trees");
            RequireCount(nodeTokens, ExpectedNodeCount, "skill nodes");
            RequireCount(weapons, ExpectedWeaponCount, "weapons");
            RequireCount(bindings, ExpectedBindingCount, "weapon Art bindings");
            RequireCount(manifests, ExpectedRecruitCount, "character progression manifests");
            RequireCount(authoredProfiles, ExpectedAuthoredArtProfileCount, "authored battle Art profiles");
            if (artDocument["profileCount"]?.Value<int>() != ExpectedAuthoredArtProfileCount)
                throw Invalid("Battle Art profileCount does not match the profiles array.");

            var trees = BuildTrees(treeTokens);
            var nodes = BuildNodes(nodeTokens, trees);
            ValidateTreeTopology(trees, nodes);
            ValidateWeaponsAndBindings(weapons, bindings, trees, nodes);
            var runtimeArts = BuildRuntimeArts(authoredProfiles, trees, nodes);
            var plansBySignature = BuildRecruitPlans(signatures, manifests, trees, nodes);
            var plansByStable = plansBySignature.Values.ToDictionary(
                value => value.StableRecruitId,
                value => value,
                StringComparer.Ordinal);

            return new DeepProgressionCatalog070(trees, nodes, runtimeArts, plansBySignature, plansByStable);
        }

        public DeepTreeDefinition070 Tree(string treeId) => Required(_trees, treeId, "tree");
        public DeepNodeDefinition070 Node(string nodeId) => Required(_nodes, nodeId, "node");
        public RuntimeArtDefinition070 RuntimeArt(string nodeId) => Required(_runtimeArts, nodeId, "runtime Art");

        public bool TryTree(string treeId, out DeepTreeDefinition070 tree) =>
            _trees.TryGetValue(treeId ?? string.Empty, out tree);

        public bool TryNode(string nodeId, out DeepNodeDefinition070 node) =>
            _nodes.TryGetValue(nodeId ?? string.Empty, out node);

        public RecruitTreePlan070 RecruitPlan(string signatureOrStableRecruitId)
        {
            if (string.IsNullOrWhiteSpace(signatureOrStableRecruitId))
                throw new ArgumentException("Signature or stable recruit ID is required.", nameof(signatureOrStableRecruitId));
            if (_plansBySignature.TryGetValue(signatureOrStableRecruitId, out var bySignature)) return bySignature;
            if (_plansByStableRecruit.TryGetValue(signatureOrStableRecruitId, out var byStable)) return byStable;
            throw new KeyNotFoundException("Unknown Signature Recruit ID: " + signatureOrStableRecruitId);
        }

        private static Dictionary<string, DeepTreeDefinition070> BuildTrees(
            IReadOnlyDictionary<string, JObject> treeTokens)
        {
            var categoryCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var result = new Dictionary<string, DeepTreeDefinition070>(StringComparer.Ordinal);
            foreach (var pair in treeTokens)
            {
                var value = pair.Value;
                var category = RequiredString(value, "category", "tree " + pair.Key).ToUpperInvariant();
                if (category != "WEAPON" && category != "MYSTIC" && category != "ROLE")
                    throw Invalid("Unknown category on tree " + pair.Key + ": " + category);
                categoryCounts[category] = categoryCounts.TryGetValue(category, out var count) ? count + 1 : 1;
                var nodeIds = StringArray(value, "nodeIds", "tree " + pair.Key);
                if (nodeIds.Count != 12 || value["nodeCount"]?.Value<int>() != 12)
                    throw Invalid("Every skill tree must declare exactly 12 nodes: " + pair.Key);
                EnsureUnique(nodeIds, "node IDs on tree " + pair.Key);
                result.Add(pair.Key, new DeepTreeDefinition070(
                    pair.Key,
                    RequiredString(value, "displayName", "tree " + pair.Key),
                    category,
                    RequiredString(value, "discipline", "tree " + pair.Key),
                    value["weaponFamilyId"]?.Value<string>() ?? string.Empty,
                    nodeIds));
            }
            RequireCategoryCount(categoryCounts, "WEAPON", 12);
            RequireCategoryCount(categoryCounts, "MYSTIC", 8);
            RequireCategoryCount(categoryCounts, "ROLE", 10);
            return result;
        }

        private static Dictionary<string, DeepNodeDefinition070> BuildNodes(
            IReadOnlyDictionary<string, JObject> nodeTokens,
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees)
        {
            var result = new Dictionary<string, DeepNodeDefinition070>(StringComparer.Ordinal);
            foreach (var pair in nodeTokens)
            {
                var value = pair.Value;
                var treeId = RequiredString(value, "treeId", "node " + pair.Key);
                Required(trees, treeId, "tree");
                var index = value["index"]?.Value<int>() ?? 0;
                var tier = value["tier"]?.Value<int>() ?? 0;
                if (index < 1 || index > 12 || tier < 1)
                    throw Invalid("Node index/tier is invalid: " + pair.Key);
                result.Add(pair.Key, new DeepNodeDefinition070(
                    pair.Key,
                    treeId,
                    index,
                    tier,
                    RequiredString(value, "displayName", "node " + pair.Key),
                    RequiredString(value, "nodeType", "node " + pair.Key),
                    RequiredString(value, "discipline", "node " + pair.Key),
                    RequiredString(value, "description", "node " + pair.Key),
                    NonNegativeInt(value, "apCost", "node " + pair.Key),
                    NonNegativeInt(value, "personalMpCost", "node " + pair.Key),
                    RequiredString(value, "targetRule", "node " + pair.Key),
                    StringArray(value, "forecastIntentTags", "node " + pair.Key),
                    ObjectStringValues(value, "effects", "type", "node " + pair.Key),
                    RequiredString(value, "meaningfulUseDefinition", "node " + pair.Key),
                    StringArray(value, "prerequisiteNodeIds", "node " + pair.Key),
                    PositiveInt(value, "suggestedCharacterLevel", "node " + pair.Key),
                    NonNegativeInt(value, "discoveryMeaningfulUsePoints", "node " + pair.Key),
                    StringArray(value, "requiredEquipmentTagsAny", "node " + pair.Key),
                    StringArray(value, "animationTags", "node " + pair.Key),
                    EffectPowerPermille(value, "node " + pair.Key),
                    StringArray(value, "legacyArtIds", "node " + pair.Key)));
            }
            return result;
        }

        private static void ValidateTreeTopology(
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes)
        {
            foreach (var tree in trees.Values)
            {
                var actual = nodes.Values.Where(value => StringComparer.Ordinal.Equals(value.TreeId, tree.TreeId)).ToList();
                if (actual.Count != 12)
                    throw Invalid("Tree does not own exactly 12 nodes: " + tree.TreeId);
                var indexes = actual.Select(value => value.Index).OrderBy(value => value).ToArray();
                if (!indexes.SequenceEqual(Enumerable.Range(1, 12)))
                    throw Invalid("Tree node indexes must be exactly 1 through 12: " + tree.TreeId);
                foreach (var nodeId in tree.NodeIds)
                {
                    var node = Required(nodes, nodeId, "node");
                    if (!StringComparer.Ordinal.Equals(node.TreeId, tree.TreeId))
                        throw Invalid("Tree node list crosses tree authority: " + tree.TreeId + " -> " + nodeId);
                }
                if (!tree.NodeIds.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                        actual.Select(value => value.NodeId).OrderBy(value => value, StringComparer.Ordinal)))
                    throw Invalid("Tree node list does not exactly cover its owned nodes: " + tree.TreeId);
            }
        }

        private static void ValidateWeaponsAndBindings(
            IReadOnlyDictionary<string, JObject> weapons,
            IReadOnlyDictionary<string, JObject> bindings,
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes)
        {
            if (!weapons.Keys.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                    bindings.Keys.OrderBy(value => value, StringComparer.Ordinal)))
                throw Invalid("Every weapon must have exactly one same-ID weapon Art binding.");

            foreach (var pair in weapons)
            {
                var weaponId = pair.Key;
                var weapon = pair.Value;
                var familyId = RequiredString(weapon, "weaponFamilyId", "weapon " + weaponId);
                var authority = weapon["contentAuthority002"] as JObject ??
                    throw Invalid("Weapon is missing contentAuthority002: " + weaponId);
                var treeId = RequiredString(authority, "artTreeId", "weapon " + weaponId);
                var tree = Required(trees, treeId, "weapon tree");
                if (tree.Category != "WEAPON" || !StringComparer.Ordinal.Equals(tree.WeaponFamilyId, familyId))
                    throw Invalid("Weapon family/tree mismatch: " + weaponId);

                var binding = bindings[weaponId];
                if (!StringComparer.Ordinal.Equals(binding["weaponFamilyId"]?.Value<string>(), familyId) ||
                    !StringComparer.Ordinal.Equals(binding["artTreeId"]?.Value<string>(), treeId))
                    throw Invalid("Weapon binding disagrees with weapon authority: " + weaponId);
                ValidateBindingNodes(binding, "discoverableNodeIdsWhileEquipped", treeId, nodes, weaponId);
                ValidateBindingNodes(binding, "usableLearnedNodeIdsWhileEquipped", treeId, nodes, weaponId);
                ValidateBindingNodes(binding, "branchBiasedNodeIds", treeId, nodes, weaponId);
            }
        }

        private static Dictionary<string, RuntimeArtDefinition070> BuildRuntimeArts(
            IReadOnlyDictionary<string, JObject> authoredProfiles,
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes)
        {
            var nonRoleNodeIds = nodes.Values
                .Where(value => Required(trees, value.TreeId, "tree").Category != "ROLE")
                .Select(value => value.NodeId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (!nonRoleNodeIds.SequenceEqual(authoredProfiles.Keys.OrderBy(value => value, StringComparer.Ordinal)))
                throw Invalid("The 240 authored profiles must exactly cover all weapon and Mystic nodes.");

            var result = new Dictionary<string, RuntimeArtDefinition070>(StringComparer.Ordinal);
            foreach (var node in nodes.Values)
            {
                var tree = Required(trees, node.TreeId, "tree");
                if (tree.Category == "ROLE")
                {
                    result.Add(node.NodeId, new RuntimeArtDefinition070(
                        node,
                        "ROLE_ART",
                        node.Description,
                        true));
                    continue;
                }

                var profile = Required(authoredProfiles, node.NodeId, "authored Art profile");
                if (!StringComparer.Ordinal.Equals(profile["treeId"]?.Value<string>(), node.TreeId))
                    throw Invalid("Authored Art profile tree mismatch: " + node.NodeId);
                var profileAp = profile["sharedApCost"]?.Value<int>() ?? -1;
                var profileMp = profile["personalMpCost"]?.Value<int>() ?? -1;
                if (profileAp < 0 || profileMp < 0)
                    throw Invalid("Authored Art profile has invalid costs: " + node.NodeId);
                var profileNode = new DeepNodeDefinition070(
                    node.NodeId,
                    node.TreeId,
                    node.Index,
                    profile["tier"]?.Value<int>() ?? node.Tier,
                    RequiredString(profile, "displayName", "Art profile " + node.NodeId),
                    RequiredString(profile, "nodeType", "Art profile " + node.NodeId),
                    node.Discipline,
                    node.Description,
                    profileAp,
                    profileMp,
                    RequiredString(profile, "targetRule", "Art profile " + node.NodeId),
                    StringArray(profile, "forecastIntentTags", "Art profile " + node.NodeId),
                    node.EffectTypes,
                    profile["learning"]?["meaningfulUseDefinition"]?.Value<string>() ?? node.MeaningfulUseDefinition,
                    node.PrerequisiteNodeIds,
                    node.SuggestedCharacterLevel,
                    node.DiscoveryMeaningfulUsePoints,
                    node.RequiredEquipmentTagsAny,
                    node.AnimationTags,
                    node.PowerCoefficientPermille,
                    node.LegacyArtIds);
                result.Add(node.NodeId, new RuntimeArtDefinition070(
                    profileNode,
                    RequiredString(profile, "artClass", "Art profile " + node.NodeId),
                    RequiredString(profile, "resolutionSummary", "Art profile " + node.NodeId),
                    false,
                    profile["maximumTargets"]?.Value<int>() ?? -1,
                    profile["resolutionBudget"]?["damageCoefficientPermille"]?.Value<int>() ?? -1,
                    profile["resolutionBudget"]?["cohesionPowerPermille"]?.Value<int>() ?? -1,
                    profile["resolutionBudget"]?["breakPowerPermille"]?.Value<int>() ?? -1));
            }

            if (result.Count != ExpectedNodeCount || result.Values.Count(value => value.IsDerivedRoleFallback) != ExpectedDerivedRoleArtCount)
                throw Invalid("Runtime Art coverage must be 240 authored plus 120 derived role definitions.");
            return result;
        }

        private static Dictionary<string, RecruitTreePlan070> BuildRecruitPlans(
            IReadOnlyDictionary<string, JObject> signatures,
            IReadOnlyDictionary<string, JObject> manifests,
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes)
        {
            if (!signatures.Keys.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(
                    manifests.Keys.OrderBy(value => value, StringComparer.Ordinal)))
                throw Invalid("All 300 Signature Recruits must have one progression manifest.");

            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var result = new Dictionary<string, RecruitTreePlan070>(StringComparer.Ordinal);
            foreach (var pair in signatures)
            {
                var signatureId = pair.Key;
                var signature = pair.Value;
                var manifest = manifests[signatureId];
                var stableId = RequiredString(signature, "stableRecruitId", "signature " + signatureId);
                if (!stableIds.Add(stableId)) throw Invalid("Duplicate stable recruit ID: " + stableId);
                if (!StringComparer.Ordinal.Equals(manifest["stableRecruitId"]?.Value<string>(), stableId))
                    throw Invalid("Signature/manifest stable recruit ID mismatch: " + signatureId);

                var weaponTreeId = RequiredString(manifest, "weaponTreeId", "manifest " + signatureId);
                var mysticTreeId = RequiredString(manifest, "mysticTreeId", "manifest " + signatureId);
                var primaryRoleTreeId = RequiredString(manifest, "primaryRoleTreeId", "manifest " + signatureId);
                var secondaryRoleTreeId = RequiredString(manifest, "secondaryRoleTreeId", "manifest " + signatureId);
                RequireTreeCategory(trees, weaponTreeId, "WEAPON", signatureId);
                RequireTreeCategory(trees, mysticTreeId, "MYSTIC", signatureId);
                RequireTreeCategory(trees, primaryRoleTreeId, "ROLE", signatureId);
                RequireTreeCategory(trees, secondaryRoleTreeId, "ROLE", signatureId);
                if (StringComparer.Ordinal.Equals(primaryRoleTreeId, secondaryRoleTreeId))
                    throw Invalid("Primary and secondary role trees must differ: " + signatureId);

                var familyId = RequiredString(manifest, "fixedWeaponFamilyId", "manifest " + signatureId);
                if (!StringComparer.Ordinal.Equals(trees[weaponTreeId].WeaponFamilyId, familyId))
                    throw Invalid("Fixed weapon family does not match weapon tree: " + signatureId);

                var authoredStarting = StringArray(manifest, "startingUnlockedNodeIds", "manifest " + signatureId);
                EnsureUnique(authoredStarting, "starting nodes on manifest " + signatureId);
                foreach (var nodeId in authoredStarting) Required(nodes, nodeId, "starting node");
                ValidateStartingMastery(manifest, nodes, signatureId);

                var activeTrees = new HashSet<string>(new[] { weaponTreeId, primaryRoleTreeId }, StringComparer.Ordinal);
                var dormantTrees = new HashSet<string>(new[] { mysticTreeId, secondaryRoleTreeId }, StringComparer.Ordinal);
                var normalized = authoredStarting
                    .Where(nodeId => activeTrees.Contains(nodes[nodeId].TreeId))
                    .ToList();
                normalized.Add(trees[weaponTreeId].RootNodeId);
                normalized.Add(trees[primaryRoleTreeId].RootNodeId);
                normalized = UniqueSorted(normalized).ToList();
                var dormant = UniqueSorted(authoredStarting.Where(nodeId => dormantTrees.Contains(nodes[nodeId].TreeId)));
                var rejected = UniqueSorted(authoredStarting.Where(nodeId =>
                    !activeTrees.Contains(nodes[nodeId].TreeId) && !dormantTrees.Contains(nodes[nodeId].TreeId)));

                var slots = new List<RecruitTreeSlotDefinition070>
                {
                    new RecruitTreeSlotDefinition070(RecruitTreeSlotKind070.Weapon, weaponTreeId, true, false, true),
                    new RecruitTreeSlotDefinition070(RecruitTreeSlotKind070.PrimaryRole, primaryRoleTreeId, true, false, false),
                    new RecruitTreeSlotDefinition070(RecruitTreeSlotKind070.Mystic, mysticTreeId, false, true, false),
                    new RecruitTreeSlotDefinition070(RecruitTreeSlotKind070.SecondaryRole, secondaryRoleTreeId, false, true, false)
                };
                result.Add(signatureId, new RecruitTreePlan070(
                    signatureId,
                    stableId,
                    manifest["displayName"]?.Value<string>() ?? signature["name"]?["display"]?.Value<string>() ?? stableId,
                    familyId,
                    slots.AsReadOnly(),
                    authoredStarting,
                    normalized.AsReadOnly(),
                    dormant,
                    rejected));
            }
            return result;
        }

        private static void ValidateStartingMastery(
            JObject manifest,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes,
            string signatureId)
        {
            var entries = manifest["startingMastery"] as JArray ??
                throw Invalid("startingMastery must be an array: " + signatureId);
            var ids = new List<string>();
            foreach (var token in entries)
            {
                var value = token as JObject ?? throw Invalid("startingMastery entries must be objects: " + signatureId);
                var nodeId = RequiredString(value, "nodeId", "starting mastery " + signatureId);
                Required(nodes, nodeId, "starting mastery node");
                ids.Add(nodeId);
                if ((value["masteryRank"]?.Value<int>() ?? -1) < 0 || (value["meaningfulUses"]?.Value<int>() ?? -1) < 0)
                    throw Invalid("Starting mastery values cannot be negative: " + signatureId);
            }
            EnsureUnique(ids, "starting mastery node IDs on manifest " + signatureId);
        }

        private static void ValidateBindingNodes(
            JObject binding,
            string property,
            string expectedTreeId,
            IReadOnlyDictionary<string, DeepNodeDefinition070> nodes,
            string weaponId)
        {
            var ids = StringArray(binding, property, "binding " + weaponId);
            EnsureUnique(ids, property + " on binding " + weaponId);
            foreach (var id in ids)
            {
                var node = Required(nodes, id, "binding node");
                if (!StringComparer.Ordinal.Equals(node.TreeId, expectedTreeId))
                    throw Invalid("Weapon binding node crosses tree authority: " + weaponId + " -> " + id);
            }
        }

        private static JObject Parse(string root, params string[] parts)
        {
            var path = root;
            for (var index = 0; index < parts.Length; index++) path = Path.Combine(path, parts[index]);
            if (!File.Exists(path)) throw new FileNotFoundException("Required deep progression authority is missing.", path);
            return JObject.Parse(File.ReadAllText(path));
        }

        private static Dictionary<string, JObject> Index(JObject document, string arrayProperty, string idProperty)
        {
            var array = document[arrayProperty] as JArray ?? throw Invalid(arrayProperty + " must be an array.");
            var result = new Dictionary<string, JObject>(StringComparer.Ordinal);
            foreach (var token in array)
            {
                var value = token as JObject ?? throw Invalid(arrayProperty + " entries must be objects.");
                var id = value[idProperty]?.Value<string>();
                if (string.IsNullOrWhiteSpace(id) || result.ContainsKey(id))
                    throw Invalid("Missing or duplicate " + idProperty + " in " + arrayProperty + ".");
                result.Add(id, value);
            }
            return result;
        }

        private static IReadOnlyList<string> StringArray(JObject value, string property, string context)
        {
            var array = value[property] as JArray ?? throw Invalid(property + " must be an array on " + context + ".");
            var result = new List<string>();
            foreach (var token in array)
            {
                var text = token.Value<string>();
                if (string.IsNullOrWhiteSpace(text)) throw Invalid(property + " contains a blank ID on " + context + ".");
                result.Add(text);
            }
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> ObjectStringValues(
            JObject value,
            string arrayProperty,
            string property,
            string context)
        {
            var array = value[arrayProperty] as JArray ?? throw Invalid(arrayProperty + " must be an array on " + context + ".");
            var result = new List<string>();
            foreach (var token in array)
            {
                var item = token as JObject ?? throw Invalid(arrayProperty + " entries must be objects on " + context + ".");
                result.Add(RequiredString(item, property, context));
            }
            return result.AsReadOnly();
        }

        private static int EffectPowerPermille(JObject value, string context)
        {
            var effects = value["effects"] as JArray ?? throw Invalid("effects must be an array on " + context + ".");
            var maximum = 0;
            foreach (var token in effects)
            {
                var effect = token as JObject ?? throw Invalid("effects entries must be objects on " + context + ".");
                var coefficient = effect["powerCoefficient"]?.Value<decimal>();
                if (!coefficient.HasValue) continue;
                var permille = checked((int)Math.Round(coefficient.Value * 1000m, MidpointRounding.AwayFromZero));
                if (permille > maximum) maximum = permille;
            }
            return maximum > 0 ? maximum : 1000;
        }

        private static IReadOnlyList<string> UniqueSorted(IEnumerable<string> source) =>
            source.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();

        private static void EnsureUnique(IEnumerable<string> values, string context)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in values)
                if (!seen.Add(value)) throw Invalid("Duplicate ID in " + context + ": " + value);
        }

        private static string RequiredString(JObject value, string property, string context)
        {
            var result = value[property]?.Value<string>();
            return string.IsNullOrWhiteSpace(result)
                ? throw Invalid(property + " is required on " + context + ".")
                : result;
        }

        private static int NonNegativeInt(JObject value, string property, string context)
        {
            var result = value[property]?.Value<int>() ?? -1;
            if (result < 0) throw Invalid(property + " cannot be negative on " + context + ".");
            return result;
        }

        private static int PositiveInt(JObject value, string property, string context)
        {
            var result = value[property]?.Value<int>() ?? 0;
            if (result <= 0) throw Invalid(property + " must be positive on " + context + ".");
            return result;
        }

        private static T Required<T>(IReadOnlyDictionary<string, T> values, string id, string context)
        {
            if (string.IsNullOrWhiteSpace(id) || !values.TryGetValue(id, out var value))
                throw new KeyNotFoundException("Unknown " + context + " ID: " + (id ?? "<null>"));
            return value;
        }

        private static void RequireVersion(JObject document, string expected, string context)
        {
            if (!StringComparer.Ordinal.Equals(document["contentVersion"]?.Value<string>(), expected))
                throw Invalid("Content version mismatch for " + context + ".");
        }

        private static void RequireCount<T>(IReadOnlyDictionary<string, T> values, int expected, string context)
        {
            if (values.Count != expected)
                throw Invalid("Expected " + expected + " " + context + " but found " + values.Count + ".");
        }

        private static void RequireCategoryCount(
            IReadOnlyDictionary<string, int> counts,
            string category,
            int expected)
        {
            if (!counts.TryGetValue(category, out var count) || count != expected)
                throw Invalid("Expected " + expected + " " + category + " trees.");
        }

        private static void RequireTreeCategory(
            IReadOnlyDictionary<string, DeepTreeDefinition070> trees,
            string treeId,
            string expectedCategory,
            string signatureId)
        {
            var tree = Required(trees, treeId, "tree");
            if (!StringComparer.Ordinal.Equals(tree.Category, expectedCategory))
                throw Invalid("Recruit tree slot/category mismatch: " + signatureId + " -> " + treeId);
        }

        private static InvalidDataException Invalid(string message) => new InvalidDataException(message);
    }
}
