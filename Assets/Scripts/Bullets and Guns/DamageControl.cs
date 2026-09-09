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

    public readonly SyncVar<float> damageMultiplier = new SyncVar<float>(
        1f, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));

    [SerializeField] private int damage = 18;

    public static DamageControl Local { get; private set; }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Local = this;
            SetDamageMultiplier(upgradeManager.Local != null ? upgradeManager.Local.getDamageMulti() : 1f);
        }
    }

    [ServerRpc]
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier.Value = Mathf.Clamp(multiplier, 1f, 4f);
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    public void ControlDamage(NetworkObject shooter, bool shotgun, float dist)
    {
        Debug.Log("Control damage reached");
        PlayerMovement victimMovement = GetComponent<PlayerMovement>();
        if (victimMovement == null || !victimMovement.canTakeDamage) return;
    
        DamageControl shooterDamage = shooter != null ? shooter.gameObject.GetComponent<DamageControl>() : null;
        float damageMultiplier = shooterDamage != null ? shooterDamage.damageMultiplier.Value : 1f;

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
