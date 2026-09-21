using System;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    // A presentation-only interruption. The supplied receipt must already be
    // committed; only its owner may apply it from the completion callback.
    public sealed class CommittedGlassShatter132 : MonoBehaviour
    {
        Action _completed;
        RectTransform[] _shards;
        Vector2[] _origins;
        Vector2[] _directions;
        Vector2[] _battleRadials132;
        float[] _battleSpins132;
        CanvasGroup _glass;
        CanvasGroup _copy;
        Button _continue132;
        string _readyLabel132;
        bool _ready132, _battleIntro132, _introPresented132;
        public bool IsPending132 => !_closed;
        float _elapsed;
        bool _closed;
        bool _reduced;
        public string ReceiptId132 { get; private set; }

        public static CommittedGlassShatter132 Play132(RectTransform parent,
            string committedReceiptId, string title, string description,
            bool boss, bool reducedMotion, Action completed)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (string.IsNullOrWhiteSpace(committedReceiptId))
                throw new ArgumentException("A committed receipt is required.", nameof(committedReceiptId));
            var panel = RuntimeUi.AddPanel(parent, "Committed Story Glass 132",
                new Color(0.008f, 0.016f, 0.03f, 0.985f));
            var root = panel.rectTransform;
            Fit132(root, Vector2.zero, Vector2.one);
            var layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            root.SetAsLastSibling();
            var effect = panel.gameObject.AddComponent<CommittedGlassShatter132>();
            effect.ReceiptId132 = committedReceiptId;
            effect._completed = completed;
            effect._reduced = reducedMotion;
            var glass = RuntimeUi.AddStretchRect(root, "Shattered Wayglass 132");
            effect._glass = glass.gameObject.AddComponent<CanvasGroup>();
            effect._glass.blocksRaycasts = false;
            effect._shards = new RectTransform[18];
            effect._origins = new Vector2[18];
            effect._directions = new Vector2[18];
            for (var i = 0; i < effect._shards.Length; i++)
            {
                var obj = new GameObject("Glass Facet " + i, typeof(RectTransform), typeof(GlassShardGraphic132));
                var rect = obj.GetComponent<RectTransform>();
                rect.SetParent(glass, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(210f + i % 3 * 80f, 170f + i % 4 * 42f);
                var direction = new Vector2(Mathf.Cos(i * 2.399963f), Mathf.Sin(i * 2.399963f));
                effect._origins[i] = direction * (80f + i % 4 * 80f);
                effect._directions[i] = direction * (620f + i % 5 * 120f);
                rect.anchoredPosition = effect._origins[i];
                rect.localRotation = Quaternion.Euler(0, 0, i * 57f);
                effect._shards[i] = rect;
                var graphic = obj.GetComponent<GlassShardGraphic132>();
                graphic.raycastTarget = false;
                graphic.color = boss ? new Color(1f, 0.27f, 0.15f, 0.58f) : new Color(0.28f, 0.79f, 1f, 0.58f);
            }
            var copy = RuntimeUi.AddStretchRect(root, "Committed Story Reading 132");
            effect._copy = copy.gameObject.AddComponent<CanvasGroup>();
            effect._copy.blocksRaycasts = false;
            var heading = RuntimeUi.AddText(copy, "Story Impact Heading 132", boss ? "STORY BATTLE" : "STORY MOMENT",
                34, TextAnchor.MiddleCenter, boss ? RuntimeUi.Warning : RuntimeUi.Accent, FontStyle.Bold);
            Fit132(heading.rectTransform, new Vector2(.08f,.76f), new Vector2(.92f,.88f));
            var name = RuntimeUi.AddText(copy, "Story Impact Title 132", title ?? "", 58,
                TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            Fit132(name.rectTransform, new Vector2(.09f,.52f), new Vector2(.91f,.75f));
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 32; name.resizeTextMaxSize = 58;
            var body = RuntimeUi.AddText(copy, "Story Impact Description 132", description ?? "", 34,
                TextAnchor.UpperCenter, RuntimeUi.Text);
            Fit132(body.rectTransform, new Vector2(.13f,.21f), new Vector2(.87f,.50f));
            body.resizeTextForBestFit = true; body.resizeTextMinSize = 24; body.resizeTextMaxSize = 34;
            effect._readyLabel132 = boss ? "ENTER BATTLE" : "CONTINUE STORY";
            effect._continue132 = RuntimeUi.AddButton(root, "Skip Committed Story Glass 132",
                boss ? "REVEALING BATTLE…" : "REVEALING STORY…", effect.Skip132, 110f, RuntimeUi.Accent);
            effect._continue132.interactable = false;
            Fit132(effect._continue132.GetComponent<RectTransform>(), new Vector2(.34f,.05f), new Vector2(.66f,.17f));
            effect.Render132(0f);
            return effect;
        }

        // Only the already-entered real battle calls this transparent presentation.
        // Settlement releases input; it never starts, resolves, or rewards combat.
        public static CommittedGlassShatter132 PlayBattleIntro132(RectTransform parent,
            string committedRequestId, string title, bool reducedMotion, Action settled)
        {
            var effect = Play132(parent, committedRequestId, title, string.Empty, true, reducedMotion, settled);
            effect._battleIntro132 = true;
            effect.BuildBattleGlass132();
            effect.GetComponent<Image>().color = Color.clear;
            effect._copy.gameObject.SetActive(false);
            effect._continue132.gameObject.SetActive(false);
            var caption = RuntimeUi.AddPanel(effect.transform, "Story Battle Intro Caption 132",
                new Color(0.004f, 0.012f, 0.022f, .72f));
            Fit132(caption.rectTransform, new Vector2(.26f,.74f), new Vector2(.74f,.90f));
            var heading = RuntimeUi.AddText(caption.transform, "Story Battle Intro Heading 132",
                "STORY BATTLE\n" + (title ?? string.Empty), 32, TextAnchor.MiddleCenter, RuntimeUi.Text, FontStyle.Bold);
            Fit132(heading.rectTransform, new Vector2(.04f,.05f), new Vector2(.96f,.95f));
            if (reducedMotion) effect.Skip132();
            return effect;
        }

        void Update()
        {
            if (_closed) return;
            // Battle construction may have consumed the preceding frame. Give
            // the actual arena one rendered frame before timing its intro.
            if (_battleIntro132 && !_introPresented132) { _introPresented132 = true; return; }
            // A slow render/capture must not consume the visible shatter in a
            // single jump. Legacy reading timing remains unchanged.
            _elapsed += _battleIntro132 ? Mathf.Min(Time.unscaledDeltaTime, 1f / 30f) : Time.unscaledDeltaTime;
            Render132(_elapsed);
            if (_battleIntro132 && _ready132) Skip132();
            // Legacy reading still requires Continue. Only the already-entered
            // battle intro dismisses itself, releasing presentation input alone.
        }

        void Render132(float elapsed)
        {
            var t = _reduced ? 1f : Mathf.Clamp01(elapsed / 1.05f);
            if (!_ready132 && t >= 1f)
            {
                _ready132 = true;
                _continue132.GetComponentInChildren<Text>(true).text = _readyLabel132;
                _continue132.interactable = true;
            }
            _glass.alpha = _battleIntro132
                ? 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.28f, 1f, t)) : 1f - t;
            _copy.alpha = _reduced ? 1f : Mathf.Clamp01(elapsed / .24f);
            if (_battleIntro132) { RenderBattleGlass132(t); return; }
            for (var i = 0; i < _shards.Length; i++)
            {
                _shards[i].anchoredPosition = _origins[i] + _directions[i] * t * t;
                _shards[i].localRotation = Quaternion.Euler(0, 0, i * 57f + t * (i % 2 == 0 ? 150f : -150f));
            }
        }

        void BuildBattleGlass132()
        {
            // Battle glass begins as one fractured pane, not overlapping
            // colored particles. The arena is visible through every facet.
            foreach (var previous in _shards)
            {
                previous.gameObject.SetActive(false);
                Destroy(previous.gameObject);
            }
            var glass = (RectTransform)_glass.transform;
            Fit132(glass, new Vector2(.018f, .205f), new Vector2(.982f, .845f));
            var edge = new[] {
                new Vector2(0f,0f), new Vector2(.33f,0f), new Vector2(.72f,0f),
                new Vector2(1f,0f), new Vector2(1f,.36f), new Vector2(1f,.72f),
                new Vector2(1f,1f), new Vector2(.67f,1f), new Vector2(.28f,1f),
                new Vector2(0f,1f), new Vector2(0f,.64f), new Vector2(0f,.28f) };
            var impact = new Vector2(.47f,.53f);
            var inner = new Vector2[edge.Length];
            for (var i = 0; i < inner.Length; i++)
                inner[i] = Vector2.Lerp(impact, edge[i], .19f + (i % 3) * .055f);
            _shards = new RectTransform[edge.Length * 2];
            _battleRadials132 = new Vector2[_shards.Length];
            _battleSpins132 = new float[_shards.Length];
            for (var i = 0; i < edge.Length; i++)
            {
                var next = (i + 1) % edge.Length;
                AddBattleFacet132(glass, i * 2, new[] { impact, inner[i], inner[next] }, impact);
                AddBattleFacet132(glass, i * 2 + 1,
                    new[] { inner[i], edge[i], edge[next], inner[next] }, impact);
            }
            RenderBattleGlass132(0f);
        }

        void AddBattleFacet132(RectTransform glass, int index, Vector2[] polygon, Vector2 impact)
        {
            var centroid = Vector2.zero;
            foreach (var point in polygon) centroid += point;
            centroid /= polygon.Length;
            var obj = new GameObject("Battle Glass Pane " + index + " 132",
                typeof(RectTransform), typeof(BattleGlassFacet132));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(glass, false);
            rect.pivot = centroid;
            Fit132(rect, Vector2.zero, Vector2.one);
            var facet = obj.GetComponent<BattleGlassFacet132>();
            facet.color = new Color(.76f + (index % 3) * .055f, .93f, 1f,
                .035f + (index % 4) * .012f);
            facet.raycastTarget = false;
            facet.Configure132(polygon);
            _shards[index] = rect;
            _battleRadials132[index] = (centroid - impact).normalized;
            _battleSpins132[index] = (index % 2 == 0 ? 1f : -1f) * (12f + index % 5 * 7f);
        }

        void RenderBattleGlass132(float time)
        {
            // A brief intact cracked pane establishes the material before
            // pieces accelerate radially away from the point of impact.
            var travel = Mathf.Clamp01((time - .12f) / .88f);
            var size = ((RectTransform)_glass.transform).rect.size;
            for (var i = 0; i < _shards.Length; i++)
            {
                var distance = travel * travel * (.72f + (i % 4) * .12f);
                _shards[i].anchoredPosition = Vector2.Scale(_battleRadials132[i], size) * distance;
                _shards[i].localRotation = Quaternion.Euler(0f, 0f, _battleSpins132[i] * travel);
                _shards[i].localScale = new Vector3(1f - .24f * Mathf.Sin(travel * Mathf.PI), 1f, 1f);
            }
        }

        public void Skip132()
        {
            if (_closed || !_ready132 || !isActiveAndEnabled) return;
            _closed = true;
            var callback = _completed; _completed = null;
            gameObject.SetActive(false);
            callback?.Invoke();
        }
        public void Cancel132()
        {
            _closed = true; _completed = null;
            if (_continue132 != null) _continue132.interactable = false;
        }
        void OnDisable() => Cancel132();
        void OnDestroy() => Cancel132();
        internal static void Fit132(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }

    // Convex pieces tile one pane in normalized coordinates. Thin edge strips
    // carry the glint; the fill stays transparent instead of whitening alpha.
    public sealed class BattleGlassFacet132 : MaskableGraphic
    {
        Vector2[] _polygon132;
        public void Configure132(Vector2[] polygon)
        {
            _polygon132 = (Vector2[])polygon.Clone();
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_polygon132 == null || _polygon132.Length < 3) return;
            var size = rectTransform.rect.size;
            var pivot = rectTransform.pivot;
            var points = new Vector2[_polygon132.Length];
            for (var i = 0; i < points.Length; i++)
            {
                points[i] = Vector2.Scale(_polygon132[i] - pivot, size);
                vh.AddVert(points[i], color, Vector2.zero);
            }
            for (var i = 1; i < points.Length - 1; i++) vh.AddTriangle(0, i, i + 1);
            for (var i = 0; i < points.Length; i++)
            {
                var a = points[i]; var b = points[(i + 1) % points.Length];
                var direction = b - a;
                if (direction.sqrMagnitude < .001f) continue;
                var edge = new Vector2(-direction.y, direction.x).normalized;
                AddEdge132(vh, a, b, edge, 1.6f, new Color(.82f, .96f, 1f, .58f));
                // A narrower silver glint gives the crack a bright center
                // without filling an entire fragment with opaque color.
                AddEdge132(vh, a, b, edge, .55f, new Color(.96f, 1f, 1f, .82f));
            }
        }

        static void AddEdge132(VertexHelper vh, Vector2 a, Vector2 b, Vector2 normal, float width, Color tint)
        {
            var first = vh.currentVertCount;
            var offset = normal * width * .5f;
            vh.AddVert(a - offset, tint, Vector2.zero);
            vh.AddVert(a + offset, tint, Vector2.up);
            vh.AddVert(b + offset, tint, Vector2.one);
            vh.AddVert(b - offset, tint, Vector2.right);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first, first + 2, first + 3);
        }
    }

    public sealed class GlassShardGraphic132 : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            vh.AddVert(new Vector3(r.xMin,r.yMin), color, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax,r.yMin+r.height*.23f), color, Vector2.right);
            vh.AddVert(new Vector3(r.xMin+r.width*.56f,r.yMax), Color.Lerp(color,Color.white,.55f), Vector2.up);
            vh.AddTriangle(0,1,2);
        }
    }
}
