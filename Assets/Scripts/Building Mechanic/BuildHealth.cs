using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Alteruna;
using Unity.VisualScripting;

public class BuildHealth : AttributesSync
{
    public Animator anim;
    public int maxHealth = 4;
    [SynchronizableField] private float currentHealth;
    [SerializeField] private GameObject build;
    public int panelStage = 0;
    public MeshRenderer transMesh;

    void Awake()
    {
        currentHealth = maxHealth;

        if (transMesh == null)
        {
            Transform cube = transform.Find("Cube");
            if (cube == null && build != null)
                cube = build.transform.Find("Cube");
            if (cube != null)
                transMesh = cube.GetComponent<MeshRenderer>();
        }
    }

    private static Material _transMat;

    public static void removeBG()
    {
        BuildHealth[] all = FindObjectsByType<BuildHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
            all[i].removeBGRef();
    }

    public void removeBGRef()
    {
        if (transMesh != null)
            transMesh.enabled = false;
    }
    public void TakeDamage(bool shotgun, float dist)
    {
        BroadcastRemoteMethod(0, shotgun, dist);
    }

    [SynchronizableMethod]
    public void buildDamageSync(bool sg, float dist) {
        // Unmerge combined meshes so damage is visible
        WallFinished wallFinished = build.GetComponent<WallFinished>();
        if (wallFinished == null)
            wallFinished = build.GetComponentInParent<WallFinished>(); // Try parent
        if (wallFinished == null)
            wallFinished = build.GetComponentInChildren<WallFinished>(); // Try children
        
        if (wallFinished != null)
        {
            wallFinished.UnmergeChildren();
            Debug.Log("Meshes unmerged on damage");
        }
        else
        {
            Debug.LogWarning("WallFinished component not found!");
        }

        if (!sg) {
            currentHealth--;
        } else {
            if (dist < 3) {
                currentHealth -= 0.5f;
            } else {
                currentHealth -= Mathf.Clamp((1-((dist-5)*0.1f))*.25f, 0.075f, 0.5f);
                Debug.Log((1-((dist-5)*0.1f)));
            }
        }
        if (_transMat == null)
        {
            _transMat = new Material(transMesh.sharedMaterial);
        }
        _transMat.color = new Color(1f, 0f, 0f, 40f / 255f);
        transMesh.sharedMaterial = _transMat;
        if (anim != null)
            anim.SetInteger("Health", (int)currentHealth);
        if ((int)currentHealth <= 0)
        {
            ObjectSpawner.DespawnObject(build);
        }
    }     
}
