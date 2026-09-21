using System;
using System.Collections.Generic;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.State;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Campaign022;

namespace SecondDimension.Gameplay.M2
{
    /// <summary>
    /// Deterministic Slice 021 reward authority. Enemy and enemy-Union values come
    /// from Pass 03 JSON; terminal outcome and mode scaling are explicit here.
    /// </summary>
    public static class M2ProgressionRewards
    {
        public const string RewardRulesVersion = "M2_REWARDS_021_V3_BATTLE_PROFILES";
        public const string EquipmentRewardDefinitionId = "LOOT020_SKYHOME_01";
        public const string EquipmentRewardDisplayName = "First-Gate Sword";
        public const string LanternRoadEquipmentRewardDefinitionId = "LOOT020_SKYHOME_03";
        public const string LanternRoadEquipmentRewardDisplayName = "Alliance Spear";
        public const string GateEaterEquipmentRewardDefinitionId = "LOOT020_SKYHOME_08";
        public const string GateEaterEquipmentRewardDisplayName = "Skyhome Shield";
        public const int VictoryMultiplierPermille = 1000;
        public const int RetreatMultiplierPermille = 350;
        public const int DefeatMultiplierPermille = 200;

        public static int OutcomeMultiplierPermille(BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.Victory: return VictoryMultiplierPermille;
                case BattleOutcome.Retreat: return RetreatMultiplierPermille;
                case BattleOutcome.Defeat: return DefeatMultiplierPermille;
                default: throw new ArgumentOutOfRangeException(nameof(outcome), "Only terminal outcomes grant rewards.");
            }
        }

        public static long ScalePositive(
            long baseAmount,
            int enemyUnionMultiplierPermille,
            int outcomeMultiplierPermille,
            int modePercent)
        {
            if (baseAmount <= 0) throw new ArgumentOutOfRangeException(nameof(baseAmount));
            if (enemyUnionMultiplierPermille <= 0)
                throw new ArgumentOutOfRangeException(nameof(enemyUnionMultiplierPermille));
            if (outcomeMultiplierPermille <= 0)
                throw new ArgumentOutOfRangeException(nameof(outcomeMultiplierPermille));
            if (modePercent < 0) throw new ArgumentOutOfRangeException(nameof(modePercent));
            var scaled = checked(baseAmount * enemyUnionMultiplierPermille);
            scaled = checked(scaled * outcomeMultiplierPermille);
            scaled = checked(scaled * modePercent);
            scaled /= 1000L * 1000L * 100L;
            return Math.Max(1L, scaled);
        }

        public static BattleRewardState CreatePending(
            CampaignState campaign,
            BattleState terminalBattle,
            M2CombatContent content)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (terminalBattle == null) throw new ArgumentNullException(nameof(terminalBattle));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (terminalBattle.Outcome == BattleOutcome.InProgress)
                throw new ArgumentException("A pending reward requires a terminal battle.", nameof(terminalBattle));

