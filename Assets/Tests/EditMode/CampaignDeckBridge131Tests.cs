#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignDeckBridge131Tests
    {
        const string Chapter="CH018_001", Union="DECK_UNION131";
        string _directory,_path;
        M1RuntimeCoordinator _owner;
        CampaignRuleCatalogAdapter019 _chapters;
        Campaign020RuleCatalogAdapter _steps;
        Campaign023RuleCatalogAdapter _boards;

        [SetUp]
        public void Setup131()
        {
            _directory=Path.Combine(Path.GetTempPath(),"sd_campaign_deck131_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);_path=Path.Combine(_directory,"campaign.json");
            _chapters=new CampaignRuleCatalogAdapter019(CampaignRegistry019.LoadFromResources().Base018);
            _steps=new Campaign020RuleCatalogAdapter(CampaignRegistry020.LoadFromResources());
            _boards=new Campaign023RuleCatalogAdapter(CampaignRegistry023.LoadFromResources());
            _owner=Owner131(_path,Fresh131());
            AssertSuccess(Persist131(_owner,State131(_owner)));
        }

        [TearDown]
        public void Teardown131()
        {
            if(_directory==null||!Directory.Exists(_directory))return;
            var root=Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\','/')+Path.DirectorySeparatorChar;
            Assert.That(Path.GetFullPath(_directory).StartsWith(root+"sd_campaign_deck131_",StringComparison.OrdinalIgnoreCase),Is.True);
            Directory.Delete(_directory,true);
        }

        [Test]
        public void FreshStartMatchesExactCatalogAuthorityChainAndSavesOnce131()
        {
            var source=State131(_owner);var before=File.ReadAllBytes(_path);
            var candidate=Require(new CampaignCommandService019().StartChapter(source,_chapters,Chapter,new[]{Union},true));
            var commands=new CampaignPlayableCommandService020();
            candidate=Require(commands.BeginOperation(candidate,_steps,Chapter));
            candidate=Require(commands.CommitNonBattleStep(candidate,_steps,"SUCCESS"));
            candidate=Require(commands.ApplyStepReceiptExactlyOnce(candidate,_steps));
            candidate=Require(new CampaignWorldGateCommandService023().BeginOperation(candidate,_boards,Chapter,
                new[]{Union},_steps,HeroMaster300CreatorRegistry087.Load().Source));
            var expected=Owner131(Path.Combine(_directory,"expected.json"),source);
            AssertSuccess(Persist131(expected,candidate));
            var changes=0;_owner.Changed+=()=>changes++;
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));
            Assert.That(CanonicalJson.Serialize(State131(_owner)),Is.EqualTo(CanonicalJson.Serialize(State131(expected))));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(_path+".bak"),"Exactly one replacement preserves the original backup.");
            Assert.That(changes,Is.EqualTo(1));
            var operation=Progress131(_owner).Playable020.ActiveOperation;
            Assert.That(operation.CurrentStepIndex,Is.EqualTo(1));
            Assert.That(operation.CompletedStepIds,Has.Count.EqualTo(1));
            Assert.That(operation.PendingReceipt,Is.Null);
            Assert.That(Gate131(_owner).ActiveOperation.DefinitionId,Is.EqualTo(Chapter));
            var saved=Snapshot131();
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));
            AssertSnapshot131(saved);Assert.That(changes,Is.EqualTo(1));
            Assert.That(CanonicalJson.Sha256Hex(source),Is.EqualTo(CanonicalJson.Sha256Hex(new AtomicSaveStore().ReadWithRecovery(_path+".bak").Value.CampaignState)));
        }

        [Test]
        public void ExistingPendingBriefingAndWrongChapterNeverAdvanceOrSave131()
        {
            AssertSuccess(_owner.StartPlayableChapter020(Chapter));
            AssertSuccess(_owner.CommitPlayableStep020("SUCCESS"));
            var pending=Progress131(_owner).Playable020.ActiveOperation.PendingReceipt;
            Assert.That(pending,Is.Not.Null);
            var saved=Snapshot131();var state=State131(_owner);
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));
            Assert.That(ReferenceEquals(State131(_owner),state),Is.True);AssertSnapshot131(saved);
            Assert.That(Progress131(_owner).Playable020.ActiveOperation.PendingReceipt.ReceiptId,Is.EqualTo(pending.ReceiptId));
            Assert.That(_owner.StartOrResumeCampaignDeck131("CH018_002").Succeeded,Is.False);
            AssertSnapshot131(saved);
            // A retained callback cannot change owner/chapter or bypass the saved result.
            Assert.That(_owner.StartOrResumeCampaignDeck131(null).Succeeded,Is.False);AssertSnapshot131(saved);
            Assert.That(Gate131(_owner).ActiveOperation,Is.Null);
        }

        [Test]
        public void ExistingWorldBoardBeginsOnceAndPendingBoardResultRemainsUntouched131()
        {
            AssertSuccess(_owner.StartPlayableChapter020(Chapter));
            AssertSuccess(_owner.CommitPlayableStep020("SUCCESS"));
            AssertSuccess(_owner.ApplyPlayableStep020());
            var before=File.ReadAllBytes(_path);
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));
            CollectionAssert.AreEqual(before,File.ReadAllBytes(_path+".bak"));
            var active=Gate131(_owner).ActiveOperation;
            Assert.That(_boards.TryGetBoard(Chapter,out var board),Is.True);
            var start=board.Nodes.Single(value=>value.NodeId==active.CurrentNodeId);
            Assert.That(start.RequiresCertifiedBattle,Is.False);
            AssertSuccess(_owner.CommitWorldGateChoice023(start.ChoiceIds.First()));
            var receipt=Gate131(_owner).ActiveOperation.PendingReceipt;
            Assert.That(receipt,Is.Not.Null);
            var saved=Snapshot131();
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));AssertSnapshot131(saved);
            Assert.That(Gate131(_owner).ActiveOperation.OperationId,Is.EqualTo(active.OperationId));
            Assert.That(Gate131(_owner).ActiveOperation.PendingReceipt.ReceiptId,Is.EqualTo(receipt.ReceiptId));
        }

        [Test]
        public void FailedSingleWriteLeavesNoPartialStartAndRetryReloadsSameDeck131()
        {
            var saved=Snapshot131();var state=State131(_owner);var changes=0;_owner.Changed+=()=>changes++;
            Directory.CreateDirectory(_path+".tmp");
            LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex(@"(?s)SAVE_WRITE_FAILED109\s+System\.(UnauthorizedAccessException|IO\.IOException).*"));
            var failed=_owner.StartOrResumeCampaignDeck131(Chapter);
            Assert.That(failed.Succeeded,Is.False);AssertSnapshot131(saved);
            Assert.That(ReferenceEquals(State131(_owner),state),Is.True);Assert.That(changes,Is.Zero);
            Directory.Delete(_path+".tmp");
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));Assert.That(changes,Is.EqualTo(1));
            var loaded=new AtomicSaveStore().ReadWithRecovery(_path);
            Assert.That(loaded.IsSuccess,Is.True,string.Join(";",loaded.Errors));
            Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState),Is.EqualTo(CanonicalJson.Sha256Hex(State131(_owner))));
            var committed=Snapshot131();AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));AssertSnapshot131(committed);
        }

        [Test]
        public void RequiredWorldTravelPausesAtSavedBoardBoundaryWithoutSpendingSupplies131()
        {
            // Synthetic unlocked-world fixture, not an earned Campaign playthrough.
            // The travel itself and every chapter/board transition use actual authorities.
            var source=State131(_owner);var city=source.Guild.GuildCity;var progress=city.Strategic017H.Campaign019;
            var foreign=CampaignRegistry023.LoadFromResources().Standing.Keys.First(value=>value!="SKYHOME");
            progress=progress.With(unlockedWorldIds:progress.UnlockedWorldIds.Concat(new[]{foreign}).Distinct().ToArray());
            var changed=source.With(source.Guild.WithGuildCity(city.With(strategic017H:city.Strategic017H.With(
                campaign019:progress,replaceCampaign019:true),replaceStrategic017H:true)),source.OpeningFlow);
            typeof(M1RuntimeCoordinator).GetField("_campaign",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(_owner,changed);
            AssertSuccess(Persist131(_owner,changed));
            AssertSuccess(_owner.TravelWorldGate023(foreign));
            var supplies=Gate131(_owner).TravelSupplies;
            var result=_owner.StartOrResumeCampaignDeck131(Chapter);AssertSuccess(result);
            Assert.That(result.Message,Does.Contain("Travel"));
            Assert.That(Progress131(_owner).Playable020.ActiveOperation.CurrentStepIndex,Is.EqualTo(1));
            Assert.That(Gate131(_owner).ActiveOperation,Is.Null);
            Assert.That(Gate131(_owner).CurrentWorldId,Is.EqualTo(foreign));Assert.That(Gate131(_owner).TravelSupplies,Is.EqualTo(supplies));
            var saved=Snapshot131();AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));AssertSnapshot131(saved);
            AssertSuccess(_owner.TravelWorldGate023("SKYHOME"));
            AssertSuccess(_owner.StartOrResumeCampaignDeck131(Chapter));
            Assert.That(Gate131(_owner).ActiveOperation.DefinitionId,Is.EqualTo(Chapter));
        }

        static CampaignState Fresh131()
        {
            // Fresh synthetic roster/profile, following existing CampaignRuntime019 fixtures.
            // No story completion flags, rewards, operation grants or receipts are injected.
            var recruit=new RecruitState("DECK_RECRUIT131",100,100,20,20);
            var union=new UnionState(Union,"Deck test Union",UnionKind.Normal,recruit.RecruitId,
                new[]{recruit.RecruitId},"FORMATION_LINE","DOCTRINE_BALANCED",20,8000);
            return new CampaignState("00000000-0000-0000-0000-000000000131",131,"1.0",
                ModeRuleSnapshot.StandardDefaults(),new GuildState("GUILD_DECK131",1000,new[]{recruit},new[]{union}),
                new NewGuildProfileState("Synthetic Deck131",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),
                new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"synthetic_deck131"));
        }
        static M1RuntimeCoordinator Owner131(string path,CampaignState source)
        {
            var owner=new M1RuntimeCoordinator(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"),path);
            typeof(M1RuntimeCoordinator).GetField("_campaign",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,source);
            return owner;
        }
        static CampaignState State131(M1RuntimeCoordinator owner)=>(CampaignState)typeof(M1RuntimeCoordinator).GetField("_campaign",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        static CampaignProgressState019 Progress131(M1RuntimeCoordinator owner)=>State131(owner).Guild.GuildCity.Strategic017H.Campaign019;
        static WorldGateRuntimeState023 Gate131(M1RuntimeCoordinator owner)=>Progress131(owner).Playable020.WorldGate023;
        static M1CommandResult Persist131(M1RuntimeCoordinator owner,CampaignState state)=>(M1CommandResult)typeof(M1RuntimeCoordinator)
            .GetMethod("ApplyAndPersist",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(owner,new object[]{Result<CampaignState>.Success(state),false,"Synthetic fixture saved."});
        static CampaignState Require(Result<CampaignState> result){Assert.That(result.IsSuccess,Is.True,string.Join(";",result.Errors));return result.Value;}
        static void AssertSuccess(M1CommandResult result)=>Assert.That(result.Succeeded,Is.True,result.Message);
        byte[][] Snapshot131()=>new[]{File.ReadAllBytes(_path),File.Exists(_path+".bak")?File.ReadAllBytes(_path+".bak"):null};
        void AssertSnapshot131(byte[][] snapshot)
        {CollectionAssert.AreEqual(snapshot[0],File.ReadAllBytes(_path));if(snapshot[1]==null)Assert.That(File.Exists(_path+".bak"),Is.False);else CollectionAssert.AreEqual(snapshot[1],File.ReadAllBytes(_path+".bak"));}
    }
}
#endif
