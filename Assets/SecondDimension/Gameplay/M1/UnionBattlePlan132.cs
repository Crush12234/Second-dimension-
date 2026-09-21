using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M1
{
    // A next-battle plan never rewrites an expedition's committed roster or IDs.
    // The nullable root field preserves the canonical bytes of older saves.
    [Serializable]
    public sealed class UnionBattlePlan132
    {
        [JsonConstructor]
        public UnionBattlePlan132(string sourceRosterHash, IReadOnlyList<UnionState> unions)
        {
            if (string.IsNullOrWhiteSpace(sourceRosterHash) || sourceRosterHash.Length != 64)
                throw new ArgumentException("The committed roster hash is required.");
            SourceRosterHash = sourceRosterHash;
            Unions = new List<UnionState>(unions ?? throw new ArgumentNullException(nameof(unions))).AsReadOnly();
        }

        public string SourceRosterHash { get; }
        public IReadOnlyList<UnionState> Unions { get; }

        static bool HasCanonicalLeader132(UnionState plan)
        {
            if (plan.MemberRecruitIds.Count == 0)
                return string.IsNullOrEmpty(plan.LeaderRecruitId);
            // The existing Union authority preserves an SSS leader regardless
            // of slot order. Validate that authority rather than replacing it
            // with a stricter first-slot rule that rejects legitimate saves.
            return StringComparer.Ordinal.Equals(plan.LeaderRecruitId,
                SecondDimension.SSS.V3.CovenantLeadership.Resolve(
                    new List<string>(plan.MemberRecruitIds), plan.LeaderRecruitId));
        }

        public void Validate(GuildState guild)
        {
            if (guild == null || !StringComparer.Ordinal.Equals(SourceRosterHash, CanonicalJson.Sha256Hex(guild.Unions)) ||
                Unions.Count < 2 || Unions.Count > 10)
                throw new InvalidOperationException("The saved next-battle plan does not match its committed Guild roster.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < Unions.Count; i++)
            {
                var plan = Unions[i];
                var original = plan == null ? null : guild.Unions.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, plan.UnionId));
                // A draft can grow without rewriting the roster committed to an
                // adventure. New IDs use the same bounded native AddUnion IDs.
                var nativeNewId = plan != null && Enumerable.Range(1,10).Any(ordinal =>
                    plan.UnionId == "UNION_OPENING_" + ordinal.ToString("00"));
                if (plan == null || !unionIds.Add(plan.UnionId) ||
                    (original == null ? !nativeNewId || plan.Kind != UnionKind.Normal : plan.Kind != original.Kind) ||
                    plan.MemberRecruitIds.Count > NormalUnionPlanRules.MaximumMembersPerUnion ||
                    !OpeningUnionCatalog.IsFormation(plan.FormationId) || !OpeningUnionCatalog.IsDoctrine(plan.DoctrineId) ||
                    !HasCanonicalLeader132(plan))
                    throw new InvalidOperationException("The next-battle plan has an invalid Union or slot.");
                foreach (var id in plan.MemberRecruitIds)
                {
                    var recruit = guild.Recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, id));
                    if (!seen.Add(id) || recruit == null || !ProtectedActorPolicy.CanEnterNormalUnion(recruit) ||
                        GuildMemberDeploymentPolicy017D.IsTraining(guild.GuildCity, id))
                        throw new InvalidOperationException("A next-battle member is duplicated, unavailable, or not owned.");
                }
            }
        }
    }

    public static class UnionBattlePlanRules132
    {
        public static IReadOnlyList<UnionState> Read(CampaignState state) =>
            state?.NextBattleUnions132?.Unions ?? state?.Guild?.Unions ?? Array.Empty<UnionState>();

        public static bool HasCommittedRoster(CampaignState state)
        {
            var progress = state?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            var playable = progress?.Playable020;
            return state?.Guild?.GuildCity?.Expedition?.Status == ExpeditionStatus017D.Active ||
                   state?.Guild?.GuildCity?.Expedition?.Status == ExpeditionStatus017D.AwaitingBattle ||
                   SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParkedRoster(state) || state?.Recovery150?.Paused == true || progress?.ActiveOperation != null || playable?.ActiveOperation != null ||
                   playable?.WorldGate023?.ActiveOperation != null || playable?.Progression022?.ActiveAbyssOperation != null;
        }

        public static GuildState ProjectGuild(CampaignState state) => state.NextBattleUnions132 == null
            ? state.Guild : state.Guild.With(state.Guild.TreasuryXp, state.Guild.Recruits, Read(state), state.Guild.Inventory);

        public static bool CanEdit(CampaignState state, out string reason)
        {
            reason = null;
            var city = state?.Guild?.GuildCity;
            if (city == null) { reason = "The Guild is unavailable."; return false; }
            var battle = state.Battle;
            if (battle != null && (battle.Outcome == BattleOutcome.InProgress || battle.Reward != null && !battle.Reward.Claimed))
            { reason = "Finish the battle and claim its rewards before changing Unions."; return false; }
            var progress = city.Strategic017H?.Campaign019;
            var playable = progress?.Playable020;
            var world = playable?.WorldGate023?.ActiveOperation;
            var tower = playable?.Progression022?.ActiveAbyssOperation;
            if (city.PendingEncounter != null || city.PendingBattleReturn != null || progress?.PendingReceipt != null ||
                playable?.ActiveOperation?.PendingReceipt != null || world?.PendingReceipt != null ||
                world?.ExpeditionDeck089?.PendingReceipt != null || tower?.PendingReceipt != null ||
                city.Expedition?.PendingQuestFate165 != null)
            { reason = "Finish the saved card or battle return before changing Unions."; return false; }
            if (GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(state) &&
                (city.Strategic017H?.ActiveDefense != null ||
                 state.Recovery150?.Paused != true && progress?.ActiveOperation == null && playable?.ActiveOperation == null && world == null && tower == null &&
                 city.Expedition?.Status != ExpeditionStatus017D.Active))
            { reason = "Return from this adventure before changing Unions."; return false; }
            return true;
        }

        public static CampaignState PromoteWhenIdle(CampaignState state)
        {
            if (state?.NextBattleUnions132 == null || SecondDimension.Gameplay.Navigation164.LoopCheckpoint164.HasParkedRoster(state) || GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(state)) return state;
            var planned = state.NextBattleUnions132.Unions;
            // Clear before replacing the Guild: the constructor validates the old
            // plan against its original roster on every immutable copy/reload.
            return state.WithNextBattleUnions132(null).With(state.Guild.With(
                state.Guild.TreasuryXp, state.Guild.Recruits, planned, state.Guild.Inventory), state.OpeningFlow);
        }
    }
}
