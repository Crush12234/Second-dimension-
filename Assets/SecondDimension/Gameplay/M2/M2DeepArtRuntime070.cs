using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    public sealed class M2DeepLearningOpportunity070
    {
        internal M2DeepLearningOpportunity070(
            string treeId,
            string sourceArtId,
            string targetArtId,
            string targetArtName,
            int currentPoints,
            int requiredPoints,
            int currentLevel,
            int requiredLevel)
        {
            TreeId = treeId;
            SourceArtId = sourceArtId;
            TargetArtId = targetArtId;
            TargetArtName = targetArtName;
            CurrentPoints = Math.Max(0, currentPoints);
            RequiredPoints = Math.Max(0, requiredPoints);
            CurrentLevel = Math.Max(1, currentLevel);
            RequiredLevel = Math.Max(1, requiredLevel);
        }

        public string TreeId { get; }
        public string SourceArtId { get; }
        public string TargetArtId { get; }
        public string TargetArtName { get; }
        public int CurrentPoints { get; }
        public int RequiredPoints { get; }
        public int CurrentLevel { get; }
        public int RequiredLevel { get; }
        public bool PointGateMet => CurrentPoints >= RequiredPoints;
        public bool LevelGateMet => CurrentLevel >= RequiredLevel;
        public bool CanLearnNow => PointGateMet && LevelGateMet;

        public string PlayerFacingProgress
        {
            get
            {
                var points = Math.Min(CurrentPoints, RequiredPoints) + "/" + RequiredPoints + " discovery";
                return LevelGateMet
                    ? TargetArtName + " · " + points
                    : TargetArtName + " · " + points + " · level " + RequiredLevel + " required";
            }
        }
    }

    /// <summary>
    /// Connects the authored four-tree recruit plans to cinematic Union combat.
    /// The player still selects a complete Union Forecast; this bridge only expands
    /// each member's legal predicted Art pool and applies authority-backed learning.
    /// </summary>
    public static class M2DeepArtRuntime070
    {
        public static IReadOnlyList<string> BattleLearnedArts(
            RecruitState recruit,
            M2CombatContent content)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            var progression = recruit.Progression;
            if (content?.DeepProgression == null)
                return progression.LearnedArtIds;

            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var artId in progression.LearnedArtIds)
                if (!content.DeepProgression.TryNode(artId, out _))
                    result.Add(artId);

            if (RejectedHeroMasterSource096(recruit, content))
                return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            if (TryAuthorityId(recruit, content, out var authorityId))
            {
                try
                {
                    var snapshot = content.DeepTreeProgression.Describe(
                        authorityId,
                        progression);
                    foreach (var art in snapshot.Arts)
                        if (art.Usable && content.Arts.ContainsKey(art.ArtId))
                            result.Add(art.ArtId);
                }
                catch (KeyNotFoundException)
                {
                    // Unknown authored authority remains limited to non-deep legacy Arts.
                }
                return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
            }

            var activeTrees = ProceduralBattleTrees(recruit, content);
            var equipmentTags = EquipmentTags(recruit);
            foreach (var artId in progression.LearnedArtIds)
            {
                if (!content.DeepProgression.TryNode(artId, out var node) ||
                    !activeTrees.Contains(node.TreeId) ||
                    !IsEquipmentLegal(node, equipmentTags) ||
                    !content.Arts.ContainsKey(artId))
                    continue;
                result.Add(artId);
            }
            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        public static void MergeActiveLearnedArts(
            RecruitState recruit,
            M2CombatContent content,
            IList<string> learnedArtIds,
            IList<BattleArtProgressState> artProgress)
        {
            if (recruit == null || content?.DeepProgression == null || learnedArtIds == null || artProgress == null)
                return;
            if (RejectedHeroMasterSource096(recruit, content)) return;
            if (!TryAuthorityId(recruit, content, out var authorityId)) return;

            RecruitTreeProgressionSnapshot070 snapshot;
            try
            {
                snapshot = content.DeepTreeProgression.Describe(authorityId, recruit.Progression);
            }
            catch (KeyNotFoundException)
            {
                return;
            }

            foreach (var art in snapshot.Arts)
            {
                if (!art.Usable || !content.Arts.ContainsKey(art.ArtId)) continue;
                AddUnique(learnedArtIds, art.ArtId);
                if (FindProgress(artProgress, art.ArtId) != null) continue;
                artProgress.Add(new BattleArtProgressState(
                    art.ArtId,
                    content.Art(art.ArtId).Discipline,
                    art.MeaningfulUses,
                    art.MasteryPoints));
            }
        }

        public static bool TryGetNextLearning(
            RecruitState recruit,
            BattleMemberState member,
            string sourceArtId,
            int projectedMasteryGain,
            M2CombatContent content,
            out M2DeepLearningOpportunity070 opportunity)
        {
            opportunity = null;
            if (recruit == null || member == null || content?.DeepProgression == null ||
                string.IsNullOrWhiteSpace(sourceArtId) ||
                !content.DeepProgression.TryNode(sourceArtId, out var sourceNode))
                return false;

            var proceduralTreeAuthority = false;
            if (RejectedHeroMasterSource096(recruit, content)) return false;
            if (TryAuthorityId(recruit, content, out var authorityId))
            {
                RecruitProgressionState normalized;
                RecruitTreePlan070 plan;
                try
                {
                    normalized = content.DeepTreeProgression.Normalize(authorityId, recruit.Progression);
                    plan = content.DeepProgression.RecruitPlan(authorityId);
                }
                catch (KeyNotFoundException)
                {
                    return false;
                }

                if (!plan.ContainsTree(sourceNode.TreeId) ||
                    !normalized.UnlockedTreeIds.Contains(sourceNode.TreeId))
                    return false;
            }
            else
            {
                proceduralTreeAuthority = HasProceduralTreeAuthority(
                    recruit,
                    member,
                    sourceNode,
                    content);
                if (!proceduralTreeAuthority) return false;
            }

            var learned = new HashSet<string>(member.LearnedArtIds, StringComparer.Ordinal);
            var tree = content.DeepProgression.Tree(sourceNode.TreeId);
            DeepNodeDefinition070 target = null;
            foreach (var nodeId in tree.NodeIds)
            {
                if (learned.Contains(nodeId)) continue;
                var candidate = content.DeepProgression.Node(nodeId);
                if (candidate.PrerequisiteNodeIds.Any(value => !learned.Contains(value))) continue;
                if (proceduralTreeAuthority &&
                    !IsEquipmentLegal(candidate, member.EquipmentTags))
                    continue;
                target = candidate;
                break;
            }
            if (target == null) return false;

            var points = Math.Max(0, projectedMasteryGain);
            foreach (var progress in member.ArtProgress)
            {
                if (!content.DeepProgression.TryNode(progress.ArtId, out var progressNode) ||
                    !StringComparer.Ordinal.Equals(progressNode.TreeId, sourceNode.TreeId))
                    continue;
                points = checked(points + progress.MasteryPoints);
            }
            opportunity = new M2DeepLearningOpportunity070(
                sourceNode.TreeId,
                sourceArtId,
                target.NodeId,
                target.DisplayName,
                points,
                target.DiscoveryMeaningfulUsePoints,
                recruit.Progression.Level,
                target.SuggestedCharacterLevel);
            return true;
        }

        public static bool IsDeepArt(M2CombatContent content, string artId) =>
            content?.DeepProgression != null && content.DeepProgression.TryNode(artId, out _);

        /// <summary>
        /// Early Field Medic training earns the existing, costed Stand Again Art.
        /// Called only after meaningful healing commits; old saves with N02 use the
        /// same path on their next meaningful Field Medic heal, never during loading.
        /// </summary>
        public static bool CanEarnFieldMedicRevival091(
            RecruitState recruit, BattleMemberState member, string sourceArtId, M2CombatContent content)
        {
            const string treeId = "TREE_CA002_ROLE_FIELD_MEDIC";
            const string rootId = treeId + "_N01";
            const string trainingId = treeId + "_N02";
            if (recruit == null || member == null || content?.DeepProgression == null ||
                member.Downed || member.LearnedArtIds.Contains("ART_STAND_AGAIN") ||
                !member.LearnedArtIds.Contains(rootId) || !member.LearnedArtIds.Contains(trainingId) ||
                !member.LearnedArtIds.Contains(sourceArtId) ||
                !content.DeepProgression.TryNode(sourceArtId, out var source) || source.TreeId != treeId ||
                !content.DeepProgression.TryNode(trainingId, out var training) ||
                recruit.Progression.Level < training.SuggestedCharacterLevel ||
                !content.Arts.TryGetValue("ART_STAND_AGAIN", out var revival) ||
                !M2BattleCommandService.IsRevivalArtForVerification080(revival) ||
                !revival.RequiredEquipmentTags.Any(member.EquipmentTags.Contains)) return false;
            if (RejectedHeroMasterSource096(recruit, content)) return false;

            if (TryAuthorityId(recruit, content, out var authorityId))
            {
                try
                {
                    var normalized = content.DeepTreeProgression.Normalize(authorityId, recruit.Progression);
                    if (!content.DeepProgression.RecruitPlan(authorityId).ContainsTree(treeId) ||
                        !normalized.UnlockedTreeIds.Contains(treeId)) return false;
                }
                catch (KeyNotFoundException) { return false; }
            }
            else if (!HasProceduralTreeAuthority(recruit, member, source, content) ||
                     !ProceduralBattleTrees(recruit, content).Contains(treeId)) return false;

            var mastery = member.ArtProgress.Where(progress =>
                content.DeepProgression.TryNode(progress.ArtId, out var node) && node.TreeId == treeId)
                .Sum(progress => (long)progress.MasteryPoints);
            return mastery >= training.DiscoveryMeaningfulUsePoints;
        }

        private static bool RejectedHeroMasterSource096(RecruitState recruit, M2CombatContent content) =>
            HeroMasterGeneratedBattleAuthority096.ClaimsHeroMasterSource096(recruit) &&
            content?.HeroMasterGeneratedAuthority096?.TryDescribe(recruit, out _) != true;

        private static bool TryAuthorityId(RecruitState recruit, M2CombatContent content, out string authorityId)
        {
            if (content?.HeroMasterGeneratedAuthority096?.TryDescribe(recruit, out _) == true)
            {
                authorityId = string.Empty;
                return false;
            }
            authorityId = !string.IsNullOrWhiteSpace(recruit.SignatureId)
                ? recruit.SignatureId
                : recruit.AuthoredStableRecruitId;
            return !string.IsNullOrWhiteSpace(authorityId);
        }

        private static bool HasProceduralTreeAuthority(
            RecruitState recruit,
            BattleMemberState member,
            DeepNodeDefinition070 sourceNode,
            M2CombatContent content)
        {
            // A generated recruit has no entry in the fixed 300-recruit registry.
            // Its persisted, deterministic root Arts are therefore the authority for
            // the trees it lawfully owns. Requiring the root in both campaign and
            // battle state prevents a forged transient Art from opening another tree.
            if (recruit.OriginKind != RecruitOriginKind.Procedural ||
                recruit.AuthorityKind != RecruitAuthorityKind.Normal ||
                string.IsNullOrWhiteSpace(recruit.CanonicalApplicantJson) ||
                !string.IsNullOrWhiteSpace(recruit.SignatureId) ||
                (!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId) &&
                 content.HeroMasterGeneratedAuthority096?.TryDescribe(recruit, out _) != true))
                return false;
            if (!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId) &&
                !ProceduralBattleTrees(recruit, content).Contains(sourceNode.TreeId)) return false;

            var rootNodeId = content.DeepProgression.Tree(sourceNode.TreeId).RootNodeId;
            return Contains(recruit.Progression.LearnedArtIds, rootNodeId) &&
                   Contains(member.LearnedArtIds, rootNodeId) &&
                   Contains(member.LearnedArtIds, sourceNode.NodeId) &&
                   IsEquipmentLegal(sourceNode, member.EquipmentTags);
        }

        private static HashSet<string> ProceduralBattleTrees(
            RecruitState recruit,
            M2CombatContent content)
        {
            if (content.HeroMasterGeneratedAuthority096 != null &&
                content.HeroMasterGeneratedAuthority096.TryDescribe(recruit, out var profile096))
                return content.HeroMasterGeneratedAuthority096.ActiveTrees(recruit, profile096);
            var active = new HashSet<string>(
                recruit.Progression.UnlockedTreeIds,
                StringComparer.Ordinal);
            var learnedTrees = recruit.Progression.LearnedArtIds
                .Select(value => content.DeepProgression.TryNode(value, out var node)
                    ? node.TreeId
                    : string.Empty)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            if (!ContainsTreeCategory(active, "WEAPON", content))
            {
                var equipmentTags = EquipmentTags(recruit);
                var legalWeaponTree = learnedTrees.FirstOrDefault(treeId =>
                {
                    var tree = content.DeepProgression.Tree(treeId);
                    return StringComparer.Ordinal.Equals(tree.Category, "WEAPON") &&
                           Contains(recruit.Progression.LearnedArtIds, tree.RootNodeId) &&
                           IsEquipmentLegal(
                               content.DeepProgression.Node(tree.RootNodeId),
                               equipmentTags);
                });
                if (string.IsNullOrWhiteSpace(legalWeaponTree))
                    legalWeaponTree = learnedTrees.FirstOrDefault(treeId =>
                        StringComparer.Ordinal.Equals(
                            content.DeepProgression.Tree(treeId).Category,
                            "WEAPON"));
                if (!string.IsNullOrWhiteSpace(legalWeaponTree)) active.Add(legalWeaponTree);
            }

            if (!ContainsTreeCategory(active, "ROLE", content))
            {
                var roleTree = learnedTrees.FirstOrDefault(treeId =>
                    StringComparer.Ordinal.Equals(
                        content.DeepProgression.Tree(treeId).Category,
                        "ROLE"));
                if (!string.IsNullOrWhiteSpace(roleTree)) active.Add(roleTree);
            }
            return active;
        }

        private static bool ContainsTreeCategory(
            IEnumerable<string> treeIds,
            string category,
            M2CombatContent content)
        {
            foreach (var treeId in treeIds)
            {
                try
                {
                    if (StringComparer.Ordinal.Equals(
                            content.DeepProgression.Tree(treeId).Category,
                            category))
                        return true;
                }
                catch (KeyNotFoundException)
                {
                    // Forward-compatible save data may reference content not installed here.
                }
            }
            return false;
        }

        private static IReadOnlyList<string> EquipmentTags(RecruitState recruit)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assignment in recruit.Equipment.Assignments)
                foreach (var tag in assignment.Item.EquipmentTags)
                    result.Add(tag);
            return result.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static bool IsEquipmentLegal(
            DeepNodeDefinition070 node,
            IReadOnlyList<string> equipmentTags)
        {
            if (node.RequiredEquipmentTagsAny.Count == 0) return true;
            for (var requiredIndex = 0;
                 requiredIndex < node.RequiredEquipmentTagsAny.Count;
                 requiredIndex++)
                if (Contains(equipmentTags, node.RequiredEquipmentTagsAny[requiredIndex]))
                    return true;
            return false;
        }

        private static bool Contains(IReadOnlyList<string> values, string expected)
        {
            if (values == null) return false;
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index], expected)) return true;
            return false;
        }

        private static BattleArtProgressState FindProgress(
            IEnumerable<BattleArtProgressState> values,
            string artId) =>
            values.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.ArtId, artId));

        private static void AddUnique(IList<string> values, string value)
        {
            for (var index = 0; index < values.Count; index++)
                if (StringComparer.Ordinal.Equals(values[index], value)) return;
            values.Add(value);
        }
    }
}
