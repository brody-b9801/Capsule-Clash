using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using System.Linq;

public class BuildUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI builds;
    [SerializeField] private Image timer;
    public static float totalBuildTime;
    public static float buildResetTime;
    public static float buildResetTimePrev;
    public float totalTime;
    public static bool started = false;
    private bool lerpingBuild = false;
    public static ObjectSpawner objectSpawner;
    [SerializeField] private Transform arrow;

    private List<bool> activePrevious = new List<bool>();

    public static bool isHost => InstanceFinder.IsServerStarted;

    // TEMPORARY diagnostic: the HUD counter sits at its authored value with no
    // warning logged, so Update either never runs or bails at one of the gates
    // below. Logs once per state change rather than every frame.
    private string _lastGate;

    private void LogGate(string gate)
    {
        if (gate == _lastGate) return;
        _lastGate = gate;
        Debug.Log($"[BuildUI] gate={gate} obj='{gameObject.name}' " +
                  $"enabled={enabled} activeInHierarchy={gameObject.activeInHierarchy}", this);
    }

    private void OnEnable()
    {
        Debug.Log($"[BuildUI] OnEnable on '{gameObject.name}'", this);
    }

    void Update()
    {
        if (!started)
        {
            LogGate("not-started");
            totalBuildTime = 0;
            return;
        }

        // started is set by RoomMenu when the room UI opens, which is independent
        // of player spawn — under FishNet the player arrives later, so the spawner
        // reference can still be null here.
        if (objectSpawner == null)
        {
            // ObjectSpawner.OnStartClient assigns this for the owner. If that
            // callback is missed — an exception in an earlier NetworkBehaviour's
            // OnStartClient aborts the rest of them — the HUD would sit frozen
            // with no complaint, so recover it from the local player instead.
            if (PlayerMovement.Local == null)
            {
                LogGate("no-local-player");
                return;
            }

            objectSpawner = PlayerMovement.Local.GetComponent<ObjectSpawner>();
            if (objectSpawner == null)
            {
                LogGate("player-has-no-spawner");
                return;
            }

            Debug.LogWarning("[BuildUI] objectSpawner was never assigned by " +
                             "ObjectSpawner.OnStartClient; recovered it from PlayerMovement.Local.");
        }

        LogGate("running");

        if (objectSpawner.buildNum < 25 && !lerpingBuild)
            StartCoroutine(lerpBuild());

        builds.text = objectSpawner.buildNum.ToString();

        // timer and arrow are optional on some HUD prefabs; dereferencing a missing
        // one threw every frame and aborted the rest of Update, which is what froze
        // the reset ring and the build clock below.
        if (timer != null) timer.fillAmount = (buildResetTime / 100);
        if (arrow != null) arrow.localEulerAngles = new Vector3(0, 0, 360 * (buildResetTime / 100));
        WarnMissingRefsOnce();

        // The server owns the clock and replicates it on the spawner; a client
        // ticking its own copy would just sit at zero and freeze the ring.
        totalBuildTime = objectSpawner.buildTime;

        buildResetTime = 100 - (totalBuildTime % 100);
        if (isHost && buildResetTime > buildResetTimePrev) {
            objectSpawner.DestroyAllBuildsSync();
        }
        buildResetTimePrev = buildResetTime;


    }

    private bool warnedMissingRefs;

    private void WarnMissingRefsOnce()
    {
        if (warnedMissingRefs) return;
        if (timer != null && arrow != null) return;

        warnedMissingRefs = true;
        Debug.LogWarning($"[BuildUI] on '{gameObject.name}': " +
                         $"timer={(timer == null ? "MISSING" : "ok")}, " +
                         $"arrow={(arrow == null ? "MISSING" : "ok")}. " +
                         "Assign them on the HUD prefab this scene actually uses.", this);
    }

    public void enableUI()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "MaskText")
            {
                child.gameObject.SetActive(activePrevious.ElementAtOrDefault(i));
            }
        }
    }

    public void disableUI()
    {
        activePrevious.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name != "MaskText")
            {
                activePrevious.Add(child.gameObject.activeSelf);
                child.gameObject.SetActive(false);
            }
        }
    }

    IEnumerator lerpBuild() {
        float time = 0;
        lerpingBuild = true;

        while (time < totalTime) {
            time += Time.deltaTime;
            yield return null;
        }   

        lerpingBuild = false;

        if (objectSpawner != null) objectSpawner.RequestBuildRegen();
        yield break;

    }
}
