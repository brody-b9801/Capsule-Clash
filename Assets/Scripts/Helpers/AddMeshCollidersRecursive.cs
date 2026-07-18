using UnityEngine;
using UnityEditor;

public class AddMeshCollidersRecursive : MonoBehaviour
{
    [ContextMenu("Add MeshColliders Recursively")]
    void AddColliders()
    {
        AddToChildren(transform);
        Debug.Log("Finished adding MeshColliders.");
    }

    void AddToChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            MeshFilter mf = child.GetComponent<MeshFilter>();
            if (mf != null)
            {
                MeshCollider mc = child.GetComponent<MeshCollider>();
                if (mc == null)
                    mc = child.gameObject.AddComponent<MeshCollider>();

                mc.sharedMesh = mf.sharedMesh;
                //mc.inflateMesh = false;
                mc.convex = false;

                // Provide Contacts
                mc.providesContacts = true;
            }

            // Recurse
            AddToChildren(child);
        }
    }
}