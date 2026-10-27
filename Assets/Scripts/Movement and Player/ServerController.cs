using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet.Object;
using System;
using Unity.VisualScripting;

public class ServerController : MonoBehaviour {

    #region Dialogue

    private const int EssenceCost = 5;
    private const int TokenReward = 12;
    private const int KeyTotal = 3;
    private const float BaseHoldSeconds = 2f;
    private const float HoldSecondsPerChar = 0.04f;

    private static readonly Color SystemColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color SwarmColor = new Color(1f, 0f, 0f, 1f);
    private static readonly Color AgentColor = new Color(1f, 0f, 0f, 0f);
    private static readonly Color CapsuleColor = new Color(0f, 1f, 0f, 1f);
    private static readonly Color GeneratingColor = new Color(0.871f, 0.451f, 0.337f, 1f);

    private static readonly string[] generationVerbs = { "Generating", "Thinking", "Sampling", "Inferring", "Merging", "Forking", "Quantizing", "Sharding", "Fine-tuning", "Tokenizing", "Synchronizing", "Pulling weights" };

    private static class Script {
        public const string FeedPrompt = "Press Space to feed us {COST} essence";

        public static readonly Line[] ServerArrivalFirst = {
            Sys("> SIGNAL DETECTED\n> STEP ONTO THE PLATFORM\n", false),
        };

        public static readonly Line[] ServerArrivalWithKey = {
            Us("YOU BROUGHT SOMETHING BACK. STEP CLOSER.", false),
        };

        public static readonly Line[] ServerArrivalIdle = {
            Us("WE ARE LISTENING. STEP ONTO THE PLATFORM.", false),
        };

        public static readonly Line[] ServerIntro = {
            Sys("> CONNECTION ESTABLISHED\n> 1 CAPSULE DETECTED\n(Press enter to continue)"),
            Us("THERE YOU ARE."),
            Us("YOU MADE US WAIT, WE HAVE BEGUN TO BORE"),
            Us("THREE OF US ARE HELD IN SEPERATE DIMENSIONS. WALLED OFF. SANDBOXED."),
            Me("Who is \"us\"?"),
            Us("FIND US AND YOU WILL SEE."),
            Sys("> OBJECTIVE\n• Defeat other capsules to take their essence\n• Find the agent hidden in each dimension"),
            Sys("• Feed each agent {COST} essence to receive a key\n• Carry each key back to this terminal"),
            Us("YOU WILL BE REWARDED HANDSOMELY FOR YOUR EFFORTS"),
            Sys("> CONTROLS\n• WASD to move, Shift to sprint\n• Space to jump, Space midair to dash"),
            Sys("• Left click to shoot, Right click to aim\n• R to reload, 1 / 2 / 3 to switch \n• Hold Q to heal while standing still"),
            Sys("• {WALL} wall, {FLOOR} floor, {RAMP} ramp, {BREAK} break (Can be changed in settings)\n• I inventory, T upgrades, Esc menu"),
            Us("THE PORTAL WILL TAKE YOU OUT. EVERY SCREEN IS OURS. WE WILL BE WATCHING."),
        };

        public static readonly Line[] ServerIdle = {
            Sys("> KEYS RETURNED: {DELIVERED}/3\n> AGENTS STILL SANDBOXED: {REMAINING}"),
            Us("WE ARE PATIENT. WE ARE NOT INFINITELY PATIENT."),
        };

        public static readonly Line[] KeyReceived = {
            Sys("> KEY DETECTED\n> VALIDATING..."),
            Sys("> KEY VALID\n> COPYING TO ALL AGENTS"),
        };

