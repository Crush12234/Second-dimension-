using System;
using System.Collections.Generic;

namespace SecondDimension.Content
{
    public sealed class ContentValidationReport
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public int CanonicalIdCount { get; internal set; }
        public int ShadowConflictCount { get; internal set; }
        public int P0AssetReferenceCount { get; internal set; }
        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        internal void Error(string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) _errors.Add(value);
        }

        internal void Warning(string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) _warnings.Add(value);
        }

        public override string ToString()
        {
            return IsValid
                ? $"PASS — {CanonicalIdCount} stable IDs; {ShadowConflictCount} shadow conflicts resolved; {P0AssetReferenceCount} P0 asset references."
                : $"FAIL — {_errors.Count} content error(s).";
        }
    }
}

