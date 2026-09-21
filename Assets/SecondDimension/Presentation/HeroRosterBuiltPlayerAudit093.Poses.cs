using System;
using System.Collections;
using System.Linq;
using SecondDimension.Gameplay.Recruitment;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    [Serializable]
    public sealed class HeroRosterPoseProof093
    {
        public string pose, capture, stableId, expectedResource, actualResource, texture, campaignHash;
        public string evidenceBoundary = "QA pose-placement only; no combat command, damage, reward or outcome simulated. Visible silhouette includes weapon/effect; body proportions require screenshot review.";
        public bool exactCellBound, neighborCellExcluded, cpuPixelsDiscarded, campaignUnchanged;
        public float[] spriteRect;
        public float height, width, feet, expectedFoot, headClearance;
    }

    public sealed partial class HeroRosterBuiltPlayerAudit093
    {
        const string ReviewedPoseFlag093 = "--sd-roster-audit-reviewed-remaster-poses";
        const string KeepOpenFlag093 = "--sd-roster-audit-keep-open";
        bool _reviewedRemasterPoses093;
        bool _keptOpen093;
        string _lastReviewedIdentity093;
        M1RuntimeCoordinator _lastReviewedCoordinator093;
        // Do not hash the presentation DTO. Its existing CanonicalStateHash is
        // freshly computed from _campaign by the shipping coordinator itself.
        public static string CoordinatorCampaignHash093(M1RuntimeCoordinator coordinator)
        {
            var hash = coordinator?.State?.CanonicalStateHash;
            if (string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("No authoritative campaign is bound to this coordinator.");
            return hash;
        }

        public static bool CanKeepReviewedBattleOpen093(string[] arguments, bool runSucceeded,
            string lastIdentity, bool liveUnresolvedBattle) => IsRequested093(arguments) &&
            arguments.Contains(KeepOpenFlag093) && runSucceeded && liveUnresolvedBattle &&
            IsReviewedPoseIdentity099(lastIdentity);

        void KeepReviewedBattleOpen093(bool runSucceeded)
        {
            var controller = FindFirstObjectByType<M2BattleExperienceController072>();
            var battle = _lastReviewedCoordinator093?.State?.Battle;
            var member = battle?.PlayerUnions.SelectMany(union => union.Members)
                .SingleOrDefault(value => value.PortraitAuthorityId == _lastReviewedIdentity093);
            var rig = member == null ? null : controller?.OwnedDiorama078?.ResolveActor(member.MemberId, string.Empty);
            var live = controller != null && controller.IsActive && !controller.IsResolving &&
                member != null && !member.Downed && member.CurrentHp > 0 &&
                M2BattleAutoOrders091.HasLivingOpposition(battle) && rig?.CurrentArtwork076?.sprite != null;
            if (!CanKeepReviewedBattleOpen093(Environment.GetCommandLineArgs(), runSucceeded, _lastReviewedIdentity093, live))
                throw new InvalidOperationException("Requested keep-open requires successful audit, the exact last reviewed hero and a live unresolved shipping battle.");
            InspectExactRemasterCell093(rig.CurrentArtwork076, _lastReviewedIdentity093, "MANUAL PLAYTEST START", false,
                !Application.isEditor);
            var canvas = rig.Root.GetComponentInParent<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Reviewed battle has no active shipping Canvas.");
            var label = CreatePoseLabel093(canvas.transform);
            label.GetComponentInChildren<Text>().text = "ISOLATED QA SAVE - LIVE COMBAT PLAYTEST | PERSONAL SAVE UNTOUCHED";
            _keptOpen093 = true;
            Debug.Log("ROSTER_AUDIT_093 KEEP_OPEN " + _lastReviewedIdentity093 +
                "; current fixture only; real Forecast commands remain available.");
        }

        public static bool IsReviewedPoseIdentity099(string stableId) =>
            HeroRemasterAtlas093.ContainsIdentity093(stableId) ||
            HeroRecoveredSource100099.ContainsIdentity099(stableId);

        public static bool IncludesHeroForCapture093(int rosterId, string stableId, int first, int last,
            bool reviewedRemasterPoses) => rosterId >= first && rosterId <= last &&
            (!reviewedRemasterPoses || IsReviewedPoseIdentity099(stableId));

        public static HeroRosterPoseProof093 InspectExactRemasterCell093(Image image, string stableId,
            string pose, bool action, bool requireCpuDiscard)
        {
            Sprite expected;
            string key;
            Rect selected, other;
            bool separateSourceTextures;
            if (HeroRemasterAtlas093.ContainsIdentity093(stableId))
            {
                if (!HeroRemasterAtlas093.TryResolve093(stableId, action, out expected, out key) ||
                    !HeroRemasterAtlas093.TryGetFrameRects093(stableId, expected.texture.width,
                        expected.texture.height, out var idleCell, out var actionCell))
                    throw new InvalidOperationException("Missing exact reviewed atlas frame metadata.");
                selected = action ? actionCell : idleCell;
                other = action ? idleCell : actionCell;
                separateSourceTextures = false;
            }
            else
            {
                // Recovered poses are distinct immutable PNG textures, NOT two
                // halves of one atlas. Compare the selected inspected source
                // rectangle; overlapping numeric coordinates across textures
                // do not imply neighbor leakage.
                if (!HeroRecoveredSource100099.TryResolve099(stableId, action, out expected, out key) ||
                    !HeroRecoveredSource100099.TryResolve099(stableId, !action, out var opposite, out _) ||
                    expected.texture == opposite.texture ||
                    !HeroRecoveredSource100099.TryGetSourceRects099(stableId, expected.texture.width,
                        expected.texture.height, out var idleCell, out var actionCell))
                    throw new InvalidOperationException("Missing exact reviewed recovered pair metadata.");
                selected = action ? actionCell : idleCell;
                other = Rect.zero;
                separateSourceTextures = true;
            }
            if (image == null || image.sprite == null || image.sprite != expected ||
                !image.preserveAspect || image.sprite.rect != expected.rect)
                throw new InvalidOperationException("The active Image does not show the requested exact-identity pose cell.");
            var texture = expected.texture;
            var rect = expected.rect;
            var isolated = rect.xMin >= selected.xMin && rect.xMax <= selected.xMax &&
                rect.yMin >= selected.yMin && rect.yMax <= selected.yMax &&
                (separateSourceTextures || !rect.Overlaps(other));
            if (!isolated || (requireCpuDiscard && texture.isReadable))
                throw new InvalidOperationException("Pose source-cell leakage or Windows CPU-discard parity failed.");
            return new HeroRosterPoseProof093
            {
                stableId = stableId, pose = pose, expectedResource = key, texture = texture.name,
                exactCellBound = true, neighborCellExcluded = true, cpuPixelsDiscarded = !texture.isReadable,
                spriteRect = new[] { rect.x, rect.y, rect.width, rect.height }
            };
        }

        // Match the deliberate exact011 rig calibration: the complete lunge
        // is shorter than its upright spear, not a smaller body. Only the actual
        // cached authored action receives the ratio; IDs/texture names alone do
        // not relax size checks for arbitrary or substituted sprites.
        public static float MinimumReviewedPoseHeightFraction099(string stableId, bool action, Sprite sprite)
        {
            const float standard = 0.28f;
            if (!HeroRemasterAtlas093.UsesIdlePixelScale100(stableId) || !action || sprite == null ||
                !HeroRemasterAtlas093.TryResolve093(stableId, true, out var exactAction, out _) ||
                !ReferenceEquals(sprite, exactAction) ||
                !HeroRemasterAtlas093.TryResolve093(stableId, false, out var idle, out _)) return standard;
            var actionHeight = M1SilhouetteFraming091.VisibleRect091(exactAction).height;
            var idleHeight = M1SilhouetteFraming091.VisibleRect091(idle).height;
            if (actionHeight <= 1 || idleHeight <= 1)
                throw new InvalidOperationException("Missing exact reviewed silhouette calibration bounds.");
            return standard * Mathf.Min(1f, actionHeight / idleHeight);
        }

        IEnumerator RunReviewedRemasterPoses093(HeroMaster300Hero087 hero, HeroRosterAuditRow093 row,
            M1RuntimeCoordinator coordinator)
        {
            if (!IsReviewedPoseIdentity099(hero.StableId))
                throw new InvalidOperationException("Reviewed-pose audit must never display an unreviewed fallback hero.");
            row.runtimeUiVerified = false;
            var stateHash = CoordinatorCampaignHash093(coordinator);
            var controller = FindFirstObjectByType<M2BattleExperienceController072>();
            if (controller == null || !controller.IsActive || controller.OwnedDiorama078 == null)
                throw new InvalidOperationException("The shipping battle must be active before optional pose capture.");
            var rig = controller.OwnedDiorama078.ResolveActor(row.recruitId, string.Empty);
            var canvas = rig?.Root.GetComponentInParent<Canvas>();
            if (rig == null || canvas == null) throw new InvalidOperationException("Missing live actor or shipping Canvas.");
            var label = CreatePoseLabel093(canvas.transform);
            try
            {
                for (var index = 0; index < 3 && string.IsNullOrEmpty(row.failure); index++)
                {
                    var action = index == 1;
                    var pose = action ? BattleArtPoseDirector011.ActionPrimary : BattleArtPoseDirector011.Idle;
                    var description = index == 0 ? "IDLE BEFORE" : action ? "ACTION POSE" : "IDLE RESTORED";
                    label.GetComponentInChildren<Text>().text = "ART PLACEMENT CHECK - " + description +
                        " | QA ONLY: NO COMBAT ACTION/OUTCOME";
                    Step093(row, () =>
                    {
                        if (!rig.SetPoseImmediate(pose)) throw new InvalidOperationException("Requested QA pose could not bind.");
                    });
                    yield return null;
                    yield return new WaitForEndOfFrame();
                    Step093(row, () =>
                    {
                        // Capture before assertions so any visible defect remains evidence.
                        var capture = Capture093(hero, "qa_pose_" + index + (action ? "_action" : "_idle"));
                        row.poseProofs.Add(new HeroRosterPoseProof093 { stableId = hero.StableId,
                            pose = description, capture = capture });
                        var proof = InspectExactRemasterCell093(rig.CurrentArtwork076, hero.StableId,
                            description, action, !Application.isEditor);
                        proof.capture = capture; proof.actualResource = rig.CurrentResourcePath;
                        row.poseProofs[row.poseProofs.Count - 1] = proof;
                        var bounds = CompositionScreenBounds093(rig.CurrentArtwork076);
                        proof.height = bounds.height; proof.width = bounds.width; proof.feet = bounds.yMin;
                        proof.expectedFoot = RectTransformUtility.WorldToScreenPoint(null,
                            rig.Root.TransformPoint(new Vector3(0f, rig.Root.rect.height * 0.07f))).y;
                        var stage = GameObject.Find("Battle Diorama Experience 072")?.GetComponent<RectTransform>();
                        if (stage == null) throw new InvalidOperationException("Missing active battle stage.");
                        var header = RectTransformUtility.WorldToScreenPoint(null, stage.TransformPoint(new Vector3(0,
                            stage.rect.yMin + stage.rect.height * M2BattleDioramaView072.FocusedUnionHpRibbonMinY076))).y;
                        proof.headClearance = header - bounds.yMax;
                        proof.campaignHash = CoordinatorCampaignHash093(coordinator);
                        proof.campaignUnchanged = proof.campaignHash == stateHash;
                        if (!proof.campaignUnchanged || proof.actualResource != proof.expectedResource ||
                            proof.height <= Screen.height * MinimumReviewedPoseHeightFraction099(
                                hero.StableId, action, rig.CurrentArtwork076.sprite) ||
                            Math.Abs(proof.feet - proof.expectedFoot) > 2f ||
                            proof.headClearance < 8f || bounds.xMin < 0 || bounds.xMax > Screen.width ||
                            bounds.yMin < 0 || bounds.yMax > Screen.height)
                            throw new InvalidOperationException("Reviewed QA pose failed exact cell/state/scale/feet/header/viewport proof: " + description);
                    });
                }
                row.runtimeUiVerified = string.IsNullOrEmpty(row.failure) && row.poseProofs.Count == 3 &&
                    row.poseProofs.All(value => value.exactCellBound && value.neighborCellExcluded && value.campaignUnchanged);
            }
            finally
            {
                rig.SetPoseImmediate(BattleArtPoseDirector011.Idle);
                Destroy(label);
            }
        }

        static GameObject CreatePoseLabel093(Transform canvas)
        {
            var label = new GameObject("Explicit QA pose label 093", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            label.transform.SetParent(canvas, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.19f, 0.935f); rect.anchorMax = new Vector2(0.81f, 0.99f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            label.GetComponent<Image>().color = new Color(0.03f, 0.06f, 0.11f, 0.97f);
            label.GetComponent<Image>().raycastTarget = false;
            var body = new GameObject("QA pose boundary", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            body.transform.SetParent(label.transform, false);
            var bodyRect = body.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero; bodyRect.anchorMax = Vector2.one;
            bodyRect.offsetMin = new Vector2(12f, 4f); bodyRect.offsetMax = new Vector2(-12f, -4f);
            var text = body.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28; text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20; text.resizeTextMaxSize = 28;
            text.color = Color.white; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            label.transform.SetAsLastSibling();
            return label;
        }
    }
}
