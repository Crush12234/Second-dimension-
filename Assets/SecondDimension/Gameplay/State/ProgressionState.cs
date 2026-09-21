using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.State
{
    /// <summary>
    /// Ten-copy duplicate growth. The saved rank starts at zero for heroes that
    /// have never been merged, then advances exactly once per matching copy.
    /// Existing combat projections already consume these bounded stat bonuses.
    /// </summary>
    public static class RecruitAscensionRules089
    {
        public const int MinimumLevel = 0;
        public const int MaximumLevel = 10;
        public const int MaximumHpBonusPerLevel = 12;
        public const int MaximumMpBonusPerLevel = 4;
        public const int CoreStatBonusPerLevel = 2;
        public const long FullyMasteredPersonalXpFallback = 500;
    }

    [Serializable]
    public sealed class RecruitArtMasteryState
    {
        [JsonConstructor]
        public RecruitArtMasteryState(string artId, string discipline, int meaningfulUses, int masteryPoints)
        {
            ArtId = Require(artId, nameof(artId));
            Discipline = discipline ?? string.Empty;
            if (meaningfulUses < 0) throw new ArgumentOutOfRangeException(nameof(meaningfulUses));
            if (masteryPoints < 0) throw new ArgumentOutOfRangeException(nameof(masteryPoints));
            MeaningfulUses = meaningfulUses;
            MasteryPoints = masteryPoints;
        }

        public string ArtId { get; }
        public string Discipline { get; }
        public int MeaningfulUses { get; }
        public int MasteryPoints { get; }

        private static string Require(string value, string parameter) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable Art ID is required.", parameter)
                : value;
    }

    /// <summary>
    /// Persistent, campaign-authoritative recruit growth. RecruitState keeps authored
    /// base vitals while this state records all battle-earned additions.
    /// </summary>
    [Serializable]
    public sealed class RecruitProgressionState
    {
        [JsonConstructor]
        public RecruitProgressionState(
            int level,
            long totalPersonalXp,
            int maximumHpBonus,
            int maximumMpBonus,
            int strengthBonus,
            int defenseBonus,
            int agilityBonus,
            int magicBonus,
            int willBonus,
            IReadOnlyList<string> learnedArtIds,
            IReadOnlyList<RecruitArtMasteryState> artMastery,
            IReadOnlyList<string> unlockedTreeIds = null,
            int? ascensionLevel = null, int progressionVersion152 = 0, long? catchUpOriginXp152 = null)
        {
            if (totalPersonalXp < 0) throw new ArgumentOutOfRangeException(nameof(totalPersonalXp));
            if(progressionVersion152<0||progressionVersion152>1)throw new ArgumentOutOfRangeException(nameof(progressionVersion152));
            if(progressionVersion152==0 ? catchUpOriginXp152!=null : !catchUpOriginXp152.HasValue||catchUpOriginXp152.Value<0||catchUpOriginXp152.Value>totalPersonalXp)
                throw new ArgumentException("Invalid Catch Up XP origin migration.");
            ProgressionVersion152=progressionVersion152;CatchUpOriginXp152=catchUpOriginXp152;
            var calculatedLevel = progressionVersion152==0 ? RecruitProgressionRules021.LevelForTotalXp(totalPersonalXp) : RecruitGrowthRules152.Level(totalPersonalXp);
            if (level != calculatedLevel)
                throw new ArgumentException("Recruit level must match total personal XP.", nameof(level));
            Level = level;
            TotalPersonalXp = totalPersonalXp;
            MaximumHpBonus = NonNegative(maximumHpBonus, nameof(maximumHpBonus));
            MaximumMpBonus = NonNegative(maximumMpBonus, nameof(maximumMpBonus));
            StrengthBonus = NonNegative(strengthBonus, nameof(strengthBonus));
            DefenseBonus = NonNegative(defenseBonus, nameof(defenseBonus));
            AgilityBonus = NonNegative(agilityBonus, nameof(agilityBonus));
            MagicBonus = NonNegative(magicBonus, nameof(magicBonus));
            WillBonus = NonNegative(willBonus, nameof(willBonus));
            LearnedArtIds = CopyUniqueStrings(learnedArtIds);
            ArtMastery = CopyUniqueMastery(artMastery);
            UnlockedTreeIds = CopyUniqueStrings(unlockedTreeIds);
            AscensionLevel = ascensionLevel ?? RecruitAscensionRules089.MinimumLevel;
            if (AscensionLevel < RecruitAscensionRules089.MinimumLevel ||
                AscensionLevel > RecruitAscensionRules089.MaximumLevel)
                throw new ArgumentOutOfRangeException(nameof(ascensionLevel));
        }

        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling=DefaultValueHandling.Ignore)]
        public int ProgressionVersion152{get;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public long? CatchUpOriginXp152{get;}
        public int Level { get; }
        public long TotalPersonalXp { get; }
        public int MaximumHpBonus { get; }
        public int MaximumMpBonus { get; }
        public int StrengthBonus { get; }
        public int DefenseBonus { get; }
        public int AgilityBonus { get; }
        public int MagicBonus { get; }
        public int WillBonus { get; }
        public IReadOnlyList<string> LearnedArtIds { get; }
        public IReadOnlyList<RecruitArtMasteryState> ArtMastery { get; }
        public IReadOnlyList<string> UnlockedTreeIds { get; }

        // Current V11 saves predate AscensionLevel. Omitting the zero default keeps
        // their canonical JSON (and therefore their existing save hash) unchanged.
        [System.ComponentModel.DefaultValue(0)]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AscensionLevel { get; }

        public long XpIntoCurrentLevel =>
            TotalPersonalXp - (ProgressionVersion152==0?RecruitProgressionRules021.TotalXpRequiredForLevel(Level):RecruitGrowthRules152.Threshold(Level));

        public long XpRequiredForNextLevel => ProgressionVersion152!=0 ? RecruitGrowthRules152.NextGap(Level) : Level >= RecruitProgressionRules021.MaximumLevel
            ? 0
            : RecruitProgressionRules021.TotalXpRequiredForLevel(Level + 1) -
              RecruitProgressionRules021.TotalXpRequiredForLevel(Level);

        public RecruitProgressionState GainPersonalXp(long amount, string classId)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
            return GainToTotal152(checked(TotalPersonalXp + amount),classId,ProgressionVersion152);
        }

        // Used only through the safe native command boundary, after old battles settle.
        // Below-cap bonuses are retained; only previously banked levels are added.
        public RecruitProgressionState MigrateGrowth152(string savedClassId) =>
            ProgressionVersion152==1 ? this : GainToTotal152(TotalPersonalXp,savedClassId,1);

        private RecruitProgressionState GainToTotal152(long total,string classId,int version)
        {
            var newLevel = version==0?RecruitProgressionRules021.LevelForTotalXp(total):RecruitGrowthRules152.Level(total);
            var levelsGained = newLevel - Level;
            var maximumHpBonus = checked(MaximumHpBonus + levelsGained * 6);
            var maximumMpBonus = checked(MaximumMpBonus + levelsGained * 2);
            var strength = StrengthBonus;
            var defense = DefenseBonus;
            var agility = AgilityBonus;
            var magic = MagicBonus;
            var will = WillBonus;
            RecruitProgressionRules021.ApplyClassGrowth(
                classId,
                levelsGained,
                ref strength,
                ref defense,
                ref agility,
                ref magic,
                ref will);
            return new RecruitProgressionState(
                newLevel,
                total,
                maximumHpBonus,
                maximumMpBonus,
                strength,
                defense,
                agility,
                magic,
                will,
                LearnedArtIds,
                ArtMastery,
                UnlockedTreeIds,
                AscensionLevel, version, version==0?(long?)null:CatchUpOriginXp152??0);
        }

        public RecruitProgressionState WithArts(
            IReadOnlyList<string> learnedArtIds,
            IReadOnlyList<RecruitArtMasteryState> artMastery) =>
            new RecruitProgressionState(
                Level,
                TotalPersonalXp,
                MaximumHpBonus,
                MaximumMpBonus,
                StrengthBonus,
                DefenseBonus,
                AgilityBonus,
                MagicBonus,
                WillBonus,
                learnedArtIds,
                artMastery,
                UnlockedTreeIds,
                AscensionLevel, ProgressionVersion152, CatchUpOriginXp152);

        public RecruitProgressionState WithUnlockedTrees(IReadOnlyList<string> unlockedTreeIds) =>
            new RecruitProgressionState(
                Level,
                TotalPersonalXp,
                MaximumHpBonus,
                MaximumMpBonus,
                StrengthBonus,
                DefenseBonus,
                AgilityBonus,
                MagicBonus,
                WillBonus,
                LearnedArtIds,
                ArtMastery,
                unlockedTreeIds,
                AscensionLevel, ProgressionVersion152, CatchUpOriginXp152);

        public RecruitProgressionState Ascend089()
        {
            if (AscensionLevel >= RecruitAscensionRules089.MaximumLevel)
                throw new InvalidOperationException(
                    "Recruit Ascension is already at rank " +
                    RecruitAscensionRules089.MaximumLevel + ".");
            return new RecruitProgressionState(
                Level,
                TotalPersonalXp,
                checked(MaximumHpBonus + RecruitAscensionRules089.MaximumHpBonusPerLevel),
                checked(MaximumMpBonus + RecruitAscensionRules089.MaximumMpBonusPerLevel),
                checked(StrengthBonus + RecruitAscensionRules089.CoreStatBonusPerLevel),
                checked(DefenseBonus + RecruitAscensionRules089.CoreStatBonusPerLevel),
                checked(AgilityBonus + RecruitAscensionRules089.CoreStatBonusPerLevel),
                checked(MagicBonus + RecruitAscensionRules089.CoreStatBonusPerLevel),
                checked(WillBonus + RecruitAscensionRules089.CoreStatBonusPerLevel),
                LearnedArtIds,
                ArtMastery,
                UnlockedTreeIds,
                AscensionLevel + 1, ProgressionVersion152, CatchUpOriginXp152);
        }

        public static RecruitProgressionState Default() =>
            new RecruitProgressionState(
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                Array.Empty<string>(),
                Array.Empty<RecruitArtMasteryState>());

        private static int NonNegative(int value, string parameter) =>
            value < 0 ? throw new ArgumentOutOfRangeException(parameter) : value;

        private static IReadOnlyList<string> CopyUniqueStrings(IReadOnlyList<string> source)
        {
            var result = new List<string>();
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    var value = source[index];
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    if (!result.Contains(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }

        private static IReadOnlyList<RecruitArtMasteryState> CopyUniqueMastery(
            IReadOnlyList<RecruitArtMasteryState> source)
        {
            var result = new List<RecruitArtMasteryState>();
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    var value = source[index] ??
                        throw new ArgumentException("Art mastery entry cannot be null.", nameof(source));
                    for (var existing = 0; existing < result.Count; existing++)
                        if (StringComparer.Ordinal.Equals(result[existing].ArtId, value.ArtId))
                            throw new ArgumentException("Art mastery IDs must be unique.", nameof(source));
                    result.Add(value);
                }
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArtId, right.ArtId));
            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Slice 021 progression curve. The curve and class gains are explicit so a
    /// later content-authority pass can migrate them rather than silently changing saves.
    /// </summary>
    public static class RecruitProgressionRules021
    {
        public const string RulesVersion = "RECRUIT_PROGRESSION_021_V1";
        public const int MaximumLevel = 100;

        public static long TotalXpRequiredForLevel(int level)
        {
            if (level < 1 || level > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return checked(50L * (level - 1) * level);
        }

        public static int LevelForTotalXp(long totalPersonalXp)
        {
            if (totalPersonalXp < 0) throw new ArgumentOutOfRangeException(nameof(totalPersonalXp));
            var level = 1;
            while (level < MaximumLevel && totalPersonalXp >= TotalXpRequiredForLevel(level + 1)) level++;
            return level;
        }

        internal static void ApplyClassGrowth(
            string classId,
            int levelsGained,
            ref int strength,
            ref int defense,
            ref int agility,
            ref int magic,
            ref int will)
        {
            if (levelsGained <= 0) return;
            // Owned recruits retain CLASS_TEND_* identities while battle members
            // use CLASS_*. Every XP source must grant the same class growth.
            const string tendencyPrefix159 = "CLASS_TEND_";
            if (classId != null && classId.StartsWith(tendencyPrefix159, StringComparison.Ordinal))
                classId = "CLASS_" + classId.Substring(tendencyPrefix159.Length);
            switch (classId ?? string.Empty)
            {
                case "CLASS_GUARDIAN":
                    defense = checked(defense + levelsGained * 2);
                    will = checked(will + levelsGained);
                    break;
                case "CLASS_WARRIOR":
                    strength = checked(strength + levelsGained * 2);
                    defense = checked(defense + levelsGained);
                    break;
                case "CLASS_RANGER":
                case "CLASS_ROGUE":
                    agility = checked(agility + levelsGained * 2);
                    strength = checked(strength + levelsGained);
                    break;
                case "CLASS_MAGE":
                    magic = checked(magic + levelsGained * 2);
                    will = checked(will + levelsGained);
                    break;
                case "CLASS_PRIEST":
                    will = checked(will + levelsGained * 2);
                    magic = checked(magic + levelsGained);
                    break;
                default:
                    strength = checked(strength + levelsGained);
                    defense = checked(defense + levelsGained);
                    agility = checked(agility + levelsGained);
                    break;
            }
        }
    }

    public static class GuildProgressionRules021
    {
        public const string RulesVersion = "GUILD_PROGRESSION_021_V1";
        public const int MaximumLevel = 100;

        public static long TotalXpRequiredForLevel(int level)
        {
            if (level < 1 || level > MaximumLevel) throw new ArgumentOutOfRangeException(nameof(level));
            return checked(100L * (level - 1) * level);
        }

        public static int LevelForLifetimeXp(long lifetimeGuildXp)
        {
            if (lifetimeGuildXp < 0) throw new ArgumentOutOfRangeException(nameof(lifetimeGuildXp));
            var level = 1;
            while (level < MaximumLevel && lifetimeGuildXp >= TotalXpRequiredForLevel(level + 1)) level++;
            return level;
        }
    }

    [Serializable]
    public sealed class FacilityProgressionState
    {
        [JsonConstructor]
        public FacilityProgressionState(string facilityId, int level, long totalFacilityXp)
        {
            FacilityId = string.IsNullOrWhiteSpace(facilityId)
                ? throw new ArgumentException("Facility ID is required.", nameof(facilityId))
                : facilityId;
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            if (totalFacilityXp < 0) throw new ArgumentOutOfRangeException(nameof(totalFacilityXp));
            Level = level;
            TotalFacilityXp = totalFacilityXp;
        }

        public string FacilityId { get; }
        public int Level { get; }
        public long TotalFacilityXp { get; }
    }

    /// <summary>
    /// Persistent foundation for Pass 05 guild enhancement. Slice 021 initializes
    /// every authored facility at L0; upgrade commands remain a later slice.
    /// </summary>
    [Serializable]
    public sealed class GuildDevelopmentState
    {
        public const string FoundationVersion = "GUILD_DEVELOPMENT_021_V1";
        public const string InitialHallStageId = "HALL_STAGE_RUINED_ANNEX";
        public const int AdventureAuthorityEntryLimit = 262144;
        private readonly bool _serializeAppliedAdventureAuthorityIds;

        private static readonly string[] FacilityIds =
        {
            "FACILITY_RECRUITMENT_OFFICE",
            "FACILITY_DORMITORIES",
            "FACILITY_UNION_COMMAND_TABLE",
            "FACILITY_TRAINING_HALL",
            "FACILITY_INFIRMARY",
            "FACILITY_GUILD_FORGE",
            "FACILITY_ALCHEMY_WORKSHOP",
            "FACILITY_TAVERN",
            "FACILITY_LIBRARY",
            "FACILITY_STRATEGY_ROOM",
            "FACILITY_SYSTEM_ARCHIVE",
            "FACILITY_EMBASSY_HALL",
            "FACILITY_GATE_OPERATIONS",
            "FACILITY_ABYSS_LICENSING_DESK",
            "FACILITY_TROPHY_CHRONICLE_HALL",
            "FACILITY_GARDEN",
            "FACILITY_RESEARCH_ROOM",
            "FACILITY_QUARTERMASTER",
            "FACILITY_EMERGENCY_SHELTER",
            "FACILITY_MILITIA_BARRACKS",
            "FACILITY_ARTISANS_HALL",
            "FACILITY_COURIER_ROOST",
            "FACILITY_BALLISTA_FOUNDRY",
            "FACILITY_TRAPWORKS",
            "FACILITY_ALCHEMY_LABORATORY",
            "FACILITY_CHRONICLE_PLAZA",
            "FACILITY_EVACUATION_SHELTERS",
            "FACILITY_WARD_PYLON_ARRAY",
            "FACILITY_SIGNAL_SPIRE",
            "FACILITY_AEGIS_WALLWORKS",
            "FACILITY_GATEHOUSE_PORTCULLIS"
        };

        [JsonConstructor]
        public GuildDevelopmentState(
            int hallStageIndex,
            string hallStageId,
            long hallEnhancementXp,
            long lifetimeTreasuryXpEarned,
            IReadOnlyList<FacilityProgressionState> facilities,
            IReadOnlyList<string> claimedBattleRewardIds,
            IReadOnlyList<string> appliedAdventureAuthorityIds = null)
        {
            if (hallStageIndex < 0) throw new ArgumentOutOfRangeException(nameof(hallStageIndex));
            if (hallEnhancementXp < 0) throw new ArgumentOutOfRangeException(nameof(hallEnhancementXp));
            if (lifetimeTreasuryXpEarned < 0) throw new ArgumentOutOfRangeException(nameof(lifetimeTreasuryXpEarned));
            HallStageIndex = hallStageIndex;
            HallStageId = string.IsNullOrWhiteSpace(hallStageId) ? InitialHallStageId : hallStageId;
            HallEnhancementXp = hallEnhancementXp;
            LifetimeTreasuryXpEarned = lifetimeTreasuryXpEarned;
            Facilities = CopyFacilities(facilities);
            ClaimedBattleRewardIds = CopyUniqueRewardIds(claimedBattleRewardIds);
            _serializeAppliedAdventureAuthorityIds =
                appliedAdventureAuthorityIds != null;
            AppliedAdventureAuthorityIds = CopyUniqueRewardIds(
                appliedAdventureAuthorityIds);
            if(AppliedAdventureAuthorityIds.Count>AdventureAuthorityEntryLimit)
                throw new ArgumentException("Adventure authority ledger exceeds its safe limit.",
                    nameof(appliedAdventureAuthorityIds));
        }

        public int HallStageIndex { get; }
        public string HallStageId { get; }
        public long HallEnhancementXp { get; }
        public long LifetimeTreasuryXpEarned { get; }
        public IReadOnlyList<FacilityProgressionState> Facilities { get; }
        public IReadOnlyList<string> ClaimedBattleRewardIds { get; }
        public IReadOnlyList<string> AppliedAdventureAuthorityIds { get; }

        // This ledger was introduced while save format 11 was already live.
        // Preserve whether an empty ledger existed so validation accepts both
        // pre-ledger V11 saves and newer V11 saves that explicitly stored [].
        public bool ShouldSerializeAppliedAdventureAuthorityIds() =>
            _serializeAppliedAdventureAuthorityIds ||
            AppliedAdventureAuthorityIds.Count > 0;

        [JsonIgnore]
        public int GuildLevel => GuildProgressionRules021.LevelForLifetimeXp(LifetimeTreasuryXpEarned);

        [JsonIgnore]
        public long GuildXpIntoCurrentLevel =>
            LifetimeTreasuryXpEarned - GuildProgressionRules021.TotalXpRequiredForLevel(GuildLevel);

        [JsonIgnore]
        public long GuildXpRequiredForNextLevel => GuildLevel >= GuildProgressionRules021.MaximumLevel
            ? 0
            : GuildProgressionRules021.TotalXpRequiredForLevel(GuildLevel + 1) -
              GuildProgressionRules021.TotalXpRequiredForLevel(GuildLevel);

        public bool HasClaimedReward(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId)) return false;
            for (var index = 0; index < ClaimedBattleRewardIds.Count; index++)
                if (StringComparer.Ordinal.Equals(ClaimedBattleRewardIds[index], rewardId)) return true;
            return false;
        }

        public bool HasAdventureAuthority(string receiptId)
        {
            if(string.IsNullOrWhiteSpace(receiptId))return false;
            for(var index=0;index<AppliedAdventureAuthorityIds.Count;index++)
                if(StringComparer.Ordinal.Equals(AppliedAdventureAuthorityIds[index],
                       receiptId))return true;
            return false;
        }

        public GuildDevelopmentState RecordAdventureAuthority(string receiptId)
        {
            if(string.IsNullOrWhiteSpace(receiptId))throw new ArgumentException(
                "Adventure authority receipt ID is required.",nameof(receiptId));
            if(HasAdventureAuthority(receiptId))return this;
            if(AppliedAdventureAuthorityIds.Count>=AdventureAuthorityEntryLimit)
                throw new InvalidOperationException(
                    "Adventure authority ledger reached its safe limit.");
            var applied=new List<string>(AppliedAdventureAuthorityIds){receiptId};
            applied.Sort(StringComparer.Ordinal);
            return new GuildDevelopmentState(HallStageIndex,HallStageId,
                HallEnhancementXp,LifetimeTreasuryXpEarned,Facilities,
                ClaimedBattleRewardIds,applied.AsReadOnly());
        }

        public bool CanRecordAdventureAuthority(string receiptId) =>
            !string.IsNullOrWhiteSpace(receiptId)&&
            (HasAdventureAuthority(receiptId)||
             AppliedAdventureAuthorityIds.Count<AdventureAuthorityEntryLimit);

        public GuildDevelopmentState RecordTreasuryReward(string rewardId, long amount)
        {
            return RecordBattleReward(rewardId, amount, amount);
        }

        public GuildDevelopmentState RecordBattleReward(
            string rewardId,
            long treasuryXpAmount,
            long hallEnhancementXpAmount)
        {
            if (string.IsNullOrWhiteSpace(rewardId)) throw new ArgumentException("Reward ID is required.", nameof(rewardId));
            if (treasuryXpAmount <= 0) throw new ArgumentOutOfRangeException(nameof(treasuryXpAmount));
            if (hallEnhancementXpAmount <= 0) throw new ArgumentOutOfRangeException(nameof(hallEnhancementXpAmount));
            if (HasClaimedReward(rewardId)) return this;
            var claimed = new List<string>(ClaimedBattleRewardIds) { rewardId };
            claimed.Sort(StringComparer.Ordinal);
            return new GuildDevelopmentState(
                HallStageIndex,
                HallStageId,
                checked(HallEnhancementXp + hallEnhancementXpAmount),
                checked(LifetimeTreasuryXpEarned + treasuryXpAmount),
                Facilities,
                claimed.AsReadOnly(),AppliedAdventureAuthorityIds);
        }


        public GuildDevelopmentState SpendHallEnhancementXp(long amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount > HallEnhancementXp) throw new InvalidOperationException("Insufficient Hall Enhancement XP.");
            return new GuildDevelopmentState(HallStageIndex, HallStageId, HallEnhancementXp - amount,
                LifetimeTreasuryXpEarned, Facilities, ClaimedBattleRewardIds,
                AppliedAdventureAuthorityIds);
        }

        public GuildDevelopmentState SetFacilityLevel(string facilityId, int level, long facilityXpContribution)
        {
            if (string.IsNullOrWhiteSpace(facilityId)) throw new ArgumentException("Facility ID is required.", nameof(facilityId));
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));
            if (facilityXpContribution < 0) throw new ArgumentOutOfRangeException(nameof(facilityXpContribution));
            var facilities = new List<FacilityProgressionState>(Facilities);
            var found = false;
            for (var index = 0; index < facilities.Count; index++)
            {
                if (!StringComparer.Ordinal.Equals(facilities[index].FacilityId, facilityId)) continue;
                facilities[index] = new FacilityProgressionState(facilityId, level,
                    checked(facilities[index].TotalFacilityXp + facilityXpContribution));
                found = true;
                break;
            }
            if (!found) facilities.Add(new FacilityProgressionState(facilityId, level, facilityXpContribution));
            facilities.Sort((left, right) => StringComparer.Ordinal.Compare(left.FacilityId, right.FacilityId));
            return new GuildDevelopmentState(HallStageIndex, HallStageId, HallEnhancementXp,
                LifetimeTreasuryXpEarned, facilities.AsReadOnly(), ClaimedBattleRewardIds,
                AppliedAdventureAuthorityIds);
        }

        public static GuildDevelopmentState Default()
        {
            var facilities = new List<FacilityProgressionState>();
            for (var index = 0; index < FacilityIds.Length; index++)
                facilities.Add(new FacilityProgressionState(FacilityIds[index], 0, 0));
            facilities.Sort((left, right) => StringComparer.Ordinal.Compare(left.FacilityId, right.FacilityId));
            return new GuildDevelopmentState(
                0,
                InitialHallStageId,
                0,
                0,
                facilities.AsReadOnly(),
                Array.Empty<string>());
        }

        private static IReadOnlyList<FacilityProgressionState> CopyFacilities(
            IReadOnlyList<FacilityProgressionState> source)
        {
            if (source == null || source.Count == 0) return DefaultFacilityList();
            var result = new List<FacilityProgressionState>();
            for (var index = 0; index < source.Count; index++)
            {
                var value = source[index] ??
                    throw new ArgumentException("Facility progression cannot be null.", nameof(source));
                for (var existing = 0; existing < result.Count; existing++)
                    if (StringComparer.Ordinal.Equals(result[existing].FacilityId, value.FacilityId))
                        throw new ArgumentException("Facility progression IDs must be unique.", nameof(source));
                result.Add(value);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.FacilityId, right.FacilityId));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<FacilityProgressionState> DefaultFacilityList()
        {
            var result = new List<FacilityProgressionState>();
            for (var index = 0; index < FacilityIds.Length; index++)
                result.Add(new FacilityProgressionState(FacilityIds[index], 0, 0));
            result.Sort((left, right) => StringComparer.Ordinal.Compare(left.FacilityId, right.FacilityId));
            return result.AsReadOnly();
        }

        private static IReadOnlyList<string> CopyUniqueRewardIds(IReadOnlyList<string> source)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (source != null)
            {
                for (var index = 0; index < source.Count; index++)
                {
                    var value = source[index];
                    if (string.IsNullOrWhiteSpace(value))
                        throw new ArgumentException("Claimed reward ID cannot be blank.", nameof(source));
                    if (seen.Add(value)) result.Add(value);
                }
            }
            result.Sort(StringComparer.Ordinal);
            return result.AsReadOnly();
        }
    }
}
