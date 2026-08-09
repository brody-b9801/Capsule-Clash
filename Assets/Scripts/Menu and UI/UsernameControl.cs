using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//   [ServerRpc] void ServerSetName(string n) => RpcSetName(n);
//   [ObserversRpc(BufferLast = true)] void RpcSetName(string n) { username = n; }
public class UsernameControl : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameDisplay;
    public float killCount;
    public string username;
}
