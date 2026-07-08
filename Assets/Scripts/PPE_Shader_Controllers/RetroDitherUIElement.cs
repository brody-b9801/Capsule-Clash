using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class RetroDitherUIElement : MonoBehaviour
{
    public Shader ditherUIShader;

    [Range(0, 0.5f)] public float curve = 0f;

    private Material _sharedMaterial;
    private HashSet<Graphic> _trackedGraphics = new HashSet<Graphic>();

    static readonly int ID_ScreenSize = Shader.PropertyToID("_ScreenSize");
    static readonly int ID_Curve      = Shader.PropertyToID("_Curve");

    void OnEnable()
    {
        if (!ditherUIShader)
            ditherUIShader = Shader.Find("UI/RetroDither");

        if (!ditherUIShader)
        {
            Debug.LogWarning("[RetroDitherUIElement] Shader 'UI/RetroDither' not found.", this);
            return;
        }

        _sharedMaterial = new Material(ditherUIShader) { hideFlags = HideFlags.HideAndDontSave };
        ApplyToAllUIElements();
        PushProperties();
    }

    void OnDisable()
    {
        foreach (var g in _trackedGraphics)
            if (g != null) g.material = null;
        _trackedGraphics.Clear();

        if (_sharedMaterial)
        {
            DestroyImmediate(_sharedMaterial);
            _sharedMaterial = null;
        }
    }

    void Update()
    {
        if (!_sharedMaterial) return;
        if (Time.frameCount % 300 == 0)
            ApplyToAllUIElements();
        PushProperties();
    }

    void ApplyToAllUIElements()
    {
        if (!_sharedMaterial) return;
        _trackedGraphics.RemoveWhere(g => g == null);

        foreach (var graphic in FindObjectsOfType<Graphic>(true))
        {
            if (!(graphic is Image || graphic is RawImage)) continue;
            if (_trackedGraphics.Contains(graphic)) continue;
            graphic.material = _sharedMaterial;
            _trackedGraphics.Add(graphic);
        }
    }

    void PushProperties()
    {
        _sharedMaterial.SetVector(ID_ScreenSize, new Vector2(Screen.width, Screen.height));
        _sharedMaterial.SetFloat (ID_Curve,      curve);
    }
}
