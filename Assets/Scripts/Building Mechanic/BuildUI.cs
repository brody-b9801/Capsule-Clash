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

    void Update()
    {
        if (!started)
        {
            totalBuildTime = 0;
            return;
        }

        // started is set by RoomMenu when the room UI opens, which is independent
        // of player spawn — under FishNet the player arrives later, so the spawner
        // reference can still be null here.
        if (objectSpawner == null) return;

        if (objectSpawner.buildNum < 25 && !lerpingBuild)
            StartCoroutine(lerpBuild());

        builds.text = objectSpawner.buildNum.ToString();
        timer.fillAmount = (buildResetTime / 100);
        arrow.localEulerAngles = new Vector3(0, 0, 360 * (buildResetTime / 100));

        if (BuildTimer.Instance != null)
            totalBuildTime = BuildTimer.Instance.Tick(totalBuildTime, Time.deltaTime);
        else if (isHost)
            totalBuildTime += Time.deltaTime;

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
        // The spawner can despawn while this coroutine is mid-wait.
        if (objectSpawner != null) objectSpawner.buildNum++;
        yield break;

    }
}
