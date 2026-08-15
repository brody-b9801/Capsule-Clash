using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using TMPro;
using NUnit.Framework;
using Unity.VisualScripting;
using Cinemachine.Utility;

[RequireComponent(typeof(MaskController))]
public class PlayerMovement : NetworkBehaviour {
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
    public bool isGrounded = false;
    public bool fastAir = false;
    public static float currentCameraRotationX = 0.0f;
    private float currentCameraRotationY = 0.0f;
    public bool isAiming = false;
    public bool isSprinting = false;
    public Vector3 newPosition;
    public Vector3 movement;
    private float _horizontal;
    private float _vertical;
    private bool _jump;
    private bool _dash;
    public static bool started = false;
    [SerializeField] private GameObject capsuleCollider;
    [SerializeField] private Transform playerTransform;
    private Vector3 lastPosition;
    private Vector3 velocityTransform;
    [SerializeField] private Transform bulletHole;
    private float newAlpha = 0.0f;
    private float newAlpha1 = 0.0f;
    private Vector3 spawn;
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
    public bool onSlope;
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
    private Vector3 remainingMovement;
    private float speedMod;
    [SerializeField] private float dashForce = 10.0f;
    private int dashes = 0;
    private bool resettingDashes = false;
    private Vector3 hitPoint;
    public bool jumpedLast = false;
    public bool isTeleporting = false;
    public Vector3 newVelocity;
    private bool groundedPrev;
    private bool groundBeneath;
    private bool sprintingPrev;
    private CharacterController characterController;
    private bool resetPrev = false;
    private bool launch = false;
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
    public Vector3 dashVector;
    public static float dashFOV = 0;
    private TextMeshProUGUI dt;
    private Coroutine dashRoutine;
    private RectTransform dashIcon;
    public bool canTakeDamage = true;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private LayerMask collisionMask2;
    public float unstuckDistance = 0.1f;
    private int maxUnstuckAttempts = 30;
    private bool isStuck = false;
    private List<Vector3> spawnVectors = new List<Vector3>();
    public Transform spawnPosContainer;
    
    private Transform mazeSpawnPosContainer;
    private Transform spaceSpawnPosContainer;
    private Transform iceSpawnPosContainer;    
    private List<Vector3> desertSpawnVectors = new List<Vector3>();
    private List<Vector3> mazeSpawnVectors = new List<Vector3>();
    private List<Vector3> spaceSpawnVectors = new List<Vector3>();
    private List<Vector3> iceSpawnVectors = new List<Vector3>();
    public int hitCount = 0;
    public GameObject respawnScreen;
    private GameObject respawnInit;
    public bool dead = false;
    private List<Collider> NoColPlayerBuilds;
    private bool unstuckFail = false;
    private float lastGroundedHeight;
    private float sideTilt;
    private float targetSideTilt;
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
    public float elapsedHealTime;

    private SettingsController settingsControl;
    private LeaderboardControl leaderboardControl;
    public GameObject usernameDisplay;
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

    private Vector3 lastFrameMovement = Vector3.zero;

    public string currDimension = "Desert";
    private float gravity = 9.81f;
    private MaskController maskController;

    private Material desertSky;
    public Material spaceSky;
    public Material iceSky;
    public Vector3 shotBoost;
    private bool wasGrounded;
    private GunThingAnim gunRenderer;

    public static PlayerMovement Local {get; private set;}
    private Shooting localShooting;
    private ObjectSpawner localSpawner;

    private float _getHorizontal() => Input.GetAxisRaw("Horizontal");
    private float _getVertical() => Input.GetAxisRaw("Vertical");
    private bool _getJump() => Input.GetKey(KeyCode.Space);
    private bool _getDash() => Input.GetKeyDown(KeyCode.Space);
    
    private void CacheInputs() {
        _horizontal = _getHorizontal();
        _vertical = _getVertical();
        _jump = _getJump();
        _dash = _getDash();
    }

    public static Vector3 getVelocity() { return Local != null ? Local.velocityTransform : Vector3.zero; }
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

    private void Awake() {
        SaveSystem.ApplyPendingKillData(this);
    }

