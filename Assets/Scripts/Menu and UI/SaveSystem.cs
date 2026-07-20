using UnityEngine;
using System;
using System.Text.Json;

[System.Serializable]
public class PlayerData
{
    public float volume;
    public float rotationSpeed;
    public bool mazeKeyAcquired;
    public bool desertKeyAcquired;
    public bool iceKeyAcquired;
    public float capsuleEssence;    
    [System.Serializable]
    public struct BuildKeys
    {
        public KeyCode floorKey;
        public KeyCode wallKey;
        public KeyCode rampKey;
        public KeyCode breakKey;
    }
    public BuildKeys buildKeys;

    public int[] upgradeLevels;
    public float lifetimeKills;

    public PlayerData(float volume, float rotationSpeed, bool mazeKeyAcquired, bool desertKeyAcquired, bool iceKeyAcquired, float capsuleEssence, BuildKeys buildKeys, int[] upgradeLevels, float lifetimeKills)
    {
        this.volume = volume;
        this.rotationSpeed = rotationSpeed;
        this.mazeKeyAcquired = mazeKeyAcquired;
        this.desertKeyAcquired = desertKeyAcquired;
        this.iceKeyAcquired = iceKeyAcquired;
        this.capsuleEssence = capsuleEssence;
        this.buildKeys = buildKeys;
        this.upgradeLevels = upgradeLevels;
        this.lifetimeKills = lifetimeKills;
    }
}
public class SaveSystem : MonoBehaviour
{
    string path = Application.persistentDataPath + "/playerdata.json";
    public static void SavePlayerData()
    {
        PlayerData playerData = new PlayerData(
            SettingsController.volume,
            SettingsController.rotationSpeed,
            MaskController.mazeKeyAcquired,
            MaskController.desertKeyAcquired,
            MaskController.iceKeyAcquired,
            UpgradeManager.capsuleEssence,
            SettingsController.buildKeys,
            UpgradeManager.upgradeLevels,
            PlayerMovement.lifetimeKills
        );
        string json = JsonSerializer.Serialize(playerData);
        System.IO.File.WriteAllText(path, json);
    }
}
