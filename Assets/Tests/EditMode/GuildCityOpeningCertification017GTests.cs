
using System;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017G;
using SecondDimension.Presentation.GuildCity017G;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityOpeningCertification017GTests
    {
        [Test]
        public void EveryGameModeHasAProfile()
        {
            Assert.AreEqual(Enum.GetValues(typeof(GameMode)).Length, GuildCityOpeningBalance017G.All.Count);
            foreach (GameMode mode in Enum.GetValues(typeof(GameMode))) Assert.AreEqual(mode, GuildCityOpeningBalance017G.For(mode).Mode);
        }

        [Test]
        public void RelaxedAndOverpoweredReducePressureWhileIronRaisesIt()
        {
            var relaxed = GuildCityOpeningBalance017G.For(GameMode.Relaxed);
            var standard = GuildCityOpeningBalance017G.For(GameMode.Standard);
            var iron = GuildCityOpeningBalance017G.For(GameMode.Iron);
            var overpowered = GuildCityOpeningBalance017G.For(GameMode.OverpoweredStart);
            Assert.Greater(relaxed.StartingSupplyBonus, standard.StartingSupplyBonus);
            Assert.Greater(overpowered.CheckModifier, relaxed.CheckModifier);
            Assert.Less(relaxed.AdjustFatigueCost(1), iron.AdjustFatigueCost(1));
            Assert.Less(relaxed.AdjustEnemyUnionCount(3), iron.AdjustEnemyUnionCount(3));
            Assert.Greater(iron.AdjustGuildReward(100), standard.AdjustGuildReward(100));
        }

        [Test]
        public void ContextualCoachBeginsWithApplicantBoardAndEndsWithCitySynergy()
        {
            var snapshot = new GuildCityOpeningSnapshot017G();
            Assert.AreEqual("GUIDE_OPEN_APPLICANTS", GuildCityOpeningCoach017G.Current(snapshot).Id);
            snapshot.HasRecruitmentBoard = true;
            snapshot.SignedApplicantCount = 1;
            snapshot.TotalRecruitCount = 6;
            snapshot.EquippedRecruitCount = 3;
            snapshot.NormalUnionCount = 2;
            snapshot.PlacedBuildingCount = 2;
            snapshot.StaffedBuildingCount = 1;
            snapshot.AdjacencyBonusCount = 1;
            snapshot.HasActiveContract = true;
            snapshot.HasExpedition = true;
            snapshot.ExpeditionVisitedNodeCount = 4;
            snapshot.CommittedCheckCount = 1;
            snapshot.ClaimedBattleRewardCount = 1;
            snapshot.OperationOrdinal = 1;
            snapshot.RelationshipCount = 1;
            snapshot.UnviewedRelationshipCount = 0;
            Assert.AreEqual("GUIDE_OPENING_COMPLETE", GuildCityOpeningCoach017G.Current(snapshot).Id);
            Assert.AreEqual(100, GuildCityOpeningCoach017G.ProgressPercent(snapshot));
        }

        [Test]
        public void CertificationDataLoadsExactCounts()
        {
            var root = GuildCityOpeningCertificationRegistry017G.Load();
            Assert.AreEqual(5, root.modeProfiles.Length);
            Assert.AreEqual(15, root.tutorialSteps.Length);
            Assert.AreEqual(8, root.pacingTargets.Length);
            Assert.AreEqual(12, root.scoreCategories.Length);
            Assert.GreaterOrEqual(root.accessibilityChecks.Length, 8);
            Assert.AreEqual(9000, root.certification.minimumAverageBasisPoints);
        }

        [Test]
        public void PendingEncounterStillGuidesPlayerIntoBattleUntilCertifiedBattleStarts()
        {
            var snapshot = new GuildCityOpeningSnapshot017G
            {
                HasRecruitmentBoard = true,
                SignedApplicantCount = 1,
                TotalRecruitCount = 6,
                EquippedRecruitCount = 3,
                NormalUnionCount = 2,
                PlacedBuildingCount = 1,
                StaffedBuildingCount = 1,
                HasActiveContract = true,
                HasExpedition = true,
                ExpeditionVisitedNodeCount = 3,
                CommittedCheckCount = 1,
                HasPendingEncounter = true
            };
            var current = GuildCityOpeningCoach017G.Current(snapshot);
            Assert.AreEqual("GUIDE_ENTER_BATTLE", current.Id);
            Assert.AreEqual(GuildCityGuideDestination017G.Battle, current.Destination);

            snapshot.HasActiveCertifiedBattle = true;
            Assert.AreEqual("GUIDE_CLAIM_REWARD", GuildCityOpeningCoach017G.Current(snapshot).Id);
        }
    }
}
