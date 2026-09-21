using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Projects trusted SS Hero Master records into the existing Creator code
    /// contract. It owns no redemption state: CreatorAccessCommandService028 is
    /// still the sole authority for code and receipt ledgers.
    /// </summary>
    public sealed class HeroMaster300CreatorCodeCatalog087 : ICreatorContentCatalog028
    {
        public const string CodeIdPrefix = "CODE_HERO_MASTER_SS_087_";

        private readonly HeroMaster300Catalog087 _source;
        private readonly Dictionary<string, CreatorCodeRule028> _codesByHash;
        private readonly Dictionary<string, HeroMaster300Hero087> _heroesByRewardId;

        public HeroMaster300CreatorCodeCatalog087(HeroMaster300Catalog087 source)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _codesByHash = new Dictionary<string, CreatorCodeRule028>(StringComparer.Ordinal);
            _heroesByRewardId = new Dictionary<string, HeroMaster300Hero087>(StringComparer.Ordinal);

            foreach (var hero in source.AcceptedHeroes)
            {
                if (hero.Rank != HeroMasterRank087.SS) continue;
                if (hero.IsNormalApplicantEligible)
                    throw new InvalidOperationException("An SS Hero Master record cannot enter normal applicant eligibility.");

                var hash = CreatorAccessCommandService028.HashNormalizedCode(hero.SsGenerationCode);
                if (string.IsNullOrWhiteSpace(hash))
                    throw new InvalidOperationException("An accepted SS Hero Master record has no redeemable code hash.");

                var rule = new CreatorCodeRule028
                {
                    CodeId = CodeIdFor(hero),
                    CodeHash = hash,
                    Category = "CHARACTER",
                    RewardId = hero.StableId,
                    DisplayLabel = hero.Name + " • SS HERO",
                    ExactOncePerCampaign = true,
                    Active = true
                };

                if (_codesByHash.ContainsKey(hash) || _heroesByRewardId.ContainsKey(rule.RewardId))
                    throw new InvalidOperationException("Accepted SS Hero Master code authority is not unique.");
                _codesByHash.Add(hash, rule);
                _heroesByRewardId.Add(rule.RewardId, hero);
            }
        }

        public int CodeCount => _codesByHash.Count;
        public int RoomCount => 0;
        public IReadOnlyList<CreatorRoomRule028> AllRooms => Array.Empty<CreatorRoomRule028>();

        public static string CodeIdFor(HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            return CodeIdPrefix + hero.RosterId.ToString("D3", CultureInfo.InvariantCulture);
        }

        public bool TryResolveInput(
            string input,
            out CreatorCodeRule028 rule,
            out HeroMaster300Hero087 hero)
        {
            hero = null;
            var hash = CreatorAccessCommandService028.HashNormalizedCode(input);
            return TryGetCodeByHash(hash, out rule)
                   && _heroesByRewardId.TryGetValue(rule.RewardId, out hero);
        }

        public bool TryGetHeroForReward(string rewardId, out HeroMaster300Hero087 hero) =>
            _heroesByRewardId.TryGetValue(rewardId ?? string.Empty, out hero);

        public bool TryGetCodeByHash(string hash, out CreatorCodeRule028 rule)
        {
            rule = null;
            return !string.IsNullOrWhiteSpace(hash)
                   && _codesByHash.TryGetValue(hash, out rule);
        }

        public bool TryGetResource(string id, out CreatorResourceRule028 rule)
        {
            rule = null;
            return false;
        }

        public bool TryGetInvitation(string id, out CreatorInvitationRule028 rule)
        {
            rule = null;
            return false;
        }

        public bool TryGetWeapon(string id, out CreatorWeaponRule028 rule)
        {
            rule = null;
            return false;
        }

        public bool TryGetContent(string id, out CreatorContentRule028 rule)
        {
            rule = null;
            return false;
        }

        public bool TryGetRoomForBoard(
            string boardDefinitionId,
            string boardId,
            out CreatorRoomRule028 rule)
        {
            rule = null;
            return false;
        }
    }

    /// <summary>
    /// Converts one already-validated SS record into the existing normal RecruitState
    /// shape. The resulting grant contains no item and no equipment assignment; the
    /// existing deterministic RecruitAutoGeneration010 service initializes its Arts.
    /// </summary>
    public static class HeroMaster300CreatorRecruitProjection087
    {
        public const string RecruitIdPrefix = "HM300_SS_RECRUIT_087_";
        public const string ExpeditionApplicantRecruitIdPrefix =
            "HM300_EXPEDITION_RECRUIT_089_";
        public const string DefaultWorldId = "WORLD_GATE_01";

        public static CreatorRecruitGrant028 FromHero(
            HeroMaster300Hero087 hero,
            string worldId = DefaultWorldId)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (hero.Rank != HeroMasterRank087.SS || hero.IsNormalApplicantEligible)
                throw new InvalidOperationException("Only trusted SS Hero Master records can use code recruitment.");

            var record = BuildOpeningRecord(hero);
            var projected = CreatorRecruitProjection028.FromRecord(
                record,
                string.IsNullOrWhiteSpace(worldId) ? DefaultWorldId : worldId);
            var recruit = projected.Recruit;

            // Preserve the authored SS combat vitals while retaining the existing
            // canonical applicant, progression, save, and equipment representations.
            recruit = new RecruitState(
                recruit.RecruitId,
                hero.Hp,
                hero.Hp,
                hero.Ap,
                hero.Ap,
                recruit.DisplayName,
                RecruitOriginKind.Procedural,
                string.Empty,
                recruit.RaceId,
                recruit.WorldId,
                recruit.ClassTendencyId,
                recruit.LeadershipBand,
                recruit.PotentialBasisPoints,
                RecruitAuthorityKind.Normal,
                recruit.CanonicalApplicantJson,
                recruit.CanonicalScoutingReportJson,
                EquipmentLoadoutState.Empty(),
                true,
                recruit.TutorialAliasId,
                hero.StableId,
                recruit.LeadershipScore,
                recruit.TacticalAptitude,
                recruit.Progression);

            return new CreatorRecruitGrant028(recruit, Array.Empty<EquipmentItemState>());
        }

        public static bool RosterContainsHero(
            IEnumerable<RecruitState> roster,
            HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (roster == null) return false;
            var recruitId = RecruitIdFor(hero);
            return roster.Any(recruit => recruit != null &&
                (StringComparer.Ordinal.Equals(recruit.RecruitId, recruitId)
                 || StringComparer.Ordinal.Equals(recruit.AuthoredStableRecruitId, hero.StableId)
                 || StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId)
                 || StringComparer.Ordinal.Equals(recruit.SignatureId, hero.GameEntityId)));
        }

        public static string RecruitIdFor(HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            return RecruitIdPrefix + hero.RosterId.ToString("D3", CultureInfo.InvariantCulture);
        }

        public static OpeningRecruitRecord BuildOpeningRecord(HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (hero.Rank != HeroMasterRank087.SS || hero.IsNormalApplicantEligible)
                throw new InvalidOperationException("Only trusted SS Hero Master records can be projected.");

            return BuildOpeningRecordCore(
                hero,
                RecruitIdFor(hero),
                DefaultWorldId,
                "HERO_MASTER_300_SS_087|",
                "HERO_MASTER_300_SS_VISUAL_087|",
                "BACKGROUND_HERO_MASTER_SS",
                "TRAIT_SS_HERO",
                "GROWTH_SS_HERO",
                "LEADERSHIP_SS_HERO",
                "WEAPON_PROFILE_HERO_MASTER_SS",
                "DISCIPLINE_PROFILE_HERO_MASTER_SS",
                "DIALOGUE_HERO_MASTER_SS",
                0,
                Math.Min(1000, 800 + Math.Max(0, hero.StatBudget) / 5),
                new[] { "HERO_MASTER_300", "SS_CODE_RECRUIT", "MANUAL_EQUIPMENT_ONLY" });
        }

        /// <summary>
        /// Projects one trusted non-SS Hero Master definition into the same canonical
        /// procedural applicant record consumed by the existing Applicant Board and
        /// Auto-Generation 010 signing path. It does not add the recruit to a roster.
        /// </summary>
        public static OpeningRecruitRecord BuildExpeditionApplicantRecord089(
            HeroMaster300Hero087 hero,
            string worldId = DefaultWorldId)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (!hero.IsNormalApplicantEligible || hero.Rank == HeroMasterRank087.SS)
                throw new InvalidOperationException(
                    "Only trusted non-SS Hero Master records can enter the Applicant Board.");

            var rank = hero.Rank.ToString().ToUpperInvariant();
            var rankPotential = hero.Rank == HeroMasterRank087.S
                ? 790
                : hero.Rank == HeroMasterRank087.A ? 700 : 620;
            return BuildOpeningRecordCore(
                hero,
                ExpeditionApplicantRecruitIdFor089(hero),
                string.IsNullOrWhiteSpace(worldId) ? DefaultWorldId : worldId,
                "HERO_MASTER_300_EXPEDITION_089|",
                "HERO_MASTER_300_EXPEDITION_VISUAL_089|",
                "BACKGROUND_EXPEDITION_RECRUIT_LEAD",
                "TRAIT_HERO_MASTER_" + rank,
                "GROWTH_HERO_MASTER_" + rank,
                "LEADERSHIP_HERO_MASTER_" + rank,
                "WEAPON_PROFILE_HERO_MASTER_" + rank,
                "DISCIPLINE_PROFILE_HERO_MASTER_" + rank,
                "DIALOGUE_HERO_MASTER",
                hero.RecruitCostXp,
                Math.Min(1000, rankPotential + Math.Max(0, hero.StatBudget) / 8),
                new[]
                {
                    "HERO_MASTER_300",
                    "EXPEDITION_RECRUIT_LEAD_089",
                    "RANK_" + rank,
                    "MANUAL_EQUIPMENT_ONLY"
                });
        }

        public static string ExpeditionApplicantRecruitIdFor089(
            HeroMaster300Hero087 hero)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            return ExpeditionApplicantRecruitIdPrefix +
                   hero.RosterId.ToString("D3", CultureInfo.InvariantCulture);
        }

        private static OpeningRecruitRecord BuildOpeningRecordCore(
            HeroMaster300Hero087 hero,
            string recruitId,
            string worldId,
            string generationSeedPrefix,
            string visualSeedPrefix,
            string backgroundId,
            string visibleTraitId,
            string growthPatternId,
            string leadershipTendencyId,
            string weaponAptitudeProfileId,
            string disciplineProfileId,
            string dialogueStyleId,
            int signingCostXp,
            int developmentPotentialScore,
            IReadOnlyList<string> variantFlags)
        {

            var disciplines = BuildDisciplineAptitudes(hero);
            var weaponAptitudes096 = BuildWeaponAptitudes(hero);
            var class096 = HeroMasterPrimaryWeapon096.ClassForNewRecord(hero, CanonicalClassId(hero));
            var stats = new Dictionary<string, StatTendency>(StringComparer.Ordinal)
            {
                ["HP"] = Tendency(Math.Max(1, (hero.Hp - 55 + 1) / 2)),
                ["STR"] = Tendency(hero.Strength),
                ["DEF"] = Tendency(hero.Defense),
                ["AGI"] = Tendency(hero.Agility),
                ["MAGIC"] = Tendency(Math.Max(hero.Magic, 40 + Math.Max(0, hero.Ap - 4) * 3)),
                ["WILL"] = Tendency(hero.Resistance)
            };

            return new OpeningRecruitRecord
            {
                RecruitId = recruitId,
                SourceType = "PROCEDURAL",
                SignatureId = string.Empty,
                GenerationSeed = generationSeedPrefix + hero.StableId,
                VisualSeed = visualSeedPrefix + hero.StableId,
                DisplayName = hero.Name,
                RaceId = CanonicalRaceId(hero.Race),
                HomeCommunityId = worldId,
                AgeBandId = "ADULT",
                PronounId = "UNSPECIFIED",
                BackgroundId = backgroundId,
                StartingClassId = class096,
                VisibleTraitIds = Array.AsReadOnly(new[] { visibleTraitId }),
                HiddenTraitId = string.Empty,
                HiddenTraitRevealed = false,
                FearId = string.Empty,
                AmbitionId = "AMBITION_MASTER_THE_FINAL_ART",
                GrowthPatternId = growthPatternId,
                LeadershipTendencyId = leadershipTendencyId,
                WeaponAptitudeProfileId = weaponAptitudeProfileId,
                DisciplineProfileId = disciplineProfileId,
                RelationshipTendencyId = "RELATIONSHIP_GUILD_LOYAL",
                DialogueStyleId = dialogueStyleId,
                PersonalEventHookId = hero.FinalArtId,
                DisciplineAptitudes = disciplines,
                StatTendencies = stats,
                WeaponAptitudes = weaponAptitudes096,
                LeadershipScore = Math.Max(60, Math.Min(100,
                    (hero.Defense + hero.Resistance + hero.Agility) / 3)),
                CommandBandwidth = 6,
                StartingArtIds = Array.Empty<string>(),
                EquipmentLoadout = new EquipmentLoadout
                {
                    LoadoutId = "LOADOUT_EMPTY_" + recruitId,
                    Slots = new Dictionary<string, EquipmentItem>(StringComparer.Ordinal),
                    AggregateBonuses = new Dictionary<string, int>(StringComparer.Ordinal),
                    AutoEquipAllowed = false
                },
                DevelopmentPotentialScore = developmentPotentialScore,
                SigningCostXp = Math.Max(0, signingCostXp),
                VariantFlags = HeroMasterPrimaryWeapon096.FlagsForNewRecord(
                    hero, variantFlags, weaponAptitudes096)
            };
        }

        private static StatTendency Tendency(int baseIndex) =>
            new StatTendency { BaseIndex = Math.Max(1, baseIndex), GrowthBias = 0 };

        private static string CanonicalRaceId(string race)
        {
            var value = (race ?? string.Empty).ToUpperInvariant();
            if (value.Contains("DARK ELF")) return "DARK_ELF";
            if (value.Contains("DEMON")) return "DEMON_HERITAGE";
            if (value.Contains("GOBLIN")) return "GOBLIN";
            if (value.Contains("ORC")) return "ORC";
            if (value.Contains("CAT") || value.Contains("WOLF") || value.Contains("DOG"))
                return "DOG_TRIBE";
            // The current modular portrait authority has complete required layers
            // for these six canonical races only. Unsupported authored lineages use
            // the human rig without changing the Hero Master preview/identity.
            return "HUMAN";
        }

        internal static string LegacyClassIdForValidation096(HeroMaster300Hero087 hero) => CanonicalClassId(hero);

        private static string CanonicalClassId(HeroMaster300Hero087 hero)
        {
            var text = SearchText(hero);
            if (ContainsAny(text, "RESTORATION", "HEAL", "PRIEST", "CLERIC")) return "CLASS_PRIEST";
            if (ContainsAny(text, "SPELLBLADE", "ARCANE TANK", "UMBRAL", "INFERNO KNIGHT", "VOID SENTINEL")) return "CLASS_SPELLBLADE";
            if (ContainsAny(text, "BARD", "TACTICAL", "INTELLIGENCE", "BANNER", "SUPPORT")) return "CLASS_TACTICIAN";
            if (ContainsAny(text, "WARDEN", "BEAST TAMER")) return "CLASS_WARDEN";
            if (ContainsAny(text, "BERSERKER")) return "CLASS_BERSERKER";
            if (ContainsAny(text, "GUARDIAN", "TANK", "SENTINEL", "PALADIN", "VALKYRIE")) return "CLASS_GUARDIAN";
            if (ContainsAny(text, "MYSTIC", "MAGE", "SORCER", "WITCH", "BINDER", "MAGIC")) return "CLASS_MAGE";
            if (ContainsAny(text, "HUNTER", "RANGER", "ARCHER", "BOW", "GUNNER", "SCOUT")) return "CLASS_RANGER";
            if (ContainsAny(text, "ROGUE", "ASSASSIN", "DANCER", "DAGGER", "SHADOW")) return "CLASS_ROGUE";
            return "CLASS_WARRIOR";
        }

        private static IReadOnlyDictionary<string, int> BuildDisciplineAptitudes(
            HeroMaster300Hero087 hero)
        {
            var text = SearchText(hero);
            var result = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["MARTIAL"] = ClampAptitude(Math.Max(hero.Strength, hero.Agility)),
                ["MYSTIC"] = ClampAptitude(hero.Magic),
                ["RESTORATION"] = ContainsAny(text, "RESTORATION", "HEAL", "PRIEST", "CLERIC") ? 100 : ClampAptitude(hero.Magic / 2),
                ["SUPPORT"] = ContainsAny(text, "BARD", "SUPPORT", "BANNER", "TAMER") ? 96 : 55,
                ["GUARD"] = ContainsAny(text, "GUARD", "TANK", "WARDEN", "SENTINEL", "PALADIN", "SHIELD") ? 98 : ClampAptitude(hero.Defense),
                ["TACTICAL"] = ContainsAny(text, "TACTICAL", "INTELLIGENCE", "HUNTER", "BARD", "SCOUT") ? 94 : ClampAptitude(hero.Agility)
            };
            return result;
        }

        private static IReadOnlyDictionary<string, int> BuildWeaponAptitudes(
            HeroMaster300Hero087 hero)
        {
            var text = SearchText(hero);
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            AddWeapon(result, text, new[] { "SWORD", "BLADE" }, "SWORD");
            AddWeapon(result, text, new[] { "AXE" }, "AXE", "GREAT_AXE");
            AddWeapon(result, text, new[] { "HAMMER", "MACE" }, "HAMMER");
            AddWeapon(result, text, new[] { "SPEAR", "LANCE", "POLEARM" }, "SPEAR", "POLEARM");
            AddWeapon(result, text, new[] { "BOW", "HUNTER" }, "BOW", "SHORTBOW", "LONGBOW");
            AddWeapon(result, text, new[] { "DAGGER" }, "DAGGER");
            AddWeapon(result, text, new[] { "SHIELD", "BUCKLER", "WARD" }, "SHIELD", "WARD_BUCKLER", "SHIELD_COMPATIBLE");
            AddWeapon(result, text, new[] { "GAUNTLET", "CLAW", "BRAWLER", "FIST", "GREAVE", "KICKBOX" }, "UNARMED", "GAUNTLET");
            AddWeapon(result, text, new[] { "STAFF" }, "STAFF");
            AddWeapon(result, text, new[] { "FOCUS", "WAND", "MAGIC", "LUTE", "CHAKRAM" }, "FOCUS_TOOL", "WAND", "FOCUS");
            AddWeapon(result, text, new[] { "TOOL", "GEAR", "MECHANICAL", "SIDEARM" }, "TOOL", "ARCANE_CONDUCTOR");
            AddWeapon(result, text, new[] { "RELIC", "VOID" }, "HYBRID_RELIC");
            if (result.Count == 0) result["SWORD"] = 80;
            return result;
        }

        private static void AddWeapon(
            IDictionary<string, int> target,
            string text,
            IEnumerable<string> matches,
            params string[] tags)
        {
            if (!matches.Any(match => text.Contains(match))) return;
            foreach (var tag in tags) target[tag] = 100;
        }

        private static string SearchText(HeroMaster300Hero087 hero) =>
            string.Join(" | ", hero.Role, hero.Weapon, hero.ArtTree1, hero.ArtTree2).ToUpperInvariant();

        private static bool ContainsAny(string text, params string[] values) =>
            values.Any(text.Contains);

        private static int ClampAptitude(int value) => Math.Max(40, Math.Min(100, value));
    }
}
