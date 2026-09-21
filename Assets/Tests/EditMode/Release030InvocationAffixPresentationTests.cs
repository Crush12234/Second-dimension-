#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;

namespace SecondDimension.Tests.EditMode
{
    public sealed class Release030InvocationAffixPresentationTests
    {
        CampaignRegistry022 _registry;
        CampaignProgressionCommandService022 _service;

        [SetUp]
        public void SetUp()
        {
            _registry = CampaignRegistry022.LoadFromResources();
            _service = new CampaignProgressionCommandService022();
        }

        [Test]
        public void AllThirtyAffixesUseTheExactSixCategoryWhitelist()
        {
            Assert.That(_registry.Affixes.Count, Is.EqualTo(30));
            CollectionAssert.AreEquivalent(
                new[] { "COMBAT", "MYSTIC", "RESTORATION", "SUPPORT", "TACTICAL", "WARDING" },
                _registry.Affixes.Values.Select(value => value.forecastCategory).Distinct().ToArray());
            Assert.That(_registry.Affixes.Values.All(value =>
                InvocationArtifactAuthority022.IsCanonicalAffixCategory(value.forecastCategory) &&
                value.effectPermille > 0 && value.meaningfulUseRequired && value.zeroEffectSpamForbidden &&
                value.maximumCopiesPerArtifact == 1), Is.True);
            Assert.That(InvocationArtifactAuthority022.IsCanonicalAffixCategory("GUARD"), Is.False);
        }

        [Test]
        public void RegistryFailsClosedForAnUnknownAffixCategory()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var affixes = registry.Affixes as Dictionary<string, AffixDto022>;
            Assert.NotNull(affixes, "The loaded registry should retain its owned mutable dictionary during validation.");
            var id = affixes.Keys.OrderBy(value => value, StringComparer.Ordinal).First();
            var original = affixes[id];
            affixes[id] = new AffixDto022
            {
                affixId = original.affixId,
                displayName = original.displayName,
                forecastCategory = "GUARD",
                effectPermille = original.effectPermille,
                meaningfulUseRequired = true,
                zeroEffectSpamForbidden = true,
                maximumCopiesPerArtifact = 1
            };
            Assert.Throws<InvalidOperationException>(() => registry.ValidateOrThrow());
        }

        [Test]
        public void RegistryFailsClosedForANonPreservingArtifactPath()
        {
            var registry = CampaignRegistry022.LoadFromResources();
            var path = registry.ArtifactPaths.Values.OrderBy(value => value.pathId, StringComparer.Ordinal).First();
            path.preserveArtifactInstance = false;
            Assert.Throws<InvalidOperationException>(() => registry.ValidateOrThrow());
        }

        [Test]
        public void ThirtyAffixesPageDeterministicallyEightEightEightSix()
        {
            var values = _registry.Affixes.Values.OrderBy(value => value.affixId, StringComparer.Ordinal)
                .Select(value => new InvocationAffixView022
                {
                    AffixId = value.affixId,
                    DisplayName = value.displayName,
                    ForecastCategory = value.forecastCategory,
                    EffectPermille = value.effectPermille
                }).ToArray();
            Assert.That(InvocationAffixSelectionRules022.PageCount(values.Length), Is.EqualTo(4));
            CollectionAssert.AreEqual(new[] { 8, 8, 8, 6 }, Enumerable.Range(0, 4)
                .Select(page => InvocationAffixSelectionRules022.Page(values, page).Count).ToArray());
            CollectionAssert.AreEqual(values.Select(value => value.AffixId), Enumerable.Range(0, 4)
                .SelectMany(page => InvocationAffixSelectionRules022.Page(values, page))
                .Select(value => value.AffixId));
            Assert.That(InvocationAffixSelectionRules022.ClampPage(99, values.Length), Is.EqualTo(3));
        }

