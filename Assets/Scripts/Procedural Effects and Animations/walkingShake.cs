using System.Collections; 
using System.Collections.Generic;
using UnityEngine;

public class walkingShake : MonoBehaviour
{
    public float walkAmplitude = 0.05f; 
    public float walkFrequency = 2.0f;
    public float sprintAmplitude = 0.0666f;
    public float sprintFrequency = 4.33333f;
    public float aimAmplitude = 0.03f;
    public float aimFrequency = 2f;

    private float amplitude;
    private float frequency;

    // FINAL upgraded values used for motion
    private float finalAmplitude;
    private float finalFrequency;

    private Vector3 originalPosition;
    public static float newY;
    public static float newX;

    private bool lerpingY = false;
    private bool lerpingX = false;
    private float time;
    private bool rampedUp = false;
    private bool rampingUp = false;
    private float rampUpTime;
    private bool lerpComplete = true;
    private bool walkStarted = false;
    private float xPrev;

    private float groundedTime = 0f;
    private const float groundedThreshold = 0.05f;

    void Start()
    {
        originalPosition = transform.localPosition;
        time = 0f;
        amplitude = walkAmplitude;
        frequency = walkFrequency;
    }

    void Update()
    {
        float oldFreq = frequency;

        float amplitudeUpgrade = 1f + ((upgradeManager.speedMultiplier - 1f) * 0.05f);
        float frequencyUpgrade = 1f + ((upgradeManager.speedMultiplier - 1f) * 0.1f);

        if (PlayerMovement.isSprinting && amplitude != sprintAmplitude)
        {
            amplitude = sprintAmplitude;
            frequency = sprintFrequency;
            time = time * oldFreq / sprintFrequency;
            rampUpTime = rampUpTime * walkFrequency / sprintFrequency;
        }
        else if (!PlayerMovement.isSprinting && amplitude != walkAmplitude && !CameraZoom.isAiming)
        {
            amplitude = walkAmplitude;
            frequency = walkFrequency;
            time = time * oldFreq / walkFrequency;        
            rampUpTime = rampUpTime * sprintFrequency / walkFrequency;
        }
        else if (CameraZoom.isAiming && amplitude != aimAmplitude && !Shaker.shooting) 
        {
            amplitude = aimAmplitude;
            frequency = aimFrequency;
            time = time * oldFreq / aimFrequency;
            rampUpTime = rampUpTime * oldFreq / aimFrequency;
        }
        else if (Shaker.shooting) 
        {
            amplitude = 0f;
        }

        finalAmplitude = amplitude * amplitudeUpgrade * PlayerMovement.percentAccelerated;
        finalFrequency = frequency * frequencyUpgrade;

        if (PlayerMovement.isGrounded)
            groundedTime += Time.deltaTime;
        else
            groundedTime = 0f;

        if (groundedTime >= groundedThreshold)
        {
            if (GunThingAnim.gunMoving && GunThingAnim.movingState && !lerpingX)
            {
                lerpComplete = false;

                if (!rampedUp && !rampingUp)
                {
                    StartCoroutine(rampUp());
                }
                else if (rampedUp)
                {
                    xPrev = newX;
                    walkStarted = true;
                    time += Time.deltaTime;

                    newY = Mathf.Sin(time * finalFrequency * Mathf.PI) * finalAmplitude;
                    newX = Mathf.Cos(time * finalFrequency * Mathf.PI) * finalAmplitude;
                }
            }
            else
            {
                if (!lerpingX && !rampingUp && !lerpComplete)
                    StartCoroutine(lerpZeroX());
            }
        }
        else
        {
            if (!lerpingX && !rampingUp && !lerpComplete)
                StartCoroutine(lerpZeroX());
        }
    }

    IEnumerator rampUp()
    {
        float newXRef = 0f;
        rampingUp = true;
        rampUpTime = 0f;

        float targetSign = Mathf.Sign(Mathf.Cos(time * finalFrequency * Mathf.PI) * finalAmplitude);

        while (rampUpTime < (1f / finalFrequency))
        {
            newY = Mathf.Sin(rampUpTime * finalFrequency * Mathf.PI) * finalAmplitude * 0.666666f;
            newXRef = Mathf.Sin(rampUpTime * finalFrequency * 0.5f * Mathf.PI) * finalAmplitude;

            rampUpTime += Time.deltaTime;
            newX = newXRef * targetSign;

            yield return null;
        }

        rampedUp = true;
        rampingUp = false;
    }

    IEnumerator lerpZeroX()
    {
        rampedUp = false;
        lerpingX = true;

        float modPrev = 0f;
        float startX = newX;

        if (!((xPrev > newX && newX > 0) || (xPrev < newX && newX < 0)) && walkStarted || !PlayerMovement.isGrounded) 
        {
            float duration = (1f / finalFrequency) / 2f;
            float elapsedTime = 0f;

            float initialX = newX;
            float initialY = newY;

            while (elapsedTime < duration)
            {
                newX = Mathf.Lerp(initialX, 0, elapsedTime / duration);
                newY = Mathf.Lerp(initialY, 0, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            newX = 0f;
            newY = 0f;

            time = (initialX > 0 || (initialX == 0 && xPrev > 0)) ? 1f / finalFrequency : 0f;

            lerpingX = false;
            lerpComplete = true;
            walkStarted = false;
            yield break;
        }

        float amplitudeX = Mathf.Abs(newX);
        float timeX = time * finalFrequency / (0.5f * finalFrequency * (amplitudeX / finalAmplitude));
        float amplitudeY = walkStarted ? finalAmplitude : finalAmplitude * 0.666666f;

        while (!(modPrev > (time / (1f / finalFrequency)) % 1f)) 
        {
            modPrev = (time / (1f / finalFrequency)) % 1f;
            time += Time.deltaTime;
            timeX += Time.deltaTime;

            newY = Mathf.Sin(time * finalFrequency * Mathf.PI) * amplitudeY;
            newX = Mathf.Cos(timeX * (0.5f * finalFrequency * (amplitudeX / finalAmplitude)) * Mathf.PI) * finalAmplitude;

            yield return null;
        }

        newX = 0f;
        newY = 0f;
        lerpingX = false;
        lerpComplete = true;
        walkStarted = false;
    }
}
