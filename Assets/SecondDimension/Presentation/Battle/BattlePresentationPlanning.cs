using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation
{
    public enum BattleBeatFamily
    {
        Establishing,
        CommandCommit,
        Invocation,
        BasicMartial,
        CombatArt,
        Mystic,
        Restoration,
        Guard,
        Interception,
        Recovery,
        Formation,
        Positioning,
        Tactical,
        Downed,
        Learning,
        Breakthrough,
        Retreat,
        Result,
        IntentionallyNonVisual
    }

    public enum BattleCameraShot
    {
        Wide,
        ActingUnion,
        Anticipation,
        ActionTrack,
        Impact,
        Support,
        Breakthrough,
        Result
    }

    public sealed class BattlePresentationBeat
    {
        public BattlePresentationBeat(
            int sourceSequence,
            int round,
            string eventType,
            BattleBeatFamily family,
            BattleCameraShot camera,
            string actorUnionId,
            string actorMemberId,
            string targetUnionId,
            string targetMemberId,
            string artId,
            int amount,
            string caption,
            string vfxCue,
            string audioCue,
            bool visuallyStaged)
        {
            SourceSequence = sourceSequence;
            Round = round;
            EventType = eventType ?? string.Empty;
            Family = family;
            Camera = camera;
            ActorUnionId = actorUnionId ?? string.Empty;
            ActorMemberId = actorMemberId ?? string.Empty;
            TargetUnionId = targetUnionId ?? string.Empty;
            TargetMemberId = targetMemberId ?? string.Empty;
            ArtId = artId ?? string.Empty;
            Amount = amount;
            Caption = caption ?? string.Empty;
            VfxCue = vfxCue ?? string.Empty;
            AudioCue = audioCue ?? string.Empty;
            VisuallyStaged = visuallyStaged;
        }

        public int SourceSequence { get; }
        public int Round { get; }
        public string EventType { get; }
        public BattleBeatFamily Family { get; }
        public BattleCameraShot Camera { get; }
        public string ActorUnionId { get; }
        public string ActorMemberId { get; }
        public string TargetUnionId { get; }
        public string TargetMemberId { get; }
        public string ArtId { get; }
        public int Amount { get; }
        public string Caption { get; }
        public string VfxCue { get; }
        public string AudioCue { get; }
        public bool VisuallyStaged { get; }

        public string StableDescriptor => string.Join("|", new[]
        {
            SourceSequence.ToString(), Round.ToString(), EventType, Family.ToString(), Camera.ToString(),
            ActorUnionId, ActorMemberId, TargetUnionId, TargetMemberId, ArtId, Amount.ToString(),
            Caption, VfxCue, AudioCue, VisuallyStaged ? "VISUAL" : "NONVISUAL"
        });
    }

    public static class BattlePresentationPlanner
    {
        public static IReadOnlyList<BattlePresentationBeat> Plan(IReadOnlyList<M2BattleEventView> events)
        {
            var result = new List<BattlePresentationBeat>();
            if (events == null) return result.AsReadOnly();
            for (var index = 0; index < events.Count; index++) result.Add(Map(events[index]));
            return result.AsReadOnly();
        }

        private static BattlePresentationBeat Map(M2BattleEventView item)
        {
            if (item == null)
                return Beat(new M2BattleEventView(), BattleBeatFamily.IntentionallyNonVisual,
                    BattleCameraShot.Wide, string.Empty, string.Empty, false);

            var id = Normalize(item.EventType);
            switch (id)
            {
                case "BATTLE_START":
                    return Beat(item, BattleBeatFamily.Establishing, BattleCameraShot.Wide,
                        "VFX_GATE_DUST", "AUDIO_BATTLE", true);
                case "FORECAST_COMMITTED":
                    return Beat(item, BattleBeatFamily.CommandCommit, BattleCameraShot.ActingUnion,
                        "VFX_COMMAND_SEAL", "SFX_COMMAND_CONFIRM", true);
                case "SUMMON_ECHO_INVOKED":
                    return Beat(item, BattleBeatFamily.Invocation, BattleCameraShot.ActionTrack,
                        "VFX_MYSTIC_BURST", "SFX_MYSTIC", true);
                case "GREAT_COVENANT_INVOKED":
                    return Beat(item, BattleBeatFamily.Invocation, BattleCameraShot.Breakthrough,
                        "VFX_BREAKTHROUGH", "SFX_BREAKTHROUGH", true);
                case "MARTIAL_HIT":
                    return Beat(item,
                        IsFundamental(item.ArtId) ? BattleBeatFamily.BasicMartial : BattleBeatFamily.CombatArt,
                        BattleCameraShot.Impact, "VFX_WEAPON_IMPACT", "SFX_WEAPON_IMPACT", true);
                case "MYSTIC_HIT":
                    return Beat(item, BattleBeatFamily.Mystic, BattleCameraShot.ActionTrack,
                        "VFX_MYSTIC_BURST", "SFX_MYSTIC", true);
                case "TACTICAL_HIT":
                    return Beat(item, BattleBeatFamily.Tactical, BattleCameraShot.ActionTrack,
                        "VFX_TACTICAL_STRIKE", "SFX_WEAPON_IMPACT", true);
                case "RESTORATION":
                case "REVIVED":
                case "CLEANSED":
                case "STABILIZED":
                    return Beat(item, BattleBeatFamily.Restoration, BattleCameraShot.Support,
                        "VFX_RESTORATION", "SFX_RESTORATION", true);
                case "GUARD":
                    return Beat(item, BattleBeatFamily.Guard, BattleCameraShot.ActingUnion,
                        "VFX_GUARD_BARRIER", "SFX_GUARD", true);
                case "INTERCEPTION":
                    return SwappedBeat(item, BattleBeatFamily.Interception, BattleCameraShot.Impact,
                        "VFX_GUARD_IMPACT", "SFX_GUARD_IMPACT", true);
                case "RECOVERY":
                case "AP_RECOVERY":
                    return Beat(item, BattleBeatFamily.Recovery, BattleCameraShot.Support,
                        "VFX_RECOVERY", "SFX_RECOVERY", true);
                case "FORMATION_RECOVERY":
                case "ALLY_SUPPORT":
                    return Beat(item, BattleBeatFamily.Formation, BattleCameraShot.ActingUnion,
                        "VFX_COHESION", "SFX_FORMATION", true);
                case "ALLY_PROTECTED":
                    return Beat(item, BattleBeatFamily.Guard, BattleCameraShot.Support,
                        "VFX_GUARD_BARRIER", "SFX_GUARD", true);
                case "ENEMY_SUPPORT_FORECAST":
                    return Beat(item, BattleBeatFamily.CommandCommit, BattleCameraShot.ActingUnion,
                        "VFX_COMMAND_SEAL", "SFX_COMMAND_CONFIRM", true);
                case "POSITION_SHIFT":
                    return Beat(item, BattleBeatFamily.Positioning, BattleCameraShot.ActionTrack,
                        "VFX_TACTICAL_ROUTE", "SFX_WHOOSH", true);
                case "ENEMY_HIT":
                    return Beat(item, BattleBeatFamily.BasicMartial, BattleCameraShot.Impact,
                        "VFX_ENEMY_IMPACT", "SFX_WEAPON_IMPACT", true);
                case "DOWNED":
                    return Beat(item, BattleBeatFamily.Downed, BattleCameraShot.Impact,
                        "VFX_DOWNED_DUST", "SFX_DOWNED", true);
                case "ART_GROWTH":
                    return Beat(item, BattleBeatFamily.Learning, BattleCameraShot.ActingUnion,
                        "VFX_ART_GROWTH", "SFX_LEARNING", true);
                case "BREAKTHROUGH":
                    return Beat(item, BattleBeatFamily.Breakthrough, BattleCameraShot.Breakthrough,
                        "VFX_BREAKTHROUGH", "SFX_BREAKTHROUGH", true);
                case "RETREAT":
                    return Beat(item, BattleBeatFamily.Retreat, BattleCameraShot.Wide,
                        "VFX_RETREAT_DUST", "AUDIO_RETREAT", true);
                case "BATTLE_RESULT":
                    return Beat(item, BattleBeatFamily.Result, BattleCameraShot.Result,
                        "VFX_BATTLE_RESULT", "AUDIO_VICTORY", true);
                case "TARGET_MEMBER_RETARGETED":
                case "TARGET_UNION_RETARGETED":
                case "ENEMY_UNION_DEFEATED":
                case "ACTION_CANCELED_DEAD_TARGET":
                case "VICTORY_SEQUENCE_STOP":
                    // These are authoritative sequence/cancellation captions. Existing
                    // DOWNED, POSITION_SHIFT, hit, and BATTLE_RESULT beats own the one
                    // visible reaction for the same transition, preventing phantom or
                    // duplicated movement/VFX.
                    return Beat(item, BattleBeatFamily.IntentionallyNonVisual,
                        BattleCameraShot.Wide, string.Empty, string.Empty, false);
                default:
                    return Beat(item, BattleBeatFamily.IntentionallyNonVisual, BattleCameraShot.Wide,
                        string.Empty, string.Empty, false);
            }
        }

        private static BattlePresentationBeat Beat(
            M2BattleEventView item,
            BattleBeatFamily family,
            BattleCameraShot camera,
            string vfx,
            string audio,
            bool visual) =>
            new BattlePresentationBeat(
                item.Sequence,
                item.Round,
                item.EventType,
                family,
                camera,
                item.ActorUnionId,
                item.ActorMemberId,
                item.TargetUnionId,
                item.TargetMemberId,
                item.ArtId,
                item.Amount,
                item.Text,
                vfx,
                audio,
                visual);

        private static BattlePresentationBeat SwappedBeat(
            M2BattleEventView item,
            BattleBeatFamily family,
            BattleCameraShot camera,
            string vfx,
            string audio,
            bool visual) =>
            new BattlePresentationBeat(
                item.Sequence,
                item.Round,
                item.EventType,
                family,
                camera,
                item.TargetUnionId,
                item.TargetMemberId,
                item.ActorUnionId,
                item.ActorMemberId,
                item.ArtId,
                item.Amount,
                item.Text,
                vfx,
                audio,
                visual);

        private static bool IsFundamental(string artId) =>
            !string.IsNullOrWhiteSpace(artId) && artId.StartsWith("ART_BASIC_", StringComparison.Ordinal);

        private static string Normalize(string value) =>
            (value ?? string.Empty).Trim().Replace(' ', '_').ToUpperInvariant();
    }
}
