using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        const string EarnedRecruitTab094 = "CAMPAIGN_RECRUITS";
        readonly List<string> _earnedRecruitJoined094 = new List<string>();
        readonly Dictionary<string, string> _earnedRecruitGrowth099 = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> _earnedRecruitNewIds099 = new HashSet<string>(StringComparer.Ordinal);
        string _earnedRecruitClaimSummary099 = string.Empty;
        int _earnedRecruitPage094;
        Button _earnedGuildAccess094;

        public static string EarnedRecruitPolicyCopy094 =>
            "Earn 3 new members per completed chapter, plus lucky quest-card recruits. Regular hiring still costs XP.";

        public static string EarnedRecruitSourceCopy094(string source)
        {
            const string prefix = "Chapter CH018_";
            if (!string.IsNullOrWhiteSpace(source) && source.StartsWith("Opening", StringComparison.OrdinalIgnoreCase))
                return "OPENING STORY REWARD";
            return source != null && source.StartsWith(prefix, StringComparison.Ordinal) &&
                   int.TryParse(source.Substring(prefix.Length), out var chapter)
                ? "CHAPTER " + chapter + " REWARD"
                : "QUEST-CARD REWARD";
        }

        public static Rect EarnedRecruitCardRect094(int index) =>
            new Rect(0.055f + Mathf.Clamp(index, 0, 2) * 0.30f, 0.265f, 0.29f, 0.565f);

        void OpenEarnedCampaignRecruits094()
        {
            _earnedRecruitJoined094.Clear();
            _earnedRecruitGrowth099.Clear();
            _earnedRecruitNewIds099.Clear();
            _earnedRecruitClaimSummary099 = string.Empty;
            _earnedRecruitPage094 = 0;
            _guildCityTab017D = EarnedRecruitTab094;
            BuildCurrentScreen();
        }

        bool TryBuildEarnedRecruitAccess094(Transform parent, Rect anchors, bool compact = false)
        {
            _earnedGuildAccess094 = null;
            var view = (_coordinator as M1RuntimeCoordinator)?.EarnedCampaignRecruits094;
            if (view == null || (view.PendingCount == 0 && view.BlockedChapterRecruits == 0)) return false;
            var caption = view.PendingCount > 0
                ? "CAMPAIGN REWARDS  •  " + view.PendingCount + " INVITATIONS"
                : "CAMPAIGN REWARD NEEDS ATTENTION";
            var button = RuntimeUi.AddButton(parent, "Earned Campaign Recruits Access 094", caption,
                OpenEarnedCampaignRecruits094, RuntimeUi.MinimumTouchPixels, RuntimeUi.Accent);
            AnchorLivingGuild074(button.GetComponent<RectTransform>(), anchors);
            ConfigureResponsiveText062(button.GetComponentInChildren<Text>(), compact ? 14 : 17, compact ? 20 : 26);
            _earnedGuildAccess094 = button;
            return true;
        }

        public static void ConfigureEarnedRecruitNavigation094(Button story, Button rewards)
        {
            if (story == null || rewards == null) return;
            var storyNavigation = story.navigation;
            storyNavigation.selectOnUp = rewards;
            story.navigation = storyNavigation;
            var rewardNavigation = rewards.navigation;
            rewardNavigation.mode = Navigation.Mode.Explicit;
            rewardNavigation.selectOnDown = story;
            rewardNavigation.selectOnLeft = story;
            rewardNavigation.selectOnRight = story;
            rewards.navigation = rewardNavigation;
        }

        void BuildEarnedCampaignRecruits094()
        {
            var runtime = _coordinator as M1RuntimeCoordinator;
            var view = runtime?.EarnedCampaignRecruits094;
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activePage = null; _activeContent = null; _activeScroll = null;
            var root = RuntimeUi.AddPanel(_screenRoot, "Earned Campaign Recruits 094", new Color(0.012f, 0.020f, 0.035f));
            Stretch(root.rectTransform);
            BuildMenuBackdrop090(root.transform, "Campaign Recruitment Alcove 094",
                "SecondDimension/Art/Backgrounds/BG_RECRUIT_DOSSIER_ALCOVE", _highContrast ? 0.58f : 0.28f);
            var back = RuntimeUi.AddButton(root.transform, "Earned Recruits Return 094", "← GUILD",
                () => { _guildCityTab017D = "HALL"; BuildCurrentScreen(); }, 72f, RuntimeUi.ButtonNormal);
            AnchorLivingGuild074(back.GetComponent<RectTransform>(), new Rect(0.015f, 0.900f, 0.13f, 0.080f));
            ConfigureResponsiveText062(back.GetComponentInChildren<Text>(), 17, 25);

            var joined = (_coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null && _earnedRecruitJoined094.Contains(value.RecruitId)).ToArray();
            var showingJoined = joined.Length > 0;
            var heading = RuntimeUi.AddText(root.transform, "Earned Recruits Heading 094",
                showingJoined ? _earnedRecruitGrowth099.Count > 0 ? "RECRUIT REWARDS APPLIED" : "WELCOME TO THE GUILD"
                    : "YOUR CAMPAIGN REWARDS", 40,
                TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            AnchorLivingGuild074(heading.rectTransform, new Rect(0.17f, 0.910f, 0.67f, 0.065f));
            ConfigureResponsiveText062(heading, 25, 40);
            var policy = RuntimeUi.AddText(root.transform, "Earned Recruits Policy 094", EarnedRecruitPolicyCopy094,
                24, TextAnchor.MiddleCenter, RuntimeUi.Text);
            AnchorLivingGuild074(policy.rectTransform, new Rect(0.16f, 0.845f, 0.69f, 0.055f));
            ConfigureResponsiveText062(policy, 17, 24);

            var totalPages = Math.Max(1, (joined.Length + 2) / 3);
            _earnedRecruitPage094 = Mathf.Clamp(_earnedRecruitPage094, 0, totalPages - 1);
            if (showingJoined)
            {
                var page = joined.Skip(_earnedRecruitPage094 * 3).Take(3).ToArray();
                for (var index = 0; index < page.Length; index++)
                {
                    var hero = page[index];
                    _earnedRecruitGrowth099.TryGetValue(hero.RecruitId, out var growth);
                    AddEarnedRecruitCard094(root.transform, index, hero.RecruitId, hero.PortraitAuthorityId,
                        hero.DisplayName, hero.RaceId, hero.ObservedClass,
                        _earnedRecruitNewIds099.Contains(hero.RecruitId) ? "JOINED YOUR GUILD" : "DUPLICATE INVITATION APPLIED",
                        hero.VisualSeed, growth);
                }
            }
            else if (view != null)
            {
                var preview = view.Preview.Take(3).ToArray();
                for (var index = 0; index < preview.Length; index++)
                {
                    var hero = preview[index];
                    AddEarnedRecruitCard094(root.transform, index, hero.StableId, hero.StableId,
                        hero.Name, hero.Race, hero.Role,
                        hero.IsDuplicate099 ? "DUPLICATE INVITATION" : EarnedRecruitSourceCopy094(hero.SourceLabel),
                        null, EarnedRecruitFeedback099.PreviewSummary099(hero));
                }
            }

            var summary = !string.IsNullOrWhiteSpace(_earnedRecruitClaimSummary099)
                ? _earnedRecruitClaimSummary099
                : view?.Summary ?? "Campaign rewards are unavailable. Your save has not been changed.";
            if (!string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive) summary = _localStatus;
            var message = RuntimeUi.AddText(root.transform, "Earned Recruits Summary 094", summary,
                26, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            AnchorLivingGuild074(message.rectTransform, new Rect(0.07f, 0.170f, 0.86f, 0.075f));
            ConfigureResponsiveText062(message, 18, 26);
            if (showingJoined && totalPages > 1)
            {
                var previous = RuntimeUi.AddButton(root.transform, "Previous Earned Heroes 094", "←",
                    () => { _earnedRecruitPage094--; BuildCurrentScreen(); }, 64f, RuntimeUi.ButtonNormal);
                var next = RuntimeUi.AddButton(root.transform, "Next Earned Heroes 094", "→",
                    () => { _earnedRecruitPage094++; BuildCurrentScreen(); }, 64f, RuntimeUi.ButtonNormal);
                AnchorLivingGuild074(previous.GetComponent<RectTransform>(), new Rect(0.01f, 0.475f, 0.04f, 0.10f));
                AnchorLivingGuild074(next.GetComponent<RectTransform>(), new Rect(0.95f, 0.475f, 0.04f, 0.10f));
                previous.interactable = _earnedRecruitPage094 > 0;
                next.interactable = _earnedRecruitPage094 + 1 < totalPages;
            }
            var action = RuntimeUi.AddButton(root.transform, "Claim Earned Campaign Recruits 094",
                showingJoined ? "UPDATE UNIONS" : (view?.ClaimableCount ?? 0) > 0
                    ? "CLAIM " + view.ClaimableCount + "  •  RECRUIT REWARDS"
                    : "NO REWARDS READY TO CLAIM",
                showingJoined ? (Action)OpenHallParty069 : () => ClaimEarnedRecruitsFromUi094(runtime),
                100f, RuntimeUi.Accent);
            AnchorLivingGuild074(action.GetComponent<RectTransform>(), new Rect(0.22f, 0.060f, 0.56f, 0.095f));
            ConfigureResponsiveText062(action.GetComponentInChildren<Text>(), 20, 31);
            action.interactable = showingJoined || (view?.CanClaim ?? false);
            if (action.interactable) action.Select(); else back.Select();
        }

        void ClaimEarnedRecruitsFromUi094(M1RuntimeCoordinator runtime)
        {
            if (runtime == null) return;
            var before = runtime.State.Recruits.ToDictionary(value => value.RecruitId, StringComparer.Ordinal);
            var claimsBefore = new HashSet<string>(runtime.ClaimedEarnedCardDuplicates099.Keys, StringComparer.Ordinal);
            var result = runtime.ClaimEarnedCampaignRecruits094();
            if (result != null && result.Succeeded)
            {
                _earnedRecruitJoined094.Clear();
                _earnedRecruitGrowth099.Clear();
                _earnedRecruitNewIds099.Clear();
                _earnedRecruitClaimSummary099 = result.Message;
                var duplicateIdentities = new HashSet<string>(runtime.ClaimedEarnedCardDuplicates099
                    .Where(pair => !claimsBefore.Contains(pair.Key)).Select(pair => pair.Value), StringComparer.Ordinal);
                foreach (var hero in runtime.State.Recruits)
                {
                    var isNew = !before.TryGetValue(hero.RecruitId, out var old);
                    var duplicateApplied = duplicateIdentities.Contains(hero.PortraitAuthorityId ?? string.Empty);
                    if (!isNew && !duplicateApplied) continue;
                    _earnedRecruitJoined094.Add(hero.RecruitId);
                    if (isNew) _earnedRecruitNewIds099.Add(hero.RecruitId);
                    if (!duplicateApplied) continue;
                    var growth = isNew
                        ? "JOINED + INVITATION APPLIED\nASCENSION " + hero.AscensionLevel + "/10"
                        : EarnedRecruitFeedback099.CommittedInvitationSummary099(old, hero);
                    _earnedRecruitGrowth099[hero.RecruitId] = growth;
                }
                _earnedRecruitPage094 = 0;
            }
            ApplyGuildCity017D(result);
        }

        void AddEarnedRecruitCard094(Transform parent, int index, string recruitId, string identity,
            string name, string race, string role, string source, string visualSeed = null, string growthSummary099 = null)
        {
            var card = RuntimeUi.AddPanel(parent, "Earned Recruit Card 094 " + index, Color.white);
            AnchorLivingGuild074(card.rectTransform, EarnedRecruitCardRect094(index));
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);
            card.raycastTarget = false;
            var art = RuntimeUi.AddPanel(card.transform, "Earned Recruit Sprite 094 " + identity, Color.white);
            art.raycastTarget = false;
            var hasGrowth099 = !string.IsNullOrWhiteSpace(growthSummary099);
            AnchorLivingGuild074(art.rectTransform, hasGrowth099
                ? new Rect(0.035f, 0.295f, 0.930f, 0.595f)
                : new Rect(0.035f, 0.145f, 0.930f, 0.745f));
            if (M1VisualAssets.TryResolveMenuStandee091(recruitId, visualSeed, race, identity, role, string.Empty,
                    out var sprite, out _) && sprite != null)
            {
                art.sprite = sprite;
                art.preserveAspect = true;
            }
            else art.color = Color.clear;
            if (hasGrowth099)
            {
                var growth = RuntimeUi.AddText(card.transform, "Earned Recruit Growth 099 " + index,
                    growthSummary099, 23, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
                AnchorLivingGuild074(growth.rectTransform, new Rect(0.035f, 0.140f, 0.93f, 0.145f));
                ConfigureResponsiveText062(growth, 16, 23);
            }
            var label = RuntimeUi.AddText(card.transform, "Earned Recruit Name 094 " + index,
                name, 31, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            AnchorLivingGuild074(label.rectTransform, new Rect(0.035f, 0.055f, 0.93f, 0.08f));
            ConfigureResponsiveText062(label, 20, 31);
            var details = RuntimeUi.AddText(card.transform, "Earned Recruit Role 094 " + index,
                role, 22, TextAnchor.MiddleCenter, RuntimeUi.Positive);
            AnchorLivingGuild074(details.rectTransform, new Rect(0.035f, 0.005f, 0.93f, 0.05f));
            ConfigureResponsiveText062(details, 16, 22);
            var badge = RuntimeUi.AddText(card.transform, "Earned Recruit Source 094 " + index,
                source, 22, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            AnchorLivingGuild074(badge.rectTransform, new Rect(0.035f, 0.905f, 0.93f, 0.065f));
            ConfigureResponsiveText062(badge, 15, 22);
        }
    }
}
