using System;
using System.Globalization;
using SecondDimension.Presentation.FirstHour071;

namespace SecondDimension.Presentation
{
    public enum Battle3DArtCameraRecipe071
    {
        None,
        MediumThreeQuarterTrackIn,
        CloseProfileLateralTrack,
        WideTacticalOrbit,
        OverShoulderReactionSnap
    }

    public enum Battle3DArtMotionRecipe071
    {
        None,
        SetAdvancePrimaryRecover,
        FeintChainSecondaryRecover,
        CommitExpandFullRecover,
        ReadReactCounterRecover
    }

    public enum Battle3DArtVfxRecipe071
    {
        None,
        FocusedPrimary,
        SecondaryAccent,
        ExpandingPattern,
        ReactionBurst
    }

    /// <summary>
    /// Broad visual grammar used by the perspective battle stage. This is not an
    /// action kind and cannot change targets, costs, damage, healing, or timing in
    /// the deterministic combat core.
    /// </summary>
    public enum Battle3DArtSemanticFamily071
    {
        None,
        WeaponMelee,
        HeavyWeapon,
        ReachWeapon,
        ProjectileWeapon,
        AgileWeapon,
        GuardSupport,
        UnarmedWeapon,
        ArcaneImplement,
        ToolEngineering,
        HybridRelic,
        MysticElemental,
        Restoration,
        Warding,
        TacticalAssault,
        TacticalSupport,
        TacticalControl
    }

    /// <summary>
    /// One stable visual style per first-hour Art tree. Keeping this separate from
    /// the four node recipes prevents Sword N01 and Flame N01 (for example) from
    /// looking like the same performance with a different label.
    /// </summary>
    public enum Battle3DArtSemanticStyle071
    {
        None,
        Sword,
        GreatWeapon,
        Axe,
        SpearPolearm,
        Bow,
        Dagger,
        Shield,
        Gauntlet,
        Staff,
        Focus,
        Engineering,
        HybridRelic,
        Flame,
        Frost,
        Storm,
        Earth,
        Aether,
        Shadow,
        Restoration,
        Warding,
        Guardian,
        Breaker,
        Duelist,
        Scout,
        Saboteur,
        FieldMedic,
        Commander,
        Formation,
        Provocation,
        Resonance
    }

    public enum Battle3DArtTraceRecipe071
    {
        None,
        EdgeArc,
        WeightedShock,
        ReachLine,
        ProjectileFlight,
        TwinTrail,
        GuardPlate,
        ImpactRing,
        ArcaneOrbit,
        ToolDiagram,
        RelicConvergence,
        ElementalSpiral,
        HealingBloom,
        WardDome,
        TacticalLine,
        TacticalMarker,
        TacticalWave
    }

    /// <summary>
    /// Immutable presentation-only projection of a First Hour 071 Art recipe. The
    /// identifiers describe procedural choreography. They are deliberately not
    /// AnimationClip asset references and expose no combat or save service.
    /// </summary>
    public sealed class Battle3DArtChoreography071
    {
        private Battle3DArtChoreography071(ResolvedFirstHourArtPresentation071 source)
        {
            StableArtId = source.StableArtId;
            TreeId = source.TreeId;
            NodeIndex = source.NodeIndex;
            DisplayName = source.DisplayName;
            ClipRecipeId = source.ClipRecipeId;
            CameraSignature = source.CameraSignature;
            VfxSignature = source.VfxSignature;
            SfxSignature = source.SfxSignature;
            MotionSignature = source.MotionSignature;
            DurationMilliseconds = source.DurationMilliseconds;
            ImpactMilliseconds = source.ImpactMilliseconds;
            CameraRecipe = ParseCamera(source.CameraSignature);
            MotionRecipe = ParseMotion(source.MotionSignature);
            VfxRecipe = ParseVfx(source.VfxSignature);
            CameraVisualSeed = StableSeed(source.CameraSignature);
            MotionVisualSeed = StableSeed(source.MotionSignature);
            VfxVisualSeed = StableSeed(source.VfxSignature);
            StableVisualSeed = StableSeed(source.CameraSignature + "|" + source.VfxSignature + "|" +
                                          source.MotionSignature);
            SemanticStyle = ResolveSemanticStyle(source.TreeId);
            SemanticFamily = ResolveSemanticFamily(SemanticStyle);
            TraceRecipe = ResolveTraceRecipe(SemanticStyle);
            TreeOrdinal = ResolveTreeOrdinal(SemanticStyle);
            PerformanceCallout = ResolvePerformanceCallout(SemanticStyle);
            EffectSpriteId = ResolveEffectSpriteId(SemanticFamily);
            SemanticPaletteHue01 = ResolvePaletteHue01(SemanticStyle, source.NodeIndex);
        }

