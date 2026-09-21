using System;
using System.Collections.Generic;
using SecondDimension.Gameplay.TitanHeroes161;
namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2CombatContent
    {
        public M2CombatContent WithTitanHeroes161()
        {
            if(_arts.ContainsKey("GOLD_TITAN_001"))return this;
            var arts=new Dictionary<string,M2ArtDefinition>(_arts,StringComparer.Ordinal);
            foreach(var art in TitanHeroCatalog161.Arts())arts.Add(art.Id,art);
            var result = new M2CombatContent(ContentVersion,
                new Dictionary<string,M2CommandDefinition>(_commands,StringComparer.Ordinal),arts,
                new Dictionary<string,M2FormationDefinition>(_formations,StringComparer.Ordinal),
                new Dictionary<string,M2EnemyDefinition>(_enemies,StringComparer.Ordinal),TutorialEnemyUnion,
                EarlyBreakthroughEligible,GuaranteedBreakthroughThreshold,DeepProgression,HeroMasterGeneratedAuthority096);
            result.TitanTrials161=TitanTrials161;
            return result;
        }
    }
}
