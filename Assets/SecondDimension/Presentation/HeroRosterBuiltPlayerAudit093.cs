using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.State;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>Opt-in roster evidence only. Never touches the user's normal save.</summary>
    public sealed partial class HeroRosterBuiltPlayerAudit093 : MonoBehaviour
    {
        M1FlowPresenter _presenter;
        string _root;
        List<HeroRosterAuditRow093> _rows;
        string _fatal = "";
        int _first = 1, _last = 300;

        public static bool IsRequested093(IReadOnlyList<string> arguments) =>
            arguments != null && arguments.Contains("--sd-roster-audit-093");

        public static string ResolveBootSavePath093(IReadOnlyList<string> arguments,
            string persistentDataPath, string temporaryCachePath)
        {
            if (TryResolveRoot093(arguments, persistentDataPath, out var root, out _))
                return Path.Combine(root, "FixtureSaves", "isolated_bootstrap_093.json");
            // Fail closed. A malformed command never falls back to personal data,
            // and no stale quarantine save from an earlier audit is loaded.
            return Path.GetFullPath(Path.Combine(string.IsNullOrWhiteSpace(temporaryCachePath)
                ? Path.GetTempPath() : temporaryCachePath, "SECOND_DIMENSION_ROSTER_QUARANTINE_093",
                Guid.NewGuid().ToString("N") + ".json"));
        }

        public static bool TryResolveRoot093(IReadOnlyList<string> arguments, string persistentDataPath,
            out string root, out string error)
        {
            root = null; error = "";
            try
            {
                var requested = Argument093(arguments?.ToArray() ?? Array.Empty<string>(), "--sd-roster-audit-output", "");
                if (string.IsNullOrWhiteSpace(requested) || !Path.IsPathRooted(requested))
                    throw new InvalidOperationException("An explicit absolute --sd-roster-audit-output directory is required.");
                root = Path.GetFullPath(requested).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var personal = Path.GetFullPath(persistentDataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.Equals(root, Path.GetPathRoot(root)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(root, personal, StringComparison.OrdinalIgnoreCase) ||
                    (root + Path.DirectorySeparatorChar).StartsWith(personal + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    (personal + Path.DirectorySeparatorChar).StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Roster evidence must not overlap the personal save directory or a drive root.");
                return true;
            }
            catch (Exception failure) { root = null; error = failure.Message; return false; }
        }

        public static bool TryResolveRange093(string first, string last, out int from, out int through) =>
            int.TryParse(first, out from) & int.TryParse(last, out through) &&
            from >= 1 && through <= 300 && from <= through;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap093()
        {
            if (!Environment.GetCommandLineArgs().Contains("--sd-roster-audit-093")) return;
            Application.runInBackground = true;
            var host = new GameObject("Isolated All Hero Roster Audit 093");
            DontDestroyOnLoad(host);
            host.AddComponent<HeroRosterBuiltPlayerAudit093>();
        }

        IEnumerator Start()
        {
            var run = Run093();
            while (true)
            {
                object next = null;
                var moved = false;
                try { moved = run.MoveNext(); if (moved) next = run.Current; }
                catch (Exception error) { _fatal = error.ToString(); }
                if (!moved || !string.IsNullOrEmpty(_fatal)) break;
                yield return next;
            }
            var failed = !string.IsNullOrEmpty(_fatal) || _rows == null ||
                _composition093.Any(row => !string.IsNullOrEmpty(row.failure)) ||
                _rows.Any(row => IncludesHeroForCapture093(row.rosterId, row.stableId, _first, _last, _reviewedRemasterPoses093) &&
                    row.eligibility == "ACCEPTED" && (row.MechanicalStatus != "PASS_MECHANICAL_ONLY" || !row.runtimeUiVerified));
            if (!failed && Environment.GetCommandLineArgs().Contains(KeepOpenFlag093))
            {
                try { KeepReviewedBattleOpen093(true); }
                catch (Exception failure) { _fatal = failure.ToString(); failed = true; }
            }
            if (_root != null && _rows != null) WriteReport093(true);
            Debug.Log("ROSTER_AUDIT_093 " + (!failed ? "COMPLETE" : "FAILED " + _fatal));
#if !UNITY_EDITOR
            if (!_keptOpen093 || failed) Application.Quit(failed ? 93 : 0);
#else
            if (!_keptOpen093 || failed) Destroy(gameObject);
#endif
        }

        IEnumerator Run093()
        {
            var arguments = Environment.GetCommandLineArgs();
            if (!TryResolveRoot093(arguments, Application.persistentDataPath, out _root, out var pathError))
                throw new InvalidOperationException(pathError);
            if (Directory.Exists(_root) && Directory.EnumerateFileSystemEntries(_root).Any())
                throw new InvalidOperationException("Use a fresh roster evidence directory; existing data is never overwritten.");
            if (!TryResolveRange093(Argument093(arguments, "--sd-roster-audit-first", "1"),
                    Argument093(arguments, "--sd-roster-audit-last", "300"), out _first, out _last))
                throw new InvalidOperationException("Roster range must satisfy 1 <= first <= last <= 300; an empty run is not a pass.");
            Directory.CreateDirectory(_root);
            Directory.CreateDirectory(Path.Combine(_root, "Screenshots"));
            Directory.CreateDirectory(Path.Combine(_root, "FixtureSaves"));
            _rows = HeroRosterAudit093.Census093();
            _reviewedRemasterPoses093 = arguments.Contains(ReviewedPoseFlag093);
            if (!_rows.Any(row => row.eligibility == "ACCEPTED" &&
                    IncludesHeroForCapture093(row.rosterId, row.stableId, _first, _last, _reviewedRemasterPoses093)))
                throw new InvalidOperationException("No eligible reviewed hero lies in the requested capture range; zero captures cannot pass.");
            WriteReport093(false);
            for (var frame = 0; frame < 180 && _presenter == null; frame++)
            {
                _presenter = FindFirstObjectByType<M1FlowPresenter>();
                if (_presenter == null) yield return null;
            }
            if (_presenter == null) throw new InvalidOperationException("Shipping M1FlowPresenter did not boot.");
            var composition = RunCompositions093();
            while (composition.MoveNext()) yield return composition.Current;
            foreach (var hero in HeroRosterAudit093.Catalog093.AcceptedHeroes.OrderBy(value => value.RosterId)
                         .Where(value => IncludesHeroForCapture093(value.RosterId, value.StableId,
                             _first, _last, _reviewedRemasterPoses093)))
            {
                var row = _rows.Single(value => value.rosterId == hero.RosterId);
                var save = Path.Combine(_root, "FixtureSaves", hero.StableId + ".json");
                CampaignState fixture = null;
                M1RuntimeCoordinator coordinator = null;
                Step093(row, () =>
                {
                    fixture = HeroRosterAudit093.CreateFixture093(hero);
                    HeroRosterAudit093.BindArt093(hero, row);
                    HeroRosterAudit093.Write093(save, fixture);
                    coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
                    _presenter.Initialize(coordinator);
                    if (hero.Rank != HeroMasterRank087.SS)
                        _presenter.ShowRosterAudit093(M1Screen.GuildOperations,
                            fixture.Guild.GuildCity.RecruitmentBoard.Applicants.Single().RecruitId);
                });
                if (string.IsNullOrEmpty(row.failure) && hero.Rank != HeroMasterRank087.SS)
                {
                    yield return new WaitForSecondsRealtime(0.22f);
                    yield return new WaitForEndOfFrame();
                    Step093(row, () =>
                    {
                        row.applicantCapture = Capture093(hero, "applicant");
                        var selected = GameObject.Find("Selected Applicant Portrait 074");
                        var art = selected == null ? null : selected.GetComponentsInChildren<Image>()
                            .FirstOrDefault(value => value.name == "Standing Hero Sprite 090" && value.sprite != null);
                        if (art == null || art.sprite.texture.name != row.idleTexture)
                            throw new InvalidOperationException("Shipping Recruitment Desk did not display this exact hero texture.");
                    });
                }
                // SS offers do not have an applicant-screen wait. Give Unity a
                // frame to retire any previous battle host removed by the empty
                // fixture's menu before the signed battle rebinds that host.
                yield return null;
                Step093(row, () =>
                {
                    fixture = HeroRosterAudit093.SignOutfitAndPlace093(fixture, hero, row);
                    HeroRosterAudit093.BindArt093(hero, row);
                    fixture = HeroRosterAudit093.StartBattleAndVerifyForecast093(fixture, row);
                    HeroRosterAudit093.Write093(save, fixture);
                    var reloaded = HeroRosterAudit093.Read093(save);
                    if (!reloaded.Guild.Recruits.Any(value => value.RecruitId == row.recruitId && value.AuthoredStableRecruitId == hero.StableId))
                        throw new InvalidOperationException("Signed identity did not survive the production save reload.");
                    coordinator = new M1RuntimeCoordinator(HeroRosterAudit093.ContentRoot093, save);
                    _presenter.Initialize(coordinator);
                    var member = coordinator.State.Battle?.PlayerUnions.SelectMany(value => value.Members)
                        .SingleOrDefault(value => value.MemberId == row.recruitId);
                    if (member == null || member.PortraitAuthorityId != hero.StableId)
                        throw new InvalidOperationException("Production battle projection lost the signed portrait authority.");
                    _presenter.ShowRosterAudit093(M1Screen.Battle);
                });
                if (string.IsNullOrEmpty(row.failure))
                {
                    yield return new WaitForSecondsRealtime(0.32f);
                    yield return new WaitForEndOfFrame();
                    Step093(row, () =>
                    {
                        row.battleCapture = Capture093(hero, "battle");
                        InspectLiveHero093(row);
                        row.runtimeUiVerified = true;
                    });
                }
                if (_reviewedRemasterPoses093 && string.IsNullOrEmpty(row.failure))
                {
                    var poses = RunReviewedRemasterPoses093(hero, row, coordinator);
                    while (poses.MoveNext()) yield return poses.Current;
                }
                var eligibleForKeepOpen = row.runtimeUiVerified && row.MechanicalStatus == "PASS_MECHANICAL_ONLY" &&
                    IsReviewedPoseIdentity099(hero.StableId);
                _lastReviewedIdentity093 = eligibleForKeepOpen ? hero.StableId : null;
                _lastReviewedCoordinator093 = eligibleForKeepOpen ? coordinator : null;
                WriteReport093(false);
                Debug.Log("ROSTER_AUDIT_093 " + hero.RosterId + "/300 " + hero.StableId + " " + row.MechanicalStatus +
                          " UI=" + row.runtimeUiVerified + " ART=" + row.artCategory + " " + row.failure);
                yield return null;
            }
        }

        static void Step093(HeroRosterAuditRow093 row, Action action)
        {
            if (!string.IsNullOrEmpty(row.failure)) return;
            try { action(); }
            catch (Exception error) { row.failure = error.ToString(); }
        }

        static void InspectLiveHero093(HeroRosterAuditRow093 row)
        {
            var controller = FindFirstObjectByType<M2BattleExperienceController072>();
            if (controller == null || !controller.IsActive || controller.OwnedDiorama078 == null)
                throw new InvalidOperationException("Shipping battle never became active; this is a screen/lifecycle failure, not evidence of a missing hero texture.");
            var actor = controller.OwnedDiorama078.ResolveActor(row.recruitId, string.Empty);
            var artwork = actor?.CurrentArtwork076;
            if (artwork == null || artwork.sprite == null || !artwork.gameObject.activeInHierarchy ||
                artwork.color.a <= 0.25f || artwork.sprite.texture.name != row.battleIdleTexture)
                throw new InvalidOperationException("Actual battle actor is absent or uses a different identity texture.");
            var sprite = artwork.sprite;
            var visible = M1SilhouetteFraming091.VisibleRect091(sprite);
            var rect = artwork.rectTransform.rect;
            var scale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
            var origin = rect.center - sprite.rect.size * (scale * 0.5f);
            var localMin = origin + (visible.min - sprite.rect.min) * scale;
            var localMax = origin + (visible.max - sprite.rect.min) * scale;
            var a = RectTransformUtility.WorldToScreenPoint(null, artwork.rectTransform.TransformPoint(localMin));
            var b = RectTransformUtility.WorldToScreenPoint(null, artwork.rectTransform.TransformPoint(localMax));
            var screenMin = Vector2.Min(a, b); var screenMax = Vector2.Max(a, b);
            row.visibleWidthPixels = screenMax.x - screenMin.x;
            row.visibleHeightPixels = screenMax.y - screenMin.y;
            row.feetPixels = screenMin.y;
            if (row.visibleHeightPixels <= Screen.height * 0.28f || row.visibleWidthPixels < 12f ||
                screenMin.x < 0f || screenMin.y < 0f || screenMax.x > Screen.width || screenMax.y > Screen.height)
                throw new InvalidOperationException("Actual Canvas-scaled hero is tiny or outside the viewport: " +
                    row.visibleWidthPixels + "x" + row.visibleHeightPixels + " at " + screenMin);
        }

        string Capture093(HeroMaster300Hero087 hero, string kind)
        {
            var relative = "Screenshots/" + hero.RosterId.ToString("000") + "_" + hero.StableId + "_" + kind + ".png";
            return CaptureRelative093(relative);
        }

        string CaptureRelative093(string relative)
        {
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            Texture2D reduced = null;
            RenderTexture target = null;
            var previous = RenderTexture.active;
            try
            {
                texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0, false);
                texture.Apply(false, false);
                var factor = Mathf.Min(1f, 1280f / Screen.width, 800f / Screen.height);
                if (factor < 1f)
                {
                    var width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * factor));
                    var height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * factor));
                    target = RenderTexture.GetTemporary(width, height, 0);
                    Graphics.Blit(texture, target);
                    RenderTexture.active = target;
                    reduced = new Texture2D(width, height, TextureFormat.RGB24, false);
                    reduced.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                    reduced.Apply(false, false);
                }
                File.WriteAllBytes(Path.Combine(_root, relative), (reduced ?? texture).EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                if (reduced != null) Destroy(reduced);
                Destroy(texture);
            }
            return relative;
        }

        void WriteReport093(bool complete)
        {
            var report = new
            {
                version = "ROSTER_093", completed = complete, fatal = _fatal,
                utc = DateTime.UtcNow.ToString("O"), platform = Application.platform.ToString(),
                screenWidth = Screen.width, screenHeight = Screen.height, requestedFirst = _first, requestedLast = _last,
                reviewedRemasterPosesOnly = _reviewedRemasterPoses093,
                keptOpenForManualReviewedBattle = _keptOpen093,
                manualPlaytestFixture = _keptOpen093
                    ? Path.Combine(_root, "FixtureSaves", _lastReviewedIdentity093 + ".json") : string.Empty,
                poseCaptureBoundary = "Optional explicit QA idle/action/idle placement checks only; no Art execution or outcome is simulated. The separate Forecast round uses the real combat authority.",
                evidenceBoundary = "Isolated fixture lead/XP and completed opening; production sign, SS code, Union, Forecast and save authorities. NOT natural acquisition of all heroes; NOT art quality certification. Static actions are labeled. User saves untouched.",
                accepted = _rows.Count(value => value.eligibility == "ACCEPTED"),
                quarantined = _rows.Count(value => value.eligibility == "QUARANTINED"),
                mechanicalPasses = _rows.Count(value => value.MechanicalStatus == "PASS_MECHANICAL_ONLY"),
                liveUiPasses = _rows.Count(value => value.runtimeUiVerified),
                failures = _rows.Count(value => !string.IsNullOrEmpty(value.failure)),
                proceduralPlaceholders = _rows.Count(value => value.artCategory == "PROCEDURAL_PLACEHOLDER_NOT_FINISHED_ART"),
                professionalArtPasses = _rows.Count(value => value.professionalArtReviewed), rows = _rows,
                compositionSource = _compositionSource093, compositionSourceSha256 = _compositionSourceHash093,
                compositions = _composition093
            };
            File.WriteAllText(Path.Combine(_root, "roster_300_report.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
            var html = new StringBuilder("<!doctype html><meta charset='utf-8'><title>300 Hero Runtime Audit</title><style>body{background:#111c2c;color:#e4edf6;font:16px system-ui;margin:30px}article{border-top:1px solid #516178;padding:16px 0}img{width:48%;max-width:920px;vertical-align:top}p{max-width:1100px}code{color:#ffcf85}.fail{color:#ff9999}input{font:inherit;padding:12px;width:70%;position:sticky;top:0}summary{cursor:pointer}</style><h1>300 Hero Runtime Audit — actual Windows UI</h1><p>Every row is separate. Mechanical success does not mean finished art. Fixture XP/leads expose accepted heroes for verification; this is not natural campaign acquisition. The 50 quarantined records remain unavailable. Screenshots are the shipping Recruitment Desk and battle builder at the real game Canvas scale, not posters.</p><input placeholder='Filter name, ID, status or art category' oninput=\"for(const a of document.querySelectorAll('article'))a.hidden=!a.textContent.toLowerCase().includes(this.value.toLowerCase())\"> ");
            foreach (var composition in _composition093)
            {
                html.Append("<article><h2>Actual saved party composition: ").Append(composition.members)
                    .Append(" members</h2><p>").Append(Escape093(composition.fixtureBoundary)).Append("</p>");
                if (!string.IsNullOrEmpty(composition.screenshot))
                    html.Append("<a href='").Append(composition.screenshot).Append("'><img src='")
                        .Append(composition.screenshot).Append("' alt='Actual saved party'></a>");
                html.Append("<details><summary>Screen geometry and source preservation</summary><pre>")
                    .Append(Escape093(JsonConvert.SerializeObject(composition, Formatting.Indented))).Append("</pre></details></article>");
            }
            foreach (var row in _rows)
            {
                html.Append("<article><h2>").Append(Escape093(row.rosterId + " · " + row.name + " · " + row.stableId)).Append("</h2><p>")
                    .Append(Escape093(row.MechanicalStatus + " · " + row.artCategory + " · " + row.acquisitionProof)).Append("</p>");
                if (row.poseProofs.Count > 0)
                    html.Append("<p>Extra labeled images: QA pose placement only, not an executed combat Art. Body proportions require visual review.</p>");
                foreach (var capture in new[] { row.applicantCapture, row.battleCapture }.Concat(row.poseProofs.Select(value => value.capture))
                             .Where(value => !string.IsNullOrEmpty(value)))
                    html.Append("<a href='").Append(capture).Append("'><img loading='lazy' src='").Append(capture).Append("' alt='").Append(Escape093(row.name)).Append("'></a>");
                html.Append("<details><summary>Exact binding and verification evidence</summary><pre>")
                    .Append(Escape093(JsonConvert.SerializeObject(row, Formatting.Indented))).Append("</pre></details></article>");
            }
            File.WriteAllText(Path.Combine(_root, "index.html"), html.ToString());
        }

        static string Escape093(string text) => (text ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;");
        static string Argument093(string[] arguments, string key, string fallback)
        {
            var index = Array.IndexOf(arguments, key);
            return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : fallback;
        }
    }
}
