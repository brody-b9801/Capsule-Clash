using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;

// UNUSED. This component is not on any prefab or scene object, so Instance was
// always null and BuildUI silently fell back to a host-only clock, freezing the
// build ring on clients. The clock now rides on ObjectSpawner's _buildTime
// SyncVar. Do not wire this up alongside it — pick one.
public class BuildTimer : NetworkBehaviour
{
    public static BuildTimer Instance { get; private set; }

    private readonly SyncVar<float> syncedBuildTime = new SyncVar<float>(0f);

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
