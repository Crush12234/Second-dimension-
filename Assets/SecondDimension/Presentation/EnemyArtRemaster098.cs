using System;
using UnityEngine;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// One exact base-family repair with ten material variants. Other 690 variants and archived source pixels
    /// remain on the raw EnemyArt700 path. The pair shares one Resources texture,
    /// so no individual pose lease may destroy its sibling's texture.
    /// </summary>
    public static class EnemyArtRemaster098
    {
        public const string BaseId098 = "ENEMY_REC_021";
        public const string VariantId098 = "ENEMY_REC_021_VAR_03";
        public const string ResourcePath098 = "SecondDimension/Art/EnemyRemaster098/ENEMY_REC_021_VAR_03_PAIR_098";
        public const int SplitX098 = 709;
        public const int MaximumResidentSprites098 = 2;
        static HeroRemasterAtlas093.Pair093 _pair098;
        static Texture2D _texture098;
        static int _idleOwners098, _actionOwners098;
        static bool _retirePending098, _resetCounterPending098;

        public static long ResourceLoadCount098 { get; private set; }
        public static int ResidentSpriteCount098 => _pair098 == null ? 0 : MaximumResidentSprites098;
        public static int OutstandingLeaseCount098 => _idleOwners098 + _actionOwners098;
        public static int PinnedSpriteCount098 => (_idleOwners098 > 0 ? 1 : 0) + (_actionOwners098 > 0 ? 1 : 0);
        public static int RetiredSpriteCount098 => _retirePending098 ? ResidentSpriteCount098 : 0;
        public static bool Handles098(string baseId, string variantId) =>
            EnemyRemasterTheme098.TryResolve098(baseId, variantId, out _);

        public static bool TrySourcePath098(Sprite sprite, out string path)
        {
            path = string.Empty;
            if (_pair098 == null || sprite == null) return false;
            if (ReferenceEquals(sprite, _pair098.Idle)) path = ResourcePath098 + "#IDLE";
            else if (ReferenceEquals(sprite, _pair098.Action)) path = ResourcePath098 + "#ACTION";
            return path.Length > 0;
        }

        public static bool TryLoad098(EnemyArt700Pose090 pose, bool acquireLease,
            out Sprite sprite, out EnemyArt700SpriteLease090 lease, out string error)
        {
            sprite = null; lease = null; error = string.Empty;
            if ((int)pose < (int)EnemyArt700Pose090.Idle || (int)pose > (int)EnemyArt700Pose090.Portrait)
            { error = "Enemy remaster pose is invalid."; return false; }
            if (!EnsurePair098(out error)) return false;
            var action = pose == EnemyArt700Pose090.Attack;
            // Portrait intentionally shows this same corrected full-body idle,
            // not the original cropped portrait or an unrelated second identity.
            sprite = action ? _pair098.Action : _pair098.Idle;
            if (acquireLease)
            {
                if (action) _actionOwners098++; else _idleOwners098++;
                lease = new EnemyArt700SpriteLease090(sprite,
                    ResourcePath098 + (action ? "#ACTION" : "#IDLE"),
                    () => Release098(action));
            }
            return sprite != null;
        }

        static bool EnsurePair098(out string error)
        {
            error = string.Empty;
            if (_pair098 != null && _pair098.Idle != null && _pair098.Action != null) return true;
            if (_pair098 != null || OutstandingLeaseCount098 != 0)
            { error = "Enemy remaster retained an invalid live pair."; return false; }
            var texture = Resources.Load<Texture2D>(ResourcePath098);
            if (texture == null)
            { error = "Reviewed enemy remaster resource is missing: " + ResourcePath098; return false; }
            if (!texture.isReadable || texture.width != 1536 || texture.height != 1024)
            { error = "Reviewed enemy remaster must be readable 1536x1024 for its one-time alpha framing."; return false; }
            var pair = HeroRemasterAtlas093.BuildPair093(texture,
                new Rect(0, 0, SplitX098, 1024), new Rect(SplitX098, 0, 1536 - SplitX098, 1024),
                discardCpuPixels: true);
            if (pair == null)
            { error = "Reviewed enemy remaster has missing, opaque or clipped pose cells."; return false; }
            _texture098 = texture; _pair098 = pair; ResourceLoadCount098++;
            return true;
        }

        static void Release098(bool action)
        {
            if (action) _actionOwners098 = Math.Max(0, _actionOwners098 - 1);
            else _idleOwners098 = Math.Max(0, _idleOwners098 - 1);
            if (_retirePending098 && OutstandingLeaseCount098 == 0) RetirePair098();
        }

        public static void Reset098(bool resetReadCounter = false)
        {
            _retirePending098 = true;
            _resetCounterPending098 |= resetReadCounter;
            if (OutstandingLeaseCount098 == 0) RetirePair098();
        }

        static void RetirePair098()
        {
            if (OutstandingLeaseCount098 != 0) return;
            if (_pair098 != null)
            {
                M1SilhouetteFraming091.ReleaseResourceFrame091(_pair098.IdleSource);
                M1SilhouetteFraming091.ReleaseResourceFrame091(_pair098.ActionSource);
                HeroRemasterAtlas093.RetireSources093(_pair098);
                _pair098 = null;
            }
            if (_texture098 != null) Resources.UnloadAsset(_texture098);
            _texture098 = null;
            _retirePending098 = false;
            if (_resetCounterPending098) ResourceLoadCount098 = 0;
            _resetCounterPending098 = false;
        }
    }
}
