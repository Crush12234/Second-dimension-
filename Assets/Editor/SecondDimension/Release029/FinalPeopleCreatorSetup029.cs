#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SecondDimension.Presentation.Release029;

namespace SecondDimension.Editor.Release029
{
    public static class FinalPeopleCreatorSetup029
    {
        public const string SmokeScene="Assets/Scenes/Dev/FinalPeopleCreatorSmoke029.unity";
        [MenuItem("Second Dimension/Final People Creator 029/Prepare Smoke Scene")]
        public static void Prepare()
        {
            FinalPeopleCreatorValidation029.Validate();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("FINAL_PEOPLE_CREATOR_SMOKE_029");
            root.AddComponent<PeopleCreatorSmoke029>();
            System.IO.Directory.CreateDirectory("Assets/Scenes/Dev");
            EditorSceneManager.SaveScene(scene,SmokeScene);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("029 smoke scene prepared: "+SmokeScene);
        }
        public static void PrepareFromCommandLine(){try{Prepare();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
    }
}
#endif
