using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.TitanTrials160
{
    [Serializable]
    public sealed class TitanBossAction161
    {
        public int Round { get; }
        public string Key { get; }
        public string Kind { get; }
        public string Channel { get; }
        public string Name { get; }
        public IReadOnlyList<string> Targets { get; }
        public IReadOnlyList<int> BudgetShares { get; }
        public string BasisHash { get; }
        public TitanBossWindow160 NextWindow { get; }
        public IReadOnlyList<string> PlayerActionOrder { get; }
        public int EnemySpeedBasisPoints { get; }
        public string Identity { get; }

        [JsonConstructor]
        public TitanBossAction161(int round, string key, string kind, string channel,
            IReadOnlyList<string> targets, IReadOnlyList<int> budgetShares, string basisHash,
            TitanBossWindow160 nextWindow, IReadOnlyList<string> playerActionOrder = null,int enemySpeedBasisPoints=10000,string name=null)
        {
            if (round < 1 || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(kind) ||
                targets == null || budgetShares == null || targets.Count != budgetShares.Count ||
                targets.Any(string.IsNullOrWhiteSpace) || targets.Distinct(StringComparer.Ordinal).Count() != targets.Count ||
                budgetShares.Any(x => x < 0) || string.IsNullOrWhiteSpace(basisHash) || nextWindow == null)
                throw new ArgumentException("TITAN161_ACTION_INVALID");
            Round = round; Key = key; Kind = kind; Channel = channel ?? "";
            Name=string.IsNullOrWhiteSpace(name)?key:name;
            Targets = Array.AsReadOnly(targets.ToArray());
            BudgetShares = Array.AsReadOnly(budgetShares.ToArray());
            BasisHash = basisHash; NextWindow = nextWindow;
            PlayerActionOrder = Array.AsReadOnly((playerActionOrder??Array.Empty<string>()).ToArray());
            if(enemySpeedBasisPoints<1||enemySpeedBasisPoints>10000)throw new ArgumentException("TITAN161_SPEED_INVALID");
            EnemySpeedBasisPoints=enemySpeedBasisPoints;
            if(PlayerActionOrder.Any(string.IsNullOrWhiteSpace)||PlayerActionOrder.Distinct(StringComparer.Ordinal).Count()!=PlayerActionOrder.Count)
                throw new ArgumentException("TITAN161_ACTION_ORDER_INVALID");
            Identity = CanonicalJson.Sha256Hex(new { Round, Key, Kind, Channel, Name, Targets, BudgetShares, BasisHash, NextWindow, PlayerActionOrder, EnemySpeedBasisPoints });
        }
        internal static TitanBossAction161 From(TitanActionCommitment160 action,IReadOnlyList<string> playerActionOrder,int enemySpeedBasisPoints=10000) =>
            new TitanBossAction161(action.Round, action.Action.Key, action.Action.Kind, action.Channel,
                action.TargetUnionIds, action.BudgetSharesBasisPoints, action.BattleBasisIdentity, action.NextWindow,playerActionOrder,enemySpeedBasisPoints,action.Action.PlayerCopy);
    }

    [Serializable]
    public sealed class TitanVenomTick161
    {
        public string TargetUnionId { get; }
        public int CleanDamage { get; }
        public int DueRound { get; }
        [JsonConstructor]
        public TitanVenomTick161(string targetUnionId, int cleanDamage, int dueRound)
        {
            if (string.IsNullOrWhiteSpace(targetUnionId) || cleanDamage < 0 || dueRound < 1)
                throw new ArgumentException("TITAN161_VENOM_INVALID");
            TargetUnionId = targetUnionId; CleanDamage = cleanDamage; DueRound = dueRound;
        }
    }

    [Serializable]
    public sealed class TitanSlow161
    {
        public string TargetUnionId { get; }
        public int ExpiresAfterRound { get; }
        [JsonConstructor]
        public TitanSlow161(string targetUnionId, int expiresAfterRound)
        {
            if (string.IsNullOrWhiteSpace(targetUnionId) || expiresAfterRound < 1)
                throw new ArgumentException("TITAN161_SLOW_INVALID");
            TargetUnionId = targetUnionId; ExpiresAfterRound = expiresAfterRound;
        }
    }

    [Serializable]
    public sealed class TitanBossRuntime161
    {
        public TitanAttempt160 Attempt { get; }
        public TitanBossAction161 Action { get; }
        public IReadOnlyList<TitanVenomTick161> Venom { get; }
        public IReadOnlyList<TitanSlow161> Slows { get; }
        public int BarrierRemaining { get; }
        public int BarrierExpiresAfterRound { get; }
        public int ExposedThroughRound { get; }
        public long HealingChannelDamage { get; }

        [JsonConstructor]
        public TitanBossRuntime161(TitanAttempt160 attempt, TitanBossAction161 action = null,
            IReadOnlyList<TitanVenomTick161> venom = null, IReadOnlyList<TitanSlow161> slows = null,
            int barrierRemaining = 0, int barrierExpiresAfterRound = 0, int exposedThroughRound = 0,
            long healingChannelDamage = 0)
        {
            Attempt = attempt ?? throw new ArgumentNullException(nameof(attempt));
            if (barrierRemaining < 0 || barrierExpiresAfterRound < 0 || exposedThroughRound < 0 || healingChannelDamage < 0 ||
                action != null && action.Round != attempt.BossWindow.LastResolvedRound + 1)
                throw new ArgumentException("TITAN161_RUNTIME_INVALID");
            Action = action;
            Venom = Array.AsReadOnly((venom ?? Array.Empty<TitanVenomTick161>()).ToArray());
            Slows = Array.AsReadOnly((slows ?? Array.Empty<TitanSlow161>()).ToArray());
            if (Venom.Any(x => x == null) || Slows.Any(x => x == null) ||
                Slows.Select(x => x.TargetUnionId).Distinct(StringComparer.Ordinal).Count() != Slows.Count)
                throw new ArgumentException("TITAN161_EFFECT_INVALID");
            BarrierRemaining = barrierRemaining; BarrierExpiresAfterRound = barrierExpiresAfterRound;
            ExposedThroughRound = exposedThroughRound; HealingChannelDamage = healingChannelDamage;
        }
    }
}
