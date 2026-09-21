using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    internal static class BattlePhoneLayout164
    {
        internal static bool IsPhone => Screen.width < 1000 || Screen.height < 570;
        internal static float Scale(Component value) => Mathf.Max(.01f,value.GetComponentInParent<Canvas>()?.scaleFactor??1f);
        internal static void Rect(RectTransform value,float left,float bottom,float right,float height,float scale)
        {
            value.anchorMin=new Vector2(left,0f);value.anchorMax=new Vector2(right,0f);value.pivot=new Vector2(.5f,0f);
            value.offsetMin=new Vector2(0f,bottom/scale);value.offsetMax=new Vector2(0f,(bottom+height)/scale);
        }
        internal static void Font(Text text,float pixels,float scale)
        {
            if(text==null)return;
            text.resizeTextForBestFit=false;text.fontSize=Mathf.CeilToInt(pixels/scale);
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.rectTransform.offsetMin=new Vector2(4f/scale,2f/scale);
            text.rectTransform.offsetMax=new Vector2(-4f/scale,-2f/scale);
        }
        internal static ScrollRect TextColumn(RectTransform parent,string name,IEnumerable<Text> labels,float scale)
        {
            var viewport=RuntimeUi.AddPanel(parent,name,Color.clear);
            viewport.rectTransform.anchorMin=Vector2.zero;viewport.rectTransform.anchorMax=Vector2.one;
            viewport.rectTransform.offsetMin=viewport.rectTransform.offsetMax=Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=RuntimeUi.AddStretchRect(viewport.transform,name+" content");
            content.anchorMin=new Vector2(0f,1f);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1f);
            content.offsetMin=content.offsetMax=Vector2.zero;
            var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding=new RectOffset(Mathf.CeilToInt(3f/scale),Mathf.CeilToInt(3f/scale),Mathf.CeilToInt(3f/scale),Mathf.CeilToInt(3f/scale));
            layout.spacing=2f/scale;layout.childControlHeight=layout.childControlWidth=true;
            layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            foreach(var text in labels)
            {
                if(text==null)continue;
                text.transform.SetParent(content,false);text.raycastTarget=false;
                text.resizeTextForBestFit=false;text.fontSize=Mathf.CeilToInt(11f/scale);
                text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
                var le=text.GetComponent<LayoutElement>()??text.gameObject.AddComponent<LayoutElement>();
                le.minHeight=12f/scale;le.preferredHeight=-1f;le.flexibleHeight=0f;
            }
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=50f;
            return scroll;
        }
    }

    public sealed partial class M2BattleCommandHud072
    {
        private int ForecastPageSize164 => BattlePhoneLayout164.IsPhone ? 2 : PagedForecastCount161;
        private int ForecastVisibleLimit164 => BattlePhoneLayout164.IsPhone ? 2 : MaximumVisibleForecasts;
        private ScrollRect _phoneUnionDetails164,_phonePreviewDetails164;
        private string _phoneLastUnion164,_phoneLastPreview164;

        private bool _phoneHudApplied164;
        private void LateUpdate()
        {
            if(BattlePhoneLayout164.IsPhone)ApplyPhoneLayout164();
            else if(_phoneHudApplied164&&_root!=null)
            {
                var host=_root.parent as RectTransform;var battle=_battle;var focus=FocusedUnionId;
                _phoneHudApplied164=false;_phoneUnionDetails164=null;_phonePreviewDetails164=null;
                Initialize(host,_focusUnion,_previewForecast,_selectForecast,_confirm);
                Refresh(battle,focus);
            }
        }
        private void ApplyPhoneLayout164()
        {
            if(_root==null||!BattlePhoneLayout164.IsPhone)return;
            _phoneHudApplied164=true;
            var scale=BattlePhoneLayout164.Scale(_root);
            BattlePhoneLayout164.Rect(_root,.008f,2f,.992f,150f,scale);
            var union=_root.Find("Focused Union Rail 072") as RectTransform;
            var orders=_root.Find("Complete Forecast Orders 072") as RectTransform;
            var preview=_root.Find("Selected Forecast Preview 072") as RectTransform;
            var navigator=_root.Find("Union Order Navigator 074") as RectTransform;
            if(union==null||orders==null||preview==null||navigator==null)return;
            BattlePhoneLayout164.Rect(union,.006f,3f,.175f,94f,scale);
            BattlePhoneLayout164.Rect(orders,.182f,3f,.642f,94f,scale);
            BattlePhoneLayout164.Rect(preview,.649f,3f,.852f,94f,scale);
            BattlePhoneLayout164.Rect((RectTransform)_confirmButton.transform,.86f,3f,.994f,94f,scale);
            BattlePhoneLayout164.Rect(navigator,.006f,100f,.994f,48f,scale);
            BattlePhoneLayout164.Font(_confirmLabel,12f,scale);
            var navWidth=Mathf.Max(1f,navigator.rect.width*scale);
            var paging=_previousUnionPage078!=null&&_previousUnionPage078.gameObject.activeSelf;
            var edge=paging?48f/navWidth:0f;
            var visible=0;foreach(var chip in _unionChips)if(chip.Button.gameObject.activeSelf)visible++;
            var gap=4f/navWidth;var width=(1f-2f*edge-gap*Mathf.Max(0,visible-1))/Mathf.Max(1,visible);
            for(var i=0;i<visible;i++)
            {
                BattlePhoneLayout164.Rect((RectTransform)_unionChips[i].Button.transform,edge+i*(width+gap),2f,edge+i*(width+gap)+width,44f,scale);
                BattlePhoneLayout164.Font(_unionChips[i].Label,11f,scale);
            }
            if(paging)
            {
                BattlePhoneLayout164.Rect((RectTransform)_previousUnionPage078.transform,0f,2f,44f/navWidth,44f,scale);
                BattlePhoneLayout164.Rect((RectTransform)_nextUnionPage078.transform,1f-44f/navWidth,2f,1f,44f,scale);
                BattlePhoneLayout164.Font(_previousUnionPageLabel078,12f,scale);BattlePhoneLayout164.Font(_nextUnionPageLabel078,12f,scale);
            }
            var orderWidth=Mathf.Max(1f,orders.rect.width*scale);
            var forecastPaging=_previousForecastPage161!=null&&_previousForecastPage161.gameObject.activeSelf;
            var forecastEdge=forecastPaging?48f/orderWidth:0f;
            var count=0;foreach(var slot in _forecastSlots)if(slot.Button.gameObject.activeSelf)count++;
            var fg=4f/orderWidth;var fw=(1f-2f*forecastEdge-fg*Mathf.Max(0,count-1))/Mathf.Max(1,count);
            for(var i=0;i<count;i++)
            {
                BattlePhoneLayout164.Rect((RectTransform)_forecastSlots[i].Button.transform,forecastEdge+i*(fw+fg),2f,forecastEdge+i*(fw+fg)+fw,90f,scale);
                BattlePhoneLayout164.Font(_forecastSlots[i].Label,12f,scale);
            }
            if(forecastPaging)
            {
                BattlePhoneLayout164.Rect((RectTransform)_previousForecastPage161.transform,0f,2f,44f/orderWidth,90f,scale);
                BattlePhoneLayout164.Rect((RectTransform)_nextForecastPage161.transform,1f-44f/orderWidth,2f,1f,90f,scale);
                BattlePhoneLayout164.Font(_previousForecastPage161.GetComponentInChildren<Text>(),12f,scale);
                BattlePhoneLayout164.Font(_nextForecastPage161.GetComponentInChildren<Text>(),12f,scale);
            }
            if(_phoneUnionDetails164==null)
            {
                var labels=new List<Text>{_activeUnionHeading,_activeUnionStats,_activeUnionEyebrow,_activeUnionState};
                for(var i=0;i<_focusedMemberStates078.Count;i++){labels.Add(_focusedMemberStates078[i]);labels.Add(_focusedMemberProgressValues078[i]);}
                _phoneUnionDetails164=BattlePhoneLayout164.TextColumn(union,"Union details scroll164",labels,scale);
                _phonePreviewDetails164=BattlePhoneLayout164.TextColumn(preview,"Forecast details scroll164",new[]{_previewSummary,_previewHeading,_previewActions},scale);
            }
            // Page accessibility runs after construction. Reapply these physical
            // column sizes after it, just as the phone command labels are sized.
            foreach(var column in new[]{_phoneUnionDetails164,_phonePreviewDetails164})
                foreach(var label in column.content.GetComponentsInChildren<Text>(true))
                {label.resizeTextForBestFit=false;label.fontSize=Mathf.CeilToInt(11f/scale);label.lineSpacing=1f;}
            // The same native text remains available by scrolling. Decorative
            // duplicate rails/icons are omitted only from this narrow phone view.
            foreach(var rail in _focusedMemberNextArtRails078)if(rail!=null)rail.gameObject.SetActive(false);
            foreach(var icon in _previewActionIcons)if(icon!=null)icon.gameObject.SetActive(false);
            if(_phoneLastUnion164!=FocusedUnionId){_phoneLastUnion164=FocusedUnionId;_phoneUnionDetails164.verticalNormalizedPosition=1f;}
            if(_phoneLastPreview164!=_previewSummary.text){_phoneLastPreview164=_previewSummary.text;_phonePreviewDetails164.verticalNormalizedPosition=1f;}
        }
    }

    public sealed partial class M2BattleExperienceController072
    {
        private bool CompactBattlePhone164 => BattlePhoneLayout164.IsPhone;
        private Button _phoneHistory164;
        private ScrollRect _phoneObjectiveScroll164;
        private string _phoneStatusSource164;
        private bool _phoneBattleApplied164;
        private readonly Dictionary<Text,Vector4> _phoneButtonInsets164=new Dictionary<Text,Vector4>();
        private void LateUpdate()
        {
            if(CompactBattlePhone164)ApplyPhoneBattleLayout164();
            else if(_phoneBattleApplied164&&_root!=null)RestoreDesktopBattleLayout164();
        }
        private void RestoreDesktopBattleLayout164()
        {
            _phoneBattleApplied164=false;
            var ribbonColor=_storyRibbon.color;ribbonColor.a=.86f;_storyRibbon.color=ribbonColor;
            if(_phoneObjectiveScroll164!=null)
            {
                var fitter=_objectiveText.GetComponent<ContentSizeFitter>();if(fitter!=null){fitter.enabled=false;Destroy(fitter);}
                _objectiveText.transform.SetParent(_storyRibbon.transform,false);
                _phoneObjectiveScroll164.gameObject.SetActive(false);Destroy(_phoneObjectiveScroll164.gameObject);_phoneObjectiveScroll164=null;
            }
            if(!string.IsNullOrEmpty(_phoneStatusSource164))_statusText.text=_phoneStatusSource164;
            SetAnchors(_storyRibbon.rectTransform,new Vector2(.012f,.868f),new Vector2(.988f,.995f));
            SetAnchors(_hudLayer,new Vector2(0f,BattleContentMinimumAnchorY110),new Vector2(1f,BattleContentMaximumAnchorY110));
            SetAnchors(_dioramaLayer,new Vector2(0f,BattleContentMinimumAnchorY110),new Vector2(1f,BattleContentMaximumAnchorY110));
            SetAnchors(_encounterTitleText076.rectTransform,new Vector2(.018f,.49f),new Vector2(.44f,.96f));
            SetAnchors(_phaseText.rectTransform,new Vector2(.018f,.04f),new Vector2(.44f,.49f));
            SetAnchors(_objectiveText.rectTransform,new Vector2(.47f,.45f),new Vector2(.855f,.96f));
            SetAnchors(_statusText.rectTransform,new Vector2(.47f,.04f),new Vector2(.855f,.45f));
            DesktopFont164(_encounterTitleText076,36,54);DesktopFont164(_phaseText,24,34);
            DesktopFont164(_objectiveText,21,30);DesktopFont164(_statusText,MinimumStoryRibbonFontSize074,28);
            if(_playbackSpeedLabel091!=null)
            {
                SetAnchors((RectTransform)_playbackSpeedLabel091.GetComponentInParent<Button>().transform,new Vector2(.875f,.51f),new Vector2(.982f,.99f));
                DesktopFont164(_playbackSpeedLabel091,22,31);
            }
            if(_autoOrdersButton091!=null)
            {
                SetAnchors((RectTransform)_autoOrdersButton091.transform,new Vector2(.875f,.01f),new Vector2(.982f,.49f));
                DesktopFont164(_autoOrdersLabel091,20,28);
            }
            if(_phoneHistory164!=null&&_autoDecisionPanel108!=null)
            {
                _phoneHistory164.transform.SetParent(_autoDecisionPanel108.transform,false);
                SetAnchors((RectTransform)_phoneHistory164.transform,new Vector2(.882f,.12f),new Vector2(.99f,.88f));
                DesktopFont164(_phoneHistory164.GetComponentInChildren<Text>(),28,32);
            }
            if(_autoHistoryPanel110!=null)
            {
                var back=_autoHistoryPanel110.Find("Back To Battle 110") as RectTransform;
                var viewport=_autoHistoryPanel110.Find("Tower Auto History Viewport 110") as RectTransform;
                if(back!=null)SetAnchors(back,new Vector2(.745f,.912f),new Vector2(.98f,.989f));
                if(viewport!=null)SetAnchors(viewport,new Vector2(.022f,.025f),new Vector2(.978f,.90f));
            }
            foreach(var entry in _phoneButtonInsets164)
                if(entry.Key!=null){entry.Key.rectTransform.offsetMin=new Vector2(entry.Value.x,entry.Value.y);entry.Key.rectTransform.offsetMax=new Vector2(entry.Value.z,entry.Value.w);}
            _phoneButtonInsets164.Clear();
            foreach(var entry in _autoHistoryEntries110)if(entry!=null){entry.resizeTextForBestFit=false;entry.fontSize=32;}
            if(_autoHistoryPanel110!=null)
            {
                var image=_autoHistoryPanel110.GetComponent<Image>();if(image!=null){var color=image.color;color.a=.99f;image.color=color;}
                var title=_autoHistoryPanel110.Find("Tower Auto History Title 110")?.GetComponent<Text>();if(title!=null)title.fontSize=32;
            }
            RefreshAutoDecisionFeed108();
        }
        private static void DesktopFont164(Text text,int min,int max)
        {
            if(text==null)return;text.resizeTextForBestFit=true;text.resizeTextMinSize=min;text.resizeTextMaxSize=max;
        }
        private void ApplyPhoneBattleLayout164()
        {
            if(_root==null||_storyRibbon==null||!CompactBattlePhone164)return;
            _phoneBattleApplied164=true;
            var ribbonColor=_storyRibbon.color;ribbonColor.a=1f;_storyRibbon.color=ribbonColor;
            var scale=BattlePhoneLayout164.Scale(_root);
            var height=Mathf.Max(1f,_root.rect.height*scale);
            BattlePhoneLayout164.Rect(_storyRibbon.rectTransform,.012f,height-70f,.988f,68f,scale);
            SetAnchors(_hudLayer,Vector2.zero,Vector2.one);
            // Move the existing arena above the taller command tray. No actor,
            // animation, forecast or native state is replaced by this layout.
            SetAnchors(_dioramaLayer,new Vector2(0f,.30f),new Vector2(1f,.985f));
            var speed=_playbackSpeedLabel091!=null?_playbackSpeedLabel091.GetComponentInParent<Button>():null;
            if(_phoneHistory164==null&&_autoDecisionPanel108!=null)
            {
                var t=_autoDecisionPanel108.transform.Find("Tower Auto History Button 110");
                if(t!=null)_phoneHistory164=t.GetComponent<Button>();
            }
            if(_phoneHistory164!=null&&_phoneHistory164.transform.parent!=_storyRibbon.transform)
                _phoneHistory164.transform.SetParent(_storyRibbon.transform,false);
            var width=Mathf.Max(1f,_storyRibbon.rectTransform.rect.width*scale);
            var controls=214f/width;var start=1f-controls;
            var index=0;foreach(var button in new[]{speed,_autoOrdersButton091,_phoneHistory164})
            {
                if(button==null)continue;
                BattlePhoneLayout164.Rect((RectTransform)button.transform,start+index*72f/width,12f,start+(index*72f+68f)/width,44f,scale);
                var label=button.GetComponentInChildren<Text>();
                if(label!=null&&!_phoneButtonInsets164.ContainsKey(label))
                {var r=label.rectTransform;_phoneButtonInsets164[label]=new Vector4(r.offsetMin.x,r.offsetMin.y,r.offsetMax.x,r.offsetMax.y);}
                BattlePhoneLayout164.Font(label,11f,scale);index++;
            }
            var textRight=Mathf.Max(.25f,start-.012f);
            SetAnchors(_encounterTitleText076.rectTransform,new Vector2(.012f,.51f),new Vector2(textRight*.48f,.98f));
            SetAnchors(_phaseText.rectTransform,new Vector2(.012f,.02f),new Vector2(textRight*.48f,.51f));
            if(_phoneObjectiveScroll164==null)
            {
                var viewport=RuntimeUi.AddPanel(_storyRibbon.transform,"Battle objective scroll164",Color.clear);
                viewport.gameObject.AddComponent<RectMask2D>();
                _objectiveText.transform.SetParent(viewport.transform,false);
                _objectiveText.rectTransform.anchorMin=new Vector2(0f,1f);_objectiveText.rectTransform.anchorMax=Vector2.one;
                _objectiveText.rectTransform.pivot=new Vector2(.5f,1f);_objectiveText.rectTransform.anchoredPosition=Vector2.zero;
                _objectiveText.rectTransform.sizeDelta=Vector2.zero;
                _objectiveText.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
                _phoneObjectiveScroll164=viewport.gameObject.AddComponent<ScrollRect>();
                _phoneObjectiveScroll164.viewport=viewport.rectTransform;_phoneObjectiveScroll164.content=_objectiveText.rectTransform;
                _phoneObjectiveScroll164.horizontal=false;_phoneObjectiveScroll164.vertical=true;
                _phoneObjectiveScroll164.movementType=ScrollRect.MovementType.Clamped;_phoneObjectiveScroll164.scrollSensitivity=45f;
            }
            SetAnchors(_phoneObjectiveScroll164.viewport,new Vector2(textRight*.50f,.29f),new Vector2(textRight,.98f));
            _objectiveText.resizeTextForBestFit=false;_objectiveText.fontSize=Mathf.CeilToInt(11f/scale);
            _objectiveText.horizontalOverflow=HorizontalWrapMode.Wrap;_objectiveText.verticalOverflow=VerticalWrapMode.Overflow;
            SetAnchors(_statusText.rectTransform,new Vector2(textRight*.50f,.02f),new Vector2(textRight,.27f));
            // The ready count is repeated by the native status; the adjacent
            // commands make its extra instruction redundant in this narrow header.
            var divider=_statusText.text.IndexOf('•');
            if(divider>=0&&_statusText.text.Contains("ORDERS READY"))
            {_phoneStatusSource164=_statusText.text;_statusText.text=_statusText.text.Substring(0,divider).Trim();}
            foreach(var text in new[]{_encounterTitleText076,_phaseText,_statusText})
            {text.resizeTextForBestFit=true;text.resizeTextMinSize=Mathf.CeilToInt(10f/scale);text.resizeTextMaxSize=Mathf.CeilToInt(12f/scale);text.fontSize=text.resizeTextMaxSize;}
            if(_autoDecisionPanel108!=null)_autoDecisionPanel108.gameObject.SetActive(false);
            if(_autoHistoryPanel110!=null)
            {
                var back=_autoHistoryPanel110.Find("Back To Battle 110") as RectTransform;
                var viewport=_autoHistoryPanel110.Find("Tower Auto History Viewport 110") as RectTransform;
                var historyHeight=_autoHistoryPanel110.rect.height*scale;
                var image=_autoHistoryPanel110.GetComponent<Image>();if(image!=null){var color=image.color;color.a=1f;image.color=color;}
                var title=_autoHistoryPanel110.Find("Tower Auto History Title 110")?.GetComponent<Text>();
                if(title!=null){title.resizeTextForBestFit=false;title.fontSize=Mathf.CeilToInt(12f/scale);}
                foreach(var entry in _autoHistoryEntries110)if(entry!=null){entry.resizeTextForBestFit=false;entry.fontSize=Mathf.CeilToInt(12f/scale);}
                if(back!=null){BattlePhoneLayout164.Rect(back,.70f,historyHeight-48f,.98f,44f,scale);BattlePhoneLayout164.Font(back.GetComponentInChildren<Text>(),12f,scale);}
                if(viewport!=null)SetAnchors(viewport,new Vector2(.022f,.025f),new Vector2(.978f,Mathf.Max(.1f,1f-52f/historyHeight)));
            }
        }
    }
}
