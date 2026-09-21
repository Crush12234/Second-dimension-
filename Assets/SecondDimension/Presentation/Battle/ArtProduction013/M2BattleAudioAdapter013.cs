using SecondDimension.Presentation;

namespace SecondDimension.Presentation.Battle.ArtProduction013
{
    /// <summary>Maps Art 013 families into the existing deterministic placeholder/audio-replacement cue bank.</summary>
    public static class M2BattleAudioAdapter013
    {
        public static void PlayAnticipation(M2BattleAudioDirector director, ResolvedArtPresentationPlan013 plan)
        {
            if (director == null || plan == null) return;
            director.PlayCue("ART013_" + plan.audioCueFamilyId + "_ANTICIPATION");
        }

        public static void PlayImpact(M2BattleAudioDirector director, ResolvedArtPresentationPlan013 plan)
        {
            if (director == null || plan == null) return;
            director.PlayCue("ART013_" + plan.audioCueFamilyId + "_IMPACT");
        }
    }
}
