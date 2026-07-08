using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Attach to your UI Camera (or main camera if UI is overlay).
/// Automatically applies the UI/ComicBook shader to ALL Image and RawImage
/// components in the scene. Settings apply globally to all UI elements.
/// 
/// SETUP:
///   1. Add this component to the camera that renders your UI Canvas
///   2. All Image/RawImage components will automatically get the comic shader
///   3. Adjust settings in Inspector — they apply to everything at once
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class ComicUIElement : MonoBehaviour
{
    public Shader comicUIShader;

    // ── Halftone ────────────────────────────────────────────────────────────
    [Header("Halftone")]
    public bool  halftoneEnabled  = true;
    [Range(4,   120)] public float halftoneScale    = 30f;
    [Range(0,   1)]   public float halftoneStrength = 0.6f;
    [Range(0,   90)]  public float halftoneAngle    = 45f;
    [Range(1,   12)]  public float halftoneContrast = 6f;
    [Range(0.1f, 2f)] public float halftoneGamma    = 1.0f;

    // ── Paper Grain ─────────────────────────────────────────────────────────
    [Header("Paper Grain")]
    public bool  paperEnabled  = true;
    [Range(0, 0.4f)]    public float paperStrength = 0.05f;
    [Range(100, 3000)]  public float paperScale    = 1400f;
    public Color paperTint = Color.white;

    // ── Ink Noise ───────────────────────────────────────────────────────────
    [Header("Ink Noise")]
    public bool  inkEnabled  = true;
    [Range(0, 0.5f)]  public float inkStrength = 0.18f;
    [Range(10, 500)]  public float inkScale    = 120f;

    // ── Color Grading ────────────────────────────────────────────────────────
    [Header("Color Grading")]
    [Range(0, 4)] public float contrast   = 1.0f;
    [Range(0, 4)] public float saturation = 1.0f;

    // ── Internals ───────────────────────────────────────────────────────────
    private Material _sharedMaterial;
    private HashSet<Graphic> _trackedGraphics = new HashSet<Graphic>();

    // Shader property IDs (cached for performance)
    static readonly int ID_ScreenSize       = Shader.PropertyToID("_ScreenSize");
    static readonly int ID_HalftoneOn       = Shader.PropertyToID("_HalftoneOn");
    static readonly int ID_HalftoneScale    = Shader.PropertyToID("_HalftoneScale");
    static readonly int ID_HalftoneStrength = Shader.PropertyToID("_HalftoneStrength");
    static readonly int ID_HalftoneAngle    = Shader.PropertyToID("_HalftoneAngle");
    static readonly int ID_HalftoneContrast = Shader.PropertyToID("_HalftoneContrast");
    static readonly int ID_HalftoneGamma    = Shader.PropertyToID("_HalftoneGamma");
    static readonly int ID_PaperOn          = Shader.PropertyToID("_PaperOn");
    static readonly int ID_PaperStrength    = Shader.PropertyToID("_PaperStrength");
    static readonly int ID_PaperScale       = Shader.PropertyToID("_PaperScale");
    static readonly int ID_PaperTint        = Shader.PropertyToID("_PaperTint");
    static readonly int ID_InkOn            = Shader.PropertyToID("_InkOn");
    static readonly int ID_InkStrength      = Shader.PropertyToID("_InkStrength");
    static readonly int ID_InkScale         = Shader.PropertyToID("_InkScale");
    static readonly int ID_Contrast         = Shader.PropertyToID("_Contrast");
    static readonly int ID_Saturation       = Shader.PropertyToID("_Saturation");

    // ────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        if (!comicUIShader)
            comicUIShader = Shader.Find("UI/ComicBook");

        if (!comicUIShader)
        {
            Debug.LogWarning("[ComicUIElement] Shader 'UI/ComicBook' not found. " +
                             "Make sure the shader is in your project.", this);
            return;
        }

        // Create a single shared material for all UI elements
        _sharedMaterial = new Material(comicUIShader) { hideFlags = HideFlags.HideAndDontSave };

        ApplyToAllUIElements();
        PushProperties();
    }

    void OnDisable()
    {
        // Restore all graphics to default material
        foreach (var g in _trackedGraphics)
        {
            if (g != null) g.material = null;
        }
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

        // Re-scan for new UI elements much less frequently
        if (Time.frameCount % 300 == 0) // every ~5 seconds at 60fps
        {
            ApplyToAllUIElements();
        }

        PushProperties();
    }

    // ── Find and apply material to all Image/RawImage in scene ───────────────
    void ApplyToAllUIElements()
    {
        if (!_sharedMaterial) return;

        // Clear dead references
        _trackedGraphics.RemoveWhere(g => g == null);

        // Find all UI graphics in the scene
        var allGraphics = FindObjectsOfType<Graphic>(true); // includes inactive

        foreach (var graphic in allGraphics)
        {
            // Only apply to Image and RawImage (not Text, etc.)
            if (!(graphic is Image || graphic is RawImage))
                continue;

            // Skip if already tracking
            if (_trackedGraphics.Contains(graphic))
                continue;

            // Apply shared material
            graphic.material = _sharedMaterial;
            _trackedGraphics.Add(graphic);
        }
    }

    // ── Push all Inspector values to the shared material ──────────────────────
    void PushProperties()
    {
        // Screen size so the halftone grid matches the PPE pass exactly
        _sharedMaterial.SetVector(ID_ScreenSize, new Vector2(Screen.width, Screen.height));

        // Halftone
        _sharedMaterial.SetFloat(ID_HalftoneOn,       halftoneEnabled ? 1f : 0f);
        _sharedMaterial.SetFloat(ID_HalftoneScale,    halftoneScale);
        _sharedMaterial.SetFloat(ID_HalftoneStrength, halftoneStrength);
        _sharedMaterial.SetFloat(ID_HalftoneAngle,    halftoneAngle);
        _sharedMaterial.SetFloat(ID_HalftoneContrast, halftoneContrast);
        _sharedMaterial.SetFloat(ID_HalftoneGamma,    halftoneGamma);

        // Paper
        _sharedMaterial.SetFloat(ID_PaperOn,       paperEnabled ? 1f : 0f);
        _sharedMaterial.SetFloat(ID_PaperStrength, paperStrength);
        _sharedMaterial.SetFloat(ID_PaperScale,    paperScale);
        _sharedMaterial.SetColor(ID_PaperTint,     paperTint);

        // Ink noise
        _sharedMaterial.SetFloat(ID_InkOn,       inkEnabled ? 1f : 0f);
        _sharedMaterial.SetFloat(ID_InkStrength, inkStrength);
        _sharedMaterial.SetFloat(ID_InkScale,    inkScale);

        // Color grading
        _sharedMaterial.SetFloat(ID_Contrast,   contrast);
        _sharedMaterial.SetFloat(ID_Saturation, saturation);
    }

    // ── Convenience presets (right-click component in Inspector) ─────────────

    [ContextMenu("Preset — Match Scene Classic")]
    public void PresetMatchSceneClassic()
    {
        halftoneEnabled = true;  halftoneScale = 55f;  halftoneStrength = 0.7f;
        halftoneAngle   = 45f;   halftoneContrast = 5f; halftoneGamma = 1.0f;
        paperEnabled    = true;  paperStrength = 0.08f; paperScale = 1200f;
        paperTint       = new Color(0.97f, 0.96f, 0.90f, 1f);
        inkEnabled      = true;  inkStrength = 0.3f;   inkScale = 130f;
        contrast        = 1.1f;  saturation = 1.15f;
    }

    [ContextMenu("Preset — Subtle (HUD bars, icons)")]
    public void PresetSubtle()
    {
        halftoneEnabled = true;  halftoneScale = 25f;  halftoneStrength = 0.4f;
        halftoneAngle   = 45f;   halftoneContrast = 8f; halftoneGamma = 1.3f;
        paperEnabled    = true;  paperStrength = 0.04f; paperScale = 1600f;
        paperTint       = Color.white;
        inkEnabled      = false; inkStrength = 0f;
        contrast        = 1.0f;  saturation = 1.0f;
    }

    [ContextMenu("Preset — Heavy Ink (panels, borders)")]
    public void PresetHeavyInk()
    {
        halftoneEnabled = true;  halftoneScale = 40f;  halftoneStrength = 0.85f;
        halftoneAngle   = 45f;   halftoneContrast = 4f; halftoneGamma = 0.8f;
        paperEnabled    = true;  paperStrength = 0.15f; paperScale = 1000f;
        paperTint       = new Color(0.92f, 0.89f, 0.80f, 1f);
        inkEnabled      = true;  inkStrength = 0.45f;  inkScale = 100f;
        contrast        = 1.3f;  saturation = 0.85f;
    }

    [ContextMenu("Preset — Pop Art")]
    public void PresetPopArt()
    {
        halftoneEnabled = true;  halftoneScale = 35f;  halftoneStrength = 0.9f;
        halftoneAngle   = 45f;   halftoneContrast = 7f; halftoneGamma = 1.0f;
        paperEnabled    = false; paperStrength = 0f;
        paperTint       = Color.white;
        inkEnabled      = false; inkStrength = 0f;
        contrast        = 1.5f;  saturation = 2.0f;
    }
}
