using System.Collections;
using UnityEngine;

public class Shaker : MonoBehaviour
{
    [SerializeField] private float shakeDuration;
    [SerializeField] private float downDuration;
    private float downDurationStart;
    private static float changeDownRot;
    public static float totalShake;
    [SerializeField] private float rotationMod = 10.0f;
    private static float rotMod;
    [SerializeField] private float noiseMagnitudeZ = 0.5f;
    [SerializeField] private float noiseMagnitudeY = 0.5f;
    public static float zRot;
    public static float yRot;
    [SerializeField] private Transform cameraTransform;
    private bool shakeStarted = false;
    private float rotationAmountX;
    private float elapsed;
    private float percentComplete;
    [SerializeField] private Camera cam2;
    [SerializeField] private float FOVMod;
    private float smoothStep;
    public static float easedRotationChange;
    public static float previousERC;
    private bool upRot = false;
    private bool fovChanged = false;
    public static bool shooting = false;
    private static bool setDownRot = true;
    [SerializeField] private float downRotationChange;
    [SerializeField] private float rotationModChange;
    private static float downRotationChangeRef;
    private static float rotationModChangeRef;
    public static bool shakeStartedRef;
    [SerializeField] private float startRot;
    [SerializeField] private float startDownSpeed;
    private static float startRotRef;
    private static float startDownSpeedRef;
    private float targetZOffset;
    private float targetYOffset;
    [SerializeField] private float frequencyFactor;
    private float rotModZoom;
    [SerializeField] private float zoomClamp;
    [SerializeField] private float zoomDivisor;
    [SerializeField] private float shakeClampZ;
    [SerializeField] private float shakeClampY;
    public static float FOVModRef;
    private static Shaker instance;

    private void Update() {
        if (shooting) {
            downDuration = downDurationStart * 1/(1 + (upgradeManager.fireRateMultiplier - 1) * 4);
        } else {
            downDuration = downDurationStart;
        }
        
        shakeStartedRef = shakeStarted;
        startRotRef = startRot;
        startDownSpeedRef = startDownSpeed;

        if (!shooting && rotMod != rotationMod) {
            if (!shakeStarted)
            rotMod = rotationMod;  
            setDownRot = true;
        } 

        if (shooting && CameraZoom.isAiming) {
            float rotModRef = rotMod;
            rotModZoom = Mathf.Clamp(rotModRef, 0f, zoomClamp);
        }

        if (upRot && !fovChanged) {
            fovChanged = true;
            if (Shooting.shotgun) {
                FOVModRef = FOVMod*10;
            } else {
                FOVModRef = FOVMod*2;
            }
        } else {
            FOVModRef = 0;
        }
    }
    
