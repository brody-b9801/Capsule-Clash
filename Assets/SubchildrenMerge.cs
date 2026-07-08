using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubchildrenMerge : MonoBehaviour
{
    [ContextMenu("Merge Subchildren")]
    public void MergeSubchildren()
    {
        Vector3 originalPosition = transform.position;
        transform.position = Vector3.zero;
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combineInstances = new CombineInstance[meshFilters.Length];
        for (int i = 0; i < meshFilters.Length; i++)
        {
            var meshFilter = meshFilters[i];
            combineInstances[i] = new CombineInstance
            {
                mesh = meshFilter.sharedMesh,
                transform = meshFilter.transform.localToWorldMatrix
            };
            meshFilter.gameObject.SetActive(false);
        }
        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances);
        gameObject.AddComponent<MeshFilter>().sharedMesh = combinedMesh;
        transform.position = originalPosition;
    }
}