        public string StableArtId { get; }
        public string TreeId { get; }
        public int NodeIndex { get; }
        public string DisplayName { get; }
        public string ClipRecipeId { get; }
        public string CameraSignature { get; }
        public string VfxSignature { get; }
        public string SfxSignature { get; }
        public string MotionSignature { get; }
        public int DurationMilliseconds { get; }
        public int ImpactMilliseconds { get; }
        public Battle3DArtCameraRecipe071 CameraRecipe { get; }
        public Battle3DArtMotionRecipe071 MotionRecipe { get; }
        public Battle3DArtVfxRecipe071 VfxRecipe { get; }
        public uint CameraVisualSeed { get; }
        public uint MotionVisualSeed { get; }
        public uint VfxVisualSeed { get; }
        public uint StableVisualSeed { get; }
        public Battle3DArtSemanticFamily071 SemanticFamily { get; }
        public Battle3DArtSemanticStyle071 SemanticStyle { get; }
        public Battle3DArtTraceRecipe071 TraceRecipe { get; }
        public int TreeOrdinal { get; }
        public string PerformanceCallout { get; }
        public string EffectSpriteId { get; }
        public float SemanticPaletteHue01 { get; }

        public float DurationSeconds => DurationMilliseconds / 1000f;
        public float ImpactSeconds => ImpactMilliseconds / 1000f;
        public float CameraMoveSeconds => Clamp(DurationSeconds * 0.24f, 0.14f, 0.30f);
        public float WindupSeconds => Clamp(ImpactSeconds * 0.30f, 0.10f, 0.28f);
        public float RecoveryAccentSeconds => Clamp((DurationSeconds - ImpactSeconds) * 0.28f, 0.09f, 0.24f);
        public float ImpactProgress => DurationMilliseconds <= 0
            ? 0f
            : (float)ImpactMilliseconds / DurationMilliseconds;
        public float MotionMagnitude => 0.18f + (MotionVisualSeed & 0xffffu) / 65535f * 0.20f;
        public float MotionTiltDegrees => 3f + ((MotionVisualSeed >> 16) & 0xffu) / 255f * 7f;
        public float CameraLateralOffset =>
            ((((CameraVisualSeed >> 8) & 0xffffu) / 65535f) * 2f - 1f) * 0.64f;
        public float VfxHue01 => (VfxVisualSeed & 0x00ffffffu) / 16777215f;
        public int SemanticVisualOrdinal => TreeOrdinal * 4 + Math.Max(0, NodeIndex - 1);
        public int SemanticTracePointCount => 9 + (SemanticVisualOrdinal % 8);
        public int SemanticTraceLayerCount => 1 + (SemanticVisualOrdinal % 3);
        public int SemanticBurstCount => 13 + SemanticVisualOrdinal % 18 + NodeIndex * 3;
        public int SemanticProjectileCount => SemanticFamily == Battle3DArtSemanticFamily071.ProjectileWeapon
            ? NodeIndex == 3 ? 5 : NodeIndex == 4 ? 3 : NodeIndex
            : SemanticFamily == Battle3DArtSemanticFamily071.MysticElemental ||
              SemanticFamily == Battle3DArtSemanticFamily071.ArcaneImplement
                ? 1 + (SemanticVisualOrdinal % 3)
                : 0;
        public float SemanticTraceRadius => 0.64f + (TreeOrdinal % 6) * 0.085f + NodeIndex * 0.07f;
        public float SemanticTraceRotationDegrees =>
            ((TreeOrdinal * 29 + NodeIndex * 47) % 360) - 180f;
        public float SemanticForwardBias => ResolveForwardBias(SemanticFamily) +
                                            (TreeOrdinal % 4) * 0.018f + NodeIndex * 0.012f;
        public float SemanticLateralBias => ResolveLateralBias(SemanticFamily) +
                                            ((TreeOrdinal + NodeIndex) % 5) * 0.014f;
        public float SemanticLiftBias => ResolveLiftBias(SemanticFamily) +
                                         ((TreeOrdinal * 2 + NodeIndex) % 4) * 0.012f;
        public float SemanticPoseTurnDegrees =>
            4f + (TreeOrdinal % 7) * 2.25f + NodeIndex * 1.5f;
        public float SemanticPaletteSaturation =>
            SemanticFamily == Battle3DArtSemanticFamily071.Restoration ? 0.48f :
            SemanticFamily == Battle3DArtSemanticFamily071.Warding ||
            SemanticFamily == Battle3DArtSemanticFamily071.GuardSupport ? 0.56f : 0.72f;
        public float SemanticPaletteValue => NodeIndex == 4 ? 1f : 0.90f + NodeIndex * 0.025f;
        public string SemanticPerformanceKey => SemanticStyle + "|" + SemanticFamily + "|" + TraceRecipe +
                                                "|N" + NodeIndex.ToString("00", CultureInfo.InvariantCulture) +
                                                "|V" + SemanticVisualOrdinal.ToString("000", CultureInfo.InvariantCulture);

