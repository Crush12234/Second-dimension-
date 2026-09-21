using System;
using System.Collections.Generic;
using SecondDimension.Presentation;

namespace SecondDimension.Presentation.People029
{
    public interface IPeopleRuntimePresentationCoordinator029
    {
        PeopleRuntimePresentationState029 PeopleRuntime029 { get; }
        M1CommandResult StartPersonalQuest029(string boardId);
        M1CommandResult RecoverPersonalQuest029(string boardId);
        M1CommandResult AdvancePersonalQuest029(string boardId, string destinationNodeId);
        M1CommandResult EnterPersonalQuestBattle029(string boardId);
        M1CommandResult ViewHallScene029(string sceneId);
        M1CommandResult DeferHallScene029(string sceneId);
        M1CommandResult CompleteMentorship029(string lessonId, string mentorId, string studentId);
        M1CommandResult RecordRelationshipMemory029(string memoryId, string firstRecruitId, string secondRecruitId, string sourceId);
        M1CommandResult UnlockLegendTechnique029(string recruitId);
        M1CommandResult SetUnionBondDoctrine029(string unionId, string doctrineId);
    }

    [Serializable]
    public sealed class PeopleRuntimePresentationState029
    {
        public bool IsAvailable; public string Error=string.Empty; public int SignatureProfiles; public int PersonalQuestBoards;
        public int PersonalQuestNodes; public int HallScenes; public int MentorshipLessons; public int BondPairs; public int LinkArts;
        public int CreatorCodes; public int CreatorRooms; public int SaveFormat; public string LastCheckpointId=string.Empty;
        public IReadOnlyList<RecruitPeopleView029> Recruits=Array.Empty<RecruitPeopleView029>();
        public IReadOnlyList<PersonalQuestView029> Quests=Array.Empty<PersonalQuestView029>();
        public IReadOnlyList<HallSceneView029> AvailableHallScenes=Array.Empty<HallSceneView029>();
        public IReadOnlyList<BondPairView029> Bonds=Array.Empty<BondPairView029>();
        public IReadOnlyList<DoctrineView029> Doctrines=Array.Empty<DoctrineView029>();
        public IReadOnlyList<UnionPeopleView029> Unions=Array.Empty<UnionPeopleView029>();
    }
    [Serializable] public sealed class RecruitPeopleView029
    {
        // Stable IDs remain command-routing data. The presenter uses the player-facing fields only.
        public string RecruitId=string.Empty; public string BoardId=string.Empty; public string NextBoardId=string.Empty;
        public string DisplayName=string.Empty; public string ChronicleTitle=string.Empty; public string WorldId=string.Empty; public string WorldName=string.Empty;
        public string QuestProgressLabel=string.Empty; public string NextQuestLabel=string.Empty;
        public bool QuestStarted; public bool QuestComplete; public bool LegendUnlocked; public bool SignatureTechniqueUnlocked;
        public int MeaningfulMemories; public int TotalQuests; public int ActiveQuests; public int CompletedQuests;
    }
    [Serializable] public sealed class PersonalQuestView029
    {
        // Board/node IDs are intentionally internal so existing exact command routes stay unchanged.
        public string BoardId=string.Empty; public string RecruitId=string.Empty; public string CurrentNodeId=string.Empty;
        public IReadOnlyList<string> NextNodeIds=Array.Empty<string>();
        public string DisplayName=string.Empty; public string RecruitDisplayName=string.Empty; public string ChronicleTitle=string.Empty;
        public string ChapterLabel=string.Empty; public string WorldName=string.Empty; public string Theme=string.Empty;
        public string StoryContext=string.Empty; public string Objective=string.Empty; public string ProgressLabel=string.Empty;
        public string CurrentNodeTitle=string.Empty; public string CurrentNodeKind=string.Empty; public string CurrentRoomLabel=string.Empty;
        public string CurrentNodeDescription=string.Empty; public string CurrentInstruction=string.Empty;
        public IReadOnlyList<PersonalQuestTileView029> Tiles=Array.Empty<PersonalQuestTileView029>();
        public IReadOnlyList<PersonalQuestDestinationView029> Destinations=Array.Empty<PersonalQuestDestinationView029>();
        public bool Started; public bool Complete; public bool RequiresCertifiedBattle; public bool NeedsRecovery;
        public bool BattleInProgress; public bool BattleRewardAwaitingClaim;
        public bool RecoveryRequiresSupport;
        public string RecoveryMessage=string.Empty; public int CompletedRooms;
    }
    [Serializable] public sealed class PersonalQuestTileView029
    {
        public string NodeId=string.Empty; public string Kind=string.Empty; public int Sequence;
        public string RoomLabel=string.Empty; public string DisplayLabel=string.Empty; public string State=string.Empty;
        public bool IsCleared; public bool IsCurrent; public bool IsRevealed; public bool IsFaceDown; public bool IsUnchosenRoute;
    }
    [Serializable] public sealed class PersonalQuestDestinationView029
    {
        public string DestinationNodeId=string.Empty; public string Kind=string.Empty; public int Sequence;
        public string RoomLabel=string.Empty; public string ButtonLabel=string.Empty; public string Description=string.Empty;
    }
    [Serializable] public sealed class HallSceneView029 { public string SceneId=string.Empty; public string DisplayName=string.Empty; public string Summary=string.Empty; public string LocationTag=string.Empty; public string LocationName=string.Empty; public bool Viewed; public bool Deferred; }
    [Serializable] public sealed class BondPairView029 { public string PairId=string.Empty; public string FirstRecruitId=string.Empty; public string SecondRecruitId=string.Empty; public string FirstDisplayName=string.Empty; public string SecondDisplayName=string.Empty; public string TierId=string.Empty; public string TierDisplayName=string.Empty; public int TierRank; public int Trust; public int Respect; public int Familiarity; public int SharedMemories; }
    [Serializable] public sealed class UnionPeopleView029 { public string UnionId=string.Empty; public string DisplayName=string.Empty; public string DoctrineId=string.Empty; public string BondDoctrineId=string.Empty; public string BondDoctrineName=string.Empty; public IReadOnlyList<string> MemberRecruitIds=Array.Empty<string>(); }
    [Serializable] public sealed class DoctrineView029 { public string DoctrineId=string.Empty; public string DisplayName=string.Empty; public string MinimumBondTierId=string.Empty; public string PreferredForecastIntent=string.Empty; public string Summary=string.Empty; }
}
