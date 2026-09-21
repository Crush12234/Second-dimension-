using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SecondDimension.Presentation
{
    // A presentation-only repair for the damaged native Kenzo pair. The original
    // files, identity, stats, ownership and all other hero mappings remain intact.
    public static class HeroRepair154
    {
        static readonly Dictionary<bool,Sprite> Poses=new Dictionary<bool,Sprite>();
        public static bool TryResolve(string stableId,bool action,out Sprite sprite,out string key)
        {
            sprite=null;key=string.Empty;
            if(stableId!="HERO_REC_254")return false;
            string file=action?"HERO_REC_254_ACTION154":"HERO_REC_254_IDLE154";
            key="SecondDimension/HeroRepair154/"+file+(action?"#ACTION":"#IDLE");
            if(Poses.TryGetValue(action,out sprite))return sprite!=null;
            Texture2D texture=null;Sprite raw=null;
            try
            {
                var path=Path.Combine(Application.streamingAssetsPath,"SecondDimension","HeroRepair154",file+".png");
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name=file,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                if(!ImageConversion.LoadImage(texture,File.ReadAllBytes(path),false)||texture.width<256||texture.height<256)
                    throw new InvalidDataException("Hero repair image is missing or invalid.");
                raw=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.035f),100f,0,SpriteMeshType.FullRect);
                raw.name=file;
                sprite=M1SilhouetteFraming091.FrameResourceSprite091(raw);
                texture.Apply(false,true);
                Poses[action]=sprite;
                return sprite!=null;
            }
            catch(Exception error)
            {
                if(raw!=null)UnityEngine.Object.Destroy(raw);
                if(texture!=null)UnityEngine.Object.Destroy(texture);
                Debug.LogWarning("Kenzo art repair unavailable: "+error.Message);
                sprite=null;key=string.Empty;return false;
            }
        }
    }
}
