using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using TMPro;

public class Username : NetworkBehaviour
{
    [SerializeField] private TMP_Text usernameDisplay;
    public float killCount;
    public string username;

    void Update()
    {
        setRotation();
        GetInfo();
    }

    public void GetInfo() {
        if (IsOwner && PlayerMovement.Local.username != null) {
            ServerSetName(PlayerMovement.Local.username);
            ServerSetKills(PlayerMovement.Local.killCount);
        }
    }

    [ServerRpc]
    private void ServerSetName(string usernameRef) => RpcSetName(usernameRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetName(string usernameRef) {
        username = usernameRef;
    }

    [ServerRpc]
    private void ServerSetKills(float killRef) => RpcSetKills(killRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetKills(float killRef) {
        killCount = killRef;
    }

    public void setRotation()
    {
        transform.LookAt(Camera.main.transform);

        Vector3 adjustedRotation = transform.eulerAngles;
        transform.eulerAngles = new Vector3(-adjustedRotation.x, adjustedRotation.y - 180, adjustedRotation.z);
    }
}
