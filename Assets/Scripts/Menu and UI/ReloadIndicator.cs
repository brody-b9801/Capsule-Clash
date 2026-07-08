using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReloadIndicator : MonoBehaviour
{
    public static Image ReloadBar;
    public static GameObject ReloadBarBg;
    [SerializeField] private Image HealBar;
    private bool healAnimStarted = false;

    void Start()
    {
        ReloadBar = GameObject.Find("ReloadBlack").GetComponent<Image>();
        ReloadBarBg = GameObject.Find("RBg");
        ReloadBar.gameObject.SetActive(false);
        ReloadBarBg.SetActive(false);
        HealBar.gameObject.SetActive(false);
    }

    void Update()
    {
        if (healParticles.healing && !healAnimStarted)
        {
            StartCoroutine(healBarAnim());
        }
    }

    public static void Reload()
    {
        ReloadBar.gameObject.SetActive(true);
        ReloadBarBg.SetActive(true);
        ReloadBar.rectTransform.sizeDelta = new Vector2(66.0f, ReloadBar.rectTransform.sizeDelta.y);
        ReloadBar.transform.parent.GetComponent<MonoBehaviour>().StartCoroutine(DecreaseWidthOverTime(2.01f / upgradeManager.reloadSpeedMultiplier));    
    }

    IEnumerator healBarAnim()
    {
        healAnimStarted = true;
        HealBar.gameObject.SetActive(true);
        ReloadBarBg.SetActive(true);
        float total = 3f / upgradeManager.regenSpeedMultiplier;

        HealBar.rectTransform.sizeDelta = new Vector2(0.0f, HealBar.rectTransform.sizeDelta.y);
        while (PlayerMovement.elapsedHealTime < total && healParticles.healing)
        {
            float percent = PlayerMovement.elapsedHealTime / total;
            float newWidth = Mathf.Lerp(0.0f, 66.0f, percent);
            HealBar.rectTransform.sizeDelta = new Vector2(newWidth, HealBar.rectTransform.sizeDelta.y);
            total = 3f / upgradeManager.regenSpeedMultiplier;
            yield return null;
        }
        healAnimStarted = false;
        HealBar.gameObject.SetActive(false);
        ReloadBarBg.SetActive(false);
    }

    private static IEnumerator DecreaseWidthOverTime(float duration)
    {
        float elapsedTime = 0.0f;
        float initialWidth = ReloadBar.rectTransform.sizeDelta.x;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float newWidth = Mathf.Lerp(initialWidth, 0.0f, t);

            ReloadBar.rectTransform.sizeDelta = new Vector2(newWidth, ReloadBar.rectTransform.sizeDelta.y);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        ReloadBar.gameObject.SetActive(false);
        ReloadBarBg.SetActive(false);
    }
}
