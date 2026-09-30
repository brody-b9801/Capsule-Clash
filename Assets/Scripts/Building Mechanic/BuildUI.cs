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

        if (objectSpawner == null)
        {
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

        if (timer != null) timer.fillAmount = (buildResetTime / 100);
        if (arrow != null) arrow.localEulerAngles = new Vector3(0, 0, 360 * (buildResetTime / 100));
        WarnMissingRefsOnce();

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
