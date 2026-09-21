using System;
using System.Collections.Generic;
using System.Globalization;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Presentation-only actions understood by the Slice 018 3D battle stage.
    /// These values describe how an authoritative beat should read; they never
    /// calculate damage, spend AP, change formation state, or resolve a command.
    /// </summary>
    public enum Battle3DAction
    {
        Establish,
        CommitCommand,
        WeaponStrike,
        CombatArt,
        MysticArt,
        Restore,
        Guard,
        Intercept,
        Recover,
        Reform,
        Reposition,
        TacticalStrike,
        Downed,
        Learn,
        Breakthrough,
        Retreat,
        Result,
        CaptionOnly
    }

    public enum Battle3DCameraIntent
    {
        Wide,
        Union,
        Anticipation,
        Tracking,
        Impact,
        Support,
        BlindSide,
        Hero,
        Result
    }

    /// <summary>
    /// Immutable projection of one authoritative presentation beat. DisplayAmount
    /// is copied for UI/VFX emphasis only; consumers must not feed it back to M2.
    /// </summary>
    public sealed class Battle3DPresentationDirective
    {
        internal Battle3DPresentationDirective(
            int sourceSequence,
            int round,
            string eventType,
            Battle3DAction action,
            Battle3DCameraIntent camera,
            string actorUnionId,
            string actorMemberId,
            string targetUnionId,
            string targetMemberId,
            string artId,
            int displayAmount,
            string caption,
            bool visuallyStaged,
            bool sideStrike,
            bool blindSide,
            bool useImpactPause,
            bool useCameraShake,
            Battle3DArtChoreography071 artChoreography)
        {
            SourceSequence = sourceSequence;
            Round = round;
            EventType = eventType ?? string.Empty;
            Action = action;
            Camera = camera;
            ActorUnionId = actorUnionId ?? string.Empty;
            ActorMemberId = actorMemberId ?? string.Empty;
            TargetUnionId = targetUnionId ?? string.Empty;
            TargetMemberId = targetMemberId ?? string.Empty;
            ArtId = artId ?? string.Empty;
            DisplayAmount = displayAmount;
            Caption = caption ?? string.Empty;
            VisuallyStaged = visuallyStaged;
            IsSideStrike = sideStrike;
            IsBlindSide = blindSide;
            UseImpactPause = useImpactPause;
            UseCameraShake = useCameraShake;
            ArtChoreography071 = artChoreography;
        }

        public int SourceSequence { get; }
        public int Round { get; }
        public string EventType { get; }
        public Battle3DAction Action { get; }
        public Battle3DCameraIntent Camera { get; }
        public string ActorUnionId { get; }
        public string ActorMemberId { get; }
        public string TargetUnionId { get; }
        public string TargetMemberId { get; }
        public string ArtId { get; }
        public int DisplayAmount { get; }
        public string Caption { get; }
        public bool VisuallyStaged { get; }
        public bool IsSideStrike { get; }
        public bool IsBlindSide { get; }
        public bool UseImpactPause { get; }
        public bool UseCameraShake { get; }
        public Battle3DArtChoreography071 ArtChoreography071 { get; }

        public bool IsPresentationOnly => true;
        public bool HasFirstHourArtChoreography071 => ArtChoreography071 != null;
        public bool UsesFirstHourArtPerformance071 => ArtChoreography071 != null &&
            Action != Battle3DAction.Establish &&
            Action != Battle3DAction.CommitCommand &&
            Action != Battle3DAction.Downed &&
            Action != Battle3DAction.Learn &&
            Action != Battle3DAction.Breakthrough &&
            Action != Battle3DAction.Retreat &&
            Action != Battle3DAction.Result &&
            Action != Battle3DAction.CaptionOnly;

        public string TacticalCallout
        {
            get
            {
                if (IsSideStrike && IsBlindSide) return "SIDE STRIKE · BLIND SIDE";
                if (IsSideStrike) return "SIDE STRIKE";
                return IsBlindSide ? "BLIND SIDE" : string.Empty;
            }
        }

        public string StableDescriptor => string.Join("|", new[]
        {
            SourceSequence.ToString(CultureInfo.InvariantCulture),
            Round.ToString(CultureInfo.InvariantCulture),
            EventType,
            Action.ToString(),
            Camera.ToString(),
            ActorUnionId,
            ActorMemberId,
            TargetUnionId,
            TargetMemberId,
            ArtId,
            DisplayAmount.ToString(CultureInfo.InvariantCulture),
            Caption,
            VisuallyStaged ? "VISUAL" : "NONVISUAL",
            IsSideStrike ? "SIDE_STRIKE" : "NO_SIDE_STRIKE",
            IsBlindSide ? "BLIND_SIDE" : "NO_BLIND_SIDE",
            UseImpactPause ? "HIT_STOP" : "NO_HIT_STOP",
            UseCameraShake ? "SHAKE" : "NO_SHAKE",
            ArtChoreography071?.StableDescriptor ?? "NO_FIRST_HOUR_ART_CHOREOGRAPHY"
        });
    }

    /// <summary>
    /// Converts an already-planned battle beat into 3D staging intent. This is a
    /// one-way, deterministic projection: it has no gameplay service dependency and
    /// cannot alter authoritative state.
    /// </summary>
    public static class M2Battle3DPresentationPolicy
    {
        public static Battle3DPresentationDirective CreateDirective(BattlePresentationBeat beat)
        {
            if (beat == null)
            {
                return new Battle3DPresentationDirective(
                    0, 0, string.Empty, Battle3DAction.CaptionOnly, Battle3DCameraIntent.Wide,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    0, string.Empty, false, false, false, false, false, null);
            }

            var action = MapAction(beat.Family);
            // POSITION_SHIFT + CMD_FLANK is authoritative tactical meaning. Caption
            // text is presentation copy and must never create combat state.
            var flankPositionShift = beat.Family == BattleBeatFamily.Positioning &&
                                     StringComparer.Ordinal.Equals(beat.EventType, "POSITION_SHIFT") &&
                                     StringComparer.Ordinal.Equals(beat.ArtId, "CMD_FLANK");
            var sideStrike = flankPositionShift;
            var blindSide = flankPositionShift;
            var impactPause = UsesImpactPause(action);
            var cameraShake = UsesCameraShake(action);
            var camera = sideStrike || blindSide
                ? Battle3DCameraIntent.BlindSide
                : MapCamera(beat.Camera);
            Battle3DArtChoreography071.TryCreate(beat.ArtId, out var artChoreography);

            return new Battle3DPresentationDirective(
                beat.SourceSequence,
                beat.Round,
                beat.EventType,
                action,
                camera,
                beat.ActorUnionId,
                beat.ActorMemberId,
                beat.TargetUnionId,
                beat.TargetMemberId,
                beat.ArtId,
                beat.Amount,
                beat.Caption,
                beat.VisuallyStaged && action != Battle3DAction.CaptionOnly,
                sideStrike,
                blindSide,
                impactPause,
                cameraShake,
                artChoreography);
        }

        public static IReadOnlyList<Battle3DPresentationDirective> CreateDirectives(
            IReadOnlyList<BattlePresentationBeat> beats)
        {
            var result = new List<Battle3DPresentationDirective>();
            if (beats == null) return result.AsReadOnly();
            for (var index = 0; index < beats.Count; index++)
                result.Add(CreateDirective(beats[index]));
            return result.AsReadOnly();
        }

        private static Battle3DAction MapAction(BattleBeatFamily family)
        {
            switch (family)
            {
                case BattleBeatFamily.Establishing: return Battle3DAction.Establish;
                case BattleBeatFamily.CommandCommit: return Battle3DAction.CommitCommand;
                case BattleBeatFamily.Invocation: return Battle3DAction.MysticArt;
                case BattleBeatFamily.BasicMartial: return Battle3DAction.WeaponStrike;
                case BattleBeatFamily.CombatArt: return Battle3DAction.CombatArt;
                case BattleBeatFamily.Mystic: return Battle3DAction.MysticArt;
                case BattleBeatFamily.Restoration: return Battle3DAction.Restore;
                case BattleBeatFamily.Guard: return Battle3DAction.Guard;
                case BattleBeatFamily.Interception: return Battle3DAction.Intercept;
                case BattleBeatFamily.Recovery: return Battle3DAction.Recover;
                case BattleBeatFamily.Formation: return Battle3DAction.Reform;
                case BattleBeatFamily.Positioning: return Battle3DAction.Reposition;
                case BattleBeatFamily.Tactical: return Battle3DAction.TacticalStrike;
                case BattleBeatFamily.Downed: return Battle3DAction.Downed;
                case BattleBeatFamily.Learning: return Battle3DAction.Learn;
                case BattleBeatFamily.Breakthrough: return Battle3DAction.Breakthrough;
                case BattleBeatFamily.Retreat: return Battle3DAction.Retreat;
                case BattleBeatFamily.Result: return Battle3DAction.Result;
                default: return Battle3DAction.CaptionOnly;
            }
        }

        private static Battle3DCameraIntent MapCamera(BattleCameraShot camera)
        {
            switch (camera)
            {
                case BattleCameraShot.ActingUnion: return Battle3DCameraIntent.Union;
                case BattleCameraShot.Anticipation: return Battle3DCameraIntent.Anticipation;
                case BattleCameraShot.ActionTrack: return Battle3DCameraIntent.Tracking;
                case BattleCameraShot.Impact: return Battle3DCameraIntent.Impact;
                case BattleCameraShot.Support: return Battle3DCameraIntent.Support;
                case BattleCameraShot.Breakthrough: return Battle3DCameraIntent.Hero;
                case BattleCameraShot.Result: return Battle3DCameraIntent.Result;
                default: return Battle3DCameraIntent.Wide;
            }
        }

        private static bool UsesImpactPause(Battle3DAction action) =>
            action == Battle3DAction.WeaponStrike ||
            action == Battle3DAction.CombatArt ||
            action == Battle3DAction.MysticArt ||
            action == Battle3DAction.TacticalStrike ||
            action == Battle3DAction.Intercept ||
            action == Battle3DAction.Downed;

        private static bool UsesCameraShake(Battle3DAction action) =>
            action == Battle3DAction.CombatArt ||
            action == Battle3DAction.MysticArt ||
            action == Battle3DAction.TacticalStrike ||
            action == Battle3DAction.Downed ||
            action == Battle3DAction.Breakthrough;

    }
}
