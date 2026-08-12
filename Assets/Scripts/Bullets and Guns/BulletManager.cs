using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;

public class BulletManager : NetworkBehaviour
{
    [SerializeField] private Material       damaged;
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
    } 

    private LayerMask layerMask;


    private List<BulletData> activeBullets = new List<BulletData>();

    public void AddBulletData()
    {
        
    }

    private void OnServerStart()
    {
        layerMask = LayerMask.GetMask("DamageCollide", "Default", "BuildNoColPlayer");
    }

    private void Update()
    {
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
                if (rb != null) rb.linearVelocity = Vector3.zero;
                if (bullet.bulletObject != null) Destroy(bullet.bulletObject.gameObject);
                activeBullets.RemoveAt(i);
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
                NetworkObject victim = hitObject.GetComponentInParent<NetworkObject>();

                if (victim == null || victim == bulletData.shooter) return;

                GameObject parentObject = victim.gameObject;

                Renderer renderer = parentObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = damaged;
                }

                ChangeMat changeMat = parentObject.GetComponent<ChangeMat>();
                if (changeMat != null)
                {
                    changeMat.TakeDamage(victim, bulletData.shooter, bulletData.isShotgun, (bulletData.bulletObject.transform.position - bulletData.startPosition).magnitude);
                }

                DamageIndicatorControl.setDamageCross = true;
            }

            if (hitObject.CompareTag("tester"))
            {
                PlayerMovement.Local.hitCount++;
            }

            bulletData.hitPrev = true;
            impactPrefabInstance(hit.point, hit.normal);
        }
    }

    public void impactPrefabInstance(Vector3 hitpoint, Vector3 hitNormal)
    {
        Vector3 spawnPosition = hitpoint + hitNormal * impactOffset;

        Quaternion rotation = Quaternion.LookRotation(hitNormal);

        Instantiate(impact, spawnPosition, rotation);
    }
}
