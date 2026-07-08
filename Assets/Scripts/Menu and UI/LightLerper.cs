using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LightLerper : MonoBehaviour
{
    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
        float newWidth = Screen.width + 50;
        float newHeight = Screen.height + 50;
        GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHeight);
        StartCoroutine(LerpLight());
    }

    IEnumerator LerpLight()
    {
        float time = 0;
        float totalDuration = 0.08f;
        while (time <= totalDuration)
        {
            image.color = new Color(image.color.r,image.color.g,image.color.b, 0.05f*(1-(time/totalDuration)));
            time += Time.deltaTime;
            yield return null;
        }
        Destroy(image);
    }
}
