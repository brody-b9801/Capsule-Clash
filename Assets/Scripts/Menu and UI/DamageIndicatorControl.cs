using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DamageIndicatorControl : MonoBehaviour
{
    [SerializeField] private Image top;
    [SerializeField] private Image left;
    [SerializeField] private Image bottom;
    [SerializeField] private Image right;
    private float alphaVal = 0;
    public static bool setDamageCross = false;
    private bool setting = false;
    private float time = 0;
    private float totalTime = 0.05f;

    void Update()
    {
        top.color = new Color32(255,255,255,(byte)alphaVal);
        left.color = new Color32(255,255,255,(byte)alphaVal);
        bottom.color = new Color32(255,255,255,(byte)alphaVal);
        right.color = new Color32(255,255,255,(byte)alphaVal);
        if (setDamageCross && !setting) {
            StartCoroutine(alphaControl());
        }
    }

    public IEnumerator alphaControl() {
        setting = true;
        alphaVal = 255f;
        time = 0;
        while (time < totalTime) {
            if (setDamageCross) {
                time = 0;
                setDamageCross = false;
            }
            alphaVal = Mathf.Cos((time/totalTime)*0.5f*Mathf.PI)*255f;
            time += Time.deltaTime;
            yield return null;
        }
        setting = false;
        alphaVal = 0;
        yield break;
    }
}
