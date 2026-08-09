using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class GunRotation : NetworkBehaviour
{
    [SerializeField] private Transform gr;
    [SerializeField] private Transform gun;
    [SerializeField] private Transform casing;
    private Transform g1;
    private Transform gm1;
    private Transform c1;
    [SerializeField] private Transform gunMag;

    void Start() {
        g1 = GameObject.Find("CamAKM").transform;
        gm1 = GameObject.Find("MC.Magazine").transform;
        c1 = GameObject.Find("CamCasing").transform;
        Debug.Log(c1.transform.position);
        if (IsOwner) {
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
            gunPosition(g1.transform.position - new Vector3(0, 0.35f, 0), g1.transform.rotation, "gun", true);
        if (gm1 != null && IsValidVector3(gm1.transform.localPosition))
            gunPosition(gm1.transform.localPosition, Quaternion.identity, "magazine", gm1.gameObject.activeSelf);
        if (c1 != null && IsValidVector3(c1.transform.position))
            gunPosition(c1.transform.localPosition, Quaternion.identity, "casing", c1.GetComponent<MeshRenderer>().enabled);
        Debug.Log(c1.transform.localPosition);
    }

    private void gunPosition(Vector3 pos, Quaternion rot, string type, bool enabled)
    {
        ref Transform thingToPosition = ref gun;
        switch (type)
        {
            case "magazine":
                thingToPosition = ref gunMag;
                break;
            case "gun":
                break;
            case "casing":
                thingToPosition = ref casing;
                break;
            default:
                return;
        }
        if (thingToPosition != null && IsValidVector3(pos) && IsValidQuaternion(rot))
        {
            if (type == "gun")
            {
                thingToPosition.transform.position = pos;
                thingToPosition.transform.rotation = rot;
            }
            //thingToPosition.transform.localEulerAngles = thingToPosition.transform.localEulerAngles - new Vector3(0, 0, 0); 
            if (type == "casing" || type == "magazine") thingToPosition.GetComponent<MeshRenderer>().enabled = enabled;
            if (type == "magazine" || type == "casing")
                thingToPosition.transform.localPosition = pos;
            else
                thingToPosition.transform.localPosition = IsOwner ? new Vector3(0.6f, thingToPosition.transform.localPosition.y - 0.1f, thingToPosition.transform.localPosition.z - 0.5f) : new Vector3(0.6f, thingToPosition.transform.localPosition.y - 0.1f, thingToPosition.transform.localPosition.z - 0.25f);
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
