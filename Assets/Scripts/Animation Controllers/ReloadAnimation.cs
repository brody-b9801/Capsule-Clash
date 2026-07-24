using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadAnimation : MonoBehaviour
{
    private static Animator animator;
    public static bool ShootState = false;
    private static ReloadAnimation instance;
    private GameObject camCasing;
    private MeshRenderer casingRenderer;

    // frame to jump to when skipping the second-shell load
    private const float SkipToFrame = 100f;
    private const float ClipFrameCount = 120f;
    private const int SkipLayer = 0;

    private bool pendingSkip;

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("NoReload");

        camCasing = GameObject.Find("CamCasing");
        if (camCasing != null)
            casingRenderer = camCasing.GetComponent<MeshRenderer>();
    }
    public static void PlayReload()
    {
        ShootState = false;
        animator.speed = upgradeManager.reloadSpeedMultiplier;
        animator.SetTrigger("Reload");
    }

    public void EndReload()
    {
        ShootState = false;
        PlayerMovement.lerpingWalkDone = false;
        animator.SetTrigger("NoReload");
    }

    public static void PlayAnim()
    {
        if (CollisionControl.avatar)
        {
            animator.SetTrigger("Shoot");
            ShootState = true;
        }
    }

    public void EndAnim()
    {
        if (ShootState)
        {
            animator.SetTrigger("NoReload");
            ShootState = false;
        }
        else
        {
            animator.SetTrigger("Reload");
        }
    }

    private void SetCasingVisible(bool visible)
    {
        if (casingRenderer != null)
            casingRenderer.enabled = visible;
    }

    public void enable()
    {
        if (Shooting.shotgun)
            SetCasingVisible(true);
    }

    public void enable2()
    {
        Debug.Log(Shooting.shottieNum);
        if (Shooting.shotgun && Shooting.shottieNum == 0) 
            SetCasingVisible(true);
        else if (Shooting.shotgun)
            pendingSkip = true;
    }

    public void disable1()
    {
        SetCasingVisible(false);
    }

    public void disable2()
    {
        SetCasingVisible(false);
    }

    public void disable3()
    {
        SetCasingVisible(false);
    }

    void LateUpdate()
    {
        if (!pendingSkip) return;
        pendingSkip = false;

        AnimatorStateInfo state = animator.IsInTransition(SkipLayer)
            ? animator.GetNextAnimatorStateInfo(SkipLayer)
            : animator.GetCurrentAnimatorStateInfo(SkipLayer);

        animator.Play(state.fullPathHash, SkipLayer, SkipToFrame / ClipFrameCount);

        SetCasingVisible(false);
    }
}