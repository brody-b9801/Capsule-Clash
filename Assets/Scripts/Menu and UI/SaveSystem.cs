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
    static bool loaded;

    // LoadPlayerData runs BeforeSceneLoad, when no upgradeManager exists yet, so
    // upgrade state is buffered here and applied by upgradeManager.Awake().
    static int pendingKillPoints = 100;
    static int pendingUpgradePoints = 0;
    static int[] pendingUpgradesPurchased;

    public static void ApplyPendingUpgradeData(upgradeManager manager)
    {
        manager.killPoints = pendingKillPoints;
        manager.upgradePoints = pendingUpgradePoints;
        if (pendingUpgradesPurchased != null)
            manager.upgradesPurchased = pendingUpgradesPurchased;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        loaded = false;
        LoadPlayerData();
        loaded = true;
    }

    void Awake()
    {
        if (!loaded)
            LoadPlayerData();
    }

    private static void LoadPlayerData()
    {   
        return;
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
        pendingKillPoints = playerData.capsuleEssence;
        pendingUpgradePoints = playerData.killPoints;
        SettingsController.buildKeys = new SettingsController.Keys
        {
            floorKey = playerData.buildKeys.floorKey,
            wallKey  = playerData.buildKeys.wallKey,
            rampKey  = playerData.buildKeys.rampKey,
            breakKey = playerData.buildKeys.breakKey
        };
        if (playerData.upgradeLevels != null)
            pendingUpgradesPurchased = playerData.upgradeLevels;
        PlayerMovement.killCount = playerData.lifetimeKills;

        // A manager may already exist if load is re-run after scene load.
        if (upgradeManager.Local != null)
            ApplyPendingUpgradeData(upgradeManager.Local);
    }
    public static void SavePlayerData()
    {
        return;
        // Fall back to the buffered values when saving outside a scene that has
        // an upgradeManager, so a save can't wipe loaded upgrade progress.
        upgradeManager manager = upgradeManager.Local;
        int savedKillPoints = manager != null ? manager.killPoints : pendingKillPoints;
        int savedUpgradePoints = manager != null ? manager.upgradePoints : pendingUpgradePoints;
        int[] savedUpgradesPurchased = manager != null ? manager.upgradesPurchased : pendingUpgradesPurchased;

        PlayerData playerData = new PlayerData(
            SettingsController.volumePercent,
            SettingsController.rs,
            MaskController.mazeKeyAcquired,
            MaskController.spaceKeyAcquired,
            MaskController.iceKeyAcquired,
            savedKillPoints,
            savedUpgradePoints,
            new PlayerData.BuildKeys
            {
                floorKey = SettingsController.buildKeys.floorKey,
                wallKey = SettingsController.buildKeys.wallKey,
                rampKey = SettingsController.buildKeys.rampKey,
                breakKey = SettingsController.buildKeys.breakKey
            },
            savedUpgradesPurchased,
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
