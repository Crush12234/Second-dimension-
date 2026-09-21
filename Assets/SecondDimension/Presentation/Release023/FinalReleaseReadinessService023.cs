using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign021;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.Campaign023;

namespace SecondDimension.Presentation.Release023
{
    [Serializable] public sealed class FinalReleaseContentSnapshot023
    {
        public bool IsAvailable; public string Error;
        public int CityBuildings; public int DefenseProfiles; public int CanonEvents; public int SignatureRecruits;
        public int ActiveCampaignChapters; public int FutureQuests; public int FutureMaps; public int FutureFortresses; public int FutureEncounters; public int FutureBosses;
        public int WorldThemes; public int EnemyPresentations; public int BossPresentations; public int RecruitOrigins; public int LootEntries; public int Materials;
        public int Repeatables; public int Crises; public int AudioCues; public int ArtBindings; public int WeaponTracks; public int WeaponRecipes; public int ArmorRecipes;
        public int AdvancedClasses; public int CertificationPaths; public int AbyssFloors; public int AbyssOperations; public int AbyssEvents; public int AbyssBosses;
        public int ArtifactBases; public int InvocationAffixes; public int SummonEchoes; public int GreatCovenants; public int WorldGateBoards; public int WorldGateNodes; public int WorldGateTravelRoutes; public int WorldGateStandingSystems; public int WorldGateRecruitUnlockSets; public int AlliedUnionCapacity; public int EnemyUnionCapacity;
        public string[] SummaryLines;
    }

