using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// The existing enemy catalog already references these two supplemental Arts,
    /// while the old loader read only the main arts array. This deliberately is
    /// not a blanket unlock of cultural/hybrid Arts or their learning gates.
    /// </summary>
    public static class EnemyReferencedSupplementalArts094
    {
        public const string Guard094 = "CULT_DOG_TRIBE_GUARD_THE_STRAGGLER";
        public const string Commander094 = "HYBRID_FIELD_COMMANDER";

        public static void AddToRuntime094(IDictionary<string, M2ArtDefinition> arts,
            JObject artRoot, JObject enemyRoot)
        {
            if (arts == null || artRoot == null || enemyRoot == null)
                throw new ArgumentNullException("Enemy supplemental Art source is required.");
            var references = new HashSet<string>(
                (enemyRoot["enemies"] as JArray ?? new JArray()).OfType<JObject>()
                    .SelectMany(enemy => (enemy["artIds"] as JArray ?? new JArray()).Values<string>()),
                StringComparer.Ordinal);
            var supplemental = (artRoot["culturalArts"] as JArray ?? new JArray())
                .Concat(artRoot["hybridArts"] as JArray ?? new JArray()).OfType<JObject>().ToArray();
            foreach (var id in new[] { Guard094, Commander094 })
            {
                if (!references.Contains(id)) continue;
                if (arts.ContainsKey(id))
                    throw new InvalidDataException("Supplemental enemy Art duplicates a runtime ID: " + id);
                var matches = supplemental.Where(row => row.Value<string>("id") == id).ToArray();
                if (matches.Length != 1)
                    throw new InvalidDataException("Exactly one authored supplemental enemy Art is required: " + id);
                var row = matches[0];
                var effects = Strings094(row["effectFormula"]).ToList();
                var sourceDiscipline = Required094(row, "discipline");
                effects.Add("SOURCE_DISCIPLINE_" + sourceDiscipline.ToUpperInvariant());
                // Explicit adapter interpretation, not a new combat formula:
                // Guard the Straggler protects another allied Union through the
                // existing Barrier handler; Field Commander uses the existing
                // generic cohesion/formation support magnitude. Both are Support
                // actions in runtime dispatch so neither can become a fake hit.
                if (id == Guard094 && !effects.Contains("BARRIER")) effects.Add("BARRIER");
                var intents = Strings094(row["forecastIntentTags"]).ToList();
                if (!intents.Contains("SUPPORT")) intents.Add("SUPPORT");
                arts.Add(id, new M2ArtDefinition(id, Required094(row, "displayName"),
                    Required094(row, "family"), "Support", Strings094(row["requiredEquipmentTags"]),
                    RequiredInt094(row, "sharedApCost"), RequiredInt094(row, "personalMpCost"),
                    intents.AsReadOnly(), Required094(row, "meaningfulUseDefinition"),
                    Required094(row, "animationTag"), false, nodeType: "AUTHORED_ENEMY_SUPPORT_094",
                    effectTags: effects.AsReadOnly(), statusTags: Strings094(row["statusTags"]),
                    targetRule: "ALLY_UNION"));
            }
        }

        static string Required094(JObject row, string key) =>
            !string.IsNullOrWhiteSpace(row.Value<string>(key)) ? row.Value<string>(key) :
                throw new InvalidDataException("Enemy supplemental Art field required: " + key);
        static int RequiredInt094(JObject row, string key) =>
            row[key]?.Type == JTokenType.Integer && row.Value<int>(key) >= 0 ? row.Value<int>(key) :
                throw new InvalidDataException("Enemy supplemental Art cost invalid: " + key);
        static string[] Strings094(JToken value) =>
            (value as JArray ?? new JArray()).Values<string>().ToArray();
    }
}
