using UnityEngine;

[ExecuteInEditMode]
public class SkyboxFogController : MonoBehaviour
{
    [Tooltip("Skybox material to drive. Leave empty to use RenderSettings.skybox.")]
    public Material skyboxMaterial;

    [Header("Fog")]
    public bool enableFog = true;
    [Range(0f, 1f)] public float fogIntensity = 1f;
    [Range(0f, 1f)] public float fogHeight = 0.9f;
    [Range(0.01f, 1f)] public float fogSmoothness = 0.01f;
    [Range(0f, 1f)] public float fogFill = 0f;
    public float fogPosition = -0.02f;

    static readonly int FogIntensityID  = Shader.PropertyToID("_FogIntensity");
    static readonly int FogHeightID     = Shader.PropertyToID("_FogHeight");
    static readonly int FogSmoothnessID = Shader.PropertyToID("_FogSmoothness");
    static readonly int FogFillID       = Shader.PropertyToID("_FogFill");
    static readonly int FogPositionID   = Shader.PropertyToID("_FogPosition");
    const string FogKeyword = "_ENABLEFOG_ON";

    Material Target => skyboxMaterial != null ? skyboxMaterial : RenderSettings.skybox;

    void OnEnable()  { Apply(); }
    void Update()    { Apply(); }

    public void Apply()
    {
        Material m = Target;
        if (m == null) return;

        if (enableFog) m.EnableKeyword(FogKeyword);
        else           m.DisableKeyword(FogKeyword);

        m.SetFloat(FogIntensityID,  fogIntensity);
        m.SetFloat(FogHeightID,     fogHeight);
        m.SetFloat(FogSmoothnessID, fogSmoothness);
        m.SetFloat(FogFillID,       fogFill);
        m.SetFloat(FogPositionID,   fogPosition);
    }

    public void SetIntensity(float value)  { fogIntensity = Mathf.Clamp01(value);              Apply(); }
    public void SetHeight(float value)     { fogHeight = Mathf.Clamp01(value);                 Apply(); }
    public void SetSmoothness(float value) { fogSmoothness = Mathf.Clamp(value, 0.01f, 1f);    Apply(); }
    public void SetFill(float value)       { fogFill = Mathf.Clamp01(value);                   Apply(); }
    public void SetPosition(float value)   { fogPosition = value;                              Apply(); }
    public void SetEnabled(bool value)     { enableFog = value;                                Apply(); }
}
