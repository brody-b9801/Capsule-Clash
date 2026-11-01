using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;

public class GunRotation : NetworkBehaviour
{
    [SerializeField] private Transform gr;
    [SerializeField] private Transform gun;   
    [SerializeField] private Transform casing;
    [SerializeField] private Transform gunMag;

    private Transform g1;
    private Transform gm1;
    private Transform c1;

    private MeshRenderer casingRenderer;
    private MeshRenderer magRenderer;
    private MeshRenderer c1Renderer;
    private MeshRenderer gm1Renderer;

    private Vector3 casingPosPrev;
    private Vector3 magPosPrev;
    private bool casingEnabledPrev;
    private bool magEnabledPrev;
    private bool sentOnce;
    private Transform mainCam;

    public override void OnStartClient()
    {
        base.OnStartClient();

        casingRenderer = casing.GetComponent<MeshRenderer>();
        magRenderer = gunMag.GetComponent<MeshRenderer>();
        g1 = SceneLookup.FindInactive("CamAKM").transform;
        gm1 = SceneLookup.FindInactive("MC.Magazine").transform;
        c1 = SceneLookup.FindInactive("CamCasing").transform;
        mainCam = Camera.main.transform;
        if (!IsOwner) return;

        c1Renderer = c1.GetComponent<MeshRenderer>();
        gm1Renderer = gm1.GetComponent<MeshRenderer>();

        Transform rendererContainer = transform.GetChild(0);
        for (int i = 0; i < rendererContainer.childCount; i++)
        {
            Transform child = rendererContainer.GetChild(i);
            if (child.TryGetComponent<Renderer>(out Renderer renderer))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;

        positionGun();

        if (c1 != null && IsValidVector3(c1.localPosition))
        {
            Vector3 pos = c1.localPosition;
            bool enabled = c1Renderer.enabled;

            if (!sentOnce || pos != casingPosPrev || enabled != casingEnabledPrev)
            {
                casingPosPrev = pos;
                casingEnabledPrev = enabled;
                syncCasingServer(pos, enabled);
            }
        }

        if (gm1 != null && IsValidVector3(gm1.localPosition))
        {
            Vector3 pos = gm1.localPosition;
            bool enabled = gm1Renderer.enabled;

            if (!sentOnce || pos != magPosPrev || enabled != magEnabledPrev)
            {
                magPosPrev = pos;
                magEnabledPrev = enabled;
                syncMagServer(pos, enabled);
            }
        }

        sentOnce = true;
    }

    private float x = 0.45f;
    private float y = -0.4f;
    private float z = -0.35f;
    private void positionGun()
    {
        bool decrease = Input.GetKey(KeyCode.H);
        if (Input.GetKeyDown(KeyCode.J)) 
            x += decrease ? -0.05f : 0.05f;
        if (Input.GetKeyDown(KeyCode.K)) 
            y += decrease ? -0.05f : 0.05f;
        if (Input.GetKeyDown(KeyCode.L))
            z += decrease ? -0.05f : 0.05f;

        if (g1 == null || gun == null) return;
        if (!IsValidVector3(g1.position) || !IsValidQuaternion(g1.rotation)) return;
        bool aiming = PlayerMovement.Local.isAiming;
        gun.SetPositionAndRotation(g1.position + transform.rotation * new Vector3(aiming ? x + 0.1f : x, y, aiming ? z + 0.1f : z), g1.rotation);
        //gr.rotation = Quaternion.Euler(new Vector3(0,0,0));
        Debug.Log(gr.rotation.eulerAngles.x);
        Debug.Log($"[{x}, {y}, {z}]");
    }

    [ServerRpc(RunLocally = true)]
    private void syncCasingServer(Vector3 pos, bool enabled, Channel channel = Channel.Unreliable)
    {
        if (IsServerInitialized)
            syncCasing(pos, enabled);
        else
            applyCasing(pos, enabled);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void syncCasing(Vector3 pos, bool enabled, Channel channel = Channel.Unreliable)
        => applyCasing(pos, enabled);

    private void applyCasing(Vector3 pos, bool enabled)
    {
        if (!IsValidVector3(pos)) return;

        casing.localPosition = pos;
        casingRenderer.enabled = enabled;
    }

    [ServerRpc(RunLocally = true)]
    private void syncMagServer(Vector3 pos, bool enabled, Channel channel = Channel.Unreliable)
    {
        if (IsServerInitialized)
            syncMag(pos, enabled);
        else
            applyMag(pos, enabled);
    }

    [ObserversRpc(ExcludeOwner = true)]
    private void syncMag(Vector3 pos, bool enabled, Channel channel = Channel.Unreliable)
        => applyMag(pos, enabled);

    private void applyMag(Vector3 pos, bool enabled)
    {
        if (!IsValidVector3(pos)) return;

        gunMag.localPosition = pos;
        magRenderer.enabled = enabled;
    }

    private bool IsValidVector3(Vector3 vector)
    {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z));
    }

    private bool IsValidQuaternion(Quaternion quaternion)
    {
        return !(float.IsNaN(quaternion.x) || float.IsNaN(quaternion.y) || float.IsNaN(quaternion.z) || float.IsNaN(quaternion.w));
    }
}