        public bool UsesProceduralMotionRecipe => true;
        public bool ClaimsAuthoredAnimationClipAsset => false;
        public bool IsPresentationOnly => true;
        public bool ChangesCombatResolution => false;

        public string StableDescriptor => string.Join("|", new[]
        {
            StableArtId,
            TreeId,
            NodeIndex.ToString(CultureInfo.InvariantCulture),
            DisplayName,
            ClipRecipeId,
            CameraSignature,
            VfxSignature,
            SfxSignature,
            MotionSignature,
            DurationMilliseconds.ToString(CultureInfo.InvariantCulture),
            ImpactMilliseconds.ToString(CultureInfo.InvariantCulture),
            CameraRecipe.ToString(),
            MotionRecipe.ToString(),
            VfxRecipe.ToString(),
            SemanticFamily.ToString(),
            SemanticStyle.ToString(),
            TraceRecipe.ToString(),
            PerformanceCallout,
            EffectSpriteId,
            SemanticPerformanceKey,
            "PROCEDURAL_PRESENTATION_RECIPE",
            "NO_AUTHORED_ANIMATION_CLIP_CLAIM"
        });

        public static bool TryCreate(string stableArtId, out Battle3DArtChoreography071 choreography)
        {
            choreography = null;
            if (!FirstHourArtPresentationResolver071.TryResolve(stableArtId, out var resolved)) return false;
            choreography = new Battle3DArtChoreography071(resolved);
            return true;
        }

        private static Battle3DArtCameraRecipe071 ParseCamera(string value)
        {
            if (Contains(value, "MEDIUM_THREE_QUARTER_TRACK_IN"))
                return Battle3DArtCameraRecipe071.MediumThreeQuarterTrackIn;
            if (Contains(value, "CLOSE_PROFILE_LATERAL_TRACK"))
                return Battle3DArtCameraRecipe071.CloseProfileLateralTrack;
            if (Contains(value, "WIDE_TACTICAL_ORBIT"))
                return Battle3DArtCameraRecipe071.WideTacticalOrbit;
            if (Contains(value, "OVER_SHOULDER_REACTION_SNAP"))
                return Battle3DArtCameraRecipe071.OverShoulderReactionSnap;
            return Battle3DArtCameraRecipe071.None;
        }

        private static Battle3DArtMotionRecipe071 ParseMotion(string value)
        {
            if (Contains(value, "SET_ADVANCE_PRIMARY_RECOVER"))
                return Battle3DArtMotionRecipe071.SetAdvancePrimaryRecover;
            if (Contains(value, "FEINT_CHAIN_SECONDARY_RECOVER"))
                return Battle3DArtMotionRecipe071.FeintChainSecondaryRecover;
            if (Contains(value, "COMMIT_EXPAND_FULL_RECOVER"))
                return Battle3DArtMotionRecipe071.CommitExpandFullRecover;
            if (Contains(value, "READ_REACT_COUNTER_RECOVER"))
                return Battle3DArtMotionRecipe071.ReadReactCounterRecover;
            return Battle3DArtMotionRecipe071.None;
        }

        private static Battle3DArtVfxRecipe071 ParseVfx(string value)
        {
            if (Contains(value, "FOCUSED_PRIMARY")) return Battle3DArtVfxRecipe071.FocusedPrimary;
            if (Contains(value, "SECONDARY_ACCENT")) return Battle3DArtVfxRecipe071.SecondaryAccent;
            if (Contains(value, "EXPANDING_PATTERN")) return Battle3DArtVfxRecipe071.ExpandingPattern;
            if (Contains(value, "REACTION_BURST")) return Battle3DArtVfxRecipe071.ReactionBurst;
            return Battle3DArtVfxRecipe071.None;
        }

