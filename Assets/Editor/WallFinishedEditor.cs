using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(WallFinished))]
public class WallFinishedEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WallFinished wf = (WallFinished)target;
        string buildType = DetectBuildType(wf.gameObject);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build Type", buildType, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Merge Children & Save Prefab"))
            SaveMergedPrefab(wf, buildType);

        if (GUILayout.Button("Assign Existing Prefab"))
            AssignExistingPrefab(wf);
    }

    private string DetectBuildType(GameObject go)
    {
        if (go.CompareTag("Wall"))  return "Wall";
        if (go.CompareTag("Floor")) return "Floor";
        if (go.CompareTag("Ramp"))  return "Ramp";
        if (go.transform.parent != null)
        {
            var p = go.transform.parent;
            if (p.CompareTag("Wall"))  return "Wall";
            if (p.CompareTag("Floor")) return "Floor";
            if (p.CompareTag("Ramp"))  return "Ramp";
        }
        return "Unknown";
    }

    private void AssignExistingPrefab(WallFinished wf)
    {
        string abs = EditorUtility.OpenFilePanel("Select Finished Prefab", Application.dataPath + "/Prefabs/Builds", "prefab");
        if (string.IsNullOrEmpty(abs)) return;

        string rel = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not load prefab.\nMake sure it is inside the Assets folder.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(wf);
        so.FindProperty("completedPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(wf);
        Debug.Log($"Assigned '{prefab.name}' as completedPrefab on {wf.gameObject.name}");
    }

    private void SaveMergedPrefab(WallFinished wf, string buildType)
    {
        Animator anim = wf.GetComponentInChildren<Animator>(true);
        bool animWasEnabled = anim != null && anim.enabled;

        // Scrub the animator to the exact end of the build animation so baked
        // transforms match what's visible when OnAnimationComplete fires.
        // Update(1f) is needed — Update(0f) evaluates no time and leaves pose unchanged.
        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("LogsWoodVertWholeBuildAnimation", 0, 1f);
            anim.Update(1f / 60f);
        }

        // Bake verts into wf-local space. At runtime the prefab is placed as a
        // child of wf at localPosition/Rotation/Scale identity, so verts land
        // exactly where they appeared during the animation.
        Matrix4x4 rootInv = wf.transform.worldToLocalMatrix;

        MeshFilter[] meshFilters = wf.GetComponentsInChildren<MeshFilter>(true);
        var byMaterial = new Dictionary<Material, List<CombineInstance>>();

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.gameObject == wf.gameObject) continue;
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mf.sharedMesh == null) continue;

            Vector3 ls = mf.transform.lossyScale;
            if (Mathf.Abs(ls.x) < 0.001f || Mathf.Abs(ls.y) < 0.001f || Mathf.Abs(ls.z) < 0.001f)
                continue;

            Matrix4x4 childToRoot = rootInv * mf.transform.localToWorldMatrix;

            Material[] mats = mr.sharedMaterials;
            if (mats.Length == 0) continue;

            for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
            {
                Material mat = sub < mats.Length ? mats[sub] : mats[0];
                if (mat == null) continue;
                if (!byMaterial.ContainsKey(mat))
                    byMaterial[mat] = new List<CombineInstance>();

                byMaterial[mat].Add(new CombineInstance
                {
                    mesh         = mf.sharedMesh,
                    subMeshIndex = sub,
                    transform    = childToRoot
                });
            }
        }

        if (anim != null) anim.enabled = animWasEnabled;

        if (byMaterial.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "No child meshes found to combine.", "OK");
            return;
        }

        string defaultName = buildType != "Unknown" ? buildType + "Finished" : "BuildFinished";
        string path = EditorUtility.SaveFilePanelInProject(
            "Save Finished Build Prefab", defaultName, "prefab",
            "Choose where to save the finished prefab",
            "Assets/Prefabs/Builds");

        if (string.IsNullOrEmpty(path)) return;

        var perMatMeshes  = new List<Mesh>();
        var materials     = new List<Material>();
        var finalCombines = new List<CombineInstance>();

        foreach (var kvp in byMaterial)
        {
            Mesh m = new Mesh();
            m.CombineMeshes(kvp.Value.ToArray(), true, true);
            perMatMeshes.Add(m);
            materials.Add(kvp.Key);
            finalCombines.Add(new CombineInstance
            {
                mesh         = m,
                subMeshIndex = 0,
                transform    = Matrix4x4.identity
            });
        }

        Mesh merged = new Mesh();
        merged.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        merged.CombineMeshes(finalCombines.ToArray(), false, false);
        merged.RecalculateBounds();
        merged.name = defaultName + "_Mesh";

        foreach (var m in perMatMeshes) Object.DestroyImmediate(m);

        string meshPath = path.Replace(".prefab", "_Mesh.asset");
        AssetDatabase.CreateAsset(merged, meshPath);

        GameObject go = new GameObject(defaultName);
        go.SetActive(true);
        go.AddComponent<MeshFilter>().sharedMesh = merged;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials.ToArray();
        renderer.enabled = true;
        go.AddComponent<MeshCollider>().sharedMesh = merged;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();

        GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (savedPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Prefab was not saved correctly. completedPrefab was not assigned.", "OK");
            return;
        }

        Debug.Log($"Saved '{defaultName}' → {path}  ({merged.vertexCount} verts, {materials.Count} material(s))");

        if (EditorUtility.DisplayDialog("Assign?",
            $"Assign '{defaultName}' as the completedPrefab on {wf.gameObject.name}?", "Yes", "No"))
        {
            SerializedObject so = new SerializedObject(wf);
            so.FindProperty("completedPrefab").objectReferenceValue = savedPrefab;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wf);
        }
    }
}
