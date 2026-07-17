using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsController : MonoBehaviour
{
    public GameObject xButton;

    public static KeyCode floorKey = KeyCode.X;
    public static KeyCode wallKey  = KeyCode.Z;
    public static KeyCode rampKey  = KeyCode.C;
    public static KeyCode breakKey = KeyCode.V;

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

    private HashSet<KeyCode> usedThisSession = new HashSet<KeyCode>();
    public static int lifetimeKills;
    private int sessionKillsSaved = 0;
    public TextMeshProUGUI startScreenText;
    private static readonly HashSet<KeyCode> blockedKeys = new HashSet<KeyCode>
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

    const string PREF_FLOOR = "key_floor";
    const string PREF_RAMP  = "key_ramp";
    const string PREF_WALL  = "key_wall";
    const string PREF_BREAK = "key_break";
    const string PREF_RS    = "rot_speed";
    const string PREF_VOL   = "volume";

    const string PREF_KILLS = "lifetime_kills";

    void Awake()
    {
        LoadSettings();

        lifetimeKills = PlayerPrefs.GetInt(PREF_KILLS, 0);
        sessionKillsSaved = 0; // Reset for new session
        rotSpeed.value = rs;
        volume.value = volumePercent;
    }

    void Update()
    {
        xButton.SetActive(canExit);
        if (startScreenText.gameObject != null) {
            startScreenText.text = "Version: " + JSONFetcher.currentVersion + "\nLifetime Kills: " + lifetimeKills;
        }
    }

    public void updateValues()
    {
        Debug.Log("Updating settings values");
        rs = rotSpeed.value;
        volumePercent = volume.value;
        Debug.Log(volumePercent);
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        
        if (playerMovement != null)
        {
            int currentKills = (int)playerMovement.killCount;
            int newKillsThisSession = currentKills - sessionKillsSaved;
            
            // Only add the new kills since last save
            if (newKillsThisSession > 0)
            {
                lifetimeKills = PlayerPrefs.GetInt(PREF_KILLS, 0) + newKillsThisSession;
                sessionKillsSaved = currentKills;
            }
        }

        SaveSettings();
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
        
        yield return SetKey("WALL",  k => wallKey  = k);
        yield return SetKey("FLOOR", k => floorKey = k);
        yield return SetKey("RAMP",  k => rampKey  = k);
        yield return SetKey("BREAK", k => breakKey = k);

        SaveSettings();

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
        updateValues();
        yield return new WaitForSeconds(1f);
    }

    void UnassignKeyFromOthers(KeyCode key)
    {
        if (floorKey == key) floorKey = KeyCode.None;
        if (rampKey  == key) rampKey  = KeyCode.None;
        if (wallKey  == key) wallKey  = KeyCode.None;
        if (breakKey == key) breakKey = KeyCode.None;
    }

    IEnumerator Reject(string name, string message)
    {
        text.text = message;
        yield return new WaitForSeconds(0.75f);
        text.text = $"Press a key to set {name}";
    }
    void SaveSettings()
    {
        PlayerPrefs.SetInt(PREF_FLOOR, (int)floorKey);
        PlayerPrefs.SetInt(PREF_RAMP,  (int)rampKey);
        PlayerPrefs.SetInt(PREF_WALL,  (int)wallKey);
        PlayerPrefs.SetInt(PREF_BREAK, (int)breakKey);

        PlayerPrefs.SetFloat(PREF_RS, rs);
        PlayerPrefs.SetFloat(PREF_VOL, volumePercent);
        
        PlayerPrefs.SetInt(PREF_KILLS, lifetimeKills);

        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        floorKey = (KeyCode)PlayerPrefs.GetInt(PREF_FLOOR, (int)floorKey);
        rampKey  = (KeyCode)PlayerPrefs.GetInt(PREF_RAMP,  (int)rampKey);
        wallKey  = (KeyCode)PlayerPrefs.GetInt(PREF_WALL,  (int)wallKey);
        breakKey = (KeyCode)PlayerPrefs.GetInt(PREF_BREAK, (int)breakKey);

        rs            = PlayerPrefs.GetFloat(PREF_RS, rs);
        volumePercent = PlayerPrefs.GetFloat(PREF_VOL, volumePercent);
    }
}