        [Test]
        public void SelectionAllowsZeroThroughTwoAndNeverSilentlyReplacesASelectedAffix()
        {
            IReadOnlyList<string> selected = Array.Empty<string>();
            selected = InvocationAffixSelectionRules022.Toggle(selected, "INVOCATION_AFFIX022_02");
            selected = InvocationAffixSelectionRules022.Toggle(selected, "INVOCATION_AFFIX022_01");
            CollectionAssert.AreEqual(new[] { "INVOCATION_AFFIX022_01", "INVOCATION_AFFIX022_02" }, selected);
            var refusedThird = InvocationAffixSelectionRules022.Toggle(selected, "INVOCATION_AFFIX022_03");
            CollectionAssert.AreEqual(selected, refusedThird);
            selected = InvocationAffixSelectionRules022.Toggle(selected, "INVOCATION_AFFIX022_01");
            selected = InvocationAffixSelectionRules022.Toggle(selected, "INVOCATION_AFFIX022_03");
            CollectionAssert.AreEqual(new[] { "INVOCATION_AFFIX022_02", "INVOCATION_AFFIX022_03" }, selected);
        }

        [Test]
        public void PresentationContractExposesExactTwoArgumentCraftCommand()
        {
            var method = typeof(ICampaignProgressionPresentationCoordinator022).GetMethod(
                "CraftInvocationArtifact022", new[] { typeof(string), typeof(IReadOnlyList<string>) });
            Assert.NotNull(method);
            Assert.NotNull(typeof(ICampaignProgressionPresentationCoordinator022).GetMethod(
                "EvolveInvocationArtifact022", new[] { typeof(string) }));
        }

        [Test]
        public void CoordinatorProjectsAllThirtyAffixesAndAllTwentyFourBasesInStableIdOrder()
        {
            var coordinator = new M1RuntimeCoordinator(null,
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "invocation_projection_030_" +
                    Guid.NewGuid().ToString("N") + ".json"));
            var campaignField = typeof(M1RuntimeCoordinator).GetField("_campaign",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(campaignField);
            campaignField.SetValue(coordinator, CampaignFactory.CreateM0Proof(30024));
            var state = coordinator.CampaignProgression022;
            Assert.That(state.IsAvailable, Is.True, state.Error);
            Assert.That(state.InvocationAffixes.Count, Is.EqualTo(30));
            Assert.That(state.InvocationArtifactBases.Count, Is.EqualTo(24));
            CollectionAssert.AreEqual(state.InvocationAffixes.Select(value => value.AffixId)
                .OrderBy(value => value, StringComparer.Ordinal), state.InvocationAffixes.Select(value => value.AffixId));
            CollectionAssert.AreEqual(state.InvocationArtifactBases.Select(value => value.BaseId)
                .OrderBy(value => value, StringComparer.Ordinal), state.InvocationArtifactBases.Select(value => value.BaseId));
        }

        [Test]
        public void CraftPreservesExactKnownSelectionAndRejectsUnknownDuplicateOrThirdAffixes()
        {
            var campaign = WithAllMaterials(CampaignFactory.CreateM0Proof(30022));
            const string baseId = "INVOCATION_BASE022_01_01";
            var accepted = Require(_service.CraftInvocationArtifact(campaign, _registry, baseId,
                new[] { "INVOCATION_AFFIX022_02", "INVOCATION_AFFIX022_01" }));
            var artifact = Progression(accepted).InvocationArtifacts.Single();
            CollectionAssert.AreEqual(new[] { "INVOCATION_AFFIX022_01", "INVOCATION_AFFIX022_02" },
                artifact.AffixIds);

            AssertFailure(_service.CraftInvocationArtifact(campaign, _registry, baseId,
                new[] { "INVOCATION_AFFIX022_UNKNOWN" }), "CAMPAIGN022_INVOCATION_AFFIX_UNKNOWN");
            AssertFailure(_service.CraftInvocationArtifact(campaign, _registry, baseId,
                new[] { "INVOCATION_AFFIX022_01", "INVOCATION_AFFIX022_01" }),
                "CAMPAIGN022_INVOCATION_AFFIX_DUPLICATE");
            AssertFailure(_service.CraftInvocationArtifact(campaign, _registry, baseId,
                new[] { "INVOCATION_AFFIX022_01", "INVOCATION_AFFIX022_02", "INVOCATION_AFFIX022_03" }),
                "CAMPAIGN022_INVOCATION_AFFIX_COUNT_INVALID");
        }

