using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ClosedMazeGenerator : MonoBehaviour
{
    [Header("Maze Size")]
    public int width = 20;
    public int height = 20;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Settings")]
    public bool autoCalculateCellSize = true;
    public float manualCellSize = 4f;
    public int seed = 0;
    public bool randomSeed = true;

    float cellSize;

    MazeCell[,] grid;

    // =============================

    public void Generate()
    {
        ClearChildren();

        if (wallPrefab == null)
        {
            Debug.LogError("Assign a wall prefab.");
            return;
        }

        if (randomSeed)
            seed = Random.Range(0, 999999);

        Random.InitState(seed);

        cellSize = autoCalculateCellSize
            ? GetWallLength()
            : manualCellSize;

        GenerateGrid();
        GenerateMaze(0, 0);
        BuildMaze();
    }

    // =============================

    float GetWallLength()
    {
        Renderer r = wallPrefab.GetComponentInChildren<Renderer>();
        if (r == null)
        {
            Debug.LogError("Wall prefab has no Renderer.");
            return 4f;
        }

        // Use largest axis (x or z)
        return Mathf.Max(r.bounds.size.x, r.bounds.size.z);
    }

    void GenerateGrid()
    {
        grid = new MazeCell[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            grid[x, y] = new MazeCell();
            for (int i = 0; i < 4; i++)
                grid[x, y].walls[i] = true;
        }
    }

    void GenerateMaze(int startX, int startY)
    {
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startX, startY));
        grid[startX, startY].visited = true;

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Peek();
            List<Vector2Int> neighbors = GetUnvisitedNeighbors(current.x, current.y);

            if (neighbors.Count > 0)
            {
                Vector2Int chosen = neighbors[Random.Range(0, neighbors.Count)];
                RemoveWall(current.x, current.y, chosen.x, chosen.y);
                grid[chosen.x, chosen.y].visited = true;
                stack.Push(chosen);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(int x, int y)
    {
        List<Vector2Int> list = new List<Vector2Int>();

        if (y + 1 < height && !grid[x, y + 1].visited)
            list.Add(new Vector2Int(x, y + 1));

        if (x + 1 < width && !grid[x + 1, y].visited)
            list.Add(new Vector2Int(x + 1, y));

        if (y - 1 >= 0 && !grid[x, y - 1].visited)
            list.Add(new Vector2Int(x, y - 1));

        if (x - 1 >= 0 && !grid[x - 1, y].visited)
            list.Add(new Vector2Int(x - 1, y));

        return list;
    }

    void RemoveWall(int x1, int y1, int x2, int y2)
    {
        if (x1 == x2)
        {
            if (y1 < y2)
            {
                grid[x1, y1].walls[0] = false;
                grid[x2, y2].walls[2] = false;
            }
            else
            {
                grid[x1, y1].walls[2] = false;
                grid[x2, y2].walls[0] = false;
            }
        }
        else
        {
            if (x1 < x2)
            {
                grid[x1, y1].walls[1] = false;
                grid[x2, y2].walls[3] = false;
            }
            else
            {
                grid[x1, y1].walls[3] = false;
                grid[x2, y2].walls[1] = false;
            }
        }
    }

void BuildMaze()
{
    Vector3 origin = transform.position;

    for (int x = 0; x < width; x++)
    for (int y = 0; y < height; y++)
    {
        Vector3 cell = origin + new Vector3(x * cellSize, 0, y * cellSize);

        if (floorPrefab != null)
            InstantiatePrefab(floorPrefab, cell, Quaternion.identity);

        // NORTH wall (horizontal, extends along X)
        if (grid[x, y].walls[0])
        {
            Vector3 pos = origin + new Vector3(
                x * cellSize,
                0,
                (y + 1) * cellSize
            );

            InstantiatePrefab(wallPrefab, pos, Quaternion.identity);
        }

        // WEST wall (vertical, rotate 90 so it extends along Z)
        if (grid[x, y].walls[3])
        {
            Vector3 pos = origin + new Vector3(
                x * cellSize,
                0,
                y * cellSize
            );

            InstantiatePrefab(wallPrefab, pos, Quaternion.Euler(0, 90, 0));
        }
    }

    // EAST border (vertical)
    for (int y = 0; y < height; y++)
    {
        Vector3 pos = origin + new Vector3(
            width * cellSize,
            0,
            y * cellSize
        );

        InstantiatePrefab(wallPrefab, pos, Quaternion.Euler(0, 90, 0));
    }

    // SOUTH border (horizontal)
    for (int x = 0; x < width; x++)
    {
        Vector3 pos = origin + new Vector3(
            x * cellSize,
            0,
            0
        );

        InstantiatePrefab(wallPrefab, pos, Quaternion.identity);
    }
}

    void InstantiatePrefab(GameObject prefab, Vector3 pos, Quaternion rot)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
            PrefabUtility.InstantiatePrefab(prefab, transform);
#endif
        GameObject obj = Instantiate(prefab, pos, rot, transform);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
    }

    void ClearChildren()
    {
        while (transform.childCount > 0)
        {
#if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(0).gameObject);
#else
            Destroy(transform.GetChild(0).gameObject);
#endif
        }
    }

    class MazeCell
    {
        public bool visited = false;
        public bool[] walls = new bool[4];
    }
}