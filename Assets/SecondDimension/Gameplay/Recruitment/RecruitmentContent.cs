using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Immutable parsed authority used by all opening recruitment services.
    /// Loading is explicit so deterministic generation itself performs no I/O.
    /// </summary>
    public sealed class RecruitmentContent
    {
        private RecruitmentContent(JObject procedural, JObject signatures, JObject office)
        {
            Procedural = procedural ?? throw new ArgumentNullException(nameof(procedural));
            Signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
            Office = office ?? throw new ArgumentNullException(nameof(office));
            ProceduralContentVersion = RequiredString(procedural, "contentVersion");
            SignatureContentVersion = RequiredString(signatures, "contentVersion");
            OfficeContentVersion = RequiredString(office, "contentVersion");
        }

        public string ProceduralContentVersion { get; }
        public string SignatureContentVersion { get; }
        public string OfficeContentVersion { get; }

        internal JObject Procedural { get; }
        internal JObject Signatures { get; }
        internal JObject Office { get; }

        public static RecruitmentContent LoadFromDirectory(string contentRoot)
        {
            if (string.IsNullOrWhiteSpace(contentRoot))
                throw new ArgumentException("A content directory is required.", nameof(contentRoot));

            return FromJson(
                File.ReadAllText(Path.Combine(contentRoot, "OPENING_PROCEDURAL_TABLES.json")),
                File.ReadAllText(Path.Combine(contentRoot, "OPENING_SIGNATURE_RECRUITS.json")),
                File.ReadAllText(Path.Combine(contentRoot, "RECRUITMENT_OFFICE_PROGRESSION.json")));
        }

        public static RecruitmentContent FromJson(
            string proceduralTablesJson,
            string signatureRecruitsJson,
            string recruitmentOfficeJson)
        {
            var settings = new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                LineInfoHandling = LineInfoHandling.Ignore
            };
            return new RecruitmentContent(
                JObject.Parse(proceduralTablesJson, settings),
                JObject.Parse(signatureRecruitsJson, settings),
                JObject.Parse(recruitmentOfficeJson, settings));
        }

        internal static string RequiredString(JToken token, string property)
        {
            var value = token == null ? null : token[property]?.Value<string>();
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException("Recruitment content is missing '" + property + "'.");
            return value;
        }
    }

    internal static class RecruitmentDeterminism
    {
        internal static readonly string[] Disciplines =
            { "MARTIAL", "MYSTIC", "RESTORATION", "SUPPORT", "GUARD", "TACTICAL" };

        internal static readonly string[] Stats =
            { "HP", "STR", "DEF", "AGI", "MAGIC", "WILL" };

        internal static int Clamp(int value, int low, int high) => Math.Max(low, Math.Min(high, value));

        internal static string SeedString(params object[] parts) => SemanticSeed.Derive(parts).ToString();

        internal static string StableHash(object value, int length = 20)
        {
            string payload;
            if (value is string raw)
                payload = raw;
            else
                payload = CanonicalJson.Serialize(value);

            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var hex = BitConverter.ToString(digest).Replace("-", string.Empty);
                return hex.Substring(0, length).ToUpperInvariant();
            }
        }

        internal static Pcg32 Rng(params object[] parts) => Pcg32.FromParts(parts);

        internal static JObject AsObject(JToken value, string message)
        {
            var result = value as JObject;
            if (result == null) throw new InvalidDataException(message);
            return result;
        }

        internal static JArray AsArray(JToken value, string message)
        {
            var result = value as JArray;
            if (result == null) throw new InvalidDataException(message);
            return result;
        }

        internal static JObject FindById(JArray values, string id)
        {
            foreach (var item in values)
            {
                if (StringComparer.Ordinal.Equals(item?["id"]?.Value<string>(), id))
                    return (JObject)item;
            }
            throw new KeyNotFoundException("Recruitment content ID was not found: " + id);
        }

        internal static JObject FindSignature(JObject signatureContent, string signatureId)
        {
            foreach (var item in AsArray(signatureContent["signatureRecruits"], "signatureRecruits must be an array."))
            {
                if (StringComparer.Ordinal.Equals(item?["signatureId"]?.Value<string>(), signatureId))
                    return (JObject)item;
            }
            throw new KeyNotFoundException("Signature recruit was not found: " + signatureId);
        }

        internal static string[] StringArray(JToken token)
        {
            var result = new List<string>();
            foreach (var item in AsArray(token, "Expected a string array.")) result.Add(item.Value<string>());
            return result.ToArray();
        }

        internal static T ChooseToken<T>(Pcg32 rng, IList<T> values)
        {
            if (values == null || values.Count == 0) throw new InvalidOperationException("Cannot choose from an empty content table.");
            return values[(int)rng.NextBounded((uint)values.Count)];
        }

        internal static JToken Choose(Pcg32 rng, JArray values)
        {
            if (values == null || values.Count == 0) throw new InvalidOperationException("Cannot choose from an empty content table.");
            return values[(int)rng.NextBounded((uint)values.Count)];
        }

        internal static int Int(JToken value) => value.Value<int>();

        internal static JObject ToObjectToken(object value)
        {
            return JObject.FromObject(value, JsonSerializer.Create(CanonicalJson.DefaultSettings()));
        }

        internal static string Invariant(object value)
        {
            if (value == null) return "NULL";
            if (value is bool boolean) return boolean ? "TRUE" : "FALSE";
            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }
    }
}
