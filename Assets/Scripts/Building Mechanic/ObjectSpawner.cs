using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Alteruna;
using Unity.VisualScripting;

public class ObjectSpawner : AttributesSync
{
    [SerializeField] private Alteruna.Avatar avatar;
    private static Spawner _spawner;
    [SerializeField] private Transform player;
    public static List<GameObject> playerSpawnedObjects;
    private float gridSize = 5f;

    public static float buildNum = 25;
    public static GameObject breakParticles;
    public GameObject breakParticlesRef;
    public GameObject ground;
    public static bool checkSupportBool = true;
    public static List<GameObject> unsupportedObjects;
    private static ObjectSpawner Instance;
    private static HashSet<Vector3> instantiatedParticles = new HashSet<Vector3>();


    private void Awake()
    {
        StartCoroutine(checkSupport());
        _spawner = GameObject.FindGameObjectWithTag("NetworkManager").GetComponent<Spawner>();
        playerSpawnedObjects = new List<GameObject>();
        breakParticles = breakParticlesRef;
        BuildUI.objectSpawner = this;
        Instance = this;
    }

    private void OnDrawGizmos()
    {
        // Draw the grid in the Scene view
        Gizmos.color = Color.gray;

        for (float x = -10; x < 10; x += gridSize)
        {
            for (float z = -10; z < 10; z += gridSize)
            {
                Vector3 pos = new Vector3(x, 0, z);
                Gizmos.DrawWireCube(pos, new Vector3(gridSize, 0.1f, gridSize));
            }
        }
    }

    private void Update()
    {
        if (avatar.IsOwner && PlayerMovement.currDimension != "Maze")
        {
            var spawned = playerSpawnedObjects;

            if (Input.GetKey((KeyCode)SettingsController.floorKey) && buildNum > 0) {
                SpawnFloor();
            }

            if (Input.GetKey((KeyCode)SettingsController.wallKey) && buildNum > 0)
            {
                SpawnWall();
            }

            if (Input.GetKey((KeyCode)SettingsController.rampKey) && buildNum > 0)
            {
                SpawnRamp();
            }

            if (Input.GetKeyDown((KeyCode)SettingsController.breakKey))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 5)) {
                    GameObject hitObject = hit.collider.gameObject;

                    BuildHealth buildHealth = hitObject.GetComponent<BuildHealth>();

                    if (buildHealth != null)
                    {
                        for (int i = 0; i < 4; i++)
                            if (buildHealth != null)
                                buildHealth.TakeDamage(false, 0);
                    }
                }
            }
        }
            //if (avatar.Owner.IsHost) {
                //string serializedData = SerializeGameObjectList(playerSpawnedObjects);
                //BroadcastRemoteMethod(0, serializedData);
            //}
            /*if (spawned.Count >= 6)
            {
                _spawner.Despawn(spawned.ElementAt(0).Item1);
                spawned.RemoveAt(0);
            }*/
    }

    [SynchronizableMethod]
    public void spawnedSync(string json) {

    }

public string SerializeGameObject(GameObject gameObject)
{
    GameObjectState state = new GameObjectState(gameObject);
    return JsonUtility.ToJson(state, true);
}

public GameObjectState DeserializeGameObjectState(string json)
{
    return JsonUtility.FromJson<GameObjectState>(json);
}

    Vector3 GetGridPosition(Vector3 position, string type)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        float z = Mathf.Round(position.z / gridSize) * gridSize;

        if (type == "Floor")
        {
            // Align ramps with the grid and slightly higher to be intuitive
            y = Mathf.Floor(position.y / gridSize) * gridSize + gridSize / 2;
        }

        return new Vector3(x, y, z);
    }

    bool IsPositionOccupied(Vector3 position, string type, Vector3? offset = null)
    {
        Vector3 finalPosition = position;
        if (offset.HasValue)
        {
            finalPosition += offset.Value;
        }

        foreach (var obj in playerSpawnedObjects)
        {
            if (obj != null && obj.transform.position == finalPosition && obj.tag == type)
            {
                return true;
            }
        }
        return false;
    }

void SpawnFloor()
{
    if (avatar.IsOwner)
    {
        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, gridSize * 0.8f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Floor");
            } else {
                spawnPosition = GetGridPosition(Camera.main.transform.position + Camera.main.transform.forward * (gridSize * 0.8f), "Floor");
            }
        }
        else
        {
            spawnPosition = GetGridPosition(Camera.main.transform.position + Camera.main.transform.forward * (gridSize * 0.8f), "Floor");
        }

        if (!IsPositionOccupied(spawnPosition, "Floor") && IsValidPlacement(spawnPosition, new Vector3(gridSize, 0.2f, gridSize), Quaternion.identity, "Floor"))
        {
            buildNum--;
            Vector3 spawnOffset = new Vector3(90f, 0f, 0f);
            Quaternion spawnRotation =  Quaternion.LookRotation(Camera.main.transform.forward);
            spawnRotation.eulerAngles = spawnOffset;

            GameObject floor = _spawner.Spawn(3, spawnPosition, spawnRotation, new Vector3(gridSize, gridSize, gridSize));
            //CollisionControl.addScript(floor);
            floor.tag = "Floor";
            spawned.Add(floor);
            checkSupportBool = true;
            //instantiatedParticles.Remove(spawnPosition);
        }
    }
}


