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

    [ServerRpc(RequireOwnership = false)]
    public void ControlDamage(NetworkObject shooter, bool shotgun, float dist)
    {
        Debug.Log("Control damage reached");
        PlayerMovement victimMovement = GetComponent<PlayerMovement>();
        if (victimMovement == null || !victimMovement.canTakeDamage) return;
    
        float damageMultiplier = upgradeManager.Local != null ? upgradeManager.Local.damageMultiplier : 1f;

        float damageDealt;
        if (!shotgun) {
            damageDealt = 18 * damageMultiplier;
        } else {
            if (dist < 3) {
                damageDealt = 17 * damageMultiplier;
            } else {
                damageDealt = Mathf.Clamp((17 * damageMultiplier - ((dist-3)*0.6f)), 1, 15 * damageMultiplier);
            }
        }

        health.Value -= damageDealt;

        bool died = health.Value <= 0;
        if (died) {
            victimMovement.killHealSync(shooter);
        }

        ApplyDamageFeedback(died);
    }

    [ObserversRpc]
    private void ApplyDamageFeedback(bool died)
    {
        HealthController.updateHealth();

        if (died)
        {
            PlayerMovement victimMovement = GetComponent<PlayerMovement>();
            if (victimMovement != null) victimMovement.Die();
        }
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
