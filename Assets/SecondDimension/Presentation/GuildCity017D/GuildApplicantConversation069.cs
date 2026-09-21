using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.GuildCity017D
{
    /// <summary>
    /// A focused, one-person-at-a-time recruitment conversation. Gameplay state is
    /// owned by the supplied callbacks; this component only presents applicant views.
    /// </summary>
    public sealed class GuildApplicantConversation069 : MonoBehaviour
    {
        public const string RootName069 = "Guild Applicant Conversation 069";
        public const string PermanentPromise069 =
            "Recruitment is permanent. This adventurer will not expire, be consumed, or disappear between quests.";

        private Func<GuildCityPresentationState017D> _stateProvider;
        private Func<string, GuildMemberDevelopmentView067> _developmentProvider;
        private Func<string, M1CommandResult> _recruitPermanently;
        private Action _back;
        private RectTransform _root;
        private GuildCityApplicantView017D[] _applicants = Array.Empty<GuildCityApplicantView017D>();
        private int _selectedIndex;
        private string _selectedRecruitId = string.Empty;
        private string _status = string.Empty;
        private bool _statusPositive;
        private string _resolvedPortraitResourceKey = string.Empty;
        private bool _usesRealPortrait;
        private Button _recruitButton;

        public bool IsOpen069 => _root != null && _root.gameObject.activeSelf;
        public RectTransform Root069 => _root;
        public int ApplicantCount069 => _applicants.Length;
        public int SelectedApplicantIndex069 => _selectedIndex;
        public string SelectedRecruitId069 => _selectedRecruitId;
        public string ResolvedPortraitResourceKey069 => _resolvedPortraitResourceKey;
        public bool UsesRealPortrait069 => _usesRealPortrait;
        public bool ShowsPermanentRecruitLanguage069 => IsOpen069;
        public Button RecruitButton069 => _recruitButton;

        public void Begin069(
            Transform uiParent,
            Func<GuildCityPresentationState017D> stateProvider,
            Func<string, GuildMemberDevelopmentView067> developmentProvider,
            Func<string, M1CommandResult> recruitPermanently,
            Action back)
        {
            if (uiParent == null) throw new ArgumentNullException(nameof(uiParent));
            if (stateProvider == null) throw new ArgumentNullException(nameof(stateProvider));
            if (recruitPermanently == null) throw new ArgumentNullException(nameof(recruitPermanently));

            Shutdown069();
            _stateProvider = stateProvider;
            _developmentProvider = developmentProvider;
            _recruitPermanently = recruitPermanently;
            _back = back;
            _root = RuntimeUi.AddStretchRect(uiParent, RootName069);
            _root.SetAsLastSibling();
            Refresh069();
        }

        public void Refresh069()
        {
            Refresh069(_stateProvider?.Invoke());
        }

        public void Refresh069(GuildCityPresentationState017D state)
        {
            if (_root == null) return;

            var previousId = _selectedRecruitId;
            _applicants = (state?.Applicants ?? Array.Empty<GuildCityApplicantView017D>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.RecruitId))
                .OrderBy(value => value.IsSigned)
                .ThenBy(value => value.Slot)
                .ThenBy(value => value.RecruitId, StringComparer.Ordinal)
                .ToArray();

            if (_applicants.Length == 0)
            {
                _selectedIndex = 0;
                _selectedRecruitId = string.Empty;
            }
            else
            {
                var preserved = Array.FindIndex(
                    _applicants,
                    value => StringComparer.Ordinal.Equals(value.RecruitId, previousId));
                _selectedIndex = preserved >= 0
                    ? preserved
                    : Mathf.Clamp(_selectedIndex, 0, _applicants.Length - 1);
                _selectedRecruitId = _applicants[_selectedIndex].RecruitId;
            }

            Build069(state);
        }

        public void Previous069()
        {
            if (_applicants.Length < 2) return;
            _selectedIndex = (_selectedIndex - 1 + _applicants.Length) % _applicants.Length;
            _selectedRecruitId = _applicants[_selectedIndex].RecruitId;
            _status = string.Empty;
            Build069(_stateProvider?.Invoke());
        }

        public void Next069()
        {
            if (_applicants.Length < 2) return;
            _selectedIndex = (_selectedIndex + 1) % _applicants.Length;
            _selectedRecruitId = _applicants[_selectedIndex].RecruitId;
            _status = string.Empty;
            Build069(_stateProvider?.Invoke());
        }

        public void RecruitSelected069()
        {
            var applicant = Selected069();
            if (applicant == null || applicant.IsSigned || !applicant.CanAfford) return;

            var result = _recruitPermanently?.Invoke(applicant.RecruitId);
            if (this == null || _root == null) return;

            _statusPositive = result != null && result.Succeeded;
            _status = result?.Message ?? "The recruitment decision could not be completed.";
            if (_statusPositive && string.IsNullOrWhiteSpace(_status))
                _status = applicant.DisplayName + " joined the Guild permanently.";

            Refresh069();
            if (!_statusPositive || _applicants.Length < 2) return;

            var current = Selected069();
            if (current == null || !current.IsSigned) return;
            var nextUnsigned = Array.FindIndex(_applicants, value => !value.IsSigned);
            if (nextUnsigned < 0) return;
            _selectedIndex = nextUnsigned;
            _selectedRecruitId = _applicants[_selectedIndex].RecruitId;
            Build069(_stateProvider?.Invoke());
        }

        public void Back069()
        {
            _back?.Invoke();
        }

        public void Shutdown069()
        {
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(_root.gameObject);
                else DestroyImmediate(_root.gameObject);
            }

            _root = null;
            _stateProvider = null;
            _developmentProvider = null;
            _recruitPermanently = null;
            _back = null;
            _applicants = Array.Empty<GuildCityApplicantView017D>();
            _selectedIndex = 0;
            _selectedRecruitId = string.Empty;
            _status = string.Empty;
            _statusPositive = false;
            _resolvedPortraitResourceKey = string.Empty;
            _usesRealPortrait = false;
            _recruitButton = null;
        }

        private void Build069(GuildCityPresentationState017D state)
        {
            if (_root == null) return;
            ClearChildren069(_root);
            _resolvedPortraitResourceKey = string.Empty;
            _usesRealPortrait = false;
            _recruitButton = null;

            AddBackdrop069();
            var veil = RuntimeUi.AddPanel(_root, "Recruitment Conversation Reading Veil 069",
                new Color(0.025f, 0.04f, 0.075f, 0.68f));
            Stretch069(veil.rectTransform);

            var top = RuntimeUi.AddPanel(_root, "Recruitment Conversation Header 069",
                new Color(0.03f, 0.06f, 0.11f, 0.94f));
            Anchor069(top.rectTransform, new Vector2(0.025f, 0.865f), new Vector2(0.975f, 0.975f));
            RuntimeUi.AddText(top.transform, "Recruitment Conversation Title 069",
                "A QUIET INTERVIEW", 58, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            var title = top.transform.Find("Recruitment Conversation Title 069")?.GetComponent<Text>();
            if (title != null)
            {
                Anchor069(title.rectTransform, new Vector2(0.035f, 0.45f), new Vector2(0.72f, 0.98f));
            }
            var subtitle = RuntimeUi.AddText(top.transform, "Recruitment Conversation Subtitle 069",
                "Meet one adventurer at a time. Choose the person, not a statistic sheet.",
                30, TextAnchor.MiddleLeft, RuntimeUi.Text);
            Anchor069(subtitle.rectTransform, new Vector2(0.035f, 0.03f), new Vector2(0.76f, 0.48f));
            var back = RuntimeUi.AddButton(top.transform, "Back To Guild Hall 069", "BACK TO HALL", Back069,
                RuntimeUi.MinimumTouchPixels, RuntimeUi.ButtonNormal);
            Anchor069(back.GetComponent<RectTransform>(), new Vector2(0.78f, 0.10f), new Vector2(0.98f, 0.90f));
            SetButtonFont069(back, 30);

            if (_applicants.Length == 0)
            {
                AddNoApplicants069(state);
                return;
            }

            var applicant = Selected069();
            var development = _developmentProvider?.Invoke(applicant.RecruitId);
            AddPortrait069(applicant);
            AddConversation069(applicant, development, state);
            AddControls069(applicant);
        }

        private void AddBackdrop069()
        {
            var backdrop = RuntimeUi.AddPanel(_root, "Recruitment Alcove Artwork 069", RuntimeUi.Background);
            Stretch069(backdrop.rectTransform);
            if (!M1VisualAssets.TryResolveBackdrop(
                    M1VisualAssets.BackdropRole.RecruitDossier,
                    out var sprite,
                    out _)) return;
            backdrop.sprite = sprite;
            backdrop.type = Image.Type.Simple;
            backdrop.preserveAspect = false;
            backdrop.color = Color.white;
        }

        private void AddPortrait069(GuildCityApplicantView017D applicant)
        {
            var frame = RuntimeUi.AddPanel(_root, "Large Applicant Portrait Frame 069", RuntimeUi.Accent);
            Anchor069(frame.rectTransform, new Vector2(0.04f, 0.19f), new Vector2(0.43f, 0.84f));
            M1PremiumUi.StylePortraitFrame(frame);

            var backing = RuntimeUi.AddPanel(frame.transform, "Applicant Portrait Backing 069",
                M1VisualAssets.FallbackPortraitColor(applicant.RaceId, applicant.VisualSeed, applicant.RecruitId));
            Anchor069(backing.rectTransform, new Vector2(0.018f, 0.018f), new Vector2(0.982f, 0.982f));
            backing.raycastTarget = false;

            _usesRealPortrait = M1VisualAssets.TryResolvePortrait(
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.ClassTendencyId,
                out var portraitSprite,
                out _resolvedPortraitResourceKey);
            if (_usesRealPortrait)
            {
                var art = RuntimeUi.AddPanel(backing.transform, "Applicant Portrait Real Art 069", Color.white);
                Anchor069(art.rectTransform, Vector2.zero, Vector2.one);
                art.sprite = portraitSprite;
                art.type = Image.Type.Simple;
                art.preserveAspect = true;
                art.raycastTarget = false;
            }
            else
            {
                var initials = RuntimeUi.AddText(backing.transform, "Applicant Portrait Fallback 069",
                    M1VisualAssets.Initials(applicant.DisplayName), 138, TextAnchor.MiddleCenter,
                    new Color(0.96f, 0.91f, 0.79f, 0.94f), FontStyle.Bold);
                Anchor069(initials.rectTransform, new Vector2(0.05f, 0.20f), new Vector2(0.95f, 0.92f));
            }

            var namePlate = RuntimeUi.AddPanel(frame.transform, "Applicant Portrait Name Plate 069",
                new Color(0.025f, 0.04f, 0.07f, 0.94f));
            Anchor069(namePlate.rectTransform, new Vector2(0.025f, 0.018f), new Vector2(0.975f, 0.19f));
            var name = RuntimeUi.AddText(namePlate.transform, "Applicant Portrait Name 069",
                DisplayName069(applicant).ToUpperInvariant(), 46, TextAnchor.MiddleCenter,
                RuntimeUi.Accent, FontStyle.Bold);
            Stretch069(name.rectTransform, 18f);
        }

        private void AddConversation069(
            GuildCityApplicantView017D applicant,
            GuildMemberDevelopmentView067 development,
            GuildCityPresentationState017D state)
        {
            var panel = RuntimeUi.AddPanel(_root, "Applicant Character Conversation 069",
                new Color(0.035f, 0.065f, 0.11f, 0.95f));
            Anchor069(panel.rectTransform, new Vector2(0.455f, 0.19f), new Vector2(0.96f, 0.84f));
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldPaper);

            var name = RuntimeUi.AddText(panel.transform, "Applicant Spoken Name 069",
                DisplayName069(applicant).ToUpperInvariant(), 60, TextAnchor.MiddleLeft,
                RuntimeUi.Accent, FontStyle.Bold);
            Anchor069(name.rectTransform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.97f));

            var className = Friendly069(applicant.ClassTendencyId);
            var identity = RuntimeUi.AddText(panel.transform, "Applicant Character Identity 069",
                M1VisualAssets.HumanizeRace(applicant.RaceId).ToUpperInvariant() + "  •  " +
                className.ToUpperInvariant() + "  •  " + Friendly069(applicant.WorldId).ToUpperInvariant(),
                30, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            Anchor069(identity.rectTransform, new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.85f));

            var background = FirstNonEmpty069(applicant.PersonalHook, applicant.ObservedSummary,
                "They came to Skyhome looking for a Guild worth standing beside.");
            var story = RuntimeUi.AddText(panel.transform, "Applicant Personal Background 069",
                "\u201c" + TrimSentence069(background, 220) + "\u201d",
                34, TextAnchor.UpperLeft, RuntimeUi.Text, FontStyle.Italic);
            Anchor069(story.rectTransform, new Vector2(0.05f, 0.57f), new Vector2(0.95f, 0.76f));

            var potential = Friendly069(FirstNonEmpty069(development?.PotentialBand, "Under review"));
            var weapon = Friendly069(FirstNonEmpty069(development?.FixedWeaponFamilyId, applicant.EquipmentSummary, "Field gear"));
            var trees070 = development?.TreeSlots070 ?? Array.Empty<GuildMemberTreeSlotView070>();
            var activeTrees070 = trees070.Where(value => value != null && value.Active &&
                                                        !string.IsNullOrWhiteSpace(value.TreeDisplayName))
                .Select(value => value.TreeDisplayName).Take(2).ToArray();
            var earnableTrees070 = trees070.Where(value => value != null && !value.Active && value.Earnable &&
                                                          !string.IsNullOrWhiteSpace(value.TreeDisplayName))
                .Select(value => value.TreeDisplayName).Take(2).ToArray();
            var activePath070 = activeTrees070.Length == 0
                ? Friendly069(FirstNonEmpty069(development?.PrimaryRoleTreeId, className))
                : string.Join(" + ", activeTrees070);
            var earnablePath070 = earnableTrees070.Length == 0
                ? "two personal paths revealed after recruitment"
                : string.Join(" + ", earnableTrees070);
            var profile = RuntimeUi.AddText(panel.transform, "Applicant Growth In Plain Language 069",
                "Growth outlook: " + potential + ".\n" +
                "Starts active: " + activePath070 + ".\n" +
                "Can earn: " + earnablePath070 + ".  Weapon: " + weapon + ".\n" +
                "Opening gear: " + TrimSentence069(FirstNonEmpty069(
                    applicant.EquipmentSummary, development?.StartingAdvantage, "ready field equipment"), 115) + ".",
                30, TextAnchor.UpperLeft, RuntimeUi.Text);
            Anchor069(profile.rectTransform, new Vector2(0.05f, 0.36f), new Vector2(0.95f, 0.57f));

            var promise = RuntimeUi.AddPanel(panel.transform, "Permanent Membership Promise 069",
                applicant.IsSigned
                    ? new Color(0.14f, 0.34f, 0.23f, 0.92f)
                    : new Color(0.28f, 0.20f, 0.08f, 0.94f));
            Anchor069(promise.rectTransform, new Vector2(0.045f, 0.13f), new Vector2(0.955f, 0.34f));
            var promiseText = RuntimeUi.AddText(promise.transform, "Permanent Membership Language 069",
                applicant.IsSigned
                    ? applicant.DisplayName + " is now a permanent Guild member. Their identity, growth, gear, and memories persist."
                    : PermanentPromise069 + "\n" + SigningLine069(applicant, state),
                29, TextAnchor.MiddleLeft,
                applicant.IsSigned ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            Stretch069(promiseText.rectTransform, 22f);

            if (string.IsNullOrWhiteSpace(_status)) return;
            var status = RuntimeUi.AddText(panel.transform, "Recruitment Result 069", _status,
                27, TextAnchor.MiddleLeft, _statusPositive ? RuntimeUi.Positive : RuntimeUi.Error,
                FontStyle.Bold);
            Anchor069(status.rectTransform, new Vector2(0.05f, 0.015f), new Vector2(0.95f, 0.12f));
        }

        private void AddControls069(GuildCityApplicantView017D applicant)
        {
            var previous = RuntimeUi.AddButton(_root, "Previous Applicant 069", "← PREVIOUS", Previous069,
                RuntimeUi.MinimumTouchPixels, RuntimeUi.ButtonNormal);
            Anchor069(previous.GetComponent<RectTransform>(), new Vector2(0.04f, 0.035f), new Vector2(0.235f, 0.145f));
            previous.interactable = _applicants.Length > 1;
            SetButtonFont069(previous, 30);

            _recruitButton = RuntimeUi.AddButton(_root, "Recruit Permanently 069",
                applicant.IsSigned
                    ? "RECRUITED PERMANENTLY"
                    : applicant.CanAfford
                        ? "RECRUIT PERMANENTLY"
                        : "NEED " + applicant.SigningCostTreasuryXp + " XP",
                RecruitSelected069,
                RuntimeUi.PrimaryTouchPixels,
                applicant.IsSigned ? RuntimeUi.Positive : RuntimeUi.Accent);
            Anchor069(_recruitButton.GetComponent<RectTransform>(), new Vector2(0.26f, 0.025f), new Vector2(0.74f, 0.155f));
            _recruitButton.interactable = !applicant.IsSigned && applicant.CanAfford;
            SetButtonFont069(_recruitButton, 38);

            var next = RuntimeUi.AddButton(_root, "Next Applicant 069", "NEXT →", Next069,
                RuntimeUi.MinimumTouchPixels, RuntimeUi.ButtonNormal);
            Anchor069(next.GetComponent<RectTransform>(), new Vector2(0.765f, 0.035f), new Vector2(0.96f, 0.145f));
            next.interactable = _applicants.Length > 1;
            SetButtonFont069(next, 30);
        }

        private void AddNoApplicants069(GuildCityPresentationState017D state)
        {
            var panel = RuntimeUi.AddPanel(_root, "No Applicant Conversation 069",
                new Color(0.035f, 0.065f, 0.11f, 0.96f));
            Anchor069(panel.rectTransform, new Vector2(0.19f, 0.25f), new Vector2(0.81f, 0.76f));
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldPaper);
            var title = RuntimeUi.AddText(panel.transform, "No Applicants Heading 069",
                state != null && state.HasRecruitmentBoard ? "THE INTERVIEWS ARE COMPLETE" : "THE DESK IS QUIET",
                58, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            Anchor069(title.rectTransform, new Vector2(0.07f, 0.60f), new Vector2(0.93f, 0.88f));
            var copy = RuntimeUi.AddText(panel.transform, "No Applicants Copy 069",
                "Return to the Guild Hall. The recruitment clerk will clearly mark when new adventurers are ready to meet.",
                36, TextAnchor.MiddleCenter, RuntimeUi.Text);
            Anchor069(copy.rectTransform, new Vector2(0.09f, 0.29f), new Vector2(0.91f, 0.61f));
            var back = RuntimeUi.AddButton(panel.transform, "No Applicants Back To Hall 069", "RETURN TO HALL", Back069,
                RuntimeUi.PrimaryTouchPixels, RuntimeUi.Accent);
            Anchor069(back.GetComponent<RectTransform>(), new Vector2(0.22f, 0.05f), new Vector2(0.78f, 0.27f));
        }

        private GuildCityApplicantView017D Selected069()
        {
            return _applicants.Length == 0 || _selectedIndex < 0 || _selectedIndex >= _applicants.Length
                ? null
                : _applicants[_selectedIndex];
        }

        private static string SigningLine069(
            GuildCityApplicantView017D applicant,
            GuildCityPresentationState017D state)
        {
            if (applicant.SigningCostTreasuryXp <= 0)
                return "Your founding charter covers this invitation.";
            return "Recruitment costs " + applicant.SigningCostTreasuryXp +
                   " XP. You have " + (state?.TreasuryXp ?? 0L) + " XP to spend.";
        }

        private static string Friendly069(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Unknown";
            var friendly = value.Trim();
            foreach (var prefix in new[]
                     {
                         "WEAPON_FAMILY_", "TREE_CA002_WPN_", "TREE_CA002_ROLE_",
                         "TREE_CA002_MYS_", "TREE_CA002_MYSTIC_", "CLASS_TEND_", "CLASS_", "ROLE_", "WORLD_"
                     })
            {
                if (!friendly.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                friendly = friendly.Substring(prefix.Length);
                break;
            }

            friendly = friendly.Replace('_', ' ').Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(friendly)
                ? "Unknown"
                : char.ToUpperInvariant(friendly[0]) + friendly.Substring(1);
        }

        private static string FirstNonEmpty069(params string[] values)
        {
            return values?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
        }

        private static string TrimSentence069(string value, int maximumCharacters)
        {
            var normalized = (value ?? string.Empty).Trim().TrimEnd('.', '!', '?');
            if (normalized.Length <= maximumCharacters) return normalized;
            var cut = normalized.LastIndexOf(' ', maximumCharacters);
            return normalized.Substring(0, cut > maximumCharacters / 2 ? cut : maximumCharacters).TrimEnd() + "…";
        }

        private static void Stretch069(RectTransform rect, float inset = 0f)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchor069(RectTransform rect, Vector2 minimum, Vector2 maximum)
        {
            if (rect == null) return;
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetButtonFont069(Button button, int fontSize)
        {
            var label = button?.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.fontSize = fontSize;
        }

        private static string DisplayName069(GuildCityApplicantView017D applicant) =>
            string.IsNullOrWhiteSpace(applicant?.DisplayName)
                ? "Unnamed Adventurer"
                : applicant.DisplayName.Trim();

        private static void ClearChildren069(Transform parent)
        {
            if (parent == null) return;
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void OnDestroy()
        {
            Shutdown069();
        }

        private void OnDisable()
        {
            Shutdown069();
        }
    }
}
