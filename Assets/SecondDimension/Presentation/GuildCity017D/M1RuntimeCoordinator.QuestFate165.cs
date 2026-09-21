using System;
using SecondDimension.Gameplay.Campaign023;

namespace SecondDimension.Presentation
{
    public interface IQuestFateCoordinator165
    {
        QuestFateView165 PendingQuestFate165 { get; }
        M1CommandResult RollQuestFate165(string expectedReceiptId);
        M1CommandResult CollectQuestFate165(string expectedReceiptId);
        int OpeningHeroCheckBoon165(string recruitId);
    }

    public sealed class QuestFateView165
    {
        public string ReceiptId;
        public string CardId;
        public string Kind;
        public string Title;
        public bool Rolled;
        public bool Lucky;
        public int D20;
        public int WheelSector;
        public string Result;
        public string Reward;
    }

    public sealed partial class M1RuntimeCoordinator : IQuestFateCoordinator165
    {
        public int OpeningHeroCheckBoon165(string recruitId) =>
            SecondDimension.Gameplay.GuildCity017D.GuildCityExpeditionService017D.HeroCheckBoon165(_campaign, recruitId);

        public QuestFateView165 PendingQuestFate165
        {
            get
            {
                if (_campaign == null || _guildCityContent == null || _guildCityRecruitment == null)
                    return null;
                var offer = _guildCityExpeditions.DescribeQuestFate165(
                    _campaign, _guildCityContent, _guildCityRecruitment);
                if (offer == null) return null;
                var wheel = offer.EffectKind165 == ExpeditionDeckService089.Wheel132;
                return new QuestFateView165
                {
                    ReceiptId = offer.ReceiptId165,
                    CardId = offer.CardId,
                    Kind = offer.EffectKind165,
                    Title = wheel ? "Fortune Wheel" : offer.EffectKind165 == ExpeditionDeckService089.CurseD20132
                        ? "Rift Hex" : "Wayglass Blessing",
                    Rolled = offer.EffectRolled165,
                    Lucky = offer.Lucky165,
                    D20 = offer.D20165,
                    WheelSector = offer.WheelSector165,
                    Result = !offer.EffectRolled165 ? string.Empty : wheel
                        ? new[] { "EXPERIENCE", "SUPPLY CACHE", "GEAR WON" }[Math.Max(0, Math.Min(2, offer.WheelSector165))]
                        : "D20  •  " + offer.D20165,
                    Reward = offer.EffectRolled165 ? offer.RewardPreview : string.Empty
                };
            }
        }

        public M1CommandResult RollQuestFate165(string expectedReceiptId) =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.RollQuestFate165(
                _campaign, _guildCityContent, _guildCityRecruitment, expectedReceiptId),
                "Your result is saved. Reveal it, then collect to continue.");

        public M1CommandResult CollectQuestFate165(string expectedReceiptId) =>
            ApplyGuildCityAndPersist(_guildCityExpeditions.CollectQuestFate165(
                _campaign, _guildCityContent, _guildCityRecruitment, expectedReceiptId),
                "Fate collected. Your reward and next quest step are saved.");
    }
}
