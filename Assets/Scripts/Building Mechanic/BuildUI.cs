using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Linq;

public class BuildUI : NetworkBehaviour
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

    // Genuinely server-authoritative: only the server advances the build clock,
    // every client reads it. Left at the default ServerOnly write permission.
    private readonly SyncVar<float> syncedBuildTime = new SyncVar<float>(0f);

    private List<bool> activePrevious = new List<bool>();

    // Replaces the old client-set 'isHost' bool. IsServerStarted is authoritative
    // and cannot be spoofed by a client, unlike the previous flag.
    public static bool isHost => InstanceFinder.IsServerStarted;

    void Update()
    {
        if (!started)
        {
            totalBuildTime = 0;
            return;
        }

        if (objectSpawner.buildNum < 25 && !lerpingBuild)
            StartCoroutine(lerpBuild());

        builds.text = objectSpawner.buildNum.ToString();
        timer.fillAmount = (buildResetTime / 100);
        arrow.localEulerAngles = new Vector3(0, 0, 360 * (buildResetTime / 100));

        if (isHost)
        {
            totalBuildTime += Time.deltaTime;
            syncedBuildTime.Value = totalBuildTime;
        }
        else
        {
            totalBuildTime = syncedBuildTime.Value;
        }

        buildResetTime = 100 - (totalBuildTime % 100);
        if (isHost && buildResetTime > buildResetTimePrev) {
            objectSpawner.DestroyAllBuildsSync();
        }
        buildResetTimePrev = buildResetTime;


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
        objectSpawner.buildNum++;
        yield break;

    }
}
