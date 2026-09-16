using UnityEngine;
using UnityEngine.SceneManagement;


[DisallowMultipleComponent]
public class PersistentObjects : MonoBehaviour {

    private static PersistentObjects _instance;
    [SerializeField] private GameObject[] objectsToPersist;

    public static void Ensure(Camera cam) {
        if (_instance != null || cam == null) return;
        if (cam.GetComponent<PersistentObjects>() == null) cam.gameObject.AddComponent<PersistentObjects>();
    }

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        foreach (GameObject obj in objectsToPersist)
        {
            obj.transform.parent = null;
            DontDestroyOnLoad(obj);
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        if (_instance != this) return;
        _instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        foreach (Camera cam in Camera.allCameras) {
            if (cam == null || cam.gameObject == gameObject) continue;
            if (!cam.CompareTag("MainCamera")) continue;

            Destroy(cam.gameObject);
        }
    }
}
