using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;

public class BuildHealth : NetworkBehaviour
{
    public Animator anim;
    public int maxHealth = 4;

    private readonly SyncVar<float> currentHealth = new SyncVar<float>(
        4f, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));
    [SerializeField] private GameObject build;
    public int panelStage = 0;
    public MeshRenderer transMesh;

    void Awake()
    {
        currentHealth.Value = maxHealth;
        currentHealth.OnChange += OnHealthChanged;
    }

    private void OnDestroy()
    {
        currentHealth.OnChange -= OnHealthChanged;
    }

    private void OnHealthChanged(float prev, float next, bool asServer)
    {
        if (next >= maxHealth || IsServerOnlyStarted) return;
        ApplyDamageVisuals(next);
    }

    private static Material _transMat;

    public static void removeBG()
    {
        BuildHealth[] all = FindObjectsByType<BuildHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            all[i].removeBGRef();
    }

    public void removeBGRef()
    {
        if (transMesh != null)
            transMesh.enabled = false;
    }
    public void TakeDamage(Shooting.currGun gun, float dist, NetworkObject shooter = null)
    {
        if (IsServerStarted)
            ApplyDamage(gun, dist, GunDamageModifier(shooter, gun));
        else
            ServerTakeDamage(gun, dist, shooter != null);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = false)]
    private void ServerTakeDamage(Shooting.currGun gun, float dist, bool shot, NetworkConnection conn = null)
    {
        DamageControl sender = conn != null && conn.FirstObject != null ? conn.FirstObject.GetComponent<DamageControl>() : null;
        if (sender != null && (sender.health.Value <= 0f || !IsWithinBreakRange(sender.transform.position))) return;
        ApplyDamage(gun, dist, shot ? GunDamageModifier(conn.FirstObject, gun) : 1f);
    }

    private static float GunDamageModifier(NetworkObject shooter, Shooting.currGun gun)
    {
        PlayerMovement shooterMovement = shooter != null ? shooter.GetComponent<PlayerMovement>() : null;
        return shooterMovement != null ? shooterMovement.GetGunDamageModifier(gun) : 1f;
    }

    private const float MaxBreakDistance = 10f;

    private bool IsWithinBreakRange(Vector3 from)
    {
        Collider buildCollider = GetComponent<Collider>();
        Vector3 nearest = buildCollider != null ? buildCollider.ClosestPoint(from) : transform.position;
        return (nearest - from).sqrMagnitude <= MaxBreakDistance * MaxBreakDistance;
    }

    [Server]
    private void ApplyDamage(Shooting.currGun gun, float dist, float modifier) {
        if (currentHealth.Value <= 0f) return;

        float damage;
        if (gun == Shooting.currGun.Sniper) {
            damage = 2f;
        } else if (gun != Shooting.currGun.Shotgun) {
            damage = 1f;
        } else if (dist < 3) {
            damage = 0.5f;
        } else {
            damage = Mathf.Clamp((1 - ((dist - 5) * 0.1f)) * .25f, 0.075f, 0.5f);
        }
        currentHealth.Value -= damage * modifier;

        if (currentHealth.Value <= 0f)
            ObjectSpawner.DespawnObject(build);
    }

    private void ApplyDamageVisuals(float health) {
        WallFinished wallFinished = build.GetComponent<WallFinished>();
        if (wallFinished == null)
            wallFinished = build.GetComponentInParent<WallFinished>();
        if (wallFinished == null)
            wallFinished = build.GetComponentInChildren<WallFinished>();

        if (wallFinished != null)
        {
            wallFinished.UnmergeChildren();
        }
        else
        {
            Debug.LogWarning("WallFinished component not found!");
        }

        if (transMesh != null)
        {
            if (_transMat == null)
                _transMat = new Material(transMesh.sharedMaterial);

            _transMat.color = new Color(1f, 0f, 0f, 40f / 255f);
            transMesh.sharedMaterial = _transMat;
        }

        if (anim != null)
            anim.SetInteger("Health", (int)health);
    }
}
