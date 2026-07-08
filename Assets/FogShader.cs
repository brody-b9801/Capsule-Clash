using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class FogShader : MonoBehaviour
{
    public Shader fogShader;
    private Material _mat;
    private Camera _cam;

    [Header("Ice Parameters")]
    public bool fogEnabledIce = true;
    public bool affectSkyboxIce = false;
    [Range(0, 1)] public float fogDensityIce = 0.5f;
    public float fogStartIce = 0f;
    public float fogEndIce = 100f;
    public Color fogColorIce = Color.black;
    [Range(0, 1)] public float skyFogIntensityIce = 1f;
    [Range(0, 1)] public float skyFogHeightIce = 0.9f;
    [Range(0.01f, 1)] public float skyFogSmoothnessIce = 0.01f;
    [Range(0, 1)] public float skyFogFillIce = 0f;
    public float skyFogPositionIce = -0.02f;

    [Header("Maze Parameters")]
    public bool fogEnabledMaze = true;
    public bool affectSkyboxMaze = true;
    [Range(0, 1)] public float fogDensityMaze = 0.5f;
    public float fogStartMaze = 0f;
    public float fogEndMaze = 100f;
    public Color fogColorMaze = Color.black;
    [Range(0, 1)] public float skyFogIntensityMaze = 1f;
    [Range(0, 1)] public float skyFogHeightMaze = 0.9f;
    [Range(0.01f, 1)] public float skyFogSmoothnessMaze = 0.01f;
    [Range(0, 1)] public float skyFogFillMaze = 0f;
    public float skyFogPositionMaze = -0.02f;

    [Header("Desert Parameters")]
    public bool fogEnabledDesert = true;
    public bool affectSkyboxDesert = false;
    [Range(0, 1)] public float fogDensityDesert = 0.5f;
    public float fogStartDesert = 0f;
    public float fogEndDesert = 100f;
    public Color fogColorDesert = Color.black;
    [Range(0, 1)] public float skyFogIntensityDesert = 1f;
    [Range(0, 1)] public float skyFogHeightDesert = 0.9f;
    [Range(0.01f, 1)] public float skyFogSmoothnessDesert = 0.01f;
    [Range(0, 1)] public float skyFogFillDesert = 0f;
    public float skyFogPositionDesert = -0.02f;

    [Header("Space (Void) Parameters")]
    public bool fogEnabledSpace = false;
    public bool affectSkyboxSpace = false;
    [Range(0, 1)] public float fogDensitySpace = 0.5f;
    public float fogStartSpace = 0f;
    public float fogEndSpace = 100f;
    public Color fogColorSpace = Color.black;
    [Range(0, 1)] public float skyFogIntensitySpace = 1f;
    [Range(0, 1)] public float skyFogHeightSpace = 0.9f;
    [Range(0.01f, 1)] public float skyFogSmoothnessSpace = 0.01f;
    [Range(0, 1)] public float skyFogFillSpace = 0f;
    public float skyFogPositionSpace = -0.02f;

    private float fogDensity = 0.5f;
    private float fogStart = 0f;
    private float fogEnd = 100f;
    private Color fogColor = Color.black;
    private bool affectSkybox = false;
    private bool fogEnabled = false;

    static readonly int SkyFogIntensityID  = Shader.PropertyToID("_FogIntensity");
    static readonly int SkyFogHeightID      = Shader.PropertyToID("_FogHeight");
    static readonly int SkyFogSmoothnessID  = Shader.PropertyToID("_FogSmoothness");
    static readonly int SkyFogFillID        = Shader.PropertyToID("_FogFill");
    static readonly int SkyFogPositionID    = Shader.PropertyToID("_FogPosition");
    const string SkyFogKeyword = "_ENABLEFOG_ON";

    void OnEnable()
    {
        _cam = GetComponent<Camera>();
        _cam.depthTextureMode |= DepthTextureMode.Depth;

        if (fogShader == null)
            Debug.LogError("[FogShader] fogShader is not assigned in the inspector; effect will be disabled.", this);
        else if (!fogShader.isSupported)
            Debug.LogError($"[FogShader] Shader '{fogShader.name}' is not supported on this GPU/build; effect will be disabled.", this);
        else
            _mat = new Material(fogShader);
    }

    void OnDisable()
    {
        if (_mat != null)
            DestroyImmediate(_mat);
    }

    public void ChangeDimension(string dimension)
    {
        switch (dimension)
        {
            case "Maze":
                fogColor = fogColorMaze;
                fogStart = fogStartMaze;
                fogEnd = fogEndMaze;
                fogDensity = fogDensityMaze;
                affectSkybox = affectSkyboxMaze;
                fogEnabled = fogEnabledMaze;
                ApplySkyboxFog(fogEnabledMaze, fogColorMaze, skyFogIntensityMaze, skyFogHeightMaze, skyFogSmoothnessMaze, skyFogFillMaze, skyFogPositionMaze);
                break;
            case "Desert":
                fogColor = fogColorDesert;
                fogStart = fogStartDesert;
                fogEnd = fogEndDesert;
                fogDensity = fogDensityDesert;
                affectSkybox = affectSkyboxDesert;
                fogEnabled = fogEnabledDesert;
                ApplySkyboxFog(fogEnabledDesert, fogColorDesert, skyFogIntensityDesert, skyFogHeightDesert, skyFogSmoothnessDesert, skyFogFillDesert, skyFogPositionDesert);
                break;
            case "Space":
                fogColor = fogColorSpace;
                fogStart = fogStartSpace;
                fogEnd = fogEndSpace;
                fogDensity = fogDensitySpace;
                affectSkybox = affectSkyboxSpace;
                fogEnabled = fogEnabledSpace;
                ApplySkyboxFog(fogEnabledSpace, fogColorSpace, skyFogIntensitySpace, skyFogHeightSpace, skyFogSmoothnessSpace, skyFogFillSpace, skyFogPositionSpace);
                break;
            case "Ice":
                fogColor = fogColorIce;
                fogStart = fogStartIce;
                fogEnd = fogEndIce;
                fogDensity = fogDensityIce;
                affectSkybox = affectSkyboxIce;
                fogEnabled = fogEnabledIce;
                ApplySkyboxFog(fogEnabledIce, fogColorIce, skyFogIntensityIce, skyFogHeightIce, skyFogSmoothnessIce, skyFogFillIce, skyFogPositionIce);
                break;
            default:
                ChangeDimension("Desert");
                break;
        }
    }

    void ApplySkyboxFog(bool enabled, Color color, float intensity, float height, float smoothness, float fill, float position)
    {
        RenderSettings.fogColor = color;

        Material sky = RenderSettings.skybox;
        if (sky == null) return;

        if (enabled) sky.EnableKeyword(SkyFogKeyword);
        else         sky.DisableKeyword(SkyFogKeyword);

        sky.SetFloat(SkyFogIntensityID,  intensity);
        sky.SetFloat(SkyFogHeightID,     height);
        sky.SetFloat(SkyFogSmoothnessID, smoothness);
        sky.SetFloat(SkyFogFillID,       fill);
        sky.SetFloat(SkyFogPositionID,   position);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (_mat == null || !fogEnabled) { Graphics.Blit(src, dest); return; }

        _mat.SetFloat("_FogDensity", fogDensity);
        _mat.SetFloat("_FogStart", fogStart);
        _mat.SetFloat("_FogEnd", fogEnd);
        _mat.SetColor("_FogColor", fogColor);
        _mat.SetInt("_AffectSkybox", affectSkybox ? 1 : 0);

        Graphics.Blit(src, dest, _mat, 0);
    }
}
