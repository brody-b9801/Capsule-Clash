using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

[DisallowMultipleComponent]
public class SceneManager : MonoBehaviour {
    public const string DesertScene = "DesertScene";
    public const string SnowScene = "SnowScene";
    public const string MazeScene = "MazeScene";
    public const string SpaceScene = "SpaceScene";
    public const string BossScene = "BossScene";
    private const float TransferTimeoutSeconds = 15f;

    public static SceneManager Instance { get; private set; }

    public string activeAdditiveScene;

    public bool InCombatScene { get; private set; }
    private string pendingTransfer;

    void Start()
    {
        LoadScene(BossScene);
    }

    public static event UnityAction<Scene, LoadSceneMode> SceneLoaded {
        add => UnitySceneManager.sceneLoaded += value;
        remove => UnitySceneManager.sceneLoaded -= value;
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (Instance == this) Instance = null;
    }

    public bool LoadScene(string scene)
    {
        if (scene == activeAdditiveScene) return true;
        if (!Application.CanStreamedLevelBeLoaded(scene)) {
            Debug.LogError($"[SceneManager] '{scene}' cannot be loaded; add it to the build settings scene list.");
            return false;
        }

        StartCoroutine(LoadSceneRoutine(scene));
        if (!string.IsNullOrEmpty(activeAdditiveScene) && UnitySceneManager.GetSceneByName(activeAdditiveScene).IsValid())
            UnitySceneManager.UnloadSceneAsync(activeAdditiveScene);
        activeAdditiveScene = scene;
        return true;
    }

    public bool TryGetActiveScene(string scene, out Scene loaded) {
        loaded = UnitySceneManager.GetSceneByName(scene);
        return scene == activeAdditiveScene && loaded.isLoaded;
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        AsyncOperation operation = UnitySceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            yield return null;
        }
    }

    public void Reset() {
        pendingTransfer = null;
        InCombatScene = activeAdditiveScene != BossScene;
    }

    public void BeginTransfer(string sceneName) {
        pendingTransfer = sceneName;
    }

    public bool CompleteTransfer(Scene scene) {
        if (scene.name != pendingTransfer) return false;
        pendingTransfer = null;
        InCombatScene = scene.name != BossScene;
        return true;
    }

    public IEnumerator TransferTimeout(string sceneName, Action onTimeout) {
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
}
