using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;

public static class PlayerSceneTransfer {

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

        sld.Options.AllowStacking = true;
        sld.Options.AutomaticallyUnload = true;

        InstanceFinder.SceneManager.LoadConnectionScenes(player.Owner, sld);
    }
}
