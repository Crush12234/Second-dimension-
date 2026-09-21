using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.State;
namespace SecondDimension.Gameplay.TitanHeroes161
{
    // Exact R43 seven-field allocations and R45 effect recipes. The original ten roster is unchanged.
    public static class TitanHeroCatalog161
    {
        public const int SharedAp = 6, PersonalMp = 10;
        public const string EidranId = "HERO_TITAN_013";
        static readonly SssTenV4HeroDefinition090[] Heroes = {
            new SssTenV4HeroDefinition090("HERO_TITAN_001","Torren Gatebreaker","physical finisher","CLASS_WARRIOR","HAMMER","WARDING",21,8,25,5,19,11,11,"","Hammer","WF03_AXE","Worldsplitter"),
            new SssTenV4HeroDefinition090("HERO_TITAN_002","Avelis Dawnguard","guard specialist","CLASS_GUARDIAN","GUARD","WARDING",26,12,9,8,23,17,5,"","Shield","WF07_SHIELD","Dawnwall Covenant"),
            new SssTenV4HeroDefinition090("HERO_TITAN_003","Nymara Tideweaver","healing/cleanse","CLASS_PRIEST","RESTORATION","MYSTIC_WATER",16,23,5,25,8,17,6,"","Staff","WF09_STAFF","Ocean of Renewal"),
            new SssTenV4HeroDefinition090("HERO_TITAN_004","Zareth Stormfang","hybrid single-target","CLASS_WARRIOR","SPEAR","MYSTIC_STORM",16,16,20,22,10,8,8,"","Spear","WF04_SPEAR_POLEARM","Heavenrend Spear"),
            new SssTenV4HeroDefinition090("HERO_TITAN_005","Malrec Oathbound","revive support","CLASS_PRIEST","RESTORATION","WARDING",22,20,8,18,15,13,4,"","Staff","WF09_STAFF","Last Dawn"),
            new SssTenV4HeroDefinition090("HERO_TITAN_006","Dorran Ashforge","threat bruiser","CLASS_WARRIOR","AXE","MYSTIC_FLAME",23,9,25,8,20,9,6,"","Axe","WF03_AXE","Heart of the Inferno"),
            new SssTenV4HeroDefinition090("HERO_TITAN_007","Vesha Moonclaw","Union sweep","CLASS_ROGUE","DUAL_WEAPON","STEALTH",14,9,29,7,9,9,23,"","Twin Blades","WF06_DAGGER","Eclipse Hunt"),
            new SssTenV4HeroDefinition090("HERO_TITAN_008","Ilyra Frostveil","Mystic protection","CLASS_MAGE","MYSTIC_FROST","WARDING",17,21,5,24,12,17,4,"","Focus","WF10_CATALYST_FOCUS","Winter Ward"),
            new SssTenV4HeroDefinition090("HERO_TITAN_009","Serelis Sunbloom","emergency protection","CLASS_PRIEST","MYSTIC_NATURE","WARDING",21,18,5,22,14,16,4,"","Staff","WF09_STAFF","Worldroot Sanctuary"),
            new SssTenV4HeroDefinition090("HERO_TITAN_010","Theryn Starseer","wounded-target finisher","CLASS_MAGE","MYSTIC_AETHER","PRECISION",12,23,5,32,7,13,8,"","Focus","WF10_CATALYST_FOCUS","Falling Firmament"),
            new SssTenV4HeroDefinition090("HERO_TITAN_011","Rhovan Ironbanner","controlled frontline","CLASS_WARRIOR","TACTICAL_COMMAND","SPEAR",21,11,20,7,20,12,9,"","Spear-banner","WF04_SPEAR_POLEARM","Sovereign\u2019s Challenge"),
            new SssTenV4HeroDefinition090("HERO_TITAN_012","Elyndra Riftcrown","hybrid finisher","CLASS_WARRIOR","SWORD","MYSTIC_AETHER",17,18,20,25,8,7,5,"","Sword","WF01_SWORD","Final Horizon"),
            new SssTenV4HeroDefinition090("HERO_TITAN_013","Eidran, Keeper of the Twelve","invocation support","CLASS_MAGE","COMPANION","WARDING",15,23,5,27,9,15,6,"","Astrolabe staff","WF09_STAFF","Accord of the Twelve")
        };
        static readonly TitanHeroRecipe161[] Recipes = {
            new TitanHeroRecipe161("GOLD_TITAN_001","Worldsplitter",1,22000,"ONE_ENEMY_UNION","EQUAL",1,"ARMOR_BREAK","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_002","Dawnwall Covenant",2,21000,"ONE_ALLIED_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_003","Ocean of Renewal",3,19000,"ONE_ALLIED_UNION","MISSING_HP",1,"CLEANSE_ONE","ANY_DAMAGE",0,0,new string[]{"HEAL"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_004","Heavenrend Spear",4,21000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE","MYSTIC_DAMAGE"},new int[]{5000,5000}),
            new TitanHeroRecipe161("GOLD_TITAN_005","Last Dawn",5,10000,"ONE_FALLEN_ALLY","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"REVIVE_HP","BARRIER"},new int[]{8000,2000}),
            new TitanHeroRecipe161("GOLD_TITAN_006","Heart of the Inferno",6,19000,"ONE_ENEMY_UNION","EQUAL",1,"THREAT","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_007","Eclipse Hunt",7,24000,"ONE_ENEMY_UNION","EQUAL",3,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_008","Winter Ward",8,20000,"ONE_ALLIED_UNION","EQUAL",1,"NONE","MYSTIC_ONLY",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_009","Worldroot Sanctuary",9,23000,"ONE_ALLIED_UNION","MISSING_HP_PLUS_ONE",1,"NONE","ANY_DAMAGE",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_010","Falling Firmament",10,22000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",3500,2500,new string[]{"MYSTIC_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_011","Sovereign\u2019s Challenge",11,19000,"ONE_ENEMY_UNION","EQUAL",1,"SLOW","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("GOLD_TITAN_012","Final Horizon",12,24000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE","MYSTIC_DAMAGE"},new int[]{5000,5000}),
            new TitanHeroRecipe161("ECHO_TITAN_001","Sunken Idol Sentinel Echo",1,20000,"ONE_ALLIED_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_002","Coralback Siege Crab Echo",2,20000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_003","Bogvenom Hydra Echo",3,20000,"ONE_ALLIED_UNION","MISSING_HP",1,"CLEANSE_ONE","ANY_DAMAGE",0,0,new string[]{"HEAL"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_004","Tempest Roc Echo",4,20000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"MYSTIC_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_005","Sunfire Phoenix Echo",5,20000,"ONE_ALLIED_UNION","MISSING_HP",1,"NONE","ANY_DAMAGE",0,0,new string[]{"HEAL"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_006","Gearbanner Castellan Echo",6,20000,"ONE_ENEMY_UNION","EQUAL",1,"THREAT","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_007","Ossuary Wyvern Echo",7,20000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_008","Frosttusk Mammoth Echo",8,20000,"ONE_ALLIED_UNION","EQUAL",1,"NONE","MYSTIC_ONLY",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_009","Elder Rootlord Echo",9,20000,"ONE_ALLIED_UNION","MISSING_HP_PLUS_ONE",1,"NONE","ANY_DAMAGE",0,0,new string[]{"BARRIER"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_010","Prismwatch Oculus Echo",10,20000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"MYSTIC_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_011","Voidspore Slime Tyrant Echo",11,20000,"ONE_ENEMY_UNION","EQUAL",1,"SLOW","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE"},new int[]{10000}),
            new TitanHeroRecipe161("ECHO_TITAN_012","Emberjaw Crocodrake Echo",12,20000,"ONE_ENEMY_UNION","EQUAL",1,"NONE","ANY_DAMAGE",0,0,new string[]{"PHYSICAL_DAMAGE","MYSTIC_DAMAGE"},new int[]{5000,5000})
        };
        public static IReadOnlyList<SssTenV4HeroDefinition090> All => Array.AsReadOnly(Heroes);
        public static IReadOnlyList<TitanHeroRecipe161> AllRecipes => Array.AsReadOnly(Recipes);
        public static bool TryGet(string id,out SssTenV4HeroDefinition090 hero)
        {hero=Heroes.FirstOrDefault(x=>StringComparer.Ordinal.Equals(x.HeroId,id));return hero!=null;}
        public static string HeroId(int slot) {if(slot<1||slot>13)throw new ArgumentOutOfRangeException(nameof(slot));return "HERO_TITAN_"+slot.ToString("000");}
        public static int Slot(string id) {for(int i=0;i<Heroes.Length;i++)if(Heroes[i].HeroId==id)return i+1;return 0;}
        public static TitanHeroRecipe161 Recipe(string id)=>Recipes.Single(x=>x.Id==id);
        public static IReadOnlyList<M2ArtDefinition> Arts()
        {
            var result=new List<M2ArtDefinition>();
            foreach(var hero in Heroes)
            {
                bool mystic=hero.MagicWeight>hero.AttackWeight;
                for(int n=1;n<=3;n++)
                {
                    string discipline=n==2?"Guard":n==3&&hero.ClassId=="CLASS_PRIEST"?"Restoration":mystic?"Mystic":"Martial";
                    string effect=discipline=="Restoration"?"HEAL":discipline=="Guard"?"GUARD":"DAMAGE";
                    result.Add(new M2ArtDefinition(hero.HeroId+"_ART_"+n.ToString("00"),hero.DisplayName+ (n==2?" — Hold the Line":n==3?" — Trained Art":" — Strike"),
                        "TITAN_PERSONAL",discipline,Array.Empty<string>(),n==1?2:4,n==1?3:6,
                        new[]{discipline=="Restoration"?"HEAL":discipline=="Guard"?"DEFENSE":"OFFENSE"},
                        "Resolve through the existing ordinary Art command.",hero.HeroId+"_ATTACK",false,
                        treeId:n<3?hero.PrimaryDiscipline:hero.SecondaryDiscipline,nodeType:"ACTION",powerCoefficientPermille:n==1?1100:1350,
                        effectTags:new[]{effect},targetRule:discipline=="Restoration"?"SELF_OR_ALLY_UNION":discipline=="Guard"?"SELF":"ENEMY_UNION"));
                }
            }
            foreach(var r in Recipes)result.Add(new M2ArtDefinition(r.Id,r.Name,"TITAN_PERSONAL_GOLD",r.IsFriendly?"Restoration":"Mystic",
                Array.Empty<string>(),SharedAp,PersonalMp,new[]{"TITAN_GOLD"},"One personal activation per encounter.",r.Id,false,
                nodeType:"SIGNATURE",forecastAction:false,effectTags:new[]{"TITAN_GOLD_ADAPTER_ONLY"},targetRule:r.TargetScope));
            return result.AsReadOnly();
        }
    }
    public sealed class TitanHeroRecipe161
    {
        public string Id{get;}public string Name{get;}public int Slot{get;}public int BudgetBasisPoints{get;}
        public string TargetScope{get;}public string Allocation{get;}public int Bursts{get;}public string Secondary{get;}
        public string BarrierScope{get;}public int WoundedThreshold{get;}public int WoundedBonus{get;}
        public IReadOnlyList<string> Channels{get;}public IReadOnlyList<int> ChannelWeights{get;}
        public bool IsEcho=>Id.StartsWith("ECHO_",StringComparison.Ordinal);
        public bool IsFriendly=>TargetScope!="ONE_ENEMY_UNION";
        internal TitanHeroRecipe161(string id,string name,int slot,int budget,string scope,string allocation,int bursts,string secondary,string barrier,int threshold,int bonus,string[] channels,int[] weights)
        {Id=id;Name=name;Slot=slot;BudgetBasisPoints=budget;TargetScope=scope;Allocation=allocation;Bursts=bursts;Secondary=secondary;BarrierScope=barrier;
         WoundedThreshold=threshold;WoundedBonus=bonus;Channels=Array.AsReadOnly(channels);ChannelWeights=Array.AsReadOnly(weights);}
    }
}

