using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadAnimation : MonoBehaviour
{
    private static Animator animator;
    public static bool ShootState = false;
    private static ReloadAnimation instance; 
    private GameObject camCasing;

    void OnEnable()
    {
        animator = GetComponent<Animator>();
        animator.SetTrigger("NoReload");
        camCasing = GameObject.Find("CamCasing");
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
        if (ShootState) {
            animator.SetTrigger("NoReload");
            ShootState = false;
        } else {
            animator.SetTrigger("Reload");
        }
    }

    void enable() {
        if (Shooting.shotgun)
            camCasing.transform.GetComponent<MeshRenderer>().enabled = true;
    }
    
    void enable2() {
        if (Shooting.shotgun && Shooting.shottieNum == 0) 
            camCasing.transform.GetComponent<MeshRenderer>().enabled = true;
    }

    void disable() {
        camCasing.transform.GetComponent<MeshRenderer>().enabled = false;
    }
}
