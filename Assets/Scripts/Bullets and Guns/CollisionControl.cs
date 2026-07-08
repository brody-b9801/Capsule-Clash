using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Alteruna;

public class CollisionControl : AttributesSync
{
    [SerializeField] private Material normal;
    [SerializeField] private Material damaged;
    private int layerMask;
    private bool isGrounded = false;
    private GameObject hitObject;
    [SerializeField] public Vector3 previousPosition;
    public static bool avatar = false;
    private bool hitPrev = false;
    [SerializeField] public Vector3 bulletEndPos;
    [SerializeField] private Material trailMaterial;
    public static GameObject playerFire;
    [SerializeField] private GameObject impact;
    [SerializeField] private float impactOffset = 0.01f;
    public static bool impactBool;
    public GameObject bulletOne;
    public GameObject Visual;
    public string shooter;
    public bool shottieBool;
    public System.Action OnReturnToPool;
    [SynchronizableField] private bool playerDead = false;
    private Vector3 bulletStartPos;
    private float bulletDist;
    private float bulletDist2;
    private bool visualEnabled = false;
    private bool rotationApplied = false;
    [SerializeField] private float showDistance = 0.5f; // Distance before bullet becomes visible
    
    // Cached references
    private Rigidbody rb;
    private TrailRenderer trail;
    private Camera mainCamera;
    private Transform mainCameraTransform;
    private static Transform _cachedBulletHole;
    
    // Optimization: reusable raycast hit
    private RaycastHit hit;
    private int framesElapsed = 0;
    private ParticleSystem ps;

    void Start()
    {
        // Cache references
        mainCamera = Camera.main;
        mainCameraTransform = mainCamera.transform;
        rb = GetComponent<Rigidbody>();
        trail = bulletOne != null ? bulletOne.GetComponentInChildren<TrailRenderer>() : GetComponentInChildren<TrailRenderer>();
        
        if (_cachedBulletHole == null)
            _cachedBulletHole = GameObject.Find("PlayerBulletHole").transform;
        bulletEndPos = _cachedBulletHole.position;
        Visual.SetActive(false);
        bulletOne.SetActive(false);
        previousPosition = mainCameraTransform.position;
        bulletStartPos = transform.position;
        rotationApplied = false;
        //ps = GetComponentInChildren<ParticleSystem>(); 
        //ps.Stop();

        layerMask = LayerMask.GetMask("DamageCollide", "Default", "BuildNoColPlayer");
    }

    void Update()
    {
        if (!avatar) return;

        Vector3 currentPosition = transform.position;

        // Draw debug ray
        Debug.DrawRay(previousPosition, currentPosition - previousPosition, Color.cyan);

        // Apply rotation immediately upon instantiation (first Update after spawn)
        if (!rotationApplied && rb != null && rb.velocity != Vector3.zero)
        {
            rb.rotation = Quaternion.LookRotation(rb.velocity.normalized);
            rotationApplied = true;
        }

        // Handle collision detection
        HandleRaycastHit(previousPosition, currentPosition, shooter);

        // Calculate distance for shotgun range check
        bulletDist = (currentPosition - bulletStartPos).magnitude;

        // Shotgun range limit
        if (shottieBool && bulletDist > 20f) {
            if (rb != null) {
                rb.velocity = Vector3.zero;
            }
            if (trail != null) { trail.emitting = false; trail.enabled = false; trail.Clear(); }
            BroadcastRemoteMethod(1);
            bulletOne.SetActive(false);
        }

        // Visual enablement distance check
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
    
    void HandleRaycastHit(Vector3 previousPos, Vector3 currentPos, string shooter)
    {
        if (!avatar || hitPrev) return;
        
        Vector3 direction = currentPos - previousPos;
        float rayDistance = direction.magnitude;
        
        // Perform raycast
        if (Physics.Raycast(previousPos, direction.normalized, out hit, rayDistance, layerMask))
        {
            hitObject = hit.collider.gameObject;
            
            // Handle building damage
            BuildHealth buildHealth = hitObject.GetComponent<BuildHealth>();
            if (buildHealth != null)
            {
                buildHealth.TakeDamage(shottieBool, bulletDist);
            }

            // Handle player damage
            if (hitObject.CompareTag("DamageCollider"))
            {
                GameObject parentObject = hitObject.transform.parent.gameObject;
                
                // Ensure we're not hitting ourselves
                if (parentObject != playerFire && avatar)
                {
                    Renderer renderer = parentObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.sharedMaterial = damaged;
                    }
                    
                    ChangeMat changeMat = parentObject.GetComponent<ChangeMat>();
                    if (changeMat != null)
                    {
                        Alteruna.Avatar targetAvatar = parentObject.GetComponent<Alteruna.Avatar>();
                        if (targetAvatar != null)
                        {
                            changeMat.TakeDamage(targetAvatar.ToString(), shooter, shottieBool, bulletDist);
                        }
                    }
                    
                    DamageIndicatorControl.setDamageCross = true;
                }
            }

            // Handle test target
            if (hitObject.CompareTag("tester"))
            {
                PlayerMovement.hitCount++;
            }

            // Mark as hit and broadcast impact
            hitPrev = true;
            BroadcastRemoteMethod(2, hit.point, hit.normal);

            if (trail != null) { trail.emitting = false; trail.enabled = false; trail.Clear(); }

            // Move bullet to hit point and destroy
            transform.position = hit.point;
            BroadcastRemoteMethod(1);
            bulletOne.SetActive(false);
        }
    }
    
    [SynchronizableMethod]
    private void damageSync()
    {
        //Placeholder method
    }

    // Called by Shooting every time the bullet is rented from the pool.
    // Replicates everything Start() did so reused bullets are clean.
    public void OnSpawn()
    {
        // Re-cache camera in case Start ran before the camera existed
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
        if (rb != null) rb.velocity = Vector3.zero;
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
            BroadcastRemoteMethod(1);
    }

    [SynchronizableMethod]
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

    [SynchronizableMethod]
    public void impactPrefabInstance(Vector3 hitpoint, Vector3 hitNormal)
    {
        Vector3 spawnPosition = hitpoint + hitNormal * impactOffset;
        
        Quaternion rotation = Quaternion.LookRotation(hitNormal);
        
        Instantiate(impact, spawnPosition, rotation);
    }
}