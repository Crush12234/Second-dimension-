using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SecondDimension.Tests.PlayMode
{
    public sealed class MissionsStory122PlayModeTests
    {
        const string SourceHash122 = "D605FB18B9945A4F1BC7816909A6F775D7F489E2073B1842DFF00BB10F92424B";
        string _source, _directory, _save;
        GameObject _host;
        M1FlowPresenter _presenter;
        M1RuntimeCoordinator _coordinator;

        [SetUp]
        public void SetUp122()
        {
            var requested = Environment.GetEnvironmentVariable("SD_MISSIONS_STORY122_SOURCE");
            _source = string.IsNullOrWhiteSpace(requested)
                ? @"C:\SecondDimension\BuildEvidence\Reset110\R280_GateA_NativeReload\EarnedReviewSave097.json"
                : Path.GetFullPath(requested);
            if (!File.Exists(_source) && string.IsNullOrWhiteSpace(requested))
                Assert.Ignore("Preserved R280 native Campaign evidence is not available on this machine.");
            Assert.That(File.Exists(_source), Is.True, "Explicit evidence path must exist.");
            Assert.That(Hash122(_source), Is.EqualTo(SourceHash122));
            _directory = Path.Combine(Path.GetTempPath(), "sd_missions_story122_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _save = Path.Combine(_directory, "CampaignCopy122.json");
            File.Copy(_source, _save);
            _coordinator = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _save);
            _host = new GameObject("Missions Story UI122");
            _presenter = _host.AddComponent<M1FlowPresenter>();
            _presenter.enabled = false;
            _presenter.Initialize(_coordinator);
        }

        [TearDown]
        public void TearDown122()
        {
            if (_presenter != null)
            {
                var canvas = Field122<Canvas>("_canvas");
                if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            if (_directory != null && Directory.Exists(_directory))
            {
                var directory = Path.GetFullPath(_directory);
                Assert.That(directory.StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\', '/') +
                    Path.DirectorySeparatorChar + "sd_missions_story122_", StringComparison.OrdinalIgnoreCase), Is.True);
                Directory.Delete(directory, true);
            }
            if (_source != null && File.Exists(_source)) Assert.That(Hash122(_source), Is.EqualTo(SourceHash122));
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator RealR280HomeMissionsShowsEligibleStoryKeepsContractsAndResumesStartedQuest122()
        {
            var chapter = _coordinator.Campaign019.Chapters.Single(value => value.ChapterId == "CH018_002");
            Assert.That(chapter.Status, Is.EqualTo("AVAILABLE"));
            Assert.That(chapter.Title, Is.EqualTo("Walls That Remember"));
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Null.Or.Empty);
            Assert.That(_coordinator.CampaignWorldGate023.ActiveOperationId, Is.Null.Or.Empty);
            var before = File.ReadAllBytes(_save);
            var beforeBackup = File.Exists(_save + ".bak") ? File.ReadAllBytes(_save + ".bak") : null;
            Invoke122("ReturnToWalkableHall069");
            Click122("Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("CAMPAIGN"));
            var startName = "Start compact playable campaign chapter CH018_002 084";
            Assert.That(Button122(startName).GetComponentInChildren<Text>().text, Is.EqualTo("START THIS STORY QUEST"));
            Assert.That(Buttons122().Any(value => value.name == "Start compact playable campaign chapter CH018_001 084"), Is.False);
            AssertBytes122(before, beforeBackup);

            Click122("Guild City Tab CONTRACTS");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("CONTRACTS"));
            Assert.That(Buttons122().Any(value => value.name == "Guild City Tab CAMPAIGN"), Is.True);
            Click122("Guild City Tab CAMPAIGN");
            AssertBytes122(before, beforeBackup);
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Null.Or.Empty,
                "Browsing Missions and Contracts never starts a chapter.");

            // Deliberate use of the real rendered chapter button is the only start.
            Click122(startName);
            Assert.That(_coordinator.CampaignPlayable020.ActiveChapterId, Is.EqualTo("CH018_002"));
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Not.Null.And.Not.Empty);
            var started = File.ReadAllBytes(_save);
            var startedBackup = File.ReadAllBytes(_save + ".bak");
            Assert.That(Field122<RectTransform>("_campaignCardRoot129").gameObject.activeInHierarchy, Is.True);
            Click122("Story Card Guild 129");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("HALL"));
            AssertBytes122(started, startedBackup);
            Click122("Living Guild Hub Facility " + WalkableGuildHall069.ContractDestinationId069 + " 074");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("CAMPAIGN"));
            Assert.That(Buttons122().Any(value => value.name == startName), Is.False,
                "An active chapter resumes its current room rather than offering a second start.");
            Assert.That(Buttons122().Any(value => value.name == "Move forward campaign room 084"), Is.True);
            AssertBytes122(started, startedBackup);
            yield return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator IdleTowerCampaignEntryIsReadOnlyAndDeliberateStoryStartReleasesTower158()
        {
            var begun = _coordinator.BeginTowerRun081();
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            var tower = _coordinator.CampaignProgression022;
            Assert.That(M1FlowPresenter.CanBrowseCampaignFromIdleTowerForVerification158(
                tower, _coordinator.GuildCity017D, M2BattleViewAccess098.Read(_coordinator)), Is.True);
            var towerId = tower.ActiveAbyssOperationId;
            var clearedFloors = tower.HighestClearedTowerFloor;
            var before = File.ReadAllBytes(_save);
            var backup = File.ReadAllBytes(_save + ".bak");
            Invoke122("OpenMissions084");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("CAMPAIGN"));
            Assert.That(Buttons122().Any(value => value.name == "Campaign required resume 158"), Is.False);
            Assert.That(Button122("Guild Mobile Nav Missions 084").GetComponentInChildren<Text>().text,
                Is.EqualTo("CAMPAIGN"));
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.EqualTo(towerId));
            AssertBytes122(before, backup);

            Click122("Start compact playable campaign chapter CH018_002 084");
            Assert.That(_coordinator.CampaignPlayable020.ActiveChapterId, Is.EqualTo("CH018_002"));
            Assert.That(_coordinator.CampaignProgression022.ActiveAbyssOperationId, Is.Null.Or.Empty);
            Assert.That(_coordinator.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(clearedFloors));
            var reloaded = new M1RuntimeCoordinator(
                Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"), _save);
            Assert.That(reloaded.CampaignPlayable020.ActiveChapterId, Is.EqualTo("CH018_002"));
            Assert.That(reloaded.CampaignProgression022.ActiveAbyssOperationId, Is.Null.Or.Empty);
            Assert.That(reloaded.CampaignProgression022.HighestClearedTowerFloor, Is.EqualTo(clearedFloors));
            yield return null;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PendingTowerReceiptShowsRequiredResumeWithoutStartingCampaign158()
        {
            var begun = _coordinator.BeginTowerRun081();
            Assert.That(begun.Succeeded, Is.True, begun.Message);
            var committed = _coordinator.CommitAbyssStep022();
            Assert.That(committed.Succeeded, Is.True, committed.Message);
            Assert.That(_coordinator.CampaignProgression022.HasPendingAbyssStepReceipt, Is.True);
            var before = File.ReadAllBytes(_save);
            var backup = File.ReadAllBytes(_save + ".bak");
            Invoke122("OpenMissions084");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("CAMPAIGN"));
            Assert.That(Button122("Campaign required resume 158").GetComponentInChildren<Text>().text,
                Is.EqualTo("RETURN TO CURRENT TOWER"));
            Assert.That(Buttons122().Any(value => value.name.StartsWith(
                "Start compact playable campaign chapter ", StringComparison.Ordinal)), Is.False);
            AssertBytes122(before, backup);
            Click122("Campaign required resume 158");
            Assert.That(Field122<string>("_guildCityTab017D"), Is.EqualTo("ABYSS"));
            Assert.That(_coordinator.CampaignProgression022.HasPendingAbyssStepReceipt, Is.True);
            Assert.That(_coordinator.CampaignPlayable020.ActiveOperationId, Is.Null.Or.Empty);
            AssertBytes122(before, backup);
            yield return null;
        }

        void AssertBytes122(byte[] primary, byte[] backup)
        {
            CollectionAssert.AreEqual(primary, File.ReadAllBytes(_save));
            if (backup == null) Assert.That(File.Exists(_save + ".bak"), Is.False);
            else CollectionAssert.AreEqual(backup, File.ReadAllBytes(_save + ".bak"));
        }

        Button[] Buttons122() => Field122<RectTransform>("_screenRoot").GetComponentsInChildren<Button>(false);
        Button Button122(string name) => Buttons122().Single(value => value.name == name);
        void Click122(string name)
        {
            var button = Button122(name);
            Assert.That(button.gameObject.activeInHierarchy && button.IsInteractable(), Is.True, name);
            button.onClick.Invoke();
        }
        T Field122<T>(string name) => (T)typeof(M1FlowPresenter).GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(_presenter);
        void Invoke122(string name) => typeof(M1FlowPresenter).GetMethod(name,
            BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_presenter, null);
        static string Hash122(string path)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", string.Empty);
        }
    }
}