void SpawnWall()
{
    if (avatar.IsOwner)
    {
        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, gridSize * 0.25f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Wall");
            } else {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0f, "Wall");
            }

        }
        else
        {
            spawnPosition = GetGridPosition(Camera.main.transform.position + Camera.main.transform.forward * (gridSize * 0.25f), "Wall");
        }

        Quaternion spawnRotation = Quaternion.LookRotation(Camera.main.transform.forward);
        spawnRotation.eulerAngles = new Vector3(0, Mathf.Round(spawnRotation.eulerAngles.y / 90) * 90, 0);

        Vector3 spawnOffset = spawnRotation * new Vector3(0f, 0f, gridSize / 2);
        Vector3 finalPosition = spawnPosition + spawnOffset;

        if (!IsPositionOccupied(spawnPosition, "Wall", spawnOffset) && IsValidPlacement(finalPosition, new Vector3(gridSize, gridSize, 0.2f), spawnRotation, "Wall"))
        {
            buildNum--;
            GameObject wall = _spawner.Spawn(2, finalPosition, spawnRotation, new Vector3(gridSize, gridSize, gridSize));
            //CollisionControl.addScript(wall);
            wall.tag = "Wall";
            spawned.Add(wall);
            checkSupportBool = true;
            //instantiatedParticles.Remove(spawnPosition);
        }
    }
}

void SpawnRamp()
{
    if (avatar.IsOwner)
    {
        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        Vector3 rayOrigin = Camera.main.transform.position;
        Vector3 rayDirection = Camera.main.transform.forward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, gridSize * 0.5f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Ramp");
            } else {
                spawnPosition = GetGridPosition(Camera.main.transform.position + Camera.main.transform.forward * (gridSize * 0.5f), "Ramp");
            }
        }
        else
        {
            spawnPosition = GetGridPosition(Camera.main.transform.position + Camera.main.transform.forward * (gridSize * 0.5f), "Ramp");
        }

        Quaternion spawnRotation = Quaternion.LookRotation(Camera.main.transform.forward);
        spawnRotation.eulerAngles = new Vector3(45f, Mathf.Round(spawnRotation.eulerAngles.y / 90) * 90, 0);

        Vector3 spawnOffset = spawnRotation * new Vector3(0f, 0f, gridSize / 2);

        if (!IsPositionOccupied(spawnPosition, "Ramp") && IsValidPlacement(spawnPosition, new Vector3(gridSize, gridSize * Mathf.Sqrt(2), 0.2f), spawnRotation, "Ramp"))
        {
            buildNum--;
            GameObject ramp = _spawner.Spawn(0, spawnPosition, spawnRotation, new Vector3(gridSize, gridSize * Mathf.Sqrt(2), gridSize));
            ramp.tag = "Ramp";
            //CollisionControl.addScript(ramp);
            spawned.Add(ramp);
            checkSupportBool = true;
            //instantiatedParticles.Remove(spawnPosition);
        }
    }
}

bool IsValidPlacement(Vector3 spawnPosition, Vector3 size, Quaternion rot, String type)
{
    Collider[] colliders = Physics.OverlapBox(spawnPosition, size / 2, rot, LayerMask.GetMask("Default", "BuildNoColPlayer"));
    if (colliders.Length > 0)
    {
        return true;
    }
    float yHeight = 0;
    if (type == "Ramp") {
        yHeight = gridSize/2;
    } else if (type == "Wall") {
        yHeight = gridSize/2;
    } else if (type == "Floor") {
        yHeight = 0f;
    }
    RaycastHit hit;
    if (Physics.Raycast(spawnPosition, Vector3.down, out hit, yHeight+gridSize/100f, LayerMask.GetMask("Default", "BuildNoColPlayer"))) {
        if (!IsBuildable(hit.transform.gameObject)) {
            return true;
        }
    }

    if (colliders.Length == 0) {
        return false;
    }

    return true;

}

