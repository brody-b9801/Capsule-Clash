using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Alteruna;
using TMPro;
using NUnit.Framework;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using NUnit.Framework.Internal;

// ─────────────────────────────────────────────────────────────────────────────
// Generic object pool. Works for any Component type.
// ─────────────────────────────────────────────────────────────────────────────
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

// ─────────────────────────────────────────────────────────────────────────────
// Shooting – uses object pooling for bullets, muzzle particles, and casings.
// ─────────────────────────────────────────────────────────────────────────────
public class Shooting : AttributesSync
{
    // ── Serialised fields ──────────────────────────────────────────────────
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

    // ── Pool sizes (tweak in Inspector via a wrapper if needed) ────────────
    private const int BulletPoolSize  = 30;
    private const int MuzzlePoolSize  = 10;
    private const int CasingPoolSize  = 30;

    // ── Object pools ───────────────────────────────────────────────────────
    private ObjectPool<Rigidbody>      _bulletPool;
    private ObjectPool<ParticleSystem> _muzzlePool;
    private ObjectPool<Transform>      _casingPool;

    // Shared pool roots (keeps hierarchy tidy)
    private Transform _bulletPoolRoot;
    private Transform _muzzlePoolRoot;
    private Transform _casingPoolRoot;

    // ── Public / static state ──────────────────────────────────────────────
    public float bulletSpeed  = 15.0f;
    public float snipeSpeed   = 150.0f;
    public float fireRate;
    public float nextFireTime = 0.0f;

    public static int  reloadNum  = 30;
    public static int  shottieNum = 2;
    public static bool reloading  = false;

    public static bool playerShot;
    public static bool isShooting;
    public static bool canShoot   = true;
    public static bool lockCursor = false;
    public static bool shotgun    = false;
    public static float distance;
    public static Vector3 deltaPosition;
    public static float spread;
    public static Vector3 lastShotDirection = Vector3.zero;

    public Vector3 end;
    public static float changeOffset;
    public static float changeRotOffset;
    public static bool  playerJoin;
    public static bool  leaveHover = false;

    // ── Private state ──────────────────────────────────────────────────────
    private static Spawner _spawner;
    private Alteruna.Avatar avatar;

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

