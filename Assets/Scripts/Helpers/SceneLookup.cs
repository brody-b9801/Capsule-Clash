using UnityEngine;

/// <summary>
/// Scene lookups that also find INACTIVE objects.
///
/// GameObject.Find only searches active objects. Under Alteruna the player
/// spawned early enough that things like CamAKM (the first-person gun, hidden
/// until GunThingAnim.enableGun runs) and UsernameInput happened to be active
/// by the time startup code looked for them.
///
/// With Fish-Net the player spawns on OnStartClient, which is later and has no
/// guaranteed order relative to the UI toggles -- so those Finds return null and
/// throw. FindInactive walks the loaded scene roots instead, including
/// deactivated branches.
///
/// Prefer a [SerializeField] reference where you can; this is for the existing
/// call sites that already rely on name lookup.
/// </summary>
public static class SceneLookup
{
    /// <summary>
    /// Finds a GameObject by name, including inactive ones. Returns null if absent.
    /// </summary>
    public static GameObject FindInactive(string name)
    {
        GameObject active = GameObject.Find(name);
        if (active != null) return active;

        for (int s = 0; s < UnityEngine.SceneManagement.SceneManager.sceneCount; s++)
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                GameObject hit = SearchChildren(root.transform, name);
                if (hit != null) return hit;
            }
        }

        return null;
    }

    private static GameObject SearchChildren(Transform t, string name)
    {
        if (t.gameObject.name == name) return t.gameObject;

        for (int i = 0; i < t.childCount; i++)
        {
            GameObject hit = SearchChildren(t.GetChild(i), name);
            if (hit != null) return hit;
        }

        return null;
    }
}