IEnumerator checkSupport() {
    yield return new WaitForSeconds(0.2f);

    if (checkSupportBool && playerSpawnedObjects != null) {
        playerSpawnedObjects.Clear();
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Wall"));
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Floor"));

        // Single-pass flood-fill: start from ground-touching pieces, mark all reachable as supported
        HashSet<GameObject> supported = new HashSet<GameObject>();
        Queue<GameObject> frontier = new Queue<GameObject>();

        // Step 1: Find all pieces directly touching non-buildable ground
        foreach (var obj in playerSpawnedObjects) {
            if (obj == null) continue;
            if (IsTouchingGround(obj)) {
                supported.Add(obj);
                frontier.Enqueue(obj);
            }
        }

        // Step 2: BFS outward from ground-touching pieces
        // Build a set for fast "is this a build piece?" lookup
        HashSet<GameObject> allPieces = new HashSet<GameObject>(playerSpawnedObjects);

        while (frontier.Count > 0) {
            GameObject current = frontier.Dequeue();
            if (current == null) continue;

            float yDim = (current.CompareTag("Ramp")) ? gridSize * Mathf.Sqrt(2) : gridSize;
            Collider[] neighbors = Physics.OverlapBox(
                current.transform.position,
                new Vector3(gridSize, yDim, 0.2f) / 2,
                current.transform.rotation,
                LayerMask.GetMask("Default", "BuildNoColPlayer"));

            foreach (Collider col in neighbors) {
                GameObject neighbor = ResolveBuildRoot(col.gameObject, allPieces);
                if (neighbor != null && neighbor != current && !supported.Contains(neighbor)) {
                    supported.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }
        }

        // Step 3: Everything not in supported set is unsupported.
        // Skip pieces still playing their build animation — their collider is
        // mid-animation and not yet at its grid resting position, so the overlap
        // probes above can spuriously miss it. They get re-evaluated once settled.
        unsupportedObjects = new List<GameObject>();
        foreach (var obj in playerSpawnedObjects) {
            if (obj != null && !supported.Contains(obj) && IsSettled(obj))
                unsupportedObjects.Add(obj);
        }

        if (unsupportedObjects.Count > 0) {
            StartCoroutine(DestroyObjectsSequentially(unsupportedObjects, playerSpawnedObjects));
        } else {
            checkSupportBool = false;
        }
    }
    StartCoroutine(checkSupport());
}

bool IsSettled(GameObject obj) {
    WallFinished wf = obj.GetComponent<WallFinished>();
    if (wf == null) wf = obj.GetComponentInChildren<WallFinished>(true);
    if (wf == null) return true;
    return wf.IsSettled;
}

GameObject ResolveBuildRoot(GameObject hit, HashSet<GameObject> allPieces) {
    Transform t = hit.transform;
    while (t != null) {
        if (allPieces.Contains(t.gameObject))
            return t.gameObject;
        t = t.parent;
    }
    return null;
}

bool IsTouchingGround(GameObject obj) {
    float yHeight = 0;
    if (obj.CompareTag("Ramp") || obj.CompareTag("Wall"))
        yHeight = gridSize / 2;

    RaycastHit hit;
    if (Physics.Raycast(obj.transform.position, Vector3.down, out hit, yHeight + gridSize / 100f, LayerMask.GetMask("Default", "BuildNoColPlayer"))) {
        if (!IsBuildable(hit.transform.gameObject))
            return true;
    }
    float yDim = (obj.CompareTag("Ramp")) ? gridSize * Mathf.Sqrt(2) : gridSize;
    Collider[] colliders = Physics.OverlapBox(obj.transform.position, new Vector3(gridSize, yDim, 0.2f) / 2, obj.transform.rotation, LayerMask.GetMask("Default", "BuildNoColPlayer"));
    foreach (Collider col in colliders) {
        if (col.gameObject != obj && !IsBuildable(col.gameObject))
            return true;
    }
    return false;
}

public void DestroyAllBuildsSync() {
    DestroyAllBuilds();
}

public void DestroyAllBuilds()
{
    List<GameObject> playerSpawnedObjectsCopy = playerSpawnedObjects;
    playerSpawnedObjectsCopy.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
    foreach (var obj in playerSpawnedObjectsCopy)
    {
        if (obj != null) {
            _spawner.Despawn(playerSpawnedObjects[0]);
            breakParticlesSync(obj.transform.position, obj.transform.rotation);
            playerSpawnedObjects.RemoveAt(0);
        }
    }
    checkSupportBool = true;
}

IEnumerator DestroyObjectsSequentially(List<GameObject> unsupportedObjects, List<GameObject> spawnedObjects) {
    unsupportedObjects.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
    foreach (GameObject obj in unsupportedObjects) {
        ObjectSpawner.DespawnObject(obj);
        yield return new WaitForSeconds(0.2f);
    }
    checkSupportBool = true;

}

public static void DespawnObject(GameObject obj) {
    if (obj != null) {
        _spawner.Despawn(obj);
        Vector3 position = obj.transform.position;
        Quaternion rotation = obj.transform.rotation;
        breakParticlesSync(obj.transform.position, obj.transform.rotation);
        checkSupportBool = true;
    }
}

public static void breakParticlesSync(Vector3 pos, Quaternion rot)
{
    Instantiate(breakParticles, pos, rot);
}


bool IsBuildable(GameObject obj)
{
    return obj.CompareTag("Ramp") || obj.CompareTag("Wall") || obj.CompareTag("Floor") || obj.CompareTag("Lava");
}


}

[Serializable]
public class GameObjectState
{
    public string name;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public String tag;
    public int layer;

    public GameObjectState(GameObject obj)
    {
        name = obj.name;
        position = obj.transform.position;
        rotation = obj.transform.rotation;
        scale = obj.transform.localScale;
        tag = obj.tag;
        layer = obj.layer;
    }
}

[Serializable]
public class SerializationWrapper<T>
{
    public List<T> items;

    public SerializationWrapper(List<T> items)
    {
        this.items = items;
    }
}
