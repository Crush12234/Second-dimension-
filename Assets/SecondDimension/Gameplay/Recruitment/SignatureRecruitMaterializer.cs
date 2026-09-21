using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>Creates a deterministic campaign instance from an authored Signature Recruit.</summary>
    public sealed class SignatureRecruitMaterializer
    {
        private readonly RecruitmentContent _content;

        public SignatureRecruitMaterializer(RecruitmentContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        public OpeningRecruitRecord Materialize(long campaignSeed, string signatureId, string sourceChannel) =>
            Materialize((object)campaignSeed, signatureId, sourceChannel);

        public OpeningRecruitRecord Materialize(string campaignSeed, string signatureId, string sourceChannel) =>
            Materialize((object)campaignSeed, signatureId, sourceChannel);

        public OpeningRecruitRecord Materialize(object campaignSeed, string signatureId, string sourceChannel)
        {
            if (campaignSeed == null) throw new ArgumentNullException(nameof(campaignSeed));
            if (string.IsNullOrWhiteSpace(signatureId)) throw new ArgumentException("Signature ID is required.", nameof(signatureId));
            if (string.IsNullOrWhiteSpace(sourceChannel)) throw new ArgumentException("Source channel is required.", nameof(sourceChannel));

            var record = RecruitmentDeterminism.FindSignature(_content.Signatures, signatureId);
            var rng = RecruitmentDeterminism.Rng(
                campaignSeed, "SIGNATURE_INSTANCE", signatureId, sourceChannel, _content.SignatureContentVersion);

            var authoredTraits = new HashSet<string>(
                RecruitmentDeterminism.StringArray(record["visibleTraitIds"]), StringComparer.Ordinal);
            var traitPool = new List<string>();
            foreach (var trait in (JArray)_content.Procedural["visibleTraits"])
            {
                var traitId = trait["id"].Value<string>();
                if (!authoredTraits.Contains(traitId)) traitPool.Add(traitId);
            }
            var secondaryTrait = RecruitmentDeterminism.ChooseToken(rng, traitPool);
            var level = rng.NextInclusive(1, 3);
            var condition = RecruitmentDeterminism.ChooseToken(
                rng, new[] { "WORN", "STANDARD", "WELL_KEPT" });
            var growthVariance = rng.NextInclusive(-8, 8);

            var identity = new JArray { JToken.FromObject(campaignSeed), signatureId };
            var recruitId = "SIGI_" + RecruitmentDeterminism.StableHash(identity, 16);
            var equipment = MaterializeLoadout((JObject)record["startingEquipment"], recruitId, condition);

            var statTendencies = new Dictionary<string, StatTendency>(StringComparer.Ordinal);
            foreach (var property in ((JObject)record["statTendencies"]).Properties())
            {
                statTendencies.Add(property.Name, new StatTendency
                {
                    BaseIndex = property.Value["baseIndex"].Value<int>(),
                    GrowthBias = property.Value["growthBias"].Value<int>()
                });
            }

            var weaponAptitudes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var preference in (JArray)record["weaponPreferences"])
            {
                // The original opening signatures use `weaponFamily`; the later
                // world packs use the canonical `weaponFamilyId`. Both are frozen
                // authored authority and must materialize through the same runtime.
                var weaponFamily = preference["weaponFamily"]?.Value<string>() ??
                                   preference["weaponFamilyId"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(weaponFamily))
                    throw new InvalidOperationException(
                        "Signature recruit weapon preference is missing its weapon family.");
                var weight = preference["weight"].Value<int>();
                if (weaponAptitudes.TryGetValue(weaponFamily, out var existingWeight))
                {
                    // A small number of frozen world-pack records repeat their fixed
                    // family as the secondary preference. Keep the stronger authored
                    // aptitude rather than crashing or inflating it by summation.
                    weaponAptitudes[weaponFamily] = Math.Max(existingWeight, weight);
                }
                else
                {
                    weaponAptitudes.Add(weaponFamily, weight);
                }
            }

            var disciplines = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var property in ((JObject)record["unionDisciplineAptitudes"]).Properties())
                disciplines.Add(property.Name, property.Value.Value<int>());

            var leadership = (JObject)record["leadership"];
            var potential = record["internalBalanceMetrics"]["developmentPotentialScore"].Value<int>() + growthVariance;
            var signingCost = 45 + level * 12 + Math.Max(0, potential - 500) / 7;
            var visualIdentity = (JObject)record["visualIdentity"];
            var visualHashPayload = new JArray
            {
                visualIdentity["visualSeedSalt"].Value<string>(),
                JToken.FromObject(campaignSeed)
            };
            var canonicalTraits = RecruitmentDeterminism.StringArray(record["visibleTraitIds"]);

            return new OpeningRecruitRecord
            {
                RecruitId = recruitId,
                SourceType = "SIGNATURE",
                SignatureId = signatureId,
                GenerationSeed = RecruitmentDeterminism.SeedString(
                    campaignSeed, "SIGNATURE_INSTANCE", signatureId, sourceChannel, _content.SignatureContentVersion),
                VisualSeed = RecruitmentDeterminism.StableHash(visualHashPayload, 24),
                DisplayName = record["name"]?["display"].Value<string>(),
                RaceId = record["raceId"].Value<string>(),
                HomeCommunityId = record["home"]?["communityId"].Value<string>(),
                AgeBandId = record["ageBand"].Value<string>(),
                PronounId = record["pronouns"].Value<string>(),
                BackgroundId = record["background"]?["id"].Value<string>(),
                StartingClassId = record["startingClassId"].Value<string>(),
                VisibleTraitIds = Array.AsReadOnly(new[] { canonicalTraits[0], secondaryTrait }),
                HiddenTraitId = record["hiddenTrait"]?["id"].Value<string>(),
                HiddenTraitRevealed = false,
                FearId = record["fearWeakness"]?["id"].Value<string>(),
                AmbitionId = record["ambition"]?["id"].Value<string>(),
                GrowthPatternId = record["growthPatternId"].Value<string>(),
                LeadershipTendencyId = leadership["styleId"].Value<string>(),
                WeaponAptitudeProfileId = "SIGNATURE_CUSTOM",
                DisciplineProfileId = "SIGNATURE_CUSTOM",
                RelationshipTendencyId = "SIGNATURE_CUSTOM",
                DialogueStyleId = record["dialogueStyle"]?["id"].Value<string>(),
                PersonalEventHookId = record["personalQuestSeed"]?["id"].Value<string>(),
                DisciplineAptitudes = disciplines,
                StatTendencies = statTendencies,
                WeaponAptitudes = weaponAptitudes,
                LeadershipScore = leadership["score"].Value<int>(),
                CommandBandwidth = leadership["commandBandwidth"].Value<int>(),
                StartingArtIds = Array.AsReadOnly(RecruitmentDeterminism.StringArray(record["startingArtIds"])),
                EquipmentLoadout = equipment,
                DevelopmentPotentialScore = potential,
                SigningCostXp = signingCost,
                VariantFlags = Array.AsReadOnly(new[]
                {
                    "START_LEVEL_" + level.ToString(CultureInfo.InvariantCulture),
                    "EQUIPMENT_" + condition,
                    "SECONDARY_" + secondaryTrait,
                    "GROWTH_VARIANCE_" + growthVariance.ToString("+0;-0;+0", CultureInfo.InvariantCulture)
                })
            };
        }

        public bool ContainsSignature(string signatureId)
        {
            if (string.IsNullOrWhiteSpace(signatureId)) return false;
            foreach (var record in (JArray)_content.Signatures["signatureRecruits"])
                if (StringComparer.Ordinal.Equals(record["signatureId"].Value<string>(), signatureId)) return true;
            return false;
        }

        private static EquipmentLoadout MaterializeLoadout(JObject source, string recruitId, string condition)
        {
            var slots = new Dictionary<string, EquipmentItem>(StringComparer.Ordinal);
            foreach (var property in ((JObject)source["slots"]).Properties())
            {
                if (property.Value.Type == JTokenType.Null)
                {
                    slots.Add(property.Name, null);
                    continue;
                }

                var item = (JObject)property.Value;
                slots.Add(property.Name, new EquipmentItem
                {
                    InstanceId = recruitId + "_" + property.Name,
                    ItemDefinitionId = item["itemDefinitionId"].Value<string>(),
                    // World-pack signature loadouts intentionally omit redundant
                    // tags. An omitted collection is an empty authored collection,
                    // not malformed content.
                    Tags = Array.AsReadOnly(OptionalStringArray(item["tags"])),
                    Locked = item["locked"]?.Value<bool>() ?? false,
                    Condition = condition
                });
            }

            var bonuses = new Dictionary<string, int>(StringComparer.Ordinal);
            var bonusesToken = source["aggregateBonuses"];
            if (bonusesToken != null && bonusesToken.Type != JTokenType.Null)
            {
                var authoredBonuses = bonusesToken as JObject;
                if (authoredBonuses == null)
                    throw new InvalidOperationException(
                        "Signature recruit aggregateBonuses must be an object when present.");
                foreach (var property in authoredBonuses.Properties())
                    bonuses.Add(property.Name, property.Value.Value<int>());
            }

            return new EquipmentLoadout
            {
                LoadoutId = "LOADOUT_" + recruitId,
                Slots = slots,
                AggregateBonuses = bonuses,
                AutoEquipAllowed = false
            };
        }

        private static string[] OptionalStringArray(JToken source)
        {
            if (source == null || source.Type == JTokenType.Null) return Array.Empty<string>();
            return RecruitmentDeterminism.StringArray(source);
        }
    }
}
