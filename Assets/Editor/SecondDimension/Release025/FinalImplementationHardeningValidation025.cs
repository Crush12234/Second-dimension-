#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using SecondDimension.Presentation.Release025;

namespace SecondDimension.Editor.Release025
{
    public static class FinalImplementationHardeningValidation025
    {
        [MenuItem("Second Dimension/Final Hardening 025/Validate Complete Project")]
        public static void Validate()
        {
            SecondDimension.Editor.Release024.FullGameIntegrationValidation024.Validate();
            var registry = ImplementationHardeningRegistry025.LoadFromResources();
            var snapshot = ImplementationReadinessService025.BuildSnapshot();
            if (!snapshot.IsReady)
                throw new InvalidOperationException(snapshot.Error);

            var project = Directory.GetParent(Application.dataPath).FullName;
            ValidatePackages(project);
            ValidateNoEmbeddedFonts(project);
            ValidateEditorBuildSettings(project);
            ValidateRuntimeEditorBoundary(project);
            ValidateAssemblyDefinitions(project);

            Debug.Log(
                "FINAL IMPLEMENTATION HARDENING 025 VALIDATION PASS\n" +
                string.Join("\n", snapshot.SummaryLines));
        }

        public static void ValidateFromCommandLine()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidatePackages(string project)
        {
            var path = Path.Combine(project, "Packages", "manifest.json");
            var root = JObject.Parse(File.ReadAllText(path));
            var dependencies = (JObject)root["dependencies"];
            if (dependencies == null)
                throw new InvalidOperationException("Packages/manifest.json has no dependencies object.");
            foreach (var required in global::SecondDimension.Presentation.Release030.ReleaseSupersessionRegistry030.Contract.requiredPackages)
                if (dependencies[required] == null)
                    throw new InvalidOperationException("Missing package: " + required);
            foreach (var forbidden in global::SecondDimension.Presentation.Release030.ReleaseSupersessionRegistry030.Contract.forbiddenPackages)
                if (dependencies[forbidden] != null)
                    throw new InvalidOperationException("Removed package returned: " + forbidden);
        }

        private static void ValidateNoEmbeddedFonts(string project)
        {
            var extensions = new[] { ".ttf", ".otf", ".woff", ".woff2" };
            var found = Directory.GetFiles(
                    Path.Combine(project, "Assets"),
                    "*",
                    SearchOption.AllDirectories)
                .Where(path => extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
                .ToArray();
            if (found.Length > 0)
                throw new InvalidOperationException(
                    "Embedded font binaries are forbidden in the 025 delivery:\n" + string.Join("\n", found));
            if (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") == null)
                throw new InvalidOperationException("Unity built-in runtime font is unavailable.");
        }

        private static void ValidateEditorBuildSettings(string project)
        {
            var text = File.ReadAllText(Path.Combine(project, "ProjectSettings", "EditorBuildSettings.asset"));
            if (text.IndexOf("com.unity.dt.app-ui", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new InvalidOperationException("Stale App UI editor configuration remains after package pruning.");
            if (text.IndexOf("Assets/Scenes/Boot.unity", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Boot scene is not enabled in EditorBuildSettings.");
        }

        private static void ValidateRuntimeEditorBoundary(string project)
        {
            var assets = Path.Combine(project, "Assets");
            foreach (var path in Directory.GetFiles(assets, "*.cs", SearchOption.AllDirectories))
            {
                var normalized = path.Replace('\\', '/');
                if (normalized.IndexOf("/Editor/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalized.IndexOf("/Tests/", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                var text = File.ReadAllText(path);
                if (text.IndexOf("UnityEditor", StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException("Runtime source references UnityEditor: " + path);
            }
        }

        private static void ValidateAssemblyDefinitions(string project)
        {
            var asmdefs = Directory.GetFiles(Path.Combine(project, "Assets"), "*.asmdef", SearchOption.AllDirectories);
            var names = new HashSet<string>(
                asmdefs.Select(path =>
                {
                    var root = JObject.Parse(File.ReadAllText(path));
                    return (string)root["name"];
                }).Where(value => !string.IsNullOrWhiteSpace(value)),
                StringComparer.Ordinal);
            foreach (var path in asmdefs)
            {
                var root = JObject.Parse(File.ReadAllText(path));
                var references = root["references"] as JArray;
                if (references == null) continue;
                foreach (var reference in references.Values<string>())
                {
                    if (string.IsNullOrWhiteSpace(reference) ||
                        reference.StartsWith("Unity.", StringComparison.Ordinal) ||
                        reference.StartsWith("GUID:", StringComparison.Ordinal))
                        continue;
                    if (!names.Contains(reference))
                        throw new InvalidOperationException(
                            "Assembly definition reference is missing: " + reference + " from " + path);
                }
            }
        }
    }
}
#endif
