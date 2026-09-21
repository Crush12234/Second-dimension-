using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace SecondDimension.Presentation.Boot
{
    // Commands are issued only by real shipping UI buttons and controller Auto.
    // Reflection below reads the existing immutable campaign and verifies its
    // save path; it never sets fields, floors, rewards, gear, orders or RNG.
    public sealed class TowerAutoPlayerSoak110 : MonoBehaviour
    {
        const string Prefix = "--sd-tower-soak110-";
        public sealed class Settings110
        {
            public string SourcePath, OutputDirectory, SavePath;
            public int Seconds = 1800, ThroughFloor, Speed = 16;
        }
        public static Settings110 Prepared110 { get; private set; }

        // Opt-in QA input readiness only. It never changes geometry, controls,
        // campaign state, or command selection to make a blocked click succeed.
        public sealed class ButtonReadiness122
        {
            Button _candidate;
            Vector4 _geometry;
            int _firstVisibleFrame;
            bool _sawVisibleHit;
            public string LastDiagnostic { get; private set; } = "No allowed navigation control has appeared.";

            public void Reset()
            {
                _candidate = null;
                _sawVisibleHit = false;
            }

            public void CheckDeadline(double elapsedSeconds, double deadlineSeconds)
            {
                if (elapsedSeconds >= deadlineSeconds)
                    throw new InvalidOperationException("Actual UI could not resume a live Tower battle within 180 seconds. Last readiness: " + LastDiagnostic);
            }

            public bool Observe(Button button, int renderedFrame)
            {
                if (!TryHit(button, out _, out var geometry, out var diagnostic))
                {
                    Reset();
                    LastDiagnostic = diagnostic;
                    return false;
                }
                if (_candidate != button || !_sawVisibleHit ||
                    Mathf.Abs(_geometry.x - geometry.x) > 0.25f || Mathf.Abs(_geometry.y - geometry.y) > 0.25f ||
                    Mathf.Abs(_geometry.z - geometry.z) > 0.25f || Mathf.Abs(_geometry.w - geometry.w) > 0.25f)
                {
                    _candidate = button;
                    _geometry = geometry;
                    _firstVisibleFrame = renderedFrame;
                    _sawVisibleHit = true;
                    LastDiagnostic = "Waiting for a rendered frame with unchanged visible geometry. " + diagnostic;
                    return false;
                }
                LastDiagnostic = diagnostic;
                return renderedFrame > _firstVisibleFrame;
            }

            public static bool TryHit(Button button, out PointerEventData pointer,
                out Vector4 geometry, out string diagnostic)
            {
                pointer = null;
                geometry = Vector4.zero;
                if (button == null || !button.isActiveAndEnabled || !button.gameObject.activeInHierarchy || !button.IsInteractable())
                {
                    diagnostic = "No active, interactable navigation control is ready.";
                    return false;
                }
                var canvas = button.GetComponentInParent<Canvas>();
                var rect = button.GetComponent<RectTransform>();
                if (canvas == null || rect == null || EventSystem.current == null)
                {
                    diagnostic = "Control lacks a canvas, rectangle, or EventSystem: " + button.name;
                    return false;
                }
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                var min = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.min));
                var max = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.max));
                geometry = new Vector4(center.x, center.y, Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
                pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left, position = center };
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, hits);
                var hit = hits.Count == 0 ? null : hits[0].gameObject;
                diagnostic = "target=" + HierarchyPath(button.transform) + " center=" + center.ToString("F1") +
                    " size=" + new Vector2(geometry.z, geometry.w).ToString("F1") +
                    " screen=" + Screen.width + "x" + Screen.height + " topHit=" +
                    (hit == null ? "NONE" : HierarchyPath(hit.transform)) + " canvas=" + canvas.name +
                    " sortingOrder=" + canvas.sortingOrder;
                return center.x >= 0f && center.y >= 0f && center.x <= Screen.width && center.y <= Screen.height &&
                    geometry.z > 0f && geometry.w > 0f && hit != null &&
                    ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == button.gameObject;
            }

            static string HierarchyPath(Transform current)
            {
                var names = new List<string>();
                for (; current != null; current = current.parent) names.Add(current.name);
                names.Reverse();
                return string.Join("/", names);
            }
        }


        public static bool IsRequested110(IReadOnlyList<string> arguments) =>
            arguments != null && arguments.Any(value => value != null &&
                value.StartsWith("--sd-tower-soak110", StringComparison.OrdinalIgnoreCase));

        public static Settings110 Parse110(IReadOnlyList<string> arguments)
        {
            if (!IsRequested110(arguments)) throw new InvalidOperationException("Explicit Tower soak flags are required.");
            var settings = new Settings110();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var argument in arguments)
            {
                if (argument == null || !argument.StartsWith("--sd-", StringComparison.OrdinalIgnoreCase)) continue;
                var separator = argument.IndexOf('=');
                if (!argument.StartsWith(Prefix, StringComparison.Ordinal) || separator < 0)
                    throw new InvalidOperationException("Tower soak cannot combine malformed or other QA flags.");
                var name = argument.Substring(Prefix.Length, separator - Prefix.Length);
                var value = argument.Substring(separator + 1);
                if (!seen.Add(name)) throw new InvalidOperationException("Duplicate Tower soak argument: " + name);
                switch (name)
                {
                    case "source": settings.SourcePath = value; break;
                    case "output": settings.OutputDirectory = value; break;
                    case "seconds": settings.Seconds = ParseInt110(value); break;
                    case "through-floor": settings.ThroughFloor = ParseInt110(value); break;
                    case "speed": settings.Speed = ParseInt110(value); break;
                    default: throw new InvalidOperationException("Unknown Tower soak argument: " + name);
                }
            }
            if (settings.Seconds < 60 || settings.Seconds > 1800 || settings.ThroughFloor < 1 ||
                !new[] { 1, 2, 4, 16 }.Contains(settings.Speed))
                throw new InvalidOperationException("Use 60–1800 seconds, an explicit positive final completed floor, and speed 1/2/4/16.");
            settings.SourcePath = ManualEarnedSaveReview097.LocalAbsolutePath097(settings.SourcePath);
            settings.OutputDirectory = ManualEarnedSaveReview097.LocalAbsolutePath097(settings.OutputDirectory);
            return settings;
        }

        public static string Prepare110(IReadOnlyList<string> arguments, string personalPath, string buildDataPath)
        {
            Prepared110 = null;
            var settings = Parse110(arguments);
            // Reuse the unchanged, tested path-boundary and byte-for-byte copy
            // authority. Its manual receipt describes only this copy operation;
            // the separate launch receipt explicitly declares automatic gameplay.
            settings.SavePath = ManualEarnedSaveReview097.Prepare097(new[] {
                "TowerAutoPlayerSoak110",
                ManualEarnedSaveReview097.SourceFlag097 + settings.SourcePath,
                ManualEarnedSaveReview097.OutputFlag097 + settings.OutputDirectory
            }, personalPath, buildDataPath);
            File.WriteAllText(Path.Combine(settings.OutputDirectory, "tower_soak110_launch.json"),
                JsonConvert.SerializeObject(new {
                    Mode = "PACKAGED_PLAYER_RENDERED_TOWER_AUTO_110", AutomaticGameplay = true,
                    Driver = "Shipping UI buttons and M2BattleExperienceController072 Auto/animation",
                    CopyHelperReceipt = "manual_review097_receipt.json",
                    PlayerPrefsWrites = false, CampaignStateEditedByLauncher = false,
                    settings.SourcePath, settings.SavePath, settings.Seconds, settings.ThroughFloor, settings.Speed
                }, Formatting.Indented));
            Prepared110 = settings;
            return settings.SavePath;
        }

        static int ParseInt110(string value)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
                throw new InvalidOperationException("Tower soak numeric arguments must be unsigned integers.");
            return number;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void CreateWhenRequested110()
        {
            if (Application.isEditor || !IsRequested110(Environment.GetCommandLineArgs())) return;
            var owner = new GameObject("Opt-in Rendered Tower Auto Soak 110");
            DontDestroyOnLoad(owner);
            owner.AddComponent<TowerAutoPlayerSoak110>();
        }

        Settings110 _settings;
        M1FlowPresenter _presenter;
        M1RuntimeCoordinator _coordinator;
        M2BattleExperienceController072 _controller;
        StreamWriter _log;
        readonly Stopwatch _wall = new Stopwatch();
        readonly Stopwatch _soak = new Stopwatch();
        readonly List<string> _errors = new List<string>();
        readonly HashSet<string> _seenRewards = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _seenPendingRewards = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> _initialItems, _initialRecruits;
        string _sourceHash, _stopReason = "not_started", _failure;
        int _startFloor, _lastCompletedFloor, _frames, _animatedFrames, _peakObjects;
        int _peakHandles, _peakActors, _peakExactBeats;
        long _peakWorkingSet, _peakManagedBytes;
        float _maxFrameSeconds;
        double _nextSample, _nextObjectSample;
        readonly List<float> _frameSample = new List<float>(4096);
        readonly List<object> _pendingEvents = new List<object>();
        bool _watching, _isolationVerified;
        bool _rewardsAndOwnershipVerified110, _durableReloadVerified110, _towerSequenceVerified110;
        int _savedHighestCompletedFloor110, _savedLastBankedFloor130;
        static readonly FieldInfo CoordinatorField = typeof(M1FlowPresenter).GetField("_coordinator", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo CampaignField = typeof(M1RuntimeCoordinator).GetField("_campaign", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo SavePathField = typeof(M1RuntimeCoordinator).GetField("_savePath", BindingFlags.Instance | BindingFlags.NonPublic);
        CampaignState Campaign110() => (CampaignState)CampaignField.GetValue(_coordinator);

        IEnumerator Start()
        {
            _wall.Start();
            Application.runInBackground = true;
            yield return Guard110(Run110());
            _controller?.SetAutoOrders091(false);
            _soak.Stop();
            if (_isolationVerified) yield return Guard110(FinishChecks110());
            FinishReport110();
        }

        IEnumerator Guard110(IEnumerator routine)
        {
            while (true)
            {
                object step = null;
                var advanced = false;
                try { advanced = routine.MoveNext(); if (advanced) step = routine.Current; }
                catch (Exception exception) { _failure = (_failure ?? string.Empty) + exception + "\n"; advanced = false; }
                if (!advanced) yield break;
                yield return step;
            }
        }

        IEnumerator Run110()
        {
            // Boot.Awake must finish fail-closed path preparation before Start.
            _settings = Prepared110 ?? throw new InvalidOperationException("Tower soak boot isolation was not prepared.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null || Application.isBatchMode)
                throw new InvalidOperationException("This evidence requires a rendered packaged player; do not pass -batchmode or -nographics.");
            _log = new StreamWriter(Path.Combine(_settings.OutputDirectory, "tower_soak110_events.jsonl"), false);
            _sourceHash = HashFile110(_settings.SourcePath);
            Application.logMessageReceived += OnRuntimeLog110;
            while (_wall.Elapsed.TotalSeconds < 180)
            {
                _presenter = Object.FindObjectsByType<M1FlowPresenter>(FindObjectsSortMode.None).SingleOrDefault();
                _coordinator = _presenter == null ? null : CoordinatorField.GetValue(_presenter) as M1RuntimeCoordinator;
                if (_coordinator != null) break;
                yield return new WaitForSecondsRealtime(0.25f);
            }
            Require110(_coordinator != null, "Shipping presenter/coordinator did not become ready in 180 seconds.");
            Require110(string.Equals(Path.GetFullPath((string)SavePathField.GetValue(_coordinator)),
                Path.GetFullPath(_settings.SavePath), StringComparison.OrdinalIgnoreCase), "Refusing input: coordinator is not using the isolated save.");
            _isolationVerified = true;
            var tower = _coordinator.CampaignProgression022;
            Require110(!string.IsNullOrWhiteSpace(tower.ActiveAbyssOperationId), "The source must have an actual active Tower floor.");
            _startFloor = tower.TowerFloorNumber;
            Require110(_settings.ThroughFloor >= _startFloor && _settings.ThroughFloor <= _startFloor + 100,
                "Final completed floor must be within the next 100 earned floors from the source.");
            var initial = Campaign110();
            _seenRewards.UnionWith(initial.Guild.Development.ClaimedBattleRewardIds);
            _initialItems = OwnedItems110(initial);
            _initialRecruits = new HashSet<string>(initial.Guild.Recruits.Select(value => value.RecruitId), StringComparer.Ordinal);
            _coordinator.Changed += ObserveCommit110;
            ObserveCommit110();
            Log110(new { Event = "ready", Utc = DateTime.UtcNow, _startFloor, _settings.ThroughFloor,
                GraphicsDevice = SystemInfo.graphicsDeviceType.ToString(), Application.version, Screen.width, Screen.height });

            var navigationDeadline = _wall.Elapsed.TotalSeconds + 180;
            var readiness122 = new ButtonReadiness122();
            string reportedWait122 = null;
            var allowed = new[] { "Title Primary Play Now 062", "Living Guild Hub Primary CTA 074",
                "Continue From Battle Results 072", "Bank Tower battle victory 088", "Climb next Tower floor 084",
                "Prepare Tower battle only 088", "Enter Tower battle 081", "Enter prepared Tower battle 081",
                "Resume Tower battle 081", "Open Tower battle results 084" };
            while (true)
            {
                readiness122.CheckDeadline(_wall.Elapsed.TotalSeconds, navigationDeadline);
                Require110(_errors.Count == 0, "Runtime error while opening the actual Tower UI.");
                _controller = _presenter.GetComponent<M2BattleExperienceController072>();
                if (_controller != null && _controller.IsActive && Campaign110().Battle?.Outcome == BattleOutcome.InProgress) break;
                var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
                var button = allowed.Select(name => buttons.FirstOrDefault(value => value.name == name && value.IsInteractable()))
                    .FirstOrDefault(value => value != null);
                if (readiness122.Observe(button, Time.renderedFrameCount))
                {
                    Click110(button);
                    readiness122.Reset();
                    reportedWait122 = null;
                }
                else if (button != null && !StringComparer.Ordinal.Equals(reportedWait122, readiness122.LastDiagnostic))
                {
                    reportedWait122 = readiness122.LastDiagnostic;
                    Log110(new { Event = "ui_navigation_wait122", Button = button.name,
                        Seconds = _wall.Elapsed.TotalSeconds, RenderedFrame = Time.renderedFrameCount,
                        Diagnostic = reportedWait122 });
                }
                yield return new WaitForSecondsRealtime(0.25f);
            }
            // Let the newly active battle HUD receive its normal layout/render pass.
            yield return null;
            var coach = Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .FirstOrDefault(value => value.name == "Dismiss First Battle Union Coach 076" && value.IsInteractable());
            if (coach != null) { Click110(coach); yield return null; }
            // Public speed cycling has the same callback as its visible button.
            // Click the actual control so logs also prove the visible input path.
            var speedButton = FindButton110("Battle Playback Speed 091");
            for (var index = 0; _controller.AnimationSpeed != _settings.Speed && index < 4; index++)
            {
                Click110(speedButton);
                yield return null;
            }
            Require110(_controller.AnimationSpeed == _settings.Speed, "Visible speed control did not select the requested speed.");
            Require110(!_controller.ReducedMotion, "Rendered animation evidence requires reduced motion to be off in the supplied save.");
            Click110(FindButton110("Battle Auto Orders 091"));
            Require110(_controller.AutoOrdersEnabled091, "Visible Auto button did not enable controller-owned Auto.");
            _soak.Start();
            _watching = true;
            while (_soak.Elapsed.TotalSeconds < _settings.Seconds &&
                   _controller.LastCompletedTowerFloor108 < _settings.ThroughFloor)
            {
                Require110(_errors.Count == 0, "Runtime error during rendered Auto; see events and player log.");
                Require110(_controller.IsActive && _controller.AutoOrdersEnabled091,
                    "Controller Auto stopped before the requested boundary; preserve the pending outcome for investigation.");
                yield return null;
            }
            _stopReason = _controller.LastCompletedTowerFloor108 >= _settings.ThroughFloor ? "completed_floor_bound" : "duration_bound";
            _controller.SetAutoOrders091(false);
        }

        void LateUpdate()
        {
            if (!_watching || _controller == null) return;
            _frames++;
            _maxFrameSeconds = Mathf.Max(_maxFrameSeconds, Time.unscaledDeltaTime);
            if (_frameSample.Count < 4096) _frameSample.Add(Time.unscaledDeltaTime);
            if (_controller.OwnedSequenceDirector078?.IsPlaying == true) _animatedFrames++;
            CaptureControllerCounters110();
            if (_soak.Elapsed.TotalSeconds < _nextSample) return;
            _nextSample = _soak.Elapsed.TotalSeconds + 1;
            foreach (var entry in _pendingEvents) Log110(entry);
            _pendingEvents.Clear();
            var battle = Campaign110().Battle;
            _frameSample.Sort();
            var p95 = _frameSample.Count == 0 ? 0f : _frameSample[(int)((_frameSample.Count - 1) * 0.95f)];
            Log110(new { Event = "sample", Seconds = _soak.Elapsed.TotalSeconds, Battle = battle?.BattleId,
                Round = battle?.Round, Outcome = battle?.Outcome.ToString(), _controller.AutoOrdersEnabled091,
                _controller.IsResolving, SequencePlaying = _controller.OwnedSequenceDirector078?.IsPlaying,
                _controller.LastCompletedTowerFloor108, _controller.LastTowerFloorSeconds108,
                _controller.LastTowerTransitionSeconds108, _controller.LastAutoActionGapSeconds108,
                _controller.CompletedExactBeatCount076, _frames, _animatedFrames, FrameP95Seconds = p95, _maxFrameSeconds });
            _frameSample.Clear();
            if (_soak.Elapsed.TotalSeconds < _nextObjectSample) return;
            _nextObjectSample = _soak.Elapsed.TotalSeconds + 30;
            var observation = Stopwatch.StartNew();
            var objects = Resources.FindObjectsOfTypeAll<Object>().Length;
            var managed = Profiler.GetMonoUsedSizeLong();
            int handles;
            long workingSet;
            double cpu;
            using (var process = Process.GetCurrentProcess())
            {
                process.Refresh(); handles = process.HandleCount;
                workingSet = process.WorkingSet64; cpu = process.TotalProcessorTime.TotalSeconds;
            }
            _peakObjects = Math.Max(_peakObjects, objects);
            _peakHandles = Math.Max(_peakHandles, handles);
            _peakWorkingSet = Math.Max(_peakWorkingSet, workingSet);
            _peakManagedBytes = Math.Max(_peakManagedBytes, managed);
            Log110(new { Event = "resources", Seconds = _soak.Elapsed.TotalSeconds, Objects = objects, Handles = handles,
                WorkingSetBytes = workingSet, ManagedBytes = managed, CpuSeconds = cpu, Gc0 = GC.CollectionCount(0),
                Gc1 = GC.CollectionCount(1), Gc2 = GC.CollectionCount(2), ObservationMilliseconds = observation.Elapsed.TotalMilliseconds });
        }

        void CaptureControllerCounters110()
        {
            if (_controller == null) return;
            // A completed transition can end the coroutine before LateUpdate.
            // Read monotonic controller counters without inventing rendered frames.
            _peakExactBeats = Math.Max(_peakExactBeats, _controller.CompletedExactBeatCount076);
            _peakActors = Math.Max(_peakActors, _controller.OwnedDiorama078?.ActiveActorCount ?? 0);
            _lastCompletedFloor = Math.Max(_lastCompletedFloor, _controller.LastCompletedTowerFloor108);
        }

        void ObserveCommit110()
        {
            var state = Campaign110();
            var reward = state.Battle?.Reward;
            if (reward != null && !reward.Claimed) _seenPendingRewards.Add(reward.RewardId);
            foreach (var id in state.Guild.Development.ClaimedBattleRewardIds)
                if (_seenRewards.Add(id)) _pendingEvents.Add(new { Event = "new_claim", Id = id,
                    Battle = state.Battle?.BattleId, state.Guild.TreasuryXp, Recruits = state.Guild.Recruits.Count });
        }

        IEnumerator FinishChecks110()
        {
            // OFF prevents another order. Let the currently committed animation
            // finish; its pending victory is allowed to remain safely unclaimed.
            var deadline = _wall.Elapsed.TotalSeconds + 60;
            while (_controller != null && _controller.IsResolving && _wall.Elapsed.TotalSeconds < deadline) yield return null;
            CaptureControllerCounters110();
            _watching = false;
            Require110(_controller == null || !_controller.IsResolving, "Committed playback did not settle within 60 seconds after Auto OFF.");
            ObserveCommit110();
            var state = Campaign110();
            var claims = state.Guild.Development.ClaimedBattleRewardIds;
            Require110(claims.Count == claims.Distinct(StringComparer.Ordinal).Count(), "Duplicate battle reward receipt.");
            Require110(_seenRewards.IsSubsetOf(claims), "An observed claimed reward disappeared.");
            Require110(_seenPendingRewards.All(id => claims.Contains(id) || state.Battle?.Reward?.RewardId == id), "A pending outcome disappeared without its reward receipt.");
            Require110(_initialItems.IsSubsetOf(OwnedItems110(state)), "Previously owned equipment disappeared during Auto.");
            Require110(_initialRecruits.IsSubsetOf(state.Guild.Recruits.Select(value => value.RecruitId)), "A prior recruit disappeared during Auto.");
            Require110(state.Guild.Recruits.Where(value => !_initialRecruits.Contains(value.RecruitId))
                .All(value => value.Equipment.Assignments.Count > 0), "A new Tower recruit arrived without equipment.");
            var gearIds = state.Guild.Recruits.SelectMany(value => value.Equipment.Assignments).Select(value => value.Item.InstanceId).ToArray();
            Require110(gearIds.Distinct(StringComparer.Ordinal).Count() == gearIds.Length &&
                !state.Guild.Inventory.Any(value => gearIds.Contains(value.InstanceId)), "Duplicate equipped/inventory item ownership.");
            _rewardsAndOwnershipVerified110 = true;
            var floors110 = SecondDimension.Gameplay.Campaign022.CampaignProgressionCommandService022
                .DescribeTowerFloors094(state, SecondDimension.Presentation.Campaign022.CampaignRegistry022.LoadFromResources());
            Require110(floors110.IsSuccess, "Final Tower sequence authority failed: " + string.Join("; ", floors110.Errors));
            _savedHighestCompletedFloor110 = floors110.Value.HighestActualFloor;
            _savedLastBankedFloor130 = floors110.Value.LatestCompletedActualFloor130;
            RequireSavedTowerCompletion130(floors110.Value, _lastCompletedFloor);
            _towerSequenceVerified110 = true;
            var saved = new AtomicSaveStore().ReadWithRecovery(_settings.SavePath);
            Require110(saved.IsSuccess, "Durable save validation failed: " + string.Join("; ", saved.Errors));
            Require110(saved.Value.CanonicalStateHash == CanonicalJson.Sha256Hex(state), "Rendered runtime differs from the durable save.");
            var durableHash = HashFile110(_settings.SavePath);
            var reloadDirectory = Path.Combine(_settings.OutputDirectory, "final_reload_check");
            Directory.CreateDirectory(reloadDirectory);
            var reloadPath = Path.Combine(reloadDirectory, "Reload.json");
            File.Copy(_settings.SavePath, reloadPath, false);
            var reloaded = new M1RuntimeCoordinator(null, reloadPath);
            var restored = (CampaignState)CampaignField.GetValue(reloaded);
            Require110(CanonicalJson.Sha256Hex(restored) == saved.Value.CanonicalStateHash, "Actual coordinator reload changed earned receipts or gear.");
            Require110(HashFile110(_settings.SavePath) == durableHash, "Reload check changed the ongoing isolated save.");
            Require110(HashFile110(_settings.SourcePath) == _sourceHash, "The preserved earned source bytes changed.");
            _durableReloadVerified110 = true;
            Log110(new { Event = "durable_reload_verified", CanonicalHash = saved.Value.CanonicalStateHash,
                Battle = restored.Battle?.BattleId, PendingReward = restored.Battle?.Reward?.Claimed == false,
                ClaimedRewardCount = claims.Count, FinalReloadPath = reloadPath });
            // Playback acceptance remains strict. Collect independent lifecycle
            // evidence first, even when a short diagnostic has no exact-art beat.
            Require110(_animatedFrames > 0 && _peakExactBeats > 0, "No rendered exact-art playback was observed; this is not completed soak evidence.");
            Require110(_lastCompletedFloor >= _startFloor, "No completed Auto floor was observed within the run bound.");
        }

        // QA comparison only. A restarted run can bank floor 1 while retaining
        // a lifetime record of 300; the record is not the latest completion.
        public static void RequireSavedTowerCompletion130(
            SecondDimension.Gameplay.Campaign022.TowerFloorProgress094 floors, int observedLastCompletedFloor)
        {
            Require110(floors != null && (observedLastCompletedFloor == 0 ||
                floors.LatestCompletedActualFloor130 == observedLastCompletedFloor),
                "Final controller floor differs from the certified Tower completion sequence.");
        }

        static HashSet<string> OwnedItems110(CampaignState state) => new HashSet<string>(
            state.Guild.Inventory.Select(value => value.InstanceId).Concat(state.Guild.Recruits
                .SelectMany(value => value.Equipment.Assignments).Select(value => value.Item.InstanceId)), StringComparer.Ordinal);
        Button FindButton110(string name) => Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(value => value.name == name && value.IsInteractable());
        void Click110(Button button)
        {
            Require110(ButtonReadiness122.TryHit(button, out var pointer, out _, out var diagnostic),
                "Actual button is blocked at its visible center: " + button.name + ". " + diagnostic);
            Log110(new { Event = "ui_pointer_click", Button = button.name, Seconds = _wall.Elapsed.TotalSeconds });
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }
        void OnRuntimeLog110(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (_errors.Count < 100) _errors.Add(type + ": " + message + "\n" + stack);
        }
        void Log110(object entry) { _log?.WriteLine(JsonConvert.SerializeObject(entry)); _log?.Flush(); }
        static void Require110(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static string HashFile110(string path)
        {
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", string.Empty);
        }
        void FinishReport110()
        {
            if (_coordinator != null) _coordinator.Changed -= ObserveCommit110;
            Application.logMessageReceived -= OnRuntimeLog110;
            var passed = _failure == null && _errors.Count == 0;
            foreach (var entry in _pendingEvents) Log110(entry);
            _log?.Dispose(); _log = null;
            if (_settings != null) File.WriteAllText(Path.Combine(_settings.OutputDirectory, "tower_soak110_result.json"),
                JsonConvert.SerializeObject(new {
                    Status = passed ? "PASS" : "FAIL", StopReason = _stopReason, Failure = _failure, Errors = _errors,
                    RunScope = _settings.Seconds < 1800 ? "SHORT_RENDERED_DIAGNOSTIC" : "BOUNDED_RENDERED_SOAK",
                    RequestedAutoSeconds = _settings.Seconds,
                    RequestedDurationReached = _soak.Elapsed.TotalSeconds >= _settings.Seconds,
                    ThirtyMinutesOfAutoObserved = _soak.Elapsed.TotalSeconds >= 1800,
                    RequestedFloorReached = _lastCompletedFloor >= _settings.ThroughFloor,
                    RewardsAndOwnershipVerified = _rewardsAndOwnershipVerified110,
                    DurableReloadVerified = _durableReloadVerified110,
                    TowerSequenceVerified = _towerSequenceVerified110,
                    SavedHighestCompletedFloor = _savedHighestCompletedFloor110,
                    SavedLastBankedFloor = _savedLastBankedFloor130,
                    RenderedExactPlaybackObserved = _animatedFrames > 0 && _peakExactBeats > 0,
                    CompletedAutoFloorObserved = _lastCompletedFloor >= _startFloor,
                    WallSeconds = _wall.Elapsed.TotalSeconds, AutoSeconds = _soak.Elapsed.TotalSeconds,
                    StartFloor = _startFloor, TargetCompletedFloor = _settings.ThroughFloor, LastCompletedFloor = _lastCompletedFloor,
                    _frames, _animatedFrames, _peakExactBeats, _peakActors, _maxFrameSeconds,
                    _peakObjects, _peakHandles, _peakWorkingSet, _peakManagedBytes,
                    AutomaticGameplay = true, Driver = "Rendered shipping battle controller", SyntheticCombat = false,
                    ForcedFloors = false, PlayerPrefsWrites = false, Source = _settings.SourcePath, Save = _settings.SavePath
                }, Formatting.Indented));
            Debug.Log("TOWER_RENDERED_SOAK110 " + (passed ? "PASS" : "FAIL") + " " + _settings?.OutputDirectory);
            Application.Quit(passed ? 0 : 1);
        }
    }
}
