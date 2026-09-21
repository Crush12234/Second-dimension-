using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017E
{
    [Serializable] public sealed class GuildCityOpeningExperienceRoot017E
    {
        public string contentVersion;
        public GuildCityContractPresentation017E[] contracts;
        public GuildCityNodePresentation017E[] nodes;
        public GuildCityEventPresentation017E[] events;
        public GuildCityBuildingPresentation017E[] buildings;
        public GuildCityDistrictPresentation017E[] districts;
        public GuildCityTutorialBeat017E[] tutorialBeats;
        public string cityMapResourcePath;
        public GuildCityBoardMapPath017E[] boardMaps;
    }

    [Serializable] public sealed class GuildCityContractPresentation017E
    {
        public string id; public string rank; public int estimatedMinutes; public string pressure;
        public string tone; public string briefing; public string failureConsequence; public string retreatConsequence;
        public string[] equipmentPossibilities; public string cityUnlock; public string sealResourcePath; public string tutorialFocus;
    }
    [Serializable] public sealed class GuildCityNodePresentation017E
    {
        public string boardId; public string nodeId; public string displayName; public string kind; public string summary;
        public string risk; public string recommendedSkill; public string iconResourcePath; public string routeCostSummary; public string battleModifier;
    }
    [Serializable] public sealed class GuildCityEventPresentation017E
    {
        public string id; public string title; public string sceneText; public string[] eligibleSkills; public string exceptionalText;
        public string fullSuccessText; public string successWithCostText; public string setbackText; public string severeSetbackText;
        public string relationshipMemory; public string cityConsequence; public string iconResourcePath;
    }
    [Serializable] public sealed class GuildCityBuildingPresentation017E
    {
        public string id; public string displayName; public string districtId; public string shortDescription;
        public string iconResourcePath; public string districtIconResourcePath; public string[] levelEffects;
        public string[] specializationOptions; public string[] staffRoles; public string[] adjacencyBuildingIds;
    }
    [Serializable] public sealed class GuildCityDistrictPresentation017E
    { public string id; public string name; public string identity; public string iconResourcePath; }
    [Serializable] public sealed class GuildCityTutorialBeat017E
    { public string id; public string title; public string instruction; public string completion; }
    [Serializable] public sealed class GuildCityBoardMapPath017E { public string boardId; public string resourcePath; }

    public static class GuildCityOpeningExperienceRegistry017E
    {
        private const string ResourcePath = "SecondDimension/GuildCity017E/Data/OPENING_EXPERIENCE_017E";
        private static GuildCityOpeningExperienceRoot017E _root;
        private static Dictionary<string,GuildCityContractPresentation017E> _contracts;
        private static Dictionary<string,GuildCityNodePresentation017E> _nodes;
        private static Dictionary<string,GuildCityEventPresentation017E> _events;
        private static Dictionary<string,GuildCityBuildingPresentation017E> _buildings;
        private static Dictionary<string,GuildCityDistrictPresentation017E> _districts;
        private static Dictionary<string,string> _boardMaps;
        private static readonly Dictionary<string,Sprite> Sprites = new Dictionary<string,Sprite>(StringComparer.Ordinal);

        public static GuildCityOpeningExperienceRoot017E Load()
        {
            if (_root != null) return _root;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing Guild City 017E experience data: " + ResourcePath);
            _root = JsonUtility.FromJson<GuildCityOpeningExperienceRoot017E>(asset.text);
            if (_root == null) throw new InvalidOperationException("Guild City 017E experience data did not parse.");
            _contracts = Index(_root.contracts, value => value.id);
            _nodes = Index(_root.nodes, value => value.boardId + "|" + value.nodeId);
            _events = Index(_root.events, value => value.id);
            _buildings = Index(_root.buildings, value => value.id);
            _districts = Index(_root.districts, value => value.id);
            _boardMaps = new Dictionary<string,string>(StringComparer.Ordinal);
            if (_root.boardMaps != null)
                for (var i=0;i<_root.boardMaps.Length;i++)
                    if (_root.boardMaps[i] != null && !string.IsNullOrWhiteSpace(_root.boardMaps[i].boardId))
                        _boardMaps[_root.boardMaps[i].boardId] = _root.boardMaps[i].resourcePath;
            return _root;
        }

        public static GuildCityContractPresentation017E Contract(string id) { Load(); return Find(_contracts,id); }
        public static GuildCityNodePresentation017E Node(string boardId,string nodeId)
        {
            Load();
            var firstHour071 = StringComparer.Ordinal.Equals(boardId,"BOARD_BELL_BENEATH_GATE_071");
            var value = Find(_nodes,PresentationBoardId(boardId)+"|"+nodeId);
            return firstHour071 ? ProjectFirstHourNode071(value,nodeId) : value;
        }
        public static GuildCityEventPresentation017E Event(string id) { Load(); return Find(_events,id); }
        public static GuildCityBuildingPresentation017E Building(string id) { Load(); return Find(_buildings,id); }
        public static GuildCityDistrictPresentation017E District(string id) { Load(); return Find(_districts,id); }
        public static string CityMapPath { get { return Load().cityMapResourcePath; } }
        public static string BoardMapPath(string boardId) { Load(); boardId=PresentationBoardId(boardId); return _boardMaps != null && _boardMaps.TryGetValue(boardId ?? string.Empty,out var path) ? path : string.Empty; }

        private static string PresentationBoardId(string boardId) =>
            StringComparer.Ordinal.Equals(boardId,"BOARD_BELL_BENEATH_GATE_069") ||
            StringComparer.Ordinal.Equals(boardId,"BOARD_BELL_BENEATH_GATE_071")
                ? "BOARD_BELL_BENEATH_GATE"
                : boardId;

        private static GuildCityNodePresentation017E ProjectFirstHourNode071(
            GuildCityNodePresentation017E source,
            string nodeId)
        {
            if (source == null) return null;
            var displayName = source.displayName;
            var kind = source.kind;
            var summary = source.summary;
            var risk = source.risk;
            switch (nodeId)
            {
                case "N01":
                    kind = "ENCOUNTER";
                    risk = "High";
                    break;
                case "N06":
                    kind = "ENCOUNTER";
                    risk = "High";
                    break;
                case "N13":
                    kind = "MAIN_OBJECTIVE";
                    risk = "High";
                    break;
            }
            return new GuildCityNodePresentation017E
            {
                boardId = "BOARD_BELL_BENEATH_GATE_071",
                nodeId = source.nodeId,
                displayName = displayName,
                kind = kind,
                summary = summary,
                risk = risk,
                recommendedSkill = source.recommendedSkill,
                iconResourcePath = source.iconResourcePath,
                routeCostSummary = source.routeCostSummary,
                battleModifier = source.battleModifier
            };
        }

        public static Sprite Sprite(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (Sprites.TryGetValue(resourcePath,out var sprite)) return sprite;
            sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(resourcePath);
                if (texture != null)
                {
                    sprite = UnityEngine.Sprite.Create(
                        texture,
                        new Rect(0f,0f,texture.width,texture.height),
                        new Vector2(0.5f,0.5f),
                        100f);
                    // Several production environment plates intentionally remain
                    // Texture2D assets. Preserve the real loaded asset identity on
                    // their runtime Sprite so presentation evidence and diagnostics
                    // can distinguish the Hall, route, road, and Gatehouse art.
                    sprite.name = texture.name;
                }
            }
            Sprites[resourcePath] = sprite;
            return sprite;
        }

        public static Image AddImage(Transform parent,string name,string resourcePath,float preferredHeight,bool preserveAspect=true)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image),typeof(LayoutElement));
            go.transform.SetParent(parent,false);
            var image = go.GetComponent<Image>(); image.sprite = Sprite(resourcePath); image.color = Color.white; image.preserveAspect = preserveAspect;
            var layout = go.GetComponent<LayoutElement>(); layout.preferredHeight = preferredHeight; layout.minHeight = Math.Min(96f,preferredHeight); layout.flexibleWidth = 1f;
            return image;
        }

        private static Dictionary<string,T> Index<T>(T[] values,Func<T,string> id) where T:class
        {
            var result = new Dictionary<string,T>(StringComparer.Ordinal);
            if (values != null) for (var i=0;i<values.Length;i++) if (values[i] != null) result[id(values[i])] = values[i];
            return result;
        }
        private static T Find<T>(Dictionary<string,T> values,string id) where T:class =>
            values != null && values.TryGetValue(id ?? string.Empty,out var value) ? value : null;
    }
}
