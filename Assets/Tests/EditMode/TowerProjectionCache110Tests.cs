#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign022;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Campaign022;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    // Existing synthetic combat helper, real Tower authority and save lifecycle.
    public sealed class TowerProjectionCache110Tests
    {
        CampaignState _completedFirstFloor;
        string _directory;
        string _savePath;
        static string ContentRoot => Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT");

        [OneTimeSetUp]
        public void PrepareVerifiedFloor110()
        {
            var fixture=new TowerHeroRewards094Tests();
            fixture.SetUp();
            var opening=new M1OpeningFlowTests();
            opening.SetUp();
            var fresh=(CampaignState)typeof(M1OpeningFlowTests)
                .GetMethod("BuildCompletedOpening",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(opening,null);
            Assert.That(fresh.OpeningFlow.Stage,Is.EqualTo(OpeningStage.Complete));
            Assert.That(fresh.OpeningFlow.ApplicantBoard,Is.Not.Null);
            Assert.That(fresh.OpeningFlow.UnionBuilderCompleted,Is.True);
            _completedFirstFloor=(CampaignState)typeof(TowerHeroRewards094Tests)
                .GetMethod("CompleteNewFloor",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(fixture,new object[]{fresh,1});
        }

        [SetUp]
        public void CreateIsolatedSave110()
        {
            _directory=Path.Combine(Path.GetTempPath(),"SecondDimensionTowerProjection110_"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _savePath=Path.Combine(_directory,"projection.json");
            new AtomicSaveStore().Write(_savePath,SaveEnvelopeV1.Create(_completedFirstFloor,DateTime.UtcNow));
        }

        [TearDown]
        public void RemoveIsolatedSave110()
        {
            if(string.IsNullOrEmpty(_directory)||!Directory.Exists(_directory))return;
            var parent=Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            var full=Path.GetFullPath(_directory);
            Assert.That(full.StartsWith(parent,StringComparison.OrdinalIgnoreCase),Is.True);
            Assert.That(Path.GetFileName(full),Does.StartWith("SecondDimensionTowerProjection110_"));
            Directory.Delete(full,true);
        }

        [Test]
        public void RepeatedProjection110ReusesPrivateAuthorityAndReturnsUnpoisonedViews()
        {
            var coordinator=new M1RuntimeCoordinator(ContentRoot,_savePath);
            var campaignHash=CanonicalJson.Sha256Hex(Campaign(coordinator));
            var timer=Stopwatch.StartNew();
            var first=coordinator.CampaignProgression022;
            timer.Stop();
            var coldMs=timer.Elapsed.TotalMilliseconds;
            AssertValid(first);
            var cached=Cache(coordinator);
            Assert.That(cached,Is.Not.Null);
            var expected=CanonicalJson.Serialize(first);
            first.TowerFloorNumber=int.MaxValue;
            first.HighestClearedTowerFloor=int.MaxValue;
            first.TowerAuthorityError094="POISONED_ERROR";
            first.TowerLastHeroRewardSummary094="POISONED_REWARD";
            first.GreatCovenantGateEarned=!first.GreatCovenantGateEarned;
            first.FloorStates[0].ClearCount=int.MaxValue;
            first.TowerOperationChoices[0].DisplayName="POISONED_OPERATION";
            if(first.TowerTrackLabels.Count>0)
                ((IList<string>)first.TowerTrackLabels)[0]="POISONED_TRACK";
            if(first.TowerRewardMaterialIds108.Count>0)
                ((IList<string>)first.TowerRewardMaterialIds108)[0]="POISONED_MATERIAL";
            timer.Restart();
            var second=coordinator.CampaignProgression022;
            timer.Stop();
            Assert.That(Cache(coordinator),Is.SameAs(cached));
            Assert.That(second,Is.Not.SameAs(first));
            Assert.That(second.FloorStates[0],Is.Not.SameAs(first.FloorStates[0]));
            Assert.That(CanonicalJson.Serialize(second),Is.EqualTo(expected));
            Assert.That(CanonicalJson.Sha256Hex(Campaign(coordinator)),Is.EqualTo(campaignHash));
            TestContext.Progress.WriteLine("TOWER_PROJECTION110 coldMs="+coldMs.ToString("F3")+
                " warmMs="+timer.Elapsed.TotalMilliseconds.ToString("F3"));
        }

        [Test]
        public void RealEquipmentCommandAndReload110InvalidatePrivateAuthority()
        {
            var coordinator=new M1RuntimeCoordinator(ContentRoot,_savePath);
            AssertValid(coordinator.CampaignProgression022);
            var before=Campaign(coordinator);
            var beforeCache=Cache(coordinator);
            var equipped=before.Guild.Recruits.First(recruit=>
                ProtectedActorPolicy.CanUseNormalEquipment(recruit)&&
                recruit.Equipment.Assignments.Any(slot=>slot?.Item!=null));
            var assignment=equipped.Equipment.Assignments.First(slot=>slot?.Item!=null);
            var expectedLock=!assignment.Item.PlayerLocked;
            var command=coordinator.SetEquipmentLock(equipped.RecruitId,
                assignment.SlotId,expectedLock);
            Assert.That(command.Succeeded,Is.True,command.Message);
            Assert.That(Campaign(coordinator),Is.Not.SameAs(before));
            var changed=coordinator.CampaignProgression022;
            AssertValid(changed);
            Assert.That(Campaign(coordinator).Guild.Recruits.Single(recruit=>
                recruit.RecruitId==equipped.RecruitId).Equipment.Find(assignment.SlotId)
                .Item.PlayerLocked,Is.EqualTo(expectedLock));
            Assert.That(Cache(coordinator),Is.Not.SameAs(beforeCache));
            var changedCache=Cache(coordinator);
            var expected=CanonicalJson.Serialize(changed);

            var loaded=new AtomicSaveStore().ReadWithRecovery(_savePath);
            Assert.That(loaded.IsSuccess,Is.True,string.Join("; ",loaded.Errors));
            Assert.That(CanonicalJson.Sha256Hex(loaded.Value.CampaignState),
                Is.EqualTo(CanonicalJson.Sha256Hex(Campaign(coordinator))));
            // Exercise replacing the in-memory snapshot with a separately
            // validated reload, including the identical-value/new-reference case.
            CampaignField.SetValue(coordinator,loaded.Value.CampaignState);
            Assert.That(CanonicalJson.Serialize(coordinator.CampaignProgression022),Is.EqualTo(expected));
            Assert.That(Cache(coordinator),Is.Not.SameAs(changedCache));
            var reloaded=new M1RuntimeCoordinator(ContentRoot,_savePath);
            AssertValid(reloaded.CampaignProgression022);
            Assert.That(reloaded.CampaignProgression022.ActiveAbyssOperationId,
                Is.EqualTo(changed.ActiveAbyssOperationId));
            Assert.That(Cache(reloaded),Is.Not.SameAs(Cache(coordinator)));
        }

        [Test]
        public void ChangedInvalidAuthority110CannotReuseValidResultAndErrorsSurviveRepeatedReads()
        {
            var coordinator=new M1RuntimeCoordinator(ContentRoot,_savePath);
            AssertValid(coordinator.CampaignProgression022);
            var validCache=Cache(coordinator);
            var valid=Campaign(coordinator);
            var city=valid.Guild.GuildCity;
            var strategic=city.Strategic017H;
            var progress=strategic.Campaign019;
            var playable=progress.Playable020;
            var corrupt=playable.Progression022.With(abyssAuthorityHash:new string('0',64));
            playable=playable.With(progression022:corrupt,replaceProgression022:true);
            progress=progress.With(playable020:playable,replacePlayable020:true);
            strategic=strategic.With(campaign019:progress,replaceCampaign019:true);
            var forged=valid.With(valid.Guild.WithGuildCity(city.With(
                strategic017H:strategic,replaceStrategic017H:true)),valid.OpeningFlow);
            var direct=CampaignProgressionCommandService022.DescribeTowerFloors094(
                forged,CampaignRegistry022.LoadFromResources());
            Assert.That(direct.IsSuccess,Is.False);
            CampaignField.SetValue(coordinator,forged);
            var rejected=coordinator.CampaignProgression022;
            Assert.That(rejected.TowerAuthorityError094,Is.Not.Empty);
            Assert.That(rejected.GreatCovenantGateEarned,Is.False);
            Assert.That(Cache(coordinator),Is.Not.SameAs(validCache));
            var failedCache=Cache(coordinator);
            var expected=CanonicalJson.Serialize(rejected);
            rejected.TowerAuthorityError094=string.Empty;
            rejected.GreatCovenantGateEarned=true;
            Assert.That(CanonicalJson.Serialize(coordinator.CampaignProgression022),Is.EqualTo(expected));
            Assert.That(Cache(coordinator),Is.SameAs(failedCache));
            Assert.That(CampaignProgressionCommandService022.DescribeTowerFloors094(
                forged,CampaignRegistry022.LoadFromResources()).IsSuccess,Is.False,
                "The private presentation cache cannot change gameplay authority checks.");
        }

        static readonly FieldInfo CampaignField=typeof(M1RuntimeCoordinator)
            .GetField("_campaign",BindingFlags.Instance|BindingFlags.NonPublic);
        static CampaignState Campaign(M1RuntimeCoordinator coordinator)=>(CampaignState)CampaignField.GetValue(coordinator);
        static object Cache(M1RuntimeCoordinator coordinator)
        {
            var field=typeof(M1RuntimeCoordinator).GetField("_towerAuthorityProjection110",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(field,Is.Not.Null,"Import the private projection cache before these tests.");
            return field.GetValue(coordinator);
        }
        static void AssertValid(CampaignProgressionPresentationState022 view)
        {
            Assert.That(view.IsAvailable,Is.True,view.Error);
            Assert.That(view.TowerAuthorityError094,Is.Empty);
        }
    }
}
#endif
