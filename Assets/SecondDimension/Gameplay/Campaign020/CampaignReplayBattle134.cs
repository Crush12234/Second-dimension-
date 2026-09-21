using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.Campaign020
{
    // Frozen only on newly committed reset mission 1. Existing unmarked
    // operations and their noncombat receipts retain their exact old rules.
    public static class CampaignReplayBattle134
    {
        public const string ChapterId = "CH018_001";
        public const string ObjectivePolicy = "CAMPAIGN_RESET_STORY_BATTLE134_V1";
        public const string BlueprintSuffix = "_RESET_BATTLE134_V1";
        public const string BattleStepId = "STEP020_CH018_001_REPLAY_BATTLE134";

        public static bool ShouldCommit(CampaignProgressState019 progress, string chapterId) =>
            chapterId == ChapterId && CampaignReplayRules130.CurrentCycle(progress) > 1 &&
            progress?.Replay130?.CycleStarts.LastOrDefault()?.Finale132 != null;

        public static bool Required(CampaignState campaign, string chapterId)
        {
            if (chapterId != ChapterId) return false;
            var progress = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019;
            var committed = progress?.ActiveOperation;
            if (committed != null)
                return committed.ChapterId == ChapterId && committed.ObjectiveIds.Contains(ObjectivePolicy) &&
                    CampaignReplayThreat130.ParseCommittedRoutes(committed.ObjectiveIds)?.Cycle > 1;
            //019 closes immediately before020 validates/closes its saved ledger.
            var op = progress?.Playable020?.ActiveOperation;
            return op?.ChapterId == ChapterId &&
                op.BlueprintId.EndsWith(BlueprintSuffix, StringComparison.Ordinal) &&
                CampaignReplayRules130.CurrentCycle(progress) > 1;
        }

        public static CampaignBlueprintRule020 Effective(CampaignState campaign, CampaignBlueprintRule020 authored)
        {
            if (authored == null || !Required(campaign, authored.ChapterId) || authored.RequiresCertifiedBattle)
                return authored;
            var steps = new List<CampaignStepRule020>(authored.Steps);
            var result = steps.FindIndex(step => step.Kind == "RESULTS");
            if (result != steps.Count - 1 || result < 0)
                throw new InvalidOperationException("CAMPAIGN134_RESET_RESULTS_BOUNDARY_REQUIRED");
            steps.Insert(result, new CampaignStepRule020 {
                StepId = BattleStepId, Kind = "CERTIFIED_BATTLE", Title = "Hold the Hall foundations",
                SourceId = ChapterId, RequiresCertifiedBattle = true, ConsumesOperation = true
            });
            return new CampaignBlueprintRule020 {
                BlueprintId = authored.BlueprintId + BlueprintSuffix, ChapterId = authored.ChapterId,
                ArcId = authored.ArcId, WorldId = authored.WorldId, MapId = authored.MapId,
                SiegeId = authored.SiegeId, EnemyPackId = authored.EnemyPackId, LootProfileId = authored.LootProfileId,
                MaterialIds = authored.MaterialIds, RepeatableUnlockIds = authored.RepeatableUnlockIds,
                Steps = steps.AsReadOnly(), RequiresCertifiedBattle = true,
                MaximumAlliedUnions = authored.MaximumAlliedUnions, MaximumEnemyUnions = authored.MaximumEnemyUnions
            };
        }
    }
}
