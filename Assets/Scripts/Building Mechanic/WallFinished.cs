using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallFinished : MonoBehaviour
{
    [Header("Assign the completed wall prefab here")]
    public GameObject completedPrefab;

    private bool isMeshesCombined = false;
    private GameObject[] _children;
    private Animator _anim;
    private GameObject _spawnedWall;
    private MeshRenderer _cubeRenderer;

    public bool IsSettled { get; private set; }

    private void Awake()
    {
        // Animator may be on this object or a parent — store it so we can
        // disable it after the swap to stop Write Defaults from undoing SetActive calls.
        _anim = GetComponentInParent<Animator>(true);

        _children = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            _children[i] = transform.GetChild(i).gameObject;
            if (_children[i].name == "Cube")
                _cubeRenderer = _children[i].GetComponent<MeshRenderer>();
        }

        StartCoroutine(SettleFailsafe());
    }

    private IEnumerator SettleFailsafe()
    {
        yield return new WaitForSeconds(3f);
        IsSettled = true;
    }

    public void OnAnimationComplete()
    {
        IsSettled = true;
        if (isMeshesCombined) return;
        if (completedPrefab == null) return;
        isMeshesCombined = true;
        StartCoroutine(SwapAfterFrame());
    }

    private IEnumerator SwapAfterFrame()
    {
        yield return new WaitForEndOfFrame();

        // Mesh is baked in wf-local space, so place as a child of wf at identity.
        // The Animator is disabled above, so it cannot overwrite these transforms.
        _spawnedWall = Instantiate(completedPrefab, transform);
        _spawnedWall.transform.localPosition = Vector3.zero;
        _spawnedWall.transform.localRotation = Quaternion.identity;
        _spawnedWall.transform.localScale = Vector3.one;
        _spawnedWall.SetActive(true);

        // Disable the Animator before hiding children so Write Defaults
        // cannot re-enable them on the next evaluation tick.
        if (_anim != null) _anim.enabled = false;

        // Keep children active so their colliders stay live for support
        // detection; only hide the visuals (the combined prefab replaces them).
        for (int i = 0; i < _children.Length; i++)
            HideVisualsKeepColliders(_children[i]);
    }

    private void HideVisualsKeepColliders(GameObject go)
    {
        foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    public void MergeAllMeshes()
    {
        if (isMeshesCombined)
        {
            Debug.Log("MergeAllMeshes: already merged, skipping.");
            return;
        }

        if (_anim != null) _anim.enabled = false;

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        List<CombineInstance> combines = new List<CombineInstance>();
        Material mat = null;
        Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.gameObject == gameObject) continue;
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mf.sharedMesh == null) continue;
            if (mat == null && mr.sharedMaterial != null)
                mat = mr.sharedMaterial;

            for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
            {
                combines.Add(new CombineInstance
                {
                    mesh = mf.sharedMesh,
                    subMeshIndex = sub,
                    transform = worldToLocal * mf.transform.localToWorldMatrix
                });
            }
        }

        if (combines.Count == 0)
        {
            Debug.LogWarning("MergeAllMeshes: no meshes found to combine.");
            return;
        }

        Mesh merged = new Mesh();
        merged.CombineMeshes(combines.ToArray(), true, true);
        merged.RecalculateBounds();
        merged.RecalculateNormals();

        for (int i = 0; i < _children.Length; i++)
            HideVisualsKeepColliders(_children[i]);

        _spawnedWall = new GameObject("MergedMesh");
        _spawnedWall.transform.SetParent(transform, false);
        _spawnedWall.AddComponent<MeshFilter>().sharedMesh = merged;
        MeshRenderer renderer = _spawnedWall.AddComponent<MeshRenderer>();
        if (mat != null) renderer.sharedMaterial = mat;
        _spawnedWall.AddComponent<MeshCollider>().sharedMesh = merged;

        isMeshesCombined = true;
    }

    public void UnmergeChildren()
    {
        if (!isMeshesCombined) return;

        if (_spawnedWall != null)
        {
            Destroy(_spawnedWall);
            _spawnedWall = null;
        }

        if (_anim != null) _anim.enabled = true;

        for (int i = 0; i < _children.Length; i++)
        {
            if (_children[i] == null) continue;
            _children[i].SetActive(true);
            foreach (Renderer r in _children[i].GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        isMeshesCombined = false;
    }
}
