using UnityEngine;

public class AnimationController2 : MonoBehaviour
{
    private static Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public static void PlayAnim()
    {
        animator.SetTrigger("Shoot");
    }

    public void EndAnim() {
        animator.SetTrigger("NoShoot");
    }
}
