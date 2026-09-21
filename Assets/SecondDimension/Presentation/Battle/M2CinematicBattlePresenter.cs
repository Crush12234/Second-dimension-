using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private sealed class BattleCombatantRuntimeView
        {
            public string MemberId;
            public string DisplayName;
            public string VisualSeed;
            public string RaceId;
            public string PortraitAuthorityId;
            public string EnemyArtBaseId090;
            public string EnemyArtVariantId090;
            public bool Enemy;
            public RectTransform Root;
            public Image Artwork;
            public Image ActionArtwork;
            public EnemyArt700SpriteLease090 EnemyArtIdleLease090;
            public EnemyArt700SpriteLease090 EnemyArtActionLease090;
            public CanvasGroup Canvas;
            public Vector2 Home;
            public Quaternion HomeRotation;
            public int CurrentHp;
            public int MaximumHp;
            public Image HpFill;
            public Text HpLabel;
        }

        private M2BattleView _battlePresentationBefore;
        private M2BattleView _battlePresentationAfter;
        private readonly Dictionary<string, BattleCombatantRuntimeView> _battleCombatants =
            new Dictionary<string, BattleCombatantRuntimeView>(StringComparer.Ordinal);
        private readonly List<EnemyArt700SpriteLease090> _battleTransientEnemyArtLeases090 =
            new List<EnemyArt700SpriteLease090>();
        private RectTransform _battlefieldCameraRoot;
        private RectTransform _battleEffectRoot;
        private Text _battleCinematicCaption;
        private Text _battlePhaseCaption;
        private bool _skipCurrentBattleBeat;
        private bool _battleAnimationPaused;
        private bool _reducedFlash;
        private float _battleShakeStrength = 1f;
        private bool _battleEntranceSeen;
        private M2BattleAudioDirector _battleAudio;
        private M2BattleVfxPool _battleVfx;
        private bool _battleOptionsOpen;

        private void BuildCinematicBattleScreen(M2BattleView battle)
        {
            var root = CreateCinematicBattleRoot("Cinematic Union Command Battle");
            AddCinematicBattleTopRibbon(root, battle, false);
            var arena = AddAnchoredPanel(root, "Live Cinematic Battlefield", Color.clear,
                new Vector2(0f, 0f), new Vector2(1f, 0.885f), Vector2.zero, Vector2.zero);
            BuildCinematicArena(arena.rectTransform, battle, false);
            BuildCinematicCommandTray(root, battle);
            if (!_battleEntranceSeen)
            {
                _battleEntranceSeen = true;
                StartCoroutine(FadeInCinematicBattle(root));
            }
        }

        private void BuildCinematicResolutionScreen(M2BattleView before)
        {
            var root = CreateCinematicBattleRoot("Cinematic Round Resolution");
            AddCinematicBattleTopRibbon(root, before, true);
            var arena = AddAnchoredPanel(root, "Live Cinematic Battlefield", Color.clear,
                new Vector2(0f, 0.095f), new Vector2(1f, 0.885f), Vector2.zero, Vector2.zero);
            BuildCinematicArena(arena.rectTransform, before, true);
            BuildResolutionControls(root);
        }

        private void BuildCinematicBattleResultScreen(M2BattleView battle)
        {
            // Results are a clean payoff screen, not another live battlefield.
            // Leaving the 3D arena active leaked its SELECT A UNION overlay through
            // the victory dashboard in the player capture.
            SetCinematic3DWorldVisible(false);
            var root = CreateCinematicBattleRoot("Cinematic Battle Result");
            AddCinematicBattleTopRibbon(root, battle, false, result: true);
            var arena = AddAnchoredPanel(root, "Result Cinematic Battlefield", new Color(0.01f, 0.02f, 0.035f, 1f),
                new Vector2(0f, 0.205f), new Vector2(1f, 0.885f), Vector2.zero, Vector2.zero);
            if (M1VisualAssets.TryResolveBattleBackdrop(battle.BattleId, out var resultBackdrop, out _))
            {
                arena.sprite = resultBackdrop;
                arena.preserveAspect = false;
                arena.color = new Color(0.42f, 0.47f, 0.54f, 1f);
            }
            var resultVeil = AddAnchoredPanel(arena.transform, "Battle Result Artwork Veil",
                new Color(0.005f, 0.012f, 0.025f, 0.72f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            resultVeil.raycastTarget = false;

            var victory = StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory");
            var retreat = StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Retreat");
            var resultColor = victory ? RuntimeUi.Positive : retreat ? RuntimeUi.Warning : RuntimeUi.Error;
            var resultPanel = AddAnchoredPanel(arena.transform, "Cinematic Result Progression Dashboard 021",
                new Color(0.02f, 0.025f, 0.045f, 0.90f),
                new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.95f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(resultPanel, victory ? M1PremiumUi.Surface.EtchedGlass : M1PremiumUi.Surface.Warning);
            AddAnchoredText(resultPanel.transform, "Battle Result Heading", battle.Outcome.ToUpperInvariant(), 70,
                TextAnchor.MiddleCenter, resultColor, FontStyle.Bold,
                new Vector2(0.04f, 0.84f), new Vector2(0.96f, 0.985f));
            AddAnchoredText(resultPanel.transform, "Battle Result Rounds",
                "BATTLE COMPLETE  ·  ROUNDS RESOLVED " + Math.Max(1, battle.LastResolvedRound) +
                "  ·  " + BattleRewardStatus(battle.Reward), 25,
                TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.05f, 0.76f), new Vector2(0.95f, 0.845f));

            AddBattleResultCategory(
                resultPanel.transform,
                "Character XP Results 021",
                "PARTY GROWTH",
                CompactPartyRewardSummary071(battle.Reward),
                RuntimeUi.Positive,
                new Vector2(0.02f, 0.06f),
                new Vector2(0.325f, 0.75f));
            AddBattleResultCategory(
                resultPanel.transform,
                "Use Based Art Mastery Results 021",
                "ART MASTERY",
                CompactArtRewardSummary071(battle),
                RuntimeUi.Warning,
                new Vector2(0.345f, 0.06f),
                new Vector2(0.655f, 0.75f));
            AddBattleResultCategory(
                resultPanel.transform,
                "Guild Treasury Results 021",
                "GUILD SPOILS",
                CompactGuildSpoilsSummary071(battle.Reward),
                RuntimeUi.Accent,
                new Vector2(0.675f, 0.06f),
                new Vector2(0.98f, 0.75f));

            var actions = AddAnchoredPanel(root, "Cinematic Result Actions",
                new Color(0.02f, 0.035f, 0.06f, 0.96f),
                new Vector2(0f, 0f), new Vector2(1f, 0.19f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(actions, M1PremiumUi.Surface.Iron);
            var guildCityCoordinator = _coordinator as GuildCity017D.IGuildCityPresentationCoordinator017D;
            var guildEncounter = guildCityCoordinator?.GuildCity017D?.HasPendingEncounter == true;
            var activeTowerRun081 = HasActiveTowerRun081();
            var towerVictory081 = StringComparer.OrdinalIgnoreCase.Equals(battle.Outcome, "Victory");
            if (activeTowerRun081)
            {
                var towerAction = AddAnchoredButton(
                    actions.transform,
                    towerVictory081
                        ? "Claim Reward And Return To Tower 081"
                        : "Return From Defeated Tower Battle 081",
                    towerVictory081
                        ? "CLAIM & RETURN TO TOWER"
                        : "RETURN TO GUILD • RETRY LATER",
                    towerVictory081
                        ? (Action)ClaimBattleRewardsAndReturnToGuild
                        : ReturnFromDefeatedTowerBattle081,
                    towerVictory081 ? RuntimeUi.Accent : RuntimeUi.Warning,
                    new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.88f));
                SetButtonFont(towerAction, 30);
            }
            else if (!guildEncounter)
            {
                var retry = AddAnchoredButton(actions.transform, "Claim XP And Retry Tutorial Battle 021", "CLAIM XP & RETRY",
                    ClaimBattleRewardsAndRetry, RuntimeUi.Accent, new Vector2(0.08f, 0.20f), new Vector2(0.47f, 0.88f));
                SetButtonFont(retry, 30);
                var returnToGuild = AddAnchoredButton(actions.transform, "Claim XP And Return To Guild 021", "CLAIM XP & RETURN TO GUILD",
                    ClaimBattleRewardsAndReturnToGuild, RuntimeUi.ButtonNormal,
                    new Vector2(0.53f, 0.20f), new Vector2(0.92f, 0.88f));
                SetButtonFont(returnToGuild, 30);
            }
            else
            {
                var returnToExpedition = AddAnchoredButton(actions.transform, "Claim Reward And Return To Expedition 017D",
                    "CLAIM & RETURN TO LANTERN ROAD", ClaimBattleRewardsAndReturnToGuild, RuntimeUi.Accent,
                    new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.88f));
                SetButtonFont(returnToExpedition, 30);
            }

            if (!string.IsNullOrWhiteSpace(_localStatus))
                AddAnchoredText(root, "Battle Result Status", _localStatus, 25, TextAnchor.MiddleCenter,
                    _localStatusPositive ? RuntimeUi.Positive : RuntimeUi.Error, FontStyle.Bold,
                    new Vector2(0.25f, 0.19f), new Vector2(0.75f, 0.235f));
        }

        private void ReturnFromDefeatedTowerBattle081()
        {
            var tower = _coordinator as Campaign022.ICampaignProgressionPresentationCoordinator022;
            var result = tower?.RetreatTowerRun081();
            _localStatus = result?.Message ?? "The defeated Tower run could not return to the Guild.";
            _localStatusPositive = result != null && result.Succeeded;
            if (!_localStatusPositive)
            {
                BuildCurrentScreen();
                return;
            }
            _guildCityTab017D = "ABYSS";
            _guildCityMoreOpen060 = false;
            Navigate(M1Screen.GuildOperations);
        }

        private void ClaimBattleRewardsAndRetry()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            var claim = m2.ClaimBattleRewards();
            _localStatus = claim?.Message ?? "The XP claim returned no result.";
            _localStatusPositive = claim != null && claim.Succeeded;
            if (claim == null || !claim.Succeeded)
            {
                BuildCurrentScreen();
                return;
            }

            RetryBattle();
        }

        private void ClaimBattleRewardsAndReturnToGuild()
        {
            var m2 = _coordinator as IM2PresentationCoordinator;
            if (m2 == null) return;
            var claim = m2.ClaimBattleRewards();
            _localStatus = claim?.Message ?? "The XP claim returned no result.";
            _localStatusPositive = claim != null && claim.Succeeded;
            if (claim == null || !claim.Succeeded)
            {
                // A failed claim never discards the result screen or its pending reward.
                BuildCurrentScreen();
                return;
            }

            if (HasActiveTowerRun081())
            {
                _guildCityTab017D = "ABYSS";
                _guildCityMoreOpen060 = false;
                Navigate(M1Screen.GuildOperations);
            }
            else if (_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D guildCity &&
                guildCity.GuildCity017D?.Expedition != null)
            {
                _screen = M1Screen.GuildOperations;
                _guildCityTab017D = "EXPEDITION";
                _guildCityMoreOpen060 = false;
                EnterExpeditionBoard074(guildCity);
            }
            else if (_coordinator is GuildCity017D.IGuildCityPresentationCoordinator017D)
            {
                _guildCityTab017D = "HALL";
                Navigate(M1Screen.GuildOperations);
            }
            else
            {
                Navigate(M1Screen.Complete);
            }
        }

        private RectTransform CreateCinematicBattleRoot(string name)
        {
            ReleaseBattleEnemyArtLeases090();
            RuntimeUi.ClearChildren(_screenRoot);
            _activePage = null;
            _activeContent = null;
            _activeScroll = null;
            _battleCombatants.Clear();
            _battlefieldCameraRoot = null;
            _battleEffectRoot = null;
            _battleEventBanner = null;
            _battleCinematicCaption = null;
            _battlePhaseCaption = null;
            _battleVfx = null;

            // The UI remains a ScreenSpaceOverlay, but its root must be transparent so
            // the dedicated perspective camera can render the arena behind the HUD.
            // The legacy arena frame paints its own opaque fallback if 3D startup fails.
            var root = RuntimeUi.AddPanel(_screenRoot, name, Color.clear);
            Stretch(root.rectTransform);
            _battleAudio = EnsureBattleComponent<M2BattleAudioDirector>(gameObject);
            return root.rectTransform;
        }

        private void AddCinematicBattleTopRibbon(
            Transform root,
            M2BattleView battle,
            bool resolving,
            bool result = false)
        {
            var outcome = string.IsNullOrWhiteSpace(battle?.Outcome) ? "RESOLVED" : battle.Outcome.ToUpperInvariant();
            var header = AddAnchoredPanel(root, "Cinematic Battle Header",
                new Color(0.012f, 0.026f, 0.044f, 0.94f),
                new Vector2(0f, 0.89f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(header, M1PremiumUi.Surface.EtchedGlass);
            header.color = new Color(1f, 1f, 1f, _highContrast ? 1f : 0.90f);
            AddAnchoredText(header.transform, "Cinematic Battle Round",
                result
                    ? "BATTLE COMPLETE · " + outcome
                    : resolving
                    ? "ROUND " + (_battlePresentationBefore?.Round ?? battle.Round) + " · COMMANDS IN MOTION"
                    : "ROUND " + battle.Round + " · CHOOSE COMPLETE UNION COMMANDS",
                42, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.025f, 0.40f), new Vector2(0.565f, 0.92f));
            var heading = header.transform.Find("Cinematic Battle Round")?.GetComponent<Text>();
            if (heading != null) M1PremiumUi.ConfigureDisplayText(heading);
            AddAnchoredText(header.transform, "Cinematic Battle Objective",
                result ? "THE GUILD RETURNS WITH A COMPLETE BATTLE RECORD" : battle.Objective, 28,
                TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.026f, 0.06f), new Vector2(0.565f, 0.42f));

            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var ready = active.Count(value => value.IsSelected);
            AddAnchoredText(header.transform, "Cinematic Command Progress",
                result ? outcome : resolving ? "MEMBER ARTS IN MOTION" : ready + " / " + active.Length + " UNIONS READY",
                34, TextAnchor.MiddleCenter, resolving ? BattleCohesion : BattleAp, FontStyle.Bold,
                new Vector2(0.575f, 0.18f), new Vector2(0.79f, 0.84f));
            AddAnchoredText(header.transform, "Cinematic No Direct Arts",
                result ? "BATTLE RECORD SECURED" : resolving ? "UNION ORDER LOCKED\nWATCH THE ROUND UNFOLD" : "CHOOSE ONE ORDER PER UNION\nMEMBERS ACT AUTOMATICALLY",
                27, TextAnchor.MiddleRight, RuntimeUi.Accent, FontStyle.Bold,
                new Vector2(0.805f, 0.10f), new Vector2(0.975f, 0.90f));
        }

        private void BuildCinematicArena(RectTransform parent, M2BattleView battle, bool resolving)
        {
            var frame = RuntimeUi.AddPanel(parent, "Illustrated Gateworks Arena", new Color(0.01f, 0.02f, 0.035f, 1f));
            Stretch(frame.rectTransform);
            M1PremiumUi.StylePanel(frame, M1PremiumUi.Surface.Iron);

            _battlefieldCameraRoot = RuntimeUi.AddStretchRect(frame.transform, "Battlefield Camera Root");
            EnsureBattleComponent<RectMask2D>(_battlefieldCameraRoot.gameObject);
            var background = RuntimeUi.AddPanel(_battlefieldCameraRoot, "Gateworks Arena Artwork", Color.white);
            Stretch(background.rectTransform);
            background.raycastTarget = false;
            background.preserveAspect = false;
            if (M1VisualAssets.TryResolveBattleBackdrop(battle.BattleId, out var arenaSprite, out _))
            {
                background.sprite = arenaSprite;
                // Preserve the authored camera and crop its edges when the Game view
                // is narrower than the 2796x1290 reference. Stretching the painting
                // made every figure and arch look squat in tablet/free-aspect views.
                var fitter = EnsureBattleComponent<AspectRatioFitter>(background.gameObject);
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = arenaSprite.rect.width / Mathf.Max(1f, arenaSprite.rect.height);
            }
            else background.color = new Color(0.025f, 0.075f, 0.11f, 1f);
            var shade = RuntimeUi.AddPanel(_battlefieldCameraRoot, "Battlefield Depth Shade",
                new Color(0.01f, 0.02f, 0.04f, _highContrast ? 0.42f : 0.18f));
            Stretch(shade.rectTransform);
            shade.raycastTarget = false;

            if (resolving)
            {
                BuildPlayerUnionStaging(_battlefieldCameraRoot, battle.PlayerUnions, true);
                BuildEnemyUnionStaging(_battlefieldCameraRoot, battle.EnemyUnions, true);
            }
            else
            {
                BuildCommandPhaseUnionStaging(_battlefieldCameraRoot, battle);
            }

            _battleEffectRoot = RuntimeUi.AddStretchRect(frame.transform, "Pooled Cinematic Effects");
            _battleEffectRoot.SetAsLastSibling();
            _battleVfx = EnsureBattleComponent<M2BattleVfxPool>(frame.gameObject);
            _battleVfx.Configure(_battleEffectRoot);
            _battleEventBanner = AddAnchoredText(frame.transform, "Cinematic Event Family",
                resolving ? "COMMANDS COMMITTED" : "SELECT A UNION COMMAND",
                44, TextAnchor.MiddleCenter, resolving ? RuntimeUi.Warning : RuntimeUi.Accent, FontStyle.Bold,
                new Vector2(0.37f, 0.875f), new Vector2(0.63f, 0.975f));
            M1PremiumUi.ConfigureDisplayText(_battleEventBanner);
            _battleCinematicCaption = AddAnchoredText(frame.transform, "Cinematic Event Caption",
                resolving ? "The predicted Arts now resolve as one connected round." : string.Empty,
                25, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.18f, 0.17f), new Vector2(0.82f, 0.255f));
            _battlePhaseCaption = AddAnchoredText(frame.transform, "Cinematic Relationship Caption",
                string.Empty, 22,
                TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.20f, 0.255f), new Vector2(0.80f, 0.32f));

            // Build every legacy child first. PlayMode guardrails and the fallback rely
            // on those stable names; only then may the perspective world hide its paint.
            TryActivateCinematic3DBattle(battle, resolving, frame, _battlefieldCameraRoot);
        }

        private void AddBattleRelationshipRibbon(Transform parent, M2BattleView battle)
        {
            var ribbon = AddAnchoredPanel(parent, "Logical Engagement Ribbon",
                new Color(0.025f, 0.055f, 0.08f, 0.84f),
                new Vector2(0.20f, 0.74f), new Vector2(0.80f, 0.84f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(ribbon, M1PremiumUi.Surface.EtchedGlass);
            var relationshipIcon = AddAnchoredPanel(ribbon.transform, "Primary Relationship Icon 011", Color.white,
                new Vector2(0.015f, 0.12f), new Vector2(0.085f, 0.88f), Vector2.zero, Vector2.zero);
            ApplyBattleArtUiAsset011(relationshipIcon, RelationshipIconAssetId011(battle), Color.white, false);
            relationshipIcon.raycastTarget = false;
            AddAnchoredText(ribbon.transform, "Logical Engagement State", RelationshipSummary(battle), 24,
                TextAnchor.MiddleCenter, BattleCohesion, FontStyle.Bold,
                new Vector2(0.09f, 0.05f), new Vector2(0.985f, 0.95f));
        }

        private void BuildPlayerUnionStaging(
            Transform parent,
            IReadOnlyList<M2BattleUnionView> unions,
            bool resolving)
        {
            var visible = unions.Where(value => value.Members.Count > 0).ToArray();
            var largeUnionCount = Math.Min(2, visible.Length);
            for (var index = 0; index < largeUnionCount; index++)
            {
                if (!resolving)
                {
                    var minX = largeUnionCount == 1 ? 0.015f : index == 0 ? 0.015f : 0.245f;
                    var maxX = largeUnionCount == 1 ? 0.47f : index == 0 ? 0.24f : 0.47f;
                    AddCinematicUnionGroup(parent, visible[index], false,
                        new Vector2(minX, 0.43f), new Vector2(maxX, 0.82f), index);
                    continue;
                }
                var minY = largeUnionCount == 1 ? 0.16f : index == 0 ? 0.42f : 0.10f;
                var maxY = largeUnionCount == 1 ? 0.72f : index == 0 ? 0.72f : 0.40f;
                AddCinematicUnionGroup(parent, visible[index], false,
                    new Vector2(0.015f, minY), new Vector2(0.46f, maxY), index);
            }
            AddCompactUnionStagingBadges020(parent, visible.Skip(largeUnionCount).ToArray(), false);
        }

        private void BuildEnemyUnionStaging(
            Transform parent,
            IReadOnlyList<M2BattleUnionView> unions,
            bool resolving)
        {
            var visible = unions.Where(value => value.Members.Count > 0).ToArray();
            var largeUnionCount = Math.Min(2, visible.Length);
            for (var index = 0; index < largeUnionCount; index++)
            {
                if (!resolving)
                {
                    var minX = largeUnionCount == 1 ? 0.53f : index == 0 ? 0.53f : 0.76f;
                    var maxX = largeUnionCount == 1 ? 0.985f : index == 0 ? 0.755f : 0.985f;
                    AddCinematicUnionGroup(parent, visible[index], true,
                        new Vector2(minX, 0.43f), new Vector2(maxX, 0.82f), index);
                    continue;
                }
                var minY = largeUnionCount == 1 ? 0.15f : index == 0 ? 0.42f : 0.10f;
                var maxY = largeUnionCount == 1 ? 0.71f : index == 0 ? 0.72f : 0.40f;
                AddCinematicUnionGroup(parent, visible[index], true,
                    new Vector2(0.54f, minY), new Vector2(0.985f, maxY), index);
            }
            AddCompactUnionStagingBadges020(parent, visible.Skip(largeUnionCount).ToArray(), true);
        }

        private void AddCompactUnionStagingBadges020(
            Transform parent,
            IReadOnlyList<M2BattleUnionView> unions,
            bool enemy)
        {
            if (parent == null || unions == null || unions.Count == 0) return;
            const int columns = 4;
            var rows = Math.Max(1, Mathf.CeilToInt(unions.Count / (float)columns));
            var sideMin = enemy ? 0.53f : 0.015f;
            var sideMax = enemy ? 0.985f : 0.47f;
            const float bandMin = 0.835f;
            const float bandMax = 0.98f;
            for (var index = 0; index < unions.Count; index++)
            {
                var union = unions[index];
                var row = index / columns;
                var column = index % columns;
                var cellWidth = (sideMax - sideMin) / columns;
                var cellHeight = (bandMax - bandMin) / rows;
                var minX = sideMin + cellWidth * column;
                var maxX = minX + cellWidth;
                var maxY = bandMax - cellHeight * row;
                var minY = maxY - cellHeight;
                var badge = AddAnchoredPanel(parent,
                    (enemy ? "Enemy" : "Player") + " Compact Union Badge 020 " + union.UnionId,
                    new Color(enemy ? 0.18f : 0.018f, enemy ? 0.025f : 0.075f,
                        enemy ? 0.04f : 0.12f, 0.92f),
                    new Vector2(minX + 0.003f, minY + 0.004f),
                    new Vector2(maxX - 0.003f, maxY - 0.004f), Vector2.zero, Vector2.zero);
                M1PremiumUi.StylePanel(badge, M1PremiumUi.Surface.EtchedGlass);
                ApplyBattleArtUiAsset011(badge, enemy ? "UI_UNION_PLATE_ENEMY" : "UI_UNION_PLATE_ALLY",
                    new Color(1f, 1f, 1f, 0.94f));
                var status = enemy
                    ? union.Engagement.ToUpperInvariant()
                    : union.IsSelected ? "ORDER SET" : "AWAITING ORDER";
                var label = AddAnchoredText(badge.transform, "Compact Union Badge Label 020 " + union.UnionId,
                    union.DisplayName.ToUpperInvariant() + "\n" + status, 16,
                    TextAnchor.MiddleCenter, enemy ? BattleEnemy : BattleCohesion, FontStyle.Bold,
                    new Vector2(0.025f, 0.05f), new Vector2(0.975f, 0.95f));
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 16;
            }
        }

        private void AddCinematicUnionGroup(
            Transform parent,
            M2BattleUnionView union,
            bool enemy,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int unionOrdinal)
        {
            var group = AddAnchoredPanel(parent, (enemy ? "Enemy" : "Player") + " Cinematic Union " + union.UnionId,
                new Color(enemy ? 0.20f : 0.025f, enemy ? 0.035f : 0.09f, enemy ? 0.05f : 0.14f, 0.10f),
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var rail = AddAnchoredPanel(group.transform, "Union Resource Ribbon " + union.UnionId,
                new Color(0.015f, 0.025f, 0.045f, 0.91f),
                new Vector2(0f, 0.76f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(rail, M1PremiumUi.Surface.Iron);
            ApplyBattleArtUiAsset011(rail, enemy ? "UI_UNION_PLATE_ENEMY" : "UI_UNION_PLATE_ALLY",
                new Color(1f, 1f, 1f, 0.96f));
            AddAnchoredText(rail.transform, "Union Battle Name " + union.UnionId,
                (enemy ? "◆ " : "◇ ") + union.DisplayName.ToUpperInvariant(), 28,
                TextAnchor.MiddleLeft, enemy ? BattleEnemy : BattleCohesion, FontStyle.Bold,
                new Vector2(0.025f, 0.44f), new Vector2(0.57f, 0.96f));
            AddAnchoredText(rail.transform, "Union Battle Resources " + union.UnionId,
                "HP " + union.Members.Sum(value => value.CurrentHp) + "/" + union.Members.Sum(value => value.MaximumHp) +
                "   AP " + union.CurrentAp + "/" + union.MaximumAp + "   COH " + union.Cohesion +
                "   FORM " + union.FormationConditionPercent + "%", 22,
                TextAnchor.MiddleRight, BattleAp, FontStyle.Bold,
                new Vector2(0.42f, 0.46f), new Vector2(0.98f, 0.94f));
            AddAnchoredText(rail.transform, "Union Battle State " + union.UnionId,
                union.Formation.ToUpperInvariant() + " · " + union.Engagement.ToUpperInvariant(), 20,
                TextAnchor.MiddleCenter, union.FormationBenefitActive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.45f));

            var count = Math.Max(1, union.Members.Count);
            for (var memberIndex = 0; memberIndex < union.Members.Count; memberIndex++)
            {
                var width = Mathf.Min(0.34f, 0.92f / count);
                var center = count == 1 ? 0.5f : 0.08f + (0.84f * memberIndex / Math.Max(1, count - 1));
                var minX = Mathf.Clamp(center - width * 0.5f, 0.01f, 0.99f - width);
                var maxX = minX + width;
                var rise = memberIndex % 2 == 0 ? 0f : 0.035f;
                AddCinematicCombatant(group.transform, union, union.Members[memberIndex], enemy,
                    new Vector2(minX, 0.02f + rise), new Vector2(maxX, 0.75f + rise),
                    memberIndex, unionOrdinal);
            }
        }

        private void AddCinematicCombatant(
            Transform parent,
            M2BattleUnionView union,
            M2BattleMemberView member,
            bool enemy,
            Vector2 anchorMin,
            Vector2 anchorMax,
            int memberOrdinal,
            int unionOrdinal,
            bool showMemberHud = true,
            bool showMemberMp = true)
        {
            var root = AddAnchoredPanel(parent, "Combatant View " + member.MemberId, Color.clear,
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            root.rectTransform.localRotation = Quaternion.Euler(0f, 0f, enemy ? -1.4f : 1.4f);
            var shadow = AddAnchoredPanel(root.transform, "Combatant Ground Shadow " + member.MemberId,
                new Color(0f, 0f, 0f, 0.52f), new Vector2(0.10f, 0.01f), new Vector2(0.90f, 0.14f),
                Vector2.zero, Vector2.zero);
            shadow.rectTransform.localScale = new Vector3(1f, 0.34f, 1f);
            ApplyBattleArtUiAsset011(shadow, "SHARED_GROUND_SHADOW", new Color(1f, 1f, 1f, 0.72f), false);

            Sprite battleArt011Standee = null;
            Sprite enemyArt700Standee090 = null;
            EnemyArt700SpriteLease090 enemyArt700Lease090 = null;
            Sprite enemyStandee = null;
            Sprite playerStandee = null;
            var hasBattleArt011Standee = BattleArtRuntimeRegistry011.TryResolvePose(
                member.MemberId,
                member.Downed ? BattleArtPoseDirector011.Downed : BattleArtPoseDirector011.Idle,
                out battleArt011Standee,
                out _);
            var hasEnemyArt700Standee090 = enemy &&
                                           !string.IsNullOrWhiteSpace(member.EnemyArtBaseId090) &&
                                           !string.IsNullOrWhiteSpace(member.EnemyArtVariantId090) &&
                                           EnemyArt700Runtime090.TryAcquireSprite090(
                                               member.EnemyArtBaseId090,
                                               member.EnemyArtVariantId090,
                                               EnemyArt700Pose090.Idle,
                                               out enemyArt700Standee090,
                                               out enemyArt700Lease090,
                                               out _);
            var hasEnemyStandee = enemy && M1VisualAssets.TryResolveEnemyBattleStandee(
                member.MemberId,
                out enemyStandee,
                out _);
            var hasPlayerStandee = !enemy && M1VisualAssets.TryResolveBattleStandee(
                member.MemberId, member.VisualSeed, member.RaceId, member.PortraitAuthorityId,
                out playerStandee, out _);
            var usesCutout = hasEnemyArt700Standee090 || hasBattleArt011Standee ||
                             hasEnemyStandee || hasPlayerStandee;
            var portraitFrame = AddAnchoredPanel(root.transform, "Battle Portrait Frame " + member.MemberId,
                usesCutout
                    ? Color.clear
                    : new Color(enemy ? 0.20f : 0.025f, enemy ? 0.035f : 0.08f, enemy ? 0.05f : 0.13f, 0.96f),
                new Vector2(0.01f, showMemberHud ? 0.10f : 0.015f), new Vector2(0.99f, 1f), Vector2.zero, Vector2.zero);
            if (!usesCutout) M1PremiumUi.StylePortraitFrame(portraitFrame);
            var artwork = RuntimeUi.AddPanel(portraitFrame.transform, "Battle Character Artwork " + member.MemberId, Color.white);
            Stretch(artwork.rectTransform);
            artwork.rectTransform.offsetMin = usesCutout ? Vector2.zero : new Vector2(8f, 8f);
            artwork.rectTransform.offsetMax = usesCutout ? Vector2.zero : new Vector2(-8f, -8f);
            artwork.preserveAspect = true;
            artwork.raycastTarget = false;
            artwork.gameObject.AddComponent<M2BattleIdleMotion>().Configure(member.MemberId, _reducedMotion);

            if (enemy)
            {
                if (hasEnemyArt700Standee090) artwork.sprite = enemyArt700Standee090;
                else if (hasBattleArt011Standee) artwork.sprite = battleArt011Standee;
                else if (hasEnemyStandee) artwork.sprite = enemyStandee;
                if (hasEnemyArt700Standee090 || hasBattleArt011Standee || hasEnemyStandee)
                {
                    var tint = 1f - memberOrdinal * 0.08f - unionOrdinal * 0.05f;
                    artwork.color = new Color(tint, tint, tint, member.Downed ? 0.38f : 1f);
                }
                else
                {
                    var fallback = M1VisualAssets.FallbackPortraitColor(
                        member.RaceId, member.VisualSeed, member.MemberId);
                    artwork.color = new Color(fallback.r, fallback.g, fallback.b,
                        member.Downed ? 0.38f : 1f);
                    AddAnchoredText(portraitFrame.transform,
                        "Battle Enemy Family " + member.MemberId,
                        member.ClassSymbol,
                        92,
                        TextAnchor.MiddleCenter,
                        RuntimeUi.Text,
                        FontStyle.Bold,
                        new Vector2(0.05f, 0.08f),
                        new Vector2(0.95f, 0.92f));
                }
            }
            else if (hasBattleArt011Standee || hasPlayerStandee)
            {
                artwork.sprite = hasBattleArt011Standee ? battleArt011Standee : playerStandee;
                artwork.color = new Color(1f, 1f, 1f, member.Downed ? 0.38f : 1f);
            }
            else if (M1VisualAssets.TryResolvePortrait(
                         member.MemberId,
                         member.VisualSeed,
                         member.RaceId,
                         member.PortraitAuthorityId,
                         out var portraitSprite,
                         out _))
            {
                artwork.sprite = portraitSprite;
                artwork.color = new Color(1f, 1f, 1f, member.Downed ? 0.38f : 1f);
            }
            else
            {
                artwork.color = M1VisualAssets.FallbackPortraitColor(member.RaceId, member.VisualSeed, member.MemberId);
                AddAnchoredText(portraitFrame.transform, "Battle Fallback Class " + member.MemberId,
                    member.ClassSymbol, 92, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold,
                    new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.92f));
            }

            if (showMemberHud && StringComparer.Ordinal.Equals(member.MemberId, union.LeaderMemberId))
                AddAnchoredText(root.transform, "Battle Leader Crest " + member.MemberId, "LEADER ◆", 20,
                    TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold,
                    new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.99f));

            Image hpFill = null;
            Text hpLabel = null;
            if (showMemberHud)
            {
                var identity = AddAnchoredPanel(root.transform, "Battle Identity Plate " + member.MemberId,
                    new Color(0.008f, 0.016f, 0.03f, 0.86f),
                    new Vector2(0.01f, 0.00f), new Vector2(0.99f, showMemberMp ? 0.235f : 0.20f),
                    Vector2.zero, Vector2.zero);
                var memberName = AddAnchoredText(identity.transform, "Battle Member Name " + member.MemberId,
                    member.ClassSymbol + "  " + member.DisplayName, 24,
                    TextAnchor.MiddleCenter, enemy ? RuntimeUi.Text : M1PremiumUi.ClassColor(member.ClassName), FontStyle.Bold,
                    new Vector2(0.03f, showMemberMp ? 0.62f : 0.53f), new Vector2(0.97f, 0.98f));
                memberName.resizeTextForBestFit = true;
                memberName.resizeTextMinSize = 17;
                memberName.resizeTextMaxSize = 24;
                AddResourceBar(identity.transform, "Battle HP " + member.MemberId, member.CurrentHp, member.MaximumHp,
                    BattleHp, "HP " + member.CurrentHp + "/" + member.MaximumHp,
                    showMemberMp ? new Vector2(0.05f, 0.33f) : new Vector2(0.05f, 0.09f),
                    showMemberMp ? new Vector2(0.95f, 0.60f) : new Vector2(0.95f, 0.47f),
                    out hpFill, out hpLabel);
                if (showMemberMp)
                {
                    AddResourceBar(identity.transform, "Battle MP " + member.MemberId, member.CurrentMp, member.MaximumMp,
                        BattleMp, "MP " + member.CurrentMp + "/" + member.MaximumMp,
                        new Vector2(0.05f, 0.045f), new Vector2(0.95f, 0.30f), out _, out _);
                }
                if (member.Downed)
                    AddAnchoredText(root.transform, "Battle Downed State " + member.MemberId,
                        member.Stabilized ? "DOWNED · STABLE" : "DOWNED", 27,
                        TextAnchor.MiddleCenter, RuntimeUi.Error, FontStyle.Bold,
                        new Vector2(0.02f, 0.36f), new Vector2(0.98f, 0.60f));
            }

            var canvas = EnsureBattleComponent<CanvasGroup>(root.gameObject);
            var runtime = new BattleCombatantRuntimeView
            {
                MemberId = member.MemberId,
                DisplayName = member.DisplayName,
                VisualSeed = member.VisualSeed,
                RaceId = member.RaceId,
                PortraitAuthorityId = member.PortraitAuthorityId,
                EnemyArtBaseId090 = member.EnemyArtBaseId090,
                EnemyArtVariantId090 = member.EnemyArtVariantId090,
                Enemy = enemy,
                Root = root.rectTransform,
                Artwork = artwork,
                EnemyArtIdleLease090 = enemyArt700Lease090,
                Canvas = canvas,
                Home = root.rectTransform.anchoredPosition,
                HomeRotation = root.rectTransform.localRotation,
                CurrentHp = member.CurrentHp,
                MaximumHp = member.MaximumHp,
                HpFill = hpFill,
                HpLabel = hpLabel
            };
            if (_battleCombatants.TryGetValue(member.MemberId, out var replaced090) &&
                replaced090 != null)
            {
                replaced090.EnemyArtIdleLease090?.Dispose();
                replaced090.EnemyArtActionLease090?.Dispose();
            }
            _battleCombatants[member.MemberId] = runtime;
        }

        private void ReleaseBattleEnemyArtLeases090()
        {
            foreach (var pair090 in _battleCombatants)
            {
                var view090 = pair090.Value;
                if (view090 == null) continue;
                view090.EnemyArtIdleLease090?.Dispose();
                view090.EnemyArtIdleLease090 = null;
                view090.EnemyArtActionLease090?.Dispose();
                view090.EnemyArtActionLease090 = null;
            }
            for (var index090 = 0;
                 index090 < _battleTransientEnemyArtLeases090.Count;
                 index090++)
                _battleTransientEnemyArtLeases090[index090]?.Dispose();
            _battleTransientEnemyArtLeases090.Clear();
        }

        private void AddResourceBar(
            Transform parent,
            string name,
            int current,
            int maximum,
            Color color,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            out Image fill,
            out Text labelText)
        {
            var back = AddAnchoredPanel(parent, name + " Back", new Color(0.005f, 0.01f, 0.02f, 0.94f),
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var ratio = maximum <= 0 ? 0f : Mathf.Clamp01((float)current / maximum);
            fill = AddAnchoredPanel(back.transform, name + " Fill", color,
                Vector2.zero, new Vector2(ratio, 1f), Vector2.zero, Vector2.zero);
            labelText = AddAnchoredText(back.transform, name + " Label", label, 20,
                TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold,
                Vector2.zero, Vector2.one);
        }

        private void BuildLegacyCinematicCommandTray(Transform root, M2BattleView battle)
        {
            var tray = AddAnchoredPanel(root, "Always Visible Union Command Tray",
                new Color(0.018f, 0.03f, 0.052f, 0.985f),
                new Vector2(0f, 0f), new Vector2(1f, 0.34f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(tray, M1PremiumUi.Surface.Iron);
            var active = battle.PlayerUnions.Where(value => value.CanAct).ToArray();
            var focus = active.FirstOrDefault(value =>
                            StringComparer.Ordinal.Equals(value.UnionId, _expandedBattleCommandUnionId)) ??
                        active.FirstOrDefault(value => !value.IsSelected) ?? active.FirstOrDefault();
            if (focus == null) return;
            _expandedBattleCommandUnionId = focus.UnionId;

            var denseNavigator = active.Length > 5;
            var unionTabs = AddAnchoredPanel(tray.transform, "Active Union Command Tabs", Color.clear,
                new Vector2(0.01f, denseNavigator ? 0.68f : 0.82f),
                new Vector2(0.25f, 0.985f), Vector2.zero, Vector2.zero);
            for (var index = 0; index < active.Length; index++)
            {
                var captured = active[index];
                UnionNavigatorCell020(index, active.Length, out var cellMin, out var cellMax);
                var chipYInset = denseNavigator ? 0.005f : 0.02f;
                var tab = AddAnchoredButton(unionTabs.transform, "Focus Union Command " + captured.UnionId,
                    UnionNavigatorLabel020(captured, index, active.Length), () =>
                    {
                        _expandedBattleCommandUnionId = captured.UnionId;
                        BuildCurrentScreen();
                    }, StringComparer.Ordinal.Equals(captured.UnionId, focus.UnionId)
                        ? RuntimeUi.Accent
                        : RuntimeUi.ButtonNormal,
                    new Vector2(cellMin.x + 0.01f, cellMin.y + chipYInset),
                    new Vector2(cellMax.x - 0.01f, cellMax.y - chipYInset));
                StyleUnionNavigatorChip020(tab, denseNavigator, 22);
            }

            AddAnchoredText(tray.transform, "Focused Union Resource Decision",
                focus.DisplayName.ToUpperInvariant() + "  ·  SHARED AP " + focus.CurrentAp + "/" + focus.MaximumAp +
                "  ·  COHESION " + focus.Cohesion + "  ·  FORMATION " + focus.FormationConditionPercent + "%",
                25, TextAnchor.MiddleLeft, BattleAp, FontStyle.Bold,
                new Vector2(0.27f, 0.82f), new Vector2(0.77f, 0.985f));

            var forecasts = battle.Forecasts.Where(value => StringComparer.Ordinal.Equals(value.UnionId, focus.UnionId)).ToArray();
            var commandRow = AddAnchoredPanel(tray.transform, "Immediate Complete Union Commands", Color.clear,
                new Vector2(0.01f, 0.545f), new Vector2(0.99f, denseNavigator ? 0.665f : 0.81f),
                Vector2.zero, Vector2.zero);
            var count = Math.Max(1, forecasts.Length);
            for (var index = 0; index < forecasts.Length; index++)
            {
                var forecast = forecasts[index];
                var minX = (float)index / count;
                var maxX = (float)(index + 1) / count;
                var button = AddAnchoredButton(commandRow.transform,
                    "Complete Union Command " + forecast.ForecastId,
                    CompactCommandButtonLabel(forecast),
                    () => SelectCompleteForecast(forecast.UnionId, forecast.ForecastId),
                    forecast.IsSelected ? RuntimeUi.Accent : CommandFamilyColor(forecast.CommandId),
                    new Vector2(minX + 0.006f, 0.02f), new Vector2(maxX - 0.006f, 0.98f));
                SetButtonFont(button, forecasts.Length > 4 ? 19 : 23);
                ApplyBattleArtUiAsset011(button.image,
                    forecast.IsSelected ? "UI_COMMAND_CARD_SELECTED" : "UI_COMMAND_CARD_NORMAL",
                    forecast.IsSelected
                        ? Color.white
                        : Color.Lerp(Color.white, CommandFamilyColor(forecast.CommandId), 0.22f));
                var commandIcon = AddAnchoredPanel(button.transform,
                    "Command Family Icon 011 " + forecast.ForecastId, Color.white,
                    new Vector2(0.025f, 0.20f), new Vector2(0.17f, 0.80f), Vector2.zero, Vector2.zero);
                ApplyBattleArtUiAsset011(commandIcon, CommandIconAssetId011(forecast.CommandId), Color.white, false);
                commandIcon.raycastTarget = false;
                var commandLabel = button.GetComponentInChildren<Text>();
                if (commandLabel != null)
                {
                    commandLabel.rectTransform.anchorMin = new Vector2(0.18f, 0f);
                    commandLabel.rectTransform.anchorMax = new Vector2(0.98f, 1f);
                    commandLabel.rectTransform.offsetMin = Vector2.zero;
                    commandLabel.rectTransform.offsetMax = Vector2.zero;
                }
                button.interactable = !_battleResolving;
            }

            var selected = forecasts.FirstOrDefault(value => value.IsSelected);
            var detail = AddAnchoredPanel(tray.transform, "Selected Complete Command Detail",
                new Color(0.025f, 0.055f, 0.08f, 0.94f),
                new Vector2(0.01f, 0.03f), new Vector2(0.79f, 0.52f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(detail, selected == null ? M1PremiumUi.Surface.EtchedGlass : M1PremiumUi.Surface.Warning);
            AddAnchoredText(detail.transform, "Predicted Arts Interaction Law",
                "PREDICTED MEMBER ARTS", 17,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.52f, 0.86f), new Vector2(0.98f, 0.99f));
            if (selected == null)
            {
                AddAnchoredText(detail.transform, "Choose Command Prompt",
                    "CHOOSE ONE COMPLETE COMMAND", 27,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold,
                    new Vector2(0.03f, 0.12f), new Vector2(0.97f, 0.84f));
            }
            else
            {
                AddAnchoredText(detail.transform, "Selected Command Summary",
                    PlayerCommandLabel(selected) + "  →  " + selected.TargetName.ToUpperInvariant() +
                    "  ·  AP " + selected.SharedApCost + "  ·  " + selected.ExpectedEffect,
                    23, TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                    new Vector2(0.02f, 0.73f), new Vector2(0.98f, 0.86f));
                var actionCount = Math.Max(1, selected.MemberActions.Count);
                for (var actionIndex = 0; actionIndex < selected.MemberActions.Count; actionIndex++)
                {
                    var action = selected.MemberActions[actionIndex];
                    var top = 0.72f - (0.47f * actionIndex / actionCount);
                    var bottom = 0.72f - (0.47f * (actionIndex + 1) / actionCount);
                    AddAnchoredText(detail.transform, "Predicted Arts Non Clickable " + action.ActorMemberId,
                        CompactPredictedAction(action), 20, TextAnchor.MiddleLeft,
                        action.BreakthroughOpportunity ? RuntimeUi.Warning : RuntimeUi.Text,
                        action.BreakthroughOpportunity ? FontStyle.Bold : FontStyle.Normal,
                        new Vector2(0.02f, bottom), new Vector2(0.98f, top));
                }
                AddAnchoredText(detail.transform, "Selected Command Risk Learning",
                    "LEARN · " + selected.LearningOpportunity + "   |   RISK · " + selected.Risk +
                    "   |   FALLBACK · " + selected.FallbackBehavior,
                    17, TextAnchor.MiddleLeft, RuntimeUi.MutedText, FontStyle.Normal,
                    new Vector2(0.02f, 0.01f), new Vector2(0.98f, 0.25f));
            }

            var confirm = AddAnchoredButton(tray.transform, "Confirm Complete Forecast Round",
                battle.CanConfirmRound ? "CONFIRM ROUND" : "SELECT ALL UNIONS",
                BeginResolveRound, battle.CanConfirmRound ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                new Vector2(0.81f, 0.03f), new Vector2(0.985f, 0.52f));
            SetButtonFont(confirm, 31);
            confirm.interactable = battle.CanConfirmRound && _coordinator is IM2PresentationCoordinator;
            AddInvocationForecastControls022(tray.transform,battle,new Vector2(0.80f,0.54f),new Vector2(0.985f,0.96f),17);
        }

        private void BuildResolutionControls(Transform root)
        {
            var controls = AddAnchoredPanel(root, "Live Cinematic Resolution Controls",
                new Color(0.018f, 0.03f, 0.052f, 0.985f),
                new Vector2(0f, 0f), new Vector2(1f, 0.082f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(controls, M1PremiumUi.Surface.Iron);
            AddAnchoredText(controls.transform, "Live Speed Label", "SPEED", 20,
                TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.01f, 0.10f), new Vector2(0.075f, 0.90f));
            AddSpeedButton(controls.transform, "Resolution Speed 1x", "1×", 1f,
                new Vector2(0.08f, 0.10f), new Vector2(0.145f, 0.90f));
            AddSpeedButton(controls.transform, "Resolution Speed 2x", "2×", 2f,
                new Vector2(0.15f, 0.10f), new Vector2(0.215f, 0.90f));
            AddSpeedButton(controls.transform, "Resolution Speed 4x", "4×", 4f,
                new Vector2(0.22f, 0.10f), new Vector2(0.285f, 0.90f));
            Button pause = null;
            pause = AddAnchoredButton(controls.transform, "Pause Cinematic Presentation", "PAUSE", () =>
                {
                    _battleAnimationPaused = !_battleAnimationPaused;
                    SetButtonText(pause, _battleAnimationPaused ? "RESUME" : "PAUSE");
                }, RuntimeUi.ButtonNormal,
                new Vector2(0.305f, 0.10f), new Vector2(0.405f, 0.90f));
            AddAnchoredButton(controls.transform, "Skip Current Presentation Beat", "SKIP BEAT",
                () => _skipCurrentBattleBeat = true, RuntimeUi.ButtonNormal,
                new Vector2(0.415f, 0.10f), new Vector2(0.535f, 0.90f));

            var options = AddAnchoredPanel(root, "Battle Presentation Options",
                new Color(0.012f, 0.024f, 0.043f, 0.985f),
                new Vector2(0.56f, 0.09f), new Vector2(0.985f, 0.39f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(options, M1PremiumUi.Surface.EtchedGlass);
            options.gameObject.SetActive(_battleOptionsOpen);
            Button optionsButton = null;
            optionsButton = AddAnchoredButton(controls.transform, "Battle Presentation Options Toggle",
                _battleOptionsOpen ? "RESUME" : "OPTIONS", () =>
                {
                    _battleOptionsOpen = !_battleOptionsOpen;
                    _battleAnimationPaused = _battleOptionsOpen;
                    if (options != null) options.gameObject.SetActive(_battleOptionsOpen);
                    SetButtonText(optionsButton, _battleOptionsOpen ? "RESUME" : "OPTIONS");
                    SetButtonText(pause, _battleAnimationPaused ? "RESUME" : "PAUSE");
                }, RuntimeUi.ButtonNormal,
                new Vector2(0.545f, 0.10f), new Vector2(0.665f, 0.90f));
            var settingsSummary = AddAnchoredText(controls.transform, "Current Presentation Settings", BattleSettingsSummary(), 18,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.68f, 0.10f), new Vector2(0.985f, 0.90f));

            AddAnchoredText(options.transform, "Battle Options Heading", "PRESENTATION", 25,
                TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold,
                new Vector2(0.04f, 0.77f), new Vector2(0.96f, 0.96f));
            Button motion = null;
            motion = AddAnchoredButton(controls.transform, "Reduced Motion Toggle",
                _reducedMotion ? "MOTION · REDUCED" : "MOTION · FULL", () =>
                {
                    _reducedMotion = !_reducedMotion;
                    ApplyReducedMotionToCombatants();
                    SetButtonText(motion, _reducedMotion ? "MOTION · REDUCED" : "MOTION · FULL");
                    if (settingsSummary != null) settingsSummary.text = BattleSettingsSummary();
                }, _reducedMotion ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                new Vector2(0.04f, 0.49f), new Vector2(0.34f, 0.75f));
            motion.transform.SetParent(options.transform, false);
            Button flash = null;
            flash = AddAnchoredButton(controls.transform, "Reduced Flash Toggle",
                _reducedFlash ? "FLASH · REDUCED" : "FLASH · FULL", () =>
                {
                    _reducedFlash = !_reducedFlash;
                    SetButtonText(flash, _reducedFlash ? "FLASH · REDUCED" : "FLASH · FULL");
                    if (settingsSummary != null) settingsSummary.text = BattleSettingsSummary();
                }, _reducedFlash ? RuntimeUi.Accent : RuntimeUi.ButtonNormal,
                new Vector2(0.35f, 0.49f), new Vector2(0.65f, 0.75f));
            flash.transform.SetParent(options.transform, false);
            Button shake = null;
            shake = AddAnchoredButton(controls.transform, "Screen Shake Toggle",
                ShakeLabel(), () =>
                {
                    _battleShakeStrength = _battleShakeStrength > 0.75f ? 0.5f :
                        _battleShakeStrength > 0.25f ? 0f : 1f;
                    SetButtonText(shake, ShakeLabel());
                    if (settingsSummary != null) settingsSummary.text = BattleSettingsSummary();
                }, RuntimeUi.ButtonNormal,
                new Vector2(0.66f, 0.49f), new Vector2(0.96f, 0.75f));
            shake.transform.SetParent(options.transform, false);
            var resolve = AddAnchoredButton(options.transform, "Resolve Already Seen Animation", "FINISH SEEN ROUND",
                () =>
                {
                    _battleAnimationPaused = false;
                    _battleOptionsOpen = false;
                    if (options != null) options.gameObject.SetActive(false);
                    _skipBattleAnimation = true;
                }, RuntimeUi.Warning,
                new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.40f));
            resolve.interactable = _battleAnimationSeen;
        }

        private string BattleSettingsSummary()
        {
            var motion = _reducedMotion ? "REDUCED MOTION" : "FULL MOTION";
            var flash = _reducedFlash ? "REDUCED FLASH" : "FULL FLASH";
            return motion + "  ·  " + flash + "  ·  " + ShakeLabel();
        }

        private void AddSpeedButton(Transform parent, string name, string label, float speed, Vector2 min, Vector2 max)
        {
            var button = AddAnchoredButton(parent, name, label, () =>
                {
                    _battleAnimationSpeed = speed;
                    RefreshSpeedButtonColors(parent);
                },
                Mathf.Approximately(_battleAnimationSpeed, speed) ? RuntimeUi.Accent : RuntimeUi.ButtonNormal, min, max);
            SetButtonFont(button, 27);
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = Mathf.Approximately(_battleAnimationSpeed, speed)
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal;
        }

        private void RefreshSpeedButtonColors(Transform parent)
        {
            if (parent == null) return;
            foreach (var button in parent.GetComponentsInChildren<Button>())
            {
                float speed;
                if (StringComparer.Ordinal.Equals(button.name, "Resolution Speed 1x")) speed = 1f;
                else if (StringComparer.Ordinal.Equals(button.name, "Resolution Speed 2x")) speed = 2f;
                else if (StringComparer.Ordinal.Equals(button.name, "Resolution Speed 4x")) speed = 4f;
                else continue;
                var image = button.GetComponent<Image>();
                if (image != null) image.color = Mathf.Approximately(_battleAnimationSpeed, speed)
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal;
            }
        }

        private IEnumerator PlayCinematicResolvedRound(IReadOnlyList<M2BattleEventView> events)
        {
            var beats = BattlePresentationPlanner.Plan(events);
            var unionTurns = BattleUnionTurnPlanner019.Plan(
                _battlePresentationBefore ?? _coordinator.State.Battle,
                events);
            // Growth is deliberately kept out of the middle of member action shots.  Build
            // this receipt list up front so FINISH SEEN ROUND cannot accidentally discard a
            // lawful breakthrough that happened on the terminal action.
            var deferredGrowth = beats
                .Where(beat => beat != null &&
                               (beat.Family == BattleBeatFamily.Learning ||
                                beat.Family == BattleBeatFamily.Breakthrough))
                .ToList();
            var terminalBreakthroughs071 = deferredGrowth
                .Where(beat => beat.Family == BattleBeatFamily.Breakthrough)
                .GroupBy(beat => (beat.ActorMemberId ?? string.Empty) + "|" + (beat.ArtId ?? string.Empty),
                    StringComparer.Ordinal)
                .Select(group => group.First())
                .ToArray();
            var resolved = _battlePresentationAfter?.IsResolved ?? _coordinator.State.Battle?.IsResolved ?? false;
            var terminalBreakthroughShown071 = false;
            var unionTurnIndex = 0;
            for (var index = 0; index < beats.Count; index++)
            {
                if (_skipBattleAnimation) break;
                _skipCurrentBattleBeat = false;

                if (unionTurnIndex < unionTurns.Count &&
                    unionTurns[unionTurnIndex].FirstEventIndex == index)
                {
                    if (unionTurnIndex > 0)
                        yield return ReturnCinematic3DToTacticalOverview019();
                    yield return ShowUnionTurnRibbon071(unionTurns[unionTurnIndex]);
                    if (_skipBattleAnimation) break;
                    // SKIP BEAT dismisses the chapter card only. It must not silently
                    // consume the authoritative FORECAST_COMMITTED event that follows.
                    _skipCurrentBattleBeat = false;
                    yield return FocusCinematic3DUnionTurn019(unionTurns[unionTurnIndex].UnionId);
                    unionTurnIndex++;
                    if (_skipBattleAnimation) break;
                    _skipCurrentBattleBeat = false;
                }

                var beat = beats[index];
                if (beat.Family == BattleBeatFamily.Learning || beat.Family == BattleBeatFamily.Breakthrough)
                {
                    continue;
                }
                if (!beat.VisuallyStaged) continue;
                if (beat.Family == BattleBeatFamily.Result)
                {
                    yield return ReturnCinematic3DToTacticalOverview019();
                    if (resolved && !terminalBreakthroughShown071 && terminalBreakthroughs071.Length > 0)
                    {
                        yield return ShowTerminalArtBreakthrough071(terminalBreakthroughs071);
                        terminalBreakthroughShown071 = true;
                    }
                }
                if (_battleEventBanner != null)
                {
                    _battleEventBanner.text = BeatTitle(beat);
                    _battleEventBanner.color = BeatColor(beat);
                }
                if (_battleCinematicCaption != null) _battleCinematicCaption.text = string.Empty;
                var directive071 = M2Battle3DPresentationPolicy.CreateDirective(beat);
                if (beat.VisuallyStaged && _battleAudio != null)
                {
                    var cue071 = directive071.UsesFirstHourArtPerformance071
                        ? directive071.ArtChoreography071.SfxSignature
                        : beat.AudioCue;
                    _battleAudio.PlayCue(cue071);
                }
                if (CanStageCinematic3DBeat())
                {
                    // The 3D directive is the single visual authority for actor motion,
                    // contact, VFX, and camera. Only the lightweight HUD HP update follows;
                    // running the legacy beat here would create a second set of actors and
                    // effects over the perspective action.
                    yield return StageCinematic3DDirective071(directive071);
                    yield return AnimatePresentedHpChange(beat);
                }
                else yield return StageCinematicBeat(beat);
            }

            // A player may skip previously seen combat.  The information that a new Art
            // entered the authoritative build is never treated as disposable animation.
            if (resolved && !terminalBreakthroughShown071 && terminalBreakthroughs071.Length > 0)
                yield return ShowTerminalArtBreakthrough071(terminalBreakthroughs071);
            if (!resolved && !_skipBattleAnimation)
                yield return ReturnCinematic3DToTacticalOverview019();
            if (!resolved && deferredGrowth.Count > 0 && !_skipBattleAnimation)
                yield return ShowRoundGrowthSummary(deferredGrowth);

            _battleAnimationSeen = true;
            _battleResolving = false;
            _battleAnimationPaused = false;
            _battleOptionsOpen = false;
            _skipCurrentBattleBeat = false;
            _skipBattleAnimation = false;
            _battlePresentationBefore = null;
            _battlePresentationAfter = null;
            Navigate(resolved ? M1Screen.BattleResults : M1Screen.Battle);
        }

        private IEnumerator ShowTerminalArtBreakthrough071(
            IReadOnlyList<BattlePresentationBeat> breakthroughs)
        {
            if (_battleEffectRoot == null || breakthroughs == null || breakthroughs.Count == 0) yield break;

            var battle = _battlePresentationAfter ?? _coordinator.State.Battle;
            var lines = new List<string>();
            for (var index = 0; index < breakthroughs.Count && lines.Count < 4; index++)
            {
                var beat = breakthroughs[index];
                if (beat == null) continue;
                var actor = BattleMemberDisplayName(battle, beat.ActorMemberId).ToUpperInvariant();
                var art = M2BattleReadableText021.ArtDisplayName(beat.ArtId, beat.Caption, true)
                    .ToUpperInvariant();
                var line = actor + "  ·  " + art;
                if (!lines.Contains(line)) lines.Add(line);
            }
            if (lines.Count == 0) yield break;
            if (breakthroughs.Count > lines.Count)
                lines.Add("+" + (breakthroughs.Count - lines.Count) + " MORE BREAKTHROUGH" +
                          (breakthroughs.Count - lines.Count == 1 ? string.Empty : "S"));

            if (_battleEventBanner != null)
            {
                _battleEventBanner.text = "NEW ART LEARNED";
                _battleEventBanner.color = RuntimeUi.Warning;
            }
            if (_battleCinematicCaption != null) _battleCinematicCaption.text = string.Empty;
            if (_battleAudio != null) _battleAudio.PlayCue("SFX_BREAKTHROUGH");

            var overlay = AddAnchoredPanel(
                _battleEffectRoot,
                "Terminal Art Breakthrough Celebration 071",
                new Color(0.012f, 0.022f, 0.045f, 0.985f),
                new Vector2(0.13f, 0.18f),
                new Vector2(0.87f, 0.86f),
                Vector2.zero,
                Vector2.zero);
            overlay.transform.SetAsLastSibling();
            M1PremiumUi.StylePanel(overlay, M1PremiumUi.Surface.Warning);
            AddAnchoredPanel(overlay.transform, "Terminal Breakthrough Gold Crown 071", RuntimeUi.Warning,
                new Vector2(0f, 0.94f), Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;
            AddAnchoredText(overlay.transform, "Terminal New Art Learned Heading 071",
                "✦ NEW ART LEARNED ✦", 54, TextAnchor.MiddleCenter, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.93f));
            AddAnchoredText(overlay.transform, "Terminal New Art Learned Details 071",
                string.Join("\n", lines), 31, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.69f));
            AddAnchoredText(overlay.transform, "Terminal Breakthrough Meaningful Use 071",
                "LEARNED THROUGH MEANINGFUL USE  ·  READY FOR FUTURE LEGAL UNION FORECASTS",
                21, TextAnchor.MiddleCenter, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.23f));
            foreach (var graphic in overlay.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;

            if (_battleCombatants.TryGetValue(breakthroughs[0].ActorMemberId ?? string.Empty, out var actorView))
                SpawnGlyphEffect(actorView, "✦", RuntimeUi.Warning, 300f);

            var group = EnsureBattleComponent<CanvasGroup>(overlay.gameObject);
            group.alpha = _reducedMotion ? 1f : 0f;
            var elapsed = 0f;
            while (!_reducedMotion && elapsed < 0.16f && overlay != null)
            {
                if (!_battleAnimationPaused) elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.16f));
                yield return null;
            }
            if (group != null) group.alpha = 1f;

            elapsed = 0f;
            while (elapsed < 1.05f && overlay != null)
            {
                if (!_battleAnimationPaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (!_reducedMotion && elapsed < 0.18f && overlay != null)
            {
                if (!_battleAnimationPaused) elapsed += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(elapsed / 0.18f);
                yield return null;
            }
            if (overlay != null) Destroy(overlay.gameObject);
        }

        private IEnumerator ShowUnionTurnRibbon071(BattleUnionTurnPhase019 turn)
        {
            if (turn == null || _battleEffectRoot == null) yield break;
            var accent = turn.IsPlayer
                ? turn.PlayerOrdinal == 1 ? BattleCohesion : BattleAp
                : BattleEnemy;
            var ribbon = AddAnchoredPanel(
                _battleEffectRoot,
                "Union Turn Ribbon 071 · " + turn.UnionId,
                new Color(0.012f, 0.026f, 0.046f, _highContrast ? 1f : 0.94f),
                new Vector2(0.20f, 0.79f),
                new Vector2(0.80f, 0.875f),
                Vector2.zero,
                Vector2.zero);
            ribbon.raycastTarget = false;
            ribbon.transform.SetAsLastSibling();
            M1PremiumUi.StylePanel(ribbon,
                turn.IsPlayer ? M1PremiumUi.Surface.EtchedGlass : M1PremiumUi.Surface.Warning);
            AddAnchoredPanel(ribbon.transform, "Union Turn Accent 071", accent,
                new Vector2(0f, 0f), new Vector2(0.014f, 1f), Vector2.zero, Vector2.zero).raycastTarget = false;

            var badge = turn.IsPlayer
                ? "UNION " + turn.PlayerOrdinal + "/" + turn.PlayerTurnCount
                : "ENEMY TURN";
            var titleText = string.IsNullOrWhiteSpace(turn.DisplayName)
                ? (turn.IsPlayer ? "GUILD UNION" : "HOSTILE UNION")
                : turn.DisplayName.ToUpperInvariant();
            var order = string.IsNullOrWhiteSpace(turn.CommandName)
                ? "ORDER LOCKED"
                : turn.CommandName.ToUpperInvariant();
            var detail = turn.IsPlayer
                ? order + "  ·  " + turn.TacticalLine
                : turn.TacticalLine;
            AddAnchoredText(ribbon.transform, "Union Turn Badge 071", badge, 21,
                TextAnchor.MiddleLeft, accent, FontStyle.Bold,
                new Vector2(0.045f, 0.08f), new Vector2(0.20f, 0.92f));
            var title = AddAnchoredText(ribbon.transform, "Union Turn Title 071", titleText, 31,
                TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.20f, 0.08f), new Vector2(0.58f, 0.92f));
            M1PremiumUi.ConfigureDisplayText(title);
            AddAnchoredText(ribbon.transform, "Union Turn Detail 071", detail, 19,
                TextAnchor.MiddleRight, RuntimeUi.MutedText, FontStyle.Bold,
                new Vector2(0.58f, 0.08f), new Vector2(0.965f, 0.92f));
            foreach (var graphic in ribbon.GetComponentsInChildren<Graphic>())
                graphic.raycastTarget = false;

            if (_battleEventBanner != null)
            {
                // The compact ribbon is the single Union-turn label. Repeating an
                // enemy response heading above it crowded the battle with two copies.
                _battleEventBanner.text = string.Empty;
                _battleEventBanner.color = accent;
            }
            if (_battleAudio != null) _battleAudio.PlayCue(turn.IsPlayer ? "SFX_COMMAND_CONFIRM" : "AUDIO_BATTLE");

            var group = EnsureBattleComponent<CanvasGroup>(ribbon.gameObject);
            group.alpha = 0f;
            var home = ribbon.rectTransform.anchoredPosition;
            ribbon.rectTransform.anchoredPosition = home + new Vector2(0f, 12f);
            var elapsed = 0f;
            const float revealDuration = 0.08f;
            while (elapsed < revealDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += UnionTurnRibbonDelta071();
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / revealDuration));
                group.alpha = t;
                ribbon.rectTransform.anchoredPosition = Vector2.Lerp(home + new Vector2(0f, 12f), home, t);
                yield return null;
            }
            group.alpha = 1f;
            ribbon.rectTransform.anchoredPosition = home;
            elapsed = 0f;
            var readableHold = turn.IsPlayer ? 0.38f : 0.30f;
            while (elapsed < readableHold && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += UnionTurnRibbonDelta071();
                yield return null;
            }

            elapsed = 0f;
            const float hideDuration = 0.08f;
            while (elapsed < hideDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += UnionTurnRibbonDelta071();
                group.alpha = 1f - Mathf.Clamp01(elapsed / hideDuration);
                yield return null;
            }
            if (ribbon != null) Destroy(ribbon.gameObject);
        }

        private float UnionTurnRibbonDelta071() => CinematicDeltaTime();

        private IEnumerator ShowRoundGrowthSummary(IReadOnlyList<BattlePresentationBeat> growth)
        {
            if (_battleEffectRoot == null || growth == null || growth.Count == 0) yield break;
            var lines = new List<string>();
            for (var index = 0; index < growth.Count && lines.Count < 4; index++)
            {
                var caption = growth[index]?.Caption;
                if (!string.IsNullOrWhiteSpace(caption) && !lines.Contains(caption)) lines.Add(caption);
            }
            if (lines.Count == 0) yield break;
            var summary = AddAnchoredPanel(_battleEffectRoot, "Round Growth Summary",
                new Color(0.012f, 0.024f, 0.043f, 0.94f),
                new Vector2(0.66f, 0.66f), new Vector2(0.985f, 0.96f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(summary, M1PremiumUi.Surface.EtchedGlass);
            AddAnchoredText(summary.transform, "Round Growth Heading", "ROUND GROWTH", 29,
                TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.06f, 0.75f), new Vector2(0.94f, 0.94f));
            AddAnchoredText(summary.transform, "Round Growth Details", string.Join("\n", lines), 20,
                TextAnchor.UpperLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.74f));
            var elapsed = 0f;
            while (elapsed < 0.72f && !_skipBattleAnimation)
            {
                if (!_battleAnimationPaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (summary != null) Destroy(summary.gameObject);
        }

        private static string BattleGrowthSummary(M2BattleView battle)
        {
            var lines = new List<string>();
            if (battle?.Events != null)
            {
                for (var index = 0; index < battle.Events.Count && lines.Count < 5; index++)
                {
                    var item = battle.Events[index];
                    if (item == null ||
                        (!StringComparer.Ordinal.Equals(item.EventType, "ART_GROWTH") &&
                         !StringComparer.Ordinal.Equals(item.EventType, "BREAKTHROUGH")) ||
                        string.IsNullOrWhiteSpace(item.Text) || lines.Contains(item.Text)) continue;
                    lines.Add(item.Text);
                }
            }

            if (lines.Count == 0 && !string.IsNullOrWhiteSpace(battle?.TutorialBreakthroughSummary))
                lines.Add(battle.TutorialBreakthroughSummary);
            if (lines.Count == 0) lines.Add("No lasting Art growth was recorded in this battle.");
            return "BATTLE GROWTH\n" + string.Join("\n", lines);
        }

        private static void AddBattleResultCategory(
            Transform parent,
            string name,
            string heading,
            string details,
            Color accent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var panel = AddAnchoredPanel(parent, name, new Color(0.012f, 0.027f, 0.047f, 0.96f),
                anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.EtchedGlass);
            var title = AddAnchoredText(panel.transform, name + " Heading", heading, 27,
                TextAnchor.MiddleLeft, accent, FontStyle.Bold,
                new Vector2(0.035f, 0.77f), new Vector2(0.965f, 0.96f));
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 18;
            title.resizeTextMaxSize = 27;
            var body = AddAnchoredText(panel.transform, name + " Details", details, 21,
                TextAnchor.UpperLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.04f, 0.055f), new Vector2(0.96f, 0.77f));
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 15;
            body.resizeTextMaxSize = 21;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static string CharacterRewardSummary(M2BattleRewardView reward)
        {
            var memberRewards = reward?.MemberRewards ?? Array.Empty<M2BattleMemberRewardView>();
            if (memberRewards.Count == 0)
                return "No character XP reward record is available.";

            var lines = new List<string>();
            foreach (var member in memberRewards.Take(6))
            {
                var stats = new List<string>();
                AddStatGain(stats, "HP", member.MaximumHpGain);
                AddStatGain(stats, "MP", member.MaximumMpGain);
                AddStatGain(stats, "STR", member.StrengthGain);
                AddStatGain(stats, "DEF", member.DefenseGain);
                AddStatGain(stats, "AGI", member.AgilityGain);
                AddStatGain(stats, "MAG", member.MagicGain);
                AddStatGain(stats, "WILL", member.WillGain);
                var previousLevel = Math.Max(1, member.PreviousLevel);
                var level = member.ProjectedLevel > 0 ? member.ProjectedLevel : previousLevel;
                var levelLabel = level == previousLevel
                    ? "LV " + level
                    : "LV " + previousLevel + " > " + level;
                lines.Add((string.IsNullOrWhiteSpace(member.DisplayName) ? member.MemberId : member.DisplayName) +
                          "  +" + FormatProgressionNumber(member.PersonalXp) + " XP  |  " + levelLabel +
                          (stats.Count == 0 ? string.Empty : "  |  " + string.Join(" ", stats)));
            }

            if (memberRewards.Count > lines.Count)
                lines.Add("+" + (memberRewards.Count - lines.Count) + " more adventurers in the saved battle record");
            return string.Join("\n", lines);
        }

        private static void AddStatGain(ICollection<string> values, string label, int amount)
        {
            if (amount > 0) values.Add(label + "+" + amount.ToString(CultureInfo.InvariantCulture));
        }

        private static string ArtRewardSummary(M2BattleView battle)
        {
            var lines = new List<string> { "ONLY MEANINGFUL USE EARNS ART XP" };
            var unique = new HashSet<string>(StringComparer.Ordinal);
            var events = battle?.Events ?? Array.Empty<M2BattleEventView>();

            foreach (var group in events
                         .Where(value => value != null && StringComparer.Ordinal.Equals(value.EventType, "ART_GROWTH"))
                         .GroupBy(value => (value.MemberId ?? value.ActorMemberId ?? string.Empty) + "|" + (value.ArtId ?? string.Empty)))
            {
                var item = group.First();
                var memberId = item.MemberId ?? item.ActorMemberId;
                var actor = BattleMemberDisplayName(battle, memberId);
                var artName = M2BattleReadableText021.ArtDisplayName(item.ArtId, item.Text, false);
                var amount = group.Sum(value => Math.Abs((long)value.Amount));
                var line = actor + "  |  " + artName +
                           (amount > 0 ? " +" + FormatProgressionNumber(amount) + " ART XP" : " ART XP GAINED");
                if (unique.Add(line)) lines.Add(line);
            }

            foreach (var item in events.Where(value => value != null &&
                                                       StringComparer.Ordinal.Equals(value.EventType, "BREAKTHROUGH")))
            {
                var memberId = item.MemberId ?? item.ActorMemberId;
                var actor = BattleMemberDisplayName(battle, memberId);
                var artName = M2BattleReadableText021.ArtDisplayName(item.ArtId, item.Text, true);
                var line = actor + "  |  NEW ART: " + artName;
                if (unique.Add(line)) lines.Add(line);
            }

            if (lines.Count == 1 && battle != null && battle.TutorialBreakthroughOccurred &&
                !string.IsNullOrWhiteSpace(battle.TutorialBreakthroughSummary))
                lines.Add(battle.TutorialBreakthroughSummary);
            if (lines.Count == 1) lines.Add("No Art gained mastery in this battle.");
            if (lines.Count > 6)
            {
                var remaining = lines.Count - 6;
                lines = lines.Take(6).ToList();
                lines.Add("+" + remaining + " more use-based Art gains in the saved record");
            }
            return string.Join("\n", lines);
        }

        private static string GuildRewardSummary(M2BattleRewardView reward)
        {
            if (reward == null) return "No Guild XP reward record is available.";
            var previousLevel = Math.Max(1, reward.GuildPreviousLevel);
            var projectedLevel = Math.Max(previousLevel, reward.GuildProjectedLevel);
            return "XP TO SPEND +" + FormatProgressionNumber(reward.GuildTreasuryXpAward) + "\n" +
                   "GUILD LV " + previousLevel + (projectedLevel == previousLevel ? string.Empty : " > " + projectedLevel) + "\n" +
                   "GUILD XP " + FormatProgressionNumber(reward.GuildXpBefore) + " > " +
                   FormatProgressionNumber(reward.GuildXpAfter) + "\n" +
                   "BATTLE REWARD " + (reward.Claimed ? "CLAIMED & SAVED" : "READY TO CLAIM");
        }

        private static string HallRewardSummary(M2BattleRewardView reward, M1PresentationState state)
        {
            if (reward == null) return "No Hall enhancement reward record is available.";
            var stageName = CleanHallStageName(state?.HallStageName).ToUpperInvariant();
            var currentXp = Math.Max(0L, state?.HallEnhancementXp ?? 0L);
            var projectedXp = reward.Claimed ? currentXp : currentXp + Math.Max(0L, reward.HallEnhancementXpAward);
            var facilityCount = state?.Facilities?.Count ?? 0;
            var facilityLine = facilityCount > 0
                ? facilityCount + " FACILITIES IN THE GUILD FOUNDATION"
                : "19 FACILITIES START AT LV 0";
            var hallSummary = "+" + FormatProgressionNumber(reward.HallEnhancementXpAward) + " HALL ENHANCEMENT XP\n" +
                   "STAGE " + Math.Max(0, state?.HallStageIndex ?? 0) + "  |  " + stageName + "\n" +
                   "HALL GROWTH " + FormatProgressionNumber(currentXp) +
                   (projectedXp == currentXp ? string.Empty : " > " + FormatProgressionNumber(projectedXp));
            if (string.IsNullOrWhiteSpace(reward.EquipmentRewardInstanceId) &&
                string.IsNullOrWhiteSpace(reward.EquipmentRewardDisplayName))
                return hallSummary + "\n" + facilityLine;

            var displayName = string.IsNullOrWhiteSpace(reward.EquipmentRewardDisplayName)
                ? "Earned equipment"
                : reward.EquipmentRewardDisplayName;
            return hallSummary + "\nNEW LOOT · " + displayName.ToUpperInvariant() +
                   "\nOPEN THE ARMORY TO EQUIP IT";
        }

        private static string CompactPartyRewardSummary071(M2BattleRewardView reward)
        {
            var members071 = reward?.MemberRewards ?? Array.Empty<M2BattleMemberRewardView>();
            if (members071.Count == 0) return "THE PARTY HELD THE LINE";
            var totalXp071 = members071.Sum(value => Math.Max(0L, value.PersonalXp));
            var levelUps071 = members071.Count(value => value.ProjectedLevel > value.PreviousLevel);
            var statGains071 = members071.Sum(value =>
                Math.Max(0, value.MaximumHpGain) + Math.Max(0, value.MaximumMpGain) +
                Math.Max(0, value.StrengthGain) + Math.Max(0, value.DefenseGain) +
                Math.Max(0, value.AgilityGain) + Math.Max(0, value.MagicGain) +
                Math.Max(0, value.WillGain));
            return "+" + FormatProgressionNumber(totalXp071) + " TOTAL XP\n" +
                   members071.Count + " ADVENTURERS GREW\n" +
                   (levelUps071 > 0 ? levelUps071 + " LEVEL UP" + (levelUps071 == 1 ? string.Empty : "S") : "MASTERY DEEPENED") +
                   "\n" + statGains071 + " STAT POINTS GAINED";
        }

        private static string CompactArtRewardSummary071(M2BattleView battle)
        {
            var lines071 = ArtRewardSummary(battle)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(value => !value.StartsWith("ONLY MEANINGFUL", StringComparison.Ordinal))
                .Take(4)
                .ToArray();
            return lines071.Length == 0
                ? "NO NEW ART THIS BATTLE\nKEEP USING ARTS TO MASTER THEM"
                : string.Join("\n", lines071);
        }

        private static string CompactGuildSpoilsSummary071(M2BattleRewardView reward)
        {
            if (reward == null) return "REWARDS RECORDED";
            var displayName071 = string.IsNullOrWhiteSpace(reward.EquipmentRewardDisplayName)
                ? "NO EQUIPMENT DROP"
                : reward.EquipmentRewardDisplayName.ToUpperInvariant();
            return "XP TO SPEND +" + FormatProgressionNumber(reward.GuildTreasuryXpAward) + "\n" +
                   "+" + FormatProgressionNumber(reward.HallEnhancementXpAward) + " HALL GROWTH\n\n" +
                   "NEW LOOT\n" + displayName071 + "\nOPEN ARMORY TO EQUIP";
        }

        private static string BattleRewardStatus(M2BattleRewardView reward)
        {
            if (reward == null) return "REWARD RECORD UNAVAILABLE";
            return reward.Claimed ? "REWARDS CLAIMED & SAVED" : "REWARDS READY TO CLAIM";
        }

        private static string BattleMemberDisplayName(M2BattleView battle, string memberId)
        {
            if (battle?.PlayerUnions != null)
            {
                var member = battle.PlayerUnions
                    .Where(value => value?.Members != null)
                    .SelectMany(value => value.Members)
                    .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.MemberId, memberId));
                if (!string.IsNullOrWhiteSpace(member?.DisplayName)) return member.DisplayName;
            }
            return string.IsNullOrWhiteSpace(memberId) ? "Adventurer" : memberId.Replace('_', ' ');
        }

        private static string FormatProgressionNumber(long value) =>
            value.ToString("N0", CultureInfo.InvariantCulture);

        private IEnumerator FadeInCinematicBattle(RectTransform root)
        {
            if (root == null) yield break;
            var group = EnsureBattleComponent<CanvasGroup>(root.gameObject);
            group.alpha = 0f;
            var elapsed = 0f;
            const float duration = 0.32f;
            while (elapsed < duration && root != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            if (group != null) group.alpha = 1f;
        }

        private static T EnsureBattleComponent<T>(GameObject host) where T : Component
        {
            var component = host.GetComponent<T>();
            if (component == null) component = host.AddComponent<T>();
            return component;
        }

        private IEnumerator StageCinematicBeat(BattlePresentationBeat beat)
        {
            _battleCombatants.TryGetValue(beat.ActorMemberId, out var actor);
            _battleCombatants.TryGetValue(beat.TargetMemberId, out var target);
            var actionFocus = BeginActionFocus(beat, actor, target);
            if (actionFocus != null)
            {
                yield return RevealActionFocus(actionFocus);
                actor = actionFocus.Actor ?? actor;
                target = actionFocus.Target ?? target;
            }
            yield return FrameCinematicBeat(beat, actor, target);
            if (_reducedMotion)
            {
                yield return PulseReducedMotion(target ?? actor, BeatColor(beat));
                yield return AnimatePresentedHpChange(beat);
                EndActionFocus(actionFocus);
                yield break;
            }

            switch (beat.Family)
            {
                case BattleBeatFamily.BasicMartial:
                case BattleBeatFamily.CombatArt:
                case BattleBeatFamily.Tactical:
                    yield return AnimateStrike(actor, target, beat);
                    break;
                case BattleBeatFamily.Mystic:
                    yield return AnimateMystic(actor, target, beat);
                    break;
                case BattleBeatFamily.Invocation:
                    yield return AnimateMystic(actor, target ?? actor, beat);
                    break;
                case BattleBeatFamily.Restoration:
                    yield return AnimateRestoration(actor, target, beat);
                    break;
                case BattleBeatFamily.Guard:
                    yield return AnimateGuard(actor, beat);
                    break;
                case BattleBeatFamily.Interception:
                    yield return AnimateInterception(actor, target, beat);
                    break;
                case BattleBeatFamily.Recovery:
                case BattleBeatFamily.Formation:
                    yield return AnimateSupport(target ?? actor, beat);
                    break;
                case BattleBeatFamily.Positioning:
                    yield return AnimatePositionShift(actor, target, beat);
                    break;
                case BattleBeatFamily.Downed:
                    yield return AnimateDowned(target ?? actor, beat);
                    break;
                case BattleBeatFamily.Learning:
                    yield return AnimateLearning(target ?? actor, beat, false);
                    break;
                case BattleBeatFamily.Breakthrough:
                    yield return AnimateLearning(target ?? actor, beat, true);
                    break;
                case BattleBeatFamily.Retreat:
                    yield return AnimateRetreat(actor, beat);
                    break;
                case BattleBeatFamily.Result:
                    yield return AnimateResult(beat);
                    break;
                case BattleBeatFamily.CommandCommit:
                    yield return AnimateCommandSeal(actor, beat);
                    break;
                default:
                    yield return WaitCinematic(0.24f);
                    break;
            }
            yield return AnimatePresentedHpChange(beat);
            ResetCinematicTransforms();
            EndActionFocus(actionFocus);
        }

        private IEnumerator FrameCinematicBeat(
            BattlePresentationBeat beat,
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target)
        {
            if (_battlefieldCameraRoot == null || _reducedMotion || beat.Camera == BattleCameraShot.Wide)
                yield break;

            var focus = target ?? actor;
            var localFocus = EffectPoint(focus);
            var zoom = beat.Camera == BattleCameraShot.Impact || beat.Camera == BattleCameraShot.Breakthrough
                ? 1.055f
                : 1.025f;
            var desired = Vector2.ClampMagnitude(-localFocus * 0.035f, 54f);
            var startPosition = _battlefieldCameraRoot.anchoredPosition;
            var startScale = _battlefieldCameraRoot.localScale;
            var elapsed = 0f;
            while (elapsed < 0.16f && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.Clamp01(elapsed / 0.16f);
                _battlefieldCameraRoot.anchoredPosition = Vector2.Lerp(startPosition, desired, t);
                _battlefieldCameraRoot.localScale = Vector3.Lerp(startScale, new Vector3(zoom, zoom, 1f), t);
                yield return null;
            }
        }

        private IEnumerator AnimateStrike(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat)
        {
            if (actor == null && target == null) { yield return WaitCinematic(0.3f); yield break; }
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011, BeatColor(beat));
            PlayBattleArtWindupAudio011(art011);
            if (actor != null)
            {
                var direction = target == null ? (actor.Root.anchorMin.x < 0.5f ? 1f : -1f) :
                    Mathf.Sign(target.Root.position.x - actor.Root.position.x);
                var closeup = IsActionCloseup(actor);
                yield return BlendBattleArtPose011(actor, BattleArtPoseDirector011.Anticipation, true, 0.10f);
                yield return AnticipateCombatant(actor, direction, closeup ? 0.13f : 0.10f);
                SetBattleArtPose011(actor, BattleArtPoseDirector011.ActivePoseFor(beat.Family));
                if (_battleAudio != null) _battleAudio.PlayCue("SFX_WHOOSH");
                var advance = closeup
                    ? (beat.Family == BattleBeatFamily.CombatArt ? 390f : 310f)
                    : (beat.Family == BattleBeatFamily.CombatArt ? 118f : 82f);
                SpawnCombatAfterimage(actor, direction);
                yield return MoveCombatant(actor, actor.Root.anchoredPosition,
                    actor.Home + new Vector2(advance * direction, 0f), 0.20f);
            }
            var impactTarget = target ?? actor;
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.trailResourcePath))
                yield return PlayBattleArtVfx011(art011.trailResourcePath, impactTarget,
                    IsActionCloseup(impactTarget) ? 690f : beat.Family == BattleBeatFamily.CombatArt ? 390f : 300f,
                    -8f, beat.Family == BattleBeatFamily.CombatArt ? 0.34f : 0.26f, Color.white);
            else
                yield return PlayAuthoredBattleVfx("WEAPON_ARC", impactTarget,
                    IsActionCloseup(impactTarget) ? 610f : beat.Family == BattleBeatFamily.CombatArt ? 360f : 280f,
                    -8f, beat.Family == BattleBeatFamily.CombatArt ? 0.34f : 0.26f);
            PlayBattleArtAudio011(art011, true);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.impactResourcePath))
                yield return PlayBattleArtVfx011(art011.impactResourcePath, impactTarget,
                    IsActionCloseup(impactTarget) ? 470f : 285f, 0f, 0.20f, Color.white);
            SpawnSlashEffect(impactTarget, artColor011, beat.Family == BattleBeatFamily.CombatArt ? 280f : 210f);
            SpawnAmountEffect(target ?? actor, beat, false);
            yield return HoldImpactFrames(BattleArtImpactFrames011(
                art011, beat.Family == BattleBeatFamily.CombatArt ? 3 : 2));
            yield return FlashImpact(target ?? actor, artColor011);
            yield return ShakeBattlefield(BattleArtShakeMagnitude011(
                art011, beat.Family == BattleBeatFamily.CombatArt ? 22f : 14f), 0.10f);
            if (target != null) yield return RecoilCombatant(target, actor == null ? -1f : Mathf.Sign(target.Root.position.x - actor.Root.position.x));
            if (actor != null)
            {
                actor.Root.localRotation = Quaternion.Euler(0f, 0f,
                    actor.HomeRotation.eulerAngles.z + (actor.Root.position.x < (target?.Root.position.x ?? actor.Root.position.x) ? -8f : 8f));
                yield return WaitCinematic(0.10f);
                SetBattleArtPose011(actor, BattleArtPoseDirector011.Recovery);
                yield return WaitCinematic(0.055f);
                yield return BlendActionPose(actor, false, 0.12f);
            }
            yield return WaitCinematic(0.08f);
        }

        private IEnumerator AnimateMystic(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat)
        {
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011, new Color(0.30f, 0.82f, 1f, 1f));
            PlayBattleArtWindupAudio011(art011);
            yield return BlendBattleArtPose011(actor, BattleArtPoseDirector011.RolePrimary, true, 0.14f);
            var castColor011 = artColor011;
            castColor011.a = _reducedFlash ? 0.55f : 0.92f;
            SpawnGlyphEffect(actor, string.IsNullOrWhiteSpace(art011?.glyph) ? "✦" : art011.glyph, castColor011, 150f);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.fieldResourcePath))
                yield return PlayBattleArtVfx011(art011.fieldResourcePath, actor,
                    IsActionCloseup(actor) ? 460f : 285f, 0f, 0.22f, Color.white);
            if (actor != null)
            {
                actor.Root.localRotation = Quaternion.Euler(0f, 0f, actor.Enemy ? -5f : 5f);
                yield return ScaleCombatant(actor, new Vector3(0.96f, 1.04f, 1f), new Vector3(1.09f, 1.09f, 1f), 0.22f);
            }
            yield return AnimateBattleArtProjectile011(art011, actor, target);
            PlayBattleArtAudio011(art011, true);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.impactResourcePath))
                yield return PlayBattleArtVfx011(art011.impactResourcePath, target ?? actor,
                    IsActionCloseup(target ?? actor) ? 690f : 390f, 0f, 0.44f, Color.white);
            else
                yield return PlayAuthoredBattleVfx("MYSTIC_BURST", target ?? actor,
                    IsActionCloseup(target ?? actor) ? 650f : 360f, 0f, 0.44f);
            var burstColor011 = artColor011;
            burstColor011.a = _reducedFlash ? 0.48f : 0.90f;
            SpawnBurstEffect(target ?? actor, burstColor011, 260f);
            yield return PlayBattleArtField011(art011, target ?? actor,
                IsActionCloseup(target ?? actor) ? 760f : 430f, 0.34f);
            SpawnAmountEffect(target ?? actor, beat, false);
            yield return HoldImpactFrames(BattleArtImpactFrames011(art011, 2));
            yield return FlashImpact(target ?? actor, artColor011);
            yield return ShakeBattlefield(BattleArtShakeMagnitude011(art011, 18f), 0.12f);
            if (target != null) yield return RecoilCombatant(target, 1f);
            SetBattleArtPose011(actor, BattleArtPoseDirector011.Recovery);
            yield return WaitCinematic(0.055f);
            yield return BlendActionPose(actor, false, 0.14f);
        }

        private IEnumerator AnimateRestoration(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat)
        {
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011, new Color(0.45f, 1f, 0.68f, 1f));
            PlayBattleArtWindupAudio011(art011);
            yield return BlendBattleArtPose011(actor, BattleArtPoseDirector011.RolePrimary, true, 0.14f);
            var restorationCast011 = artColor011; restorationCast011.a = 0.95f;
            SpawnGlyphEffect(actor, string.IsNullOrWhiteSpace(art011?.glyph) ? "✚" : art011.glyph, restorationCast011, 120f);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.fieldResourcePath))
                yield return PlayBattleArtVfx011(art011.fieldResourcePath, actor,
                    IsActionCloseup(actor) ? 440f : 270f, 0f, 0.22f, Color.white);
            yield return WaitCinematic(0.10f);
            PlayBattleArtAudio011(art011, true);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.impactResourcePath))
                yield return PlayBattleArtVfx011(art011.impactResourcePath, target ?? actor,
                    IsActionCloseup(target ?? actor) ? 640f : 350f, 0f, 0.52f, Color.white);
            else
                yield return PlayAuthoredBattleVfx("RESTORATION_BLOOM", target ?? actor,
                    IsActionCloseup(target ?? actor) ? 610f : 330f, 0f, 0.52f);
            yield return PlayBattleArtField011(art011, target ?? actor,
                IsActionCloseup(target ?? actor) ? 720f : 410f, 0.42f);
            var restorationTarget011 = artColor011; restorationTarget011.a = _reducedFlash ? 0.55f : 0.92f;
            SpawnGlyphEffect(target ?? actor, "◉", restorationTarget011, 190f);
            SpawnAmountEffect(target ?? actor, beat, true);
            if (target != null) yield return ScaleCombatant(target, Vector3.one, new Vector3(1.07f, 1.07f, 1f), 0.28f);
            yield return FlashImpact(target ?? actor, artColor011);
            SetBattleArtPose011(actor, BattleArtPoseDirector011.Recovery);
            yield return WaitCinematic(0.055f);
            yield return BlendActionPose(actor, false, 0.14f);
        }

        private IEnumerator AnimateGuard(BattleCombatantRuntimeView actor, BattlePresentationBeat beat)
        {
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011, new Color(0.35f, 0.90f, 1f, 1f));
            PlayBattleArtWindupAudio011(art011);
            yield return BlendBattleArtPose011(actor, BattleArtPoseDirector011.RolePrimary, true, 0.12f);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.fieldResourcePath))
                yield return PlayBattleArtVfx011(art011.fieldResourcePath, actor,
                    IsActionCloseup(actor) ? 470f : 290f, 0f, 0.18f, Color.white);
            PlayBattleArtAudio011(art011, true);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.impactResourcePath))
                yield return PlayBattleArtVfx011(art011.impactResourcePath, actor,
                    IsActionCloseup(actor) ? 620f : 350f, 0f, 0.42f, Color.white);
            else
                yield return PlayAuthoredBattleVfx("GUARD_IMPACT", actor,
                    IsActionCloseup(actor) ? 590f : 325f, 0f, 0.42f);
            yield return PlayBattleArtField011(art011, actor,
                IsActionCloseup(actor) ? 660f : 370f, 0.38f);
            var guardGlyph011 = artColor011; guardGlyph011.a = 0.92f;
            SpawnGlyphEffect(actor, "◇", guardGlyph011, 230f);
            if (actor != null)
            {
                actor.Root.localRotation = Quaternion.Euler(0f, 0f, actor.HomeRotation.eulerAngles.z + 4f);
                yield return ScaleCombatant(actor, Vector3.one, new Vector3(1.05f, 1.05f, 1f), 0.26f);
            }
            else yield return WaitCinematic(0.26f);
            yield return HoldImpactFrames(BattleArtImpactFrames011(art011, 2));
            SetBattleArtPose011(actor, BattleArtPoseDirector011.Recovery);
            yield return WaitCinematic(0.055f);
            yield return BlendActionPose(actor, false, 0.12f);
        }

        private IEnumerator AnimateInterception(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat)
        {
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011, RuntimeUi.Warning);
            PlayBattleArtWindupAudio011(art011);
            yield return BlendBattleArtPose011(actor, BattleArtPoseDirector011.RolePrimary, true, 0.10f);
            if (actor != null)
            {
                SpawnCombatAfterimage(actor, actor.Enemy ? 1f : -1f);
                yield return MoveCombatant(actor, actor.Home, actor.Home + new Vector2(actor.Enemy ? 130f : -130f, 0f), 0.18f);
            }
            PlayBattleArtAudio011(art011, true);
            if (art011 != null && !string.IsNullOrWhiteSpace(art011.impactResourcePath))
                yield return PlayBattleArtVfx011(art011.impactResourcePath, target ?? actor,
                    IsActionCloseup(target ?? actor) ? 650f : 365f, -4f, 0.44f, Color.white);
            else
                yield return PlayAuthoredBattleVfx("GUARD_IMPACT", target ?? actor,
                    IsActionCloseup(target ?? actor) ? 620f : 340f, -4f, 0.44f);
            yield return PlayBattleArtField011(art011, target ?? actor,
                IsActionCloseup(target ?? actor) ? 680f : 390f, 0.34f);
            var interceptGlyph011 = artColor011; interceptGlyph011.a = 0.95f;
            SpawnGlyphEffect(target ?? actor, "◆", interceptGlyph011, 245f);
            SpawnSlashEffect(target ?? actor, artColor011, 210f);
            SpawnAmountEffect(target ?? actor, beat, false);
            yield return HoldImpactFrames(BattleArtImpactFrames011(art011, 2));
            yield return FlashImpact(target ?? actor, RuntimeUi.Warning);
            yield return ShakeBattlefield(BattleArtShakeMagnitude011(art011, 20f), 0.11f);
            if (target != null) yield return RecoilCombatant(target, -1f);
            SetBattleArtPose011(actor, BattleArtPoseDirector011.Recovery);
            yield return WaitCinematic(0.055f);
            yield return BlendActionPose(actor, false, 0.12f);
        }

        private IEnumerator AnimateSupport(BattleCombatantRuntimeView target, BattlePresentationBeat beat)
        {
            var art011 = ResolveBattleArt011(beat);
            var artColor011 = BattleArtColor011(art011,
                beat.Family == BattleBeatFamily.Formation ? BattleCohesion : BattleAp);
            PlayBattleArtWindupAudio011(art011);
            yield return BlendBattleArtPose011(target, BattleArtPoseDirector011.RolePrimary, true, 0.10f);
            yield return PlayBattleArtField011(art011, target,
                IsActionCloseup(target) ? 650f : 360f, 0.34f);
            PlayBattleArtAudio011(art011, true);
            SpawnGlyphEffect(target, beat.Family == BattleBeatFamily.Formation ? "⬡" : "↟",
                artColor011, 165f);
            SpawnAmountEffect(target, beat, true);
            yield return PulseReducedMotion(target, artColor011);
            SetBattleArtPose011(target, BattleArtPoseDirector011.Recovery);
            yield return WaitCinematic(0.055f);
            yield return BlendActionPose(target, false, 0.10f);
        }

        private IEnumerator AnimatePositionShift(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat)
        {
            if (actor == null)
            {
                yield return WaitCinematic(0.30f);
                yield break;
            }
            var direction = target == null || target.Root.position.x >= actor.Root.position.x ? 1f : -1f;
            SpawnGlyphEffect(actor, "↗", BattleCohesion, 190f);
            SpawnCombatAfterimage(actor, direction);
            yield return AnticipateCombatant(actor, direction, 0.10f);
            yield return MoveCombatant(actor, actor.Home,
                actor.Home + new Vector2(95f * direction, 62f), 0.24f);
            SpawnGlyphEffect(target ?? actor, "SIDE", RuntimeUi.Warning, 150f);
            yield return PulseReducedMotion(target ?? actor, RuntimeUi.Warning);
        }

        private IEnumerator AnimateDowned(BattleCombatantRuntimeView target, BattlePresentationBeat beat)
        {
            if (target == null) { yield return WaitCinematic(0.28f); yield break; }
            yield return BlendBattleArtPose011(target, BattleArtPoseDirector011.Downed, true, 0.12f);
            SpawnBurstEffect(target, new Color(0.40f, 0.30f, 0.25f, 0.75f), 180f);
            var start = target.Root.localRotation;
            var elapsed = 0f;
            while (elapsed < 0.34f && !_skipCurrentBattleBeat)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.Clamp01(elapsed / 0.34f);
                target.Root.localRotation = Quaternion.Lerp(start,
                    Quaternion.Euler(0f, 0f, IsActionCloseup(target) ? -28f : -12f), t);
                target.Canvas.alpha = Mathf.Lerp(1f, IsActionCloseup(target) ? 0.25f : 0.42f, t);
                yield return null;
            }
        }

        private IEnumerator AnimateLearning(
            BattleCombatantRuntimeView target,
            BattlePresentationBeat beat,
            bool breakthrough)
        {
            var overlay = AddAnchoredPanel(_battleEffectRoot, breakthrough ? "Breakthrough Toast" : "Art Growth Toast",
                new Color(0.015f, 0.025f, 0.05f, 0.90f),
                new Vector2(0.68f, 0.69f), new Vector2(0.985f, 0.96f), Vector2.zero, Vector2.zero);
            M1PremiumUi.StylePanel(overlay, breakthrough ? M1PremiumUi.Surface.Warning : M1PremiumUi.Surface.EtchedGlass);
            AddAnchoredText(overlay.transform, breakthrough ? "New Art Learned Heading" : "Meaningful Art Growth Heading",
                breakthrough ? "✦ NEW ART" : "ART MASTERY", breakthrough ? 34 : 27,
                TextAnchor.MiddleLeft, RuntimeUi.Warning, FontStyle.Bold,
                new Vector2(0.06f, 0.62f), new Vector2(0.94f, 0.93f));
            AddAnchoredText(overlay.transform, breakthrough ? "New Art Learned Text" : "Meaningful Art Growth Text",
                beat.Caption, breakthrough ? 24 : 21, TextAnchor.MiddleLeft, RuntimeUi.Text, FontStyle.Bold,
                new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.63f));
            SpawnGlyphEffect(target, breakthrough ? "✦" : "+", RuntimeUi.Warning, breakthrough ? 245f : 150f);
            yield return WaitCinematic(breakthrough ? 0.58f : 0.30f);
            if (overlay != null) Destroy(overlay.gameObject);
        }

        private IEnumerator AnimateRetreat(BattleCombatantRuntimeView actor, BattlePresentationBeat beat)
        {
            if (actor == null) { yield return WaitCinematic(0.35f); yield break; }
            var direction = actor.Root.position.x < Screen.width * 0.5f ? -1f : 1f;
            yield return MoveCombatant(actor, actor.Home, actor.Home + new Vector2(190f * direction, 0f), 0.42f);
            actor.Canvas.alpha = 0.35f;
        }

        private IEnumerator AnimateResult(BattlePresentationBeat beat)
        {
            var victory = beat != null && beat.Caption.IndexOf("defeated", StringComparison.OrdinalIgnoreCase) >= 0;
            if (victory)
            {
                foreach (var view in _battleCombatants.Values)
                {
                    if (view == null || view.Enemy || view.CurrentHp <= 0) continue;
                    if (!SetBattleArtPose011(view, BattleArtPoseDirector011.Victory)) continue;
                    if (view.Artwork != null)
                    {
                        var idle = view.Artwork.color;
                        idle.a = 0f;
                        view.Artwork.color = idle;
                    }
                    if (view.ActionArtwork != null)
                    {
                        var action = view.ActionArtwork.color;
                        action.a = 1f;
                        view.ActionArtwork.color = action;
                    }
                }
            }
            SpawnGlyphEffect(null, victory ? "✦" : "◆", RuntimeUi.Warning, 260f);
            if (_battlefieldCameraRoot != null)
            {
                var start = _battlefieldCameraRoot.localScale;
                var elapsed = 0f;
                while (elapsed < 0.46f && !_skipCurrentBattleBeat)
                {
                    elapsed += CinematicDeltaTime();
                    _battlefieldCameraRoot.localScale = Vector3.Lerp(start, new Vector3(1.035f, 1.035f, 1f), Mathf.Clamp01(elapsed / 0.46f));
                    yield return null;
                }
            }
        }

        private IEnumerator AnimateCommandSeal(BattleCombatantRuntimeView actor, BattlePresentationBeat beat)
        {
            SpawnGlyphEffect(actor, "◆", RuntimeUi.Warning, 115f);
            yield return PulseReducedMotion(actor, RuntimeUi.Warning);
        }

        private IEnumerator MoveCombatant(BattleCombatantRuntimeView view, Vector2 from, Vector2 to, float baseDuration)
        {
            if (view == null) { yield return WaitCinematic(baseDuration); yield break; }
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.Clamp01(elapsed / baseDuration);
                view.Root.anchoredPosition = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
        }

        private IEnumerator ScaleCombatant(BattleCombatantRuntimeView view, Vector3 from, Vector3 to, float baseDuration)
        {
            if (view == null) { yield return WaitCinematic(baseDuration); yield break; }
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                view.Root.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / baseDuration));
                yield return null;
            }
        }

        private IEnumerator BlendActionPose(BattleCombatantRuntimeView view, bool actionVisible, float baseDuration)
        {
            if (view == null || view.Artwork == null || view.ActionArtwork == null)
            {
                yield return WaitCinematic(baseDuration);
                yield break;
            }

            var idleStart = view.Artwork.color;
            var actionStart = view.ActionArtwork.color;
            var idleTarget = idleStart;
            var actionTarget = actionStart;
            idleTarget.a = actionVisible ? 0f : 1f;
            actionTarget.a = actionVisible ? 1f : 0f;
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / baseDuration));
                view.Artwork.color = Color.Lerp(idleStart, idleTarget, t);
                view.ActionArtwork.color = Color.Lerp(actionStart, actionTarget, t);
                yield return null;
            }
            if (view.Artwork != null) view.Artwork.color = idleTarget;
            if (view.ActionArtwork != null) view.ActionArtwork.color = actionTarget;
        }

        private IEnumerator AnticipateCombatant(BattleCombatantRuntimeView view, float direction, float baseDuration)
        {
            if (view == null) { yield return WaitCinematic(baseDuration); yield break; }
            var startPosition = view.Root.anchoredPosition;
            var targetPosition = view.Home + new Vector2(-86f * direction, 18f);
            var startRotation = view.Root.localRotation;
            var targetRotation = Quaternion.Euler(0f, 0f,
                view.HomeRotation.eulerAngles.z + 7f * direction);
            var startScale = view.Root.localScale;
            var targetScale = new Vector3(0.94f, 1.055f, 1f);
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / baseDuration));
                view.Root.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                view.Root.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
                view.Root.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
        }

        private IEnumerator FlashImpact(BattleCombatantRuntimeView view, Color impactColor)
        {
            if (view == null || view.Artwork == null) yield break;
            var idleColor = view.Artwork.color;
            var actionColor = view.ActionArtwork == null ? Color.clear : view.ActionArtwork.color;
            var scale = view.Root.localScale;
            var flash = Color.Lerp(Color.white, impactColor, 0.35f);
            flash.a = idleColor.a;
            view.Artwork.color = flash;
            if (view.ActionArtwork != null)
            {
                var actionFlash = Color.Lerp(Color.white, impactColor, 0.35f);
                actionFlash.a = actionColor.a;
                view.ActionArtwork.color = actionFlash;
            }
            view.Root.localScale = new Vector3(scale.x * 1.08f, scale.y * 0.92f, 1f);
            yield return WaitCinematic(0.055f);
            if (view.Artwork != null) view.Artwork.color = idleColor;
            if (view.ActionArtwork != null) view.ActionArtwork.color = actionColor;
            if (view.Root != null) view.Root.localScale = scale;
        }

        private IEnumerator HoldImpactFrames(int frameCount)
        {
            for (var frame = 0; frame < Math.Max(1, frameCount); frame++)
            {
                while (_battleAnimationPaused && !_skipCurrentBattleBeat && !_skipBattleAnimation)
                    yield return null;
                if (_skipCurrentBattleBeat || _skipBattleAnimation) yield break;
                yield return null;
            }
        }

        private void SpawnCombatAfterimage(BattleCombatantRuntimeView view, float direction)
        {
            if (view == null || _battleVfx == null) return;
            var sprite = view.ActionArtwork != null && view.ActionArtwork.sprite != null
                ? view.ActionArtwork.sprite
                : view.Artwork?.sprite;
            if (sprite == null) return;
            var ghost = _battleVfx.SpawnPanel("Pooled Combat Afterimage",
                new Color(0.38f, 0.86f, 1f, _reducedFlash ? 0.13f : 0.30f));
            if (ghost == null) return;
            ghost.sprite = sprite;
            ghost.preserveAspect = true;
            ghost.rectTransform.anchorMin = ghost.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ghost.rectTransform.sizeDelta = view.Root.rect.size;
            ghost.rectTransform.anchoredPosition = view.Root.anchoredPosition - new Vector2(42f * direction, 0f);
            ghost.rectTransform.localScale = view.Root.localScale * 0.97f;
            ghost.rectTransform.localRotation = view.Root.localRotation;
            StartCoroutine(FadeCombatAfterimage(ghost));
        }

        private IEnumerator FadeCombatAfterimage(Image ghost)
        {
            if (ghost == null) yield break;
            var start = ghost.color;
            var elapsed = 0f;
            const float duration = 0.18f;
            while (elapsed < duration && ghost != null)
            {
                elapsed += CinematicDeltaTime();
                var color = start;
                color.a = Mathf.Lerp(start.a, 0f, Mathf.Clamp01(elapsed / duration));
                ghost.color = color;
                yield return null;
            }
            if (ghost != null && _battleVfx != null) _battleVfx.Release(ghost, 0f);
        }

        private IEnumerator RecoilCombatant(BattleCombatantRuntimeView view, float direction)
        {
            if (view == null) yield break;
            var priorSprite = view.ActionArtwork == null ? null : view.ActionArtwork.sprite;
            var priorIdleColor = view.Artwork == null ? Color.white : view.Artwork.color;
            var priorActionColor = view.ActionArtwork == null ? Color.clear : view.ActionArtwork.color;
            var hasHitPose = SetBattleArtPose011(view, BattleArtPoseDirector011.HitReaction);
            if (hasHitPose && view.Artwork != null && view.ActionArtwork != null)
            {
                var hidden = priorIdleColor; hidden.a = 0f; view.Artwork.color = hidden;
                var shown = priorActionColor; shown.a = 1f; view.ActionArtwork.color = shown;
            }
            var magnitude = (IsActionCloseup(view) ? 105f : 30f) * Mathf.Clamp(_battleShakeStrength, 0f, 1f);
            yield return MoveCombatant(view, view.Home, view.Home + new Vector2(magnitude * direction, 0f), 0.11f);
            yield return MoveCombatant(view, view.Root.anchoredPosition, view.Home, 0.15f);
            if (view.ActionArtwork != null)
            {
                view.ActionArtwork.sprite = priorSprite;
                view.ActionArtwork.color = priorActionColor;
            }
            if (view.Artwork != null) view.Artwork.color = priorIdleColor;
        }

        private IEnumerator ShakeBattlefield(float magnitude, float baseDuration)
        {
            if (_battlefieldCameraRoot == null || _reducedMotion || _battleShakeStrength <= 0.01f)
                yield break;

            var home = _battlefieldCameraRoot.anchoredPosition;
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                var phase = elapsed * 92f;
                var falloff = 1f - Mathf.Clamp01(elapsed / baseDuration);
                var strength = magnitude * Mathf.Clamp01(_battleShakeStrength) * falloff;
                _battlefieldCameraRoot.anchoredPosition = home +
                    new Vector2(Mathf.Sin(phase) * strength, Mathf.Cos(phase * 1.37f) * strength * 0.45f);
                yield return null;
            }
            if (_battlefieldCameraRoot != null) _battlefieldCameraRoot.anchoredPosition = home;
        }

        private IEnumerator PulseReducedMotion(BattleCombatantRuntimeView view, Color color)
        {
            if (view == null) { yield return WaitCinematic(0.15f); yield break; }
            var prior = view.Artwork.color;
            view.Artwork.color = Color.Lerp(prior, color, _reducedFlash ? 0.20f : 0.42f);
            yield return WaitCinematic(_reducedMotion ? 0.10f : 0.22f);
            if (view.Artwork != null) view.Artwork.color = prior;
        }

        private IEnumerator AnimateProjectile(
            BattleCombatantRuntimeView actor,
            BattleCombatantRuntimeView target,
            string glyph,
            Color color)
        {
            var projectile = AddEffectText("Mystic Projectile", glyph, color, 96);
            if (projectile == null) { yield return WaitCinematic(0.12f); yield break; }
            var from = EffectPoint(actor);
            var to = EffectPoint(target);
            projectile.rectTransform.anchoredPosition = from;
            var elapsed = 0f;
            while (elapsed < 0.34f && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                projectile.rectTransform.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / 0.34f));
                projectile.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(elapsed * 28f) * 0.12f);
                yield return null;
            }
            if (projectile != null && _battleVfx != null) _battleVfx.Release(projectile, 0f);
        }

        private IEnumerator WaitCinematic(float baseDuration)
        {
            var elapsed = 0f;
            while (elapsed < baseDuration && !_skipCurrentBattleBeat && !_skipBattleAnimation)
            {
                elapsed += CinematicDeltaTime();
                yield return null;
            }
        }

        private float CinematicDeltaTime() => _battleAnimationPaused
            ? 0f
            : Time.unscaledDeltaTime * Mathf.Max(1f, _battleAnimationSpeed);

        private void SpawnSlashEffect(BattleCombatantRuntimeView target, Color color, float width)
        {
            if (_battleEffectRoot == null) return;
            var slash = _battleVfx == null ? null : _battleVfx.SpawnPanel("Pooled Weapon Trail", color);
            if (slash == null) return;
            slash.rectTransform.anchorMin = slash.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            slash.rectTransform.sizeDelta = new Vector2(width, 18f);
            slash.rectTransform.anchoredPosition = EffectPoint(target);
            slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            _battleVfx.Release(slash, 0.30f / Mathf.Max(1f, _battleAnimationSpeed));
        }

        private void SpawnBurstEffect(BattleCombatantRuntimeView target, Color color, float size)
        {
            SpawnGlyphEffect(target, "✦", color, size);
            SpawnGlyphEffect(target, "◇", new Color(color.r, color.g, color.b, color.a * 0.72f), size * 0.78f);
        }

        private void SpawnGlyphEffect(BattleCombatantRuntimeView target, string glyph, Color color, float size)
        {
            if (_battleEffectRoot == null) return;
            var effect = AddEffectText("Pooled " + glyph + " Effect", glyph, color, Mathf.RoundToInt(size));
            if (effect == null) return;
            effect.rectTransform.anchoredPosition = EffectPoint(target);
            if (_battleVfx != null)
                _battleVfx.Release(effect, (_reducedMotion ? 0.18f : 0.48f) / Mathf.Max(1f, _battleAnimationSpeed));
        }

        private void SpawnAmountEffect(BattleCombatantRuntimeView target, BattlePresentationBeat beat, bool positive)
        {
            if (_battleEffectRoot == null || beat.Amount == 0) return;
            var prefix = positive ? "+" : "−";
            var suffix = beat.Family == BattleBeatFamily.Recovery || beat.Family == BattleBeatFamily.Formation
                ? string.Empty
                : " HP";
            var amount = AddEffectText("Authoritative Floating Result", prefix + Math.Abs(beat.Amount) + suffix,
                positive ? RuntimeUi.Positive : RuntimeUi.Error, 45);
            if (amount == null) return;
            amount.rectTransform.anchoredPosition = EffectPoint(target) + new Vector2(0f, 92f);
            if (_battleVfx != null) _battleVfx.Release(amount, 0.72f / Mathf.Max(1f, _battleAnimationSpeed));
        }

        private Text AddEffectText(string name, string value, Color color, int fontSize)
        {
            var text = _battleVfx == null ? null : _battleVfx.SpawnText(name, value, color, fontSize);
            if (text == null) return null;
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(Math.Max(220f, fontSize * 4f), Math.Max(140f, fontSize * 1.8f));
            text.raycastTarget = false;
            return text;
        }

        private Vector2 EffectPoint(BattleCombatantRuntimeView view)
        {
            if (_battleEffectRoot == null) return Vector2.zero;
            if (view == null) return Vector2.zero;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_battleEffectRoot, view.Root);
            return bounds.center;
        }

        private void ResetCinematicTransforms()
        {
            foreach (var view in _battleCombatants.Values)
            {
                if (view.Root == null) continue;
                view.Root.anchoredPosition = view.Home;
                view.Root.localRotation = view.HomeRotation;
                view.Root.localScale = Vector3.one;
            }
            if (_battlefieldCameraRoot != null)
            {
                _battlefieldCameraRoot.anchoredPosition = Vector2.zero;
                _battlefieldCameraRoot.localScale = Vector3.one;
            }
        }

        private void ApplyReducedMotionToCombatants()
        {
            foreach (var view in _battleCombatants.Values)
            {
                if (view.Artwork == null) continue;
                var idle = view.Artwork.GetComponent<M2BattleIdleMotion>();
                if (idle != null) idle.Configure(view.MemberId, _reducedMotion);
            }
        }

        private static string CompactPredictedAction(M2PredictedActionView action)
        {
            var growth = action.PredictedGrowth > 0 ? " · LEARN +" + action.PredictedGrowth : string.Empty;
            var breakthrough = action.BreakthroughOpportunity ? " · ✦ " + action.BreakthroughTargetArtName : string.Empty;
            var level = action.ArtLevel > 0
                ? " · LV" + action.ArtLevel +
                  (string.IsNullOrWhiteSpace(action.ArtPowerCue)
                      ? string.Empty
                      : " " + action.ArtPowerCue)
                : string.Empty;
            return action.ActorName + ": " + action.ArtName + level + " → " + action.TargetName +
                   " · MP " + action.PersonalMpCost + growth + breakthrough;
        }

        private static string CompactCommandButtonLabel(M2ForecastView forecast)
        {
            var command = PlayerCommandLabel(forecast);
            var target = forecast.TargetName ?? "OBJECTIVE";
            if (target.Length > 22) target = target.Substring(0, 21).TrimEnd() + "…";
            return command + "\nAP " + forecast.SharedApCost + " · → " + target.ToUpperInvariant();
        }

        private static string RelationshipSummary(M2BattleView battle)
        {
            var players = string.Join("  ◆  ", battle.PlayerUnions.Select(value =>
                value.DisplayName + " · " + value.Engagement));
            var enemies = string.Join("  ◆  ", battle.EnemyUnions.Select(value =>
                value.DisplayName + " · " + value.Engagement));
            return players.ToUpperInvariant() + "   ⇄   " + enemies.ToUpperInvariant();
        }

        private string BeatTitle(BattlePresentationBeat beat)
        {
            var registeredName = ForecastArtDisplayName(beat.ArtId);
            switch (beat.Family)
            {
                case BattleBeatFamily.BasicMartial: return "BASIC ATTACK";
                case BattleBeatFamily.CombatArt:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "COMBAT ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Mystic:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "MYSTIC ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Invocation:
                    return M2BattleReadableText021.ArtBannerTitle(
                        StringComparer.Ordinal.Equals(beat.EventType, "GREAT_COVENANT_INVOKED")
                            ? "GREAT COVENANT"
                            : "INVOCATION ECHO",
                        beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Restoration:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "RESTORATION", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Tactical:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "TACTICAL ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Guard:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "WARDING ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Interception:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "INTERCEPTION ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Learning: return "MEANINGFUL USE";
                case BattleBeatFamily.Breakthrough: return "NEW ART BREAKTHROUGH";
                case BattleBeatFamily.Formation:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "FORMATION ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Positioning:
                    return string.IsNullOrWhiteSpace(registeredName)
                        ? "SIDE STRIKE · BLIND SIDE OPEN"
                        : M2BattleReadableText021.ArtBannerTitle(
                            "POSITIONING ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Recovery:
                    return M2BattleReadableText021.ArtBannerTitle(
                        "RECOVERY ART", beat.ArtId, beat.Caption, registeredName);
                case BattleBeatFamily.Downed: return "DOWNED";
                case BattleBeatFamily.Result: return "BATTLE RESULT";
                default: return beat.EventType.Replace('_', ' ').ToUpperInvariant();
            }
        }

        private string ForecastArtDisplayName(string artId)
        {
            if (string.IsNullOrWhiteSpace(artId)) return string.Empty;
            var resolved = ForecastArtDisplayName(_battlePresentationBefore, artId);
            if (!string.IsNullOrWhiteSpace(resolved)) return resolved;
            resolved = ForecastArtDisplayName(_battlePresentationAfter, artId);
            if (!string.IsNullOrWhiteSpace(resolved)) return resolved;
            resolved = ForecastArtDisplayName(_coordinator?.State?.Battle, artId);
            if (!string.IsNullOrWhiteSpace(resolved)) return resolved;
            return Battle3DArtChoreography071.TryCreate(artId, out var choreography071)
                ? choreography071.DisplayName
                : string.Empty;
        }

        private static string ForecastArtDisplayName(M2BattleView battle, string artId)
        {
            if (battle?.Forecasts == null || string.IsNullOrWhiteSpace(artId)) return string.Empty;
            foreach (var forecast in battle.Forecasts)
            {
                if (forecast?.MemberActions == null) continue;
                foreach (var action in forecast.MemberActions)
                    if (action != null && StringComparer.Ordinal.Equals(action.ArtId, artId) &&
                        !string.IsNullOrWhiteSpace(action.ArtName)) return action.ArtName;
            }
            return string.Empty;
        }

        private static Color BeatColor(BattlePresentationBeat beat)
        {
            switch (beat.Family)
            {
                case BattleBeatFamily.Mystic:
                case BattleBeatFamily.Invocation: return BattleMp;
                case BattleBeatFamily.Restoration: return RuntimeUi.Positive;
                case BattleBeatFamily.Guard:
                case BattleBeatFamily.Interception: return BattleCohesion;
                case BattleBeatFamily.Positioning: return RuntimeUi.Warning;
                case BattleBeatFamily.Breakthrough:
                case BattleBeatFamily.Learning: return RuntimeUi.Warning;
                case BattleBeatFamily.Downed: return RuntimeUi.Error;
                case BattleBeatFamily.CombatArt:
                case BattleBeatFamily.Tactical: return BattleAp;
                default: return RuntimeUi.Text;
            }
        }

        // This helper is shared by every file in the partial M1FlowPresenter class.
        // Keep the legacy entry point here because the roster and expedition screens
        // also use it when authored display metadata is unavailable. Delegate to the
        // readable-text service so neither deep Art IDs nor outcome codes leak into UI.
        private static string HumanizePresentationId(string id) =>
            M2BattleReadableText021.ArtDisplayName(id, string.Empty, false);

        private static Color CommandFamilyColor(string commandId)
        {
            switch (commandId)
            {
                case "CMD_MYSTIC": return new Color(0.22f, 0.35f, 0.62f, 1f);
                case "CMD_HEAL": return new Color(0.16f, 0.40f, 0.30f, 1f);
                case "CMD_GUARD": return new Color(0.18f, 0.38f, 0.48f, 1f);
                case "CMD_ALL_OUT": return new Color(0.48f, 0.22f, 0.16f, 1f);
                case "CMD_SUPPORT": return new Color(0.30f, 0.26f, 0.48f, 1f);
                default: return RuntimeUi.ButtonNormal;
            }
        }

        private static Image AddAnchoredPanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            var panel = RuntimeUi.AddPanel(parent, name, color);
            panel.rectTransform.anchorMin = anchorMin;
            panel.rectTransform.anchorMax = anchorMax;
            panel.rectTransform.offsetMin = offsetMin;
            panel.rectTransform.offsetMax = offsetMax;
            return panel;
        }

        private static Text AddAnchoredText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            FontStyle style,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var text = RuntimeUi.AddText(parent, name, value, fontSize, alignment, color, style);
            text.rectTransform.anchorMin = anchorMin;
            text.rectTransform.anchorMax = anchorMax;
            text.rectTransform.offsetMin = new Vector2(8f, 4f);
            text.rectTransform.offsetMax = new Vector2(-8f, -4f);
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
            return text;
        }

        private static Button AddAnchoredButton(
            Transform parent,
            string name,
            string label,
            Action action,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var button = RuntimeUi.AddButton(parent, name, label, action, RuntimeUi.MinimumTouchPixels, color);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(5f, 5f);
            rect.offsetMax = new Vector2(-5f, -5f);
            return button;
        }

        private static void SetButtonFont(Button button, int size)
        {
            var text = button == null ? null : button.GetComponentInChildren<Text>();
            if (text == null) return;
            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(16, size - 10);
            text.resizeTextMaxSize = size;
            text.lineSpacing = 0.92f;
            if (size >= 34) text.font = RuntimeUi.DisplayFont;
            var shadow = text.GetComponent<Shadow>();
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
        }

        private static void SetButtonText(Button button, string value)
        {
            var label = button == null ? null : button.GetComponentInChildren<Text>();
            if (label != null) label.text = value;
        }

        private string ShakeLabel()
        {
            if (_battleShakeStrength <= 0.01f) return "SHAKE · OFF";
            if (_battleShakeStrength < 0.75f) return "SHAKE · LOW";
            return "SHAKE · FULL";
        }
    }
}
