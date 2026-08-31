#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;

public class PostBuild : IPostprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
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
