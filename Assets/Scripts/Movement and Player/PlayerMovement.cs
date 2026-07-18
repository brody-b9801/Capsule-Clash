using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Alteruna;
using TMPro;
using NUnit.Framework;
using Unity.VisualScripting;
using Cinemachine.Utility;

[RequireComponent(typeof(MaskController))]
public class PlayerMovement : AttributesSync {
    public static float moveSpeed = 4.0f;
    [SerializeField] private float jumpForce = 8.0f;
    public static float rotationSpeed = 10.0f;
    [SerializeField] private float maxLookUpAngle = 80.0f;
    [SerializeField] private float maxLookDownAngle = 80.0f;
    [SerializeField] private float airSpeed = 8.0f;
    [SerializeField] private float sprintSpeed = 8.0f;
    private Camera playerCamera;
    [SerializeField] private Transform gun;
    public static Vector3 gunRotation;
    public static float healthWidth = 180.0f; 
    [SerializeField] private Renderer playerRenderer;
    public static bool isGrounded = false;
    public static bool fastAir = false;
    public static float currentCameraRotationX = 0.0f;
    private float currentCameraRotationY = 0.0f;
    public static bool isAiming = false;
    public static bool isSprinting = false;
    public static Vector3 newPosition;
    public static Vector3 movement;
    private Alteruna.Avatar _avatar;
    private SyncedAxis _horizontal;
    private SyncedAxis _vertical;
    private SyncedKey _jump;
    private SyncedKey _dash;
    public static bool started = false;
    [SerializeField] private GameObject capsuleCollider;
    [SerializeField] private Transform playerTransform;
    private Vector3 lastPosition;
    private static Vector3 velocityTransform;
    private InputSynchronizable _input;
    [SerializeField] private Transform bulletHole;
    private float newAlpha = 0.0f;
    private float newAlpha1 = 0.0f;
    private static Vector3 spawn;
    [SerializeField] private GameObject borderPrefab;
    private Transform borderInstance;
    private float rotationX;
    private float rotationY;
    private Transform cam2;
    private Transform akm;
    private Vector3 akmBaseLocalPos;
    private Vector3 akmBaseLocalRot;
    [SerializeField] private float forceMod;
    private CapsuleCollider meshCollider;
    public static bool onSlope;
    private GameObject portal1A;
    private GameObject portal1B;
    private GameObject portal2A;
    private GameObject portal2B;
    private GameObject portal3A;
    private GameObject portal3B;
    private GameObject portal4;
    private bool canTeleport = true;
    private bool checkTele = true;
    [SerializeField] private float launchForce;
    public static string avatarString;
    private Vector3 remainingMovement;
    private float speedMod;
    [SerializeField] private float dashForce = 10.0f;
    private static int dashes = 0;
    private bool resettingDashes = false;
    private Vector3 hitPoint;
    public static bool jumpedLast = false;
    public static bool isTeleporting = false;
    public static bool isDashing = false;
    public static Vector3 newVelocity;
    private bool groundedPrev;
    private bool groundBeneath;
    private bool sprintingPrev;
    private CharacterController characterController;
    private bool resetPrev = false;
    private bool launch = false;
    private Vector3 planeToProject;
    private float angleSlope;
    private Transform gunThing;
    [SerializeField] private float targetWalkXPos;
    [SerializeField] private float targetWalkYPos;
    [SerializeField] private float targetWalkZPos;
    [SerializeField] private float targetWalkXRot;
    [SerializeField] private float targetWalkYRot;
    [SerializeField] private float targetWalkZRot;
    private bool lerpingWalk = false;
    public static bool lerpingWalkDone = false;
    private bool lerpingWalkEnd = false;
    private bool lerpingWalkDoneEnd = true;
    public static float jumpOffset;
    private float jumpOffsetTwo;
    private bool lerpingJump = false;
    private bool lerpingJumpTwo = false;

    // Cached masks and reusable collections
    private static int _groundMask = -1;
    private static int GroundMask { get { if (_groundMask == -1) _groundMask = LayerMask.GetMask("Default", "BuildNoColPlayer"); return _groundMask; } }
    private static int _defaultMask = -1;
    private static int DefaultMask { get { if (_defaultMask == -1) _defaultMask = LayerMask.GetMask("Default"); return _defaultMask; } }
    private static int _slopeMask = 0;
    private static int SlopeMask { get { if (_slopeMask == 0) _slopeMask = ~LayerMask.GetMask("Player", "Bullet", "FiredBullet", "DCSelf", "DamageCollide", "IgnoreRaycast", "BuildNoColPlayer"); return _slopeMask; } }
    private readonly List<string> _hitNames = new List<string>();
    private RaycastHit[] _capsuleHits = new RaycastHit[16];
    private Renderer[] _borderRenderers;

    private float gunXRot;
    private float gunYRot;
    private bool lerpingXRot = false;
    private bool lerpingYRot = false;
    public float targetAimXPos;
    public float targetAimZPos;
    public float targetAimXRot;
    public float targetAimYRot;
    private bool lerpingAim = false;
    private bool lerpingAimDone = false;
    private bool lerpingAimEnd = false;
    private bool lerpingAimDoneEnd = true;
    private Vector3 aimVectorPos = Vector3.zero;
    private Vector3 aimVectorRot = Vector3.zero;
    private Vector3 walkVectorPos = Vector3.zero;
    private Vector3 walkVectorRot = Vector3.zero;
    public static Vector3 dashVector;
    public static float dashFOV = 0;
    private TextMeshProUGUI dt;
    private bool lerpingDash = false;
    private RectTransform dashIcon;
    public static bool canTakeDamage = true;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private LayerMask collisionMask2;
    public float unstuckDistance = 0.1f;
    private int maxUnstuckAttempts = 30;
    private bool isStuck = false;
    private List<Vector3> spawnVectors = new List<Vector3>();
    public Transform spawnPosContainer;
    
    // Dimension-specific spawn containers
    public Transform desertSpawnPosContainer;
    private Transform mazeSpawnPosContainer;
    private Transform spaceSpawnPosContainer;
    private Transform iceSpawnPosContainer;    
    // Dimension-specific spawn vectors
    private List<Vector3> desertSpawnVectors = new List<Vector3>();
    private List<Vector3> mazeSpawnVectors = new List<Vector3>();
    private List<Vector3> spaceSpawnVectors = new List<Vector3>();
    private List<Vector3> iceSpawnVectors = new List<Vector3>();
    private static string avatarRef;
    public static int hitCount = 0;
    public GameObject respawnScreen;
    private GameObject respawnInit;
    public static bool dead = false;
    private List<Collider> NoColPlayerBuilds;
    private bool unstuckFail = false;
    private float lastGroundedHeight;
    private float sideTilt;
    private float targetSideTilt;
    private TextMeshProUGUI usernameText;
    public string username;
    public int killCount = 0;
    private GameObject sceneLight;
    [SerializeField] private float walkAnimTuneGun = 1f;
    [SerializeField] private float walkAnimTune = 1f;
    [SerializeField] private float jumpAnimTune = 1f;
    [SerializeField] private float shootAnimTune = 1f;
    [SerializeField] private float turnAnimTune = 1f;
    [SerializeField] private float sidewaysAnimTune = 1f;
    [SerializeField] private float breatheAnimTune = 1f;
    [SerializeField] private Material selfMaterial;
    public static float elapsedHealTime;

