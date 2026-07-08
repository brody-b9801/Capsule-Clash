using UnityEngine;

public class AnimationController : MonoBehaviour
{
    private static Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public static void PlayReload()
    {
        animator.SetTrigger("Reload");
    }

    public void EndReload() {
        animator.SetTrigger("NoReload");
    }
}
