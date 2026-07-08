using UnityEngine;
using System.Collections.Generic;

public class ProceduralTreePlacer : MonoBehaviour
{
    public LayerMask terrainLayer;
    public GameObject[] treePrefabs;
    public int maxTrees = 1000;
    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    [Range(0f, 1f)] public float densityThreshold = 0.5f;
    public float noiseScale = 25f;
    public Vector2 noiseOffset;
    public Vector2 areaSize = new Vector2(200, 200);
    public Vector3 areaCenter;
    public List<BoxExclusionZone> exclusionZones = new List<BoxExclusionZone>();
    [Range(0, 90)] public float maxSlopeAngle = 35f;

    [ContextMenu("Generate Trees")]
    public void GenerateTrees()
    {
        ClearTrees();

        if (treePrefabs == null || treePrefabs.Length == 0)
        {
            Debug.LogError("[TreePlacer] treePrefabs is empty.", this);
            return;
        }
        if (terrainLayer == 0)
            Debug.LogWarning("[TreePlacer] terrainLayer is set to Nothing; no raycast will hit.", this);

        int placed = 0;
        int attempts = 0;
        int rejNoise = 0, rejRaycast = 0, rejExclusion = 0, rejSlope = 0, rejTree = 0;

        while (placed < maxTrees && attempts < maxTrees * 10)
        {
            attempts++;

            Vector3 samplePos = new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                500f,
                Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f)
            ) + areaCenter;

            float noiseValue = Mathf.PerlinNoise(
                (samplePos.x + noiseOffset.x) / noiseScale,
                (samplePos.z + noiseOffset.y) / noiseScale
            );

            if (noiseValue < densityThreshold)
            { rejNoise++; continue; }

            if (!Physics.Raycast(samplePos, Vector3.down, out RaycastHit hit, 1000f, terrainLayer))
            { rejRaycast++; continue; }

            if (hit.collider.transform.IsChildOf(transform))
            { rejTree++; continue; }

            if (IsInsideExclusionBox(hit.point))
            { rejExclusion++; continue; }

            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope > maxSlopeAngle)
            { rejSlope++; continue; }

            GameObject prefab = treePrefabs[Random.Range(0, treePrefabs.Length)];
            GameObject tree = Instantiate(
                prefab,
                hit.point,
                Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                transform
            );

            float scale = Random.Range(scaleRange.x, scaleRange.y);
            tree.transform.localScale = new Vector3(scale, scale, scale);

            placed++;
        }

        Debug.Log($"[TreePlacer] placed={placed} attempts={attempts} | rejected: noise={rejNoise} raycast={rejRaycast} tree={rejTree} exclusion={rejExclusion} slope={rejSlope}", this);
    }

    [ContextMenu("Clear Trees")]
    void ClearTrees()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(transform.GetChild(i).gameObject);
            else
                Destroy(transform.GetChild(i).gameObject);
            #else
            Destroy(transform.GetChild(i).gameObject);
            #endif
        }
    }

    bool IsInsideExclusionBox(Vector3 worldPos)
    {
        Vector2 p = new Vector2(worldPos.x, worldPos.z);

        foreach (var zone in exclusionZones)
        {
            Vector2 min = zone.center - zone.size * 0.5f;
            Vector2 max = zone.center + zone.size * 0.5f;

            if (p.x >= min.x && p.x <= max.x &&
                p.y >= min.y && p.y <= max.y)
                return true;
        }
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(areaCenter, new Vector3(areaSize.x, 1f, areaSize.y));

        Gizmos.color = Color.red;
        foreach (var zone in exclusionZones)
        {
            Vector3 center = new Vector3(zone.center.x, areaCenter.y, zone.center.y);
            Vector3 size = new Vector3(zone.size.x, 1f, zone.size.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}

[System.Serializable]
public class BoxExclusionZone
{
    [Tooltip("XZ world position")]
    public Vector2 center;

    [Tooltip("Width (X) and Depth (Z)")]
    public Vector2 size = new Vector2(10f, 10f);
}
