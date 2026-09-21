#if UNITY_EDITOR
namespace SecondDimension.Editor.Release025
{
    /// <summary>
    /// Historical command-line compatibility facade. Release 030 remains the active pipeline.
    /// </summary>
    public static class FinalImplementationSetup025
    {
        public static void Prepare()
        {
            FinalImplementationHardeningSetup025.Prepare();
        }

        public static void PrepareFromCommandLine()
        {
            FinalImplementationHardeningSetup025.PrepareFromCommandLine();
        }
    }
}
#endif
