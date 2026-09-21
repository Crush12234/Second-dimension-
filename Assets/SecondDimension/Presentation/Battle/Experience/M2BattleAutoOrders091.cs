using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Chooses only complete, already-generated Union Forecasts. This is a player
    /// input convenience, not an Art picker, resolver, or alternative combat engine.
    /// The coordinator revalidates every selection and remains the sole authority.
    /// </summary>
    public static class M2BattleAutoOrders091
    {
        public static bool HasLivingOpposition(M2BattleView battle) =>
            battle != null && !battle.IsResolved &&
            (battle.EnemyUnions ?? Array.Empty<M2BattleUnionView>()).Any(union =>
                union?.Members != null && union.Members.Any(member =>
                    member != null && !member.Downed && member.CurrentHp > 0));

        public static M2ForecastView Choose(M2BattleView battle, string unionId)
        {
            if (!HasLivingOpposition(battle)) return null;
            var union = battle.PlayerUnions?.FirstOrDefault(value =>
                value != null && value.UnionId == unionId && value.CanAct);
            if (union == null) return null;
            return (battle.Forecasts ?? Array.Empty<M2ForecastView>())
                .Where(forecast => forecast != null && forecast.UnionId == unionId &&
                    !string.IsNullOrWhiteSpace(forecast.ForecastId) && Affordable(union, forecast))
                .Select(forecast => new { Forecast = forecast, Score = Score(battle, union, forecast) })
                .OrderByDescending(choice => choice.Score.DeadlineDamage134)
                .ThenByDescending(choice => choice.Score.RescuePriority)
                .ThenByDescending(choice => choice.Score.Utility)
                .ThenBy(choice => choice.Forecast.SharedApCost)
                .ThenBy(choice => choice.Forecast.ForecastId, StringComparer.Ordinal)
                .Select(choice => choice.Forecast)
                .FirstOrDefault();
        }

        public static bool SelectCompletePlan(IM2PresentationCoordinator coordinator, out string failure)
        {
            return SelectCompletePlan(
                coordinator,
                M2BattleViewAccess098.Read(coordinator),
                out _,
                out failure);
        }

        public static bool SelectCompletePlan(
            IM2PresentationCoordinator coordinator,
            M2BattleView before,
            out M2BattleView ready,
            out string failure)
        {
            ready = null;
            failure = string.Empty;
            if (!HasLivingOpposition(before)) return false;
            var active = (before.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
                .Where(union => union != null && union.CanAct).ToArray();
            if (active.Length == 0) return false;
            // Do not partially automate a round if an active Union has no legal order.
            if (active.Any(union => Choose(before, union.UnionId) == null))
            {
                failure = "A Union needs your command.";
                return false;
            }

            var selections = active.Select(union =>
            {
                var choice = Choose(before, union.UnionId);
                return new M2AutoForecastSelection108
                {
                    UnionId = union.UnionId,
                    ForecastId = choice.ForecastId
                };
            }).ToArray();

            if (coordinator is IM2BattleAutoPlanCoordinator108 batch)
            {
                var result = batch.SelectAutoForecastPlan108(selections);
                if (!result.Succeeded) { failure = result.Message; return false; }
            }
            else
            {
                foreach (var selection in selections)
                {
                    var result = coordinator.SelectForecast(selection.UnionId, selection.ForecastId);
                    if (!result.Succeeded) { failure = result.Message; return false; }
                }
            }
            var verified = M2BattleViewAccess098.Read(coordinator);
            ready = verified;
            return verified != null && verified.BattleId == before.BattleId && verified.Round == before.Round &&
                HasLivingOpposition(verified) && verified.CanConfirmRound &&
                active.All(union => verified.PlayerUnions.Any(value => value.UnionId == union.UnionId &&
                    !string.IsNullOrWhiteSpace(value.SelectedForecastId)));
        }

        public static bool SelectAndResolveCompletePlan108(
            IM2PresentationCoordinator coordinator,
            M2BattleView before,
            out M2BattleView committed,
            out M2BattleView resolved,
            out string failure)
        {
            committed = null;
            resolved = null;
            failure = string.Empty;
            if (!HasLivingOpposition(before)) return false;
            var active = (before.PlayerUnions ?? Array.Empty<M2BattleUnionView>())
                .Where(union => union != null && union.CanAct).ToArray();
            if (active.Length == 0) return false;
            var selections = active.Select(union => new
            {
                Union = union,
                Choice = Choose(before, union.UnionId)
            }).ToArray();
            if (selections.Any(value => value.Choice == null))
            {
                failure = "A Union needs your command.";
                return false;
            }
            var plan = selections.Select(value => new M2AutoForecastSelection108
            {
                UnionId = value.Union.UnionId,
                ForecastId = value.Choice.ForecastId
            }).ToArray();

            if (coordinator is IM2BattleAutoRoundCoordinator108 automaticRound)
            {
                var result = automaticRound.ResolveAutoForecastPlan108(plan);
                if (!result.Succeeded) { failure = result.Message; return false; }
                ApplySelectionsForPresentation108(before, plan);
                committed = before;
            }
            else
            {
                if (!SelectCompletePlan(coordinator, before, out committed, out failure)) return false;
                var result = coordinator.ConfirmBattleRound();
                if (!result.Succeeded) { failure = result.Message; return false; }
            }

            resolved = M2BattleViewAccess098.Read(coordinator);
            return resolved != null && resolved.BattleId == before.BattleId &&
                (resolved.Round > before.Round || resolved.IsResolved);
        }

        private static void ApplySelectionsForPresentation108(
            M2BattleView battle,
            IReadOnlyList<M2AutoForecastSelection108> selections)
        {
            if (battle == null || selections == null) return;
            foreach (var selection in selections)
            {
                var union = battle.PlayerUnions?.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.UnionId, selection.UnionId));
                if (union != null)
                {
                    union.IsSelected = true;
                    union.SelectedForecastId = selection.ForecastId;
                }
                foreach (var forecast in battle.Forecasts ?? Array.Empty<M2ForecastView>())
                    if (forecast != null && StringComparer.Ordinal.Equals(forecast.UnionId, selection.UnionId))
                        forecast.IsSelected = StringComparer.Ordinal.Equals(
                            forecast.ForecastId, selection.ForecastId);
            }
        }

        private static bool Affordable(M2BattleUnionView union, M2ForecastView forecast)
        {
            if (forecast.SharedApCost < 0 || forecast.SharedApCost > union.CurrentAp) return false;
            var actions = forecast.MemberActions ?? Array.Empty<M2PredictedActionView>();
            foreach (var costs in actions.Where(action => action != null && action.PersonalMpCost > 0)
                .GroupBy(action => action.ActorMemberId))
            {
                var member = union.Members?.FirstOrDefault(value => value != null && value.MemberId == costs.Key);
                if (member == null || member.Downed || costs.Sum(action => action.PersonalMpCost) > member.CurrentMp)
                    return false;
            }
            return true;
        }

        private sealed class ForecastScore107
        {
            public long DeadlineDamage134;
            public int RescuePriority;
            public long Utility;
        }

        private static ForecastScore107 Score(M2BattleView battle, M2BattleUnionView acting, M2ForecastView forecast)
        {
            // Basic attacks are an efficient option, not a permanent +100 winner
            // over every equally sized learned-Art Union command.
            long score = 100;
            long effectiveDamage = 0;
            long training = 0;
            var rescuePriority = 0;
            var remaining = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var enemyUnion in battle.EnemyUnions ?? Array.Empty<M2BattleUnionView>())
                foreach (var enemy in enemyUnion?.Members ?? Array.Empty<M2BattleMemberView>())
                    if (enemy != null && !enemy.Downed && enemy.CurrentHp > 0)
                        remaining[DamageKey097(enemyUnion.UnionId, enemy.MemberId)] = enemy.CurrentHp;
            var ownCritical = (acting.Members ?? Array.Empty<M2BattleMemberView>()).Any(member =>
                member != null && !member.Downed && member.CurrentHp * 100L <= member.MaximumHp * 30L);
            foreach (var action in forecast.MemberActions ?? Array.Empty<M2PredictedActionView>())
            {
                if (action == null) continue;
                var actor = acting.Members?.FirstOrDefault(member => member != null && member.MemberId == action.ActorMemberId);
                if (actor == null || actor.Downed || actor.CurrentHp <= 0) continue;
                var target = battle.PlayerUnions?.FirstOrDefault(union => union != null &&
                    union.UnionId == action.TargetUnionId);
                var patients = target?.Members ?? Array.Empty<M2BattleMemberView>();
                var patient = patients.FirstOrDefault(member => member != null &&
                    member.MemberId == action.TargetMemberId);
                // IsRevival091 is projected from the exact learned Art's authority,
                // never inferred from a label, portrait, role, or generic heal.
                if (action.IsRevival091 && patient != null && patient.Downed)
                {
                    rescuePriority = Math.Max(rescuePriority, 4);
                    score += Math.Max(0L, action.PredictedHpDelta097) * 8L;
                }
                else if (string.Equals(action.ActionKind, "Restoration", StringComparison.OrdinalIgnoreCase))
                {
                    // The authority already clips single/group healing to missing
                    // HP and projects its primary patient. Do not turn a cleanse,
                    // protection Art or unrelated wounded member into an HP rescue.
                    var healing = Math.Min(Math.Max(0L, action.PredictedHpDelta097),
                        patients.Where(member => member != null && !member.Downed)
                            .Sum(member => Math.Max(0L, (long)member.MaximumHp - member.CurrentHp)));
                    if (healing > 0 && patient != null && !patient.Downed)
                    {
                        if (patient.CurrentHp * 100L <= patient.MaximumHp * 30L)
                            rescuePriority = Math.Max(rescuePriority, 3);
                        else if (patient.CurrentHp * 100L <= patient.MaximumHp * 65L)
                            rescuePriority = Math.Max(rescuePriority, 2);
                        score += 50 + healing * 8L;
                    }
                    else if (!action.IsRevival091 && patient != null && patient.Downed && !patient.Stabilized)
                        rescuePriority = Math.Max(rescuePriority, 1);
                    else score += 50;
                }
                else if (action.ActionKind == "Martial" || action.ActionKind == "Mystic" || action.ActionKind == "Tactical")
                {
                    var usefulDamage = ConsumeEffectiveDamage097(action, remaining);
                    score += usefulDamage > 0 ? 600 : action.ActionKind == "Tactical" ? 250 : 0;
                    effectiveDamage += usefulDamage;
                    // Growth only matters when this planned attack can actually
                    // hurt a living recipient. Existing Art selection owns which
                    // older/newer learned Art each whole-Union command contains.
                    if (usefulDamage > 0)
                    {
                        if (usefulDamage >= PredictedDamage097(action) && action.PredictedGrowth > 0 && !string.IsNullOrWhiteSpace(action.ArtId) &&
                            !action.ArtId.StartsWith("ART_BASIC_", StringComparison.Ordinal) &&
                            action.ArtId != "ART_ASSIST_ALLY") training += 40;
                        if (action.BreakthroughOpportunity) training += 120;
                    }
                }
                else if (action.ActionKind == "Guard") score += ownCritical ? 2000 : 15;
                else if (action.ActionKind == "Recovery") score += acting.CurrentAp <= 2 ? 1500 : 20;
            }
            // Rescue priority is compared separately so useful damage need not
            // flatten at 500 HP. Powerful legal Mystic/area Arts can beat a weaker
            // basic attack at every scale, while equal overkill conserves AP/MP.
            return new ForecastScore107
            {
                // The authoritative clock says this is the last playable round.
                // Healing/guarding cannot extend it. Prefer useful legal damage
                // now; normal revival/triage ordering remains unchanged otherwise.
                DeadlineDamage134 = LastBreachRound134(battle) ? effectiveDamage : 0,
                RescuePriority = rescuePriority,
                Utility = score + effectiveDamage * 8L + Math.Min(720L, training) -
                    forecast.SharedApCost * 12L - (forecast.MemberActions ?? Array.Empty<M2PredictedActionView>())
                        .Where(action => action != null).Sum(action => Math.Max(0L, action.PersonalMpCost)) * 8L
            };
        }

        private static bool LastBreachRound134(M2BattleView battle) =>
            battle != null && battle.Round > 1 &&
            (battle.LastResolvedRoundEvents ?? Array.Empty<M2BattleEventView>())
                .Concat(battle.Events ?? Array.Empty<M2BattleEventView>())
                .Any(value => value != null && value.EventType == "BOSS_ADVANCE_CLOCK" &&
                    value.Round == battle.Round - 1 && value.Amount == 1);

        private static string DamageKey097(string unionId, string memberId) =>
            (unionId ?? string.Empty) + "\n" + (memberId ?? string.Empty);

        private static long PredictedDamage097(M2PredictedActionView action) =>
            action.DamageRecipients097 != null && action.DamageRecipients097.Count > 0
                ? action.DamageRecipients097.Where(value => value != null).Sum(value => Math.Max(0L, value.PredictedHpLoss))
                : Math.Max(0L, -(long)action.PredictedHpDelta097);

        private static long ConsumeEffectiveDamage097(M2PredictedActionView action, IDictionary<string, long> remaining)
        {
            if (action.DamageRecipients097 != null && action.DamageRecipients097.Count > 0)
            {
                long total = 0;
                foreach (var recipient in action.DamageRecipients097)
                    if (recipient != null) total += ConsumeRecipient097(remaining,
                        recipient.UnionId, recipient.MemberId, Math.Max(0L, recipient.PredictedHpLoss));
                return total; // The committed AoE total is never also added as a scalar hit.
            }
            return ConsumeRecipient097(remaining, action.TargetUnionId, action.TargetMemberId,
                Math.Max(0L, -(long)action.PredictedHpDelta097));
        }

        private static long ConsumeRecipient097(IDictionary<string, long> remaining,
            string unionId, string memberId, long predictedLoss)
        {
            var key = DamageKey097(unionId, memberId);
            if (!remaining.TryGetValue(key, out var hp) || hp <= 0 || predictedLoss <= 0) return 0;
            var effective = Math.Min(hp, predictedLoss);
            remaining[key] = hp - effective;
            return effective;
        }
    }
}
