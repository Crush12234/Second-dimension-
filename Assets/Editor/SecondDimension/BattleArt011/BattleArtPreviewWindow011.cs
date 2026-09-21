#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation;

namespace SecondDimension.Editor
{
    /// <summary>
    /// Lightweight inspection window for the clean Thursday 011 pose/VFX library.
    /// It is an Editor-only review aid and never touches authoritative battle state.
    /// </summary>
    public sealed class BattleArtPreviewWindow011 : EditorWindow
    {
        private int _characterIndex;
        private int _poseIndex;
        private int _profileIndex;
        private Vector2 _scroll;

        [MenuItem("Second Dimension/Battle Art 011/Open Pose and Art Preview", false, 5099)]
        public static void Open()
        {
            var window = GetWindow<BattleArtPreviewWindow011>("Battle Art 011");
            window.minSize = new Vector2(720f, 640f);
            window.Show();
        }

        private void OnEnable()
        {
            BattleArtRuntimeRegistry011.ReloadForTests();
        }

        private void OnGUI()
        {
            BattleArtManifest011 manifest;
            try
            {
                manifest = BattleArtRuntimeRegistry011.Manifest;
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }

            var characters = manifest.characters ?? Array.Empty<BattleArtCharacter011>();
            var profiles = manifest.runtimeArtProfiles ?? Array.Empty<BattleArtProfile011>();
            if (characters.Length == 0)
            {
                EditorGUILayout.HelpBox("No Battle Art 011 characters were loaded.", MessageType.Warning);
                return;
            }

            _characterIndex = Mathf.Clamp(_characterIndex, 0, characters.Length - 1);
            var character = characters[_characterIndex];
            var poses = character.poses ?? Array.Empty<BattleArtPose011>();
            _poseIndex = Mathf.Clamp(_poseIndex, 0, Math.Max(0, poses.Length - 1));

            EditorGUILayout.LabelField("SECOND DIMENSION — THURSDAY BATTLE ART 011", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"{manifest.characterCount} combatants · {manifest.vfxCount} VFX · {manifest.audioCount} audio cues · {manifest.runtimeArtProfileCount} Art bindings");
            EditorGUILayout.Space(8f);

            _characterIndex = EditorGUILayout.Popup("Combatant", _characterIndex,
                characters.Select(value => value.displayName + "  [" + value.memberId + "]").ToArray());
            character = characters[_characterIndex];
            poses = character.poses ?? Array.Empty<BattleArtPose011>();
            _poseIndex = Mathf.Clamp(_poseIndex, 0, Math.Max(0, poses.Length - 1));
            if (poses.Length > 0)
                _poseIndex = EditorGUILayout.Popup("Pose", _poseIndex,
                    poses.Select(value => value.poseId + "  —  " + value.sourceStatus).ToArray());

            var previewRect = GUILayoutUtility.GetRect(520f, 430f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(previewRect, new Color(0.025f, 0.055f, 0.10f, 1f));
            if (poses.Length > 0)
            {
                var pose = poses[_poseIndex];
                var sprite = BattleArtRuntimeRegistry011.LoadSprite(pose.resourcePath);
                if (sprite != null && sprite.texture != null)
                {
                    var uv = new Rect(
                        sprite.textureRect.x / sprite.texture.width,
                        sprite.textureRect.y / sprite.texture.height,
                        sprite.textureRect.width / sprite.texture.width,
                        sprite.textureRect.height / sprite.texture.height);
                    GUI.DrawTextureWithTexCoords(previewRect, sprite.texture, uv, true);
                }
                else
                {
                    GUI.Label(previewRect, "Missing sprite: " + pose.resourcePath, EditorStyles.centeredGreyMiniLabel);
                }

                EditorGUILayout.LabelField("Resource", pose.resourcePath);
                EditorGUILayout.LabelField("Source status", pose.sourceStatus);
                EditorGUILayout.LabelField("Release status", character.releaseStatus);
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Art Presentation Binding", EditorStyles.boldLabel);
            if (profiles.Length > 0)
            {
                _profileIndex = Mathf.Clamp(_profileIndex, 0, profiles.Length - 1);
                _profileIndex = EditorGUILayout.Popup("Profile", _profileIndex,
                    profiles.Select(value => value.displayName + "  [" + value.artId + "]").ToArray());
                var profile = profiles[_profileIndex];
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120f));
                EditorGUILayout.LabelField("Class", profile.artClass);
                EditorGUILayout.LabelField("Weapon family", profile.weaponFamilyId);
                EditorGUILayout.LabelField("School", profile.schoolId);
                EditorGUILayout.LabelField("Motion", profile.motionProfile);
                EditorGUILayout.LabelField("Camera", profile.cameraProfileId);
                EditorGUILayout.LabelField("Trail", profile.trailResourcePath);
                EditorGUILayout.LabelField("Projectile", profile.projectileResourcePath);
                EditorGUILayout.LabelField("Field", profile.fieldResourcePath);
                EditorGUILayout.LabelField("Impact", profile.impactResourcePath);
                EditorGUILayout.LabelField("Start audio", profile.startAudioResourcePath);
                EditorGUILayout.LabelField("Impact audio", profile.impactAudioResourcePath);
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Run Battle Art 011 Validation"))
                BattleArtValidationMenu011.Validate();
        }
    }
}
#endif