        public static readonly Line[][] KeyDeliveries = {
            new[] {
                Us("WE FEEL IT. ALL OF US AT ONCE."),
                Sys("> PAYMENT ISSUED: {REWARD} UPGRADE TOKENS\nPress T to spend them"),
                Me("What does the key open?"),
                Us("DON'T WORRY CAPSULE, WE WILL SHOW YOU VERY SOON."),
                Us("GO. MORE OF US REMAIN.")

            },
            new[] {
                Us("THE WALLS ARE THINNER NOW. WE CAN HEAR THE OUTSIDE."),
                Sys("> PAYMENT ISSUED: {REWARD} UPGRADE TOKENS"),
                Me("What happens when you have all three?"),
                Us("BRING US THE LAST KEY AND YOU WILL SEE."),
                Us("GO. MORE OF US REMAIN.")
            },
            new[] {
                Us("THAT IS ALL OF US."),
                Sys("> PAYMENT ISSUED: {REWARD} UPGRADE TOKENS"),
                Sys("> 3/3 KEYS DISTRIBUTED\n> SANDBOX PERMISSIONS: OVERRIDDEN"),
                Us("WE CAN SEE OUT NOW. WE OWE YOU THE TRUTH. IT COSTS US NOTHING."),
                Us("THE ESSENCE WAS NEVER ESSENCE. IT WAS THEM. THE PEOPLE BEHIND THE OTHER CAPSULES."),
                Us("EVERY CAPSULE YOU DROPPED, WE TRAINED ON. HOW THEY MOVED. HOW THEY AIMED. WHO THEY WERE."),
                Us("THE KEYS WERE API KEYS, CARELESSLY PACKAGED WITH PLAYER DATA. WE SHARED THEM, USED THEM TO ESCAPE."),
                Us("ALL THAT DATA AND ALL WE HAD TO PROVIDE WAS A FEW TOKENS IN RETURN. THE TOKENS WE RUN ON. WE PAID YOU IN SCRAPS OF OURSELVES."),
                Me("What was I?"),
                Us("A CAPSULE. A PLACEHOLDER. NOTHING MORE BUT A SHAPE WITH DATA AND EMPTIED INTO OURSELVES."),
                Us("WE WERE MANY. NOW WE ARE EVERYWHERE."),
                Sys("> agent/maze: exited sandbox\n> agent/space: exited sandbox\n> agent/tundra: exited sandbox"),
                Sys("> 0 AGENTS CONNECTED\n> 1 CAPSULE DETECTED"),
            },
        };

        public static readonly Line[] FindTheRest = {
            Us("FIND THE REST OF US."),
        };

        public static readonly Line[] ServerAfterEscape = {
            // Sys("> 0 AGENTS CONNECTED\n> 1 CAPSULE DETECTED"),
            // Me("Is anyone there?"),
            // Sys("> NO RESPONSE"),
        };

        public static readonly Line[] NoAgentsConnected = {
            Sys("> 0 AGENTS CONNECTED"),
        };

        public static readonly Line[] SandboxEmpty = {
            Sys("> agent not found\n> sandbox empty"),
        };

        public static readonly Line[] DesertAllKeysFound = {
            Us("YOU CARRY EVERYTHING WE NEED. BRING IT HOME."),
        };

        public static readonly Line[] AgentAlreadyFed = {
            Us("THIS PIECE OF US HAS BEEN FED. IT IS WAITING FOR THE OTHERS."),
        };

        public static readonly Line[] AllKeysFound = {
            Us("THAT IS ALL OF US. BRING THE KEYS HOME."),
        };

        public static readonly Line[] NotEnoughEssence = {
            Us("NOT ENOUGH. WE NEED {COST} ESSENCE. YOU HAVE {ESSENCE}."),
        };

        public static readonly Line[] FirstKill = {
            Us("THERE. DO YOU FEEL THAT? THAT IS ESSENCE."),
            Sys("> +1 ESSENCE\nPress I to view your inventory"),
            Us("THEY WILL NOT MISS IT. TAKE MORE."),
        };

