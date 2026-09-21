#if UNITY_EDITOR
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Presentation.Release024;
using SecondDimension.Presentation.Release025;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalSupersessionPipeline030Tests
    {
        [SetUp]
        public void Setup() => ReleaseSupersessionRegistry030.ClearCacheForTests();

        [Test]
        public void ActiveReleaseContractAndRetainedManifestsAreCompatible()
        {
            var snapshot = ReleaseSupersessionReadinessService030.BuildSnapshot();
            Assert.IsTrue(snapshot.IsReady, snapshot.Error);
            Assert.AreEqual(11, snapshot.SaveFormatVersion);
            Assert.AreEqual(6, snapshot.CompatibleReleaseManifests);
            Assert.AreEqual("05_RUN_COMPLETE_FINAL_SUPERSESSION_PIPELINE_030.cmd",
                snapshot.ActivePipeline);
        }


        [Test]
        public void RetainedReleaseRegistriesAcceptSaveElevenAndTenByTenUnionCapacity()
        {
            var integration = FullGameIntegrationHealthService024.BuildSnapshot();
            Assert.IsTrue(integration.IsReady, integration.Error);
            Assert.AreEqual(10, integration.AlliedUnionCapacity);
            Assert.AreEqual(10, integration.EnemyUnionCapacity);

            var hardening = ImplementationReadinessService025.BuildSnapshot();
            Assert.IsTrue(hardening.IsReady, hardening.Error);
            Assert.AreEqual(11, hardening.SaveFormatVersion);
            Assert.AreEqual("6000.3.22f1", hardening.UnityVersion);
            Assert.AreEqual(10, hardening.AlliedUnionCapacity);
            Assert.AreEqual(10, hardening.EnemyUnionCapacity);
        }

        [Test]
        public void PackageManifestKeepsTheApprovedMinimumAndRemovedPackagesStayRemoved()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var manifest = JObject.Parse(
                File.ReadAllText(Path.Combine(root, "Packages", "manifest.json")));
            var dependencies = (JObject)manifest["dependencies"];
            foreach (var required in ReleaseSupersessionRegistry030.Contract.requiredPackages)
                Assert.IsNotNull(dependencies[required], required);
            foreach (var forbidden in ReleaseSupersessionRegistry030.Contract.forbiddenPackages)
                Assert.IsNull(dependencies[forbidden], forbidden);
        }

        [Test]
        public void SaveElevenRetainsBothWorldGateAndCreatorMigrations()
        {
            Assert.AreEqual(11, SaveEnvelopeV1.CurrentFormatVersion);
            var root = Directory.GetParent(Application.dataPath).FullName;
            var store = File.ReadAllText(Path.Combine(root, "Assets", "SecondDimension",
                "Save", "AtomicSaveStore.cs"));
            StringAssert.Contains(
                "save_v9_to_v10_world_gate_expedition_runtime_023_defaults", store);
            StringAssert.Contains(
                "save_v10_to_v11_creator_codes_rooms_028_defaults", store);
        }

        [Test]
        public void LegacyTestsNoLongerClaimPermanentOwnershipOfSaveTen()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var files = Directory.GetFiles(Path.Combine(root, "Assets", "Tests"), "*.cs",
                SearchOption.AllDirectories);
            var pattern = new Regex(
                @"Assert\.AreEqual\(\s*10\s*,\s*SaveEnvelopeV1\.CurrentFormatVersion|" +
                @"CurrentFormatVersion[^\r\n]*Is\.EqualTo\(10\)");
            foreach (var path in files)
                Assert.IsFalse(pattern.IsMatch(File.ReadAllText(path)), path);
        }

        [Test]
        public void LegacyPipelineShimRoutesForward()
        {
            var root = Directory.GetParent(Application.dataPath).FullName;
            var text = File.ReadAllText(
                Path.Combine(root, "05_RUN_COMPLETE_FINAL_PIPELINE_025.ps1"));
            StringAssert.Contains("05_RUN_COMPLETE_FINAL_SUPERSESSION_PIPELINE_030.ps1",
                text);
        }
    }
}
#endif
