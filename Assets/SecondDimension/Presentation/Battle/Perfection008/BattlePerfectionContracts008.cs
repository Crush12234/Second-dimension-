using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SecondDimension.Presentation
{
    public enum BattlePresentationMode008
    {
        TacticalOverview,
        CommandFocus,
        ForecastCommit,
        CinematicAction,
        ConsequenceRead,
        ReturnToTactical,
        Results
    }

    public enum BattleUnionLod008
    {
        Dormant,
        Navigator,
        TacticalAnchor,
        ContextUnion,
        HeroUnion
    }

    public enum BattleRelationshipKind008
    {
        None,
        Deadlock,
        SideStrike,
        RearAttack,
        Interference,
        Guard,
        Protect,
        Rescue,
        Support
    }

    public enum BattleActionFamily008
    {
        BasicMartial,
        HeavyMartial,
        RangedMartial,
        Mystic,
        Restoration,
        Guard,
        Support,
        CoordinatedUnion,
        FlankOrRear,
        Breakthrough,
        Result
    }

    public enum BattleShot008
    {
        TacticalOverviewWide,
        ActiveUnionFocus,
        TargetPreview,
        MeleeContact,
        RangedTrack,
        MysticField,
        HealerRecipient,
        GuardInterception,
        FlankReveal,
        RearAttack,
        UnionBreak,
        BreakthroughCue,
        VictoryWide
    }

    public enum BattleDeviceProfile008
    {
        Desktop,
        MobileHigh,
        MobileLow
    }

    [Serializable]
    public sealed class BattleRelationship008
    {
        public string SourceUnionId = string.Empty;
        public string TargetUnionId = string.Empty;
        public BattleRelationshipKind008 Kind = BattleRelationshipKind008.None;
        public bool Active = true;

        public string StableDescriptor => string.Join("|", new[]
        {
            SourceUnionId ?? string.Empty,
            TargetUnionId ?? string.Empty,
            Kind.ToString(),
            Active ? "ACTIVE" : "INACTIVE"
        });
    }

    public sealed class BattlePresentationRequest008
    {
        public BattlePresentationMode008 Mode { get; set; } = BattlePresentationMode008.TacticalOverview;
        public string ActiveUnionId { get; set; } = string.Empty;
        public string TargetUnionId { get; set; } = string.Empty;
        public IReadOnlyList<BattleRelationship008> Relationships { get; set; } = Array.Empty<BattleRelationship008>();
        public bool ReducedMotion { get; set; }
        public float AspectRatio { get; set; } = 16f / 9f;
        public Rect NormalizedSafeArea { get; set; } = new Rect(0f, 0f, 1f, 1f);
        public BattleDeviceProfile008 DeviceProfile { get; set; } = BattleDeviceProfile008.Desktop;
    }

    public sealed class BattleUnionDirective008
    {
        internal BattleUnionDirective008(
            string unionId,
            bool enemy,
            int sideOrdinal,
            BattleUnionLod008 lod,
            int visibleMemberLimit,
            int targetUpdateHz,
            bool keepNavigatorPlate,
            bool useActionPose,
            bool useSecondaryMotion,
            bool useMajorVfx,
            bool defeated)
        {
            UnionId = unionId ?? string.Empty;
            Enemy = enemy;
            SideOrdinal = sideOrdinal;
            Lod = lod;
            VisibleMemberLimit = Math.Max(0, visibleMemberLimit);
            TargetUpdateHz = Math.Max(0, targetUpdateHz);
            KeepNavigatorPlate = keepNavigatorPlate;
            UseActionPose = useActionPose;
            UseSecondaryMotion = useSecondaryMotion;
            UseMajorVfx = useMajorVfx;
            Defeated = defeated;
        }

        public string UnionId { get; }
        public bool Enemy { get; }
        public int SideOrdinal { get; }
        public BattleUnionLod008 Lod { get; }
        public int VisibleMemberLimit { get; }
        public int TargetUpdateHz { get; }
        public bool KeepNavigatorPlate { get; }
        public bool UseActionPose { get; }
        public bool UseSecondaryMotion { get; }
        public bool UseMajorVfx { get; }
        public bool Defeated { get; }
        public bool IsPresentationOnly => true;

        public string StableDescriptor => string.Join("|", new[]
        {
            UnionId,
            Enemy ? "ENEMY" : "ALLY",
            SideOrdinal.ToString(CultureInfo.InvariantCulture),
            Lod.ToString(),
            VisibleMemberLimit.ToString(CultureInfo.InvariantCulture),
            TargetUpdateHz.ToString(CultureInfo.InvariantCulture),
            KeepNavigatorPlate ? "NAV" : "NO_NAV",
            UseActionPose ? "ACTION" : "NO_ACTION",
            UseSecondaryMotion ? "SECONDARY" : "NO_SECONDARY",
            UseMajorVfx ? "MAJOR_VFX" : "NO_MAJOR_VFX",
            Defeated ? "DEFEATED" : "LIVING"
        });
    }

    public sealed class BattleNavigatorChip008
    {
        internal BattleNavigatorChip008(string unionId, bool enemy, int sideOrdinal, Rect normalizedRect)
        {
            UnionId = unionId ?? string.Empty;
            Enemy = enemy;
            SideOrdinal = sideOrdinal;
            NormalizedRect = normalizedRect;
        }

        public string UnionId { get; }
        public bool Enemy { get; }
        public int SideOrdinal { get; }
        public Rect NormalizedRect { get; }

        public string StableDescriptor => string.Join("|", new[]
        {
            UnionId,
            Enemy ? "ENEMY" : "ALLY",
            SideOrdinal.ToString(CultureInfo.InvariantCulture),
            NormalizedRect.x.ToString("R", CultureInfo.InvariantCulture),
            NormalizedRect.y.ToString("R", CultureInfo.InvariantCulture),
            NormalizedRect.width.ToString("R", CultureInfo.InvariantCulture),
            NormalizedRect.height.ToString("R", CultureInfo.InvariantCulture)
        });
    }

    public sealed class BattleSafeLayout008
    {
        internal BattleSafeLayout008(
            Rect safeArea,
            Rect battlefield,
            Rect leftNavigator,
            Rect rightNavigator,
            Rect commandPanel,
            Rect forecastDetails,
            IReadOnlyList<BattleNavigatorChip008> chips)
        {
            SafeArea = safeArea;
            Battlefield = battlefield;
            LeftNavigator = leftNavigator;
            RightNavigator = rightNavigator;
            CommandPanel = commandPanel;
            ForecastDetails = forecastDetails;
            NavigatorChips = chips ?? Array.Empty<BattleNavigatorChip008>();
        }

        public Rect SafeArea { get; }
        public Rect Battlefield { get; }
        public Rect LeftNavigator { get; }
        public Rect RightNavigator { get; }
        public Rect CommandPanel { get; }
        public Rect ForecastDetails { get; }
        public IReadOnlyList<BattleNavigatorChip008> NavigatorChips { get; }
    }

    public sealed class BattlePerformanceBudget008
    {
        internal BattlePerformanceBudget008(
            int maxHeroUnions,
            int maxContextUnions,
            int maxVisibleMembers,
            int maxMajorVfx,
            int maxPooledProjectiles,
            int tacticalUpdateHz,
            int navigatorUpdateHz)
        {
            MaxHeroUnions = maxHeroUnions;
            MaxContextUnions = maxContextUnions;
            MaxVisibleMembers = maxVisibleMembers;
            MaxMajorVfx = maxMajorVfx;
            MaxPooledProjectiles = maxPooledProjectiles;
            TacticalUpdateHz = tacticalUpdateHz;
            NavigatorUpdateHz = navigatorUpdateHz;
        }

        public int MaxHeroUnions { get; }
        public int MaxContextUnions { get; }
        public int MaxVisibleMembers { get; }
        public int MaxMajorVfx { get; }
        public int MaxPooledProjectiles { get; }
        public int TacticalUpdateHz { get; }
        public int NavigatorUpdateHz { get; }
    }

    public sealed class BattlePresentationPlan008
    {
        internal BattlePresentationPlan008(
            IReadOnlyList<BattleUnionDirective008> directives,
            BattleSafeLayout008 layout,
            BattlePerformanceBudget008 budget,
            BattleShot008 openingShot,
            string activeUnionId,
            string targetUnionId)
        {
            Directives = directives ?? Array.Empty<BattleUnionDirective008>();
            Layout = layout;
            Budget = budget;
            OpeningShot = openingShot;
            ActiveUnionId = activeUnionId ?? string.Empty;
            TargetUnionId = targetUnionId ?? string.Empty;
        }

        public IReadOnlyList<BattleUnionDirective008> Directives { get; }
        public BattleSafeLayout008 Layout { get; }
        public BattlePerformanceBudget008 Budget { get; }
        public BattleShot008 OpeningShot { get; }
        public string ActiveUnionId { get; }
        public string TargetUnionId { get; }
        public bool IsPresentationOnly => true;

        public string StableDescriptor => string.Join("\n", Directives == null
            ? Array.Empty<string>()
            : System.Linq.Enumerable.Select(Directives, value => value.StableDescriptor));
    }

    public sealed class BattleActionBeat008
    {
        internal BattleActionBeat008(string name, float startSeconds, float durationSeconds, bool impactBeat)
        {
            Name = name ?? string.Empty;
            StartSeconds = Math.Max(0f, startSeconds);
            DurationSeconds = Math.Max(0f, durationSeconds);
            ImpactBeat = impactBeat;
        }

        public string Name { get; }
        public float StartSeconds { get; }
        public float DurationSeconds { get; }
        public bool ImpactBeat { get; }
        public float EndSeconds => StartSeconds + DurationSeconds;
    }

    public sealed class BattleActionSchedule008
    {
        internal BattleActionSchedule008(
            BattleActionFamily008 family,
            BattleShot008 shot,
            IReadOnlyList<BattleActionBeat008> beats,
            float hitTimeSeconds)
        {
            Family = family;
            Shot = shot;
            Beats = beats ?? Array.Empty<BattleActionBeat008>();
            HitTimeSeconds = Math.Max(0f, hitTimeSeconds);
        }

        public BattleActionFamily008 Family { get; }
        public BattleShot008 Shot { get; }
        public IReadOnlyList<BattleActionBeat008> Beats { get; }
        public float HitTimeSeconds { get; }
        public float TotalDurationSeconds => Beats == null || Beats.Count == 0 ? 0f : Beats[Beats.Count - 1].EndSeconds;
    }
}
