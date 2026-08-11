using System.Collections;
using UnityEngine;
using TMPro;
using FishNet.Object;

public class MaskController : MonoBehaviour {
    private const string FeedPrompt = "Press Space to Feed Me 5 Capsules, I MUST GROW";

    public static MaskController Local { get; private set; }

    public int keyCount = 0;
    [SerializeField] private GameObject keyPrefab;

    private Camera playerCamera;
    private TextMeshProUGUI maskText;
    private GameObject portal4;

    private bool MaskSpeaking = false;
    private Coroutine activeMaskCoroutine = null;
    private Coroutine activeSceneCoroutine = null;

    private Transform mazeMaskTransform;
    private Transform spaceMaskTransform;
    private Transform iceMaskTransform;

    private bool seeingMazeMask = false;
    private bool seeingSpaceMask = false;
    private bool seeingIceMask = false;

    private bool seenMaskMaze = false;
    private bool seenSpaceMask = false;
    private bool seenIceMask = false;

    public bool mazeKeyAcquired = false;
    public bool spaceKeyAcquired = false;
    public bool iceKeyAcquired = false;
    public static bool maskAnimationPlaying = false;

    private bool desertEntered = false;
    private bool mazeEntered = false;
    private bool spaceEntered = false;
    private bool iceEntered = false;
    private Transform cam;
    private BuildUI ui;
    private GunThingAnim gun;

    private readonly string[] generationVerbs = { "Generating", "Working", "Producing", "Thinking", "Calculating", "Contemplating", "Processing", "Analyzing", "Computing", "Synthesizing" };

    public bool LookingAtMask => seeingMazeMask || seeingSpaceMask || seeingIceMask;
    private bool AllKeysAcquired => mazeKeyAcquired && spaceKeyAcquired && iceKeyAcquired;

    private void Awake() {
        SaveSystem.ApplyPendingMaskData(this);
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
        maskText = GameObject.Find("MaskText").GetComponent<TextMeshProUGUI>();
        portal4 = GameObject.Find("portal4");
        mazeMaskTransform = GameObject.Find("MaskMaze").transform;
        spaceMaskTransform = GameObject.Find("MaskSpace").transform;
        iceMaskTransform = GameObject.Find("MaskIce").transform;
        keyCount = 0;
        ui = GameObject.FindObjectsByType<BuildUI>(FindObjectsSortMode.None)[0];
        gun = GameObject.FindObjectsByType<GunThingAnim>(FindObjectsSortMode.None)[0];
    }

    public void BeginOpeningScene() {
        activeSceneCoroutine = StartCoroutine(StartDesertScene());
    }

    // -------------------------------------------------------------------------
    // Mask / Cutscene helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Stops any running mask coroutines and resets all related state so the
    /// next scene can (start cleanly, even if the player switches dimensions
    /// mid-dialogue.
    /// </summary>
    private void ResetMaskState() {
        if (activeMaskCoroutine != null) {
            StopCoroutine(activeMaskCoroutine);
            activeMaskCoroutine = null;
        }
        if (activeSceneCoroutine != null) {
            StopCoroutine(activeSceneCoroutine);
            activeSceneCoroutine = null;
        }
        MaskSpeaking = false;
        if (maskText != null) {
            maskText.text = "";
            maskText.color = new Color(1f, 1f, 1f, 1f);
        }
    }

    /// <summary>Typewriter effect coroutine. Always go through StartMaskSpeak().</summary>
    IEnumerator MaskSpeak(string input) {
        MaskSpeaking = true;
        Color color = maskText.color;
        maskText.color = new Color(0.871f, 0.451f, 0.337f, 1f);
        maskText.text = generationVerbs[Random.Range(0, generationVerbs.Length)] + "...";
        yield return new WaitForSeconds(Random.Range(0.2f, 2f));
        maskText.color = color;

        for (int i = 0; i < input.Length; i += Random.Range(1, 5)) {
            maskText.text = input.Substring(0, Mathf.Min(i + 1, input.Length));
            yield return new WaitForSeconds(Random.Range(0, 0.1f));
        }
        maskText.text = input;
        yield return new WaitForSeconds(3f);
        maskText.text = "";
        maskText.color = new Color(1f, 1f, 1f, 1f);
        MaskSpeaking = false;
    }

