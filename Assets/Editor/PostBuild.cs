#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;

public class PostBuild : MonoBehaviour
{
    public int callbackOrder => 0;
    [MenuItem("Build")]
    public static void Build()
    {
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        string buildPath = "Builds/WindowsServer/Server.exe";        
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64; 
        buildPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server;    
        
        BuildPlayerOptions serverPlayerOptions = new BuildPlayerOptions();
        string serverPath = "Builds/WindowsServer/Server.exe";        
        serverPlayerOptions.locationPathName = serverPath;
        serverPlayerOptions.target = BuildTarget.StandaloneWindows64; 
        serverPlayerOptions.subtarget = (int)StandaloneBuildSubtarget.Server; 

        BuildReport buildResult = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (buildResult.summary.result == BuildResult.Succeeded)
        {
            BuildReport serverResult = BuildPipeline.BuildPlayer(serverPlayerOptions);
            if (serverResult.summary.result == BuildResult.Succeeded)
            {
                zip();
            }
        }
    }

    public static void zip()
    {
        string zip_path = "Builds.zip";
        if (File.Exists(zip_path))
        {
            File.Delete(zip_path);
        }
        ZipFile.CreateFromDirectory("Builds", zip_path);
    }
}
#endif
