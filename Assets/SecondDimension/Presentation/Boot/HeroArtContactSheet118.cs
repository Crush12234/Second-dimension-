using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.Recruitment;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Boot
{
    // Explicit built-resource QA only. Boot never creates a campaign coordinator
    // in this mode. These images are contact sheets, not another game renderer.
    public sealed class HeroArtContactSheet118 : MonoBehaviour
    {
        public const string Flag118 = "--sd-hero-art118";
        public const string OutputFlag118 = "--sd-hero-art118-output=";
        public const string CatalogResource118 = "SecondDimension/HeroMaster300/Data/HERO_MASTER_001_300";
        const int Width118 = 1600, Height118 = 1200, PerSheet118 = 12;
        static readonly UTF8Encoding Utf8118 = new UTF8Encoding(false);

        public sealed class Pose118
        {
            public string BindingAuthority = "NOT_CALLED", ResourceKey = "", TextureName = "";
            public string SourceKind = "MISSING", RenderStatus = "NOT_RENDERED", PixelSha256 = "", Error = "";
            public int TextureWidth, TextureHeight, VisibleProbePixels;
            public bool TextureCpuReadable;
            public float[] SpriteRect = Array.Empty<float>();
            [JsonIgnore] public Sprite Sprite;
        }

        public sealed class Row118
        {
            public int RosterId;
            public string StableId, GameEntityId, Name, Rank, Race, Role, Weapon, MetadataStatus;
            public string[] QuarantineReasons = Array.Empty<string>();
            public Pose118 Portrait = new Pose118(), Idle = new Pose118(), Action = new Pose118();
            public bool SameIdleActionPixelsOrSource, DistinctBuiltActionCandidate;
            public string Sheet;
            public int SheetCell;
        }

        public static bool IsRequested118(IReadOnlyList<string> arguments) => arguments != null &&
            arguments.Any(value => value != null && value.StartsWith(Flag118, StringComparison.OrdinalIgnoreCase));

        public static string ValidateOutput118(IReadOnlyList<string> arguments, string personalPath, string dataPath)
        {
            bool enabled = false;
            string output = null;
            foreach (var argument in arguments ?? Array.Empty<string>())
            {
                if (argument == Flag118 && !enabled) enabled = true;
                else if (argument != null && argument.StartsWith(OutputFlag118, StringComparison.Ordinal) && output == null)
                    output = argument.Substring(OutputFlag118.Length);
                else if (argument != null && argument.StartsWith("--sd-", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Art contact QA rejects duplicate, malformed and combined QA flags.");
            }
            if (!enabled) throw new InvalidOperationException("Explicit --sd-hero-art118 is required.");
            output = ManualEarnedSaveReview097.LocalAbsolutePath097(output);
            var personal = ManualEarnedSaveReview097.LocalAbsolutePath097(personalPath);
            var data = ManualEarnedSaveReview097.LocalAbsolutePath097(dataPath);
            var build = Path.GetDirectoryName(data);
            if (Same118(output, Path.GetPathRoot(output)) || Overlaps118(output, personal) || Overlaps118(output, build))
                throw new InvalidOperationException("Art evidence must be outside personal data and the player/project installation.");
            if (File.Exists(output) || Directory.Exists(output) || !Directory.Exists(Path.GetDirectoryName(output)))
                throw new InvalidOperationException("Use a fresh output directory with an existing parent; evidence is never overwritten.");
            RejectReparse118(output); RejectReparse118(personal); RejectReparse118(data);
            return output;
        }

        // Called only by the early opt-in Boot branch, before any save path or
        // gameplay presenter is selected. Invalid requests stay fail-closed.
        public static void Begin118(GameObject boot, IReadOnlyList<string> arguments, string personalPath, string dataPath)
        {
            M1PresentationCoordinatorRegistry.Register(() =>
                throw new InvalidOperationException("Resource-only art QA does not instantiate a campaign coordinator."));
            try
            {
                if (Application.isEditor) throw new InvalidOperationException("Use an actual packaged player for this opt-in export.");
                var output = ValidateOutput118(arguments, personalPath, dataPath);
                var helper = boot.AddComponent<HeroArtContactSheet118>();
                helper._output = output;
            }
            catch (Exception error)
            {
                Debug.LogError("HERO_ART_CONTACT118 BLOCKED: " + error.Message);
                Application.Quit(118);
            }
        }

        string _output, _failure = "", _catalogHash = "";
        bool _ownsOutput;
        List<Row118> _rows;
        readonly List<object> _sheets = new List<object>();
        RenderSurface118 _probe, _sheet;

        IEnumerator Start()
        {
            var run = Run118();
            while (true)
            {
                bool moved;
                object next = null;
                try { moved = run.MoveNext(); if (moved) next = run.Current; }
                catch (Exception error) { _failure = error.ToString(); break; }
                if (!moved) break;
                yield return next;
            }
            _probe?.Dispose(); _sheet?.Dispose();
            try { if (_ownsOutput) WriteReport118(); }
            catch (Exception error) { _failure += "\nReport write failed: " + error; }
            Debug.Log("HERO_ART_CONTACT118 " + (string.IsNullOrEmpty(_failure) ? "EXPORT_COMPLETE (art quality not certified)" : "FAILED " + _failure));
            Application.Quit(string.IsNullOrEmpty(_failure) ? 0 : 118);
        }

        IEnumerator Run118()
        {
            // Revalidate immediately before the sole output-directory creation.
            _output = ValidateOutput118(Environment.GetCommandLineArgs(), Application.persistentDataPath, Application.dataPath);
            Directory.CreateDirectory(_output);
            RejectReparse118(_output);
            if (Directory.EnumerateFileSystemEntries(_output).Any())
                throw new InvalidOperationException("The fresh art output changed while preparing it.");
            _ownsOutput = true;
            var asset = Resources.Load<TextAsset>(CatalogResource118);
            if (asset == null) throw new InvalidOperationException("Built Hero Master 300 resource is missing.");
            _catalogHash = Hash118(asset.bytes);
            _rows = ReadCatalog118(asset.text);
            _probe = new RenderSurface118(160, 240, 30);
            _sheet = new RenderSurface118(Width118, Height118, 31);
            for (int first = 0; first < _rows.Count; first += PerSheet118)
            {
                _sheet.Clear118();
                _sheet.Panel118(0, 0, Width118, Height118, new Color(.035f, .05f, .085f, 1));
                var title = "Same 300 catalog IDs | built resources | " + (first + 1) + "-" + Math.Min(first + PerSheet118, _rows.Count);
                _sheet.Label118(title, 20, 10, 1560, 37, 27, Color.white);
                _sheet.Label118("P portrait   I idle   A action | distinct source is a review candidate, never finished-art certification", 20, 48, 1560, 28, 19, new Color(.8f, .84f, .9f));
                for (int i = first; i < Math.Min(first + PerSheet118, _rows.Count); i++)
                {
                    var row = _rows[i];
                    if (row.MetadataStatus == "ACCEPTED") Bind118(row, _probe);
                    row.Sheet = "heroes_" + (first + 1).ToString("000") + "_" + Math.Min(first + PerSheet118, _rows.Count).ToString("000") + ".png";
                    row.SheetCell = i - first + 1;
                    AddCard118(_sheet, row, i - first);
                    yield return null;
                }
                Canvas.ForceUpdateCanvases();
                yield return null; // allow the built font atlas to update before the actual camera render
                var image = _sheet.Read118();
                try
                {
                    var bytes = image.EncodeToPNG();
                    WriteNew118(Path.Combine(_output, _rows[first].Sheet), bytes);
                    _sheets.Add(new { File = _rows[first].Sheet, Width = Width118, Height = Height118, Sha256 = Hash118(bytes) });
                }
                finally { Destroy(image); }
                // Keep report metadata but release this helper's sprite references.
                // The existing binding caches retain their normal resource ownership.
                foreach (var row in _rows.Skip(first).Take(PerSheet118))
                    row.Portrait.Sprite = row.Idle.Sprite = row.Action.Sprite = null;
                Debug.Log("HERO_ART_CONTACT118 rendered " + Math.Min(first + PerSheet118, _rows.Count) + "/300");
            }
        }

        public static List<Row118> ReadCatalog118(string json)
        {
            var catalog = HeroMaster300Catalog087.FromJson(json); // validates the exact 300 structural identities
            var accepted = catalog.AcceptedHeroes.ToDictionary(value => value.StableId, StringComparer.Ordinal);
            var quarantined = catalog.QuarantinedHeroes.ToDictionary(value => value.StableId, StringComparer.Ordinal);
            return ((JArray)JObject.Parse(json)["heroes"]).Cast<JObject>().Select(raw =>
            {
                var id = raw.Value<string>("stable_id");
                var row = new Row118 {
                    RosterId = raw.Value<int>("roster_id"), StableId = id,
                    GameEntityId = raw.Value<string>("game_entity_id"), Name = raw.Value<string>("name"),
                    Rank = raw.Value<string>("rank"), Race = raw.Value<string>("race"),
                    Role = raw.Value<string>("role"), Weapon = raw.Value<string>("weapon"),
                    MetadataStatus = accepted.ContainsKey(id) ? "ACCEPTED" : "QUARANTINED"
                };
                if (quarantined.TryGetValue(id, out var blocked))
                {
                    row.QuarantineReasons = blocked.Reasons.ToArray();
                    foreach (var pose in new[] { row.Portrait, row.Idle, row.Action })
                        pose.SourceKind = "NOT_BOUND_METADATA_QUARANTINED";
                }
                return row;
            }).OrderBy(value => value.RosterId).ToList();
        }

        static void Bind118(Row118 row, RenderSurface118 probe)
        {
            row.Portrait = Resolve118(row, "PORTRAIT", probe);
            row.Idle = Resolve118(row, "IDLE", probe);
            row.Action = Resolve118(row, "ACTION", probe);
            row.SameIdleActionPixelsOrSource = SameVisual118(row.Idle, row.Action);
            row.DistinctBuiltActionCandidate = IsDistinctBuiltAction118(row.Idle, row.Action);
        }

        static Pose118 Resolve118(Row118 row, string pose, RenderSurface118 probe)
        {
            var result = new Pose118();
            try
            {
                Sprite sprite = null;
                string key = "";
                bool found;
                if (pose != "PORTRAIT" && BattleArtRuntimeRegistry011.TryResolvePose(row.StableId,
                        pose == "IDLE" ? BattleArtPoseDirector011.Idle : BattleArtPoseDirector011.ActionPrimary,
                        out sprite, out key))
                {
                    // The shipping actor frames every registry/resource pose
                    // before displaying it; M1VisualAssets fallbacks do so already.
                    sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
                    found = sprite != null;
                    result.BindingAuthority = "BattleArtRuntimeRegistry011.TryResolvePose(exact stable ID)";
                }
                else
                {
                    // Same permanent identity arguments as HeroRosterAudit093.BindArt093.
                    // No recruit, profile, class, procedural applicant or alias is created.
                    result.BindingAuthority = pose == "PORTRAIT" ? "M1VisualAssets.TryResolvePortrait" :
                        pose == "IDLE" ? "M1VisualAssets.TryResolveBattleStandee" : "M1VisualAssets.TryResolveBattleActionPose";
                    found = pose == "PORTRAIT"
                        ? M1VisualAssets.TryResolvePortrait(row.StableId, row.StableId, row.Race, row.StableId, row.Role, out sprite, out key)
                        : pose == "IDLE"
                            ? M1VisualAssets.TryResolveBattleStandee(row.StableId, row.StableId, row.Race, row.StableId, out sprite, out key)
                            : M1VisualAssets.TryResolveBattleActionPose(row.StableId, row.StableId, row.Race, row.StableId, out sprite, out key);
                }
                result.ResourceKey = key ?? "";
                if (!found || sprite == null || sprite.texture == null) return result;
                result.Sprite = sprite;
                result.SourceKind = M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(key)
                    ? "GENERATED_FALLBACK_NOT_FINISHED_ART" : "BUILT_IDENTITY_BOUND_SOURCE";
                result.TextureName = sprite.texture.name;
                result.TextureWidth = sprite.texture.width; result.TextureHeight = sprite.texture.height;
                result.TextureCpuReadable = sprite.texture.isReadable;
                result.SpriteRect = new[] { sprite.rect.x, sprite.rect.y, sprite.rect.width, sprite.rect.height };
                var rendered = probe.Probe118(sprite);
                result.VisibleProbePixels = rendered.VisibleProbePixels;
                result.PixelSha256 = rendered.PixelSha256;
                result.RenderStatus = rendered.RenderStatus;
            }
            catch (Exception error) { result.Error = error.ToString(); result.RenderStatus = "BIND_OR_RENDER_ERROR"; }
            return result;
        }

        public static bool SameVisual118(Pose118 idle, Pose118 action) => idle != null && action != null &&
            ((idle.Sprite != null && action.Sprite != null && idle.Sprite.texture == action.Sprite.texture && idle.Sprite.rect == action.Sprite.rect) ||
             (idle.RenderStatus == "RENDERED_VISIBLE" && action.RenderStatus == "RENDERED_VISIBLE" &&
              !string.IsNullOrEmpty(idle.PixelSha256) && idle.PixelSha256 == action.PixelSha256));

        public static bool IsDistinctBuiltAction118(Pose118 idle, Pose118 action) => idle != null && action != null &&
            idle.SourceKind == "BUILT_IDENTITY_BOUND_SOURCE" && action.SourceKind == "BUILT_IDENTITY_BOUND_SOURCE" &&
            idle.RenderStatus == "RENDERED_VISIBLE" && action.RenderStatus == "RENDERED_VISIBLE" &&
            !string.IsNullOrEmpty(idle.PixelSha256) && !string.IsNullOrEmpty(action.PixelSha256) && !SameVisual118(idle, action);

        static void AddCard118(RenderSurface118 surface, Row118 row, int cell)
        {
            float x = 10 + (cell % 4) * 397, y = 88 + (cell / 4) * 367;
            surface.Panel118(x, y, 388, 354, new Color(.09f, .12f, .18f, 1));
            surface.Label118(row.RosterId.ToString("000") + "  " + row.StableId + "\n" + row.Name,
                x + 8, y + 4, 372, 49, 17, Color.white);
            var poses = new[] { row.Portrait, row.Idle, row.Action };
            var labels = new[] { "P", "I", "A" };
            for (int p = 0; p < poses.Length; p++)
            {
                var pose = poses[p];
                float px = x + 7 + p * 125;
                surface.Label118(labels[p], px, y + 53, 119, 20, 16, new Color(.8f, .84f, .9f));
                surface.Picture118(pose.Sprite, px, y + 76, 119, 211);
                var label = pose.SourceKind == "NOT_BOUND_METADATA_QUARANTINED" ? "MISSING\nDATA BLOCKED" :
                    pose.SourceKind == "GENERATED_FALLBACK_NOT_FINISHED_ART" ? "GENERATED\nFALLBACK" :
                    pose.RenderStatus != "RENDERED_VISIBLE" ? "MISSING / ERROR" :
                    p == 2 && row.SameIdleActionPixelsOrSource ? "IDLE REUSED" : "BUILT SOURCE";
                surface.Label118(label, px, y + 290, 119, 35, 14,
                    label == "BUILT SOURCE" ? new Color(.55f, .86f, .67f) : new Color(1f, .76f, .42f));
            }
            surface.Label118(row.MetadataStatus == "QUARANTINED" ? "No approved runtime binding; disk absence not inferred." :
                row.DistinctBuiltActionCandidate ? "Distinct pose candidate - visual review required" : "Missing or reused action / review still required",
                x + 8, y + 327, 372, 24, 12, new Color(.8f, .84f, .9f));
        }

        void WriteReport118()
        {
            var rows = _rows ?? new List<Row118>();
            var complete = string.IsNullOrEmpty(_failure) && rows.Count == 300 && _sheets.Count == 25;
            var report = new {
                Mode = "OPT_IN_PACKAGED_RESOURCE_CONTACT_SHEETS_118", ExportComplete = complete, Failure = _failure,
                Application.version, Application.buildGUID, Application.unityVersion,
                CatalogResource = CatalogResource118, CatalogSha256 = _catalogHash,
                CatalogRows = rows.Count, AcceptedMetadataRows = rows.Count(value => value.MetadataStatus == "ACCEPTED"),
                QuarantinedMetadataRows = rows.Count(value => value.MetadataStatus == "QUARANTINED"),
                DistinctBuiltActionCandidates = rows.Count(value => value.DistinctBuiltActionCandidate),
                ReusedIdleActionRows = rows.Count(value => value.SameIdleActionPixelsOrSource),
                FinishedArtCertification = "NOT_PERFORMED", AnimationCertification = "NOT_PERFORMED",
                Limits = "Actual built sprites rendered through Unity UI. Distinct pixels are only review candidates. Quarantined rows are not bound and do not prove source-file absence. No acquisition, character creation, battle or campaign state is exercised.",
                Probe = "160x240 transparent Unity camera/UI readback; pixel hashes compare rendered thumbnails, not original file hashes.",
                CampaignCoordinatorCreated = false, SaveReadOrWritten = false, PlayerPrefsWritten = false,
                Sheets = _sheets, Rows = rows
            };
            var csv = new StringBuilder("roster_id,stable_id,game_entity_id,name,rank,race,role,weapon,metadata_status,quarantine_reasons,sheet,sheet_cell,same_idle_action,distinct_built_action_candidate");
            foreach (var pose in new[] { "portrait", "idle", "action" })
                csv.Append("," + pose + "_binding," + pose + "_resource_key," + pose + "_texture," + pose + "_source_kind," + pose + "_render_status," + pose + "_pixel_sha256," + pose + "_error");
            csv.AppendLine();
            foreach (var row in rows)
            {
                var fields = new List<string> { row.RosterId.ToString(CultureInfo.InvariantCulture), row.StableId, row.GameEntityId,
                    row.Name, row.Rank, row.Race, row.Role, row.Weapon, row.MetadataStatus, string.Join("; ", row.QuarantineReasons),
                    row.Sheet, row.SheetCell.ToString(CultureInfo.InvariantCulture), row.SameIdleActionPixelsOrSource.ToString(), row.DistinctBuiltActionCandidate.ToString() };
                foreach (var pose in new[] { row.Portrait, row.Idle, row.Action })
                    fields.AddRange(new[] { pose.BindingAuthority, pose.ResourceKey, pose.TextureName, pose.SourceKind, pose.RenderStatus, pose.PixelSha256, pose.Error });
                csv.AppendLine(string.Join(",", fields.Select(value => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"")));
            }
            WriteNew118(Path.Combine(_output, "hero_art118.csv"), Utf8118.GetBytes(csv.ToString()));
            // Publish the final completion report only after the CSV succeeds.
            // FileMode.CreateNew failures must never leave a completed JSON claim.
            WriteNew118(Path.Combine(_output, "hero_art118.json"), Utf8118.GetBytes(JsonConvert.SerializeObject(report, Formatting.Indented)));
        }

        static void WriteNew118(string path, byte[] bytes)
        {
            RejectReparse118(path);
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None)) file.Write(bytes, 0, bytes.Length);
        }
        static string Hash118(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        static bool Same118(string a, string b) => string.Equals(a?.TrimEnd('\\', '/'), b?.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        static bool Within118(string path, string directory) => Same118(path, directory) ||
            path.StartsWith(directory.TrimEnd('\\', '/') + "\\", StringComparison.OrdinalIgnoreCase);
        static bool Overlaps118(string a, string b) => Within118(a, b) || Within118(b, a);
        static void RejectReparse118(string path)
        {
            for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
                if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Art evidence paths cannot contain links, junctions or reparse points.");
        }

        // Offscreen Unity UI capture. Original imported textures are never read,
        // rewritten, made readable, reimported or recropped by this helper.
        public sealed class RenderSurface118 : IDisposable
        {
            readonly GameObject _root;
            readonly Camera _camera;
            readonly Canvas _canvas;
            readonly RenderTexture _target;
            readonly int _width, _height, _layer;
            public RenderSurface118(int width, int height, int layer)
            {
                _width = width; _height = height; _layer = layer;
                _root = new GameObject("Read-only art contact render 118");
                var cameraObject = new GameObject("Art capture camera 118", typeof(Camera));
                cameraObject.transform.SetParent(_root.transform, false);
                _camera = cameraObject.GetComponent<Camera>();
                _camera.enabled = false; _camera.orthographic = true; _camera.orthographicSize = height / 2f;
                _camera.aspect = (float)width / height;
                _camera.nearClipPlane = .1f; _camera.farClipPlane = 10f;
                _camera.clearFlags = CameraClearFlags.SolidColor; _camera.backgroundColor = Color.clear;
                _camera.cullingMask = 1 << layer; _camera.allowHDR = false; _camera.allowMSAA = false;
                _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                _target.Create(); _camera.targetTexture = _target;
                var canvasObject = new GameObject("Art capture canvas 118", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(_root.transform, false); canvasObject.layer = layer;
                _canvas = canvasObject.GetComponent<Canvas>(); _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                _canvas.worldCamera = _camera; _canvas.planeDistance = 1f; _canvas.pixelPerfect = true;
            }
            public void Clear118()
            {
                foreach (Transform child in _canvas.transform) { child.gameObject.SetActive(false); UnityEngine.Object.Destroy(child.gameObject); }
            }
            public void Panel118(float x, float y, float width, float height, Color color)
            {
                var image = Element118("Card", x, y, width, height).gameObject.AddComponent<Image>();
                image.color = color; image.raycastTarget = false;
            }
            public void Picture118(Sprite sprite, float x, float y, float width, float height)
            {
                if (sprite == null) return;
                var image = Element118("Existing bound sprite", x, y, width, height).gameObject.AddComponent<Image>();
                image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true;
                image.useSpriteMesh = true; image.color = Color.white; image.raycastTarget = false;
            }
            public void Label118(string value, float x, float y, float width, float height, int size, Color color)
            {
                var text = Element118("QA label", x, y, width, height).gameObject.AddComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // existing M1PremiumUi built-in font
                text.text = value; text.fontSize = size; text.color = color; text.alignment = TextAnchor.UpperLeft;
                text.supportRichText = false; text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
            }
            RectTransform Element118(string name, float x, float y, float width, float height)
            {
                var owner = new GameObject(name, typeof(RectTransform)); owner.layer = _layer;
                var rect = owner.GetComponent<RectTransform>(); rect.SetParent(_canvas.transform, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
                return rect;
            }
            public Texture2D Read118()
            {
                var previous = RenderTexture.active;
                Texture2D pixels = null;
                try
                {
                    Canvas.ForceUpdateCanvases(); _camera.Render(); RenderTexture.active = _target;
                    pixels = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
                    pixels.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false); pixels.Apply(false, false);
                    return pixels;
                }
                catch { if (pixels != null) UnityEngine.Object.Destroy(pixels); throw; }
                finally { RenderTexture.active = previous; }
            }
            public Pose118 Probe118(Sprite sprite)
            {
                Clear118(); Picture118(sprite, 4, 4, _width - 8, _height - 8);
                var texture = Read118();
                try
                {
                    var pixels = texture.GetPixels32(); var bytes = new byte[pixels.Length * 4]; int visible = 0;
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        if (pixels[i].a >= 16) visible++;
                        bytes[i * 4] = pixels[i].r; bytes[i * 4 + 1] = pixels[i].g;
                        bytes[i * 4 + 2] = pixels[i].b; bytes[i * 4 + 3] = pixels[i].a;
                    }
                    return new Pose118 { VisibleProbePixels = visible, PixelSha256 = Hash118(bytes),
                        RenderStatus = visible > 4 ? "RENDERED_VISIBLE" : "BUILT_SPRITE_RENDERED_EMPTY" };
                }
                finally { UnityEngine.Object.Destroy(texture); }
            }
            public void Dispose()
            {
                _root.SetActive(false); _camera.targetTexture = null;
                _target.Release(); UnityEngine.Object.Destroy(_target); UnityEngine.Object.Destroy(_root);
            }
        }
    }
}
