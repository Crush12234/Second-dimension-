#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondDimension.Presentation.Release024;

namespace SecondDimension.Editor.Release024
{
    public static class FullGameIntegrationSetup024
    {
        private const string ScenePath = "Assets/Scenes/Dev/FinalGameIntegration024.unity";

        [MenuItem("Second Dimension/Final Integration 024/Prepare Integration Smoke Scene")]
        public static void Prepare()
        {
            FullGameIntegrationValidation024.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FINAL GAME + WORLD GATE DASHBOARD 024");
            root.AddComponent<FullGameIntegrationDashboard024>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Final Integration 024 smoke scene prepared at " + ScenePath);
        }

        public static void PrepareFromCommandLine()
        {
            try
            {
                Prepare();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
