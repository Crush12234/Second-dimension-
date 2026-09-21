using UnityEditor;

namespace SecondDimension.ProductionSlice014.Editor
{
    /// <summary>
    /// Update 015 safety placeholder. The 014 automatic scene generator was
    /// intentionally retired because editor reload callbacks must not author or
    /// replace production scenes. Existing 014 source assets remain available
    /// for inspection, but nothing runs automatically.
    /// </summary>
    public static class MarenProduction3DSetup014
    {
        [MenuItem("Second Dimension/Production 3D/014 Prototype Retired — Update 015", false, 3000)]
        public static void ExplainRetirement()
        {
            EditorUtility.DisplayDialog(
                "Update 014 prototype retired",
                "The automatic 014 proof builder is disabled. It was a low-poly " +
                "pipeline test and is not the production graphics direction. " +
                "Update 015 uses pre-authored scenes, verified character prefabs, " +
                "Timeline, and Cinemachine instead.",
                "OK");
        }
    }
}
