using UnityEngine;

public class AddMeshColliders : MonoBehaviour
{
    void Start()
    {
        AddCollidersRecursively(transform);
    }
    private void AddCollidersRecursively(Transform parent)
    {
        parent.transform.position = new Vector3(parent.transform.position.x, -10.09f, parent.transform.position.z);
    }
    
}