    public override void OnStartClient() {
        base.OnStartClient();

        Application.targetFrameRate = -1;

        if (IsOwner) {
            Local = this;
            gunRenderer = GameObject.FindObjectsByType<GunThingAnim>(FindObjectsSortMode.None)[0];
            gunRenderer.enableGun();
            settingsControl = GameObject.Find("Room Menu (1)").GetComponent<SettingsController>();
            leaderboardControl = GetComponent<LeaderboardControl>();
            maskController = GetComponent<MaskController>();
            iceSpawnPosContainer = GameObject.Find("IceSpawnContainer").transform;
            spaceSpawnPosContainer = GameObject.Find("VoidSpawnContainer").transform;
            mazeSpawnPosContainer = GameObject.Find("MazeSpawnContainer").transform;
            localShooting = GetComponent<Shooting>();
            localSpawner = GetComponent<ObjectSpawner>();
            if (localShooting != null) localShooting.canShoot = true;
            Shooting.lockCursor = true;
            dt = GameObject.Find("DashText").GetComponent<TextMeshProUGUI>();
            sceneLight = GameObject.Find("DynamicLight");
            meshCollider = GetComponent<CapsuleCollider>();
            playerCamera = Camera.main;
            baseFOV = playerCamera.fieldOfView;
            currentFOV = baseFOV;
            cam2 = GameObject.Find("CameraTwo").transform;
            gunThing = SceneLookup.FindInactive("gunThing").transform;
            akm = SceneLookup.FindInactive("CamAKM").transform;
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
            
            Transform desertContainer = spawnPosContainer;
            foreach (Transform s in desertContainer) 
                desertSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            foreach (Transform s in mazeSpawnPosContainer)
                mazeSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            foreach (Transform s in spaceSpawnPosContainer)
                spaceSpawnVectors.Add(new Vector3(s.position.x, s.position.y + 5.0f, s.position.z));
            foreach (Transform s in iceSpawnPosContainer)
                    iceSpawnVectors.Add(new Vector3(s.position.x, s.position.y, s.position.z));
            canTakeDamage = false;
            HealthController.updateHealth();
            if (localShooting != null) localShooting.reloadNum = 30;
            GunThingAnim.movingState = false;
            dashes = 0;
            if (localSpawner != null) localSpawner.buildNum = 25;
            lastGroundedHeight = -13;
            currentCameraRotationX = 0;
            currentCameraRotationY = 0;
            playerCamera.transform.localEulerAngles = Vector3.zero;
            rotationSpeed = SettingsController.rs;

            username = RoomMenu.TypedUsername;

            RetroDither.isTeleporting = true;
            maskController.Initialize(playerCamera);
            maskController.BeginOpeningScene();
            Camera.main.GetComponent<FogShader>().ChangeDimension("Desert");

            desertSky = RenderSettings.skybox;
            InitializeDimensions();
            SetActiveDimension(desertInfo);
            GetComponent<MeshRenderer>().enabled = false;
            started = true;
        } else {
            foreach (Transform child in transform) {
                if (child.name == "RenderedBody") child.gameObject.SetActive(false);
            }
        }

    }

    public static bool getAvatarBool(NetworkObject avatar1) {
        return Local != null && avatar1 == Local.NetworkObject && Local.canTakeDamage;
    }

    private void FixedUpdate() {
        Vector3 currentPosition = playerTransform.position;
        velocityTransform = (currentPosition - lastPosition) / Time.deltaTime;
        lastPosition = currentPosition;
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }
    
    public bool isGround() {
        Vector3 origin = transform.position + characterController.center;
        float castDist = characterController.height * 0.5f - characterController.radius
                        + characterController.skinWidth + 0.08f;

        if (Physics.SphereCast(origin, characterController.radius - 0.01f, Vector3.down,
                            out RaycastHit hit, castDist, GroundMask, QueryTriggerInteraction.Ignore)) {

            floorNormal = hit.normal;

            if (Physics.Raycast(hit.point + Vector3.up * 0.05f, Vector3.down,
                                out RaycastHit faceHit, 0.15f, GroundMask, QueryTriggerInteraction.Ignore)
                && faceHit.collider == hit.collider) {
                floorNormal = faceHit.normal;
            }

            return Vector3.Angle(floorNormal, Vector3.up) <= characterController.slopeLimit;
        }
        floorNormal = Vector3.up;
        return false;
    }