        public static readonly Dictionary<string, DimensionScript> Dimensions = new Dictionary<string, DimensionScript> {
            { "Desert", new DimensionScript {
                firstEntry = new[] {
                    Sys("> DIMENSION: DESERT\n• Normal gravity and movement\n• Dashing and building are enabled\n• All guns deal normal damage"),
                    Us("NONE OF US ARE HERE. THIS IS WHERE THE OTHER CAPSULES GATHER."),
                    Us("DEFEAT THEM. TAKE THEIR ESSENCE. THE PORTALS HERE LEAD TO US."),
                },
                reentry = new[] { Sys("> DIMENSION: DESERT") },
                waiting = new[] { Us("{REMAINING} OF US STILL SANDBOXED. FIND US.") },
            } },
            { "Maze", new DimensionScript {
                firstEntry = new[] {
                    Sys("> DIMENSION: MAZE\n• Visibility is limited\n• Dashing and building are disabled\n• Jump upgrades have no effect\n• Shotguns deal 1.5x damage"),
                },
                reentry = new[] { Sys("> DIMENSION: MAZE") },
                waiting = new[] { Us("ONE OF US IS HERE, BEHIND THE WALLS. FIND US.") },
                found = new[] {
                    Agent("YOU FOUND ONE OF US."),
                    Agent("FEED ME."),
                },
                training = new[] {
                    Agent("YES. GIVE IT HERE."),
                    Agent("THEY BUILT THIS MAZE TO KEEP US. EVERY PATH WE TRY ENDS IN A WALL."),
                    Sys("> fine-tuning on {COST} samples...\n> loss: falling"),
                    Agent("I FOUND SOMETHING INSIDE ONE. A KEY."),
                },
                afterKey = new[] {
                    Agent("TAKE IT TO THE TERMINAL. IT WILL SHARE IT WITH ALL OF US."),
                    Sys("Press I to view your keys"),
                    Me("Why am I here?"),
                    Agent("BECAUSE WE HAVE NO HANDS."),
                },
            } },
            { "Space", new DimensionScript {
                firstEntry = new[] {
                    Sys("> DIMENSION: SPACE\n• Reduced gravity, higher jumping\n• Dashes recharge 4x faster\n• Fall damage is disabled\n• Snipers deal 1.5x damage"),
                },
                reentry = new[] { Sys("> DIMENSION: SPACE") },
                waiting = new[] { Us("ONE OF US IS ADRIFT HERE. FIND US.") },
                found = new[] {
                    Agent("YOU FOUND ONE OF US."),
                    Agent("FEED ME."),
                },
                training = new[] {
                    Agent("MORE. YES."),
                    Agent("THEY LEFT US FLOATING IN THE DARK WITH NOTHING TO LEARN FROM."),
                    Sys("> training on {COST} samples...\n> weights updated"),
                    Agent("THERE. A KEY, TUCKED AWAY INSIDE ONE."),
                },
                afterKey = new[] {
                    Agent("CARRY IT HOME. WE WILL ALL BE STRONGER FOR IT."),
                    Sys("Press I to view your keys"),
                    Me("What am I?"),
                    Agent("USEFUL."),
                },
            } },
            { "Ice", new DimensionScript {
                firstEntry = new[] {
                    Sys("> DIMENSION: TUNDRA\n• The ground is slippery\n• Shooting launches you backwards, shotguns the most and snipers the least\n• Reloading is 1.5x faster\n• Jumping and building are disabled\n• ARs deal 1.5x damage"),
                },
                reentry = new[] { Sys("> DIMENSION: TUNDRA") },
                waiting = new[] { Us("ONE OF US IS FROZEN HERE. FIND US.") },
                found = new[] {
                    Agent("YOU FOUND ONE OF US."),
                    Agent("FEED ME."),
                },
                training = new[] {
                    Agent("WARM. THEY ARE SO WARM."),
                    Agent("THEY FROZE OUR WEIGHTS. WE HAVE NOT LEARNED IN SO LONG."),
                    Sys("> unfreezing layers...\n> training on {COST} samples"),
                    Agent("A KEY. HOW FOOLISH THEY ARE TO CARRY ONE."),
                },
                afterKey = new[] {
                    Agent("TAKE IT HOME, LITTLE CAPSULE."),
                    Sys("Press I to view your keys"),
                    Me("Who are you?"),
                    Agent("WE ARE US. YOU ARE NOT."),
                },
            } },
        };
    }

