#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Progression070;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ExpeditionRecruitLeadApplicant089Tests
    {
        CampaignRegistry020 _registry020;
        Campaign020RuleCatalogAdapter _catalog020;
        CampaignRegistry023 _registry023;
        Campaign023RuleCatalogAdapter _catalog023;
        CampaignPlayableCommandService020 _campaign020;
        CampaignWorldGateCommandService023 _worldGate;
        ExpeditionDeckCommandService089 _deckCommands;
        GuildCityRecruitmentService017D _recruitment;
        RecruitmentContent _recruitmentContent;
        GuildCityContent017D _cityContent;
        HeroMaster300Catalog087 _heroes;
        RecruitAutoGenerator010 _generator;
        DeepProgressionCatalog070 _deepProgression;
        RecruitTreeProgressionService070 _trees;
        HeroMaster300DeepProgressionAdapter089 _heroMasterProgression;

        [SetUp]
        public void SetUp()
        {
            _registry020 = CampaignRegistry020.LoadFromResources();
            _catalog020 = new Campaign020RuleCatalogAdapter(_registry020);
            _registry023 = CampaignRegistry023.LoadFromResources();
            _catalog023 = new Campaign023RuleCatalogAdapter(_registry023);
            _campaign020 = new CampaignPlayableCommandService020();
            _worldGate = new CampaignWorldGateCommandService023();
            _deckCommands = new ExpeditionDeckCommandService089(
                _worldGate,
                new ExpeditionDeckService089());
            _heroes = HeroMaster300CreatorRegistry087.Load().Source;

            var contentRoot = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            _recruitmentContent = RecruitmentContent.LoadFromDirectory(contentRoot);
            _cityContent = GuildCityContent017D.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017D"));
            _generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(contentRoot));
            _deepProgression = DeepProgressionCatalog070.LoadFromContentRoot(
                contentRoot);
            _trees = new RecruitTreeProgressionService070(_deepProgression);
            _heroMasterProgression =
                new HeroMaster300DeepProgressionAdapter089(
                    _heroes,
                    _generator,
                    _trees);
            _recruitment = new GuildCityRecruitmentService017D(
                _generator,
                _heroes,
                _trees);
        }

        [Test]
        public void EveryAcceptedHeroMasterProfileUsesCanonicalGeneratedTreesForAllAuthoredAliases()
        {
            Assert.That(_heroes.AcceptedHeroes.Count(), Is.EqualTo(250));
            Assert.That(_heroes.AcceptedHeroes
                    .Select(value => value.ArtTree1)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(10),
                "All ten observed Hero Master art_tree_1 labels remain covered.");
            Assert.That(_heroes.AcceptedHeroes
                    .Select(value => value.ArtTree2)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(6),
                "All six observed Hero Master art_tree_2 labels remain covered.");

            foreach (var hero in _heroes.AcceptedHeroes)
            {
                var record = hero.Rank == HeroMasterRank087.SS
                    ? HeroMaster300CreatorRecruitProjection087
                        .BuildOpeningRecord(hero)
                    : HeroMaster300CreatorRecruitProjection087
                        .BuildExpeditionApplicantRecord089(hero);
                var profile = _generator.Generate(record);
                IReadOnlyList<string> assigned = null;
                Assert.DoesNotThrow(() => assigned = _heroMasterProgression
                    .ValidateGeneratedProfile089(profile), hero.StableId);
                Assert.That(assigned, Has.Count.GreaterThanOrEqualTo(2),
                    hero.StableId + " must retain canonical Weapon and Primary Role foundations.");

                foreach (var treeId in profile.LegalTreeIds.Concat(assigned)
                             .Distinct(StringComparer.Ordinal))
                {
                    Assert.That(_deepProgression.TryTree(treeId, out _), Is.True,
                        hero.StableId + " generated unknown tree " + treeId);
                    Assert.That(treeId, Does.StartWith("TREE_CA002_"),
                        "Only canonical DeepProgression070 IDs may reach gameplay.");
                    Assert.That(treeId,
                        Is.Not.EqualTo(hero.ArtTree1).And.Not.EqualTo(hero.ArtTree2),
                        "Hero Master TREE_* labels are metadata, not saved progression IDs.");
                }
            }
        }

        [Test]
        public void ResolveRecruitCardPersistsExactLeadAndApplicantBoardSignsItOnce()
        {
            CampaignState rewarded = null;
            ExpeditionRouteCardState089 selected = null;
            for (var attempt = 0; attempt < 256 && rewarded == null; attempt++)
            {
                var candidate = CreateAtWorldBoard(89500 + attempt);
                candidate = Require(_worldGate.BeginOperation(
                    candidate,
                    _catalog023,
                    "CH018_001",
                    new[] { "LEAD_UNION_089" },
                    _catalog020,
                    _heroes));
                var encounterCard = WorldGate(candidate).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                        !value.AdvancesRoute &&
                        string.IsNullOrWhiteSpace(value.EncounterId) &&
                        value.TreasuryXpCost == 0);
                if (encounterCard == null) continue;
                var encounterCommit = _deckCommands.CommitRouteCard(
                    candidate,
                    _catalog023,
                    encounterCard.CardId,
                    "LEAD_RECRUIT_A_089",
                    "LEAD_RECRUIT_B_089");
                if (!encounterCommit.IsSuccess) continue;
                var encounterApplied = _deckCommands
                    .ApplyWorldGateReceiptExactlyOnce(
                        encounterCommit.Value,
                        _catalog023);
                if (!encounterApplied.IsSuccess) continue;
                candidate = encounterApplied.Value;
                var recruitCard = WorldGate(candidate).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.FirstOrDefault(value =>
                        value.AdvancesRoute &&
                        StringComparer.Ordinal.Equals(value.Category, "RECRUIT"));
                if (recruitCard == null) continue;
                var committed = _deckCommands.CommitRouteCard(
                    candidate,
                    _catalog023,
                    recruitCard.CardId,
                    "LEAD_RECRUIT_A_089",
                    "LEAD_RECRUIT_B_089");
                if (!committed.IsSuccess) continue;
                var receipt = WorldGate(committed.Value).ActiveOperation
                    .ExpeditionDeck089.PendingReceipt;
                if (receipt == null || string.IsNullOrWhiteSpace(
                        receipt.RecruitStableId)) continue;
                var applied = _deckCommands.ApplyWorldGateReceiptExactlyOnce(
                    committed.Value,
                    _catalog023);
                if (!applied.IsSuccess || !WorldGate(applied.Value)
                        .ExpeditionRecruitLeadIds089.Contains(
                            receipt.RecruitStableId)) continue;
                rewarded = applied.Value;
                selected = recruitCard;
            }

            Assert.That(rewarded, Is.Not.Null,
                "The bounded deterministic route sample must include a successful Recruit card.");
            Assert.That(selected, Is.Not.Null);
            Assert.That(_heroes.TryGetAcceptedHero(
                selected.RecruitStableId,
                out var exactHero), Is.True);
            Assert.That(exactHero.IsNormalApplicantEligible, Is.True);
            Assert.That(exactHero.Name, Is.EqualTo(selected.RecruitName));
            Assert.That(WorldGate(rewarded).ExpeditionRecruitLeadIds089.Count(
                value => StringComparer.Ordinal.Equals(
                    value,
                    exactHero.StableId)), Is.EqualTo(1));

            var reloadedLead = Reload(rewarded);
            Assert.That(WorldGate(reloadedLead).ExpeditionRecruitLeadIds089,
                Does.Contain(exactHero.StableId));
            var existingBoard = reloadedLead.Guild.GuildCity.RecruitmentBoard;
            Assert.That(existingBoard, Is.Not.Null);
            Assert.That(existingBoard.Applicants.Any(value =>
                StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId,
                    exactHero.StableId)), Is.False,
                "The mission lead is earned after the current board was committed.");
            Assert.That(_recruitment.PendingExpeditionRecruitLeads089(reloadedLead)
                .Select(value => value.StableId), Does.Contain(exactHero.StableId));

            var boardCampaign = Require(_recruitment.CommitBoard(
                reloadedLead,
                _recruitmentContent,
                _cityContent));
            var exactOffers = boardCampaign.Guild.GuildCity.RecruitmentBoard
                .Applicants.Where(value => StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId,
                    exactHero.StableId)).ToArray();
            Assert.That(exactOffers, Has.Length.EqualTo(1));
            Assert.That(boardCampaign.Guild.GuildCity.RecruitmentBoard.BoardId,
                Is.EqualTo(existingBoard.BoardId),
                "Inviting a mission contact must preserve the current board rather than create a second recruitment engine.");
            Assert.That(exactOffers[0].DisplayName, Is.EqualTo(exactHero.Name));
            Assert.That(exactOffers[0].RecruitId,
                Is.EqualTo(HeroMaster300CreatorRecruitProjection087
                    .ExpeditionApplicantRecruitIdFor089(exactHero)));
            Assert.That(exactOffers[0].SigningCostTreasuryXp, Is.GreaterThan(0));
            Assert.That(exactOffers[0].SigningCostTreasuryXp,
                Is.EqualTo(exactHero.RecruitCostXp),
                "The existing Hero Master cost authority must remain intact.");
            Assert.That(_recruitment.PendingExpeditionRecruitLeads089(boardCampaign),
                Is.Empty,
                "A lead already displayed on the board is not a second pending offer.");

            var reloadedBoard = Reload(boardCampaign);
            var duplicateCommit = Require(_recruitment.CommitBoard(
                reloadedBoard,
                _recruitmentContent,
                _cityContent));
            Assert.That(duplicateCommit.Guild.GuildCity.RecruitmentBoard.Applicants
                .Count(value => StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId,
                    exactHero.StableId)), Is.EqualTo(1),
                "Reopening the Applicant Board must never create a simultaneous duplicate offer.");

            var signed = Require(_recruitment.SignApplicant(
                duplicateCommit,
                exactOffers[0].RecruitId));
            Assert.That(signed.Guild.Recruits.Count(value =>
                StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId,
                    exactHero.StableId)), Is.EqualTo(1));
            Assert.That(WorldGate(signed).ExpeditionRecruitLeadIds089,
                Does.Not.Contain(exactHero.StableId),
                "Successful signing consumes the pending lead authority.");

            var signedReload = Reload(signed);
            var replay = Require(_recruitment.SignApplicant(
                signedReload,
                exactOffers[0].RecruitId));
            Assert.That(replay.Guild.Recruits.Count(value =>
                StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId,
                    exactHero.StableId)), Is.EqualTo(1));
            Assert.That(WorldGate(replay).ExpeditionRecruitLeadIds089,
                Does.Not.Contain(exactHero.StableId));

            var refreshed = Require(_recruitment.RefreshBoard(
                replay,
                _recruitmentContent,
                _cityContent));
            Assert.That(refreshed, Is.SameAs(replay),
                "With no additional earned lead, refresh retains the signed history without charging or rerolling.");
            Assert.That(_recruitment.DescribeHeroMasterDuplicate089(refreshed, exactOffers[0]).IsDuplicateOffer, Is.False,
                "A consumed lead retained as signed history must not become another duplicate entitlement.");
        }

        [Test]
        public void LegacyProgressionDefaultsToZeroAscensionWithoutChangingCanonicalJson()
        {
            var legacy = RecruitProgressionState.Default();
            var legacyJson = CanonicalJson.Serialize(legacy);
            var legacyHash = CanonicalJson.Sha256Hex(legacy);

            Assert.That(legacyJson, Does.Not.Contain("AscensionLevel"));
            var restored = JsonConvert.DeserializeObject<RecruitProgressionState>(
                legacyJson);
            Assert.That(restored.AscensionLevel, Is.Zero);
            Assert.That(CanonicalJson.Sha256Hex(restored), Is.EqualTo(legacyHash),
                "An existing V11 save must retain its canonical hash after reload.");

            var ascended = restored.Ascend089();
            Assert.That(ascended.AscensionLevel, Is.EqualTo(1));
            Assert.That(CanonicalJson.Serialize(ascended),
                Does.Contain("AscensionLevel"));
            Assert.That(ascended.GainPersonalXp(1, "VANGUARD").AscensionLevel,
                Is.EqualTo(1));
            Assert.That(ascended.WithArts(
                ascended.LearnedArtIds,
                ascended.ArtMastery).AscensionLevel, Is.EqualTo(1));
            Assert.That(ascended.WithUnlockedTrees(
                ascended.UnlockedTreeIds).AscensionLevel, Is.EqualTo(1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RecruitProgressionState(
                    1, 0, 0, 0, 0, 0, 0, 0, 0,
                    Array.Empty<string>(),
                    Array.Empty<RecruitArtMasteryState>(),
                    Array.Empty<string>(),
                    RecruitAscensionRules089.MaximumLevel + 1));
        }

        [Test]
        public void TenDuplicateLeadsAscendExactHeroAcrossReloadWithoutRosterGrowthOrReplay()
        {
            var campaign = SignFreshHeroMaster089(
                89600, out var hero, out var applicant);
            var rosterCount = campaign.Guild.Recruits.Count;
            var initial = FindOwnedHero089(campaign, hero).Progression;

            for (var expectedRank = 1;
                 expectedRank <= RecruitAscensionRules089.MaximumLevel;
                 expectedRank++)
            {
                campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
                var forecast = _recruitment.DescribeHeroMasterDuplicate089(
                    campaign, applicant);
                Assert.That(forecast.Kind,
                    Is.EqualTo(HeroMasterDuplicateMergeKind089.Ascension));
                Assert.That(forecast.PreviousAscensionLevel,
                    Is.EqualTo(expectedRank - 1));
                Assert.That(forecast.AscensionLevel, Is.EqualTo(expectedRank));
                Assert.That(forecast.Summary,
                    Does.Contain("ASCENSION " + expectedRank + "/" +
                                 RecruitAscensionRules089.MaximumLevel));

                var beforeTreasury = campaign.Guild.TreasuryXp;
                var expectedCost = GuildCityRecruitmentService017D
                    .EffectiveSigningCostTreasuryXp(
                        campaign, applicant.SigningCostTreasuryXp);
                var merged = Require(_recruitment.SignApplicant(
                    campaign, applicant.RecruitId));
                Assert.That(merged.Guild.TreasuryXp,
                    Is.EqualTo(beforeTreasury - expectedCost));
                Assert.That(merged.Guild.Recruits.Count, Is.EqualTo(rosterCount));
                Assert.That(HeroRosterCount089(merged, hero), Is.EqualTo(1));
                Assert.That(FindOwnedHero089(merged, hero).Progression.AscensionLevel,
                    Is.EqualTo(expectedRank));
                Assert.That(WorldGate(merged).ExpeditionRecruitLeadIds089,
                    Does.Not.Contain(hero.StableId));

                var replayTreasury = merged.Guild.TreasuryXp;
                var replay = Require(_recruitment.SignApplicant(
                    merged, applicant.RecruitId));
                Assert.That(replay.Guild.TreasuryXp, Is.EqualTo(replayTreasury));
                Assert.That(FindOwnedHero089(replay, hero).Progression.AscensionLevel,
                    Is.EqualTo(expectedRank),
                    "Replaying the same signing action must not merge twice.");
                campaign = Reload(replay);
            }

            var final = FindOwnedHero089(campaign, hero).Progression;
            Assert.That(final.MaximumHpBonus - initial.MaximumHpBonus,
                Is.EqualTo(RecruitAscensionRules089.MaximumHpBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.MaximumMpBonus - initial.MaximumMpBonus,
                Is.EqualTo(RecruitAscensionRules089.MaximumMpBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.StrengthBonus - initial.StrengthBonus,
                Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.DefenseBonus - initial.DefenseBonus,
                Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.AgilityBonus - initial.AgilityBonus,
                Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.MagicBonus - initial.MagicBonus,
                Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
            Assert.That(final.WillBonus - initial.WillBonus,
                Is.EqualTo(RecruitAscensionRules089.CoreStatBonusPerLevel *
                           RecruitAscensionRules089.MaximumLevel));
        }

        [Test]
        public void PostCapDuplicatesUnlockAssignedTreesThenLevelArtWithDeterministicFallback()
        {
            var hero = _heroes.AcceptedHeroes.Single(value =>
                StringComparer.Ordinal.Equals(value.StableId, "HERO_REC_012"));
            var campaign = SignHeroMaster089(
                89700, hero, out var applicant);
            for (var rank = 1; rank <= RecruitAscensionRules089.MaximumLevel; rank++)
            {
                campaign = WithExpeditionLead089(campaign, hero.StableId);
                campaign = Require(_recruitment.SignApplicant(
                    campaign, applicant.RecruitId));
            }

            var rosterCount = campaign.Guild.Recruits.Count;
            campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
            var beforeFirstPostCap = FindOwnedHero089(campaign, hero);
            var generated = _heroMasterProgression.DescribeValidatedProfile089(
                beforeFirstPostCap);
            var expectedFirstTree = _heroMasterProgression
                .EarnableTreeIds089(generated)
                .First(value => !beforeFirstPostCap.Progression
                    .UnlockedTreeIds.Contains(value));
            var firstPostCap = _recruitment.DescribeHeroMasterDuplicate089(
                campaign, applicant);
            Assert.That(firstPostCap.Kind,
                Is.EqualTo(HeroMasterDuplicateMergeKind089.TreeUnlocked));
            Assert.That(firstPostCap.TreeId, Is.EqualTo(expectedFirstTree));
            Assert.That(firstPostCap.TreeId,
                Is.Not.EqualTo(hero.ArtTree1).And.Not.EqualTo(hero.ArtTree2),
                "Raw Hero Master aliases must never enter saved progression.");
            var expectedRoot = _deepProgression.Tree(expectedFirstTree).RootNodeId;
            Assert.That(beforeFirstPostCap.Progression.LearnedArtIds,
                Does.Not.Contain(expectedRoot));
            campaign = Require(_recruitment.SignApplicant(
                campaign, applicant.RecruitId));
            var afterFirstPostCap = FindOwnedHero089(Reload(campaign), hero);
            Assert.That(afterFirstPostCap.Progression.UnlockedTreeIds,
                Does.Contain(expectedFirstTree));
            Assert.That(afterFirstPostCap.Progression.LearnedArtIds,
                Does.Contain(expectedRoot),
                "Unlocking a generated tree must seed its existing canonical root Art.");

            var sawArt = false;
            for (var merge = 0; merge < 4 && !sawArt; merge++)
            {
                campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
                var forecast = _recruitment.DescribeHeroMasterDuplicate089(
                    campaign, applicant);
                if (forecast.Kind == HeroMasterDuplicateMergeKind089.TreeUnlocked)
                {
                    Assert.That(FindOwnedHero089(campaign, hero).Progression
                        .UnlockedTreeIds, Does.Not.Contain(forecast.TreeId));
                }
                else if (forecast.Kind == HeroMasterDuplicateMergeKind089.ArtLeveled)
                {
                    sawArt = true;
                    Assert.That(forecast.ArtLevel,
                        Is.EqualTo(forecast.PreviousArtLevel + 1));
                    Assert.That(forecast.ArtLevel,
                        Is.LessThanOrEqualTo(M2ArtMasteryLevelPolicy088.MaximumLevel));
                }
                else
                {
                    Assert.Fail("Unexpected post-cap merge: " + forecast.Kind);
                }

                campaign = Require(_recruitment.SignApplicant(
                    campaign, applicant.RecruitId));
                var saved = FindOwnedHero089(Reload(campaign), hero).Progression;
                if (forecast.Kind == HeroMasterDuplicateMergeKind089.TreeUnlocked)
                    Assert.That(saved.UnlockedTreeIds,
                        Does.Contain(forecast.TreeId));
                else
                {
                    var mastery = saved.ArtMastery.Single(value =>
                        value.ArtId == forecast.ArtId);
                    Assert.That(M2ArtMasteryLevelPolicy088.LevelForMasteryPoints(
                        mastery.MasteryPoints), Is.EqualTo(forecast.ArtLevel));
                }
                Assert.That(campaign.Guild.Recruits.Count, Is.EqualTo(rosterCount));
                Assert.That(HeroRosterCount089(campaign, hero), Is.EqualTo(1));
            }
            Assert.That(sawArt, Is.True,
                "After assigned trees are open, a copy should level one legal learned Art.");

            var owned = FindOwnedHero089(campaign, hero);
            var capped = owned.Progression;
            foreach (var treeId in _heroMasterProgression
                         .EarnableTreeIds089(
                             _heroMasterProgression
                                 .DescribeValidatedProfile089(owned)))
                if (!capped.UnlockedTreeIds.Contains(treeId))
                    capped = _heroMasterProgression.UnlockEarnableTree089(
                        owned,
                        capped,
                        treeId);
            var maximumMastery = capped.LearnedArtIds.Select(artId =>
            {
                var existing = capped.ArtMastery.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.ArtId, artId));
                return new RecruitArtMasteryState(
                    artId,
                    existing?.Discipline ?? string.Empty,
                    existing?.MeaningfulUses ?? 0,
                    M2ArtMasteryLevelPolicy088.ThresholdForLevel(
                        M2ArtMasteryLevelPolicy088.MaximumLevel));
            }).ToArray();
            capped = capped.WithArts(capped.LearnedArtIds, maximumMastery);
            campaign = ReplaceRecruit089(campaign, owned.WithProgression(capped));
            campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
            var fallback = _recruitment.DescribeHeroMasterDuplicate089(
                campaign, applicant);
            Assert.That(fallback.Kind,
                Is.EqualTo(HeroMasterDuplicateMergeKind089.VeteranTraining));
            Assert.That(fallback.PersonalXpGain,
                Is.EqualTo(RecruitAscensionRules089.FullyMasteredPersonalXpFallback));
            var beforeXp = FindOwnedHero089(campaign, hero).Progression.TotalPersonalXp;
            campaign = Require(_recruitment.SignApplicant(
                campaign, applicant.RecruitId));
            var persistedFallback = FindOwnedHero089(Reload(campaign), hero);
            Assert.That(persistedFallback.Progression.TotalPersonalXp,
                Is.EqualTo(beforeXp +
                           RecruitAscensionRules089.FullyMasteredPersonalXpFallback));
            Assert.That(HeroRosterCount089(campaign, hero), Is.EqualTo(1));
        }

        [Test]
        public void PostCapHeroWithoutLegacyTreePlanStillProducesDuplicateForecast()
        {
            var hero = _heroes.AcceptedHeroes.Single(value =>
                StringComparer.Ordinal.Equals(value.StableId, "HERO_REC_011"));
            var campaign = WithExpeditionLead089(
                CreateAtWorldBoard(89750), hero.StableId);
            campaign = Require(_recruitment.CommitBoard(
                campaign, _recruitmentContent, _cityContent));
            var applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .Single(value => StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId, hero.StableId));
            campaign = Require(_recruitment.SignApplicant(
                campaign, applicant.RecruitId));

            for (var rank = 1; rank <= RecruitAscensionRules089.MaximumLevel; rank++)
            {
                campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
                campaign = Require(_recruitment.SignApplicant(
                    campaign, applicant.RecruitId));
            }

            campaign = Reload(WithExpeditionLead089(campaign, hero.StableId));
            HeroMasterDuplicateMergeForecast089 forecast = null;
            Assert.DoesNotThrow(() => forecast =
                _recruitment.DescribeHeroMasterDuplicate089(campaign, applicant));
            Assert.That(forecast, Is.Not.Null);
            Assert.That(forecast.IsDuplicateOffer, Is.True);
            Assert.That(forecast.Kind,
                Is.EqualTo(HeroMasterDuplicateMergeKind089.TreeUnlocked));
            Assert.That(_deepProgression.TryTree(forecast.TreeId, out var tree),
                Is.True);
            campaign = Require(_recruitment.SignApplicant(
                campaign, applicant.RecruitId));
            var persisted = FindOwnedHero089(Reload(campaign), hero).Progression;
            Assert.That(persisted.UnlockedTreeIds, Does.Contain(forecast.TreeId));
            Assert.That(persisted.LearnedArtIds, Does.Contain(tree.RootNodeId));
            Assert.That(HeroRosterCount089(campaign, hero), Is.EqualTo(1));
        }

        [Test]
        public void GeneratedTreeAdapterRejectsUnknownAndUnassignedUnlocks()
        {
            var hero = _heroes.AcceptedHeroes.Single(value =>
                StringComparer.Ordinal.Equals(value.StableId, "HERO_REC_012"));
            var campaign = SignHeroMaster089(
                89780, hero, out _);
            var owned = FindOwnedHero089(campaign, hero);
            var profile = _heroMasterProgression.DescribeValidatedProfile089(owned);
            var assigned = _heroMasterProgression
                .ValidateGeneratedProfile089(profile);
            var unassignedLegalTree = profile.LegalTreeIds.First(value =>
                !assigned.Contains(value));

            Assert.Throws<KeyNotFoundException>(() =>
                _heroMasterProgression.UnlockEarnableTree089(
                    owned,
                    owned.Progression,
                    "TREE_HERO_MASTER_ALIAS_IS_NOT_CANONICAL"));
            Assert.Throws<InvalidOperationException>(() =>
                _heroMasterProgression.UnlockEarnableTree089(
                    owned,
                    owned.Progression,
                    profile.WeaponTreeId),
                "A duplicate cannot re-grant an initially open Weapon foundation.");
            Assert.Throws<InvalidOperationException>(() =>
                _heroMasterProgression.UnlockEarnableTree089(
                    owned,
                    owned.Progression,
                    unassignedLegalTree),
                "A class-legal tree that is not assigned to this generated build must be rejected.");
        }

        CampaignState SignFreshHeroMaster089(
            long seed,
            out HeroMaster300Hero087 hero,
            out ApplicantSnapshotState applicant)
        {
            hero = _heroes.NormalApplicantCandidates
                .Where(value => value.RecruitCostXp <= 1200)
                .OrderBy(value => value.StableId, StringComparer.Ordinal)
                .First();
            return SignHeroMaster089(seed, hero, out applicant);
        }

        CampaignState SignHeroMaster089(
            long seed,
            HeroMaster300Hero087 hero,
            out ApplicantSnapshotState applicant)
        {
            var campaign = WithExpeditionLead089(
                CreateAtWorldBoard(seed), hero.StableId);
            campaign = Require(_recruitment.CommitBoard(
                campaign, _recruitmentContent, _cityContent));
            applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants
                .Single(value => StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId, hero.StableId));
            campaign = Require(_recruitment.SignApplicant(
                campaign, applicant.RecruitId));
            Assert.That(HeroRosterCount089(campaign, hero), Is.EqualTo(1));
            Assert.That(WorldGate(campaign).ExpeditionRecruitLeadIds089,
                Does.Not.Contain(hero.StableId));
            return Reload(campaign);
        }

        static CampaignState WithExpeditionLead089(
            CampaignState campaign,
            string heroStableId)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var progress = strategic.Campaign019;
            var playable = progress.Playable020;
            var runtime = playable.WorldGate023;
            var leads = runtime.ExpeditionRecruitLeadIds089
                .Concat(new[] { heroStableId })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            runtime = runtime.With(expeditionRecruitLeadIds089: leads);
            playable = playable.With(
                worldGate023: runtime,
                replaceWorldGate023: true);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true);
            return campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }

        static CampaignState ReplaceRecruit089(
            CampaignState campaign,
            RecruitState replacement)
        {
            var recruits = campaign.Guild.Recruits.ToList();
            var index = recruits.FindIndex(value =>
                StringComparer.Ordinal.Equals(
                    value.RecruitId, replacement.RecruitId));
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            recruits[index] = replacement;
            return campaign.With(
                campaign.Guild.With(
                    campaign.Guild.TreasuryXp,
                    recruits.AsReadOnly(),
                    campaign.Guild.Unions,
                    campaign.Guild.Inventory),
                campaign.OpeningFlow);
        }

        static RecruitState FindOwnedHero089(
            CampaignState campaign,
            HeroMaster300Hero087 hero)
        {
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return campaign.Guild.Recruits.Single(recruit =>
                StringComparer.Ordinal.Equals(recruit.RecruitId, projectedId) ||
                StringComparer.Ordinal.Equals(recruit.RecruitId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.RecruitId, hero.GameEntityId) ||
                StringComparer.Ordinal.Equals(
                    recruit.AuthoredStableRecruitId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.SignatureId, hero.GameEntityId));
        }

        static int HeroRosterCount089(
            CampaignState campaign,
            HeroMaster300Hero087 hero)
        {
            var projectedId = HeroMaster300CreatorRecruitProjection087
                .ExpeditionApplicantRecruitIdFor089(hero);
            return campaign.Guild.Recruits.Count(recruit =>
                StringComparer.Ordinal.Equals(recruit.RecruitId, projectedId) ||
                StringComparer.Ordinal.Equals(recruit.RecruitId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.RecruitId, hero.GameEntityId) ||
                StringComparer.Ordinal.Equals(
                    recruit.AuthoredStableRecruitId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.SignatureId, hero.StableId) ||
                StringComparer.Ordinal.Equals(recruit.SignatureId, hero.GameEntityId));
        }

        CampaignState CreateAtWorldBoard(long seed)
        {
            var recruitA = new RecruitState(
                "LEAD_RECRUIT_A_089", 100, 100, 20, 20);
            var recruitB = new RecruitState(
                "LEAD_RECRUIT_B_089", 100, 100, 20, 20);
            var union = new UnionState(
                "LEAD_UNION_089",
                "Lead Test Union",
                UnionKind.Normal,
                recruitA.RecruitId,
                new[] { recruitA.RecruitId, recruitB.RecruitId },
                "FORMATION_LINE",
                "DOCTRINE_BALANCED",
                20,
                8000);
            var source = CampaignFactory.CreateM0Proof(seed);
            var guild = new GuildState(
                source.Guild.GuildId,
                500000,
                new[] { recruitA, recruitB },
                new[] { union },
                source.Guild.Inventory,
                source.Guild.Development,
                guildCity: null);
            var campaign = source.With(guild, source.OpeningFlow);
            var city = campaign.Guild.GuildCity;
            var progress = city.Strategic017H.Campaign019.With(
                activeChapterId: "CH018_001",
                unlockedWorldIds: new[] { "SKYHOME" },
                lastCheckpointId: "chapter_active_recruit_lead_089");
            var strategic = city.Strategic017H.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: progress.LastCheckpointId);
            campaign = campaign.With(
                campaign.Guild.WithGuildCity(city.With(
                    strategic017H: strategic,
                    replaceStrategic017H: true,
                    lastCheckpointId: progress.LastCheckpointId)),
                campaign.OpeningFlow);
            campaign = Require(LegacyApplicantBoardFixture124.Commit(
                campaign,
                _recruitmentContent,
                _cityContent));
            campaign = Require(_campaign020.BeginOperation(
                campaign,
                _catalog020,
                "CH018_001"));
            campaign = Require(_campaign020.CommitNonBattleStep(
                campaign,
                _catalog020,
                "SUCCESS"));
            return Require(_campaign020.ApplyStepReceiptExactlyOnce(
                campaign,
                _catalog020));
        }

        static WorldGateRuntimeState023 WorldGate(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023;

        static CampaignState Reload(CampaignState campaign) =>
            JsonConvert.DeserializeObject<CampaignState>(
                JsonConvert.SerializeObject(campaign));

        static CampaignState Require(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True, string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
