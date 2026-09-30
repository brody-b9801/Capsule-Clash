using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PersistentSceneObject : MonoBehaviour {

    private static readonly Dictionary<string, PersistentSceneObject> kept = new Dictionary<string, PersistentSceneObject>();

    private string key;

    public static void Keep(GameObject go, string key) {
        if (go == null) return;

        if (kept.TryGetValue(key, out PersistentSceneObject existing) && existing != null) {
            if (existing.gameObject == go) return;

            Debug.Log($"[PersistentSceneObject] dropping duplicate '{go.name}' for key '{key}'.", go);
            Destroy(go);
            return;
        }

        go.transform.SetParent(null, true);
        DontDestroyOnLoad(go);

        PersistentSceneObject marker = go.GetComponent<PersistentSceneObject>();
        if (marker == null) marker = go.AddComponent<PersistentSceneObject>();
        marker.key = key;
        kept[key] = marker;

        Debug.Log($"[PersistentSceneObject] keeping '{go.name}' across scene loads (key '{key}').", go);
    }

    private void OnDestroy() {
        if (key != null && kept.TryGetValue(key, out PersistentSceneObject current) && current == this)
            kept.Remove(key);
    }
}