    // ─────────────────────────────────────────────────────────────────────
    void Start()
    {
        avatar = GetComponent<Alteruna.Avatar>();
        if (!avatar.IsOwner) return;

        alphaVal = 0;

        // Cache camera
        mainCamera          = Camera.main;
        mainCameraTransform = mainCamera.transform;

        // Scene references
        muzzleFlashCamera = GameObject.Find("CamQuad").GetComponent<MeshRenderer>().material;
        muzzleFlashCamera.color = new Color(
            muzzleFlashCamera.color.r,
            muzzleFlashCamera.color.g,
            muzzleFlashCamera.color.b, alphaVal);

        bulletHole  = GameObject.Find("MCBH").transform;
        bulletSpawn = GameObject.Find("bulletSpawn");
        casingSpawn = GameObject.Find("casingSpawn").transform;
        gunMesh     = GameObject.Find("CamAKM").GetComponent<MeshFilter>();
        CamAKM      = GameObject.Find("CamAKM");
        mag         = GameObject.Find("MC.Magazine");
        camCasing   = GameObject.Find("CamCasing");
        gunThing_g1 = GameObject.Find("CamAKM").transform;

        previousPosition = transform.position;
        lockCursor       = false;

        _spawner = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<Spawner>();
        cam2     = GameObject.Find("CameraTwo").GetComponent<Camera>();

        foreach (Transform child in gunThing)
            child.gameObject.layer = 1;

        camCasing.GetComponent<MeshRenderer>().enabled = false;

        // ── Initialise pools ──────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!avatar.IsOwner) return;

        isShooting = false;

        // Muzzle-flash fade
        muzzleFlashCamera.color = new Color(
            muzzleFlashCamera.color.r,
            muzzleFlashCamera.color.g,
            muzzleFlashCamera.color.b, alphaVal);

        // Movement delta
        Vector3 currentPosition = transform.position;
        deltaPosition    = currentPosition - previousPosition;
        previousPosition = currentPosition;

        // Cache camera for this frame
        Vector3 cameraPosition = mainCameraTransform.position;
        Vector3 cameraForward  = mainCameraTransform.forward;

        // ── Fire ──────────────────────────────────────────────────────────
        if (!shotgun)
        {
            if (Input.GetMouseButton(0) && Time.time >= nextFireTime &&
                reloadNum > 0 && !reloading && canShoot && !PlayerMovement.dead)
            {
                Vector3 useCameraPos = IsValidVector3(cameraPosition) ? cameraPosition : posSave;
                posSave = useCameraPos;

                isShooting = true;

                FireBullet(useCameraPos, cameraForward,
                    bulletSpawn.transform.position, 25f, 18f,
                    bulletHole.position, bH.transform.position,
                    Random.Range(-spread, spread),
                    Random.Range(-spread, spread),
                    Random.Range(-spread, spread), true);

                Shaker.shooting = true;
                Shaker.StopShake();
                Shaker.Instance.Shake();
                RetroDither.shotFired = true;
                ReloadAnimation.PlayAnim();
                StartCoroutine(EnableDisable());
                nextFireTime = Time.time + 1f / (fireRate * upgradeManager.fireRateMultiplier);
                reloadNum--;
            }
            else if (!(Input.GetMouseButton(0) && reloadNum > 0 && !reloading))
            {
                Shaker.shooting = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime &&
                shottieNum > 0 && !reloading && canShoot && !PlayerMovement.dead)
            {
                Vector3 useCameraPos = IsValidVector3(cameraPosition) ? cameraPosition : posSave;
                posSave = useCameraPos;

                isShooting = true;

                const float spreadMulti = 10f;
                for (int i = 0; i < 9; i++)
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
                RetroDither.shotgunFired = true;
                ReloadAnimation.PlayAnim();
                StartCoroutine(EnableDisable());
                nextFireTime = Time.time + 1f / fireRate * upgradeManager.fireRateMultiplier;
                shottieNum--;
            }
            else if (!(Input.GetMouseButton(0) && shottieNum > 0 && !reloading))
            {
                Shaker.shooting = false;
            }
        }

        // ── Reload ────────────────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!shotgun && !reloading && reloadNum != 30)
            {
                ReloadAnimation.PlayReload();
                StartCoroutine(waitReload());
                ReloadIndicator.Reload();
            }
            else if (shotgun && !reloading && shottieNum != 2)
            {
                ReloadAnimation.PlayReload();
                StartCoroutine(waitReload());
                ReloadIndicator.Reload();
            }
        }

