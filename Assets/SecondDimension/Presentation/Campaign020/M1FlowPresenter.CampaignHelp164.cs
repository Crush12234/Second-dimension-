using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private Transform BuildCampaignHelpScroll164(RectTransform body)
        {
            var scale=Mathf.Max(.01f,_canvas.scaleFactor);
            var view=RuntimeUi.AddPanel(body,"Campaign help viewport164",new Color(.015f,.025f,.04f,1f));
            Stretch(view.rectTransform);view.rectTransform.offsetMax=new Vector2(0f,-48f/scale);
            view.gameObject.AddComponent<RectMask2D>();
            var content=RuntimeUi.AddStretchRect(view.transform,"Campaign help content164");
            content.anchorMin=new Vector2(0f,1f);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1f);
            content.offsetMin=content.offsetMax=Vector2.zero;
            RuntimeUi.AddVerticalLayout(content,new RectOffset(12,12,12,12),12f,TextAnchor.UpperLeft);
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=view.gameObject.AddComponent<ScrollRect>();scroll.viewport=view.rectTransform;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=55f;
            _activeScroll=scroll;
            content.gameObject.AddComponent<CampaignHelpReadability164>();
            return content;
        }

        private void FinishCampaignHelpScroll164(RectTransform body,Transform content)
        {
            var close=content.Find("Toggle Expedition Deck details 089") as RectTransform;
            if(close==null)return;
            close.SetParent(body,false);close.anchorMin=new Vector2(0f,1f);close.anchorMax=Vector2.one;close.pivot=new Vector2(.5f,1f);
            close.anchoredPosition=Vector2.zero;close.sizeDelta=new Vector2(0f,44f/Mathf.Max(.01f,_canvas.scaleFactor));
            close.gameObject.AddComponent<CampaignHelpReadability164>();
        }
    }

    internal sealed class CampaignHelpReadability164 : MonoBehaviour
    {
        private void LateUpdate()
        {
            var scale=Mathf.Max(.01f,GetComponentInParent<Canvas>()?.scaleFactor??1f);
            foreach(var text in GetComponentsInChildren<Text>())
            {
                var button=text.GetComponentInParent<Button>();
                var title=text.name.StartsWith("Title",System.StringComparison.Ordinal);
                text.resizeTextForBestFit=false;text.fontSize=Mathf.CeilToInt((title?14f:12f)/scale);
                if(title)text.color=RuntimeUi.Accent;
                text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
                if(button!=null)continue;
                var element=text.GetComponent<LayoutElement>()??text.gameObject.AddComponent<LayoutElement>();
                element.minHeight=0f;element.flexibleHeight=0f;element.preferredHeight=text.preferredHeight+4f/scale;
            }
        }
    }
}
