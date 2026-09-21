using System;
using System.Linq;
using Newtonsoft.Json;
namespace SecondDimension.Gameplay.Campaign019
{
    // Created only after the final chapter's ordinary battle and020 receipts
    // validate. The immutable boundary hash binds this actual earned force.
    [Serializable]
    public sealed class CampaignFinaleProof132
    {
        [JsonConstructor]
        public CampaignFinaleProof132(string battleId,string rewardId,string finalStateHash,
            long minimumTotalHp,long minimumTotalAttack,long minimumTotalMagic,int minimumEnemyUnions)
        {
            if(string.IsNullOrWhiteSpace(battleId)||string.IsNullOrWhiteSpace(rewardId)||
                string.IsNullOrWhiteSpace(finalStateHash)||finalStateHash.Length!=64||finalStateHash.Any(value=>!Uri.IsHexDigit(value))||
                minimumTotalHp<=0||minimumTotalAttack<=0||minimumTotalMagic<=0||
                minimumTotalHp>60L*CampaignReplayThreat130.MaximumMemberHp130||
                minimumTotalAttack>60L*CampaignReplayThreat130.MaximumMemberOffense130||
                minimumTotalMagic>60L*CampaignReplayThreat130.MaximumMemberOffense130||
                minimumEnemyUnions<1||minimumEnemyUnions>10)
                throw new ArgumentException("CAMPAIGN132_FINALE_FORCE_INVALID");
            BattleId=battleId;RewardId=rewardId;FinalStateHash=finalStateHash;
            MinimumTotalHp=minimumTotalHp;MinimumTotalAttack=minimumTotalAttack;
            MinimumTotalMagic=minimumTotalMagic;MinimumEnemyUnions=minimumEnemyUnions;
        }
        public string BattleId{get;} public string RewardId{get;} public string FinalStateHash{get;}
        public long MinimumTotalHp{get;} public long MinimumTotalAttack{get;} public long MinimumTotalMagic{get;}
        public int MinimumEnemyUnions{get;}
    }
}
