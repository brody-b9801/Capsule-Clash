using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heal : MonoBehaviour
{
    private RectTransform healRect;
    private Image image;
    [SerializeField] private RectTransform canvas;
    private float alphaVal = 0;

    void Start()
    {
        healRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        healRect.sizeDelta = canvas.sizeDelta + new Vector2(50f,50f);
    }

    void Update()
    {
        image.color = new Color32(152,250,152,(byte)alphaVal);
        if (HealthController.healAnim) {
            HealthController.healAnim = false;
            StartCoroutine(setAlpha());
        }
    }

    IEnumerator setAlpha() {
        float startingVal = alphaVal;
        float total = 0.25f;
        float elapsed = 0;
        while (elapsed < total) {
            if (HealthController.healAnim) {
                startingVal = alphaVal;
                HealthController.healAnim = false;
                elapsed = 0;
            } 
            alphaVal = Mathf.Sin(elapsed/total*Mathf.PI)*50;
            elapsed += Time.deltaTime;
            yield return null;
        }
        alphaVal = 0;
        yield break;
    }
}
