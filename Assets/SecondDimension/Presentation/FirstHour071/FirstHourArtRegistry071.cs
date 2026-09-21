using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation.FirstHour071
{
    [Serializable]
    public sealed class FirstHourArtManifest071
    {
        public string contentVersion;
        public int recipeCount;
        public FirstHourArtContract071 contract;
        public FirstHourArtRecipe071[] recipes;
    }

    [Serializable]
    public sealed class FirstHourArtContract071
    {
        public bool presentationOnly;
        public bool changesCombatResolution;
        public bool deterministicCoreOwnsArtSelection;
        public bool clipIdsAreLogicalProceduralRecipes;
        public bool authoredAnimationClipAssetsClaimed;
    }

    [Serializable]
    public sealed class FirstHourArtRecipe071
    {
        public string artId;
        public string treeId;
        public int nodeIndex;
        public string displayName;
        public string clipId;
        public string clipKind;
        public string cameraSignature;
        public string vfxSignature;
        public string sfxSignature;
        public string motionSignature;
        public int durationMilliseconds;
        public int impactMilliseconds;
        public FirstHourReactionTrigger071 reactionTrigger;
    }

    [Serializable]
    public sealed class FirstHourReactionTrigger071
    {
        public string sourceEvent;
        public string requiredResolvedArtId;
        public string orderKey;
        public string seedPolicy;
        public bool oncePerResolvedEvent;
        public bool presentationOnly;
        public bool changesCombatResolution;
    }

    /// <summary>
    /// Validated presentation authority for the first four Arts in all thirty trees.
    /// Clip IDs are logical procedural choreography recipes, not claims that matching
    /// Unity AnimationClip assets exist. Selection and resolution remain core-owned.
    /// </summary>
    public sealed class FirstHourArtRegistry071
    {
        public const string ContentVersion = "FIRST_HOUR_ART_PRESENTATION_071_1.0";
        public const string ResourcePath = "SecondDimension/FirstHour071/FIRST_HOUR_ART_RECIPES_120";
        public const int ExpectedTreeCount = 30;
        public const int ExpectedRecipesPerTree = 4;
        public const int ExpectedRecipeCount = ExpectedTreeCount * ExpectedRecipesPerTree;
        public const string LogicalClipKind = "PROCEDURAL_PRESENTATION_RECIPE";

        private static readonly string[] CanonicalTreeIds =
        {
            "TREE_CA002_WPN_SWORD",
            "TREE_CA002_WPN_GREAT_WEAPON",
            "TREE_CA002_WPN_AXE",
            "TREE_CA002_WPN_SPEAR_POLEARM",
            "TREE_CA002_WPN_BOW",
            "TREE_CA002_WPN_DAGGER",
            "TREE_CA002_WPN_SHIELD",
            "TREE_CA002_WPN_GAUNTLET",
            "TREE_CA002_WPN_STAFF",
            "TREE_CA002_WPN_FOCUS",
            "TREE_CA002_WPN_ENGINEERING",
            "TREE_CA002_WPN_HYBRID_RELIC",
            "TREE_CA002_MYS_FLAME",
            "TREE_CA002_MYS_FROST",
            "TREE_CA002_MYS_STORM",
            "TREE_CA002_MYS_EARTH",
            "TREE_CA002_MYS_AETHER",
            "TREE_CA002_MYS_SHADOW",
            "TREE_CA002_MYS_RESTORATION",
            "TREE_CA002_MYS_WARDING",
            "TREE_CA002_ROLE_GUARDIAN",
            "TREE_CA002_ROLE_BREAKER",
            "TREE_CA002_ROLE_DUELIST",
            "TREE_CA002_ROLE_SCOUT",
            "TREE_CA002_ROLE_SABOTEUR",
            "TREE_CA002_ROLE_FIELD_MEDIC",
            "TREE_CA002_ROLE_COMMANDER",
            "TREE_CA002_ROLE_FORMATION",
            "TREE_CA002_ROLE_PROVOCATION",
            "TREE_CA002_ROLE_RESONANCE"
        };

        private readonly FirstHourArtManifest071 _manifest;
        private readonly Dictionary<string, FirstHourArtRecipe071> _recipesById;
        private readonly IReadOnlyList<FirstHourArtRecipe071> _recipes;

        private FirstHourArtRegistry071(
            FirstHourArtManifest071 manifest,
            Dictionary<string, FirstHourArtRecipe071> recipesById)
        {
            _manifest = manifest;
            _recipesById = recipesById;
            _recipes = manifest.recipes
                .OrderBy(value => value.treeId, StringComparer.Ordinal)
                .ThenBy(value => value.nodeIndex)
                .ToArray();
        }

        public FirstHourArtManifest071 Manifest => _manifest;
        public IReadOnlyList<FirstHourArtRecipe071> Recipes => _recipes;
        public static IReadOnlyList<string> ExpectedTreeIds => CanonicalTreeIds;

        public static FirstHourArtRegistry071 Load()
        {
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null)
                throw new InvalidOperationException("Missing first-hour Art authority at Resources/" + ResourcePath + ".json");
            return ParseAndValidate(asset.text);
        }

        public static FirstHourArtRegistry071 ParseAndValidate(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw Invalid("JSON is empty.");

            FirstHourArtManifest071 manifest;
            try
            {
                manifest = JsonUtility.FromJson<FirstHourArtManifest071>(json);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("First-hour Art JSON could not be parsed.", exception);
            }

            if (manifest == null)
                throw Invalid("JSON did not contain a manifest.");
            if (!StringComparer.Ordinal.Equals(manifest.contentVersion, ContentVersion))
                throw Invalid("Unexpected contentVersion: " + (manifest.contentVersion ?? "<null>"));
            if (manifest.recipeCount != ExpectedRecipeCount)
                throw Invalid("recipeCount must be exactly " + ExpectedRecipeCount + ".");
            ValidateContract(manifest.contract);
            if (manifest.recipes == null || manifest.recipes.Length != ExpectedRecipeCount)
                throw Invalid("recipes must contain exactly " + ExpectedRecipeCount + " entries.");

            var recipesById = new Dictionary<string, FirstHourArtRecipe071>(StringComparer.Ordinal);
            foreach (var recipe in manifest.recipes)
            {
                // Unity 6's JsonUtility materializes an empty nested object for an
                // omitted serializable reference. Normalize that representation so
                // N01-N03 remain honestly free of reaction metadata.
                if (recipe != null && recipe.nodeIndex < ExpectedRecipesPerTree &&
                    IsEmptyReactionTrigger(recipe.reactionTrigger))
                {
                    recipe.reactionTrigger = null;
                }
                ValidateAndAddRecipe(recipe, recipesById);
            }

            ValidateExactCoverage(recipesById.Values);
            RequireUnique(recipesById.Values.Select(value => value.displayName), "display names");
            RequireUnique(recipesById.Values.Select(value => value.clipId), "clip IDs");
            RequireUnique(recipesById.Values.Select(value => value.cameraSignature), "camera signatures");
            RequireUnique(recipesById.Values.Select(value => value.vfxSignature), "VFX signatures");
            RequireUnique(recipesById.Values.Select(value => value.sfxSignature), "SFX signatures");
            RequireUnique(recipesById.Values.Select(value => value.motionSignature), "motion signatures");
            return new FirstHourArtRegistry071(manifest, recipesById);
        }

        public bool TryGet(string stableArtId, out FirstHourArtRecipe071 recipe) =>
            _recipesById.TryGetValue(stableArtId ?? string.Empty, out recipe);

        public FirstHourArtRecipe071 Get(string stableArtId)
        {
            if (!TryGet(stableArtId, out var recipe))
                throw new KeyNotFoundException("No first-hour Art recipe for " + (stableArtId ?? "<null>"));
            return recipe;
        }

        private static void ValidateContract(FirstHourArtContract071 contract)
        {
            if (contract == null)
                throw Invalid("contract is required.");
            if (!contract.presentationOnly || contract.changesCombatResolution)
                throw Invalid("The registry must remain presentation-only.");
            if (!contract.deterministicCoreOwnsArtSelection)
                throw Invalid("The deterministic core must own Art selection.");
            if (!contract.clipIdsAreLogicalProceduralRecipes || contract.authoredAnimationClipAssetsClaimed)
                throw Invalid("clipId values must be honest logical procedural recipes, not asset claims.");
        }

        private static void ValidateAndAddRecipe(
            FirstHourArtRecipe071 recipe,
            IDictionary<string, FirstHourArtRecipe071> recipesById)
        {
            if (recipe == null)
                throw Invalid("recipes may not contain null entries.");
            RequireText(recipe.artId, "artId");
            RequireText(recipe.treeId, "treeId on " + recipe.artId);
            RequireText(recipe.displayName, "displayName on " + recipe.artId);
            RequireText(recipe.clipId, "clipId on " + recipe.artId);
            RequireText(recipe.cameraSignature, "cameraSignature on " + recipe.artId);
            RequireText(recipe.vfxSignature, "vfxSignature on " + recipe.artId);
            RequireText(recipe.sfxSignature, "sfxSignature on " + recipe.artId);
            RequireText(recipe.motionSignature, "motionSignature on " + recipe.artId);

            if (recipe.nodeIndex < 1 || recipe.nodeIndex > ExpectedRecipesPerTree)
                throw Invalid("nodeIndex must be 1 through 4 on " + recipe.artId + ".");
            var expectedArtId = recipe.treeId + "_N" + recipe.nodeIndex.ToString("00");
            if (!StringComparer.Ordinal.Equals(recipe.artId, expectedArtId))
                throw Invalid("Stable Art ID does not match its tree/index: " + recipe.artId + ".");
            if (!StringComparer.Ordinal.Equals(recipe.clipKind, LogicalClipKind))
                throw Invalid("clipKind must be " + LogicalClipKind + " on " + recipe.artId + ".");
            if (recipe.durationMilliseconds <= 0 || recipe.impactMilliseconds <= 0 ||
                recipe.impactMilliseconds >= recipe.durationMilliseconds)
                throw Invalid("Animation timing is invalid on " + recipe.artId + ".");
            if (LooksLikeRawId(recipe.displayName))
                throw Invalid("displayName exposes a raw ID on " + recipe.artId + ": " + recipe.displayName);

            if (recipe.nodeIndex == ExpectedRecipesPerTree)
                ValidateReactionTrigger(recipe);
            else if (recipe.reactionTrigger != null)
                throw Invalid("Only N04 may declare first-hour reaction metadata: " + recipe.artId + ".");

            if (recipesById.ContainsKey(recipe.artId))
                throw Invalid("Duplicate Art ID: " + recipe.artId + ".");
            recipesById.Add(recipe.artId, recipe);
        }

        private static void ValidateReactionTrigger(FirstHourArtRecipe071 recipe)
        {
            var trigger = recipe.reactionTrigger;
            if (trigger == null)
                throw Invalid("N04 requires deterministic reaction presentation metadata: " + recipe.artId + ".");
            if (!StringComparer.Ordinal.Equals(trigger.sourceEvent, "CORE_REACTION_RESOLVED") ||
                !StringComparer.Ordinal.Equals(trigger.requiredResolvedArtId, recipe.artId) ||
                !StringComparer.Ordinal.Equals(trigger.orderKey, "CORE_EVENT_SEQUENCE") ||
                !StringComparer.Ordinal.Equals(trigger.seedPolicy, "NONE") ||
                !trigger.oncePerResolvedEvent || !trigger.presentationOnly || trigger.changesCombatResolution)
                throw Invalid("Reaction metadata must mirror an already-resolved deterministic core event: " + recipe.artId + ".");
        }

        private static bool IsEmptyReactionTrigger(FirstHourReactionTrigger071 trigger) =>
            trigger != null &&
            string.IsNullOrWhiteSpace(trigger.sourceEvent) &&
            string.IsNullOrWhiteSpace(trigger.requiredResolvedArtId) &&
            string.IsNullOrWhiteSpace(trigger.orderKey) &&
            string.IsNullOrWhiteSpace(trigger.seedPolicy) &&
            !trigger.oncePerResolvedEvent &&
            !trigger.presentationOnly &&
            !trigger.changesCombatResolution;

        private static void ValidateExactCoverage(IEnumerable<FirstHourArtRecipe071> recipes)
        {
            var expectedTrees = new HashSet<string>(CanonicalTreeIds, StringComparer.Ordinal);
            var grouped = recipes.GroupBy(value => value.treeId, StringComparer.Ordinal).ToArray();
            var actualTrees = new HashSet<string>(
                grouped.Select(value => value.Key),
                StringComparer.Ordinal);
            if (grouped.Length != ExpectedTreeCount ||
                !actualTrees.SetEquals(expectedTrees))
                throw Invalid("Recipes must cover exactly the thirty canonical skill trees.");

            foreach (var group in grouped)
            {
                var indexes = group.Select(value => value.nodeIndex).OrderBy(value => value).ToArray();
                if (!indexes.SequenceEqual(Enumerable.Range(1, ExpectedRecipesPerTree)))
                    throw Invalid("Tree must contain exactly N01 through N04: " + group.Key + ".");
            }
        }

        private static bool LooksLikeRawId(string value) =>
            value.IndexOf('_') >= 0 ||
            value.IndexOf("TREE_", StringComparison.OrdinalIgnoreCase) >= 0 ||
            value.IndexOf("CA002", StringComparison.OrdinalIgnoreCase) >= 0;

        private static void RequireUnique(IEnumerable<string> values, string label)
        {
            var array = values.ToArray();
            if (array.Any(string.IsNullOrWhiteSpace) ||
                array.Distinct(StringComparer.Ordinal).Count() != ExpectedRecipeCount)
                throw Invalid("All " + label + " must be present and distinct.");
        }

        private static void RequireText(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw Invalid(label + " is required.");
        }

        private static InvalidOperationException Invalid(string message) =>
            new InvalidOperationException("First-hour Art authority is invalid: " + message);
    }
}
