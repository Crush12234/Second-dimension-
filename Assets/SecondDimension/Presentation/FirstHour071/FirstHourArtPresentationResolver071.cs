using System;

namespace SecondDimension.Presentation.FirstHour071
{
    /// <summary>
    /// Presentation-only handoff for a future cinematic director. It resolves stable
    /// Art IDs to logical procedural recipes and deliberately exposes no combat data.
    /// </summary>
    public sealed class ResolvedFirstHourArtPresentation071
    {
        internal ResolvedFirstHourArtPresentation071(FirstHourArtRecipe071 recipe)
        {
            StableArtId = recipe.artId;
            TreeId = recipe.treeId;
            NodeIndex = recipe.nodeIndex;
            DisplayName = recipe.displayName;
            ClipRecipeId = recipe.clipId;
            CameraSignature = recipe.cameraSignature;
            VfxSignature = recipe.vfxSignature;
            SfxSignature = recipe.sfxSignature;
            MotionSignature = recipe.motionSignature;
            DurationMilliseconds = recipe.durationMilliseconds;
            ImpactMilliseconds = recipe.impactMilliseconds;
            ReactionTrigger = recipe.reactionTrigger;
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
        public FirstHourReactionTrigger071 ReactionTrigger { get; }
        public bool UsesProceduralMotionRecipe => true;
        public bool ClaimsAuthoredAnimationClipAsset => false;
        public bool PresentationOnly => true;
        public bool ChangesCombatResolution => false;
    }

    public static class FirstHourArtPresentationResolver071
    {
        private static FirstHourArtRegistry071 _registry;
        private static bool _loadFailed;

        public static bool TryResolve(
            string stableArtId,
            out ResolvedFirstHourArtPresentation071 presentation)
        {
            presentation = null;
            if (string.IsNullOrWhiteSpace(stableArtId) || _loadFailed) return false;

            try
            {
                if (_registry == null) _registry = FirstHourArtRegistry071.Load();
            }
            catch (Exception)
            {
                // This is an optional presentation layer. A missing or invalid recipe
                // manifest must never prevent the authoritative battle from resolving.
                _loadFailed = true;
                return false;
            }

            if (!_registry.TryGet(stableArtId, out var recipe))
                return false;
            presentation = new ResolvedFirstHourArtPresentation071(recipe);
            return true;
        }
    }
}
