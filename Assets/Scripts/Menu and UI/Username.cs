using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Alteruna;
using TMPro;

public class Username : AttributesSync
{
    [SerializeField] private TMP_Text usernameDisplay;
    [SerializeField] private Alteruna.Avatar avatar;
    public static float killCount;
    public static string username;

    void Update()
    {
        //setRotation();
    }

    /*public void GetInfo() {
        if (avatar.IsOwner && PlayerMovement.username != null && PlayerMovement.killCount != null) {
            BroadcastRemoteMethod(0, PlayerMovement.username);
            BroadcastRemoteMethod(1, PlayerMovement.killCount);
        }
    }

    [SynchronizableMethod]
    public void getName(string usernameRef) {
        username = usernameRef;   
    }

    [SynchronizableMethod]
    public void getKills(float killRef) {
        killCount = killRef;   
    }*/
    public void setRotation()
    {
        transform.LookAt(Camera.main.transform);

        Vector3 adjustedRotation = transform.eulerAngles;
        transform.eulerAngles = new Vector3(-adjustedRotation.x, adjustedRotation.y - 180, adjustedRotation.z);
    }
}
