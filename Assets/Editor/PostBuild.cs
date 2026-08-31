#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;

public class PostBuild
{
    [MenuItem("Build/Windows Client Server")]
    public static void Build()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        string buildPath = "Builds/WindowsClient/Client.exe";
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/CombatScene.unity" }; //Change when boss scene added
        
        BuildPlayerOptions serverPlayerOptions = new BuildPlayerOptions();
        string serverPath = "Builds/WindowsServer/Server.exe";        
        serverPlayerOptions.locationPathName = serverPath;
        serverPlayerOptions.scenes = new[] { "Assets/Scenes/CombatScene.unity" };
        serverPlayerOptions.target = BuildTarget.StandaloneLinux64; 
        serverPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server; 

        bool startTargetLinux = EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64;

        BuildReport buildResult = BuildPipeline.BuildPlayer(startTargetLinux ? serverPlayerOptions : buildPlayerOptions);
        if (buildResult.summary.result == BuildResult.Succeeded)
        {
            BuildReport serverResult = BuildPipeline.BuildPlayer(startTargetLinux ? buildPlayerOptions : serverPlayerOptions);
            if (serverResult.summary.result == BuildResult.Succeeded)
            {
                Zip();
            } else {
                UnityEngine.Debug.LogError("Server build failed: " + serverResult.summary.result);
            }
        } else {
            UnityEngine.Debug.LogError("Client build failed: " + buildResult.summary.result);
        }

    }

    public static void Zip()
    {
        string zip_path = "Builds.zip";
        if (File.Exists(zip_path))
        {
            File.Delete(zip_path);
        }
        ZipFile.CreateFromDirectory("Builds", zip_path, System.IO.Compression.CompressionLevel.Optimal, true);
    }
}
#endif
