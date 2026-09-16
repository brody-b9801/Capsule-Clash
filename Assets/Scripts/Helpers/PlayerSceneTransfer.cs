using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;

/// <summary>
/// Carries the spawned players into another scene without despawning them, so every
/// field, SyncVar and component state on the player survives the transition.
/// FishNet rejects DontDestroyOnLoad on a NetworkBehaviour (error FN0002); handing the
/// player objects to its own SceneManager is the supported equivalent.
/// </summary>
public static class PlayerSceneTransfer {

    /// <summary>
    /// Server only. Loads sceneName for every client and moves all player objects into it.
    /// </summary>
    public static void MovePlayerToScene(NetworkObject player, string sceneName) {
        if (!InstanceFinder.IsServerStarted) {
            Debug.LogWarning($"[PlayerSceneTransfer] Ignored '{sceneName}'; only the server may load networked scenes.");
            return;
        }

        List<NetworkObject> players = new List<NetworkObject>();
        players.Add(player);


        SceneLoadData sld = new SceneLoadData(sceneName) {
            // The players ride along instead of being destroyed with the old scene.
            MovedNetworkObjects = players.ToArray(),
            // All: also unloads the starting scene, which Unity loaded rather than FishNet.
            // Use OnlineOnly instead if you want additive/offline scenes left alone.
            ReplaceScenes = ReplaceOption.All
        };

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }
}
