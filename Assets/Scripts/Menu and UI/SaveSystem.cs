using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class PlayerData
{
    public float volume;
    public float rotationSpeed;
    public bool mazeKeyAcquired;
    public bool spaceKeyAcquired;
    public bool iceKeyAcquired;
    public int capsuleEssence;
    public int killPoints;
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
    public int lifetimeKills;

    public PlayerData(float volume, float rotationSpeed, bool mazeKeyAcquired, bool spaceKeyAcquired, bool iceKeyAcquired, int capsuleEssence, int killPoints, BuildKeys buildKeys, int[] upgradeLevels, int lifetimeKills)
    {
        this.volume = volume;
        this.rotationSpeed = rotationSpeed;
        this.mazeKeyAcquired = mazeKeyAcquired;
        this.spaceKeyAcquired = spaceKeyAcquired;
        this.iceKeyAcquired = iceKeyAcquired;
        this.capsuleEssence = capsuleEssence;
        this.killPoints = killPoints;
        this.buildKeys = buildKeys;
        this.upgradeLevels = upgradeLevels;
        this.lifetimeKills = lifetimeKills;
    }
}
public class SaveSystem : MonoBehaviour
{   
    void Awake()
    {
        LoadPlayerData();
    }

    private void LoadPlayerData()
    {
        string path = Application.persistentDataPath + "/playerdata.json";
        if (!System.IO.File.Exists(path))
            return;

        PlayerData playerData;
        try
        {
            string json = System.IO.File.ReadAllText(path);
            playerData = JsonConvert.DeserializeObject<PlayerData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to load player data, using defaults: {e.Message}");
            return;
        }

        if (playerData == null)
            return;

        SettingsController.volumePercent = playerData.volume;
        SettingsController.rs = playerData.rotationSpeed;
        MaskController.mazeKeyAcquired = playerData.mazeKeyAcquired;
        MaskController.spaceKeyAcquired = playerData.spaceKeyAcquired;
        MaskController.iceKeyAcquired = playerData.iceKeyAcquired;
        upgradeManager.killPoints = playerData.capsuleEssence;
        upgradeManager.upgradePoints = playerData.killPoints;
        SettingsController.buildKeys = new SettingsController.Keys
        {
            floorKey = playerData.buildKeys.floorKey,
            wallKey  = playerData.buildKeys.wallKey,
            rampKey  = playerData.buildKeys.rampKey,
            breakKey = playerData.buildKeys.breakKey
        };
        if (playerData.upgradeLevels != null)
            upgradeManager.upgradesPurchased = playerData.upgradeLevels;
        PlayerMovement.killCount = playerData.lifetimeKills;
    }
    public static void SavePlayerData()
    {
        PlayerData playerData = new PlayerData(
            SettingsController.volumePercent,
            SettingsController.rs,
            MaskController.mazeKeyAcquired,
            MaskController.spaceKeyAcquired,
            MaskController.iceKeyAcquired,
            upgradeManager.killPoints,
            upgradeManager.upgradePoints,
            new PlayerData.BuildKeys
            {
                floorKey = SettingsController.buildKeys.floorKey,
                wallKey = SettingsController.buildKeys.wallKey,
                rampKey = SettingsController.buildKeys.rampKey,
                breakKey = SettingsController.buildKeys.breakKey
            },
            upgradeManager.upgradesPurchased,
            PlayerMovement.killCount
        );
        string json = JsonConvert.SerializeObject(playerData);
        string path = Application.persistentDataPath + "/playerdata.json";
        string tempPath = path + ".tmp";
        try
        {
            System.IO.File.WriteAllText(tempPath, json);
            if (System.IO.File.Exists(path))
                System.IO.File.Replace(tempPath, path, null);
            else
                System.IO.File.Move(tempPath, path);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to save player data: {e.Message}");
        }
    }
}
