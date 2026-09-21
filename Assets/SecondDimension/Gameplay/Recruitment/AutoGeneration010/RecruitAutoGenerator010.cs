using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment.AutoGeneration010
{
    /// <summary>
    /// Expands the already-committed recruit identity into a complete build and visual recipe.
    /// It never calls a network service, Unity RNG, frame time, or runtime generative AI.
    /// </summary>
    public sealed class RecruitAutoGenerator010
    {
        public const string ContentVersion = "RECRUIT_AUTOGEN_010_1.0";
        private static readonly string[] LayerIds =
        {
            "L01_BASE_RACE","L02_BUILD","L03_FACE","L04_SKIN","L05_EYES","L06_BROWS","L07_EARS_HORNS",
            "L08_HAIR_BACK","L09_FACIAL_HAIR","L10_MARKINGS","L11_SCARS","L12_OUTFIT","L13_ARMOR",
            "L14_HAIR_FRONT","L15_ACCESSORY","L16_WEAPON","L17_EXPRESSION","L18_INJURY","L19_TURNING_POINT",
            "L20_LEGEND","L21_LIGHT","L22_WORLD_FX"
        };
        private static readonly HashSet<string> RequiredLayers = new HashSet<string>(new[]
        {
            "L01_BASE_RACE","L02_BUILD","L03_FACE","L04_SKIN","L05_EYES","L06_BROWS","L12_OUTFIT",
            "L17_EXPRESSION","L21_LIGHT"
        }, StringComparer.Ordinal);

        private readonly RecruitAutoGenerationCatalog010 _catalog;

        public RecruitAutoGenerator010(RecruitAutoGenerationCatalog010 catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public GeneratedRecruitProfile010 Generate(OpeningRecruitRecord recruit, string worldId = "", int collisionSalt = 0)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            if (!ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(recruit.RecruitId, recruit.SignatureId, RecruitAuthorityKind.Normal))
                throw new InvalidOperationException("Protected actors cannot use normal recruit generation.");

            var cls = _catalog.Class(recruit.StartingClassId);
            var signatureProgression = StringComparer.Ordinal.Equals(recruit.SourceType, "SIGNATURE")
                ? _catalog.Progression(recruit.SignatureId)
                : null;
            var build = signatureProgression == null
                ? SelectProceduralBuild(recruit, cls)
                : BuildFromSignatureProgression(recruit, signatureProgression);

            var mysticTreeId = signatureProgression?["mysticTreeId"]?.Value<string>() ?? SelectMysticTree(recruit, cls, build.WeaponTreeId);
            var roleTrees = signatureProgression == null
                ? SelectRoleTrees(recruit, cls)
                : new[]
                {
                    signatureProgression["primaryRoleTreeId"]?.Value<string>() ?? string.Empty,
                    signatureProgression["secondaryRoleTreeId"]?.Value<string>() ?? string.Empty
                };

            var primaryRoleTreeId = roleTrees.Length > 0 ? roleTrees[0] : string.Empty;
            var stableNodes = signatureProgression == null
                ? StartingNodesForProcedural(recruit, build.WeaponTreeId, primaryRoleTreeId)
                : StartingNodesForSignature(
                    signatureProgression,
                    build.WeaponTreeId,
                    primaryRoleTreeId);
            var legacy = UniqueSorted((recruit.StartingArtIds ?? Array.Empty<string>())
                .Where(value => IsLegitimateStartingLegacyArt(
                    value,
                    build.WeaponTreeId,
                    primaryRoleTreeId)));
            var learned = UniqueSorted(stableNodes.Concat(legacy));
            var disciplines = new Dictionary<string,string>(StringComparer.Ordinal);
            foreach (var id in stableNodes)
            {
                var node = _catalog.Node(id);
                disciplines[id] = _catalog.Tree(node["treeId"].Value<string>())
                    ["discipline"].Value<string>().ToUpperInvariant();
            }
            foreach (var id in legacy) if (!disciplines.ContainsKey(id)) disciplines[id] = "LEGACY";

            var legalTrees = UniqueSorted(_catalog.Class(recruit.StartingClassId)["allowedTreeIds"] is JArray array
                ? array.Values<string>()
                : Array.Empty<string>());
            var roleWeights = RoleWeights(recruit);
            var stats = BaseStats(recruit);
            var visual = BuildVisualRecipe(recruit, build.FixedWeaponFamilyId, collisionSalt);
            var puppet = BuildBattlePuppet(recruit, build.FixedWeaponFamilyId, visual);
            var stableAuthoredId = signatureProgression?["stableRecruitId"]?.Value<string>() ?? string.Empty;

            var profile = new GeneratedRecruitProfile010
            {
                ContentVersion = ContentVersion,
                ProfileId = "RGP010_" + RecruitmentDeterminism.StableHash(recruit.RecruitId + "|" + ContentVersion, 20),
                RecruitId = recruit.RecruitId,
                SourceType = recruit.SourceType,
                SignatureId = recruit.SignatureId ?? string.Empty,
                StableAuthoredRecruitId = stableAuthoredId,
                DisplayName = recruit.DisplayName,
                RaceId = recruit.RaceId,
                WorldId = string.IsNullOrWhiteSpace(worldId) ? recruit.HomeCommunityId : worldId,
                StartingClassId = recruit.StartingClassId,
                FixedWeaponFamilyId = build.FixedWeaponFamilyId,
                WeaponTreeId = build.WeaponTreeId,
                MysticTreeId = mysticTreeId ?? string.Empty,
                PrimaryRoleTreeId = primaryRoleTreeId,
                SecondaryRoleTreeId = roleTrees.Length > 1 ? roleTrees[1] : string.Empty,
                LegalTreeIds = legalTrees,
                StartingStableNodeIds = stableNodes,
                PreservedLegacyArtIds = legacy,
                StartingLearnedArtIds = learned,
                ArtDisciplineById = disciplines,
                RoleWeights = roleWeights,
                BaseStats = stats,
                MaximumHp = 55 + stats["HP"] * 2,
                MaximumMp = Math.Max(0, 4 + (stats["MAGIC"] - 40) / 3),
                LeadershipScore = recruit.LeadershipScore,
                CommandBandwidth = recruit.CommandBandwidth,
                AutoEquipAllowed = false,
                VisualRecipe = visual,
                BattlePuppetRecipe = puppet,
                ProfileHash = string.Empty
            };
            profile.ProfileHash = ProfileHash(profile);
            return profile;
        }

        public GeneratedRecruitProfile010 Generate(RecruitState recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            if (string.IsNullOrWhiteSpace(recruit.CanonicalApplicantJson))
                throw new InvalidOperationException("A signed recruit needs canonical applicant JSON for deterministic regeneration.");
            var record = JObject.Parse(recruit.CanonicalApplicantJson).ToObject<OpeningRecruitRecord>();
            return Generate(record, recruit.WorldId);
        }

        public GeneratedApplicantBoard010 GenerateBoard(string boardId, IReadOnlyList<OpeningRecruitRecord> recruits, string worldId = "")
        {
            if (string.IsNullOrWhiteSpace(boardId)) throw new ArgumentException("Board ID is required.", nameof(boardId));
            if (recruits == null || recruits.Count == 0) throw new ArgumentException("At least one recruit is required.", nameof(recruits));
            var profiles = new List<GeneratedRecruitProfile010>();
            var collisionKeys = new HashSet<string>(StringComparer.Ordinal);
            var repairs = 0;
            for (var index = 0; index < recruits.Count; index++)
            {
                GeneratedRecruitProfile010 profile = null;
                for (var salt = 0; salt < 64; salt++)
                {
                    var candidate = Generate(recruits[index], worldId, salt);
                    if (!collisionKeys.Contains(candidate.VisualRecipe.CollisionKey) && AdjacentSilhouetteIsDistinct(profiles, candidate))
                    {
                        profile = candidate;
                        if (salt > 0) repairs++;
                        break;
                    }
                }
                if (profile == null) throw new InvalidOperationException("Could not satisfy deterministic board visual diversity within 64 salts.");
                collisionKeys.Add(profile.VisualRecipe.CollisionKey);
                profiles.Add(profile);
            }
            return new GeneratedApplicantBoard010
            {
                BoardId = boardId,
                Profiles = profiles.AsReadOnly(),
                VisualCollisionRepairs = repairs,
                GenerationHash = CanonicalJson.Sha256Hex(profiles)
            };
        }

        private BuildChoice SelectProceduralBuild(OpeningRecruitRecord recruit, JObject cls)
        {
            var recipes = _catalog.BuildRecipesForClass(recruit.StartingClassId);
            if (recipes.Count == 0) throw new InvalidOperationException("Class has no legal weapon build recipes: " + recruit.StartingClassId);
            JObject best = null;
            var bestScore = int.MinValue;
            foreach (var recipe in recipes)
            {
                var family = _catalog.Family(recipe["fixedWeaponFamilyId"].Value<string>());
                var score = 0;
                foreach (var tag in StringValues(family["equipmentTagsGranted"]))
                {
                    if (recruit.WeaponAptitudes != null && recruit.WeaponAptitudes.TryGetValue(tag, out var aptitude)) score += aptitude * 5;
                    if (MainHandTags(recruit).Contains(tag, StringComparer.Ordinal)) score += 1000;
                }
                score += DeterministicTie(recruit.RecruitId, recipe["id"].Value<string>(), 97);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = recipe;
                }
            }
            var requested096 = HeroMasterPrimaryWeapon096.RequestedFamily(
                recruit, best["fixedWeaponFamilyId"].Value<string>());
            var primary096 = recipes.FirstOrDefault(recipe => StringComparer.Ordinal.Equals(
                recipe["fixedWeaponFamilyId"].Value<string>(), requested096));
            if (primary096 != null) best = primary096;
            return new BuildChoice(best["fixedWeaponFamilyId"].Value<string>(), best["weaponTreeId"].Value<string>());
        }

        private static BuildChoice BuildFromSignatureProgression(OpeningRecruitRecord recruit, JObject progression)
        {
            var family = progression["fixedWeaponFamilyId"]?.Value<string>();
            var tree = progression["weaponTreeId"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(family) || string.IsNullOrWhiteSpace(tree))
                throw new InvalidOperationException("Signature progression is missing fixed weapon family/tree for " + recruit.SignatureId + ".");
            return new BuildChoice(family, tree);
        }

        private string SelectMysticTree(OpeningRecruitRecord recruit, JObject cls, string weaponTreeId)
        {
            var values = _catalog.AllowedTrees(recruit.StartingClassId, "MYSTIC");
            JObject best = null;
            var bestScore = int.MinValue;
            foreach (var tree in values)
            {
                var discipline = tree["discipline"].Value<string>().ToUpperInvariant();
                var score = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue(discipline, out var aptitude) ? aptitude * 10 : 0;
                score += DeterministicTie(recruit.RecruitId, tree["id"].Value<string>(), 101);
                if (score > bestScore) { bestScore = score; best = tree; }
            }
            if (best == null) return string.Empty;
            var mystic = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue("MYSTIC", out var m) ? m : 0;
            var restoration = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue("RESTORATION", out var r) ? r : 0;
            var guard = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue("GUARD", out var g) ? g : 0;
            return Math.Max(mystic, Math.Max(restoration, guard)) >= 78 ? best["id"].Value<string>() : string.Empty;
        }

        private string[] SelectRoleTrees(OpeningRecruitRecord recruit, JObject cls)
        {
            var values = _catalog.AllowedTrees(recruit.StartingClassId, "ROLE");
            var scored = new List<KeyValuePair<int,string>>();
            foreach (var tree in values)
            {
                var discipline = tree["discipline"].Value<string>().ToUpperInvariant();
                var score = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue(discipline, out var aptitude) ? aptitude * 10 : 0;
                score += DeterministicTie(recruit.RecruitId, tree["id"].Value<string>(), 89);
                scored.Add(new KeyValuePair<int,string>(score, tree["id"].Value<string>()));
            }
            scored.Sort((a,b) => a.Key == b.Key ? StringComparer.Ordinal.Compare(a.Value,b.Value) : b.Key.CompareTo(a.Key));
            return scored.Take(2).Select(x => x.Value).ToArray();
        }

        private IReadOnlyList<string> StartingNodesForProcedural(
            OpeningRecruitRecord recruit,
            string weaponTreeId,
            string primaryRoleTreeId)
        {
            var result = new List<string>();
            AddRoot(result, weaponTreeId);
            if (recruit.DevelopmentPotentialScore >= 690)
            {
                var tree = _catalog.Tree(weaponTreeId);
                var ids = StringValues(tree["nodeIds"]);
                if (ids.Length > 1) result.Add(ids[1]);
            }
            AddRoot(result, primaryRoleTreeId);
            return UniqueSorted(result);
        }

        private IReadOnlyList<string> StartingNodesForSignature(
            JObject signatureProgression,
            string weaponTreeId,
            string primaryRoleTreeId)
        {
            var result = new List<string>();
            foreach (var nodeId in StringValues(signatureProgression["startingUnlockedNodeIds"]))
            {
                var node = _catalog.Node(nodeId);
                var treeId = node["treeId"]?.Value<string>() ?? string.Empty;
                if (StringComparer.Ordinal.Equals(treeId, weaponTreeId) ||
                    StringComparer.Ordinal.Equals(treeId, primaryRoleTreeId))
                    result.Add(nodeId);
            }
            AddRoot(result, weaponTreeId);
            AddRoot(result, primaryRoleTreeId);
            return UniqueSorted(result);
        }

        private bool IsLegitimateStartingLegacyArt(
            string artId,
            string weaponTreeId,
            string primaryRoleTreeId)
        {
            if (!_catalog.TryNode(artId, out var node)) return true;
            var treeId = node["treeId"]?.Value<string>() ?? string.Empty;
            return StringComparer.Ordinal.Equals(treeId, weaponTreeId) ||
                   StringComparer.Ordinal.Equals(treeId, primaryRoleTreeId);
        }

        private void AddRoot(ICollection<string> result, string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) return;
            var values = StringValues(_catalog.Tree(treeId)["nodeIds"]);
            if (values.Length > 0) result.Add(values[0]);
        }

        private RecruitVisualRecipe010 BuildVisualRecipe(OpeningRecruitRecord recruit, string familyId, int collisionSalt)
        {
            var signature = StringComparer.Ordinal.Equals(recruit.SourceType, "SIGNATURE") ? _catalog.SignatureVisual(recruit.SignatureId) : null;
            if (signature != null)
            {
                var palette = StringValues(signature["paletteAuthority"]);
                return new RecruitVisualRecipe010
                {
                    VisualRecipeId = "VISR010_" + RecruitmentDeterminism.StableHash(recruit.RecruitId + "|SIGNATURE", 20),
                    VisualSeed = recruit.VisualSeed,
                    VisualMode = signature["visualMode"].Value<string>(),
                    PortraitResourcePath = signature["portraitResourcePath"].Value<string>(),
                    StandeeResourcePath = signature["standeeResourcePath"].Value<string>(),
                    ActionResourcePath = signature["actionResourcePath"].Value<string>(),
                    PaletteIds = palette,
                    Layers = BuildModularLayers(recruit, familyId, collisionSalt),
                    CollisionKey = "SIGNATURE|" + signature["stableRecruitId"].Value<string>(),
                    SilhouetteKey = signature["silhouetteAuthority"].Value<string>(),
                    RuntimeGeneratedAi = false,
                    CosmeticOnly = true
                };
            }
            var layers = BuildModularLayers(recruit, familyId, collisionSalt);
            return new RecruitVisualRecipe010
            {
                VisualRecipeId = "VISR010_" + RecruitmentDeterminism.StableHash(recruit.RecruitId + "|" + collisionSalt + "|VISUAL", 20),
                VisualSeed = recruit.VisualSeed,
                VisualMode = "DETERMINISTIC_MODULAR_22_LAYER",
                PortraitResourcePath = "GeneratedRecruitPortraitCache010/" + recruit.RecruitId,
                StandeeResourcePath = "GeneratedRecruitStandeeCache010/" + recruit.RecruitId,
                ActionResourcePath = string.Empty,
                PaletteIds = Array.AsReadOnly(new[] { LayerPiece(layers, "L04_SKIN"), LayerPiece(layers, "L21_LIGHT") }),
                Layers = layers,
                CollisionKey = string.Join("|", LayerPiece(layers, "L03_FACE"), LayerPiece(layers, "L08_HAIR_BACK"), LayerPiece(layers, "L12_OUTFIT")),
                SilhouetteKey = string.Join("|", LayerPiece(layers, "L02_BUILD"), LayerPiece(layers, "L12_OUTFIT"), LayerPiece(layers, "L13_ARMOR"), familyId),
                RuntimeGeneratedAi = false,
                CosmeticOnly = true
            };
        }

        private IReadOnlyList<RecruitVisualLayerSelection010> BuildModularLayers(OpeningRecruitRecord recruit, string familyId, int collisionSalt)
        {
            var rng = Pcg32.FromParts(recruit.VisualSeed, ContentVersion, "VISUAL_RECIPE", collisionSalt);
            var result = new List<RecruitVisualLayerSelection010>();
            for (var index = 0; index < LayerIds.Length; index++)
            {
                var layerId = LayerIds[index];
                var candidates = _catalog.VisualPieces(layerId, recruit.RaceId).ToList();
                if (layerId == "L16_WEAPON")
                    candidates = candidates.Where(x => HasTag(x, "weaponFamily:" + familyId)).ToList();
                var required = RequiredLayers.Contains(layerId);
                var include = required || layerId == "L16_WEAPON" || rng.NextBounded(100) < OptionalChance(layerId);
                if (!include || candidates.Count == 0) continue;
                var chosen = candidates[(int)rng.NextBounded((uint)candidates.Count)];
                result.Add(new RecruitVisualLayerSelection010
                {
                    LayerId = layerId,
                    PieceId = chosen["id"].Value<string>(),
                    ResourcePath = chosen["resourcePath"].Value<string>(),
                    Required = required,
                    AssetStatus = chosen["assetStatus"].Value<string>(),
                    Order = index + 1
                });
            }
            foreach (var required in RequiredLayers)
                if (!result.Any(x => StringComparer.Ordinal.Equals(x.LayerId, required)))
                    throw new InvalidOperationException("Required visual layer has no compatible piece: " + required + " for " + recruit.RaceId);
            result.Sort((a,b) => a.Order.CompareTo(b.Order));
            return result.AsReadOnly();
        }

        private RecruitBattlePuppetRecipe010 BuildBattlePuppet(OpeningRecruitRecord recruit, string familyId, RecruitVisualRecipe010 visual)
        {
            var build = LayerPiece(visual.Layers, "L02_BUILD");
            var armor = LayerPath(visual.Layers, "L13_ARMOR");
            var weapon = LayerPath(visual.Layers, "L16_WEAPON");
            var animations = new List<string>
            {
                "ANIM010_IDLE_BREATH", "ANIM010_HIT_REACT", "ANIM010_GUARD", "ANIM010_VICTORY",
                "ANIM009_MARTIAL_" + familyId.Replace("WEAPON_FAMILY_", string.Empty)
            };
            return new RecruitBattlePuppetRecipe010
            {
                BattlePuppetId = "BPU010_" + RecruitmentDeterminism.StableHash(recruit.RecruitId + "|PUPPET", 20),
                BodyRigId = "RIG010_" + recruit.RaceId + "_" + SafeSuffix(build),
                WeaponFamilyId = familyId,
                WeaponPropResourcePath = weapon,
                OffhandPropResourcePath = string.Empty,
                ArmorOverlayResourcePath = armor,
                AnimationFamilyIds = animations.AsReadOnly(),
                LodModes = Array.AsReadOnly(new[] { "HERO", "CONTEXT", "TACTICAL_ANCHOR", "NAVIGATOR" }),
                SupportsPoseSwap = true,
                SupportsLayeredRig = true
            };
        }

        private static IReadOnlyDictionary<string,int> RoleWeights(OpeningRecruitRecord recruit)
        {
            var result = new Dictionary<string,int>(StringComparer.Ordinal);
            foreach (var axis in RecruitmentDeterminism.Disciplines)
                result[axis] = recruit.DisciplineAptitudes != null && recruit.DisciplineAptitudes.TryGetValue(axis, out var value) ? value : 0;
            return result;
        }

        private static IReadOnlyDictionary<string,int> BaseStats(OpeningRecruitRecord recruit)
        {
            var result = new Dictionary<string,int>(StringComparer.Ordinal);
            foreach (var stat in RecruitmentDeterminism.Stats)
                result[stat] = recruit.StatTendencies != null && recruit.StatTendencies.TryGetValue(stat, out var value) ? value.BaseIndex : 0;
            return result;
        }

        private static HashSet<string> MainHandTags(OpeningRecruitRecord recruit)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            if (recruit.EquipmentLoadout?.Slots != null && recruit.EquipmentLoadout.Slots.TryGetValue("MAIN_HAND", out var item) && item?.Tags != null)
                foreach (var tag in item.Tags) result.Add(tag);
            return result;
        }

        private static int DeterministicTie(string recruitId, string stableId, uint bound) =>
            (int)Pcg32.FromParts(recruitId, "AUTOGEN010_TIE", stableId).NextBounded(bound);

        private static int OptionalChance(string layerId)
        {
            switch (layerId)
            {
                case "L07_EARS_HORNS": return 85;
                case "L08_HAIR_BACK":
                case "L14_HAIR_FRONT": return 88;
                case "L09_FACIAL_HAIR": return 22;
                case "L10_MARKINGS": return 28;
                case "L11_SCARS": return 24;
                case "L13_ARMOR": return 72;
                case "L15_ACCESSORY": return 65;
                case "L18_INJURY":
                case "L19_TURNING_POINT":
                case "L20_LEGEND": return 0;
                case "L22_WORLD_FX": return 35;
                default: return 50;
            }
        }

        private static bool HasTag(JObject value, string tag)
        {
            var tags = value["compatibilityTags"] as JArray;
            return tags != null && tags.Values<string>().Contains(tag, StringComparer.Ordinal);
        }

        private static bool AdjacentSilhouetteIsDistinct(IReadOnlyList<GeneratedRecruitProfile010> previous, GeneratedRecruitProfile010 candidate)
        {
            if (previous.Count == 0) return true;
            var left = previous[previous.Count - 1].VisualRecipe.SilhouetteKey.Split('|');
            var right = candidate.VisualRecipe.SilhouetteKey.Split('|');
            var count = Math.Min(left.Length, right.Length);
            var differences = Math.Abs(left.Length - right.Length);
            for (var i = 0; i < count; i++) if (!StringComparer.Ordinal.Equals(left[i], right[i])) differences++;
            return differences >= 3;
        }

        private static string LayerPiece(IReadOnlyList<RecruitVisualLayerSelection010> layers, string layerId)
        {
            for (var i = 0; i < layers.Count; i++) if (StringComparer.Ordinal.Equals(layers[i].LayerId, layerId)) return layers[i].PieceId;
            return "NONE";
        }

        private static string LayerPath(IReadOnlyList<RecruitVisualLayerSelection010> layers, string layerId)
        {
            for (var i = 0; i < layers.Count; i++) if (StringComparer.Ordinal.Equals(layers[i].LayerId, layerId)) return layers[i].ResourcePath;
            return string.Empty;
        }

        private static string SafeSuffix(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "DEFAULT";
            var index = value.LastIndexOf('_');
            return index >= 0 && index + 1 < value.Length ? value.Substring(index + 1) : value;
        }

        private static string ProfileHash(GeneratedRecruitProfile010 profile)
        {
            var token = JObject.FromObject(profile);
            token.Remove("profileHash");
            return CanonicalJson.Sha256Hex(token);
        }

        private static string[] StringValues(JToken token) => token is JArray array ? array.Values<string>().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray() : Array.Empty<string>();
        private static string[] UniqueSorted(IEnumerable<string> values) => values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        private readonly struct BuildChoice
        {
            public BuildChoice(string fixedWeaponFamilyId, string weaponTreeId) { FixedWeaponFamilyId = fixedWeaponFamilyId; WeaponTreeId = weaponTreeId; }
            public string FixedWeaponFamilyId { get; }
            public string WeaponTreeId { get; }
        }
    }
}
