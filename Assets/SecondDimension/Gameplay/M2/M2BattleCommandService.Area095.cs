using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2BattleCommandService
    {
        private static BattleAreaActionPlan095 BuildNewAreaPlan095(BattleState battle,
            M2CombatContent content, BattleMemberState actor, BattleUnionState source,
            M2ArtDefinition art, BattleActionKind kind, string commandId,
            BattleUnionState target, BattleMemberState primary,
            IReadOnlyList<BattleUnionState> opponents)
        {
            var profile = art?.AreaProfile095;
            if (profile == null || !IsOffensiveAction088(kind) || target == null ||
                !art.IsForecastAction || !actor.LearnedArtIds.Contains(art.Id) ||
                !IsEquipmentLegal(art, actor.EquipmentTags) || !AllowsEquivalentOffensiveRetarget088(art))
                return null;
            // Use the existing base/mode/formation grammar, replacing (not multiplying)
            // generic node power with the reviewed authored TOTAL-action coefficient.
            var coefficient = profile.HasAuthoredBudget
                ? profile.DamageCoefficientPermille : art.PowerCoefficientPermille;
            var damage = -PredictedHpDelta(actor, primary, kind, commandId,
                source.FormationBenefitActive, art,
                EffectiveArtPowerPermille088(battle, content, actor, art.Id), coefficient);
            // No secondary unit formula is authored. Explicit conservative adapter:
            // scale the existing pulse once, then distribute over affected Unions.
            var cohesion = CeilingPulse095(kind == BattleActionKind.Tactical ? 8 : 3,
                profile.CohesionCoefficientPermille);
            var formation = CeilingPulse095(kind == BattleActionKind.Tactical ? 700 : 300,
                profile.FormationCoefficientPermille);
            return BuildAreaRecipients095(art.Id, profile.Scope, profile.MaximumTargets,
                damage, cohesion, formation, target.UnionId, primary?.MemberId,
                opponents, commandId);
        }

        private static int CeilingPulse095(int pulse, int coefficient) =>
            checked((pulse * coefficient + 999) / 1000);

        private static BattleAreaActionPlan095 BuildAreaRecipients095(string artId,
            string scope, int maximumTargets, int damage, int cohesion, int formation,
            string primaryUnionId, string primaryMemberId,
            IReadOnlyList<BattleUnionState> opponents, string commandId)
        {
            var selectedIndex = FindUnionIndex(opponents, primaryUnionId);
            if (selectedIndex < 0 || !IsActive(opponents[selectedIndex]))
                selectedIndex = NextLivingEnemyUnionIndex088(opponents, selectedIndex, commandId);
            var selected = new List<Tuple<BattleUnionState, BattleMemberState>>();
            if (selectedIndex >= 0)
            {
                var indices = new List<int> { selectedIndex };
                if (scope == M2AreaArtProfile095.CrossUnion)
                {
                    var additional = Enumerable.Range(0, opponents.Count)
                        .Where(index => index != selectedIndex && IsActive(opponents[index])).ToList();
                    additional.Sort((left, right) => CompareRetargetCandidates088(
                        opponents[left], left, opponents[right], right, selectedIndex, commandId));
                    indices.AddRange(additional);
                }
                var living = indices.Select(index =>
                {
                    var union = opponents[index];
                    var members = union.Members.Where(member => !member.Downed).ToList();
                    var primaryIndex = members.FindIndex(member => member.MemberId == primaryMemberId);
                    if (index == selectedIndex && primaryIndex > 0)
                    {
                        var primary = members[primaryIndex];
                        members.RemoveAt(primaryIndex);
                        members.Insert(0, primary);
                    }
                    return Tuple.Create(union, members);
                }).ToArray();
                // For cross-Union Arts take one living member per ordered Union
                // before a second member, with a TOTAL cap rather than cap per Union.
                for (var memberIndex = 0; selected.Count < maximumTargets; memberIndex++)
                {
                    var any = false;
                    foreach (var row in living)
                    {
                        if (memberIndex >= row.Item2.Count) continue;
                        any = true;
                        selected.Add(Tuple.Create(row.Item1, row.Item2[memberIndex]));
                        if (selected.Count == maximumTargets) break;
                    }
                    if (!any) break;
                }
            }
            var recipients = new List<BattleAreaRecipient095>();
            for (var index = 0; index < selected.Count; index++)
            {
                var union = selected[index].Item1;
                var member = selected[index].Item2;
                var share = ShareAreaBudget095(damage, selected.Count, index);
                var guarded = union.Guarding || member.Guarding;
                // A zero allocation MUST remain zero, including under Guard.
                var loss = share <= 0 ? 0 : guarded ? Math.Max(1, share / 2) : share;
                recipients.Add(new BattleAreaRecipient095(union.UnionId, union.DisplayName,
                    member.MemberId, member.DisplayName, share, Math.Min(member.CurrentHp, loss), guarded));
            }
            return new BattleAreaActionPlan095(artId, scope, maximumTargets, damage,
                cohesion, formation, recipients.AsReadOnly());
        }

        private static int ShareAreaBudget095(int total, int count, int index) =>
            count <= 0 ? 0 : total / count + (index < total % count ? 1 : 0);

        private static string AreaPrediction095(BattleAreaActionPlan095 plan) =>
            plan.Recipients.Count + " targets " +
            (plan.Scope == M2AreaArtProfile095.CrossUnion ? "across enemy Unions" : "in the selected Union") +
            ": " + string.Join("; ", plan.Recipients.Select(value => value.UnionName + " / " +
                value.MemberName + " −" + value.PredictedHpLoss + " HP" + (value.Guarded ? " (Guard)" : ""))) +
            ". Total up to " + plan.PredictedHpLoss + " HP (" + plan.TotalDamageBudget +
            " before Guard/remaining HP); one Art cost, one growth award.";

        private static BattlePlannedActionState WithAreaPlan095(BattlePlannedActionState action,
            BattleAreaActionPlan095 plan)
        {
            var primary = plan.Recipients.FirstOrDefault();
            return new BattlePlannedActionState(action.ActorMemberId, action.ActorName,
                primary?.UnionId ?? action.TargetUnionId, primary?.MemberId ?? action.TargetMemberId,
                action.ArtId, action.ArtName, action.Kind, action.SharedApCost, action.PersonalMpCost,
                -plan.PredictedHpLoss, -plan.TotalCohesionBudget, -plan.TotalFormationBudget,
                AreaPrediction095(plan), plan.PredictedHpLoss > 0, action.BreakthroughOpportunity,
                action.AnimationTag, action.Discipline, action.PredictedGrowth,
                action.BreakthroughTargetArtId, action.BreakthroughTargetArtName, plan);
        }

        private static bool TryPrepareAreaAction095(BattleState battle, M2CombatContent content,
            BattleForecastState forecast, BattleUnionState source, BattleMemberState actor,
            BattlePlannedActionState action, IReadOnlyList<BattleUnionState> opponents,
            List<BattleEventState> events, out BattlePlannedActionState prepared, out bool victoryStop)
        {
            prepared = action;
            victoryStop = AllDefeated(opponents);
            if (victoryStop)
            {
                AddVictorySequenceStop088(battle.Round, source, action, events);
                return false;
            }
            var old = action.AreaActionPlan095;
            if (!content.Arts.TryGetValue(action.ArtId, out var art) || art.AreaProfile095 == null ||
                !art.IsForecastAction || !actor.LearnedArtIds.Contains(art.Id) ||
                !IsEquipmentLegal(art, actor.EquipmentTags) || actor.CurrentMp < art.PersonalMpCost ||
                action.SharedApCost < art.SharedApCost || action.PersonalMpCost < art.PersonalMpCost ||
                !AllowsEquivalentOffensiveRetarget088(art) || old.ArtId != art.Id ||
                old.Scope != art.AreaProfile095.Scope || old.MaximumTargets != art.AreaProfile095.MaximumTargets)
            {
                AddDeadTargetCancellation088(battle.Round, source, action, events);
                return false;
            }
            // Retain committed totals. Only equivalent living recipients/Guard are
            // reprojected; no resource or power reroll is permitted during resolution.
            var plan = BuildAreaRecipients095(art.Id, old.Scope, old.MaximumTargets,
                old.TotalDamageBudget, old.TotalCohesionBudget, old.TotalFormationBudget,
                action.TargetUnionId, action.TargetMemberId, opponents, forecast.CommandId);
            if (plan.Recipients.Count == 0 || plan.PredictedHpLoss == 0)
            {
                AddDeadTargetCancellation088(battle.Round, source, action, events);
                return false;
            }
            prepared = WithAreaPlan095(action, plan);
            if (!old.Recipients.Select(value => value.MemberId)
                .SequenceEqual(plan.Recipients.Select(value => value.MemberId)))
                events.Add(Event(events.Count, battle.Round, "AREA_RETARGETED", source.Side,
                    source.UnionId, actor.MemberId, art.Id,
                    actor.DisplayName + " redirects the existing area budget to legal living targets. " +
                    AreaPrediction095(plan), 0, source.UnionId, actor.MemberId,
                    prepared.TargetUnionId, prepared.TargetMemberId));
            return true;
        }

        private static int ResolveAreaAttack095(int round, BattlePlannedActionState action,
            List<BattleUnionState> allies, List<BattleUnionState> opponents,
            List<BattleEventState> events, BattleSide actingSide)
        {
            var plan = action.AreaActionPlan095;
            if (plan == null) return 0;
            var affected = plan.Recipients.Where(value => value.PredictedHpLoss > 0)
                .Select(value => value.UnionId).Distinct(StringComparer.Ordinal).ToArray();
            var appliedPulse = new HashSet<string>(StringComparer.Ordinal);
            var useful = 0;
            foreach (var recipient in plan.Recipients)
            {
                if (recipient.DamageBudget <= 0 || recipient.PredictedHpLoss <= 0) continue;
                var unionIndex = Array.IndexOf(affected, recipient.UnionId);
                var firstInUnion = appliedPulse.Add(recipient.UnionId);
                var cohesion = firstInUnion ? ShareAreaBudget095(plan.TotalCohesionBudget, affected.Length, unionIndex) : 0;
                var formation = firstInUnion ? ShareAreaBudget095(plan.TotalFormationBudget, affected.Length, unionIndex) : 0;
                var hit = RetargetSssAreaAction090(action, recipient.UnionId,
                    recipient.MemberId, -recipient.PredictedHpLoss, -cohesion, -formation,
                    "One recipient of the existing area Art budget.");
                // Same mutation/HP/Downed authority as ordinary attacks. Exact zero
                // secondary budgets bypass the single-hit minimums, not legality.
                useful = checked(useful + ResolveAttack(round, hit, allies, opponents, events,
                    actingSide, recipient.PredictedHpLoss, cohesion, formation));
            }
            return useful;
        }

        private static bool TryResolveEnemyArea095(BattleState battle, M2CombatContent content,
            List<BattleUnionState> enemies, int sourceIndex, int actorIndex,
            List<BattleUnionState> players, BattleUnionState target, BattleMemberState primary,
            M2ArtDefinition art, List<BattleEventState> events)
        {
            if (art?.AreaProfile095 == null || AllDefeated(players)) return false;
            var source = enemies[sourceIndex];
            var actor = source.Members[actorIndex];
            if (actor.Downed || !actor.LearnedArtIds.Contains(art.Id) || !art.IsForecastAction ||
                source.CurrentAp < art.SharedApCost || actor.CurrentMp < art.PersonalMpCost ||
                !IsEquipmentLegal(art, actor.EquipmentTags)) return false;
            var kind = ActionKind("CMD_BALANCED", art, battle);
            var plan = BuildNewAreaPlan095(battle, content, actor, source, art, kind,
                "CMD_BALANCED", target, primary, players);
            if (plan == null || plan.PredictedHpLoss <= 0) return false;
            var action = new BattlePlannedActionState(actor.MemberId, actor.DisplayName,
                target.UnionId, primary.MemberId, art.Id, art.Name, kind,
                art.SharedApCost, art.PersonalMpCost, -plan.PredictedHpLoss,
                -plan.TotalCohesionBudget, -plan.TotalFormationBudget, AreaPrediction095(plan),
                true, false, art.AnimationTag, art.Discipline, areaActionPlan095: plan);
            var members = source.Members.ToArray();
            members[actorIndex] = actor.With(currentMp: actor.CurrentMp - art.PersonalMpCost, guarding: false);
            enemies[sourceIndex] = source.With(members: members, currentAp: source.CurrentAp - art.SharedApCost);
            events.Add(Event(events.Count, battle.Round, "ENEMY_AREA_FORECAST", BattleSide.Enemy,
                source.UnionId, actor.MemberId, art.Id, source.DisplayName + " commits " + art.Name +
                ": " + AreaPrediction095(plan), art.SharedApCost,
                source.UnionId, actor.MemberId, action.TargetUnionId, action.TargetMemberId));
            var useful = ResolveAreaAttack095(battle.Round, action, enemies, players, events, BattleSide.Enemy);
            if (useful > 0)
            {
                source = enemies[sourceIndex];
                members = source.Members.ToArray();
                actor = members[actorIndex];
                var gain = M2MeaningfulUse.PersonalProgressGain(kind, true, -useful);
                members[actorIndex] = actor.With(artProgress: AddMeaningfulArtUse(
                    actor.ArtProgress, art.Id, art.Discipline, gain));
                enemies[sourceIndex] = source.With(members: members);
                events.Add(Event(events.Count, battle.Round, "ART_GROWTH", BattleSide.Enemy,
                    source.UnionId, actor.MemberId, art.Id,
                    actor.DisplayName + " grows " + art.Name + " once through meaningful area use.", gain,
                    source.UnionId, actor.MemberId, source.UnionId, actor.MemberId));
            }
            return true;
        }
    }
}

