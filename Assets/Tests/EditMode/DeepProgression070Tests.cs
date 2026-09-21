using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.State;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class DeepProgression070Tests
    {
        private DeepProgressionCatalog070 _catalog;
        private RecruitTreeProgressionService070 _service;

        [SetUp]
        public void SetUp()
        {
            var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
            _catalog = DeepProgressionCatalog070.LoadFromContentRoot(contentRoot);
            _service = new RecruitTreeProgressionService070(_catalog);
        }

        [Test]
        public void CatalogPinsAllDeepAuthorityCountsAndBuildsRoleFallbacks()
        {
            Assert.That(_catalog.SignatureRecruitCount, Is.EqualTo(300));
            Assert.That(_catalog.TreeCount, Is.EqualTo(30));
            Assert.That(_catalog.NodeCount, Is.EqualTo(360));
            Assert.That(_catalog.WeaponCount, Is.EqualTo(264));
            Assert.That(_catalog.WeaponArtBindingCount, Is.EqualTo(264));
            Assert.That(_catalog.AuthoredArtProfileCount, Is.EqualTo(240));
            Assert.That(_catalog.RuntimeArtCount, Is.EqualTo(360));
            Assert.That(_catalog.DerivedRoleArtCount, Is.EqualTo(120));
            Assert.That(_catalog.RuntimeArts.Count(value => !value.IsDerivedRoleFallback), Is.EqualTo(240));
            Assert.That(_catalog.RuntimeArt("TREE_CA002_ROLE_GUARDIAN_N01").IsDerivedRoleFallback, Is.True);
            Assert.That(_catalog.RuntimeArt("TREE_CA002_WPN_SWORD_N01").IsDerivedRoleFallback, Is.False);
        }

        [Test]
        public void EverySignatureRecruitHasTwoActiveAndTwoEarnableTreeSlots()
        {
            var plan = _catalog.RecruitPlan("SIG_W01_01");
            Assert.That(plan.Slots.Count, Is.EqualTo(4));
            Assert.That(plan.Slot(RecruitTreeSlotKind070.Weapon).InitiallyUnlocked, Is.True);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.Weapon).PermanentlyFixed, Is.True);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.PrimaryRole).InitiallyUnlocked, Is.True);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.Mystic).InitiallyUnlocked, Is.False);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.Mystic).Earnable, Is.True);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.SecondaryRole).InitiallyUnlocked, Is.False);
            Assert.That(plan.Slot(RecruitTreeSlotKind070.SecondaryRole).Earnable, Is.True);
            Assert.That(_catalog.RecruitPlan(plan.StableRecruitId), Is.SameAs(plan));
        }

        [Test]
        public void AuthoredStartingNodesAreNormalizedToTheTwoActiveTrees()
        {
            // This inherited manifest contains an Earth node outside its assigned four
            // trees. Version 70 records the bad reference and never seeds it as active.
            var plan = _catalog.RecruitPlan("SIG_W01_03");
            Assert.That(plan.RejectedStartingNodeIds, Does.Contain("TREE_CA002_MYS_EARTH_N01"));
            Assert.That(plan.NormalizedStartingNodeIds, Does.Not.Contain("TREE_CA002_MYS_EARTH_N01"));
            Assert.That(plan.NormalizedStartingNodeIds, Does.Contain(plan.Slot(RecruitTreeSlotKind070.Weapon).TreeId + "_N01"));
            Assert.That(plan.NormalizedStartingNodeIds, Does.Contain(plan.Slot(RecruitTreeSlotKind070.PrimaryRole).TreeId + "_N01"));
            Assert.That(plan.NormalizedStartingNodeIds.All(nodeId =>
                {
                    var treeId = _catalog.Node(nodeId).TreeId;
                    return treeId == plan.Slot(RecruitTreeSlotKind070.Weapon).TreeId ||
                           treeId == plan.Slot(RecruitTreeSlotKind070.PrimaryRole).TreeId;
                }), Is.True);
        }

        [Test]
        public void NormalizePreservesOldAndDormantMasteryWithoutMakingItUsable()
        {
            var plan = _catalog.RecruitPlan("SIG_W01_03");
            var mysticRoot = _catalog.Tree(plan.Slot(RecruitTreeSlotKind070.Mystic).TreeId).RootNodeId;
            var inheritedOutsidePlan = "TREE_CA002_MYS_EARTH_N01";
            var state = RecruitProgressionState.Default()
                .WithArts(
                    new[] { mysticRoot, inheritedOutsidePlan, "LEGACY_UNKNOWN_ART" },
                    new[] { new RecruitArtMasteryState(mysticRoot, "Restoration", 11, 87) })
                .WithUnlockedTrees(new[] { "TREE_NOT_AUTHORIZED" });

            var normalized = _service.Normalize(plan.SignatureRecruitId, state);
            Assert.That(normalized.LearnedArtIds, Does.Contain(mysticRoot));
            Assert.That(normalized.LearnedArtIds, Does.Contain(inheritedOutsidePlan));
            Assert.That(normalized.LearnedArtIds, Does.Contain("LEGACY_UNKNOWN_ART"));
            Assert.That(normalized.ArtMastery.Single().MasteryPoints, Is.EqualTo(87));
            Assert.That(normalized.UnlockedTreeIds, Does.Contain(plan.Slot(RecruitTreeSlotKind070.Weapon).TreeId));
            Assert.That(normalized.UnlockedTreeIds, Does.Contain(plan.Slot(RecruitTreeSlotKind070.PrimaryRole).TreeId));
            Assert.That(normalized.UnlockedTreeIds, Does.Not.Contain(plan.Slot(RecruitTreeSlotKind070.Mystic).TreeId));
            Assert.That(normalized.UnlockedTreeIds, Does.Not.Contain("TREE_NOT_AUTHORIZED"));

            var art = _service.DescribeArt(plan.SignatureRecruitId, normalized, mysticRoot);
            Assert.That(art.Dormant, Is.True);
            Assert.That(art.Usable, Is.False);
            Assert.That(art.MasteryPoints, Is.EqualTo(87));
        }

        [Test]
        public void EarnedTreeUnlocksItsRootAndReactivatesDormantMastery()
        {
            var plan = _catalog.RecruitPlan("SIG_W01_03");
            var mysticTree = plan.Slot(RecruitTreeSlotKind070.Mystic).TreeId;
            var mysticRoot = _catalog.Tree(mysticTree).RootNodeId;
            var state = RecruitProgressionState.Default().WithArts(
                new[] { mysticRoot },
                new[] { new RecruitArtMasteryState(mysticRoot, "Restoration", 4, 29) });

            var unlocked = _service.UnlockEarnableTree(plan.SignatureRecruitId, state, mysticTree);
            Assert.That(unlocked.UnlockedTreeIds, Does.Contain(mysticTree));
            Assert.That(unlocked.LearnedArtIds, Does.Contain(mysticRoot));
            Assert.That(_service.DescribeArt(plan.SignatureRecruitId, unlocked, mysticRoot).Usable, Is.True);
            Assert.That(_service.DescribeArt(plan.SignatureRecruitId, unlocked, mysticRoot).MasteryPoints, Is.EqualTo(29));
        }

        [Test]
        public void OnlyTheTwoEarnableAssignedTreesCanBeUnlocked()
        {
            var plan = _catalog.RecruitPlan("SIG_W01_01");
            Assert.Throws<InvalidOperationException>(() =>
                _service.UnlockEarnableTree(plan.SignatureRecruitId, RecruitProgressionState.Default(), "TREE_CA002_MYS_EARTH"));
            var weaponTree = plan.Slot(RecruitTreeSlotKind070.Weapon).TreeId;
            var normalized = _service.UnlockEarnableTree(plan.SignatureRecruitId, RecruitProgressionState.Default(), weaponTree);
            Assert.That(normalized.UnlockedTreeIds, Does.Contain(weaponTree));
        }

        [Test]
        public void OldProgressionJsonDefaultsUnlockedTreesAndXpGrowthPreservesThem()
        {
            var oldJson = JObject.FromObject(RecruitProgressionState.Default());
            oldJson.Remove("UnlockedTreeIds");
            var restored = JsonConvert.DeserializeObject<RecruitProgressionState>(oldJson.ToString(Formatting.None));
            Assert.That(restored, Is.Not.Null);
            Assert.That(restored.UnlockedTreeIds, Is.Empty);

            var withTree = restored.WithUnlockedTrees(new[] { "TREE_CA002_WPN_SWORD" });
            var grown = withTree.GainPersonalXp(100, "CLASS_WARRIOR");
            Assert.That(grown.UnlockedTreeIds, Is.EquivalentTo(withTree.UnlockedTreeIds));
        }
    }
}
