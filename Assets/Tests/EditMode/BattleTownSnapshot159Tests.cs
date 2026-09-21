using System;
using Newtonsoft.Json;
using NUnit.Framework;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;

namespace SecondDimension.Tests.EditMode
{
    public sealed class BattleTownSnapshot159Tests
    {
        [Test]
        public void LegacyBattleJsonRetainsItsExactShapeAndHash159()
        {
            const string oldJson = "{\"BattleId\":\"LEGACY159\",\"CommittedForecasts\":[],\"ContentVersion\":\"old\",\"EnemyUnions\":[],\"EventLog\":[],\"FinalStateHash\":\"\",\"ForecastStateBasisHash\":\"\",\"InitialBattleStateHash\":\"\",\"Objective\":\"legacy\",\"Outcome\":0,\"Phase\":0,\"PlayerUnions\":[],\"Reward\":null,\"Round\":1,\"RoundRecords\":[],\"Selections\":[],\"TutorialBreakthroughArtId\":\"\",\"TutorialBreakthroughMemberId\":\"\",\"TutorialBreakthroughOccurred\":false}";
            var read = JsonConvert.DeserializeObject<BattleState>(oldJson);
            Assert.That(read.TownBonuses159, Is.Null);
            Assert.That(CanonicalJson.Serialize(read), Is.EqualTo(oldJson));
            Assert.That(CanonicalJson.Sha256Hex(read.With().WithReward(null)), Is.EqualTo(CanonicalJson.Sha256Hex(read)));
        }

        [Test]
        public void FrozenTownSnapshotSurvivesEveryBattleCopyAndReload159()
        {
            var bonus = new TownBattleBonuses159(850, 300);
            var battle = new BattleState("BONUS159", "current", 1, BattlePhase.ForecastSelection, BattleOutcome.InProgress,
                "snapshot", Array.Empty<BattleUnionState>(), Array.Empty<BattleUnionState>(),
                Array.Empty<BattleForecastState>(), Array.Empty<BattleForecastSelectionState>(),
                Array.Empty<BattleEventState>(), Array.Empty<BattleRoundRecordState>(), "", "", "", "", "", false,
                townBonuses159: bonus);
            var copied = battle.With(round: 2, phase: BattlePhase.Resolved, outcome: BattleOutcome.Victory).WithReward(null);
            Assert.That(copied.TownBonuses159, Is.SameAs(bonus));
            var json = CanonicalJson.Serialize(copied);
            var read = JsonConvert.DeserializeObject<BattleState>(json);
            Assert.That(read.TownBonuses159.TreasuryBasisPoints, Is.EqualTo(850));
            Assert.That(read.TownBonuses159.PersonalBasisPoints, Is.EqualTo(300));
            Assert.That(CanonicalJson.Serialize(read), Is.EqualTo(json));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TownBattleBonuses159(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TownBattleBonuses159(0, -1));
        }
    }
}
