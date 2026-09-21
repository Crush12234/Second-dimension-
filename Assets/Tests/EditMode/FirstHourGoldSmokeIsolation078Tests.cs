using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation;
using SecondDimension.Presentation.Boot;
using SecondDimension.Presentation.FirstHour071;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class FirstHourGoldSmokeIsolation078Tests
    {
        private const string SmokeFlag = "--sd-first-hour-gold-smoke";
        private const string EvidencePrefix = "--sd-first-hour-evidence-dir=";
        private const string SavePrefix = "--sd-first-hour-save-path=";

        [Test]
        public void Release084ProofSchemaCertifiesTheBoardAndNotTheRetiredCorridor084()
        {
            var booleanProofFields = typeof(FirstHourGoldSmokeReport071)
                .GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Where(value => value.FieldType == typeof(bool))
                .Select(value => value.Name)
                .ToArray();

            Assert.That(booleanProofFields, Has.Length.GreaterThanOrEqualTo(38),
                "The established Release 084 proof fields may grow additively, but must not disappear.");
            Assert.That(new FirstHourGoldSmokeReport071().schema,
                Is.EqualTo("SECOND_DIMENSION_FIRST_HOUR_GOLD_SMOKE_084_1"));
            Assert.That(typeof(FirstHourGoldSmokeReport071).GetField(
                    nameof(FirstHourGoldSmokeReport071.applicationVersion)),
                Is.Not.Null);
            Assert.That(booleanProofFields,
                Does.Contain(nameof(FirstHourGoldSmokeReport071.expeditionBoardVerified)));
            Assert.That(booleanProofFields,
                Does.Contain(nameof(FirstHourGoldSmokeReport071.directGuildEntry091Verified)));
            Assert.That(booleanProofFields,
                Does.Not.Contain("walkableLanternRoadVerified"),
                "Release 084 keeps the repeated side-scroll corridor retired in favor of the board quest.");
            Assert.That(M1FlowPresenter.IsGuidedFirstHourBoardForVerification076(
                    GuildCityExpeditionService017D.FirstHourThreeBattleBoardId071),
                Is.False,
                "The obsolete field corridor must remain closed for the certified first-hour board.");
        }

        [Test]
        public void TransientLearnedArtCopyContractMatchesShippingNotice078()
        {
            const string heading = "NEW ART LEARNED";
            const string shippingDetail =
                "DAEVEN FELLSTAR LEARNED\nBLOODLESS PRESSURE\nREADY FOR FUTURE BATTLES";
            const string retiredDetail =
                "DAEVEN FELLSTAR LEARNED\nBLOODLESS PRESSURE\nPERMANENTLY UNLOCKED";

            Assert.That(
                FirstHourGoldSmoke071.HasReadableTransientLearnedArtCopy078(
                    heading,
                    shippingDetail),
                Is.True);
            Assert.That(
                FirstHourGoldSmoke071.HasReadableTransientLearnedArtCopy078(
                    heading,
                    retiredDetail),
                Is.False,
                "The packaged proof must track the compact shipping notice, not retired copy.");
            Assert.That(
                FirstHourGoldSmoke071.HasReadableTransientLearnedArtCopy078(
                    heading,
                    shippingDetail + "\nEXTRA"),
                Is.False,
                "The transient notice is an exact three-line player-facing contract.");
        }

        [Test]
        public void QuestDeckSmokeSelectsSafeUsableCardForExactStoryDestination090()
        {
            var nodes = new[]
            {
                new ExpeditionBoardNodeView074
                {
                    NodeId = "N10",
                    DisplayName = "Petra's Lantern Line"
                },
                new ExpeditionBoardNodeView074
                {
                    NodeId = "N09",
                    DisplayName = "Gatehouse Stalker"
                }
            };
            var lockedMerchant = new GuildQuestCardView090
            {
                CardId = "MERCHANT_LOCKED",
                Category = "MERCHANT",
                DestinationNodeId = "N10",
                DestinationLabel = "A new room",
                CanChoose = false
            };
            var optionalBattle = new GuildQuestCardView090
            {
                CardId = "OPTIONAL_BATTLE",
                Category = "BATTLE",
                DestinationNodeId = "N10",
                DestinationLabel = "A new room",
                CanChoose = true
            };
            var storyBoon = new GuildQuestCardView090
            {
                CardId = "STORY_BOON",
                Category = "BOON",
                DestinationNodeId = "N10",
                DestinationLabel = "A new room",
                CanChoose = true
            };
            var wrongRoute = new GuildQuestCardView090
            {
                CardId = "OTHER_ROUTE",
                Category = "XP",
                DestinationNodeId = "N09",
                DestinationLabel = "A new room",
                CanChoose = true
            };
            var cards = new[]
            {
                lockedMerchant,
                optionalBattle,
                storyBoon,
                wrongRoute
            };

            var selected = FirstHourGoldSmoke071
                .SelectQuestCardForDestinationForVerification090(
                    cards,
                    nodes,
                    "N10");

            Assert.That(selected, Is.SameAs(storyBoon),
                "The certified story route must prefer a usable reward/support card over an optional battle even when visible labels are ambiguous.");
            Assert.That(FirstHourGoldSmoke071
                    .SelectQuestCardForDestinationForVerification090(
                        cards,
                        nodes,
                        "N09"),
                Is.SameAs(wrongRoute),
                "The non-rendered route key must select the requested node even when cards share the same player-facing fallback label.");

            Assert.That(FirstHourGoldSmoke071
                    .SelectQuestCardForDestinationForVerification090(
                        new[]
                        {
                            optionalBattle,
                            new GuildQuestCardView090
                            {
                                CardId = "OPTIONAL_RECRUIT",
                                Category = "RECRUIT",
                                DestinationNodeId = "N10",
                                DestinationLabel = "A new room",
                                CanChoose = true
                            }
                        },
                        nodes,
                        "N10"),
                Is.Null,
                "The certified story smoke must never change its five-battle order or twenty-member roster through optional cards.");
        }

        [Test]
        public void Release089RequiresSeventyTwoFramesAndEarnedRecruitAscensionProof089()
        {
            var smokeSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.cs"));
            var buildSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Editor",
                "SecondDimension",
                "Release072",
                "FirstHourGoldWindowsBuild.cs"));
            var boardQuestSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "GuildCity017D",
                "M1FlowPresenter.BoardQuest081.cs"));
            var smokePresenterSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "M1FlowPresenter.FirstHourGoldSmoke071.cs"));
            var runtimeCoordinatorSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "M1RuntimeCoordinator.cs"));
            var recruitSmokeSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.RecruitAscension089.cs"));

            Assert.That(smokeSource, Does.Contain("_screenshotOrdinal == 75"));
            Assert.That(smokeSource, Does.Not.Contain("_screenshotOrdinal >= 75"));
            Assert.That(buildSource, Does.Contain("SmokeExpectedScreenshotCount = 75"));
            Assert.That(smokeSource, Does.Contain("_report.gates.Count == 58"));
            Assert.That(buildSource, Does.Contain("SmokeExpectedGateCount = 58"));
            Assert.That(smokeSource, Does.Contain(
                "three_card_choose_one_quest_deck_ch1_12_ch2_10_saved_receipts"));
            Assert.That(smokeSource, Does.Not.Contain(
                "five_card_one_tap_room_flip_quest_loop_over_authored_route"),
                "The gate ledger must describe the shipping choose-one deck and its exact saved round receipts, not the retired one-tap loop.");
            Assert.That(recruitSmokeSource, Does.Contain(
                "CommitExpeditionRouteCard089"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "new M1RuntimeCoordinator(contentRoot, savePath)"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "Merge Hero Duplicate Primary 089"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "ascensionAfter == ascensionBefore + 1"));
            Assert.That(recruitSmokeSource, Does.Contain(
                "HeroRosterCount089(afterMerge, firstPlan.Hero) == 1"));
            Assert.That(smokeSource, Does.Contain(
                "SmokeChapterBoardId084 = \"CH018_032\""));
            Assert.That(smokeSource, Does.Contain(
                "\"REPEAT020_SKYHOME_04\""));
            Assert.That(smokeSource, Does.Contain(
                "campaign023_authored_chapter_objective"));
            Assert.That(smokeSource, Does.Contain(
                "campaign023_authored_chapter_mission_brief"));
            Assert.That(smokeSource, Does.Contain(
                "campaign023_longest_12_room_contract_objective"));
            Assert.That(smokeSource, Does.Contain(
                "campaign023_longest_12_room_contract_track"));
            Assert.That(smokeSource, Does.Contain(
                "ProjectedExpeditionCards089("));
            Assert.That(smokeSource, Does.Contain(
                "CountActiveButtonsNamed076(\"Blind Quest Card Back \") == 3"));
            Assert.That(smokeSource, Does.Contain(
                "FindActiveButtonByName081(\"Move forward World Gate room 084\") == null"));
            Assert.That(smokeSource, Does.Contain(
                "c023_three_card_chapter_and_twelve_room_contract_at_certified_resolution"));
            Assert.That(smokeSource, Does.Contain("state.BoardObjective"));
            Assert.That(smokeSource, Does.Contain("state.CurrentNode.Objective"));
            Assert.That(smokeSource, Does.Contain("state.CurrentNode.StoryFlavor"));
            Assert.That(smokeSource, Does.Contain(
                "A future C023 room title leaked before its tile was revealed"));
            Assert.That(smokeSource, Does.Contain(
                "Raw C023 authority token leaked into player copy"));
            Assert.That(smokePresenterSource, Does.Contain(
                "ShowFirstHourGoldAdventureBoard084"));
            Assert.That(smokePresenterSource, Does.Contain(
                "BuildGuildCityWorldGate023(body, coordinator, state)"));
            Assert.That(smokePresenterSource, Does.Contain(
                "FocusFirstHourGoldAdventureMissionBrief084"));
            Assert.That(smokePresenterSource, Does.Contain(
                "FocusFirstHourGoldAdventureTrack084"));
            Assert.That(smokeSource,
                Does.Contain("_report.claimedBattleRewardCount == 5"));
            Assert.That(smokeSource,
                Does.Contain("_report.totalClaimedProgressionReceiptCount == 7"));
            Assert.That(smokeSource,
                Does.Contain("_report.resolvedCheckCount == 12"));
            Assert.That(smokeSource,
                Does.Contain("RequireQuestCardRounds090(12, \"Chapter 1\")"));
            Assert.That(smokeSource,
                Does.Contain("RequireQuestCardRounds090(10, \"Chapter 2\")"));
            Assert.That(smokeSource, Does.Contain(
                "departureRoot090.GetComponentsInChildren<Text>(includeInactive: false)"),
                "The one-objective gate must inspect the current Board Quest hierarchy, not every deferred UI object in the scene.");
            Assert.That(smokeSource, Does.Contain(
                "value.gameObject.name.StartsWith("),
                "The one-objective gate must accept the responsive-text suffix appended to the authored objective name.");
            Assert.That(smokeSource, Does.Contain(
                "\"Board Quest Goal 081\",\n                            StringComparison.Ordinal"),
                "The one-objective gate must use an ordinal prefix match for the authored objective name.");
            Assert.That(smokeSource, Does.Contain(
                "result090\n                .GetComponentsInChildren<Text>(includeInactive: false)"),
                "The card-result gate must inspect the current resolution hierarchy rather than exact scene-global object names.");
            Assert.That(smokeSource, Does.Contain(
                "\"Board Quest Resolved Card Title 090\",\n                    StringComparison.Ordinal"));
            Assert.That(smokeSource, Does.Contain(
                "\"Board Quest Resolved Card Reward 090\",\n                    StringComparison.Ordinal"));
            Assert.That(smokeSource, Does.Contain(
                "diorama090.Focus(certificationPlayerUnionId090, union090.UnionId);"),
                "Enemy Art certification must focus each authoritative enemy Union because the shipping diorama intentionally renders one enemy Union at a time.");
            Assert.That(smokeSource, Does.Contain(
                "diorama090.Focus(originalPlayerUnionId090, originalEnemyUnionId090);"),
                "Enemy Art certification must restore the player's original battle focus after cycling every enemy Union.");
            Assert.That(smokeSource, Does.Contain(
                "current Board Quest rendered \" + departureObjectives090.Length"),
                "A packaged failure must report the current-root objective count separately from the active-scene diagnostic count.");
            Assert.That(smokeSource,
                Does.Contain("yield return MoveExpeditionBoard078(\"N08\");"));
            Assert.That(smokeSource,
                Does.Contain("\"EVENT_FOG_ECHO\""));
            Assert.That(smokeSource,
                Does.Contain("chapter_2_fog_echo_seventh_return"));
            Assert.That(smokeSource,
                Does.Not.Contain("chapter_2_astrolabe_points_below"),
                "The required N08 proof reuses the former astrolabe screenshot slot so the exact 72-frame contract remains stable.");
            Assert.That(smokeSource,
                Does.Contain("ENCOUNTER_SURVEYOR_RESCUE"));
            Assert.That(smokeSource,
                Does.Contain("chapter_2_surveyors_at_door_rescue"));
            Assert.That(smokeSource,
                Does.Contain("chapter_2_testimony_to_skyhome_cliffhanger"));
            Assert.That(smokeSource,
                Does.Contain("chapter_3_hall_objective_after_surveyor_rescue"));
            Assert.That(smokeSource,
                Does.Contain("_report.chapterTwoRouteNodeId = \"N14\";"));
            Assert.That(smokeSource,
                Does.Contain("Chapter 3: Keep Skyhome's relief road open"));
            Assert.That(buildSource, Does.Contain("Smoke expected screenshots: exactly "));
            Assert.That(buildSource,
                Does.Contain("Release 084 exact screenshot-count gate"));
            Assert.That(smokeSource,
                Does.Contain("0.132.0-alpha"));
            Assert.That(smokeSource,
                Does.Contain("applicationVersion = Application.version"));
            Assert.That(buildSource,
                Does.Contain("SECOND-DIMENSION-ALPHA-132"));
            Assert.That(buildSource,
                Does.Contain("PlayerVersion = \"0.132.0-alpha\""));
            Assert.That(buildSource,
                Does.Contain("SECOND_DIMENSION_FIRST_HOUR_GOLD_SMOKE_084_1"));
            Assert.That(buildSource,
                Does.Contain("FIRST_HOUR_GOLD_SMOKE_084.json"));
            Assert.That(smokeSource, Does.Contain(
                "Debug.Log(\"FIRST HOUR GOLD BUILT PLAYER SMOKE 084 \" + " +
                "_report.status + \" • \" + _reportPath);"));
            Assert.That(buildSource, Does.Contain(
                "Release 084 exact live built-player completion expression"));
            Assert.That(smokeSource,
                Does.Contain("RequireChapterTwoCommitted2d6Resolution081"));
            Assert.That(buildSource,
                Does.Contain("RequireChapterTwoCommitted2d6Resolution081"));
            Assert.That(smokeSource,
                Does.Contain("primaryCopy.IndexOf('%') < 0"));
            Assert.That(buildSource,
                Does.Contain("primaryCopy.IndexOf('%') < 0"));
            Assert.That(smokeSource,
                Does.Not.Contain("RequireChapterTwoAuthoredResolution079"));
            Assert.That(buildSource,
                Does.Not.Contain("RequireChapterTwoAuthoredResolution079"));
            Assert.That(smokeSource,
                Does.Not.Contain("RequireVisibleButtonPrefixTextNotContains079"));
            Assert.That(buildSource,
                Does.Not.Contain("RequireVisibleButtonPrefixTextNotContains079"));
            Assert.That(smokeSource, Does.Contain(
                "ClickVisibleButtonByName076(\"Featured Contract Accept 062\");"));
            Assert.That(smokeSource, Does.Contain(
                "_coordinator.GuildCity017D.HasActiveContract &&\n" +
                "                       _coordinator.GuildCity017D.Expedition == null"));
            Assert.That(smokeSource, Does.Contain(
                "RequireFocusedVisibleAction076(\"BEGIN EXPEDITION\");"));
            Assert.That(smokeSource, Does.Contain(
                "ClickVisibleButtonByName076(\"First Contract Begin Expedition 062\");"));
            Assert.That(smokeSource, Does.Contain(
                "Require071(_coordinator.GuildCity017D.Expedition != null,"));
            Assert.That(smokeSource, Does.Not.Contain(
                "RequireFocusedVisibleAction076(\"START QUEST\\nPLACE PAWN AT GUILD DOOR\")"),
                "The accepted-contract Begin Expedition action already creates N00; smoke must not start the same expedition twice.");
            Assert.That(smokeSource, Does.Contain(
                "yield return WaitForQuestCardDraft090(\n" +
                "                \"the first-hour Board Quest departure\",\n" +
                "                3);"));
            Assert.That(smokeSource, Does.Contain(
                "departureQuestCards090.Length == 3"));
            Assert.That(smokeSource, Does.Contain(
                "SelectQuestCardForDestinationForVerification090("));
            Assert.That(runtimeCoordinatorSource, Does.Contain(
                "DestinationNodeId = value.DestinationNodeId"),
                "The production card projection must retain its non-rendered route authority for exact input selection.");
            Assert.That(smokeSource, Does.Contain(
                "\"Choose Board Quest Card \" + selectedCard090.CardId + \" 090\""));
            Assert.That(smokeSource, Does.Contain(
                "yield return WaitForBoardQuestCardResult090("));
            Assert.That(smokeSource, Does.Contain(
                "!StringComparer.Ordinal.Equals(value.Category, \"BATTLE\")"));
            Assert.That(smokeSource, Does.Contain(
                "!StringComparer.Ordinal.Equals(value.Category, \"RECRUIT\")"));
            Assert.That(smokeSource, Does.Not.Contain(
                "single MOVE FORWARD / FLIP NEXT ROOM action"),
                "Release 090 smoke must drive the shipping three-card quest table, not the retired one-tap route control.");
            for (var retiredMarker = 70; retiredMarker <= 83; retiredMarker++)
                Assert.That(smokeSource,
                    Does.Not.Contain("FIRST HOUR GOLD BUILT PLAYER SMOKE " +
                                     retiredMarker.ToString("000")));
            Assert.That(buildSource, Does.Contain(
                "for (var retiredMarker = 70; retiredMarker <= 83; retiredMarker++)"),
                "Release 084 build preflight must reject the retired Release 083 smoke marker.");
            Assert.That(buildSource, Does.Not.Contain("\"Release 083 "),
                "Current Release 084 preflight diagnostics must not identify failures as Release 083.");
            Assert.That(smokeSource, Does.Contain(
                "Board Quest Space \", StringComparison.Ordinal)) == 5"));
            Assert.That(smokeSource, Does.Contain(
                "five-space face-down room strip"));
            Assert.That(smokeSource, Does.Contain(
                "BoardTowerEnhancementCatalog001.LoadFromResources()"));
            Assert.That(smokeSource, Does.Contain(
                "if (BoardQuestRules081.CanSurfaceEnhancementRoomIdentity001(roomKind081))"));
            Assert.That(smokeSource, Does.Contain(
                "StringComparer.Ordinal.Equals(value, roomModuleFlag081)) == 1"));
            Assert.That(smokeSource, Does.Contain(
                "if (!nextQuestCardDraft090)\n" +
                "                RequireNamedVisibleTextEquals076(\n" +
                "                    \"Board Quest Revealed Reward Copy 081\",\n" +
                "                    expectedRewardCopy081);"),
                "Legacy room-copy presentation is required only when the next three-card draft has not already replaced it.");
            Assert.That(boardQuestSource, Does.Contain("\"Board Quest 081\""));
            Assert.That(boardQuestSource,
                Does.Not.Contain("\"Full Screen Expedition Board 074\""),
                "The packaged Board Quest proof must bind the real rendered root, never an empty legacy marker.");
            Assert.That(buildSource,
                Does.Contain("ValidateClosedWorldRuntimeDataDirectoryOrThrow"));
            Assert.That(buildSource,
                Does.Contain("RejectOwnerCreatorCodeArtifactsOrThrow"));
            Assert.That(buildSource,
                Does.Contain("CreatorGiveawayRegistry10000.ExpectedCodeCount"));
            Assert.That(buildSource,
                Does.Contain("BoardTowerEnhancementCatalog001.ExpectedItemCount001"));
            Assert.That(buildSource,
                Does.Not.Contain("SmokeExpectedMinimumScreenshotCount"));
        }

        [Test]
        public void Release084BuilderPinsBattleOnlyTowerContract084()
        {
            var buildSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Editor",
                "SecondDimension",
                "Release072",
                "FirstHourGoldWindowsBuild.cs"));
            var towerUiSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "Campaign022",
                "GuildCityFlowPresenter022.cs"));
            var towerBattleOnlyTestSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Tests",
                "PlayMode",
                "TowerBattleOnly088Tests.cs"));
            var smokeSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "SecondDimension",
                "Presentation",
                "FirstHour071",
                "FirstHourGoldSmoke071.cs"));
            var normalizedSmokeSource = smokeSource.Replace("\r\n", "\n");
            var towerSmokeStart = normalizedSmokeSource.IndexOf(
                "private IEnumerator CertifyPackagedTowerFloorOne081()",
                StringComparison.Ordinal);
            var towerSmokeEnd = normalizedSmokeSource.IndexOf(
                "private IEnumerator ResolveVisibleTowerBattle081()",
                StringComparison.Ordinal);
            Assert.That(towerSmokeStart, Is.GreaterThanOrEqualTo(0));
            Assert.That(towerSmokeEnd, Is.GreaterThan(towerSmokeStart));
            var towerSmokeSource = normalizedSmokeSource.Substring(
                towerSmokeStart,
                towerSmokeEnd - towerSmokeStart);

            Assert.That(towerUiSource, Does.Contain(
                "()=>BeginTowerBattleOnly088(c),"));
            Assert.That(towerUiSource, Does.Contain(
                "BuildTowerPrimaryAction084(body,c,s);"));
            Assert.That(towerUiSource, Does.Contain(
                "AdvanceTowerBattleOnlyTransitions088("));
            Assert.That(towerUiSource, Does.Contain(
                "YOUR NEXT UNION BATTLE"));
            Assert.That(towerUiSource, Does.Contain(
                "BANK VICTORY & UNLOCK NEXT FLOOR"));
            Assert.That(towerUiSource, Does.Not.Contain(
                    "BuildTowerAdventureTrack084(body,s);"),
                "The shipping Tower lobby must not route into the retired card-and-pawn track.");
            Assert.That(towerBattleOnlyTestSource, Does.Contain(
                "LobbyOffersOneBattleAndNoCardRoute088"));
            Assert.That(towerBattleOnlyTestSource, Does.Contain(
                "AssertNoCards088"));

            Assert.That(buildSource, Does.Contain(
                "battle-only Tower begin button wiring"));
            Assert.That(buildSource, Does.Contain(
                "BeginTowerBattleOnly088(c)"));
            Assert.That(buildSource, Does.Contain(
                "saved Tower bookend fast-forward authority"));
            Assert.That(buildSource, Does.Contain(
                "AdvanceTowerBattleOnlyTransitions088("));
            Assert.That(buildSource, Does.Contain(
                "plain-language battle-only Tower lobby action"));
            Assert.That(buildSource, Does.Contain(
                "YOUR NEXT UNION BATTLE"));
            Assert.That(buildSource, Does.Contain(
                "battle-only Tower victory banking action"));
            Assert.That(buildSource, Does.Contain(
                "BANK VICTORY & UNLOCK NEXT FLOOR"));
            Assert.That(buildSource, Does.Contain(
                "Tower card-track presentation routing"));
            Assert.That(buildSource, Does.Contain(
                "BuildTowerAdventureTrack084(body,s);"));
            Assert.That(buildSource, Does.Contain(
                "\"Assets/Tests/PlayMode/TowerBattleOnly088Tests.cs\""),
                "The battle-only Tower proof must be part of the certified source inventory.");
            Assert.That(buildSource, Does.Contain(
                "LobbyOffersOneBattleAndNoCardRoute088"));
            Assert.That(buildSource, Does.Contain(
                "AssertNoCards088"));
            Assert.That(smokeSource, Does.Contain(
                "Bank Tower battle victory 088"));
            Assert.That(smokeSource, Does.Contain(
                "battle-only Floor 1 action did not enter combat directly"));
            Assert.That(towerSmokeSource.Split(new[]
                {
                    "yield return WaitForTowerStateAndUi084("
                }, StringSplitOptions.None).Length - 1,
                Is.EqualTo(3),
                "Tower begin, battle-result return, and victory banking must each use a bounded state/UI wait.");
            Assert.That(normalizedSmokeSource, Does.Contain(
                "Func<CampaignProgressionPresentationState022, bool> statePredicate"));
            Assert.That(normalizedSmokeSource, Does.Contain(
                "Func<bool> uiPredicate"));
            Assert.That(towerSmokeSource, Does.Not.Contain(
                "beginFloor081.onClick.Invoke();\n            yield return null;\n            yield return null;"));
            Assert.That(towerSmokeSource, Does.Not.Contain(
                "ClickVisibleButtonByName076(\"Continue From Battle Results 072\");\n            yield return null;\n            yield return null;"));
            Assert.That(towerSmokeSource, Does.Not.Contain(
                "ClickVisibleButtonByName076(\"Bank Tower battle victory 088\");\n            yield return null;\n            yield return null;"));
            Assert.That(towerSmokeSource, Does.Contain(
                "The Floor 1 entry did not create the live battle controller."));
            Assert.That(towerSmokeSource, Does.Contain(
                "The Floor 1 battle diorama resolved the wrong backdrop resource key."));
            Assert.That(towerSmokeSource, Does.Contain(
                "The Floor 1 battle did not expose an interactable first Forecast order."));
            Assert.That(smokeSource, Does.Not.Contain(
                "Move forward Tower room 084"));
            Assert.That(smokeSource, Does.Not.Contain(
                "Save Tower battle tile 084"));
            Assert.That(smokeSource, Does.Not.Contain(
                "Reveal Tower battle tile 084"));
            Assert.That(smokeSource, Does.Not.Contain(
                "Bank Tower rewards and return 084"));
        }

        [Test]
        public void Release084EveryNonBuildingPreflightAcceptsCurrentProject084()
        {
            var builderType = Type.GetType(
                "SecondDimension.Editor.Release072.FirstHourGoldWindowsBuild, Assembly-CSharp-Editor",
                throwOnError: false);
            Assert.That(builderType, Is.Not.Null,
                "The canonical Release 084 Editor builder must be loaded for EditMode certification.");
            var validate = builderType.GetMethod(
                "ValidateEssentialsOrThrow",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(validate, Is.Not.Null,
                "The non-building Release 084 asset/source/hash preflight must remain callable by reflection.");

            try
            {
                validate.Invoke(null, null);
            }
            catch (TargetInvocationException exception)
            {
                var cause = exception.InnerException ?? exception;
                Assert.Fail(
                    "Release 084 non-building preflight rejected the current project:\n" +
                    cause);
            }
        }

        [Test]
        public void RuntimeSafeLibraryPacksArePinnedToEveryAuditedDataFile084()
        {
            var expected = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "Resources/SecondDimension/Creator10000/Data/CODE_SYSTEM_INTEGRATION_RULES_v1.csv", "D00FA379B32840A5A650B1D512AF11FCB6ED2B4262907BC4CB36487A223EA65B" },
                { "Resources/SecondDimension/Creator10000/Data/CREATOR_REWARD_BUNDLES_10000_v1.json", "AA16D14E105CFDE5C98B4A4791E62E71EC346B3FC61950475CD8F36894387D2C" },
                { "Resources/SecondDimension/Creator10000/Data/EXPEDITION_PREPARATION_ITEM_CATALOG_v1.csv", "C6164AB9133EA48962293B288E53743CD1F0B19A7DE62A16B63EFA513E4890D7" },
                { "Resources/SecondDimension/Creator10000/Data/NATURAL_REWARD_AND_SKILL_USE_HOOKS_v1.csv", "0081E6E8E09C407536DA248F068F32B4CF64ECC5EF60C91B70A0918673E6D49B" },
                { "Resources/SecondDimension/Creator10000/Data/RUNTIME_SAFE_CREATOR_CODE_HASH_MANIFEST_10000_v1.json", "F6AFAA0E4B6559BD05CC69930A5CFEA2B1B0F0E1EB110B6D2634D692DF3DE6EA" },
                { "Resources/SecondDimension/Creator10000/Data/WEAPON_GIVEAWAY_TEMPLATE_CATALOG_v1.csv", "222034FD27BAEA198FC9106B32771FFCCF3ADB1EAF7E59FEFED4E53AE54958AA" },
                { "Resources/SecondDimension/BoardTower001/Data/BOARD_TOWER_ITEM_CATALOG_144_001.json", "8A8FF4FE46E3B7B86FBD4BA28FA1FA8AC4A3453EE9A606997A3CBD75DCAC6851" },
                { "Resources/SecondDimension/BoardTower001/Data/ROOM_MODULE_CATALOG_72_001.json", "001C5114BFEAF747C133FEA6D438055D34AA6EE52FB88A0D024E310EACBE9174" },
                { "Resources/SecondDimension/BoardTower001/Data/RUN_EFFECT_CATALOG_72_001.json", "080D8D0176F9E2302E27B85CE0D810569154A372B986BEFD94CB3EF8F449552E" },
                { "Resources/SecondDimension/BoardTower001/Data/TURNING_POINT_SEEDS_24_001.json", "6D273078349C577C8E213C2FBB6BD5E0456D235D409523DE1AB212AED62F9808" },
                { "Resources/SecondDimension/BoardTower001/Data/NATURAL_REWARD_HOOKS_30_001.json", "24A936245A173E3FE1760EFB5C554B97311565C76BCE15640737A2CCB09AF138" },
                { "Resources/SecondDimension/BoardTower001/Data/P0_FIRST_HOUR_AND_TOWER_SUBSET_001.json", "1BF7BB70C66C1628CF4DC4C87F8F0CC4A6A7A1D71D726EC8EFCB8EB8FAD8F941" },
                { "Resources/SecondDimension/BoardTower001/Data/CONTENT_COUNTS_AND_VALIDATION_001.json", "35E8F47E61D72CECDF9E70412425EE14946845752CE5014D71F005E45DB80F1D" },
                { "Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_CATALOG_60_001.json", "9B463F49CD82E51984A8498B36252C09A3FE0BD780829DFB34144DE3F039E909" },
                { "Resources/SecondDimension/SpecialRelic001/Data/P0_SPECIAL_RELICS_12_001.json", "978B794447079C83739A06BCC5562B78CF5FB215D592D3AD096F9B198B306796" },
                { "Resources/SecondDimension/SpecialRelic001/Data/SPECIAL_RELIC_REWARD_HOOKS_001.csv", "F0C575E8853FA018372411436B00AECE20517726C266BCCC7CBEBEBF011C44B8" },
                { "Resources/SecondDimension/RelicCode1000/Data/RUNTIME_SAFE_RELIC_CODE_HASH_MANIFEST_1000_v1.json", "13D67DF4AF699DDD8B381F53AF87193D16808BD4D48AE407E5FBA2D951E7C299" },
                { "Resources/SecondDimension/RelicCode1000/Data/RELIC_CODE_REWARD_BUNDLES_64_v1.json", "642A84BE335385B3EDFB6CDCB368BBA2BD079F9186F4F586355F8378D8E5E5D4" }
            };
            var buildSource = File.ReadAllText(Path.Combine(
                Application.dataPath,
                "Editor",
                "SecondDimension",
                "Release072",
                "FirstHourGoldWindowsBuild.cs"));

            Assert.That(expected, Has.Count.EqualTo(18));
            Assert.That(buildSource, Does.Contain("PinnedRuntimeDataSha256"));
            Assert.That(buildSource, Does.Contain("changed after audit"));
            foreach (var pair in expected)
            {
                var absolute = Path.Combine(
                    Application.dataPath,
                    pair.Key.Replace('/', Path.DirectorySeparatorChar));
                Assert.That(File.Exists(absolute), Is.True, pair.Key);
                Assert.That(ComputeSha256081(absolute), Is.EqualTo(pair.Value), pair.Key);
                Assert.That(buildSource, Does.Contain("Assets/" + pair.Key), pair.Key);
                Assert.That(buildSource, Does.Contain(pair.Value), pair.Key);
            }
        }

        [Test]
        public void NormalBootUsesThePersonalFirstHourSave081()
        {
            var persistent = Path.Combine(Path.GetTempPath(), "sd081_personal_" + Guid.NewGuid().ToString("N"));
            var temporary = Path.Combine(Path.GetTempPath(), "sd081_temp_" + Guid.NewGuid().ToString("N"));

            var resolved = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                Array.Empty<string>(),
                persistent,
                temporary);

            Assert.That(resolved, Is.EqualTo(Path.GetFullPath(Path.Combine(
                persistent,
                "second_dimension_first_hour_slice_071.json"))));
        }

        [Test]
        public void SmokeBootAndSmokeWorkflowShareOneEvidenceContainedSave081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_isolated_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var temporary = Path.Combine(root, "temporary");
            var evidence = Path.Combine(root, "evidence");
            var save = Path.Combine(evidence, "state", "smoke.json");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + evidence,
                SavePrefix + save
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out var resolvedEvidence,
                out var resolvedSave,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.True, error);
            Assert.That(resolvedEvidence, Is.EqualTo(Path.GetFullPath(evidence)));
            Assert.That(resolvedSave, Is.EqualTo(Path.GetFullPath(save)));
            Assert.That(bootSave, Is.EqualTo(resolvedSave));
            Assert.That(bootSave, Is.Not.EqualTo(Path.GetFullPath(Path.Combine(
                persistent,
                       "second_dimension_first_hour_slice_071.json"))));
        }

        [Test]
        public void DefaultSmokeFallbackStaysOutsidePersistentData081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_default_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var temporary = Path.Combine(root, "temporary");
            var arguments = new[] { SmokeFlag };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out var evidence,
                out var save,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.True, error);
            Assert.That(PathsOverlap081(evidence, persistent), Is.False,
                "Default smoke evidence must never live in or contain personal persistent data.");
            Assert.That(PathsOverlap081(save, persistent), Is.False,
                "Default smoke save must never live in or contain personal persistent data.");
            Assert.That(IsSameOrChild081(save, evidence), Is.True);
            Assert.That(bootSave, Is.EqualTo(save));
        }

        [Test]
        public void EvidenceEqualToPersistentDataIsRejectedAndBootQuarantines081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_evidence_equal_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var temporary = Path.Combine(root, "temporary");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + persistent
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out _,
                out _,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.StartWith("Personal-data protection:"));
            Assert.That(error, Does.Contain("evidence"));
            AssertQuarantined081(bootSave, temporary);
        }

        [Test]
        public void EvidenceInsidePersistentDataIsRejectedAndBootQuarantines081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_evidence_child_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var evidence = Path.Combine(persistent, "forbidden-smoke-evidence");
            var temporary = Path.Combine(root, "temporary");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + evidence
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out _,
                out _,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.StartWith("Personal-data protection:"));
            Assert.That(error, Does.Contain("evidence"));
            AssertQuarantined081(bootSave, temporary);
        }

        [Test]
        public void EvidenceContainingPersistentDataIsRejectedAndBootQuarantines081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_evidence_parent_" + Guid.NewGuid().ToString("N"));
            var evidence = Path.Combine(root, "evidence");
            var persistent = Path.Combine(evidence, "personal");
            var temporary = Path.Combine(root, "temporary");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + evidence
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out _,
                out _,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.StartWith("Personal-data protection:"));
            Assert.That(error, Does.Contain("either direction"));
            AssertQuarantined081(bootSave, temporary);
        }

        [Test]
        public void PersonalSaveOverrideIsRejectedAndBootQuarantinesInstead081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_rejected_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var temporary = Path.Combine(root, "temporary");
            var evidence = Path.Combine(root, "evidence");
            var personalSave = Path.Combine(
                persistent,
                "second_dimension_first_hour_slice_071.json");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + evidence,
                SavePrefix + personalSave
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out _,
                out _,
                out var error);
            var bootSave = BootCoordinator.ResolveCoordinatorSavePathForVerification078(
                arguments,
                persistent,
                temporary);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.StartWith("Personal-data protection:"));
            Assert.That(error, Does.Contain("smoke save"));
            Assert.That(bootSave, Is.Not.EqualTo(Path.GetFullPath(personalSave)));
            AssertQuarantined081(bootSave, temporary);
        }

        [Test]
        public void SaveOutsideEvidenceRemainsRejected081()
        {
            var root = Path.Combine(Path.GetTempPath(), "sd081_outside_evidence_" + Guid.NewGuid().ToString("N"));
            var persistent = Path.Combine(root, "personal");
            var evidence = Path.Combine(root, "evidence");
            var unrelatedSave = Path.Combine(root, "unrelated", "smoke.json");
            var arguments = new[]
            {
                SmokeFlag,
                EvidencePrefix + evidence,
                SavePrefix + unrelatedSave
            };

            var valid = FirstHourGoldSmoke071.TryResolveIsolatedPaths078(
                arguments,
                persistent,
                out _,
                out _,
                out var error);

            Assert.That(valid, Is.False);
            Assert.That(error, Does.Contain("inside its evidence directory"));
        }

        [Test]
        public void ClosedWorldManifestAcceptsEveryListedFileIncludingEmpty081()
        {
            var root = CreateManifestRoot081("valid");
            try
            {
                WritePackagedFile081(root, "SECOND_DIMENSION_GUILD_OF_WORLDS.exe", "player");
                WritePackagedFile081(root, "Data/Managed/Assembly-CSharp.dll", "assembly");
                WritePackagedFile081(root, "Data/empty-runtime-marker.bin", string.Empty);
                var manifestPath = WriteManifest081(root,
                    "SECOND_DIMENSION_GUILD_OF_WORLDS.exe",
                    "Data/Managed/Assembly-CSharp.dll",
                    "Data/empty-runtime-marker.bin");

                var verified = FirstHourGoldSmoke071
                    .VerifyClosedWorldBuildManifestForVerification081(root, manifestPath);

                Assert.That(verified, Has.Count.EqualTo(3));
                Assert.That(verified.Keys,
                    Does.Contain("Data/empty-runtime-marker.bin"));
            }
            finally
            {
                DeleteManifestRoot081(root);
            }
        }

        [Test]
        public void ClosedWorldManifestRejectsUnlistedPackagedFile081()
        {
            var root = CreateManifestRoot081("unlisted");
            try
            {
                WritePackagedFile081(root, "listed.bin", "listed");
                var manifestPath = WriteManifest081(root, "listed.bin");
                WritePackagedFile081(root, "injected-after-manifest.bin", "unlisted");

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    FirstHourGoldSmoke071.VerifyClosedWorldBuildManifestForVerification081(
                        root,
                        manifestPath));

                Assert.That(exception.Message,
                    Does.Contain("files missing from BUILD_SHA256.txt"));
                Assert.That(exception.Message,
                    Does.Contain("injected-after-manifest.bin"));
            }
            finally
            {
                DeleteManifestRoot081(root);
            }
        }

        [Test]
        public void ClosedWorldManifestRejectsMissingPackagedFile081()
        {
            var root = CreateManifestRoot081("missing");
            try
            {
                var missingPath = WritePackagedFile081(
                    root,
                    "removed-after-manifest.bin",
                    "present");
                var manifestPath = WriteManifest081(root, "removed-after-manifest.bin");
                File.Delete(missingPath);

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    FirstHourGoldSmoke071.VerifyClosedWorldBuildManifestForVerification081(
                        root,
                        manifestPath));

                Assert.That(exception.Message,
                    Does.Contain("lists a missing packaged file"));
                Assert.That(exception.Message,
                    Does.Contain("removed-after-manifest.bin"));
            }
            finally
            {
                DeleteManifestRoot081(root);
            }
        }

        [Test]
        public void ClosedWorldManifestRejectsTamperedPackagedFile081()
        {
            var root = CreateManifestRoot081("tampered");
            try
            {
                var packagedPath = WritePackagedFile081(root, "tampered.bin", "original");
                var manifestPath = WriteManifest081(root, "tampered.bin");
                File.WriteAllText(packagedPath, "tampered");

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    FirstHourGoldSmoke071.VerifyClosedWorldBuildManifestForVerification081(
                        root,
                        manifestPath));

                Assert.That(exception.Message,
                    Does.Contain("hash does not match"));
                Assert.That(exception.Message, Does.Contain("tampered.bin"));
            }
            finally
            {
                DeleteManifestRoot081(root);
            }
        }

        private static string CreateManifestRoot081(string label)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "sd081_manifest_" + label + "_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static string WritePackagedFile081(
            string root,
            string relativePath,
            string content)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? root);
            File.WriteAllText(path, content ?? string.Empty);
            return path;
        }

        private static string WriteManifest081(string root, params string[] relativePaths)
        {
            var lines = new List<string>();
            foreach (var relativePath in relativePaths)
            {
                var normalized = relativePath.Replace('\\', '/');
                var fullPath = Path.Combine(
                    root,
                    normalized.Replace('/', Path.DirectorySeparatorChar));
                lines.Add(ComputeSha256081(fullPath) + "  " + normalized);
            }
            var manifestPath = Path.Combine(root, "BUILD_SHA256.txt");
            File.WriteAllLines(manifestPath, lines);
            return manifestPath;
        }

        private static string ComputeSha256081(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var algorithm = SHA256.Create())
                return BitConverter.ToString(algorithm.ComputeHash(stream))
                    .Replace("-", string.Empty);
        }

        private static void DeleteManifestRoot081(string root)
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }

        private static void AssertQuarantined081(string bootSave, string temporary)
        {
            Assert.That(bootSave, Does.StartWith(Path.GetFullPath(temporary)));
            Assert.That(bootSave,
                Does.Contain("SECOND_DIMENSION_FIRST_HOUR_SMOKE_QUARANTINE"));
        }

        private static bool PathsOverlap081(string left, string right) =>
            IsSameOrChild081(left, right) || IsSameOrChild081(right, left);

        private static bool IsSameOrChild081(string candidate, string parent)
        {
            var normalizedCandidate = Path.GetFullPath(candidate)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedParent = Path.GetFullPath(parent)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return StringComparer.OrdinalIgnoreCase.Equals(normalizedCandidate, normalizedParent) ||
                   normalizedCandidate.StartsWith(
                       normalizedParent + Path.DirectorySeparatorChar,
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}
