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

    public override void OnStartClient()
    {
        base.OnStartClient();

        casingRenderer = casing.GetComponent<MeshRenderer>();
        magRenderer = gunMag.GetComponent<MeshRenderer>();

        if (!IsOwner) return;

        g1 = SceneLookup.FindInactive("CamAKM").transform;
        gm1 = SceneLookup.FindInactive("MC.Magazine").transform;
        c1 = SceneLookup.FindInactive("CamCasing").transform;

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

    private void positionGun()
    {
        if (g1 == null || gun == null) return;
        if (!IsValidVector3(g1.position) || !IsValidQuaternion(g1.rotation)) return;

        gun.SetPositionAndRotation(g1.position - new Vector3(0f, 0.35f, 0f), g1.rotation);
    }

    [ServerRpc(RunLocally = true)]
    private void syncCasingServer(Vector3 pos, bool enabled, Channel channel = Channel.Unreliable)
    {
        if (IsServerInitialized)
            syncCasing(pos, enabled);
        else
            applyCasing(pos, enabled);   // owner's RunLocally pass
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