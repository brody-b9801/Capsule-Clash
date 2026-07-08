#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class GenerateLightmapUVs : EditorWindow
{
    [MenuItem("Tools/Generate Lightmap UVs On Scene Meshes Only")]
    static void Generate()
    {
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
        int count = 0;

        foreach (MeshRenderer r in renderers)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            string path = AssetDatabase.GetAssetPath(mf.sharedMesh);
            if (string.IsNullOrEmpty(path)) continue;

            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) continue;

            if (!importer.generateSecondaryUV)
            {
                importer.generateSecondaryUV = true;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Generated lightmap UVs on {count} models");
    }
}
#endif