using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    // Browsing only. This component has no equipment, assignment, XP or save API.
    public sealed class OwnedHeroPicker155 : MonoBehaviour
    {
        const int PageSize = 6;
        Func<M1PresentationState> read;
        Action<string> selected;
        M1PresentationState snapshot;
        string currentId;
        int page;
        RectTransform results;
        InputField search;
        Text summary;
        Button previous, next, back;
        readonly List<Selectable> focus = new List<Selectable>();
        CanvasGroup ownerGroup;
        bool oldInteractable, closed, closeQueued;
        GameObject previousFocus;

        public static OwnedHeroPicker155 Show(Transform owner, Func<M1PresentationState> read,
            string currentId, Action<string> selected)
        {
            if (owner == null || read == null || selected == null) return null;
            var old = owner.GetComponentInChildren<OwnedHeroPicker155>(true);
            if (old != null) old.Close();
            var host = new GameObject("Find Owned Hero 155", typeof(RectTransform), typeof(Image),
                typeof(LayoutElement), typeof(CanvasGroup), typeof(OwnedHeroPicker155));
            host.transform.SetParent(owner, false);
            host.GetComponent<LayoutElement>().ignoreLayout = true;
            var picker = host.GetComponent<OwnedHeroPicker155>();
            picker.read = read; picker.selected = selected; picker.currentId = currentId;
            picker.previousFocus = EventSystem.current?.currentSelectedGameObject;
            picker.ownerGroup = owner.GetComponent<CanvasGroup>();
            if (picker.ownerGroup == null) picker.ownerGroup = owner.gameObject.AddComponent<CanvasGroup>();
            picker.oldInteractable = picker.ownerGroup.interactable;
            picker.ownerGroup.interactable = false;
            // Block underlying buttons and their keyboard navigation, while this
            // child modal remains interactive. Parent save/lifecycle is untouched.
            host.GetComponent<CanvasGroup>().ignoreParentGroups = true;
            Anchor(host.GetComponent<RectTransform>(), new Rect(0, 0, 1, 1));
            host.GetComponent<Image>().color = RuntimeUi.Background;
            picker.Build();
            return picker;
        }

        // Exact IDs survive sorting and identical names. Empty search includes all.
        public static M1RecruitLoadoutView[] Filter(M1PresentationState state, string query)
        {
            var tokens = (query ?? string.Empty).Trim().Split(new[] { ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            return (state?.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                .Where(hero => hero != null && !string.IsNullOrWhiteSpace(hero.RecruitId))
                .GroupBy(hero => hero.RecruitId, StringComparer.Ordinal).Select(group => group.First())
                .Where(hero => tokens.All(token =>
                    (hero.DisplayName ?? string.Empty).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (hero.ObservedClass ?? string.Empty).IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(hero => hero.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(hero => hero.RecruitId, StringComparer.Ordinal).ToArray();
        }

        void Build()
        {
            var heading = Label(transform, "Find hero title 155", "FIND AN OWNED HERO",
                new Rect(.04f, .89f, .65f, .075f), 55, RuntimeUi.Accent);
            heading.fontStyle = FontStyle.Bold;
            back = Button(transform, "Close hero search 155", "BACK", new Rect(.80f, .89f, .16f, .075f), Close);
            search = RuntimeUi.AddInputField(transform, "Hero name or role 155", "Search name or role…", 80);
            Anchor(search.GetComponent<RectTransform>(), new Rect(.04f, .775f, .92f, .095f));
            search.textComponent.fontSize = 42;
            ((Text)search.placeholder).fontSize = 38;
            search.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel = QueueClose;
            // Keep the field alive while rebuilding results. Typing never calls
            // native mutations, changes selected hero, or submits a purchase.
            search.onValueChanged.AddListener(value => { page = 0; DrawResults(); });
            summary = Label(transform, "Hero search summary 155", "", new Rect(.04f, .71f, .92f, .05f), 32, RuntimeUi.MutedText);
            results = RuntimeUi.AddStretchRect(transform, "Owned hero results 155");
            Anchor(results, new Rect(.04f, .15f, .92f, .545f));
            previous = Button(transform, "Previous hero results 155", "← PREVIOUS", new Rect(.04f, .045f, .25f, .075f),
                () => { page--; DrawResults(); });
            next = Button(transform, "Next hero results 155", "NEXT →", new Rect(.71f, .045f, .25f, .075f),
                () => { page++; DrawResults(); });
            Label(transform, "Hero search browsing 155", "Choose a hero to view. Gear and Union changes stay separate.",
                new Rect(.31f, .04f, .38f, .085f), 29, RuntimeUi.MutedText);
            ReadSnapshot();
            DrawResults();
            search.Select(); search.ActivateInputField();
        }

        bool ReadSnapshot()
        {
            try { snapshot = read?.Invoke(); return snapshot != null; }
            catch (Exception error)
            {
                snapshot = null;
                Debug.LogWarning("Owned hero list unavailable: " + error.Message);
                return false;
            }
        }

        void DrawResults()
        {
            if (closed || results == null) return;
            foreach (Transform child in results) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            focus.Clear(); focus.Add(search); focus.Add(back);
            var matches = Filter(snapshot, search.text);
            var pages = Math.Max(1, (matches.Length + PageSize - 1) / PageSize);
            page = Mathf.Clamp(page, 0, pages - 1);
            summary.text = snapshot == null ? "Roster unavailable. Return and reopen this screen."
                : matches.Length == 0 ? "No matching owned heroes. Clear or change the search."
                : matches.Length + " MATCHES  •  PAGE " + (page + 1) + " / " + pages;
            for (int i = 0; i < PageSize && page * PageSize + i < matches.Length; i++)
            {
                var hero = matches[page * PageSize + i];
                var id = hero.RecruitId;
                var union = (snapshot.Unions ?? Array.Empty<M1UnionView>()).FirstOrDefault(value =>
                    value != null && (value.MemberRecruitIds ?? Array.Empty<string>()).Contains(id));
                var place = union == null ? "RESERVE" : M1UnionIdentity076.ResolveTab(union.UnionId, union.DisplayName, union.Index);
                var name = string.IsNullOrWhiteSpace(hero.DisplayName) ? "Adventurer" : hero.DisplayName;
                var caption = name + (id == currentId ? "  ✓" : "") + "\n" +
                    (hero.ObservedClass ?? "Adventurer") + "  ·  LV " + Math.Max(1, hero.Level) + "  ·  " + place;
                var button = Button(results, "Choose owned hero " + id + " 155", caption,
                    new Rect((i % 2) * .51f, .69f - (i / 2) * .345f, .49f, .31f), () => Choose(id));
                var text = button.GetComponentInChildren<Text>();
                text.alignment = TextAnchor.MiddleLeft;
                text.fontSize = 36; text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 28; text.resizeTextMaxSize = 36;
                focus.Add(button);
            }
            previous.interactable = page > 0; next.interactable = page + 1 < pages;
            focus.Add(previous); focus.Add(next);
        }

        void Choose(string id)
        {
            // Ownership may have changed since opening/filtering. Never resolve
            // by the old row index or substitute the first hero after a mismatch.
            if (!ReadSnapshot() || !(snapshot.Recruits ?? Array.Empty<M1RecruitLoadoutView>())
                    .Any(hero => hero != null && StringComparer.Ordinal.Equals(hero.RecruitId, id)))
            {
                DrawResults();
                summary.text = "That hero is no longer available in this roster. Choose again.";
                return;
            }
            var callback = selected;
            Close();
            callback?.Invoke(id);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { QueueClose(); return; }
            if (!Input.GetKeyDown(KeyCode.Tab)) return;
            var available = focus.Where(value => value != null && value.IsInteractable()).ToArray();
            if (available.Length == 0) return;
            var current = EventSystem.current?.currentSelectedGameObject;
            var index = Array.FindIndex(available, value => value.gameObject == current);
            var direction = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? -1 : 1;
            var target = available[(index + direction + available.Length) % available.Length];
            search.DeactivateInputField(); target.Select();
            if (target == search) search.ActivateInputField();
        }

        void QueueClose()
        {
            if (closed || closeQueued) return;
            closeQueued = true;
            StartCoroutine(CloseAfterInputFrame());
        }

        System.Collections.IEnumerator CloseAfterInputFrame()
        {
            // Armory buttons have native Cancel handlers. Keep focus inside this
            // modal until all input processing for this frame has completed, so
            // one Escape cannot also activate the underlying Back-to-Hall path.
            yield return new WaitForEndOfFrame();
            Close();
        }

        public void Close()
        {
            if (closed) return;
            closed = true;
            RestoreOwner();
            if (previousFocus != null && previousFocus.activeInHierarchy)
                EventSystem.current?.SetSelectedGameObject(previousFocus);
            gameObject.SetActive(false); Destroy(gameObject);
        }

        void RestoreOwner()
        {
            if (ownerGroup == null) return;
            ownerGroup.interactable = oldInteractable;
            // Retain the restored group: Destroy is deferred, so another picker
            // opened this frame must not inherit a group awaiting destruction.
            ownerGroup = null;
        }
        void OnDestroy() { RestoreOwner(); }
        static void Anchor(RectTransform rect, Rect bounds)
        {
            rect.anchorMin = bounds.min; rect.anchorMax = bounds.max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        static Text Label(Transform parent, string name, string value, Rect bounds, int size, Color color)
        {
            var text = RuntimeUi.AddText(parent, name + " [Readable 079]", value, size, TextAnchor.MiddleLeft, color);
            Anchor(text.rectTransform, bounds); text.raycastTarget = false; return text;
        }
        Button Button(Transform parent, string name, string label, Rect bounds, Action action)
        {
            var button = RuntimeUi.AddButton(parent, name, label, action);
            Anchor(button.GetComponent<RectTransform>(), bounds);
            var text = button.GetComponentInChildren<Text>();
            text.name += " [Readable 079]"; text.fontSize = 36;
            button.gameObject.AddComponent<ConfirmationCancelHandler077>().Cancel = QueueClose;
            return button;
        }
    }
}
