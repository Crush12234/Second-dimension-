using System;

namespace SecondDimension.Gameplay.PeopleBonds026
{
    [Serializable] public sealed class BondTierDefinition026
    {
        public string tierId; public int rank; public string displayName; public int minimumTrust; public int minimumSharedMemories;
        public int coordinationBonusBasisPoints; public bool allowsLinkArts;
    }
    [Serializable] public sealed class LinkArtDefinition026
    {
        public string linkArtId; public string displayName; public string minimumBondTierId; public int requiredParticipants;
        public string[] requiredDisciplines=Array.Empty<string>(); public int sharedApSurcharge; public int personalMpSurcharge;
        public string forecastIntent; public string effectSummary; public int cohesionBonus; public int enemyCohesionPressure;
        public bool forecastOnlyInStandard; public bool predictedMemberActionsClickable; public bool preservesUnderlyingArtIds;
        public int maxPerUnionPerRound; public string animationFamily; public string cameraProfile; public string vfxFamily; public string audioFamily;
    }
    [Serializable] public sealed class UnionBondDoctrineDefinition026
    {
        public string doctrineId; public string displayName; public string minimumBondTierId; public string preferredForecastIntent;
        public int cohesionBonus; public int linkArtWeightBasisPoints; public bool directMemberSelection; public string summary;
    }
    [Serializable] public sealed class SignatureTechniqueTemplate026
    {
        public string id; public string displayName; public string family; public bool requiresPersonalQuest; public bool requiresEmergentLegend;
        public int minimumMasteryPoints; public bool forecastOnlyInStandard; public bool directIndividualSelection;
        public bool preservesUnderlyingArtLegality; public int maxPerBattle; public string effectSummary;
    }
}
