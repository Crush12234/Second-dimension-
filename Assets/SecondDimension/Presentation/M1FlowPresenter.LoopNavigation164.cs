using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private RectTransform _loopStrip164;
        private RectTransform _loopInventoryHost164;
        private Image _loopModal164;
        private Text _loopCurrent164;
        private Text _loopNotice164;
        private Button _loopBack164;
        private bool _loopRouting164;
        private bool _loopMenuAutoWasEnabled164;
        private float _loopLastScale164;
        private int _loopLastWidth164, _loopLastHeight164;
        private string _loopLastLocation164;
        private readonly List<LoopFrame164> _loopHistory164 = new List<LoopFrame164>();
        private readonly Dictionary<string, LoopFrame164> _loopBookmarks164 = new Dictionary<string, LoopFrame164>(StringComparer.Ordinal);

        private sealed class LoopFrame164
        {
            public string OwnerLoop, Tab, Applicant, GuildApplicant, Recruit, Slot, Item, TownFacility, Merchant, Offer;
            public M1Screen Screen;
            public int Union, ApplicantPage, GuildApplicantPage, InventoryPage, StockPage, TitanPage, TitanSlot, TitanTier;
            public int TownPouchPage165;
            public bool Inventory, TownMerchants, TownDetail, TownOffer, TitanDetail, TownPouch165;
            public bool AutoOrders;
            public float BattleSpeed;
            public Vector2 Scroll, InventoryScroll, LoadoutScroll;
        }

        private bool LoopNavigationAvailable164 => _coordinator is M1RuntimeCoordinator owner && owner.CanNavigateLoops164;

        private bool LoopStripVisible164 => LoopNavigationAvailable164 &&
            _screen != M1Screen.MainMenu && _screen != M1Screen.NewGuild &&
            _screen != M1Screen.FirstHourOpening && _screen != M1Screen.FirstHourGuildReady &&
            !_showAccessibilityOptions156;

        private string CurrentLoopLocation164()
        {
            if (_compactInventory069 != null && _compactInventory069.IsRunningForTests) return "HEROES";
            if (_screen == M1Screen.UnionBuilder) return "UNIONS";
            if (_screen == M1Screen.Equipment) return "HEROES";
            if (_screen == M1Screen.Battle || _screen == M1Screen.BattleResults)
                return ((_coordinator as M1RuntimeCoordinator)?.CurrentLoop164 ?? "CAMPAIGN") + " · BATTLE";
            switch (_guildCityTab017D)
            {
                case "ABYSS": return "TOWER";
                case "TITANS": return "TITANS";
                case "CAMPAIGN": case "EXPEDITION": case "WORLD GATE": case "DEFENSE": case "CONTRACTS": case "PARTY": return "CAMPAIGN";
                case "TOWN": return _townMerchantsOpen153 ? "MERCHANTS" : "TOWN";
                case "PROGRESSION": return "FORGE";
                case "DEVELOPMENT": return "HERO TRAINING";
                case "APPLICANTS": return "RECRUITMENT";
                case "HALL": return "GUILD HALL";
                default: return (_guildCityTab017D ?? "GUILD").Replace('_', ' ');
            }
        }

        private string CurrentLoopDestination164()
        {
            var location=CurrentLoopLocation164();
            if(location.Contains(" · BATTLE"))return ((_coordinator as M1RuntimeCoordinator)?.CurrentLoop164)??"CAMPAIGN";
            if(location=="HERO TRAINING")return "TRAINING";
            if(location=="GUILD HALL")return "HALL";
            return location;
        }

        private void RefreshLoopNavigation164()
        {
            if (_canvas == null || _screenRoot == null) return;
            var visible = LoopStripVisible164;
            var scale = Mathf.Max(.1f, _canvas.scaleFactor);
            var height = 48f / scale;
            // Replace the old vertical margins instead of adding a second full
            // header margin. The existing page retains nearly all of its height.
            _screenRoot.offsetMin = new Vector2(96f, visible ? 0f : 42f);
            _screenRoot.offsetMax = new Vector2(-96f, visible ? -height : -42f);
            if (_loopInventoryHost164 != null)
                _loopInventoryHost164.offsetMax = new Vector2(0f, visible ? -height : 0f);
            if (_loopStrip164 == null && visible)
            {
                var panel = RuntimeUi.AddPanel(_screenRoot.parent, "Loop navigation strip164", RuntimeUi.Panel);
                _loopStrip164 = panel.rectTransform;
                _loopStrip164.anchorMin = new Vector2(0f, 1f);
                _loopStrip164.anchorMax = Vector2.one;
                _loopStrip164.pivot = new Vector2(.5f, 1f);
                _loopBack164 = LoopButton164(panel.transform, "Loop back164", "← BACK", BackThroughLoops164);
                LoopRect164((RectTransform)_loopBack164.transform, .012f, .04f, .23f, .92f);
                _loopCurrent164 = RuntimeUi.AddText(panel.transform, "Current loop164", "", 32,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
                _loopCurrent164.raycastTarget = false;
                LoopRect164(_loopCurrent164.rectTransform, .245f, .04f, .50f, .92f);
                var menu = LoopButton164(panel.transform, "Loop menu164", "LOOPS", OpenLoopMenu164);
                LoopRect164((RectTransform)menu.transform, .76f, .04f, .228f, .92f);
            }
            if (_loopStrip164 != null)
            {
                _loopStrip164.gameObject.SetActive(visible);
                _loopStrip164.sizeDelta = new Vector2(0f, height);
                _loopStrip164.anchoredPosition = Vector2.zero;
                _loopStrip164.SetAsLastSibling();
                if (visible)
                {
                    _loopCurrent164.text = CurrentLoopLocation164();
                    LoopText164(_loopCurrent164, 16f);
                    var backLabel = _loopBack164.GetComponentInChildren<Text>();
                    backLabel.text = _loopHistory164.Count > 0 ? "← BACK" : "← GUILD";
                    _loopBack164.interactable = _loopHistory164.Count > 0 || CurrentLoopLocation164() != "GUILD HALL";
                    foreach (var b in _loopStrip164.GetComponentsInChildren<Button>()) LoopText164(b.GetComponentInChildren<Text>(), 15f);
                }
            }
            _loopLastScale164 = scale; _loopLastWidth164 = Screen.width; _loopLastHeight164 = Screen.height;
            _loopLastLocation164 = CurrentLoopLocation164();
        }

        private void LateUpdate()
        {
            if (_canvas == null || _screenRoot == null) return;
            if (_loopLastWidth164 != Screen.width || _loopLastHeight164 != Screen.height ||
                !Mathf.Approximately(_loopLastScale164, _canvas.scaleFactor) ||
                _loopLastLocation164 != CurrentLoopLocation164()) RefreshLoopNavigation164();
        }

        private Transform LoopInventoryHost164()
        {
            RefreshLoopNavigation164();
            if (_loopInventoryHost164 == null)
                _loopInventoryHost164 = RuntimeUi.AddStretchRect(_screenRoot.parent, "Loop inventory content164");
            _loopInventoryHost164.offsetMin = Vector2.zero;
            _loopInventoryHost164.offsetMax = new Vector2(0f, LoopStripVisible164 ? -48f / Mathf.Max(.1f, _canvas.scaleFactor) : 0f);
            _loopInventoryHost164.SetAsLastSibling();
            if (_loopStrip164 != null) _loopStrip164.SetAsLastSibling();
            return _loopInventoryHost164;
        }

        private void OpenLoopMenu164()
        {
            if (_loopModal164 != null || !LoopStripVisible164) return;
            _loopMenuAutoWasEnabled164 = _battleExperience072 != null && _battleExperience072.AutoOrdersEnabled091;
            if (_battleExperience072 != null) _battleExperience072.SetAutoOrders091(false);
            _loopModal164 = RuntimeUi.AddPanel(_canvas.transform, "Loop modal blocker164", new Color(.01f,.02f,.04f,1f));
            Stretch(_loopModal164.rectTransform);
            // The backdrop intentionally catches pointer events; labels never do.
            _loopModal164.raycastTarget = true;
            _loopModal164.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel = () => CloseLoopMenu164(true);
            var safe = RuntimeUi.AddSafeArea(_loopModal164.transform);
            BuildIllustratedLoopHub165(safe);
        }

        private void CloseLoopMenu164(bool restoreAuto)
        {
            if (_loopModal164 != null)
            {
                _loopModal164.gameObject.SetActive(false); Destroy(_loopModal164.gameObject); _loopModal164 = null;
            }
            _loopNotice164 = null;
            if (restoreAuto && _loopMenuAutoWasEnabled164 && _battleExperience072 != null && _battleExperience072.IsActive)
                _battleExperience072.SetAutoOrders091(true);
            _loopMenuAutoWasEnabled164 = false;
        }

        private bool TryRouteLoopService164(string tab)
        {
            if (_loopRouting164 || !LoopNavigationAvailable164) return false;
            string id;
            switch (tab)
            {
                case "CAMPAIGN": id="CAMPAIGN"; break;
                case "WORLD GATE": case "DEFENSE": case "EXPEDITION": case "CONTRACTS": case "PARTY": id="CAMPAIGN"; break;
                case "GUIDE": case "DETAILS": case "DUTIES": case "PEOPLE": case "RELATIONSHIPS": case "CITY": case "CHRONICLE": case "CODES": id="TOWN"; break;
                case "ABYSS": id="TOWER"; break;
                case "TITANS": id="TITANS"; break;
                case "TOWN": id="TOWN"; break;
                case "MERCHANT153": id="MERCHANTS"; break;
                case "PROGRESSION": id="FORGE"; break;
                case "EQUIPMENT": id="HEROES"; break;
                case "UNIONS": id="UNIONS"; break;
                case "DEVELOPMENT": id="TRAINING"; break;
                case "APPLICANTS": id="RECRUITMENT"; break;
                case "HALL": id="HALL"; break;
                default: return false;
            }
            var exactPage = tab=="WORLD GATE"||tab=="DEFENSE"||tab=="EXPEDITION"||tab=="CONTRACTS"||tab=="PARTY"||
                tab=="GUIDE"||tab=="DETAILS"||tab=="DUTIES"||tab=="PEOPLE"||tab=="RELATIONSHIPS"||tab=="CITY"||tab=="CHRONICLE"||tab=="CODES" ? tab : null;
            OpenLoopDestination164(id, null, false, exactPage); return true;
        }

        private bool SwitchLoopOwner164(string target)
        {
            var owner = _coordinator as M1RuntimeCoordinator;
            if (owner == null) return false;
            if (_battleResolving || (_battleExperience072 != null && _battleExperience072.IsResolving) ||
                (_compactInventory069 != null && _compactInventory069.IsAutoEquipmentResolving123))
            { LoopNavigationFailure164("The current action is finishing. Try again in a moment."); return false; }
            var battleVisible = IsFirstHourBattleExperienceActive072;
            if (_battleExperience072 != null) _battleExperience072.SetAutoOrders091(false);
            if (battleVisible) SetFirstHourBattleExperienceVisible072(false);
            var wasBuilding = _building; _building = true;
            M1CommandResult result;
            try { result = owner.SwitchLoop164(target); }
            finally { _building = wasBuilding; }
            if (result == null || !result.Succeeded)
            {
                if (battleVisible) SetFirstHourBattleExperienceVisible072(true);
                LoopNavigationFailure164(result?.Message ?? "This loop could not open. Your current progress is unchanged."); return false;
            }
            // Release delayed view work only after the native owner switch succeeds.
            // Durable card and dice receipts remain saved for their original loop.
            StopAllCoroutines();
            _pendingLayoutPass = null;
            ClearWorldGateReceiptApplyScheduling084();
            ClearCampaignCardScheduling129();
            return true;
        }

        private void OpenLoopDestination164(string destination, string preferredRecruit = null, bool resumeBookmark = true, string exactPage = null)
        {
            if (!LoopNavigationAvailable164 || _loopRouting164) return;
            if(resumeBookmark && _loopModal164 != null && CurrentLoopDestination164()==destination && string.IsNullOrEmpty(preferredRecruit))
            {CloseLoopMenu164(true);return;}
            var previous = CaptureLoopFrame164();
            var previousDestination=CurrentLoopDestination164();
            var ownerId = destination == "CAMPAIGN" || destination == "TOWER" || destination == "TITANS" ? destination : "TOWN";
            if (!SwitchLoopOwner164(ownerId)) return;
            CloseLoopMenu164(false);
            if (previous.Screen != M1Screen.MainMenu)
            {
                _loopBookmarks164[previousDestination]=previous;
                if (_loopHistory164.Count >= 32) _loopHistory164.RemoveAt(0);
                _loopHistory164.Add(previous);
            }
            _loopRouting164 = true;
            try
            {
                CloseVersion69Experiences069();
                var resume = ((M1RuntimeCoordinator)_coordinator).LoopResumeScreen164(ownerId);
                if (ownerId != "TOWN" && (resume == M1Screen.Battle || resume == M1Screen.BattleResults))
                {
                    if(_loopBookmarks164.TryGetValue(destination,out var battleBookmark))
                    { _battleAnimationSpeed=battleBookmark.BattleSpeed;Navigate(resume);RestoreLoopBattleChoices164(battleBookmark); }
                    else Navigate(resume);
                    return;
                }
                var resumeTab = ((M1RuntimeCoordinator)_coordinator).LoopResumeTab164(ownerId);
                if (!string.IsNullOrEmpty(resumeTab))
                { _screen=M1Screen.GuildOperations;_guildCityTab017D=resumeTab;BuildCurrentScreen();return; }
                if(!string.IsNullOrEmpty(exactPage))
                { _screen=M1Screen.GuildOperations;_guildCityTab017D=exactPage;_guildCityMoreOpen060=false;BuildCurrentScreen();return; }
                if(resumeBookmark&&string.IsNullOrEmpty(preferredRecruit)&&_loopBookmarks164.TryGetValue(destination,out var bookmark))
                {RestoreLoopFrame164(bookmark);return;}
                _screen = M1Screen.GuildOperations;
                _guildCityMoreOpen060 = false;
                switch (destination)
                {
                    case "CAMPAIGN": OpenMainCampaign158(); break;
                    case "TOWER": OpenTownService153("ABYSS"); break;
                    case "TITANS": OpenTownService153("TITANS"); break;
                    case "TOWN": _townMerchantsOpen153=false; OpenTownService153("TOWN"); break;
                    case "MERCHANTS": _townMerchantsOpen153=true; _townCompactOffer153=false; OpenTownService153("TOWN"); break;
                    case "FORGE": OpenTownService153("PROGRESSION"); break;
                    case "TRAINING": OpenTownService153("DEVELOPMENT"); break;
                    case "RECRUITMENT": OpenTownService153("APPLICANTS"); break;
                    case "UNIONS": Navigate(M1Screen.UnionBuilder); break;
                    case "HEROES": _screen=M1Screen.GuildOperations; OpenFocusedCompactInventory069(preferredRecruit ?? _selectedRecruitId); break;
                    default: OpenTownService153("HALL"); break;
                }
            }
            finally { _loopRouting164=false; RefreshLoopNavigation164(); }
        }

        private LoopFrame164 CaptureLoopFrame164()
        {
            var inventory = _compactInventory069 != null && _compactInventory069.IsRunningForTests;
            return new LoopFrame164 { OwnerLoop=((M1RuntimeCoordinator)_coordinator).CurrentLoop164,
                Screen=_screen,Tab=_guildCityTab017D,Applicant=_selectedApplicantId,Recruit=inventory?_compactInventory069.SelectedRecruitIdForTests:_selectedRecruitId,
                Slot=inventory?_compactInventory069.SelectedSlotIdForTests:_selectedSlotId,Item=inventory?_compactInventory069.SelectedItemIdForTests:_selectedItemId,
                Union=_selectedUnionIndex,ApplicantPage=_applicantPage,GuildApplicant=_guildApplicantSelectedId066,GuildApplicantPage=_guildApplicantPage066,InventoryPage=_inventoryMemberPage068,Inventory=inventory,
                TownFacility=_townFacility153,Merchant=_townMerchant153,Offer=_townOffer153,StockPage=_townStockPage163,
                TownMerchants=_townMerchantsOpen153,TownDetail=_townCompactDetail153,TownOffer=_townCompactOffer153,
                TownPouch165=_townPouch165,TownPouchPage165=_townPouchPage165,
                TitanPage=_titanPage164,TitanSlot=_titanSlot161,TitanTier=_titanTier161,TitanDetail=_titanDetail161,
                AutoOrders=_loopModal164!=null?_loopMenuAutoWasEnabled164:(_battleExperience072!=null&&_battleExperience072.AutoOrdersEnabled091),
                BattleSpeed=_battleExperience072!=null?_battleExperience072.AnimationSpeed:_battleAnimationSpeed,
                LoadoutScroll=inventory&&_compactInventory069.LoadoutScrollForTests164!=null?_compactInventory069.LoadoutScrollForTests164.normalizedPosition:Vector2.up,
                Scroll=_activeScroll!=null?_activeScroll.normalizedPosition:Vector2.up,
                InventoryScroll=inventory&&_compactInventory069.ItemScrollForTests!=null?_compactInventory069.ItemScrollForTests.normalizedPosition:Vector2.up };
        }

        private void BackThroughLoops164()
        {
            if (_loopHistory164.Count == 0) { OpenLoopDestination164("HALL"); return; }
            var frame=_loopHistory164[_loopHistory164.Count-1];
            var previous=CaptureLoopFrame164();var previousDestination=CurrentLoopDestination164();
            if (!SwitchLoopOwner164(frame.OwnerLoop)) return;
            _loopBookmarks164[previousDestination]=previous;
            CloseLoopMenu164(false); _loopHistory164.RemoveAt(_loopHistory164.Count-1);
            _loopRouting164=true;
            try
            {
                CloseVersion69Experiences069();
                RestoreLoopFrame164(frame);
            }
            finally { _loopRouting164=false; RefreshLoopNavigation164(); }
        }

        private void RestoreLoopFrame164(LoopFrame164 frame)
        {
                var resume=((M1RuntimeCoordinator)_coordinator).LoopResumeScreen164(frame.OwnerLoop);
                if(frame.OwnerLoop!="TOWN"&&(resume==M1Screen.Battle||resume==M1Screen.BattleResults))
                {_battleAnimationSpeed=frame.BattleSpeed;Navigate(resume);RestoreLoopBattleChoices164(frame);return;}
                _screen=frame.Screen;_guildCityTab017D=frame.Tab;_selectedApplicantId=frame.Applicant;_selectedRecruitId=frame.Recruit;
                if(_screen==M1Screen.Battle||_screen==M1Screen.BattleResults)
                {_screen=M1Screen.GuildOperations;_guildCityTab017D=frame.OwnerLoop=="TOWER"?"ABYSS":frame.OwnerLoop=="TITANS"?"TITANS":"CAMPAIGN";}
                _selectedSlotId=frame.Slot;_selectedItemId=frame.Item;_selectedUnionIndex=frame.Union;_applicantPage=frame.ApplicantPage;
                _guildApplicantSelectedId066=frame.GuildApplicant;_guildApplicantPage066=frame.GuildApplicantPage;
                _inventoryMemberPage068=frame.InventoryPage;_townFacility153=frame.TownFacility;_townMerchant153=frame.Merchant;
                _townOffer153=frame.Offer;_townStockPage163=frame.StockPage;_townMerchantsOpen153=frame.TownMerchants;
                _townCompactDetail153=frame.TownDetail;_townCompactOffer153=frame.TownOffer;
                _townPouch165=frame.TownPouch165;_townPouchPage165=frame.TownPouchPage165;
                _titanPage164=frame.TitanPage;_titanSlot161=frame.TitanSlot;_titanTier161=frame.TitanTier;_titanDetail161=frame.TitanDetail;
                if (frame.Inventory)
                {
                    OpenFocusedCompactInventory069(frame.Recruit);
                    if (!string.IsNullOrEmpty(frame.Slot)) _compactInventory069?.TrySelectSlot(frame.Slot);
                    if (!string.IsNullOrEmpty(frame.Item)) _compactInventory069?.TrySelectItem(frame.Item);
                }
                else BuildCurrentScreen();
                StartCoroutine(RestoreLoopScroll164(frame));
        }

        private IEnumerator RestoreLoopScroll164(LoopFrame164 frame)
        {
            yield return null; yield return null;
            if(frame.Inventory && _compactInventory069!=null && _compactInventory069.ItemScrollForTests!=null)
                _compactInventory069.ItemScrollForTests.normalizedPosition=frame.InventoryScroll;
            else if(_activeScroll!=null)_activeScroll.normalizedPosition=frame.Scroll;
            if(frame.Inventory&&_compactInventory069!=null&&_compactInventory069.LoadoutScrollForTests164!=null)
                _compactInventory069.LoadoutScrollForTests164.normalizedPosition=frame.LoadoutScroll;
        }

        private void RestoreLoopBattleChoices164(LoopFrame164 frame)
        {
            if(_battleExperience072==null)return;
            _battleExperience072.AnimationSpeed=frame.BattleSpeed;
            var battle=M2BattleViewAccess098.Read(_coordinator);
            if(battle!=null&&!battle.IsResolved)_battleExperience072.SetAutoOrders091(frame.AutoOrders);
        }

        private void LoopNavigationFailure164(string message)
        {
            if(_loopNotice164==null)OpenLoopMenu164();
            if (_loopNotice164 != null) { _loopNotice164.text=message;_loopNotice164.color=RuntimeUi.Warning; }
        }

        private Button LoopButton164(Transform parent,string name,string label,Action action,Color? color=null)
        {
            var b=RuntimeUi.AddButton(parent,name,label,action,RuntimeUi.PrimaryTouchPixels,color);
            var text=b.GetComponentInChildren<Text>();text.raycastTarget=false;LoopText164(text,15f);
            return b;
        }

        private void LoopText164(Text text,float pixels)
        {
            if(text==null)return;
            var scale=Mathf.Max(.1f,_canvas.scaleFactor);
            text.fontSize=Mathf.RoundToInt(pixels*Mathf.Clamp(_textScale,1f,1.45f)/scale);
            text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.RoundToInt(12f/scale);
            text.resizeTextMaxSize=text.fontSize;text.raycastTarget=false;
        }

        private static void LoopRect164(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(x+w,y+h);rect.offsetMin=rect.offsetMax=Vector2.zero; }
    }
}