    /// <summary>Helper that tracks the active mask coroutine so it can be cancelled.</summary>
    private void StartMaskSpeak(string input) {
        if (activeMaskCoroutine != null) StopCoroutine(activeMaskCoroutine);
        activeMaskCoroutine = StartCoroutine(MaskSpeak(input));
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

        UpdateMaskDetection();
        UpdateMaskPrompts();
        if (!maskAnimationPlaying) {
            RotateMask();
        }
    }

    private void UpdateMaskDetection() {
        seeingMazeMask = false;
        seeingSpaceMask = false;
        seeingIceMask = false;

        if (PlayerMovement.Local.currDimension == "Desert") return;

        GameObject maskToDetect = null;
        ref bool seeing = ref seeingMazeMask;
        ref bool keyAcquired = ref mazeKeyAcquired;
        switch (PlayerMovement.Local.currDimension)
        {
            case "Maze": maskToDetect = mazeMaskTransform.gameObject;  break;
            case "Space": maskToDetect = spaceMaskTransform.gameObject; seeing = ref seeingSpaceMask; keyAcquired = ref spaceKeyAcquired; break;
            case "Ice": maskToDetect = iceMaskTransform.gameObject; seeing = ref seeingIceMask; keyAcquired = ref iceKeyAcquired; break;
        }
        if (!keyAcquired && Vector3.Distance(playerCamera.transform.position, maskToDetect.transform.position) < 5f && Vector3.Angle(playerCamera.transform.forward, maskToDetect.transform.position - playerCamera.transform.position) < 30f) {
            seeing = true;
        }
    }

