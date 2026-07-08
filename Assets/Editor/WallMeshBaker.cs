#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class WallMeshBaker : EditorWindow
{
    private GameObject wallPrefab;

    [MenuItem("Tools/Assign Wall Prefab")]
    static void Open()
    {
        GetWindow<WallMeshBaker>("Assign Wall Prefab");
    }

    void OnGUI()
    {
        GUILayout.Label("Drag in the completed wall prefab, then click Assign to set it on all WallFinished components.", EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space();

        wallPrefab = (GameObject)EditorGUILayout.ObjectField("Completed Wall Prefab", wallPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(wallPrefab == null);
        if (GUILayout.Button("Assign to All WallFinished"))
            AssignToAll(wallPrefab);
        EditorGUI.EndDisabledGroup();
    }

    static void AssignToAll(GameObject prefab)
    {
        int count = 0;

        // Assign on scene instances
        WallFinished[] sceneInstances = Object.FindObjectsOfType<WallFinished>(true);
        foreach (WallFinished wf in sceneInstances)
        {
            SerializedObject so = new SerializedObject(wf);
            SerializedProperty prop = so.FindProperty("completedPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(wf);
                count++;
            }
        }

        // Assign on prefab assets that have WallFinished
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null) continue;

            WallFinished[] wfs = prefabAsset.GetComponentsInChildren<WallFinished>(true);
            foreach (WallFinished wf in wfs)
            {
                SerializedObject so = new SerializedObject(wf);
                SerializedProperty prop = so.FindProperty("completedPrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = prefab;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(wf);
                    count++;
                }
            }
        }

        AssetDatabase.SaveAssets();

        if (count > 0)
            Debug.Log($"Assigned prefab to {count} WallFinished component(s).");
        else
            Debug.LogWarning("No WallFinished components found to assign.");
    }
}
#endif
