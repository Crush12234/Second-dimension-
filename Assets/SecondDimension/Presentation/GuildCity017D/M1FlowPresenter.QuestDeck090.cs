using System;
using System.Linq;
using SecondDimension.Presentation.Campaign023;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Full-width, phone-readable presentation for the authoritative first-hour
    /// quest deck. Gameplay and save authority remain in
    /// GuildCityExpeditionService017D; this partial only deals, flips and submits
    /// one of its three legal cards.
    /// </summary>
    public sealed partial class M1FlowPresenter
    {
        GuildQuestCardView090 _lastBoardQuestCard090;
        bool _boardQuestCardAwaitingAcknowledgement090;

        private static bool ShouldBuildBoardQuestCardDraft090(
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            ExpeditionBoardView074 view) =>
            view != null &&
            view.ActionKind == ExpeditionBoardActionKind074.ChooseRoute &&
            coordinator is IBoardQuestDeckCoordinator090 &&
            state?.QuestCards090 != null &&
            state.QuestCards090.Count == 3;

        private bool ShouldShowBoardQuestCardResolution090(
            GuildCityPresentationState017D state) =>
            _lastBoardQuestCard090 != null &&
            (ShouldHoldBoardQuestAction081(state) ||
             (_boardQuestCardAwaitingAcknowledgement090 &&
              state?.Expedition != null));

        private static bool ShouldRequireBoardQuestCardAcknowledgement090(
            bool isPlaying,
            bool reducedMotion) =>
            isPlaying && reducedMotion;

        private RectTransform BuildBoardQuestCardDraft090(
            Transform parent,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state)
        {
            var context = new GameObject(
                    "Board Quest Three Card Draft 090",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            context.SetParent(parent, false);
            SetAnchors074(context,
                new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.795f));

            var panel = RuntimeUi.AddPanel(
                context,
                "Board Quest Deck Table 090",
                new Color(0.004f, 0.012f, 0.024f, 0.16f));
            Stretch(panel.rectTransform);

            if (ShouldShowBoardQuestCardResolution090(state))
            {
                BuildBoardQuestCardResolution090(panel.transform, state);
                if (!_boardQuestCardAwaitingAcknowledgement090)
                    ScheduleBoardQuestActionUnlock081(
                        state?.Expedition?.CurrentNodeId);
                return context;
            }

            var headingPanel = RuntimeUi.AddPanel(
                panel.transform,
                "Board Quest Card Round Heading 090",
                new Color(0.004f, 0.012f, 0.024f, 0.72f));
            SetAnchors074(headingPanel.rectTransform,
                new Vector2(0.07f, 0.83f), new Vector2(0.93f, 0.985f));

            var heading = RuntimeUi.AddText(
                headingPanel.transform,
                "Board Quest Card Round Label 090",
                QuestCardRoundHeading090(state),
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            SetAnchors074(heading.rectTransform,
                new Vector2(0.025f, 0.65f), new Vector2(0.62f, 0.98f));
            ConfigureAuthoredCompactText076(heading, 19, 29);

            var dealStatus = RuntimeUi.AddText(
                headingPanel.transform,
                "Board Quest Shuffle Deal Status 090",
                _reducedMotion
                    ? "THREE FACE-DOWN CARDS READY"
                    : "SHUFFLING THE QUEST DECK…",
                20,
                TextAnchor.MiddleRight,
                RuntimeUi.Warning,
                FontStyle.Bold);
            SetAnchors074(dealStatus.rectTransform,
                new Vector2(0.62f, 0.65f), new Vector2(0.975f, 0.98f));
            ConfigureAuthoredCompactText076(dealStatus, 15, 21);

            var boardView = ExpeditionBoardProjection074.Build(state);
            var currentRoom = (boardView?.Nodes ??
                               Array.Empty<ExpeditionBoardNodeView074>())
                .FirstOrDefault(value => value != null && value.IsCurrent);
            var currentMoment = currentRoom == null
                ? "Choose one card. Its result saves, then your pawn moves."
                : "NOW  •  " +
                  (currentRoom.DisplayName ?? "The next room").ToUpperInvariant() +
                  "\n" + CompactBoardQuestCopy081(
                      BoardQuestCurrentMomentSummary081(state, currentRoom),
                      150);
            var instruction = RuntimeUi.AddText(
                headingPanel.transform,
                "Expedition Current Position Summary 074",
                currentMoment,
                23,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Normal);
            SetAnchors074(instruction.rectTransform,
                new Vector2(0.025f, 0.08f), new Vector2(0.975f, 0.62f));
            ConfigureAuthoredCompactText076(instruction, 20, 25);

            if (!string.IsNullOrWhiteSpace(_localStatus) &&
                !_localStatusPositive)
            {
                instruction.text = "NEEDS ATTENTION  •  " + _localStatus;
                instruction.color = RuntimeUi.Error;
            }

            var row = new GameObject(
                    "Board Quest Three Card Row 090",
                    typeof(RectTransform))
                .GetComponent<RectTransform>();
            row.SetParent(panel.transform, false);
            SetAnchors074(row,
                new Vector2(0.11f, 0.020f), new Vector2(0.89f, 0.815f));
            RuntimeUi.AddHorizontalLayout(
                row,
                new RectOffset(6, 6, 5, 5),
                20f,
                TextAnchor.MiddleCenter);
            var blindChoice = row.gameObject.AddComponent<ExpeditionCardChoice091>();
            blindChoice.Configure091(!_reducedMotion && Application.isPlaying, dealStatus);

            var cards = (state?.QuestCards090 ??
                         Array.Empty<GuildQuestCardView090>())
                .Take(3)
                .ToArray();
            for (var cardIndex = 0; cardIndex < cards.Length; cardIndex++)
                BuildBoardQuestCard090(
                    row,
                    coordinator,
                    state,
                    cards[cardIndex],
                    cardIndex,
                    blindChoice);
            return context;
        }

        private void BuildBoardQuestCard090(
            Transform row,
            IGuildCityPresentationCoordinator017D coordinator,
            GuildCityPresentationState017D state,
            GuildQuestCardView090 card,
            int cardIndex,
            ExpeditionCardChoice091 blindChoice)
        {
            if (card == null) return;
            var wrapper = RuntimeUi.AddPanel(
                row,
                "Board Quest Card Slot " + cardIndex + " 090",
                Color.clear);
            RuntimeUi.SetLayout(
                wrapper,
                preferredHeight: 680f,
                flexibleWidth: 1f);

            var surface = RuntimeUi.AddPanel(
                wrapper.transform,
                "Board Quest Card Surface " + card.CardId + " 090",
                Color.white);
            Stretch(surface.rectTransform);
            M1PremiumUi.StylePanel(surface, QuestCardSurface090(card.Category));
            RuntimeUi.AddVerticalLayout(
                surface.transform,
                new RectOffset(12, 12, 8, 8),
                3f,
                TextAnchor.UpperCenter);

            var motion = wrapper.gameObject
                .AddComponent<ExpeditionRouteCardMotion089>();
            motion.Configure089(
                cardIndex,
                card.CardId,
                !_reducedMotion);

            var category = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Category " + card.CardId + " 090",
                QuestCardCategoryLabel090(card) +
                "  •  " + (string.IsNullOrWhiteSpace(card.RiskLabel)
                    ? "REVEAL"
                    : card.RiskLabel.ToUpperInvariant()),
                20,
                TextAnchor.MiddleCenter,
                QuestCardAccent090(card.Category),
                FontStyle.Bold);
            RuntimeUi.SetLayout(category, preferredHeight: 22f);
            ConfigureAuthoredCompactText076(category, 13, 19);

            GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                surface.transform,
                "Board Quest Card Art " + card.CardId + " 090",
                card.VisualResourcePath,
                120f);

            var title = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Title " + card.CardId + " 090",
                (card.Title ?? "Quest Card").ToUpperInvariant(),
                25,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            RuntimeUi.SetLayout(title, preferredHeight: 36f);
            ConfigureAuthoredCompactText076(title, 17, 24);

            var description = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Description " + card.CardId + " 090",
                CompactBoardQuestCopy081(
                    card.Description,
                    92),
                18,
                TextAnchor.MiddleCenter,
                RuntimeUi.Text,
                FontStyle.Normal);
            RuntimeUi.SetLayout(description, preferredHeight: 32f);
            ConfigureAuthoredCompactText076(description, 13, 18);
            description.gameObject.SetActive(card.CanChoose);

            var power = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Exact Power " + card.CardId + " 090",
                QuestCardExactPower090(card, state?.TreasuryXp ?? 0),
                18,
                TextAnchor.MiddleCenter,
                card.CanChoose ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            RuntimeUi.SetLayout(power, preferredHeight: 32f);
            ConfigureAuthoredCompactText076(power, 13, 18);

            var reward = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Exact Reward " + card.CardId + " 090",
                "YOU GET  •  " + CompactBoardQuestCopy081(
                    card.RewardPreview,
                    96),
                18,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(reward, preferredHeight: 36f);
            ConfigureAuthoredCompactText076(reward, 13, 18);

            var route = RuntimeUi.AddText(
                surface.transform,
                "Board Quest Card Destination " + card.CardId + " 090",
                QuestCardRoutePreview090(card),
                16,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            RuntimeUi.SetLayout(route, preferredHeight: 24f);
            ConfigureAuthoredCompactText076(route, 13, 17);
            // The board already displays the current route and objective. Keep
            // this duplicate route label out of the physical card face.
            route.gameObject.SetActive(false);

            if (!card.CanChoose)
            {
                var locked = RuntimeUi.AddText(
                    surface.transform,
                    "Board Quest Card Locked Reason " + card.CardId + " 090",
                    (card.LockedReason ?? "This card is unavailable.")
                        .ToUpperInvariant(),
                    16,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Warning,
                    FontStyle.Bold);
                RuntimeUi.SetLayout(locked, preferredHeight: 34f);
                ConfigureAuthoredCompactText076(locked, 13, 17);
            }

            var captured = card;
            var deck = coordinator as IBoardQuestDeckCoordinator090;
            var button = RuntimeUi.AddButton(
                surface.transform,
                "Choose Board Quest Card " + card.CardId + " 090",
                QuestCardButtonLabel090(card),
                card.CanChoose && deck != null
                    ? (Action)(() => CommitBoardQuestCard090(
                        coordinator,
                        deck,
                        captured))
                    : null,
                48f,
                card.CanChoose ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
            ConfigureAuthoredCompactText076(
                button.GetComponentInChildren<Text>(),
                15,
                20);
            button.interactable = false;
            var relay = button.gameObject
                .AddComponent<ExpeditionRouteCardButtonRelay089>();
            relay.Bind089(motion);

            blindChoice.Register091(wrapper.rectTransform, surface.rectTransform,
                button, card.CanChoose && deck != null,
                () => CommitBoardQuestCard090(coordinator, deck, captured),
                Resources.Load<Sprite>(BoardAdventureCardFrameResource084),
                card.TreasuryXpCost > 0 || card.Category == "RECRUIT");
            ArrangeIllustratedBoardCard091(surface.rectTransform,
                "Board Quest Card Art ", true);
            if (StringComparer.Ordinal.Equals(card.Category, "CHEST"))
                UseClosedChestCardArt092(surface.rectTransform, "Board Quest Card Art ");
        }

        private void CommitBoardQuestCard090(
            IGuildCityPresentationCoordinator017D coordinator,
            IBoardQuestDeckCoordinator090 deck,
            GuildQuestCardView090 card)
        {
            if (_boardQuestCommandRunning081 || deck == null || card == null)
                return;
            _boardQuestCommandRunning081 = true;
            _lastBoardQuestCard090 = card;
            _boardQuestCardAwaitingAcknowledgement090 = false;
            M1CommandResult result;
            _suppressBoardAdventureCoordinatorRefresh084 = true;
            try
            {
                result = deck.CommitBoardQuestCard090(card.CardId);
            }
            finally
            {
                _suppressBoardAdventureCoordinatorRefresh084 = false;
                _boardQuestCommandRunning081 = false;
            }

            _localStatus = result?.Succeeded == true
                ? string.Empty
                : result?.Message ?? "That quest card could not be saved.";
            _localStatusPositive = result != null && result.Succeeded;
            if (_localStatusPositive)
            {
                // Fate selection saves a sealed event. Its durable roll and
                // collection surface owns the result; do not show the old
                // transient "reward saved / pawn moving" animation yet.
                if ((coordinator as IQuestFateCoordinator165)?.PendingQuestFate165 != null)
                {
                    _lastBoardQuestCard090 = null;
                    _boardQuestCardAwaitingAcknowledgement090 = false;
                    BuildCurrentScreen();
                    return;
                }
                if (result.LootReward092 != null && result.LootReward092.IsCommitted)
                {
                    card.ResultRewardCopy092 = result.LootReward092.Summary;
                    card.ItemVisualId = result.LootReward092.ItemVisualId;
                    card.ItemName = result.LootReward092.ItemName;
                    card.RarityId = result.LootReward092.RarityId;
                }
                _selectedExpeditionDestination074 = null;
                if (!Application.isPlaying ||
                    coordinator?.GuildCity017D?.Expedition == null)
                    _lastBoardQuestCard090 = null;
                else if (ShouldRequireBoardQuestCardAcknowledgement090(
                             Application.isPlaying, _reducedMotion))
                    _boardQuestCardAwaitingAcknowledgement090 = true;
                else
                    HoldBoardQuestAction081(
                        coordinator.GuildCity017D,
                        Math.Max(StringComparer.Ordinal.Equals(card.Category, "CHEST")
                            ? BoardChestReveal092.OpeningDuration092 + 1.50f : 0f,
                        ExpeditionRouteCardMotion089.ShuffleAndDealLead089 +
                        BoardAdventureCardFlipDuration084 +
                        BoardAdventurePawnTravelDuration084 + 0.40f));
            }
            else
            {
                _lastBoardQuestCard090 = null;
                _boardQuestCardAwaitingAcknowledgement090 = false;
            }
            BuildCurrentScreen();
        }

        private void BuildBoardQuestCardResolution090(
            Transform parent,
            GuildCityPresentationState017D state)
        {
            var card = _lastBoardQuestCard090;
            var resolution = RuntimeUi.AddPanel(
                parent,
                "Board Quest Card Resolution 090",
                new Color(0.015f, 0.036f, 0.060f, 0.99f));
            SetAnchors074(resolution.rectTransform,
                new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.97f));
            StyleNeutralBoardSurface091(resolution);
            RuntimeUi.AddVerticalLayout(
                resolution.transform,
                new RectOffset(22, 22, 14, 16),
                7f,
                TextAnchor.UpperCenter);

            AddBoardQuestText081(
                resolution.transform,
                "Board Quest Card Resolution Heading 090",
                card == null
                    ? "CARD RESOLVED  •  READY TO CONTINUE"
                    : QuestCardResolutionHeading090(card.Category),
                25,
                38f,
                RuntimeUi.Positive,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);

            if (card != null)
            {
                GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(
                    resolution.transform,
                    "Board Quest Resolved Card Art " + card.CardId + " 090",
                    card.VisualResourcePath,
                    158f);
                AddBoardQuestText081(
                    resolution.transform,
                    "Board Quest Resolved Card Title 090",
                    (card.Title ?? "Quest card").ToUpperInvariant(),
                    27,
                    44f,
                    Color.white,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);

                if (QuestCardUsesPhysicalDice094(card) &&
                    card.DieOne > 0 && card.DieTwo > 0)
                {
                    var total = card.DieOne + card.DieTwo +
                                card.FateCheckModifier;
                    BuildAuthoritativeDiceRoll084(
                        resolution.transform,
                        card.DieOne,
                        card.DieTwo,
                        card.FateCheckModifier,
                        total,
                        "BOARD_QUEST_CARD_DICE_090|" + card.CardId,
                        total >= Math.Max(2, card.Target));
                }

                AddBoardQuestText081(
                    resolution.transform,
                    "Board Quest Resolved Card Reward 090",
                    ResolvedQuestCardReward090(card),
                    21,
                    58f,
                    RuntimeUi.Accent,
                    FontStyle.Bold,
                    TextAnchor.MiddleCenter);
            }
            AddBoardQuestText081(
                resolution.transform,
                "Board Quest Card Pawn Motion 090",
                _boardQuestCardAwaitingAcknowledgement090
                    ? "REWARD SAVED  •  REVIEW THE RESULT, THEN CONTINUE"
                    : "REWARD SAVED  •  PAWN MOVING TO THE NEXT ROOM…",
                19,
                34f,
                RuntimeUi.Text,
                FontStyle.Bold,
                TextAnchor.MiddleCenter);
            if (_boardQuestCardAwaitingAcknowledgement090)
            {
                var acknowledge = RuntimeUi.AddButton(
                    resolution.transform,
                    "Acknowledge Board Quest Card Resolution 090",
                    "CONTINUE",
                    AcknowledgeBoardQuestCardResolution090,
                    62f,
                    RuntimeUi.Accent);
                ConfigureAuthoredCompactText076(
                    acknowledge.GetComponentInChildren<Text>(),
                    17,
                    24);
            }
            ArrangeIllustratedBoardCard091(resolution.rectTransform,
                "Board Quest Resolved Card Art ", false);
            if (StringComparer.Ordinal.Equals(card?.Category, "CHEST"))
                AttachChestRewardReveal092(resolution.rectTransform,
                    "Board Quest Resolved Card Art ", "Board Quest Card Resolution Heading 090",
                    new[] { "Board Quest Resolved Card Reward 090" },
                    "QUEST_CHEST_092|" + card.CardId, card.ItemVisualId,
                    "CHEST OPENED  •  LOOT REVEALED");
        }

        private void AcknowledgeBoardQuestCardResolution090()
        {
            _lastBoardQuestCard090 = null;
            _boardQuestCardAwaitingAcknowledgement090 = false;
            BuildCurrentScreen();
        }

        private static string QuestCardRoundHeading090(
            GuildCityPresentationState017D state)
        {
            var round = Math.Max(1, state?.QuestCardRound090 ?? 1);
            var minimum = Math.Max(0, state?.QuestCardMinimumRounds090 ?? 0);
            return minimum >= SecondDimension.Gameplay.GuildCity017D
                       .GuildCityExpeditionService017D.MinimumQuestCardRounds090
                ? "CARD ROUND " + round + "  •  " + minimum +
                  "+ CARD ROUNDS THIS QUEST"
                : "CARD ROUND " + round +
                  "  •  CHOOSE 1 OF 3 • CARD ADVENTURE";
        }

        private static M1PremiumUi.Surface QuestCardSurface090(string category)
        {
            switch ((category ?? string.Empty).ToUpperInvariant())
            {
                case "CHEST": return M1PremiumUi.Surface.Vellum;
                case "MERCHANT": return M1PremiumUi.Surface.WorldPaper;
                case "RECRUIT":
                case "FREE_RECRUIT": return M1PremiumUi.Surface.WorldRibbon;
                case "BOON":
                case "XP": return M1PremiumUi.Surface.Positive;
                case "SCAR":
                case "BATTLE": return M1PremiumUi.Surface.Warning;
                case "FATE": return M1PremiumUi.Surface.EtchedGlass;
                default: return M1PremiumUi.Surface.WorldGlass;
            }
        }

        private static Color QuestCardAccent090(string category)
        {
            switch ((category ?? string.Empty).ToUpperInvariant())
            {
                case "CHEST": return new Color(1f, 0.78f, 0.26f);
                case "MERCHANT": return new Color(0.35f, 0.88f, 1f);
                case "RECRUIT":
                case "FREE_RECRUIT": return new Color(0.72f, 0.68f, 1f);
                case "BOON": return new Color(0.36f, 1f, 0.72f);
                case "XP": return new Color(1f, 0.86f, 0.32f);
                case "SCAR":
                case "BATTLE": return new Color(1f, 0.38f, 0.28f);
                default: return RuntimeUi.Accent;
            }
        }

        private static string QuestCardCategoryLabel090(
            GuildQuestCardView090 card)
        {
            var category = (card?.Category ?? "STORY").ToUpperInvariant();
            if (category == "FREE_RECRUIT") return "FREE RECRUIT CHANCE";
            if (!string.IsNullOrWhiteSpace(card?.RarityId))
                return card.RarityId.Replace("QUALITY_", string.Empty) +
                       "  " + category;
            if (card?.IsPermanentHeroBoon == true)
                return "PERMANENT HERO BOON";
            return category;
        }

        private static string QuestCardExactPower090(
            GuildQuestCardView090 card,
            long treasuryXp)
        {
            if (StringComparer.Ordinal.Equals(card?.Category, "FATE"))
                return "FORTUNE WHEEL  •  XP, SUPPLIES OR GEAR";
            if (StringComparer.Ordinal.Equals(card?.Category, "BOON"))
                return "ROLL D20  •  QUEST BLESSING OR NATURAL 20";
            if (StringComparer.Ordinal.Equals(card?.Category, "SCAR"))
                return "ROLL D20  •  TEMPORARY QUEST CURSE";
            if (!string.IsNullOrWhiteSpace(card?.ItemName))
                return "COMBAT POWER  •  PWR +" + card.PhysicalPower +
                       "  •  MYS +" + card.MysticPower +
                       (card.TreasuryXpCost > 0
                           ? "  •  COST " + card.TreasuryXpCost +
                             " XP (HAVE " + treasuryXp + ")"
                           : string.Empty);
            if (card?.IsPermanentHeroBoon == true)
                return "PERMANENT  •  " +
                       (string.IsNullOrWhiteSpace(card.HeroName)
                           ? "ONE ACTIVE HERO"
                           : card.HeroName.ToUpperInvariant());
            if (StringComparer.Ordinal.Equals(card?.Category, "BATTLE"))
                return "FULL UNION BATTLE  •  " +
                       Math.Max(1, card.EnemyUnionCount) +
                       (card.EnemyUnionCount == 1
                           ? " ENEMY UNION"
                           : " ENEMY UNIONS");
            if (QuestCardUsesPhysicalDice094(card))
                return "PHYSICAL 2D6  •  QUEST " +
                       SignedBoardQuest081(card.FateCheckModifier) +
                       "  •  TARGET " + Math.Max(2, card.Target);
            if (card != null && card.TreasuryXpDelta > 0)
                return "SPENDABLE GUILD XP  •  +" + card.TreasuryXpDelta;
            return card?.CanChoose == true
                ? "RESOLVES NOW  •  SAVES AUTOMATICALLY"
                : "CARD LOCKED";
        }

        private static string QuestCardButtonLabel090(
            GuildQuestCardView090 card)
        {
            if (card == null || !card.CanChoose) return "LOCKED";
            switch ((card.Category ?? string.Empty).ToUpperInvariant())
            {
                case "MERCHANT": return "BUY ITEM";
                case "RECRUIT": return "RECRUIT / ASCEND";
                case "CHEST": return "OPEN CHEST";
                case "FATE": return "OPEN FORTUNE WHEEL";
                case "BOON":
                case "SCAR": return "OPEN D20 ROLL";
                case "FREE_RECRUIT": return "ROLL THE DICE";
                case "BATTLE": return "REVEAL BATTLE";
                default: return "TAKE THIS CARD";
            }
        }

        private static string QuestCardRoutePreview090(
            GuildQuestCardView090 card)
        {
            if (!string.IsNullOrWhiteSpace(card?.RoutePreview))
                return card.RoutePreview.ToUpperInvariant();
            return "NEXT: " +
                   (string.IsNullOrWhiteSpace(card?.DestinationLabel)
                       ? "A NEW ROOM"
                       : card.DestinationLabel.ToUpperInvariant());
        }

        private static string QuestCardResolutionHeading090(string category)
        {
            switch ((category ?? string.Empty).ToUpperInvariant())
            {
                case "CHEST": return "CHEST OPENED  •  LOOT REVEALED";
                case "MERCHANT": return "PURCHASE COMPLETE  •  ITEM SAVED";
                case "RECRUIT": return "RECRUITMENT RESOLVED  •  HERO SAVED";
                case "FATE": return "DICE ROLLING  •  FATE RESOLVING";
                case "FREE_RECRUIT": return "DICE ROLLING  •  RECRUIT INVITATION";
                case "BATTLE": return "ENEMY REVEALED  •  UNIONS DEPLOYING";
                case "BOON": return "BOON CLAIMED  •  QUEST EMPOWERED";
                case "SCAR": return "RISK ACCEPTED  •  REWARD CLAIMED";
                default: return "REWARD CLAIMED  •  PROGRESS SAVED";
            }
        }

        private static bool QuestCardUsesPhysicalDice094(GuildQuestCardView090 card) =>
            StringComparer.Ordinal.Equals(card?.Category, "FREE_RECRUIT");

        private static string ResolvedQuestCardReward090(
            GuildQuestCardView090 card)
        {
            if (!string.IsNullOrWhiteSpace(card?.ResultRewardCopy092))
                return card.ResultRewardCopy092;
            if (StringComparer.Ordinal.Equals(card?.Category, "CHEST") ||
                StringComparer.Ordinal.Equals(card?.Category, "MERCHANT"))
                return (card.RewardPreview ?? card.ItemName ?? "Equipment") +
                       "  •  ADDED TO INVENTORY";
            if (StringComparer.Ordinal.Equals(card?.Category, "FREE_RECRUIT"))
            {
                var recruitTotal = card.DieOne + card.DieTwo + card.FateCheckModifier;
                return recruitTotal >= Math.Max(2, card.Target)
                    ? "DICE TOTAL " + recruitTotal + "  •  INVITATION EARNED  •  " +
                      (card.HeroName ?? "NEW ALLY") +
                      "  •  CLAIM FREE AFTER THIS ADVENTURE"
                    : "DICE TOTAL " + recruitTotal +
                      "  •  NO INVITATION THIS TIME  •  CONTINUE THE QUEST  •  NO XP SPENT";
            }
            if (!StringComparer.Ordinal.Equals(card?.Category, "FATE"))
                return card?.RewardPreview ?? string.Empty;
            var total = card.DieOne + card.DieTwo + card.FateCheckModifier;
            return total >= Math.Max(2, card.Target)
                ? "DICE TOTAL " + total + "  •  PASS  •  +20 XP  •  −1 FATIGUE"
                : "DICE TOTAL " + total + "  •  MISS  •  +3 XP  •  +1 FATIGUE";
        }
    }
}
