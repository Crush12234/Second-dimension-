using System;
using System.IO;

namespace SecondDimension.Presentation.Boot
{
    /// <summary>Portable alpha uses the existing save store in its own game folder.</summary>
    public static class AlphaProfile132
    {
        public const string MarkerFile132 = "SECOND_DIMENSION_PORTABLE_ALPHA.txt";
        public const string MarkerValue132 = "SECOND_DIMENSION_PORTABLE_ALPHA132";
        public static string SessionDirectory132 { get; private set; }

        public static void SetSessionSave132(string savePath)
        {
            SessionDirectory132 = Path.GetDirectoryName(Path.GetFullPath(savePath));
        }

        public static string ResolveDirectory132(string personalDirectory, string buildDataPath)
        {
            if (string.IsNullOrWhiteSpace(buildDataPath)) return personalDirectory;
            var root = Path.GetDirectoryName(Path.GetFullPath(buildDataPath));
            var marker = Path.Combine(root, MarkerFile132);
            if (!File.Exists(marker)) return personalDirectory;
            if (!string.Equals(File.ReadAllText(marker).Trim(), MarkerValue132, StringComparison.Ordinal))
                throw new IOException("The portable alpha marker is invalid. Extract the complete alpha package again.");
            var directory = Path.Combine(root, "SaveData");
            RejectRedirect132(root);
            RejectRedirect132(directory);
            try
            {
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, ".write-check-" + Guid.NewGuid().ToString("N"));
                using (var file = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    1, FileOptions.DeleteOnClose)) { file.WriteByte(1); file.Flush(); }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new IOException("Cannot write to this alpha's SaveData folder. Copy the complete game folder to a writable drive and open it there. Existing saves were not moved.", exception);
            }
            return directory;
        }

        static void RejectRedirect132(string path)
        {
            if ((File.Exists(path) || Directory.Exists(path)) &&
                (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("The portable alpha game and SaveData folders must be ordinary folders.");
        }
    }
}