    private bool CanJump() { return isGrounded; }

    private void Update() {
        if (!IsOwner || MaskController.maskAnimationPlaying) return;

        if (!started) return;
        CacheInputs();

        IsOnSlope();
        CheckIfStuckAndMoveUp();
        dt.text = dashes.ToString();

        usernameDisplay.transform.gameObject.GetComponent<MeshRenderer>().enabled = false;

        isGrounded = isGround();
        lastFrameMovement = movement;
        HandleCameraRotation();
        UpdateMovementVector();
        characterController.Move(movement * Time.deltaTime + GetJumpAndGravityVector() + upgradeManager.Local.dashForceMultiplier * dashVector * Time.deltaTime - shotBoost * 10 * Time.deltaTime);
        wasGrounded = isGrounded;
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

        return baseSpeed * upgradeManager.Local.speedMultiplier;
    }

private void UpdateMovementVector()
{
    Vector3 inputDirection = new Vector3(_horizontal, 0, _vertical);
    if (inputDirection.magnitude > 1f) inputDirection.Normalize();

    Vector3 forward = transform.forward;
    Vector3 right = transform.right;
    forward.y = 0f;
    right.y = 0f;
    float targetSpeed = SetTargetSpeed();
    float baseSpeed = targetSpeed / upgradeManager.Local.speedMultiplier;
    Vector3 moveDirection = forward * inputDirection.z + right * inputDirection.x;
    if (moveDirection.magnitude > 1f) moveDirection.Normalize();
    if (isGrounded) {
        moveDirection = Vector3.ProjectOnPlane(moveDirection, floorNormal);
        moveDirection = moveDirection.magnitude > 1e-6f ? moveDirection.normalized : Vector3.zero;
    }
    if (moveDirection.magnitude > characterController.minMoveDistance)
    {
        Vector3 projectedMoveDirection = Vector3.Project(movement, moveDirection);
        Vector3 perpendicularMovement = movement - projectedMoveDirection;
        float perpendicularSpeed = perpendicularMovement.magnitude;
        if (perpendicularSpeed > characterController.minMoveDistance)
        {   
            float turnFrictionScale = 3f; //Used for tuning how snappy turns feel
            float frictionDrop = perpendicularSpeed * friction * turnFrictionScale * Time.deltaTime;
            perpendicularMovement *= Mathf.Max(perpendicularSpeed - frictionDrop, 0f) / perpendicularSpeed;
        }
        movement = projectedMoveDirection + perpendicularMovement;
        projectedMoveDirection = Vector3.Project(movement, moveDirection);
        float currentSpeed = projectedMoveDirection.magnitude * Mathf.Sign(Vector3.Dot(projectedMoveDirection, moveDirection));
        float alignment = Vector3.Dot(moveDirection.normalized, movement.normalized);
        float accelRate = (alignment < 0.5f) ? groundDeceleration : groundAcceleration;
        if (isSprinting) accelRate *= sprintAccelerationMultiplier;

        float addSpeed = baseSpeed - currentSpeed;
        if (addSpeed > 0) {
            float accelSpeed = Mathf.Min(accelRate * Time.deltaTime, addSpeed);
            movement += moveDirection * accelSpeed;
        }

        float currentMag = movement.magnitude;
        if (currentMag > baseSpeed && currentMag < targetSpeed)
            movement = movement.normalized * Mathf.Lerp(currentMag, targetSpeed, Time.deltaTime * 5f);
    } else {
        float speed = movement.magnitude;
        if (speed > 0.01f) {
            float drop = speed * friction * Time.deltaTime;
            movement *= Mathf.Max(speed - drop, 0) / speed;
        } else {
            movement = Vector3.zero;
        }
    }
    float maxAllowedSpeed = targetSpeed * 1.1f;
    if (movement.magnitude > maxAllowedSpeed) movement = movement.normalized * maxAllowedSpeed;

    percentAccelerated = Mathf.Clamp01(movement.magnitude / (targetSpeed * 0.8f));    percentAccelerated = Mathf.Clamp01(new Vector3(movement.x, 0, movement.z).magnitude / (targetSpeed * 0.8f * Time.deltaTime));
}
    private Vector3 GetJumpAndGravityVector() {
        if (_jump) maskController.TryFeed();
        

        if (_jump && isGrounded && !isAiming && !maskController.LookingAtMask) {
            if (currDimension == "Maze")
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + jumpForce, 0, Mathf.Infinity);
            else if (currDimension == "Space")
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + 1.5f * jumpForce * upgradeManager.Local.jumpMultiplier, 0, Mathf.Infinity);
            else
                newVelocity.y = Mathf.Clamp(movement.y / 1.5f + jumpForce * upgradeManager.Local.jumpMultiplier, 0, Mathf.Infinity);
            jumpedLast = true;
            resetPrev = false;
            if (isSprinting) fastAir = true;
        }
        Vector3 groundingForce = Vector3.zero;
        if (isGrounded && !jumpedLast && newVelocity.y <= 0f) {
            newVelocity.y = -2;
        } else {
            groundingForce = wasGrounded && newVelocity.y <= 0 && !characterController.isGrounded && !jumpedLast ? Vector3.down * SetTargetSpeed() * Mathf.Tan(characterController.slopeLimit * Mathf.Deg2Rad) : Vector3.zero;
            newVelocity.y = Mathf.Max(newVelocity.y - gravity * Time.deltaTime, -50f);
        }

        Vector3 verticalVelo = Vector3.Angle(floorNormal, Vector3.up) > characterController.slopeLimit ? Vector3.ProjectOnPlane(newVelocity, floorNormal) : newVelocity;

        groundedPrev = isGrounded;
        return (verticalVelo + groundingForce) * Time.deltaTime;
    }

    private void HandleCameraRotation() {
        float mouseX = Input.GetAxisRaw("Mouse X");
        float mouseY = Input.GetAxisRaw("Mouse Y");
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
            characterController.stepOffset = (currDimension != "Maze" && currDimension != "Ice") ? 0.55f : 0f;
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
        CameraZoom.moving = (Mathf.Abs(_horizontal) > 0.01f || Mathf.Abs(_vertical) > 0.01f);
    }

    private void HandleDashing() {
        if (_dash && dashes > 0 && !isGrounded && currDimension != "Maze") {
            Debug.Log("dashing");
            HealthController.noFDAnim = true;
            dashes--;
            dashVector = Vector3.Project(dashVector, playerCamera.transform.forward * dashForce)
                         + (playerCamera.transform.forward * dashForce);
            newVelocity.y = 0;
            jumpedLast = true;
            dashFOV = Mathf.Clamp(dashFOV, 20, dashFOV + 10);
            lastGroundedHeight = -30;
            if (dashRoutine != null) StopCoroutine(dashRoutine);
            dashRoutine = StartCoroutine(LerpDash());
        } else {    
            dashFOV = 0;
        }

        if (!resettingDashes) StartCoroutine(addDash());
    }

    private void HandleLaunch()
    {
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

        if (Input.GetKey(KeyCode.R) && dead) Respawn();

        if (Input.GetKey(KeyCode.Q) && !Shooting.Local.reloading && !CameraZoom.moving && !Shaker.shooting
            && isGrounded && !healParticles.healing && DamageControl.Local.health.Value < 180.0f)
            StartCoroutine(stationaryHealing());

        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            && isGrounded && StaminaController.Local.canSprint && !isAiming) {
            isSprinting = true;
        } else {
            if (!fastAir) isSprinting = false;
        }

        if ((Input.GetKeyUp(KeyCode.LeftShift) || Input.GetKeyUp(KeyCode.RightShift)) && !isAiming) {
            isSprinting = false;
            fastAir = false;
        }
        
        if (Input.GetKeyUp(KeyCode.Escape)) {
            Cursor.lockState = CursorLockMode.None;
            Shooting.lockCursor = false;
        }
    }

    private void ManageSprintState() {
        if (!StaminaController.Local.canSprint) fastAir = false;

        if (sprintingPrev && groundedPrev && !isGrounded) {
            fastAir = true;
            isSprinting = true;
        }
    }

    private void HandleShotBoost() {
        if (currDimension != "Ice" || !isGrounded) {
            shotBoost = Vector3.zero;
            return;
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
        if ((Input.GetMouseButton(1) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && isGrounded) {
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
            float total = 10 * (1.0f / upgradeManager.Local.dashRegenMultiplier);
            if (currDimension == "Space")
            {
                total *= 0.25f;
            }
            while (elapsedTime < total) {
                elapsedTime += Time.deltaTime;
                float percent = elapsedTime / total;
                dashIcon.sizeDelta = new Vector2(75, percent * 72);
                float totalPrev = total;
                total = 10 * (1.0f / upgradeManager.Local.dashRegenMultiplier);
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
        if (!IsOwner) return;
        
        string hitTag = hit.transform.gameObject.tag;
        if (isGrounded) {
            HandleFallDamage(hitTag);
            jumpedLast = false;
            fastAir = false;
            resetPrev = false;
        }
        VelocityResetCheck(hit.normal);
        if (hit.gameObject.CompareTag("Launchpad")) launch = true;
        TeleportationCheck(hit.gameObject);

    }

    private void TeleportationCheck(GameObject hitObject) {        
        if (canTeleport) {
            if (hitObject == portal1A) HandleTeleportation(portal1B, desertInfo);
            else if (hitObject == portal1B) HandleTeleportation(portal1A, mazeInfo);
            else if (hitObject == portal2A) HandleTeleportation(portal2B, desertInfo);
            else if (hitObject == portal2B) HandleTeleportation(portal2A, spaceInfo);
            else if (hitObject == portal3A) HandleTeleportation(portal3B, desertInfo);
            else if (hitObject == portal3B) HandleTeleportation(portal3A, iceInfo);
            else if (hitObject == portal4 && MaskController.Local.keyCount == 3) {
            }
        }
    }
    
    private void VelocityResetCheck(Vector3 hitNormal) {
        if (Vector3.Angle(dashVector, hitNormal) > 90f) { //check to reset dash if hit wall
            dashVector = Vector3.zero;
        }
        if  (Vector3.Angle(hitNormal, Vector3.down) < characterController.slopeLimit) { //check to reset vertical velocity if hit ceiling
            newVelocity.y = 0f;
        }

    }

    private void HandleFallDamage(string hitTag)
    {
        if (!groundedPrev && hitTag != "Launchpad" && hitTag != "Portal") {
                float heightChange = lastGroundedHeight - transform.position.y - 8;
                if (heightChange > 0) {
                    float shakeMagnitude = Mathf.Min(heightChange / 20f, 1f);
                    StartCoroutine(ApplyLandingShake(shakeMagnitude));
                    DamageControl.Local.health.Value -= ((int)(heightChange / 4)) * 12;
                }
                if (DamageControl.Local.health.Value <= 0) Die();
                HealthController.updateHealth();
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

    public void Die() {             
        characterController.enabled = false;
        dead = true;
        if (IsOwner) respawnInit = Instantiate(respawnScreen);
        Cursor.lockState = CursorLockMode.None;
        Shooting.lockCursor = false;
        transform.position = new Vector3(0, -30, 0);
        RenderSettings.skybox = desertSky;
    }
    public void Respawn() {
        DamageControl.Local.health.Value = 180;
        canTakeDamage = false;
        HealthController.updateHealth();
        // Own siblings, not the statics — these resets apply to this player.
        if (localShooting != null) localShooting.reloadNum = 30;
        GunThingAnim.movingState = false;
        dashes = 0;
        if (localSpawner != null) localSpawner.buildNum = 25;
        transform.localEulerAngles = Vector3.zero;
        newVelocity = Vector3.zero;
        characterController.enabled = false;
        dead = false;
        lastGroundedHeight = -13;
        movement = Vector3.zero;
            
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
        if (localShooting != null) localShooting.canShoot = true;

        lastPosition = playerTransform.position;
        velocityTransform = Vector3.zero;
        velocityFOVBoost = 0f;

        foreach (var obj in GameObject.FindGameObjectsWithTag("Respawn"))
            Destroy(obj);
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void killHeal(NetworkObject shooter) { ServerKillHeal(shooter); }

    [ServerRpc(RequireOwnership = false)]
    private void ServerKillHeal(NetworkObject shooter) => killHealSync(shooter);

    [ObserversRpc]
    public void killHealSync(NetworkObject shooter) {
        if (SettingsController.lifetimeKills == 0)
        {
            StartCoroutine(maskController.StartFirstKillScene());
        }
        if (Local != null && Local.NetworkObject == shooter) {
            upgradeManager.Local.killPoints++;
            killCount++;
            DamageControl.Local.health.Value = 180;
            HealthController.updateHealth();
            HealthController.healAnim = true;
            SaveSystem.SavePlayerData();
        }
    }

    IEnumerator Invulnerable() {
        yield return new WaitForSeconds(0.1f);
        while (!isGround()) yield return null;
        canTakeDamage = true;
    }

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

    IEnumerator teleTrue() {
        HealthController.tpAnim = true;
        isTeleporting = true;
        yield return new WaitForSeconds(2f);
        isTeleporting = false;
        canTeleport = true;
    }
    
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
        while (elapsedHealTime < 3f / upgradeManager.Local.regenSpeedMultiplier) {
            healParticles.healing = true;
            if (!(Input.GetKey(KeyCode.Q) && !CameraZoom.moving && !Shaker.shooting
                  && DamageControl.Local.health.Value < 180.0f && isGrounded && !Shooting.Local.reloading)) {
                healParticles.healing = false;
                yield break;
            }
            elapsedHealTime += Time.deltaTime;
            yield return null;
        }
        healParticles.healing = false;
        DamageControl.Local.health.Value = Mathf.Clamp(DamageControl.Local.health.Value + 45.0f, 0.0f, 180.0f);
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
                targetY = (Shooting.Local.shotgun) ? 0 : 0.085f;
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
            if (isSprinting && !Shaker.shooting && !Shooting.Local.reloading && CameraZoom.moving) {
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
            if (!isSprinting || Shaker.shooting || Shooting.Local.reloading || !CameraZoom.moving) {
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
        Vector3 dashVectorRef = dashVector;
        float elapsedTime = 0f;
        float duration = 2f * (1 + (upgradeManager.Local.dashForceMultiplier - 1) * 0.5f);
        while (elapsedTime < duration && !isGround() && dashVector.magnitude > 0) {
            dashVector = Vector3.Lerp(dashVectorRef, Vector3.zero, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        dashVector = Vector3.zero;
        dashRoutine = null;
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
        if (!IsOwner || MaskController.maskAnimationPlaying) return;
        if (!Shooting.Local.reloading) {
            akm.localPosition = akmBaseLocalPos;
            akm.localEulerAngles = akmBaseLocalRot;
        }
        if (!dead && !MaskController.maskAnimationPlaying) {
            //playerCamera.gameObject.transform.position = transform.position + new Vector3(.5f * Mathf.Sin(Camera.main.transform.eulerAngles.y * Mathf.Deg2Rad), .75f, .5f * Mathf.Cos(Camera.main.transform.eulerAngles.y * Mathf.Deg2Rad)) + landingCameraOffset;
            playerCamera.gameObject.transform.position = transform.position + new Vector3(0, .75f, 0) + landingCameraOffset + 0.4f * transform.forward;
            if (!Shooting.Local.reloading) {
                Vector3 shootOffset = new Vector3((-Shaker.yRot - Shaker.zRot) / 500, Shaker.easedRotationChange / 125, -Shaker.easedRotationChange / 250) * shootAnimTune;
                akm.localPosition += akm.parent.InverseTransformVector(shootOffset);
            }
        } else if (dead) {
            playerCamera.gameObject.transform.position = new Vector3(0, -5, 0);
            akm.position = new Vector3(0, -30, 0);
        }

        SideMovementCameraTilt();

        if (!Shaker.shooting && isGrounded) {
                playerCamera.gameObject.transform.localEulerAngles = new Vector3(
                    jumpOffsetTwo * jumpAnimTune + BreathingAnim.yVal * -6 * breatheAnimTune + currentCameraRotationX - Shaker.easedRotationChange - Mathf.Abs(walkingShake.newY) * 10f * walkAnimTune,
                    currentCameraRotationY + Shaker.yRot + 2.5f * walkingShake.newX * walkAnimTune,
                    turnAnimTune * -1.2f * gunYRot + 2.1f * sideTilt * sidewaysAnimTune);
        } else {
                playerCamera.gameObject.transform.localEulerAngles = new Vector3(
                    breatheAnimTune * BreathingAnim.yVal * -10 + currentCameraRotationX - Shaker.easedRotationChange,
                    currentCameraRotationY + Shaker.yRot * shootAnimTune,
                    shootAnimTune * Shaker.zRot + -1.2f * gunYRot * turnAnimTune + 2.1f * sideTilt * sidewaysAnimTune);
        }

        if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && isSprinting && !Shaker.shooting && !Shooting.Local.reloading && !lerpingWalk && !lerpingWalkDone && CameraZoom.moving)
            StartCoroutine(lerpWalkStart());
        else if (isSprinting && (Shaker.shooting || Shooting.Local.reloading) && !lerpingWalkEnd && !lerpingWalkDoneEnd)
            StartCoroutine(lerpWalkEnd(0.05f));
        else if (!lerpingWalkEnd && !lerpingWalkDoneEnd && (!CameraZoom.moving || !isSprinting || !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))))
            StartCoroutine(lerpWalkEnd(0.25f));

        if (!CameraZoom.moving || Shaker.shooting || CameraZoom.isAiming || !isSprinting)
            lerpingWalkDone = false;

            playerCamera.gameObject.transform.localPosition +=
                playerCamera.transform.right * ((walkingShake.newX / -10f) * walkAnimTune) +
                playerCamera.transform.up * (0.25f * BreathingAnim.yVal * breatheAnimTune + Mathf.Abs((walkingShake.newY / 2.5f) * walkAnimTune));

        if (!CameraZoom.isAiming && !lerpingAimEnd && !lerpingAimDoneEnd)
            StartCoroutine(lerpAimEnd());
        else if (CameraZoom.isAiming && !lerpingAim && !lerpingAimDone)
            StartCoroutine(lerpAimStart());

        if (!CameraZoom.isAiming) {
            lerpingAimDone = false;

                gunThing.localPosition = new Vector3(
                    Mathf.Clamp((Shaker.yRot + Shaker.zRot) * 1000000000f, -0.00125f, 0.00125f) * shootAnimTune - aimVectorPos.x,
                    Shaker.easedRotationChange / 100f * shootAnimTune - aimVectorPos.y,
                    -Shaker.easedRotationChange / 75f * shootAnimTune - aimVectorPos.z);
        } else {
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
                akm.localPosition += posOffset;

            Vector3 rotOffset = new Vector3(
                2f * Mathf.Sin(jumpOffset / 0.0075f * Mathf.PI / 2f) * jumpAnimTune + BreathingAnim.yVal * 3.5f * breatheAnimTune + Shooting.changeRotOffset * shootAnimTune - Mathf.Abs(walkingShake.newY) * -16.5f * walkAnimTuneGun + walkVectorRot.x * walkAnimTuneGun - gunXRot * turnAnimTune * 0.6f + 0.4f * Mathf.Abs(jumpOffsetTwo) * jumpAnimTune,
                -walkingShake.newX * 5.25f * walkAnimTuneGun + walkVectorRot.y * walkAnimTuneGun + gunYRot * turnAnimTune * 0.6f + Mathf.Clamp(0f, -Mathf.Infinity, 0),
                walkingShake.newX * 2.25f * walkAnimTuneGun + walkVectorRot.z * walkAnimTuneGun + sideTilt * sidewaysAnimTune * 1.5f);
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