        // ── Switch gun ────────────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.E) && !reloading && canChangeGun && !isShooting)
            StartCoroutine(gunChangeAnim());
    }

    // ─────────────────────────────────────────────────────────────────────
    private void FireBullet(
        Vector3 origin, Vector3 direction, Vector3 bS,
        float force, float damage,
        Vector3 bulletOrigin, Vector3 bHPos,
        float randomX, float randomY, float randomZ,
        bool doMuzzleFlash)
    {
        lockCursor = true;

        Vector3 spawnMuzzlePosition = avatar.IsOwner ? bulletOrigin : bHPos;
        Vector3 spawnPosition       = bS;

        if (!IsValidVector3(spawnMuzzlePosition) || !IsValidVector3(spawnPosition))
            return;

        // ── Rent bullet from pool ─────────────────────────────────────────
        Rigidbody bulletRb = _bulletPool.Get(spawnPosition, Quaternion.identity);
        GameObject bulletGO = bulletRb.gameObject;
        bulletGO.layer = avatar.IsOwner ? 7 : 6;

        // Reset all state before use
        CollisionControl cc = bulletGO.GetComponent<CollisionControl>();
        cc.OnSpawn();
        if (avatar.IsOwner)
        {
            cc.shooter        = avatar.ToString();
            cc.shottieBool    = shotgun;
            CollisionControl.avatar = true;
            playerShot        = true;
            lastShotDirection = direction;
        }
        else
        {
            CollisionControl.avatar = false;
            playerShot = false;
        }

        // Hook up the return-to-pool callback on CollisionControl
        // CollisionControl must call this when the bullet is done.
        cc.OnReturnToPool = () => _bulletPool.Return(bulletRb, _bulletPoolRoot);
        cc.InitBullet(this);

        CollisionControl.playerFire = gameObject;

        // ── Muzzle flash & casing ─────────────────────────────────────────
        if (doMuzzleFlash)
        {
            float randomAngle = Random.Range(-45f, 45f);

            if (avatar.IsOwner)
            {
                if (IsValidQuaternion(bulletHole.transform.rotation))
                {
                    Quaternion muzzleRot = bulletHole.transform.rotation *
                                          Quaternion.Euler(randomAngle, -90f, 0f);

                    ParticleSystem muzzleInst = _muzzlePool.Get(
                        spawnMuzzlePosition, muzzleRot, bulletHole.transform);
                    muzzleInst.transform.localPosition = Vector3.zero;
                    muzzleInst.Play();

                    // Auto-return muzzle particle after it finishes
                    StartCoroutine(ReturnParticleAfterPlay(muzzleInst, bulletHole.transform));

                    // ── Casing ────────────────────────────────────────────
                    Transform casing = _casingPool.Get(
                        casingSpawn.position, bulletCasingPrefab.transform.rotation, casingSpawn);
                    casing.gameObject.layer = 5;

                    BulletCasingAnim casingAnim = casing.GetComponent<BulletCasingAnim>();
                    if (casingAnim != null)
                        casingAnim.OnReturnToPool = () => _casingPool.Return(casing, _casingPoolRoot);
                }
            }
            else
            {
                if (IsValidQuaternion(bH.transform.rotation))
                {
                    Quaternion muzzleRot = bH.transform.rotation *
                                          Quaternion.Euler(randomAngle, -90f, 0f);

                    ParticleSystem muzzleInst = _muzzlePool.Get(
                        spawnMuzzlePosition, muzzleRot, bH.transform);
                    muzzleInst.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    muzzleInst.gameObject.layer     = 0;

                    foreach (Transform child in muzzleInst.transform)
                        child.gameObject.layer = 0;

                    muzzleInst.Play();
                    StartCoroutine(ReturnParticleAfterPlay(muzzleInst, bH.transform));
                }
            }
        }

        // ── Trajectory ────────────────────────────────────────────────────
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
        PlayerMovement.shotBoost = new Vector3(fireDirection.x, 0, fireDirection.z);

        Vector3 velocity      = fireDirection * bulletSpeed;

        if (IsValidVector3(PlayerMovement.newVelocity))
        {
            velocity += PlayerMovement.isGrounded
                ? new Vector3(PlayerMovement.newVelocity.x, 0f, PlayerMovement.newVelocity.z)
                  + PlayerMovement.dashVector
                : PlayerMovement.newVelocity + PlayerMovement.dashVector;
        }
        else
        {
            velocity += PlayerMovement.dashVector;
        }

        bulletRb.velocity = velocity;
        bulletRb.rotation = Quaternion.LookRotation(fireDirection);

        end            = targetPoint;
        isFiringBullet = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Pool return helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Waits for a ParticleSystem to finish playing then returns it to the pool.</summary>
    private IEnumerator ReturnParticleAfterPlay(ParticleSystem ps, Transform defaultParent)
    {
        // Guard: if the particle system was destroyed externally, bail out
        yield return new WaitWhile(() => ps != null && ps.IsAlive(true));
        if (ps != null)
            _muzzlePool.Return(ps, _muzzlePoolRoot);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Everything below is unchanged
    // ─────────────────────────────────────────────────────────────────────

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

        BroadcastRemoteMethod(1, shotgun,
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

    [SynchronizableMethod]
    private void BulletSync()
    {
        // Placeholder method
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
        yield return new WaitForSeconds(2.01f / upgradeManager.reloadSpeedMultiplier);

        if (shotgun) shottieNum = 2;
        else         reloadNum  = 30;

        reloading = false;
    }

    private void LateUpdate()
    {
        if (lockCursor && !ButtonHoverDetector.isHovering)
            Cursor.lockState = CursorLockMode.Locked;
    }

    [SynchronizableMethod]
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