using UnityEngine;

[System.Serializable]
public class PlayerData : MonoBehaviour
{
    public float volume;
    public float rotationSpeed;
    public bool mazeKeyAcquired;
    public bool desertKeyAcquired;
    public bool iceKeyAcquired;
    public float capsuleEssence;
    [System.Serializable]
    struct BuildKeys
    {
        public KeyCode floorKey;
        public KeyCode wallKey;
        public KeyCode rampKey;
        public KeyCode breakKey;
    }
    public int[] upgradeLevels;
    public float lifetimeKills;
}
public class SaveSystem : MonoBehaviour
{
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
