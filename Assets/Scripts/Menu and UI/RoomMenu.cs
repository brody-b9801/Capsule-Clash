using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using FishNet;
using FishNet.Transporting;

public class RoomMenu : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Address of the dedicated server. 'localhost' for local testing.")]
    [SerializeField] private string serverAddress = "localhost";
    [SerializeField] private ushort serverPort = 7770;
    [Tooltip("Start a local server alongside the client. Editor testing only.")]
    [SerializeField] private bool hostLocally = false;

    [SerializeField] private Text TitleText;
    [SerializeField] private Button StartButton;
    [SerializeField] private Button LeaveButton;
    public TMPro.TextMeshProUGUI connectionText;

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject x;
    [SerializeField] private GameObject contents;
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject Gun;
    [SerializeField] private GameObject Loading;
    [SerializeField] private GameObject roomText;
    [SerializeField] private GameObject startMenu;
    [SerializeField] private GameObject divider;
    [SerializeField] private GameObject titleBg;
    [SerializeField] private Camera CamTwo;
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject usernameInput;
    [SerializeField] private Canvas UICanvas;

    private void Start()
    {
        roomText.SetActive(false);
        divider.SetActive(false);
        Loading.SetActive(false);

        if (InstanceFinder.ClientManager == null)
        {
            Debug.LogError("RoomMenu: no NetworkManager found in the scene.");
            if (TitleText != null) TitleText.text = "Missing NetworkManager";
            enabled = false;
            return;
        }

        InstanceFinder.ClientManager.OnClientConnectionState += OnClientConnectionState;

        StartButton.onClick.AddListener(() =>
        {
            Loading.SetActive(true);
            GetComponent<Canvas>().enabled = false;

            if (hostLocally && InstanceFinder.ServerManager != null && !IsPortInUse(serverPort))
                InstanceFinder.ServerManager.StartConnection();

            InstanceFinder.ClientManager.StartConnection(serverAddress, serverPort);
        });

        LeaveButton.onClick.AddListener(() =>
        {
            GameObject.FindObjectsByType<GunThingAnim>(FindObjectsSortMode.None)[0].enableGun();
            transform.gameObject.GetComponent<MenuHandler>().titleStart();

            InstanceFinder.ClientManager.StopConnection();
            if (hostLocally && InstanceFinder.ServerManager != null)
                InstanceFinder.ServerManager.StopConnection(true);
        });

        if (TitleText != null) TitleText.text = "Connecting";

        if (InstanceFinder.IsClientStarted) Connected();
    }

    private void OnDestroy()
    {
        if (InstanceFinder.ClientManager != null)
            InstanceFinder.ClientManager.OnClientConnectionState -= OnClientConnectionState;
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started) Connected();
        else if (args.ConnectionState == LocalConnectionState.Stopped) Disconnected();
    }

    private void Connected()
    {
        if (TitleText != null) TitleText.text = "Connected";
        JoinedRoom();
    }

    private void Disconnected()
    {
        if (TitleText != null) TitleText.text = "Disconnected";
        LeftRoom();
    }

    private void JoinedRoom()
    {
        CamTwo.cullingMask |= (1 << LayerMask.NameToLayer("UI"));
        titleUI.SetActive(false);
        UI.SetActive(true);
        titleBg.SetActive(false);
        StartButton.interactable = false;
        LeaveButton.interactable = true;
        GetComponent<Canvas>().enabled = true;
        roomText.SetActive(true);
        divider.SetActive(true);
        Loading.SetActive(false);
        panel.SetActive(false);
        startMenu.SetActive(false);
        x.SetActive(true);
        Shooting.playerJoin = true;
        BulletText.roomName = serverAddress;
        BuildUI.started = true;

        CaptureUsername();

        usernameInput.SetActive(false);
        UICanvas.enabled = true;

        if (TitleText != null) TitleText.text = "In Game";
    }

    /// <summary>
    /// True if something is already listening on this UDP port -- i.e. another
    /// instance (a ParrelSync clone) is already hosting. Lets the first instance
    /// host and later ones join automatically, without a per-instance setting.
    /// </summary>
    private static bool IsPortInUse(ushort port)
    {
        try
        {
            var props = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            foreach (var ep in props.GetActiveUdpListeners())
            {
                if (ep.Port == port) return true;
            }
        }
        catch
        {
        }

        return false;
    }

    /// <summary>
    /// Username typed at the menu, captured while the input field is still
    /// active. PlayerMovement reads this instead of searching the scene.
    /// </summary>
    public static string TypedUsername { get; private set; } = "Player";

    private void CaptureUsername()
    {
        if (usernameInput == null) return;

        TMPro.TextMeshProUGUI t = usernameInput.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (t == null) return;

        string result = t.text.Replace(" ", "").Replace("​", "");
        TypedUsername = result.Length > 0 ? t.text : "Player";
    }

    private void LeftRoom()
    {
        CamTwo.nearClipPlane = 0.01f;
        StartButton.interactable = true;
        LeaveButton.interactable = false;
        roomText.SetActive(false);
        divider.SetActive(false);
        Loading.SetActive(false);
        titleUI.SetActive(true);
        usernameInput.SetActive(true);
        UICanvas.enabled = false;

        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("(Clone)")) Destroy(obj);
        }

        startMenu.SetActive(true);
        GetComponent<Canvas>().enabled = true;
        x.SetActive(false);
        BuildUI.started = false;

        if (TitleText != null) TitleText.text = "Menu";
    }
}
