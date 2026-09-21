using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    [DisallowMultipleComponent]
    public sealed partial class M1FlowPresenter : MonoBehaviour, FirstHour072.IFirstHourBattleExperience072
    {
        private IM1PresentationCoordinator _coordinator;
        private Canvas _canvas;
        private RectTransform _screenRoot;
        private Image _backgroundRoot;
        private Image _backgroundArtwork;
        private AspectRatioFitter _backgroundArtworkFitter;
        private Image _backgroundScrim;
        private Text _backgroundCaption;
        private M1WorldAmbientMotion _backgroundMotion;
        private AudioSource _studioAmbience076;
        private AudioClip _studioAmbienceClip076;
        private M1Screen _screen = M1Screen.MainMenu;
        private string _selectedApplicantId;
        private string _selectedRecruitId;
        private string _selectedSlotId;
        private string _selectedItemId;
        private int _selectedUnionIndex;
        private int _applicantPage;
        private int _inventoryMemberPage068;
        private string _guildmasterDraft = string.Empty;
        private string _selectedModeId = "Standard";
        private string _tutorialDepthId = "Full Tutorial";
        private float _textScale = 1f;
        private bool _highContrast;
        private bool _reducedMotion;
        private string _localStatus = string.Empty;
        private bool _localStatusPositive;
        private bool _building;
        private RectTransform _activePage;
        private RectTransform _activeContent;
        private ScrollRect _activeScroll;
        private Coroutine _pendingLayoutPass;
        private bool _battleResolving;
        private bool _battleAnimationSeen;
        private bool _skipBattleAnimation;
        private float _battleAnimationSpeed = 1f;
        private Text _battleEventBanner;
        private RectTransform _battleStagePlayer;
        private RectTransform _battleStageEnemy;
        private const string TitleSkyhomeMarketResource071 =
            "SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071";
        private const string StudioHallAmbienceResource076 =
            "SecondDimension/GuildCity017F/Audio/AMB_HALL_RUINED_017F";
        private const string StudioExpeditionAmbienceResource076 =
            "SecondDimension/GuildCity017F/Audio/AMB_CAMP_REST_017F";
        private static bool _titleSkyhomeMarketResolved071;
        private static Sprite _titleSkyhomeMarketSprite071;

        private void Start()
        {
            EnsureCanvas();
            if (_coordinator == null && M1PresentationCoordinatorRegistry.TryCreate(out var coordinator))
            {
                Attach(coordinator);
            }

            if (_coordinator == null) BuildCoordinatorMissing();
        }

        private void OnDestroy()
        {
            DetachTowerManual117();
            DetachTowerRestart130();
            ReleaseBattleEnemyArtLeases090();
            if (_coordinator != null) _coordinator.Changed -= HandleCoordinatorChanged;
            if (_battleExperience072 != null)
                _battleExperience072.BattleCompleted -= HandleBattleExperienceCompleted072;
            if (_pendingLayoutPass != null) StopCoroutine(_pendingLayoutPass);
            CloseVersion69Experiences069();
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        private void OnDisable()
        {
            DetachTowerManual117();
            DetachTowerRestart130();
            // A disabled view stops the deferred Options close. Rebuild on the
            // existing enable path rather than retain a hidden page/closing flag.
            if (_showAccessibilityOptions156 || _closingAccessibilityOptions156)
                _towerManualRefreshOnEnable117 = true;
            _showAccessibilityOptions156 = false;
            _closingAccessibilityOptions156 = false;
            _restoreOptionsFocus156 = false;
            _accessibilityFocus156 = null;
            // A disabled presentation cannot finish delayed card/dice/reward work.
            // Stop the iterators and release receipt schedule keys so the same
            // already-saved result can resume safely when the player re-enters.
            StopAllCoroutines();
            _pendingLayoutPass = null;
            ClearWorldGateReceiptApplyScheduling084();
            ClearCampaignCardScheduling129();
        }

        public void Initialize(IM1PresentationCoordinator coordinator)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            EnsureCanvas();
            Attach(coordinator);
        }

        private void Attach(IM1PresentationCoordinator coordinator)
        {
            var changedCoordinator = !ReferenceEquals(_coordinator, coordinator);
            if (_coordinator != null) _coordinator.Changed -= HandleCoordinatorChanged;
            if (changedCoordinator)
            {
                CloseLoopMenu164(false);
                _loopHistory164.Clear();
                _loopBookmarks164.Clear();
                DetachTowerManual117(false);
                DetachTowerRestart130(false);
                _showAccessibilityOptions156 = false;
                _closingAccessibilityOptions156 = false;
                _restoreOptionsFocus156 = false;
                _accessibilityFocus156 = null;
                // Operation feedback belongs to the coordinator instance that
                // produced it. A real save reload must never carry that toast
                // onto the next chapter's fixed-height title presentation.
                _localStatus = string.Empty;
                _localStatusPositive = false;
            }
            _coordinator = coordinator;
            _coordinator.Changed += HandleCoordinatorChanged;
            HydrateSavedAccessibility156();
            _selectedModeId = string.IsNullOrWhiteSpace(coordinator.State.SelectedModeId)
                ? "Standard"
                : coordinator.State.SelectedModeId;
            BuildCurrentScreen();
        }

        private void EnsureCanvas()
        {
            if (_canvas != null) return;
            RuntimeUi.EnsureEventSystem();
            _canvas = RuntimeUi.CreateCanvas("M1 Playable Proof Canvas");

            _backgroundRoot = RuntimeUi.AddPanel(_canvas.transform, "Background", RuntimeUi.Background);
            Stretch(_backgroundRoot.rectTransform);

            _backgroundArtwork = RuntimeUi.AddPanel(_backgroundRoot.transform, "M1 Backdrop Artwork", RuntimeUi.Background);
            Stretch(_backgroundArtwork.rectTransform);
            _backgroundArtwork.raycastTarget = false;
            _backgroundArtworkFitter = _backgroundArtwork.gameObject.AddComponent<AspectRatioFitter>();
            _backgroundArtworkFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            _backgroundArtworkFitter.aspectRatio = RuntimeUi.ReferenceResolution.x / RuntimeUi.ReferenceResolution.y;
            _backgroundMotion = _backgroundArtwork.gameObject.AddComponent<M1WorldAmbientMotion>();

            _backgroundScrim = RuntimeUi.AddPanel(_backgroundRoot.transform, "M1 Backdrop Scrim", new Color(0.01f, 0.025f, 0.06f, 0.10f));
            Stretch(_backgroundScrim.rectTransform);
            _backgroundScrim.raycastTarget = false;

            _backgroundCaption = RuntimeUi.AddText(
                _backgroundRoot.transform,
                "M1 Backdrop Fallback Caption",
                string.Empty,
                RuntimeUi.SmallBodyFontPixels,
                TextAnchor.LowerLeft,
                new Color(RuntimeUi.Accent.r, RuntimeUi.Accent.g, RuntimeUi.Accent.b, 0.55f),
                FontStyle.Bold);
            _backgroundCaption.rectTransform.anchorMin = new Vector2(0.035f, 0.035f);
            _backgroundCaption.rectTransform.anchorMax = new Vector2(0.60f, 0.14f);
            _backgroundCaption.rectTransform.offsetMin = Vector2.zero;
            _backgroundCaption.rectTransform.offsetMax = Vector2.zero;
            _backgroundCaption.raycastTarget = false;

            var safeArea = RuntimeUi.AddSafeArea(_backgroundRoot.transform);
            _screenRoot = RuntimeUi.AddStretchRect(safeArea, "M1 Screen Root");
            _screenRoot.offsetMin = new Vector2(96f, 42f);
            _screenRoot.offsetMax = new Vector2(-96f, -42f);

            var ambienceObject076 = new GameObject("Studio Ambience 076");
            ambienceObject076.transform.SetParent(transform, false);
            _studioAmbience076 = ambienceObject076.AddComponent<AudioSource>();
            _studioAmbience076.playOnAwake = false;
            _studioAmbience076.loop = true;
            _studioAmbience076.spatialBlend = 0f;
            _studioAmbience076.volume = 0.13f;
            _studioAmbience076.ignoreListenerPause = true;
        }

        private void HandleCoordinatorChanged()
        {
            HydrateSavedAccessibility156();
            if ((!isActiveAndEnabled || !gameObject.activeInHierarchy) &&
                ((_coordinator as Campaign022.ITowerManualAsyncTransition117)?.PendingTowerManualTransition117 != null ||
                 (_coordinator as Campaign022.ITowerClaimAsyncTransition120)?.PendingTowerClaim120 != null ||
                 (_coordinator as Campaign022.ITowerRestartAsyncTransition130)?.PendingTowerRestart130 != null ||
                 (_coordinator as IAutoEquipmentAsyncCoordinator123)?.PendingAutoEquipment123 != null))
            {
                _towerManualRefreshOnEnable117 = true;
                return;
            }
            // The inventory holds publication until its owner task settles.
            // Do not refresh an underlying hall/battle surface in that interval.
            if (_compactInventory069 != null && _compactInventory069.IsRunningForTests &&
                _compactInventory069.IsAutoEquipmentResolving123) return;
            if (RefreshFirstHourBattleExperience072()) return;
            if (_battleResolving) return;
            if (_suppressBoardAdventureCoordinatorRefresh084) return;
            if (RefreshActiveVersion69Experience069()) return;
            if (!_building) BuildCurrentScreen();
        }

        private void BuildCurrentScreen()
        {
            if (_coordinator == null) return;
            if (_pendingLayoutPass != null)
            {
                StopCoroutine(_pendingLayoutPass);
                _pendingLayoutPass = null;
            }

            _building = true;
            try
            {
                RefreshLoopNavigation164();
                if (_screen != M1Screen.Battle)
                    SetFirstHourBattleExperienceVisible072(false);
                ApplyBackdropForScreen();
                ApplyStudioAmbience076();
                switch (_screen)
                {
                    case M1Screen.MainMenu:
                        BuildMainMenu();
                        break;
                    case M1Screen.NewGuild:
                        BuildNewGuild();
                        break;
                    case M1Screen.ApplicantBoard:
                        BuildApplicantBoard();
                        break;
                    case M1Screen.RecruitDetail:
                        BuildRecruitDetail();
                        break;
                    case M1Screen.Equipment:
                        BuildEquipment();
                        break;
                    case M1Screen.UnionBuilder:
                        BuildUnionBuilder();
                        break;
                    case M1Screen.Complete:
                        BuildComplete();
                        break;
                    case M1Screen.GuildOperations:
                        BuildGuildOperations017D();
                        break;
                    case M1Screen.Battle:
                        BuildBattle();
                        break;
                    case M1Screen.BattleResults:
                        BuildBattleResults();
                        break;
                    case M1Screen.FirstHourOpening:
                        BuildGuildCharterPrologue074();
                        break;
                    case M1Screen.FirstHourGuildReady:
                        BuildFirstHourGuildReady071();
                        break;
                }

                ApplyTextScale();
                FinalizeActivePageLayout();
                RefreshLoopNavigation164();
                if (isActiveAndEnabled)
                {
                    _pendingLayoutPass = StartCoroutine(FinalizeActivePageLayoutNextFrame());
                }
            }
            finally
            {
                _building = false;
            }
        }

        private void ApplyStudioAmbience076()
        {
            if (_studioAmbience076 == null) return;
            if (_screen == M1Screen.Battle || _screen == M1Screen.BattleResults)
            {
                if (_studioAmbience076.isPlaying) _studioAmbience076.Stop();
                _studioAmbience076.clip = null;
                _studioAmbienceClip076 = null;
                return;
            }

            var resourcePath = _screen == M1Screen.GuildOperations &&
                               StringComparer.Ordinal.Equals(_guildCityTab017D, "EXPEDITION")
                ? StudioExpeditionAmbienceResource076
                : StudioHallAmbienceResource076;
            var clip = Resources.Load<AudioClip>(resourcePath);
            if (clip == null)
            {
                if (_studioAmbience076.isPlaying) _studioAmbience076.Stop();
                _studioAmbience076.clip = null;
                _studioAmbienceClip076 = null;
                return;
            }
            if (ReferenceEquals(_studioAmbienceClip076, clip) && _studioAmbience076.isPlaying)
                return;

            _studioAmbience076.Stop();
            _studioAmbienceClip076 = clip;
            _studioAmbience076.clip = clip;
            _studioAmbience076.Play();
        }

        private void SuspendStudioAmbienceForWorld076()
        {
            // Walkable hosts own their own ambience. Keeping the presenter's menu
            // loop alive underneath them doubles the mix and obscures field cues.
            // BuildCurrentScreen restores the correct studio track when the host
            // hands control back to the fixed presentation.
            if (_studioAmbience076 != null && _studioAmbience076.isPlaying)
                _studioAmbience076.Stop();
        }

        private void ApplyBackdropForScreen()
        {
            if (_backgroundArtwork == null) return;
            var cinematicBattle = _screen == M1Screen.Battle || _screen == M1Screen.BattleResults;
            SetCinematic3DWorldVisible(cinematicBattle);
            if (_backgroundRoot != null)
                _backgroundRoot.color = cinematicBattle ? Color.clear : RuntimeUi.Background;
            var role = M1VisualAssets.BackdropRole.SkyhomeHall;
            switch (_screen)
            {
                case M1Screen.MainMenu:
                case M1Screen.NewGuild:
                case M1Screen.FirstHourOpening:
                    role = M1VisualAssets.BackdropRole.SkyhomeTitle;
                    break;
                case M1Screen.ApplicantBoard:
                    role = M1VisualAssets.BackdropRole.ApplicantBoard;
                    break;
                case M1Screen.RecruitDetail:
                    role = M1VisualAssets.BackdropRole.RecruitDossier;
                    break;
                case M1Screen.Equipment:
                    role = M1VisualAssets.BackdropRole.QuartermasterArmory;
                    break;
                case M1Screen.UnionBuilder:
                    role = M1VisualAssets.BackdropRole.UnionStrategy;
                    break;
                case M1Screen.Complete:
                case M1Screen.GuildOperations:
                case M1Screen.FirstHourGuildReady:
                    role = M1VisualAssets.BackdropRole.GuildHallStage01;
                    break;
            }

            Sprite sprite = null;
            if (_screen == M1Screen.MainMenu)
                sprite = ResolveTitleSkyhomeMarket071();

            if (sprite != null || M1VisualAssets.TryResolveBackdrop(role, out sprite, out _))
            {
                _backgroundArtwork.sprite = sprite;
                _backgroundArtwork.color = Color.white;
                _backgroundArtwork.type = Image.Type.Simple;
                _backgroundArtwork.preserveAspect = false;
                _backgroundArtworkFitter.enabled = true;
                _backgroundArtworkFitter.aspectRatio = sprite.rect.width / sprite.rect.height;
                _backgroundCaption.gameObject.SetActive(false);
            }
            else
            {
                _backgroundArtworkFitter.enabled = false;
                Stretch(_backgroundArtwork.rectTransform);
                _backgroundArtwork.sprite = null;
                _backgroundArtwork.color = BackdropFallbackColor(role);
                _backgroundCaption.text = role == M1VisualAssets.BackdropRole.ApplicantBoard
                    ? "SKYHOME • APPLICANT INTERVIEWS"
                    : role == M1VisualAssets.BackdropRole.SkyhomeTitle
                        ? "SKYHOME • THE GATEWARD"
                        : "SKYHOME • GUILD ANNEX";
                _backgroundCaption.gameObject.SetActive(true);
            }

            if (_backgroundScrim != null)
            {
                _backgroundScrim.color = _highContrast
                    ? new Color(0f, 0f, 0f, 0.56f)
                    : new Color(0.01f, 0.025f, 0.06f, 0.10f);
            }
            if (_backgroundMotion != null)
            {
                _backgroundMotion.enabled = !cinematicBattle;
                _backgroundMotion.Configure(_reducedMotion || cinematicBattle, role.ToString());
            }
            if (cinematicBattle)
            {
                // Preserve the existing backdrop objects and resolved sprite for tests
                // and fallback, but let the dedicated 3D camera own the battle pixels.
                var artworkColor = _backgroundArtwork.color;
                artworkColor.a = 0f;
                _backgroundArtwork.color = artworkColor;
                if (_backgroundScrim != null) _backgroundScrim.color = Color.clear;
                if (_backgroundCaption != null) _backgroundCaption.gameObject.SetActive(false);
            }
        }

        private static Sprite ResolveTitleSkyhomeMarket071()
        {
            if (_titleSkyhomeMarketResolved071) return _titleSkyhomeMarketSprite071;
            _titleSkyhomeMarketResolved071 = true;

            try
            {
                var texture = Resources.Load<Texture2D>(TitleSkyhomeMarketResource071);
                if (texture == null) return null;

                _titleSkyhomeMarketSprite071 = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                _titleSkyhomeMarketSprite071.name = texture.name + "_RUNTIME_SPRITE_071";
                return _titleSkyhomeMarketSprite071;
            }
            catch (Exception)
            {
                // A missing or unreadable optional presentation asset must never
                // prevent the established M1VisualAssets title fallback.
                _titleSkyhomeMarketSprite071 = null;
                return null;
            }
        }

        private static Color BackdropFallbackColor(M1VisualAssets.BackdropRole role)
        {
            switch (role)
            {
                case M1VisualAssets.BackdropRole.ApplicantBoard:
                    return new Color(0.15f, 0.13f, 0.18f, 1f);
                case M1VisualAssets.BackdropRole.SkyhomeTitle:
                    return new Color(0.07f, 0.15f, 0.24f, 1f);
                default:
                    return new Color(0.08f, 0.12f, 0.18f, 1f);
            }
        }

        private void BuildMainMenu()
        {
            if (_showAccessibilityOptions156)
            {
                BuildAccessibilityOptions156();
                return;
            }
            RuntimeUi.ClearChildren(_screenRoot);
            _activeContent = null;
            _activeScroll = null;

            var city = (_coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D)
                ?.GuildCity017D;
            var firstComplete = city != null &&
                                IsStoryContractCompleted065(city, FirstStoryContractId065);
            var secondComplete = city != null &&
                                 IsStoryContractCompleted065(city, SecondStoryContractId065);
            var thirdComplete = city != null &&
                                IsStoryContractCompleted065(city, ThirdStoryContractId065);
            var chapterHeading076 = city == null
                ? "CHAPTER I  •  THE BELL BENEATH SKYHOME"
                : StoryChapterHeading065(firstComplete, secondComplete, thirdComplete);
            var savedHook076 = "YOUR PEOPLE ARE WAITING.\nTHE ROAD IS STILL OPEN.";
            var savedPromise076 =
                "Continue your saved adventure. Find the main card campaign in the Guild Hall.";
            if (city != null && firstComplete && !secondComplete)
            {
                savedHook076 = "THE PATROL CAME HOME.\nTHE WAYGLASS OPENED A ROAD WITHIN.";
                savedPromise076 = NeedsChapterTwoUnionRepair076(city)
                    ? "Repair your active Union plans before beginning the Wayglass descent beneath Skyhome."
                    : city.Expedition == null || ShouldShowChapterTwoOpening076(city)
                        ? "Return to the Hall table. Follow the survey crew's brass line beneath Skyhome."
                        : "Resume the saved route. Find the survey crew and the unrecorded door beneath Skyhome.";
            }
            else if (city != null && secondComplete && !thirdComplete)
            {
                savedHook076 = "THE DOOR INSIDE IS OPEN.\nSKYHOME'S RELIEF ROAD IS BREAKING.";
                savedPromise076 = "Return to your Guild and keep the medicine convoy moving through Chapter 3.";
            }
            else if (city != null && thirdComplete)
            {
                savedHook076 = "THREE OPENING CONTRACTS COMPLETE.\nTHE MAIN CARD CAMPAIGN CONTINUES.";
                savedPromise076 = "Continue your saved adventure. Choose Campaign in the Guild Hall for the main three-card story.";
            }

            var stage = RuntimeUi.AddPanel(
                _screenRoot,
                "Studio Title Stage 076",
                Color.clear);
            Stretch(stage.rectTransform);
            stage.raycastTarget = false;
            _activePage = stage.rectTransform;

            AddTitleCinematicScrim091(stage.transform);

            // The first screen must introduce a person, not only a destination.
            // Reuse Kiri's authored Skyhome key art as a tall cinematic vignette;
            // the story and command surfaces remain separate so neither copy nor
            // focus navigation is baked into the illustration.
            AddTitleCharacterKeyArt076(stage.transform);

            var lockup = RuntimeUi.AddPanel(
                stage.transform,
                "Studio Title Lockup 076",
                Color.clear);
            AnchorStudioRect076(
                lockup.rectTransform,
                new Vector2(0.025f, 0.790f),
                new Vector2(0.675f, 0.975f));
            lockup.raycastTarget = false;

            var title = RuntimeUi.AddText(
                lockup.transform,
                "Studio Game Title 076",
                "GUILD OF WORLDS",
                62,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorStudioRect076(
                title.rectTransform,
                new Vector2(0.035f, 0.32f),
                new Vector2(0.965f, 0.95f));
            ConfigureResponsiveText062(title, 36, 66);
            M1PremiumUi.ConfigureDisplayText(title);

            var subtitle = RuntimeUi.AddText(
                lockup.transform,
                "Studio Game Subtitle 076",
                "SECOND DIMENSION",
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorStudioRect076(
                subtitle.rectTransform,
                new Vector2(0.04f, 0.08f),
                new Vector2(0.965f, 0.32f));
            ConfigureResponsiveText062(subtitle, 17, 23);

            var story = RuntimeUi.AddPanel(
                stage.transform,
                "Studio Title Story Card 076",
                Color.clear);
            AnchorStudioRect076(
                story.rectTransform,
                new Vector2(0.025f, 0.180f),
                new Vector2(0.675f, 0.760f));
            story.raycastTarget = false;
            RuntimeUi.AddVerticalLayout(
                story.transform,
                new RectOffset(34, 34, 24, 24),
                8f,
                TextAnchor.UpperLeft);

            AddResponsiveText062(
                story.transform,
                "Studio Title Chapter 076",
                chapterHeading076,
                18,
                25,
                42f,
                RuntimeUi.Warning,
                FontStyle.Bold);
            var hook = AddResponsiveText062(
                story.transform,
                "Studio Title Hook 076",
                _coordinator.State.HasCampaign
                    ? savedHook076
                    : "A BELL RINGS BENEATH SKYHOME.\nTEN PEOPLE NEVER CAME HOME.",
                30,
                48,
                126f,
                RuntimeUi.Text,
                FontStyle.Bold);
            M1PremiumUi.ConfigureDisplayText(hook);
            if (_textScale > 1f)
            {
                // Fit the fixed story hook without painting over its neighbors.
                // Enlarged text remains preferred; its authored floor can fit.
                ConfigureAuthoredCompactText076(hook, 32, 48);
                hook.verticalOverflow = VerticalWrapMode.Truncate;
            }
            AddResponsiveText062(
                story.transform,
                "Studio Title Promise 076",
                _coordinator.State.HasCampaign
                    ? savedPromise076
                    : "Choose face-down quest cards. Lead whole Unions in battle. Bring the missing Lantern Patrol home.",
                20,
                30,
                78f,
                RuntimeUi.Text);

            if (!string.IsNullOrWhiteSpace(_coordinator.State.StatusMessage))
                AddStatus(story.transform, _coordinator.State.StatusMessage, false);
            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddStatus(story.transform, _localStatus, _localStatusPositive);

            var primaryPlay = RuntimeUi.AddButton(
                story.transform,
                "Title Primary Play Now 062",
                _coordinator.State.HasCampaign ? "CONTINUE GAME" : "START NEW GUILD",
                PlayNowFromTitle062,
                94f,
                RuntimeUi.Accent);
            primaryPlay.Select();

            // A save must never trap the player behind CONTINUE GAME. Keep the
            // existing campaign as the safe, focused default, while making a
            // deliberate restart visible without erasing anything up front.
            if (_coordinator.State.HasCampaign)
            {
                var titleOptionsRow156 = AddRow(story.transform, "Title Story Options 156", 12f, RuntimeUi.MinimumTouchPixels);
                var startNewGuild = RuntimeUi.AddButton(
                    titleOptionsRow156,
                    "Title Start New Guild 077",
                    "START NEW GUILD",
                    ConfirmStartNewGuildFromTitle077,
                    68f,
                    new Color(0.10f, 0.13f, 0.16f, 0.96f));
                ConfigureResponsiveText062(
                    startNewGuild.GetComponentInChildren<Text>(),
                    18,
                    24);
                AddAccessibilityOptionsEntry156(titleOptionsRow156);
            }

            var quiet = RuntimeUi.AddPanel(
                stage.transform,
                "Studio Title Quiet Utilities 076",
                new Color(0.006f, 0.014f, 0.025f, _highContrast ? 0.98f : 0.82f));
            AnchorStudioRect076(
                quiet.rectTransform,
                new Vector2(0.695f, 0.035f),
                new Vector2(0.975f, 0.270f));
            M1PremiumUi.StylePanel(quiet, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(
                quiet.transform,
                new RectOffset(16, 16, 12, 12),
                8f,
                TextAnchor.MiddleCenter);
            AddResponsiveText062(
                quiet.transform,
                "Studio Title Identity 076",
                _coordinator.State.HasCampaign && city != null
                    ? "CURRENT ORDER\n" +
                      (firstComplete && !secondComplete &&
                       IsStoryContractActive065(city, SecondStoryContractId065)
                          ? "CONTINUE CHAPTER 2"
                          : StoryNextActionLabel065(
                              city,
                              firstComplete,
                              secondComplete,
                              thirdComplete))
                    : _coordinator.State.HasCampaign
                        ? "YOUR GUILD\nSKYHOME"
                    : "GUILDMASTER'S ORDER\nBRING THEM HOME",
                18,
                28,
                78f,
                RuntimeUi.Accent,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            if (!_coordinator.State.HasCampaign)
            {
                var foundingStakes = AddResponsiveText062(
                    quiet.transform,
                    "Studio Title Pillars 076",
                    "SIX FOUNDERS WAIT IN THE HALL\nTEN LANTERNS ARE MISSING BELOW",
                    20,
                    26,
                    78f,
                    RuntimeUi.Positive,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
                // This is an authored two-line title-card lockup, not page body copy.
                // Preserve its readable 20-26 px range so the global 32 px body floor
                // cannot turn either complete story fact into an unintended third line.
                ConfigureAuthoredCompactText076(foundingStakes, 20, 26);
            }
            else if (city != null && firstComplete && !secondComplete)
            {
                AddResponsiveText062(
                    quiet.transform,
                    "Studio Title Chapter Two Proof 076",
                    "20 MEMBERS HOME\nWAYGLASS SECURED",
                    20,
                    26,
                    78f,
                    RuntimeUi.Positive,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
            }

            if (_coordinator.State.HasCampaign &&
                _coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D &&
                !(city != null && firstComplete && !secondComplete))
            {
                var practice = RuntimeUi.AddButton(
                    quiet.transform,
                    "Title Optional Practice 071",
                    _coordinator.State.OpeningUnionsLegal
                        ? "OPTIONAL PRACTICE"
                        : "PARTY SETUP",
                    BeginQuickBattle069,
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.10f, 0.13f, 0.16f, 0.94f));
                ConfigureResponsiveText062(practice.GetComponentInChildren<Text>(), 18, 24);
            }

            if (_coordinator.State.HasCampaign &&
                !(_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D) &&
                _coordinator.State.ResumeScreen == M1Screen.Battle)
            {
                RuntimeUi.AddButton(
                    quiet.transform,
                    "Resume Battle",
                    "RESUME BATTLE",
                    () => Navigate(M1Screen.Battle),
                    RuntimeUi.MinimumTouchPixels);
            }

            AddLegacyM2TitleCompatibility063(quiet.transform);

            if (!string.IsNullOrWhiteSpace(_coordinator.State.SaveRecoveryDiagnostic))
            {
                RuntimeUi.AddButton(
                    quiet.transform,
                    "Title Save Recovery Details 071",
                    "SAVE RECOVERY DETAILS",
                    ShowSaveRecoveryDetails071,
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.10f, 0.13f, 0.16f, 0.94f));
            }
        }

        private static Sprite _titleCinematicScrim091;

        private void AddTitleCinematicScrim091(Transform parent)
        {
            if (_titleCinematicScrim091 == null)
            {
                var texture = new Texture2D(128, 2, TextureFormat.RGBA32, false)
                {
                    name = "Title Cinematic Scrim Texture 091",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                var pixels = new Color[256];
                for (var x = 0; x < 128; x++)
                {
                    var fade = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.46f, 0.86f, x / 127f));
                    var color = new Color(0.014f, 0.025f, 0.040f,
                        Mathf.Lerp(0.94f, 0.05f, fade));
                    pixels[x] = pixels[x + 128] = color;
                }
                texture.SetPixels(pixels);
                texture.Apply(false, true);
                _titleCinematicScrim091 = Sprite.Create(texture,
                    new Rect(0, 0, 128, 2), Vector2.one * 0.5f);
            }
            var scrim = RuntimeUi.AddPanel(parent,
                "Studio Title Cinematic Scrim 091", Color.white);
            Stretch(scrim.rectTransform);
            // Artwork shading is full-bleed, not a safe-area UI panel. Overscan
            // the padded stage so its edge cannot form a dark rectangular frame.
            scrim.rectTransform.anchorMin = new Vector2(-0.12f, -0.12f);
            scrim.rectTransform.anchorMax = new Vector2(1.12f, 1.12f);
            scrim.sprite = _titleCinematicScrim091;
            scrim.type = Image.Type.Simple;
            scrim.raycastTarget = false;
        }

        private void AddTitleCharacterKeyArt076(Transform parent)
        {
            if (parent == null) return;

            var frame = RuntimeUi.AddPanel(
                parent,
                "Studio Title Kiri Hero Frame 076",
                new Color(0.025f, 0.055f, 0.075f, 0.96f));
            AnchorStudioRect076(
                frame.rectTransform,
                new Vector2(0.680f, 0.285f),
                new Vector2(0.975f, 0.955f));
            M1PremiumUi.StylePanel(frame, M1PremiumUi.Surface.WorldPaper);
            frame.raycastTarget = false;

            var viewport = RuntimeUi.AddPanel(
                frame.transform,
                "Studio Title Kiri Hero Viewport 076",
                Color.black);
            AnchorStudioRect076(
                viewport.rectTransform,
                new Vector2(0.018f, 0.014f),
                new Vector2(0.982f, 0.986f));
            viewport.raycastTarget = false;
            viewport.gameObject.AddComponent<RectMask2D>();

            var artwork = RuntimeUi.AddPanel(
                viewport.transform,
                "Studio Title Kiri Hero Artwork 076",
                Color.white);
            Stretch(artwork.rectTransform);
            artwork.raycastTarget = false;
            artwork.sprite = ResolveVisualSliceSprite062(ChapterTwoKiriKeyArtResource076);
            artwork.type = Image.Type.Simple;
            artwork.preserveAspect = false;
            artwork.rectTransform.pivot = new Vector2(0.5f, 0.86f);
            if (artwork.sprite == null) return;

            var fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = artwork.sprite.rect.width /
                                 Mathf.Max(1f, artwork.sprite.rect.height);
        }

        private static void AnchorStudioRect076(
            RectTransform rect,
            Vector2 minimum,
            Vector2 maximum)
        {
            if (rect == null) return;
            rect.anchorMin = minimum;
            rect.anchorMax = maximum;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void ShowSaveRecoveryDetails071()
        {
            var diagnostic = _coordinator.State.SaveRecoveryDiagnostic;
            if (string.IsNullOrWhiteSpace(diagnostic)) return;
            _localStatus = "Your previous save was left untouched because it could not be opened. " + diagnostic;
            _localStatusPositive = false;
            BuildCurrentScreen();
        }

        private void AddLegacyM2TitleCompatibility063(Transform parent)
        {
            // Release 062 intentionally gives the full Guild/City coordinator one
            // obvious PLAY NOW route.  The base M2 coordinator is also a supported
            // production presentation surface, however, and it has no Guild Hall
            // tabs to replace its established battle/result entry points.  Keep
            // those actions only for that smaller coordinator contract so the new
            // five-click story route remains unchanged for the shipping game.
            if (parent == null ||
                !_coordinator.State.HasCampaign ||
                !(_coordinator is IM2PresentationCoordinator) ||
                _coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D)
            {
                return;
            }

            if (_coordinator.State.ResumeScreen == M1Screen.Complete ||
                _coordinator.State.ResumeScreen == M1Screen.GuildOperations)
            {
                RuntimeUi.AddButton(
                    parent,
                    "Base M2 Battle Entry Compatibility 063",
                    BattleNowLabel060(),
                    OpenBattleNow060,
                    RuntimeUi.MinimumTouchPixels);
                RuntimeUi.AddButton(
                    parent,
                    "Base M2 Guild Review Compatibility 063",
                    "REVIEW RECRUITS & UNIONS",
                    () => Navigate(M1Screen.Complete),
                    RuntimeUi.MinimumTouchPixels);
                return;
            }

            if (_coordinator.State.ResumeScreen == M1Screen.BattleResults)
            {
                RuntimeUi.AddButton(
                    parent,
                    "Base M2 Result Entry Compatibility 063",
                    "VIEW LAST BATTLE RESULT",
                    () => Navigate(M1Screen.BattleResults),
                    RuntimeUi.MinimumTouchPixels);
            }
        }

        private void PlayNowFromTitle062()
        {
            if (!_coordinator.State.HasCampaign)
            {
                if (_coordinator is M1RuntimeCoordinator)
                    EnterDirectGuildCharter091();
                else
                    Navigate(M1Screen.NewGuild);
                return;
            }

            if (_coordinator.State.ResumeScreen == M1Screen.BattleResults)
            {
                Navigate(M1Screen.BattleResults);
                return;
            }
            if (_coordinator.State.ResumeScreen == M1Screen.Battle)
            {
                Navigate(M1Screen.Battle);
                return;
            }
            if (_coordinator is M1RuntimeCoordinator resumeOwner164 && resumeOwner164.CanNavigateLoops164 &&
                resumeOwner164.CurrentLoop164 != "TOWN")
            {
                // Reopen the persisted adventure owner without dealing cards,
                // starting a battle or disturbing its waiting receipt.
                OpenLoopDestination164(resumeOwner164.CurrentLoop164, null, false);
                return;
            }
            // Runtime save projection can correctly mark an illegal party as a
            // UnionBuilder resume even while Chapter 2 also owns an exact N00/N01
            // expedition junction. Honor the authored Chapter 2 repair promise
            // before the generic interrupted-founding recovery route below.
            if (_coordinator is M1RuntimeCoordinator &&
                _coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D chapterTwoCity078 &&
                chapterTwoCity078.GuildCity017D?.Expedition != null &&
                NeedsChapterTwoUnionRepair076(chapterTwoCity078.GuildCity017D))
            {
                OpenChapterTwoUnionRepair076();
                return;
            }
            if (_coordinator.State.ResumeScreen == M1Screen.Complete ||
                _coordinator.State.ResumeScreen == M1Screen.GuildOperations)
            {
                if (_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D guildCity)
                {
                    var state = guildCity.GuildCity017D;
                    _screen = M1Screen.GuildOperations;
                    if (_coordinator is M1RuntimeCoordinator)
                    {
                        if (state?.Expedition != null)
                        {
                            // A migrated Chapter 2 save can retain its exact N00/N01
                            // expedition beat while its old Union plans are no longer
                            // legal. The title promises repair in that state, so honor
                            // that promise before exposing either route command.
                            if (NeedsChapterTwoUnionRepair076(state))
                            {
                                OpenChapterTwoUnionRepair076();
                                return;
                            }
                            EnterExpeditionBoard074(guildCity);
                            return;
                        }
                        // A saved operation resumes on its exact fixed-board beat.
                        // The retired side-scroller remains closed; a pending first
                        // operation report resumes before the lived-in Hall.
                        // Smaller fake/compatibility coordinators retain the old direct
                        // router so their isolated presentation contracts stay stable.
                        _guildCityTab017D = NeedsFirstOperationConsequences077(state)
                            ? "CONSEQUENCES"
                            : "HALL";
                        _guildCityMoreOpen060 = false;
                        BuildCurrentScreen();
                    }
                    else
                    {
                        OpenStoryNextStep065(
                            state,
                            IsStoryContractCompleted065(state, FirstStoryContractId065),
                            IsStoryContractCompleted065(state, SecondStoryContractId065),
                            IsStoryContractCompleted065(state, ThirdStoryContractId065));
                    }
                }
                else if (_coordinator is IM2PresentationCoordinator)
                {
                    OpenBattleNow060();
                }
                else
                {
                    Navigate(M1Screen.Complete);
                }
                return;
            }

            if (_coordinator is M1RuntimeCoordinator &&
                (_coordinator.State.ResumeScreen == M1Screen.ApplicantBoard ||
                 _coordinator.State.ResumeScreen == M1Screen.Equipment ||
                 _coordinator.State.ResumeScreen == M1Screen.UnionBuilder))
            {
                _firstHourCharterRoute071 = true;
                _foundingBriefingPage076 =
                    _coordinator.State.OpeningUnionsLegal &&
                    (_coordinator.State.Recruits?.Count ?? 0) >= 10
                        ? 1
                        : 0;
                _screen = M1Screen.FirstHourGuildReady;
                BuildCurrentScreen();
                return;
            }

            Navigate(_coordinator.State.ResumeScreen);
        }

        private void ConfirmStartNewGuildFromTitle077()
        {
            ShowConfirmation(
                "START A NEW GUILD?",
                "Your current Guild and all saved progress will be replaced when you sign the new charter. Until then, your current save remains safe. This cannot be undone after the new charter is signed.",
                "START NEW GUILD",
                EnterFreshFirstHourFromSavedTitle077,
                "KEEP CURRENT GUILD",
                RuntimeUi.Error);
        }

        private void EnterFreshFirstHourFromSavedTitle077()
        {
            // Restart only presentation state here. The authoritative campaign
            // remains untouched until CreateGuild successfully persists the new
            // charter, so backing out of the prologue cannot lose progress.
            CloseWalkableSkyhomeArrival071();
            _skyhomeArrivalComplete071 = false;
            _firstHourCharterRoute071 = false;
            _foundingBriefingPage076 = 0;
            _guildmasterDraft = string.Empty;
            _selectedApplicantId = null;
            _selectedRecruitId = null;
            _selectedSlotId = null;
            _selectedItemId = null;
            _selectedUnionIndex = 0;
            _applicantPage = 0;
            _inventoryMemberPage068 = 0;
            _localStatus = string.Empty;
            _localStatusPositive = false;

            if (_coordinator is M1RuntimeCoordinator)
                EnterDirectGuildCharter091();
            else
                Navigate(M1Screen.NewGuild);
        }

        private static string MainMenuResumeLabel(M1Screen screen)
        {
            switch (screen)
            {
                case M1Screen.ApplicantBoard: return "NEXT: SIGN 6 RECRUITS";
                case M1Screen.Equipment: return "NEXT: REVIEW STARTER EQUIPMENT";
                case M1Screen.UnionBuilder: return "NEXT: BUILD 2 UNIONS";
                case M1Screen.GuildOperations: return "RUN THE GUILD";
                default: return "CONTINUE ADVENTURE";
            }
        }

        private void BuildNewGuild()
        {
            var body = CreatePage(
                "SIGN THE SKYHOME CHARTER",
                "CHAPTER 1  •  THE BELL BENEATH SKYHOME  •  ONE DECISION TO BEGIN",
                () => Navigate(M1Screen.MainMenu));

            var story = AddColumnPanel(body, "WHY SKYHOME NEEDS YOU", 1f, 205f);
            AddResponsiveText062(
                story,
                "Clean Charter Story 073",
                "A sealed bell is ringing beneath the Guild Hall, and a Lantern Road patrol has vanished with a Wayglass. Kiri Aetherheart needs one new Guild to bring them home.",
                25,
                36,
                102f,
                RuntimeUi.Text,
                FontStyle.Bold,
                TextAnchor.MiddleLeft);

            var identity = AddColumnPanel(body, "YOUR CHARTER", 1f, 820f);
            M1PremiumUi.AddSectionDivider(identity, "WHAT SHOULD THE GUILD CALL YOU?", "✦");
            var nameField = RuntimeUi.AddInputField(identity, "Guildmaster Name", "Enter a name", 28);
            nameField.text = _guildmasterDraft;
            nameField.onValueChanged.AddListener(value => _guildmasterDraft = value ?? string.Empty);

            M1PremiumUi.AddSectionDivider(identity, "CHOOSE THE PRESSURE", "◇");
            var modes = _coordinator.State.Modes ?? Array.Empty<M1ModeView>();
            var modeRow = AddRow(identity, "Clean Charter Mode Choices 073", 10f, 94f);
            foreach (var mode in modes)
            {
                var captured = mode;
                var selected = StringComparer.Ordinal.Equals(_selectedModeId, captured.Id);
                var button = RuntimeUi.AddButton(
                    modeRow,
                    "Mode " + captured.Id,
                    (selected ? "✓  " : string.Empty) +
                    (captured.DisplayName.StartsWith("Custom", StringComparison.OrdinalIgnoreCase)
                        ? "CUSTOM"
                        : captured.DisplayName.ToUpperInvariant()),
                    () =>
                    {
                        if (captured.IsAvailable)
                        {
                            _selectedModeId = captured.Id;
                            _localStatus = string.Empty;
                        }
                        BuildCurrentScreen();
                    },
                    88f,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                button.interactable = captured.IsAvailable;
            }

            var selectedMode = modes.FirstOrDefault(value =>
                value != null && StringComparer.Ordinal.Equals(value.Id, _selectedModeId));
            var modeSummary = RuntimeUi.AddPanel(
                identity,
                "Selected Pressure Summary 073",
                Color.white);
            RuntimeUi.SetLayout(modeSummary, preferredHeight: 112f);
            M1PremiumUi.StylePanel(modeSummary, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(
                modeSummary.transform,
                new RectOffset(28, 28, 12, 12),
                0f,
                TextAnchor.MiddleLeft);
            AddResponsiveText062(
                modeSummary.transform,
                "Selected Pressure Summary Text 073",
                (selectedMode?.DisplayName ?? "Standard").ToUpperInvariant() + "  •  " +
                CompactPlayerFacingLine073(
                    selectedMode?.Summary ?? "The authored baseline adventure.",
                    125),
                22,
                32,
                82f,
                RuntimeUi.Text,
                FontStyle.Bold);

            RuntimeUi.AddButton(
                identity,
                "Start Playable Adventure 069",
                "SIGN CHARTER & ENTER SKYHOME",
                BeginPlayableGuild069,
                116f,
                RuntimeUi.Accent);

            AddAccessibilityControls156(identity, false);

            if (!string.IsNullOrWhiteSpace(_localStatus)) AddStatus(identity, _localStatus, false);
        }


        // Saved preferences are read from the existing immutable profile. Charter
        // controls remain a local draft until the player signs a new charter.
        private bool _showAccessibilityOptions156;
        private bool _closingAccessibilityOptions156;
        private bool _restoreOptionsFocus156;
        private string _accessibilityFocus156;

        private void HydrateSavedAccessibility156()
        {
            var saved = (_coordinator as M1RuntimeCoordinator)?.SavedAccessibility156;
            if (saved == null) return;
            _textScale = saved.TextScalePercent / 100f;
            _highContrast = saved.HighContrast;
            _reducedMotion = saved.ReducedMotion;
        }

        private void AddAccessibilityOptionsEntry156(Transform parent)
        {
            var runtime = _coordinator as M1RuntimeCoordinator;
            if (runtime?.SavedAccessibility156 == null) return;
            var options = RuntimeUi.AddButton(parent, "Title Options 156", "OPTIONS", () =>
            {
                _showAccessibilityOptions156 = true;
                _accessibilityFocus156 = null;
                _localStatus = string.Empty;
                HydrateSavedAccessibility156();
                BuildCurrentScreen();
            }, 68f, RuntimeUi.ButtonNormal);
            ConfigureResponsiveText062(options.GetComponentInChildren<Text>(), 18, 24);
            if (_restoreOptionsFocus156)
            {
                _restoreOptionsFocus156 = false;
                options.Select();
            }
        }

        private void BuildAccessibilityOptions156()
        {
            HydrateSavedAccessibility156();
            var body = CreatePage("OPTIONS", "READABILITY & MOTION", CloseAccessibilityOptions156);
            AddResponsiveText062(body, "Saved Options Explanation 156",
                "Changes save to this Guild. Reduced motion limits supported background, card and battle effects; some animation remains.",
                28, 42, 150f, RuntimeUi.Text);
            AddAccessibilityControls156(body, true);
            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddStatus(body, _localStatus, _localStatusPositive);
            var close = RuntimeUi.AddButton(body, "Close Options 156", "RETURN TO TITLE",
                CloseAccessibilityOptions156, RuntimeUi.MinimumTouchPixels);
            Button selected = null;
            foreach (var button in _activePage.GetComponentsInChildren<Button>(false))
            {
                button.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel = CloseAccessibilityOptions156;
                if (button.interactable && button.name == (_accessibilityFocus156 ?? "Text Smaller"))
                    selected = button;
            }
            (selected ?? close).Select();
        }

        private void CloseAccessibilityOptions156()
        {
            if (_closingAccessibilityOptions156) return;
            _closingAccessibilityOptions156 = true;
            // Consume the current Cancel/Submit before focusing a title button.
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (_activePage != null) _activePage.gameObject.SetActive(false);
            StartCoroutine(CloseAccessibilityOptionsNextFrame156());
        }

        private IEnumerator CloseAccessibilityOptionsNextFrame156()
        {
            yield return null;
            _closingAccessibilityOptions156 = false;
            _showAccessibilityOptions156 = false;
            _restoreOptionsFocus156 = true;
            _localStatus = string.Empty;
            BuildCurrentScreen();
        }

        private void AddAccessibilityControls156(Transform parent, bool saveExisting)
        {
            var runtime = _coordinator as M1RuntimeCoordinator;
            var revision = saveExisting ? runtime?.AccessibilityRevision156 : null;
            var percent = Mathf.RoundToInt(_textScale * 100f);
            var contrast = _highContrast;
            var motion = _reducedMotion;
            Action<int, bool, bool, string> apply = (nextPercent, nextContrast, nextMotion, focus) =>
            {
                if (saveExisting)
                {
                    _accessibilityFocus156 = focus;
                    Execute(runtime == null ? M1CommandResult.Failure("Saved options are unavailable.") :
                        runtime.SetAccessibility156(revision, nextPercent, nextContrast, nextMotion));
                    return;
                }
                _textScale = nextPercent / 100f;
                _highContrast = nextContrast;
                _reducedMotion = nextMotion;
                BuildCurrentScreen();
            };
            M1PremiumUi.AddSectionDivider(parent, "READABILITY", "◈");
            var row = AddRow(parent, "Text Accessibility Controls", 10f);
            RuntimeUi.AddButton(row, "Text Smaller", "TEXT −", () =>
                apply(Mathf.Max(85, percent - 10), contrast, motion, "Text Smaller"), RuntimeUi.MinimumTouchPixels);
            RuntimeUi.AddButton(row, "Text Scale", $"TEXT {percent}%", null,
                RuntimeUi.MinimumTouchPixels, RuntimeUi.PanelRaised).interactable = false;
            RuntimeUi.AddButton(row, "Text Larger", "TEXT +", () =>
                apply(Mathf.Min(145, percent + 10), contrast, motion, "Text Larger"), RuntimeUi.MinimumTouchPixels);
            var visual = AddRow(parent, "Visual Accessibility Controls", 10f);
            RuntimeUi.AddButton(visual, "High Contrast", contrast ? "HIGH CONTRAST: ON" : "HIGH CONTRAST: OFF", () =>
                apply(percent, !contrast, motion, "High Contrast"), RuntimeUi.MinimumTouchPixels,
                contrast ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            RuntimeUi.AddButton(visual, "Reduced Motion", motion ? "REDUCED MOTION: ON" : "REDUCED MOTION: OFF", () =>
                apply(percent, contrast, !motion, "Reduced Motion"), RuntimeUi.MinimumTouchPixels,
                motion ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
        }

        private void BeginCreateGuild()
        {
            if (string.IsNullOrWhiteSpace(_guildmasterDraft))
            {
                _localStatus = "Enter a Guildmaster name before creating the guild.";
                _localStatusPositive = false;
                BuildCurrentScreen();
                return;
            }

            var intent = new M1NewGuildIntent
            {
                GuildmasterName = _guildmasterDraft.Trim(),
                ModeId = _selectedModeId,
                TutorialDepthId = _tutorialDepthId,
                TextScale = _textScale,
                HighContrast = _highContrast,
                ReducedMotion = _reducedMotion
            };

            var selectedMode = (_coordinator.State.Modes ?? Array.Empty<M1ModeView>())
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.Id, _selectedModeId));
            if (selectedMode != null && selectedMode.RequiresPermanentConsequenceConfirmation)
            {
                ShowConfirmation(
                    "IRON GUILD WARNING",
                    "Iron Guild increases enemy pressure, injury severity, and recovery demands. Signed members remain permanent.",
                    "CHOOSE IRON GUILD",
                    () => CreateGuild(intent));
                return;
            }

            CreateGuild(intent);
        }

        private void CreateGuild(M1NewGuildIntent intent)
        {
            var result = _coordinator.CreateGuild(intent);
            if (result.Succeeded)
            {
                _selectedApplicantId = null;
                _applicantPage = 0;
                _localStatus = result.Message;
                _localStatusPositive = true;
                _screen = M1Screen.ApplicantBoard;
                BuildCurrentScreen();
            }
            else
            {
                _localStatus = result.Message;
                _localStatusPositive = false;
                BuildCurrentScreen();
            }
        }

        private void BuildApplicantBoard()
        {
            var applicants = _coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>();
            if (applicants.Count > 0 && applicants.All(value => !StringComparer.Ordinal.Equals(value.RecruitId, _selectedApplicantId)))
            {
                _selectedApplicantId = applicants[0].RecruitId;
            }

            _applicantPage = Mathf.Clamp(_applicantPage, 0, Mathf.Max(0, (applicants.Count - 1) / 3));
            var signedCount = applicants.Count(value => value.IsSigned);
            var body = CreatePage(
                "MEET YOUR FOUNDING PARTY",
                $"STEP 2  •  SIGNED {signedCount}/6  •  MEET THREE PEOPLE AT A TIME",
                () => Navigate(M1Screen.MainMenu));

            var boardNotice = RuntimeUi.AddPanel(body, "Committed Applicant Board Notice", Color.white);
            RuntimeUi.SetLayout(boardNotice, preferredHeight: 92f);
            M1PremiumUi.StylePanel(boardNotice, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddHorizontalLayout(boardNotice.transform, new RectOffset(28, 28, 12, 12), 24f, TextAnchor.MiddleLeft);
            RuntimeUi.AddText(boardNotice.transform, "Committed Board Title", "YOUR SIX FOUNDERS", 38, TextAnchor.MiddleLeft, RuntimeUi.Positive, FontStyle.Bold);
            RuntimeUi.AddText(
                boardNotice.transform,
                "Committed Board Rule",
                "Select one portrait, read one short introduction, then sign them.",
                30,
                TextAnchor.MiddleRight,
                RuntimeUi.Text);

            var cardRow = AddRow(body, "Applicant Dossiers", 24f, 540f);
            foreach (var applicant in applicants.Skip(_applicantPage * 3).Take(3))
            {
                var captured = applicant;
                var label =
                    $"{captured.DisplayName}\n{captured.RaceAndWorld}  •  {captured.ObservedClass}";
                var button = RuntimeUi.AddButton(
                    cardRow,
                    "Applicant " + captured.RecruitId,
                    label,
                    () =>
                    {
                        _selectedApplicantId = captured.RecruitId;
                        BuildCurrentScreen();
                    },
                    520f,
                    Color.white);
                M1PremiumUi.StyleDossierCard(
                    button,
                    StringComparer.Ordinal.Equals(_selectedApplicantId, captured.RecruitId));
                AddPortraitToButton(button, captured, large: true);
                M1PremiumUi.AddClassCrest(button, captured.ClassSymbol, captured.ObservedClass);
                M1PremiumUi.AddLegalitySeal(
                    button.transform,
                    "Premium Applicant Commitment Seal",
                    captured.IsSigned,
                    captured.IsSigned ? "SIGNED" : "AVAILABLE");
                var labelText = button.transform.Find("Label")?.GetComponent<Text>();
                if (labelText != null) labelText.fontSize = 30;
                var seal = button.transform.Find("Premium Applicant Commitment Seal") as RectTransform;
                if (seal != null)
                {
                    seal.anchorMin = new Vector2(0.05f, 0.88f);
                    seal.anchorMax = new Vector2(0.38f, 0.97f);
                    seal.offsetMin = Vector2.zero;
                    seal.offsetMax = Vector2.zero;
                }
            }

            var pageRow = AddRow(body, "Applicant Page Controls", 18f);
            var previous = RuntimeUi.AddButton(pageRow, "Previous Applicants", "← PREVIOUS 3", () =>
            {
                _applicantPage = Mathf.Max(0, _applicantPage - 1);
                BuildCurrentScreen();
            }, RuntimeUi.MinimumTouchPixels);
            previous.interactable = _applicantPage > 0;
            var next = RuntimeUi.AddButton(pageRow, "Next Applicants", "NEXT 3 →", () =>
            {
                _applicantPage = Mathf.Min(Mathf.Max(0, (applicants.Count - 1) / 3), _applicantPage + 1);
                BuildCurrentScreen();
            }, RuntimeUi.MinimumTouchPixels);
            next.interactable = (_applicantPage + 1) * 3 < applicants.Count;

            var selected = FindApplicant(_selectedApplicantId);
            if (selected != null)
            {
                var drawer = AddRow(body, "Selected Applicant Drawer", 20f, 210f);
                var summary = RuntimeUi.AddPanel(drawer, "Selected Applicant Summary", Color.white);
                RuntimeUi.SetLayout(summary, preferredHeight: 196f, flexibleWidth: 1.6f);
                M1PremiumUi.StylePanel(summary, M1PremiumUi.Surface.WorldRibbon);
                RuntimeUi.AddVerticalLayout(summary.transform, new RectOffset(30, 30, 18, 18), 6f);
                RuntimeUi.AddText(summary.transform, "Selected Applicant Name", selected.DisplayName, 42, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
                RuntimeUi.AddText(
                    summary.transform,
                    "Selected Applicant Observations",
                    CompactPlayerFacingLine073(selected.ScoutObservations, 135) +
                    "\n" + CompactPlayerFacingLine073(selected.PersonalityClues, 120),
                    29,
                    TextAnchor.UpperLeft,
                    RuntimeUi.Text);

                var applicantActions = new GameObject("Applicant Actions", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
                applicantActions.SetParent(drawer, false);
                RuntimeUi.SetLayout(applicantActions, preferredHeight: 196f, flexibleWidth: 0.70f);
                RuntimeUi.AddVerticalLayout(applicantActions, new RectOffset(0, 0, 0, 0), 14f);
                RuntimeUi.AddButton(applicantActions, "Open Dossier", "MEET THIS APPLICANT", () => Navigate(M1Screen.RecruitDetail));
                var sign = RuntimeUi.AddButton(applicantActions, "Sign Recruit", selected.IsSigned ? "SIGNED" : "SIGN RECRUIT", () => Execute(_coordinator.SignRecruit(selected.RecruitId)));
                sign.interactable = !selected.IsSigned;
            }

            if (!string.IsNullOrWhiteSpace(_localStatus)) AddStatus(body, _localStatus, _localStatusPositive);
            var continueButton = RuntimeUi.AddButton(body, "Continue To Equipment", "CONTINUE TO EQUIPMENT", () => Navigate(M1Screen.Equipment));
            continueButton.interactable = _coordinator.State.AllSixSigned;
        }

        private void BuildRecruitDetail()
        {
            var applicant = FindApplicant(_selectedApplicantId);
            if (applicant == null)
            {
                Navigate(M1Screen.ApplicantBoard);
                return;
            }

            var body = CreatePage(
                "MEET " + applicant.DisplayName.ToUpperInvariant(),
                "WHAT WE KNOW SO FAR",
                () => Navigate(M1Screen.ApplicantBoard));
            var columns = AddRow(body, "Dossier Columns", 26f, 760f);
            var portrait = RuntimeUi.AddPanel(columns, "Portrait Frame " + applicant.RecruitId, RuntimeUi.Accent);
            RuntimeUi.SetLayout(portrait, preferredWidth: 820f, preferredHeight: 740f, flexibleWidth: 0f);
            PopulatePortraitFrame(
                portrait,
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.DisplayName,
                applicant.RaceAndWorld,
                $"{applicant.DisplayName}\n{applicant.RaceAndWorld}\n{applicant.ObservedClass}",
                roleIdentity: applicant.ObservedClass);
            M1PremiumUi.AddClassIdentityBadge(portrait.transform, applicant.ClassSymbol, applicant.ObservedClass);

            var dossier = RuntimeUi.AddPanel(columns, "Observed Dossier", RuntimeUi.Panel);
            RuntimeUi.SetLayout(dossier, preferredHeight: 740f, flexibleWidth: 1.25f);
            M1PremiumUi.StylePanel(dossier, M1PremiumUi.Surface.WorldPaper);
            RuntimeUi.AddVerticalLayout(dossier.transform, new RectOffset(46, 46, 34, 34), 14f);
            RuntimeUi.AddText(dossier.transform, "Scout Observations", "SCOUT OBSERVATIONS\n" + applicant.ScoutObservations, 42, TextAnchor.UpperLeft, RuntimeUi.Text);
            RuntimeUi.AddText(dossier.transform, "Leadership", "OBSERVED LEADERSHIP\n" + applicant.LeadershipBand, 38, TextAnchor.UpperLeft, RuntimeUi.Text);
            RuntimeUi.AddText(dossier.transform, "Personality", "PERSONALITY CLUES\n" + applicant.PersonalityClues, 38, TextAnchor.UpperLeft, RuntimeUi.Text);
            RuntimeUi.AddText(dossier.transform, "Ambition", "AMBITION\n" + (string.IsNullOrWhiteSpace(applicant.Ambition) ? "Not yet understood" : applicant.Ambition), 38, TextAnchor.UpperLeft, RuntimeUi.Text);
            RuntimeUi.AddText(dossier.transform, "Unknown Rule", "Unresolved information remains: Not yet understood", 34, TextAnchor.UpperLeft, RuntimeUi.MutedText, FontStyle.Italic);

            var interview = AddColumnPanel(columns, "INTERVIEW NOTES", 0.72f, 740f);
            AddMessagePanel(interview, "CURRENT GEAR", applicant.GearSummary, RuntimeUi.Accent);
            AddMessagePanel(interview, "SIGNING COST", applicant.SigningCost, RuntimeUi.Warning);
            AddMessagePanel(
                interview,
                "THE GUILD DOES NOT KNOW YET",
                "Exact potential, hidden traits, future Arts, and growth ceilings remain private until experience reveals them.",
                RuntimeUi.MutedText);
            var sign = RuntimeUi.AddButton(interview, "Sign From Dossier", applicant.IsSigned ? "SIGNED TO THE GUILD" : "SIGN RECRUIT", () => Execute(_coordinator.SignRecruit(applicant.RecruitId)));
            sign.interactable = !applicant.IsSigned;
        }

        private void BuildEquipment()
        {
            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            if (recruits.Count > 0 && recruits.All(value => !StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId)))
            {
                _selectedRecruitId = recruits[0].RecruitId;
            }

            var recruit = recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            if (recruit != null && recruit.Slots.Count > 0 && recruit.Slots.All(value => !StringComparer.Ordinal.Equals(value.SlotId, _selectedSlotId)))
            {
                _selectedSlotId = recruit.Slots[0].SlotId;
                _selectedItemId = recruit.Slots[0].EquippedItemId;
            }

            var openingReview = _coordinator.State.ResumeScreen == M1Screen.Equipment;
            var body = CreatePage(
                "ARMORY & INVENTORY",
                "Choose a member, choose an equipped slot, then choose an item.",
                BackFromInventory068);

            if (openingReview)
                AddStatus(body, "STARTER GEAR READY — changes are optional", true);

            const int membersPerPage068 = 6;
            var pageCount068 = Math.Max(1, (recruits.Count + membersPerPage068 - 1) / membersPerPage068);
            // Paging must remain independent of the selected member. Otherwise a
            // selection on page two immediately forces PREVIOUS back to page two.
            _inventoryMemberPage068 = Mathf.Clamp(_inventoryMemberPage068, 0, pageCount068 - 1);

            var memberHeading = AddRow(body, "Inventory Member Heading 068", 12f, 72f);
            RuntimeUi.AddText(
                memberHeading,
                "Inventory Choose Member 068",
                "CHOOSE A MEMBER",
                30,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            if (pageCount068 > 1)
            {
                var previous = RuntimeUi.AddButton(memberHeading, "Inventory Previous Members 068", "← MEMBERS", () =>
                {
                    _inventoryMemberPage068 = Math.Max(0, _inventoryMemberPage068 - 1);
                    BuildCurrentScreen();
                }, 68f);
                previous.interactable = _inventoryMemberPage068 > 0;
                var next = RuntimeUi.AddButton(memberHeading, "Inventory Next Members 068", "MORE MEMBERS →", () =>
                {
                    _inventoryMemberPage068 = Math.Min(pageCount068 - 1, _inventoryMemberPage068 + 1);
                    BuildCurrentScreen();
                }, 68f);
                next.interactable = _inventoryMemberPage068 < pageCount068 - 1;
            }

            var recruitRow = AddRow(body, "Inventory Member Cards 068", 12f, 180f);
            foreach (var value in recruits
                         .Skip(_inventoryMemberPage068 * membersPerPage068)
                         .Take(membersPerPage068))
            {
                var captured = value;
                var button = RuntimeUi.AddButton(
                    recruitRow,
                    "Inventory Member " + captured.RecruitId + " 068",
                    captured.DisplayName + "\n" + captured.ObservedClass + " • LV " + Math.Max(1, captured.Level),
                    () =>
                    {
                        _selectedRecruitId = captured.RecruitId;
                        _selectedSlotId = null;
                        _selectedItemId = null;
                        BuildCurrentScreen();
                    },
                    174f,
                    StringComparer.Ordinal.Equals(captured.RecruitId, _selectedRecruitId)
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal);
                button.GetComponentInChildren<Text>().fontSize = RuntimeUi.SmallBodyFontPixels;
                AddPortraitToButton(button, captured, large: false);
                M1PremiumUi.AddClassCrest(button, captured.ClassSymbol, captured.ObservedClass);
            }

            if (recruit == null)
            {
                AddStatus(body, "No signed recruits are available for equipment.", false);
                return;
            }

            var selectedSlot = recruit.Slots.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.SlotId, _selectedSlotId));
            var legalChoices068 = selectedSlot?.Choices?.Where(
                    value => value != null && value.IsLegal)
                .ToArray() ?? Array.Empty<M1EquipmentChoiceView>();
            var selectedChoice = legalChoices068.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ItemId, _selectedItemId));
            var inventoryWorkspaceHeight068 = 1300f +
                                              Math.Max(0, legalChoices068.Length - 1) * 144f;
            var inventoryPanelHeight068 = inventoryWorkspaceHeight068 - 30f;
            var equipmentColumns = AddRow(
                body,
                "Inventory Workspace 068",
                20f,
                inventoryWorkspaceHeight068);

            var characterPanel = AddColumnPanel(
                equipmentColumns,
                "SELECTED MEMBER",
                0.82f,
                inventoryPanelHeight068);
            AddRecruitShowcase(characterPanel, recruit, "READY IN THE ARMORY", 360f);
            AddStatus(
                characterPanel,
                recruit.IsLegal
                    ? "READY TO DEPLOY"
                    : "EQUIPMENT NEEDS ATTENTION",
                recruit.IsLegal);

            var slotsPanel = AddColumnPanel(
                equipmentColumns,
                "EQUIPPED",
                0.92f,
                inventoryPanelHeight068);
            foreach (var slot in recruit.Slots)
            {
                var captured = slot;
                var slotButton = RuntimeUi.AddButton(
                    slotsPanel,
                    "Slot " + captured.SlotId,
                    $"{captured.DisplayName}\n{captured.EquippedItemName}\n{(captured.IsLocked ? "LOCKED" : captured.IsLegal ? "EQUIPPED" : "EMPTY")}",
                    () =>
                    {
                        _selectedSlotId = captured.SlotId;
                        _selectedItemId = captured.EquippedItemId;
                        BuildCurrentScreen();
                    },
                    126f,
                    StringComparer.Ordinal.Equals(captured.SlotId, _selectedSlotId) ? RuntimeUi.Accent : RuntimeUi.PanelRaised);
                M1PremiumUi.AddEquipmentArtworkToButton(
                    slotButton,
                    captured.EquippedVisualId,
                    captured.EquippedRarityTierId,
                    captured.EquippedRarityDisplayName,
                    captured.VisualGlyph,
                    equipped: false,
                    locked: captured.IsLocked);
            }

            var choicesPanel = AddColumnPanel(
                equipmentColumns,
                "INVENTORY",
                1.28f,
                inventoryPanelHeight068);
            if (selectedSlot != null)
            {
                AddMessagePanel(
                    choicesPanel,
                    selectedSlot.DisplayName.ToUpperInvariant(),
                    "Choose a compatible item for this slot.",
                    RuntimeUi.Accent);
                foreach (var choice in legalChoices068)
                {
                    var captured = choice;
                    var itemButton = RuntimeUi.AddButton(
                        choicesPanel,
                        "Item " + captured.ItemId,
                        captured.DisplayName + "\n" + captured.RarityDisplayName +
                        (captured.IsEquipped ? " • EQUIPPED" : string.Empty),
                        () =>
                        {
                            _selectedItemId = captured.ItemId;
                            BuildCurrentScreen();
                        },
                        126f,
                        StringComparer.Ordinal.Equals(captured.ItemId, _selectedItemId) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                    M1PremiumUi.AddEquipmentArtworkToButton(
                        itemButton,
                        captured.EquipmentVisualId,
                        captured.RarityTierId,
                        captured.RarityDisplayName,
                        captured.VisualGlyph,
                        captured.IsEquipped,
                        locked: false);
                }
                if (legalChoices068.Length == 0)
                {
                    AddMessagePanel(
                        choicesPanel,
                        "QUARTERMASTER NOTE",
                        "No compatible item is available for this slot. Choose another slot or recruit.",
                        RuntimeUi.MutedText);
                }
            }

            if (selectedChoice != null)
            {
                var itemPreview = RuntimeUi.AddPanel(choicesPanel, "Selected Inventory Item Preview 068", RuntimeUi.PanelOverlay);
                RuntimeUi.SetLayout(itemPreview, preferredHeight: 230f);
                M1PremiumUi.StylePanel(itemPreview, M1PremiumUi.Surface.Parchment);
                M1PremiumUi.AddEquipmentPreview(
                    itemPreview,
                    selectedChoice.EquipmentVisualId,
                    selectedChoice.RarityTierId,
                    selectedChoice.RarityDisplayName,
                    selectedChoice.DisplayName,
                    selectedChoice.VisualGlyph,
                    selectedChoice.IsEquipped);
                AddMessagePanel(
                    choicesPanel,
                    "CURRENT → SELECTED",
                    (selectedSlot?.EquippedItemName ?? "EMPTY") + "  →  " + selectedChoice.DisplayName +
                    "\n" + (selectedChoice.DirectChange ?? "Compatible with this slot.") +
                    (string.IsNullOrWhiteSpace(selectedChoice.ForecastBehavior)
                        ? string.Empty
                        : "\nIN BATTLE — " + selectedChoice.ForecastBehavior),
                    RuntimeUi.Positive);
            }

            if (selectedSlot != null)
            {
                var actions = AddRow(choicesPanel, "Inventory Actions 068", 14f);
                var equip = RuntimeUi.AddButton(actions, "Equip Selected", "EQUIP", () => Execute(_coordinator.EquipItem(recruit.RecruitId, selectedSlot.SlotId, _selectedItemId)));
                equip.interactable = selectedChoice != null &&
                                     !selectedSlot.IsLocked &&
                                     !StringComparer.Ordinal.Equals(selectedSlot.EquippedItemId, selectedChoice.ItemId);
                var unequip = RuntimeUi.AddButton(actions, "Unequip Selected", "REMOVE", () => Execute(_coordinator.UnequipItem(recruit.RecruitId, selectedSlot.SlotId)));
                unequip.interactable = !string.IsNullOrWhiteSpace(selectedSlot.EquippedItemId) && !selectedSlot.IsLocked;
                if (selectedSlot.IsLocked)
                    RuntimeUi.AddButton(actions, "Unlock Selected", "UNLOCK", () => Execute(
                        _coordinator.SetEquipmentLock(recruit.RecruitId, selectedSlot.SlotId, false)));
            }

            if (!string.IsNullOrWhiteSpace(_localStatus)) AddStatus(body, _localStatus, _localStatusPositive);
            if (openingReview)
            {
                var continueButton = RuntimeUi.AddButton(
                    body,
                    "Continue To Union Builder",
                    "CONTINUE TO PARTY SETUP",
                    CompleteEquipmentReview,
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                continueButton.interactable = _coordinator.State.OpeningEquipmentLegal;
            }
            else
            {
                RuntimeUi.AddButton(
                    body,
                    "Inventory Done 068",
                    "DONE — BACK TO GUILD",
                    BackFromInventory068,
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
            }
        }

        private void BackFromInventory068()
        {
            if (_returnToWalkableHall069 && _coordinator is M1RuntimeCoordinator)
            {
                ReturnToWalkableHall069();
                return;
            }
            if (_coordinator.State.ResumeScreen == M1Screen.Equipment)
            {
                Navigate(M1Screen.ApplicantBoard);
                return;
            }
            Navigate(M1Screen.GuildOperations);
        }

        private void OpenInventoryForRecruit068(string recruitId)
        {
            _selectedRecruitId = recruitId;
            _selectedSlotId = null;
            _selectedItemId = null;
            if (_coordinator is M1RuntimeCoordinator &&
                _coordinator?.State?.ResumeScreen != M1Screen.Equipment)
            {
                OpenFocusedCompactInventory069(recruitId);
                return;
            }
            var recruits = _coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var recruitIndex = recruits
                .Select((value, index) => new { value, index })
                .Where(pair => pair.value != null &&
                               StringComparer.Ordinal.Equals(pair.value.RecruitId, recruitId))
                .Select(pair => pair.index)
                .DefaultIfEmpty(0)
                .First();
            _inventoryMemberPage068 = Math.Max(0, recruitIndex / 6);
            Navigate(M1Screen.Equipment);
        }

        private void CompleteEquipmentReview()
        {
            var result = _coordinator.CompleteEquipmentReview();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            if (result.Succeeded)
            {
                _screen = M1Screen.UnionBuilder;
            }
            BuildCurrentScreen();
        }

        private void BuildUnionBuilder()
        {
            BuildUnionPlanner074();
        }

        // Retained as a compatibility reference for old saves and test fixtures.
        // The player-facing route no longer opens this dashboard.
        private void BuildLegacyUnionBuilder073()
        {
            var unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            _selectedUnionIndex = Mathf.Clamp(_selectedUnionIndex, 0, Mathf.Max(0, unions.Count - 1));
            var union = unions.FirstOrDefault(value => value.Index == _selectedUnionIndex) ?? unions.FirstOrDefault();
            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var assignedRecruitIds = new HashSet<string>(
                unions.SelectMany(value => value.MemberRecruitIds),
                StringComparer.Ordinal);
            var availableRecruits = recruits
                .Where(value => !assignedRecruitIds.Contains(value.RecruitId))
                .ToArray();

            var body = CreatePage(
                "UNION BUILDER",
                "Click an available recruit to add them to the selected Union. Slot 1 leads automatically, assigned recruits leave the available list, and formations may repeat.",
                BackFromUnionBuilder068);

            var unionTabs = AddRow(body, "Union Tabs", 12f, 132f);
            foreach (var candidate in unions)
            {
                var captured = candidate;
                RuntimeUi.AddButton(
                    unionTabs,
                    "Union " + captured.Index,
                    captured.DisplayName.ToUpperInvariant() + "\n" + captured.MemberRecruitIds.Count + "/3  " +
                    (captured.MemberRecruitIds.Count == 0 ? "EMPTY" : captured.IsLegal ? "READY" : "NEEDS SETUP"),
                    () =>
                    {
                        _selectedUnionIndex = captured.Index;
                        BuildCurrentScreen();
                    },
                    RuntimeUi.MinimumTouchPixels,
                    captured.Index == _selectedUnionIndex ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            }

            var unionPlanControls = AddRow(body, "Union Plan Controls", 18f, RuntimeUi.MinimumTouchPixels);
            var addUnion = RuntimeUi.AddButton(
                unionPlanControls,
                "Add Union Plan",
                "ADD UNION PLAN",
                () => Execute(_coordinator.AddUnion()));
            addUnion.interactable = unions.Count < _coordinator.State.MaximumUnionPlanCount;
            var removeUnion = RuntimeUi.AddButton(
                unionPlanControls,
                "Remove Empty Union Plan",
                "REMOVE SELECTED EMPTY PLAN",
                () => Execute(_coordinator.RemoveUnion(union.Index)),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.Warning);
            removeUnion.interactable = unions.Count > 2 && union != null && union.MemberRecruitIds.Count == 0;
            AddMessagePanel(
                body,
                "UNION PLANS FOR THE ROAD",
                "You may prepare up to six Union plans. Every ready plan joins your party when an expedition begins.",
                RuntimeUi.Accent);

            if (union == null)
            {
                AddStatus(body, "At least two Union plans are required.", false);
                return;
            }

            M1PremiumUi.AddSectionDivider(body, "UNION COMMAND TABLE", "✦");
            M1PremiumUi.AddUnionStatReadout(
                body,
                union.MemberRecruitIds.Count,
                union.SharedAp,
                union.CohesionBasisPoints,
                union.CombinedCurrentHp,
                union.CombinedMaximumHp,
                union.CombinedMp,
                union.CombinedMaximumMp,
                union.CombinedAttack,
                union.CombinedMagicAttack,
                union.CombinedDefense,
                union.CombinedAgility,
                union.CombinedWill);
            var workspace = AddRow(body, "Union Command Workspace", 20f, 840f);

            var memberTray = AddColumnPanel(workspace, "AVAILABLE RECRUITS", 0.95f, 800f);
            if (availableRecruits.Length > 0)
            {
                AddRecruitShowcase(memberTray, availableRecruits[0], "NEXT AVAILABLE", 250f);
                if (union.MemberRecruitIds.Count >= 3)
                {
                    AddMessagePanel(
                        memberTray,
                        "SELECT ANOTHER UNION",
                        "This Union is full. Choose another Union tab before adding the remaining recruits.",
                        RuntimeUi.Warning);
                }
            }
            else
            {
                AddMessagePanel(
                    memberTray,
                    "ALL RECRUITS ASSIGNED",
                    "Every recruit is already visible in a Union slot. Tap an occupied slot to return that recruit here.",
                    RuntimeUi.Positive);
            }
            for (var memberRowIndex = 0; memberRowIndex < (availableRecruits.Length + 2) / 3; memberRowIndex++)
            {
                var memberRow = AddRow(memberTray, "Member Tray Row " + memberRowIndex, 10f, 210f);
                foreach (var recruit in availableRecruits.Skip(memberRowIndex * 3).Take(3))
                {
                    var captured = recruit;
                    var memberButton = RuntimeUi.AddButton(
                        memberRow,
                        "Available Recruit " + captured.RecruitId,
                        "ADD " + captured.DisplayName.ToUpperInvariant() + "\n" + captured.ClassSymbol + "  " + captured.ObservedClass +
                        "\nLV " + Math.Max(1, captured.Level) + "  |  " + RecruitXpProgress(captured),
                        () => Execute(_coordinator.AssignRecruitToUnion(
                            captured.RecruitId,
                            union.Index,
                            union.MemberRecruitIds.Count)),
                        202f,
                        RuntimeUi.ButtonNormal);
                    memberButton.interactable = union.MemberRecruitIds.Count < 3;
                    AddPortraitToButton(memberButton, captured, large: false);
                    M1PremiumUi.AddClassCrest(memberButton, captured.ClassSymbol, captured.ObservedClass);
                }
            }

            var formationBoard = AddColumnPanel(workspace, "FORMATION BOARD", 1.42f, 800f);
            AddMessagePanel(
                formationBoard,
                "FIRST SLOT LEADS AUTOMATICALLY",
                "Recruits fill the next open slot in order. Tap an occupied slot to remove that recruit and make them available again.",
                RuntimeUi.Accent);
            var slotRow = RuntimeUi.AddPanel(formationBoard, "Formation Slots", Color.white);
            RuntimeUi.SetLayout(slotRow, preferredHeight: 430f);
            M1PremiumUi.StylePanel(slotRow, M1PremiumUi.Surface.WorldPaper);
            for (var slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                var memberId = slotIndex < union.MemberRecruitIds.Count ? union.MemberRecruitIds[slotIndex] : null;
                var member = recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, memberId));
                var memberName = member?.DisplayName ?? "EMPTY SLOT";
                var slotButton = RuntimeUi.AddButton(
                    slotRow.transform,
                    "Union Slot " + slotIndex,
                    $"SLOT {slotIndex + 1}" + (slotIndex == 0 ? " • LEADER" : string.Empty) + $"\n{memberName}" +
                    (member == null ? string.Empty : "\n" + member.ClassSymbol + "  " + member.ObservedClass +
                     "  |  LV " + Math.Max(1, member.Level) + "\nTAP TO REMOVE"),
                    member == null ? (Action)null : () => Execute(_coordinator.UnassignRecruitFromUnion(memberId)),
                    360f,
                    slotIndex == 0 && member != null ? RuntimeUi.Accent : RuntimeUi.PanelRaised);
                slotButton.GetComponent<LayoutElement>().ignoreLayout = true;
                PositionFormationSocket(slotButton.GetComponent<RectTransform>(), union.FormationId, slotIndex);
                slotButton.interactable = member != null;
                if (member != null) AddPortraitToButton(slotButton, member, large: false);
                if (member != null) M1PremiumUi.AddClassCrest(slotButton, member.ClassSymbol, member.ObservedClass);
                M1PremiumUi.AddFormationSocketBadge(
                    slotButton,
                    slotIndex,
                    slotIndex == 0 && member != null,
                    union.IsLegal);
            }

            var doctrinePanel = AddColumnPanel(workspace, "UNION READOUT", 0.98f, 800f);
            var unionLeader = union.MemberRecruitIds.Count == 0
                ? null
                : recruits.FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, union.MemberRecruitIds[0]));
            if (unionLeader != null)
            {
                AddRecruitShowcase(doctrinePanel, unionLeader, "UNION LEADER", 220f);
            }
            var currentFormation = FindChoice(_coordinator.State.Formations, union.FormationId);
            var currentDoctrine = FindChoice(_coordinator.State.Doctrines, union.DoctrineId);
            M1PremiumUi.AddFormationEffectReadout(
                doctrinePanel,
                currentFormation?.DisplayName,
                currentFormation?.Summary,
                union.BaseCohesionBasisPoints,
                union.FormationCohesionRuleBasisPoints,
                union.CohesionBasisPoints);
            AddMessagePanel(
                doctrinePanel,
                "LIVE COMMAND PREVIEW",
                "Leader: " + (union.MemberRecruitIds.Count == 0
                    ? "Add the first recruit"
                    : (unionLeader?.DisplayName ?? "Slot 1")) +
                "    Doctrine: " + (currentDoctrine?.DisplayName ?? "Not chosen") + "\n" +
                $"Projected Shared AP: {union.SharedAp}    Projected Cohesion: {union.CohesionBasisPoints / 100f:0.#}%\n" +
                $"Readiness: {union.LegalitySummary}",
                union.IsLegal ? RuntimeUi.Positive : RuntimeUi.Warning);

            var choiceWorkspace = AddRow(body, "Union Configuration Choices", 20f, 850f);
            var formationChoices = AddColumnPanel(choiceWorkspace, "CHOOSE FORMATION", 1.0f, 810f);
            AddMessagePanel(
                formationChoices,
                "FORMATIONS ARE REUSABLE",
                "Shield Wall or any other formation may be selected for more than one Union.",
                RuntimeUi.Positive);
            AddChoiceGrid(
                formationChoices,
                "Formation Choice",
                _coordinator.State.Formations,
                union.FormationId,
                id => _coordinator.SetFormation(union.Index, id));

            var doctrineChoices = AddColumnPanel(choiceWorkspace, "CHOOSE DOCTRINE", 1.08f, 810f);
            AddMessagePanel(
                doctrineChoices,
                "UNION BEHAVIOR",
                "Doctrine changes which complete Forecasts the System favors; it never gives individual-Art buttons.",
                RuntimeUi.Accent);
            AddChoiceGrid(
                doctrineChoices,
                "Doctrine Choice",
                _coordinator.State.Doctrines,
                union.DoctrineId,
                id => _coordinator.SetDoctrine(union.Index, id));

            if (!string.IsNullOrWhiteSpace(_localStatus)) AddStatus(body, _localStatus, _localStatusPositive);
            AddMessagePanel(
                body,
                _coordinator.State.OpeningUnionsLegal ? "ROSTER PLAN READY" : "FINISH THE ROSTER PLAN",
                "Assign all six recruits exactly once across at least two used plans. Each used plan needs one to three members, with slot 1 automatically leading, plus a formation and doctrine. " +
                _coordinator.State.UsedUnionCount + " used plan" + (_coordinator.State.UsedUnionCount == 1 ? string.Empty : "s") + " currently.",
                _coordinator.State.OpeningUnionsLegal ? RuntimeUi.Positive : RuntimeUi.Warning);
            var save = RuntimeUi.AddButton(body, "Save Union Plans", "SAVE UNION PLANS & CONTINUE", SaveUnions);
            save.interactable = _coordinator.State.OpeningUnionsLegal;
        }

        private void BuildCleanUnionBuilder073()
        {
            var unions = _coordinator.State.Unions ?? Array.Empty<M1UnionView>();
            _selectedUnionIndex = Mathf.Clamp(
                _selectedUnionIndex,
                0,
                Mathf.Max(0, unions.Count - 1));
            var union = unions.FirstOrDefault(value => value.Index == _selectedUnionIndex) ??
                        unions.FirstOrDefault();
            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var assignedIds = new HashSet<string>(
                unions.SelectMany(value => value.MemberRecruitIds ?? Array.Empty<string>()),
                StringComparer.Ordinal);
            var available = recruits
                .Where(value => value != null && !assignedIds.Contains(value.RecruitId))
                .ToArray();
            var assignedCount = recruits.Count - available.Length;

            var body = CreatePage(
                "FOUNDING UNIONS",
                "STEP 3  •  PUT EACH FOUNDER IN A TEAM  •  CHOOSE ONE SIMPLE PLAN",
                BackFromUnionBuilder068);

            var progressRow = AddRow(body, "Clean Union Progress 073", 16f, 112f);
            var progress = RuntimeUi.AddPanel(progressRow, "Union Assignment Progress 073", Color.white);
            RuntimeUi.SetLayout(progress, preferredHeight: 104f, flexibleWidth: 1f);
            M1PremiumUi.StylePanel(progress, M1PremiumUi.Surface.WorldRibbon);
            var progressText = RuntimeUi.AddText(
                progress.transform,
                "Union Assignment Progress Text 073",
                "FOUNDERS  " + assignedCount + " / " + recruits.Count +
                "   •   READY TEAMS  " +
                unions.Count(value => value.MemberRecruitIds.Count > 0 && value.IsLegal) +
                "   •   MINIMUM  2",
                30,
                TextAnchor.MiddleCenter,
                assignedCount == recruits.Count ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            progressText.GetComponent<LayoutElement>().ignoreLayout = true;
            Stretch(progressText.rectTransform);
            progressText.rectTransform.offsetMin = new Vector2(22f, 10f);
            progressText.rectTransform.offsetMax = new Vector2(-22f, -10f);
            var xp = RuntimeUi.AddPanel(progressRow, "Union Spendable XP 073", Color.white);
            RuntimeUi.SetLayout(xp, preferredHeight: 104f, flexibleWidth: 0.86f);
            M1PremiumUi.StylePanel(xp, M1PremiumUi.Surface.WorldGlass);
            var xpText = RuntimeUi.AddText(
                xp.transform,
                "Union Spendable XP Text 073",
                "GUILD XP  " + FormatProgressionNumber(_coordinator.State.GuildXpIntoCurrentLevel) +
                " / " + FormatProgressionNumber(_coordinator.State.GuildXpRequiredForNextLevel) +
                "   •   XP TO SPEND  " + FormatProgressionNumber(_coordinator.State.TreasuryXp),
                28,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);
            xpText.GetComponent<LayoutElement>().ignoreLayout = true;
            Stretch(xpText.rectTransform);
            xpText.rectTransform.offsetMin = new Vector2(22f, 10f);
            xpText.rectTransform.offsetMax = new Vector2(-22f, -10f);

            if (union == null)
            {
                AddStatus(body, "Two founding Unions are required.", false);
                RuntimeUi.AddButton(
                    body,
                    "Add Union Plan",
                    "CREATE FIRST UNION",
                    () => Execute(_coordinator.AddUnion()),
                    RuntimeUi.PrimaryTouchPixels,
                    RuntimeUi.Accent);
                return;
            }

            var tabs = AddRow(body, "Union Tabs", 12f, 126f);
            foreach (var candidate in unions)
            {
                var captured = candidate;
                RuntimeUi.AddButton(
                    tabs,
                    "Union " + captured.Index,
                    (captured.Index == _selectedUnionIndex ? "✓  " : string.Empty) +
                    captured.DisplayName.ToUpperInvariant() + "\n" +
                    captured.MemberRecruitIds.Count + "/3  •  " +
                    (captured.IsLegal ? "READY" : "NEEDS YOU"),
                    () =>
                    {
                        _selectedUnionIndex = captured.Index;
                        BuildCurrentScreen();
                    },
                    112f,
                    captured.Index == _selectedUnionIndex
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal);
            }

            var workspace = AddRow(body, "Clean Union Workspace 073", 20f, 610f);
            var selected = AddColumnPanel(workspace, "SELECTED UNION", 1.18f, 585f);
            AddMessagePanel(
                selected,
                union.DisplayName.ToUpperInvariant(),
                "Slot 1 leads automatically. Select an occupied founder to return them to the available list.",
                union.IsLegal ? RuntimeUi.Positive : RuntimeUi.Accent);
            var slots = AddRow(selected, "Formation Slots", 12f, 330f);
            for (var slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                var memberId = slotIndex < union.MemberRecruitIds.Count
                    ? union.MemberRecruitIds[slotIndex]
                    : null;
                var member = recruits.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.RecruitId, memberId));
                var capturedMemberId = memberId;
                var slot = RuntimeUi.AddButton(
                    slots,
                    "Union Slot " + slotIndex,
                    "SLOT " + (slotIndex + 1) + (slotIndex == 0 ? " • LEADER" : string.Empty) +
                    "\n" + (member?.DisplayName ?? "EMPTY") +
                    (member == null ? "\nADD A FOUNDER" : "\n" + member.ObservedClass + "\nSELECT TO REMOVE"),
                    member == null
                        ? (Action)null
                        : () => Execute(_coordinator.UnassignRecruitFromUnion(capturedMemberId)),
                    318f,
                    slotIndex == 0 && member != null
                        ? RuntimeUi.Accent
                        : RuntimeUi.PanelRaised);
                slot.interactable = member != null;
                if (member != null)
                {
                    AddPortraitToButton(slot, member, large: false);
                    M1PremiumUi.AddClassCrest(slot, member.ClassSymbol, member.ObservedClass);
                }
                M1PremiumUi.AddFormationSocketBadge(
                    slot,
                    slotIndex,
                    slotIndex == 0 && member != null,
                    union.IsLegal);
            }

            var availablePanel = AddColumnPanel(workspace, "AVAILABLE FOUNDERS", 0.92f, 585f);
            AddMessagePanel(
                availablePanel,
                available.Length == 0
                    ? "EVERYONE HAS A TEAM"
                    : union.MemberRecruitIds.Count >= 3
                        ? "SELECT ANOTHER UNION"
                        : "SELECT A FOUNDER",
                union.MemberRecruitIds.Count >= 3
                    ? "This Union is full. Select another Union above or remove one member."
                    : "Selecting a founder adds them to the next empty slot.",
                available.Length == 0 ? RuntimeUi.Positive : RuntimeUi.Accent);
            for (var rowIndex = 0; rowIndex < (available.Length + 2) / 3; rowIndex++)
            {
                var row = AddRow(availablePanel, "Member Tray Row " + rowIndex, 10f, 220f);
                foreach (var recruit in available.Skip(rowIndex * 3).Take(3))
                {
                    var captured = recruit;
                    var add = RuntimeUi.AddButton(
                        row,
                        "Available Recruit " + captured.RecruitId,
                        "ADD " + captured.DisplayName.ToUpperInvariant() + "\n" +
                        captured.ClassSymbol + "  " + captured.ObservedClass +
                        "\nLV " + Math.Max(1, captured.Level) + "  |  " + RecruitXpProgress(captured),
                        () => Execute(_coordinator.AssignRecruitToUnion(
                            captured.RecruitId,
                            union.Index,
                            union.MemberRecruitIds.Count)),
                        210f,
                        RuntimeUi.ButtonNormal);
                    add.interactable = union.MemberRecruitIds.Count < 3;
                    AddPortraitToButton(add, captured, large: false);
                    M1PremiumUi.AddClassCrest(add, captured.ClassSymbol, captured.ObservedClass);
                }
            }

            var choices = AddRow(body, "Starter Union Choices 073", 20f, 760f);
            var formations = AddColumnPanel(choices, "STARTER FORMATION", 1f, 735f);
            AddMessagePanel(
                formations,
                "CHOOSE ONE OF THREE",
                "More formations unlock through contracts, Guild levels, and Creator Code discoveries.",
                RuntimeUi.Positive);
            foreach (var formation in (_coordinator.State.Formations ?? Array.Empty<M1ChoiceView>())
                         .Where(value => value != null)
                         .Take(3))
            {
                var captured = formation;
                var active = StringComparer.Ordinal.Equals(captured.Id, union.FormationId);
                RuntimeUi.AddButton(
                    formations,
                    "Formation Choice " + captured.Id,
                    (active ? "✓  " : string.Empty) + captured.DisplayName.ToUpperInvariant() +
                    "\n" + StarterFormationDescription073(captured.Id),
                    () => Execute(_coordinator.SetFormation(union.Index, captured.Id)),
                    102f,
                    active ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            }

            var doctrines = AddColumnPanel(choices, "HOW THIS UNION THINKS", 1f, 735f);
            AddMessagePanel(
                doctrines,
                "CHOOSE A FORECAST STYLE",
                "Guides which complete battle plans appear. You still choose the final forecast.",
                RuntimeUi.Accent);
            foreach (var doctrine in (_coordinator.State.Doctrines ?? Array.Empty<M1ChoiceView>())
                         .Where(value => value != null)
                         .Take(3))
            {
                var captured = doctrine;
                var active = StringComparer.Ordinal.Equals(captured.Id, union.DoctrineId);
                RuntimeUi.AddButton(
                    doctrines,
                    "Doctrine Choice " + captured.Id,
                    (active ? "✓  " : string.Empty) + captured.DisplayName.ToUpperInvariant() +
                    "\n" + StarterDoctrineDescription073(captured.Id),
                    () => Execute(_coordinator.SetDoctrine(union.Index, captured.Id)),
                    102f,
                    active ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            }

            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddStatus(body, _localStatus, _localStatusPositive);
            AddMessagePanel(
                body,
                _coordinator.State.OpeningUnionsLegal ? "READY FOR THE ROAD" : "ONE CLEAR GOAL",
                _coordinator.State.OpeningUnionsLegal
                    ? "Every founder has a team. Deeper formation details appear only when they matter in battle."
                    : "Put all six founders into at least two teams. Slot 1 leads; choose one formation and one forecast style for each used Union.",
                _coordinator.State.OpeningUnionsLegal ? RuntimeUi.Positive : RuntimeUi.Warning);
            var save = RuntimeUi.AddButton(
                body,
                "Save Union Plans",
                "SAVE UNIONS & CONTINUE",
                SaveUnions,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            save.interactable = _coordinator.State.OpeningUnionsLegal;

            var optional = AddRow(body, "Optional Union Plan Controls 073", 18f, RuntimeUi.MinimumTouchPixels);
            var addUnion = RuntimeUi.AddButton(
                optional,
                "Add Union Plan",
                "ADD UNION PLAN",
                () => Execute(_coordinator.AddUnion()));
            addUnion.interactable = unions.Count < _coordinator.State.MaximumUnionPlanCount;
            var removeUnion = RuntimeUi.AddButton(
                optional,
                "Remove Empty Union Plan",
                "REMOVE SELECTED EMPTY UNION",
                () => Execute(_coordinator.RemoveUnion(union.Index)),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.Warning);
            removeUnion.interactable = unions.Count > 2 && union.MemberRecruitIds.Count == 0;
        }

        private static string StarterFormationDescription073(string formationId)
        {
            switch (formationId)
            {
                case "FORMATION_SHIELD_WALL": return "HOLD  •  safer when the enemy presses";
                case "FORMATION_WEDGE": return "BREAK  •  concentrates force on one opening";
                case "FORMATION_SKIRMISH_LINE": return "MOVE  •  flexible against changing threats";
                default: return "A specialized formation earned by this Guild.";
            }
        }

        private static string StarterDoctrineDescription073(string doctrineId)
        {
            switch (doctrineId)
            {
                case "DOCTRINE_BALANCED": return "Offers a mix of attack, guard, and recovery plans";
                case "DOCTRINE_AGGRESSIVE": return "Favors pressure and breaking enemy formations";
                case "DOCTRINE_GUARDIAN": return "Favors guarding, rescue, and keeping members standing";
                default: return "A specialized forecast style earned by this Guild.";
            }
        }

        private void AddChoiceGrid(
            Transform parent,
            string name,
            IReadOnlyList<M1ChoiceView> choices,
            string selectedId,
            Func<string, M1CommandResult> command)
        {
            if (choices == null || choices.Count == 0) return;
            const int columns = 3;
            for (var rowIndex = 0; rowIndex < (choices.Count + columns - 1) / columns; rowIndex++)
            {
                var row = AddRow(parent, name + " Row " + rowIndex, 10f, 184f);
                foreach (var choice in choices.Skip(rowIndex * columns).Take(columns))
                {
                    var captured = choice;
                    var button = RuntimeUi.AddButton(
                        row,
                        name + " " + captured.Id,
                        captured.DisplayName.ToUpperInvariant(),
                        () => Execute(command(captured.Id)),
                        178f,
                        StringComparer.Ordinal.Equals(captured.Id, selectedId) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                    var label = button.GetComponentInChildren<Text>();
                    if (label != null)
                    {
                        label.fontSize = 34;
                        label.rectTransform.anchorMin = new Vector2(0.04f, 0.56f);
                        label.rectTransform.anchorMax = new Vector2(0.96f, 0.96f);
                        label.rectTransform.offsetMin = Vector2.zero;
                        label.rectTransform.offsetMax = Vector2.zero;
                    }
                    var summary = RuntimeUi.AddText(
                        button.transform,
                        "Choice Effect Summary",
                        captured.Summary ?? string.Empty,
                        26,
                        TextAnchor.MiddleCenter,
                        RuntimeUi.MutedText,
                        FontStyle.Bold);
                    summary.rectTransform.anchorMin = new Vector2(0.04f, 0.04f);
                    summary.rectTransform.anchorMax = new Vector2(0.96f, 0.60f);
                    summary.rectTransform.offsetMin = Vector2.zero;
                    summary.rectTransform.offsetMax = Vector2.zero;
                    summary.raycastTarget = false;
                }
            }
        }

        private void SaveUnions()
        {
            var result = _coordinator.SaveAndReloadProof();
            _localStatus = result.Message;
            _localStatusPositive = result.Succeeded;
            if (result.Succeeded)
            {
                ConfirmGuidedFirstContractUnionBriefing080();
                // The full Guild/City game should land in the illustrated Hall,
                // not the legacy proof/dashboard screen.  Keep the smaller base
                // coordinator's established completion route for compatibility.
                if (_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D)
                {
                    if (_returnToChapterTwoAfterUnionRepair076)
                    {
                        _returnToChapterTwoAfterUnionRepair076 = false;
                        _guildCityTab017D = "CHAPTER2";
                    }
                    else if (_returnToExpeditionAfterUnionReview076)
                    {
                        if (TryReturnToGuidedFirstHourFieldAfterUnionReview076())
                            return;
                        _guildCityTab017D = "EXPEDITION";
                    }
                    else
                    {
                        _guildCityTab017D = "HALL";
                    }
                    Navigate(M1Screen.GuildOperations);
                }
                else
                {
                    Navigate(M1Screen.Complete);
                }
            }
            else
            {
                BuildCurrentScreen();
            }
        }

        private void BackFromUnionBuilder068()
        {
            ConfirmGuidedFirstContractUnionBriefing080();
            if (_returnToChapterTwoAfterUnionRepair076)
            {
                _returnToChapterTwoAfterUnionRepair076 = false;
                _guildCityTab017D = "CHAPTER2";
                Navigate(M1Screen.GuildOperations);
                return;
            }
            if (_returnToExpeditionAfterUnionReview076)
            {
                if (TryReturnToGuidedFirstHourFieldAfterUnionReview076())
                    return;
                _guildCityTab017D = "EXPEDITION";
                Navigate(M1Screen.GuildOperations);
                return;
            }
            if (_returnToWalkableHall069 && _coordinator is M1RuntimeCoordinator)
            {
                ReturnToWalkableHall069();
                return;
            }
            if (_coordinator.State.ResumeScreen == M1Screen.UnionBuilder)
            {
                Navigate(M1Screen.Equipment);
                return;
            }
            Navigate(M1Screen.GuildOperations);
        }

        private bool TryReturnToGuidedFirstHourFieldAfterUnionReview076()
        {
            _returnToExpeditionAfterUnionReview076 = false;
            var coordinator076 = _coordinator as
                GuildCity017D.IGuildCityPresentationCoordinator017D;
            if (!ShouldEnterGuidedFirstHourField076(coordinator076))
                return false;

            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "EXPEDITION";
            _guildCityMoreOpen060 = false;
            _localStatus = "The Lantern Patrol joined your deployed Unions. Face the Gate-Eater at the saved marker.";
            _localStatusPositive = true;
            EnterExpeditionBoard074(coordinator076);
            return true;
        }

        private void BuildComplete()
        {
            var body = CreatePage(
                "RUINED ANNEX — GUILD HALL",
                "THE FIRST GUILD STANDS READY • THE GATEWORKS ALARM IS SOUNDING",
                () => Navigate(M1Screen.MainMenu));

            var facilities = AddRow(body, "Living Guild Hall Facilities", 18f, 360f);
            var applicants = RuntimeUi.AddButton(
                facilities,
                "Applicant Board Hotspot",
                "APPLICANT BOARD",
                () => Navigate(M1Screen.ApplicantBoard),
                340f,
                new Color(1f, 1f, 1f, 0.82f));
            M1PremiumUi.StyleLocationHotspot(applicants, "A", "RECRUITMENT DESK");

            var quartermaster = RuntimeUi.AddButton(
                facilities,
                "Quartermaster Hotspot",
                "QUARTERMASTER",
                () => Navigate(M1Screen.Equipment),
                340f,
                new Color(1f, 1f, 1f, 0.82f));
            M1PremiumUi.StyleLocationHotspot(quartermaster, "EQ", "ARMORY & LOADOUTS");

            var strategy = RuntimeUi.AddButton(
                facilities,
                "Union Strategy Hotspot",
                "UNION STRATEGY",
                () => Navigate(M1Screen.UnionBuilder),
                340f,
                new Color(1f, 1f, 1f, 0.82f));
            M1PremiumUi.StyleLocationHotspot(strategy, "III", "FORMATIONS & DOCTRINE");

            if (_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D)
            {
                var operations = RuntimeUi.AddButton(
                    facilities,
                    "Living Guild Battle Now Hotspot 060",
                    BattleNowLabel060(),
                    OpenBattleNow060,
                    340f,
                    RuntimeUi.Accent);
                M1PremiumUi.StyleLocationHotspot(operations, "!", "BATTLE READY", selected: true);
                RuntimeUi.AddButton(body, "Guild Story And Operations 060", AdventureLabel060(),
                    ContinueAdventure060, RuntimeUi.PrimaryTouchPixels);
            }

            if (_coordinator is IM2PresentationCoordinator &&
                !(_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D))
            {
                var gateworks = RuntimeUi.AddButton(
                    facilities,
                    "Enter Cinematic Tutorial Battle",
                    "START BATTLE",
                    BeginTutorialBattle,
                    340f,
                    RuntimeUi.Accent);
                M1PremiumUi.StyleLocationHotspot(gateworks, "!", "GATEWORKS ALARM", selected: true);
            }

            var guildProgression = AddRow(body, "Persistent Guild Progression 021", 16f, 255f);
            var guildLedger = AddColumnPanel(guildProgression, "GUILD LEVEL & TREASURY", 1f, 245f);
            RuntimeUi.AddText(
                guildLedger,
                "Persistent Guild XP Ledger 021",
                "GUILD LV " + Math.Max(1, _coordinator.State.GuildLevel) +
                "\nGUILD XP " + FormatProgressionNumber(_coordinator.State.GuildXpIntoCurrentLevel) +
                " / " + FormatProgressionNumber(_coordinator.State.GuildXpRequiredForNextLevel) +
                "\nXP TO SPEND " + FormatProgressionNumber(_coordinator.State.TreasuryXp),
                34,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);

            var hallLedger = AddColumnPanel(guildProgression, "HALL ENHANCEMENT", 1f, 245f);
            var hallName = CleanHallStageName(_coordinator.State.HallStageName);
            RuntimeUi.AddText(
                hallLedger,
                "Persistent Hall Enhancement Ledger 021",
                "HALL STAGE " + Math.Max(0, _coordinator.State.HallStageIndex) + "  |  " + hallName.ToUpperInvariant() +
                "\nHALL ENHANCEMENT XP " + FormatProgressionNumber(_coordinator.State.HallEnhancementXp) +
                "\nEACH CLAIMED BATTLE BUILDS THE GUILD",
                34,
                TextAnchor.MiddleLeft,
                BattleCohesion,
                FontStyle.Bold);

            var facilitiesState = _coordinator.State.Facilities ?? Array.Empty<M1FacilityProgressionView>();
            var foundation = AddColumnPanel(body, "19-FACILITY FOUNDATION", 1f, 390f);
            RuntimeUi.AddText(
                foundation,
                "Facility Foundation Summary 021",
                facilitiesState.Count + " / 19 FACILITIES RECORDED  |  LEVEL 0 FOUNDATION",
                38,
                TextAnchor.MiddleLeft,
                RuntimeUi.Warning,
                FontStyle.Bold);
            var facilityColumns = AddRow(foundation, "All Facility Levels 021", 22f, 250f);
            RuntimeUi.AddText(
                facilityColumns,
                "Facility Levels Column A 021",
                FacilityProgressionColumn(facilitiesState, 0, 10),
                27,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.AddText(
                facilityColumns,
                "Facility Levels Column B 021",
                FacilityProgressionColumn(facilitiesState, 10, 9),
                27,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);

            var recruits = _coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            if (recruits.Count > 0)
            {
                var dutyRow = AddRow(body, "Guild Members On Duty", 14f, 220f);
                foreach (var recruit in recruits.Take(3))
                {
                    var captured = recruit;
                    var cameo = RuntimeUi.AddButton(
                        dutyRow,
                        "Guild Hall Cameo " + captured.RecruitId,
                        captured.DisplayName + "\n" + captured.ObservedClass + " • ON DUTY" +
                        "\nLV " + Math.Max(1, captured.Level) + "  |  " + RecruitXpProgress(captured),
                        null,
                        210f,
                        new Color(0.035f, 0.055f, 0.07f, 0.72f));
                    cameo.interactable = false;
                    AddPortraitToButton(cameo, captured, large: false);
                    M1PremiumUi.AddClassCrest(cameo, captured.ClassSymbol, captured.ObservedClass);
                }
            }

            if (!string.IsNullOrWhiteSpace(_localStatus))
            {
                AddStatus(body, _localStatus, _localStatusPositive);
            }

            var proofRow = AddRow(body, "Premium Completion Medallions", 14f, 170f);
            M1PremiumUi.AddProofMedallion(
                proofRow,
                "Recruit Proof",
                "VI",
                "MEMBERS",
                recruits.Count + " MEMBERS",
                _coordinator.State.AllSixSigned);
            M1PremiumUi.AddProofMedallion(proofRow, "Equipment Proof", "EQ", "EQUIPMENT", "READY", _coordinator.State.OpeningEquipmentLegal);
            M1PremiumUi.AddProofMedallion(
                proofRow,
                "Union Proof",
                _coordinator.State.UsedUnionCount.ToString(),
                "UNION PLANS",
                _coordinator.State.UsedUnionCount + " READY",
                _coordinator.State.OpeningUnionsLegal);
            M1PremiumUi.AddProofMedallion(
                proofRow,
                "Save Proof",
                "✓",
                "SAVED PARTY",
                _coordinator.State.SaveReloadVerified ? "SECURED" : "REQUIRED",
                _coordinator.State.SaveReloadVerified);
            AddMessagePanel(
                body,
                _coordinator.State.SaveReloadVerified ? "GUILD DEPLOYMENT READY" : "PARTY NEEDS ATTENTION",
                "Your " + recruits.Count + " members have " + _coordinator.State.UsedUnionCount +
                " ready Union plans, ready equipment, and preserved formations, doctrines, and leaders.",
                _coordinator.State.SaveReloadVerified ? RuntimeUi.Positive : RuntimeUi.Warning);

            var editRow = AddRow(body, "Edit Actions", 18f);
            RuntimeUi.AddButton(editRow, "Edit Equipment", "EDIT EQUIPMENT", () => Navigate(M1Screen.Equipment));
            RuntimeUi.AddButton(editRow, "Edit Unions", "EDIT UNIONS", () => Navigate(M1Screen.UnionBuilder));
            RuntimeUi.AddButton(body, "Reload Saved Proof", "CHECK PARTY READINESS", () => Execute(_coordinator.SaveAndReloadProof()));
            RuntimeUi.AddButton(body, "Return Main Menu", "RETURN TO TITLE", () => Navigate(M1Screen.MainMenu));
        }

        private RectTransform CreatePage(string title, string subtitle, Action back)
        {
            RuntimeUi.ClearChildren(_screenRoot);
            var page = RuntimeUi.AddPanel(
                _screenRoot,
                "Page " + title,
                _highContrast ? new Color(0f, 0f, 0f, 0.95f) : RuntimeUi.PanelOverlay);
            Stretch(page.rectTransform);
            M1PremiumUi.StylePage(page, _highContrast);
            RuntimeUi.AddVerticalLayout(page.transform, new RectOffset(22, 22, 18, 18), 12f);

            var header = RuntimeUi.AddPanel(page.transform, "Premium Screen Header", Color.white);
            var headerScale = Mathf.Max(1f, _textScale);
            RuntimeUi.SetLayout(header, preferredHeight: 168f * headerScale);
            M1PremiumUi.StylePanel(header, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddHorizontalLayout(header.transform, new RectOffset(24, 24, 16, 16), 20f, TextAnchor.MiddleLeft);

            if (back != null)
            {
                var backButton = RuntimeUi.AddButton(header.transform, "Back", "←  BACK", back, RuntimeUi.MinimumTouchPixels);
                RuntimeUi.SetLayout(backButton, preferredWidth: 230f, flexibleWidth: 0f);
            }

            var titleStack = new GameObject("World Location Title Stack", typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            titleStack.SetParent(header.transform, false);
            RuntimeUi.SetLayout(titleStack, preferredHeight: 136f * headerScale, flexibleWidth: 1f);
            RuntimeUi.AddVerticalLayout(titleStack, new RectOffset(16, 16, 4, 4), 2f, TextAnchor.MiddleLeft);
            var heading = RuntimeUi.AddText(titleStack, "Screen Title", title, 66, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 78f * headerScale);
            M1PremiumUi.ConfigureDisplayText(heading);
            ConfigureResponsiveText062(heading, 34, 66);
            var subheading = RuntimeUi.AddText(titleStack, "Screen Subtitle", subtitle, 34, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
            RuntimeUi.SetLayout(subheading, preferredHeight: 46f * headerScale);
            ConfigureResponsiveText062(subheading, 22, 34);

            var locationBadge = RuntimeUi.AddPanel(header.transform, "Living Location Badge", Color.white);
            RuntimeUi.SetLayout(locationBadge, preferredWidth: 350f, preferredHeight: 136f * headerScale, flexibleWidth: 0f);
            M1PremiumUi.StylePanel(locationBadge, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(locationBadge.transform, new RectOffset(20, 20, 10, 10), 0f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(locationBadge.transform, "Location Layer", "LIVING GUILD", 22, TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold);
            RuntimeUi.AddText(locationBadge.transform, "Location Name", WorldLocationLabel(_screen), 30, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            var presentation110 = _coordinator.State;
            RuntimeUi.AddText(
                locationBadge.transform,
                "Always Visible Spendable XP 073",
                PremiumProgressionBadgeCopyForVerification079(
                    presentation110.HasCampaign,
                    presentation110.GuildXpIntoCurrentLevel,
                    presentation110.GuildXpRequiredForNextLevel,
                    presentation110.TreasuryXp),
                18,
                TextAnchor.MiddleCenter,
                RuntimeUi.Positive,
                FontStyle.Bold);

            var scrollObject = new GameObject("Scrollable Body", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollObject.transform.SetParent(page.transform, false);
            scrollObject.GetComponent<Image>().color = Color.clear;
            var scrollLayout = RuntimeUi.SetLayout(
                scrollObject.GetComponent<RectTransform>(),
                preferredHeight: 0f,
                flexibleHeight: 1f);
            scrollLayout.minHeight = RuntimeUi.MinimumTouchPixels;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObject.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>();
            Stretch(viewportRect);
            viewportRect.offsetMax = new Vector2(-26f, 0f);
            // Mask still needs a rendered stencil source even when its own graphic
            // is hidden. Sub-byte alpha can quantize to zero in the CanvasRenderer,
            // culling the mask mesh and every child below it on some displays.
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            var content = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            RuntimeUi.AddVerticalLayout(content.transform, new RectOffset(10, 10, 10, 10), 16f);
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 72f;
            scroll.verticalNormalizedPosition = 1f;
            AddPremiumScrollbar(scrollObject.transform, scroll);

            _activePage = page.rectTransform;
            _activeContent = contentRect;
            _activeScroll = scroll;
            return contentRect;
        }

        private static string WorldLocationLabel(M1Screen screen)
        {
            switch (screen)
            {
                case M1Screen.MainMenu: return "SKYHOME";
                case M1Screen.NewGuild: return "CHARTER DESK";
                case M1Screen.ApplicantBoard: return "RECRUITMENT";
                case M1Screen.RecruitDetail: return "INTERVIEW NOOK";
                case M1Screen.Equipment: return "QUARTERMASTER";
                case M1Screen.UnionBuilder: return "STRATEGY CHAMBER";
                case M1Screen.Complete: return "RUINED ANNEX";
                case M1Screen.GuildOperations: return "GUILD OPERATIONS";
                case M1Screen.Battle: return "GATEWORKS";
                case M1Screen.BattleResults: return "AFTER ACTION";
                default: return "GUILD CHRONICLE";
            }
        }

        public static string PremiumProgressionBadgeCopyForVerification079(
            bool hasCampaign,
            long guildXpIntoCurrentLevel,
            long guildXpRequiredForNextLevel,
            long xpToSpend)
        {
            if (!hasCampaign)
                return "GUILD XP — / —\nXP TO SPEND —";

            return "GUILD XP " +
                   FormatProgressionNumber(guildXpIntoCurrentLevel) + " / " +
                   FormatProgressionNumber(guildXpRequiredForNextLevel) +
                   "\nXP TO SPEND " + FormatProgressionNumber(xpToSpend);
        }

        private static string CompactPlayerFacingLine073(string value, int maximumCharacters)
        {
            var compact = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (compact.Contains("  ")) compact = compact.Replace("  ", " ");
            if (compact.Length <= maximumCharacters) return compact;
            return compact.Substring(0, Mathf.Max(1, maximumCharacters - 1)).TrimEnd() + "…";
        }

        private static void AddPremiumScrollbar(Transform parent, ScrollRect scroll)
        {
            var track = RuntimeUi.AddPanel(parent, "Premium Scroll Track", new Color(0.035f, 0.05f, 0.06f, 0.94f));
            track.rectTransform.anchorMin = new Vector2(1f, 0f);
            track.rectTransform.anchorMax = new Vector2(1f, 1f);
            track.rectTransform.pivot = new Vector2(1f, 0.5f);
            track.rectTransform.sizeDelta = new Vector2(22f, 0f);
            track.rectTransform.anchoredPosition = new Vector2(-3f, 0f);
            M1PremiumUi.StylePanel(track, M1PremiumUi.Surface.Iron);

            var slidingArea = RuntimeUi.AddStretchRect(track.transform, "Sliding Area");
            slidingArea.offsetMin = new Vector2(4f, 10f);
            slidingArea.offsetMax = new Vector2(-4f, -10f);
            var handle = RuntimeUi.AddPanel(slidingArea, "Premium Scroll Thumb", new Color(0.34f, 0.78f, 0.85f, 0.96f));
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(1f, 0.28f);
            handle.rectTransform.offsetMin = Vector2.zero;
            handle.rectTransform.offsetMax = Vector2.zero;

            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.targetGraphic = handle;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 10f;
        }

        private System.Collections.IEnumerator FinalizeActivePageLayoutNextFrame()
        {
            yield return null;
            _pendingLayoutPass = null;
            FinalizeActivePageLayout();
        }

        private void FinalizeActivePageLayout()
        {
            if (_activePage == null || _activeContent == null || _activeScroll == null) return;
            if (!_activePage.gameObject.activeInHierarchy || !_activeContent.gameObject.activeInHierarchy) return;

            // The page and its scroll content are assembled at runtime in one frame.
            // Resolve the parent allocation first, then every nested content layout,
            // so the scroll viewport receives the remaining page height and the
            // content starts with real geometry instead of a transient zero-height
            // layout. A second pass on the following frame covers the CanvasScaler
            // and safe-area update that can occur after a scene-to-M1 transition.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_activePage);
            ForceRebuildLayoutTree(_activeContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_activePage);
            Canvas.ForceUpdateCanvases();

            _activeScroll.StopMovement();
            _activeScroll.verticalNormalizedPosition = 1f;
            Canvas.ForceUpdateCanvases();
        }

        private static void ForceRebuildLayoutTree(RectTransform root)
        {
            var rects = root.GetComponentsInChildren<RectTransform>(includeInactive: false);
            for (var i = rects.Length - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rects[i]);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
        }

        private void BuildCoordinatorMissing()
        {
            RuntimeUi.ClearChildren(_screenRoot);
            var panel = RuntimeUi.AddPanel(_screenRoot, "M1 Coordinator Missing", RuntimeUi.Panel);
            Stretch(panel.rectTransform);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(120, 120, 120, 120), 30f, TextAnchor.MiddleCenter);
            RuntimeUi.AddText(panel.transform, "Title", "SKYHOME COULD NOT OPEN", RuntimeUi.HeadingFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            RuntimeUi.AddText(
                panel.transform,
                "Message",
                "The game could not finish loading. Return to the title and try again.",
                RuntimeUi.BodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning);
        }

        private void Navigate(M1Screen screen)
        {
            CloseVersion69Experiences069();
            _screen = screen;
            _localStatus = string.Empty;
            _localStatusPositive = false;
            BuildCurrentScreen();
        }

        private void Execute(M1CommandResult result)
        {
            _localStatus = result?.Message ?? "Something went wrong—try again.";
            _localStatusPositive = result != null && result.Succeeded;
            BuildCurrentScreen();
        }

        private M1ApplicantView FindApplicant(string recruitId) =>
            (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
            .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.RecruitId, recruitId));

        private static M1ChoiceView FindChoice(IReadOnlyList<M1ChoiceView> choices, string id) =>
            (choices ?? Array.Empty<M1ChoiceView>()).FirstOrDefault(value => StringComparer.Ordinal.Equals(value.Id, id));

        private void CycleChoice(IReadOnlyList<M1ChoiceView> choices, string currentId, Func<string, M1CommandResult> command)
        {
            if (choices == null || choices.Count == 0) return;
            var index = -1;
            for (var i = 0; i < choices.Count; i++)
            {
                if (StringComparer.Ordinal.Equals(choices[i].Id, currentId)) index = i;
            }
            Execute(command(choices[(index + 1) % choices.Count].Id));
        }

        private void ShowConfirmation(
            string title,
            string message,
            string confirmLabel,
            Action confirm,
            string cancelLabel = "CANCEL",
            Color? confirmColor = null)
        {
            var previousSelection = EventSystem.current?.currentSelectedGameObject;
            var blocker = RuntimeUi.AddPanel(_canvas.transform, "Confirmation Blocker", new Color(0f, 0f, 0f, 0.85f));
            Stretch(blocker.rectTransform);
            var safe = RuntimeUi.AddSafeArea(blocker.transform);
            var modal = RuntimeUi.AddPanel(safe, "Confirmation", RuntimeUi.PanelRaised);
            var rect = modal.rectTransform;
            rect.anchorMin = new Vector2(0.18f, 0.18f);
            rect.anchorMax = new Vector2(0.82f, 0.82f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(modal, M1PremiumUi.Surface.Warning);
            RuntimeUi.AddVerticalLayout(modal.transform, new RectOffset(90, 90, 70, 70), 28f, TextAnchor.MiddleCenter);
            var modalTitle = RuntimeUi.AddText(modal.transform, "Title", title, RuntimeUi.HeadingFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold);
            M1PremiumUi.ConfigureDisplayText(modalTitle);
            M1PremiumUi.AddHeaderOrnament(modal.transform);
            if (_textScale > 1f)
            {
                // Keep the title and both decisions visible. Long cost/consent
                // copy scrolls at the requested size rather than losing lines.
                ConfigureAuthoredCompactText076(modalTitle, 32, RuntimeUi.HeadingFontPixels);
                modalTitle.verticalOverflow = VerticalWrapMode.Truncate;
                RuntimeUi.SetLayout(modalTitle, preferredHeight: 128f).minHeight = 96f;
                var holder = new GameObject("Confirmation Message Scroll 156", typeof(RectTransform), typeof(ScrollRect));
                holder.transform.SetParent(modal.transform, false);
                RuntimeUi.SetLayout(holder.GetComponent<RectTransform>(), preferredHeight: 0f, flexibleHeight: 1f).minHeight = 132f;
                var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
                viewport.transform.SetParent(holder.transform, false);
                viewport.GetComponent<Image>().color = Color.clear;
                var viewportRect = viewport.GetComponent<RectTransform>();
                Stretch(viewportRect);
                viewportRect.offsetMax = new Vector2(-26f, 0f);
                var body = RuntimeUi.AddText(viewport.transform, "Message", message,
                    RuntimeUi.BodyFontPixels, TextAnchor.UpperLeft, RuntimeUi.Text);
                body.verticalOverflow = VerticalWrapMode.Overflow;
                var content = body.rectTransform;
                content.anchorMin = new Vector2(0f, 1f);
                content.anchorMax = new Vector2(1f, 1f);
                content.pivot = new Vector2(.5f, 1f);
                content.anchoredPosition = Vector2.zero;
                content.sizeDelta = Vector2.zero;
                body.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                var scroll = holder.GetComponent<ScrollRect>();
                scroll.viewport = viewportRect;
                scroll.content = content;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 72f;
                AddPremiumScrollbar(holder.transform, scroll);
            }
            else RuntimeUi.AddText(modal.transform, "Message", message, RuntimeUi.BodyFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Text);
            var confirmButton = RuntimeUi.AddButton(modal.transform, "Confirm", confirmLabel, () =>
            {
                blocker.gameObject.SetActive(false);
                Destroy(blocker.gameObject);
                confirm();
            }, RuntimeUi.PrimaryTouchPixels, confirmColor);

            Button cancelButton = null;
            Action cancel = () =>
            {
                blocker.gameObject.SetActive(false);
                Destroy(blocker.gameObject);
                if (previousSelection != null && previousSelection.activeInHierarchy &&
                    EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(previousSelection);
            };
            cancelButton = RuntimeUi.AddButton(
                modal.transform,
                "Cancel",
                cancelLabel,
                cancel,
                RuntimeUi.MinimumTouchPixels);

            var confirmNavigation = confirmButton.navigation;
            confirmNavigation.mode = Navigation.Mode.Explicit;
            confirmNavigation.selectOnUp = cancelButton;
            confirmNavigation.selectOnDown = cancelButton;
            confirmButton.navigation = confirmNavigation;
            var cancelNavigation = cancelButton.navigation;
            cancelNavigation.mode = Navigation.Mode.Explicit;
            cancelNavigation.selectOnUp = confirmButton;
            cancelNavigation.selectOnDown = confirmButton;
            cancelButton.navigation = cancelNavigation;

            var cancelHandler = cancelButton.gameObject.AddComponent<ConfirmationCancelHandler077>();
            cancelHandler.Cancel = cancel;
            ApplyTextScale(modal.transform);
            cancelButton.Select();
        }

        private static void AddRecruitShowcase(
            Transform parent,
            M1RecruitLoadoutView recruit,
            string title,
            float preferredHeight)
        {
            if (parent == null || recruit == null) return;
            var panel = RuntimeUi.AddPanel(parent, "Large Recruit Showcase " + recruit.RecruitId, RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: preferredHeight);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Vellum);

            var portrait = RuntimeUi.AddPanel(panel.transform, "Large Selected Portrait " + recruit.RecruitId, RuntimeUi.Accent);
            portrait.rectTransform.anchorMin = new Vector2(0.025f, 0.06f);
            portrait.rectTransform.anchorMax = new Vector2(0.43f, 0.94f);
            portrait.rectTransform.offsetMin = Vector2.zero;
            portrait.rectTransform.offsetMax = Vector2.zero;
            PopulatePortraitFrame(
                portrait,
                recruit.RecruitId,
                recruit.VisualSeed,
                recruit.RaceId,
                recruit.PortraitAuthorityId,
                recruit.DisplayName,
                M1VisualAssets.HumanizeRace(recruit.RaceId),
                recruit.DisplayName + "\n" + recruit.ClassSymbol + "  " + recruit.ObservedClass,
                roleIdentity: recruit.ObservedClass);

            var details = RuntimeUi.AddPanel(panel.transform, "Large Recruit Details " + recruit.RecruitId, new Color(0.03f, 0.05f, 0.07f, 0.94f));
            details.rectTransform.anchorMin = new Vector2(0.46f, 0.06f);
            details.rectTransform.anchorMax = new Vector2(0.975f, 0.94f);
            details.rectTransform.offsetMin = Vector2.zero;
            details.rectTransform.offsetMax = Vector2.zero;
            M1PremiumUi.StylePanel(details, M1PremiumUi.Surface.Iron);
            RuntimeUi.AddVerticalLayout(details.transform, new RectOffset(28, 28, 22, 22), 8f, TextAnchor.MiddleCenter);
            var classColor = M1PremiumUi.ClassColor(recruit.ObservedClass);
            RuntimeUi.AddText(details.transform, "Showcase Title", title, RuntimeUi.SmallBodyFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
            if (preferredHeight >= 400f)
                RuntimeUi.AddText(details.transform, "Showcase Class Symbol", recruit.ClassSymbol,
                    94, TextAnchor.MiddleCenter, classColor, FontStyle.Bold);
            RuntimeUi.AddText(details.transform, "Showcase Name", recruit.DisplayName, RuntimeUi.BodyFontPixels, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            RuntimeUi.AddText(details.transform, "Showcase Class", recruit.ObservedClass + "  •  " + M1VisualAssets.HumanizeRace(recruit.RaceId), RuntimeUi.SmallBodyFontPixels, TextAnchor.MiddleCenter, classColor, FontStyle.Bold);

            var progressionSummary = preferredHeight < 400f
                ? "LV " + Math.Max(1, recruit.Level) + "  |  " + RecruitXpProgress(recruit) +
                  "\nTOP ART  |  " + RecruitTopArts(recruit, 1)
                : "LV " + Math.Max(1, recruit.Level) + "  |  PERSONAL XP " +
                  FormatProgressionNumber(recruit.TotalPersonalXp) + "  |  " + RecruitXpProgress(recruit) +
                  "\nLIVE STATS  |  HP " + recruit.MaximumHp + BonusText(recruit.MaximumHpBonus) +
                  "  MP " + recruit.MaximumMp + BonusText(recruit.MaximumMpBonus) +
                  "  STR " + recruit.StrengthIndex + BonusText(recruit.StrengthBonus) +
                  "  DEF " + recruit.DefenseIndex + BonusText(recruit.DefenseBonus) +
                  "  AGI " + recruit.AgilityIndex + BonusText(recruit.AgilityBonus) +
                  "  MAG " + recruit.MagicIndex + BonusText(recruit.MagicBonus) +
                  "  WILL " + recruit.WillIndex + BonusText(recruit.WillBonus) +
                  "\nTOP USE-BASED ARTS  |  " + RecruitTopArts(recruit, 3);
            RuntimeUi.AddText(
                details.transform,
                "Showcase Persistent Progression 021",
                progressionSummary,
                preferredHeight < 400f ? 22 : RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleLeft,
                RuntimeUi.Warning,
                FontStyle.Bold);

            var equipmentSummary = recruit.Slots == null
                ? "No loadout available"
                : string.Join("\n", recruit.Slots
                    .Where(value => !string.IsNullOrWhiteSpace(value.EquippedItemId))
                    .Select(value => value.VisualGlyph + "  " + value.EquippedItemName));
            if (preferredHeight >= 400f)
                RuntimeUi.AddText(
                    details.transform,
                    "Showcase Equipment",
                    string.IsNullOrWhiteSpace(equipmentSummary) ? "No equipment assigned" : equipmentSummary,
                    RuntimeUi.SmallBodyFontPixels,
                    TextAnchor.MiddleLeft,
                    RuntimeUi.Text);
        }

        private static string RecruitXpProgress(M1RecruitLoadoutView recruit)
        {
            if (recruit == null) return "XP unavailable";
            if (recruit.XpRequiredForNextLevel <= 0) return "XP " + FormatProgressionNumber(recruit.TotalPersonalXp);
            return "XP " + FormatProgressionNumber(recruit.XpIntoCurrentLevel) + " / " +
                   FormatProgressionNumber(recruit.XpRequiredForNextLevel);
        }

        private static string BonusText(int amount) =>
            amount > 0 ? " (+" + amount + ")" : string.Empty;

        private static string CleanHallStageName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                value.IndexOf("RUINED ANNEX", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Ruined Annex";
            return value;
        }

        private static string RecruitTopArts(M1RecruitLoadoutView recruit, int limit)
        {
            var mastery = recruit?.ArtMastery ?? Array.Empty<M1ArtMasteryView>();
            var top = mastery
                .Where(value => value != null)
                .OrderByDescending(value => value.MasteryPoints)
                .ThenBy(value => value.DisplayName, StringComparer.Ordinal)
                .Take(Math.Max(1, limit))
                .Select(value =>
                {
                    var name = string.IsNullOrWhiteSpace(value.DisplayName)
                        ? HumanizePresentationId(value.ArtId)
                        : value.DisplayName;
                    return name + " " + value.MasteryPoints + " XP / " + value.MeaningfulUses + " uses";
                })
                .ToArray();
            return top.Length == 0
                ? "No mastery yet - Arts grow only through meaningful use"
                : string.Join("  •  ", top);
        }

        private static string FacilityProgressionColumn(
            IReadOnlyList<M1FacilityProgressionView> facilities,
            int start,
            int count)
        {
            if (facilities == null || facilities.Count <= start) return "Facility records are being established.";
            return string.Join("\n", facilities
                .Skip(start)
                .Take(count)
                .Select(value =>
                    (string.IsNullOrWhiteSpace(value.DisplayName) ? value.FacilityId : value.DisplayName) +
                    "  |  LV " + Math.Max(0, value.Level) +
                    "  |  XP " + FormatProgressionNumber(value.TotalFacilityXp)));
        }

        private static void AddPortraitToButton(
            Button button,
            M1ApplicantView applicant,
            bool large,
            bool cropToFill = false)
        {
            AddPortraitToButton(
                button,
                applicant.RecruitId,
                applicant.VisualSeed,
                applicant.RaceId,
                applicant.PortraitAuthorityId,
                applicant.DisplayName,
                applicant.RaceAndWorld,
                large,
                cropToFill,
                applicant.ObservedClass);
        }

        private static void AddPortraitToButton(
            Button button,
            M1RecruitLoadoutView recruit,
            bool large,
            bool cropToFill = false)
        {
            AddPortraitToButton(
                button,
                recruit.RecruitId,
                recruit.VisualSeed,
                recruit.RaceId,
                recruit.PortraitAuthorityId,
                recruit.DisplayName,
                M1VisualAssets.HumanizeRace(recruit.RaceId),
                large,
                cropToFill,
                recruit.ObservedClass);
        }

        private static void AddPortraitToButton(
            Button button,
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string displayName,
            string raceLabel,
            bool large,
            bool cropToFill = false,
            string roleIdentity = null)
        {
            if (button == null) return;
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.rectTransform.anchorMin = large ? new Vector2(0.04f, 0.025f) : new Vector2(0.34f, 0.04f);
                label.rectTransform.anchorMax = large ? new Vector2(0.96f, 0.33f) : new Vector2(0.97f, 0.96f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
                label.alignment = large ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
            }

            var frame = RuntimeUi.AddPanel(button.transform, "Portrait Frame " + recruitId, RuntimeUi.Accent);
            frame.raycastTarget = false;
            frame.rectTransform.anchorMin = large ? new Vector2(0.05f, 0.35f) : new Vector2(0.035f, 0.12f);
            frame.rectTransform.anchorMax = large ? new Vector2(0.95f, 0.96f) : new Vector2(0.30f, 0.88f);
            frame.rectTransform.offsetMin = Vector2.zero;
            frame.rectTransform.offsetMax = Vector2.zero;
            PopulatePortraitFrame(
                frame,
                recruitId,
                visualSeed,
                raceId,
                portraitAuthorityId,
                displayName,
                raceLabel,
                identityLabel: null,
                cropToFill: cropToFill,
                roleIdentity: roleIdentity);
        }

        private static void PopulatePortraitFrame(
            Image frame,
            string recruitId,
            string visualSeed,
            string raceId,
            string portraitAuthorityId,
            string displayName,
            string raceLabel,
            string identityLabel,
            bool cropToFill = false,
            string roleIdentity = null)
        {
            if (frame == null) return;
            frame.raycastTarget = false;
            M1PremiumUi.StylePortraitFrame(frame);

            var hasIdentity = !string.IsNullOrWhiteSpace(identityLabel);
            var backing = RuntimeUi.AddPanel(
                frame.transform,
                "Portrait Backing",
                M1VisualAssets.FallbackPortraitColor(raceId, visualSeed, recruitId));
            backing.raycastTarget = false;
            backing.rectTransform.anchorMin = hasIdentity ? new Vector2(0f, 0.27f) : Vector2.zero;
            backing.rectTransform.anchorMax = Vector2.one;
            backing.rectTransform.offsetMin = new Vector2(7f, 7f);
            backing.rectTransform.offsetMax = new Vector2(-7f, -7f);
            if (cropToFill)
                backing.gameObject.AddComponent<RectMask2D>();

            if (M1VisualAssets.TryResolvePortrait(
                    recruitId,
                    visualSeed,
                    raceId,
                    portraitAuthorityId,
                    roleIdentity,
                    out var sprite,
                    out _))
            {
                var artwork = RuntimeUi.AddPanel(backing.transform, "Portrait Artwork", Color.white);
                Stretch(artwork.rectTransform);
                artwork.sprite = sprite;
                artwork.type = Image.Type.Simple;
                artwork.preserveAspect = !cropToFill;
                artwork.raycastTarget = false;
                if (cropToFill)
                {
                    var fitter = artwork.gameObject.AddComponent<AspectRatioFitter>();
                    fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                    fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
                }
            }
            else
            {
                var initials = RuntimeUi.AddText(
                    backing.transform,
                    "Portrait Fallback Initials",
                    M1VisualAssets.Initials(displayName),
                    hasIdentity ? 138 : 60,
                    TextAnchor.MiddleCenter,
                    new Color(0.95f, 0.92f, 0.82f, 0.96f),
                    FontStyle.Bold);
                initials.rectTransform.anchorMin = new Vector2(0.05f, 0.22f);
                initials.rectTransform.anchorMax = new Vector2(0.95f, 0.94f);
                initials.rectTransform.offsetMin = Vector2.zero;
                initials.rectTransform.offsetMax = Vector2.zero;
                initials.raycastTarget = false;

                var race = RuntimeUi.AddText(
                    backing.transform,
                    "Portrait Fallback Race",
                    string.IsNullOrWhiteSpace(raceLabel) ? M1VisualAssets.HumanizeRace(raceId) : raceLabel,
                    hasIdentity ? 38 : 26,
                    TextAnchor.MiddleCenter,
                    new Color(0.95f, 0.92f, 0.82f, 0.78f),
                    FontStyle.Bold);
                race.rectTransform.anchorMin = new Vector2(0.04f, 0.03f);
                race.rectTransform.anchorMax = new Vector2(0.96f, 0.27f);
                race.rectTransform.offsetMin = Vector2.zero;
                race.rectTransform.offsetMax = Vector2.zero;
                race.raycastTarget = false;
            }

            if (hasIdentity)
            {
                var identityPanel = RuntimeUi.AddPanel(frame.transform, "Portrait Identity Strip", new Color(0.025f, 0.04f, 0.075f, 0.98f));
                identityPanel.raycastTarget = false;
                identityPanel.rectTransform.anchorMin = Vector2.zero;
                identityPanel.rectTransform.anchorMax = new Vector2(1f, 0.27f);
                identityPanel.rectTransform.offsetMin = new Vector2(7f, 7f);
                identityPanel.rectTransform.offsetMax = new Vector2(-7f, -2f);
                var identity = RuntimeUi.AddText(
                    identityPanel.transform,
                    "Portrait Identity",
                    identityLabel,
                    RuntimeUi.SmallBodyFontPixels,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                Stretch(identity.rectTransform);
                identity.rectTransform.offsetMin = new Vector2(16f, 8f);
                identity.rectTransform.offsetMax = new Vector2(-16f, -8f);
                identity.raycastTarget = false;
            }
        }

        private static RectTransform AddRow(
            Transform parent,
            string name,
            float spacing,
            float preferredHeight = RuntimeUi.PrimaryTouchPixels)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(LayoutElement)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            RuntimeUi.AddHorizontalLayout(rect, new RectOffset(0, 0, 0, 0), spacing);
            RuntimeUi.SetLayout(rect, preferredHeight: preferredHeight);
            return rect;
        }

        private static void PositionFormationSocket(RectTransform rect, string formationId, int slotIndex)
        {
            if (rect == null) return;
            var formation = (formationId ?? string.Empty).ToUpperInvariant();
            Vector2 min;
            Vector2 max;

            if (formation.Contains("SHIELD_WALL") || formation.Contains("SKIRMISH"))
            {
                min = new Vector2(0.025f + slotIndex * 0.325f, 0.12f);
                max = new Vector2(0.325f + slotIndex * 0.325f, 0.90f);
            }
            else if (formation.Contains("RESCUE_COLUMN"))
            {
                min = new Vector2(0.33f, 0.66f - slotIndex * 0.30f);
                max = new Vector2(0.67f, 0.96f - slotIndex * 0.30f);
            }
            else if (formation.Contains("CRESCENT"))
            {
                if (slotIndex == 0)
                {
                    min = new Vector2(0.34f, 0.04f);
                    max = new Vector2(0.66f, 0.55f);
                }
                else
                {
                    min = new Vector2(slotIndex == 1 ? 0.035f : 0.645f, 0.38f);
                    max = new Vector2(slotIndex == 1 ? 0.355f : 0.965f, 0.92f);
                }
            }
            else
            {
                if (slotIndex == 0)
                {
                    min = new Vector2(0.34f, 0.46f);
                    max = new Vector2(0.66f, 0.96f);
                }
                else
                {
                    min = new Vector2(slotIndex == 1 ? 0.035f : 0.645f, 0.035f);
                    max = new Vector2(slotIndex == 1 ? 0.355f : 0.965f, 0.53f);
                }
            }

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = new Vector2(8f, 8f);
            rect.offsetMax = new Vector2(-8f, -8f);
        }

        private static RectTransform AddColumnPanel(
            Transform parent,
            string title,
            float flexibleWidth,
            float preferredHeight = 620f)
        {
            var panel = RuntimeUi.AddPanel(parent, title, RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: preferredHeight, flexibleWidth: flexibleWidth);
            M1PremiumUi.StylePanel(
                panel,
                title.IndexOf("FORMATION", StringComparison.OrdinalIgnoreCase) >= 0
                    ? M1PremiumUi.Surface.WorldPaper
                    : M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(26, 26, 22, 22), 12f);
            if (title.StartsWith("Member Tree ", StringComparison.Ordinal) &&
                title.EndsWith(" 070", StringComparison.Ordinal))
            {
                // Four Art cards share one row. Their full-width path heading must
                // not inherit a divider's center-only label or its internal title.
                // The existing colored slot label below still shows Active/Locked.
                var heading = title.Substring("Member Tree ".Length,
                    title.Length - "Member Tree ".Length - " 070".Length);
                var statusSeparator = heading.IndexOf('•');
                if (statusSeparator >= 0) heading = heading.Substring(statusSeparator + 1).Trim();
                var treeHeading = RuntimeUi.AddText(panel.transform,
                    "Member Tree Compact Heading 070", heading, 34,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
                RuntimeUi.SetLayout(treeHeading, preferredHeight: 62f).minHeight = 62f;
                ConfigureAuthoredCompactText076(treeHeading, 28, 34);
                treeHeading.verticalOverflow = VerticalWrapMode.Truncate;
                treeHeading.raycastTarget = false;
            }
            else M1PremiumUi.AddSectionDivider(panel.transform, title);
            return panel.rectTransform;
        }

        private static Transform AddMessagePanel(Transform parent, string title, string message, Color titleColor)
        {
            var safeMessage = message ?? string.Empty;
            var explicitLines = safeMessage.Count(character => character == '\n');
            var estimatedWrappedLines = Mathf.Max(1, Mathf.CeilToInt(safeMessage.Length / 86f));
            var messageLines = Mathf.Max(1, explicitLines + estimatedWrappedLines);
            var messageHeight = Mathf.Clamp(66f + messageLines * 40f, 106f, 270f);
            var panelHeight = 40f + 58f + 4f + messageHeight;

            var panel = RuntimeUi.AddPanel(parent, title, RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(panel, preferredHeight: panelHeight);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(34, 34, 18, 18), 4f);
            AddResponsiveText062(
                panel.transform,
                "Title",
                title,
                24,
                36,
                58f,
                titleColor,
                FontStyle.Bold);
            AddResponsiveText062(
                panel.transform,
                "Message",
                safeMessage,
                22,
                34,
                messageHeight,
                RuntimeUi.Text,
                FontStyle.Normal,
                TextAnchor.UpperLeft);
            return panel.transform;
        }

        private static void AddStatus(Transform parent, string message, bool positive)
        {
            var safeMessage = (message ?? string.Empty).Trim();
            var estimatedLines = Mathf.Clamp(
                Mathf.Max(1, safeMessage.Count(character => character == '\n') + 1),
                1,
                3);
            if (safeMessage.Length > 78) estimatedLines = Mathf.Max(estimatedLines, 2);
            if (safeMessage.Length > 142) estimatedLines = 3;
            var statusHeight = 28f + estimatedLines * 24f;
            var panel = RuntimeUi.AddPanel(
                parent,
                positive ? "Pass Status" : "Notice Status",
                positive ? new Color(0.10f, 0.28f, 0.20f, 1f) : new Color(0.34f, 0.24f, 0.08f, 1f));
            RuntimeUi.SetLayout(panel, preferredHeight: Mathf.Max(RuntimeUi.MinimumTouchPixels, statusHeight));
            M1PremiumUi.StylePanel(panel, positive ? M1PremiumUi.Surface.Positive : M1PremiumUi.Surface.Warning);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(22, 22, 8, 8), 0f, TextAnchor.MiddleCenter);
            var statusText = AddResponsiveText062(
                panel.transform,
                "Status Text",
                (positive ? "✓ PASS — " : "▲ NOTICE — ") + safeMessage,
                18,
                RuntimeUi.SmallBodyFontPixels,
                Mathf.Max(RuntimeUi.MinimumTouchPixels - 16f, statusHeight - 16f),
                positive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            statusText.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static Text AddResponsiveText062(
            Transform parent,
            string name,
            string value,
            int minimumFontSize,
            int maximumFontSize,
            float preferredHeight,
            Color color,
            FontStyle style = FontStyle.Normal,
            TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var text = RuntimeUi.AddText(
                parent,
                name,
                value,
                maximumFontSize,
                alignment,
                color,
                style);
            RuntimeUi.SetLayout(text, preferredHeight: preferredHeight);
            ConfigureResponsiveText062(text, minimumFontSize, maximumFontSize);
            return text;
        }

        private static void ConfigureResponsiveText062(Text text, int minimumFontSize, int maximumFontSize)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(18, minimumFontSize);
            text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, maximumFontSize);
            text.fontSize = text.resizeTextMaxSize;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            if (!text.gameObject.name.EndsWith(" [Responsive 062]", StringComparison.Ordinal))
                text.gameObject.name += " [Responsive 062]";
        }

        private static void ConfigureAuthoredCompactText076(
            Text text,
            int minimumFontSize,
            int maximumFontSize)
        {
            if (text == null) return;
            ConfigureResponsiveText062(text, minimumFontSize, maximumFontSize);
            text.resizeTextMinSize = Mathf.Max(12, minimumFontSize);
            text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, maximumFontSize);
            text.fontSize = text.resizeTextMaxSize;
            if (text.gameObject.name.IndexOf(
                    "[Authored Compact 076]",
                    StringComparison.Ordinal) < 0)
                text.gameObject.name += " [Authored Compact 076]";
        }

        private void ApplyTextScale()
        {
            if (_screenRoot != null) ApplyTextScale(_screenRoot);
        }

        private void ApplyTextScale(Transform root)
        {
            // Battle screens use a deliberately tiered, fixed-screen typography system.
            // Applying the page-oriented 32 px floor here flattened headings, body copy,
            // resource labels, and metadata into one size and made the command camera
            // wrap into adjacent regions on narrower Game views.
            var minimum = _screen == M1Screen.Battle || _screen == M1Screen.BattleResults
                ? 14
                : 32;
            foreach (var text in root.GetComponentsInChildren<Text>(includeInactive: false))
            {
                // The persistent arena owns fixed actor and HP-bar label bounds.
                // Reapplying page scale on return multiplied their best-fit floors
                // until even their smallest glyphs no longer fit those bounds.
                // Preserve the arena's authored typography; the command/header
                // layouts continue to handle their own accessible presentation.
                if ((_screen == M1Screen.Battle || _screen == M1Screen.BattleResults) &&
                    IsAuthoredBattleDioramaText164(text.transform))
                    continue;

                // Four reserve identities share one fixed row with portraits and role
                // crests. Their authored 18-22 px contract is already the accessible
                // compact treatment; the page-wide text scale must not inflate it
                // back over either neighboring visual.
                if (text.gameObject.name.IndexOf(
                        "[Readable 079]",
                        StringComparison.Ordinal) >= 0)
                    continue;

                // Fixed roster cards deliberately trade headline scale for complete
                // names and class information. Keep the page-level accessibility
                // floor everywhere else, but do not inflate these compact labels
                // back over their portrait-safe bounds after the screen is built.
                if (_textScale > 1f && text.name == "Divider Label" &&
                    text.transform.parent != null &&
                    text.transform.parent.name.StartsWith("Premium Divider ", StringComparison.Ordinal))
                {
                    // Existing divider text occupies only the center32% of a
                    // fixed62-unit strip. Fit it; never discard the heading.
                    ConfigureAuthoredCompactText076(text, 32, RuntimeUi.SmallBodyFontPixels);
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                }
                var authoredCompact076 = text.gameObject.name.IndexOf(
                    "[Authored Compact 076]",
                    StringComparison.Ordinal) >= 0;
                var textMinimum = authoredCompact076
                    ? Mathf.Max(12, text.resizeTextMinSize)
                    : text.gameObject.name.IndexOf(
                        "[Compact Fixed 074]",
                        StringComparison.Ordinal) >= 0
                        ? UnionPlannerReserveLabelMinimumFontSize074
                        : minimum;
                if (_textScale > 1f && _screen != M1Screen.Battle && _screen != M1Screen.BattleResults &&
                    text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null &&
                    (text.name == "Label" || text.name.StartsWith("Label [", StringComparison.Ordinal)))
                {
                    // Standard fixed menu buttons keep their full action label.
                    // Prefer the requested size, fitting down only when needed.
                    var authoredMax = text.resizeTextForBestFit ? text.resizeTextMaxSize : text.fontSize;
                    var authoredMin = text.resizeTextForBestFit ? Mathf.Min(textMinimum, text.resizeTextMinSize) : textMinimum;
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = Mathf.Max(18, authoredMin);
                    text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, Mathf.RoundToInt(authoredMax * _textScale));
                    text.fontSize = text.resizeTextMaxSize;
                    text.horizontalOverflow = HorizontalWrapMode.Wrap;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    continue;
                }
                text.fontSize = Mathf.Max(textMinimum, Mathf.RoundToInt(text.fontSize * _textScale));
                if (!text.resizeTextForBestFit) continue;
                // Compact authored labels still prefer the larger accessibility size,
                // but their best-fit floor remains at the authored readable minimum.
                // This prevents a five-line identity ribbon from truncating at 145%.
                text.resizeTextMinSize = authoredCompact076
                    ? textMinimum
                    : Mathf.Max(textMinimum,
                        Mathf.RoundToInt(text.resizeTextMinSize * _textScale));
                text.resizeTextMaxSize = Mathf.Max(text.fontSize,
                    Mathf.RoundToInt(text.resizeTextMaxSize * _textScale));
            }
        }

        private static bool IsAuthoredBattleDioramaText164(Transform current)
        {
            for (; current != null; current = current.parent)
                if (StringComparer.Ordinal.Equals(current.name, "Authored Battle Diorama Layer 072"))
                    return true;
            return false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal sealed class ConfirmationCancelHandler077 : MonoBehaviour, ICancelHandler
    {
        public Action Cancel { get; set; }

        public void OnCancel(BaseEventData eventData)
        {
            eventData?.Use();
            Cancel?.Invoke();
        }
    }
}
