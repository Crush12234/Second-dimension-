using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.M2
{
    [Serializable]
    public sealed class BattleAreaRecipient095
    {
        [JsonConstructor]
        public BattleAreaRecipient095(string unionId, string unionName, string memberId,
            string memberName, int damageBudget, int predictedHpLoss, bool guarded)
        {
            UnionId = unionId ?? string.Empty;
            UnionName = unionName ?? string.Empty;
            MemberId = memberId ?? string.Empty;
            MemberName = memberName ?? string.Empty;
            DamageBudget = Math.Max(0, damageBudget);
            PredictedHpLoss = Math.Max(0, predictedHpLoss);
            Guarded = guarded;
        }
        public string UnionId { get; }
        public string UnionName { get; }
        public string MemberId { get; }
        public string MemberName { get; }
        public int DamageBudget { get; }
        public int PredictedHpLoss { get; }
        public bool Guarded { get; }
    }

    /// <summary>Only new Forecasts carry this plan; null on old actions is omitted from JSON.</summary>
    [Serializable]
    public sealed class BattleAreaActionPlan095
    {
        [JsonConstructor]
        public BattleAreaActionPlan095(string artId, string scope, int maximumTargets,
            int totalDamageBudget, int totalCohesionBudget, int totalFormationBudget,
            IReadOnlyList<BattleAreaRecipient095> recipients)
        {
            if (maximumTargets < 1 || maximumTargets > 5)
                throw new ArgumentOutOfRangeException(nameof(maximumTargets));
            ArtId = artId ?? string.Empty;
            Scope = scope ?? string.Empty;
            MaximumTargets = maximumTargets;
            TotalDamageBudget = Math.Max(0, totalDamageBudget);
            TotalCohesionBudget = Math.Max(0, totalCohesionBudget);
            TotalFormationBudget = Math.Max(0, totalFormationBudget);
            var copy = recipients == null ? Array.Empty<BattleAreaRecipient095>() : recipients.ToArray();
            if (copy.Length > maximumTargets || copy.Any(value => value == null) ||
                copy.Select(value => value.MemberId).Distinct(StringComparer.Ordinal).Count() != copy.Length ||
                copy.Sum(value => (long)value.DamageBudget) > TotalDamageBudget ||
                copy.Any(value => value.PredictedHpLoss > value.DamageBudget))
                throw new ArgumentException("M2_AREA095_RECIPIENT_BUDGET_INVALID");
            Recipients = Array.AsReadOnly(copy);
        }
        public string ArtId { get; }
        public string Scope { get; }
        public int MaximumTargets { get; }
        public int TotalDamageBudget { get; }
        public int TotalCohesionBudget { get; }
        public int TotalFormationBudget { get; }
        public IReadOnlyList<BattleAreaRecipient095> Recipients { get; }
        [JsonIgnore] public int PredictedHpLoss => Recipients.Sum(value => value.PredictedHpLoss);
    }
}


