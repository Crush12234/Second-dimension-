using System;
using System.IO;
using NUnit.Framework;
using SecondDimension.Presentation.Boot;

namespace SecondDimension.Tests.EditMode
{
    public sealed class AlphaPortable132Tests
    {
        string root;
        [SetUp] public void SetUp() { root = Path.Combine(Path.GetTempPath(), "SDAlpha132_" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); }
        [TearDown] public void TearDown() { if (Directory.Exists(root)) Directory.Delete(root, true); }
        [Test] public void PortablePackageNeverLoadsPersonalSaveAndCarriesItsSaveWhenMoved()
        {
            var personal = Path.Combine(root, "Personal"); Directory.CreateDirectory(personal);
            File.WriteAllText(Path.Combine(personal, "untouched.json"), "personal-original");
            var game = Path.Combine(root, "Game"); Directory.CreateDirectory(game);
            File.WriteAllText(Path.Combine(game, AlphaProfile132.MarkerFile132), AlphaProfile132.MarkerValue132);
            var saveDir = AlphaProfile132.ResolveDirectory132(personal, Path.Combine(game, "Game_Data"));
            Assert.That(saveDir, Is.EqualTo(Path.Combine(game, "SaveData")));
            Assert.That(Directory.GetFiles(saveDir), Is.Empty);
            File.WriteAllText(Path.Combine(saveDir, "progress.json"), "alpha-progress");
            var moved = Path.Combine(root, "MovedGame"); Directory.Move(game, moved);
            var movedDir = AlphaProfile132.ResolveDirectory132(personal, Path.Combine(moved, "Game_Data"));
            Assert.That(File.ReadAllText(Path.Combine(movedDir, "progress.json")), Is.EqualTo("alpha-progress"));
            Assert.That(File.ReadAllText(Path.Combine(personal, "untouched.json")), Is.EqualTo("personal-original"));
        }
        [Test] public void OwnerBuildKeepsExistingLocationAndInvalidPortableMarkerCannotFallBackToIt()
        {
            var personal = Path.Combine(root, "Personal");
            Assert.That(AlphaProfile132.ResolveDirectory132(personal, Path.Combine(root, "Game_Data")), Is.EqualTo(personal));
            File.WriteAllText(Path.Combine(root, AlphaProfile132.MarkerFile132), "broken");
            Assert.Throws<IOException>(() => AlphaProfile132.ResolveDirectory132(personal, Path.Combine(root, "Game_Data")));
            Assert.That(Directory.Exists(personal), Is.False);
        }
    }
}
