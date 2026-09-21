using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.TitanTrials160
{
    // Native content/commitment component. This does not enable a battle, mint a
    // reward or substitute ordinary attacks for an unbound R45 mechanic.
    public sealed class TitanTrialCatalog160
    {
        public const string Policy = "TITAN_TRIAL160_FIXED_CANDIDATE_V1";
        readonly IReadOnlyList<TitanTrialDefinition160> _trials;
        public IReadOnlyList<TitanTrialDefinition160> Trials => _trials;
        public string Identity { get; }
        TitanTrialCatalog160(IReadOnlyList<TitanTrialDefinition160> trials)
        {
            if(trials==null||trials.Count!=12||!trials.Select(t=>t.Slot).SequenceEqual(Enumerable.Range(1,12)))
                throw new ArgumentException("TITAN160_COMPLETE_TWELVE_REQUIRED");
            if(trials.Select(t=>t.HeroId).Distinct(StringComparer.Ordinal).Count()!=12||
               trials.Select(t=>t.SourceFamilyId).Distinct(StringComparer.Ordinal).Count()!=12)
                throw new ArgumentException("TITAN160_DUPLICATE_IDENTITY");
            _trials=Array.AsReadOnly(trials.ToArray());
            Identity=CanonicalJson.Sha256Hex(new{Policy,Trials=_trials});
        }
        public TitanTrialDefinition160 Trial(int slot)
        {if(slot<1||slot>12)throw new ArgumentOutOfRangeException(nameof(slot));return _trials[slot-1];}
        public static TitanTrialCatalog160 LoadFromDirectory(string directory)=>FromJson(
            File.ReadAllText(Path.Combine(directory,"TITAN_ENCOUNTER_BLUEPRINTS.json")),
            File.ReadAllText(Path.Combine(directory,"TITAN_CHARACTER_BLUEPRINTS.json")),
            File.ReadAllText(Path.Combine(directory,"TITAN_ACTION_RECIPES.json")),
            File.ReadAllText(Path.Combine(directory,"TITAN_EFFECT_RECIPES.json")));
        public static TitanTrialCatalog160 FromJson(string encounters,string heroes,string actions,string effects)
        {
            var encountersRoot=JObject.Parse(encounters);var heroRoot=JObject.Parse(heroes);
            var actionRoot=JObject.Parse(actions);var effectRoot=JObject.Parse(effects);
            var e=RequireArray(encountersRoot,"variants",12);var h=RequireArray(heroRoot,"characters",13);
            var a=RequireArray(actionRoot,"encounters",12);var fx=RequireArray(effectRoot,"recipes",24);
            var list=new List<TitanTrialDefinition160>();
            for(int slot=1;slot<=12;slot++)
            {
                string id="TITAN_TRIAL_"+slot.ToString("000"),hero="HERO_TITAN_"+slot.ToString("000"),gold="GOLD_TITAN_"+slot.ToString("000");
                var blueprint=One(e,"logical_encounter_blueprint_id",id);
                var character=One(h,"logical_hero_id",hero);var recipe=One(a,"id",id);
                var goldRecipe=One(fx,"id",gold);
                if((int)blueprint["slot"]!=slot||(int)recipe["slot"]!=slot||
                   (string)blueprint["reward_hero_id"]!=hero||(string)blueprint["gold_art_id"]!=gold||
                   (string)recipe["reward_hero_id"]!=hero||(string)goldRecipe["logical_hero_id"]!=hero)
                    throw new ArgumentException("TITAN160_SOURCE_MAPPING_MISMATCH");
                var parsedActions=((JArray)recipe["actions"]).Select(x=>new TitanActionDefinition160((JObject)x)).ToArray();
                var phases=((JArray)recipe["phases"]).Select(x=>new TitanPhaseDefinition160((int)x["enter_at_or_below_hp_bp"],
                    ((JArray)x["cycle"]).Values<string>().ToArray())).ToArray();
                list.Add(new TitanTrialDefinition160(slot,id,(string)blueprint["display_name"],(string)blueprint["source_family_id"],
                    hero,(string)character["display_name"],gold,parsedActions,phases,
                    character.ToString(Formatting.None),goldRecipe.ToString(Formatting.None),recipe.ToString(Formatting.None)));
            }
            return new TitanTrialCatalog160(list.AsReadOnly());
        }
        static JArray RequireArray(JObject root,string name,int count)
        {var result=root[name] as JArray;if(result==null||result.Count!=count)throw new ArgumentException("TITAN160_SOURCE_COUNT:"+name);return result;}
        static JObject One(JArray array,string key,string value)
        {var matches=array.OfType<JObject>().Where(x=>(string)x[key]==value).ToArray();if(matches.Length!=1)throw new ArgumentException("TITAN160_SOURCE_ID:"+value);return matches[0];}
    }
    public sealed class TitanTrialDefinition160
    {
        public int Slot{get;} public string Id{get;} public string Name{get;} public string SourceFamilyId{get;}
        public string HeroId{get;} public string HeroName{get;} public string GoldArtId{get;}
        public IReadOnlyList<TitanActionDefinition160> Actions{get;} public IReadOnlyList<TitanPhaseDefinition160> Phases{get;}
        // Complete immutable recipe bytes keep unsupported secondary effects visible.
        // Binding code must never infer readiness from a partial action projection.
        public string CharacterBlueprintJson{get;} public string GoldRecipeJson{get;} public string BossRecipeJson{get;}
        public string RecipeIdentity{get;}
        internal TitanTrialDefinition160(int slot,string id,string name,string source,string hero,string heroName,string gold,
            IReadOnlyList<TitanActionDefinition160> actions,IReadOnlyList<TitanPhaseDefinition160> phases,string character,string goldRecipe,string bossRecipe)
        {
            Slot=slot;Id=id;Name=name;SourceFamilyId=source;HeroId=hero;HeroName=heroName;GoldArtId=gold;
            Actions=Array.AsReadOnly(actions.ToArray());Phases=Array.AsReadOnly(phases.ToArray());
            CharacterBlueprintJson=character;GoldRecipeJson=goldRecipe;BossRecipeJson=bossRecipe;
            if(Phases.Count!=3||!Phases.Select(p=>p.ThresholdBasisPoints).SequenceEqual(new[]{10000,6500,3000})||Phases.Any(p=>p.Cycle.Count!=4)||
               Actions.Select(x=>x.Key).Distinct(StringComparer.Ordinal).Count()!=Actions.Count)
                throw new ArgumentException("TITAN160_RECIPE_TOPOLOGY");
            foreach(var phase in Phases)foreach(var key in phase.Cycle)Action(key);
            foreach(var action in Actions.Where(x=>x.Kind=="WIND_UP"))
            {
                var impact=Action(action.Announces);
                if(impact.Kind!="IMPACT"||impact.RequiresWarning!=action.Key||action.MinimumForecastOpportunities<1)
                    throw new ArgumentException("TITAN160_WARNING_BINDING");
            }
            RecipeIdentity=CanonicalJson.Sha256Hex(new{TitanTrialCatalog160.Policy,Id,SourceFamilyId,CharacterBlueprintJson,GoldRecipeJson,BossRecipeJson});
        }
        public TitanActionDefinition160 Action(string key)=>Actions.Single(x=>x.Key==key);
        public TitanFixedStats160 Stats(int tier)
        {
            if(tier<1)throw new ArgumentOutOfRangeException(nameof(tier));
            // Explicit isolated tuning candidate; absolute slot/tier inputs only.
            // No roster, current level, gear, Treasury or success history is read.
            long hp=24000L+6000L*Slot,attack=100L+12L*Slot,magic=90L+14L*Slot;
            checked
            {
                hp=(hp*(10000L+1800L*(tier-1))+9999)/10000;
                attack=(attack*(10000L+1200L*(tier-1))+9999)/10000;
                magic=(magic*(10000L+1200L*(tier-1))+9999)/10000;
                if(hp>int.MaxValue||attack>int.MaxValue||magic>int.MaxValue)
                    throw new OverflowException("TITAN160_NATIVE_STAT_RANGE");
                return new TitanFixedStats160((int)hp,(int)attack,(int)magic);
            }
        }
    }
    public sealed class TitanFixedStats160
    {public int MaximumHp{get;}public int Attack{get;}public int MagicAttack{get;}
     internal TitanFixedStats160(int hp,int attack,int magic){MaximumHp=hp;Attack=attack;MagicAttack=magic;}}
    public sealed class TitanPhaseDefinition160
    {public int ThresholdBasisPoints{get;}public IReadOnlyList<string> Cycle{get;}
     internal TitanPhaseDefinition160(int threshold,IReadOnlyList<string> cycle){ThresholdBasisPoints=threshold;Cycle=Array.AsReadOnly(cycle.ToArray());}}
    public sealed class TitanActionDefinition160
    {
        public string Key{get;}public string Kind{get;}public string Channel{get;}public int TotalBudgetBasisPoints{get;}
        public string Targets{get;}public string FreezeTargets{get;}public string Announces{get;}public string RequiresWarning{get;}
        public int MinimumForecastOpportunities{get;}public string Special{get;}public int MaximumSpecialEvents{get;}public string PlayerCopy{get;}
        public string CompleteRecipeJson{get;}
        internal TitanActionDefinition160(JObject value)
        {
            Key=(string)value["key"];Kind=(string)value["type"];Channel=(string)value["channel"]??"";
            TotalBudgetBasisPoints=(int?)value["total_budget_bp"]??0;Targets=(string)value["targets"]??"";
            FreezeTargets=(string)value["freeze_targets"]??"";Announces=(string)value["announces"]??"";
            RequiresWarning=(string)value["requires_warning"]??"";MinimumForecastOpportunities=(int?)value["minimum_player_forecast_opportunities_before_impact"]??0;
            Special=(string)value["special"]??"";MaximumSpecialEvents=(int?)value["maximum_special_events"]??0;
            PlayerCopy=(string)value["player_copy"]??(string)value["name"]??Key;
            CompleteRecipeJson=value.ToString(Formatting.None);
            if(string.IsNullOrWhiteSpace(Key)||!(new[]{"DAMAGE","WIND_UP","IMPACT","RECOVERY"}).Contains(Kind)||TotalBudgetBasisPoints<0)
                throw new ArgumentException("TITAN160_ACTION_INVALID");
        }
    }
}