        private static bool Contains(string value, string token) =>
            !string.IsNullOrWhiteSpace(value) && value.IndexOf(token, StringComparison.Ordinal) >= 0;

        private static Battle3DArtSemanticStyle071 ResolveSemanticStyle(string treeId)
        {
            switch (treeId)
            {
                case "TREE_CA002_WPN_SWORD": return Battle3DArtSemanticStyle071.Sword;
                case "TREE_CA002_WPN_GREAT_WEAPON": return Battle3DArtSemanticStyle071.GreatWeapon;
                case "TREE_CA002_WPN_AXE": return Battle3DArtSemanticStyle071.Axe;
                case "TREE_CA002_WPN_SPEAR_POLEARM": return Battle3DArtSemanticStyle071.SpearPolearm;
                case "TREE_CA002_WPN_BOW": return Battle3DArtSemanticStyle071.Bow;
                case "TREE_CA002_WPN_DAGGER": return Battle3DArtSemanticStyle071.Dagger;
                case "TREE_CA002_WPN_SHIELD": return Battle3DArtSemanticStyle071.Shield;
                case "TREE_CA002_WPN_GAUNTLET": return Battle3DArtSemanticStyle071.Gauntlet;
                case "TREE_CA002_WPN_STAFF": return Battle3DArtSemanticStyle071.Staff;
                case "TREE_CA002_WPN_FOCUS": return Battle3DArtSemanticStyle071.Focus;
                case "TREE_CA002_WPN_ENGINEERING": return Battle3DArtSemanticStyle071.Engineering;
                case "TREE_CA002_WPN_HYBRID_RELIC": return Battle3DArtSemanticStyle071.HybridRelic;
                case "TREE_CA002_MYS_FLAME": return Battle3DArtSemanticStyle071.Flame;
                case "TREE_CA002_MYS_FROST": return Battle3DArtSemanticStyle071.Frost;
                case "TREE_CA002_MYS_STORM": return Battle3DArtSemanticStyle071.Storm;
                case "TREE_CA002_MYS_EARTH": return Battle3DArtSemanticStyle071.Earth;
                case "TREE_CA002_MYS_AETHER": return Battle3DArtSemanticStyle071.Aether;
                case "TREE_CA002_MYS_SHADOW": return Battle3DArtSemanticStyle071.Shadow;
                case "TREE_CA002_MYS_RESTORATION": return Battle3DArtSemanticStyle071.Restoration;
                case "TREE_CA002_MYS_WARDING": return Battle3DArtSemanticStyle071.Warding;
                case "TREE_CA002_ROLE_GUARDIAN": return Battle3DArtSemanticStyle071.Guardian;
                case "TREE_CA002_ROLE_BREAKER": return Battle3DArtSemanticStyle071.Breaker;
                case "TREE_CA002_ROLE_DUELIST": return Battle3DArtSemanticStyle071.Duelist;
                case "TREE_CA002_ROLE_SCOUT": return Battle3DArtSemanticStyle071.Scout;
                case "TREE_CA002_ROLE_SABOTEUR": return Battle3DArtSemanticStyle071.Saboteur;
                case "TREE_CA002_ROLE_FIELD_MEDIC": return Battle3DArtSemanticStyle071.FieldMedic;
                case "TREE_CA002_ROLE_COMMANDER": return Battle3DArtSemanticStyle071.Commander;
                case "TREE_CA002_ROLE_FORMATION": return Battle3DArtSemanticStyle071.Formation;
                case "TREE_CA002_ROLE_PROVOCATION": return Battle3DArtSemanticStyle071.Provocation;
                case "TREE_CA002_ROLE_RESONANCE": return Battle3DArtSemanticStyle071.Resonance;
                default: return Battle3DArtSemanticStyle071.None;
            }
        }