    // Controllers
    private SettingsController settingsControl;
    private LeaderboardControl leaderboardControl;
    public GameObject usernameDisplay;
    // Enhanced Acceleration variables
    [Header("Movement Physics")]
    [SerializeField] private float groundAcceleration = 20f;
    [SerializeField] private float groundDeceleration = 25f;
    [SerializeField] private float airAcceleration = 3f;
    [SerializeField] private float airDeceleration = 2f;
    [SerializeField] private float friction = 8f;
    [SerializeField] private float sprintAccelerationMultiplier = 1.5f;
    [SerializeField] private float airControlMultiplier = 0.5f;

    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 wishDir = Vector3.zero;
    public static float percentAccelerated;

    // Enhanced visual effects
    [Header("Visual Effects")]
    [SerializeField] private float speedFOVBoost = 10f;
    [SerializeField] private float sprintFOVBoost = 5f;
    [SerializeField] private float landingCameraShake = 0.15f;
    [SerializeField] private float velocityBasedTilt = 2f;
    [SerializeField] private float momentumCameraSway = 0.5f;

    private float baseFOV = 70f;
    private float targetFOV = 70f;
    private float currentFOV = 70f;
    private float velocityFOVBoost = 0f;
    private float currentSprintFOV = 0f;
    private Vector3 landingCameraOffset = Vector3.zero;

    // Collision velocity tracking
    private Vector3 lastFrameMovement = Vector3.zero;

    public static string currDimension = "Desert";
    private float gravity = 9.81f;
    private MaskController maskController;

    // ---Skybox Materials ---
    private Material desertSky;
    public Material spaceSky;
    public Material iceSky;
    public static Vector3 shotBoost;
    // -------------------------------------------------------------------------
    // Input
    // -------------------------------------------------------------------------
    private void InitializeInput() {
        _input = GetComponent<InputSynchronizable>();
        _horizontal = new SyncedAxis(_input, "Horizontal");
        _vertical = new SyncedAxis(_input, "Vertical");
        _jump = new SyncedKey(_input, KeyCode.Space);
        _dash = new SyncedKey(_input, KeyCode.Space, SyncedKey.KeyMode.KeyDown);
    }

    public static Vector3 getVelocity() { return velocityTransform; }
    public int getKills() { return killCount; }
    public string getUsername() { return username; }
    private struct DimensionInfo {
        public string name;
        public string materialName;
        public Material skybox;
        public float farClip;
        public float gravity;
        public float accel;
        public float decel;
        public float fric;
        public bool snow;
        public GameObject root;
    }

    private DimensionInfo desertInfo;
    private DimensionInfo mazeInfo;
    private DimensionInfo spaceInfo;
    private DimensionInfo iceInfo;
    private DimensionInfo[] allDimensions;
    Vector3 floorNormal = Vector3.up;

    private void InitializeDimensions() {
        desertInfo = new DimensionInfo {
            name = "Desert",
            materialName = "Desert",
            skybox = desertSky,
            farClip = 1000f,
            gravity = 9.81f,
            accel = 1000f,
            decel = 1000f,
            fric = 8f,
            snow = false,
            root = GameObject.Find("Desert")
        };
        mazeInfo = new DimensionInfo {
            name = "Maze",
            materialName = "Maze",
            skybox = null,
            farClip = 10f,
            gravity = 9.81f,
            accel = 1000f,
            decel = 1000f,
            fric = 8f,
            snow = false,
            root = GameObject.Find("Maze")
        };
        spaceInfo = new DimensionInfo {
            name = "Space",
            materialName = "Void",
            skybox = spaceSky,
            farClip = 400f,
            gravity = 6f,
            accel = 1000f,
            decel = 1000f,
            fric = 8f,
            snow = false,
            root = GameObject.Find("Void")
        };
        iceInfo = new DimensionInfo {
            name = "Ice",
            materialName = "Ice",
            skybox = iceSky,
            farClip = 400f,
            gravity = 9.81f,
            accel = 10f,
            decel = 10f,
            fric = 1f,
            snow = true,
            root = GameObject.Find("Ice")
        };
        allDimensions = new DimensionInfo[] { desertInfo, mazeInfo, spaceInfo, iceInfo };
    }

    private void SetActiveDimension(DimensionInfo target) {
        for (int i = 0; i < allDimensions.Length; i++) {
            GameObject root = allDimensions[i].root;
            if (root != null) root.SetActive(allDimensions[i].name == target.name);
        }
    }

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start() {
        Application.targetFrameRate = -1;
        InitializeInput();
        _avatar = GetComponent<Alteruna.Avatar>();

        if (_avatar.IsOwner) {
            settingsControl = GameObject.Find("Room Menu (1)").GetComponent<SettingsController>();
            leaderboardControl = GetComponent<LeaderboardControl>();
            maskController = GetComponent<MaskController>();
            iceSpawnPosContainer = GameObject.Find("IceSpawnContainer").transform;
            spaceSpawnPosContainer = GameObject.Find("VoidSpawnContainer").transform;
            mazeSpawnPosContainer = GameObject.Find("MazeSpawnContainer").transform;
            killCount = 0;
            Shooting.canShoot = true;
            Shooting.lockCursor = true;
            usernameText = GameObject.Find("UsernameInput").GetComponent<TextMeshProUGUI>();
            dt = GameObject.Find("DashText").GetComponent<TextMeshProUGUI>();
            sceneLight = GameObject.Find("DynamicLight");
            meshCollider = GetComponent<CapsuleCollider>();
            avatarString = _avatar.ToString();
            started = true;
            playerCamera = Camera.main;
            baseFOV = playerCamera.fieldOfView;
            currentFOV = baseFOV;
            cam2 = GameObject.Find("CameraTwo").transform;
            gunThing = GameObject.Find("gunThing").transform;
            akm = GameObject.Find("CamAKM").transform;
            akmBaseLocalPos = akm.localPosition;
            akmBaseLocalRot = akm.localEulerAngles;
            portal1A = GameObject.Find("portal1B");
            portal1B = GameObject.Find("portal1A");
            portal2A = GameObject.Find("portal2B");
            portal2B = GameObject.Find("portal2A");
            portal3A = GameObject.Find("portal3B");
            portal3B = GameObject.Find("portal3A");
            portal4 = GameObject.Find("portal4");
            characterController = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            lastPosition = playerTransform.position;
            capsuleCollider.layer = 10;
            transform.GetComponent<Renderer>().material = selfMaterial;
            borderInstance = Instantiate(borderPrefab, Vector3.zero, Quaternion.identity).transform;
            _borderRenderers = borderInstance.GetComponentsInChildren<Renderer>();
            spawn = transform.position;
            dashIcon = GameObject.Find("dashBG").GetComponent<RectTransform>();
            
            // Initialize dimension-specific spawn vectors
            // If desertSpawnPosContainer is not set, use the old spawnPosContainer as desert spawns
            Transform desertContainer = spawnPosContainer;
            foreach (Transform s in desertContainer) 
                desertSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            foreach (Transform s in mazeSpawnPosContainer)
                mazeSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            foreach (Transform s in spaceSpawnPosContainer)
                spaceSpawnVectors.Add(new Vector3(s.position.x, s.position.y + 5.0f, s.position.z));
            foreach (Transform s in iceSpawnPosContainer)
                    iceSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            avatarRef = _avatar.ToString();
            healthWidth = 180.0f;
            canTakeDamage = false;
            HealthController.updateHealth();
            Shooting.reloadNum = 30;
            GunThingAnim.movingState = false;
            dashes = 0;
            characterController.enableOverlapRecovery = false;
            ObjectSpawner.buildNum = 25;
            lastGroundedHeight = -13;
            currentCameraRotationX = 0;
            currentCameraRotationY = 0;
            playerCamera.transform.localEulerAngles = Vector3.zero;
            rotationSpeed = SettingsController.rs;

            string result = usernameText.text.Replace(" ", "");
            string finalResult = result.Replace("​", "");
            username = (finalResult.Length > 0) ? usernameText.text : "Player";

            // Kick off the opening scene
            RetroDither.isTeleporting = true;
            maskController.Initialize(playerCamera);
            maskController.BeginOpeningScene();
            Camera.main.GetComponent<FogShader>().ChangeDimension("Desert");

            desertSky = RenderSettings.skybox;
            InitializeDimensions();
            SetActiveDimension(desertInfo);
            GetComponent<MeshRenderer>().enabled = false;
        } else {
            foreach (Transform child in transform) {
                if (child.name == "Renderer") child.gameObject.SetActive(false);
            }
        }

    }

