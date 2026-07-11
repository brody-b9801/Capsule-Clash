using UnityEngine;

public class RetroDither : MonoBehaviour
{
    public Shader ditherShader;
    private Material _mat;
    [Range(2, 16)] public int colorAmount = 4;
    [Range(-1, 1)] public float bias = -0.25f;
    [Range(1, 32)] public int pixelSize = 1;
    public bool pixelate = true;
    [Range(1, 16)] public int ditherScale = 1;
    [Range(0, 0.5f)] public float curve = 0f;
    public float refHeight = 1080f;
    [Range(0, 10000)] public float scanlineSpeed = 50f;
    [Range(0, 10000)] public float scanlineFrequency = 2150f;
    [Range(0, 1)] public float scanlineDarkness = 1f;
    [Range(0, 5)] public float shakeIntensity = 0.5f;
    [Range(0, 500)] public float shakeFrequency = 50f;
    [Range(0, 0.02f)] public float chromaticAberration = 0.002f;
    [Range(0, 1)] public float minHW = 0.1f;
    [Range(0, 1)] public float maxHW = 0.5f;

    [Header("Bloom / Phosphor Glow")]
    public bool bloomEnabled = true;
    [Range(0, 1)] public float bloomThreshold = 0.7f;
    [Range(0.01f, 1f)] public float bloomKnee = 0.3f;
    [Range(0, 3)] public float bloomStrength = 0.3f;
    [Range(0, 3)] public float glowStrength = 0.2f;
    [Range(1, 6)] public int bloomIterations = 3;

    [Header("RGB Subpixel")]
    public bool subpixelEnabled = true;
    [Range(0, 1)] public float subpixelStrength = 0.4f;
    [Range(1, 16)] public float subpixelMaskSize = 3f;
    [Range(0, 1)] public float subpixelBorder = 0.5f;
    [Range(1, 4)] public float subpixelBrightness = 3f;

    [Header("Start Screen")]
    [Tooltip("Shake intensity used before the game starts (higher = more CRT wobble on the title screen).")]
    [Range(0, 5)] public float startScreenShakeIntensity = 1.5f;
    public static bool isStartScreen = false;

    [Header("Teleport Pixelize")]
    [Tooltip("Peak pixel size when teleporting.")]
    [Range(1, 64)] public int teleportPixelSpike = 24;
    [Tooltip("Seconds to hold at peak before decaying.")]
    [Range(0f, 3f)] public float teleportHoldDuration = 0.4f;
    [Tooltip("Speed at which pixels shrink back to normal after the hold.")]
    [Range(0.5f, 20f)] public float teleportPixelDecay = 4f;

    [Header("Shot Shake")]
    public float rifleShakeSpike = 1.5f;
    public float shotgunShakeSpike = 4f;
    public float shakeDecay = 6f;

    public static bool isTeleporting = false;
    public static bool shotFired = false;
    public static bool shotgunFired = false;

    public static bool TeleportPixelizeActive { get; private set; } = false;

    private float _currentPixelSize;
    private float _currentShake;
    private float _teleportHoldTimer = 0f;
    private bool _holding = false;

    void OnEnable()
    {
        if (ditherShader == null)
            Debug.LogError("[RetroDither] ditherShader is not assigned in the inspector; effect will be disabled.", this);
        else if (!ditherShader.isSupported)
            Debug.LogError($"[RetroDither] Shader '{ditherShader.name}' is not supported on this GPU/build; effect will be disabled.", this);
        else
            _mat = new Material(ditherShader);

        _currentPixelSize = pixelSize;
        _currentShake = isStartScreen ? startScreenShakeIntensity : shakeIntensity;
    }

    void OnDisable()
    {
        if (_mat != null)
            DestroyImmediate(_mat);
    }

