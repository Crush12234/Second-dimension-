using System;

namespace SecondDimension.Presentation.FirstHour072
{
    /// <summary>
    /// Public handoff implemented by the dedicated battle presenter. The opening root
    /// never reaches into battle visuals. Victory completes after reward claim; a failed
    /// attempt returns unclaimed so the opening can discard it and offer a clean retry.
    /// </summary>
    public interface IFirstHourBattleExperience072
    {
        event Action FirstHourBattleExperienceCompleted072;
        bool IsFirstHourBattleExperienceActive072 { get; }
        void EnterFirstHourBattleExperience072();
    }

    public interface IFirstHourOpeningCoordinator072
    {
        string FirstHourCampaignGuid072 { get; }
        M1CommandResult CompleteFirstHourFoundingCompany072();
        M1CommandResult StartFirstHourHallBreach072();
    }
}
