using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.Navigation164;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.M2
{
    public sealed partial class M2BattleCommandService
    {
        // Only a saved 164 execution checkpoint authorizes additive claims. The
        // battle and its pending reward remain immutable; shared hero progression
        // may have advanced while this execution was parked.
        private static LoopExecution164 ResolveParkedRewardContext164(CampaignState campaign, BattleState battle)
        {
            var loops = campaign.Loops164;
            if (loops == null) return null;
            if (!StringComparer.Ordinal.Equals(loops.ProfileId, campaign.CampaignGuid))
                throw new InvalidOperationException("Parked reward profile differs.");
            var saved = loops.Checkpoints.FirstOrDefault(x => x.Battle?.BattleId == battle.BattleId);
            if (saved?.Members == null) return null;
            // Practice/replay entry points may reuse a BattleId. A previously
            // settled checkpoint cannot authorize or obstruct a fresh reward.
            if (saved.Battle.Reward != null && saved.Battle.Reward.RewardId != battle.Reward.RewardId &&
                campaign.Guild.Development.HasClaimedReward(saved.Battle.Reward.RewardId)) return null;
            if (saved.LoopId != loops.ActiveLoop || saved.LoopId != LoopCheckpoint164.BattleOwner(campaign) ||
                saved.Battle.InitialBattleStateHash != battle.InitialBattleStateHash ||
                saved.Battle.InitialIntegrityStateHash090 != battle.InitialIntegrityStateHash090 ||
                saved.Battle.ContentVersion != battle.ContentVersion ||
                saved.Battle.Reward?.Claimed == true || saved.Battle.RoundRecords.Count > battle.RoundRecords.Count ||
                ParkedRewardHash164(saved.Encounter) != ParkedRewardHash164(campaign.Guild.GuildCity?.PendingEncounter))
                throw new InvalidOperationException("Parked reward execution differs from its checkpoint.");
            for (var i = 0; i < saved.Battle.RoundRecords.Count; i++)
                if (CanonicalJson.Serialize(saved.Battle.RoundRecords[i]) != CanonicalJson.Serialize(battle.RoundRecords[i]))
                    throw new InvalidOperationException("Parked reward completed rounds differ.");
            if (!HasValidFinalStateHash090(battle))
                throw new InvalidOperationException("Parked reward final battle integrity differs.");
            if (saved.Battle.Reward != null &&
                (saved.RewardMembers == null ||
                 CanonicalJson.Serialize(saved.Battle.Reward) != CanonicalJson.Serialize(battle.Reward)))
                throw new InvalidOperationException("Parked reward differs from its committed result.");
            return saved;
        }

        private static string ParkedRewardHash164(object value) => value == null ? "NULL" : CanonicalJson.Sha256Hex(value);

        private static RecruitProgressionState MergeParkedMemberReward164(
            CampaignState campaign, LoopExecution164 saved, BattleMemberRewardState reward,
            BattleMemberState member, RecruitProgressionState current)
        {
            var original = saved.Members.FirstOrDefault(x => x.MemberId == member.MemberId)?.Progression;
            if (original == null) throw new InvalidOperationException("Parked reward member baseline is missing.");
            // An in-progress checkpoint has no reward projection yet. When that
            // battle completes, native finalization projects from the current
            // roster. A parked terminal reward instead uses its frozen baseline.
            var rewardBaseline = saved.Battle.Reward == null ? current :
                saved.RewardMembers.FirstOrDefault(x => x.MemberId == member.MemberId)?.Progression;
            if (rewardBaseline == null) throw new InvalidOperationException("Parked reward projection baseline is missing.");
            ValidateMemberRewardProjection(reward, rewardBaseline,
                rewardBaseline.GainPersonalXp(reward.PersonalXp, member.ClassId));
            if (current.TotalPersonalXp < original.TotalPersonalXp || current.TotalPersonalXp < rewardBaseline.TotalPersonalXp)
                throw new InvalidOperationException("Shared hero experience predates its checkpoint.");
            var progressed = current.GainPersonalXp(reward.PersonalXp, member.ClassId);
            var mastery = new List<RecruitArtMasteryState>(current.ArtMastery);
            var route = saved.Encounter?.RouteModifiers;
            foreach (var art in member.ArtProgress)
            {
                var initial = FindPersistentMastery017H(original.ArtMastery, art.ArtId);
                var live = FindPersistentMastery017H(current.ArtMastery, art.ArtId);
                var initialPoints = initial?.MasteryPoints ?? 0;
                var initialUses = initial?.MeaningfulUses ?? 0;
                var livePoints = live?.MasteryPoints ?? 0;
                var liveUses = live?.MeaningfulUses ?? 0;
                if (art.MasteryPoints < initialPoints || art.MeaningfulUses < initialUses ||
                    livePoints < initialPoints || liveUses < initialUses ||
                    initial != null && initial.Discipline != art.Discipline ||
                    live != null && live.Discipline != art.Discipline)
                    throw new InvalidOperationException("Parked battle art baseline differs from shared progression.");
                var earnedPoints = checked(art.MasteryPoints - initialPoints);
                var earnedUses = checked(art.MeaningfulUses - initialUses);
                var points = checked(livePoints + GuildCityBattleModifierRules017H.ApplyBasisPoints(
                    earnedPoints, GuildCityBattleModifierRules017H.MasteryBasisPoints(route, art.Discipline)));
                var merged = new RecruitArtMasteryState(art.ArtId, art.Discipline,
                    checked(liveUses + earnedUses), points);
                var index = mastery.FindIndex(x => StringComparer.Ordinal.Equals(x.ArtId, art.ArtId));
                if (index < 0) mastery.Add(merged); else mastery[index] = merged;
            }
            var learned = new List<string>(current.LearnedArtIds);
            foreach (var id in member.LearnedArtIds)
                if (!learned.Contains(id, StringComparer.Ordinal)) learned.Add(id);
            return progressed.WithArts(learned.AsReadOnly(), mastery.AsReadOnly());
        }
    }
}
