using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FishNet.Object;
using TMPro;
using NUnit.Framework;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

    /// <summary>Rent an object from the pool (activates it).</summary>
    public T Get(Vector3 position, Quaternion rotation, Transform newParent = null)
    {
        T instance = _pool.Count > 0 ? _pool.Dequeue() : CreateInstance();

        Transform t = instance.transform;
        if (newParent != null) t.SetParent(newParent, false);
        t.position = position;
        t.rotation = rotation;
        instance.gameObject.SetActive(true);
        return instance;
    }

    /// <summary>Return an object to the pool (deactivates it).</summary>
    public void Return(T instance, Transform defaultParent = null)
    {
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
    [SerializeField] private Material       damaged;
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

    private const int BulletPoolSize  = 30;
    private const int MuzzlePoolSize  = 10;
    private const int CasingPoolSize  = 30;

    private ObjectPool<Rigidbody>      _bulletPool;
    private ObjectPool<ParticleSystem> _muzzlePool;
    private ObjectPool<Transform>      _casingPool;

    private Transform _bulletPoolRoot;
    private Transform _muzzlePoolRoot;
    private Transform _casingPoolRoot;
    public float bulletSpeed  = 15.0f;
    public float snipeSpeed   = 150.0f;
    public float fireRate;
    public float nextFireTime = 0.0f;

    public static Shooting Local { get; private set; }

    public int  reloadNum  = 30;
    public int shottieNum = 2;
    public bool reloading  = false;

    public bool playerShot;
    public bool isShooting;
    public bool canShoot   = true;
    public static bool lockCursor = false;
    public bool shotgun    = false;
    public static float distance;
    public static Vector3 deltaPosition;
    public static float spread;
    public static Vector3 lastShotDirection = Vector3.zero;

    public Vector3 end;
    public static float changeOffset;
    public static float changeRotOffset;
    public static bool  playerJoin;
    public static bool  leaveHover = false;


    private Transform bulletHole;
    private Transform casingSpawn;
    private Material  muzzleFlashCamera;
    private Transform gunThing_g1;

    private Material  muzzleFlashCameraMat;
    private float     alphaVal;
    private bool      isFiringBullet = false;
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

    public float trailFadeDuration = 0.5f;

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        base.OnStopClient();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner) return;

        // Must be owner-gated: Awake ran on every player instance, so the last
        // one to spawn claimed Local. Callers such as PlayerMovement then wrote
        // to a remote player's Shooting, whose fields were never initialized
        // because this method returns early for non-owners.
        Local = this;

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
        camCasing   = SceneLookup.FindInactive("CamCasing");
        gunThing_g1 = SceneLookup.FindInactive("CamAKM").transform;

        previousPosition = transform.position;
        lockCursor       = false;

        cam2     = GameObject.Find("CameraTwo").GetComponent<Camera>();

        camCasing.GetComponent<MeshRenderer>().enabled = false;

        _bulletPoolRoot = CreatePoolRoot("Pool_Bullets");
        _muzzlePoolRoot = CreatePoolRoot("Pool_Muzzle");
        _casingPoolRoot = CreatePoolRoot("Pool_Casings");

        _bulletPool = new ObjectPool<Rigidbody>(
            bulletPrefab.GetComponent<Rigidbody>(),
            BulletPoolSize, _bulletPoolRoot);

        _muzzlePool = new ObjectPool<ParticleSystem>(
            muzzlePrefab,
            MuzzlePoolSize, _muzzlePoolRoot);

        _casingPool = new ObjectPool<Transform>(
            bulletCasingPrefab.GetComponent<Transform>(),
            CasingPoolSize, _casingPoolRoot);
    }

    private Transform CreatePoolRoot(string name)
    {
        var go = new GameObject(name);
        return go.transform;
    }

    void Update()
    {
        if (!IsOwner) return;
        // OnStartClient does the scene lookups that populate these; it can run
        // after the first Update tick, so idle until initialization completed.
        if (muzzleFlashCamera == null || mainCameraTransform == null) return;

        isShooting = false;

        muzzleFlashCamera.color = new Color(muzzleFlashCamera.color.r, muzzleFlashCamera.color.g, muzzleFlashCamera.color.b, alphaVal);

        Vector3 currentPosition = transform.position;
        deltaPosition    = currentPosition - previousPosition;
        previousPosition = currentPosition;

        Vector3 cameraPosition = mainCameraTransform.position;
        Vector3 cameraForward  = mainCameraTransform.forward;

        ref int ammo = ref shotgun ? ref shottieNum : ref reloadNum;

            bool inputCheck = shotgun ? Input.GetMouseButtonDown(0) : Input.GetMouseButton(0);
            if (inputCheck && Time.time >= nextFireTime && PlayerMovement.Local != null &&
                ammo > 0 && !reloading && canShoot && !PlayerMovement.Local.dead && !HoverCheck.isHovering)
            {
                Vector3 useCameraPos = IsValidVector3(cameraPosition) ? cameraPosition : posSave;
                posSave = useCameraPos;

                isShooting = true;
                

                float spreadMulti = shotgun ? 10f : 1f;
                for (int i = 0; i < (shotgun ? 9 : 1); i++)
                {
                    FireBullet(useCameraPos, cameraForward,
                        bulletSpawn.transform.position, 25f, 15f,
                        bulletHole.position, bH.transform.position,
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        Random.Range(-spread * spreadMulti, spread * spreadMulti),
                        i == 0);
                }

                Shaker.shooting = true;
                Shaker.StopShake();
                Shaker.Instance.Shake();
                if (shotgun) RetroDither.shotgunFired = true;
                else        RetroDither.shotFired    = true;
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
            if ((!shotgun && !reloading && reloadNum != 30) || (shotgun && !reloading && shottieNum != 2))
            {
                ReloadAnimation.PlayReload();
                StartCoroutine(waitReload());
                ReloadIndicator.Reload();
            }
        }

        if (Input.GetKeyDown(KeyCode.E) && !reloading && canChangeGun && !isShooting)
            StartCoroutine(gunChangeAnim());
    }

    private void FireBullet(
        Vector3 origin, Vector3 direction, Vector3 bS,
        float force, float damage,
        Vector3 bulletOrigin, Vector3 bHPos,
        float randomX, float randomY, float randomZ,
        bool doMuzzleFlash)
    {
        bool isOwner = IsOwner;
        lockCursor = true;

        Vector3 spawnMuzzlePosition = IsOwner ? bulletOrigin : bHPos;
        Vector3 spawnPosition       = bS;

        Rigidbody bulletRb = _bulletPool.Get(spawnPosition, Quaternion.identity);
        GameObject bulletGO = bulletRb.gameObject;
        bulletGO.layer = IsOwner ? 7 : 6;

        CollisionControl cc = bulletGO.GetComponent<CollisionControl>();
        cc.OnSpawn();
        if (isOwner)
        {
            cc.shooter        = NetworkObject;
            cc.shottieBool    = shotgun;
            lastShotDirection = direction;
        }
        CollisionControl.avatar = isOwner;
        playerShot = isOwner;

        cc.OnReturnToPool = () => _bulletPool.Return(bulletRb, _bulletPoolRoot);
        cc.InitBullet(this);

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

        RaycastHit hit;
        Vector3 targetPoint;

        if (Physics.Raycast(new Ray(mainCameraTransform.position, mainCameraTransform.forward), out hit, 1.5f, ~ignoreLayers)){
            targetPoint              = transform.position + direction * force;
            origin = mainCameraTransform.position;
            bulletGO.transform.position = origin;
            CollisionControl.impactBool = true; 
        }
        else if (Physics.Raycast(new Ray(origin, direction), out hit, force, ~ignoreLayers))
        {
            targetPoint              = hit.point;
            CollisionControl.impactBool = true;
        }
        else
        {
            targetPoint              = origin + direction * force;
            CollisionControl.impactBool = false;
        }

        Vector3 spreadVector     = new Vector3(randomX, randomY, randomZ);
        float   distanceFromCamera = Vector3.Distance(origin, targetPoint);
        targetPoint += spreadVector * distanceFromCamera;

        Vector3 fireDirection = (targetPoint - origin).normalized;
        PlayerMovement.Local.shotBoost = new Vector3(fireDirection.x, 0, fireDirection.z);

        Vector3 velocity      = fireDirection * bulletSpeed;

        if (IsValidVector3(PlayerMovement.Local.newVelocity))
        {
            velocity += PlayerMovement.Local.isGrounded
                ? new Vector3(PlayerMovement.Local.newVelocity.x, 0f, PlayerMovement.Local.newVelocity.z)
                  + PlayerMovement.Local.dashVector
                : PlayerMovement.Local.newVelocity + PlayerMovement.Local.dashVector;
        }
        else
        {
            velocity += PlayerMovement.Local.dashVector;
        }

        bulletRb.linearVelocity = velocity;
        bulletRb.rotation = Quaternion.LookRotation(fireDirection);

        end            = targetPoint;
        isFiringBullet = true;
    }

    /// <summary>Waits for a ParticleSystem to finish playing then returns it to the pool.</summary>
    private IEnumerator ReturnParticleAfterPlay(ParticleSystem ps, Transform defaultParent)
    {
        yield return new WaitWhile(() => ps != null && ps.IsAlive(true));
        if (ps != null)
            _muzzlePool.Return(ps, _muzzlePoolRoot);
    }

    public IEnumerator gunChangeAnim()
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

        shotgun = !shotgun;

        if (shotgun)
        {
            fireRate = 3.5f;
            CamAKM.transform.localScale = new Vector3(1.075f, 1.075f, 1.075f);
            gunMesh.mesh = shotgunMesh;
            mag.GetComponent<MeshRenderer>().enabled    = false;
            camCasing.GetComponent<MeshRenderer>().enabled = true;
            bulletHole.transform.localPosition = new Vector3(bulletHole.transform.localPosition.x, bulletHole.transform.localPosition.y, 0.36f);
            bH.transform.localPosition         = new Vector3(bH.transform.localPosition.x, bH.transform.localPosition.y, 0.36f);
        }
        else
        {
            fireRate = 10f;
            CamAKM.transform.localScale = new Vector3(1.25f, 1f, 1f);
            gunMesh.mesh = M4Mesh;
            mag.GetComponent<MeshRenderer>().enabled    = true;
            camCasing.GetComponent<MeshRenderer>().enabled = false;
            bulletHole.transform.localPosition = new Vector3(bulletHole.transform.localPosition.x, bulletHole.transform.localPosition.y, 0.6f);
            bH.transform.localPosition         = new Vector3(bH.transform.localPosition.x, bH.transform.localPosition.y, 0.6f);
        }

        ServerGunSkin(shotgun,
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

    IEnumerator waitReload()
    {
        reloading = true;
        yield return new WaitForSeconds((shotgun && shottieNum == 1) ? 1.31f : 2.01f / upgradeManager.Local.reloadSpeedMultiplier);

        if (shotgun) shottieNum = 2;
        else         reloadNum  = 30;

        reloading = false;
    }

    private void LateUpdate()
    {
        if (lockCursor && !HoverCheck.isHovering)
            Cursor.lockState = CursorLockMode.Locked;
    }

    [ServerRpc]
    private void ServerGunSkin(bool sg, Vector3 pos, Quaternion rot, bool networkedCall)
        => RpcGunSkin(sg, pos, rot, networkedCall);

    [ObserversRpc(BufferLast = true)]
    private void RpcGunSkin(bool sg, Vector3 pos, Quaternion rot, bool networkedCall)
        => gunSkinSync(sg, pos, rot, networkedCall);

    public void gunSkinSync(bool sg, Vector3 pos, Quaternion rot, bool networkedCall)
    {
        if (!networkedCall)
        {
            if (sg)
            {
                playerMesh.mesh                         = shotgunMesh;
                playerMesh.transform.localScale         = new Vector3(1.2f, 1.2f, 1.2f);
                playerMag.SetActive(false);
            }
            else
            {
                playerMesh.mesh                         = M4Mesh;
                playerMesh.transform.localScale         = new Vector3(1f, 1f, 1f);
                playerMag.SetActive(true);
            }
        }
    }

    private bool IsValidVector3(Vector3 v)   => !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
    private bool IsValidQuaternion(Quaternion q) => !(float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w));
}