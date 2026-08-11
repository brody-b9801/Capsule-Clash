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

    // Server-authoritative: only the server writes health, observers read it.
    // Previously ClientUnsynchronized, which let every client decrement its own
    // private copy and decide independently when the build died.
    private readonly SyncVar<float> currentHealth = new SyncVar<float>(
        4f, new SyncTypeSettings(WritePermission.ServerOnly, ReadPermission.Observers));
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
    public void TakeDamage(bool shotgun, float dist)
    {
        Debug.Log($"Taking damage: shotgun={shotgun}, dist={dist}");
        ServerTakeDamage(shotgun, dist);
    }

    /// <summary>
    /// Applies the health change on the server only, then tells observers to play
    /// the visual reaction. Despawning happens here — once, on the authority —
    /// rather than inside the observers RPC where every client raced to do it.
    /// </summary>
    [ServerRpc(RequireOwnership = false, RunLocally = false)]
    private void ServerTakeDamage(bool shotgun, float dist) {
        Debug.Log($"Server received damage: shotgun={shotgun}, dist={dist}");

        // Already dead and awaiting despawn — ignore further hits so a burst of
        // shots cannot despawn the same build more than once.
        if (currentHealth.Value <= 0f) return;

        if (!shotgun) {
            currentHealth.Value--;
        } else if (dist < 3) {
            currentHealth.Value -= 0.5f;
        } else {
            currentHealth.Value -= Mathf.Clamp((1 - ((dist - 5) * 0.1f)) * .25f, 0.075f, 0.5f);
        }

        RpcBuildDamage();

        if (currentHealth.Value <= 0f)
            ObjectSpawner.DespawnObject(build);
    }

    /// <summary>Visual reaction only — no health arithmetic, no despawn.</summary>
    [ObserversRpc(ExcludeServer = false)]
    private void RpcBuildDamage()
    {
        ApplyDamageVisuals();
    }

    private void ApplyDamageVisuals() {
        WallFinished wallFinished = build.GetComponent<WallFinished>();
        if (wallFinished == null)
            wallFinished = build.GetComponentInParent<WallFinished>(); // Try parent
        if (wallFinished == null)
            wallFinished = build.GetComponentInChildren<WallFinished>(); // Try children

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
            anim.SetInteger("Health", (int)currentHealth.Value);
    }
}
