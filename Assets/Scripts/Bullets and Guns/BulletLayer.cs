using UnityEngine;
using FishNet.Object;

public class BulletLayer : NetworkBehaviour
{
    private const int LayerBullet      = 6;
    private const int LayerFiredBullet = 7;

    public override void OnStartClient()
    {
        base.OnStartClient();

        int layer = (Owner != null && Owner.IsLocalClient) ? LayerFiredBullet : LayerBullet;

        gameObject.layer = layer;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }
}
