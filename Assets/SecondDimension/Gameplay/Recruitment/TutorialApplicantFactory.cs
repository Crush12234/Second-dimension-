using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace SecondDimension.Gameplay.Recruitment
{
    /// <summary>
    /// Frozen New Guild tutorial board. Authoring slots 1..6 map to board indices 0..5.
    /// The two tutorial stable IDs are aliases, not replacements for canonical content IDs.
    /// </summary>
    public sealed class TutorialApplicantFactory
    {
        public const string TutorialRootSeed = "SDGOW_TUTORIAL_V1_001";
        public const string TutorialBoardId = "BOARD_TUTORIAL_V1_001";
        public const string MarenTutorialAlias = "SIG_MAREN_HOLT";
        public const string OdeliaTutorialAlias = "SIG_ODELIA_FEN";

        private static readonly string[] FrozenRaces =
        {
            "HUMAN", "ORC", "GOBLIN", "DOG_TRIBE", "DARK_ELF", "DEMON_HERITAGE"
        };

        private static readonly TutorialSignatureAlias[] FrozenAliases =
        {
            new TutorialSignatureAlias(MarenTutorialAlias, "SIG_W01_01", "SIGREC_MAREN_HOLT"),
            new TutorialSignatureAlias(OdeliaTutorialAlias, "SIG_W01_03", "SIGREC_ODELIA_FEN")
        };

        private static readonly TutorialApplicantAuthoritySlot[] FrozenAuthoritySlots =
        {
            new TutorialApplicantAuthoritySlot(1, 0, "PROCEDURAL", "TUT_APPLICANT_001", null, null, null, "Guardian"),
            new TutorialApplicantAuthoritySlot(2, 1, "PROCEDURAL", "TUT_APPLICANT_002", null, null, null, "Warrior"),
            new TutorialApplicantAuthoritySlot(3, 2, "PROCEDURAL", "TUT_APPLICANT_003", null, null, null, "Ranger"),
            new TutorialApplicantAuthoritySlot(4, 3, "PROCEDURAL", "TUT_APPLICANT_004", null, null, null, "Priest"),
            new TutorialApplicantAuthoritySlot(5, 4, "SIGNATURE_ELIGIBLE", null, MarenTutorialAlias, "SIG_W01_01", "SIGREC_MAREN_HOLT", "Guardian/Leader"),
            new TutorialApplicantAuthoritySlot(6, 5, "SIGNATURE_ELIGIBLE", null, OdeliaTutorialAlias, "SIG_W01_03", "SIGREC_ODELIA_FEN", "Priest/Support")
        };

        private static readonly ProceduralTutorialSlot[] ProceduralSlots =
        {
            new ProceduralTutorialSlot(1, 0, "TUT_APPLICANT_001", "Guardian", "CLASS_GUARDIAN", 9, "PROC_36344E2400DC98B6"),
            new ProceduralTutorialSlot(2, 1, "TUT_APPLICANT_002", "Warrior", "CLASS_WARRIOR", 5, "PROC_F85A4CAA747BC8C6"),
            new ProceduralTutorialSlot(3, 2, "TUT_APPLICANT_003", "Ranger", "CLASS_RANGER", 7, "PROC_5B14E7816E55FFB5"),
            new ProceduralTutorialSlot(4, 3, "TUT_APPLICANT_004", "Priest", "CLASS_PRIEST", 10, "PROC_748DD03A23E1FEB0")
        };

        private readonly RecruitmentContent _content;
        private readonly OpeningRecruitGenerator _procedural;
        private readonly SignatureRecruitMaterializer _signatures;
        private readonly DetailedApplicantBoardGenerator _boards;

        public TutorialApplicantFactory(RecruitmentContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _procedural = new OpeningRecruitGenerator(content);
            _signatures = new SignatureRecruitMaterializer(content);
            _boards = new DetailedApplicantBoardGenerator(content);
        }

        public static IReadOnlyList<TutorialSignatureAlias> SignatureAliases => Array.AsReadOnly(FrozenAliases);
        public static IReadOnlyList<TutorialApplicantAuthoritySlot> AuthoritySlots => Array.AsReadOnly(FrozenAuthoritySlots);

        public static TutorialSignatureAlias ResolveSignatureAlias(string tutorialStableId)
        {
            for (var i = 0; i < FrozenAliases.Length; i++)
                if (StringComparer.Ordinal.Equals(FrozenAliases[i].TutorialStableId, tutorialStableId))
                    return FrozenAliases[i];
            throw new KeyNotFoundException("Unknown tutorial Signature alias: " + tutorialStableId);
        }

        public DetailedApplicantBoard CreateFrozenBoard()
        {
            if (!StringComparer.Ordinal.Equals(_content.ProceduralContentVersion, "OPENING_PROCEDURAL_TABLES_0.6")
                || !StringComparer.Ordinal.Equals(_content.SignatureContentVersion, "OPENING_SIGNATURE_RECRUITS_0.6"))
                throw new InvalidOperationException("The frozen tutorial factory requires the 0.6 opening recruitment authority.");

            var baseOffice = _boards.GetOfficeState(0, 0, 0);
            var office = new RecruitmentOfficeState
            {
                Tier = baseOffice.Tier,
                BoardSize = 6,
                ScoutingAccuracyBase = baseOffice.ScoutingAccuracyBase,
                SignatureBonusBasisPoints = baseOffice.SignatureBonusBasisPoints,
                FreeRefreshesPerDay = baseOffice.FreeRefreshesPerDay,
                ManualRefreshBaseXp = baseOffice.ManualRefreshBaseXp,
                TargetedScouting = baseOffice.TargetedScouting,
                DryStreak = 0,
                RefreshIndexToday = 0
            };

            var applicants = new ApplicantSlotDetailed[6];
            for (var i = 0; i < ProceduralSlots.Length; i++)
            {
                var authority = ProceduralSlots[i];
                var recruit = _procedural.Generate(new ProceduralRecruitRequest
                {
                    CampaignSeed = TutorialRootSeed,
                    GuildDay = 1,
                    RefreshIndex = 0,
                    SlotIndex = authority.BoardSlotIndex,
                    SourceChannel = "GUILD_BOARD",
                    UnlockedRaces = FrozenRaces,
                    ExtraSalt = authority.Seed + "|ROLE_MATCH|" + authority.MatchAttempt.ToString("D3", CultureInfo.InvariantCulture)
                });
                if (!StringComparer.Ordinal.Equals(recruit.StartingClassId, authority.StartingClassId)
                    || !StringComparer.Ordinal.Equals(recruit.RecruitId, authority.ExpectedRecruitId))
                    throw new InvalidOperationException("Frozen tutorial procedural authority drifted at authoring slot " + authority.AuthoringSlot + ".");
                applicants[authority.BoardSlotIndex] = CreateSlot(
                    authority.BoardSlotIndex, "GUILD_BOARD", recruit, office);
            }

            for (var aliasIndex = 0; aliasIndex < FrozenAliases.Length; aliasIndex++)
            {
                var alias = FrozenAliases[aliasIndex];
                var signatureRecord = RecruitmentDeterminism.FindSignature(_content.Signatures, alias.SignatureId);
                if (!StringComparer.Ordinal.Equals(signatureRecord["stableRecruitId"].Value<string>(), alias.StableRecruitId))
                    throw new InvalidOperationException("Tutorial Signature alias no longer resolves to its frozen recruit ID.");
                var recruit = _signatures.Materialize(TutorialRootSeed, alias.SignatureId, "GUILD_BOARD");
                var expectedRecruitId = aliasIndex == 0 ? "SIGI_4559425B6CF6CBE6" : "SIGI_CE2768FD5A985F8B";
                var expectedSigningCost = aliasIndex == 0 ? 101 : 89;
                if (!StringComparer.Ordinal.Equals(recruit.RecruitId, expectedRecruitId)
                    || recruit.SigningCostXp != expectedSigningCost)
                    throw new InvalidOperationException("Frozen tutorial Signature instance drifted for " + alias.TutorialStableId + ".");
                var boardIndex = 4 + aliasIndex;
                applicants[boardIndex] = CreateSlot(boardIndex, "GUILD_BOARD", recruit, office);
            }

            var identityPayload = new JObject
            {
                ["tutorialRootSeed"] = TutorialRootSeed,
                ["applicantIds"] = new JArray(applicants.Select(x => x.RecruitId)),
                ["aliasIds"] = new JArray(FrozenAliases.Select(x => x.TutorialStableId))
            };
            return new DetailedApplicantBoard
            {
                BoardId = TutorialBoardId,
                GenerationSeed = RecruitmentDeterminism.SeedString(
                    TutorialRootSeed, "TUTORIAL_APPLICANT_BOARD",
                    _content.ProceduralContentVersion, _content.SignatureContentVersion),
                StateHash = RecruitmentDeterminism.StableHash(identityPayload),
                CampaignSeedHash = RecruitmentDeterminism.StableHash(TutorialRootSeed, 12),
                GuildDay = 1,
                RefreshIndex = 0,
                WorldIds = Array.AsReadOnly(new[] { "WORLD_GATE_01" }),
                SignatureChanceBasisPoints = 0,
                DryStreakBefore = 0,
                DryStreakAfter = 0,
                MercyCharterTriggered = false,
                OfficeState = office,
                Applicants = Array.AsReadOnly(applicants),
                Committed = true,
                ExpiresAfterGuildDay = 2
            };
        }

        public IReadOnlyList<ApplicantSlotDetailed> CreateFrozenApplicants() => CreateFrozenBoard().Applicants;

        private ApplicantSlotDetailed CreateSlot(
            int boardSlotIndex,
            string sourceChannel,
            OpeningRecruitRecord recruit,
            RecruitmentOfficeState office)
        {
            return new ApplicantSlotDetailed
            {
                SlotIndex = boardSlotIndex,
                SourceType = recruit.SourceType,
                SourceChannel = sourceChannel,
                Guaranteed = true,
                RecruitId = recruit.RecruitId,
                SignatureId = recruit.SignatureId,
                SigningCostXp = recruit.SigningCostXp,
                ApplicantRecord = recruit,
                ScoutingReport = _procedural.CreateScoutingReport(
                    recruit, office.Tier, office.ScoutingAccuracyBase, 0)
            };
        }

        private sealed class ProceduralTutorialSlot
        {
            public ProceduralTutorialSlot(
                int authoringSlot,
                int boardSlotIndex,
                string seed,
                string roleHint,
                string startingClassId,
                int matchAttempt,
                string expectedRecruitId)
            {
                AuthoringSlot = authoringSlot;
                BoardSlotIndex = boardSlotIndex;
                Seed = seed;
                RoleHint = roleHint;
                StartingClassId = startingClassId;
                MatchAttempt = matchAttempt;
                ExpectedRecruitId = expectedRecruitId;
            }

            public int AuthoringSlot { get; }
            public int BoardSlotIndex { get; }
            public string Seed { get; }
            public string RoleHint { get; }
            public string StartingClassId { get; }
            public int MatchAttempt { get; }
            public string ExpectedRecruitId { get; }
        }
    }
}
