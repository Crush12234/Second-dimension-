using System;
using System.Collections.Generic;
using System.Linq;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Gameplay.SSSTenV4;
using SecondDimension.Gameplay.TitanHeroes161;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>
    /// Compact, presentation-only RPG inventory for one recruit. The component reads
    /// coordinator projections and submits the selected item instance through the
    /// existing equipment authority; it never mutates a view or gameplay record.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed partial class CompactInventoryPresenter069 : MonoBehaviour
    {
        public const string VersionId = "COMPACT_RPG_INVENTORY_069";
        public const string RootObjectName = "Compact RPG Inventory 069";
        private const int SlotColumns069 = 2;
        private const string Version70LootItemPrefix069 = "LOOT_ITEM_070_";
        private const string Version70ReceiptMarker069 = "LOOT_RECEIPT_070_";
        private const string Version70SourceMarker069 = "LOOT_SOURCE_070_";
        private static readonly PlayerFacingLabelService070 PlayerLabels070 =
            new PlayerFacingLabelService070();

        private readonly List<Button> _slotButtons = new List<Button>();
        private readonly List<Button> _itemButtons = new List<Button>();
        private readonly List<Button> _navigableItemButtons = new List<Button>();
        private readonly List<Button> _preparedFamilyButtons090 = new List<Button>();

        private IM1PresentationCoordinator _coordinator;
        private Func<M1PresentationState> _readState;
        private Func<string, string, string, M1CommandResult> _equip;
        private Func<string, M1CommandResult> _ascendSss090;
        private Func<string, SssPreparedFamilyLoadoutView090> _readSssPreparation090;
        private Func<string, int, M1CommandResult> _cycleSssPreparedFamily090;
        private Func<string, M1CommandResult> _claimSssSignatureWeapon090;
        private Action _closeRequested;
        private RectTransform _host;
        private RectTransform _root;
        private ScrollRect _itemScroll;
        private Button _equipButton;
        private Button _ascendButton090;
        private Button _claimSignatureWeaponButton090;
        private Button _previousRecruitButton;
        private Button _nextRecruitButton;
        private Button _backButton;
        private Button _selectedSlotButton;
        private Button _selectedItemButton;
        private string _selectedRecruitId;
        private string _selectedSlotId;
        private string _selectedItemId;
        private string _statusMessage = string.Empty;
        private bool _statusPositive;
        private bool _running;
        private bool _building;
        private bool _refreshQueued;
        private bool _hasBuilt;
        private FocusRegion069 _focusRegion = FocusRegion069.Slot;

        private enum FocusRegion069
        {
            Slot,
            Item,
            Equip
        }

        /// <summary>
        /// Mounts the inventory under <paramref name="host"/> and binds it to the
        /// current presentation/equipment authority.
        /// </summary>
        public void Begin(
            Transform host,
            IM1PresentationCoordinator equipAuthority,
            string preferredRecruitId = null,
            Action closeRequested = null)
        {
            if (equipAuthority == null) throw new ArgumentNullException(nameof(equipAuthority));

            BeginCore(
                host,
                () => equipAuthority.State,
                equipAuthority.EquipItem,
                preferredRecruitId,
                closeRequested,
                deferInitialRefresh123: (equipAuthority as IAutoEquipmentAsyncCoordinator123)?.PendingAutoEquipment123 != null);
            _coordinator = equipAuthority;
            if (equipAuthority is ISssTenV4PresentationCoordinator090 sssAuthority090)
                _ascendSss090 = sssAuthority090.AscendSssHero090;
            if (equipAuthority is ISssPreparedFamilyPresentationCoordinator090 preparation090)
            {
                _readSssPreparation090 = preparation090.ReadSssPreparedFamilies090;
                _cycleSssPreparedFamily090 = preparation090.CycleSssPreparedFamily090;
                _claimSssSignatureWeapon090 = preparation090.ClaimSssSignatureWeapon090;
            }
            _coordinator.Changed += HandleAuthorityChanged;
            Refresh();
        }

        /// <summary>
        /// Delegate binding is a narrow test/embedding seam for hosts that already
        /// adapt the same authority. Call Refresh when the supplied snapshot changes.
        /// </summary>
        public void Begin(
            Transform host,
            Func<M1PresentationState> readState,
            Func<string, string, string, M1CommandResult> equipCallback,
            string preferredRecruitId = null,
            Action closeRequested = null)
        {
            BeginCore(host, readState, equipCallback, preferredRecruitId, closeRequested);
        }

        /// <summary>Re-resolves every selection from the latest authority snapshot.</summary>
        public void Refresh()
        {
            if (!_running || _root == null) return;
            if (ObservePendingAutoEquipment123()) return;
            if (_building)
            {
                _refreshQueued = true;
                return;
            }

            do
            {
                _refreshQueued = false;
                _building = true;
                try
                {
                    BuildLatestSnapshot069();
                }
                finally
                {
                    _building = false;
                }
            } while (_refreshQueued && _running && _root != null);
        }

        /// <summary>Changes the displayed recruit while retaining the same authority.</summary>
        public void Refresh(string recruitId)
        {
            _selectedRecruitId = recruitId;
            _selectedSlotId = null;
            _selectedItemId = null;
            _focusRegion = FocusRegion069.Slot;
            _statusMessage = string.Empty;
            Refresh();
        }

        /// <summary>Unsubscribes and removes all UI created by Begin.</summary>
        public void Shutdown()
        {
            DetachAutoEquipment123();
            if (_coordinator != null) _coordinator.Changed -= HandleAuthorityChanged;
            _coordinator = null;
            _readState = null;
            _equip = null;
            _ascendSss090 = null;
            _readSssPreparation090 = null;
            _cycleSssPreparedFamily090 = null;
            _claimSssSignatureWeapon090 = null;
            _closeRequested = null;
            _host = null;
            _running = false;
            _refreshQueued = false;
            _hasBuilt = false;

            _slotButtons.Clear();
            _itemButtons.Clear();
            _navigableItemButtons.Clear();
            _preparedFamilyButtons090.Clear();
            _itemScroll = null;
            _equipButton = null;
            _ascendButton090 = null;
            _claimSignatureWeaponButton090 = null;
            _findHero155 = null;
            _previousRecruitButton = null;
            _nextRecruitButton = null;
            _backButton = null;
            _selectedSlotButton = null;
            _selectedItemButton = null;

            if (_root != null)
            {
                var eventSystem = EventSystem.current;
                var focused = eventSystem == null ? null : eventSystem.currentSelectedGameObject;
                if (focused != null && focused.transform.IsChildOf(_root))
                    eventSystem.SetSelectedGameObject(null);
                var rootObject = _root.gameObject;
                _root = null;
                rootObject.SetActive(false);
                DestroyUiObject069(rootObject);
            }
        }

        /// <summary>Selects an equipped slot. Useful to external roster hosts and tests.</summary>
        public bool TrySelectSlot(string slotId)
        {
            if (!TryResolveCurrent069(out _, out var recruit, out _, out _) || recruit == null)
                return false;
            var slot = (recruit.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .FirstOrDefault(value => value != null &&
                                         StringComparer.Ordinal.Equals(value.SlotId, slotId));
            if (slot == null) return false;

            _selectedSlotId = slot.SlotId;
            _selectedItemId = null;
            _focusRegion = FocusRegion069.Slot;
            _statusMessage = string.Empty;
            Refresh();
            return true;
        }

        /// <summary>
        /// Selects one legal item from the current slot's projected choices.
        /// Kept as a public seam for controller/keyboard accessibility and focused tests.
        /// Touch buttons use <see cref="TapGearToEquip069"/> instead.
        /// </summary>
        public bool TrySelectItem(string itemId)
        {
            if (!TryResolveCurrent069(out _, out _, out var slot, out _) || slot == null)
                return false;
            var choice = LegalChoices069(slot)
                .FirstOrDefault(value => StringComparer.Ordinal.Equals(value.ItemId, itemId));
            if (choice == null) return false;

            _selectedItemId = choice.ItemId;
            _focusRegion = FocusRegion069.Item;
            _statusMessage = string.Empty;
            Refresh();
            return true;
        }

        /// <summary>
        /// Phone-first gear action: after the player has chosen an adventurer and a
        /// slot, tapping compatible gear equips it immediately. The existing
        /// selection command remains available for accessibility and compatibility.
        /// </summary>
        public M1CommandResult TapGearToEquip069(string itemId)
        {
            if (!TrySelectItem(itemId))
            {
                _statusMessage = "Choose a gear slot, then tap gear from its pack.";
                _statusPositive = false;
                Refresh();
                return M1CommandResult.Failure(_statusMessage);
            }

            if (!TryResolveExactEquipTarget069(out _, out var slot, out var choice) ||
                slot == null || choice == null)
            {
                _statusMessage = "Gear changed while the Armory was open. Try that tap again.";
                _statusPositive = false;
                Refresh();
                return M1CommandResult.Failure(_statusMessage);
            }

            if (choice.IsEquipped || StringComparer.Ordinal.Equals(slot.EquippedItemId, choice.ItemId))
            {
                _statusMessage = PlayerFacingItemName069(choice) + " is already equipped.";
                _statusPositive = true;
                Refresh();
                return M1CommandResult.Success(_statusMessage);
            }

            return EquipSelected();
        }

        private void CycleRecruit069(int direction)
        {
            if (!_running || _readState == null || direction == 0) return;
            M1PresentationState state;
            try
            {
                state = _readState();
            }
            catch (Exception exception)
            {
                _statusMessage = "Roster refresh failed: " + exception.Message;
                _statusPositive = false;
                Refresh();
                return;
            }

            var recruits = (state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            if (recruits.Length < 2) return;
            var current = Array.FindIndex(recruits, value =>
                StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            if (current < 0) current = 0;
            var next = (current + (direction > 0 ? 1 : -1) + recruits.Length) % recruits.Length;
            Refresh(recruits[next].RecruitId);
        }

        /// <summary>
        /// The single command seam used by the EQUIP button. It deliberately
        /// re-resolves IDs from fresh state before calling the authority.
        /// </summary>
        public M1CommandResult EquipSelected()
        {
            if (!_running || _equip == null)
                return M1CommandResult.Failure("Inventory is not bound to equipment authority.");
            if (!TryResolveExactEquipTarget069(out var recruit, out var slot, out var choice))
            {
                _statusMessage = "Equipment changed while the armory was open. Choose the slot and item again.";
                _statusPositive = false;
                Refresh();
                return M1CommandResult.Failure(_statusMessage);
            }
            if (!CanEquipSelection(slot, choice))
            {
                _statusMessage = EquipBlockedReason069(slot, choice);
                _statusPositive = false;
                Refresh();
                return M1CommandResult.Failure(_statusMessage);
            }

            M1CommandResult result;
            try
            {
                result = _equip(recruit.RecruitId, slot.SlotId, choice.ItemId) ??
                         M1CommandResult.Failure("Equipment authority returned no result.");
            }
            catch (Exception exception)
            {
                result = M1CommandResult.Failure("Equip failed: " + exception.Message);
            }

            _statusPositive = result.Succeeded;
            _statusMessage = result.Succeeded
                ? PlayerFacingItemName069(choice) + " equipped and saved."
                : string.IsNullOrWhiteSpace(result.Message) ? "Equip failed." : result.Message;
            _focusRegion = FocusRegion069.Equip;

            // Runtime coordinators raise Changed synchronously. This explicit pass
            // also keeps delegate seams and lightweight fake coordinators accurate.
            Refresh();
            return result;
        }

        /// <summary>Central enablement policy, exposed as a deterministic test seam.</summary>
        public static bool CanEquipSelection(
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            return slot != null &&
                   choice != null &&
                   choice.IsLegal &&
                   !slot.IsLocked &&
                   !choice.IsEquipped &&
                   !string.IsNullOrWhiteSpace(choice.ItemId) &&
                   !StringComparer.Ordinal.Equals(slot.EquippedItemId, choice.ItemId);
        }

        /// <summary>Stable comparison copy used by the view and focused tests.</summary>
        public static string BuildStatComparison(
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice) =>
            BuildStatComparison(null, slot, choice);

        /// <summary>
        /// Player-facing before/after combat values. These use the same additive
        /// progression-equipment policy consumed by M2 battle construction.
        /// </summary>
        public static string BuildStatComparison(
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            var current = string.IsNullOrWhiteSpace(slot?.EquippedItemName)
                ? "EMPTY"
                : PlayerFacingEquipmentName069(
                    slot.EquippedItemName,
                    slot.EquippedItemId,
                    "Equipped item");
            if (choice == null)
                return current + "  →  PICK GEAR\nTap compatible gear from the pack to equip it.";

            var selected = PlayerFacingItemName069(choice);
            var change = string.IsNullOrWhiteSpace(choice.DirectChange)
                ? "Compatible with this slot."
                : StripTechnicalEquipmentMetadata069(choice.DirectChange);
            if (string.IsNullOrWhiteSpace(change)) change = "Compatible with this slot.";

            var comparison = string.Empty;
            if (IsNewVersion70Loot069(choice))
            {
                comparison = "NEW LOOT  •  " + PlayerFacingRarity069(choice).ToUpperInvariant() +
                             "  •  " + PlayerFacingWeaponFamily069(choice).ToUpperInvariant() + "\n";
            }
            comparison += current + "  →  " + selected + "\nCHANGE  •  " + change;
            if (recruit != null)
            {
                var projectedPhysical = Math.Max(0,
                    recruit.PhysicalAttack - (slot?.EquippedPhysicalAttackBonus ?? 0) +
                    choice.PhysicalAttackBonus);
                var projectedMystic = Math.Max(0,
                    recruit.MysticAttack - (slot?.EquippedMysticAttackBonus ?? 0) +
                    choice.MysticAttackBonus);
                comparison += "\nPHYSICAL POWER  •  " + recruit.PhysicalAttack + " → " +
                              projectedPhysical + "  " +
                              SignedDelta069(projectedPhysical - recruit.PhysicalAttack);
                comparison += "\nMYSTIC POWER  •  " + recruit.MysticAttack + " → " +
                              projectedMystic + "  " +
                              SignedDelta069(projectedMystic - recruit.MysticAttack);
            }
            if (!string.IsNullOrWhiteSpace(choice.ForecastBehavior))
                comparison += "\nIN BATTLE  •  " +
                              StripTechnicalEquipmentMetadata069(choice.ForecastBehavior);
            if (CanEquipSelection(slot, choice))
                comparison += "\nREADY  •  TAP GEAR TO EQUIP";
            return comparison;
        }

        private static string SignedDelta069(int delta) =>
            delta > 0 ? "( +" + delta + " )" :
            delta < 0 ? "( " + delta + " )" : "( no change )";

        /// <summary>
        /// Builds the compact item-list label. Version 70 drops are deliberately
        /// identified as earned loot and expose their real catalog rarity/family,
        /// never the receipt or source markers carried by authoritative state.
        /// </summary>
        public static string BuildInventoryItemLabel069(M1EquipmentChoiceView choice)
        {
            if (choice == null) return "Field gear";
            var name = PlayerFacingItemName069(choice);
            if (IsVersion70Loot069(choice))
            {
                var state = choice.IsEquipped ? "EQUIPPED" : "NEW LOOT";
                return state + "  •  " + PlayerFacingRarity069(choice).ToUpperInvariant() +
                       "  •  " + PlayerFacingWeaponFamily069(choice).ToUpperInvariant() +
                       "\n" + name;
            }

            var itemState = choice.IsEquipped ? "  •  EQUIPPED" : string.Empty;
            return name + "\n" + PlayerFacingRarity069(choice) + itemState;
        }

        /// <summary>True only for an unequipped deterministic 264-catalog drop.</summary>
        public static bool IsNewVersion70Loot069(M1EquipmentChoiceView choice) =>
            IsVersion70Loot069(choice) && !choice.IsEquipped;

        /// <summary>Projects the Version 70 QUALITY_* authority as its actual rarity.</summary>
        public static string PlayerFacingRarity069(M1EquipmentChoiceView choice)
        {
            if (choice == null) return "Basic";
            if (IsVersion70Loot069(choice) &&
                !string.IsNullOrWhiteSpace(choice.QualityId))
            {
                var quality = choice.QualityId.Trim();
                const string qualityPrefix = "QUALITY_";
                if (quality.StartsWith(qualityPrefix, StringComparison.OrdinalIgnoreCase))
                    quality = quality.Substring(qualityPrefix.Length);
                switch (quality.ToUpperInvariant())
                {
                    case "STARTER": return "Starter";
                    case "COMMON": return "Common";
                    case "UNCOMMON": return "Uncommon";
                    case "RARE": return "Rare";
                    case "EPIC": return "Epic";
                    case "LEGENDARY": return "Legendary";
                    case "GODLY": return "Godly";
                }
            }

            return PlayerLabels070.DefaultLabel(
                choice.RarityDisplayName,
                choice.RarityTierId,
                "Basic");
        }

        /// <summary>Maps all seven catalog rarities onto the existing five-color frame set.</summary>
        public static string PlayerFacingRarityTier069(M1EquipmentChoiceView choice)
        {
            if (!IsVersion70Loot069(choice))
                return string.IsNullOrWhiteSpace(choice?.RarityTierId)
                    ? "BASIC"
                    : choice.RarityTierId.Trim().ToUpperInvariant();
            switch (PlayerFacingRarity069(choice).ToUpperInvariant())
            {
                case "COMMON":
                case "UNCOMMON": return "COMMON";
                case "RARE":
                case "EPIC": return "RARE";
                case "LEGENDARY": return "LEGENDARY";
                case "GODLY": return "GODLY";
                default: return "BASIC";
            }
        }

        /// <summary>
        /// Reads the actual WEAPON_FAMILY_* projection embedded in DirectChange.
        /// This avoids reducing the twelve-family catalog to the smaller art atlas.
        /// </summary>
        public static string PlayerFacingWeaponFamily069(M1EquipmentChoiceView choice)
        {
            if (choice == null) return "Equipment";
            var raw = choice.DirectChange ?? string.Empty;
            var segments = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            const string marker = "Weapon Family ";
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index].Trim();
                var markerIndex = segment.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0) continue;
                var family = segment.Substring(markerIndex + marker.Length).Trim();
                if (family.Length == 0) continue;
                if (family.Equals("Spear Polearm", StringComparison.OrdinalIgnoreCase))
                    return "Spear / Polearm";
                return family;
            }

            var visual = PlayerLabels070.DefaultLabel(
                choice.EquipmentVisualId,
                choice.EquipmentVisualId,
                "Weapon");
            return string.IsNullOrWhiteSpace(visual) ? "Weapon" : visual;
        }

        /// <summary>
        /// Removes deterministic bookkeeping markers before copy reaches any Text
        /// component. Other player-meaningful slot, branch and weapon traits remain.
        /// </summary>
        public static string StripTechnicalEquipmentMetadata069(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var segments = value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var visible = new List<string>();
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index].Trim();
                if (IsTechnicalLootSegment069(segment)) continue;
                if (segment.StartsWith("Weapon Family ", StringComparison.OrdinalIgnoreCase) ||
                    segment.Equals("Weapon", StringComparison.OrdinalIgnoreCase))
                    continue;
                segment = RemovePrefix069(segment, "Branch ");
                if (!string.IsNullOrWhiteSpace(segment)) visible.Add(segment);
            }
            return string.Join(", ", visible.ToArray()).Trim();
        }

        private static bool IsVersion70Loot069(M1EquipmentChoiceView choice) =>
            choice != null && !string.IsNullOrWhiteSpace(choice.ItemId) &&
            choice.ItemId.StartsWith(Version70LootItemPrefix069, StringComparison.Ordinal);

        public static string PlayerFacingItemName069(M1EquipmentChoiceView choice) =>
            choice == null
                ? "Field gear"
                : PlayerFacingEquipmentName069(
                    choice.DisplayName,
                    choice.ItemId,
                    "Field weapon");

        /// <summary>
        /// Uses the authoritative definition-shaped display value before an opaque
        /// per-instance ID. Procedural starter gear commonly stores names such as
        /// EQ_PROC_WARD_BUCKLER; that is enough to produce "Ward Buckler" and must
        /// never collapse to a visible Unknown-weapon placeholder.
        /// </summary>
        public static string PlayerFacingEquipmentName069(
            string authoredOrDefinitionName,
            string instanceId,
            string fallback)
        {
            var authorityForLabel = string.IsNullOrWhiteSpace(authoredOrDefinitionName)
                ? instanceId
                : authoredOrDefinitionName;
            return PlayerLabels070.DefaultLabel(
                authoredOrDefinitionName,
                authorityForLabel,
                string.IsNullOrWhiteSpace(fallback) ? "Field gear" : fallback);
        }

        private static bool IsTechnicalLootSegment069(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment)) return false;
            var normalized = segment.Trim()
                .Replace(' ', '_')
                .Replace('-', '_')
                .ToUpperInvariant();
            return normalized.IndexOf(Version70ReceiptMarker069, StringComparison.Ordinal) >= 0 ||
                   normalized.IndexOf(Version70SourceMarker069, StringComparison.Ordinal) >= 0 ||
                   normalized.StartsWith("LOOT_RECEIPT_", StringComparison.Ordinal) ||
                   normalized.StartsWith("LOOT_SOURCE_", StringComparison.Ordinal);
        }

        private static string RemovePrefix069(string value, string prefix) =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                ? value.Substring(prefix.Length).Trim()
                : value;

        // Explicit UI seams let PlayMode tests verify compactness, scrolling and
        // navigation without reflection or reaching into gameplay state.
        public RectTransform RootForTests => _root;
        public ScrollRect ItemScrollForTests => _itemScroll;
        public Button EquipButtonForTests => _equipButton;
        public Button AscendButton090ForTests => _ascendButton090;
        public Button ClaimSignatureWeaponButton090ForTests =>
            _claimSignatureWeaponButton090;
        public IReadOnlyList<Button> PreparedFamilyButtons090ForTests =>
            _preparedFamilyButtons090;
        public Button PreviousRecruitButtonForTests => _previousRecruitButton;
        public Button NextRecruitButtonForTests => _nextRecruitButton;
        public Button BackButtonForTests => _backButton;
        public IReadOnlyList<Button> SlotButtonsForTests => _slotButtons;
        public IReadOnlyList<Button> ItemButtonsForTests => _itemButtons;
        public string SelectedRecruitIdForTests => _selectedRecruitId;
        public string SelectedSlotIdForTests => _selectedSlotId;
        public string SelectedItemIdForTests => _selectedItemId;
        public bool IsRunningForTests => _running;

        private void BeginCore(
            Transform host,
            Func<M1PresentationState> readState,
            Func<string, string, string, M1CommandResult> equipCallback,
            string preferredRecruitId,
            Action closeRequested,
            bool deferInitialRefresh123 = false)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (readState == null) throw new ArgumentNullException(nameof(readState));
            if (equipCallback == null) throw new ArgumentNullException(nameof(equipCallback));

            var resolvedHost = host as RectTransform ?? host.GetComponent<RectTransform>();
            if (resolvedHost == null)
                throw new ArgumentException("Compact inventory host requires a RectTransform.", nameof(host));

            Shutdown();
