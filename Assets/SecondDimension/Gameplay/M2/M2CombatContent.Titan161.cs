using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SecondDimension.Gameplay.TitanTrials160;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2CombatContent
    {
        public TitanTrialCatalog160 TitanTrials161 { get; private set; }

        // Existing content/rule identities stay unchanged for historical pending
        // battles. A Titan battle itself commits the full catalog recipe hash.
        public M2CombatContent WithTitanTrials161(TitanTrialCatalog160 catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var copy = new M2CombatContent(ContentVersion,
                new Dictionary<string,M2CommandDefinition>(_commands,StringComparer.Ordinal),
                new Dictionary<string,M2ArtDefinition>(_arts,StringComparer.Ordinal),
                new Dictionary<string,M2FormationDefinition>(_formations,StringComparer.Ordinal),
                new Dictionary<string,M2EnemyDefinition>(_enemies,StringComparer.Ordinal),
                TutorialEnemyUnion,EarlyBreakthroughEligible,GuaranteedBreakthroughThreshold,
                DeepProgression,HeroMasterGeneratedAuthority096);
            copy.TitanTrials161 = catalog;
            return copy;
        }
        public M2EnemyDefinition TitanEnemy161(int slot, int tier)
        {
            if (TitanTrials161 == null) throw new InvalidOperationException("TITAN161_CONTENT_REQUIRED");
            var trial=TitanTrials161.Trial(slot);var stats=trial.Stats(tier);
            int rewardScale=checked(10000+1200*(tier-1));
            int personal=checked((int)(((140L+20L*slot)*rewardScale+9999)/10000));
            int treasury=checked((int)(((100L+15L*slot)*rewardScale+9999)/10000));
            return new M2EnemyDefinition(trial.Id+"_TIER_"+tier.ToString(CultureInfo.InvariantCulture),
                trial.Name,stats.MaximumHp,100,10,100,new[]{"ART_QUICK_CUT"},personal,treasury);
        }
        private bool TryTitanEnemy161(string id,out M2EnemyDefinition enemy)
        {
            enemy=null;
            if(TitanTrials161==null||id==null||!id.StartsWith("TITAN_TRIAL_",StringComparison.Ordinal))return false;
            var parts=id.Split(new[]{"_TIER_"},StringSplitOptions.None);
            if(parts.Length!=2||!int.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out int tier)||tier<1)return false;
            var trial=TitanTrials161.Trials.SingleOrDefault(x=>x.Id==parts[0]);
            if(trial==null)return false;
            enemy=TitanEnemy161(trial.Slot,tier);
            return enemy.Id==id;
        }
    }
}
