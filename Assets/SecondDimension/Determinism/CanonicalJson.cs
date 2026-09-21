using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Determinism
{
    public static class CanonicalJson
    {
        public static string Serialize(object value)
        {
            var token = value is JToken existing
                ? existing.DeepClone()
                : JToken.FromObject(value, JsonSerializer.Create(DefaultSettings()));

            var builder = new StringBuilder();
            WriteToken(builder, token);
            return builder.ToString();
        }

        public static string Sha256Hex(object value, bool uppercase = false)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Serialize(value)));
                var hex = BitConverter.ToString(hash).Replace("-", string.Empty);
                return uppercase ? hex : hex.ToLowerInvariant();
            }
        }

        public static JsonSerializerSettings DefaultSettings()
        {
            return new JsonSerializerSettings
            {
                Culture = CultureInfo.InvariantCulture,
                DateParseHandling = DateParseHandling.None,
                FloatFormatHandling = FloatFormatHandling.String,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include
            };
        }

        private static void WriteToken(StringBuilder builder, JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    builder.Append('{');
                    var firstProperty = true;
                    var properties = new System.Collections.Generic.List<JProperty>(((JObject)token).Properties());
                    properties.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
                    foreach (var property in properties)
                    {
                        if (!firstProperty) builder.Append(',');
                        firstProperty = false;
                        builder.Append(JsonConvert.ToString(property.Name));
                        builder.Append(':');
                        WriteToken(builder, property.Value);
                    }
                    builder.Append('}');
                    break;

                case JTokenType.Array:
                    builder.Append('[');
                    var firstItem = true;
                    foreach (var item in (JArray)token)
                    {
                        if (!firstItem) builder.Append(',');
                        firstItem = false;
                        WriteToken(builder, item);
                    }
                    builder.Append(']');
                    break;

                case JTokenType.Integer:
                    builder.Append(Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture));
                    break;

                case JTokenType.Boolean:
                    builder.Append(token.Value<bool>() ? "true" : "false");
                    break;

                case JTokenType.Null:
                case JTokenType.Undefined:
                    builder.Append("null");
                    break;

                case JTokenType.String:
                    builder.Append(JsonConvert.ToString(token.Value<string>()));
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Canonical authoritative JSON does not allow token type {token.Type}. Use integer/fixed-point values.");
            }
        }
    }
}
