using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourArtRegistry071Tests
    {
        [Test]
        public void RegistryExactlyCoversCanonicalN01ThroughN04AcrossThirtyTrees()
        {
            var registry = FirstHourArtRegistry071.Load();
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            var progression = DeepProgressionCatalog070.LoadFromContentRoot(contentRoot);
            var expectedIds = FirstHourArtRegistry071.ExpectedTreeIds
                .SelectMany(treeId => progression.Tree(treeId).NodeIds.Take(4))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var actualIds = registry.Recipes
                .Select(value => value.artId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(FirstHourArtRegistry071.ExpectedTreeIds.Count,
                Is.EqualTo(FirstHourArtRegistry071.ExpectedTreeCount));
            Assert.That(registry.Recipes.Count, Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(actualIds, Is.EqualTo(expectedIds));

            foreach (var treeId in FirstHourArtRegistry071.ExpectedTreeIds)
            {
                var indexes = registry.Recipes
                    .Where(value => StringComparer.Ordinal.Equals(value.treeId, treeId))
                    .Select(value => value.nodeIndex)
                    .OrderBy(value => value)
                    .ToArray();
                Assert.That(indexes, Is.EqualTo(new[] { 1, 2, 3, 4 }), treeId);
            }
        }

        [Test]
        public void EveryRecipeHasDistinctNameClipCameraVfxSfxAndMotionSignatures()
        {
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            AssertDistinct(recipes.Select(value => value.displayName), "displayName");
            AssertDistinct(recipes.Select(value => value.clipId), "clipId");
            AssertDistinct(recipes.Select(value => value.cameraSignature), "cameraSignature");
            AssertDistinct(recipes.Select(value => value.vfxSignature), "vfxSignature");
            AssertDistinct(recipes.Select(value => value.sfxSignature), "sfxSignature");
            AssertDistinct(recipes.Select(value => value.motionSignature), "motionSignature");
        }

        [Test]
        public void EveryN04MirrorsAnAlreadyResolvedDeterministicCoreReaction()
        {
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            var reactions = recipes.Where(value => value.nodeIndex == 4).ToArray();
            Assert.That(reactions.Length, Is.EqualTo(FirstHourArtRegistry071.ExpectedTreeCount));

            foreach (var recipe in reactions)
            {
                var trigger = recipe.reactionTrigger;
                Assert.That(trigger, Is.Not.Null, recipe.artId);
                Assert.That(trigger.sourceEvent, Is.EqualTo("CORE_REACTION_RESOLVED"), recipe.artId);
                Assert.That(trigger.requiredResolvedArtId, Is.EqualTo(recipe.artId), recipe.artId);
                Assert.That(trigger.orderKey, Is.EqualTo("CORE_EVENT_SEQUENCE"), recipe.artId);
                Assert.That(trigger.seedPolicy, Is.EqualTo("NONE"), recipe.artId);
                Assert.That(trigger.oncePerResolvedEvent, Is.True, recipe.artId);
                Assert.That(trigger.presentationOnly, Is.True, recipe.artId);
                Assert.That(trigger.changesCombatResolution, Is.False, recipe.artId);
            }

            Assert.That(recipes.Where(value => value.nodeIndex < 4)
                .All(value => value.reactionTrigger == null), Is.True);
        }

        [Test]
        public void DisplayNamesNeverExposeAuthorityIds()
        {
            foreach (var recipe in FirstHourArtRegistry071.Load().Recipes)
            {
                Assert.That(recipe.displayName, Does.Not.Contain("_"), recipe.artId);
                Assert.That(recipe.displayName, Does.Not.Contain("TREE"), recipe.artId);
                Assert.That(recipe.displayName, Does.Not.Contain("CA002"), recipe.artId);
                Assert.That(recipe.displayName, Is.Not.EqualTo(recipe.artId), recipe.artId);
            }
        }

        [Test]
        public void ResolverIsAnHonestPresentationOnlyProceduralHook()
        {
            Assert.That(FirstHourArtPresentationResolver071.TryResolve(
                "TREE_CA002_WPN_SWORD_N01",
                out var presentation), Is.True);
            Assert.That(presentation.DisplayName, Is.EqualTo("Ready Cut"));
            Assert.That(presentation.UsesProceduralMotionRecipe, Is.True);
            Assert.That(presentation.ClaimsAuthoredAnimationClipAsset, Is.False);
            Assert.That(presentation.PresentationOnly, Is.True);
            Assert.That(presentation.ChangesCombatResolution, Is.False);
        }

        [Test]
        public void Shipping072ResolverExactlyPresentsAll120ArtsWithSemanticIcons076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            Assert.That(recipes.Count, Is.EqualTo(120));

            foreach (var recipe in recipes)
            {
                Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                    recipe.artId,
                    BattleBeatFamily.CombatArt,
                    out var profile), Is.True, recipe.artId);
                Assert.That(profile, Is.Not.Null, recipe.artId);
                Assert.That(profile.exactFirstHourRecipe, Is.True, recipe.artId);
                Assert.That(profile.artId, Is.EqualTo(recipe.artId), recipe.artId);
                Assert.That(profile.displayName, Is.EqualTo(recipe.displayName), recipe.artId);
                Assert.That(profile.presentationRecipeId, Is.EqualTo(recipe.clipId), recipe.artId);
                Assert.That(profile.presentationTreeId, Is.EqualTo(recipe.treeId), recipe.artId);
                Assert.That(profile.presentationVfxSignature, Is.EqualTo(recipe.vfxSignature), recipe.artId);
                Assert.That(profile.presentationSfxSignature, Is.EqualTo(recipe.sfxSignature), recipe.artId);
                Assert.That(profile.presentationMotionSignature, Is.EqualTo(recipe.motionSignature), recipe.artId);
                Assert.That(profile.presentationDurationMilliseconds, Is.EqualTo(recipe.durationMilliseconds), recipe.artId);
                Assert.That(profile.presentationImpactMilliseconds, Is.EqualTo(recipe.impactMilliseconds), recipe.artId);
                Assert.That(profile.startAudioResourcePath, Is.Not.Empty, recipe.artId + " start audio");
                Assert.That(profile.impactAudioResourcePath, Is.Not.Empty, recipe.artId + " impact audio");
                Assert.That(Resources.Load<AudioClip>(profile.startAudioResourcePath), Is.Not.Null,
                    recipe.artId + " start audio asset");
                Assert.That(Resources.Load<AudioClip>(profile.impactAudioResourcePath), Is.Not.Null,
                    recipe.artId + " impact audio asset");
                Assert.That(BattleArtRuntimeRegistry011.TryResolveSemanticIcon076(
                    recipe.artId,
                    BattleBeatFamily.CombatArt,
                    false,
                    out var iconId,
                    out var icon), Is.True, recipe.artId);
                Assert.That(iconId, Is.EqualTo(profile.semanticIconAssetId), recipe.artId);
                Assert.That(icon, Is.Not.Null, recipe.artId);
            }

            var roleProfiles = recipes
                .Where(value => value.treeId.Contains("_ROLE_"))
                .Select(value => BattleArtRuntimeRegistry011.ResolveProfile(
                    value.artId,
                    BattleBeatFamily.Tactical))
                .ToArray();
            Assert.That(roleProfiles.Length, Is.EqualTo(40));
            Assert.That(roleProfiles.All(value => value.exactFirstHourRecipe), Is.True);
            Assert.That(roleProfiles.All(value => !string.IsNullOrWhiteSpace(value.semanticIconAssetId)), Is.True);
        }

        [Test]
        public void LegacyPresentationAliasDoesNotMasqueradeAsCanonicalExactRecipe076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var legacyProfile076 = BattleArtRuntimeRegistry011.ResolveProfile(
                "ART_BASIC_THRUST",
                BattleBeatFamily.BasicMartial);

            Assert.That(legacyProfile076, Is.Not.Null,
                "Legacy tutorial Arts still require a normal presentation profile.");
            Assert.That(M2BattleDioramaView072.TryResolveLiveExactRecipe076(
                legacyProfile076,
                "ART_BASIC_THRUST",
                out _), Is.False,
                "A legacy alias may render normally, but must not increment the canonical 120-Art exact-recipe diagnostic.");
        }

        [Test]
        public void Live072PathConsumesCanonicalMotionVfxTimingAndExactSfxSelection076()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            var cueIds076 = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

            foreach (var recipe076 in recipes)
            {
                Assert.That(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                    recipe076.artId,
                    BattleBeatFamily.CombatArt,
                    out var profile076), Is.True, recipe076.artId);
                Assert.That(M2BattleDioramaView072.TryResolveLiveExactRecipe076(
                    profile076,
                    recipe076.artId,
                    out var choreography076), Is.True, recipe076.artId);
                Assert.That(choreography076.MotionRecipe,
                    Is.Not.EqualTo(Battle3DArtMotionRecipe071.None), recipe076.artId);
                Assert.That(choreography076.VfxRecipe,
                    Is.Not.EqualTo(Battle3DArtVfxRecipe071.None), recipe076.artId);
                Assert.That(choreography076.CameraRecipe,
                    Is.Not.EqualTo(Battle3DArtCameraRecipe071.None), recipe076.artId);
                Assert.That(choreography076.TraceRecipe,
                    Is.Not.EqualTo(Battle3DArtTraceRecipe071.None), recipe076.artId);
                Assert.That(choreography076.SemanticTracePointCount,
                    Is.GreaterThanOrEqualTo(6), recipe076.artId);
                Assert.That(choreography076.SemanticTraceLayerCount,
                    Is.GreaterThanOrEqualTo(1), recipe076.artId);
                Assert.That(choreography076.DurationMilliseconds,
                    Is.EqualTo(recipe076.durationMilliseconds), recipe076.artId);
                Assert.That(choreography076.ImpactMilliseconds,
                    Is.EqualTo(recipe076.impactMilliseconds), recipe076.artId);

                var startCue076 = M2BattleSequenceDirector072.ExactRecipeAudioCueId076(
                    profile076,
                    recipe076.artId,
                    false);
                var impactCue076 = M2BattleSequenceDirector072.ExactRecipeAudioCueId076(
                    profile076,
                    recipe076.artId,
                    true);
                Assert.That(startCue076, Does.StartWith("FH071_EXACT_START|")
                    .And.Contain(recipe076.sfxSignature), recipe076.artId);
                Assert.That(impactCue076, Does.StartWith("FH071_EXACT_IMPACT|")
                    .And.Contain(recipe076.sfxSignature), recipe076.artId);
                Assert.That(cueIds076.Add(startCue076), Is.True, recipe076.artId);
                Assert.That(cueIds076.Add(impactCue076), Is.True, recipe076.artId);
            }

            Assert.That(cueIds076.Count, Is.EqualTo(240));

            var exact076 = BattleArtRuntimeRegistry011.ResolveProfile(
                "TREE_CA002_WPN_SWORD_N01",
                BattleBeatFamily.CombatArt);
            var canonicalMotion076 = exact076.presentationMotionSignature;
            try
            {
                exact076.presentationMotionSignature = canonicalMotion076 + "_STALE";
                Assert.That(M2BattleDioramaView072.TryResolveLiveExactRecipe076(
                    exact076,
                    exact076.artId,
                    out _), Is.False,
                    "A stale metadata copy must never be certified as live recipe consumption.");
            }
            finally
            {
                exact076.presentationMotionSignature = canonicalMotion076;
            }
        }

        [Test]
        public void ValidatorRejectsRawDisplayIds()
        {
            var asset = Resources.Load<TextAsset>(FirstHourArtRegistry071.ResourcePath);
            Assert.That(asset, Is.Not.Null);
            var invalid = asset.text.Replace(
                "\"displayName\": \"Ready Cut\"",
                "\"displayName\": \"TREE_CA002_WPN_SWORD_N01\"");
            Assert.That(invalid, Is.Not.EqualTo(asset.text));
            Assert.Throws<InvalidOperationException>(() =>
                FirstHourArtRegistry071.ParseAndValidate(invalid));
        }

        private static void AssertDistinct(System.Collections.Generic.IEnumerable<string> values, string label)
        {
            var array = values.ToArray();
            Assert.That(array.Length, Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount), label);
            Assert.That(array.All(value => !string.IsNullOrWhiteSpace(value)), Is.True, label);
            Assert.That(array.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(array.Length), label);
        }
    }
}
