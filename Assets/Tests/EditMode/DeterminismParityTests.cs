using System.Text;
using NUnit.Framework;
using SecondDimension.Determinism;

namespace SecondDimension.Tests.EditMode
{
    public sealed class DeterminismParityTests
    {
        [Test]
        public void SemanticSeedSerializationMatchesFrozenExample()
        {
            var actual = Encoding.UTF8.GetString(SemanticSeed.Serialize(20260811, "FLOOR", 3));
            Assert.That(actual, Is.EqualTo("8:20260811;5:FLOOR;1:3;"));
        }

        [Test]
        public void SemanticSeedPairMatchesFrozenPythonAuthority()
        {
            var actual = SemanticSeed.Derive(20260811, "GOLDEN", 1);
            Assert.That(actual.Seed, Is.EqualTo(13813106353344599285UL));
            Assert.That(actual.Stream, Is.EqualTo(651481422552750002UL));
        }

        [Test]
        public void FirstTenPcgOutputsMatchGoldenVector()
        {
            var expected = new uint[]
            {
                198797011, 3803684820, 346211905, 4218557422, 1074995891,
                594085060, 3561282023, 3541317521, 244531059, 3383138586
            };
            var random = Pcg32.FromParts(20260811, "GOLDEN", 1);

            foreach (var value in expected)
            {
                Assert.That(random.NextUInt32(), Is.EqualTo(value));
            }
        }

        [Test]
        public void RejectionSampledBoundsMatchFrozenPythonAuthority()
        {
            var bounds = new ulong[] { 1, 2, 3, 6, 10, 100, 1000, 1UL << 31, 1UL << 32 };
            var expected = new ulong[] { 0, 1, 0, 1, 7, 15, 242, 1811095437, 4003407617 };
            var random = Pcg32.FromParts(20260811, "BOUNDED", 1);

            for (var index = 0; index < bounds.Length; index++)
            {
                Assert.That(random.NextBounded(bounds[index]), Is.EqualTo(expected[index]));
            }
        }

        [Test]
        public void CanonicalJsonSortsObjectKeysWithoutReorderingArrays()
        {
            var value = new { z = 2, a = new[] { 3, 1 }, m = new { b = 2, a = 1 } };
            Assert.That(CanonicalJson.Serialize(value), Is.EqualTo("{\"a\":[3,1],\"m\":{\"a\":1,\"b\":2},\"z\":2}"));
        }
    }
}
