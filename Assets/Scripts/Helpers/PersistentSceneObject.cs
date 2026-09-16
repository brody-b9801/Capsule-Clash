using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Carries a plain scene object into the next scene. Used for the HUD root that the
/// upgradeManager lives on, which the boss scene has no copy of. These are ordinary
/// GameObjects rather than NetworkBehaviours, so DontDestroyOnLoad is allowed;
/// networked objects have to travel via PlayerSceneTransfer instead.
/// </summary>
[DisallowMultipleComponent]
public class PersistentSceneObject : MonoBehaviour {

    private static readonly Dictionary<string, PersistentSceneObject> kept = new Dictionary<string, PersistentSceneObject>();

    private string key;

    /// <summary>
    /// Keeps go alive for the rest of the session. Safe to call every spawn; only the
    /// first object registered under a key is kept, later ones are discarded.
    /// </summary>
    public static void Keep(GameObject go, string key) {
        if (go == null) return;

        if (kept.TryGetValue(key, out PersistentSceneObject existing) && existing != null) {
            if (existing.gameObject == go) return;

            // A later scene brought its own copy; the one already carried over wins so
            // the HUD does not end up rendered twice.
            Debug.Log($"[PersistentSceneObject] dropping duplicate '{go.name}' for key '{key}'.", go);
            Destroy(go);
            return;
        }

        // DontDestroyOnLoad only accepts root objects, and this one is parented under
        // the level contents that the scene load unloads.
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
