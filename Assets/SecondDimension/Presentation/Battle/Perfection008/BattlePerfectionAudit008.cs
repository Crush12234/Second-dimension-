using System;
using System.Linq;

namespace SecondDimension.Presentation
{
    public static class BattlePerfectionAudit008
    {
        public static bool TryValidate(BattlePresentationPlan008 plan, out string error)
        {
            if (plan == null) { error = "Plan is null."; return false; }
            if (plan.Directives == null) { error = "Directives are null."; return false; }
            if (plan.Directives.Count > BattlePerfectionPlanning008.TotalUnionCapacity)
            { error = "More than 20 Union directives were produced."; return false; }
            if (plan.Directives.Select(value => value.UnionId).Distinct(StringComparer.Ordinal).Count() != plan.Directives.Count)
            { error = "Duplicate Union directive IDs were produced."; return false; }
            if (plan.Directives.Count(value => value.Lod == BattleUnionLod008.HeroUnion) > plan.Budget.MaxHeroUnions)
            { error = "Hero Union budget exceeded."; return false; }
            if (plan.Directives.Count(value => value.Lod == BattleUnionLod008.ContextUnion) > plan.Budget.MaxContextUnions)
            { error = "Context Union budget exceeded."; return false; }
            if (plan.Directives.Sum(value => value.VisibleMemberLimit) > plan.Budget.MaxVisibleMembers)
            { error = "Visible member budget exceeded."; return false; }
            if (plan.Directives.Any(value => !value.IsPresentationOnly))
            { error = "A directive is not presentation-only."; return false; }
            if (plan.Layout == null || plan.Layout.NavigatorChips == null)
            { error = "Safe layout or navigator chips are missing."; return false; }
            if (plan.Layout.NavigatorChips.Count != plan.Directives.Count)
            { error = "Navigator chip count does not match occupied Union count."; return false; }
            error = string.Empty;
            return true;
        }
    }
}
