using System;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>Reviewed ordinary-area adapter; unrelated and SSS Arts remain single/default.</summary>
    public sealed class M2AreaArtProfile095
    {
        public const string SelectedUnion = "SELECTED_ENEMY_UNION";
        public const string CrossUnion = "ORDERED_ENEMY_UNIONS";

        private M2AreaArtProfile095(string scope, int maximumTargets, int damage,
            int cohesion, int formation, bool authored)
        {
            Scope = scope;
            MaximumTargets = maximumTargets;
            DamageCoefficientPermille = damage;
            CohesionCoefficientPermille = cohesion;
            FormationCoefficientPermille = formation;
            HasAuthoredBudget = authored;
        }

        public string Scope { get; }
        public int MaximumTargets { get; }
        public int DamageCoefficientPermille { get; }
        public int CohesionCoefficientPermille { get; }
        public int FormationCoefficientPermille { get; }
        public bool HasAuthoredBudget { get; }

        public static M2AreaArtProfile095 FromAuthored(string artId, int maximumTargets,
            int damage, int cohesion, int formation)
        {
            var cross = artId == "TREE_CA002_MYS_FLAME_N10" || artId == "TREE_CA002_MYS_STORM_N10";
            var selected = artId == "TREE_CA002_WPN_STAFF_N02" ||
                artId == "TREE_CA002_WPN_GREAT_WEAPON_N03" ||
                artId == "TREE_CA002_WPN_BOW_N03" ||
                artId == "TREE_CA002_WPN_AXE_N06" ||
                artId == "TREE_CA002_WPN_BOW_N08";
            if (!cross && !selected) return null;
            if (maximumTargets != 5 || damage <= 0 || cohesion < 0 || formation < 0)
                throw new ArgumentException("M2_AREA095_REVIEWED_PROFILE_BUDGET_INVALID: " + artId);
            return new M2AreaArtProfile095(cross ? CrossUnion : SelectedUnion,
                maximumTargets, damage, cohesion, formation, true);
        }

        public static M2AreaArtProfile095 Legacy(string artId) =>
            artId == "ART_SPLIT_VOLLEY" ? new M2AreaArtProfile095(SelectedUnion, 3, 1000, 1000, 1000, false) :
            artId == "ART_CONVERGENCE" ? new M2AreaArtProfile095(SelectedUnion, 5, 1000, 1000, 1000, false) : null;
    }
}


