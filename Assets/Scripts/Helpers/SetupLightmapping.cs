using UnityEngine;
using UnityEditor;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

#if UNITY_EDITOR
public class SetupLightmapping : EditorWindow
{
    [MenuItem("Tools/Setup Lightmapping On All Objects")]
    static void SetupAll()
    {
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
        int count = 0;

        foreach (MeshRenderer r in renderers)
        {
            Undo.RecordObject(r.gameObject, "Setup Lightmapping");
            Undo.RecordObject(r, "Setup Lightmapping");

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            flags |= StaticEditorFlags.ContributeGI;
            GameObjectUtility.SetStaticEditorFlags(r.gameObject, flags);

            r.receiveGI = ReceiveGI.Lightmaps;

            r.shadowCastingMode = ShadowCastingMode.On;

            EditorUtility.SetDirty(r.gameObject);
            EditorUtility.SetDirty(r);

}
        Debug.Log($"Setup lightmapping on {count} objects");
    }
}
#endif
