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

        if (transMesh == null)
        {
            Transform cube = transform.Find("Cube");
            if (cube == null && build != null)
                cube = build.transform.Find("Cube");
            if (cube != null)
                transMesh = cube.GetComponent<MeshRenderer>();
        }
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
    public void TakeDamage(Shooting.currGun gun, float dist)
    {
        Debug.Log($"Taking damage: gun={gun}, dist={dist}");
        if (IsServerStarted)
            ApplyDamage(gun, dist);
        else
            ServerTakeDamage(gun, dist);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = false)]
    private void ServerTakeDamage(Shooting.currGun gun, float dist, NetworkConnection conn = null)
    {
        DamageControl sender = conn != null && conn.FirstObject != null ? conn.FirstObject.GetComponent<DamageControl>() : null;
        if (sender != null && sender.health.Value <= 0f) return;
        ApplyDamage(gun, dist);
    }

    [Server]
    private void ApplyDamage(Shooting.currGun gun, float dist) {
        Debug.Log($"Server received damage: gun={gun}, dist={dist}");

        if (currentHealth.Value <= 0f) return;

        if (gun == Shooting.currGun.Sniper) {
            currentHealth.Value -= 2f;
        } else if (gun != Shooting.currGun.Shotgun) {
            currentHealth.Value--;
        } else if (dist < 3) {
            currentHealth.Value -= 0.5f;
        } else {
            currentHealth.Value -= Mathf.Clamp((1 - ((dist - 5) * 0.1f)) * .25f, 0.075f, 0.5f);
        }

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
