using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class ComicWatercolorCamera : MonoBehaviour
{
    public Shader comicShader;
    private Material _mat;
    private Camera   _cam;

    // ─── Screen Curvature ─────────────────────────────────────────────────────
    [Header("Screen Curvature")]
    [Tooltip("0 = flat. ~0.15 = subtle. ~0.3 = realistic CRT bow.")]
    [Range(0f, 0.5f)] public float curveStrength = 0f;

    // ─── Chromatic Aberration ─────────────────────────────────────────────────
    [Header("Chromatic Aberration")]
    [Tooltip("R/B channel UV offset in pixels. 0 = off.")]
    [Range(0f, 20f)] public float chromaStrength = 0f;

    // ─── CRT Shadow Mask ──────────────────────────────────────────────────────
    [Header("CRT Shadow Mask")]
    [Range(0f, 1f)]   public float crtStrength      = 0f;
    [Tooltip("Size of one CRT phosphor cell in screen pixels.")]
    [Range(1f, 32f)]  public float crtPixelSize      = 3f;
    [Tooltip("Sharpness of the subcell border. Higher = harder edge.")]
    [Range(0f, 10f)]  public float crtMaskBorder     = 3f;
    [Tooltip("How strongly the RGB mask colour is blended onto the image.")]
    [Range(0f, 2f)]   public float crtMaskIntensity  = 1f;
    [Tooltip("Staggered = aperture grill (columns offset). Stripe = no stagger.")]
    public bool crtStagger = true;

    // ─── Scanlines ────────────────────────────────────────────────────────────
    [Header("Scanlines")]
    [Range(0f, 1f)]    public float scanlineStrength  = 0f;
    [Tooltip("Number of scanline pairs per screen height.")]
    [Range(10f, 2000f)] public float scanlineFrequency = 240f;
    [Tooltip("Scroll speed. 0 = static.")]
    [Range(0f, 10f)]   public float scanlineSpeed     = 0f;

    // ─── Color Quantization ───────────────────────────────────────────────────
    public enum QuantMode { Off = 0, Luminance = 1, RGB = 2, HueLightness = 3, PaletteTexture = 4 }
    [Header("Color Quantization")]
    [Tooltip("Off = passthrough. Luminance = greyscale steps. RGB = per-channel. HueLightness = palette by hue. PaletteTexture = sample palette strip.")]
    public QuantMode quantMode = QuantMode.Off;
    [Tooltip("Number of discrete colour steps (2 = B&W, higher = more shades).")]
    [Range(2f, 32f)] public float quantColorNum = 4f;
    [Tooltip("1-D palette strip texture (mode PaletteTexture). Sample colour = quantized luminance along X.")]
    public Texture2D paletteTex;
    [Tooltip("Hue-Lightness palette — up to 8 colours. Used in HueLightness mode.")]
    public Color[] hlPalette = new Color[]
    {
        Color.black, Color.white, new Color(0.8f,0.2f,0.1f), new Color(0.2f,0.5f,0.9f)
    };

    // ─── Artistic Dithering ───────────────────────────────────────────────────
    [Header("Artistic Dithering")]
    [Range(0f, 1f)] public float ditherStrength = 0f;
    [Tooltip("Pixel noise amplitude.")]
    [Range(0f, 2f)] public float ditherSpread   = 0.25f;

    public enum DitherPattern { Bayer4 = 0, Bayer8 = 1, HashCluster = 2, AnimatedGrain = 3, Texture = 4 }
    public DitherPattern ditherPattern = DitherPattern.Bayer8;

    [Tooltip("Custom dither texture (Pattern = Texture). Red channel used as threshold.")]
    public Texture2D ditherTexture;
    [Tooltip("Screen pixels per texel of the dither texture.")]
    public Vector2 ditherTexelSize = new Vector2(4f, 4f);

    [Tooltip("Per-channel weights. (1,1,1) = uniform.")]
    public Vector3 ditherChannelWeights = Vector3.one;

    [Range(0f, 1f)] public float ditherShadowBias    = 0f;
    [Range(0f, 1f)] public float ditherHighlightBias = 0f;

    [Tooltip("AnimatedGrain: pattern changes per second.")]
    [Range(0f, 60f)] public float ditherAnimSpeed = 12f;

    [Tooltip("Lock pattern to world XZ — no shimmer on moving objects.")]
    public bool ditherWorldSpace = false;
    [Range(0.1f, 20f)] public float ditherWorldScale = 4f;

    // ─── Crosshatch ───────────────────────────────────────────────────────────
    [Header("Crosshatch")]
    public bool enableCrosshatch    = true;
    public bool crosshatchAffectSky = false;
    public enum CrosshatchFamilies { OneDiagonal = 1, TwoFamilies = 2 }
    public CrosshatchFamilies crosshatchFamilies = CrosshatchFamilies.TwoFamilies;
    [Range(0, 1)]     public float crosshatchStrength  = 1f;
    [Range(10, 200)]  public float crosshatchScale     = 60f;
    [Range(0, 1)]     public float crosshatchThreshold = 0.65f;
    [Range(0.1f, 5f)] public float crosshatchThickness = 2f;
    public Color crosshatchColor = Color.black;

    [Header("Crosshatch Dash")]
    [Range(0f, 0.5f)] public float crosshatchDash        = 0f;
    [Range(0f, 1f)]   public float crosshatchDashGrazing = 0.5f;

    // ─── Posterization ────────────────────────────────────────────────────────
    [Header("Posterization")]
    [Range(2, 16)]    public int   posterize       = 5;
    [Range(0, 2)]     public float colorSaturation = 1.3f;
    [Range(0.5f, 2f)] public float colorContrast   = 1.2f;
    public bool enablePosterize    = true;
    public bool posterizeAffectSky = true;

    // ─── Color Grading ────────────────────────────────────────────────────────
    [Header("Color Grading")]
    public Color colorTint = Color.white;
    [Range(0, 1)] public float tintStrength = 0f;

    // ─── ASCII ────────────────────────────────────────────────────────────────
    [Header("ASCII Filter")]
    public Texture2D asciiAtlas;
    [Range(0, 1)]    public float asciiStrength = 0f;
    [Range(1, 32)]   public float asciiCellSize = 8f;
    public Color asciiInkColor = Color.white;
    public Color asciiBgColor  = Color.black;
    public enum AsciiColorMode { Fixed = 0, Scene = 1, Neon = 2, GreenTerminal = 3 }
    public AsciiColorMode asciiColorMode = AsciiColorMode.Neon;

    // ─── Debug ────────────────────────────────────────────────────────────────
    public enum DebugMode { None = 0, WorldPos = 1, Luminance = 3, Depth = 4 }
    [Header("Debug")]
    public DebugMode debugMode = DebugMode.None;

    // ─────────────────────────────────────────────────────────────────────────

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        if (!comicShader) comicShader = Shader.Find("Hidden/ComicBookPost");
        _mat           = new Material(comicShader);
        _mat.hideFlags = HideFlags.HideAndDontSave;
        _cam.depthTextureMode |= DepthTextureMode.Depth;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (!_mat || !_cam || !_mat.shader || !_mat.shader.isSupported)
        {
            Graphics.Blit(src, dst);
            return;
        }

        Matrix4x4 gpuProj     = GL.GetGPUProjectionMatrix(_cam.projectionMatrix, false);
        Matrix4x4 invViewProj = (gpuProj * _cam.worldToCameraMatrix).inverse;
        _mat.SetMatrix("_InvViewProj",   invViewProj);
        _mat.SetVector("_CameraForward", _cam.transform.forward);
        _mat.SetFloat ("_DebugMode",     (float)debugMode);

        // Curvature
        _mat.SetFloat("_CurveStrength", curveStrength);

        // Chromatic aberration
        _mat.SetFloat("_ChromaStrength", chromaStrength);

        // CRT mask
        _mat.SetFloat("_CRTStrength",     crtStrength);
        _mat.SetFloat("_CRTPixelSize",    crtPixelSize);
        _mat.SetFloat("_CRTMaskBorder",   crtMaskBorder);
        _mat.SetFloat("_CRTMaskIntensity",crtMaskIntensity);
        _mat.SetFloat("_CRTStagger",      crtStagger ? 1f : 0f);

        // Scanlines
        _mat.SetFloat("_ScanlineStrength",  scanlineStrength);
        _mat.SetFloat("_ScanlineFrequency", scanlineFrequency);
        _mat.SetFloat("_ScanlineSpeed",     scanlineSpeed);

        // Posterization
        _mat.SetFloat("_Posterize",          enablePosterize ? posterize       : 256f);
        _mat.SetFloat("_ColorSaturation",    enablePosterize ? colorSaturation : 1f);
        _mat.SetFloat("_ColorContrast",      enablePosterize ? colorContrast   : 1f);
        _mat.SetFloat("_PosterizeAffectSky", posterizeAffectSky ? 1f : 0f);
        _mat.SetColor("_ColorTint",          colorTint);
        _mat.SetFloat("_TintStrength",       tintStrength);

        _mat.SetFloat("_Pixelate",  0f);
        _mat.SetFloat("_PixelSize", 1f);

        // Color quantization
        _mat.SetFloat("_QuantMode",     (float)quantMode);
        _mat.SetFloat("_QuantColorNum", quantColorNum);
        if (paletteTex != null) _mat.SetTexture("_PaletteTex", paletteTex);

        // HL palette — pack up to 8 entries into float4 array
        int hlCount = Mathf.Min(hlPalette != null ? hlPalette.Length : 0, 8);
        _mat.SetFloat("_HLPaletteCount", hlCount);
        for (int pi = 0; pi < 8; pi++)
        {
            Color c = (pi < hlCount) ? hlPalette[pi] : Color.black;
            // Store hue in .x via HSV conversion for the shader to read
            float h, s, v;
            Color.RGBToHSV(c, out h, out s, out v);
            _mat.SetVector("_HLPalette[" + pi + "]", new Vector4(h, c.r, c.g, c.b));
        }

        // Dithering
        _mat.SetFloat ("_DitherStrength",       ditherStrength);
        _mat.SetFloat ("_DitherSpread",         ditherSpread);
        _mat.SetFloat ("_DitherPattern",        (float)ditherPattern);
        _mat.SetVector("_DitherChannelWeights", ditherChannelWeights);
        _mat.SetFloat ("_DitherShadowBias",     ditherShadowBias);
        _mat.SetFloat ("_DitherHighlightBias",  ditherHighlightBias);
        _mat.SetFloat ("_DitherAnimSpeed",      ditherAnimSpeed);
        _mat.SetFloat ("_DitherWorldSpace",     ditherWorldSpace ? 1f : 0f);
        _mat.SetFloat ("_DitherWorldScale",     ditherWorldScale);
        if (ditherTexture != null) _mat.SetTexture("_DitherTex", ditherTexture);
        _mat.SetVector("_DitherTexelSize",      ditherTexelSize);

        // Crosshatch
        _mat.SetFloat("_CrosshatchStrength",    enableCrosshatch ? crosshatchStrength : 0f);
        _mat.SetFloat("_CrosshatchScale",       crosshatchScale);
        _mat.SetFloat("_CrosshatchThresh",      crosshatchThreshold);
        _mat.SetFloat("_CrosshatchThickness",   crosshatchThickness);
        _mat.SetColor("_CrosshatchColor",       crosshatchColor);
        _mat.SetFloat("_CrosshatchAffectSky",   crosshatchAffectSky ? 1f : 0f);
        _mat.SetFloat("_CrosshatchFamilies",    (float)crosshatchFamilies);
        _mat.SetFloat("_CrosshatchDash",        crosshatchDash);
        _mat.SetFloat("_CrosshatchDashGrazing", crosshatchDashGrazing);

        // ASCII
        if (asciiAtlas != null)
        {
            _mat.SetTexture("_AsciiAtlas",     asciiAtlas);
            _mat.SetFloat  ("_AsciiAtlasCols", 16f);
            _mat.SetFloat  ("_AsciiAtlasRows", Mathf.CeilToInt(95f / 16f));
        }
        _mat.SetFloat("_AsciiStrength",  asciiStrength);
        _mat.SetFloat("_AsciiCellSize",  asciiCellSize);
        _mat.SetColor("_AsciiInkColor",  asciiInkColor);
        _mat.SetColor("_AsciiBgColor",   asciiBgColor);
        _mat.SetFloat("_AsciiColorMode", (float)asciiColorMode);

        Graphics.Blit(src, dst, _mat);
    }

    void OnDisable()
    {
        if (_mat) DestroyImmediate(_mat);
        _mat = null;
    }
}
