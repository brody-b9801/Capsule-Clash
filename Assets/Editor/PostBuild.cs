using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.Diagnostics;
using System.IO.Compression;

public class PostBuild : IPostprocessBuildWithReport
{
    void Start()
    {
        
    }

    void Update()
    {
        
    }
    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        ZipFile.CreateFromDirectory("Builds", "Builds.zip");
    }
}
