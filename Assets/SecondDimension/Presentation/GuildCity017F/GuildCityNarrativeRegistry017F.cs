using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017F
{
    [Serializable] public sealed class GuildCityNarrativeRoot017F
    {
        public string contentVersion;
        public GuildCityOnboardingScene017F[] onboardingScenes;
        public GuildCityContractArc017F[] contractArcs;
        public GuildCityNodeNarrative017F[] nodeNarratives;
        public GuildCityEventNarrative017F[] eventNarratives;
        public GuildCityRelationshipScene017F[] relationshipScenes;
        public GuildCityFacilityNarrative017F[] facilities;
        public GuildCityHallAmbient017F[] hallAmbient;
        public GuildCityAudioCue017F[] audioCues;
        public GuildCityOutcomeEpilogue017F[] outcomeEpilogues;
    }

    [Serializable] public sealed class GuildCityOnboardingScene017F
    { public string id; public int operationMin; public int operationMax; public string title; public string narration; public string guildmasterLine; public string instruction; public string completion; }
    [Serializable] public sealed class GuildCityContractArc017F
    { public string id; public string openingTitle; public string sponsorName; public string sponsorLine; public string guildmasterPrompt; public string deploymentLine; public string battleBriefingLine; public string victoryLine; public string mixedLine; public string retreatLine; public string failureLine; public string cityConsequenceLine; }
    [Serializable] public sealed class GuildCityNodeNarrative017F
    { public string boardId; public string nodeId; public string title; public string arrivalText; public string decisionPrompt; public string riskLine; public string successTransition; public string setbackTransition; public string ambientCueId; }
    [Serializable] public sealed class GuildCityEventNarrative017F
    { public string id; public string title; public string speakerTag; public string[] openingLines; public string[] choicePrompts; public string[] exceptionalLines; public string[] fullSuccessLines; public string[] successWithCostLines; public string[] setbackLines; public string[] severeSetbackLines; public string relationshipMemory; public bool costsOperation; public bool canCauseDeparture; }
    [Serializable] public sealed class GuildCityRelationshipScene017F
    { public string id; public string[] triggerTags; public string title; public string setup; public string firstLine; public string secondLine; public string closing; public bool costsOperation; public bool expires; public bool canCauseDeparture; }
    [Serializable] public sealed class GuildCityFacilityState017F { public string state; public string resourcePath; }
    [Serializable] public sealed class GuildCityFacilityNarrative017F
    { public string id; public string displayName; public string districtId; public string constructionStart; public string constructionComplete; public string staffedLine; public string upgradeLine; public string[] ambientActions; public GuildCityFacilityState017F[] states; }
    [Serializable] public sealed class GuildCityHallAmbient017F
    { public string id; public string facilityId; public string roleTag; public string actionText; public string statusText; public int weight; }
    [Serializable] public sealed class GuildCityAudioCue017F
    { public string id; public string resourcePath; public string mode; public string description; public bool placeholder; }
    [Serializable] public sealed class GuildCityOutcomeEpilogue017F
    { public string id; public string contractId; public string outcome; public string narration; public string cityConsequence; }

    public static class GuildCityNarrativeRegistry017F
    {
        private const string ResourcePath = "SecondDimension/GuildCity017F/Data/OPENING_NARRATIVE_017F";
        private static GuildCityNarrativeRoot017F _root;
        private static Dictionary<string,GuildCityContractArc017F> _contracts;
        private static Dictionary<string,GuildCityNodeNarrative017F> _nodes;
        private static Dictionary<string,GuildCityEventNarrative017F> _events;
        private static Dictionary<string,GuildCityFacilityNarrative017F> _facilities;
        private static Dictionary<string,GuildCityAudioCue017F> _audio;
        private static readonly Dictionary<string,Sprite> Sprites = new Dictionary<string,Sprite>(StringComparer.Ordinal);
        private static readonly Dictionary<string,AudioClip> Clips = new Dictionary<string,AudioClip>(StringComparer.Ordinal);

        public static GuildCityNarrativeRoot017F Load()
        {
            if (_root != null) return _root;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing Guild City 017F narrative data: " + ResourcePath);
            _root = JsonUtility.FromJson<GuildCityNarrativeRoot017F>(asset.text);
            if (_root == null) throw new InvalidOperationException("Guild City 017F narrative data did not parse.");
            _contracts = Index(_root.contractArcs, value => value.id);
            _nodes = Index(_root.nodeNarratives, value => value.boardId + "|" + value.nodeId);
            _events = Index(_root.eventNarratives, value => value.id);
            _facilities = Index(_root.facilities, value => value.id);
            _audio = Index(_root.audioCues, value => value.id);
            return _root;
        }

        public static GuildCityContractArc017F Contract(string id) { Load(); return Find(_contracts,id); }
        public static GuildCityNodeNarrative017F Node(string boardId,string nodeId) { Load(); return Find(_nodes,(boardId ?? string.Empty)+"|"+(nodeId ?? string.Empty)); }
        public static GuildCityEventNarrative017F Event(string id) { Load(); return Find(_events,id); }
        public static GuildCityFacilityNarrative017F Facility(string id) { Load(); return Find(_facilities,id); }
        public static GuildCityAudioCue017F AudioCue(string id) { Load(); return Find(_audio,id); }

        public static GuildCityOnboardingScene017F OnboardingForOperation(int operationOrdinal)
        {
            var root = Load();
            if (root.onboardingScenes == null || root.onboardingScenes.Length == 0) return null;
            for (var i=0;i<root.onboardingScenes.Length;i++)
            {
                var value=root.onboardingScenes[i];
                if (value != null && operationOrdinal >= value.operationMin && operationOrdinal <= value.operationMax) return value;
            }
            return root.onboardingScenes[root.onboardingScenes.Length-1];
        }

        public static GuildCityRelationshipScene017F RelationshipForSummary(string summary)
        {
            var root=Load(); var source=summary ?? string.Empty;
            if (root.relationshipScenes != null)
                for (var i=0;i<root.relationshipScenes.Length;i++)
                {
                    var scene=root.relationshipScenes[i];
                    if (scene?.triggerTags == null) continue;
                    for (var j=0;j<scene.triggerTags.Length;j++)
                        if (!string.IsNullOrWhiteSpace(scene.triggerTags[j]) && source.IndexOf(scene.triggerTags[j],StringComparison.OrdinalIgnoreCase)>=0) return scene;
                }
            return root.relationshipScenes != null && root.relationshipScenes.Length>0 ? root.relationshipScenes[0] : null;
        }

        public static string FacilityStatePath(string buildingId,string state)
        {
            var facility=Facility(buildingId);
            if (facility?.states == null) return string.Empty;
            for (var i=0;i<facility.states.Length;i++)
                if (StringComparer.OrdinalIgnoreCase.Equals(facility.states[i].state,state)) return facility.states[i].resourcePath;
            return string.Empty;
        }

        public static Sprite Sprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (Sprites.TryGetValue(resourcePath,out var sprite)) return sprite;
            sprite=Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture=Resources.Load<Texture2D>(resourcePath);
                if (texture != null) sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f);
            }
            Sprites[resourcePath]=sprite; return sprite;
        }

        public static AudioClip Audio(string cueId)
        {
            var cue=AudioCue(cueId); if (cue == null || string.IsNullOrWhiteSpace(cue.resourcePath)) return null;
            if (Clips.TryGetValue(cue.resourcePath,out var clip)) return clip;
            clip=Resources.Load<AudioClip>(cue.resourcePath); Clips[cue.resourcePath]=clip; return clip;
        }

        public static Image AddImage(Transform parent,string name,string resourcePath,float preferredHeight)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(LayoutElement)); go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>(); image.sprite=Sprite(resourcePath); image.preserveAspect=true; image.color=Color.white;
            var layout=go.GetComponent<LayoutElement>(); layout.preferredHeight=preferredHeight; layout.minHeight=Math.Min(96f,preferredHeight); layout.flexibleWidth=1f;
            return image;
        }

        private static Dictionary<string,T> Index<T>(T[] values,Func<T,string> id) where T:class
        {
            var result=new Dictionary<string,T>(StringComparer.Ordinal);
            if (values != null) for (var i=0;i<values.Length;i++) if (values[i] != null) result[id(values[i])]=values[i];
            return result;
        }
        private static T Find<T>(Dictionary<string,T> values,string id) where T:class => values != null && values.TryGetValue(id ?? string.Empty,out var value) ? value : null;
    }
}
