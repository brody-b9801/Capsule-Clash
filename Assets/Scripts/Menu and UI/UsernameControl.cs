using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Alteruna;
using TMPro;

public class UsernameControl : AttributesSync
{
    [SerializeField] private TMP_Text usernameDisplay;
    [SerializeField] private Alteruna.Avatar avatar;
    public float killCount;
    public string username;

    [SynchronizableMethod]
    public void getName(string usernameRef) {
        username = usernameRef;   
    }

    [SynchronizableMethod]
    public void getKills(float killRef) {
        killCount = killRef;   
    }
}
