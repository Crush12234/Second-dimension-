using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;

namespace SecondDimension.Tests.EditMode
{
    public sealed class TitanPresentation161Tests
    {
        static M2BattleView Battle()=>new M2BattleView {
            PlayerUnions=new[]{new M2BattleUnionView{UnionId="ALLY",DisplayName="Guild"}},
            EnemyUnions=new[]{new M2BattleUnionView{UnionId="TITAN",DisplayName="Titan"}}};
        static M2BattleEventView Event(string type,string actor,int amount=0)=>new M2BattleEventView {
            EventType=type,UnionId=actor,ActorUnionId=actor,ActorMemberId=actor+"_MEMBER",
            TargetUnionId=actor,TargetMemberId=actor+"_MEMBER",Amount=amount,Text="Authoritative support"};

        [Test]
        public void EnemySupportStartsAnEnemyChapterAndKeepsNativeVisuals()
        {
            var kinds=new[]{"RESTORATION","ENEMY_SUPPORT_FORECAST","RECOVERY","GUARD"};
            foreach(var kind in kinds)
            {
                var events=new[]{Event("FORECAST_COMMITTED","ALLY"),Event(kind,"TITAN",123)};
                var turns=BattleUnionTurnPlanner019.Plan(Battle(),events);
                Assert.That(turns.Count,Is.EqualTo(2),kind);
                Assert.That(turns[0].IsPlayer,Is.True);Assert.That(turns[1].IsPlayer,Is.False);
                Assert.That(turns[1].FirstEventIndex,Is.EqualTo(1));
                var beat=BattlePresentationPlanner.Plan(events)[1];
                Assert.That(beat.VisuallyStaged,Is.True,kind);
                Assert.That(beat.ActorUnionId,Is.EqualTo("TITAN"));
                Assert.That(beat.TargetUnionId,Is.EqualTo("TITAN"));
                Assert.That(beat.Amount,Is.EqualTo(123));
            }
        }

        [Test]
        public void BarrierReactionDoesNotAnnounceAnEnemyTurnOrAnimateHpLoss()
        {
            var events=new[]{Event("FORECAST_COMMITTED","ALLY"),Event("ALLY_PROTECTED","TITAN",77),
                Event("ENEMY_SUPPORT_FORECAST","TITAN")};
            var turns=BattleUnionTurnPlanner019.Plan(Battle(),events);
            Assert.That(turns.Count,Is.EqualTo(2));Assert.That(turns[1].FirstEventIndex,Is.EqualTo(2));
            var beat=BattlePresentationPlanner.Plan(events)[1];
            Assert.That(beat.Family,Is.EqualTo(BattleBeatFamily.Guard));
            Assert.That(beat.Amount,Is.EqualTo(77));Assert.That(beat.EventType,Is.EqualTo("ALLY_PROTECTED"));
        }

        [Test]
        public void AllySupportStillRequiresItsOwnCommittedForecastChapter()
        {
            var events=new[]{Event("RESTORATION","ALLY",50),Event("RECOVERY","ALLY"),
                Event("FORECAST_COMMITTED","ALLY"),Event("RESTORATION","ALLY",25)};
            var turns=BattleUnionTurnPlanner019.Plan(Battle(),events);
            Assert.That(turns.Count,Is.EqualTo(1));Assert.That(turns.Single().IsPlayer,Is.True);
            Assert.That(turns.Single().FirstEventIndex,Is.EqualTo(2));
        }
    }
}
