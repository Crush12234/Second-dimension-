using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildApplicantConversation069Tests
    {
        [Test]
        public void ConversationShowsOnePersonAtATimeAndRoutesRecruitmentThroughCallback()
        {
            var host = new GameObject("Applicant Conversation Test Canvas", typeof(RectTransform), typeof(Canvas));
            var owner = new GameObject("Applicant Conversation Test Owner");
            try
            {
                var selectedByCallback = string.Empty;
                var backCount = 0;
                var state = State069();
                var conversation = owner.AddComponent<GuildApplicantConversation069>();
                conversation.Begin069(
                    host.transform,
                    () => state,
                    id => Development069(id),
                    id =>
                    {
                        selectedByCallback = id;
                        return M1CommandResult.Success("Permanent recruitment saved.");
                    },
                    () => backCount++);

                Assert.That(conversation.IsOpen069, Is.True);
                Assert.That(conversation.ApplicantCount069, Is.EqualTo(2));
                Assert.That(conversation.SelectedRecruitId069, Is.EqualTo("APP_A"));
                Assert.That(conversation.ShowsPermanentRecruitLanguage069, Is.True);
                Assert.That(conversation.RecruitButton069, Is.Not.Null);
                Assert.That(conversation.RecruitButton069.GetComponentInChildren<Text>().text,
                    Is.EqualTo("RECRUIT PERMANENTLY"));

                conversation.Next069();
                Assert.That(conversation.SelectedRecruitId069, Is.EqualTo("APP_B"));
                conversation.Previous069();
                Assert.That(conversation.SelectedRecruitId069, Is.EqualTo("APP_A"));
                conversation.RecruitSelected069();
                Assert.That(selectedByCallback, Is.EqualTo("APP_A"));

                conversation.Back069();
                Assert.That(backCount, Is.EqualTo(1));
                conversation.Shutdown069();
                Assert.That(conversation.IsOpen069, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void RefreshPreservesTheSelectedPersonWhenTheBoardUpdates()
        {
            var host = new GameObject("Applicant Refresh Test Canvas", typeof(RectTransform), typeof(Canvas));
            var owner = new GameObject("Applicant Refresh Test Owner");
            try
            {
                var state = State069();
                var conversation = owner.AddComponent<GuildApplicantConversation069>();
                conversation.Begin069(
                    host.transform,
                    () => state,
                    id => Development069(id),
                    id => M1CommandResult.Success(),
                    () => { });
                conversation.Next069();
                Assert.That(conversation.SelectedRecruitId069, Is.EqualTo("APP_B"));

                state.TreasuryXp = 950;
                conversation.Refresh069();
                Assert.That(conversation.SelectedRecruitId069, Is.EqualTo("APP_B"));
                Assert.That(conversation.SelectedApplicantIndex069, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(owner);
                Object.DestroyImmediate(host);
            }
        }

        private static GuildCityPresentationState017D State069()
        {
            return new GuildCityPresentationState017D
            {
                IsAvailable = true,
                HasRecruitmentBoard = true,
                TreasuryXp = 500,
                Applicants = new[]
                {
                    Applicant069("APP_A", 0, "Mira Vale", "HUMAN", "CLASS_TEND_GUARDIAN"),
                    Applicant069("APP_B", 1, "Rook Ember", "ORC", "CLASS_TEND_WARRIOR")
                }
            };
        }

        private static GuildCityApplicantView017D Applicant069(
            string id,
            int slot,
            string name,
            string race,
            string classId)
        {
            return new GuildCityApplicantView017D
            {
                RecruitId = id,
                Slot = slot,
                DisplayName = name,
                RaceId = race,
                WorldId = "SKYHOME",
                ClassTendencyId = classId,
                VisualSeed = "SEED_" + id,
                PortraitAuthorityId = id,
                PersonalHook = "I want to protect the roads that brought my family home.",
                ObservedSummary = "Keeps calm when plans go wrong.",
                EquipmentSummary = "Travel weapon and fitted field armor",
                SigningCostTreasuryXp = 0,
                CanAfford = true
            };
        }

        private static GuildMemberDevelopmentView067 Development069(string id)
        {
            return new GuildMemberDevelopmentView067
            {
                RecruitId = id,
                PotentialBand = "HIGH",
                FixedWeaponFamilyId = "WEAPON_FAMILY_SWORD",
                PrimaryRoleTreeId = "TREE_CA002_ROLE_GUARDIAN",
                StartingAdvantage = "Steady under pressure"
            };
        }
    }
}
