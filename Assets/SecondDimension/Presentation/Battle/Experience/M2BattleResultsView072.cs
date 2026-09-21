using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// A cinematic, staged result payoff. It deliberately presents a few meaningful
    /// outcomes in sequence instead of exposing the underlying reward record as a grid.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class M2BattleResultsView072 : MonoBehaviour
    {
        public const float BreakthroughHeroSeconds076 = 1.65f;
        public const float ResultsVeilOpacity076 = 1f;
        public const int MinimumSkillProgressFontSize076 = 16;

        private RectTransform _root;
        private Image _resultsVeil076;
        private CanvasGroup _resultsStageGroup076;
        private Image _breakthroughHeroStage076;
        private Image _breakthroughPortrait076;
        private Text _breakthroughInitials076;
        private Text _breakthroughMember076;
        private Text _breakthroughArt076;
        private Text _breakthroughTrigger076;
        private Text _breakthroughMeaning076;
        private Image _breakthroughProgressFill076;
        private Text _breakthroughProgressLabel076;
        private Text _breakthroughAdvanceCue076;
        private Text _outcomeKicker;
        private Text _outcomeTitle;
        private Text _battleSummary;
        private Image _bossVictoryArt076;
        private Image _heroPortrait;
        private Text _heroInitials;
        private Text _heroName;
        private Text _heroClass;
        private Text _heroXp;
        private Text _heroLevel;
        private Text _heroArt;
        private Image _skillProgressRail076;
        private Image _skillProgressFill076;
        private Text _skillProgressLabel076;
        private Text _heroStats;
        private Text _partyGrowth;
        private Text _storyRewardHeading;
        private Text _storyReward;
        private Text _lootQuality;
        private Text _lootName;
        private Text _lootDetail;
        private Image _lootArtFrame076;
        private Image _lootArt076;
        private Text _guildGrowth;
        private Text _nextObjective;
        private Button _continueButton;
        private Text _continueLabel;
        private CanvasGroup[] _revealStages = Array.Empty<CanvasGroup>();
        private Coroutine _revealRoutine;
        private Coroutine _breakthroughRoutine076;
        private string _breakthroughPresentationKey076 = string.Empty;
        private Action _continueAction;
        private Func<M2BattleView, int> _towerFloorReader156B;

        // Read-only presentation diagnostics used by the compact-layout regressions.
        // None of these values can mutate the authority-backed reward projection.
        public string OutcomeKickerText079 => _outcomeKicker == null ? string.Empty : _outcomeKicker.text;
        public string OutcomeTitleText079 => _outcomeTitle == null ? string.Empty : _outcomeTitle.text;
        public string BattleSummaryText079 => _battleSummary == null ? string.Empty : _battleSummary.text;
        public string StoryRewardHeadingText079 =>
            _storyRewardHeading == null ? string.Empty : _storyRewardHeading.text;
        public string LootHeadingText079 => _lootQuality == null ? string.Empty : _lootQuality.text;
        public string LootNameText079 => _lootName == null ? string.Empty : _lootName.text;
        public string NextObjectiveText079 => _nextObjective == null ? string.Empty : _nextObjective.text;

        public void Initialize(RectTransform host, Action continueAction,
            Func<M2BattleView, int> towerFloorReader156B = null)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

            DisposeVisualRoot();
            _breakthroughPresentationKey076 = string.Empty;
            _continueAction = continueAction;
            _towerFloorReader156B = towerFloorReader156B;
            RuntimeUi.EnsureEventSystem();

            // Results must completely retire stale health bars, nameplates, and tactical
            // controls. Keep this veil fully opaque: even a small amount of transparency
            // lets bright battlefield labels bleed through the intentional stage gutters.
            var veil = RuntimeUi.AddPanel(host, "Battle Results Payoff 072",
                new Color(0.003f, 0.008f, 0.016f, ResultsVeilOpacity076));
            _resultsVeil076 = veil;
            _root = veil.rectTransform;
            Anchor(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _root.SetAsLastSibling();

            var commandTrayCover = RuntimeUi.AddPanel(_root, "Battle Command Tray Cover 074",
                new Color(0.003f, 0.008f, 0.016f, 1f));
            Anchor(commandTrayCover.rectTransform, Vector2.zero, new Vector2(1f, 0.255f),
                Vector2.zero, Vector2.zero);
            commandTrayCover.raycastTarget = false;

            var priorHudCover = RuntimeUi.AddPanel(_root, "Battle Prior HUD Cover 074",
                new Color(0.003f, 0.008f, 0.016f, 1f));
            Anchor(priorHudCover.rectTransform, new Vector2(0f, 0.74f), Vector2.one,
                Vector2.zero, Vector2.zero);
            priorHudCover.raycastTarget = false;

            var panel = RuntimeUi.AddPanel(_root, "Battle Results Studio Stage 076", Color.clear);
            Anchor(panel.rectTransform, new Vector2(0.012f, 0.018f), new Vector2(0.988f, 0.982f),
                Vector2.zero, Vector2.zero);
            panel.raycastTarget = false;
            _resultsStageGroup076 = panel.gameObject.AddComponent<CanvasGroup>();

            var outcomeStage = AddStage(panel.rectTransform, "Outcome Reveal 072",
                new Vector2(0.018f, 0.805f), new Vector2(0.982f, 0.972f),
                new Color(0.008f, 0.018f, 0.032f, 0.96f));
            M1PremiumUi.StylePanel(outcomeStage, M1PremiumUi.Surface.Iron);
            _bossVictoryArt076 = RuntimeUi.AddPanel(
                outcomeStage.transform,
                "Gate-Eater Victory Crest 076",
                Color.clear);
            Anchor(_bossVictoryArt076.rectTransform,
                new Vector2(0.785f, 0.015f), new Vector2(0.992f, 0.985f),
                Vector2.zero, Vector2.zero);
            _bossVictoryArt076.preserveAspect = true;
            _bossVictoryArt076.raycastTarget = false;
            _bossVictoryArt076.gameObject.SetActive(false);
            _outcomeKicker = AddAnchoredText(outcomeStage.transform, "Outcome Kicker 076", "VICTORY", 34,
                TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold,
                new Vector2(0.025f, 0.56f), new Vector2(0.64f, 0.90f));
            ConfigureTextFit(_outcomeKicker, 34, 24);
            _outcomeTitle = AddAnchoredText(outcomeStage.transform, "Outcome Story Title 076", string.Empty, 52,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.025f, 0.08f), new Vector2(0.66f, 0.62f));
            ConfigureTextFit(_outcomeTitle, 52, 34);
            _battleSummary = AddAnchoredText(outcomeStage.transform, "Outcome Summary 072", string.Empty, 29,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.66f, 0.13f), new Vector2(0.975f, 0.87f));
            ConfigureTextFit(_battleSummary, 29, 20);

            var heroStage = AddStage(panel.rectTransform, "Featured Growth Story 076",
                new Vector2(0.018f, 0.255f), new Vector2(0.625f, 0.785f),
                new Color(0.008f, 0.040f, 0.039f, 0.94f));
            M1PremiumUi.StylePanel(heroStage, M1PremiumUi.Surface.Positive);
            var heroHeading = AddAnchoredText(heroStage.transform, "Featured Growth Heading 076",
                "GROWTH  •  PERSONAL PROGRESS", 29, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold,
                new Vector2(0.035f, 0.875f), new Vector2(0.965f, 0.97f));
            ConfigureTextFit(heroHeading, 29, 21);

            var portraitFrame = RuntimeUi.AddPanel(heroStage.transform,
                "Featured Adventurer Portrait Frame 076", RuntimeUi.Accent);
            Anchor(portraitFrame.rectTransform, new Vector2(0.035f, 0.14f), new Vector2(0.355f, 0.855f),
                Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePortraitFrame(portraitFrame, selected: true);
            var portraitBacking = RuntimeUi.AddPanel(portraitFrame.transform,
                "Featured Adventurer Portrait Backing 076", new Color(0.012f, 0.025f, 0.035f, 1f));
            Anchor(portraitBacking.rectTransform, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f),
                Vector2.zero, Vector2.zero);
            portraitBacking.raycastTarget = false;
            _heroPortrait = RuntimeUi.AddPanel(portraitBacking.transform,
                "Featured Adventurer Portrait Art 076", Color.white);
            Anchor(_heroPortrait.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _heroPortrait.preserveAspect = true;
            _heroPortrait.raycastTarget = false;
            _heroInitials = AddAnchoredText(portraitBacking.transform,
                "Featured Adventurer Portrait Fallback 076", "?", 58, TextAnchor.MiddleCenter,
                RuntimeUi.Warning, FontStyle.Bold, Vector2.zero, Vector2.one);
            ConfigureTextFit(_heroInitials, 58, 34);

            _heroName = AddAnchoredText(heroStage.transform, "Featured Adventurer Name 076", string.Empty, 42,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.395f, 0.70f), new Vector2(0.965f, 0.855f));
            ConfigureTextFit(_heroName, 42, 30);
            _heroClass = AddAnchoredText(heroStage.transform, "Featured Adventurer Class 076", string.Empty, 26,
                TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.395f, 0.63f), new Vector2(0.965f, 0.72f));
            ConfigureTextFit(_heroClass, 26, 18);
            _heroXp = AddAnchoredText(heroStage.transform, "Featured Adventurer XP 076", string.Empty, 46,
                TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold,
                new Vector2(0.395f, 0.48f), new Vector2(0.965f, 0.64f));
            ConfigureTextFit(_heroXp, 46, 31);
            _heroLevel = AddAnchoredText(heroStage.transform, "Featured Adventurer Level 076", string.Empty, 29,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.395f, 0.41f), new Vector2(0.965f, 0.50f));
            ConfigureTextFit(_heroLevel, 29, 20);
            _heroArt = AddAnchoredText(heroStage.transform, "Featured Adventurer Art 076", string.Empty, 28,
                TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.395f, 0.325f), new Vector2(0.965f, 0.41f));
            ConfigureTextFit(_heroArt, 28, 19);
            _skillProgressLabel076 = AddAnchoredText(
                heroStage.transform,
                "Next Art Progress Label 076",
                string.Empty,
                23,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 1f, 0.88f, 1f),
                FontStyle.Bold,
                new Vector2(0.395f, 0.215f), new Vector2(0.965f, 0.325f));
            ConfigureTextFit(_skillProgressLabel076, 23, MinimumSkillProgressFontSize076);
            _skillProgressRail076 = RuntimeUi.AddPanel(
                heroStage.transform,
                "Next Art Progress Rail 076",
                new Color(0.018f, 0.075f, 0.072f, 1f));
            Anchor(_skillProgressRail076.rectTransform,
                new Vector2(0.395f, 0.185f), new Vector2(0.965f, 0.212f),
                Vector2.zero, Vector2.zero);
            _skillProgressRail076.raycastTarget = false;
            _skillProgressFill076 = RuntimeUi.AddPanel(
                _skillProgressRail076.transform,
                "Next Art Progress Fill 076",
                new Color(0.30f, 1f, 0.70f, 1f));
            Anchor(_skillProgressFill076.rectTransform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _skillProgressFill076.raycastTarget = false;
            _heroStats = AddAnchoredText(heroStage.transform, "Featured Adventurer Stat Growth 076", string.Empty, 24,
                TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.395f, 0.095f), new Vector2(0.965f, 0.185f));
            ConfigureTextFit(_heroStats, 24, 16);
            _partyGrowth = AddAnchoredText(heroStage.transform, "Party Growth Summary 076", string.Empty, 23,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.035f, 0.018f), new Vector2(0.965f, 0.088f));
            ConfigureTextFit(_partyGrowth, 23, 16);

            var rewardStage = AddStage(panel.rectTransform, "Meaningful Rewards 076",
                new Vector2(0.632f, 0.255f), new Vector2(0.982f, 0.785f),
                new Color(0.050f, 0.038f, 0.016f, 0.94f));
            M1PremiumUi.StylePanel(rewardStage, M1PremiumUi.Surface.Warning);
            var rewardHeading = AddAnchoredText(rewardStage.transform, "Meaningful Rewards Heading 076",
                "LOOT  •  GUILD REWARDS", 29, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.06f, 0.875f), new Vector2(0.94f, 0.97f));
            ConfigureTextFit(rewardHeading, 29, 21);
            _storyRewardHeading = AddAnchoredText(rewardStage.transform, "Story Reward Heading 076", string.Empty, 36,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.87f));
            ConfigureTextFit(_storyRewardHeading, 36, 25);
            _storyReward = AddAnchoredText(rewardStage.transform, "Story Reward Detail 076", string.Empty, 26,
                TextAnchor.UpperLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.06f, 0.58f), new Vector2(0.94f, 0.73f));
            ConfigureTextFit(_storyReward, 26, 18);
            _lootArtFrame076 = RuntimeUi.AddPanel(
                rewardStage.transform,
                "Authored Loot Identity Frame 076",
                new Color(0.08f, 0.065f, 0.025f, 0.98f));
            Anchor(_lootArtFrame076.rectTransform,
                new Vector2(0.055f, 0.255f), new Vector2(0.34f, 0.575f),
                Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePortraitFrame(_lootArtFrame076, selected: true);
            _lootArt076 = RuntimeUi.AddPanel(
                _lootArtFrame076.transform,
                "Authored Loot Identity Art 076",
                Color.clear);
            Anchor(_lootArt076.rectTransform,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f),
                Vector2.zero, Vector2.zero);
            _lootArt076.preserveAspect = true;
            _lootArt076.raycastTarget = false;
            _lootQuality = AddAnchoredText(rewardStage.transform, "Armory Reward Heading 076", "ARMORY SPOILS", 23,
                TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.37f, 0.49f), new Vector2(0.94f, 0.58f));
            ConfigureTextFit(_lootQuality, 23, 16);
            _lootName = AddAnchoredText(rewardStage.transform, "Armory Reward Name 076", string.Empty, 34,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.37f, 0.38f), new Vector2(0.94f, 0.50f));
            ConfigureTextFit(_lootName, 34, 23);
            _lootDetail = AddAnchoredText(rewardStage.transform, "Armory Reward Detail 076", string.Empty, 23,
                TextAnchor.UpperLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.37f, 0.26f), new Vector2(0.94f, 0.39f));
            ConfigureTextFit(_lootDetail, 23, 16);
            _guildGrowth = AddAnchoredText(rewardStage.transform, "Guild Growth Reward 076", string.Empty, 28,
                TextAnchor.UpperLeft, RuntimeUi.Positive, FontStyle.Bold,
                new Vector2(0.06f, 0.055f), new Vector2(0.94f, 0.25f));
            ConfigureTextFit(_guildGrowth, 28, 19);

            var nextStage = AddStage(panel.rectTransform, "Next Story Objective 076",
                new Vector2(0.018f, 0.025f), new Vector2(0.655f, 0.225f),
                new Color(0.008f, 0.018f, 0.032f, 0.97f));
            M1PremiumUi.StylePanel(nextStage, M1PremiumUi.Surface.Iron);
            var nextHeading = AddAnchoredText(nextStage.transform, "Next Story Objective Heading 076",
                "YOUR NEXT ORDER", 26, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.035f, 0.64f), new Vector2(0.965f, 0.91f));
            ConfigureTextFit(nextHeading, 26, 18);
            _nextObjective = AddAnchoredText(nextStage.transform, "Next Story Objective Copy 076", string.Empty, 34,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.035f, 0.10f), new Vector2(0.965f, 0.66f));
            ConfigureTextFit(_nextObjective, 34, 23);

            var continueStage = AddStage(panel.rectTransform, "Continue Reveal 072",
                new Vector2(0.675f, 0.025f), new Vector2(0.982f, 0.225f), Color.clear);
            _continueButton = RuntimeUi.AddButton(continueStage.transform,
                "Continue From Battle Results 072", "CLAIM REWARDS & CONTINUE", HandleContinue,
                RuntimeUi.PrimaryTouchPixels, RuntimeUi.ButtonNormal);
            Anchor(_continueButton.GetComponent<RectTransform>(), Vector2.zero, Vector2.one,
                new Vector2(4f, 4f), new Vector2(-4f, -4f));
            _continueLabel = _continueButton.GetComponentInChildren<Text>();
            var continueColors = _continueButton.colors;
            continueColors.normalColor = new Color(0.055f, 0.18f, 0.31f, 1f);
            continueColors.highlightedColor = new Color(0.075f, 0.30f, 0.43f, 1f);
            continueColors.selectedColor = continueColors.highlightedColor;
            continueColors.pressedColor = new Color(0.025f, 0.105f, 0.18f, 1f);
            _continueButton.colors = continueColors;
            _continueLabel.color = RuntimeUi.Text;
            ConfigureTextFit(_continueLabel, 34, 23);

            _revealStages = new[]
            {
                outcomeStage.GetComponent<CanvasGroup>(),
                heroStage.GetComponent<CanvasGroup>(),
                rewardStage.GetComponent<CanvasGroup>(),
                nextStage.GetComponent<CanvasGroup>(),
                continueStage.GetComponent<CanvasGroup>()
            };
            BuildBreakthroughHero076();
            _root.gameObject.SetActive(false);
        }

        private void BuildBreakthroughHero076()
        {
            _breakthroughHeroStage076 = RuntimeUi.AddPanel(
                _root,
                "Art Breakthrough Hero Stage 076",
                new Color(0.003f, 0.018f, 0.026f, 1f));
            Anchor(_breakthroughHeroStage076.rectTransform,
                new Vector2(0.012f, 0.018f), new Vector2(0.988f, 0.982f),
                Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(_breakthroughHeroStage076, M1PremiumUi.Surface.Positive);

            var title = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Title 076",
                "ART BREAKTHROUGH",
                72,
                TextAnchor.MiddleCenter,
                new Color(0.52f, 1f, 0.80f, 1f),
                FontStyle.Bold,
                new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.96f));
            ConfigureTextFit(title, 72, 46);

            var portraitFrame = RuntimeUi.AddPanel(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Portrait Frame 076",
                RuntimeUi.Accent);
            Anchor(portraitFrame.rectTransform,
                new Vector2(0.075f, 0.16f), new Vector2(0.405f, 0.80f),
                Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePortraitFrame(portraitFrame, selected: true);
            var portraitBacking = RuntimeUi.AddPanel(
                portraitFrame.transform,
                "Art Breakthrough Hero Portrait Backing 076",
                new Color(0.006f, 0.018f, 0.026f, 1f));
            Anchor(portraitBacking.rectTransform,
                new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.975f),
                Vector2.zero, Vector2.zero);
            _breakthroughPortrait076 = RuntimeUi.AddPanel(
                portraitBacking.transform,
                "Art Breakthrough Hero Portrait Art 076",
                Color.white);
            Anchor(_breakthroughPortrait076.rectTransform, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);
            _breakthroughPortrait076.preserveAspect = true;
            _breakthroughPortrait076.raycastTarget = false;
            _breakthroughInitials076 = AddAnchoredText(
                portraitBacking.transform,
                "Art Breakthrough Hero Portrait Fallback 076",
                "?",
                72,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold,
                Vector2.zero, Vector2.one);
            ConfigureTextFit(_breakthroughInitials076, 72, 42);

            _breakthroughMember076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Adventurer 076",
                string.Empty,
                48,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold,
                new Vector2(0.45f, 0.66f), new Vector2(0.94f, 0.80f));
            ConfigureTextFit(_breakthroughMember076, 48, 32);
            _breakthroughArt076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Art 076",
                string.Empty,
                66,
                TextAnchor.MiddleLeft,
                RuntimeUi.Warning,
                FontStyle.Bold,
                new Vector2(0.45f, 0.48f), new Vector2(0.94f, 0.67f));
            ConfigureTextFit(_breakthroughArt076, 66, 40);
            _breakthroughTrigger076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Trigger 076",
                string.Empty,
                32,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold,
                new Vector2(0.45f, 0.41f), new Vector2(0.94f, 0.50f));
            ConfigureTextFit(_breakthroughTrigger076, 32, 23);
            _breakthroughProgressLabel076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Mastery Threshold 076",
                string.Empty,
                28,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 1f, 0.88f, 1f),
                FontStyle.Bold,
                new Vector2(0.45f, 0.33f), new Vector2(0.94f, 0.405f));
            ConfigureTextFit(_breakthroughProgressLabel076, 28, 20);
            var breakthroughRail = RuntimeUi.AddPanel(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Mastery Rail 076",
                new Color(0.018f, 0.075f, 0.072f, 1f));
            Anchor(breakthroughRail.rectTransform,
                new Vector2(0.45f, 0.295f), new Vector2(0.94f, 0.325f),
                Vector2.zero, Vector2.zero);
            breakthroughRail.raycastTarget = false;
            _breakthroughProgressFill076 = RuntimeUi.AddPanel(
                breakthroughRail.transform,
                "Art Breakthrough Mastery Fill 076",
                new Color(0.30f, 1f, 0.70f, 1f));
            Anchor(_breakthroughProgressFill076.rectTransform,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _breakthroughProgressFill076.raycastTarget = false;
            _breakthroughMeaning076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Hero Meaning 076",
                string.Empty,
                32,
                TextAnchor.MiddleLeft,
                RuntimeUi.Positive,
                FontStyle.Bold,
                new Vector2(0.45f, 0.18f), new Vector2(0.94f, 0.285f));
            ConfigureTextFit(_breakthroughMeaning076, 32, 23);
            _breakthroughAdvanceCue076 = AddAnchoredText(
                _breakthroughHeroStage076.transform,
                "Art Breakthrough Rewards Cue 076",
                "REWARDS FOLLOW",
                29,
                TextAnchor.MiddleRight,
                RuntimeUi.MutedText,
                FontStyle.Bold,
                new Vector2(0.45f, 0.07f), new Vector2(0.94f, 0.15f));
            ConfigureTextFit(_breakthroughAdvanceCue076, 29, 21);
            _breakthroughHeroStage076.gameObject.SetActive(false);
        }

        public void Show(M2BattleView battle)
        {
            if (_root == null) throw new InvalidOperationException("Initialize must be called before Show.");

            ApplyBattle(battle);
            if(!string.IsNullOrWhiteSpace(battle?.Reward?.TitanRewardSummary161))
            {
                // Keep the matched reward portrait beside the battle summary,
                // never behind its text. ApplyBattle restores the ordinary layout.
                Anchor(_outcomeTitle.rectTransform,new Vector2(.025f,.08f),new Vector2(.60f,.62f),
                    new Vector2(5f,3f),new Vector2(-5f,-3f));
                Anchor(_battleSummary.rectTransform,new Vector2(.61f,.13f),new Vector2(.865f,.87f),
                    Vector2.zero,Vector2.zero);
                Anchor(_bossVictoryArt076.rectTransform,new Vector2(.88f,.015f),new Vector2(.992f,.985f),
                    Vector2.zero,Vector2.zero);
                _storyRewardHeading.text="TITAN REWARD";
                _storyReward.text=battle.Reward.TitanRewardSummary161;
                _nextObjective.text=battle.Reward.Claimed?"Return to Titan Trials.":"Claim this result to return to Titan Trials.";
                _continueLabel.text=battle.Reward.Claimed?"RETURN TO TITANS":"CLAIM & RETURN TO TITANS";
                var portrait161=TitanArt161.HeroPortrait(battle.Reward.TitanRewardHeroId161);
                if(portrait161!=null&&StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome,"Victory"))
                {_bossVictoryArt076.sprite=portrait161;_bossVictoryArt076.color=Color.white;_bossVictoryArt076.gameObject.SetActive(true);}
            }
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            var breakthrough = LatestBreakthrough076(battle);
            var key = BreakthroughPresentationKey076(battle, breakthrough);
            if (breakthrough != null &&
                !StringComparer.Ordinal.Equals(key, _breakthroughPresentationKey076))
            {
                _breakthroughPresentationKey076 = key;
                ShowBreakthroughHero076(battle, breakthrough);
                return;
            }
            if (_breakthroughHeroStage076 != null && _breakthroughHeroStage076.gameObject.activeSelf)
                return;
            ShowStandardResults076();
        }

        private void ShowStandardResults076()
        {
            StopBreakthrough076();
            if (_breakthroughHeroStage076 != null)
                _breakthroughHeroStage076.gameObject.SetActive(false);
            if (_resultsStageGroup076 != null)
            {
                _resultsStageGroup076.alpha = 1f;
                _resultsStageGroup076.interactable = true;
                _resultsStageGroup076.blocksRaycasts = true;
            }
            PlayReveal();
        }

        public void Hide()
        {
            ResetBreakthroughVisual076();
            StopReveal();
            if (_root != null) _root.gameObject.SetActive(false);
        }

        public void SetContinueInteractable(bool interactable)
        {
            if (_continueButton != null) _continueButton.interactable = interactable;
        }

        public void ResumeClaim120(M2BattleView battle, bool saving)
        {
            ApplyBattle(battle);
            ResetBreakthroughVisual076();
            StopReveal();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            // Claim was already deliberately requested in the previous view. Its
            // status and any retry must be visible immediately, with no reveal
            // coroutine later enabling Continue while the save remains pending.
            for (var index = 0; index < _revealStages.Length; index++)
            {
                var stage = _revealStages[index];
                if (stage == null) continue;
                stage.alpha = 1f;
                stage.interactable = index == _revealStages.Length - 1;
                stage.blocksRaycasts = index == _revealStages.Length - 1;
            }
            ShowClaimPending120(saving);
        }

        public void ShowClaimPending120(bool saving)
        {
            if (_nextObjective != null)
            {
                _nextObjective.text = saving ? "SAVING…" : "CLAIMING REWARDS…";
                _nextObjective.color = RuntimeUi.Text;
            }
            SetContinueInteractable(false);
        }

        public void ShowClaimFailure107(string message)
        {
            // The battle story ribbon is hidden behind terminal results. Keep a
            // failed claim visible here and leave its ordinary retry button usable.
            if (_nextObjective != null)
            {
                _nextObjective.text = message ?? "The reward could not be secured. Try again.";
                _nextObjective.color = RuntimeUi.Error;
            }
            SetContinueInteractable(true);
        }

        private void ShowBreakthroughHero076(M2BattleView battle, M2BattleEventView breakthrough)
        {
            StopReveal();
            StopBreakthrough076();
            if (_resultsStageGroup076 != null)
            {
                _resultsStageGroup076.alpha = 0f;
                _resultsStageGroup076.interactable = false;
                _resultsStageGroup076.blocksRaycasts = false;
            }

            var memberId = breakthrough?.MemberId ?? breakthrough?.ActorMemberId ?? string.Empty;
            var member = FindBattleMember(battle, memberId);
            var memberName = CleanDisplayName(
                member?.DisplayName,
                BreakthroughMemberName076(breakthrough?.Text, "Guild Adventurer"));
            var artName = M2BattleReadableText021.ArtDisplayName(
                breakthrough?.ArtId,
                breakthrough?.Text,
                true);
            if (string.IsNullOrWhiteSpace(artName)) artName = "New Art";

            _breakthroughMember076.text = memberName.ToUpperInvariant();
            _breakthroughArt076.text = artName.ToUpperInvariant();
            _breakthroughTrigger076.text = BreakthroughTrigger076(breakthrough?.Text);
            _breakthroughMeaning076.text = BreakthroughMeaning076(breakthrough?.Text);
            ApplyBreakthroughThreshold076(breakthrough);
            _breakthroughAdvanceCue076.text = "REWARDS FOLLOW";
            ApplyBreakthroughPortrait076(member, memberName);

            _breakthroughHeroStage076.gameObject.SetActive(true);
            _breakthroughHeroStage076.transform.SetAsLastSibling();
            _breakthroughRoutine076 = StartCoroutine(AdvanceFromBreakthrough076());
        }

        private IEnumerator AdvanceFromBreakthrough076()
        {
            yield return new WaitForSecondsRealtime(BreakthroughHeroSeconds076);
            _breakthroughRoutine076 = null;
            if (_breakthroughHeroStage076 != null)
                _breakthroughHeroStage076.gameObject.SetActive(false);
            if (_resultsStageGroup076 != null)
            {
                _resultsStageGroup076.alpha = 1f;
                _resultsStageGroup076.interactable = true;
                _resultsStageGroup076.blocksRaycasts = true;
            }
            PlayReveal();
        }

        private void ApplyBreakthroughPortrait076(M2BattleMemberView member, string displayName)
        {
            if (_breakthroughPortrait076 == null || _breakthroughInitials076 == null) return;
            _breakthroughPortrait076.sprite = null;
            _breakthroughPortrait076.color = new Color(0.006f, 0.018f, 0.026f, 1f);
            _breakthroughInitials076.text = Initials(displayName);
            _breakthroughInitials076.gameObject.SetActive(true);
            if (member == null) return;
            if (!TryResolveResultHeroSprite099(member, out var sprite)) return;
            _breakthroughPortrait076.sprite = sprite;
            _breakthroughPortrait076.color = Color.white;
            _breakthroughInitials076.gameObject.SetActive(false);
        }

        private static M2BattleEventView LatestBreakthrough076(M2BattleView battle)
        {
            var events = battle?.Events ?? Array.Empty<M2BattleEventView>();
            for (var index = events.Count - 1; index >= 0; index--)
                if (events[index] != null && StringComparer.OrdinalIgnoreCase.Equals(
                        events[index].EventType,
                        "BREAKTHROUGH"))
                    return events[index];
            return null;
        }

        private static string BreakthroughPresentationKey076(
            M2BattleView battle,
            M2BattleEventView breakthrough)
        {
            if (breakthrough == null) return string.Empty;
            return (battle?.BattleId ?? string.Empty) + "|" +
                   (battle?.FinalStateHash ?? string.Empty) + "|" +
                   breakthrough.Sequence.ToString(CultureInfo.InvariantCulture) + "|" +
                   (breakthrough.MemberId ?? breakthrough.ActorMemberId ?? string.Empty) + "|" +
                   (breakthrough.ArtId ?? string.Empty);
        }

        private static string BreakthroughMemberName076(string eventText, string fallback)
        {
            if (string.IsNullOrWhiteSpace(eventText)) return fallback;
            var markers = new[] { " turns meaningful ", " fills the ", " learned " };
            for (var index = 0; index < markers.Length; index++)
            {
                var marker = eventText.IndexOf(markers[index], StringComparison.OrdinalIgnoreCase);
                if (marker > 0) return CleanDisplayName(eventText.Substring(0, marker), fallback);
            }
            return fallback;
        }

        private static string BreakthroughTrigger076(string eventText)
        {
            var sourceArt = "COMBAT MASTERY";
            if (!string.IsNullOrWhiteSpace(eventText))
            {
                const string startMarker = " turns meaningful ";
                const string endMarker = " use into";
                var start = eventText.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
                if (start >= 0)
                {
                    start += startMarker.Length;
                    var end = eventText.IndexOf(endMarker, start, StringComparison.OrdinalIgnoreCase);
                    if (end > start) sourceArt = eventText.Substring(start, end - start).Trim().ToUpperInvariant();
                }
                else
                {
                    const string throughMarker = " through ";
                    var through = eventText.IndexOf(
                        throughMarker,
                        StringComparison.OrdinalIgnoreCase);
                    if (through >= 0)
                    {
                        through += throughMarker.Length;
                        var end = eventText.IndexOf('.', through);
                        if (end < 0) end = eventText.Length;
                        if (end > through)
                            sourceArt = eventText.Substring(through, end - through)
                                .Trim()
                                .ToUpperInvariant();
                    }
                }
            }
            return "MASTERED IN BATTLE  •  " + sourceArt;
        }

        private static string BreakthroughMeaning076(string eventText)
        {
            return "PERMANENTLY LEARNED  •  READY FOR FUTURE BATTLES";
        }

        private void ApplyBreakthroughThreshold076(M2BattleEventView breakthrough)
        {
            var reported = Math.Max(0, breakthrough?.Amount ?? 0);
            var reached = reported <= 0 ? 100 : Math.Min(100, reported);
            if (_breakthroughProgressFill076 != null)
            {
                var rect = _breakthroughProgressFill076.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(Mathf.Clamp01(reached / 100f), 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            if (_breakthroughProgressLabel076 != null)
                _breakthroughProgressLabel076.text = reported > 0
                    ? "DISCOVERY  " + reached.ToString(CultureInfo.InvariantCulture) +
                      " / 100  •  THRESHOLD REACHED"
                    : "DISCOVERY THRESHOLD  •  REACHED";
        }

        private void StopBreakthrough076()
        {
            if (_breakthroughRoutine076 == null) return;
            StopCoroutine(_breakthroughRoutine076);
            _breakthroughRoutine076 = null;
        }

        private void ResetBreakthroughVisual076()
        {
            StopBreakthrough076();
            if (_breakthroughHeroStage076 != null)
                _breakthroughHeroStage076.gameObject.SetActive(false);
            if (_resultsStageGroup076 == null) return;
            _resultsStageGroup076.alpha = 1f;
            _resultsStageGroup076.interactable = true;
            _resultsStageGroup076.blocksRaycasts = true;
        }

        private void ApplyBattle(M2BattleView battle)
        {
            Anchor(_outcomeTitle.rectTransform,new Vector2(.025f,.08f),new Vector2(.66f,.62f),
                new Vector2(5f,3f),new Vector2(-5f,-3f));
            Anchor(_bossVictoryArt076.rectTransform,new Vector2(.785f,.015f),new Vector2(.992f,.985f),
                Vector2.zero,Vector2.zero);
            var outcome = battle?.Outcome ?? string.Empty;
            var victory = StringComparer.OrdinalIgnoreCase.Equals(outcome, "Victory");
            var retreat = StringComparer.OrdinalIgnoreCase.Equals(outcome, "Retreat") ||
                          StringComparer.OrdinalIgnoreCase.Equals(outcome, "Withdrawal");
            var presentation = BattlePayoffPresentation076.For(battle, victory, retreat,
                _towerFloorReader156B?.Invoke(battle) ?? 0);
            var bossVictory = victory && IsGateEaterPayoff076(battle);
            var fogStalkerVictory079 = victory && IsFogStalkerEncounter079(battle);

            if (_resultsVeil076 != null)
                _resultsVeil076.color = bossVictory
                    ? new Color(0.014f, 0.004f, 0.032f, ResultsVeilOpacity076)
                    : new Color(0.003f, 0.008f, 0.016f, ResultsVeilOpacity076);
            _outcomeKicker.text = bossVictory
                ? "BOSS VICTORY  •  SKYHOME SECURED"
                : victory
                    ? FirstHourVictoryKicker076(battle)
                    : retreat ? "WITHDRAWAL  •  UNION PRESERVED" : "DEFEAT  •  OBJECTIVE LOST";
            _outcomeKicker.color = victory ? RuntimeUi.Positive : retreat ? RuntimeUi.Warning : RuntimeUi.Error;
            _outcomeTitle.text = presentation.OutcomeTitle;
            ApplyBossVictoryArt076(bossVictory);
            _battleSummary.text = BuildBattleSummary079(battle, victory);
            Anchor(
                _battleSummary.rectTransform,
                bossVictory
                    ? new Vector2(0.59f, 0.13f)
                    : fogStalkerVictory079
                        ? new Vector2(0.66f, 0.10f)
                        : new Vector2(0.66f, 0.13f),
                bossVictory
                    ? new Vector2(0.79f, 0.87f)
                    : fogStalkerVictory079
                        ? new Vector2(0.975f, 0.90f)
                        : new Vector2(0.975f, 0.87f),
                Vector2.zero,
                Vector2.zero);

            ApplyFeaturedGrowth(battle, victory);
            ApplyRewards(battle?.Reward, presentation, victory);
            _nextObjective.text = presentation.NextObjective;
            _nextObjective.color = RuntimeUi.Text;
            if (_continueLabel != null) _continueLabel.text = presentation.ContinueLabel;
        }

        private void ApplyFeaturedGrowth(M2BattleView battle, bool victory)
        {
            IReadOnlyList<M2BattleMemberRewardView> rewards =
                (battle?.Reward?.MemberRewards ?? Array.Empty<M2BattleMemberRewardView>())
                .Where(value => value != null && (value.PersonalXp > 0 || value.LevelsGained > 0 ||
                    value.ProjectedLevel > value.PreviousLevel)).ToArray();
            var featured = FeaturedReward(battle, rewards);
            var member = featured == null
                ? FirstBattleMember(battle)
                : FindBattleMember(battle, featured.MemberId);
            var projectedRewardLevel = featured == null
                ? 0
                : Math.Max(
                    Math.Max(1, featured.PreviousLevel),
                    featured.ProjectedLevel);
            var name = CleanDisplayName(featured?.DisplayName,
                CleanDisplayName(member?.DisplayName, victory ? "Guild Adventurer" : "The Union"));
            _heroName.text = name.ToUpperInvariant();
            _heroClass.text = member == null || string.IsNullOrWhiteSpace(member.ClassName)
                ? "GUILD ADVENTURER"
                : Friendly(member.ClassName, "Guild Adventurer").ToUpperInvariant();
            ApplyFeaturedPortrait(member, name);
            ApplySkillProgress076(member?.NextSkillProgress, projectedRewardLevel);

            if (featured == null)
            {
                _heroXp.text = "NO PERSONAL XP REWARD";
                _heroLevel.text = "No personal XP or level gains are listed.";
                _heroArt.text = member != null && !string.IsNullOrWhiteSpace(member.ArtGrowthSummary)
                    ? Compact(member.ArtGrowthSummary, 58)
                    : "No Art growth is listed.";
                _heroStats.text = string.Empty;
                _partyGrowth.text = victory
                    ? "THE COMPANY FOUGHT AS ONE UNION FORCE"
                    : "REGROUP AT THE GUILD HALL";
                return;
            }

            var previous = Math.Max(1, featured.PreviousLevel);
            var projected = Math.Max(previous, featured.ProjectedLevel);
            _heroXp.text = "PERSONAL XP  •  +" + FormatNumber(featured.PersonalXp) + " XP";
            _heroLevel.text = projected > previous
                ? "LEVEL " + previous.ToString(CultureInfo.InvariantCulture) + "  →  " +
                  projected.ToString(CultureInfo.InvariantCulture)
                : "LEVEL " + previous.ToString(CultureInfo.InvariantCulture) + "  •  XP TOWARD NEXT LEVEL";
            _heroArt.text = BuildMemberArtGrowth(battle, featured.MemberId);
            _heroStats.text = BuildStatGrowth(featured);

            var levels = rewards.Sum(value => value == null
                ? 0
                : Math.Max(value.LevelsGained,
                    Math.Max(0, value.ProjectedLevel - value.PreviousLevel)));
            _partyGrowth.text = rewards.Count.ToString(CultureInfo.InvariantCulture) +
                                (rewards.Count == 1 ? " ADVENTURER" : " ADVENTURERS") +
                                (battle?.Reward?.Claimed == true ? "   •   XP SAVED" : "   •   XP TO CLAIM") +
                                (levels > 0
                                    ? "   •   " + levels.ToString(CultureInfo.InvariantCulture) +
                                      (levels == 1 ? " LEVEL" : " LEVELS")
                                    : string.Empty);
        }

        private void ApplyFeaturedPortrait(M2BattleMemberView member, string displayName)
        {
            _heroPortrait.sprite = null;
            _heroPortrait.color = new Color(0.012f, 0.025f, 0.035f, 1f);
            _heroInitials.text = Initials(displayName);
            _heroInitials.gameObject.SetActive(true);
            if (member == null) return;
            if (!TryResolveResultHeroSprite099(member, out var sprite)) return;
            _heroPortrait.sprite = sprite;
            _heroPortrait.color = Color.white;
            _heroInitials.gameObject.SetActive(false);
        }

        private static bool TryResolveResultHeroSprite099(M2BattleMemberView member, out Sprite sprite)
        {
            // Use this exact hero's already-framed battle body in both result stages.
            // Existing portrait/initial fallbacks remain for identities with no body;
            // no new family substitution, crop, or resource ownership is introduced.
            return M1VisualAssets.TryResolveBattleStandee(member.MemberId, member.VisualSeed,
                       member.RaceId, member.PortraitAuthorityId, out sprite, out _) ||
                   M1VisualAssets.TryResolvePortrait(member.MemberId, member.VisualSeed,
                       member.RaceId, member.PortraitAuthorityId, out sprite, out _);
        }

        private void ApplySkillProgress076(
            M2BattleSkillProgressView progress,
            int projectedRewardLevel)
        {
            var visible = progress != null && !string.IsNullOrWhiteSpace(progress.DisplayName);
            if (_skillProgressLabel076 != null)
            {
                _skillProgressLabel076.gameObject.SetActive(visible);
                _skillProgressLabel076.text = visible
                    ? SkillProgressLabel076(progress, projectedRewardLevel)
                    : string.Empty;
            }
            if (_skillProgressRail076 != null) _skillProgressRail076.gameObject.SetActive(visible);
            if (_skillProgressFill076 == null) return;
            var rect = _skillProgressFill076.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(visible ? SkillProgressRatio076(progress) : 0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static float SkillProgressRatio076(M2BattleSkillProgressView progress)
        {
            if (progress == null) return 0f;
            if (progress.RequiredPoints <= 0) return 1f;
            return Mathf.Clamp01(Math.Max(0, progress.CurrentPoints) / (float)progress.RequiredPoints);
        }

        public static string SkillProgressLabel076(
            M2BattleSkillProgressView progress,
            int projectedRewardLevel = 0)
        {
            if (progress == null || string.IsNullOrWhiteSpace(progress.DisplayName)) return string.Empty;
            var required = Math.Max(0, progress.RequiredPoints);
            var current = Math.Max(0, progress.CurrentPoints);
            var shown = required > 0 ? Math.Min(current, required) : current;
            var kind = string.IsNullOrWhiteSpace(progress.ProgressKind)
                ? "DISCOVERY"
                : progress.ProgressKind.Trim().ToUpperInvariant();
            var requiredLevel = Math.Max(1, progress.RequiredLevel);
            var pointsReady = required <= 0 || current >= required;
            var projectedGateOpened = !progress.LevelGateMet && pointsReady &&
                                      projectedRewardLevel >= requiredLevel;
            var gate = !progress.LevelGateMet
                ? projectedGateOpened
                    ? "LEVEL " + requiredLevel.ToString(CultureInfo.InvariantCulture) +
                      " EARNED  •  READY NEXT USE"
                    : "LEVEL " + requiredLevel.ToString(CultureInfo.InvariantCulture) + " REQUIRED"
                : pointsReady
                    ? "READY TO LEARN"
                    : (required - current).ToString(CultureInfo.InvariantCulture) + " TO GO";
            if (projectedGateOpened)
                return "NEXT ART  •  " + CleanDisplayName(progress.DisplayName, "Unknown Art").ToUpperInvariant() +
                       "\n" + kind + " " + shown.ToString(CultureInfo.InvariantCulture) + "/" +
                       required.ToString(CultureInfo.InvariantCulture) + "  •  " + gate;
            return "NEXT ART  •  " + CleanDisplayName(progress.DisplayName, "Unknown Art").ToUpperInvariant() +
                   "\n" + kind + "  " + shown.ToString(CultureInfo.InvariantCulture) + " / " +
                   required.ToString(CultureInfo.InvariantCulture) + "  •  " + gate;
        }

        private void ApplyRewards(
            M2BattleRewardView reward,
            BattlePayoffPresentation076 presentation,
            bool victory)
        {
            _storyRewardHeading.text = presentation.StoryRewardHeading;
            _storyReward.text = presentation.StoryReward;
            var memberXp = reward?.GuildTreasuryXpAward ?? 0L;
            var hallXp = reward?.HallEnhancementXpAward ?? 0L;
            var levelBefore = Math.Max(1, reward?.GuildPreviousLevel ?? 1);
            var levelAfter = Math.Max(levelBefore, reward?.GuildProjectedLevel ?? levelBefore);
            var titanReward161=!string.IsNullOrWhiteSpace(reward?.TitanRewardSummary161);
            _guildGrowth.text = (titanReward161?"TREASURY XP  +":"MEMBER XP TO SPEND  +") + FormatNumber(memberXp) + "\n" +
                                (titanReward161?"GUILD XP  +":"HALL IMPROVEMENT  +") + FormatNumber(hallXp);
            if (reward != null)
                _guildGrowth.text += "\n" + (levelAfter > levelBefore
                    ? "GUILD LEVEL  " + levelBefore.ToString(CultureInfo.InvariantCulture) +
                      "  →  " + levelAfter.ToString(CultureInfo.InvariantCulture)
                    : "GUILD LEVEL  " + levelBefore.ToString(CultureInfo.InvariantCulture));

            if (string.IsNullOrWhiteSpace(reward?.EquipmentRewardInstanceId))
            {
                _lootQuality.text = "NO EQUIPMENT REWARD";
                _lootName.text = "No equipment recovered.";
                _lootDetail.text = "XP rewards are listed on this screen.";
                ApplyLootArtwork076(null, false);
                return;
            }

            // Outcome headings stay separate from the authority-backed reward.
            // Displaying an item or XP never claims it or changes the battle result.
            var equipment = CleanDisplayName(
                reward.EquipmentRewardDisplayName,
                "Equipment reward");
            var quality = Friendly(reward.EquipmentRewardQualityId, "Field").ToUpperInvariant();
            var slot = FirstEquipmentSlot(reward.EquipmentRewardValidSlotIds);
            _lootQuality.text = victory
                ? string.IsNullOrWhiteSpace(presentation.LootHeadingOverride)
                    ? "ARMORY SPOILS  •  " + presentation.LootContext
                    : presentation.LootHeadingOverride
                : "ARMORY RECOVERY";
            _lootName.text = equipment;
            _lootDetail.text = quality + (string.IsNullOrWhiteSpace(slot) ? string.Empty : "  •  " + slot) +
                                "\n" + (victory ? presentation.LootPurpose : reward.Claimed
                                    ? "Saved to your inventory."
                                    : "Added to your inventory when this reward is claimed.");
            ApplyLootArtwork076(reward, true);
        }

        private void ApplyLootArtwork076(M2BattleRewardView reward, bool victory)
        {
            if (_lootArtFrame076 == null || _lootArt076 == null) return;
            _lootArt076.sprite = null;
            _lootArt076.color = Color.clear;
            _lootArtFrame076.gameObject.SetActive(victory && reward != null);
            if (!victory || reward == null) return;

            var rawSlot = reward.EquipmentRewardValidSlotIds != null &&
                          reward.EquipmentRewardValidSlotIds.Count > 0
                ? reward.EquipmentRewardValidSlotIds[0]
                : string.Empty;
            var visualId = M1VisualAssets.EquipmentVisualId(
                reward.EquipmentRewardDefinitionId,
                rawSlot,
                new[] { reward.EquipmentRewardDisplayName ?? string.Empty });
            if (!M1VisualAssets.TryResolveEquipment(visualId, out var sprite, out _) || sprite == null)
                return;
            _lootArt076.sprite = sprite;
            _lootArt076.color = Color.white;
        }

        private void ApplyBossVictoryArt076(bool visible)
        {
            if (_bossVictoryArt076 == null) return;
            _bossVictoryArt076.sprite = null;
            _bossVictoryArt076.color = Color.clear;
            _bossVictoryArt076.gameObject.SetActive(visible);
            if (!visible) return;
            var sprite = BattleArtRuntimeRegistry011.LoadSprite(
                M2BattleActorRig072.GateEaterIdleResourcePath076);
            if (sprite == null) return;
            _bossVictoryArt076.sprite = sprite;
            _bossVictoryArt076.color = new Color(1f, 0.72f, 0.38f, 0.56f);
        }

        private static bool IsGateEaterPayoff076(M2BattleView battle) =>
            Contains(battle?.BattleId, "GATE_EATER") ||
            Contains(battle?.Objective, "Gate-Eater");

        private static string FirstHourVictoryKicker076(M2BattleView battle)
        {
            if (IsSurveyorRescueEncounter079(battle))
                return "VICTORY  •  SURVEYORS RESCUED";
            if (IsFogStalkerEncounter079(battle))
                return "VICTORY  •  WAYGLASS LINE SECURED";
            if (Contains(battle?.BattleId, "HALL_BREACH") ||
                Contains(battle?.Objective, "Guild Hall"))
                return "VICTORY  •  GUILD HALL SECURED";
            if (Contains(battle?.BattleId, "LANTERN_ROAD_AMBUSH") ||
                Contains(battle?.Objective, "Lantern Road ambush"))
                return "VICTORY  •  LANTERN ROAD CLEARED";
            return "VICTORY  •  OBJECTIVE SECURED";
        }

        public static bool IsFogStalkerEncounter079(M2BattleView battle) =>
            Contains(battle?.BattleId, "ENCOUNTER_FOG_STALKERS_STANDARD") ||
            Contains(battle?.Objective, "Fog Stalker") ||
            Contains(battle?.Objective, "Fog-Stalker");

        public static bool IsSurveyorRescueEncounter079(M2BattleView battle) =>
            Contains(battle?.BattleId, "ENCOUNTER_SURVEYOR_RESCUE");

        /// <summary>
        /// Keeps the 1280-wide outcome ribbon free of forced ellipses. The Fog-Stalker
        /// encounter has a short authored recap; other encounters retain their
        /// established compact objective treatment.
        /// </summary>
        public static string BuildBattleSummary079(M2BattleView battle, bool victory)
        {
            var rounds = ResolvedRounds(battle);
            var objective = IsSurveyorRescueEncounter079(battle)
                ? victory
                    ? "ORRA'S CREW IS FREE\nTHE DOOR IS SECURED"
                    : "THE SURVEY CREW REMAINS TRAPPED"
                : IsFogStalkerEncounter079(battle)
                ? victory
                    ? "FOG-STALKERS DOWN\nORRA'S LINE HOLDS"
                    : "THE FALSE LINE STILL HOLDS"
                : string.IsNullOrWhiteSpace(battle?.Objective)
                    ? victory ? "Enemy force defeated." : "The objective remains incomplete."
                    : Compact(battle.Objective, 82);
            return objective + "\n" + (victory ? "WON IN " : "ENDED AFTER ") +
                   rounds.ToString(CultureInfo.InvariantCulture) +
                   (rounds == 1 ? " ROUND" : " ROUNDS");
        }

        private static bool Contains(string value, string fragment) =>
            !string.IsNullOrWhiteSpace(value) &&
            value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;

        private static M2BattleMemberRewardView FeaturedReward(
            M2BattleView battle,
            IReadOnlyList<M2BattleMemberRewardView> rewards)
        {
            if (rewards == null || rewards.Count == 0) return null;
            var events = battle?.Events ?? Array.Empty<M2BattleEventView>();
            for (var index = 0; index < events.Count; index++)
            {
                var item = events[index];
                if (item == null || !StringComparer.Ordinal.Equals(item.EventType, "BREAKTHROUGH")) continue;
                for (var rewardIndex = 0; rewardIndex < rewards.Count; rewardIndex++)
                    if (rewards[rewardIndex] != null && MatchesMember(item, rewards[rewardIndex].MemberId))
                        return rewards[rewardIndex];
            }

            M2BattleMemberRewardView best = null;
            var bestLevelGain = int.MinValue;
            var bestStatGain = int.MinValue;
            var bestXp = long.MinValue;
            for (var index = 0; index < rewards.Count; index++)
            {
                var candidate = rewards[index];
                if (candidate == null) continue;
                var levelGain = Math.Max(candidate.LevelsGained,
                    Math.Max(0, candidate.ProjectedLevel - candidate.PreviousLevel));
                var statGain = candidate.MaximumHpGain + candidate.MaximumMpGain + candidate.StrengthGain +
                               candidate.DefenseGain + candidate.AgilityGain + candidate.MagicGain + candidate.WillGain;
                var xp = Math.Max(0L, candidate.PersonalXp);
                var stronger = best == null ||
                               levelGain > bestLevelGain ||
                               levelGain == bestLevelGain && statGain > bestStatGain ||
                               levelGain == bestLevelGain && statGain == bestStatGain && xp > bestXp;
                if (!stronger) continue;
                best = candidate;
                bestLevelGain = levelGain;
                bestStatGain = statGain;
                bestXp = xp;
            }
            return best ?? rewards[0];
        }

        private static string BuildStatGrowth(M2BattleMemberRewardView reward)
        {
            if (reward == null) return "Progress recorded in the Guild ledger.";
            var values = new List<string>();
            AddStat(values, "HP", reward.MaximumHpGain);
            AddStat(values, "MP", reward.MaximumMpGain);
            AddStat(values, "STR", reward.StrengthGain);
            AddStat(values, "DEF", reward.DefenseGain);
            AddStat(values, "AGI", reward.AgilityGain);
            AddStat(values, "MAG", reward.MagicGain);
            AddStat(values, "WILL", reward.WillGain);
            return values.Count > 0
                ? "STAT GROWTH  •  " + string.Join("   ", values.Take(4))
                : "Progress recorded in the Guild ledger.";
        }

        private static void AddStat(ICollection<string> values, string label, int amount)
        {
            if (amount > 0) values.Add(label + " +" + amount.ToString(CultureInfo.InvariantCulture));
        }

        private static string BuildMemberArtGrowth(M2BattleView battle, string memberId)
        {
            var events = battle?.Events ?? Array.Empty<M2BattleEventView>();
            for (var pass = 0; pass < 2; pass++)
                for (var index = 0; index < events.Count; index++)
                {
                    var item = events[index];
                    if (item == null || !MatchesMember(item, memberId)) continue;
                    var breakthrough = StringComparer.Ordinal.Equals(item.EventType, "BREAKTHROUGH");
                    var growth = StringComparer.Ordinal.Equals(item.EventType, "ART_GROWTH");
                    if ((pass == 0 && !breakthrough) || (pass == 1 && !growth)) continue;
                    var art = M2BattleReadableText021.ArtDisplayName(item.ArtId, item.Text, breakthrough);
                    if (string.IsNullOrWhiteSpace(art)) art = breakthrough ? "New Art" : "Art mastery";
                    return breakthrough
                        ? "NEW ART  •  " + Compact(art, 42)
                        : "MASTERY " + (item.Amount > 0 ? "+" + item.Amount + "  •  " : "•  ") +
                          Compact(art, 40);
                }

            var member = FindBattleMember(battle, memberId);
            return member != null && !string.IsNullOrWhiteSpace(member.ArtGrowthSummary)
                ? Compact(member.ArtGrowthSummary, 64)
                : "Arts advanced through meaningful use.";
        }

        private static bool MatchesMember(M2BattleEventView item, string memberId) =>
            !string.IsNullOrWhiteSpace(memberId) &&
            (StringComparer.Ordinal.Equals(item.MemberId, memberId) ||
             StringComparer.Ordinal.Equals(item.ActorMemberId, memberId));

        private static M2BattleMemberView FirstBattleMember(M2BattleView battle)
        {
            if (battle?.PlayerUnions == null) return null;
            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
            {
                var members = battle.PlayerUnions[unionIndex]?.Members;
                if (members != null && members.Count > 0) return members[0];
            }
            return null;
        }

        private static M2BattleMemberView FindBattleMember(M2BattleView battle, string memberId)
        {
            if (battle?.PlayerUnions == null || string.IsNullOrWhiteSpace(memberId)) return null;
            for (var unionIndex = 0; unionIndex < battle.PlayerUnions.Count; unionIndex++)
            {
                var members = battle.PlayerUnions[unionIndex]?.Members;
                if (members == null) continue;
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                    if (members[memberIndex] != null &&
                        StringComparer.Ordinal.Equals(members[memberIndex].MemberId, memberId))
                        return members[memberIndex];
            }
            return null;
        }

        private static int ResolvedRounds(M2BattleView battle)
        {
            if (battle == null) return 1;
            return Math.Max(1, battle.LastResolvedRound > 0 ? battle.LastResolvedRound : battle.Round);
        }

        private static string FirstEquipmentSlot(IReadOnlyList<string> slots)
        {
            if (slots == null || slots.Count == 0) return string.Empty;
            var slot = Friendly(slots[0], string.Empty).ToUpperInvariant();
            return slot.Replace("MAIN HAND", "MAIN-HAND WEAPON")
                .Replace("OFF HAND", "OFF-HAND GEAR");
        }

        private static string Initials(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "?";
            var words = displayName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "?";
            return words.Length == 1
                ? words[0].Substring(0, Math.Min(2, words[0].Length)).ToUpperInvariant()
                : (words[0][0].ToString() + words[words.Length - 1][0]).ToUpperInvariant();
        }

        private void HandleContinue()
        {
            if (_continueButton != null) _continueButton.interactable = false;
            _continueAction?.Invoke();
        }

        private static Image AddStage(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color color)
        {
            var stage = RuntimeUi.AddPanel(parent, name, color);
            Anchor(stage.rectTransform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            stage.gameObject.AddComponent<CanvasGroup>();
            return stage;
        }

        private static Text AddAnchoredText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var text = RuntimeUi.AddText(parent, name, value, fontSize, alignment, color, style);
            Anchor(text.rectTransform, anchorMin, anchorMax, new Vector2(5f, 3f), new Vector2(-5f, -3f));
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureTextFit(Text text, int maximumSize, int minimumSize)
        {
            if (text == null) return;
            text.fontSize = maximumSize;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = minimumSize;
            text.resizeTextMaxSize = maximumSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
        }

        private static string CleanDisplayName(string value, string fallback = "")
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var trimmed = value.Trim();
            if (Guid.TryParse(trimmed, out _)) return fallback;
            return Friendly(trimmed, fallback);
        }

        private static string Friendly(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var trimmed = value.Trim();
            if (Guid.TryParse(trimmed, out _)) return fallback;
            if (trimmed.IndexOf('_') < 0) return trimmed;

            var prefixes = new[]
            {
                "QUALITY_", "RARITY_", "EQ_", "EQUIPMENT_", "ITEM_", "REWARD_", "SLOT_"
            };
            for (var index = 0; index < prefixes.Length; index++)
            {
                if (!trimmed.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) continue;
                trimmed = trimmed.Substring(prefixes[index].Length);
                break;
            }
            trimmed = trimmed.Replace('_', ' ').Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) return fallback;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmed.ToLowerInvariant());
        }

        private static string FormatNumber(long value)
        {
            return Math.Max(0L, value).ToString("N0", CultureInfo.InvariantCulture);
        }

        private static string Compact(string value, int maximumCharacters)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var singleLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (singleLine.IndexOf("  ", StringComparison.Ordinal) >= 0)
                singleLine = singleLine.Replace("  ", " ");
            return singleLine.Length <= maximumCharacters
                ? singleLine
                : singleLine.Substring(0, Math.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private static void Anchor(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            if (rect == null) return;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private void DisposeVisualRoot()
        {
            if (_root == null) return;
            StopBreakthrough076();
            StopReveal();
            _root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_root.gameObject);
            _root = null;
            _resultsVeil076 = null;
            _resultsStageGroup076 = null;
            _breakthroughHeroStage076 = null;
            _breakthroughPortrait076 = null;
            _breakthroughInitials076 = null;
            _breakthroughProgressFill076 = null;
            _breakthroughProgressLabel076 = null;
            _skillProgressRail076 = null;
            _skillProgressFill076 = null;
            _skillProgressLabel076 = null;
            _bossVictoryArt076 = null;
            _lootArtFrame076 = null;
            _lootArt076 = null;
        }

        private void PlayReveal()
        {
            StopReveal();
            for (var index = 0; index < _revealStages.Length; index++)
            {
                var stage = _revealStages[index];
                if (stage == null) continue;
                stage.alpha = 0f;
                stage.interactable = false;
                stage.blocksRaycasts = false;
            }
            if (_continueButton != null) _continueButton.interactable = false;
            _revealRoutine = StartCoroutine(Reveal());
        }

        private void StopReveal()
        {
            if (_revealRoutine != null)
            {
                StopCoroutine(_revealRoutine);
                _revealRoutine = null;
            }
        }

        private IEnumerator Reveal()
        {
            for (var index = 0; index < _revealStages.Length; index++)
            {
                var stage = _revealStages[index];
                if (stage == null) continue;
                var elapsed = 0f;
                const float duration = 0.18f;
                while (elapsed < duration && stage != null)
                {
                    elapsed += Time.unscaledDeltaTime;
                    stage.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
                if (stage != null) stage.alpha = 1f;
                if (index < _revealStages.Length - 1) yield return new WaitForSecondsRealtime(0.07f);
            }

            if (_revealStages.Length > 0 && _revealStages[_revealStages.Length - 1] != null)
            {
                _revealStages[_revealStages.Length - 1].interactable = true;
                _revealStages[_revealStages.Length - 1].blocksRaycasts = true;
            }
            if (_continueButton != null)
            {
                _continueButton.interactable = true;
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(_continueButton.gameObject);
            }
            _revealRoutine = null;
        }

        private void OnDisable()
        {
            ResetBreakthroughVisual076();
            StopReveal();
        }

        private sealed class BattlePayoffPresentation076
        {
            private BattlePayoffPresentation076(
                string outcomeTitle,
                string storyRewardHeading,
                string storyReward,
                string nextObjective,
                string continueLabel,
                string lootContext,
                string lootPurpose,
                string lootHeadingOverride = "")
            {
                OutcomeTitle = outcomeTitle;
                StoryRewardHeading = storyRewardHeading;
                StoryReward = storyReward;
                NextObjective = nextObjective;
                ContinueLabel = continueLabel;
                LootContext = lootContext;
                LootPurpose = lootPurpose;
                LootHeadingOverride = lootHeadingOverride;
            }

            public string OutcomeTitle { get; }
            public string StoryRewardHeading { get; }
            public string StoryReward { get; }
            public string NextObjective { get; }
            public string ContinueLabel { get; }
            public string LootContext { get; }
            public string LootPurpose { get; }
            public string LootHeadingOverride { get; }

            public static BattlePayoffPresentation076 For(
                M2BattleView battle, bool victory, bool retreat, int towerFloor156B)
            {
                if (!victory)
                    return new BattlePayoffPresentation076(
                        retreat ? "THE UNION WITHDRAWS" : "THE LINE HAS BROKEN",
                        retreat ? "THE COMPANY ESCAPED" : "THE OBJECTIVE REMAINS",
                        retreat
                            ? "Your people live to attempt the order again."
                            : "Regroup, rebuild the Unions, and return prepared.",
                        "Return to the Guild Hall and prepare another attempt.",
                        retreat ? "REGROUP AT THE GUILD" : "RETURN TO THE GUILD",
                        "NOT RECOVERED",
                        "No equipment was added to the armory.");

                if (towerFloor156B > 0)
                    return new BattlePayoffPresentation076(
                        "TOWER FLOOR " + towerFloor156B.ToString(CultureInfo.InvariantCulture) + " CLEARED",
                        "TOWER VICTORY",
                        "Your Unions won the battle on this floor.",
                        "Continue to the Tower to choose your next action.",
                        "CLAIM REWARDS & CONTINUE",
                        "TOWER SPOILS",
                        "Earned by defeating this floor's enemy Unions.");

                var id = battle?.BattleId ?? string.Empty;
                var objective = battle?.Objective ?? string.Empty;
                if (IsSurveyorRescueEncounter079(battle))
                    return new BattlePayoffPresentation076(
                        "THE SURVEY BELL ANSWERS",
                        "ORRA'S CREW COMES HOME",
                        "The last false line is broken. Sella reaches the missing surveyors, and Orra's field book names the door below Skyhome.",
                        "Escort the crew home and deliver their testimony before the forged marks can be replaced.",
                        "BRING THE SURVEYORS HOME",
                        "WAYGLASS UNDERCROFT",
                        "Recovered while freeing Orra's crew at the unrecorded door.");
                if (IsFogStalkerEncounter079(battle))
                    return new BattlePayoffPresentation076(
                        "THE FALSE LINE IS BROKEN",
                        "WAYGLASS SIGNAL RESTORED",
                        "The Chaincaller's fog cordon is down. Sella and Orra's original brass line survive.",
                        "Regroup with Sella. Her testimony keeps the Guild on Orra's original brass line.",
                        "CONTINUE ON ORRA'S LINE",
                        "WAYGLASS UNDERCROFT",
                        "Recovered from the Chaincaller's false Wayglass cordon.",
                        "WAYGLASS RECOVERY  •  ECHO FILAMENT");
                if (StringComparer.Ordinal.Equals(id, "BATTLE_FIRST_HOUR072_HALL_BREACH"))
                    return new BattlePayoffPresentation076(
                        "THE HALL STILL STANDS",
                        "GUILD HALL SECURED",
                        "Your founding company held its first line together.",
                        "Meet Kiri in the Hall, then take the rescue order to Lantern Road.",
                        "RETURN TO THE GUILD HALL",
                        "HALL BREACH",
                        "Ready for one of the six founders.");

                if (Contains(id, "HALL_BREACH") || Contains(objective, "Guild Hall"))
                    return new BattlePayoffPresentation076(
                        "THE HALL STILL STANDS",
                        "GUILD HALL SECURED",
                        "Your founding company held its first line together.",
                        "Follow Lantern Road and find Zorin's missing patrol.",
                        "CONTINUE THE RESCUE",
                        "HALL BREACH",
                        "Ready for one of the six founders.");

                if (Contains(id, "LANTERN_ROAD_AMBUSH") || Contains(objective, "Lantern Road ambush"))
                    return new BattlePayoffPresentation076(
                        "THE ROAD IS OPEN",
                        "LANTERN ROAD REOPENED",
                        "The ambush is broken. Zorin's patrol trail continues beyond it.",
                        "Follow the fresh trail and reach the missing Lantern Patrol.",
                        "CONTINUE TO THE PATROL",
                        "ROAD AMBUSH",
                        "Reserve armament for the rescue party.");

                if (Contains(id, "GATE_EATER") || Contains(objective, "Gate-Eater"))
                    return new BattlePayoffPresentation076(
                        "THE GATE-EATER HAS FALLEN",
                        "SKYHOME IS SAFE",
                        "The rescued Lantern Patrol and its Wayglass can finally come home.",
                        "Return to Skyhome, reunite the Guild, and report what the Wayglass revealed.",
                        "CONTINUE TO THE RETURN ROAD",
                        "GATE-EATER",
                        "Defensive field gear for the restored twenty-member company.");

                return new BattlePayoffPresentation076(
                    "THE OBJECTIVE IS YOURS",
                    "CONTRACT OBJECTIVE CLEARED",
                    "The Guild completed the order and secured its reward.",
                    "Claim the reward and return to the Guild Hall for your next order.",
                    "CLAIM REWARDS & CONTINUE",
                    "CONTRACT SPOILS",
                    "Logged to the Guild armory.");
            }

            private static bool Contains(string value, string fragment) =>
                !string.IsNullOrWhiteSpace(value) &&
                value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
