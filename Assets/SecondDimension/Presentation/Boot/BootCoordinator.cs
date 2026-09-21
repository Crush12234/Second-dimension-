using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SecondDimension.Content;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using SecondDimension.Presentation.FirstHour071;
using SecondDimension.Presentation.Release025;
using SecondDimension.Presentation.Release029;
using SecondDimension.Presentation.Release030;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Boot
{
    public sealed class BootCoordinator : MonoBehaviour
    {
        private const long ProofSeed = 20260811;
        private const string FirstHourSaveFileName071 = "second_dimension_first_hour_slice_071.json";
        private const string LoadingBackdropResource071 =
            "SecondDimension/Art/FirstHour071/Environments/SKYHOME_MARKET_GAMEPLAY_PLATE_071";
        private Canvas _bootCanvas;
        private Image _loadingBackdropArtwork;
        private Sprite _loadingBackdropSprite;
        private Text _loadingStatus;
        private Text _loadingDetail;
        private Image _loadingProgressFill;
        private float _loadingStartedAt;
        private int _lastHeartbeatSecond = -1;
        private string _manualReviewFailure097;

        private void Awake()
        {
            if (HeroArtContactSheet118.IsRequested118(Environment.GetCommandLineArgs()))
            {
                HeroArtContactSheet118.Begin118(gameObject, Environment.GetCommandLineArgs(),
                    Application.persistentDataPath, Application.dataPath);
                return;
            }
            EnsureDisplayCamera();
            string coordinatorSavePath078;
            try
            {
                coordinatorSavePath078 = ResolveCoordinatorSavePathForVerification078(
                    Environment.GetCommandLineArgs(),
                    Application.persistentDataPath,
                    Application.temporaryCachePath,
                    Application.dataPath);
            }
            catch (Exception exception)
            {
                _manualReviewFailure097 = exception.Message;
                // Never leave a previous/default factory available on failed QA boot.
                M1PresentationCoordinatorRegistry.Register(() =>
                    throw new InvalidOperationException("Earned save review blocked: " + _manualReviewFailure097));
                Debug.LogError("[Manual earned review] " + _manualReviewFailure097);
                return;
            }
            if (ManualEarnedSaveReview097.IsRequested097(Environment.GetCommandLineArgs()))
                Debug.Log("[Manual earned review] ISOLATED COPY: " + coordinatorSavePath078 +
                    " | Normal manual gameplay; source untouched. SHA-256 receipt is beside the review save.");
            AlphaProfile132.SetSessionSave132(coordinatorSavePath078);
            M1PresentationCoordinatorRegistry.Register(() => new M1RuntimeCoordinator(
                null,
                coordinatorSavePath078));
        }

        public static string ResolveCoordinatorSavePathForVerification078(
            IReadOnlyList<string> arguments,
            string persistentDataPath,
            string temporaryCachePath,
            string buildDataPath097 = null)
        {
            if (CampaignAlphaPlayerLoop132.IsRequested132(arguments))
                return CampaignAlphaPlayerLoop132.Prepare132(arguments, persistentDataPath,
                    buildDataPath097 ?? Application.dataPath);
            if (TowerAutoPlayerSoak110.IsRequested110(arguments))
                return TowerAutoPlayerSoak110.Prepare110(arguments, persistentDataPath,
                    buildDataPath097 ?? Application.dataPath);
            if (ManualEarnedSaveReview097.IsRequested097(arguments))
                return ManualEarnedSaveReview097.Prepare097(arguments, persistentDataPath,
                    buildDataPath097 ?? Application.dataPath);
            var personalSavePath = Path.GetFullPath(Path.Combine(
                persistentDataPath,
                FirstHourSaveFileName071));
            // Roster evidence must be isolated before the shipping presenter or
            // coordinator is instantiated, including malformed audit requests.
            if (HeroRosterBuiltPlayerAudit093.IsRequested093(arguments))
                return HeroRosterBuiltPlayerAudit093.ResolveBootSavePath093(
                    arguments, persistentDataPath, temporaryCachePath);
            if (!FirstHourGoldSmoke071.IsSmokeRequested078(arguments))
                return Path.GetFullPath(Path.Combine(
                    AlphaProfile132.ResolveDirectory132(persistentDataPath, buildDataPath097),
                    FirstHourSaveFileName071));

            if (FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                    arguments,
                    persistentDataPath,
                    out _,
                    out var isolatedSavePath,
                    out _))
            {
                return isolatedSavePath;
            }

            // An invalid smoke command must still never instantiate a coordinator
            // against the player's personal Guild. The smoke itself will report the
            // path error and quit; this quarantine path only keeps boot fail-closed
            // while the presenter is being created.
            var quarantineRoot = string.IsNullOrWhiteSpace(temporaryCachePath)
                ? Path.GetTempPath()
                : temporaryCachePath;
            return Path.GetFullPath(Path.Combine(
                quarantineRoot,
                "SECOND_DIMENSION_FIRST_HOUR_SMOKE_QUARANTINE",
                "invalid_smoke_save.json"));
        }

        private void Start()
        {
            if (HeroArtContactSheet118.IsRequested118(Environment.GetCommandLineArgs())) return;
            RuntimeUi.EnsureEventSystem();
            if (_manualReviewFailure097 != null)
            {
                BuildProofUi(BootProofResult.Fail("COULD NOT OPEN SAVE", _manualReviewFailure097));
                return;
            }
            BuildLoadingUi();
            StartCoroutine(LoadLoadingBackdrop071());
            StartCoroutine(EnterPlayableFirstHour071());
        }

        private IEnumerator EnterPlayableFirstHour071()
        {
            // The exhaustive frozen-content/release proof remains available through
            // ValidateAndEnter, but it is not a player-blocking startup gate. Runtime
            // coordinators load the authority needed by the playable slice themselves.
            yield return null;
            SetLoadingStage(
                1,
                "OPENING SKYHOME",
                "Preparing your Guild and adventures.",
                0.65f);
            yield return null;
            SetLoadingStage(5, "GUILD RECORD READY", "Opening the Guild Hall.", 1f);
            yield return null;
            EnterM1();
        }

        private IEnumerator ValidateAndEnter()
        {
            // Render the branded presentation before validation begins. The frozen
            // authority scan is pure file/JSON work, so keep it off Unity's main
            // thread. Unity APIs and Resources checks remain on the main thread.
            yield return null;

            _loadingStartedAt = Time.realtimeSinceStartup;
            string authorityRoot = null;
            Exception authorityException = null;
            try
            {
                var repositoryRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                authorityRoot = Directory.Exists(Path.Combine(repositoryRoot, "IMPLEMENTATION_FREEZE"))
                    ? repositoryRoot
                    : Path.Combine(Application.streamingAssetsPath, "Authority");
            }
            catch (Exception exception)
            {
                authorityException = exception;
            }
            if (authorityException != null)
            {
                BuildProofUi(BootProofResult.Fail("M0 BOOT FAILED", authorityException.ToString()));
                yield break;
            }

            SetLoadingStage(
                1,
                "VERIFYING THE GUILD RECORD",
                "Checking 5,092 protected content links.",
                0.08f);
            var contentStartedAt = Time.realtimeSinceStartup;
            Task<ContentRegistry> contentTask = null;
            Exception taskStartException = null;
            try
            {
                contentTask = Task.Run(() => ContentRegistry.LoadAndValidate(authorityRoot));
            }
            catch (Exception exception)
            {
                taskStartException = exception;
            }
            if (taskStartException != null)
            {
                BuildProofUi(BootProofResult.Fail("M0 BOOT FAILED", taskStartException.ToString()));
                yield break;
            }

            while (!contentTask.IsCompleted)
            {
                UpdateContentHeartbeat();
                yield return null;
            }

            if (contentTask.IsCanceled || contentTask.IsFaulted)
            {
                var exception = contentTask.Exception?.GetBaseException();
                BuildProofUi(BootProofResult.Fail(
                    "M0 BOOT FAILED",
                    exception?.ToString() ?? "Guild-record validation was canceled."));
                yield break;
            }

            var content = contentTask.Result;
            Debug.Log(
                "[Boot] Frozen content validation completed in " +
                (Time.realtimeSinceStartup - contentStartedAt).ToString("0.0") + " seconds.");

            SetLoadingStage(2, "CHECKING SAVE COMPATIBILITY", "Preparing deterministic campaign state.", 0.62f);
            yield return null;
            SaveEnvelopeV1 envelope = null;
            Exception saveException = null;
            try
            {
                var campaign = CampaignFactory.CreateM0Proof(ProofSeed);
                envelope = SaveEnvelopeV1.Create(campaign, DateTime.UtcNow);
            }
            catch (Exception exception)
            {
                saveException = exception;
            }
            if (saveException != null)
            {
                BuildProofUi(BootProofResult.Fail("M0 BOOT FAILED", saveException.ToString()));
                yield break;
            }

            SetLoadingStage(3, "CHECKING GAME SYSTEMS", "Confirming the active Unity build and world data.", 0.72f);
            yield return null;
            var stageStartedAt = Time.realtimeSinceStartup;
            var readiness = ImplementationReadinessService025.BuildSnapshot();
            LogStageDuration("Game-system readiness", stageStartedAt);

            SetLoadingStage(4, "CHECKING PEOPLE AND CREATOR DATA", "Confirming recruits, stories, bonds, and Creator Codes.", 0.84f);
            yield return null;
            stageStartedAt = Time.realtimeSinceStartup;
            var peopleCreator = PeopleCreatorReadinessService029.BuildSnapshot();
            LogStageDuration("People/Creator readiness", stageStartedAt);

            SetLoadingStage(5, "FINALIZING RELEASE 030", "Confirming save and release compatibility.", 0.94f);
            yield return null;
            stageStartedAt = Time.realtimeSinceStartup;
            var supersession = ReleaseSupersessionReadinessService030.BuildSnapshot();
            LogStageDuration("Release supersession readiness", stageStartedAt);

            var proof = BuildM0ProofResult(content, envelope, readiness, peopleCreator, supersession);
            if (proof.Succeeded)
            {
                SetLoadingStage(5, "GUILD RECORD READY", "Opening the Guild Hall.", 1f);
                Debug.Log(
                    "[Boot] Complete startup proof passed in " +
                    (Time.realtimeSinceStartup - _loadingStartedAt).ToString("0.0") + " seconds.");
                yield return null;
                EnterM1();
                yield break;
            }

            BuildProofUi(proof);
        }

        private void BuildLoadingUi()
        {
            _bootCanvas = RuntimeUi.CreateCanvas("Second Dimension Loading Canvas");
            var background = RuntimeUi.AddPanel(_bootCanvas.transform, "Loading Background", RuntimeUi.Background);
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            _loadingBackdropArtwork = RuntimeUi.AddPanel(
                background.transform,
                "Loading Skyhome Market Artwork 071",
                Color.clear);
            Stretch(_loadingBackdropArtwork.rectTransform);
            _loadingBackdropArtwork.raycastTarget = false;

            var scrim = RuntimeUi.AddPanel(
                background.transform,
                "Loading Readability Scrim 063",
                new Color(0.005f, 0.012f, 0.025f, 0.62f));
            Stretch(scrim.rectTransform);
            scrim.raycastTarget = false;

            var safeArea = RuntimeUi.AddSafeArea(background.transform);
            var title = RuntimeUi.AddText(
                safeArea,
                "Loading Title",
                "SECOND DIMENSION",
                RuntimeUi.HeadingFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0.12f, 0.48f);
            title.rectTransform.anchorMax = new Vector2(0.88f, 0.64f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;
            _loadingStatus = RuntimeUi.AddText(
                safeArea,
                "Loading Status",
                "STARTING THE GUILD RECORD",
                RuntimeUi.BodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            _loadingStatus.rectTransform.anchorMin = new Vector2(0.12f, 0.39f);
            _loadingStatus.rectTransform.anchorMax = new Vector2(0.88f, 0.49f);
            _loadingStatus.rectTransform.offsetMin = Vector2.zero;
            _loadingStatus.rectTransform.offsetMax = Vector2.zero;

            _loadingDetail = RuntimeUi.AddText(
                safeArea,
                "Loading Detail",
                "STEP 1 OF 5",
                RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText);
            _loadingDetail.rectTransform.anchorMin = new Vector2(0.12f, 0.30f);
            _loadingDetail.rectTransform.anchorMax = new Vector2(0.88f, 0.39f);
            _loadingDetail.rectTransform.offsetMin = Vector2.zero;
            _loadingDetail.rectTransform.offsetMax = Vector2.zero;

            var progressTrack = RuntimeUi.AddPanel(
                safeArea,
                "Loading Progress Track",
                new Color(1f, 1f, 1f, 0.12f));
            progressTrack.rectTransform.anchorMin = new Vector2(0.22f, 0.265f);
            progressTrack.rectTransform.anchorMax = new Vector2(0.78f, 0.282f);
            progressTrack.rectTransform.offsetMin = Vector2.zero;
            progressTrack.rectTransform.offsetMax = Vector2.zero;

            _loadingProgressFill = RuntimeUi.AddPanel(
                progressTrack.transform,
                "Loading Progress Fill",
                RuntimeUi.Accent);
            _loadingProgressFill.rectTransform.anchorMin = Vector2.zero;
            _loadingProgressFill.rectTransform.anchorMax = new Vector2(0.04f, 1f);
            _loadingProgressFill.rectTransform.offsetMin = Vector2.zero;
            _loadingProgressFill.rectTransform.offsetMax = Vector2.zero;
        }

        private IEnumerator LoadLoadingBackdrop071()
        {
            ResourceRequest request = null;
            try
            {
                // Resources remains a Unity-main-thread operation. LoadAsync keeps
                // the first rendered frame responsive while the optional art enters.
                request = Resources.LoadAsync<Texture2D>(LoadingBackdropResource071);
            }
            catch (Exception)
            {
                request = null;
            }
            if (request == null) yield break;

            while (!request.isDone) yield return null;
            if (_bootCanvas == null || _loadingBackdropArtwork == null) yield break;

            var texture = request.asset as Texture2D;
            if (texture == null) yield break;
            try
            {
                _loadingBackdropSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0u,
                    SpriteMeshType.FullRect);
                _loadingBackdropSprite.name = texture.name + "_BOOT_RUNTIME_SPRITE_071";
                _loadingBackdropArtwork.sprite = _loadingBackdropSprite;
                _loadingBackdropArtwork.type = Image.Type.Simple;
                _loadingBackdropArtwork.preserveAspect = false;
                _loadingBackdropArtwork.color = Color.white;
            }
            catch (Exception)
            {
                _loadingBackdropSprite = null;
                _loadingBackdropArtwork.sprite = null;
                _loadingBackdropArtwork.color = Color.clear;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetLoadingStage(int step, string status, string detail, float progress)
        {
            if (_loadingStatus != null)
                _loadingStatus.text = status ?? string.Empty;
            if (_loadingDetail != null)
                _loadingDetail.text =
                    "STEP " + step + " OF 5  •  " + (detail ?? string.Empty);
            SetLoadingProgress(progress);
        }

        private void UpdateContentHeartbeat()
        {
            var elapsed = Mathf.Max(0f, Time.realtimeSinceStartup - _loadingStartedAt);
            var elapsedSecond = Mathf.FloorToInt(elapsed);
            if (elapsedSecond != _lastHeartbeatSecond)
            {
                _lastHeartbeatSecond = elapsedSecond;
                if (_loadingDetail != null)
                {
                    var dots = new string('.', 1 + elapsedSecond % 3);
                    _loadingDetail.text =
                        "STEP 1 OF 5  •  CHECKING 5,092 CONTENT LINKS" + dots +
                        "  •  " + elapsedSecond + "s ELAPSED";
                }
            }

            // This is intentionally an estimated, monotonic stage bar. The exact
            // amount of disk work differs by machine, but the heartbeat remains
            // truthful and proves that the player has not frozen.
            var stageProgress = 0.08f + 0.50f * (1f - Mathf.Exp(-elapsed / 36f));
            SetLoadingProgress(stageProgress);
        }

        private void SetLoadingProgress(float progress)
        {
            if (_loadingProgressFill == null) return;
            var rect = _loadingProgressFill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void LogStageDuration(string label, float startedAt)
        {
            Debug.Log(
                "[Boot] " + label + " completed in " +
                (Time.realtimeSinceStartup - startedAt).ToString("0.0") + " seconds.");
        }

        private static BootProofResult BuildM0ProofResult(
            ContentRegistry content,
            SaveEnvelopeV1 envelope,
            ImplementationReadinessSnapshot025 readiness,
            PeopleCreatorHealthSnapshot029 peopleCreator,
            ReleaseSupersessionSnapshot030 supersession)
        {
            try
            {
                if (!content.Validation.IsValid)
                    return BootProofResult.Fail(
                        "M0 CONTENT VALIDATION FAILED",
                        string.Join("\n", content.Validation.Errors));
                if (!readiness.IsReady)
                    return BootProofResult.Fail(
                        "FULL GAME INTEGRATION VALIDATION FAILED",
                        readiness.Error);
                if (!peopleCreator.IsReady)
                    return BootProofResult.Fail(
                        "PEOPLE / CREATOR INTEGRATION VALIDATION FAILED",
                        peopleCreator.Error);
                if (!supersession.IsReady)
                    return BootProofResult.Fail(
                        "RELEASE SUPERSESSION VALIDATION FAILED",
                        supersession.Error);

                return BootProofResult.Pass(
                    content.Validation + "\n" + string.Join("\n", readiness.SummaryLines) + "\n" + string.Join("\n", peopleCreator.SummaryLines) + "\n" + string.Join("\n", supersession.SummaryLines),
                    $"Save v{envelope.SaveFormatVersion} ready\nState hash: {envelope.CanonicalStateHash}");
            }
            catch (Exception exception)
            {
                return BootProofResult.Fail("M0 BOOT FAILED", exception.ToString());
            }
        }

        private void BuildProofUi(BootProofResult proof)
        {
            if (_bootCanvas == null) _bootCanvas = RuntimeUi.CreateCanvas("Second Dimension Loading Canvas");
            else RuntimeUi.ClearChildren(_bootCanvas.transform);
            var background = RuntimeUi.AddPanel(_bootCanvas.transform, "Background", RuntimeUi.Background);
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var safeArea = RuntimeUi.AddSafeArea(background.transform);
            var panel = RuntimeUi.AddPanel(safeArea, "Foundation Proof", RuntimeUi.Panel);
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = new Vector2(0.09f, 0.08f);
            panelRect.anchorMax = new Vector2(0.91f, 0.92f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(120, 120, 80, 80), 26f, TextAnchor.MiddleCenter);

            var title = RuntimeUi.AddText(
                panel.transform,
                "Title",
                "STARTUP CHECK",
                RuntimeUi.HeadingFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(title, preferredHeight: 120f);

            var result = RuntimeUi.AddText(
                panel.transform,
                "Result",
                proof.Succeeded ? "READY" : "STARTUP BLOCKED",
                RuntimeUi.CriticalFontPixels,
                TextAnchor.MiddleCenter,
                proof.Succeeded ? RuntimeUi.Positive : RuntimeUi.Error,
                FontStyle.Bold);
            RuntimeUi.SetLayout(result, preferredHeight: 100f);

            var summary = RuntimeUi.AddText(
                panel.transform,
                "Validation Summary",
                proof.Summary,
                RuntimeUi.BodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text);
            RuntimeUi.SetLayout(summary, preferredHeight: 150f);

            var details = RuntimeUi.AddText(
                panel.transform,
                "Validation Details",
                proof.Details,
                RuntimeUi.SmallBodyFontPixels,
                TextAnchor.MiddleCenter,
                RuntimeUi.MutedText);
            RuntimeUi.SetLayout(details, preferredHeight: 230f, flexibleHeight: 1f);

            if (!proof.Succeeded)
            {
                RuntimeUi.AddText(
                    panel.transform,
                    "Failure Direction",
                    "The game did not open because required content failed validation. Preserve the last good save and correct the error above.",
                    RuntimeUi.SmallBodyFontPixels,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Warning);
            }
        }

        private void EnterM1()
        {
            if (_bootCanvas != null)
            {
                _bootCanvas.gameObject.SetActive(false);
                Destroy(_bootCanvas.gameObject);
                _bootCanvas = null;
            }
            if (_loadingBackdropSprite != null)
            {
                Destroy(_loadingBackdropSprite);
                _loadingBackdropSprite = null;
            }

            var presenterObject = new GameObject("M1 Flow Presenter");
            presenterObject.AddComponent<M1FlowPresenter>();
        }

        private static void EnsureDisplayCamera()
        {
            var camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            var createdForBoot = camera == null;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                camera = cameraObject.GetComponent<Camera>();
            }

            if (camera.GetComponent<AudioListener>() == null &&
                UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();

            camera.enabled = true;
            camera.targetDisplay = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = RuntimeUi.Background;
            if (createdForBoot)
            {
                camera.orthographic = true;
                camera.cullingMask = 0;
            }
        }

        private readonly struct BootProofResult
        {
            private BootProofResult(bool succeeded, string summary, string details)
            {
                Succeeded = succeeded;
                Summary = summary ?? string.Empty;
                Details = details ?? string.Empty;
            }

            public bool Succeeded { get; }
            public string Summary { get; }
            public string Details { get; }

            public static BootProofResult Pass(string summary, string details) =>
                new BootProofResult(true, summary, details);

            public static BootProofResult Fail(string summary, string details) =>
                new BootProofResult(false, summary, details);
        }
    }
}
