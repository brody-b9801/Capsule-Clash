using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadAnimation : MonoBehaviour
{
    private static Animator animator;
    public static bool ShootState = false;
    private GameObject camCasing;
    private MeshRenderer casingRenderer;

    // frame to jump to when skipping the second-shell load
    private const float SkipToFrame = 100f;
    private const float ClipFrameCount = 120f;
    private const int SkipLayer = 0;
    private bool pendingSkip;

    void OnEnable()
    {
        Animator ownAnimator = GetComponent<Animator>();
        if (ownAnimator == null) return;

        animator = ownAnimator;
        animator.SetTrigger("NoReload");

        camCasing = SceneLookup.FindInactive("CamCasing");
        if (camCasing != null)
            casingRenderer = camCasing.GetComponent<MeshRenderer>();
    }
    public static void PlayReload()
    {
        if (animator == null) return;

        ShootState = false;
        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("NoReload");
        animator.speed = upgradeManager.Local != null ? upgradeManager.Local.reloadSpeedMultiplier : 1f;
        animator.SetTrigger("Reload");
    }

    public void EndReload()
    {
        ShootState = false;
        PlayerMovement.lerpingWalkDone = false;
        animator.speed = 1f;
        animator.SetTrigger("NoReload");
    }

    public static void PlayAnim()
    {
        if (animator == null) return;

        animator.SetTrigger("Shoot");
        ShootState = true;
    }

    public void EndAnim()
    {
        if (ShootState)
        {
            animator.SetTrigger("NoReload");
            ShootState = false;
        }
        else if (Shooting.Local != null && !Shooting.Local.reloading)
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
        if (Shooting.Local != null && Shooting.Local.shotgun)
            SetCasingVisible(true);
    }

    public void enable2()
    {
        if (Shooting.Local == null) return;

        if (Shooting.Local.shotgun && Shooting.Local.shottieNum == 0)
            SetCasingVisible(true);
        else if (Shooting.Local.shotgun)
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
        if (animator == null) return;

        AnimatorStateInfo state = animator.IsInTransition(SkipLayer)
            ? animator.GetNextAnimatorStateInfo(SkipLayer)
            : animator.GetCurrentAnimatorStateInfo(SkipLayer);

        animator.Play(state.fullPathHash, SkipLayer, SkipToFrame / ClipFrameCount);

        SetCasingVisible(false);
    }
}