using System;
using System.Linq;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private void FindUnionHero155()
        {
            var authority = _coordinator;
            if (authority == null || _activePage == null) return;
            OwnedHeroPicker155.Show(_activePage,
                () => ReferenceEquals(authority, _coordinator) ? authority.State : null,
                _selectedRecruitId, id =>
                {
                    if (!ReferenceEquals(authority, _coordinator)) return;
                    var state = authority.State;
                    var recruits = (state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                        .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId)).ToArray();
                    var hero = recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, id));
                    if (hero == null) return;
                    var unions = (state.Unions ?? Array.Empty<M1UnionView>()).Where(value => value != null).ToArray();
                    var union = unions.FirstOrDefault(value => (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(id));
                    _selectedRecruitId = id;
                    _unionAdvanced132 = false;
                    _unionSelectedReserve109 = null;
                    if (union != null)
                    {
                        _selectedUnionIndex = union.Index;
                        _localStatus = hero.DisplayName + " is in " + M1UnionIdentity076.ResolveTab(union.UnionId, union.DisplayName, union.Index) +
                            ". Use the existing member controls to move or remove them.";
                    }
                    else
                    {
                        var assigned = unions.SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>()).ToArray();
                        var reserve = recruits.Where(value => !assigned.Contains(value.RecruitId))
                            .OrderBy(value => UnionPlannerRoleSortOrder074(value.ObservedClass))
                            .ThenBy(value => value.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase).ToArray();
                        var position = Array.FindIndex(reserve, value => value.RecruitId == id);
                        _unionReservePage074 = Math.Max(0, position / UnionPlannerReservePageSize074);
                        if (UnionEditable132)
                        {
                            _unionSelectedReserve109 = id;
                            _localStatus = hero.DisplayName + " selected. Choose a destination slot above.";
                        }
                        else _localStatus = hero.DisplayName + " is in reserve. " +
                            (authority is IUnionPlanningCoordinator132 plan ? plan.UnionPlanStatus132 : "Union changes are unavailable.");
                    }
                    _localStatusPositive = true;
                    BuildCurrentScreen();
                });
        }
    }
}