    public static bool getAvatarBool(string avatar1) {
        return avatar1.Equals(avatarString) && canTakeDamage;
    }

    private void FixedUpdate() {
        Vector3 currentPosition = playerTransform.position;
        velocityTransform = (currentPosition - lastPosition) / Time.deltaTime;
        lastPosition = currentPosition;
    }
    public bool isGround()
    {
        RaycastHit hit;
        if (Physics.SphereCast(transform.position + characterController.center, characterController.radius, Vector3.down, out hit, characterController.height / 2f + characterController.skinWidth * 2, GroundMask, QueryTriggerInteraction.Ignore)) {
            floorNormal = hit.normal;
            return true;
        }
        return false;
    }

    private bool CanJump() { return isGrounded; }

    private void Update() {
        if (!_avatar.IsOwner) return;

        IsOnSlope();
        CheckIfStuckAndMoveUp();
        dt.text = dashes.ToString();

        usernameDisplay.transform.gameObject.GetComponent<MeshRenderer>().enabled = false;

        isGrounded = isGround();
        lastFrameMovement = movement;
        HandleCameraRotation();
        HandleMovement();
        HandleJumpAndGravity();
        
        SetExtraneousStates(); //needs cleanup
        HandleLaunch();
        KeyEvents();
        HandleDashing();
        HandleShotBoost();
        ManageSprintState();
        SetAimRotSpeed();
        BorderWarning();
        UpdateDynamicFOV();
    }

