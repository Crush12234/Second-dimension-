using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Gameplay.SSSTenV4
{
    /// <summary>
    /// Exact V4 Art identities projected into M2's existing Art-definition shape.
    /// The package deliberately leaves live cost tuning to the host. Arts 01-03
    /// therefore use bounded low/medium M2 costs and the normal Forecast resolver;
    /// Art 04 remains visible in the registry but executes only via the Gold adapter.
    /// </summary>
    public static class SssTenV4ArtRegistry090
    {
        public const string ContentVersion090 = "SSS_TEN_V4_ARTS_040";

        private sealed class Row090
        {
            public Row090(
                string id,
                string name,
                string tree,
                string effect,
                string costTier)
            {
                Id = id;
                Name = name;
                Tree = tree;
                Effect = effect;
                CostTier = costTier;
            }

            public string Id { get; }
            public string Name { get; }
            public string Tree { get; }
            public string Effect { get; }
            public string CostTier { get; }
            public bool Gold => Id.EndsWith("_ART_04", StringComparison.Ordinal);
        }

        private static readonly Row090[] Rows =
        {
            Row("SSS_RYLEN_STONEBOND", 1, "Beast Mark", "Combat", "DamageOneEnemyUnion", "Low"),
            Row("SSS_RYLEN_STONEBOND", 2, "Pack Instinct", "Beastcraft", "BuffSourceUnion", "Medium"),
            Row("SSS_RYLEN_STONEBOND", 3, "Wild Mend", "Restoration", "HealOneAlliedUnion", "Medium"),
            Row("SSS_RYLEN_STONEBOND", 4, "Call the Tamed Union", "Beastcraft", "SpawnSixMonsterUnion", "High"),
            Row("SSS_ELYSIA_NIGHTCALL", 1, "Echo Bolt", "Arcane", "DamageOneEnemyUnion", "Low"),
            Row("SSS_ELYSIA_NIGHTCALL", 2, "Spirit Veil", "Warding", "BarrierOneAlliedUnion", "Medium"),
            Row("SSS_ELYSIA_NIGHTCALL", 3, "Pact Convergence", "Conjuration", "BuffOneAlliedGuestUnion", "Medium"),
            Row("SSS_ELYSIA_NIGHTCALL", 4, "Manifest the Pact", "Conjuration", "SpawnSingleGuestUnion", "High"),
            Row("SSS_VAELIS_MANYFORM", 1, "Primal Rend", "Combat", "DamageOneEnemyUnion", "Low"),
            Row("SSS_VAELIS_MANYFORM", 2, "Adaptive Hide", "Warding", "BuffSourceUnion", "Medium"),
            Row("SSS_VAELIS_MANYFORM", 3, "Instinctive Surge", "Instinct", "BuffSourceUnion", "Medium"),
            Row("SSS_VAELIS_MANYFORM", 4, "Union Metamorphosis", "Metamorphosis", "ReplaceSourceUnion", "High"),
            Row("SSS_NERIS_DAWNWELL", 1, "Dawn Pulse", "Restoration", "HealOneAlliedUnion", "Low"),
            Row("SSS_NERIS_DAWNWELL", 2, "Cleansing Tide", "Restoration", "CleanseAllAlliedUnions", "Medium"),
            Row("SSS_NERIS_DAWNWELL", 3, "Life Chorus", "Restoration", "HealAllAlliedUnions", "Medium"),
            Row("SSS_NERIS_DAWNWELL", 4, "Mercy Beyond Measure", "Restoration", "HealAllAlliedUnions", "High"),
            Row("SSS_MYRIEN_STARFALL", 1, "Astral Lance", "Astral", "DamageOneEnemyUnion", "Low"),
            Row("SSS_MYRIEN_STARFALL", 2, "Meteor Choir", "Elemental", "DamageAllEnemyUnions", "Medium"),
            Row("SSS_MYRIEN_STARFALL", 3, "Void Comet", "Astral", "DamageOneEnemyUnion", "Medium"),
            Row("SSS_MYRIEN_STARFALL", 4, "Astral Cataclysm", "Astral", "DamageAllEnemyUnions", "High"),
            Row("SSS_ASTERION_SUNWARD", 1, "Sunward Strike", "Combat", "DamageOneEnemyUnion", "Low"),
            Row("SSS_ASTERION_SUNWARD", 2, "Rallying Call", "Leadership", "BuffSourceUnion", "Medium"),
            Row("SSS_ASTERION_SUNWARD", 3, "Dawn Standard", "Leadership", "BuffAllAlliedUnions", "Medium"),
            Row("SSS_ASTERION_SUNWARD", 4, "Banner of Ten Thousand Dawns", "Leadership", "BuffAllAlliedUnions", "High"),
            Row("SSS_SOLENNE_AEGIS", 1, "Shield Arc", "Combat", "DamageOneEnemyUnion", "Low"),
            Row("SSS_SOLENNE_AEGIS", 2, "Aegis Ward", "Warding", "BarrierOneAlliedUnion", "Medium"),
            Row("SSS_SOLENNE_AEGIS", 3, "Fortress Chorus", "Warding", "BarrierAllAlliedUnions", "Medium"),
            Row("SSS_SOLENNE_AEGIS", 4, "Citadel Without End", "Warding", "BarrierAllAlliedUnions", "High"),
            Row("SSS_CAEDRAN_TEMPEST", 1, "Storm Spear", "Storm", "DamageOneEnemyUnion", "Low"),
            Row("SSS_CAEDRAN_TEMPEST", 2, "Thunder Chorus", "Storm", "DamageAllEnemyUnions", "Medium"),
            Row("SSS_CAEDRAN_TEMPEST", 3, "Ion Break", "Storm", "DamageOneEnemyUnion", "Medium"),
            Row("SSS_CAEDRAN_TEMPEST", 4, "Storm Across Worlds", "Storm", "DamageAllEnemyUnions", "High"),
            Row("SSS_ISOLDE_ECLIPSERIFT", 1, "Rift Needle", "Void", "DamageOneEnemyUnion", "Low"),
            Row("SSS_ISOLDE_ECLIPSERIFT", 2, "Umbral Collapse", "Void", "DamageAllEnemyUnions", "Medium"),
            Row("SSS_ISOLDE_ECLIPSERIFT", 3, "Eclipse Seal", "Hexcraft", "DebuffOneEnemyUnion", "Medium"),
            Row("SSS_ISOLDE_ECLIPSERIFT", 4, "Eclipse of Every Front", "Void", "DamageDebuffAllEnemyUnions", "High"),
            Row("SSS_ORINTH_WORLDSONG", 1, "Resonant Note", "Harmonics", "DamageOneEnemyUnion", "Low"),
            Row("SSS_ORINTH_WORLDSONG", 2, "March of Worlds", "Leadership", "BuffAllAlliedUnions", "Medium"),
            Row("SSS_ORINTH_WORLDSONG", 3, "Renewal Verse", "Restoration", "HealAllAlliedUnions", "Medium"),
            Row("SSS_ORINTH_WORLDSONG", 4, "Concordance of Worlds", "Harmonics", "BuffAllAlliedUnions", "High")
        };

        public static IReadOnlyList<M2ArtDefinition> Build()
        {
            if (Rows.Length != 40 ||
                Rows.Select(value => value.Id)
                    .Distinct(StringComparer.Ordinal).Count() != 40)
                throw new InvalidOperationException(
                    "The SSS Ten V4 Art registry must contain 40 unique IDs.");
            return Array.AsReadOnly(Rows.Select(Build).ToArray());
        }

        private static M2ArtDefinition Build(Row090 row)
        {
            var discipline = Discipline(row);
            Cost(row.CostTier, out var ap, out var mp, out var power);
            return new M2ArtDefinition(
                row.Id,
                row.Name,
                "SSS_" + row.Tree.ToUpperInvariant(),
                discipline,
                Array.Empty<string>(),
                ap,
                mp,
                IntentTags(discipline),
                MeaningfulUse(row.Effect),
                row.Id + "_ANIM",
                false,
                treeId: row.Tree,
                nodeType: row.Gold ? "SIGNATURE" : "ACTION",
                powerCoefficientPermille: power,
                forecastAction: !row.Gold,
                effectTags: EffectTags(row.Effect),
                targetRule: TargetRule(row.Effect));
        }

        private static Row090 Row(
            string heroId,
            int ordinal,
            string name,
            string tree,
            string effect,
            string tier) =>
            new Row090(
                heroId + "_ART_" + ordinal.ToString("00"),
                name,
                tree,
                effect,
                tier);

        private static string Discipline(Row090 row)
        {
            if (row.Effect.StartsWith("Heal", StringComparison.Ordinal) ||
                row.Effect.StartsWith("Cleanse", StringComparison.Ordinal))
                return "Restoration";
            if (row.Effect.StartsWith("Buff", StringComparison.Ordinal) ||
                row.Effect.StartsWith("Barrier", StringComparison.Ordinal))
                return "Support";
            if (StringComparer.Ordinal.Equals(row.Tree, "Combat") ||
                StringComparer.Ordinal.Equals(row.Tree, "Beastcraft") ||
                StringComparer.Ordinal.Equals(row.Tree, "Instinct"))
                return "Martial";
            return "Mystic";
        }

        private static void Cost(
            string tier,
            out int ap,
            out int mp,
            out int power)
        {
            if (StringComparer.Ordinal.Equals(tier, "Low"))
            {
                ap = 2;
                mp = 3;
                power = 1100;
                return;
            }
            if (StringComparer.Ordinal.Equals(tier, "Medium"))
            {
                ap = 4;
                mp = 6;
                power = 1350;
                return;
            }
            ap = SssBattleIntegration090.GoldSharedApCost090;
            mp = SssBattleIntegration090.GoldPersonalMpCost090;
            power = 1700;
        }

        private static IReadOnlyList<string> IntentTags(string discipline)
        {
            switch (discipline)
            {
                case "Restoration": return new[] { "HEAL", "RESCUE" };
                case "Support": return new[] { "SUPPORT", "RESCUE" };
                case "Mystic": return new[] { "MYSTIC", "OFFENSE" };
                default: return new[] { "OFFENSE", "PRESSURE" };
            }
        }

        private static IReadOnlyList<string> EffectTags(string effect)
        {
            var tags = new List<string> { effect };
            if (effect.StartsWith("Heal", StringComparison.Ordinal))
            {
                tags.Add("HEAL");
                if (effect.IndexOf("All", StringComparison.Ordinal) >= 0)
                {
                    tags.Add("MASS_HEAL");
                    tags.Add("AREA_HEAL");
                }
            }
            if (effect.StartsWith("Cleanse", StringComparison.Ordinal))
            {
                tags.Add("CLEANSE");
                tags.Add("AREA_CLEANSE");
            }
            if (effect.StartsWith("Barrier", StringComparison.Ordinal))
            {
                tags.Add("BARRIER");
                tags.Add("MAGIC_BARRIER");
                if (effect.IndexOf("All", StringComparison.Ordinal) >= 0)
                    tags.Add("AREA_PROTECTION");
            }
            if (effect.StartsWith("Buff", StringComparison.Ordinal))
            {
                tags.Add("BUFF_ALLY");
                tags.Add("COHESION_RESTORE");
                if (effect.IndexOf("All", StringComparison.Ordinal) >= 0)
                    tags.Add("AREA_PROTECTION");
            }
            if (effect.IndexOf("DamageAll", StringComparison.Ordinal) >= 0)
                tags.Add("AREA_DAMAGE");
            if (StringComparer.Ordinal.Equals(
                    effect, "BuffOneAlliedGuestUnion"))
                tags.Add("SSS_V4_SCOPE_OWNED_GUEST_UNION");
            if (effect.IndexOf("AllEnemyUnions", StringComparison.Ordinal) >= 0)
                tags.Add("SSS_V4_SCOPE_ALL_ENEMY_UNIONS");
            if (effect.IndexOf("AllAlliedUnions", StringComparison.Ordinal) >= 0)
                tags.Add("SSS_V4_SCOPE_ALL_ALLIED_UNIONS");
            if (effect.IndexOf("Debuff", StringComparison.Ordinal) >= 0)
                tags.Add("DEBUFF");
            if (effect.StartsWith("Spawn", StringComparison.Ordinal) ||
                effect.StartsWith("Replace", StringComparison.Ordinal))
                tags.Add("SSS_GOLD_ADAPTER_ONLY");
            return tags.AsReadOnly();
        }

        private static string TargetRule(string effect)
        {
            if (effect.IndexOf("SourceUnion", StringComparison.Ordinal) >= 0 ||
                effect.StartsWith("Replace", StringComparison.Ordinal))
                return "SELF";
            if (effect.IndexOf("Allied", StringComparison.Ordinal) >= 0 ||
                effect.IndexOf("GuestUnion", StringComparison.Ordinal) >= 0 ||
                effect.StartsWith("Barrier", StringComparison.Ordinal))
                return "SELF_OR_ALLY_UNION";
            return "ENEMY_UNION";
        }

        private static string MeaningfulUse(string effect)
        {
            if (effect.StartsWith("Heal", StringComparison.Ordinal))
                return "Restore missing HP on a legal living allied Union.";
            if (effect.StartsWith("Cleanse", StringComparison.Ordinal))
                return "Remove existing harmful pressure from a legal allied Union.";
            if (effect.StartsWith("Buff", StringComparison.Ordinal) ||
                effect.StartsWith("Barrier", StringComparison.Ordinal))
                return "Improve a legal allied Union's current defensive or formation state.";
            if (effect.StartsWith("Damage", StringComparison.Ordinal) ||
                effect.IndexOf("Debuff", StringComparison.Ordinal) >= 0)
                return "Deal nonzero damage or meaningful pressure to a legal enemy Union.";
            return "Resolved only by the certified SSS Gold Forecast adapter.";
        }
    }
}
