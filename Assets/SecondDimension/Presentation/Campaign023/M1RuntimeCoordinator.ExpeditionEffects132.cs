using System;
using System.Linq;
using SecondDimension.Gameplay.Campaign023;

namespace SecondDimension.Presentation
{
    public interface IExpeditionEffectCoordinator132
    {
        ExpeditionEffectView132 PendingExpeditionEffect132 { get; }
        string[] MysteryEffectCardIds132 { get; }
        M1CommandResult RollCommittedExpeditionEffect132(string expectedReceiptId);
    }

    public sealed class ExpeditionEffectView132
    {
        public string ReceiptId;
        public string SurfaceReceiptId;
        public string PresentationReceiptId132 => string.IsNullOrWhiteSpace(SurfaceReceiptId) ? ReceiptId : SurfaceReceiptId;
        public string Kind;
        public string Title;
        public bool Rolled;
        public int D20;
        public int WheelSector;
        public string Result;
        public string Reward;
    }

    public sealed partial class M1RuntimeCoordinator : IExpeditionEffectCoordinator132
    {
        public string[] MysteryEffectCardIds132 => (_campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
            .Playable020?.WorldGate023?.ActiveOperation?.ExpeditionDeck089?.CurrentRow ??
            Array.Empty<ExpeditionRouteCardState089>()).Where(ExpeditionDeckService089.CanOfferEffect132)
            .Select(value=>value.CardId).ToArray();

        public ExpeditionEffectView132 PendingExpeditionEffect132
        {
            get
            {
                var deck = _campaign?.Guild?.GuildCity?.Strategic017H?.Campaign019?
                    .Playable020?.WorldGate023?.ActiveOperation?.ExpeditionDeck089;
                var receipt = deck?.PendingReceipt;
                var effect = receipt?.Effect132;
                if (effect == null || !new ExpeditionDeckService089().ValidateCommittedReceipt(deck,receipt)) return null;
                var card = deck.CurrentRow.FirstOrDefault(value=>value.CardId==receipt.CardId);
                if (card == null) return null;
                var view = new ExpeditionEffectView132 { ReceiptId=receipt.ReceiptId,Kind=effect.Kind,
                    SurfaceReceiptId=_campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation.PendingReceipt?.ReceiptId ?? receipt.ReceiptId,
                    Title=ExpeditionDeckService089.IsRouteFate132(effect) ? effect.Kind==ExpeditionDeckService089.CurseD20132 ? "Rift Hex" : "Wayglass Blessing" : card.Title,Rolled=effect.IsRolled132,D20=effect.D20,WheelSector=effect.WheelSector,
                    Result="",Reward="" };
                if (!effect.IsRolled132) return view;
                var routeReward = ExpeditionDeckService089.IsRouteFate132(effect)
                    ? _campaign.Guild.GuildCity.Strategic017H.Campaign019.Playable020.WorldGate023.ActiveOperation.PendingReceipt : null;
                var guildXp=receipt.GuildXp+(routeReward?.GuildXp??0);
                var hallXp=receipt.HallXp+(routeReward?.HallXp??0);
                view.Reward = guildXp>0 || hallXp>0 ? "+"+guildXp+" Guild XP  •  +"+hallXp+" Hall XP" : "";
                if(routeReward!=null)
                {
                    var materials=receipt.MaterialIds.Count+routeReward.MaterialIds.Count;
                    if(materials>0)view.Reward += "\n+"+materials+" materials";
                    if(receipt.TreasuryXpCost>0)view.Reward += "\nCost: "+receipt.TreasuryXpCost+" Guild XP";
                }
                if (effect.Kind == ExpeditionDeckService089.Wheel132)
                {
                    view.Result = new[] { "EXPERIENCE", "MATERIAL CACHE", "GEAR WON" }[effect.WheelSector];
                    if (effect.WheelSector == 1) view.Reward = receipt.MaterialIds.Count + " material cache";
                    if (effect.WheelSector == 2) view.Reward = ExpeditionDeckService089.ChestEquipmentReward089(receipt.CardId,receipt.ReceiptId).DisplayName;
                }
                else if (effect.Kind == ExpeditionDeckService089.BoonD20132 && effect.D20 == 20 && effect.TargetRecruitId.Length > 0)
                {
                    var hero = _campaign.Guild.Recruits.FirstOrDefault(value=>value.RecruitId==effect.TargetRecruitId);
                    view.Result = "NATURAL 20  •  PERMANENT BOON";
                    view.Reward = (hero?.DisplayName ?? "Owned hero") + " gains +1 permanent Expedition checks.\n" + view.Reward;
                }
                else
                {
                    view.Result = effect.Kind == ExpeditionDeckService089.CurseD20132 ? "TEMPORARY CURSE" : "QUEST BLESSING";
                    var afterMomentum=Math.Max(-2,Math.Min(2,deck.Momentum+ExpeditionDeckService089.AppliedMomentum132(receipt)));
                    view.Reward = "Quest check bonus: " + (deck.Momentum>0?"+":"") + deck.Momentum + " → " +
                        (afterMomentum>0?"+":"") + afterMomentum + ". This quest only.\n" + view.Reward;
                    if (effect.D20 == 20 && effect.Kind == ExpeditionDeckService089.BoonD20132)
                        view.Reward = "Every owned hero is at the permanent boon limit.\n" + view.Reward;
                }
                if(ExpeditionDeckService089.IsRouteFate132(effect))
                    view.Reward += "\nRoute: " + receipt.Outcome.Replace('_',' ') +
                        (receipt.DieOne>0 ? " ("+receipt.DieOne+" + "+receipt.DieTwo+
                            (receipt.EffectiveModifier>=0?" + ":" − ")+Math.Abs(receipt.EffectiveModifier)+")" : "");
                return view;
            }
        }
        public M1CommandResult RollCommittedExpeditionEffect132(string expectedReceiptId)
        {
            Registry023();
            return ApplyAndPersist(_expeditionDeckCommands089.RollCommittedEffect132(_campaign,_campaignRules023,expectedReceiptId),
                true,"The roll is saved. Reveal your fate.");
        }
    }
}
