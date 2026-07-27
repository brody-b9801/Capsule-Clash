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
    private Coroutine lerpZeroXRoutine;

    void Start()
    {
        originalPosition = transform.localPosition;
        time = 0f;
        amplitude = walkAmplitude;
        frequency = walkFrequency;
    }

    void Update()
    {
        // Guard every frequency this frame: the time/rampUpTime rescales below and
        // the 1/frequency periods in the coroutines all divide by these, so a zero
        // anywhere turns time into Inf and every Sin/Cos downstream into NaN.
        walkFrequency = Mathf.Max(walkFrequency, 0.01f);
        sprintFrequency = Mathf.Max(sprintFrequency, 0.01f);
        aimFrequency = Mathf.Max(aimFrequency, 0.01f);

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
            if (GunThingAnim.gunMoving && GunThingAnim.movingState)
            {
                if (lerpingX)
                {
                    if (lerpZeroXRoutine != null)
                        StopCoroutine(lerpZeroXRoutine);
                    lerpZeroXRoutine = null;
                    lerpingX = false;

                    rampedUp = true;
                    rampingUp = false;

                    float resumeFrequency = Mathf.Max(finalFrequency, 0.01f);
                    float resumeAmplitude = Mathf.Max(Mathf.Abs(finalAmplitude), 0.0001f);
                    float phase = Mathf.Acos(Mathf.Clamp(newX / resumeAmplitude, -1f, 1f));

                    if (newY < 0f)
                        phase = (2f * Mathf.PI) - phase;

                    time = phase / (resumeFrequency * Mathf.PI);
                }

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
                    lerpZeroXRoutine = StartCoroutine(lerpZeroX());
            }
        }
        else
        {
            if (!lerpingX && !rampingUp && !lerpComplete)
                lerpZeroXRoutine = StartCoroutine(lerpZeroX());
        }
    }

    IEnumerator rampUp()
    {
        float newXRef = 0f;
        rampingUp = true;
        rampUpTime = 0f;

        float targetSign = Mathf.Sign(Mathf.Cos(time * finalFrequency * Mathf.PI) * finalAmplitude);
        float safeFrequency = Mathf.Max(finalFrequency, 0.01f);

        while (rampUpTime < (1f / safeFrequency))
        {
            newY = Mathf.Sin(rampUpTime * safeFrequency * Mathf.PI) * finalAmplitude * 0.666666f;
            newXRef = Mathf.Sin(rampUpTime * safeFrequency * 0.5f * Mathf.PI) * finalAmplitude;

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

        float safeFrequency = Mathf.Max(finalFrequency, 0.01f);
        float easeAmplitude = Mathf.Max(Mathf.Abs(newX), Mathf.Abs(finalAmplitude));

        if (easeAmplitude < 0.0001f || !((xPrev > newX && newX > 0) || (xPrev < newX && newX < 0)) && walkStarted || !PlayerMovement.isGrounded)
        {
            float duration = (1f / safeFrequency) / 2f;
            float elapsedTime = 0f;

            float initialX = newX;
            float initialY = newY;

            while (elapsedTime < duration)
            {

                float t = Mathf.SmoothStep(0f, 1f, elapsedTime / duration);
                newX = Mathf.Lerp(initialX, 0, t);
                newY = Mathf.Lerp(initialY, 0, t);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            newX = 0f;
            newY = 0f;

            time = (initialX > 0 || (initialX == 0 && xPrev > 0)) ? 1f / safeFrequency : 0f;

            lerpingX = false;
            lerpZeroXRoutine = null;
            lerpComplete = true;
            walkStarted = false;
            yield break;
        }

        float amplitudeX = Mathf.Abs(newX);
        float normalizedX = Mathf.Clamp(amplitudeX / easeAmplitude, 0.1f, 1f);
        float rateX = 0.5f * safeFrequency * normalizedX;

        float timeX = time * safeFrequency / rateX;
        float amplitudeY = walkStarted ? easeAmplitude : easeAmplitude * 0.666666f;

        while (!(modPrev > (time / (1f / safeFrequency)) % 1f))
        {
            modPrev = (time / (1f / safeFrequency)) % 1f;
            time += Time.deltaTime;
            timeX += Time.deltaTime;

            newY = Mathf.Sin(time * safeFrequency * Mathf.PI) * amplitudeY;
            newX = Mathf.Cos(timeX * rateX * Mathf.PI) * easeAmplitude;

            yield return null;
        }

        newX = 0f;
        newY = 0f;
        lerpingX = false;
        lerpZeroXRoutine = null;
        lerpComplete = true;
        walkStarted = false;
    }
}
