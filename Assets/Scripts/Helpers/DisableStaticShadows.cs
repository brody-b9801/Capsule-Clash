#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class DisableStaticShadows : EditorWindow
{
    [MenuItem("Tools/Disable Cast Shadows On Static Objects")]
    static void DisableShadows()
    {
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
        int count = 0;

        foreach (MeshRenderer r in renderers)
        {
            if (GameObjectUtility.GetStaticEditorFlags(r.gameObject)
                .HasFlag(StaticEditorFlags.ContributeGI))
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                count++;
            }
        }

        Debug.Log($"Disabled shadows on {count} static objects");
    }
}
#endif