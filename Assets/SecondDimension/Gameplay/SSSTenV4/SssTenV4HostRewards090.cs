using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.Campaign019;
using SecondDimension.Gameplay.Campaign020;
using SecondDimension.Gameplay.Creator028;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.GuildCity017H;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.State;
using SecondDimension.SSS.V3;

namespace SecondDimension.Gameplay.SSSTenV4
{
    public sealed class SssTenV4CodePreview090
    {
        public bool Recognized { get; internal set; }
        public bool IsPrimaryRecruit { get; internal set; }
        public bool IsAscensionCredit { get; internal set; }
        public bool IsSignatureWeapon { get; internal set; }
        public bool HeroOwned { get; internal set; }
        public bool AlreadyRedeemed { get; internal set; }
        public bool CanRedeem { get; internal set; }
        public string CodeId { get; internal set; }
        public string HeroId { get; internal set; }
        public string HeroName { get; internal set; }
        public string RewardName { get; internal set; }
        public string Summary { get; internal set; }
    }

    public sealed class SssTenV4AscensionPreview090
    {
        public string HeroId { get; internal set; }
        public int CurrentRank { get; internal set; }
        public int CreditCount { get; internal set; }
        public string StateGuard { get; internal set; }
    }

    public static class SssTenV4Inventory090
    {
        public const string SignatureQualityId = "QUALITY_SSS_SIGNATURE";
        public const string CreditQualityId = "RESOURCE_SSS_ASCENSION";
        public const string EquipOnlyPrefix = "EQUIP_ONLY:";
        public const string BoundToPrefix = "BOUND_TO:";

        public static EquipmentItemState CreateAscensionCredit(
            CampaignState campaign,
            string heroId,
            string authorityReceiptId)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            var hero = SssTenV4Roster090.Get(heroId);
            var instanceId = StableInstanceId(
                "SSSCREDIT090_", campaign.CampaignGuid, hero.HeroId, authorityReceiptId);
            return new EquipmentItemState(
                instanceId,
                hero.CreditItemId,
                hero.DisplayName + " Ascension Credit",
                Array.Empty<string>(),
                new[]
                {
                    "SSS_TEN_V4",
                    "SSS_ASCENSION_CREDIT",
                    BoundToPrefix + hero.HeroId
                },
                CreditQualityId,
                10000,
                false,
                true);
        }

        public static EquipmentItemState CreateSignatureWeapon(
            CampaignState campaign,
            string heroId)
        {
            if (campaign == null) throw new ArgumentNullException(nameof(campaign));
            var hero = SssTenV4Roster090.Get(heroId);
            var entitlement = SssSignatureWeapons.EntitlementReceipt(hero.HeroId);
            var slots = StringComparer.Ordinal.Equals(hero.WeaponFamilyId, "WF07_SHIELD")
                ? new[] { EquipmentSlotIds.OffHand }
                : StringComparer.Ordinal.Equals(hero.WeaponFamilyId, "WF10_CATALYST_FOCUS")
                    ? new[] { EquipmentSlotIds.MainHand, EquipmentSlotIds.ToolRelic }
                    : new[] { EquipmentSlotIds.MainHand };
            var effectReadinessTag =
                SssBattleIntegration090.HasSignatureWeaponEffectHandler090(hero.HeroId)
                    ? "SSS_EFFECT_HANDLER_READY"
                    : "SSS_EFFECT_PENDING";
            return new EquipmentItemState(
                StableInstanceId(
                    "SSSWEAPON090_", campaign.CampaignGuid, hero.HeroId, entitlement),
                hero.WeaponItemId,
                hero.WeaponName,
                slots,
                new[]
                {
                    "SSS_TEN_V4",
                    "SSS_SIGNATURE",
                    effectReadinessTag,
                    EquipOnlyPrefix + hero.HeroId,
                    hero.WeaponFamilyId,
                    "MODIFIED_WEAPON"
                },
                SignatureQualityId,
                10000,
                false);
        }

        public static int AscensionCreditCount(CampaignState campaign, string heroId)
        {
            if (campaign?.Guild == null || !SssTenV4Roster090.TryGet(heroId, out var hero))
                return 0;
            return campaign.Guild.Inventory.Count(item =>
                IsAscensionCreditForHero(item, hero));
        }

