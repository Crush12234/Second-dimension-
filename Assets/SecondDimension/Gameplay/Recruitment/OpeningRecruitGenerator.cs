using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>Portable PCG32 opening applicant generator. No Unity APIs or ambient state.</summary>
    public sealed class OpeningRecruitGenerator
    {
        private readonly RecruitmentContent _content;
        private readonly JObject _tables;

        public OpeningRecruitGenerator(RecruitmentContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _tables = content.Procedural;
        }

        public OpeningRecruitRecord Generate(ProceduralRecruitRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.CampaignSeed == null) throw new ArgumentException("CampaignSeed is required.", nameof(request));
            if (string.IsNullOrWhiteSpace(request.SourceChannel)) throw new ArgumentException("SourceChannel is required.", nameof(request));
            if (request.UnlockedRaces == null || request.UnlockedRaces.Count == 0)
                throw new ArgumentException("At least one race must be unlocked.", nameof(request));

            var unlockedRaces = request.UnlockedRaces.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var extraSalt = request.ExtraSalt ?? string.Empty;
            var keyParts = new object[]
            {
                request.CampaignSeed, "OPENING_APPLICANT", request.GuildDay, request.RefreshIndex,
                request.SlotIndex, request.SourceChannel, extraSalt, _content.ProceduralContentVersion
            };
            var rng = RecruitmentDeterminism.Rng(keyParts);
            var raceId = RecruitmentDeterminism.ChooseToken(rng, unlockedRaces);
            var first = RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["names"]?[raceId], "names." + raceId)).Value<string>();
            var family = RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["surnames"]?[raceId], "surnames." + raceId)).Value<string>();
            var background = ChooseBackground(rng, raceId);
            var classTendency = ChooseClassTendency(rng, background);
            var classId = classTendency["startingClassId"].Value<string>();
            var disciplineProfile = ChooseDisciplineProfile(rng, classId);
            var visibleTraits = ChooseVisibleTraits(rng);
            var hidden = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["hiddenTraits"], "hiddenTraits"));
            var fear = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["fears"], "fears"));
            var ambition = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["ambitions"], "ambitions"));
            var growth = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["growthPatterns"], "growthPatterns"));
            var leadTendency = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["leadershipTendencies"], "leadershipTendencies"));
            if (StringComparer.Ordinal.Equals(growth["id"].Value<string>(), "GROWTH_NATURAL_LEADER"))
            {
                leadTendency = RecruitmentDeterminism.FindById(
                    RequiredArray(_tables["leadershipTendencies"], "leadershipTendencies"),
                    rng.NextBounded(100) < 50 ? "LEAD_ROUTE_AUTHORITY" : "LEAD_CONSENSUS");
            }
            var relationship = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["relationshipTendencies"], "relationshipTendencies"));
            var dialogue = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["dialogueStyles"], "dialogueStyles"));
            var eventHook = (JObject)RecruitmentDeterminism.Choose(rng, RequiredArray(_tables["personalEventHooks"], "personalEventHooks"));
            var age = WeightedIdChoice(rng, RequiredArray(_tables["ageBands"], "ageBands"));
            var pronoun = WeightedIdChoice(rng, RequiredArray(_tables["pronouns"], "pronouns"));
            var community = RecruitmentDeterminism.Choose(
                rng, RequiredArray(_tables["startingCommunityByRace"]?[raceId], "startingCommunityByRace." + raceId)).Value<string>();

            var recruitIdentity = new JArray
            {
                JToken.FromObject(request.CampaignSeed), request.GuildDay, request.RefreshIndex, request.SlotIndex,
                request.SourceChannel, extraSalt
            };
            var recruitId = "PROC_" + RecruitmentDeterminism.StableHash(recruitIdentity, 16);

            var disciplines = new Dictionary<string, int>(StringComparer.Ordinal);
            var profileBase = (JObject)disciplineProfile["base"];
            var profileJitter = disciplineProfile["jitter"]?.Value<int>() ?? 13;
            foreach (var axis in RecruitmentDeterminism.Disciplines)
            {
                var value = profileBase[axis].Value<int>() + rng.NextInclusive(-profileJitter, profileJitter);
                disciplines.Add(axis, RecruitmentDeterminism.Clamp(value, 52, 152));
            }

            var statTendencies = new Dictionary<string, StatTendency>(StringComparer.Ordinal);
            var finalCurve = ((JArray)growth["curve"]).Last.Value<int>();
            var classStats = (JObject)_tables["classStatProfiles"]?[classId];
            if (classStats == null) throw new InvalidOperationException("Missing class stat profile " + classId + ".");
            foreach (var property in classStats.Properties())
            {
                var bounds = (JArray)property.Value;
                statTendencies.Add(property.Name, new StatTendency
                {
                    BaseIndex = rng.NextInclusive(bounds[0].Value<int>(), bounds[1].Value<int>()),
                    GrowthBias = RecruitmentDeterminism.Clamp(finalCurve + rng.NextInclusive(-12, 12), 65, 155)
                });
            }

            var leadershipRange = (JArray)leadTendency["baseRange"];
            var leadershipScore = rng.NextInclusive(leadershipRange[0].Value<int>(), leadershipRange[1].Value<int>())
                                  + (growth["leadershipBonus"]?.Value<int>() ?? 0);
            leadershipScore = RecruitmentDeterminism.Clamp(leadershipScore, 20, 99);
            var commandBandwidth = RecruitmentDeterminism.Clamp(2 + leadershipScore / 23, 2, 6);

            var templateId = classTendency["equipmentTemplateId"].Value<string>();
            var weaponProfileId = WeaponProfileForTemplate(templateId);
            var weaponProfile = RecruitmentDeterminism.FindById(
                RequiredArray(_tables["weaponAptitudeProfiles"], "weaponAptitudeProfiles"), weaponProfileId);
            var weaponAptitudes = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var property in ((JObject)weaponProfile["weights"]).Properties())
            {
                weaponAptitudes.Add(property.Name,
                    RecruitmentDeterminism.Clamp(property.Value.Value<int>() + rng.NextInclusive(-10, 10), 55, 160));
            }
            var equipment = EquipmentFromTemplate(templateId, recruitId);

            var basePotential = (disciplines.Values.Sum() * 55
                                + statTendencies.Values.Sum(x => x.GrowthBias) * 35
                                + leadershipScore * 60) / 100;
            var potential = basePotential * growth["potentialMultiplierBasisPoints"].Value<int>() / 10000;
            potential += HiddenPotentialBonus(hidden["id"].Value<string>());
            var signingCost = 38 + Math.Max(0, potential - 480) / 6 + rng.NextInclusive(0, 18);

            var visualParts = new object[keyParts.Length + 1];
            Array.Copy(keyParts, visualParts, keyParts.Length);
            visualParts[visualParts.Length - 1] = "VISUAL";
            var visualRng = RecruitmentDeterminism.Rng(visualParts);
            var visualGeneration = (JObject)_tables["visualGeneration"];
            var visualTable = (JObject)visualGeneration[raceId];
            var visualComponents = new List<string>
            {
                RecruitmentDeterminism.Choose(visualRng, (JArray)visualTable["bodyFrames"]).Value<string>(),
                RecruitmentDeterminism.Choose(visualRng, PaletteArray(visualTable)).Value<string>(),
                RecruitmentDeterminism.Choose(visualRng, (JArray)visualTable["hairStyles"]).Value<string>(),
                RecruitmentDeterminism.Choose(visualRng, HairColorArray(visualTable)).Value<string>(),
                RecruitmentDeterminism.Choose(visualRng, (JArray)visualTable["eyeColors"]).Value<string>()
            };
            var raceFeatures = visualTable["raceFeatures"] as JArray;
            if (raceFeatures != null && raceFeatures.Count > 0)
                visualComponents.Add(RecruitmentDeterminism.Choose(visualRng, raceFeatures).Value<string>());
            visualComponents.Add(RecruitmentDeterminism.Choose(visualRng, (JArray)visualGeneration["outfitSilhouettes"]).Value<string>());
            visualComponents.Add(RecruitmentDeterminism.Choose(visualRng, (JArray)visualGeneration["accessories"]).Value<string>());
            var visualHashPayload = new JArray
            {
                RecruitmentDeterminism.SeedString(visualParts),
                new JArray(visualComponents)
            };

            return new OpeningRecruitRecord
            {
                RecruitId = recruitId,
                SourceType = "PROCEDURAL",
                SignatureId = null,
                GenerationSeed = RecruitmentDeterminism.SeedString(keyParts),
                VisualSeed = RecruitmentDeterminism.StableHash(visualHashPayload, 24),
                DisplayName = first + " " + family,
                RaceId = raceId,
                HomeCommunityId = community,
                AgeBandId = age,
                PronounId = pronoun,
                BackgroundId = background["id"].Value<string>(),
                StartingClassId = classId,
                VisibleTraitIds = Array.AsReadOnly(visibleTraits),
                HiddenTraitId = hidden["id"].Value<string>(),
                HiddenTraitRevealed = false,
                FearId = fear["id"].Value<string>(),
                AmbitionId = ambition["id"].Value<string>(),
                GrowthPatternId = growth["id"].Value<string>(),
                LeadershipTendencyId = leadTendency["id"].Value<string>(),
                WeaponAptitudeProfileId = weaponProfileId,
                DisciplineProfileId = disciplineProfile["id"].Value<string>(),
                RelationshipTendencyId = relationship["id"].Value<string>(),
                DialogueStyleId = dialogue["id"].Value<string>(),
                PersonalEventHookId = eventHook["id"].Value<string>(),
                DisciplineAptitudes = disciplines,
                StatTendencies = statTendencies,
                WeaponAptitudes = weaponAptitudes,
                LeadershipScore = leadershipScore,
                CommandBandwidth = commandBandwidth,
                StartingArtIds = Array.AsReadOnly(RecruitmentDeterminism.StringArray(classTendency["startingArts"])),
                EquipmentLoadout = equipment,
                DevelopmentPotentialScore = potential,
                SigningCostXp = signingCost,
                VariantFlags = Array.AsReadOnly(visualComponents.ToArray())
            };
        }

        public OpeningRecruitRecord RevealHiddenTrait(OpeningRecruitRecord recruit)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            var token = JObject.FromObject(recruit, JsonSerializer.Create(SecondDimension.Determinism.CanonicalJson.DefaultSettings()));
            token["hiddenTraitRevealed"] = true;
            return token.ToObject<OpeningRecruitRecord>(JsonSerializer.Create(SecondDimension.Determinism.CanonicalJson.DefaultSettings()));
        }

        public ScoutingReport CreateScoutingReport(
            OpeningRecruitRecord recruit,
            int officeTier,
            int scoutingAccuracyBase,
            int scoutSkill,
            IReadOnlyList<string> revealFlags = null)
        {
            if (recruit == null) throw new ArgumentNullException(nameof(recruit));
            var accuracy = RecruitmentDeterminism.Clamp(scoutingAccuracyBase + scoutSkill / 5, 25, 98);
            var uncertainty = Math.Max(3, 31 - accuracy / 4);
            var estimates = new Dictionary<string, AptitudeEstimate>(StringComparer.Ordinal);
            foreach (var axis in RecruitmentDeterminism.Disciplines)
            {
                var actual = recruit.DisciplineAptitudes[axis];
                var rng = RecruitmentDeterminism.Rng(recruit.GenerationSeed, "SCOUT_AXIS", officeTier, scoutSkill, axis);
                var center = RecruitmentDeterminism.Clamp(actual + rng.NextInclusive(-uncertainty, uncertainty), 40, 165);
                var low = RecruitmentDeterminism.Clamp(center - uncertainty, 35, 165);
                var high = RecruitmentDeterminism.Clamp(center + uncertainty, 35, 165);
                var midpoint = (low + high) / 2;
                estimates.Add(axis, new AptitudeEstimate
                {
                    EstimatedRange = Array.AsReadOnly(new[] { low, high }),
                    Label = AptitudeLabel(midpoint)
                });
            }

            JObject growth = null;
            foreach (var candidate in RequiredArray(_tables["growthPatterns"], "growthPatterns"))
            {
                if (StringComparer.Ordinal.Equals(candidate["id"].Value<string>(), recruit.GrowthPatternId))
                {
                    growth = (JObject)candidate;
                    break;
                }
            }

            string growthText;
            if (growth == null)
                growthText = "AUTHORED_DEVELOPMENT_PATTERN";
            else if (accuracy < 45)
                growthText = "HARD_TO_READ";
            else if (accuracy < 68)
                growthText = ScoutText(growth["scoutDescriptors"][0].Value<string>());
            else if (accuracy < 86)
                growthText = ScoutText(string.Join(" / ", RecruitmentDeterminism.StringArray(growth["scoutDescriptors"])));
            else
                growthText = ScoutText(growth["displayName"].Value<string>());

            var estimatedTotal = estimates.Values.Sum(x => (x.EstimatedRange[0] + x.EstimatedRange[1]) / 2);
            var quality = estimatedTotal < 510 ? "LIMITED_NOW"
                : estimatedTotal < 585 ? "STEADY_OR_SPECIALIZED"
                : estimatedTotal < 660 ? "PROMISING"
                : "UNUSUAL_MEASUREMENTS";

            var reveal = recruit.HiddenTraitRevealed;
            if (!reveal && revealFlags != null)
            {
                for (var i = 0; i < revealFlags.Count; i++)
                {
                    if (StringComparer.Ordinal.Equals(revealFlags[i], "HIDDEN_TRAIT_ALL")
                        || StringComparer.Ordinal.Equals(revealFlags[i], recruit.HiddenTraitId))
                    {
                        reveal = true;
                        break;
                    }
                }
            }

            return new ScoutingReport
            {
                RecruitId = recruit.RecruitId,
                DisplayName = recruit.DisplayName,
                RaceId = recruit.RaceId,
                HomeCommunityId = recruit.HomeCommunityId,
                StartingClassId = recruit.StartingClassId,
                BackgroundId = recruit.BackgroundId,
                VisibleTraitIds = recruit.VisibleTraitIds,
                LeadershipEstimate = new ValueRange
                {
                    Range = Array.AsReadOnly(new[]
                    {
                        RecruitmentDeterminism.Clamp(recruit.LeadershipScore - uncertainty, 0, 100),
                        RecruitmentDeterminism.Clamp(recruit.LeadershipScore + uncertainty, 0, 100)
                    }),
                    Confidence = accuracy
                },
                DisciplineEstimates = estimates,
                GrowthAssessment = growthText,
                QualityProfile = quality,
                ExactPotentialDisplayed = false,
                HiddenTraitRevealed = reveal,
                HiddenDataWithheld = !reveal,
                ScoutingAccuracy = accuracy,
                HiddenTraitId = reveal ? recruit.HiddenTraitId : null
            };
        }

        private JObject ChooseBackground(Pcg32 rng, string raceId)
        {
            var eligible = new List<JObject>();
            foreach (var token in RequiredArray(_tables["backgrounds"], "backgrounds"))
            {
                var raceIds = RecruitmentDeterminism.StringArray(token["allowedRaceIds"]);
                if (raceIds.Contains("ALL", StringComparer.Ordinal) || raceIds.Contains(raceId, StringComparer.Ordinal))
                    eligible.Add((JObject)token);
            }
            return RecruitmentDeterminism.ChooseToken(rng, eligible);
        }

        private JObject ChooseClassTendency(Pcg32 rng, JObject background)
        {
            var tags = new HashSet<string>(RecruitmentDeterminism.StringArray(background["aptitudeTags"]), StringComparer.Ordinal);
            var weighted = new List<WeightedValue<JObject>>();
            foreach (var token in RequiredArray(_tables["classTendencies"], "classTendencies"))
            {
                var tendency = (JObject)token;
                var weight = 45;
                foreach (var property in ((JObject)tendency["weightByTag"]).Properties())
                    if (tags.Contains(property.Name)) weight += property.Value.Value<int>();
                weighted.Add(new WeightedValue<JObject>(tendency, checked((uint)weight)));
            }
            return rng.WeightedChoice(weighted);
        }

        private JObject ChooseDisciplineProfile(Pcg32 rng, string classId)
        {
            var preferred = PreferredDisciplineProfiles(classId);
            var profiles = RequiredArray(_tables["disciplineProfiles"], "disciplineProfiles");
            if (rng.NextBounded(100) < 14) return (JObject)RecruitmentDeterminism.Choose(rng, profiles);
            return RecruitmentDeterminism.FindById(profiles, RecruitmentDeterminism.ChooseToken(rng, preferred));
        }

        private string[] ChooseVisibleTraits(Pcg32 rng)
        {
            var values = RequiredArray(_tables["visibleTraits"], "visibleTraits");
            var first = (JObject)RecruitmentDeterminism.Choose(rng, values);
            var incompatible = new HashSet<string>(
                first["incompatibleWith"] == null ? Array.Empty<string>() : RecruitmentDeterminism.StringArray(first["incompatibleWith"]),
                StringComparer.Ordinal);
            var candidates = new List<JObject>();
            foreach (var token in values)
            {
                var candidate = (JObject)token;
                var candidateId = candidate["id"].Value<string>();
                var reverse = candidate["incompatibleWith"] == null
                    ? Array.Empty<string>()
                    : RecruitmentDeterminism.StringArray(candidate["incompatibleWith"]);
                if (!StringComparer.Ordinal.Equals(candidateId, first["id"].Value<string>())
                    && !incompatible.Contains(candidateId)
                    && !reverse.Contains(first["id"].Value<string>(), StringComparer.Ordinal))
                    candidates.Add(candidate);
            }
            var second = RecruitmentDeterminism.ChooseToken(rng, candidates);
            return new[] { first["id"].Value<string>(), second["id"].Value<string>() };
        }

        private EquipmentLoadout EquipmentFromTemplate(string templateId, string recruitId)
        {
            var template = (JObject)_tables["equipmentTemplates"]?[templateId];
            if (template == null) throw new InvalidOperationException("Missing equipment template " + templateId + ".");
            var accessories = (JArray)template["accessories"];
            var slots = new Dictionary<string, EquipmentItem>(StringComparer.Ordinal)
            {
                { "MAIN_HAND", MaterializeItem(recruitId + "_MAIN", (JObject)template["main"]) },
                { "OFF_HAND", template["off"] == null || template["off"].Type == JTokenType.Null ? null : MaterializeItem(recruitId + "_OFF", (JObject)template["off"]) },
                { "BODY", MaterializeItem(recruitId + "_BODY", (JObject)template["body"]) },
                { "ACCESSORY_1", MaterializeAccessory(recruitId + "_A1", accessories[0].Value<string>()) },
                { "ACCESSORY_2", MaterializeAccessory(recruitId + "_A2", accessories[1].Value<string>()) },
                { "TOOL_RELIC", null }
            };
            return new EquipmentLoadout
            {
                LoadoutId = "LOADOUT_" + recruitId,
                Slots = slots,
                AggregateBonuses = new Dictionary<string, int>(StringComparer.Ordinal),
                AutoEquipAllowed = false
            };
        }

        private static EquipmentItem MaterializeItem(string instanceId, JObject source)
        {
            return new EquipmentItem
            {
                InstanceId = instanceId,
                ItemDefinitionId = source["itemDefinitionId"].Value<string>(),
                Tags = Array.AsReadOnly(RecruitmentDeterminism.StringArray(source["tags"])),
                Locked = false,
                Condition = "STANDARD"
            };
        }

        private static EquipmentItem MaterializeAccessory(string instanceId, string itemDefinitionId)
        {
            return new EquipmentItem
            {
                InstanceId = instanceId,
                ItemDefinitionId = itemDefinitionId,
                Tags = Array.AsReadOnly(new[] { "ACCESSORY" }),
                Locked = false,
                Condition = "STANDARD"
            };
        }

        private static string WeightedIdChoice(Pcg32 rng, JArray values)
        {
            var weighted = new List<WeightedValue<string>>();
            foreach (var token in values)
                weighted.Add(new WeightedValue<string>(token["id"].Value<string>(), checked((uint)token["weight"].Value<int>())));
            return rng.WeightedChoice(weighted);
        }

        private static JArray PaletteArray(JObject visualTable)
        {
            return (JArray)(visualTable["skinPalettes"] ?? visualTable["furPalettes"] ?? new JArray("DEFAULT"));
        }

        private static JArray HairColorArray(JObject visualTable)
        {
            return (JArray)(visualTable["hairColors"] ?? new JArray("FUR_MATCH"));
        }

        private static string AptitudeLabel(int value)
        {
            if (value < 75) return "LIMITED";
            if (value < 90) return "DEVELOPING";
            if (value < 105) return "CAPABLE";
            if (value < 120) return "STRONG";
            if (value < 135) return "EXCEPTIONAL";
            return "RARE_APTITUDE";
        }

        private static string ScoutText(string text) => text.ToUpperInvariant().Replace(" ", "_");

        private static string[] PreferredDisciplineProfiles(string classId)
        {
            switch (classId)
            {
                case "CLASS_GUARDIAN": return new[] { "DISC_FRONT_GUARD", "DISC_ARCANE_GUARD", "DISC_GUARD_SUPPORT", "DISC_BALANCED" };
                case "CLASS_WARRIOR": return new[] { "DISC_BREAKER", "DISC_MARTIAL_TACTICAL", "DISC_LATE_ANCHOR", "DISC_BALANCED" };
                case "CLASS_PRIEST": return new[] { "DISC_HEALER", "DISC_RESTORATION_TACTICAL", "DISC_GUARD_SUPPORT", "DISC_BALANCED" };
                case "CLASS_MAGE": return new[] { "DISC_SUPPORT_MYSTIC", "DISC_MYSTIC_BURST", "DISC_SIGNALER", "DISC_BALANCED" };
                case "CLASS_RANGER": return new[] { "DISC_SCOUT_MEDIC", "DISC_COMMAND_SCOUT", "DISC_MARTIAL_TACTICAL", "DISC_BALANCED" };
                case "CLASS_ROGUE": return new[] { "DISC_DUSK_STALKER", "DISC_MARTIAL_TACTICAL", "DISC_UNPREDICTABLE", "DISC_BALANCED" };
                default: throw new InvalidOperationException("Unsupported opening class " + classId + ".");
            }
        }

        private static string WeaponProfileForTemplate(string templateId)
        {
            switch (templateId)
            {
                case "LOADOUT_TEMPLATE_GUARDIAN": return "WEAPON_PROFILE_SPEAR_SHIELD";
                case "LOADOUT_TEMPLATE_WARRIOR": return "WEAPON_PROFILE_GREAT_AXE";
                case "LOADOUT_TEMPLATE_PRIEST": return "WEAPON_PROFILE_STAFF";
                case "LOADOUT_TEMPLATE_MAGE": return "WEAPON_PROFILE_WAND_FOCUS";
                case "LOADOUT_TEMPLATE_RANGER": return "WEAPON_PROFILE_SHORTBOW_KIT";
                case "LOADOUT_TEMPLATE_ROGUE": return "WEAPON_PROFILE_DUAL_DAGGER";
                case "LOADOUT_TEMPLATE_WARDBLADE": return "WEAPON_PROFILE_WARD_BLADE";
                case "LOADOUT_TEMPLATE_TRAIL_MEDIC": return "WEAPON_PROFILE_SHORTBOW_KIT";
                case "LOADOUT_TEMPLATE_SIGNALER": return "WEAPON_PROFILE_SIGNAL_FOCUS";
                case "LOADOUT_TEMPLATE_PORTER": return "WEAPON_PROFILE_POLEARM_TOOL";
                case "LOADOUT_TEMPLATE_DUSK_SCOUT": return "WEAPON_PROFILE_DUAL_DAGGER";
                case "LOADOUT_TEMPLATE_GENERALIST": return "WEAPON_PROFILE_RANDOM_ADAPTIVE";
                default: throw new InvalidOperationException("Unsupported opening loadout template " + templateId + ".");
            }
        }

        private static int HiddenPotentialBonus(string hiddenId)
        {
            switch (hiddenId)
            {
                case "HIDDEN_PROCEDURAL_LEGEND_SEED":
                case "HIDDEN_FALSE_MEDIOCRITY": return 62;
                case "HIDDEN_UNBOUND_LEARNER":
                case "HIDDEN_TWIN_DISCIPLINE":
                case "HIDDEN_UNFINISHED_CLASS": return 44;
                case "HIDDEN_LATE_FIRE":
                case "HIDDEN_SLOW_CERTAINTY":
                case "HIDDEN_MENTORS_HEIR": return 30;
                default: return 12;
            }
        }

        private static JArray RequiredArray(JToken token, string path)
        {
            var array = token as JArray;
            if (array == null) throw new InvalidOperationException("Missing recruitment array " + path + ".");
            return array;
        }
    }
}
