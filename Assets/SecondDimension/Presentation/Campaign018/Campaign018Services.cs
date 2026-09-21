using System;
using System.Collections.Generic;
using System.Linq;

namespace SecondDimension.Presentation.Campaign018
{
    public sealed class CampaignGateService018
    {
        readonly CampaignRegistry018 _registry;
        public CampaignGateService018(CampaignRegistry018 registry) { _registry = registry ?? throw new ArgumentNullException(nameof(registry)); }

        public bool IsArcUnlocked(CampaignArcDefinition018 arc, CampaignProgressState018 state, ISet<string> storyFlags, bool ownerApprovedCandidateOrder)
        {
            if (arc == null || state == null) return false;
            foreach (var gate in arc.unlockGates ?? Array.Empty<string>())
            {
                if (gate == "OWNER_OR_CAMPAIGN_ORDER_APPROVAL" && !ownerApprovedCandidateOrder) return false;
                if (gate.StartsWith("COMPLETE:", StringComparison.Ordinal) && !state.completedChapterIds.Contains(LastChapterId(gate.Substring(9)))) return false;
                if (gate.StartsWith("STORY:", StringComparison.Ordinal) && (storyFlags == null || !storyFlags.Contains(gate.Substring(6)))) return false;
            }
            return true;
        }

        string LastChapterId(string arcId)
        {
            if (!_registry.Arcs.TryGetValue(arcId, out var arc) || arc.chapterIds == null || arc.chapterIds.Length == 0) return string.Empty;
            return arc.chapterIds[arc.chapterIds.Length - 1];
        }
    }

    public sealed class CampaignProgressService018
    {
        readonly CampaignRegistry018 _registry;
        public CampaignProgressService018(CampaignRegistry018 registry) { _registry = registry ?? throw new ArgumentNullException(nameof(registry)); }

        public bool TryStartChapter(CampaignProgressState018 state, string chapterId, string requestId)
        {
            if (state == null || string.IsNullOrEmpty(requestId) || !_registry.Chapters.ContainsKey(chapterId)) return false;
            if (!string.IsNullOrEmpty(state.activeCampaignRequestId)) return false;
            state.activeChapterId = chapterId; state.activeCampaignRequestId = requestId; return true;
        }

        public bool TryApplyReceipt(CampaignProgressState018 state, CampaignOperationReceipt018 receipt)
        {
            if (state == null || receipt == null || string.IsNullOrEmpty(receipt.receiptId)) return false;
            if (state.resolvedReceiptIds.Contains(receipt.receiptId)) return false;
            if (receipt.requestId != state.activeCampaignRequestId || receipt.chapterId != state.activeChapterId) return false;
            if (!_registry.Chapters.ContainsKey(receipt.chapterId)) return false;
            state.resolvedReceiptIds.Add(receipt.receiptId);
            if (!state.completedChapterIds.Contains(receipt.chapterId) && receipt.outcome != "FAILURE") state.completedChapterIds.Add(receipt.chapterId);
            state.campaignProgress += Math.Max(0, receipt.guildXp + receipt.hallXp);
            state.activeChapterId = null; state.activeCampaignRequestId = null;
            return true;
        }
    }

    public interface ICertifiedFortressRuntime018
    {
        bool TryCommitStrategicFortressOperation(FortressSiegeDefinition018 definition, CampaignOperationRequest018 request);
        bool TryLaunchCertifiedUnionBattle(string campaignRequestId);
    }

    public sealed class CampaignFortressBridge018
    {
        readonly CampaignRegistry018 _registry; readonly ICertifiedFortressRuntime018 _runtime;
        public CampaignFortressBridge018(CampaignRegistry018 registry, ICertifiedFortressRuntime018 runtime) { _registry = registry; _runtime = runtime; }
        public bool TryCommit(string siegeId, CampaignOperationRequest018 request)
        {
            if (request == null || !_registry.Sieges.TryGetValue(siegeId, out var siege)) return false;
            if (!siege.usesStrategicDefense017H || !siege.usesCertifiedUnionBattle || siege.createsSecondCombatResolver) return false;
            return _runtime != null && _runtime.TryCommitStrategicFortressOperation(siege, request);
        }
    }
}
