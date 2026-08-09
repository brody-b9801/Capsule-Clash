using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class DamageControl : NetworkBehaviour
{
    // SyncVar<T> replaces Alteruna's [SynchronizableField].
    // Must be 'readonly' -- you never reassign the wrapper, only health.Value.
    //
    // ClientUnsynchronized: clients may write, but the write stays LOCAL and is
    // not sent to the server. This preserves today's behavior, where every client
    // independently runs ControlDamage against its own health copy. Damage was
    // never actually replicated under Alteruna either.
    // Phase 5 flips this to the default ServerOnly and routes writes via [ServerRpc].
    public readonly SyncVar<float> health = new SyncVar<float>(
        180f, new SyncTypeSettings(WritePermission.ClientUnsynchronized, ReadPermission.Observers));

    [SerializeField] private int damage = 18;
    [SerializeField] private int playerSelfLayer;

    public static DamageControl Local { get; private set; }

    // Replaces Alteruna's Start()/IsOwner gate. Fires on the owning client only,
    // and only once the object is actually spawned on the network.
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Local = this;
            gameObject.layer = playerSelfLayer;
        }
    }

    // NetworkBehaviour exposes real virtual lifecycle hooks, so the
    // 'private new void OnDestroy() + base.OnDestroy()' hiding trick is gone.
    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    // Currently has zero callers. Kept as the natural seam for Phase 5:
    // this becomes [ServerRpc] and every damage path routes through it.
    public void Hit(float damageTaken)
    {
        health.Value -= damageTaken;
        if (health.Value <= 0)
        {
            Debug.Log("Die");
        }
    }
}
