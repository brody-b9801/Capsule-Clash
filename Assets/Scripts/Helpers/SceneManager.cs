using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public static class SceneManager {
    public const string CombatScene = "CombatScene";
    public const string BossScene = "BossScene";
    private const float TransferTimeoutSeconds = 15f;

    public static bool InCombatScene { get; private set; } = true;
    private static string pendingTransfer;

    public static event UnityAction<Scene, LoadSceneMode> SceneLoaded {
        add => UnitySceneManager.sceneLoaded += value;
        remove => UnitySceneManager.sceneLoaded -= value;
    }

    public static void Reset() {
        pendingTransfer = null;
        InCombatScene = true;
    }

    public static void BeginTransfer(string sceneName) {
        pendingTransfer = sceneName;
    }

    public static bool CompleteTransfer(Scene scene) {
        if (scene.name != pendingTransfer) return false;
        pendingTransfer = null;
        InCombatScene = scene.name != BossScene;
        return true;
    }

    public static IEnumerator TransferTimeout(string sceneName, Action onTimeout) {
        yield return new WaitForSecondsRealtime(TransferTimeoutSeconds);
        if (pendingTransfer != sceneName) yield break;
        Debug.LogWarning($"[SceneManager] Transfer to '{sceneName}' never arrived; portals re-enabled.");
        pendingTransfer = null;
        onTimeout?.Invoke();
    }

    public static Func<string, GameObject> Finder(Scene scene) {
        Dictionary<string, GameObject> byName = new Dictionary<string, GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name)) byName[t.name] = t.gameObject;
        return name => byName.TryGetValue(name, out GameObject go) ? go : null;
    }

    public static bool SurvivesInto(GameObject go, Scene scene) {
        return go.scene == scene || go.scene.name == "DontDestroyOnLoad";
    }
}
