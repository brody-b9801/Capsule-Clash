using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using TMPro;

public class UsernameControl : NetworkBehaviour
{
    [SerializeField] private TMP_Text usernameDisplay;
    public float killCount;
    public string username;

    // Alteruna's [SynchronizableMethod] had no direction -- BroadcastRemoteMethod
    // sent to everyone. FishNet splits that into two explicit hops:
    // owner -> server ([ServerRpc]), then server -> all clients ([ObserversRpc]).
    // BufferLast = true replays the latest value to players who join later.
    [ServerRpc]
    public void ServerSetName(string usernameRef) => RpcSetName(usernameRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetName(string usernameRef) {
        username = usernameRef;
    }

    [ServerRpc]
    public void ServerSetKills(float killRef) => RpcSetKills(killRef);

    [ObserversRpc(BufferLast = true)]
    private void RpcSetKills(float killRef) {
        killCount = killRef;
    }
}
