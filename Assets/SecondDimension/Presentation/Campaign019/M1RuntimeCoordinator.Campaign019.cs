using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Presentation
{
    public sealed partial class M1RuntimeCoordinator : Campaign019.ICampaignPresentationCoordinator019, Campaign019.ICampaignReplayPresentationCoordinator130
    {
        private Campaign019.CampaignRegistry019 _campaignRegistry019;
        private Campaign019.CampaignRuleCatalogAdapter019 _campaignRules019;
        private readonly CampaignCommandService019 _campaignCommands019 = new CampaignCommandService019();

        public Campaign019.CampaignPresentationState019 Campaign019
        {
            get
            {
                try { return BuildCampaignPresentation019(); }
                catch (Exception exception)
                {
                    return new Campaign019.CampaignPresentationState019
                    {
                        IsAvailable = false,
                        Error = exception.Message
                    };
                }
            }
        }

        private Campaign019.CampaignRegistry019 Registry019()
        {
            if (_campaignRegistry019 != null) return _campaignRegistry019;
            _campaignRegistry019 = global::SecondDimension.Presentation.Campaign019.CampaignRegistry019.LoadFromResources();
            _campaignRules019 = new Campaign019.CampaignRuleCatalogAdapter019(
                _campaignRegistry019.Base018);
            return _campaignRegistry019;
        }

        public M1CommandResult RefreshCampaign019()
        {
            Registry019();
            return M1CommandResult.Success("Campaign authority refreshed without changing the save.");
        }

        public M1CommandResult StartChapter019(string chapterId)
        {
            Registry019();
            var boundary = ReleaseIdleTowerForGuildProgression107(_campaign);
            if (!boundary.IsSuccess)
                return M1CommandResult.Failure(FriendlyErrors(boundary.Errors));
            return ApplyAndPersist(
                _campaignCommands019.StartChapter(
                    boundary.Value,
                    _campaignRules019,
                    chapterId,
                    FirstCampaignUnionIds019(),
                    ownerApprovedCandidateOrder: true),
                notify: true,
                successMessage: "Campaign chapter, map, objectives, and return checkpoint committed.");
        }

        public M1CommandResult StartNextCampaignCycle130(int expectedCurrentCycle,int growthPercent=25)
        {
            if(TowerWriteBusy116())return M1CommandResult.Failure(TowerBusy116);
            Registry019();Registry023();
            return ApplyAndPersist(new CampaignReplayCommandService130().StartNextCycle(
                _campaign,_campaignRules019,_campaignRules023,expectedCurrentCycle,growthPercent),
                notify:true,successMessage:"The next story cycle is saved. Your heroes, equipment, rewards and world unlocks are kept.");
        }

        public M1CommandResult EnterCampaignCertifiedBattle019()
        {
            Registry019();
            if (_campaign?.Guild?.GuildCity?.PendingEncounter == null)
            {
                var committed = ApplyAndPersist(
                    _campaignCommands019.CommitCertifiedBattle(_campaign, _campaignRules019),
                    notify: false,
                    successMessage: "Campaign encounter committed before transition.");
                if (!committed.Succeeded) return committed;
            }
            return StartCommittedGuildCityBattle017D();
        }

        public M1CommandResult CommitCampaignNonCombat019(string outcome)
        {
            Registry019();
            return ApplyAndPersist(
                _campaignCommands019.CommitNonCombatReceipt(_campaign, _campaignRules019, outcome),
                notify: true,
                successMessage: "Campaign noncombat result committed. Apply it once to advance the world.");
        }

        public M1CommandResult ApplyCampaignReceipt019()
        {
            Registry019(); Registry020();
            return ApplyAndPersist(
                _campaignCommands019.ApplyReceiptExactlyOnce(_campaign,
                    _campaignRules019,_campaignRules020),
                notify: true,
                successMessage: "Campaign result applied exactly once. World, city, Guild, and story progress were saved.");
        }

        private bool HasActiveCampaignEncounter019(CampaignState campaign)
        {
            var operation = campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?.ActiveOperation;
            var encounter = campaign?.Guild?.GuildCity?.PendingEncounter;
            return operation != null && operation.BattleCommitted && encounter != null &&
                   StringComparer.Ordinal.Equals(encounter.RequestId,
                       "CAMPAIGN_ENCOUNTER_" + operation.RequestId);
        }

        private Result<CampaignState> CommitClaimedCampaignBattle019(CampaignState campaign)
        {
            Registry019();
            var returned = _campaignCommands019.CommitClaimedBattleReceiptAndClearEncounter(
                campaign,
                _campaignRules019);
            if (!returned.IsSuccess || returned.Value.Guild.GuildCity.Strategic017H.Campaign019.Playable020?.ActiveOperation == null)
                return returned;
            Registry020();
            // The actual claim transaction prepares the saved postbattle story
            // receipt; presentation never manufactures or reapplies battle loot.
            return _campaignCardFlow132.PrepareNextInterruption(returned.Value, _campaignRules020, _campaignRules019);
        }

        private IReadOnlyList<string> FirstCampaignUnionIds019()
        {
            var ids = new List<string>();
            if (_campaign?.Guild?.Unions != null)
            {
                for (var i = 0; i < _campaign.Guild.Unions.Count && ids.Count < 10; i++)
                {
                    var union = _campaign.Guild.Unions[i];
                    if (union != null && union.MemberRecruitIds.Count > 0) ids.Add(union.UnionId);
                }
            }
            return ids.AsReadOnly();
        }

        private Campaign019.CampaignPresentationState019 BuildCampaignPresentation019()
        {
            var registry = Registry019();
            var progress = _campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019 ??
                           CampaignProgressState019.Default();
            var cycleCompleted130=CampaignReplayRules130.CurrentCompleted(progress);
            var storyGates130=_campaign?.Guild?.GuildCity?.Strategic017H?.StoryGates??Array.Empty<string>();
            var arcViews = new List<Campaign019.CampaignArcView019>();
            foreach (var arc in registry.Base018.Arcs.Values.OrderBy(value => value.order))
            {
                var chapterIds = arc.chapterIds ?? Array.Empty<string>();
                var completed = chapterIds.Count(id => cycleCompleted130.Contains(id));
                arcViews.Add(new Campaign019.CampaignArcView019
                {
                    ArcId = arc.id,
                    DisplayName = arc.name,
                    CanonStatus = arc.canonStatus,
                    Unlocked = _campaignRules019.TryGetArc(arc.id,out var arcRule130)&&
                        CampaignCommandService019.ArcGatesSatisfied(_campaignRules019,arcRule130,
                            cycleCompleted130,storyGates130,true),
                    Completed = completed,
                    Total = chapterIds.Length,
                    Summary = arc.completionOutcome
                });
            }

            var chapterViews = new List<Campaign019.CampaignChapterView019>();
            foreach (var narrative in registry.Chapters.Values
                         .OrderBy(value => registry.Base018.Arcs[value.arcId].order)
                         .ThenBy(value => registry.Base018.Chapters[value.chapterId].chapterNumber))
            {
                var definition = registry.Base018.Chapters[narrative.chapterId];
                var arc = registry.Base018.Arcs[narrative.arcId];
                var mapPath = string.Empty;
                if (definition.mapIds != null && definition.mapIds.Length > 0 &&
                    registry.Base018.Maps.TryGetValue(definition.mapIds[0], out var map))
                    mapPath = map.resourcePath;
                var chapterIndex = Array.IndexOf(arc.chapterIds ?? Array.Empty<string>(), narrative.chapterId);
                var previousComplete = chapterIndex <= 0 ||
                                       cycleCompleted130.Contains(arc.chapterIds[chapterIndex - 1]);
                var completed = cycleCompleted130.Contains(narrative.chapterId);
                var active = StringComparer.Ordinal.Equals(progress.ActiveChapterId, narrative.chapterId);
                var available = _campaignRules019.TryGetArc(narrative.arcId,out var arcRule130)&&
                    CampaignCommandService019.ArcGatesSatisfied(_campaignRules019,arcRule130,
                        cycleCompleted130,storyGates130,true)&&
                    previousComplete&&!completed;
                chapterViews.Add(new Campaign019.CampaignChapterView019
                {
                    ChapterId = narrative.chapterId,
                    ArcId = narrative.arcId,
                    WorldId = narrative.worldId,
                    Title = narrative.title,
                    Region = narrative.regionFocus,
                    Briefing = narrative.openingBriefing,
                    CivilianScene = narrative.civilianScene,
                    FactionTension = narrative.factionTension,
                    Objective = narrative.primaryObjectiveText,
                    BattleBriefing = narrative.battleBriefing,
                    BattleRequired = narrative.battleRequired,
                    Completed = completed,
                    Active = active,
                    Status = completed ? "COMPLETED" : active ? "ACTIVE" : available ? "AVAILABLE" : "LOCKED",
                    MapResourcePath = mapPath,
                    GuideTip = narrative.guideTip,
                    RelationshipBeat = narrative.relationshipMemoryText,
                    CityConsequence = narrative.cityConsequenceText
                });
            }

            var worldViews = new List<Campaign019.CampaignWorldView019>();
            foreach (var world in registry.Worlds.Values.OrderBy(value => value.name))
            {
                var mapPath = registry.Base018.Maps.Values.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.worldId, world.worldId) &&
                    StringComparer.Ordinal.Equals(value.mapType, "WORLD_OVERVIEW"))?.resourcePath ?? string.Empty;
                worldViews.Add(new Campaign019.CampaignWorldView019
                {
                    WorldId = world.worldId,
                    DisplayName = world.name,
                    Fortress = world.fortress,
                    Theme = world.theme,
                    CentralQuestion = world.centralQuestion,
                    TimeLaw = world.timeLaw,
                    CanonStatus = world.canonStatus,
                    Unlocked = progress.UnlockedWorldIds.Contains(world.worldId),
                    MapResourcePath = mapPath
                });
            }

            var activeNarrative = chapterViews.FirstOrDefault(value => value.Active);
            var activeWorldId = activeNarrative?.WorldId ??
                                worldViews.FirstOrDefault(value => value.Unlocked)?.WorldId ?? string.Empty;
            var eventViews = new List<Campaign019.CampaignEventView019>();
            var diplomacyViews = new List<Campaign019.CampaignDiplomacyView019>();
            var bossViews = new List<Campaign019.CampaignBossView019>();
            if (!string.IsNullOrWhiteSpace(activeWorldId) && registry.Worlds.TryGetValue(activeWorldId, out var pack))
            {
                foreach (var id in pack.eventIds ?? Array.Empty<string>())
                    if (registry.Events.TryGetValue(id, out var item))
                        eventViews.Add(new Campaign019.CampaignEventView019
                        {
                            EventId = item.eventId,
                            Title = item.title,
                            Category = item.category,
                            Region = item.region,
                            Setup = item.setup,
                            ActingRole = item.actingRole
                        });
                foreach (var id in pack.diplomacyIncidentIds ?? Array.Empty<string>())
                    if (registry.Diplomacy.TryGetValue(id, out var item))
                        diplomacyViews.Add(new Campaign019.CampaignDiplomacyView019
                        {
                            IncidentId = item.incidentId,
                            Title = item.title,
                            FactionName = item.factionName,
                            Setup = item.setup,
                            CentralQuestion = item.centralQuestion
                        });
                foreach (var id in pack.bossIds ?? Array.Empty<string>())
                    if (registry.Bosses.TryGetValue(id, out var item))
                        bossViews.Add(new Campaign019.CampaignBossView019
                        {
                            BossId = item.bossId,
                            Name = item.name,
                            Role = item.encounterRole,
                            Objective = item.objectiveHook,
                            RewardTheme = item.rewardTheme
                        });
            }

            var activeOperation = progress.ActiveOperation;
            var activeRequiresBattle = activeOperation != null &&
                                       activeNarrative != null && activeNarrative.BattleRequired;
            var battleInProgress = activeOperation != null && _campaign?.Battle != null &&
                                   _campaign.Battle.Outcome == BattleOutcome.InProgress;
            var awaitingReward = activeOperation != null && _campaign?.Battle != null &&
                                 _campaign.Battle.Phase == BattlePhase.Resolved &&
                                 _campaign.Battle.Reward != null && !_campaign.Battle.Reward.Claimed;

            return new Campaign019.CampaignPresentationState019
            {
                IsAvailable = true,
                ActiveArcId = progress.ActiveArcId,
                ActiveChapterId = progress.ActiveChapterId,
                ActiveRequestId = activeOperation?.RequestId ?? string.Empty,
                PendingReceiptId = progress.PendingReceipt?.ReceiptId ?? string.Empty,
                CampaignProgress = progress.CampaignProgress,
                CurrentCycle130=CampaignReplayRules130.CurrentCycle(progress),
                CycleCompleted130=cycleCompleted130.Count,
                CycleTotal130=CampaignReplayRules130.ChapterCount,
                ReplayGrowthPercent130=CampaignReplayRules130.GrowthPercent(progress),
                CanStartNextCycle130=CampaignReplayRules130.HasCompleteChapterSet(cycleCompleted130)&&
                    !SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D.HasAnyUnresolvedAdventure084(_campaign),
                CanEnterBattle = activeRequiresBattle && progress.PendingReceipt == null && !awaitingReward,
                CanResolveNonCombat = activeOperation != null && !activeRequiresBattle && progress.PendingReceipt == null,
                CanApplyReceipt = progress.PendingReceipt != null,
                BattleInProgress = battleInProgress,
                AwaitingBattleRewardClaim = awaitingReward,
                Arcs = arcViews.AsReadOnly(),
                Chapters = chapterViews.AsReadOnly(),
                Worlds = worldViews.AsReadOnly(),
                ActiveWorldEvents = eventViews.AsReadOnly(),
                ActiveWorldDiplomacy = diplomacyViews.AsReadOnly(),
                ActiveWorldBosses = bossViews.AsReadOnly()
            };
        }
    }
}
