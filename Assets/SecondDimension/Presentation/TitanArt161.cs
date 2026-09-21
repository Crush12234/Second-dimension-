using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SecondDimension.Presentation
{
    // Exact, presentation-only identity bindings. PNGs remain original files;
    // generated atlases are sliced with Sprite rectangles, never rewritten.
    public static class TitanArt161
    {
        public const string ContentDirectory = "SecondDimension/TitanArt161";
        public const int ExpectedPoseCount = 75;
        static readonly string[] Families = { "ENEMY_REC_044", "ENEMY_REC_034", "ENEMY_REC_043",
            "ENEMY_REC_042", "ENEMY_REC_056", "ENEMY_REC_031", "ENEMY_REC_033", "ENEMY_REC_060",
            "ENEMY_REC_003", "ENEMY_REC_039", "ENEMY_REC_069", "ENEMY_REC_070" };
        static readonly Dictionary<string, Sprite[]> Sheets = new Dictionary<string, Sprite[]>(StringComparer.Ordinal);
        static readonly HashSet<string> Failed = new HashSet<string>(StringComparer.Ordinal);
        static readonly List<Sprite> RawSprites = new List<Sprite>();
        static readonly List<Texture2D> Textures = new List<Texture2D>();

        public static bool IsHero(string id) => Slot(id, "HERO_TITAN_", 13) > 0;
        public static bool IsBoss(string id) => BossSlot(id) > 0;
        public static bool IsMember(string id) => IsHero(id) || IsBoss(id);
        public static string HeroIdentity(string id, string authority) => IsHero(id) ? id : IsHero(authority) ? authority : null;
        public static Sprite BossPortrait(string id) => Resolve(id, 2, true, out _);
        public static Sprite BossPose(string id, bool attack) => Resolve(id, attack ? 1 : 0, true, out _);
        public static Sprite HeroPortrait(string id) => Resolve(id, 2, false, out _);
        public static Sprite HeroPose(string id, bool attack) => Resolve(id, attack ? 1 : 0, false, out _);
        public static bool TryHeroPortrait(string id, out Sprite sprite, out string path)
        { sprite = Resolve(id, 2, false, out path); return sprite != null; }
        public static bool TryHeroPose(string id, bool attack, out Sprite sprite, out string path)
        { sprite = Resolve(id, attack ? 1 : 0, false, out path); return sprite != null; }
        public static bool TryResolvePose(string id, string poseId, out Sprite sprite, out string path)
        {
            var attack = poseId == BattleArtPoseDirector011.Anticipation ||
                poseId == BattleArtPoseDirector011.ActionPrimary || poseId == BattleArtPoseDirector011.RolePrimary;
            sprite = Resolve(id, attack ? 1 : 0, IsBoss(id), out path);
            return sprite != null;
        }

        static int Slot(string id, string prefix, int maximum)
        {
            if (id == null || id.Length != prefix.Length + 3 || !id.StartsWith(prefix, StringComparison.Ordinal)) return 0;
            var a = id[prefix.Length] - '0'; var b = id[prefix.Length + 1] - '0'; var c = id[prefix.Length + 2] - '0';
            if (a < 0 || a > 9 || b < 0 || b > 9 || c < 0 || c > 9) return 0;
            var value = a * 100 + b * 10 + c;
            return value >= 1 && value <= maximum ? value : 0;
        }
        static int BossSlot(string id)
        {
            if (id != null && id.EndsWith("_BOSS", StringComparison.Ordinal)) id = id.Substring(0, id.Length - 5);
            return Slot(id, "TITAN_TRIAL_", 12);
        }
        static Sprite Resolve(string id, int pose, bool boss, out string resourcePath)
        {
            resourcePath = string.Empty;
            var slot = boss ? BossSlot(id) : Slot(id, "HERO_TITAN_", 13);
            if (slot == 0) return null;
            var atlas = !boss && slot != 1;
            var poseName = pose == 0 ? "IDLE" : pose == 1 ? "ATTACK" : "PORTRAIT";
            var file = boss ? Families[slot - 1] + "_" + poseName + ".png" :
                atlas ? id + "_ATLAS161.png" : "HERO_TITAN_001_" + poseName + "_R46.png";
            resourcePath = ContentDirectory + "/" + file + "#" + poseName;
            if (Failed.Contains(file)) return null;
            if (!Sheets.TryGetValue(file, out var sprites)) sprites = Load(file, atlas, pose == 2);
            return sprites == null ? null : sprites[atlas ? pose : 0];
        }

        static Sprite[] Load(string file, bool atlas, bool portrait)
        {
            Texture2D texture = null;
            var raw = new List<Sprite>();
            try
            {
                // file is constructed solely from fixed identity and pose tables.
                var full = Path.Combine(Application.streamingAssetsPath, "SecondDimension", "TitanArt161", file);
                var info = new FileInfo(full);
                if (!info.Exists || info.Length < 32 || info.Length > 24 * 1024 * 1024)
                    throw new InvalidDataException("Missing or oversized image: " + file);
                var bytes = File.ReadAllBytes(full);
                if (bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                    throw new InvalidDataException("Not a PNG: " + file);
                var width = PngInt(bytes, 16); var height = PngInt(bytes, 20);
                if (width < 128 || height < 128 || width > 4096 || height > 4096 ||
                    (atlas && (width != 1254 || height != 1254)))
                    throw new InvalidDataException("Invalid image dimensions: " + file);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { name = file, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!ImageConversion.LoadImage(texture, bytes, false) || texture.width != width || texture.height != height)
                    throw new InvalidDataException("PNG decode failed: " + file);
                var sprites = new Sprite[atlas ? 3 : 1];
                for (var i = 0; i < sprites.Length; i++)
                {
                    // Unity's rectangle origin is bottom-left. The supplied atlas
                    // uses top-left idle, top-right action, bottom-left portrait.
                    var half = width / 2;
                    // The reviewed originals have three small boundary deviations.
                    // Keep their pixels intact and use measured empty seams rather
                    // than clipping toes or drawing Eidran's cape in his idle pose.
                    var rowFromTop = (file.StartsWith("HERO_TITAN_004_", StringComparison.Ordinal) ||
                        file.StartsWith("HERO_TITAN_009_", StringComparison.Ordinal)) ? 631 : half;
                    var column = file.StartsWith("HERO_TITAN_013_", StringComparison.Ordinal) ? 600 : half;
                    var rect = !atlas ? new Rect(0, 0, width, height) :
                        i == 0 ? new Rect(0, height - rowFromTop, column, rowFromTop) :
                        i == 1 ? new Rect(column, height - rowFromTop, width - column, rowFromTop) :
                        new Rect(0, 0, half, height - rowFromTop);
                    var isPortrait = atlas ? i == 2 : portrait;
                    var sprite = Sprite.Create(texture, rect, new Vector2(.5f, isPortrait ? .5f : .035f),
                        100f, 0, SpriteMeshType.FullRect);
                    sprite.name = file + "_" + (atlas ? i.ToString() : "0");
                    raw.Add(sprite);
                    sprites[i] = isPortrait ? sprite : M1SilhouetteFraming091.FrameResourceSprite091(sprite);
                }
                // Measure every pose before releasing CPU pixels; subsequent rig
                // framing reuses the cached rectangles, without GPU readbacks.
                texture.Apply(false, true);
                Textures.Add(texture); RawSprites.AddRange(raw); Sheets.Add(file, sprites);
                return sprites;
            }
            catch (Exception error)
            {
                foreach (var sprite in raw) { M1SilhouetteFraming091.ReleaseResourceFrame091(sprite); Destroy(sprite); }
                Destroy(texture); Failed.Add(file);
                Debug.LogWarning("Titan art unavailable: " + error.Message);
                return null;
            }
        }
        static int PngInt(byte[] bytes, int offset) => checked((int)((uint)bytes[offset] << 24 |
            (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3]));

        public static bool ValidateAll161(out string detail)
        {
            var missing = new List<string>(); var count = 0;
            for (var slot = 1; slot <= 13; slot++)
            for (var pose = 0; pose < 3; pose++)
            {
                var id = "HERO_TITAN_" + slot.ToString("D3");
                if (Resolve(id, pose, false, out var path) == null) missing.Add(path); else count++;
            }
            for (var slot = 1; slot <= 12; slot++)
            for (var pose = 0; pose < 3; pose++)
            {
                var id = "TITAN_TRIAL_" + slot.ToString("D3");
                if (Resolve(id, pose, true, out var path) == null) missing.Add(path); else count++;
            }
            detail = count + "/" + ExpectedPoseCount + " exact Titan/hero images loaded" +
                (missing.Count == 0 ? "." : ": " + string.Join(", ", missing));
            return missing.Count == 0 && count == ExpectedPoseCount;
        }
        static void Destroy(UnityEngine.Object value)
        { if (value == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(value); else UnityEngine.Object.DestroyImmediate(value); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset161()
        {
            foreach (var raw in RawSprites) { M1SilhouetteFraming091.ReleaseResourceFrame091(raw); Destroy(raw); }
            foreach (var texture in Textures) Destroy(texture);
            RawSprites.Clear(); Textures.Clear(); Sheets.Clear(); Failed.Clear();
        }
    }
}
