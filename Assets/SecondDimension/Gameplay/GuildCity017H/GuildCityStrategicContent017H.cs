using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SecondDimension.Gameplay.GuildCity017H
{
    [Serializable] public sealed class BuildingContributionValues017H
    {
        public int PersonalXpBp; public int CombatArtMasteryBp; public int MysticMasteryBp; public int RestorationMasteryBp; public int WardingMasteryBp;
        public int UnionDisciplineBp; public int GuildTreasuryXpBp; public int CivicHallXpBp; public int StaffDutyXpFlat; public int RelationshipMemoryBp;
        public int StartingApFlat; public int StartingCohesionFlat; public int StartingMpFlat; public int EnemyCohesionDamageFlat; public int SuppliesFlat;
        public int ScoutingFlat; public int DefensePowerFlat; public int BarrierIntegrityFlat; public int ResupplyFlat; public int ReinforcementReadinessFlat;
        public int GuardPreparationFlat; public int UrgencyReductionFlat;
    }
    [Serializable] public sealed class BuildingContributionDefinition017H { public string BuildingId; public string Summary; public BuildingContributionValues017H PerLevel; public int MaximumContributionLevel; }
    [Serializable] internal sealed class BuildingContributionRoot017H { public string ContentVersion; public int BasisPointCapPerCategory; public int FlatContributionCap; public string StackingRule; public BuildingContributionDefinition017H[] Buildings; }
    [Serializable] public sealed class DefenseLaneDefinition017H { public string Id; public string DisplayName; public string Summary; public string[] PreferredDefenseTags; }
    [Serializable] public sealed class DefenseWaveDefinition017H { public int WaveIndex; public string DisplayName; public string[] LaneIds; public int EnemyPower; public int SupportResolutionThreshold; public bool DecisiveBattle; public int EnemyUnionCount; public string Objective; public int RewardGuildXp; public int RewardCivicHallXp; public int DefenseMasteryXp; public string StableWaveId(string profileId)=>profileId+"_WAVE_"+WaveIndex.ToString("00"); }
    [Serializable] public sealed class DefenseProfileDefinition017H { public string Id; public string DisplayName; public string Classification; public string RequiredStoryGate; public bool AvailableInOpening; public bool FutureLocked; public int Difficulty; public string[] LaneIds; public DefenseWaveDefinition017H[] Waves; public string Summary; public DefenseWaveDefinition017H WaveAt(int index){if(Waves==null||index<0||index>=Waves.Length)throw new IndexOutOfRangeException("Defense wave is outside the profile.");return Waves[index];} }
    [Serializable] internal sealed class DefenseRoot017H { public string ContentVersion; public string ResolutionModel; public bool CreatesSecondCombatEngine; public int MaximumAlliedUnions; public int MaximumEnemyUnions; public DefenseLaneDefinition017H[] Lanes; public DefenseProfileDefinition017H[] Profiles; }
    [Serializable] public sealed class CanonEventDefinition017H { public string Id; public string DisplayName; public string Classification; public string RequiredStoryGate; public bool AvailableInOpening; public bool OutcomeLocked; public string DefenseProfileId; public string Summary; public string[] SourceAuthority; public string[] Consequences; public bool CanRewritePublishedCanon; public bool RevealsProtectedSystemTruth; public bool CanCauseInvoluntaryDeparture; }
    [Serializable] internal sealed class CanonRoot017H { public string ContentVersion; public string[] Classifications; public bool SandboxEnabled; public CanonEventDefinition017H[] Events; }

    public sealed class GuildCityStrategicContent017H
    {
        private readonly Dictionary<string,BuildingContributionDefinition017H> _contributions;
        private readonly Dictionary<string,DefenseLaneDefinition017H> _lanes;
        private readonly Dictionary<string,DefenseProfileDefinition017H> _profiles;
        private readonly Dictionary<string,CanonEventDefinition017H> _canon;
        private GuildCityStrategicContent017H(BuildingContributionRoot017H contributions,DefenseRoot017H defense,CanonRoot017H canon)
        {
            BasisPointCap=contributions.BasisPointCapPerCategory;FlatCap=contributions.FlatContributionCap;
            _contributions=Index(contributions.Buildings,x=>x.BuildingId,"building contribution");
            _lanes=Index(defense.Lanes,x=>x.Id,"defense lane");_profiles=Index(defense.Profiles,x=>x.Id,"defense profile");_canon=Index(canon.Events,x=>x.Id,"canon event");
            if(_contributions.Count!=30)throw new InvalidDataException("017H requires exactly 30 building contribution definitions.");
            if(_profiles.Count!=12)throw new InvalidDataException("017H requires exactly 12 defense profiles.");
            if(_canon.Count!=36)throw new InvalidDataException("017H requires exactly 36 canon-event definitions.");
            if(defense.CreatesSecondCombatEngine)throw new InvalidDataException("City defense may not create a second combat engine.");
            foreach(var value in _canon.Values){if(value.CanRewritePublishedCanon||value.RevealsProtectedSystemTruth||value.CanCauseInvoluntaryDeparture)throw new InvalidDataException("Canon safety violation: "+value.Id);}
        }
        public int BasisPointCap{get;} public int FlatCap{get;}
        public IReadOnlyDictionary<string,BuildingContributionDefinition017H> Contributions=>_contributions;
        public IReadOnlyDictionary<string,DefenseLaneDefinition017H> Lanes=>_lanes;
        public IReadOnlyDictionary<string,DefenseProfileDefinition017H> Profiles=>_profiles;
        public IReadOnlyDictionary<string,CanonEventDefinition017H> CanonEvents=>_canon;
        public BuildingContributionDefinition017H Contribution(string id)=>_contributions[id]; public DefenseProfileDefinition017H DefenseProfile(string id)=>_profiles[id]; public CanonEventDefinition017H CanonEvent(string id)=>_canon[id];
        public static GuildCityStrategicContent017H LoadFromDirectory(string directory)
        {
            if(string.IsNullOrWhiteSpace(directory))throw new ArgumentException("017H content directory is required.",nameof(directory));
            return new GuildCityStrategicContent017H(Read<BuildingContributionRoot017H>(directory,"BUILDING_COMBAT_XP_CONTRIBUTIONS_017H.json"),Read<DefenseRoot017H>(directory,"CITY_DEFENSE_PROFILES_017H.json"),Read<CanonRoot017H>(directory,"CANON_EVENT_CATALOG_017H.json"));
        }
        private static T Read<T>(string directory,string file){var path=Path.Combine(directory,file);if(!File.Exists(path))throw new FileNotFoundException("017H authority file missing.",path);var value=JsonConvert.DeserializeObject<T>(File.ReadAllText(path));return value==null?throw new InvalidDataException("Empty 017H authority file: "+file):value;}
        private static Dictionary<string,T> Index<T>(IReadOnlyList<T> values,Func<T,string> id,string label){var result=new Dictionary<string,T>(StringComparer.Ordinal);if(values!=null)for(var i=0;i<values.Count;i++){var key=id(values[i]);if(string.IsNullOrWhiteSpace(key)||result.ContainsKey(key))throw new InvalidDataException("Invalid or duplicate "+label+": "+key);result.Add(key,values[i]);}return result;}
    }
}