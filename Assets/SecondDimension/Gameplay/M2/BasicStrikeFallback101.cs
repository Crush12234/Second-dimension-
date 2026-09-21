using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Explicit, ordinary unarmed/close physical fallback. This is not the authored
    /// Support/Rescue Art Assist Ally, nor a grant of Shield Bash or a learned tree node.
    /// The 1 AP/0 MP cost and 1.0 coefficient retain the former fallback's expenditure
    /// and base damage; only its identity/discipline/mastery are corrected in new battles.
    /// </summary>
    public static class BasicStrikeFallback101
    {
        public const string ArtId = "ART_BASIC_STRIKE_101";

        public static void AddToRuntime(IDictionary<string, M2ArtDefinition> arts)
        {
            if (arts == null) throw new ArgumentNullException(nameof(arts));
            // An authored collision is an error, never an overwrite of content authority.
            arts.Add(ArtId, new M2ArtDefinition(
                ArtId, "Basic Strike", "BASIC", "Martial", Array.Empty<string>(),
                1, 0, new[] { "OFFENSE" }, "Deal actual HP damage with a basic physical strike.",
                "ART_BASIC_STRIKE_ANIM", false, powerCoefficientPermille: 1000,
                effectTags: new[] { "HP_DAMAGE" }, targetRule: "single_enemy"));
        }
    }
}
