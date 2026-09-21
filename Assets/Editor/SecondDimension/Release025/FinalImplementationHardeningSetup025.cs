#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondDimension.Presentation.Release025;

namespace SecondDimension.Editor.Release025
{
    public static class FinalImplementationHardeningSetup025
    {
        private const string ScenePath = "Assets/Scenes/Dev/FinalImplementationHardening025.unity";

        [MenuItem("Second Dimension/Final Hardening 025/Prepare Hardening Smoke Scene")]
        public static void Prepare()
        {
            FinalImplementationHardeningValidation025.Validate();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FINAL IMPLEMENTATION HARDENING DASHBOARD 025");
            root.AddComponent<ImplementationHardeningDashboard025>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Final Implementation Hardening 025 smoke scene prepared at " + ScenePath);
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
