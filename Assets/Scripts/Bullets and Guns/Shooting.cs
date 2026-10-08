using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using NUnit.Framework;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using NUnit.Framework.Internal;
using Unity.VisualScripting;

public class ObjectPool<T> where T : Component
{
    private readonly T _prefab;
    private readonly Transform _parent;
    private readonly Queue<T> _pool = new Queue<T>();

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        _prefab  = prefab;
        _parent  = parent;

        for (int i = 0; i < initialSize; i++)
            _pool.Enqueue(CreateInstance());
    }

    private T CreateInstance()
    {
        T instance = Object.Instantiate(_prefab, _parent);
        instance.gameObject.SetActive(false);
        return instance;
    }

    public T Get(Vector3 position, Quaternion rotation, Transform newParent = null)
    {
        T instance = null;
        while (instance == null && _pool.Count > 0) instance = _pool.Dequeue();
        if (instance == null) instance = CreateInstance();

        Transform t = instance.transform;
        if (newParent != null) t.SetParent(newParent, false);
        t.position = position;
        t.rotation = rotation;
        instance.gameObject.SetActive(true);
        return instance;
    }

    public void Return(T instance, Transform defaultParent = null)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
        if (defaultParent != null) instance.transform.SetParent(defaultParent, false);
        _pool.Enqueue(instance);
    }
}
public class Shooting : NetworkBehaviour
{
    [SerializeField] private GameObject     bulletPrefab;
    [SerializeField] private Transform      gun;
    [SerializeField] private GameObject     player;
    [SerializeField] private Material       normal;
    [SerializeField] private Material       bulletTrailMaterial;
    [SerializeField] private LayerMask      ignoreLayers;
    [SerializeField] private GameObject     muzzleFlash;
    [SerializeField] private Transform      gunRotation;
    [SerializeField] public  Transform      gunThing;
    [SerializeField] private GameObject     bH;
    [SerializeField] private ParticleSystem muzzlePrefab;
    [SerializeField] private GameObject     bulletCasingPrefab;
    [SerializeField] private MeshFilter     playerMesh;
    [SerializeField] private Mesh           shotgunMesh;
    [SerializeField] private Mesh           M4Mesh;
    [SerializeField] private Mesh           sniperMesh;
    [SerializeField] private Mesh           sniperMagMesh;
    [SerializeField] private Vector3        sniperMagPosition = new Vector3(0f, -0.16f, 0.1f);

    private const int MuzzlePoolSize  = 10;
    private const int CasingPoolSize  = 30;


    private ObjectPool<ParticleSystem> _muzzlePool;
    private ObjectPool<Transform>      _casingPool;

    private Transform _muzzlePoolRoot;
    private Transform _casingPoolRoot;
    public float bulletSpeed  = 15.0f;
    public float snipeSpeed   = 150.0f;
    public float fireRate;
    public float nextFireTime = 0.0f;

    public static Shooting Local { get; private set; }

    public int  reloadNum  = 30;
    public int shottieNum = 2;
    public int sniperNum  = 5;
    public bool reloading  = false;

    public bool playerShot;
    public bool isShooting;
    public bool canShoot   = true;
    public static bool lockCursor = false;
    public enum currGun
    {
        AR,
        Shotgun,
        Sniper
    }
    public currGun currentGun;
    public static float distance;
    public static Vector3 deltaPosition;
    public static float spread;
    public static Vector3 lastShotDirection = Vector3.zero;

    public Vector3 end;
    public static float changeOffset;
    public static float changeRotOffset;
    public static bool  playerJoin;


    private Transform bulletHole;
    private Transform casingSpawn;
    private Material  muzzleFlashCamera;
    private Transform gunThing_g1;

    private Material  muzzleFlashCameraMat;
    private float     alphaVal;
    private bool      isFiringBullet = false;
    private bool      clickStartedOverUI = false;
    private bool      canChangeGun   = true;
    private bool      changingGun    = false;

    private Camera    cam2;
    private Camera    mainCamera;
    private Transform mainCameraTransform;

    private Vector3   previousPosition;
    private Vector3   posSave;

    private MeshFilter gunMesh;
    private GameObject mag;
    private GameObject camCasing;
    private GameObject CamAKM;
    private GameObject bulletSpawn;
    public  GameObject playerMag;

    private MeshFilter magFilter;
    private MeshFilter playerMagFilter;
    private Mesh       defaultMagMesh;
    private Mesh       defaultPlayerMagMesh;
    private Vector3    defaultMagPosition;

    private BulletManager bulletManager;


    public float trailFadeDuration = 0.5f;

