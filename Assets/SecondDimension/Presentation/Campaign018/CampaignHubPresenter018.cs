using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SecondDimension.Presentation.Campaign018
{
    public sealed class CampaignHubPresenter018 : MonoBehaviour
    {
        [SerializeField] Image mapImage;
        [SerializeField] Text titleText;
        [SerializeField] Text bodyText;
        [SerializeField] Text statusText;
        CampaignRegistry018 _registry;

        public void Initialize()
        {
            _registry = CampaignRegistry018.LoadFromResources();
            ShowArc(_registry.Arcs.Values.OrderBy(x => x.order).First().id);
        }

        public void ShowArc(string arcId)
        {
            if (_registry == null) Initialize();
            if (!_registry.Arcs.TryGetValue(arcId, out var arc)) return;
            titleText.text = arc.name;
            bodyText.text = $"{arc.chapterIds.Length} chapters\n{arc.completionOutcome}";
            statusText.text = arc.canonStatus;
            var chapter = _registry.Chapters[arc.chapterIds[0]];
            if (chapter.mapIds != null && chapter.mapIds.Length > 0 && _registry.Maps.TryGetValue(chapter.mapIds[0], out var map))
            {
                var sprite = Resources.Load<Sprite>(map.resourcePath);
                if (mapImage != null) mapImage.sprite = sprite;
            }
        }
    }
}
