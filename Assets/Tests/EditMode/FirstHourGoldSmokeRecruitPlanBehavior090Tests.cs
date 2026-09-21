#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourGoldSmokeRecruitPlanBehavior090Tests
    {
        [Test]
        public void PlanFindersReplayBothPhysicalRowsThroughProductionAuthorities090()
        {
            var registry020 = CampaignRegistry020.LoadFromResources();
            var catalog020 = new Campaign020RuleCatalogAdapter(registry020);
            var registry023 = CampaignRegistry023.LoadFromResources();
            var catalog023 = new Campaign023RuleCatalogAdapter(registry023);
            var campaign020 = new CampaignPlayableCommandService020();
            var worldGate = new CampaignWorldGateCommandService023();
            var deck = new ExpeditionDeckCommandService089(
                worldGate, new ExpeditionDeckService089());
            var heroes = HeroMaster300CreatorRegistry087.Load().Source;
            var contentRoot = Path.Combine(
                Application.streamingAssetsPath, "Authority", "CONTENT");
            var recruitmentContent = RecruitmentContent.LoadFromDirectory(
                contentRoot);
            var cityContent = GuildCityContent017D.LoadFromDirectory(
                Path.Combine(contentRoot, "GUILD_CITY_017D"));
            var recruitment = new GuildCityRecruitmentService017D(
                new RecruitAutoGenerator010(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(
                        contentRoot)),
                heroes);

            var firstPlan = InvokePlanFinder090(
                "FindNewRecruitPlan089",
                campaign020, worldGate, deck, catalog020, catalog023, heroes,
                recruitment, recruitmentContent, cityContent);
            Assert.That(firstPlan, Is.Not.Null,
                "The smoke sampler must find a real successful Recruit route after the encounter row.");
            var firstBase = Field090<CampaignState>(firstPlan, "BaseCampaign");
            var hero = Field090<HeroMaster300Hero087>(firstPlan, "Hero");
            var firstTargetId = Field090<string>(firstPlan, "TargetCardId");
            var firstAdvances = Field090<IReadOnlyList<string>>(
                firstPlan, "AdvanceCardIds");
            Assert.That(firstAdvances.Count, Is.EqualTo(1));
            Assert.That(firstTargetId, Is.Not.EqualTo(firstAdvances[0]));

            var recruitBoard = Require090(worldGate.BeginOperation(
                firstBase, catalog023, "CH018_001",
                new[] { "RECRUIT_SMOKE_UNION_089" }, catalog020, heroes));
            var initialDeck = WorldGate090(recruitBoard).ActiveOperation
                .ExpeditionDeck089;
            Assert.That(initialDeck.CurrentRow, Is.Not.Empty);
            Assert.That(initialDeck.CurrentRow.All(value => !value.AdvancesRoute),
                Is.True, "A shipping room must expose its encounter row first.");
            Assert.That(initialDeck.CurrentRow.Any(value =>
                StringComparer.Ordinal.Equals(value.Category, "RECRUIT")),
                Is.False, "Recruit is scheduled in the route row, not the initial encounter row.");

            recruitBoard = ReplayCard090(
                recruitBoard, firstAdvances[0], deck, catalog023);
            var recruitRouteDeck = WorldGate090(recruitBoard).ActiveOperation
                .ExpeditionDeck089;
            Assert.That(recruitRouteDeck.CurrentRow.All(value =>
                value.AdvancesRoute), Is.True);
            var recruitTarget = recruitRouteDeck.CurrentRow.SingleOrDefault(
                value => StringComparer.Ordinal.Equals(
                    value.CardId, firstTargetId));
            Assert.That(recruitTarget, Is.Not.Null);
            Assert.That(recruitTarget.RecruitStableId,
                Is.EqualTo(hero.StableId));
            Assert.That(recruitTarget.RecruitOfferKind,
                Is.EqualTo(ExpeditionDeckService089.NewRecruitOfferKind089));
            var rewarded = ReplayCard090(
                recruitBoard, firstTargetId, deck, catalog023);
            Assert.That(WorldGate090(rewarded).ExpeditionRecruitLeadIds089,
                Does.Contain(hero.StableId));

            var applicantBoard = Require090(recruitment.CommitBoard(
                rewarded, recruitmentContent, cityContent));
            var applicant = applicantBoard.Guild.GuildCity.RecruitmentBoard
                .Applicants.Single(value => StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId, hero.StableId));
            var signed = Require090(recruitment.SignApplicant(
                applicantBoard, applicant.RecruitId));
            Assert.That(signed.Guild.Recruits.Count(value =>
                StringComparer.Ordinal.Equals(
                    value.AuthoredStableRecruitId, hero.StableId)),
                Is.EqualTo(1));

            var ascensionPlan = InvokePlanFinder090(
                "FindAscensionPlan089",
                signed, hero, campaign020, worldGate, deck, catalog020,
                catalog023, heroes);
            Assert.That(ascensionPlan, Is.Not.Null,
                "The bounded smoke traversal must reach the exact owned Hero's matching copy.");
            var ascensionBase = Field090<CampaignState>(
                ascensionPlan, "BaseCampaign");
            var ascensionTargetId = Field090<string>(
                ascensionPlan, "TargetCardId");
            var ascensionAdvances = Field090<IReadOnlyList<string>>(
                ascensionPlan, "AdvanceCardIds");
            Assert.That(ascensionAdvances.Count, Is.GreaterThan(2),
                "The owned hero is the fourth weighted Recruit slot; traversal must cross multiple physical rows.");

            var ascensionBoard = Require090(worldGate.BeginOperation(
                ascensionBase, catalog023, "CH018_001",
                new[] { "RECRUIT_SMOKE_UNION_089" }, catalog020, heroes));
            foreach (var cardId in ascensionAdvances)
            {
                var liveCard = WorldGate090(ascensionBoard).ActiveOperation
                    .ExpeditionDeck089.CurrentRow.SingleOrDefault(value =>
                        StringComparer.Ordinal.Equals(value.CardId, cardId));
                Assert.That(liveCard, Is.Not.Null,
                    "Every stored traversal ID must be replayable from its pristine base campaign.");
                Assert.That(ExpeditionDeckService089.IsOptionalBattleCard089(
                    liveCard), Is.False);
                Assert.That(liveCard.RecruitOfferKind, Is.Empty,
                    "Traversal may not consume an unrelated Hero lead.");
                ascensionBoard = ReplayCard090(
                    ascensionBoard, cardId, deck, catalog023);
            }

            var ascensionTarget = WorldGate090(ascensionBoard).ActiveOperation
                .ExpeditionDeck089.CurrentRow.SingleOrDefault(value =>
                    StringComparer.Ordinal.Equals(
                        value.CardId, ascensionTargetId));
            Assert.That(ascensionTarget, Is.Not.Null);
            Assert.That(ascensionTarget.RecruitStableId,
                Is.EqualTo(hero.StableId));
            Assert.That(ascensionTarget.RecruitOfferKind,
                Is.EqualTo(ExpeditionDeckService089.AscensionOfferKind089));
            var ascensionRewarded = ReplayCard090(
                ascensionBoard, ascensionTargetId, deck, catalog023);
            Assert.That(WorldGate090(ascensionRewarded)
                    .ExpeditionRecruitLeadIds089.Count(value =>
                        StringComparer.Ordinal.Equals(value, hero.StableId)),
                Is.EqualTo(1));
        }

        private static object InvokePlanFinder090(
            string methodName,
            params object[] arguments)
        {
            var method = typeof(FirstHourGoldSmoke071).GetMethod(
                methodName, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            try
            {
                return method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException ?? exception;
            }
        }

        private static T Field090<T>(object value, string fieldName)
        {
            Assert.That(value, Is.Not.Null);
            var field = value.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null);
            return (T)field.GetValue(value);
        }

        private static CampaignState ReplayCard090(
            CampaignState campaign,
            string cardId,
            ExpeditionDeckCommandService089 deck,
            Campaign023RuleCatalogAdapter catalog)
        {
            var committed = Require090(deck.CommitRouteCard(
                campaign, catalog, cardId,
                "RECRUIT_SMOKE_ACTOR_089",
                "RECRUIT_SMOKE_ASSISTANT_089"));
            return Require090(deck.ApplyWorldGateReceiptExactlyOnce(
                committed, catalog));
        }

        private static WorldGateRuntimeState023 WorldGate090(
            CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020
                .WorldGate023;

        private static CampaignState Require090(Result<CampaignState> result)
        {
            Assert.That(result.IsSuccess, Is.True,
                string.Join("\n", result.Errors));
            return result.Value;
        }
    }
}
#endif
