using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

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
    private CallResult<LobbyMatchList_t> m_LobbyMatchList;    
    private bool browserOpen = false;
    private bool awaitingLobbyList = false;

    void Start()
    {
        roomSelectionPanel.SetActive(false);
        Camera.main.transform.position = new Vector3(4f,14.6f,-26.5f);
        Camera.main.transform.localEulerAngles = new Vector3(15,-13.5f,0);
        if (SteamManager.Initialized) {
			m_LobbyMatchList = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
		}
    }
    
	
    private void OnLobbyMatchList(LobbyMatchList_t pCallback, bool bIOFailure) {
        awaitingLobbyList = false;
        loadingCanvas.SetActive(false);

        if (bIOFailure) {
            if (!browserOpen) {
                startScreen.SetActive(true);
            }
            Instantiate(errorScreen, transform);
            return;
        }

        foreach (Transform card in roomCardContainer.transform) {
            Destroy(card.gameObject);
        }

        for (int i = 0; i < pCallback.m_nLobbiesMatching; i++) {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            createRoomCard(SteamMatchmaking.GetLobbyData(lobbyId, "name"));
        }

        roomSelectionPanel.SetActive(true);
        browserOpen = true;
    }

    private void createRoomCard(string name)
    {
        GameObject card = Instantiate(roomCard, roomCardContainer.transform);
        card.GetComponentInChildren<Text>().text = name;
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
