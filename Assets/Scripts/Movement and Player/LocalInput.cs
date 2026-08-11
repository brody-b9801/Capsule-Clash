using UnityEngine;

public readonly struct SyncedAxis
{
    private readonly string _axisName;

    public SyncedAxis(string axisName)
    {
        _axisName = axisName;
    }

    public float Value => string.IsNullOrEmpty(_axisName) ? 0f : Input.GetAxisRaw(_axisName);

    public static implicit operator float(SyncedAxis axis) => axis.Value;

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
