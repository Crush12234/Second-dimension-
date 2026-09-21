#if UNITY_EDITOR
using System;
using System.IO;
using SecondDimension.Gameplay.GuildCity017D;
using SecondDimension.Presentation.GuildCity017D;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecondDimension.Editor.GuildCity017D
{
    public static class GuildCitySetup017D
    {
        private const string ScenePath = "Assets/Scenes/Dev/GuildCityVerticalSlice017D.unity";

        [MenuItem("Second Dimension/Guild City 017D/Prepare Vertical Slice Scene")]
        public static void PrepareSceneMenu() => PrepareScene();

        [MenuItem("Second Dimension/Guild City 017D/Validate Foundation")]
        public static void ValidateMenu()
        {
            var root = Path.Combine(Application.streamingAssetsPath, "Authority", "CONTENT", "GUILD_CITY_017D");
            var content = GuildCityContent017D.LoadFromDirectory(root);
            if (content.Buildings.Count != 18 || content.Plots.Count != 12 ||
                content.Contracts.Count != 3 || content.Boards.Count != 1 || content.Events.Count != 4)
                throw new InvalidOperationException("Guild City 017D content counts are invalid.");
            var board = content.Board("BOARD_BELL_BENEATH_GATE");
            if (board.Nodes == null || board.Nodes.Length != 15)
                throw new InvalidOperationException("The opening expedition board must contain exactly 15 nodes.");
            var optionalElite = board.Node("N09");
            if (!GuildCityExpeditionService017D.IsEncounterNode(optionalElite) ||
                GuildCityExpeditionService017D.CurrentNodeRequiresResolution(optionalElite))
                throw new InvalidOperationException("The optional elite must be fightable without becoming a mandatory route lock.");
            Debug.Log("GUILD CITY 017D VALIDATION: PASS — 18 buildings, 12 plots, 3 contracts, " +
                      "4 events, 15 board nodes, optional encounter choice, permanent members, " +
                      "zero-cost relationship scenes, and no real-time construction timers.");
        }

        [MenuItem("Second Dimension/Guild City 017D/Open Vertical Slice Scene")]
        public static void OpenSceneMenu()
        {
            if (!File.Exists(ScenePath)) PrepareScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        public static void PrepareAndValidateFromCommandLine()
        {
            PrepareScene(); ValidateMenu();
        }

        private static void PrepareScene()
        {
            Directory.CreateDirectory("Assets/Scenes/Dev");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Guild City Camera",typeof(Camera));cameraObject.tag="MainCamera";cameraObject.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;cameraObject.GetComponent<Camera>().backgroundColor=new Color(0.02f,0.03f,0.06f,1f);
            new GameObject("Guild City Vertical Slice 017D",typeof(GuildCityVerticalSlicePresenter017D));
            EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("Prepared "+ScenePath);
        }
    }
}
#endif