#if UNITY_EDITOR
            if (!Application.isPlaying && !runInEditMode)
                runInEditMode = true;
#endif
            RuntimeUi.EnsureEventSystem();
            if (EventSystem.current == null)
            {
                var availableEventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
                if (availableEventSystem != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying && !availableEventSystem.runInEditMode)
                        availableEventSystem.runInEditMode = true;
#endif
                    if (EventSystem.current == null)
                    {
                        availableEventSystem.enabled = false;
                        availableEventSystem.enabled = true;
                    }
                }
            }
            _host = resolvedHost;

            _readState = readState;
            _equip = equipCallback;
            _closeRequested = closeRequested;
            _selectedRecruitId = preferredRecruitId;
            _selectedSlotId = null;
            _selectedItemId = null;
            _statusMessage = string.Empty;
            _statusPositive = false;
            _focusRegion = FocusRegion069.Slot;
            _hasBuilt = false;
            _running = true;

            try
            {
                CreateRoot069();
                if (!deferInitialRefresh123) Refresh();
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        private void CreateRoot069()
        {
            var rootObject = new GameObject(RootObjectName, typeof(RectTransform), typeof(Image));
            rootObject.transform.SetParent(_host, false);
            _root = rootObject.GetComponent<RectTransform>();
            Stretch069(_root, 0f);

            var image = rootObject.GetComponent<Image>();
            M1PremiumUi.StylePanel(image, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.AddVerticalLayout(
                _root,
                new RectOffset(22, 22, 18, 18),
                14f,
                TextAnchor.UpperLeft);
        }

        private void BuildLatestSnapshot069()
        {
            var restoreFocus = ShouldRestoreFocus069();
            var eventSystem = EventSystem.current;
            var focused = eventSystem == null ? null : eventSystem.currentSelectedGameObject;
            if (restoreFocus && focused != null && focused.transform.IsChildOf(_root))
                eventSystem.SetSelectedGameObject(null);
            if(LoadoutScrollForTests164!=null)_loadoutScrollPosition164=LoadoutScrollForTests164.normalizedPosition;
            LoadoutScrollForTests164=null;
            ClearChildrenImmediatelyAware069(_root);
            _slotButtons.Clear();
            _itemButtons.Clear();
            _navigableItemButtons.Clear();
            _preparedFamilyButtons090.Clear();
            _itemScroll = null;
            _equipButton = null;
            _ascendButton090 = null;
            _claimSignatureWeaponButton090 = null;
            _findHero155 = null;
            _previousRecruitButton = null;
            _nextRecruitButton = null;
            _backButton = null;
            _selectedSlotButton = null;
            _selectedItemButton = null;

            M1PresentationState state;
            try
            {
                state = _readState?.Invoke();
            }
            catch (Exception exception)
            {
                BuildUnavailable069("Inventory snapshot failed: " + exception.Message);
                FinalizeUnavailable069(restoreFocus);
                return;
            }

            var recruit = ResolveRecruit069(state);
            if (recruit == null)
            {
                BuildUnavailable069("No signed adventurer is available for equipment.");
                FinalizeUnavailable069(restoreFocus);
                return;
            }

            var slot = ResolveSlot069(recruit);
            var choice = ResolveChoice069(slot);
            BuildHeader069(state, recruit);
            BuildAutoEquipmentToolbar112(recruit);
            BuildWorkspace069(recruit, slot, choice);
            ApplyPhoneButtonText164();
            ConfigureNavigation069();
            Canvas.ForceUpdateCanvases();
            if(LoadoutScrollForTests164!=null)StartCoroutine(RestoreLoadoutScroll164(LoadoutScrollForTests164,_loadoutScrollPosition164));
            RevealSelectedItem069(slot, choice);
            if (restoreFocus) RestoreFocus069();
            _hasBuilt = true;
        }

        private M1RecruitLoadoutView ResolveRecruit069(M1PresentationState state)
        {
            var recruits = state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>();
            var recruit = recruits.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            if (recruit == null) recruit = recruits.FirstOrDefault(value => value != null);
            _selectedRecruitId = recruit?.RecruitId;
            return recruit;
        }

        private M1EquipmentSlotView ResolveSlot069(M1RecruitLoadoutView recruit)
        {
            var slots = recruit?.Slots ?? Array.Empty<M1EquipmentSlotView>();
            var slot = slots.FirstOrDefault(value => value != null &&
                StringComparer.Ordinal.Equals(value.SlotId, _selectedSlotId));
            if (slot == null) slot = slots.FirstOrDefault(value => value != null);
            _selectedSlotId = slot?.SlotId;
            return slot;
        }

        private M1EquipmentChoiceView ResolveChoice069(M1EquipmentSlotView slot)
        {
            var choices = LegalChoices069(slot);
            var choice = choices.FirstOrDefault(value =>
                StringComparer.Ordinal.Equals(value.ItemId, _selectedItemId));
            if (choice == null)
            {
                choice = choices.FirstOrDefault(value =>
                             value.IsEquipped || StringComparer.Ordinal.Equals(
                                 value.ItemId,
                                 slot?.EquippedItemId)) ??
                         choices.FirstOrDefault();
            }
            _selectedItemId = choice?.ItemId;
            return choice;
        }

        private static M1EquipmentChoiceView[] LegalChoices069(M1EquipmentSlotView slot)
        {
            return (slot?.Choices ?? Array.Empty<M1EquipmentChoiceView>())
                .Where(value => value != null && value.IsLegal)
                .ToArray();
        }

        private void BuildHeader069(M1PresentationState state, M1RecruitLoadoutView recruit)
        {
            var header = RuntimeUi.AddPanel(_root, "Inventory Header 069", RuntimeUi.PanelOverlay);
            RuntimeUi.SetLayout(header, preferredHeight: 156f);
            M1PremiumUi.StylePanel(header, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddHorizontalLayout(header.transform, new RectOffset(20, 20, 12, 12), 14f);

            var recruits = (state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(value => value != null)
                .ToArray();
            var recruitIndex = Array.FindIndex(recruits, value =>
                StringComparer.Ordinal.Equals(value.RecruitId, recruit.RecruitId));
            if (recruitIndex < 0) recruitIndex = 0;
            var rosterCopy = recruits.Length > 1
                ? "  •  ADVENTURER " + (recruitIndex + 1) + " / " + recruits.Length
                : string.Empty;

            var title = RuntimeUi.AddText(
                header.transform,
                "Inventory Title 069",
                "ARMORY  •  " + PlayerLabels070.DefaultLabel(
                    recruit.DisplayName,
                    recruit.RecruitId,
                    "Adventurer").ToUpperInvariant() + rosterCopy +
                "\nTAP A SLOT  •  TAP GEAR TO EQUIP",
                34,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(title, flexibleWidth: 1f, preferredHeight: RuntimeUi.MinimumTouchPixels);

            AddFindHero155(header.transform);
            if (recruits.Length > 1)
            {
                _previousRecruitButton = RuntimeUi.AddButton(
                    header.transform,
                    "Previous Adventurer 069",
                    "← PREVIOUS",
                    () => CycleRecruit069(-1),
                    RuntimeUi.MinimumTouchPixels);
                RuntimeUi.SetLayout(_previousRecruitButton, preferredWidth: 320f);
                MakeCompactButton069(_previousRecruitButton, RuntimeUi.MinimumTouchPixels, 26);
                AddCancelOnSelectable069(_previousRecruitButton);

                _nextRecruitButton = RuntimeUi.AddButton(
                    header.transform,
                    "Next Adventurer 069",
                    "NEXT →",
                    () => CycleRecruit069(1),
                    RuntimeUi.MinimumTouchPixels);
                RuntimeUi.SetLayout(_nextRecruitButton, preferredWidth: 280f);
                MakeCompactButton069(_nextRecruitButton, RuntimeUi.MinimumTouchPixels, 26);
                AddCancelOnSelectable069(_nextRecruitButton);
            }

            if (_closeRequested != null)
            {
                _backButton = RuntimeUi.AddButton(
                    header.transform,
                    "Back To Hall 069",
                    "BACK TO HALL",
                    () => _closeRequested?.Invoke(),
                    RuntimeUi.MinimumTouchPixels,
                    RuntimeUi.Accent);
                RuntimeUi.SetLayout(_backButton, preferredWidth: 350f);
                MakeCompactButton069(_backButton, RuntimeUi.MinimumTouchPixels, 27);
                AddCancelOnSelectable069(_backButton);
            }
        }

        private void BuildWorkspace069(
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView selectedSlot,
            M1EquipmentChoiceView selectedChoice)
        {
            var workspaceObject = new GameObject(
                "Inventory Workspace 069",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));
            workspaceObject.transform.SetParent(_root, false);
            var workspace = workspaceObject.GetComponent<RectTransform>();
            RuntimeUi.SetLayout(workspace, preferredHeight: 1040f, flexibleHeight: 1f);
            var row = workspaceObject.GetComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(0, 0, 0, 0);
            row.spacing = 18f;
            row.childAlignment = TextAnchor.UpperLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            BuildLoadoutColumn069(workspace, recruit, selectedSlot);
            BuildInventoryColumn069(workspace, recruit, selectedSlot, selectedChoice);
        }

        private void BuildLoadoutColumn069(
            Transform parent,
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView selectedSlot)
        {
            var panel = RuntimeUi.AddPanel(parent, "Equipped Loadout Column 069", RuntimeUi.Panel);
            RuntimeUi.SetLayout(panel, preferredWidth: 900f, flexibleWidth: 0.42f, flexibleHeight: 1f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Iron);
            var content = CompactPhone164 ? CreateLoadoutScroll164(panel.rectTransform) : panel.transform;
            RuntimeUi.AddVerticalLayout(content, new RectOffset(18,18,16,16),9f);

            var heading = RuntimeUi.AddText(
                content,
                "Adventurer Heading 069",
                "ADVENTURER  •  ASCENSION " +
                (recruit.AscensionLevel > 0 ? "★ " : "☆ ") +
                recruit.AscensionLevel + "/" +
                SecondDimension.Gameplay.State.RecruitAscensionRules089.MaximumLevel,
                31,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 42f);
            AddCharacterStandee069(content, recruit);
            if (recruit.IsSssHero)
            {
                var preparation090 = TitanHeroCatalog161.Slot(recruit.SssHeroId) > 0
                    ? null : ReadSssPreparation090(recruit.SssHeroId);
                AddSssProgressionCard090(content, recruit, preparation090);
                if (preparation090 != null &&
                    preparation090.RequiredSlotCount > 0)
                    AddSssPreparedFamilyCard090(
                        content, recruit, preparation090);
            }

            var slotHeading = RuntimeUi.AddText(
                content,
                "Equipped Slots Heading 069",
                "EQUIPPED SLOTS",
                31,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(slotHeading, preferredHeight: 42f);

            var slots = recruit.Slots ?? Array.Empty<M1EquipmentSlotView>();
            var visibleSlots = slots.Where(value => value != null).ToArray();
            var slotRows = Math.Max(1, (visibleSlots.Length + SlotColumns069 - 1) / SlotColumns069);
            var slotGridObject = new GameObject(
                "Equipped Slot Grid 069",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(GridLayoutGroup));
            slotGridObject.transform.SetParent(content, false);
            var slotGridRect = slotGridObject.GetComponent<RectTransform>();
            RuntimeUi.SetLayout(
                slotGridRect,
                preferredHeight: slotRows * RuntimeUi.MinimumTouchPixels + (slotRows - 1) * 10f,
                flexibleHeight: 1f);
            var slotGrid = slotGridObject.GetComponent<GridLayoutGroup>();
            slotGrid.padding = new RectOffset(0, 0, 0, 0);
            slotGrid.spacing = new Vector2(10f, 10f);
            slotGrid.cellSize = new Vector2(410f, RuntimeUi.MinimumTouchPixels);
            slotGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            slotGrid.constraintCount = SlotColumns069;
            slotGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            slotGrid.childAlignment = TextAnchor.UpperCenter;
            slotGridObject.AddComponent<InventoryGridTouch164>();

            foreach (var slot in visibleSlots)
            {
                var captured = slot;
                var selected = StringComparer.Ordinal.Equals(captured.SlotId, selectedSlot?.SlotId);
                var state = captured.IsLocked
                    ? "LOCKED"
                    : string.IsNullOrWhiteSpace(captured.EquippedItemId) ? "EMPTY" : "EQUIPPED";
                var label = PlayerLabels070.DefaultLabel(
                                captured.DisplayName,
                                captured.SlotId,
                                "Equipment slot").ToUpperInvariant() +
                            "\n" + (string.IsNullOrWhiteSpace(captured.EquippedItemName)
                                ? "Empty"
                                : PlayerFacingEquipmentName069(
                                    captured.EquippedItemName,
                                    captured.EquippedItemId,
                                    "Equipped item")) +
                            "  •  " + state;
                if(CompactPhone164)
                {
                    var slotName=PlayerLabels070.DefaultLabel(captured.DisplayName,captured.SlotId,"Equipment slot").ToUpperInvariant();
                    label=slotName+"\n"+(captured.IsLocked?"LOCKED":string.IsNullOrWhiteSpace(captured.EquippedItemId)?"EMPTY":
                        PlayerFacingEquipmentName069(captured.EquippedItemName,captured.EquippedItemId,"Equipped item"));
                }
                var button = RuntimeUi.AddButton(
                    slotGridRect,
                    "Equipped Slot " + captured.SlotId + " 069",
                    label,
                    () => TrySelectSlot(captured.SlotId),
                    RuntimeUi.MinimumTouchPixels,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                MakeCompactButton069(button, RuntimeUi.MinimumTouchPixels, 27);
                M1PremiumUi.AddEquipmentArtworkToButton(
                    button,
                    captured.EquippedVisualId,
                    captured.EquippedRarityTierId,
                    captured.EquippedRarityDisplayName,
                    captured.VisualGlyph,
                    equipped: false,
                    locked: captured.IsLocked);
                AddCancelOnSelectable069(button);
                var slotLabel = button.transform.Find("Label")?.GetComponent<Text>();
                if (slotLabel != null)
                {
                    slotLabel.rectTransform.offsetMin = new Vector2(126f, 10f);
                    slotLabel.rectTransform.offsetMax = new Vector2(captured.IsLocked ? -96f : -16f, -10f);
                }
                _slotButtons.Add(button);
                if (selected) _selectedSlotButton = button;
            }
        }

        private void AddCharacterStandee069(Transform parent, M1RecruitLoadoutView recruit)
        {
            var frame = RuntimeUi.AddPanel(parent, "Character Portrait Standee 069", RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(frame, preferredHeight: CompactPhone164 ? 160f / InventoryScale164 : 310f);
            M1PremiumUi.StylePortraitFrame(frame, selected: true);

            var artObject = new GameObject("Character Art 069", typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(frame.transform, false);
            var artRect = artObject.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.02f, 0.05f);
            artRect.anchorMax = new Vector2(0.43f, 0.95f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
            var art = artObject.GetComponent<Image>();
            art.preserveAspect = true;
            art.raycastTarget = false;

            Sprite sprite = null;
            string unusedResourceKey = string.Empty;
            var hasArt = recruit.IsSssHero && TitanHeroCatalog161.Slot(recruit.SssHeroId) == 0
                ? !string.IsNullOrWhiteSpace(recruit.SssPortraitArtResourcePath) &&
                  M1VisualAssets.TryResolveExactResource090(
                      recruit.SssPortraitArtResourcePath,
                      out sprite)
                : M1VisualAssets.TryResolveBattleStandee(
                    recruit.RecruitId,
                    recruit.VisualSeed,
                    recruit.RaceId,
                    recruit.PortraitAuthorityId,
                    out sprite,
                    out unusedResourceKey);
            if (!hasArt && recruit.RecruitId?.StartsWith("PROC_", StringComparison.Ordinal) == true)
                hasArt = M1VisualAssets.TryResolveMenuStandee091(recruit.RecruitId, recruit.VisualSeed,
                    recruit.RaceId, recruit.PortraitAuthorityId, recruit.ObservedClass,
                    string.Join(" ", recruit.Slots.Select(slot => slot.EquippedItemName)),
                    out sprite, out unusedResourceKey);
            if (hasArt && sprite != null)
            {
                art.sprite = sprite;
                art.color = Color.white;
            }
            else
            {
                art.color = recruit.IsSssHero
                    ? new Color(0.07f, 0.075f, 0.10f, 1f)
                    : M1VisualAssets.FallbackPortraitColor(
                        recruit.RaceId,
                        recruit.VisualSeed,
                        recruit.RecruitId);
                var glyph = RuntimeUi.AddText(
                    artObject.transform,
                    "Character Fallback Crest 069",
                    recruit.IsSssHero
                        ? "SSS\nSPRITE PENDING"
                        : string.IsNullOrWhiteSpace(recruit.ClassSymbol) ? "◆" : recruit.ClassSymbol,
                    recruit.IsSssHero ? 28 : 88,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.Text,
                    FontStyle.Bold);
                Stretch069(glyph.rectTransform, 8f);
                glyph.raycastTarget = false;
            }

            var identity = RuntimeUi.AddText(
                frame.transform,
                "Character Identity 069",
                PlayerLabels070.DefaultLabel(
                    recruit.DisplayName,
                    recruit.RecruitId,
                    "Adventurer") + "\n" +
                PlayerLabels070.DefaultLabel(
                    recruit.ObservedClass,
                    recruit.ObservedClass,
                    "Unknown calling") + "  •  LV " + Math.Max(1, recruit.Level) +
                "\n" + (recruit.IsLegal ? "LOADOUT READY" : "LOADOUT NEEDS ATTENTION"),
                34,
                TextAnchor.MiddleLeft,
                recruit.IsLegal ? RuntimeUi.Text : RuntimeUi.Warning,
                FontStyle.Bold);
            identity.rectTransform.anchorMin = new Vector2(0.47f, 0.53f);
            identity.rectTransform.anchorMax = new Vector2(0.97f, 0.92f);
            identity.rectTransform.offsetMin = Vector2.zero;
            identity.rectTransform.offsetMax = Vector2.zero;
            identity.raycastTarget = false;
            if(CompactPhone164) identity.fontSize = Mathf.CeilToInt(13f / InventoryScale164);

            var statsPlate = RuntimeUi.AddPanel(
                frame.transform,
                "Character Combat Stats Plate 090",
                new Color(0.012f, 0.026f, 0.042f, 0.92f));
            statsPlate.rectTransform.anchorMin = new Vector2(0.45f, 0.05f);
            statsPlate.rectTransform.anchorMax = new Vector2(0.98f, 0.52f);
            statsPlate.rectTransform.offsetMin = Vector2.zero;
            statsPlate.rectTransform.offsetMax = Vector2.zero;
            statsPlate.raycastTarget = false;

            var stats = RuntimeUi.AddText(
                frame.transform,
                "Character Combat Stats 087",
                "VITALS   HP " + Math.Max(0, recruit.MaximumHp) + "   MP " +
                Math.Max(0, recruit.MaximumMp) +
                (TitanHeroCatalog161.Slot(recruit.SssHeroId) > 0
                    ? "\nGROWTH  STR +" + recruit.StrengthBonus + "   MAG +" + recruit.MagicBonus + "   DEF +" + recruit.DefenseBonus +
                      "\nGROWTH  AGI +" + recruit.AgilityBonus + "   WILL +" + recruit.WillBonus
                    : "\nCORE     STR " + recruit.StrengthIndex + "   MAG " + recruit.MagicIndex +
                "   DEF " + recruit.DefenseIndex +
                "\nSPEED    AGI " + recruit.AgilityIndex + "   WILL " + recruit.WillIndex) +
                "\nPOWER    PHYSICAL " + recruit.PhysicalAttack + "   MYSTIC " + recruit.MysticAttack,
                23,
                TextAnchor.UpperLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            stats.rectTransform.anchorMin = new Vector2(0.48f, 0.08f);
            stats.rectTransform.anchorMax = new Vector2(0.96f, 0.49f);
            stats.rectTransform.offsetMin = Vector2.zero;
            stats.rectTransform.offsetMax = Vector2.zero;
            stats.raycastTarget = false;
            if(CompactPhone164)
            {
                artRect.anchorMin=new Vector2(.02f,.40f);artRect.anchorMax=new Vector2(.43f,.97f);
                identity.rectTransform.anchorMin=new Vector2(.47f,.42f);identity.rectTransform.anchorMax=new Vector2(.97f,.95f);
                statsPlate.rectTransform.anchorMin=new Vector2(.025f,.025f);statsPlate.rectTransform.anchorMax=new Vector2(.975f,.385f);
                stats.rectTransform.anchorMin=new Vector2(.045f,.04f);stats.rectTransform.anchorMax=new Vector2(.96f,.37f);
                stats.fontSize=Mathf.CeilToInt(11f/InventoryScale164);
            }
        }

        private void AddSssProgressionCard090(
            Transform parent,
            M1RecruitLoadoutView recruit,
            SssPreparedFamilyLoadoutView090 preparation090)
        {
            var panel = RuntimeUi.AddPanel(
                parent,
                "SSS Ascension Card 090",
                new Color(0.10f, 0.055f, 0.16f, 0.97f));
            RuntimeUi.SetLayout(panel, preferredHeight: 196f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.WorldRibbon);
            RuntimeUi.AddHorizontalLayout(
                panel.transform,
                new RectOffset(16, 16, 12, 12),
                12f);

            var atMaximum = recruit.AscensionLevel >= 10;
            var copy = RuntimeUi.AddText(
                panel.transform,
                "SSS Ascension Status 090",
                (recruit.SssHeroId == TitanHeroCatalog161.EidranId ? "SSSS" : "SSS") + "  •  " + recruit.SssRole.ToUpperInvariant() +
                "  •  A" + recruit.AscensionLevel + " / A10" +
                "\nBOUND CREDITS  " + recruit.AscensionCredits +
                (atMaximum
                    ? "  •  MAXIMUM ASCENSION"
                    : "  •  NEXT +12 HP  +4 MP  +2 ALL CORE") +
                (TitanHeroCatalog161.Slot(recruit.SssHeroId) > 0 ? "\n" + recruit.SssSignatureReadiness :
                "\nOMEGA HUNT  " + recruit.SssHuntFamiliesCompleted +
                " / 70 FAMILIES  •  " + recruit.SssHuntRemainingDefeats + " DEFEATS LEFT" +
                "\n" + recruit.SssSignatureWeaponName.ToUpperInvariant() +
                "  •  " + (recruit.SssSignatureEffectReady
                    ? "SIGNATURE EFFECT ACTIVE"
                    : "OMEGA STATS READY • EFFECT ADAPTER PENDING")),
                21,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(copy, flexibleWidth: 1f, preferredHeight: 168f);
            copy.resizeTextForBestFit = true;
            copy.resizeTextMinSize = 14;
            copy.resizeTextMaxSize = 21;

            _ascendButton090 = RuntimeUi.AddButton(
                panel.transform,
                "Manual SSS Ascend 090",
                atMaximum ? "A10\nMAX" : "ASCEND\nTO A" + (recruit.AscensionLevel + 1),
                () => AscendSelectedSss090(recruit),
                112f,
                recruit.AscensionCredits > 0 && !atMaximum
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal);
            RuntimeUi.SetLayout(_ascendButton090, preferredWidth: 210f, preferredHeight: 112f);
            _ascendButton090.interactable = _ascendSss090 != null &&
                                            recruit.AscensionCredits > 0 &&
                                            !atMaximum;
            MakeCompactButton069(_ascendButton090, 112f, 25);
            AddCancelOnSelectable069(_ascendButton090);

            if (TitanHeroCatalog161.Slot(recruit.SssHeroId) > 0) return;

            var signatureOwned090 = preparation090?.SignatureWeaponOwned == true;
            var signatureClaimable090 = preparation090?.SignatureWeaponClaimable == true;
            _claimSignatureWeaponButton090 = RuntimeUi.AddButton(
                panel.transform,
                "Claim SSS Signature Weapon 090",
                signatureOwned090
                    ? "SIGNATURE\nOWNED"
                    : signatureClaimable090
                        ? "CLAIM\nSIGNATURE"
                        : "OMEGA HUNT\nLOCKED",
                () => ClaimSelectedSssSignatureWeapon090(recruit),
                112f,
                signatureClaimable090
                    ? RuntimeUi.Accent
                    : RuntimeUi.ButtonNormal);
            RuntimeUi.SetLayout(
                _claimSignatureWeaponButton090,
                preferredWidth: 210f,
                preferredHeight: 112f);
            _claimSignatureWeaponButton090.interactable =
                _claimSssSignatureWeapon090 != null && signatureClaimable090;
            MakeCompactButton069(_claimSignatureWeaponButton090, 112f, 23);
            AddCancelOnSelectable069(_claimSignatureWeaponButton090);
        }

        private SssPreparedFamilyLoadoutView090 ReadSssPreparation090(
            string heroId)
        {
            if (_readSssPreparation090 == null ||
                string.IsNullOrWhiteSpace(heroId)) return null;
            try
            {
                return _readSssPreparation090(heroId);
            }
            catch (Exception exception)
            {
                _statusPositive = false;
                _statusMessage = "SSS preparation unavailable: " + exception.Message;
                return null;
            }
        }

        private void AddSssPreparedFamilyCard090(
            Transform parent,
            M1RecruitLoadoutView recruit,
            SssPreparedFamilyLoadoutView090 view)
        {
            var panel = RuntimeUi.AddPanel(
                parent,
                "SSS Gold Family Preparation 090",
                new Color(0.055f, 0.075f, 0.13f, 0.97f));
            RuntimeUi.SetLayout(
                panel,
                preferredHeight: view.RequiredSlotCount == 6 ? 236f : 162f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(
                panel.transform,
                new RectOffset(12, 12, 10, 10),
                7f);

            var heading = RuntimeUi.AddText(
                panel.transform,
                "SSS Gold Preparation Heading 090",
                "GOLD ART LOADOUT  •  " + view.CommandName.ToUpperInvariant() +
                "\n" + view.UnlockedFamilyCount +
                " / 70 FAMILIES UNLOCKED  •  TAP A SLOT TO CYCLE",
                19,
                TextAnchor.MiddleLeft,
                view.Complete ? RuntimeUi.Accent : RuntimeUi.Text,
                FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 54f);
            heading.resizeTextForBestFit = true;
            heading.resizeTextMinSize = 13;
            heading.resizeTextMaxSize = 19;

            var gridObject = new GameObject(
                "SSS Prepared Family Slot Grid 090",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(GridLayoutGroup));
            gridObject.transform.SetParent(panel.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            var rows = Math.Max(1, (view.RequiredSlotCount + 2) / 3);
            RuntimeUi.SetLayout(
                gridRect,
                preferredHeight: rows * 72f + (rows - 1) * 7f);
            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.spacing = new Vector2(7f, 7f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = view.RequiredSlotCount == 1 ? 1 : 3;
            grid.cellSize = view.RequiredSlotCount == 1
                ? new Vector2(816f, 72f)
                : new Vector2(267f, 72f);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;

            for (var slotIndex = 0;
                 slotIndex < view.RequiredSlotCount;
                 slotIndex++)
            {
                var capturedSlot = slotIndex;
                var familyId = slotIndex < view.PreparedFamilyIds.Count
                    ? view.PreparedFamilyIds[slotIndex]
                    : string.Empty;
                var family = view.Family(familyId);
                var label = family == null
                    ? "SLOT " + (slotIndex + 1) + "  •  CHOOSE FAMILY"
                    : "SLOT " + (slotIndex + 1) + "  •  " +
                      family.DisplayName.ToUpperInvariant() +
                      "\nMASTERY R" + family.MasteryRank +
                      "  •  " + family.TotalDefeats + " DEFEATS";
                var button = RuntimeUi.AddButton(
                    gridRect,
                    "SSS Prepared Family Slot " + (slotIndex + 1) + " 090",
                    label,
                    () => CycleSelectedSssPreparedFamily090(
                        recruit, capturedSlot),
                    72f,
                    family == null ? RuntimeUi.ButtonNormal : RuntimeUi.Accent);
                button.interactable = _cycleSssPreparedFamily090 != null &&
                                      view.LegalPreparationBoundary &&
                                      view.UnlockedFamilyCount > 0;
                MakeCompactButton069(button, 72f, 17);
                AddCancelOnSelectable069(button);
                _preparedFamilyButtons090.Add(button);
            }
        }

        private void CycleSelectedSssPreparedFamily090(
            M1RecruitLoadoutView recruit,
            int slotIndex)
        {
            if (_cycleSssPreparedFamily090 == null || recruit == null) return;
            M1CommandResult result;
            try
            {
                result = _cycleSssPreparedFamily090(
                             recruit.SssHeroId, slotIndex) ??
                         M1CommandResult.Failure(
                             "Family preparation authority returned no result.");
            }
            catch (Exception exception)
            {
                result = M1CommandResult.Failure(
                    "Family preparation failed: " + exception.Message);
            }
            _statusPositive = result.Succeeded;
            _statusMessage = result.Message;
            Refresh();
        }

        private void ClaimSelectedSssSignatureWeapon090(
            M1RecruitLoadoutView recruit)
        {
            if (_claimSssSignatureWeapon090 == null || recruit == null) return;
            M1CommandResult result;
            try
            {
                result = _claimSssSignatureWeapon090(recruit.SssHeroId) ??
                         M1CommandResult.Failure(
                             "Signature claim authority returned no result.");
            }
            catch (Exception exception)
            {
                result = M1CommandResult.Failure(
                    "Signature claim failed: " + exception.Message);
            }
            _statusPositive = result.Succeeded;
            _statusMessage = result.Message;
            Refresh();
        }

        private void AscendSelectedSss090(M1RecruitLoadoutView recruit)
        {
            if (_ascendSss090 == null || recruit == null) return;
            M1CommandResult result;
            try
            {
                result = _ascendSss090(recruit.SssHeroId) ??
                         M1CommandResult.Failure("Ascension authority returned no result.");
            }
            catch (Exception exception)
            {
                result = M1CommandResult.Failure("Ascension failed: " + exception.Message);
            }
            _statusPositive = result.Succeeded;
            _statusMessage = result.Message;
            Refresh();
        }

        private void BuildInventoryColumn069(
            Transform parent,
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView selectedSlot,
            M1EquipmentChoiceView selectedChoice)
        {
            var panel = RuntimeUi.AddPanel(parent, "Inventory Pack Column 069", RuntimeUi.Panel);
            RuntimeUi.SetLayout(panel, preferredWidth: 1500f, flexibleWidth: 0.58f, flexibleHeight: 1f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.EtchedGlass);
            RuntimeUi.AddVerticalLayout(panel.transform, new RectOffset(18, 18, 16, 16), 10f);

            var slotName = selectedSlot == null
                ? "PICK A GEAR SLOT"
                : "PACK  •  " + PlayerLabels070.DefaultLabel(
                    selectedSlot.DisplayName,
                    selectedSlot.SlotId,
                    "Equipment slot").ToUpperInvariant() + "  •  TAP GEAR TO EQUIP";
            var heading = RuntimeUi.AddText(
                panel.transform,
                "Item List Heading 069",
                slotName,
                34,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 48f);

            var choices = LegalChoices069(selectedSlot);
            var content = CreateItemScroll069(panel.transform);
            for (var index = 0; index < choices.Length; index++)
            {
                var captured = choices[index];
                var capturedIndex = index;
                var selected = StringComparer.Ordinal.Equals(captured.ItemId, selectedChoice?.ItemId);
                var label = BuildInventoryItemLabel069(captured) +
                            BuildInventoryChoiceDelta069(recruit, selectedSlot, captured);
                var button = RuntimeUi.AddButton(
                    content,
                    "Inventory Item " + captured.ItemId + " 069",
                    label,
                    () => TapGearToEquip069(captured.ItemId),
                    RuntimeUi.MinimumTouchPixels,
                    selected ? RuntimeUi.Accent : RuntimeUi.ButtonNormal);
                MakeCompactButton069(button, RuntimeUi.MinimumTouchPixels, 31);
                M1PremiumUi.AddEquipmentArtworkToButton(
                    button,
                    captured.EquipmentVisualId,
                    PlayerFacingRarityTier069(captured),
                    PlayerFacingRarity069(captured),
                    captured.VisualGlyph,
                    captured.IsEquipped,
                    locked: false);
                ApplyInventoryItemReadability069(button, selected, captured.IsEquipped);
                AddScrollOnFocus069(button, capturedIndex, choices.Length);
                AddCancelOnSelectable069(button);
                _itemButtons.Add(button);
                _navigableItemButtons.Add(button);
                if (selected) _selectedItemButton = button;
            }

            if (choices.Length == 0)
            {
                var empty = RuntimeUi.AddText(
                    content,
                    "Empty Inventory Note 069",
                    "The quartermaster has no compatible item for this slot.",
                    34,
                    TextAnchor.MiddleCenter,
                    RuntimeUi.MutedText,
                    FontStyle.Italic);
                RuntimeUi.SetLayout(empty, preferredHeight: 180f);
            }

            BuildComparisonPanel069(panel.transform, recruit, selectedSlot, selectedChoice);

            _equipButton = RuntimeUi.AddButton(
                panel.transform,
                "Equip Selected Item 069",
                CanEquipSelection(selectedSlot, selectedChoice)
                    ? "TAP THE GEAR ABOVE TO EQUIP"
                    : selectedChoice != null && selectedChoice.IsEquipped
                        ? "EQUIPPED AND ACTIVE"
                        : "TAP GEAR TO EQUIP",
                () => EquipSelected(),
                RuntimeUi.MinimumTouchPixels,
                RuntimeUi.Accent);
            MakeCompactButton069(_equipButton, RuntimeUi.MinimumTouchPixels, 38);
            // Retain this control as an accessibility fallback for existing controller
            // navigation. Touch players equip directly from a gear card above.
            _equipButton.interactable = CanEquipSelection(selectedSlot, selectedChoice);
            AddCancelOnSelectable069(_equipButton);
        }

        private Transform CreateItemScroll069(Transform parent)
        {
            var scrollObject = new GameObject(
                "Scrollable Item List 069",
                typeof(RectTransform),
                typeof(Image),
                typeof(ScrollRect),
                typeof(LayoutElement));
            scrollObject.transform.SetParent(parent, false);
            var scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = RuntimeUi.PanelOverlay;
            M1PremiumUi.StylePanel(scrollImage, M1PremiumUi.Surface.WorldGlass);
            RuntimeUi.SetLayout(scrollObject.GetComponent<RectTransform>(), preferredHeight: 610f, flexibleHeight: 1f);

            var viewportObject = new GameObject(
                "Item List Viewport 069",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = new Vector2(0.965f, 1f);
            viewport.offsetMin = new Vector2(10f, 10f);
            viewport.offsetMax = new Vector2(-4f, -10f);
            var viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = Color.white;
            viewportObject.GetComponent<Mask>().showMaskGraphic = false;

            var contentObject = new GameObject(
                "Item List Content 069",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var layout = contentObject.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarObject = new GameObject(
                "Item List Scrollbar 069",
                typeof(RectTransform),
                typeof(Image),
                typeof(Scrollbar));
            scrollbarObject.transform.SetParent(scrollObject.transform, false);
            var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
            scrollbarRect.anchorMin = new Vector2(0.972f, 0.02f);
            scrollbarRect.anchorMax = new Vector2(0.995f, 0.98f);
            scrollbarRect.offsetMin = Vector2.zero;
            scrollbarRect.offsetMax = Vector2.zero;
            scrollbarObject.GetComponent<Image>().color = RuntimeUi.PanelRaised;

            var handleObject = new GameObject("Scrollbar Handle 069", typeof(RectTransform), typeof(Image));
            handleObject.transform.SetParent(scrollbarObject.transform, false);
            var handle = handleObject.GetComponent<RectTransform>();
            Stretch069(handle, 2f);
            handleObject.GetComponent<Image>().color = RuntimeUi.Accent;
            var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleObject.GetComponent<Image>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            _itemScroll = scrollObject.GetComponent<ScrollRect>();
            _itemScroll.viewport = viewport;
            _itemScroll.content = content;
            _itemScroll.horizontal = false;
            _itemScroll.vertical = true;
            _itemScroll.movementType = ScrollRect.MovementType.Clamped;
            _itemScroll.inertia = true;
            _itemScroll.scrollSensitivity = 54f;
            _itemScroll.verticalScrollbar = scrollbar;
            _itemScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            _itemScroll.verticalNormalizedPosition = 1f;
            return content;
        }

        private void BuildComparisonPanel069(
            Transform parent,
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            if(CompactPhone164){BuildPhoneComparison164(parent,recruit,slot,choice);return;}
            var comparison = RuntimeUi.AddPanel(parent, "Stat Comparison 069", RuntimeUi.PanelRaised);
            RuntimeUi.SetLayout(comparison, preferredHeight: 330f);
            M1PremiumUi.StylePanel(comparison, M1PremiumUi.Surface.Parchment);

            var heading = RuntimeUi.AddText(
                comparison.transform,
                "Stat Comparison Heading 069",
                "BEFORE  →  AFTER",
                28,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorComparison069(heading.rectTransform, new Rect(0.025f, 0.855f, 0.95f, 0.105f));
            heading.raycastTarget = false;

            var currentName = string.IsNullOrWhiteSpace(slot?.EquippedItemName)
                ? "EMPTY SLOT"
                : PlayerFacingEquipmentName069(
                    slot.EquippedItemName,
                    slot.EquippedItemId,
                    "Equipped item");
            var selectedName = choice == null ? "PICK GEAR" : PlayerFacingItemName069(choice);
            var details = RuntimeUi.AddText(
                comparison.transform,
                "Stat Comparison Details 069",
                PlayerLabels070.DefaultLabel(slot?.DisplayName, slot?.SlotId, "GEAR").ToUpperInvariant() +
                "  •  " + currentName + "  →  " + selectedName,
                22,
                TextAnchor.MiddleLeft,
                RuntimeUi.Text,
                FontStyle.Bold);
            AnchorComparison069(details.rectTransform, new Rect(0.025f, 0.725f, 0.95f, 0.115f));
            details.resizeTextForBestFit = true;
            details.resizeTextMinSize = 15;
            details.resizeTextMaxSize = 22;
            details.raycastTarget = false;

            var currentPhysical = Math.Max(0, recruit?.PhysicalAttack ?? 0);
            var currentMystic = Math.Max(0, recruit?.MysticAttack ?? 0);
            var projectedPhysical = choice == null
                ? currentPhysical
                : Math.Max(0, currentPhysical - (slot?.EquippedPhysicalAttackBonus ?? 0) +
                                   choice.PhysicalAttackBonus);
            var projectedMystic = choice == null
                ? currentMystic
                : Math.Max(0, currentMystic - (slot?.EquippedMysticAttackBonus ?? 0) +
                                   choice.MysticAttackBonus);

            BuildComparisonStatCard069(
                comparison.transform,
                "Current Stats Card 090",
                "CURRENT",
                currentPhysical,
                currentMystic,
                new Rect(0.025f, 0.265f, 0.285f, 0.420f),
                new Color(0.035f, 0.085f, 0.135f, 0.96f),
                RuntimeUi.Text);

            var arrow = RuntimeUi.AddText(
                comparison.transform,
                "Equipment Change Arrow 090",
                "→",
                42,
                TextAnchor.MiddleCenter,
                RuntimeUi.Accent,
                FontStyle.Bold);
            AnchorComparison069(arrow.rectTransform, new Rect(0.320f, 0.36f, 0.075f, 0.22f));
            arrow.raycastTarget = false;

            BuildComparisonStatCard069(
                comparison.transform,
                "Projected Stats Card 090",
                choice != null && choice.IsEquipped ? "ACTIVE" : "AFTER EQUIP",
                projectedPhysical,
                projectedMystic,
                new Rect(0.405f, 0.265f, 0.285f, 0.420f),
                new Color(0.045f, 0.125f, 0.095f, 0.96f),
                RuntimeUi.Positive);

            var physicalDelta = projectedPhysical - currentPhysical;
            var mysticDelta = projectedMystic - currentMystic;
            var deltaColor = physicalDelta < 0 || mysticDelta < 0
                ? RuntimeUi.Warning
                : physicalDelta > 0 || mysticDelta > 0
                    ? RuntimeUi.Positive
                    : RuntimeUi.MutedText;
            var deltaCard = RuntimeUi.AddPanel(
                comparison.transform,
                "Equipment Delta Card 090",
                new Color(0.018f, 0.030f, 0.044f, 0.96f));
            AnchorComparison069(deltaCard.rectTransform, new Rect(0.705f, 0.265f, 0.270f, 0.420f));
            M1PremiumUi.StylePanel(deltaCard, M1PremiumUi.Surface.WorldGlass);
            var delta = RuntimeUi.AddText(
                deltaCard.transform,
                "Equipment Delta Values 090",
                "CHANGE\nPHYSICAL  " + SignedCompactDelta069(physicalDelta) +
                "\nMYSTIC  " + SignedCompactDelta069(mysticDelta),
                22,
                TextAnchor.MiddleCenter,
                deltaColor,
                FontStyle.Bold);
            AnchorComparison069(delta.rectTransform, new Rect(0.06f, 0.08f, 0.88f, 0.84f));
            delta.resizeTextForBestFit = true;
            delta.resizeTextMinSize = 14;
            delta.resizeTextMaxSize = 22;
            delta.raycastTarget = false;

            var status = string.IsNullOrWhiteSpace(_statusMessage)
                ? EquipBlockedReason069(slot, choice)
                : _statusMessage;
            var statusPositive = string.IsNullOrWhiteSpace(_statusMessage)
                ? CanEquipSelection(slot, choice)
                : _statusPositive;
            var statusText = RuntimeUi.AddText(
                comparison.transform,
                "Inventory Status 069",
                status,
                27,
                TextAnchor.MiddleLeft,
                statusPositive ? RuntimeUi.Positive : RuntimeUi.Warning,
                FontStyle.Bold);
            AnchorComparison069(statusText.rectTransform, new Rect(0.025f, 0.045f, 0.95f, 0.155f));
            statusText.resizeTextForBestFit = true;
            statusText.resizeTextMinSize = 14;
            statusText.resizeTextMaxSize = 22;
            statusText.raycastTarget = false;
        }

        private static string BuildInventoryChoiceDelta069(
            M1RecruitLoadoutView recruit,
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            if (recruit == null || slot == null || choice == null || choice.IsEquipped)
                return string.Empty;
            var physical = choice.PhysicalAttackBonus - slot.EquippedPhysicalAttackBonus;
            var mystic = choice.MysticAttackBonus - slot.EquippedMysticAttackBonus;
            if (physical == 0 && mystic == 0) return "  •  NO STAT CHANGE";
            return "  •  PWR " + SignedCompactDelta069(physical) +
                   "  MYS " + SignedCompactDelta069(mystic);
        }

        private static string SignedCompactDelta069(int delta) =>
            delta > 0 ? "+" + delta : delta.ToString();

        private static void BuildComparisonStatCard069(
            Transform parent,
            string objectName,
            string heading,
            int physical,
            int mystic,
            Rect anchors,
            Color background,
            Color textColor)
        {
            var card = RuntimeUi.AddPanel(parent, objectName, background);
            AnchorComparison069(card.rectTransform, anchors);
            M1PremiumUi.StylePanel(card, M1PremiumUi.Surface.WorldGlass);
            card.color = background;
            var copy = RuntimeUi.AddText(
                card.transform,
                objectName + " Values",
                heading + "\nPHYSICAL  " + physical + "\nMYSTIC  " + mystic,
                22,
                TextAnchor.MiddleCenter,
                textColor,
                FontStyle.Bold);
            AnchorComparison069(copy.rectTransform, new Rect(0.06f, 0.08f, 0.88f, 0.84f));
            copy.resizeTextForBestFit = true;
            copy.resizeTextMinSize = 14;
            copy.resizeTextMaxSize = 22;
            copy.raycastTarget = false;
        }

        private static void AnchorComparison069(RectTransform rect, Rect anchors)
        {
            if (rect == null) return;
            rect.anchorMin = anchors.min;
            rect.anchorMax = anchors.max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static string EquipBlockedReason069(
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            if (slot == null) return "Tap a gear slot first.";
            if (slot.IsLocked) return "LOCKED  •  Unlock this slot before changing gear.";
            if (choice == null) return "Tap compatible gear from the pack.";
            if (!choice.IsLegal)
                return string.IsNullOrWhiteSpace(choice.LegalityReason)
                    ? "This item cannot be equipped."
                    : choice.LegalityReason;
            if (choice.IsEquipped || StringComparer.Ordinal.Equals(slot.EquippedItemId, choice.ItemId))
                return "Equipped and active.";
            return "Tap the gear card to equip it.";
        }

        private void ConfigureNavigation069()
        {
            var headerButtons = new[] { _findHero155, _previousRecruitButton, _nextRecruitButton, _backButton,
                _autoEquipHero112, _autoEquipUnions112, _undoAutoEquip112 }
                .Where(value => value != null)
                .ToArray();
            for (var index = 0; index < headerButtons.Length; index++)
            {
                var left = headerButtons[(index - 1 + headerButtons.Length) % headerButtons.Length];
                var right = headerButtons[(index + 1) % headerButtons.Length];
                var bodyTarget = _selectedSlotButton ?? _selectedItemButton ??
                                 (_equipButton != null && _equipButton.interactable
                                     ? _equipButton
                                     : headerButtons[index]);
                SetExplicitNavigation069(
                    headerButtons[index],
                    headerButtons[index],
                    bodyTarget,
                    left,
                    right);
            }

            for (var index = 0; index < _slotButtons.Count; index++)
            {
                var column = index % SlotColumns069;
                var upIndex = index - SlotColumns069;
                Selectable up;
                if (upIndex >= 0)
                {
                    up = _slotButtons[upIndex];
                }
                else if (headerButtons.Length > 0)
                {
                    up = headerButtons[Math.Min(column, headerButtons.Length - 1)];
                }
                else
                {
                    upIndex = _slotButtons.Count - 1;
                    while (upIndex > 0 && upIndex % SlotColumns069 != column) upIndex--;
                    up = _slotButtons[upIndex];
                }
                var downIndex = index + SlotColumns069;
                if (downIndex >= _slotButtons.Count)
                    downIndex = Math.Min(column, _slotButtons.Count - 1);
                var left = column > 0 ? _slotButtons[index - 1] : _slotButtons[index];
                var right = column + 1 < SlotColumns069 && index + 1 < _slotButtons.Count
                    ? _slotButtons[index + 1]
                    : _selectedItemButton ?? _navigableItemButtons.FirstOrDefault() ??
                      (_equipButton != null && _equipButton.interactable
                          ? _equipButton
                          : _slotButtons[index]);
                SetExplicitNavigation069(
                    _slotButtons[index],
                    up,
                    _slotButtons[downIndex],
                    left,
                    right);
            }

            for (var index = 0; index < _navigableItemButtons.Count; index++)
            {
                var up = index > 0 ? _navigableItemButtons[index - 1] : _selectedSlotButton;
                var canReachEquip = _equipButton != null && _equipButton.interactable;
                var down = index + 1 < _navigableItemButtons.Count
                    ? _navigableItemButtons[index + 1]
                    : canReachEquip ? _equipButton : _navigableItemButtons[0];
                SetExplicitNavigation069(
                    _navigableItemButtons[index],
                    up,
                    down,
                    _selectedSlotButton,
                    canReachEquip ? _equipButton : _navigableItemButtons[index]);
            }

            if (_equipButton != null)
            {
                SetExplicitNavigation069(
                    _equipButton,
                    _selectedItemButton ?? _navigableItemButtons.LastOrDefault() ?? _selectedSlotButton,
                    _selectedSlotButton,
                    _selectedSlotButton,
                    _equipButton);
            }
            if (_ascendButton090 != null)
            {
                SetExplicitNavigation069(
                    _ascendButton090,
                    _previousRecruitButton ?? _backButton ?? _ascendButton090,
                    _selectedSlotButton ?? _slotButtons.FirstOrDefault() ?? _ascendButton090,
                    _ascendButton090,
                    _selectedSlotButton ?? _ascendButton090);
            }
        }

        private static void SetExplicitNavigation069(
            Selectable selectable,
            Selectable up,
            Selectable down,
            Selectable left,
            Selectable right)
        {
            if (selectable == null) return;
            var navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = up;
            navigation.selectOnDown = down;
            navigation.selectOnLeft = left;
            navigation.selectOnRight = right;
            selectable.navigation = navigation;
        }

        private void AddScrollOnFocus069(Button button, int index, int count)
        {
            var trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            entry.callback.AddListener(_ => RevealItemIndex069(index, count));
            trigger.triggers.Add(entry);
        }

        private void AddCancelOnSelectable069(Selectable selectable)
        {
            if (selectable == null || _closeRequested == null) return;
            var trigger = selectable.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = selectable.gameObject.AddComponent<EventTrigger>();
            if (trigger.triggers == null) trigger.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.Cancel };
            entry.callback.AddListener(_ => _closeRequested?.Invoke());
            trigger.triggers.Add(entry);
        }

        private void RevealSelectedItem069(
            M1EquipmentSlotView slot,
            M1EquipmentChoiceView choice)
        {
            var choices = LegalChoices069(slot);
            var index = Array.FindIndex(choices, value => value != null &&
                StringComparer.Ordinal.Equals(value.ItemId, choice?.ItemId));
            if (index >= 0) RevealItemIndex069(index, choices.Length);
        }

        private void RevealItemIndex069(int index, int count)
        {
            if (_itemScroll == null) return;
            _itemScroll.verticalNormalizedPosition = count <= 1
                ? 1f
                : 1f - Mathf.Clamp01(index / (float)(count - 1));
        }

        private bool ShouldRestoreFocus069()
        {
            if (EventSystem.current == null) return false;
            if (!_hasBuilt) return true;
            var selected = EventSystem.current.currentSelectedGameObject;
            return selected == null || (_root != null && selected.transform.IsChildOf(_root));
        }

        private void RestoreFocus069()
        {
            if (EventSystem.current == null) return;
            GameObject target;
            switch (_focusRegion)
            {
                case FocusRegion069.Item:
                    target = (_selectedItemButton ?? _navigableItemButtons.FirstOrDefault() ??
                              _selectedSlotButton ?? _previousRecruitButton ?? _nextRecruitButton ?? _backButton)
                        ?.gameObject;
                    break;
                case FocusRegion069.Equip:
                    target = _equipButton != null && _equipButton.interactable
                        ? _equipButton.gameObject
                        : (_selectedItemButton ?? _selectedSlotButton ??
                           _previousRecruitButton ?? _nextRecruitButton ?? _backButton)?.gameObject;
                    break;
                default:
                    target = (_selectedSlotButton ?? _slotButtons.FirstOrDefault() ??
                              _previousRecruitButton ?? _nextRecruitButton ?? _backButton)?.gameObject;
                    break;
            }
            if (target != null && target.activeInHierarchy)
                EventSystem.current.SetSelectedGameObject(target);
        }

        private bool TryResolveCurrent069(
            out M1PresentationState state,
            out M1RecruitLoadoutView recruit,
            out M1EquipmentSlotView slot,
            out M1EquipmentChoiceView choice)
        {
            state = null;
            recruit = null;
            slot = null;
            choice = null;
            if (!_running || _readState == null) return false;
            try
            {
                state = _readState();
            }
            catch
            {
                return false;
            }
            recruit = ResolveRecruit069(state);
            slot = ResolveSlot069(recruit);
            choice = ResolveChoice069(slot);
            return recruit != null;
        }

        private bool TryResolveExactEquipTarget069(
            out M1RecruitLoadoutView recruit,
            out M1EquipmentSlotView slot,
            out M1EquipmentChoiceView choice)
        {
            recruit = null;
            slot = null;
            choice = null;
            if (!_running || _readState == null ||
                string.IsNullOrWhiteSpace(_selectedRecruitId) ||
                string.IsNullOrWhiteSpace(_selectedSlotId) ||
                string.IsNullOrWhiteSpace(_selectedItemId))
                return false;

            M1PresentationState state;
            try
            {
                state = _readState();
            }
            catch
            {
                return false;
            }

            recruit = (state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.RecruitId, _selectedRecruitId));
            slot = (recruit?.Slots ?? Array.Empty<M1EquipmentSlotView>())
                .FirstOrDefault(value => value != null &&
                    StringComparer.Ordinal.Equals(value.SlotId, _selectedSlotId));
            choice = (slot?.Choices ?? Array.Empty<M1EquipmentChoiceView>())
                .FirstOrDefault(value => value != null && value.IsLegal &&
                    StringComparer.Ordinal.Equals(value.ItemId, _selectedItemId));
            return recruit != null && slot != null && choice != null;
        }

        private void BuildUnavailable069(string message)
        {
            var header = RuntimeUi.AddText(
                _root,
                "Inventory Unavailable Heading 069",
                "ARMORY",
                42,
                TextAnchor.MiddleLeft,
                RuntimeUi.Accent,
                FontStyle.Bold);
            RuntimeUi.SetLayout(header, preferredHeight: 76f);
            var panel = RuntimeUi.AddPanel(_root, "Inventory Unavailable 069", RuntimeUi.Panel);
            RuntimeUi.SetLayout(panel, flexibleHeight: 1f);
            M1PremiumUi.StylePanel(panel, M1PremiumUi.Surface.Iron);
            var copy = RuntimeUi.AddText(
                panel.transform,
                "Inventory Unavailable Copy 069",
                message,
                38,
                TextAnchor.MiddleCenter,
                RuntimeUi.Warning,
                FontStyle.Bold);
            Stretch069(copy.rectTransform, 36f);

            if (_closeRequested != null)
            {
                _backButton = RuntimeUi.AddButton(
                    _root,
                    "Unavailable Back To Hall 069",
                    "BACK TO HALL",
                    () => _closeRequested?.Invoke(),
                    RuntimeUi.MinimumTouchPixels,
                    RuntimeUi.Accent);
                MakeCompactButton069(_backButton, RuntimeUi.MinimumTouchPixels, 30);
                AddCancelOnSelectable069(_backButton);
            }
        }

        private void FinalizeUnavailable069(bool restoreFocus)
        {
            ConfigureNavigation069();
            Canvas.ForceUpdateCanvases();
            if (restoreFocus) RestoreFocus069();
            _hasBuilt = true;
        }

        private static void MakeCompactButton069(Button button, float height, int fontSize)
        {
            if (button == null) return;
            var layout = button.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = height;
                layout.preferredHeight = height;
            }
            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null) label.fontSize = fontSize;
        }

        private static void ApplyInventoryItemReadability069(
            Button button,
            bool selected,
            bool equipped)
        {
            if (button == null) return;
            var colors = button.colors;
            if (selected)
            {
                colors.normalColor = RuntimeUi.Accent;
                colors.highlightedColor = RuntimeUi.Warning;
                colors.selectedColor = RuntimeUi.Warning;
                colors.pressedColor = new Color(0.63f, 0.44f, 0.17f, 1f);
            }
            else
            {
                colors.normalColor = RuntimeUi.ButtonNormal;
                colors.highlightedColor = RuntimeUi.ButtonHighlighted;
                colors.selectedColor = RuntimeUi.ButtonHighlighted;
                colors.pressedColor = RuntimeUi.ButtonPressed;
            }
            button.colors = colors;

            var label = button.transform.Find("Label")?.GetComponent<Text>();
            if (label != null)
            {
                // Equipment artwork owns the first 23.5% of every pack row. Pixel
                // padding scaled down at 1280x800 and let that frame paint across
                // the beginning of item names, so reserve a proportional text
                // column with a visible gap at every supported desktop resolution.
                label.rectTransform.anchorMin = new Vector2(0.260f, 0f);
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12f, 12f);
                label.rectTransform.offsetMax = new Vector2(
                    equipped ? -210f : -24f,
                    -12f);
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 18;
                label.resizeTextMaxSize = 31;
                label.color = selected
                    ? new Color(0.07f, 0.08f, 0.09f, 1f)
                    : RuntimeUi.Text;
            }
        }

        private static void Stretch069(RectTransform rect, float inset)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void ClearChildrenImmediatelyAware069(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index--)
            {
                var child = parent.GetChild(index).gameObject;
                child.SetActive(false);
                DestroyUiObject069(child);
            }
        }

        private static void DestroyUiObject069(GameObject value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }

        private void HandleAuthorityChanged()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (_running) Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