    void Awake()
    {
        bulletManager = FindFirstObjectByType<BulletManager>();

        playerMagFilter = playerMag.GetComponent<MeshFilter>();
        defaultPlayerMagMesh = playerMagFilter.sharedMesh;
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        if (_muzzlePoolRoot != null) Destroy(_muzzlePoolRoot.gameObject);
        if (_casingPoolRoot != null) Destroy(_casingPoolRoot.gameObject);
        base.OnStopClient();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;
        Local = this;
        currentGun = currGun.AR;

        alphaVal = 0;

        mainCamera          = Camera.main;
        mainCameraTransform = mainCamera.transform;

        muzzleFlashCamera = GameObject.Find("CamQuad").GetComponent<MeshRenderer>().material;
        muzzleFlashCamera.color = new Color(
            muzzleFlashCamera.color.r,
            muzzleFlashCamera.color.g,
            muzzleFlashCamera.color.b, alphaVal);

        bulletHole  = SceneLookup.FindInactive("MCBH").transform;
        bulletSpawn = GameObject.Find("bulletSpawn");
        casingSpawn = GameObject.Find("casingSpawn").transform;
        gunMesh     = SceneLookup.FindInactive("CamAKM").GetComponent<MeshFilter>();
        CamAKM      = SceneLookup.FindInactive("CamAKM");
        mag         = SceneLookup.FindInactive("MC.Magazine");
        magFilter      = mag.GetComponent<MeshFilter>();
        defaultMagMesh = magFilter.sharedMesh;
        defaultMagPosition = mag.transform.localPosition;
        camCasing   = SceneLookup.FindInactive("CamCasing");
        gunThing_g1 = SceneLookup.FindInactive("CamAKM").transform;

        previousPosition = transform.position;
        lockCursor       = false;

        cam2     = GameObject.Find("CameraTwo").GetComponent<Camera>();

        camCasing.GetComponent<MeshRenderer>().enabled = false;

        _muzzlePoolRoot = CreatePoolRoot("Pool_Muzzle");
        _casingPoolRoot = CreatePoolRoot("Pool_Casings");

        _muzzlePool = new ObjectPool<ParticleSystem>(
            muzzlePrefab,
            MuzzlePoolSize, _muzzlePoolRoot);

        _casingPool = new ObjectPool<Transform>(
            bulletCasingPrefab.GetComponent<Transform>(),
            CasingPoolSize, _casingPoolRoot);
        if (!IsOwner) GetComponent<Shooting>().enabled = false;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    private Transform CreatePoolRoot(string name)
    {
        var go = new GameObject(name);
        PersistentSceneObject.Keep(go, name);
        return go.transform;
    }

    void Update()
    {
        isShooting = false;

        muzzleFlashCamera.color = new Color(muzzleFlashCamera.color.r, muzzleFlashCamera.color.g, muzzleFlashCamera.color.b, alphaVal);

        Vector3 currentPosition = transform.position;
        deltaPosition    = currentPosition - previousPosition;
        previousPosition = currentPosition;

        Vector3 cameraPosition = mainCameraTransform.position;
        Vector3 cameraForward  = mainCameraTransform.forward;

        ref int ammo = ref CurrentAmmo();

            if (Input.GetMouseButtonDown(0))
                clickStartedOverUI = Cursor.lockState == CursorLockMode.None &&
                                     EventSystem.current != null &&
                                     EventSystem.current.IsPointerOverGameObject();
            else if (Input.GetMouseButtonUp(0))
                clickStartedOverUI = false;

            bool inputCheck = currentGun == currGun.AR ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
            if (inputCheck && Time.time >= nextFireTime &&
                ammo > 0 && !reloading && canShoot && !PlayerMovement.Local.dead && !clickStartedOverUI)
            {
                Vector3 useCameraPos = IsValidVector3(cameraPosition) ? cameraPosition : posSave;
                posSave = useCameraPos;

                isShooting = true;
                

                float spreadMulti = currentGun switch
                {
                    currGun.Shotgun => 10f,
                    currGun.Sniper  => CameraZoom.isAiming ? 0f : 3f,
                    _               => 1f,
                };
                float range = currentGun == currGun.Sniper ? 150f : 25f;
                for (int i = 0; i < (currentGun == currGun.Shotgun ? 9 : 1); i++)
                {
                    FireBullet(useCameraPos, cameraForward,
                        bulletSpawn.transform.position, range, 15f,
                        bulletHole.position, bH.transform.position,
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        i == 0);
                }

                Shaker.shooting = true;
                Shaker.StopShake();
                Shaker.Instance.Shake();
                if (currentGun != currGun.AR) RetroDither.shotgunFired = true;
                else                          RetroDither.shotFired    = true;
                ReloadAnimation.PlayAnim();
                StartCoroutine(EnableDisable());
                nextFireTime = Time.time + 1f / fireRate * upgradeManager.Local.fireRateMultiplier;
                ammo--;
            }
            else if (!(inputCheck && ammo > 0 && !reloading))
            {
                Shaker.shooting = false;
            }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (canChangeGun && !reloading && CurrentAmmo() != MaxAmmo(currentGun))
            {
                ReloadAnimation.PlayReload();
                StartCoroutine(waitReload());
                ReloadIndicator.Reload();
            }
        }

        bool upgradeWindowOpen = upgradeManager.Local != null && upgradeManager.Local.UpgradeWindowOpen;
        if (!upgradeWindowOpen && !reloading && canChangeGun && !isShooting)
        {
            currGun? selectedGun = null;
            if      (Input.GetKeyDown(KeyCode.Alpha1)) selectedGun = currGun.AR;
            else if (Input.GetKeyDown(KeyCode.Alpha2)) selectedGun = currGun.Shotgun;
            else if (Input.GetKeyDown(KeyCode.Alpha3)) selectedGun = currGun.Sniper;

            if (selectedGun.HasValue && selectedGun.Value != currentGun)
                StartCoroutine(gunChangeAnim(selectedGun.Value));
        }
    }

