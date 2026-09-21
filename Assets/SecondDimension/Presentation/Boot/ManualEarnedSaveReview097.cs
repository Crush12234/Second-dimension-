using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using SecondDimension.Save;

namespace SecondDimension.Presentation.Boot
{
    /// <summary>Explicit QA path selection only; never edits campaign state or runs gameplay commands.</summary>
    public static class ManualEarnedSaveReview097
    {
        public const string SourceFlag097 = "--sd-earned-review-source=";
        public const string OutputFlag097 = "--sd-earned-review-output=";
        public const string CopyFileName097 = "EarnedReviewSave097.json";

        public static bool IsRequested097(IReadOnlyList<string> arguments) =>
            arguments != null && arguments.Any(value => value != null &&
                value.StartsWith("--sd-earned-review", StringComparison.OrdinalIgnoreCase));

        public static string Prepare097(IReadOnlyList<string> arguments,
            string persistentDataPath, string buildDataPath)
        {
            if (!IsRequested097(arguments))
                throw new InvalidOperationException("An explicit earned-save review request is required.");
            string source = null, output = null;
            foreach (var argument in arguments)
            {
                if (argument == null) continue;
                if (argument.StartsWith(SourceFlag097, StringComparison.Ordinal) && source == null)
                    source = argument.Substring(SourceFlag097.Length);
                else if (argument.StartsWith(OutputFlag097, StringComparison.Ordinal) && output == null)
                    output = argument.Substring(OutputFlag097.Length);
                else if (argument.StartsWith("--sd-", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Earned-save review cannot combine duplicate, unknown, or automated QA flags.");
            }

            source = LocalAbsolutePath097(source);
            output = LocalAbsolutePath097(output);
            var personal = LocalAbsolutePath097(persistentDataPath);
            var buildData = LocalAbsolutePath097(buildDataPath);
            var buildRoot = Path.GetDirectoryName(buildData);
            var sourceDirectory = Path.GetDirectoryName(source);
            if (!File.Exists(source) || Directory.Exists(source))
                throw new InvalidOperationException("The exact earned source save must already exist as a file.");
            if (!string.Equals(Path.GetExtension(source), ".json", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The earned source must be a JSON save envelope.");
            if (Same097(output, Path.GetPathRoot(output)) ||
                Overlaps097(output, sourceDirectory) || Overlaps097(output, personal) ||
                Overlaps097(output, buildRoot) || IsWithin097(source, personal) || IsWithin097(source, buildRoot))
                throw new InvalidOperationException("Review paths must be outside the source directory, personal save directory, and player/project installation.");
            if (File.Exists(output) || Directory.Exists(output))
                throw new InvalidOperationException("The review output must be a fresh directory that does not yet exist; existing review saves are never overwritten.");
            if (!Directory.Exists(Path.GetDirectoryName(output)))
                throw new InvalidOperationException("The review output's parent directory must already exist.");
            RejectReparseAncestors097(source);
            RejectReparseAncestors097(output);
            RejectReparseAncestors097(personal);
            RejectReparseAncestors097(buildData);

            // Hold a read-only, non-writer-shared source handle throughout copying
            // and validation. No source backup fallback, serialization, or rewrite.
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var sourceHash = Hash097(input);
                input.Position = 0;
                Directory.CreateDirectory(output);
                RejectReparseAncestors097(output);
                if (Directory.EnumerateFileSystemEntries(output).Any())
                    throw new InvalidOperationException("The review output changed while preparing it.");
                var savePath = Path.Combine(output, CopyFileName097);
                using (var copy = new FileStream(savePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    input.CopyTo(copy);
                    copy.Flush(true);
                }
                RejectReparseAncestors097(savePath);
                string copyHash;
                using (var copy = new FileStream(savePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    copyHash = Hash097(copy);
                input.Position = 0;
                if (sourceHash != copyHash || sourceHash != Hash097(input))
                    throw new InvalidOperationException("The earned save copy did not match its unchanged source SHA-256.");

                // This fresh directory contains no .bak file: ReadWithRecovery
                // must validate this exact primary, never a different save.
                var read = new AtomicSaveStore().ReadWithRecovery(savePath);
                if (!read.IsSuccess)
                    throw new InvalidOperationException("The copied earned save failed existing save validation: " +
                        string.Join("; ", read.Errors));
                var receipt = JsonConvert.SerializeObject(new
                {
                    Mode = "MANUAL_EARNED_SAVE_REVIEW_097",
                    SourcePath = source,
                    ReviewSavePath = savePath,
                    SourceSha256 = sourceHash,
                    CopiedSha256 = copyHash,
                    ValidatedCanonicalStateHash = read.Value.CanonicalStateHash,
                    AutomaticGameplay = false,
                    CampaignStateEditedByLauncher = false
                }, Formatting.Indented);
                using (var stream = new FileStream(Path.Combine(output, "manual_review097_receipt.json"),
                    FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream)) writer.Write(receipt);
                return savePath;
            }
        }

        // Windows player-only local paths. Reject device/UNC/ADS, relative paths,
        // traversal, ambiguous trailing dots/spaces, and wildcard names before IO.
        public static string LocalAbsolutePath097(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.Length < 3 || !char.IsLetter(path[0]) ||
                path[1] != ':' || (path[2] != '\\' && path[2] != '/'))
                throw new InvalidOperationException("Review paths must be explicit absolute local Windows paths.");
            var normalized = path.Replace('/', '\\');
            foreach (var segment in normalized.Substring(3).Split('\\'))
            {
                if (segment.Length == 0) continue;
                if (segment == "." || segment == ".." || segment.EndsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith(" ", StringComparison.Ordinal) ||
                    segment.Any(character => character < 32 || "<>:\"|?*~".IndexOf(character) >= 0))
                    throw new InvalidOperationException("Review path contains an ambiguous or unsafe component.");
                var stem = segment.Split('.')[0].ToUpperInvariant();
                if (stem == "CON" || stem == "PRN" || stem == "AUX" || stem == "NUL" ||
                    (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) ||
                        stem.StartsWith("LPT", StringComparison.Ordinal)) && stem[3] >= '0' && stem[3] <= '9'))
                    throw new InvalidOperationException("Review paths cannot contain Windows device names.");
            }
            return Path.GetFullPath(normalized).TrimEnd('\\', '/') +
                (normalized.TrimEnd('\\', '/').Length == 2 ? "\\" : "");
        }

        private static void RejectReparseAncestors097(string path)
        {
            for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            {
                if (!File.Exists(current) && !Directory.Exists(current)) continue;
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new InvalidOperationException("Review paths cannot contain symbolic links, junctions, or reparse points.");
            }
        }

        private static bool Same097(string a, string b) =>
            string.Equals(a?.TrimEnd('\\', '/'), b?.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        private static bool IsWithin097(string path, string directory) => Same097(path, directory) ||
            path.StartsWith(directory.TrimEnd('\\', '/') + "\\", StringComparison.OrdinalIgnoreCase);
        private static bool Overlaps097(string a, string b) => IsWithin097(a, b) || IsWithin097(b, a);
        private static string Hash097(Stream stream)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
