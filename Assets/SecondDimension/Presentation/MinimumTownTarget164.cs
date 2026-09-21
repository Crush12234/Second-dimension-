using UnityEngine;

namespace SecondDimension.Presentation
{
    // Anchored Town controls have no LayoutGroup to honor LayoutElement.minHeight.
    // Keep the actual drawn/raycast rectangle at least44 physical pixels high.
    internal sealed class MinimumTownTarget164 : MonoBehaviour
    {
        public Rect Bounds;
        private readonly Vector3[] _corners=new Vector3[4];
        private void LateUpdate()
        {
            var rect=transform as RectTransform;var parent=rect!=null?rect.parent as RectTransform:null;
            if(parent==null)return;
            parent.GetWorldCorners(_corners);
            var height=Mathf.Abs(_corners[2].y-_corners[0].y);
            if(height<=1f)return;
            var h=Mathf.Max(Bounds.height,44f/height);
            var min=new Vector2(Bounds.x,Bounds.y);var max=new Vector2(Bounds.xMax,Bounds.y+h);
            if(rect.anchorMin!=min||rect.anchorMax!=max)
            {rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        }
    }
}
