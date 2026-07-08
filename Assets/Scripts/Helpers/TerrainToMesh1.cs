using UnityEngine;
using System.Collections.Generic;

public class TerrainToMesh : MonoBehaviour
{
    [Header("Terrain Settings")]
    [Tooltip("The terrain to convert to a mesh")]
    public Terrain terrain;
    
    [Header("Mesh Settings")]
    [Tooltip("Resolution of the mesh. Lower = fewer vertices. 1 = full resolution")]
    [Range(1, 32)]
    public int resolution = 1;
    
    [Tooltip("Generate mesh with normals")]
    public bool generateNormals = true;
    
    [Tooltip("Generate mesh with UVs")]
    public bool generateUVs = true;
    
    [Header("Output Settings")]
    [Tooltip("Create a new GameObject with the mesh")]
    public bool createMeshObject = true;
    
    [Tooltip("Save the mesh as an asset")]
    public bool saveMeshAsset = false;
    
    [Tooltip("Path to save the mesh (relative to Assets folder)")]
    public string savePath = "GeneratedMeshes/TerrainMesh.asset";

    public Mesh ConvertTerrainToMesh()
    {
        if (terrain == null)
        {
            Debug.LogError("No terrain assigned!");
            return null;
        }

        TerrainData terrainData = terrain.terrainData;
        
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        Vector3 size = terrainData.size;
        
        int step = resolution;
        int vertexWidth = (width - 1) / step + 1;
        int vertexHeight = (height - 1) / step + 1;
        
        Debug.Log($"Converting terrain: {width}x{height} heightmap to {vertexWidth}x{vertexHeight} mesh");
        
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        
        for (int y = 0; y < height; y += step)
        {
            for (int x = 0; x < width; x += step)
            {
                float xNorm = (float)x / (width - 1);
                float yNorm = (float)y / (height - 1);
                
                float heightValue = terrainData.GetHeight(x, y);
                
                Vector3 vertex = new Vector3(
                    xNorm * size.x,
                    heightValue,
                    yNorm * size.z
                );
                
                vertices.Add(vertex);

                if (generateUVs)
                {
                    uvs.Add(new Vector2(xNorm, yNorm));
                }
            }
        }
        
        for (int y = 0; y < vertexHeight - 1; y++)
        {
            for (int x = 0; x < vertexWidth - 1; x++)
            {
                int topLeft = y * vertexWidth + x;
                int topRight = topLeft + 1;
                int bottomLeft = (y + 1) * vertexWidth + x;
                int bottomRight = bottomLeft + 1;
                
                triangles.Add(topLeft);
                triangles.Add(bottomLeft);
                triangles.Add(topRight);
                
                triangles.Add(topRight);
                triangles.Add(bottomLeft);
                triangles.Add(bottomRight);
            }
        }
        
        Mesh mesh = new Mesh();
        mesh.name = "TerrainMesh";
        
        if (vertices.Count > 65535)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        
        if (generateUVs)
        {
            mesh.SetUVs(0, uvs);
        }
        
        if (generateNormals)
        {
            mesh.RecalculateNormals();
        }
        
        mesh.RecalculateBounds();        
        return mesh;
    }
    public GameObject CreateMeshGameObject(Mesh mesh)
    {
        GameObject meshObject = new GameObject("TerrainMesh");
        meshObject.transform.position = terrain.transform.position;
        meshObject.transform.rotation = terrain.transform.rotation;
        
        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        
        if (terrain.materialTemplate != null)
        {
            meshRenderer.material = terrain.materialTemplate;
        }
        else
        {
            meshRenderer.material = new Material(Shader.Find("Standard"));
        }
        
        MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        
        Debug.Log("Mesh GameObject created");
        return meshObject;
    }
    public void SaveMeshAsAsset(Mesh mesh)
    {
        #if UNITY_EDITOR
        string fullPath = "Assets/" + savePath;
        
        string directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        UnityEditor.AssetDatabase.CreateAsset(mesh, fullPath);
        UnityEditor.AssetDatabase.SaveAssets();
        
        Debug.Log($"Mesh saved to: {fullPath}");
        #else
        Debug.LogWarning("Mesh saving only works in the Unity Editor");
        #endif
    }
    
    [ContextMenu("Convert Terrain to Mesh")]
    public void Execute()
    {
        Mesh mesh = ConvertTerrainToMesh();
        
        if (mesh == null)
        {
            return;
        }
        
        if (createMeshObject)
        {
            CreateMeshGameObject(mesh);
        }
        
        if (saveMeshAsset)
        {
            SaveMeshAsAsset(mesh);
        }
    }
}
