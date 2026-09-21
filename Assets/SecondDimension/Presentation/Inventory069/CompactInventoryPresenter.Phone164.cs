using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class CompactInventoryPresenter069
    {
        public ScrollRect LoadoutScrollForTests164 { get; private set; }
        private Vector2 _loadoutScrollPosition164 = Vector2.up;
        private System.Collections.IEnumerator RestoreLoadoutScroll164(ScrollRect scroll, Vector2 position)
        { yield return null; yield return null; if(scroll!=null)scroll.normalizedPosition=position; }
        private bool CompactPhone164 => Screen.width < 1000 || Screen.height < 570;
        private float InventoryScale164 => Mathf.Max(.01f, _root.GetComponentInParent<Canvas>()?.scaleFactor ?? 1f);

        private void ApplyPhoneButtonText164()
        {
            if(!CompactPhone164||_root==null)return;
            foreach(var button in _root.GetComponentsInChildren<Button>(true))
            {
                var text=button.transform.Find("Label")?.GetComponent<Text>();
                if(text==null)continue;text.resizeTextForBestFit=false;
                text.fontSize=Mathf.Max(text.fontSize,Mathf.CeilToInt(12f/InventoryScale164));
            }
        }

        private void BuildPhoneComparison164(Transform parent,M1RecruitLoadoutView recruit,M1EquipmentSlotView slot,M1EquipmentChoiceView choice)
        {
            var physical=Mathf.Max(0,recruit?.PhysicalAttack??0);var mystic=Mathf.Max(0,recruit?.MysticAttack??0);
            var afterPhysical=choice==null?physical:Mathf.Max(0,physical-(slot?.EquippedPhysicalAttackBonus??0)+choice.PhysicalAttackBonus);
            var afterMystic=choice==null?mystic:Mathf.Max(0,mystic-(slot?.EquippedMysticAttackBonus??0)+choice.MysticAttackBonus);
            var status=string.IsNullOrWhiteSpace(_statusMessage)?EquipBlockedReason069(slot,choice):_statusMessage;
            var copy=(choice==null?"PICK GEAR":PlayerFacingItemName069(choice))+"\n"+
                "PHYSICAL  "+physical+" → "+afterPhysical+"  ("+SignedCompactDelta069(afterPhysical-physical)+")\n"+
                "MYSTIC  "+mystic+" → "+afterMystic+"  ("+SignedCompactDelta069(afterMystic-mystic)+")\n"+status;
            var panel=RuntimeUi.AddPanel(parent,"Stat Comparison 069",RuntimeUi.PanelRaised);
            var height=62f/InventoryScale164;RuntimeUi.SetLayout(panel,preferredHeight:height).minHeight=height;
            var viewport=RuntimeUi.AddPanel(panel.transform,"Phone comparison viewport164",Color.clear);
            Stretch069(viewport.rectTransform,8f);viewport.gameObject.AddComponent<RectMask2D>();
            var text=RuntimeUi.AddText(viewport.transform,"Phone comparison native values164",copy,Mathf.CeilToInt(12f/InventoryScale164),TextAnchor.UpperLeft,RuntimeUi.Text);
            var content=text.rectTransform;content.anchorMin=new Vector2(0f,1f);content.anchorMax=Vector2.one;
            content.pivot=new Vector2(.5f,1f);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport.rectTransform;scroll.content=content;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=45f;
        }

        private Transform CreateLoadoutScroll164(RectTransform panel)
        {
            var view = new GameObject("Loadout Scroll View164", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
            view.transform.SetParent(panel,false);
            var viewport=(RectTransform)view.transform; Stretch069(viewport,0f);
            view.GetComponent<Image>().color=Color.clear;
            var contentObject=new GameObject("Loadout Scroll Content164",typeof(RectTransform),typeof(ContentSizeFitter));
            contentObject.transform.SetParent(viewport,false);
            var content=(RectTransform)contentObject.transform;
            content.anchorMin=new Vector2(0f,1f);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1f);
            content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;
            contentObject.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=view.GetComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;
            LoadoutScrollForTests164=scroll;
            scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=55f;
            return content;
        }
    }

    // GridLayoutGroup ignores child LayoutElements: size the actual slot cells,
    // then reserve all rows in the scrollable loadout rather than clip controls.
    internal sealed class InventoryGridTouch164 : MonoBehaviour
    {
        private GridLayoutGroup _grid;
        private void LateUpdate()
        {
            if(_grid==null)_grid=GetComponent<GridLayoutGroup>();
            var rect=transform as RectTransform;var canvas=GetComponentInParent<Canvas>();
            if(_grid==null||rect==null||canvas==null)return;
            var columns=Mathf.Max(1,_grid.constraintCount);
            var target=Screen.width<1000||Screen.height<570?64f:44f;
            var height=Mathf.Max(RuntimeUi.MinimumTouchPixels,target/Mathf.Max(.01f,canvas.scaleFactor));
            var width=Mathf.Max(1f,(rect.rect.width-_grid.padding.horizontal-_grid.spacing.x*(columns-1))/columns);
            var cell=new Vector2(width,height);
            if(_grid.cellSize!=cell)_grid.cellSize=cell;
            var rows=Mathf.Max(1,Mathf.CeilToInt((float)transform.childCount/columns));
            var total=rows*height+Mathf.Max(0,rows-1)*_grid.spacing.y+_grid.padding.vertical;
            var layout=GetComponent<LayoutElement>();
            if(layout!=null){layout.minHeight=total;layout.preferredHeight=total;layout.flexibleHeight=0f;}
        }
    }
}
