#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondDimension.Presentation.Release026;

namespace SecondDimension.Editor.Release026
{
    public static class FinalExecutionSetup026
    {
        private const string ScenePath = "Assets/Scenes/Dev/FinalExecutionReadiness026.unity";

        [MenuItem("Second Dimension/Final Execution 026/Prepare Readiness Scene")]
        public static void Prepare()
        {
            FinalExecutionValidation026.Validate();
            var project = Directory.GetParent(Application.dataPath).FullName;
            Directory.CreateDirectory(Path.Combine(project, Path.GetDirectoryName(ScenePath)));
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("FINAL EXECUTION READINESS 026").AddComponent<FinalExecutionDashboard026>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Final Execution 026 readiness scene prepared: " + ScenePath);
        }

        public static void PrepareFromCommandLine()
        {
            try { Prepare(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }
    }
}
#endif