        private static Battle3DArtSemanticFamily071 ResolveSemanticFamily(Battle3DArtSemanticStyle071 style)
        {
            switch (style)
            {
                case Battle3DArtSemanticStyle071.Sword:
                case Battle3DArtSemanticStyle071.Axe:
                    return Battle3DArtSemanticFamily071.WeaponMelee;
                case Battle3DArtSemanticStyle071.GreatWeapon:
                    return Battle3DArtSemanticFamily071.HeavyWeapon;
                case Battle3DArtSemanticStyle071.SpearPolearm:
                    return Battle3DArtSemanticFamily071.ReachWeapon;
                case Battle3DArtSemanticStyle071.Bow:
                    return Battle3DArtSemanticFamily071.ProjectileWeapon;
                case Battle3DArtSemanticStyle071.Dagger:
                    return Battle3DArtSemanticFamily071.AgileWeapon;
                case Battle3DArtSemanticStyle071.Shield:
                    return Battle3DArtSemanticFamily071.GuardSupport;
                case Battle3DArtSemanticStyle071.Gauntlet:
                    return Battle3DArtSemanticFamily071.UnarmedWeapon;
                case Battle3DArtSemanticStyle071.Staff:
                case Battle3DArtSemanticStyle071.Focus:
                    return Battle3DArtSemanticFamily071.ArcaneImplement;
                case Battle3DArtSemanticStyle071.Engineering:
                    return Battle3DArtSemanticFamily071.ToolEngineering;
                case Battle3DArtSemanticStyle071.HybridRelic:
                    return Battle3DArtSemanticFamily071.HybridRelic;
                case Battle3DArtSemanticStyle071.Flame:
                case Battle3DArtSemanticStyle071.Frost:
                case Battle3DArtSemanticStyle071.Storm:
                case Battle3DArtSemanticStyle071.Earth:
                case Battle3DArtSemanticStyle071.Aether:
                case Battle3DArtSemanticStyle071.Shadow:
                    return Battle3DArtSemanticFamily071.MysticElemental;
                case Battle3DArtSemanticStyle071.Restoration:
                case Battle3DArtSemanticStyle071.FieldMedic:
                    return Battle3DArtSemanticFamily071.Restoration;
                case Battle3DArtSemanticStyle071.Warding:
                    return Battle3DArtSemanticFamily071.Warding;
                case Battle3DArtSemanticStyle071.Breaker:
                case Battle3DArtSemanticStyle071.Duelist:
                    return Battle3DArtSemanticFamily071.TacticalAssault;
                case Battle3DArtSemanticStyle071.Guardian:
                case Battle3DArtSemanticStyle071.Commander:
                case Battle3DArtSemanticStyle071.Formation:
                case Battle3DArtSemanticStyle071.Resonance:
                    return Battle3DArtSemanticFamily071.TacticalSupport;
                case Battle3DArtSemanticStyle071.Scout:
                case Battle3DArtSemanticStyle071.Saboteur:
                case Battle3DArtSemanticStyle071.Provocation:
                    return Battle3DArtSemanticFamily071.TacticalControl;
                default:
                    return Battle3DArtSemanticFamily071.None;
            }
        }

        private static Battle3DArtTraceRecipe071 ResolveTraceRecipe(Battle3DArtSemanticStyle071 style)
        {
            switch (style)
            {
                case Battle3DArtSemanticStyle071.Sword:
                case Battle3DArtSemanticStyle071.Axe:
                    return Battle3DArtTraceRecipe071.EdgeArc;
                case Battle3DArtSemanticStyle071.GreatWeapon:
                case Battle3DArtSemanticStyle071.Breaker:
                    return Battle3DArtTraceRecipe071.WeightedShock;
                case Battle3DArtSemanticStyle071.SpearPolearm:
                    return Battle3DArtTraceRecipe071.ReachLine;
                case Battle3DArtSemanticStyle071.Bow:
                    return Battle3DArtTraceRecipe071.ProjectileFlight;
                case Battle3DArtSemanticStyle071.Dagger:
                case Battle3DArtSemanticStyle071.Duelist:
                    return Battle3DArtTraceRecipe071.TwinTrail;
                case Battle3DArtSemanticStyle071.Shield:
                    return Battle3DArtTraceRecipe071.GuardPlate;
                case Battle3DArtSemanticStyle071.Gauntlet:
                    return Battle3DArtTraceRecipe071.ImpactRing;
                case Battle3DArtSemanticStyle071.Staff:
                case Battle3DArtSemanticStyle071.Focus:
                    return Battle3DArtTraceRecipe071.ArcaneOrbit;
                case Battle3DArtSemanticStyle071.Engineering:
                    return Battle3DArtTraceRecipe071.ToolDiagram;
                case Battle3DArtSemanticStyle071.HybridRelic:
                    return Battle3DArtTraceRecipe071.RelicConvergence;
                case Battle3DArtSemanticStyle071.Flame:
                case Battle3DArtSemanticStyle071.Frost:
                case Battle3DArtSemanticStyle071.Storm:
                case Battle3DArtSemanticStyle071.Earth:
                case Battle3DArtSemanticStyle071.Aether:
                case Battle3DArtSemanticStyle071.Shadow:
                    return Battle3DArtTraceRecipe071.ElementalSpiral;
                case Battle3DArtSemanticStyle071.Restoration:
                case Battle3DArtSemanticStyle071.FieldMedic:
                    return Battle3DArtTraceRecipe071.HealingBloom;
                case Battle3DArtSemanticStyle071.Warding:
                    return Battle3DArtTraceRecipe071.WardDome;
                case Battle3DArtSemanticStyle071.Guardian:
                case Battle3DArtSemanticStyle071.Formation:
                    return Battle3DArtTraceRecipe071.TacticalLine;
                case Battle3DArtSemanticStyle071.Scout:
                case Battle3DArtSemanticStyle071.Saboteur:
                    return Battle3DArtTraceRecipe071.TacticalMarker;
                case Battle3DArtSemanticStyle071.Commander:
                case Battle3DArtSemanticStyle071.Provocation:
                case Battle3DArtSemanticStyle071.Resonance:
                    return Battle3DArtTraceRecipe071.TacticalWave;
                default:
                    return Battle3DArtTraceRecipe071.None;
            }
        }

