#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Campaign023;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Campaign019;
using SecondDimension.Presentation.Campaign020;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CampaignCardFlow132Tests
    {
        const string Union = "FLOW_UNION132";
        CampaignRuleCatalogAdapter019 Chapters() => new CampaignRuleCatalogAdapter019(CampaignRegistry019.LoadFromResources().Base018);
        Campaign020RuleCatalogAdapter Steps() => new Campaign020RuleCatalogAdapter(CampaignRegistry020.LoadFromResources());
        Campaign023RuleCatalogAdapter Boards() => new Campaign023RuleCatalogAdapter(CampaignRegistry023.LoadFromResources());

        [Test]
        public void All82RealCatalogDecksProvideDistinctEncounterAndRouteChoicesWith37AuthoredBattles132()
        {
            var boards=Boards(); var steps=Steps(); var source=Fresh();
            var heroes=HeroMaster300CreatorRegistry087.Load().Source;
            var deckService=new ExpeditionDeckService089();
            var chapters=CampaignRegistry020.LoadFromResources().Blueprints.Keys.OrderBy(id=>id).ToArray();
            Assert.That(chapters,Has.Length.EqualTo(82));
            Assert.That(chapters,Is.EqualTo(Enumerable.Range(1,82).Select(n=>"CH018_"+n.ToString("000")).ToArray()));
            var battleCount=0;
            foreach(var id in chapters)
            {
                Assert.That(steps.TryGetBlueprint(id,out var blueprint),Is.True,id);
                Assert.That(boards.TryGetBoard(id,out var board),Is.True,id);
                Assert.That(board.OperationKind,Is.EqualTo("CHAPTER"),id);
                Assert.That(blueprint.Steps.Count(step=>CampaignAdventureRules084.IsWorldBoardStep084(step)),Is.EqualTo(1),id);
                battleCount+=blueprint.Steps.Count(step=>step.RequiresCertifiedBattle);
                var deck=deckService.Create(source.CampaignSeed,"CATALOG132_"+id,board,
                    source.Guild.Recruits,source.Guild.Inventory,heroes,Array.Empty<string>());
                Assert.That(deck.CurrentRow,Has.Count.EqualTo(3),id);
                Assert.That(deck.CurrentRow.Select(card=>card.CardId).Distinct().Count(),Is.EqualTo(3),id);
                var all=deck.DrawPile.Concat(deck.CurrentRow).ToArray();
                foreach(var node in board.Nodes)
                {
                    Assert.That(node.RequiresCertifiedBattle,Is.False,"Story battle stays in authored020, not a forced single exploration choice: "+id);
                    foreach(var advances in new[]{false,true})
                    {
                        var cards=all.Where(card=>card.NodeId==node.NodeId&&card.AdvancesRoute==advances).ToArray();
                        Assert.That(cards.Length,Is.GreaterThanOrEqualTo(3),id+" "+node.NodeId);
                        Assert.That(cards.Select(card=>card.CardId).Distinct().Count(),Is.EqualTo(cards.Length));
                        Assert.That(cards.Select(card=>card.Category).Distinct().Count(),Is.GreaterThanOrEqualTo(2),
                            "Distinct authored edge/event/reward semantics, not three aliases of one button: "+id+" "+node.NodeId);
                        Assert.That(cards.All(card=>node.ChoiceIds.Contains(card.ChoiceId)),Is.True,id);
                    }
                }
            }
            Assert.That(battleCount,Is.EqualTo(37));
            Assert.That(chapters.Select(CampaignMissionMap132.Number).Distinct().Count(),Is.EqualTo(81));
            Assert.That(CampaignMissionMap132.CompletedCount(chapters.Take(81)),Is.EqualTo(80),"Finale must be earned before mission81 completes.");
            Assert.That(CampaignMissionMap132.CompletedCount(chapters),Is.EqualTo(81));
        }

        [Test, Timeout(240000)]
        public void RealFirstChapterCardsKeepReceiptsAndPrepareEachAuthoredStoryInterruptionAfterReload132()
        {
            var chapters=Chapters();var steps=Steps();var boards=Boards();var source=Fresh();
            var original=CanonicalJson.Serialize(source);
            var command019=new CampaignCommandService019();var command020=new CampaignPlayableCommandService020();
            var command023=new CampaignWorldGateCommandService023();var deck=new ExpeditionDeckCommandService089();
            var flow=new CampaignCardFlow132();
            var candidate=Require(command019.StartChapter(source,chapters,"CH018_001",new[]{Union},true));
            candidate=Require(command020.BeginOperation(candidate,steps,"CH018_001"));
            candidate=Require(command020.CommitNonBattleStep(candidate,steps,"SUCCESS"));
            candidate=Require(command020.ApplyStepReceiptExactlyOnce(candidate,steps));
            candidate=Require(command023.BeginOperation(candidate,boards,"CH018_001",new[]{Union},steps,HeroMaster300CreatorRegistry087.Load().Source));
            candidate=CompleteActualDeck(candidate,boards);
            var completedBoard=candidate;
            candidate=Require(flow.FinishBoardAndPrepare(candidate,steps,chapters,boards));
            Assert.That(Gate(candidate).ActiveOperation,Is.Null);
            Assert.That(Progress(candidate).Playable020.ActiveOperation.PendingReceipt,Is.Not.Null);
            Assert.That(Progress(candidate).Playable020.ActiveOperation.CurrentStepIndex,Is.EqualTo(2));
            Assert.That(flow.FinishBoardAndPrepare(candidate,steps,chapters,boards).IsSuccess,Is.False);
            var identity=Progress(candidate).Playable020.ActiveOperation.PendingReceipt.ReceiptId;
            Assert.That(ReferenceEquals(Require(flow.PrepareNextInterruption(candidate,steps,chapters)),candidate),Is.True);
            candidate=RoundTrip(candidate);
            Assert.That(Progress(candidate).Playable020.ActiveOperation.PendingReceipt.ReceiptId,Is.EqualTo(identity));
            var storyCount=0;
            while(Progress(candidate).Playable020.ActiveOperation.PendingReceipt!=null)
            {
                Assert.That(++storyCount,Is.LessThan(8));
                var before=Progress(candidate).Playable020.ActiveOperation;
                candidate=Require(flow.ApplyStoryAndPrepare(candidate,steps,chapters));
                Assert.That(Progress(candidate).Playable020.ActiveOperation.CurrentStepIndex,Is.EqualTo(before.CurrentStepIndex+1));
                candidate=RoundTrip(candidate);
            }
            Assert.That(storyCount,Is.EqualTo(3));
            Assert.That(Progress(candidate).PendingReceipt,Is.Not.Null);
            candidate=Require(command019.ApplyReceiptExactlyOnce(candidate,chapters,steps));
            candidate=Require(command020.CloseCompletedOperation(candidate,steps));
            Assert.That(Progress(candidate).CompletedChapterIds,Does.Contain("CH018_001"));
            Assert.That(Progress(candidate).Playable020.ActiveOperation,Is.Null);
            var final=CanonicalJson.Sha256Hex(candidate);
            Assert.That(flow.ApplyStoryAndPrepare(candidate,steps,chapters).IsSuccess,Is.False);
            Assert.That(CanonicalJson.Sha256Hex(candidate),Is.EqualTo(final));
            Assert.That(CanonicalJson.Serialize(source),Is.EqualTo(original));
            Assert.That(Gate(completedBoard).ActiveOperation.Status,Is.EqualTo(WorldGateOperationStatus023.ReadyToFinalize));
            // Earn the noncombat second mission, then prepare the first authored
            // fixed battle in CH003. No completed IDs or battle result are supplied.
            foreach(var next in new[]{"CH018_002","CH018_003"})
            {
                candidate=Require(command019.StartChapter(candidate,chapters,next,new[]{Union},true));
                candidate=Require(command020.BeginOperation(candidate,steps,next));
                candidate=Require(command020.CommitNonBattleStep(candidate,steps,"SUCCESS"));
                candidate=Require(command020.ApplyStepReceiptExactlyOnce(candidate,steps));
                candidate=Require(command023.BeginOperation(candidate,boards,next,new[]{Union},steps,HeroMaster300CreatorRegistry087.Load().Source));
                candidate=CompleteActualDeck(candidate,boards);
                candidate=Require(flow.FinishBoardAndPrepare(candidate,steps,chapters,boards));
                var interruptions=0;
                while(Progress(candidate).Playable020.ActiveOperation.PendingReceipt!=null)
                {
                    Assert.That(++interruptions,Is.LessThan(8));
                    candidate=Require(flow.ApplyStoryAndPrepare(candidate,steps,chapters));
                }
                if(next=="CH018_002")
                {
                    Assert.That(Progress(candidate).Playable020.ActiveOperation.Status,Is.EqualTo(CampaignPlayableOperationStatus020.ReadyToFinalize));
                    candidate=Require(command019.ApplyReceiptExactlyOnce(candidate,chapters,steps));
                    candidate=Require(command020.CloseCompletedOperation(candidate,steps));
                    Assert.That(Progress(candidate).CompletedChapterIds,Does.Contain(next));
                }
            }
            Assert.That(Progress(candidate).Playable020.ActiveOperation.Status,Is.EqualTo(CampaignPlayableOperationStatus020.AwaitingBattle));
            var request=candidate.Guild.GuildCity.PendingEncounter;
            Assert.That(request,Is.Not.Null);Assert.That(request.ContractId,Is.EqualTo("CH018_003"));
            Assert.That(Progress(candidate).ActiveOperation.BattleCommitted,Is.True);
            var readyHash=CanonicalJson.Sha256Hex(candidate);
            candidate=RoundTrip(candidate);
            Assert.That(CanonicalJson.Sha256Hex(candidate),Is.EqualTo(readyHash));
            Assert.That(ReferenceEquals(Require(flow.PrepareNextInterruption(candidate,steps,chapters)),candidate),Is.True);
            Assert.That(Progress(candidate).CompletedChapterIds,Does.Not.Contain("CH018_003"));

        }

        static CampaignState CompleteActualDeck(CampaignState candidate,IWorldGateOperationsCatalog023 boards)
        {
            var deck=new ExpeditionDeckCommandService089();
            var turns=0;
            while(Gate(candidate).ActiveOperation.Status!=WorldGateOperationStatus023.ReadyToFinalize)
            {
                Assert.That(++turns,Is.LessThan(80));
                var op=Gate(candidate).ActiveOperation;
                Assert.That(op.ExpeditionDeck089.CurrentRow,Has.Count.EqualTo(3));
                // CHECK encounter rows legitimately contain CHANCE/HAZARD/BATTLE.
                // Fate cards use their explicit saved Roll below; no invented free
                // non-fate choice is required from the real authored deck.
                var choices=op.ExpeditionDeck089.CurrentRow.Where(card=>!ExpeditionDeckService089.IsOptionalBattleCard089(card))
                    .OrderBy(card=>card.TreasuryXpCost).ToArray();
                var committed=Result<CampaignState>.Failure("TEST132_NO_ACCEPTED_NONBATTLE_CARD: "+
                    string.Join(";",op.ExpeditionDeck089.CurrentRow.Select(card=>card.CardId+"/"+card.Category+"/cost="+card.TreasuryXpCost)));
                foreach(var card in choices)
                {
                    committed=deck.CommitRouteCard(candidate,boards,card.CardId,op.AlliedRecruitIds.First(),string.Empty);
                    if(committed.IsSuccess)break;
                }
                candidate=Require(committed);
                var pending=Gate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                Assert.That(pending,Is.Not.Null);
                var committedHash=CanonicalJson.Sha256Hex(candidate);
                candidate=RoundTrip(candidate);
                Assert.That(CanonicalJson.Sha256Hex(candidate),Is.EqualTo(committedHash));
                var savedEffect=Gate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt;
                if(savedEffect.Effect132!=null&&!savedEffect.Effect132.IsRolled132)
                {
                    Assert.That(deck.ApplyWorldGateReceiptExactlyOnce(candidate,boards).IsSuccess,Is.False,
                        "The selected fate card cannot collect before its explicit Roll command.");
                    candidate=Require(deck.RollCommittedEffect132(candidate,boards,savedEffect.ReceiptId));
                    Assert.That(Gate(candidate).ActiveOperation.ExpeditionDeck089.PendingReceipt.Effect132.IsRolled132,Is.True);
                    candidate=RoundTrip(candidate);
                }
                candidate=Require(deck.ApplyWorldGateReceiptExactlyOnce(candidate,boards));
            }
            Assert.That(turns,Is.GreaterThanOrEqualTo(10));
            return candidate;
        }

        static CampaignProgressState019 Progress(CampaignState state)=>state.Guild.GuildCity.Strategic017H.Campaign019;
        static WorldGateRuntimeState023 Gate(CampaignState state)=>Progress(state).Playable020.WorldGate023;
        static CampaignState Require(Result<CampaignState> value)
        {Assert.That(value,Is.Not.Null);Assert.That(value.IsSuccess,Is.True,string.Join(";",value.Errors));return value.Value;}
        static CampaignState RoundTrip(CampaignState value)
        {
            var root=Path.Combine(Path.GetTempPath(),"sd_card_flow132_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
            try
            {
                var path=Path.Combine(root,"campaign.json");var store=new AtomicSaveStore();store.Write(path,SaveEnvelopeV1.Create(value,DateTime.UtcNow));
                var loaded=store.ReadWithRecovery(path);Assert.That(loaded.IsSuccess,Is.True,string.Join(";",loaded.Errors));return loaded.Value.CampaignState;
            }
            finally
            {
                Assert.That(Path.GetFullPath(root).StartsWith(Path.GetFullPath(Path.GetTempPath()).TrimEnd('\\','/')+Path.DirectorySeparatorChar+"sd_card_flow132_",StringComparison.OrdinalIgnoreCase),Is.True);
                Directory.Delete(root,true);
            }
        }
        static CampaignState Fresh()
        {
            // Only the initial synthetic roster/profile is supplied; every quest,
            // choice, receipt, reward and chapter completion is earned by its real reducer.
            var recruit=new RecruitState("FLOW_RECRUIT132",100,100,20,20);
            var union=new UnionState(Union,"Flow test Union",UnionKind.Normal,recruit.RecruitId,new[]{recruit.RecruitId},"FORMATION_LINE","DOCTRINE_BALANCED",20,8000);
            return new CampaignState("00000000-0000-0000-0000-000000000132",132,"1.0",ModeRuleSnapshot.StandardDefaults(),
                new GuildState("GUILD_FLOW132",1000,new[]{recruit},new[]{union}),
                new NewGuildProfileState("Synthetic Flow132",GameMode.Standard,TutorialDepth.FullTutorial,AccessibilitySettingsState.Defaults(),false),
                new OpeningFlowState(OpeningStage.Complete,"SDGOW_TUTORIAL_V1_001",true,null,false,439,0,true,true,true,true,"synthetic_flow132"));
        }
    }
}
#endif
