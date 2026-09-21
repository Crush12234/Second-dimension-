using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Boot;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ManualEarnedSaveReview097Tests
    {
        private string _root, _source, _personal, _buildData, _output;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "SecondDimension_ManualReview097_" + Guid.NewGuid().ToString("N"));
            _source = Path.Combine(_root, "Source", "Earned.json");
            _personal = Path.Combine(_root, "Personal");
            _buildData = Path.Combine(_root, "Player", "SecondDimension_Data");
            _output = Path.Combine(_root, "Review");
            Directory.CreateDirectory(Path.GetDirectoryName(_source));
            Directory.CreateDirectory(_personal);
            Directory.CreateDirectory(_buildData);
            // A save-format/path fixture only, never an earned-playthrough claim.
            new AtomicSaveStore().Write(_source,
                SaveEnvelopeV1.Create(CampaignFactory.CreateM0Proof(9701), DateTime.UnixEpoch));
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null && Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        private string[] Args(string source = null, string output = null) => new[]
        {
            "SecondDimension.exe", ManualEarnedSaveReview097.SourceFlag097 + (source ?? _source),
            ManualEarnedSaveReview097.OutputFlag097 + (output ?? _output)
        };
        private string Prepare(string[] arguments) => ManualEarnedSaveReview097.Prepare097(arguments, _personal, _buildData);

        [Test]
        public void ActualBootResolverCopiesExactEnvelopeAndLeavesSourceAndPersonalUntouched097()
        {
            var before = File.ReadAllBytes(_source);
            var personalSave = Path.Combine(_personal, "second_dimension_first_hour_slice_071.json");
            File.WriteAllText(personalSave, "PERSONAL SAVE SENTINEL");
            var path = BootCoordinator.ResolveCoordinatorSavePathForVerification078(Args(), _personal,
                Path.Combine(_root, "Cache"), _buildData);
            Assert.That(path, Is.EqualTo(Path.Combine(_output, ManualEarnedSaveReview097.CopyFileName097)));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(_source), Is.EqualTo(before));
            Assert.That(File.ReadAllText(personalSave), Is.EqualTo("PERSONAL SAVE SENTINEL"));
            var receipt = JObject.Parse(File.ReadAllText(Path.Combine(_output, "manual_review097_receipt.json")));
            Assert.That(receipt.Value<string>("SourceSha256"), Is.EqualTo(receipt.Value<string>("CopiedSha256")));
            Assert.That(receipt.Value<bool>("AutomaticGameplay"), Is.False);
            Assert.That(receipt.Value<bool>("CampaignStateEditedByLauncher"), Is.False);
            Assert.That(new AtomicSaveStore().ReadWithRecovery(path).Value.CanonicalStateHash,
                Is.EqualTo(new AtomicSaveStore().ReadWithRecovery(_source).Value.CanonicalStateHash));
            Assert.That(Directory.GetFiles(_output).Length, Is.EqualTo(2));
        }

        [Test]
        public void NoReviewFlagPreservesOldPersonalDefaultWithoutCreatingReview097()
        {
            var path = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                new[] { "SecondDimension.exe", "-screen-width", "1920" }, _personal, _root, _buildData);
            Assert.That(path, Is.EqualTo(Path.Combine(_personal, "second_dimension_first_hour_slice_071.json")));
            Assert.That(Directory.Exists(_output), Is.False);
            Assert.That(ManualEarnedSaveReview097.IsRequested097(null), Is.False);
        }

        [TestCase("--sd-earned-review-source")]
        [TestCase("--SD-EARNED-REVIEW-SOURCE=C:/missing.json")]
        [TestCase("--sd-earned-review-output=")]
        [TestCase("--sd-earned-review-unknown=yes")]
        public void MalformedExplicitRequestThrowsRatherThanReturningPersonalSave097(string malformed)
        {
            Assert.That(ManualEarnedSaveReview097.IsRequested097(new[] { malformed }), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                BootCoordinator.ResolveCoordinatorSavePathForVerification078(new[] { malformed }, _personal, _root, _buildData));
            Assert.That(Directory.Exists(_output), Is.False);
        }

        [TestCase("--sd-roster-audit-093")]
        [TestCase("--sd-first-hour-gold-smoke")]
        [TestCase("--sd-owner-smoke-030")]
        [TestCase("--sd-earned-review-source=C:/duplicate.json")]
        [TestCase("--sd-earned-review-output=C:/duplicate")]
        public void ConflictingAutomationOrDuplicateFlagsFailBeforeCopy097(string extra)
        {
            Assert.Throws<InvalidOperationException>(() => Prepare(Args().Concat(new[] { extra }).ToArray()));
            Assert.That(Directory.Exists(_output), Is.False);
        }

        [TestCase("relative.json")]
        [TestCase("C:relative.json")]
        [TestCase("\\root-relative.json")]
        [TestCase("\\\\server\\share\\save.json")]
        [TestCase("\\\\?\\C:\\save.json")]
        [TestCase("C:\\Review\\..\\Personal")]
        [TestCase("C:\\Review\\.\\Save")]
        [TestCase("C:\\Review\\Save:stream")]
        [TestCase("C:\\Review\\Save. ")]
        [TestCase("C:\\Review\\*.json")]
        [TestCase("C:\\Review\\NUL.json")]
        [TestCase("C:\\Review\\COM1")]
        [TestCase("C:\\Users\\SIMON~1\\AppData\\Review")]
        public void UnsafeLexicalPathsAreRejectedBeforeIo097(string path)
        {
            Assert.Throws<InvalidOperationException>(() => ManualEarnedSaveReview097.LocalAbsolutePath097(path));
        }

        [TestCase("root")]
        [TestCase("source-directory")]
        [TestCase("source-child")]
        [TestCase("source-file")]
        [TestCase("personal")]
        [TestCase("personal-child")]
        [TestCase("build-data")]
        [TestCase("build-installation")]
        [TestCase("missing-parent")]
        public void UnsafeOutputBoundariesNeverWriteOrOverwrite097(string kind)
        {
            var target = kind == "root" ? Path.GetPathRoot(_root) :
                kind == "source-directory" ? Path.GetDirectoryName(_source) :
                kind == "source-child" ? Path.Combine(Path.GetDirectoryName(_source), "Review") :
                kind == "source-file" ? _source :
                kind == "personal" ? _personal :
                kind == "personal-child" ? Path.Combine(_personal, "Review") :
                kind == "build-data" ? Path.Combine(_buildData, "Review") :
                kind == "build-installation" ? Path.Combine(Path.GetDirectoryName(_buildData), "Review") :
                Path.Combine(_root, "Missing", "Review");
            var before = File.ReadAllBytes(_source);
            Assert.Throws<InvalidOperationException>(() => Prepare(Args(output: target)));
            Assert.That(File.ReadAllBytes(_source), Is.EqualTo(before));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExistingEmptyOrNonemptyOutputIsNeverReused097(bool nonempty)
        {
            Directory.CreateDirectory(_output);
            if (nonempty) File.WriteAllText(Path.Combine(_output, "keep.txt"), "KEEP");
            Assert.Throws<InvalidOperationException>(() => Prepare(Args()));
            Assert.That(Directory.GetFiles(_output).Length, Is.EqualTo(nonempty ? 1 : 0));
        }

        [Test]
        public void RelaunchRequiresNewDirectoryAndCannotResetPlayedReviewSave097()
        {
            var path = Prepare(Args());
            var copiedBefore = File.ReadAllBytes(path);
            Assert.Throws<InvalidOperationException>(() => Prepare(Args()));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(copiedBefore));
        }

        [TestCase("personal")]
        [TestCase("build")]
        public void PersonalOrInstalledSourceIsNotAccepted097(string kind)
        {
            var source = Path.Combine(kind == "personal" ? _personal : _buildData, "Save.json");
            File.Copy(_source, source);
            Assert.Throws<InvalidOperationException>(() => Prepare(Args(source: source)));
            Assert.That(Directory.Exists(_output), Is.False);
        }

        [Test]
        public void InvalidPrimaryDoesNotBorrowItsValidSourceBackupOrFallBack097()
        {
            File.Copy(_source, _source + ".bak");
            File.WriteAllText(_source, "not a valid save", Encoding.UTF8);
            Assert.Throws<InvalidOperationException>(() => Prepare(Args()));
            Assert.That(File.ReadAllText(_source), Is.EqualTo("not a valid save"));
            Assert.That(File.Exists(Path.Combine(_output, "manual_review097_receipt.json")), Is.False);
            Assert.That(File.Exists(Path.Combine(_output, ManualEarnedSaveReview097.CopyFileName097 + ".bak")), Is.False);
        }

        [Test]
        public void SourceOrOutputThroughActualReparseParentIsRejected097()
        {
            var link = Path.Combine(_root, "LinkedSource");
            // Developer Mode or the Windows symlink privilege is required to create
            // this isolated test link; production rejection never needs that privilege.
            if (!CreateSymbolicLink(link, Path.GetDirectoryName(_source), 0x1 | 0x2))
                Assert.Ignore("Windows denied creation of a test-only directory symlink: " + Marshal.GetLastWin32Error());
            try
            {
                Assert.Throws<InvalidOperationException>(() => Prepare(Args(source: Path.Combine(link, "Earned.json"))));
                Assert.Throws<InvalidOperationException>(() => Prepare(Args(output: Path.Combine(link, "Review"))));
                Assert.That(Directory.Exists(_output), Is.False);
            }
            finally
            {
                // Remove only the exact symlink, never recursively follow its target.
                Directory.Delete(link, false);
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool CreateSymbolicLink(string symbolicFileName, string targetFileName, int flags);
    }
}