        [Test]
        public void EquippedOnlyArtifactRemainsOwnedAndEvolvesOnItsExactPath()
        {
            var crafted = Require(_service.CraftInvocationArtifact(
                WithAllMaterials(CampaignFactory.CreateM0Proof(30023)), _registry,
                "INVOCATION_BASE022_01_01", new[] { "INVOCATION_AFFIX022_01" }));
            var artifact = Progression(crafted).InvocationArtifacts.Single();
            var item = crafted.Guild.Inventory.Single(value => value.InstanceId == artifact.InstanceId);
            var recruit = new RecruitState("INVOCATION_OWNER_030", 100, 100, 30, 30).WithEquipment(
                new EquipmentLoadoutState(new[]
                {
                    new EquipmentSlotAssignmentState(EquipmentSlotIds.ToolRelic, item)
                }));
            var guild = crafted.Guild.With(crafted.Guild.TreasuryXp, new[] { recruit }, crafted.Guild.Unions,
                crafted.Guild.Inventory.Where(value => value.InstanceId != item.InstanceId).ToArray(),
                crafted.Guild.Development);
            crafted = crafted.With(guild, crafted.OpeningFlow);
            var state = Progression(crafted);
            artifact = artifact.With(resonance: 20);
            state = state.With(invocationArtifacts: new[] { artifact }, abyssFloors: new[]
            {
                new AbyssFloorProgressState022("ABYSS_FLOOR_002_ARROW_RAIN_FIELD", 1, 1, true, false,
                    Array.Empty<string>())
            }, lastCheckpointId: "invocation_evolution_ready_030");
            crafted = WithProgression(crafted, state);

            var owned = M1RuntimeCoordinator.OwnedEquipmentForPresentation022(crafted);
            Assert.That(owned.Count(value => value.InstanceId == artifact.InstanceId), Is.EqualTo(1));
            Assert.That(crafted.Guild.Inventory.Any(value => value.InstanceId == artifact.InstanceId), Is.False);
            var evolved = Require(_service.EvolveInvocationArtifact(crafted, _registry, artifact.InstanceId));
            var evolvedArtifact = Progression(evolved).InvocationArtifacts.Single();
            Assert.That(evolvedArtifact.InstanceId, Is.EqualTo(artifact.InstanceId));
            Assert.That(evolvedArtifact.EvolutionPathId, Is.EqualTo("INVOCATION_PATH022_01"));
            Assert.That(evolvedArtifact.EvolutionStage, Is.EqualTo(1));

            var malformed = WithProgression(crafted, state.With(invocationArtifacts: new[]
            {
                artifact.With(evolutionPathId: "INVOCATION_PATH022_02")
            }, lastCheckpointId: "forged_invocation_path_030"));
            AssertFailure(_service.EvolveInvocationArtifact(malformed, _registry, artifact.InstanceId),
                "CAMPAIGN022_INVOCATION_PATH_MISMATCH");
        }

        CampaignState WithAllMaterials(CampaignState campaign)
        {
            var ids = _registry.ArtifactBases.Values.SelectMany(value => value.materialCosts ??
                Array.Empty<MaterialCostDto022>()).Select(value => value.materialId).Distinct(StringComparer.Ordinal);
            var materials = ids.OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => new WorldMaterialAmount020(value, 100)).ToArray();
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(worldMaterials: materials, lastCheckpointId: "materials_030");
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: playable.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: strategic.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignProgressionState022 Progression(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.Progression022;

        static CampaignState WithProgression(CampaignState campaign, CampaignProgressionState022 state)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020.With(progression022: state, replaceProgression022: true,
                lastCheckpointId: state.LastCheckpointId);
            progress = progress.With(playable020: playable, replacePlayable020: true,
                lastCheckpointId: state.LastCheckpointId);
            strategic = strategic.With(campaign019: progress, replaceCampaign019: true,
                lastCheckpointId: state.LastCheckpointId);
            city = city.With(strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: state.LastCheckpointId);
            return campaign.With(campaign.Guild.WithGuildCity(city), campaign.OpeningFlow);
        }

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }

        static void AssertFailure(Result<CampaignState> result, string error)
        {
            Assert.That(result.IsSuccess, Is.False);
            CollectionAssert.Contains(result.Errors, error);
        }
    }
}
#endif
