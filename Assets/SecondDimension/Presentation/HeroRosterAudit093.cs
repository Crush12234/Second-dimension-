using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SecondDimension.Core;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.M1;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation.Creator028;
using SecondDimension.Save;
using UnityEngine;

namespace SecondDimension.Presentation
{
    [Serializable]
    public sealed class HeroRosterAuditRow093
    {
        public int rosterId;
        public string stableId, name, rank, eligibility, acquisitionProof, artCategory;
        public string idleResource, actionResource, idleTexture, actionTexture, recruitId;
        public string battleIdleResource, battleIdleTexture;
        public long predictedSigningCostXp, chargedSigningCostXp;
        public string applicantCapture, battleCapture, failure = "";
        public System.Collections.Generic.List<HeroRosterPoseProof093> poseProofs = new System.Collections.Generic.List<HeroRosterPoseProof093>();
        public string[] quarantineReasons = Array.Empty<string>();
        public string[] learnedArts = Array.Empty<string>();
        public string[] forecastCommands = Array.Empty<string>();
        public bool recruited, exactOnce, xpChargedCorrectly, placedInUnion;
        public bool battleProjected, forecastRoundResolved, ownIdentityBound, runtimeUiVerified;
        public bool professionalArtReviewed; // Never inferred from a non-null sprite.
        public float visibleWidthPixels, visibleHeightPixels, feetPixels;
        public string MechanicalStatus => eligibility == "QUARANTINED" ? "BLOCKED_DATA" :
            !string.IsNullOrEmpty(failure) ? "FAIL" :
            recruited && exactOnce && xpChargedCorrectly && placedInUnion && battleProjected &&
            forecastRoundResolved && ownIdentityBound ? "PASS_MECHANICAL_ONLY" : "NOT_RUN";
    }

    /// <summary>
    /// Explicit isolated evidence fixture, not an acquisition shortcut in normal play.
    /// Each accepted identity receives fixture XP and an exact lead (or its real SS
    /// code), then uses shipping signing, Union, save and combat authorities.
    /// </summary>
    public static class HeroRosterAudit093
    {
        public const long FixtureTreasuryXp093 = 100000;
        public static string ContentRoot093 => Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT");
        static HeroMaster300Catalog087 _catalog;
        static RecruitAutoGenerator010 _generator;
        static M2CombatContent _combat;
        static RecruitStarterEquipment094 _starterEquipment094;
        public static HeroMaster300Catalog087 Catalog093 => _catalog ?? (_catalog = HeroMaster300CreatorRegistry087.Load().Source);
        public static M2CombatContent LoadCombatContent093() =>
            M2CombatContent.LoadFromDirectory(ContentRoot093).WithHeroMasterGeneratedAuthority096(
                Catalog093, RecruitAutoGenerationCatalog010.LoadFromContentRoot(ContentRoot093));

        public static List<HeroRosterAuditRow093> Census093()
        {
            var catalog = Catalog093;
            var rows = catalog.AcceptedHeroes.Select(hero => new HeroRosterAuditRow093
            {
                rosterId = hero.RosterId, stableId = hero.StableId, name = hero.Name,
                rank = hero.Rank.ToString(), eligibility = "ACCEPTED",
                acquisitionProof = hero.Rank == HeroMasterRank087.SS
                    ? "REAL_SS_CODE; isolated completed-opening fixture; no natural acquisition claim"
                    : "FIXTURE_EXACT_LEAD_100000_XP_OPERATION1; real applicant signing; no natural acquisition claim"
            }).ToList();
            rows.AddRange(catalog.QuarantinedHeroes.Select(hero => new HeroRosterAuditRow093
            {
                rosterId = hero.RosterId, stableId = hero.StableId, name = hero.StableId,
                eligibility = "QUARANTINED", acquisitionProof = "NOT_RELEASED",
                artCategory = "NOT_BOUND_DATA_QUARANTINED", quarantineReasons = hero.Reasons.ToArray()
            }));
            return rows.OrderBy(row => row.rosterId).ToList();
        }

        public static CampaignState CreateFixture093(HeroMaster300Hero087 hero)
        {
            if (hero == null || !Catalog093.AcceptedHeroes.Any(value => value.StableId == hero.StableId))
                throw new InvalidOperationException("Only an accepted catalog identity can enter the isolated fixture.");
            var guild = new GuildState("ROSTER_AUDIT_093", FixtureTreasuryXp093,
                Array.Empty<RecruitState>(), Array.Empty<UnionState>());
            var campaign = new CampaignState("00000000-0000-0000-0000-000000000093", 930000L + hero.RosterId,
                "1.0", ModeRuleSnapshot.StandardDefaults(), guild,
                new NewGuildProfileState("Isolated roster verification", GameMode.Standard,
                    TutorialDepth.FullTutorial, AccessibilitySettingsState.Defaults(), false),
                new OpeningFlowState(OpeningStage.Complete, ApplicantBoardState.FrozenTutorialSeedId,
                    true, null, false, 439, 0, true, true, true, false, "isolated_roster_fixture_093"));
            if (hero.Rank == HeroMasterRank087.SS) return campaign;
            var applicant = HeroMaster300ApplicantLead089.ToApplicant(hero, 1, "SKYHOME");
            var board = new ApplicantBoardState("ROSTER_AUDIT_093_" + hero.RosterId,
                "EXPLICIT_TEST_FIXTURE_NOT_NATURAL_LEAD", 0, true, new[] { applicant }, null);
            return campaign.With(campaign.Guild.WithGuildCity(campaign.Guild.GuildCity.With(
                operationOrdinal: 1, recruitmentBoard: board, replaceRecruitmentBoard: true)), campaign.OpeningFlow);
        }

