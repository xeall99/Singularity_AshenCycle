using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    [MenuItem("Build/Windows Standalone (Dev)")]
    public static void BuildWindowsDev()
    {
        string[] scenes = new string[]
        {
            "Assets/Project/Scenes/MainMenu.unity",
            "Assets/Project/Scenes/Battle.unity"
        };

        string buildPath = System.Environment.GetEnvironmentVariable("SYNGRAVA_BUILD_OUTPUT");
        if (string.IsNullOrEmpty(buildPath))
        {
            buildPath = "Builds/Windows/SYNGRAVA.exe";
        }

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        Debug.Log($"[BuildScript] Starting Windows x86_64 Development Build to: {buildPath}...");
        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] BUILD SUCCEEDED — Size: {summary.totalSize:N0} bytes, Time: {summary.totalTime.TotalSeconds:F2}s, Warnings: {summary.totalWarnings}, Errors: {summary.totalErrors}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }
        else
        {
            Debug.LogError($"[BuildScript] BUILD FAILED — Result: {summary.result}, Errors: {summary.totalErrors}, Warnings: {summary.totalWarnings}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
