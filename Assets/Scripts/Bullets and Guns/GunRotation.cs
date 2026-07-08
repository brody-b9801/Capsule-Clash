using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alteruna;

public class GunRotation : AttributesSync
{
    [SerializeField] private Transform gr;
    [SerializeField] private Transform gun;
    private Transform g1;
    [SerializeField] private Alteruna.Avatar avatar;

    void Start() {
        g1 = GameObject.Find("CamAKM").transform;
    }

    void Update()
    {
        if (avatar.IsOwner)
        {
            if (g1 != null && IsValidVector3(g1.transform.position) && IsValidQuaternion(g1.transform.rotation))
            {
                gunPosition(g1.transform.position - new Vector3(0, 0.35f, 0), g1.transform.rotation);
            }
        }
    }


    private void gunPosition(Vector3 pos, Quaternion rot)
    {
        if (gun != null && IsValidVector3(pos) && IsValidQuaternion(rot))
        {
            gun.transform.position = pos;
            gun.transform.rotation = rot;
            gun.transform.localEulerAngles = gun.transform.localEulerAngles - new Vector3(0, 0, 0); 
            gun.transform.localPosition = new Vector3(0.6f, gun.transform.localPosition.y, gun.transform.localPosition.z + 0.1f);
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
