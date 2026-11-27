using UnityEngine;
using UnityEngine.UI;
using TMPro;
[DefaultExecutionOrder(-100)]

public class SceneReferenceContainer : MonoBehaviour
{
    public GameObject staminaBar;
    public GameObject staminaBlack;

    [Header("Camera Gun")]
    public GameObject camAKM;
    public GameObject camCasing;
    public GameObject camMagazine;
    public MeshRenderer camQuad;
    public Transform camBulletHole;
    public GameObject bulletSpawn;
    public Transform casingSpawn;
    public Camera cameraTwo;
    public Transform gunThing;

    [Header("HUD")]
    public Image reloadBar;
    public GameObject reloadBarBg;
    public TextMeshProUGUI dashText;
    public RectTransform dashIcon;
    public TextMeshProUGUI maskText;

    [Header("Portals")]
    public GameObject portal1A;
    public GameObject portal2A;
    public GameObject portal3A;
    public GameObject portal4A;
    public GameObject portal4B;
    public GameObject serverPortal;

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
