#if UNITY_EDITOR
//using System;
using System.Collections.Generic;
//using System.Numerics;
using UnityEditor.EditorTools;
using UnityEngine;

public class ProceduralVoidIslands : MonoBehaviour
{
    [Header("Islands")]
    public GameObject[] islandPrefabs;
    public int maxIslands = 50;
    public Vector2 areaDimensions = new Vector2(100, 100);
    public float heightOffsetAmp = 10;
    public Vector2 scalingRange = new Vector2(0.8f, 1.2f);
    [Tooltip("Adjust for clump size")]
    public float noiseScale = 25f;
    [Tooltip("Higher = fewer, tighter clumps")]
    public float noiseThreshold = 0.6f;
    [Tooltip("Extra clearance added to each island's bounds radius when checking for overlap")]
    public float islandSpacingPadding = 2f;

    [Header("Decorations")]
    public GameObject[] decorationPrefabs;
    [Tooltip("Decoration attempts per unit of island radius (density scales with island size)")]
    public float decorationsPerRadius = 1.5f;
    [Tooltip("Hard cap on decoration attempts for a single island")]
    public int maxDecorationsPerIsland = 40;
    [Tooltip("Extra clearance added between decoration footprints so they don't overlap")]
    public float decorationSpacingPadding = 0.25f;
    public Vector2 decorationScaleRange = new Vector2(0.8f, 1.2f);
    [Tooltip("Raycast origin height above island center")]
    public float raycastHeight = 20f;
    [Tooltip("Radius around island center to scatter decoration raycasts")]
    public float decorationRadius = 3f;
    [Tooltip("Layer mask for island surfaces")]
    public LayerMask islandLayerMask = ~0;

    private int spawnedIslands = 0;
    private readonly List<Vector3> placedCenters = new List<Vector3>();
    private readonly List<float> placedRadii = new List<float>();

