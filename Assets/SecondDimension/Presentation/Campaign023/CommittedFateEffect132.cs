using System;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    // The supplied outcome is already saved. Animation uses time only and
    // never consumes gameplay RNG or changes the result.
    public sealed class CommittedFateEffect132 : MonoBehaviour
    {
        FatePolyhedronGraphic132 _graphic;
        Text _number;
        RectTransform _stage;
        Text[] _facets;
        FateSoftShadow132 _diceShadow;
        Quaternion _landing;
        FateImpactRing132 _impact;
        AudioSource _audio;
        AudioClip _bounceClip;
        AudioClip _impactClip;
        int _lastBounce = -1;
        int _lastWheelStep = int.MinValue;
        RectTransform _pointer;
        float _settledElapsed;
        Action _settled;
        int _result;
        bool _wheel;
        bool _closed;
        bool _reduced;
        float _elapsed;
        bool _skipFirstDelta132 = true;
        public bool Settled132 { get; private set; }

        public static CommittedFateEffect132 Play132(RectTransform parent,
            bool wheel, int savedResult, Color color, bool reducedMotion, Action settled)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (wheel ? savedResult < 0 || savedResult > 2 : savedResult < 1 || savedResult > 20)
                throw new ArgumentOutOfRangeException(nameof(savedResult));
            var obj = new GameObject(wheel ? "Committed Fortune Wheel 132" : "Committed Tumbling D20 132",
                typeof(RectTransform), typeof(CommittedFateEffect132));
            var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            CommittedGlassShatter132.Fit132(rect, Vector2.zero, Vector2.one);
            var effect = obj.GetComponent<CommittedFateEffect132>();
            effect._wheel = wheel; effect._result = savedResult; effect._settled = settled;
            effect._reduced = reducedMotion;
            effect._stage = RuntimeUi.AddStretchRect(rect, "Fate Motion Stage 132");
            var graphic = effect._stage.gameObject.AddComponent<FatePolyhedronGraphic132>();
            effect._graphic = graphic; graphic.color = color; graphic.raycastTarget = false; graphic.Wheel132 = wheel;
            if(!wheel)
            {
                var shadow=RuntimeUi.AddStretchRect(rect,"Soft Elliptical Dice Shadow 132");
                effect._diceShadow=shadow.gameObject.AddComponent<FateSoftShadow132>();
                CommittedGlassShatter132.Fit132(shadow,new Vector2(.16f,.14f),new Vector2(.84f,.24f));
                effect._diceShadow.color=new Color(0,0,0,.65f);effect._diceShadow.raycastTarget=false;
                shadow.SetAsFirstSibling();
                effect._landing=FatePolyhedronGraphic132.ResultFacingRotation132(savedResult);
            }
            var ring = RuntimeUi.AddStretchRect(rect,"Fate Impact Ring 132");
            effect._impact=ring.gameObject.AddComponent<FateImpactRing132>();
            effect._impact.color=color; effect._impact.raycastTarget=false; effect._impact.Progress132=1f;
            if (!wheel)
            {
                effect._facets=new Text[20];
                for (var index=0;index<20;index++)
                {
                    var label=RuntimeUi.AddText(effect._stage,"D20 Face "+(index+1),(index+1).ToString(),46,
                        TextAnchor.MiddleCenter,RuntimeUi.Text,FontStyle.Bold);
                    var labelRect=label.rectTransform;
                    labelRect.anchorMin=labelRect.anchorMax=new Vector2(.5f,.5f);
                    labelRect.sizeDelta=new Vector2(90f,70f); label.raycastTarget=false;
                    var outline=label.gameObject.AddComponent<Outline>();
                    outline.effectColor=new Color(.025f,.035f,.055f,.9f);outline.effectDistance=new Vector2(1.4f,-1.4f);
                    effect._facets[index]=label;
                }
            }
            effect._audio=obj.AddComponent<AudioSource>(); effect._audio.playOnAwake=false;
            effect._audio.spatialBlend=0f; effect._audio.volume=.23f;
            // Existing authored local impacts; missing audio changes no timing.
            effect._bounceClip=Resources.Load<AudioClip>("SecondDimension/Audio/Battle011/Weapons/SFX_STAFF_IMPACT");
            effect._impactClip=Resources.Load<AudioClip>("SecondDimension/Audio/Battle011/Mystics/SFX_WARDING_IMPACT");
            effect._number = RuntimeUi.AddText(rect, "Saved Fate Face 132", "", wheel ? 34 : 44,
                TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            CommittedGlassShatter132.Fit132(effect._number.rectTransform,
                wheel?new Vector2(.1f,.34f):new Vector2(.1f,.025f),
                wheel?new Vector2(.9f,.66f):new Vector2(.9f,.135f));
            effect._number.raycastTarget = false;
            if (wheel)
            {
                FateWheelLabels132.Attach132(graphic);
                var pointer = RuntimeUi.AddText(rect, "Fortune Wheel Pointer 132", "▼", 62,
                    TextAnchor.MiddleCenter, RuntimeUi.Accent, FontStyle.Bold);
                CommittedGlassShatter132.Fit132(pointer.rectTransform,
                    new Vector2(.42f,.82f), new Vector2(.58f,1f));
                pointer.raycastTarget = false;
                effect._pointer=pointer.rectTransform;
            }
            effect.Render132(0f);
            if (reducedMotion) effect.Settle132();
            return effect;
        }

        void Update()
        {
            if (_closed) return;
            if (Settled132)
            {
                _settledElapsed+=Time.unscaledDeltaTime;
                _impact.Progress132=_reduced?1f:Mathf.Clamp01(_settledElapsed/.52f);
                _impact.SetVerticesDirty(); return;
            }
            // Match QuestDice132: a save/resource/PNG stall must not consume
            // the toss before it can render. Keep its first pose for a frame,
            // then advance at most one 30 Hz presentation step per actual frame.
            if (_skipFirstDelta132) { _skipFirstDelta132 = false; return; }
            _elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            var t = Mathf.Clamp01(_elapsed / (_wheel ? 2.8f : 2.1f));
            Render132(t);
            if (t >= 1f) Settle132();
        }

        void Render132(float t)
        {
            var eased = 1f - Mathf.Pow(1f - t, 3f);
            if (_wheel)
            {
                // Twelve visual wedges repeat the three saved reward kinds.
                // Wedge0/1/2 corresponds exactly to saved kind0/1/2; the
                // repetition changes neither odds nor the committed result.
                _graphic.Angle132 = Mathf.Lerp(-1440f, -_result * 30f, eased);
                _graphic.LightPhase132=t*18f;
                var step=Mathf.FloorToInt(_graphic.Angle132/30f);
                if(t>0f && step!=_lastWheelStep)
                {
                    _lastWheelStep=step;
                    if(_bounceClip!=null){_audio.pitch=1.8f;_audio.PlayOneShot(_bounceClip,.12f);}
                }
                if(_pointer!=null)_pointer.localRotation=Quaternion.Euler(0,0,
                    Mathf.Sin((_graphic.Angle132%30f)/30f*Mathf.PI)*11f*(1f-t));
            }
            else
            {
                // A perspective toss crosses the table, hits, then makes three
                // progressively shorter hops. Time alone drives the motion.
                var size=((RectTransform)transform).rect.size;
                var hop=t<.42f?0:t<.68f?1:t<.86f?2:3;
                var from=hop==0?0f:hop==1 ? .42f:hop==2 ? .68f:.86f;
                var to=hop==0 ? .42f:hop==1 ? .68f:hop==2 ? .86f:1f;
                var height=hop==0 ? .26f:hop==1 ? .115f:hop==2 ? .046f:.014f;
                var bounce=Mathf.Sin(Mathf.Clamp01((t-from)/(to-from))*Mathf.PI)*height;
                var slide=-.37f*Mathf.Pow(1f-Mathf.Clamp01(t/.64f),2f)+
                    Mathf.Sin(t*Mathf.PI*5f)*Mathf.Pow(1f-t,2f)*.052f;
                var tumble=Mathf.Pow(1f-t,1.8f);
                _graphic.Rotation132=Quaternion.Euler(tumble*1080f,tumble*850f,tumble*440f)*_landing;
                _stage.anchoredPosition=new Vector2(slide*size.x,(.055f+bounce)*size.y);
                var forwardScale=Mathf.Lerp(.62f,1f,Mathf.Clamp01(t/.50f));
                var squash=Mathf.Sin(Mathf.Clamp01((t-from)/.036f)*Mathf.PI)*.055f*(hop==0?0f:1f/(hop+1));
                _stage.localScale=new Vector3(forwardScale*(1f+squash),forwardScale*(1f-squash),1f);
                _diceShadow.rectTransform.anchoredPosition=new Vector2(slide*size.x,0);
                _diceShadow.rectTransform.localScale=Vector3.one*(forwardScale*(1f+bounce*.7f));
                _diceShadow.color=new Color(0,0,0,.65f-bounce*1.35f);
                if(t>0 && hop!=_lastBounce)
                {
                    _lastBounce=hop;
                    if(hop>0 && _bounceClip!=null){_audio.pitch=.8f+hop*.13f;_audio.PlayOneShot(_bounceClip,.64f/(1f+hop*.3f));}
                }
                for(var index=0;index<_facets.Length;index++)
                {
                    var projected=_graphic.FaceCenter132(index);
                    var facing=_graphic.FaceFacing132(index);
                    _facets[index].enabled=facing>.30f;
                    _facets[index].rectTransform.anchoredPosition=new Vector2(projected.x,projected.y);
                    _facets[index].rectTransform.localScale=Vector3.one*(.56f+.44f*Mathf.Clamp01(facing));
                    _facets[index].color=new Color(1f,.97f,.84f,Mathf.Clamp01((facing-.18f)*1.7f));
                }
            }
            _graphic.SetVerticesDirty();
        }

        public void Settle132()
        {
            if (_closed || Settled132) return;
            Render132(1f); Settled132 = true;
            if(_wheel){_graphic.HighlightWedge132=_result;_graphic.SetVerticesDirty();}
            _impact.Progress132=_reduced?1f:0f; _impact.SetVerticesDirty();
            if (_impactClip!=null) { _audio.pitch=1f; _audio.PlayOneShot(_impactClip); }
            _number.text = _wheel ? "WIN!" : _result.ToString();
            var callback = _settled; _settled = null; callback?.Invoke();
        }
        public void Cancel132() { _closed = true; _settled = null; }
        void OnDisable() => Cancel132();
        void OnDestroy() => Cancel132();
    }

    // Twenty real triangular faces of an icosahedron, depth sorted and shaded
    // as the mesh tumbles. A separate fixed result label is revealed on settle.
    public sealed class FatePolyhedronGraphic132 : MaskableGraphic
    {
        public bool Wheel132;
        public float Angle132;
        public float LightPhase132;
        public int HighlightWedge132=-1;
        public Quaternion Rotation132 = Quaternion.identity;
        static readonly float Phi132 = (1f + Mathf.Sqrt(5f)) * .5f;
        static readonly Vector3[] Vertices132 = {
            new Vector3(-1,Phi132,0),new Vector3(1,Phi132,0),new Vector3(-1,-Phi132,0),new Vector3(1,-Phi132,0),
            new Vector3(0,-1,Phi132),new Vector3(0,1,Phi132),new Vector3(0,-1,-Phi132),new Vector3(0,1,-Phi132),
            new Vector3(Phi132,0,-1),new Vector3(Phi132,0,1),new Vector3(-Phi132,0,-1),new Vector3(-Phi132,0,1) };
        static readonly int[] Faces132 = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,
            10,7,6,7,1,8,3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
        readonly Vector3[] _rotated = new Vector3[12];
        readonly int[] _order = new int[20];
        readonly float[] _depth = new float[20];
        public Vector3 FaceCenter132(int face)
        {
            var center=Vector3.zero;
            for(var corner=0;corner<3;corner++) center+=Rotation132*Vertices132[Faces132[face*3+corner]].normalized;
            center/=3f;
            var radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.34f;
            var projected=Project132(center,radius);
            return new Vector3(projected.x,projected.y,center.z);
        }
        public float FaceFacing132(int face)
        {
            var center=Vector3.zero;
            for(var corner=0;corner<3;corner++)center+=Vertices132[Faces132[face*3+corner]].normalized;
            return (Rotation132*center.normalized).z;
        }
        public static Quaternion ResultFacingRotation132(int savedResult)
        {
            if(savedResult<1 || savedResult>20)throw new ArgumentOutOfRangeException(nameof(savedResult));
            var index=(savedResult-1)*3;
            var center=(Vertices132[Faces132[index]]+Vertices132[Faces132[index+1]]+Vertices132[Faces132[index+2]]).normalized;
            var facing=Quaternion.FromToRotation(center,Vector3.forward);
            var tip=facing*Vertices132[Faces132[index]].normalized;
            return Quaternion.AngleAxis(90f-Mathf.Atan2(tip.y,tip.x)*Mathf.Rad2Deg,Vector3.forward)*facing;
        }
        static Vector2 Project132(Vector3 point,float radius) =>
            new Vector2(point.x,point.y)*(radius/(1f-point.z*.16f));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            var center = r.center; var radius = Mathf.Min(r.width,r.height)*(Wheel132 ? .43f : .34f);
            if (Wheel132) { DrawWheel132(vh,center,radius); return; }
            for (var i=0;i<12;i++) _rotated[i] = Rotation132 * Vertices132[i].normalized;
            for (var f=0;f<20;f++) { _order[f]=f; _depth[f]=(_rotated[Faces132[f*3]].z+_rotated[Faces132[f*3+1]].z+_rotated[Faces132[f*3+2]].z)/3f; }
            Array.Sort(_depth,_order);
            for (var index=0;index<20;index++)
            {
                var face = _order[index];
                var normal=(_rotated[Faces132[face*3]]+_rotated[Faces132[face*3+1]]+_rotated[Faces132[face*3+2]]).normalized;
                var light=new Vector3(-.45f,.65f,1f).normalized;
                var diffuse=Mathf.Max(0f,Vector3.Dot(normal,light));
                var specular=Mathf.Pow(Mathf.Max(0f,Vector3.Dot(Vector3.Reflect(-light,normal),Vector3.forward)),18f);
                var shade=.22f+diffuse*.74f;
                var c=Color.Lerp(new Color(color.r*shade,color.g*shade,color.b*shade,color.a),new Color(1f,.98f,.83f,color.a),specular*.48f);
                var edge=Color.Lerp(c,new Color(.88f,.96f,1f,color.a),.45f+diffuse*.3f);
                var start = vh.currentVertCount;
                for (var corner=0;corner<3;corner++)
                {
                    var v = _rotated[Faces132[face*3+corner]];
                    vh.AddVert(center+Project132(v,radius),edge,Vector2.zero);
                }
                vh.AddTriangle(start,start+1,start+2);
                // Inset triangles expose a thin bright edge between facets.
                var a=_rotated[Faces132[face*3]]; var b=_rotated[Faces132[face*3+1]]; var d=_rotated[Faces132[face*3+2]];
                var centroid=(a+b+d)/3f; start=vh.currentVertCount;
                foreach(var v in new[] {a,b,d})
                {
                    var inset=Vector3.Lerp(v,centroid,.045f);
                    vh.AddVert(center+Project132(inset,radius),c,Vector2.zero);
                }
                vh.AddTriangle(start,start+1,start+2);
            }
        }
        void DrawWheel132(VertexHelper vh,Vector2 center,float radius)
        {
            Disk132(vh,center,radius*1.10f,new Color(.09f,.055f,.025f));
            Disk132(vh,center,radius*1.055f,new Color(.90f,.69f,.24f));
            Disk132(vh,center,radius*.955f,new Color(.13f,.08f,.045f));
            var colors = new[] {new Color(.10f,.62f,.96f),new Color(.70f,.20f,.66f),new Color(.95f,.43f,.13f),
                new Color(.04f,.73f,.61f),new Color(.54f,.30f,.92f),new Color(.97f,.69f,.10f)};
            for(var sector=0;sector<12;sector++)
            for(var segment=0;segment<10;segment++)
            {
                var a=(75f+sector*30f+segment*3f+Angle132+(segment==0?.35f:0f))*Mathf.Deg2Rad;
                var b=(75f+sector*30f+(segment+1)*3f+Angle132-(segment==9?.35f:0f))*Mathf.Deg2Rad;
                var start=vh.currentVertCount;
                var c=colors[sector%colors.Length];
                if(sector==HighlightWedge132)c=Color.Lerp(c,Color.white,.40f);
                vh.AddVert(center,c,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*.93f,c,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius*.93f,c,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);
            }
            for(var lamp=0;lamp<36;lamp++)
            {
                var angle=lamp*Mathf.PI/18f;
                var lit=(lamp+Mathf.FloorToInt(LightPhase132))%3==0;
                Disk132(vh,center+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius*1.008f,
                    radius*.022f,lit?new Color(1f,.98f,.72f):new Color(.68f,.38f,.10f),10);
            }
            Disk132(vh,center,radius*.23f,new Color(.99f,.83f,.43f));
            Disk132(vh,center,radius*.205f,new Color(.09f,.045f,.02f));
        }
        static void Disk132(VertexHelper vh,Vector2 center,float radius,Color tint,int segments=64)
        {
            for(var i=0;i<segments;i++)
            {
                var a=i*Mathf.PI*2f/segments;var b=(i+1)*Mathf.PI*2f/segments;var start=vh.currentVertCount;
                vh.AddVert(center,tint,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,tint,Vector2.zero);
                vh.AddVert(center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,tint,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);
            }
        }
    }

    public sealed class FateSoftShadow132 : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var rect=rectTransform.rect;var radii=rect.size*.5f;
            for(var ring=0;ring<10;ring++)
            for(var segment=0;segment<64;segment++)
            {
                var inner=ring/10f;var outer=(ring+1)/10f;
                var a=segment*Mathf.PI/32f;var b=(segment+1)*Mathf.PI/32f;
                var u=new Vector2(Mathf.Cos(a)*radii.x,Mathf.Sin(a)*radii.y);
                var v=new Vector2(Mathf.Cos(b)*radii.x,Mathf.Sin(b)*radii.y);
                var c=color;c.a*=Mathf.Pow(1f-inner,1.8f);var d=color;d.a*=Mathf.Pow(1f-outer,1.8f);
                var start=vh.currentVertCount;
                vh.AddVert(rect.center+u*inner,c,Vector2.zero);vh.AddVert(rect.center+v*inner,c,Vector2.zero);
                vh.AddVert(rect.center+v*outer,d,Vector2.zero);vh.AddVert(rect.center+u*outer,d,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }

    public sealed class FateWheelLabels132 : MonoBehaviour
    {
        FatePolyhedronGraphic132 _wheel;
        Text[] _labels;
        Vector2 _lastSize=new Vector2(-1,-1);
        float _lastAngle=float.NaN;
        public static void Attach132(FatePolyhedronGraphic132 wheel)
        {
            var labels=wheel.gameObject.AddComponent<FateWheelLabels132>();labels._wheel=wheel;
            labels._labels=new Text[12];
            for(var i=0;i<12;i++)
            {
                var text=RuntimeUi.AddText(wheel.transform,"Wheel Prize Type "+i,new[]{"XP","CACHE","GEAR"}[i%3],26,
                    TextAnchor.MiddleCenter,Color.white,FontStyle.Bold);
                text.raycastTarget=false;var rect=text.rectTransform;
                rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(116,42);
                var shadow=text.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(1,-1);
                labels._labels[i]=text;
            }
            labels.Refresh132();
        }
        void LateUpdate()=>Refresh132();
        void Refresh132()
        {
            if(_wheel==null || _labels==null)return;
            var size=_wheel.rectTransform.rect.size;
            if(size==_lastSize && Mathf.Approximately(_wheel.Angle132,_lastAngle))return;
            _lastSize=size;_lastAngle=_wheel.Angle132;
            var radius=Mathf.Min(size.x,size.y)*.43f*.64f;
            for(var i=0;i<12;i++)
            {
                var degrees=90f+i*30f+_wheel.Angle132;var radians=degrees*Mathf.Deg2Rad;
                _labels[i].rectTransform.anchoredPosition=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians))*radius;
                _labels[i].rectTransform.localRotation=Quaternion.Euler(0,0,degrees-90f);
            }
        }
    }

    public sealed class FateImpactRing132 : MaskableGraphic
    {
        public float Progress132=1f;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if(Progress132>=1f)return;
            var r=rectTransform.rect;var radius=Mathf.Min(r.width,r.height)*(.26f+Progress132*.25f);
            var thickness=8f*(1f-Progress132);var c=color;c.a*=1f-Progress132;
            for(var i=0;i<64;i++)
            {
                var a=i*Mathf.PI/32f;var b=(i+1)*Mathf.PI/32f;var start=vh.currentVertCount;
                var u=new Vector2(Mathf.Cos(a),Mathf.Sin(a));var v=new Vector2(Mathf.Cos(b),Mathf.Sin(b));
                vh.AddVert(r.center+u*radius,c,Vector2.zero);vh.AddVert(r.center+v*radius,c,Vector2.zero);
                vh.AddVert(r.center+v*(radius+thickness),c,Vector2.zero);vh.AddVert(r.center+u*(radius+thickness),c,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
