using System;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only pose vocabulary for Battle Art 011. It maps an already
    /// resolved authoritative beat to a clean local pose ID; it never selects Arts,
    /// targets, resources, damage, healing, or learning outcomes.
    /// </summary>
    public static class BattleArtPoseDirector011
    {
        public const string Idle = "POSE_IDLE";
        public const string Anticipation = "POSE_ANTICIPATION";
        public const string ActionPrimary = "POSE_ACTION_PRIMARY";
        public const string RolePrimary = "POSE_ROLE_PRIMARY";
        public const string HitReaction = "POSE_HIT_REACTION";
        public const string Recovery = "POSE_RECOVERY";
        public const string Downed = "POSE_DOWNED";
        public const string Victory = "POSE_VICTORY";

        public static string ActivePoseFor(BattleBeatFamily family)
        {
            switch (family)
            {
                case BattleBeatFamily.Mystic:
                case BattleBeatFamily.Invocation:
                case BattleBeatFamily.Restoration:
                case BattleBeatFamily.Guard:
                case BattleBeatFamily.Interception:
                case BattleBeatFamily.Formation:
                    return RolePrimary;
                case BattleBeatFamily.Recovery:
                    return Idle;
                default:
                    return ActionPrimary;
            }
        }

        public static bool IsSupportedPoseId(string poseId)
        {
            return StringComparer.Ordinal.Equals(poseId, Idle) ||
                   StringComparer.Ordinal.Equals(poseId, Anticipation) ||
                   StringComparer.Ordinal.Equals(poseId, ActionPrimary) ||
                   StringComparer.Ordinal.Equals(poseId, RolePrimary) ||
                   StringComparer.Ordinal.Equals(poseId, HitReaction) ||
                   StringComparer.Ordinal.Equals(poseId, Recovery) ||
                   StringComparer.Ordinal.Equals(poseId, Downed) ||
                   StringComparer.Ordinal.Equals(poseId, Victory);
        }
    }
}
