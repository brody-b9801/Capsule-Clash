using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System.Collections.Generic;

public class BulletManager : NetworkBehaviour
{    
    [SerializeField] private GameObject impact;
    [SerializeField] private float impactOffset = 0.01f;
    
    public struct BulletData
    {
        public NetworkObject bulletObject;
        public Vector3 previousPosition;
        public Vector3 startPosition;
        public float timeActive;
        public Shooting.currGun gunType;
        public NetworkObject shooter;
        public bool hitPrev;
    }

    private LayerMask layerMask;

    private bool onServer = false;


    private List<BulletData> activeBullets = new List<BulletData>();

    public void AddBulletData(NetworkObject bulletGO, Vector3 origin, Shooting.currGun gun, NetworkObject shooterObj)
    {
        activeBullets.Add(new BulletData
        {
            bulletObject = bulletGO,
            previousPosition = origin,
            startPosition = origin,
            timeActive = 0f,
            gunType = gun,
            shooter = shooterObj,
            hitPrev = false
        });
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        onServer = true;
        layerMask = LayerMask.GetMask("DamageCollide", "Default", "BuildNoColPlayer");
    }

    private void Update()
    {
        if (!onServer) return;
        BulletCollisionDetection();
    }
    
    private void BulletCollisionDetection() {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            BulletData bullet = activeBullets[i];

            if (bullet.bulletObject == null) { activeBullets.RemoveAt(i); continue; }

            Rigidbody rb = bullet.bulletObject.GetComponent<Rigidbody>();
            Vector3 currentPosition = bullet.bulletObject.transform.position;

            if (!bullet.hitPrev)
            {
                if (rb != null && rb.linearVelocity.sqrMagnitude > 0f)
                    rb.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);

                HandleRaycastHit(ref bullet);
            }

            bullet.timeActive += Time.deltaTime;
            bullet.previousPosition = currentPosition;

            float bulletDist = (currentPosition - bullet.startPosition).magnitude;

            if (bullet.hitPrev || (bullet.gunType == Shooting.currGun.Shotgun && bulletDist > 20f) || bullet.timeActive > 7.5f)
            {
                DestroyBullet(i, bullet);
            }
            else
            {
                activeBullets[i] = bullet;
            }
        }
    }

    private const int MaxShooterSkips = 4;
    private const float ShooterSkipDistance = 0.01f;

    public static bool RaycastSkippingShooter(Vector3 origin, Vector3 direction, float distance, int mask, NetworkObject shooter, out RaycastHit hit)
    {
        for (int i = 0; i < MaxShooterSkips && distance > 0f; i++)
        {
            if (!Physics.Raycast(origin, direction, out hit, distance, mask)) return false;
            if (shooter == null || hit.collider.GetComponentInParent<NetworkObject>() != shooter) return true;

            float skip = hit.distance + ShooterSkipDistance;
            origin += direction * skip;
            distance -= skip;
        }

        hit = default;
        return false;
    }

    void HandleRaycastHit(ref BulletData bulletData)
    {
        Vector3 direction = bulletData.bulletObject.transform.position - bulletData.previousPosition;
        float rayDistance = direction.magnitude;
        if (rayDistance <= 0f) return;

        if (RaycastSkippingShooter(bulletData.previousPosition, direction / rayDistance, rayDistance, layerMask, bulletData.shooter, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            float hitDistance = (hit.point - bulletData.startPosition).magnitude;

            BuildHealth buildHealth = hitObject.GetComponent<BuildHealth>();
            if (buildHealth != null)
            {
                buildHealth.TakeDamage(bulletData.gunType, hitDistance);
            }

            if (hitObject.CompareTag("DamageCollider"))
            {
                NetworkObject victim = hitObject.GetComponentInParent<NetworkObject>();
                DamageControl damage = victim != null ? victim.gameObject.GetComponent<DamageControl>() : null;
                bool damageApplied = damage != null
                    && damage.ControlDamage(bulletData.shooter, bulletData.gunType, hitDistance);

                if (damageApplied && bulletData.shooter != null && bulletData.shooter.Owner != null && bulletData.shooter.Owner.IsValid)
                    SetDamageCross(bulletData.shooter.Owner);
            }
            impactPrefabInstance(hit.point, hit.normal);
            bulletData.hitPrev = true;
        }
    }

    private void DestroyBullet(int index, BulletData bullet)
    {
        activeBullets.RemoveAt(index);

        if (bullet.bulletObject != null && bullet.bulletObject.IsSpawned)
            ServerManager.Despawn(bullet.bulletObject);
    }
    
    [ObserversRpc]
    public void impactPrefabInstance(Vector3 hitpoint, Vector3 hitNormal)
    {
        Vector3 spawnPosition = hitpoint + hitNormal * impactOffset;
        Quaternion rotation = Quaternion.LookRotation(hitNormal);
        Instantiate(impact, spawnPosition, rotation);
    }

    [TargetRpc]
    private void SetDamageCross(NetworkConnection target)
    {
        DamageIndicatorControl.setDamageCross = true;
    }
}