    void Update()
    {
        if (isTeleporting)
        {
            _currentPixelSize = teleportPixelSpike;
            _teleportHoldTimer = teleportHoldDuration;
            _holding = true;
            isTeleporting = false;
            TeleportPixelizeActive = true;
        }
        else if (_holding)
        {
            _teleportHoldTimer -= Time.deltaTime;
            if (_teleportHoldTimer <= 0f)
                _holding = false;
        }
        else if (_currentPixelSize != pixelSize)
        {
            _currentPixelSize = Mathf.Lerp(_currentPixelSize, pixelSize, Time.deltaTime * teleportPixelDecay);
            if (Mathf.Abs(_currentPixelSize - pixelSize) < 0.01f)
            {
                _currentPixelSize = pixelSize;
                TeleportPixelizeActive = false;
            }
        }

        float baseShake = isStartScreen ? startScreenShakeIntensity : shakeIntensity;

        if (shotgunFired)
        {
            _currentShake = baseShake + shotgunShakeSpike;
            shotgunFired = false;
        }
        else if (shotFired)
        {
            _currentShake = baseShake + rifleShakeSpike;
            shotFired = false;
        }
        else
        {
            _currentShake = Mathf.Lerp(_currentShake, baseShake, Time.deltaTime * shakeDecay);
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_mat == null) { Graphics.Blit(src, dest); return; }

        int activePixelSize = pixelate ? Mathf.RoundToInt(_currentPixelSize) : 1;
        float activeDitherScale = (_holding || _currentPixelSize > pixelSize + 0.5f)
            ? Mathf.Max(1f, _currentPixelSize * 0.5f)
            : ditherScale;

        float scale = src.height / refHeight;
        int cell = Mathf.Max(1, Mathf.RoundToInt(activeDitherScale * scale));
        int block = cell * Mathf.Max(1, Mathf.RoundToInt(activePixelSize * scale / cell));
        int contentW = Mathf.Max(1, Mathf.CeilToInt(src.width / (float)block));
        int contentH = Mathf.Max(1, Mathf.CeilToInt(src.height / (float)block));
        int ditherW = Mathf.Max(1, Mathf.CeilToInt(src.width / (float)cell));
        int ditherH = Mathf.Max(1, Mathf.CeilToInt(src.height / (float)cell));

        _mat.SetFloat("_ColorAmount", colorAmount);
        _mat.SetFloat("_Bias", bias);
        _mat.SetFloat("_ScanlineSpeed", scanlineSpeed);
        _mat.SetFloat("_ScanlineFrequency", scanlineFrequency);
        _mat.SetFloat("_ScanlineDarkness", scanlineDarkness);
        _mat.SetFloat("_Curve", curve);
        _mat.SetFloat("_ShakeIntensity", _currentShake);
        _mat.SetFloat("_ShakeFrequency", shakeFrequency);
        _mat.SetFloat("_ChromaticAberration", chromaticAberration);
        _mat.SetFloat("_RefHeight", refHeight);
        _mat.SetVector("_Resolution", new Vector2(src.width, src.height));
        _mat.SetFloat("_MinHW", minHW);
        _mat.SetFloat("_MaxHW", maxHW);
        _mat.SetVector("_DitherRes", new Vector2(ditherW, ditherH));
        _mat.SetVector("_ContentRes", new Vector2(contentW, contentH));
        _mat.SetFloat("_CellSize", cell);

        _mat.SetFloat("_BloomThreshold", bloomThreshold);
        _mat.SetFloat("_BloomKnee", bloomKnee);
        _mat.SetFloat("_BloomStrength", bloomEnabled ? bloomStrength : 0f);
        _mat.SetFloat("_GlowStrength", bloomEnabled ? glowStrength : 0f);
        _mat.SetFloat("_SubpixelEnabled", subpixelEnabled ? 1f : 0f);
        _mat.SetFloat("_SubpixelStrength", subpixelStrength);
        _mat.SetFloat("_SubpixelMaskSize", subpixelMaskSize);
        _mat.SetFloat("_SubpixelBorder", subpixelBorder);
        _mat.SetFloat("_SubpixelBrightness", subpixelBrightness);

        src.filterMode = FilterMode.Bilinear;
        RenderTexture cur = src;
        while (cur.width / 2 >= contentW && cur.height / 2 >= contentH)
        {
            RenderTexture half = RenderTexture.GetTemporary(cur.width / 2, cur.height / 2, 0, src.format);
            half.filterMode = FilterMode.Bilinear;
            Graphics.Blit(cur, half);
            if (cur != src) RenderTexture.ReleaseTemporary(cur);
            cur = half;
        }

        RenderTexture content = RenderTexture.GetTemporary(contentW, contentH, 0, src.format);
        content.filterMode = FilterMode.Point;
        Graphics.Blit(cur, content);
        if (cur != src) RenderTexture.ReleaseTemporary(cur);

        RenderTexture dithered = RenderTexture.GetTemporary(ditherW, ditherH, 0, src.format);
        dithered.filterMode = FilterMode.Point;
        Graphics.Blit(content, dithered, _mat, 0);

        RenderTexture bloom = null;
        if (bloomEnabled && (bloomStrength > 0f || glowStrength > 0f))
        {
            int bloomW = Mathf.Max(1, contentW / 2);
            int bloomH = Mathf.Max(1, contentH / 2);

            bloom = RenderTexture.GetTemporary(bloomW, bloomH, 0, src.format);
            bloom.filterMode = FilterMode.Bilinear;
            Graphics.Blit(content, bloom, _mat, 2);

            RenderTexture blurTmp = RenderTexture.GetTemporary(bloomW, bloomH, 0, src.format);
            blurTmp.filterMode = FilterMode.Bilinear;
            int iterations = Mathf.Max(1, bloomIterations);
            for (int p = 0; p < iterations; p++)
            {
                _mat.SetVector("_BlurDir", new Vector2(1f, 0f));
                Graphics.Blit(bloom, blurTmp, _mat, 3);
                _mat.SetVector("_BlurDir", new Vector2(0f, 1f));
                Graphics.Blit(blurTmp, bloom, _mat, 3);
            }
            RenderTexture.ReleaseTemporary(blurTmp);
            _mat.SetTexture("_BloomTex", bloom);
        }
        else
        {
            _mat.SetTexture("_BloomTex", Texture2D.blackTexture);
        }
        RenderTexture.ReleaseTemporary(content);

        Graphics.Blit(dithered, dest, _mat, 1);
        RenderTexture.ReleaseTemporary(dithered);
        if (bloom != null) RenderTexture.ReleaseTemporary(bloom);
    }
}
