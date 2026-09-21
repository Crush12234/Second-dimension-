using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SecondDimension.Presentation
{
    // Two exact-identity repairs and explicitly pooled procedural portraits.
    // The raw PNGs are packaged in StreamingAssets alongside explicit provenance.
    public static class HeroArtRepair163
    {
        public const string Root = "SecondDimension/HeroArtRepair163/";
        public const string KaelResourceKey = Root + "CANON_KAEL_STANDEE163";
        public const string FreyaActionResourceKey = Root + "HERO_REC_287_ACTION163";
        public const string GoblinPriestResourceKey = Root + "PROCEDURAL_GOBLIN_PRIEST_PORTRAIT163";
        public const string ProceduralAtlasResourceKey = Root + "PROCEDURAL_PORTRAITS_ATLAS163";
        private static readonly Dictionary<string, Sprite> Cache =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite KaelStandee() => Load(KaelResourceKey);

        public static bool TryProceduralPortrait(string race, string role, out Sprite sprite, out string key)
        {
            sprite = null; key = string.Empty;
            if (role == "RESTORATION" || role == "HEALER") role = "PRIEST";
            var identity = race + "/" + role;
            int slot;
            switch (identity)
            {
                case "DARK_ELF/RANGER": slot=0; break;
                case "DARK_ELF/ROGUE": slot=1; break;
                case "DOG_TRIBE/PRIEST": slot=2; break;
                case "GOBLIN/WARRIOR": slot=3; break;
                case "HUMAN/PRIEST": slot=4; break;
                case "HUMAN/WARRIOR": slot=5; break;
                case "HUMAN/ROGUE": slot=6; break;
                default: return false;
            }
            key = ProceduralAtlasResourceKey + "#" + identity;
            if (Cache.TryGetValue(key,out sprite) && sprite != null) return true;
            var atlas=Load(ProceduralAtlasResourceKey);
            if(atlas==null)return false;
            var texture=atlas.texture; int col=slot%2,row=slot/2;
            // Pixel bounds are derived independently: the preserved original is887x1774.
            // Four-pixel inner gutters avoid neighboring cells and the painted divider.
            int left=(texture.width*col)/2+4,right=(texture.width*(col+1))/2-4;
            int top=(texture.height*row)/4+4,bottom=(texture.height*(row+1))/4-4;
            sprite=Sprite.Create(texture,new Rect(left,texture.height-bottom,right-left,bottom-top),
                new Vector2(.5f,.5f),100f,0,SpriteMeshType.FullRect);
            sprite.name="PROCEDURAL_"+race+"_"+role+"_163";Cache[key]=sprite;return true;
        }

        public static bool TryFreyaAction(string stableId, out Sprite sprite, out string key)
        {
            sprite = null;
            key = string.Empty;
            if (!StringComparer.Ordinal.Equals(stableId, "HERO_REC_287")) return false;
            sprite = Load(FreyaActionResourceKey);
            if (sprite == null) return false;
            key = FreyaActionResourceKey;
            return true;
        }

        public static Sprite Load(string key)
        {
            // Never convert arbitrary resource keys into filesystem paths.
            if (key != KaelResourceKey && key != FreyaActionResourceKey && key != GoblinPriestResourceKey &&
                key != ProceduralAtlasResourceKey) return null;
            if (Cache.TryGetValue(key, out var cached) && cached != null) return cached;
            Texture2D texture = null;
            Sprite raw = null;
            try
            {
                var file = key.Substring(Root.Length) + ".png";
                var path = Path.Combine(Application.streamingAssetsPath,
                    "SecondDimension", "HeroArtRepair163", file);
                var info = new FileInfo(path);
                if (!info.Exists || info.Length < 32 || info.Length > 12 * 1024 * 1024)
                    return null;
                var bytes = File.ReadAllBytes(path);
                if (bytes[0] != 137 || bytes[1] != 80 || bytes[2] != 78 || bytes[3] != 71)
                    return null;
                // Check dimensions before decoding a bounded local project asset.
                int width = ReadDimension(bytes, 16), height = ReadDimension(bytes, 20);
                if (width < 256 || height < 256 || width > 2048 || height > 2048)
                    return null;
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { name = file, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!ImageConversion.LoadImage(texture, bytes, false) ||
                    texture.width != width || texture.height != height)
                    throw new InvalidDataException("Invalid identity repair PNG.");
                raw = Sprite.Create(texture, new Rect(0, 0, width, height),
                    new Vector2(.5f, .035f), 100f, 0, SpriteMeshType.FullRect);
                raw.name = file;
                var sprite = M1SilhouetteFraming091.FrameResourceSprite091(raw);
                if (sprite == null) throw new InvalidDataException("Missing visible identity art.");
                texture.Apply(false, true);
                Cache[key] = sprite;
                return sprite;
            }
            catch (Exception error)
            {
                if (raw != null) UnityEngine.Object.Destroy(raw);
                if (texture != null) UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("Hero art repair163 unavailable: " + error.Message);
                return null;
            }
        }

        private static int ReadDimension(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) |
            (bytes[offset + 2] << 8) | bytes[offset + 3];
    }
}
