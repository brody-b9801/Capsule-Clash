using FishNet.Managing;
using FishNet.Transporting;
using Steamworks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FishNet.Example
{
    public class NetworkHudCanvases : MonoBehaviour
    {
        #region Types.
        /// <summary>
        /// Ways the HUD will automatically start a connection.
        /// </summary>
        private enum AutoStartType
        {
            Disabled,
            Host,
            Server,
            Client
        }
        #endregion

        #region Serialized.
        /// <summary>
        /// What connections to automatically start on play.
        /// </summary>
        [Tooltip("What connections to automatically start on play.")]
        [SerializeField]
        private AutoStartType _autoStartType = AutoStartType.Disabled;
        /// <summary>
        /// Color when socket is stopped.
        /// </summary>
        [Tooltip("Color when socket is stopped.")]
        [SerializeField]
        private Color _stoppedColor;
        /// <summary>
        /// Color when socket is changing.
        /// </summary>
        [Tooltip("Color when socket is changing.")]
        [SerializeField]
        private Color _changingColor;
        /// <summary>
        /// Color when socket is started.
        /// </summary>
        [Tooltip("Color when socket is started.")]
        [SerializeField]
        private Color _startedColor;
        [Header("Indicators")]
        /// <summary>
        /// Indicator for server state.
        /// </summary>
        [Tooltip("Indicator for server state.")]
        [SerializeField]
        private Image _serverIndicator;
        /// <summary>
        /// Indicator for client state.
        /// </summary>
        [Tooltip("Indicator for client state.")]
        [SerializeField]
        private Image _clientJoin;
        [SerializeField]
        private GameObject _loadingCanvas;
        [SerializeField]
        private Canvas _roomMenu;
        [SerializeField]
        private Canvas _gameHUD;
        [SerializeField]
        private Canvas _startScreenUI;
        [SerializeField]
        private GameObject _startDecorations;
        [SerializeField]
        private int _maxLobbyMembers = 8;
        #endregion

        #region Private.
        /// <summary>
        /// Found NetworkManager.
        /// </summary>
        private NetworkManager _networkManager;
        /// <summary>
        /// Current state of client socket.
        /// </summary>
        private LocalConnectionState _clientState = LocalConnectionState.Stopped;
        /// <summary>
        /// Current state of server socket.
        /// </summary>
        private LocalConnectionState _serverState = LocalConnectionState.Stopped;
        private bool checkStateChange = false;
        private CallResult<LobbyCreated_t> _lobbyCreated;
        private CSteamID _lobbyId = CSteamID.Nil;
        private bool _creatingLobby = false;
        private CallResult<LobbyEnter_t> _lobbyEntered;
        private bool _joiningLobby = false;
        private System.Action<string> _onJoinFailed;
        private bool _connectingToHost = false;
        private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
        private Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
        private string _hostAddress;
        private bool _migrating = false;
        private bool _waitingForHost = false;
        private float _hostWaitTimer;
        private const float HostWaitTimeout = 15f;
        private const string HostEpochKey = "HostEpoch";
        private int _hostEpoch;
#if !ENABLE_INPUT_SYSTEM
        /// <summary>
        /// EventSystem for the project.
        /// </summary>
        private EventSystem _eventSystem;
#endif
        #endregion

        void Update()
        {
            if (_migrating || _waitingForHost)
                UpdateMigration();
            else if (checkStateChange) 
                GetNextStateText(_clientState);
        }

        private void ShowLoading()
        {
            _roomMenu.enabled = false;
            _startScreenUI.enabled = false;
            _loadingCanvas.SetActive(true);
        }

        private string GetNextStateText(LocalConnectionState state)
        {
            _roomMenu.enabled = state == LocalConnectionState.Stopped;
            _loadingCanvas.SetActive(state == LocalConnectionState.Starting || state == LocalConnectionState.Stopping);
            _gameHUD.enabled = state == LocalConnectionState.Started;
            checkStateChange = _loadingCanvas.activeSelf;
            _startScreenUI.enabled = _roomMenu.enabled;
            _startDecorations.SetActive(state != LocalConnectionState.Started);
            if (state == LocalConnectionState.Stopped) 
                return "Start";
            else if (state == LocalConnectionState.Starting)
                return "Starting";
            else if (state == LocalConnectionState.Stopping) 
                return "Stopping";
            else if (state == LocalConnectionState.Started)                  
                return "Stop";
            else
                return "Invalid";
        }

        private void Start()
        {
            _networkManager = FindObjectOfType<NetworkManager>();
            if (_networkManager == null)
            {
                Debug.LogError("NetworkManager not found, HUD will not function.");
                return;
            }
            else
            {
                UpdateColor(LocalConnectionState.Stopped, ref _serverIndicator);
                UpdateColor(LocalConnectionState.Stopped, ref _clientJoin);
                _networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
                _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;
                _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
                _lobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
                _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
                _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
            }
        }

        private void OnDestroy()
        {
            if (_networkManager == null)
                return;
            _loadingCanvas.SetActive(false);
            _networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;
            _lobbyChatUpdate?.Dispose();
            _lobbyDataUpdate?.Dispose();
        }

        /// <summary>
        /// Updates img color baased on state.
        /// </summary>
        /// <param name = "state"></param>
        /// <param name = "img"></param>
        private void UpdateColor(LocalConnectionState state, ref Image img)
        {
            Color c;
            if (state == LocalConnectionState.Started) {
                c = _startedColor;
                _serverIndicator.transform.gameObject.SetActive(false);
            } else if (state == LocalConnectionState.Stopped)
                c = _stoppedColor;
            else
                c = _changingColor;
            if (state != LocalConnectionState.Started)
            {
                _serverIndicator.transform.gameObject.SetActive(true);
            }
            img.color = c;
        }

        private void ClientManager_OnClientConnectionState(ClientConnectionStateArgs obj)
        {
            _clientState = obj.ConnectionState;
            UpdateColor(obj.ConnectionState, ref _clientJoin);
            if (obj.ConnectionState == LocalConnectionState.Started)
                _connectingToHost = false;
            else if (obj.ConnectionState == LocalConnectionState.Stopped)
            {
                if (_migrating)
                    return;
                if (_connectingToHost)
                    HostConnectionFailed();
                else if (_lobbyId != CSteamID.Nil && _hostAddress != SteamUser.GetSteamID().ToString())
                {
                    _waitingForHost = true;
                    _hostWaitTimer = HostWaitTimeout;
                    CheckHostChanged();
                }
            }
        }

        private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs obj)
        {
            _serverState = obj.ConnectionState;
            UpdateColor(obj.ConnectionState, ref _serverIndicator);
        }

        public void OnClick_Server()
        {
            if (_networkManager == null)
                return;

            if (_serverState != LocalConnectionState.Stopped)
                _networkManager.ServerManager.StopConnection(true);
            else
                _networkManager.ServerManager.StartConnection();
        }

        public void OnClick_Client_Start()
        {
            if (_networkManager == null || _creatingLobby || _joiningLobby)
                return;

            try
            {
                SteamAPICall_t handle = SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, _maxLobbyMembers);     
                _lobbyCreated.Set(handle);
            }
            catch (System.InvalidOperationException)
            {
                Debug.Log("Steam is not initialized, cannot create a lobby.");
                return;
            }

            _creatingLobby = true;
            ShowLoading();
        }

        private void OnLobbyCreated(LobbyCreated_t pCallback, bool bIOFailure)
        {
            _creatingLobby = false;

            if (bIOFailure || pCallback.m_eResult != EResult.k_EResultOK)
            {
                Debug.Log("There was an error creating the lobby: " + pCallback.m_eResult);
                GetNextStateText(_clientState);
                return;
            }

            _lobbyId = new CSteamID(pCallback.m_ulSteamIDLobby);
            _hostAddress = SteamUser.GetSteamID().ToString();
            _hostEpoch = 1;
            AnnounceHost();
            SteamMatchmaking.SetLobbyData(_lobbyId, "name", SteamFriends.GetPersonaName() + "'s room");

            if (_serverState == LocalConnectionState.Stopped)
                _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
            GetNextStateText(_clientState);
        }

        public void JoinLobby(CSteamID lobbyId, System.Action<string> onFailed = null)
        {
            if (_networkManager == null || _creatingLobby || _joiningLobby)
                return;
            if (_clientState != LocalConnectionState.Stopped)
                return;

            try
            {
                SteamAPICall_t handle = SteamMatchmaking.JoinLobby(lobbyId);
                _lobbyEntered.Set(handle);
            }
            catch (System.InvalidOperationException)
            {
                Debug.Log("Steam is not initialized, cannot join a lobby.");
                onFailed?.Invoke("Steam is not running,\nrestart the game to try again");
                return;
            }

            _joiningLobby = true;
            _onJoinFailed = onFailed;
            ShowLoading();
        }

        private void OnLobbyEntered(LobbyEnter_t pCallback, bool bIOFailure)
        {
            _joiningLobby = false;

            if (bIOFailure || pCallback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                Debug.Log("There was an error joining the lobby: " + (EChatRoomEnterResponse)pCallback.m_EChatRoomEnterResponse);
                GetNextStateText(_clientState);
                _onJoinFailed?.Invoke("Failed to join room,\nit may be full or closed");
                return;
            }

            _lobbyId = new CSteamID(pCallback.m_ulSteamIDLobby);
            _hostEpoch = 0;
            _hostAddress = SteamMatchmaking.GetLobbyData(_lobbyId, "HostAddress");
            if (FindAnnouncedHost(out string announcedHost, out int announcedEpoch))
            {
                _hostAddress = announcedHost;
                _hostEpoch = announcedEpoch;
            }
            if (string.IsNullOrEmpty(_hostAddress))
            {
                Debug.Log("Lobby has no host address, cannot connect.");
                LeaveLobby();
                GetNextStateText(_clientState);
                _onJoinFailed?.Invoke("Room has no host,\ntry another room");
                return;
            }

            _connectingToHost = true;
            if (!_networkManager.ClientManager.StartConnection(_hostAddress))
                HostConnectionFailed();
            GetNextStateText(_clientState);
        }

        private void OnLobbyChatUpdate(LobbyChatUpdate_t pCallback)
        {
            if (pCallback.m_ulSteamIDLobby == _lobbyId.m_SteamID)
                CheckHostChanged();
        }

        private void OnLobbyDataUpdate(LobbyDataUpdate_t pCallback)
        {
            if (pCallback.m_ulSteamIDLobby == _lobbyId.m_SteamID)
                CheckHostChanged();
        }

        private void CheckHostChanged()
        {
            if (_lobbyId == CSteamID.Nil || _creatingLobby || _joiningLobby)
                return;

            if (FindAnnouncedHost(out string newHost, out int newEpoch))
            {
                if (newHost == _hostAddress)
                    _hostEpoch = newEpoch;
                else
                    MigrateTo(newHost, newEpoch);
            }
            else if (ShouldTakeOver())
            {
                MigrateTo(SteamUser.GetSteamID().ToString(), _hostEpoch + 1);
            }

            SyncLobbyHostAddress();
        }

        private bool FindAnnouncedHost(out string host, out int epoch)
        {
            host = null;
            epoch = _hostEpoch;
            ulong.TryParse(_hostAddress, out ulong bestId);

            CSteamID local = SteamUser.GetSteamID();
            int count = SteamMatchmaking.GetNumLobbyMembers(_lobbyId);
            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyId, i);
                if (member == local)
                    continue;
                if (!int.TryParse(SteamMatchmaking.GetLobbyMemberData(_lobbyId, member, HostEpochKey), out int memberEpoch))
                    continue;

                if (memberEpoch > epoch || (memberEpoch == epoch && member.m_SteamID < bestId))
                {
                    epoch = memberEpoch;
                    bestId = member.m_SteamID;
                    host = member.ToString();
                }
            }
            return host != null;
        }

        private bool ShouldTakeOver()
        {
            CSteamID local = SteamUser.GetSteamID();
            if (_hostAddress == local.ToString())
                return false;

            int count = SteamMatchmaking.GetNumLobbyMembers(_lobbyId);
            if (count == 0)
                return false;

            bool hostPresent = false;
            for (int i = 0; i < count; i++)
            {
                CSteamID member = SteamMatchmaking.GetLobbyMemberByIndex(_lobbyId, i);
                if (member.ToString() == _hostAddress)
                    hostPresent = true;
                else if (member.m_SteamID < local.m_SteamID)
                    return false;
            }
            return _waitingForHost || !hostPresent;
        }

        private void MigrateTo(string newHost, int newEpoch)
        {
            bool wasHost = _hostAddress == SteamUser.GetSteamID().ToString();
            Debug.Log("The host is gone, migrating to " + newHost);
            _hostAddress = newHost;
            _hostEpoch = newEpoch;
            _waitingForHost = false;
            _migrating = true;
            if (wasHost)
            {
                SteamMatchmaking.SetLobbyMemberData(_lobbyId, HostEpochKey, "");
                if (_serverState != LocalConnectionState.Stopped)
                    _networkManager.ServerManager.StopConnection(true);
            }
            if (_clientState != LocalConnectionState.Stopped)
                _networkManager.ClientManager.StopConnection();
        }

        private void AnnounceHost()
        {
            SteamMatchmaking.SetLobbyMemberData(_lobbyId, HostEpochKey, _hostEpoch.ToString());
            SyncLobbyHostAddress();
        }

        private void SyncLobbyHostAddress()
        {
            if (_lobbyId == CSteamID.Nil || SteamMatchmaking.GetLobbyOwner(_lobbyId) != SteamUser.GetSteamID())
                return;
            if (SteamMatchmaking.GetLobbyData(_lobbyId, "HostAddress") != _hostAddress)
                SteamMatchmaking.SetLobbyData(_lobbyId, "HostAddress", _hostAddress);
        }

        private void UpdateMigration()
        {
            ShowLoading();
            if (_migrating)
            {
                if (_clientState == LocalConnectionState.Stopped)
                    FinishMigration();
                return;
            }

            _hostWaitTimer -= Time.unscaledDeltaTime;
            if (_hostWaitTimer <= 0f)
                HostLost();
        }

        private void FinishMigration()
        {
            _migrating = false;
            if (_hostAddress == SteamUser.GetSteamID().ToString())
            {
                _connectingToHost = false;
                AnnounceHost();
                if (_serverState == LocalConnectionState.Stopped)
                    _networkManager.ServerManager.StartConnection();
                _networkManager.ClientManager.StartConnection();
            }
            else
            {
                _connectingToHost = true;
                if (!_networkManager.ClientManager.StartConnection(_hostAddress))
                    HostConnectionFailed();
            }
            GetNextStateText(_clientState);
        }

        private void HostLost()
        {
            Debug.Log("No new host took over, leaving the lobby.");
            LeaveLobby();
            GetNextStateText(_clientState);
            _onJoinFailed?.Invoke("Lost connection to host,\nthe room has closed");
        }

        private void HostConnectionFailed()
        {
            if (!_connectingToHost)
                return;
            _connectingToHost = false;
            Debug.Log("There was an error connecting to the lobby host.");
            LeaveLobby();
            GetNextStateText(_clientState);
            _onJoinFailed?.Invoke("Failed to connect to host,\ntry again later");
        }

        private void LeaveLobby()
        {
            if (_lobbyId == CSteamID.Nil)
                return;
            SteamMatchmaking.LeaveLobby(_lobbyId);
            _lobbyId = CSteamID.Nil;
            _hostAddress = null;
            _hostEpoch = 0;
            _migrating = false;
            _waitingForHost = false;
        }

        public void OnClick_Client_Stop()
        {
            if (_networkManager == null)
                return;

            LeaveLobby();
            if (_clientState != LocalConnectionState.Stopped)
                _networkManager.ClientManager.StopConnection();
            if (_serverState != LocalConnectionState.Stopped)
                _networkManager.ServerManager.StopConnection(true);
            GetNextStateText(_clientState);
        }
    }
}