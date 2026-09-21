using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    // Authored reference-space minima can shrink below a finger-sized target on
    // landscape phones. Layout groups may grow/scroll instead of shrinking them.
    [DisallowMultipleComponent]
    internal sealed class AdaptiveTouchTarget164 : MonoBehaviour
    {
        LayoutElement _layout;
        Canvas _canvas;
        float _authoredHeight, _authoredWidth, _appliedHeight, _appliedWidth;
        bool _ready;

        void LateUpdate()
        {
            if(_layout==null)_layout=GetComponent<LayoutElement>();
            if(_canvas==null)_canvas=GetComponentInParent<Canvas>();
            if(_layout==null||_canvas==null||_canvas.renderMode==RenderMode.WorldSpace)return;
            if(!_ready)
            {
                _authoredHeight=_layout.minHeight;_authoredWidth=_layout.minWidth;
                _appliedHeight=_authoredHeight;_appliedWidth=_authoredWidth;_ready=true;
            }
            // Honor a caller that changes its authored layout after construction.
            if(!Mathf.Approximately(_layout.minHeight,_appliedHeight))_authoredHeight=_layout.minHeight;
            if(!Mathf.Approximately(_layout.minWidth,_appliedWidth))_authoredWidth=_layout.minWidth;
            var minimum=44f/Mathf.Max(.01f,_canvas.scaleFactor);
            _appliedHeight=Mathf.Max(_authoredHeight,minimum);
            _appliedWidth=Mathf.Max(_authoredWidth,minimum);
            _layout.minHeight=_appliedHeight;_layout.minWidth=_appliedWidth;
        }
    }
}