    private ref int CurrentAmmo()
    {
        switch (currentGun)
        {
            case currGun.Shotgun: return ref shottieNum;
            case currGun.Sniper:  return ref sniperNum;
            default:              return ref reloadNum;
        }
    }

    public static int MaxAmmo(currGun gun) => gun switch
    {
        currGun.Shotgun => 2,
        currGun.Sniper  => 5,
        _               => 30,
    };

    private void FireBullet(
        Vector3 origin, Vector3 direction, Vector3 bS,
        float force, float damage,
        Vector3 bulletOrigin, Vector3 bHPos,
        float randomX, float randomY, float randomZ,
        bool doMuzzleFlash)
    {
        bool isOwner = IsOwner;

        Vector3 shooterVelocity = PlayerMovement.Local.dashVector;
        if (IsValidVector3(PlayerMovement.Local.newVelocity))
        {
            shooterVelocity += PlayerMovement.Local.isGrounded
                ? new Vector3(PlayerMovement.Local.newVelocity.x, 0f, PlayerMovement.Local.newVelocity.z)
                : PlayerMovement.Local.newVelocity;
        }

        ServerBulletLogic(
            spawnPosition: bS, direction: direction, force: force, origin: origin,
            randomX: randomX, randomY: randomY, randomZ: randomZ,
            camPosition: mainCameraTransform.position,
            camForward: mainCameraTransform.forward,
            gunType: currentGun,
            shooterVelocity: shooterVelocity);

        lockCursor = true;

        Vector3 spawnMuzzlePosition = IsOwner ? bulletOrigin : bHPos;
        Vector3 spawnPosition       = bS;

        if (doMuzzleFlash)
        {
            float randomAngle = Random.Range(-45f, 45f);
            Transform bulletHoleRef = IsOwner ? bulletHole : bH.transform;

                if (IsValidQuaternion(bulletHoleRef.rotation))
                {
                    Quaternion muzzleRot = bulletHoleRef.rotation *
                                          Quaternion.Euler(randomAngle, -90f, 0f);

                    ParticleSystem muzzleInst = _muzzlePool.Get(
                        spawnMuzzlePosition, muzzleRot, bulletHoleRef);
                    muzzleInst.transform.localPosition = Vector3.zero;
                    muzzleInst.Play();

                    StartCoroutine(ReturnParticleAfterPlay(muzzleInst, bulletHoleRef));

                    if (IsOwner) {
                        Transform casing = _casingPool.Get(
                            casingSpawn.position, bulletCasingPrefab.transform.rotation, casingSpawn);
                        casing.gameObject.layer = 5;

                        BulletCasingAnim casingAnim = casing.GetComponent<BulletCasingAnim>();
                        if (casingAnim != null)
                            casingAnim.OnReturnToPool = () => _casingPool.Return(casing, _casingPoolRoot);
                    }
                    Transform casingHolder = transform.GetChild(2).GetChild(0).GetChild(1).GetChild(0);
                    Transform nonCameraCasing = _casingPool.Get(
                        casingHolder.position, bulletCasingPrefab.transform.rotation, casingHolder);
                    nonCameraCasing.gameObject.layer = 11;
                    if (IsOwner) nonCameraCasing.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                    BulletCasingAnim casingAnimNonCam = nonCameraCasing.GetComponent<BulletCasingAnim>();
                    if (casingAnimNonCam != null)
                        casingAnimNonCam.OnReturnToPool = () => _casingPool.Return(nonCameraCasing, _casingPoolRoot);
                }

        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerBulletLogic(
        Vector3 spawnPosition, Vector3 direction, float force, Vector3 origin,
        float randomX, float randomY, float randomZ,
        Vector3 camPosition, Vector3 camForward, currGun gunType, Vector3 shooterVelocity,
        NetworkConnection conn = null)
    {
        NetworkObject shooterObj = conn != null ? conn.FirstObject : NetworkObject;

        Vector3 targetPoint;
        Vector3 bulletPosition = spawnPosition;

        if (BulletManager.RaycastSkippingShooter(camPosition, camForward, 1.5f, ~ignoreLayers, shooterObj, out _))
        {
            targetPoint    = transform.position + direction * force;
            origin         = camPosition;
            bulletPosition = origin;
        }
        else if (BulletManager.RaycastSkippingShooter(origin, direction, force, ~ignoreLayers, shooterObj, out RaycastHit hit))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = origin + direction * force;
        }

        Vector3 spreadVector       = new Vector3(randomX, randomY, randomZ);
        float   distanceFromCamera = Vector3.Distance(origin, targetPoint);
        targetPoint += spreadVector * distanceFromCamera;

        Vector3 fireDirection = (targetPoint - origin).normalized;
        float   speed         = gunType == currGun.Sniper ? snipeSpeed : bulletSpeed;
        Vector3 velocity      = fireDirection * speed + shooterVelocity;

        GameObject    bulletGO  = Instantiate(bulletPrefab, bulletPosition, Quaternion.LookRotation(fireDirection));
        Rigidbody     bulletRb  = bulletGO.GetComponent<Rigidbody>();
        NetworkObject bulletNob = bulletGO.GetComponent<NetworkObject>();

        if (bulletRb != null) bulletRb.linearVelocity = velocity;

        ServerManager.Spawn(bulletNob, shooterObj != null ? shooterObj.Owner : conn);

        if (bulletManager == null) bulletManager = FindFirstObjectByType<BulletManager>();
        if (bulletManager != null)
        {
            bulletManager.AddBulletData(bulletNob, origin, gunType, shooterObj);
            Debug.Log("Bullet added to BulletManager");
        } else
        {
            Debug.Log("BulletManager is null");
        }

        end            = targetPoint;
        isFiringBullet = true;
    }

    private IEnumerator ReturnParticleAfterPlay(ParticleSystem ps, Transform defaultParent)
    {
        yield return new WaitWhile(() => ps != null && ps.IsAlive(true));
        if (ps != null)
            _muzzlePool.Return(ps, _muzzlePoolRoot);
    }

    public IEnumerator gunChangeAnim(currGun newGun)
    {
        isShooting      = false;
        Shaker.shooting = false;
        float time      = 0f;
        float totalTime = 1f;
        canChangeGun    = false;
        canShoot        = false;

        while (time < totalTime / 2f)
        {
            changeOffset    = Mathf.Cos((time / totalTime) * 2f * Mathf.PI) * 0.5f - 0.5f;
            changeRotOffset = Mathf.Sin((time / totalTime) * Mathf.PI) * 90f;
            time += Time.deltaTime;
            yield return null;
        }

        currentGun = newGun;
        if (PlayerMovement.Local != null) PlayerMovement.Local.ReplayAimLerp();

        float muzzleZ;
        switch (currentGun)
        {
            case currGun.Shotgun:
                fireRate = 3.5f;
                CamAKM.transform.localScale = new Vector3(1.075f, 1.075f, 1.075f);
                gunMesh.mesh = shotgunMesh;
                mag.GetComponent<MeshRenderer>().enabled    = false;
                camCasing.GetComponent<MeshRenderer>().enabled = true;
                muzzleZ = 0.36f;
                break;
            case currGun.Sniper:
                fireRate = 1.25f;
                CamAKM.transform.localScale = new Vector3(1f, 1f, 1f);
                gunMesh.mesh = sniperMesh != null ? sniperMesh : M4Mesh;
                mag.GetComponent<MeshRenderer>().enabled    = true;
                camCasing.GetComponent<MeshRenderer>().enabled = false;
                muzzleZ = 0.8f;
                break;
            default:
                fireRate = 10f;
                CamAKM.transform.localScale = new Vector3(1.25f, 1f, 1f);
                gunMesh.mesh = M4Mesh;
                mag.GetComponent<MeshRenderer>().enabled    = true;
                camCasing.GetComponent<MeshRenderer>().enabled = false;
                muzzleZ = 0.6f;
                break;
        }
        magFilter.sharedMesh = currentGun == currGun.Sniper && sniperMagMesh != null ? sniperMagMesh : defaultMagMesh;
        mag.transform.localPosition = currentGun == currGun.Sniper ? sniperMagPosition : defaultMagPosition;
        bulletHole.transform.localPosition = new Vector3(bulletHole.transform.localPosition.x, bulletHole.transform.localPosition.y, muzzleZ);
        bH.transform.localPosition         = new Vector3(bH.transform.localPosition.x, bH.transform.localPosition.y, muzzleZ);

        ServerGunSkin(currentGun,
            gunThing_g1.transform.position - new Vector3(0f, 0.35f, 0f),
            gunThing_g1.transform.rotation, false);

        while (time < totalTime)
        {
            changeOffset    = Mathf.Cos((time / totalTime) * 2f * Mathf.PI) * 0.5f - 0.5f;
            changeRotOffset = Mathf.Sin((time / totalTime) * Mathf.PI) * 90f;
            time += Time.deltaTime;
            yield return null;
        }

        canShoot        = true;
        canChangeGun    = true;
        changeOffset    = 0f;
        changeRotOffset = 0f;
    }

    IEnumerator EnableDisable()
    {
        CameraZoom.shot          = true;
        float startingVal        = alphaVal;
        HealthController.healAnim = false;
        float total  = 0.05f;
        float elapsed = 0f;

        while (elapsed < total)
        {
            if (HealthController.healAnim)
            {
                startingVal              = alphaVal;
                HealthController.healAnim = false;
                elapsed                  = 0f;
            }

            alphaVal = Mathf.Sin(elapsed / total * Mathf.PI) * 0.2f;
            elapsed += Time.deltaTime;
            yield return null;
        }

        alphaVal = 0f;
    }

    public const float ReloadDuration = 2.01f;
    public const float ShotgunSingleShellReloadDuration = 1.31f;
    public const float SniperReloadDuration = 2.01f;

    public static float CurrentReloadDuration =>
        (Local.currentGun == currGun.Sniper ? SniperReloadDuration
            : (Local.currentGun == currGun.Shotgun && Local.shottieNum == 1) ? ShotgunSingleShellReloadDuration
            : ReloadDuration)
        / upgradeManager.Local.reloadSpeedMultiplier;

    IEnumerator waitReload()
    {
        reloading = true;
        yield return new WaitForSeconds(CurrentReloadDuration);

        CurrentAmmo() = MaxAmmo(currentGun);

        reloading = false;
    }

    private void LateUpdate()
    {
        if (!IsOwner) return;
        if (currentGun == currGun.Sniper && !reloading && mag != null)
            mag.transform.localPosition = sniperMagPosition;
        if (lockCursor && !clickStartedOverUI)
            Cursor.lockState = CursorLockMode.Locked;
        else
            lockCursor = false;
    }

    [ServerRpc]
    private void ServerGunSkin(currGun gun, Vector3 pos, Quaternion rot, bool networkedCall)
        => RpcGunSkin(gun, pos, rot, networkedCall);

    [ObserversRpc(BufferLast = true)]
    private void RpcGunSkin(currGun gun, Vector3 pos, Quaternion rot, bool networkedCall)
        => gunSkinSync(gun, pos, rot, networkedCall);

    public void gunSkinSync(currGun gun, Vector3 pos, Quaternion rot, bool networkedCall)
    {
        if (!networkedCall)
        {
            switch (gun)
            {
                case currGun.Shotgun:
                    playerMesh.mesh                         = shotgunMesh;
                    playerMesh.transform.localScale         = new Vector3(1.2f, 1.2f, 1.2f);
                    playerMag.SetActive(false);
                    break;
                case currGun.Sniper:
                    playerMesh.mesh                         = sniperMesh != null ? sniperMesh : M4Mesh;
                    playerMesh.transform.localScale         = new Vector3(1f, 1f, 1f);
                    playerMag.SetActive(true);
                    break;
                default:
                    playerMesh.mesh                         = M4Mesh;
                    playerMesh.transform.localScale         = new Vector3(1f, 1f, 1f);
                    playerMag.SetActive(true);
                    break;
            }
            playerMagFilter.sharedMesh = gun == currGun.Sniper && sniperMagMesh != null ? sniperMagMesh : defaultPlayerMagMesh;
        }
    }

    private bool IsValidVector3(Vector3 v)   => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
    private bool IsValidQuaternion(Quaternion q) => !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w));
}