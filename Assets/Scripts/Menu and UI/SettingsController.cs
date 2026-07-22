using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsController : MonoBehaviour
{
    public GameObject xButton;
    [System.Serializable]
    public struct Keys
    {
        public KeyCode floorKey;
        public KeyCode wallKey;
        public KeyCode rampKey;
        public KeyCode breakKey;
    }
    public static Keys buildKeys = new()
    {
        floorKey = KeyCode.X,
        wallKey  = KeyCode.Z,
        rampKey  = KeyCode.C,
        breakKey = KeyCode.V
    };

    public static float volumePercent = 1f;
    public static float rs = 10f;

    [SerializeField] private Slider volume;
    [SerializeField] private Slider rotSpeed;

    [SerializeField] private GameObject keybindsButton;
    [SerializeField] private GameObject volumeSettings;
    [SerializeField] private GameObject rotationSettings;

    [SerializeField] private TextMeshProUGUI text;

    private bool waitingForKey;
    private bool canExit = true;

    private static readonly WaitForSeconds keySetDelay = new(1f);
    private static readonly WaitForSeconds rejectDelay = new(0.75f);

    private readonly HashSet<KeyCode> usedThisSession = new();
    public static int lifetimeKills;
    public TextMeshProUGUI startScreenText;
    private static readonly HashSet<KeyCode> blockedKeys = new()
    {
        KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D,
        KeyCode.Q, KeyCode.E, KeyCode.R,
        KeyCode.LeftShift, KeyCode.RightShift,
        KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.Escape,
        KeyCode.UpArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.Mouse0, KeyCode.Mouse1,
        KeyCode.Mouse2, KeyCode.Mouse3,
        KeyCode.Mouse4, KeyCode.Mouse5,
        KeyCode.Mouse6
    };

    void Update()
    {
        xButton.SetActive(canExit);
        if (startScreenText.gameObject != null) {
            startScreenText.text = "Version: " + JSONFetcher.currentVersion + "\nLifetime Kills: " + lifetimeKills;
        }
    }

    public void UpdateVolume()
    {
        volumePercent = volume.value;
        SaveSystem.SavePlayerData();
    }

    public void UpdateRotationSpeed()
    {
        rs = rotSpeed.value;
        SaveSystem.SavePlayerData();
    }

    public void setBinds()
    {
        keybindsButton.SetActive(false);
        volumeSettings.SetActive(false);
        rotationSettings.SetActive(false);

        usedThisSession.Clear();

        text.gameObject.SetActive(true);
        StartCoroutine(KeybindRoutine());
    }

    IEnumerator KeybindRoutine()
    {
        canExit = false;
        
        yield return SetKey("WALL",  k => buildKeys.wallKey  = k);
        yield return SetKey("FLOOR", k => buildKeys.floorKey = k);
        yield return SetKey("RAMP",  k => buildKeys.rampKey  = k);
        yield return SetKey("BREAK", k => buildKeys.breakKey = k);

        SaveSystem.SavePlayerData();

        keybindsButton.SetActive(true);
        volumeSettings.SetActive(true);
        rotationSettings.SetActive(true);
        text.gameObject.SetActive(false);
        canExit = true;
    }

    IEnumerator SetKey(string name, System.Action<KeyCode> assign)
    {
        text.text = $"Press a key to set {name}";
        waitingForKey = true;

        while (waitingForKey)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyDown(key))
                    continue;

                if (blockedKeys.Contains(key))
                {
                    yield return Reject(name, $"{key} cannot be used");
                    break;
                }

                if (usedThisSession.Contains(key))
                {
                    yield return Reject(name, $"{key} already used this session");
                    break;
                }

                UnassignKeyFromOthers(key);

                assign(key);
                usedThisSession.Add(key);

                text.text = $"{name} set to {key}";
                waitingForKey = false;
                break;
            }

            yield return null;
        }
        yield return keySetDelay;
    }

    void UnassignKeyFromOthers(KeyCode key)
    {
        if (buildKeys.floorKey == key) buildKeys.floorKey = KeyCode.None;
        if (buildKeys.rampKey  == key) buildKeys.rampKey  = KeyCode.None;
        if (buildKeys.wallKey  == key) buildKeys.wallKey  = KeyCode.None;
        if (buildKeys.breakKey == key) buildKeys.breakKey = KeyCode.None;
    }

    IEnumerator Reject(string name, string message)
    {
        text.text = message;
        yield return rejectDelay;
        text.text = $"Press a key to set {name}";
    }
}
