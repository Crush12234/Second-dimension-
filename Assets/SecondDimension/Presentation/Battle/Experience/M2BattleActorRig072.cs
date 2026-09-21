using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// A presentation-only two-layer actor rig for the 072 battle experience.
    /// Player poses resolve through BattleArtRuntimeRegistry011 before their existing
    /// authored standee/action fallbacks. Enemies prefer their hand-picked Battle079/086
    /// identities, then the Battle075 two-state cutouts, with older clean standees and
    /// pose sheets retained only as recovery paths. Bosses receive authored identities.
    /// No runtime silhouette is ever accepted or generated here.
    /// </summary>
    public sealed class M2BattleActorRig072
    {
        private const string RuntimeEnemySilhouette070 = "RUNTIME_ENEMY_SILHOUETTE_070";
        private const string GenericEnemy01 = "ENEMY_GATE_GNAWER_01";
        private const string GenericEnemy02 = "ENEMY_GATE_GNAWER_02";
        private const string GenericEnemy03 = "ENEMY_GATE_GNAWER_03";
        private const string AuthoredBattleRoot = "SecondDimension/Art/Battle";
        private const string GeneratedEnemyRoot075 = "SecondDimension/Art/Battle075/Enemies";
        private const string OriginalEnemyRoot086 = "SecondDimension/Art/Battle086/Enemies";
        private const string HingeEaterBossToken075 = "HINGE_EATER_COLOSSUS";
        private const string CaptainRavelToken086 = "ENEMY_CAPTAIN_RAVEL_";
        private const string GateheartWardenToken086 = "ENEMY_GATEHEART_WARDEN_";
        private const string BrassjawPacklordToken087 = "ENEMY_BRASSJAW_PACKLORD_";

        // These ranks are presentation metadata only. In particular, Gateheart
        // Warden must not become BossEnemy075: that flag is part of the certified
        // Gate-Eater encounter contract and is consumed by battle-specific logic.
        public const int StandardEnemyPresentationTier086 = 0;
        public const int MinibossEnemyPresentationTier086 = 1;
        public const int MajorEnemyPresentationTier086 = 2;
        public const float CaptainRavelPresentationScale086 = 1.18f;
        public const float GateheartWardenPresentationScale086 = 1.38f;

        // DOWNED is an artwork-only treatment. Keeping these values off Root is
        // important: exact-recipe completion measures Root against its authored
        // home transform, while the lower third must remain upright and legible.
        public const float DownedArtworkTiltDegrees076 = 64f;
        public const float DownedArtworkSettlePixels076 = 44f;
        public const float DownedArtworkScale076 = 0.88f;
        private const float DownedArtworkSideOffsetPixels076 = 18f;

        public const string GateEaterDisplayName076 = "THE GATE-EATER";
        public const string GateEaterIdleResourcePath076 =
            "SecondDimension/Art/Battle075/Enemies/HINGE_EATER_COLOSSUS_IDLE";
        public const string GateEaterActionResourcePath076 =
            "SecondDimension/Art/Battle075/Enemies/HINGE_EATER_COLOSSUS_ACTION";

        public const string EnemyCutoutShaderResource075 =
            "SecondDimension/UI/EnemyCutoutClean075";
        public const string EnemyCutoutShaderName075 =
            "SecondDimension/UI/EnemyCutoutClean075";

        private readonly bool _enemy;
        private readonly string _unionId;
        private M2BattleMemberView _member;
        private Image _currentArtwork;
        private Image _incomingArtwork;
        private readonly Image _groundShadow;
        private readonly Image _namePlate;
        private readonly Image _healthRail076;
        private readonly Image _healthFill;
        private readonly Text _healthLabel076;
        private readonly Text _nameLabel;
        private readonly CanvasGroup _canvas;
        private readonly CanvasGroup _lowerThirdCanvas076;
        private int _presentedCurrentHp076;
        private int _presentedMaximumHp076;
        private float _presentedHpFill076;
        private string _currentPoseId = string.Empty;
        private string _currentResourcePath = string.Empty;
        private Sprite _currentSprite;
        private EnemyArt700SpriteLease090 _currentEnemyArtLease090;
        private EnemyArt700SpriteLease090 _incomingEnemyArtLease090;
        private Material _enemyCutoutMaterial075;
        private EnemyRemasterTheme098 _enemyRemasterTheme098;
        private readonly M2EnemyThreatVisual089 _enemyThreat089;
        private float _alliedVisibleHeight091;
        private float _alliedMaximumWidth091;
        private float _alliedActionMaximumWidth093;
        private float _alliedStandingFrameAspect091;

        // Command-view body size comes from idle silhouettes. Wide spell/action
        // artwork must not shrink the entire party before anyone takes an action.
        public float AlliedStandingFrameAspect091
        {
            get
            {
                if (_enemy) return 1f;
                if (_alliedStandingFrameAspect091 > 0f) return _alliedStandingFrameAspect091;
                foreach (var pose in new[] { BattleArtPoseDirector011.Idle })
                {
                    if (!TryResolveAuthoredPose(pose, out var sprite, out _, out var lease)) continue;
                    var visible = M1SilhouetteFraming091.VisibleRect091(sprite);
                    if (visible.height > 1f)
                        _alliedStandingFrameAspect091 = Mathf.Max(_alliedStandingFrameAspect091,
                            sprite.rect.width / visible.height);
                    ReleaseEnemyArtLease090(ref lease);
                }
                return _alliedStandingFrameAspect091 = Mathf.Max(0.01f, _alliedStandingFrameAspect091);
            }
        }

        public void ConfigureAlliedSilhouetteFit091(float visibleHeight, float maximumWidth,
            float maximumActionWidth093 = 0f)
        {
            if (_enemy) return;
            _alliedVisibleHeight091 = Mathf.Max(1f, visibleHeight);
            _alliedMaximumWidth091 = Mathf.Max(1f, maximumWidth);
            _alliedActionMaximumWidth093 = Mathf.Max(_alliedMaximumWidth091, maximumActionWidth093);
            ApplyArtworkPoseTreatment076(_currentArtwork, _currentPoseId, _currentArtwork.color.a);
            ResetArtworkPoseTreatment076(_incomingArtwork);
        }

        public M2BattleActorRig072(
            RectTransform parent,
            M2BattleUnionView union,
            M2BattleMemberView member,
            bool enemy)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (union == null) throw new ArgumentNullException(nameof(union));
            if (member == null) throw new ArgumentNullException(nameof(member));

            _enemy = enemy;
            _unionId = union.UnionId ?? string.Empty;
            _member = member;
            _enemyThreat089 = M2EnemyThreatPalette089.Resolve(
                member,
                enemy,
                enemy && IsHingeEaterBoss075(member.MemberId));

            var rootObject = new GameObject(
                "Battle Actor 072 · " + PlayerFacingDisplayName076(
                    member,
                    enemy ? "Enemy" : "Guild Member"),
                typeof(RectTransform), typeof(CanvasGroup));
            Root = rootObject.GetComponent<RectTransform>();
            Root.SetParent(parent, false);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.pivot = new Vector2(0.5f, 0f);
            Root.sizeDelta = new Vector2(300f, 560f);
            _canvas = rootObject.GetComponent<CanvasGroup>();
            _canvas.interactable = false;
            _canvas.blocksRaycasts = false;

            var boss = BossEnemy075;
            var originalPresentationTier086 = OriginalEnemyPresentationTier086;
            var prominentEnemy086 = boss || originalPresentationTier086 > StandardEnemyPresentationTier086;
            var majorEnemy086 = boss || originalPresentationTier086 >= MajorEnemyPresentationTier086;
            _groundShadow = CreateImage(
                Root, "Authored Contact Shadow 076",
                majorEnemy086
                    ? new Vector2(0.005f, 0.002f)
                    : prominentEnemy086 ? new Vector2(0.025f, 0.005f) : new Vector2(0.045f, 0.008f),
                majorEnemy086
                    ? new Vector2(0.995f, 0.155f)
                    : prominentEnemy086 ? new Vector2(0.975f, 0.140f) : new Vector2(0.955f, 0.125f));
            _groundShadow.color = new Color(
                1f,
                1f,
                1f,
                majorEnemy086 ? 0.86f : prominentEnemy086 ? 0.78f : 0.70f);
            _groundShadow.raycastTarget = false;
            if (BattleArtRuntimeRegistry011.TryResolveUiAsset(
                    "SHARED_GROUND_SHADOW", out _, out var shadowSprite) && shadowSprite != null)
            {
                _groundShadow.sprite = shadowSprite;
                _groundShadow.preserveAspect = false;
            }
            else
            {
                // A transparent panel is preferable to inventing a procedural actor shadow.
                _groundShadow.color = Color.clear;
            }

            _currentArtwork = CreateImage(
                Root, "Authored Pose A 072",
                new Vector2(0f, 0.07f), new Vector2(1f, 1f));
            _incomingArtwork = CreateImage(
                Root, "Authored Pose B 072",
                new Vector2(0f, 0.07f), new Vector2(1f, 1f));
            ConfigureArtwork(
                _currentArtwork, enemy, boss, originalPresentationTier086, _enemyThreat089);
            ConfigureArtwork(
                _incomingArtwork, enemy, boss, originalPresentationTier086, _enemyThreat089);
            if (_enemy) InstallEnemyCutoutMaterial075();
            _incomingArtwork.color = Color.clear;

            _namePlate = CreateImage(
                Root, "Authored Actor Lower Third 076",
                majorEnemy086
                    ? new Vector2(0.055f, 0.002f)
                    : prominentEnemy086 ? new Vector2(0.09f, 0.002f) : new Vector2(0.14f, 0.002f),
                majorEnemy086
                    ? new Vector2(0.945f, 0.078f)
                    : prominentEnemy086 ? new Vector2(0.91f, 0.078f) : new Vector2(0.86f, 0.078f));
            _namePlate.color = enemy
                ? WithAlpha089(
                    _enemyThreat089.PlateColor,
                    majorEnemy086 ? 0.92f : prominentEnemy086 ? 0.88f : 0.82f)
                : new Color(0.018f, 0.055f, 0.085f, 0.66f);
            _namePlate.raycastTarget = false;
            // Cinematic focus is applied to the actor root. The authored artwork may
            // dim, topple, and desaturate, but a downed combatant's identity and exact
            // HP must never become an anonymous floating bar. This child group keeps
            // the lower third at full presentation opacity even when Root is de-emphasized.
            _lowerThirdCanvas076 = _namePlate.gameObject.AddComponent<CanvasGroup>();
            _lowerThirdCanvas076.alpha = 1f;
            _lowerThirdCanvas076.interactable = false;
            _lowerThirdCanvas076.blocksRaycasts = false;
            _lowerThirdCanvas076.ignoreParentGroups = true;

            _healthRail076 = CreateImage(
                _namePlate.rectTransform, "Visible Health Rail 076",
                new Vector2(0.018f, 0.025f), new Vector2(0.982f, 0.455f));
            _healthRail076.color = new Color(0.006f, 0.012f, 0.020f, 0.96f);
            _healthRail076.raycastTarget = false;
            var healthRailOutline076 = _healthRail076.gameObject.AddComponent<Outline>();
            healthRailOutline076.effectColor = enemy
                ? WithAlpha089(_enemyThreat089.FrameColor, 0.92f)
                : new Color(0.38f, 0.96f, 0.76f, 0.88f);
            healthRailOutline076.effectDistance = new Vector2(1f, -1f);

            _healthFill = CreateImage(
                _healthRail076.rectTransform, "Health Fill 072",
                new Vector2(0.008f, 0.10f), new Vector2(0.992f, 0.90f));
            _healthFill.color = enemy
                ? new Color(1f, 0.24f, 0.20f, 1f)
                : new Color(0.20f, 0.92f, 0.64f, 1f);
            // This color-only Image has no sprite. Unity's Filled mesh path is not
            // used without an active sprite, so fillAmount alone would still draw a
            // visually full quad. The HP ratio is therefore represented by the
            // RectTransform width in PresentHp076.
            _healthFill.type = Image.Type.Simple;
            _healthFill.raycastTarget = false;

            _healthLabel076 = CreateText(
                _namePlate.rectTransform, "Visible HP Value 076",
                new Vector2(0.025f, 0.015f), new Vector2(0.975f, 0.475f));
            _healthLabel076.alignment = TextAnchor.MiddleCenter;
            _healthLabel076.fontSize = majorEnemy086 ? 22 : 21;
            _healthLabel076.fontStyle = FontStyle.Bold;
            _healthLabel076.color = Color.white;
            _healthLabel076.resizeTextForBestFit = true;
            _healthLabel076.resizeTextMinSize = 16;
            _healthLabel076.resizeTextMaxSize = majorEnemy086 ? 22 : 21;
            var healthOutline076 = _healthLabel076.gameObject.AddComponent<Outline>();
            healthOutline076.effectColor = new Color(0f, 0f, 0f, 0.92f);
            healthOutline076.effectDistance = new Vector2(1f, -1f);

            _nameLabel = CreateText(
                _namePlate.rectTransform, "Member Display Name 072",
                new Vector2(0.035f, 0.47f), new Vector2(0.965f, 1f));
            _nameLabel.alignment = TextAnchor.MiddleCenter;
            _nameLabel.fontSize = majorEnemy086 ? 22 : prominentEnemy086 ? 20 : 19;
            _nameLabel.fontStyle = FontStyle.Bold;
            _nameLabel.color = new Color(0.96f, 0.95f, 0.88f, 1f);
            _nameLabel.resizeTextForBestFit = true;
            _nameLabel.resizeTextMinSize = 15;
            _nameLabel.resizeTextMaxSize = majorEnemy086 ? 22 : prominentEnemy086 ? 20 : 19;
            var nameOutline076 = _nameLabel.gameObject.AddComponent<Outline>();
            nameOutline076.effectColor = new Color(0f, 0f, 0f, 0.94f);
            nameOutline076.effectDistance = new Vector2(1f, -1f);

            Refresh(member);
            SetPoseImmediate(member.Downed
                ? BattleArtPoseDirector011.Downed
                : BattleArtPoseDirector011.Idle);
        }

        public RectTransform Root { get; }
        public int InstanceId => Root == null ? 0 : Root.gameObject.GetInstanceID();
        public string MemberId => _member?.MemberId ?? string.Empty;
        public string UnionId => _unionId;
        public string DisplayName => PlayerFacingDisplayName076(
            _member,
            _enemy ? "Enemy" : "Guild Member");
        public bool Enemy => _enemy;
        public bool BossEnemy075 => _enemy && IsHingeEaterBoss075(MemberId);
        public int OriginalEnemyPresentationTier086 => _enemy
            ? ResolveOriginalEnemyPresentationTier086(_member)
            : StandardEnemyPresentationTier086;
        public bool ProminentEnemyPresentation086 =>
            OriginalEnemyPresentationTier086 > StandardEnemyPresentationTier086;
        public bool MajorEnemyPresentation086 =>
            OriginalEnemyPresentationTier086 >= MajorEnemyPresentationTier086;
        public float OriginalEnemyPresentationScale086 =>
            OriginalEnemyPresentationTier086 >= MajorEnemyPresentationTier086
                ? GateheartWardenPresentationScale086
                : OriginalEnemyPresentationTier086 == MinibossEnemyPresentationTier086
                    ? CaptainRavelPresentationScale086
                    : 1f;
        public float EnemyArtFamilyScale090 => _enemy
            ? EnemyArt700Runtime090.RelativeScale090(_member?.EnemyArtBaseId090)
            : 1f;
        public bool Downed => _presentedMaximumHp076 > 0
            ? _presentedCurrentHp076 <= 0
            : _member != null && _member.Downed;
        public Image GroundShadow076 => _groundShadow;
        public Image NamePlate076 => _namePlate;
        public Image HealthRail076 => _healthRail076;
        public Image HealthFill076 => _healthFill;
        public Text HealthLabel076 => _healthLabel076;
        public Text NameLabel076 => _nameLabel;
        public CanvasGroup LowerThirdCanvas076 => _lowerThirdCanvas076;
        public Image CurrentArtwork076 => _currentArtwork;
        public int PresentedCurrentHp076 => _presentedCurrentHp076;
        public int PresentedMaximumHp076 => _presentedMaximumHp076;
        public float PresentedHpFill076 => _presentedHpFill076;
        public int EnemyThreatTier089 => _enemy ? _enemyThreat089.Tier : 0;
        public string EnemyThreatLabel089 => _enemy ? _enemyThreat089.Label : string.Empty;
        public Color EnemyThreatTint089 => _enemy ? _enemyThreat089.ArtworkTint : Color.white;
        public string CurrentPoseId => _currentPoseId;
        public string CurrentResourcePath => _currentResourcePath;
        public string CurrentEnemyRemasterVariantId098 => _enemyRemasterTheme098?.VariantId098 ?? string.Empty;
        public string CurrentEnemyRemasterThemeName098 => _enemyRemasterTheme098?.Name098 ?? string.Empty;
        public Vector2 HomePosition { get; private set; }
        public Vector3 HomeScale { get; private set; } = Vector3.one;
        public int MissingPoseCount { get; private set; }
        public int SuccessfulPoseChangeCount { get; private set; }
        public string EnemyArtworkShaderName075 =>
            _enemyCutoutMaterial075 == null || _enemyCutoutMaterial075.shader == null
                ? string.Empty
                : _enemyCutoutMaterial075.shader.name;

        public void Refresh(M2BattleMemberView member)
        {
            if (member == null) return;
            _member = member;
            PresentHp076(member.CurrentHp, member.MaximumHp);

            if (member.Downed && !StringComparer.Ordinal.Equals(
                    _currentPoseId, BattleArtPoseDirector011.Downed))
                SetPoseImmediate(BattleArtPoseDirector011.Downed);
        }

        /// <summary>
        /// Applies an already-authoritative HP value to presentation only. The member
        /// view remains untouched so animation can never feed state back into combat.
        /// </summary>
        public void PresentHp076(int currentHp, int maximumHp)
        {
            _presentedMaximumHp076 = Math.Max(0, maximumHp);
            _presentedCurrentHp076 = Mathf.Clamp(currentHp, 0, _presentedMaximumHp076);
            _presentedHpFill076 = _presentedMaximumHp076 <= 0
                ? 0f
                : Mathf.Clamp01(_presentedCurrentHp076 / (float)_presentedMaximumHp076);
            if (_healthFill != null)
            {
                var maximumAnchor076 = _healthFill.rectTransform.anchorMax;
                maximumAnchor076.x = Mathf.Lerp(0.008f, 0.992f, _presentedHpFill076);
                _healthFill.rectTransform.anchorMax = maximumAnchor076;
            }
            if (_healthLabel076 != null)
                _healthLabel076.text = "HP " +
                                         _presentedCurrentHp076.ToString(CultureInfo.InvariantCulture) + " / " +
                                         _presentedMaximumHp076.ToString(CultureInfo.InvariantCulture);
            RefreshLowerThirdState076(_currentPoseId);
        }

        public void PresentHpDelta076(int signedDelta)
        {
            PresentHp076(
                Mathf.Clamp(_presentedCurrentHp076 + signedDelta, 0, _presentedMaximumHp076),
                _presentedMaximumHp076);
        }

        public void SetLayout(Vector2 anchor, Vector2 size, float scale, int siblingIndex)
        {
            if (Root == null) return;
            Root.anchorMin = Root.anchorMax = anchor;
            Root.anchoredPosition = Vector2.zero;
            Root.sizeDelta = size;
            HomePosition = Root.anchoredPosition;
            HomeScale = new Vector3(scale, scale, 1f);
            Root.localScale = HomeScale;
            Root.localRotation = Quaternion.identity;
            Root.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, Root.parent.childCount - 1));
        }

        public void SetEmphasis(float alpha, float scaleMultiplier)
        {
            if (_canvas != null) _canvas.alpha = Mathf.Clamp01(alpha);
            if (Root != null)
                Root.localScale = HomeScale * Mathf.Max(0.1f, scaleMultiplier);
        }

        public void SetStageOffset(Vector2 offset)
        {
            if (Root != null) Root.anchoredPosition = HomePosition + offset;
        }

        public void ReturnHome()
        {
            if (Root == null) return;
            Root.anchoredPosition = HomePosition;
            Root.localScale = HomeScale;
            Root.localRotation = Quaternion.identity;
            if (_canvas != null) _canvas.alpha = 1f;
        }

        public bool SetPoseImmediate(string poseId)
        {
            if (!TryResolveAuthoredPose(
                    poseId,
                    out var sprite,
                    out var resourcePath,
                    out var enemyArtLease090))
            {
                MissingPoseCount++;
                return false;
            }

            ReleaseEnemyArtLease090(ref _currentEnemyArtLease090);
            ReleaseEnemyArtLease090(ref _incomingEnemyArtLease090);
            _currentEnemyArtLease090 = enemyArtLease090;
            _currentArtwork.sprite = sprite;
            ApplyArtworkPoseTreatment076(_currentArtwork, poseId, 1f);
            _incomingArtwork.sprite = null;
            ResetArtworkPoseTreatment076(_incomingArtwork);
            _incomingArtwork.color = Color.clear;
            _currentSprite = sprite;
            _currentPoseId = poseId ?? string.Empty;
            _currentResourcePath = resourcePath ?? string.Empty;
            RefreshLowerThirdState076(_currentPoseId);
            SuccessfulPoseChangeCount++;
            return true;
        }

        public IEnumerator CrossfadeToPose(
            string poseId,
            float duration,
            Func<float> speed,
            Func<bool> skip)
        {
            if (!TryResolveAuthoredPose(
                    poseId,
                    out var sprite,
                    out var resourcePath,
                    out var enemyArtLease090))
            {
                MissingPoseCount++;
                yield break;
            }

            if (_currentSprite == sprite || duration <= 0f || ShouldSkip(skip))
            {
                ReleaseEnemyArtLease090(ref _currentEnemyArtLease090);
                ReleaseEnemyArtLease090(ref _incomingEnemyArtLease090);
                _currentEnemyArtLease090 = enemyArtLease090;
                _currentArtwork.sprite = sprite;
                ApplyArtworkPoseTreatment076(_currentArtwork, poseId, 1f);
                _incomingArtwork.sprite = null;
                ResetArtworkPoseTreatment076(_incomingArtwork);
                _incomingArtwork.color = Color.clear;
                _currentSprite = sprite;
                _currentPoseId = poseId ?? string.Empty;
                _currentResourcePath = resourcePath ?? string.Empty;
                RefreshLowerThirdState076(_currentPoseId);
                SuccessfulPoseChangeCount++;
                yield break;
            }

            ReleaseEnemyArtLease090(ref _incomingEnemyArtLease090);
            _incomingEnemyArtLease090 = enemyArtLease090;
            _incomingArtwork.sprite = sprite;
            ApplyArtworkPoseTreatment076(_incomingArtwork, poseId, 0f);
            var elapsed = 0f;
            while (elapsed < duration && !ShouldSkip(skip))
            {
                elapsed += Time.unscaledDeltaTime * ResolveSpeed(speed);
                var amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetArtworkPoseAlpha076(_currentArtwork, _currentPoseId, 1f - amount);
                SetArtworkPoseAlpha076(_incomingArtwork, poseId, amount);
                yield return null;
            }

            ApplyArtworkPoseTreatment076(_incomingArtwork, poseId, 1f);
            SetArtworkPoseAlpha076(_currentArtwork, _currentPoseId, 0f);
            var previous = _currentArtwork;
            _currentArtwork = _incomingArtwork;
            _incomingArtwork = previous;
            ReleaseEnemyArtLease090(ref _currentEnemyArtLease090);
            _currentEnemyArtLease090 = _incomingEnemyArtLease090;
            _incomingEnemyArtLease090 = null;
            _incomingArtwork.sprite = null;
            ResetArtworkPoseTreatment076(_incomingArtwork);
            _incomingArtwork.color = Color.clear;
            _currentSprite = sprite;
            _currentPoseId = poseId ?? string.Empty;
            _currentResourcePath = resourcePath ?? string.Empty;
            RefreshLowerThirdState076(_currentPoseId);
            SuccessfulPoseChangeCount++;
        }

        private void RefreshLowerThirdState076(string poseId)
        {
            if (_nameLabel == null || _namePlate == null) return;
            var downed076 = Downed ||
                            StringComparer.Ordinal.Equals(
                                poseId,
                                BattleArtPoseDirector011.Downed);
            _nameLabel.text = PlayerFacingDisplayName076(
                                  _member,
                                  _enemy ? "Enemy" : "Guild Member") +
                              (_enemy
                                  ? "  •  THREAT " + _enemyThreat089.RomanTier
                                  : string.Empty) +
                              (downed076 ? "  •  DOWN" : string.Empty);
            _namePlate.color = _enemy
                ? WithAlpha089(
                    _enemyThreat089.PlateColor,
                    downed076
                        ? 0.96f
                        : BossEnemy075 || MajorEnemyPresentation086
                            ? 0.82f
                            : ProminentEnemyPresentation086 ? 0.80f : 0.76f)
                : new Color(0.018f, 0.055f, 0.085f, downed076 ? 0.94f : 0.66f);
            if (_lowerThirdCanvas076 != null) _lowerThirdCanvas076.alpha = 1f;
        }

        public void Dispose()
        {
            ReleaseEnemyArtLease090(ref _currentEnemyArtLease090);
            ReleaseEnemyArtLease090(ref _incomingEnemyArtLease090);
            if (_currentArtwork != null) _currentArtwork.material = null;
            if (_incomingArtwork != null) _incomingArtwork.material = null;
            var enemyCutoutMaterial = _enemyCutoutMaterial075;
            _enemyCutoutMaterial075 = null;
            if (Root != null)
            {
                Root.gameObject.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(Root.gameObject);
                else UnityEngine.Object.DestroyImmediate(Root.gameObject);
            }
            if (enemyCutoutMaterial != null)
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(enemyCutoutMaterial);
                else UnityEngine.Object.DestroyImmediate(enemyCutoutMaterial);
            }
        }

        private bool TryResolveAuthoredPose(
            string requestedPoseId,
            out Sprite sprite,
            out string resourcePath,
            out EnemyArt700SpriteLease090 enemyArtLease090)
        {
            if (!TryResolveAuthoredPoseSource091(requestedPoseId, out sprite,
                    out resourcePath, out enemyArtLease090)) return false;
            // Raw-file enemy sprites are already framed and owned by their lease.
            // Registry/resource poses use the shared cached silhouette framing.
            if (enemyArtLease090 == null)
                sprite = M1SilhouetteFraming091.FrameResourceSprite091(sprite);
            if (_enemy && _enemyCutoutMaterial075 != null && !TitanArt161.IsBoss(MemberId))
                EnemyRemasterTheme098.ApplyToMaterial098(_enemyCutoutMaterial075,
                    _member.EnemyArtBaseId090, _member.EnemyArtVariantId090, sprite,
                    out _enemyRemasterTheme098);
            return sprite != null;
        }

        private bool TryResolveAuthoredPoseSource091(
            string requestedPoseId,
            out Sprite sprite,
            out string resourcePath,
            out EnemyArt700SpriteLease090 enemyArtLease090)
        {
            enemyArtLease090 = null;
            var poseId = BattleArtPoseDirector011.IsSupportedPoseId(requestedPoseId)
                ? requestedPoseId
                : BattleArtPoseDirector011.Idle;

            // Titan identity owns its exact body before the generic enemy routes.
            // A packaging error must not display another creature as this Titan.
            if (TitanArt161.IsMember(MemberId))
                return TitanArt161.TryResolvePose(MemberId, poseId, out sprite, out resourcePath);

            // The generated 075 set is the certified enemy presentation. Anticipation
            // and primary actions use its action cutout; every other state returns to
            // idle. The boss is routed by stable member identity, never by hash. Player
            // rigs deliberately skip this branch and keep their full authored poses.
            if (_enemy)
            {
                // Enemy Art 700 is a presentation-only, bounded raw-file load.
                // Explicit IDs were committed by the existing battle authority,
                // so this never rerolls a visual during a pose change or reload.
                var enemyArtPose090 = UsesEnemyActionPose(poseId)
                    ? EnemyArt700Pose090.Attack
                    : EnemyArt700Pose090.Idle;
                if (!string.IsNullOrWhiteSpace(_member?.EnemyArtBaseId090) &&
                    !string.IsNullOrWhiteSpace(_member.EnemyArtVariantId090) &&
                    EnemyArt700Runtime090.TryAcquireSprite090(
                        _member.EnemyArtBaseId090,
                        _member.EnemyArtVariantId090,
                        enemyArtPose090,
                        out sprite,
                        out enemyArtLease090,
                        out _))
                {
                    resourcePath = EnemyArtRemaster098.TrySourcePath098(sprite, out var remasterPath098)
                        ? remasterPath098
                        : "EnemyArt700/" + _member.EnemyArtVariantId090 + "/" +
                          (enemyArtPose090 == EnemyArt700Pose090.Attack ? "ATTACK" : "IDLE");
                    return true;
                }

                // Passes 086-089 give the previously repeated outer-gate enemy
                // families their own original standees. Resolve these newest
                // production assets before the older Chapter Two compatibility map;
                // otherwise its Echo Stalker and Chaincaller aliases would intercept
                // the dedicated 089 art. Runtime/spawn suffixes remain presentation-
                // only so save and encounter identities are unchanged.
                if ((TryResolveOriginalEnemyResourcePath086(
                         _member?.PortraitAuthorityId, out resourcePath) ||
                     TryResolveOriginalEnemyResourcePath086(
                         MemberId, out resourcePath)))
                {
                    sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
                    if (sprite != null && IsAllowedAuthoredPath(resourcePath))
                        return true;
                }

                // Compatibility only: keep Chapter Two's earlier bespoke identities
                // available if an authority is not covered by the current production
                // enemy set. These single-pose standees intentionally persist for
                // every beat rather than falling through to generic Gate Gnawer art.
                if (M1VisualAssets.TryResolveChapterTwoEnemyBattleStandee079(
                        MemberId, out sprite, out resourcePath) &&
                    IsAllowedAuthoredPath(resourcePath))
                    return true;

                resourcePath = GeneratedEnemyPath075(
                    GenericEnemyIdentity(),
                    BossEnemy075,
                    UsesEnemyActionPose(poseId));
                sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
                if (sprite != null && IsAllowedAuthoredPath(resourcePath)) return true;

                // Recovery only: these older standees are clean enough to keep the
                // battle playable if a generated resource is ever lost in packaging.
                resourcePath = GenericEnemyLegacyPath(
                    GenericEnemyIdentity(),
                    UsesEnemyActionPose(poseId));
                sprite = BattleArtRuntimeRegistry011.LoadSprite(resourcePath);
                if (sprite != null && IsAllowedAuthoredPath(resourcePath)) return true;
            }

            if (BattleArtRuntimeRegistry011.TryResolvePose(
                    MemberId, poseId, out sprite, out resourcePath) &&
                IsAllowedAuthoredPath(resourcePath))
                return true;

            // Signature recruits use deterministic campaign-instance IDs (SIGI_*),
            // while their full pose manifests are keyed by the stable authored
            // identity carried in PortraitAuthorityId (SIGREC_*).
            var portraitAuthority = _member?.PortraitAuthorityId ?? string.Empty;
            if (!StringComparer.Ordinal.Equals(portraitAuthority, MemberId) &&
                BattleArtRuntimeRegistry011.TryResolvePose(
                    portraitAuthority, poseId, out sprite, out resourcePath) &&
                IsAllowedAuthoredPath(resourcePath))
                return true;

            if (_enemy)
            {
                var genericEnemyId = GenericEnemyIdentity();
                if (BattleArtRuntimeRegistry011.TryResolvePose(
                        genericEnemyId, poseId, out sprite, out resourcePath) &&
                    IsAllowedAuthoredPath(resourcePath))
                    return true;

                if (!StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.Idle) &&
                    BattleArtRuntimeRegistry011.TryResolvePose(
                        genericEnemyId, BattleArtPoseDirector011.Idle,
                        out sprite, out resourcePath) && IsAllowedAuthoredPath(resourcePath))
                    return true;

                sprite = null;
                resourcePath = string.Empty;
                return false;
            }

            var preferAction = StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.ActionPrimary) ||
                               StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.RolePrimary);
            if (preferAction && M1VisualAssets.TryResolveBattleActionPose(
                    MemberId,
                    _member?.VisualSeed ?? string.Empty,
                    _member?.RaceId ?? string.Empty,
                    _member?.PortraitAuthorityId ?? string.Empty,
                    out sprite,
                    out resourcePath) && IsAllowedAuthoredPath(resourcePath))
                return true;

            if (M1VisualAssets.TryResolveBattleStandee(
                    MemberId,
                    _member?.VisualSeed ?? string.Empty,
                    _member?.RaceId ?? string.Empty,
                    _member?.PortraitAuthorityId ?? string.Empty,
                    out sprite,
                    out resourcePath) && IsAllowedAuthoredPath(resourcePath))
                return true;

            // Procedural recruits do not all have a dedicated full-body pose set.
            // Their authored race/class portrait is a far better stable fallback
            // than an empty Image, and pose motion remains presentation-only.
            if (M1VisualAssets.TryResolvePortrait(
                    MemberId,
                    _member?.VisualSeed ?? string.Empty,
                    _member?.RaceId ?? string.Empty,
                    _member?.PortraitAuthorityId ?? string.Empty,
                    _member?.ClassName ?? string.Empty,
                    out sprite,
                    out resourcePath) && IsAllowedAuthoredPath(resourcePath))
                return true;

            sprite = null;
            resourcePath = string.Empty;
            return false;
        }

        private static void ReleaseEnemyArtLease090(
            ref EnemyArt700SpriteLease090 lease090)
        {
            var release090 = lease090;
            lease090 = null;
            release090?.Dispose();
        }

        private string GenericEnemyIdentity()
        {
            var authoredIdentity = StableGateGnawerIdentity086(_member != null ? _member.PortraitAuthorityId : string.Empty);
            if (string.IsNullOrWhiteSpace(authoredIdentity))
            {
                authoredIdentity = StableGateGnawerIdentity086(MemberId);
            }

            if (!string.IsNullOrWhiteSpace(authoredIdentity))
            {
                return authoredIdentity;
            }

            var hash = StableHash((_unionId ?? string.Empty) + "|" + (MemberId ?? string.Empty));
            switch (hash % 3u)
            {
                case 1u: return GenericEnemy02;
                case 2u: return GenericEnemy03;
                default: return GenericEnemy01;
            }
        }

        private static string StableGateGnawerIdentity086(string identity)
        {
            switch (NormalizeEnemySourceIdentity086(identity))
            {
                // The authored filenames and player-facing Gnawer ranks predate one another.
                // Keep source identity stable while selecting the matching visual silhouette.
                case GenericEnemy01: return GenericEnemy02; // Scout
                case GenericEnemy02: return GenericEnemy01; // Standard / Veteran
                case GenericEnemy03: return GenericEnemy03; // Bulwark / Leader
                default: return string.Empty;
            }
        }

        private static string NormalizeEnemySourceIdentity086(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                return string.Empty;
            }

            var normalized = identity.Trim().ToUpperInvariant();
            var spawnMarker = normalized.IndexOf("_SPAWN070_", System.StringComparison.Ordinal);
            if (spawnMarker > 0)
            {
                normalized = normalized.Substring(0, spawnMarker);
            }

            var packMarker = normalized.IndexOf("_PACK_", System.StringComparison.Ordinal);
            return packMarker > 0 ? normalized.Substring(0, packMarker) : normalized;
        }

        private static bool TryResolveOriginalEnemyResourcePath086(
            string identity,
            out string resourcePath)
        {
            var normalized = NormalizeEnemySourceIdentity086(identity);
            string assetName;
            // The third stable authority variant is each family's named elite visual.
            // Match the exact source ID before the family fallback so a battle-local
            // _SPAWN070_ identity keeps the same leader artwork without changing any
            // simulation, save, or encounter identity.
            if (StringComparer.Ordinal.Equals(
                    normalized, "ENEMY_RUSTBACK_HOUND_03"))
                assetName = "RUSTBACK_HOUND_LEADER_IDLE_089";
            else if (StringComparer.Ordinal.Equals(
                         normalized, "ENEMY_HOLLOW_SALVAGER_03"))
                assetName = "HOLLOW_SALVAGER_LEADER_IDLE_089";
            else if (StringComparer.Ordinal.Equals(
                         normalized, "ENEMY_SHARDWING_SWARM_03"))
                assetName = "SHARDWING_SIGNAL_QUEEN_IDLE_089";
            else if (StringComparer.Ordinal.Equals(
                         normalized, "ENEMY_TOLLROAD_CUTTER_03"))
                assetName = "TOLLROAD_CUTTER_LEADER_IDLE_089";
            else if (StringComparer.Ordinal.Equals(
                         normalized, "ENEMY_RIFT_MOLD_CREEPER_03"))
                assetName = "RIFT_MOLD_CROWN_IDLE_089";
            else if (normalized.StartsWith(
                    "ENEMY_RUSTBACK_HOUND_", StringComparison.Ordinal))
                assetName = "RUSTBACK_HOUND_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_HOLLOW_SALVAGER_", StringComparison.Ordinal))
                assetName = "HOLLOW_SALVAGER_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_SHARDWING_SWARM_", StringComparison.Ordinal))
                assetName = "SHARDWING_SWARM_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_RIFT_MOLD_CREEPER_", StringComparison.Ordinal))
                assetName = "RIFT_MOLD_CREEPER_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_GATEIRON_BRUTE_", StringComparison.Ordinal))
                assetName = "GATEIRON_BRUTE_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_ASH_MEDIC_", StringComparison.Ordinal))
                assetName = "ASH_MEDIC_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_CAPTAIN_RAVEL_", StringComparison.Ordinal))
                assetName = "CAPTAIN_RAVEL_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_GATEHEART_WARDEN_", StringComparison.Ordinal))
                assetName = "GATEHEART_WARDEN_IDLE_086";
            else if (normalized.StartsWith(
                         "ENEMY_TOLLROAD_CUTTER_", StringComparison.Ordinal))
                assetName = "TOLLROAD_CUTTER_IDLE_087";
            else if (normalized.StartsWith(
                         "ENEMY_BRASSJAW_PACKLORD_", StringComparison.Ordinal))
                assetName = "BRASSJAW_PACKLORD_IDLE_087";
            else if (normalized.StartsWith(
                         "ENEMY_PULSE_SCRIBE_", StringComparison.Ordinal))
                assetName = "PULSE_SCRIBE_IDLE_087";
            else if (normalized.StartsWith(
                         "ENEMY_CHAINCALLER_", StringComparison.Ordinal))
                assetName = "CHAINCALLER_IDLE_089";
            else if (normalized.StartsWith(
                         "ENEMY_ECHO_STALKER_", StringComparison.Ordinal))
                assetName = "ECHO_STALKER_IDLE_089";
            else
            {
                resourcePath = string.Empty;
                return false;
            }

            resourcePath = OriginalEnemyRoot086 + "/" + assetName;
            return true;
        }

        private static int ResolveOriginalEnemyPresentationTier086(M2BattleMemberView member)
        {
            if (member == null) return StandardEnemyPresentationTier086;
            var tier = OriginalEnemyPresentationTierForIdentity086(member.PortraitAuthorityId);
            return tier > StandardEnemyPresentationTier086
                ? tier
                : OriginalEnemyPresentationTierForIdentity086(member.MemberId);
        }

        private static int OriginalEnemyPresentationTierForIdentity086(string identity)
        {
            var normalized = NormalizeEnemySourceIdentity086(identity);
            if (normalized.StartsWith(GateheartWardenToken086, StringComparison.Ordinal))
                return MajorEnemyPresentationTier086;
            if (normalized.StartsWith(CaptainRavelToken086, StringComparison.Ordinal))
                return MinibossEnemyPresentationTier086;
            if (normalized.StartsWith(BrassjawPacklordToken087, StringComparison.Ordinal))
                return MinibossEnemyPresentationTier086;
            return StandardEnemyPresentationTier086;
        }

        private static string GenericEnemyLegacyPath(string enemyId, bool action)
        {
            var suffix = StringComparer.Ordinal.Equals(enemyId, GenericEnemy02)
                ? "GATE_GNAWER_SCOUT"
                : StringComparer.Ordinal.Equals(enemyId, GenericEnemy03)
                    ? "GATE_GNAWER_BULWARK"
                    : "GATE_GNAWER_A";
            return AuthoredBattleRoot + "/" + (action ? "ACTION_ENEMY_" : "ENEMY_") + suffix;
        }

        private static string GeneratedEnemyPath075(
            string enemyId,
            bool boss,
            bool action)
        {
            var identity = boss
                ? "HINGE_EATER_COLOSSUS"
                : StringComparer.Ordinal.Equals(enemyId, GenericEnemy02)
                    ? "GATE_GNAWER_SCOUT"
                    : StringComparer.Ordinal.Equals(enemyId, GenericEnemy03)
                        ? "GATE_GNAWER_BULWARK"
                        : "GATE_GNAWER_STANDARD";
            if (boss) return action ? GateEaterActionResourcePath076 : GateEaterIdleResourcePath076;
            return GeneratedEnemyRoot075 + "/" + identity + (action ? "_ACTION" : "_IDLE");
        }

        private static bool UsesEnemyActionPose(string poseId) =>
            StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.ActionPrimary) ||
            StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.RolePrimary) ||
            StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.Anticipation);

        public static bool IsGateEaterBossMember076(string memberId) =>
            !string.IsNullOrWhiteSpace(memberId) &&
            memberId.IndexOf(HingeEaterBossToken075, StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool IsHingeEaterBoss075(string memberId) =>
            IsGateEaterBossMember076(memberId);

        private static bool IsAllowedAuthoredPath(string resourcePath) =>
            !string.IsNullOrWhiteSpace(resourcePath) &&
            resourcePath.IndexOf(RuntimeEnemySilhouette070, StringComparison.OrdinalIgnoreCase) < 0;

        private static uint StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                return hash;
            }
        }

        private static Image CreateImage(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var transform = child.GetComponent<RectTransform>();
            transform.SetParent(parent, false);
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            return child.GetComponent<Image>();
        }

        private static Text CreateText(
            RectTransform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            var transform = child.GetComponent<RectTransform>();
            transform.SetParent(parent, false);
            transform.anchorMin = anchorMin;
            transform.anchorMax = anchorMax;
            transform.offsetMin = Vector2.zero;
            transform.offsetMax = Vector2.zero;
            var text = child.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureArtwork(
            Image image,
            bool enemy,
            bool boss,
            int originalPresentationTier086,
            M2EnemyThreatVisual089 threat089)
        {
            image.preserveAspect = true;
            image.rectTransform.pivot = new Vector2(0.5f, 0f);
            image.raycastTarget = false;
            // Keep a missing authored sprite invisible; a white Image rectangle must
            // never become the live-path fallback for a character.
            image.color = Color.clear;
            var rim = image.gameObject.AddComponent<Outline>();
            rim.effectColor = enemy && threat089 != null
                ? WithAlpha089(
                    threat089.FrameColor,
                    boss || originalPresentationTier086 >= MajorEnemyPresentationTier086
                        ? 0.96f
                        : originalPresentationTier086 == MinibossEnemyPresentationTier086
                            ? 0.88f
                            : 0.72f)
                : boss || originalPresentationTier086 >= MajorEnemyPresentationTier086
                ? new Color(1f, 0.66f, 0.24f, 0.72f)
                : originalPresentationTier086 == MinibossEnemyPresentationTier086
                    ? new Color(1f, 0.48f, 0.22f, 0.58f)
                : enemy
                    ? new Color(0.88f, 0.22f, 0.18f, 0.42f)
                    : new Color(0.22f, 0.78f, 1f, 0.38f);
            rim.effectDistance = boss || originalPresentationTier086 >= MajorEnemyPresentationTier086
                ? new Vector2(3f, -1f)
                : originalPresentationTier086 == MinibossEnemyPresentationTier086
                    ? new Vector2(2f, -0.75f)
                    : new Vector2(1.5f, -0.5f);
            rim.useGraphicAlpha = true;
        }

        private void ApplyArtworkPoseTreatment076(Image artwork, string poseId, float alpha)
        {
            if (artwork == null) return;
            ResetArtworkPoseTreatment076(artwork, poseId);
            var transform = artwork.rectTransform;
            if (StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.Downed))
            {
                // Allies and enemies settle away from the stage centre. The pose is
                // intentionally derived from side only, so repeated captures and
                // generated enemies that reuse an idle cutout remain deterministic.
                var direction076 = _enemy ? -1f : 1f;
                transform.anchoredPosition += new Vector2(
                    direction076 * DownedArtworkSideOffsetPixels076,
                    -DownedArtworkSettlePixels076);
                transform.localScale = new Vector3(
                    DownedArtworkScale076,
                    DownedArtworkScale076,
                    1f);
                transform.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    direction076 * DownedArtworkTiltDegrees076);
                artwork.color = DownedArtworkTint076(alpha);
                return;
            }

            artwork.color = ArtworkColor089(alpha);
        }

        private void ResetArtworkPoseTreatment076(Image artwork, string poseId = null)
        {
            if (artwork == null) return;
            var transform = artwork.rectTransform;
            transform.anchoredPosition = Vector2.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (_enemy || _alliedVisibleHeight091 <= 0f || artwork.sprite == null) return;
            var sprite = artwork.sprite;
            var visible = M1SilhouetteFraming091.VisibleRect091(sprite);
            if (visible.width <= 1f || visible.height <= 1f) return;
            // During a focused action, the authored effect can extend beyond its
            // resting lane. Its separate viewport bound keeps the body readable
            // without scaling down every idle ally to accommodate one wide spell.
            var actionWidth093 = !string.IsNullOrEmpty(poseId) &&
                !StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.Idle) &&
                !StringComparer.Ordinal.Equals(poseId, BattleArtPoseDirector011.Recovery);
            var maximumWidth093 = actionWidth093 ? _alliedActionMaximumWidth093 : _alliedMaximumWidth091;
            var pixelScale = Mathf.Min(_alliedVisibleHeight091 / visible.height,
                maximumWidth093 / sprite.rect.width);
            pixelScale = HeroRemasterAtlas093.CalibrateBattlePixelScale099(sprite,
                pixelScale, _alliedVisibleHeight091, _alliedMaximumWidth091);
            transform.anchorMin = transform.anchorMax = new Vector2(0.5f, 0.07f);
            transform.sizeDelta = sprite.rect.size * pixelScale;
            // Keep actual silhouette feet, not transparent canvas edges, on the
            // existing rig baseline. Pixels retain one uniform X/Y scale.
            transform.anchoredPosition = new Vector2(
                (sprite.rect.center.x - visible.center.x) * pixelScale,
                -(visible.yMin - sprite.rect.yMin) * pixelScale);
        }

        private void SetArtworkPoseAlpha076(Image artwork, string poseId, float alpha)
        {
            if (artwork == null) return;
            artwork.color = StringComparer.Ordinal.Equals(
                    poseId,
                    BattleArtPoseDirector011.Downed)
                ? DownedArtworkTint076(alpha)
                : ArtworkColor089(alpha);
        }

        private Color ArtworkColor089(float alpha)
        {
            // Catalog theme color is not the difficulty grade. Preserve threat
            // name/frame, alpha and separate downed treatment without muting the
            // exact remaster crystal colors through a second unrelated RGB tint.
            var tint = _enemyRemasterTheme098 != null ? Color.white : _enemy && _enemyThreat089 != null
                ? _enemyThreat089.ArtworkTint
                : Color.white;
            tint.a = Mathf.Clamp01(alpha);
            return tint;
        }

        private static Color WithAlpha089(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Color DownedArtworkTint076(float alpha) =>
            new Color(0.46f, 0.50f, 0.58f, Mathf.Clamp01(alpha));

        private void InstallEnemyCutoutMaterial075()
        {
            var shader = Resources.Load<Shader>(EnemyCutoutShaderResource075);
            if (shader == null) shader = Shader.Find(EnemyCutoutShaderName075);
            if (shader == null) return;

            _enemyCutoutMaterial075 = new Material(shader)
            {
                name = "Enemy Cutout Clean 075 · " + PlayerFacingDisplayName076(
                    _member,
                    BossEnemy075 ? GateEaterDisplayName076 : "Enemy")
            };
            _currentArtwork.material = _enemyCutoutMaterial075;
            _incomingArtwork.material = _enemyCutoutMaterial075;
        }

        private static string SafeDisplayName(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value)) return fallback;
            var result = value.Trim();
            if (Guid.TryParse(result, out _)) return fallback;
            if (result.IndexOf('_') < 0) return result;
            var prefixes = new[] { "SIGREC_", "SIGI_", "PROC_", "ENEMY_", "MEMBER_", "RECRUIT_" };
            for (var index = 0; index < prefixes.Length; index++)
            {
                if (!result.StartsWith(prefixes[index], StringComparison.OrdinalIgnoreCase)) continue;
                result = result.Substring(prefixes[index].Length);
                break;
            }
            result = result.Replace('_', ' ').Trim();
            return string.IsNullOrWhiteSpace(result)
                ? fallback
                : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(result.ToLowerInvariant());
        }

        private static string PlayerFacingDisplayName076(
            M2BattleMemberView member,
            string fallback)
        {
            if (member != null && IsGateEaterBossMember076(member.MemberId))
                return GateEaterDisplayName076;
            return NormalizeBossIdentity076(SafeDisplayName(member?.DisplayName, fallback));
        }

        public static string NormalizeBossIdentity076(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;
            var result = value;
            var aliases = new[]
            {
                "HINGE-EATER COLOSSUS",
                "HINGE-EATER BOSS",
                "HINGE EATER COLOSSUS",
                "HINGE_EATER_COLOSSUS",
                "HINGE-EATER",
                "HINGE EATER"
            };
            for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
            {
                var alias = aliases[aliasIndex];
                var match = result.IndexOf(alias, StringComparison.OrdinalIgnoreCase);
                while (match >= 0)
                {
                    result = result.Substring(0, match) + GateEaterDisplayName076 +
                             result.Substring(match + alias.Length);
                    match = result.IndexOf(alias, match + GateEaterDisplayName076.Length,
                        StringComparison.OrdinalIgnoreCase);
                }
            }
            return result;
        }

        private static bool ShouldSkip(Func<bool> skip) => skip != null && skip();

        private static float ResolveSpeed(Func<float> speed) =>
            Mathf.Clamp(speed == null ? 1f : speed(), 0.1f,
                M2BattleExperienceController072.MaximumPlaybackSpeed108);
    }
}
