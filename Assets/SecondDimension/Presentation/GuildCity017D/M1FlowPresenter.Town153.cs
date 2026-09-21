using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    // Native presentation only. All prices, eligibility, grants and save authority stay
    // in the coordinator. Viewing a facility or selecting an offer never mutates a save.
    public sealed partial class M1FlowPresenter
    {
        private string _townFacility153 = "GUILD_HALL";
        private string _townMerchant153 = "";
        private string _townOffer153 = "";
        private bool _townMerchantsOpen153;
        private int _townStockPage163;
        private bool _townCompactDetail153;
        private bool _townCompactOffer153;
        private bool _townPouch165;
        private int _townPouchPage165;
        private static readonly Color TownInk153 = new Color(.035f, .061f, .086f, .97f);
        private static readonly Color TownGold153 = new Color(.92f, .76f, .43f, 1f);
        private static readonly Dictionary<string, Sprite> TownArtCache153 =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private static readonly HashSet<string> TownArtKeys153 = BuildTownArtKeys153();

        // Entry point: BuildGuildOperations017D should route the TOWN tab here.
        private void BuildTown153()
        {
            var authority = _coordinator as M1RuntimeCoordinator;
            if (authority == null) return;
            var view = authority.ReadTown153();
            var compact = Screen.width < 1000 || Screen.height < 570 || _textScale >= 1.25f;
            RuntimeUi.ClearChildren(_screenRoot);
            RuntimeUi.EnsureEventSystem();
            _activeContent = null;
            _activeScroll = null;
            var root = RuntimeUi.AddPanel(_screenRoot, "Skyhome Town 153", TownInk153);
            Stretch(root.rectTransform);
            _activePage = root.rectTransform;
            root.raycastTarget = false;
            root.gameObject.AddComponent<TownViewport153>().Changed = () =>
            {
                if (_screen == M1Screen.GuildOperations && _guildCityTab017D == "TOWN") BuildCurrentScreen();
            };
            var backdrop = TownImage153(root.transform, "Skyhome streets 153",
                _townMerchantsOpen153 ? ResolveTitleSkyhomeMarket071() : ResolveTownArt153("SKYHOME_TOWN154"), new Rect(0, 0, 1, 1));
            backdrop.preserveAspect = false;
            var cover = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (backdrop.sprite != null) cover.aspectRatio = backdrop.sprite.rect.width / backdrop.sprite.rect.height;
            var scrim = TownPanel153(root.transform, "Town readability 153",
                new Rect(0, 0, 1, 1), new Color(.015f, .03f, .05f, _highContrast ? .66f : .08f));
            scrim.raycastTarget = false;
            TownPanel153(root.transform, "Town heading 153", new Rect(0, .85f, 1, .15f), TownInk153);
            TownLabel153(root.transform, "Town title 153", _townMerchantsOpen153 ? "MERCHANT ROW" : "SKYHOME",
                new Rect(.035f, .91f, .55f, .085f), compact ? 21 : 29, TownGold153, true);
            TownLabel153(root.transform, "Town treasury 153", "TREASURY  " + FormatProgressionNumber(view.Treasury) + " XP",
                new Rect(.58f, .918f, .385f, .06f), compact ? 13 : 18, RuntimeUi.Positive, true, TextAnchor.MiddleRight);
            TownLabel153(root.transform, "Town subtitle 153", _townMerchantsOpen153
                    ? view.StockSummary
                    : "Your Guild's home between adventures.",
                new Rect(.036f, .851f, .92f, .058f), compact ? 12 : 15, RuntimeUi.Text);

            if (_townMerchantsOpen153 && _townPouch165)
                DrawTownPouch165(root.transform, authority, compact);
            else if (_townMerchantsOpen153)
                DrawTownMerchants153(root.transform, authority, view, compact);
            else
                DrawTownMap153(root.transform, authority, view, compact);

            TownPanel153(root.transform, "Town footer 153", new Rect(0, 0, 1, compact ? .17f : .12f), TownInk153);
            var back = TownButton153(root.transform, "Town back 153", _townMerchantsOpen153 || _townCompactDetail153
                    ? "←  TOWN" : "←  GUILD HALL", new Rect(.03f, compact ? .015f : .028f, compact ? (_townMerchantsOpen153 ? .22f : .33f) : .22f, compact ? .136f : .068f), () =>
                {
                    if (_townMerchantsOpen153 || _townCompactDetail153)
                    {
                        _townMerchantsOpen153 = false; _townCompactDetail153 = false;
                        _townCompactOffer153 = false; _townPouch165 = false; BuildCurrentScreen();
                    }
                    else OpenTownService153("HALL");
                }, true, false, compact ? 14 : 16);
            var status = !string.IsNullOrWhiteSpace(_localStatus) ? _localStatus : view.Status;
            if (string.IsNullOrWhiteSpace(status)) status = "Purchases and upgrades save immediately.";
            if(_townMerchantsOpen153)
            {
                TownButton153(root.transform,"Town pouch165",_townPouch165 ? "SHOPS" : "TRAVEL POUCH",
                    new Rect(compact?.27f:.28f,compact?.015f:.035f,compact?.285f:.22f,compact?.136f:.068f),
                    ()=>{_townPouch165=!_townPouch165;BuildCurrentScreen();},true,_townPouch165,compact?12:14);
                TownButton153(root.transform,"Town restock159","RESTOCK · "+FormatProgressionNumber(view.RestockCost)+" XP",
                    new Rect(compact?.575f:.525f,compact?.015f:.035f,compact?.39f:.25f,compact?.136f:.068f),()=>ReviewTownRestock159(authority),view.CanRestock,false,compact?12:15);
            }
            if(!_townMerchantsOpen153)TownLabel153(root.transform, "Town status 153", status,
                _townMerchantsOpen153?new Rect(.55f,.016f,.415f,.09f):new Rect(compact ? .39f : .28f, .016f, compact ? .575f : .685f, .09f), compact ? 11 : 14,
                !string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive ? RuntimeUi.Warning : RuntimeUi.MutedText,
                false, TextAnchor.MiddleRight);
            back.Select();
        }

        private void DrawTownPouch165(Transform root, M1RuntimeCoordinator authority, bool compact)
        {
            var view=authority.ReadTownConsumables165();
            var panel=TownPanel153(root,"Travel pouch165",new Rect(.035f,.18f,.93f,.66f),TownInk153);
            TownLabel153(panel.transform,"Travel pouch heading165","TRAVEL POUCH",new Rect(.03f,.87f,.63f,.12f),compact?18:24,TownGold153,true);
            TownLabel153(panel.transform,"Travel pouch active165",view.HasActiveLuck?"LUCK READY":"NO ACTIVE CHARM",new Rect(.66f,.87f,.31f,.12f),compact?11:14,view.HasActiveLuck?RuntimeUi.Positive:RuntimeUi.MutedText,true,TextAnchor.MiddleRight);
            TownScrollText153(panel.transform,"Travel pouch guidance165",(!_localStatusPositive&&!string.IsNullOrWhiteSpace(_localStatus)?_localStatus+"\n":"")+view.Status,new Rect(.03f,.69f,.94f,.16f),compact?12:15);
            var items=view.Items??Array.Empty<TownConsumable165>();
            var pages=Math.Max(1,(items.Length+1)/2);_townPouchPage165=Mathf.Clamp(_townPouchPage165,0,pages-1);
            if(items.Length==0)
                TownLabel153(panel.transform,"Travel pouch empty165","Your pouch is empty. Visit Hearthpack Provisions to buy a luck tonic.",new Rect(.04f,.26f,.92f,.31f),compact?15:20,RuntimeUi.Text);
            for(var index=_townPouchPage165*2;index<items.Length&&index<(_townPouchPage165+1)*2;index++)
            {
                var item=items[index];var y=.46f-(index%2)*.24f;
                TownImage153(panel.transform,"Pouch tonic art165",ResolveTownArt153("CONSUMABLE:LUCK165"),new Rect(.035f,y,.095f,.20f));
                TownLabel153(panel.transform,"Pouch item "+item.InstanceId+"165",item.Name,new Rect(.145f,y,.50f,.20f),compact?14:18,RuntimeUi.Text,true);
                TownButton153(panel.transform,"Use pouch "+item.InstanceId+"165",view.HasActiveLuck?"LUCK READY":"USE TONIC",new Rect(.675f,y,.29f,.20f),()=>
                {
                    var result=authority.UseTownConsumable165(item.InstanceId,view.Revision);
                    _localStatus=result.Message;_localStatusPositive=result.Succeeded;BuildCurrentScreen();
                },item.CanUse,false,compact?12:15);
            }
            if(pages>1)
            {
                TownButton153(panel.transform,"Pouch previous165","←",new Rect(.035f,.007f,.20f,.19f),()=>{_townPouchPage165--;BuildCurrentScreen();},_townPouchPage165>0);
                TownLabel153(panel.transform,"Pouch page165",(_townPouchPage165+1)+" / "+pages,new Rect(.30f,.015f,.40f,.10f),11,RuntimeUi.MutedText,false,TextAnchor.MiddleCenter);
                TownButton153(panel.transform,"Pouch next165","→",new Rect(.765f,.007f,.20f,.19f),()=>{_townPouchPage165++;BuildCurrentScreen();},_townPouchPage165+1<pages);
            }
        }

        private void DrawTownMap153(Transform root, M1RuntimeCoordinator authority, TownView153 view, bool compact)
        {
            if(view.Facilities==null||view.Facilities.Length==0)return;
            var selected=view.Facilities.FirstOrDefault(f=>f.Id==_townFacility153)??view.Facilities[0];
            _townFacility153=selected.Id;
            if(!compact||!_townCompactDetail153)
            {
                // These world positions follow the real illustrated destinations.
                // The environment stays visible; a hotspot identifies the service.
                var locations=new[]{new Vector2(.31f,.73f),new Vector2(.13f,.52f),
                    new Vector2(.36f,.37f),new Vector2(.135f,.19f),new Vector2(.60f,.53f),
                    new Vector2(.60f,.25f),new Vector2(.38f,.16f),new Vector2(.58f,.77f)};
                for(int i=0;i<view.Facilities.Length;i++)
                {
                    var f=view.Facilities[i];var p=locations[i];
                    var rect=compact?new Rect(.035f+(i%2)*.48f,.68f-(i/2)*.16f,.45f,.13f)
                        :new Rect(p.x-.075f,p.y,.175f,.067f);
                    var node=TownButton153(root,"Town landmark "+f.Id+" 154",f.Name+"  ·  "+f.Level,rect,()=>
                        {_townFacility153=f.Id;_townCompactDetail153=true;_localStatus="";BuildCurrentScreen();},true,f.Id==selected.Id,compact?13:13);
                    var face=node.GetComponent<Image>();face.color=f.Id==selected.Id?new Color(.30f,.21f,.09f,.96f):new Color(.025f,.055f,.082f,.88f);
                    if(!compact)
                    {
                        var marker=TownLabel153(root,"Landmark pin "+f.Id+"154","◆",new Rect(p.x-.007f,p.y+.066f,.032f,.035f),15,TownGold153,true,TextAnchor.MiddleCenter);
                        marker.gameObject.AddComponent<TownGlow154>();
                    }
                }
            }
            if(!compact||_townCompactDetail153)
            {
                var panel=TownPanel153(root,"Destination details154",compact?new Rect(.035f,.18f,.93f,.665f):new Rect(.76f,.145f,.225f,.70f),new Color(.022f,.035f,.055f,.94f));
                if(!compact)TownLabel153(panel.transform,"Destination district154","SKYHOME  /  YOUR GUILD",new Rect(.07f,.885f,.86f,.065f),11,TownGold153,true);
                TownLabel153(panel.transform,"Destination name154",selected.Name,compact?new Rect(.07f,.81f,.86f,.16f):new Rect(.07f,.69f,.86f,.175f),compact?22:23,RuntimeUi.Text,true);
                TownLabel153(panel.transform,"Destination level154","FACILITY LEVEL  "+selected.Level,compact?new Rect(.07f,.72f,.86f,.08f):new Rect(.07f,.60f,.86f,.055f),12,TownGold153,true);
                TownScrollText153(panel.transform,"Destination purpose154",selected.Description,
                    compact?(selected.Id=="GUILD_HALL"?new Rect(.07f,.49f,.86f,.20f):new Rect(.07f,.25f,.86f,.45f)):
                    selected.Id=="GUILD_HALL"?new Rect(.07f,.425f,.86f,.15f):new Rect(.07f,.295f,.86f,.28f),compact?16:15);
                if(selected.Id=="GUILD_HALL")
                    TownButton153(panel.transform,"Open Titan trials161","TITAN TRIALS",compact?new Rect(.07f,.27f,.86f,.19f):new Rect(.07f,.295f,.86f,.105f),
                        ()=>OpenTownService153("TITANS"),true,selected.Level>=10,compact?15:14);
                TownButton153(panel.transform,"Use facility "+selected.Id+"154",selected.ServiceLabel,compact?new Rect(.07f,.035f,selected.CanUpgrade?.41f:.86f,.19f):new Rect(.07f,.16f,.86f,.11f),()=>
                {if(selected.ServiceTab=="MERCHANT153"){_townMerchantsOpen153=true;_townMerchant153="";_townCompactOffer153=false;BuildCurrentScreen();}
                 else OpenTownService153(selected.ServiceTab);},selected.CanUse,false,compact?15:14);
                if(selected.CanUpgrade)
                    TownButton153(panel.transform,"Improve facility "+selected.Id+"154",selected.UpgradeLabel,compact?new Rect(.52f,.035f,.41f,.19f):new Rect(.07f,.025f,.86f,.10f),()=>ReviewTownBuilding153(authority,selected.Id),true,false,compact?14:13);
            }
        }

        private void DrawTownMerchants153(Transform root, M1RuntimeCoordinator authority, TownView153 view, bool compact)
        {
            var merchants = view.Merchants;
            if (merchants == null || merchants.Length == 0) return;
            var selected = merchants.FirstOrDefault(x => x.Id == _townMerchant153);
            if (!compact && selected == null) selected = merchants[0];
            if (!compact || selected == null)
            {
                for (int index = 0; index < merchants.Length; index++)
                {
                    var merchant = merchants[index];
                    var rect = compact ? new Rect(.035f+(index%2)*.48f, .64f-(index/2)*.205f, .45f, .19f)
                        : new Rect(.025f + index * .193f, .715f, .185f, .13f);
                    var button = TownButton153(root, "Merchant " + merchant.Id + " 153", "", rect, () =>
                        {
                            _townMerchant153 = merchant.Id; _townOffer153 = ""; _townStockPage163 = 0;
                            _townCompactOffer153 = false; BuildCurrentScreen();
                        }, true, selected != null && merchant.Id == selected.Id);
                    TownImage153(button.transform, "Merchant portrait " + merchant.Id + " 153",
                        ResolveTownMerchantPortrait153(merchant.Id), new Rect(.03f, .10f, .27f, .80f));
                    var merchantName156D = TownLabel153(button.transform, "Merchant name " + merchant.Id + " 153", merchant.Name,
                        new Rect(.33f, .45f, .64f, .44f), compact ? 17 : 14, RuntimeUi.Text, true);
                    if (_textScale > 1f)
                    {
                        // Fixed single-line slot: prefer the requested scale, but
                        // retain a visible name when the enlarged line is too tall.
                        ConfigureResponsiveText062(merchantName156D,
                            Mathf.RoundToInt((compact ? 17 : 14) * 2.6f), merchantName156D.fontSize);
                        merchantName156D.verticalOverflow = VerticalWrapMode.Truncate;
                    }
                    TownLabel153(button.transform, "Merchant shop " + merchant.Id + " 153", merchant.Specialty165 ?? merchant.Shop,
                        new Rect(.33f, .07f, .64f, .38f), compact ? 13 : 11, TownGold153);
                }
            }
            if (selected == null) return;
            _townMerchant153 = selected.Id;
            if(!compact)
            {
                TownPanel153(root,"Merchant character backdrop154",new Rect(.025f,.15f,.225f,.54f),new Color(.018f,.03f,.045f,.72f));
                var portrait=selected.Id=="04"?ResolveTownMerchantPortrait153("04", false):ResolveTownArt153("MR001_MERCHANT_"+selected.Id+"_FULLBODY_R46");
                TownImage153(root,"Merchant presence154",portrait,new Rect(.028f,.22f,.22f,.465f));
                TownLabel153(root,"Merchant introduction154",new[]{"Supplies for the road ahead.","Good steel earns its place.","Something for the next journey.","Every oath deserves its edge.","Look closely. Wonders linger."}[int.Parse(selected.Id)-1],new Rect(.037f,.16f,.202f,.058f),13,TownGold153,false,TextAnchor.MiddleCenter);
            }
            var offers = selected.Offers;
            if (offers == null || offers.Length == 0)
            {
                TownLabel153(root, "No stock 153", "This merchant has no stock available today.",
                    new Rect(.05f, .4f, .9f, .15f), 18, RuntimeUi.Text);
                return;
            }
            var pages163 = Math.Max(1, (offers.Length + 1) / 2);
            _townStockPage163 = Mathf.Clamp(_townStockPage163, 0, pages163 - 1);
            var chosen = offers.FirstOrDefault(x => x.Id == _townOffer153) ?? offers[0];
            _townOffer153 = chosen.Id;
            if (!compact || !_townCompactOffer153)
            {
                var display = TownPanel153(root, "Merchant stock 153",
                    compact ? new Rect(.035f, .32f, .93f, .52f) : new Rect(.265f, .15f, .42f, .54f),
                    new Color(.02f, .04f, .06f, .75f));
                var stockTitle156D = TownLabel153(display.transform, "Merchant stock title 153", selected.Shop,
                    new Rect(.035f, .83f, .93f, .14f), compact ? 17 : 19, TownGold153, true);
                if (_textScale > 1f)
                {
                    ConfigureResponsiveText062(stockTitle156D,
                        Mathf.RoundToInt((compact ? 17 : 19) * 2.6f), stockTitle156D.fontSize);
                    stockTitle156D.verticalOverflow = VerticalWrapMode.Truncate;
                }
                for (int index = _townStockPage163 * 2; index < offers.Length && index < (_townStockPage163 + 1) * 2; index++)
                {
                    var offer = offers[index];
                    var rect = new Rect(.035f + index % 2 * .49f, .315f, .45f, .475f);
                    var card = TownButton153(display.transform, "Town offer " + offer.Id + " 153", "", rect, () =>
                        {
                            _townOffer153 = offer.Id; _townCompactOffer153 = true; BuildCurrentScreen();
                        }, true, chosen.Id == offer.Id);
                    TownImage153(card.transform, "Offer thumbnail " + offer.Id + " 153", ResolveTownArt153(offer.ArtKey),
                        new Rect(.035f, .37f, .25f, .51f));
                    TownLabel153(card.transform, "Offer name " + offer.Id + " 153", offer.Name,
                        new Rect(.31f, .40f, .65f, .50f), compact ? 14 : 13, RuntimeUi.Text, true);
                    TownLabel153(card.transform, "Offer tier " + offer.Id + " 153", offer.Quality,
                        new Rect(.07f, .25f, .86f, .20f), compact ? 11 : 13, TownGold153);
                    TownLabel153(card.transform, "Offer cost " + offer.Id + " 153", FormatProgressionNumber(offer.Cost) + " XP",
                        new Rect(.07f, .055f, .86f, .20f), compact ? 14 : 15, RuntimeUi.Positive, true);
                }
                TownButton153(display.transform, "Stock previous163", "←", new Rect(.035f,.025f,.22f,.25f),
                    () => { _townStockPage163--; BuildCurrentScreen(); }, _townStockPage163 > 0, false, 20);
                TownLabel153(display.transform, "Stock page163", (_townStockPage163 + 1) + " / " + pages163,
                    new Rect(.30f,.045f,.40f,.15f), 15, RuntimeUi.Text, true, TextAnchor.MiddleCenter);
                TownButton153(display.transform, "Stock next163", "→", new Rect(.745f,.025f,.22f,.25f),
                    () => { _townStockPage163++; BuildCurrentScreen(); }, _townStockPage163 + 1 < pages163, false, 20);
                if (compact)
                    TownButton153(root, "Other merchants 153", "←  OTHER MERCHANTS", new Rect(.035f, .175f, .93f, .13f),
                        () => { _townMerchant153 = ""; BuildCurrentScreen(); }, true, false, 14);
            }
            if (!compact || _townCompactOffer153)
            {
                var detail = TownPanel153(root, "Purchase detail 153",
                    compact ? new Rect(.035f, .32f, .93f, .52f) : new Rect(.70f, .15f, .275f, .54f), TownInk153);
                TownImage153(detail.transform, "Purchase item art 153", ResolveTownArt153(chosen.ArtKey),
                    new Rect(.72f, .74f, .22f, .21f));
                TownLabel153(detail.transform, "Purchase item name 153", chosen.Name,
                    compact?new Rect(.06f,.74f,.65f,.25f):new Rect(.06f, .78f, .65f, .18f), compact ? 16 : 15, RuntimeUi.Text, true);
                TownLabel153(detail.transform, "Purchase item quality 153", chosen.Quality,
                    compact?new Rect(.06f,.635f,.86f,.105f):new Rect(.06f, .705f, .86f, .07f), 13, TownGold153, true);
                var description = chosen.Stats + "\n\n" + chosen.Description;
                if (!chosen.CanBuy && !string.IsNullOrWhiteSpace(chosen.UnavailableReason))
                    description += "\n\n" + chosen.UnavailableReason;
                TownScrollText153(detail.transform, "Purchase effect 153", description,
                    compact?new Rect(.06f,.415f,.88f,.20f):new Rect(.06f, .27f, .88f, .415f), 15);
                TownLabel153(detail.transform, "Purchase exact price 153", "PRICE  " + FormatProgressionNumber(chosen.Cost) + " TREASURY XP",
                    compact?new Rect(.06f,.305f,.88f,.105f):new Rect(.06f, .165f, .88f, .08f), compact ? 14 : 15, RuntimeUi.Positive, true);
                TownButton153(detail.transform, "Review purchase 153", chosen.CanBuy ? "REVIEW PURCHASE" :
                    StringComparer.Ordinal.Equals(chosen.UnavailableReason, "SOLD") ? "SOLD" : "UNAVAILABLE",
                    compact?new Rect(.06f,.025f,.88f,.255f):new Rect(.06f, .035f, .88f, .105f), () => ReviewTownPurchase153(authority, chosen.Id),
                    chosen.CanBuy, false, compact ? 15 : 15);
                if (compact)
                    TownButton153(root, "Back to stock 153", "←  " + selected.Shop.ToUpperInvariant(),
                        new Rect(.035f, .175f, .93f, .13f), () => { _townCompactOffer153 = false; BuildCurrentScreen(); }, true, false, 14);
            }
        }

        private void ReviewTownPurchase153(M1RuntimeCoordinator authority, string id)
        {
            if (!ReferenceEquals(authority, _coordinator)) return;
            var result = authority.QuoteTownPurchase153(id);
            if (!result.IsSuccess) { _localStatus = string.Join("\n", result.Errors); _localStatusPositive = false; BuildCurrentScreen(); return; }
            var quote = result.Value;
            if (!quote.Affordable) { _localStatus = "You need more Treasury XP for this purchase."; _localStatusPositive = false; BuildCurrentScreen(); return; }
            ShowTownConfirmation154("CONFIRM PURCHASE", "Receive: " + quote.Name,
                quote.Cost + " Treasury XP\nTreasury after purchase: " + FormatProgressionNumber(quote.Wallet - quote.Cost) + " XP",
                "CONFIRM PURCHASE", () =>
                {
                    if (ReferenceEquals(authority, _coordinator)) ApplyGuildCity017D(authority.ConfirmTownPurchase153(quote));
                });
        }

        private void ReviewTownRestock159(M1RuntimeCoordinator authority)
        {
            if(!ReferenceEquals(authority,_coordinator))return;
            var result=authority.QuoteTownRestock159();
            if(!result.IsSuccess){_localStatus=string.Join("\n",result.Errors);_localStatusPositive=false;BuildCurrentScreen();return;}
            var quote=result.Value;
            if(!quote.Affordable){_localStatus="You need more Treasury XP to restock.";_localStatusPositive=false;BuildCurrentScreen();return;}
            ShowTownConfirmation154("RESTOCK ALL MERCHANTS","Refresh each merchant's specialist stock. Purchased heroes and items remain yours. Your adventures keep their progress.",
                quote.Cost+" Treasury XP\nTreasury after restock: "+FormatProgressionNumber(quote.Wallet-quote.Cost)+" XP","CONFIRM RESTOCK",()=>
                {if(ReferenceEquals(authority,_coordinator)){_townOffer153="";ApplyGuildCity017D(authority.ConfirmTownRestock159(quote));}});
        }

        private void ReviewTownBuilding153(M1RuntimeCoordinator authority, string id)
        {
            if (!ReferenceEquals(authority, _coordinator)) return;
            var result = authority.QuoteTownBuilding153(id);
            if (!result.IsSuccess) { _localStatus = string.Join("\n", result.Errors); _localStatusPositive = false; BuildCurrentScreen(); return; }
            var quote = result.Value;
            if (!quote.Affordable) { _localStatus = "More resources are needed. " + quote.CostText; _localStatusPositive = false; BuildCurrentScreen(); return; }
            ShowTownConfirmation154(quote.Title, quote.Summary, quote.CostText, "CONFIRM UPGRADE", () =>
                {
                    if (ReferenceEquals(authority, _coordinator)) ApplyGuildCity017D(authority.ConfirmTownBuilding153(quote));
                });
        }

        // Fixed cost and action regions prevent long authored descriptions from
        // pushing the amount or title out of the confirmation. Overflow can scroll.
        private void ShowTownConfirmation154(string title,string summary,string cost,string confirmLabel,Action confirm)
        {
            var previous=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            var blocker=RuntimeUi.AddPanel(_canvas.transform,"Town confirmation blocker154",new Color(0,0,0,.83f));
            Stretch(blocker.rectTransform);
            var safe=RuntimeUi.AddSafeArea(blocker.transform);
            var compact=Screen.width<1000||Screen.height<570;
            var reviewInk164=TownInk153;reviewInk164.a=1f;
            var modal=TownPanel153(safe,"Town confirmation154",compact?new Rect(.04f,.08f,.92f,.84f):new Rect(.17f,.12f,.66f,.76f),reviewInk164);
            TownLabel153(modal.transform,"Review title154",title,new Rect(.06f,.825f,.88f,.13f),22,TownGold153,true);
            TownLabel153(modal.transform,"Review cost heading154","COST",new Rect(.06f,.755f,.88f,.06f),13,TownGold153,true);
            TownScrollText153(modal.transform,"Review full cost154",cost,new Rect(.06f,.43f,.88f,.32f),18);
            TownScrollText153(modal.transform,"Review benefit154",summary,new Rect(.06f,compact?.25f:.19f,.88f,compact?.155f:.215f),15);
            Action dismiss=()=>{blocker.gameObject.SetActive(false);Destroy(blocker.gameObject);};
            Action cancel=()=>{dismiss();if(previous!=null&&previous.activeInHierarchy&&UnityEngine.EventSystems.EventSystem.current!=null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(previous);};
            var yes=TownButton153(modal.transform,"Confirm town154",confirmLabel,new Rect(.06f,.045f,.54f,compact?.16f:.11f),()=>{dismiss();confirm();},true,true,15);
            var no=TownButton153(modal.transform,"Cancel town154","CANCEL",new Rect(.63f,.045f,.31f,compact?.16f:.11f),cancel,true,false,15);
            var nav=yes.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnRight=no;nav.selectOnDown=no;yes.navigation=nav;
            nav=no.navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnLeft=yes;nav.selectOnUp=yes;no.navigation=nav;
            no.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel=cancel;
            no.Select();
        }

        private void OpenTownService153(string tab)
        {
            if (TryRouteLoopService164(tab)) return;
            if (string.IsNullOrWhiteSpace(tab)) return;
            if (tab == "EQUIPMENT") { Navigate(M1Screen.Equipment); return; }
            if (tab == "UNIONS") { Navigate(M1Screen.UnionBuilder); return; }
            _guildCityTab017D = tab;
            _townCompactDetail153 = false;
            _townCompactOffer153 = false;
            BuildCurrentScreen();
        }

        private int TownFont153(int pixels)
        {
            return Mathf.RoundToInt(pixels * 2.6f * Mathf.Max(1f, _textScale));
        }

        private static void TownAnchor153(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = new Vector2(bounds.xMin, bounds.yMin);
            rect.anchorMax = new Vector2(bounds.xMax, bounds.yMax);
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        }

        private static Image TownPanel153(Transform parent, string name, Rect bounds, Color color)
        {
            var panel = RuntimeUi.AddPanel(parent, name, color);
            TownAnchor153(panel.rectTransform, bounds); panel.raycastTarget = false;
            return panel;
        }

        private static Image TownImage153(Transform parent, string name, Sprite sprite, Rect bounds)
        {
            var image = TownPanel153(parent, name, bounds, sprite == null ? Color.clear : Color.white);
            image.sprite = sprite; image.preserveAspect = true;
            return image;
        }

        private Text TownLabel153(Transform parent, string name, string text, Rect bounds, int pixels,
            Color color, bool bold = false, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var label = RuntimeUi.AddText(parent, name + " [Readable 079]", text, TownFont153(pixels), alignment, color,
                bold ? FontStyle.Bold : FontStyle.Normal);
            TownAnchor153(label.rectTransform, bounds); label.raycastTarget = false;
            return label;
        }

        private Button TownButton153(Transform parent, string name, string title, Rect bounds, Action action,
            bool enabled = true, bool selected = false, int pixels = 15)
        {
            var button = RuntimeUi.AddButton(parent, name, title, action, 132f,
                selected ? new Color(.25f, .21f, .13f, .98f) : new Color(.035f, .075f, .11f, .94f));
            TownAnchor153(button.GetComponent<RectTransform>(), bounds);
            if(Screen.width<1000||Screen.height<570||_textScale>=1.25f)
                button.gameObject.AddComponent<MinimumTownTarget164>().Bounds=bounds;
            var label = button.GetComponentInChildren<Text>();
            label.name += " [Readable 079]";
            label.fontSize = TownFont153(pixels); label.color = selected ? TownGold153 : RuntimeUi.Text;
            TownAnchor153(label.rectTransform, new Rect(.035f, .06f, .93f, .88f));
            button.interactable = enabled;
            var outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = selected ? TownGold153 : new Color(.45f, .53f, .59f, .45f);
            outline.effectDistance = new Vector2(1.3f, -1.3f);
            return button;
        }

        private void TownScrollText153(Transform parent, string name, string value, Rect bounds, int pixels)
        {
            var holder = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            holder.transform.SetParent(parent, false);
            TownAnchor153(holder.GetComponent<RectTransform>(), bounds);
            holder.GetComponent<Image>().color = new Color(0, 0, 0, .001f);
            var viewport = new GameObject(name + " viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(holder.transform, false);
            var viewportRect = viewport.GetComponent<RectTransform>(); Stretch(viewportRect);
            var label = RuntimeUi.AddText(viewport.transform, name + " text [Readable 079]", value, TownFont153(pixels), TextAnchor.UpperLeft, RuntimeUi.Text);
            label.raycastTarget = false; label.verticalOverflow = VerticalWrapMode.Overflow;
            label.rectTransform.anchorMin = new Vector2(0, 1); label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.pivot = new Vector2(.5f, 1); label.rectTransform.sizeDelta = Vector2.zero;
            var fit = label.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = holder.GetComponent<ScrollRect>(); scroll.viewport = viewportRect; scroll.content = label.rectTransform;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35f / (_canvas == null ? 1f : Mathf.Max(.1f, _canvas.scaleFactor));
            AddPremiumScrollbar(holder.transform, scroll);
        }

        private static HashSet<string> BuildTownArtKeys153()
        {
            var keys = new HashSet<string>(StringComparer.Ordinal) { "SKYHOME_TOWN154", "WAYGLASS_LUCK_TONIC165" };
            foreach (var facility in new[] { "GUILD_HALL", "MERCHANT_ROW", "FORGE", "TRAINING_GROUNDS", "RECRUITMENT_HALL", "EXPEDITION_GUILD", "INFIRMARY", "ACADEMY_ARCHIVE" })
                for (int level = 0; level <= 10; level++) keys.Add("CITY_" + facility + "_LV" + level.ToString("00"));
            for (int merchant = 1; merchant <= 5; merchant++)
            {
                keys.Add("MR001_MERCHANT_" + merchant.ToString("00") + "_FULLBODY_R46");
                keys.Add("MR001_MERCHANT_" + merchant.ToString("00") + "_PORTRAIT_R46");
            }
            return keys;
        }

        // Packaging verifies these allowlisted, flat PNG names against the supplied manifests.
        // No source-tree fallback in a player: a missing payload must stay visibly diagnosable.
        private static Sprite ResolveTownArt153(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return null;
            if (key == "CONSUMABLE:LUCK165") return ResolveTownArt153("WAYGLASS_LUCK_TONIC165");
            if (key.StartsWith("HERO:", StringComparison.Ordinal))
            {
                var identity = key.Substring(5);
                M1VisualAssets.TryResolvePortrait(identity, identity, string.Empty, identity, out var hero, out _);
                return hero;
            }
            if (key.StartsWith("EQUIPMENT:", StringComparison.Ordinal))
            {
                M1VisualAssets.TryResolveEquipment(key.Substring(10), out var equipment, out _);
                return equipment;
            }
            if (!TownArtKeys153.Contains(key)) return null;
            if (TownArtCache153.TryGetValue(key, out var cached)) return cached;
            Texture2D texture = null; Sprite sprite = null;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "SecondDimension", "Town153", key + ".png");
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                { name = key + " Texture153", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false)) throw new InvalidDataException("PNG decode failed.");
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = key; texture.Apply(false, true);
            }
            catch (Exception exception)
            {
                if (sprite != null) UnityEngine.Object.Destroy(sprite);
                if (texture != null) UnityEngine.Object.Destroy(texture);
                sprite = null; Debug.LogWarning("Town153 art unavailable: " + key + ": " + exception.Message);
            }
            TownArtCache153[key] = sprite;
            return sprite;
        }

        private static Sprite ResolveTownMerchantPortrait153(string merchant, bool icon = true)
        {
            if (merchant != null && merchant.Length == 2) merchant = "MR001_MERCHANT_" + merchant;
            if (merchant != "MR001_MERCHANT_04") return ResolveTownArt153(merchant + "_PORTRAIT_R46");
            var key = icon ? "SELLA_VEY_NATIVE_ICON_164" : "SELLA_VEY_NATIVE_153";
            if (TownArtCache153.TryGetValue(key, out var cached)) return cached;
            const string portrait = "SecondDimension/Art/Portraits/ChapterTwo079/SELLA_VEY_PORTRAIT_079";
            var sprite = Resources.Load<Sprite>(portrait);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(portrait);
                if (texture != null) sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
            }
            if (icon && sprite != null)
            {
                // Approved native identity, closer framing for the small icon.
                // Full desktop portrait and original source pixels stay intact.
                var r = sprite.rect;
                sprite = Sprite.Create(sprite.texture,
                    new Rect(r.x+r.width*.24f,r.y+r.height*.37f,r.width*.52f,r.height*.606f),
                    new Vector2(.5f,.5f),100f);
            }
            TownArtCache153[key] = sprite; return sprite;
        }
    }

    // A window resize changes the column layout once after sizing settles. No save or
    // source art is inspected here; the presenter owns all view reconstruction.
    internal sealed class TownGlow154 : MonoBehaviour
    {
        Text label;
        void Awake(){label=GetComponent<Text>();}
        void Update(){if(label!=null){var c=label.color;c.a=.72f+.20f*Mathf.Sin(Time.unscaledTime*1.6f);label.color=c;}}
    }

    internal sealed class TownViewport153 : MonoBehaviour
    {
        internal Action Changed;
        private int width, height;
        private float changedAt = -1;
        private void Awake() { width = Screen.width; height = Screen.height; }
        private void Update()
        {
            if (Screen.width != width || Screen.height != height)
            { width = Screen.width; height = Screen.height; changedAt = Time.unscaledTime; }
            else if (changedAt >= 0 && Time.unscaledTime - changedAt > .2f)
            { changedAt = -1; Changed?.Invoke(); }
        }
    }
}
