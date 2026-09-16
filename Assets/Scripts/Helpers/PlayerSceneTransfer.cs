using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;

public static class PlayerSceneTransfer {

    /// <summary>
    /// Server only. Loads sceneName for every client and moves all player objects into it.
    /// </summary>
    public static void MovePlayerToScene(NetworkObject player, string sceneName) {
        if (!InstanceFinder.IsServerStarted) {
            Debug.LogWarning($"[PlayerSceneTransfer] Ignored '{sceneName}'; only the server may load networked scenes.");
            return;
        }

        List<NetworkObject> players = new List<NetworkObject>() {player};
        SceneLoadData sld = new SceneLoadData(sceneName) {
            MovedNetworkObjects = players.ToArray(),
            ReplaceScenes = ReplaceOption.All
        };
        InstanceFinder.SceneManager.LoadConnectionScenes(player.Owner, sld);

    }
}
