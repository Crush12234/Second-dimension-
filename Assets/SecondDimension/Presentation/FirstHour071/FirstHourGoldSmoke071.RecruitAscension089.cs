using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.FirstHour071
{
    public sealed partial class FirstHourGoldSmoke071
    {
        private const string RecruitSmokeChapter089 = "CH018_001";
        private const string RecruitSmokeUnion089 = "RECRUIT_SMOKE_UNION_089";
        private const string RecruitSmokeActor089 = "RECRUIT_SMOKE_ACTOR_089";
        private const string RecruitSmokeAssistant089 = "RECRUIT_SMOKE_ASSISTANT_089";

        private sealed class EarnedRecruitPlan089
        {
            public CampaignState BaseCampaign;
            public HeroMaster300Hero087 Hero;
            public string TargetCardId;
            public IReadOnlyList<string> AdvanceCardIds = Array.Empty<string>();
        }

        private sealed class RecruitTraversalState089
        {
            public CampaignState Campaign;
            public IReadOnlyList<string> AdvanceCardIds = Array.Empty<string>();
        }

        private IEnumerator CertifyEarnedRecruitAndAscension089()
        {
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            var savePath = Path.Combine(
                _evidenceRoot, "recruit_ascension_089",
                "earned_recruit_ascension_smoke.json");
            _report.recruitAscensionSavePath = savePath;
            if (File.Exists(savePath)) File.Delete(savePath);
            if (File.Exists(savePath + ".bak")) File.Delete(savePath + ".bak");

            var registry020 = CampaignRegistry020.LoadFromResources();
            var catalog020 = new Campaign020RuleCatalogAdapter(registry020);
            var registry023 = CampaignRegistry023.LoadFromResources();
            var catalog023 = new Campaign023RuleCatalogAdapter(registry023);
            var campaign020 = new CampaignPlayableCommandService020();
            var worldGate = new CampaignWorldGateCommandService023();
            var deck = new ExpeditionDeckCommandService089(
                worldGate, new ExpeditionDeckService089());
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var recruitmentContent = RecruitmentContent.LoadFromDirectory(contentRoot);
            var cityContent = GuildCityContent017D.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017D"));
            var recruitment = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot)),
                heroes);

            var firstPlan = FindNewRecruitPlan089(
                campaign020, worldGate, deck, catalog020, catalog023, heroes,
                recruitment, recruitmentContent, cityContent);
            Require071(firstPlan != null,
                "The bounded production deck sample did not produce a successful earned Recruit card.");
            WriteRecruitSmokeCampaign089(savePath, firstPlan.BaseCampaign);

            var recruitCoordinator = new M1RuntimeCoordinator(contentRoot, savePath);
            RequireCommand089(recruitCoordinator.BeginWorldGateOperation023(
                RecruitSmokeChapter089), "begin earned Recruit-card board");
            RequireCommand089(recruitCoordinator.AcknowledgeExpeditionDeckTutorial089(),
                "save Expedition Deck tutorial acknowledgement");
            foreach (var advanceCardId in firstPlan.AdvanceCardIds)
            {
                RequireCommand089(recruitCoordinator.CommitExpeditionRouteCard089(
                    advanceCardId), "resolve encounter row before earned Recruit route");
                RequireCommand089(recruitCoordinator.ApplyWorldGateReceipt023(),
                    "apply encounter row before earned Recruit route");
            }
            var recruitRoute = recruitCoordinator.CampaignWorldGate023.RouteCards
                .SingleOrDefault(value => StringComparer.Ordinal.Equals(
                    value.CardId, firstPlan.TargetCardId));
            Require071(recruitRoute != null &&
                       StringComparer.Ordinal.Equals(recruitRoute.Category, "RECRUIT") &&
                       StringComparer.Ordinal.Equals(recruitRoute.RecruitStableId,
                           firstPlan.Hero.StableId),
                "The shipping three-card row did not expose the exact planned Hero Master Recruit lead.");
            _presenter.ShowFirstHourGoldAdventureBoard084(
                recruitCoordinator, recruitCoordinator.CampaignWorldGate023);
            // The shipping row deliberately deals each card face down.  Evidence
            // must wait for this exact Hero card to finish its physical flip;
            // hidden hierarchy text is not proof that a player can see or choose it.
            yield return WaitForVisibleExpeditionRouteCard089(
                firstPlan.TargetCardId, "earned Recruit Hero");
            RequireNamedVisibleTextContains076(
                "Expedition card title " + firstPlan.TargetCardId + " 089",
                firstPlan.Hero.Name.ToUpperInvariant());
            yield return Capture071("earned_recruit_card_exact_hero");

            RequireCommand089(recruitCoordinator.CommitExpeditionRouteCard089(
                firstPlan.TargetCardId), "resolve exact Hero Master Recruit card");
            RequireCommand089(recruitCoordinator.ApplyWorldGateReceipt023(),
                "apply exact Hero Master Recruit reward");
            Require071(recruitCoordinator.CampaignWorldGate023.ExpeditionRecruitLeadIds
                    .Count(value => StringComparer.Ordinal.Equals(
                        value, firstPlan.Hero.StableId)) == 1,
                "The successful Recruit card did not save one exact Hero Master lead.");
            _report.expeditionRecruitCardLeadSavedVerified = true;
            _report.expeditionRecruitHeroStableId = firstPlan.Hero.StableId;
            _report.expeditionRecruitHeroName = firstPlan.Hero.Name;

            recruitCoordinator = new M1RuntimeCoordinator(contentRoot, savePath);
            Require071(recruitCoordinator.CampaignWorldGate023.ExpeditionRecruitLeadIds
                    .Contains(firstPlan.Hero.StableId),
                "The exact earned Hero Master lead did not survive a production coordinator reload.");
            _report.expeditionRecruitLeadReloadVerified = true;
            RequireCommand089(recruitCoordinator.CommitGuildCityApplicantBoard017D(),
                "merge earned Hero Master lead into the Applicant Board");
            var exactApplicant = recruitCoordinator.GuildCity017D.Applicants
                .SingleOrDefault(value => StringComparer.Ordinal.Equals(
                    value.PortraitAuthorityId, firstPlan.Hero.StableId));
            Require071(exactApplicant != null &&
                       StringComparer.Ordinal.Equals(exactApplicant.DisplayName,
                           firstPlan.Hero.Name) &&
                       !exactApplicant.IsAscensionMerge,
                "Applicant Board did not expose the same exact earned Hero Master identity.");
            _report.expeditionRecruitApplicantExactHeroVerified = true;
            _presenter.Initialize(recruitCoordinator);
            _presenter.ShowFirstHourGoldRecruitmentApplicant089(
                exactApplicant.RecruitId);
            Require071(GameObject.Find("Recruit Applicant Primary 074") != null &&
                       GameObject.Find("Selected Applicant Portrait 074") != null,
                "The exact earned recruit did not render through the shipping Recruitment Desk.");
            RequireNamedVisibleTextContains076(
                "Applicant Decision Name 074", firstPlan.Hero.Name.ToUpperInvariant());
            yield return Capture071("earned_recruit_applicant_board");

            var rosterBeforeSign = recruitCoordinator.GuildCity017D.TotalRecruitCount;
            RequireCommand089(recruitCoordinator.SignGuildCityApplicant017D(
                exactApplicant.RecruitId), "sign exact earned Hero Master recruit");
            var signedCampaign = ReadRecruitSmokeCampaign089(savePath);
            Require071(HeroRosterCount089(signedCampaign, firstPlan.Hero) == 1 &&
                       signedCampaign.Guild.Recruits.Count == rosterBeforeSign + 1 &&
                       !WorldGate089(signedCampaign).ExpeditionRecruitLeadIds089
                           .Contains(firstPlan.Hero.StableId),
                "Signing did not consume the lead and create exactly one matching roster hero.");
            var replayCount = signedCampaign.Guild.Recruits.Count;
            RequireCommand089(recruitCoordinator.SignGuildCityApplicant017D(
                exactApplicant.RecruitId), "replay signed applicant action");
            signedCampaign = ReadRecruitSmokeCampaign089(savePath);
            Require071(signedCampaign.Guild.Recruits.Count == replayCount &&
                       HeroRosterCount089(signedCampaign, firstPlan.Hero) == 1,
                "Replaying the signed applicant action created a duplicate roster hero.");
            _report.expeditionRecruitSigningExactOnceVerified = true;
            _report.recruitRosterAfterSigning = signedCampaign.Guild.Recruits.Count;

            var ascensionPlan = FindAscensionPlan089(
                signedCampaign, firstPlan.Hero, campaign020, worldGate, deck,
                catalog020, catalog023, heroes);
            Require071(ascensionPlan != null,
                "The bounded production deck sample did not produce a successful matching Ascension card.");
            WriteRecruitSmokeCampaign089(savePath, ascensionPlan.BaseCampaign);
            var ascensionCoordinator = new M1RuntimeCoordinator(contentRoot, savePath);
            RequireCommand089(ascensionCoordinator.BeginWorldGateOperation023(
                RecruitSmokeChapter089), "begin matching-copy Expedition board");
            RequireCommand089(ascensionCoordinator.AcknowledgeExpeditionDeckTutorial089(),
                "retain Expedition Deck tutorial acknowledgement");
            foreach (var advanceCardId in ascensionPlan.AdvanceCardIds)
            {
                RequireCommand089(ascensionCoordinator.CommitExpeditionRouteCard089(
                    advanceCardId), "advance toward matching-copy route");
                RequireCommand089(ascensionCoordinator.ApplyWorldGateReceipt023(),
                    "apply route before matching-copy reveal");
            }
            var ascensionRoute = ascensionCoordinator.CampaignWorldGate023.RouteCards
                .SingleOrDefault(value => StringComparer.Ordinal.Equals(
                    value.CardId, ascensionPlan.TargetCardId));
            Require071(ascensionRoute != null &&
                       StringComparer.Ordinal.Equals(ascensionRoute.Category, "ASCENSION") &&
                       StringComparer.Ordinal.Equals(ascensionRoute.RecruitStableId,
                           firstPlan.Hero.StableId),
                "The second earned route did not expose a distinct Ascension copy of the owned hero.");
            _presenter.ShowFirstHourGoldAdventureBoard084(
                ascensionCoordinator, ascensionCoordinator.CampaignWorldGate023);
            yield return WaitForVisibleExpeditionRouteCard089(
                ascensionPlan.TargetCardId, "earned Ascension Hero");
            RequireNamedVisibleTextContains076(
                "Expedition card title " + ascensionPlan.TargetCardId + " 089",
                firstPlan.Hero.Name.ToUpperInvariant());
            RequireNamedVisibleTextContains076(
                "Expedition card category " + ascensionPlan.TargetCardId + " 089",
                "ASCENSION");
            yield return Capture071("earned_ascension_card_exact_hero");
            RequireCommand089(ascensionCoordinator.CommitExpeditionRouteCard089(
                ascensionPlan.TargetCardId), "resolve matching Hero Ascension card");
            RequireCommand089(ascensionCoordinator.ApplyWorldGateReceipt023(),
                "apply matching Hero Ascension reward");
            _report.expeditionAscensionCardEarnedVerified = true;

            ascensionCoordinator = new M1RuntimeCoordinator(contentRoot, savePath);
            var duplicateApplicant = ascensionCoordinator.GuildCity017D.Applicants
                .SingleOrDefault(value => StringComparer.Ordinal.Equals(
                    value.PortraitAuthorityId, firstPlan.Hero.StableId));
            Require071(duplicateApplicant != null &&
                       duplicateApplicant.IsAscensionMerge &&
                       duplicateApplicant.NextAscensionLevel ==
                           duplicateApplicant.CurrentAscensionLevel + 1,
                "The earned matching copy did not project the live duplicate Ascension view.");
            _presenter.Initialize(ascensionCoordinator);
            _presenter.ShowFirstHourGoldRecruitmentApplicant089(
                duplicateApplicant.RecruitId);
            var mergeButton = GameObject.Find("Merge Hero Duplicate Primary 089")
                ?.GetComponent<Button>();
            Require071(mergeButton != null && mergeButton.interactable &&
                       GameObject.Find("Applicant Decision Card 074") != null,
                "The shipping Recruitment Desk did not render its live duplicate-merge control.");
            RequireNamedVisibleTextContains076(
                "Applicant Personal Hook 074", "MATCHING HERO COPY");
            RequireNamedVisibleTextContains076(
                "Applicant Permanent Cost 074", "USES NO ROSTER SLOT");
            _report.expeditionDuplicateMergeUiVerified = true;
            yield return Capture071("recruitment_duplicate_ascension_preview");

            var beforeMerge = ReadRecruitSmokeCampaign089(savePath);
            var rosterBeforeMerge = beforeMerge.Guild.Recruits.Count;
            var ascensionBefore = FindOwnedHero089(
                beforeMerge, firstPlan.Hero).Progression.AscensionLevel;
            mergeButton.onClick.Invoke();
            yield return null;
            yield return null;
            var afterMerge = ReadRecruitSmokeCampaign089(savePath);
            var ascensionAfter = FindOwnedHero089(
                afterMerge, firstPlan.Hero).Progression.AscensionLevel;
            Require071(afterMerge.Guild.Recruits.Count == rosterBeforeMerge &&
                       HeroRosterCount089(afterMerge, firstPlan.Hero) == 1 &&
                       ascensionAfter == ascensionBefore + 1 &&
                       !WorldGate089(afterMerge).ExpeditionRecruitLeadIds089
                           .Contains(firstPlan.Hero.StableId),
                "Duplicate merge did not advance Ascension once while preserving roster size.");
            RequireCommand089(ascensionCoordinator.SignGuildCityApplicant017D(
                duplicateApplicant.RecruitId), "replay duplicate merge action");
            var replayMerge = ReadRecruitSmokeCampaign089(savePath);
            Require071(replayMerge.Guild.Recruits.Count == rosterBeforeMerge &&
                       FindOwnedHero089(replayMerge, firstPlan.Hero)
                           .Progression.AscensionLevel == ascensionAfter,
                "Replaying the duplicate merge advanced Ascension more than once.");
            _report.expeditionDuplicateMergeExactOnceVerified = true;
            _report.recruitRosterAfterAscension = replayMerge.Guild.Recruits.Count;
            _report.recruitAscensionBefore = ascensionBefore;
            _report.recruitAscensionAfter = ascensionAfter;
            WriteReport071();
        }

        private static IEnumerator WaitForVisibleExpeditionRouteCard089(
            string cardId,
            string evidenceLabel)
        {
            var wrapperName = "Expedition route card " + cardId + " 089";
            var buttonName = "Choose Expedition route card " + cardId + " 089";
            yield return RevealBlindDraftCardForVerification091(wrapperName, evidenceLabel);
            var deadline = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline)
            {
                var wrapper = GameObject.Find(wrapperName);
                var button = FindActiveButtonByName081(buttonName);
                var activeFlipStage = wrapper != null &&
                    wrapper.GetComponentsInChildren<Transform>(true).Any(value =>
                        value != null && value.gameObject.activeInHierarchy &&
                        StringComparer.Ordinal.Equals(
                            value.name, "Board Adventure Card Flip Stage 086"));
                if (wrapper != null && wrapper.activeInHierarchy &&
                    !activeFlipStage && button != null && button.IsInteractable())
                    yield break;
                yield return null;
            }

            Require071(false,
                "The exact " + evidenceLabel +
                " route card did not finish its visible physical reveal within 12 seconds: " +
                cardId + ".");
        }

        private static IEnumerator RevealBlindDraftCardForVerification091(
            string wrapperName, string evidenceLabel)
        {
            var deadline = Time.realtimeSinceStartup + 12f;
            Campaign023.ExpeditionCardChoice091 choice = null;
            while (Time.realtimeSinceStartup < deadline)
            {
                var wrapper = GameObject.Find(wrapperName);
                choice = wrapper == null ? null :
                    wrapper.GetComponentInParent<Campaign023.ExpeditionCardChoice091>();
                var pick = wrapper == null ? null :
                    wrapper.GetComponentsInChildren<Button>()
                        .FirstOrDefault(value => value.name.StartsWith(
                            "Blind Quest Card Back ", StringComparison.Ordinal));
                if (choice != null && pick != null && pick.interactable &&
                    choice.Phase091 == Campaign023.ExpeditionCardChoice091.ChoicePhase091.AwaitingChoice)
                {
                    pick.Select();
                    pick.onClick.Invoke();
                    break;
                }
                yield return null;
            }
            Require071(choice != null && choice.SelectedIndex091 >= 0,
                "The exact " + evidenceLabel + " mystery card was not pickable within 12 seconds.");
            while (Time.realtimeSinceStartup < deadline)
            {
                if (choice != null && choice.Phase091 ==
                    Campaign023.ExpeditionCardChoice091.ChoicePhase091.AwaitingAction)
                    yield break;
                yield return null;
            }
            Require071(false, "The selected " + evidenceLabel +
                " mystery card did not finish flipping before its explicit action.");
        }

        private static EarnedRecruitPlan089 FindNewRecruitPlan089(
            CampaignPlayableCommandService020 campaign020,
            CampaignWorldGateCommandService023 worldGate,
            ExpeditionDeckCommandService089 deck,
            Campaign020RuleCatalogAdapter catalog020,
            Campaign023RuleCatalogAdapter catalog023,
            HeroMaster300Catalog087 heroes,
            GuildCityRecruitmentService017D recruitment,
            RecruitmentContent recruitmentContent,
            GuildCityContent017D cityContent)
        {
            for (var attempt = 0; attempt < 256; attempt++)
            {
                var baseCampaign = CreateRecruitSmokeBase089(
                    89900 + attempt, campaign020, catalog020, recruitment,
                    recruitmentContent, cityContent);
                var begun = worldGate.BeginOperation(baseCampaign, catalog023,
                    RecruitSmokeChapter089, new[] { RecruitSmokeUnion089 },
                    catalog020, heroes);
                if (!begun.IsSuccess) continue;
                // Every shipping safe room now has two real three-card choices:
                // encounter first, then route.  Advance a legal safe encounter
                // through the same production commands before looking for the
                // authored Recruit route; inspecting only the initial row can
                // never find that scheduled route card.
                var state = begun.Value;
                var encounter = WorldGate089(state).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                        value != null && !value.AdvancesRoute &&
                        value.ResolutionDifficulty == 0 &&
                        value.TreasuryXpCost == 0 &&
                        !ExpeditionDeckService089.IsOptionalBattleCard089(value));
                if (encounter == null) continue;
                state = ResolveCard089(
                    state, encounter.CardId, deck, catalog023);
                if (state == null) continue;
                var card = WorldGate089(state).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                        StringComparer.Ordinal.Equals(value.Category, "RECRUIT") &&
                        StringComparer.Ordinal.Equals(value.RecruitOfferKind,
                            ExpeditionDeckService089.NewRecruitOfferKind089));
                if (card == null) continue;
                var applied = ResolveCard089(
                    state, card.CardId, deck, catalog023);
                if (applied == null || !WorldGate089(applied)
                        .ExpeditionRecruitLeadIds089.Contains(card.RecruitStableId) ||
                    !heroes.TryGetAcceptedHero(card.RecruitStableId, out var hero))
                    continue;
                return new EarnedRecruitPlan089
                {
                    BaseCampaign = baseCampaign,
                    Hero = hero,
                    TargetCardId = card.CardId,
                    AdvanceCardIds = new[] { encounter.CardId }
                };
            }
            return null;
        }

        private static EarnedRecruitPlan089 FindAscensionPlan089(
            CampaignState signedCampaign,
            HeroMaster300Hero087 hero,
            CampaignPlayableCommandService020 campaign020,
            CampaignWorldGateCommandService023 worldGate,
            ExpeditionDeckCommandService089 deck,
            Campaign020RuleCatalogAdapter catalog020,
            Campaign023RuleCatalogAdapter catalog023,
            HeroMaster300Catalog087 heroes)
        {
            for (var attempt = 0; attempt < 256; attempt++)
            {
                var baseCampaign = PrepareRecruitSmokeWorldBoard089(
                    signedCampaign, 90300 + attempt, campaign020, catalog020);
                var begun = worldGate.BeginOperation(baseCampaign, catalog023,
                    RecruitSmokeChapter089, new[] { RecruitSmokeUnion089 },
                    catalog020, heroes);
                if (!begun.IsSuccess) continue;
                // The matching copy may live several nodes into the authored
                // deck. Traverse both physical rows with production Commit/Apply
                // commands and branch only on real non-battle cards. This keeps
                // the evidence honest without manufacturing a lead or rewriting
                // the generated deck.
                var pending = new Queue<RecruitTraversalState089>();
                pending.Enqueue(new RecruitTraversalState089
                {
                    Campaign = begun.Value
                });
                var visited = new HashSet<string>(StringComparer.Ordinal);
                var expanded = 0;
                while (pending.Count > 0 && expanded++ < 192)
                {
                    var traversal = pending.Dequeue();
                    if (traversal.AdvanceCardIds.Count > 24) continue;
                    var operation = WorldGate089(traversal.Campaign)
                        ?.ActiveOperation;
                    var expeditionDeck = operation?.ExpeditionDeck089;
                    if (operation == null || expeditionDeck == null ||
                        expeditionDeck.CurrentRow.Count == 0) continue;
                    var phase = expeditionDeck.CurrentRow.All(value =>
                        value != null && value.AdvancesRoute)
                        ? "ROUTE" : "ENCOUNTER";
                    var visitKey = operation.CurrentNodeId + "|" + phase + "|" +
                                   expeditionDeck.Momentum;
                    if (!visited.Add(visitKey)) continue;

                    var target = expeditionDeck.CurrentRow.FirstOrDefault(value =>
                        value != null &&
                        StringComparer.Ordinal.Equals(value.RecruitStableId,
                            hero.StableId) &&
                        StringComparer.Ordinal.Equals(value.RecruitOfferKind,
                            ExpeditionDeckService089.AscensionOfferKind089));
                    if (target != null)
                    {
                        var applied = ResolveCard089(
                            traversal.Campaign, target.CardId, deck, catalog023);
                        if (applied != null && WorldGate089(applied)
                                .ExpeditionRecruitLeadIds089.Contains(hero.StableId))
                            return new EarnedRecruitPlan089
                            {
                                BaseCampaign = baseCampaign,
                                Hero = hero,
                                TargetCardId = target.CardId,
                                AdvanceCardIds = traversal.AdvanceCardIds
                            };
                        continue;
                    }

                    foreach (var card in expeditionDeck.CurrentRow
                                 .Where(IsRecruitTraversalCard089)
                                 .OrderBy(value => value.ResolutionDifficulty)
                                 .ThenBy(value => value.TreasuryXpCost)
                                 .ThenBy(value => value.CardId,
                                     StringComparer.Ordinal))
                    {
                        if (card.TreasuryXpCost >
                            traversal.Campaign.Guild.TreasuryXp) continue;
                        var advanced = ResolveCard089(
                            traversal.Campaign, card.CardId, deck, catalog023);
                        if (advanced == null) continue;
                        var cardIds = new List<string>(
                            traversal.AdvanceCardIds) { card.CardId };
                        pending.Enqueue(new RecruitTraversalState089
                        {
                            Campaign = advanced,
                            AdvanceCardIds = cardIds.AsReadOnly()
                        });
                    }
                }
            }
            return null;
        }

        private static bool IsRecruitTraversalCard089(
            ExpeditionRouteCardState089 card)
        {
            if (card == null ||
                ExpeditionDeckService089.IsOptionalBattleCard089(card) ||
                StringComparer.Ordinal.Equals(card.Category, "RECRUIT") ||
                !string.IsNullOrWhiteSpace(card.RecruitOfferKind))
                return false;
            return !StringComparer.Ordinal.Equals(card.Category, "BATTLE") &&
                   !StringComparer.Ordinal.Equals(card.Category, "AMBUSH") &&
                   !StringComparer.Ordinal.Equals(card.Category, "ELITE") &&
                   !StringComparer.Ordinal.Equals(card.Category, "BOSS");
        }

        private static CampaignState CreateRecruitSmokeBase089(
            long seed,
            CampaignPlayableCommandService020 campaign020,
            Campaign020RuleCatalogAdapter catalog020,
            GuildCityRecruitmentService017D recruitment,
            RecruitmentContent recruitmentContent,
            GuildCityContent017D cityContent)
        {
            var actor = new RecruitState(RecruitSmokeActor089, 120, 120, 30, 30);
            var assistant = new RecruitState(
                RecruitSmokeAssistant089, 110, 110, 30, 30);
            var union = new UnionState(
                RecruitSmokeUnion089, "Certification Union", UnionKind.Normal,
                actor.RecruitId, new[] { actor.RecruitId, assistant.RecruitId },
                "FORMATION_LINE", "DOCTRINE_BALANCED", 20, 8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(
                source.Guild.GuildId, 50000,
                new[] { actor, assistant }, new[] { union },
                source.Guild.Inventory, source.Guild.Development,
                guildCity: null);
            var campaign = source.With(guild, source.OpeningFlow);
            var board = recruitment.CommitBoard(
                campaign, recruitmentContent, cityContent);
            if (!board.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", board.Errors));
            return PrepareRecruitSmokeWorldBoard089(
                board.Value, seed, campaign020, catalog020);
        }

        private static CampaignState PrepareRecruitSmokeWorldBoard089(
            CampaignState campaign,
            long seed,
            CampaignPlayableCommandService020 campaign020,
            Campaign020RuleCatalogAdapter catalog020)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019.With(
                activeChapterId: RecruitSmokeChapter089,
                activeOperation: null,
                replaceActiveOperation: true,
                pendingReceipt: null,
                replacePendingReceipt: true,
                completedChapterIds: Array.Empty<string>(),
                unlockedWorldIds: new[] { "SKYHOME" },
                lastCheckpointId: "smoke_089_world_board_ready",
                playable020: CampaignPlayableState020.Default(),
                replacePlayable020: true);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            city = city.With(
                activeContract: null, replaceActiveContract: true,
                expedition: null, replaceExpedition: true,
                pendingEncounter: null, replacePendingEncounter: true,
                pendingBattleReturn: null, replacePendingBattleReturn: true,
                strategic017H: strategic, replaceStrategic017H: true,
                lastCheckpointId: progress.LastCheckpointId);
            campaign = new CampaignState(
                campaign.CampaignGuid, seed, campaign.ContentAuthorityVersion,
                campaign.Rules, campaign.Guild.WithGuildCity(city),
                campaign.Profile, campaign.OpeningFlow, battle: null);
            var begun = campaign020.BeginOperation(
                campaign, catalog020, RecruitSmokeChapter089);
            if (!begun.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", begun.Errors));
            var committed = campaign020.CommitNonBattleStep(
                begun.Value, catalog020, "SUCCESS");
            if (!committed.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", committed.Errors));
            var applied = campaign020.ApplyStepReceiptExactlyOnce(
                committed.Value, catalog020);
            if (!applied.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", applied.Errors));
            return applied.Value;
        }

        private static CampaignState ResolveCard089(
            CampaignState campaign,
            string cardId,
            ExpeditionDeckCommandService089 deck,
            Campaign023RuleCatalogAdapter catalog023)
        {
            var committed = deck.CommitRouteCard(
                campaign, catalog023, cardId,
                RecruitSmokeActor089, RecruitSmokeAssistant089);
            if (!committed.IsSuccess) return null;
            var applied = deck.ApplyWorldGateReceiptExactlyOnce(
                committed.Value, catalog023);
            return applied.IsSuccess ? applied.Value : null;
        }

        private static void WriteRecruitSmokeCampaign089(
            string path, CampaignState campaign) =>
            new AtomicSaveStore().Write(
                path, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));

        private static CampaignState ReadRecruitSmokeCampaign089(string path)
        {
            var loaded = new AtomicSaveStore().ReadWithRecovery(path);
            if (!loaded.IsSuccess)
                throw new InvalidOperationException(string.Join("; ", loaded.Errors));
            return loaded.Value.CampaignState;
        }

        private static void RequireCommand089(
            M1CommandResult result, string action)
        {
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException(
                    "Could not " + action + ": " + (result?.Message ?? "no result"));
        }

        private static WorldGateRuntimeState023 WorldGate089(
            CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023;

        private static RecruitState FindOwnedHero089(
            CampaignState campaign, HeroMaster300Hero087 hero)
        {
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return campaign.Guild.Recruits.Single(value => value != null &&
                (StringComparer.Ordinal.Equals(value.RecruitId, projectedId) ||
                 StringComparer.Ordinal.Equals(value.RecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.RecruitId, hero.GameEntityId) ||
                 StringComparer.Ordinal.Equals(value.AuthoredStableRecruitId,
                     hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.SignatureId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.SignatureId,
                     hero.GameEntityId)));
        }

        private static int HeroRosterCount089(
            CampaignState campaign, HeroMaster300Hero087 hero)
        {
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return campaign.Guild.Recruits.Count(value => value != null &&
                (StringComparer.Ordinal.Equals(value.RecruitId, projectedId) ||
                 StringComparer.Ordinal.Equals(value.RecruitId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.RecruitId, hero.GameEntityId) ||
                 StringComparer.Ordinal.Equals(value.AuthoredStableRecruitId,
                     hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.SignatureId, hero.StableId) ||
                 StringComparer.Ordinal.Equals(value.SignatureId,
                     hero.GameEntityId)));
        }
    }
}