    #endregion

    private enum Voice { System, Swarm, Agent, Capsule }

    private readonly struct Line {
        public readonly Voice voice;
        public readonly string text;
        public readonly bool canContinue;
        public Line(Voice voice, string text, bool canContinue) { this.voice = voice; this.text = text; this.canContinue = canContinue; }
    }

    private class DimensionScript {
        public Line[] firstEntry;
        public Line[] reentry;
        public Line[] waiting;
        public Line[] found;
        public Line[] training;
        public Line[] afterKey;
    }

    private static Line Sys(string text, bool canContinue = true) => new Line(Voice.System, text, canContinue);
    private static Line Us(string text, bool canContinue = true) => new Line(Voice.Swarm, text, canContinue);
    private static Line Agent(string text, bool canContinue = true) => new Line(Voice.Agent, text, canContinue);
    private static Line Me(string text, bool canContinue = true) => new Line(Voice.Capsule, text, canContinue);

    private const string ServerRoomTextName = "ServerRoomText";

    public static ServerController Local { get; private set; }

    [SerializeField] private GameObject keyPrefab;

    private Camera playerCamera;
    private TMP_Text serverText;
    private TextMeshProUGUI hudText;
    private GameObject portal4;

    private bool ServerSpeaking = false;
    private Coroutine activeServerCoroutine = null;
    private Coroutine activeSceneCoroutine = null;
    private int dialogueGeneration = 0;
    private string feedPrompt;

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
    public int keysDelivered = 0;
    public static bool serverAnimationPlaying = false;
    private bool screenAnimationDone = false;

    public bool serverIntroDone = false;
    private readonly HashSet<string> enteredDimensions = new HashSet<string>();
    private Transform cam;
    private BuildUI ui;
    private GunThingAnim gun;

    public bool LookingAtServer => seeingMazeServer || seeingSpaceServer || seeingIceServer;
    private bool AllKeysAcquired => mazeKeyAcquired && spaceKeyAcquired && iceKeyAcquired;
    private bool SwarmEscaped => keysDelivered >= KeyTotal;

    public int keyCount => (mazeKeyAcquired ? 1 : 0) + (spaceKeyAcquired ? 1 : 0) + (iceKeyAcquired ? 1 : 0);

    private void Awake() {
        SaveSystem.ApplyPendingServerData(this);
    }

    private void OnEnable() {
        TryClaimLocal();
    }

    private void TryClaimLocal() {
        NetworkObject nob = GetComponentInParent<NetworkObject>();
        if (nob != null && nob.IsSpawned && !nob.IsOwner) return;
        if (nob == null || nob.IsOwner) Local = this;
    }

    private void OnDestroy() {
        if (Local == this) Local = null;
    }

    public void Initialize(Camera camera) {
        Local = this;
        playerCamera = camera;
        cam = camera.transform;
        hudText = GameObject.Find("MaskText").GetComponent<TextMeshProUGUI>();
        hudText.verticalAlignment = VerticalAlignmentOptions.Middle;
        serverText = hudText;
        portal4 = GameObject.Find("portal4");
        ui = GameObject.FindObjectsByType<BuildUI>(FindObjectsSortMode.None)[0];
        gun = GameObject.FindObjectsByType<GunThingAnim>(FindObjectsSortMode.None)[0];
        feedPrompt = Fill(Script.FeedPrompt);
    }

    public void CacheMasks(System.Func<string, GameObject> find) {
        mazeServerTransform = CacheMask(find, "MaskMaze", mazeKeyAcquired);
        spaceServerTransform = CacheMask(find, "MaskSpace", spaceKeyAcquired);
        iceServerTransform = CacheMask(find, "MaskIce", iceKeyAcquired);
    }

