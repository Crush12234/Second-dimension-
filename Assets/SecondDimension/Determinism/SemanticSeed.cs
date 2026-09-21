using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SecondDimension.Determinism
{
    public readonly struct SeedPair : IEquatable<SeedPair>
    {
        public SeedPair(ulong seed, ulong stream)
        {
            Seed = seed;
            Stream = stream;
        }

        public ulong Seed { get; }
        public ulong Stream { get; }

        public bool Equals(SeedPair other) => Seed == other.Seed && Stream == other.Stream;
        public override bool Equals(object obj) => obj is SeedPair other && Equals(other);
        public override int GetHashCode() => Seed.GetHashCode() ^ Stream.GetHashCode();
        public override string ToString() => $"{Seed:X16}:{Stream:X16}";
    }

    public static class SemanticSeed
    {
        public static byte[] Serialize(params object[] parts)
        {
            var bytes = new List<byte>();
            foreach (var part in parts ?? Array.Empty<object>())
            {
                var raw = Encoding.UTF8.GetBytes(ToInvariantString(part));
                bytes.AddRange(Encoding.ASCII.GetBytes(raw.Length.ToString(CultureInfo.InvariantCulture)));
                bytes.Add((byte)':');
                bytes.AddRange(raw);
                bytes.Add((byte)';');
            }

            return bytes.ToArray();
        }

        public static SeedPair Derive(params object[] parts)
        {
            using (var sha = SHA256.Create())
            {
                var digest = sha.ComputeHash(Serialize(parts));
                return new SeedPair(ReadUInt64BigEndian(digest, 0), ReadUInt64BigEndian(digest, 8));
            }
        }

        private static string ToInvariantString(object value)
        {
            if (value == null)
            {
                return "NULL";
            }

            if (value is bool boolean)
            {
                return boolean ? "TRUE" : "FALSE";
            }

            return value is IFormattable formattable
                ? formattable.ToString(null, CultureInfo.InvariantCulture)
                : value.ToString();
        }

        private static ulong ReadUInt64BigEndian(byte[] bytes, int offset)
        {
            return ((ulong)bytes[offset] << 56)
                 | ((ulong)bytes[offset + 1] << 48)
                 | ((ulong)bytes[offset + 2] << 40)
                 | ((ulong)bytes[offset + 3] << 32)
                 | ((ulong)bytes[offset + 4] << 24)
                 | ((ulong)bytes[offset + 5] << 16)
                 | ((ulong)bytes[offset + 6] << 8)
                 | bytes[offset + 7];
        }
    }
}

