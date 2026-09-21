using System;
using System.Collections.Generic;
using System.IO;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.FirstHour071
{
    /// <summary>
    /// Adds the four named Skyhome charter companions and the ten rescued Lantern
    /// Patrol members to the permanent roster. The operation is deterministic and
    /// idempotent: it never rolls applicants, spends Treasury XP, or replaces an
    /// existing member. Kael and the Founders are absent by construction.
    /// </summary>
    public sealed class FirstHourRosterService071
    {
        private const string DormitoriesFacilityId = "FACILITY_DORMITORIES";

        private sealed class AuthoredRecruit071
        {
            public AuthoredRecruit071(string signatureId, string stableRecruitId, string worldId)
            {
                SignatureId = signatureId;
                StableRecruitId = stableRecruitId;
                WorldId = worldId;
            }

            public string SignatureId { get; }
            public string StableRecruitId { get; }
            public string WorldId { get; }
        }

        private static readonly AuthoredRecruit071[] CharterSource =
        {
            Recruit("SIG_W01_07", "SIGREC_TALA_STORMROAD", "WORLD_GATE_01"),
            Recruit("SIG_W01_08", "SIGREC_ORREN_CLAY", "WORLD_GATE_01"),
            Recruit("SIG_W01_09", "SIGREC_BESSA_BRASSWHISTLE", "WORLD_GATE_01"),
            Recruit("SIG_W01_10", "SIGREC_VAELIS_NOCT", "WORLD_GATE_01")
        };

        private static readonly AuthoredRecruit071[] PatrolSource =
        {
            Recruit("SIG_W02_01", "SIGREC_ZORIN_BRAMBLECROSS", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_02", "SIGREC_UNA_QUEENSREST", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_03", "SIGREC_JUNIA_SKYWARD", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_04", "SIGREC_DAIN_DEEPWELL", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_05", "SIGREC_WILLOW_LONGSTRIDE", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_06", "SIGREC_QUIN_LOWEN", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_07", "SIGREC_ASTER_MARSHLIGHT", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_08", "SIGREC_PETRA_RUNEBROOK", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_09", "SIGREC_QUIN_CROWNHILL", "WORLD_EMBERCHAIN_02"),
            Recruit("SIG_W02_10", "SIGREC_YVES_THORNFIELD", "WORLD_EMBERCHAIN_02")
        };

        private static readonly IReadOnlyList<string> CharterIds = Array.AsReadOnly(new[]
        {
            "SIGREC_TALA_STORMROAD", "SIGREC_ORREN_CLAY",
            "SIGREC_BESSA_BRASSWHISTLE", "SIGREC_VAELIS_NOCT"
        });

        private static readonly IReadOnlyList<string> PatrolIds = Array.AsReadOnly(new[]
        {
            "SIGREC_ZORIN_BRAMBLECROSS", "SIGREC_UNA_QUEENSREST",
            "SIGREC_JUNIA_SKYWARD", "SIGREC_DAIN_DEEPWELL",
            "SIGREC_WILLOW_LONGSTRIDE", "SIGREC_QUIN_LOWEN",
            "SIGREC_ASTER_MARSHLIGHT", "SIGREC_PETRA_RUNEBROOK",
            "SIGREC_QUIN_CROWNHILL", "SIGREC_YVES_THORNFIELD"
        });

        private readonly SignatureRecruitMaterializer _materializer;
        private readonly RecruitAutoGenerationSigningService010 _autoGeneration;

        private FirstHourRosterService071(
            SignatureRecruitMaterializer materializer,
            RecruitAutoGenerationSigningService010 autoGeneration)
        {
            _materializer = materializer ?? throw new ArgumentNullException(nameof(materializer));
            _autoGeneration = autoGeneration ?? throw new ArgumentNullException(nameof(autoGeneration));
        }

        public static IReadOnlyList<string> CharterStableRecruitIds => CharterIds;
        public static IReadOnlyList<string> PatrolStableRecruitIds => PatrolIds;

        public static FirstHourRosterService071 LoadFromContentRoot(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot))
                throw new ArgumentException("Content root is required.", nameof(contentRoot));

            string Read(params string[] parts)
            {
                var path = contentRoot;
                for (var index = 0; index < parts.Length; index++)
                    path = Path.Combine(path, parts[index]);
                return File.ReadAllText(path);
            }

            var fullSignatureContent = RecruitmentContent.FromJson(
                Read("OPENING_PROCEDURAL_TABLES.json"),
                Read("CONTENT_AUTHORITY_002", "DATA", "SIGNATURE_RECRUITS_300.json"),
                Read("RECRUITMENT_OFFICE_PROGRESSION.json"));
            var generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot));
            return new FirstHourRosterService071(
                new SignatureRecruitMaterializer(fullSignatureContent),
                new RecruitAutoGenerationSigningService010(new M1CommandService(), generator));
        }

        public Result<CampaignState> EnsureCharterRoster(CampaignState campaign)
        {
            return EnsureWave(campaign, CharterSource, "FIRST_HOUR_071_CHARTER");
        }

        public Result<CampaignState> EnsureLanternPatrol(CampaignState campaign)
        {
            return EnsureWave(campaign, PatrolSource, "FIRST_HOUR_071_LANTERN_PATROL");
        }

        private Result<CampaignState> EnsureWave(
            CampaignState campaign,
            IReadOnlyList<AuthoredRecruit071> wave,
            string sourceChannel)
        {
            if (campaign == null) return Result<CampaignState>.Failure("FH071_CAMPAIGN_REQUIRED");
            try
            {
                var recruits = new List<RecruitState>(campaign.Guild.Recruits);
                var rosterChanged = false;
                for (var index = 0; index < wave.Count; index++)
                {
                    var authored = wave[index];
                    var materialized = _materializer.Materialize(
                        campaign.CampaignSeed, authored.SignatureId, sourceChannel);
                    var existingIndex = FindAuthoredRecruitIndex(recruits, authored);
                    if (existingIndex >= 0)
                    {
                        var canonicalEquipment = BuildEquipment(materialized.EquipmentLoadout);
                        var repairedEquipment = RepairMissingEquipmentTags(
                            recruits[existingIndex].Equipment,
                            canonicalEquipment,
                            out var equipmentChanged);
                        if (equipmentChanged)
                        {
                            recruits[existingIndex] = recruits[existingIndex].WithEquipment(repairedEquipment);
                            rosterChanged = true;
                        }
                        continue;
                    }
                    recruits.Add(CreateRecruit(materialized, authored));
                    rosterChanged = true;
                }

                var assignmentsChanged = !HasAssignmentsForAll(
                    campaign.Guild.GuildCity.MemberAssignments, recruits);
                var assignments = EnsureAssignments(
                    campaign.Guild.GuildCity.MemberAssignments, recruits);
                var development = EnsureRosterCapacity(
                    campaign.Guild.Development, recruits.Count);
                var developmentChanged = !ReferenceEquals(
                    development, campaign.Guild.Development);
                if (!rosterChanged && !assignmentsChanged && !developmentChanged)
                    return Result<CampaignState>.Success(campaign);

                var city = campaign.Guild.GuildCity.With(
                    memberAssignments: assignments,
                    lastCheckpointId: sourceChannel);
                var guild = campaign.Guild.With(
                        campaign.Guild.TreasuryXp,
                        recruits.AsReadOnly(),
                        campaign.Guild.Unions,
                        campaign.Guild.Inventory,
                        development)
                    .WithGuildCity(city);
                return Result<CampaignState>.Success(campaign.With(guild, campaign.OpeningFlow));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "FH071_ROSTER_WAVE_REJECTED: " + exception.Message);
            }
        }

        private RecruitState CreateRecruit(
            OpeningRecruitRecord materialized,
            AuthoredRecruit071 authored)
        {
            if (!ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    materialized.RecruitId,
                    materialized.SignatureId,
                    RecruitAuthorityKind.Normal) ||
                !ProtectedActorPolicy.CanEnterNormalApplicantOrRoster(
                    authored.StableRecruitId,
                    authored.SignatureId,
                    RecruitAuthorityKind.Normal))
                throw new InvalidOperationException("Protected actors cannot enter the first-hour roster.");

            var maximumHp = 55 + materialized.StatTendencies["HP"].BaseIndex * 2;
            var maximumMp = Math.Max(0, 4 +
                (materialized.StatTendencies["MAGIC"].BaseIndex - 40) / 3);
            var equipment = BuildEquipment(materialized.EquipmentLoadout);
            var recruit = new RecruitState(
                materialized.RecruitId,
                maximumHp,
                maximumHp,
                maximumMp,
                maximumMp,
                materialized.DisplayName,
                RecruitOriginKind.Signature,
                materialized.SignatureId,
                materialized.RaceId,
                authored.WorldId,
                materialized.StartingClassId,
                LeadershipBand(materialized.LeadershipScore),
                materialized.DevelopmentPotentialScore,
                RecruitAuthorityKind.Normal,
                CanonicalJson.Serialize(materialized),
                string.Empty,
                equipment,
                true,
                string.Empty,
                authored.StableRecruitId,
                materialized.LeadershipScore,
                materialized.DisciplineAptitudes["TACTICAL"]);
            return _autoGeneration.InitializeRecruit(recruit);
        }

        private static EquipmentLoadoutState BuildEquipment(EquipmentLoadout loadout)
        {
            var assignments = new List<EquipmentSlotAssignmentState>();
            if (loadout?.Slots == null)
                throw new InvalidOperationException("FH071_EQUIPMENT_LOADOUT_REQUIRED");
            if (!loadout.Slots.TryGetValue("MAIN_HAND", out var mainHand) || mainHand == null)
                throw new InvalidOperationException("FH071_MAIN_HAND_REQUIRED");
            foreach (var pair in loadout.Slots)
            {
                var item = pair.Value;
                if (item == null) continue;
                if (item.Tags == null || item.Tags.Count == 0)
                    throw new InvalidOperationException(
                        "FH071_EQUIPMENT_TAGS_REQUIRED: " + item.ItemDefinitionId);
                // World-two authority also carries future HEAD/ARMS/LEGS slots.
                // The live campaign schema has six opening slots, so retain those
                // future pieces in CanonicalApplicantJson and equip every slot the
                // current schema can represent.
                if (!TryStateSlotId(pair.Key, out var slotId)) continue;
                var stateItem = new EquipmentItemState(
                    item.InstanceId,
                    item.ItemDefinitionId,
                    item.ItemDefinitionId,
                    new[] { slotId },
                    item.Tags,
                    string.Empty,
                    10000,
                    item.Locked);
                assignments.Add(new EquipmentSlotAssignmentState(slotId, stateItem));
            }
            assignments.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.SlotId, right.SlotId));
            return new EquipmentLoadoutState(assignments.AsReadOnly());
        }

        private static EquipmentLoadoutState RepairMissingEquipmentTags(
            EquipmentLoadoutState current,
            EquipmentLoadoutState canonical,
            out bool changed)
        {
            changed = false;
            if (current == null || canonical == null) return current;
            var assignments = new List<EquipmentSlotAssignmentState>();
            for (var index = 0; index < current.Assignments.Count; index++)
            {
                var assignment = current.Assignments[index];
                var item = assignment.Item;
                var canonicalAssignment = canonical.Find(assignment.SlotId);
                if (item.EquipmentTags.Count == 0 &&
                    canonicalAssignment != null &&
                    StringComparer.Ordinal.Equals(
                        item.DefinitionId,
                        canonicalAssignment.Item.DefinitionId))
                {
                    item = new EquipmentItemState(
                        item.InstanceId,
                        item.DefinitionId,
                        item.DisplayName,
                        item.ValidSlotIds,
                        canonicalAssignment.Item.EquipmentTags,
                        item.QualityId,
                        item.ConditionBasisPoints,
                        item.PlayerLocked);
                    changed = true;
                }
                assignments.Add(new EquipmentSlotAssignmentState(assignment.SlotId, item));
            }
            return changed
                ? new EquipmentLoadoutState(assignments.AsReadOnly())
                : current;
        }

        private static IReadOnlyList<GuildMemberAssignmentState017D> EnsureAssignments(
            IReadOnlyList<GuildMemberAssignmentState017D> source,
            IReadOnlyList<RecruitState> recruits)
        {
            var result = new List<GuildMemberAssignmentState017D>(
                source ?? Array.Empty<GuildMemberAssignmentState017D>());
            for (var recruitIndex = 0; recruitIndex < recruits.Count; recruitIndex++)
            {
                var recruitId = recruits[recruitIndex].RecruitId;
                var exists = false;
                for (var assignmentIndex = 0; assignmentIndex < result.Count; assignmentIndex++)
                    if (StringComparer.Ordinal.Equals(result[assignmentIndex].RecruitId, recruitId))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    result.Add(new GuildMemberAssignmentState017D(
                        recruitId, GuildMemberAssignmentKind017D.Reserve,
                        string.Empty, 0, 0, 0));
            }
            result.Sort((left, right) =>
                StringComparer.Ordinal.Compare(left.RecruitId, right.RecruitId));
            return result.AsReadOnly();
        }

        private static int FindAuthoredRecruitIndex(
            IReadOnlyList<RecruitState> recruits,
            AuthoredRecruit071 authored)
        {
            for (var index = 0; index < recruits.Count; index++)
            {
                var recruit = recruits[index];
                if (StringComparer.Ordinal.Equals(
                        recruit.SignatureId, authored.SignatureId) ||
                    StringComparer.Ordinal.Equals(
                        recruit.AuthoredStableRecruitId, authored.StableRecruitId) ||
                    StringComparer.Ordinal.Equals(
                        recruit.RecruitId, authored.StableRecruitId))
                    return index;
            }
            return -1;
        }

        private static bool HasAssignmentsForAll(
            IReadOnlyList<GuildMemberAssignmentState017D> assignments,
            IReadOnlyList<RecruitState> recruits)
        {
            if (assignments == null) return recruits.Count == 0;
            for (var recruitIndex = 0; recruitIndex < recruits.Count; recruitIndex++)
            {
                var found = false;
                for (var assignmentIndex = 0; assignmentIndex < assignments.Count; assignmentIndex++)
                {
                    if (!StringComparer.Ordinal.Equals(
                            assignments[assignmentIndex].RecruitId,
                            recruits[recruitIndex].RecruitId)) continue;
                    found = true;
                    break;
                }
                if (!found) return false;
            }
            return true;
        }

        private static GuildDevelopmentState EnsureRosterCapacity(
            GuildDevelopmentState development,
            int rosterCount)
        {
            if (development == null) development = GuildDevelopmentState.Default();
            var effects = new GuildCityEffectService017D();
            // Receipt-backed earned members already have roster capacity. Do not
            // grant free Dormitory upgrades for them again during resume repair.
            var baseHousingRosterCount094 = Math.Max(0, rosterCount -
                GuildCityRecruitmentService017D.EarnedRecruitCapacity094(development));
            var requiredLevel = 0;
            while (requiredLevel < GuildCityEffectService017D.MaximumAuthoredGuildHousingLevel &&
                   effects.RosterCapacityAtDormitoryLevel(requiredLevel) < baseHousingRosterCount094)
                requiredLevel++;
            var currentLevel = 0;
            for (var index = 0; index < development.Facilities.Count; index++)
            {
                var facility = development.Facilities[index];
                if (!StringComparer.Ordinal.Equals(
                        facility.FacilityId, DormitoriesFacilityId)) continue;
                currentLevel = facility.Level;
                break;
            }
            return currentLevel >= requiredLevel
                ? development
                : development.SetFacilityLevel(
                    DormitoriesFacilityId, requiredLevel, facilityXpContribution: 0);
        }

        private static string LeadershipBand(int score)
        {
            if (score >= 85) return "HIGH";
            if (score >= 65) return "STEADY";
            return "DEVELOPING";
        }

        private static bool TryStateSlotId(
            string recruitmentSlotId,
            out string stateSlotId)
        {
            switch (recruitmentSlotId)
            {
                case "MAIN_HAND": stateSlotId = EquipmentSlotIds.MainHand; return true;
                case "OFF_HAND": stateSlotId = EquipmentSlotIds.OffHand; return true;
                case "BODY": stateSlotId = EquipmentSlotIds.BodyArmor; return true;
                case "ACCESSORY_1": stateSlotId = EquipmentSlotIds.AccessoryOne; return true;
                case "ACCESSORY_2": stateSlotId = EquipmentSlotIds.AccessoryTwo; return true;
                case "TOOL_RELIC": stateSlotId = EquipmentSlotIds.ToolRelic; return true;
                default: stateSlotId = string.Empty; return false;
            }
        }

        private static AuthoredRecruit071 Recruit(
            string signatureId,
            string stableRecruitId,
            string worldId)
        {
            return new AuthoredRecruit071(signatureId, stableRecruitId, worldId);
        }
    }
}
