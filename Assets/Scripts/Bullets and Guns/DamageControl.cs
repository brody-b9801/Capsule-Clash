using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class DamageControl : NetworkBehaviour
{
    public const float MaxHealth = 180f;
    public const float MigrationRestoreWindow = 60f;

    private struct MigrationSnapshot
    {
        public float health;
        public float buildNum;
        public float time;
    }

    private static readonly Dictionary<string, MigrationSnapshot> migrationSnapshots = new Dictionary<string, MigrationSnapshot>();

    public readonly SyncVar<string> steamId = new SyncVar<string>(
        "", new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));

    public readonly SyncVar<float> health = new SyncVar<float>(
        MaxHealth, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));

    public readonly SyncVar<bool> spawnProtected = new SyncVar<bool>(
        false, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));

    public readonly SyncVar<float> damageMultiplier = new SyncVar<float>(
        1f, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));

    [SerializeField] private int damage = 18;
    [SerializeField] private float maxSpawnProtectionTime = 15f;

    private Coroutine spawnProtectionFailsafe;

    public static DamageControl Local { get; private set; }

    private void Awake()
    {
        health.OnChange += OnHealthChanged;
    }

    private void OnDestroy()
    {
        health.OnChange -= OnHealthChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        GrantSpawnProtection();
        if (Owner.IsValid) steamId.Value = Owner.GetAddress();
        RestoreMigrationSnapshot();
    }

    private void RestoreMigrationSnapshot()
    {
        string id = steamId.Value;
        if (string.IsNullOrEmpty(id) || !migrationSnapshots.TryGetValue(id, out MigrationSnapshot snapshot)) return;
        migrationSnapshots.Remove(id);

        float now = Time.realtimeSinceStartup;
        if (now - NetworkHudCanvases.LastMigrationTime > MigrationRestoreWindow) return;
        if (now - snapshot.time > MigrationRestoreWindow || snapshot.health <= 0f) return;

        health.Value = Mathf.Min(snapshot.health, MaxHealth);
        ObjectSpawner spawner = GetComponent<ObjectSpawner>();
        if (spawner != null) spawner.RestoreBuildNum(snapshot.buildNum);
    }

    private void CaptureMigrationSnapshot()
    {
        if (string.IsNullOrEmpty(steamId.Value)) return;

        ObjectSpawner spawner = GetComponent<ObjectSpawner>();
        migrationSnapshots[steamId.Value] = new MigrationSnapshot
        {
            health = health.Value,
            buildNum = spawner != null ? spawner.buildNum : 25f,
            time = Time.realtimeSinceStartup
        };
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (IsOwner)
        {
            Local = this;
            SetDamageMultiplier(upgradeManager.Local != null ? upgradeManager.Local.getDamageMulti() : 1f);
            HealthController.updateHealth();
        }
    }

    [ServerRpc]
    public void SetDamageMultiplier(float multiplier)
    {
        damageMultiplier.Value = Mathf.Clamp(multiplier, 1f, 4f);
    }

    public override void OnStopClient()
    {
        CaptureMigrationSnapshot();
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    private void OnHealthChanged(float prev, float next, bool asServer)
    {
        if (asServer || !IsOwner) return;
        HealthController.updateHealth();
    }

    public bool ControlDamage(NetworkObject shooter, Shooting.currGun gun, float dist)
    {
        if (!IsServerInitialized || !CanBeDamaged()) return false;

        DamageControl shooterDamage = shooter != null ? shooter.gameObject.GetComponent<DamageControl>() : null;
        float damageMultiplier = shooterDamage != null ? shooterDamage.damageMultiplier.Value : 1f;

        float damageDealt;
        if (gun == Shooting.currGun.Sniper) {
            damageDealt = 90 * damageMultiplier;
        } else if (gun != Shooting.currGun.Shotgun) {
            damageDealt = 18 * damageMultiplier;
        } else {
            if (dist < 3) {
                damageDealt = 17 * damageMultiplier;
            } else {
                damageDealt = Mathf.Clamp((17 * damageMultiplier - ((dist-3)*0.6f)), 1, 15 * damageMultiplier);
            }
        }

        ApplyDamage(damageDealt, shooter);
        return true;
    }

    private bool CanBeDamaged() => health.Value > 0f && !spawnProtected.Value;

    private void ApplyDamage(float amount, NetworkObject shooter)
    {
        health.Value -= amount;

        bool died = health.Value <= 0;
        if (died && shooter != null) {
            DamageControl shooterDamage = shooter.GetComponent<DamageControl>();
            if (shooterDamage != null && shooterDamage != this && shooterDamage.health.Value > 0f)
                shooterDamage.health.Value = MaxHealth;

            PlayerMovement victimMovement = GetComponent<PlayerMovement>();
            if (victimMovement != null) victimMovement.killHealSync(shooter);
        }

        ApplyDamageFeedback(died);
    }

    [ObserversRpc]
    private void ApplyDamageFeedback(bool died)
    {
        ChangeMat changeMat = GetComponent<ChangeMat>();
        if (changeMat != null) changeMat.FlashDamaged();

        if (died)
        {
            PlayerMovement victimMovement = GetComponent<PlayerMovement>();
            if (victimMovement != null) victimMovement.Die();
        }
    }

    public void Hit(float damageTaken)
    {
        if (!IsServerInitialized || !CanBeDamaged()) return;
        ApplyDamage(damageTaken, null);
    }

    [ServerRpc]
    public void ServerApplyFallDamage(float amount)
    {
        if (amount <= 0f || health.Value <= 0f) return;
        ApplyDamage(Mathf.Min(amount, MaxHealth), null);
    }

    [ServerRpc]
    public void ServerKillSelf()
    {
        if (health.Value <= 0f) return;
        health.Value = 0f;
    }

    [ServerRpc]
    public void ServerHeal(float amount)
    {
        if (amount <= 0f || health.Value <= 0f) return;
        health.Value = Mathf.Clamp(health.Value + amount, 0f, MaxHealth);
    }

    [ServerRpc]
    public void ServerRespawn()
    {
        if (health.Value > 0f) return;
        health.Value = MaxHealth;
        GrantSpawnProtection();

        ObjectSpawner spawner = GetComponent<ObjectSpawner>();
        if (spawner != null) spawner.buildNum = 25;
    }

    [ServerRpc]
    public void ServerEndSpawnProtection()
    {
        EndSpawnProtection();
    }

    private void GrantSpawnProtection()
    {
        spawnProtected.Value = true;
        if (spawnProtectionFailsafe != null) StopCoroutine(spawnProtectionFailsafe);
        spawnProtectionFailsafe = StartCoroutine(SpawnProtectionFailsafe());
    }

    private void EndSpawnProtection()
    {
        if (spawnProtectionFailsafe != null)
        {
            StopCoroutine(spawnProtectionFailsafe);
            spawnProtectionFailsafe = null;
        }
        spawnProtected.Value = false;
    }

    IEnumerator SpawnProtectionFailsafe()
    {
        yield return new WaitForSeconds(maxSpawnProtectionTime);
        spawnProtectionFailsafe = null;
        spawnProtected.Value = false;
    }
}
