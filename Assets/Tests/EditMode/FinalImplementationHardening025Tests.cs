using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using SecondDimension.Presentation.Release025;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalImplementationHardening025Tests
    {
        [Test]
        public void RegistryAndFullIntegrationHealthAreReady()
        {
            var registry = ImplementationHardeningRegistry025.LoadFromResources();
            var snapshot = ImplementationReadinessService025.BuildSnapshot();
            Assert.That(registry.CompileRiskGates.gates.Length, Is.EqualTo(16));
            Assert.That(snapshot.IsReady, Is.True, snapshot.Error);
            Assert.That(snapshot.WorldGateBoards, Is.EqualTo(130));
            Assert.That(snapshot.WorldGateNodes, Is.EqualTo(1372));
            Assert.That(snapshot.AlliedUnionCapacity, Is.EqualTo(10));
            Assert.That(snapshot.EnemyUnionCapacity, Is.EqualTo(10));
        }

        [Test]
        public void BuiltInFontIsUsedAndEmbeddedFontResourcesAreAbsent()
        {
            Assert.That(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), Is.Not.Null);
            Assert.That(Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionUISans"), Is.Null);
            Assert.That(Resources.Load<Font>("SecondDimension/Fonts/SecondDimensionDisplay"), Is.Null);
            var embedded = Directory.GetFiles(Application.dataPath, "*", SearchOption.AllDirectories)
                .Where(path => new[] { ".ttf", ".otf", ".woff", ".woff2" }
                    .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Assert.That(embedded, Is.Empty);
        }

        [Test]
        public void PackageManifestContainsOnlyApprovedMinimumSet()
        {
            var project = Directory.GetParent(Application.dataPath).FullName;
            var root = JObject.Parse(File.ReadAllText(Path.Combine(project, "Packages", "manifest.json")));
            var dependencies = (JObject)root["dependencies"];
            foreach (var required in global::SecondDimension.Presentation.Release030.ReleaseSupersessionRegistry030.Contract.requiredPackages)
                Assert.That(dependencies[required], Is.Not.Null, required);
            foreach (var forbidden in global::SecondDimension.Presentation.Release030.ReleaseSupersessionRegistry030.Contract.forbiddenPackages)
                Assert.That(dependencies[forbidden], Is.Null, forbidden);
        }

        [Test]
        public void SaveFormatRetainsTheHardeningBaselineAndCurrentSupersession()
        {
            Assert.That(SaveEnvelopeV1.CurrentFormatVersion, Is.GreaterThanOrEqualTo(10));
            Assert.That(AtomicSaveStore.CurrentSaveFormatVersion, Is.EqualTo(SaveEnvelopeV1.CurrentFormatVersion));
        }

        [Test]
        public void EditorBuildSettingsContainsOnlyTheBootSceneAndNoRemovedPackageConfig()
        {
            var project = Directory.GetParent(Application.dataPath).FullName;
            var text = File.ReadAllText(Path.Combine(project, "ProjectSettings", "EditorBuildSettings.asset"));
            Assert.That(text, Does.Contain("Assets/Scenes/Boot.unity"));
            Assert.That(text, Does.Not.Contain("com.unity.dt.app-ui"));
        }
    }
}