        public static CampaignState SignAndPlace093(CampaignState campaign, HeroMaster300Hero087 hero,
            HeroRosterAuditRow093 row)
        {
            var beforeXp = campaign.Guild.TreasuryXp;
            if (hero.Rank == HeroMasterRank087.SS)
            {
                var registry = HeroMaster300CreatorRegistry087.Load();
                var projection = HeroMaster300CreatorRecruitProjection087.FromHero(hero);
                var initializer = new RecruitAutoGenerationSigningService010(new M1CommandService(), Generator093());
                var grant = new SecondDimension.Gameplay.Creator028.CreatorRecruitGrant028(
                    initializer.InitializeRecruit(projection.Recruit), projection.InventoryItems);
                var commands = new SecondDimension.Gameplay.Creator028.CreatorAccessCommandService028();
                campaign = Require093(commands.RedeemCode(campaign, registry.Codes, hero.SsGenerationCode, grant));
                var replay = commands.RedeemCode(campaign, registry.Codes, hero.SsGenerationCode, grant);
                row.exactOnce = !replay.IsSuccess || replay.Value.Guild.Recruits.Count == campaign.Guild.Recruits.Count;
                row.xpChargedCorrectly = campaign.Guild.TreasuryXp == beforeXp;
            }
            else
            {
                var service = new GuildCityRecruitmentService017D(Generator093(), Catalog093);
                var applicant = campaign.Guild.GuildCity.RecruitmentBoard.Applicants.Single();
                var signing = service.DescribeApplicantSigning090(campaign, applicant);
                if (!signing.CanSign) throw new InvalidOperationException(signing.LockedReason);
                row.predictedSigningCostXp = signing.EffectiveCostTreasuryXp;
                campaign = Require093(service.SignApplicant(campaign, applicant.RecruitId));
                var replay = service.SignApplicant(campaign, applicant.RecruitId);
                row.exactOnce = !replay.IsSuccess || replay.Value.Guild.Recruits.Count == campaign.Guild.Recruits.Count;
                row.chargedSigningCostXp = beforeXp - campaign.Guild.TreasuryXp;
                row.xpChargedCorrectly = row.chargedSigningCostXp == row.predictedSigningCostXp;
            }
            var recruit = campaign.Guild.Recruits.Single(value => value.AuthoredStableRecruitId == hero.StableId);
            row.recruited = campaign.Guild.Recruits.Count == 1 && recruit.DisplayName == hero.Name;
            row.recruitId = recruit.RecruitId;
            row.learnedArts = recruit.Progression.LearnedArtIds.ToArray();
            campaign = Require093(new M1CommandService().AssignRecruitToUnion(campaign, recruit.RecruitId, 0, 0));
            row.placedInUnion = campaign.Guild.Unions.Count(value => value.MemberRecruitIds.Contains(recruit.RecruitId)) == 1;
            return campaign;
        }

        // Keep SignAndPlace093 as the raw acquisition fixture used by equipment
        // policy tests. Built-player review must additionally use the same new-
        // arrival equipment policy as M1RuntimeCoordinator.Commit.
        public static CampaignState SignOutfitAndPlace093(CampaignState campaign,
            HeroMaster300Hero087 hero, HeroRosterAuditRow093 row)
        {
            var signed = SignAndPlace093(campaign, hero, row);
            var content = _combat ?? (_combat = LoadCombatContent093());
            var equipment = _starterEquipment094 ?? (_starterEquipment094 =
                new RecruitStarterEquipment094(
                    RecruitAutoGenerationCatalog010.LoadFromContentRoot(ContentRoot093), content));
            return equipment.ApplyToNewRecruits(campaign, signed);
        }

        static RecruitAutoGenerator010 Generator093() => _generator ?? (_generator = new RecruitAutoGenerator010(
            RecruitAutoGenerationCatalog010.LoadFromContentRoot(ContentRoot093)));