    private void Awake()
    {
        shakeStartedRef = shakeStarted;
        rotMod = rotationMod;
        changeDownRot = downDuration;
        downRotationChangeRef = downRotationChange;
        downDurationStart = downDuration;
        rotationModChangeRef = rotationModChange;
        totalShake = shakeDuration + downDuration;

        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static Shaker Instance
    {
        get { return instance; }
    }

    public void Shake()
    {
        StartCoroutine(ShakeScreen());
        StartCoroutine(ShakeWithPerlinNoise());
    }

    private IEnumerator ShakeScreen()
    {
        if (CollisionControl.avatar) {
            elapsed = 0.0f;
            if (!CameraZoom.isAiming) {
                if (Shooting.shotgun) {
                    rotationAmountX = previousERC + (rotMod * 2 * Random.Range(0.9f, 1f));
                } else if (CameraZoom.moving) {
                    rotationAmountX = previousERC + (rotMod * 1.2f * Random.Range(0.9f, 1f));
                } else {
                    rotationAmountX = previousERC + (rotMod * Random.Range(0.9f, 1f));
                }
            } else {
                rotationAmountX = previousERC + (rotModZoom * Random.Range(0.9f, 1f));
            }
            percentComplete = 0.0f;
            easedRotationChange = previousERC;
            fovChanged = false;

            if (!shakeStarted) {
                shakeStarted = true;
            }

            while (elapsed < shakeDuration)
            {
                upRot = true;
                if (shakeStarted) {
                    percentComplete = elapsed / shakeDuration;
                    smoothStep = Mathf.SmoothStep(0f, 1f, percentComplete);

                    easedRotationChange = Mathf.SmoothStep(previousERC, rotationAmountX, percentComplete);

                    elapsed += Time.deltaTime;
                    yield return null;
                } else {
                    StopShake();
                    yield return null;
                }
            }

            elapsed = 0.0f;
            percentComplete = 0.0f;
            smoothStep = 0.0f;

            while (elapsed < changeDownRot)
            {
                upRot = false;
                if (!shooting) {
                    changeDownRot = downDuration;
                }
                if (shakeStarted) {
                    percentComplete = elapsed / changeDownRot;
                    smoothStep = Mathf.SmoothStep(0f, 1f, percentComplete);

                    easedRotationChange = rotationAmountX - Mathf.SmoothStep(0f, rotationAmountX-previousERC, percentComplete);

                    elapsed += Time.deltaTime;
                    yield return null;
                } else {
                    StopShake();
                    yield return null;
                }
            }

            setDownRot = true;
            changeDownRot = downDuration;
            shakeStarted = false;
            previousERC = 0.0f;
            yield break;
        }
    }

    private IEnumerator ShakeWithPerlinNoise()
    {
        Vector3 originalPosition = cameraTransform.localPosition;

        float time = 0;

        float startZOffset = 0f;
        float startYOffset = 0f;

        while (time < shakeDuration/2f)
        {
            float perlinX = Mathf.PerlinNoise(Time.time * frequencyFactor, 0);
            float perlinY = Mathf.PerlinNoise(0, Time.time * frequencyFactor);

            if (!CameraZoom.isAiming) {
                targetZOffset = Mathf.Clamp(Mathf.Lerp(-noiseMagnitudeZ, noiseMagnitudeZ, perlinX), -shakeClampZ, shakeClampZ);
                targetYOffset = Mathf.Clamp(Mathf.Lerp(-noiseMagnitudeY, noiseMagnitudeY, perlinY), -shakeClampZ, shakeClampY);
            } else {
                targetZOffset = Mathf.Clamp(Mathf.Lerp(-noiseMagnitudeZ, noiseMagnitudeZ, perlinX), -shakeClampZ/zoomDivisor, shakeClampZ/zoomDivisor);
                targetYOffset = Mathf.Clamp(Mathf.Lerp(-noiseMagnitudeY, noiseMagnitudeY, perlinY), -shakeClampZ/zoomDivisor, shakeClampY/zoomDivisor);
            }

            float t = 0;
            while (t < 1)
            {
                t += Time.deltaTime / (shakeDuration/1.25f);
                zRot = Mathf.Lerp(startZOffset, Mathf.Clamp(targetZOffset*Mathf.Infinity, -shakeClampZ, shakeClampZ), t);
                yRot = Mathf.Lerp(startYOffset, Mathf.Clamp(targetYOffset*Mathf.Infinity, -shakeClampY, shakeClampY), t);
                yield return null;
            }

            startZOffset = targetZOffset; 
            startYOffset = targetYOffset;

            time += Time.deltaTime; 
        }

        float resetTime = 0;
        while (resetTime < shakeDuration/2)
        {
            float t = resetTime / shakeDuration/2;
            zRot = Mathf.Lerp(targetZOffset, startZOffset, t);
            yRot = Mathf.Lerp(targetYOffset, startYOffset, t);
            resetTime += Time.deltaTime;
            yield return null;
        }

        zRot = 0;
        yRot = 0;
        cameraTransform.localPosition = originalPosition;
    }

    public static void StopShake()
    {
        if (CollisionControl.avatar) {
            if (shakeStartedRef && setDownRot) {
                changeDownRot = startDownSpeedRef;
                rotMod = startRotRef;
                setDownRot = false;
            }
            
            if (instance != null)
            {
                changeDownRot -= downRotationChangeRef;
                rotMod -= rotationModChangeRef;
                PlayerMovement.currentCameraRotationX -= easedRotationChange;
                easedRotationChange = 0.0f;
                instance.shakeStarted = false;
                instance.StopAllCoroutines();
                instance.ResetCameraRotation();
            }
        }
    }

    private void ResetCameraRotation()
    {
        previousERC = easedRotationChange;
    }
}
