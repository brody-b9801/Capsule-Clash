using UnityEngine;

// Drop-in replacements for Alteruna's SyncedAxis / SyncedKey.
//
// Under Alteruna these read from an InputSynchronizable, which replicated a
// remote player's inputs so their movement could be simulated locally.
// Fish-Net does not need that here: PlayerMovement.Update() is gated on IsOwner,
// so only the owning client ever reads these, and position is replicated by a
// NetworkTransform component on the prefab instead of by re-simulating input.
//
// The implicit operators are what let the ~8 call sites stay unchanged --
// `new Vector3(_horizontal, 0, _vertical)` and `if (_jump)` still compile.
//
// If/when movement moves to [Replicate]/[Reconcile] prediction, these get
// replaced by a ReplicateData struct rather than extended.

public readonly struct SyncedAxis
{
    private readonly string _axisName;

    public SyncedAxis(string axisName)
    {
        _axisName = axisName;
    }

    public float Value => string.IsNullOrEmpty(_axisName) ? 0f : Input.GetAxisRaw(_axisName);

    public static implicit operator float(SyncedAxis axis) => axis.Value;

    // Supports `CameraZoom.moving = (_horizontal || _vertical);`
    public static implicit operator bool(SyncedAxis axis) => Mathf.Abs(axis.Value) > 0.001f;
}

public readonly struct SyncedKey
{
    public enum KeyMode
    {
        Key,     // held down  -> Input.GetKey
        KeyDown, // pressed this frame -> Input.GetKeyDown
        KeyUp    // released this frame -> Input.GetKeyUp
    }

    private readonly KeyCode _key;
    private readonly KeyMode _mode;

    public SyncedKey(KeyCode key, KeyMode mode = KeyMode.Key)
    {
        _key = key;
        _mode = mode;
    }

    public bool Value
    {
        get
        {
            switch (_mode)
            {
                case KeyMode.KeyDown: return Input.GetKeyDown(_key);
                case KeyMode.KeyUp:   return Input.GetKeyUp(_key);
                default:              return Input.GetKey(_key);
            }
        }
    }

    public static implicit operator bool(SyncedKey key) => key.Value;
}
