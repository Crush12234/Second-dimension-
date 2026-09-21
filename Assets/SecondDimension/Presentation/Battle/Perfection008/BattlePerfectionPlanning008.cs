using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Pure presentation planning for the 10-allied / 10-enemy battlefield. It reads
    /// immutable view data and never changes authoritative combat state or results.
    /// </summary>
    public static class BattlePerfectionPlanning008
    {
        public const int MaxUnionsPerSide = 10;
        public const int TotalUnionCapacity = 20;
        public const int MaxHeroUnions = 2;
        public const int MaxContextUnions = 2;

        public static BattlePresentationPlan008 Plan(M2BattleView battle, BattlePresentationRequest008 request)
        {
            request ??= new BattlePresentationRequest008();
            var deployment = BattleUnionDeploymentPlanning020.Plan(battle);
            var budget = BattlePerformancePlanning008.For(request.DeviceProfile, request.ReducedMotion);
            var layout = BattleSafeLayoutPlanning008.Plan(
                deployment,
                request.AspectRatio,
                request.NormalizedSafeArea,
                request.Mode == BattlePresentationMode008.CommandFocus ||
                request.Mode == BattlePresentationMode008.ForecastCommit);

            var existingIds = new HashSet<string>(
                deployment.Slots.Where(value => value != null)
                    .Select(value => value.UnionId),
                StringComparer.Ordinal);
            var active = existingIds.Contains(request.ActiveUnionId ?? string.Empty)
                ? request.ActiveUnionId
                : string.Empty;
            var target = existingIds.Contains(request.TargetUnionId ?? string.Empty)
                ? request.TargetUnionId
                : string.Empty;

            var heroIds = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(active)) heroIds.Add(active);
            if (!string.IsNullOrWhiteSpace(target) && heroIds.Count < MaxHeroUnions) heroIds.Add(target);

            var contextIds = ResolveContextIds(request.Relationships, heroIds, existingIds, budget.MaxContextUnions);
            var directives = new List<BattleUnionDirective008>(deployment.Slots.Count);
            foreach (var slot in deployment.Slots)
            {
                if (slot == null || string.IsNullOrWhiteSpace(slot.UnionId)) continue;
                var union = FindUnion(battle, slot.UnionId);
                var defeated = IsDefeated(union);
                var lod = ResolveLod(request.Mode, slot.UnionId, defeated, heroIds, contextIds);
                directives.Add(CreateDirective(slot, union, lod, request.ReducedMotion, budget));
            }

            TrimVisibleMemberBudget(directives, budget.MaxVisibleMembers);
            var openingShot = ResolveOpeningShot(request.Mode);
            return new BattlePresentationPlan008(
                directives.AsReadOnly(), layout, budget, openingShot, active, target);
        }

        private static BattleUnionLod008 ResolveLod(
            BattlePresentationMode008 mode,
            string unionId,
            bool defeated,
            ISet<string> heroIds,
            ISet<string> contextIds)
        {
            if (heroIds.Contains(unionId)) return BattleUnionLod008.HeroUnion;
            if (contextIds.Contains(unionId)) return BattleUnionLod008.ContextUnion;
            if (defeated && mode == BattlePresentationMode008.CinematicAction)
                return BattleUnionLod008.Navigator;
            if (mode == BattlePresentationMode008.CommandFocus ||
                mode == BattlePresentationMode008.ForecastCommit)
                return BattleUnionLod008.TacticalAnchor;
            if (mode == BattlePresentationMode008.Results)
                return defeated ? BattleUnionLod008.Navigator : BattleUnionLod008.TacticalAnchor;
            return BattleUnionLod008.TacticalAnchor;
        }

        private static BattleUnionDirective008 CreateDirective(
            BattleUnionDeploymentSlot020 slot,
            M2BattleUnionView union,
            BattleUnionLod008 lod,
            bool reducedMotion,
            BattlePerformanceBudget008 budget)
        {
            var memberCount = union?.Members?.Count ?? 0;
            switch (lod)
            {
                case BattleUnionLod008.HeroUnion:
                    return new BattleUnionDirective008(
                        slot.UnionId, slot.Enemy, slot.SideOrdinal, lod,
                        Math.Min(5, memberCount), reducedMotion ? 30 : 60,
                        true, true, !reducedMotion, true, IsDefeated(union));
                case BattleUnionLod008.ContextUnion:
                    return new BattleUnionDirective008(
                        slot.UnionId, slot.Enemy, slot.SideOrdinal, lod,
                        Math.Min(3, memberCount), reducedMotion ? 20 : 30,
                        true, true, false, false, IsDefeated(union));
                case BattleUnionLod008.TacticalAnchor:
                    return new BattleUnionDirective008(
                        slot.UnionId, slot.Enemy, slot.SideOrdinal, lod,
                        Math.Min(2, memberCount), budget.TacticalUpdateHz,
                        true, false, false, false, IsDefeated(union));
                case BattleUnionLod008.Navigator:
                    return new BattleUnionDirective008(
                        slot.UnionId, slot.Enemy, slot.SideOrdinal, lod,
                        0, budget.NavigatorUpdateHz,
                        true, false, false, false, IsDefeated(union));
                default:
                    return new BattleUnionDirective008(
                        slot.UnionId, slot.Enemy, slot.SideOrdinal, lod,
                        0, 0, false, false, false, false, IsDefeated(union));
            }
        }

        private static HashSet<string> ResolveContextIds(
            IReadOnlyList<BattleRelationship008> relationships,
            ISet<string> heroIds,
            ISet<string> existingIds,
            int maxContext)
        {
            var candidates = new List<Tuple<int, string>>();
            foreach (var relationship in relationships ?? Array.Empty<BattleRelationship008>())
            {
                if (relationship == null || !relationship.Active) continue;
                var source = relationship.SourceUnionId ?? string.Empty;
                var target = relationship.TargetUnionId ?? string.Empty;
                var touchesHero = heroIds.Contains(source) || heroIds.Contains(target);
                if (!touchesHero) continue;
                AddCandidate(candidates, existingIds, heroIds, source, relationship.Kind);
                AddCandidate(candidates, existingIds, heroIds, target, relationship.Kind);
            }

            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in candidates
                         .OrderByDescending(value => value.Item1)
                         .ThenBy(value => value.Item2, StringComparer.Ordinal))
            {
                if (result.Count >= Math.Max(0, maxContext)) break;
                result.Add(candidate.Item2);
            }
            return result;
        }

        private static void AddCandidate(
            ICollection<Tuple<int, string>> candidates,
            ISet<string> existingIds,
            ISet<string> heroIds,
            string unionId,
            BattleRelationshipKind008 kind)
        {
            if (string.IsNullOrWhiteSpace(unionId) || !existingIds.Contains(unionId) || heroIds.Contains(unionId)) return;
            candidates.Add(Tuple.Create(BattleRelationshipVisualPolicy008.Priority(kind), unionId));
        }

        private static M2BattleUnionView FindUnion(M2BattleView battle, string unionId)
        {
            if (battle == null || string.IsNullOrWhiteSpace(unionId)) return null;
            return (battle.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
                .Concat(battle.EnemyUnions ?? Array.Empty<M2BattleUnionView>())
                .FirstOrDefault(value => value != null &&
                                         string.Equals(value.UnionId, unionId, StringComparison.Ordinal));
        }

        private static bool IsDefeated(M2BattleUnionView union)
        {
            return union != null && union.Members != null && union.Members.Count > 0 &&
                   union.Members.All(value => value == null || value.Downed);
        }

        private static void TrimVisibleMemberBudget(
            IList<BattleUnionDirective008> directives,
            int maximum)
        {
            var total = directives.Sum(value => value.VisibleMemberLimit);
            if (total <= maximum) return;

            for (var index = directives.Count - 1; index >= 0 && total > maximum; index--)
            {
                var value = directives[index];
                if (value.Lod != BattleUnionLod008.TacticalAnchor || value.VisibleMemberLimit <= 1) continue;
                directives[index] = CloneWithVisibleMembers(value, 1);
                total--;
            }
            for (var index = directives.Count - 1; index >= 0 && total > maximum; index--)
            {
                var value = directives[index];
                if (value.Lod != BattleUnionLod008.ContextUnion || value.VisibleMemberLimit <= 2) continue;
                directives[index] = CloneWithVisibleMembers(value, 2);
                total--;
            }
        }

        private static BattleUnionDirective008 CloneWithVisibleMembers(
            BattleUnionDirective008 value,
            int visibleMembers)
        {
            return new BattleUnionDirective008(
                value.UnionId, value.Enemy, value.SideOrdinal, value.Lod,
                visibleMembers, value.TargetUpdateHz, value.KeepNavigatorPlate,
                value.UseActionPose, value.UseSecondaryMotion, value.UseMajorVfx, value.Defeated);
        }

        private static BattleShot008 ResolveOpeningShot(BattlePresentationMode008 mode)
        {
            switch (mode)
            {
                case BattlePresentationMode008.CommandFocus:
                case BattlePresentationMode008.ForecastCommit:
                    return BattleShot008.ActiveUnionFocus;
                case BattlePresentationMode008.CinematicAction:
                    return BattleShot008.MeleeContact;
                case BattlePresentationMode008.Results:
                    return BattleShot008.VictoryWide;
                default:
                    return BattleShot008.TacticalOverviewWide;
            }
        }
    }
}
