using System;

namespace SecondDimension.Gameplay.State
{
    public static class CampaignFactory
    {
        public static CampaignState CreateM0Proof(long campaignSeed)
        {
            var guildId = "GUILD_M0_PROOF";
            return new CampaignState(
                campaignGuid: "00000000-0000-0000-0000-000000000001",
                campaignSeed: campaignSeed,
                contentAuthorityVersion: "1.0",
                rules: ModeRuleSnapshot.StandardDefaults(),
                guild: new GuildState(guildId, 0, Array.Empty<RecruitState>(), Array.Empty<UnionState>()));
        }
    }
}

