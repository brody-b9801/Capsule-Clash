using System.Collections;
using UnityEngine;
using TMPro;
using FishNet.Object;

public class ServerController : MonoBehaviour {
    private const string FeedPrompt = "Press Space to Feed Me 5 Capsules, I MUST GROW";

    public static ServerController Local { get; private set; }

    public int keyCount = 0;
    [SerializeField] private GameObject keyPrefab;

    private Camera playerCamera;
    private TextMeshProUGUI serverText;
    private GameObject portal4;

    private bool ServerSpeaking = false;
    private Coroutine activeServerCoroutine = null;
    private Coroutine activeSceneCoroutine = null;

    private Transform mazeServerTransform;
    private Transform spaceServerTransform;
    private Transform iceServerTransform;

    private bool seeingMazeServer = false;
    private bool seeingSpaceServer = false;
    private bool seeingIceServer = false;

    private bool seenServerMaze = false;
    private bool seenSpaceServer = false;
    private bool seenIceServer = false;

    public bool mazeKeyAcquired = false;
    public bool spaceKeyAcquired = false;
    public bool iceKeyAcquired = false;
    public static bool serverAnimationPlaying = false;

    private bool desertEntered = false;
    private bool mazeEntered = false;
    private bool spaceEntered = false;
    private bool iceEntered = false;
    private Transform cam;
    private BuildUI ui;
    private GunThingAnim gun;

    private readonly string[] generationVerbs = { "Generating", "Working", "Producing", "Thinking", "Calculating", "Contemplating", "Processing", "Analyzing", "Computing", "Synthesizing" };

    public bool LookingAtServer => seeingMazeServer || seeingSpaceServer || seeingIceServer;
    private bool AllKeysAcquired => mazeKeyAcquired && spaceKeyAcquired && iceKeyAcquired;

    private void Awake() {
        SaveSystem.ApplyPendingServerData(this);
    }

    private void OnEnable() {
        TryClaimLocal();
    }

    /// <summary>
    /// This is a MonoBehaviour on the networked player prefab, so it has no
    /// IsOwner of its own — ownership is read from the sibling NetworkObject.
    /// Claiming in Awake unconditionally (as before) let the last player to
    /// spawn overwrite Local with a remote player's controller.
    /// </summary>
    private void TryClaimLocal() {
        NetworkObject nob = GetComponentInParent<NetworkObject>();
        // Ownership is not populated until the object is spawned; Initialize()
        // re-runs this for the owner once startup has progressed far enough.
        if (nob != null && nob.IsSpawned && !nob.IsOwner) return;
        if (nob == null || nob.IsOwner) Local = this;
    }

    private void OnDestroy() {
        if (Local == this) Local = null;
    }

    public void Initialize(Camera camera) {
        // Called from PlayerMovement's owner-gated startup — by now ownership is
        // known for certain, so make sure the local player holds the claim.
        Local = this;
        playerCamera = camera;
        cam = camera.transform;
        serverText = GameObject.Find("MaskText").GetComponent<TextMeshProUGUI>();
        portal4 = GameObject.Find("portal4");
        mazeServerTransform = GameObject.Find("MaskMaze").transform;
        spaceServerTransform = GameObject.Find("MaskSpace").transform;
        iceServerTransform = GameObject.Find("MaskIce").transform;
        keyCount = 0;
        ui = GameObject.FindObjectsByType<BuildUI>(FindObjectsSortMode.None)[0];
        gun = GameObject.FindObjectsByType<GunThingAnim>(FindObjectsSortMode.None)[0];
    }

    public void BeginOpeningScene() {
        activeSceneCoroutine = StartCoroutine(StartDesertScene());
    }

