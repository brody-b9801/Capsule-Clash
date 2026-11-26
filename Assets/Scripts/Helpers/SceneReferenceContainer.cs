using UnityEngine;
[DefaultExecutionOrder(-100)]

public class SceneReferenceContainer : MonoBehaviour
{
    public GameObject staminaBar;
    public GameObject staminaBlack;
    public static SceneReferenceContainer Local { get; private set; }

    void Awake()
    {
        Local = this;
    }

    void OnDestroy()
    {
        if (Local == this) Local = null;
    }
}
