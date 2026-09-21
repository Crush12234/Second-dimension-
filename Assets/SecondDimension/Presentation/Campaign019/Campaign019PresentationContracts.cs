using System;
using System.Collections.Generic;
using SecondDimension.Presentation;

namespace SecondDimension.Presentation.Campaign019
{
    public interface ICampaignPresentationCoordinator019
    {
        CampaignPresentationState019 Campaign019 { get; }
        M1CommandResult RefreshCampaign019();
        M1CommandResult StartChapter019(string chapterId);
        M1CommandResult EnterCampaignCertifiedBattle019();
        M1CommandResult CommitCampaignNonCombat019(string outcome);
        M1CommandResult ApplyCampaignReceipt019();
    }

    public interface ICampaignReplayPresentationCoordinator130
    {
        M1CommandResult StartNextCampaignCycle130(int expectedCurrentCycle,int growthPercent=25);
    }

    public sealed class CampaignPresentationState019
    {
        public bool IsAvailable;
        public string Error;
        public string ActiveArcId;
        public string ActiveChapterId;
        public string ActiveRequestId;
        public string PendingReceiptId;
        public int CampaignProgress;
        public int CurrentCycle130=1;
        public int CycleCompleted130;
        public int CycleTotal130=82;
        public int ReplayGrowthPercent130=25;
        public bool CanStartNextCycle130;
        public bool CanEnterBattle;
        public bool CanResolveNonCombat;
        public bool CanApplyReceipt;
        public bool BattleInProgress;
        public bool AwaitingBattleRewardClaim;
        public IReadOnlyList<CampaignArcView019> Arcs = Array.Empty<CampaignArcView019>();
        public IReadOnlyList<CampaignChapterView019> Chapters = Array.Empty<CampaignChapterView019>();
        public IReadOnlyList<CampaignWorldView019> Worlds = Array.Empty<CampaignWorldView019>();
        public IReadOnlyList<CampaignEventView019> ActiveWorldEvents = Array.Empty<CampaignEventView019>();
        public IReadOnlyList<CampaignDiplomacyView019> ActiveWorldDiplomacy = Array.Empty<CampaignDiplomacyView019>();
        public IReadOnlyList<CampaignBossView019> ActiveWorldBosses = Array.Empty<CampaignBossView019>();
    }

    public sealed class CampaignArcView019
    {
        public string ArcId; public string DisplayName; public string CanonStatus;
        public bool Unlocked; public int Completed; public int Total; public string Summary;
    }

    public sealed class CampaignChapterView019
    {
        public string ChapterId; public string ArcId; public string WorldId; public string Title;
        public string Region; public string Briefing; public string CivilianScene; public string FactionTension;
        public string Objective; public string BattleBriefing; public bool BattleRequired; public bool Completed;
        public bool Active; public string Status; public string MapResourcePath; public string GuideTip;
        public string RelationshipBeat; public string CityConsequence;
    }

    public sealed class CampaignWorldView019
    {
        public string WorldId; public string DisplayName; public string Fortress; public string Theme;
        public string CentralQuestion; public string TimeLaw; public string CanonStatus; public bool Unlocked;
        public string MapResourcePath;
    }

    public sealed class CampaignEventView019
    {
        public string EventId; public string Title; public string Category; public string Region;
        public string Setup; public string ActingRole;
    }

    public sealed class CampaignDiplomacyView019
    {
        public string IncidentId; public string Title; public string FactionName;
        public string Setup; public string CentralQuestion;
    }

    public sealed class CampaignBossView019
    {
        public string BossId; public string Name; public string Role; public string Objective;
        public string RewardTheme;
    }
}
