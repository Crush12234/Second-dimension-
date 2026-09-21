#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Editor.Campaign018
{
    public static class Campaign018ValidationMenu
    {
        [MenuItem("Second Dimension/Campaign 018/Validate Campaign Content")]
        public static void Validate()
        {
            try
            {
                var registry = SecondDimension.Presentation.Campaign018.CampaignRegistry018.LoadFromResources();
                Debug.Log($"CAMPAIGN 018 VALIDATION PASS — Worlds {registry.Worlds.Count}, Arcs {registry.Arcs.Count}, Chapters {registry.Chapters.Count}, Maps {registry.Maps.Count}, Sieges {registry.Sieges.Count}, Bosses {registry.Bosses.Count}");
            }
            catch (Exception ex) { Debug.LogException(ex); throw; }
        }

        public static void ValidateFromCommandLine()
        {
            try { Validate(); EditorApplication.Exit(0); }
            catch { EditorApplication.Exit(1); }
        }
    }
}
#endif