            long basePersonalXp = 0;
            long baseTreasuryXp = 0;
            var resolvedEnemyCount070 = 0;
            var allResolvedEnemiesKnown070 = true;
            for (var unionIndex = 0; unionIndex < terminalBattle.EnemyUnions.Count; unionIndex++)
            {
                var union = terminalBattle.EnemyUnions[unionIndex];
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    if (!content.TryEnemy(member.ClassId, out var resolvedEnemy070))
                    {
                        allResolvedEnemiesKnown070 = false;
                        continue;
                    }
                    resolvedEnemyCount070++;
                    basePersonalXp = checked(basePersonalXp + resolvedEnemy070.PersonalXpReward);
                    baseTreasuryXp = checked(baseTreasuryXp + resolvedEnemy070.GuildTreasuryXpReward);
                }
            }
            var usesResolvedRoster070 = allResolvedEnemiesKnown070 && resolvedEnemyCount070 > 0;
            if (!usesResolvedRoster070)
            {
                basePersonalXp = 0;
                baseTreasuryXp = 0;
                for (var index = 0; index < content.TutorialEnemyUnion.MemberIds.Count; index++)
                {
                    var enemy = content.Enemy(content.TutorialEnemyUnion.MemberIds[index]);
                    basePersonalXp = checked(basePersonalXp + enemy.PersonalXpReward);
                    baseTreasuryXp = checked(baseTreasuryXp + enemy.GuildTreasuryXpReward);
                }
            }

            var outcomeMultiplier = OutcomeMultiplierPermille(terminalBattle.Outcome);
            var enemyRewardMultiplier = usesResolvedRoster070
                ? 1000
                : content.TutorialEnemyUnion.RewardMultiplierPermille;
            var personalXp = ScalePositive(
                basePersonalXp,
                enemyRewardMultiplier,
                outcomeMultiplier,
                campaign.Rules.PersonalXpPct);
            var treasuryXp = ScalePositive(
                baseTreasuryXp,
                enemyRewardMultiplier,
                outcomeMultiplier,
                campaign.Rules.TreasuryXpPct);
            var routeModifiers017H = campaign.Guild.GuildCity?.PendingEncounter?.RouteModifiers;
            var personalXpBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers017H, "CITY_PERSONAL_XP_BP_");
            var guildXpBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers017H, "CITY_GUILD_XP_BP_");
            var hallXpBonus017H = GuildCityBattleModifierRules017H.Amount(routeModifiers017H, "CITY_HALL_XP_BP_");
            personalXp = GuildCityBattleModifierRules017H.ApplyBasisPoints(personalXp, personalXpBonus017H);
            var baseScaledTreasury017H = treasuryXp;
            treasuryXp = GuildCityBattleModifierRules017H.ApplyBasisPoints(baseScaledTreasury017H, guildXpBonus017H);
            var hallXp017H = GuildCityBattleModifierRules017H.ApplyBasisPoints(baseScaledTreasury017H, hallXpBonus017H);
            var replay163 = terminalBattle.Progression163;
            if (replay163?.HasReplayBonus == true)
            {
                personalXp = replay163.ScaleReplayReward(personalXp);
                treasuryXp = replay163.ScaleReplayReward(treasuryXp);
                hallXp017H = replay163.ScaleReplayReward(hallXp017H);
            }
            var towerBonus159 = TowerEconomy159.ReadBonus159(routeModifiers017H);
            personalXp = TowerEconomy159.ApplyBonus159(personalXp, towerBonus159);
            treasuryXp = TowerEconomy159.ApplyBonus159(treasuryXp, towerBonus159);
            hallXp017H = TowerEconomy159.ApplyBonus159(hallXp017H, towerBonus159);
            var townTreasury159 = terminalBattle.TownBonuses159?.TreasuryBasisPoints ?? 0;
            var townPersonal159 = terminalBattle.TownBonuses159?.PersonalBasisPoints ?? 0;
            treasuryXp = TowerEconomy159.ApplyBonus159(treasuryXp, townTreasury159);
            personalXp = TowerEconomy159.ApplyBonus159(personalXp, townPersonal159);
            var version159 = replay163?.HasReplayBonus == true ? "M2_REWARDS_163_REPLAY_HP_RATIO_V1" :
                towerBonus159 != 0 || townTreasury159 != 0 || townPersonal159 != 0
                ? "M2_REWARDS_159_INDEPENDENT_PROGRESSION_V1" : RewardRulesVersion;
            var rewards = new List<BattleMemberRewardState>();
            for (var unionIndex = 0; unionIndex < terminalBattle.PlayerUnions.Count; unionIndex++)
            {
                var union = terminalBattle.PlayerUnions[unionIndex];
                for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
                {
                    var member = union.Members[memberIndex];
                    var recruit = FindRecruit(campaign.Guild.Recruits, member.MemberId);
                    var previous = recruit.Progression;
                    var projected = previous.GainPersonalXp(personalXp, member.ClassId);
                    rewards.Add(new BattleMemberRewardState(
                        member.MemberId,
                        member.DisplayName,
                        personalXp,
                        previous.Level,
                        projected.Level,
                        projected.MaximumHpBonus - previous.MaximumHpBonus,
                        projected.MaximumMpBonus - previous.MaximumMpBonus,
                        projected.StrengthBonus - previous.StrengthBonus,
                        projected.DefenseBonus - previous.DefenseBonus,
                        projected.AgilityBonus - previous.AgilityBonus,
                        projected.MagicBonus - previous.MagicBonus,
                        projected.WillBonus - previous.WillBonus));
                }
            }

            var equipmentProfile = EquipmentProfileForBattle(terminalBattle.BattleId);

            var rewardIdHash = CanonicalJson.Sha256Hex(new
            {
                RewardRulesVersion,
                campaign.CampaignGuid,
                terminalBattle.BattleId,
                terminalBattle.InitialBattleStateHash,
                terminalBattle.Round,
                terminalBattle.Outcome,
                campaign.Guild.Development.LifetimeTreasuryXpEarned,
                BasePersonalXp = basePersonalXp,
                BaseTreasuryXp = baseTreasuryXp,
                EnemyUnionMultiplier = enemyRewardMultiplier,
                OutcomeMultiplier = outcomeMultiplier,
                campaign.Rules.PersonalXpPct,
                campaign.Rules.TreasuryXpPct,
                personalXpBonus017H,
                guildXpBonus017H,
                hallXpBonus017H,
                EquipmentRewardProfile = new
                {
                    equipmentProfile.DefinitionId,
                    equipmentProfile.DisplayName,
                    equipmentProfile.ValidSlotIds,
                    equipmentProfile.EquipmentTags,
                    QualityId = "QUALITY_STANDARD",
                    ConditionBasisPoints = 10000,
                    PlayerLocked = false
                }
            });
            if (version159 != RewardRulesVersion)
                rewardIdHash = CanonicalJson.Sha256Hex(new {
                    OriginalRewardHash = rewardIdHash, RewardRulesVersion = version159,
                    towerBonus159, townTreasury159, townPersonal159 });
            if (replay163?.HasReplayBonus == true)
                rewardIdHash = CanonicalJson.Sha256Hex(new { OriginalRewardHash = rewardIdHash,
                    replay163.ReplayHpNumerator, replay163.ReplayHpDenominator });
            var equipmentReward = new EquipmentItemState(
                "BATTLE_ITEM_" + rewardIdHash.Substring(0, 24).ToUpperInvariant(),
                equipmentProfile.DefinitionId,
                equipmentProfile.DisplayName,
                equipmentProfile.ValidSlotIds,
                equipmentProfile.EquipmentTags,
                "QUALITY_STANDARD",
                10000,
                false);
            return new BattleRewardState(
                "BATTLE_REWARD_" + rewardIdHash.Substring(0, 24).ToUpperInvariant(),
                version159,
                terminalBattle.Outcome,
                basePersonalXp,
                baseTreasuryXp,
                enemyRewardMultiplier,
                outcomeMultiplier,
                campaign.Rules.PersonalXpPct,
                campaign.Rules.TreasuryXpPct,
                treasuryXp,
                hallXp017H,
                rewards.AsReadOnly(),
                claimed: false,
                equipmentReward: equipmentReward);
        }

        private static EquipmentRewardProfile021 EquipmentProfileForBattle(string battleId)
        {
            if (!string.IsNullOrWhiteSpace(battleId) &&
                battleId.IndexOf("ENCOUNTER071_LANTERN_ROAD_AMBUSH", StringComparison.Ordinal) >= 0)
                return new EquipmentRewardProfile021(
                    LanternRoadEquipmentRewardDefinitionId,
                    LanternRoadEquipmentRewardDisplayName,
                    new[] { EquipmentSlotIds.MainHand },
                    new[] { "SPEAR", "WEAPON" });

            if (!string.IsNullOrWhiteSpace(battleId) &&
                battleId.IndexOf("ENCOUNTER071_GATE_EATER", StringComparison.Ordinal) >= 0)
                return new EquipmentRewardProfile021(
                    GateEaterEquipmentRewardDefinitionId,
                    GateEaterEquipmentRewardDisplayName,
                    new[] { EquipmentSlotIds.OffHand },
                    new[] { "SHIELD", "ARMOR" });

            return new EquipmentRewardProfile021(
                EquipmentRewardDefinitionId,
                EquipmentRewardDisplayName,
                new[] { EquipmentSlotIds.MainHand },
                new[] { "SWORD", "WEAPON" });
        }

        private sealed class EquipmentRewardProfile021
        {
            public EquipmentRewardProfile021(
                string definitionId,
                string displayName,
                IReadOnlyList<string> validSlotIds,
                IReadOnlyList<string> equipmentTags)
            {
                DefinitionId = definitionId;
                DisplayName = displayName;
                ValidSlotIds = validSlotIds;
                EquipmentTags = equipmentTags;
            }

            public string DefinitionId { get; }
            public string DisplayName { get; }
            public IReadOnlyList<string> ValidSlotIds { get; }
            public IReadOnlyList<string> EquipmentTags { get; }
        }

        private static RecruitState FindRecruit(IReadOnlyList<RecruitState> recruits, string recruitId)
        {
            for (var index = 0; index < recruits.Count; index++)
                if (StringComparer.Ordinal.Equals(recruits[index].RecruitId, recruitId)) return recruits[index];
            throw new InvalidOperationException("Battle participant is missing from the guild roster: " + recruitId + ".");
        }
    }
}
