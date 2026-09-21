using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using SecondDimension.Core;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    /// <summary>
    /// Frozen gameplay mapping for the 70 SSS enemy families. The mapping is
    /// deliberately exact: no display name, loose prefix, or saved art field can
    /// become a progression identity.
    /// </summary>
    public sealed class SssTenV4FamilyResolver090 : IBaseFamilyResolver
    {
        public static readonly SssTenV4FamilyResolver090 Instance =
            new SssTenV4FamilyResolver090();

        private readonly Dictionary<string, string> _families =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private SssTenV4FamilyResolver090()
        {
            for (var familyIndex = 1; familyIndex <= 70; familyIndex++)
            {
                var family = "ENEMY_REC_" +
                             familyIndex.ToString("000", CultureInfo.InvariantCulture);
                _families.Add(family, family);
                for (var variantIndex = 1; variantIndex <= 10; variantIndex++)
                    _families.Add(family + "_VAR_" +
                                  variantIndex.ToString("00", CultureInfo.InvariantCulture),
                        family);
            }
        }

        public bool TryResolve(string enemyDefinitionOrVariantId,
            out string baseFamilyId)
        {
            if (string.IsNullOrWhiteSpace(enemyDefinitionOrVariantId))
            {
                baseFamilyId = null;
                return false;
            }

            return _families.TryGetValue(
                enemyDefinitionOrVariantId.Trim().ToUpperInvariant(),
                out baseFamilyId);
        }
    }

    /// <summary>
    /// Binds package progression to the existing certified battle reward
    /// transaction. Nothing is awarded from animation or presentation callbacks.
    /// </summary>
    public static class SssTenV4ProgressionService090
    {
        public const string TowerBattlePrefix090 = "ABYSS_BATTLE022_FLOOR_";
        public const string WorldGateBattlePrefix090 = "WORLD_GATE_BATTLE_023_";
        public const string ExpeditionBattlePrefix090 = "EXPEDITION_CARD_BATTLE089_";

        public static bool IsTowerBattle090(string battleId) =>
            !string.IsNullOrWhiteSpace(battleId) &&
            battleId.StartsWith(TowerBattlePrefix090, StringComparison.Ordinal);

        public static bool IsCampaignBattle090(string battleId) =>
            !string.IsNullOrWhiteSpace(battleId) &&
            (battleId.StartsWith(WorldGateBattlePrefix090, StringComparison.Ordinal) ||
             battleId.StartsWith(ExpeditionBattlePrefix090, StringComparison.Ordinal));

        /// <summary>
        /// Call only from M2's existing reward-claim transaction after its normal
        /// receipt has been recorded and the BattleReward has been marked claimed.
        /// Replays are harmless because package defeat/victory receipts are stable.
        /// </summary>
        public static Result<CampaignState> ApplyClaimedBattleReward090(
            CampaignState campaign)
        {
            try
            {
                if (campaign?.Guild == null || campaign.Battle == null)
                    return Result<CampaignState>.Failure(
                        "SSS090_CLAIMED_BATTLE_REQUIRED");

                var battle = campaign.Battle;
                if (!IsCampaignBattle090(battle.BattleId) &&
                    !IsTowerBattle090(battle.BattleId))
                    return Result<CampaignState>.Success(campaign);
                if (battle.Phase != BattlePhase.Resolved ||
                    battle.Reward == null || !battle.Reward.Claimed ||
                    !campaign.Guild.Development.HasClaimedReward(
                        battle.Reward.RewardId) ||
                    !M2BattleCommandService.HasValidFinalStateHash090(battle))
                    return Result<CampaignState>.Failure(
                        "SSS090_CERTIFIED_CLAIMED_REWARD_REQUIRED");

                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var progression = state.Progression;
                var hunt = state.WeaponHunt;
                var deployed = DeployedSssHeroIds090(battle);
                var creditedSpawns = new HashSet<string>(StringComparer.Ordinal);

                for (var eventIndex = 0; eventIndex < battle.EventLog.Count;
                     eventIndex++)
                {
                    var battleEvent = battle.EventLog[eventIndex];
                    if (battleEvent == null ||
                        !StringComparer.Ordinal.Equals(
                            battleEvent.EventType, "DOWNED") ||
                        battleEvent.Side != BattleSide.Enemy)
                        continue;

                    var unionId = string.IsNullOrWhiteSpace(battleEvent.UnionId)
                        ? battleEvent.TargetUnionId
                        : battleEvent.UnionId;
                    var memberId = string.IsNullOrWhiteSpace(battleEvent.MemberId)
                        ? battleEvent.TargetMemberId
                        : battleEvent.MemberId;
                    if (string.IsNullOrWhiteSpace(unionId) ||
                        string.IsNullOrWhiteSpace(memberId))
                        continue;

                    var spawnId = ExactNumbers.Key(unionId, memberId);
                    if (!creditedSpawns.Add(spawnId)) continue;
                    if (!TryFindEnemy090(battle, unionId, memberId,
                            out var unionIndex, out var memberIndex, out var member))
                        return Result<CampaignState>.Failure(
                            "SSS090_DOWNED_ENEMY_IDENTITY_MISSING");

                    var enemyIdentity = CanonicalEnemyIdentity090(battle.BattleId,
                        unionIndex, memberIndex, member);
                    var fact = new ConfirmedEnemyDefeat
                    {
                        encounterInstanceId = battle.Reward.RewardId,
                        enemySpawnId = spawnId,
                        enemyDefinitionOrVariantId = enemyIdentity,
                        hostile = true,
                        rewardEligible = true,
                        friendlyOrUnrewardedSummon = false,
                        deployedHeroIds = deployed.ToList()
                    };

                    progression = SssProgression.Defeat(progression, fact,
                        SssTenV4FamilyResolver090.Instance).next;
                    hunt = SssWeaponHunt.Defeat(hunt, fact,
                        SssTenV4FamilyResolver090.Instance).next;
                }

                if (IsCampaignBattle090(battle.BattleId) &&
                    battle.Outcome == BattleOutcome.Victory)
                    progression = SssProgression.Victory(progression,
                        new VictoryFact
                        {
                            encounterInstanceId = battle.Reward.RewardId,
                            mode = "Campaign",
                            won = true,
                            rewardEligible = true,
                            completedCampaignStage = false
                        }).next;

                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(campaign,
                        state.With(progression: progression, weaponHunt: hunt)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_BATTLE_PROGRESSION_REJECTED:" + exception.Message);
            }
        }

        /// <summary>
        /// Records a completed Campaign stage/echo/cycle without inventing an
        /// additional battle win. Authored definition identity is the exact-once
        /// key, so replaying the same chapter/echo cannot farm world power. Any
        /// distinct definitions already present in the certified World Gate ledger
        /// are backfilled when an old save next completes an operation.
        /// </summary>
        public static Result<CampaignState> RecordCampaignCompletion090(
            CampaignState campaign,
            string operationCompletionKey)
        {
            try
            {
                if (campaign == null ||
                    string.IsNullOrWhiteSpace(operationCompletionKey))
                    return Result<CampaignState>.Failure(
                        "SSS090_CAMPAIGN_COMPLETION_REQUIRED");

                ExactNumbers.RequireId(operationCompletionKey);
                var state = SssTenV4CampaignAccessor090.Read(campaign);
                var progression = state.Progression.Copy();
                var completionKeys = new List<string>();
                var certifiedDefinitions = campaign.Guild?.GuildCity
                    ?.Strategic017H?.Campaign019?.Playable020?.WorldGate023
                    ?.CompletedDefinitionIds;
                if (certifiedDefinitions != null)
                    for (var index = 0; index < certifiedDefinitions.Count;
                         index++)
                        completionKeys.Add(CampaignCompletionKey090(
                            certifiedDefinitions[index]));
                completionKeys.Add(operationCompletionKey);

                var added = 0;
                foreach (var completionKey in completionKeys
                             .Distinct(StringComparer.Ordinal)
                             .OrderBy(value => value, StringComparer.Ordinal))
                {
                    if (progression.world.completedStageKeys.Contains(
                            completionKey))
                        continue;
                    progression.world.completedStageKeys.Add(completionKey);
                    added++;
                }
                if (added == 0)
                    return Result<CampaignState>.Success(campaign);
                progression.world.completedStageKeys.Sort(StringComparer.Ordinal);
                progression.world.uniqueCampaignStageCompletions =
                    ExactNumbers.Write(ExactNumbers.Read(
                        progression.world.uniqueCampaignStageCompletions) + added);
                progression.revision = ExactNumbers.Write(
                    ExactNumbers.Read(progression.revision) + 1);
                SssProgression.Validate(progression);
                return Result<CampaignState>.Success(
                    SssTenV4CampaignAccessor090.WithState(campaign,
                        state.With(progression: progression)));
            }
            catch (Exception exception)
            {
                return Result<CampaignState>.Failure(
                    "SSS090_CAMPAIGN_COMPLETION_REJECTED:" + exception.Message);
            }
        }

        public static string CampaignCompletionKey090(string definitionId)
        {
            ExactNumbers.RequireId(definitionId);
            return ExactNumbers.Key("CampaignCompletion090",
                definitionId);
        }

        public static SssTenV4State090 ApplyConfirmedDefeatForTests090(
            SssTenV4State090 state,
            string encounterInstanceId,
            string enemySpawnId,
            string enemyDefinitionOrVariantId,
            IEnumerable<string> deployedHeroIds)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var fact = new ConfirmedEnemyDefeat
            {
                encounterInstanceId = encounterInstanceId,
                enemySpawnId = enemySpawnId,
                enemyDefinitionOrVariantId = enemyDefinitionOrVariantId,
                hostile = true,
                rewardEligible = true,
                friendlyOrUnrewardedSummon = false,
                deployedHeroIds = (deployedHeroIds ?? Array.Empty<string>()).ToList()
            };
            var progression = SssProgression.Defeat(state.Progression, fact,
                SssTenV4FamilyResolver090.Instance).next;
            var hunt = SssWeaponHunt.Defeat(state.WeaponHunt, fact,
                SssTenV4FamilyResolver090.Instance).next;
            return state.With(progression: progression, weaponHunt: hunt);
        }

        private static IReadOnlyList<string> DeployedSssHeroIds090(
            BattleState battle)
        {
            return battle.PlayerUnions
                .Where(union => union != null)
                .SelectMany(union => union.Members ??
                                     Array.Empty<BattleMemberState>())
                .Where(member => member != null)
                .Select(member => SssHeroes.CanonicalId(member.MemberId))
                .Where(SssHeroes.IsSss)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }

        private static bool TryFindEnemy090(
            BattleState battle,
            string unionId,
            string memberId,
            out int unionIndex,
            out int memberIndex,
            out BattleMemberState member)
        {
            for (unionIndex = 0; unionIndex < battle.EnemyUnions.Count;
                 unionIndex++)
            {
                var union = battle.EnemyUnions[unionIndex];
                if (union == null ||
                    !StringComparer.Ordinal.Equals(union.UnionId, unionId))
                    continue;
                for (memberIndex = 0; memberIndex < union.Members.Count;
                     memberIndex++)
                {
                    member = union.Members[memberIndex];
                    if (member != null && StringComparer.Ordinal.Equals(
                            member.MemberId, memberId))
                        return true;
                }
            }

            unionIndex = -1;
            memberIndex = -1;
            member = null;
            return false;
        }

        private static string CanonicalEnemyIdentity090(
            string battleId,
            int unionIndex,
            int memberIndex,
            BattleMemberState member)
        {
            if (SssTenV4FamilyResolver090.Instance.TryResolve(member.ClassId,
                    out _))
                return member.ClassId.Trim().ToUpperInvariant();

            if (member.EquipmentTags != null)
                for (var tagIndex = 0; tagIndex < member.EquipmentTags.Count;
                     tagIndex++)
                    if (SssTenV4FamilyResolver090.Instance.TryResolve(
                            member.EquipmentTags[tagIndex], out _))
                        return member.EquipmentTags[tagIndex].Trim()
                            .ToUpperInvariant();

            EnemyArtIdentity090.Resolve090(battleId, unionIndex, memberIndex,
                member, out _, out var canonicalVariant, out _);
            if (!SssTenV4FamilyResolver090.Instance.TryResolve(canonicalVariant,
                    out _))
                throw new InvalidOperationException(
                    "Certified enemy does not resolve into the frozen 70-family catalog.");
            return canonicalVariant;
        }
    }
}
