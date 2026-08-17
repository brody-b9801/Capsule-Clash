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
        public bool isShotgun;
        public NetworkObject shooter;
        public bool hitPrev;
        public Vector3 hitPoint;
        public Vector3 hitNormal;
    } 

    private LayerMask layerMask;

    private bool onServer = false;


    private List<BulletData> activeBullets = new List<BulletData>();

    public void AddBulletData(NetworkObject bulletGO, Vector3 origin, bool shotgun, NetworkObject shooterObj)
    {
        activeBullets.Add(new BulletData
        {
            bulletObject = bulletGO,
            previousPosition = origin,
            startPosition = origin,
            timeActive = 0f,
            isShotgun = shotgun,
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
                Debug.DrawRay(bullet.previousPosition, currentPosition - bullet.previousPosition, Color.cyan);

                if (rb != null && rb.linearVelocity.sqrMagnitude > 0f)
                    rb.rotation = Quaternion.LookRotation(rb.linearVelocity.normalized);

                HandleRaycastHit(ref bullet);
            }

            bullet.timeActive += Time.deltaTime;
            bullet.previousPosition = currentPosition;

            float bulletDist = (currentPosition - bullet.startPosition).magnitude;

            if (bullet.hitPrev || (bullet.isShotgun && bulletDist > 20f) || bullet.timeActive > 7.5f)
            {
                DestroyBullet(bullet);
            }
            else
            {
                activeBullets[i] = bullet;
            }
        }
    }

    void HandleRaycastHit(ref BulletData bulletData)
    {
        Vector3 direction = bulletData.bulletObject.transform.position - bulletData.previousPosition;
        float rayDistance = direction.magnitude;
        RaycastHit hit;

        GameObject hitObject;
        if (Physics.Raycast(bulletData.previousPosition, direction.normalized, out hit, rayDistance, layerMask))
        {
            hitObject = hit.collider.gameObject;
            
            BuildHealth buildHealth = hitObject.GetComponent<BuildHealth>();
            if (buildHealth != null)
            {
                buildHealth.TakeDamage(bulletData.isShotgun, (bulletData.bulletObject.transform.position - bulletData.startPosition).magnitude);
            }

            if (hitObject.CompareTag("DamageCollider"))
            {
                Debug.Log("DamageCollider hit");
                NetworkObject victim = hitObject.GetComponentInParent<NetworkObject>();

                if (victim == null || victim == bulletData.shooter) return;
                Debug.Log("early return did not terminate");

                DamageControl damage = victim.gameObject.GetComponent<DamageControl>();
                if (damage != null)
                    damage.ControlDamage(bulletData.shooter, bulletData.isShotgun, (bulletData.bulletObject.transform.position - bulletData.startPosition).magnitude);
                
                if (bulletData.shooter.Owner != null)
                    SetDamageCross(bulletData.shooter.Owner);
            }
            impactPrefabInstance(hit.point, hit.normal);
            bulletData.hitPrev = true;
            DestroyBullet(bulletData);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void DestroyBullet(BulletData bullet)
    {
        ServerManager.Despawn(bullet.bulletObject);
        CollisionControl.SpawnImpact(bullet.hitPoint, bullet.hitNormal);
        activeBullets.Remove(bullet);
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
