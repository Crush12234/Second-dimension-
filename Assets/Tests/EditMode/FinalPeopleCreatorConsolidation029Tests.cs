using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.PeopleBonds026;
using SecondDimension.Gameplay.RecruitChronicles025;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.PeopleBonds026;
using SecondDimension.Presentation.RecruitChronicles025;
using SecondDimension.Presentation.Release029;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FinalPeopleCreatorConsolidation029Tests
    {
        [Test] public void RegistriesExposeCompletePeopleAndCreatorAuthority()
        {
            var chronicles=RecruitChronicleRegistry025.LoadFromResources(); var bonds=PeopleBondRegistry026.Load(); var health=PeopleCreatorReadinessService029.BuildSnapshot();
            Assert.That(health.IsReady,Is.True,health.Error);Assert.That(chronicles.Profiles.Count,Is.EqualTo(300));Assert.That(chronicles.Boards.Count,Is.EqualTo(360));
            Assert.That(chronicles.Boards.Values.Sum(x=>x.nodes.Length),Is.EqualTo(3600));Assert.That(chronicles.HallEvents.Count,Is.EqualTo(112));Assert.That(bonds.LinkArts.Count,Is.EqualTo(36));
            Assert.That(health.Manifest.creatorCodes,Is.EqualTo(300));Assert.That(health.Manifest.creatorRooms,Is.EqualTo(133));Assert.That(health.Manifest.saveFormatVersion,Is.EqualTo(11));
        }

        [Test] public void PermanentPeopleLawsAreEnforcedAcrossAllContent()
        {
            var chronicles=RecruitChronicleRegistry025.LoadFromResources();
            Assert.That(chronicles.Profiles.Values.All(x=>x.permanentRecruit&&!x.canInvoluntarilyLeave&&!x.relationshipScenesCostOperation&&!x.relationshipScenesExpire),Is.True);
            Assert.That(chronicles.Boards.Values.All(x=>x.deterministic&&x.reloadCannotReroll&&x.failureRecoverable&&!x.canCauseDeparture),Is.True);
            Assert.That(chronicles.HallEvents.Values.All(x=>x.operationCost==0&&!x.expires&&!x.canCauseDeparture&&x.deferWithoutPenalty),Is.True);
        }

        [Test] public void CampaignPlayableStateKeepsCreatorAndPeopleBranchesTogether()
        {
            var campaign=CreatePeopleCampaign(); var playable=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;
            Assert.That(playable.CreatorAccess028,Is.Not.Null);Assert.That(playable.RecruitChronicles025,Is.Not.Null);Assert.That(playable.PeopleBonds026,Is.Not.Null);
            var roundTrip=JsonConvert.DeserializeObject<CampaignState>(JsonConvert.SerializeObject(campaign)); var restored=roundTrip.Guild.GuildCity.Strategic017H.Campaign019.Playable020;
            Assert.That(restored.CreatorAccess028,Is.Not.Null);Assert.That(restored.RecruitChronicles025.ContentAuthorityVersion,Is.EqualTo(RecruitChronicleState025.ContentVersion));Assert.That(restored.PeopleBonds026.ContentAuthorityVersion,Is.EqualTo(PeopleBondState026.ContentVersion));
        }

        [Test] public void RelationshipMemoryIsExactOnceAndCannotConsumeOperation()
        {
            var campaign=CreatePeopleCampaign();var registry=RecruitChronicleRegistry025.LoadFromResources();var memory=registry.Memories.Values.First();var before=campaign.Guild.GuildCity.OperationOrdinal;var service=new PeopleBondService026();
            var first=Require(service.ApplyMemory(campaign,memory,"SIGREC_MAREN_HOLT","SIGREC_ODELIA_FEN","RCPT_TEST_029","UNIT_TEST"));
            var second=Require(service.ApplyMemory(first,memory,"SIGREC_MAREN_HOLT","SIGREC_ODELIA_FEN","RCPT_TEST_029","UNIT_TEST"));
            var bonds=second.Guild.GuildCity.Strategic017H.Campaign019.Playable020.PeopleBonds026;
            Assert.That(second.Guild.GuildCity.OperationOrdinal,Is.EqualTo(before));Assert.That(bonds.Pairs.Count,Is.EqualTo(1));Assert.That(bonds.Pairs[0].SharedMemoryCount,Is.EqualTo(1));Assert.That(bonds.AppliedReceiptIds.Count,Is.EqualTo(1));
        }

        [Test] public void AuthoredQuestNodeMemoryIsReceiptBoundAndExactOnce()
        {
            var campaign=CreatePeopleCampaign();var registry=RecruitChronicleRegistry025.LoadFromResources();var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);var bonds=new PeopleBondService026();campaign=Require(commands.StartPersonalQuest(campaign,board));
            var destination=RecruitChronicleRules025.FindNode(board,RecruitChronicleRules025.FindNode(board,board.entryNodeId).nextNodeIds[0]);
            var questReceipt=RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed,board.boardId,destination.nodeId,board.recruitId);
            campaign=Require(commands.AdvancePersonalQuest(campaign,board,destination.nodeId,questReceipt));
            var wrongMemory=registry.Memories.Values.First(x=>!StringComparer.Ordinal.Equals(x.memoryId,destination.rewardMemoryId));
            var mismatched=bonds.ApplyPersonalQuestNodeMemory(campaign,board,destination,wrongMemory,questReceipt);
            Assert.That(mismatched.IsSuccess,Is.False);Assert.That(mismatched.Errors,Does.Contain("PEOPLE026_QUEST_NODE_MEMORY_MISMATCH"));
            var first=Require(bonds.ApplyPersonalQuestNodeMemory(campaign,board,destination,registry.Memory(destination.rewardMemoryId),questReceipt));
            var second=Require(bonds.ApplyPersonalQuestNodeMemory(first,board,destination,registry.Memory(destination.rewardMemoryId),questReceipt));
            var playable=second.Guild.GuildCity.Strategic017H.Campaign019.Playable020;var pair=playable.PeopleBonds026.Pairs.Single();var memory=playable.RecruitChronicles025.RelationshipMemories.Single();
            var expectedMemoryReceipt=RecruitChronicleRules025.DeterministicReceiptId(campaign.CampaignSeed,questReceipt,destination.rewardMemoryId,board.recruitId);
            Assert.That(pair.PairId,Is.EqualTo(BondPairState026.CanonicalPairId("SIGREC_GARA_REDTAIL","SIGREC_MAREN_HOLT")));
            Assert.That(pair.SharedMemoryCount,Is.EqualTo(1));Assert.That(pair.AppliedMemoryReceiptIds.Count,Is.EqualTo(1));Assert.That(playable.PeopleBonds026.AppliedReceiptIds.Count,Is.EqualTo(1));
            Assert.That(memory.ReceiptId,Is.EqualTo(expectedMemoryReceipt));Assert.That(memory.MemoryId,Is.EqualTo(destination.rewardMemoryId));Assert.That(memory.SourceId,Is.EqualTo("PERSONAL_QUEST_NODE:"+questReceipt));
        }

        [Test] public void UnionBondDoctrineRejectsPrematureSelection()
        {
            var campaign=CreatePeopleCampaign();var doctrine=PeopleBondRegistry026.Load().Doctrines["BOND_DOCTRINE026_00"];
            var result=new PeopleBondService026().SetUnionDoctrine(campaign,"U1",doctrine);
            Assert.That(result.IsSuccess,Is.False);Assert.That(result.Errors,Does.Contain("PEOPLE026_DOCTRINE_BOND_TIER_REQUIRED"));
        }

        [Test] public void AuthoredQuestMemoriesEarnEligibleUnionBondDoctrine()
        {
            var campaign=CreatePeopleCampaign();var registry=RecruitChronicleRegistry025.LoadFromResources();var board=registry.Boards["PQ025_SIGREC_MAREN_HOLT_01"];
            var commands=new RecruitChronicleCommandService025(registry);var bonds=new PeopleBondService026();campaign=Require(commands.StartPersonalQuest(campaign,board));
            for(var i=0;i<2;i++)
            {
                var quest=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025.PersonalQuests.Single(x=>x.BoardId==board.boardId);
                var destination=RecruitChronicleRules025.FindNode(board,RecruitChronicleRules025.FindNode(board,quest.CurrentNodeId).nextNodeIds[0]);
                var receipt=RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed,board.boardId,destination.nodeId,board.recruitId);
                campaign=Require(commands.AdvancePersonalQuest(campaign,board,destination.nodeId,receipt));
                campaign=Require(bonds.ApplyPersonalQuestNodeMemory(campaign,board,destination,registry.Memory(destination.rewardMemoryId),receipt));
            }
            var people=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.PeopleBonds026;var strongest=PeopleBondService026.StrongestPair(people,campaign.Guild.Unions.Single(x=>x.UnionId=="U1").MemberRecruitIds);
            Assert.That(strongest,Is.Not.Null);Assert.That(PeopleBondService026.TierRank(strongest.TierId),Is.GreaterThanOrEqualTo(1));Assert.That(strongest.SharedMemoryCount,Is.EqualTo(2));
            var doctrine=PeopleBondRegistry026.Load().Doctrines["BOND_DOCTRINE026_00"];campaign=Require(bonds.SetUnionDoctrine(campaign,"U1",doctrine));
            people=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.PeopleBonds026;
            Assert.That(people.Unions.Single(x=>x.UnionId=="U1").DoctrineId,Is.EqualTo(doctrine.doctrineId));
        }

        [Test] public void HallSceneDeferralPersistsInSaveV11AndIsExactOnce()
        {
            var path=Path.Combine(Path.GetTempPath(),"second_dimension_hall_defer_029_"+Guid.NewGuid().ToString("N")+".json");
            try
            {
                var campaign=CreatePeopleCampaign();var beforeOperation=campaign.Guild.GuildCity.OperationOrdinal;var registry=RecruitChronicleRegistry025.LoadFromResources();
                var scene=registry.HallEvents.Values.First(x=>x.operationCost==0&&!x.expires&&!x.canCauseDeparture&&x.deferWithoutPenalty);var service=new RecruitChronicleCommandService025(registry);
                var first=Require(service.DeferHallScene(campaign,scene));var second=Require(service.DeferHallScene(first,scene));var receipt=RecruitChronicleRules025.HallSceneDeferralReceiptId(campaign.CampaignSeed,scene.sceneId);
                var chronicles=second.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
                Assert.That(second.Guild.GuildCity.OperationOrdinal,Is.EqualTo(beforeOperation));Assert.That(chronicles.AppliedReceiptIds.Count(x=>x==receipt),Is.EqualTo(1));Assert.That(chronicles.ViewedHallSceneIds,Does.Not.Contain(scene.sceneId));
                new AtomicSaveStore().Write(path,SaveEnvelopeV1.Create(second,new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)));var loaded=new AtomicSaveStore().ReadWithRecovery(path);
                Assert.That(loaded.IsSuccess,Is.True,string.Join("\n",loaded.Errors));Assert.That(loaded.Value.SaveFormatVersion,Is.EqualTo(11));
                var replayed=Require(service.DeferHallScene(loaded.Value.CampaignState,scene));chronicles=replayed.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;
                Assert.That(RecruitChronicleRules025.HasDeferredHallScene(chronicles,replayed.CampaignSeed,scene.sceneId),Is.True);Assert.That(chronicles.AppliedReceiptIds.Count(x=>x==receipt),Is.EqualTo(1));
            }
            finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");if(File.Exists(path+".tmp"))File.Delete(path+".tmp");}
        }

        [Test] public void HallSceneDeferralRejectsEveryTamperedDeferralLaw()
        {
            var campaign=CreatePeopleCampaign();var registry=RecruitChronicleRegistry025.LoadFromResources();var scene=registry.HallEvents.Values.First(x=>x.operationCost==0&&!x.expires&&!x.canCauseDeparture&&x.deferWithoutPenalty);var service=new RecruitChronicleCommandService025(registry);
            var tampered=new[]{CopyHallScene(scene,operationCost:1),CopyHallScene(scene,expires:true),CopyHallScene(scene,canCauseDeparture:true),CopyHallScene(scene,deferWithoutPenalty:false)};
            foreach(var candidate in tampered)
            {
                var result=service.DeferHallScene(campaign,candidate);Assert.That(result.IsSuccess,Is.False);Assert.That(result.Errors,Does.Contain("CHRONICLE025_SCENE_DEFERRAL_LAW_VIOLATION"));
            }
            var chronicles=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025;Assert.That(chronicles.AppliedReceiptIds,Is.Empty);
        }

        [Test] public void PersonalQuestCommitIsDeterministicAndBattleNodeUsesExistingEncounterBridge()
        {
            var campaign=CreatePeopleCampaign();var registry=RecruitChronicleRegistry025.LoadFromResources();var board=registry.Boards.Values.First(x=>x.recruitId=="SIGREC_MAREN_HOLT");var commands=new RecruitChronicleCommandService025(registry);
            campaign=Require(commands.StartPersonalQuest(campaign,board));var q=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025.PersonalQuests.First(x=>x.BoardId==board.boardId);
            while(!StringComparer.Ordinal.Equals(RecruitChronicleRules025.FindNode(board,q.CurrentNodeId).kind,"BATTLE"))
            {var node=RecruitChronicleRules025.FindNode(board,q.CurrentNodeId);var destination=node.nextNodeIds[0];var receipt=RecruitChronicleRules025.PersonalQuestMoveReceiptId(campaign.CampaignSeed,board.boardId,destination,board.recruitId);campaign=Require(commands.AdvancePersonalQuest(campaign,board,destination,receipt));q=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.RecruitChronicles025.PersonalQuests.First(x=>x.BoardId==board.boardId);}
            var battle=new RecruitChronicleBattleService025(registry);var first=Require(battle.CommitEncounter(campaign,board,new[]{"U1"}));var second=Require(battle.CommitEncounter(first,board,new[]{"U1"}));
            Assert.That(first.Guild.GuildCity.PendingEncounter.RequestId,Is.EqualTo(second.Guild.GuildCity.PendingEncounter.RequestId));Assert.That(first.Guild.GuildCity.PendingEncounter.ContractId,Does.StartWith(RecruitChronicleBattleService025.ContractPrefix));
        }

        [Test] public void EarnedBondProducesCompleteForecastLinkArtAndOneRoundTrigger()
        {
            var campaign=CreateBattleCampaign();var playable=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020;
            var pair=new BondPairState026(BondPairState026.CanonicalPairId("RECRUIT_0","RECRUIT_1"),"RECRUIT_0","RECRUIT_1",95,90,90,20,"BOND_TIER026_5",Array.Empty<string>());
            var bondState=new PeopleBondState026(PeopleBondState026.ContentVersion,new[]{pair},Array.Empty<UnionBondRuntimeState026>(),Array.Empty<string>(),"test_bond_ready");
            playable=playable.With(peopleBonds026:bondState,replacePeopleBonds026:true);campaign=WithPlayable(campaign,playable);
            var content=M2CombatContent.LoadFromDirectory(Path.Combine(Application.streamingAssetsPath,"Authority","CONTENT"));var service=new M2BattleCommandService();campaign=Require(service.StartTutorialBattle(campaign,content));
            var link=campaign.Battle.CommittedForecasts.FirstOrDefault(x=>x.CommandName.StartsWith("LINK ART",StringComparison.Ordinal));Assert.That(link,Is.Not.Null,"A high-tier bonded five-member Union should receive one complete Link Art Forecast.");
            campaign=Require(service.SelectForecast(campaign,link.UnionId,link.ForecastId));foreach(var union in campaign.Battle.PlayerUnions.Where(x=>!x.IsDefeated&&!x.Retreated&&x.UnionId!=link.UnionId)){var f=campaign.Battle.CommittedForecasts.First(x=>x.UnionId==union.UnionId);campaign=Require(service.SelectForecast(campaign,union.UnionId,f.ForecastId));}
            campaign=Require(service.ConfirmRound(campaign,content));Assert.That(campaign.Battle.EventLog.Any(x=>x.EventType=="LINK_ART_TRIGGERED"),Is.True);var after=campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.PeopleBonds026;Assert.That(after.Unions.Sum(x=>x.LinkArtTriggerCount),Is.EqualTo(1));
        }

        [Test] public void SaveV11RoundTripRetainsPeopleAndCreatorState()
        {
            var path=Path.Combine(Path.GetTempPath(),"second_dimension_029_"+Guid.NewGuid().ToString("N")+".json");try{var campaign=CreatePeopleCampaign();new AtomicSaveStore().Write(path,SaveEnvelopeV1.Create(campaign,new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)));var loaded=new AtomicSaveStore().ReadWithRecovery(path);Assert.That(loaded.IsSuccess,Is.True,string.Join("\n",loaded.Errors));Assert.That(loaded.Value.SaveFormatVersion,Is.EqualTo(11));var p=loaded.Value.CampaignState.Guild.GuildCity.Strategic017H.Campaign019.Playable020;Assert.That(p.CreatorAccess028,Is.Not.Null);Assert.That(p.RecruitChronicles025,Is.Not.Null);Assert.That(p.PeopleBonds026,Is.Not.Null);}finally{if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");if(File.Exists(path+".tmp"))File.Delete(path+".tmp");}
        }

        static CampaignState CreatePeopleCampaign()
        {
            var ids=new[]{"SIGREC_MAREN_HOLT","SIGREC_ODELIA_FEN","SIGREC_GARA_REDTAIL","SIGREC_DAEVEN_FELLSTAR","SIGREC_TAZREN_WARMASK","SIGREC_JAZZI_WIREWICK"};var recruits=ids.Select(x=>new RecruitState(x,120,120,30,30)).ToArray();var unions=new[]{new UnionState("U1","First Union",UnionKind.Normal,ids[0],ids.Take(3).ToArray(),"FORMATION_LINE","DOCTRINE_BALANCED",30,8500),new UnionState("U2","Second Union",UnionKind.Normal,ids[3],ids.Skip(3).ToArray(),"FORMATION_LINE","DOCTRINE_BALANCED",30,8500)};return CompleteCampaign(recruits,unions,29029L);
        }
        static CampaignState CreateBattleCampaign()
        {
            var classes=new[]{"CLASS_TEND_GUARDIAN","CLASS_TEND_WARRIOR","CLASS_TEND_PRIEST","CLASS_TEND_MAGE","CLASS_TEND_ROGUE"};var tags=new[]{new[]{"SHIELD","SWORD","WEAPON"},new[]{"SWORD","WEAPON"},new[]{"STAFF","HEALING"},new[]{"WAND","FOCUS_TOOL"},new[]{"DAGGER","WEAPON"}};var recruits=new List<RecruitState>();
            for(var i=0;i<classes.Length;i++){var item=new EquipmentItemState("ITEM_"+i,"EQ_"+i,"Weapon "+i,new[]{EquipmentSlotIds.MainHand},tags[i],"QUALITY_STANDARD",10000,false);recruits.Add(new RecruitState("RECRUIT_"+i,140,140,35,35,"Recruit "+i,RecruitOriginKind.Procedural,string.Empty,"HUMAN","SKYHOME",classes[i],"Observed",6000,RecruitAuthorityKind.Normal,string.Empty,string.Empty,new EquipmentLoadoutState(new[]{new EquipmentSlotAssignmentState(EquipmentSlotIds.MainHand,item)}),true));}
            var union=new UnionState("UNION_LINK_029","Bonded Union",UnionKind.Normal,"RECRUIT_0",recruits.Select(x=>x.RecruitId).ToArray(),"FORMATION_SHIELD_WALL","DOCTRINE_BALANCED",30,9000);return CompleteCampaign(recruits.ToArray(),new[]{union},29030L);
        }
        static CampaignState CompleteCampaign(IReadOnlyList<RecruitState> recruits,IReadOnlyList<UnionState> unions,long seed){var guild=new GuildState("GUILD_029",0,recruits,unions);var profile=new NewGuildProfileState("Tester",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false);var flow=new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,false,"complete");return new CampaignState("00000000-0000-0000-0000-000000000029",seed,"1.0",ModeRuleSnapshot.StandardDefaults(),guild,profile,flow);}
        static HallSocialEventDefinition025 CopyHallScene(HallSocialEventDefinition025 source,int? operationCost=null,bool? expires=null,bool? canCauseDeparture=null,bool? deferWithoutPenalty=null)=>new HallSocialEventDefinition025{sceneId=source.sceneId,displayName=source.displayName,participantRecruitIds=source.participantRecruitIds,triggerMemoryId=source.triggerMemoryId,locationTag=source.locationTag,summary=source.summary,operationCost=operationCost??source.operationCost,expires=expires??source.expires,canCauseDeparture=canCauseDeparture??source.canCauseDeparture,deferWithoutPenalty=deferWithoutPenalty??source.deferWithoutPenalty};
        static CampaignState WithPlayable(CampaignState campaign,CampaignPlayableState020 playable){var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var progress=strategic.Campaign019.With(playable020:playable,replacePlayable020:true);strategic=strategic.With(campaign019:progress,replaceCampaign019:true);city=city.With(strategic017H:strategic,replaceStrategic017H:true);return campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow);}
        static CampaignState Require(Result<CampaignState> result){Assert.That(result.IsSuccess,Is.True,string.Join("\n",result.Errors));return result.Value;}
    }
}
