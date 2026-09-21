using System;
using System.Linq;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Tests.EditMode
{
    public sealed class CompactInventoryPresenter069Tests
    {
        private GameObject _host;
        private GameObject _presenterObject;
        private CompactInventoryPresenter069 _presenter;
        private EventSystem _eventSystemBefore;
        private EventSystem _ownedEventSystem;
        private GameObject _selectedObjectBefore;

        [TearDown]
        public void TearDown()
        {
            if (_presenter != null) _presenter.Shutdown();
            if (_presenterObject != null) UnityEngine.Object.DestroyImmediate(_presenterObject);
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            _presenter = null;
            _presenterObject = null;
            _host = null;

            if (_ownedEventSystem != null)
                UnityEngine.Object.DestroyImmediate(_ownedEventSystem.gameObject);
            else if (_eventSystemBefore != null)
                _eventSystemBefore.SetSelectedGameObject(
                    _selectedObjectBefore != null && _selectedObjectBefore.activeInHierarchy
                        ? _selectedObjectBefore
                        : null);
            _eventSystemBefore = null;
            _ownedEventSystem = null;
            _selectedObjectBefore = null;
        }

        [Test]
        public void BeginBuildsFixedTwoColumnInventoryAndSubmitsExactAuthorityIds()
        {
            CreatePresenter();
            var state = StateWithManyItems069();
            string submittedRecruit = null;
            string submittedSlot = null;
            string submittedItem = null;
            var closeCalls = 0;

            _presenter.Begin(
                _host.transform,
                () => state,
                (recruitId, slotId, itemId) =>
                {
                    submittedRecruit = recruitId;
                    submittedSlot = slotId;
                    submittedItem = itemId;
                    return M1CommandResult.Success("Authority accepted the item.");
                },
                "RECRUIT_069",
                () => closeCalls++);

            Assert.That(_presenter.IsRunningForTests, Is.True);
            Assert.That(_presenter.RootForTests, Is.Not.Null);
            Assert.That(_presenter.RootForTests.name, Is.EqualTo(CompactInventoryPresenter069.RootObjectName));
            Assert.That(_presenter.RootForTests.Find("Inventory Workspace 069"), Is.Not.Null);
            Assert.That(_presenter.RootForTests.Find("Inventory Workspace 069/Equipped Loadout Column 069"), Is.Not.Null);
            Assert.That(_presenter.RootForTests.Find("Inventory Workspace 069/Inventory Pack Column 069"), Is.Not.Null);
            Assert.That(_presenter.RootForTests.GetComponentsInChildren<RectTransform>()
                .Any(value => StringComparer.Ordinal.Equals(value.name, "Character Portrait Standee 069")), Is.True);
            Assert.That(_presenter.ItemScrollForTests, Is.Not.Null);
            Assert.That(_presenter.ItemScrollForTests.vertical, Is.True);
            Assert.That(_presenter.ItemScrollForTests.horizontal, Is.False);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_presenter.RootForTests);
            Canvas.ForceUpdateCanvases();
            Assert.That(_presenter.ItemScrollForTests.viewport.GetComponent<Mask>(), Is.Not.Null);
            Assert.That(
                LayoutUtility.GetPreferredHeight(_presenter.ItemScrollForTests.content),
                Is.GreaterThan(_presenter.ItemScrollForTests.GetComponent<LayoutElement>().preferredHeight),
                "Eleven full-size item rows must overflow the masked viewport and require scrolling.");
            Assert.That(_presenter.ItemButtonsForTests.Count, Is.EqualTo(11));
            Assert.That(_presenter.SlotButtonsForTests.Count, Is.EqualTo(2));
            Assert.That(
                _presenter.EquipButtonForTests.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("EQUIPPED AND ACTIVE"));

            var workspaceRect = _presenter.RootForTests.Find("Inventory Workspace 069")
                .GetComponent<RectTransform>();
            var workspace = workspaceRect.GetComponent<LayoutElement>();
            Assert.That(workspaceRect.GetComponent<HorizontalLayoutGroup>(), Is.Not.Null);
            Assert.That(workspace.preferredHeight, Is.EqualTo(1040f),
                "Item count must grow the ScrollRect content, not the full screen.");
            var leftColumn = workspaceRect.Find("Equipped Loadout Column 069").GetComponent<RectTransform>();
            var rightColumn = workspaceRect.Find("Inventory Pack Column 069").GetComponent<RectTransform>();
            Assert.That(
                RectTransformUtility.CalculateRelativeRectTransformBounds(workspaceRect, leftColumn).center.x,
                Is.LessThan(RectTransformUtility.CalculateRelativeRectTransformBounds(workspaceRect, rightColumn).center.x));
            Assert.That(_presenter.SlotButtonsForTests.All(button =>
                button.navigation.mode == Navigation.Mode.Explicit), Is.True);
            Assert.That(_presenter.ItemButtonsForTests.All(button =>
                button.navigation.mode == Navigation.Mode.Explicit), Is.True);
            Assert.That(_presenter.PreviousRecruitButtonForTests, Is.Not.Null);
            Assert.That(_presenter.NextRecruitButtonForTests, Is.Not.Null);
            Assert.That(_presenter.BackButtonForTests, Is.Not.Null);
            Assert.That(_presenter.PreviousRecruitButtonForTests.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(_presenter.NextRecruitButtonForTests.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(_presenter.BackButtonForTests.navigation.mode, Is.EqualTo(Navigation.Mode.Explicit));
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(_presenter.SlotButtonsForTests[0].gameObject));
            Assert.That(_presenter.SlotButtonsForTests[0].navigation.selectOnUp,
                Is.SameAs(_presenter.PreviousRecruitButtonForTests));
            Assert.That(_presenter.BackButtonForTests.navigation.selectOnLeft,
                Is.SameAs(_presenter.NextRecruitButtonForTests));

            _presenter.NextRecruitButtonForTests.onClick.Invoke();
            Assert.That(_presenter.SelectedRecruitIdForTests, Is.EqualTo("RECRUIT_069_B"));
            _presenter.PreviousRecruitButtonForTests.onClick.Invoke();
            Assert.That(_presenter.SelectedRecruitIdForTests, Is.EqualTo("RECRUIT_069"));
            _presenter.BackButtonForTests.onClick.Invoke();
            Assert.That(closeCalls, Is.EqualTo(1));

            var mountedRoot = _presenter.RootForTests;
            _presenter.Refresh();
            Assert.That(_presenter.RootForTests, Is.SameAs(mountedRoot));
            Assert.That(_presenter.ItemButtonsForTests.Count, Is.EqualTo(11));

            Assert.That(_presenter.SelectedItemIdForTests, Is.EqualTo("ITEM_EQUIPPED_069"));
            Assert.That(_presenter.EquipButtonForTests.interactable, Is.False);
            var equippedButton = _presenter.ItemButtonsForTests
                .Single(button => button.name.Contains("ITEM_EQUIPPED_069"));
            Assert.That(equippedButton.navigation.selectOnRight,
                Is.Not.SameAs(_presenter.EquipButtonForTests));
            Assert.That(_presenter.TrySelectItem("ITEM_PACK_10_069"), Is.True);
            Assert.That(_presenter.EquipButtonForTests.interactable, Is.True);
            Assert.That(
                _presenter.EquipButtonForTests.transform.Find("Label").GetComponent<Text>().text,
                Is.EqualTo("TAP THE GEAR ABOVE TO EQUIP"));
            var alternateButton = _presenter.ItemButtonsForTests
                .Single(button => button.name.Contains("ITEM_PACK_10_069"));
            Assert.That(alternateButton.navigation.selectOnRight,
                Is.SameAs(_presenter.EquipButtonForTests));
            Assert.That(_presenter.EquipButtonForTests.navigation.selectOnLeft,
                Is.SameAs(_presenter.SlotButtonsForTests[0]));

            var result = _presenter.EquipSelected();

            Assert.That(result.Succeeded, Is.True);
            Assert.That(submittedRecruit, Is.EqualTo("RECRUIT_069"));
            Assert.That(submittedSlot, Is.EqualTo("SLOT_BODY_069"));
            Assert.That(submittedItem, Is.EqualTo("ITEM_PACK_10_069"));

            _presenter.Shutdown();
            Assert.That(_presenter.IsRunningForTests, Is.False);
            Assert.That(_presenter.RootForTests, Is.Null);
        }

        [Test]
        public void TappingEligibleGearEquipsItWithoutASecondConfirmationTap()
        {
            CreatePresenter();
            var state = StateWithManyItems069();
            string submittedRecruit = null;
            string submittedSlot = null;
            string submittedItem = null;
            _presenter.Begin(
                _host.transform,
                () => state,
                (recruitId, slotId, itemId) =>
                {
                    submittedRecruit = recruitId;
                    submittedSlot = slotId;
                    submittedItem = itemId;
                    return M1CommandResult.Success();
                },
                "RECRUIT_069");

            var gearCard = _presenter.ItemButtonsForTests
                .Single(button => button.name.Contains("ITEM_PACK_10_069"));
            gearCard.onClick.Invoke();

            Assert.That(submittedRecruit, Is.EqualTo("RECRUIT_069"));
            Assert.That(submittedSlot, Is.EqualTo("SLOT_BODY_069"));
            Assert.That(submittedItem, Is.EqualTo("ITEM_PACK_10_069"));
            Assert.That(_presenter.SelectedItemIdForTests, Is.EqualTo("ITEM_PACK_10_069"));
        }

        [Test]
        public void ComparisonAndEquipPolicyExplainCurrentSelectedAndLockedStates()
        {
            var slot = new M1EquipmentSlotView
            {
                SlotId = "SLOT_BODY_069",
                EquippedItemId = "OLD_069",
                EquippedItemName = "Weathered Coat"
            };
            var choice = new M1EquipmentChoiceView
            {
                ItemId = "NEW_069",
                DisplayName = "Ember Plate",
                DirectChange = "+7 Defense, -1 Agility",
                ForecastBehavior = "Guard forecasts become steadier.",
                IsLegal = true
            };

            Assert.That(CompactInventoryPresenter069.CanEquipSelection(slot, choice), Is.True);
            var copy = CompactInventoryPresenter069.BuildStatComparison(slot, choice);
            Assert.That(copy, Does.Contain("Weathered Coat  →  Ember Plate"));
            Assert.That(copy, Does.Contain("+7 Defense, -1 Agility"));
            Assert.That(copy, Does.Contain("IN BATTLE"));

            choice.IsEquipped = true;
            Assert.That(CompactInventoryPresenter069.CanEquipSelection(slot, choice), Is.False);
            choice.IsEquipped = false;
            slot.IsLocked = true;
            Assert.That(CompactInventoryPresenter069.CanEquipSelection(slot, choice), Is.False);
        }

        [Test]
        public void ComparisonShowsTrueBeforeAndAfterCombatPower087()
        {
            var recruit = new M1RecruitLoadoutView
            {
                PhysicalAttack = 30,
                MysticAttack = 20
            };
            var slot = new M1EquipmentSlotView
            {
                EquippedItemId = "STARTER_SWORD",
                EquippedItemName = "Training Sword",
                EquippedPhysicalAttackBonus = 0,
                EquippedMysticAttackBonus = 0
            };
            var creatorWeapon = new M1EquipmentChoiceView
            {
                ItemId = "CRITEM10000_087",
                DisplayName = "Warden Pattern Sword",
                PhysicalAttackBonus = 7,
                MysticAttackBonus = 2,
                IsLegal = true
            };

            var copy = CompactInventoryPresenter069.BuildStatComparison(
                recruit,
                slot,
                creatorWeapon);

            Assert.That(copy, Does.Contain("PHYSICAL POWER  •  30 → 37"));
            Assert.That(copy, Does.Contain("MYSTIC POWER  •  20 → 22"));
            Assert.That(copy, Does.Contain("+7"));
            Assert.That(copy, Does.Contain("+2"));
        }

        [Test]
        public void Version70CatalogLootShowsRealRewardAndHidesReceiptMetadata()
        {
            var slot = new M1EquipmentSlotView
            {
                SlotId = "SLOT_MAIN_HAND",
                EquippedItemId = "STARTER_SWORD",
                EquippedItemName = "Training Sword"
            };
            var loot = new M1EquipmentChoiceView
            {
                ItemId = "LOOT_ITEM_070_1234567890ABCDEF12345678",
                DisplayName = "Roadwarden Blade",
                EquipmentVisualId = "SWORD",
                QualityId = "QUALITY_RARE",
                RarityTierId = "BASIC",
                RarityDisplayName = "Basic",
                DirectChange =
                    "Main Hand • Sword, Loot Receipt 070 DEADBEEF1234567890ABCDEF, " +
                    "Loot Source 070 ABCDEF1234567890ABCD, Weapon, Weapon Family Sword",
                ForecastBehavior = "May strengthen offense-oriented Union forecasts.",
                IsLegal = true
            };

            var label = CompactInventoryPresenter069.BuildInventoryItemLabel069(loot);
            var comparison = CompactInventoryPresenter069.BuildStatComparison(slot, loot);

            Assert.That(CompactInventoryPresenter069.IsNewVersion70Loot069(loot), Is.True);
            Assert.That(CompactInventoryPresenter069.PlayerFacingRarity069(loot), Is.EqualTo("Rare"),
                "The catalog QUALITY_RARE value must win over the legacy Basic art tier projection.");
            Assert.That(CompactInventoryPresenter069.PlayerFacingRarityTier069(loot), Is.EqualTo("RARE"));
            Assert.That(CompactInventoryPresenter069.PlayerFacingWeaponFamily069(loot), Is.EqualTo("Sword"));
            Assert.That(label, Is.EqualTo("NEW LOOT  •  RARE  •  SWORD\nRoadwarden Blade"));
            Assert.That(comparison, Does.StartWith("NEW LOOT  •  RARE  •  SWORD"));
            Assert.That(comparison, Does.Contain("Training Sword  →  Roadwarden Blade"));
            Assert.That(comparison, Does.Contain("READY  •  TAP GEAR TO EQUIP"));
            Assert.That(comparison, Does.Not.Contain("Receipt"));
            Assert.That(comparison, Does.Not.Contain("Source"));
            Assert.That(comparison, Does.Not.Contain("DEADBEEF"));
            Assert.That(comparison, Does.Not.Contain("ABCDEF"));

            loot.IsEquipped = true;
            Assert.That(CompactInventoryPresenter069.IsNewVersion70Loot069(loot), Is.False);
            Assert.That(
                CompactInventoryPresenter069.BuildInventoryItemLabel069(loot),
                Does.StartWith("EQUIPPED  •  RARE  •  SWORD"));
        }

        [TestCase("Sword", "Sword")]
        [TestCase("Great Weapon", "Great Weapon")]
        [TestCase("Axe", "Axe")]
        [TestCase("Spear Polearm", "Spear / Polearm")]
        [TestCase("Bow", "Bow")]
        [TestCase("Dagger", "Dagger")]
        [TestCase("Shield", "Shield")]
        [TestCase("Gauntlet", "Gauntlet")]
        [TestCase("Staff", "Staff")]
        [TestCase("Focus", "Focus")]
        [TestCase("Engineering Tool", "Engineering Tool")]
        [TestCase("Hybrid Relic Weapon", "Hybrid Relic Weapon")]
        public void Version70FamilyProjectionKeepsAllTwelveFamilySpecificity(
            string catalogFamily,
            string expectedLabel)
        {
            var loot = new M1EquipmentChoiceView
            {
                ItemId = "LOOT_ITEM_070_1234567890ABCDEF12345678",
                DisplayName = "Tempered Polearm",
                EquipmentVisualId = "SPEAR",
                QualityId = "QUALITY_UNCOMMON",
                RarityDisplayName = "Basic",
                DirectChange =
                    "Main Hand • Polearm, Loot Receipt 070 DEADBEEF, Weapon, " +
                    "Weapon Family " + catalogFamily,
                IsLegal = true
            };

            Assert.That(
                CompactInventoryPresenter069.PlayerFacingWeaponFamily069(loot),
                Is.EqualTo(expectedLabel));
            Assert.That(
                CompactInventoryPresenter069.BuildInventoryItemLabel069(loot),
                Does.Contain("UNCOMMON  •  " + expectedLabel.ToUpperInvariant()));
        }

        [TestCase("QUALITY_STARTER", "Starter", "BASIC")]
        [TestCase("QUALITY_COMMON", "Common", "COMMON")]
        [TestCase("QUALITY_UNCOMMON", "Uncommon", "COMMON")]
        [TestCase("QUALITY_RARE", "Rare", "RARE")]
        [TestCase("QUALITY_EPIC", "Epic", "RARE")]
        [TestCase("QUALITY_LEGENDARY", "Legendary", "LEGENDARY")]
        [TestCase("QUALITY_GODLY", "Godly", "GODLY")]
        public void Version70RarityProjectionPreservesCatalogRarity(
            string qualityId,
            string expectedLabel,
            string expectedFrameTier)
        {
            var loot = new M1EquipmentChoiceView
            {
                ItemId = "LOOT_ITEM_070_1234567890ABCDEF12345678",
                DisplayName = "Catalog Weapon",
                QualityId = qualityId,
                RarityTierId = "BASIC",
                RarityDisplayName = "Basic"
            };

            Assert.That(CompactInventoryPresenter069.PlayerFacingRarity069(loot), Is.EqualTo(expectedLabel));
            Assert.That(
                CompactInventoryPresenter069.PlayerFacingRarityTier069(loot),
                Is.EqualTo(expectedFrameTier));
        }

        [TestCase(1920f, 1080f)]
        [TestCase(1280f, 800f)]
        public void ArmoryEvidenceUsesFriendlyNamesHighContrastAndTwinBladeArtwork(
            float width,
            float height)
        {
            CreatePresenter();
            _host.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

            var daggerVisual = M1VisualAssets.EquipmentVisualId(
                "CA002_WPN_DAGGER_NIGHTGLASS_T2",
                "SLOT_OFF_HAND",
                new[] { "BRANCH_NIGHTGLASS", "DAGGER", "WEAPON", "WEAPON_FAMILY_DAGGER" });
            Assert.That(daggerVisual, Is.EqualTo("DAGGER"),
                "A legal off-hand dagger must win over the slot's shield fallback.");

            var equipped = new M1EquipmentChoiceView
            {
                ItemId = "PROC_36344E2400DC98B6_OFF",
                DisplayName = "EQ_PROC_WARD_BUCKLER",
                EquipmentVisualId = "SHIELD",
                RarityTierId = "BASIC",
                RarityDisplayName = "Basic",
                DirectChange = "Off Hand, Ward Buckler, Shield",
                IsEquipped = true,
                IsLegal = true
            };
            var recoveredDagger = new M1EquipmentChoiceView
            {
                ItemId = "LOOT_ITEM_070_050F270B14863FB65048038F",
                DisplayName = "Tempered Nightglass Twin Blades",
                EquipmentVisualId = daggerVisual,
                QualityId = "QUALITY_UNCOMMON",
                RarityTierId = "COMMON",
                RarityDisplayName = "Uncommon",
                DirectChange = "Off Hand, Branch Nightglass, Dagger, Weapon Family Dagger",
                ForecastBehavior = "May strengthen offense-oriented Union forecasts.",
                IsEquipped = false,
                IsLegal = true
            };
            var state = new M1PresentationState
            {
                Recruits = new[]
                {
                    new M1RecruitLoadoutView
                    {
                        RecruitId = "PROC_36344E2400DC98B6",
                        RaceId = "DOG_TRIBE",
                        VisualSeed = "234B7103DA5CD0DEC716E0C3",
                        PortraitAuthorityId = "PROC_36344E2400DC98B6",
                        DisplayName = "Gara Redtail",
                        ObservedClass = "Guardian",
                        ClassSymbol = "◆",
                        Level = 3,
                        IsLegal = true,
                        Slots = new[]
                        {
                            new M1EquipmentSlotView
                            {
                                SlotId = "SLOT_OFF_HAND",
                                DisplayName = "Off Hand",
                                EquippedItemId = equipped.ItemId,
                                EquippedItemName = equipped.DisplayName,
                                EquippedVisualId = equipped.EquipmentVisualId,
                                EquippedRarityTierId = "BASIC",
                                EquippedRarityDisplayName = "Basic",
                                IsLegal = true,
                                Choices = new[] { equipped, recoveredDagger }
                            }
                        }
                    }
                }
            };

            _presenter.Begin(
                _host.transform,
                () => state,
                (recruitId, slotId, itemId) => M1CommandResult.Success(),
                "PROC_36344E2400DC98B6");
            Assert.That(_presenter.TrySelectItem(recoveredDagger.ItemId), Is.True);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_presenter.RootForTests);
            Canvas.ForceUpdateCanvases();

            var itemLabels = _presenter.ItemButtonsForTests
                .Select(button => button.transform.Find("Label").GetComponent<Text>())
                .ToArray();
            Assert.That(itemLabels.Any(label => label.text.IndexOf(
                "Unknown", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            Assert.That(itemLabels.Any(label => label.text.StartsWith(
                "Ward Buckler\nBasic", StringComparison.Ordinal)), Is.True);
            Assert.That(itemLabels.Any(label => label.text.StartsWith(
                "NEW LOOT  •  UNCOMMON  •  DAGGER\nTempered Nightglass Twin Blades",
                StringComparison.Ordinal)), Is.True);
            Assert.That(itemLabels.Any(label =>
                label.text.Contains("NO STAT CHANGE")), Is.True,
                "Every unequipped gear choice must retain its at-a-glance stat comparison.");
            Assert.That(
                CompactInventoryPresenter069.BuildStatComparison(
                    state.Recruits[0].Slots[0],
                    recoveredDagger),
                Does.Contain("Ward Buckler  →  Tempered Nightglass Twin Blades"));

            foreach (var button in _presenter.ItemButtonsForTests)
            {
                var label = button.transform.Find("Label").GetComponent<Text>();
                var artworkFrame = button.GetComponentsInChildren<RectTransform>(true)
                    .Single(value => value.name.StartsWith(
                        "Premium Equipment Rarity Frame ",
                        StringComparison.Ordinal));
                Assert.That(label.rectTransform.anchorMin.x,
                    Is.GreaterThan(artworkFrame.anchorMax.x),
                    button.name + " must reserve a proportional text column beyond its artwork at " +
                    width + "×" + height + ".");
                foreach (var surface in new[]
                         {
                             button.colors.normalColor,
                             button.colors.highlightedColor,
                             button.colors.selectedColor
                         })
                {
                    Assert.That(
                        ContrastRatio069(label.color, surface),
                        Is.GreaterThanOrEqualTo(4.5f),
                        button.name + " must retain WCAG AA contrast at " +
                        width + "×" + height + ".");
                }
            }

            var daggerButton = _presenter.ItemButtonsForTests.Single(button =>
                button.name.Contains(recoveredDagger.ItemId));
            var artworkNames = daggerButton.GetComponentsInChildren<RectTransform>(true)
                .Select(value => value.name)
                .ToArray();
            Assert.That(artworkNames, Does.Contain("Premium Equipment Art DAGGER Left"));
            Assert.That(artworkNames, Does.Contain("Premium Equipment Art DAGGER Right"));
            Assert.That(artworkNames, Does.Not.Contain("Premium Equipment Art SHIELD"));
            Assert.That(_presenter.EquipButtonForTests.interactable, Is.True,
                "Presentation repair must preserve the existing equip action.");
        }

        [Test]
        public void EquipFailsClosedWhenTheFreshSnapshotNoLongerContainsTheSelection()
        {
            CreatePresenter();
            var state = StateWithManyItems069();
            var authorityCalls = 0;
            _presenter.Begin(
                _host.transform,
                () => state,
                (recruitId, slotId, itemId) =>
                {
                    authorityCalls++;
                    return M1CommandResult.Success();
                },
                "RECRUIT_069");
            Assert.That(_presenter.TrySelectItem("ITEM_PACK_10_069"), Is.True);

            var slot = state.Recruits[0].Slots[0];
            slot.Choices = slot.Choices
                .Where(value => !StringComparer.Ordinal.Equals(value.ItemId, "ITEM_PACK_10_069"))
                .ToArray();

            var result = _presenter.EquipSelected();

            Assert.That(result.Succeeded, Is.False);
            Assert.That(authorityCalls, Is.Zero,
                "A stale UI selection must never fall back to a different authority target.");
        }

        [Test]
        public void UnavailableInventoryKeepsAVisibleFocusedBackToHallCallback()
        {
            CreatePresenter();
            var closeCalls = 0;
            _presenter.Begin(
                _host.transform,
                () => new M1PresentationState { Recruits = Array.Empty<M1RecruitLoadoutView>() },
                (recruitId, slotId, itemId) => M1CommandResult.Success(),
                closeRequested: () => closeCalls++);

            Assert.That(_presenter.BackButtonForTests, Is.Not.Null);
            Assert.That(EventSystem.current.currentSelectedGameObject,
                Is.SameAs(_presenter.BackButtonForTests.gameObject));
            _presenter.BackButtonForTests.onClick.Invoke();
            Assert.That(closeCalls, Is.EqualTo(1));
            _presenter.enabled = false;
            Assert.That(_presenter.IsRunningForTests, Is.False);
            Assert.That(_presenter.RootForTests, Is.Null);
        }

        private void CreatePresenter()
        {
            _eventSystemBefore = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            _selectedObjectBefore = _eventSystemBefore == null
                ? null
                : _eventSystemBefore.currentSelectedGameObject;
            if (_eventSystemBefore == null)
            {
                var eventSystemObject = new GameObject(
                    "Compact Inventory Test EventSystem 069",
                    typeof(EventSystem),
                    typeof(StandaloneInputModule));
                _ownedEventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            _host = new GameObject(
                "Inventory Host Canvas 069",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _host.GetComponent<RectTransform>().sizeDelta = RuntimeUiReference069();
            _host.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = _host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RuntimeUiReference069();
            _presenterObject = new GameObject("Compact Inventory Presenter 069");
            _presenter = _presenterObject.AddComponent<CompactInventoryPresenter069>();
        }

        private static Vector2 RuntimeUiReference069()
        {
            return new Vector2(2796f, 1290f);
        }

        private static M1PresentationState StateWithManyItems069()
        {
            var equipped = Choice069("ITEM_EQUIPPED_069", "Weathered Coat", true, 0);
            var pack = Enumerable.Range(1, 10)
                .Select(index => Choice069(
                    "ITEM_PACK_" + index + "_069",
                    "Pack Armor " + index,
                    false,
                    index))
                .ToArray();
            return new M1PresentationState
            {
                Recruits = new[]
                {
                    new M1RecruitLoadoutView
                    {
                        RecruitId = "RECRUIT_069",
                        RaceId = "HUMAN",
                        VisualSeed = "VISUAL_069",
                        PortraitAuthorityId = "RECRUIT_069",
                        DisplayName = "Maren Holt",
                        ObservedClass = "Guardian",
                        ClassSymbol = "◆",
                        Level = 4,
                        IsLegal = true,
                        Slots = new[]
                        {
                            new M1EquipmentSlotView
                            {
                                SlotId = "SLOT_BODY_069",
                                DisplayName = "Body Armor",
                                EquippedItemId = equipped.ItemId,
                                EquippedItemName = equipped.DisplayName,
                                EquippedVisualId = "ARMOR",
                                EquippedRarityTierId = "COMMON",
                                EquippedRarityDisplayName = "Common",
                                IsLegal = true,
                                Choices = new[] { equipped }.Concat(pack).ToArray()
                            },
                            new M1EquipmentSlotView
                            {
                                SlotId = "SLOT_RELIC_069",
                                DisplayName = "Relic",
                                IsLegal = true,
                                Choices = Array.Empty<M1EquipmentChoiceView>()
                            }
                        }
                    },
                    new M1RecruitLoadoutView
                    {
                        RecruitId = "RECRUIT_069_B",
                        RaceId = "GOBLIN",
                        VisualSeed = "VISUAL_069_B",
                        PortraitAuthorityId = "RECRUIT_069_B",
                        DisplayName = "Tovvi Copperspark",
                        ObservedClass = "Rogue",
                        ClassSymbol = "◇",
                        Level = 3,
                        IsLegal = true,
                        Slots = new[]
                        {
                            new M1EquipmentSlotView
                            {
                                SlotId = "SLOT_BODY_069",
                                DisplayName = "Body Armor",
                                EquippedItemId = "ITEM_B_EQUIPPED_069",
                                EquippedItemName = "Trail Leathers",
                                EquippedVisualId = "ARMOR",
                                EquippedRarityTierId = "COMMON",
                                EquippedRarityDisplayName = "Common",
                                IsLegal = true,
                                Choices = new[]
                                {
                                    Choice069("ITEM_B_EQUIPPED_069", "Trail Leathers", true, 2)
                                }
                            }
                        }
                    }
                }
            };
        }

        private static M1EquipmentChoiceView Choice069(
            string itemId,
            string displayName,
            bool equipped,
            int defense)
        {
            return new M1EquipmentChoiceView
            {
                ItemId = itemId,
                DisplayName = displayName,
                VisualGlyph = "◇",
                EquipmentVisualId = "ARMOR",
                RarityTierId = defense > 7 ? "RARE" : "COMMON",
                RarityDisplayName = defense > 7 ? "Rare" : "Common",
                DirectChange = "+" + defense + " Defense",
                ForecastBehavior = "May strengthen Guard-oriented forecasts.",
                IsEquipped = equipped,
                IsLegal = true
            };
        }

        private static float ContrastRatio069(Color first, Color second)
        {
            var lighter = Mathf.Max(RelativeLuminance069(first), RelativeLuminance069(second));
            var darker = Mathf.Min(RelativeLuminance069(first), RelativeLuminance069(second));
            return (lighter + 0.05f) / (darker + 0.05f);
        }

        private static float RelativeLuminance069(Color color) =>
            0.2126f * LinearChannel069(color.r) +
            0.7152f * LinearChannel069(color.g) +
            0.0722f * LinearChannel069(color.b);

        private static float LinearChannel069(float channel) =>
            channel <= 0.03928f
                ? channel / 12.92f
                : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
