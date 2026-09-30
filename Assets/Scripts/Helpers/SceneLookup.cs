using UnityEngine;

public static class SceneLookup
{
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
