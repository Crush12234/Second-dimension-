using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2BattleCommandService
    {
        private static readonly ConditionalWeakTable<List<BattleEventState>, M2BattlePolicy163> ProgressionRounds163 =
            new ConditionalWeakTable<List<BattleEventState>, M2BattlePolicy163>();
        private static void BeginProgressionRound163(BattleState battle, List<BattleEventState> events)
        {
            if (battle.Progression163 != null) ProgressionRounds163.Add(events, battle.Progression163);
        }
        private static int AdjustDefenseDamage163(string targetMemberId, BattleActionKind kind, int damage,
            List<BattleEventState> events) =>
            ProgressionRounds163.TryGetValue(events, out var policy) ? policy.PhysicalDamage(targetMemberId, kind, damage) : damage;
        private static string ExtendProgressionHash163(BattleState battle, string original) =>
            battle.Progression163 == null ? original : CanonicalJson.Sha256Hex(new { OriginalBattleHash = original, battle.Progression163 });
    }
}
