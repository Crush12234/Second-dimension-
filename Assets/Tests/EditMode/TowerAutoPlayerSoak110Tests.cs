using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Boot;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    // Path/opt-in tests use a save-format fixture, never an earned combat proof.
    public sealed class TowerAutoPlayerSoak110Tests
    {
        string _root, _source, _personal, _build, _output;
        [SetUp]
        public void SetUp110()
        {
            _root = Path.Combine(Path.GetTempPath(), "SecondDimensionSoak110Paths_" + Guid.NewGuid().ToString("N"));
            _source = Path.Combine(_root, "Source", "Envelope.json");
            _personal = Path.Combine(_root, "Personal");
            _build = Path.Combine(_root, "Player", "SecondDimension_Data");
            _output = Path.Combine(_root, "Output");
            Directory.CreateDirectory(Path.GetDirectoryName(_source));
            Directory.CreateDirectory(_personal);
            Directory.CreateDirectory(_build);
            new AtomicSaveStore().Write(_source, SaveEnvelopeV1.Create(CampaignFactory.CreateM0Proof(110), DateTime.UnixEpoch));
            File.WriteAllText(Path.Combine(_personal, "personal.sentinel"), "KEEP");
        }
        string[] Args110() => new[] { "SecondDimension.exe", "--sd-tower-soak110-source=" + _source,
            "--sd-tower-soak110-output=" + _output, "--sd-tower-soak110-through-floor=350" };
        [Test]
        public void ExplicitSoakReusesGuardedExactCopyAndDeclaresAutomaticGameplay110()
        {
            var bytes = File.ReadAllBytes(_source);
            var path = TowerAutoPlayerSoak110.Prepare110(Args110(), _personal, _build);
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(bytes));
            Assert.That(File.ReadAllBytes(_source), Is.EqualTo(bytes));
            Assert.That(File.ReadAllText(Path.Combine(_personal, "personal.sentinel")), Is.EqualTo("KEEP"));
            Assert.That(TowerAutoPlayerSoak110.Prepared110.Seconds, Is.EqualTo(1800));
            Assert.That(TowerAutoPlayerSoak110.Prepared110.Speed, Is.EqualTo(16));
            var launch = JObject.Parse(File.ReadAllText(Path.Combine(_output, "tower_soak110_launch.json")));
            Assert.That(launch.Value<bool>("AutomaticGameplay"), Is.True);
            Assert.That(launch.Value<bool>("CampaignStateEditedByLauncher"), Is.False);
            Assert.Throws<InvalidOperationException>(() => TowerAutoPlayerSoak110.Prepare110(Args110(), _personal, _build));
        }
        [TestCase("--sd-tower-soak110-seconds=1801")]
        [TestCase("--sd-tower-soak110-speed=128")]
        [TestCase("--sd-tower-soak110-through-floor=351")]
        [TestCase("--sd-first-hour-gold-smoke")]
        [TestCase("--sd-earned-review-source=C:/other.json")]
        [TestCase("--SD-TOWER-SOAK110-SECONDS=60")]
        public void UnsafeOrConflictingFlagFailsBeforeCopy110(string extra)
        {
            Assert.Throws<InvalidOperationException>(() => TowerAutoPlayerSoak110.Prepare110(
                Args110().Concat(new[] { extra }).ToArray(), _personal, _build));
            Assert.That(Directory.Exists(_output), Is.False);
            Assert.That(TowerAutoPlayerSoak110.Prepared110, Is.Null);
        }
        [Test]
        public void NormalPlayerArgumentsNeverRequestSoak110() => Assert.That(
            TowerAutoPlayerSoak110.IsRequested110(new[] { "SecondDimension.exe", "-screen-width", "1920" }), Is.False);
        [Test]
        public void FinalControllerSnapshotCapturesCompletedFloorWithoutInventingFrames110()
        {
            var owner = new UnityEngine.GameObject("Soak reporting fixture 110");
            try
            {
                var controller = owner.AddComponent<SecondDimension.Presentation.M2BattleExperienceController072>();
                var soak = owner.AddComponent<TowerAutoPlayerSoak110>();
                var type = typeof(TowerAutoPlayerSoak110);
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                type.GetField("_controller", flags).SetValue(soak, controller);
                type.GetField("_lastCompletedFloor", flags).SetValue(soak, 302);
                type.GetField("_frames", flags).SetValue(soak, 249);
                type.GetField("_animatedFrames", flags).SetValue(soak, 237);
                typeof(SecondDimension.Presentation.M2BattleExperienceController072)
                    .GetProperty("LastCompletedTowerFloor108").SetValue(controller, 303);
                type.GetMethod("CaptureControllerCounters110", flags).Invoke(soak, null);
                Assert.That(type.GetField("_lastCompletedFloor", flags).GetValue(soak), Is.EqualTo(303));
                Assert.That(type.GetField("_frames", flags).GetValue(soak), Is.EqualTo(249));
                Assert.That(type.GetField("_animatedFrames", flags).GetValue(soak), Is.EqualTo(237));
                Assert.That(type.GetField("_peakExactBeats", flags).GetValue(soak), Is.Zero,
                    "A final lifecycle snapshot cannot fabricate an exact-art playback witness.");
            }
            finally { UnityEngine.Object.DestroyImmediate(owner); }
        }

        [TearDown]
        public void RemoveOnlyPathFixture110()
        {
            foreach (var directory in Directory.GetDirectories(_root, "*", SearchOption.AllDirectories)
                .OrderByDescending(value => value.Length))
            {
                foreach (var file in Directory.GetFiles(directory)) File.Delete(file);
                Directory.Delete(directory, false);
            }
            foreach (var file in Directory.GetFiles(_root)) File.Delete(file);
            Directory.Delete(_root, false);
        }
    }
}
