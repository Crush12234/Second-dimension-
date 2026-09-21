using System;
using System.Collections.Generic;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;

namespace SecondDimension.Gameplay.GuildCity017H
{
    public sealed class GuildCityCanonEventService017H
    {
        public Result<CampaignState> SynchronizeAvailability(CampaignState campaign,GuildCityStrategicContent017H content)
        {
            if(campaign==null||content==null)return Result<CampaignState>.Failure("GC017H_CANON_INPUT_REQUIRED");
            var synchronized = GuildCityStoryGateService017H.SynchronizeDerivedGates(campaign);
            if (!synchronized.IsSuccess) return synchronized;
            campaign = synchronized.Value;
            var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();var states=new List<CanonEventState017H>();
            foreach(var definition in content.CanonEvents.Values)
            {
                var existing=Find(strategic.CanonEvents,definition.Id);if(existing!=null&&(existing.Status==CanonEventStatus017H.Viewed||existing.Status==CanonEventStatus017H.Resolved)){states.Add(existing);continue;}
                var available=GateSatisfied(strategic.StoryGates,definition.RequiredStoryGate)&&!StringComparer.Ordinal.Equals(definition.Classification,"WHAT_IF_SANDBOX");states.Add(new CanonEventState017H(definition.Id,available?CanonEventStatus017H.Available:CanonEventStatus017H.Locked,string.Empty,0));
            }
            states.Sort((a,b)=>StringComparer.Ordinal.Compare(a.EventId,b.EventId));return Success(campaign,city.With(strategic017H:strategic.With(canonEvents:states.AsReadOnly(),lastCheckpointId:"canon_events_synchronized"),replaceStrategic017H:true,lastCheckpointId:"canon_events_synchronized"));
        }
        public Result<CampaignState> ViewEvent(CampaignState campaign,GuildCityStrategicContent017H content,string eventId)
        {
            var synced=SynchronizeAvailability(campaign,content);if(!synced.IsSuccess)return synced;campaign=synced.Value;var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H;var definition=content.CanonEvent(eventId);var states=new List<CanonEventState017H>(strategic.CanonEvents);var index=Index(states,eventId);if(index<0||states[index].Status==CanonEventStatus017H.Locked)return Result<CampaignState>.Failure("GC017H_CANON_EVENT_LOCKED");states[index]=states[index].With(status:CanonEventStatus017H.Viewed);return Success(campaign,city.With(strategic017H:strategic.With(canonEvents:states.AsReadOnly(),lastCheckpointId:"canon_event_viewed"),replaceStrategic017H:true,lastCheckpointId:"canon_event_viewed"));
        }
        public Result<CampaignState> ResolveEvent(CampaignState campaign,GuildCityStrategicContent017H content,string eventId,string choiceId)
        {
            if (campaign == null || content == null || string.IsNullOrWhiteSpace(eventId))
                return Result<CampaignState>.Failure("GC017H_CANON_INPUT_REQUIRED");
            if (!content.CanonEvents.TryGetValue(eventId, out var definition))
                return Result<CampaignState>.Failure("GC017H_CANON_EVENT_NOT_FOUND");

            var synced = SynchronizeAvailability(campaign, content);
            if (!synced.IsSuccess) return synced;
            campaign = synced.Value;
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H;
            var states = new List<CanonEventState017H>(strategic.CanonEvents);
            var index = Index(states, eventId);
            if (index < 0 || states[index].Status == CanonEventStatus017H.Locked)
                return Result<CampaignState>.Failure("GC017H_CANON_EVENT_LOCKED");
            if (states[index].Status == CanonEventStatus017H.Resolved)
                return Result<CampaignState>.Success(campaign);
            if (StringComparer.Ordinal.Equals(definition.Classification, "FUTURE_LOCKED") &&
                !GateSatisfied(strategic.StoryGates, definition.RequiredStoryGate))
                return Result<CampaignState>.Failure("GC017H_CANON_EVENT_STORY_GATE_LOCKED");

            var resolution = definition.OutcomeLocked
                ? "LOCKED_CANON_OUTCOME"
                : "CHOICE_" + (string.IsNullOrWhiteSpace(choiceId) ? "DEFAULT" : choiceId);
            states[index] = states[index].With(
                status: CanonEventStatus017H.Resolved,
                resolutionId: resolution,
                resolvedOperationOrdinal: city.OperationOrdinal);

            var rewardId = "CANON_EVENT_" + CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignGuid,
                eventId,
                resolution
            }).Substring(0, 24).ToUpperInvariant();
            var applied = new List<string>(strategic.AppliedStrategicReceiptIds);
            var guild = campaign.Guild;
            var newlyApplied = !applied.Contains(rewardId);
            if (newlyApplied)
            {
                applied.Add(rewardId);
                applied.Sort(StringComparer.Ordinal);
                var development = guild.Development.RecordBattleReward(rewardId, 30, 30);
                guild = guild.With(
                    checked(guild.TreasuryXp + 30),
                    guild.Recruits,
                    guild.Unions,
                    guild.Inventory,
                    development);
            }

            var next = strategic.With(
                canonEvents: states.AsReadOnly(),
                appliedStrategicReceiptIds: applied.AsReadOnly(),
                defenseMasteryXp: strategic.DefenseMasteryXp +
                                  (newlyApplied && !string.IsNullOrWhiteSpace(definition.DefenseProfileId) ? 10 : 0),
                lastCheckpointId: "canon_event_resolved");
            city = city.With(
                strategic017H: next,
                replaceStrategic017H: true,
                lastCheckpointId: "canon_event_resolved");
            return Result<CampaignState>.Success(
                campaign.With(guild.WithGuildCity(city), campaign.OpeningFlow));
        }
        public Result<CampaignState> GrantStoryGate(CampaignState campaign,string storyGateId)
        {
            if(campaign==null||string.IsNullOrWhiteSpace(storyGateId))return Result<CampaignState>.Failure("GC017H_STORY_GATE_REQUIRED");var city=campaign.Guild.GuildCity;var strategic=city.Strategic017H??GuildCityStrategicState017H.Default();var gates=new List<string>(strategic.StoryGates);if(!gates.Contains(storyGateId))gates.Add(storyGateId);gates.Sort(StringComparer.Ordinal);return Success(campaign,city.With(strategic017H:strategic.With(storyGates:gates.AsReadOnly(),lastCheckpointId:"story_gate_granted"),replaceStrategic017H:true,lastCheckpointId:"story_gate_granted"));
        }
        private static bool GateSatisfied(IReadOnlyList<string> gates,string required){if(string.IsNullOrWhiteSpace(required))return true;if(gates!=null)for(var i=0;i<gates.Count;i++)if(StringComparer.Ordinal.Equals(gates[i],required))return true;return false;}
        private static CanonEventState017H Find(IReadOnlyList<CanonEventState017H> values,string id){var index=Index(values,id);return index<0?null:values[index];}
        private static int Index(IReadOnlyList<CanonEventState017H> values,string id){if(values!=null)for(var i=0;i<values.Count;i++)if(StringComparer.Ordinal.Equals(values[i].EventId,id))return i;return -1;}
        private static Result<CampaignState> Success(CampaignState campaign,GuildCityState017D city)=>Result<CampaignState>.Success(campaign.With(campaign.Guild.WithGuildCity(city),campaign.OpeningFlow));
    }
}