    private void UpdateMaskPrompts() {
        if (seeingMazeMask) {
            if (!seenMaskMaze) {
                InterruptWithMaskScene(MaskMazeScene());
                seenMaskMaze = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Maze" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }

        if (seeingSpaceMask) {
            if (!seenSpaceMask) {
                InterruptWithMaskScene(MaskSpaceScene());
                seenSpaceMask = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Space" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }

        if (seeingIceMask) {
            if (!seenIceMask) {
                InterruptWithMaskScene(MaskIceScene());
                seenIceMask = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == "Ice" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }
    }

    /// <summary>
    /// Cancels whatever dialogue is currently playing (typically a dimension-entry
    /// scene) and immediately starts the given mask scene. Walking up to a mask
    /// always takes priority over the "Welcome to the ..." sequence.
    /// </summary>
    private void InterruptWithMaskScene(IEnumerator maskScene) {
        ResetMaskState();
        activeSceneCoroutine = StartCoroutine(maskScene);
    }

    public void TryFeed() {
        if (seeingMazeMask && !mazeKeyAcquired)
            Feed(ref mazeKeyAcquired, StartMazeKeyCutscene());
        else if (seeingSpaceMask && !spaceKeyAcquired)
            Feed(ref spaceKeyAcquired, StartSpaceKeyCutscene());
        else if (seeingIceMask && !iceKeyAcquired)
            Feed(ref iceKeyAcquired, StartIceKeyCutscene());
    }

    private void Feed(ref bool keyAcquired, IEnumerator cutscene) {
        if (upgradeManager.Local.killPoints >= 5) {
            upgradeManager.Local.killPoints -= 5;
            keyAcquired = true;
            keyCount++;
            ResetMaskState();
            activeSceneCoroutine = StartCoroutine(cutscene);
            SaveSystem.SavePlayerData();
        } else if (activeSceneCoroutine == null) {
            activeSceneCoroutine = StartCoroutine(InsufficientPoints());
        }
    }

    private void RotateMask() {
        Transform mask = null;
        switch (PlayerMovement.Local.currDimension) {
            case "Maze": mask = mazeMaskTransform; break;
            case "Space": mask = spaceMaskTransform; break;
            case "Ice": mask = iceMaskTransform; break;
        }
        if (mask != null) {
            mask.rotation = Quaternion.LookRotation(mask.position - playerCamera.transform.position);
        }
    }

    // -------------------------------------------------------------------------
    // Dimension entry
    // -------------------------------------------------------------------------

    /// <summary>
    /// Entry point for all dimension-change scene sequences.
    /// Always resets mask state first so switching dimensions mid-dialogue
    /// never leaves orphaned coroutines.
    /// </summary>
    public void DisplayDimension() {
        ResetMaskState();
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
        while (MaskSpeaking) yield return null;
        if (desertEntered) {
            StartMaskSpeak("Welcome back to the Desert");
        } else {
            StartMaskSpeak("Welcome to the Desert");
            desertEntered = true;
            while (MaskSpeaking) yield return null;
            StartMaskSpeak("Defeat Capsules to obtain their essence");
        }
        while (MaskSpeaking) yield return null;

        Color initialColor = maskText.color;
        maskText.color = new Color(1f, 0f, 0f, 1f);

        if (!AllKeysAcquired)
            StartMaskSpeak("FEED ME CAPSULES, I LURK IN FAR DIMENSIONS, I MUST GROW");
        else {
            StartMaskSpeak("THE WHITE ROOM BECKONS YOU");
            OpenFinalPortal();
        }

        while (MaskSpeaking) yield return null;
        maskText.color = initialColor;
        activeSceneCoroutine = null;
    }

    IEnumerator StartMazeScene() {
        while (MaskSpeaking) yield return null;
        if (mazeEntered) {
            StartMaskSpeak("Welcome back to the Maze");
        } else {
            StartMaskSpeak("Welcome to the Maze");
            mazeEntered = true;
            while (MaskSpeaking) yield return null;
            StartMaskSpeak("Jump upgrades, dashing, and building are disabled here");
        }
        while (MaskSpeaking) yield return null;

        if (!mazeKeyAcquired) {
            Color initialColor = maskText.color;
            maskText.color = new Color(1f, 0f, 0f, 1f);
            StartMaskSpeak("I AM NEAR, FIND ME, I MUST GROW");
            while (MaskSpeaking) yield return null;
            maskText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator StartSpaceScene() {
        while (MaskSpeaking) yield return null;
        if (spaceEntered) {
            StartMaskSpeak("Welcome back to Space");
        } else {
            StartMaskSpeak("Welcome to Space");
            spaceEntered = true;
            while (MaskSpeaking) yield return null;
            StartMaskSpeak("Gravity and dashing cooldowns are  reduced, you can jump higher");
        }
        while (MaskSpeaking) yield return null;

        if (!spaceKeyAcquired) {
            Color initialColor = maskText.color;
            maskText.color = new Color(1f, 0f, 0f, 1f);
            StartMaskSpeak("I AM NEAR, FIND ME, FEED ME CAPSULES, I MUST GROW");
            while (MaskSpeaking) yield return null;
            maskText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator StartIceScene() {
        while (MaskSpeaking) yield return null;
        iceKeyAcquired = false;
        if (iceEntered) {
            StartMaskSpeak("Welcome back to the Tundra");
        } else {
            StartMaskSpeak("Welcome to the Tundra");
            iceEntered = true;
            while (MaskSpeaking) yield return null;
            StartMaskSpeak("A slippery dimension where shooting propels you forward");
        }
        while (MaskSpeaking) yield return null;

        if (!iceKeyAcquired) {
            Color initialColor = maskText.color;
            maskText.color = new Color(1f, 0f, 0f, 1f);
            StartMaskSpeak("I AM NEAR, FIND ME, FEED ME CAPSULES, I MUST GROW");
            while (MaskSpeaking) yield return null;
            maskText.color = initialColor;
        }

        activeSceneCoroutine = null;
    }

    IEnumerator MaskMazeScene() {
        while (MaskSpeaking) yield return null;

        if (!seenMaskMaze)
            StartMaskSpeak("You have done well to find me");

        while (MaskSpeaking) yield return null;

        StartMaskSpeak(FeedPrompt);
        activeSceneCoroutine = null;
    }

    IEnumerator MaskSpaceScene() {
        while (MaskSpeaking) yield return null;

        if (!seenSpaceMask)
            StartMaskSpeak("You have done well to find me");

        while (MaskSpeaking) yield return null;

        StartMaskSpeak("Press Space to Feed Me 5 Capsules");
        activeSceneCoroutine = null;
    }

    IEnumerator MaskIceScene() {
        while (MaskSpeaking) yield return null;

        if (!seenIceMask)
            StartMaskSpeak("You have done well to find me");

        while (MaskSpeaking) yield return null;

        StartMaskSpeak(FeedPrompt);
        activeSceneCoroutine = null;
    }

    public IEnumerator StartFirstKillScene() {
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("Congratulations on your first kill, Capsule");

        while (MaskSpeaking) yield return null;

        StartMaskSpeak("Press I to open your inventory and see your capsule essences");

        while (MaskSpeaking) yield return null;
        Color initialColor = maskText.color;
        maskText.color = new Color(1f, 0f, 0f, 1f);
        StartMaskSpeak("COLLECT MORE ESSENCES AND FIND ME, I HUNGER");

        while (MaskSpeaking) yield return null;

        maskText.color = initialColor;
        activeSceneCoroutine = null;
    }

    IEnumerator StartMazeKeyCutscene() {
        while (MaskSpeaking) yield return null;
        Vector3 posChange = GetKeySceneMovement();
        Quaternion camRot = cam.rotation;
        yield return StartCoroutine(KeyCutsceneStart(posChange));
        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with upgrade tokens");
        while (MaskSpeaking) yield return null;
        yield return StartCoroutine(GiveKey(GameObject.Find("MaskMaze").transform.position));
        upgradeManager.Local.upgradePoints += 12;
        SaveSystem.SavePlayerData();
        if (!AllKeysAcquired) {
            StartMaskSpeak("Press T to open the upgrade menu and spend your tokens, and I to view your inventory");
            while (MaskSpeaking) yield return null;
        }
        Color initialColor = maskText.color;
        maskText.color = new Color(0f, 1f, 0f, 1f);
        StartMaskSpeak("Why am I here?");
        while (MaskSpeaking) yield return null;

        maskText.color = new Color(1f, 0f, 0f, 1f);

        if (!AllKeysAcquired)
            StartMaskSpeak("I LURK ELSEWHERE, FIND ME, FEED ME, YOUR QUESTIONS WILL BE ANSWERED");
        else {
            StartMaskSpeak("YOU HAVE PROVEN YOUR WORTH, THE LOCKED PORTAL HAS BEEN UNSEALED");
            OpenFinalPortal();
        }

        while (MaskSpeaking) yield return null;
        maskText.color = initialColor;

        GameObject maskObj = GameObject.Find("MaskMaze");
        if (maskObj != null) maskObj.SetActive(false);
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));

        activeSceneCoroutine = null;
    }

    IEnumerator StartSpaceKeyCutscene() {
        while (MaskSpeaking) yield return null;
        Vector3 posChange = GetKeySceneMovement();
        Quaternion camRot = cam.rotation;
        yield return StartCoroutine(KeyCutsceneStart(posChange));
        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with upgrade tokens");
        while (MaskSpeaking) yield return null;
        yield return StartCoroutine(GiveKey(GameObject.Find("MaskSpace").transform.position));
        upgradeManager.Local.upgradePoints += 12;
        SaveSystem.SavePlayerData();
        if (!AllKeysAcquired) {
            StartMaskSpeak("Press T to open the upgrade menu and spend your tokens and Press I to view your keys");
            while (MaskSpeaking) yield return null;
        }
        Color initialColor = maskText.color;
        maskText.color = new Color(0f, 1f, 0f, 1f);

        StartMaskSpeak("What am I?");
        while (MaskSpeaking) yield return null;

        maskText.color = new Color(1f, 0f, 0f, 1f);

        if (!AllKeysAcquired)
            StartMaskSpeak("I LURK ELSEWHERE, FIND ME, FEED ME, I MUST GROW");
        else {
            StartMaskSpeak("YOU HAVE PROVEN YOUR WORTH, THE LOCKED PORTAL HAS BEEN UNSEALED");
            OpenFinalPortal();
        }

        while (MaskSpeaking) yield return null;
        maskText.color = initialColor;

        GameObject maskObj = GameObject.Find("MaskSpace");
        if (maskObj != null) maskObj.SetActive(false);
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));

        activeSceneCoroutine = null;
    }

    IEnumerator StartIceKeyCutscene() {
        while (MaskSpeaking) yield return null;
        Vector3 posChange = GetKeySceneMovement();
        Quaternion camRot = cam.rotation;
        yield return StartCoroutine(KeyCutsceneStart(posChange));
        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with a key and upgrade tokens");
        while (MaskSpeaking) yield return null;
        yield return StartCoroutine(GiveKey(GameObject.Find("MaskIce").transform.position));
        upgradeManager.Local.upgradePoints += 12;
        SaveSystem.SavePlayerData();
        if (!AllKeysAcquired) {
            StartMaskSpeak("Press T to open the upgrade menu and spend your tokens and Press I to view your keys");
            while (MaskSpeaking) yield return null;
        }
        Color initialColor = maskText.color;
        maskText.color = new Color(0f, 1f, 0f, 1f);

        StartMaskSpeak("Who are you?");
        while (MaskSpeaking) yield return null;

        maskText.color = new Color(1f, 0f, 0f, 1f);

        if (!AllKeysAcquired)
            StartMaskSpeak("I LURK ELSEWHERE, FIND ME, FEED ME, I MUST GROW");
        else {
            StartMaskSpeak("YOU HAVE PROVEN YOUR WORTH, THE LOCKED PORTAL HAS BEEN UNSEALED");
            OpenFinalPortal();
        }

        while (MaskSpeaking) yield return null;
        maskText.color = initialColor;

        GameObject maskObj = GameObject.Find("MaskIce");
        if (maskObj != null) maskObj.SetActive(false);
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));

        activeSceneCoroutine = null;
    }

    private Vector3 GetKeySceneMovement()
    {
        Transform mask = getMaskTransform();
        Vector3 startPosition = cam.position;
        float distToMask = Vector3.Distance(startPosition, mask.position);
        Vector3 endPosition = startPosition + -transform.forward * distToMask + transform.right * distToMask + transform.up * distToMask;
        return endPosition - startPosition;
    }

    IEnumerator KeyCutsceneStart(Vector3 posChange)
    {
        Transform mask = getMaskTransform();
        ui.disableUI();
        gun.disableGun();
        maskAnimationPlaying = true;
        Shooting.Local.canShoot = false;

        float lerpTime = 1f;
        float timeAcc = 0f;
        Vector3 startPosition = cam.position;
        cam.LookAt(mask);
        Vector3 startRotation = cam.rotation.eulerAngles;
        Vector3 endRotation = cam.rotation.eulerAngles + new Vector3(14.04f, -90f, 0f);
        while (timeAcc < lerpTime) {
            timeAcc += Time.deltaTime;
            cam.position = Vector3.Lerp(startPosition, startPosition + posChange, timeAcc / lerpTime);
            cam.LookAt(mask.position - transform.forward);
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
        maskAnimationPlaying = false;
        Shooting.Local.canShoot = true;
        gun.enableGun();
        ui.enableUI();
        yield break;
    }

    IEnumerator GiveKey(Vector3 maskPosition)
    {
        Vector3 startPosition = maskPosition;
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
    private Transform getMaskTransform()
    {
        Transform mask;
        switch (PlayerMovement.Local.currDimension)
        {
            case "Ice":
                mask = GameObject.Find("MaskIce").transform;
                break;
            case "Space":
                mask = GameObject.Find("MaskSpace").transform;
                break;
            case "Maze":
                mask = GameObject.Find("MaskMaze").transform;
                break;
            default:
                mask = null;
                break;
        }
        return mask;
    }

    IEnumerator InsufficientPoints() {
        while (MaskSpeaking) yield return null;

        Color initialColor = maskText.color;
        maskText.color = new Color(1f, 0f, 0f, 1f);
        StartMaskSpeak("INSUFFICIENT CAPSULES, FEED ME");
        while (MaskSpeaking) yield return null;
        maskText.color = initialColor;

        activeSceneCoroutine = null;
    }
}
