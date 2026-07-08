using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Alteruna;

public class BuildUI : AttributesSync
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
    public static bool isHost;
    [SerializeField] private Transform arrow;
    [SynchronizableField] private float syncedBuildTime; 
    void Update()
    {
        if (!started)
        {
            totalBuildTime = 0;
            return;
        }

        if (ObjectSpawner.buildNum < 25 && !lerpingBuild)
            StartCoroutine(lerpBuild());

        builds.text = ObjectSpawner.buildNum.ToString();
        timer.fillAmount = (buildResetTime / 100);
        arrow.localEulerAngles = new Vector3(0, 0, 360 * (buildResetTime / 100));

        if (isHost)
        {
            totalBuildTime += Time.deltaTime;
            syncedBuildTime = totalBuildTime;
        }
        else
        {
            totalBuildTime = syncedBuildTime; 
        }

        buildResetTime = 100 - (totalBuildTime % 100);
        if (isHost && buildResetTime > buildResetTimePrev) {
            objectSpawner.DestroyAllBuildsSync();
        }
        buildResetTimePrev = buildResetTime;


    }


    IEnumerator lerpBuild() {
        float time = 0;
        lerpingBuild = true;

        while (time < totalTime) {
            time += Time.deltaTime;
            yield return null;
        }   

        lerpingBuild = false;
        ObjectSpawner.buildNum++;
        yield break;

    }
}
