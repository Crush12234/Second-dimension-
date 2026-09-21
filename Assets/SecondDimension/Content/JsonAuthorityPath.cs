using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Content
{
    internal static class JsonAuthorityPath
    {
        public static JToken Resolve(JToken root, string path)
        {
            if (root == null || string.IsNullOrWhiteSpace(path) || path[0] != '$')
            {
                return null;
            }

            var current = root;
            var index = 1;
            while (index < path.Length)
            {
                if (path[index] == '.')
                {
                    index++;
                    var start = index;
                    while (index < path.Length && path[index] != '.' && path[index] != '[') index++;
                    var key = path.Substring(start, index - start);
                    current = current?[key];
                }
                else if (path[index] == '[')
                {
                    var close = path.IndexOf(']', index + 1);
                    if (close < 0) return null;
                    if (!int.TryParse(path.Substring(index + 1, close - index - 1), NumberStyles.None,
                            CultureInfo.InvariantCulture, out var arrayIndex))
                    {
                        return null;
                    }

                    current = current is JArray array && arrayIndex >= 0 && arrayIndex < array.Count
                        ? array[arrayIndex]
                        : null;
                    index = close + 1;
                }
                else
                {
                    return null;
                }

                if (current == null) return null;
            }

            return current;
        }
    }
}

