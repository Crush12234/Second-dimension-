#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Release026;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalExecutionReadiness026Tests
    {
        [Test]
        public void FinalExecutionHealthAndCoordinatorSurfaceAreReady()
        {
            var health = FinalExecutionHealthService026.BuildSnapshot();
            Assert.IsTrue(health.IsReady, health.Error);
            Assert.AreEqual(9, health.CoordinatorInterfacesExpected);
            Assert.AreEqual(9, health.CoordinatorInterfacesImplemented);
            Assert.Greater(health.CoordinatorCommandMethods, 30);
            Assert.AreEqual(SaveEnvelopeV1.CurrentFormatVersion, health.SaveFormatVersion);
            Assert.AreEqual(130, health.WorldGateBoards);
            Assert.AreEqual(1372, health.WorldGateNodes);
            Assert.AreEqual(30, health.CityBuildings);
            Assert.AreEqual(240, health.ArtBindings);
        }

        [Test]
        public void EveryCoordinatorInterfaceMapsToConcreteRuntimeMethods()
        {
            var coordinator = typeof(M1RuntimeCoordinator);
            foreach (var contract in FinalExecutionHealthService026.ExpectedCoordinatorInterfaces())
            {
                Assert.IsTrue(contract.IsAssignableFrom(coordinator), contract.FullName);
                var map = coordinator.GetInterfaceMap(contract);
                Assert.IsTrue(map.TargetMethods.All(value => value != null && !value.IsAbstract), contract.FullName);
            }
        }

        [Test]
        public void FreshGuildCommitsApplicantBoardAndReloadsSameState()
        {
            var save = Path.Combine(Path.GetTempPath(),
                "SecondDimension_FinalExecution026_" + Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var content = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
                var coordinator = new M1RuntimeCoordinator(content, save);
                var created = coordinator.CreateGuild(new M1NewGuildIntent
                {
                    GuildmasterName = "Execution Tester",
                    ModeId = "Standard",
                    TutorialDepthId = "Full Tutorial",
                    TextScale = 1f,
                    HighContrast = false,
                    ReducedMotion = false
                });
                Assert.IsTrue(created.Succeeded, created.Message);
                var first = coordinator.State;
                Assert.IsTrue(first.HasCampaign);
                Assert.AreEqual(6, first.Applicants.Count);
                Assert.IsTrue(File.Exists(save));
                Assert.IsFalse(string.IsNullOrWhiteSpace(first.CanonicalStateHash));

                var reloaded = new M1RuntimeCoordinator(content, save);
                var second = reloaded.State;
                Assert.IsTrue(second.HasCampaign);
                Assert.AreEqual(6, second.Applicants.Count);
                Assert.AreEqual(first.CanonicalStateHash, second.CanonicalStateHash);
                Assert.GreaterOrEqual(SaveEnvelopeV1.CurrentFormatVersion, 10);
            }
            finally
            {
                foreach (var path in Directory.GetFiles(Path.GetDirectoryName(save),
                             Path.GetFileName(save) + "*"))
                    try { File.Delete(path); } catch { }
            }
        }

        [Test]
        public void UnreadableLegacySaveIsPreservedAndReportedAsOptionalDiagnostic071()
        {
            var save = Path.Combine(Path.GetTempPath(),
                "SecondDimension_SaveRecovery071_" + Guid.NewGuid().ToString("N") + ".json");
            const string unreadableLegacyPayload = "{\"SaveFormatVersion\":1,\"CampaignState\":{}}";
            try
            {
                File.WriteAllText(save, unreadableLegacyPayload);
                var content = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");

                var coordinator = new M1RuntimeCoordinator(content, save);
                var state = coordinator.State;

                Assert.IsFalse(state.HasCampaign);
                Assert.IsTrue(state.HasSave);
                Assert.IsTrue(string.IsNullOrWhiteSpace(state.StatusMessage),
                    "A save-only recovery problem must not become the red startup status.");
                StringAssert.Contains("Primary save failed", state.SaveRecoveryDiagnostic);
                Assert.AreEqual(unreadableLegacyPayload, File.ReadAllText(save),
                    "Opening the title must never rewrite or delete an unreadable save.");
            }
            finally
            {
                foreach (var path in Directory.GetFiles(Path.GetDirectoryName(save),
                             Path.GetFileName(save) + "*"))
                    try { File.Delete(path); } catch { }
            }
        }

        [Test]
        public void RequiredUnityPackagesAndBootAuthorityExist()
        {
            var root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var manifest = File.ReadAllText(Path.Combine(root, "Packages", "manifest.json"));
            foreach (var package in new[]
                     {
                         "com.unity.nuget.newtonsoft-json", "com.unity.test-framework",
                         "com.unity.ugui"
                     })
                StringAssert.Contains(package, manifest);
            Assert.IsTrue(File.Exists(Path.Combine(root, "Assets", "Scenes", "Boot.unity")));
            Assert.IsTrue(File.Exists(Path.Combine(root, "Assets", "StreamingAssets", "Authority",
                "AUTHORITY_RUNTIME_MANIFEST.json")));
        }
    }
}
#endif
