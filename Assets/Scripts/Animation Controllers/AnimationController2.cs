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
        if (CollisionControl.avatar) {
            animator.SetTrigger("Shoot");
        }
    }

    public void EndAnim() {
        if (CollisionControl.avatar) {
            animator.SetTrigger("NoShoot");
        }
    }
}
