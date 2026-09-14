#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build entry points for GameCI (.github/workflows/gameci.yml).
// GameCI passes the output file as -customBuildPath; a failed build exits
// non-zero so the workflow run fails instead of uploading an empty artifact.
public static class CIBuild
{
    public static void BuildWindows()
    {
        Build(StandaloneBuildSubtarget.Player, "build/Client/Client.exe");
    }

    public static void BuildServer()
    {
        Build(StandaloneBuildSubtarget.Server, "build/Server/Server.exe");
    }

    private static void Build(StandaloneBuildSubtarget subtarget, string defaultPath)
    {
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = PostBuild.Scenes,
            target = BuildTarget.StandaloneWindows64,
            subtarget = (int)subtarget,
            locationPathName = GetArg("-customBuildPath") ?? defaultPath,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("CI build failed: " + report.summary.result);
            EditorApplication.Exit(1);
        }
    }

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }
}
#endif
