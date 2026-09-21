using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation
{
    public sealed partial class M1FlowPresenter
    {
        private static Texture2D _loopWaystationArt165;
        private static bool _loopArtAttempted165;

        private static Texture2D LoopWaystationArt165()
        {
            if (_loopArtAttempted165) return _loopWaystationArt165;
            _loopArtAttempted165 = true;
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, "SecondDimension", "Art165", "LOOP_WAYSTATION165.png");
                if (!File.Exists(path)) return null;
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true))
                { UnityEngine.Object.Destroy(texture); return null; }
                texture.name = "Waystation illustrated loop hub165";
                texture.wrapMode = TextureWrapMode.Clamp;
                _loopWaystationArt165 = texture;
            }
            catch (Exception exception) { Debug.LogWarning("Waystation art unavailable: " + exception.Message); }
            return _loopWaystationArt165;
        }

        private void BuildIllustratedLoopHub165(RectTransform safe)
        {
            var art = LoopWaystationArt165();
            if (art != null)
            {
                var backdrop = RuntimeUi.AddStretchRect(_loopModal164.transform, "Waystation backdrop165");
                backdrop.SetAsFirstSibling();
                var image = backdrop.gameObject.AddComponent<RawImage>();
                image.texture = art; image.raycastTarget = false;
                var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = art.width / (float)art.height;
                var shade = RuntimeUi.AddPanel(backdrop, "Waystation atmospheric shade165", new Color(.015f,.025f,.055f,.36f));
                Stretch(shade.rectTransform); shade.raycastTarget = false;
            }
            var title = RuntimeUi.AddText(safe, "Loop menu title164", "THE WAYSTATION", 40,
                TextAnchor.MiddleLeft, new Color(1f,.90f,.65f), FontStyle.Bold);
            var subtitle = RuntimeUi.AddText(safe, "Loop menu subtitle165", "Choose your next adventure", 26,
                TextAnchor.MiddleLeft, RuntimeUi.Text);
            var close = LoopButton164(safe, "Loop close164", "RETURN ×", () => CloseLoopMenu164(true));
            var ids = new[] { "CAMPAIGN", "TOWER", "TITANS", "TOWN" };
            var names = new[] { "CAMPAIGN", "ENDLESS TOWER", "TITAN TRIALS", "YOUR TOWN" };
            var details = new[] { "The main card adventure", "Battle · Climb · Idle", "Bosses & SSR heroes", "Build · Trade · Grow" };
            var crops = new[] { new Rect(.035f,.24f,.31f,.51f), new Rect(.04f,.49f,.31f,.51f),
                new Rect(.65f,.49f,.31f,.51f), new Rect(.65f,.13f,.31f,.51f) };
            var main = new Button[4];
            for (var index = 0; index < ids.Length; index++)
            {
                var id = ids[index];
                var button = LoopButton164(safe, "Loop destination " + id + "164", string.Empty,
                    () => OpenLoopDestination164(id), new Color(.10f,.15f,.23f,.92f));
                main[index] = button;
                var face = (RectTransform)button.transform;
                if (art != null)
                {
                    var picture = RuntimeUi.AddStretchRect(face, "Loop " + id + " artwork165");
                    picture.SetAsFirstSibling();
                    var image = picture.gameObject.AddComponent<RawImage>();
                    image.texture = art; image.uvRect = crops[index]; image.raycastTarget = false;
                    picture.gameObject.AddComponent<LoopArtCover165>().Configure165(image, crops[index]);
                }
                var caption = RuntimeUi.AddPanel(face, "Loop " + id + " caption165", new Color(.015f,.025f,.05f,.91f));
                LoopRect164(caption.rectTransform, 0f, 0f, 1f, .47f); caption.raycastTarget = false;
                var heading = RuntimeUi.AddText(face, "Loop tile title165", names[index], 32,
                    TextAnchor.MiddleLeft, new Color(1f,.91f,.69f), FontStyle.Bold);
                LoopRect164(heading.rectTransform, .07f, .28f, .86f, .15f);
                var detail = RuntimeUi.AddText(face, "Loop tile detail165", details[index], 24,
                    TextAnchor.MiddleLeft, RuntimeUi.Text);
                LoopRect164(detail.rectTransform, .07f, .13f, .86f, .15f);
                var status = RuntimeUi.AddText(face, "Loop tile status165", (_coordinator as M1RuntimeCoordinator)?.LoopStatus165(id) ?? "READY",
                    20, TextAnchor.MiddleLeft, RuntimeUi.Accent, FontStyle.Bold);
                LoopRect164(status.rectTransform, .07f, .01f, .86f, .12f);
                var border = button.gameObject.AddComponent<Outline>();
                border.effectColor = CurrentLoopDestination164() == id ? RuntimeUi.Accent : new Color(.55f,.48f,.32f,.8f);
                border.effectDistance = new Vector2(2f,-2f);
                foreach (var text in button.GetComponentsInChildren<Text>()) text.raycastTarget = false;
            }
            var services = new[] { "UNIONS", "HEROES", "TRAINING", "FORGE", "MERCHANTS", "RECRUITMENT", "HALL" };
            var serviceLabels = new[] { "BUILD UNIONS", "HEROES & GEAR", "HERO TRAINING", "BLACKSMITH", "MERCHANTS", "RECRUIT HEROES", "GUILD HALL" };
            var serviceButtons = new Button[8];
            for (var index = 0; index < services.Length; index++)
            {
                var id = services[index];
                serviceButtons[index] = LoopButton164(safe, "Loop destination " + id + "164", serviceLabels[index],
                    () => OpenLoopDestination164(id), new Color(.018f,.038f,.065f,.93f));
            }
            serviceButtons[7] = LoopButton164(safe, "Loop return previous165", "← PREVIOUS VIEW", BackThroughLoops164,
                new Color(.08f,.13f,.18f,.94f));
            _loopNotice164 = RuntimeUi.AddText(safe, "Loop switch notice164", "Each adventure keeps its place. Your Guild grows across every loop.", 26,
                TextAnchor.MiddleCenter, RuntimeUi.Text);
            _loopNotice164.raycastTarget = false;
            safe.gameObject.AddComponent<LoopHubLayout165>().Configure165(title, subtitle, close, main, serviceButtons, _loopNotice164);
            close.Select();
        }
    }

    internal sealed class LoopArtCover165 : MonoBehaviour
    {
        private RawImage _image;
        private Rect _region;
        public void Configure165(RawImage image, Rect region) { _image=image; _region=region; }
        private void LateUpdate()
        {
            if (_image?.texture == null) return;
            var size=((RectTransform)transform).rect.size;
            if (size.x<=0f || size.y<=0f) return;
            var targetAspect=size.x/size.y;
            var regionAspect=_region.width*_image.texture.width/(_region.height*_image.texture.height);
            var crop=_region;
            if (targetAspect<regionAspect)
            {
                crop.width=_region.width*targetAspect/regionAspect;
                crop.x=_region.center.x-crop.width*.5f;
            }
            else
            {
                crop.height=_region.height*regionAspect/targetAspect;
                crop.y=_region.center.y-crop.height*.5f;
            }
            _image.uvRect=crop;
        }
    }

    internal sealed class LoopHubLayout165 : MonoBehaviour
    {
        private Text _title, _subtitle, _notice;
        private Button _close;
        private Button[] _main, _services;

        public void Configure165(Text title, Text subtitle, Button close, Button[] main, Button[] services, Text notice)
        { _title=title; _subtitle=subtitle; _close=close; _main=main; _services=services; _notice=notice; }

        private void LateUpdate()
        {
            if (_title == null) return;
            var rect=(RectTransform)transform;
            var scale=Mathf.Max(.01f,GetComponentInParent<Canvas>()?.scaleFactor??1f);
            var width=rect.rect.width*scale;
            var height=rect.rect.height*scale;
            var margin=width<900f?12f:28f;
            var gap=width<900f?8f:14f;
            var columns=width<620f?2:4;
            var tileWidth=(width-margin*2f-gap*(columns-1))/columns;
            var serviceRows=Mathf.CeilToInt(8f/columns);
            var serviceHeight=44f;
            var serviceTop=26f+serviceRows*(serviceHeight+gap);
            var headerHeight=58f;
            var mainRows=4/columns;
            var mainHeight=Mathf.Max(88f,(height-headerHeight-serviceTop-gap*(mainRows+1))/mainRows);
            Pixel165(_title.rectTransform,margin,height-31f,width-150f-margin,25f,scale);
            Pixel165(_subtitle.rectTransform,margin,height-53f,width-150f-margin,20f,scale);
            Pixel165((RectTransform)_close.transform,width-margin-112f,height-51f,112f,44f,scale);
            Font165(_title,21f,scale);Font165(_subtitle,12f,scale);Font165(_close.GetComponentInChildren<Text>(),13f,scale);
            for(var i=0;i<_main.Length;i++)
            {
                Pixel165((RectTransform)_main[i].transform,margin+(i%columns)*(tileWidth+gap),
                    height-headerHeight-(i/columns+1)*(mainHeight+gap),tileWidth,mainHeight,scale);
                foreach(var text in _main[i].GetComponentsInChildren<Text>())
                    Font165(text,text.name=="Loop tile title165"?(width<900f?14f:21f):text.name=="Loop tile status165"?10f:12f,scale);
            }
            for(var i=0;i<_services.Length;i++)
            {
                Pixel165((RectTransform)_services[i].transform,margin+(i%columns)*(tileWidth+gap),
                    serviceTop-(i/columns+1)*(serviceHeight+gap),tileWidth,serviceHeight,scale);
                Font165(_services[i].GetComponentInChildren<Text>(),width<900f?12f:15f,scale);
            }
            Pixel165(_notice.rectTransform,margin,2f,width-2f*margin,20f,scale);Font165(_notice,width<900f?10f:12f,scale);
        }
        private static void Pixel165(RectTransform rect,float x,float y,float width,float height,float scale)
        {rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;rect.anchoredPosition=new Vector2(x,y)/scale;rect.sizeDelta=new Vector2(width,height)/scale;}
        private static void Font165(Text text,float pixels,float scale)
        {if(text==null)return;text.resizeTextForBestFit=false;text.fontSize=Mathf.CeilToInt(pixels/scale);text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
    }
}
