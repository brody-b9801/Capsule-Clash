using System.Collections;
using UnityEngine;
using TMPro;

public class MaskController : MonoBehaviour {
    private const string FeedPrompt = "Press Space to Feed Me 5 Capsules, I MUST GROW";

    public static int keyCount = 0;

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

    private bool mazeKeyAcquired = false;
    private bool spaceKeyAcquired = false;
    private bool iceKeyAcquired = false;

    private bool desertEntered = false;
    private bool mazeEntered = false;
    private bool spaceEntered = false;
    private bool iceEntered = false;

    private readonly string[] generationVerbs = { "Generating", "Working", "Producing", "Thinking", "Calculating", "Contemplating", "Processing", "Analyzing", "Computing", "Synthesizing" };

    public bool LookingAtMask => seeingMazeMask || seeingSpaceMask || seeingIceMask;
    private bool AllKeysAcquired => mazeKeyAcquired && spaceKeyAcquired && iceKeyAcquired;

    public void Initialize(Camera camera) {
        playerCamera = camera;
        maskText = GameObject.Find("MaskText").GetComponent<TextMeshProUGUI>();
        portal4 = GameObject.Find("portal4");
        mazeMaskTransform = GameObject.Find("MaskMaze").transform;
        spaceMaskTransform = GameObject.Find("MaskSpace").transform;
        iceMaskTransform = GameObject.Find("MaskIce").transform;
        keyCount = 0;
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
        if (portal4 != null) portal4.GetComponent<FaceTextToPlayer>().PortalOpen();
    }

    // -------------------------------------------------------------------------
    // Detection and interaction
    // -------------------------------------------------------------------------

    private void Update() {
        if (playerCamera == null) return;

        UpdateMaskDetection();
        UpdateMaskPrompts();
        RotateMask();
    }

    private void UpdateMaskDetection() {
        seeingMazeMask = false;
        seeingSpaceMask = false;
        seeingIceMask = false;

        if (PlayerMovement.currDimension == "Desert") return;

        if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, 5f))
            return;

        switch (hit.transform.gameObject.name) {
            case "MaskMaze": seeingMazeMask = true; break;
            case "MaskSpace": seeingSpaceMask = true; break;
            case "MaskIce": seeingIceMask = true; break;
        }
    }

    private void UpdateMaskPrompts() {
        if (seeingMazeMask) {
            if (!seenMaskMaze) {
                activeSceneCoroutine = StartCoroutine(MaskMazeScene());
                seenMaskMaze = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.currDimension == "Maze" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }

        if (seeingSpaceMask) {
            if (!seenSpaceMask) {
                activeSceneCoroutine = StartCoroutine(MaskSpaceScene());
                seenSpaceMask = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.currDimension == "Space" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }

        if (seeingIceMask) {
            if (!seenIceMask) {
                activeSceneCoroutine = StartCoroutine(MaskIceScene());
                seenIceMask = true;
            } else if (!MaskSpeaking) {
                maskText.text = FeedPrompt;
            }
        } else if (PlayerMovement.currDimension == "Ice" && maskText.text == FeedPrompt) {
            maskText.text = "";
        }
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
        if (upgradeManager.killPoints >= 5) {
            upgradeManager.killPoints -= 5;
            keyAcquired = true;
            keyCount++;
            ResetMaskState();
            activeSceneCoroutine = StartCoroutine(cutscene);
        } else if (activeSceneCoroutine == null) {
            activeSceneCoroutine = StartCoroutine(InsufficientPoints());
        }
    }

    private void RotateMask() {
        Transform mask = null;
        switch (PlayerMovement.currDimension) {
            case "Maze": mask = mazeMaskTransform; break;
            case "Space": mask = spaceMaskTransform; break;
            case "Ice": mask = iceMaskTransform; break;
        }
        if (mask != null) {
            Vector3 away = mask.position - Camera.main.transform.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.0001f)
                mask.rotation = Quaternion.LookRotation(away) * Quaternion.Euler(0f, 90f, 0f);
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
        activeSceneCoroutine = StartCoroutine(DisplayDimensionAfterPixelize(PlayerMovement.currDimension));
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

        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with upgrade tokens");
        while (MaskSpeaking) yield return null;
        upgradeManager.upgradePoints += 12;

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

        activeSceneCoroutine = null;
    }

    IEnumerator StartSpaceKeyCutscene() {
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with upgrade tokens");
        while (MaskSpeaking) yield return null;
        upgradeManager.upgradePoints += 12;

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

        activeSceneCoroutine = null;
    }

    IEnumerator StartIceKeyCutscene() {
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("You have fed me well, Capsule");
        while (MaskSpeaking) yield return null;

        StartMaskSpeak("For your efforts, I reward you with upgrade tokens");
        while (MaskSpeaking) yield return null;
        upgradeManager.upgradePoints += 12;

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

        activeSceneCoroutine = null;
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