        internal static bool IsAscensionCreditForHero(
            EquipmentItemState item,
            SssTenV4HeroDefinition090 hero) =>
            item != null && hero != null && item.InventoryOnly &&
            StringComparer.Ordinal.Equals(item.DefinitionId, hero.CreditItemId) &&
            StringComparer.Ordinal.Equals(item.QualityId, CreditQualityId) &&
            item.EquipmentTags.Contains("SSS_ASCENSION_CREDIT") &&
            item.EquipmentTags.Contains(BoundToPrefix + hero.HeroId);

        public static bool CanEquip(
            RecruitState recruit,
            EquipmentItemState item,
            string slotId,
            out string reason)
        {
            reason = string.Empty;
            if (recruit == null || item == null)
            {
                reason = "Recruit and item are required.";
                return false;
            }
            if (item.InventoryOnly)
            {
                reason = "This is an inventory resource, not equipment.";
                return false;
            }
            if (!item.CanEquipIn(slotId))
            {
                reason = "This item cannot use that slot.";
                return false;
            }
            var restriction = item.EquipmentTags.FirstOrDefault(tag =>
                tag != null && tag.StartsWith(EquipOnlyPrefix, StringComparison.Ordinal));
            if (string.IsNullOrEmpty(restriction)) return true;
            var required = restriction.Substring(EquipOnlyPrefix.Length);
            var owned = SssTenV4Roster090.FindOwned(new[] { recruit }, required);
            if (owned != null) return true;
            reason = SssTenV4Roster090.TryGet(required, out var requiredHero)
                ? requiredHero.DisplayName +
                  " is the only hero who can equip this signature weapon."
                : "This signature weapon's hero restriction is unresolved.";
            return false;
        }

