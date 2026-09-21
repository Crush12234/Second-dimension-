using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using SecondDimension.Gameplay.M2;
using SecondDimension.Gameplay.Recruitment.AutoGeneration010;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Determinism;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.State;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Release030;
using SecondDimension.Save;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor
{
    [Serializable]
    public sealed class EarnedRecruitAssessmentReport094
    {
        public string status = "RUNNING", source, sourceSha256, isolatedSave, failure;
        public string initialHash, finalHash, startedUtc, finishedUtc;
        public string scope = "Actual earned chapter/opening reward claims and legal ten-by-six Union assignment on an isolated earned save. No injected recruits, XP, health, or victory; no battle-balance claim.";
        public int initialOwned, finalOwned, initialCapacity, finalCapacity, deployed, unions;
        public int assigned097, actualBattleProjected097;
        public int completedCatalogChapters, chapterRewards, openingRewards, cardRewards, claimedNew;
        public int reloadChecks;
        public long initialXp, finalXp;
        public List<string> newlyJoined = new List<string>();
        public List<string> commands = new List<string>();
        public string equipmentScope = "All newly earned heroes: real item-slot legality, canonical weapon family, known learned-action equipment requirements and personal MP. Deployed subset additionally uses actual read-only M2 member projection and Forecast candidates at its real AP/MP. No battle launched or resolved; no claim every skill is usable in every context.";
        public int newlyEquipped, newlyArmored, newlyWithLegalLearnedAction, newlyBattleProjected, newlyWithForecastCandidate;
        public int newlyWithLearnedForecast097, newlyWithWeaponRoot097;
        public bool oldLoadoutsUnchanged, equipmentInspectionDidNotMutate;
        public List<EarnedRecruitEquipmentRow094> equipment = new List<EarnedRecruitEquipmentRow094>();
    }

    [Serializable]
    public sealed class EarnedRecruitEquipmentRow094
    {
        public string recruitId, stableId, name, expectedFamily, unionId = "", status = "NOT_CHECKED";
        public bool legalHand, legalArmor, allAssignmentsLegal, battleProjected;
        public int starterItems, ownedUpgradeItems, projectedAp, currentMp;
        public string[] equippedItems = Array.Empty<string>(), unknownArts = Array.Empty<string>();
        public string[] equipmentLegalLearnedActions = Array.Empty<string>(), equipmentBlockedLearnedActions = Array.Empty<string>();
        public string[] personalMpReadyLearnedActions = Array.Empty<string>(), actualForecastCandidates = Array.Empty<string>();
        public string weaponRoot097 = "";
        public bool weaponRootRetained097;
        public string[] actualLearnedForecastCandidates097 = Array.Empty<string>();
    }

    public sealed class EarnedRecruitSixtyAssessment094
    {
        AtomicSaveStore _store = new AtomicSaveStore();
        M1RuntimeCoordinator _coordinator;
        EarnedRecruitAssessmentReport094 _report;
        string _contentRoot, _directory;

        public static void Run()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                string Value(string prefix) => args.FirstOrDefault(value =>
                    value.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length) ?? "";
                var result = new EarnedRecruitSixtyAssessment094().Assess(
                    Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT"),
                    Value("--sd-earned-source="), Value("--sd-earned-evidence="));
                Debug.Log("EARNED RECRUITS 094 " + result.status + " owned=" + result.finalOwned +
                    " deployed=" + result.deployed + " " + result.failure);
                EditorApplication.Exit(result.status == "PASS" ? 0 : 94);
            }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(95); }
        }

        public EarnedRecruitAssessmentReport094 Assess(string contentRoot, string source,
            string evidenceDirectory)
        {
            _contentRoot = contentRoot;
            _directory = Path.GetFullPath(evidenceDirectory);
            // Reuse the proven isolation guard; filename is deliberately the
            // same standard canonical save filename accepted by the deep runner.
            var isolated = DeepCampaignVerification093.IsolatedSavePath093(source, _directory);
            Require(File.Exists(source), "Actual earned source save is missing.");
            _report = new EarnedRecruitAssessmentReport094 { source = Path.GetFullPath(source),
                sourceSha256 = Hash(source), isolatedSave = isolated,
                startedUtc = DateTime.UtcNow.ToString("O") };
            Directory.CreateDirectory(_directory);
            File.Copy(source, isolated, false);
            try
            {
                Reload();
                var initial = Read();
                var campaign = initial.CampaignState;
                _report.initialHash = initial.CanonicalStateHash;
                _report.initialOwned = campaign.Guild.Recruits.Count;
                _report.initialXp = campaign.Guild.TreasuryXp;
                _report.initialCapacity = _coordinator.GuildCity017D.RosterCapacity;
                _report.completedCatalogChapters = campaign.Guild.GuildCity.Strategic017H
                    .Campaign019.CompletedChapterIds.Count;
                var view = _coordinator.EarnedCampaignRecruits094;
                _report.chapterRewards = view.PendingChapterRecruits;
                _report.openingRewards = view.PendingOpeningStoryRecruits;
                _report.cardRewards = view.PendingCardRecruits;
                Require(view.BlockedChapterRecruits == 0, "Some old chapters lack sufficient authentic proof.");
                Require(view.CanClaim, "Earned rewards are not claimable: " + view.Summary);
                var oldIds = new HashSet<string>(campaign.Guild.Recruits.Select(value => value.RecruitId));
                var expected = view.ClaimableCount;
                Command("Claim earned campaign recruits", () => _coordinator.ClaimEarnedCampaignRecruits094());
                campaign = Read().CampaignState;
                var added = campaign.Guild.Recruits.Where(value => !oldIds.Contains(value.RecruitId)).ToArray();
                Require(added.Length == expected, "Claim count differs from exact authoritative preview.");
                Require(campaign.Guild.TreasuryXp == _report.initialXp, "Earned claim changed XP.");
                Require(added.Select(value => value.AuthoredStableRecruitId).Distinct().Count() == added.Length,
                    "A reward counted a duplicate rather than a new hero.");
                _report.claimedNew = added.Length;
                _report.newlyJoined.AddRange(added.Select(value => value.RecruitId + "|" + value.AuthoredStableRecruitId + "|" + value.DisplayName));
                Reload();
                var hash = Read().CanonicalStateHash;
                Command("Replay earned claim (must be no-op)", () => _coordinator.ClaimEarnedCampaignRecruits094());
                Require(Read().CanonicalStateHash == hash, "Replay altered claimed rewards.");
                FillSixty();
                Reload();
                campaign = Read().CampaignState;
                Require(campaign.Guild.Unions.Count == 10 &&
                    campaign.Guild.Unions.All(value => value.MemberRecruitIds.Count == 6),
                    "Actual commands did not establish ten full legal Unions.");
                Require(campaign.Guild.Unions.SelectMany(value => value.MemberRecruitIds)
                    .Distinct().Count() == 60, "A recruit was deployed twice.");
                Require(campaign.Guild.TreasuryXp == _report.initialXp,
                    "Claiming/assigning campaign rewards unexpectedly spent XP.");
                InspectNewEquipment094(initial.CampaignState, campaign, oldIds);
                _report.status = "PASS";
            }
            catch (Exception exception) { _report.status = "BLOCKED"; _report.failure = exception.ToString(); }
            finally
            {
                _report.finishedUtc = DateTime.UtcNow.ToString("O");
                try
                {
                    var final = Read();
                    _report.finalHash = final.CanonicalStateHash;
                    _report.finalOwned = final.CampaignState.Guild.Recruits.Count;
                    _report.finalXp = final.CampaignState.Guild.TreasuryXp;
                    _report.finalCapacity = new GuildCityEffectService017D().RosterCapacity(final.CampaignState.Guild.Development);
                    _report.assigned097 = final.CampaignState.Guild.Unions.Sum(value => value.MemberRecruitIds.Count);
                    _report.deployed = EarnedDeploymentSixtyVerification097.DeployableAssigned097(final.CampaignState);
                    _report.unions = final.CampaignState.Guild.Unions.Count;
                    Require(Hash(_report.source) == _report.sourceSha256, "Original source save changed.");
                }
                catch (Exception exception) { _report.status = "BLOCKED"; _report.failure += "\n" + exception; }
                File.WriteAllText(Path.Combine(_directory, "earned_recruits094_report.json"), JsonUtility.ToJson(_report, true));
            }
            return _report;
        }

        void InspectNewEquipment094(CampaignState initial, CampaignState campaign, HashSet<string> oldIds)
        {
            var beforeHash = CanonicalJson.Sha256Hex(campaign);
            var beforeSaveHash = Read().CanonicalStateHash;
            // Inspect the same configured content authority used by this live
            // coordinator, not a separately loaded and potentially unbound copy.
            var contentField097 = typeof(M1RuntimeCoordinator).GetField("_combatContent",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var content = contentField097?.GetValue(_coordinator) as M2CombatContent;
            Require(content != null, "The live coordinator combat authority is unavailable.");
            var generator = new RecruitAutoGenerator010(
                RecruitAutoGenerationCatalog010.LoadFromContentRoot(_contentRoot));
            var families = JObject.Parse(File.ReadAllText(Path.Combine(_contentRoot,
                "CONTENT_AUTHORITY_002", "DATA", "WEAPON_FAMILIES_12_PRESERVED.json")))
                ["weaponFamilies"].Cast<JObject>().ToDictionary(value => value.Value<string>("id"));
            // Cached for this inspection, these invoke the shipping authority.
            // No copied legality formula or fabricated battle/roster is used.
            var equipmentLegal = typeof(M2BattleCommandService).GetMethod("IsEquipmentLegal",
                BindingFlags.Static | BindingFlags.NonPublic);
            var project = typeof(M2BattleCommandService).GetMethod("CreatePlayerUnions",
                BindingFlags.Static | BindingFlags.NonPublic);
            Require(equipmentLegal != null && project != null, "Real battle equipment/projection verifier is unavailable.");
            var projections = (IReadOnlyList<BattleUnionState>)project.Invoke(null,
                new object[] { campaign, content, null, 10 });
            _report.actualBattleProjected097 = projections.Sum(union => union.Members.Count);
            var assigned097 = campaign.Guild.Unions.SelectMany(union => union.MemberRecruitIds)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var projected097 = projections.SelectMany(union => union.Members).Select(member => member.MemberId)
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Require(projected097.Length == 60 && assigned097.SequenceEqual(projected097),
                "Sixty assigned slots did not produce the exact same sixty legal battle members.");
            _report.oldLoadoutsUnchanged = initial.Guild.Recruits.All(old =>
                campaign.Guild.Recruits.Any(current => current.RecruitId == old.RecruitId &&
                    CanonicalJson.Serialize(current.Equipment) == CanonicalJson.Serialize(old.Equipment)));
            foreach (var recruit in campaign.Guild.Recruits.Where(value => !oldIds.Contains(value.RecruitId))
                .OrderBy(value => value.RecruitId, StringComparer.Ordinal))
            {
                var generated097 = generator.Generate(recruit);
                var family = generated097.FixedWeaponFamilyId;
                var requiredTags = families[family]["equipmentTagsGranted"].Values<string>().ToArray();
                var handSlot = family == "WEAPON_FAMILY_SHIELD" ? EquipmentSlotIds.OffHand : EquipmentSlotIds.MainHand;
                var hand = recruit.Equipment.Find(handSlot)?.Item;
                var armor = recruit.Equipment.Find(EquipmentSlotIds.BodyArmor)?.Item;
                var tags = recruit.Equipment.Assignments.SelectMany(value => value.Item.EquipmentTags)
                    .Distinct(StringComparer.Ordinal).ToArray();
                var row = new EarnedRecruitEquipmentRow094
                {
                    recruitId = recruit.RecruitId, stableId = recruit.AuthoredStableRecruitId,
                    name = recruit.DisplayName, expectedFamily = family, currentMp = recruit.CurrentMp,
                    weaponRoot097 = content.DeepProgression.Tree(generated097.WeaponTreeId).RootNodeId,
                    legalHand = hand != null && SssTenV4Inventory090.CanEquip(recruit, hand, handSlot, out _) &&
                        requiredTags.Any(hand.EquipmentTags.Contains),
                    legalArmor = armor != null && SssTenV4Inventory090.CanEquip(recruit, armor, EquipmentSlotIds.BodyArmor, out _),
                    allAssignmentsLegal = recruit.Equipment.Assignments.All(value =>
                        SssTenV4Inventory090.CanEquip(recruit, value.Item, value.SlotId, out _)),
                    starterItems = recruit.Equipment.Assignments.Count(value => value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")),
                    ownedUpgradeItems = recruit.Equipment.Assignments.Count(value => !value.Item.EquipmentTags.Contains("RECRUIT_STARTER_094")),
                    equippedItems = recruit.Equipment.Assignments.Select(value => value.SlotId + "|" +
                        value.Item.InstanceId + "|" + value.Item.DisplayName + "|" + string.Join(",", value.Item.EquipmentTags)).ToArray(),
                    unknownArts = recruit.Progression.LearnedArtIds.Where(id => !content.Arts.ContainsKey(id)).ToArray()
                };
                var learnedActions = recruit.Progression.LearnedArtIds.Where(content.Arts.ContainsKey)
                    .Select(content.Art).Where(art => art.IsForecastAction).ToArray();
                row.equipmentLegalLearnedActions = learnedActions.Where(art =>
                    (bool)equipmentLegal.Invoke(null, new object[] { art, tags })).Select(art => art.Id).ToArray();
                row.equipmentBlockedLearnedActions = learnedActions.Select(art => art.Id)
                    .Except(row.equipmentLegalLearnedActions).ToArray();
                row.personalMpReadyLearnedActions = row.equipmentLegalLearnedActions
                    .Where(id => content.Art(id).PersonalMpCost <= recruit.CurrentMp).ToArray();
                var projectedUnion = projections.FirstOrDefault(union => union.Members.Any(member => member.MemberId == recruit.RecruitId));
                if (projectedUnion != null)
                {
                    row.battleProjected = true;
                    row.unionId = projectedUnion.UnionId;
                    row.projectedAp = projectedUnion.CurrentAp;
                    var member = projectedUnion.Members.Single(value => value.MemberId == recruit.RecruitId);
                    row.actualForecastCandidates = content.Commands.Keys.OrderBy(value => value, StringComparer.Ordinal)
                        .SelectMany(command => M2BattleCommandService.CandidateArtsForVerification090(
                            member, command, content, projectedUnion.CurrentAp, null, projectedUnion.UnionId)
                            .Where(member.LearnedArtIds.Contains).Select(id => command + "|" + id)).Distinct().ToArray();
                    row.weaponRootRetained097 = member.LearnedArtIds.Contains(row.weaponRoot097);
                    row.actualLearnedForecastCandidates097 = row.actualForecastCandidates.Where(value =>
                        row.equipmentLegalLearnedActions.Contains(value.Substring(value.IndexOf('|') + 1))).ToArray();
                }
                row.status = row.legalHand && row.legalArmor && row.allAssignmentsLegal &&
                    row.unknownArts.Length == 0 && row.equipmentLegalLearnedActions.Length > 0 &&
                    (!row.battleProjected || (row.weaponRootRetained097 &&
                        row.actualLearnedForecastCandidates097.Length > 0)) ? "PASS" : "BLOCKED";
                _report.equipment.Add(row);
            }
            _report.newlyEquipped = _report.equipment.Count(row => row.legalHand && row.allAssignmentsLegal);
            _report.newlyArmored = _report.equipment.Count(row => row.legalArmor);
            _report.newlyWithLegalLearnedAction = _report.equipment.Count(row => row.equipmentLegalLearnedActions.Length > 0);
            _report.newlyBattleProjected = _report.equipment.Count(row => row.battleProjected);
            _report.newlyWithForecastCandidate = _report.equipment.Count(row => row.battleProjected && row.actualForecastCandidates.Length > 0);
            _report.newlyWithLearnedForecast097 = _report.equipment.Count(row => row.battleProjected && row.actualLearnedForecastCandidates097.Length > 0);
            _report.newlyWithWeaponRoot097 = _report.equipment.Count(row => row.battleProjected && row.weaponRootRetained097);
            _report.equipmentInspectionDidNotMutate = CanonicalJson.Sha256Hex(campaign) == beforeHash &&
                Read().CanonicalStateHash == beforeSaveHash;
            Require(_report.oldLoadoutsUnchanged, "Claiming new heroes changed an existing hero's equipment.");
            Require(_report.equipmentInspectionDidNotMutate, "Read-only equipment inspection mutated state/save.");
            Require(_report.equipment.Count == _report.claimedNew, "Equipment evidence omitted a newly joined hero.");
            Require(_report.equipment.All(row => row.status == "PASS"),
                "Newly earned equipment/Art readiness failed: " +
                string.Join(", ", _report.equipment.Where(row => row.status != "PASS").Select(row => row.stableId)));
        }

        void FillSixty()
        {
            EarnedDeploymentSixtyVerification097.ReturnFullHealthRecovery097(
                _coordinator, () => Read().CampaignState, _report.commands.Add);
            foreach (var id in Read().CampaignState.Guild.Recruits.Select(value => value.RecruitId).ToArray())
            {
                var campaign = Read().CampaignState;
                var assigned = campaign.Guild.Unions.SelectMany(value => value.MemberRecruitIds).ToArray();
                if (assigned.Length >= 60)
                {
                    Require(EarnedDeploymentSixtyVerification097.DeployableAssigned097(campaign) == 60,
                        "The filled Union plan includes an unavailable member.");
                    return;
                }
                if (assigned.Contains(id)) continue;
                if (GuildMemberDeploymentPolicy017D.IsTraining(campaign.Guild.GuildCity, id))
                    Command("Return training member to active: " + id,
                        () => _coordinator.SetGuildCityAssignment017D(id, "Active"));
                campaign = Read().CampaignState;
                if (!GuildMemberDeploymentPolicy017D.IsRecruitDeployable(campaign.Guild.GuildCity, id)) continue;
                if (campaign.Guild.Unions.All(value => value.MemberRecruitIds.Count >= 6))
                {
                    Command("Add legal Union", () => _coordinator.AddUnion());
                    campaign = Read().CampaignState;
                }
                var index = Enumerable.Range(0, campaign.Guild.Unions.Count)
                    .First(value => campaign.Guild.Unions[value].MemberRecruitIds.Count < 6);
                var slot = campaign.Guild.Unions[index].MemberRecruitIds.Count;
                Command("Assign " + id + " to union " + index + " slot " + slot,
                    () => _coordinator.AssignRecruitToUnion(id, index, slot));
            }
            throw new InvalidOperationException("Not enough legitimately owned recruits for sixty deployment.");
        }

        void Command(string label, Func<M1CommandResult> command)
        { var result = command(); Require(result.Succeeded, label + ": " + result.Message); _report.commands.Add(label); }
        void Reload()
        {
            var before = Read().CanonicalStateHash;
            _coordinator = new M1RuntimeCoordinator(_contentRoot, _report.isolatedSave);
            Require(_coordinator.State.CanonicalStateHash == before && Read().CanonicalStateHash == before,
                "Loading changed canonical earned state.");
            _report.reloadChecks++;
        }
        SaveEnvelopeV1 Read()
        { var result = _store.ReadWithRecovery(_report.isolatedSave); Require(result.IsSuccess, string.Join("; ", result.Errors));
          Require(CanonicalJson.Sha256Hex(result.Value.CampaignState) == result.Value.CanonicalStateHash, "Invalid save hash."); return result.Value; }
        static string Hash(string path)
        { using (var stream = File.OpenRead(path)) using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", ""); }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
