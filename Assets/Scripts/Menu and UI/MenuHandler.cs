using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using TMPro;

public class MenuHandler : MonoBehaviour
{
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject titleUI;
    [SerializeField] private GameObject mainCameraGun;
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject roomSelectionPanel;
    [SerializeField] private GameObject SettingsScreen;
    [SerializeField] private GameObject titleBg;
    [SerializeField] private GameObject usernameInput;
    [SerializeField] private GameObject instructions;
    [SerializeField] private GameObject loadingCanvas;

    [SerializeField] private RectTransform refreshIcon;
    [SerializeField] private GameObject roomCardContainer;
    [SerializeField] private GameObject roomCard;
    [SerializeField] private GameObject errorScreen;
    [SerializeField] private TMP_InputField JoinCodeInput;
    private CallResult<LobbyMatchList_t> m_LobbyMatchList;
    private Callback<LobbyDataUpdate_t> m_LobbyDataUpdate;
    private CSteamID pendingCodeLobby = CSteamID.Nil;
    private bool browserOpen = false;
    private bool awaitingLobbyList = false;
    private NetworkHudCanvases networkHud;
    private static MenuHandler instance;

    public static void ShowPopup(string message)
    {
        if (instance == null) {
            return;
        }

        GameObject canvasObject = new GameObject("Popup Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        CanvasScaler source = instance.GetComponentInParent<CanvasScaler>();
        if (source != null) {
            scaler.uiScaleMode = source.uiScaleMode;
            scaler.referenceResolution = source.referenceResolution;
            scaler.screenMatchMode = source.screenMatchMode;
            scaler.matchWidthOrHeight = source.matchWidthOrHeight;
        }

        GameObject popup = Instantiate(instance.errorScreen, canvasObject.transform);
        popup.GetComponent<ErrorScreen>().SetMessage(message);
        Destroy(canvasObject, 10f);
    }

    void Start()
    {
        instance = this;
        roomSelectionPanel.SetActive(false);
        networkHud = FindObjectOfType<NetworkHudCanvases>();
        Camera.main.transform.position = new Vector3(4f,14.6f,-26.5f);
        Camera.main.transform.localEulerAngles = new Vector3(15,-13.5f,0);
        if (SteamManager.Initialized) {
			m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
			m_LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
		}
    }
    
	
    private void OnLobbyMatchList(LobbyMatchList_t pCallback, bool bIOFailure) {
        awaitingLobbyList = false;
        loadingCanvas.SetActive(false);

        if (bIOFailure) {
            if (!browserOpen) {
                startScreen.SetActive(true);
            }
            showError("Failed to fetch lobby data,\nrefresh to try again");
            return;
        }

        foreach (Transform card in roomCardContainer.transform) {
            Destroy(card.gameObject);
        }

        for (int i = 0; i < pCallback.m_nLobbiesMatching; i++) {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            createRoomCard(LobbyCode.Encode(lobbyId), lobbyId);
        }

        roomSelectionPanel.SetActive(true);
        browserOpen = true;
    }

    private void createRoomCard(string name, CSteamID lobbyId)
    {
        GameObject card = Instantiate(roomCard, roomCardContainer.transform);
        card.GetComponentInChildren<Text>().text = name;
        card.GetComponentInChildren<Button>().onClick.AddListener(() => joinClicked(lobbyId));
    }

    private void joinClicked(CSteamID lobbyId)
    {
        browserBack();
        networkHud.JoinLobby(lobbyId, showError);
    }

    public void joinCodeClicked()
    {
        if (!SteamManager.Initialized || m_LobbyDataUpdate == null) {
            showError("Steam is not running,\nrestart the game to try again");
            return;
        }
        if (pendingCodeLobby != CSteamID.Nil) {
            return;
        }

        if (!LobbyCode.TryDecode(JoinCodeInput.text, out CSteamID lobbyId)) {
            showError("Invalid room code,\ncheck it and try again");
            return;
        }

        if (!SteamMatchmaking.RequestLobbyData(lobbyId)) {
            showError("Failed to look up room,\ntry again later");
            return;
        }
        pendingCodeLobby = lobbyId;
        loadingCanvas.SetActive(true);
    }

    private void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback)
    {
        if (pendingCodeLobby == CSteamID.Nil || pCallback.m_ulSteamIDLobby != pendingCodeLobby.m_SteamID) {
            return;
        }

        CSteamID lobbyId = pendingCodeLobby;
        pendingCodeLobby = CSteamID.Nil;
        loadingCanvas.SetActive(false);

        if (pCallback.m_bSuccess == 0 || SteamMatchmaking.GetLobbyData(lobbyId, "HostAddress") == "") {
            showError("Room not found,\ncheck the code and try again");
            return;
        }
        joinClicked(lobbyId);
    }

    private void showError(string message)
    {
        GameObject error = Instantiate(errorScreen, transform);
        error.GetComponent<ErrorScreen>().SetMessage(message);
    }

    public void browseClicked()
    {
        if (!SteamManager.Initialized || m_LobbyMatchList == null) {
            Debug.Log("Steam is not initialized, cannot browse lobbies.");
            return;
        }

        startScreen.SetActive(false);
        if (!browserOpen) {
            loadingCanvas.SetActive(true);
        } else {
            StartCoroutine(RefreshAnimation());
        }
        awaitingLobbyList = true;
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_LobbyMatchList.Set(handle);
    }

    IEnumerator RefreshAnimation()
    {
        while (browserOpen && awaitingLobbyList)
        {
            refreshIcon.Rotate(Vector3.forward * -360 * Time.deltaTime);
            yield return null;
        }
        refreshIcon.localEulerAngles = new Vector3(0,0,0);
    }
    public void browserBack()
    {
        startScreen.SetActive(true);
        roomSelectionPanel.SetActive(false);
        browserOpen = false;
    }

    public void settingsClicked()
    {
        startScreen.SetActive(false);
        SettingsScreen.SetActive(true);
    }

    public void settingsBack()
    {
        startScreen.SetActive(true);
        SettingsScreen.SetActive(false);
    }

    public void instructionsClicked()
    {
        startScreen.SetActive(false);
        instructions.SetActive(true);
    }

    public void instructionsBack()
    {
        startScreen.SetActive(true);
        instructions.SetActive(false);
    }

    public void titleStart()
    {
        titleScreen.SetActive(false);
        startScreen.SetActive(true);
        titleUI.SetActive(true);
        UI.SetActive(true);
        roomSelectionPanel.SetActive(false);
        SettingsScreen.SetActive(false);
        usernameInput.SetActive(true);
        StartCoroutine(titleLerp());
    }               

    IEnumerator titleLerp()
    {
        Camera cam = Camera.main;
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 endPos = new Vector3(3.42f, 10.3f, -2f);
        Quaternion endRot = Quaternion.Euler(new Vector3(8, -204.16f, 0));
        
        float time = 0f;
        float totalTime = 0.5f;

        while (time < totalTime)
        {
            float t = time / totalTime;
            t = t * t * (3f - 2f * t);

            cam.transform.position = Vector3.Lerp(startPos, endPos, t);
            cam.transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            time += Time.deltaTime;
            yield return null;
        }
        cam.transform.position = endPos;
        cam.transform.rotation = endRot;
    }
                                                                                                             
}
