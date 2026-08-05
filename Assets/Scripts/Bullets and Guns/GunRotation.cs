using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class GunRotation : AttributesSync
{
    [SerializeField] private Transform gr;
    [SerializeField] private Transform gun;
    private Transform g1;
    private Transform gm1;
    [SerializeField] private Transform gunMag;
    [SerializeField] private Alteruna.Avatar avatar;

    void Start() {
        g1 = GameObject.Find("CamAKM").transform;
        gm1 = GameObject.Find("MC.Magazine").transform;
        if (avatar.IsOwner) {
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
        if (g1 != null && IsValidVector3(g1.transform.position) && IsValidQuaternion(g1.transform.rotation))
            gunPosition(g1.transform.position - new Vector3(0, 0.35f, 0), g1.transform.rotation, false);
        if (gm1 != null && IsValidVector3(gm1.transform.localPosition) && IsValidQuaternion(gm1.transform.rotation))
            gunPosition(gm1.transform.position - new Vector3(0, 0.35f, 0), gm1.transform.rotation, true);
    }

    private void gunPosition(Vector3 pos, Quaternion rot, bool isMagazine)
    {
        ref Transform thingToPosition = ref isMagazine ? ref gunMag : ref gun;
        if (thingToPosition != null && IsValidVector3(pos) && IsValidQuaternion(rot))
        {
            thingToPosition.transform.position = pos;
            thingToPosition.transform.rotation = rot;
            thingToPosition.transform.localEulerAngles = thingToPosition.transform.localEulerAngles - new Vector3(0, 0, 0); 
            if (isMagazine)
            {
                thingToPosition.transform.localPosition = pos;
            } else
            {
                thingToPosition.transform.localPosition = avatar.IsOwner ? new Vector3(0.6f, thingToPosition.transform.localPosition.y - 0.05f, thingToPosition.transform.localPosition.z - 0.65f) : new Vector3(0.6f, thingToPosition.transform.localPosition.y, thingToPosition.transform.localPosition.z + 0.1f);

            }
        }
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
