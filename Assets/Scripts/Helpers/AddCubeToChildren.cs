using UnityEngine;

public class AddCubeToChildren : MonoBehaviour
{
    void Start() {
        AddCubeMeshToChildren();
    }
    void AddCubeMeshToChildren()
    {
        foreach (Transform child in transform)
        {
            if (child.Find("CubeChild") != null)
            {
                continue;
            }

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            cube.name = "CubeChild";

            cube.transform.SetParent(child);

            cube.transform.localPosition = Vector3.zero;
            cube.transform.localRotation = Quaternion.identity;

            Renderer childRenderer = child.GetComponent<Renderer>();
            if (childRenderer != null)
            {
                Vector3 childSize = childRenderer.bounds.size;
                cube.transform.localScale = childSize;
            }
            else
            {
                cube.transform.localScale = Vector3.one; // Default scale if no Renderer is found
            }
        }
    }
}
