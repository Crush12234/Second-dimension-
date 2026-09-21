using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SecondDimension.Presentation.GuildCity017D;
using SecondDimension.Presentation.FirstHour071;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.FirstHour072
{
    /// <summary>
    /// Single owner of the rebuilt opening route. This component never opens a legacy
    /// dashboard and never submits a gameplay command before the matching player action.
    /// The Market and Hall remain world hosts; focused overlays own the authored choices.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FirstHourExperienceRoot : MonoBehaviour
    {
        public const string PlayerReadyMarkerName072 =
            "PLAYER_READY_FIRST_HOUR_REBUILD_072.txt";
        public const string RouteSaveFileName072 =
            "second_dimension_first_hour_route_072.json";

        private const string TitleArtResource072 =
            "SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071";
        private const string KiriPortraitId072 = "CANON_KIRI_AETHERHEART";
        private const string MarenPortraitId072 = "SIGREC_MAREN_HOLT";

        private static readonly DialogueBeat072[] CharterDialogue072 =
        {
            new DialogueBeat072(
                KiriPortraitId072,
                "Kiri Aetherheart",
                "Skyhome is where broken worlds agreed to build something together. Today, that agreement needs defenders."),
            new DialogueBeat072(
                MarenPortraitId072,
                "Maren Holt",
                "Give us a charter and a clear duty. We will carry our share."),
            new DialogueBeat072(
                KiriPortraitId072,
                "Kiri Aetherheart",
                "The Founders guard the gates. Your guild protects whoever falls between them. Sign, and meet the six people waiting for you.")
        };

        private IM1PresentationCoordinator _coordinator;
        private IFirstHourOpeningCoordinator072 _openingCoordinator;
        private FirstHourOpeningStateStore072 _stateStore;
        private FirstHourOpeningState072 _state;
        private WalkableSkyhomeArrival071 _market;
        private WalkableGuildHall069 _hall;
        private M1FlowPresenter _battlePresenter;
        private IFirstHourBattleExperience072 _battleExperience;
        private Canvas _routeCanvas;
        private string _guildmasterDraft = string.Empty;
        private string _status = string.Empty;
        private bool _statusPositive;
        private bool _playerReadyMarkerWrittenThisSession;
        private Coroutine _playerReadyMarkerRoutine;
        private readonly List<Sprite> _ownedSprites = new List<Sprite>();

        [Serializable]
        private sealed class PlayerReadyContract072
        {
            public string schema;
            public string buildId;
            public string stage;
            public string checkpoint;
            public string utc;
        }

        /// <summary>
        /// Fallback integration seam for a build where the dedicated battle presenter
        /// is intentionally absent. It requests a real already-started battle and never
        /// marks it complete. A host must call NotifyFirstHourBattleCompleted072 only
        /// after its result Continue action has successfully claimed rewards.
        /// </summary>
        public event Action<IM2PresentationCoordinator> FirstHourBattleRequested072;

        public FirstHourOpeningPhase072 CurrentPhase072 =>
            _state?.Phase ?? FirstHourOpeningPhase072.Title;
        public string CurrentCheckpointId072 => _state?.LastCheckpointId ?? string.Empty;
        public string BuildId072 => FirstHourOpeningState072.BuildId;
        public string RouteSavePath072 => _stateStore?.PrimaryPath ?? string.Empty;
        public bool HasLegacyDashboard072 => false;

        private void Start()
        {
            RuntimeUi.EnsureEventSystem();
            _stateStore = new FirstHourOpeningStateStore072(Path.Combine(
                SecondDimension.Presentation.Boot.AlphaProfile132.SessionDirectory132 ?? Application.persistentDataPath,
                RouteSaveFileName072));
            var loaded = _stateStore.Load();
            _state = loaded.State;
            if (!string.IsNullOrWhiteSpace(loaded.Diagnostic))
            {
                _status = loaded.Diagnostic;
                _statusPositive = loaded.RecoveredFromBackup;
            }

            if (!M1PresentationCoordinatorRegistry.TryCreate(out _coordinator) ||
                _coordinator == null)
            {
                _status = "The Guild record could not be opened. Your existing progress was not changed.";
                _statusPositive = false;
                BuildTitle072(canStart: false);
                return;
            }

            _openingCoordinator = _coordinator as IFirstHourOpeningCoordinator072;
            if (_openingCoordinator == null)
            {
                _status = "The Guild record is temporarily unavailable. Your existing progress was not changed.";
                _statusPositive = false;
                BuildTitle072(canStart: false);
                return;
            }

            ReconcileRouteWithCampaign072();
            BuildTitle072(canStart: true);
        }

        private void OnDestroy()
        {
            if (_playerReadyMarkerRoutine != null)
                StopCoroutine(_playerReadyMarkerRoutine);
            CloseBattleExperience072();
            CloseMarket072();
            CloseHall072();
            DestroyRouteCanvas072();
            for (var index = 0; index < _ownedSprites.Count; index++)
                if (_ownedSprites[index] != null) Destroy(_ownedSprites[index]);
            _ownedSprites.Clear();
        }

        private void ReconcileRouteWithCampaign072()
        {
            var campaignGuid = _openingCoordinator.FirstHourCampaignGuid072;
            if (!string.IsNullOrWhiteSpace(_state.CampaignGuid) &&
                !StringComparer.Ordinal.Equals(_state.CampaignGuid, campaignGuid))
            {
                PersistState072(FirstHourOpeningState072.New());
                _status = "This journey belongs to a different Guild. Begin again at Skyhome Market.";
                _statusPositive = false;
                return;
            }

            if (_state.Phase >= FirstHourOpeningPhase072.FounderIntroductions &&
                string.IsNullOrWhiteSpace(campaignGuid))
            {
                PersistState072(FirstHourOpeningState072.New());
                _status = "The Guild record was incomplete, so the journey returned safely to Skyhome.";
                _statusPositive = false;
                return;
            }

            var battle = _coordinator.State.Battle;
            if (_state.Phase == FirstHourOpeningPhase072.HallBreachReady &&
                battle != null && StringComparer.Ordinal.Equals(
                    battle.BattleId,
                    FirstHourOpeningState072.HallBreachBattleId))
            {
                PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachInProgress,
                    "FH072_CP_08_HALL_BREACH_ACTIVE"));
            }
            if (_state.Phase == FirstHourOpeningPhase072.HallBreachInProgress &&
                IsClaimedHallBreachVictory072(battle))
            {
                PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachRewarded,
                    "FH072_CP_09_HALL_BREACH_REWARDED"));
            }
        }

        private void BuildTitle072(bool canStart)
        {
            CloseBattleExperience072();
            CloseMarket072();
            CloseHall072();
            DestroyRouteCanvas072();

            _routeCanvas = RuntimeUi.CreateCanvas("First Hour Rebuild 072 Title");
            _routeCanvas.overrideSorting = true;
            _routeCanvas.sortingOrder = 400;
            var background = RuntimeUi.AddPanel(
                _routeCanvas.transform,
                "First Hour Title Artwork 072",
                new Color(0.025f, 0.05f, 0.09f, 1f));
            Stretch072(background.rectTransform);
            var art = LoadResourceSprite072(TitleArtResource072);
            if (art != null)
            {
                background.sprite = art;
                background.preserveAspect = false;
                background.color = Color.white;
            }

            var veil = RuntimeUi.AddPanel(
                background.transform,
                "First Hour Title Readability 072",
                new Color(0.005f, 0.012f, 0.025f, 0.58f));
            Stretch072(veil.rectTransform);
            var safe = RuntimeUi.AddSafeArea(veil.transform);

            AddAnchoredText072(
                safe,
                "Second Dimension Title 072",
                "SECOND DIMENSION",
                82,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold,
                new Vector2(0.10f, 0.67f),
                new Vector2(0.90f, 0.82f));
            AddAnchoredText072(
                safe,
                "Guild Of Worlds Subtitle 072",
                "GUILD OF WORLDS",
                40,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold,
                new Vector2(0.12f, 0.60f),
                new Vector2(0.88f, 0.68f));
            AddAnchoredText072(
                safe,
                "Chapter Title 072",
                "CHAPTER 1  •  THE BELL BENEATH SKYHOME",
                28,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold,
                new Vector2(0.14f, 0.53f),
                new Vector2(0.86f, 0.60f));

            var playAction = canStart ? (Action)ContinueFromTitle072 : null;
            var play = RuntimeUi.AddButton(
                safe,
                "First Hour Single Start 072",
                _state != null && _state.Phase != FirstHourOpeningPhase072.Title
                    ? "CONTINUE"
                    : "START NEW GUILD",
                playAction,
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            Anchor072(play.GetComponent<RectTransform>(),
                new Vector2(0.31f, 0.33f), new Vector2(0.69f, 0.46f));
            play.interactable = canStart;

            if (!string.IsNullOrWhiteSpace(_status))
            {
                AddAnchoredText072(
                    safe,
                    "First Hour Title Status 072",
                    _status,
                    23,
                    TextAnchor.MiddleCenter,
                    _statusPositive ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold,
                    new Vector2(0.18f, 0.21f),
                    new Vector2(0.82f, 0.31f));
            }

            SchedulePlayerReadyMarker072();
        }

        private void ContinueFromTitle072()
        {
            _status = string.Empty;
            if (_state.Phase == FirstHourOpeningPhase072.Title)
            {
                if (!PersistState072(_state.AdvanceTo(
                        FirstHourOpeningPhase072.MarketArrival,
                        "FH072_CP_01_MARKET_CONTROL")))
                {
                    BuildTitle072(canStart: true);
                    return;
                }
                EnterMarket072();
                return;
            }

            if (_state.Phase == FirstHourOpeningPhase072.MarketArrival)
            {
                EnterMarket072();
                return;
            }
            if (_state.Phase == FirstHourOpeningPhase072.HallBreachInProgress)
            {
                if (_coordinator.State.Battle == null ||
                    IsClaimedNonVictory072(_coordinator.State.Battle))
                {
                    EnterHall072(showCurrentOverlay: false);
                    ShowHallBreachRetry072();
                }
                else EnterBattleExperience072();
                return;
            }
            EnterHall072(showCurrentOverlay: true);
        }

        private void EnterMarket072()
        {
            CloseBattleExperience072();
            CloseHall072();
            CloseMarket072();
            DestroyRouteCanvas072();
            try
            {
                _market = gameObject.AddComponent<WalkableSkyhomeArrival071>();
                _market.Begin071(ReachGuildHall072, () => BuildTitle072(canStart: true));
                SchedulePlayerReadyMarker072();
            }
            catch (Exception exception)
            {
                CloseMarket072();
                ShowBlockingOverlay072(
                    "SKYHOME MARKET COULD NOT OPEN",
                    "Your progress is safe at the Market entrance. Try entering Skyhome again.",
                    EnterMarket072);
                Debug.LogException(exception, this);
            }
        }

        private void ReachGuildHall072()
        {
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.CharterDialogue,
                    "FH072_CP_02_HALL_REACHED")))
            {
                BuildTitle072(canStart: true);
                return;
            }
            CloseMarket072();
            EnterHall072(showCurrentOverlay: true);
        }

        private void EnterHall072(bool showCurrentOverlay)
        {
            CloseBattleExperience072();
            CloseMarket072();
            DestroyRouteCanvas072();
            if (_hall == null || !_hall.IsActive069)
            {
                CloseHall072();
                try
                {
                    _hall = gameObject.AddComponent<WalkableGuildHall069>();
                    _hall.Begin069(BuildHallCallbacks072(), ResolveHallObjective072);
                    _hall.SetProgressionSummaryProvider073(() =>
                    {
                        var progression = _coordinator?.State;
                        if (progression == null) return "GUILD XP  —  •  SPENDABLE XP  —";
                        return "GUILD LV " + Math.Max(1, progression.GuildLevel) +
                               "  •  XP " + progression.GuildXpIntoCurrentLevel.ToString("N0") +
                               " / " + progression.GuildXpRequiredForNextLevel.ToString("N0") +
                               "  •  XP TO SPEND " + progression.TreasuryXp.ToString("N0");
                    });
                    RemoveLegacyHallShortcuts072();
                    SchedulePlayerReadyMarker072();
                }
                catch (Exception exception)
                {
                    CloseHall072();
                    ShowBlockingOverlay072(
                        "GUILD HALL COULD NOT OPEN",
                        "Your progress is safe outside the Hall. Try opening its doors again.",
                        () => EnterHall072(showCurrentOverlay: true));
                    Debug.LogException(exception, this);
                    return;
                }
            }
            else
            {
                _hall.Refresh069();
                _hall.ResumeInput069();
            }

            if (showCurrentOverlay) ShowCurrentPhaseOverlay072();
        }

        private GuildHallDestinationCallbacks069 BuildHallCallbacks072() =>
            new GuildHallDestinationCallbacks069
            {
                Guide = OpenGuideStation072,
                Recruitment = OpenRecruitmentStation072,
                Armory = OpenArmoryStation072,
                Party = OpenPartyStation072,
                Contract = OpenContractStation072,
                Practice = () => ShowLockedStation072(
                    "TRAINING YARD",
                    "The Hall Breach is real. Training opens after the first rescue order."),
                QuickPlay = null,
                Exit = null,
                Leave = null
            };

        private GuildHallObjective069 ResolveHallObjective072()
        {
            switch (_state.Phase)
            {
                case FirstHourOpeningPhase072.CharterDialogue:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.GuideDestinationId069,
                        "Meet Kiri and Maren at the charter desk.");
                case FirstHourOpeningPhase072.FounderIntroductions:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.RecruitmentDestinationId069,
                        "Meet each of the six founding companions and ask them to join.");
                case FirstHourOpeningPhase072.LoadoutReview:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.ArmoryDestinationId069,
                        "Review the six starter loadouts before forming Unions.");
                case FirstHourOpeningPhase072.UnionSetup:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.PartyDestinationId069,
                        "Form two three-person Unions and choose each leader and formation.");
                case FirstHourOpeningPhase072.HallBreachReady:
                case FirstHourOpeningPhase072.HallBreachInProgress:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.ContractDestinationId069,
                        "The bell has opened a breach below the Hall. Join Kael now.");
                case FirstHourOpeningPhase072.HallBreachRewarded:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.GuideDestinationId069,
                        "Report the Hall Breach victory to Kiri and Maren.");
                case FirstHourOpeningPhase072.LanternRoadHook:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.ContractDestinationId069,
                        "The next order points to Lantern Road. Kiri is gathering reinforcements before departure.");
                default:
                    return new GuildHallObjective069(
                        WalkableGuildHall069.GuideDestinationId069,
                        "Follow the gold marker to continue Chapter 1.");
            }
        }

        private void OpenGuideStation072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.CharterDialogue)
                ShowCharterDialogue072();
            else if (_state.Phase == FirstHourOpeningPhase072.HallBreachRewarded)
                ShowHallBreachAftermath072();
            else if (_state.Phase == FirstHourOpeningPhase072.LanternRoadHook)
                ShowLanternRoadHook072();
            else
                ShowLockedStation072("KIRI'S DESK", ResolveHallObjective072().Description);
        }

        private void OpenRecruitmentStation072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.FounderIntroductions)
                ShowFounderIntroduction072();
            else
                ShowLockedStation072(
                    "RECRUITMENT DESK",
                    "The founding roster is fixed for this opening. New applicants arrive after Lantern Road.");
        }

        private void OpenArmoryStation072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.LoadoutReview)
                ShowLoadoutReview072();
            else
                ShowLockedStation072("ARMORY", "The six founder loadouts are saved and ready.");
        }

        private void OpenPartyStation072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.UnionSetup)
                ShowUnionSetup072();
            else
                ShowLockedStation072("UNION TABLE", "Your current founding Union plan is saved.");
        }

        private void OpenContractStation072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.HallBreachReady ||
                _state.Phase == FirstHourOpeningPhase072.HallBreachInProgress)
                ShowHallBreachEntry072();
            else if (_state.Phase == FirstHourOpeningPhase072.LanternRoadHook)
                ShowLanternRoadHook072();
            else
                ShowLockedStation072("CONTRACT BOARD", ResolveHallObjective072().Description);
        }

        private void ShowCurrentPhaseOverlay072()
        {
            switch (_state.Phase)
            {
                case FirstHourOpeningPhase072.CharterDialogue:
                    ShowCharterDialogue072();
                    break;
                case FirstHourOpeningPhase072.FounderIntroductions:
                    ReconcileAcceptedFounders072();
                    if (_state.Phase == FirstHourOpeningPhase072.LoadoutReview)
                        ShowLoadoutReview072();
                    else
                        ShowFounderIntroduction072();
                    break;
                case FirstHourOpeningPhase072.LoadoutReview:
                    ShowLoadoutReview072();
                    break;
                case FirstHourOpeningPhase072.UnionSetup:
                    ShowUnionSetup072();
                    break;
                case FirstHourOpeningPhase072.HallBreachRewarded:
                    ShowHallBreachAftermath072();
                    break;
                case FirstHourOpeningPhase072.LanternRoadHook:
                    ShowLanternRoadHook072();
                    break;
                default:
                    _hall?.ResumeInput069();
                    break;
            }
        }

        private void ShowCharterDialogue072()
        {
            var beatIndex = Mathf.Clamp(
                _state.CharterDialogueBeatIndex,
                0,
                FirstHourOpeningState072.CharterDialogueBeatCount);
            if (beatIndex < CharterDialogue072.Length)
            {
                var beat = CharterDialogue072[beatIndex];
                var panel = BeginCompactOverlay072(
                    "SKYHOME GUILD HALL  •  EMERGENCY CHARTER",
                    beat.Speaker,
                    beat.Line);
                AddPortrait072(panel, beat.PortraitId, beat.Speaker);
                AddPrimaryButton072(panel, "CONTINUE", () =>
                {
                    if (!PersistState072(_state.WithDialogueBeat(beatIndex + 1)))
                    {
                        ShowCharterDialogue072();
                        return;
                    }
                    ShowCharterDialogue072();
                });
                return;
            }

            if (_coordinator.State.HasCampaign)
            {
                var existingName = (_coordinator.State.GuildmasterName ?? string.Empty).Trim();
                var recoveryPanel = BeginCompactOverlay072(
                    "EMERGENCY CHARTER",
                    "The charter bears your seal",
                    string.IsNullOrWhiteSpace(existingName)
                        ? "Skyhome has your charter. Return to the six companions waiting at the recruitment desk."
                        : existingName + ", Skyhome has your charter. Return to the six companions waiting at the recruitment desk.");
                AddPrimaryButton072(
                    recoveryPanel,
                    "CONTINUE TO THE FOUNDERS",
                    ResumeExistingCharter072);
                return;
            }

            var signingPanel = BeginCompactOverlay072(
                "EMERGENCY CHARTER",
                "Name the Guildmaster",
                "This name will be carried in every Guild record. Signing establishes your command; each founder will still decide whether to stand with you.");
            var nameField = RuntimeUi.AddInputField(
                signingPanel,
                "First Hour Guildmaster Name 072",
                "ENTER GUILDMASTER NAME",
                28);
            RuntimeUi.SetLayout(nameField, preferredHeight: 138f);
            _guildmasterDraft = string.IsNullOrWhiteSpace(_guildmasterDraft)
                ? _state.GuildmasterName
                : _guildmasterDraft;
            nameField.text = _guildmasterDraft;
            nameField.onValueChanged.AddListener(value => _guildmasterDraft = value ?? string.Empty);
            AddPrimaryButton072(signingPanel, "SIGN THE EMERGENCY CHARTER", SignCharter072);
        }

        private void SignCharter072()
        {
            if (_coordinator.State.HasCampaign)
            {
                ResumeExistingCharter072();
                return;
            }

            var name = (_guildmasterDraft ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                _status = "Enter the Guildmaster name before signing.";
                _statusPositive = false;
                ShowCharterDialogue072();
                return;
            }

            var result = _coordinator.CreateGuild(new M1NewGuildIntent
            {
                GuildmasterName = name,
                ModeId = "Standard",
                TutorialDepthId = "Contextual Tips",
                TextScale = 1f,
                HighContrast = false,
                ReducedMotion = false
            });
            if (result == null || !result.Succeeded)
            {
                _status = result?.Message ?? "The charter could not be saved.";
                _statusPositive = false;
                ShowCharterDialogue072();
                return;
            }

            if (!CommitCharterRoute072(name))
            {
                ShowBlockingOverlay072(
                    "THE CHARTER COULD NOT BE RECORDED",
                    "Your Guild record is safe. Try signing once more.",
                    SignCharter072);
                return;
            }
            _status = "Charter signed. Meet each founder before anyone joins.";
            _statusPositive = true;
            _hall?.Refresh069();
            DestroyRouteCanvas072();
            _hall?.ResumeInput069();
        }

        private void ResumeExistingCharter072()
        {
            var name = (_coordinator.State.GuildmasterName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "Guildmaster";
            if (!CommitCharterRoute072(name))
            {
                ShowBlockingOverlay072(
                    "THE CHARTER COULD NOT BE RECORDED",
                    "Your Guild is safe. Try opening the charter once more.",
                    ResumeExistingCharter072);
                return;
            }
            _status = "Charter secured. Meet each founder before anyone joins.";
            _statusPositive = true;
            _hall?.Refresh069();
            DestroyRouteCanvas072();
            _hall?.ResumeInput069();
        }

        private bool CommitCharterRoute072(string guildmasterName)
        {
            var campaignGuid = _openingCoordinator.FirstHourCampaignGuid072;
            if (string.IsNullOrWhiteSpace(campaignGuid) ||
                _state.Phase != FirstHourOpeningPhase072.CharterDialogue)
                return false;
            var next = _state
                .WithCampaign(campaignGuid, guildmasterName)
                .AdvanceTo(
                    FirstHourOpeningPhase072.FounderIntroductions,
                    "FH072_CP_03_CHARTER_SIGNED");
            return PersistState072(next);
        }

        private void ReconcileAcceptedFounders072()
        {
            var applicants = (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(FirstHourOpeningState072.RequiredFounderCount)
                .ToArray();
            var reconciled = _state;
            foreach (var applicant in applicants)
            {
                if (applicant.IsSigned &&
                    !reconciled.AcceptedFounderIds.Contains(applicant.RecruitId, StringComparer.Ordinal))
                    reconciled = reconciled.WithAcceptedFounder(applicant.RecruitId);
            }
            if (!ReferenceEquals(reconciled, _state) && !PersistState072(reconciled)) return;
            if (_state.AllFoundersAccepted &&
                _state.Phase == FirstHourOpeningPhase072.FounderIntroductions)
            {
                if (!PersistState072(_state.AdvanceTo(
                        FirstHourOpeningPhase072.LoadoutReview,
                        "FH072_CP_04_FOUNDERS_ACCEPTED"))) return;
                _hall?.Refresh069();
            }
        }

        private void ShowFounderIntroduction072()
        {
            ReconcileAcceptedFounders072();
            if (_state.Phase == FirstHourOpeningPhase072.LoadoutReview)
            {
                ShowLoadoutReview072();
                return;
            }

            var applicants = (_coordinator.State.Applicants ?? Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(FirstHourOpeningState072.RequiredFounderCount)
                .ToArray();
            var founder = applicants.FirstOrDefault(value =>
                !_state.AcceptedFounderIds.Contains(value.RecruitId, StringComparer.Ordinal));
            if (founder == null)
            {
                ShowBlockingOverlay072(
                    "FOUNDING ROSTER UNAVAILABLE",
                    "The six charter applicants have not reached the desk. No one joined without meeting you.",
                    ShowFounderIntroduction072);
                return;
            }

            var panel = BeginCompactOverlay072(
                "FOUNDER " + (_state.AcceptedFounderIds.Count + 1) + " OF 6",
                founder.DisplayName,
                founder.ObservedClass + "  •  " + founder.RaceAndWorld + "\n\n" +
                FriendlyFounderDetail072(founder));
            AddPortrait072(panel, founder);
            AddPrimaryButton072(
                panel,
                "ACCEPT " + FirstName072(founder.DisplayName).ToUpperInvariant(),
                () => AcceptFounder072(founder.RecruitId));
        }

        private void AcceptFounder072(string recruitId)
        {
            var result = _coordinator.SignRecruit(recruitId);
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "FOUNDER COULD NOT JOIN",
                    result?.Message ?? "The founder could not join. Please try again.",
                    ShowFounderIntroduction072);
                return;
            }

            var next = _state.WithAcceptedFounder(recruitId);
            if (next.AllFoundersAccepted)
                next = next.AdvanceTo(
                    FirstHourOpeningPhase072.LoadoutReview,
                    "FH072_CP_04_FOUNDERS_ACCEPTED");
            if (!PersistState072(next))
            {
                ShowBlockingOverlay072(
                    "THE FOUNDER COULD NOT BE RECORDED",
                    "The companion is safe. Reopen the interview to confirm the Guild record.",
                    ShowFounderIntroduction072);
                return;
            }
            _hall?.Refresh069();
            if (_state.Phase == FirstHourOpeningPhase072.LoadoutReview)
            {
                _status = "All six founders joined. Meet the quartermaster at the Armory.";
                _statusPositive = true;
                DestroyRouteCanvas072();
                _hall?.ResumeInput069();
            }
            else
                ShowFounderIntroduction072();
        }

        private void ShowLoadoutReview072()
        {
            var recruits = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .Take(FirstHourOpeningState072.RequiredFounderCount)
                .ToArray();
            if (recruits.Length != FirstHourOpeningState072.RequiredFounderCount)
            {
                ShowBlockingOverlay072(
                    "STARTER LOADOUTS UNAVAILABLE",
                    "All six accepted founders must be present before the armory review can continue.",
                    ShowLoadoutReview072);
                return;
            }

            var summary = string.Join("\n", recruits.Select(value =>
                value.DisplayName + "  •  " + value.ObservedClass + "  •  " +
                FirstEquippedItemName072(value)));
            var panel = BeginCompactOverlay072(
                "ARMORY  •  STARTER LOADOUT REVIEW",
                "Six readable roles",
                summary +
                "\n\nEach weapon establishes the Arts this member can develop. The Armory remains available after the breach.");
            AddPrimaryButton072(panel, "ACCEPT THESE STARTER LOADOUTS", ConfirmLoadouts072);
        }

        private void ConfirmLoadouts072()
        {
            var result = _coordinator.CompleteEquipmentReview();
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "LOADOUT REVIEW COULD NOT SAVE",
                    result?.Message ?? "The Armory could not record the review. Please try again.",
                    ShowLoadoutReview072);
                return;
            }

            if (!PersistState072(_state
                    .ConfirmLoadouts()
                    .AdvanceTo(
                        FirstHourOpeningPhase072.UnionSetup,
                        "FH072_CP_05_LOADOUTS_CONFIRMED")))
            {
                ShowBlockingOverlay072(
                    "THE ARMORY REVIEW COULD NOT BE RECORDED",
                    "Your equipment is safe. Review the loadouts once more.",
                    ShowLoadoutReview072);
                return;
            }
            _status = "Starter roles saved. Cross the Hall to the Union table.";
            _statusPositive = true;
            _hall?.Refresh069();
            DestroyRouteCanvas072();
            _hall?.ResumeInput069();
        }

        private void ShowUnionSetup072()
        {
            var liveUnions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null && value.MemberRecruitIds.Count > 0)
                .Take(2)
                .ToArray();
            if (!_state.UnionDraftCreated && liveUnions.Length == 2 &&
                !PersistState072(_state.MarkUnionDraftCreated()))
            {
                ShowBlockingOverlay072(
                    "THE UNION DRAFT COULD NOT BE RECORDED",
                    "Both Unions are safe. Reopen the Union table to continue.",
                    ShowUnionSetup072);
                return;
            }

            if (!_state.UnionDraftCreated)
            {
                var roster = string.Join("  •  ",
                    (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .Where(value => value != null)
                    .Take(6)
                    .Select(value => value.DisplayName));
                var createPanel = BeginCompactOverlay072(
                    "UNION TABLE  •  STEP 1",
                    "Form two three-person Unions",
                    roster +
                    "\n\nDivide the six founders into two field groups. You will choose both leaders and both formations next.");
                AddPrimaryButton072(createPanel, "FORM TWO FOUNDING UNIONS", CreateUnionDrafts072);
                return;
            }

            if (liveUnions.Length != 2)
            {
                ShowBlockingOverlay072(
                    "UNION DRAFT IS INCOMPLETE",
                    "The founding plan must contain exactly two occupied Unions. Return to the table and form both groups.",
                    ShowUnionSetup072);
                return;
            }

            var pendingChoice = _state.UnionChoices.FirstOrDefault(value => !value.Confirmed);
            if (pendingChoice == null)
            {
                var finalPanel = BeginCompactOverlay072(
                    "UNION TABLE  •  FINAL CHECK",
                    "Two Unions ready",
                    UnionSummary072(liveUnions[0]) + "\n" + UnionSummary072(liveUnions[1]) +
                    "\n\nKiri will recognize these six companions as your founding company.");
                AddPrimaryButton072(finalPanel, "CONFIRM FOUNDING UNIONS", FinalizeFoundingUnions072);
                return;
            }

            var union = liveUnions[pendingChoice.UnionIndex];
            if (string.IsNullOrWhiteSpace(pendingChoice.LeaderRecruitId))
            {
                var leaderPanel = BeginCompactOverlay072(
                    "UNION " + (pendingChoice.UnionIndex + 1) + "  •  CHOOSE LEADER",
                    union.DisplayName,
                    "The leader occupies slot one and shapes the Union's forecast tendencies.");
                foreach (var memberId in union.MemberRecruitIds)
                {
                    var captured = memberId;
                    AddPrimaryButton072(
                        leaderPanel,
                        "LEAD WITH " + RecruitName072(captured).ToUpperInvariant(),
                        () => ChooseUnionLeader072(pendingChoice.UnionIndex, union.Index, captured),
                        RuntimeUi.ButtonNormal);
                }
                return;
            }

            if (string.IsNullOrWhiteSpace(pendingChoice.FormationId))
            {
                var formationPanel = BeginCompactOverlay072(
                    "UNION " + (pendingChoice.UnionIndex + 1) + "  •  CHOOSE FORMATION",
                    RecruitName072(pendingChoice.LeaderRecruitId) + " leads",
                    "Choose one starter plan for the Hall Breach. More formations are earned through play and Creator Code discoveries.");
                foreach (var formation in (_coordinator.State.Formations ?? Array.Empty<M1ChoiceView>())
                             .Where(value => value != null)
                             .Take(3))
                {
                    var captured = formation;
                    AddPrimaryButton072(
                        formationPanel,
                        captured.DisplayName.ToUpperInvariant() + "  •  " + captured.Summary,
                        () => ChooseUnionFormation072(
                            pendingChoice.UnionIndex,
                            union.Index,
                            captured.Id),
                        RuntimeUi.ButtonNormal);
                }
                return;
            }

            var confirmPanel = BeginCompactOverlay072(
                "UNION " + (pendingChoice.UnionIndex + 1) + "  •  READY",
                RecruitName072(pendingChoice.LeaderRecruitId) + " leads",
                "Formation: " + FormationName072(pendingChoice.FormationId) +
                "\n\nLock this Union, then prepare the next one.");
            AddPrimaryButton072(
                confirmPanel,
                "LOCK UNION " + (pendingChoice.UnionIndex + 1),
                () => ConfirmUnionChoice072(pendingChoice.UnionIndex));
        }

        private void CreateUnionDrafts072()
        {
            var result = _coordinator.AddUnion();
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "UNIONS COULD NOT BE FORMED",
                    result?.Message ?? "The Union table could not form both groups. Please try again.",
                    ShowUnionSetup072);
                return;
            }
            var liveUnions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Count(value => value != null && value.MemberRecruitIds.Count > 0);
            if (liveUnions != 2)
            {
                ShowBlockingOverlay072(
                    "FOUNDING UNION COUNT IS WRONG",
                    "The table does not yet show exactly two occupied founding Unions.",
                    ShowUnionSetup072);
                return;
            }
            if (!PersistState072(_state.MarkUnionDraftCreated()))
            {
                ShowBlockingOverlay072(
                    "THE UNION DRAFT COULD NOT BE RECORDED",
                    "Both Unions are safe. Reopen the Union table to continue.",
                    ShowUnionSetup072);
                return;
            }
            ShowUnionSetup072();
        }

        private void ChooseUnionLeader072(int choiceIndex, int liveUnionIndex, string recruitId)
        {
            var result = _coordinator.SetUnionLeader(liveUnionIndex, recruitId);
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "LEADER CHOICE COULD NOT SAVE",
                    result?.Message ?? "The leader choice could not be recorded. Please try again.",
                    ShowUnionSetup072);
                return;
            }
            if (!PersistState072(_state.WithUnionLeader(choiceIndex, recruitId)))
            {
                ShowBlockingOverlay072(
                    "THE LEADER CHOICE COULD NOT BE RECORDED",
                    "The Union is safe. Choose its leader once more.",
                    ShowUnionSetup072);
                return;
            }
            ShowUnionSetup072();
        }

        private void ChooseUnionFormation072(int choiceIndex, int liveUnionIndex, string formationId)
        {
            var result = _coordinator.SetFormation(liveUnionIndex, formationId);
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "FORMATION CHOICE COULD NOT SAVE",
                    result?.Message ?? "The formation could not be recorded. Please try again.",
                    ShowUnionSetup072);
                return;
            }
            if (!PersistState072(_state.WithUnionFormation(choiceIndex, formationId)))
            {
                ShowBlockingOverlay072(
                    "THE FORMATION COULD NOT BE RECORDED",
                    "The Union is safe. Choose its formation once more.",
                    ShowUnionSetup072);
                return;
            }
            ShowUnionSetup072();
        }

        private void ConfirmUnionChoice072(int choiceIndex)
        {
            if (!PersistState072(_state.ConfirmUnion(choiceIndex)))
            {
                ShowBlockingOverlay072(
                    "THE UNION COULD NOT BE LOCKED",
                    "The Union is safe. Confirm it once more.",
                    ShowUnionSetup072);
                return;
            }
            ShowUnionSetup072();
        }

        private void FinalizeFoundingUnions072()
        {
            if (!_state.AllUnionsConfirmed)
            {
                ShowUnionSetup072();
                return;
            }
            var result = _openingCoordinator.CompleteFirstHourFoundingCompany072();
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "FOUNDING PLAN COULD NOT SAVE",
                    result?.Message ?? "The founding plan could not be recorded. Please try again.",
                    ShowUnionSetup072);
                return;
            }
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachReady,
                    "FH072_CP_07_HALL_BREACH_READY")))
            {
                ShowBlockingOverlay072(
                    "THE FOUNDING PLAN COULD NOT BE RECORDED",
                    "Both Unions are safe. Confirm the founding plan once more.",
                    ShowUnionSetup072);
                return;
            }
            DestroyRouteCanvas072();
            _hall?.ResumeInput069();
            _hall?.Refresh069();
        }

        private void ShowHallBreachEntry072()
        {
            if (_state.Phase == FirstHourOpeningPhase072.HallBreachInProgress)
            {
                if (_coordinator.State.Battle == null ||
                    IsClaimedNonVictory072(_coordinator.State.Battle))
                    ShowHallBreachRetry072();
                else
                    EnterBattleExperience072();
                return;
            }
            var panel = BeginCompactOverlay072(
                "THE BELL WITHOUT A ROPE",
                "The Hall Breach",
                "Orren: That bell has no rope.\n\nKiri: Then something below is pulling it.\n\nKael is holding the stair. Your two founding Unions must defend the Hall.");
            AddPrimaryButton072(panel, "ENTER THE HALL BREACH", StartHallBreach072);
        }

        private void StartHallBreach072()
        {
            var result = _openingCoordinator.StartFirstHourHallBreach072();
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "HALL BREACH COULD NOT START",
                    result?.Message ?? "The Hall Breach could not be entered. Please try again.",
                    ShowHallBreachEntry072);
                return;
            }
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachInProgress,
                    "FH072_CP_08_HALL_BREACH_ACTIVE")))
            {
                ShowBlockingOverlay072(
                    "THE BATTLE RECORD COULD NOT BE SAVED",
                    "The Hall Breach is waiting. Enter it again after the Guild record is confirmed.",
                    ShowHallBreachEntry072);
                return;
            }
            EnterBattleExperience072();
        }

        private void EnterBattleExperience072()
        {
            var battleCoordinator = _coordinator as IM2PresentationCoordinator;
            var battle = _coordinator.State.Battle;
            if (battleCoordinator == null || battle == null ||
                !StringComparer.Ordinal.Equals(
                    battle.BattleId,
                    FirstHourOpeningState072.HallBreachBattleId))
            {
                EnterHall072(showCurrentOverlay: false);
                ShowBlockingOverlay072(
                    "HALL BREACH SAVE IS UNAVAILABLE",
                    "The Hall Breach record could not be found. Your Guild remains safe in the Hall.",
                    ShowHallBreachRetry072);
                return;
            }

            CloseHall072();
            DestroyRouteCanvas072();
            CloseBattleExperience072();

            if (typeof(IFirstHourBattleExperience072).IsAssignableFrom(typeof(M1FlowPresenter)))
            {
                var battleObject = new GameObject("First Hour Battle Experience Host 072");
                _battlePresenter = battleObject.AddComponent<M1FlowPresenter>();
                _battleExperience = _battlePresenter as IFirstHourBattleExperience072;
                _battleExperience.FirstHourBattleExperienceCompleted072 += HandleBattleCompleted072;
                _battlePresenter.Initialize(_coordinator);
                _battleExperience.EnterFirstHourBattleExperience072();
                return;
            }

            var fallback = FirstHourBattleRequested072;
            if (fallback != null)
            {
                fallback.Invoke(battleCoordinator);
                return;
            }

            EnterHall072(showCurrentOverlay: false);
            ShowBlockingOverlay072(
                "THE HALL BREACH CANNOT OPEN",
                "The battle remains unresolved. Return to the Hall and try the stair again.",
                EnterBattleExperience072);
        }

        public void NotifyFirstHourBattleCompleted072()
        {
            HandleBattleCompleted072();
        }

        private void HandleBattleCompleted072()
        {
            var battle = _coordinator.State.Battle;
            if (battle == null || !StringComparer.Ordinal.Equals(
                    battle.BattleId,
                    FirstHourOpeningState072.HallBreachBattleId))
            {
                _status = "A different battle record was returned. The Hall Breach remains open.";
                _statusPositive = false;
                CloseBattleExperience072();
                EnterHall072(showCurrentOverlay: false);
                ShowHallBreachRetry072();
                return;
            }
            if (!StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory"))
            {
                _status = "The Hall Breach is still open. No victory reward was granted.";
                _statusPositive = false;
                CloseBattleExperience072();
                EnterHall072(showCurrentOverlay: false);
                ShowHallBreachRetry072();
                return;
            }
            if (battle.Reward?.Claimed != true)
            {
                _status = "The victory reward has not been secured yet.";
                _statusPositive = false;
                return;
            }
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachRewarded,
                    "FH072_CP_09_HALL_BREACH_REWARDED")))
            {
                ShowBlockingOverlay072(
                    "THE VICTORY COULD NOT BE SAVED",
                    "The battle reward is safe. Confirm the Guild record to return to the Hall.",
                    HandleBattleCompleted072);
                return;
            }
            CloseBattleExperience072();
            EnterHall072(showCurrentOverlay: true);
        }

        private void ShowHallBreachRetry072()
        {
            var panel = BeginCompactOverlay072(
                "THE HALL BREACH REMAINS",
                "Regroup with Kael",
                "The last attempt did not close the breach, and no victory reward was granted. Catch your breath in the Hall, then return to the stair and fight the encounter again.");
            AddPrimaryButton072(panel, "REENTER THE HALL BREACH", RetryHallBreach072);
            AddPrimaryButton072(panel, "RETURN TO THE GUILD HALL", () =>
            {
                DestroyRouteCanvas072();
                _hall?.ResumeInput069();
                _hall?.Refresh069();
            }, RuntimeUi.ButtonNormal);
        }

        private void RetryHallBreach072()
        {
            var result = _openingCoordinator.StartFirstHourHallBreach072();
            if (result == null || !result.Succeeded)
            {
                ShowBlockingOverlay072(
                    "HALL BREACH RETRY COULD NOT START",
                    result?.Message ?? "The Hall Breach could not be reentered. Please try again.",
                    ShowHallBreachRetry072);
                return;
            }
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.HallBreachInProgress,
                    "FH072_CP_08_HALL_BREACH_RETRY_ACTIVE")))
            {
                ShowBlockingOverlay072(
                    "THE RETURN COULD NOT BE SAVED",
                    "The Hall Breach is waiting. Confirm the Guild record before reentering.",
                    ShowHallBreachRetry072);
                return;
            }
            EnterBattleExperience072();
        }

        public static bool IsClaimedHallBreachVictory072(M2BattleView battle) =>
            StringComparer.Ordinal.Equals(
                battle?.BattleId,
                FirstHourOpeningState072.HallBreachBattleId) &&
            battle?.Reward?.Claimed == true &&
            StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory");

        private static bool IsClaimedNonVictory072(M2BattleView battle) =>
            StringComparer.Ordinal.Equals(
                battle?.BattleId,
                FirstHourOpeningState072.HallBreachBattleId) &&
            battle?.Reward?.Claimed == true &&
            !StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory");

        private void ShowHallBreachAftermath072()
        {
            var panel = BeginCompactOverlay072(
                "HALL BREACH  •  VICTORY SAVED",
                "The hand behind the strike is still moving",
                "Kael: The strike has been returned. The hand that sent it is still moving.\n\nKiri: A Lantern Road patrol vanished with a Wayglass. The breach and their disappearance are connected.\n\nMaren: Give us the road order.");
            AddPrimaryButton072(panel, "HEAR THE LANTERN ROAD ORDER", AcceptLanternRoadHook072);
        }

        private void AcceptLanternRoadHook072()
        {
            if (!PersistState072(_state.AdvanceTo(
                    FirstHourOpeningPhase072.LanternRoadHook,
                    "FH072_CP_10_LANTERN_ROAD_ORDER_HEARD")))
            {
                ShowHallBreachAftermath072();
                return;
            }
            _hall?.Refresh069();
            ShowLanternRoadHook072();
        }

        private void ShowLanternRoadHook072()
        {
            var panel = BeginCompactOverlay072(
                "NEXT CONTRACT REVEALED",
                "Lantern Road",
                "The six founders survived their first battle. A missing patrol, its Wayglass, and the old Gatehouse now form one clear next objective.\n\nThe Hall is preparing the reinforcements needed for the road. Your Guild, two Unions, and battle reward are safe.");
            AddPrimaryButton072(panel, "RETURN TO THE GUILD HALL", () =>
            {
                DestroyRouteCanvas072();
                _hall?.ResumeInput069();
                _hall?.Refresh069();
            });
        }

        private void ShowLockedStation072(string title, string message)
        {
            var panel = BeginCompactOverlay072("SKYHOME GUILD HALL", title, message);
            AddPrimaryButton072(panel, "RETURN TO HALL", () =>
            {
                DestroyRouteCanvas072();
                _hall?.ResumeInput069();
            }, RuntimeUi.ButtonNormal);
        }

        private void ShowBlockingOverlay072(string title, string message, Action retry)
        {
            var panel = BeginCompactOverlay072("YOUR GUILD RECORD IS SAFE", title, message);
            AddPrimaryButton072(panel, "TRY AGAIN", retry, RuntimeUi.Warning);
            AddPrimaryButton072(panel, "RETURN TO TITLE", () => BuildTitle072(canStart: true), RuntimeUi.ButtonNormal);
        }

        private Transform BeginCompactOverlay072(string eyebrow, string title, string body)
        {
            DestroyRouteCanvas072();
            _hall?.SuspendInput069();
            _routeCanvas = RuntimeUi.CreateCanvas("First Hour Focused Story Overlay 072");
            _routeCanvas.overrideSorting = true;
            _routeCanvas.sortingOrder = 500;
            var scrim = RuntimeUi.AddPanel(
                _routeCanvas.transform,
                "First Hour Overlay Scrim 072",
                new Color(0.003f, 0.008f, 0.018f, 0.72f));
            Stretch072(scrim.rectTransform);
            var safe = RuntimeUi.AddSafeArea(scrim.transform);
            var panel = RuntimeUi.AddPanel(
                safe,
                "First Hour Focused Story Card 072",
                new Color(0.025f, 0.045f, 0.075f, 0.98f));
            Anchor072(panel.rectTransform, new Vector2(0.14f, 0.07f), new Vector2(0.86f, 0.93f));
            RuntimeUi.AddVerticalLayout(
                panel.transform,
                new RectOffset(54, 54, 42, 42),
                16f,
                TextAnchor.UpperCenter);

            var eyebrowText = RuntimeUi.AddText(
                panel.transform,
                "First Hour Story Eyebrow 072",
                eyebrow,
                22,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(eyebrowText, preferredHeight: 50f);
            var titleText = RuntimeUi.AddText(
                panel.transform,
                "First Hour Story Title 072",
                title,
                46,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(titleText, preferredHeight: 82f);
            var bodyText = RuntimeUi.AddText(
                panel.transform,
                "First Hour Story Body 072",
                body,
                27,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Normal);
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            RuntimeUi.SetLayout(bodyText, preferredHeight: 215f, flexibleHeight: 1f);
            if (!string.IsNullOrWhiteSpace(_status))
            {
                var statusText = RuntimeUi.AddText(
                    panel.transform,
                    "First Hour Story Status 072",
                    _status,
                    21,
                    TextAnchor.MiddleCenter,
                    _statusPositive ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(statusText, preferredHeight: 56f);
                _status = string.Empty;
            }
            return panel.transform;
        }

        private void AddPortrait072(Transform parent, DialogueBeat072 beat) =>
            AddPortrait072(parent, beat.PortraitId, beat.Speaker);

        private void AddPortrait072(Transform parent, string identityId, string displayName)
        {
            if (!M1VisualAssets.TryResolvePortrait(
                    identityId,
                    identityId,
                    string.Empty,
                    identityId,
                    out var sprite,
                    out _)) return;
            var portrait = RuntimeUi.AddPanel(
                parent,
                "First Hour Portrait " + displayName + " 072",
                Color.white);
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            RuntimeUi.SetLayout(portrait, preferredHeight: 230f);
        }

        private void AddPortrait072(Transform parent, M1ApplicantView applicant)
        {
            if (applicant == null) return;
            if (!M1VisualAssets.TryResolvePortrait(
                    applicant.RecruitId,
                    applicant.VisualSeed,
                    applicant.RaceId,
                    applicant.PortraitAuthorityId,
                    out var sprite,
                    out _)) return;
            var portrait = RuntimeUi.AddPanel(
                parent,
                "First Hour Founder Portrait " + applicant.DisplayName + " 072",
                Color.white);
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            RuntimeUi.SetLayout(portrait, preferredHeight: 260f);
        }

        private static Button AddPrimaryButton072(
            Transform parent,
            string label,
            Action action,
            Color? color = null)
        {
            var button = RuntimeUi.AddButton(
                parent,
                "First Hour Action " + label + " 072",
                label,
                action,
                138f,
                color ?? RuntimeUi.Accent);
            var text = button.GetComponentInChildren<Text>();
            if (text != null) text.fontSize = label.Length > 60 ? 20 : label.Length > 34 ? 24 : 30;
            return button;
        }

        private void RemoveLegacyHallShortcuts072()
        {
            var hud = _hall?.HudCanvasForVerification069;
            if (hud == null) return;
            foreach (var button in hud.GetComponentsInChildren<Button>(includeInactive: true))
                if (button != null && button.name.IndexOf("Quick Play", StringComparison.OrdinalIgnoreCase) >= 0)
                    button.gameObject.SetActive(false);
            foreach (var text in hud.GetComponentsInChildren<Text>(includeInactive: true))
            {
                if (text == null || string.IsNullOrWhiteSpace(text.text)) continue;
                text.text = text.text
                    .Replace("   •   STORY  Q / Y", string.Empty)
                    .Replace("STORY  Q / Y   •   ", string.Empty);
            }
        }

        private void SchedulePlayerReadyMarker072()
        {
            if (_playerReadyMarkerWrittenThisSession) return;
            if (_playerReadyMarkerRoutine != null) StopCoroutine(_playerReadyMarkerRoutine);
            _playerReadyMarkerRoutine = StartCoroutine(WritePlayerReadyMarkerAfterRenderedWorld072());
        }

        private IEnumerator WritePlayerReadyMarkerAfterRenderedWorld072()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            var titleReady = _routeCanvas != null && _routeCanvas.isActiveAndEnabled;
            var marketReady = _market != null && _market.IsActive071 &&
                              _market.ControlledAvatar071 != null &&
                              _market.WorldCamera071 != null && _market.WorldCamera071.enabled &&
                              _market.UsesCrossPlatformWorldInput071;
            var hallReady = _hall != null && _hall.IsActive069 &&
                            _hall.ControlledAvatar069 != null &&
                            _hall.WorldCameraForVerification069 != null &&
                            _hall.WorldCameraForVerification069.enabled &&
                            _hall.UsesCrossPlatformWorldInput071 && !_hall.InputSuspended069;
            if (!titleReady && !marketReady && !hallReady)
            {
                _playerReadyMarkerRoutine = null;
                yield break;
            }

            try
            {
                var requestedBuildId = ReadCommandLineValue072("-sdBuildId");
                if (!string.IsNullOrWhiteSpace(requestedBuildId) &&
                    !StringComparer.Ordinal.Equals(
                        requestedBuildId,
                        FirstHourOpeningState072.BuildId))
                    throw new InvalidOperationException(
                        "The launched Player build ID does not match this opening experience.");

                var requestedMarkerPath = ReadCommandLineValue072("-sdReadyMarker");
                var markerPath = string.IsNullOrWhiteSpace(requestedMarkerPath)
                    ? Path.Combine(SecondDimension.Presentation.Boot.AlphaProfile132.SessionDirectory132 ?? Application.persistentDataPath, PlayerReadyMarkerName072)
                    : Path.GetFullPath(requestedMarkerPath);
                var markerDirectory = Path.GetDirectoryName(markerPath);
                if (!string.IsNullOrWhiteSpace(markerDirectory))
                    Directory.CreateDirectory(markerDirectory);

                string marker;
                if (string.IsNullOrWhiteSpace(requestedMarkerPath))
                {
                    marker = FirstHourOpeningState072.BuildId + "\n" +
                             "PLAYER_ROUTE_RENDERED=TRUE\n" +
                             "CHECKPOINT=" + (_state?.LastCheckpointId ?? string.Empty) + "\n" +
                             "UTC=" + DateTime.UtcNow.ToString("O") + "\n";
                }
                else
                {
                    marker = JsonUtility.ToJson(new PlayerReadyContract072
                    {
                        schema = "SECOND_DIMENSION_PLAYER_READY_072_V1",
                        buildId = FirstHourOpeningState072.BuildId,
                        stage = hallReady ? "GUILD_HALL" : marketReady ? "SKYHOME_MARKET" : "TITLE",
                        checkpoint = _state?.LastCheckpointId ?? string.Empty,
                        utc = DateTime.UtcNow.ToString("O")
                    }, prettyPrint: true) + "\n";
                }
                using (var stream = new FileStream(
                           markerPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(marker);
                    writer.Flush();
                    stream.Flush(true);
                }
                _playerReadyMarkerWrittenThisSession = true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[FirstHour072] Player-ready marker could not be written: " + exception.Message);
            }
            _playerReadyMarkerRoutine = null;
        }

        private static string ReadCommandLineValue072(string key)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < arguments.Length; index++)
            {
                if (!StringComparer.Ordinal.Equals(arguments[index], key)) continue;
                return arguments[index + 1] ?? string.Empty;
            }
            return string.Empty;
        }

        private bool PersistState072(FirstHourOpeningState072 next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            try
            {
                _stateStore.Save(next);
                _state = next;
                return true;
            }
            catch (Exception exception)
            {
                _status = "Your last choice could not be saved. Your earlier Guild record is still safe.";
                _statusPositive = false;
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void CloseMarket072()
        {
            var market = _market;
            _market = null;
            if (market == null) return;
            market.Shutdown071();
            Destroy(market);
        }

        private void CloseHall072()
        {
            var hall = _hall;
            _hall = null;
            if (hall == null) return;
            hall.Shutdown069();
            Destroy(hall);
        }

        private void CloseBattleExperience072()
        {
            if (_battleExperience != null)
                _battleExperience.FirstHourBattleExperienceCompleted072 -= HandleBattleCompleted072;
            _battleExperience = null;
            var presenter = _battlePresenter;
            _battlePresenter = null;
            if (presenter == null) return;
            presenter.gameObject.SetActive(false);
            Destroy(presenter.gameObject);
        }

        private void DestroyRouteCanvas072()
        {
            var canvas = _routeCanvas;
            _routeCanvas = null;
            if (canvas == null) return;
            canvas.gameObject.SetActive(false);
            Destroy(canvas.gameObject);
        }

        private Sprite LoadResourceSprite072(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            sprite.name = texture.name + "_FIRST_HOUR072_RUNTIME";
            _ownedSprites.Add(sprite);
            return sprite;
        }

        private string RecruitName072(string recruitId)
        {
            var recruit = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault(value => value != null &&
                                         StringComparer.Ordinal.Equals(value.RecruitId, recruitId));
            return recruit?.DisplayName ?? "Guild member";
        }

        private string FormationName072(string formationId)
        {
            var formation = (_coordinator.State.Formations ?? Array.Empty<M1ChoiceView>())
                .FirstOrDefault(value => value != null &&
                                         StringComparer.Ordinal.Equals(value.Id, formationId));
            return formation?.DisplayName ?? "Chosen formation";
        }

        private string UnionSummary072(M1UnionView union)
        {
            if (union == null) return "Union unavailable";
            var members = string.Join(", ", union.MemberRecruitIds.Select(RecruitName072));
            return union.DisplayName + ": " + members + " — " +
                   RecruitName072(union.LeaderRecruitId) + " leads in " +
                   FormationName072(union.FormationId) + ".";
        }

        private static string FriendlyFounderDetail072(M1ApplicantView founder)
        {
            var observation = string.IsNullOrWhiteSpace(founder.ScoutObservations)
                ? founder.PersonalityClues
                : founder.ScoutObservations;
            if (string.IsNullOrWhiteSpace(observation))
                observation = "A permanent founding companion with a clear battlefield role.";
            return observation + "\n\nStarting gear: " +
                   (string.IsNullOrWhiteSpace(founder.GearSummary) ? "Ready for review" : founder.GearSummary);
        }

        private static string FirstEquippedItemName072(M1RecruitLoadoutView recruit)
        {
            var item = (recruit.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .FirstOrDefault(value => value != null && !string.IsNullOrWhiteSpace(value.EquippedItemName));
            return item?.EquippedItemName ?? "Starter weapon ready";
        }

        private static string FirstName072(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "FOUNDER";
            var index = displayName.IndexOf(' ');
            return index > 0 ? displayName.Substring(0, index) : displayName;
        }

        private static Text AddAnchoredText072(
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
            Anchor072(text.rectTransform, anchorMin, anchorMax);
            return text;
        }

        private static void Stretch072(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Anchor072(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class DialogueBeat072
        {
            public DialogueBeat072(string portraitId, string speaker, string line)
            {
                PortraitId = portraitId ?? string.Empty;
                Speaker = speaker ?? string.Empty;
                Line = line ?? string.Empty;
            }

            public string PortraitId { get; }
            public string Speaker { get; }
            public string Line { get; }
        }
    }
}
