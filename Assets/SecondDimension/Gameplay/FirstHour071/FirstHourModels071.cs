using System;
using System.Collections.Generic;

namespace SecondDimension.Gameplay.FirstHour071
{
    public enum FirstHourPhase071
    {
        SkyhomeArrival = 0,
        EmergencyCharter = 1,
        BellWithoutRope = 2,
        HallBreachBattle = 3,
        LanternRoadOrder = 4,
        ReadyTheGuild = 5,
        FormTheUnions = 6,
        WalkLanternRoad = 7,
        RoadsideAmbushBattle = 8,
        FindThePatrol = 9,
        RecoverTheWayglass = 10,
        GateEaterBattle = 11,
        ReturnToSkyhome = 12,
        ChapterTwoHook = 13,
        Complete = 14
    }

    public enum FirstHourAction071
    {
        ReachGuildHall = 0,
        SignEmergencyCharter = 1,
        FollowTheBell = 2,
        WinHallBreach = 3,
        AcceptLanternRoadOrder = 4,
        ReadyRosterAndEquipment = 5,
        ConfirmThreeUnions = 6,
        ReachBrokenWaymarker = 7,
        WinRoadsideAmbush = 8,
        FollowPatrolTrail = 9,
        FreePatrolAndRecoverWayglass = 10,
        WinGateEaterBattle = 11,
        EnterGuildHall = 12,
        OpenWayglassMap = 13
    }

    public enum FirstHourSequenceKind071
    {
        Story = 0,
        GuildManagement = 1,
        Exploration = 2,
        Battle = 3,
        ChapterHook = 4
    }

    public enum FirstHourRosterWave071
    {
        OpeningLead = 0,
        SkyhomeCharter = 1,
        LanternPatrol = 2
    }

    [Serializable]
    public sealed class FirstHourDialogueBeat071
    {
        public FirstHourDialogueBeat071(string speakerId, string speakerName, string line)
        {
            SpeakerId = speakerId ?? string.Empty;
            SpeakerName = speakerName ?? string.Empty;
            Line = line ?? string.Empty;
        }

        public string SpeakerId { get; }
        public string SpeakerName { get; }
        public string Line { get; }
    }

    [Serializable]
    public sealed class FirstHourSegment071
    {
        public FirstHourSegment071(
            string segmentId,
            FirstHourPhase071 phase,
            int startMinute,
            int endMinute,
            FirstHourSequenceKind071 kind,
            string objective,
            FirstHourAction071 requiredAction,
            string nextAction,
            string destinationId,
            string destinationName,
            string completionCheckpointId,
            string encounterId,
            IReadOnlyList<FirstHourDialogueBeat071> dialogue)
        {
            SegmentId = segmentId ?? string.Empty;
            Phase = phase;
            StartMinute = startMinute;
            EndMinute = endMinute;
            Kind = kind;
            Objective = objective ?? string.Empty;
            RequiredAction = requiredAction;
            NextAction = nextAction ?? string.Empty;
            DestinationId = destinationId ?? string.Empty;
            DestinationName = destinationName ?? string.Empty;
            CompletionCheckpointId = completionCheckpointId ?? string.Empty;
            EncounterId = encounterId ?? string.Empty;
            Dialogue = dialogue ?? Array.Empty<FirstHourDialogueBeat071>();
        }

        public string SegmentId { get; }
        public FirstHourPhase071 Phase { get; }
        public int StartMinute { get; }
        public int EndMinute { get; }
        public FirstHourSequenceKind071 Kind { get; }
        public string Objective { get; }
        public FirstHourAction071 RequiredAction { get; }
        public string NextAction { get; }
        public string DestinationId { get; }
        public string DestinationName { get; }
        public string CompletionCheckpointId { get; }
        public string EncounterId { get; }
        public IReadOnlyList<FirstHourDialogueBeat071> Dialogue { get; }
        public bool IsBattle => Kind == FirstHourSequenceKind071.Battle;
    }

    [Serializable]
    public sealed class FirstHourRosterEntry071
    {
        public FirstHourRosterEntry071(
            string recruitId,
            string displayName,
            string classId,
            string homeName,
            FirstHourRosterWave071 wave,
            int availableAtMinute,
            FirstHourPhase071 availableAtPhase,
            string availabilityReason)
        {
            RecruitId = recruitId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            ClassId = classId ?? string.Empty;
            HomeName = homeName ?? string.Empty;
            Wave = wave;
            AvailableAtMinute = availableAtMinute;
            AvailableAtPhase = availableAtPhase;
            AvailabilityReason = availabilityReason ?? string.Empty;
        }

        public string RecruitId { get; }
        public string DisplayName { get; }
        public string ClassId { get; }
        public string HomeName { get; }
        public FirstHourRosterWave071 Wave { get; }
        public int AvailableAtMinute { get; }
        public FirstHourPhase071 AvailableAtPhase { get; }
        public string AvailabilityReason { get; }
        public bool IsPlayable => true;
    }

    [Serializable]
    public sealed class FirstHourProtectedActor071
    {
        public FirstHourProtectedActor071(string actorId, string displayName, string canonStatus)
        {
            ActorId = actorId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            CanonStatus = canonStatus ?? string.Empty;
        }

        public string ActorId { get; }
        public string DisplayName { get; }
        public string CanonStatus { get; }
        public bool CanJoinFirstHourRoster => false;
    }

    [Serializable]
    public sealed class FirstHourState071
    {
        internal FirstHourState071(int completedSegmentCount)
        {
            CompletedSegmentCount = completedSegmentCount;
        }

        public int CompletedSegmentCount { get; }
    }
}
