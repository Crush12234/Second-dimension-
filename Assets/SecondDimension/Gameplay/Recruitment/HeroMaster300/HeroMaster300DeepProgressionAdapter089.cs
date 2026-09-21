using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Joins a trusted Hero Master identity to the canonical tree choices already
    /// produced by Auto-Generation 010. Hero Master art-tree labels are presentation
    /// metadata and never enter saved progression; the generated profile's legal
    /// DeepProgression070 IDs remain the only gameplay authority.
    /// </summary>
    public sealed class HeroMaster300DeepProgressionAdapter089
    {
        private readonly HeroMaster300Catalog087 _heroes;
        private readonly RecruitAutoGenerator010 _generator;
        private readonly RecruitTreeProgressionService070 _progression;

        public HeroMaster300DeepProgressionAdapter089(
            HeroMaster300Catalog087 heroes,
            RecruitAutoGenerator010 generator,
            RecruitTreeProgressionService070 progression)
        {
            _heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _progression = progression ??
                           throw new ArgumentNullException(nameof(progression));
        }

        /// <summary>
        /// Regenerates and validates the immutable build profile already used when
        /// this recruit was signed. No Hero Master alias is translated or persisted.
        /// </summary>
        public GeneratedRecruitProfile010 DescribeValidatedProfile089(
            RecruitState recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            if (string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId) ||
                !_heroes.TryGetAcceptedHero(
                    recruit.AuthoredStableRecruitId,
                    out var hero) ||
                hero == null)
                throw new InvalidOperationException(
                    "Only a trusted Hero Master recruit can use the progression adapter.");

            var profile = _generator.Generate(recruit);
            ValidateGeneratedProfile089(profile);
            return profile;
        }

        /// <summary>
        /// Proves that every generated slot and legal tree resolves in the existing
        /// DeepProgression catalog and has the category expected by its slot.
        /// Returns the distinct canonical assigned trees in slot order.
        /// </summary>
        public IReadOnlyList<string> ValidateGeneratedProfile089(
            GeneratedRecruitProfile010 profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.LegalTreeIds == null || profile.LegalTreeIds.Count == 0)
                throw new InvalidOperationException(
                    "Generated recruit profile has no legal canonical trees.");

            var legal = new HashSet<string>(StringComparer.Ordinal);
            foreach (var legalTreeId in profile.LegalTreeIds)
            {
                if (!_progression.TryCanonicalTree(legalTreeId, out _))
                    throw new KeyNotFoundException(
                        "Generated profile names an unknown legal tree: " +
                        legalTreeId);
                legal.Add(legalTreeId);
            }

            var assigned = new List<string>();
            AddAssignedTree089(
                profile.WeaponTreeId,
                "WEAPON",
                required: true,
                legal,
                assigned);
            AddAssignedTree089(
                profile.PrimaryRoleTreeId,
                "ROLE",
                required: true,
                legal,
                assigned);
            AddAssignedTree089(
                profile.MysticTreeId,
                "MYSTIC",
                required: false,
                legal,
                assigned);
            AddAssignedTree089(
                profile.SecondaryRoleTreeId,
                "ROLE",
                required: false,
                legal,
                assigned);
            return assigned.AsReadOnly();
        }

        /// <summary>
        /// Unlocks Mystic, then Secondary Role, when that canonical generated slot is
        /// still locked. Weapon and Primary Role are signed foundations and cannot be
        /// granted by a duplicate merge.
        /// </summary>
        public bool TryUnlockNextEarnableTree089(
            RecruitState recruit,
            RecruitProgressionState progression,
            out string treeId,
            out RecruitProgressionState unlocked)
        {
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));
            var profile = DescribeValidatedProfile089(recruit);
            RequireSignedFoundations089(profile, progression);
            var earnable = EarnableTreeIds089(profile);
            foreach (var candidate in earnable)
            {
                if (progression.UnlockedTreeIds.Contains(candidate)) continue;
                treeId = candidate;
                unlocked = _progression.UnlockValidatedAssignedTree089(
                    progression,
                    earnable,
                    candidate);
                return true;
            }

            treeId = string.Empty;
            unlocked = progression;
            return false;
        }

        /// <summary>
        /// Explicit strict entry point used by regression tests and future callers.
        /// Unknown, legal-but-unassigned, Weapon, and Primary Role IDs are rejected.
        /// </summary>
        public RecruitProgressionState UnlockEarnableTree089(
            RecruitState recruit,
            RecruitProgressionState progression,
            string treeId)
        {
            if (progression == null)
                throw new ArgumentNullException(nameof(progression));
            if (string.IsNullOrWhiteSpace(treeId))
                throw new ArgumentException("Tree ID is required.", nameof(treeId));
            if (!_progression.TryCanonicalTree(treeId, out _))
                throw new KeyNotFoundException(
                    "Unknown canonical progression tree: " + treeId);

            var profile = DescribeValidatedProfile089(recruit);
            RequireSignedFoundations089(profile, progression);
            var earnable = EarnableTreeIds089(profile);
            if (!earnable.Contains(treeId))
                throw new InvalidOperationException(
                    "Tree is not an assigned earnable generated slot: " + treeId);
            return _progression.UnlockValidatedAssignedTree089(
                progression,
                earnable,
                treeId);
        }

        public IReadOnlyList<string> EarnableTreeIds089(
            GeneratedRecruitProfile010 profile)
        {
            ValidateGeneratedProfile089(profile);
            return new[] { profile.MysticTreeId, profile.SecondaryRoleTreeId }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToList()
                .AsReadOnly();
        }

        private void AddAssignedTree089(
            string treeId,
            string expectedCategory,
            bool required,
            ISet<string> legal,
            ICollection<string> assigned)
        {
            if (string.IsNullOrWhiteSpace(treeId))
            {
                if (required)
                    throw new InvalidOperationException(
                        "Generated recruit profile is missing its " +
                        expectedCategory + " foundation tree.");
                return;
            }
            if (!_progression.TryCanonicalTree(treeId, out var tree))
                throw new KeyNotFoundException(
                    "Generated profile names an unknown assigned tree: " + treeId);
            if (!StringComparer.Ordinal.Equals(tree.Category, expectedCategory))
                throw new InvalidOperationException(
                    "Generated tree " + treeId + " is not a " +
                    expectedCategory + " tree.");
            if (!legal.Contains(treeId))
                throw new InvalidOperationException(
                    "Generated tree is not legal for this recruit's class: " +
                    treeId);
            if (!assigned.Contains(treeId)) assigned.Add(treeId);
        }

        private static void RequireSignedFoundations089(
            GeneratedRecruitProfile010 profile,
            RecruitProgressionState progression)
        {
            if (!progression.UnlockedTreeIds.Contains(profile.WeaponTreeId) ||
                !progression.UnlockedTreeIds.Contains(profile.PrimaryRoleTreeId))
                throw new InvalidOperationException(
                    "Hero Master generated Weapon and Primary Role foundations " +
                    "must be initialized before duplicate progression.");
        }
    }
}
