using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.FirstHour071;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        public const string LivingGuildHubBackgroundResource074 =
            "SecondDimension/Art/GuildHome074/LIVING_GUILD_HALL_074";
        public const string LivingGuildHubTitle074 = "SKYHOME GUILD HALL";
        public const string LivingGuildHubMembersLabel076 = "RECRUIT\nMEET MEMBERS";
        public const string LivingGuildHubCityDestinationId078 = "CITY_BUILD_MODE_078";
        public const string LivingGuildHubTowerDestinationId081 = "ENDLESS_TOWER_081";
        public const string LivingGuildHubCodesDestinationId084 = "CODES";
        public const string LivingGuildHubObjectiveHeading084 = "NEXT STORY";
        public const int LivingGuildHubRosterVisibleLimit074 = 6;
        public const int LivingGuildHubRosterIdentityLineCapacity074 = 3;
        public const int LivingGuildHubRosterIdentityMinimumFontSize076 = 22;
        public const int LivingGuildHubRosterIdentityMaximumFontSize076 = 26;

        private static readonly Rect LivingGuildRosterRegion074 =
            new Rect(0.015f, 0.015f, 0.970f, 0.180f);
        private static readonly Rect LivingGuildObjectiveRegion074 =
            new Rect(0.045f, 0.285f, 0.390f, 0.520f);
        private static readonly Rect LivingGuildRosterIdentityRegion074 =
            new Rect(0.410f, 0.300f, 0.575f, 0.660f);
        private static readonly Rect LivingGuildRosterReadinessRegion074 =
            new Rect(0.410f, 0.030f, 0.575f, 0.250f);
        private static readonly Rect LivingGuildHomecomingRegion076 =
            new Rect(0.185f, 0.205f, 0.800f, 0.115f);
        private static readonly Rect LivingGuildHomecomingCameoRegion076 =
            new Rect(0.255f, 0.075f, 0.725f, 0.850f);
        private const float LivingGuildHomecomingCameoGap076 = 0.010f;
        private const float LivingGuildRosterStartX074 = 0.010f;
        private const float LivingGuildRosterEndX074 = 0.990f;
        private const float LivingGuildRosterGap074 = 0.006f;
        private const float LivingGuildRosterCardY074 = 0.035f;
        private const float LivingGuildRosterCardHeight074 = 0.740f;
        private const float LivingGuildActionStartX084 = 0.025f;
        private const float LivingGuildActionWidth084 = 0.220f;
        private const float LivingGuildActionGapX084 = 0.024f;
        private const float LivingGuildActionHeight084 = 0.200f;
        private const float LivingGuildActionBottomY084 = 0.035f;

        private sealed class LivingGuildFacilitySpec074
        {
            public LivingGuildFacilitySpec074(
                string id,
                string label,
                string context,
                string glyph,
                string artworkResource,
                Rect anchors)
            {
                Id = id;
                Label = label;
                Context = context;
                Glyph = glyph;
                ArtworkResource = artworkResource;
                Anchors = anchors;
            }

            public string Id { get; }
            public string Label { get; }
            public string Context { get; }
            public string Glyph { get; }
            public string ArtworkResource { get; }
            public Rect Anchors { get; }
        }

        private static Rect LivingGuildActionRect084(int index) =>
            new Rect(
                LivingGuildActionStartX084 +
                Math.Max(0, Math.Min(3, index)) *
                (LivingGuildActionWidth084 + LivingGuildActionGapX084),
                LivingGuildActionBottomY084,
                LivingGuildActionWidth084,
                LivingGuildActionHeight084);

        private static readonly LivingGuildFacilitySpec074[] LivingGuildFacilities074 =
        {
            new LivingGuildFacilitySpec074(
                GuildCity017D.WalkableGuildHall069.ContractDestinationId069,
                "CAMPAIGN", "THREE-CARD QUESTS", "QUEST",
                "SecondDimension/Art/Backgrounds/CONTRACT_BELL_BENEATH_GATE_KEY_ART_V62",
                LivingGuildActionRect084(0)),
            new LivingGuildFacilitySpec074(
                LivingGuildHubTowerDestinationId081,
                "TOWER", "OPTIONAL BATTLES", "BATTLE",
                "SecondDimension/Art/Campaign083/ABYSS_FLOOR_01_MUD_TRENCHES_BATTLE_083",
                LivingGuildActionRect084(1)),
            new LivingGuildFacilitySpec074(
                GuildCity017D.WalkableGuildHall069.PartyDestinationId069,
                "UNIONS", "HERO PARTY PLANS", "HEROES",
                "SecondDimension/Art/Backgrounds/BG_UNION_STRATEGY_CHAMBER",
                LivingGuildActionRect084(2)),
            new LivingGuildFacilitySpec074(
                GuildCity017D.WalkableGuildHall069.ArmoryDestinationId069,
                "EQUIPMENT", "GEAR & ITEMS", "ARMORY",
                "SecondDimension/Art/Backgrounds/BG_QUARTERMASTER_ARMORY",
                LivingGuildActionRect084(3))
        };

        private static Sprite _livingGuildBackgroundSprite074;

        public static IReadOnlyList<string> LivingGuildHubFacilityIdsForVerification074 =>
            Array.AsReadOnly(LivingGuildFacilities074.Select(value => value.Id).ToArray());

        public static IReadOnlyList<Rect> LivingGuildHubFacilityRectsForVerification074 =>
            Array.AsReadOnly(LivingGuildFacilities074.Select(value => value.Anchors).ToArray());

        public static IReadOnlyList<string> LivingGuildHubFacilityLabelsForVerification084 =>
            Array.AsReadOnly(LivingGuildFacilities074
                .Select(value => value.Label + "\n" + value.Context)
                .ToArray());

        public static Rect LivingGuildHubRosterRegionForVerification074 =>
            LivingGuildRosterRegion074;

        public static Rect LivingGuildHubObjectiveRegionForVerification078 =>
            LivingGuildObjectiveRegion074;

        public static Rect LivingGuildHubRosterIdentityRegionForVerification074 =>
            LivingGuildRosterIdentityRegion074;

        public static Rect LivingGuildHubRosterReadinessRegionForVerification074 =>
            LivingGuildRosterReadinessRegion074;

        public static Rect LivingGuildHubHomecomingRegionForVerification076 =>
            LivingGuildHomecomingRegion076;

        public static IReadOnlyList<Rect> LivingGuildHubHomecomingCameoRectsForVerification076()
        {
            const int count = 4;
            var width = (LivingGuildHomecomingCameoRegion076.width -
                         LivingGuildHomecomingCameoGap076 * (count - 1)) / count;
            var rects = new Rect[count];
            for (var index = 0; index < count; index++)
                rects[index] = new Rect(
                    LivingGuildHomecomingCameoRegion076.xMin +
                    index * (width + LivingGuildHomecomingCameoGap076),
                    LivingGuildHomecomingCameoRegion076.yMin,
                    width,
                    LivingGuildHomecomingCameoRegion076.height);
            return Array.AsReadOnly(rects);
        }

        public static string LivingGuildHubStatusCopyForVerification084(
            int guildLevel,
            long guildLevelXp,
            long guildLevelXpRequired,
            long xpToSpend) =>
            "GUILD LEVEL  " + Math.Max(1, guildLevel) +
            "  •  GUILD XP  " + FormatProgressionNumber(guildLevelXp) +
            " / " + FormatProgressionNumber(Math.Max(1L, guildLevelXpRequired)) +
            "  •  XP TO SPEND  " + FormatProgressionNumber(xpToSpend);

        public static string LivingGuildHubLedgerCopyForVerification076(
            int guildLevel,
            long guildLevelXp,
            long guildLevelXpRequired,
            long spendableMemberXp,
            long hallImprovementXp,
            int day,
            int reputation) => LivingGuildHubStatusCopyForVerification084(
                guildLevel,
                guildLevelXp,
                guildLevelXpRequired,
                spendableMemberXp);

        public static string LivingGuildHubConciseObjectiveForVerification084(string copy)
        {
            var value = string.IsNullOrWhiteSpace(copy)
                ? "Choose your next mission from the board."
                : copy.Trim();
            return value.StartsWith("NEXT: ", StringComparison.OrdinalIgnoreCase)
                ? value.Substring(6).Trim()
                : value;
        }

        public static IReadOnlyList<string> LivingGuildHubFeaturedAuthorityIdsForVerification076(
            IReadOnlyList<string> authorityIds,
            bool homecoming)
        {
            var valid = (authorityIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (!homecoming)
                return Array.AsReadOnly(valid.Take(LivingGuildHubRosterVisibleLimit074).ToArray());

            var patrolIds = new HashSet<string>(
                FirstHourRosterService071.PatrolStableRecruitIds,
                StringComparer.Ordinal);
            var featured = valid.Where(value => !patrolIds.Contains(value)).Take(3)
                .Concat(valid.Where(patrolIds.Contains).Take(3))
                .ToList();
            featured.AddRange(valid.Where(value => !featured.Contains(value, StringComparer.Ordinal))
                .Take(LivingGuildHubRosterVisibleLimit074 - featured.Count));
            return Array.AsReadOnly(featured.Take(LivingGuildHubRosterVisibleLimit074).ToArray());
        }

        public static IReadOnlyList<Rect> LivingGuildHubRosterCardRectsForVerification074(
            int visibleCount)
        {
            var count = Mathf.Clamp(visibleCount, 0, LivingGuildHubRosterVisibleLimit074);
            if (count == 0) return Array.Empty<Rect>();
            var width = (LivingGuildRosterEndX074 - LivingGuildRosterStartX074 -
                         LivingGuildRosterGap074 * (count - 1)) / count;
            var cards = new Rect[count];
            for (var index = 0; index < count; index++)
                cards[index] = new Rect(
                    LivingGuildRosterStartX074 + index * (width + LivingGuildRosterGap074),
                    LivingGuildRosterCardY074,
                    width,
                    LivingGuildRosterCardHeight074);
            return Array.AsReadOnly(cards);
        }

        public static string LivingGuildHubRosterIdentityForVerification074(
            string displayName,
            string observedClass,
            int level)
        {
            var role = string.IsNullOrWhiteSpace(observedClass)
                ? "Adventurer"
                : observedClass.Trim();
            return LivingGuildHubRosterNameForVerification076(displayName) +
                   "\n" + role + " L" + Math.Max(1, level);
        }

        public static string LivingGuildHubRosterNameForVerification076(string displayName)
        {
            var name = string.IsNullOrWhiteSpace(displayName)
                ? "Unnamed Adventurer"
                : displayName.Trim();
            var words = name.Split(
                new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            if (words.Length <= 1) return name;

            // The six-card desktop rail has room for two deliberate name lines and
            // one role line. Pick the whole-word break with the shortest longest
            // line, then the most even pair. Unity must never improvise a fourth
            // line by cutting a surname such as Bramblecross in half.
            var bestBreak = 1;
            var bestLongestLine = int.MaxValue;
            var bestDifference = int.MaxValue;
            for (var split = 1; split < words.Length; split++)
            {
                var leftLength = words.Take(split).Sum(value => value.Length) + split - 1;
                var rightWordCount = words.Length - split;
                var rightLength = words.Skip(split).Sum(value => value.Length) +
                                  rightWordCount - 1;
                var longestLine = Math.Max(leftLength, rightLength);
                var difference = Math.Abs(leftLength - rightLength);
                if (longestLine > bestLongestLine ||
                    (longestLine == bestLongestLine && difference >= bestDifference))
                    continue;
                bestBreak = split;
                bestLongestLine = longestLine;
                bestDifference = difference;
            }

            return string.Join(" ", words.Take(bestBreak)) + "\n" +
                   string.Join(" ", words.Skip(bestBreak));
        }

        public static string LivingGuildHubRosterReadinessForVerification074(bool isLegal) =>
            isLegal ? "READY" : "CHECK GEAR";

        public static string LivingGuildHubRosterHeadingForVerification079(
            int memberCount,
            int currentHousingCapacity,
            int shownCount)
        {
            var safeMembers = Math.Max(0, memberCount);
            var safeCapacity = Math.Max(safeMembers, currentHousingCapacity);
            return "YOUR PEOPLE  •  " + safeMembers + " MEMBERS" +
                   "  •  CURRENT HOUSING CAP " + safeCapacity +
                   "  •  " + Math.Max(0, shownCount) + " SHOWN";
        }

        /// <summary>
        /// A scene-led Guild home: companions inhabit the Hall, one story action
        /// anchors the left side, and four direct destinations share a bottom dock.
        /// </summary>
        private void BuildLivingGuildHub074(
            GuildCity017D.IGuildCityPresentationCoordinator017D coordinator,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();

            var root = RuntimeUi.AddPanel(
                _screenRoot,
                "Living Guild Hub 074",
                new Color(0.008f, 0.014f, 0.022f, 1f));
            Stretch(root.rectTransform);
            root.raycastTarget = false;
            _activePage = root.rectTransform;
            _activeContent = null;
            _activeScroll = null;

            BuildLivingGuildBackdrop074(root.transform);

            var shade = RuntimeUi.AddPanel(
                root.transform,
                "Living Guild Hub Readability Shade 074",
                new Color(0.008f, 0.014f, 0.022f, _highContrast ? 0.34f : 0.15f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            BuildLivingGuildStatusBar074(root.transform, state);
            BuildLivingGuildCompanions091(root.transform);

            var dock = RuntimeUi.AddPanel(root.transform,
                "Living Guild Direct Navigation Dock 091", Color.white);
            AnchorLivingGuild074(dock.rectTransform, new Rect(0.015f, 0.020f, 0.970f, 0.230f));
            M1PremiumUi.StylePanel(dock, M1PremiumUi.Surface.WorldRibbon);
            dock.raycastTarget = false;

            var objective = ResolveWalkableHallObjective069();
            var objectiveId = objective?.TargetHotspotId ??
                              GuildCity017D.WalkableGuildHall069.GuideDestinationId069;
            var facilityButtons = new List<Button>(LivingGuildFacilities074.Length);
            Button selectedFacility = null;
            for (var index = 0; index < LivingGuildFacilities074.Length; index++)
            {
                var spec = LivingGuildFacilities074[index];
                var selected = StringComparer.Ordinal.Equals(spec.Id, objectiveId);
                var button = AddLivingGuildFacility074(root.transform, spec, selected);
                facilityButtons.Add(button);
                if (selected) selectedFacility = button;
            }

            var primary = BuildLivingGuildObjectiveCard074(root.transform, state, objective);
            var secondary = BuildLivingGuildSecondaryNavigation110(root.transform);
            ConfigureLivingGuildNavigation074(
                facilityButtons,
                primary,
                secondary);
            ConfigureEarnedRecruitNavigation094(primary, _earnedGuildAccess094);
            if (_earnedGuildAccess094 != null && secondary.Length > 0)
            {
                var rewardNavigation = _earnedGuildAccess094.navigation;
                rewardNavigation.selectOnLeft = secondary[secondary.Length - 1];
                _earnedGuildAccess094.navigation = rewardNavigation;
                var secondaryNavigation = secondary[secondary.Length - 1].navigation;
                secondaryNavigation.selectOnRight = _earnedGuildAccess094;
                secondary[secondary.Length - 1].navigation = secondaryNavigation;
            }

            // Controller players enter on the single next-story action; mouse players
            // still have the complete Hall available in one glance.
            if (primary != null)
            {
                primary.Select();
                if (EventSystem.current != null)
                    EventSystem.current.SetSelectedGameObject(primary.gameObject);
            }
            else selectedFacility?.Select();
        }

        private static void BuildLivingGuildBackdrop074(Transform parent)
        {
            var background = RuntimeUi.AddPanel(
                parent,
                "Living Guild Hub Populated Background 074",
                new Color(0.08f, 0.07f, 0.06f, 1f));
            Stretch(background.rectTransform);
            background.raycastTarget = false;

            var sprite = ResolveLivingGuildBackground074();
            if (sprite == null &&
                M1VisualAssets.TryResolveBackdrop(
                    M1VisualAssets.BackdropRole.GuildHallStage01,
                    out var fallback,
                    out _))
                sprite = fallback;
            if (sprite == null) return;

            background.sprite = sprite;
            background.type = Image.Type.Simple;
            background.preserveAspect = false;
            background.color = Color.white;
            var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
        }

        private static Sprite ResolveLivingGuildBackground074()
        {
            if (_livingGuildBackgroundSprite074 != null) return _livingGuildBackgroundSprite074;
            _livingGuildBackgroundSprite074 = Resources.Load<Sprite>(LivingGuildHubBackgroundResource074);
            if (_livingGuildBackgroundSprite074 != null) return _livingGuildBackgroundSprite074;

            var texture = Resources.Load<Texture2D>(LivingGuildHubBackgroundResource074);
            if (texture == null) return null;
            _livingGuildBackgroundSprite074 = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0u,
                SpriteMeshType.FullRect);
            _livingGuildBackgroundSprite074.name = "Living Guild Hub Populated Background Sprite 074";
            return _livingGuildBackgroundSprite074;
        }

        private void BuildLivingGuildCompanions091(Transform parent)
        {
            var companions = (_coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .Take(3)
                .ToArray();
            var visible = 0;
            for (var index = 0; index < companions.Length; index++)
            {
                var companion = companions[index];
                if (!M1VisualAssets.TryResolveMenuStandee091(
                        companion.RecruitId,
                        companion.VisualSeed,
                        companion.RaceId,
                        companion.PortraitAuthorityId,
                        companion.ObservedClass,
                        string.Join(" ", (companion.Slots ?? Array.Empty<M1EquipmentSlotView>())
                            .Where(slot => slot != null)
                            .Select(slot => slot.EquippedItemName)),
                        out var sprite,
                        out _) || sprite == null)
                    continue;

                var stage = RuntimeUi.AddStretchRect(parent,
                    "Living Guild Companion Stage 091 " + companion.RecruitId);
                AnchorLivingGuild074(stage, new Rect(0.495f + index * 0.155f, 0.265f, 0.205f, 0.510f));
                var shadow = RuntimeUi.AddPanel(stage,
                    "Companion Ground Shadow 091", new Color(0f, 0f, 0f, 0.42f));
                shadow.sprite = M1PremiumUi.RoundedMask091;
                shadow.type = Image.Type.Sliced;
                shadow.raycastTarget = false;
                AnchorLivingGuild074(shadow.rectTransform, new Rect(0.19f, 0.022f, 0.62f, 0.048f));

                var hero = RuntimeUi.AddPanel(stage,
                    "Living Guild Standing Companion 091 " + companion.RecruitId, Color.white);
                hero.sprite = sprite;
                hero.preserveAspect = true;
                hero.raycastTarget = false;
                AnchorLivingGuild074(hero.rectTransform, new Rect(0f, 0.045f, 1f, 0.955f));
                visible++;
            }
            if (TryBuildEarnedRecruitAccess094(parent, new Rect(0.51f, 0.795f, 0.43f, 0.09f))) return;
            if (visible == 0) return;

            var welcome = RuntimeUi.AddText(parent,
                "Living Guild Company Presence 091", "YOUR COMPANY IS HOME",
                24, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            welcome.raycastTarget = false;
            AnchorLivingGuild074(welcome.rectTransform, new Rect(0.51f, 0.795f, 0.43f, 0.050f));
            ConfigureResponsiveText062(welcome, 17, 24);
        }

        private void BuildLivingGuildStatusBar074(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state)
        {
            var bar = RuntimeUi.AddPanel(
                parent,
                "Living Guild Hub Persistent Status 074",
                new Color(0.01f, 0.018f, 0.03f, 0.94f));
            AnchorLivingGuild074(bar.rectTransform, new Rect(0.015f, 0.905f, 0.97f, 0.080f));
            M1PremiumUi.StylePanel(bar, M1PremiumUi.Surface.WorldRibbon);

            var title = RuntimeUi.AddText(
                bar.transform,
                "Living Guild Hub Guild Name 074",
                LivingGuildHubTitle074,
                24,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorLivingGuild074(title.rectTransform, new Rect(0.02f, 0.10f, 0.22f, 0.80f));
            title.raycastTarget = false;
            ConfigureResponsiveText062(title, 15, 24);

            var presentation = _coordinator?.State;
            var rank = Math.Max(1, presentation?.GuildLevel ?? 1);
            var xp = presentation?.GuildXpIntoCurrentLevel ?? 0;
            var xpRequired = Math.Max(1, presentation?.GuildXpRequiredForNextLevel ?? 1);
            var treasury = presentation?.TreasuryXp ?? state?.TreasuryXp ?? 0;
            var resources = RuntimeUi.AddText(
                bar.transform,
                "Living Guild Hub Rank Resources Day Reputation 074",
                LivingGuildHubStatusCopyForVerification084(
                    rank,
                    xp,
                    xpRequired,
                    treasury),
                22,
                TextAnchor.MiddleRight,
                RuntimeUi.Positive,
                FontStyle.Bold);
            AnchorLivingGuild074(resources.rectTransform, new Rect(0.25f, 0.08f, 0.73f, 0.84f));
            resources.raycastTarget = false;
            ConfigureResponsiveText062(resources, 14, 22);
        }

        private Button[] BuildLivingGuildSecondaryNavigation110(Transform parent)
        {
            var compact164 = Screen.width < 1000 || Screen.height < 570;
            var codes = RuntimeUi.AddButton(parent, "Living Guild Home Codes 110", "CODES",
                () => InvokeLivingGuildFacility074(LivingGuildHubCodesDestinationId084));
            AnchorLivingGuild074(codes.GetComponent<RectTransform>(), compact164 ? new Rect(.025f,.740f,.12f,.145f) : new Rect(.025f,.830f,.085f,.063f));
            var options = RuntimeUi.AddButton(parent, "Living Guild Home Title Options 110", "TITLE & OPTIONS",
                () => Navigate(M1Screen.MainMenu));
            AnchorLivingGuild074(options.GetComponent<RectTransform>(), compact164 ? new Rect(.155f,.740f,.17f,.145f) : new Rect(.12f,.830f,.175f,.063f));
            var town = RuntimeUi.AddButton(parent, "Living Guild Town 153", "TOWN & SHOPS",
                () => OpenTownService153("TOWN"));
            AnchorLivingGuild074(town.GetComponent<RectTransform>(), compact164 ? new Rect(.335f,.740f,.16f,.145f) : new Rect(.305f,.830f,.19f,.063f));
            foreach (var button in new[] { codes, options, town })
            {
                var rect = button.GetComponent<RectTransform>();
                if(compact164) button.gameObject.AddComponent<MinimumTownTarget164>().Bounds =
                    new Rect(rect.anchorMin.x,rect.anchorMin.y,rect.anchorMax.x-rect.anchorMin.x,rect.anchorMax.y-rect.anchorMin.y);
                var label = button.GetComponentInChildren<Text>();
                ConfigureResponsiveText062(label, 26, 32);
                label.rectTransform.offsetMin = new Vector2(8f, 4f);
                label.rectTransform.offsetMax = new Vector2(-8f, -4f);
            }
            return new[] { codes, options, town };
        }

        private Button AddLivingGuildFacility074(
            Transform parent,
            LivingGuildFacilitySpec074 spec,
            bool selected)
        {
            var capturedId = spec.Id;
            var button = RuntimeUi.AddButton(
                parent,
                "Living Guild Hub Facility " + spec.Id + " 074",
                spec.Label + "\n" + spec.Context,
                () => InvokeLivingGuildFacility074(capturedId),
                RuntimeUi.MinimumTouchPixels,
                selected ? new Color(0.16f, 0.12f, 0.055f, 0.92f) : new Color(0.025f, 0.045f, 0.055f, 0.88f));
            AnchorLivingGuild074(button.GetComponent<RectTransform>(), spec.Anchors);
            DecorateLivingGuildFacility090(
                button,
                spec.ArtworkResource,
                spec.Glyph,
                selected);
            // Keep the location visible above the dock. Art belongs to a compact
            // navigation control here, with four direct destinations sharing the same dock.
            var artworkWindow = button.transform.Find("Facility Artwork Window 090") as RectTransform;
            if (artworkWindow != null)
                AnchorLivingGuild074(artworkWindow, new Rect(0.055f, 0.395f, 0.890f, 0.565f));
            var caption = button.transform.Find("Facility Caption Plate 090") as RectTransform;
            if (caption != null)
                AnchorLivingGuild074(caption, new Rect(0.025f, 0.020f, 0.950f, 0.370f));
            var category = button.transform.Find("Facility Category Tag Backing 090");
            if (category != null) category.gameObject.SetActive(false);
            var storyBadge = button.transform.Find("Facility Story Target Badge Backing 090");
            if (storyBadge != null) storyBadge.gameObject.SetActive(false);
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                label.fontSize = 36;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 28;
                label.resizeTextMaxSize = 36;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleCenter;
                label.rectTransform.anchorMin = new Vector2(0.035f, 0.025f);
                label.rectTransform.anchorMax = new Vector2(0.965f, 0.385f);
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
            return button;
        }

        private Button BuildLivingGuildObjectiveCard074(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state,
            GuildCity017D.GuildHallObjective069 objective)
        {
            var firstComplete = IsStoryContractCompleted065(state, FirstStoryContractId065);
            var secondComplete = IsStoryContractCompleted065(state, SecondStoryContractId065);
            var thirdComplete = IsStoryContractCompleted065(state, ThirdStoryContractId065);
            var activeRoute110 = CurrentActiveAdventureRoute110(state);
            var hasActiveAdventure110 = !string.IsNullOrWhiteSpace(activeRoute110);
            var card = RuntimeUi.AddPanel(
                parent,
                "Living Guild Hub Objective Card 074",
                new Color(0.012f, 0.020f, 0.032f, 0.96f));
            AnchorLivingGuild074(card.rectTransform, (Screen.width < 1000 || Screen.height < 570)
                ? new Rect(.045f,.285f,.39f,.44f) : LivingGuildObjectiveRegion074);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldRibbon);

            var chapter = RuntimeUi.AddText(
                card.transform,
                "Living Guild Hub Chapter 074",
                hasActiveAdventure110
                    ? "CURRENT ADVENTURE\n" + ActiveAdventureActionLabel110(activeRoute110)
                    : LivingGuildHubObjectiveHeading084 + "\n" +
                      StoryChapterHeading065(firstComplete, secondComplete, thirdComplete),
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorLivingGuild074(chapter.rectTransform, new Rect(0.055f, 0.735f, 0.89f, 0.215f));
            chapter.raycastTarget = false;
            ConfigureResponsiveText062(chapter, 15, 22);

            var objectiveName = LivingGuildFacilityLabel074(objective?.TargetHotspotId);
            var objectiveDescription = string.IsNullOrWhiteSpace(objective?.Description)
                ? StoryNextObjective065(state, firstComplete, secondComplete, thirdComplete)
                : objective.Description.Trim();
            if (hasActiveAdventure110)
            {
                objectiveName = activeRoute110 == "ABYSS" ? "Infinite Tower" : "Saved adventure";
                objectiveDescription = activeRoute110 == "BATTLE RESULTS"
                    ? "Your battle is complete. Collect its earned result, then continue from the saved return."
                    : "Continue the battle or quest already in progress. Your saved route and earned results are waiting.";
            }
            objectiveDescription = LivingGuildHubConciseObjectiveForVerification084(
                LivingHallObjectiveCopy069(objectiveDescription));
            var objectiveCopy = RuntimeUi.AddText(
                card.transform,
                "Living Guild Hub Current Objective 074",
                objectiveName.ToUpperInvariant() + "\n" + objectiveDescription,
                19,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorLivingGuild074(objectiveCopy.rectTransform, new Rect(0.055f, 0.465f, 0.89f, 0.240f));
            objectiveCopy.raycastTarget = false;
            ConfigureResponsiveText062(objectiveCopy, 14, 19);
            // This one compact overlay has a deliberate 16 px floor. The page-wide
            // responsive helper clamps ordinary page copy to 18 px, which is too
            // large for the longer saved-expedition objective variants at 1280x800.
            objectiveCopy.resizeTextMinSize = 16;
            objectiveCopy.verticalOverflow = VerticalWrapMode.Truncate;

            var needsChapterTwoUnionRepair076 = !hasActiveAdventure110 && firstComplete && !secondComplete &&
                                                  NeedsChapterTwoUnionRepair076(state);
            var needsFirstOperationConsequences077 =
                !hasActiveAdventure110 && !needsChapterTwoUnionRepair076 &&
                NeedsFirstOperationConsequences077(state);
            var needsMiraWayglassHandoff = !needsFirstOperationConsequences077 &&
                                          !hasActiveAdventure110 &&
                                          !needsChapterTwoUnionRepair076 &&
                                          firstComplete && !secondComplete &&
                                          NeedsGuidedFirstHallImprovement069(state);
            var actionLabel = hasActiveAdventure110
                ? ActiveAdventureActionLabel110(activeRoute110)
                : needsChapterTwoUnionRepair076
                ? "FIX UNION PLANS"
                : needsFirstOperationConsequences077
                    ? FirstOperationConsequenceActionForVerification077(state)
                : needsMiraWayglassHandoff
                    ? "OPEN THE WAYGLASS WITH MIRA"
                    : StoryNextActionLabel065(
                    state,
                    firstComplete,
                    secondComplete,
                    thirdComplete);
            if (!firstComplete)
            {
                if (StringComparer.Ordinal.Equals(actionLabel, "BEGIN CHAPTER 1"))
                    actionLabel = "OPEN THE RESCUE CONTRACT";
                else if (StringComparer.Ordinal.Equals(actionLabel, "REVIEW PARTY"))
                    actionLabel = "READY UNIONS FOR LANTERN ROAD";
            }
            var action = RuntimeUi.AddButton(
                card.transform,
                "Living Guild Hub Primary CTA 074",
                actionLabel + "  →",
                needsChapterTwoUnionRepair076
                    ? (Action)OpenChapterTwoUnionRepair076
                    : needsFirstOperationConsequences077
                        ? (Action)OpenFirstOperationConsequences077
                    : needsMiraWayglassHandoff
                        ? (Action)OpenHallGuide069
                        : () =>
                    {
                        _showMiraWayglassBriefing069 = false;
                        OpenStoryNextStep065(
                            state,
                            firstComplete,
                            secondComplete,
                            thirdComplete);
                    },
                RuntimeUi.PrimaryTouchPixels,
                RuntimeUi.Accent);
            AnchorLivingGuild074(action.GetComponent<RectTransform>(), new Rect(0.055f, 0.055f, 0.89f, 0.360f));
            var label = action.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.fontSize = 23;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 17;
                label.resizeTextMaxSize = 23;
            }
            return action;
        }

        private void BuildLivingGuildHomecoming076(Transform parent)
        {
            var allRecruits = (_coordinator?.State?.Recruits ??
                               Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var patrolIds = new HashSet<string>(
                FirstHourRosterService071.PatrolStableRecruitIds,
                StringComparer.Ordinal);
            var patrol = allRecruits
                .Where(value => patrolIds.Contains(LivingGuildAuthorityId076(value)))
                .ToArray();
            var cameos = patrol.Skip(3).Take(4).ToArray();
            if (cameos.Length < 4) cameos = patrol.Take(4).ToArray();
            var unionCount = (_coordinator?.State?.Unions ?? Array.Empty<M1UnionView>())
                .Count(value => value != null && (value.MemberRecruitIds?.Count ?? 0) > 0);

            var celebration = RuntimeUi.AddPanel(
                parent,
                "Living Guild Hub Homecoming 076",
                new Color(0.055f, 0.038f, 0.012f, 0.96f));
            AnchorLivingGuild074(celebration.rectTransform, LivingGuildHomecomingRegion076);
            M1PremiumUi.StylePanel(celebration, M1PremiumUi.Surface.Warning);
            var heading = RuntimeUi.AddText(
                celebration.transform,
                "Living Guild Hub Homecoming Heading 076",
                "THE PATROL IS HOME\n" + allRecruits.Length + " PEOPLE  •  " +
                unionCount + " UNIONS",
                19,
                TextAnchor.MiddleLeft,
                RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorLivingGuild074(heading.rectTransform, new Rect(0.020f, 0.075f, 0.220f, 0.850f));
            ConfigureResponsiveText062(heading, 13, 20);
            heading.raycastTarget = false;

            var cameoRects = LivingGuildHubHomecomingCameoRectsForVerification076();
            for (var index = 0; index < cameos.Length && index < cameoRects.Count; index++)
            {
                var captured = cameos[index];
                var cameo = RuntimeUi.AddButton(
                    celebration.transform,
                    "Living Guild Hub Homecoming Member " + captured.RecruitId + " 076",
                    captured.DisplayName + "\n" + captured.ObservedClass,
                    () => OpenLivingGuildMemberDevelopment076(captured.RecruitId),
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.018f, 0.040f, 0.060f, 0.98f));
                AnchorLivingGuild074(cameo.GetComponent<RectTransform>(), cameoRects[index]);
                AddPortraitToButton(cameo, captured, large: false, cropToFill: true);
                ConfigureResponsiveText062(cameo.GetComponentInChildren<Text>(), 12, 17);
            }
        }

        private static string LivingGuildAuthorityId076(M1RecruitLoadoutView recruit) =>
            !string.IsNullOrWhiteSpace(recruit?.PortraitAuthorityId)
                ? recruit.PortraitAuthorityId
                : recruit?.RecruitId;

        private List<Button> BuildLivingGuildRoster074(
            Transform parent,
            GuildCity017D.GuildCityPresentationState017D state,
            bool homecoming)
        {
            var roster = RuntimeUi.AddPanel(
                parent,
                "Living Guild Hub Roster 074",
                new Color(0.01f, 0.018f, 0.03f, 0.95f));
            AnchorLivingGuild074(roster.rectTransform, LivingGuildRosterRegion074);
            M1PremiumUi.StylePanel(roster, M1PremiumUi.Surface.WorldGlass);

            var allRecruits = (_coordinator?.State?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var featuredIds = LivingGuildHubFeaturedAuthorityIdsForVerification076(
                allRecruits.Select(LivingGuildAuthorityId076).ToArray(),
                homecoming);
            var recruits = featuredIds
                .Select(authorityId => allRecruits.FirstOrDefault(value =>
                    StringComparer.Ordinal.Equals(LivingGuildAuthorityId076(value), authorityId)))
                .Where(value => value != null)
                .ToArray();
            var totalRecruitCount = Math.Max(allRecruits.Length, state?.TotalRecruitCount ?? 0);
            var rosterCapacity = Math.Max(totalRecruitCount, state?.RosterCapacity ?? 0);
            var heading = RuntimeUi.AddText(
                roster.transform,
                "Living Guild Hub Roster Heading 074",
                LivingGuildHubRosterHeadingForVerification079(
                    totalRecruitCount,
                    rosterCapacity,
                    recruits.Length),
                18,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorLivingGuild074(heading.rectTransform, new Rect(0.015f, 0.805f, 0.970f, 0.160f));
            heading.raycastTarget = false;
            ConfigureResponsiveText062(heading, 14, 18);

            var buttons = new List<Button>(recruits.Length);
            if (recruits.Length == 0)
            {
                var empty = RuntimeUi.AddText(
                    roster.transform,
                    "Living Guild Hub Empty Roster 074",
                    "Your signed adventurers will gather here.",
                    22,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.MutedText,
                    FontStyle.Bold);
                AnchorLivingGuild074(empty.rectTransform, new Rect(0.13f, 0.10f, 0.84f, 0.80f));
                empty.raycastTarget = false;
                return buttons;
            }

            var cardRects = LivingGuildHubRosterCardRectsForVerification074(recruits.Length);
            for (var index = 0; index < recruits.Length; index++)
            {
                var captured = recruits[index];
                var status = LivingGuildHubRosterReadinessForVerification074(captured.IsLegal);
                var member = RuntimeUi.AddButton(
                    roster.transform,
                    "Living Guild Hub Roster " + captured.RecruitId + " 074",
                    LivingGuildHubRosterIdentityForVerification074(
                        captured.DisplayName,
                        captured.ObservedClass,
                        captured.Level),
                    () => OpenLivingGuildMemberDevelopment076(captured.RecruitId),
                    RuntimeUi.MinimumTouchPixels,
                    new Color(0.025f, 0.045f, 0.055f, 0.96f));
                AnchorLivingGuild074(
                    member.GetComponent<RectTransform>(),
                    cardRects[index]);
                AddPortraitToButton(member, captured, large: false, cropToFill: true);
                var portrait = member.transform.Find("Portrait Frame " + captured.RecruitId)
                    ?.GetComponent<RectTransform>();
                if (portrait != null)
                {
                    portrait.anchorMin = new Vector2(0.015f, 0.10f);
                    portrait.anchorMax = new Vector2(0.405f, 0.90f);
                    portrait.offsetMin = Vector2.zero;
                    portrait.offsetMax = Vector2.zero;
                }
                var label = member.transform.Find("Label")?.GetComponent<Text>();
                if (label != null)
                {
                    AnchorLivingGuild074(label.rectTransform, LivingGuildRosterIdentityRegion074);
                    ConfigureAuthoredCompactText076(
                        label,
                        LivingGuildHubRosterIdentityMinimumFontSize076,
                        LivingGuildHubRosterIdentityMaximumFontSize076);
                    label.horizontalOverflow = HorizontalWrapMode.Overflow;
                    label.lineSpacing = 0.94f;
                    label.alignment = TextAnchor.MiddleLeft;
                    label.verticalOverflow = VerticalWrapMode.Truncate;
                }

                var readiness = RuntimeUi.AddText(
                    member.transform,
                    "Living Guild Hub Roster Readiness " + captured.RecruitId + " 074",
                    status,
                    17,
                    TextAnchor.MiddleLeft,
                    captured.IsLegal ? RuntimeUi.Positive : RuntimeUi.Warning,
                    FontStyle.Bold);
                AnchorLivingGuild074(readiness.rectTransform, LivingGuildRosterReadinessRegion074);
                readiness.raycastTarget = false;
                ConfigureResponsiveText062(readiness, 13, 17);
                readiness.verticalOverflow = VerticalWrapMode.Truncate;
                buttons.Add(member);
            }
            return buttons;
        }

        private void OpenLivingGuildMemberDevelopment076(string preferredRecruitId)
        {
            if (!string.IsNullOrWhiteSpace(preferredRecruitId))
                _selectedRecruitId = preferredRecruitId;
            else if (string.IsNullOrWhiteSpace(_selectedRecruitId))
                _selectedRecruitId = (_coordinator?.State?.Recruits ??
                                      Array.Empty<M1RecruitLoadoutView>())
                    .FirstOrDefault(value => value != null)?.RecruitId;
            _screen = M1Screen.GuildOperations;
            _guildCityTab017D = "DEVELOPMENT";
            _guildCityMoreOpen060 = false;
            BuildCurrentScreen();
        }

        private void InvokeLivingGuildFacility074(string destinationId)
        {
            if (StringComparer.Ordinal.Equals(destinationId, LivingGuildHubTowerDestinationId081) &&
                TryRouteLoopService164("ABYSS")) return;
            if (StringComparer.Ordinal.Equals(
                        destinationId,
                        GuildCity017D.WalkableGuildHall069.RecruitmentDestinationId069))
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "APPLICANTS";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        GuildCity017D.WalkableGuildHall069.ArmoryDestinationId069))
                OpenHallInventory069();
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        GuildCity017D.WalkableGuildHall069.PartyDestinationId069))
                OpenHallParty069();
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        GuildCity017D.WalkableGuildHall069.ContractDestinationId069))
            {
                OpenMissions084();
            }
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        LivingGuildHubCityDestinationId078))
            {
                _guildCityTab017D = "CITY";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        LivingGuildHubCodesDestinationId084))
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "CODES";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
            else if (StringComparer.Ordinal.Equals(
                        destinationId,
                        LivingGuildHubTowerDestinationId081))
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "ABYSS";
                _guildCityMoreOpen060 = false;
                BuildCurrentScreen();
            }
        }

        private static string LivingGuildFacilityLabel074(string destinationId)
        {
            var match = LivingGuildFacilities074.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.Id, destinationId));
            return match != null ? match.Label : "GUILD GUIDE";
        }

        private static void ConfigureLivingGuildNavigation074(
            IReadOnlyList<Button> facilities,
            Button primary,
            IReadOnlyList<Button> roster)
        {
            const int columns = 4;
            for (var index = 0; index < facilities.Count; index++)
            {
                var current = facilities[index];
                if (current == null) continue;
                var row = index / columns;
                var column = index % columns;
                var rowStart = row * columns;
                var rowEnd = Math.Min(facilities.Count - 1, rowStart + columns - 1);
                var navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = column > 0
                    ? facilities[index - 1]
                    : primary ?? facilities[rowEnd];
                navigation.selectOnRight = index < rowEnd
                    ? facilities[index + 1]
                    : primary ?? facilities[rowStart];
                navigation.selectOnUp = index >= columns
                    ? facilities[index - columns]
                    : primary;
                navigation.selectOnDown = index + columns < facilities.Count
                    ? facilities[index + columns]
                    : primary;
                current.navigation = navigation;
            }

            if (primary != null)
            {
                var navigation = primary.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                if (facilities.Count > 0)
                {
                    navigation.selectOnLeft = facilities[Math.Min(2, facilities.Count - 1)];
                    navigation.selectOnRight = facilities[facilities.Count - 1];
                    navigation.selectOnDown = facilities[0];
                }
                if (roster != null && roster.Count > 0) navigation.selectOnUp = roster[0];
                primary.navigation = navigation;
            }

            if (roster == null) return;
            for (var index = 0; index < roster.Count; index++)
            {
                var current = roster[index];
                if (current == null) continue;
                var navigation = current.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnLeft = roster[(index - 1 + roster.Count) % roster.Count];
                navigation.selectOnRight = roster[(index + 1) % roster.Count];
                navigation.selectOnDown = primary ??
                    (facilities.Count > 0 ? facilities[Math.Min(index, facilities.Count - 1)] : null);
                current.navigation = navigation;
            }
        }

        private static void AnchorLivingGuild074(RectTransform rect, Rect anchors)
        {
            if (rect == null) return;
            rect.anchorMin = new Vector2(anchors.xMin, anchors.yMin);
            rect.anchorMax = new Vector2(anchors.xMax, anchors.yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
