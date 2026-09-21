using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    /// <summary>Player-driven presentation of two already committed dice. No state, RNG, or reward commands.</summary>
    [DisallowMultipleComponent]
    public sealed class CommittedQuestDice132 : MonoBehaviour
    {
        public const float Duration132 = 2.40f;
        public const float FirstLanding132 = 1.97f;
        public const string RollButton132 = "Roll Committed Quest Dice 132";
        public bool IsSettled132 { get; private set; }
        public bool IsRolling132 { get; private set; }
        public int LandedDice132 { get; private set; }
        public Transform CopyScope132 => _scope;
        readonly List<Copy132> _copies = new List<Copy132>();
        readonly List<Button> _heldButtons = new List<Button>();
        RectTransform[] _dice;
        Image[] _shadows, _lights;
        Shadow[] _edges;
        Transform _scope;
        Text _status, _equation;
        Button _roll;
        Action<int, int> _pips;
        Action _settled;
        int _one, _two, _modifier, _total;
        bool _positive, _configured, _skipFirstDelta;
        float _elapsed;
        AudioSource _audio;
        AudioClip _impact;
        Image _surface;
        Color _surfaceColor;

        sealed class Copy132 { public Text Text; public string Copy; public Color Color; }

        public void Configure132(RectTransform first, RectTransform second, Text status, Text equation,
            int one, int two, int modifier, int total, bool positive, bool settleImmediately,
            Action<int, int> pips, Action settled, Transform copyScope, Sprite disc)
        {
            _dice = new[] { first, second }; _status = status; _equation = equation;
            _one = one; _two = two; _modifier = modifier; _total = total; _positive = positive;
            _pips = pips; _settled = settled; _scope = copyScope;
            // The sum occupies the action area after landing, leaving the full felt for the toss.
            _equation.transform.SetParent(transform, false);
            var equationLayout = _equation.GetComponent<LayoutElement>();
            if (equationLayout != null) equationLayout.ignoreLayout = true;
            _equation.rectTransform.anchorMin = new Vector2(.04f, .015f);
            _equation.rectTransform.anchorMax = new Vector2(.96f, .23f);
            _equation.rectTransform.offsetMin = _equation.rectTransform.offsetMax = Vector2.zero;
            _shadows = new Image[2]; _lights = new Image[2]; _edges = new Shadow[2];
            var felt = GetComponent<Image>();
            if (felt != null) felt.color = new Color(.018f, .115f, .093f, 1f);
            for (var i = 0; i < 2; i++)
            {
                var die = _dice[i];
                _edges[i] = die.GetComponent<Shadow>();
                _shadows[i] = RuntimeUi.AddPanel(die.parent, "Dice Contact Shadow 132 " + i,
                    new Color(0, 0, 0, .42f));
                _shadows[i].sprite = disc; _shadows[i].raycastTarget = false;
                _shadows[i].gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                var shadow = _shadows[i].rectTransform;
                shadow.anchorMin = shadow.anchorMax = new Vector2(.5f, .5f);
                shadow.sizeDelta = new Vector2(98, 28); shadow.anchoredPosition = new Vector2(0, -43);
                shadow.SetAsFirstSibling();
                var depth = new GameObject("Ivory Dice Depth 132 " + i, typeof(RectTransform), typeof(QuestDiceDepth132));
                var depthRect = depth.GetComponent<RectTransform>(); depthRect.SetParent(die, false);
                depthRect.anchorMin = Vector2.zero; depthRect.anchorMax = Vector2.one;
                depthRect.offsetMin = depthRect.offsetMax = Vector2.zero; depthRect.SetAsFirstSibling();
                depth.GetComponent<QuestDiceDepth132>().raycastTarget = false;
                _lights[i] = RuntimeUi.AddPanel(die, "Dice Moving Light 132 " + i, new Color(1, 1, 1, .32f));
                _lights[i].raycastTarget = false;
                var light = _lights[i].rectTransform;
                light.anchorMin = new Vector2(.08f, .88f); light.anchorMax = new Vector2(.92f, .95f);
                light.offsetMin = light.offsetMax = Vector2.zero;
            }
            _roll = RuntimeUi.AddButton(transform, RollButton132, "ROLL THE DICE", Roll132, 50f, RuntimeUi.Accent);
            var rect = _roll.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.19f, .01f); rect.anchorMax = new Vector2(.81f, .23f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var rollLayout = _roll.GetComponent<LayoutElement>();
            if (rollLayout == null) rollLayout = _roll.gameObject.AddComponent<LayoutElement>();
            rollLayout.ignoreLayout = true;
            var label = _roll.GetComponentInChildren<Text>();
            label.fontSize = 28; label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22; label.resizeTextMaxSize = 30;
            _configured = true;
            if (settleImmediately) { Settle132(); return; }
            // Cosmetic rest faces deliberately do not expose either committed face.
            _pips?.Invoke(1, 6);
            _equation.text = "";
            _status.text = "YOUR ROLL  •  TOSS BOTH DICE";
            _status.color = RuntimeUi.Text;
            _surface = _scope == null ? null : _scope.GetComponent<Image>();
            if (_surface != null) { _surfaceColor = _surface.color; _surface.color = new Color(.025f, .06f, .065f, .98f); }
            CaptureCopy132();
        }

        public static CommittedQuestDice132 Find132(Transform origin)
        {
            for (var current = origin; current != null; current = current.parent)
            {
                var dice = current.GetComponentInChildren<CommittedQuestDice132>(true);
                if (dice != null) return dice;
                if (current.GetComponent<Canvas>() != null) break;
            }
            return null;
        }

        public void Roll132()
        {
            if (!_configured || !isActiveAndEnabled || IsRolling132 || IsSettled132) return;
            IsRolling132 = true; _elapsed = 0; _skipFirstDelta = true;
            _roll.interactable = false;
            _roll.GetComponentInChildren<Text>().text = "ROLLING…";
            _status.text = "TOSSING THE DICE…";
            PrepareAudio132();
            Present132(0);
        }

        void Update()
        {
            if (!IsRolling132) return;
            // Resource creation or a stalled frame must not swallow the toss.
            if (_skipFirstDelta) { _skipFirstDelta = false; return; }
            _elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            Present132(_elapsed);
            if (_elapsed >= Duration132) Settle132();
        }

        void LateUpdate()
        {
            // Callers append their exact outcome text after constructing the dice.
            // Capture it before the first canvas render, then restore it only on landing.
            if (_configured && !IsSettled132) CaptureCopy132();
        }

        void Present132(float elapsed)
        {
            var firstDone = elapsed >= FirstLanding132;
            var secondDone = elapsed >= Duration132;
            var count = secondDone ? 2 : firstDone ? 1 : 0;
            if (count > LandedDice132)
            {
                LandedDice132 = count;
                if (_audio != null && _impact != null) _audio.PlayOneShot(_impact, count == 1 ? .72f : 1f);
            }
            var step = Mathf.FloorToInt(elapsed / .063f);
            _pips?.Invoke(firstDone ? _one : (step * 5 + 2) % 6 + 1,
                secondDone ? _two : (step * 5 + 4) % 6 + 1);
            for (var i = 0; i < 2; i++)
            {
                var duration = i == 0 ? FirstLanding132 : Duration132;
                var t = Mathf.Clamp01(elapsed / duration);
                var remaining = 1 - t;
                var die = _dice[i]; if (die == null) continue;
                var width = die.rect.width;
                var airborne = Mathf.Sin(Mathf.Min(1, t / .43f) * Mathf.PI);
                var bounce = t < .43f ? airborne : Mathf.Abs(Mathf.Sin((t - .43f) * 4.5f * Mathf.PI)) * remaining;
                var direction = i == 0 ? -1f : 1f;
                var tossWidth = Mathf.Min(width * .70f, Mathf.Max(0,
                    (((RectTransform)transform).rect.width - width * 2 - 76) * .5f));
                die.anchoredPosition = new Vector2(direction * tossWidth * remaining * remaining,
                    width * (.46f * bounce - .04f * remaining));
                var tumble = remaining * remaining;
                die.localRotation = Quaternion.Euler(
                    Mathf.Sin(t * 7 * Mathf.PI + i) * 49 * tumble,
                    Mathf.Cos(t * 8 * Mathf.PI + i) * 43 * tumble,
                    direction * 640 * tumble);
                var impact = t < .43f ? 0 : Mathf.Pow(1 - Mathf.Abs(Mathf.Sin((t - .43f) * 4.5f * Mathf.PI)), 9) * remaining;
                die.localScale = new Vector3(1 + impact * .15f, 1 - impact * .19f, 1);
                _shadows[i].rectTransform.sizeDelta = new Vector2(width * (1.18f + bounce * .35f), width * .30f);
                _shadows[i].color = new Color(0, 0, 0, .44f - bounce * .26f);
                _shadows[i].rectTransform.anchoredPosition = new Vector2(die.anchoredPosition.x * .55f, -width * .49f);
                _lights[i].color = new Color(1, .97f, .83f, .20f + Mathf.Abs(Mathf.Sin(t * 15 + i)) * .38f * remaining);
                if (_edges[i] != null) _edges[i].effectDistance = new Vector2(5 * tumble, -6 - 13 * bounce);
            }
            if (firstDone && !secondDone) _status.text = "ONE DOWN…";
        }

        void Settle132()
        {
            if (!_configured || IsSettled132) return;
            Present132(Duration132);
            IsRolling132 = false; IsSettled132 = true;
            _pips?.Invoke(_one, _two);
            _equation.text = (_modifier >= 0 ? "+" : "") + _modifier + "  =  " + _total;
            _equation.fontSize = 40;
            _equation.color = _positive ? RuntimeUi.Positive : RuntimeUi.Warning;
            _status.text = "DICE SETTLED  •  SAVED RESULT";
            _status.color = _equation.color;
            _roll.gameObject.SetActive(false);
            foreach (var copy in _copies)
                if (copy.Text != null) { copy.Text.text = copy.Copy; copy.Text.color = copy.Color; }
            foreach (var button in _heldButtons) if (button != null) button.interactable = true;
            if (_surface != null) _surface.color = _surfaceColor;
            var settled = _settled; _settled = null; settled?.Invoke();
        }

        void CaptureCopy132()
        {
            if (_scope == null) return;
            foreach (var text in _scope.GetComponentsInChildren<Text>(true))
            {
                var name = CopyName132(text.name);
                if (!SensitiveCopy132(name) || _copies.Exists(value => value.Text == text)) continue;
                _copies.Add(new Copy132 { Text = text, Copy = text.text, Color = text.color });
                if (name == "Message")
                {
                    var split = text.text.IndexOf("\nRESULT", StringComparison.Ordinal);
                    text.text = (split >= 0 ? text.text.Substring(0, split) + "\n" : "") + "Roll the dice to reveal the result.";
                }
                else if (name == "World Gate Saved Dice Heading 084")
                    text.text = text.text.Replace("SAVED CHECK", "ROLL CHECK");
                else text.text = name == "World Gate Automatic Result Apply 084"
                    ? "WAITING FOR YOUR ROLL" : "";
                text.color = RuntimeUi.Text;
            }
            foreach (var button in _scope.GetComponentsInChildren<Button>(true))
                if ((button.name == "Retry saved World Gate result 084" ||
                     button.name == "Acknowledge Board Quest Card Resolution 090" ||
                     button.name == "Enter optional Expedition card battle 089") &&
                    button.interactable && !_heldButtons.Contains(button))
                { _heldButtons.Add(button); button.interactable = false; }
        }

        static string CopyName132(string name)
        {
            // Both existing layout helpers decorate names, sometimes repeatedly.
            // Match only their known trailing annotations; retain the exact copy identity.
            foreach (var suffix in new[] { " [Responsive 062]", " [Authored Compact 076]" })
                if (name.EndsWith(suffix, StringComparison.Ordinal))
                    return CopyName132(name.Substring(0, name.Length - suffix.Length));
            return name;
        }

        static bool SensitiveCopy132(string name) => name == "Message" ||
            name == "World Gate Saved Dice Heading 084" || name == "World Gate Automatic Result Apply 084" ||
            name == "World Gate Chest Exact Reward 092" || name == "Board Adventure Resolved Reward Copy 087" ||
            name == "Board Quest Dice Numbers 081" || name == "Board Quest Dice Outcome 081" ||
            name == "Board Quest Dice Reward 081" || name == "Expedition Current Position Summary 074" ||
            name == "Board Quest Resolved Card Reward 090" || name == "Board Quest Card Pawn Motion 090";

        void PrepareAudio132()
        {
            if (FindAnyObjectByType<AudioListener>() == null) return;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false; _audio.spatialBlend = 0; _audio.volume = .24f;
            const int rate = 22050;
            var samples = new float[2646];
            for (var i = 0; i < samples.Length; i++)
            {
                var t = i / (float)rate;
                var grain = Mathf.Sin(i * 1.713f) * Mathf.Sin(i * .437f);
                samples[i] = (Mathf.Sin(2 * Mathf.PI * 142 * t) * .62f + grain * .38f) * Mathf.Exp(-t * 46);
            }
            _impact = AudioClip.Create("Ivory Dice Table Impact 132", samples.Length, 1, rate, false);
            _impact.SetData(samples, 0);
        }

        void OnDisable()
        {
            // Closing a view never settles or applies its receipt. A new view can roll the same saved dice.
            IsRolling132 = false; _settled = null;
            if (_audio != null) _audio.Stop();
        }
        void OnDestroy() { if (_impact != null) Destroy(_impact); }
    }

    sealed class QuestDiceDepth132 : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect; const float d = 7;
            Quad132(vh, new Vector2(r.xMin, r.yMin), new Vector2(r.xMax, r.yMin),
                new Vector2(r.xMax + d, r.yMin - d), new Vector2(r.xMin + d, r.yMin - d), new Color(.48f, .44f, .34f));
            Quad132(vh, new Vector2(r.xMax, r.yMin), new Vector2(r.xMax, r.yMax),
                new Vector2(r.xMax + d, r.yMax - d), new Vector2(r.xMax + d, r.yMin - d), new Color(.69f, .66f, .56f));
        }
        static void Quad132(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            var start = vh.currentVertCount;
            foreach (var point in new[] { a, b, c, d }) vh.AddVert(point, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
