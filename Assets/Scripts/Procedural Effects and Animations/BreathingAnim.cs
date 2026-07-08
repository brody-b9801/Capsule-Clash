using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreathingAnim : MonoBehaviour
{
    public static float yVal;
    private bool breathing;

    void Update()
    {
        if (!CameraZoom.moving && !breathing && PlayerMovement.isGrounded && !Shaker.shooting) {
            StartCoroutine(BreathingControl());
        } 
    }

    private IEnumerator BreathingControl() {
        breathing = true;
        float time = 0;
        float sinVal = 0;
        yVal = 0;
        yield return new WaitForSeconds(0.75f);
        while (!CameraZoom.moving && PlayerMovement.isGrounded && !Shaker.shooting) {
            while (time < (Mathf.PI/1.8f)) {
                time += Time.deltaTime;
                sinVal = Mathf.Sin(0.9f * time);
                yVal = Mathf.Abs(sinVal)*0.1f;
                yield return null;
            }
            time = 0;
            while (time < (Mathf.PI/1.8f)) {
                time += Time.deltaTime;
                sinVal = Mathf.Sin(0.9f * time);
                yVal = 0.1f - Mathf.Abs(sinVal)*0.1f;
                yield return null;
            }
            time = 0;
            yield return new WaitForSeconds(0.02f);
        }
        
        float yValEnd = yVal;
        time = 0;
        while (yVal > 0.01f) {
            yVal = Mathf.Lerp(yValEnd, 0, 10*time);
            time += Time.deltaTime;
            yield return null;
        }
        yVal = 0;
        breathing = false;
        yield break;
    }
}