        private static int ResolveTreeOrdinal(Battle3DArtSemanticStyle071 style) =>
            style == Battle3DArtSemanticStyle071.None ? -1 : (int)style - 1;

        private static string ResolvePerformanceCallout(Battle3DArtSemanticStyle071 style)
        {
            switch (style)
            {
                case Battle3DArtSemanticStyle071.GreatWeapon: return "GREAT-WEAPON ART";
                case Battle3DArtSemanticStyle071.SpearPolearm: return "POLEARM ART";
                case Battle3DArtSemanticStyle071.HybridRelic: return "RELIC ART";
                case Battle3DArtSemanticStyle071.FieldMedic: return "FIELD-MEDIC ART";
                default: return style == Battle3DArtSemanticStyle071.None
                    ? "ART"
                    : style.ToString().ToUpperInvariant() + " ART";
            }
        }

        private static string ResolveEffectSpriteId(Battle3DArtSemanticFamily071 family)
        {
            switch (family)
            {
                case Battle3DArtSemanticFamily071.Restoration:
                    return "RESTORATION_BLOOM";
                case Battle3DArtSemanticFamily071.Warding:
                case Battle3DArtSemanticFamily071.GuardSupport:
                case Battle3DArtSemanticFamily071.TacticalSupport:
                    return "GUARD_IMPACT";
                case Battle3DArtSemanticFamily071.MysticElemental:
                case Battle3DArtSemanticFamily071.ArcaneImplement:
                case Battle3DArtSemanticFamily071.HybridRelic:
                case Battle3DArtSemanticFamily071.ToolEngineering:
                case Battle3DArtSemanticFamily071.TacticalControl:
                    return "MYSTIC_BURST";
                default:
                    return "WEAPON_ARC";
            }
        }

