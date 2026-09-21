using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Progression070
{
    public sealed class RecruitTreeSlotProgress070
    {
        internal RecruitTreeSlotProgress070(RecruitTreeSlotDefinition070 definition, bool unlocked)
        {
            SlotKind = definition.SlotKind;
            TreeId = definition.TreeId;
            InitiallyUnlocked = definition.InitiallyUnlocked;
            Earnable = definition.Earnable;
            PermanentlyFixed = definition.PermanentlyFixed;
            Unlocked = unlocked;
        }

        public RecruitTreeSlotKind070 SlotKind { get; }
        public string TreeId { get; }
        public bool InitiallyUnlocked { get; }
        public bool Earnable { get; }
        public bool PermanentlyFixed { get; }
        public bool Unlocked { get; }
    }

    public sealed class RecruitArtProgress070
    {
        internal RecruitArtProgress070(
            string artId,
            string treeId,
            bool knownNode,
            bool assignedTree,
            bool treeUnlocked,
            bool learned,
            int meaningfulUses,
            int masteryPoints)
        {
            ArtId = artId;
            TreeId = treeId;
            KnownNode = knownNode;
            AssignedTree = assignedTree;
            TreeUnlocked = treeUnlocked;
            Learned = learned;
            MeaningfulUses = meaningfulUses;
            MasteryPoints = masteryPoints;
        }

        public string ArtId { get; }
        public string TreeId { get; }
        public bool KnownNode { get; }
        public bool AssignedTree { get; }
        public bool TreeUnlocked { get; }
        public bool Learned { get; }
        public int MeaningfulUses { get; }
        public int MasteryPoints { get; }
        public bool Usable => KnownNode && AssignedTree && TreeUnlocked && Learned;
        public bool Dormant => (Learned || MasteryPoints > 0 || MeaningfulUses > 0) && !Usable;
    }

    public sealed class RecruitTreeProgressionSnapshot070
    {
        internal RecruitTreeProgressionSnapshot070(
            RecruitTreePlan070 plan,
            IReadOnlyList<RecruitTreeSlotProgress070> slots,
            IReadOnlyList<RecruitArtProgress070> arts)
        {
            Plan = plan;
            Slots = slots;
            Arts = arts;
        }

        public RecruitTreePlan070 Plan { get; }
        public IReadOnlyList<RecruitTreeSlotProgress070> Slots { get; }
        public IReadOnlyList<RecruitArtProgress070> Arts { get; }
    }

    /// <summary>
    /// Applies the Version 70 four-slot law without erasing older learned Arts or
    /// mastery. Weapon and primary-role trees start active. Mystic and secondary-role
    /// trees remain visible but earnable, and their prior mastery stays dormant.
    /// </summary>
    public sealed class RecruitTreeProgressionService070
    {
        private readonly DeepProgressionCatalog070 _catalog;

        public RecruitTreeProgressionService070(DeepProgressionCatalog070 catalog)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        public RecruitState NormalizeRecruit(RecruitState recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            return recruit.WithProgression(Normalize(AuthorityId(recruit), recruit.Progression));
        }

        public RecruitProgressionState Normalize(
            string signatureOrStableRecruitId,
            RecruitProgressionState progression)
        {
            if (progression == null) throw new ArgumentNullException(nameof(progression));
            var plan = _catalog.RecruitPlan(signatureOrStableRecruitId);
            var assignedTrees = new HashSet<string>(plan.Slots.Select(value => value.TreeId), StringComparer.Ordinal);
            var unlockedTrees = new HashSet<string>(StringComparer.Ordinal);
            foreach (var treeId in progression.UnlockedTreeIds)
                if (assignedTrees.Contains(treeId)) unlockedTrees.Add(treeId);
            foreach (var slot in plan.Slots)
                if (slot.InitiallyUnlocked) unlockedTrees.Add(slot.TreeId);

            // Existing learned IDs and mastery are never removed. Only malformed or
            // out-of-plan authored starting IDs are prevented from being newly seeded.
            var learned = new HashSet<string>(progression.LearnedArtIds, StringComparer.Ordinal);
            foreach (var nodeId in plan.NormalizedStartingNodeIds) learned.Add(nodeId);
            return progression
                .WithArts(Sorted(learned), progression.ArtMastery)
                .WithUnlockedTrees(Sorted(unlockedTrees));
        }

        public RecruitState UnlockEarnableTree(RecruitState recruit, string treeId)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            return recruit.WithProgression(UnlockEarnableTree(AuthorityId(recruit), recruit.Progression, treeId));
        }

        /// <summary>
        /// Exposes the existing immutable tree authority to narrow adapters. A caller
        /// may use this to prove that an externally named tree is canonical before it
        /// ever enters persistent progression.
        /// </summary>
        public bool TryCanonicalTree(
            string treeId,
            out DeepTreeDefinition070 tree) =>
            _catalog.TryTree(treeId, out tree);

        /// <summary>
        /// Unlocks a tree already assigned by an existing validated build profile.
        /// This is the same root-seeding operation used by authored RecruitPlans; the
        /// supplied assignment boundary prevents adapters from granting arbitrary or
        /// unknown trees and therefore does not create a second progression engine.
        /// </summary>
        public RecruitProgressionState UnlockValidatedAssignedTree089(
            RecruitProgressionState progression,
            IReadOnlyList<string> assignedTreeIds,
            string treeId)
        {
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));
            if (assignedTreeIds == null)
                throw new ArgumentNullException(nameof(assignedTreeIds));
            if (string.IsNullOrWhiteSpace(treeId))
                throw new ArgumentException("Tree ID is required.", nameof(treeId));
            if (!_catalog.TryTree(treeId, out var tree))
                throw new KeyNotFoundException(
                    "Unknown canonical progression tree: " + treeId);

            var assigned = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assignedTreeId in assignedTreeIds)
            {
                if (string.IsNullOrWhiteSpace(assignedTreeId))
                    throw new InvalidOperationException(
                        "Assigned progression tree IDs cannot be blank.");
                if (!_catalog.TryTree(assignedTreeId, out _))
                    throw new KeyNotFoundException(
                        "Unknown canonical assigned progression tree: " +
                        assignedTreeId);
                assigned.Add(assignedTreeId);
            }
            if (!assigned.Contains(treeId))
                throw new InvalidOperationException(
                    "Tree is not assigned to this generated recruit: " + treeId);

            var unlocked = new HashSet<string>(
                progression.UnlockedTreeIds,
                StringComparer.Ordinal)
            {
                treeId
            };
            var learned = new HashSet<string>(
                progression.LearnedArtIds,
                StringComparer.Ordinal)
            {
                tree.RootNodeId
            };
            return progression
                .WithArts(Sorted(learned), progression.ArtMastery)
                .WithUnlockedTrees(Sorted(unlocked));
        }

        public RecruitProgressionState UnlockEarnableTree(
            string signatureOrStableRecruitId,
            RecruitProgressionState progression,
            string treeId)
        {
            if (string.IsNullOrWhiteSpace(treeId)) throw new ArgumentException("Tree ID is required.", nameof(treeId));
            var plan = _catalog.RecruitPlan(signatureOrStableRecruitId);
            var slot = plan.Slots.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.TreeId, treeId));
            if (slot == null) throw new InvalidOperationException("Tree is not assigned to this recruit: " + treeId);
            var normalized = Normalize(signatureOrStableRecruitId, progression);
            if (!slot.Earnable)
            {
                if (slot.InitiallyUnlocked) return normalized;
                throw new InvalidOperationException("Tree cannot be earned: " + treeId);
            }

            var unlocked = new HashSet<string>(normalized.UnlockedTreeIds, StringComparer.Ordinal) { treeId };
            var learned = new HashSet<string>(normalized.LearnedArtIds, StringComparer.Ordinal)
            {
                _catalog.Tree(treeId).RootNodeId
            };
            return normalized
                .WithArts(Sorted(learned), normalized.ArtMastery)
                .WithUnlockedTrees(Sorted(unlocked));
        }

        public RecruitTreeProgressionSnapshot070 Describe(
            string signatureOrStableRecruitId,
            RecruitProgressionState progression)
        {
            var normalized = Normalize(signatureOrStableRecruitId, progression);
            var plan = _catalog.RecruitPlan(signatureOrStableRecruitId);
            var unlocked = new HashSet<string>(normalized.UnlockedTreeIds, StringComparer.Ordinal);
            var slotProgress = plan.Slots
                .Select(value => new RecruitTreeSlotProgress070(value, unlocked.Contains(value.TreeId)))
                .ToList()
                .AsReadOnly();

            var artIds = new HashSet<string>(normalized.LearnedArtIds, StringComparer.Ordinal);
            foreach (var mastery in normalized.ArtMastery) artIds.Add(mastery.ArtId);
            var arts = artIds
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => DescribeArt(plan, normalized, value))
                .ToList()
                .AsReadOnly();
            return new RecruitTreeProgressionSnapshot070(plan, slotProgress, arts);
        }

        public RecruitArtProgress070 DescribeArt(
            string signatureOrStableRecruitId,
            RecruitProgressionState progression,
            string artId)
        {
            var normalized = Normalize(signatureOrStableRecruitId, progression);
            return DescribeArt(_catalog.RecruitPlan(signatureOrStableRecruitId), normalized, artId);
        }

        private RecruitArtProgress070 DescribeArt(
            RecruitTreePlan070 plan,
            RecruitProgressionState progression,
            string artId)
        {
            if (string.IsNullOrWhiteSpace(artId)) throw new ArgumentException("Art ID is required.", nameof(artId));
            var known = _catalog.TryNode(artId, out var node);
            var treeId = known ? node.TreeId : string.Empty;
            var mastery = progression.ArtMastery.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.ArtId, artId));
            return new RecruitArtProgress070(
                artId,
                treeId,
                known,
                known && plan.ContainsTree(treeId),
                known && progression.UnlockedTreeIds.Contains(treeId),
                progression.LearnedArtIds.Contains(artId),
                mastery?.MeaningfulUses ?? 0,
                mastery?.MasteryPoints ?? 0);
        }

        private static string AuthorityId(RecruitState recruit)
        {
            if (!string.IsNullOrWhiteSpace(recruit.SignatureId)) return recruit.SignatureId;
            if (!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId)) return recruit.AuthoredStableRecruitId;
            throw new InvalidOperationException("A Signature Recruit needs a signature or stable authored recruit ID.");
        }

        private static IReadOnlyList<string> Sorted(IEnumerable<string> source) =>
            source.Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
    }
}
