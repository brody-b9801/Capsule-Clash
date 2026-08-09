using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;

// Owns the networked build-phase clock.
//
// This exists as a separate component because BuildUI is a HUD script living on
// a Canvas prefab. Any NetworkBehaviour forces Fish-Net's editor tooling to add
// a NetworkObject to transform.root (see NetworkBehaviour.TryAddNetworkObject),
// which would make the UI Canvas a network-spawned object -- wrong, and it
// re-adds itself every time Unity reserializes the prefab.
//
// Put this on a scene object (the NetworkManager object is a good home).
// BuildUI reads from BuildTimer.Instance and stays a plain MonoBehaviour.
public class BuildTimer : NetworkBehaviour
{
    public static BuildTimer Instance { get; private set; }

    // Server-authoritative: only the server advances the clock, clients read it.
    // Default ServerOnly write permission is correct here.
    private readonly SyncVar<float> syncedBuildTime = new SyncVar<float>(0f);

    // True when this process is the authority for the build clock.
    public static bool IsAuthority => InstanceFinder.IsServerStarted;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStopClient()
    {
        if (Instance == this) Instance = null;
        base.OnStopClient();
    }

    /// <summary>
    /// Advances the clock when server, otherwise returns the replicated value.
    /// </summary>
    public float Tick(float currentTotal, float deltaTime)
    {
        if (IsAuthority)
        {
            float next = currentTotal + deltaTime;
            syncedBuildTime.Value = next;
            return next;
        }

        return syncedBuildTime.Value;
    }
}