    public static class FinalReleaseReadinessService023
    {
        public static FinalReleaseContentSnapshot023 BuildSnapshot()
        {
            try
            {
                var contentRoot = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
                var city = GuildCityContent017D.LoadFromDirectory(Path.Combine(contentRoot, "GUILD_CITY_017D"));
                var strategic = GuildCityStrategicContent017H.LoadFromDirectory(Path.Combine(contentRoot, "GUILD_CITY_017H"));
                var campaign019 = CampaignRegistry019.LoadFromResources();
                var campaign020 = CampaignRegistry020.LoadFromResources();
                var campaign021 = CampaignPresentationRegistry021.LoadFromResources();
                var campaign022 = CampaignRegistry022.LoadFromResources();
                var campaign023 = CampaignRegistry023.LoadFromResources();
                var futureRoot = Path.Combine(contentRoot, "GUILD_CITY_018B");
                var artAsset = Resources.Load<TextAsset>("SecondDimension/Art/Generated013/art_presentation_bindings_013");
                if (artAsset == null) throw new InvalidOperationException("The 240-Art presentation registry is missing.");
                var artBindings = JObject.Parse(artAsset.text)["bindings"] as JArray;
                var signatures = ReadArrayCount(Path.Combine(contentRoot, "RECRUIT_AUTOGEN_010", "DATA", "SIGNATURE_VISUAL_RECIPES_300_010.json"), "recipes");
                var snapshot = new FinalReleaseContentSnapshot023
                {
                    IsAvailable = true,
                    Error = string.Empty,
                    CityBuildings = city.Buildings.Count,
                    DefenseProfiles = strategic.Profiles.Count,
                    CanonEvents = strategic.CanonEvents.Count,
                    SignatureRecruits = signatures,
                    ActiveCampaignChapters = campaign020.Blueprints.Count,
                    FutureQuests = ReadArrayCount(Path.Combine(futureRoot, "WORLD_CAMPAIGN_QUESTS_018B.json"), "Quests"),
                    FutureMaps = ReadArrayCount(Path.Combine(futureRoot, "WORLD_CAMPAIGN_MAPS_018B.json"), "Maps"),
                    FutureFortresses = ReadArrayCount(Path.Combine(futureRoot, "FORTRESS_OPERATIONS_018B.json"), "Operations"),
                    FutureEncounters = ReadArrayCount(Path.Combine(futureRoot, "WORLD_ENCOUNTER_PROFILES_018B.json"), "Encounters"),
                    FutureBosses = ReadArrayCount(Path.Combine(futureRoot, "WORLD_BOSS_PROFILES_018B.json"), "Bosses"),
                    WorldThemes = campaign021.Worlds.Count,
                    EnemyPresentations = campaign021.Enemies.Count,
                    BossPresentations = campaign021.Bosses.Count,
                    RecruitOrigins = campaign021.Recruits.Count,
                    LootEntries = campaign021.Loot.Count,
                    Materials = campaign020.Materials.Count,
                    Repeatables = campaign020.Repeatables.Count,
                    Crises = campaign020.Crises.Count,
                    AudioCues = campaign021.Audio.Count,
                    ArtBindings = artBindings == null ? 0 : artBindings.Count,
                    WeaponTracks = campaign022.WeaponTracks.Count,
                    WeaponRecipes = campaign022.WeaponRecipes.Count,
                    ArmorRecipes = campaign022.ArmorRecipes.Count,
                    AdvancedClasses = campaign022.Classes.Count,
                    CertificationPaths = campaign022.CertificationPaths.Count,
                    AbyssFloors = campaign022.Floors.Count,
                    AbyssOperations = campaign022.AbyssOperations.Count,
                    AbyssEvents = ReadResourceArrayCount("SecondDimension/Campaign022/Data/AbyssEvents022", "events"),
                    AbyssBosses = ReadResourceArrayCount("SecondDimension/Campaign022/Data/AbyssBosses022", "bosses"),
                    ArtifactBases = campaign022.ArtifactBases.Count,
                    InvocationAffixes = campaign022.Affixes.Count,
                    SummonEchoes = campaign022.Echoes.Count,
                    GreatCovenants = campaign022.Covenants.Count,
                    WorldGateBoards = campaign023.Boards.Count,
                    WorldGateNodes = campaign023.Boards.Values.Sum(value => value.nodes == null ? 0 : value.nodes.Length),
                    WorldGateTravelRoutes = campaign023.Travel.Count,
                    WorldGateStandingSystems = campaign023.Standing.Count,
                    WorldGateRecruitUnlockSets = campaign023.RecruitUnlocks.Count,
                    AlliedUnionCapacity = campaign020.Crises.Values.Max(value => value.maximumAlliedUnions),
                    EnemyUnionCapacity = campaign020.Crises.Values.Max(value => value.maximumEnemyUnions)
                };
                snapshot.SummaryLines = new[]
                {
                    "Guild/City: " + snapshot.CityBuildings + " buildings, " + snapshot.DefenseProfiles + " defenses, " + snapshot.CanonEvents + " canon events",
                    "Roster/Battle: " + snapshot.SignatureRecruits + " signature recipes, " + snapshot.ArtBindings + " Art bindings, " + snapshot.AlliedUnionCapacity + "v" + snapshot.EnemyUnionCapacity + " Union capacity",
                    "Active Campaign: " + snapshot.ActiveCampaignChapters + " playable chapters, " + snapshot.WorldGateBoards + " expedition boards, " + snapshot.WorldGateNodes + " committed nodes",
                    "Future Authority: " + snapshot.FutureQuests + " quests, " + snapshot.FutureMaps + " maps, " + snapshot.FutureFortresses + " fortresses, " + snapshot.FutureEncounters + " encounters",
                    "Progression/Endgame: " + snapshot.WeaponRecipes + " weapon recipes, " + snapshot.ArmorRecipes + " armor recipes, " + snapshot.AbyssFloors + " Abyss floors, " + snapshot.GreatCovenants + " Covenants"
                };
                return snapshot;
            }
            catch (Exception exception)
            {
                return new FinalReleaseContentSnapshot023 { IsAvailable = false, Error = exception.ToString(), SummaryLines = Array.Empty<string>() };
            }
        }

        static int ReadArrayCount(string path, string property)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);
            var array = JObject.Parse(File.ReadAllText(path))[property] as JArray;
            if (array == null) throw new InvalidOperationException("Missing array " + property + " in " + path);
            return array.Count;
        }

        static int ReadResourceArrayCount(string resourcePath, string property)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new InvalidOperationException("Missing resource: " + resourcePath);
            var array = JObject.Parse(asset.text)[property] as JArray;
            if (array == null) throw new InvalidOperationException("Missing array " + property + " in " + resourcePath);
            return array.Count;
        }
    }
}
