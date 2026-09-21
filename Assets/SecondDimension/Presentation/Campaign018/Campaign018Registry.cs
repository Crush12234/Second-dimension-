using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SecondDimension.Presentation.Campaign018
{
    public sealed class CampaignRegistry018
    {
        public CampaignManifest018 Manifest { get; private set; }
        public IReadOnlyDictionary<string, WorldDefinition018> Worlds => _worlds;
        public IReadOnlyDictionary<string, CampaignArcDefinition018> Arcs => _arcs;
        public IReadOnlyDictionary<string, QuestChapterDefinition018> Chapters => _chapters;
        public IReadOnlyDictionary<string, WorldMapDefinition018> Maps => _maps;
        public IReadOnlyDictionary<string, FortressSiegeDefinition018> Sieges => _sieges;
        public IReadOnlyDictionary<string, BossEncounterDefinition018> Bosses => _bosses;

        readonly Dictionary<string, WorldDefinition018> _worlds = new Dictionary<string, WorldDefinition018>(StringComparer.Ordinal);
        readonly Dictionary<string, CampaignArcDefinition018> _arcs = new Dictionary<string, CampaignArcDefinition018>(StringComparer.Ordinal);
        readonly Dictionary<string, QuestChapterDefinition018> _chapters = new Dictionary<string, QuestChapterDefinition018>(StringComparer.Ordinal);
        readonly Dictionary<string, WorldMapDefinition018> _maps = new Dictionary<string, WorldMapDefinition018>(StringComparer.Ordinal);
        readonly Dictionary<string, FortressSiegeDefinition018> _sieges = new Dictionary<string, FortressSiegeDefinition018>(StringComparer.Ordinal);
        readonly Dictionary<string, BossEncounterDefinition018> _bosses = new Dictionary<string, BossEncounterDefinition018>(StringComparer.Ordinal);

        public static CampaignRegistry018 LoadFromResources()
        {
            var r = new CampaignRegistry018();
            r.Manifest = Load<CampaignManifest018>("SecondDimension/Campaign018/Data/CampaignManifest018");
            foreach (var x in Load<WorldDefinitionsFile018>("SecondDimension/Campaign018/Data/WorldDefinitions018").worlds ?? Array.Empty<WorldDefinition018>()) r._worlds.Add(x.id,x);
            foreach (var x in Load<CampaignArcsFile018>("SecondDimension/Campaign018/Data/CampaignArcs018").arcs ?? Array.Empty<CampaignArcDefinition018>()) r._arcs.Add(x.id,x);
            foreach (var x in Load<QuestChaptersFile018>("SecondDimension/Campaign018/Data/QuestChapters018").chapters ?? Array.Empty<QuestChapterDefinition018>()) r._chapters.Add(x.id,x);
            foreach (var x in Load<WorldMapsFile018>("SecondDimension/Campaign018/Data/WorldMaps018").maps ?? Array.Empty<WorldMapDefinition018>()) r._maps.Add(x.id,x);
            foreach (var x in Load<FortressSiegesFile018>("SecondDimension/Campaign018/Data/FortressSieges018").sieges ?? Array.Empty<FortressSiegeDefinition018>()) r._sieges.Add(x.id,x);
            foreach (var x in Load<BossEncountersFile018>("SecondDimension/Campaign018/Data/BossEncounters018").bosses ?? Array.Empty<BossEncounterDefinition018>()) r._bosses.Add(x.id,x);
            r.ValidateOrThrow();
            return r;
        }

        static T Load<T>(string path)
        {
            var asset = Resources.Load<TextAsset>(path);
            if (asset == null) throw new InvalidOperationException("Missing Campaign 018 resource: " + path);
            var value = JsonUtility.FromJson<T>(asset.text);
            if (value == null) throw new InvalidOperationException("Invalid Campaign 018 JSON: " + path);
            return value;
        }

        public void ValidateOrThrow()
        {
            if (Manifest == null || string.IsNullOrEmpty(Manifest.contentVersion)) throw new InvalidOperationException("Campaign manifest missing");
            if (_worlds.Count != 7) throw new InvalidOperationException("Campaign 018 requires seven race worlds");
            if (!_worlds.TryGetValue("WORLD_GOBLIN_001", out var goblin) || goblin.timeLaw.IndexOf("1 Second Dimension day equals 1 Goblin World month", StringComparison.Ordinal) < 0) throw new InvalidOperationException("Goblin time law changed");
            foreach (var arc in _arcs.Values)
            {
                if (!arc.noConquestChecklist) throw new InvalidOperationException("Conquest-only arc forbidden: " + arc.id);
                foreach (var chapterId in arc.chapterIds ?? Array.Empty<string>()) if (!_chapters.ContainsKey(chapterId)) throw new InvalidOperationException("Missing chapter " + chapterId);
            }
            foreach (var c in _chapters.Values)
            {
                if (c.individualArtSelection) throw new InvalidOperationException("Individual Art selection forbidden: " + c.id);
                if (!c.nonCombatBeatRequired || !c.relationshipMemoryRequired || !c.cityConsequenceRequired) throw new InvalidOperationException("World chapter lacks civilization content: " + c.id);
                foreach (var mapId in c.mapIds ?? Array.Empty<string>()) if (mapId.StartsWith("MAP018_", StringComparison.Ordinal) && !_maps.ContainsKey(mapId)) throw new InvalidOperationException("Missing map " + mapId);
                if (!string.IsNullOrEmpty(c.siegeProfileId) && !_sieges.ContainsKey(c.siegeProfileId) && c.siegeProfileId != "SIEGE_SKYHOME_GOBLIN_WAR") throw new InvalidOperationException("Missing siege " + c.siegeProfileId);
            }
            foreach (var s in _sieges.Values)
            {
                if (!s.usesStrategicDefense017H || !s.usesCertifiedUnionBattle || s.createsSecondCombatResolver) throw new InvalidOperationException("Fortress operation bypasses certified systems: " + s.id);
                if (s.maxAlliedUnions > 10 || s.maxEnemyUnions > 10) throw new InvalidOperationException("Union capacity exceeded");
            }
        }
    }
}
