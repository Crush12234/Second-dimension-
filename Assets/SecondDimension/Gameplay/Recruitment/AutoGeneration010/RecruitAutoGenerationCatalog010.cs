using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.Recruitment.AutoGeneration010
{
    /// <summary>Explicit immutable authority loader. Generation performs no I/O.</summary>
    public sealed class RecruitAutoGenerationCatalog010
    {
        public const string ExpectedVersion = "RECRUIT_AUTOGEN_010_1.0";

        private readonly Dictionary<string,JObject> _classes;
        private readonly Dictionary<string,JObject> _trees;
        private readonly Dictionary<string,JObject> _nodes;
        private readonly Dictionary<string,JObject> _families;
        private readonly Dictionary<string,JObject> _artProfiles;
        private readonly Dictionary<string,JObject> _buildRecipes;
        private readonly Dictionary<string,JObject> _progressionBySignature;
        private readonly Dictionary<string,JObject> _signatureVisualBySignature;
        private readonly Dictionary<string,List<JObject>> _visualPiecesByLayer;

        private RecruitAutoGenerationCatalog010(
            JObject rules,
            JObject classes,
            JObject trees,
            JObject nodes,
            JObject families,
            JObject artProfiles,
            JObject buildRecipes,
            JObject progressions,
            JObject signatureVisuals,
            JObject visualPieces)
        {
            Rules = rules;
            _classes = Index(classes, "classes", "id");
            _trees = Index(trees, "skillTrees", "id");
            _nodes = Index(nodes, "skillNodes", "id");
            _families = Index(families, "weaponFamilies", "id");
            _artProfiles = Index(artProfiles, "profiles", "stableNodeId");
            _buildRecipes = Index(buildRecipes, "recipes", "id");
            _progressionBySignature = Index(progressions, "characterProgressionManifests", "signatureRecruitId");
            _signatureVisualBySignature = Index(signatureVisuals, "recipes", "signatureRecruitId");
            _visualPiecesByLayer = Group(visualPieces, "pieces", "layerId");

            if (!StringComparer.Ordinal.Equals(rules["contentVersion"]?.Value<string>(), ExpectedVersion))
                throw new InvalidDataException("Recruit auto-generation content version mismatch.");
            if (_classes.Count != 10 || _trees.Count != 30 || _nodes.Count != 360 || _families.Count != 12 || _artProfiles.Count != 240)
                throw new InvalidDataException("Inherited class/tree/node/family/Art counts do not match Battle Perfection 009 authority.");
            if (_progressionBySignature.Count != 300 || _signatureVisualBySignature.Count != 300)
                throw new InvalidDataException("All 300 Signature Recruits require progression and visual recipes.");
        }

        internal JObject Rules { get; }

        public static RecruitAutoGenerationCatalog010 LoadFromContentRoot(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot)) throw new ArgumentException("Content root is required.", nameof(contentRoot));
            string Read(params string[] parts) => File.ReadAllText(Path.Combine(Combine(contentRoot, parts)));
            JObject Parse(params string[] parts) => JObject.Parse(Read(parts));

            return new RecruitAutoGenerationCatalog010(
                Parse("RECRUIT_AUTOGEN_010", "DATA", "RECRUIT_AUTO_GENERATION_RULES_010.json"),
                Parse("CONTENT_AUTHORITY_002", "DATA", "CLASS_FOUNDATIONS_10.json"),
                Parse("CONTENT_AUTHORITY_002", "DATA", "SKILL_TREES_30.json"),
                Parse("CONTENT_AUTHORITY_002", "DATA", "SKILL_NODES_360.json"),
                Parse("CONTENT_AUTHORITY_002", "DATA", "WEAPON_FAMILIES_12_PRESERVED.json"),
                Parse("BATTLE_ARTS_009", "DATA", "BATTLE_ART_PROFILES_240.json"),
                Parse("RECRUIT_AUTOGEN_010", "DATA", "CLASS_WEAPON_BUILD_RECIPES_010.json"),
                Parse("CONTENT_AUTHORITY_003_BRIDGE", "DATA", "CHARACTER_PROGRESSION_MANIFESTS_300.json"),
                Parse("RECRUIT_AUTOGEN_010", "DATA", "SIGNATURE_VISUAL_RECIPES_300_010.json"),
                Parse("RECRUIT_AUTOGEN_010", "DATA", "PROCEDURAL_VISUAL_PIECE_CATALOG_010.json"));
        }

        internal JObject Class(string id) => Required(_classes, id, "class");
        internal JObject Tree(string id) => Required(_trees, id, "tree");
        internal JObject Node(string id) => Required(_nodes, id, "node");
        internal bool TryNode(string id, out JObject node) =>
            _nodes.TryGetValue(id ?? string.Empty, out node);
        internal JObject Family(string id) => Required(_families, id, "weapon family");
        internal JObject ArtProfile(string id) => _artProfiles.TryGetValue(id, out var value) ? value : null;
        internal JObject Progression(string signatureId) => _progressionBySignature.TryGetValue(signatureId ?? string.Empty, out var value) ? value : null;
        internal JObject SignatureVisual(string signatureId) => _signatureVisualBySignature.TryGetValue(signatureId ?? string.Empty, out var value) ? value : null;

        internal IReadOnlyList<JObject> BuildRecipesForClass(string classId)
        {
            var result = new List<JObject>();
            foreach (var pair in _buildRecipes)
                if (StringComparer.Ordinal.Equals(pair.Value["classId"]?.Value<string>(), classId)) result.Add(pair.Value);
            result.Sort((a,b) => StringComparer.Ordinal.Compare(a["id"].Value<string>(), b["id"].Value<string>()));
            return result.AsReadOnly();
        }

        internal IReadOnlyList<JObject> VisualPieces(string layerId, string raceId)
        {
            var result = new List<JObject>();
            if (!_visualPiecesByLayer.TryGetValue(layerId, out var values)) return result.AsReadOnly();
            foreach (var value in values)
            {
                var races = value["allowedRaceIds"] as JArray;
                if (races == null) continue;
                foreach (var token in races)
                {
                    if (StringComparer.Ordinal.Equals(token.Value<string>(), raceId))
                    {
                        result.Add(value);
                        break;
                    }
                }
            }
            result.Sort((a,b) => StringComparer.Ordinal.Compare(a["id"].Value<string>(), b["id"].Value<string>()));
            return result.AsReadOnly();
        }

        internal IReadOnlyList<JObject> AllowedTrees(string classId, string category)
        {
            var cls = Class(classId);
            var result = new List<JObject>();
            foreach (var token in (JArray)cls["allowedTreeIds"])
            {
                var tree = Tree(token.Value<string>());
                if (StringComparer.Ordinal.Equals(tree["category"]?.Value<string>(), category)) result.Add(tree);
            }
            result.Sort((a,b) => StringComparer.Ordinal.Compare(a["id"].Value<string>(), b["id"].Value<string>()));
            return result.AsReadOnly();
        }

        private static string Combine(string root, params string[] parts)
        {
            var path = root;
            for (var i = 0; i < parts.Length; i++) path = Path.Combine(path, parts[i]);
            return path;
        }

        private static Dictionary<string,JObject> Index(JObject document, string arrayProperty, string idProperty)
        {
            var result = new Dictionary<string,JObject>(StringComparer.Ordinal);
            var array = document[arrayProperty] as JArray ?? throw new InvalidDataException(arrayProperty + " must be an array.");
            foreach (var token in array)
            {
                var value = token as JObject ?? throw new InvalidDataException(arrayProperty + " entries must be objects.");
                var id = value[idProperty]?.Value<string>();
                if (string.IsNullOrWhiteSpace(id) || result.ContainsKey(id)) throw new InvalidDataException("Invalid or duplicate ID in " + arrayProperty + ".");
                result.Add(id, value);
            }
            return result;
        }

        private static Dictionary<string,List<JObject>> Group(JObject document, string arrayProperty, string keyProperty)
        {
            var result = new Dictionary<string,List<JObject>>(StringComparer.Ordinal);
            var array = document[arrayProperty] as JArray ?? throw new InvalidDataException(arrayProperty + " must be an array.");
            foreach (var token in array)
            {
                var value = token as JObject ?? throw new InvalidDataException(arrayProperty + " entries must be objects.");
                var key = value[keyProperty]?.Value<string>();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException("Missing group key in " + arrayProperty + ".");
                if (!result.TryGetValue(key, out var list)) result.Add(key, list = new List<JObject>());
                list.Add(value);
            }
            return result;
        }

        private static JObject Required(IReadOnlyDictionary<string,JObject> values, string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || !values.TryGetValue(id, out var value))
                throw new KeyNotFoundException("Unknown " + label + " ID: " + (id ?? "<null>"));
            return value;
        }
    }
}
