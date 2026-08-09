using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Plain MonoBehaviour. Lives on UsernameDisplay.prefab (a UI object) and holds
// no synced state.
//
// This previously carried [ServerRpc]/[ObserversRpc] pairs converted from
// Alteruna's [SynchronizableMethod]s, but they had ZERO callers -- the only
// call sites are inside the commented-out block in Username.cs. Keeping them
// forced a NetworkObject requirement onto a UI prefab for no benefit.
//
// If username/kill replication is wired up later, the pattern to restore is:
//   [ServerRpc] void ServerSetName(string n) => RpcSetName(n);
//   [ObserversRpc(BufferLast = true)] void RpcSetName(string n) { username = n; }
// on a component that lives on the PLAYER object, not on UI.
public class UsernameControl : MonoBehaviour
{
    [SerializeField] private TMP_Text usernameDisplay;
    public float killCount;
    public string username;
}
