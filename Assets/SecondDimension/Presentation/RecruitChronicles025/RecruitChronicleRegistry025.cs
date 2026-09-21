using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SecondDimension.Gameplay.RecruitChronicles025;
using UnityEngine;

namespace SecondDimension.Presentation.RecruitChronicles025
{
    [Serializable] public sealed class RecruitChronicleManifest025
    {
        public string contentVersion; public string title; public int signatureRecruitProfiles; public int personalQuestBoards; public int personalQuestNodes;
        public int freeHallEvents; public int mentorshipLessons; public int relationshipDimensions; public int relationshipMemories; public int relationshipChains;
        public int emergentLegendArchetypes; public int signatureTechniqueCandidates; public int unionIdentitySidegrades; public int visualHooks; public int proceduralQuestTemplates;
        public string[] hardLaws=Array.Empty<string>();
    }
    [Serializable] sealed class ProfileEnvelope025 { public RecruitChronicleProfile025[] profiles=Array.Empty<RecruitChronicleProfile025>(); }
    [Serializable] sealed class BoardEnvelope025 { public PersonalQuestBoardDefinition025[] boards=Array.Empty<PersonalQuestBoardDefinition025>(); }
    [Serializable] sealed class HallEnvelope025 { public HallSocialEventDefinition025[] events=Array.Empty<HallSocialEventDefinition025>(); }
    [Serializable] sealed class MentorEnvelope025 { public MentorshipLessonDefinition025[] lessons=Array.Empty<MentorshipLessonDefinition025>(); }
    [Serializable] sealed class DimensionEnvelope025 { public RelationshipDimensionDefinition025[] dimensions=Array.Empty<RelationshipDimensionDefinition025>(); }
    [Serializable] sealed class MemoryEnvelope025 { public RelationshipMemoryDefinition025[] memories=Array.Empty<RelationshipMemoryDefinition025>(); }
    [Serializable] sealed class ChainEnvelope025 { public RelationshipEventChainDefinition025[] chains=Array.Empty<RelationshipEventChainDefinition025>(); }
    [Serializable] sealed class LegendEnvelope025 { public LegendArchetypeDefinition025[] archetypes=Array.Empty<LegendArchetypeDefinition025>(); }
    [Serializable] sealed class TechniqueEnvelope025 { public SignatureTechniqueCandidateDefinition025[] candidates=Array.Empty<SignatureTechniqueCandidateDefinition025>(); }
    [Serializable] sealed class TraitEnvelope025 { public UnionIdentityTraitDefinition025[] traits=Array.Empty<UnionIdentityTraitDefinition025>(); }
    [Serializable] sealed class HookEnvelope025 { public VisualDetailHookDefinition025[] hooks=Array.Empty<VisualDetailHookDefinition025>(); }
    [Serializable] sealed class TemplateEnvelope025 { public ChronicleTemplateDefinition025[] templates=Array.Empty<ChronicleTemplateDefinition025>(); }
    [Serializable] sealed class ProceduralEnvelope025 { public ProceduralPersonalQuestTemplate025[] templates=Array.Empty<ProceduralPersonalQuestTemplate025>(); }

    public sealed class RecruitChronicleRegistry025 : IRecruitChronicleBoardAuthority025
    {
        const string Base="SecondDimension/RecruitChronicles025/Data/";
        public RecruitChronicleManifest025 Manifest{get;private set;}
        public IReadOnlyDictionary<string,RecruitChronicleProfile025> Profiles{get;private set;}
        public IReadOnlyDictionary<string,PersonalQuestBoardDefinition025> Boards{get;private set;}
        public IReadOnlyDictionary<string,HallSocialEventDefinition025> HallEvents{get;private set;}
        public IReadOnlyDictionary<string,MentorshipLessonDefinition025> MentorshipLessons{get;private set;}
        public IReadOnlyDictionary<string,RelationshipDimensionDefinition025> RelationshipDimensions{get;private set;}
        public IReadOnlyDictionary<string,RelationshipMemoryDefinition025> Memories{get;private set;}
        public IReadOnlyDictionary<string,RelationshipEventChainDefinition025> RelationshipChains{get;private set;}
        public IReadOnlyDictionary<string,LegendArchetypeDefinition025> Legends{get;private set;}
        public IReadOnlyDictionary<string,SignatureTechniqueCandidateDefinition025> Techniques{get;private set;}
        public IReadOnlyDictionary<string,UnionIdentityTraitDefinition025> UnionIdentityTraits{get;private set;}
        public IReadOnlyDictionary<string,VisualDetailHookDefinition025> VisualHooks{get;private set;}
        public IReadOnlyDictionary<string,ChronicleTemplateDefinition025> ChronicleTemplates{get;private set;}
        public IReadOnlyDictionary<string,ProceduralPersonalQuestTemplate025> ProceduralTemplates{get;private set;}

