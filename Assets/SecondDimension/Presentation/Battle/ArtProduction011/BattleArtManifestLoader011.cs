using System; using System.Collections.Generic; using UnityEngine;
namespace SecondDimension.Presentation.Battle.ArtProduction011 {
public static class BattleArtManifestLoader011 {
 public const string ResourcePath="SecondDimension/Art/Generated011/battle_art_manifest_011"; static BattleArtManifest011 cache; static Dictionary<string,CharacterPoseSet011> chars;
 public static BattleArtManifest011 Load(){ if(cache!=null)return cache; var t=Resources.Load<TextAsset>(ResourcePath); if(t==null)throw new InvalidOperationException("Missing "+ResourcePath); cache=JsonUtility.FromJson<BattleArtManifest011>(t.text); chars=new Dictionary<string,CharacterPoseSet011>(StringComparer.Ordinal); foreach(var c in cache.characters) chars[c.stableId]=c; return cache; }
 public static PoseAsset011 Pose(string stableId,BattlePoseId011 id){ Load(); string key=Key(id); if(!chars.TryGetValue(stableId,out var c))throw new KeyNotFoundException(stableId); foreach(var p in c.poses)if(p.poseId==key)return p; throw new KeyNotFoundException(stableId+"/"+key); }
 public static string Key(BattlePoseId011 id){ switch(id){case BattlePoseId011.IdleReady:return "IDLE_READY";case BattlePoseId011.Anticipation:return "ANTICIPATION";case BattlePoseId011.PrimaryAction:return "PRIMARY_ACTION";case BattlePoseId011.RoleAction:return "ROLE_ACTION";case BattlePoseId011.GuardCastSupport:return "GUARD_CAST_SUPPORT";case BattlePoseId011.HitReaction:return "HIT_REACTION";case BattlePoseId011.Downed:return "DOWNED";case BattlePoseId011.Victory:return "VICTORY";default:throw new ArgumentOutOfRangeException();} }
}}
