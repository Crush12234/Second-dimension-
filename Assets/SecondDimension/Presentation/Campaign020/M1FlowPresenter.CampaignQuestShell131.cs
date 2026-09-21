using System;
using System.Linq;
using SecondDimension.Presentation.GuildCity017D;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        // The owning Campaign adapter binds its existing receipt cancellation
        // to this actual root. The shell never reads or commits campaign state.
        RectTransform _campaignQuestRoot131;

        RectTransform BuildCampaignQuestShell131(string title, string objective,
            string world, string progress, Action back,
            string backdropResourcePath = null)
        {
            if (_screenRoot == null) return null;
            if (_activePage != null && _activePage != _screenRoot &&
                _activePage != _campaignQuestRoot131 && _activePage.IsChildOf(_screenRoot))
                _activePage.gameObject.SetActive(false);
            if (_campaignQuestRoot131 != null)
            {
                _campaignQuestRoot131.gameObject.SetActive(false);
                Destroy(_campaignQuestRoot131.gameObject);
            }

            var root = RuntimeUi.AddPanel(_screenRoot, "Campaign Quest Shell 131",
                new Color(0.006f, 0.012f, 0.020f, 1f));
            _campaignQuestRoot131 = root.rectTransform;
            NormalizeExpeditionViewport076(_campaignQuestRoot131);
            root.gameObject.AddComponent<WorldGateEventLifetime110>();
            _activePage = _campaignQuestRoot131;
            _activeContent = null;
            _activeScroll = null;

            // Use the opening quest's actual backdrop resolver. A caller may
            // supply an authored map path; world display copy is never an art ID.
            BuildStudioExpeditionBackdrop076(root.transform,
                new ExpeditionBoardView074 { MapResourcePath = backdropResourcePath });

            var returnButton = RuntimeUi.AddButton(root.transform,
                "Campaign Quest Return To Guild 131", "\u2190  GUILD HALL",
                () =>
                {
                    if(_coordinator is Campaign020.ICampaignRecoveryCoordinator150 recovery)
                    {
                        var result=recovery.PauseCampaign150();
                        if(!result.Succeeded)
                        {
                            _localStatus=result.Message; _localStatusPositive=false;
                            BuildCurrentScreen();
                            return;
                        }
                    }
                    back?.Invoke();
                }, 70f, RuntimeUi.ButtonNormal);
            SetAnchors074(returnButton.GetComponent<RectTransform>(),
                new Vector2(0.025f, 0.895f), new Vector2(0.145f, 0.975f));
            ConfigureAuthoredCompactText076(returnButton.GetComponentInChildren<Text>(), 24, 30);
            var phoneNavigation164 = LoopStripVisible164 && (Screen.width < 1000 || Screen.height < 570);
            if (phoneNavigation164) returnButton.gameObject.SetActive(false);

            var header = RuntimeUi.AddPanel(root.transform, "Campaign Quest Header 131",
                new Color(0.004f, 0.012f, 0.022f, 0.94f));
            SetAnchors074(header.rectTransform,
                new Vector2(0.155f, 0.885f), new Vector2(0.975f, 0.985f));
            if (phoneNavigation164) SetAnchors074(header.rectTransform,
                new Vector2(0.155f, 0.825f), new Vector2(0.975f, 0.985f));
            StyleNeutralBoardSurface091(header, 0.76f);
            var heading = RuntimeUi.AddText(header.transform, "Campaign Quest Title 131",
                title ?? string.Empty, 30, TextAnchor.MiddleLeft,
                RuntimeUi.Accent, FontStyle.Bold);
            SetAnchors074(heading.rectTransform,
                new Vector2(0.025f, 0.53f), new Vector2(0.76f, 0.95f));
            ConfigureAuthoredCompactText076(heading, 20, 34);

            var position = RuntimeUi.AddText(header.transform, "Campaign Quest Progress 131",
                string.Join("\n", new[] { world, progress }.Where(value => !string.IsNullOrWhiteSpace(value))),
                22, TextAnchor.MiddleRight, RuntimeUi.Warning, FontStyle.Bold);
            SetAnchors074(position.rectTransform,
                new Vector2(0.815f, 0.10f), new Vector2(0.975f, 0.95f));
            ConfigureAuthoredCompactText076(position, 15, 23);

            var hasFailure = !string.IsNullOrWhiteSpace(_localStatus) && !_localStatusPositive;
            var goal = RuntimeUi.AddText(header.transform, "Campaign Quest Objective 131",
                hasFailure ? _localStatus : objective ?? string.Empty, 23, TextAnchor.MiddleLeft,
                hasFailure ? RuntimeUi.Error : RuntimeUi.Text, FontStyle.Bold);
            SetAnchors074(goal.rectTransform,
                new Vector2(0.025f, 0.04f), new Vector2(0.80f, 0.53f));
            ConfigureAuthoredCompactText076(goal, 17, 25);

            var table = RuntimeUi.AddPanel(root.transform, "Campaign Quest Card Table 131",
                new Color(0.004f, 0.012f, 0.024f, 0.16f));
            SetAnchors074(table.rectTransform,
                new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.86f));
            if (phoneNavigation164) SetAnchors074(table.rectTransform,
                new Vector2(0.04f, 0.025f), new Vector2(0.96f, 0.815f));
            _campaignQuestRoot131.SetAsLastSibling();
            ScheduleExpeditionViewportFinalize076(_campaignQuestRoot131);
            return table.rectTransform;
        }

        RectTransform BuildCampaignQuestScene131(Transform parent, string title,
            string description, string illustrationResourcePath)
        {
            var scene = RuntimeUi.AddPanel(parent, "Campaign Quest Scene 131",
                new Color(0.015f, 0.036f, 0.060f, 0.99f));
            RuntimeUi.AddVerticalLayout(scene.transform,
                new RectOffset(22, 22, 14, 16), 7f, TextAnchor.UpperCenter);
            var heading = RuntimeUi.AddText(scene.transform, "Campaign Quest Scene Title 131",
                title ?? string.Empty, 27, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            RuntimeUi.SetLayout(heading, preferredHeight: 66f);
            var copy = RuntimeUi.AddText(scene.transform, "Campaign Quest Scene Description 131",
                description ?? string.Empty, 21, TextAnchor.MiddleLeft, RuntimeUi.Text);
            RuntimeUi.SetLayout(copy, preferredHeight: 120f);
            StyleCampaignQuestExistingScene131(scene.rectTransform, illustrationResourcePath);
            return scene.rectTransform;
        }

        void StyleCampaignQuestExistingScene131(RectTransform face,
            string illustrationResourcePath)
        {
            if (face == null) return;
            // This is the opening 090 resolved-card footprint, not a stretched
            // full-screen information panel. Existing receipt/roll controls stay.
            SetAnchors074(face, new Vector2(0.12f, 0.03f), new Vector2(0.88f, 0.97f));
            var sizing = face.GetComponent<ContentSizeFitter>();
            if (sizing != null) sizing.enabled = false;
            var faceLayout = face.GetComponent<LayoutElement>();
            if (faceLayout == null) faceLayout = face.gameObject.AddComponent<LayoutElement>();
            faceLayout.ignoreLayout = true;
            StyleNeutralBoardSurface091(face.GetComponent<Image>());

            // Keep the existing library frame but omit the generic category
            // ribbon: the authored title already identifies this actual event.
            foreach (var ribbon in face.GetComponentsInChildren<Image>(true).Where(value =>
                value.name.StartsWith("Board Adventure Revealed Card Type Ribbon 087", StringComparison.Ordinal)))
                ribbon.gameObject.SetActive(false);
            if (face.Find("Board Adventure Revealed Card Library Frame 086") == null)
            {
                var frame = RuntimeUi.AddPanel(face,
                    "Board Adventure Revealed Card Library Frame 086", new Color(0.48f, 0.53f, 0.62f, 0.24f));
                frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                Stretch(frame.rectTransform);
                frame.transform.SetAsFirstSibling();
                frame.raycastTarget = false;
                frame.sprite = Resources.Load<Sprite>(BoardAdventureCardFaceResource084);
                frame.type = frame.sprite == null ? Image.Type.Simple : Image.Type.Sliced;
            }
            var shadow = face.GetComponent<Shadow>();
            if (shadow == null) shadow = face.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.88f);
            shadow.effectDistance = new Vector2(0f, -10f);
            shadow.useGraphicAlpha = false;

            if (CampaignQuestReadingColumn131(face) == null &&
                !string.IsNullOrWhiteSpace(illustrationResourcePath))
            {
                GuildCity017E.GuildCityOpeningExperienceRegistry017E.AddImage(face,
                    "Campaign Quest Illustration 131", illustrationResourcePath, 360f);
                ArrangeIllustratedBoardCard091(face, "Campaign Quest Illustration 131", false);
            }
            FitCampaignQuestActions131(face);
            face.Find("Board Adventure Card Flip Stage 086")?.SetAsLastSibling();
        }

        static RectTransform CampaignQuestReadingColumn131(RectTransform face) =>
            face == null ? null : face.Find("Board Card Reading Column 091") as RectTransform;

        static RectTransform CampaignQuestActionsParent131(RectTransform face) =>
            CampaignQuestReadingColumn131(face) ?? face;

        static void FitCampaignQuestActions131(RectTransform face)
        {
            var reading = CampaignQuestReadingColumn131(face);
            if (reading == null) return;
            // Root adapters move the original actions into this column; do not
            // replace listeners, interactability, or nested dice/reward controls.
            foreach (var button in reading.Cast<Transform>()
                .Select(value => value.GetComponent<Button>()).Where(value => value != null))
            {
                RuntimeUi.SetLayout(button, preferredHeight: 64f).minHeight = 64f;
                ConfigureAuthoredCompactText076(button.GetComponentInChildren<Text>(true), 18, 25);
            }
        }
    }
}