    // -------------------------------------------------------------------------
    // Server / Cutscene helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stops any running server coroutines and resets all related state so the
    /// next scene can (start cleanly, even if the player switches dimensions
    /// mid-dialogue.
    /// </summary>
    private void ResetServerState() {
        if (activeServerCoroutine != null) {
            StopCoroutine(activeServerCoroutine);
            activeServerCoroutine = null;
        }
        if (activeSceneCoroutine != null) {
            StopCoroutine(activeSceneCoroutine);
            activeSceneCoroutine = null;
        }
        ServerSpeaking = false;
        if (serverText != null) {
            serverText.text = "";
            serverText.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    /// <summary>Typewriter effect coroutine. Always go through StartServerSpeak().</summary>
    IEnumerator ServerSpeak(string input) {
        ServerSpeaking = true;
        Color color = serverText.color;
        serverText.color = new Color(0.871f, 0.451f, 0.337f, 1f);
        serverText.text = generationVerbs[Random.Range(0, generationVerbs.Length)] + "...";
        yield return new WaitForSeconds(Random.Range(0.2f, 2f));
        serverText.color = color;

        for (int i = 0; i < input.Length; i += Random.Range(1, 5)) {
            serverText.text = input.Substring(0, Mathf.Min(i + 1, input.Length));
            yield return new WaitForSeconds(Random.Range(0, 0.1f));
        }
        serverText.text = input;
        yield return new WaitForSeconds(3f);
        serverText.text = "";
        serverText.color = new Color(1f, 1f, 1f, 1f);
        ServerSpeaking = false;
    }

    /// <summary>Helper that tracks the active server coroutine so it can be cancelled.</summary>
    private void StartServerSpeak(string input) {
        if (activeServerCoroutine != null) StopCoroutine(activeServerCoroutine);
        activeServerCoroutine = StartCoroutine(ServerSpeak(input));
    }

    private void OpenFinalPortal() {
        if (portal4 != null) portal4.transform.GetChild(0).GetComponent<FaceTextToPlayer>().PortalOpen();
    }

    // -------------------------------------------------------------------------
    // Detection and interaction
    // -------------------------------------------------------------------------

    private void Update() {
        if (playerCamera == null) return;
        // The local player is spawned by FishNet after this component starts.
        if (PlayerMovement.Local == null) return;

        UpdateServerDetection();
        UpdateServerPrompts();
        if (!serverAnimationPlaying) {
            RotateServer();
        }
    }

    private void UpdateServerDetection() {
        seeingMazeServer = false;
        seeingSpaceServer = false;
        seeingIceServer = false;

        if (PlayerMovement.Local.currDimension == "Desert") return;

        GameObject serverToDetect = null;
        ref bool seeing = ref seeingMazeServer;
        ref bool keyAcquired = ref mazeKeyAcquired;
        switch (PlayerMovement.Local.currDimension)
        {
            case "Maze": serverToDetect = mazeServerTransform.gameObject;  break;
            case "Space": serverToDetect = spaceServerTransform.gameObject; seeing = ref seeingSpaceServer; keyAcquired = ref spaceKeyAcquired; break;
            case "Ice": serverToDetect = iceServerTransform.gameObject; seeing = ref seeingIceServer; keyAcquired = ref iceKeyAcquired; break;
        }
        if (!keyAcquired && Vector3.Distance(playerCamera.transform.position, serverToDetect.transform.position) < 5f && Vector3.Angle(playerCamera.transform.forward, serverToDetect.transform.position - playerCamera.transform.position) < 30f) {
            seeing = true;
        }
    }

    private void UpdateServerPrompts() {
        if (seeingMazeServer) {
            if (!seenServerMaze) {
                InterruptWithServerScene(ServerMazeScene());
                seenServerMaze = true;
            } else if (!ServerSpeaking) {
                serverText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Maze" && serverText.text == FeedPrompt) {
            serverText.text = "";
        }

        if (seeingSpaceServer) {
            if (!seenSpaceServer) {
                InterruptWithServerScene(ServerSpaceScene());
                seenSpaceServer = true;
            } else if (!ServerSpeaking) {
                serverText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Space" && serverText.text == FeedPrompt) {
            serverText.text = "";
        }

        if (seeingIceServer) {
            if (!seenIceServer) {
                InterruptWithServerScene(ServerIceScene());
                seenIceServer = true;
            } else if (!ServerSpeaking) {
                serverText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Ice" && serverText.text == FeedPrompt) {
            serverText.text = "";
        }
    }

    /// <summary>
    /// Cancels whatever dialogue is currently playing (typically a dimension-entry
    /// scene) and immediately starts the given server scene. Walking up to a server
    /// always takes priority over the "Welcome to the ..." sequence.
    /// </summary>
    private void InterruptWithServerScene(IEnumerator serverScene) {
        ResetServerState();
        activeSceneCoroutine = StartCoroutine(serverScene);
    }

    public void TryFeed() {
        if (seeingMazeServer && !mazeKeyAcquired)
            Feed(ref mazeKeyAcquired, StartMazeKeyCutscene());
        else if (seeingSpaceServer && !spaceKeyAcquired)
            Feed(ref spaceKeyAcquired, StartSpaceKeyCutscene());
        else if (seeingIceServer && !iceKeyAcquired)
            Feed(ref iceKeyAcquired, StartIceKeyCutscene());
    }

    private void Feed(ref bool keyAcquired, IEnumerator cutscene) {
        if (upgradeManager.Local.killPoints >= 5) {
            upgradeManager.Local.killPoints -= 5;
            keyAcquired = true;
            keyCount++;
            ResetServerState();
            activeSceneCoroutine = StartCoroutine(cutscene);
            SaveSystem.SavePlayerData();
        } else if (activeSceneCoroutine == null) {
            activeSceneCoroutine = StartCoroutine(InsufficientPoints());
        }
    }

    private void RotateServer() {
        Transform server = null;
        switch (PlayerMovement.Local.currDimension) {
            case "Maze": server = mazeServerTransform; break;
            case "Space": server = spaceServerTransform; break;
            case "Ice": server = iceServerTransform; break;
        }
        if (server != null) {
            server.rotation = Quaternion.LookRotation(server.position - playerCamera.transform.position);
        }
    }

    // -------------------------------------------------------------------------
    // Dimension entry
    // -------------------------------------------------------------------------

    /// <summary>
    /// Entry point for all dimension-change scene sequences.
    /// Always resets server state first so switching dimensions mid-dialogue
    /// never leaves orphaned coroutines.
    /// </summary>
    public void DisplayDimension() {
        ResetServerState();
        activeSceneCoroutine = StartCoroutine(DisplayDimensionAfterPixelize(PlayerMovement.Local.currDimension));
    }

    private IEnumerator DisplayDimensionAfterPixelize(string dimension) {
        while (RetroDither.TeleportPixelizeActive) yield return null;

        if (dimension == "Desert")
            yield return StartDesertScene();
        else if (dimension == "Maze")
            yield return StartMazeScene();
        else if (dimension == "Space")
            yield return StartSpaceScene();
        else if (dimension == "Ice")
            yield return StartIceScene();
    }

    // -------------------------------------------------------------------------
    // Scene coroutines
    // -------------------------------------------------------------------------

    IEnumerator StartDesertScene() {
        while (ServerSpeaking) yield return null;
        if (desertEntered) {
            StartServerSpeak("Welcome back to the Desert");
        } else {
            StartServerSpeak("Welcome to the Desert");
            desertEntered = true;
            while (ServerSpeaking) yield return null;
            StartServerSpeak("Defeat Capsules to obtain their essence");
        }
        while (ServerSpeaking) yield return null;

        Color initialColor = serverText.color;
        serverText.color = new Color(1f, 0f, 0f, 1f);

        if (!AllKeysAcquired)
            StartServerSpeak("FEED ME CAPSULES, I LURK IN FAR DIMENSIONS, I MUST GROW");
        else {
            StartServerSpeak("THE WHITE ROOM BECKONS YOU");
            OpenFinalPortal();
        }

        while (ServerSpeaking) yield return null;
        serverText.color = initialColor;
        activeSceneCoroutine = null;
    }

    IEnumerator StartMazeScene() {
        while (ServerSpeaking) yield return null;
        if (mazeEntered) {
            StartServerSpeak("Welcome back to the Maze");
        } else {
            StartServerSpeak("Welcome to the Maze");
            mazeEntered = true;
            while (ServerSpeaking) yield return null;
            StartServerSpeak("Jump upgrades, dashing, and building are disabled here");
        }
        while (ServerSpeaking) yield return null;

        if (!mazeKeyAcquired) {
            Color initialColor = serverText.color;
            serverText.color = new Color(1f, 0f, 0f, 1f);
            StartServerSpeak("I AM NEAR, FIND ME, I MUST GROW");
            while (ServerSpeaking) yield return null;
            serverText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator StartSpaceScene() {
        while (ServerSpeaking) yield return null;
        if (spaceEntered) {
            StartServerSpeak("Welcome back to Space");
        } else {
            StartServerSpeak("Welcome to Space");
            spaceEntered = true;
            while (ServerSpeaking) yield return null;
            StartServerSpeak("Gravity and dashing cooldowns are  reduced, you can jump higher");
        }
        while (ServerSpeaking) yield return null;

        if (!spaceKeyAcquired) {
            Color initialColor = serverText.color;
            serverText.color = new Color(1f, 0f, 0f, 1f);
            StartServerSpeak("I AM NEAR, FIND ME, FEED ME CAPSULES, I MUST GROW");
            while (ServerSpeaking) yield return null;
            serverText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator StartIceScene() {
        while (ServerSpeaking) yield return null;
        iceKeyAcquired = false;
        if (iceEntered) {
            StartServerSpeak("Welcome back to the Tundra");
        } else {
            StartServerSpeak("Welcome to the Tundra");
            iceEntered = true;
            while (ServerSpeaking) yield return null;
            StartServerSpeak("A slippery dimension where shooting propels you forward");
        }
        while (ServerSpeaking) yield return null;

        if (!iceKeyAcquired) {
            Color initialColor = serverText.color;
            serverText.color = new Color(1f, 0f, 0f, 1f);
            StartServerSpeak("I AM NEAR, FIND ME, FEED ME CAPSULES, I MUST GROW");
            while (ServerSpeaking) yield return null;
            serverText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator ServerMazeScene() {
        while (ServerSpeaking) yield return null;

        if (!seenServerMaze)
            StartServerSpeak("You have done well to find me");

        while (ServerSpeaking) yield return null;

        StartServerSpeak(FeedPrompt);
        activeSceneCoroutine = null;
    }

    IEnumerator ServerSpaceScene() {
        while (ServerSpeaking) yield return null;

        if (!seenSpaceServer)
            StartServerSpeak("You have done well to find me");

        while (ServerSpeaking) yield return null;

        StartServerSpeak("Press Space to Feed Me 5 Capsules");
        activeSceneCoroutine = null;
    }

    IEnumerator ServerIceScene() {
        while (ServerSpeaking) yield return null;

        if (!seenIceServer)
            StartServerSpeak("You have done well to find me");

        while (ServerSpeaking) yield return null;

        StartServerSpeak(FeedPrompt);
        activeSceneCoroutine = null;
    }

    public IEnumerator StartFirstKillScene() {
        while (ServerSpeaking) yield return null;

        StartServerSpeak("Congratulations on your first kill, Capsule");

        while (ServerSpeaking) yield return null;

        StartServerSpeak("Press I to open your inventory and see your capsule essences");

        while (ServerSpeaking) yield return null;
        Color initialColor = serverText.color;
        serverText.color = new Color(1f, 0f, 0f, 1f);
        StartServerSpeak("COLLECT MORE ESSENCES AND FIND ME, I HUNGER");

        while (ServerSpeaking) yield return null;

        serverText.color = initialColor;
        activeSceneCoroutine = null;
    }

    // The three key cutscenes were near-identical copies, and the Space one had
    // drifted: it ran KeyCutsceneEnd twice, so the camera lerped home and then
    // straight back out again, leaving it stranded until the next LateUpdate
    // snapped it. Sharing one body means whichever server the player feeds last
    // ends the same way, and the return lerp can only ever run once.
    IEnumerator StartMazeKeyCutscene() => KeyCutscene(
        "MaskMaze",
        "For your efforts, I reward you with upgrade tokens",
        "Press T to open the upgrade menu and spend your tokens, and I to view your inventory",
        "Why am I here?",
        "I LURK ELSEWHERE, FIND ME, FEED ME, YOUR QUESTIONS WILL BE ANSWERED");

    IEnumerator StartSpaceKeyCutscene() => KeyCutscene(
        "MaskSpace",
        "For your efforts, I reward you with upgrade tokens",
        "Press T to open the upgrade menu and spend your tokens and Press I to view your keys",
        "What am I?",
        "I LURK ELSEWHERE, FIND ME, FEED ME, I MUST GROW");

    IEnumerator StartIceKeyCutscene() => KeyCutscene(
        "MaskIce",
        "For your efforts, I reward you with a key and upgrade tokens",
        "Press T to open the upgrade menu and spend your tokens and Press I to view your keys",
        "Who are you?",
        "I LURK ELSEWHERE, FIND ME, FEED ME, I MUST GROW");

    /// <summary>
    /// Pans the camera off the player, plays the reward dialogue for one server,
    /// then lerps the camera back. The dialogue branches on AllKeysAcquired, but
    /// the camera bookends do not, so the last server fed returns the view exactly
    /// like the first two.
    /// </summary>
    private IEnumerator KeyCutscene(string serverName, string rewardLine, string tokenHintLine, string question, string lurkLine) {
        while (ServerSpeaking) yield return null;

        GameObject serverObj = GameObject.Find(serverName);
        Vector3 posChange = GetKeySceneMovement();
        Quaternion camRot = cam.rotation;
        yield return StartCoroutine(KeyCutsceneStart(posChange));

        StartServerSpeak("You have fed me well, Capsule");
        while (ServerSpeaking) yield return null;

        StartServerSpeak(rewardLine);
        while (ServerSpeaking) yield return null;

        if (serverObj != null) yield return StartCoroutine(GiveKey(serverObj.transform.position));
        upgradeManager.Local.upgradePoints += 12;
        SaveSystem.SavePlayerData();

        if (!AllKeysAcquired) {
            StartServerSpeak(tokenHintLine);
            while (ServerSpeaking) yield return null;
        }

        Color initialColor = serverText.color;
        serverText.color = new Color(0f, 1f, 0f, 1f);
        StartServerSpeak(question);
        while (ServerSpeaking) yield return null;

        serverText.color = new Color(1f, 0f, 0f, 1f);
        if (!AllKeysAcquired) {
            StartServerSpeak(lurkLine);
        } else {
            StartServerSpeak("YOU HAVE PROVEN YOUR WORTH, THE LOCKED PORTAL HAS BEEN UNSEALED");
            OpenFinalPortal();
        }
        while (ServerSpeaking) yield return null;
        serverText.color = initialColor;

        if (serverObj != null) serverObj.SetActive(false);
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));

        activeSceneCoroutine = null;
    }

    private Vector3 GetKeySceneMovement()
    {
        Transform server = getServerTransform();
        Vector3 startPosition = cam.position;
        float distToServer = Vector3.Distance(startPosition, server.position);
        Vector3 endPosition = startPosition + -transform.forward * distToServer + transform.right * distToServer + transform.up * distToServer;
        return endPosition - startPosition;
    }

    IEnumerator KeyCutsceneStart(Vector3 posChange)
    {
        Transform server = getServerTransform();
        ui.disableUI();
        gun.disableGun();
        serverAnimationPlaying = true;
        Shooting.Local.canShoot = false;

        float lerpTime = 1f;
        float timeAcc = 0f;
        Vector3 startPosition = cam.position;
        cam.LookAt(server);
        Vector3 startRotation = cam.rotation.eulerAngles;
        Vector3 endRotation = cam.rotation.eulerAngles + new Vector3(14.04f, -90f, 0f);
        while (timeAcc < lerpTime) {
            timeAcc += Time.deltaTime;
            cam.position = Vector3.Lerp(startPosition, startPosition + posChange, timeAcc / lerpTime);
            cam.LookAt(server.position - transform.forward);
            yield return null;
        }
        yield break;
    }

    IEnumerator KeyCutsceneEnd(Vector3 posChange, Quaternion endRot)
    {
        float lerpTime = 1f;
        float timeAcc = 0f;
        Vector3 startPosition = cam.position;
        Quaternion startRot = cam.rotation;
        while (timeAcc < lerpTime) {
            timeAcc += Time.deltaTime;
            cam.position = Vector3.Lerp(startPosition, startPosition - posChange, timeAcc / lerpTime);
            cam.rotation = Quaternion.Slerp(startRot, endRot, timeAcc / lerpTime);
            yield return null;
        }
        serverAnimationPlaying = false;
        Shooting.Local.canShoot = true;
        gun.enableGun();
        ui.enableUI();
        yield break;
    }

    IEnumerator GiveKey(Vector3 serverPosition)
    {
        Vector3 startPosition = serverPosition;
        Vector3 endPosition = transform.Find("RenderedBody").position;
        Transform keyPrefabInstance = Instantiate(keyPrefab, startPosition, Quaternion.identity).transform;
        float rotationSpeed = 360f; // degrees per second
        float lerpTime = 1f;
        float timeAcc = 0f;
        while (timeAcc < lerpTime) {
            timeAcc += Time.deltaTime;
            keyPrefabInstance.position = Vector3.Lerp(startPosition, endPosition, timeAcc / lerpTime);
            keyPrefabInstance.rotation = Quaternion.Euler(new Vector3(0, rotationSpeed * timeAcc / lerpTime, 0));
            yield return null;
        }
        Destroy(keyPrefabInstance.gameObject);
        yield break;
    }
    private Transform getServerTransform()
    {
        Transform server;
        switch (PlayerMovement.Local.currDimension)
        {
            case "Ice":
                server = GameObject.Find("MaskIce").transform;
                break;
            case "Space":
                server = GameObject.Find("MaskSpace").transform;
                break;
            case "Maze":
                server = GameObject.Find("MaskMaze").transform;
                break;
            default:
                server = null;
                break;
        }
        return server;
    }

    IEnumerator InsufficientPoints() {
        while (ServerSpeaking) yield return null;

        Color initialColor = serverText.color;
        serverText.color = new Color(1f, 0f, 0f, 1f);
        StartServerSpeak("INSUFFICIENT CAPSULES, FEED ME");
        while (ServerSpeaking) yield return null;
        serverText.color = initialColor;

        activeSceneCoroutine = null;
    }
}
