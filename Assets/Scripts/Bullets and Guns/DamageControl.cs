using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class DamageControl : NetworkBehaviour
{
    public readonly SyncVar<float> health = new SyncVar<float>(
        180f, new SyncTypeSettings(WritePermission.ClientUnsynchronized, ReadPermission.Observers));

    [SerializeField] private int damage = 18;
    [SerializeField] private int playerSelfLayer;

    public static DamageControl Local { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Local = this;
            gameObject.layer = playerSelfLayer;
        }
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    public void Hit(float damageTaken)
    {
        health.Value -= damageTaken;
        if (health.Value <= 0)
        {
            Debug.Log("Die");
        }
    }
}
