using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;

public class BuildHealth : NetworkBehaviour
{
    public Animator anim;
    public int maxHealth = 4;

    // ClientUnsynchronized mirrors today's behavior: buildDamageSync ran on every
    // client independently, each mutating its own copy. Phase 5 makes this
    // ServerOnly so the server is the single source of truth for build health.
    private readonly SyncVar<float> currentHealth = new SyncVar<float>(
        4f, new SyncTypeSettings(WritePermission.ClientUnsynchronized, ReadPermission.Observers));
    [SerializeField] private GameObject build;
    public int panelStage = 0;
    public MeshRenderer transMesh;

    void Awake()
    {
        currentHealth.Value = maxHealth;

        if (transMesh == null)
        {
            Transform cube = transform.Find("Cube");
            if (cube == null && build != null)
                cube = build.transform.Find("Cube");
            if (cube != null)
                transMesh = cube.GetComponent<MeshRenderer>();
        }
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
    // Bullets from ANY player damage builds, and the build is not owned by the
    // shooter, so RequireOwnership must be false or the RPC is rejected.
    // RunLocally = true keeps the shooter's own feedback instant instead of
    // waiting a round trip -- matching how the old broadcast felt.
    public void TakeDamage(bool shotgun, float dist)
    {
        ServerTakeDamage(shotgun, dist);
    }

    [ServerRpc(RequireOwnership = false, RunLocally = true)]
    private void ServerTakeDamage(bool shotgun, float dist) => RpcBuildDamage(shotgun, dist);

    [ObserversRpc(ExcludeServer = true)]
    private void RpcBuildDamage(bool sg, float dist) => buildDamageSync(sg, dist);

    private void buildDamageSync(bool sg, float dist) {
        // Unmerge combined meshes so damage is visible
        WallFinished wallFinished = build.GetComponent<WallFinished>();
        if (wallFinished == null)
            wallFinished = build.GetComponentInParent<WallFinished>(); // Try parent
        if (wallFinished == null)
            wallFinished = build.GetComponentInChildren<WallFinished>(); // Try children
        
        if (wallFinished != null)
        {
            wallFinished.UnmergeChildren();
            Debug.Log("Meshes unmerged on damage");
        }
        else
        {
            Debug.LogWarning("WallFinished component not found!");
        }

        if (!sg) {
            currentHealth.Value--;
        } else {
            if (dist < 3) {
                currentHealth.Value -= 0.5f;
            } else {
                currentHealth.Value -= Mathf.Clamp((1-((dist-5)*0.1f))*.25f, 0.075f, 0.5f);
                Debug.Log((1-((dist-5)*0.1f)));
            }
        }
        if (_transMat == null)
        {
            _transMat = new Material(transMesh.sharedMaterial);
        }
        _transMat.color = new Color(1f, 0f, 0f, 40f / 255f);
        transMesh.sharedMaterial = _transMat;
        if (anim != null)
            anim.SetInteger("Health", (int)currentHealth.Value);
        if ((int)currentHealth.Value <= 0)
        {
            ObjectSpawner.DespawnObject(build);
        }
    }     
}