        private static float ResolvePaletteHue01(Battle3DArtSemanticStyle071 style, int nodeIndex)
        {
            float hue;
            switch (style)
            {
                case Battle3DArtSemanticStyle071.Sword: hue = 0.58f; break;
                case Battle3DArtSemanticStyle071.GreatWeapon: hue = 0.055f; break;
                case Battle3DArtSemanticStyle071.Axe: hue = 0.985f; break;
                case Battle3DArtSemanticStyle071.SpearPolearm: hue = 0.47f; break;
                case Battle3DArtSemanticStyle071.Bow: hue = 0.31f; break;
                case Battle3DArtSemanticStyle071.Dagger: hue = 0.77f; break;
                case Battle3DArtSemanticStyle071.Shield: hue = 0.56f; break;
                case Battle3DArtSemanticStyle071.Gauntlet: hue = 0.015f; break;
                case Battle3DArtSemanticStyle071.Staff: hue = 0.68f; break;
                case Battle3DArtSemanticStyle071.Focus: hue = 0.84f; break;
                case Battle3DArtSemanticStyle071.Engineering: hue = 0.105f; break;
                case Battle3DArtSemanticStyle071.HybridRelic: hue = 0.43f; break;
                case Battle3DArtSemanticStyle071.Flame: hue = 0.045f; break;
                case Battle3DArtSemanticStyle071.Frost: hue = 0.54f; break;
                case Battle3DArtSemanticStyle071.Storm: hue = 0.64f; break;
                case Battle3DArtSemanticStyle071.Earth: hue = 0.13f; break;
                case Battle3DArtSemanticStyle071.Aether: hue = 0.50f; break;
                case Battle3DArtSemanticStyle071.Shadow: hue = 0.75f; break;
                case Battle3DArtSemanticStyle071.Restoration: hue = 0.40f; break;
                case Battle3DArtSemanticStyle071.Warding: hue = 0.59f; break;
                case Battle3DArtSemanticStyle071.Guardian: hue = 0.57f; break;
                case Battle3DArtSemanticStyle071.Breaker: hue = 0.99f; break;
                case Battle3DArtSemanticStyle071.Duelist: hue = 0.92f; break;
                case Battle3DArtSemanticStyle071.Scout: hue = 0.32f; break;
                case Battle3DArtSemanticStyle071.Saboteur: hue = 0.25f; break;
                case Battle3DArtSemanticStyle071.FieldMedic: hue = 0.385f; break;
                case Battle3DArtSemanticStyle071.Commander: hue = 0.61f; break;
                case Battle3DArtSemanticStyle071.Formation: hue = 0.48f; break;
                case Battle3DArtSemanticStyle071.Provocation: hue = 0.075f; break;
                case Battle3DArtSemanticStyle071.Resonance: hue = 0.79f; break;
                default: hue = 0f; break;
            }
            hue += Math.Max(0, nodeIndex - 1) * 0.008f;
            return hue >= 1f ? hue - 1f : hue;
        }

        private static float ResolveForwardBias(Battle3DArtSemanticFamily071 family)
        {
            switch (family)
            {
                case Battle3DArtSemanticFamily071.HeavyWeapon: return 0.22f;
                case Battle3DArtSemanticFamily071.ReachWeapon: return 0.19f;
                case Battle3DArtSemanticFamily071.WeaponMelee:
                case Battle3DArtSemanticFamily071.AgileWeapon:
                case Battle3DArtSemanticFamily071.UnarmedWeapon:
                case Battle3DArtSemanticFamily071.TacticalAssault: return 0.15f;
                case Battle3DArtSemanticFamily071.ProjectileWeapon:
                case Battle3DArtSemanticFamily071.MysticElemental:
                case Battle3DArtSemanticFamily071.Restoration:
                case Battle3DArtSemanticFamily071.Warding: return -0.045f;
                default: return 0.035f;
            }
        }

        private static float ResolveLateralBias(Battle3DArtSemanticFamily071 family)
        {
            switch (family)
            {
                case Battle3DArtSemanticFamily071.AgileWeapon:
                case Battle3DArtSemanticFamily071.TacticalControl: return 0.14f;
                case Battle3DArtSemanticFamily071.ArcaneImplement:
                case Battle3DArtSemanticFamily071.MysticElemental:
                case Battle3DArtSemanticFamily071.HybridRelic: return 0.08f;
                case Battle3DArtSemanticFamily071.TacticalSupport: return 0.06f;
                default: return 0.025f;
            }
        }

        private static float ResolveLiftBias(Battle3DArtSemanticFamily071 family)
        {
            switch (family)
            {
                case Battle3DArtSemanticFamily071.ProjectileWeapon:
                case Battle3DArtSemanticFamily071.ArcaneImplement:
                case Battle3DArtSemanticFamily071.MysticElemental:
                case Battle3DArtSemanticFamily071.HybridRelic: return 0.11f;
                case Battle3DArtSemanticFamily071.Restoration:
                case Battle3DArtSemanticFamily071.Warding: return 0.08f;
                case Battle3DArtSemanticFamily071.HeavyWeapon: return -0.025f;
                default: return 0.02f;
            }
        }

        private static float Clamp(float value, float minimum, float maximum) =>
            Math.Max(minimum, Math.Min(maximum, value));

        private static uint StableSeed(string value)
        {
            var result = 2166136261u;
            value = value ?? string.Empty;
            for (var index = 0; index < value.Length; index++) result = (result ^ value[index]) * 16777619u;
            return result;
        }
    }
}
