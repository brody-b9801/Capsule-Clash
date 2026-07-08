using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Blood : MonoBehaviour
{
    private RectTransform bloodRect;
    private Image image;
    [SerializeField] private RectTransform canvas;
    private float alphaVal = 0;

    void Start()
    {
        bloodRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        bloodRect.sizeDelta = canvas.sizeDelta + new Vector2(50f,50f);
    }

    void Update()
    {
        image.color = new Color32(255,0,0,(byte)alphaVal);
        if (HealthController.damageAnim) {
            HealthController.damageAnim = false;
            StartCoroutine(setAlpha());
        }
    }

    IEnumerator setAlpha() {
        float startingVal = alphaVal;
        float total = 0.1f;
        float elapsed = 0;
        while (elapsed < total) {
            if (HealthController.damageAnim) {
                startingVal = alphaVal;
                HealthController.damageAnim = false;
                elapsed = 0;
            } 
            alphaVal = Mathf.Sin(elapsed/total*Mathf.PI)*75;
            elapsed += Time.deltaTime;
            yield return null;
        }
        alphaVal = 0;
        yield break;
    }
}
