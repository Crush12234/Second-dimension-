#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondDimension.Presentation.Release023;

namespace SecondDimension.Editor.Release023
{
    public static class FinalReleaseSetup023
    {
        const string ScenePath = "Assets/Scenes/Dev/FinalGameSmoke023.unity";
        [MenuItem("Second Dimension/Final Release 023/Prepare Smoke Scene")]
        public static void Prepare()
        {
            FinalReleaseValidation023.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FINAL GAME SMOKE DASHBOARD 023");
            root.AddComponent<FinalGameSmokeDashboard023>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Final Game Smoke 023 scene prepared at " + ScenePath);
        }
        public static void PrepareFromCommandLine()
        {
            try { Prepare(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
    }
}
#endif