    private float SetTargetSpeed()
    {
        float baseSpeed;
        if (isAiming)
            baseSpeed = 2.5f;
        else if (isSprinting && isGrounded)
            baseSpeed = 12.0f;
        else if (isSprinting && fastAir)
            baseSpeed = 10.0f;
        else if (!isGrounded)
            baseSpeed = 7.5f;
        else
            baseSpeed = 8.5f;

        return baseSpeed * upgradeManager.speedMultiplier;
    }

private void HandleMovement()
{
    Vector3 inputDirection = new Vector3(_horizontal, 0, _vertical);
    if (inputDirection.magnitude > 1f) inputDirection.Normalize();

    Vector3 forward = transform.forward;
    Vector3 right = transform.right;
    forward.y = 0f;
    right.y = 0f;

    Vector3 moveDirection = forward * inputDirection.z + right * inputDirection.x;
    if (moveDirection.magnitude > 1f) moveDirection.Normalize();

    float targetSpeed = SetTargetSpeed();

    characterController.Move((moveDirection * targetSpeed + (upgradeManager.dashForceMultiplier * dashVector)) * Time.deltaTime - shotBoost * 10 * Time.deltaTime);

    //percentAccelerated = Mathf.Clamp01(new Vector3(movement.x, 0, movement.z).magnitude / (targetSpeed * 0.8f));
    percentAccelerated = 1;
}
    private void HandleJumpAndGravity() {
        if (_jump) maskController.TryFeed();

        if (_jump && isGrounded && !isAiming && !maskController.LookingAtMask) {
            if (currDimension == "Maze")
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + jumpForce, 0, Mathf.Infinity);
            else if (currDimension == "Space")
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + 1.5f * jumpForce * upgradeManager.jumpMultiplier, 0, Mathf.Infinity);
            else
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + jumpForce * upgradeManager.jumpMultiplier, 0, Mathf.Infinity);
            jumpedLast = true;
            resetPrev = false;
            if (isSprinting) fastAir = true;
        }
        bool wasGrounded = characterController.isGrounded;

        if (wasGrounded && newVelocity.y <= 0 && !characterController.isGrounded && !jumpedLast)
        {
            characterController.Move(Vector3.down * SetTargetSpeed() * Time.deltaTime * Mathf.Tan(characterController.slopeLimit));
        }
        if (!isGrounded && groundedPrev && !jumpedLast)
        {
            newVelocity.y = 0;
            Debug.Log("test");
        } 
        newVelocity.y -= gravity * Time.deltaTime;
        Vector3 verticalVelo = Vector3.Angle(floorNormal, Vector3.up) > characterController.slopeLimit ? Vector3.ProjectOnPlane(newVelocity, floorNormal) : newVelocity;
        characterController.Move(verticalVelo * Time.deltaTime);
        groundedPrev = isGrounded;
    }

    private void HandleCameraRotation() {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");
        rotationX = -(mouseY * rotationSpeed);
        rotationY = mouseX * rotationSpeed;

        currentCameraRotationX += rotationX;
        currentCameraRotationX = Mathf.Clamp(currentCameraRotationX, -maxLookDownAngle, maxLookUpAngle);
        currentCameraRotationY += rotationY;
        transform.localEulerAngles = new Vector3(0.0f, currentCameraRotationY, 0.0f);
    }

    private void SetExtraneousStates() {
        RaycastHit hit;
        if (isGrounded) {
            characterController.stepOffset = (currDimension != "Maze") ? 0.55f : 0f;
            if (currDimension == "Desert") {
                Vector3 p1 = transform.position + Vector3.up * 0.5f;
                Vector3 p2 = transform.position + Vector3.down * 0.5f;
                int capsuleHitCount = Physics.CapsuleCastNonAlloc(p1, p2, 0.55f, wishDir, _capsuleHits, 0.5f, DefaultMask);

                _hitNames.Clear();
                bool noStep = false;
                for (int chi = 0; chi < capsuleHitCount; chi++) {
                    RaycastHit collision = _capsuleHits[chi];
                    string hitName = collision.transform.gameObject.name;
                    _hitNames.Add(hitName);
                    if (hitName.Contains("Tree")) {
                        noStep = true;
                    } else if (hitName.Contains("Building")) {
                        string input = hitName.Substring(9, 1);
                        int outVal;
                        int.TryParse(input, out outVal);
                        if ((outVal < 5 && outVal > 0) || outVal == 8) noStep = true;
                    } else if (hitName.Contains("MarketplaceTop")) {
                        noStep = true;
                    }
                }
                if (_hitNames.Contains("Sand") && noStep) characterController.stepOffset = 0;
            } else if (currDimension == "Ice") {
                if (Physics.SphereCast(transform.position, 0.545f, Vector3.down, out hit, 0.5f, SlopeMask)) {
                    if (hit.transform.gameObject.tag == "Ramp" || hit.transform.gameObject.tag == "Floor" || hit.transform.gameObject.tag == "Wall")
                    {
                        setFrictionIce(false);
                    } else
                    {
                        setFrictionIce(true);
                    }
                } else
                {
                    setFrictionIce(true);
                }
            }
        } else {
            characterController.stepOffset = 0f;
            groundBeneath = false;
            if (groundedPrev) {
                lastGroundedHeight = transform.position.y;
            }
        }
        if (!canTakeDamage) StartCoroutine(Invulnerable());
        CameraZoom.moving = (_horizontal || _vertical);
    }

    private void HandleDashing() {
        if (_dash && dashes > 0 && !isGround() && currDimension != "Maze") {
            HealthController.noFDAnim = true;
            dashes--;
            dashVector = Vector3.Project(dashVector, playerCamera.transform.forward * dashForce)
                         + (playerCamera.transform.forward * dashForce);
            newVelocity.y = 0;
            jumpedLast = true;
            dashFOV = 20;
            lastGroundedHeight = -30;
            if (!lerpingDash) StartCoroutine(LerpDash());
        } else {
            dashFOV = 0;
        }

        if (!resettingDashes) StartCoroutine(addDash());
    }

    private void HandleLaunch()
    {
        // Launch pads
        if (launch) {
            newVelocity.y = launchForce;
            jumpedLast = true;
            resetPrev = false;
            HealthController.noFDAnim = true;
            StartCoroutine(resetLaunch());
        }
    }
    private void KeyEvents()
    {
        // Respawn

        if (Input.GetKey(KeyCode.R) && dead) Respawn();

        // Healing

        if (Input.GetKey(KeyCode.Q) && !Shooting.reloading && !CameraZoom.moving && !Shaker.shooting
            && isGrounded && !healParticles.healing && healthWidth < 180.0f)
            StartCoroutine(stationaryHealing());
        
        // Sprint

        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            && isGround() && StaminaController.canSprint && !isAiming) {
            isSprinting = true;
        } else {
            if (!fastAir) isSprinting = false;
        }

        if ((Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)) && !isAiming) {
            isSprinting = false;
            fastAir = false;
        }
        
        // Unlock cursor
        
        if (Input.GetKeyUp(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            Shooting.lockCursor = false;
        }
    }

    private void ManageSprintState() {
        if (!StaminaController.canSprint) fastAir = false;

        if (sprintingPrev && groundedPrev && !isGround()) {
            fastAir = true;
            isSprinting = true;
        }
    }

    private void HandleShotBoost() {        
        if (currDimension != "Ice" || !isGround())
        {
            shotBoost = Vector3.zero;
        }
        shotBoost = Vector3.Lerp(shotBoost, Vector3.zero, Time.deltaTime);
    }

    private void UpdateDynamicFOV() {
        float horizontalSpeed = new Vector3(velocityTransform.x, 0, velocityTransform.z).magnitude;
        velocityFOVBoost = Mathf.Lerp(velocityFOVBoost, isTeleporting ? 0f : (horizontalSpeed / 15f) * speedFOVBoost, Time.deltaTime * 5f);

        bool sprintingAndMoving = isSprinting && movement.magnitude > 0.1f && !isAiming;
        float sprintFOVTarget = sprintingAndMoving ? sprintFOVBoost : 0f;
        currentSprintFOV = Mathf.Lerp(currentSprintFOV, sprintFOVTarget, Time.deltaTime * 8f);
        float aimOffset = CameraZoom.aimZoomOffset;

        targetFOV = isAiming
            ? baseFOV + aimOffset + dashFOV
            : baseFOV + velocityFOVBoost + currentSprintFOV + dashFOV + aimOffset;

        currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * 8f);
        if (playerCamera != null) playerCamera.fieldOfView = currentFOV + Shaker.FOVModRef;
    }

    private void SetAimRotSpeed()
    {
        if ((Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && isGround()) {
            isAiming = true;
            rotationSpeed = SettingsController.rs * (1.5f / 4.0f);
        } else {
            rotationSpeed = SettingsController.rs;
            isAiming = false;
        }
    }
    IEnumerator addDash() {
        float elapsedTime = 0;
        if (dashes < 3) {
            resettingDashes = true;
            float total = 10 * (1.0f / upgradeManager.dashRegenMultiplier);
            if (currDimension == "Space")
            {
                total *= 0.25f;
            }
            while (elapsedTime < total) {
                elapsedTime += Time.deltaTime;
                float percent = elapsedTime / total;
                dashIcon.sizeDelta = new Vector2(75, percent * 72);
                float totalPrev = total;
                total = 10 * (1.0f / upgradeManager.dashRegenMultiplier);
                if (currDimension == "Space")
                {
                    total *= 0.25f;
                }
                if (totalPrev != total) elapsedTime = percent * total;
                yield return null;
            }
            dashes++;
            resettingDashes = false;
        }
    }

    IEnumerator resetLaunch() {
        yield return new WaitForSeconds(0.01f);
        launch = false;
    }

    void OnControllerColliderHit(ControllerColliderHit hit) {
        if (!_avatar.IsOwner) return;

        dashVector = Vector3.zero;
        planeToProject = hit.normal;
        string hitTag = hit.transform.gameObject.tag;

        if (isGrounded) {
            if (!groundedPrev && hitTag != "Launchpad" && hitTag != "Portal") {
                float heightChange = (lastGroundedHeight - transform.position.y) - 8;
                if (heightChange > 0) {
                    float shakeMagnitude = Mathf.Min(heightChange / 20f, 1f);
                    StartCoroutine(ApplyLandingShake(shakeMagnitude));
                    healthWidth -= ((int)(heightChange / 4)) * 12;
                }
                if (healthWidth <= 0) Die();
                HealthController.updateHealth();
            }
            jumpedLast = false;
            resetPrev = false;
        }

        if (hit.gameObject.CompareTag("Launchpad")) launch = true;

        // if (hit.normal.y < -0.85f && !resetPrev && newVelocity.y > 0) {
        //     newVelocity.y = 0;
        //     resetPrev = true;
        // }


        if (IsTopFaceCollision(hit) && isGround()) fastAir = false;

        if (canTeleport) {
            if (hit.gameObject == portal1A) HandleTeleportation(portal1B, desertInfo);
            else if (hit.gameObject == portal1B) HandleTeleportation(portal1A, mazeInfo);
            else if (hit.gameObject == portal2A) HandleTeleportation(portal2B, desertInfo);
            else if (hit.gameObject == portal2B) HandleTeleportation(portal2A, spaceInfo);
            else if (hit.gameObject == portal3A) HandleTeleportation(portal3B, desertInfo);
            else if (hit.gameObject == portal3B) HandleTeleportation(portal3A, iceInfo);
            else if (hit.gameObject == portal4 && MaskController.keyCount == 3) {
                //Start boss fight scene, to be implemented
            }
        }
    }
        private void setFrictionIce(bool onIce) {
        groundAcceleration = onIce ? iceInfo.accel : desertInfo.accel;
        groundDeceleration = onIce ? iceInfo.decel : desertInfo.decel;
        friction = onIce ? iceInfo.fric : desertInfo.fric;
        characterController.stepOffset = onIce ? 0f : 0.55f;
    }



    private bool IsTopFaceCollision(ControllerColliderHit collision) {
        return Vector3.Angle(collision.normal, Vector3.up) <= characterController.slopeLimit;
    }

    private bool IsOnSlope() {
        RaycastHit hit;
        LayerMask layerMask = SlopeMask;
        if (!Physics.SphereCast(transform.position, 0.545f, Vector3.down, out hit, 0.5f, layerMask)) {
            onSlope = false;
            return false;
        }
        angleSlope = Vector3.Angle(hit.normal, Vector3.up);
        if (angleSlope > 0 && angleSlope <= 47) {
            onSlope = true;
            return true;
        }
        onSlope = false;
        return false;
    }

    private void BorderWarning() {
        float horizontalDistanceFromOrigin;
        float absX = Mathf.Abs(transform.position.x) - 60; //60 = x size of bounds
        float absZ = Mathf.Abs(transform.position.z) - 90; //90 = z size of bounds

        float yDistanceFromOrigin = transform.position.y;
        if (currDimension == "Space") {
            absX = Mathf.Abs(transform.position.x) - 55;
            absZ = Mathf.Abs(transform.position.z - 2000) - 55;//2000 = z pos of void container
        } else if (currDimension == "Ice") {
            absX = Mathf.Abs(transform.position.x + 2500) - 55;
            absZ = Mathf.Abs(transform.position.z) - 55;
        }
        
        horizontalDistanceFromOrigin = Mathf.Max(absX, absZ);

        if (currDimension == "Desert") {
            newAlpha = horizontalDistanceFromOrigin > 0f ? (horizontalDistanceFromOrigin / 20f) * 255f : 0f;
            newAlpha1 = yDistanceFromOrigin > 55f ? ((yDistanceFromOrigin - 55f) / 20f) * 255f : 0f;
        } else if (currDimension == "Space") {
            newAlpha = horizontalDistanceFromOrigin > 0f ? (horizontalDistanceFromOrigin / 20f) * 255f : 0f;
            newAlpha1 = Mathf.Abs(yDistanceFromOrigin) > 50f ? ((Mathf.Abs(yDistanceFromOrigin - 50f)) / 20f) * 255f : 0f;
        } else if (currDimension == "Ice") {
            newAlpha = horizontalDistanceFromOrigin > 0f ? (horizontalDistanceFromOrigin / 20f) * 255f : 0f;
            newAlpha1 = yDistanceFromOrigin > 55f ? ((yDistanceFromOrigin - 55f) / 20f) * 255f : 0f;
        }

        float alphaToUse = Mathf.Clamp(Mathf.Max(newAlpha, newAlpha1), 0, 30f);
        Color borderColor = new Color(1f, 0f, 0f, alphaToUse / 255f);
        for (int i = 0; i < _borderRenderers.Length; i++)
            _borderRenderers[i].sharedMaterial.color = borderColor;
    }


    //-----Death/Damage-----//     
    public void Die() {             
        characterController.enabled = false;
        dead = true;
        if (_avatar.IsOwner) respawnInit = Instantiate(respawnScreen);
        Cursor.lockState = CursorLockMode.None;
        Shooting.lockCursor = false;
        transform.position = new Vector3(0, -30, 0);
        RenderSettings.skybox = desertSky;
    }
    public void Respawn() {
        healthWidth = 180.0f;
        canTakeDamage = false;
        HealthController.updateHealth();
        Shooting.reloadNum = 30;
        GunThingAnim.movingState = false;
        dashes = 0;
        ObjectSpawner.buildNum = 25;
        transform.localEulerAngles = Vector3.zero;
        movement = Vector3.zero;
        newVelocity = Vector3.zero;
        characterController.enabled = false;
        dead = false;
        lastGroundedHeight = -13;
        ChangeMat.healed = false;
        movement = Vector3.zero;
            
        // Select appropriate spawn list based on current dimension
        List<Vector3> currentSpawns = desertSpawnVectors; // default to desert spawns
        if (currDimension == "Maze" && mazeSpawnVectors.Count > 0)
            currentSpawns = mazeSpawnVectors;
        else if (currDimension == "Space" && spaceSpawnVectors.Count > 0)
            currentSpawns = spaceSpawnVectors;
        else if (currDimension == "Ice" && iceSpawnVectors.Count > 0)
            currentSpawns = iceSpawnVectors;
            
        int num = Random.Range(0, currentSpawns.Count);
        transform.position = currentSpawns[num];

        Vector3 targetDirection = new Vector3(17f - 13.94f, -9f, -27f + 3.89f) - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        currentCameraRotationY = targetRotation.eulerAngles.y;

        characterController.enabled = true;
        Shooting.canShoot = true;

        lastPosition = playerTransform.position;
        velocityTransform = Vector3.zero;
        velocityFOVBoost = 0f;

        foreach (var obj in GameObject.FindGameObjectsWithTag("Respawn"))
            Destroy(obj);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void killHeal(string shooter) { BroadcastRemoteMethod(0, shooter); }

    [SynchronizableMethod]
    public void killHealSync(string shooter) {
        if (SettingsController.lifetimeKills == 0)
        {
            StartCoroutine(maskController.StartFirstKillScene());
        }
        if (avatarRef == shooter) {
            upgradeManager.killPoints++;
            killCount++;
            healthWidth = 180.0f;
            HealthController.updateHealth();
            HealthController.healAnim = true;
        }
        settingsControl.updateValues();
    }

    IEnumerator Invulnerable() {
        yield return new WaitForSeconds(0.1f);
        while (!isGround()) yield return null;
        canTakeDamage = true;
    }

    //-----Teleportation handling-----//
    private void HandleTeleportation(GameObject endPortal, DimensionInfo target) {
        characterController.enabled = false;
        SetActiveDimension(target);
        GameObject.Find("Scene Light").transform.localScale = (target.name == "Desert") ? Vector3.one * 150f : Vector3.zero;
        canTeleport = false;
        StartCoroutine(teleTrue());
        RetroDither.isTeleporting = true;
        transform.position = endPortal.transform.position + new Vector3(0f, 3f, 0f);
        characterController.enabled = true;

        currDimension = target.name;
        playerCamera.farClipPlane = target.farClip;
        if (target.skybox != null) {
            RenderSettings.skybox = target.skybox;
            playerCamera.clearFlags = CameraClearFlags.Skybox;
        } else {
            playerCamera.clearFlags = CameraClearFlags.SolidColor;
        }
        gravity = target.gravity;
        groundAcceleration = target.accel;
        groundDeceleration = target.decel;
        friction = target.fric;

        maskController.DisplayDimension();
        GetComponent<ChangeMat>().dimensionMaterialChange(target.materialName);
        Camera.main.GetComponent<SnowParticles>().toggleParticles(target.snow);
        Camera.main.GetComponent<FogShader>().ChangeDimension(target.name);
    }

    //Allow teleporting only every 2 seconds
    IEnumerator teleTrue() {
        HealthController.tpAnim = true;
        isTeleporting = true;
        yield return new WaitForSeconds(2f);
        isTeleporting = false;
        canTeleport = true;
    }
    
    //Method to get players unstuck from builds they place that intersect with their character by temporarily making the builds non-collidable
    void CheckIfStuckAndMoveUp() {
        Vector3 capsuleBottom = transform.position + characterController.center - Vector3.up * (characterController.height / 2 - characterController.radius);
        Vector3 capsuleTop = transform.position + characterController.center + Vector3.up * (characterController.height / 2 - characterController.radius);

        Collider[] hitColliders = Physics.OverlapCapsule(capsuleBottom, capsuleTop, characterController.radius, collisionMask);
        if (hitColliders.Length == 0) return;

        bool isBuildCollision = false;
        foreach (var col in hitColliders) {
            if (col.tag == "Build" || col.tag == "Ramp" || col.tag == "Wall" || col.tag == "Floor") {
                isBuildCollision = true;
                break;
            }
        }
        if (!isBuildCollision) return;

        Vector3 start = transform.position;
        Dictionary<Collider, int> originalLayers = new Dictionary<Collider, int>();
        foreach (var col in hitColliders) {
            if (col.tag == "Ramp") {
                originalLayers[col] = col.gameObject.layer;
                col.gameObject.layer = 2;
            }
        }

        RaycastHit hit;
        if (!Physics.Raycast(transform.position + new Vector3(0, 0.5f, 0), Vector3.up, out hit, 3.5f, collisionMask2)) {
            foreach (var entry in originalLayers) entry.Key.gameObject.layer = entry.Value;

            for (int i = 0; i < maxUnstuckAttempts; i++) {
                characterController.enabled = false;
                transform.position += Vector3.up * unstuckDistance;
                characterController.enabled = true;
                capsuleBottom = transform.position + characterController.center - Vector3.up * (characterController.height / 2 - characterController.radius);
                capsuleTop = transform.position + characterController.center + Vector3.up * (characterController.height / 2 - characterController.radius);
                hitColliders = Physics.OverlapCapsule(capsuleBottom, capsuleTop, characterController.radius, collisionMask);
                if (hitColliders.Length == 0) { unstuckFail = false; break; }
                else unstuckFail = true;
            }

            if (unstuckFail) {
                newVelocity.y = 0;
                foreach (Collider thing in hitColliders) thing.transform.gameObject.layer = 11;
                characterController.enabled = false;
                transform.position = start;
                characterController.enabled = true;
            }
        } else {
            foreach (var entry in originalLayers) entry.Key.gameObject.layer = entry.Value;
            foreach (Collider thing in hitColliders) thing.transform.gameObject.layer = 11;
        }
    }





















//-----Procedural Animations-----//
    IEnumerator ApplyLandingShake(float magnitude) {
        float duration = 0.2f;
        float elapsed = 0f;
        while (elapsed < duration) {
            float strength = (1f - (elapsed / duration)) * magnitude * landingCameraShake;
            landingCameraOffset = new Vector3(
                Random.Range(-strength, strength),
                Random.Range(-strength, strength),
                0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        landingCameraOffset = Vector3.zero;
    }

    IEnumerator stationaryHealing() {
        elapsedHealTime = 0f;
        while (elapsedHealTime < 3f / upgradeManager.regenSpeedMultiplier) {
            healParticles.healing = true;
            if (!(Input.GetKey(KeyCode.Q) && !CameraZoom.moving && !Shaker.shooting
                  && healthWidth < 180.0f && isGrounded && !Shooting.reloading)) {
                healParticles.healing = false;
                yield break;
            }
            elapsedHealTime += Time.deltaTime;
            yield return null;
        }
        healParticles.healing = false;
        healthWidth = Mathf.Clamp(healthWidth + 45.0f, 0.0f, 180.0f);
        HealthController.updateHealth();
        HealthController.healAnim = true;
    }

    IEnumerator lerpAimStart() {
        lerpingAim = true;
        Vector3 startAimVectorPos = aimVectorPos;
        Vector3 startAimVectorRot = aimVectorRot;
        float targetY = 0;
        float duration = 0.25f;
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            if (CameraZoom.isAiming) {
                targetY = (Shooting.shotgun) ? 0 : 0.085f;
                aimVectorPos = Vector3.Lerp(startAimVectorPos, new Vector3(targetAimXPos, targetY, targetAimZPos), elapsedTime / duration);
                aimVectorRot = Vector3.Lerp(startAimVectorRot, new Vector3(targetAimXRot, targetAimYRot, 0), elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            } else {
                lerpingAim = false;
                lerpingAimDone = true;
                lerpingAimDoneEnd = false;
                yield break;
            }
        }
        aimVectorPos = new Vector3(targetAimXPos, targetY, targetAimZPos);
        aimVectorRot = new Vector3(targetAimXRot, targetAimYRot, 0);
        lerpingAim = false;
        lerpingAimDone = true;
        lerpingAimDoneEnd = false;
    }

    IEnumerator lerpWalkStart() {
        lerpingWalk = true;
        Vector3 walkVectorPosStart = walkVectorPos;
        Vector3 walkVectorRotStart = walkVectorRot;
        float duration = 0.25f;
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            if (isSprinting && !Shaker.shooting && !Shooting.reloading && CameraZoom.moving) {
                walkVectorPos = Vector3.Lerp(walkVectorPosStart, new Vector3(targetWalkXPos, targetWalkYPos, targetWalkZPos), elapsedTime / duration);
                walkVectorRot = Vector3.Lerp(walkVectorRotStart, new Vector3(targetWalkXRot, targetWalkYRot, targetWalkZRot), elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            } else {
                lerpingWalk = false;
                lerpingWalkDone = true;
                lerpingWalkDoneEnd = false;
                yield break;
            }
        }
        walkVectorPos = new Vector3(targetWalkXPos, targetWalkYPos, targetWalkZPos);
        walkVectorRot = new Vector3(targetWalkXRot, targetWalkYRot, targetWalkZRot);
        lerpingWalk = false;
        lerpingWalkDone = true;
        lerpingWalkDoneEnd = false;
    }

    IEnumerator lerpAimEnd() {
        lerpingAimEnd = true;
        Vector3 startAimVectorPos = aimVectorPos;
        Vector3 startAimVectorRot = aimVectorRot;
        float duration = 0.25f;
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            if (!CameraZoom.isAiming) {
                aimVectorPos = Vector3.Lerp(startAimVectorPos, Vector3.zero, elapsedTime / duration);
                aimVectorRot = Vector3.Lerp(startAimVectorRot, Vector3.zero, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            } else {
                lerpingAimEnd = false;
                lerpingAimDoneEnd = true;
                yield break;
            }
        }
        aimVectorPos = Vector3.zero;
        aimVectorRot = Vector3.zero;
        lerpingAimEnd = false;
        lerpingAimDoneEnd = true;
    }

    IEnumerator lerpWalkEnd(float speed) {
        lerpingWalkEnd = true;
        Vector3 walkVectorPosStart = walkVectorPos;
        Vector3 walkVectorRotStart = walkVectorRot;
        float duration = speed;
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            if (!isSprinting || Shaker.shooting || Shooting.reloading || !CameraZoom.moving) {
                walkVectorPos = Vector3.Lerp(walkVectorPosStart, Vector3.zero, elapsedTime / duration);
                walkVectorRot = Vector3.Lerp(walkVectorRotStart, Vector3.zero, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            } else {
                walkVectorPos = Vector3.zero;
                walkVectorRot = Vector3.zero;
                lerpingWalkEnd = false;
                lerpingWalkDoneEnd = true;
                yield break;
            }
        }
        walkVectorPos = Vector3.zero;
        walkVectorRot = Vector3.zero;
        lerpingWalkEnd = false;
        lerpingWalkDoneEnd = true;
    }

    IEnumerator jumpLerp() {
        lerpingJump = true;
        if (jumpOffset > 0) {
            while (jumpOffset > 0) {
                jumpOffset = Mathf.Clamp(jumpOffset - 0.025f * Time.deltaTime, 0, 0.0075f);
                yield return null;
            }
        } else {
            while (jumpOffset < 0) {
                jumpOffset = Mathf.Clamp(jumpOffset + 0.025f * Time.deltaTime, -0.0075f, 0);
                yield return null;
            }
        }
        lerpingJump = false;
        jumpOffset = 0;
    }

    IEnumerator jumpLerpTwo() {
        lerpingJumpTwo = true;
        float time = 0;
        while (time < 0.175f) {
            jumpOffsetTwo = Mathf.Sin(Mathf.PI * time / 0.175f);
            time += Time.deltaTime;
            yield return null;
        }
        lerpingJumpTwo = false;
        jumpOffsetTwo = 0;
    }

    IEnumerator rotLerpX() {
        lerpingXRot = true;
        if (gunXRot > 0) {
            while (rotationX == 0 && gunXRot > 0) {
                gunXRot = Mathf.Clamp(gunXRot - 5f * Time.deltaTime, 0, Mathf.Infinity);
                yield return null;
            }
        } else {
            while (rotationX == 0 && gunXRot < 0) {
                gunXRot = Mathf.Clamp(gunXRot + 5f * Time.deltaTime, -Mathf.Infinity, 0);
                yield return null;
            }
        }
        lerpingXRot = false;
    }

    IEnumerator LerpDash() {
        lerpingDash = true;
        isDashing = true;
        Vector3 dashVectorRef = dashVector;
        float elapsedTime = 0f;
        float duration = 2f * (1 + (upgradeManager.dashForceMultiplier - 1) * 0.5f);
        while (elapsedTime < duration && !isGround() && dashVector.magnitude > 0) {
            if (!(_dash && dashes > 0 && !isGround())) {
                dashVector = Vector3.Lerp(dashVectorRef, Vector3.zero, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            } else {
                HealthController.noFDAnim = true;
                lerpingDash = false;
                dashes--;
                newVelocity.y = 0;
                dashVector = Vector3.Project(dashVector, playerCamera.transform.forward * dashForce)
                             + (playerCamera.transform.forward * dashForce);
                jumpedLast = true;
                dashFOV += 10;
                StartCoroutine(LerpDash());
                yield break;
            }
        }
        dashVector = Vector3.zero;
        lerpingDash = false;
        isDashing = false;
    }

    IEnumerator rotLerpY() {
        lerpingYRot = true;
        if (gunYRot > 0) {
            while (gunYRot > 0) {
                gunYRot = Mathf.Clamp(gunYRot - 5f * Time.deltaTime, 0, Mathf.Infinity);
                yield return null;
            }
        } else {
            while (gunYRot < 0) {
                gunYRot = Mathf.Clamp(gunYRot + 5f * Time.deltaTime, -Mathf.Infinity, 0);
                yield return null;
            }
        }
        lerpingYRot = false;
    }
    private void SideMovementCameraTilt()
    {
        Vector3 horizontalMovement = new Vector3(movement.x, 0, movement.z);
        float horizontalSpeed = horizontalMovement.magnitude;

        if (horizontalSpeed > 2f) {
            Vector3 flatCameraRight = new Vector3(playerCamera.transform.right.x, 0, playerCamera.transform.right.z).normalized;
            float sidewaysMovement = -Vector3.Dot(horizontalMovement.normalized, flatCameraRight);
            targetSideTilt = sidewaysMovement * velocityBasedTilt * Mathf.Clamp01(horizontalSpeed / 8f);
        } else {
            targetSideTilt = 0f;
        }

        float lerpSpeed = (horizontalSpeed > 2f) ? 5f : 8f;
        sideTilt = Mathf.Lerp(sideTilt, targetSideTilt, Time.deltaTime * lerpSpeed);
    }
    private void LateUpdate() {
        if (!_avatar.IsOwner) return;
        if (!Shooting.reloading) {
            akm.localPosition = akmBaseLocalPos;
            akm.localEulerAngles = akmBaseLocalRot;
        }
        if (!dead) {
            playerCamera.gameObject.transform.position = transform.position + new Vector3(.5f * Mathf.Sin(Camera.main.transform.eulerAngles.y * Mathf.Deg2Rad), .75f, .5f * Mathf.Cos(Camera.main.transform.eulerAngles.y * Mathf.Deg2Rad)) + landingCameraOffset;
            playerCamera.gameObject.transform.position = transform.position + new Vector3(0, .75f, 0) + landingCameraOffset;
            if (!Shooting.reloading) {
                Vector3 shootOffset = new Vector3((-Shaker.yRot - Shaker.zRot) / 500, Shaker.easedRotationChange / 125, -Shaker.easedRotationChange / 250) * shootAnimTune;
                if (IsValidVector3(shootOffset))
                    akm.localPosition += akm.parent.InverseTransformVector(shootOffset);
            }
        } else {
            playerCamera.gameObject.transform.position = new Vector3(0, -5, 0);
            akm.position = new Vector3(0, -30, 0);
        }

        SideMovementCameraTilt();

        if (!Shaker.shooting && isGrounded) {
            if (IsValidVector3(new Vector3(currentCameraRotationX - Shaker.easedRotationChange - Mathf.Abs(walkingShake.newY) * 10f,
                                           currentCameraRotationY + Shaker.yRot + 2.5f * walkingShake.newX,
                                           -3f * walkingShake.newX + -10f * gunYRot + 10 * sideTilt)))
                playerCamera.gameObject.transform.localEulerAngles = new Vector3(
                    jumpOffsetTwo * jumpAnimTune + BreathingAnim.yVal * -6 * breatheAnimTune + currentCameraRotationX - Shaker.easedRotationChange - Mathf.Abs(walkingShake.newY) * 10f * walkAnimTune,
                    currentCameraRotationY + Shaker.yRot + 2.5f * walkingShake.newX * walkAnimTune,
                    -3f * walkingShake.newX * walkAnimTune + turnAnimTune * -1.2f * gunYRot + 2.1f * sideTilt * sidewaysAnimTune);
        } else {
            if (IsValidVector3(new Vector3(currentCameraRotationX - Shaker.easedRotationChange,
                                           currentCameraRotationY + Shaker.yRot,
                                           Shaker.zRot + -1.5f * gunYRot + -10 * sideTilt)))
                playerCamera.gameObject.transform.localEulerAngles = new Vector3(
                    breatheAnimTune * BreathingAnim.yVal * -10 + currentCameraRotationX - Shaker.easedRotationChange,
                    currentCameraRotationY + Shaker.yRot * shootAnimTune,
                    shootAnimTune * Shaker.zRot + -1.2f * gunYRot * turnAnimTune + 2.1f * sideTilt * sidewaysAnimTune);
        }

        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isSprinting && !Shaker.shooting && !Shooting.reloading && !lerpingWalk && !lerpingWalkDone && CameraZoom.moving)
            StartCoroutine(lerpWalkStart());
        else if (isSprinting && (Shaker.shooting || Shooting.reloading) && !lerpingWalkEnd && !lerpingWalkDoneEnd)
            StartCoroutine(lerpWalkEnd(0.05f));
        else if (!lerpingWalkEnd && !lerpingWalkDoneEnd && (!CameraZoom.moving || !isSprinting || !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))))
            StartCoroutine(lerpWalkEnd(0.25f));

        if (!CameraZoom.moving || Shaker.shooting || CameraZoom.isAiming || !isSprinting)
            lerpingWalkDone = false;

        if (IsValidVector3(playerCamera.gameObject.transform.localPosition + playerCamera.transform.right * (walkingShake.newX / 12.5f) + playerCamera.transform.up * (Mathf.Abs(walkingShake.newY / 2.5f))))
            playerCamera.gameObject.transform.localPosition +=
                playerCamera.transform.right * ((walkingShake.newX / 12.5f) * walkAnimTune) +
                playerCamera.transform.up * (0.25f * BreathingAnim.yVal * breatheAnimTune + Mathf.Abs((walkingShake.newY / 2.5f) * walkAnimTune));

        if (!CameraZoom.isAiming && !lerpingAimEnd && !lerpingAimDoneEnd)
            StartCoroutine(lerpAimEnd());
        else if (CameraZoom.isAiming && !lerpingAim && !lerpingAimDone)
            StartCoroutine(lerpAimStart());

        if (!CameraZoom.isAiming) {
            lerpingAimDone = false;
            if (IsValidVector3(new Vector3(Mathf.Clamp((Shaker.yRot + Shaker.zRot) * 1000000000f, -0.00125f, 0.00125f) - aimVectorPos.x,
                                           Shaker.easedRotationChange / 100f - aimVectorPos.y,
                                           -Shaker.easedRotationChange / 75f - aimVectorPos.z)))
                gunThing.localPosition = new Vector3(
                    Mathf.Clamp((Shaker.yRot + Shaker.zRot) * 1000000000f, -0.00125f, 0.00125f) * shootAnimTune - aimVectorPos.x,
                    Shaker.easedRotationChange / 100f * shootAnimTune - aimVectorPos.y,
                    -Shaker.easedRotationChange / 75f * shootAnimTune - aimVectorPos.z);
        } else {
            if (IsValidVector3(new Vector3(-aimVectorPos.x, -aimVectorPos.y, -aimVectorPos.z)))
                gunThing.localPosition = new Vector3(-aimVectorPos.x, -aimVectorPos.y, -aimVectorPos.z);
        }

        gunThing.localEulerAngles = aimVectorRot;

        if (!Shaker.shooting) {
            Vector3 posOffset;
            if (!isSprinting) {
                posOffset = new Vector3(
                    walkingShake.newX * 0.015f * walkAnimTuneGun + walkVectorPos.x,
                    -0.01f * Mathf.Abs(jumpOffsetTwo) * jumpAnimTune + BreathingAnim.yVal * 0.025f * breatheAnimTune + Mathf.Abs(walkingShake.newY) * 0.03f * walkAnimTuneGun + walkVectorPos.y + jumpOffset * jumpAnimTune + Shooting.changeOffset * shootAnimTune,
                    Mathf.Abs(walkingShake.newY) * 0.03f * walkAnimTuneGun + walkVectorPos.z);
            } else {
                posOffset = new Vector3(
                    walkingShake.newX * 0.06f * walkAnimTuneGun + walkVectorPos.x,
                    -0.01f * Mathf.Abs(jumpOffsetTwo) * jumpAnimTune + BreathingAnim.yVal * 0.025f * breatheAnimTune + Mathf.Abs(walkingShake.newY) * 0.045f * walkAnimTuneGun + walkVectorPos.y + jumpOffset * jumpAnimTune + Shooting.changeOffset * shootAnimTune,
                    Mathf.Abs(walkingShake.newY) * 0.03f * walkAnimTuneGun + walkVectorPos.z);
            }
            if (IsValidVector3(posOffset))
                akm.localPosition += posOffset;

            Vector3 rotOffset = new Vector3(
                2f * Mathf.Sin(jumpOffset / 0.0075f * Mathf.PI / 2f) * jumpAnimTune + BreathingAnim.yVal * 3.5f * breatheAnimTune + Shooting.changeRotOffset * shootAnimTune - Mathf.Abs(walkingShake.newY) * -16.5f * walkAnimTuneGun + walkVectorRot.x * walkAnimTuneGun - gunXRot * turnAnimTune * 0.6f + 0.4f * Mathf.Abs(jumpOffsetTwo) * jumpAnimTune,
                -walkingShake.newX * 5.25f * walkAnimTuneGun + walkVectorRot.y * walkAnimTuneGun + gunYRot * turnAnimTune * 0.6f + Mathf.Clamp(0f, -Mathf.Infinity, 0),
                walkingShake.newX * 2.25f * walkAnimTuneGun + walkVectorRot.z * walkAnimTuneGun + sideTilt * sidewaysAnimTune * 1.5f);
            if (IsValidVector3(rotOffset))
                akm.localEulerAngles += rotOffset;
        }

        sprintingPrev = isSprinting;

        if (rotationX != 0)
            gunXRot = Mathf.Clamp(gunXRot + Mathf.Clamp(rotationX * -3.75f, -25f, 25f) * Time.deltaTime, -Mathf.Infinity, Mathf.Infinity);
        if (rotationY != 0)
            gunYRot = Mathf.Clamp(gunYRot + Mathf.Clamp(rotationY * 3.75f, -40f, 40f) * Time.deltaTime, -Mathf.Infinity, Mathf.Infinity);

        gunXRot = Mathf.Lerp(gunXRot, 0f, Time.deltaTime * 5f);
        gunYRot = Mathf.Lerp(gunYRot, 0f, Time.deltaTime * 5f);

        if (isGrounded) {
            if (jumpOffset != 0 && !lerpingJump) {
                StartCoroutine(jumpLerp());
                StartCoroutine(jumpLerpTwo());
            }
        } else {
            jumpOffset = Mathf.Clamp(jumpOffset + Mathf.Sign(characterController.velocity.y) * 0.01f * Time.deltaTime, -0.01f, 0.01f);
        }
        if (transform.position.y < -58.5f) {
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            Die();
        }
    }

    private bool IsValidVector3(Vector3 vector) {
        return !(float.IsNaN(vector.x) || float.IsNaN(vector.y) || float.IsNaN(vector.z));
    }

    private bool IsValidQuaternion(Quaternion quaternion) {
        return !(float.IsNaN(quaternion.x) || float.IsNaN(quaternion.y) || float.IsNaN(quaternion.z) || float.IsNaN(quaternion.w));
    }
}