    [ContextMenu("Clear Islands")]
    void ClearIslands()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        spawnedIslands = 0;
        placedCenters.Clear();
        placedRadii.Clear();
    }

    [ContextMenu("Generate Islands")]
    void GenerateIslands()
    {
        spawnedIslands = 0;
        placedCenters.Clear();
        placedRadii.Clear();
        int attempts = 0;
        while (spawnedIslands < maxIslands && attempts < maxIslands * 10)
        {
            attempts++;
            float x = Random.Range(-0.5f, 0.5f) * areaDimensions.x;
            float z = Random.Range(-0.5f, 0.5f) * areaDimensions.y;
            float yOffset = Random.Range(-heightOffsetAmp, heightOffsetAmp);
            float noiseValue = Mathf.PerlinNoise((x + 1000f) / noiseScale, (z + 1000f) / noiseScale);
            if (noiseValue < noiseThreshold) continue;

            int islandIndex = Random.Range(0, islandPrefabs.Length);
            GameObject prefab = islandPrefabs[islandIndex];

            float scale = Random.Range(scalingRange.x, scalingRange.y);
            float prefabRadius = GetPrefabHorizontalRadius(prefab);
            float candidateRadius = prefabRadius * scale + islandSpacingPadding;

            Vector3 pose = transform.position + new Vector3(x, yOffset, z);

            if (OverlapsExisting(pose, candidateRadius)) continue;

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject island = Instantiate(prefab, pose, rotation, transform);
            island.transform.localScale = new Vector3(scale, scale, scale);

            placedCenters.Add(pose);
            placedRadii.Add(candidateRadius);
            spawnedIslands++;

            if (decorationPrefabs != null && decorationPrefabs.Length > 0)
                PlaceDecorations(island);
        }
    }

    bool OverlapsExisting(Vector3 center, float radius)
    {
        for (int i = 0; i < placedCenters.Count; i++)
        {
            Vector3 d = placedCenters[i] - center;
            float minDist = placedRadii[i] + radius;
            if (d.sqrMagnitude < minDist * minDist) return true;
        }
        return false;
    }

    float GetPrefabHorizontalRadius(GameObject prefab)
    {
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return 1f;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return Mathf.Max(bounds.extents.x, bounds.extents.z);
    }

    bool OverlapsDecorations(Vector3 center, float footprintRadius, List<Vector3> centers, List<float> radii)
    {
        for (int i = 0; i < centers.Count; i++)
        {
            float dx = centers[i].x - center.x;
            float dz = centers[i].z - center.z;
            float minDist = radii[i] + footprintRadius + decorationSpacingPadding;
            if (dx * dx + dz * dz < minDist * minDist) return true;
        }
        return false;
    }

    bool FootprintOverhangs(Vector3 center, float footprintRadius, HashSet<Collider> islandColliderSet)
    {
        if (footprintRadius <= 0.001f) return false;

        const int samples = 8;
        for (int i = 0; i < samples; i++)
        {
            float a = (i / (float)samples) * Mathf.PI * 2f;
            Vector3 edge = center + new Vector3(Mathf.Cos(a) * footprintRadius, 0f, Mathf.Sin(a) * footprintRadius);
            Vector3 origin = edge + Vector3.up * raycastHeight;
            if (!RaycastIsland(origin, islandColliderSet, out _)) return true;
        }
        return false;
    }

    bool RaycastIsland(Vector3 origin, HashSet<Collider> islandColliderSet, out RaycastHit hit)
    {
        hit = default;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, raycastHeight * 2f, islandLayerMask);
        bool found = false;
        float closest = float.MaxValue;
        foreach (var h in hits)
        {
            if (!islandColliderSet.Contains(h.collider)) continue;
            if (h.distance < closest) { closest = h.distance; hit = h; found = true; }
        }
        return found;
    }

    void PlaceDecorations(GameObject island)
    {
        Vector3 islandCenter = island.transform.position;

        // Derive radius from the island's combined renderer bounds
        Renderer[] renderers = island.GetComponentsInChildren<Renderer>();
        float radius = decorationRadius;
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        }

        // Collect all colliders that belong to the island itself before any decorations are added
        Collider[] islandColliders = island.GetComponentsInChildren<Collider>();
        HashSet<Collider> islandColliderSet = new HashSet<Collider>(islandColliders);

        // Density scales with island size: bigger islands get proportionally more decorations
        int attemptCount = Mathf.Clamp(Mathf.RoundToInt(radius * decorationsPerRadius), 1, maxDecorationsPerIsland);

        // Track placed decorations on this island so new ones don't overlap them
        List<Vector3> decoCenters = new List<Vector3>();
        List<float> decoRadii = new List<float>();

        for (int i = 0; i < attemptCount; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float r = Mathf.Sqrt(Random.value) * radius;
            float ox = Mathf.Cos(angle) * r;
            float oz = Mathf.Sin(angle) * r;

            Vector3 rayOrigin = islandCenter + new Vector3(ox, raycastHeight, oz);

            if (!RaycastIsland(rayOrigin, islandColliderSet, out RaycastHit hit)) continue;

            // Skip cliff faces
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.5f) continue;
            int objectIndex = Random.Range(0, 2); // 0 or 1
            GameObject prefab = decorationPrefabs[Random.Range(0, decorationPrefabs.Length)];

            float decScale = Random.Range(decorationScaleRange.x, decorationScaleRange.y);

            // Reject placements whose footprint would overhang the island edge.
            // Sample points around the decoration's footprint and require each to land on this island.
            float footprint = GetPrefabHorizontalRadius(prefab) * decScale;
            if (FootprintOverhangs(hit.point, footprint, islandColliderSet)) continue;

            // Reject placements that overlap an already-placed decoration (compared on the horizontal plane)
            if (OverlapsDecorations(hit.point, footprint, decoCenters, decoRadii)) continue;

            Quaternion rot = Quaternion.FromToRotation(Vector3.up, hit.normal)
                           * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject dec = Instantiate(prefab, hit.point, rot, island.transform);
            dec.transform.localScale = prefab.transform.localScale * decScale;

            decoCenters.Add(hit.point);
            decoRadii.Add(footprint);
        }
    }
}
#endif
