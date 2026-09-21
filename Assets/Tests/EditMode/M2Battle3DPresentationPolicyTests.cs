using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class M2Battle3DPresentationPolicyTests
    {
        [Test]
        public void AuthoritativeFlankPositionShiftUsesBlindSideCameraAndRepositionAction()
        {
            var source = BattlePresentationPlanner.Plan(new[]
            {
                new M2BattleEventView
                {
                    Sequence = 12,
                    Round = 3,
                    EventType = "POSITION_SHIFT",
                    ArtId = "CMD_FLANK",
                    Text = "Second Union repositions.",
                    ActorUnionId = "PLAYER_UNION",
                    ActorMemberId = "PLAYER_MEMBER",
                    TargetUnionId = "ENEMY_UNION",
                    TargetMemberId = "ENEMY_MEMBER"
                }
            }).Single();

            var directive = M2Battle3DPresentationPolicy.CreateDirective(source);

            Assert.That(directive.Action, Is.EqualTo(Battle3DAction.Reposition));
            Assert.That(directive.Camera, Is.EqualTo(Battle3DCameraIntent.BlindSide));
            Assert.That(directive.EventType, Is.EqualTo("POSITION_SHIFT"));
            Assert.That(directive.ArtId, Is.EqualTo("CMD_FLANK"));
            Assert.That(directive.IsSideStrike, Is.True);
            Assert.That(directive.IsBlindSide, Is.True);
            Assert.That(directive.TacticalCallout, Is.EqualTo("SIDE STRIKE · BLIND SIDE"));
            Assert.That(directive.UseImpactPause, Is.False,
                "A position event must not be invented into a damage event by presentation.");
            Assert.That(directive.IsPresentationOnly, Is.True);
        }

        [TestCase(BattleBeatFamily.BasicMartial, "POSITION_SHIFT", "CMD_FLANK")]
        [TestCase(BattleBeatFamily.Positioning, "TACTICAL_HIT", "CMD_FLANK")]
        [TestCase(BattleBeatFamily.Positioning, "POSITION_SHIFT", "CMD_BALANCED")]
        public void SideStrikeRequiresExactAuthoritativeFamilyEventAndCommandFields(
            BattleBeatFamily family,
            string eventType,
            string commandId)
        {
            var source = Beat(family, BattleCameraShot.Impact,
                "Copy says SIDE STRIKE and BLIND SIDE, but copy is not authority.",
                true, eventType, commandId);

            var directive = M2Battle3DPresentationPolicy.CreateDirective(source);

            Assert.That(directive.Camera, Is.EqualTo(Battle3DCameraIntent.Impact));
            Assert.That(directive.EventType, Is.EqualTo(eventType));
            Assert.That(directive.ArtId, Is.EqualTo(commandId));
            Assert.That(directive.IsSideStrike, Is.False);
            Assert.That(directive.IsBlindSide, Is.False);
            Assert.That(directive.TacticalCallout, Is.Empty);
        }

        [Test]
        public void InterceptionUsesThePlannerSwappedProtectorAsVisualActorExactlyOnce()
        {
            var events = new[]
            {
                new M2BattleEventView
                {
                    Sequence = 7,
                    Round = 2,
                    EventType = "INTERCEPTION",
                    Text = "Maren intercepts the attack.",
                    ActorUnionId = "ENEMY_UNION",
                    ActorMemberId = "ENEMY_ATTACKER",
                    TargetUnionId = "PLAYER_UNION",
                    TargetMemberId = "MAREN_HOLT"
                }
            };

            var planned = BattlePresentationPlanner.Plan(events).Single();
            var directive = M2Battle3DPresentationPolicy.CreateDirective(planned);

            Assert.That(directive.Action, Is.EqualTo(Battle3DAction.Intercept));
            Assert.That(directive.ActorMemberId, Is.EqualTo("MAREN_HOLT"));
            Assert.That(directive.TargetMemberId, Is.EqualTo("ENEMY_ATTACKER"));
            Assert.That(directive.UseImpactPause, Is.True);
        }

        [TestCase(BattleBeatFamily.BasicMartial, Battle3DAction.WeaponStrike, true, false)]
        [TestCase(BattleBeatFamily.CombatArt, Battle3DAction.CombatArt, true, true)]
        [TestCase(BattleBeatFamily.Mystic, Battle3DAction.MysticArt, true, true)]
        [TestCase(BattleBeatFamily.Restoration, Battle3DAction.Restore, false, false)]
        [TestCase(BattleBeatFamily.Guard, Battle3DAction.Guard, false, false)]
        [TestCase(BattleBeatFamily.Downed, Battle3DAction.Downed, true, true)]
        [TestCase(BattleBeatFamily.Breakthrough, Battle3DAction.Breakthrough, false, true)]
        [TestCase(BattleBeatFamily.Result, Battle3DAction.Result, false, false)]
        public void RequiredFamiliesHaveExplicit3DStagingSemantics(
            BattleBeatFamily family,
            Battle3DAction expectedAction,
            bool expectedImpactPause,
            bool expectedShake)
        {
            var directive = M2Battle3DPresentationPolicy.CreateDirective(
                Beat(family, CameraFor(family), family.ToString()));

            Assert.That(directive.Action, Is.EqualTo(expectedAction));
            Assert.That(directive.VisuallyStaged, Is.True);
            Assert.That(directive.UseImpactPause, Is.EqualTo(expectedImpactPause));
            Assert.That(directive.UseCameraShake, Is.EqualTo(expectedShake));
        }

        [Test]
        public void DirectiveIsDeterministicAndCopiesOnlyDisplayData()
        {
            var source = new BattlePresentationBeat(
                91, 4, "MARTIAL_HIT", BattleBeatFamily.CombatArt, BattleCameraShot.Impact,
                "PLAYER_UNION", "MAREN_HOLT", "ENEMY_UNION", "GATE_GNAWER",
                "ART_POWER_CUT", 37, "Power Cut deals 37 damage.",
                "VFX_WEAPON_IMPACT", "SFX_WEAPON_IMPACT", true);

            var first = M2Battle3DPresentationPolicy.CreateDirective(source);
            var second = M2Battle3DPresentationPolicy.CreateDirective(source);

            Assert.That(second.StableDescriptor, Is.EqualTo(first.StableDescriptor));
            Assert.That(first.SourceSequence, Is.EqualTo(91));
            Assert.That(first.DisplayAmount, Is.EqualTo(37));
            Assert.That(first.ActorMemberId, Is.EqualTo("MAREN_HOLT"));
            Assert.That(first.TargetMemberId, Is.EqualTo("GATE_GNAWER"));
            Assert.That(first.IsPresentationOnly, Is.True);
        }

        [Test]
        public void FirstHourStableArtIdProjectsHonestProceduralChoreographyIntoLiveDirective()
        {
            var source = new BattlePresentationBeat(
                101, 2, "MARTIAL_HIT", BattleBeatFamily.CombatArt, BattleCameraShot.Impact,
                "PLAYER_UNION", "MAREN_HOLT", "ENEMY_UNION", "GATE_GNAWER",
                "TREE_CA002_WPN_SWORD_N01", 24, "Maren uses Ready Cut.",
                "VFX_WEAPON_IMPACT", "SFX_WEAPON_IMPACT", true);

            var directive = M2Battle3DPresentationPolicy.CreateDirective(source);
            var choreography = directive.ArtChoreography071;

            Assert.That(directive.HasFirstHourArtChoreography071, Is.True);
            Assert.That(directive.UsesFirstHourArtPerformance071, Is.True);
            Assert.That(choreography, Is.Not.Null);
            Assert.That(choreography.DisplayName, Is.EqualTo("Ready Cut"));
            Assert.That(choreography.CameraRecipe,
                Is.EqualTo(Battle3DArtCameraRecipe071.MediumThreeQuarterTrackIn));
            Assert.That(choreography.MotionRecipe,
                Is.EqualTo(Battle3DArtMotionRecipe071.SetAdvancePrimaryRecover));
            Assert.That(choreography.VfxRecipe,
                Is.EqualTo(Battle3DArtVfxRecipe071.FocusedPrimary));
            Assert.That(choreography.DurationMilliseconds, Is.EqualTo(720));
            Assert.That(choreography.ImpactMilliseconds, Is.EqualTo(420));
            Assert.That(choreography.SfxSignature, Does.Contain("READY_CUT"));
            Assert.That(choreography.UsesProceduralMotionRecipe, Is.True);
            Assert.That(choreography.ClaimsAuthoredAnimationClipAsset, Is.False);
            Assert.That(choreography.IsPresentationOnly, Is.True);
            Assert.That(choreography.ChangesCombatResolution, Is.False);

            Assert.That(directive.Action, Is.EqualTo(Battle3DAction.CombatArt));
            Assert.That(directive.DisplayAmount, Is.EqualTo(24));
            Assert.That(directive.Camera, Is.EqualTo(Battle3DCameraIntent.Impact),
                "Recipe camera motion must not rewrite the authoritative beat's tactical camera meaning.");
        }

        [Test]
        public void N01ThroughN04ProduceDistinctCameraVfxMotionTimingAndDescriptors()
        {
            var ids = new[]
            {
                "TREE_CA002_WPN_SWORD_N01",
                "TREE_CA002_WPN_SWORD_N02",
                "TREE_CA002_WPN_SWORD_N03",
                "TREE_CA002_WPN_SWORD_N04"
            };
            var directives = ids
                .Select(id => M2Battle3DPresentationPolicy.CreateDirective(
                    Beat(BattleBeatFamily.CombatArt, BattleCameraShot.Impact, id, true, "MARTIAL_HIT", id)))
                .ToArray();
            var choreography = directives.Select(value => value.ArtChoreography071).ToArray();

            Assert.That(choreography.All(value => value != null), Is.True);
            Assert.That(choreography.Select(value => value.CameraRecipe).Distinct().Count(), Is.EqualTo(4));
            Assert.That(choreography.Select(value => value.VfxRecipe).Distinct().Count(), Is.EqualTo(4));
            Assert.That(choreography.Select(value => value.MotionRecipe).Distinct().Count(), Is.EqualTo(4));
            Assert.That(choreography.Select(value => value.DurationMilliseconds).Distinct().Count(), Is.EqualTo(4));
            Assert.That(choreography.Select(value => value.ImpactMilliseconds).Distinct().Count(), Is.EqualTo(4));
            Assert.That(choreography.Select(value => value.StableDescriptor).Distinct().Count(), Is.EqualTo(4));
            Assert.That(directives.Select(value => value.StableDescriptor).Distinct().Count(), Is.EqualTo(4));
        }

        [Test]
        public void AllOneHundredTwentyRecipesReachTheLiveDirectiveWithUniqueDescriptors()
        {
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            var directives = recipes.Select(recipe =>
                M2Battle3DPresentationPolicy.CreateDirective(
                    Beat(BattleBeatFamily.CombatArt, BattleCameraShot.Impact,
                        recipe.displayName, true, "MARTIAL_HIT", recipe.artId))).ToArray();

            Assert.That(directives.Length, Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.All(value => value.HasFirstHourArtChoreography071), Is.True);
            Assert.That(directives.All(value => value.UsesFirstHourArtPerformance071), Is.True);
            Assert.That(directives.All(value => value.ArtChoreography071.CameraRecipe !=
                                                Battle3DArtCameraRecipe071.None), Is.True);
            Assert.That(directives.All(value => value.ArtChoreography071.MotionRecipe !=
                                                Battle3DArtMotionRecipe071.None), Is.True);
            Assert.That(directives.All(value => value.ArtChoreography071.VfxRecipe !=
                                                Battle3DArtVfxRecipe071.None), Is.True);
            Assert.That(directives.Select(value => value.ArtChoreography071.StableDescriptor)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.StableVisualSeed)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.CameraVisualSeed)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.MotionVisualSeed)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.VfxVisualSeed)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.MotionMagnitude)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.Select(value => value.ArtChoreography071.CameraLateralOffset)
                .Distinct().Count(), Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(directives.All(value => value.ArtChoreography071.DurationSeconds > 0f &&
                                                value.ArtChoreography071.ImpactSeconds > 0f), Is.True);
            Assert.That(directives.All(value => !value.ArtChoreography071.ClaimsAuthoredAnimationClipAsset),
                Is.True);
        }

        [Test]
        public void AllThirtyTreesProjectSemanticStylesAndAllOneHundredTwentyPerformancesStayDistinct()
        {
            var recipes = FirstHourArtRegistry071.Load().Recipes;
            var choreography = recipes
                .Select(value =>
                {
                    Assert.That(Battle3DArtChoreography071.TryCreate(value.artId, out var resolved), Is.True,
                        value.artId);
                    return resolved;
                })
                .ToArray();

            Assert.That(choreography.Select(value => value.SemanticStyle).Distinct().Count(), Is.EqualTo(30));
            Assert.That(choreography.Select(value => value.TreeOrdinal).Distinct().Count(), Is.EqualTo(30));
            Assert.That(choreography.Select(value => value.SemanticVisualOrdinal).Distinct().Count(),
                Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(choreography.Select(value => value.SemanticPerformanceKey).Distinct().Count(),
                Is.EqualTo(FirstHourArtRegistry071.ExpectedRecipeCount));
            Assert.That(choreography.All(value => value.SemanticFamily != Battle3DArtSemanticFamily071.None),
                Is.True);
            Assert.That(choreography.All(value => value.SemanticStyle != Battle3DArtSemanticStyle071.None),
                Is.True);
            Assert.That(choreography.All(value => value.TraceRecipe != Battle3DArtTraceRecipe071.None),
                Is.True);
            Assert.That(choreography.All(value => !string.IsNullOrWhiteSpace(value.PerformanceCallout)), Is.True);
            Assert.That(choreography.All(value => !string.IsNullOrWhiteSpace(value.EffectSpriteId)), Is.True);
            Assert.That(choreography.All(value => value.SemanticTracePointCount >= 9 &&
                                                  value.SemanticTraceLayerCount >= 1 &&
                                                  value.SemanticBurstCount >= 16), Is.True);
            Assert.That(choreography.All(value => value.IsPresentationOnly && !value.ChangesCombatResolution),
                Is.True);

            foreach (var tree in choreography.GroupBy(value => value.TreeId))
            {
                Assert.That(tree.Select(value => value.SemanticStyle).Distinct().Count(), Is.EqualTo(1), tree.Key);
                Assert.That(tree.Select(value => value.SemanticPerformanceKey).Distinct().Count(), Is.EqualTo(4),
                    tree.Key);
                Assert.That(tree.Select(value => value.SemanticTraceRadius).Distinct().Count(), Is.EqualTo(4),
                    tree.Key);
                Assert.That(tree.Select(value => value.SemanticPoseTurnDegrees).Distinct().Count(), Is.EqualTo(4),
                    tree.Key);
            }
        }

        [TestCase("TREE_CA002_WPN_SWORD_N01", Battle3DArtSemanticFamily071.WeaponMelee,
            Battle3DArtSemanticStyle071.Sword, Battle3DArtTraceRecipe071.EdgeArc, "WEAPON_ARC")]
        [TestCase("TREE_CA002_WPN_BOW_N03", Battle3DArtSemanticFamily071.ProjectileWeapon,
            Battle3DArtSemanticStyle071.Bow, Battle3DArtTraceRecipe071.ProjectileFlight, "WEAPON_ARC")]
        [TestCase("TREE_CA002_WPN_ENGINEERING_N02", Battle3DArtSemanticFamily071.ToolEngineering,
            Battle3DArtSemanticStyle071.Engineering, Battle3DArtTraceRecipe071.ToolDiagram, "MYSTIC_BURST")]
        [TestCase("TREE_CA002_MYS_FLAME_N04", Battle3DArtSemanticFamily071.MysticElemental,
            Battle3DArtSemanticStyle071.Flame, Battle3DArtTraceRecipe071.ElementalSpiral, "MYSTIC_BURST")]
        [TestCase("TREE_CA002_MYS_RESTORATION_N03", Battle3DArtSemanticFamily071.Restoration,
            Battle3DArtSemanticStyle071.Restoration, Battle3DArtTraceRecipe071.HealingBloom,
            "RESTORATION_BLOOM")]
        [TestCase("TREE_CA002_MYS_WARDING_N02", Battle3DArtSemanticFamily071.Warding,
            Battle3DArtSemanticStyle071.Warding, Battle3DArtTraceRecipe071.WardDome, "GUARD_IMPACT")]
        [TestCase("TREE_CA002_ROLE_COMMANDER_N04", Battle3DArtSemanticFamily071.TacticalSupport,
            Battle3DArtSemanticStyle071.Commander, Battle3DArtTraceRecipe071.TacticalWave, "GUARD_IMPACT")]
        public void RepresentativeTreesResolveToReadableSemanticPerformanceFamilies(
            string artId,
            Battle3DArtSemanticFamily071 expectedFamily,
            Battle3DArtSemanticStyle071 expectedStyle,
            Battle3DArtTraceRecipe071 expectedTrace,
            string expectedEffect)
        {
            Assert.That(Battle3DArtChoreography071.TryCreate(artId, out var choreography), Is.True);
            Assert.That(choreography.SemanticFamily, Is.EqualTo(expectedFamily));
            Assert.That(choreography.SemanticStyle, Is.EqualTo(expectedStyle));
            Assert.That(choreography.TraceRecipe, Is.EqualTo(expectedTrace));
            Assert.That(choreography.EffectSpriteId, Is.EqualTo(expectedEffect));
            Assert.That(choreography.PerformanceCallout, Does.Contain("ART"));
            Assert.That(choreography.ClaimsAuthoredAnimationClipAsset, Is.False);
        }

        [Test]
        public void PerspectiveWorldBuildsDifferentGeometryForProjectileMysticHealingAndToolArts()
        {
            var method = typeof(M2Battle3DWorld).GetMethod(
                "BuildSemanticTracePoints071",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "The live 3D stage must consume the semantic trace recipe.");

            var bow = TracePoints(method, "TREE_CA002_WPN_BOW_N03");
            var flame = TracePoints(method, "TREE_CA002_MYS_FLAME_N03");
            var healing = TracePoints(method, "TREE_CA002_MYS_RESTORATION_N03");
            var tool = TracePoints(method, "TREE_CA002_WPN_ENGINEERING_N03");

            Assert.That(Vector3.Distance(bow[0], Vector3.zero), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(bow[bow.Length - 1], new Vector3(4f, 0f, 0f)),
                Is.LessThan(0.001f), "A Bow trace must visibly travel from actor to target.");
            Assert.That(TraceSignature(bow), Is.Not.EqualTo(TraceSignature(flame)));
            Assert.That(TraceSignature(flame), Is.Not.EqualTo(TraceSignature(healing)));
            Assert.That(TraceSignature(healing), Is.Not.EqualTo(TraceSignature(tool)));
        }

        [Test]
        public void RecipeLookupDoesNotInventAReactionOrChangeCombatProjection()
        {
            var source = Beat(
                BattleBeatFamily.CombatArt,
                BattleCameraShot.Impact,
                "Core already resolved this event.",
                true,
                "MARTIAL_HIT",
                "TREE_CA002_WPN_SWORD_N04");

            var directive = M2Battle3DPresentationPolicy.CreateDirective(source);

            Assert.That(directive.ArtChoreography071.NodeIndex, Is.EqualTo(4));
            Assert.That(directive.Action, Is.EqualTo(Battle3DAction.CombatArt));
            Assert.That(directive.EventType, Is.EqualTo("MARTIAL_HIT"));
            Assert.That(directive.IsSideStrike, Is.False);
            Assert.That(directive.IsBlindSide, Is.False);
            Assert.That(directive.UsesFirstHourArtPerformance071, Is.True);
        }

        [Test]
        public void UnknownOrNullBeatFallsBackToNonvisualCaptionOnlyDirective()
        {
            var unknown = Beat(BattleBeatFamily.IntentionallyNonVisual, BattleCameraShot.Wide, "Readable log truth.", false);
            var unknownDirective = M2Battle3DPresentationPolicy.CreateDirective(unknown);
            var nullDirective = M2Battle3DPresentationPolicy.CreateDirective(null);

            Assert.That(unknownDirective.Action, Is.EqualTo(Battle3DAction.CaptionOnly));
            Assert.That(unknownDirective.VisuallyStaged, Is.False);
            Assert.That(unknownDirective.Caption, Is.EqualTo("Readable log truth."));
            Assert.That(nullDirective.Action, Is.EqualTo(Battle3DAction.CaptionOnly));
            Assert.That(nullDirective.VisuallyStaged, Is.False);
            Assert.That(unknownDirective.HasFirstHourArtChoreography071, Is.False);
            Assert.That(nullDirective.HasFirstHourArtChoreography071, Is.False);
        }

        private static BattlePresentationBeat Beat(
            BattleBeatFamily family,
            BattleCameraShot camera,
            string caption,
            bool visuallyStaged = true,
            string eventType = null,
            string artId = "ART_TEST") =>
            new BattlePresentationBeat(
                12, 3, eventType ?? family.ToString(), family, camera,
                "PLAYER_UNION", "PLAYER_MEMBER", "ENEMY_UNION", "ENEMY_MEMBER",
                artId, 0, caption, "VFX_TEST", "SFX_TEST", visuallyStaged);

        private static Vector3[] TracePoints(MethodInfo method, string artId)
        {
            Assert.That(Battle3DArtChoreography071.TryCreate(artId, out var choreography), Is.True);
            return (Vector3[])method.Invoke(
                null,
                new object[] { choreography, 0, Vector3.zero, new Vector3(4f, 0f, 0f) });
        }

        private static string TraceSignature(Vector3[] points) =>
            string.Join("|", points.Select(value =>
                value.x.ToString("F3") + "," + value.y.ToString("F3") + "," + value.z.ToString("F3")));

        private static BattleCameraShot CameraFor(BattleBeatFamily family)
        {
            switch (family)
            {
                case BattleBeatFamily.Restoration: return BattleCameraShot.Support;
                case BattleBeatFamily.Breakthrough: return BattleCameraShot.Breakthrough;
                case BattleBeatFamily.Result: return BattleCameraShot.Result;
                case BattleBeatFamily.Guard: return BattleCameraShot.ActingUnion;
                default: return BattleCameraShot.Impact;
            }
        }
    }
}
