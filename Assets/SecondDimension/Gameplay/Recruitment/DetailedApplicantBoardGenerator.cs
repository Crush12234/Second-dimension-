using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SecondDimension.Determinism;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Deterministically commits a whole applicant board. Callers persist the returned board and reopen it;
    /// they must not regenerate a committed board in response to UI navigation.
    /// </summary>
    public sealed class DetailedApplicantBoardGenerator
    {
        public const int BaseSignatureBasisPoints = 300;
        public const int DryBonusPerBoardBasisPoints = 75;
        public const int DryBonusCapBasisPoints = 500;
        public const int NormalChanceCapBasisPoints = 1200;
        public const int MercyThresholdBoards = 10;

        private readonly RecruitmentContent _content;
        private readonly OpeningRecruitGenerator _procedural;
        private readonly SignatureRecruitMaterializer _signatures;

        public DetailedApplicantBoardGenerator(RecruitmentContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _procedural = new OpeningRecruitGenerator(content);
            _signatures = new SignatureRecruitMaterializer(content);
        }

        public RecruitmentOfficeState GetOfficeState(int tier, int dryStreak, int refreshIndexToday)
        {
            JObject selected = null;
            foreach (var token in (JArray)_content.Office["tiers"])
            {
                if (token["tier"].Value<int>() == tier)
                {
                    selected = (JObject)token;
                    break;
                }
            }
            if (selected == null) throw new ArgumentOutOfRangeException(nameof(tier), "Unsupported Recruitment Office tier.");
            return new RecruitmentOfficeState
            {
                Tier = tier,
                BoardSize = selected["boardSize"].Value<int>(),
                ScoutingAccuracyBase = selected["scoutingAccuracyBase"].Value<int>(),
                SignatureBonusBasisPoints = selected["signatureBonusBasisPoints"].Value<int>(),
                FreeRefreshesPerDay = selected["freeRefreshesPerDay"].Value<int>(),
                ManualRefreshBaseXp = selected["manualRefreshBaseXp"].Value<int>(),
                TargetedScouting = selected["targetedScouting"].Value<bool>(),
                DryStreak = dryStreak,
                RefreshIndexToday = refreshIndexToday
            };
        }

        public int GetSignatureChanceBasisPoints(RecruitmentOfficeState office, int eventBonusBasisPoints = 0)
        {
            if (office == null) throw new ArgumentNullException(nameof(office));
            var drought = Math.Min(DryBonusCapBasisPoints,
                Math.Max(0, office.DryStreak) * DryBonusPerBoardBasisPoints);
            return Math.Min(NormalChanceCapBasisPoints,
                BaseSignatureBasisPoints + office.SignatureBonusBasisPoints + drought + Math.Max(0, eventBonusBasisPoints));
        }

        public DetailedApplicantBoard Generate(ApplicantBoardRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.CampaignSeed == null) throw new ArgumentException("CampaignSeed is required.", nameof(request));
            if (request.UnlockedWorlds == null || request.UnlockedWorlds.Count == 0
                || request.UnlockedRaces == null || request.UnlockedRaces.Count == 0)
                throw new ArgumentException("At least one world and race must be unlocked.", nameof(request));

            var office = GetOfficeState(request.OfficeTier, request.DryStreak, request.RefreshIndex);
            var boardSize = !request.BoardSizeOverride.HasValue || request.BoardSizeOverride.Value == 0
                ? office.BoardSize
                : request.BoardSizeOverride.Value;
            if (boardSize < 2 || boardSize > 12) throw new ArgumentOutOfRangeException(nameof(request), "Board size must be 2..12.");

            var target = NormalizeTarget(request.TargetedSearch);
            if (target != null && !office.TargetedScouting)
                throw new InvalidOperationException("Targeted scouting is not unlocked at this office tier.");

            var worlds = Sorted(request.UnlockedWorlds);
            var races = Sorted(request.UnlockedRaces);
            var flags = Sorted(request.EligibilityFlags);
            var recruited = new HashSet<string>(request.RecruitedSignatureIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var seen = new HashSet<string>(request.SeenSignatureIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            var declined = new Dictionary<string, int>(StringComparer.Ordinal);
            if (request.DeclinedSignatureUntilDay != null)
                foreach (var pair in request.DeclinedSignatureUntilDay) declined.Add(pair.Key, pair.Value);
            var forcedApplicants = request.ForcedApplicants ?? Array.Empty<ForcedApplicantSpec>();

            var eligible = EligibleSignatures(
                new HashSet<string>(worlds, StringComparer.Ordinal), recruited,
                new HashSet<string>(flags, StringComparer.Ordinal), request.GuildRank, declined, request.GuildDay);
            var rollEligible = eligible
                .Where(candidate => SupportsSourceChannel(
                    candidate, RollSourceChannel(candidate, target)))
                .ToList();
            var chanceBasisPoints = GetSignatureChanceBasisPoints(office, request.EventBonusBasisPoints);
            var mercy = office.DryStreak >= MercyThresholdBoards && rollEligible.Count > 0;

            var targetToken = target == null ? new JObject() : RecruitmentDeterminism.ToObjectToken(target);
            var forcedToken = ForcedToken(forcedApplicants);
            var keyParts = new object[]
            {
                request.CampaignSeed, "DETAILED_APPLICANT_BOARD", request.GuildDay, request.RefreshIndex,
                request.OfficeTier, request.DryStreak,
                RecruitmentDeterminism.StableHash(new JArray(worlds)),
                RecruitmentDeterminism.StableHash(new JArray(races)),
                RecruitmentDeterminism.StableHash(new JArray(flags)),
                RecruitmentDeterminism.StableHash(new JArray(recruited.OrderBy(x => x, StringComparer.Ordinal))),
                RecruitmentDeterminism.StableHash(new JArray(seen.OrderBy(x => x, StringComparer.Ordinal))),
                RecruitmentDeterminism.StableHash(targetToken),
                RecruitmentDeterminism.StableHash(forcedToken),
                request.EventBonusBasisPoints,
                _content.ProceduralContentVersion
            };
            var rng = RecruitmentDeterminism.Rng(keyParts);
            var slots = new ApplicantSlotDetailed[boardSize];
            var selectedSignatureIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var forced in forcedApplicants)
            {
                if (forced == null) throw new ArgumentException("A forced applicant entry cannot be null.", nameof(request));
                var slotIndex = forced.SlotIndex ?? 1;
                if (slotIndex <= 0 || slotIndex >= boardSize)
                    throw new ArgumentOutOfRangeException(nameof(request), "Forced applicant slot must be 1..boardSize-1.");
                if (slots[slotIndex] != null)
                    throw new InvalidOperationException("Duplicate forced applicant slot " + slotIndex + ".");
                var sourceChannel = string.IsNullOrEmpty(forced.SourceChannel) ? "EVENT" : forced.SourceChannel;
                OpeningRecruitRecord instance;
                if (StringComparer.Ordinal.Equals(forced.SourceType, "SIGNATURE"))
                {
                    var signatureId = forced.SignatureId;
                    if (recruited.Contains(signatureId) || selectedSignatureIds.Contains(signatureId))
                        throw new InvalidOperationException("Forced Signature Recruit is unavailable or duplicated.");
                    var authored = eligible.FirstOrDefault(candidate => StringComparer.Ordinal.Equals(
                        candidate["signatureId"]?.Value<string>(), signatureId));
                    if (authored == null)
                        throw new InvalidOperationException(
                            "Forced Signature Recruit does not satisfy its authored eligibility rules.");
                    if (!SupportsSourceChannel(authored, sourceChannel))
                        throw new InvalidOperationException(
                            "Forced Signature Recruit does not support source channel " + sourceChannel + ".");
                    instance = _signatures.Materialize(request.CampaignSeed, signatureId, sourceChannel);
                    selectedSignatureIds.Add(signatureId);
                }
                else
                {
                    instance = _procedural.Generate(new ProceduralRecruitRequest
                    {
                        CampaignSeed = request.CampaignSeed,
                        GuildDay = request.GuildDay,
                        RefreshIndex = request.RefreshIndex,
                        SlotIndex = slotIndex,
                        SourceChannel = sourceChannel,
                        UnlockedRaces = races,
                        ExtraSalt = forced.ExtraSalt ?? "FORCED"
                    });
                }
                slots[slotIndex] = CreateSlot(
                    slotIndex, sourceChannel, true, instance, office, request.ScoutSkill);
            }

            var firstProcedural = _procedural.Generate(new ProceduralRecruitRequest
            {
                CampaignSeed = request.CampaignSeed,
                GuildDay = request.GuildDay,
                RefreshIndex = request.RefreshIndex,
                SlotIndex = 0,
                SourceChannel = "GUILD_BOARD",
                UnlockedRaces = races,
                ExtraSalt = string.Empty
            });
            slots[0] = CreateSlot(0, "GUILD_BOARD", true, firstProcedural, office, request.ScoutSkill);

            var freeIndices = new List<int>();
            for (var i = 0; i < slots.Length; i++) if (slots[i] == null) freeIndices.Add(i);
            var mercySlot = mercy && freeIndices.Count > 0 ? (int?)freeIndices[freeIndices.Count - 1] : null;
            foreach (var slotIndex in freeIndices)
            {
                var candidates = rollEligible
                    .Where(x => !selectedSignatureIds.Contains(x["signatureId"].Value<string>()))
                    .ToList();
                var mustSignature = mercySlot.HasValue && slotIndex == mercySlot.Value && candidates.Count > 0;
                var signatureRoll = mustSignature
                    || (candidates.Count > 0 && rng.NextBounded(10000) < chanceBasisPoints);
                OpeningRecruitRecord instance;
                string sourceChannel;
                if (signatureRoll)
                {
                    var weighted = new List<WeightedValue<JObject>>();
                    foreach (var candidate in candidates)
                    {
                        var candidateSignatureId = candidate["signatureId"].Value<string>();
                        var unseen = seen.Contains(candidateSignatureId) ? 1 : 2;
                        var targetMultiplier = target != null && MatchesTarget(candidate, target) ? 4 : 1;
                        var secretModifier = StringComparer.Ordinal.Equals(
                            candidate["contentRole"].Value<string>(), "SECRET_CONDITION_HEAVY") ? 60 : 100;
                        var weight = Math.Max(1,
                            candidate["recruitment"]["baseApplicantWeight"].Value<int>()
                            * unseen * targetMultiplier * secretModifier / 100);
                        weighted.Add(new WeightedValue<JObject>(candidate, checked((uint)weight)));
                    }
                    var chosen = rng.WeightedChoice(weighted);
                    var signatureId = chosen["signatureId"].Value<string>();
                    selectedSignatureIds.Add(signatureId);
                    sourceChannel = RollSourceChannel(chosen, target);
                    instance = _signatures.Materialize(request.CampaignSeed, signatureId, sourceChannel);
                }
                else
                {
                    sourceChannel = "GUILD_BOARD";
                    instance = _procedural.Generate(new ProceduralRecruitRequest
                    {
                        CampaignSeed = request.CampaignSeed,
                        GuildDay = request.GuildDay,
                        RefreshIndex = request.RefreshIndex,
                        SlotIndex = slotIndex,
                        SourceChannel = sourceChannel,
                        UnlockedRaces = races,
                        ExtraSalt = string.Empty
                    });
                }
                slots[slotIndex] = CreateSlot(
                    slotIndex, sourceChannel, mustSignature, instance, office, request.ScoutSkill);
            }

            var anySignature = slots.Any(x => StringComparer.Ordinal.Equals(x.SourceType, "SIGNATURE"));
            var dryAfter = anySignature ? 0 : request.DryStreak + 1;
            var statePayload = StatePayload(request, office, worlds, races, flags, recruited, seen, declined, target, forcedToken);
            var boardHash = RecruitmentDeterminism.StableHash(statePayload, 16);
            var mercyTriggered = mercy && slots.Any(x =>
                x.Guaranteed && StringComparer.Ordinal.Equals(x.SourceType, "SIGNATURE"));

            return new DetailedApplicantBoard
            {
                BoardId = "BOARD_" + boardHash
                          + "_D" + request.GuildDay.ToString("D4", CultureInfo.InvariantCulture)
                          + "_R" + request.RefreshIndex.ToString("D2", CultureInfo.InvariantCulture),
                GenerationSeed = RecruitmentDeterminism.SeedString(keyParts),
                StateHash = RecruitmentDeterminism.StableHash(statePayload),
                CampaignSeedHash = RecruitmentDeterminism.StableHash(
                    RecruitmentDeterminism.Invariant(request.CampaignSeed), 12),
                GuildDay = request.GuildDay,
                RefreshIndex = request.RefreshIndex,
                WorldIds = Array.AsReadOnly(worlds),
                SignatureChanceBasisPoints = chanceBasisPoints,
                DryStreakBefore = request.DryStreak,
                DryStreakAfter = dryAfter,
                MercyCharterTriggered = mercyTriggered,
                OfficeState = office,
                Applicants = Array.AsReadOnly(slots),
                Committed = true,
                ExpiresAfterGuildDay = request.GuildDay + 1
            };
        }

        private List<JObject> EligibleSignatures(
            HashSet<string> worlds,
            HashSet<string> recruited,
            HashSet<string> flags,
            int guildRank,
            IReadOnlyDictionary<string, int> declined,
            int guildDay)
        {
            var result = new List<JObject>();
            foreach (var token in (JArray)_content.Signatures["signatureRecruits"])
            {
                var record = (JObject)token;
                var signatureId = record["signatureId"].Value<string>();
                if (!worlds.Contains(record["home"]?["worldId"].Value<string>())) continue;
                if (recruited.Contains(signatureId)) continue;
                if (declined.TryGetValue(signatureId, out var returnDay) && returnDay > guildDay) continue;
                var recruitment = (JObject)record["recruitment"];
                if (recruitment["minimumGuildRank"].Value<int>() > guildRank) continue;
                var requiredFlags = RecruitmentDeterminism.StringArray(recruitment["eligibilityFlags"]);
                if (requiredFlags.Any(x => !flags.Contains(x))) continue;
                result.Add(record);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(
                left["signatureId"].Value<string>(), right["signatureId"].Value<string>()));
            return result;
        }

        private ApplicantSlotDetailed CreateSlot(
            int slotIndex,
            string sourceChannel,
            bool guaranteed,
            OpeningRecruitRecord instance,
            RecruitmentOfficeState office,
            int scoutSkill)
        {
            return new ApplicantSlotDetailed
            {
                SlotIndex = slotIndex,
                SourceType = instance.SourceType,
                SourceChannel = sourceChannel,
                Guaranteed = guaranteed,
                RecruitId = instance.RecruitId,
                SignatureId = instance.SignatureId,
                SigningCostXp = instance.SigningCostXp,
                ApplicantRecord = instance,
                ScoutingReport = _procedural.CreateScoutingReport(
                    instance, office.Tier, office.ScoutingAccuracyBase, scoutSkill)
            };
        }

        private static JObject StatePayload(
            ApplicantBoardRequest request,
            RecruitmentOfficeState office,
            string[] worlds,
            string[] races,
            string[] flags,
            HashSet<string> recruited,
            HashSet<string> seen,
            IReadOnlyDictionary<string, int> declined,
            TargetedSearchSpec target,
            JArray forced)
        {
            var declinedPairs = new JArray();
            foreach (var pair in declined.OrderBy(x => x.Key, StringComparer.Ordinal))
                declinedPairs.Add(new JArray(pair.Key, pair.Value));

            return new JObject
            {
                ["campaignSeed"] = JToken.FromObject(request.CampaignSeed),
                ["guildDay"] = request.GuildDay,
                ["refreshIndex"] = request.RefreshIndex,
                ["office"] = RecruitmentDeterminism.ToObjectToken(office),
                ["worlds"] = new JArray(worlds),
                ["races"] = new JArray(races),
                ["eligibilityFlags"] = new JArray(flags),
                ["recruited"] = new JArray(recruited.OrderBy(x => x, StringComparer.Ordinal)),
                ["seen"] = new JArray(seen.OrderBy(x => x, StringComparer.Ordinal)),
                ["declined"] = declinedPairs,
                ["target"] = target == null
                    ? (JToken)JValue.CreateNull()
                    : RecruitmentDeterminism.ToObjectToken(target),
                ["forced"] = forced.DeepClone()
            };
        }

        private static JArray ForcedToken(IReadOnlyList<ForcedApplicantSpec> forced)
        {
            var result = new JArray();
            for (var i = 0; i < forced.Count; i++)
            {
                if (forced[i] == null)
                {
                    result.Add(JValue.CreateNull());
                    continue;
                }
                result.Add(JObject.FromObject(forced[i], JsonSerializer.Create(
                    SecondDimension.Determinism.CanonicalJson.DefaultSettings())));
            }
            return result;
        }

        private static TargetedSearchSpec NormalizeTarget(TargetedSearchSpec target)
        {
            if (target == null) return null;
            return string.IsNullOrEmpty(target.SignatureId)
                   && string.IsNullOrEmpty(target.RaceId)
                   && string.IsNullOrEmpty(target.StartingClassId)
                ? null
                : target;
        }

        private static bool MatchesTarget(JObject record, TargetedSearchSpec target)
        {
            if (target == null) return true;
            if (!string.IsNullOrEmpty(target.SignatureId)
                && !StringComparer.Ordinal.Equals(record["signatureId"].Value<string>(), target.SignatureId)) return false;
            if (!string.IsNullOrEmpty(target.RaceId)
                && !StringComparer.Ordinal.Equals(record["raceId"].Value<string>(), target.RaceId)) return false;
            if (!string.IsNullOrEmpty(target.StartingClassId)
                && !StringComparer.Ordinal.Equals(record["startingClassId"].Value<string>(), target.StartingClassId)) return false;
            return true;
        }

        private static string RollSourceChannel(JObject record, TargetedSearchSpec target) =>
            target != null && MatchesTarget(record, target)
                ? "TARGETED_SCOUT"
                : "GUILD_BOARD";

        private static bool SupportsSourceChannel(JObject record, string sourceChannel)
        {
            if (record == null || string.IsNullOrWhiteSpace(sourceChannel)) return false;
            var recruitment = record["recruitment"] as JObject;
            var channels = recruitment?["sourceChannels"] as JArray;
            if (channels == null) return false;
            return channels.Values<string>().Any(value =>
                StringComparer.Ordinal.Equals(value, sourceChannel));
        }

        private static string[] Sorted(IReadOnlyList<string> values)
        {
            return (values ?? Array.Empty<string>()).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }
    }
}
