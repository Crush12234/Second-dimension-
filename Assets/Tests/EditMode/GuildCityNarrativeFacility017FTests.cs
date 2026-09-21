using NUnit.Framework;
using SecondDimension.Presentation.GuildCity017F;

namespace SecondDimension.Tests.EditMode
{
    public sealed class GuildCityNarrativeFacility017FTests
    {
        [Test] public void OpeningNarrativeHasCompleteAuthoredCoverage()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            Assert.That(root.onboardingScenes.Length,Is.EqualTo(12));
            Assert.That(root.contractArcs.Length,Is.EqualTo(3));
            Assert.That(root.nodeNarratives.Length,Is.EqualTo(45));
            Assert.That(root.eventNarratives.Length,Is.EqualTo(18));
            Assert.That(root.relationshipScenes.Length,Is.EqualTo(12));
            Assert.That(root.facilities.Length,Is.EqualTo(30));
            Assert.That(root.hallAmbient.Length,Is.GreaterThanOrEqualTo(40));
            Assert.That(root.audioCues.Length,Is.EqualTo(15));
            Assert.That(root.outcomeEpilogues.Length,Is.EqualTo(12));
        }

        [Test] public void EveryFacilityHasFourImportableConstructionStates()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            foreach(var facility in root.facilities)
            {
                Assert.That(facility.states.Length,Is.EqualTo(4),facility.id);
                foreach(var state in facility.states) Assert.That(GuildCityNarrativeRegistry017F.Sprite(state.resourcePath),Is.Not.Null,state.resourcePath);
            }
        }

        [Test] public void AudioCueLibraryIsLocalAndLoadable()
        {
            foreach(var cue in GuildCityNarrativeRegistry017F.Load().audioCues)
            {
                Assert.That(cue.resourcePath,Does.StartWith("SecondDimension/GuildCity017F/Audio/"),cue.id);
                Assert.That(GuildCityNarrativeRegistry017F.Audio(cue.id),Is.Not.Null,cue.id);
            }
        }

        [Test] public void RelationshipAndEventNarrativePreservesOwnerLaws()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            foreach(var value in root.eventNarratives){Assert.That(value.costsOperation,Is.False,value.id);Assert.That(value.canCauseDeparture,Is.False,value.id);}
            foreach(var value in root.relationshipScenes){Assert.That(value.costsOperation,Is.False,value.id);Assert.That(value.expires,Is.False,value.id);Assert.That(value.canCauseDeparture,Is.False,value.id);}
        }

        [Test] public void EveryContractHasAllFourOutcomeEpilogues()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            foreach(var contract in root.contractArcs)
            {
                Assert.That(System.Array.FindAll(root.outcomeEpilogues,value=>value.contractId==contract.id).Length,Is.EqualTo(4),contract.id);
                Assert.That(contract.battleBriefingLine,Is.Not.Empty,contract.id);
                Assert.That(contract.cityConsequenceLine,Is.Not.Empty,contract.id);
            }
        }

        [Test] public void Release071OpeningNarrativeNamesOneLanternRoadStoryAndDoorInsideHook()
        {
            var root=GuildCityNarrativeRegistry017F.Load();
            var first=System.Array.Find(root.contractArcs,value=>value.id=="CONTRACT_BELL_BENEATH_GATE");
            var second=System.Array.Find(root.contractArcs,value=>value.id=="CONTRACT_LINES_NOT_RETURNED");
            var wayglass=System.Array.Find(root.eventNarratives,value=>value.id=="EVENT_UNSTABLE_BELL_CHAIN");
            var gatehouse=System.Array.Find(root.nodeNarratives,value=>
                value.boardId=="BOARD_BELL_BENEATH_GATE"&&value.nodeId=="N13");
            Assert.That(first.openingTitle,Is.EqualTo("THE BELL BENEATH SKYHOME"));
            Assert.That(first.guildmasterPrompt,Does.Contain("Wayglass"));
            Assert.That(second.openingTitle,Is.EqualTo("THE DOOR INSIDE"));
            Assert.That(wayglass.title,Is.EqualTo("Wayglass Resonance"));
            Assert.That(gatehouse.title,Is.EqualTo("Old Gatehouse Breach"));
        }
    }
}
