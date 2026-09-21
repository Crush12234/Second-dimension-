namespace SecondDimension.Presentation
{
    public sealed class BattleRelationshipVisual008
    {
        internal BattleRelationshipVisual008(
            string label,
            string iconToken,
            string lineToken,
            string pulseToken,
            int priority)
        {
            Label = label;
            IconToken = iconToken;
            LineToken = lineToken;
            PulseToken = pulseToken;
            Priority = priority;
        }

        public string Label { get; }
        public string IconToken { get; }
        public string LineToken { get; }
        public string PulseToken { get; }
        public int Priority { get; }
    }

    public static class BattleRelationshipVisualPolicy008
    {
        public static BattleRelationshipVisual008 For(BattleRelationshipKind008 kind)
        {
            switch (kind)
            {
                case BattleRelationshipKind008.RearAttack:
                    return new BattleRelationshipVisual008("REAR ATTACK", "REL_REAR", "LINE_REAR", "PULSE_DANGER", 100);
                case BattleRelationshipKind008.SideStrike:
                    return new BattleRelationshipVisual008("SIDE STRIKE", "REL_SIDE", "LINE_FLANK", "PULSE_TACTICAL", 90);
                case BattleRelationshipKind008.Interference:
                    return new BattleRelationshipVisual008("INTERFERENCE", "REL_INTERFERENCE", "LINE_INTERCEPT", "PULSE_WARNING", 85);
                case BattleRelationshipKind008.Rescue:
                    return new BattleRelationshipVisual008("RESCUE", "REL_RESCUE", "LINE_RESCUE", "PULSE_SUPPORT", 80);
                case BattleRelationshipKind008.Protect:
                    return new BattleRelationshipVisual008("PROTECT", "REL_PROTECT", "LINE_GUARD", "PULSE_GUARD", 75);
                case BattleRelationshipKind008.Guard:
                    return new BattleRelationshipVisual008("GUARD", "REL_GUARD", "LINE_GUARD", "PULSE_GUARD", 70);
                case BattleRelationshipKind008.Support:
                    return new BattleRelationshipVisual008("SUPPORT", "REL_SUPPORT", "LINE_SUPPORT", "PULSE_SUPPORT", 60);
                case BattleRelationshipKind008.Deadlock:
                    return new BattleRelationshipVisual008("DEADLOCK", "REL_DEADLOCK", "LINE_DEADLOCK", "PULSE_ENGAGED", 50);
                default:
                    return new BattleRelationshipVisual008("OPEN", "REL_OPEN", "LINE_NONE", "PULSE_NONE", 0);
            }
        }

        public static int Priority(BattleRelationshipKind008 kind) => For(kind).Priority;
    }
}
