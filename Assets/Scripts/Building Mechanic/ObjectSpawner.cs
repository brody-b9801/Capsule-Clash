using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class ObjectSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject rampPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject floorPrefab;

    [SerializeField] private Transform player;
    public GameObject breakParticlesRef;
    public GameObject ground;

    private float gridSize = 5f;

    /// <summary>
    /// Backed by a SyncVar because builds are consumed on the server but the
    /// counter is displayed by BuildUI on the client; a plain field would drift.
    /// Writes are server-authoritative, so client-side assignments (the respawn
    /// resets in PlayerMovement) only take effect when running as host/server.
    /// </summary>
    private readonly SyncVar<float> _buildNum = new SyncVar<float>(25f);

    public float buildNum
    {
        get => _buildNum.Value;
        set { if (IsServerStarted) _buildNum.Value = value; }
    }

    /// <summary>
    /// Server-only registry of every build currently standing. Rebuilt each
    /// support pass from the tagged objects in the scene, so it is authoritative
    /// on the server and left empty on pure clients.
    ///
    /// Static because its contents are global: the support scan populates it from
    /// FindGameObjectsWithTag (every build, not just this player's), and only the
    /// elected loop owner runs that scan. As a per-instance list, a non-owner's
    /// copy would stay empty and DestroyAllBuilds would silently wipe nothing.
    /// </summary>
    public static List<GameObject> playerSpawnedObjects = new List<GameObject>();

    /// <summary>
    /// Set when the build graph changes and support must be re-evaluated. Static
    /// because the build graph is global: a build placed by one player can unsupport
    /// another's. As a per-instance flag, only the acting player's spawner was
    /// marked dirty, so whichever loop ran first could clear it and the others
    /// would never re-check.
    /// </summary>
    private static bool checkSupportBool = true;
    private List<GameObject> unsupportedObjects = new List<GameObject>();

    /// <summary>
    /// The one spawner that runs the support scan. The scan reads the whole scene
    /// via FindGameObjectsWithTag, so it is identical work no matter which spawner
    /// runs it — with N players every spawner was repeating the same global sweep
    /// (and the same OverlapBox per piece) five times a second.
    /// </summary>
    private static ObjectSpawner _supportLoopOwner;

    public static ObjectSpawner Local { get; private set; }

    private enum BuildType : byte { Floor = 0, Wall = 1, Ramp = 2 }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // The claim set is static and so outlives a session; clear it when the
        // first spawner starts or stale cells would stay blocked forever.
        if (!_claimSetInitialized)
        {
            _claimedCells.Clear();
            playerSpawnedObjects.Clear();
            checkSupportBool = true;
            _claimSetInitialized = true;
        }

        // Exactly one spawner drives the global support scan.
        if (_supportLoopOwner == null)
        {
            _supportLoopOwner = this;
            StartCoroutine(CheckSupportLoop());
        }
    }

    public override void OnStopServer()
    {
        // Hand the scan to another live spawner, otherwise unsupported builds
        // would stop collapsing once the owning player leaves.
        if (_supportLoopOwner == this)
        {
            _supportLoopOwner = null;
            foreach (ObjectSpawner candidate in FindObjectsByType<ObjectSpawner>(FindObjectsSortMode.None))
            {
                if (candidate == this || !candidate.IsServerStarted) continue;
                _supportLoopOwner = candidate;
                candidate.StartCoroutine(candidate.CheckSupportLoop());
                break;
            }
        }
        base.OnStopServer();
    }

    private static bool _claimSetInitialized;

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Both bindings are owner-only: the HUD shows the local player's build
        // count, and Awake previously ran on every player instance, so the last
        // one to spawn pointed the UI at a remote player's spawner.
        if (IsOwner)
        {
            Local = this;
            BuildUI.objectSpawner = this;
        }
    }

    public override void OnStopClient()
    {
        if (Local == this) Local = null;
        if (BuildUI.objectSpawner == this) BuildUI.objectSpawner = null;
        base.OnStopClient();
    }

    /// <summary>
    /// Server-side spawn. Instantiates the prefab and hands it to FishNet, which
    /// replicates it to every observer — no ObserversRpc needed for the build itself.
    /// </summary>
    [Server]
    private GameObject SpawnBuild(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, string tag)
    {
        if (prefab == null) return null;

        GameObject go = Instantiate(prefab, pos, rot);
        go.transform.localScale = scale;
        go.tag = tag;

        if (go.GetComponent<NetworkObject>() != null)
            ServerManager.Spawn(go);

        return go;
    }

    /// <summary>
    /// Server-side despawn. Plays the break effect on all observers first, since
    /// the object is gone by the time the despawn replicates.
    /// </summary>
    [Server]
    private void DespawnBuild(GameObject obj)
    {
        if (obj == null) return;

        // Free the grid cell before the object goes away, or that spot stays
        // permanently unbuildable. This is the single choke point for every
        // removal path (break, unsupported collapse, timed wipe).
        ReleaseCell(obj.transform.position, obj.tag, obj.transform.eulerAngles.y);

        RpcBreakParticles(obj.transform.position, obj.transform.rotation);

        if (obj.TryGetComponent(out NetworkObject nob) && nob.IsSpawned)
            ServerManager.Despawn(obj);
        else
            Destroy(obj);

        checkSupportBool = true;
    }

    /// <summary>Cosmetic only — the particle prefab is not a NetworkObject.</summary>
    [ObserversRpc(RunLocally = true)]
    private void RpcBreakParticles(Vector3 pos, Quaternion rot)
    {
        if (breakParticlesRef != null)
            Instantiate(breakParticlesRef, pos, rot);
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (PlayerMovement.Local == null || PlayerMovement.Local.currDimension == "Maze") return;
        if (Camera.main == null) return;

        Vector3 camPos = Camera.main.transform.position;
        Vector3 camFwd = Camera.main.transform.forward;

        if (buildNum > 0)
        {
            if (Input.GetKey((KeyCode)SettingsController.buildKeys.floorKey))
                CmdSpawnBuild(BuildType.Floor, camPos, camFwd);
            else if (Input.GetKey((KeyCode)SettingsController.buildKeys.wallKey))
                CmdSpawnBuild(BuildType.Wall, camPos, camFwd);
            else if (Input.GetKey((KeyCode)SettingsController.buildKeys.rampKey))
                CmdSpawnBuild(BuildType.Ramp, camPos, camFwd);
        }

        if (Input.GetKeyDown((KeyCode)SettingsController.buildKeys.breakKey))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 5))
            {
                BuildHealth buildHealth = hit.collider.GetComponentInParent<BuildHealth>();
                if (buildHealth != null)
                {
                    for (int i = 0; i < 4; i++)
                        buildHealth.TakeDamage(false, 0);
                }
            }
        }
    }

    /// <summary>
    /// Single entry point from client to server. The camera transform is passed as
    /// plain data; all placement decisions happen server-side.
    /// </summary>
    [ServerRpc]
    private void CmdSpawnBuild(BuildType type, Vector3 cameraPosition, Vector3 cameraForward)
    {
        if (buildNum <= 0) return;

        switch (type)
        {
            case BuildType.Floor: SpawnFloor(cameraPosition, cameraForward); break;
            case BuildType.Wall: SpawnWall(cameraPosition, cameraForward); break;
            case BuildType.Ramp: SpawnRamp(cameraPosition, cameraForward); break;
        }
    }

    Vector3 GetGridPosition(Vector3 position, string type)
    {
        float x = Mathf.Round(position.x / gridSize) * gridSize;
        float y = Mathf.Round(position.y / gridSize) * gridSize;
        float z = Mathf.Round(position.z / gridSize) * gridSize;

        if (type == "Floor")
        {
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

    /// <summary>
    /// Grid cells claimed by builds, shared by every player's spawner on the server.
    /// playerSpawnedObjects is per-instance, so IsPositionOccupied only ever sees
    /// the local player's builds and cannot detect a clash with another player.
    /// Physics.OverlapBox does see everyone, but a collider instantiated earlier in
    /// the same frame is not registered with the physics engine yet — so two builds
    /// requested on the same tick could both pass validation. Claiming a cell here
    /// is immediate and atomic, which closes that window.
    /// </summary>
    private static readonly HashSet<BuildCell> _claimedCells = new HashSet<BuildCell>();

    private readonly struct BuildCell : System.IEquatable<BuildCell>
    {
        private readonly int _x, _y, _z, _yaw;
        private readonly string _type;

        public BuildCell(Vector3 worldPos, string type, float yawDegrees)
        {
            // Quantize to hundredths: float drift would otherwise make two
            // logically identical cells hash differently.
            _x = Mathf.RoundToInt(worldPos.x * 100f);
            _y = Mathf.RoundToInt(worldPos.y * 100f);
            _z = Mathf.RoundToInt(worldPos.z * 100f);
            // Walls and ramps occupy a cell face, so orientation is part of the
            // identity: two walls can share a position on perpendicular faces.
            // Normalized to 0-359 so -90 and 270 are the same cell.
            _yaw = ((Mathf.RoundToInt(yawDegrees / 90f) * 90) % 360 + 360) % 360;
            _type = type;
        }

        public bool Equals(BuildCell other) =>
            _x == other._x && _y == other._y && _z == other._z &&
            _yaw == other._yaw && _type == other._type;

        public override bool Equals(object obj) => obj is BuildCell other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _x;
                hash = hash * 31 + _y;
                hash = hash * 31 + _z;
                hash = hash * 31 + _yaw;
                hash = hash * 31 + (_type?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// Atomically claims a cell. Returns false if another build already holds it,
    /// in which case the caller must abort without spawning or charging a build.
    /// </summary>
    [Server]
    private bool TryClaimCell(Vector3 worldPos, string type, float yawDegrees)
    {
        return _claimedCells.Add(new BuildCell(worldPos, type, yawDegrees));
    }

    [Server]
    private void ReleaseCell(Vector3 worldPos, string type, float yawDegrees)
    {
        _claimedCells.Remove(new BuildCell(worldPos, type, yawDegrees));
    }

[Server]
void SpawnFloor(Vector3 cameraPosition, Vector3 cameraForward)
{
        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        if (Physics.Raycast(cameraPosition, cameraForward, out hit, gridSize * 0.8f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Floor");
            } else {
                spawnPosition = GetGridPosition(cameraPosition + cameraForward * (gridSize * 0.8f), "Floor");
            }
        }
        else
        {
            spawnPosition = GetGridPosition(cameraPosition + cameraForward * (gridSize * 0.8f), "Floor");
        }

        if (!IsPositionOccupied(spawnPosition, "Floor") && IsValidPlacement(spawnPosition, new Vector3(gridSize, 0.2f, gridSize), Quaternion.identity, "Floor"))
        {
            // Floors are flat, so orientation is not part of the cell identity.
            if (!TryClaimCell(spawnPosition, "Floor", 0f)) return;

            Quaternion spawnRotation = Quaternion.LookRotation(cameraForward);
            spawnRotation.eulerAngles = new Vector3(90f, 0f, 0f);

            GameObject floor = SpawnBuild(floorPrefab, spawnPosition, spawnRotation, new Vector3(gridSize, gridSize, gridSize), "Floor");
            //CollisionControl.addScript(floor);
            if (floor == null)
            {
                ReleaseCell(spawnPosition, "Floor", 0f);
                return;
            }

            buildNum--;
            spawned.Add(floor);
            checkSupportBool = true;
        }
}

[Server]
void SpawnWall(Vector3 cameraPosition, Vector3 cameraForward)
{

        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        if (Physics.Raycast(cameraPosition, cameraForward, out hit, gridSize * 0.25f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Wall");
            } else {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0f, "Wall");
            }

        }
        else
        {
            spawnPosition = GetGridPosition(cameraPosition + cameraForward * (gridSize * 0.25f), "Wall");
        }

        Quaternion spawnRotation = Quaternion.LookRotation(cameraForward);
        spawnRotation.eulerAngles = new Vector3(0, Mathf.Round(spawnRotation.eulerAngles.y / 90) * 90, 0);

        Vector3 spawnOffset = spawnRotation * new Vector3(0f, 0f, gridSize / 2);
        Vector3 finalPosition = spawnPosition + spawnOffset;

        if (!IsPositionOccupied(spawnPosition, "Wall", spawnOffset) && IsValidPlacement(finalPosition, new Vector3(gridSize, gridSize, 0.2f), spawnRotation, "Wall"))
        {
            // A wall occupies one face of a cell, so yaw distinguishes the
            // perpendicular wall that legitimately shares this position.
            float yaw = spawnRotation.eulerAngles.y;
            if (!TryClaimCell(finalPosition, "Wall", yaw)) return;

            GameObject wall = SpawnBuild(wallPrefab, finalPosition, spawnRotation, new Vector3(gridSize, gridSize, gridSize), "Wall");
            //CollisionControl.addScript(wall);
            if (wall == null)
            {
                ReleaseCell(finalPosition, "Wall", yaw);
                return;
            }

            buildNum--;
            spawned.Add(wall);
            checkSupportBool = true;
        }

}

[Server]
void SpawnRamp(Vector3 cameraPosition, Vector3 cameraForward)
{

        var spawned = playerSpawnedObjects;
        RaycastHit hit;
        Vector3 spawnPosition;

        Vector3 rayOrigin = cameraPosition;
        Vector3 rayDirection = cameraForward;

        if (Physics.Raycast(rayOrigin, rayDirection, out hit, gridSize * 0.5f))
        {
            if (!(hit.transform.gameObject.tag == "Ramp")) {
                spawnPosition = GetGridPosition(hit.point + hit.normal * 0.05f, "Ramp");
            } else {
                spawnPosition = GetGridPosition(cameraPosition + cameraForward * (gridSize * 0.5f), "Ramp");
            }
        }
        else
        {
            spawnPosition = GetGridPosition(cameraPosition + cameraForward * (gridSize * 0.5f), "Ramp");
        }

        Quaternion spawnRotation = Quaternion.LookRotation(cameraForward);
        spawnRotation.eulerAngles = new Vector3(45f, Mathf.Round(spawnRotation.eulerAngles.y / 90) * 90, 0);

        Vector3 spawnOffset = spawnRotation * new Vector3(0f, 0f, gridSize / 2);

        if (!IsPositionOccupied(spawnPosition, "Ramp") && IsValidPlacement(spawnPosition, new Vector3(gridSize, gridSize * Mathf.Sqrt(2), 0.2f), spawnRotation, "Ramp"))
        {
            // Ramps are directional, so yaw is part of the cell identity.
            float yaw = spawnRotation.eulerAngles.y;
            if (!TryClaimCell(spawnPosition, "Ramp", yaw)) return;

            GameObject ramp = SpawnBuild(rampPrefab, spawnPosition, spawnRotation, new Vector3(gridSize, gridSize * Mathf.Sqrt(2), gridSize), "Ramp");
            //CollisionControl.addScript(ramp);
            if (ramp == null)
            {
                ReleaseCell(spawnPosition, "Ramp", yaw);
                return;
            }

            buildNum--;
            spawned.Add(ramp);
            checkSupportBool = true;
        }
    
}

[Server]
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

/// <summary>
/// Runs for the lifetime of the server on a single elected spawner. Previously
/// this restarted itself with StartCoroutine at the end of its own body, which
/// spawned a new coroutine every pass and grew without bound; it is now a single
/// long-lived loop, and only _supportLoopOwner runs it.
/// </summary>
[Server]
IEnumerator CheckSupportLoop() {
  WaitForSeconds wait = new WaitForSeconds(0.2f);

  while (true) {
    yield return wait;

    // Ownership can transfer on despawn; stop if this instance is no longer it.
    if (_supportLoopOwner != this) yield break;

    if (checkSupportBool && playerSpawnedObjects != null) {
        playerSpawnedObjects.Clear();
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Wall"));
        playerSpawnedObjects.AddRange(GameObject.FindGameObjectsWithTag("Floor"));

        HashSet<GameObject> supported = new HashSet<GameObject>();
        Queue<GameObject> frontier = new Queue<GameObject>();

        foreach (var obj in playerSpawnedObjects) {
            if (obj == null) continue;
            if (IsTouchingGround(obj)) {
                supported.Add(obj);
                frontier.Enqueue(obj);
            }
        }

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

        unsupportedObjects = new List<GameObject>();
        foreach (var obj in playerSpawnedObjects) {
            if (obj != null && !supported.Contains(obj) && IsSettled(obj))
                unsupportedObjects.Add(obj);
        }

        if (unsupportedObjects.Count > 0) {
            yield return DestroyObjectsSequentially(unsupportedObjects);
        } else {
            checkSupportBool = false;
        }
    }
  }
}

[Server]
bool IsSettled(GameObject obj) {
    WallFinished wf = obj.GetComponent<WallFinished>();
    if (wf == null) wf = obj.GetComponentInChildren<WallFinished>(true);
    if (wf == null) return true;
    return wf.IsSettled;
}

[Server]
GameObject ResolveBuildRoot(GameObject hit, HashSet<GameObject> allPieces) {
    Transform t = hit.transform;
    while (t != null) {
        if (allPieces.Contains(t.gameObject))
            return t.gameObject;
        t = t.parent;
    }
    return null;
}

[Server]
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

[Server]
public void DestroyAllBuildsSync() {
    DestroyAllBuilds();
}

[Server]
public void DestroyAllBuilds()
{
    // Copy before iterating: the original assigned the same reference and then
    // mutated it inside the loop, which despawned the wrong objects and threw.
    List<GameObject> toDestroy = new List<GameObject>(playerSpawnedObjects);
    toDestroy.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

    foreach (var obj in toDestroy)
    {
        if (obj != null)
            DespawnBuild(obj);
    }

    playerSpawnedObjects.Clear();
    checkSupportBool = true;
}

[Server]
IEnumerator DestroyObjectsSequentially(List<GameObject> toDestroy) {
    WaitForSeconds wait = new WaitForSeconds(0.2f);
    toDestroy.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));

    foreach (GameObject obj in toDestroy) {
        if (obj == null) continue;
        DespawnBuild(obj);
        playerSpawnedObjects.Remove(obj);
        yield return wait;
    }
    checkSupportBool = true;
}

/// <summary>
/// Static entry point kept for BuildHealth, which calls this from an ObserversRpc
/// on every client. Only the server may actually despawn, so non-server callers
/// are ignored and the server's own despawn replicates to everyone.
/// </summary>
public static void DespawnObject(GameObject obj) {
    if (obj == null) return;
    if (!InstanceFinder.IsServerStarted) return;

    ObjectSpawner spawner = Local != null
        ? Local
        : FindAnyObjectByType<ObjectSpawner>();
    if (spawner == null) return;

    spawner.DespawnBuild(obj);
    playerSpawnedObjects.Remove(obj);
}

[Server]
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
