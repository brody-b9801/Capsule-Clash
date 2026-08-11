using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using FishNet.Object;

public class CollisionControl : MonoBehaviour
{
    [SerializeField] public Vector3 previousPosition;
    public static bool avatar = false;
    private bool hitPrev = false;
    [SerializeField] public Vector3 bulletEndPos;
    [SerializeField] private Material trailMaterial;
    [SerializeField] private GameObject impact;
    [SerializeField] private float impactOffset = 0.01f;
    public static bool impactBool;
    public GameObject bulletOne;
    public GameObject Visual;
    public bool shottieBool;
    public System.Action OnReturnToPool;
    private Vector3 bulletStartPos;
    private float bulletDist;
    private float bulletDist2;
    private bool visualEnabled = false;
    private bool rotationApplied = false;
    [SerializeField] private float showDistance = 0.5f; // Distance before bullet becomes visible
    
    private Rigidbody rb;
    private TrailRenderer trail;
    private Camera mainCamera;
    private Transform mainCameraTransform;
    private static Transform _cachedBulletHole;
    
    private RaycastHit hit;
    private int framesElapsed = 0;
    private ParticleSystem ps;

    void Start()
    {
        mainCamera = Camera.main;
        mainCameraTransform = mainCamera.transform;
        rb = GetComponent<Rigidbody>();
        trail = bulletOne != null ? bulletOne.GetComponentInChildren<TrailRenderer>() : GetComponentInChildren<TrailRenderer>();
        
        if (_cachedBulletHole == null)
            _cachedBulletHole = GameObject.Find("PlayerBulletHole").transform;
        if (_cachedImpactPrefab == null)
            _cachedImpactPrefab = impact;
        bulletEndPos = _cachedBulletHole.position;
        Visual.SetActive(false);
        bulletOne.SetActive(false);
        previousPosition = mainCameraTransform.position;
        bulletStartPos = transform.position;
        rotationApplied = false;
        //ps = GetComponentInChildren<ParticleSystem>();
        //ps.Stop();
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;

        // Apply rotation immediately upon instantiation (first Update after spawn)
        if (!rotationApplied && rb != null && rb.linearVelocity != Vector3.zero)
        {
            rb.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);
            rotationApplied = true;
        }

        bulletDist = (currentPosition - bulletStartPos).magnitude;

        if (shottieBool && bulletDist > 20f) {
            if (rb != null) {
                rb.linearVelocity = Vector3.zero;
            }
            if (trail != null) { trail.emitting = false; trail.enabled = false; trail.Clear(); }
            DestroyObject();
            bulletOne.SetActive(false);
        }

        if (!visualEnabled) {
            float movedDist = (currentPosition - bulletStartPos).magnitude;
            if (movedDist >= showDistance) {
                visualEnabled = true;
                Visual.SetActive(true);
                //ps.Play();
                bulletOne.SetActive(true);
            } else {
                Visual.SetActive(false);
                bulletOne.SetActive(false);
            }
        }
    }
    public void OnSpawn()
    {
        if (mainCameraTransform == null)
        {
            mainCamera = Camera.main;
            mainCameraTransform = mainCamera.transform;
        }

        if (_cachedBulletHole == null)
            _cachedBulletHole = GameObject.Find("PlayerBulletHole").transform;
        bulletEndPos     = _cachedBulletHole.position;
        previousPosition = mainCameraTransform.position;
        bulletStartPos   = transform.position;
        hitPrev         = false;
        visualEnabled   = false;
        Visual.SetActive(false);
        bulletOne.SetActive(false);
        if (rb != null) rb.linearVelocity = Vector3.zero;
        if (trail == null)
            trail = bulletOne != null ? bulletOne.GetComponentInChildren<TrailRenderer>() : GetComponentInChildren<TrailRenderer>();
        if (trail != null)
        {
            trail.emitting = false;
            trail.enabled = true;
            trail.autodestruct = false;
            trail.Clear();
            trail.emitting = true;
        }
    }
    public void InitBullet(MonoBehaviour coroutineOwner)
    {
        coroutineOwner.StartCoroutine(BulletTimeout());
    }

    private IEnumerator BulletTimeout()
    {
        yield return new WaitForSeconds(7.5f);
        if (gameObject.activeInHierarchy)
            DestroyObject();
    }

    public void DestroyObject()
    {
        if (trail != null) { trail.emitting = false; trail.enabled = false; trail.Clear(); }

        Visual.SetActive(false);
        bulletOne.SetActive(false);
        hitPrev = false;
        visualEnabled = false;

        if (OnReturnToPool != null)
        {
            OnReturnToPool.Invoke();
            OnReturnToPool = null;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (avatar)
        {
            previousPosition = transform.position;
        }
    }

    public void impactPrefabInstance(Vector3 hitpoint, Vector3 hitNormal)
    {
        Vector3 spawnPosition = hitpoint + hitNormal * impactOffset;

        Quaternion rotation = Quaternion.LookRotation(hitNormal);

        Instantiate(impact, spawnPosition, rotation);
    }

    /// <summary>
    /// Impact FX spawner for server-driven hits. Shooting.RpcBulletImpact calls this
    /// on every client; the prefab reference is cached from the first bullet to spawn.
    /// </summary>
    private static GameObject _cachedImpactPrefab;

    public static void SpawnImpact(Vector3 hitpoint, Vector3 hitNormal, float offset = 0.01f)
    {
        if (_cachedImpactPrefab == null) return;

        Vector3    spawnPosition = hitpoint + hitNormal * offset;
        Quaternion rotation      = Quaternion.LookRotation(hitNormal);

        Instantiate(_cachedImpactPrefab, spawnPosition, rotation);
    }
}