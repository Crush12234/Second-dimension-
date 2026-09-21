using System.IO;
using NUnit.Framework;
using SecondDimension.Content;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ContentRegistryTests
    {
        [Test]
        public void FrozenRegistryImportsAllCanonicalIdsAndResolvesReferences()
        {
            var authorityRoot = ResolveAuthorityRoot();
            var registry = ContentRegistry.LoadAndValidate(authorityRoot);

            if (!registry.Validation.IsValid)
            {
                Assert.Fail(string.Join("\n", registry.Validation.Errors));
            }

            Assert.That(registry.Count, Is.EqualTo(5092));
            Assert.That(registry.Validation.ShadowConflictCount, Is.EqualTo(120));
            Assert.That(registry.Validation.P0AssetReferenceCount, Is.EqualTo(112));
        }

        [Test]
        public void ProtectedStableIdsExistInCanonicalRegistry()
        {
            var authorityRoot = ResolveAuthorityRoot();
            var registry = ContentRegistry.LoadAndValidate(authorityRoot);

            Assert.That(registry.Contains("ABYSS_FLOOR_001"), Is.True);
            Assert.That(registry.Contains("ART_GUARD"), Is.True);
            Assert.That(registry.Contains("UI_FRAME_UNION_BATTLE"), Is.True);
        }

        [Test]
        public void BundledRuntimeAuthorityMatchesFrozenRegistry()
        {
            var runtimeAuthority = Path.Combine(Application.streamingAssetsPath, "Authority");
            var registry = ContentRegistry.LoadAndValidate(runtimeAuthority);

            if (!registry.Validation.IsValid)
            {
                Assert.Fail(string.Join("\n", registry.Validation.Errors));
            }

            Assert.That(registry.Count, Is.EqualTo(5092));
        }

        private static string ResolveAuthorityRoot()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var projectFreeze = Path.Combine(projectRoot, "IMPLEMENTATION_FREEZE");
            if (Directory.Exists(projectFreeze)) return projectRoot;

            // Source Collector packages intentionally put the frozen authority inside
            // StreamingAssets so a freshly opened Unity project is self-contained.
            var bundledAuthority = Path.Combine(Application.streamingAssetsPath, "Authority");
            return Directory.Exists(Path.Combine(bundledAuthority, "IMPLEMENTATION_FREEZE"))
                ? bundledAuthority
                : projectRoot;
        }
    }
}
