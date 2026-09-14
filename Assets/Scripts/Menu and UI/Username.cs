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

    private PlayerMovement player;
    private string sentName;
    private float sentKills = -1f;

    void Awake()
    {
        player = GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        setRotation();
        GetInfo();
    }


    public void GetInfo() {
        if (!IsOwner || player == null || player.username == null) return;

        if (player.username != sentName) {
            sentName = player.username;
            ServerSetName(sentName);
        }
        if (player.killCount != sentKills) {
            sentKills = player.killCount;
            ServerSetKills(sentKills);
        }
    }

    [ServerRpc]
    private void ServerSetName(string usernameRef) => RpcSetName(usernameRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetName(string usernameRef) {
        username = usernameRef;
        if (usernameDisplay != null) usernameDisplay.text = usernameRef;
        if (!IsOwner && player != null) player.username = usernameRef;
        //RefreshLeaderboard();
    }

    [ServerRpc]
    private void ServerSetKills(float killRef) => RpcSetKills(killRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetKills(float killRef) {
        killCount = killRef;
        if (!IsOwner && player != null) player.killCount = (int)killRef;
        //RefreshLeaderboard();
    }

    // private void RefreshLeaderboard()
    // {
    //     if (player == null) return;
    //     LeaderboardControl lb = player.GetComponent<LeaderboardControl>();
    //     if (lb != null) lb.UpdateLB();
    // }

    public void setRotation()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.LookAt(cam.transform);

        Vector3 adjustedRotation = transform.eulerAngles;
        transform.eulerAngles = new Vector3(-adjustedRotation.x, adjustedRotation.y - 180, adjustedRotation.z);
    }
}