        private static string StableInstanceId(
            string prefix,
            string campaignGuid,
            string heroId,
            string receiptId)
        {
            var hash = CanonicalJson.Sha256Hex(new
            {
                campaignGuid,
                heroId,
                receiptId,
                authority = "SSS_TEN_V4"
            });
            return prefix + hash.Substring(0, 24).ToUpperInvariant();
        }
    }

    /// <summary>
    /// Host-side exact-once mutations shared by Creator codes and natural SSS
    /// acquisition. Callers commit the returned CampaignState through the existing
    /// save authority; this class never creates a separate file or auto-equips.
    /// </summary>
    public static class SssTenV4HostRewards090
    {
        public static SssTenV4AscensionPreview090 PreviewAscension(
            CampaignState campaign,
            string heroId)
        {
            if (!Valid(campaign, heroId, "SSS_ASCENSION_PREVIEW_090",
                    out var error))
                throw new ArgumentException(error, nameof(campaign));
            var hero = SssTenV4Roster090.Get(heroId);
            var recruit = SssTenV4Roster090.FindOwned(
                campaign.Guild.Recruits, hero.HeroId);
            if (recruit == null)
                throw new InvalidOperationException("Recruit this SSS hero first.");
            var creditInstanceIds = campaign.Guild.Inventory
                .Where(item => SssTenV4Inventory090.IsAscensionCreditForHero(
                    item, hero))
                .Select(item => item.InstanceId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var sss = SssTenV4CampaignAccessor090.Read(campaign);
            return new SssTenV4AscensionPreview090
            {
                HeroId = hero.HeroId,
                CurrentRank = recruit.Progression.AscensionLevel,
                CreditCount = creditInstanceIds.Length,
                StateGuard = CanonicalJson.Sha256Hex(new
                {
                    campaign.CampaignGuid,
                    hero.HeroId,
                    RecruitId = recruit.RecruitId,
                    recruit.Progression,
                    CreditInstanceIds = creditInstanceIds,
                    sss.SpecialUseReceiptIds
                })
            };
        }

        public static Result<CampaignState> GrantRecruit(
            CampaignState campaign,
            string heroId,
            string authorityReceiptId)
        {
            if (!Valid(campaign, heroId, authorityReceiptId, out var error))
                return Result<CampaignState>.Failure(error);
            var hero = SssTenV4Roster090.Get(heroId);
            return new CreatorAccessCommandService028().GrantCharacterReward(
                campaign,
                "SSS_RECRUIT_" + authorityReceiptId,
                hero.HeroId,
                SssTenV4Roster090.MaterializeGrant(hero.HeroId));
        }

        public static Result<CampaignState> GrantAscensionCredit(
            CampaignState campaign,
            string heroId,
            string authorityReceiptId)
        {
            if (!Valid(campaign, heroId, authorityReceiptId, out var error))
                return Result<CampaignState>.Failure(error);
            var hero = SssTenV4Roster090.Get(heroId);
            if (!SssTenV4Roster090.RosterContains(campaign.Guild.Recruits, hero.HeroId))
                return Result<CampaignState>.Failure(
                    "Recruit " + hero.DisplayName + " before claiming this bound credit.");
            var receipt = HostReceipt("ASCENSION_CREDIT", authorityReceiptId, hero.HeroId);
            var creator = ReadCreator(campaign);
            if (creator.AppliedReceiptIds.Contains(receipt))
                return Result<CampaignState>.Success(campaign);

            var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory);
            var item = SssTenV4Inventory090.CreateAscensionCredit(
                campaign, hero.HeroId, authorityReceiptId);
            if (!inventory.Any(x => StringComparer.Ordinal.Equals(x.InstanceId, item.InstanceId)))
                inventory.Add(item);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                inventory.AsReadOnly(),
                campaign.Guild.Development);
            var next = campaign.With(guild, campaign.OpeningFlow);
            return Result<CampaignState>.Success(WithCreator(
                next,
                AddCreatorReceipt(ReadCreator(next), receipt, null,
                    "sss_v4_ascension_credit_claimed")));
        }

        /// <summary>
        /// Shared unique entitlement for both code and 70-family hunt paths.
        /// Invoke this against the pre-plan campaign snapshot; it owns adding the
        /// entitlement receipt, allowing callers to merge their other planned SSS
        /// progression changes afterward in the same returned CampaignState.
        /// </summary>
        public static Result<CampaignState> GrantSignatureWeapon(
            CampaignState campaign,
            string heroId,
            string authorityReceiptId)
        {
            if (!Valid(campaign, heroId, authorityReceiptId, out var error))
                return Result<CampaignState>.Failure(error);
            var hero = SssTenV4Roster090.Get(heroId);
            if (!SssTenV4Roster090.RosterContains(campaign.Guild.Recruits, hero.HeroId))
                return Result<CampaignState>.Failure(
                    "Recruit " + hero.DisplayName + " before claiming the signature weapon.");
            var entitlement = SssSignatureWeapons.EntitlementReceipt(hero.HeroId);
            var receipt = HostReceipt("SIGNATURE_WEAPON", entitlement, hero.HeroId);
            var creator = ReadCreator(campaign);
            var sss = SssTenV4CampaignAccessor090.Read(campaign);
            if (creator.AppliedReceiptIds.Contains(receipt) ||
                sss.Progression.codeReceipts.Contains(entitlement))
            {
                // The persistent entitlement remains authoritative after discard.
                return Result<CampaignState>.Success(campaign);
            }

            var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory);
            var item = SssTenV4Inventory090.CreateSignatureWeapon(campaign, hero.HeroId);
            if (!inventory.Any(x => StringComparer.Ordinal.Equals(x.DefinitionId, item.DefinitionId)))
                inventory.Add(item);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                campaign.Guild.Recruits,
                campaign.Guild.Unions,
                inventory.AsReadOnly(),
                campaign.Guild.Development);
            var progression = sss.Progression.Copy();
            progression.codeReceipts.Add(entitlement);
            progression.codeReceipts = progression.codeReceipts
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            progression.revision = Increment(progression.revision);
            var next = campaign.With(guild, campaign.OpeningFlow)
                .WithSssV4090(sss.With(progression: progression));
            return Result<CampaignState>.Success(WithCreator(
                next,
                AddCreatorReceipt(ReadCreator(next), receipt, null,
                    "sss_v4_signature_weapon_claimed")));
        }

        public static Result<CampaignState> Ascend(
            CampaignState campaign,
            string heroId,
            string requestId,
            int expectedCurrentRank,
            int expectedCreditCount,
            string expectedStateGuard)
        {
            if (!Valid(campaign, heroId, requestId, out var error))
                return Result<CampaignState>.Failure(error);
            var hero = SssTenV4Roster090.Get(heroId);
            var sss = SssTenV4CampaignAccessor090.Read(campaign);
            var requestReceipt = ExactNumbers.Key("SssAscend", requestId);
            if (sss.SpecialUseReceiptIds.Contains(requestReceipt))
                return Result<CampaignState>.Success(campaign);
            if (campaign.Battle != null && campaign.Battle.Outcome == BattleOutcome.InProgress)
                return Result<CampaignState>.Failure(
                    "Ascension is available at a preparation boundary, not during battle.");
            var recruits = new List<RecruitState>(campaign.Guild.Recruits);
            var recruit = SssTenV4Roster090.FindOwned(recruits, hero.HeroId);
            if (recruit == null)
                return Result<CampaignState>.Failure("Recruit this SSS hero first.");
            var current = PreviewAscension(campaign, hero.HeroId);
            if (expectedCurrentRank != current.CurrentRank ||
                expectedCreditCount != current.CreditCount ||
                string.IsNullOrWhiteSpace(expectedStateGuard) ||
                !StringComparer.Ordinal.Equals(expectedStateGuard,
                    current.StateGuard))
                return Result<CampaignState>.Failure(
                    "Stale Ascension preview. Refresh before spending; no credit was consumed.");
            if (recruit.Progression.AscensionLevel >= RecruitAscensionRules089.MaximumLevel)
                return Result<CampaignState>.Failure(
                    "A10 reached. No credit was consumed; mastery remains unchanged.");
            var creditIndex = -1;
            for (var index = 0; index < campaign.Guild.Inventory.Count; index++)
                if (SssTenV4Inventory090.IsAscensionCreditForHero(
                        campaign.Guild.Inventory[index], hero))
                { creditIndex = index; break; }
            if (creditIndex < 0)
                return Result<CampaignState>.Failure(
                    "One " + hero.DisplayName + " Ascension Credit is required.");

            var recruitIndex = recruits.IndexOf(recruit);
            recruits[recruitIndex] = recruit.WithProgression(
                recruit.Progression.Ascend089());
            var inventory = new List<EquipmentItemState>(campaign.Guild.Inventory);
            inventory.RemoveAt(creditIndex);
            var guild = campaign.Guild.With(
                campaign.Guild.TreasuryXp,
                recruits.AsReadOnly(),
                campaign.Guild.Unions,
                inventory.AsReadOnly(),
                campaign.Guild.Development);
            var uses = sss.SpecialUseReceiptIds.Concat(new[] { requestReceipt })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            return Result<CampaignState>.Success(
                campaign.With(guild, campaign.OpeningFlow)
                    .WithSssV4090(sss.With(specialUseReceiptIds: uses)));
        }

        internal static CampaignState MarkCreatorCode(
            CampaignState campaign,
            string codeId,
            string checkpoint)
        {
            var creator = ReadCreator(campaign);
            if (creator.RedeemedCodeIds.Contains(codeId)) return campaign;
            var redeemed = creator.RedeemedCodeIds.Concat(new[] { codeId })
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            creator = creator.With(
                redeemedCodeIds: redeemed,
                lastCheckpointId: checkpoint);
            return WithCreator(campaign, creator);
        }

        private static bool Valid(
            CampaignState campaign,
            string heroId,
            string receiptId,
            out string error)
        {
            error = string.Empty;
            if (campaign?.Guild?.GuildCity == null)
            { error = "SSS Ten requires an active campaign and Guild."; return false; }
            if (!SssTenV4Roster090.TryGet(heroId, out _))
            { error = "Unknown SSS Ten hero."; return false; }
            if (string.IsNullOrWhiteSpace(receiptId))
            { error = "An exact-once authority receipt is required."; return false; }
            return true;
        }

        private static string HostReceipt(string kind, string authorityReceiptId, string heroId)
        {
            var hash = CanonicalJson.Sha256Hex(new
            {
                kind,
                authorityReceiptId,
                heroId,
                authority = "SSS_TEN_V4"
            });
            return "SSSREC090_" + hash.Substring(0, 24).ToUpperInvariant();
        }

        private static string Increment(string value) =>
            ExactNumbers.Write(ExactNumbers.Read(value) + 1);

        private static CreatorAccessState028 AddCreatorReceipt(
            CreatorAccessState028 creator,
            string receipt,
            string codeId,
            string checkpoint)
        {
            var applied = creator.AppliedReceiptIds.Concat(new[] { receipt })
                .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();
            var redeemed = string.IsNullOrWhiteSpace(codeId)
                ? creator.RedeemedCodeIds
                : creator.RedeemedCodeIds.Concat(new[] { codeId })
                    .Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray();
            return creator.With(
                redeemedCodeIds: redeemed,
                appliedReceiptIds: applied,
                lastCheckpointId: checkpoint);
        }

        private static CreatorAccessState028 ReadCreator(CampaignState campaign) =>
            campaign.Guild.GuildCity.Strategic017H?.Campaign019?.Playable020
                ?.CreatorAccess028 ?? CreatorAccessState028.Default();

        private static CampaignState WithCreator(
            CampaignState campaign,
            CreatorAccessState028 creator)
        {
            var city = campaign.Guild.GuildCity;
            var strategic = city.Strategic017H ?? GuildCityStrategicState017H.Default();
            var progress = strategic.Campaign019 ?? CampaignProgressState019.Default();
            var playable = progress.Playable020 ?? CampaignPlayableState020.Default();
            playable = playable.With(
                creatorAccess028: creator,
                replaceCreatorAccess028: true,
                lastCheckpointId: creator.LastCheckpointId);
            progress = progress.With(
                playable020: playable,
                replacePlayable020: true,
                lastCheckpointId: creator.LastCheckpointId);
            strategic = strategic.With(
                campaign019: progress,
                replaceCampaign019: true,
                lastCheckpointId: creator.LastCheckpointId);
            city = city.With(
                strategic017H: strategic,
                replaceStrategic017H: true,
                lastCheckpointId: creator.LastCheckpointId);
            return campaign.With(
                campaign.Guild.WithGuildCity(city),
                campaign.OpeningFlow);
        }
    }

    public sealed class SssTenV4CreatorCommand090
    {
        private readonly SssTenV4CreatorCatalog090 _primary =
            new SssTenV4CreatorCatalog090();
        private readonly CreatorAccessCommandService028 _creator =
            new CreatorAccessCommandService028();

        public int CodeCount => _primary.CodeCount +
                                SssSupplementalCodes.Definitions().Count;

        public bool TryPreview(
            CampaignState campaign,
            string input,
            out SssTenV4CodePreview090 preview)
        {
            preview = new SssTenV4CodePreview090();
            if (campaign?.Guild == null || string.IsNullOrWhiteSpace(input)) return false;
            var normalized = input.Trim().ToUpperInvariant();
            if (_primary.TryResolveInput(normalized, out var code, out var primaryHero))
            {
                var progression = SssTenV4CampaignAccessor090.Read(campaign).Progression;
                var receipt = ExactNumbers.Key("SssCode", primaryHero.RecruitCode);
                var owned = SssTenV4Roster090.RosterContains(
                    campaign.Guild.Recruits, primaryHero.HeroId);
                preview = BuildPreview(primaryHero, code.CodeId, true, false, false,
                    owned, progression.codeReceipts.Contains(receipt),
                    "Permanent SSS recruit • starts at A0 • joins Reserve • no equipment is auto-equipped.");
                return true;
            }
            var supplemental = SssSupplementalCodes.Definitions()
                .FirstOrDefault(x => StringComparer.Ordinal.Equals(x.code, normalized));
            if (supplemental == null) return false;
            var hero = SssTenV4Roster090.Get(supplemental.heroId);
            var state = SssTenV4CampaignAccessor090.Read(campaign).Progression;
            var receiptId = ExactNumbers.Key("SssCode", supplemental.code);
            var heroOwned = SssTenV4Roster090.RosterContains(
                campaign.Guild.Recruits, hero.HeroId);
            var credit = supplemental.kind == SssSupplementKind.HeroBoundAscensionCredit;
            preview = BuildPreview(
                hero,
                supplemental.code,
                false,
                credit,
                !credit,
                heroOwned,
                state.codeReceipts.Contains(receiptId),
                credit
                    ? "Raises this hero by one Ascension rank automatically, up to A10. During battle, it applies after rewards are saved."
                    : SssBattleIntegration090.HasSignatureWeaponEffectHandler090(hero.HeroId)
                        ? "Unique Omega-power signature weapon equips on its hero automatically after battle; its equipped-only battle effect is active."
                        : "Unique Omega-power signature weapon equips on its hero automatically after battle; its bespoke effect adapter is not active yet.");
            return true;
        }

        public Result<CampaignState> Redeem(CampaignState campaign, string input)
        {
            if (!TryPreview(campaign, input, out var preview))
                return Result<CampaignState>.Failure("SSS_CODE_NOT_RECOGNIZED");
            if (preview.AlreadyRedeemed)
                return Result<CampaignState>.Failure(
                    "That SSS code was already redeemed in this campaign.");
            if (!preview.CanRedeem)
                return Result<CampaignState>.Failure(
                    "Recruit " + preview.HeroName + " first; this code was not consumed.");

            var sss = SssTenV4CampaignAccessor090.Read(campaign);
            var identities = OwnedIdentities(campaign.Guild.Recruits);
            if (preview.IsPrimaryRecruit)
            {
                var plan = SssAcquisition.RedeemCode(
                    sss.Progression, input, identities);
                var creatorResult = _creator.RedeemCode(
                    campaign,
                    _primary,
                    input,
                    SssTenV4Roster090.MaterializeGrant(plan.heroId));
                if (!creatorResult.IsSuccess) return creatorResult;
                return Result<CampaignState>.Success(
                    creatorResult.Value.WithSssV4090(
                        sss.With(progression: plan.next)));
            }

            var supplemental = SssSupplementalCodes.Plan(
                sss.Progression,
                input,
                identities,
                campaign.Guild.Inventory.Select(x => x.DefinitionId));
            if (supplemental.definition == null)
                return Result<CampaignState>.Failure("SSS_CODE_NOT_RECOGNIZED");
            Result<CampaignState> host;
            if (supplemental.definition.kind == SssSupplementKind.HeroBoundAscensionCredit)
                host = SssTenV4HostRewards090.GrantAscensionCredit(
                    campaign, supplemental.definition.heroId, supplemental.receipt);
            else if (supplemental.grant)
                host = SssTenV4HostRewards090.GrantSignatureWeapon(
                    campaign, supplemental.definition.heroId, supplemental.receipt);
            else
                host = Result<CampaignState>.Success(campaign);
            if (!host.IsSuccess) return host;
            var next = host.Value.WithSssV4090(
                sss.With(progression: supplemental.next));
            next = SssTenV4HostRewards090.MarkCreatorCode(
                next, supplemental.definition.code, "sss_v4_code_redeemed");
            return Result<CampaignState>.Success(next);
        }

        private static SssTenV4CodePreview090 BuildPreview(
            SssTenV4HeroDefinition090 hero,
            string codeId,
            bool primary,
            bool credit,
            bool weapon,
            bool owned,
            bool redeemed,
            string summary) =>
            new SssTenV4CodePreview090
            {
                Recognized = true,
                IsPrimaryRecruit = primary,
                IsAscensionCredit = credit,
                IsSignatureWeapon = weapon,
                HeroOwned = owned,
                AlreadyRedeemed = redeemed,
                CanRedeem = !redeemed && (primary || owned),
                CodeId = codeId,
                HeroId = hero.HeroId,
                HeroName = hero.DisplayName,
                RewardName = primary ? hero.DisplayName : credit
                    ? hero.DisplayName + " Ascension Credit"
                    : hero.WeaponName,
                Summary = summary
            };

        private static IReadOnlyList<string> OwnedIdentities(
            IReadOnlyList<RecruitState> recruits)
        {
            var identities = new List<string>();
            if (recruits != null)
                foreach (var recruit in recruits.Where(x => x != null))
                {
                    identities.Add(recruit.RecruitId);
                    if (!string.IsNullOrWhiteSpace(recruit.AuthoredStableRecruitId))
                        identities.Add(recruit.AuthoredStableRecruitId);
                    if (!string.IsNullOrWhiteSpace(recruit.SignatureId))
                        identities.Add(recruit.SignatureId);
                    if (!string.IsNullOrWhiteSpace(recruit.TutorialAliasId))
                        identities.Add(recruit.TutorialAliasId);
                }
            return identities;
        }
    }
}
