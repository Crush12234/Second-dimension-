#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class EarnedCardFeedback099PlayModeTests
    {
        string _directory;
        GameObject _owner;
        GameObject _audioListenerOwner;

        [SetUp] public void PrepareIsolatedScene099()
        {
            // Production BootCoordinator supplies this scene dependency. This
            // test creates only a presenter, so supply a listener explicitly;
            // retain strict unexpected-log checks instead of hiding audio logs.
            if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                _audioListenerOwner = new GameObject("Earned feedback test listener", typeof(AudioListener));
        }

        [UnityTearDown] public IEnumerator Cleanup099()
        {
            if (_owner != null) UnityEngine.Object.Destroy(_owner);
            yield return null;
            if (!string.IsNullOrEmpty(_directory) && Directory.Exists(_directory)) Directory.Delete(_directory, true);
            if (_audioListenerOwner != null) UnityEngine.Object.Destroy(_audioListenerOwner);
        }

        [UnityTest]
        public IEnumerator EarnedDuplicatePreviewAndClaimShowExactHeroAndCommittedGrowth099()
        {
            const string heroId = "HERO_REC_136";
            var source = Path.Combine(Application.dataPath, "Tests", "Fixtures", "R103_EarnedCard099.fixture.json");
            var original = File.ReadAllBytes(source);
            _directory = Path.Combine(Path.GetTempPath(), "sd_earned_card_ui099_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            foreach (var ascension in new[] { 0, 10 })
            {
                var save = Path.Combine(_directory, "CopiedInvitation_A" + ascension + ".json");
                File.Copy(source, save);
                var runtime = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
                var earned = Field099<CampaignState>(runtime, "_campaign");
                var authority = Field099<GuildCityRecruitmentService017D>(runtime, "_guildCityRecruitment");
                var hero = HeroRosterAudit093.Catalog093.AcceptedHeroes.Single(value => value.StableId == heroId);
                var applicant = HeroMaster300ApplicantLead089.ToApplicant(hero, 1, "WORLD_GOBLIN_001");
                // Explicit isolated applicant-board/affordable-wallet UNIT setup:
                // R103 has 7,522 XP, below this hero's authored 9,000 XP price.
                // The synthetic wallet below is not natural acquisition evidence;
                // the original source/earned receipt and housing stay untouched.
                // A10 below is also a labeled boundary fixture.
                var board = new ApplicantBoardState("UI_PAID099", "UI_PAID099", 0, true, new[] { applicant }, null);
                var prepared = earned.With(earned.Guild.WithGuildCity(earned.Guild.GuildCity.With(
                    recruitmentBoard: board, replaceRecruitmentBoard: true)), earned.OpeningFlow);
                var price = authority.DescribeApplicantSigning090(prepared, applicant).EffectiveCostTreasuryXp;
                Assert.That(price, Is.GreaterThan(0));
                prepared = prepared.With(prepared.Guild.With(Math.Max(prepared.Guild.TreasuryXp, price),
                    prepared.Guild.Recruits, prepared.Guild.Unions, prepared.Guild.Inventory,
                    prepared.Guild.Development), prepared.OpeningFlow);
                var signed = authority.SignApplicant(prepared, applicant.RecruitId);
                Assert.That(signed.IsSuccess, Is.True, string.Join(";", signed.Errors));
                Assert.That(signed.Value.Guild.TreasuryXp, Is.EqualTo(prepared.Guild.TreasuryXp - price));
                var owned = signed.Value.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == heroId);
                var progress = owned.Progression;
                while (progress.AscensionLevel < ascension) progress = progress.Ascend089();
                var fixture = signed.Value.With(signed.Value.Guild.With(signed.Value.Guild.TreasuryXp,
                    signed.Value.Guild.Recruits.Select(value => value.RecruitId == owned.RecruitId
                        ? value.WithProgression(progress) : value).ToArray(), signed.Value.Guild.Unions,
                    signed.Value.Guild.Inventory, signed.Value.Guild.Development), signed.Value.OpeningFlow);
                var store = new AtomicSaveStore();
                store.Write(save, SaveEnvelopeV1.Create(fixture, new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc)));
                runtime = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
                var before = runtime.State;
                var beforeHero = before.Recruits.Single(value => value.PortraitAuthorityId == heroId);
                var oldCapacity = GuildCityRecruitmentService017D.EarnedRecruitCapacity094(
                    store.ReadWithRecovery(save).Value.CampaignState.Guild.Development);
                var view = runtime.EarnedCampaignRecruits094;
                var preview = view.Preview.Single(value => value.StableId == heroId);
                Assert.That(preview.IsDuplicate099, Is.True);
                Assert.That(view.ClaimableDuplicateCards099, Is.EqualTo(1));
                var treasuryBefore = fixture.Guild.TreasuryXp;

                _owner = new GameObject("Earned invitation UI A" + ascension);
                var presenter = _owner.AddComponent<M1FlowPresenter>();
                presenter.Initialize(runtime);
                Set099(presenter, "_screen", M1Screen.GuildOperations);
                Invoke099(presenter, "OpenEarnedCampaignRecruits094");
                yield return null;
                var canvas = Field099<Canvas>(presenter, "_canvas");
                Assert.That(Text099(canvas, "Earned Recruit Name 094 0").text, Is.EqualTo(hero.Name));
                Assert.That(Text099(canvas, "Earned Recruit Source 094 0").text, Is.EqualTo("DUPLICATE INVITATION"));
                Assert.That(Text099(canvas, "Earned Recruit Growth 099 0").text, Is.EqualTo(preview.RewardSummary099));
                var action = canvas.GetComponentsInChildren<Button>(true)
                    .Single(value => value.name == "Claim Earned Campaign Recruits 094");
                Assert.That(action.interactable, Is.True);
                action.onClick.Invoke(); // Actual earned-card UI → runtime → existing claim/merge authority.
                yield return null;

                var after = runtime.State;
                var afterHero = after.Recruits.Single(value => value.RecruitId == beforeHero.RecruitId);
                var growth = EarnedRecruitFeedback099.CommittedGrowth099(beforeHero, afterHero);
                Assert.That(growth, Is.Not.Empty, "These A0/A10 boundary fixtures must cause real progression.");
                Assert.That(Field099<Dictionary<string, string>>(presenter, "_earnedRecruitGrowth099")[beforeHero.RecruitId], Is.EqualTo(growth));
                Assert.That(Text099(canvas, "Earned Recruit Name 094 0").text, Is.EqualTo(hero.Name));
                Assert.That(Text099(canvas, "Earned Recruit Growth 099 0").text, Is.EqualTo(growth));
                Assert.That(Text099(canvas, "Earned Recruit Source 094 0").text, Is.EqualTo("DUPLICATE INVITATION APPLIED"));
                var summary = Text099(canvas, "Earned Recruits Summary 094").text;
                Assert.That(summary, Does.Contain("1 duplicate invitation applied").And.Not.Contain("0 earned").And.Not.Contain("0 new"));
                Assert.That(Text099(canvas, "Earned Recruits Heading 094").text, Is.EqualTo("RECRUIT REWARDS APPLIED"));
                var saved = store.ReadWithRecovery(save);
                Assert.That(saved.IsSuccess, Is.True, string.Join(";", saved.Errors));
                Assert.That(saved.Value.CampaignState.Guild.TreasuryXp, Is.EqualTo(treasuryBefore));
                Assert.That(saved.Value.CampaignState.Guild.Recruits.Count,
                    Is.EqualTo(before.Recruits.Count + view.ClaimableCount - 1));
                Assert.That(GuildCityRecruitmentService017D.EarnedRecruitCapacity094(saved.Value.CampaignState.Guild.Development),
                    Is.EqualTo(oldCapacity + view.ClaimableCount - 1), "The duplicate invitation grants no capacity.");
                Assert.That(runtime.ClaimedEarnedCardDuplicates099.Values, Does.Contain(heroId));
                var hash = saved.Value.CanonicalStateHash;
                var replay = runtime.ClaimEarnedCampaignRecruits094();
                Assert.That(replay.Succeeded, Is.True);
                Assert.That(replay.Message, Is.EqualTo("No new earned rewards to claim."));
                Assert.That(runtime.State.CanonicalStateHash, Is.EqualTo(hash));
                Assert.That(File.ReadAllBytes(source), Is.EqualTo(original));
                UnityEngine.Object.Destroy(_owner);
                _owner = null;
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        static Text Text099(Canvas canvas, string name) => canvas.GetComponentsInChildren<Text>(true).Single(value =>
            value.name == name || value.name.StartsWith(name + " [", StringComparison.Ordinal));
        static T Field099<T>(object target, string name) => (T)target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        static void Set099(object target, string name, object value) => target.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        static void Invoke099(object target, string name) => target.GetType().GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    }
}
#endif
