using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.RecruitChronicles025
{
    [Serializable] public sealed class RecruitChronicleProfile025
    {
        public string profileId; public string recruitId; public string displayName; public string raceId; public string worldId;
        public string startingClassId; public string chronicleTitle; public string coreWound; public string ambition; public string fear;
        public string[] personalQuestBoardIds = Array.Empty<string>(); public string[] hallSceneIds = Array.Empty<string>();
        public string signatureTechniqueCandidateId; public string legendArchetypeId; public string mentorshipAffinityId;
        public string unionIdentityTraitId; public string visualHookId; public bool permanentRecruit; public bool canInvoluntarilyLeave;
        public bool relationshipScenesCostOperation; public bool relationshipScenesExpire;
    }
    [Serializable] public sealed class PersonalQuestNodeDefinition025
    {
        public string nodeId; public string kind; public string title; public string description; public string[] nextNodeIds = Array.Empty<string>();
        public string[] choiceIds = Array.Empty<string>(); public string battleProfileId; public string checkDimensionId; public int difficulty;
        public string rewardMemoryId; public int operationCost;
    }
    [Serializable] public sealed class PersonalQuestBoardDefinition025
    {
        public string boardId; public string recruitId; public string displayName; public string theme; public string entryNodeId;
        public PersonalQuestNodeDefinition025[] nodes = Array.Empty<PersonalQuestNodeDefinition025>();
        public bool deterministic; public bool reloadCannotReroll; public bool failureRecoverable; public bool canCauseDeparture;
    }
    [Serializable] public sealed class HallSocialEventDefinition025
    {
        public string sceneId; public string displayName; public string[] participantRecruitIds = Array.Empty<string>(); public string triggerMemoryId;
        public string locationTag; public string summary; public int operationCost; public bool expires; public bool canCauseDeparture; public bool deferWithoutPenalty;
    }
    [Serializable] public sealed class MentorshipLessonDefinition025
    {
        public string lessonId; public string displayName; public string discipline; public int requiredMeaningfulUses; public int masteryReward;
        public string relationshipMemoryId; public int operationCost; public bool parallelProgress; public bool canCauseDeparture;
    }
    [Serializable] public sealed class RelationshipDimensionDefinition025
    {
        public string dimensionId; public string displayName; public int minimum; public int maximum; public int defaultValue;
        public string positiveMeaning; public string negativeMeaning;
    }
    [Serializable] public sealed class RelationshipMemoryDefinition025
    {
        public string memoryId; public string displayName; public string sourceKind; public string dimensionId; public int strength;
        public int trustDelta; public int respectDelta; public string summary; public bool repeatable; public int operationCost; public bool canCauseDeparture;
    }
    [Serializable] public sealed class RelationshipEventChainDefinition025
    {
        public string chainId; public string displayName; public string[] memoryIds = Array.Empty<string>(); public string conflictStateId;
        public string[] recoveryChoiceIds = Array.Empty<string>(); public bool canForceDeparture; public bool expires; public int operationCost;
    }
    [Serializable] public sealed class LegendArchetypeDefinition025
    {
        public string archetypeId; public string displayName; public int requiredPersonalQuestCount; public int requiredMeaningfulMemories;
        public int requiredMasteryPoints; public string unlockSummary;
    }
    [Serializable] public sealed class SignatureTechniqueCandidateDefinition025
    {
        public string candidateId; public string recruitId; public string displayName; public string templateId; public string requiredBoardId;
        public string requiredLegendArchetypeId; public int requiredMasteryPoints; public bool forecastOnlyInStandard; public bool directIndividualSelection;
    }
    [Serializable] public sealed class UnionIdentityTraitDefinition025
    {
        public string traitId; public string displayName; public string preferredIntent; public int cohesionModifier;
        public int disciplineGrowthBasisPoints; public bool directMemberSelection;
    }
    [Serializable] public sealed class VisualDetailHookDefinition025
    {
        public string hookId; public string displayName; public string trigger; public string description;
    }
    [Serializable] public sealed class ChronicleTemplateDefinition025 { public string templateId; public string displayName; public string summary; }
    [Serializable] public sealed class ProceduralPersonalQuestTemplate025
    {
        public string templateId; public string displayName; public string[] themeTags = Array.Empty<string>(); public string[] nodePattern = Array.Empty<string>();
        public string[] deterministicSeedParts = Array.Empty<string>(); public bool canCauseDeparture;
    }

    [Serializable] public sealed class PersonalQuestProgressState025
    {
        [JsonConstructor] public PersonalQuestProgressState025(string boardId,string recruitId,string currentNodeId,IReadOnlyList<string> completedNodeIds,
            IReadOnlyList<string> appliedReceiptIds,bool completed,string existingBattleRewardReceiptId,string lastCheckpointId)
        {
            BoardId=Req(boardId,nameof(boardId)); RecruitId=Req(recruitId,nameof(recruitId)); CurrentNodeId=currentNodeId??string.Empty;
            CompletedNodeIds=Copy(completedNodeIds); AppliedReceiptIds=Copy(appliedReceiptIds); Completed=completed;
            ExistingBattleRewardReceiptId=existingBattleRewardReceiptId??string.Empty; LastCheckpointId=lastCheckpointId??string.Empty;
        }
        public string BoardId{get;} public string RecruitId{get;} public string CurrentNodeId{get;} public IReadOnlyList<string> CompletedNodeIds{get;}
        public IReadOnlyList<string> AppliedReceiptIds{get;} public bool Completed{get;} public string ExistingBattleRewardReceiptId{get;} public string LastCheckpointId{get;}
        public PersonalQuestProgressState025 With(string currentNodeId=null,IReadOnlyList<string> completedNodeIds=null,IReadOnlyList<string> appliedReceiptIds=null,
            bool? completed=null,string existingBattleRewardReceiptId=null,string lastCheckpointId=null)=>new PersonalQuestProgressState025(BoardId,RecruitId,currentNodeId??CurrentNodeId,
            completedNodeIds??CompletedNodeIds,appliedReceiptIds??AppliedReceiptIds,completed??Completed,existingBattleRewardReceiptId??ExistingBattleRewardReceiptId,lastCheckpointId??LastCheckpointId);
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
    }
    [Serializable] public sealed class RelationshipMemoryProgressState025
    {
        [JsonConstructor] public RelationshipMemoryProgressState025(string receiptId,string firstRecruitId,string secondRecruitId,string memoryId,int strength,string sourceId)
        {ReceiptId=Req(receiptId,nameof(receiptId));FirstRecruitId=Req(firstRecruitId,nameof(firstRecruitId));SecondRecruitId=Req(secondRecruitId,nameof(secondRecruitId));
         MemoryId=Req(memoryId,nameof(memoryId));Strength=Math.Max(1,strength);SourceId=sourceId??string.Empty;}
        public string ReceiptId{get;} public string FirstRecruitId{get;} public string SecondRecruitId{get;} public string MemoryId{get;} public int Strength{get;} public string SourceId{get;}
        static string Req(string v,string p)=>string.IsNullOrWhiteSpace(v)?throw new ArgumentException("Stable ID required.",p):v;
    }
    [Serializable] public sealed class RecruitChronicleState025
    {
        public const string ContentVersion="RECRUIT_CHRONICLES_025_1.0";
        [JsonConstructor] public RecruitChronicleState025(string contentVersion,IReadOnlyList<PersonalQuestProgressState025> personalQuests,
            IReadOnlyList<string> viewedHallSceneIds,IReadOnlyList<string> completedMentorshipLessonIds,IReadOnlyList<RelationshipMemoryProgressState025> relationshipMemories,
            IReadOnlyList<string> unlockedLegendIds,IReadOnlyList<string> unlockedSignatureTechniqueIds,IReadOnlyList<string> appliedReceiptIds,string lastCheckpointId)
        {
            ContentAuthorityVersion=string.IsNullOrWhiteSpace(contentVersion)?ContentVersion:contentVersion;PersonalQuests=CopyQuests(personalQuests);
            ViewedHallSceneIds=Copy(viewedHallSceneIds);CompletedMentorshipLessonIds=Copy(completedMentorshipLessonIds);RelationshipMemories=CopyMemories(relationshipMemories);
            UnlockedLegendIds=Copy(unlockedLegendIds);UnlockedSignatureTechniqueIds=Copy(unlockedSignatureTechniqueIds);AppliedReceiptIds=Copy(appliedReceiptIds);
            LastCheckpointId=lastCheckpointId??string.Empty;
        }
        public string ContentAuthorityVersion{get;} public IReadOnlyList<PersonalQuestProgressState025> PersonalQuests{get;} public IReadOnlyList<string> ViewedHallSceneIds{get;}
        public IReadOnlyList<string> CompletedMentorshipLessonIds{get;} public IReadOnlyList<RelationshipMemoryProgressState025> RelationshipMemories{get;}
        public IReadOnlyList<string> UnlockedLegendIds{get;} public IReadOnlyList<string> UnlockedSignatureTechniqueIds{get;} public IReadOnlyList<string> AppliedReceiptIds{get;} public string LastCheckpointId{get;}
        public RecruitChronicleState025 With(IReadOnlyList<PersonalQuestProgressState025> personalQuests=null,IReadOnlyList<string> viewedHallSceneIds=null,
            IReadOnlyList<string> completedMentorshipLessonIds=null,IReadOnlyList<RelationshipMemoryProgressState025> relationshipMemories=null,
            IReadOnlyList<string> unlockedLegendIds=null,IReadOnlyList<string> unlockedSignatureTechniqueIds=null,IReadOnlyList<string> appliedReceiptIds=null,string lastCheckpointId=null)=>
            new RecruitChronicleState025(ContentAuthorityVersion,personalQuests??PersonalQuests,viewedHallSceneIds??ViewedHallSceneIds,
                completedMentorshipLessonIds??CompletedMentorshipLessonIds,relationshipMemories??RelationshipMemories,unlockedLegendIds??UnlockedLegendIds,
                unlockedSignatureTechniqueIds??UnlockedSignatureTechniqueIds,appliedReceiptIds??AppliedReceiptIds,lastCheckpointId??LastCheckpointId);
        public static RecruitChronicleState025 Default()=>new RecruitChronicleState025(ContentVersion,Array.Empty<PersonalQuestProgressState025>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<RelationshipMemoryProgressState025>(),Array.Empty<string>(),Array.Empty<string>(),Array.Empty<string>(),"recruit_chronicles_025_initialized");
        static IReadOnlyList<string> Copy(IReadOnlyList<string> values){var r=new List<string>();if(values!=null)for(var i=0;i<values.Count;i++)if(!string.IsNullOrWhiteSpace(values[i])&&!r.Contains(values[i]))r.Add(values[i]);r.Sort(StringComparer.Ordinal);return r.AsReadOnly();}
        static IReadOnlyList<PersonalQuestProgressState025> CopyQuests(IReadOnlyList<PersonalQuestProgressState025> values){var r=new List<PersonalQuestProgressState025>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Quest cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.BoardId,b.BoardId));return r.AsReadOnly();}
        static IReadOnlyList<RelationshipMemoryProgressState025> CopyMemories(IReadOnlyList<RelationshipMemoryProgressState025> values){var r=new List<RelationshipMemoryProgressState025>();if(values!=null)for(var i=0;i<values.Count;i++)r.Add(values[i]??throw new ArgumentException("Memory cannot be null.",nameof(values)));r.Sort((a,b)=>StringComparer.Ordinal.Compare(a.ReceiptId,b.ReceiptId));return r.AsReadOnly();}
    }
}
