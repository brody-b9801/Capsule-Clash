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
    // Shared with CIBuild so local and CI builds ship the same scenes
    public static readonly string[] Scenes = { "Assets/Scenes/CombatScene.unity" }; //Change when boss scene added

    [MenuItem("Build/Windows Client Server")]
    public static void Build()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        string buildPath = "Builds/WindowsClient/Client.exe";
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.scenes = Scenes;

        BuildPlayerOptions serverPlayerOptions = new BuildPlayerOptions();
        string serverPath = "Builds/LinuxServer/Server.exe";
        serverPlayerOptions.locationPathName = serverPath;
        serverPlayerOptions.scenes = Scenes;
        serverPlayerOptions.target = BuildTarget.StandaloneLinux64; 
        serverPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server; 

        bool startTargetLinux = EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneLinux64;
        
        if (startTargetLinux) {
            BuildServer(ref serverPlayerOptions);
            BuildClient(ref buildPlayerOptions);    
        } else {
            BuildClient(ref buildPlayerOptions);
            BuildServer(ref serverPlayerOptions);      
        }
    }

    public static void BuildClient(ref BuildPlayerOptions buildPlayerOptions)
    {
        BuildReport buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (buildResult.summary.result == BuildResult.Succeeded)
        {
            //Ship to steam
        } else {
            UnityEngine.Debug.LogError("Client build failed: " + buildResult.summary.result);
        }
    }

    public static void BuildServer(ref BuildPlayerOptions serverPlayerOptions)
    {
        BuildReport serverResult = BuildPipeline.BuildPlayer(serverPlayerOptions);
        if (serverResult.summary.result == BuildResult.Succeeded)
            Zip("Builds/LinuxServer");
            //Upload to R2
        else
            UnityEngine.Debug.LogError("Server build failed: " + serverResult.summary.result);
    }

    public static void Zip(string path)
    {
        string zip_path = "Builds.zip";
        if (File.Exists(zip_path))
        {
            File.Delete(zip_path);
        }
        ZipFile.CreateFromDirectory(path, zip_path, System.IO.Compression.CompressionLevel.Optimal, true);
    }
}
#endif
