using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TP : MonoBehaviour
{
    private RectTransform tpRect;
    private Image image;
    [SerializeField]private RectTransform canvas;
    private float alphaVal = 0;

    void Start()
    {
        tpRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        tpRect.sizeDelta = canvas.sizeDelta + new Vector2(50f,50f);
        image.color = new Color32(0,255,255,(byte)alphaVal);
        if (HealthController.tpAnim) {
            HealthController.tpAnim = false;
            StartCoroutine(setAlpha());
        }
    }

    IEnumerator setAlpha() {
        float startingVal = alphaVal;
        float total = 0.25f;
        float elapsed = 0;
        while (elapsed < total) {
            if (HealthController.tpAnim) {
                startingVal = alphaVal;
                HealthController.tpAnim = false;
                elapsed = 0;
            } 
            alphaVal = Mathf.Sin(elapsed/total*Mathf.PI)*20;
            elapsed += Time.deltaTime;
            yield return null;
        }
        alphaVal = 0;
        yield break;
    }
}
