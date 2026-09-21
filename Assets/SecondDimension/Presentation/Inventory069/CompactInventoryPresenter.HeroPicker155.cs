using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class CompactInventoryPresenter069
    {
        Button _findHero155;

        void AddFindHero155(Transform header)
        {
            _findHero155 = RuntimeUi.AddButton(header, "Find Armory Hero 155", "FIND HERO", () =>
            {
                if (!_running || _root == null || _readState == null) return;
                var readAtOpen = _readState;
                OwnedHeroPicker155.Show(_root,
                    () => _running && ReferenceEquals(readAtOpen, _readState) ? readAtOpen() : null,
                    _selectedRecruitId, id =>
                    {
                        if (!_running || !ReferenceEquals(readAtOpen, _readState)) return;
                        var fresh = readAtOpen();
                        if (!(fresh?.Recruits ?? Array.Empty<M1RecruitLoadoutView>()).Any(hero =>
                                hero != null && StringComparer.Ordinal.Equals(hero.RecruitId, id))) return;
                        Refresh(id); // Existing display-only selection; no Equip call.
                    });
            }, RuntimeUi.MinimumTouchPixels);
            RuntimeUi.SetLayout(_findHero155, preferredWidth: 290f);
            MakeCompactButton069(_findHero155, RuntimeUi.MinimumTouchPixels, 26);
            AddCancelOnSelectable069(_findHero155);
        }
    }
}
