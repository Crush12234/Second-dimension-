using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation
{
    public static class BattleActionPresentationPlanning008
    {
        public static BattleActionFamily008 ResolveFamily(
            M2PredictedActionView action,
            BattleRelationshipKind008 relationship,
            bool breakthrough)
        {
            if (breakthrough || (action != null && action.BreakthroughOpportunity))
                return BattleActionFamily008.Breakthrough;
            if (relationship == BattleRelationshipKind008.SideStrike ||
                relationship == BattleRelationshipKind008.RearAttack ||
                relationship == BattleRelationshipKind008.Interference)
                return BattleActionFamily008.FlankOrRear;

            var kind = action?.ActionKind ?? string.Empty;
            var discipline = action?.Discipline ?? string.Empty;
            var animation = action?.AnimationTag ?? string.Empty;
            if (Contains(kind, "Restoration") || Contains(discipline, "Restoration") || Contains(kind, "Heal"))
                return BattleActionFamily008.Restoration;
            if (Contains(kind, "Guard") || Contains(discipline, "Guard") || Contains(kind, "Protect"))
                return BattleActionFamily008.Guard;
            if (Contains(kind, "Support") || Contains(discipline, "Support"))
                return BattleActionFamily008.Support;
            if (Contains(kind, "Mystic") || Contains(discipline, "Mystic") || Contains(kind, "Magic"))
                return BattleActionFamily008.Mystic;
            if (Contains(animation, "Bow") || Contains(animation, "Ranged") || Contains(animation, "Projectile"))
                return BattleActionFamily008.RangedMartial;
            if (Contains(kind, "Coordinated") || Contains(kind, "Link"))
                return BattleActionFamily008.CoordinatedUnion;
            if (Contains(animation, "Heavy") || Contains(kind, "Break"))
                return BattleActionFamily008.HeavyMartial;
            return BattleActionFamily008.BasicMartial;
        }

        public static BattleActionSchedule008 Schedule(
            BattleActionFamily008 family,
            BattleRelationshipKind008 relationship,
            bool reducedMotion)
        {
            var timings = Timings(family, reducedMotion);
            var beats = new List<BattleActionBeat008>();
            var cursor = 0f;
            Add(beats, ref cursor, "read", timings[0], false);
            Add(beats, ref cursor, "anticipation", timings[1], false);
            Add(beats, ref cursor, "approach_or_cast", timings[2], false);
            var hitTime = cursor;
            Add(beats, ref cursor, "contact_or_release", timings[3], true);
            Add(beats, ref cursor, "consequence", timings[4], false);
            Add(beats, ref cursor, "recovery", timings[5], false);
            Add(beats, ref cursor, "formation_return", timings[6], false);
            return new BattleActionSchedule008(
                family, SelectShot(family, relationship), beats.AsReadOnly(), hitTime);
        }

        public static BattleShot008 SelectShot(
            BattleActionFamily008 family,
            BattleRelationshipKind008 relationship)
        {
            if (relationship == BattleRelationshipKind008.RearAttack) return BattleShot008.RearAttack;
            if (relationship == BattleRelationshipKind008.SideStrike ||
                relationship == BattleRelationshipKind008.Interference) return BattleShot008.FlankReveal;
            if (relationship == BattleRelationshipKind008.Guard ||
                relationship == BattleRelationshipKind008.Protect ||
                relationship == BattleRelationshipKind008.Rescue) return BattleShot008.GuardInterception;
            switch (family)
            {
                case BattleActionFamily008.RangedMartial: return BattleShot008.RangedTrack;
                case BattleActionFamily008.Mystic: return BattleShot008.MysticField;
                case BattleActionFamily008.Restoration: return BattleShot008.HealerRecipient;
                case BattleActionFamily008.Guard: return BattleShot008.GuardInterception;
                case BattleActionFamily008.FlankOrRear: return BattleShot008.FlankReveal;
                case BattleActionFamily008.Breakthrough: return BattleShot008.BreakthroughCue;
                case BattleActionFamily008.Result: return BattleShot008.VictoryWide;
                default: return BattleShot008.MeleeContact;
            }
        }

        private static float[] Timings(BattleActionFamily008 family, bool reducedMotion)
        {
            float[] normal;
            switch (family)
            {
                case BattleActionFamily008.HeavyMartial:
                    normal = new[] { 0.16f, 0.34f, 0.28f, 0.08f, 0.30f, 0.34f, 0.24f }; break;
                case BattleActionFamily008.RangedMartial:
                    normal = new[] { 0.14f, 0.26f, 0.20f, 0.08f, 0.24f, 0.28f, 0.18f }; break;
                case BattleActionFamily008.Mystic:
                    normal = new[] { 0.18f, 0.32f, 0.30f, 0.10f, 0.32f, 0.34f, 0.20f }; break;
                case BattleActionFamily008.Restoration:
                    normal = new[] { 0.16f, 0.28f, 0.26f, 0.10f, 0.34f, 0.28f, 0.18f }; break;
                case BattleActionFamily008.Guard:
                    normal = new[] { 0.12f, 0.24f, 0.18f, 0.08f, 0.28f, 0.30f, 0.18f }; break;
                case BattleActionFamily008.CoordinatedUnion:
                    normal = new[] { 0.18f, 0.36f, 0.34f, 0.10f, 0.38f, 0.38f, 0.28f }; break;
                case BattleActionFamily008.FlankOrRear:
                    normal = new[] { 0.14f, 0.28f, 0.30f, 0.08f, 0.34f, 0.32f, 0.22f }; break;
                case BattleActionFamily008.Breakthrough:
                    normal = new[] { 0.18f, 0.42f, 0.24f, 0.10f, 0.34f, 0.30f, 0.18f }; break;
                default:
                    normal = new[] { 0.12f, 0.24f, 0.22f, 0.07f, 0.24f, 0.28f, 0.18f }; break;
            }
            if (!reducedMotion) return normal;
            normal[2] *= 0.55f;
            normal[4] *= 0.80f;
            normal[5] *= 0.70f;
            normal[6] *= 0.45f;
            return normal;
        }

        private static void Add(
            ICollection<BattleActionBeat008> beats,
            ref float cursor,
            string name,
            float duration,
            bool impact)
        {
            beats.Add(new BattleActionBeat008(name, cursor, duration, impact));
            cursor += duration;
        }

        private static bool Contains(string source, string value) =>
            !string.IsNullOrWhiteSpace(source) &&
            source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