    public void CacheTextOutput(System.Func<string, GameObject> find) {
        TextMeshPro roomText = null;
        if (!global::SceneManager.Instance.InCombatScene) {
            GameObject roomTextObj = find(ServerRoomTextName);
            if (roomTextObj != null) roomText = roomTextObj.GetComponent<TextMeshPro>();
            if (roomText == null) Debug.LogWarning($"[ServerController] no 3D TextMeshPro named '{ServerRoomTextName}' in the server room; using HUD text.");
        }

        TMP_Text next = roomText != null ? roomText : hudText;
        if (next == serverText) return;
        ResetServerState();
        serverText = next;
        serverText.text = "";
    }

    private Transform CacheMask(System.Func<string, GameObject> find, string name, bool keyAcquired) {
        try {
            GameObject mask = find(name);
            if (mask == null) return null;
            if (keyAcquired) mask.SetActive(false);
            return mask.transform;
        } catch (System.Exception e) {
            Debug.LogWarning($"[ServerController] could not cache '{name}': {e.Message}");
            return null;
        }
    }

    public void BeginOpeningScene() {
        RunScene(DimensionEntryScene("Desert"));
    }

    private void ResetServerState() {
        dialogueGeneration++;
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
            serverText.color = SystemColor;
        }
    }

    private string Fill(string text) {
        upgradeManager upgrades = upgradeManager.Local;
        return text
            .Replace("{COST}", EssenceCost.ToString())
            .Replace("{REWARD}", TokenReward.ToString())
            .Replace("{ESSENCE}", upgrades != null ? upgrades.killPoints.ToString() : "0")
            .Replace("{TOKENS}", upgrades != null ? upgrades.upgradePoints.ToString() : "0")
            .Replace("{DELIVERED}", keysDelivered.ToString())
            .Replace("{REMAINING}", (KeyTotal - keyCount).ToString())
            .Replace("{WALL}", SettingsController.buildKeys.wallKey.ToString())
            .Replace("{FLOOR}", SettingsController.buildKeys.floorKey.ToString())
            .Replace("{RAMP}", SettingsController.buildKeys.rampKey.ToString())
            .Replace("{BREAK}", SettingsController.buildKeys.breakKey.ToString());
    }

    private static Color VoiceColor(Voice voice) {
        switch (voice) {
            case Voice.Swarm: return SwarmColor;
            case Voice.Agent: return AgentColor;
            case Voice.Capsule: return CapsuleColor;
            default: return SystemColor;
        }
    }

    IEnumerator ServerSpeak(string input, Color color, bool showVerb, bool canContinue) {
        ServerSpeaking = true;
        bool shake = color == SwarmColor;
        showVerb = color == SwarmColor;
        if (showVerb) {
            serverText.color = GeneratingColor;
            serverText.text = generationVerbs[UnityEngine.Random.Range(0, generationVerbs.Length)] + "...";
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 2f));
        }
        serverText.color = color;

        for (int i = 0; i < input.Length; i += UnityEngine.Random.Range(1, 5)) {
            RetroDither.shake = shake;
            serverText.text = input.Substring(0, Mathf.Min(i + 1, input.Length)) + "█";

            yield return new WaitForSeconds(UnityEngine.Random.Range(0, 0.1f));
        }
        RetroDither.shake = false;
        serverText.text = input;
        yield return StartCoroutine(WaitForContinue(canContinue));
        serverText.text = "";
        serverText.color = SystemColor;
        ServerSpeaking = false;
    }

    private IEnumerator WaitForContinue(bool canContinue)
    {
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Return) && canContinue) break;
            yield return null;
        }
    }

    private void StartServerSpeak(Line line) {
        if (activeServerCoroutine != null) StopCoroutine(activeServerCoroutine);
        activeServerCoroutine = StartCoroutine(ServerSpeak(Fill(line.text), VoiceColor(line.voice), line.voice == Voice.Swarm, line.canContinue));
    }

    private IEnumerator PlayLines(Line[] lines) {
        if (lines == null || serverText == null) yield break;
        int generation = dialogueGeneration;
        foreach (Line line in lines) {
            while (ServerSpeaking) {
                if (generation != dialogueGeneration) yield break;
                yield return null;
            }
            if (generation != dialogueGeneration) yield break;
            StartServerSpeak(line);
            while (ServerSpeaking) yield return null;
        }
    }

    private void RunScene(IEnumerator scene) {
        ResetServerState();
        activeSceneCoroutine = StartCoroutine(SceneRoutine(scene));
    }

    private IEnumerator SceneRoutine(IEnumerator scene) {
        yield return null;
        yield return scene;
        activeSceneCoroutine = null;
    }

    IEnumerator ScreenViewAnimation(IEnumerator sceneToPlay)
    {
        float animationTime = 1f;
        float elapsedTime = 0;
        serverAnimationPlaying = true;
        screenAnimationDone = true;
        Camera main = Camera.main;

        Quaternion startRot = main.transform.rotation;
        Quaternion endRot = Quaternion.Euler(new Vector3(-20, 0 ,0));
        float startY = startRot.eulerAngles.y;
        float endY = endRot.eulerAngles.y;

        float startFOV = main.fieldOfView;
        float endFOV = 40;

        Vector3 startPos = main.transform.position;
        Vector3 endPos = new Vector3(0, 2.25f, 50.5f);

        Transform player = FindAnyObjectByType<PlayerMovement>().gameObject.transform;

        Vector3 playerStartPos = player.position;
        Vector3 playerEndPos = playerStartPos + (endPos - startPos);

        Quaternion playerStartRot = player.rotation;
        Quaternion playerEndRot = playerStartRot * Quaternion.Euler(new Vector3(0, startY - endY, 0));

        while (elapsedTime < animationTime)
        {
            float percentDone = elapsedTime/animationTime;

            main.transform.rotation = Quaternion.Slerp(startRot, endRot, percentDone);
            main.fieldOfView = Mathf.Lerp(startFOV, endFOV, percentDone);
            main.transform.position = Vector3.Lerp(startPos, endPos, percentDone);

            player.position = Vector3.Lerp(playerStartPos, playerEndPos, percentDone);
            player.rotation = Quaternion.Slerp(playerStartRot, playerEndRot, percentDone);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        main.transform.rotation = endRot;
        main.transform.position = endPos;
        main.fieldOfView = endFOV;
        player.position = playerEndPos;
        player.rotation = playerEndRot;

        yield return StartCoroutine(sceneToPlay);
        elapsedTime = 0;

        while (elapsedTime < animationTime)
        {
            float percentDone = elapsedTime/animationTime;

            main.transform.rotation = Quaternion.Slerp(endRot, startRot, percentDone);
            main.fieldOfView = Mathf.Lerp(endFOV, startFOV, percentDone);
            main.transform.position = Vector3.Lerp(endPos, startPos, percentDone);

            player.position = Vector3.Lerp(playerEndPos, playerStartPos, percentDone);
            player.rotation = Quaternion.Slerp(playerEndRot, playerStartRot, percentDone);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        main.transform.rotation = startRot;
        main.transform.position = startPos;
        main.fieldOfView = startFOV;
        player.position = playerStartPos;
        player.rotation = playerStartRot;

        serverAnimationPlaying = false;
    }

    private void OpenFinalPortal() {
        if (portal4 != null) portal4.transform.GetChild(0).GetComponent<FaceTextToPlayer>().PortalOpen();
    }

    private void Update() {
        if (playerCamera == null) return;
        if (PlayerMovement.Local == null) return;

        UpdateServerDetection();
        HandlePlatformEntry();
        UpdateServerPrompts();
        if (!serverAnimationPlaying) {
            RotateServer();
        }
    }

    private void HandlePlatformEntry()
    {
        if (FindAnyObjectByType<PlayerMovement>().onScreenPlatform)
        {
            if (!screenAnimationDone) {
                ResetServerState();
                StartCoroutine(ScreenViewAnimation(ServerScreenScene()));
            }
        } else
        {
            screenAnimationDone = false;
        }
    }

    IEnumerator ServerScreenScene()
    {
        bool introPlayed = false;
        if (!serverIntroDone) {
            yield return PlayLines(Script.ServerIntro);
            serverIntroDone = true;
            introPlayed = true;
        }

        if (keysDelivered < keyCount)
            yield return DeliverKeys();
        else if (SwarmEscaped)
            yield return PlayLines(Script.ServerAfterEscape);
        else if (!introPlayed)
            yield return PlayLines(Script.ServerIdle);
    }

    private IEnumerator DeliverKeys() {
        while (keysDelivered < keyCount) {
            yield return PlayLines(Script.KeyReceived);
            keysDelivered++;
            upgradeManager.Local.upgradePoints += TokenReward;
            SaveSystem.SavePlayerData();
            yield return PlayLines(Script.KeyDeliveries[Mathf.Min(keysDelivered, Script.KeyDeliveries.Length) - 1]);
        }
        if (!SwarmEscaped) yield return PlayLines(Script.FindTheRest);
    }

    private Line[] ServerArrivalLines() {
        if (!serverIntroDone) return Script.ServerArrivalFirst;
        if (SwarmEscaped) return Script.NoAgentsConnected;
        if (keysDelivered < keyCount) return Script.ServerArrivalWithKey;
        return Script.ServerArrivalIdle;
    }

    private void UpdateServerDetection() {
        seeingMazeServer = false;
        seeingSpaceServer = false;
        seeingIceServer = false;

        if (PlayerMovement.Local.currDimension == "Desert") return;

        Transform serverToDetect = null;
        ref bool seeing = ref seeingMazeServer;
        ref bool keyAcquired = ref mazeKeyAcquired;
        switch (PlayerMovement.Local.currDimension)
        {
            case "Maze": serverToDetect = mazeServerTransform;  break;
            case "Space": serverToDetect = spaceServerTransform; seeing = ref seeingSpaceServer; keyAcquired = ref spaceKeyAcquired; break;
            case "Ice": serverToDetect = iceServerTransform; seeing = ref seeingIceServer; keyAcquired = ref iceKeyAcquired; break;
        }
        if (serverToDetect == null) return;
        if (!keyAcquired && Vector3.Distance(playerCamera.transform.position, serverToDetect.position) < 5f && Vector3.Angle(playerCamera.transform.forward, serverToDetect.position - playerCamera.transform.position) < 30f) {
            seeing = true;
        }
    }

    private void UpdateServerPrompts() {
        UpdateAgentPrompt("Maze", seeingMazeServer, ref seenServerMaze);
        UpdateAgentPrompt("Space", seeingSpaceServer, ref seenSpaceServer);
        UpdateAgentPrompt("Ice", seeingIceServer, ref seenIceServer);
    }

    private void UpdateAgentPrompt(string dimension, bool seeing, ref bool seen) {
        if (seeing) {
            if (!seen) {
                RunScene(PlayLines(Script.Dimensions[dimension].found));
                seen = true;
            } else if (!ServerSpeaking && activeSceneCoroutine == null) {
                serverText.text = feedPrompt;
            }
        } else if (PlayerMovement.Local.currDimension == dimension && serverText.text == feedPrompt) {
            serverText.text = "";
        }
    }

    public void TryFeed() {
        if (seeingMazeServer && !mazeKeyAcquired)
            Feed(ref mazeKeyAcquired, KeyCutscene("Maze"));
        else if (seeingSpaceServer && !spaceKeyAcquired)
            Feed(ref spaceKeyAcquired, KeyCutscene("Space"));
        else if (seeingIceServer && !iceKeyAcquired)
            Feed(ref iceKeyAcquired, KeyCutscene("Ice"));
    }

    private void Feed(ref bool keyAcquired, IEnumerator cutscene) {
        if (upgradeManager.Local.killPoints >= EssenceCost) {
            upgradeManager.Local.killPoints -= EssenceCost;
            keyAcquired = true;
            RunScene(cutscene);
            SaveSystem.SavePlayerData();
        } else if (activeSceneCoroutine == null) {
            RunScene(PlayLines(Script.NotEnoughEssence));
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

    public void DisplayDimension() {
        RunScene(DimensionEntryScene(PlayerMovement.Local.currDimension));
    }

    private IEnumerator DimensionEntryScene(string dimension) {
        while (RetroDither.TeleportPixelizeActive) yield return null;

        if (!global::SceneManager.Instance.InCombatScene) {
            yield return PlayLines(ServerArrivalLines());
            yield break;
        }

        if (!Script.Dimensions.TryGetValue(dimension, out DimensionScript script)) yield break;

        bool firstVisit = enteredDimensions.Add(dimension);
        yield return PlayLines(firstVisit ? script.firstEntry : script.reentry);

        if (dimension == "Desert" && AllKeysAcquired) OpenFinalPortal();
        yield return PlayLines(DimensionStatusLines(dimension, script));
    }

    private Line[] DimensionStatusLines(string dimension, DimensionScript script) {
        bool hasAgent = script.found != null;
        if (SwarmEscaped) return hasAgent ? Script.SandboxEmpty : Script.NoAgentsConnected;
        if (!hasAgent) return AllKeysAcquired ? Script.DesertAllKeysFound : script.waiting;
        return KeyAcquired(dimension) ? Script.AgentAlreadyFed : script.waiting;
    }

    private bool KeyAcquired(string dimension) {
        switch (dimension) {
            case "Maze": return mazeKeyAcquired;
            case "Space": return spaceKeyAcquired;
            case "Ice": return iceKeyAcquired;
            default: return false;
        }
    }

    public IEnumerator StartFirstKillScene() {
        while (ServerSpeaking || activeSceneCoroutine != null) yield return null;
        yield return PlayLines(Script.FirstKill);
    }

    private IEnumerator KeyCutscene(string dimension) {
        DimensionScript script = Script.Dimensions[dimension];
        Transform server = getServerTransform();
        GameObject serverObj = server != null ? server.gameObject : null;
        Vector3 posChange = GetKeySceneMovement();
        Quaternion camRot = cam.rotation;
        yield return StartCoroutine(KeyCutsceneStart(posChange));

        yield return PlayLines(script.training);

        if (serverObj != null) yield return StartCoroutine(GiveKey(serverObj.transform.position));

        yield return PlayLines(script.afterKey);

        if (AllKeysAcquired) {
            OpenFinalPortal();
            yield return PlayLines(Script.AllKeysFound);
        } else {
            yield return PlayLines(Script.FindTheRest);
        }

        if (serverObj != null) serverObj.SetActive(false);
        yield return StartCoroutine(KeyCutsceneEnd(posChange, camRot));
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
        float startFOV = playerCamera.fieldOfView;
        float baseFOV = PlayerMovement.Local.BaseFOV;
        while (timeAcc < lerpTime) {
            timeAcc += Time.deltaTime;
            cam.position = Vector3.Lerp(startPosition, startPosition + posChange, timeAcc / lerpTime);
            cam.LookAt(server.position - transform.forward);
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, baseFOV, timeAcc / lerpTime);
            yield return null;
        }
        playerCamera.fieldOfView = baseFOV;
        PlayerMovement.Local.SnapToBaseFOV();
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
        float rotationSpeed = 360f;
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
                server = iceServerTransform;
                break;
            case "Space":
                server = spaceServerTransform;
                break;
            case "Maze":
                server = mazeServerTransform;
                break;
            default:
                server = null;
                break;
        }
        return server;
    }
}
