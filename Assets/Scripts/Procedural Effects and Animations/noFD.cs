using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class noFD : MonoBehaviour
{
    private RectTransform fdRect;
    private Image image;
    [SerializeField] private RectTransform canvas;
    private float alphaVal = 0;

    void Start()
    {
        fdRect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    void Update()
    {
        fdRect.sizeDelta = canvas.sizeDelta + new Vector2(50f,50f);
        image.color = new Color32(255,248,204,(byte)alphaVal);
        if (HealthController.noFDAnim) {
            HealthController.noFDAnim = false;
            StartCoroutine(setAlpha());
        }
    }

    IEnumerator setAlpha() {
        float startingVal = alphaVal;
        float total = 0.25f;
        float elapsed = 0;
        while (elapsed < total) {
            if (HealthController.noFDAnim) {
                startingVal = alphaVal;
                HealthController.noFDAnim = false;
                elapsed = 0;
            } 
            alphaVal = Mathf.Sin(elapsed/total*Mathf.PI)*35f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        alphaVal = 0;
        yield break;
    }
}