        public static RecruitChronicleRegistry025 LoadFromResources()
        {
            var r=new RecruitChronicleRegistry025
            {
                Manifest=Load<RecruitChronicleManifest025>("RecruitChronicleManifest025"),
                Profiles=Map(Load<ProfileEnvelope025>("RecruitProfiles025").profiles,x=>x.profileId),
                Boards=Map(Load<BoardEnvelope025>("PersonalQuestBoards025").boards,x=>x.boardId),
                HallEvents=Map(Load<HallEnvelope025>("HallSocialEvents025").events,x=>x.sceneId),
                MentorshipLessons=Map(Load<MentorEnvelope025>("MentorshipLessons025").lessons,x=>x.lessonId),
                RelationshipDimensions=Map(Load<DimensionEnvelope025>("RelationshipDimensions025").dimensions,x=>x.dimensionId),
                Memories=Map(Load<MemoryEnvelope025>("RelationshipMemoryDefinitions025").memories,x=>x.memoryId),
                RelationshipChains=Map(Load<ChainEnvelope025>("RelationshipEventChains025").chains,x=>x.chainId),
                Legends=Map(Load<LegendEnvelope025>("LegendArchetypes025").archetypes,x=>x.archetypeId),
                Techniques=Map(Load<TechniqueEnvelope025>("SignatureTechniqueCandidates025").candidates,x=>x.candidateId),
                UnionIdentityTraits=Map(Load<TraitEnvelope025>("UnionIdentityTraits025").traits,x=>x.traitId),
                VisualHooks=Map(Load<HookEnvelope025>("VisualDetailHooks025").hooks,x=>x.hookId),
                ChronicleTemplates=Map(Load<TemplateEnvelope025>("ChronicleTemplates025").templates,x=>x.templateId),
                ProceduralTemplates=Map(Load<ProceduralEnvelope025>("ProceduralPersonalQuestTemplates025").templates,x=>x.templateId)
            };
            r.ValidateOrThrow(); return r;
        }
        public RecruitChronicleProfile025 ProfileForRecruit(string recruitId)=>Profiles.Values.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.recruitId,recruitId));
        public PersonalQuestBoardDefinition025 Board(string boardId)=>Boards.TryGetValue(boardId,out var value)?value:null;
        public bool TryGetCanonicalBoard(string boardId,out PersonalQuestBoardDefinition025 board)=>Boards.TryGetValue(boardId,out board);
        public bool RecruitOwnsBoard(string authoredRecruitId,string boardId)=>Profiles.Values.Any(profile=>
            StringComparer.Ordinal.Equals(profile.recruitId,authoredRecruitId)&&
            (profile.personalQuestBoardIds??Array.Empty<string>()).Contains(boardId,StringComparer.Ordinal));
        public RelationshipMemoryDefinition025 Memory(string memoryId)=>Memories.TryGetValue(memoryId,out var value)?value:null;
        public HallSocialEventDefinition025 HallScene(string sceneId)=>HallEvents.TryGetValue(sceneId,out var value)?value:null;
        public void ValidateOrThrow()
        {
            if(Manifest==null||!StringComparer.Ordinal.Equals(Manifest.contentVersion,"RECRUIT_CHRONICLES_025_1.0"))throw new InvalidOperationException("Recruit Chronicle manifest is invalid.");
            Check(Profiles.Count,Manifest.signatureRecruitProfiles,"profiles");Check(Boards.Count,Manifest.personalQuestBoards,"boards");
            Check(Boards.Values.Sum(x=>x.nodes==null?0:x.nodes.Length),Manifest.personalQuestNodes,"nodes");Check(HallEvents.Count,Manifest.freeHallEvents,"Hall scenes");
            Check(MentorshipLessons.Count,Manifest.mentorshipLessons,"mentorship lessons");Check(RelationshipDimensions.Count,Manifest.relationshipDimensions,"dimensions");
            Check(Memories.Count,Manifest.relationshipMemories,"memories");Check(RelationshipChains.Count,Manifest.relationshipChains,"chains");
            Check(Legends.Count,Manifest.emergentLegendArchetypes,"legends");Check(Techniques.Count,Manifest.signatureTechniqueCandidates,"techniques");
            Check(UnionIdentityTraits.Count,Manifest.unionIdentitySidegrades,"Union traits");Check(VisualHooks.Count,Manifest.visualHooks,"visual hooks");
            foreach(var p in Profiles.Values){if(!p.permanentRecruit||p.canInvoluntarilyLeave||p.relationshipScenesCostOperation||p.relationshipScenesExpire)throw new InvalidOperationException("Permanent recruit law failed: "+p.recruitId);foreach(var b in p.personalQuestBoardIds??Array.Empty<string>())if(!Boards.ContainsKey(b))throw new InvalidOperationException("Missing personal quest board "+b);}
            foreach(var b in Boards.Values){if(!b.deterministic||!b.reloadCannotReroll||!b.failureRecoverable||b.canCauseDeparture||b.nodes==null||b.nodes.Length!=10)throw new InvalidOperationException("Personal quest law failed: "+b.boardId);}
            foreach(var e in HallEvents.Values)if(e.operationCost!=0||e.expires||e.canCauseDeparture||!e.deferWithoutPenalty)throw new InvalidOperationException("Hall scene law failed: "+e.sceneId);
        }
        static void Check(int actual,int expected,string label){if(actual!=expected)throw new InvalidOperationException(label+" mismatch: "+actual+" != "+expected);}
        static T Load<T>(string name){var a=Resources.Load<TextAsset>(Base+name);if(a==null)throw new InvalidOperationException("Missing Recruit Chronicles resource: "+name);var v=JsonConvert.DeserializeObject<T>(a.text);if(v==null)throw new InvalidOperationException("Invalid Recruit Chronicles resource: "+name);return v;}
        static IReadOnlyDictionary<string,T> Map<T>(IEnumerable<T> values,Func<T,string> id){var d=new Dictionary<string,T>(StringComparer.Ordinal);foreach(var v in values??Array.Empty<T>()){var k=id(v);if(string.IsNullOrWhiteSpace(k)||d.ContainsKey(k))throw new InvalidOperationException("Invalid or duplicate Recruit Chronicle ID: "+k);d.Add(k,v);}return d;}
    }
}
