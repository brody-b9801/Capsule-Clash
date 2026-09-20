using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;

public static class PlayerSceneTransfer {

    /// <summary>
    /// Server only. Loads sceneName for player's connection only and moves that player
    /// into it. Each call creates its own instance of the scene (stacking), so two
    /// players entering portal4A land in separate BossScenes rather than sharing one.
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

        sld.Options.AllowStacking = true;
        sld.Options.AutomaticallyUnload = true;

        InstanceFinder.SceneManager.LoadConnectionScenes(player.Owner, sld);
    }
}
