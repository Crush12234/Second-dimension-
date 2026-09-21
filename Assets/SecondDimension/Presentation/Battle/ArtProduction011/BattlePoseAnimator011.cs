using System.Collections; using UnityEngine;
namespace SecondDimension.Presentation.Battle.ArtProduction011 {
public sealed class BattlePoseAnimator011:MonoBehaviour {
 [SerializeField] SpriteRenderer target; [SerializeField] Transform motionRoot; [SerializeField] bool reducedMotion; Vector3 home; Coroutine running;
 void Awake(){if(target==null)target=GetComponentInChildren<SpriteRenderer>();if(motionRoot==null)motionRoot=transform;home=motionRoot.localPosition;}
 public bool SetPose(string stableId,BattlePoseId011 pose){var p=BattleArtManifestLoader011.Pose(stableId,pose);var s=Resources.Load<Sprite>(p.resourcesPath);if(s==null||target==null)return false;target.sprite=s;return true;}
 public void PlayResolvedAction(string stableId,BattlePoseId011 action){if(running!=null)StopCoroutine(running);running=StartCoroutine(Run(stableId,action));}
 IEnumerator Run(string id,BattlePoseId011 action){float f=reducedMotion ? .55f : 1f;SetPose(id,BattlePoseId011.Anticipation);yield return new WaitForSecondsRealtime(.18f*f);SetPose(id,action);var peak=home+new Vector3(reducedMotion ? .08f : .32f,.04f,0);yield return Move(home,peak,.20f*f);yield return new WaitForSecondsRealtime(.24f*f);yield return Move(peak,home,.22f*f);SetPose(id,BattlePoseId011.IdleReady);running=null;}
 IEnumerator Move(Vector3 a,Vector3 b,float d){if(d<=0){motionRoot.localPosition=b;yield break;}float e=0;while(e<d){e+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(e/d);motionRoot.localPosition=Vector3.LerpUnclamped(a,b,t*t*(3-2*t));yield return null;}motionRoot.localPosition=b;}
}}