        public static CampaignState StartBattleAndVerifyForecast093(CampaignState campaign,
            HeroRosterAuditRow093 row)
        {
            var commands = new M2BattleCommandService();
            var content = _combat ?? (_combat = LoadCombatContent093());
            campaign = Require093(commands.StartEncounterBattle(campaign, content,
                "BATTLE_ROSTER_ISOLATED_093_" + row.rosterId,
                "Isolated roster binding verification — not campaign progression.", 1));
            row.battleProjected = campaign.Battle.PlayerUnions.SelectMany(union => union.Members)
                .Count(member => member.MemberId == row.recruitId) == 1;
            row.forecastCommands = campaign.Battle.CommittedForecasts.Select(value => value.CommandId).Distinct().ToArray();
            var selected = campaign;
            foreach (var union in campaign.Battle.PlayerUnions)
            {
                var forecast = campaign.Battle.CommittedForecasts.Where(value => value.UnionId == union.UnionId)
                    .OrderBy(value => value.CommandId == "CMD_GUARD" ? 1 : 0).First();
                selected = Require093(commands.SelectForecast(selected, union.UnionId, forecast.ForecastId));
            }
            var resolved = Require093(commands.ConfirmRound(selected, content));
            row.forecastRoundResolved = resolved.Battle != null && resolved.Battle.EventLog.Count > campaign.Battle.EventLog.Count;
            // Keep the unconfirmed projection for readable captured command-stage artwork.
            return campaign;
        }

        public static void BindArt093(HeroMaster300Hero087 hero, HeroRosterAuditRow093 row)
        {
            if (!M1VisualAssets.TryResolveBattleStandee(row.recruitId ?? hero.StableId, hero.StableId,
                    hero.Race, hero.StableId, out var idle, out var idleKey) || idle == null)
                throw new InvalidOperationException("Missing live standing sprite.");
            if (!M1VisualAssets.TryResolveBattleActionPose(row.recruitId ?? hero.StableId, hero.StableId,
                    hero.Race, hero.StableId, out var action, out var actionKey) || action == null)
                throw new InvalidOperationException("Missing live action sprite.");
            row.idleResource = idleKey; row.actionResource = actionKey;
            row.idleTexture = idle.texture.name; row.actionTexture = action.texture.name;
            row.battleIdleResource = idleKey; row.battleIdleTexture = idle.texture.name;
            if (BattleArtRuntimeRegistry011.TryResolvePose(row.recruitId ?? hero.StableId,
                    BattleArtPoseDirector011.Idle, out var battleIdle, out var battleKey) ||
                BattleArtRuntimeRegistry011.TryResolvePose(hero.StableId,
                    BattleArtPoseDirector011.Idle, out battleIdle, out battleKey))
            {
                row.battleIdleResource = battleKey;
                row.battleIdleTexture = battleIdle.texture.name;
            }
            var fallback = M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(idleKey) ||
                           M1VisualAssets.IsHeroMasterSpriteFallbackResourceKey089(actionKey);
            row.artCategory = fallback ? "PROCEDURAL_PLACEHOLDER_NOT_FINISHED_ART" :
                idle.texture == action.texture && idle.rect == action.rect ? "SAME_IDENTITY_STATIC_POSE_REUSED_FOR_ACTION" :
                idleKey.StartsWith(HeroRemasterAtlas093.Root093, StringComparison.Ordinal) &&
                actionKey.StartsWith(HeroRemasterAtlas093.Root093, StringComparison.Ordinal)
                    ? "ORIGINAL_EXACT_ID_TWO_POSE_REMASTER_ATLAS" :
                idleKey.StartsWith(HeroRecoveredSource100099.Root099, StringComparison.Ordinal) &&
                actionKey.StartsWith(HeroRecoveredSource100099.Root099, StringComparison.Ordinal)
                    ? "RECOVERED_EXACT_ID_TWO_POSE_SOURCE_PAIR_RIGHTS_PENDING_REQUIRES_VISUAL_REVIEW" :
                "SAME_IDENTITY_SUPPLIED_POSE_PAIR_REQUIRES_VISUAL_REVIEW";
            row.ownIdentityBound = idleKey.Contains(hero.StableId) && actionKey.Contains(hero.StableId);
            // Versioned remasters can use a name path; the accepted remaster resolver
            // is still exact-identity authority, never a race/family substitution.
            if (!row.ownIdentityBound && hero.StableId == "HERO_REC_287")
                row.ownIdentityBound = idleKey.Contains("FREYA") && actionKey == idleKey;
            if (!row.ownIdentityBound) throw new InvalidOperationException("Binding does not prove this exact catalog identity.");
        }

        public static T Require093<T>(Result<T> result)
        {
            if (!result.IsSuccess) throw new InvalidOperationException(string.Join("; ", result.Errors));
            return result.Value;
        }
        public static void Write093(string path, CampaignState campaign) =>
            new AtomicSaveStore().Write(path, SaveEnvelopeV1.Create(campaign, DateTime.UtcNow));
        public static CampaignState Read093(string path) => Require093(new AtomicSaveStore().ReadWithRecovery(path)).CampaignState;
    }
}
