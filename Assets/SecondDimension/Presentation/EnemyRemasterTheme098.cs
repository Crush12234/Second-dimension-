using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>One repaired Myrmidon body, ten explicit material themes. Not ten drawings or combat rules.</summary>
    public sealed class EnemyRemasterTheme098
    {
        EnemyRemasterTheme098(int index, string name, Color accent, float amount = 1f)
        {
            VariantIndex098 = index;
            VariantId098 = "ENEMY_REC_021_VAR_" + index.ToString("00", System.Globalization.CultureInfo.InvariantCulture);
            Name098 = name; Accent098 = accent; Amount098 = amount;
        }
        public int VariantIndex098 { get; }
        public string VariantId098 { get; }
        public string Name098 { get; }
        public Color Accent098 { get; }
        public float Amount098 { get; }
        public const string AccentProperty098 = "_RemasterAccentColor098";
        public const string AmountProperty098 = "_RemasterAccentAmount098";
        static readonly EnemyRemasterTheme098[] Themes098 =
        {
            new EnemyRemasterTheme098(1, "Standard", new Color(0.80f, 0.58f, 1.00f, 1f)),
            new EnemyRemasterTheme098(2, "Veteran", new Color(0.82f, 0.42f, 0.17f, 1f)),
            new EnemyRemasterTheme098(3, "Armored", new Color(0.78f, 0.30f, 1.00f, 1f), 0f),
            new EnemyRemasterTheme098(4, "Swift", new Color(0.10f, 1.00f, 0.62f, 1f)),
            new EnemyRemasterTheme098(5, "Frost", new Color(0.44f, 0.85f, 1.00f, 1f)),
            new EnemyRemasterTheme098(6, "Storm", new Color(0.16f, 0.35f, 1.00f, 1f)),
            new EnemyRemasterTheme098(7, "Venom", new Color(0.50f, 1.00f, 0.08f, 1f)),
            new EnemyRemasterTheme098(8, "Void", new Color(0.40f, 0.04f, 0.70f, 1f)),
            new EnemyRemasterTheme098(9, "Tower Elite", new Color(1.00f, 0.07f, 0.22f, 1f)),
            new EnemyRemasterTheme098(10, "Apex", new Color(1.00f, 0.76f, 0.12f, 1f))
        };

        public static bool TryResolve098(string baseId, string variantId, out EnemyRemasterTheme098 theme)
        {
            theme = null;
            if (!StringComparer.Ordinal.Equals(baseId, "ENEMY_REC_021")) return false;
            foreach (var candidate in Themes098)
                if (StringComparer.Ordinal.Equals(candidate.VariantId098, variantId))
                { theme = candidate; return true; }
            return false;
        }

        // Caller owns this material. Actual provider provenance is required so a
        // missing-resource/raw recovery path cannot be mislabeled as the remaster.
        public static bool ApplyToMaterial098(Material material, string baseId, string variantId,
            Sprite actualSprite, out EnemyRemasterTheme098 theme)
        {
            theme = null;
            if (material == null || !material.HasProperty(AmountProperty098) ||
                !material.HasProperty(AccentProperty098)) return false;
            material.SetFloat(AmountProperty098, 0f);
            material.SetColor(AccentProperty098, Color.white);
            if (!EnemyArtRemaster098.TrySourcePath098(actualSprite, out _) ||
                !TryResolve098(baseId, variantId, out theme)) return false;
            material.SetColor(AccentProperty098, theme.Accent098);
            material.SetFloat(AmountProperty098, theme.Amount098);
            return true;
        }
    }
}
