using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    [Serializable]
    public sealed class SssBattleGuestState090
    {
        [JsonConstructor]
        public SssBattleGuestState090(
            string ownerHeroId,
            string sourceUnionId,
            string guestUnionId,
            bool active = true)
        {
            OwnerHeroId = SssHeroes.CanonicalId(ownerHeroId);
            if (!SssHeroes.IsSss(OwnerHeroId))
                throw new ArgumentException("Guest owner must be an SSS hero.", nameof(ownerHeroId));
            SourceUnionId = Require(sourceUnionId, nameof(sourceUnionId));
            GuestUnionId = Require(guestUnionId, nameof(guestUnionId));
            Active = active;
        }

        public string OwnerHeroId { get; }
        public string SourceUnionId { get; }
        public string GuestUnionId { get; }
        public bool Active { get; }

        public SssBattleGuestState090 WithActive(bool active) =>
            new SssBattleGuestState090(OwnerHeroId, SourceUnionId, GuestUnionId, active);

        private static string Require(string value, string name) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID is required.", name)
                : value;
    }

    [Serializable]
    public sealed class SssBattleTransformationState090
    {
        [JsonConstructor]
        public SssBattleTransformationState090(
            string formMemberId,
            string originalCasterMemberId,
            BattleUnionState originalUnion,
            UnionTransformationSave transformation)
        {
            FormMemberId = string.IsNullOrWhiteSpace(formMemberId)
                ? throw new ArgumentException("Form member ID is required.", nameof(formMemberId))
                : formMemberId;
            OriginalCasterMemberId = string.IsNullOrWhiteSpace(originalCasterMemberId)
                ? throw new ArgumentException("Original caster member ID is required.", nameof(originalCasterMemberId))
                : originalCasterMemberId;
            OriginalUnion = originalUnion ?? throw new ArgumentNullException(nameof(originalUnion));
            Transformation = transformation?.Copy() ??
                throw new ArgumentNullException(nameof(transformation));
            CovenantTransformation.Validate(Transformation);
            if (!StringComparer.Ordinal.Equals(
                    OriginalUnion.UnionId,
                    Transformation.sourceUnionId))
                throw new ArgumentException("Transformation source Union mismatch.", nameof(transformation));
        }

        public string FormMemberId { get; }
        public string OriginalCasterMemberId { get; }
        public BattleUnionState OriginalUnion { get; }
        public UnionTransformationSave Transformation { get; }

        public SssBattleTransformationState090 WithTransformation(
            UnionTransformationSave transformation) =>
            new SssBattleTransformationState090(
                FormMemberId,
                OriginalCasterMemberId,
                OriginalUnion,
                transformation);
    }

    /// <summary>
    /// Battle-local snapshots only. Long-term progression, preparation and receipts
    /// remain in CampaignState.SssV4090; this state exists solely so a reloaded M2
    /// battle can safely retain guest ownership and restore a transformed Union.
    /// </summary>
    [Serializable]
    public sealed class SssBattleRuntimeState090
    {
        [JsonConstructor]
        public SssBattleRuntimeState090(
            IEnumerable<SssBattleGuestState090> guests = null,
            IEnumerable<SssBattleTransformationState090> transformations = null)
        {
            Guests = (guests ?? Array.Empty<SssBattleGuestState090>())
                .Where(value => value != null)
                .OrderBy(value => value.GuestUnionId, StringComparer.Ordinal)
                .ToArray();
            Transformations = (transformations ??
                    Array.Empty<SssBattleTransformationState090>())
                .Where(value => value != null)
                .OrderBy(value => value.Transformation.sourceUnionId,
                    StringComparer.Ordinal)
                .ToArray();
            if (Guests.Select(value => value.GuestUnionId)
                    .Distinct(StringComparer.Ordinal).Count() != Guests.Length)
                throw new ArgumentException("Guest Union IDs must be unique.", nameof(guests));
            if (Transformations.Select(value => value.Transformation.sourceUnionId)
                    .Distinct(StringComparer.Ordinal).Count() != Transformations.Length)
                throw new ArgumentException("A source Union may transform only once.",
                    nameof(transformations));
        }

        public SssBattleGuestState090[] Guests { get; }
        public SssBattleTransformationState090[] Transformations { get; }

        public static SssBattleRuntimeState090 Empty() =>
            new SssBattleRuntimeState090();

        public SssBattleRuntimeState090 With(
            IEnumerable<SssBattleGuestState090> guests = null,
            IEnumerable<SssBattleTransformationState090> transformations = null) =>
            new SssBattleRuntimeState090(
                guests ?? Guests,
                transformations ?? Transformations);
    }

    /// <summary>
    /// Additive adapter from the staged SSS V3 pure planners/reducers into the one
    /// certified M2 Forecast engine. It never invokes the V3 executor hosts and it
    /// never creates a parallel battle, reward, Art-legality, or targeting authority.
    /// </summary>
    public static class SssBattleIntegration090
    {
        public const int GoldSharedApCost090 = 6;
        public const int GoldPersonalMpCost090 = 10;
        public const string GoldMarker090 = "SSS_GOLD_FORECAST_V4_090";
        public const string GuestCommandId090 = "SSS_GUEST_ASSAULT_090";
        public const string FormCommandId090 = "SSS_FORM_ASSAULT_090";
        public const string SyntheticMemberPrefix090 = "SSS_BATTLE_SYNTH_090_";
        public const int SignatureEffectBudgetPoints090 = 4;

        private const string DefaultFamilyId090 = "ENEMY_REC_001";

        public static bool IsGoldForecast090(BattleForecastState forecast) =>
            forecast != null &&
            forecast.CommandId.StartsWith("SSS_CMD_", StringComparison.Ordinal) &&
            forecast.MemberActions.Count >= 1 &&
            !string.IsNullOrWhiteSpace(forecast.MemberActions[0].ArtId) &&
            forecast.MemberActions[0].ArtId.EndsWith("_ART_04", StringComparison.Ordinal);

        public static bool IsSyntheticForecast090(BattleForecastState forecast) =>
            forecast != null &&
            (StringComparer.Ordinal.Equals(forecast.CommandId, GuestCommandId090) ||
             StringComparer.Ordinal.Equals(forecast.CommandId, FormCommandId090));

        public static bool IsSyntheticMember090(string memberId) =>
            !string.IsNullOrWhiteSpace(memberId) &&
            memberId.StartsWith(SyntheticMemberPrefix090, StringComparison.Ordinal);

        /// <summary>
        /// Public readiness boundary used by inventory/presentation audits.  Only
        /// the exact ten V4 identities have a live handler, and every handler is
        /// resolved inside the certified M2 gold-Forecast transaction.
        /// </summary>
        public static bool HasSignatureWeaponEffectHandler090(string heroId) =>
            !string.IsNullOrWhiteSpace(heroId) &&
            SssTenV4Roster090.TryGet(heroId, out var hero) &&
            Array.IndexOf(SssHeroes.All, hero.HeroId) >= 0;

        public static string SpecialUseReceiptId090(
            CampaignState campaign,
            BattleState battle,
            string heroId)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            if (battle == null) throw new ArgumentNullException(nameof(battle));
            heroId = SssHeroes.CanonicalId(heroId);
            if (!SssHeroes.IsSss(heroId))
                throw new ArgumentException("Unknown SSS hero.", nameof(heroId));
            return ExactNumbers.Key(
                campaign.CampaignGuid + "|" + battle.BattleId + "|" +
                battle.InitialBattleStateHash,
                heroId);
        }

        public static string HeroIdForMember090(
            CampaignState campaign,
            BattleMemberState member)
        {
            if (campaign == null || member == null || IsSyntheticMember090(member.MemberId))
                return null;
            var candidates = new List<string> { member.MemberId };
            var recruits = campaign.Guild?.Recruits;
            if (recruits != null)
            {
                for (var index = 0; index < recruits.Count; index++)
                {
                    var recruit = recruits[index];
                    if (!StringComparer.Ordinal.Equals(recruit.RecruitId, member.MemberId))
                        continue;
                    candidates.Add(recruit.SignatureId);
                    candidates.Add(recruit.AuthoredStableRecruitId);
                    candidates.Add(recruit.TutorialAliasId);
                    break;
                }
            }
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = SssHeroes.CanonicalId(candidates[index]);
                if (SssHeroes.IsSss(candidate)) return candidate;
            }
            return null;
        }

        private static bool HasEquippedSignatureWeapon090(
            CampaignState campaign,
            BattleMemberState member,
            string heroId)
        {
            if (campaign?.Guild?.Recruits == null || member == null ||
                !HasSignatureWeaponEffectHandler090(heroId))
                return false;
            var hero = SssTenV4Roster090.Get(heroId);
            for (var recruitIndex = 0;
                 recruitIndex < campaign.Guild.Recruits.Count;
                 recruitIndex++)
            {
                var recruit = campaign.Guild.Recruits[recruitIndex];
                if (recruit == null || !StringComparer.Ordinal.Equals(
                        recruit.RecruitId, member.MemberId) ||
                    !SssTenV4Roster090.TryGetRecruit(recruit, out var owner) ||
                    !StringComparer.Ordinal.Equals(owner.HeroId, hero.HeroId))
                    continue;
                for (var assignmentIndex = 0;
                     assignmentIndex < recruit.Equipment.Assignments.Count;
                     assignmentIndex++)
                {
                    var item = recruit.Equipment.Assignments[assignmentIndex]?.Item;
                    if (item != null && !item.InventoryOnly &&
                        StringComparer.Ordinal.Equals(
                            item.DefinitionId, hero.WeaponItemId) &&
                        ContainsTag090(item.EquipmentTags, "SSS_SIGNATURE") &&
                        ContainsTag090(item.EquipmentTags,
                            SssTenV4Inventory090.EquipOnlyPrefix + hero.HeroId))
                        return true;
                }
                return false;
            }
            return false;
        }

        public static IReadOnlyList<BattleForecastState> BuildGoldForecasts090(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState source,
            int firstSlot,
            string forecastBasisHash,
            BattleForecastState companionForecast = null,
            Func<string, BattleForecastState> companionForecastForCaster = null)
        {
            if (campaign == null || battle == null || source == null)
                return Array.Empty<BattleForecastState>();
            if (source.Side != BattleSide.Player || source.IsDefeated || source.Retreated)
                return Array.Empty<BattleForecastState>();
            var state = SssTenV4CampaignAccessor090.Read(campaign);
            var runtime = battle.SssBattleRuntime090 ?? SssBattleRuntimeState090.Empty();
            var result = new List<BattleForecastState>();
            for (var memberIndex = 0; memberIndex < source.Members.Count; memberIndex++)
            {
                var member = source.Members[memberIndex];
                if (member.Downed) continue;
                var heroId = HeroIdForMember090(campaign, member);
                if (string.IsNullOrWhiteSpace(heroId)) continue;
                if (!TryPlan090(
                        campaign,
                        battle,
                        source,
                        battle.PlayerUnions,
                        battle.EnemyUnions,
                        member,
                        heroId,
                        state,
                        runtime,
                        out var plan,
                        out _))
                    continue;
                result.Add(BuildForecast090(
                    campaign,
                    battle,
                    source,
                    member,
                    plan,
                    firstSlot + result.Count,
                    forecastBasisHash,
                    companionForecastForCaster?.Invoke(member.MemberId) ??
                    companionForecast));
            }
            return result.AsReadOnly();
        }

        /// <summary>
        /// Separates the displayed non-caster actions from a selected Gold
        /// Forecast so M2 can execute them through its ordinary action resolver.
        /// Vaelis is the one exception: transformation replaces the source actors,
        /// so the package explicitly suppresses their original actions.
        /// </summary>
        public static bool TryBuildCompanionForecast090(
            BattleForecastState gold,
            out BattleForecastState companion)
        {
            companion = null;
            if (!IsGoldForecast090(gold) ||
                StringComparer.Ordinal.Equals(
                    gold.CommandId, "SSS_CMD_VAELIS_MANYFORM"))
                return false;
            var actions = gold.MemberActions.Skip(1).ToArray();
            if (actions.Length == 0) return false;
            companion = new BattleForecastState(
                gold.ForecastId + "_COMPANIONS",
                gold.UnionId,
                "CMD_BALANCED",
                "Gold Forecast — Union Follow-through",
                "The other members carry out the legal actions shown with the Gold command.",
                "Complete Union Forecast",
                gold.TargetId,
                gold.TargetName,
                actions,
                actions.Sum(value => value.SharedApCost),
                0,
                actions.Sum(value => value.PersonalMpCost),
                "Resolve the displayed non-caster actions through certified M2 rules.",
                gold.Risk,
                gold.LearningOpportunity,
                gold.FallbackBehavior,
                gold.GenerationIdentity + "|COMPANIONS",
                gold.DeterministicDebugEvidence);
            return true;
        }

        public static bool TryBuildSyntheticForecast090(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState source,
            M2CombatContent content,
            int slot,
            string forecastBasisHash,
            out BattleForecastState forecast)
        {
            forecast = null;
            if (campaign == null || battle == null || source == null || content == null ||
                source.Side != BattleSide.Player || source.IsDefeated || source.Retreated ||
                battle.SssBattleRuntime090 == null)
                return false;
            var runtime = battle.SssBattleRuntime090;
            var guest = runtime.Guests.FirstOrDefault(value =>
                value.Active && StringComparer.Ordinal.Equals(
                    value.GuestUnionId, source.UnionId));
            var transformation = runtime.Transformations.FirstOrDefault(value =>
                value.Transformation.active && !value.Transformation.restored &&
                StringComparer.Ordinal.Equals(
                    value.Transformation.sourceUnionId, source.UnionId));
            if (guest == null && transformation == null) return false;

            var targetUnion = FirstActiveUnion090(battle.EnemyUnions);
            var targetMember = FirstActiveMember090(targetUnion);
            if (targetUnion == null || targetMember == null) return false;
            var commandId = transformation == null ? GuestCommandId090 : FormCommandId090;
            var artId = transformation == null
                ? "ART_BASIC_HEAVY_SWING"
                : "ART_BASIC_RUNE_BOLT";
            var art = content.Art(artId);
            var actions = new List<BattlePlannedActionState>();
            var remainingAp = source.CurrentAp;
            for (var memberIndex = 0; memberIndex < source.Members.Count; memberIndex++)
            {
                var member = source.Members[memberIndex];
                if (member.Downed || member.CurrentMp < art.PersonalMpCost ||
                    remainingAp < art.SharedApCost)
                    continue;
                var damage = Math.Max(1,
                    10 + (transformation == null ? member.Attack : member.MagicAttack));
                actions.Add(new BattlePlannedActionState(
                    member.MemberId,
                    member.DisplayName,
                    targetUnion.UnionId,
                    targetMember.MemberId,
                    art.Id,
                    art.Name,
                    transformation == null ? BattleActionKind.Martial : BattleActionKind.Mystic,
                    art.SharedApCost,
                    art.PersonalMpCost,
                    -damage,
                    -3,
                    0,
                    "Strike the next legal hostile Union for about " + damage + " HP.",
                    false,
                    false,
                    art.AnimationTag,
                    art.Discipline));
                remainingAp -= art.SharedApCost;
            }
            if (actions.Count == 0) return false;
            var identity = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignSeed,
                battle.BattleId,
                battle.Round,
                source.UnionId,
                commandId,
                slot,
                forecastBasisHash
            });
            forecast = new BattleForecastState(
                "FORECAST_" + identity.Substring(0, 16).ToUpperInvariant(),
                source.UnionId,
                commandId,
                transformation == null ? "TAMED UNION — ADVANCE" : "MANYFORM — ADVANCE",
                "Advance as one complete guest Union.",
                "Certified Union assault",
                targetUnion.UnionId,
                targetUnion.DisplayName,
                actions.AsReadOnly(),
                actions.Sum(value => value.SharedApCost),
                0,
                actions.Sum(value => value.PersonalMpCost),
                "About " + actions.Sum(value => -value.PredictedHpDelta) +
                " total HP pressure through ordinary M2 Arts.",
                "The guest Union follows normal HP, AP, MP, target and defeat rules.",
                "Guest actions grant no guild recruit mastery or duplicate rewards.",
                "Retarget the next legal living enemy through the M2 action resolver.",
                identity,
                CanonicalJson.Serialize(new
                {
                    Marker = GoldMarker090,
                    SyntheticUnion = source.UnionId,
                    commandId,
                    LegalActions = actions,
                    UsesCertifiedM2Resolver = true
                }));
            return true;
        }

        public static bool TryResolveGoldForecast090(
            CampaignState campaign,
            BattleState battle,
            int sourceUnionIndex,
            BattleForecastState forecast,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events,
            ref SssBattleRuntimeState090 runtime,
            ref SssTenV4State090 state,
            out string failure)
        {
            failure = null;
            if (!IsGoldForecast090(forecast) || campaign == null || battle == null ||
                players == null || enemies == null || events == null ||
                sourceUnionIndex < 0 || sourceUnionIndex >= players.Count)
            {
                failure = "SSS_V4_GOLD_FORECAST_INVALID";
                return false;
            }
            runtime = runtime ?? SssBattleRuntimeState090.Empty();
            state = state ?? SssTenV4State090.Default();
            var source = players[sourceUnionIndex];
            if (source.Side != BattleSide.Player ||
                !StringComparer.Ordinal.Equals(source.UnionId, forecast.UnionId))
            {
                failure = "SSS_V4_PLAYER_UNION_REQUIRED";
                return false;
            }
            var action = forecast.MemberActions[0];
            var actorIndex = source.FindMemberIndex(action.ActorMemberId);
            if (actorIndex < 0 || source.Members[actorIndex].Downed)
            {
                failure = "SSS_V4_LIVING_CASTER_REQUIRED";
                return false;
            }
            var actor = source.Members[actorIndex];
            var heroId = HeroIdForMember090(campaign, actor);
            if (string.IsNullOrWhiteSpace(heroId) ||
                !StringComparer.Ordinal.Equals(forecast.CommandId, CommandId090(heroId)) ||
                !StringComparer.Ordinal.Equals(action.ArtId, SignatureArtId090(heroId)) ||
                action.SharedApCost != GoldSharedApCost090 ||
                action.PersonalMpCost != GoldPersonalMpCost090 ||
                forecast.SharedApCost != forecast.MemberActions.Sum(value =>
                    value.SharedApCost) ||
                forecast.CombinedMpCost != forecast.MemberActions.Sum(value =>
                    value.PersonalMpCost))
            {
                failure = "SSS_V4_COMMITTED_PLAN_MISMATCH";
                return false;
            }
            if (!TryPlan090(
                    campaign,
                    battle,
                    source,
                    players,
                    enemies,
                    actor,
                    heroId,
                    state,
                    runtime,
                    out var livePlan,
                    out var planFailure))
            {
                // A simultaneous allied gold command can legitimately consume the
                // last guest slot or finish all meaningful healing first. Preserve
                // transactional costs and the once-per-battle receipt on cancellation.
                if (StringComparer.Ordinal.Equals(heroId, SssHeroes.All[0]) ||
                    StringComparer.Ordinal.Equals(heroId, SssHeroes.All[1]) ||
                    StringComparer.Ordinal.Equals(heroId, SssHeroes.All[3]))
                {
                    events.Add(M2BattleCommandService.CreateSssEvent090(
                        events.Count,
                        battle.Round,
                        "SSS_GOLD_CANCELLED",
                        BattleSide.Player,
                        source.UnionId,
                        actor.MemberId,
                        action.ArtId,
                        actor.DisplayName + " holds " + forecast.CommandName +
                        "; the live battlefield no longer has a meaningful legal target. No AP, MP, action, or receipt is spent.",
                        0,
                        source.UnionId,
                        actor.MemberId,
                        string.Empty,
                        string.Empty));
                    return true;
                }
                failure = "SSS_V4_REVALIDATION_FAILED: " + planFailure;
                return false;
            }
            if (!StringComparer.Ordinal.Equals(livePlan.id, forecast.CommandId))
            {
                failure = "SSS_V4_LIVE_PLAN_MISMATCH";
                return false;
            }

            var members = new List<BattleMemberState>(source.Members);
            members[actorIndex] = actor.With(
                currentMp: actor.CurrentMp - GoldPersonalMpCost090,
                guarding: false);
            source = source.With(
                members: members.AsReadOnly(),
                currentAp: source.CurrentAp - GoldSharedApCost090,
                guarding: false,
                engagement: EngagementState.Advancing);
            players[sourceUnionIndex] = source;
            actor = source.Members[actorIndex];
            events.Add(M2BattleCommandService.CreateSssEvent090(
                events.Count,
                battle.Round,
                "FORECAST_COMMITTED",
                BattleSide.Player,
                source.UnionId,
                actor.MemberId,
                action.ArtId,
                source.DisplayName + " commits gold Forecast “" + livePlan.label +
                "” for " + GoldSharedApCost090 + " AP and " +
                GoldPersonalMpCost090 + " personal MP.",
                GoldSharedApCost090,
                source.UnionId,
                actor.MemberId,
                forecast.TargetId,
                action.TargetMemberId));

            var signatureEffectMagnitude090 = ApplyEffect090(
                campaign,
                battle,
                sourceUnionIndex,
                actor,
                livePlan,
                players,
                enemies,
                events,
                ref runtime);

            if (signatureEffectMagnitude090 > 0)
            {
                var hero = SssTenV4Roster090.Get(heroId);
                events.Add(M2BattleCommandService.CreateSssEvent090(
                    events.Count,
                    battle.Round,
                    "SSS_SIGNATURE_WEAPON_EFFECT",
                    BattleSide.Player,
                    source.UnionId,
                    actor.MemberId,
                    action.ArtId,
                    hero.WeaponName + " triggers " + hero.SignatureEffectName +
                    ": one bounded equipped effect resolves inside the certified " +
                    "gold Forecast (" + signatureEffectMagnitude090 + "/" +
                    SignatureEffectBudgetPoints090 + " effect points).",
                    signatureEffectMagnitude090,
                    source.UnionId,
                    actor.MemberId,
                    forecast.TargetId,
                    action.TargetMemberId));
            }

            var receipt = SpecialUseReceiptId090(campaign, battle, heroId);
            var receipts = new List<string>(state.SpecialUseReceiptIds);
            if (!receipts.Contains(receipt, StringComparer.Ordinal)) receipts.Add(receipt);
            receipts.Sort(StringComparer.Ordinal);
            state = state.With(specialUseReceiptIds: receipts);
            events.Add(M2BattleCommandService.CreateSssEvent090(
                events.Count,
                battle.Round,
                "SSS_GOLD_COMMAND",
                BattleSide.Player,
                source.UnionId,
                actor.MemberId,
                action.ArtId,
                actor.DisplayName + " completes " + livePlan.label +
                "; its once-per-battle use is certified.",
                1,
                source.UnionId,
                actor.MemberId,
                forecast.TargetId,
                action.TargetMemberId));
            return true;
        }

        public static void SynchronizeTransformations090(
            IReadOnlyList<BattleUnionState> players,
            ref SssBattleRuntimeState090 runtime)
        {
            if (runtime == null || runtime.Transformations.Length == 0 || players == null)
                return;
            var transforms = new List<SssBattleTransformationState090>(
                runtime.Transformations);
            for (var index = 0; index < transforms.Count; index++)
            {
                var record = transforms[index];
                var snapshot = record.Transformation;
                if (!snapshot.active || snapshot.restored || snapshot.defeated) continue;
                var union = FindUnion090(players, snapshot.sourceUnionId);
                var formIndex = union?.FindMemberIndex(record.FormMemberId) ?? -1;
                if (formIndex < 0) continue;
                var form = union.Members[formIndex];
                snapshot = CovenantTransformation.WithResources(
                    snapshot,
                    form.CurrentHp.ToString(),
                    form.CurrentMp.ToString());
                transforms[index] = record.WithTransformation(snapshot);
            }
            runtime = runtime.With(transformations: transforms);
        }

        public static void RestoreTerminalActors090(
            int round,
            List<BattleUnionState> players,
            List<BattleEventState> events,
            ref SssBattleRuntimeState090 runtime)
        {
            if (runtime == null || players == null || events == null) return;
            SynchronizeTransformations090(players, ref runtime);

            var guests = new List<SssBattleGuestState090>(runtime.Guests);
            for (var index = 0; index < guests.Count; index++)
            {
                var guest = guests[index];
                if (!guest.Active) continue;
                var unionIndex = FindUnionIndex090(players, guest.GuestUnionId);
                if (unionIndex >= 0) players.RemoveAt(unionIndex);
                guests[index] = guest.WithActive(false);
                events.Add(M2BattleCommandService.CreateSssEvent090(
                    events.Count,
                    round,
                    "SSS_GUEST_DEPARTED",
                    BattleSide.Player,
                    guest.GuestUnionId,
                    string.Empty,
                    CommandId090(guest.OwnerHeroId),
                    "The friendly guest Union departs before normal guild rewards are calculated.",
                    0,
                    guest.SourceUnionId,
                    guest.OwnerHeroId,
                    guest.GuestUnionId,
                    string.Empty));
            }

            var transforms = new List<SssBattleTransformationState090>(
                runtime.Transformations);
            for (var index = 0; index < transforms.Count; index++)
            {
                var record = transforms[index];
                if (!record.Transformation.active || record.Transformation.restored)
                    continue;
                var liveIndex = FindUnionIndex090(
                    players,
                    record.Transformation.sourceUnionId);
                if (liveIndex < 0)
                    throw new InvalidOperationException(
                        "Active SSS transformation Union is missing at terminal cleanup.");
                var live = players[liveIndex];
                var ended = CovenantTransformation.EndBattle(record.Transformation);
                var restoredMembers = new List<BattleMemberState>();
                for (var memberIndex = 0;
                     memberIndex < record.OriginalUnion.Members.Count;
                     memberIndex++)
                {
                    var original = record.OriginalUnion.Members[memberIndex];
                    var snapshotMemberId = StringComparer.Ordinal.Equals(
                        original.MemberId,
                        record.OriginalCasterMemberId)
                        ? record.Transformation.heroId
                        : original.MemberId;
                    var restored = ended.restoredMembers.Single(value =>
                        StringComparer.Ordinal.Equals(value.memberId, snapshotMemberId));
                    restoredMembers.Add(original.With(
                        currentHp: Int090(restored.currentHp),
                        currentMp: Int090(restored.currentMp),
                        stabilized: false,
                        guarding: false));
                }
                players[liveIndex] = record.OriginalUnion.With(
                    members: restoredMembers.AsReadOnly(),
                    currentAp: Math.Min(
                        record.OriginalUnion.MaximumAp,
                        live.CurrentAp),
                    cohesion: live.Cohesion,
                    formationConditionBasisPoints:
                        live.FormationConditionBasisPoints,
                    engagement: ended.sourceUnionDefeated
                        ? EngagementState.Broken
                        : live.Engagement,
                    guarding: false,
                    retreated: live.Retreated,
                    unionMeaningfulUsePoints: live.UnionMeaningfulUsePoints);
                transforms[index] = record.WithTransformation(ended.next);
                events.Add(M2BattleCommandService.CreateSssEvent090(
                    events.Count,
                    round,
                    "SSS_TRANSFORMATION_RESTORED",
                    BattleSide.Player,
                    record.OriginalUnion.UnionId,
                    record.Transformation.heroId,
                    SignatureArtId090(record.Transformation.heroId),
                    "Union Metamorphosis ends: the exact original members return with conserved HP and MP fractions before rewards.",
                    restoredMembers.Sum(value => value.CurrentHp),
                    record.OriginalUnion.UnionId,
                    record.Transformation.heroId,
                    record.OriginalUnion.UnionId,
                    record.OriginalUnion.LeaderMemberId));
            }
            runtime = runtime.With(guests, transforms);
        }

        private static bool TryPlan090(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState source,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            BattleMemberState caster,
            string heroId,
            SssTenV4State090 state,
            SssBattleRuntimeState090 runtime,
            out SssCommandPlan plan,
            out string reason)
        {
            plan = null;
            reason = null;
            if (campaign == null || battle == null || source == null || caster == null ||
                state == null || runtime == null || source.Side != BattleSide.Player)
            {
                reason = "Invalid M2 SSS planning context.";
                return false;
            }
            heroId = SssHeroes.CanonicalId(heroId);
            var receipt = SpecialUseReceiptId090(campaign, battle, heroId);
            var used = state.SpecialUseReceiptIds.Contains(
                receipt,
                StringComparer.Ordinal);
            var transformed = runtime.Transformations.Any(value =>
                value.Transformation.active && !value.Transformation.restored &&
                StringComparer.Ordinal.Equals(
                    value.Transformation.sourceUnionId,
                    source.UnionId));
            var sourceIds = source.Members.Select(value =>
                    HeroIdForMember090(campaign, value) ?? value.MemberId)
                .ToList();
            var context = new SssCommandContext
            {
                signatureCooldownReady = !used,
                covenant = new GoldCommandContext
                {
                    encounterInstanceId = battle.BattleId,
                    sourceUnionId = source.UnionId,
                    casterHeroId = heroId,
                    sourceMemberIds = sourceIds,
                    preparedFamilyIds = PreparedFamilies090(state, heroId),
                    legalForecastPhase = battle.Outcome == BattleOutcome.InProgress &&
                                         battle.Phase == BattlePhase.ForecastSelection,
                    casterAliveAndCanAct = !caster.Downed && !source.Retreated,
                    sourceUnionAlreadyTransformed = transformed,
                    liveCostAffordable = source.CurrentAp >= GoldSharedApCost090 &&
                                         caster.CurrentMp >= GoldPersonalMpCost090,
                    guestSpaceAvailable = players.Count(value => !value.Retreated) < 10,
                    alreadyUsedThisBattle = used,
                    alreadyOwnsActiveGuest = runtime.Guests.Any(value =>
                        value.Active && StringComparer.Ordinal.Equals(
                            value.OwnerHeroId, heroId))
                },
                unions = BuildTargets090(heroId, source, players, enemies)
            };
            return SssGoldCommands.TryPlan(
                context,
                state.Progression,
                out plan,
                out reason);
        }

        private static List<string> PreparedFamilies090(
            SssTenV4State090 state,
            string heroId)
        {
            var loadout = state.PreparedFamilies.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.HeroId, heroId));
            return loadout == null
                ? new List<string>()
                : new List<string>(loadout.FamilyIds);
        }

        private static List<SssUnionTarget> BuildTargets090(
            string heroId,
            BattleUnionState source,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies)
        {
            var result = new List<SssUnionTarget>();
            var heroIndex = Array.IndexOf(SssHeroes.All, heroId);
            var alliedEffect = heroIndex == 3 || heroIndex == 5 ||
                               heroIndex == 6 || heroIndex == 9;
            if (alliedEffect)
            {
                for (var index = 0; index < players.Count; index++)
                {
                    var union = players[index];
                    var living = IsActive090(union);
                    var targetable = living;
                    if (heroIndex == 3)
                        targetable = living &&
                                     !StringComparer.Ordinal.Equals(
                                         union.UnionId, source.UnionId) &&
                                     HasLivingWound090(union);
                    result.Add(new SssUnionTarget
                    {
                        unionId = union.UnionId,
                        allied = true,
                        living = living,
                        targetable = targetable
                    });
                }
            }
            else
            {
                for (var index = 0; index < enemies.Count; index++)
                {
                    var union = enemies[index];
                    result.Add(new SssUnionTarget
                    {
                        unionId = union.UnionId,
                        allied = false,
                        living = IsActive090(union),
                        targetable = IsActive090(union)
                    });
                }
            }
            return result;
        }

        private static BattleForecastState BuildForecast090(
            CampaignState campaign,
            BattleState battle,
            BattleUnionState source,
            BattleMemberState caster,
            SssCommandPlan plan,
            int slot,
            string forecastBasisHash,
            BattleForecastState companionForecast)
        {
            var heroIndex = Array.IndexOf(SssHeroes.All, plan.heroId);
            var targetUnion = PrimaryTarget090(
                heroIndex,
                source,
                battle.PlayerUnions,
                battle.EnemyUnions,
                plan.targetUnionIds);
            var targetMember = heroIndex == 3
                ? MostWoundedLivingMember090(targetUnion)
                : FirstActiveMember090(targetUnion);
            var signatureEquipped = HasEquippedSignatureWeapon090(
                campaign, caster, plan.heroId);
            var predictedHp = PredictedHp090(
                heroIndex,
                caster,
                battle.PlayerUnions,
                battle.EnemyUnions,
                plan,
                signatureEquipped);
            var expectedEffect = EffectText090(
                heroIndex, plan.targetUnionIds.Count) +
                (signatureEquipped
                    ? " · EQUIPPED SIGNATURE: " +
                      SssTenV4Roster090.Get(plan.heroId).SignatureEffectName +
                      " resolves once within its bounded " +
                      SignatureEffectBudgetPoints090 + "-point effect budget."
                    : string.Empty);
            var kind = heroIndex == 3
                ? BattleActionKind.Restoration
                : heroIndex == 4 || heroIndex == 7 || heroIndex == 8
                    ? BattleActionKind.Mystic
                    : BattleActionKind.Tactical;
            var artId = SignatureArtId090(plan.heroId);
            var action = new BattlePlannedActionState(
                caster.MemberId,
                caster.DisplayName,
                targetUnion?.UnionId ?? source.UnionId,
                targetMember?.MemberId ?? string.Empty,
                artId,
                plan.label,
                kind,
                GoldSharedApCost090,
                GoldPersonalMpCost090,
                predictedHp,
                PredictedCohesion090(heroIndex, signatureEquipped),
                PredictedFormation090(heroIndex, signatureEquipped),
                expectedEffect,
                false,
                false,
                "SSS_GOLD_" + plan.effect.ToString().ToUpperInvariant(),
                kind == BattleActionKind.Restoration
                    ? "Restoration"
                    : kind == BattleActionKind.Mystic
                        ? "Mystic"
                        : "Tactical");
            var actions = new List<BattlePlannedActionState> { action };
            if (heroIndex != 2 && companionForecast != null &&
                StringComparer.Ordinal.Equals(
                    companionForecast.UnionId, source.UnionId))
            {
                for (var companionIndex = 0;
                     companionIndex < companionForecast.MemberActions.Count;
                     companionIndex++)
                {
                    var companion = companionForecast.MemberActions[companionIndex];
                    if (StringComparer.Ordinal.Equals(
                            companion.ActorMemberId, caster.MemberId))
                        continue;
                    var sourceMemberIndex = source.FindMemberIndex(
                        companion.ActorMemberId);
                    if (sourceMemberIndex < 0 ||
                        source.Members[sourceMemberIndex].Downed)
                        continue;
                    actions.Add(companion);
                }
            }
            var sharedApCost = actions.Sum(value => value.SharedApCost);
            var combinedMpCost = actions.Sum(value => value.PersonalMpCost);
            if (actions.Count > 1)
                expectedEffect += " · " + (actions.Count - 1) +
                                  " other Union member" +
                                  (actions.Count == 2 ? string.Empty : "s") +
                                  " follow with the displayed legal actions.";
            var identity = CanonicalJson.Sha256Hex(new
            {
                campaign.CampaignSeed,
                battle.BattleId,
                battle.Round,
                source.UnionId,
                caster.MemberId,
                plan.id,
                plan.effect,
                plan.targetUnionIds,
                plan.powerNumerator,
                plan.powerDenominator,
                CompanionActions = actions.Skip(1).Select(value => new
                {
                    value.ActorMemberId,
                    value.TargetUnionId,
                    value.TargetMemberId,
                    value.ArtId,
                    value.SharedApCost,
                    value.PersonalMpCost
                }),
                slot,
                forecastBasisHash,
                SssRevision = SssTenV4CampaignAccessor090.Read(campaign)
                    .Progression.revision
            });
            return new BattleForecastState(
                "FORECAST_" + identity.Substring(0, 16).ToUpperInvariant(),
                source.UnionId,
                plan.id,
                "GOLD ART — " + plan.label,
                "Unleash “" + plan.label + "” as this Union's complete command.",
                "SSS signature",
                targetUnion?.UnionId ?? source.UnionId,
                targetUnion?.DisplayName ?? source.DisplayName,
                actions.AsReadOnly(),
                sharedApCost,
                0,
                combinedMpCost,
                expectedEffect,
                "Once per battle. Costs are revalidated from live Union AP and caster MP.",
                "Gold Arts use SSS progression but do not grant duplicate normal Art actions.",
                "If simultaneous resolution removes every meaningful target, cancel without cost.",
                identity,
                CanonicalJson.Serialize(new
                {
                    Marker = GoldMarker090,
                    plan.id,
                    plan.heroId,
                    plan.effect,
                    plan.targetUnionIds,
                    SharedAp = sharedApCost,
                    PersonalMp = combinedMpCost,
                    CompanionActionCount = actions.Count - 1,
                    plan.powerNumerator,
                    plan.powerDenominator,
                    SignatureWeaponEquipped = signatureEquipped,
                    SignatureEffectHandlerReady =
                        HasSignatureWeaponEffectHandler090(plan.heroId),
                    SignatureEffectBudgetPoints = signatureEquipped
                        ? SignatureEffectBudgetPoints090
                        : 0,
                    UsesV3PurePlanner = true,
                    UsesStandaloneExecutor = false,
                    UsesCertifiedM2Commit = true
                }));
        }

        private static int ApplyEffect090(
            CampaignState campaign,
            BattleState battle,
            int sourceUnionIndex,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events,
            ref SssBattleRuntimeState090 runtime)
        {
            var signatureEquipped = HasEquippedSignatureWeapon090(
                campaign, caster, plan.heroId);
            switch (plan.effect)
            {
                case SssEffect.TamedUnion:
                    AddGuestUnion090(battle, sourceUnionIndex, caster, plan,
                        players, events, ref runtime, 6, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.GuestSummon:
                    AddGuestUnion090(battle, sourceUnionIndex, caster, plan,
                        players, events, ref runtime, 1, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.Transformation:
                    TransformSourceUnion090(battle, sourceUnionIndex, caster,
                        plan, players, events, ref runtime, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.HealAllAllies:
                    return HealOtherUnions090(
                        battle.Round, sourceUnionIndex, caster,
                        plan, players, events, signatureEquipped);
                case SssEffect.DamageAllEnemies:
                    DamageAllEnemies090(battle.Round, sourceUnionIndex, caster,
                        plan, players, enemies, events, 90, false,
                        signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.RallyAllAllies:
                    BuffAllAllies090(battle.Round, caster, plan, players,
                        events, 20, 1800, 0, 0, false, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.BarrierAllAllies:
                    BuffAllAllies090(battle.Round, caster, plan, players,
                        events, 8, 1000, 0, 0, true, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.StormAllEnemies:
                    DamageAllEnemies090(battle.Round, sourceUnionIndex, caster,
                        plan, players, enemies, events, 75, false,
                        signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.EclipseAllEnemies:
                    DamageAllEnemies090(battle.Round, sourceUnionIndex, caster,
                        plan, players, enemies, events, 65, true,
                        signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                case SssEffect.WorldsongAllAllies:
                    BuffAllAllies090(battle.Round, caster, plan, players,
                        events, 12, 1200, 3, 5, false, signatureEquipped);
                    return signatureEquipped ? SignatureEffectBudgetPoints090 : 0;
                default:
                    throw new InvalidOperationException("Unsupported SSS gold effect.");
            }
        }

        private static void AddGuestUnion090(
            BattleState battle,
            int sourceUnionIndex,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleEventState> events,
            ref SssBattleRuntimeState090 runtime,
            int count,
            bool signatureEquipped)
        {
            if (plan.covenantPlan == null ||
                plan.covenantPlan.baseFamilyIds.Count != count ||
                plan.covenantPlan.masteryDefeats.Count != count ||
                players.Count >= 10)
                throw new InvalidOperationException("SSS guest plan is no longer legal.");
            var source = players[sourceUnionIndex];
            var suffix = CanonicalJson.Sha256Hex(new
            {
                battle.BattleId,
                source.UnionId,
                plan.heroId,
                battle.Round
            }).Substring(0, 12).ToUpperInvariant();
            var guestUnionId = SyntheticMemberPrefix090 + "UNION_" + suffix;
            if (FindUnionIndex090(players, guestUnionId) >= 0)
                throw new InvalidOperationException("SSS guest Union already exists.");
            var members = new List<BattleMemberState>();
            for (var index = 0; index < count; index++)
            {
                var familyId = plan.covenantPlan.baseFamilyIds[index];
                var mastery = CovenantMastery.View(
                    plan.covenantPlan.masteryDefeats[index]);
                if (!mastery.unlocked)
                    throw new InvalidOperationException("Prepared guest family is not unlocked.");
                var familyOrdinal = FamilyOrdinal090(familyId);
                var rank = Int090(mastery.rank);
                var baseHp = count == 1 ? 180 : 72;
                var baseAttack = count == 1 ? 44 : 24;
                var maximumHp = Scale090(baseHp + familyOrdinal % 17,
                    mastery.powerNumerator, mastery.powerDenominator);
                var attack = Scale090(baseAttack + familyOrdinal % 9,
                    mastery.powerNumerator, mastery.powerDenominator);
                var memberId = SyntheticMemberPrefix090 + "MEMBER_" + suffix +
                               "_" + (index + 1).ToString("D2");
                var guestMember = new BattleMemberState(
                    memberId,
                    count == 1 ? "Manifested Pact" : "Stonebound Ally " + (index + 1),
                    "SSS_FRIENDLY_" + familyId,
                    maximumHp,
                    maximumHp,
                    20,
                    20,
                    attack,
                    Math.Max(1, attack / 2),
                    new[] { "HAMMER", familyId },
                    false,
                    false,
                    false,
                    new[] { "ART_BASIC_HEAVY_SWING", "ART_GUARD" },
                    0,
                    0,
                    string.Empty,
                    null,
                    null,
                    familyId,
                    familyId + "_TIER_" + Math.Max(1, rank),
                    Math.Max(1, rank));
                if (signatureEquipped)
                {
                    // Rylen's six-member Union shares one four-point attack
                    // reserve across at most four bodies. Elysia's single guest
                    // receives two physical and two Mystic potency points.
                    var rylen = StringComparer.Ordinal.Equals(
                        plan.heroId, SssHeroes.All[0]);
                    guestMember = WithCombatStats090(
                        guestMember,
                        rylen ? (index < SignatureEffectBudgetPoints090 ? 1 : 0) : 2,
                        rylen ? 0 : 2,
                        true);
                }
                members.Add(guestMember);
            }
            var guest = new BattleUnionState(
                guestUnionId,
                count == 1 ? "Elysia's Manifested Pact" : "Rylen's Tamed Union",
                BattleSide.Player,
                members[0].MemberId,
                members.AsReadOnly(),
                source.FormationId,
                source.FormationName,
                count >= 3,
                count >= 3 ? string.Empty : "Guest formation fights as one manifested member.",
                12,
                12,
                80,
                10000,
                signatureEquipped
                    ? EngagementState.Guarded
                    : EngagementState.Reinforcing,
                signatureEquipped,
                false,
                0);
            players.Add(guest);
            var guests = new List<SssBattleGuestState090>(runtime.Guests)
            {
                new SssBattleGuestState090(
                    plan.heroId,
                    source.UnionId,
                    guestUnionId)
            };
            runtime = runtime.With(guests: guests);
            events.Add(M2BattleCommandService.CreateSssEvent090(
                events.Count,
                battle.Round,
                "SSS_GUEST_UNION_ARRIVED",
                BattleSide.Player,
                guestUnionId,
                members[0].MemberId,
                SignatureArtId090(plan.heroId),
                caster.DisplayName + " calls a complete friendly guest Union of " +
                count + (count == 1 ? " member." : " members."),
                count,
                source.UnionId,
                caster.MemberId,
                guestUnionId,
                members[0].MemberId));
        }

        private static void TransformSourceUnion090(
            BattleState battle,
            int sourceUnionIndex,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleEventState> events,
            ref SssBattleRuntimeState090 runtime,
            bool signatureEquipped)
        {
            if (plan.covenantPlan == null ||
                plan.covenantPlan.baseFamilyIds.Count != 1 ||
                plan.covenantPlan.masteryDefeats.Count != 1)
                throw new InvalidOperationException("SSS transformation plan is incomplete.");
            var source = players[sourceUnionIndex];
            var snapshots = new List<OriginalMemberSnapshot>();
            for (var index = 0; index < source.Members.Count; index++)
            {
                var member = source.Members[index];
                snapshots.Add(new OriginalMemberSnapshot
                {
                    memberId = StringComparer.Ordinal.Equals(
                        member.MemberId, caster.MemberId)
                        ? plan.heroId
                        : member.MemberId,
                    stats = new CovenantStats
                    {
                        maxHp = member.MaximumHp.ToString(),
                        maxMp = member.MaximumMp.ToString(),
                        attack = member.Attack.ToString(),
                        magic = member.MagicAttack.ToString(),
                        defense = Math.Max(1, member.Attack / 2).ToString(),
                        resistance = Math.Max(1, member.MagicAttack / 2).ToString(),
                        agility = Math.Max(1, (member.Attack + member.MagicAttack) / 4).ToString()
                    },
                    currentHp = member.CurrentHp.ToString(),
                    currentMp = member.CurrentMp.ToString(),
                    hostPersistentStateJson = CanonicalJson.Serialize(member)
                });
            }
            var transformation = CovenantTransformation.Begin(
                battle.BattleId,
                source.UnionId,
                plan.heroId,
                plan.covenantPlan.baseFamilyIds[0],
                snapshots,
                new CovenantStats(),
                plan.covenantPlan.masteryDefeats[0]);
            var formMemberId = SyntheticMemberPrefix090 + "FORM_" +
                               CanonicalJson.Sha256Hex(new
                               {
                                   battle.BattleId,
                                   source.UnionId,
                                   plan.heroId
                               }).Substring(0, 12).ToUpperInvariant();
            var form = new BattleMemberState(
                formMemberId,
                "Vaelis — Manyform",
                "SSS_FRIENDLY_" + transformation.baseFamilyId,
                Int090(transformation.currentHp),
                Int090(transformation.transformedStats.maxHp),
                Int090(transformation.currentMp),
                Int090(transformation.transformedStats.maxMp),
                Math.Max(1, Int090(transformation.transformedStats.attack)),
                Math.Max(1, Int090(transformation.transformedStats.magic)),
                new[] { "FOCUS_TOOL", transformation.baseFamilyId },
                false,
                false,
                false,
                new[] { "ART_BASIC_RUNE_BOLT", "ART_GUARD" },
                0,
                0,
                string.Empty,
                null,
                null,
                transformation.baseFamilyId,
                transformation.baseFamilyId + "_FORM",
                Math.Max(1, Int090(transformation.masteryRank)));
            if (signatureEquipped)
            {
                // The ordinary equipped-item stats were already included exactly
                // once in the frozen original aggregate above. Perfect Confluence
                // adds only its separate two-plus-two bounded effect here.
                form = WithCombatStats090(form, 2, 2, true);
            }
            players[sourceUnionIndex] = new BattleUnionState(
                source.UnionId,
                source.DisplayName + " — Manyform",
                BattleSide.Player,
                form.MemberId,
                new[] { form },
                source.FormationId,
                source.FormationName,
                false,
                "Original member actions are suppressed during Union Metamorphosis.",
                source.CurrentAp,
                source.MaximumAp,
                source.Cohesion,
                source.FormationConditionBasisPoints,
                signatureEquipped
                    ? EngagementState.Guarded
                    : EngagementState.Reinforcing,
                signatureEquipped,
                source.Retreated,
                source.UnionMeaningfulUsePoints);
            var transformations = new List<SssBattleTransformationState090>(
                runtime.Transformations)
            {
                new SssBattleTransformationState090(
                    formMemberId,
                    caster.MemberId,
                    source,
                    transformation)
            };
            runtime = runtime.With(transformations: transformations);
            events.Add(M2BattleCommandService.CreateSssEvent090(
                events.Count,
                battle.Round,
                "SSS_UNION_TRANSFORMED",
                BattleSide.Player,
                source.UnionId,
                formMemberId,
                SignatureArtId090(plan.heroId),
                caster.DisplayName + " freezes the original Union aggregate once and becomes Manyform; original actions are suppressed until exact terminal restoration.",
                form.MaximumHp,
                source.UnionId,
                caster.MemberId,
                source.UnionId,
                formMemberId));
        }

        private static int HealOtherUnions090(
            int round,
            int sourceUnionIndex,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleEventState> events,
            bool signatureEquipped)
        {
            var heal = Scale090(
                Math.Max(30, caster.MagicAttack * 2),
                plan.powerNumerator,
                plan.powerDenominator);
            var total = 0;
            var totalOverheal = 0;
            for (var unionIndex = 0; unionIndex < players.Count; unionIndex++)
            {
                if (unionIndex == sourceUnionIndex || !IsActive090(players[unionIndex]))
                    continue;
                var union = players[unionIndex];
                var members = new List<BattleMemberState>(union.Members);
                var unionHealing = 0;
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                {
                    var target = members[memberIndex];
                    if (target.Downed || target.CurrentHp >= target.MaximumHp) continue;
                    var actual = Math.Min(heal, target.MaximumHp - target.CurrentHp);
                    totalOverheal = checked(totalOverheal + heal - actual);
                    members[memberIndex] = target.With(
                        currentHp: target.CurrentHp + actual,
                        stabilized: false,
                        guarding: false);
                    unionHealing += actual;
                    total += actual;
                    events.Add(M2BattleCommandService.CreateSssEvent090(
                        events.Count,
                        round,
                        "RESTORATION",
                        BattleSide.Player,
                        union.UnionId,
                        target.MemberId,
                        SignatureArtId090(plan.heroId),
                        caster.DisplayName + " sends " + plan.label +
                        " across Union lines, restoring " + actual + " HP to " +
                        target.DisplayName + ".",
                        actual,
                        players[sourceUnionIndex].UnionId,
                        caster.MemberId,
                        union.UnionId,
                        target.MemberId));
                }
                if (unionHealing <= 0) continue;
                players[unionIndex] = union.With(
                    members: members.AsReadOnly(),
                    cohesion: Math.Min(100, union.Cohesion + 8),
                    formationConditionBasisPoints: Math.Min(
                        10000,
                        union.FormationConditionBasisPoints + 800),
                    engagement: EngagementState.Reinforcing);
            }
            if (total <= 0)
                throw new InvalidOperationException(
                    "Neris gold healing lost every meaningful cross-Union target.");
            if (!signatureEquipped || totalOverheal <= 0) return 0;

            // Mercy Overflow converts only surplus healing from living targets
            // into the existing one-hit guard/barrier state. Reapplication can
            // only refresh that boolean state; it cannot stack a second barrier.
            var reserve = Math.Min(
                SignatureEffectBudgetPoints090,
                Math.Max(1, totalOverheal / 10));
            for (var unionIndex = 0; unionIndex < players.Count; unionIndex++)
            {
                var union = players[unionIndex];
                if (!IsActive090(union)) continue;
                var members = new List<BattleMemberState>(union.Members);
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                    if (!members[memberIndex].Downed)
                        members[memberIndex] = members[memberIndex].With(
                            guarding: true);
                players[unionIndex] = union.With(
                    members: members.AsReadOnly(),
                    cohesion: Math.Min(100, union.Cohesion + reserve),
                    engagement: EngagementState.Guarded,
                    guarding: true);
            }
            return reserve;
        }

        private static void DamageAllEnemies090(
            int round,
            int sourceUnionIndex,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleUnionState> enemies,
            List<BattleEventState> events,
            int powerPercent,
            bool debuff,
            bool signatureEquipped)
        {
            var fallingConstellation = signatureEquipped &&
                StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[4]);
            var tempestConductor = signatureEquipped &&
                StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[7]);
            var eclipseMeridian = signatureEquipped &&
                StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[8]);
            var damage = Scale090(
                Math.Max(1, caster.MagicAttack * powerPercent / 100),
                plan.powerNumerator,
                plan.powerDenominator) +
                (fallingConstellation ? SignatureEffectBudgetPoints090 : 0);
            var targets = enemies
                .SelectMany(union => union.Members.Select(member => new
                {
                    UnionId = union.UnionId,
                    MemberId = member.MemberId
                }))
                .ToArray();
            for (var index = 0; index < targets.Length; index++)
            {
                var union = FindUnion090(enemies, targets[index].UnionId);
                var memberIndex = union?.FindMemberIndex(targets[index].MemberId) ?? -1;
                if (memberIndex < 0 || union.Members[memberIndex].Downed) continue;
                var action = new BattlePlannedActionState(
                    caster.MemberId,
                    caster.DisplayName,
                    union.UnionId,
                    union.Members[memberIndex].MemberId,
                    SignatureArtId090(plan.heroId),
                    plan.label,
                    BattleActionKind.Mystic,
                    0,
                    0,
                    -damage,
                    debuff ? -10 : -5,
                    debuff ? -1000 : -400,
                    "All-enemy Mystic pressure.",
                    false,
                    false,
                    "SSS_GOLD_MYSTIC",
                    "Mystic");
                M2BattleCommandService.ResolveSssAttack090(
                    round,
                    action,
                    players,
                    enemies,
                    events);
            }
            if (tempestConductor)
            {
                // Break pressure is applied once per living hostile Union, not
                // once per member, keeping the four-point reserve bounded.
                for (var unionIndex = 0; unionIndex < enemies.Count; unionIndex++)
                {
                    var union = enemies[unionIndex];
                    if (!IsActive090(union)) continue;
                    var cohesion = Math.Max(
                        0,
                        union.Cohesion - SignatureEffectBudgetPoints090);
                    enemies[unionIndex] = union.With(
                        cohesion: cohesion,
                        engagement: cohesion == 0
                            ? EngagementState.Broken
                            : EngagementState.RearPressure);
                }
            }
            if (!debuff) return;
            for (var unionIndex = 0; unionIndex < enemies.Count; unionIndex++)
            {
                var union = enemies[unionIndex];
                if (!IsActive090(union)) continue;
                enemies[unionIndex] = union.With(
                    cohesion: Math.Max(0, union.Cohesion - 12),
                    formationConditionBasisPoints: Math.Max(
                        0,
                        union.FormationConditionBasisPoints - 1200 -
                        (eclipseMeridian
                            ? SignatureEffectBudgetPoints090 * 100
                            : 0)),
                    engagement: EngagementState.RearPressure,
                    guarding: false);
                events.Add(M2BattleCommandService.CreateSssEvent090(
                    events.Count,
                    round,
                    "SSS_ECLIPSE_DEBUFF",
                    BattleSide.Enemy,
                    union.UnionId,
                    union.LeaderMemberId,
                    SignatureArtId090(plan.heroId),
                    caster.DisplayName + " leaves " + union.DisplayName +
                    " under Eclipse pressure: Cohesion and formation fall.",
                    12,
                    players[sourceUnionIndex].UnionId,
                    caster.MemberId,
                    union.UnionId,
                    union.LeaderMemberId));
            }
        }

        private static void BuffAllAllies090(
            int round,
            BattleMemberState caster,
            SssCommandPlan plan,
            List<BattleUnionState> players,
            List<BattleEventState> events,
            int cohesion,
            int formation,
            int ap,
            int mp,
            bool barrier,
            bool signatureEquipped)
        {
            var signatureCohesion = 0;
            var signatureFormation = 0;
            if (signatureEquipped)
            {
                if (StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[5]))
                    signatureCohesion = SignatureEffectBudgetPoints090;
                else if (StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[6]))
                    signatureFormation = SignatureEffectBudgetPoints090 * 100;
                else if (StringComparer.Ordinal.Equals(plan.heroId, SssHeroes.All[9]))
                {
                    signatureCohesion = SignatureEffectBudgetPoints090 / 2;
                    signatureFormation =
                        SignatureEffectBudgetPoints090 / 2 * 100;
                }
            }
            var sourceUnion = players.First(value =>
                value.FindMemberIndex(caster.MemberId) >= 0);
            for (var unionIndex = 0; unionIndex < players.Count; unionIndex++)
            {
                var union = players[unionIndex];
                if (!IsActive090(union)) continue;
                var members = new List<BattleMemberState>(union.Members);
                for (var memberIndex = 0; memberIndex < members.Count; memberIndex++)
                {
                    var member = members[memberIndex];
                    if (member.Downed) continue;
                    members[memberIndex] = member.With(
                        currentMp: Math.Min(member.MaximumMp,
                            member.CurrentMp + mp),
                        guarding: barrier || member.Guarding);
                }
                players[unionIndex] = union.With(
                    members: members.AsReadOnly(),
                    currentAp: Math.Min(union.MaximumAp, union.CurrentAp + ap),
                    cohesion: Math.Min(
                        100,
                        union.Cohesion + cohesion + signatureCohesion),
                    formationConditionBasisPoints: Math.Min(
                        10000,
                        union.FormationConditionBasisPoints + formation +
                        signatureFormation),
                    engagement: barrier
                        ? EngagementState.Guarded
                        : EngagementState.Reinforcing,
                    guarding: barrier || union.Guarding);
                events.Add(M2BattleCommandService.CreateSssEvent090(
                    events.Count,
                    round,
                    barrier ? "SSS_ALLIED_BARRIER" : "SSS_ALLIED_BUFF",
                    BattleSide.Player,
                    union.UnionId,
                    union.LeaderMemberId,
                    SignatureArtId090(plan.heroId),
                    caster.DisplayName + " carries " + plan.label + " to " +
                    union.DisplayName + ".",
                    barrier ? 1 : cohesion,
                    sourceUnion.UnionId,
                    caster.MemberId,
                    union.UnionId,
                    union.LeaderMemberId));
            }
        }

        private static BattleUnionState PrimaryTarget090(
            int heroIndex,
            BattleUnionState source,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            IReadOnlyList<string> targetIds)
        {
            if (heroIndex < 3) return source;
            if (heroIndex == 3)
                return targetIds
                    .Select(id => FindUnion090(players, id))
                    .Where(value => value != null)
                    .OrderBy(AverageHpBasisPoints090)
                    .ThenBy(value => value.UnionId, StringComparer.Ordinal)
                    .FirstOrDefault();
            var allied = heroIndex == 5 || heroIndex == 6 || heroIndex == 9;
            var pool = allied ? players : enemies;
            for (var index = 0; index < targetIds.Count; index++)
            {
                var union = FindUnion090(pool, targetIds[index]);
                if (union != null) return union;
            }
            return source;
        }

        private static int PredictedHp090(
            int heroIndex,
            BattleMemberState caster,
            IReadOnlyList<BattleUnionState> players,
            IReadOnlyList<BattleUnionState> enemies,
            SssCommandPlan plan,
            bool signatureEquipped)
        {
            if (heroIndex == 3)
            {
                var heal = Scale090(
                    Math.Max(30, caster.MagicAttack * 2),
                    plan.powerNumerator,
                    plan.powerDenominator);
                long total = 0;
                for (var index = 0; index < players.Count; index++)
                    for (var memberIndex = 0;
                         memberIndex < players[index].Members.Count;
                         memberIndex++)
                    {
                        var member = players[index].Members[memberIndex];
                        if (!StringComparer.Ordinal.Equals(
                                players[index].UnionId, plan.sourceUnionId) &&
                            !member.Downed)
                            total += Math.Min(heal,
                                member.MaximumHp - member.CurrentHp);
                    }
                return ClampInt090(total);
            }
            if (heroIndex == 4 || heroIndex == 7 || heroIndex == 8)
            {
                var percent = heroIndex == 4 ? 90 : heroIndex == 7 ? 75 : 65;
                var damage = Scale090(
                    Math.Max(1, caster.MagicAttack * percent / 100),
                    plan.powerNumerator,
                    plan.powerDenominator) +
                    (signatureEquipped && heroIndex == 4
                        ? SignatureEffectBudgetPoints090
                        : 0);
                var living = enemies.Sum(union =>
                    union.Members.Count(member => !member.Downed));
                return -ClampInt090((long)damage * living);
            }
            return 0;
        }

        private static int PredictedCohesion090(
            int heroIndex,
            bool signatureEquipped) =>
            heroIndex == 5
                ? 20 + (signatureEquipped ? SignatureEffectBudgetPoints090 : 0)
                : heroIndex == 6 ? 8
                : heroIndex == 9
                    ? 12 + (signatureEquipped
                        ? SignatureEffectBudgetPoints090 / 2
                        : 0)
                    : heroIndex == 8 ? -12
                    : heroIndex == 7 && signatureEquipped
                        ? -SignatureEffectBudgetPoints090
                        : 0;

        private static int PredictedFormation090(
            int heroIndex,
            bool signatureEquipped) =>
            heroIndex == 5 ? 1800
                : heroIndex == 6
                    ? 1000 + (signatureEquipped
                        ? SignatureEffectBudgetPoints090 * 100
                        : 0)
                    : heroIndex == 9
                        ? 1200 + (signatureEquipped
                            ? SignatureEffectBudgetPoints090 / 2 * 100
                            : 0)
                        : heroIndex == 8
                            ? -1200 - (signatureEquipped
                                ? SignatureEffectBudgetPoints090 * 100
                                : 0)
                            : 0;

        private static string EffectText090(int heroIndex, int targets)
        {
            switch (heroIndex)
            {
                case 0: return "Summon one six-member friendly guest Union.";
                case 1: return "Summon one powerful one-member friendly guest Union.";
                case 2: return "Replace this Union with one frozen aggregate form until terminal restoration.";
                case 3: return "Heal wounded living members in " + targets + " other allied Union(s).";
                case 4: return "Deal Mystic damage to every living enemy member.";
                case 5: return "Rally every living allied Union.";
                case 6: return "Barrier every living allied Union for the enemy turn.";
                case 7: return "Storm every living enemy member with Mystic damage.";
                case 8: return "Damage and debuff every living enemy Union.";
                case 9: return "Restore AP, MP, Cohesion and formation across all allied Unions.";
                default: return "SSS gold effect.";
            }
        }

        private static string CommandId090(string heroId) =>
            "SSS_CMD_" + SssHeroes.CanonicalId(heroId).Substring(4);

        private static string SignatureArtId090(string heroId) =>
            SssHeroes.CanonicalId(heroId) + "_ART_04";

        private static BattleUnionState FirstActiveUnion090(
            IReadOnlyList<BattleUnionState> unions)
        {
            if (unions == null) return null;
            for (var index = 0; index < unions.Count; index++)
                if (IsActive090(unions[index])) return unions[index];
            return null;
        }

        private static BattleMemberState FirstActiveMember090(
            BattleUnionState union)
        {
            if (union == null) return null;
            for (var index = 0; index < union.Members.Count; index++)
                if (!union.Members[index].Downed) return union.Members[index];
            return null;
        }

        private static BattleMemberState MostWoundedLivingMember090(
            BattleUnionState union) =>
            union?.Members
                .Where(value => !value.Downed && value.CurrentHp < value.MaximumHp)
                .OrderBy(value => (long)value.CurrentHp * 10000L / value.MaximumHp)
                .ThenBy(value => value.MemberId, StringComparer.Ordinal)
                .FirstOrDefault();

        private static bool HasLivingWound090(BattleUnionState union) =>
            union != null && union.Members.Any(value =>
                !value.Downed && value.CurrentHp < value.MaximumHp);

        private static int AverageHpBasisPoints090(BattleUnionState union)
        {
            if (union == null) return int.MaxValue;
            long current = 0;
            long maximum = 0;
            for (var index = 0; index < union.Members.Count; index++)
            {
                if (union.Members[index].Downed) continue;
                current += union.Members[index].CurrentHp;
                maximum += union.Members[index].MaximumHp;
            }
            return maximum <= 0 ? int.MaxValue : (int)(current * 10000L / maximum);
        }

        private static bool IsActive090(BattleUnionState union) =>
            union != null && !union.Retreated && !union.IsDefeated;

        private static BattleUnionState FindUnion090(
            IReadOnlyList<BattleUnionState> unions,
            string unionId)
        {
            if (unions == null) return null;
            for (var index = 0; index < unions.Count; index++)
                if (StringComparer.Ordinal.Equals(unions[index].UnionId, unionId))
                    return unions[index];
            return null;
        }

        private static int FindUnionIndex090(
            IReadOnlyList<BattleUnionState> unions,
            string unionId)
        {
            if (unions == null) return -1;
            for (var index = 0; index < unions.Count; index++)
                if (StringComparer.Ordinal.Equals(unions[index].UnionId, unionId))
                    return index;
            return -1;
        }

        private static int FamilyOrdinal090(string familyId)
        {
            if (string.IsNullOrWhiteSpace(familyId)) return 1;
            var separator = familyId.LastIndexOf('_');
            return separator >= 0 && int.TryParse(
                familyId.Substring(separator + 1), out var value)
                ? Math.Max(1, value)
                : 1;
        }

        private static BattleMemberState WithCombatStats090(
            BattleMemberState member,
            int attackBonus,
            int magicBonus,
            bool guarding) =>
            new BattleMemberState(
                member.MemberId,
                member.DisplayName,
                member.ClassId,
                member.CurrentHp,
                member.MaximumHp,
                member.CurrentMp,
                member.MaximumMp,
                checked(member.Attack + Math.Max(0, attackBonus)),
                checked(member.MagicAttack + Math.Max(0, magicBonus)),
                member.EquipmentTags,
                member.Downed,
                member.Stabilized,
                guarding || member.Guarding,
                member.LearnedArtIds,
                member.MeaningfulUsePoints,
                member.DiscoveryProgress,
                member.BreakthroughArtId,
                member.ArtProgress,
                member.EquippedMainHandInstanceId,
                member.EnemyArtBaseId090,
                member.EnemyArtVariantId090,
                member.VisualVariantSeed090);

        private static bool ContainsTag090(
            IReadOnlyList<string> tags,
            string expected)
        {
            if (tags == null || string.IsNullOrWhiteSpace(expected)) return false;
            for (var index = 0; index < tags.Count; index++)
                if (StringComparer.Ordinal.Equals(tags[index], expected))
                    return true;
            return false;
        }

        private static int Scale090(
            int value,
            string numerator,
            int denominator)
        {
            if (value < 0 || denominator <= 0) throw new ArgumentOutOfRangeException();
            var scaled = (new BigInteger(value) * ExactNumbers.Read(numerator) +
                          denominator - 1) / denominator;
            return ClampBigInteger090(scaled);
        }

        private static int Int090(string value) =>
            ClampBigInteger090(ExactNumbers.Read(value));

        private static int ClampBigInteger090(BigInteger value)
        {
            if (value < BigInteger.Zero) return 0;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        private static int ClampInt090(long value) =>
            value <= 0 ? 0 : value > int.MaxValue ? int.MaxValue : (int)value;
    }
}
