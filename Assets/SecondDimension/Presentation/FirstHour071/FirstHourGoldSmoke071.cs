using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.FirstHour071;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Presentation.BoardTower001;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation.FirstHour071
{
    [Serializable]
    public sealed class FirstHourGoldSmokeReport071
    {
        public string schema = "SECOND_DIMENSION_FIRST_HOUR_GOLD_SMOKE_084_1";
        public string status = "RUNNING";
        public string buildId = string.Empty;
        public string executableSha256 = string.Empty;
        public string playerAssemblySha256 = string.Empty;
        public string resourcesSha256 = string.Empty;
        public string buildManifestSha256 = string.Empty;
        public string applicationVersion = string.Empty;
        public string unityVersion = string.Empty;
        public string platform = string.Empty;
        public string startedUtc = string.Empty;
        public string completedUtc = string.Empty;
        public string savePath = string.Empty;
        public string canonicalStateHash = string.Empty;
        public string reloadedStateHash = string.Empty;
        public string nextObjective = string.Empty;
        public string certificationFrame = string.Empty;
        public string chapterTwoRouteNodeId = string.Empty;
        public string towerSavePath = string.Empty;
        public string towerFloorId = string.Empty;
        public string towerBattleId = string.Empty;
        public string towerBattleFinalStateHash = string.Empty;
        public string towerReloadedStateHash = string.Empty;
        public string failure = string.Empty;
        public int rosterCount;
        public int activeUnionCount;
        public int claimedBattleRewardCount;
        public int totalClaimedProgressionReceiptCount;
        public int resolvedCheckCount;
        public int screenWidth;
        public int screenHeight;
        public int buildManifestEntryCount;
        public int towerEnemyUnionCount;
        public int towerBattleRounds;
        public int towerHighestClearedFloor;
        public int towerTotalClears;
        public int towerTerminalReceiptCountDelta;
        public bool buildIdentityVerified;
        public bool characterIdentityArtVerified;
        public bool personFirstTitleVerified;
        public bool saveReloadVerified;
        public bool keyboardInputVerified;
        public bool controllerInputVerified;
        public bool artBreakthroughObserved;
        public bool livingGuildHubVerified;
        public bool foundingCompanyVerified;
        public bool foundingGuildmasterPreparationVerified;
        public bool unionPlannerVerified;
        public bool unionPlannerDragDropVerified;
        public bool expeditionBoardVerified;
        public bool firstBattleCoachVerified;
        public bool battleOrdersReadiedVerified;
        public bool battleExchangeResolvedVerified;
        public bool battleHpImpactPresentationVerified;
        public bool transientLearnedArtNoticeVerified;
        public bool campaignEnemyArt700Verified;
        public bool towerEnemyArt700Verified;
        public bool chapterOneStoryContinuityVerified;
        public bool lanternPatrolCeremonyVerified;
        public bool chapterTwoOpeningVerified;
        public bool firstHallObjectiveVerified;
        public bool chapterTwoObjectiveAndEvidenceVerified;
        public bool chapterTwoCrewAssignmentsVerified;
        public bool guildCityVisualProofVerified;
        public bool chapterTwoRoutePersistenceVerified;
        public bool chapterTwoPlayableChainVerified;
        public bool directGuildEntry091Verified;
        public bool missionBriefRoundTripVerified;
        public bool postBattleFieldReturnVerified;
        public bool patrolCombatReadyVerified;
        public bool firstHourArtPresentationVerified;
        public bool liveFirstHourArtRecipeConsumptionVerified;
        public bool twoStartingTreesVerified;
        public bool tenBySixCampaignCapacityVerified;
        public bool manualLootEquippedVerified;
        public bool oneObjectivePerFieldNodeVerified;
        public bool threeCardQuestChoiceVerified;
        public bool towerFloorOneUiVerified;
        public bool towerBattleResolvedVerified;
        public bool towerRewardExactOnceVerified;
        public bool towerReloadVerified;
        public bool endlessTowerFloorOneVerified;
        public bool expeditionRecruitCardLeadSavedVerified;
        public bool expeditionRecruitLeadReloadVerified;
        public bool expeditionRecruitApplicantExactHeroVerified;
        public bool expeditionRecruitSigningExactOnceVerified;
        public bool expeditionAscensionCardEarnedVerified;
        public bool expeditionDuplicateMergeUiVerified;
        public bool expeditionDuplicateMergeExactOnceVerified;
        public string expeditionRecruitHeroStableId = string.Empty;
        public string expeditionRecruitHeroName = string.Empty;
        public string recruitAscensionSavePath = string.Empty;
        public int recruitRosterAfterSigning;
        public int recruitRosterAfterAscension;
        public int recruitAscensionBefore;
        public int recruitAscensionAfter;
        public bool renderedScreenshotValidationVerified;
        public List<string> battleIds = new List<string>();
        public List<string> screenshots = new List<string>();
        public List<string> screenshotSha256 = new List<string>();
        public List<string> portraitResourceKeys = new List<string>();
        public List<string> standeeResourceKeys = new List<string>();
        public List<string> actionResourceKeys = new List<string>();
        public List<string> gates = new List<string>();
    }

    /// <summary>
    /// Built-player-only deterministic first-hour playthrough. It uses the shipping
    /// coordinator, expedition, battle, input, presentation, save, and reload paths.
    /// </summary>
    public sealed partial class FirstHourGoldSmoke071 : MonoBehaviour
    {
        private const string ExpectedBuildId = "SECOND-DIMENSION-ALPHA-132";
        private const string ExpectedPlayerVersion = "0.132.0-alpha";
        private const string ExecutableName = "SECOND_DIMENSION_GUILD_OF_WORLDS.exe";
        private const string BuildIdFileName = "FIRST_HOUR_GOLD_BUILD_ID.txt";
        private const string BuildManifestFileName = "BUILD_SHA256.txt";
        private const string BuildCompleteFileName = "FIRST_HOUR_GOLD_BUILD_COMPLETE.txt";
        private const string SmokeFlag = "--sd-first-hour-gold-smoke";
        private const string EvidencePrefix = "--sd-first-hour-evidence-dir=";
        private const string SavePrefix = "--sd-first-hour-save-path=";
        private const string ChapterTwoSurveyorRescueEncounterId078 =
            "ENCOUNTER_SURVEYOR_RESCUE";
        private const string SmokeChapterBoardId084 = "CH018_032";
        private const string SmokeLongestRepeatableBoardId084 =
            "REPEAT020_SKYHOME_04";
        private const int ObjectiveTraversalMaximumSteps076 = 720;
        private const int ObjectiveTraversalStallLimit076 = 120;
        private const int VisibleBattleForecastLimit076 = 5;
        public const string TransientLearnedArtReadyPhrase078 =
            "READY FOR FUTURE BATTLES";
        private const float ObjectiveTraversalStepSeconds076 = 1f / 30f;
        // Simulate070 retains input until the motor's next Update, whose safe tick is
        // capped at 0.1 s. Include that legitimate low-FPS step plus a small lane/
        // grounding allowance; both bounds remain below a multi-metre teleport.
        private const float ObjectiveTraversalRetainedTickSeconds076 = 0.1f;
        private const float ObjectiveTraversalPositionTolerance076 = 0.2f;
        private const float MarketTraversalMaximumSpeed076 = 8.4f;
        private const float FieldTraversalMaximumSpeed076 = 5.4f;
        private const float MarketTraversalMaximumStep076 =
            MarketTraversalMaximumSpeed076 *
            (ObjectiveTraversalStepSeconds076 + ObjectiveTraversalRetainedTickSeconds076) +
            ObjectiveTraversalPositionTolerance076;
        private const float FieldTraversalMaximumStep076 =
            FieldTraversalMaximumSpeed076 *
            (ObjectiveTraversalStepSeconds076 + ObjectiveTraversalRetainedTickSeconds076) +
            ObjectiveTraversalPositionTolerance076;
        private const float ObjectiveTraversalProgressEpsilon076 = 0.01f;
        private const float ObjectiveTraversalTimeoutSeconds076 = 20f;
        private static bool _created;
        private FirstHourGoldSmokeReport071 _report;
        private string _evidenceRoot;
        private string _screenshotRoot;
        private string _reportPath;
        private string _savePath;
        private M1RuntimeCoordinator _coordinator;
        private M1FlowPresenter _presenter;
        private int _screenshotOrdinal;
        private int _claimedProgressionReceiptBaseline071;
        private readonly HashSet<string> _screenshotHashes071 =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _mandatoryBattleRoundCounts078 =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private bool _authoritativeBattleExchangeObserved078;

        public static bool IsSmokeRequested078(IReadOnlyList<string> arguments)
        {
            return (arguments ?? Array.Empty<string>()).Any(value =>
                StringComparer.Ordinal.Equals(value, SmokeFlag));
        }

        public static bool TryResolveIsolatedPaths078(
            IReadOnlyList<string> arguments,
            string persistentDataPath,
            out string evidenceRoot,
            out string savePath,
            out string error)
        {
            evidenceRoot = string.Empty;
            savePath = string.Empty;
            error = string.Empty;
            try
            {
                if (string.IsNullOrWhiteSpace(persistentDataPath))
                {
                    error = "Persistent data path is required for First Hour smoke isolation.";
                    return false;
                }

                var persistentRoot = Path.GetFullPath(persistentDataPath);
                var requestedEvidence = ReadValue071(arguments, EvidencePrefix);
                if (string.IsNullOrWhiteSpace(requestedEvidence))
                    requestedEvidence = Path.Combine(
                        Path.GetTempPath(),
                        "SECOND_DIMENSION_FIRST_HOUR_SMOKE_EVIDENCE",
                        System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                evidenceRoot = Path.GetFullPath(requestedEvidence);
                if (PathsOverlap071(evidenceRoot, persistentRoot))
                {
                    error = "Personal-data protection: First Hour smoke evidence must not " +
                            "overlap Application.persistentDataPath in either direction.";
                    return false;
                }

                var requestedSave = ReadValue071(arguments, SavePrefix);
                if (string.IsNullOrWhiteSpace(requestedSave))
                    requestedSave = Path.Combine(
                        evidenceRoot,
                        "first_hour_gold_smoke_save.json");
                savePath = Path.GetFullPath(requestedSave);
                if (PathsOverlap071(savePath, persistentRoot))
                {
                    error = "Personal-data protection: First Hour smoke save must not " +
                            "overlap Application.persistentDataPath in either direction.";
                    return false;
                }

                var saveDirectory = Path.GetDirectoryName(savePath);
                if (string.IsNullOrWhiteSpace(saveDirectory) ||
                    !IsSameOrChild071(saveDirectory, evidenceRoot))
                {
                    error = "First Hour smoke save must be a file inside its evidence directory.";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                error = "First Hour smoke paths are invalid: " + exception.Message;
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateWhenRequested071()
        {
            if (_created || !IsSmokeRequested078(Environment.GetCommandLineArgs())) return;
            _created = true;
            Application.runInBackground = true;
            var host = new GameObject("First Hour Gold Built Player Smoke 071");
            DontDestroyOnLoad(host);
            host.AddComponent<FirstHourGoldSmoke071>();
        }

        private IEnumerator Start()
        {
            var started = DateTime.UtcNow;
            Exception failure = null;
            yield return RunGuarded071(Execute071(), exception => failure = exception);

            if (failure != null)
            {
                if (_report == null)
                {
                    _report = new FirstHourGoldSmokeReport071
                    {
                        applicationVersion = Application.version,
                        unityVersion = Application.unityVersion,
                        platform = Application.platform.ToString(),
                        startedUtc = started.ToString("O")
                    };
                }
                _report.status = "FAIL";
                _report.failure = failure.ToString();
                Debug.LogException(failure);
            }

            _report.completedUtc = DateTime.UtcNow.ToString("O");
            WriteReport071();
            Debug.Log("FIRST HOUR GOLD BUILT PLAYER SMOKE 084 " + _report.status + " • " + _reportPath);
#if UNITY_EDITOR
            Destroy(gameObject);
#else
            Application.Quit(StringComparer.Ordinal.Equals(_report.status, "PASS") ? 0 : 71);
#endif
        }

        private IEnumerator Execute071()
        {
            ResolvePaths071();
            Directory.CreateDirectory(_evidenceRoot);
            Directory.CreateDirectory(_screenshotRoot);
            _report = new FirstHourGoldSmokeReport071
            {
                applicationVersion = Application.version,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                startedUtc = DateTime.UtcNow.ToString("O"),
                savePath = _savePath,
                screenWidth = Screen.width,
                screenHeight = Screen.height
            };
            WriteReport071();
            Require071(Application.platform == RuntimePlatform.WindowsPlayer,
                "Studio First Hour certification must run in the packaged Windows player.");
            Require071(StringComparer.Ordinal.Equals(Application.unityVersion, "6000.3.22f1"),
                "Built player uses the wrong Unity version.");
            Require071(StringComparer.Ordinal.Equals(Application.version, ExpectedPlayerVersion),
                "Built player version is stale or unexpected: " + Application.version +
                "; expected " + ExpectedPlayerVersion + ".");
            BindPackagedBuildIdentity076();
            VerifyFirstHourArtPresentation076();
            WriteReport071();
            yield return WaitForPresenter071();
            Require071(_presenter != null, "Shipping presenter did not boot.");

            if (File.Exists(_savePath)) File.Delete(_savePath);
            if (File.Exists(_savePath + ".bak")) File.Delete(_savePath + ".bak");
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                _savePath);
            _presenter.Initialize(_coordinator);
            VerifyMaximumCampaignBattleCapacity078();

            RequireCertificationFrame076();
            RequireNamedVisibleTextEquals076(
                "Studio Title Hook 076",
                "A BELL RINGS BENEATH SKYHOME.\nTEN PEOPLE NEVER CAME HOME.");
            RequireNamedVisibleTextEquals076(
                "Studio Title Promise 076",
                "Choose face-down quest cards. Lead whole Unions in battle. Bring the missing Lantern Patrol home.");
            var titleKiriArtwork076 = GameObject.Find("Studio Title Kiri Hero Artwork 076")
                ?.GetComponent<Image>();
            _report.personFirstTitleVerified = titleKiriArtwork076 != null &&
                                                 titleKiriArtwork076.sprite != null;
            Require071(_report.personFirstTitleVerified,
                "The title did not introduce Kiri with authored person-first key art.");
            RequireFocusedVisibleAction076("START NEW GUILD");
            yield return Capture071("title");
            ClickVisibleAction076("START NEW GUILD");
            yield return null;
            var marketArrival076 = UnityEngine.Object
                .FindFirstObjectByType<WalkableSkyhomeArrival071>();
            _report.directGuildEntry091Verified =
                (marketArrival076 == null || !marketArrival076.IsActive071) &&
                GameObject.Find("Guild Charter Prologue 074") != null;
            Require071(_report.directGuildEntry091Verified,
                "START NEW GUILD must open the Guild charter directly without walking or pressing E.");
            RequireFocusedVisibleAction076("SIGN THE CHARTER");
            yield return Capture071("direct_guild_entry_no_walk_required_091");
            Require071(GameObject.Find("Guild Charter Prologue 074") != null,
                "Direct Guild entry did not retain the authored charter prologue.");
            var kiriArtwork076 = GameObject.Find("Kiri Aetherheart Prologue Portrait 076")
                ?.transform.Find("Portrait Backing/Portrait Artwork")
                ?.GetComponent<Image>();
            Require071(kiriArtwork076 != null && kiriArtwork076.sprite != null &&
                       StringComparer.Ordinal.Equals(
                           kiriArtwork076.sprite.name,
                           "KIRI_AETHERHEART_PROLOGUE_CROP_076") &&
                       !kiriArtwork076.preserveAspect,
                "Kiri's prologue portrait did not use the certified face-forward crop.");
            RequireFocusedVisibleAction076("SIGN THE CHARTER");
            yield return Capture071("guild_charter_prologue");

            ClickVisibleAction076("SIGN THE CHARTER");
            yield return null;
            Require071(GameObject.Find("Founding Company Presentation 076") != null,
                "Signing the charter did not open the six-founder presentation.");
            var presentedFounderIds = (_coordinator.State.Applicants ??
                                       Array.Empty<M1ApplicantView>())
                .Where(value => value != null)
                .Take(6)
                .Select(value => value.RecruitId)
                .ToArray();
            Require071(presentedFounderIds.Length == 6 &&
                       presentedFounderIds.Distinct(StringComparer.Ordinal).Count() == 6,
                "The founding presentation did not contain six distinct canonical identities.");
            foreach (var recruitId in presentedFounderIds)
                Require071(GameObject.Find("Founding Applicant " + recruitId + " 076") != null,
                    "The visible founder card did not match canonical recruit " + recruitId + ".");
            Require071(GameObject.Find(M1FlowPresenter.FoundingPreparationRootName078) != null,
                "Signing the charter did not enter the mandatory five-decision Guildmaster preparation.");
            RequireNamedVisibleTextContains076("Founding Preparation Step 078", "1 OF 5");
            RequireFocusedVisibleButtonByNamePrefix076("Founding Applicant ");
            yield return Capture071("founding_six_who_answered");
            ClickVisibleButtonByName076("Founding Applicant " + presentedFounderIds[0] + " 076");
            yield return null;
            RequireFocusedVisibleButtonByNamePrefix076("Founding Lead Confirm 078");
            yield return Capture071("founding_preparation_1_field_lead");
            ClickVisibleButtonByName076("Founding Lead Confirm 078");
            yield return null;

            RequireNamedVisibleTextContains076("Founding Preparation Step 078", "2 OF 5");
            RequireFocusedVisibleButtonByNamePrefix076("Founding Equipment Choice ");
            ClickVisibleButtonByName076("Founding Equipment Choice " +
                                        EquipmentSlotIds.MainHand + " 078");
            yield return null;
            RequireFocusedVisibleButtonByNamePrefix076("Founding Equipment Confirm 078");
            yield return Capture071("founding_preparation_2_equipment_priority");
            ClickVisibleButtonByName076("Founding Equipment Confirm 078");
            yield return null;

            RequireNamedVisibleTextContains076("Founding Preparation Step 078", "3 OF 5");
            var preparationUnion078 = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value078 => value078 != null)
                .OrderBy(value078 => value078.Index)
                .First();
            RequireFocusedVisibleButtonByNamePrefix076("Founding Union Destination ");
            ClickVisibleButtonByName076("Founding Union Destination " +
                                        preparationUnion078.Index + " 078");
            yield return null;
            RequireFocusedVisibleButtonByNamePrefix076("Founding Placement Confirm 078");
            yield return Capture071("founding_preparation_3_union_role");
            ClickVisibleButtonByName076("Founding Placement Confirm 078");
            yield return null;

            RequireNamedVisibleTextContains076("Founding Preparation Step 078", "4 OF 5");
            var preparationFormation078 = (_coordinator.State.Formations ??
                                           Array.Empty<M1ChoiceView>())
                .Where(value078 => value078 != null)
                .First();
            RequireFocusedVisibleButtonByNamePrefix076("Founding Formation Choice ");
            ClickVisibleButtonByName076("Founding Formation Choice " +
                                        preparationFormation078.Id + " 078");
            yield return null;
            RequireFocusedVisibleButtonByNamePrefix076("Founding Formation Confirm 078");
            yield return Capture071("founding_preparation_4_formation");
            ClickVisibleButtonByName076("Founding Formation Confirm 078");
            yield return null;

            RequireNamedVisibleTextContains076("Founding Preparation Step 078", "5 OF 5");
            RequireFocusedVisibleButtonByNamePrefix076(
                M1FlowPresenter.FoundingPreparationConfirmName078);
            yield return Capture071("founding_preparation_5_confirm_team");
            ClickVisibleButtonByName076(M1FlowPresenter.FoundingPreparationConfirmName078);
            yield return null;
            yield return null;
            Require071(GameObject.Find(M1FlowPresenter.FoundingPreparationRootName078) == null,
                "Confirming the rescue team did not leave the mandatory preparation cleanly.");
            _report.foundingGuildmasterPreparationVerified = true;

            Require071(presentedFounderIds.All(recruitId =>
                    (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .Any(value => value != null &&
                                  StringComparer.Ordinal.Equals(value.RecruitId, recruitId))),
                "The charter signed different identities from the six people shown to the player.");
            Require071(_coordinator.State.Recruits.Count == 10,
                "The founding company briefing did not save the ten-member charter.");
            Require071(_coordinator.State.OpeningUnionsLegal,
                "The founding company briefing did not save legal opening Unions.");
            var foundingUnions = (_coordinator.State.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                (value.MemberRecruitIds?.Count ?? 0) > 0)
                .Take(3)
                .ToArray();
            Require071(foundingUnions.Length == 3 && foundingUnions.All(union =>
                    GameObject.Find("Founding Union Summary " + union.Index + " 076") != null),
                "The founding company briefing did not show the three saved field Unions.");
            Require071(foundingUnions.All(union => GameObject.Find(
                    "Founding Union Leader Role Color " + union.Index + " 076") != null),
                "The founding company briefing did not color-code every Union leader role.");
            var assignedRecruitIds = new HashSet<string>(
                foundingUnions.SelectMany(value => value.MemberRecruitIds),
                StringComparer.Ordinal);
            var reserveRecruits = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null && !assignedRecruitIds.Contains(value.RecruitId))
                .ToArray();
            Require071(GameObject.Find("Founding Reserve Strip 076") != null,
                "The founding company briefing did not show its compact reserve strip.");
            RequireNamedVisibleTextContains076(
                "Founding Reserve Strip Text 076",
                reserveRecruits.Length + " READY AT HALL");
            foreach (var recruit in reserveRecruits)
                RequireNamedVisibleTextContains076(
                    "Founding Reserve Strip Text 076",
                    recruit.DisplayName);
            Require071(GameObject.Find("Founding Union Lesson 076") == null &&
                       GameObject.Find("Founding Hall Crew 076") == null,
                "The low-density founding briefing still rendered a tutorial or portrait dashboard.");
            _report.foundingCompanyVerified = true;
            RequireFocusedVisibleAction076("ENTER SKYHOME GUILD HALL");
            yield return Capture071("founding_company_union_briefing");

            ClickVisibleAction076("ENTER SKYHOME GUILD HALL");
            yield return null;
            _report.livingGuildHubVerified =
                GameObject.Find("Living Guild Hub 074") != null;
            Require071(_report.livingGuildHubVerified,
                "The populated Living Guild Hub did not render.");
            var phoneSimpleHallActions084 = new[]
            {
                "MISSIONS\nPLAY A QUEST",
                "RECRUIT\nMEET MEMBERS",
                "UNIONS\nBUILD THE PARTY",
                "INVENTORY\nGEAR & ITEMS",
                "INFINITE TOWER\nFIGHT & CLIMB",
                "ENTER CODE\nCLAIM REWARDS"
            };
            Require071(
                CountActiveButtonsNamed076("Living Guild Hub Facility ") == 6 &&
                phoneSimpleHallActions084.All(value =>
                    FindVisibleAction076(value) != null),
                "The Guild Hall did not present exactly its six phone-simple actions.");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Rank Resources Day Reputation 074",
                "XP TO SPEND");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Current Objective 074",
                "missing Wayglass");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Current Objective 074",
                "Lantern Road");
            _report.firstHallObjectiveVerified = true;
            yield return Capture071("living_guild_hall");

            ClickVisibleButtonByName076(
                "Living Guild Hub Facility " + WalkableGuildHall069.RecruitmentDestinationId069 + " 074");
            yield return null;
            Require071(GameObject.Find("Recruitment Desk 074") != null,
                "The Hall recruitment desk did not open its person-first applicant view.");
            var postNotice = GameObject.Find("Post Applicant Notice 074")
                ?.GetComponent<Button>();
            if (postNotice != null)
            {
                Require071(postNotice.interactable && postNotice.gameObject.activeInHierarchy,
                    "The first recruitment notice was visible but unavailable.");
                postNotice.onClick.Invoke();
                yield return null;
                yield return null;
            }
            RequireNamedVisibleTextContains076(
                "Applicant Personal Hook 074",
                "They");
            RequireNamedVisibleTextNotContains076(
                "Applicant Personal Hook 074",
                "HOOK_");
            RequireNamedVisibleTextNotContains076(
                "Applicant Personal Hook 074",
                "QUEST_");
            RequireNamedVisibleTextNotContains076(
                "Applicant Personal Hook 074",
                "_");
            Require071(GameObject.Find("Selected Applicant Portrait 074") != null &&
                       GameObject.Find("Applicant Decision Card 074") != null,
                "The recruitment desk did not show one applicant portrait and one readable decision.");
            yield return Capture071("recruitment_desk_person_first");
            ClickVisibleButtonByName076("Recruitment Return To Hall 074");
            yield return null;
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "The recruitment desk did not return to the Guild Hall.");

            ClickVisibleButtonByName076(
                "Living Guild Hub Facility " + WalkableGuildHall069.PartyDestinationId069 + " 074");
            yield return null;
            Require071(GameObject.Find("Union Planner 074") != null,
                "The compact Union Planner did not render.");
            RequireNamedVisibleTextContains076(
                "Union Planner Spendable Treasury XP Text 074",
                "FIELD  •  10 UNIONS × 6 = 60");
            RequireNamedVisibleTextContains076(
                "Union Planner Spendable Treasury XP Text 074",
                "HOUSING III GOAL  •  CAP 75");
            Require071(CountActiveButtonsNamed076("Union Planner Formation ") == 3,
                "The first-hour planner must present exactly three starter formations.");
            yield return VerifyVisibleUnionPlannerDragDrop078();
            _report.unionPlannerVerified = _report.unionPlannerDragDropVerified;
            ClickVisibleButtonByName076("Union Planner Back 074");
            yield return null;
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "The Union review did not return to the visible Guild Hall.");

            ClickVisibleButtonByName076(
                "Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074");
            yield return null;
            RequireNamedVisibleTextEquals076(
                "Featured Contract Reward 062",
                "REWARD  •  42 XP TO SPEND  •  44 HALL XP");
            yield return Capture071("contract_board");

            VerifyInputAuthorities071();
            ClickVisibleButtonByName076("Featured Contract Accept 062");
            yield return null;
            Require071(_coordinator.GuildCity017D.HasActiveContract &&
                       _coordinator.GuildCity017D.Expedition == null,
                "Accepting the featured contract did not persist the ready-to-depart contract state.");
            Require071(GameObject.Find("First Contract Begin Expedition 062") != null,
                "Accepting the featured contract did not expose its single Begin Expedition action.");
            RequireFocusedVisibleAction076("BEGIN EXPEDITION");
            ClickVisibleButtonByName076("First Contract Begin Expedition 062");
            yield return null;
            Require071(_coordinator.GuildCity017D.Expedition != null,
                "Beginning the featured contract did not create its authoritative expedition state.");
            Require071(GameObject.Find("Board Quest 081") != null,
                "Beginning the featured contract did not open the compact Board Quest.");
            _claimedProgressionReceiptBaseline071 =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount;
            Require071(_claimedProgressionReceiptBaseline071 == 0,
                "A fresh first-hour expedition must begin without previously claimed progression receipts.");
            Require071(StringComparer.Ordinal.Equals(
                    _coordinator.GuildCity017D.Expedition.BoardId,
                    GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071),
                "Ten-member charter did not select the certified three-battle board.");

            RequireExpeditionBoardNode078("N00");
            _report.expeditionBoardVerified = true;
            _report.missionBriefRoundTripVerified = true;
            yield return WaitForQuestCardDraft090(
                "the first-hour Board Quest departure",
                3);
            var departureQuestCards090 = ActiveQuestCardButtons090();
            _report.threeCardQuestChoiceVerified =
                (_coordinator.GuildCity017D.QuestCards090 ??
                 Array.Empty<GuildQuestCardView090>()).Count == 3 &&
                departureQuestCards090.Length == 3 &&
                departureQuestCards090.All(value => value.interactable) &&
                departureQuestCards090.Select(value => value.name)
                    .Distinct(StringComparer.Ordinal).Count() == 3 &&
                EventSystem.current != null &&
                departureQuestCards090.Any(value =>
                    value.gameObject ==
                    EventSystem.current.currentSelectedGameObject);
            var activeSceneObjectiveCount090 = UnityEngine.Object
                .FindObjectsByType<Text>(FindObjectsSortMode.InstanceID)
                .Count(value => value != null &&
                    value.gameObject.activeInHierarchy &&
                    value.gameObject.name.StartsWith(
                        "Board Quest Goal 081",
                        StringComparison.Ordinal) &&
                    !string.IsNullOrWhiteSpace(value.text));
            var departureRoot090 = GameObject.Find("Board Quest 081");
            var departureObjectives090 = departureRoot090 == null
                ? Array.Empty<Text>()
                : departureRoot090.GetComponentsInChildren<Text>(includeInactive: false)
                    .Where(value => value != null &&
                        value.gameObject.activeInHierarchy &&
                        value.gameObject.name.StartsWith(
                            "Board Quest Goal 081",
                            StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(value.text))
                    .ToArray();
            _report.oneObjectivePerFieldNodeVerified =
                departureObjectives090.Length == 1;
            Require071(_report.threeCardQuestChoiceVerified,
                "The Board Quest departure did not expose exactly three usable, distinct quest cards with controller focus on one card.");
            Require071(_report.oneObjectivePerFieldNodeVerified,
                "The three-card Board Quest did not preserve one clear visible story objective; " +
                "current Board Quest rendered " + departureObjectives090.Length +
                " and the active scene contained " + activeSceneObjectiveCount090 + ".");
            var departureRoomStrip081 = GameObject.Find("Board Quest Face Down Rooms 081");
            Require071(departureRoomStrip081 != null &&
                       departureRoomStrip081.transform.Cast<Transform>().Count(value =>
                           value != null && value.name.StartsWith(
                               "Board Quest Space ", StringComparison.Ordinal)) == 5,
                "The Board Quest did not show its five-space face-down room strip.");
            var departureRoot081 = GameObject.Find("Board Quest 081");
            Require071(departureRoot081 != null &&
                       !departureRoot081.GetComponentsInChildren<Text>(true).Any(value =>
                           value != null && value.gameObject.activeInHierarchy &&
                           ((value.text ?? string.Empty).IndexOf(
                                "N00", StringComparison.Ordinal) >= 0 ||
                            (value.text ?? string.Empty).IndexOf(
                                "N01", StringComparison.Ordinal) >= 0)),
                "Internal route-node IDs leaked into the simple Board Quest presentation.");
            yield return Capture071("expedition_board_departure");

            yield return MoveExpeditionBoard078("N01");
            yield return EnterExpeditionBoardBattle078("ENCOUNTER071_HALL_BREACH");
            yield return RunEncounter071("ENCOUNTER071_HALL_BREACH", "battle_1_hall_breach", true);

            RequireExpeditionBoardNode078("N01");
            yield return MoveExpeditionBoard078("N02");
            Require071(!_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Una's blood-marked ledger scene resolved before the Guildmaster gave an order.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Una");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "ledger");
            yield return Capture071("expedition_una_blood_marked_ledger");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Una's blood-marked ledger order did not commit.");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N03");
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Tazren's scouting beat was not committed by the saved board move.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Tazren");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "lantern scuffs");
            yield return Capture071("expedition_tazren_scouts_the_trail");

            yield return MoveExpeditionBoard078("N04");
            RequireExpeditionBoardNode078("N04");
            Require071(!_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The broken-waymarker decision resolved before the Guildmaster gave an order.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "fractured waymarker");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Quin");
            yield return Capture071("expedition_broken_waymarker_decision");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The broken-waymarker order did not commit its authored check.");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N05");
            yield return Capture071("expedition_patrol_cache");
            yield return MoveExpeditionBoard078("N06");
            yield return EnterExpeditionBoardBattle078(
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH");
            yield return RunEncounter071(
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH", "battle_2_lantern_road_ambush", false);

            RequireExpeditionBoardNode078("N06");
            yield return MoveExpeditionBoard078("N07");
            RequireExpeditionBoardNode078("N07");
            Require071(StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentEventId,
                           "EVENT_LANTERN_WATCH_CAMP") &&
                       _coordinator.GuildCity017D.Expedition.CurrentEventUsesCommitted2d6,
                "The Chapter 1 Lantern Watch camp did not keep its dedicated committed 2d6 decision.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Jazzi");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Zorin");
            yield return Capture071("expedition_camp_relationship_decision");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The camp relationship order did not commit.");
            Require071(_coordinator.GuildCity017D.Expedition.LastCheckDieOne > 0 &&
                       _coordinator.GuildCity017D.Expedition.LastCheckDieTwo > 0,
                "The Chapter 1 camp relationship order did not record its committed 2d6 result.");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N08");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Wayglass");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The mandatory Wayglass incident did not commit.");
            _report.resolvedCheckCount++;
            Require071((_coordinator.GuildCity017D.Expedition.LinkedNodeIds ??
                        Array.Empty<string>()).Contains("N09", StringComparer.Ordinal) &&
                       (_coordinator.GuildCity017D.Expedition.LinkedNodeIds ??
                        Array.Empty<string>()).Contains("N10", StringComparer.Ordinal),
                "Resolving the Wayglass incident did not expose both the optional elite and marked bypass.");
            yield return Capture071("expedition_wayglass_resolved_optional_elite_bypass");

            yield return MoveExpeditionBoard078("N10");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Petra");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Zorin");
            yield return Capture071("expedition_trapped_foreman_decision");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The trapped-foreman rescue did not commit.");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N11");
            RequireExpeditionBoardNode078("N11");
            yield return Capture071("expedition_wayglass_secondary_objective");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The Wayglass secondary objective did not commit.");
            _report.resolvedCheckCount++;

            var chapterOneVisitedNodes078 = new HashSet<string>(
                _coordinator.GuildCity017D.Expedition.VisitedNodeIds ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            _report.chapterOneStoryContinuityVerified =
                new[] { "N02", "N03", "N04", "N07", "N08", "N10", "N11" }
                    .All(chapterOneVisitedNodes078.Contains) &&
                !chapterOneVisitedNodes078.Contains("N09");
            Require071(_report.chapterOneStoryContinuityVerified,
                "The certified route did not preserve Una, Tazren, Quin, camp, Wayglass, elite bypass, and repaired-service-line continuity.");

            yield return MoveExpeditionBoard078("N13");
            RequireExpeditionBoardNode078("N13");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "patrol");
            yield return Capture071("expedition_lantern_patrol_rescue");
            yield return CommitExpeditionPrimaryOrder078();
            yield return null;
            Require071(GameObject.Find("Lantern Patrol Rescue Ceremony 076") != null,
                "Rallying the patrol did not open their visible arrival ceremony.");
            Require071(_coordinator.State.Recruits.Count == 20,
                "The permanent roster did not reach twenty before the boss.");
            VerifyTwoStartingTreesAndRosterLegality076();
            VerifyLanternPatrolVisualIdentityAssets076();
            VerifyLanternPatrolCeremonyPage076(0);
            RequireFocusedVisibleAction076("MEET THE REST OF THE PATROL");
            yield return Capture071("lantern_patrol_rescue_first_five");
            ClickVisibleAction076("MEET THE REST OF THE PATROL");
            yield return null;
            VerifyLanternPatrolCeremonyPage076(1);
            _report.lanternPatrolCeremonyVerified = true;
            RequireFocusedVisibleAction076("REVIEW 20-MEMBER UNION PLAN");
            yield return Capture071("lantern_patrol_rescue_second_five");
            ClickVisibleAction076("REVIEW 20-MEMBER UNION PLAN");
            yield return null;
            Require071(GameObject.Find("Union Planner 074") != null,
                "The patrol ceremony did not open the twenty-member Union review.");
            yield return Capture071("roster_20_union_review");
            RequireFocusedVisibleAction076("SAVE & FACE THE GATE-EATER");
            ClickVisibleAction076("SAVE & FACE THE GATE-EATER");
            yield return null;
            yield return null;
            RequireExpeditionBoardNode078("N13");
            Require071(GameObject.Find("Lantern Patrol Rescue Ceremony 076") == null &&
                       _coordinator.GuildCity017D.Expedition.CanCommitEncounter,
                "Saving the twenty-member Union review did not restore the Gate-Eater board order.");
            yield return EnterExpeditionBoardBattle078("ENCOUNTER071_GATE_EATER");
            yield return RunEncounter071("ENCOUNTER071_GATE_EATER", "battle_3_gate_eater_boss", false);

            RequireExpeditionBoardNode078("N13");
            yield return MoveExpeditionBoard078("N14");
            RequireQuestCardRounds090(12, "Chapter 1");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "home");
            yield return Capture071("expedition_road_home");
            yield return CommitExpeditionPrimaryOrder078();
            yield return null;
            Require071(GameObject.Find(
                           M1FlowPresenter.FirstOperationConsequencesRootName077) != null,
                "The first operation skipped the one-tap Guild homecoming.");
            Require071(M1FlowPresenter.FirstOperationConsequenceStageForVerification077(
                           _coordinator.GuildCity017D) ==
                       FirstOperationConsequenceStage077.RecoveryOrTraining,
                "The one-tap homecoming did not begin from the saved operation result.");
            RequireNamedVisibleTextEquals076(
                "Phone Simple Homecoming Heading 085",
                "MISSION COMPLETE  •  THE PATROL IS HOME");
            RequireNamedVisibleTextContains076(
                "Phone Simple Homecoming Summary 085",
                "HOME BASE READY");
            RequireFocusedVisibleAction076("CONTINUE TO GUILD HALL  →");
            Require071(FindVisibleButtonWithPrefix078("First Operation Choose ") == null &&
                       FindVisibleButtonWithPrefix078("First Operation Remember ") == null &&
                       FindVisibleButtonWithPrefix078("First Operation Build ") == null &&
                       FindVisibleButtonWithPrefix078("First Operation Staff ") == null,
                "The one-tap homecoming exposed retired management-choice buttons.");
            yield return Capture071("guild_homecoming_one_tap");
            ClickVisibleAction076("CONTINUE TO GUILD HALL  →");
            yield return null;
            yield return null;
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "The completed first operation did not return the player to Skyhome Guild Hall.");
            Require071(M1FlowPresenter.FirstOperationConsequenceStageForVerification077(
                           _coordinator.GuildCity017D) ==
                       FirstOperationConsequenceStage077.Complete,
                "The one-tap homecoming did not finish every saved background order.");
            Require071((_coordinator.GuildCity017D.Assignments ??
                        Array.Empty<GuildCityAssignmentView017D>()).Any(value =>
                    value != null &&
                    StringComparer.OrdinalIgnoreCase.Equals(value.Kind, "Recovering")),
                "The one-tap homecoming did not save a recovery order.");
            Require071((_coordinator.GuildCity017D.Relationships ??
                        Array.Empty<GuildCityRelationshipView017D>()).Any(value =>
                    value != null && value.Viewed),
                "The one-tap homecoming did not preserve a shared Guild memory.");
            Require071(_coordinator.GuildCity017D.StaffedBuildingCount == 1,
                "The one-tap homecoming did not persist one staffed home-base bonus.");
            Require071(_coordinator.GuildCity017D.FirstFacilityPayoffAcknowledged080,
                "The automatic home-base bonus remained as a blocking facility-review step.");
            Require071(M1FlowPresenter.GuidedHallStageForVerification080(
                           _coordinator.GuildCity017D,
                           true,
                           false) == GuildHallGuidedStage080.Ready,
                "Facility or inventory management still blocked Chapter 2 after homecoming.");
            _report.guildCityVisualProofVerified = true;

            ClickVisibleButtonByName076(
                "Living Guild Hub Facility " + WalkableGuildHall069.ArmoryDestinationId069 + " 074");
            yield return null;
            yield return EquipFirstRecoveredLoot078();
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "The armory did not return to the Guild Hall after equipping recovered loot.");
            yield return Capture071("guild_hall_return");
            Require071(_coordinator.SaveAndReloadProof(), "final first-hour save/reload proof");

            var chapterOneHash = _coordinator.State.CanonicalStateHash;
            var restored = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                _savePath);
            Require071(restored.State.HasCampaign, "saved campaign did not reload");
            Require071(restored.State.Recruits.Count == 20,
                "reloaded roster did not preserve twenty members");
            Require071(StringComparer.Ordinal.Equals(
                    restored.State.CanonicalStateHash,
                    chapterOneHash),
                "reloaded canonical state hash changed");
            _coordinator = restored;
            _presenter.Initialize(restored);
            _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.MainMenu);
            Require071(FindVisibleAction076("CONTINUE GAME") != null,
                "The reloaded save did not offer CONTINUE GAME on the title screen.");
            Require071(FindVisibleAction076("START NEW GUILD") != null,
                "The reloaded save trapped the player behind CONTINUE GAME without a Start New Guild choice.");
            var savedTitleHash077 = _coordinator.State.CanonicalStateHash;
            var savedTitleBytes077 = File.ReadAllBytes(_savePath);
            ClickVisibleAction076("START NEW GUILD");
            yield return null;
            RequireNamedVisibleTextEquals076("Title", "START A NEW GUILD?");
            RequireFocusedVisibleAction076("KEEP CURRENT GUILD");
            ClickVisibleAction076("KEEP CURRENT GUILD");
            yield return null;
            Require071(StringComparer.Ordinal.Equals(
                    _coordinator.State.CanonicalStateHash,
                    savedTitleHash077),
                "Opening and cancelling Start New Guild changed the active campaign.");
            Require071(File.ReadAllBytes(_savePath).SequenceEqual(savedTitleBytes077),
                "Opening and cancelling Start New Guild changed the save on disk.");
            Require071(FindVisibleAction076("CONTINUE GAME") != null &&
                       FindVisibleAction076("START NEW GUILD") != null,
                "Cancelling Start New Guild did not restore both saved-title choices.");
            RequireNamedVisibleTextEquals076(
                "Studio Title Chapter 076",
                "CHAPTER 2  •  THE DOOR INSIDE");
            RequireNamedVisibleTextEquals076(
                "Studio Title Hook 076",
                "THE PATROL CAME HOME.\nTHE WAYGLASS OPENED A ROAD WITHIN.");
            RequireNamedVisibleTextEquals076(
                "Studio Title Promise 076",
                "Return to the Hall table. Follow the survey crew's brass line beneath Skyhome.");
            RequireFocusedVisibleAction076("CONTINUE GAME");
            yield return Capture071("reload_continue_story");
            ClickVisibleAction076("CONTINUE GAME");
            yield return null;
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "CONTINUE GAME did not restore the player to the saved Guild Hall.");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Current Objective 074",
                "Door Inside");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Current Objective 074",
                "Wayglass");
            yield return Capture071("chapter_2_hall_objective");
            Require071(_coordinator.GuildCity017D.PlacedBuildingCount == 1,
                "The player-chosen first Hall facility did not survive reload.");
            RequireFocusedVisibleAction076("BEGIN CHAPTER 2  →");

            ClickVisibleButtonByName076("Living Guild Hub Primary CTA 074");
            yield return null;
            Require071(FindVisibleAction076("BEGIN CHAPTER 2") != null,
                "The Hall objective did not lead to the Chapter 2 contract.");
            ClickVisibleAction076("BEGIN CHAPTER 2");
            yield return null;
            _report.chapterTwoOpeningVerified =
                GameObject.Find(M1FlowPresenter.ChapterTwoOpeningRootName076) != null;
            Require071(_report.chapterTwoOpeningVerified,
                "BEGIN CHAPTER 2 did not open the authored Wayglass briefing.");
            RequireFocusedVisibleAction076(
                M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");
            Require071(FindVisibleAction076(
                           M1FlowPresenter.ChapterTwoNorthRouteAction076 + "  →") == null &&
                       FindVisibleAction076(
                           M1FlowPresenter.ChapterTwoUnderhallRouteAction076 + "  →") == null,
                "Chapter 2 still asked for a blind route choice before meeting the apprentice.");
            yield return Capture071("chapter_2_wayglass_opening");

            ClickVisibleAction076(M1FlowPresenter.ChapterTwoThresholdAction078 + "  →");
            yield return null;
            yield return null;
            Require071(_coordinator.GuildCity017D.Expedition != null &&
                       StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.BoardId,
                           GuildCityExpeditionService017D.SecondStoryBoardId076) &&
                       StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentNodeId,
                           "N00") &&
                       !_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Descending with Kiri did not open Sella's unresolved threshold scene at N00.");
            RequireExpeditionBoardNode078("N00");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Sella Vey");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra Vale");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "brass line-tag");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Wayglass");
            RequireNamedVisibleTextContains076(
                "Expedition Check Lead Heading 074",
                "READY");
            RequireNamedVisibleTextContains076(
                "Expedition Companion Story Beat Text 076",
                "MAREN");
            RequireBoardQuestStoryBackdrop081("Sella's Chapter 2 threshold");
            RequireBoardQuestDiceChoice081("Sella's Chapter 2 threshold");
            _report.chapterTwoCrewAssignmentsVerified = true;
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The Guild did not hear Sella's threshold evidence before the next room flip.");
            RequireChapterTwoCommitted2d6Resolution081("Sella's threshold evidence");
            yield return Capture071("chapter_2_sella_threshold");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N01");
            RequireExpeditionBoardNode078("N01");
            Require071((_coordinator.GuildCity017D.Expedition.LinkedNodeIds ??
                        Array.Empty<string>()).Count == 2,
                "Sella's evidence did not preserve the two authored destinations behind the automatic room shuffle.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Sella's testimony");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "reversed fresh marks");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            RequireNamedVisibleTextContains076(
                "Board Quest Goal 081",
                "unrecorded door");
            var chapterTwoEvidenceAtFork081 =
                ChapterTwoCrewMechanics079.BuildEvidenceStatus079(
                    _coordinator.GuildCity017D);
            Require071(chapterTwoEvidenceAtFork081.IsVisible &&
                       chapterTwoEvidenceAtFork081.ReliableEvidenceCount +
                       chapterTwoEvidenceAtFork081.PartialEvidenceCount >= 1 &&
                       !StringComparer.Ordinal.Equals(
                           chapterTwoEvidenceAtFork081.SellaStatus,
                           "WAITING") &&
                       !string.IsNullOrWhiteSpace(
                           chapterTwoEvidenceAtFork081.CrewTrustBand),
                "Sella's saved evidence and crew-trust state were not authoritative before the automatic next-room flip.");
            _report.chapterTwoObjectiveAndEvidenceVerified = true;
            yield return Capture071("chapter_2_evidence_auto_room");
            yield return MoveExpeditionBoard078(M1FlowPresenter.ChapterTwoNorthRouteNodeId076);
            yield return Capture071("chapter_2_fresh_marks_operation_board");

            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The Guild did not test Orra's brass tag against the false fresh marks.");
            RequireChapterTwoCommitted2d6Resolution081("Orra's fresh-mark evidence");
            _report.resolvedCheckCount++;
            yield return MoveExpeditionBoard078("N03");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "official survey steps");
            yield return Capture071("chapter_2_scouting_orra_true_line");

            yield return MoveExpeditionBoard078("N06");
            yield return EnterExpeditionBoardBattle078(
                "ENCOUNTER_FOG_STALKERS_STANDARD");
            yield return RunEncounter071(
                "ENCOUNTER_FOG_STALKERS_STANDARD",
                "battle_4_chapter_2_fog_stalkers",
                false);

            RequireExpeditionBoardNode078("N06");
            yield return MoveExpeditionBoard078("N07");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Sella");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            yield return Capture071("chapter_2_camp_sella_names_orra");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Sella's saved camp testimony did not commit.");
            RequireChapterTwoCommitted2d6Resolution081("Sella's camp testimony");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N08");
            Require071(StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentEventId,
                           "EVENT_FOG_ECHO") &&
                       _coordinator.GuildCity017D.Expedition
                           .CurrentEventUsesCommitted2d6 &&
                       !_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The required fog-echo room did not expose its authored committed-2d6 story decision.");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "fog");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            // Reuse the former astrolabe-only evidence slot so the release keeps
            // its exact 72-frame review contract while proving the new required
            // ten-card route beat.
            yield return Capture071("chapter_2_fog_echo_seventh_return");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "The Guild did not challenge the fog imitation with Sella's seventh-return evidence.");
            RequireChapterTwoCommitted2d6Resolution081(
                "the fog echo's false Orra voice");
            _report.resolvedCheckCount++;
            Require071((_coordinator.GuildCity017D.Expedition.LinkedNodeIds ??
                        Array.Empty<string>()).Contains("N09", StringComparer.Ordinal) &&
                       (_coordinator.GuildCity017D.Expedition.LinkedNodeIds ??
                        Array.Empty<string>()).Contains("N10", StringComparer.Ordinal),
                "Resolving the fog echo did not preserve the optional Route Mutilator and marked bypass.");

            yield return MoveExpeditionBoard078("N10");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "astrolabe");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "door");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Orra's astrolabe evidence did not commit.");
            RequireChapterTwoCommitted2d6Resolution081("Orra's astrolabe evidence");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N11");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "field book");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            // The newly flipped card deliberately holds its next action while the
            // physical reveal finishes. CommitExpeditionPrimaryOrder078 waits for
            // that same lock, then proves the automatic saved dice result. The
            // threshold checkpoint above already certifies the visible no-menu
            // roll presentation before a card-settle hold is active.
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.GuildCity017D.Expedition.ResolutionComplete,
                "Orra's field-book evidence did not commit before the surveyor rescue.");
            RequireChapterTwoCommitted2d6Resolution081("Orra's field-book evidence");
            yield return Capture071("chapter_2_next_objective_orra_field_book");
            _report.resolvedCheckCount++;

            yield return MoveExpeditionBoard078("N13");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "crew");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "door");
            RequireBoardQuestStoryBackdrop081("Orra's surveyor-rescue objective");
            yield return Capture071("chapter_2_surveyors_at_door_rescue");
            yield return EnterExpeditionBoardBattle078(
                ChapterTwoSurveyorRescueEncounterId078);
            yield return RunEncounter071(
                ChapterTwoSurveyorRescueEncounterId078,
                "battle_5_chapter_2_surveyor_rescue",
                false);

            RequireExpeditionBoardNode078("N13");
            yield return MoveExpeditionBoard078("N14");
            RequireQuestCardRounds090(10, "Chapter 2");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Sella");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Orra");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Skyhome");
            RequireNamedVisibleTextContains076(
                "Board Quest Scene Title 081",
                "THE WAY HOME");
            RequireNamedVisibleTextContains076(
                "Expedition Current Position Summary 074",
                "Skyhome's Wayglass table");
            RequireNamedVisibleTextNotContains076(
                "Expedition Current Position Summary 074",
                "patrol and Wayglass");
            RequireNamedVisibleTextContains076(
                "Board Quest Goal 081",
                "survey line is secure");
            var chapterTwoReturnEvidence081 =
                ChapterTwoCrewMechanics079.BuildEvidenceStatus079(
                    _coordinator.GuildCity017D);
            Require071(chapterTwoReturnEvidence081.IsVisible &&
                       chapterTwoReturnEvidence081.OrraRescued &&
                       (chapterTwoReturnEvidence081.CompactReadout ?? string.Empty).IndexOf(
                           "ORRA TRAIL SECURED", StringComparison.Ordinal) >= 0 &&
                       (chapterTwoReturnEvidence081.CompactReadout ?? string.Empty).IndexOf(
                           "ORRA TRAIL 4/6", StringComparison.Ordinal) < 0,
                "The authoritative Chapter 2 return state did not keep Orra's trail secured.");
            RequireBoardQuestStoryBackdrop081("Sella and Orra's return to Skyhome");
            yield return Capture071("chapter_2_testimony_to_skyhome_cliffhanger");

            var chapterTwoVisited079 = new HashSet<string>(
                _coordinator.GuildCity017D.Expedition.VisitedNodeIds ??
                Array.Empty<string>(),
                StringComparer.Ordinal);
            _report.chapterTwoPlayableChainVerified =
                new[]
                {
                    "N00", "N01", "N02", "N03", "N06", "N07", "N08", "N10", "N11", "N13", "N14"
                }
                    .All(chapterTwoVisited079.Contains) &&
                _report.battleIds.Contains(
                    "ENCOUNTER_FOG_STALKERS_STANDARD",
                    StringComparer.Ordinal) &&
                _report.battleIds.Contains(
                    ChapterTwoSurveyorRescueEncounterId078,
                    StringComparer.Ordinal) &&
                _coordinator.GuildCity017D.Expedition.CanFinalizeOperation;
            Require071(_report.chapterTwoPlayableChainVerified,
                "Chapter 2 did not preserve all ten card moves, crew decisions, field-book evidence, two encounters, surveyor rescue, and return route.");

            yield return CommitExpeditionPrimaryOrder078();
            yield return null;
            Require071(GameObject.Find("Living Guild Hub 074") != null,
                "Returning Sella, Orra, and the survey crew did not restore the Guild Hall.");
            Require071((_coordinator.GuildCity017D.Contracts ??
                        Array.Empty<GuildCityContractView017D>()).Any(value =>
                    value != null &&
                    StringComparer.Ordinal.Equals(
                        value.ContractId,
                        GuildCityExpeditionService017D.SecondStoryContractId076) &&
                    value.IsCompleted),
                "The Surveyor Rescue return did not complete Chapter 2's saved contract.");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Chapter 074",
                "CHAPTER 3");
            RequireNamedVisibleTextContains076(
                "Living Guild Hub Current Objective 074",
                "relief road");
            RequireFocusedVisibleAction076("BEGIN CHAPTER 3  →");
            yield return Capture071("chapter_3_hall_objective_after_surveyor_rescue");

            var chapterTwoHash = _coordinator.State.CanonicalStateHash;
            var chapterTwoReloaded = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                _savePath);
            _report.chapterTwoRoutePersistenceVerified =
                chapterTwoReloaded.GuildCity017D.Expedition == null &&
                (chapterTwoReloaded.GuildCity017D.Contracts ??
                 Array.Empty<GuildCityContractView017D>()).Any(value =>
                    value != null &&
                    StringComparer.Ordinal.Equals(
                        value.ContractId,
                        GuildCityExpeditionService017D.SecondStoryContractId076) &&
                    value.IsCompleted) &&
                StringComparer.Ordinal.Equals(
                    chapterTwoReloaded.State.CanonicalStateHash,
                    chapterTwoHash);
            Require071(_report.chapterTwoRoutePersistenceVerified,
                "The completed Chapter 2 rescue, return, and Chapter 3 handoff did not survive reload.");

            _report.canonicalStateHash = chapterTwoHash;
            _report.reloadedStateHash = chapterTwoReloaded.State.CanonicalStateHash;
            _report.rosterCount = chapterTwoReloaded.State.Recruits.Count;
            _report.activeUnionCount = chapterTwoReloaded.State.Unions.Count(value =>
                value != null && value.MemberRecruitIds != null && value.MemberRecruitIds.Count > 0);
            _report.totalClaimedProgressionReceiptCount =
                chapterTwoReloaded.GuildCity017D.ClaimedBattleRewardCount;
            var newlyClaimedProgressionReceipts071 =
                _report.totalClaimedProgressionReceiptCount -
                _claimedProgressionReceiptBaseline071;
            Require071(newlyClaimedProgressionReceipts071 == _report.battleIds.Count + 2,
                "Two completed operations must add two contract rewards beyond the five battle rewards.");
            _report.claimedBattleRewardCount = _report.battleIds.Count;
            _report.saveReloadVerified = StringComparer.Ordinal.Equals(
                chapterTwoReloaded.State.CanonicalStateHash, chapterTwoHash);
            _report.chapterTwoRouteNodeId = "N14";
            _report.nextObjective = "Chapter 3: Keep Skyhome's relief road open";
            Require071(_report.buildIdentityVerified,
                "The packaged build identity was not bound to the smoke evidence.");
            Require071(_report.characterIdentityArtVerified,
                "The ten rescued recruits did not retain unique portrait and battle identities.");
            Require071(_report.directGuildEntry091Verified &&
                       _report.expeditionBoardVerified &&
                       _report.missionBriefRoundTripVerified &&
                       _report.threeCardQuestChoiceVerified &&
                       _report.oneObjectivePerFieldNodeVerified &&
                       _report.postBattleFieldReturnVerified,
                "The certified route did not preserve direct Guild entry and the complete saved expedition-board journey.");
            Require071(_report.firstHourArtPresentationVerified,
                "The shipping battle presentation did not cover all 120 first-hour Arts exactly.");
            Require071(_report.liveFirstHourArtRecipeConsumptionVerified,
                "No real 072 battle exchange consumed an exact Art motion, VFX, timing, and SFX recipe.");
            Require071(_report.twoStartingTreesVerified,
                "The certified roster did not preserve exactly two usable starting Art trees per member.");
            Require071(_report.manualLootEquippedVerified,
                "The Guild never manually equipped a real recovered item through the armory.");
            Require071(_report.patrolCombatReadyVerified,
                "The rescued patrol did not participate through the complete saved Gate-Eater Union plan.");
            Require071(_report.rosterCount == 20,
                "The certified reload did not preserve the twenty-member Guild roster.");
            Require071(_report.tenBySixCampaignCapacityVerified,
                "The packaged player did not prove ten six-member Unions and sixty unique deployed members.");
            VerifyPersistedStoryUnionPlans078(chapterTwoReloaded.State);
            Require071(_report.claimedBattleRewardCount == 5,
                "The five certified encounters did not leave exactly five claimed reward records.");
            Require071(_report.totalClaimedProgressionReceiptCount == 7,
                "Two completed operations and five battles must leave exactly seven progression receipts.");
            Require071(_report.resolvedCheckCount == 12,
                "The connected review route must resolve six Chapter 1 decisions plus six committed-2d6 Chapter 2 crew decisions.");
            Require071(_report.battleIds.SequenceEqual(new[]
            {
                "ENCOUNTER071_HALL_BREACH",
                "ENCOUNTER071_LANTERN_ROAD_AMBUSH",
                "ENCOUNTER071_GATE_EATER",
                "ENCOUNTER_FOG_STALKERS_STANDARD",
                ChapterTwoSurveyorRescueEncounterId078
            }), "certified battle order changed");
            Require071(_report.artBreakthroughObserved,
                "No live Art breakthrough was observed during the five battles.");
            Require071(_report.battleOrdersReadiedVerified,
                "The evidence run never showed every active Union with a readied order.");
            Require071(_report.battleExchangeResolvedVerified,
                "The evidence run never proved a real authoritative battle exchange.");
            Require071(_report.battleHpImpactPresentationVerified,
                "The evidence run never showed a real mid-impact HP change with signed and before-to-after values.");
            Require071(_report.transientLearnedArtNoticeVerified,
                "The evidence run never captured a learned-Art notice both visible and cleared during battle.");
            Require071(_report.chapterOneStoryContinuityVerified,
                "The evidence run did not preserve the mandatory Chapter 1 character and Wayglass story chain.");
            Require071(_report.foundingGuildmasterPreparationVerified &&
                       _report.unionPlannerDragDropVerified &&
                       _report.firstHallObjectiveVerified &&
                       _report.chapterTwoCrewAssignmentsVerified &&
                       _report.chapterTwoObjectiveAndEvidenceVerified &&
                       _report.chapterTwoPlayableChainVerified &&
                       _report.guildCityVisualProofVerified,
                "The visual Guildmaster, Union drag/drop, Chapter 2 evidence, or one-tap home-base proof was incomplete.");
            yield return CertifyPackagedTowerFloorOne081();
            Require071(_report.endlessTowerFloorOneVerified,
                "The packaged Endless Tower did not preserve its floor-one battle, exact-once reward, and reload proof.");
            Require071(_report.campaignEnemyArt700Verified,
                "No packaged Campaign battle proved that every live enemy actor rendered its committed Enemy Art 700 identity.");
            Require071(_report.towerEnemyArt700Verified,
                "The packaged Tower Floor 1 battle did not prove that every live enemy actor rendered its committed Enemy Art 700 identity.");
            yield return CertifyProjectedCampaign023Boards084();
            yield return CertifyEarnedRecruitAndAscension089();
            Require071(_chestCaptureVerified092 && _screenshotOrdinal == 75 &&
                       _screenshotHashes071.Count == _screenshotOrdinal &&
                       _report.screenshotSha256.Count == _screenshotOrdinal &&
                       _report.renderedScreenshotValidationVerified,
                "The phone-simple evidence set must contain exactly 75 unique, visibly rendered frames, including three real chest poses.");
            _report.gates.Add("PASS: packaged_build_identity_bound");
            _report.gates.Add("PASS: certified_frame_" + _report.certificationFrame);
            _report.gates.Add("PASS: title_to_completed_chapter_2_return");
            _report.gates.Add("PASS: title_copy_tracks_current_chapter");
            _report.gates.Add("PASS: person_first_title_key_art");
            _report.gates.Add("PASS: direct_guild_entry_without_walk_or_interaction_gate_091");
            _report.gates.Add("PASS: kiri_prologue_face_forward_crop");
            _report.gates.Add("PASS: six_visible_founders_match_signed_ids");
            _report.gates.Add("PASS: five_saved_guildmaster_preparation_decisions");
            _report.gates.Add("PASS: ten_member_founding_company_briefing");
            _report.gates.Add("PASS: populated_living_guild_hub_with_visible_first_objective");
            _report.gates.Add("PASS: person_first_applicant_desk");
            _report.gates.Add("PASS: six_phone_simple_guild_hall_actions");
            _report.gates.Add("PASS: spendable_xp_uses_player_language");
            _report.gates.Add("PASS: six_slot_shipping_drag_drop_three_formation_union_planner");
            _report.gates.Add("PASS: three_card_choose_one_quest_deck_ch1_12_ch2_10_saved_receipts");
            _report.gates.Add("PASS: mandatory_una_tazren_quin_story_continuity");
            _report.gates.Add("PASS: mandatory_camp_then_wayglass_story_continuity");
            _report.gates.Add("PASS: optional_secondary_objective");
            _report.gates.Add("PASS: six_committed_chapter_1_field_decisions");
            _report.gates.Add("PASS: post_battle_board_position_reconstructed");
            _report.gates.Add("PASS: first_battle_union_command_coach");
            _report.gates.Add("PASS: every_active_union_order_visibly_readied");
            _report.gates.Add("PASS: authoritative_battle_exchange_resolved");
            _report.gates.Add("PASS: exact_battle_impact_visibly_changes_hp");
            _report.gates.Add("PASS: learned_art_notice_visible_then_cleared_during_battle");
            _report.gates.Add("PASS: five_ordered_union_battles");
            _report.gates.Add("PASS: two_page_ten_person_patrol_rescue");
            _report.gates.Add("PASS: ten_unique_lantern_patrol_visual_identities");
            _report.gates.Add("PASS: twenty_member_union_review_before_boss");
            _report.gates.Add("PASS: legal_story_union_plans_five_battle_rewards_seven_progression_receipts_and_ten_by_six_campaign_capacity");
            _report.gates.Add("PASS: all_ten_rescued_patrol_members_receive_gate_eater_actions");
            _report.gates.Add("PASS: exactly_two_usable_starting_art_trees_per_member");
            _report.gates.Add("PASS: exact_live_120_art_profiles_audio_and_icons");
            _report.gates.Add("PASS: live_072_exact_art_motion_vfx_timing_and_sfx_consumed");
            _report.gates.Add("PASS: keyboard_controller_unified_input");
            _report.gates.Add("PASS: chapter_1_hall_reward_save_reload");
            _report.gates.Add("PASS: one_tap_homecoming_saves_recovery_memory_and_home_base");
            _report.gates.Add("PASS: first_home_base_bonus_staffed_automatically");
            _report.gates.Add("PASS: chapter_2_unblocked_without_management_graphs");
            _report.gates.Add("PASS: recovered_equipment_manually_equipped");
            _report.gates.Add("PASS: chapter_2_wayglass_opening");
            _report.gates.Add("PASS: chapter_2_auto_field_team_and_visible_committed_2d6_result");
            _report.gates.Add("PASS: chapter_2_sella_orra_evidence_before_next_room");
            _report.gates.Add("PASS: chapter_2_five_crew_story_beats_camp_and_two_encounters");
            _report.gates.Add("PASS: chapter_2_completed_route_persists");
            _report.gates.Add("PASS: chapter_2_surveyor_rescue_return_and_chapter_3_objective");
            _report.gates.Add("PASS: packaged_endless_tower_floor_battle_reward_and_reload");
            _report.gates.Add("PASS: packaged_campaign_enemy_art_700");
            _report.gates.Add("PASS: packaged_tower_enemy_art_700");
            _report.gates.Add("PASS: c023_three_card_chapter_and_twelve_room_contract_at_certified_resolution");
            _report.gates.Add("PASS: earned_recruit_card_saves_exact_hero_master_lead");
            _report.gates.Add("PASS: earned_recruit_lead_survives_production_save_reload");
            _report.gates.Add("PASS: applicant_board_exposes_and_signs_same_exact_hero_once");
            _report.gates.Add("PASS: another_earned_card_targets_owned_hero_for_ascension");
            _report.gates.Add("PASS: live_recruitment_desk_renders_duplicate_merge_preview_and_control");
            _report.gates.Add("PASS: duplicate_merge_advances_ascension_once_without_roster_growth");
            _report.gates.Add("PASS: rendered_distinct_screenshots");
            Require071(_report.gates.Count == 58,
                "The expanded first-hour certification gate ledger changed unexpectedly.");
            _report.status = "PASS";
        }

        private IEnumerator CertifyProjectedCampaign023Boards084()
        {
            var presentationCoordinator =
                _coordinator as ICampaignWorldGatePresentationCoordinator023;
            Require071(presentationCoordinator != null,
                "The packaged runtime does not expose the shipping C023 presentation coordinator.");

            var boardRegistry = CampaignRegistry023.LoadFromResources();
            var chapterRegistry = CampaignRegistry019.LoadFromResources();
            var chapterState = BuildProjectedCampaign023SmokeState084(
                boardRegistry, chapterRegistry, SmokeChapterBoardId084);
            var repeatableState = BuildProjectedCampaign023SmokeState084(
                boardRegistry, chapterRegistry, SmokeLongestRepeatableBoardId084);
            Require071(chapterState.TotalNodes >= 10 &&
                       repeatableState.TotalNodes == 12,
                "The packaged C023 proof did not select an authored chapter and the longest twelve-room contract.");

            var width = Screen.width;
            var height = Screen.height;
            Require071((width == 1280 && height == 800) ||
                       (width == 1920 && height == 1080),
                "The packaged C023 proof must run at 1280x800 or 1920x1080.");

            _presenter.ShowFirstHourGoldAdventureBoard084(
                presentationCoordinator, chapterState);
            yield return null;
            RequireProjectedCampaign023BoardCopy084(
                boardRegistry, chapterRegistry, chapterState,
                SmokeChapterBoardId084, width, height);
            yield return Capture071("campaign023_authored_chapter_objective");
            _presenter.FocusFirstHourGoldAdventureMissionBrief084();
            yield return null;
            yield return Capture071("campaign023_authored_chapter_mission_brief");

            _presenter.ShowFirstHourGoldAdventureBoard084(
                presentationCoordinator, repeatableState);
            yield return null;
            RequireProjectedCampaign023BoardCopy084(
                boardRegistry, chapterRegistry, repeatableState,
                SmokeLongestRepeatableBoardId084, width, height);
            yield return Capture071(
                "campaign023_longest_12_room_contract_objective");
            _presenter.FocusFirstHourGoldAdventureTrack084();
            yield return null;
            yield return Capture071(
                "campaign023_longest_12_room_contract_track");
        }

        private static CampaignWorldGatePresentationState023
            BuildProjectedCampaign023SmokeState084(
                CampaignRegistry023 boardRegistry,
                CampaignRegistry019 chapterRegistry,
                string definitionId)
        {
            if (boardRegistry == null) throw new ArgumentNullException(nameof(boardRegistry));
            if (chapterRegistry == null) throw new ArgumentNullException(nameof(chapterRegistry));
            if (!boardRegistry.Boards.TryGetValue(definitionId, out var board) ||
                board == null)
                throw new InvalidOperationException(
                    "Packaged C023 smoke board is missing: " + definitionId + ".");
            var nodes = board.nodes ?? Array.Empty<NodeDto023>();
            var current = nodes.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.nodeId, board.startNodeId));
            if (current == null)
                throw new InvalidOperationException(
                    "Packaged C023 smoke board has no authored start room: " +
                    definitionId + ".");

            ChapterNarrative019 chapter = null;
            if (StringComparer.Ordinal.Equals(board.operationKind, "CHAPTER"))
                chapterRegistry.Chapters.TryGetValue(board.definitionId, out chapter);
            var worldName = boardRegistry.Standing.TryGetValue(
                    board.worldId, out var standing) && standing != null
                ? standing.displayName
                : "Guild World";
            var projectedRooms = new Dictionary<string,
                AdventureBoardNarrativeProjection084.RoomCopy084>(StringComparer.Ordinal);
            var projectedTitles = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var node in nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId)) continue;
                var projected = AdventureBoardNarrativeProjection084.Project(
                    board, node, chapter, worldName);
                projectedRooms[node.nodeId] = projected;
                projectedTitles[node.nodeId] = projected.Title;
            }

            var currentCopy = projectedRooms[current.nodeId];
            const string smokeOperationId = "PACKAGED-C023-PROJECTION-084";
            var orderedChoices = BoardAdventureRules084.OrderedChoices084(
                smokeOperationId, current.nodeId,
                current.choiceIds ?? Array.Empty<string>());
            var choiceViews = orderedChoices.Select(value => new ChoiceView023
            {
                ChoiceId = value,
                Label = BoardAdventureRules084.ChoiceLabel084(value),
                StableOrderKey = BoardAdventureRules084.RouteOrderKey084(
                    smokeOperationId, current.nodeId, value)
            }).ToArray();
            var nodeRule = new WorldGateNodeRule023
            {
                NodeId = current.nodeId,
                Kind = current.kind,
                Title = current.title,
                Description = current.description,
                NextNodeIds = current.nextNodeIds ?? Array.Empty<string>(),
                ChoiceIds = current.choiceIds ?? Array.Empty<string>(),
                CheckDifficulty = current.checkDifficulty,
                SupplyDelta = current.supplyDelta,
                FatigueDelta = current.fatigueDelta,
                UrgencyDelta = current.urgencyDelta,
                ThreatDelta = current.threatDelta,
                TrustDelta = current.trustDelta,
                TensionDelta = current.tensionDelta,
                CivilianSupportDelta = current.civilianSupportDelta,
                GuildXp = current.guildXp,
                HallXp = current.hallXp,
                MaterialIds = current.materialIds ?? Array.Empty<string>(),
                RequiresCertifiedBattle = current.requiresCertifiedBattle,
                EnemyUnionCount = current.enemyUnionCount,
                Objective = current.objective,
                Optional = current.optional,
                SourceId = current.sourceId
            };
            return new CampaignWorldGatePresentationState023
            {
                IsAvailable = true,
                CurrentWorldId = board.worldId,
                CurrentWorldName = worldName,
                TravelSupplies = 8,
                ActiveOperationId = smokeOperationId,
                ActiveDefinitionId = board.definitionId,
                ActiveBoardTitle = BoardAdventureRules084.PlayerCopy084(
                    board.title, board.definitionId, board.boardId),
                ActiveOperationKind = board.operationKind,
                ActiveStatus = "Active",
                BoardObjective = AdventureBoardNarrativeProjection084.BoardObjective(
                    board, chapter, worldName),
                Supplies = 6,
                Fatigue = 0,
                Urgency = 0,
                Threat = 0,
                CompletedNodes = 0,
                TotalNodes = nodes.Length,
                CurrentNode = new NodeView023
                {
                    NodeId = current.nodeId,
                    Kind = current.kind,
                    RoomKind = BoardAdventureRules084.RoomKind084(current.kind),
                    RoomTitle = BoardAdventureRules084.RoomTitle084(current.kind),
                    Title = currentCopy.Title,
                    Description = currentCopy.Description,
                    StoryFlavor = currentCopy.StoryFlavor,
                    Objective = currentCopy.Objective,
                    RewardPreview = BoardAdventureRules084.RewardPreview084(nodeRule),
                    Choices = orderedChoices,
                    ChoiceViews = choiceViews,
                    Difficulty = current.checkDifficulty,
                    RequiresBattle = current.requiresCertifiedBattle,
                    IconResource = "SecondDimension/Campaign023/Icons/NODE_" +
                                   current.kind + "_023"
                },
                AdventureTiles = AdventureBoardTrackProjection084.Project(
                    board, current.nodeId, Array.Empty<string>(), null,
                    projectedTitles),
                Boards = Array.Empty<BoardView023>(),
                Standings = Array.Empty<StandingView023>(),
                UnlockedRecruitOriginIds = Array.Empty<string>(),
                RouteCards = ProjectedExpeditionCards089(
                    smokeOperationId, worldName, board.operationKind),
                DeckDrawPileCount = 15,
                DeckDiscardCount = 0,
                DeckBanishedCount = 0,
                DeckMomentum = 1,
                DeckModifierLabels = new[] {"TRAIL RATIONS  •  +5% CAMP"},
                DeckRemainingCompositionLabels = new[]
                {
                    "STORY ×4", "CHEST ×3", "CHANCE ×3", "BUFF ×2",
                    "HAZARD ×2", "RECRUIT ×1"
                },
                DeckDiscardCompositionLabels = Array.Empty<string>(),
                ExpeditionRecruitLeadIds = Array.Empty<string>(),
                ExpeditionDeckTutorialSeen = true,
                ExpeditionDeckTutorialSteps = ExpeditionDeckService089.TutorialSteps
            };
        }

        private static IReadOnlyList<ExpeditionRouteCardView089>
            ProjectedExpeditionCards089(
                string operationId,
                string worldName,
                string operationKind)
        {
            var routeLabel = StringComparer.Ordinal.Equals(operationKind, "CHAPTER")
                ? "STORY ROUTE"
                : "GUILD CONTRACT ROUTE";
            return new[]
            {
                new ExpeditionRouteCardView089
                {
                    CardId = operationId + "|STORY",
                    Category = "STORY",
                    VisualCategoryKey = "STORY",
                    VisualResourcePath =
                        "SecondDimension/Art/Board086/CardFaces/CARD_FACE_STORY_089",
                    Title = worldName + " Dispatch",
                    Description = "Follow a witnessed lead and advance the Guild's objective.",
                    RiskLabel = "STEADY",
                    Odds = "ODDS  •  GUARANTEED",
                    OutcomePreview =
                        "SUCCESS • full listed reward  |  NO FAILURE ROLL",
                    SuccessBasisPoints = 10000,
                    RewardPreview = "+3 GUILD / HALL XP  •  STORY PROGRESS",
                    RouteLabel = routeLabel
                },
                new ExpeditionRouteCardView089
                {
                    CardId = operationId + "|CHEST",
                    Category = "CHEST",
                    VisualCategoryKey = "CHEST",
                    VisualResourcePath =
                        "SecondDimension/Art/Board086/CardFaces/CARD_FACE_CHEST_089",
                    Title = "Quartermaster Cache",
                    Description = "Risk a short detour for equippable gear, salvage, and expedition supplies.",
                    RiskLabel = "BALANCED",
                    Odds = "ODDS  •  72%  •  BASE 67% + ITEM 5%",
                    OutcomePreview =
                        "SUCCESS • full listed reward  |  SETBACK • up to 2 XP, no material",
                    SuccessBasisPoints = 7200,
                    RewardPreview = "COMMON–EPIC EQUIPMENT  •  1 MATERIAL  •  +10 GUILD / HALL XP",
                    RouteLabel = "SALVAGE ROUTE"
                },
                new ExpeditionRouteCardView089
                {
                    CardId = operationId + "|CHANCE",
                    Category = "CHANCE",
                    VisualCategoryKey = "CHANCE",
                    VisualResourcePath =
                        "SecondDimension/Art/Board086/CardFaces/CARD_FACE_CHANCE_089",
                    Title = "Wayglass Gamble",
                    Description = "Roll two physical dice and accept the revealed route result.",
                    RiskLabel = "RISKY",
                    Odds = "ODDS  •  55%  •  BASE 50% + MOMENTUM 5%",
                    OutcomePreview =
                        "SUCCESS • full listed reward  |  SETBACK • up to 2 XP, no material, −1 momentum",
                    SuccessBasisPoints = 5500,
                    RewardPreview = "1 MATERIAL  •  +12 GUILD / HALL XP",
                    RouteLabel = "FORTUNE ROUTE"
                }
            };
        }

        private static void RequireProjectedCampaign023BoardCopy084(
            CampaignRegistry023 boardRegistry,
            CampaignRegistry019 chapterRegistry,
            CampaignWorldGatePresentationState023 state,
            string definitionId,
            int width,
            int height)
        {
            Require071(Screen.width == width && Screen.height == height,
                "The packaged C023 board did not render at " + width + "x" + height + ".");
            Require071(boardRegistry.Boards.TryGetValue(definitionId, out var board) &&
                       board != null,
                "The packaged C023 proof lost its authored board: " + definitionId + ".");
            var track = GameObject.Find("Adventure Compact Progress Strip 084");
            var canvas = track?.GetComponentInParent<Canvas>();
            Require071(track != null && canvas != null && track.activeInHierarchy,
                "The production C023 phone-simple room strip did not render.");
            var visibleTexts = canvas.GetComponentsInChildren<Text>(true)
                .Where(value => value != null && value.gameObject.activeInHierarchy)
                .ToArray();
            var visibleCopy = string.Join("\n", visibleTexts.Select(value =>
                value.text ?? string.Empty));
            Require071(!string.IsNullOrWhiteSpace(state.ActiveBoardTitle) &&
                       !string.IsNullOrWhiteSpace(state.BoardObjective) &&
                       state.CurrentNode != null &&
                       !string.IsNullOrWhiteSpace(state.CurrentNode.Title) &&
                       !string.IsNullOrWhiteSpace(state.CurrentNode.Objective) &&
                       !string.IsNullOrWhiteSpace(state.CurrentNode.StoryFlavor),
                "The C023 board projection exposed empty mission copy.");
            var compactBoardObjective084 = CompactProjectedCampaignCopy084(
                state.BoardObjective, 118);
            var routeHeading084 = GameObject.Find(
                    "Expedition route row heading text 089")
                ?.GetComponent<Text>();
            var routeHeadingCopy084 = routeHeading084?.text ?? string.Empty;
            Require071(visibleCopy.IndexOf(state.ActiveBoardTitle,
                               StringComparison.OrdinalIgnoreCase) >= 0 &&
                       visibleCopy.IndexOf(compactBoardObjective084,
                               StringComparison.OrdinalIgnoreCase) >= 0 &&
                       routeHeading084 != null &&
                       routeHeadingCopy084.IndexOf("ROUTE ROUND 1",
                               StringComparison.OrdinalIgnoreCase) >= 0 &&
                       routeHeadingCopy084.IndexOf("10+ CHOICES THIS QUEST",
                               StringComparison.OrdinalIgnoreCase) >= 0,
                "The rendered C023 board omitted its title, compact goal, or three-card route action.");
            var projectedRouteCards084 = (state.RouteCards ??
                                           Array.Empty<ExpeditionRouteCardView089>())
                .Take(3).ToArray();
            var activeProjectedRouteCards084 = projectedRouteCards084.Count(value084 =>
            {
                var card084 = GameObject.Find(
                    "Expedition route card " + value084.CardId + " 089");
                return card084 != null && card084.activeInHierarchy;
            });
            Require071(
                projectedRouteCards084.Length == 3 &&
                activeProjectedRouteCards084 == 3 &&
                CountActiveNamedObjects076("Expedition card face ") == 0 &&
                CountActiveButtonsNamed076("Blind Quest Card Back ") == 3 &&
                FindActiveButtonByName081("Move forward World Gate room 084") == null &&
                visibleCopy.IndexOf("ODDS  •", StringComparison.Ordinal) < 0 &&
                visibleCopy.IndexOf("REWARD  •", StringComparison.Ordinal) < 0,
                "The packaged C023 proof did not present exactly three blind card backs, or disclosed a hidden card's odds/reward before selection.");
            Require071(visibleCopy.IndexOf(state.CurrentNode.Objective,
                               StringComparison.OrdinalIgnoreCase) < 0 &&
                       visibleCopy.IndexOf(state.CurrentNode.StoryFlavor,
                               StringComparison.OrdinalIgnoreCase) < 0,
                "The phone-simple C023 board exposed the face-down room outcome before its flip.");
            Require071(!StringComparer.OrdinalIgnoreCase.Equals(
                           state.BoardObjective, state.CurrentNode.Objective) &&
                       !StringComparer.Ordinal.Equals(
                           state.CurrentNode.StoryFlavor,
                           "The Guild turns this room face up and follows the revealed route."),
                "The C023 board collapsed its board objective and current story into generic copy.");

            var nodes = board.nodes ?? Array.Empty<NodeDto023>();
            var authorityTokens = new List<string>
            {
                board.definitionId,
                board.boardId
            };
            foreach (var node in nodes)
            {
                if (node == null) continue;
                authorityTokens.Add(node.nodeId);
                authorityTokens.Add(node.sourceId);
                authorityTokens.AddRange(node.choiceIds ?? Array.Empty<string>());
            }
            foreach (var token in authorityTokens.Where(value =>
                         !string.IsNullOrWhiteSpace(value)))
                Require071(!BoardAdventureRules084.ContainsRawOpaqueAuthorityToken084(
                               visibleCopy, token),
                    "Raw C023 authority token leaked into player copy: " + token);

            var tiles = state.AdventureTiles ??
                        Array.Empty<AdventureTrackTileView023>();
            Require071(tiles.Count == nodes.Length && tiles.Count >= 10,
                "The authoritative C023 projection did not retain every authored room.");
            var spaces084 = tiles.GroupBy(value => value.SpaceNumber)
                .OrderBy(value => value.Key)
                .ToArray();
            var currentTile084 = tiles.SingleOrDefault(value =>
                StringComparer.Ordinal.Equals(
                    value.State, AdventureBoardTrackProjection084.CurrentState));
            var visibleRoomTexts084 = visibleTexts.Where(value =>
                    value.name.StartsWith("Adventure Compact Room Copy ",
                        StringComparison.Ordinal))
                .ToArray();
            Require071(currentTile084 != null && spaces084.Length >= 5 &&
                       visibleRoomTexts084.Length == Math.Min(5, spaces084.Length),
                "The phone-simple C023 strip did not show its five-room window and current pawn.");
            var currentRoomText084 = visibleRoomTexts084.SingleOrDefault(value =>
                value.name.StartsWith(
                    "Adventure Compact Room Copy " + currentTile084.SpaceNumber + " 084",
                    StringComparison.Ordinal));
            Require071(currentRoomText084 != null &&
                       currentRoomText084.text.IndexOf("YOUR PAWN",
                           StringComparison.Ordinal) >= 0 &&
                       currentRoomText084.text.IndexOf(
                           "ROOM " + currentTile084.SpaceNumber,
                           StringComparison.Ordinal) >= 0 &&
                       currentRoomText084.text.IndexOf("FACE DOWN",
                           StringComparison.Ordinal) >= 0,
                "The compact C023 strip did not keep the current pawn on a face-down room.");
            Require071(visibleRoomTexts084.All(value =>
                           value.text.IndexOf("FACE DOWN",
                               StringComparison.Ordinal) >= 0) &&
                       visibleTexts.All(value =>
                           !value.name.StartsWith("Adventure Authored Room Copy ",
                               StringComparison.Ordinal)),
                "The compact C023 strip leaked a future room or rendered the retired full graph.");
            var progressHeading084 = visibleTexts.SingleOrDefault(value =>
                value.name.StartsWith("Adventure Compact Progress Heading 084",
                    StringComparison.Ordinal));
            var maximumSpace084 = spaces084.Max(value => value.Key);
            Require071(progressHeading084 != null &&
                       progressHeading084.text.IndexOf(
                           "ROOM " + currentTile084.SpaceNumber + " OF " + maximumSpace084,
                           StringComparison.Ordinal) >= 0 &&
                       progressHeading084.text.IndexOf("QUEST MAP",
                           StringComparison.OrdinalIgnoreCase) >= 0 &&
                       progressHeading084.text.IndexOf("BRANCHING PATH",
                           StringComparison.Ordinal) >= 0,
                "The compact C023 strip did not explain its branching quest-map progress.");
            Require071(visibleCopy.IndexOf(
                           "ROOM  " + currentTile084.SpaceNumber + " OF " +
                           maximumSpace084 + "  •  SUPPLY",
                           StringComparison.Ordinal) >= 0,
                "The C023 mission brief contradicted the playable room count shown on its compact strip.");

            ChapterNarrative019 chapter = null;
            if (StringComparer.Ordinal.Equals(board.operationKind, "CHAPTER"))
                chapterRegistry.Chapters.TryGetValue(board.definitionId, out chapter);
            var worldName = boardRegistry.Standing.TryGetValue(
                    board.worldId, out var standing) && standing != null
                ? standing.displayName
                : "Guild World";
            foreach (var future in nodes.Where(value => value != null &&
                         !StringComparer.Ordinal.Equals(
                             value.nodeId, state.CurrentNode.NodeId)))
            {
                var projectedFuture = AdventureBoardNarrativeProjection084.Project(
                    board, future, chapter, worldName);
                Require071(visibleCopy.IndexOf(projectedFuture.Title,
                               StringComparison.OrdinalIgnoreCase) < 0,
                    "A future C023 room title leaked before its tile was revealed: " +
                    projectedFuture.Title);
            }
        }

        private static string CompactProjectedCampaignCopy084(
            string copy,
            int maximumLength)
        {
            var value = (copy ?? string.Empty).Replace('\n', ' ').Trim();
            while (value.Contains("  ")) value = value.Replace("  ", " ");
            if (value.Length <= maximumLength) return value;
            var cut = value.LastIndexOf(' ', maximumLength);
            if (cut < maximumLength / 2) cut = maximumLength;
            return value.Substring(0, cut).TrimEnd(' ', '.', ',', ';', ':') + "…";
        }

        private IEnumerator VerifyVisibleUnionPlannerDragDrop078()
        {
            var slots078 = UnityEngine.Object
                .FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                .Where(value078 => value078 != null &&
                                   value078.gameObject.activeInHierarchy &&
                                   value078.name.StartsWith(
                                       "Union Planner Member Slot ",
                                       StringComparison.Ordinal))
                .OrderBy(value078 => value078.name, StringComparer.Ordinal)
                .ToArray();
            Require071(slots078.Length == NormalUnionPlanRules.MaximumMembersPerUnion &&
                       slots078.Length == 6,
                "The shipping Union planner did not visibly render all six member slots.");
            for (var slotIndex078 = 0; slotIndex078 < slots078.Length; slotIndex078++)
            {
                Require071(StringComparer.Ordinal.Equals(
                               slots078[slotIndex078].name,
                               "Union Planner Member Slot " + slotIndex078 + " 074") &&
                           slots078[slotIndex078].GetComponent<UnionPlannerDropTarget074>() != null,
                    "Visible Union slot " + slotIndex078 +
                    " did not expose the shipping drop-target component.");
            }

            var source078 = slots078[0].GetComponent<UnionPlannerRecruitDrag074>();
            var target078 = slots078[1].GetComponent<UnionPlannerDropTarget074>();
            Require071(source078 != null && source078.CanDrag &&
                       target078 != null && target078.IsAvailableForAssigned &&
                       !string.IsNullOrWhiteSpace(source078.RecruitId),
                "The first two visible Union slots did not expose a reversible shipping drag/drop path.");
            var movedRecruitId078 = source078.RecruitId;
            var baselinePlan078 = CurrentUnionPlanFingerprint078();
            var baselineRoster078 = string.Join("|", (_coordinator.State.Recruits ??
                                                       Array.Empty<M1RecruitLoadoutView>())
                .Where(value078 => value078 != null)
                .Select(value078 => value078.RecruitId)
                .OrderBy(value078 => value078, StringComparer.Ordinal));
            var baselineTreasuryXp078 = _coordinator.State.TreasuryXp;
            yield return Capture071("union_planner_six_slots_before_drag");

            var moveEvent078 = new PointerEventData(EventSystem.current)
            {
                pointerDrag = source078.gameObject
            };
            source078.OnBeginDrag(moveEvent078);
            Require071(ReferenceEquals(UnionPlannerRecruitDrag074.ActiveDrag, source078) &&
                       GameObject.Find("Union Planner Drag Ghost 074") != null,
                "Beginning the shipping Union drag did not expose its drag ghost and active source.");
            target078.OnDrop(moveEvent078);
            Require071(source078.DropCompleted && target078.LastDropAccepted,
                "The shipping Union drop target rejected a legal assigned-member swap.");
            yield return null;
            yield return null;
            Require071(!StringComparer.Ordinal.Equals(
                           CurrentUnionPlanFingerprint078(),
                           baselinePlan078),
                "The accepted shipping drag/drop did not change the authoritative Union plan.");
            yield return Capture071("union_planner_after_shipping_drop");

            var restoreSource078 = UnityEngine.Object
                .FindObjectsByType<UnionPlannerRecruitDrag074>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value078 => value078 != null && value078.CanDrag &&
                                             value078.gameObject.activeInHierarchy &&
                                             StringComparer.Ordinal.Equals(
                                                 value078.RecruitId,
                                                 movedRecruitId078));
            var restoreTarget078 = GameObject.Find("Union Planner Member Slot 0 074")
                ?.GetComponent<UnionPlannerDropTarget074>();
            Require071(restoreSource078 != null && restoreTarget078 != null &&
                       restoreTarget078.IsAvailableForAssigned,
                "The changed Union plan did not retain a reversible shipping drop path.");
            var restoreEvent078 = new PointerEventData(EventSystem.current)
            {
                pointerDrag = restoreSource078.gameObject
            };
            restoreSource078.OnBeginDrag(restoreEvent078);
            restoreTarget078.OnDrop(restoreEvent078);
            Require071(restoreSource078.DropCompleted && restoreTarget078.LastDropAccepted,
                "The shipping Union drop target could not restore the authored story plan.");
            yield return null;
            yield return null;
            _report.unionPlannerDragDropVerified =
                StringComparer.Ordinal.Equals(
                    CurrentUnionPlanFingerprint078(),
                    baselinePlan078) &&
                StringComparer.Ordinal.Equals(
                    string.Join("|", (_coordinator.State.Recruits ??
                                      Array.Empty<M1RecruitLoadoutView>())
                        .Where(value078 => value078 != null)
                        .Select(value078 => value078.RecruitId)
                        .OrderBy(value078 => value078, StringComparer.Ordinal)),
                    baselineRoster078) &&
                _coordinator.State.TreasuryXp == baselineTreasuryXp078 &&
                UnionPlannerRecruitDrag074.ActiveDrag == null;
            Require071(_report.unionPlannerDragDropVerified,
                "The reversible drag/drop proof did not preserve the authored story Union plan.");
        }

        private string CurrentUnionPlanFingerprint078()
        {
            return string.Join("|", (_coordinator?.State?.Unions ??
                                      Array.Empty<M1UnionView>())
                .Where(value078 => value078 != null)
                .OrderBy(value078 => value078.Index)
                .Select(value078 => value078.Index + ":" +
                                    string.Join(",", value078.MemberRecruitIds ??
                                                     Array.Empty<string>()) + ":" +
                                    (value078.FormationId ?? string.Empty) + ":" +
                                    (value078.DoctrineId ?? string.Empty)));
        }

        private IEnumerator EquipFirstRecoveredLoot078()
        {
            var armory = UnityEngine.Object
                .FindFirstObjectByType<CompactInventoryPresenter069>();
            Require071(armory != null &&
                       GameObject.Find(CompactInventoryPresenter069.RootObjectName) != null,
                "The Hall armory did not open its compact character equipment view.");

            string recruitId = null;
            string slotId = null;
            string itemId = null;
            string itemName = null;
            foreach (var recruit in _coordinator.State.Recruits ??
                                      Array.Empty<M1RecruitLoadoutView>())
            {
                if (recruit == null) continue;
                foreach (var slot in recruit.Slots ?? Array.Empty<M1EquipmentSlotView>())
                {
                    if (slot == null) continue;
                    var choice = (slot.Choices ?? Array.Empty<M1EquipmentChoiceView>())
                        .FirstOrDefault(value =>
                            value != null &&
                            CompactInventoryPresenter069.IsNewVersion70Loot069(value) &&
                            CompactInventoryPresenter069.CanEquipSelection(slot, value));
                    if (choice == null) continue;
                    recruitId = recruit.RecruitId;
                    slotId = slot.SlotId;
                    itemId = choice.ItemId;
                    itemName = choice.DisplayName;
                    break;
                }
                if (!string.IsNullOrWhiteSpace(itemId)) break;
            }

            Require071(!string.IsNullOrWhiteSpace(recruitId) &&
                       !string.IsNullOrWhiteSpace(slotId) &&
                       !string.IsNullOrWhiteSpace(itemId),
                "The four certified battles produced no compatible unequipped armory reward.");
            armory.Refresh(recruitId);
            yield return null;
            Require071(armory.TrySelectSlot(slotId),
                "The armory could not select the compatible slot for recovered loot.");
            Require071(armory.TrySelectItem(itemId),
                "The armory could not select the recovered item by its committed identity.");
            yield return null;
            Require071(armory.EquipButtonForTests != null &&
                       armory.EquipButtonForTests.interactable,
                "The recovered item was visible but the real EQUIP action was unavailable.");
            yield return Capture071("armory_recovered_loot_comparison");
            armory.EquipButtonForTests.onClick.Invoke();
            yield return null;
            yield return null;

            var equipped = (_coordinator.State.Recruits ??
                            Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, recruitId))
                ?.Slots?.FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.SlotId, slotId));
            _report.manualLootEquippedVerified = equipped != null &&
                                                  StringComparer.Ordinal.Equals(
                                                      equipped.EquippedItemId,
                                                      itemId);
            Require071(_report.manualLootEquippedVerified,
                "Equipping " + (itemName ?? "recovered loot") +
                " did not persist on the selected Guild member.");

            var back = GameObject.Find("Back To Hall 069")?.GetComponent<Button>();
            Require071(back != null && back.gameObject.activeInHierarchy,
                "The armory did not expose a visible return to the Guild Hall.");
            back.onClick.Invoke();
            yield return null;
            yield return null;
        }

        private void BindPackagedBuildIdentity076()
        {
            var dataRoot = Path.GetFullPath(Application.dataPath);
            var playerRoot = Directory.GetParent(dataRoot)?.FullName;
            Require071(!string.IsNullOrWhiteSpace(playerRoot),
                "The packaged player root could not be resolved.");
            playerRoot = Path.GetFullPath(playerRoot);

            var buildIdPath = Path.Combine(playerRoot, BuildIdFileName);
            var completePath = Path.Combine(playerRoot, BuildCompleteFileName);
            var manifestPath = Path.Combine(playerRoot, BuildManifestFileName);
            RequireNonEmptyFile076(buildIdPath);
            RequireNonEmptyFile076(completePath);
            RequireNonEmptyFile076(manifestPath);
            var buildId = File.ReadAllText(buildIdPath).Trim();
            Require071(StringComparer.Ordinal.Equals(buildId, ExpectedBuildId),
                "Packaged build ID is stale or unexpected: " + buildId + ".");
            var completion = File.ReadAllText(completePath);
            Require071(completion.IndexOf(ExpectedBuildId, StringComparison.Ordinal) >= 0 &&
                       completion.IndexOf("BUILD COMPLETE", StringComparison.Ordinal) >= 0,
                "The packaged build completion marker is stale or malformed.");

            var manifest = ReadBuildManifest076(manifestPath, playerRoot);
            var verifiedHashes = VerifyClosedWorldBuildManifest081(playerRoot, manifest);
            var dataFolder = Path.GetFileName(dataRoot);
            _report.buildId = buildId;
            _report.buildManifestEntryCount = manifest.Count;
            _report.buildManifestSha256 = ComputeFileSha256076(manifestPath);
            _report.executableSha256 = RequireVerifiedBuildHash081(
                ExecutableName, verifiedHashes);
            _report.playerAssemblySha256 = RequireVerifiedBuildHash081(
                dataFolder + "/Managed/Assembly-CSharp.dll", verifiedHashes);
            _report.resourcesSha256 = RequireVerifiedBuildHash081(
                dataFolder + "/resources.assets", verifiedHashes);
            RequireVerifiedBuildHash081(BuildIdFileName, verifiedHashes);
            RequireVerifiedBuildHash081(BuildCompleteFileName, verifiedHashes);
            _report.buildIdentityVerified = true;
        }

        public static IReadOnlyDictionary<string, string>
            VerifyClosedWorldBuildManifestForVerification081(
                string playerRoot,
                string manifestPath)
        {
            if (string.IsNullOrWhiteSpace(playerRoot))
                throw new ArgumentException("Packaged player root is required.", nameof(playerRoot));
            if (string.IsNullOrWhiteSpace(manifestPath))
                throw new ArgumentException("Build manifest path is required.", nameof(manifestPath));
            var normalizedRoot = Path.GetFullPath(playerRoot);
            var normalizedManifest = Path.GetFullPath(manifestPath);
            Require071(IsSameOrChild071(normalizedManifest, normalizedRoot),
                "Build hash manifest must be inside the packaged player root.");
            RequireNonEmptyFile076(normalizedManifest);
            return VerifyClosedWorldBuildManifest081(
                normalizedRoot,
                ReadBuildManifest076(normalizedManifest, normalizedRoot));
        }

        private static IReadOnlyDictionary<string, string> ReadBuildManifest076(
            string manifestPath,
            string playerRoot)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var rootPrefix = Path.GetFullPath(playerRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                             Path.DirectorySeparatorChar;
            foreach (var rawLine in File.ReadAllLines(manifestPath))
            {
                var line = rawLine ?? string.Empty;
                Require071(line.Length > 66 && line[64] == ' ' && line[65] == ' ',
                    "Build hash manifest contains a malformed line.");
                var hash = line.Substring(0, 64);
                var relative = line.Substring(66).Replace('\\', '/');
                Require071(IsSha256076(hash) && !string.IsNullOrWhiteSpace(relative) &&
                           !Path.IsPathRooted(relative),
                    "Build hash manifest contains an invalid hash or path.");
                var fullPath = Path.GetFullPath(Path.Combine(
                    playerRoot,
                    relative.Replace('/', Path.DirectorySeparatorChar)));
                Require071(fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase),
                    "Build hash manifest path escapes the packaged player root: " + relative + ".");
                Require071(!result.ContainsKey(relative),
                    "Build hash manifest contains a duplicate path: " + relative + ".");
                result.Add(relative, hash);
            }
            Require071(result.Count > 0 && !result.ContainsKey(BuildManifestFileName),
                "Build hash manifest is empty or recursively lists itself.");
            return result;
        }

        private static IReadOnlyDictionary<string, string> VerifyClosedWorldBuildManifest081(
            string playerRoot,
            IReadOnlyDictionary<string, string> manifest)
        {
            var normalizedRoot = Path.GetFullPath(playerRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rootPrefix = normalizedRoot + Path.DirectorySeparatorChar;
            var verified = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in manifest.OrderBy(value => value.Key, StringComparer.OrdinalIgnoreCase))
            {
                var relative = entry.Key.Replace('\\', '/');
                var fullPath = Path.GetFullPath(Path.Combine(
                    normalizedRoot,
                    relative.Replace('/', Path.DirectorySeparatorChar)));
                Require071(fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase),
                    "Build hash manifest path escapes the packaged player root: " + relative + ".");
                Require071(File.Exists(fullPath),
                    "Build hash manifest lists a missing packaged file: " + relative + ".");
                var actual = ComputeFileSha256076(fullPath);
                Require071(StringComparer.Ordinal.Equals(actual, entry.Value),
                    "Packaged file hash does not match the build manifest: " + relative + ".");
                verified.Add(relative, actual);
            }

            var packagedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var fullPath in Directory.EnumerateFiles(
                         normalizedRoot,
                         "*",
                         SearchOption.AllDirectories))
            {
                var normalizedFullPath = Path.GetFullPath(fullPath);
                Require071(normalizedFullPath.StartsWith(
                               rootPrefix,
                               StringComparison.OrdinalIgnoreCase),
                    "Packaged file enumeration escaped the player root: " + fullPath + ".");
                var relative = normalizedFullPath.Substring(rootPrefix.Length)
                    .Replace('\\', '/');
                if (StringComparer.OrdinalIgnoreCase.Equals(relative, BuildManifestFileName))
                    continue;
                packagedFiles.Add(relative);
            }

            var missingFromDisk = manifest.Keys
                .Where(relative => !packagedFiles.Contains(relative))
                .OrderBy(relative => relative, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Require071(missingFromDisk.Length == 0,
                "Build hash manifest lists files absent from the packaged player: " +
                string.Join(", ", missingFromDisk) + ".");
            var unlistedOnDisk = packagedFiles
                .Where(relative => !manifest.ContainsKey(relative))
                .OrderBy(relative => relative, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            Require071(unlistedOnDisk.Length == 0,
                "Packaged player contains files missing from BUILD_SHA256.txt: " +
                string.Join(", ", unlistedOnDisk) + ".");
            Require071(verified.Count == manifest.Count && verified.Count == packagedFiles.Count,
                "Closed-world build hash audit did not cover every packaged file exactly once.");
            return verified;
        }

        private static string RequireVerifiedBuildHash081(
            string relativePath,
            IReadOnlyDictionary<string, string> verifiedHashes)
        {
            var normalized = (relativePath ?? string.Empty).Replace('\\', '/');
            Require071(verifiedHashes.TryGetValue(normalized, out var verified),
                "Build hash manifest is missing required packaged file: " + normalized + ".");
            return verified;
        }

        private static string ComputeFileSha256076(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", string.Empty);
        }

        private static bool IsSha256076(string value) =>
            value != null && value.Length == 64 && value.All(Uri.IsHexDigit);

        private static void RequireNonEmptyFile076(string path)
        {
            Require071(File.Exists(path) && new FileInfo(path).Length > 0,
                "Required packaged build file is missing or empty: " + path + ".");
        }

        private static IEnumerator RunGuarded071(IEnumerator root, Action<Exception> onFailure)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Peek();
                object yielded = null;
                bool moved;
                try
                {
                    moved = current.MoveNext();
                    if (moved) yielded = current.Current;
                }
                catch (Exception exception)
                {
                    onFailure(exception);
                    yield break;
                }

                if (!moved)
                {
                    stack.Pop();
                    continue;
                }
                if (yielded is IEnumerator nested)
                {
                    stack.Push(nested);
                    continue;
                }
                yield return yielded;
            }
        }

        private IEnumerator WaitForPresenter071()
        {
            for (var frame = 0; frame < 900; frame++)
            {
                _presenter = UnityEngine.Object.FindFirstObjectByType<M1FlowPresenter>();
                if (_presenter != null) yield break;
                yield return null;
            }
        }

        private void VerifyFirstHourArtPresentation076()
        {
            var recipes076 = FirstHourArtRegistry071.Load().Recipes;
            Require071(recipes076.Count == FirstHourArtRegistry071.ExpectedRecipeCount &&
                       recipes076.Count == 120,
                "The first-hour 120-Art recipe catalog is incomplete.");
            foreach (var recipe076 in recipes076)
            {
                Require071(BattleArtRuntimeRegistry011.TryResolveExactFirstHourProfile076(
                        recipe076.artId,
                        BattleBeatFamily.CombatArt,
                        out var profile076) && profile076 != null,
                    "The shipping 072 battle route has no exact profile for " + recipe076.artId + ".");
                Require071(profile076.exactFirstHourRecipe &&
                           StringComparer.Ordinal.Equals(profile076.artId, recipe076.artId) &&
                           StringComparer.Ordinal.Equals(profile076.displayName, recipe076.displayName) &&
                           StringComparer.Ordinal.Equals(profile076.presentationRecipeId, recipe076.clipId) &&
                           StringComparer.Ordinal.Equals(profile076.presentationTreeId, recipe076.treeId) &&
                           StringComparer.Ordinal.Equals(profile076.presentationVfxSignature, recipe076.vfxSignature) &&
                           StringComparer.Ordinal.Equals(profile076.presentationSfxSignature, recipe076.sfxSignature) &&
                           StringComparer.Ordinal.Equals(profile076.presentationMotionSignature, recipe076.motionSignature),
                    "The live presentation profile drifted from the exact recipe for " + recipe076.artId + ".");
                Require071(!string.IsNullOrWhiteSpace(profile076.startAudioResourcePath) &&
                           Resources.Load<AudioClip>(profile076.startAudioResourcePath) != null &&
                           !string.IsNullOrWhiteSpace(profile076.impactAudioResourcePath) &&
                           Resources.Load<AudioClip>(profile076.impactAudioResourcePath) != null,
                    "The live first-hour Art has missing phased audio: " + recipe076.artId + ".");
                Require071(BattleArtRuntimeRegistry011.TryResolveSemanticIcon076(
                        recipe076.artId,
                        BattleBeatFamily.CombatArt,
                        false,
                        out var iconId076,
                        out var icon076) &&
                           !string.IsNullOrWhiteSpace(iconId076) && icon076 != null,
                    "The live first-hour Art has no semantic forecast icon: " + recipe076.artId + ".");
            }
            _report.firstHourArtPresentationVerified = true;
        }

        private void RequireExpeditionBoardNode078(string expectedNodeId078)
        {
            Require071(GameObject.Find("Board Quest 081") != null,
                "The compact Board Quest is not visible at " + expectedNodeId078 + ".");
            Require071(_coordinator?.GuildCity017D?.Expedition != null &&
                       StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentNodeId,
                           expectedNodeId078),
                "Expected expedition node " + expectedNodeId078 + " but found " +
                (_coordinator?.GuildCity017D?.Expedition?.CurrentNodeId ?? "none") + ".");
            Require071(UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>() == null,
                "The retired side-scroll corridor reopened during board play.");
        }

        private IEnumerator CommitExpeditionPrimaryOrder078()
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            var boardView081 = _coordinator?.GuildCity017D == null
                ? null
                : ExpeditionBoardProjection074.Build(_coordinator.GuildCity017D);
            // CaptureScreenshot writes asynchronously. The automatic roll can
            // commit while the pre-order evidence PNG is being released, which
            // advances the projection from ResolveCheck to ChooseRoute before
            // this coroutine resumes. A saved check at this same node is the
            // authoritative half of that race; still require the live physical
            // dice-result surface below so the certification cannot become a
            // logic-only pass.
            if (expedition?.HasCommittedCheckAtCurrentNode == true ||
                (expedition?.CurrentEventUsesCommitted2d6 == true &&
                 boardView081?.ActionKind == ExpeditionBoardActionKind074.ResolveCheck))
            {
                yield return WaitForAutomaticBoardQuestDice081(
                    "Board Quest check at " + expedition.CurrentNodeId);
                yield break;
            }

            // A newly flipped room intentionally holds its action until the card
            // reveal finishes. Wait for that same player-facing lock instead of
            // assuming the next battle/event button exists two render frames later.
            yield return WaitForInteractableButtonByName076(
                "Expedition Primary Context Action 074",
                12f);
            var primary = GameObject.Find("Expedition Primary Context Action 074")
                ?.GetComponent<Button>();
            Require071(primary != null && primary.gameObject.activeInHierarchy &&
                       primary.interactable,
                "The expedition screen did not expose one usable primary order at " +
                (_coordinator?.GuildCity017D?.Expedition?.CurrentNodeId ?? "none") + ".");
            primary.onClick.Invoke();
            yield return null;
            yield return null;
        }

        private IEnumerator MoveExpeditionBoard078(string destinationNodeId078)
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            Require071(expedition != null &&
                       (expedition.LinkedNodeIds ?? Array.Empty<string>()).Contains(
                           destinationNodeId078,
                           StringComparer.Ordinal),
                "Destination " + destinationNodeId078 + " is not linked from " +
                (expedition?.CurrentNodeId ?? "none") + ".");
            var departureView081 = ExpeditionBoardProjection074.Build(
                _coordinator.GuildCity017D);
            var questCards090 = (_coordinator.GuildCity017D.QuestCards090 ??
                                 Array.Empty<GuildQuestCardView090>()).ToArray();
            var selectedCard090 = SelectQuestCardForDestinationForVerification090(
                questCards090,
                departureView081?.Nodes,
                destinationNodeId078);
            Require071(selectedCard090 != null,
                "The three-card quest row did not offer a usable card leading to " +
                destinationNodeId078 + ".");
            var objectiveFlagsBefore081 = new HashSet<string>(
                expedition?.ObjectiveFlags ?? Array.Empty<string>(),
                StringComparer.Ordinal);
            yield return WaitForQuestCardDraft090(
                "the route from " + expedition.CurrentNodeId + " to " +
                destinationNodeId078,
                3);
            var renderedQuestCards090 = ActiveQuestCardButtons090();
            Require071(renderedQuestCards090.Length == 3 &&
                       renderedQuestCards090.All(value => value.interactable),
                "The Board Quest did not present exactly three blind pick controls.");
            var selectedIndex091 = Array.FindIndex(questCards090,
                value => value != null && value.CardId == selectedCard090.CardId);
            yield return RevealBlindDraftCardForVerification091(
                "Board Quest Card Slot " + selectedIndex091 + " 090",
                "Board Quest " + selectedCard090.CardId);
            var choice = GameObject.Find(
                    "Choose Board Quest Card " + selectedCard090.CardId + " 090")
                ?.GetComponent<Button>();
            Require071(choice != null,
                "The Board Quest did not render the chosen quest card for " +
                destinationNodeId078 + ".");
            Require071(choice.gameObject.activeInHierarchy && choice.interactable,
                "The quest card leading to " + destinationNodeId078 +
                " was visible but could not be chosen.");
            Require071(ActiveQuestCardButtons090().Length == 0,
                "The two passed cards still accepted blind input after selection.");
            Require071(GameObject.Find(
                           "Select Expedition Destination " + destinationNodeId078 + " 074") == null,
                "A retired destination-choice button leaked into the three-card Board Quest.");
            choice.Select();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(choice.gameObject);
            Require071(EventSystem.current != null &&
                       EventSystem.current.currentSelectedGameObject == choice.gameObject,
                "Controller focus could not select the quest card leading to " +
                destinationNodeId078 + ".");
            choice.onClick.Invoke();
            if (!_chestCaptureVerified092 && StringComparer.Ordinal.Equals(selectedCard090.Category, "CHEST"))
                yield return CaptureCommittedQuestChest092(selectedCard090.CardId);
            yield return null;
            yield return null;
            RequireExpeditionBoardNode078(destinationNodeId078);
            yield return WaitForBoardQuestCardResult090(
                selectedCard090.CardId,
                destinationNodeId078);

            var arrivedView081 = ExpeditionBoardProjection074.Build(
                _coordinator.GuildCity017D);
            var arrivedNode081 = (arrivedView081?.Nodes ??
                                  Array.Empty<ExpeditionBoardNodeView074>())
                .FirstOrDefault(value => value != null && value.IsCurrent &&
                    StringComparer.Ordinal.Equals(value.NodeId, destinationNodeId078));
            Require071(arrivedNode081 != null,
                "The Board Quest projection did not mark " + destinationNodeId078 +
                " as the current revealed room.");
            var roomKind081 = BoardQuestRules081.RoomKind081(
                _coordinator.GuildCity017D,
                arrivedNode081);
            var roomRewardFlag081 = GuildCityExpeditionService017D.BoardRoomRewardFlag081(
                destinationNodeId078,
                roomKind081);
            var objectiveFlagsAfter081 =
                _coordinator.GuildCity017D.Expedition.ObjectiveFlags ??
                Array.Empty<string>();
            Require071(!objectiveFlagsBefore081.Contains(roomRewardFlag081) &&
                       objectiveFlagsAfter081.Count(value => StringComparer.Ordinal.Equals(
                           value,
                           roomRewardFlag081)) == 1,
                "Moving the pawn did not commit exactly one deterministic room reveal for " +
                destinationNodeId078 + ".");
            var nextQuestCardDraft090 =
                GameObject.Find("Board Quest Three Card Draft 090") != null;
            if (!nextQuestCardDraft090)
                RequireNamedVisibleTextEquals076(
                    "Board Quest Place 081",
                    "CARD REVEALED");
            BoardTowerRoom001 roomModule081 = null;
            if (BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001(roomKind081))
            {
                var roomModuleId081 = BoardTowerEnhancementRules001.SelectP0RoomModuleId001(
                    _coordinator.GuildCity017D.Expedition.ExpeditionId,
                    destinationNodeId078,
                    roomKind081);
                var roomModuleFlag081 = BoardTowerEnhancementRules001.SelectedRoomFlag001(
                    destinationNodeId078,
                    roomModuleId081);
                Require071(objectiveFlagsAfter081.Count(value =>
                        StringComparer.Ordinal.Equals(value, roomModuleFlag081)) == 1,
                    "The compatible Board Quest room did not commit its exact Tower module flag once.");
                roomModule081 = BoardTowerEnhancementCatalog001.LoadFromResources()
                    .CommittedBoardRoom001(
                        _coordinator.GuildCity017D.Expedition.ExpeditionId,
                        destinationNodeId078,
                        roomKind081,
                        objectiveFlagsAfter081);
                Require071(roomModule081 != null,
                    "The compatible Board Quest room did not resolve its committed Tower enhancement module.");
                Require071(StringComparer.Ordinal.Equals(roomModule081.roomId, roomModuleId081),
                    "The resolved Board Quest Tower module did not match its deterministic committed ID.");
            }
            var expectedRewardCopy081 =
                _coordinator.GuildCity017D.Expedition.HasCommittedCheckAtCurrentNode
                    ? "FATE RESOLVED  •  REWARD SAVED  •  MOVE AGAIN"
                    : BoardQuestRules081.RoomReward081(roomKind081);
            if (!nextQuestCardDraft090)
                RequireNamedVisibleTextEquals076(
                    "Board Quest Revealed Reward Copy 081",
                    expectedRewardCopy081);
        }

        public static GuildQuestCardView090
            SelectQuestCardForDestinationForVerification090(
                IReadOnlyList<GuildQuestCardView090> cards,
                IReadOnlyList<ExpeditionBoardNodeView074> nodes,
                string destinationNodeId)
        {
            if (cards == null || nodes == null ||
                string.IsNullOrWhiteSpace(destinationNodeId))
                return null;
            var destination = nodes.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.NodeId, destinationNodeId));
            if (destination == null)
                return null;

            // The smoke follows the authored story route while exercising the
            // same physical pick and revealed action as a player. The harness
            // alone inspects authority IDs to preserve the certified route; no
            // title/category/reward is disclosed to the player before their pick.
            // Prefer reward/support cards
            // so optional BATTLE and RECRUIT cards cannot silently change the
            // certified five-battle order or twenty-member story roster.
            return cards
                .Where(value => value != null && value.CanChoose &&
                    !StringComparer.Ordinal.Equals(value.Category, "BATTLE") &&
                    !StringComparer.Ordinal.Equals(value.Category, "RECRUIT") &&
                    (StringComparer.Ordinal.Equals(
                         value.DestinationNodeId,
                         destinationNodeId) ||
                     (string.IsNullOrWhiteSpace(value.DestinationNodeId) &&
                      !string.IsNullOrWhiteSpace(destination.DisplayName) &&
                      StringComparer.Ordinal.Equals(
                          value.DestinationLabel,
                          destination.DisplayName))))
                .OrderBy(QuestCardSmokePreference090)
                .FirstOrDefault();
        }

        private static int QuestCardSmokePreference090(
            GuildQuestCardView090 card)
        {
            switch ((card?.Category ?? string.Empty).ToUpperInvariant())
            {
                case "XP": return 0;
                case "BOON": return 1;
                case "CHEST": return 2;
                case "FATE": return 3;
                case "SCAR": return 4;
                case "MERCHANT": return 5;
                case "RECRUIT": return 90;
                case "BATTLE": return 100;
                default: return 50;
            }
        }

        private static Button[] ActiveQuestCardButtons090()
        {
            var draft = GameObject.Find("Board Quest Three Card Draft 090");
            return draft == null
                ? Array.Empty<Button>()
                : draft.GetComponentsInChildren<Button>(true)
                    .Where(value => value != null &&
                        value.gameObject.activeInHierarchy &&
                        value.gameObject.name.StartsWith(
                            "Blind Quest Card Back ",
                            StringComparison.Ordinal))
                    .OrderBy(value => value.gameObject.name,
                        StringComparer.Ordinal)
                    .ToArray();
        }

        private IEnumerator WaitForQuestCardDraft090(
            string label,
            int expectedInteractableCount)
        {
            var deadline090 = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < deadline090)
            {
                var cards090 = ActiveQuestCardButtons090();
                if (cards090.Length == 3 &&
                    cards090.Count(value => value.interactable) ==
                    expectedInteractableCount)
                    yield break;
                yield return null;
            }

            var finalCards090 = ActiveQuestCardButtons090();
            Require071(false,
                "The Board Quest did not finish dealing exactly three blind cards for " +
                label + "; rendered " + finalCards090.Length +
                " cards with " + finalCards090.Count(value => value.interactable) +
                " usable.");
        }

        private IEnumerator WaitForBoardQuestCardResult090(
            string cardId,
            string destinationNodeId)
        {
            var resultDeadline090 = Time.realtimeSinceStartup + 12f;
            GameObject result090 = null;
            while (Time.realtimeSinceStartup < resultDeadline090)
            {
                result090 = GameObject.Find("Board Quest Card Resolution 090");
                if (result090 != null && result090.activeInHierarchy) break;
                yield return null;
            }
            Require071(result090 != null && result090.activeInHierarchy,
                "Choosing quest card " + cardId + " moved to " +
                destinationNodeId + " without showing its saved card result.");
            var resolutionTexts090 = result090
                .GetComponentsInChildren<Text>(includeInactive: false)
                .Where(value => value != null &&
                    value.gameObject.activeInHierarchy &&
                    !string.IsNullOrWhiteSpace(value.text))
                .ToArray();
            var resolvedTitleVisible090 = resolutionTexts090.Any(value =>
                value.gameObject.name.StartsWith(
                    "Board Quest Resolved Card Title 090",
                    StringComparison.Ordinal));
            var resolvedRewardVisible090 = resolutionTexts090.Any(value =>
                value.gameObject.name.StartsWith(
                    "Board Quest Resolved Card Reward 090",
                    StringComparison.Ordinal));
            Require071(resolvedTitleVisible090 && resolvedRewardVisible090,
                "The resolved quest card did not visibly show both its identity and reward.");

            // Reduced-motion play deliberately holds the result until CONTINUE;
            // normal motion dismisses it after the card flip, reward and pawn
            // travel have all been visible. Support both real shipping paths.
            var acknowledge090 = GameObject.Find(
                    "Acknowledge Board Quest Card Resolution 090")
                ?.GetComponent<Button>();
            if (acknowledge090 != null && acknowledge090.interactable)
            {
                acknowledge090.onClick.Invoke();
                yield return null;
                yield return null;
            }
            else
            {
                var settleDeadline090 = Time.realtimeSinceStartup + 12f;
                while (Time.realtimeSinceStartup < settleDeadline090 &&
                       GameObject.Find("Board Quest Card Resolution 090") != null)
                    yield return null;
            }
            Require071(GameObject.Find("Board Quest Card Resolution 090") == null,
                "The quest-card result did not settle before the smoke continued from " +
                destinationNodeId + ".");
        }

        private IEnumerator WaitForAutomaticBoardQuestDice081(string label)
        {
            var deadline081 = Time.realtimeSinceStartup + 12f;
            var fallbackClicked081 = false;
            while (Time.realtimeSinceStartup < deadline081)
            {
                var expedition081 = _coordinator?.GuildCity017D?.Expedition;
                if (expedition081 != null &&
                    expedition081.HasCommittedCheckAtCurrentNode &&
                    expedition081.ResolutionComplete &&
                    GameObject.Find("Board Quest Dice Result 081") != null)
                    yield break;

                var primary081 = GameObject.Find("Expedition Primary Context Action 074")
                    ?.GetComponent<Button>();
                var primaryCopy081 = primary081 == null
                    ? string.Empty
                    : string.Join("\n", primary081.GetComponentsInChildren<Text>(true)
                        .Where(value => value != null)
                        .Select(value => value.text ?? string.Empty));
                if (!fallbackClicked081 && primary081 != null &&
                    primary081.gameObject.activeInHierarchy &&
                    primary081.interactable &&
                    primaryCopy081.IndexOf(
                        "ROLL 2D6", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    primaryCopy081.IndexOf(
                        "AUTO-RESOLVE", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    primary081.onClick.Invoke();
                    fallbackClicked081 = true;
                }
                yield return null;
            }

            Require071(false,
                label + " did not automatically settle and save its visible 2d6 result.");
        }

        private void RequireQuestCardRounds090(int expectedRounds, string label)
        {
            var objectiveFlags = _coordinator?.GuildCity017D?.Expedition
                                     ?.ObjectiveFlags ?? Array.Empty<string>();
            var receipts = objectiveFlags
                .Where(value => (value ?? string.Empty).StartsWith(
                    GuildCityExpeditionService017D.QuestCardReceiptPrefix090,
                    StringComparison.Ordinal))
                .ToArray();
            Require071(
                GuildCityExpeditionService017D.QuestCardRoundCount090(
                    objectiveFlags) == expectedRounds &&
                receipts.Length == expectedRounds &&
                receipts.Distinct(StringComparer.Ordinal).Count() ==
                expectedRounds,
                label + " completed " + receipts.Length +
                " distinct saved quest-card rounds; expected exactly " +
                expectedRounds + " outside locked story battles.");
        }

        private IEnumerator EnterExpeditionBoardBattle078(string encounterId078)
        {
            Require071(StringComparer.Ordinal.Equals(
                    _coordinator?.GuildCity017D?.Expedition?.CurrentEncounterId,
                    encounterId078),
                "Expected board encounter is not active: " + encounterId078 + ".");
            yield return CommitExpeditionPrimaryOrder078();
            Require071(_coordinator.State.Battle != null &&
                       !_coordinator.State.Battle.IsResolved &&
                       (_coordinator.State.Battle.BattleId ?? string.Empty).IndexOf(
                           encounterId078,
                           StringComparison.Ordinal) >= 0,
                "The committed board encounter did not enter battle: " + encounterId078 + ".");
        }

        private OuterGateworksExploration066 RequireGuidedField076(string expectedNodeId076)
        {
            var field076 = UnityEngine.Object.FindFirstObjectByType<OuterGateworksExploration066>();
            Require071(field076 != null && field076.IsActiveForVerification076,
                "The guided Lantern Road field is not active at " + expectedNodeId076 + ".");
            Require071(StringComparer.Ordinal.Equals(
                    field076.CurrentNodeForVerification076,
                    expectedNodeId076),
                "The guided field reconstructed the wrong saved node; expected " +
                expectedNodeId076 + " but found " + field076.CurrentNodeForVerification076 + ".");
            Require071(field076.ControlledAvatarForVerification076 != null &&
                       !string.IsNullOrWhiteSpace(field076.CurrentObjectiveForVerification076) &&
                       !string.IsNullOrWhiteSpace(field076.AuthoredBackdropForVerification076),
                "The guided field is missing its avatar, objective, or authored environment at " +
                expectedNodeId076 + ".");
            Require071(field076.AuthoritativeActionCountForVerification076 == 1,
                "The guided field has more than one competing gold action at " + expectedNodeId076 + ".");
            Require071(!field076.HasOptionalCampOrSecretForVerification076,
                "Optional systems leaked into the first-hour field at " + expectedNodeId076 + ".");
            return field076;
        }

        private IEnumerator WalkMarketToCurrentObjective076(
            WalkableSkyhomeArrival071 market076)
        {
            Require071(market076 != null && market076.IsActive071 &&
                       market076.ControlledAvatar071 != null,
                "The Skyhome Market traversal lost its controlled avatar.");
            var initialDistance076 = market076.CurrentObjectiveDistanceForVerification076;
            var radius076 = market076.CurrentObjectiveInteractionRadiusForVerification076;
            Require071(IsFinite076(initialDistance076) && radius076 > 0f,
                "The Skyhome Market Hall objective has no finite walkable range.");
            var start076 = market076.ControlledAvatar071.position;
            var bestDistance076 = initialDistance076;
            var stalledSteps076 = 0;
            // Offscreen evidence capture can render at a deliberately throttled frame
            // cadence. Guard time without progress, rather than total traversal wall
            // time, so a continuously advancing shipping motor is not rejected merely
            // because the GPU is producing audit frames slowly.
            var progressDeadline076 =
                Time.realtimeSinceStartup + ObjectiveTraversalTimeoutSeconds076;

            for (var step076 = 0;
                 step076 < ObjectiveTraversalMaximumSteps076 &&
                 !market076.IsWithinCurrentObjectiveInteractionRangeForVerification076;
                 step076++)
            {
                Require071(Time.realtimeSinceStartup < progressDeadline076,
                    "Walking through Skyhome Market made no measurable progress before " +
                    "the real-time traversal guard expired.");
                var direction076 = market076.CurrentObjectiveDirectionForVerification076;
                Require071(direction076.sqrMagnitude > 0.25f,
                    "The Guild Hall direction vanished before the player reached interaction range.");
                var before076 = market076.ControlledAvatar071.position;
                market076.ApplyMovementForVerification071(
                    direction076,
                    true,
                    false,
                    ObjectiveTraversalStepSeconds076);
                yield return null;

                Require071(market076.IsActive071 && market076.ControlledAvatar071 != null,
                    "The Skyhome Market closed during player-driven traversal.");
                var after076 = market076.ControlledAvatar071.position;
                var stepDistance076 = Vector3.Distance(before076, after076);
                Require071(IsFinite076(stepDistance076) &&
                           stepDistance076 < MarketTraversalMaximumStep076,
                    "Skyhome Market traversal jumped instead of using the shipping movement motor.");
                var distance076 = market076.CurrentObjectiveDistanceForVerification076;
                Require071(IsFinite076(distance076),
                    "The Guild Hall objective distance became invalid during traversal.");
                if (distance076 < bestDistance076 - ObjectiveTraversalProgressEpsilon076)
                {
                    bestDistance076 = distance076;
                    stalledSteps076 = 0;
                    progressDeadline076 =
                        Time.realtimeSinceStartup + ObjectiveTraversalTimeoutSeconds076;
                }
                else
                {
                    stalledSteps076++;
                    Require071(stalledSteps076 < ObjectiveTraversalStallLimit076,
                        "The player could not make progress toward the Guild Hall through Skyhome Market.");
                }
            }

            Require071(market076.IsWithinCurrentObjectiveInteractionRangeForVerification076 &&
                       market076.CurrentObjectiveDistanceForVerification076 <= radius076,
                "The shipping movement path did not reach the Guild Hall interaction range.");
            Require071(bestDistance076 < initialDistance076 - 1f &&
                       Vector3.Distance(start076, market076.ControlledAvatar071.position) > 1f,
                "The Market certification did not traverse a meaningful player-controlled distance.");
        }

        private IEnumerator WalkFieldToCurrentObjective076(
            OuterGateworksExploration066 field076,
            string objectiveDescription076)
        {
            Require071(field076 != null && field076.IsActiveForVerification076 &&
                       field076.ControlledAvatarForVerification076 != null,
                "The Lantern Road traversal lost its controlled avatar while approaching " +
                objectiveDescription076 + ".");
            var initialDistance076 = field076.CurrentObjectiveDistanceForVerification076;
            var radius076 = field076.CurrentObjectiveInteractionRadiusForVerification076;
            Require071(IsFinite076(initialDistance076) && radius076 > 0f,
                "The field objective has no finite walkable range: " + objectiveDescription076 + ".");
            var bestDistance076 = initialDistance076;
            var stalledSteps076 = 0;
            // The deterministic motor, finite-distance, per-step displacement, step
            // count, and stalled-step checks below remain the traversal authorities.
            // This wall-clock deadline detects a genuinely frozen player while still
            // allowing slow offscreen rendering to certify continuous real movement.
            var progressDeadline076 =
                Time.realtimeSinceStartup + ObjectiveTraversalTimeoutSeconds076;

            for (var step076 = 0;
                 step076 < ObjectiveTraversalMaximumSteps076 &&
                 !field076.IsWithinCurrentObjectiveInteractionRangeForVerification076;
                 step076++)
            {
                Require071(Time.realtimeSinceStartup < progressDeadline076,
                    "Walking to " + objectiveDescription076 +
                    " made no measurable progress before the real-time traversal guard expired.");
                var direction076 = field076.CurrentObjectiveDirectionForVerification076;
                Require071(direction076.sqrMagnitude > 0.25f,
                    "The objective direction vanished before reaching " +
                    objectiveDescription076 + ".");
                var before076 = field076.ControlledAvatarForVerification076.position;
                field076.ApplyMovementInput066(
                    direction076,
                    false,
                    ObjectiveTraversalStepSeconds076);
                yield return null;

                Require071(field076.IsActiveForVerification076 &&
                           field076.ControlledAvatarForVerification076 != null,
                    "Lantern Road closed while walking to " + objectiveDescription076 + ".");
                var after076 = field076.ControlledAvatarForVerification076.position;
                var stepDistance076 = Vector3.Distance(before076, after076);
                Require071(IsFinite076(stepDistance076) &&
                           stepDistance076 < FieldTraversalMaximumStep076,
                    "Lantern Road traversal jumped instead of using the shipping movement motor at " +
                    objectiveDescription076 + ".");
                var distance076 = field076.CurrentObjectiveDistanceForVerification076;
                Require071(IsFinite076(distance076),
                    "The objective distance became invalid while walking to " +
                    objectiveDescription076 + ".");
                if (distance076 < bestDistance076 - ObjectiveTraversalProgressEpsilon076)
                {
                    bestDistance076 = distance076;
                    stalledSteps076 = 0;
                    progressDeadline076 =
                        Time.realtimeSinceStartup + ObjectiveTraversalTimeoutSeconds076;
                }
                else
                {
                    stalledSteps076++;
                    Require071(stalledSteps076 < ObjectiveTraversalStallLimit076,
                        "The player could not make progress toward " + objectiveDescription076 + ".");
                }
            }

            Require071(field076.IsWithinCurrentObjectiveInteractionRangeForVerification076 &&
                       field076.CurrentObjectiveDistanceForVerification076 <= radius076,
                "The shipping movement path did not reach interaction range for " +
                objectiveDescription076 + ".");
            if (initialDistance076 > radius076 + 0.10f)
                Require071(bestDistance076 < initialDistance076 -
                           Mathf.Min(0.10f, (initialDistance076 - radius076) * 0.5f),
                    "The field certification did not make measurable progress toward " +
                    objectiveDescription076 + ".");
        }

        private IEnumerator AdvanceGuidedField076(
            OuterGateworksExploration066 field076,
            string expectedDestination076)
        {
            yield return WalkFieldToCurrentObjective076(
                field076,
                "the route to " + expectedDestination076);
            field076.InteractForVerification076();
            Require071(_coordinator.GuildCity017D?.Expedition != null &&
                       StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentNodeId,
                           expectedDestination076),
                "The field interaction did not advance to " + expectedDestination076 + ".");
        }

        private static IEnumerator WaitForGuidedFieldInteractionReady076(
            OuterGateworksExploration066 field076,
            string interactionDescription076)
        {
            const float timeoutSeconds076 = 10f;
            var deadline076 = Time.realtimeSinceStartup + timeoutSeconds076;
            while (field076 != null &&
                   field076.InteractionLockedForVerification076 &&
                   Time.realtimeSinceStartup < deadline076)
                yield return null;
            Require071(field076 != null && !field076.InteractionLockedForVerification076,
                "The field did not finish " + interactionDescription076 +
                " within " + timeoutSeconds076.ToString("0") + " seconds.");
        }

        private IEnumerator EnterGuidedBattle076(
            OuterGateworksExploration066 field076,
            string encounterId076)
        {
            Require071(field076 != null &&
                       _coordinator.GuildCity017D?.Expedition != null &&
                       StringComparer.Ordinal.Equals(
                           _coordinator.GuildCity017D.Expedition.CurrentEncounterId,
                           encounterId076),
                "The guided field is not presenting the expected encounter " + encounterId076 + ".");
            yield return WalkFieldToCurrentObjective076(
                field076,
                "the encounter marker for " + encounterId076);
            field076.InteractForVerification076();
            Require071(_coordinator.State.Battle != null &&
                       !_coordinator.State.Battle.IsResolved &&
                       (_coordinator.State.Battle.BattleId ?? string.Empty).IndexOf(
                           encounterId076,
                           StringComparison.Ordinal) >= 0,
                "ACT at the field encounter did not start the authoritative battle " +
                encounterId076 + ".");
        }

        private static bool IsFinite076(float value076) =>
            !float.IsNaN(value076) && !float.IsInfinity(value076);

        private void VerifyMaximumCampaignBattleCapacity078()
        {
            var capacityCampaign078 = CreateMaximumCampaignCapacity078();
            var unionValidation078 = new M1CommandService()
                .ValidateGuildUnionPlans(capacityCampaign078.Guild);
            var alliedUnionIds078 = capacityCampaign078.Guild.Unions
                .Select(value => value.UnionId)
                .ToArray();
            var contentRoot078 = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            var organicHousing078 = GuildCityContent017D
                .LoadFromDirectory(Path.Combine(contentRoot078, "GUILD_CITY_017D"))
                .Building("GC017D_BUILD_GUILD_HOUSING");
            var organicRosterCapacity078 = new GuildCityEffectService017D()
                .RosterCapacityAtDormitoryLevel(3);
            var battleResult078 = new M2BattleCommandService().StartEncounterBattle(
                capacityCampaign078,
                M2CombatContent.LoadFromDirectory(contentRoot078),
                "BATTLE_FIRST_HOUR_GOLD_CAPACITY_078",
                "Prove the packaged campaign can field ten six-member Unions.",
                10,
                new[] { "SCOUTED_APPROACH" },
                alliedUnionIds078);
            var battle078 = battleResult078.IsSuccess ? battleResult078.Value.Battle : null;
            var deployedUnionIds078 = new HashSet<string>(
                battle078?.PlayerUnions.Select(value => value.UnionId) ??
                Array.Empty<string>(),
                StringComparer.Ordinal);
            var deployedMemberIds078 = new HashSet<string>(
                battle078?.PlayerUnions
                    .SelectMany(value => value.Members)
                    .Select(value => value.MemberId) ??
                Array.Empty<string>(),
                StringComparer.Ordinal);
            var forecastUnionIds078 = new HashSet<string>(
                battle078?.CommittedForecasts.Select(value => value.UnionId) ??
                Array.Empty<string>(),
                StringComparer.Ordinal);
            var exactForecastMembership078 = battle078 != null &&
                battle078.CommittedForecasts
                    .Where(value => alliedUnionIds078.Contains(
                        value.UnionId,
                        StringComparer.Ordinal))
                    .All(value =>
                    {
                        var union078 = battle078.PlayerUnions.FirstOrDefault(candidate078 =>
                            StringComparer.Ordinal.Equals(
                                candidate078.UnionId,
                                value.UnionId));
                        return union078 != null &&
                               value.MemberActions != null &&
                               value.MemberActions.Count == union078.Members.Count &&
                               new HashSet<string>(
                                   value.MemberActions.Select(action078 =>
                                       action078.ActorMemberId),
                                   StringComparer.Ordinal)
                                   .SetEquals(union078.Members.Select(member078 =>
                                       member078.MemberId));
                    });
            _report.tenBySixCampaignCapacityVerified =
                NormalUnionPlanRules.MaximumPlanCount == 10 &&
                NormalUnionPlanRules.MaximumDeployedUnionCount == 10 &&
                NormalUnionPlanRules.MaximumMembersPerUnion == 6 &&
                M2BattleCommandHud072.MaximumSupportedPlayerUnions078 == 10 &&
                M2BattleCommandHud072.MaximumFocusedMemberStates078 == 6 &&
                organicHousing078 != null &&
                organicHousing078.MaxLevel >= 3 &&
                organicRosterCapacity078 >= 60 &&
                unionValidation078.IsSuccess &&
                battle078 != null &&
                battle078.PlayerUnions.Count == 10 &&
                battle078.EnemyUnions.Count == 10 &&
                deployedUnionIds078.SetEquals(alliedUnionIds078) &&
                battle078.PlayerUnions.All(value =>
                    value != null &&
                    value.Members != null &&
                    value.Members.Count == 6 &&
                    value.FormationBenefitActive) &&
                deployedMemberIds078.Count == 60 &&
                battle078.EnemyUnions
                    .Select(value => value.UnionId)
                    .Distinct(StringComparer.Ordinal)
                    .Count() == 10 &&
                forecastUnionIds078.SetEquals(alliedUnionIds078) &&
                exactForecastMembership078;
            Require071(
                _report.tenBySixCampaignCapacityVerified,
                "The packaged 10x6 capacity probe failed. " +
                (battleResult078.IsSuccess
                    ? "Validation=" + unionValidation078.IsSuccess +
                      ", organicHousingMaxLevel=" + (organicHousing078?.MaxLevel ?? 0) +
                      ", organicLevel3RosterCapacity=" + organicRosterCapacity078 +
                      ", playerUnions=" + (battle078?.PlayerUnions.Count ?? 0) +
                      ", enemyUnions=" + (battle078?.EnemyUnions.Count ?? 0) +
                      ", uniqueMembers=" + deployedMemberIds078.Count +
                      ", forecastUnions=" + forecastUnionIds078.Count + ". " +
                      string.Join(" ", unionValidation078.Errors)
                    : string.Join(" ", battleResult078.Errors)));
        }

        private static CampaignState CreateMaximumCampaignCapacity078()
        {
            var recruits078 = new List<RecruitState>();
            var unions078 = new List<UnionState>();
            for (var unionIndex078 = 0;
                 unionIndex078 < NormalUnionPlanRules.MaximumPlanCount;
                 unionIndex078++)
            {
                var memberIds078 = new List<string>();
                for (var memberIndex078 = 0;
                     memberIndex078 < NormalUnionPlanRules.MaximumMembersPerUnion;
                     memberIndex078++)
                {
                    var ordinal078 = unionIndex078 *
                                     NormalUnionPlanRules.MaximumMembersPerUnion +
                                     memberIndex078 + 1;
                    var recruitId078 = "CAPACITY078_RECRUIT_" +
                                       ordinal078.ToString("00");
                    var weapon078 = new EquipmentItemState(
                        "CAPACITY078_ITEM_" + ordinal078.ToString("00"),
                        "EQ_CAPACITY078_SWORD",
                        "Capacity Sword",
                        new[] { EquipmentSlotIds.MainHand },
                        new[] { "SWORD", "WEAPON" },
                        "QUALITY_STANDARD",
                        10000,
                        false);
                    recruits078.Add(new RecruitState(
                        recruitId078,
                        150,
                        150,
                        30,
                        30,
                        "Capacity Recruit " + ordinal078,
                        RecruitOriginKind.Procedural,
                        string.Empty,
                        "HUMAN",
                        "WORLD_GATE_01",
                        "CLASS_TEND_WARRIOR",
                        "Observed",
                        7500,
                        RecruitAuthorityKind.Normal,
                        string.Empty,
                        string.Empty,
                        new EquipmentLoadoutState(new[]
                        {
                            new EquipmentSlotAssignmentState(
                                EquipmentSlotIds.MainHand,
                                weapon078)
                        }),
                        true,
                        string.Empty,
                        string.Empty,
                        60 + ordinal078,
                        60 + ordinal078));
                    memberIds078.Add(recruitId078);
                }

                unions078.Add(new UnionState(
                    "CAPACITY078_UNION_" + (unionIndex078 + 1).ToString("00"),
                    "Capacity Union " + (unionIndex078 + 1),
                    UnionKind.Normal,
                    memberIds078[0],
                    memberIds078.AsReadOnly(),
                    "FORMATION_SHIELD_WALL",
                    "DOCTRINE_BALANCED",
                    18,
                    8500));
            }

            var guild078 = new GuildState(
                "GUILD_FIRST_HOUR_GOLD_CAPACITY_078",
                0,
                recruits078.AsReadOnly(),
                unions078.AsReadOnly());
            var profile078 = new NewGuildProfileState(
                "Capacity Guildmaster",
                GameMode.Standard,
                TutorialDepth.FullTutorial,
                AccessibilitySettingsState.Defaults(),
                false);
            var flow078 = new OpeningFlowState(
                OpeningStage.Complete,
                "SDGOW_TUTORIAL_V1_001",
                true,
                null,
                false,
                439,
                0,
                true,
                true,
                true,
                false,
                "capacity_078_ready");
            return new CampaignState(
                "00000000-0000-0000-0000-000000000278",
                20260830L,
                "1.0",
                ModeRuleSnapshot.StandardDefaults(),
                guild078,
                profile078,
                flow078);
        }

        private void VerifyTwoStartingTreesAndRosterLegality076()
        {
            var recruits076 = (_coordinator.State.Recruits ??
                               Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            Require071(recruits076.Length == 20,
                "The two-tree roster gate requires all twenty Guild members.");

            _report.twoStartingTreesVerified = recruits076.All(value =>
                (value.LearnedArtIds ?? Array.Empty<string>())
                .Select(DeepTreeId076)
                .Where(treeId076 => !string.IsNullOrWhiteSpace(treeId076))
                .Distinct(StringComparer.Ordinal)
                .Count() == 2);
            Require071(_report.twoStartingTreesVerified,
                "Every first-hour recruit must enter play with exactly two usable Art trees.");

            var patrol076 = recruits076.Where(value =>
                    FirstHourRosterService071.PatrolStableRecruitIds.Contains(
                        value.PortraitAuthorityId,
                        StringComparer.Ordinal))
                .ToArray();
            Require071(patrol076.Length == 10 && patrol076.All(value =>
                           value.IsLegal &&
                           (value.Slots ?? Array.Empty<M1EquipmentSlotView>())
                           .Where(slot076 => slot076 != null &&
                                             !string.IsNullOrWhiteSpace(slot076.EquippedItemId))
                           .All(slot076 => slot076.IsLegal)),
                "A rescued Lantern Patrol member still has an illegal live equipment loadout.");
        }

        private static string DeepTreeId076(string artId076)
        {
            if (string.IsNullOrWhiteSpace(artId076) ||
                !artId076.StartsWith("TREE_CA002_", StringComparison.Ordinal))
                return string.Empty;
            var nodeMarker076 = artId076.LastIndexOf("_N", StringComparison.Ordinal);
            if (nodeMarker076 <= 0 || nodeMarker076 + 2 >= artId076.Length)
                return string.Empty;
            for (var index076 = nodeMarker076 + 2; index076 < artId076.Length; index076++)
                if (!char.IsDigit(artId076[index076])) return string.Empty;
            return artId076.Substring(0, nodeMarker076);
        }

        private static void VerifyPersistedStoryUnionPlans078(
            M1PresentationState state078)
        {
            Require071(state078 != null,
                "The persisted story Union plan was unavailable after Chapter 2 reload.");
            var activePlans078 = (state078.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                value.MemberRecruitIds != null &&
                                value.MemberRecruitIds.Count > 0)
                .ToArray();
            var unionIds078 = new HashSet<string>(
                activePlans078.Select(value => value.UnionId),
                StringComparer.Ordinal);
            var plannedMemberCount078 = activePlans078.Sum(value =>
                value.MemberRecruitIds.Count);
            var plannedMemberIds078 = new HashSet<string>(
                activePlans078.SelectMany(value => value.MemberRecruitIds),
                StringComparer.Ordinal);
            var roster078 = (state078.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var rosterMemberIds078 = new HashSet<string>(
                roster078.Select(value => value.RecruitId),
                StringComparer.Ordinal);
            var patrolMemberIds078 = MapRescuedPatrolMemberIds078(
                roster078,
                out var allPatrolIdentitiesMapped078);

            Require071(state078.OpeningUnionsLegal &&
                       activePlans078.Length > 0 &&
                       activePlans078.Length <= NormalUnionPlanRules.MaximumPlanCount &&
                       unionIds078.Count == activePlans078.Length &&
                       activePlans078.All(value =>
                           !string.IsNullOrWhiteSpace(value.UnionId) &&
                           value.IsLegal &&
                           value.MemberRecruitIds.Count >= 1 &&
                           value.MemberRecruitIds.Count <=
                           NormalUnionPlanRules.MaximumMembersPerUnion) &&
                       plannedMemberIds078.Count == plannedMemberCount078 &&
                       plannedMemberIds078.All(rosterMemberIds078.Contains) &&
                       allPatrolIdentitiesMapped078 &&
                       patrolMemberIds078.Count == 10 &&
                       patrolMemberIds078.All(plannedMemberIds078.Contains),
                "The persisted story plan was illegal or did not keep all ten rescued patrol members fieldable. " +
                "Plans=" + activePlans078.Length +
                ", plannedMembers=" + plannedMemberCount078 +
                ", mappedPatrolMembers=" + patrolMemberIds078.Count + ".");
        }

        private void VerifyGateEaterStoryDeployment078(
            M2BattleView battle078,
            IReadOnlyList<M2ForecastView> selectedForecasts078)
        {
            var storyState078 = _coordinator.State;
            var plannedUnions078 = (storyState078.Unions ?? Array.Empty<M1UnionView>())
                .Where(value => value != null &&
                                value.MemberRecruitIds != null &&
                                value.MemberRecruitIds.Count > 0)
                .ToArray();
            var plannedUnionIds078 = new HashSet<string>(
                plannedUnions078.Select(value => value.UnionId),
                StringComparer.Ordinal);
            var plannedMemberCount078 = plannedUnions078.Sum(value =>
                value.MemberRecruitIds.Count);
            var plannedMemberIds078 = new HashSet<string>(
                plannedUnions078.SelectMany(value => value.MemberRecruitIds),
                StringComparer.Ordinal);
            var roster078 = (storyState078.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var rosterMemberIds078 = new HashSet<string>(
                roster078.Select(value => value.RecruitId),
                StringComparer.Ordinal);
            var rescuedPatrolMemberIds078 = MapRescuedPatrolMemberIds078(
                roster078,
                out var allPatrolIdentitiesMapped078);
            var deployedUnions078 = (battle078.PlayerUnions ??
                                     Array.Empty<M2BattleUnionView>())
                .Where(value => value != null)
                .ToArray();
            var deployedUnionIds078 = new HashSet<string>(
                deployedUnions078.Select(value => value.UnionId),
                StringComparer.Ordinal);
            var activeUnionIds078 = new HashSet<string>(
                deployedUnions078.Where(value => value.CanAct)
                    .Select(value => value.UnionId),
                StringComparer.Ordinal);
            var deployedMembers078 = deployedUnions078
                .SelectMany(value => value.Members ?? Array.Empty<M2BattleMemberView>())
                .Where(value => value != null)
                .ToArray();
            var deployedMemberIds078 = new HashSet<string>(
                deployedMembers078.Select(value => value.MemberId),
                StringComparer.Ordinal);
            var deployedPatrolMemberIds078 = new HashSet<string>(
                deployedMembers078
                    .Where(value => FirstHourRosterService071.PatrolStableRecruitIds.Contains(
                        value.PortraitAuthorityId,
                        StringComparer.Ordinal))
                    .Select(value => value.MemberId),
                StringComparer.Ordinal);
            var forecasts078 = (selectedForecasts078 ?? Array.Empty<M2ForecastView>())
                .Where(value => value != null)
                .ToArray();
            var forecastUnionIds078 = new HashSet<string>(
                forecasts078.Select(value => value.UnionId),
                StringComparer.Ordinal);
            var actions078 = forecasts078
                .SelectMany(value => value.MemberActions ??
                                     Array.Empty<M2PredictedActionView>())
                .Where(value => value != null &&
                                !string.IsNullOrWhiteSpace(value.ActorMemberId))
                .ToArray();
            var actionMemberIds078 = new HashSet<string>(
                actions078.Select(value => value.ActorMemberId),
                StringComparer.Ordinal);
            var exactDeployedMembership078 = plannedUnions078.All(plan078 =>
            {
                var deployed078 = deployedUnions078.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, plan078.UnionId));
                return deployed078 != null &&
                       new HashSet<string>(
                           deployed078.Members.Select(value => value.MemberId),
                           StringComparer.Ordinal)
                           .SetEquals(plan078.MemberRecruitIds);
            });
            var exactForecastMembership078 = forecasts078.All(forecast078 =>
            {
                var deployed078 = deployedUnions078.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(value.UnionId, forecast078.UnionId));
                return deployed078 != null &&
                       forecast078.MemberActions != null &&
                       forecast078.MemberActions.Count == deployed078.Members.Count &&
                       new HashSet<string>(
                           forecast078.MemberActions.Select(value => value.ActorMemberId),
                           StringComparer.Ordinal)
                           .SetEquals(deployed078.Members.Select(value => value.MemberId));
            });

            _report.patrolCombatReadyVerified =
                storyState078.OpeningUnionsLegal &&
                plannedUnions078.Length > 0 &&
                plannedUnions078.Length <= NormalUnionPlanRules.MaximumPlanCount &&
                plannedUnionIds078.Count == plannedUnions078.Length &&
                plannedUnions078.All(value => value.IsLegal &&
                    value.MemberRecruitIds.Count >= 1 &&
                    value.MemberRecruitIds.Count <=
                    NormalUnionPlanRules.MaximumMembersPerUnion) &&
                plannedMemberIds078.Count == plannedMemberCount078 &&
                plannedMemberIds078.All(rosterMemberIds078.Contains) &&
                allPatrolIdentitiesMapped078 &&
                rescuedPatrolMemberIds078.Count == 10 &&
                rescuedPatrolMemberIds078.All(plannedMemberIds078.Contains) &&
                deployedUnions078.Length == plannedUnions078.Length &&
                deployedUnionIds078.SetEquals(plannedUnionIds078) &&
                activeUnionIds078.SetEquals(plannedUnionIds078) &&
                exactDeployedMembership078 &&
                deployedMembers078.Length == plannedMemberCount078 &&
                deployedMemberIds078.SetEquals(plannedMemberIds078) &&
                deployedMembers078.All(value => !value.Downed &&
                    value.EquipmentTags != null && value.EquipmentTags.Count > 0) &&
                rescuedPatrolMemberIds078.SetEquals(deployedPatrolMemberIds078) &&
                forecasts078.Length == plannedUnions078.Length &&
                forecastUnionIds078.SetEquals(plannedUnionIds078) &&
                exactForecastMembership078 &&
                actions078.Length == plannedMemberCount078 &&
                actionMemberIds078.SetEquals(plannedMemberIds078) &&
                rescuedPatrolMemberIds078.All(actionMemberIds078.Contains);
            Require071(_report.patrolCombatReadyVerified,
                "The Gate-Eater deployment diverged from the saved legal Union plan or omitted a rescued patrol action. " +
                "Plans=" + plannedUnions078.Length +
                ", plannedMembers=" + plannedMemberCount078 +
                ", deployedUnions=" + deployedUnions078.Length +
                ", deployedMembers=" + deployedMembers078.Length +
                ", patrolMembers=" + deployedPatrolMemberIds078.Count +
                ", forecastUnions=" + forecastUnionIds078.Count +
                ", actionMembers=" + actionMemberIds078.Count + ".");
        }

        private static HashSet<string> MapRescuedPatrolMemberIds078(
            IReadOnlyList<M1RecruitLoadoutView> roster078,
            out bool everyStableIdentityMappedOnce078)
        {
            var mappedRecruitIds078 = new HashSet<string>(StringComparer.Ordinal);
            everyStableIdentityMappedOnce078 =
                FirstHourRosterService071.PatrolStableRecruitIds.Count == 10 &&
                FirstHourRosterService071.PatrolStableRecruitIds
                    .Distinct(StringComparer.Ordinal)
                    .Count() == 10;
            foreach (var stableId078 in FirstHourRosterService071.PatrolStableRecruitIds)
            {
                var matches078 = (roster078 ?? Array.Empty<M1RecruitLoadoutView>())
                    .Where(value => value != null &&
                                    (StringComparer.Ordinal.Equals(
                                         value.PortraitAuthorityId,
                                         stableId078) ||
                                     StringComparer.Ordinal.Equals(
                                         value.RecruitId,
                                         stableId078)))
                    .ToArray();
                if (matches078.Length != 1 ||
                    string.IsNullOrWhiteSpace(matches078[0].RecruitId) ||
                    !mappedRecruitIds078.Add(matches078[0].RecruitId))
                    everyStableIdentityMappedOnce078 = false;
            }

            return mappedRecruitIds078;
        }

        private IEnumerator RunEncounter071(string encounterId, string screenshotName, bool captureBreakthrough)
        {
            var activeBattle076 = _coordinator.State.Battle;
            var alreadyStartedFromField076 = activeBattle076 != null &&
                                               !activeBattle076.IsResolved &&
                                               (activeBattle076.BattleId ?? string.Empty).IndexOf(
                                                   encounterId,
                                                   StringComparison.Ordinal) >= 0;
            if (!alreadyStartedFromField076)
            {
                Require071(StringComparer.Ordinal.Equals(
                        _coordinator.GuildCity017D.Expedition.CurrentEncounterId,
                        encounterId),
                    "Expected encounter is not active: " + encounterId);
                Require071(_coordinator.CommitGuildCityEncounter017D(encounterId),
                    "commit " + encounterId);
                Require071(_coordinator.StartCommittedGuildCityBattle017D(),
                    "start " + encounterId);
            }
            Require071(_coordinator.State.Battle != null &&
                       _coordinator.State.Battle.BattleId.IndexOf(
                    encounterId, StringComparison.Ordinal) >= 0,
                "The certified battle identity lost its encounter authority: " + encounterId);
            _report.battleIds.Add(encounterId);
            _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.Battle);
            var isFirstBattle = M2BattleExperienceController072.IsFirstBattleTutorial076(
                _coordinator.State.Battle);
            if (isFirstBattle)
            {
                _report.firstBattleCoachVerified =
                    GameObject.Find("First Battle Union Coach 076") != null;
                Require071(_report.firstBattleCoachVerified,
                    "The Hall Breach did not open the first Union-command coach.");
                RequireFocusedVisibleAction076("TAKE COMMAND");
                yield return Capture071("battle_1_union_command_coach");
                ClickVisibleAction076("TAKE COMMAND");
                yield return null;
                Require071(GameObject.Find("First Battle Union Coach 076") == null,
                    "TAKE COMMAND did not dismiss the first battle coach.");
            }
            else
            {
                Require071(GameObject.Find("First Battle Union Coach 076") == null,
                    "The one-time first battle coach returned during a later encounter.");
            }
            yield return Capture071(screenshotName);

            for (var round = 0; round < 30 && !_coordinator.State.Battle.IsResolved; round++)
            {
                var battle = _coordinator.State.Battle;
                var stateHashBeforeExchange = battle.StateHash ?? string.Empty;
                var selectedForecasts076 = new List<M2ForecastView>();
                for (var unionIndex076 = 0;
                     unionIndex076 < battle.PlayerUnions.Count;
                     unionIndex076++)
                {
                    var union076 = battle.PlayerUnions[unionIndex076];
                    if (union076 == null || !union076.CanAct) continue;
                    var choices076 = battle.Forecasts.Where(value =>
                            value != null && StringComparer.Ordinal.Equals(
                                value.UnionId,
                                union076.UnionId))
                        .Take(VisibleBattleForecastLimit076)
                        .ToArray();
                    Require071(choices076.Length > 0,
                        "No visible complete Union Forecast for " + union076.UnionId);
                    var choice076 = choices076.FirstOrDefault(value =>
                                     StringComparer.Ordinal.Equals(value.CommandId, "CMD_ALL_OUT")) ??
                                 choices076.FirstOrDefault(value =>
                                     StringComparer.Ordinal.Equals(value.CommandId, "CMD_FLANK")) ??
                                 choices076[0];
                    yield return SelectVisibleBattleForecast076(
                        battle,
                        unionIndex076,
                        union076,
                        choice076);
                    selectedForecasts076.Add(choice076);
                }
                if (round == 0 && StringComparer.Ordinal.Equals(
                        encounterId,
                        "ENCOUNTER071_GATE_EATER"))
                {
                    VerifyGateEaterStoryDeployment078(battle, selectedForecasts076);
                    yield return null;
                    Require071(CountActiveNamedObjects076("Predicted Member Art Icon ") > 0,
                        "The live 072 command HUD did not render semantic icons for predicted member Arts.");
                }
                if (isFirstBattle && round == 0)
                {
                    var readied = _coordinator.State.Battle;
                    var activeUnionCount = readied.PlayerUnions.Count(value => value.CanAct);
                    var readiedUnionCount = readied.PlayerUnions.Count(value => value.CanAct &&
                        (value.IsSelected || !string.IsNullOrWhiteSpace(value.SelectedForecastId)));
                    _report.battleOrdersReadiedVerified = activeUnionCount > 0 &&
                                                         readiedUnionCount == activeUnionCount &&
                                                         readied.CanConfirmRound;
                    Require071(_report.battleOrdersReadiedVerified,
                        "The Hall Breach did not reach the all-Unions-ready command state.");
                    _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.Battle);
                    yield return null;
                    var executeRound076 = GameObject.Find("Confirm Complete Union Forecasts 072")
                        ?.GetComponent<Button>();
                    var executeRoundLabel076 = executeRound076?.GetComponentInChildren<Text>();
                    Require071(executeRound076 != null && executeRound076.interactable &&
                               executeRoundLabel076 != null &&
                               executeRoundLabel076.text.IndexOf(
                                   "ALL ORDERS READY",
                                   StringComparison.OrdinalIgnoreCase) >= 0,
                        "The all-Unions-ready state was authoritative but not visibly actionable.");
                    yield return Capture071("battle_1_all_union_orders_readied");
                }
                yield return ExecuteVisibleBattleRound076(encounterId);
                var exchanged = _coordinator.State.Battle;
                _authoritativeBattleExchangeObserved078 =
                    _authoritativeBattleExchangeObserved078 ||
                    (exchanged.LastResolvedRound > 0 &&
                     exchanged.LastResolvedRoundEvents != null &&
                     exchanged.LastResolvedRoundEvents.Count > 0 &&
                     !StringComparer.Ordinal.Equals(stateHashBeforeExchange, exchanged.StateHash));
                if (!_report.artBreakthroughObserved &&
                    (_coordinator.State.Battle.Events ?? Array.Empty<M2BattleEventView>()).Any(value =>
                        value != null && StringComparer.Ordinal.Equals(value.EventType, "BREAKTHROUGH")))
                {
                    _report.artBreakthroughObserved = true;
                }
            }
            Require071(_coordinator.State.Battle.IsResolved, encounterId + " exceeded the round guard.");
            Require071(StringComparer.OrdinalIgnoreCase.Equals(
                    _coordinator.State.Battle.Outcome, "Victory"),
                encounterId + " did not end in victory.");
            var resolvedRoundCount078 = _coordinator.State.Battle.LastResolvedRound;
            var minimumRoundCount078 = MinimumCertifiedEncounterRounds078(encounterId);
            Require071(resolvedRoundCount078 >= minimumRoundCount078,
                encounterId + " collapsed in " + resolvedRoundCount078 +
                " round(s); the studio slice requires at least " +
                minimumRoundCount078 + " authoritative exchanges.");
            _mandatoryBattleRoundCounts078[encounterId] = resolvedRoundCount078;
            _report.battleExchangeResolvedVerified =
                _authoritativeBattleExchangeObserved078 &&
                AllMandatoryBattleRoundMinimumsMet078();
            if (captureBreakthrough && !_report.artBreakthroughObserved)
                Debug.LogWarning("Hall Breach did not produce the first observed breakthrough; later battles remain eligible.");
            _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.BattleResults);
            // A breakthrough spotlight can precede the five compact payoff stages.
            // Wait for the shipping return action itself instead of racing a fixed
            // duration, so evidence always contains Growth, Loot, and a usable exit.
            yield return WaitForInteractableButtonByName076(
                "Continue From Battle Results 072",
                5f);
            yield return Capture071(screenshotName + "_rewards");
            var returnNode076 = _coordinator.GuildCity017D?.Expedition?.CurrentNodeId ?? string.Empty;
            ClickVisibleButtonByName076("Continue From Battle Results 072");
            yield return null;
            yield return null;
            if (_coordinator.GuildCity017D?.Expedition != null &&
                (StringComparer.Ordinal.Equals(
                     _coordinator.GuildCity017D.Expedition.BoardId,
                     GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071) ||
                 StringComparer.Ordinal.Equals(
                     _coordinator.GuildCity017D.Expedition.BoardId,
                     GuildCityExpeditionService017D.SecondStoryBoardId076)))
            {
                RequireExpeditionBoardNode078(returnNode076);
                _report.postBattleFieldReturnVerified =
                    GameObject.Find("Board Quest 081") != null;
                Require071(_report.postBattleFieldReturnVerified,
                    "Claiming battle rewards did not reconstruct the saved expedition-board position.");
            }
        }

        private IEnumerator CertifyPackagedTowerFloorOne081()
        {
            const string floorOneId081 = "ABYSS_FLOOR_001_MUD_TRENCHES";
            const string floorOneOperationId081 = "ABYSS_OP022_01_GUARDIAN";
            var contentRoot081 = Path.Combine(
                Application.streamingAssetsPath,
                "Authority",
                "CONTENT");
            var towerSavePath081 = Path.GetFullPath(Path.Combine(
                _evidenceRoot,
                "tower_floor_one_smoke_save.json"));
            var towerSaveDirectory081 = Path.GetDirectoryName(towerSavePath081);
            Require071(!string.IsNullOrWhiteSpace(towerSaveDirectory081) &&
                       IsSameOrChild071(towerSaveDirectory081, _evidenceRoot),
                "The Tower certification save must remain inside its isolated evidence directory.");
            Require071(!PathsOverlap071(
                    towerSavePath081,
                    Path.GetFullPath(Application.persistentDataPath)),
                "Personal-data protection: the Tower certification save overlaps the player's persistent data.");
            Require071(File.Exists(_savePath),
                "The completed Chapter 2 smoke save was unavailable for the isolated Tower copy.");
            var storySaveSha256BeforeTower081 = ComputeFileSha256076(_savePath);
            Require071(!StringComparer.OrdinalIgnoreCase.Equals(
                           Path.GetFullPath(_savePath),
                           towerSavePath081),
                "The Tower certification requires a separate copied save, not the story smoke save itself.");
            File.Copy(_savePath, towerSavePath081, true);
            if (File.Exists(towerSavePath081 + ".bak"))
                File.Delete(towerSavePath081 + ".bak");
            _report.towerSavePath = towerSavePath081;

            _coordinator = new M1RuntimeCoordinator(contentRoot081, towerSavePath081);
            _presenter.Initialize(_coordinator);
            _presenter.ShowFirstHourGoldGuildTab071("ABYSS");
            yield return null;

            var lobby081 = _coordinator.CampaignProgression022;
            var floorOne081 = (lobby081.FloorStates ?? Array.Empty<AbyssFloorView022>())
                .SingleOrDefault(value081 => value081 != null &&
                    StringComparer.Ordinal.Equals(value081.FloorId, floorOneId081));
            var floorArt081 = GameObject.Find("Tower Current Floor Art 081")
                ?.GetComponent<Image>();
            var towerSummary084 = GameObject.Find("Tower Phone Simple Summary 084");
            var firstClimbTitle083 = towerSummary084 == null
                ? null
                : towerSummary084.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(value083 => value083 != null &&
                        (StringComparer.Ordinal.Equals(value083.name, "Title") ||
                         value083.name.StartsWith(
                             "Title [Responsive",
                             StringComparison.Ordinal)));
            var beginFloor081 = FindActiveButtonByName081(
                "Climb next Tower floor 084");
            _report.towerFloorOneUiVerified =
                lobby081.IsAvailable &&
                string.IsNullOrWhiteSpace(lobby081.ActiveAbyssOperationId) &&
                lobby081.HighestClearedTowerFloor == 0 &&
                lobby081.TotalTowerClears == 0 &&
                lobby081.TowerFloorNumber == 1 &&
                lobby081.TowerExpectedEnemyUnions == 1 &&
                StringComparer.Ordinal.Equals(lobby081.TowerFloorId, floorOneId081) &&
                StringComparer.Ordinal.Equals(
                    lobby081.TowerOperationId,
                    floorOneOperationId081) &&
                StringComparer.Ordinal.Equals(
                    lobby081.TowerArtResourcePath,
                    TowerRunRules081.FloorOneCampaign083BattleArtResourcePath) &&
                floorOne081 != null &&
                floorOne081.IsUnlocked &&
                floorOne081.IsCurrentGoal &&
                floorOne081.ClearCount == 0 &&
                floorArt081 != null &&
                floorArt081.sprite != null &&
                floorArt081.sprite.texture != null &&
                StringComparer.Ordinal.Equals(
                    floorArt081.sprite.texture.name,
                    TowerRunRules081.FloorOneCampaign083BattleArtTextureName) &&
                firstClimbTitle083 != null &&
                TowerRunRules081.ContrastRatio083(
                    firstClimbTitle083.color,
                    TowerRunRules081.TowerRibbonSurfaceColor083) >=
                    TowerRunRules081.MinimumReadableContrast083 &&
                !ActiveTowerPlaceholderCopyPresent083() &&
                beginFloor081 != null &&
                beginFloor081.IsInteractable();
            Require071(_report.towerFloorOneUiVerified,
                "The packaged Tower lobby did not render Floor 1, its authored art, one enemy Union, and a usable Begin action.");
            yield return Capture071("tower_floor_1_lobby");

            beginFloor081.onClick.Invoke();
            yield return WaitForTowerStateAndUi084(
                value084 =>
                    value084.TowerBattleInProgress &&
                    StringComparer.Ordinal.Equals(
                        value084.ActiveAbyssStatus,
                        "AwaitingBattle"),
                () =>
                {
                    var controller081 = UnityEngine.Object
                        .FindFirstObjectByType<M2BattleExperienceController072>();
                    return controller081 != null &&
                           controller081.IsActive &&
                           GameObject.Find("Board Adventure Guild Pawn 086") == null &&
                           GameObject.Find(
                               "Board Adventure Next Face Down Physical Card 086") == null;
                },
                8f);

            // The shipping Tower is deliberately battle-only. Its one lobby action
            // advances the preserved, saved Campaign022 bookends internally and
            // opens the Guardian battle without exposing quest cards or a pawn.
            Require071(_coordinator.CampaignProgression022.TowerBattleInProgress,
                "The battle-only Floor 1 action did not enter combat directly.");
            Require071(StringComparer.Ordinal.Equals(
                    _coordinator.CampaignProgression022.ActiveAbyssStatus,
                    "AwaitingBattle"),
                "The battle-only Floor 1 action did not reach the saved AwaitingBattle state.");
            Require071(GameObject.Find("Board Adventure Guild Pawn 086") == null,
                "The retired Board Quest pawn leaked into the battle-only Tower.");
            Require071(GameObject.Find(
                           "Board Adventure Next Face Down Physical Card 086") == null,
                "The retired face-down quest card leaked into the battle-only Tower.");

            var entered081 = _coordinator.CampaignProgression022;
            var openingBattle081 = _coordinator.State.Battle;
            _report.towerFloorId = entered081.TowerFloorId;
            _report.towerBattleId = openingBattle081?.BattleId ?? string.Empty;
            _report.towerEnemyUnionCount = openingBattle081?.EnemyUnions?.Count ?? 0;
            Require071(openingBattle081 != null &&
                       !openingBattle081.IsResolved &&
                       openingBattle081.BattleId.StartsWith(
                           "ABYSS_BATTLE022_",
                           StringComparison.Ordinal) &&
                       entered081.TowerBattleInProgress &&
                       entered081.TowerExpectedEnemyUnions == 1 &&
                       openingBattle081.EnemyUnions.Count == 1 &&
                       openingBattle081.PlayerUnions.Any(value081 =>
                           value081 != null && value081.CanAct),
                "The Floor 1 action did not create the exact authoritative Tower battle.");
            Require071(M1VisualAssets.TryResolveBattleBackdrop(
                           openingBattle081.BattleId,
                           out var expectedTowerBattleBackdrop081,
                           out var expectedTowerBattleBackdropKey081) &&
                       expectedTowerBattleBackdrop081 != null &&
                       expectedTowerBattleBackdrop081.texture != null &&
                       StringComparer.Ordinal.Equals(
                           expectedTowerBattleBackdropKey081,
                           TowerRunRules081.FloorOneCampaign083BattleArtResourcePath) &&
                       StringComparer.Ordinal.Equals(
                           expectedTowerBattleBackdrop081.texture.name,
                           TowerRunRules081.FloorOneCampaign083BattleArtTextureName),
                "The exact Floor 1 battle identity did not resolve its Campaign 083 battlefield art.");

            // EnterAbyssBattle022 and Navigate are synchronous, but player rendering,
            // responsive labels, and a replaced battle host settle on later frames.
            // Observe the current shipping 072 battle surface until every required
            // object is simultaneously ready instead of racing two rendered frames.
            var towerBattlePresentationDeadline081 = Time.realtimeSinceStartup + 8f;
            M2BattleExperienceController072 battleController081 = null;
            M2BattleDioramaView072 battleDiorama081 = null;
            Image authoredBattleBackdrop081 = null;
            GameObject forecastOrders081 = null;
            Button firstUnionButton081 = null;
            Text firstUnionLabel081 = null;
            Button firstForecastButton083 = null;
            Text firstForecastLabel083 = null;
            Button confirmForecasts081 = null;
            do
            {
                battleController081 = UnityEngine.Object
                    .FindFirstObjectByType<M2BattleExperienceController072>();
                battleDiorama081 = battleController081?.OwnedDiorama078;
                authoredBattleBackdrop081 = GameObject.Find(
                        "Authored Encounter Backdrop 072")
                    ?.GetComponent<Image>();
                forecastOrders081 = GameObject.Find("Complete Forecast Orders 072");
                firstUnionButton081 = FindActiveButtonByName081(
                    "Union Focus Chip 1 072");
                firstUnionLabel081 = firstUnionButton081?.GetComponentInChildren<Text>();
                firstForecastButton083 = FindActiveButtonByName081(
                    "Complete Forecast Order 1 072");
                firstForecastLabel083 = firstForecastButton083?.GetComponentInChildren<Text>();
                confirmForecasts081 = FindActiveButtonByName081(
                    "Confirm Complete Union Forecasts 072");
                if (battleController081 != null &&
                    battleController081.IsActive &&
                    battleDiorama081 != null &&
                    battleDiorama081.IsReady &&
                    battleDiorama081.Root.gameObject.activeInHierarchy &&
                    authoredBattleBackdrop081 != null &&
                    authoredBattleBackdrop081.gameObject.activeInHierarchy &&
                    authoredBattleBackdrop081.sprite != null &&
                    authoredBattleBackdrop081.sprite.texture != null &&
                    StringComparer.Ordinal.Equals(
                        battleDiorama081.ActiveBackdropResourceKey083,
                        TowerRunRules081.FloorOneCampaign083BattleArtResourcePath) &&
                    StringComparer.Ordinal.Equals(
                        battleDiorama081.ActiveBackdropTextureName083,
                        TowerRunRules081.FloorOneCampaign083BattleArtTextureName) &&
                    forecastOrders081 != null &&
                    forecastOrders081.activeInHierarchy &&
                    firstUnionButton081 != null &&
                    firstUnionButton081.IsInteractable() &&
                    firstUnionLabel081 != null &&
                    !string.IsNullOrWhiteSpace(firstUnionLabel081.text) &&
                    firstForecastButton083 != null &&
                    firstForecastButton083.IsInteractable() &&
                    firstForecastLabel083 != null &&
                    !string.IsNullOrWhiteSpace(firstForecastLabel083.text) &&
                    firstForecastLabel083.resizeTextMinSize >=
                        M2BattleCommandHud072.MinimumCriticalOrderFontSize074 &&
                    confirmForecasts081 != null)
                    break;
                yield return null;
            } while (Time.realtimeSinceStartup < towerBattlePresentationDeadline081);

            Require071(battleController081 != null,
                "The Floor 1 entry did not create the live battle controller.");
            Require071(battleController081.IsActive,
                "The Floor 1 live battle controller was not active.");
            Require071(battleDiorama081 != null,
                "The Floor 1 live battle controller did not own a diorama.");
            Require071(battleDiorama081.IsReady,
                "The Floor 1 battle diorama did not become ready.");
            Require071(battleDiorama081.Root != null &&
                       battleDiorama081.Root.gameObject.activeInHierarchy,
                "The Floor 1 battle diorama root was not active.");
            Require071(authoredBattleBackdrop081 != null &&
                       authoredBattleBackdrop081.gameObject.activeInHierarchy,
                "The Floor 1 authored battle backdrop was missing or inactive.");
            Require071(authoredBattleBackdrop081.sprite != null,
                "The Floor 1 authored battle backdrop had no sprite.");
            Require071(authoredBattleBackdrop081.sprite.texture != null,
                "The Floor 1 authored battle backdrop sprite had no texture.");
            Require071(StringComparer.Ordinal.Equals(
                           authoredBattleBackdrop081.sprite.texture.name,
                           TowerRunRules081.FloorOneCampaign083BattleArtTextureName),
                "The Floor 1 authored battle backdrop rendered the wrong texture.");
            Require071(StringComparer.Ordinal.Equals(
                           battleDiorama081.ActiveBackdropResourceKey083,
                           TowerRunRules081.FloorOneCampaign083BattleArtResourcePath),
                "The Floor 1 battle diorama resolved the wrong backdrop resource key.");
            Require071(StringComparer.Ordinal.Equals(
                           battleDiorama081.ActiveBackdropTextureName083,
                           TowerRunRules081.FloorOneCampaign083BattleArtTextureName),
                "The Floor 1 battle diorama resolved the wrong backdrop texture.");
            Require071(forecastOrders081 != null &&
                       forecastOrders081.activeInHierarchy,
                "The Floor 1 battle did not show its complete Forecast order panel.");
            Require071(firstUnionButton081 != null &&
                       firstUnionButton081.IsInteractable(),
                "The Floor 1 battle did not expose an interactable first Union selector.");
            Require071(firstUnionLabel081 != null &&
                       !string.IsNullOrWhiteSpace(firstUnionLabel081.text),
                "The Floor 1 first Union selector had no readable label.");
            Require071(firstForecastButton083 != null &&
                       firstForecastButton083.IsInteractable(),
                "The Floor 1 battle did not expose an interactable first Forecast order.");
            Require071(firstForecastLabel083 != null &&
                       !string.IsNullOrWhiteSpace(firstForecastLabel083.text),
                "The Floor 1 first Forecast order had no readable label.");
            Require071(firstForecastLabel083.resizeTextMinSize >=
                       M2BattleCommandHud072.MinimumCriticalOrderFontSize074,
                "The Floor 1 first Forecast order fell below the certified minimum font size.");
            Require071(confirmForecasts081 != null,
                "The Floor 1 battle did not show its complete-plan confirmation action.");
            Require071(!ActiveTowerPlaceholderCopyPresent083(),
                "Forbidden placeholder copy appeared in the Floor 1 live battle.");
            _report.towerEnemyArt700Verified = CertifyLiveEnemyArt700Actors090(
                openingBattle081,
                battleDiorama081,
                "Tower Floor 1",
                true);
            yield return Capture071("tower_floor_1_live_battle");
            yield return ResolveVisibleTowerBattle081();

            var resolvedBattle081 = _coordinator.State.Battle;
            _report.towerBattleRounds = resolvedBattle081?.LastResolvedRound ?? 0;
            _report.towerBattleFinalStateHash =
                resolvedBattle081?.FinalStateHash ?? string.Empty;
            _report.towerBattleResolvedVerified =
                resolvedBattle081 != null &&
                resolvedBattle081.IsResolved &&
                StringComparer.OrdinalIgnoreCase.Equals(
                    resolvedBattle081.Outcome,
                    "Victory") &&
                resolvedBattle081.LastResolvedRound > 0 &&
                resolvedBattle081.Reward != null &&
                !resolvedBattle081.Reward.Claimed &&
                !string.IsNullOrWhiteSpace(resolvedBattle081.FinalStateHash);
            Require071(_report.towerBattleResolvedVerified,
                "The real Floor 1 Guardian battle did not resolve to an authoritative unclaimed victory reward.");
            yield return WaitForInteractableButtonByName076(
                "Continue From Battle Results 072",
                8f);
            var receiptsBeforeBattleClaim081 =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount;
            ClickVisibleButtonByName076("Continue From Battle Results 072");
            yield return WaitForTowerStateAndUi084(
                value084 =>
                    _coordinator.State.Battle != null &&
                    _coordinator.State.Battle.Reward != null &&
                    _coordinator.State.Battle.Reward.Claimed &&
                    _coordinator.GuildCity017D.ClaimedBattleRewardCount ==
                        receiptsBeforeBattleClaim081 + 1 &&
                    value084.TowerBattleResolved &&
                    value084.TowerBattleWon &&
                    !string.IsNullOrWhiteSpace(value084.ActiveAbyssOperationId),
                () =>
                    FindActiveButtonByName081(
                        "Bank Tower battle victory 088") != null &&
                    GameObject.Find("Board Adventure Guild Pawn 086") == null &&
                    GameObject.Find(
                        "Board Adventure Next Face Down Physical Card 086") == null,
                8f);

            var afterBattleClaim081 = _coordinator.CampaignProgression022;
            Require071(_coordinator.State.Battle != null &&
                       _coordinator.State.Battle.Reward != null &&
                       _coordinator.State.Battle.Reward.Claimed &&
                       _coordinator.GuildCity017D.ClaimedBattleRewardCount ==
                       receiptsBeforeBattleClaim081 + 1 &&
                       afterBattleClaim081.TowerBattleResolved &&
                       afterBattleClaim081.TowerBattleWon &&
                       !string.IsNullOrWhiteSpace(afterBattleClaim081.ActiveAbyssOperationId) &&
                       FindActiveButtonByName081("Bank Tower battle victory 088") != null &&
                       GameObject.Find("Board Adventure Guild Pawn 086") == null &&
                       GameObject.Find("Board Adventure Next Face Down Physical Card 086") == null,
                "The existing battle reward did not claim exactly once and return to the active Tower floor.");

            var battleClaimHash081 = _coordinator.State.CanonicalStateHash;
            var battleClaimReceiptCount081 =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount;
            Require071(_coordinator.ClaimBattleRewards(),
                "repeat the already-claimed Tower battle reward");
            Require071(StringComparer.Ordinal.Equals(
                           _coordinator.State.CanonicalStateHash,
                           battleClaimHash081) &&
                       _coordinator.GuildCity017D.ClaimedBattleRewardCount ==
                       battleClaimReceiptCount081,
                "Reclaiming the Floor 1 battle reward changed the canonical save or duplicated its receipt.");

            var beforeFloorClaim081 = _coordinator.CampaignProgression022;
            var treasuryBeforeFloorClaim081 = _coordinator.State.TreasuryXp;
            var lifetimeBeforeFloorClaim081 = _coordinator.State.LifetimeGuildXp;
            var hallBeforeFloorClaim081 = _coordinator.State.HallEnhancementXp;
            var receiptsBeforeFloorClaim081 =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount;
            var floorGuildXpReward081 = beforeFloorClaim081.TowerGuildXpReward;
            var floorHallXpReward081 = beforeFloorClaim081.TowerHallXpReward;
            yield return WaitForInteractableButtonByName076(
                "Bank Tower battle victory 088",
                8f);
            ClickVisibleButtonByName076("Bank Tower battle victory 088");
            yield return WaitForTowerStateAndUi084(
                value084 =>
                    string.IsNullOrWhiteSpace(value084.ActiveAbyssOperationId) &&
                    !value084.HasPendingAbyssStepReceipt &&
                    !value084.HasPendingAbyssBattleReceipt,
                () => FindActiveButtonByName081(
                    "Climb next Tower floor 084") != null,
                8f);
            var bankedTower081 = _coordinator.CampaignProgression022;
            Require071(bankedTower081 != null,
                "The Floor 1 bank action did not restore the Tower progression projection.");
            Require071(string.IsNullOrWhiteSpace(
                    bankedTower081.ActiveAbyssOperationId),
                "The Floor 1 bank action did not close the active Tower operation.");
            Require071(!bankedTower081.HasPendingAbyssStepReceipt,
                "The Floor 1 bank action left a pending Tower step receipt.");
            Require071(!bankedTower081.HasPendingAbyssBattleReceipt,
                "The Floor 1 bank action left a pending Tower battle receipt.");
            Require071(FindActiveButtonByName081(
                           "Climb next Tower floor 084") != null,
                "The Floor 1 bank action did not return to the next-floor Tower lobby.");

            var cleared081 = _coordinator.CampaignProgression022;
            var clearedFloorOne081 = (cleared081.FloorStates ?? Array.Empty<AbyssFloorView022>())
                .SingleOrDefault(value081 => value081 != null &&
                    StringComparer.Ordinal.Equals(value081.FloorId, floorOneId081));
            _report.towerHighestClearedFloor = cleared081.HighestClearedTowerFloor;
            _report.towerTotalClears = cleared081.TotalTowerClears;
            _report.towerTerminalReceiptCountDelta =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount -
                receiptsBeforeFloorClaim081;
            var clearedHash081 = _coordinator.State.CanonicalStateHash;
            var clearedReceiptCount081 =
                _coordinator.GuildCity017D.ClaimedBattleRewardCount;
            var duplicateFloorClaim081 = _coordinator.AdvanceTowerRun081();
            _report.towerRewardExactOnceVerified =
                clearedFloorOne081 != null &&
                clearedFloorOne081.ClearCount == 1 &&
                cleared081.HighestClearedTowerFloor == 1 &&
                cleared081.TotalTowerClears == 1 &&
                string.IsNullOrWhiteSpace(cleared081.ActiveAbyssOperationId) &&
                _report.towerTerminalReceiptCountDelta == 1 &&
                floorGuildXpReward081 > 0 &&
                floorHallXpReward081 > 0 &&
                _coordinator.State.TreasuryXp ==
                treasuryBeforeFloorClaim081 + floorGuildXpReward081 &&
                _coordinator.State.LifetimeGuildXp ==
                lifetimeBeforeFloorClaim081 + floorGuildXpReward081 &&
                _coordinator.State.HallEnhancementXp ==
                hallBeforeFloorClaim081 + floorHallXpReward081 &&
                duplicateFloorClaim081 != null &&
                !duplicateFloorClaim081.Succeeded &&
                StringComparer.Ordinal.Equals(
                    _coordinator.State.CanonicalStateHash,
                    clearedHash081) &&
                _coordinator.GuildCity017D.ClaimedBattleRewardCount ==
                clearedReceiptCount081;
            Require071(_report.towerRewardExactOnceVerified,
                "The Floor 1 terminal receipt did not award one clear and its exact XP once, or a duplicate claim mutated the save.");

            var reloaded081 = new M1RuntimeCoordinator(contentRoot081, towerSavePath081);
            var reloadedTower081 = reloaded081.CampaignProgression022;
            var reloadedFloorOne081 = (reloadedTower081.FloorStates ??
                                      Array.Empty<AbyssFloorView022>())
                .SingleOrDefault(value081 => value081 != null &&
                    StringComparer.Ordinal.Equals(value081.FloorId, floorOneId081));
            _report.towerReloadedStateHash = reloaded081.State.CanonicalStateHash;
            _report.towerReloadVerified =
                StringComparer.Ordinal.Equals(
                    reloaded081.State.CanonicalStateHash,
                    clearedHash081) &&
                reloadedFloorOne081 != null &&
                reloadedFloorOne081.ClearCount == 1 &&
                reloadedTower081.HighestClearedTowerFloor == 1 &&
                reloadedTower081.TotalTowerClears == 1 &&
                reloadedTower081.TowerFloorNumber == 2 &&
                string.IsNullOrWhiteSpace(reloadedTower081.ActiveAbyssOperationId) &&
                reloaded081.GuildCity017D.ClaimedBattleRewardCount ==
                clearedReceiptCount081 &&
                reloaded081.State.TreasuryXp ==
                treasuryBeforeFloorClaim081 + floorGuildXpReward081 &&
                reloaded081.State.HallEnhancementXp ==
                hallBeforeFloorClaim081 + floorHallXpReward081;
            Require071(_report.towerReloadVerified,
                "The Floor 1 clear, exact rewards, next-floor unlock, or canonical hash did not survive Tower save reload.");
            Require071(File.Exists(_savePath) && StringComparer.Ordinal.Equals(
                           ComputeFileSha256076(_savePath),
                           storySaveSha256BeforeTower081),
                "The isolated Tower certification changed the completed story smoke save.");

            _coordinator = reloaded081;
            _presenter.Initialize(_coordinator);
            _presenter.ShowFirstHourGoldGuildTab071("ABYSS");
            yield return null;
            var floorTwoSummary084 = GameObject.Find("Tower Phone Simple Summary 084");
            var floorTwoSummaryCopy084 = floorTwoSummary084 == null
                ? string.Empty
                : string.Join(
                    "\n",
                    floorTwoSummary084.GetComponentsInChildren<Text>(true)
                        .Where(value084 => value084 != null &&
                                           value084.gameObject.activeInHierarchy)
                        .Select(value084 => value084.text ?? string.Empty));
            var floorTwoBegin081 = FindActiveButtonByName081(
                "Climb next Tower floor 084");
            var floorTwoBeginLabel081 = floorTwoBegin081?.GetComponentInChildren<Text>();
            var floorTwoBeginCopy081 = floorTwoBeginLabel081?.text ?? string.Empty;
            Require071(floorTwoSummary084 != null &&
                       floorTwoSummaryCopy084.IndexOf(
                           "FLOOR 2",
                           StringComparison.OrdinalIgnoreCase) >= 0 &&
                       floorTwoSummaryCopy084.IndexOf(
                           "BEST FLOOR  •  1",
                           StringComparison.OrdinalIgnoreCase) >= 0 &&
                       floorTwoBegin081 != null &&
                       floorTwoBegin081.IsInteractable() &&
                       floorTwoBeginLabel081 != null &&
                       floorTwoBeginCopy081.IndexOf(
                           "FIGHT FLOOR " + reloadedTower081.TowerFloorNumber,
                           StringComparison.OrdinalIgnoreCase) >= 0 &&
                       floorTwoBeginCopy081.IndexOf(
                           "YOUR NEXT UNION BATTLE",
                           StringComparison.OrdinalIgnoreCase) >= 0,
                "The reloaded Tower did not visibly present Floor 1 as cleared and Floor 2 as the next fight.");
            _report.endlessTowerFloorOneVerified =
                _report.towerFloorOneUiVerified &&
                _report.towerBattleResolvedVerified &&
                _report.towerRewardExactOnceVerified &&
                _report.towerReloadVerified;
            Require071(_report.endlessTowerFloorOneVerified,
                "The packaged Floor 1 Tower certification was incomplete.");
            yield return Capture071("tower_floor_1_clear_persisted");
        }

        private IEnumerator ResolveVisibleTowerBattle081()
        {
            var authoritativeExchangeCount081 = 0;
            for (var roundGuard081 = 0;
                 roundGuard081 < 30 &&
                 _coordinator.State.Battle != null &&
                 !_coordinator.State.Battle.IsResolved;
                 roundGuard081++)
            {
                _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.Battle);
                yield return null;
                var battleBefore081 = _coordinator.State.Battle;
                var actingUnions081 = (battleBefore081.PlayerUnions ??
                                       Array.Empty<M2BattleUnionView>())
                    .Count(value081 => value081 != null && value081.CanAct);
                Require071(actingUnions081 > 0,
                    "The active Tower battle had no Union able to receive a complete order.");
                for (var unionIndex081 = 0;
                     unionIndex081 < battleBefore081.PlayerUnions.Count;
                     unionIndex081++)
                {
                    var union081 = battleBefore081.PlayerUnions[unionIndex081];
                    if (union081 == null || !union081.CanAct) continue;
                    var choices081 = (battleBefore081.Forecasts ??
                                      Array.Empty<M2ForecastView>())
                        .Where(value081 => value081 != null &&
                            StringComparer.Ordinal.Equals(
                                value081.UnionId,
                                union081.UnionId))
                        .Take(VisibleBattleForecastLimit076)
                        .ToArray();
                    Require071(choices081.Length > 0,
                        "The Tower command screen exposed no complete Forecast for " +
                        union081.DisplayName + ".");
                    var choice081 = choices081.FirstOrDefault(value081 =>
                                         StringComparer.Ordinal.Equals(
                                             value081.CommandId,
                                             "CMD_ALL_OUT")) ??
                                    choices081.FirstOrDefault(value081 =>
                                        StringComparer.Ordinal.Equals(
                                            value081.CommandId,
                                            "CMD_FLANK")) ??
                                    choices081[0];
                    yield return SelectVisibleBattleForecast076(
                        battleBefore081,
                        unionIndex081,
                        union081,
                        choice081);
                    var selectedUnion081 = (_coordinator.State.Battle.PlayerUnions ??
                                            Array.Empty<M2BattleUnionView>())
                        .FirstOrDefault(value081 => value081 != null &&
                            StringComparer.Ordinal.Equals(
                                value081.UnionId,
                                union081.UnionId));
                    Require071(selectedUnion081 != null &&
                               (selectedUnion081.IsSelected ||
                                StringComparer.Ordinal.Equals(
                                    selectedUnion081.SelectedForecastId,
                                    choice081.ForecastId)),
                        "The shipping Tower order card did not commit the selected Forecast for " +
                        union081.DisplayName + ".");
                }

                var readied081 = _coordinator.State.Battle;
                Require071(readied081.CanConfirmRound &&
                           readied081.PlayerUnions
                               .Where(value081 => value081 != null && value081.CanAct)
                               .All(value081 => value081.IsSelected ||
                                   !string.IsNullOrWhiteSpace(
                                       value081.SelectedForecastId)),
                    "The Tower battle never reached the all-Unions-ready command state.");
                var stateHashBefore081 = readied081.StateHash ?? string.Empty;
                var roundBefore081 = readied081.LastResolvedRound;
                yield return ExecuteVisibleBattleRound076(readied081.BattleId);
                var exchanged081 = _coordinator.State.Battle;
                Require071(exchanged081 != null &&
                           exchanged081.LastResolvedRound > roundBefore081 &&
                           exchanged081.LastResolvedRoundEvents != null &&
                           exchanged081.LastResolvedRoundEvents.Count > 0 &&
                           !StringComparer.Ordinal.Equals(
                               exchanged081.StateHash,
                               stateHashBefore081),
                    "The visible Tower round command did not resolve an authoritative combat exchange.");
                authoritativeExchangeCount081++;
            }

            Require071(_coordinator.State.Battle != null &&
                       _coordinator.State.Battle.IsResolved &&
                       StringComparer.OrdinalIgnoreCase.Equals(
                           _coordinator.State.Battle.Outcome,
                           "Victory") &&
                       authoritativeExchangeCount081 > 0,
                "The Floor 1 Tower battle exceeded its round guard or did not end in victory.");
        }

        private static int MinimumCertifiedEncounterRounds078(string encounterId)
        {
            return StringComparer.Ordinal.Equals(
                encounterId,
                M2BattleCommandService.GateEaterBattleToken076)
                ? 3
                : 2;
        }

        private bool AllMandatoryBattleRoundMinimumsMet078()
        {
            var required078 = new[]
            {
                M2BattleCommandService.HallBreachBattleToken076,
                M2BattleCommandService.LanternRoadBattleToken076,
                M2BattleCommandService.GateEaterBattleToken076,
                M2BattleCommandService.FogStalkersBattleToken078,
                ChapterTwoSurveyorRescueEncounterId078
            };
            for (var index078 = 0; index078 < required078.Length; index078++)
            {
                var encounterId078 = required078[index078];
                if (!_mandatoryBattleRoundCounts078.TryGetValue(
                        encounterId078,
                        out var resolvedRoundCount078) ||
                    resolvedRoundCount078 < MinimumCertifiedEncounterRounds078(encounterId078))
                    return false;
            }
            return true;
        }

        private IEnumerator SelectVisibleBattleForecast076(
            M2BattleView battle076,
            int unionIndex076,
            M2BattleUnionView union076,
            M2ForecastView choice076)
        {
            Require071(battle076 != null && union076 != null && choice076 != null,
                "The visible Union-order selection received an incomplete battle choice.");
            var visibleChoices076 = (battle076.Forecasts ?? Array.Empty<M2ForecastView>())
                .Where(value => value != null && StringComparer.Ordinal.Equals(
                    value.UnionId,
                    union076.UnionId))
                .Take(VisibleBattleForecastLimit076)
                .ToArray();
            var forecastIndex076 = Array.FindIndex(
                visibleChoices076,
                value => StringComparer.Ordinal.Equals(
                    value.ForecastId,
                    choice076.ForecastId));
            Require071(forecastIndex076 >= 0,
                "The preferred complete Forecast was not exposed by the shipping command HUD for " +
                union076.DisplayName + ".");

            ClickVisibleButtonByName076(
                "Union Focus Chip " + (unionIndex076 + 1) + " 072");
            yield return null;
            ClickVisibleButtonByName076(
                "Complete Forecast Order " + (forecastIndex076 + 1) + " 072");
            yield return null;

            var selectedUnion076 = (_coordinator.State.Battle?.PlayerUnions ??
                                    Array.Empty<M2BattleUnionView>())
                .FirstOrDefault(value => value != null && StringComparer.Ordinal.Equals(
                    value.UnionId,
                    union076.UnionId));
            var selectedForecast076 = (_coordinator.State.Battle?.Forecasts ??
                                       Array.Empty<M2ForecastView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.UnionId, union076.UnionId) &&
                    StringComparer.Ordinal.Equals(value.ForecastId, choice076.ForecastId));
            Require071(selectedUnion076 != null &&
                       (StringComparer.Ordinal.Equals(
                            selectedUnion076.SelectedForecastId,
                            choice076.ForecastId) ||
                        (selectedForecast076 != null && selectedForecast076.IsSelected)),
                "Clicking the shipping order card did not select the expected complete Forecast for " +
                union076.DisplayName + ".");
        }

        private IEnumerator ExecuteVisibleBattleRound076(string encounterId076)
        {
            // Execute through the shipping 072 control, not the coordinator shortcut.
            // The coordinator remains authoritative, while this path also proves the
            // live sequence renderer consumed the selected members' exact Art recipe.
            _presenter.ShowFirstHourGoldSmokeScreen071(M1Screen.Battle);
            yield return null;
            var controller076 = UnityEngine.Object
                .FindFirstObjectByType<M2BattleExperienceController072>();
            Require071(controller076 != null && controller076.IsActive,
                "The shipping 072 battle controller was not active for " + encounterId076 + ".");
            var sequence076 = controller076.OwnedSequenceDirector078;
            Require071(sequence076 != null,
                "The shipping 072 battle sequence was unavailable for " + encounterId076 + ".");
            if (!_report.battleHpImpactPresentationVerified)
            {
                var speedButton091 = UnityEngine.Object.FindObjectsByType<Button>(
                    FindObjectsSortMode.None).FirstOrDefault(button =>
                        button.name == "Battle Playback Speed 091");
                Require071(speedButton091 != null && speedButton091.interactable &&
                           speedButton091.gameObject.activeInHierarchy,
                    "The shipping battle speed button is missing or unavailable.");
                controller076.AnimationSpeed = 1f;
                foreach (var expectedSpeed091 in new[] { 2f, 4f, 16f, 1f })
                {
                    speedButton091.onClick.Invoke();
                    Require071(Mathf.Approximately(controller076.AnimationSpeed, expectedSpeed091) &&
                               speedButton091.GetComponentsInChildren<Text>().Any(label =>
                                   label.text.Contains(expectedSpeed091.ToString("0") + "×")),
                        "The visible battle speed control did not cycle or update its label.");
                }
                Debug.Log("PASS: shipping_battle_speed_button_1x_2x_4x_16x_1x_108");
            }
            controller076.AnimationSpeed = 4f;

            var diorama076 = controller076.OwnedDiorama078;
            Require071(diorama076 != null,
                "The shipping battle had no live diorama for HP-impact verification.");
            var certifyFirstHpImpact076 = !_report.battleHpImpactPresentationVerified;
            // Run the single certified contact at readable speed. This prevents a
            // low-frame-rate player from advancing across two impacts between smoke
            // observations, then restores fast traversal after the evidence frame.
            if (certifyFirstHpImpact076) controller076.AnimationSpeed = 1f;
            var priorObservedExactImpactCount076 =
                diorama076.LiveArtRecipeDiagnostics076.ObservedImpactExactBeatCount;
            var priorHpImpactPresentationCount076 = diorama076.HpImpactPresentationCount076;
            var priorBreakthroughPresentationCount078 =
                diorama076.BreakthroughNotificationPresentationCount076;
            var presentationBattleBefore076 = _coordinator.State.Battle;
            if (!_report.campaignEnemyArt700Verified &&
                presentationBattleBefore076 != null &&
                !EnemyArtIdentity090.TryTowerFloor090(
                    presentationBattleBefore076.BattleId,
                    out _))
                _report.campaignEnemyArt700Verified = CertifyLiveEnemyArt700Actors090(
                    presentationBattleBefore076,
                    diorama076,
                    "Campaign battle " + encounterId076,
                    false);
            var presentedHpBefore076 = (presentationBattleBefore076.PlayerUnions ??
                                        Array.Empty<M2BattleUnionView>())
                .Concat(presentationBattleBefore076.EnemyUnions ??
                        Array.Empty<M2BattleUnionView>())
                .Where(union076 => union076 != null)
                .SelectMany(union076 => (union076.Members ??
                                         Array.Empty<M2BattleMemberView>())
                    .Where(member076 => member076 != null)
                    .Select(member076 => new
                    {
                        UnionId = union076.UnionId,
                        MemberId = member076.MemberId,
                        CurrentHp = member076.CurrentHp,
                        MaximumHp = member076.MaximumHp
                    }))
                .ToArray();
            var priorCompletedExactBeatCount076 = controller076.CompletedExactBeatCount076;
            var priorImpactSfxCount076 = controller076.ExactRecipeImpactSfxCount076;
            // One complete canonical 120-Art recipe is a release-wide renderer
            // certification, not a promise that every authoritative round chose one
            // of those exact node IDs. Later rounds may legally contain only legacy
            // presentation aliases (for example ART_BASIC_THRUST).
            var certifyLiveExactArtRecipe076 =
                !_report.liveFirstHourArtRecipeConsumptionVerified;
            ClickVisibleButtonByName076("Confirm Complete Union Forecasts 072");
            var terminalGateEaterRound078 =
                StringComparer.Ordinal.Equals(encounterId076, "ENCOUNTER071_GATE_EATER") &&
                _coordinator.State.Battle != null &&
                _coordinator.State.Battle.IsResolved &&
                StringComparer.OrdinalIgnoreCase.Equals(
                    _coordinator.State.Battle.Outcome,
                    "Victory");
            if (terminalGateEaterRound078) controller076.AnimationSpeed = 1f;
            yield return null;
            var resolvedRoundEvents078 = (_coordinator.State.Battle?.LastResolvedRoundEvents ??
                                          Array.Empty<M2BattleEventView>())
                .Where(value078 => value078 != null)
                .ToArray();
            var resolvedRoundHasBreakthrough078 =
                resolvedRoundEvents078.Any(value078 =>
                    StringComparer.OrdinalIgnoreCase.Equals(
                        value078.EventType,
                        "BREAKTHROUGH"));
            var expectedVisuallyStagedBeatCount078 = BattlePresentationPlanner
                .Plan(resolvedRoundEvents078)
                .Count(value078 => value078 != null && value078.VisuallyStaged);
            Require071(expectedVisuallyStagedBeatCount078 > 0,
                "The authoritative round produced no visually staged battle beats for " +
                encounterId076 + ".");
            var roundPresentationWindowSeconds078 = Mathf.Clamp(
                12f + expectedVisuallyStagedBeatCount078 * 1.5f,
                20f,
                120f);
            var roundPresentationDeadline078 =
                Time.realtimeSinceStartup + roundPresentationWindowSeconds078;

            if (certifyFirstHpImpact076)
            {
                var impactDeadline076 = Time.realtimeSinceStartup + 12f;
                while (controller076.IsResolving &&
                       Time.realtimeSinceStartup < impactDeadline076 &&
                       (diorama076.LiveArtRecipeDiagnostics076.ObservedImpactExactBeatCount <=
                            priorObservedExactImpactCount076 ||
                        diorama076.HpImpactPresentationCount076 <=
                            priorHpImpactPresentationCount076 ||
                        !diorama076.HpImpactVisible076))
                    yield return null;

                Require071(
                    diorama076.LiveArtRecipeDiagnostics076.ObservedImpactExactBeatCount >
                        priorObservedExactImpactCount076 &&
                    diorama076.HpImpactPresentationCount076 >
                        priorHpImpactPresentationCount076 &&
                    diorama076.HpImpactVisible076,
                    "The first exact Art impact did not visibly present a real HP change.");
                var hpImpactHeading076 = diorama076.HpImpactHeading076;
                var hpImpactDetail076 = diorama076.HpImpactDetail076;
                Require071(hpImpactHeading076 != null &&
                           hpImpactDetail076 != null &&
                           hpImpactHeading076.gameObject.activeInHierarchy &&
                           hpImpactDetail076.gameObject.activeInHierarchy,
                    "The HP impact callout was not active at the exact contact frame.");
                var signedHpDelta076 = 0;
                var beforeHp076 = 0;
                var afterHp076 = 0;
                var maximumHp076 = 0;
                Require071(
                    TryParseSignedHpImpact076(
                        hpImpactHeading076.text,
                        out signedHpDelta076) &&
                    TryParseBeforeAfterHpImpact076(
                        hpImpactDetail076.text,
                        out beforeHp076,
                        out afterHp076,
                        out maximumHp076) &&
                    afterHp076 < beforeHp076 &&
                    signedHpDelta076 < 0 &&
                    signedHpDelta076 == afterHp076 - beforeHp076 &&
                    diorama076.LastHpImpactDelta076 == signedHpDelta076 &&
                    diorama076.LastHpImpactBefore076 == beforeHp076 &&
                    diorama076.LastHpImpactAfter076 == afterHp076,
                    "The HP impact callout did not contain truthful damaging HP loss and a negative signed delta.");

                var changedPresentation076 = presentedHpBefore076
                    .Select(value076 => new
                    {
                        Baseline = value076,
                        Actor = diorama076.ResolveActor(
                            value076.MemberId,
                            value076.UnionId)
                    })
                    .FirstOrDefault(value076 =>
                        value076.Actor != null &&
                        StringComparer.Ordinal.Equals(
                            value076.Actor.MemberId,
                            value076.Baseline.MemberId) &&
                        StringComparer.Ordinal.Equals(
                            value076.Actor.UnionId,
                            value076.Baseline.UnionId) &&
                        value076.Actor.PresentedCurrentHp076 !=
                            value076.Baseline.CurrentHp);
                Require071(changedPresentation076 != null &&
                           changedPresentation076.Baseline.CurrentHp == beforeHp076 &&
                           changedPresentation076.Baseline.MaximumHp == maximumHp076 &&
                           changedPresentation076.Actor.PresentedCurrentHp076 == afterHp076 &&
                           changedPresentation076.Actor.PresentedMaximumHp076 == maximumHp076 &&
                           changedPresentation076.Actor.PresentedHpFill076 <
                               changedPresentation076.Baseline.CurrentHp /
                               (float)Math.Max(1, changedPresentation076.Baseline.MaximumHp) &&
                           changedPresentation076.Actor.HealthFill076 != null &&
                           changedPresentation076.Actor.HealthFill076.gameObject.activeInHierarchy &&
                           changedPresentation076.Actor.HealthFill076.rectTransform.anchorMax.x <
                               Mathf.Lerp(
                                   0.008f,
                                   0.992f,
                                   changedPresentation076.Baseline.CurrentHp /
                                   (float)Math.Max(1, changedPresentation076.Baseline.MaximumHp)) &&
                           changedPresentation076.Actor.HealthLabel076 != null &&
                           changedPresentation076.Actor.HealthLabel076.gameObject.activeInHierarchy &&
                           changedPresentation076.Actor.HealthLabel076.text.IndexOf(
                               "HP " + afterHp076 + " / " + maximumHp076,
                               StringComparison.Ordinal) >= 0,
                    "The signed HP callout changed, but the target actor's visible HP bar/value did not.");

                var hpImpactRect076 = diorama076.HpImpactCallout076?.rectTransform;
                Require071(hpImpactRect076 != null &&
                           hpImpactRect076.gameObject.activeInHierarchy &&
                           hpImpactRect076.anchorMax.x - hpImpactRect076.anchorMin.x >= 0.35f &&
                           hpImpactRect076.anchorMax.y - hpImpactRect076.anchorMin.y >= 0.10f &&
                           hpImpactHeading076.resizeTextMinSize >= 24 &&
                           hpImpactDetail076.resizeTextMinSize >=
                               M2BattleDioramaView072.MinimumHpImpactFontSize076,
                    "The HP change existed but was not prominent enough for normal play.");

                var certifiedHpHeading076 = hpImpactHeading076.text;
                var certifiedHpDetail076 = hpImpactDetail076.text;
                // This volatile proof is latched at the current frame's render
                // boundary. The player-facing 1.35-second callout remains unchanged.
                controller076.AnimationSpeed = 0.25f;
                yield return CaptureImmediate071(
                    "battle_1_first_exact_hp_impact_readout",
                    () => diorama076.HpImpactVisible076 &&
                          StringComparer.Ordinal.Equals(
                              diorama076.HpImpactHeading076?.text,
                              certifiedHpHeading076) &&
                          StringComparer.Ordinal.Equals(
                              diorama076.HpImpactDetail076?.text,
                              certifiedHpDetail076) &&
                          diorama076.LastHpImpactDelta076 == signedHpDelta076 &&
                          diorama076.LastHpImpactBefore076 == beforeHp076 &&
                          diorama076.LastHpImpactAfter076 == afterHp076,
                    "The exact HP impact changed before its same-frame evidence was latched.");
                // Keep the one release-certifying exact beat at readable cadence
                // through recovery and return-to-home. Only later beats accelerate.
                controller076.AnimationSpeed = certifyLiveExactArtRecipe076 ? 1f : 4f;
                _report.battleHpImpactPresentationVerified = true;
            }

            if (certifyLiveExactArtRecipe076)
            {
                var liveDeadline076 = Time.realtimeSinceStartup + 12f;
                while (controller076.IsResolving &&
                       Time.realtimeSinceStartup < liveDeadline076)
                {
                    if (controller076.CompletedExactBeatCount076 >
                        priorCompletedExactBeatCount076) break;
                    yield return null;
                }

                Require071(controller076.CompletedExactBeatCount076 >
                           priorCompletedExactBeatCount076,
                    "The visible 072 renderer did not complete the release-certifying exact Art beat through impact, recovery, and return-to-home before the certification deadline for " +
                    encounterId076 + ".");
                Require071(controller076.HasCompleteLiveArtRecipeConsumption076 &&
                           controller076.HasConsumedLiveExactArtRecipe076 &&
                           controller076.ExactRecipeImpactSfxCount076 > priorImpactSfxCount076 &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactArtId076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactRecipeId076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactMotionSignature076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactVfxSignature076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactSfxSignature076) &&
                           controller076.HasConsumedExactRecipeSfxSignature076(
                               controller076.LastCompletedExactSfxSignature076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactMotionRecipe076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactVfxRecipe076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactCameraRecipe076) &&
                           !string.IsNullOrWhiteSpace(controller076.LastCompletedExactTraceRecipe076) &&
                           controller076.LastCompletedExactDurationMilliseconds076 > 0 &&
                           controller076.LastCompletedExactImpactMilliseconds076 > 0 &&
                           controller076.LastCompletedExactImpactMilliseconds076 <
                           controller076.LastCompletedExactDurationMilliseconds076 &&
                           controller076.LastCompletedExactObservedImpactMilliseconds076 > 0 &&
                           controller076.LastCompletedExactObservedCompletionMilliseconds076 >
                           controller076.LastCompletedExactObservedImpactMilliseconds076 &&
                           controller076.LastCompletedExactTraceGeometryCount076 > 0,
                    "The completed exact Art beat did not preserve its authored motion, VFX, SFX, camera, trace geometry, and observed timing evidence for " +
                    encounterId076 + ".");
                _report.liveFirstHourArtRecipeConsumptionVerified = true;
                controller076.AnimationSpeed = 4f;
            }

            M2BattleEventView terminalGateEaterDownedEvent078 = null;
            if (terminalGateEaterRound078)
            {
                var terminalDownedEvents078 = resolvedRoundEvents078
                    .Where(value078 => StringComparer.OrdinalIgnoreCase.Equals(
                        value078.EventType,
                        "DOWNED"))
                    .ToArray();
                Require071(terminalDownedEvents078.Length == 1,
                    "The terminal Gate-Eater round must contain one authoritative DOWNED event.");
                terminalGateEaterDownedEvent078 = terminalDownedEvents078[0];
                var terminalDownedUnionId078 = !string.IsNullOrWhiteSpace(
                    terminalGateEaterDownedEvent078.TargetUnionId)
                    ? terminalGateEaterDownedEvent078.TargetUnionId
                    : terminalGateEaterDownedEvent078.UnionId;
                var terminalDownedMemberId078 = !string.IsNullOrWhiteSpace(
                    terminalGateEaterDownedEvent078.TargetMemberId)
                    ? terminalGateEaterDownedEvent078.TargetMemberId
                    : terminalGateEaterDownedEvent078.MemberId;
                var authoritativeDownedMember078 =
                    (_coordinator.State.Battle.EnemyUnions ??
                     Array.Empty<M2BattleUnionView>())
                    .Where(union078 => union078 != null && StringComparer.Ordinal.Equals(
                        union078.UnionId,
                        terminalDownedUnionId078))
                    .SelectMany(union078 => union078.Members ??
                        Array.Empty<M2BattleMemberView>())
                    .FirstOrDefault(member078 => member078 != null &&
                        StringComparer.Ordinal.Equals(
                            member078.MemberId,
                            terminalDownedMemberId078));
                Require071(authoritativeDownedMember078 != null &&
                           authoritativeDownedMember078.Downed &&
                           authoritativeDownedMember078.CurrentHp == 0,
                    "The terminal Gate-Eater DOWNED event did not match the authoritative zero-HP enemy.");

                var terminalDownedActor078 = diorama076.ResolveActor(
                    terminalDownedMemberId078,
                    terminalDownedUnionId078);
                while (controller076.IsResolving &&
                       Time.realtimeSinceStartup < roundPresentationDeadline078 &&
                       !HasVisibleZeroHpDownedPresentation078(terminalDownedActor078))
                    yield return null;
                Require071(controller076.IsResolving &&
                           GameObject.Find("Battle Results Payoff 072") == null &&
                           HasVisibleZeroHpDownedPresentation078(terminalDownedActor078),
                    "The lethal Gate-Eater hit did not visibly reach zero HP and the DOWNED pose before results.");
                controller076.AnimationSpeed = 0.25f;
                yield return Capture071(
                    "battle_3_gate_eater_lethal_hp_zero_downed_before_results");
                Require071(controller076.IsResolving &&
                           GameObject.Find("Battle Results Payoff 072") == null &&
                           HasVisibleZeroHpDownedPresentation078(terminalDownedActor078),
                    "The Gate-Eater zero-HP DOWNED evidence frame raced into the results screen.");
                controller076.AnimationSpeed = 4f;
            }

            // A breakthrough round must reach its shipping transient notice before
            // any acceleration. This capture seam lives inside the executing round,
            // rather than inspecting stale events after Execute returns.
            if (controller076.IsResolving &&
                resolvedRoundHasBreakthrough078 &&
                !_report.transientLearnedArtNoticeVerified)
                yield return CaptureTransientLearnedArtNotice078(
                    controller076,
                    diorama076,
                    priorBreakthroughPresentationCount078);

            // Every remaining shipping beat runs at the accelerated player-facing
            // speed. The sequence must complete and present a nonempty bounded
            // subset; routine beats intentionally reconcile immediately at 4x/16x.
            controller076.AnimationSpeed = 4f;
            while (controller076.IsResolving &&
                   Time.realtimeSinceStartup < roundPresentationDeadline078)
                yield return null;
            Require071(!controller076.IsResolving,
                "The visible 072 round presentation did not complete all " +
                expectedVisuallyStagedBeatCount078 + " staged beats within " +
                roundPresentationWindowSeconds078 + " seconds for " + encounterId076 + ".");
            Require071(sequence076.LastPresentedBeatCount > 0 &&
                       sequence076.LastPresentedBeatCount <=
                           expectedVisuallyStagedBeatCount078,
                "The shipping 072 sequence presented " +
                sequence076.LastPresentedBeatCount + " of " +
                expectedVisuallyStagedBeatCount078 + " visually staged beats for " +
                encounterId076 + ".");

            if (terminalGateEaterRound078)
            {
                var terminalDownedUnionId078 = !string.IsNullOrWhiteSpace(
                    terminalGateEaterDownedEvent078.TargetUnionId)
                    ? terminalGateEaterDownedEvent078.TargetUnionId
                    : terminalGateEaterDownedEvent078.UnionId;
                var terminalDownedMemberId078 = !string.IsNullOrWhiteSpace(
                    terminalGateEaterDownedEvent078.TargetMemberId)
                    ? terminalGateEaterDownedEvent078.TargetMemberId
                    : terminalGateEaterDownedEvent078.MemberId;
                diorama076.Focus(
                    terminalGateEaterDownedEvent078.ActorUnionId,
                    terminalDownedUnionId078);
                var completedDownedActor078 = diorama076.ResolveActor(
                    terminalDownedMemberId078,
                    terminalDownedUnionId078);
                Require071(completedDownedActor078 != null &&
                           completedDownedActor078.Downed &&
                           HasVisibleZeroHpDownedPresentation078(completedDownedActor078),
                    "The completed Gate-Eater round did not retain its zero-HP DOWNED presentation.");
            }
        }

        private static bool CertifyLiveEnemyArt700Actors090(
            M2BattleView battle090,
            M2BattleDioramaView072 diorama090,
            string context090,
            bool requireTowerFloorOneIdentity090)
        {
            Require071(battle090 != null,
                context090 + " had no authoritative battle view for Enemy Art 700 proof.");
            Require071(diorama090 != null && diorama090.IsReady,
                context090 + " had no ready live diorama for Enemy Art 700 proof.");

            if (requireTowerFloorOneIdentity090)
                Require071(EnemyArtIdentity090.TryTowerFloor090(
                               battle090.BattleId,
                               out var towerFloor090) &&
                           towerFloor090 == 1,
                    context090 + " did not retain the exact Floor 1 Enemy Art 700 identity route.");

            var enemyUnions090 = (battle090.EnemyUnions ??
                                  Array.Empty<M2BattleUnionView>())
                .Where(union090 => union090 != null)
                .ToArray();
            Require071(enemyUnions090.Length > 0,
                context090 + " had no enemy Unions to certify against Enemy Art 700.");

            var certifiedActors090 = 0;
            var originalPlayerUnionId090 = diorama090.FocusedPlayerUnionId;
            var originalEnemyUnionId090 = diorama090.TargetEnemyUnionId;
            var certificationPlayerUnionId090 = originalPlayerUnionId090;
            if (string.IsNullOrWhiteSpace(certificationPlayerUnionId090))
                certificationPlayerUnionId090 = (battle090.PlayerUnions ??
                                                  Array.Empty<M2BattleUnionView>())
                    .FirstOrDefault(union090 => union090 != null)
                    ?.UnionId ?? string.Empty;
            try
            {
                for (var unionIndex090 = 0;
                     unionIndex090 < enemyUnions090.Length;
                     unionIndex090++)
                {
                    var union090 = enemyUnions090[unionIndex090];
                    diorama090.Focus(certificationPlayerUnionId090, union090.UnionId);
                    Require071(StringComparer.Ordinal.Equals(
                                   diorama090.TargetEnemyUnionId,
                                   union090.UnionId),
                        context090 + " could not focus enemy Union " +
                        union090.UnionId + " for Enemy Art 700 proof.");
                    var members090 = (union090.Members ??
                                      Array.Empty<M2BattleMemberView>())
                        .Where(member090 => member090 != null)
                        .ToArray();
                    Require071(members090.Length > 0,
                        context090 + " contained an enemy Union with no members to render.");

                    for (var memberIndex090 = 0;
                         memberIndex090 < members090.Length;
                         memberIndex090++)
                    {
                        var member090 = members090[memberIndex090];
                        Require071(!string.IsNullOrWhiteSpace(member090.EnemyArtBaseId090) &&
                                   !string.IsNullOrWhiteSpace(member090.EnemyArtVariantId090),
                            context090 + " enemy " + member090.MemberId +
                            " did not expose a committed Enemy Art 700 identity.");

                        if (requireTowerFloorOneIdentity090)
                        {
                            var expectedBaseIndex090 = unionIndex090 * 3 + memberIndex090 + 1;
                            Require071(StringComparer.Ordinal.Equals(
                                           member090.EnemyArtBaseId090,
                                           EnemyArtIdentity090.BaseId090(expectedBaseIndex090)) &&
                                       StringComparer.Ordinal.Equals(
                                           member090.EnemyArtVariantId090,
                                           EnemyArtIdentity090.VariantId090(
                                               expectedBaseIndex090,
                                               1)),
                                context090 + " enemy " + member090.MemberId +
                                " did not retain its deterministic Floor 1 family and variant.");
                        }

                        var actor090 = diorama090.ResolveActor(
                            member090.MemberId,
                            union090.UnionId);
                        Require071(actor090 != null && actor090.Enemy,
                            context090 + " enemy " + member090.MemberId +
                            " had no live enemy actor in the shipping diorama.");
                        Require071(actor090.CurrentArtwork076 != null &&
                                   actor090.CurrentArtwork076.gameObject.activeInHierarchy &&
                                   actor090.CurrentArtwork076.sprite != null &&
                                   actor090.CurrentArtwork076.sprite.texture != null,
                            context090 + " enemy " + member090.MemberId +
                            " had no visibly rendered Enemy Art 700 sprite.");

                        var expectedResourcePrefix090 =
                            "EnemyArt700/" + member090.EnemyArtVariantId090 + "/";
                        Require071(actor090.CurrentResourcePath.StartsWith(
                                       expectedResourcePrefix090,
                                       StringComparison.Ordinal) &&
                                   (actor090.CurrentResourcePath.EndsWith(
                                        "/IDLE",
                                        StringComparison.Ordinal) ||
                                    actor090.CurrentResourcePath.EndsWith(
                                        "/ATTACK",
                                        StringComparison.Ordinal)),
                            context090 + " enemy " + member090.MemberId +
                            " rendered a fallback instead of its Enemy Art 700 idle/attack pair.");
                        Require071(StringComparer.Ordinal.Equals(
                                       actor090.EnemyArtworkShaderName075,
                                       M2BattleActorRig072.EnemyCutoutShaderName075) &&
                                   actor090.CurrentArtwork076.material != null &&
                                   actor090.CurrentArtwork076.material.shader != null &&
                                   StringComparer.Ordinal.Equals(
                                       actor090.CurrentArtwork076.material.shader.name,
                                       M2BattleActorRig072.EnemyCutoutShaderName075),
                            context090 + " enemy " + member090.MemberId +
                            " did not render through the certified fringe-cleanup shader.");
                        certifiedActors090++;
                    }
                }
            }
            finally
            {
                diorama090.Focus(originalPlayerUnionId090, originalEnemyUnionId090);
            }

            Require071(certifiedActors090 > 0,
                context090 + " certified zero live Enemy Art 700 actors.");
            return true;
        }

        private static bool HasVisibleZeroHpDownedPresentation078(
            M2BattleActorRig072 actor078)
        {
            return actor078 != null && actor078.Enemy &&
                   actor078.PresentedMaximumHp076 > 0 &&
                   actor078.PresentedCurrentHp076 == 0 &&
                   Mathf.Approximately(actor078.PresentedHpFill076, 0f) &&
                   actor078.HealthFill076 != null &&
                   actor078.HealthFill076.gameObject.activeInHierarchy &&
                   actor078.HealthLabel076 != null &&
                   actor078.HealthLabel076.gameObject.activeInHierarchy &&
                   StringComparer.Ordinal.Equals(
                       actor078.HealthLabel076.text,
                       "HP 0 / " + actor078.PresentedMaximumHp076) &&
                   StringComparer.Ordinal.Equals(
                       actor078.CurrentPoseId,
                       BattleArtPoseDirector011.Downed);
        }

        private IEnumerator CaptureTransientLearnedArtNotice078(
            M2BattleExperienceController072 controller078,
            M2BattleDioramaView072 diorama078,
            int priorPresentationCount078)
        {
            controller078.AnimationSpeed = 4f;
            var visibleDeadline078 = Time.realtimeSinceStartup + 20f;
            while (controller078.IsResolving &&
                   Time.realtimeSinceStartup < visibleDeadline078 &&
                   !diorama078.BreakthroughNotificationVisible076)
                yield return null;

            var heading078 = diorama078.BreakthroughNotificationHeading076;
            var detail078 = diorama078.BreakthroughNotificationDetail076;
            var signature078 = diorama078.ActiveBreakthroughNotificationSignature076;
            Require071(diorama078.BreakthroughNotificationVisible076 &&
                       diorama078.BreakthroughNotificationPresentationCount076 >
                           priorPresentationCount078 &&
                       diorama078.BreakthroughNotificationRemainingSeconds076 > 0f &&
                       diorama078.BreakthroughNotificationRemainingSeconds076 <=
                           M2BattleDioramaView072.BreakthroughNotificationSeconds076 + 0.05f &&
                       heading078 != null && heading078.gameObject.activeInHierarchy &&
                       detail078 != null && detail078.gameObject.activeInHierarchy &&
                       HasReadableTransientLearnedArtCopy078(
                           heading078.text,
                           detail078.text) &&
                       !string.IsNullOrWhiteSpace(signature078),
                "The authoritative breakthrough did not become a readable transient learned-Art notice.");
            controller078.AnimationSpeed = 0.25f;
            yield return CaptureImmediate071(
                "battle_learned_art_notice_visible",
                () => diorama078.BreakthroughNotificationVisible076 &&
                      StringComparer.Ordinal.Equals(
                          diorama078.ActiveBreakthroughNotificationSignature076,
                          signature078),
                "The learned-Art notice changed before its same-frame evidence was latched.");
            Require071(diorama078.BreakthroughNotificationVisible076 &&
                       StringComparer.Ordinal.Equals(
                           diorama078.ActiveBreakthroughNotificationSignature076,
                           signature078),
                "The learned-Art notice vanished before its evidence frame completed.");
            controller078.AnimationSpeed = 4f;

            var clearedDeadline078 = Time.realtimeSinceStartup + 12f;
            while (Time.realtimeSinceStartup < clearedDeadline078 &&
                   (diorama078.BreakthroughNotificationVisible076 ||
                    diorama078.QueuedBreakthroughNotificationCount076 > 0))
                yield return null;
            Require071(!diorama078.BreakthroughNotificationVisible076 &&
                       diorama078.QueuedBreakthroughNotificationCount076 == 0 &&
                       string.IsNullOrWhiteSpace(
                           diorama078.ActiveBreakthroughNotificationSignature076) &&
                       diorama078.BreakthroughNotificationRemainingSeconds076 == 0f,
                "The learned-Art notice remained visible after its bounded notification window.");
            var stableRoundDeadline078 = Time.realtimeSinceStartup + 30f;
            while (controller078.IsResolving &&
                   Time.realtimeSinceStartup < stableRoundDeadline078)
                yield return null;
            Require071(!controller078.IsResolving,
                "The learned-Art cleared proof never reached a stable completed-round state.");
            var clearedImpactDeadline078 = Time.realtimeSinceStartup + 4f;
            while (diorama078.HpImpactVisible076 &&
                   Time.realtimeSinceStartup < clearedImpactDeadline078)
                yield return null;
            Require071(!diorama078.HpImpactVisible076,
                "The learned-Art cleared proof still contained an unrelated transient HP callout.");
            yield return Capture071("battle_learned_art_notice_cleared");
            Require071(!diorama078.BreakthroughNotificationVisible076,
                "The cleared learned-Art notice reopened from old battle events.");
            _report.transientLearnedArtNoticeVerified = true;
        }

        public static bool HasReadableTransientLearnedArtCopy078(
            string heading078,
            string detail078)
        {
            if (!StringComparer.OrdinalIgnoreCase.Equals(
                    (heading078 ?? string.Empty).Trim(),
                    "NEW ART LEARNED") ||
                string.IsNullOrWhiteSpace(detail078))
                return false;

            var lines078 = detail078
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Split('\n');
            return lines078.Length == 3 &&
                   lines078[0].Trim().Length > "LEARNED".Length &&
                   lines078[0].Trim().EndsWith(
                       " LEARNED",
                       StringComparison.OrdinalIgnoreCase) &&
                   !string.IsNullOrWhiteSpace(lines078[1]) &&
                   StringComparer.OrdinalIgnoreCase.Equals(
                       lines078[2].Trim(),
                       TransientLearnedArtReadyPhrase078);
        }

        public static bool TryParseSignedHpImpact076(
            string heading076,
            out int signedDelta076)
        {
            signedDelta076 = 0;
            if (string.IsNullOrWhiteSpace(heading076)) return false;
            var hpSuffix076 = heading076.LastIndexOf(" HP", StringComparison.Ordinal);
            if (hpSuffix076 <= 0) return false;
            var beforeSuffix076 = heading076.Substring(0, hpSuffix076);
            var separator076 = beforeSuffix076.LastIndexOf('•');
            if (separator076 < 0 || separator076 + 1 >= beforeSuffix076.Length) return false;
            var signedValue076 = beforeSuffix076.Substring(separator076 + 1).Trim();
            return signedValue076.Length > 1 &&
                   (signedValue076[0] == '+' || signedValue076[0] == '-') &&
                   int.TryParse(signedValue076, out signedDelta076) &&
                   signedDelta076 != 0;
        }

        public static bool TryParseBeforeAfterHpImpact076(
            string detail076,
            out int beforeHp076,
            out int afterHp076,
            out int maximumHp076)
        {
            beforeHp076 = 0;
            afterHp076 = 0;
            maximumHp076 = 0;
            if (string.IsNullOrWhiteSpace(detail076)) return false;
            // The presentation intentionally owns two truthful HP scopes: the
            // impacted member on line one and the whole Union on line two. Parse
            // only the member line so the Union total's second slash cannot make
            // the member maximum look non-numeric to the certification smoke.
            var memberLineEnd076 = detail076.IndexOf('\n');
            if (memberLineEnd076 < 0) memberLineEnd076 = detail076.IndexOf('\r');
            var memberLine076 = memberLineEnd076 >= 0
                ? detail076.Substring(0, memberLineEnd076)
                : detail076;
            var hpPrefix076 = memberLine076.IndexOf("HP ", StringComparison.Ordinal);
            var arrow076 = memberLine076.IndexOf('→');
            var slash076 = memberLine076.IndexOf('/');
            if (hpPrefix076 < 0 || arrow076 <= hpPrefix076 + 3 || slash076 <= arrow076 + 1)
                return false;
            return int.TryParse(
                       memberLine076.Substring(
                           hpPrefix076 + 3,
                           arrow076 - hpPrefix076 - 3).Trim(),
                       out beforeHp076) &&
                   int.TryParse(
                       memberLine076.Substring(
                           arrow076 + 1,
                           slash076 - arrow076 - 1).Trim(),
                       out afterHp076) &&
                   int.TryParse(
                       memberLine076.Substring(slash076 + 1).Trim(),
                       out maximumHp076) &&
                   maximumHp076 > 0;
        }

        private void RequireCertificationFrame076()
        {
            var isReviewFrame = Screen.width == 1280 && Screen.height == 800;
            var isStudioFrame = Screen.width == 1920 && Screen.height == 1080;
            Require071(isReviewFrame || isStudioFrame,
                "Certification screenshots must run at 1280x800 or 1920x1080; actual frame was " +
                Screen.width + "x" + Screen.height + ".");
            _report.certificationFrame = Screen.width + "x" + Screen.height;
        }

        private void VerifyLanternPatrolCeremonyPage076(int page)
        {
            Require071(GameObject.Find("Lantern Patrol Rescue Ceremony 076") != null,
                "The rescued Lantern Patrol was added silently instead of receiving its story ceremony.");
            var expectedIds = FirstHourRosterService071.PatrolStableRecruitIds
                .Skip(Mathf.Clamp(page, 0, 1) * 5)
                .Take(5)
                .ToArray();
            Require071(expectedIds.Length == 5,
                "The certified Lantern Patrol authority does not contain ten people.");
            foreach (var recruitId in expectedIds)
                Require071(GameObject.Find(
                               "Rescued Lantern Patrol Portrait " + recruitId + " 076") != null,
                    "The patrol ceremony page did not show canonical recruit " + recruitId + ".");
            Require071(CountActiveNamedObjects076("Rescued Lantern Patrol Portrait ") == 5,
                "Each patrol ceremony page must show exactly five distinct rescued members.");
        }

        private void VerifyLanternPatrolVisualIdentityAssets076()
        {
            var stableIds = FirstHourRosterService071.PatrolStableRecruitIds.ToArray();
            Require071(stableIds.Length == 10 &&
                       stableIds.Distinct(StringComparer.Ordinal).Count() == 10,
                "The Lantern Patrol authority must contain ten distinct stable identities.");
            var portraitKeys = new HashSet<string>(StringComparer.Ordinal);
            var standeeKeys = new HashSet<string>(StringComparer.Ordinal);
            var actionKeys = new HashSet<string>(StringComparer.Ordinal);
            var textureInstances = new HashSet<int>();

            foreach (var stableId in stableIds)
            {
                var recruit = (_coordinator.State.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .FirstOrDefault(value => value != null &&
                        (StringComparer.Ordinal.Equals(value.PortraitAuthorityId, stableId) ||
                         StringComparer.Ordinal.Equals(value.RecruitId, stableId)));
                Require071(recruit != null,
                    "The permanent roster is missing Lantern Patrol identity " + stableId + ".");

                var expectedPortrait = M1VisualAssets.FirstHourPortraitRoot076 + "/" +
                                       stableId + "_PORTRAIT_076";
                Require071(M1VisualAssets.TryResolvePortrait(
                               recruit.RecruitId,
                               recruit.VisualSeed,
                               recruit.RaceId,
                               recruit.PortraitAuthorityId,
                               out var portrait,
                               out var portraitKey) &&
                           portrait != null && portrait.texture != null,
                    "The packaged portrait is unavailable for " + stableId + ".");
                Require071(StringComparer.Ordinal.Equals(portraitKey, expectedPortrait) &&
                           portrait.texture.width == 1254 && portrait.texture.height == 1254 &&
                           portraitKeys.Add(portraitKey) &&
                           textureInstances.Add(portrait.texture.GetInstanceID()),
                    "The packaged portrait mapping is stale, duplicated, or resized for " + stableId + ".");

                var expectedStandee = M1VisualAssets.FirstHourBattleRoot076 + "/STANDEE_" +
                                      stableId + "_076";
                Require071(M1VisualAssets.TryResolveBattleStandee(
                               recruit.RecruitId,
                               recruit.VisualSeed,
                               recruit.RaceId,
                               recruit.PortraitAuthorityId,
                               out var standee,
                               out var standeeKey) &&
                           standee != null && standee.texture != null,
                    "The packaged battle standee is unavailable for " + stableId + ".");
                Require071(StringComparer.Ordinal.Equals(standeeKey, expectedStandee) &&
                           standee.texture.width >= 850 &&
                           standee.texture.height >= 1500 &&
                           standee.texture.height > standee.texture.width &&
                           standeeKeys.Add(standeeKey) &&
                           textureInstances.Add(standee.texture.GetInstanceID()),
                    "The packaged battle standee mapping is stale or duplicated for " + stableId + ".");

                var expectedAction = M1VisualAssets.FirstHourBattleRoot076 + "/ACTION_" +
                                     stableId + "_076";
                Require071(M1VisualAssets.TryResolveBattleActionPose(
                               recruit.RecruitId,
                               recruit.VisualSeed,
                               recruit.RaceId,
                               recruit.PortraitAuthorityId,
                               out var action,
                               out var actionKey) &&
                           action != null && action.texture != null,
                    "The packaged battle action pose is unavailable for " + stableId + ".");
                Require071(StringComparer.Ordinal.Equals(actionKey, expectedAction) &&
                           action.texture.width >= 850 &&
                           action.texture.height >= 1500 &&
                           action.texture.height > action.texture.width &&
                           actionKeys.Add(actionKey) &&
                           textureInstances.Add(action.texture.GetInstanceID()),
                    "The packaged battle action mapping is stale or duplicated for " + stableId + ".");

                _report.portraitResourceKeys.Add(portraitKey);
                _report.standeeResourceKeys.Add(standeeKey);
                _report.actionResourceKeys.Add(actionKey);
            }

            Require071(portraitKeys.Count == 10 && standeeKeys.Count == 10 &&
                       actionKeys.Count == 10 && textureInstances.Count == 30,
                "The packaged Lantern Patrol must contain thirty distinct identity-matched art assets.");
            _report.characterIdentityArtVerified = true;
        }

        private static Button FindVisibleAction076(string label)
        {
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(
                    value.name,
                    "M1 Playable Proof Canvas"));
            return canvas == null ? null : canvas.GetComponentsInChildren<Button>()
                .FirstOrDefault(value =>
                    value != null && value.gameObject.activeInHierarchy &&
                    value.GetComponentsInChildren<Text>().Any(text =>
                        text != null && StringComparer.Ordinal.Equals(text.text, label)));
        }

        private static void RequireNamedVisibleTextContains076(string objectName, string expected)
        {
            var text = FindVisibleTextByNamePrefix076(objectName);
            Require071(text != null && text.gameObject.activeInHierarchy,
                "Visible text object was not found: " + objectName);
            Require071(text.text != null &&
                       text.text.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0,
                objectName + " did not contain: " + expected);
        }

        private static void RequireNamedVisibleTextNotContains076(string objectName, string forbidden)
        {
            var text = FindVisibleTextByNamePrefix076(objectName);
            Require071(text != null && text.gameObject.activeInHierarchy,
                "Visible text object was not found: " + objectName);
            Require071(text.text != null &&
                       text.text.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) < 0,
                objectName + " still contained internal copy: " + forbidden);
        }

        private static void RequireNamedVisibleTextEquals076(string objectName, string expected)
        {
            var text = FindVisibleTextByNamePrefix076(objectName);
            Require071(text != null && text.gameObject.activeInHierarchy,
                "Visible text object was not found: " + objectName);
            Require071(StringComparer.Ordinal.Equals(text.text, expected),
                objectName + " did not match the certified story state.");
        }

        private void RequireBoardQuestDiceChoice081(string label)
        {
            var expedition = _coordinator?.GuildCity017D?.Expedition;
            Require071(expedition != null && expedition.CurrentEventUsesCommitted2d6,
                label + " did not use the committed 2d6 authority.");
            if (expedition.HasCommittedCheckAtCurrentNode)
            {
                Require071(GameObject.Find("Board Quest Dice Result 081") != null,
                    label + " saved its dice but did not show the physical result.");
                return;
            }
            RequireNamedVisibleTextContains076(
                "Board Quest Plain Prompt Copy 081",
                "best useful crew");
            RequireNamedVisibleTextContains076(
                "Board Quest Plain Prompt Copy 081",
                "no approach menu");
            RequireNamedVisibleTextContains076(
                "Expedition Check Lead Heading 074",
                "READY");

            var primary = GameObject.Find("Expedition Primary Context Action 074")
                ?.GetComponent<Button>();
            Require071(primary != null && primary.gameObject.activeInHierarchy,
                label + " did not expose the automatic visible dice roll.");
            var primaryCopy = string.Join(
                "\n",
                primary.GetComponentsInChildren<Text>(true)
                    .Where(value => value != null)
                    .Select(value => value.text ?? string.Empty));
            var automaticRoll081 =
                primaryCopy.IndexOf(
                    "ROLLING 2D6", StringComparison.OrdinalIgnoreCase) >= 0 &&
                primaryCopy.IndexOf(
                    "WATCH THE DICE", StringComparison.OrdinalIgnoreCase) >= 0 &&
                !primary.interactable;
            var fallbackRoll081 =
                primaryCopy.IndexOf(
                    "ROLL 2D6", StringComparison.OrdinalIgnoreCase) >= 0 &&
                primaryCopy.IndexOf(
                    "AUTO-RESOLVE", StringComparison.OrdinalIgnoreCase) >= 0 &&
                primary.interactable;
            Require071((automaticRoll081 || fallbackRoll081) &&
                       primaryCopy.IndexOf(
                           "TEAM UP", StringComparison.OrdinalIgnoreCase) < 0 &&
                       primaryCopy.IndexOf(
                           "MOVE FAST", StringComparison.OrdinalIgnoreCase) < 0 &&
                       primaryCopy.IndexOf('%') < 0 &&
                       primaryCopy.IndexOf(
                           "PROMISED CONSEQUENCE", StringComparison.OrdinalIgnoreCase) < 0,
                label + " did not present one automatic 2d6 roll without an approach menu.");
        }

        private static void RequireBoardQuestStoryBackdrop081(string label)
        {
            var backdrop = GameObject.Find("Expedition Illustrated Story Backdrop 076")
                ?.GetComponent<Image>();
            Require071(backdrop != null && backdrop.gameObject.activeInHierarchy &&
                       backdrop.sprite != null,
                label + " did not retain the authored Board Quest story backdrop.");
        }

        private void RequireChapterTwoCommitted2d6Resolution081(string label)
        {
            var expedition = _coordinator.GuildCity017D.Expedition;
            Require071(expedition != null &&
                       expedition.CurrentEventUsesCommitted2d6 &&
                       expedition.HasCommittedCheckAtCurrentNode &&
                       expedition.LastCheckDieOne >= 1 &&
                       expedition.LastCheckDieOne <= 6 &&
                       expedition.LastCheckDieTwo >= 1 &&
                       expedition.LastCheckDieTwo <= 6 &&
                       expedition.LastCheckTotal ==
                       expedition.LastCheckDieOne + expedition.LastCheckDieTwo +
                       expedition.LastCheckModifier,
                label + " did not preserve its committed 2d6 result and truthful total.");
            Require071(GameObject.Find("Board Quest Dice Result 081") != null,
                label + " did not show the committed dice result in the packaged UI.");
            var signedModifier081 = expedition.LastCheckModifier >= 0
                ? "+" + expedition.LastCheckModifier
                : expedition.LastCheckModifier.ToString();
            RequireNamedVisibleTextEquals076(
                "Board Quest Dice Numbers 081",
                "DICE SETTLED  •  SAVED RESULT");
            Require071(GameObject.Find("Authoritative Dice Roll 084") != null &&
                       GameObject.Find("First Authoritative Die 084") != null &&
                       GameObject.Find("Second Authoritative Die 084") != null,
                label + " did not render both physical authoritative dice.");
            RequireNamedVisibleTextEquals076(
                "Authoritative Dice Equation 084",
                signedModifier081 + "  =  " + expedition.LastCheckTotal);
            RequireNamedVisibleTextEquals076(
                "Board Quest Dice Outcome 081",
                BoardQuestRules081.OutcomeHeading081(expedition.LastCheckOutcome));
            RequireNamedVisibleTextEquals076(
                "Board Quest Dice Reward 081",
                "+" + BoardQuestRules081.GuildXpForOutcome081(
                    expedition.LastCheckOutcome) + " GUILD XP" +
                (string.IsNullOrWhiteSpace(expedition.LastCheckAssistantRecruitId)
                    ? string.Empty
                    : "   •   NEW BOND MEMORY"));
        }

        private static Text FindVisibleTextByNamePrefix076(string objectNamePrefix)
        {
            return UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => value != null &&
                                         value.gameObject.activeInHierarchy &&
                                         value.name.StartsWith(objectNamePrefix, StringComparison.Ordinal));
        }

        private static void ClickVisibleAction076(string label)
        {
            var button = FindVisibleAction076(label);
            Require071(button != null, "Visible action was not found: " + label);
            Require071(button.IsInteractable(), "Visible action was disabled: " + label);
            button.onClick.Invoke();
        }

        private static void RequireFocusedVisibleAction076(string label)
        {
            var button = FindVisibleAction076(label);
            Require071(button != null, "Focused action was not found: " + label);
            Require071(EventSystem.current != null,
                "No EventSystem exists while checking controller focus for " + label + ".");
            Require071(EventSystem.current.currentSelectedGameObject == button.gameObject,
                "Controller focus was not on the visible primary action: " + label + ".");
        }

        private static void RequireFocusedVisibleButtonByNamePrefix076(string prefix)
        {
            Require071(EventSystem.current != null,
                "No EventSystem exists while checking controller focus for " + prefix + ".");
            var focusedObject = EventSystem.current.currentSelectedGameObject;
            var focusedName = focusedObject == null
                ? "<none>"
                : focusedObject.name;
            var focusedButton = focusedObject == null
                ? null
                : focusedObject.GetComponent<Button>();
            Require071(focusedButton != null &&
                       focusedButton.gameObject.activeInHierarchy &&
                       focusedButton.IsInteractable() &&
                       focusedButton.name.StartsWith(prefix, StringComparison.Ordinal),
                "Controller focus was not on a visible action beginning " + prefix +
                "; focus was " + focusedName + ".");
        }

        private static void ClickVisibleButtonByName076(string name)
        {
            var button = FindActiveButtonByName081(name);
            Require071(button != null, "Visible button was not found: " + name);
            Require071(button.IsInteractable(), "Visible button was disabled: " + name);
            button.onClick.Invoke();
        }

        private static Button FindActiveButtonByName081(string name)
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => value != null &&
                                         value.gameObject.activeInHierarchy &&
                                         StringComparer.Ordinal.Equals(value.name, name));
        }

        private static void ClickFirstVisibleButtonWithPrefix078(string prefix)
        {
            var button = FindVisibleButtonWithPrefix078(prefix);
            Require071(button != null, "Visible button prefix was not found: " + prefix);
            Require071(button.IsInteractable(),
                "Visible button prefix was disabled: " + prefix);
            button.onClick.Invoke();
        }

        private static Button FindVisibleButtonWithPrefix078(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<Button>(
                    FindObjectsSortMode.InstanceID)
                .FirstOrDefault(value => value != null &&
                                         value.gameObject.activeInHierarchy &&
                                         value.name.StartsWith(
                                             prefix,
                                             StringComparison.Ordinal));
        }

        private string ClickVisibleNonPatrolTrainingChoice078()
        {
            const string prefix = "First Operation Choose Training ";
            const string suffix = " 077";
            var recruits = _coordinator?.State?.Recruits ??
                           Array.Empty<M1RecruitLoadoutView>();
            var choice = UnityEngine.Object.FindObjectsByType<Button>(
                    FindObjectsSortMode.InstanceID)
                .Where(value => value != null &&
                                value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(prefix, StringComparison.Ordinal) &&
                                value.name.EndsWith(suffix, StringComparison.Ordinal))
                .Select(value => new
                {
                    Button = value,
                    RecruitId = value.name.Substring(
                        prefix.Length,
                        value.name.Length - prefix.Length - suffix.Length)
                })
                .FirstOrDefault(value => recruits.Any(recruit =>
                    recruit != null &&
                    StringComparer.Ordinal.Equals(
                        recruit.RecruitId,
                        value.RecruitId) &&
                    !FirstHourRosterService071.PatrolStableRecruitIds.Contains(
                        recruit.PortraitAuthorityId,
                        StringComparer.Ordinal)));
            Require071(choice != null,
                "The first-operation report did not expose a non-patrol Training choice.");
            Require071(choice.Button.IsInteractable(),
                "The non-patrol Training choice was disabled.");
            choice.Button.onClick.Invoke();
            return choice.RecruitId;
        }

        private static IEnumerator WaitForInteractableButtonByName076(
            string name,
            float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                    .FirstOrDefault(value => value != null &&
                                             value.gameObject.activeInHierarchy &&
                                             StringComparer.Ordinal.Equals(value.name, name));
                if (button != null && button.IsInteractable()) yield break;
                yield return null;
            }

            Require071(false,
                "Visible button did not become interactable within " +
                timeoutSeconds.ToString("0.0") + " seconds: " + name);
        }

        private IEnumerator WaitForTowerStateAndUi084(
            Func<CampaignProgressionPresentationState022, bool> statePredicate,
            Func<bool> uiPredicate,
            float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (Time.realtimeSinceStartup < deadline)
            {
                var state = _coordinator?.CampaignProgression022;
                var stateReady = state != null &&
                                 (statePredicate == null || statePredicate(state));
                var uiReady = uiPredicate == null || uiPredicate();
                if (stateReady && uiReady) yield break;
                yield return null;
            }
        }

        private static int CountActiveNamedObjects076(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.InstanceID)
                .Count(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static int CountActiveButtonsNamed076(string prefix)
        {
            return UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.InstanceID)
                .Count(value => value != null && value.gameObject.activeInHierarchy &&
                                value.name.StartsWith(prefix, StringComparison.Ordinal));
        }

        private void VerifyInputAuthorities071()
        {
            WorldInputSource071 source;
            var keyboard = WorldInput071.SelectMovement071(
                Vector2.right, Vector2.up, Vector2.zero, out source);
            _report.keyboardInputVerified = source == WorldInputSource071.Keyboard &&
                                            keyboard == Vector2.right;
            var controller = WorldInput071.SelectMovement071(
                Vector2.zero, Vector2.up, Vector2.zero, out source);
            _report.controllerInputVerified = source == WorldInputSource071.Controller &&
                                              controller == Vector2.up;
            Require071(_report.keyboardInputVerified, "Keyboard path did not win unified input selection.");
            Require071(_report.controllerInputVerified, "Controller path did not win unified input selection.");
        }

        private IEnumerator Capture071(string label)
        {
            // Room changes use a short intentional fade. Evidence must represent
            // the stable playable view rather than a transitional dark frame.
            var settleUntil = Time.unscaledTime + 0.45f;
            while (Time.unscaledTime < settleUntil) yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            var fileName = (++_screenshotOrdinal).ToString("00") + "_" + label + ".png";
            var path = Path.Combine(_screenshotRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            var screenCaptureType = Type.GetType(
                "UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule", true);
            var captureMethod = screenCaptureType.GetMethod(
                "CaptureScreenshot", new[] { typeof(string), typeof(int) });
            Require071(captureMethod != null, "Unity ScreenCapture API is unavailable.");
            captureMethod.Invoke(null, new object[] { path, 1 });
            byte[] screenshotBytes = null;
            var screenshotDeadline071 = Time.realtimeSinceStartup + 15f;
            var screenshotRetryDelay071 = new WaitForSecondsRealtime(0.02f);
            do
            {
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    try
                    {
                        // CaptureScreenshot writes asynchronously. On Windows the PNG can
                        // exist with a non-zero length while Unity still owns an exclusive
                        // file handle, so readiness means it can be read—not merely found.
                        screenshotBytes = File.ReadAllBytes(path);
                        if (screenshotBytes.Length > 0) break;
                    }
                    catch (IOException)
                    {
                        screenshotBytes = null;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        screenshotBytes = null;
                    }
                }
                if (screenshotBytes == null)
                    yield return screenshotRetryDelay071;
            } while (screenshotBytes == null &&
                     Time.realtimeSinceStartup < screenshotDeadline071);
            Require071(screenshotBytes != null && screenshotBytes.Length > 0,
                "Screenshot was not fully written or released for validation: " + fileName);
            // CaptureScreenshot can return a non-empty solid-black frame when a
            // Windows player is fully hidden. Reject that false evidence here so
            // a logic-only PASS can never be mistaken for a reviewed build.
            ValidateRenderedScreenshot071(screenshotBytes, fileName);
            _report.screenshots.Add(path);
            WriteReport071();
        }

        private IEnumerator CaptureImmediate071(
            string label,
            Func<bool> invariant,
            string invariantFailure)
        {
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Require071(invariant == null || invariant(), invariantFailure);

            var fileName = (++_screenshotOrdinal).ToString("00") + "_" + label + ".png";
            var path = Path.Combine(_screenshotRoot, fileName);
            if (File.Exists(path)) File.Delete(path);
            Texture2D captured = null;
            byte[] screenshotBytes;
            try
            {
                var screenCaptureType = Type.GetType(
                    "UnityEngine.ScreenCapture, UnityEngine.ScreenCaptureModule", true);
                var captureMethod = screenCaptureType.GetMethod(
                    "CaptureScreenshotAsTexture",
                    Type.EmptyTypes);
                Require071(captureMethod != null,
                    "Unity same-frame ScreenCapture API is unavailable.");
                captured = captureMethod.Invoke(null, null) as Texture2D;
                Require071(captured != null,
                    "Unity could not latch the current rendered frame: " + fileName);
                screenshotBytes = captured.EncodeToPNG();
            }
            finally
            {
                if (captured != null) UnityEngine.Object.Destroy(captured);
            }

            Require071(screenshotBytes != null && screenshotBytes.Length > 0,
                "The latched screenshot was empty: " + fileName);
            File.WriteAllBytes(path, screenshotBytes);
            ValidateRenderedScreenshot071(screenshotBytes, fileName);
            _report.screenshots.Add(path);
            WriteReport071();
        }

        private void ValidateRenderedScreenshot071(byte[] bytes, string fileName)
        {
            Require071(bytes.Length > 1024, "Screenshot payload is implausibly small: " + fileName);

            var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            try
            {
                Require071(texture.LoadImage(bytes, false),
                    "Screenshot could not be decoded: " + fileName);
                Require071(texture.width >= 640 && texture.height >= 360,
                    "Screenshot resolution is below the review floor: " + fileName);
                Require071(texture.width == _report.screenWidth &&
                           texture.height == _report.screenHeight,
                    "Screenshot frame changed during certification: " + fileName +
                    " rendered " + texture.width + "x" + texture.height +
                    " instead of " + _report.certificationFrame + ".");

                var minimum = 1f;
                var maximum = 0f;
                var visiblyLitSamples = 0;
                const int horizontalSamples = 12;
                const int verticalSamples = 8;
                for (var y = 0; y < verticalSamples; y++)
                for (var x = 0; x < horizontalSamples; x++)
                {
                    var pixel = texture.GetPixelBilinear(
                        (x + 0.5f) / horizontalSamples,
                        (y + 0.5f) / verticalSamples);
                    var luminance = 0.2126f * pixel.r + 0.7152f * pixel.g + 0.0722f * pixel.b;
                    minimum = Mathf.Min(minimum, luminance);
                    maximum = Mathf.Max(maximum, luminance);
                    if (luminance >= 0.10f) visiblyLitSamples++;
                }

                Require071(visiblyLitSamples >= 6 && maximum - minimum >= 0.08f,
                    "Screenshot is blank or effectively unrendered: " + fileName);
            }
            finally
            {
                UnityEngine.Object.Destroy(texture);
            }

            string hash;
            using (var algorithm = System.Security.Cryptography.SHA256.Create())
                hash = BitConverter.ToString(algorithm.ComputeHash(bytes)).Replace("-", string.Empty);
            Require071(_screenshotHashes071.Add(hash),
                "Screenshot duplicated an earlier frame: " + fileName);
            _report.screenshotSha256.Add(hash);
            _report.renderedScreenshotValidationVerified =
                _screenshotHashes071.Count == _screenshotOrdinal &&
                _report.screenshotSha256.Count == _screenshotOrdinal;
        }

        private static bool ActiveTowerPlaceholderCopyPresent083()
        {
            return UnityEngine.Object.FindObjectsByType<Text>(
                    FindObjectsSortMode.InstanceID)
                .Any(value083 =>
                    value083 != null &&
                    value083.gameObject.activeInHierarchy &&
                    TowerRunRules081.ContainsForbiddenPlaceholderCopy083(value083.text));
        }

        private void ResolvePaths071()
        {
            var arguments = Environment.GetCommandLineArgs();
            var resolved = TryResolveIsolatedPaths078(
                arguments,
                Application.persistentDataPath,
                out _evidenceRoot,
                out _savePath,
                out var pathError);
            if (!string.IsNullOrWhiteSpace(_evidenceRoot))
            {
                _screenshotRoot = Path.Combine(_evidenceRoot, "Screenshots");
                _reportPath = Path.Combine(_evidenceRoot, "FIRST_HOUR_GOLD_SMOKE_084.json");
            }
            if (!resolved) throw new InvalidOperationException(pathError);
            _screenshotRoot = Path.Combine(_evidenceRoot, "Screenshots");
            _reportPath = Path.Combine(_evidenceRoot, "FIRST_HOUR_GOLD_SMOKE_084.json");
            var dataRoot = Path.GetFullPath(Application.dataPath);
            var playerRoot = Directory.GetParent(dataRoot)?.FullName;
            Require071(!IsSameOrChild071(_evidenceRoot, dataRoot),
                "Evidence must be outside the built Player Data folder.");
            Require071(!IsSameOrChild071(_savePath, dataRoot),
                "Smoke save must be outside the built Player Data folder.");
            Require071(string.IsNullOrWhiteSpace(playerRoot) ||
                       !IsSameOrChild071(_evidenceRoot, playerRoot),
                "Evidence must be outside the entire packaged player folder.");
            Require071(string.IsNullOrWhiteSpace(playerRoot) ||
                       !IsSameOrChild071(_savePath, playerRoot),
                "Smoke save must be outside the entire packaged player folder.");
        }

        private void WriteReport071()
        {
            if (_report == null || string.IsNullOrWhiteSpace(_reportPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_reportPath) ?? _evidenceRoot);
            File.WriteAllText(_reportPath, JsonUtility.ToJson(_report, true));
        }

        private static string ReadValue071(IReadOnlyList<string> arguments, string prefix)
        {
            string result = null;
            foreach (var argument in arguments ?? Array.Empty<string>())
                if (argument != null && argument.StartsWith(prefix, StringComparison.Ordinal))
                    result = argument.Substring(prefix.Length);
            return result;
        }

        private static bool IsSameOrChild071(string candidate, string parent)
        {
            var normalizedCandidate = Path.GetFullPath(candidate)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedParent = Path.GetFullPath(parent)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return StringComparer.OrdinalIgnoreCase.Equals(normalizedCandidate, normalizedParent) ||
                   normalizedCandidate.StartsWith(
                       normalizedParent + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool PathsOverlap071(string left, string right) =>
            IsSameOrChild071(left, right) || IsSameOrChild071(right, left);

        private static void Require071(M1CommandResult result, string operation)
        {
            if (result == null || !result.Succeeded)
                throw new InvalidOperationException(
                    operation + " failed: " + (result?.Message ?? "no result"));
        }

        private static void Require071(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
