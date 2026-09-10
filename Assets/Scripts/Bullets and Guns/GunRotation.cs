using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class GunRotation : NetworkBehaviour
{
    [SerializeField] private Transform gr;
    [SerializeField] private Transform gun;
    [SerializeField] private Transform casing;       
    [SerializeField] private Transform gunMag;
    private Transform g1;
    private Transform gm1;
    private Transform c1;
    Vector3 magPos;
    Vector3 magPosPrev;
    Vector3 casingPos;
    Vector3 casingPosPrev;
    public override void OnStartClient() {
        base.OnStartClient();

        if (IsOwner) {
            g1 = SceneLookup.FindInactive("CamAKM").transform;
            gm1 = SceneLookup.FindInactive("MC.Magazine").transform;
            c1 = SceneLookup.FindInactive("CamCasing").transform;
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
    }

    void Update()
    {
        positionGun();

        casingPos = c1.transform.localPosition;
        if (casingPos != casingPosPrev)
        {
            syncCasingServer(c1.transform.localPosition, c1.GetComponent<MeshRenderer>().enabled);
        }
        casingPosPrev = casingPos;

        magPos = gm1.transform.localPosition;
        if (magPos != magPosPrev)
        {
            syncMagServer(gm1.transform.localPosition, gm1.GetComponent<MeshRenderer>().enabled);
        }
        magPosPrev = magPos;
    }
    
    private void positionGun()
    {
        if (g1 != null && IsValidVector3(g1.transform.position) && IsValidQuaternion(g1.transform.rotation))
            gun.transform.position = g1.transform.position - new Vector3(0, 0.35f, 0);
            gun.transform.rotation = g1.transform.rotation;
    }

    [ServerRpc]
    private void syncCasingServer(Vector3 pos, bool enabled)
    {
        syncCasing(pos, enabled);
    }

    [ObserversRpc]
    private void syncCasing(Vector3 pos, bool enabled)
    {
        casing.localPosition = pos;
        casing.GetComponent<MeshRenderer>().enabled = enabled;
    }
    [ServerRpc]
    private void syncMagServer(Vector3 pos, bool enabled)
    {
        syncMag(pos, enabled);
    }

    [ObserversRpc]
    private void syncMag(Vector3 pos, bool enabled)
    {
        gunMag.localPosition = pos;
        gunMag.GetComponent<MeshRenderer>().enabled = enabled;
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
