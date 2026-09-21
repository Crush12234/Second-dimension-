using System;
using System.Collections.Generic;

namespace SecondDimension.Presentation.Campaign018
{
    [Serializable] public sealed class CampaignManifest018 { public string contentVersion; public string title; public string[] worldIds; public string[] arcIds; public string[] chapterIds; public string[] mapIds; public string[] siegeIds; public string[] bossIds; public bool campaignOrderIsCanonOnlyThroughGoblinWorld; public string laterWorldOrderStatus; public string[] launchScopeRecommendation; public string[] futureContentReady; public string[] hardLaws; }
    [Serializable] public sealed class WorldDefinitionsFile018 { public WorldDefinition018[] worlds; }
    [Serializable] public sealed class WorldDefinition018 { public string id; public string key; public string name; public string fortress; public string founder; public string theme; public string centralQuestion; public string tone; public string timeLaw; public string canonStatus; public string[] regions; public string[] capitals; public string[] heroes; public string[] villains; public string[] disasters; public string conflict; public string[] fortressOperations; }
    [Serializable] public sealed class CampaignArcsFile018 { public CampaignArcDefinition018[] arcs; }
    [Serializable] public sealed class CampaignArcDefinition018 { public string id; public string name; public string worldId; public int order; public string canonStatus; public string[] chapterIds; public string previousArcId; public string nextArcId; public bool requiresOwnerOrStoryGate; public string completionOutcome; public bool noConquestChecklist; public string[] unlockGates; }
    [Serializable] public sealed class QuestChaptersFile018 { public QuestChapterDefinition018[] chapters; }
    [Serializable] public sealed class QuestChapterDefinition018 { public string id; public string arcId; public int chapterNumber; public string title; public string worldId; public string worldName; public string canonStatus; public string[] unlockGates; public string[] gameplayModes; public string[] mapIds; public string siegeProfileId; public string primaryObjective; public string[] optionalObjectives; public bool nonCombatBeatRequired; public bool relationshipMemoryRequired; public bool cityConsequenceRequired; public bool battleUsesCertifiedEngine; public bool individualArtSelection; public string failurePath; public CampaignRewardBundle018 rewardBundle; public string nextChapterId; public string[] canonGuardrails; }
    [Serializable] public sealed class CampaignRewardBundle018 { public int personalXp; public int guildXp; public int hallXp; public int materials; public string equipmentReward; }
    [Serializable] public sealed class WorldMapsFile018 { public WorldMapDefinition018[] maps; }
    [Serializable] public sealed class WorldMapDefinition018 { public string id; public string worldId; public string name; public string mapType; public string resourcePath; public string[] nodes; public string[] capitals; public string canonStatus; }
    [Serializable] public sealed class FortressSiegesFile018 { public FortressSiegeDefinition018[] sieges; }
    [Serializable] public sealed class FortressSiegeDefinition018 { public string id; public string worldId; public string name; public string objectiveType; public string canonStatus; public bool usesStrategicDefense017H; public bool usesCertifiedUnionBattle; public bool createsSecondCombatResolver; public string[] lanes; public int waveCount; public int maxAlliedUnions; public int maxEnemyUnions; public bool requiresCivilianRules; public bool allowsSurrender; public string decisiveBattleTrigger; public string[] resultModes; public bool noConquestEnding; public string rewardChannel; }
    [Serializable] public sealed class BossEncountersFile018 { public BossEncounterDefinition018[] bosses; }
    [Serializable] public sealed class BossEncounterDefinition018 { public string id; public string worldId; public string name; public string encounterRole; public string[] resolutionModes; public bool mustNotRequireExecution; public bool usesCertifiedUnionBattle; public int phaseCount; public string objectiveHook; public string rewardTheme; }

    [Serializable] public sealed class CampaignProgressState018
    {
        public string contentVersion = "CAMPAIGN_FORTRESS_WORLDS_018_1.0";
        public List<string> completedChapterIds = new List<string>();
        public List<string> unlockedArcIds = new List<string>();
        public List<string> completedSiegeIds = new List<string>();
        public List<string> resolvedReceiptIds = new List<string>();
        public List<string> unlockedWorldIds = new List<string>();
        public string activeChapterId;
        public string activeCampaignRequestId;
        public int campaignProgress;
    }

    [Serializable] public sealed class CampaignOperationRequest018
    {
        public string requestId; public string chapterId; public string arcId; public string worldId; public string mapId; public string siegeProfileId; public string canonicalSeed; public string[] alliedUnionIds; public string[] objectiveIds; public string returnCheckpointId; public string preOperationStateHash;
    }

    [Serializable] public sealed class CampaignOperationReceipt018
    {
        public string receiptId; public string requestId; public string chapterId; public string outcome; public string authoritativeBattleResultHash; public string existingEquipmentRewardReceiptId; public int personalXp; public int guildXp; public int hallXp; public int materials; public string[] relationshipMemoryIds; public string[] cityUnlockIds; public string returnCheckpointId; public int appliedVersion;
    }
}
