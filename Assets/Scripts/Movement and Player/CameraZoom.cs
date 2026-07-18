using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Alteruna;
using UnityEngine.UI;
using System;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float zoomSpeed = 10.0f;
    [SerializeField] private float maxZoom = 60.0f;
    [SerializeField] private float minZoom = 20.0f;
    [SerializeField] private float sprintZoom = 65.0f;
    [SerializeField] private float gunMoveSpeed = 5.0f;
    [SerializeField] private Camera cameraToZoom;
    private float targetZoom;
    private float currentVelocity1;
    private float currentVelocity2;
    [SerializeField] private GameObject GunThing;
    [SerializeField] private GameObject Gun;
    public static bool isAiming = false;
    [SerializeField] private float zoomFloat = 1.3f;
    [SerializeField] private RectTransform left;
    [SerializeField] private RectTransform right;
    [SerializeField] private RectTransform top;
    [SerializeField] private RectTransform bottom;
    [SerializeField] private RectTransform topleft;
    [SerializeField] private RectTransform topright;
    [SerializeField] private RectTransform bottomleft;
    [SerializeField] private RectTransform bottomright;
    [SerializeField] private float zoomFOV;
    [SerializeField] private float nonZoomFOV;
    [SerializeField] private float zoomCH;
    [SerializeField] private float regCH;
    [SerializeField] private float outCH;
    [SerializeField] private float zoomSpreadOG;
    [SerializeField] private float regSpreadOG;
    [SerializeField] private float outSpreadOG;
    [SerializeField] private float zoomSpread;
    [SerializeField] private float regSpread;
    [SerializeField] private float outSpread;
    private float zoomCHFov;
    private float regCHFov;
    private float outCHFov;
    public static bool moving = false;
    public static bool shot = false;
    private float smoothTime = 0.1f;
    private float factor = 1.0f;
    float cameraToZoomFloat;
    float mainCamFloat;
    private float airFOVOffset = 0f;
    private float airFOVVelocity = 0f;
    [SerializeField] private GameObject ZoomedPos;
    [SerializeField] private float targetZoomMod;
    [SerializeField] private Transform player;
    private float crosshairDelta;
    public static float aimZoomOffset = 0f;
    public static float baseFOVTarget = 60f;

    // Cached Image components
    private Image _leftImg, _rightImg, _topImg, _bottomImg;
    private Image _topleftImg, _toprightImg, _bottomleftImg, _bottomrightImg;

    private void Start()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
        baseFOVTarget = Camera.main.fieldOfView;
        targetZoom = baseFOVTarget;
        zoomCHFov = zoomCH;
        regCHFov = regCH;
        outCHFov = outCH;

        _leftImg = left.GetComponent<Image>();
        _rightImg = right.GetComponent<Image>();
        _topImg = top.GetComponent<Image>();
        _bottomImg = bottom.GetComponent<Image>();
        _topleftImg = topleft.GetComponent<Image>();
        _toprightImg = topright.GetComponent<Image>();
        _bottomleftImg = bottomleft.GetComponent<Image>();
        _bottomrightImg = bottomright.GetComponent<Image>();
    }

    private void Update()
    {
        if (shot) {
            zoomCHFov += 5;
            regCHFov += 5;
            outCHFov += 5;
            shot = false;
        } else {
            if (zoomCHFov != zoomCH || regCHFov != regCH || outCHFov != outCH) {
                zoomCHFov = Mathf.Lerp(zoomCHFov, zoomCH, Time.deltaTime * 10);
                regCHFov = Mathf.Lerp(regCHFov, regCH, Time.deltaTime * 10);
                outCHFov = Mathf.Lerp(outCHFov, outCH, Time.deltaTime * 10);
            }
        }

        float desiredZoom;
        if ((Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && PlayerMovement.isGrounded)
        {
            isAiming = true;
            if (!moving) {
                Shooting.spread = zoomSpread;
                crosshairDelta = zoomCHFov * factor;
            } else {
                Shooting.spread = 1.5f * zoomSpread;
                crosshairDelta = zoomCHFov * 1.25f * factor;
            }
            desiredZoom = minZoom;
        }
        else if (StaminaController.zoomOut)
        {
            isAiming = false;
            if (PlayerMovement.isGrounded) {
                crosshairDelta = outCHFov * factor;
            } else {
                Shooting.spread = 1.8f * outSpread;
                crosshairDelta = outCHFov * 1.5f * factor;
            }
            desiredZoom = sprintZoom;
            GunThing.transform.position = Vector3.Lerp(GunThing.transform.position, Gun.transform.position, gunMoveSpeed * Time.deltaTime);
            GunThing.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        }
        else
        {
            isAiming = false;
            if (!PlayerMovement.isGrounded) {
                Shooting.spread = 1.8f * regSpread;
                crosshairDelta = regCHFov * 1.5f * factor;
            } else if (moving) {
                Shooting.spread = regSpread;
                crosshairDelta = regCHFov * 1.25f * factor;
            } else {
                Shooting.spread = regSpread;
                crosshairDelta = regCHFov * factor;
            }
            desiredZoom = maxZoom;
            GunThing.transform.position = Vector3.Lerp(GunThing.transform.position, Gun.transform.position, gunMoveSpeed * Time.deltaTime);
            GunThing.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        }
        targetZoom = Mathf.Lerp(targetZoom, desiredZoom, Time.deltaTime * zoomSpeed);
        
        RaycastHit hit;

        float airFOVTarget = 0f;
        if (!PlayerMovement.isGrounded && !PlayerMovement.isTeleporting)
            airFOVTarget = StaminaController.zoomOut ? targetZoomMod * 2 : targetZoomMod;
        airFOVOffset = Mathf.SmoothDamp(airFOVOffset, airFOVTarget, ref airFOVVelocity, 0.12f);
        targetZoom += airFOVOffset;
        
        // Calculate the zoom offset for PlayerMovement to use
        aimZoomOffset = targetZoom - baseFOVTarget;
        
        // Crosshair positioning
        if (!Shooting.shotgun) {
            if (!_leftImg.enabled) {
                _leftImg.enabled = true;
                _rightImg.enabled = true;
                _topImg.enabled = true;
                _bottomImg.enabled = true;    

                _topleftImg.enabled = false;
                _toprightImg.enabled = false;
                _bottomleftImg.enabled = false;
                _bottomrightImg.enabled = false;          
            }
            left.anchoredPosition = Vector2.Lerp(left.anchoredPosition, new Vector2(-crosshairDelta, left.anchoredPosition.y), smoothTime);
            right.anchoredPosition = Vector2.Lerp(right.anchoredPosition, new Vector2(crosshairDelta, right.anchoredPosition.y), smoothTime);
            top.anchoredPosition = Vector2.Lerp(top.anchoredPosition, new Vector2(top.anchoredPosition.x, crosshairDelta), smoothTime);
            bottom.anchoredPosition = Vector2.Lerp(bottom.anchoredPosition, new Vector2(top.anchoredPosition.x, -crosshairDelta), smoothTime);
        } else {
            if (_leftImg.enabled) {
                _leftImg.enabled = false;
                _rightImg.enabled = false;
                _topImg.enabled = false;
                _bottomImg.enabled = false;    
                    
                _topleftImg.enabled = true;
                _toprightImg.enabled = true;
                _bottomleftImg.enabled = true;
                _bottomrightImg.enabled = true;  
            }         
            topleft.anchoredPosition = Vector2.Lerp(topleft.anchoredPosition, new Vector2(-crosshairDelta/Mathf.Sqrt(2), crosshairDelta/Mathf.Sqrt(2)), smoothTime);
            topright.anchoredPosition = Vector2.Lerp(topright.anchoredPosition, new Vector2(crosshairDelta/Mathf.Sqrt(2), crosshairDelta/Mathf.Sqrt(2)), smoothTime);
            bottomleft.anchoredPosition = Vector2.Lerp(bottomleft.anchoredPosition, new Vector2(-crosshairDelta/Mathf.Sqrt(2), -crosshairDelta/Mathf.Sqrt(2)), smoothTime);
            bottomright.anchoredPosition = Vector2.Lerp(bottomright.anchoredPosition, new Vector2(crosshairDelta/Mathf.Sqrt(2), -crosshairDelta/Mathf.Sqrt(2)), smoothTime);       
        }
    }

    private void LateUpdate()
    {
        if (cameraToZoomFloat != Mathf.Clamp(targetZoom, 60, 80))
            cameraToZoomFloat = Mathf.SmoothDamp(cameraToZoom.fieldOfView, Mathf.Clamp(targetZoom, 60, 80), ref currentVelocity1, 0.1f);

        cameraToZoom.fieldOfView = cameraToZoomFloat + Shaker.FOVModRef*0 + PlayerMovement.dashFOV;
    }
}