using System;
using System.Collections.Generic;

namespace SecondDimension.Determinism
{
    public static class DeterministicOrdering
    {
        public static List<string> OrdinalStableIds(IEnumerable<string> values)
        {
            var result = new List<string>(values ?? Array.Empty<string>());
            result.Sort(StringComparer.Ordinal);
            return result;
        }
    }
}

