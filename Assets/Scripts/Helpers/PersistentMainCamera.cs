using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps the main camera rig alive across scene loads and removes the duplicate the
/// newly loaded scene brings with it. The camera is a plain GameObject, not a
/// NetworkBehaviour, so DontDestroyOnLoad is allowed here.
/// </summary>
[DisallowMultipleComponent]
public class PersistentMainCamera : MonoBehaviour {

    private static PersistentMainCamera _instance;

    /// <summary>
    /// Attaches the keeper to the camera the first time it is called; later calls do nothing.
    /// </summary>
    public static void Ensure(Camera cam) {
        if (_instance != null || cam == null) return;
        if (cam.GetComponent<PersistentMainCamera>() == null) cam.gameObject.AddComponent<PersistentMainCamera>();
    }

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(transform.root.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy() {
        if (_instance != this) return;
        _instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Both CombatScene and BossScene ship their own Main Camera instance. Two enabled
    /// cameras tagged MainCamera make Camera.main ambiguous and render the view twice,
    /// so the arriving scene's copy is dropped in favour of the one we carried over.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        foreach (Camera cam in Camera.allCameras) {
            if (cam == null || cam.gameObject == gameObject) continue;
            if (!cam.CompareTag("MainCamera")) continue;

            Debug.Log($"[PersistentMainCamera] dropping duplicate '{cam.name}' from '{scene.name}'; " +
                      "the carried-over camera keeps its post-processing state.", cam);
            Destroy(cam.gameObject);
        }
    }
}
