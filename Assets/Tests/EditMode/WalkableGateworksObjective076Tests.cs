using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using SecondDimension.Presentation.GuildCity017D;

namespace SecondDimension.Tests.EditMode
{
    public sealed class WalkableGateworksObjective076Tests
    {
        private static readonly MethodInfo ReturnObjectiveBuilder076 =
            typeof(M1FlowPresenter).GetMethod(
                "WalkableGateworksObjective066",
                BindingFlags.Static | BindingFlags.NonPublic,
                null,
                new[] { typeof(GuildCityExpeditionView017D) },
                null);

        [TestCase("Failed", "operation failed")]
        [TestCase("Extracted", "party has extracted")]
        [TestCase("Completed", "patrol and Wayglass are secure")]
        [TestCase("ObjectiveComplete", "operation has ended")]
        public void FinalizableMissionBriefUsesVisibleReturnMarkersInsteadOfAFalseCardinalDirection076(
            string status076,
            string outcomePhrase076)
        {
            Assert.That(ReturnObjectiveBuilder076, Is.Not.Null,
                "The walkable mission brief builder must remain independently certifiable.");

            var expedition076 = new GuildCityExpeditionView017D
            {
                CurrentNodeId = "N14",
                CurrentNodeKind = "EXTRACTION",
                Status = status076,
                CanFinalizeOperation = true
            };
            var objective076 = ReturnObjectiveBuilder076.Invoke(
                null,
                new object[] { expedition076 }) as string;

            Assert.That(objective076, Does.Contain(outcomePhrase076).IgnoreCase);
            Assert.That(objective076, Does.Contain("glowing Skyhome markers"),
                "A returning player needs an observable landmark, not an assumed compass direction.");
            Assert.That(objective076,
                Does.Not.Match("(?i)\\b(?:north|south|east|west)\\b"),
                "The return brief must stay correct regardless of camera or authored road orientation.");
        }
    }
}
